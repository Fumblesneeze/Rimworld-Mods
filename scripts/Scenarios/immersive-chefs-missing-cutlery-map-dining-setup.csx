new System.Func<string>(() =>
{
    const string scenarioName = "Forkless colonist diner";
    var stage = "resolve map";
    try
    {
    var map = Find.CurrentMap;
    if (map == null)
    {
        throw new System.InvalidOperationException("The missing-cutlery scenario requires a playable map.");
    }

    stage = "generate pawn";
    var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
    pawn.Name = new NameSingle(scenarioName);
    pawn.inventory.innerContainer.ClearAndDestroyContents();

    stage = "find fixture cell";
    var diningCell = IntVec3.Invalid;
    foreach (var cell in GenRadial.RadialCellsAround(map.Center, 30f, true))
    {
        var valid = true;
        foreach (var candidate in CellRect.CenteredOn(cell, 4).Cells)
        {
            if (!candidate.InBounds(map) ||
                !candidate.Standable(map) ||
                candidate.GetEdifice(map) != null ||
                candidate.GetFirstPawn(map) != null)
            {
                valid = false;
                break;
            }
        }

        if (valid)
        {
            diningCell = cell;
            break;
        }
    }

    if (!diningCell.IsValid)
    {
        throw new System.InvalidOperationException("Could not find a dirt-accepting room for the missing-cutlery fixture.");
    }

    stage = "clear and wall fixture";
    var room = CellRect.CenteredOn(diningCell, 4);
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
            GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel), cell, map);
        }
    }

    if (!FilthMaker.CanMakeFilth(diningCell, map, ThingDefOf.Filth_Dirt))
    {
        throw new System.InvalidOperationException("The concrete dining cell does not accept vanilla dirt.");
    }

    stage = "create plated meal";
    var mealCell = new IntVec3(diningCell.x + 1, 0, diningCell.z);
    var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
    var plate = (ThingWithComps)ThingMaker.MakeThing(
        DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
        ThingDefOf.Steel);
    plate.GetComp<ImmersiveChefs.CompSanitation>().MarkClean(ImmersiveChefs.WashProvenance.Safe);
    meal.GetComp<ImmersiveChefs.CompCulinaryState>().ReplaceServings(new[]
    {
        new ImmersiveChefs.CulinaryServingRecord(
            50,
            35f,
            ImmersiveChefs.ContaminationSources.None,
            0,
            Find.TickManager.TicksGame)
    });
    if (!meal.GetComp<ImmersiveChefs.CompEmbeddedWare>().TryEmbedPlate(plate))
    {
        throw new System.InvalidOperationException("Could not embed the missing-cutlery scenario plate.");
    }

    stage = "spawn fixture";
    GenSpawn.Spawn(pawn, diningCell, map);
    GenSpawn.Spawn(meal, mealCell, map);
    pawn.needs.food.CurLevelPercentage = 0.50f;
    stage = "frame fixture";
    Find.CameraDriver.JumpToCurrentMapLoc(diningCell);
    Find.CameraDriver.SetRootSize(12f);
    Find.Selector.ClearSelection();
    Find.Selector.Select(pawn);
    stage = "format result";
    return string.Format(
        System.Globalization.CultureInfo.InvariantCulture,
        "{0}|{1}|{2}|{3}",
        pawn.ThingID,
        meal.ThingID,
        plate.ThingID,
        diningCell);
    }
    catch (System.Exception error)
    {
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
