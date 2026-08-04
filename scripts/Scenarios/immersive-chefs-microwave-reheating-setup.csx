new System.Func<string>(() =>
{
    var stage = "resolve map";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The microwave-reheating scenario requires a playable map.");
        }

        Find.TickManager.Pause();
        stage = "find fixture area";
        var center = IntVec3.Invalid;
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 40f, true))
        {
            var fixture = new CellRect(candidate.x - 7, candidate.z - 6, 15, 13);
            var valid = true;
            foreach (var cell in fixture.Cells)
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
                "Could not find a clear area for the microwave-reheating fixture.");
        }

        stage = "build sealed room";
        var room = new CellRect(center.x - 7, center.z - 6, 15, 13);
        foreach (var cell in room.Cells)
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
            if (cell.x == room.minX || cell.x == room.maxX ||
                cell.z == room.minZ || cell.z == room.maxZ)
            {
                var wall = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
                wall.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(wall, cell, map);
            }
        }

        stage = "create connected microwave power net";
        var conduitDef = DefDatabase<ThingDef>.GetNamed("PowerConduit");
        for (var x = center.x; x <= center.x + 5; x++)
        {
            var conduit = ThingMaker.MakeThing(conduitDef);
            conduit.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(conduit, new IntVec3(x, 0, center.z + 3), map);
        }

        var microwave = ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Microwave"));
        microwave.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(microwave, new IntVec3(center.x + 1, 0, center.z + 3), map, Rot4.North);

        var battery = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Battery"));
        battery.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(battery, new IntVec3(center.x + 4, 0, center.z + 3), map, Rot4.North);
        battery.TryGetComp<CompPowerBattery>().SetStoredEnergyPct(1f);
        map.powerNetManager.UpdatePowerNetsAndConnections_First();

        var microwavePower = microwave.TryGetComp<CompPowerTrader>();
        for (var tick = 0; tick <= 200 && !microwavePower.PowerOn; tick++)
        {
            Find.TickManager.DoSingleTick();
        }

        if (!microwavePower.PowerOn || microwavePower.PowerNet == null)
        {
            throw new System.InvalidOperationException(
                "RimWorld did not connect the microwave to the charged battery PowerNet.");
        }

        stage = "create hungry drafted diner";
        var diner = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            Faction.OfPlayer,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false,
            forceNoGear: true));
        diner.Name = new NameSingle("Microwave Diner");
        diner.inventory.innerContainer.ClearAndDestroyContents();
        GenSpawn.Spawn(diner, new IntVec3(center.x - 4, 0, center.z), map);
        diner.needs.food.CurLevelPercentage = 0.05f;
        diner.drafter.Drafted = true;

        stage = "create frozen plated meal and clean cutlery";
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var cutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        plate.GetComp<CompQuality>().SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
        cutlery.GetComp<CompQuality>().SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
        plate.GetComp<ImmersiveChefs.CompSanitation>()
            .MarkClean(ImmersiveChefs.WashProvenance.Safe);
        cutlery.GetComp<ImmersiveChefs.CompSanitation>()
            .MarkClean(ImmersiveChefs.WashProvenance.Safe);
        meal.GetComp<ImmersiveChefs.CompCulinaryState>().ReplaceServings(new[]
        {
            new ImmersiveChefs.CulinaryServingRecord(
                80,
                -5f,
                ImmersiveChefs.ContaminationSources.None,
                0,
                Find.TickManager.TicksGame)
        });
        if (!meal.GetComp<ImmersiveChefs.CompEmbeddedWare>().TryEmbedPlate(plate))
        {
            throw new System.InvalidOperationException(
                "Could not embed the exact microwave-reheating plate.");
        }

        GenSpawn.Spawn(meal, new IntVec3(center.x - 2, 0, center.z), map);
        GenSpawn.Spawn(cutlery, new IntVec3(center.x - 1, 0, center.z - 2), map);

        stage = "frame paused fixture";
        Find.TickManager.Pause();
        Find.CameraDriver.SetRootPosAndSize(center.ToVector3Shifted(), 18f);
        Find.Selector.ClearSelection();
        Find.Selector.Select(meal);
        return diner.ThingID + "|" + meal.ThingID + "|" + plate.ThingID + "|" +
               cutlery.ThingID + "|" + microwave.ThingID + "|" + battery.ThingID;
    }
    catch (System.Exception error)
    {
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
