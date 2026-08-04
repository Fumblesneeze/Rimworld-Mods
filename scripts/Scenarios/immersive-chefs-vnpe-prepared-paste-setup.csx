new System.Func<string>(() =>
{
    var stage = "resolve map";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The VNPE prepared-paste scenario requires a playable map.");
        }

        Find.TickManager.Pause();
        stage = "find clear fixture room";
        var center = IntVec3.Invalid;
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 60f, true))
        {
            var fixture = CellRect.CenteredOn(candidate, 13);
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
                "Could not find a clear area for the VNPE prepared-paste fixture.");
        }

        var fixtureRect = CellRect.CenteredOn(center, 13);
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

        stage = "spawn the real VNPE pipe network";
        var pipeDef = DefDatabase<ThingDef>.GetNamed("VNPE_NutrientPastePipe");
        for (var x = center.x - 5; x <= center.x + 3; x++)
        {
            var pipe = ThingMaker.MakeThing(pipeDef);
            pipe.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(pipe, new IntVec3(x, 0, center.z), map);
        }

        var tapDef = DefDatabase<ThingDef>.GetNamed("VNPE_NutrientPasteTap");
        var tap = (Building_NutrientPasteDispenser)ThingMaker.MakeThing(tapDef);
        tap.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(tap, new IntVec3(center.x - 5, 0, center.z), map, Rot4.North);

        var vatDef = DefDatabase<ThingDef>.GetNamed("VNPE_NutrientPasteVat");
        var vat = ThingMaker.MakeThing(vatDef);
        vat.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(vat, new IntVec3(center.x + 2, 0, center.z), map, Rot4.North);

        stage = "create charged connected power net";
        var conduitDef = DefDatabase<ThingDef>.GetNamed("PowerConduit");
        for (var x = center.x - 6; x <= center.x + 6; x++)
        {
            var conduit = ThingMaker.MakeThing(conduitDef);
            conduit.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(conduit, new IntVec3(x, 0, center.z - 1), map);
        }

        for (var z = center.z - 3; z <= center.z; z++)
        {
            var conduit = ThingMaker.MakeThing(conduitDef);
            conduit.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(conduit, new IntVec3(center.x + 6, 0, z), map);
        }

        var batteryDef = DefDatabase<ThingDef>.GetNamed("Battery");
        var battery = ThingMaker.MakeThing(
            batteryDef,
            batteryDef.MadeFromStuff ? ThingDefOf.Steel : null);
        battery.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(battery, new IntVec3(center.x + 6, 0, center.z - 3), map, Rot4.North);
        var batteryComp = battery.TryGetComp<CompPowerBattery>();
        batteryComp.SetStoredEnergyPct(1f);

        stage = "fill the connected VNPE vat as a scenario precondition";
        map.powerNetManager.UpdatePowerNetsAndConnections_First();
        for (var tick = 0; tick < 240; tick++)
        {
            Find.TickManager.DoSingleTick();
        }

        var storage = vat.TryGetComp<PipeSystem.CompResourceStorage>();
        var tapResource = tap.TryGetComp<PipeSystem.CompResource>();
        if (storage == null || tapResource == null || storage.PipeNet == null ||
            tapResource.PipeNet == null ||
            !System.Object.ReferenceEquals(storage.PipeNet, tapResource.PipeNet))
        {
            throw new System.InvalidOperationException(
                "The real VNPE tap and vat did not join the same native pipe network.");
        }

        storage.AddResource(12f);
        for (var tick = 0; tick < 120; tick++)
        {
            Find.TickManager.DoSingleTick();
        }

        if (!tap.TryGetComp<CompPowerTrader>().PowerOn ||
            !vat.TryGetComp<CompPowerTrader>().PowerOn)
        {
            throw new System.InvalidOperationException(
                "The VNPE tap or vat did not join the charged native power net.");
        }

        if (!tap.CanDispenseNow)
        {
            throw new System.InvalidOperationException(
                "The powered VNPE tap cannot dispense from its filled native pipe network.");
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
                worker.Name = new NameSingle("VNPE Paste Operator");
            }
            else
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }

        if (worker == null)
        {
            throw new System.InvalidOperationException(
                "Could not generate a capable VNPE prepared-paste operator.");
        }

        worker.inventory.innerContainer.ClearAndDestroyContents();
        worker.needs.food.CurLevelPercentage = 0.9f;
        GenSpawn.Spawn(worker, tap.InteractionCell + new IntVec3(0, 0, 3), map);

        Find.CameraDriver.SetRootPosAndSize(
            new UnityEngine.Vector3(center.x + 0.5f, 0f, center.z + 0.5f),
            15f);
        map.mapDrawer.RegenerateEverythingNow();
        Find.Selector.ClearSelection();
        Find.Selector.Select(worker);
        Messages.Message(
            "VNPE test ready: right-click the nutrient paste tap and choose 'Dispense prepared cooking paste'.",
            MessageTypeDefOf.NeutralEvent,
            false);
        Find.TickManager.Pause();
        return worker.ThingID + "|" + tap.ThingID + "|" + vat.ThingID + "|" +
               storage.AmountStored.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }
    catch (System.Exception error)
    {
        Log.Error(
            "[ImmersiveChefsScenario] VNPE prepared-paste setup failed at " + stage +
            ": " + error.GetType().Name + ": " + error.Message);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
