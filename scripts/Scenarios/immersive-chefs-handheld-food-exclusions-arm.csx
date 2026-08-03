new System.Func<string>(() =>
{
    var stage = "resolve diners";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException("The handheld-food fixture has no map.");
        }

        Pawn pemmicanDiner = null;
        Pawn survivalDiner = null;
        foreach (var pawn in map.mapPawns.AllPawnsSpawned)
        {
            if (pawn.LabelShort == "Pemmican Handheld Diner") pemmicanDiner = pawn;
            else if (pawn.LabelShort == "Survival Meal Handheld Diner") survivalDiner = pawn;
        }
        if (pemmicanDiner == null || survivalDiner == null)
        {
            throw new System.InvalidOperationException("The exact two handheld-food diners are unavailable.");
        }

        stage = "resolve vanilla food-choice boundary";
        var tryGiveFood = HarmonyLib.AccessTools.Method(typeof(JobGiver_GetFood), "TryGiveJob");
        if (tryGiveFood == null)
        {
            throw new System.InvalidOperationException("Vanilla JobGiver_GetFood.TryGiveJob is unavailable.");
        }

        var diners = new[] { pemmicanDiner, survivalDiner };
        var expectedFoods = new[] { ThingDefOf.Pemmican, ThingDefOf.MealSurvivalPack };
        var jobIds = new System.Collections.Generic.List<int>();
        for (var index = 0; index < diners.Length; index++)
        {
            var diner = diners[index];
            stage = "choose " + expectedFoods[index].defName;
            Thing expectedFood = null;
            var nearbyFoodCount = 0;
            foreach (var candidate in map.listerThings.ThingsOfDef(expectedFoods[index]))
            {
                if (candidate.Spawned &&
                    candidate.Position.DistanceToSquared(diner.Position) <= 25)
                {
                    expectedFood = candidate;
                    nearbyFoodCount++;
                }
            }

            if (nearbyFoodCount != 1)
            {
                throw new System.InvalidOperationException(
                    "Expected exactly one nearby " + expectedFoods[index].defName +
                    " fixture, found " + nearbyFoodCount + ".");
            }
            diner.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced, false);
            diner.needs.food.CurLevel = 0.01f;
            var job = (Verse.AI.Job)tryGiveFood.Invoke(
                new JobGiver_GetFood(),
                new object[] { diner });
            if (job == null || job.def != JobDefOf.Ingest ||
                !System.Object.ReferenceEquals(job.targetA.Thing, expectedFood))
            {
                throw new System.InvalidOperationException(
                    "Vanilla did not choose the exact " + expectedFood.ThingID + " ingest job.");
            }

            job.playerForced = true;
            diner.jobs.StartJob(job, Verse.AI.JobCondition.InterruptForced);
            if (diner.CurJobDef != JobDefOf.Ingest)
            {
                throw new System.InvalidOperationException(
                    "RimWorld rejected the exact " + expectedFoods[index].defName + " ingest job.");
            }
            jobIds.Add(job.loadID);
        }

        Find.Selector.ClearSelection();
        Find.Selector.Select(pemmicanDiner);
        Find.Selector.Select(survivalDiner);
        Find.TickManager.Pause();
        return pemmicanDiner.ThingID + "|" + jobIds[0] + "|" +
               survivalDiner.ThingID + "|" + jobIds[1];
    }
    catch (System.Exception error)
    {
        Log.Error(
            "[ImmersiveChefsScenario] Handheld-food arming failed at " + stage +
            ": " + error.GetType().Name + ": " + error.Message);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
