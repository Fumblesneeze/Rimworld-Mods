new System.Func<string>(() =>
{
    var stage = "resolve map";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The hidden-provenance scenario requires a playable map.");
        }

        Find.TickManager.Pause();
        stage = "find clear fixture area";
        var center = IntVec3.Invalid;
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 70f, true))
        {
            var fixture = new CellRect(candidate.x - 19, candidate.z - 7, 39, 15);
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
                "Could not find a clear area for the hidden-provenance fixture.");
        }

        var billCenter = new IntVec3(center.x - 10, 0, center.z);
        var policyCenter = new IntVec3(center.x + 10, 0, center.z);
        var fixtureRect = new CellRect(center.x - 19, center.z - 7, 39, 15);
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

        stage = "build sealed comparison rooms";
        foreach (var roomCenter in new[] { billCenter, policyCenter })
        {
            var room = new CellRect(roomCenter.x - 8, roomCenter.z - 6, 17, 13);
            foreach (var cell in room.EdgeCells)
            {
                var wall = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
                wall.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(wall, cell, map);
            }
        }

        var cookingWorkType = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        stage = "generate bill cook";
        Pawn cook = null;
        for (var attempt = 0; attempt < 48 && cook == null; attempt++)
        {
            var candidate = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                forceNoGear: true));
            if (!candidate.WorkTypeIsDisabled(cookingWorkType) &&
                candidate.health.capacities.GetLevel(PawnCapacityDefOf.Moving) >= 0.99f &&
                candidate.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) >= 0.99f)
            {
                cook = candidate;
            }
            else
            {
                candidate.Destroy(DestroyMode.Vanish);
            }
        }

        stage = "generate policy diner";
        Pawn diner = null;
        for (var attempt = 0; attempt < 48 && diner == null; attempt++)
        {
            var candidate = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                forceNoGear: true));
            if (candidate.health.capacities.GetLevel(PawnCapacityDefOf.Moving) >= 0.99f &&
                candidate.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) >= 0.99f)
            {
                diner = candidate;
            }
            else
            {
                candidate.Destroy(DestroyMode.Vanish);
            }
        }

        if (cook == null || diner == null)
        {
            throw new System.InvalidOperationException(
                "Could not generate both hidden-provenance fixture pawns.");
        }

        cook.Name = new NameSingle("Hidden Bill Cook");
        diner.Name = new NameSingle("Hidden Policy Diner");
        foreach (var pawn in new[] { cook, diner })
        {
            pawn.inventory.innerContainer.ClearAndDestroyContents();
            pawn.workSettings.EnableAndInitialize();
            foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                pawn.workSettings.SetPriority(workType, 0);
            }
            if (pawn.needs.rest != null)
            {
                pawn.needs.rest.CurLevel = pawn.needs.rest.MaxLevel;
            }
        }
        cook.workSettings.SetPriority(cookingWorkType, 1);
        cook.skills.GetSkill(SkillDefOf.Cooking).Level = 12;
        for (var hour = 0; hour < 24; hour++)
        {
            cook.timetable.SetAssignment(hour, TimeAssignmentDefOf.Work);
            diner.timetable.SetAssignment(hour, TimeAssignmentDefOf.Anything);
        }
        cook.needs.food.CurLevel = cook.needs.food.MaxLevel;
        diner.needs.food.CurLevel = 0.15f;
        GenSpawn.Spawn(cook, new IntVec3(billCenter.x - 5, 0, billCenter.z - 2), map);
        GenSpawn.Spawn(diner, new IntVec3(policyCenter.x - 4, 0, policyCenter.z - 2), map);
        cook.drafter.Drafted = true;
        diner.drafter.Drafted = true;

        Find.CameraDriver.SetRootPosAndSize(
            new UnityEngine.Vector3(center.x + 0.5f, 0f, center.z + 0.5f),
            23f);
        return cook.ThingID + "|" + diner.ThingID + "|" + center;
    }
    catch (System.Exception error)
    {
        Log.Error(
            "[ImmersiveChefsScenario] Hidden provenance structure failed at " + stage +
            ": " + error.GetType().Name + ": " + error.Message);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
