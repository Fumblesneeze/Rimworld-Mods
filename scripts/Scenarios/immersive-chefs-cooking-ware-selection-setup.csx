new System.Func<string>(() =>
{
    var stage = "resolve map";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The cooking ware-selection scenario requires a playable map.");
        }

        Find.TickManager.Pause();
        stage = "find clear fixture area";
        var center = IntVec3.Invalid;
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 65f, true))
        {
            var fixture = new CellRect(candidate.x - 18, candidate.z - 7, 37, 19);
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
                "Could not find a clear area for the isolated cooking fixtures.");
        }

        var fixtureRect = new CellRect(center.x - 18, center.z - 7, 37, 19);
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

        var cleanCenter = new IntVec3(center.x - 10, 0, center.z);
        var urgentCenter = new IntVec3(center.x + 10, 0, center.z);
        var starvingCenter = new IntVec3(center.x, 0, center.z + 8);

        stage = "build mutually unreachable rooms";
        foreach (var room in new[]
                 {
                     new CellRect(cleanCenter.x - 6, cleanCenter.z - 5, 13, 11),
                     new CellRect(urgentCenter.x - 6, urgentCenter.z - 5, 13, 11),
                     new CellRect(starvingCenter.x - 2, starvingCenter.z - 2, 5, 5)
                 })
        {
            foreach (var cell in room.EdgeCells)
            {
                var wall = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
                wall.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(wall, cell, map);
            }
        }

        stage = "create clean-preference cook";
        var cookingWorkType = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        Pawn cleanCook = null;
        for (var attempt = 0; attempt < 32 && cleanCook == null; attempt++)
        {
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                forceNoGear: true));
            if (!pawn.WorkTypeIsDisabled(cookingWorkType) &&
                pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving) &&
                pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                cleanCook = pawn;
                cleanCook.Name = new NameSingle("Clean Ware Cook");
                cleanCook.inventory.innerContainer.ClearAndDestroyContents();
                cleanCook.skills.GetSkill(SkillDefOf.Cooking).Level = 20;
                cleanCook.workSettings.EnableAndInitialize();
                cleanCook.workSettings.SetPriority(cookingWorkType, 1);
            }
            else
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }

        stage = "create urgent-fallback cook";
        Pawn urgentCook = null;
        for (var attempt = 0; attempt < 32 && urgentCook == null; attempt++)
        {
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                forceNoGear: true));
            if (!pawn.WorkTypeIsDisabled(cookingWorkType) &&
                pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving) &&
                pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                urgentCook = pawn;
                urgentCook.Name = new NameSingle("Urgent Dirty Ware Cook");
                urgentCook.inventory.innerContainer.ClearAndDestroyContents();
                urgentCook.skills.GetSkill(SkillDefOf.Cooking).Level = 20;
                urgentCook.workSettings.EnableAndInitialize();
                urgentCook.workSettings.SetPriority(cookingWorkType, 1);
            }
            else
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }

        if (cleanCook == null || urgentCook == null)
        {
            throw new System.InvalidOperationException("Could not generate both capable cooks.");
        }
        GenSpawn.Spawn(cleanCook, new IntVec3(cleanCenter.x, 0, cleanCenter.z - 3), map);
        GenSpawn.Spawn(urgentCook, new IntVec3(urgentCenter.x, 0, urgentCenter.z - 3), map);

        stage = "create sealed starving requester";
        var starvingPawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            Faction.OfPlayer,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false,
            forceNoGear: true));
        starvingPawn.Name = new NameSingle("Starving Meal Requester");
        starvingPawn.inventory.innerContainer.ClearAndDestroyContents();
        starvingPawn.needs.food.CurLevel = 0.01f;
        GenSpawn.Spawn(starvingPawn, starvingCenter, map);

        stage = "create clean-preference stove and bill";
        var stoveDef = DefDatabase<ThingDef>.GetNamed("FueledStove");
        var recipe = DefDatabase<RecipeDef>.GetNamed("CookMealSimple");
        var rawRice = DefDatabase<ThingDef>.GetNamed("RawRice");
        var cleanStove = ThingMaker.MakeThing(stoveDef, stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
        cleanStove.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(cleanStove, cleanCenter, map, Rot4.North);
        var cleanFuel = cleanStove.TryGetComp<CompRefuelable>();
        cleanFuel.Refuel(cleanFuel.Props.fuelCapacity);
        var cleanBill = new Bill_Production(recipe);
        cleanBill.repeatMode = BillRepeatModeDefOf.RepeatCount;
        cleanBill.repeatCount = 1;
        cleanBill.ingredientSearchRadius = 8f;
        cleanBill.ingredientFilter.SetDisallowAll();
        cleanBill.ingredientFilter.SetAllow(rawRice, true);
        cleanBill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        cleanBill.SetPawnRestriction(cleanCook);
        ((IBillGiver)cleanStove).BillStack.AddBill(cleanBill);

        stage = "create urgent-fallback stove and bill";
        var urgentStove = ThingMaker.MakeThing(stoveDef, stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
        urgentStove.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(urgentStove, urgentCenter, map, Rot4.North);
        var urgentFuel = urgentStove.TryGetComp<CompRefuelable>();
        urgentFuel.Refuel(urgentFuel.Props.fuelCapacity);
        var urgentBill = new Bill_Production(recipe);
        urgentBill.repeatMode = BillRepeatModeDefOf.RepeatCount;
        urgentBill.repeatCount = 1;
        urgentBill.ingredientSearchRadius = 8f;
        urgentBill.ingredientFilter.SetDisallowAll();
        urgentBill.ingredientFilter.SetAllow(rawRice, true);
        urgentBill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        urgentBill.SetPawnRestriction(urgentCook);
        ((IBillGiver)urgentStove).BillStack.AddBill(urgentBill);

        stage = "frame fixtures";
        Find.CameraDriver.JumpToCurrentMapLoc(center);
        Find.CameraDriver.SetRootSize(27f);
        Find.Selector.ClearSelection();
        Find.Selector.Select(cleanCook);
        Find.Selector.Select(urgentCook);
        Find.TickManager.Pause();
        return cleanCook.ThingID + "|" + urgentCook.ThingID + "|" + starvingPawn.ThingID + "|" +
               cleanStove.ThingID + "|" + urgentStove.ThingID;
    }
    catch (System.Exception error)
    {
        Log.Error(
            "[ImmersiveChefsScenario] Cooking structure setup failed at " + stage +
            ": " + error.GetType().Name + ": " + error.Message);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
