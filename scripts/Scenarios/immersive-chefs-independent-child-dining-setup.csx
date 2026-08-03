new System.Func<string>(() =>
{
    var stage = "resolve map";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException("The child dining scenario requires a playable map.");
        }

        Find.TickManager.Pause();
        stage = "verify active content";
        var biotechActive = false;
        foreach (var mod in LoadedModManager.RunningModsListForReading)
        {
            if (string.Equals(
                    mod.PackageId,
                    "ludeon.rimworld.biotech",
                    System.StringComparison.OrdinalIgnoreCase))
            {
                biotechActive = true;
                break;
            }
        }

        if (!biotechActive)
        {
            throw new System.InvalidOperationException("Biotech must be active for a real child developmental stage.");
        }

        stage = "find fixture area";
        var center = IntVec3.Invalid;
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 40f, true))
        {
            var fixture = new CellRect(candidate.x - 6, candidate.z - 6, 13, 13);
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
            throw new System.InvalidOperationException("Could not find a clear area for the child dining fixture.");
        }

        stage = "build sealed dining room";
        var fixtureRoom = new CellRect(center.x - 6, center.z - 6, 13, 13);
        foreach (var cell in fixtureRoom.Cells)
        {
            var existingThings = cell.GetThingList(map).ToList();
            foreach (var thing in existingThings)
            {
                if (thing.def.category == ThingCategory.Item ||
                    thing.def.category == ThingCategory.Plant ||
                    thing.def.category == ThingCategory.Filth)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
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

        stage = "create independent child";
        var child = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            Faction.OfPlayer,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false,
            fixedBiologicalAge: 8f,
            fixedChronologicalAge: 8f,
            developmentalStages: DevelopmentalStage.Child,
            forceNoGear: true));
        child.Name = new NameSingle("Independent Child Diner");
        child.inventory.innerContainer.ClearAndDestroyContents();
        if (child.DevelopmentalStage != DevelopmentalStage.Child)
        {
            throw new System.InvalidOperationException(
                "Pawn generation did not produce a real child developmental stage.");
        }

        if (!child.RaceProps.Humanlike || child.needs == null || child.needs.food == null || child.jobs == null)
        {
            throw new System.InvalidOperationException("The generated child cannot perform ordinary self-feeding.");
        }

        var childCell = new IntVec3(center.x - 2, 0, center.z);
        GenSpawn.Spawn(child, childCell, map);

        stage = "create plated meal and cutlery";
        var meal = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("MealLavish"));
        var gold = DefDatabase<ThingDef>.GetNamed("Gold");
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            gold);
        var cutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            gold);
        plate.GetComp<CompQuality>().SetQuality(QualityCategory.Excellent, ArtGenerationContext.Colony);
        cutlery.GetComp<CompQuality>().SetQuality(QualityCategory.Excellent, ArtGenerationContext.Colony);
        plate.GetComp<ImmersiveChefs.CompSanitation>()
            .MarkClean(ImmersiveChefs.WashProvenance.Safe);
        cutlery.GetComp<ImmersiveChefs.CompSanitation>()
            .MarkClean(ImmersiveChefs.WashProvenance.Safe);
        meal.GetComp<ImmersiveChefs.CompCulinaryState>().ReplaceServings(new[]
        {
            new ImmersiveChefs.CulinaryServingRecord(
                95,
                70f,
                ImmersiveChefs.ContaminationSources.None,
                0,
                Find.TickManager.TicksGame)
        });
        if (!meal.GetComp<ImmersiveChefs.CompEmbeddedWare>().TryEmbedPlate(plate))
        {
            throw new System.InvalidOperationException("Could not embed the child's exact clean plate.");
        }

        var mealCell = new IntVec3(center.x + 2, 0, center.z);
        var cutleryCell = new IntVec3(center.x, 0, center.z + 2);
        GenSpawn.Spawn(meal, mealCell, map);
        GenSpawn.Spawn(cutlery, cutleryCell, map);
        child.needs.food.CurLevelPercentage = 0.15f;

        stage = "frame fixture";
        Find.CameraDriver.JumpToCurrentMapLoc(center);
        Find.CameraDriver.SetRootSize(17f);
        Find.Selector.ClearSelection();
        Find.Selector.Select(child);
        return child.ThingID + "|" + meal.ThingID + "|" + plate.ThingID + "|" + cutlery.ThingID;
    }
    catch (System.Exception error)
    {
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
