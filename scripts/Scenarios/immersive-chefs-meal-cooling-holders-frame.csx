new System.Func<string>(() =>
{
    const string contextKey = "ImmersiveChefs.MealCoolingHolderScenario";
    var stage = "resolve completed context";
    try
    {
        var context = AppDomain.CurrentDomain.GetData(contextKey) as
            System.Collections.Generic.Dictionary<string, object>;
        if (context == null)
        {
            throw new System.InvalidOperationException("The completed holder context is missing.");
        }

        var center = (IntVec3)context["center"];
        var meals = (System.Collections.Generic.List<ThingWithComps>)context["meals"];
        var plates = (System.Collections.Generic.List<ThingWithComps>)context["plates"];
        var battery = (Thing)context["battery"];
        var controlNames = (string[])context["controlNames"];
        stage = "frame paused controls";
        Find.TickManager.Pause();
        Find.CameraDriver.SetRootPosAndSize(center.ToVector3Shifted(), 27f);
        Find.Selector.ClearSelection();
        Find.Selector.Select(meals[0]);

        var ids = new System.Collections.Generic.List<string>();
        for (var index = 0; index < meals.Count; index++)
        {
            ids.Add(meals[index].ThingID);
            ids.Add(plates[index].ThingID);
        }

        AppDomain.CurrentDomain.SetData(contextKey, null);
        return string.Join("|", ids.ToArray()) + "|" + battery.ThingID + "|" +
               string.Join(",", controlNames);
    }
    catch (System.Exception error)
    {
        AppDomain.CurrentDomain.SetData(contextKey, null);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
