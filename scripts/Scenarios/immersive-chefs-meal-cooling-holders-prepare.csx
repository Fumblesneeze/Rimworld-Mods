new System.Func<string>(() =>
{
    const string contextKey = "ImmersiveChefs.MealCoolingHolderScenario";
    var stage = "resolve map";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The meal-cooling holder scenario requires a playable map.");
        }

        Find.TickManager.Pause();
        stage = "find fixture area";
        var center = IntVec3.Invalid;
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 55f, true))
        {
            var footprint = new CellRect(candidate.x - 17, candidate.z - 6, 35, 15);
            var valid = true;
            foreach (var cell in footprint.Cells)
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
                "Could not find a clear area for the meal-cooling holder fixture.");
        }

        var offsets = new[] { -11, 0, 11 };
        var controlNames = new[]
        {
            "Ambient Cooling Control",
            "Refrigerated Cooling Control",
            "Frozen Cooling Control"
        };
        var targetAmbient = new[] { 21f, 5f, -10f };
        var mealCells = new System.Collections.Generic.List<IntVec3>();
        var coolerCells = new System.Collections.Generic.List<IntVec3>();
        for (var index = 0; index < offsets.Length; index++)
        {
            var roomCenter = new IntVec3(center.x + offsets[index], 0, center.z);
            mealCells.Add(roomCenter);
            coolerCells.Add(new IntVec3(roomCenter.x, 0, roomCenter.z + 5));
        }

        stage = "clear fixture footprint";
        var fixtureFootprint = new CellRect(center.x - 17, center.z - 6, 35, 15);
        foreach (var cell in fixtureFootprint.Cells)
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

        var context = new System.Collections.Generic.Dictionary<string, object>();
        context["map"] = map;
        context["center"] = center;
        context["offsets"] = offsets;
        context["controlNames"] = controlNames;
        context["targetAmbient"] = targetAmbient;
        context["mealCells"] = mealCells;
        context["coolerCells"] = coolerCells;
        AppDomain.CurrentDomain.SetData(contextKey, context);
        return center.ToString();
    }
    catch (System.Exception error)
    {
        AppDomain.CurrentDomain.SetData(contextKey, null);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
