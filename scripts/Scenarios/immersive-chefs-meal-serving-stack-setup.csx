new System.Func<string>(() =>
{
    var stage = "resolve map";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The meal-serving-stack scenario requires a playable map.");
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
                "Could not find a clear area for the meal-serving-stack fixture.");
        }

        stage = "build sealed dining room";
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

        stage = "create hungry drafted diner";
        var diner = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            Faction.OfPlayer,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false,
            forceNoGear: true));
        diner.Name = new NameSingle("Serving Stack Diner");
        diner.inventory.innerContainer.ClearAndDestroyContents();
        GenSpawn.Spawn(diner, new IntVec3(center.x - 4, 0, center.z), map);
        diner.needs.food.CurLevelPercentage = 0.01f;
        diner.drafter.Drafted = true;

        stage = "create three distinct servings and place settings";
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        meal.stackCount = 3;
        meal.GetComp<ImmersiveChefs.CompCulinaryState>().ReplaceServings(new[]
        {
            new ImmersiveChefs.CulinaryServingRecord(20, 70f,
                ImmersiveChefs.ContaminationSources.None, 0, Find.TickManager.TicksGame),
            new ImmersiveChefs.CulinaryServingRecord(50, 70f,
                ImmersiveChefs.ContaminationSources.None, 0, Find.TickManager.TicksGame),
            new ImmersiveChefs.CulinaryServingRecord(80, 70f,
                ImmersiveChefs.ContaminationSources.None, 0, Find.TickManager.TicksGame)
        });

        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        plate.stackCount = 3;
        plate.GetComp<CompQuality>().SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
        plate.GetComp<ImmersiveChefs.CompSanitation>()
            .MarkClean(ImmersiveChefs.WashProvenance.Safe);
        var ware = meal.GetComp<ImmersiveChefs.CompEmbeddedWare>();
        for (var index = 0; index < 3; index++)
        {
            if (!ware.TryEmbedPlate(plate))
            {
                throw new System.InvalidOperationException(
                    "Could not embed serving plate " + (index + 1) + ".");
            }
        }

        var cutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        cutlery.stackCount = 3;
        cutlery.GetComp<CompQuality>().SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
        cutlery.GetComp<ImmersiveChefs.CompSanitation>()
            .MarkClean(ImmersiveChefs.WashProvenance.Safe);

        GenSpawn.Spawn(meal, new IntVec3(center.x - 1, 0, center.z), map);
        GenSpawn.Spawn(cutlery, new IntVec3(center.x, 0, center.z - 2), map);

        stage = "frame paused fixture";
        Find.TickManager.Pause();
        Find.CameraDriver.SetRootPosAndSize(center.ToVector3Shifted(), 18f);
        Find.Selector.ClearSelection();
        Find.Selector.Select(meal);
        return diner.ThingID + "|" + meal.ThingID + "|" + plate.ThingID + "|" +
               cutlery.ThingID;
    }
    catch (System.Exception error)
    {
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
