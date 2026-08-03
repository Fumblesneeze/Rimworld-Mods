new System.Func<string>(() =>
{
    var map = Find.CurrentMap;
    Pawn child = null;
    foreach (var pawn in map.mapPawns.AllPawnsSpawned)
    {
        if (pawn.LabelShort == "Independent Child Diner")
        {
            child = pawn;
            break;
        }
    }

    if (child == null || child.DevelopmentalStage != DevelopmentalStage.Child)
    {
        throw new System.InvalidOperationException("The independent child fixture pawn is unavailable.");
    }

    var expectedMealCell = new IntVec3(child.Position.x + 4, 0, child.Position.z);
    var fixtureMeals = expectedMealCell.GetThingList(map)
        .Where(candidate =>
            candidate.def == DefDatabase<ThingDef>.GetNamed("MealLavish") &&
            !candidate.Destroyed)
        .ToList();
    if (fixtureMeals.Count != 1)
    {
        throw new System.InvalidOperationException(
            "Expected exactly one plated lavish meal at the fixture cell, found " +
            fixtureMeals.Count + ".");
    }

    var meal = fixtureMeals[0];

    child.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced, false);
    child.needs.food.CurLevelPercentage = 0.15f;
    var ingest = JobMaker.MakeJob(JobDefOf.Ingest, meal);
    ingest.count = 1;
    ingest.playerForced = true;
    child.jobs.StartJob(ingest, Verse.AI.JobCondition.InterruptForced);
    if (child.CurJobDef != JobDefOf.Ingest)
    {
        throw new System.InvalidOperationException("Vanilla rejected the child's ordinary ingest job.");
    }

    Find.Selector.ClearSelection();
    Find.Selector.Select(child);
    Find.TickManager.Pause();
    return child.ThingID + "|" + meal.ThingID + "|" + ingest.loadID;
})()
