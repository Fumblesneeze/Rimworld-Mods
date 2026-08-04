new System.Func<string>(() =>
{
    const string contextKey = "ImmersiveChefs.TerminalPlateScenario";
    var context = AppDomain.CurrentDomain.GetData(contextKey) as
        System.Collections.Generic.Dictionary<string, object>;
    if (context == null || !context.ContainsKey("meals"))
    {
        throw new System.InvalidOperationException("The terminal plate meal context is missing.");
    }

    var map = (Map)context["map"];
    var mealCells = (System.Collections.Generic.List<IntVec3>)context["mealCells"];
    var meals = (System.Collections.Generic.List<ThingWithComps>)context["meals"];
    var center = new IntVec3(
        (mealCells[0].x + mealCells[1].x + mealCells[2].x) / 3,
        0,
        (mealCells[0].z + mealCells[1].z + mealCells[2].z) / 3);
    map.mapDrawer.RegenerateEverythingNow();
    map.roofGrid.Drawer.SetDirty();
    Find.Selector.ClearSelection();
    Find.Selector.Select(meals[0]);
    Find.CameraDriver.SetRootPosAndSize(center.ToVector3Shifted(), 20f);
    Messages.Message(
        "Terminal plate fixture ready: let the selected meal expire, then start real fires on the other two meals.",
        MessageTypeDefOf.NeutralEvent,
        false);
    return meals[0].ThingID + "|" + meals[1].ThingID + "|" + meals[2].ThingID;
})()
