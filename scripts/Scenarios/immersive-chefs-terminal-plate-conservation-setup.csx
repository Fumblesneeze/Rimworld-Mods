new System.Func<string>(() =>
{
    const string contextKey = "ImmersiveChefs.TerminalPlateScenario";
    var map = Find.CurrentMap;
    if (map == null)
    {
        throw new System.InvalidOperationException(
            "The terminal plate-conservation scenario requires a playable map.");
    }

    Find.TickManager.Pause();
    var mealCells = new System.Collections.Generic.List<IntVec3>();
    foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 75f, true))
    {
        var separated = true;
        foreach (var existing in mealCells)
        {
            if (existing.DistanceToSquared(candidate) < 100f)
            {
                separated = false;
                break;
            }
        }

        if (!separated)
        {
            continue;
        }

        var fixture = CellRect.CenteredOn(candidate, 3);
        var valid = true;
        foreach (var cell in fixture.Cells)
        {
            if (!cell.InBounds(map) ||
                cell.GetEdifice(map) != null ||
                cell.GetFirstPawn(map) != null ||
                map.zoneManager.ZoneAt(cell) != null)
            {
                valid = false;
                break;
            }
        }

        if (valid)
        {
            mealCells.Add(candidate);
            if (mealCells.Count == 3)
            {
                break;
            }
        }
    }

    if (mealCells.Count != 3)
    {
        throw new System.InvalidOperationException(
            "Could not find three separated edifice-free areas for the terminal plate fixture.");
    }

    foreach (var mealCell in mealCells)
    {
        var room = CellRect.CenteredOn(mealCell, 3);
        foreach (var cell in room.Cells)
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
            if (cell.x == room.minX || cell.x == room.maxX ||
                cell.z == room.minZ || cell.z == room.maxZ)
            {
                var wall = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
                wall.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(wall, cell, map);
            }
            else
            {
                map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
            }
        }
    }

    var context = new System.Collections.Generic.Dictionary<string, object>();
    context["map"] = map;
    context["mealCells"] = mealCells;
    AppDomain.CurrentDomain.SetData(contextKey, context);
    return mealCells[0] + "|" + mealCells[1] + "|" + mealCells[2];
})()
