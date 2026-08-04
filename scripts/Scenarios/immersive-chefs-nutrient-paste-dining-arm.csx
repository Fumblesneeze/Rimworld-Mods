new System.Func<string>(() =>
{
    var stage = "resolve exact fixture";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException("The nutrient-paste fixture has no map.");
        }

        Pawn diner = null;
        foreach (var pawn in map.mapPawns.AllPawnsSpawned)
        {
            if (pawn.LabelShort == "Nutrient Paste Plate Diner")
            {
                diner = pawn;
                break;
            }
        }

        if (diner == null)
        {
            throw new System.InvalidOperationException("The exact nutrient-paste diner is unavailable.");
        }

        Building_NutrientPasteDispenser dispenser = null;
        var nearbyDispenserCount = 0;
        foreach (var candidate in map.listerBuildings.AllBuildingsColonistOfClass<Building_NutrientPasteDispenser>())
        {
            if (candidate.Position.DistanceToSquared(diner.Position) <= 144)
            {
                dispenser = candidate;
                nearbyDispenserCount++;
            }
        }

        if (nearbyDispenserCount != 1 || dispenser == null || !dispenser.CanDispenseNow)
        {
            throw new System.InvalidOperationException(
                "Expected one nearby operational nutrient-paste dispenser, found " +
                nearbyDispenserCount + ".");
        }

        stage = "ask vanilla food chooser";
        var tryGiveFood = HarmonyLib.AccessTools.Method(typeof(JobGiver_GetFood), "TryGiveJob");
        if (tryGiveFood == null)
        {
            throw new System.InvalidOperationException("Vanilla JobGiver_GetFood.TryGiveJob is unavailable.");
        }

        diner.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced, false);
        diner.needs.food.CurLevelPercentage = 0.2f;
        var job = (Verse.AI.Job)tryGiveFood.Invoke(new JobGiver_GetFood(), new object[] { diner });
        if (job == null || job.def != JobDefOf.Ingest ||
            !System.Object.ReferenceEquals(job.targetA.Thing, dispenser))
        {
            throw new System.InvalidOperationException(
                "Vanilla did not choose the exact nutrient-paste dispenser ingest job.");
        }

        job.playerForced = true;
        diner.jobs.StartJob(job, Verse.AI.JobCondition.InterruptForced);
        if (diner.CurJobDef != JobDefOf.Ingest)
        {
            throw new System.InvalidOperationException(
                "RimWorld rejected the exact nutrient-paste dispenser ingest job.");
        }

        Find.Selector.ClearSelection();
        Find.Selector.Select(diner);
        Find.TickManager.Pause();
        return diner.ThingID + "|" + dispenser.ThingID + "|" + job.loadID;
    }
    catch (System.Exception error)
    {
        Log.Error(
            "[ImmersiveChefsScenario] Nutrient-paste arming failed at " + stage +
            ": " + error.GetType().Name + ": " + error.Message);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
