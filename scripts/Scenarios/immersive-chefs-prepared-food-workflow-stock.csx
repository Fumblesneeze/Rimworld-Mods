new System.Func<string>(() =>
{
    var stage = "resolve named workers";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException("The prepared-food fixture has no map.");
        }

        var prepChef = map.mapPawns.AllPawnsSpawned
            .FirstOrDefault(pawn => pawn.LabelShort == "Prep Chef");
        var preparedCook = map.mapPawns.AllPawnsSpawned
            .FirstOrDefault(pawn => pawn.LabelShort == "Prepared Cook");
        var rawCook = map.mapPawns.AllPawnsSpawned
            .FirstOrDefault(pawn => pawn.LabelShort == "Raw Cook");
        if (prepChef == null || preparedCook == null || rawCook == null)
        {
            throw new System.InvalidOperationException("The prepared-food workers are unavailable.");
        }

        var preparedCenter = new IntVec3(prepChef.Position.x + 6, 0, prepChef.Position.z + 2);
        var rawCenter = new IntVec3(rawCook.Position.x + 6, 0, rawCook.Position.z + 2);
        var rawRice = DefDatabase<ThingDef>.GetNamed("RawRice");
        var preparedFood = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_PreparedFood");
        var prepStationDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_PrepStation");
        var stoveDef = DefDatabase<ThingDef>.GetNamed("FueledStove");
        var cookwareDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cookware");
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");

        stage = "create real preparation bill";
        var prepStation = ThingMaker.MakeThing(prepStationDef);
        prepStation.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(
            prepStation,
            new IntVec3(preparedCenter.x - 3, 0, preparedCenter.z + 2),
            map,
            Rot4.North);
        var prepBill = new Bill_Production(
            DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_PrepareIngredients"));
        prepBill.repeatMode = BillRepeatModeDefOf.RepeatCount;
        prepBill.repeatCount = 2;
        prepBill.ingredientSearchRadius = 12f;
        prepBill.ingredientFilter.SetDisallowAll();
        prepBill.ingredientFilter.SetAllow(rawRice, true);
        prepBill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        prepBill.SetPawnRestriction(prepChef);
        ((IBillGiver)prepStation).BillStack.AddBill(prepBill);

        var prepRice = ThingMaker.MakeThing(rawRice);
        prepRice.stackCount = 20;
        GenSpawn.Spawn(
            prepRice,
            new IntVec3(preparedCenter.x - 6, 0, preparedCenter.z - 4),
            map);

        stage = "create matched native cooking bills";
        var roomCenters = new[] { preparedCenter, rawCenter };
        var cooks = new[] { preparedCook, rawCook };
        var ingredients = new[] { preparedFood, rawRice };
        Thing preparedStove = null;
        Thing rawStove = null;
        for (var index = 0; index < roomCenters.Length; index++)
        {
            var roomCenter = roomCenters[index];
            var stove = ThingMaker.MakeThing(
                stoveDef,
                stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
            stove.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(stove, new IntVec3(roomCenter.x + 3, 0, roomCenter.z + 2), map, Rot4.North);
            var fuel = stove.TryGetComp<CompRefuelable>();
            fuel.Refuel(fuel.Props.fuelCapacity);

            var bill = new Bill_Production(DefDatabase<RecipeDef>.GetNamed("CookMealSimple"));
            bill.repeatMode = BillRepeatModeDefOf.RepeatCount;
            bill.repeatCount = 1;
            bill.ingredientSearchRadius = 12f;
            bill.ingredientFilter.SetDisallowAll();
            bill.ingredientFilter.SetAllow(ingredients[index], true);
            bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
            bill.SetPawnRestriction(cooks[index]);
            ((IBillGiver)stove).BillStack.AddBill(bill);

            var cookware = (ThingWithComps)ThingMaker.MakeThing(cookwareDef, ThingDefOf.Steel);
            cookware.GetComp<CompQuality>()
                .SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
            cookware.GetComp<ImmersiveChefs.CompSanitation>()
                .MarkClean(ImmersiveChefs.WashProvenance.Safe);
            GenSpawn.Spawn(
                cookware,
                new IntVec3(roomCenter.x + 5, 0, roomCenter.z - 4),
                map);

            var plate = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
            plate.GetComp<CompQuality>()
                .SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
            plate.GetComp<ImmersiveChefs.CompSanitation>()
                .MarkClean(ImmersiveChefs.WashProvenance.Safe);
            GenSpawn.Spawn(
                plate,
                new IntVec3(roomCenter.x + 6, 0, roomCenter.z - 4),
                map);

            if (index == 0)
            {
                preparedStove = stove;
            }
            else
            {
                rawStove = stove;
            }
        }

        var rawCookingRice = ThingMaker.MakeThing(rawRice);
        rawCookingRice.stackCount = 20;
        GenSpawn.Spawn(
            rawCookingRice,
            new IntVec3(rawCenter.x - 3, 0, rawCenter.z - 4),
            map);

        return prepStation.ThingID + "|" + preparedStove.ThingID + "|" +
               rawStove.ThingID + "|" + prepRice.ThingID + "|" + rawCookingRice.ThingID;
    }
    catch (System.Exception error)
    {
        Log.Error(
            "[ImmersiveChefsScenario] Prepared-food stock failed at " + stage +
            ": " + error.GetType().Name + ": " + error.Message);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
