new System.Func<string>(() =>
{
    var stage = "resolve map";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The recipe-complexity scenario requires a playable map.");
        }

        Find.TickManager.Pause();
        stage = "find clear fixture area";
        var center = IntVec3.Invalid;
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 75f, true))
        {
            var fixture = new CellRect(candidate.x - 25, candidate.z - 7, 51, 15);
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
                "Could not find a clear area for three isolated recipe fixtures.");
        }

        var fixtureRect = new CellRect(center.x - 25, center.z - 7, 51, 15);
        foreach (var cell in fixtureRect.Cells)
        {
            foreach (var thing in cell.GetThingList(map).ToList())
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

        var simpleCenter = new IntVec3(center.x - 17, 0, center.z);
        var fineCenter = new IntVec3(center.x, 0, center.z);
        var lavishCenter = new IntVec3(center.x + 17, 0, center.z);
        stage = "build mutually unreachable kitchens";
        foreach (var roomCenter in new[] { simpleCenter, fineCenter, lavishCenter })
        {
            var room = new CellRect(roomCenter.x - 6, roomCenter.z - 5, 13, 11);
            foreach (var cell in room.EdgeCells)
            {
                var wall = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
                wall.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(wall, cell, map);
            }
        }

        var cookingWorkType = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        stage = "create matched simple-meal cook";
        Pawn simpleCook = null;
        for (var attempt = 0; attempt < 32 && simpleCook == null; attempt++)
        {
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist, Faction.OfPlayer, forceGenerateNewPawn: true,
                canGeneratePawnRelations: false, forceNoGear: true));
            if (!pawn.WorkTypeIsDisabled(cookingWorkType) &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) >= 0.99f &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) >= 0.99f)
            {
                simpleCook = pawn;
                simpleCook.Name = new NameSingle("Simple Recipe Cook");
            }
            else
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }

        stage = "create matched fine-meal cook";
        Pawn fineCook = null;
        for (var attempt = 0; attempt < 32 && fineCook == null; attempt++)
        {
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist, Faction.OfPlayer, forceGenerateNewPawn: true,
                canGeneratePawnRelations: false, forceNoGear: true));
            if (!pawn.WorkTypeIsDisabled(cookingWorkType) &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) >= 0.99f &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) >= 0.99f)
            {
                fineCook = pawn;
                fineCook.Name = new NameSingle("Fine Recipe Cook");
            }
            else
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }

        stage = "create matched lavish-meal cook";
        Pawn lavishCook = null;
        for (var attempt = 0; attempt < 32 && lavishCook == null; attempt++)
        {
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist, Faction.OfPlayer, forceGenerateNewPawn: true,
                canGeneratePawnRelations: false, forceNoGear: true));
            if (!pawn.WorkTypeIsDisabled(cookingWorkType) &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) >= 0.99f &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) >= 0.99f)
            {
                lavishCook = pawn;
                lavishCook.Name = new NameSingle("Lavish Recipe Cook");
            }
            else
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }

        if (simpleCook == null || fineCook == null || lavishCook == null)
        {
            throw new System.InvalidOperationException("Could not generate three matched capable cooks.");
        }

        foreach (var cook in new[] { simpleCook, fineCook, lavishCook })
        {
            cook.inventory.innerContainer.ClearAndDestroyContents();
            foreach (var trait in cook.story.traits.allTraits.ToList())
            {
                cook.story.traits.RemoveTrait(trait, false);
            }
            cook.skills.GetSkill(SkillDefOf.Cooking).Level = 20;
            cook.workSettings.EnableAndInitialize();
            cook.workSettings.SetPriority(cookingWorkType, 1);
        }

        GenSpawn.Spawn(simpleCook, new IntVec3(simpleCenter.x, 0, simpleCenter.z - 3), map);
        GenSpawn.Spawn(fineCook, new IntVec3(fineCenter.x, 0, fineCenter.z - 3), map);
        GenSpawn.Spawn(lavishCook, new IntVec3(lavishCenter.x, 0, lavishCenter.z - 3), map);

        var stoveDef = DefDatabase<ThingDef>.GetNamed("FueledStove");
        var rawRice = DefDatabase<ThingDef>.GetNamed("RawRice");
        var milk = DefDatabase<ThingDef>.GetNamed("Milk");
        stage = "create simple stove and exact bill";
        var simpleStove = ThingMaker.MakeThing(stoveDef, stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
        simpleStove.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(simpleStove, simpleCenter, map, Rot4.North);
        var simpleFuel = simpleStove.TryGetComp<CompRefuelable>();
        simpleFuel.Refuel(simpleFuel.Props.fuelCapacity);
        var simpleBill = new Bill_Production(DefDatabase<RecipeDef>.GetNamed("CookMealSimple"));
        simpleBill.repeatMode = BillRepeatModeDefOf.RepeatCount;
        simpleBill.repeatCount = 1;
        simpleBill.ingredientSearchRadius = 8f;
        simpleBill.ingredientFilter.SetDisallowAll();
        simpleBill.ingredientFilter.SetAllow(rawRice, true);
        simpleBill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        simpleBill.SetPawnRestriction(simpleCook);
        ((IBillGiver)simpleStove).BillStack.AddBill(simpleBill);

        stage = "create fine stove and exact bill";
        var fineStove = ThingMaker.MakeThing(stoveDef, stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
        fineStove.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(fineStove, fineCenter, map, Rot4.North);
        var fineFuel = fineStove.TryGetComp<CompRefuelable>();
        fineFuel.Refuel(fineFuel.Props.fuelCapacity);
        var fineBill = new Bill_Production(DefDatabase<RecipeDef>.GetNamed("CookMealFine"));
        fineBill.repeatMode = BillRepeatModeDefOf.RepeatCount;
        fineBill.repeatCount = 1;
        fineBill.ingredientSearchRadius = 8f;
        fineBill.ingredientFilter.SetDisallowAll();
        fineBill.ingredientFilter.SetAllow(rawRice, true);
        fineBill.ingredientFilter.SetAllow(milk, true);
        fineBill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        fineBill.SetPawnRestriction(fineCook);
        ((IBillGiver)fineStove).BillStack.AddBill(fineBill);

        stage = "create lavish stove and exact bill";
        var lavishStove = ThingMaker.MakeThing(stoveDef, stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
        lavishStove.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(lavishStove, lavishCenter, map, Rot4.North);
        var lavishFuel = lavishStove.TryGetComp<CompRefuelable>();
        lavishFuel.Refuel(lavishFuel.Props.fuelCapacity);
        var lavishBill = new Bill_Production(DefDatabase<RecipeDef>.GetNamed("CookMealLavish"));
        lavishBill.repeatMode = BillRepeatModeDefOf.RepeatCount;
        lavishBill.repeatCount = 1;
        lavishBill.ingredientSearchRadius = 8f;
        lavishBill.ingredientFilter.SetDisallowAll();
        lavishBill.ingredientFilter.SetAllow(rawRice, true);
        lavishBill.ingredientFilter.SetAllow(milk, true);
        lavishBill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        lavishBill.SetPawnRestriction(lavishCook);
        ((IBillGiver)lavishStove).BillStack.AddBill(lavishBill);

        ImmersiveChefs.ImmersiveChefsMod.Settings.WareRequirementMode =
            ImmersiveChefs.WareRequirementMode.Strict;
        ImmersiveChefs.ImmersiveChefsMod.Settings.AutoCallAssistants = false;
        Find.CameraDriver.JumpToCurrentMapLoc(center);
        Find.CameraDriver.SetRootSize(34f);
        Find.Selector.ClearSelection();
        Find.Selector.Select(simpleCook);
        Find.Selector.Select(fineCook);
        Find.Selector.Select(lavishCook);
        Find.TickManager.Pause();
        return simpleCook.ThingID + "|" + fineCook.ThingID + "|" + lavishCook.ThingID + "|" +
               simpleStove.ThingID + "|" + fineStove.ThingID + "|" + lavishStove.ThingID;
    }
    catch (System.Exception error)
    {
        Log.Error(
            "[ImmersiveChefsScenario] Recipe-complexity structure failed at " + stage +
            ": " + error.GetType().Name + ": " + error.Message);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
