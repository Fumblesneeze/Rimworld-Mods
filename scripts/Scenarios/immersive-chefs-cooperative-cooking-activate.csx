new System.Func<string>(() =>
{
    var map = Find.CurrentMap;
    if (map == null)
    {
        throw new System.InvalidOperationException("The cooperative-cooking fixture has no map.");
    }

    var assistedLead = map.mapPawns.AllPawnsSpawned
        .FirstOrDefault(pawn => pawn.LabelShort == "Assisted Lead");
    var assistedSpecialist = map.mapPawns.AllPawnsSpawned
        .FirstOrDefault(pawn => pawn.LabelShort == "Assisted Specialist");
    var controlLead = map.mapPawns.AllPawnsSpawned
        .FirstOrDefault(pawn => pawn.LabelShort == "Control Lead");
    if (assistedLead == null || assistedSpecialist == null || controlLead == null)
    {
        throw new System.InvalidOperationException("The comparison cooks are unavailable.");
    }

    Thing station = null;
    var stationDistance = int.MaxValue;
    foreach (var candidateStation in map.listerThings
                 .ThingsOfDef(DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_SauceStation")))
    {
        var candidateDistance = candidateStation.Position.DistanceToSquared(assistedLead.Position);
        if (candidateDistance < stationDistance)
        {
            station = candidateStation;
            stationDistance = candidateDistance;
        }
    }
    Thing assistedStove = null;
    foreach (var candidateStove in map.listerThings
                 .ThingsOfDef(DefDatabase<ThingDef>.GetNamed("FueledStove")))
    {
        var candidateAffected = candidateStove.TryGetComp<CompAffectedByFacilities>();
        if (candidateAffected != null &&
            candidateAffected.LinkedFacilitiesListForReading.Contains(station))
        {
            assistedStove = candidateStove;
            break;
        }
    }
    if (assistedStove == null || station == null)
    {
        throw new System.InvalidOperationException("The assisted kitchen is unavailable.");
    }

    var affected = assistedStove.TryGetComp<CompAffectedByFacilities>();
    if (affected == null || !affected.LinkedFacilitiesListForReading.Contains(station))
    {
        throw new System.InvalidOperationException(
            "The spawned sauce station did not link to the assisted stove.");
    }
    var power = station.TryGetComp<CompPowerTrader>();
    var flick = station.TryGetComp<CompFlickable>();
    if (power == null || power.PowerNet == null || !power.PowerOn ||
        flick == null || !flick.SwitchIsOn)
    {
        throw new System.InvalidOperationException(
            "The sauce station is not connected to the active fixture power net.");
    }

    ImmersiveChefs.ImmersiveChefsMod.Settings.WareRequirementMode =
        ImmersiveChefs.WareRequirementMode.Strict;
    ImmersiveChefs.ImmersiveChefsMod.Settings.AutoCallAssistants = true;
    ImmersiveChefs.ImmersiveChefsMod.Settings.MaximumAssistants = 1;
    ImmersiveChefs.ImmersiveChefsMod.Settings.AssistantEffectScale = 3f;
    foreach (var pawn in new[] { assistedLead, assistedSpecialist, controlLead })
    {
        pawn.drafter.Drafted = false;
    }

    var center = new IntVec3(
        (assistedLead.Position.x + controlLead.Position.x) / 2,
        0,
        assistedLead.Position.z + 2);
    Find.CameraDriver.SetRootPosAndSize(
        new UnityEngine.Vector3(center.x + 0.5f, 0f, center.z + 0.5f),
        25f);
    map.mapDrawer.RegenerateEverythingNow();
    Find.Selector.ClearSelection();
    Find.Selector.Select(assistedLead);
    Find.Selector.Select(assistedSpecialist);
    Find.Selector.Select(controlLead);
    Messages.Message(
        "Cooperative cooking ready: resume normal time. The specialist should man the linked sauce station while the assisted lead collects ware and rice, then overlap cooking and help that meal finish first with higher culinary quality.",
        MessageTypeDefOf.NeutralEvent,
        false);
    Find.TickManager.Pause();
    return assistedLead.ThingID + "|" + assistedSpecialist.ThingID + "|" +
           controlLead.ThingID + "|" + assistedStove.ThingID + "|" + station.ThingID;
})()
