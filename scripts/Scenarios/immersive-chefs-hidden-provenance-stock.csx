new System.Func<string>(() =>
{
    var stage = "resolve named pawns";
    try
    {
        var map = Find.CurrentMap;
        if (map == null)
        {
            throw new System.InvalidOperationException(
                "The hidden-provenance fixture has no map.");
        }

        var cook = map.mapPawns.AllPawnsSpawned
            .FirstOrDefault(pawn => pawn.LabelShort == "Hidden Bill Cook");
        var diner = map.mapPawns.AllPawnsSpawned
            .FirstOrDefault(pawn => pawn.LabelShort == "Hidden Policy Diner");
        if (cook == null || diner == null)
        {
            throw new System.InvalidOperationException(
                "The hidden-provenance fixture pawns are unavailable.");
        }

        var billCenter = new IntVec3(cook.Position.x + 5, 0, cook.Position.z + 2);
        var policyCenter = new IntVec3(diner.Position.x + 4, 0, diner.Position.z + 2);
        var preparedDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_PreparedFood");
        var riceDef = DefDatabase<ThingDef>.GetNamed("RawRice");
        var humanMeatDef = DefDatabase<ThingDef>.GetNamed("Meat_Human");

        stage = "create native hidden-source cooking bill";
        var stoveDef = DefDatabase<ThingDef>.GetNamed("FueledStove");
        var stove = ThingMaker.MakeThing(stoveDef, stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
        stove.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(stove, new IntVec3(billCenter.x + 3, 0, billCenter.z + 2), map, Rot4.North);
        var fuel = stove.TryGetComp<CompRefuelable>();
        fuel.Refuel(fuel.Props.fuelCapacity);
        var bill = new Bill_Production(DefDatabase<RecipeDef>.GetNamed("CookMealSimple"));
        bill.repeatMode = BillRepeatModeDefOf.RepeatCount;
        bill.repeatCount = 1;
        bill.ingredientSearchRadius = 12f;
        bill.ingredientFilter.SetDisallowAll();
        bill.ingredientFilter.SetAllow(preparedDef, true);
        bill.ingredientFilter.SetAllow(riceDef, true);
        bill.ingredientFilter.SetAllow(humanMeatDef, false);
        bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        bill.SetPawnRestriction(cook);
        ((IBillGiver)stove).BillStack.AddBill(bill);

        var forbiddenPrepared = (ThingWithComps)ThingMaker.MakeThing(preparedDef);
        forbiddenPrepared.stackCount = 10;
        forbiddenPrepared.GetComp<ImmersiveChefs.CompPreparedFood>().Initialize(
            new ImmersiveChefs.PreparedFoodState(
                new[]
                {
                    new ImmersiveChefs.IngredientContribution(
                        humanMeatDef.defName,
                        0.05f,
                        1,
                        50)
                },
                20,
                null,
                ImmersiveChefs.DietaryFlags.Animal | ImmersiveChefs.DietaryFlags.HumanMeat,
                exactSourcesHidden: true,
                ingredientPoisonChance: 0f));
        var safePrepared = (ThingWithComps)ThingMaker.MakeThing(preparedDef);
        safePrepared.stackCount = 10;
        safePrepared.GetComp<ImmersiveChefs.CompPreparedFood>().Initialize(
            new ImmersiveChefs.PreparedFoodState(
                new[]
                {
                    new ImmersiveChefs.IngredientContribution(
                        riceDef.defName,
                        0.05f,
                        1,
                        50)
                },
                20,
                null,
                ImmersiveChefs.DietaryFlags.Plant |
                    ImmersiveChefs.DietaryFlags.VegetarianCompatible,
                exactSourcesHidden: true,
                ingredientPoisonChance: 0f));
        GenSpawn.Spawn(
            forbiddenPrepared,
            new IntVec3(billCenter.x - 3, 0, billCenter.z - 3),
            map);
        GenSpawn.Spawn(
            safePrepared,
            new IntVec3(billCenter.x + 5, 0, billCenter.z - 3),
            map);

        stage = "create hidden-source food policy choice";
        var forbiddenMeal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        forbiddenMeal.GetComp<ImmersiveChefs.CompCulinaryState>().ReplaceServings(new[]
        {
            new ImmersiveChefs.CulinaryServingRecord(
                50,
                40f,
                ImmersiveChefs.ContaminationSources.None,
                0,
                Find.TickManager.TicksGame,
                new[] { humanMeatDef.defName },
                ImmersiveChefs.DietaryFlags.Animal | ImmersiveChefs.DietaryFlags.HumanMeat)
        });
        var safeMeal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        safeMeal.GetComp<ImmersiveChefs.CompCulinaryState>().ReplaceServings(new[]
        {
            new ImmersiveChefs.CulinaryServingRecord(
                50,
                40f,
                ImmersiveChefs.ContaminationSources.None,
                0,
                Find.TickManager.TicksGame,
                new[] { riceDef.defName },
                ImmersiveChefs.DietaryFlags.Plant |
                    ImmersiveChefs.DietaryFlags.VegetarianCompatible)
        });
        GenSpawn.Spawn(
            forbiddenMeal,
            new IntVec3(policyCenter.x - 3, 0, policyCenter.z - 3),
            map);
        GenSpawn.Spawn(
            safeMeal,
            new IntVec3(policyCenter.x + 5, 0, policyCenter.z - 3),
            map);

        var foodPolicy = new FoodPolicy(9901, "Hidden provenance safety");
        foodPolicy.filter.SetDisallowAll();
        foodPolicy.filter.SetAllow(ThingDefOf.MealSimple, true);
        foodPolicy.filter.SetAllow(riceDef, true);
        foodPolicy.filter.SetAllow(humanMeatDef, false);
        diner.foodRestriction.CurrentFoodPolicy = foodPolicy;

        Find.CameraDriver.SetRootPosAndSize(
            new UnityEngine.Vector3(
                (billCenter.x + policyCenter.x) / 2f + 0.5f,
                0f,
                billCenter.z + 0.5f),
            23f);
        return stove.ThingID + "|" + forbiddenPrepared.ThingID + "|" +
               safePrepared.ThingID + "|" + forbiddenMeal.ThingID + "|" + safeMeal.ThingID;
    }
    catch (System.Exception error)
    {
        Log.Error(
            "[ImmersiveChefsScenario] Hidden provenance stock failed at " + stage +
            ": " + error.GetType().Name + ": " + error.Message);
        throw new System.InvalidOperationException(
            stage + ": " + error.GetType().Name + ": " + error.Message,
            error);
    }
})()
