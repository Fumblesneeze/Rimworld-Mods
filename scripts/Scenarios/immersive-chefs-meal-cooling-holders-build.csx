new System.Func<string>(() =>
{
    const string contextKey = "ImmersiveChefs.MealCoolingHolderScenario";
    var stage = "resolve meal context";
    try
    {
        var context = AppDomain.CurrentDomain.GetData(contextKey) as
            System.Collections.Generic.Dictionary<string, object>;
        if (context == null)
        {
            throw new System.InvalidOperationException("The meal holder context is missing.");
        }

        var map = (Map)context["map"];
        var center = (IntVec3)context["center"];
        var mealCells = (System.Collections.Generic.List<IntVec3>)context["mealCells"];
        var coolerCells = (System.Collections.Generic.List<IntVec3>)context["coolerCells"];
        var controlNames = (string[])context["controlNames"];
        var targetAmbient = (float[])context["targetAmbient"];

        stage = "build three sealed roofed holders";
        for (var index = 0; index < mealCells.Count; index++)
        {
            var roomCenter = mealCells[index];
            var bounds = new CellRect(roomCenter.x - 4, roomCenter.z - 5, 9, 11);
            foreach (var cell in bounds.Cells)
            {
                var boundary = cell.x == bounds.minX || cell.x == bounds.maxX ||
                               cell.z == bounds.minZ || cell.z == bounds.maxZ;
                if (boundary)
                {
                    var wall = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
                    wall.SetFactionDirect(Faction.OfPlayer);
                    GenSpawn.Spawn(wall, cell, map);
                }
                else
                {
                    map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
                }
            }
        }

        stage = "create the charged power net";
        var conduitDef = DefDatabase<ThingDef>.GetNamed("PowerConduit");
        for (var x = center.x - 13; x <= center.x + 13; x++)
        {
            var conduit = ThingMaker.MakeThing(conduitDef);
            conduit.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(conduit, new IntVec3(x, 0, center.z + 6), map);
        }

        var battery = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Battery"));
        battery.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(battery, new IntVec3(center.x, 0, center.z + 7), map, Rot4.North);
        battery.TryGetComp<CompPowerBattery>().SetStoredEnergyPct(1f);

        stage = "create powered ambient heater";
        var heater = ThingMaker.MakeThing(ThingDefOf.Heater);
        heater.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(
            heater,
            new IntVec3(mealCells[0].x, 0, center.z + 4),
            map,
            Rot4.North);
        heater.TryGetComp<CompTempControl>().targetTemperature = targetAmbient[0];

        stage = "create powered refrigerator and freezer coolers";
        var coolerDef = ThingDefOf.Cooler;
        var coolers = new System.Collections.Generic.List<Thing>();
        for (var index = 1; index <= 2; index++)
        {
            var coolerCell = coolerCells[index];
            var wall = coolerCell.GetEdifice(map);
            if (wall == null || wall.def != ThingDefOf.Wall)
            {
                throw new System.InvalidOperationException(
                    controlNames[index] + " has no replaceable north wall.");
            }

            wall.Destroy(DestroyMode.Vanish);
            var cooler = ThingMaker.MakeThing(coolerDef);
            cooler.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(cooler, coolerCell, map, Rot4.North);
            cooler.TryGetComp<CompTempControl>().targetTemperature = targetAmbient[index];
            coolers.Add(cooler);
        }

        map.powerNetManager.UpdatePowerNetsAndConnections_First();
        var temperatureControls = new System.Collections.Generic.List<Thing> { heater };
        temperatureControls.AddRange(coolers);
        foreach (var temperatureControl in temperatureControls)
        {
            var power = temperatureControl.TryGetComp<CompPowerTrader>();
            if (power == null || power.PowerNet == null)
            {
                throw new System.InvalidOperationException(
                    "A holder temperature control did not join the charged battery PowerNet.");
            }
        }

        stage = "verify sealed holder rooms";
        for (var index = 0; index < mealCells.Count; index++)
        {
            var room = mealCells[index].GetRoom(map);
            if (room == null || room.PsychologicallyOutdoors)
            {
                throw new System.InvalidOperationException(
                    controlNames[index] + " is not a real sealed indoor Room.");
            }
        }

        context["battery"] = battery;
        context["heater"] = heater;
        context["coolers"] = coolers;
        return battery.ThingID;
    }
    catch (System.Exception error)
    {
        AppDomain.CurrentDomain.SetData(contextKey, null);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
