new System.Func<string>(() =>
{
    const string scenarioName = "Cutlery-free colonist diner";
    var pawn = Find.CurrentMap.mapPawns.AllPawnsSpawned
        .Single(candidate => string.Equals(candidate.LabelShort, scenarioName, System.StringComparison.Ordinal));
    pawn.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced, false);
    pawn.needs.food.CurLevel = 0.01f;
    Find.Selector.ClearSelection();
    Find.Selector.Select(pawn);
    Find.TickManager.Pause();
    return pawn.ThingID;
})()
