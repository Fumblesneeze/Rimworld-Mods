new System.Func<string>(() =>
{
    const string dinerName = "Nora Pike";
    var stage = "resolve passive fixture";
    try
    {
        var map = Find.CurrentMap;
        Pawn diner = null;
        foreach (var pawn in map.mapPawns.AllPawnsSpawned)
        {
            if (pawn.LabelShort != dinerName) continue;
            if (diner != null) return "ERROR|multiple named diners";
            diner = pawn;
        }
        if (diner == null) return "ERROR|named diner absent";
        Thing meal = null;
        foreach (var candidate in map.listerThings.ThingsOfDef(ThingDefOf.MealSimple))
        {
            if (candidate.Position.DistanceToSquared(diner.Position) > 4) continue;
            if (meal != null) return "ERROR|multiple nearby meals";
            meal = candidate;
        }
        if (meal == null) return "ERROR|nearby meal absent";
        if (!diner.Drafted || diner.CurJobDef == JobDefOf.Ingest)
            throw new System.InvalidOperationException("The diner was not a passive drafted fixture.");

        stage = "choose native Consume option";
        RimWorld.FloatMenuContext menuContext;
        var options = FloatMenuMakerMap.GetOptions(
            new System.Collections.Generic.List<Pawn> { diner },
            meal.Position.ToVector3Shifted(),
            out menuContext);
        FloatMenuOption consume = null;
        foreach (var option in options)
        {
            if (!option.Disabled && option.Label.StartsWith("Consume", System.StringComparison.OrdinalIgnoreCase))
            {
                if (consume != null)
                    throw new System.InvalidOperationException("Multiple enabled native Consume options were offered.");
                consume = option;
            }
        }
        if (consume == null)
            throw new System.InvalidOperationException("No enabled native Consume option was offered.");
        diner.drafter.Drafted = false;
        consume.Chosen(true, null);
        if (diner.CurJobDef != JobDefOf.Ingest)
            throw new System.InvalidOperationException("The native Consume callback did not start ingestion.");

        Find.TickManager.CurTimeSpeed = TimeSpeed.Superfast;
        return diner.ThingID + "|" + meal.ThingID + "|" + diner.CurJob.loadID;
    }
    catch (System.Exception exception)
    {
        return "ERROR|" + stage + "|" + exception;
    }
})()
