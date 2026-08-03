new System.Func<string>(() =>
{
    var map = Find.CurrentMap;
    var selected = Find.Selector.SingleSelectedThing;
    if (map == null || selected == null ||
        selected.def.defName != "ImmersiveChefs_Plate" ||
        !selected.IsForbidden(Faction.OfPlayer))
    {
        throw new System.InvalidOperationException(
            "The storage scenario did not retain its selected forbidden plate.");
    }

    var center = new IntVec3(selected.Position.x - 1, 0, selected.Position.z + 3);
    Find.CameraDriver.SetRootPosAndSize(
        new UnityEngine.Vector3(center.x + 0.5f, 0f, center.z + 0.5f),
        10f);
    map.mapDrawer.RegenerateEverythingNow();
    return center.ToString();
})()
