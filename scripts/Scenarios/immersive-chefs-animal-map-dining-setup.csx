new System.Func<string>(() =>
{
    const string scenarioName = "Forkless wild map diner";
    var map = Find.CurrentMap;
    if (map == null)
    {
        throw new System.InvalidOperationException("The animal map scenario requires a playable map.");
    }
    var animal = PawnGenerator.GeneratePawn(
        DefDatabase<PawnKindDef>.GetNamed("Raccoon"),
        null);
    animal.Name = new NameSingle(scenarioName);
    animal.inventory.innerContainer.ClearAndDestroyContents();

    var animalCell = IntVec3.Invalid;
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
            animalCell = cell;
            break;
        }
    }

    if (!animalCell.IsValid)
    {
        throw new System.InvalidOperationException("Could not find a standable room for the animal map dining fixture.");
    }

    var room = CellRect.CenteredOn(animalCell, 4);
    foreach (var cell in room.Cells)
    {
        foreach (var thing in cell.GetThingList(map)
                     .Where(thing => thing.def.category == ThingCategory.Item ||
                                     thing.def.category == ThingCategory.Plant)
                     .ToList())
        {
            thing.Destroy(DestroyMode.Vanish);
        }

        if (cell.x == room.minX || cell.x == room.maxX ||
            cell.z == room.minZ || cell.z == room.maxZ)
        {
            GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel), cell, map);
        }
    }

    var mealCell = new IntVec3(animalCell.x + 1, 0, animalCell.z);
    var cutleryCell = new IntVec3(animalCell.x - 1, 0, animalCell.z);

    var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
    var plate = (ThingWithComps)ThingMaker.MakeThing(
        DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
        ThingDefOf.Steel);
    var cutlery = (ThingWithComps)ThingMaker.MakeThing(
        DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
        ThingDefOf.Steel);
    plate.GetComp<ImmersiveChefs.CompSanitation>().MarkClean(ImmersiveChefs.WashProvenance.Safe);
    cutlery.GetComp<ImmersiveChefs.CompSanitation>().MarkClean(ImmersiveChefs.WashProvenance.Safe);
    meal.GetComp<ImmersiveChefs.CompCulinaryState>().ReplaceServings(new[]
    {
        new ImmersiveChefs.CulinaryServingRecord(
            0,
            -20f,
            ImmersiveChefs.ContaminationSources.DirtyCookware |
            ImmersiveChefs.ContaminationSources.DirtyPlate |
            ImmersiveChefs.ContaminationSources.DirtyCutlery,
            20,
            Find.TickManager.TicksGame)
    });
    if (!meal.GetComp<ImmersiveChefs.CompEmbeddedWare>().TryEmbedPlate(plate))
    {
        throw new System.InvalidOperationException("Could not embed the animal map scenario plate in its meal.");
    }

    GenSpawn.Spawn(animal, animalCell, map);
    GenSpawn.Spawn(meal, mealCell, map);
    GenSpawn.Spawn(cutlery, cutleryCell, map);
    animal.needs.food.CurLevelPercentage = 0.50f;
    Find.CameraDriver.JumpToCurrentMapLoc(meal.Position);
    Find.CameraDriver.SetRootSize(12f);
    Find.Selector.ClearSelection();
    Find.Selector.Select(meal);
    return string.Format(
        System.Globalization.CultureInfo.InvariantCulture,
        "{0}|{1}|{2}|{3}",
        animal.ThingID,
        meal.ThingID,
        plate.ThingID,
        cutlery.ThingID);
})()
