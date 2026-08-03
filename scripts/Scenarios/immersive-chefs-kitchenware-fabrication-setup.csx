new System.Func<string>(() =>
{
    var stage = "resolve map";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The kitchenware fabrication scenario requires a playable map.");
        }

        Find.TickManager.Pause();
        stage = "find clear fixture area";
        var center = IntVec3.Invalid;
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 55f, true))
        {
            var fixture = new CellRect(candidate.x - 16, candidate.z - 7, 33, 15);
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
                "Could not find a clear area for two fabrication fixtures.");
        }

        var fixtureRoom = new CellRect(center.x - 16, center.z - 7, 33, 15);
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

        stage = "create capable crafters";
        Pawn primitiveCrafter = null;
        for (var attempt = 0; attempt < 32 && primitiveCrafter == null; attempt++)
        {
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                forceNoGear: true));
            if (!pawn.WorkTypeIsDisabled(WorkTypeDefOf.Crafting) &&
                pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving) &&
                pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                primitiveCrafter = pawn;
                primitiveCrafter.Name = new NameSingle("Primitive Cookware Crafter");
                primitiveCrafter.inventory.innerContainer.ClearAndDestroyContents();
                primitiveCrafter.skills.GetSkill(SkillDefOf.Crafting).Level = 20;
                primitiveCrafter.workSettings.EnableAndInitialize();
                primitiveCrafter.workSettings.SetPriority(WorkTypeDefOf.Crafting, 1);
            }
            else
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }

        Pawn modernCrafter = null;
        for (var attempt = 0; attempt < 32 && modernCrafter == null; attempt++)
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
                modernCrafter = pawn;
                modernCrafter.Name = new NameSingle("Modern Cookware Machinist");
                modernCrafter.inventory.innerContainer.ClearAndDestroyContents();
                modernCrafter.skills.GetSkill(SkillDefOf.Crafting).Level = 20;
                modernCrafter.workSettings.EnableAndInitialize();
                modernCrafter.workSettings.SetPriority(WorkTypeDefOf.Smithing, 1);
            }
            else
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }

        if (primitiveCrafter == null || modernCrafter == null)
        {
            throw new System.InvalidOperationException("Could not generate both capable crafting workers.");
        }
        var primitiveCenter = new IntVec3(center.x - 9, 0, center.z);
        var modernCenter = new IntVec3(center.x + 9, 0, center.z);

        stage = "create real work tables";
        var craftingSpot = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("CraftingSpot"));
        craftingSpot.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(craftingSpot, primitiveCenter, map, Rot4.North);

        var machiningDef = DefDatabase<ThingDef>.GetNamed("TableMachining");
        var machiningTable = ThingMaker.MakeThing(
            machiningDef,
            machiningDef.MadeFromStuff ? ThingDefOf.Steel : null);
        machiningTable.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(machiningTable, modernCenter, map, Rot4.North);

        stage = "create charged connected power net";
        var conduitDef = DefDatabase<ThingDef>.GetNamed("PowerConduit");
        for (var x = modernCenter.x - 3; x <= modernCenter.x + 5; x++)
        {
            var conduit = ThingMaker.MakeThing(conduitDef);
            conduit.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(conduit, new IntVec3(x, 0, modernCenter.z + 3), map);
        }

        var batteryDef = DefDatabase<ThingDef>.GetNamed("Battery");
        var battery = ThingMaker.MakeThing(
            batteryDef,
            batteryDef.MadeFromStuff ? ThingDefOf.Steel : null);
        battery.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(
            battery,
            new IntVec3(modernCenter.x + 4, 0, modernCenter.z + 3),
            map,
            Rot4.North);
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
        GenSpawn.Spawn(
            primitiveCrafter,
            new IntVec3(primitiveCenter.x - 3, 0, primitiveCenter.z),
            map);
        GenSpawn.Spawn(
            modernCrafter,
            new IntVec3(modernCenter.x - 4, 0, modernCenter.z),
            map);
        stage = "create exact recipe ingredients";
        var granite = DefDatabase<ThingDef>.GetNamed("BlocksGranite");
        var primitiveStone = ThingMaker.MakeThing(granite);
        primitiveStone.stackCount = 40;
        GenSpawn.Spawn(
            primitiveStone,
            new IntVec3(primitiveCenter.x - 2, 0, primitiveCenter.z + 2),
            map);
        var primitiveWood = ThingMaker.MakeThing(ThingDefOf.WoodLog);
        primitiveWood.stackCount = 5;
        GenSpawn.Spawn(
            primitiveWood,
            new IntVec3(primitiveCenter.x - 2, 0, primitiveCenter.z - 2),
            map);
        var modernSteel = ThingMaker.MakeThing(ThingDefOf.Steel);
        modernSteel.stackCount = 50;
        GenSpawn.Spawn(
            modernSteel,
            new IntVec3(modernCenter.x - 2, 0, modernCenter.z + 2),
            map);
        var modernWood = ThingMaker.MakeThing(ThingDefOf.WoodLog);
        modernWood.stackCount = 5;
        GenSpawn.Spawn(
            modernWood,
            new IntVec3(modernCenter.x - 2, 0, modernCenter.z - 2),
            map);

        stage = "create real one-shot bills";
        var primitiveBill = new Bill_Production(
            DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakePrimitiveCookware"));
        primitiveBill.repeatMode = BillRepeatModeDefOf.RepeatCount;
        primitiveBill.repeatCount = 1;
        primitiveBill.ingredientSearchRadius = 12f;
        primitiveBill.ingredientFilter.SetDisallowAll();
        primitiveBill.ingredientFilter.SetAllow(granite, true);
        primitiveBill.ingredientFilter.SetAllow(ThingDefOf.WoodLog, true);
        primitiveBill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        primitiveBill.SetPawnRestriction(primitiveCrafter);
        ((IBillGiver)craftingSpot).BillStack.AddBill(primitiveBill);

        var modernBill = new Bill_Production(
            DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakeModernCookware"));
        modernBill.repeatMode = BillRepeatModeDefOf.RepeatCount;
        modernBill.repeatCount = 1;
        modernBill.ingredientSearchRadius = 12f;
        modernBill.ingredientFilter.SetDisallowAll();
        modernBill.ingredientFilter.SetAllow(ThingDefOf.Steel, true);
        modernBill.ingredientFilter.SetAllow(ThingDefOf.WoodLog, true);
        modernBill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        modernBill.SetPawnRestriction(modernCrafter);
        ((IBillGiver)machiningTable).BillStack.AddBill(modernBill);

        stage = "frame fixtures";
        Find.CameraDriver.JumpToCurrentMapLoc(center);
        Find.CameraDriver.SetRootSize(24f);
        Find.Selector.ClearSelection();
        Find.Selector.Select(primitiveCrafter);
        Find.Selector.Select(modernCrafter);
        return primitiveCrafter.ThingID + "|" + modernCrafter.ThingID + "|" +
               craftingSpot.ThingID + "|" + machiningTable.ThingID + "|" +
               primitiveBill.GetUniqueLoadID() + "|" + modernBill.GetUniqueLoadID();
    }
    catch (System.Exception error)
    {
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
