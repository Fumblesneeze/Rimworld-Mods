new System.Func<string>(() =>
{
    var stage = "resolve map";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The handheld-food exclusion scenario requires a playable map.");
        }

        Find.TickManager.Pause();
        stage = "find clear fixture area";
        var center = IntVec3.Invalid;
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 60f, true))
        {
            var fixture = new CellRect(candidate.x - 13, candidate.z - 5, 27, 11);
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
                "Could not find a clear area for two isolated handheld-food fixtures.");
        }

        var fixtureRect = new CellRect(center.x - 13, center.z - 5, 27, 11);
        foreach (var cell in fixtureRect.Cells)
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
        }

        var pemmicanCenter = new IntVec3(center.x - 7, 0, center.z);
        var survivalCenter = new IntVec3(center.x + 7, 0, center.z);
        stage = "build mutually unreachable dining rooms";
        foreach (var roomCenter in new[] { pemmicanCenter, survivalCenter })
        {
            var room = CellRect.CenteredOn(roomCenter, 4);
            foreach (var cell in room.EdgeCells)
            {
                var wall = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
                wall.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(wall, cell, map);
            }
        }

        stage = "generate pemmican diner";
        Pawn pemmicanDiner = null;
        for (var attempt = 0; attempt < 32 && pemmicanDiner == null; attempt++)
        {
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist, Faction.OfPlayer, forceGenerateNewPawn: true,
                canGeneratePawnRelations: false, forceNoGear: true));
            if (pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) >= 0.99f &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) >= 0.99f)
            {
                pemmicanDiner = pawn;
                pemmicanDiner.Name = new NameSingle("Pemmican Handheld Diner");
            }
            else
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }

        stage = "generate survival-meal diner";
        Pawn survivalDiner = null;
        for (var attempt = 0; attempt < 32 && survivalDiner == null; attempt++)
        {
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist, Faction.OfPlayer, forceGenerateNewPawn: true,
                canGeneratePawnRelations: false, forceNoGear: true));
            if (pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) >= 0.99f &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) >= 0.99f)
            {
                survivalDiner = pawn;
                survivalDiner.Name = new NameSingle("Survival Meal Handheld Diner");
            }
            else
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }

        if (pemmicanDiner == null || survivalDiner == null)
        {
            throw new System.InvalidOperationException("Could not generate two capable handheld-food diners.");
        }

        pemmicanDiner.inventory.innerContainer.ClearAndDestroyContents();
        survivalDiner.inventory.innerContainer.ClearAndDestroyContents();
        GenSpawn.Spawn(pemmicanDiner, new IntVec3(pemmicanCenter.x, 0, pemmicanCenter.z - 1), map);
        GenSpawn.Spawn(survivalDiner, new IntVec3(survivalCenter.x, 0, survivalCenter.z - 1), map);

        stage = "create exact handheld foods";
        var pemmican = ThingMaker.MakeThing(ThingDefOf.Pemmican);
        pemmican.stackCount = 50;
        GenSpawn.Spawn(pemmican, new IntVec3(pemmicanCenter.x + 2, 0, pemmicanCenter.z), map);
        var survivalMeal = ThingMaker.MakeThing(ThingDefOf.MealSurvivalPack);
        GenSpawn.Spawn(survivalMeal, new IntVec3(survivalCenter.x + 2, 0, survivalCenter.z), map);

        stage = "create untouched service-ware controls";
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        var cutleryDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery");
        var controlIds = new System.Collections.Generic.List<string>();
        foreach (var roomCenter in new[] { pemmicanCenter, survivalCenter })
        {
            var plate = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
            plate.GetComp<CompQuality>()
                .SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
            plate.GetComp<ImmersiveChefs.CompSanitation>()
                .MarkClean(ImmersiveChefs.WashProvenance.Safe);
            GenSpawn.Spawn(plate, new IntVec3(roomCenter.x - 2, 0, roomCenter.z + 1), map);

            var cutlery = (ThingWithComps)ThingMaker.MakeThing(cutleryDef, ThingDefOf.Steel);
            cutlery.GetComp<CompQuality>()
                .SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
            cutlery.GetComp<ImmersiveChefs.CompSanitation>()
                .MarkClean(ImmersiveChefs.WashProvenance.Safe);
            GenSpawn.Spawn(cutlery, new IntVec3(roomCenter.x - 2, 0, roomCenter.z - 1), map);
            controlIds.Add(plate.ThingID);
            controlIds.Add(cutlery.ThingID);
        }

        pemmicanDiner.needs.food.CurLevelPercentage = 0.5f;
        survivalDiner.needs.food.CurLevelPercentage = 0.5f;
        Find.CameraDriver.JumpToCurrentMapLoc(center);
        Find.CameraDriver.SetRootSize(22f);
        Find.Selector.ClearSelection();
        Find.Selector.Select(pemmicanDiner);
        Find.Selector.Select(survivalDiner);
        Find.TickManager.Pause();
        return pemmicanDiner.ThingID + "|" + pemmican.ThingID + "|" +
               survivalDiner.ThingID + "|" + survivalMeal.ThingID + "|" +
               string.Join("|", controlIds.ToArray());
    }
    catch (System.Exception error)
    {
        Log.Error(
            "[ImmersiveChefsScenario] Handheld-food setup failed at " + stage +
            ": " + error.GetType().Name + ": " + error.Message);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
