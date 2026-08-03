new System.Func<string>(() =>
{
    const string scenarioName = "Forkless wild map diner";
    var animal = Find.CurrentMap.mapPawns.AllPawnsSpawned
        .Single(candidate => string.Equals(candidate.LabelShort, scenarioName, System.StringComparison.Ordinal));
    animal.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced, false);
    animal.needs.food.CurLevel = 0.01f;
    Find.Selector.ClearSelection();
    Find.Selector.Select(animal);
    Find.TickManager.Pause();
    return animal.ThingID;
})()
