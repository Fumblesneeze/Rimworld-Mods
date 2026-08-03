new System.Func<string>(() =>
{
    var map = Find.CurrentMap;
    Pawn feeder = null;
    Pawn patient = null;
    foreach (var pawn in map.mapPawns.AllPawnsSpawned)
    {
        if (pawn.LabelShort == "Conscious Nurse") feeder = pawn;
        if (pawn.LabelShort == "Conscious Patient") patient = pawn;
    }
    Thing meal = null;
    var mealDistance = int.MaxValue;
    foreach (var candidate in map.listerThings.ThingsOfDef(ThingDefOf.MealSimple))
    {
        var distance = candidate.Position.DistanceToSquared(feeder.Position);
        if (!candidate.Destroyed && distance < mealDistance)
        {
            meal = candidate;
            mealDistance = distance;
        }
    }
    feeder.jobs.EndCurrentJob(Verse.AI.JobCondition.InterruptForced, false);
    patient.needs.food.CurLevel = 0.01f;
    var feed = JobMaker.MakeJob(JobDefOf.FeedPatient, meal, patient);
    feed.count = 1;
    feeder.jobs.StartJob(feed, Verse.AI.JobCondition.InterruptForced);
    Find.TickManager.Pause();
    return feeder.ThingID + ":" + patient.ThingID + ":" + meal.ThingID;
})()
