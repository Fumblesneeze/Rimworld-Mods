new System.Func<string>(() =>
{
    var stage = "resolve finalized route Defs";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The kitchenware route matrix requires a playable map.");
        }

        Find.TickManager.Pause();
        var recipeNames = new[]
        {
            "ImmersiveChefs_MakeSoftPlates",
            "ImmersiveChefs_MakeSoftCutlery",
            "ImmersiveChefs_MakeAdobePlates",
            "ImmersiveChefs_MakeMedievalCookware",
            "ImmersiveChefs_SmithPlates",
            "ImmersiveChefs_SmithCutlery"
        };
        var recipes = new RecipeDef[recipeNames.Length];
        for (var index = 0; index < recipeNames.Length; index++)
        {
            recipes[index] = DefDatabase<RecipeDef>.GetNamedSilentFail(recipeNames[index]);
            if (recipes[index] == null)
            {
                throw new System.InvalidOperationException(
                    "The loaded mod set did not finalize recipe " + recipeNames[index] + ".");
            }
        }

        var adobeBricks = DefDatabase<ThingDef>.GetNamedSilentFail("EM_AdobeBricks");
        if (adobeBricks == null || adobeBricks.stuffProps != null)
        {
            throw new System.InvalidOperationException(
                "Expanded Materials - Masonry did not provide fixed non-Stuff EM_AdobeBricks.");
        }

        stage = "find clear fixture area";
        var center = IntVec3.Invalid;
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 65f, true))
        {
            var fixture = new CellRect(candidate.x - 16, candidate.z - 7, 33, 15);
            var valid = true;
            foreach (var cell in fixture.Cells)
            {
                if (!cell.InBounds(map) || cell.GetEdifice(map) != null || cell.GetFirstPawn(map) != null)
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
            {
                center = candidate;
                break;
            }
        }

        if (!center.IsValid)
        {
            throw new System.InvalidOperationException(
                "Could not find a clear area for the six fabrication routes.");
        }

        var fixtureRoom = new CellRect(center.x - 16, center.z - 7, 33, 15);
        foreach (var cell in fixtureRoom.Cells)
        {
            var existing = cell.GetThingList(map).ToList();
            foreach (var thing in existing)
            {
                if (thing.def.category == ThingCategory.Item ||
                    thing.def.category == ThingCategory.Plant ||
                    thing.def.category == ThingCategory.Filth)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }

            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
        }

        stage = "create capable workers";
        var workerNames = new[]
        {
            "Wood Plate Crafter",
            "Wood Cutlery Crafter",
            "Adobe Plate Crafter",
            "Silver Cookware Smith",
            "Silver Plate Smith",
            "Silver Cutlery Smith"
        };
        var workers = new Pawn[workerNames.Length];
        for (var index = 0; index < workers.Length; index++)
        {
            var requiredWorkType = index < 3 ? WorkTypeDefOf.Crafting : WorkTypeDefOf.Smithing;
            for (var attempt = 0; attempt < 32 && workers[index] == null; attempt++)
            {
                var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    PawnKindDefOf.Colonist,
                    Faction.OfPlayer,
                    forceGenerateNewPawn: true,
                    canGeneratePawnRelations: false,
                    forceNoGear: true));
                if (!pawn.WorkTypeIsDisabled(requiredWorkType) &&
                    pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving) &&
                    pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                {
                    workers[index] = pawn;
                    pawn.Name = new NameSingle(workerNames[index]);
                    pawn.inventory.innerContainer.ClearAndDestroyContents();
                    pawn.skills.GetSkill(SkillDefOf.Crafting).Level = 20;
                    pawn.workSettings.EnableAndInitialize();
                    pawn.workSettings.SetPriority(requiredWorkType, 1);
                }
                else
                {
                    pawn.Destroy(DestroyMode.Vanish);
                }
            }

            if (workers[index] == null)
            {
                throw new System.InvalidOperationException(
                    "Could not generate capable worker " + workerNames[index] + ".");
            }
        }

        stage = "create real crafting spots and fueled smithies";
        var offsets = new[]
        {
            new IntVec3(-10, 0, 4),
            new IntVec3(0, 0, 4),
            new IntVec3(10, 0, 4),
            new IntVec3(-10, 0, -4),
            new IntVec3(0, 0, -4),
            new IntVec3(10, 0, -4)
        };
        var tables = new Thing[offsets.Length];
        for (var index = 0; index < tables.Length; index++)
        {
            var tableDef = DefDatabase<ThingDef>.GetNamed(index < 3 ? "CraftingSpot" : "FueledSmithy");
            var table = ThingMaker.MakeThing(
                tableDef,
                tableDef.MadeFromStuff ? ThingDefOf.Steel : null);
            table.SetFactionDirect(Faction.OfPlayer);
            var tableCell = new IntVec3(center.x + offsets[index].x, 0, center.z + offsets[index].z);
            GenSpawn.Spawn(table, tableCell, map, Rot4.North);
            tables[index] = table;

            if (index >= 3)
            {
                var refuelable = table.TryGetComp<CompRefuelable>();
                if (refuelable == null)
                {
                    throw new System.InvalidOperationException("FueledSmithy has no CompRefuelable.");
                }

                refuelable.Refuel(50f);
                if (!refuelable.HasFuel)
                {
                    throw new System.InvalidOperationException("A route smithy refused its fixture fuel.");
                }
            }

            GenSpawn.Spawn(
                workers[index],
                new IntVec3(tableCell.x - 4, 0, tableCell.z),
                map);
        }

        stage = "create exact route ingredients";
        var primaryMaterials = new[]
        {
            ThingDefOf.WoodLog,
            ThingDefOf.WoodLog,
            adobeBricks,
            ThingDefOf.Silver,
            ThingDefOf.Silver,
            ThingDefOf.Silver
        };
        var primaryCounts = new[] { 4, 2, 4, 6, 4, 2 };
        for (var index = 0; index < tables.Length; index++)
        {
            var primary = ThingMaker.MakeThing(primaryMaterials[index]);
            primary.stackCount = primaryCounts[index];
            GenSpawn.Spawn(
                primary,
                new IntVec3(tables[index].Position.x - 2, 0, tables[index].Position.z + 2),
                map);
            if (index == 3)
            {
                var handles = ThingMaker.MakeThing(ThingDefOf.WoodLog);
                handles.stackCount = 1;
                GenSpawn.Spawn(
                    handles,
                    new IntVec3(tables[index].Position.x - 2, 0, tables[index].Position.z - 2),
                    map);
            }
        }

        stage = "create real pawn-restricted one-shot bills";
        for (var index = 0; index < tables.Length; index++)
        {
            var bill = new Bill_Production(recipes[index]);
            bill.repeatMode = BillRepeatModeDefOf.RepeatCount;
            bill.repeatCount = 1;
            bill.ingredientSearchRadius = 10f;
            bill.ingredientFilter.SetDisallowAll();
            bill.ingredientFilter.SetAllow(primaryMaterials[index], true);
            if (index == 3)
            {
                bill.ingredientFilter.SetAllow(ThingDefOf.WoodLog, true);
            }

            bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
            bill.SetPawnRestriction(workers[index]);
            ((IBillGiver)tables[index]).BillStack.AddBill(bill);
        }

        stage = "frame the six routes";
        Find.CameraDriver.JumpToCurrentMapLoc(center);
        Find.CameraDriver.SetRootSize(28f);
        Find.Selector.ClearSelection();
        for (var index = 0; index < workers.Length; index++)
        {
            Find.Selector.Select(workers[index]);
        }

        Find.TickManager.Pause();
        return string.Join("|", workerNames);
    }
    catch (System.Exception error)
    {
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
