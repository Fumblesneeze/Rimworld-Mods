new System.Func<string>(() =>
{
    var stage = "resolve map";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The Processor dishwasher scenario requires a playable map.");
        }

        Find.TickManager.Pause();
        stage = "find fixture area";
        var center = IntVec3.Invalid;
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 45f, true))
        {
            var room = new CellRect(candidate.x - 8, candidate.z - 5, 17, 11);
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
                "Could not find a clear area for the Processor dishwasher fixture.");
        }

        stage = "build powered fixture";
        var fixtureRoom = new CellRect(center.x - 8, center.z - 5, 17, 11);
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
        }

        foreach (var cell in fixtureRoom.Cells)
        {
            if (cell.x != fixtureRoom.minX &&
                cell.x != fixtureRoom.maxX &&
                cell.z != fixtureRoom.minZ &&
                cell.z != fixtureRoom.maxZ)
            {
                continue;
            }

            var wall = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
            wall.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(wall, cell, map);
        }

        var conduitDef = DefDatabase<ThingDef>.GetNamed("PowerConduit");
        for (var x = center.x - 3; x <= center.x + 3; x++)
        {
            var conduit = ThingMaker.MakeThing(conduitDef);
            conduit.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(conduit, new IntVec3(x, 0, center.z + 3), map);
        }

        var dishwasher = ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Dishwasher"));
        dishwasher.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(
            dishwasher,
            new IntVec3(center.x - 2, 0, center.z + 3),
            map,
            Rot4.North);

        var battery = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Battery"));
        battery.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(
            battery,
            new IntVec3(center.x + 2, 0, center.z + 3),
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
                "RimWorld did not connect the Processor dishwasher to the charged battery.");
        }

        stage = "create dirty weighted ware";
        var ware = new System.Collections.Generic.List<ThingWithComps>();
        ware.Add((ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cookware"),
            ThingDefOf.Steel));
        ware.Add((ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel));
        ware.Add((ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel));
        for (var index = 0; index < ware.Count; index++)
        {
            ware[index].GetComp<ImmersiveChefs.CompSanitation>().MarkDirty();
            ware[index].HitPoints = ware[index].MaxHitPoints - (index + 1);
            GenSpawn.Spawn(
                ware[index],
                new IntVec3(center.x - 1 + index, 0, center.z - 2),
                map);
        }

        stage = "create native haulers";
        var haulerIds = new System.Collections.Generic.List<string>();
        for (var workerIndex = 0; workerIndex < 3; workerIndex++)
        {
            Pawn worker = null;
            for (var attempt = 0; attempt < 32 && worker == null; attempt++)
            {
                var candidate = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    PawnKindDefOf.Colonist,
                    Faction.OfPlayer,
                    forceGenerateNewPawn: true,
                    canGeneratePawnRelations: false));
                if (!candidate.WorkTypeIsDisabled(WorkTypeDefOf.Hauling) &&
                    candidate.health.capacities.CapableOf(PawnCapacityDefOf.Moving) &&
                    candidate.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
                {
                    worker = candidate;
                }
                else
                {
                    candidate.Destroy(DestroyMode.Vanish);
                }
            }

            if (worker == null)
            {
                throw new System.InvalidOperationException(
                    "Could not generate a capable Processor dishwasher hauler.");
            }

            worker.Name = new NameSingle("Dishwasher Hauler " + (workerIndex + 1));
            worker.inventory.innerContainer.ClearAndDestroyContents();
            GenSpawn.Spawn(
                worker,
                new IntVec3(center.x - 1 + workerIndex, 0, center.z),
                map);
            worker.workSettings.EnableAndInitialize();
            foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                worker.workSettings.SetPriority(workType, 0);
            }
            worker.workSettings.SetPriority(WorkTypeDefOf.Hauling, 1);
            haulerIds.Add(worker.ThingID);
        }

        stage = "frame fixture";
        Find.CameraDriver.SetRootPosAndSize(
            new UnityEngine.Vector3(center.x + 0.5f, 0f, center.z + 0.5f),
            9f);
        map.mapDrawer.RegenerateEverythingNow();
        Find.Selector.ClearSelection();
        Find.Selector.Select(dishwasher);
        Messages.Message(
            "Processor dishwasher ready: unpause to let the three native haulers load 5.25 place settings.",
            MessageTypeDefOf.NeutralEvent,
            false);
        return string.Join("|", new[]
        {
            dishwasher.ThingID,
            battery.ThingID,
            ware[0].ThingID,
            ware[1].ThingID,
            ware[2].ThingID,
            string.Join(",", haulerIds),
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
