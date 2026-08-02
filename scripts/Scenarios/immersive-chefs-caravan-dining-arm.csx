new System.Func<string>(() =>
{
    const string scenarioName = "Immersive Chefs caravan dining scenario";
    var caravan = Find.WorldObjects.Caravans
        .Single(candidate => string.Equals(candidate.Name, scenarioName, System.StringComparison.Ordinal));
    var pawn = caravan.PawnsListForReading.Single(candidate => candidate.RaceProps.Humanlike);
    pawn.needs.food.CurLevel = 0.01f;
    caravan.RecacheInventory();
    Find.TickManager.Pause();
    return pawn.ThingID;
})()
