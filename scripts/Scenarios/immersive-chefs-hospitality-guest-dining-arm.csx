new System.Func<string>(() =>
{
    var map = Find.CurrentMap;
    var guestNames = new[] { "Colony Cutlery Guest", "Personal Cutlery Guest" };
    var armed = new System.Collections.Generic.List<string>();
    foreach (var guestName in guestNames)
    {
        Pawn guest = null;
        foreach (var candidatePawn in map.mapPawns.AllPawnsSpawned)
        {
            if (candidatePawn.LabelShort == guestName)
            {
                guest = candidatePawn;
                break;
            }
        }

        if (guest == null)
        {
            throw new System.InvalidOperationException("Could not find " + guestName + ".");
        }

        Thing meal = null;
        var bestDistance = int.MaxValue;
        foreach (var candidateMeal in map.listerThings.ThingsOfDef(ThingDefOf.MealSimple))
        {
            var distance = candidateMeal.Position.DistanceToSquared(guest.Position);
            if (distance < bestDistance)
            {
                meal = candidateMeal;
                bestDistance = distance;
            }
        }

        if (meal == null)
        {
            throw new System.InvalidOperationException("Could not find a meal for " + guestName + ".");
        }
        guest.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced, false);
        guest.needs.food.CurLevel = 0.01f;
        var ingest = JobMaker.MakeJob(JobDefOf.Ingest, meal);
        guest.jobs.StartJob(ingest, Verse.AI.JobCondition.InterruptForced);
        armed.Add(guest.ThingID + ":" + meal.ThingID);
    }

    Find.TickManager.Pause();
    Find.Selector.ClearSelection();
    Find.Selector.Select(map.mapPawns.AllPawnsSpawned.Single(
        pawn => pawn.LabelShort == "Colony Cutlery Guest"));
    return string.Join("|", armed.ToArray());
})()
