new System.Func<string>(() =>
{
    var stage = "resolve map";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The prepared-food scenario requires a playable map.");
        }

        Find.TickManager.Pause();
        stage = "find two-room fixture area";
        var center = IntVec3.Invalid;
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 70f, true))
        {
            var fixture = new CellRect(candidate.x - 21, candidate.z - 8, 43, 17);
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
                "Could not find a clear area for the prepared-food comparison.");
        }

        var preparedCenter = new IntVec3(center.x - 11, 0, center.z);
        var rawCenter = new IntVec3(center.x + 11, 0, center.z);
        var fixtureRect = new CellRect(center.x - 21, center.z - 8, 43, 17);
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
        foreach (var roomCenter in new[] { preparedCenter, rawCenter })
        {
            var room = new CellRect(roomCenter.x - 9, roomCenter.z - 7, 19, 15);
            foreach (var cell in room.EdgeCells)
            {
                var wall = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
                wall.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(wall, cell, map);
            }
        }

        var cookingWorkType = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        stage = "generate prep chef";
        Pawn prepChef = null;
        for (var attempt = 0; attempt < 48 && prepChef == null; attempt++)
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
                prepChef = candidate;
            }
            else
            {
                candidate.Destroy(DestroyMode.Vanish);
            }
        }

        stage = "generate prepared cook";
        Pawn preparedCook = null;
        for (var attempt = 0; attempt < 48 && preparedCook == null; attempt++)
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
                preparedCook = candidate;
            }
            else
            {
                candidate.Destroy(DestroyMode.Vanish);
            }
        }

        stage = "generate raw cook";
        Pawn rawCook = null;
        for (var attempt = 0; attempt < 48 && rawCook == null; attempt++)
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
                rawCook = candidate;
            }
            else
            {
                candidate.Destroy(DestroyMode.Vanish);
            }
        }

        if (prepChef == null || preparedCook == null || rawCook == null)
        {
            throw new System.InvalidOperationException("Could not generate three capable cooking pawns.");
        }

        prepChef.Name = new NameSingle("Prep Chef");
        preparedCook.Name = new NameSingle("Prepared Cook");
        rawCook.Name = new NameSingle("Raw Cook");
        foreach (var pawn in new[] { prepChef, preparedCook, rawCook })
        {
            pawn.inventory.innerContainer.ClearAndDestroyContents();
            foreach (var trait in pawn.story.traits.allTraits.ToList())
            {
                pawn.story.traits.RemoveTrait(trait, false);
            }
            pawn.workSettings.EnableAndInitialize();
            foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                pawn.workSettings.SetPriority(workType, 0);
            }
            pawn.workSettings.SetPriority(cookingWorkType, 1);
            for (var hour = 0; hour < 24; hour++)
            {
                pawn.timetable.SetAssignment(hour, TimeAssignmentDefOf.Work);
            }
            pawn.needs.food.CurLevel = pawn.needs.food.MaxLevel;
            if (pawn.needs.rest != null)
            {
                pawn.needs.rest.CurLevel = pawn.needs.rest.MaxLevel;
            }
        }
        prepChef.skills.GetSkill(SkillDefOf.Cooking).Level = 20;
        preparedCook.skills.GetSkill(SkillDefOf.Cooking).Level = 10;
        rawCook.skills.GetSkill(SkillDefOf.Cooking).Level = 10;

        stage = "spawn matched workers";
        GenSpawn.Spawn(
            prepChef,
            new IntVec3(preparedCenter.x - 6, 0, preparedCenter.z - 2),
            map);
        GenSpawn.Spawn(
            preparedCook,
            new IntVec3(preparedCenter.x + 6, 0, preparedCenter.z - 2),
            map);
        GenSpawn.Spawn(
            rawCook,
            new IntVec3(rawCenter.x - 6, 0, rawCenter.z - 2),
            map);
        foreach (var pawn in new[] { prepChef, preparedCook, rawCook })
        {
            pawn.drafter.Drafted = true;
        }

        Find.CameraDriver.SetRootPosAndSize(
            new UnityEngine.Vector3(center.x + 0.5f, 0f, center.z + 0.5f),
            25f);
        return prepChef.ThingID + "|" + preparedCook.ThingID + "|" +
               rawCook.ThingID + "|" + center;
    }
    catch (System.Exception error)
    {
        Log.Error(
            "[ImmersiveChefsScenario] Prepared-food structure failed at " + stage +
            ": " + error.GetType().Name + ": " + error.Message);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
