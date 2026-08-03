new System.Func<string>(() =>
{
    var stage = "resolve map";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException("The patient-feeding scenario requires a playable map.");
        }

        Find.TickManager.Pause();
        stage = "find fixture room";
        var center = IntVec3.Invalid;
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 40f, true))
        {
            var room = new CellRect(candidate.x - 11, candidate.z - 6, 23, 13);
            var valid = true;
            foreach (var cell in room.Cells)
            {
                if (!cell.InBounds(map) ||
                    cell.GetEdifice(map) != null ||
                    cell.GetFirstPawn(map) != null)
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
            throw new System.InvalidOperationException("Could not find a clear area for the patient-feeding fixture.");
        }

        stage = "build fixture room";
        var fixtureRoom = new CellRect(center.x - 11, center.z - 6, 23, 13);
        foreach (var cell in fixtureRoom.Cells)
        {
            foreach (var thing in cell.GetThingList(map)
                         .Where(thing => thing.def.category == ThingCategory.Item ||
                                         thing.def.category == ThingCategory.Plant ||
                                         thing.def.category == ThingCategory.Filth)
                         .ToList())
            {
                thing.Destroy(DestroyMode.Vanish);
            }

            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
            if (cell.x == fixtureRoom.minX || cell.x == fixtureRoom.maxX ||
                cell.z == fixtureRoom.minZ || cell.z == fixtureRoom.maxZ)
            {
                var wall = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
                wall.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(wall, cell, map);
            }
        }

        foreach (var existing in map.listerThings.ThingsOfDef(
                     DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery")))
        {
            existing.SetForbidden(true, false);
        }

        stage = "create patients and feeders";
        var patientNames = new[] { "Cutlery Patient", "Conscious Patient", "Unconscious Patient" };
        var feederNames = new[] { "Cutlery Nurse", "Conscious Nurse", "Unconscious Nurse" };
        var xOffsets = new[] { -7, 0, 7 };
        var resultIds = new System.Collections.Generic.List<string>();

        for (var index = 0; index < patientNames.Length; index++)
        {
            var bedCell = new IntVec3(center.x + xOffsets[index], 0, center.z - 2);
            var bed = (Building_Bed)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog);
            bed.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(bed, bedCell, map, Rot4.North);
            bed.Medical = true;

            var patient = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false));
            patient.Name = new NameSingle(patientNames[index]);
            patient.inventory.innerContainer.ClearAndDestroyContents();
            GenSpawn.Spawn(patient, bed.GetSleepingSlotPos(0), map);
            var injuryPart = patient.health.hediffSet.GetNotMissingParts()
                .First(part => part.def == BodyPartDefOf.Torso);
            var injury = HediffMaker.MakeHediff(HediffDefOf.Cut, patient, injuryPart);
            injury.Severity = 5f;
            patient.health.AddHediff(injury);
            injury.Tended(1f, 1f);
            patient.needs.food.CurLevel = 0.01f;
            var layDown = JobMaker.MakeJob(JobDefOf.LayDown, bed);
            layDown.restUntilHealed = true;
            patient.jobs.StartJob(layDown, Verse.AI.JobCondition.InterruptForced);
            for (var tick = 0; tick < 30 && !patient.InBed(); tick++)
            {
                patient.jobs.JobTrackerTick();
            }

            if (!patient.InBed())
            {
                throw new System.InvalidOperationException(patientNames[index] + " did not enter the medical bed.");
            }

            if (index == 2)
            {
                patient.health.AddHediff(HediffDefOf.Anesthetic);
                if (patient.health.capacities.CanBeAwake)
                {
                    throw new System.InvalidOperationException("The unconscious patient can still be awake.");
                }
            }

            var feeder = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false));
            feeder.Name = new NameSingle(feederNames[index]);
            feeder.inventory.innerContainer.ClearAndDestroyContents();
            GenSpawn.Spawn(feeder, new IntVec3(bedCell.x, 0, center.z + 4), map);

            var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
            var plate = (ThingWithComps)ThingMaker.MakeThing(
                DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
                ThingDefOf.Steel);
            plate.GetComp<ImmersiveChefs.CompSanitation>()
                .MarkClean(ImmersiveChefs.WashProvenance.Safe);
            meal.GetComp<ImmersiveChefs.CompCulinaryState>().ReplaceServings(new[]
            {
                new ImmersiveChefs.CulinaryServingRecord(
                    50,
                    index == 0 ? -5f : 35f,
                    ImmersiveChefs.ContaminationSources.None,
                    0,
                    Find.TickManager.TicksGame)
            });
            if (!meal.GetComp<ImmersiveChefs.CompEmbeddedWare>().TryEmbedPlate(plate))
            {
                throw new System.InvalidOperationException("Could not embed " + patientNames[index] + "'s plate.");
            }

            GenSpawn.Spawn(meal, new IntVec3(bedCell.x + 2, 0, center.z + 2), map);
            resultIds.Add(patient.ThingID);
            resultIds.Add(feeder.ThingID);
            resultIds.Add(meal.ThingID);
            resultIds.Add(plate.ThingID);
        }

        stage = "create powered microwave";
        var microwave = ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Microwave"));
        microwave.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(
            microwave,
            new IntVec3(center.x - 3, 0, center.z + 1),
            map,
            Rot4.North);
        microwave.TryGetComp<CompPowerTrader>().PowerOn = true;
        resultIds.Add(microwave.ThingID);

        stage = "create reserved cutlery";
        var cutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        cutlery.GetComp<ImmersiveChefs.CompSanitation>()
            .MarkClean(ImmersiveChefs.WashProvenance.Safe);
        GenSpawn.Spawn(cutlery, new IntVec3(center.x - 7, 0, center.z + 1), map);
        resultIds.Add(cutlery.ThingID);

        stage = "frame fixture";
        Find.CameraDriver.JumpToCurrentMapLoc(center);
        Find.CameraDriver.SetRootSize(29f);
        Find.Selector.ClearSelection();
        Find.Selector.Select(map.mapPawns.AllPawnsSpawned.Single(
            pawn => pawn.LabelShort == "Cutlery Nurse"));
        resultIds.Add(center.ToString());
        return string.Join("|", resultIds.ToArray());
    }
    catch (System.Exception error)
    {
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
