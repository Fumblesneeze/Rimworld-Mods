new System.Func<string>(() =>
{
    const string contextKey = "ImmersiveChefs.KitchenwareAlertScenario";
    var map = Find.CurrentMap;
    if (map == null)
    {
        throw new System.InvalidOperationException(
            "The kitchenware-alert scenario requires a playable map.");
    }

    var stage = "initialize";
    try
    {
    Find.TickManager.Pause();
    var existingWare = map.listerThings.AllThings
        .Where(thing =>
            thing.def.GetModExtension<ImmersiveChefs.KitchenwareExtension>() != null)
        .ToList();
    if (existingWare.Count != 0)
    {
        throw new System.InvalidOperationException(
            "The alert fixture requires a clean map without existing Immersive Chefs kitchenware.");
    }

    stage = "find and clear fixture area";
    var center = IntVec3.Invalid;
    foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 55f, true))
    {
        var area = CellRect.CenteredOn(candidate, 6);
        var areaIsClear = true;
        foreach (var cell in area.Cells)
        {
            if (!cell.InBounds(map) ||
                cell.GetEdifice(map) != null ||
                cell.GetFirstPawn(map) != null ||
                map.zoneManager.ZoneAt(cell) != null)
            {
                areaIsClear = false;
                break;
            }
        }

        if (areaIsClear)
        {
            center = candidate;
            break;
        }
    }

    if (!center.IsValid)
    {
        throw new System.InvalidOperationException(
            "Could not find a clear area for the kitchenware-alert fixture.");
    }

    foreach (var cell in CellRect.CenteredOn(center, 6).Cells)
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

    stage = "create the only active cook";
    var cookingWork = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
    foreach (var colonist in map.mapPawns.FreeColonistsSpawned)
    {
        colonist.workSettings?.SetPriority(cookingWork, 0);
    }

    Pawn worker = null;
    for (var attempt = 0; attempt < 64; attempt++)
    {
        var candidate = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            Faction.OfPlayer,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false));
        if (!candidate.WorkTypeIsDisabled(cookingWork) &&
            candidate.health.capacities.CapableOf(PawnCapacityDefOf.Moving) &&
            candidate.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
        {
            worker = candidate;
            break;
        }

        candidate.Destroy(DestroyMode.Vanish);
    }

    if (worker == null)
    {
        throw new System.InvalidOperationException(
            "Could not generate a colonist capable of cooking.");
    }

    worker.Name = new NameSingle("Alert Tester");
    worker.inventory.innerContainer.ClearAndDestroyContents();
    GenSpawn.Spawn(worker, new IntVec3(center.x - 4, 0, center.z), map);
    worker.workSettings.EnableAndInitialize();
    foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
    {
        worker.workSettings.SetPriority(workType, 0);
    }
    worker.workSettings.SetPriority(cookingWork, 1);
    worker.drafter.Drafted = false;

    stage = "create fueled stove and active bill";
    var recipe = DefDatabase<RecipeDef>.GetNamed("CookMealSimple");
    var stoveDef = DefDatabase<ThingDef>.GetNamed("FueledStove");
    var stove = ThingMaker.MakeThing(
        stoveDef,
        stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
    stove.SetFactionDirect(Faction.OfPlayer);
    GenSpawn.Spawn(stove, new IntVec3(center.x, 0, center.z), map, Rot4.North);
    var stoveFuel = stove.TryGetComp<CompRefuelable>();
    if (stoveFuel == null)
    {
        throw new System.InvalidOperationException("FueledStove has no refuelable component.");
    }
    stoveFuel.Refuel(stoveFuel.Props.fuelCapacity);
    var stoveBill = new Bill_Production(recipe);
    stoveBill.repeatMode = BillRepeatModeDefOf.RepeatCount;
    stoveBill.repeatCount = 1;
    ((IBillGiver)stove).BillStack.AddBill(stoveBill);

    stage = "create campfire and raw-food controls";
    var campfireDef = DefDatabase<ThingDef>.GetNamed("Campfire");
    var campfire = ThingMaker.MakeThing(
        campfireDef,
        campfireDef.MadeFromStuff ? ThingDefOf.Steel : null);
    campfire.SetFactionDirect(Faction.OfPlayer);
    GenSpawn.Spawn(campfire, new IntVec3(center.x + 5, 0, center.z), map, Rot4.North);
    var campfireFuel = campfire.TryGetComp<CompRefuelable>();
    if (campfireFuel == null)
    {
        throw new System.InvalidOperationException("Campfire has no refuelable component.");
    }
    campfireFuel.Refuel(campfireFuel.Props.fuelCapacity);
    var campfireBill = new Bill_Production(recipe);
    campfireBill.repeatMode = BillRepeatModeDefOf.RepeatCount;
    campfireBill.repeatCount = 1;
    ((IBillGiver)campfire).BillStack.AddBill(campfireBill);

    var berries = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RawBerries"));
    berries.stackCount = 20;
    GenSpawn.Spawn(berries, new IntVec3(center.x + 5, 0, center.z - 2), map);

    stage = "create dirty physical kitchenware controls";
    var cookware = (ThingWithComps)ThingMaker.MakeThing(
        DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cookware"),
        ThingDefOf.Steel);
    var plate = (ThingWithComps)ThingMaker.MakeThing(
        DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
        ThingDefOf.Steel);
    cookware.GetComp<ImmersiveChefs.CompSanitation>().MarkDirty();
    plate.GetComp<ImmersiveChefs.CompSanitation>().MarkDirty();
    GenSpawn.Spawn(cookware, new IntVec3(center.x - 1, 0, center.z - 3), map);
    GenSpawn.Spawn(plate, new IntVec3(center.x + 1, 0, center.z - 3), map);
    cookware.SetForbidden(true, false);
    plate.SetForbidden(true, false);

    stage = "publish fixture context";
    var context = new System.Collections.Generic.Dictionary<string, object>();
    context["map"] = map;
    context["center"] = center;
    context["worker"] = worker;
    context["stove"] = stove;
    context["campfire"] = campfire;
    context["berries"] = berries;
    context["cookware"] = cookware;
    context["plate"] = plate;
    AppDomain.CurrentDomain.SetData(contextKey, context);
    return string.Join("|", new[]
    {
        worker.ThingID,
        stove.ThingID,
        campfire.ThingID,
        berries.ThingID,
        cookware.ThingID,
        plate.ThingID,
        center.ToString()
    });
    }
    catch (System.Exception error)
    {
        Log.Error(
            "[ImmersiveChefsScenario] Kitchenware-alert setup failed at " + stage +
            ": " + error.GetType().Name + ": " + error.Message);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
