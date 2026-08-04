new System.Func<string>(() =>
{
    var stage = "resolve map";
    try
    {
        var fixtureName = "Prepared Save Reload";
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The prepared-food save/load scenario requires a playable map.");
        }

        Find.TickManager.Pause();
        stage = "find clear fixture area";
        var center = IntVec3.Invalid;
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 60f, true))
        {
            var fixture = CellRect.CenteredOn(candidate, 11);
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
                "Could not find a clear area for the prepared-food save/load fixture.");
        }

        var fixtureRect = CellRect.CenteredOn(center, 11);
        foreach (var cell in fixtureRect.Cells)
        {
            map.fogGrid.Unfog(cell);
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
        }

        stage = "create persistent prepared-food fixture";
        var preparedDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_PreparedFood");
        var prepared = (ThingWithComps)ThingMaker.MakeThing(preparedDef);
        prepared.stackCount = 7;
        var preparedComp = prepared.GetComp<ImmersiveChefs.CompPreparedFood>();
        preparedComp.Initialize(new ImmersiveChefs.PreparedFoodState(
            new[]
            {
                new ImmersiveChefs.IngredientContribution("RawRice", 0.035f, 4, 72),
                new ImmersiveChefs.IngredientContribution("AgaveFruit", 0.015f, 2, 31)
            },
            83,
            fixtureName,
            ImmersiveChefs.DietaryFlags.Plant,
            false,
            0.0375f));
        var rottable = prepared.GetComp<CompRottable>();
        rottable.RotProgress = 1234.5f;
        GenSpawn.Spawn(prepared, center, map);
        prepared.SetForbidden(false, false);

        Find.CameraDriver.SetRootPosAndSize(
            new UnityEngine.Vector3(center.x + 0.5f, 0f, center.z + 0.5f),
            10f);
        map.mapDrawer.RegenerateEverythingNow();
        Find.Selector.ClearSelection();
        Find.Selector.Select(prepared);
        Messages.Message(
            "Prepared save/reload fixture ready. Save and reload through RimWorld, then inspect the selected stack.",
            MessageTypeDefOf.NeutralEvent,
            false);
        Find.TickManager.Pause();
        return fixtureName + "|" + prepared.ThingID + "|" + center.x + "," + center.z +
               "|Preparation quality 83|RotProgress " + rottable.RotProgress.ToString("0.0");
    }
    catch (System.Exception error)
    {
        Log.Error(
            "[ImmersiveChefsScenario] Prepared-food save/load setup failed at " + stage +
            ": " + error.GetType().Name + ": " + error.Message);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
