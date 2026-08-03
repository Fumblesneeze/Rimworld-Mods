new System.Func<string>(() =>
{
    var stage = "resolve map";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The chef's knife scenario requires a playable map.");
        }

        Find.TickManager.Pause();
        stage = "find clear fixture area";
        var center = IntVec3.Invalid;
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 55f, true))
        {
            var fixture = new CellRect(candidate.x - 9, candidate.z - 6, 19, 13);
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
                "Could not find a clear area for the chef's knife fixture.");
        }

        var fixtureRoom = new CellRect(center.x - 9, center.z - 6, 19, 13);
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

        stage = "create capable armed machinist";
        Pawn machinist = null;
        for (var attempt = 0; attempt < 32 && machinist == null; attempt++)
        {
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                forceNoGear: true));
            if (!pawn.WorkTypeIsDisabled(WorkTypeDefOf.Smithing) &&
                pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving) &&
                pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                machinist = pawn;
                machinist.Name = new NameSingle("Chef Knife Machinist");
                machinist.inventory.innerContainer.ClearAndDestroyContents();
                machinist.skills.GetSkill(SkillDefOf.Crafting).Level = 20;
                machinist.workSettings.EnableAndInitialize();
                machinist.workSettings.SetPriority(WorkTypeDefOf.Smithing, 1);
                var ordinaryKnife = (ThingWithComps)ThingMaker.MakeThing(
                    DefDatabase<ThingDef>.GetNamed("MeleeWeapon_Knife"),
                    ThingDefOf.Steel);
                machinist.equipment.AddEquipment(ordinaryKnife);
            }
            else
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }

        if (machinist == null)
        {
            throw new System.InvalidOperationException("Could not generate a capable machinist.");
        }

        stage = "create powered machining table";
        var machiningDef = DefDatabase<ThingDef>.GetNamed("TableMachining");
        var machiningTable = ThingMaker.MakeThing(
            machiningDef,
            machiningDef.MadeFromStuff ? ThingDefOf.Steel : null);
        machiningTable.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(machiningTable, center, map, Rot4.North);

        var conduitDef = DefDatabase<ThingDef>.GetNamed("PowerConduit");
        for (var x = center.x - 3; x <= center.x + 5; x++)
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
        GenSpawn.Spawn(battery, new IntVec3(center.x + 4, 0, center.z + 3), map, Rot4.North);
        var batteryComp = battery.TryGetComp<CompPowerBattery>();
        batteryComp.SetStoredEnergyPct(1f);
        map.powerNetManager.UpdatePowerNetsAndConnections_First();
        var machiningPower = machiningTable.TryGetComp<CompPowerTrader>();
        for (var tick = 0; tick <= 200 && !machiningPower.PowerOn; tick++)
        {
            Find.TickManager.DoSingleTick();
        }

        if (!machiningPower.PowerOn || machiningPower.PowerNet == null ||
            !System.Object.ReferenceEquals(machiningPower.PowerNet, batteryComp.PowerNet))
        {
            throw new System.InvalidOperationException(
                "The machining table did not join the charged battery's real power net.");
        }

        Find.TickManager.Pause();
        GenSpawn.Spawn(machinist, new IntVec3(center.x - 4, 0, center.z), map);

        stage = "create exact steel and real bill";
        var steel = ThingMaker.MakeThing(ThingDefOf.Steel);
        steel.stackCount = 30;
        GenSpawn.Spawn(steel, new IntVec3(center.x - 2, 0, center.z + 2), map);
        var bill = new Bill_Production(
            DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakeChefsKnife"));
        bill.repeatMode = BillRepeatModeDefOf.RepeatCount;
        bill.repeatCount = 1;
        bill.ingredientSearchRadius = 10f;
        bill.ingredientFilter.SetDisallowAll();
        bill.ingredientFilter.SetAllow(ThingDefOf.Steel, true);
        bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        bill.SetPawnRestriction(machinist);
        ((IBillGiver)machiningTable).BillStack.AddBill(bill);

        stage = "frame native fabrication fixture";
        Find.CameraDriver.JumpToCurrentMapLoc(center);
        Find.CameraDriver.SetRootSize(16f);
        Find.Selector.ClearSelection();
        Find.Selector.Select(machinist);
        Find.TickManager.Pause();
        return machinist.ThingID + "|" + machiningTable.ThingID + "|" + bill.GetUniqueLoadID();
    }
    catch (System.Exception error)
    {
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
