new System.Func<string>(() =>
{
    var stage = "resolve map";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The storage and dishwashing scenario requires a playable map.");
        }

        Find.TickManager.Pause();
        stage = "find fixture room";
        var center = IntVec3.Invalid;
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 45f, true))
        {
            var room = new CellRect(candidate.x - 9, candidate.z - 6, 19, 13);
            var valid = true;
            foreach (var cell in room.Cells)
            {
                if (!cell.InBounds(map) ||
                    cell.GetEdifice(map) != null ||
                    cell.GetFirstPawn(map) != null ||
                    map.zoneManager.ZoneAt(cell) != null)
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
                "Could not find a clear area for the storage and dishwashing fixture.");
        }

        stage = "build fixture room";
        var fixtureRoom = new CellRect(center.x - 9, center.z - 6, 19, 13);
        foreach (var cell in fixtureRoom.Cells)
        {
            foreach (var thing in cell.GetThingList(map)
                         .Where(thing => thing.def.category == ThingCategory.Item ||
                                         thing.def.category == ThingCategory.Plant ||
                                         thing.def.category == ThingCategory.Filth)
                         .ToList())
            {
                thing.Destroy(DestroyMode.Vanish);
            }

            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
            if (cell.x == fixtureRoom.minX || cell.x == fixtureRoom.maxX ||
                cell.z == fixtureRoom.minZ || cell.z == fixtureRoom.maxZ)
            {
                var wall = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
                wall.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(wall, cell, map);
            }
        }

        stage = "create sanitation stockpiles";
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        var cleanSpecial = DefDatabase<SpecialThingFilterDef>.GetNamed(
            "ImmersiveChefs_AllowCleanKitchenware");
        var dirtySpecial = DefDatabase<SpecialThingFilterDef>.GetNamed(
            "ImmersiveChefs_AllowDirtyKitchenware");
        var cleanZone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
        var dirtyZone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
        map.zoneManager.RegisterZone(cleanZone);
        map.zoneManager.RegisterZone(dirtyZone);
        cleanZone.label = "CLEAN PLATES";
        dirtyZone.label = "DIRTY PLATES";
        foreach (var cell in new CellRect(center.x - 7, center.z - 2, 3, 4).Cells)
        {
            cleanZone.AddCell(cell);
        }
        foreach (var cell in new CellRect(center.x + 5, center.z - 2, 3, 4).Cells)
        {
            dirtyZone.AddCell(cell);
        }

        foreach (var zone in new[] { cleanZone, dirtyZone })
        {
            zone.settings.Priority = StoragePriority.Critical;
            zone.settings.filter.SetDisallowAll();
            zone.settings.filter.SetAllow(plateDef, true);
        }
        cleanZone.settings.filter.SetAllow(cleanSpecial, true);
        cleanZone.settings.filter.SetAllow(dirtySpecial, false);
        dirtyZone.settings.filter.SetAllow(cleanSpecial, false);
        dirtyZone.settings.filter.SetAllow(dirtySpecial, true);

        stage = "create connected powered dishwasher";
        var conduitDef = DefDatabase<ThingDef>.GetNamed("PowerConduit");
        for (var x = center.x - 3; x <= center.x + 3; x++)
        {
            var conduit = ThingMaker.MakeThing(conduitDef);
            conduit.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(conduit, new IntVec3(x, 0, center.z + 4), map);
        }
        var dishwasher = ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Dishwasher"));
        dishwasher.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(
            dishwasher,
            new IntVec3(center.x - 2, 0, center.z + 4),
            map,
            Rot4.North);
        var battery = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Battery"));
        battery.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(
            battery,
            new IntVec3(center.x + 2, 0, center.z + 4),
            map,
            Rot4.North);
        battery.TryGetComp<CompPowerBattery>().SetStoredEnergyPct(1f);
        map.powerNetManager.UpdatePowerNetsAndConnections_First();
        var dishwasherPower = dishwasher.TryGetComp<CompPowerTrader>();
        for (var tick = 0; tick <= 200 && !dishwasherPower.PowerOn; tick++)
        {
            Find.TickManager.DoSingleTick();
        }
        if (!dishwasherPower.PowerOn)
        {
            throw new System.InvalidOperationException(
                "RimWorld did not connect the dishwasher to the charged battery power net.");
        }

        stage = "create fixture pawns";
        System.Func<string, WorkTypeDef, Pawn> createWorker = (name, requiredWork) =>
        {
            for (var attempt = 0; attempt < 32; attempt++)
            {
                var candidate = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    PawnKindDefOf.Colonist,
                    Faction.OfPlayer,
                    forceGenerateNewPawn: true,
                    canGeneratePawnRelations: false));
                if (!candidate.WorkTypeIsDisabled(requiredWork) &&
                    candidate.health.capacities.CapableOf(PawnCapacityDefOf.Moving) &&
                    candidate.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                {
                    candidate.Name = new NameSingle(name);
                    return candidate;
                }

                candidate.Destroy(DestroyMode.Vanish);
            }

            throw new System.InvalidOperationException(
                "Could not generate a capable " + requiredWork.label + " worker after 32 attempts.");
        };

        var hauler = createWorker("Storage Hauler", WorkTypeDefOf.Hauling);
        hauler.inventory.innerContainer.ClearAndDestroyContents();
        GenSpawn.Spawn(hauler, new IntVec3(center.x - 2, 0, center.z), map);
        hauler.workSettings.EnableAndInitialize();

        var cleaner = createWorker("Dish Cleaner", WorkTypeDefOf.Cleaning);
        cleaner.inventory.innerContainer.ClearAndDestroyContents();
        GenSpawn.Spawn(cleaner, new IntVec3(center.x + 2, 0, center.z), map);
        cleaner.workSettings.EnableAndInitialize();

        foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
        {
            hauler.workSettings.SetPriority(workType, 0);
            cleaner.workSettings.SetPriority(workType, 0);
        }
        hauler.workSettings.SetPriority(WorkTypeDefOf.Hauling, 1);
        cleaner.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 1);
        cleaner.drafter.Drafted = true;

        stage = "create clean dirty and forbidden plates";
        var cleanPlate = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        var dirtyPlate = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        var forbiddenPlate = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        cleanPlate.GetComp<ImmersiveChefs.CompSanitation>()
            .MarkClean(ImmersiveChefs.WashProvenance.Safe);
        dirtyPlate.GetComp<ImmersiveChefs.CompSanitation>().MarkDirty();
        forbiddenPlate.GetComp<ImmersiveChefs.CompSanitation>().MarkDirty();
        GenSpawn.Spawn(cleanPlate, new IntVec3(center.x - 1, 0, center.z - 3), map);
        GenSpawn.Spawn(dirtyPlate, new IntVec3(center.x, 0, center.z - 3), map);
        GenSpawn.Spawn(forbiddenPlate, new IntVec3(center.x + 1, 0, center.z - 3), map);
        forbiddenPlate.SetForbidden(true, false);

        stage = "frame fixture";
        Find.CameraDriver.SetRootPosAndSize(
            new UnityEngine.Vector3(center.x + 0.5f, 0f, center.z + 0.5f),
            10f);
        map.mapDrawer.RegenerateEverythingNow();
        Find.Selector.ClearSelection();
        Find.Selector.Select(forbiddenPlate);
        Messages.Message(
            "Storage test ready: unpause to route the allowed plates; the selected dirty plate must remain forbidden.",
            MessageTypeDefOf.NeutralEvent,
            false);
        return string.Join("|", new[]
        {
            cleanPlate.ThingID,
            dirtyPlate.ThingID,
            forbiddenPlate.ThingID,
            hauler.ThingID,
            cleaner.ThingID,
            dishwasher.ThingID,
            battery.ThingID,
            cleanZone.ID.ToString(System.Globalization.CultureInfo.InvariantCulture),
            dirtyZone.ID.ToString(System.Globalization.CultureInfo.InvariantCulture),
            center.ToString()
        });
    }
    catch (System.Exception error)
    {
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
