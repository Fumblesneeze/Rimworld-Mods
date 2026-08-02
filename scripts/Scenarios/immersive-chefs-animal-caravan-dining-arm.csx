new System.Func<string>(() =>
{
    const string scenarioName = "Immersive Chefs animal caravan dining scenario";
    var caravan = Find.WorldObjects.Caravans
        .Single(candidate => string.Equals(candidate.Name, scenarioName, System.StringComparison.Ordinal));
    var escort = caravan.PawnsListForReading.Single(candidate => candidate.RaceProps.Humanlike);
    var animal = caravan.PawnsListForReading.Single(candidate => !candidate.RaceProps.Humanlike);
    escort.needs.food.CurLevelPercentage = 1f;
    animal.needs.food.CurLevel = 0.01f;
    caravan.RecacheInventory();
    Find.TickManager.Pause();
    return animal.ThingID;
})()
