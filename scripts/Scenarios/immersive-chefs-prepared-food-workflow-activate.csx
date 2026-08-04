new System.Func<string>(() =>
{
    var map = Find.CurrentMap;
    if (map == null)
    {
        throw new System.InvalidOperationException("The prepared-food fixture has no map.");
    }

    var prepChef = map.mapPawns.AllPawnsSpawned
        .FirstOrDefault(pawn => pawn.LabelShort == "Prep Chef");
    var preparedCook = map.mapPawns.AllPawnsSpawned
        .FirstOrDefault(pawn => pawn.LabelShort == "Prepared Cook");
    var rawCook = map.mapPawns.AllPawnsSpawned
        .FirstOrDefault(pawn => pawn.LabelShort == "Raw Cook");
    if (prepChef == null || preparedCook == null || rawCook == null)
    {
        throw new System.InvalidOperationException("The prepared-food workers are unavailable.");
    }

    ImmersiveChefs.ImmersiveChefsMod.Settings.WareRequirementMode =
        ImmersiveChefs.WareRequirementMode.Strict;
    ImmersiveChefs.ImmersiveChefsMod.Settings.PreparedWorkReduction = 0.40f;
    ImmersiveChefs.ImmersiveChefsMod.Settings.AutoCallAssistants = false;
    prepChef.drafter.Drafted = false;

    var center = new IntVec3(
        (prepChef.Position.x + rawCook.Position.x) / 2,
        0,
        prepChef.Position.z + 2);
    Find.CameraDriver.SetRootPosAndSize(
        new UnityEngine.Vector3(center.x + 0.5f, 0f, center.z + 0.5f),
        25f);
    map.mapDrawer.RegenerateEverythingNow();
    Find.Selector.ClearSelection();
    Find.Selector.Select(prepChef);
    Find.Selector.Select(preparedCook);
    Find.Selector.Select(rawCook);
    Messages.Message(
        "Prepared-food workflow ready: resume time for the Prep Chef's two native prep bills. The matched cooks remain drafted until the prepared stack is inspected; undraft them together to compare prepared and raw Simple-meal work.",
        MessageTypeDefOf.NeutralEvent,
        false);
    Find.TickManager.Pause();
    return prepChef.ThingID + "|" + preparedCook.ThingID + "|" + rawCook.ThingID;
})()
