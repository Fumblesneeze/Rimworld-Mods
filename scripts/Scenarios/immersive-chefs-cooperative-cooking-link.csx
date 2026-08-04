new System.Func<string>(() =>
{
    var map = Find.CurrentMap;
    if (map == null)
    {
        throw new System.InvalidOperationException("The cooperative-cooking fixture has no map.");
    }

    var assistedLead = map.mapPawns.AllPawnsSpawned
        .FirstOrDefault(pawn => pawn.LabelShort == "Assisted Lead");
    if (assistedLead == null)
    {
        throw new System.InvalidOperationException("The assisted lead is unavailable.");
    }

    var assistedCenter = new IntVec3(assistedLead.Position.x + 5, 0, assistedLead.Position.z + 2);
    var conduitDef = DefDatabase<ThingDef>.GetNamed("PowerConduit");
    for (var x = assistedCenter.x; x <= assistedCenter.x + 7; x++)
    {
        var conduit = ThingMaker.MakeThing(conduitDef);
        conduit.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(conduit, new IntVec3(x, 0, assistedCenter.z + 2), map);
    }
    for (var z = assistedCenter.z + 3; z <= assistedCenter.z + 5; z++)
    {
        var conduit = ThingMaker.MakeThing(conduitDef);
        conduit.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(conduit, new IntVec3(assistedCenter.x + 6, 0, z), map);
    }

    var station = ThingMaker.MakeThing(
        DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_SauceStation"));
    station.SetFactionDirect(Faction.OfPlayer);
    GenSpawn.Spawn(
        station,
        new IntVec3(assistedCenter.x + 3, 0, assistedCenter.z + 2),
        map,
        Rot4.North);
    var battery = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Battery"));
    battery.SetFactionDirect(Faction.OfPlayer);
    GenSpawn.Spawn(
        battery,
        new IntVec3(assistedCenter.x + 6, 0, assistedCenter.z + 5),
        map,
        Rot4.North);
    battery.TryGetComp<CompPowerBattery>().SetStoredEnergyPct(1f);
    map.powerNetManager.UpdatePowerNetsAndConnections_First();
    var stationPower = station.TryGetComp<CompPowerTrader>();
    for (var tick = 0; tick <= 200 && !stationPower.PowerOn; tick++)
    {
        Find.TickManager.DoSingleTick();
    }
    if (!stationPower.PowerOn)
    {
        throw new System.InvalidOperationException(
            "The sauce station did not become powered on its charged fixture net.");
    }
    return station.ThingID + "|" + battery.ThingID;
})()
