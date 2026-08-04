new System.Func<string>(() =>
{
    var stage = "resolve map";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The prepared-paste scenario requires a playable map.");
        }

        Find.TickManager.Pause();
        stage = "find clear fixture room";
        var center = IntVec3.Invalid;
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 60f, true))
        {
            var fixture = CellRect.CenteredOn(candidate, 9);
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
                "Could not find a clear area for the prepared-paste fixture.");
        }

        var fixtureRect = CellRect.CenteredOn(center, 9);
        foreach (var cell in fixtureRect.Cells)
        {
            map.fogGrid.Unfog(cell);
            foreach (var thing in cell.GetThingList(map).ToList())
            {
                if (thing.def.category == ThingCategory.Item ||
                    thing.def.category == ThingCategory.Plant ||
                    thing.def.category == ThingCategory.Filth)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }

            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
            if (cell.x == fixtureRect.minX || cell.x == fixtureRect.maxX ||
                cell.z == fixtureRect.minZ || cell.z == fixtureRect.maxZ)
            {
                var wall = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
                wall.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(wall, cell, map);
            }
        }

        stage = "spawn real dispenser and hopper";
        var dispenserDef = DefDatabase<ThingDef>.GetNamed("NutrientPasteDispenser");
        var dispenser = (Building_NutrientPasteDispenser)ThingMaker.MakeThing(dispenserDef);
        dispenser.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(dispenser, new IntVec3(center.x - 1, 0, center.z - 2), map, Rot4.North);

        var hopperCell = IntVec3.Invalid;
        foreach (var candidate in GenAdj.CellsAdjacentCardinal(dispenser))
        {
            if (candidate.InBounds(map) && candidate != dispenser.InteractionCell &&
                candidate.GetEdifice(map) == null)
            {
                hopperCell = candidate;
                break;
            }
        }

        if (!hopperCell.IsValid)
        {
            throw new System.InvalidOperationException("The dispenser has no free adjacent hopper cell.");
        }

        var hopper = ThingMaker.MakeThing(ThingDefOf.Hopper);
        hopper.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(hopper, hopperCell, map, Rot4.North);
        var rice = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RawRice"));
        rice.stackCount = 30;
        GenSpawn.Spawn(rice, hopperCell, map);
        rice.SetForbidden(true, false);

        stage = "create charged connected power net";
        var conduitDef = DefDatabase<ThingDef>.GetNamed("PowerConduit");
        var powerRow = dispenser.Position.z - 1;
        for (var x = center.x - 7; x <= center.x + 7; x++)
        {
            var conduit = ThingMaker.MakeThing(conduitDef);
            conduit.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(conduit, new IntVec3(x, 0, powerRow), map);
        }

        var batteryDef = DefDatabase<ThingDef>.GetNamed("Battery");
        var battery = ThingMaker.MakeThing(
            batteryDef,
            batteryDef.MadeFromStuff ? ThingDefOf.Steel : null);
        battery.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(battery, new IntVec3(center.x + 6, 0, powerRow), map, Rot4.North);
        var batteryComp = battery.TryGetComp<CompPowerBattery>();
        batteryComp.SetStoredEnergyPct(1f);
        map.powerNetManager.UpdatePowerNetsAndConnections_First();
        var dispenserPower = dispenser.TryGetComp<CompPowerTrader>();
        for (var tick = 0; tick <= 200 && !dispenserPower.PowerOn; tick++)
        {
            Find.TickManager.DoSingleTick();
        }

        if (!dispenserPower.PowerOn || dispenserPower.PowerNet == null ||
            !System.Object.ReferenceEquals(dispenserPower.PowerNet, batteryComp.PowerNet))
        {
            throw new System.InvalidOperationException(
                "The prepared-paste dispenser did not join the charged battery's real power net.");
        }

        if (!dispenser.CanDispenseNow)
        {
            throw new System.InvalidOperationException(
                "The powered prepared-paste dispenser cannot consume its exact hopper feedstock.");
        }

        stage = "generate capable operator";
        Pawn worker = null;
        for (var attempt = 0; attempt < 32 && worker == null; attempt++)
        {
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                forceNoGear: true));
            if (pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) >= 0.99f &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) >= 0.99f)
            {
                worker = pawn;
                worker.Name = new NameSingle("Prepared Paste Operator");
            }
            else
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }

        if (worker == null)
        {
            throw new System.InvalidOperationException("Could not generate a capable prepared-paste operator.");
        }

        worker.inventory.innerContainer.ClearAndDestroyContents();
        worker.needs.food.CurLevelPercentage = 0.9f;
        GenSpawn.Spawn(worker, dispenser.InteractionCell + new IntVec3(0, 0, 3), map);

        Find.CameraDriver.SetRootPosAndSize(
            new UnityEngine.Vector3(center.x + 0.5f, 0f, center.z + 0.5f),
            11f);
        map.mapDrawer.RegenerateEverythingNow();
        Find.Selector.ClearSelection();
        Find.Selector.Select(worker);
        Messages.Message(
            "Prepared-paste test ready: right-click the powered dispenser and choose 'Dispense prepared cooking paste'.",
            MessageTypeDefOf.NeutralEvent,
            false);
        Find.TickManager.Pause();
        return worker.ThingID + "|" + dispenser.ThingID + "|" + hopper.ThingID + "|" +
               rice.ThingID;
    }
    catch (System.Exception error)
    {
        Log.Error(
            "[ImmersiveChefsScenario] Prepared-paste setup failed at " + stage +
            ": " + error.GetType().Name + ": " + error.Message);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
