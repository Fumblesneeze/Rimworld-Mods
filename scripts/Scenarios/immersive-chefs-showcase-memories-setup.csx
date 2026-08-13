new System.Func<string>(() =>
{
    const string dinerName = "Nora Pike";
    var stage = "resolve map";
    try
    {
        var map = Find.CurrentMap;
        if (map == null) throw new System.InvalidOperationException("A playable map is required.");
        Find.TickManager.Pause();

        stage = "find dining-room site";
        var center = IntVec3.Invalid;
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 55f, true))
        {
            var rect = new CellRect(candidate.x - 8, candidate.z - 5, 17, 11);
            var available = true;
            foreach (var cell in rect.Cells)
            {
                if (!cell.InBounds(map) || cell.GetEdifice(map) != null || cell.GetFirstPawn(map) != null)
                {
                    available = false;
                    break;
                }
            }
            if (available)
            {
                center = candidate;
                break;
            }
        }
        if (!center.IsValid) throw new System.InvalidOperationException("No clear dining-room site was available.");

        stage = "build lived-in dining room";
        var room = new CellRect(center.x - 8, center.z - 5, 17, 11);
        foreach (var cell in room.Cells)
        {
            var things = cell.GetThingList(map);
            for (var index = things.Count - 1; index >= 0; index--)
            {
                var thing = things[index];
                if (thing.def.category == ThingCategory.Item ||
                    thing.def.category == ThingCategory.Plant ||
                    thing.def.category == ThingCategory.Filth)
                    thing.Destroy(DestroyMode.Vanish);
            }
            map.terrainGrid.SetTerrain(cell, DefDatabase<TerrainDef>.GetNamed("WoodPlankFloor"));
            if (cell.x > room.minX && cell.x < room.maxX && cell.z > room.minZ && cell.z < room.maxZ)
            {
                map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
            }
        }

        var wallStuff = DefDatabase<ThingDef>.GetNamed("BlocksGranite");
        foreach (var cell in room.EdgeCells)
        {
            if (cell == new IntVec3(center.x, 0, room.minZ)) continue;
            var wall = ThingMaker.MakeThing(ThingDefOf.Wall, wallStuff);
            wall.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(wall, cell, map);
        }
        var door = ThingMaker.MakeThing(ThingDefOf.Door, ThingDefOf.WoodLog);
        door.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(door, new IntVec3(center.x, 0, room.minZ), map, Rot4.North);

        var furnitureNames = new[] { "DiningChair", "DiningChair", "DiningChair", "DiningChair", "TorchLamp" };
        var furnitureCells = new[]
        {
            center + new IntVec3(5, 0, -2), center + new IntVec3(5, 0, 2),
            center + new IntVec3(3, 0, 0), center + new IntVec3(6, 0, 0),
            center + new IntVec3(-1, 0, 3)
        };
        var furnitureRotations = new[] { Rot4.North, Rot4.South, Rot4.East, Rot4.West, Rot4.North };
        for (var index = 0; index < furnitureNames.Length; index++)
        {
            var def = DefDatabase<ThingDef>.GetNamed(furnitureNames[index]);
            var thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? ThingDefOf.WoodLog : null);
            thing.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(thing, furnitureCells[index], map, furnitureRotations[index]);
        }
        var shelfDefName = DefDatabase<ThingDef>.GetNamedSilentFail("ShelfSmall") != null ? "ShelfSmall" : "Shelf";
        var shelfDef = DefDatabase<ThingDef>.GetNamed(shelfDefName);
        var shelf = ThingMaker.MakeThing(shelfDef, shelfDef.MadeFromStuff ? ThingDefOf.WoodLog : null);
        shelf.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(shelf, center + new IntVec3(-3, 0, 3), map, Rot4.East);

        stage = "remove competing cutlery";
        var cutleryDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery");
        var cutleryThings = map.listerThings.ThingsOfDef(cutleryDef);
        for (var index = cutleryThings.Count - 1; index >= 0; index--) cutleryThings[index].Destroy(DestroyMode.Vanish);

        stage = "create cold plated meal";
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.WoodLog);
        plate.GetComp<CompQuality>().SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
        plate.GetComp<ImmersiveChefs.CompSanitation>().MarkClean(ImmersiveChefs.WashProvenance.Safe);
        meal.GetComp<ImmersiveChefs.CompCulinaryState>().ReplaceServings(new[]
        {
            new ImmersiveChefs.CulinaryServingRecord(
                50,
                5f,
                ImmersiveChefs.ContaminationSources.None,
                0,
                Find.TickManager.TicksGame)
        });
        if (!meal.GetComp<ImmersiveChefs.CompEmbeddedWare>().TryEmbedPlate(plate))
            throw new System.InvalidOperationException("The exact wooden plate could not be embedded.");

        stage = "create named diner";
        var diner = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            Faction.OfPlayer,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false,
            forceNoGear: false));
        diner.Name = new NameSingle(dinerName);
        diner.inventory.innerContainer.ClearAndDestroyContents();
        diner.needs.food.CurLevelPercentage = 0.08f;
        var diningCell = center + new IntVec3(-6, 0, -1);
        var mealCell = center + new IntVec3(-5, 0, -1);
        GenSpawn.Spawn(diner, diningCell, map);
        GenSpawn.Spawn(meal, mealCell, map);
        diner.drafter.Drafted = true;

        stage = "frame passive player fixture";
        Find.Selector.ClearSelection();
        Find.CameraDriver.SetRootPosAndSize((center + new IntVec3(3, 0, 0)).ToVector3Shifted(), 17f);
        return diner.ThingID + "|" + meal.ThingID + "|" + plate.ThingID + "|" + center;
    }
    catch (System.Exception exception)
    {
        return "ERROR|" + stage + "|" + exception;
    }
})()
