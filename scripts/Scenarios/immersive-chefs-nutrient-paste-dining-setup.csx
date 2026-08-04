new System.Func<string>(() =>
{
    var stage = "resolve map";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The nutrient-paste dining scenario requires a playable map.");
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
                "Could not find a clear area for the nutrient-paste fixture.");
        }

        var fixtureRect = CellRect.CenteredOn(center, 9);
        foreach (var cell in fixtureRect.Cells)
        {
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
                "The nutrient-paste dispenser did not join the charged battery's real power net.");
        }

        if (!dispenser.CanDispenseNow)
        {
            throw new System.InvalidOperationException(
                "The powered nutrient-paste dispenser cannot consume its exact hopper feedstock.");
        }

        stage = "generate capable paste diner";
        Pawn diner = null;
        for (var attempt = 0; attempt < 32 && diner == null; attempt++)
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
                diner = pawn;
                diner.Name = new NameSingle("Nutrient Paste Plate Diner");
            }
            else
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }

        if (diner == null)
        {
            throw new System.InvalidOperationException("Could not generate a capable nutrient-paste diner.");
        }

        diner.inventory.innerContainer.ClearAndDestroyContents();
        diner.needs.food.CurLevelPercentage = 0.2f;
        var dinerCell = dispenser.InteractionCell + new IntVec3(0, 0, 3);
        GenSpawn.Spawn(diner, dinerCell, map);

        stage = "create exact clean serving ware";
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        var plate = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        plate.GetComp<CompQuality>().SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
        plate.GetComp<ImmersiveChefs.CompSanitation>()
            .MarkClean(ImmersiveChefs.WashProvenance.Safe);
        GenSpawn.Spawn(plate, dinerCell + new IntVec3(-2, 0, 0), map);

        var cutleryDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery");
        var cutlery = (ThingWithComps)ThingMaker.MakeThing(cutleryDef, ThingDefOf.Steel);
        cutlery.GetComp<CompQuality>().SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
        cutlery.GetComp<ImmersiveChefs.CompSanitation>()
            .MarkClean(ImmersiveChefs.WashProvenance.Safe);
        GenSpawn.Spawn(cutlery, dinerCell + new IntVec3(2, 0, 0), map);

        if (ImmersiveChefs.ImmersiveChefsMod.Settings.WareRequirementMode !=
            ImmersiveChefs.WareRequirementMode.Strict)
        {
            throw new System.InvalidOperationException(
                "The isolated scenario requires the default Strict serving-ware mode.");
        }

        Find.CameraDriver.JumpToCurrentMapLoc(center);
        Find.CameraDriver.SetRootSize(21f);
        Find.Selector.ClearSelection();
        Find.Selector.Select(diner);
        Find.TickManager.Pause();
        return diner.ThingID + "|" + dispenser.ThingID + "|" + hopper.ThingID + "|" +
               rice.ThingID + "|" + plate.ThingID + "|" + cutlery.ThingID;
    }
    catch (System.Exception error)
    {
        Log.Error(
            "[ImmersiveChefsScenario] Nutrient-paste setup failed at " + stage +
            ": " + error.GetType().Name + ": " + error.Message);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
