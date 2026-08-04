new System.Func<string>(() =>
{
    const string dinerName = "Common Sense Diner";
    const string nurseName = "Common Sense Nurse";
    const string patientName = "Common Sense Patient";
    var stage = "resolve map";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The Common Sense cleanup scenario requires a playable map.");
        }

        Find.TickManager.Pause();
        stage = "find fixture room";
        var center = IntVec3.Invalid;
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 45f, true))
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
            throw new System.InvalidOperationException(
                "Could not find a clear area for the Common Sense cleanup fixture.");
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

        stage = "create powered dishwasher";
        var conduitDef = DefDatabase<ThingDef>.GetNamed("PowerConduit");
        for (var x = center.x - 3; x <= center.x + 3; x++)
        {
            var conduit = ThingMaker.MakeThing(conduitDef);
            conduit.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(conduit, new IntVec3(x, 0, center.z + 4), map);
        }

        var dishwasher = ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Dishwasher"));
        dishwasher.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(
            dishwasher,
            new IntVec3(center.x - 1, 0, center.z + 4),
            map,
            Rot4.North);
        var battery = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Battery"));
        battery.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(
            battery,
            new IntVec3(center.x + 2, 0, center.z + 4),
            map,
            Rot4.North);
        battery.TryGetComp<CompPowerBattery>().SetStoredEnergyPct(1f);
        map.powerNetManager.UpdatePowerNetsAndConnections_First();
        var dishwasherPower = dishwasher.TryGetComp<CompPowerTrader>();
        for (var tick = 0; tick <= 200 && !dishwasherPower.PowerOn; tick++)
        {
            Find.TickManager.DoSingleTick();
        }

        if (!dishwasherPower.PowerOn)
        {
            throw new System.InvalidOperationException(
                "RimWorld did not connect the Common Sense dishwasher to its charged battery.");
        }

        stage = "create cleaning-capable pawns";
        System.Func<string, Pawn> createPawn = name =>
        {
            for (var attempt = 0; attempt < 32; attempt++)
            {
                var candidate = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    PawnKindDefOf.Colonist,
                    Faction.OfPlayer,
                    forceGenerateNewPawn: true,
                    canGeneratePawnRelations: false));
                if (!candidate.WorkTypeIsDisabled(WorkTypeDefOf.Cleaning) &&
                    candidate.health.capacities.CapableOf(PawnCapacityDefOf.Moving) &&
                    candidate.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                {
                    candidate.Name = new NameSingle(name);
                    candidate.inventory.innerContainer.ClearAndDestroyContents();
                    return candidate;
                }

                candidate.Destroy(DestroyMode.Vanish);
            }

            throw new System.InvalidOperationException(
                "Could not generate a cleaning-capable pawn for " + name + ".");
        };

        var diner = createPawn(dinerName);
        var nurse = createPawn(nurseName);
        var patient = createPawn(patientName);
        GenSpawn.Spawn(diner, new IntVec3(center.x - 7, 0, center.z), map);
        GenSpawn.Spawn(nurse, new IntVec3(center.x + 7, 0, center.z + 3), map);
        foreach (var worker in new[] { diner, nurse })
        {
            worker.workSettings.EnableAndInitialize();
            foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                worker.workSettings.SetPriority(workType, 0);
            }
        }
        nurse.workSettings.SetPriority(WorkTypeDefOf.Doctor, 1);
        nurse.drafter.Drafted = true;

        stage = "place patient in medical bed";
        var bed = (Building_Bed)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog);
        bed.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(bed, new IntVec3(center.x + 5, 0, center.z - 2), map, Rot4.North);
        bed.Medical = true;
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
            throw new System.InvalidOperationException("The Common Sense patient did not enter the medical bed.");
        }

        stage = "create plated meals and cutlery";
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        var cutleryDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery");
        var dinerMeal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var dinerPlate = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        dinerPlate.GetComp<ImmersiveChefs.CompSanitation>()
            .MarkClean(ImmersiveChefs.WashProvenance.Safe);
        dinerMeal.GetComp<ImmersiveChefs.CompCulinaryState>().ReplaceServings(new[]
        {
            new ImmersiveChefs.CulinaryServingRecord(
                60,
                35f,
                ImmersiveChefs.ContaminationSources.None,
                0,
                Find.TickManager.TicksGame)
        });
        if (!dinerMeal.GetComp<ImmersiveChefs.CompEmbeddedWare>().TryEmbedPlate(dinerPlate))
        {
            throw new System.InvalidOperationException("Could not embed the diner plate.");
        }

        var patientMeal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var patientPlate = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        patientPlate.GetComp<ImmersiveChefs.CompSanitation>()
            .MarkClean(ImmersiveChefs.WashProvenance.Safe);
        patientMeal.GetComp<ImmersiveChefs.CompCulinaryState>().ReplaceServings(new[]
        {
            new ImmersiveChefs.CulinaryServingRecord(
                60,
                35f,
                ImmersiveChefs.ContaminationSources.None,
                0,
                Find.TickManager.TicksGame)
        });
        if (!patientMeal.GetComp<ImmersiveChefs.CompEmbeddedWare>().TryEmbedPlate(patientPlate))
        {
            throw new System.InvalidOperationException("Could not embed the patient plate.");
        }

        var dinerCutlery = (ThingWithComps)ThingMaker.MakeThing(cutleryDef, ThingDefOf.Steel);
        dinerCutlery.GetComp<ImmersiveChefs.CompSanitation>()
            .MarkClean(ImmersiveChefs.WashProvenance.Safe);
        var patientCutlery = (ThingWithComps)ThingMaker.MakeThing(cutleryDef, ThingDefOf.Steel);
        patientCutlery.GetComp<ImmersiveChefs.CompSanitation>()
            .MarkClean(ImmersiveChefs.WashProvenance.Safe);
        GenSpawn.Spawn(dinerMeal, new IntVec3(center.x - 5, 0, center.z), map);
        GenSpawn.Spawn(dinerCutlery, new IntVec3(center.x - 6, 0, center.z + 2), map);
        GenSpawn.Spawn(patientMeal, new IntVec3(center.x + 7, 0, center.z), map);
        GenSpawn.Spawn(patientCutlery, new IntVec3(center.x + 6, 0, center.z + 2), map);
        diner.needs.food.CurLevelPercentage = 0.45f;

        stage = "frame fixture";
        Find.CameraDriver.SetRootPosAndSize(
            new UnityEngine.Vector3(center.x + 0.5f, 0f, center.z + 0.5f),
            13f);
        map.mapDrawer.RegenerateEverythingNow();
        Find.Selector.ClearSelection();
        Find.Selector.Select(diner);
        Messages.Message(
            "Common Sense cleanup ready: order the selected diner to eat, then order the nurse to feed the patient.",
            MessageTypeDefOf.NeutralEvent,
            false);
        return string.Join("|", new[]
        {
            diner.ThingID,
            dinerMeal.ThingID,
            dinerPlate.ThingID,
            dinerCutlery.ThingID,
            nurse.ThingID,
            patient.ThingID,
            patientMeal.ThingID,
            patientPlate.ThingID,
            patientCutlery.ThingID,
            dishwasher.ThingID,
            battery.ThingID,
            center.ToString()
        });
    }
    catch (System.Exception error)
    {
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
