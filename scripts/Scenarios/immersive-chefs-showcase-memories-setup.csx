new System.Func<string>(() =>
{
    var stage = "resolve map";
    try
    {
        var map = Find.CurrentMap;
        if (map == null) throw new System.InvalidOperationException("A playable map is required.");
        Find.TickManager.Pause();

        stage = "resolve colonist";
        var diner = map.mapPawns.FreeColonistsSpawned
            .FirstOrDefault(pawn => pawn.needs != null && pawn.needs.mood != null &&
                pawn.needs.mood.thoughts != null && pawn.needs.mood.thoughts.memories != null);
        if (diner == null) throw new System.InvalidOperationException("No conscious colonist with memories was available.");

        stage = "add displayed memories";
        diner.needs.mood.thoughts.memories.TryGainMemory(DefDatabase<ThoughtDef>.GetNamed("AteWithoutTable"));
        diner.needs.mood.thoughts.memories.TryGainMemory(
            ThoughtMaker.MakeThought(DefDatabase<ThoughtDef>.GetNamed("ImmersiveChefs_MealTemperature"), 3));
        diner.needs.mood.thoughts.memories.TryGainMemory(
            ThoughtMaker.MakeThought(DefDatabase<ThoughtDef>.GetNamed("ImmersiveChefs_DiningExperience"), 1));

        stage = "frame colonist";
        Find.CameraDriver.SetRootPosAndSize(
            new UnityEngine.Vector3(diner.Position.x + 0.5f, 0f, diner.Position.z + 0.5f),
            12f);
        Find.Selector.ClearSelection();
        Find.Selector.Select(diner);

        stage = "open needs tab";
        var needsTab = typeof(ITab_Pawn_Needs);
        var openedTab = InspectPaneUtility.OpenTab(needsTab);
        if (openedTab == null || openedTab.GetType() != needsTab)
            throw new System.InvalidOperationException("The native Needs tab did not open.");

        return diner.ThingID + "|" + diner.LabelShort;
    }
    catch (System.Exception exception)
    {
        throw new System.InvalidOperationException(
            "Memories showcase setup failed during '" + stage + "': " + exception.Message,
            exception);
    }
})()
