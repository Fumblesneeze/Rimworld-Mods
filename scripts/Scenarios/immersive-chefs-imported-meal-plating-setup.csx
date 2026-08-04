new System.Func<string>(() =>
{
    var stage = "resolve map";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The imported-meal plating scenario requires a playable map.");
        }

        Find.TickManager.Pause();
        stage = "find clear fixture area";
        var center = IntVec3.Invalid;
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 55f, true))
        {
            var room = new CellRect(candidate.x - 8, candidate.z - 6, 17, 13);
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
                "Could not find a clear area for the imported-meal plating fixture.");
        }

        stage = "build fixture room";
        var fixture = new CellRect(center.x - 8, center.z - 6, 17, 13);
        foreach (var cell in fixture.Cells)
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
            if (cell.x == fixture.minX || cell.x == fixture.maxX ||
                cell.z == fixture.minZ || cell.z == fixture.maxZ)
            {
                var wall = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
                wall.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(wall, cell, map);
            }
        }

        stage = "create native plating surface";
        var stoveDef = DefDatabase<ThingDef>.GetNamed("FueledStove");
        var stove = ThingMaker.MakeThing(
            stoveDef,
            stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
        stove.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(stove, new IntVec3(center.x + 3, 0, center.z), map, Rot4.North);
        var refuelable = stove.TryGetComp<CompRefuelable>();
        refuelable.Refuel(refuelable.Props.fuelCapacity);

        stage = "create cooking worker";
        var cookingWorkType = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        Pawn cook = null;
        for (var attempt = 0; attempt < 32 && cook == null; attempt++)
        {
            var candidate = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                forceNoGear: true));
            if (!candidate.WorkTypeIsDisabled(cookingWorkType) &&
                candidate.health.capacities.CapableOf(PawnCapacityDefOf.Moving) &&
                candidate.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                cook = candidate;
            }
            else
            {
                candidate.Destroy(DestroyMode.Vanish);
            }
        }

        if (cook == null)
        {
            throw new System.InvalidOperationException("Could not generate a capable plating cook.");
        }

        cook.Name = new NameSingle("Imported Meal Plating Cook");
        cook.inventory.innerContainer.ClearAndDestroyContents();
        cook.workSettings.EnableAndInitialize();
        foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
        {
            cook.workSettings.SetPriority(workType, 0);
        }
        cook.workSettings.SetPriority(cookingWorkType, 1);
        for (var hour = 0; hour < 24; hour++)
        {
            cook.timetable.SetAssignment(hour, TimeAssignmentDefOf.Work);
        }
        if (cook.needs.food != null)
        {
            cook.needs.food.CurLevel = cook.needs.food.MaxLevel;
        }
        if (cook.needs.rest != null)
        {
            cook.needs.rest.CurLevel = cook.needs.rest.MaxLevel;
        }
        if (cook.needs.joy != null)
        {
            cook.needs.joy.CurLevel = cook.needs.joy.MaxLevel;
        }
        GenSpawn.Spawn(cook, new IntVec3(center.x - 5, 0, center.z), map);

        stage = "create imported meal data";
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        meal.stackCount = 3;
        var culinary = meal.GetComp<ImmersiveChefs.CompCulinaryState>();
        culinary.ReplaceServings(new[]
        {
            new ImmersiveChefs.CulinaryServingRecord(
                73, 42f, ImmersiveChefs.ContaminationSources.None, 0, Find.TickManager.TicksGame),
            new ImmersiveChefs.CulinaryServingRecord(
                73, 42f, ImmersiveChefs.ContaminationSources.None, 0, Find.TickManager.TicksGame),
            new ImmersiveChefs.CulinaryServingRecord(
                73, 42f, ImmersiveChefs.ContaminationSources.None, 0, Find.TickManager.TicksGame)
        });
        var ingredients = meal.GetComp<CompIngredients>();
        ingredients.RegisterIngredient(DefDatabase<ThingDef>.GetNamed("RawRice"));
        var rottable = meal.TryGetComp<CompRottable>();
        rottable.RotProgress = 1200f;
        GenSpawn.Spawn(meal, new IntVec3(center.x - 2, 0, center.z + 2), map);

        stage = "create clean plate supply";
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        var plates = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        plates.stackCount = 3;
        plates.GetComp<CompQuality>().SetQuality(
            QualityCategory.Excellent,
            ArtGenerationContext.Colony);
        plates.GetComp<ImmersiveChefs.CompSanitation>()
            .MarkClean(ImmersiveChefs.WashProvenance.Safe);
        GenSpawn.Spawn(plates, new IntVec3(center.x, 0, center.z - 2), map);

        stage = "frame untouched fixture";
        Find.CameraDriver.SetRootPosAndSize(
            new UnityEngine.Vector3(center.x + 0.5f, 0f, center.z + 0.5f),
            12f);
        map.mapDrawer.RegenerateEverythingNow();
        Find.Selector.ClearSelection();
        Find.Selector.Select(meal);
        Messages.Message(
            "Imported meal plating ready: press Space to let the Cooking work scheduler plate the selected imported meals.",
            MessageTypeDefOf.NeutralEvent,
            false);
        Find.TickManager.Pause();
        return cook.ThingID + "|" + meal.ThingID + "|" + plates.ThingID + "|" + stove.ThingID;
    }
    catch (System.Exception error)
    {
        Log.Error(
            "[ImmersiveChefsScenario] Imported-meal plating setup failed at " + stage +
            ": " + error.GetType().Name + ": " + error.Message);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
