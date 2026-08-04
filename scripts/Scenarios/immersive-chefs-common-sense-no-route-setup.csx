new System.Func<string>(() =>
{
    const string dinerName = "Common Sense No Route Diner";
    var stage = "resolve map";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The Common Sense no-route scenario requires a playable map.");
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
                    cell.GetFirstPawn(map) != null)
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
                "Could not find a clear area for the Common Sense no-route fixture.");
        }

        stage = "build sealed dry fixture";
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

        stage = "create powered but switched-off dishwasher";
        var conduitDef = DefDatabase<ThingDef>.GetNamed("PowerConduit");
        for (var x = center.x; x <= center.x + 7; x++)
        {
            var conduit = ThingMaker.MakeThing(conduitDef);
            conduit.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(conduit, new IntVec3(x, 0, center.z + 4), map);
        }

        var dishwasher = ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Dishwasher"));
        dishwasher.SetFactionDirect(Faction.OfPlayer);
        dishwasher.TryGetComp<CompFlickable>().SwitchIsOn = false;
        GenSpawn.Spawn(
            dishwasher,
            new IntVec3(center.x + 2, 0, center.z + 4),
            map,
            Rot4.North);
        var battery = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Battery"));
        battery.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(
            battery,
            new IntVec3(center.x + 6, 0, center.z + 4),
            map,
            Rot4.North);
        battery.TryGetComp<CompPowerBattery>().SetStoredEnergyPct(1f);
        map.powerNetManager.UpdatePowerNetsAndConnections_First();
        if (dishwasher.TryGetComp<CompPowerTrader>().PowerOn)
        {
            throw new System.InvalidOperationException(
                "The no-route dishwasher unexpectedly started switched on.");
        }

        stage = "create cleaning-capable diner";
        Pawn diner = null;
        for (var attempt = 0; attempt < 32 && diner == null; attempt++)
        {
            var candidate = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                forceNoGear: true));
            if (!candidate.WorkTypeIsDisabled(WorkTypeDefOf.Cleaning) &&
                candidate.health.capacities.CapableOf(PawnCapacityDefOf.Moving) &&
                candidate.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                diner = candidate;
            }
            else
            {
                candidate.Destroy(DestroyMode.Vanish);
            }
        }

        if (diner == null)
        {
            throw new System.InvalidOperationException(
                "Could not generate a cleaning-capable no-route diner.");
        }

        diner.Name = new NameSingle(dinerName);
        diner.inventory.innerContainer.ClearAndDestroyContents();
        diner.workSettings.EnableAndInitialize();
        foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
        {
            diner.workSettings.SetPriority(workType, 0);
        }
        diner.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 1);
        diner.workSettings.SetPriority(DefDatabase<WorkTypeDef>.GetNamed("BasicWorker"), 1);
        for (var hour = 0; hour < 24; hour++)
        {
            diner.timetable.SetAssignment(hour, TimeAssignmentDefOf.Work);
        }
        diner.needs.food.CurLevelPercentage = 0.45f;
        if (diner.needs.rest != null)
        {
            diner.needs.rest.CurLevel = diner.needs.rest.MaxLevel;
        }
        GenSpawn.Spawn(diner, new IntVec3(center.x - 6, 0, center.z), map);

        stage = "create exact place setting";
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        meal.GetComp<ImmersiveChefs.CompCulinaryState>().ReplaceServings(new[]
        {
            new ImmersiveChefs.CulinaryServingRecord(
                60,
                35f,
                ImmersiveChefs.ContaminationSources.None,
                0,
                Find.TickManager.TicksGame)
        });
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        plate.GetComp<ImmersiveChefs.CompSanitation>()
            .MarkClean(ImmersiveChefs.WashProvenance.Safe);
        if (!meal.GetComp<ImmersiveChefs.CompEmbeddedWare>().TryEmbedPlate(plate))
        {
            throw new System.InvalidOperationException("Could not embed the no-route plate.");
        }

        var cutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        cutlery.GetComp<ImmersiveChefs.CompSanitation>()
            .MarkClean(ImmersiveChefs.WashProvenance.Safe);
        GenSpawn.Spawn(meal, new IntVec3(center.x - 3, 0, center.z), map);
        GenSpawn.Spawn(cutlery, new IntVec3(center.x - 4, 0, center.z + 2), map);

        stage = "frame player-owned workflow";
        Find.CameraDriver.SetRootPosAndSize(
            new UnityEngine.Vector3(center.x + 0.5f, 0f, center.z + 0.5f),
            12f);
        map.mapDrawer.RegenerateEverythingNow();
        Find.Selector.ClearSelection();
        Find.Selector.Select(diner);
        Messages.Message(
            "No-route cleanup ready: order the selected diner to eat. After the dirty place setting remains free, switch on the dishwasher and let ordinary Cleaning work collect it.",
            MessageTypeDefOf.NeutralEvent,
            false);
        Find.TickManager.Pause();
        return string.Join("|", new[]
        {
            diner.ThingID,
            meal.ThingID,
            plate.ThingID,
            cutlery.ThingID,
            dishwasher.ThingID,
            battery.ThingID,
            center.ToString()
        });
    }
    catch (System.Exception error)
    {
        Log.Error(
            "[ImmersiveChefsScenario] Common Sense no-route setup failed at " + stage +
            ": " + error.GetType().Name + ": " + error.Message);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
