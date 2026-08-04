new System.Func<string>(() =>
{
    var map = Find.CurrentMap;
    if (map == null)
    {
        throw new System.InvalidOperationException(
            "The hidden-provenance fixture has no map.");
    }

    var cook = map.mapPawns.AllPawnsSpawned
        .FirstOrDefault(pawn => pawn.LabelShort == "Hidden Bill Cook");
    var diner = map.mapPawns.AllPawnsSpawned
        .FirstOrDefault(pawn => pawn.LabelShort == "Hidden Policy Diner");
    if (cook == null || diner == null)
    {
        throw new System.InvalidOperationException(
            "The hidden-provenance fixture pawns are unavailable.");
    }

    ImmersiveChefs.ImmersiveChefsMod.Settings.WareRequirementMode =
        ImmersiveChefs.WareRequirementMode.Off;
    ImmersiveChefs.ImmersiveChefsMod.Settings.AutoCallAssistants = false;
    cook.drafter.Drafted = false;
    diner.drafter.Drafted = false;
    Find.Selector.ClearSelection();
    Find.Selector.Select(cook);
    Find.Selector.Select(diner);
    Messages.Message(
        "Hidden provenance ready and paused. In each room the closer left stack contains forbidden hidden human meat; the farther right stack contains allowed hidden rice. Resume time: the cook and diner should both skip the closer forbidden stack.",
        MessageTypeDefOf.NeutralEvent,
        false);
    Find.TickManager.Pause();
    return cook.ThingID + "|" + diner.ThingID;
})()
