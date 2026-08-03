new System.Func<string>(() =>
{
    var stage = "resolve matched cooks";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException("The recipe-complexity fixture has no map.");
        }

        Pawn simpleCook = null;
        Pawn fineCook = null;
        Pawn lavishCook = null;
        foreach (var pawn in map.mapPawns.AllPawnsSpawned)
        {
            if (pawn.LabelShort == "Simple Recipe Cook") simpleCook = pawn;
            else if (pawn.LabelShort == "Fine Recipe Cook") fineCook = pawn;
            else if (pawn.LabelShort == "Lavish Recipe Cook") lavishCook = pawn;
        }
        if (simpleCook == null || fineCook == null || lavishCook == null)
        {
            throw new System.InvalidOperationException("The exact three matched cooks are unavailable.");
        }

        stage = "resolve finalized bill work giver";
        WorkGiver_DoBill workGiver = null;
        foreach (var def in DefDatabase<WorkGiverDef>.AllDefsListForReading)
        {
            if (def.giverClass == typeof(WorkGiver_DoBill) &&
                def.fixedBillGiverDefs != null &&
                def.fixedBillGiverDefs.Any(thingDef => thingDef.defName == "FueledStove"))
            {
                workGiver = (WorkGiver_DoBill)def.Worker;
                break;
            }
        }
        if (workGiver == null)
        {
            throw new System.InvalidOperationException("No finalized fueled-stove bill work giver exists.");
        }

        var cooks = new[] { simpleCook, fineCook, lavishCook };
        var expectedRecipes = new[] { "CookMealSimple", "CookMealFine", "CookMealLavish" };
        var jobIds = new System.Collections.Generic.List<int>();
        foreach (var cook in cooks)
        {
            Thing nearestStove = null;
            var nearestDistance = int.MaxValue;
            foreach (var stove in map.listerThings
                         .ThingsOfDef(DefDatabase<ThingDef>.GetNamed("FueledStove")))
            {
                var distance = stove.Position.DistanceToSquared(cook.Position);
                if (distance < nearestDistance)
                {
                    nearestStove = stove;
                    nearestDistance = distance;
                }
            }

            var index = System.Array.IndexOf(cooks, cook);
            stage = "arm " + expectedRecipes[index];
            cook.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced, false);
            var job = nearestStove == null ? null : workGiver.JobOnThing(cook, nearestStove, true);
            if (job == null || job.def != JobDefOf.DoBill ||
                job.RecipeDef == null || job.RecipeDef.defName != expectedRecipes[index])
            {
                throw new System.InvalidOperationException(
                    "Vanilla could not create the exact " + expectedRecipes[index] + " bill job.");
            }

            job.playerForced = true;
            cook.jobs.StartJob(job, Verse.AI.JobCondition.InterruptForced);
            if (cook.CurJobDef != JobDefOf.DoBill)
            {
                throw new System.InvalidOperationException(
                    "RimWorld rejected the exact " + expectedRecipes[index] + " bill job.");
            }
            jobIds.Add(job.loadID);
        }

        Find.Selector.ClearSelection();
        Find.Selector.Select(simpleCook);
        Find.Selector.Select(fineCook);
        Find.Selector.Select(lavishCook);
        Find.TickManager.Pause();
        return simpleCook.ThingID + "|" + jobIds[0] + "|" +
               fineCook.ThingID + "|" + jobIds[1] + "|" +
               lavishCook.ThingID + "|" + jobIds[2];
    }
    catch (System.Exception error)
    {
        Log.Error(
            "[ImmersiveChefsScenario] Recipe-complexity arming failed at " + stage +
            ": " + error.GetType().Name + ": " + error.Message);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
