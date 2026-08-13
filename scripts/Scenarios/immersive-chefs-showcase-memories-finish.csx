new System.Func<string>(() =>
{
    const string dinerName = "Nora Pike";
    var stage = "resolve completed ingestion";
    try
    {
        var map = Find.CurrentMap;
        Pawn diner = null;
        foreach (var pawn in map.mapPawns.AllPawnsSpawned)
        {
            if (pawn.LabelShort != dinerName) continue;
            if (diner != null) throw new System.InvalidOperationException("Multiple named diners remained.");
            diner = pawn;
        }
        if (diner == null) throw new System.InvalidOperationException("The named diner was absent.");
        var memoryDefs = new[]
        {
            "AteWithoutTable",
            "ImmersiveChefs_DiningExperience",
            "ImmersiveChefs_MealTemperature"
        };
        var memories = diner.needs.mood.thoughts.memories.Memories;
        foreach (var defName in memoryDefs)
        {
            var count = 0;
            foreach (var memory in memories)
            {
                if (memory.def.defName == defName) count++;
            }
            if (count != 1)
                throw new System.InvalidOperationException("Native ingestion has not produced exactly one " + defName + " memory.");
        }
        var diningMemory = memories.First(memory => memory.def.defName == "ImmersiveChefs_DiningExperience");
        var temperatureMemory = memories.First(memory => memory.def.defName == "ImmersiveChefs_MealTemperature");
        if (diningMemory.CurStageIndex != 1 || temperatureMemory.CurStageIndex != 3)
            throw new System.InvalidOperationException("Native ingestion produced the wrong dining or cold-food stage.");
        var returnedPlates = map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"))
            .OfType<ThingWithComps>()
            .ToArray();
        if (returnedPlates.Length != 1)
            throw new System.InvalidOperationException("The exact plate did not return once.");

        stage = "open native Thoughts pane";
        Find.TickManager.Pause();
        Find.Selector.ClearSelection();
        Find.Selector.Select(diner);
        if (InspectPaneUtility.OpenTab(typeof(ITab_Pawn_Needs)) == null)
            throw new System.InvalidOperationException("The native Needs tab did not open.");
        var roomCenter = returnedPlates[0].Position + new IntVec3(9, 0, 1);
        Find.CameraDriver.SetRootPosAndSize(roomCenter.ToVector3Shifted(), 17f);
        return diner.ThingID + "|" + returnedPlates[0].ThingID + "|native-ingestion-complete";
    }
    catch (System.Exception exception)
    {
        return "ERROR|" + stage + "|" + exception;
    }
})()
