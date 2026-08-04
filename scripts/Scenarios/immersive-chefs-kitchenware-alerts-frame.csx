new System.Func<string>(() =>
{
    const string contextKey = "ImmersiveChefs.KitchenwareAlertScenario";
    var context = AppDomain.CurrentDomain.GetData(contextKey) as
        System.Collections.Generic.Dictionary<string, object>;
    if (context == null)
    {
        throw new System.InvalidOperationException("The kitchenware-alert fixture context is missing.");
    }

    var map = (Map)context["map"];
    var center = (IntVec3)context["center"];
    var cookware = (Thing)context["cookware"];
    var plate = (Thing)context["plate"];
    map.mapDrawer.RegenerateEverythingNow();
    Find.CameraDriver.SetRootPosAndSize(center.ToVector3Shifted(), 15f);
    Find.Selector.ClearSelection();
    Find.Selector.Select(cookware);
    Find.Selector.Select(plate);
    Messages.Message(
        "Alert fixture ready: the dirty forbidden ware counts as owned, so no alert is expected. Destroy both with native dev actions to reveal Missing kitchenware on the fueled stove; drafting the only active cook must then suppress it. The campfire and berries remain silent controls.",
        MessageTypeDefOf.NeutralEvent,
        false);
    return cookware.ThingID + "|" + plate.ThingID;
})()
