new System.Func<string>(() =>
{
    var stage = "resolve map";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The glitterworld trade scenario requires a playable map.");
        }

        Find.TickManager.Pause();
        stage = "find clear fixture area";
        var center = IntVec3.Invalid;
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 55f, true))
        {
            var fixture = new CellRect(candidate.x - 10, candidate.z - 7, 21, 15);
            var valid = true;
            foreach (var cell in fixture.Cells)
            {
                if (!cell.InBounds(map) || cell.GetEdifice(map) != null || cell.GetFirstPawn(map) != null)
                {
                    valid = false;
                    break;
                }
            }

            if (valid)
            {
                center = candidate;
                break;
            }
        }

        if (!center.IsValid)
        {
            throw new System.InvalidOperationException(
                "Could not find a clear area for the glitterworld trade fixture.");
        }

        var fixtureRoom = new CellRect(center.x - 10, center.z - 7, 21, 15);
        foreach (var cell in fixtureRoom.Cells)
        {
            var existing = cell.GetThingList(map).ToList();
            foreach (var thing in existing)
            {
                if (thing.def.category == ThingCategory.Item ||
                    thing.def.category == ThingCategory.Plant ||
                    thing.def.category == ThingCategory.Filth)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }

            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
        }

        stage = "create capable negotiator";
        Pawn negotiator = null;
        for (var attempt = 0; attempt < 32 && negotiator == null; attempt++)
        {
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                forceNoGear: true));
            if (pawn.health.capacities.CapableOf(PawnCapacityDefOf.Talking) &&
                pawn.health.capacities.CapableOf(PawnCapacityDefOf.Hearing) &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Talking) >= 0.99f &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Hearing) >= 0.99f)
            {
                negotiator = pawn;
                negotiator.Name = new NameSingle("Glitter Cookware Buyer");
                negotiator.skills.GetSkill(SkillDefOf.Social).Level = 20;
            }
            else
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }

        if (negotiator == null)
        {
            throw new System.InvalidOperationException("Could not generate a capable trade negotiator.");
        }

        GenSpawn.Spawn(negotiator, new IntVec3(center.x, 0, center.z - 3), map);

        stage = "create powered orbital trade fixtures";
        var conduitDef = DefDatabase<ThingDef>.GetNamed("PowerConduit");
        for (var x = center.x - 6; x <= center.x + 6; x++)
        {
            var conduit = ThingMaker.MakeThing(conduitDef);
            conduit.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(conduit, new IntVec3(x, 0, center.z + 3), map);
        }

        var batteryDef = DefDatabase<ThingDef>.GetNamed("Battery");
        var battery = ThingMaker.MakeThing(
            batteryDef,
            batteryDef.MadeFromStuff ? ThingDefOf.Steel : null);
        battery.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(battery, new IntVec3(center.x + 6, 0, center.z + 3), map, Rot4.North);
        var batteryComp = battery.TryGetComp<CompPowerBattery>();
        batteryComp.SetStoredEnergyPct(1f);

        var consoleDef = DefDatabase<ThingDef>.GetNamed("CommsConsole");
        var console = ThingMaker.MakeThing(
            consoleDef,
            consoleDef.MadeFromStuff ? ThingDefOf.Steel : null);
        console.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(console, new IntVec3(center.x - 4, 0, center.z + 3), map, Rot4.North);

        var beaconDef = DefDatabase<ThingDef>.GetNamed("OrbitalTradeBeacon");
        var beacon = (Building_OrbitalTradeBeacon)ThingMaker.MakeThing(
            beaconDef,
            beaconDef.MadeFromStuff ? ThingDefOf.Steel : null);
        beacon.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(beacon, new IntVec3(center.x + 2, 0, center.z + 3), map, Rot4.North);

        foreach (var poweredFixture in new ThingWithComps[] { (ThingWithComps)console, beacon })
        {
            var flick = poweredFixture.GetComp<CompFlickable>();
            if (flick != null && !flick.SwitchIsOn)
            {
                flick.DoFlick();
            }
        }

        map.powerNetManager.UpdatePowerNetsAndConnections_First();
        for (var tick = 0; tick <= 200 &&
             (!console.TryGetComp<CompPowerTrader>().PowerOn ||
              !beacon.TryGetComp<CompPowerTrader>().PowerOn); tick++)
        {
            Find.TickManager.DoSingleTick();
        }

        if (!console.TryGetComp<CompPowerTrader>().PowerOn ||
            !beacon.TryGetComp<CompPowerTrader>().PowerOn)
        {
            throw new System.InvalidOperationException(
                "The comms console and orbital trade beacon did not join the charged power net.");
        }

        stage = "frame native trade fixture";
        Find.CameraDriver.JumpToCurrentMapLoc(center);
        Find.CameraDriver.SetRootSize(18f);
        Find.Selector.ClearSelection();
        Find.Selector.Select(console);
        Find.TickManager.Pause();
        return negotiator.ThingID + "|" + console.ThingID + "|" + beacon.ThingID;
    }
    catch (System.Exception error)
    {
        Log.Error(
            "[ImmersiveChefsScenario] Glitterworld trade setup failed at " + stage +
            ": " + error.GetType().Name + ": " + error.Message);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
