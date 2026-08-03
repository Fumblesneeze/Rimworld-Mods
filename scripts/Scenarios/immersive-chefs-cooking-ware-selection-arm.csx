new System.Func<string>(() =>
{
    var stage = "resolve exact cooks";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The cooking ware-selection fixture has no current map.");
        }

        Pawn cleanCook = null;
        Pawn urgentCook = null;
        foreach (var pawn in map.mapPawns.AllPawnsSpawned)
        {
            if (pawn.LabelShort == "Clean Ware Cook")
            {
                cleanCook = pawn;
            }
            else if (pawn.LabelShort == "Urgent Dirty Ware Cook")
            {
                urgentCook = pawn;
            }
        }

        if (cleanCook == null || urgentCook == null)
        {
            throw new System.InvalidOperationException("The exact two cooking workers are unavailable.");
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
            throw new System.InvalidOperationException(
                "No finalized WorkGiver_DoBill owns the fueled stove.");
        }

        Thing cleanStove = null;
        Thing urgentStove = null;
        var cleanDistance = int.MaxValue;
        var urgentDistance = int.MaxValue;
        foreach (var thing in map.listerThings
                     .ThingsOfDef(DefDatabase<ThingDef>.GetNamed("FueledStove")))
        {
            var distanceToClean = thing.Position.DistanceToSquared(cleanCook.Position);
            if (distanceToClean < cleanDistance)
            {
                cleanStove = thing;
                cleanDistance = distanceToClean;
            }

            var distanceToUrgent = thing.Position.DistanceToSquared(urgentCook.Position);
            if (distanceToUrgent < urgentDistance)
            {
                urgentStove = thing;
                urgentDistance = distanceToUrgent;
            }
        }

        if (cleanStove == null || urgentStove == null ||
            cleanDistance > 25 || urgentDistance > 25 ||
            System.Object.ReferenceEquals(cleanStove, urgentStove))
        {
            throw new System.InvalidOperationException(
                "The two exact fueled stoves are unavailable.");
        }

        stage = "create clean-preference native bill job";
        cleanCook.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced, false);
        var cleanJob = workGiver.JobOnThing(cleanCook, cleanStove, true);
        if (cleanJob == null || cleanJob.def != JobDefOf.DoBill ||
            cleanJob.RecipeDef == null || cleanJob.RecipeDef.defName != "CookMealSimple")
        {
            throw new System.InvalidOperationException(
                "Vanilla could not create the clean-preference simple-meal bill.");
        }
        cleanJob.playerForced = true;
        cleanCook.jobs.StartJob(cleanJob, Verse.AI.JobCondition.InterruptForced);

        stage = "create urgent-dirty-fallback native bill job";
        urgentCook.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced, false);
        var urgentJob = workGiver.JobOnThing(urgentCook, urgentStove, true);
        if (urgentJob == null || urgentJob.def != JobDefOf.DoBill ||
            urgentJob.RecipeDef == null || urgentJob.RecipeDef.defName != "CookMealSimple")
        {
            throw new System.InvalidOperationException(
                "Vanilla could not create the urgent dirty-fallback simple-meal bill.");
        }
        urgentJob.playerForced = true;
        urgentCook.jobs.StartJob(urgentJob, Verse.AI.JobCondition.InterruptForced);

        if (cleanCook.CurJobDef != JobDefOf.DoBill || urgentCook.CurJobDef != JobDefOf.DoBill)
        {
            throw new System.InvalidOperationException("RimWorld rejected one of the native cooking jobs.");
        }

        Find.Selector.ClearSelection();
        Find.Selector.Select(cleanCook);
        Find.Selector.Select(urgentCook);
        Find.TickManager.Pause();
        return cleanCook.ThingID + "|" + cleanJob.loadID + "|" +
               urgentCook.ThingID + "|" + urgentJob.loadID;
    }
    catch (System.Exception error)
    {
        Log.Error(
            "[ImmersiveChefsScenario] Cooking bill arming failed at " + stage +
            ": " + error.GetType().Name + ": " + error.Message);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
