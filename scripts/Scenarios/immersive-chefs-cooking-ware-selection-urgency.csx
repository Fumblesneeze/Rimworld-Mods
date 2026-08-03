new System.Func<string>(() =>
{
    var stage = "resolve starving requester";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException("The ware-selection fixture has no current map.");
        }

        var starvingPawn = map.mapPawns.AllPawnsSpawned
            .FirstOrDefault(pawn => pawn.LabelShort == "Starving Meal Requester");
        if (starvingPawn == null)
        {
            throw new System.InvalidOperationException("The sealed starving requester is unavailable.");
        }

        stage = "ask vanilla food giver for unreachable food";
        starvingPawn.needs.food.CurLevel = 0.01f;
        var tryGiveFood = HarmonyLib.AccessTools.Method(typeof(JobGiver_GetFood), "TryGiveJob");
        var unavailableFoodJob = tryGiveFood.Invoke(
            new JobGiver_GetFood(),
            new object[] { starvingPawn }) as Verse.AI.Job;
        if (unavailableFoodJob != null)
        {
            throw new System.InvalidOperationException(
                "The sealed starving pawn unexpectedly found reachable food.");
        }

        Find.TickManager.Pause();
        return starvingPawn.ThingID + "|" + starvingPawn.needs.food.CurLevelPercentage;
    }
    catch (System.Exception error)
    {
        Log.Error(
            "[ImmersiveChefsScenario] Cooking urgency setup failed at " + stage +
            ": " + error.GetType().Name + ": " + error.Message);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
