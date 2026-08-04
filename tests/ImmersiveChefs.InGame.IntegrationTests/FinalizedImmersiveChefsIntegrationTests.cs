using System;
using System.IO;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorldDevGateway.IntegrationTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.InGame.IntegrationTests;

public static class FinalizedImmersiveChefsIntegrationTests
{
    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ImmersiveChefsModInitializedFromTheRealActiveSet()
    {
        IntegrationAssert.True(
            LoadedModManager.RunningModsListForReading.Any(
                mod => string.Equals(
                    mod.PackageId,
                    ImmersiveChefsMod.PackageId,
                    StringComparison.OrdinalIgnoreCase)),
            "Immersive Chefs must be an actually loaded ModContentPack, not merely a referenced assembly.");
        IntegrationAssert.NotNull(
            ImmersiveChefsMod.Integrations,
            "The real Immersive Chefs Mod constructor must initialize its optional-integration snapshot.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void GameplayDefsAreFinalizedAndResearchGated()
    {
        var expectedThings = new[]
        {
            "ImmersiveChefs_Cookware", "ImmersiveChefs_Plate", "ImmersiveChefs_Cutlery",
            "ImmersiveChefs_ChefsKnife", "ImmersiveChefs_Dishwasher",
            "ImmersiveChefs_IndustrialDishwasher", "ImmersiveChefs_PreparedFood",
            "ImmersiveChefs_PrepStation", "ImmersiveChefs_SauceStation",
            "ImmersiveChefs_MeatStation", "ImmersiveChefs_VegetableStation",
            "ImmersiveChefs_PastryStation", "ImmersiveChefs_Microwave"
        };
        foreach (var defName in expectedThings)
        {
            IntegrationAssert.NotNull(DefDatabase<ThingDef>.GetNamedSilentFail(defName), $"Missing ThingDef {defName}.");
        }

        var professionalKitchens = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(
            "ImmersiveChefs_ProfessionalKitchens");
        IntegrationAssert.NotNull(professionalKitchens, "Professional Kitchens research must finalize.");
        var prerequisites = professionalKitchens!.prerequisites.Select(value => value.defName).ToList();
        IntegrationAssert.True(
            prerequisites.Contains("ImmersiveChefs_Dishwashing") && prerequisites.Contains("Machining"),
            "Professional Kitchens must require both Dishwashing and vanilla Machining.");
        IntegrationAssert.NotNull(
            DefDatabase<RecipeDef>.GetNamedSilentFail("ImmersiveChefs_PrepareIngredients"),
            "Prepared-food recipe must finalize.");
        IntegrationAssert.True(
            !DefDatabase<ThingDef>.AllDefsListForReading.Any(def =>
                def.defName.StartsWith("ImmersiveChefs_Ceramic", StringComparison.OrdinalIgnoreCase) ||
                def.defName.StartsWith("ImmersiveChefs_Porcelain", StringComparison.OrdinalIgnoreCase)),
            "Immersive Chefs must not invent ceramic or porcelain content in this release.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FinalizedKitchenwareRecipesUseExactUnitCostsAndMatchingWorkTypes()
    {
        var primitive = DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakePrimitiveCookware");
        var medieval = DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakeMedievalCookware");
        var modern = DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakeModernCookware");

        IntegrationAssert.NotNull(
            medieval.IngredientValueGetter,
            "The finalized medieval cookware recipe must instantiate its ingredient-value getter.");
        IntegrationAssert.Equal(
            typeof(IngredientValueGetter_Units),
            medieval.IngredientValueGetter!.GetType(),
            "Finalized kitchenware recipes must count resource units rather than vanilla Stuff volume.");
        IntegrationAssert.Equal(
            50f,
            medieval.ingredients[0].GetBaseCount(),
            "The finalized medieval cookware recipe must retain its fifty-unit material cost.");
        IntegrationAssert.Equal(
            1f,
            medieval.IngredientValueGetter.ValuePerUnitOf(ThingDefOf.Silver),
            "Small-volume silver must contribute one whole recipe unit per item.");
        IntegrationAssert.Equal(
            WorkTypeDefOf.Crafting,
            primitive.requiredGiverWorkType,
            "Crafting-spot kitchenware must use the Crafting work giver.");
        IntegrationAssert.Equal(
            WorkTypeDefOf.Smithing,
            medieval.requiredGiverWorkType,
            "Smithy kitchenware must use the Smithing work giver.");
        IntegrationAssert.Equal(
            WorkTypeDefOf.Smithing,
            modern.requiredGiverWorkType,
            "Machining kitchenware must use the Smithing work giver.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FinalizedChefsKnifeIsBeltApparelWithoutSanitationState()
    {
        var knife = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_ChefsKnife");
        var recipe = DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_MakeChefsKnife");

        IntegrationAssert.Equal(
            typeof(Apparel),
            knife.thingClass,
            "The chef's knife set must instantiate as apparel rather than weapon equipment.");
        IntegrationAssert.True(knife.IsApparel, "The finalized chef's knife Def must be classified as apparel.");
        IntegrationAssert.Equal(
            "None",
            knife.equipmentType.ToString(),
            "The finalized chef's knife Def must not declare a weapon equipment type.");
        IntegrationAssert.NotNull(knife.apparel, "The chef's knife set must finalize apparel properties.");
        IntegrationAssert.True(
            knife.apparel!.bodyPartGroups.Any(group => group.defName == "Waist") &&
            knife.apparel.layers.Any(layer => layer.defName == "Belt"),
            "The chef's knife set must occupy the waist belt layer.");
        IntegrationAssert.True(
            knife.comps.All(properties => properties.compClass != typeof(CompSanitation)),
            "A personal chef's knife must not acquire mutable dish-sanitation state.");
        IntegrationAssert.True(
            knife.comps.All(properties => properties.compClass != typeof(CompEquippable)),
            "A personal chef's knife must not finalize an equippable weapon component.");
        IntegrationAssert.Equal(
            30f,
            recipe.ingredients[0].GetBaseCount(),
            "The finalized machining recipe must consume thirty units of one eligible metal.");
        IntegrationAssert.True(
            recipe.recipeUsers.Any(user => user.defName == "TableMachining"),
            "The chef's knife recipe must remain on the machining table.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FinalizedGlitterworldCookwareIsTradeOnlyAndSelfCleaning()
    {
        var glitterworld = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_GlitterworldCookware");
        var extension = glitterworld.GetModExtension<KitchenwareExtension>();

        IntegrationAssert.Equal(
            Tradeability.Buyable,
            glitterworld.tradeability,
            "Glitterworld cookware must be eligible for trader stock.");
        IntegrationAssert.True(
            glitterworld.generateCommonality > 0f,
            "Glitterworld cookware must remain eligible for generated trader and quest stock.");
        IntegrationAssert.Equal(
            TechLevel.Spacer,
            glitterworld.techLevel,
            "Glitterworld cookware must retain its imported spacer-tech identity.");
        IntegrationAssert.NotNull(extension, "Glitterworld cookware must finalize its kitchenware extension.");
        IntegrationAssert.True(
            extension!.fixedMaterialKind == KitchenMaterialKind.Glitterworld && extension.selfCleaning,
            "Glitterworld cookware must keep its fixed exceptional material profile and self-cleaning behavior.");
        IntegrationAssert.True(
            !DefDatabase<RecipeDef>.AllDefsListForReading.Any(recipe =>
                recipe.products?.Any(product => product.thingDef == glitterworld) == true),
            "No finalized recipe may manufacture glitterworld cookware.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void OptionalMasonryRecipeMatchesTheRealLoadedModSet()
    {
        var masonryLoaded = LoadedModManager.RunningModsListForReading.Any(mod =>
            string.Equals(
                mod.PackageId,
                "argon.expandedmaterials.masonry",
                StringComparison.OrdinalIgnoreCase));
        var adobeRecipe = DefDatabase<RecipeDef>.GetNamedSilentFail("ImmersiveChefs_MakeAdobePlates");

        if (!masonryLoaded)
        {
            IntegrationAssert.Null(
                adobeRecipe,
                "The fixed adobe recipe must not exist when Expanded Materials - Masonry is absent.");
            return;
        }

        var adobeBricks = DefDatabase<ThingDef>.GetNamedSilentFail("EM_AdobeBricks");
        IntegrationAssert.NotNull(adobeRecipe, "The loaded masonry patch must add the adobe plate recipe.");
        IntegrationAssert.NotNull(adobeBricks, "The real masonry mod must provide EM_AdobeBricks.");
        IntegrationAssert.Null(
            adobeBricks!.stuffProps,
            "EM_AdobeBricks must remain a fixed ingredient rather than being misrepresented as Stuff.");
        IntegrationAssert.True(
            adobeRecipe!.recipeUsers.Any(user => user.defName == "CraftingSpot"),
            "The active adobe recipe must be available at the crafting spot.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FinalizedMealsContainRuntimeStateWithoutReplacingIngredients()
    {
        var meal = DefDatabase<ThingDef>.GetNamed("MealSimple");
        IntegrationAssert.True(meal.comps.Any(comp => comp.compClass == typeof(CompEmbeddedWare)),
            "MealSimple must receive embedded plate state after final Def initialization.");
        IntegrationAssert.True(meal.comps.Any(comp => comp.compClass == typeof(CompCulinaryState)),
            "MealSimple must receive culinary serving state after final Def initialization.");
        IntegrationAssert.True(meal.comps.Any(comp => comp.compClass == typeof(CompIngredients)),
            "MealSimple must retain vanilla CompIngredients for variety compatibility.");
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void ImportedMealPlatingQueueAndDiningGateAreFinalized()
    {
        var job = DefDatabase<JobDef>.GetNamedSilentFail("ImmersiveChefs_PlateMeals");
        var workGiver = DefDatabase<WorkGiverDef>.GetNamedSilentFail("ImmersiveChefs_PlateMeals");
        IntegrationAssert.NotNull(job, "The dedicated imported-meal plating JobDef must finalize.");
        IntegrationAssert.NotNull(workGiver, "The dedicated imported-meal plating WorkGiverDef must finalize.");
        IntegrationAssert.Equal(
            typeof(JobDriver_PlateMeals),
            job!.driverClass,
            "The plating JobDef must use the native plating driver.");
        IntegrationAssert.Equal(
            DefDatabase<WorkTypeDef>.GetNamed("Cooking"),
            workGiver!.workType,
            "Imported-meal plating must be governed by Cooking work.");
        IntegrationAssert.Equal(
            typeof(WorkGiver_PlateMeals),
            workGiver.giverClass,
            "The finalized WorkGiver must scan for imported unplated meals.");

        var diningBoundary = AccessTools.Method(
            typeof(RimWorld.FoodUtility),
            "IsFoodSourceOnMapSociallyProper",
            new[] { typeof(Thing), typeof(Pawn), typeof(Pawn), typeof(bool) });
        IntegrationAssert.NotNull(
            diningBoundary,
            "The vanilla food-selection boundary must exist for the plating gate.");
        IntegrationAssert.True(
            Harmony.GetPatchInfo(diningBoundary!)?.Postfixes.Any(patch =>
                patch.owner == ImmersiveChefsMod.PackageId &&
                patch.PatchMethod?.DeclaringType?.Name == "ImportedMealDiningGatePatch") == true,
            "Immersive Chefs must patch the finalized normal-dining selection boundary.");

        var optimalityBoundary = AccessTools.Method(
            typeof(RimWorld.FoodUtility),
            "FoodOptimality",
            new[] { typeof(Pawn), typeof(Thing), typeof(ThingDef), typeof(float), typeof(bool) });
        IntegrationAssert.NotNull(
            optimalityBoundary,
            "The vanilla food-optimality boundary must exist for plated-meal precedence.");
        IntegrationAssert.True(
            Harmony.GetPatchInfo(optimalityBoundary!)?.Postfixes.Any(patch =>
                patch.owner == ImmersiveChefsMod.PackageId &&
                patch.PatchMethod?.DeclaringType?.Name == "PlatedMealFoodOptimalityPatch") == true,
            "Immersive Chefs must install the finalized plated-meal tie breaker.");
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void DishwasherCapacitiesAreMaterializedAtDefFinalization()
    {
        var expectedScale = ImmersiveChefsMod.Settings.DishwasherCapacityScale;
        var domestic = ImmersiveChefsDefOf.ImmersiveChefs_Dishwasher.comps?
            .OfType<CompProperties_Dishwasher>()
            .SingleOrDefault();
        var industrial = ImmersiveChefsDefOf.ImmersiveChefs_IndustrialDishwasher.comps?
            .OfType<CompProperties_Dishwasher>()
            .SingleOrDefault();

        IntegrationAssert.NotNull(
            domestic,
            "The domestic dishwasher must retain its finalized capacity properties.");
        IntegrationAssert.NotNull(
            industrial,
            "The industrial dishwasher must retain its finalized capacity properties.");
        IntegrationAssert.True(
            Math.Abs(domestic!.basePlateCapacity - (16f * expectedScale)) < 0.001f,
            "The domestic dishwasher must materialize the restart-only scale into its finalized Def.");
        IntegrationAssert.True(
            Math.Abs(industrial!.basePlateCapacity - (64f * expectedScale)) < 0.001f,
            "The industrial dishwasher must materialize the restart-only scale into its finalized Def.");
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void PreparedFoodWorkGiverTargetsOnlyThePrepStation()
    {
        var workGiver = DefDatabase<WorkGiverDef>.GetNamedSilentFail(
            "ImmersiveChefs_PrepareIngredients");
        IntegrationAssert.NotNull(
            workGiver,
            "The dedicated prepared-food WorkGiverDef must finalize.");
        IntegrationAssert.Equal(
            typeof(WorkGiver_DoBill),
            workGiver!.giverClass,
            "Prepared ingredients must use RimWorld's native bill workgiver.");
        IntegrationAssert.Equal(
            DefDatabase<WorkTypeDef>.GetNamed("Cooking"),
            workGiver.workType,
            "Prepared-food bills must remain governed by Cooking work.");
        IntegrationAssert.Equal(
            1,
            workGiver.fixedBillGiverDefs?.Count ?? 0,
            "Prepared-food work must scan exactly one explicit bill-giver Def.");
        IntegrationAssert.Equal(
            "ImmersiveChefs_PrepStation",
            workGiver.fixedBillGiverDefs![0].defName,
            "Prepared-food work must target only the ingredient prep station.");
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void PreparedFoodBillsEvaluateEveryHiddenSourceWithoutChangingOrdinaryFilters()
    {
        var ingredientBoundary = AccessTools.Method(
            typeof(Bill),
            nameof(Bill.IsFixedOrAllowedIngredient),
            new[] { typeof(Thing) });
        IntegrationAssert.True(
            Harmony.GetPatchInfo(ingredientBoundary)?.Owners.Contains(ImmersiveChefsMod.PackageId) == true,
            "The loaded mod must own the prepared-food bill ingredient boundary.");

        var preparedDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_PreparedFood");
        var riceDef = DefDatabase<ThingDef>.GetNamed("RawRice");
        var humanMeatDef = DefDatabase<ThingDef>.GetNamed("Meat_Human");
        var prepared = ThingMaker.MakeThing(preparedDef);
        var preparedComp = (prepared as ThingWithComps)?.GetComp<CompPreparedFood>();
        IntegrationAssert.NotNull(preparedComp, "The finalized prepared-food Def must expose provenance.");
        preparedComp!.Initialize(new PreparedFoodState(
            new[]
            {
                new IngredientContribution(riceDef.defName, 0.025f, 1),
                new IngredientContribution(humanMeatDef.defName, 0.025f, 1)
            },
            preparationQuality: 50,
            preparerThingId: null,
            DietaryFlags.Plant | DietaryFlags.HumanMeat,
            exactSourcesHidden: true,
            ingredientPoisonChance: 0f));

        var recipe = DefDatabase<RecipeDef>.GetNamed("CookMealSimple");
        var ordinaryPrepared = ThingMaker.MakeThing(preparedDef);
        var ordinaryPreparedComp = (ordinaryPrepared as ThingWithComps)?.GetComp<CompPreparedFood>();
        IntegrationAssert.NotNull(
            ordinaryPreparedComp,
            "The ordinary prepared-food fixture must expose provenance.");
        ordinaryPreparedComp!.Initialize(new PreparedFoodState(
            new[] { new IngredientContribution(riceDef.defName, 0.05f, 1) },
            preparationQuality: 50,
            preparerThingId: null,
            DietaryFlags.Plant | DietaryFlags.VegetarianCompatible,
            exactSourcesHidden: false,
            ingredientPoisonChance: 0f));
        var preparedOnlyBill = new Bill_Production(recipe);
        preparedOnlyBill.ingredientFilter.SetDisallowAll();
        preparedOnlyBill.ingredientFilter.SetAllow(preparedDef, true);
        IntegrationAssert.True(
            preparedOnlyBill.IsFixedOrAllowedIngredient(ordinaryPrepared),
            "A bill restricted to visible prepared food must not also require every visible raw source Def.");

        var bill = new Bill_Production(recipe);
        bill.ingredientFilter.SetAllow(preparedDef, true);
        bill.ingredientFilter.SetAllow(riceDef, true);
        bill.ingredientFilter.SetAllow(humanMeatDef, false);

        var ordinaryFilter = new ThingFilter();
        ordinaryFilter.SetAllow(preparedDef, true);
        IntegrationAssert.True(
            ordinaryFilter.Allows(prepared),
            "Prepared provenance must not alter ordinary stockpile-style ThingFilter evaluation.");
        IntegrationAssert.True(
            !bill.IsFixedOrAllowedIngredient(prepared),
            "One hidden disallowed source must reject the whole prepared stack from the bill.");

        bill.ingredientFilter.SetAllow(humanMeatDef, true);
        IntegrationAssert.True(
            bill.IsFixedOrAllowedIngredient(prepared),
            "The same prepared stack must become eligible when every hidden source is allowed.");

        var meal = ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var mealIngredients = (meal as ThingWithComps)?.GetComp<CompIngredients>();
        var culinary = (meal as ThingWithComps)?.GetComp<CompCulinaryState>();
        IntegrationAssert.NotNull(mealIngredients, "A finalized covered meal must expose vanilla ingredients.");
        IntegrationAssert.NotNull(culinary, "A finalized covered meal must expose culinary serving state.");
        culinary!.ReplaceServings(new[]
        {
            new CulinaryServingRecord(
                50,
                40f,
                ContaminationSources.None,
                0,
                0,
                new[] { riceDef.defName, humanMeatDef.defName },
                DietaryFlags.Plant | DietaryFlags.HumanMeat)
        });

        var foodPolicy = new FoodPolicy(9001, "Immersive Chefs integration policy");
        foodPolicy.filter.SetAllow(meal.def, true);
        foodPolicy.filter.SetAllow(riceDef, true);
        foodPolicy.filter.SetAllow(humanMeatDef, false);
        IntegrationAssert.True(
            !foodPolicy.Allows(meal),
            "A finished hidden-source meal must remain forbidden when one source is forbidden.");
        IntegrationAssert.True(
            !mealIngredients!.ingredients.Contains(humanMeatDef),
            "Food-policy evaluation must not reveal hidden source Defs through CompIngredients.");

        foodPolicy.filter.SetAllow(humanMeatDef, true);
        IntegrationAssert.True(
            foodPolicy.Allows(meal),
            "A finished hidden-source meal must become allowed when every source is allowed.");

        var thoughtBoundary = AccessTools.Method(
            typeof(FoodUtility),
            nameof(FoodUtility.ThoughtsFromIngesting),
            new[] { typeof(Pawn), typeof(Thing), typeof(ThingDef) });
        IntegrationAssert.True(
            Harmony.GetPatchInfo(thoughtBoundary)?.Owners.Contains(ImmersiveChefsMod.PackageId) == true,
            "The loaded mod must own the ingestion-thought provenance scope.");
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void HiddenPreparedSourcesDriveVanillaIngredientThoughtsAndRemainHiddenAfterward()
    {
        var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var meal = ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var humanMeatDef = DefDatabase<ThingDef>.GetNamed("Meat_Human");
        var mealIngredients = (meal as ThingWithComps)?.GetComp<CompIngredients>();
        var culinary = (meal as ThingWithComps)?.GetComp<CompCulinaryState>();
        IntegrationAssert.NotNull(mealIngredients, "The thought fixture meal must expose vanilla ingredients.");
        IntegrationAssert.NotNull(culinary, "The thought fixture meal must expose culinary state.");
        culinary!.ReplaceServings(new[]
        {
            new CulinaryServingRecord(
                50,
                40f,
                ContaminationSources.None,
                0,
                0,
                new[] { humanMeatDef.defName },
                DietaryFlags.HumanMeat)
        });
        var ingredientThought = DefDatabase<ThoughtDef>.GetNamed("AteHumanlikeMeatAsIngredient");
        IntegrationAssert.True(
            !mealIngredients!.ingredients.Contains(humanMeatDef),
            "The hidden source must not be present before thought evaluation.");

        try
        {
            var thoughts = FoodUtility.ThoughtsFromIngesting(pawn, meal, meal.def);

            IntegrationAssert.True(
                thoughts.Any(thought => thought.thought == ingredientThought),
                "Vanilla thought evaluation must observe the hidden human-meat source.");
            IntegrationAssert.True(
                !mealIngredients.ingredients.Contains(humanMeatDef),
                "The hidden source must be removed immediately after thought evaluation.");
        }
        finally
        {
            pawn.Destroy(DestroyMode.Vanish);
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void PreparedFoodRoundTripsThroughTheRealScribePipeline()
    {
        var preparedDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_PreparedFood");
        var original = (ThingWithComps)ThingMaker.MakeThing(preparedDef);
        original.stackCount = 7;
        var originalPrepared = original.GetComp<CompPreparedFood>();
        var originalRottable = original.GetComp<CompRottable>();
        IntegrationAssert.NotNull(originalPrepared, "The finalized prepared-food Def must expose provenance.");
        IntegrationAssert.NotNull(originalRottable, "The finalized prepared-food Def must expose rot state.");
        originalPrepared!.Initialize(new PreparedFoodState(
            new[]
            {
                new IngredientContribution("RawRice", 0.035f, 4, 72),
                new IngredientContribution("Meat_Human", 0.015f, 2, 31)
            },
            preparationQuality: 83,
            preparerThingId: "Thing_Preparer4242",
            dietaryFlags: DietaryFlags.Plant | DietaryFlags.Animal | DietaryFlags.HumanMeat,
            exactSourcesHidden: true,
            ingredientPoisonChance: 0.0375f));
        originalRottable!.RotProgress = 1234.5f;

        var path = Path.Combine(
            GenFilePaths.TempFolderPath,
            "immersive-chefs-prepared-food-roundtrip-" + Guid.NewGuid().ToString("N") + ".xml");
        Thing? loaded = null;
        try
        {
            Thing originalForScribe = original;
            try
            {
                Scribe.saver.InitSaving(path, "preparedFoodRoundTrip");
                Scribe_Deep.Look(ref originalForScribe, "thing");
                Scribe.saver.FinalizeSaving();
            }
            catch
            {
                Scribe.saver.ForceStop();
                throw;
            }

            try
            {
                Scribe.loader.InitLoading(path);
                Scribe_Deep.Look(ref loaded, "thing");
                Scribe.loader.FinalizeLoading();
            }
            catch
            {
                Scribe.loader.ForceStop();
                throw;
            }

            var loadedWithComps = loaded as ThingWithComps;
            var loadedPrepared = loadedWithComps?.GetComp<CompPreparedFood>();
            var loadedRottable = loadedWithComps?.GetComp<CompRottable>();
            IntegrationAssert.NotNull(loadedWithComps, "Scribe must reconstruct a real prepared-food Thing.");
            IntegrationAssert.Equal(7, loadedWithComps!.stackCount, "Scribe must preserve the prepared stack count.");
            IntegrationAssert.NotNull(loadedPrepared, "Scribe must reconstruct the prepared-food component.");
            IntegrationAssert.NotNull(loadedRottable, "Scribe must reconstruct the rot component.");
            IntegrationAssert.Equal(83, loadedPrepared!.PreparationQuality);
            IntegrationAssert.Equal("Thing_Preparer4242", loadedPrepared.PreparerThingId);
            IntegrationAssert.Equal(
                DietaryFlags.Plant | DietaryFlags.Animal | DietaryFlags.HumanMeat,
                loadedPrepared.DietaryFlags);
            IntegrationAssert.True(loadedPrepared.ExactSourcesHidden);
            IntegrationAssert.True(Math.Abs(loadedPrepared.IngredientPoisonChance - 0.0375f) < 0.0001f);
            IntegrationAssert.True(Math.Abs(loadedPrepared.NutritionPerItem - 0.05f) < 0.0001f);
            IntegrationAssert.Equal(2, loadedPrepared.Contributions.Count);
            IntegrationAssert.True(
                loadedPrepared.Contributions.Any(value =>
                    value.DefName == "RawRice" &&
                    Math.Abs(value.Nutrition - 0.035f) < 0.0001f &&
                    value.SourceCount == 4 &&
                    value.CraftsmanshipScore == 72));
            IntegrationAssert.True(
                loadedPrepared.Contributions.Any(value =>
                    value.DefName == "Meat_Human" &&
                    Math.Abs(value.Nutrition - 0.015f) < 0.0001f &&
                    value.SourceCount == 2 &&
                    value.CraftsmanshipScore == 31));
            IntegrationAssert.True(Math.Abs(loadedRottable!.RotProgress - 1234.5f) < 0.01f);
            IntegrationAssert.True(
                loadedPrepared.CompInspectStringExtra().Contains("Source: nutrient paste"),
                "The reconstructed hidden-source stack must remain opaque in ordinary inspection.");
        }
        finally
        {
            if (!original.Destroyed)
            {
                original.Destroy(DestroyMode.Vanish);
            }

            if (loaded is not null && !loaded.Destroyed)
            {
                loaded.Destroy(DestroyMode.Vanish);
            }

            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FinalizedTravelFoodsUseTheCoverageContract()
    {
        IntegrationAssert.True(
            MealCoveragePolicy.IsCovered(ThingDefOf.MealSimple),
            "A normal finalized meal must keep Immersive Chefs state while travelling.");
        foreach (var excludedFood in new[] { ThingDefOf.Pemmican, ThingDefOf.MealSurvivalPack })
        {
            IntegrationAssert.True(
                !MealCoveragePolicy.IsCovered(excludedFood),
                $"{excludedFood.defName} must remain a hand-eaten travel-food exclusion.");
            IntegrationAssert.True(
                excludedFood.comps.All(comp => comp.compClass != typeof(CompEmbeddedWare)),
                $"{excludedFood.defName} must not receive embedded serving ware.");
            IntegrationAssert.True(
                excludedFood.comps.All(comp => comp.compClass != typeof(CompCulinaryState)),
                $"{excludedFood.defName} must not receive culinary state or temperature handling.");
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FinalizedRecipeWorkUsesOnlyTheExactComplexityTable()
    {
        var workAmountMethod = AccessTools.Method(typeof(RecipeDef), nameof(RecipeDef.WorkAmountForStuff));
        var ownedPostfixes = Harmony.GetPatchInfo(workAmountMethod)?.Postfixes
            .Count(patch => patch.owner == ImmersiveChefsMod.PackageId) ?? 0;
        IntegrationAssert.Equal(
            1,
            ownedPostfixes,
            "Recipe work amount must have exactly one Immersive Chefs Harmony postfix.");

        foreach (var defName in new[] { "CookMealSimple", "CookMealSimpleBulk" })
        {
            AssertWorkMultiplier(defName, 0.75f);
        }

        foreach (var defName in new[]
                 {
                     "CookMealFine", "CookMealFine_Veg", "CookMealFine_Meat",
                     "CookMealFineBulk", "CookMealFineBulk_Meat", "CookMealFineBulk_Veg"
                 })
        {
            AssertWorkMultiplier(defName, 2f);
        }

        foreach (var defName in new[]
                 {
                     "CookMealLavish", "CookMealLavish_Meat", "CookMealLavish_Veg",
                     "CookMealLavishBulk", "CookMealLavishBulk_Veg", "CookMealLavishBulk_Meat"
                 })
        {
            AssertWorkMultiplier(defName, 3f);
        }

        AssertWorkMultiplier("CookMealSurvival", 1f);
        AssertWorkMultiplier("Make_Pemmican", 1f);

        var vanillaCookingExpandedLoaded = LoadedModManager.RunningModsListForReading.Any(mod =>
            string.Equals(mod.PackageId, "vanillaexpanded.vcooke", StringComparison.OrdinalIgnoreCase));
        var unclassifiedBake = DefDatabase<RecipeDef>.GetNamedSilentFail("VCE_CookBakeSimple");
        if (vanillaCookingExpandedLoaded)
        {
            IntegrationAssert.NotNull(
                unclassifiedBake,
                "The active Vanilla Cooking Expanded matrix must finalize its simple-bake recipe.");
            AssertWorkMultiplier("VCE_CookBakeSimple", 1f);
        }
        else
        {
            IntegrationAssert.Null(
                unclassifiedBake,
                "The base matrix must not invent a Vanilla Cooking Expanded recipe.");
        }
    }

    private static void AssertWorkMultiplier(string defName, float expectedMultiplier)
    {
        var recipe = DefDatabase<RecipeDef>.GetNamed(defName);
        var actualMultiplier = RecipeWorkRuntime.MultiplierFor(recipe);
        IntegrationAssert.True(
            Math.Abs(actualMultiplier - expectedMultiplier) < 0.0001f,
            $"{defName} must retain the exact {expectedMultiplier:0.##}x complexity multiplier.");
        var baseWorkAmount = recipe.workAmount >= 0f
            ? recipe.workAmount
            : recipe.products[0].thingDef.GetStatValueAbstract(StatDefOf.WorkToMake, null);
        var actualWorkAmount = recipe.WorkAmountForStuff(null);
        var expectedWorkAmount = baseWorkAmount * expectedMultiplier;
        IntegrationAssert.True(
            Math.Abs(actualWorkAmount - expectedWorkAmount) < 0.01f,
            $"{defName} must expose its Harmony-adjusted finalized work amount; " +
            $"expected {expectedWorkAmount:0.##} from base {baseWorkAmount:0.##}, actual {actualWorkAmount:0.##}.");
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void CaravanIngestionReturnsTheExactWareWashedInWildWater()
    {
        var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var caravan = CaravanMaker.MakeCaravan(
            new[] { pawn },
            Faction.OfPlayer,
            Find.CurrentMap.Tile,
            addToWorldPawnsIfNotAlready: true);
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var cutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        var originalPlateId = plate.ThingID;
        var originalCutleryId = cutlery.ThingID;

        try
        {
            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            cutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            var thermalStartTick = Math.Max(
                0,
                Find.TickManager.TicksGame - ThermalCalculator.TicksPerHour);
            meal.GetComp<CompCulinaryState>().ReplaceServings(new[]
            {
                new CulinaryServingRecord(
                    60,
                    70f,
                    ContaminationSources.None,
                    0,
                    thermalStartTick)
            });
            IntegrationAssert.True(
                pawn.inventory.innerContainer.TryAdd(meal, canMergeWithExistingStacks: false),
                "The travel fixture must put its meal in the caravan inventory.");
            IntegrationAssert.True(
                pawn.inventory.innerContainer.TryAdd(plate, canMergeWithExistingStacks: false),
                "The travel fixture must put its plate in the caravan inventory.");
            IntegrationAssert.True(
                pawn.inventory.innerContainer.TryAdd(cutlery, canMergeWithExistingStacks: false),
                "The travel fixture must put its cutlery in the caravan inventory.");

            var tileAmbient = GenTemperature.GetTemperatureAtTile(caravan.Tile);
            var expectedTemperature = ThermalCalculator.TemperatureAfter(
                70f,
                tileAmbient,
                Find.TickManager.TicksGame - thermalStartTick,
                ImmersiveChefsMod.Settings.ThermalHalfLifeHours);
            var travelServing = meal.GetComp<CompCulinaryState>().PeekCurrentServing();
            IntegrationAssert.True(
                Math.Abs(meal.AmbientTemperature - tileAmbient) < 0.01f,
                "A held caravan meal must resolve RimWorld's current world-tile ambient temperature.");
            IntegrationAssert.True(
                travelServing is not null && Math.Abs(travelServing.TemperatureCelsius - expectedTemperature) < 0.01f,
                $"Caravan meal temperature must continue moving toward the current world-tile climate " +
                $"(expected {expectedTemperature:0.###}, actual {travelServing?.TemperatureCelsius:0.###}, " +
                $"ambient {tileAmbient:0.###}).");

            meal.Ingested(pawn, 0.9f);
            caravan.RecacheInventory();

            var returnedPlate = caravan.AllThings.SingleOrDefault(thing => thing.ThingID == originalPlateId);
            var returnedCutlery = caravan.AllThings.SingleOrDefault(thing => thing.ThingID == originalCutleryId);
            IntegrationAssert.True(
                ReferenceEquals(plate, returnedPlate),
                "Caravan dining must return the exact selected plate Thing without replacement or duplication.");
            IntegrationAssert.True(
                ReferenceEquals(cutlery, returnedCutlery),
                "Caravan dining must return the exact selected cutlery Thing without replacement or duplication.");
            IntegrationAssert.Equal(
                WashProvenance.WildWater,
                plate.GetComp<CompSanitation>().WashProvenance,
                "Travel-washed plates must retain the wild-water risk marker.");
            IntegrationAssert.Equal(
                WashProvenance.WildWater,
                cutlery.GetComp<CompSanitation>().WashProvenance,
                "Travel-washed cutlery must retain the wild-water risk marker.");
        }
        finally
        {
            if (!caravan.Destroyed)
            {
                caravan.Destroy();
            }

            if (!pawn.Destroyed)
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void CaravanIngestionReturnsTheExactEmbeddedPlateOnlyAfterEating()
    {
        var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var caravan = CaravanMaker.MakeCaravan(
            new[] { pawn },
            Faction.OfPlayer,
            Find.CurrentMap.Tile,
            addToWorldPawnsIfNotAlready: true);
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var cutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);

        try
        {
            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            cutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            IntegrationAssert.True(
                meal.GetComp<CompEmbeddedWare>().TryEmbedPlate(plate),
                "The fixture must begin with its exact plate contained by the meal.");
            IntegrationAssert.True(
                pawn.inventory.innerContainer.TryAdd(meal, canMergeWithExistingStacks: false),
                "The fixture must put its plated meal in caravan inventory.");
            IntegrationAssert.True(
                pawn.inventory.innerContainer.TryAdd(cutlery, canMergeWithExistingStacks: false),
                "The fixture must put its cutlery in caravan inventory.");
            IntegrationAssert.True(
                !pawn.inventory.innerContainer.Contains(plate),
                "An embedded plate must not be a direct loose caravan inventory item before eating.");

            meal.Ingested(pawn, 0.9f);
            caravan.RecacheInventory();

            IntegrationAssert.True(
                caravan.AllThings.Any(thing => ReferenceEquals(thing, plate)),
                "Eating must move the exact plate out of the consumed meal and into caravan inventory.");
            IntegrationAssert.True(
                caravan.AllThings.Any(thing => ReferenceEquals(thing, cutlery)),
                "Eating must return the exact selected cutlery to caravan inventory.");
            IntegrationAssert.Equal(
                WashProvenance.WildWater,
                plate.GetComp<CompSanitation>().WashProvenance,
                "The returned embedded plate must receive caravan wild-water wash provenance.");
        }
        finally
        {
            if (!caravan.Destroyed)
            {
                caravan.Destroy();
            }

            if (!pawn.Destroyed)
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void CancelledCaravanIngestionRestoresUnusedWareWithoutWashing()
    {
        var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var caravan = CaravanMaker.MakeCaravan(
            new[] { pawn },
            Faction.OfPlayer,
            Find.CurrentMap.Tile,
            addToWorldPawnsIfNotAlready: true);
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var cutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);

        try
        {
            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.WildWater);
            cutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            meal.GetComp<CompCulinaryState>().ReplaceServings(new[]
            {
                new CulinaryServingRecord(
                    55,
                    20f,
                    ContaminationSources.DirtyCookware,
                    0,
                    Find.TickManager.TicksGame)
            });
            pawn.inventory.innerContainer.TryAdd(meal, canMergeWithExistingStacks: false);
            pawn.inventory.innerContainer.TryAdd(plate, canMergeWithExistingStacks: false);
            pawn.inventory.innerContainer.TryAdd(cutlery, canMergeWithExistingStacks: false);

            DiningSessionRegistry.TryAttachTravel(pawn, meal);
            IntegrationAssert.True(
                ReferenceEquals(meal.GetComp<CompEmbeddedWare>().PeekPlateThing(), plate),
                "The cancellation fixture must import its exact loose caravan plate before rollback.");
            DiningSessionRegistry.BeginIngestion(pawn);
            DiningSessionRegistry.EndIngestion(pawn);
            caravan.RecacheInventory();

            IntegrationAssert.True(
                meal.GetComp<CompEmbeddedWare>().PeekPlateThing() is null,
                "A cancelled travel attempt must detach the plate it imported into an unplated meal.");
            IntegrationAssert.True(
                caravan.AllThings.Any(thing => ReferenceEquals(thing, plate)),
                "A cancelled travel attempt must return the exact unused plate.");
            IntegrationAssert.True(
                caravan.AllThings.Any(thing => ReferenceEquals(thing, cutlery)),
                "A cancelled travel attempt must return the exact unused cutlery.");
            IntegrationAssert.Equal(
                ContaminationSources.DirtyCookware,
                meal.GetComp<CompCulinaryState>().PeekCurrentServing()!.Contamination,
                "A cancelled travel attempt must leave the uneaten meal's prior contamination unchanged.");
            IntegrationAssert.True(
                !plate.GetComp<CompSanitation>().IsDirty,
                "Cancellation must preserve a clean unused plate's sanitation state.");
            IntegrationAssert.Equal(
                WashProvenance.WildWater,
                plate.GetComp<CompSanitation>().WashProvenance,
                "Cancellation must preserve the unused plate's prior wild-water provenance.");
            IntegrationAssert.Equal(
                WashProvenance.Safe,
                cutlery.GetComp<CompSanitation>().WashProvenance,
                "Cancellation must not claim that the unused cutlery was washed in wild water.");
        }
        finally
        {
            if (!caravan.Destroyed)
            {
                caravan.Destroy();
            }

            if (!pawn.Destroyed)
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void CaravanAnimalsDoNotUseOrDestroyTableware()
    {
        var animal = PawnGenerator.GeneratePawn(PawnKindDefOf.Muffalo, Faction.OfPlayer);
        var caravan = CaravanMaker.MakeCaravan(
            new[] { animal },
            Faction.OfPlayer,
            Find.CurrentMap.Tile,
            addToWorldPawnsIfNotAlready: true);
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var cutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        var settings = ImmersiveChefsMod.Settings;
        var originalCulinaryQualityEnabled = settings.CulinaryQualityEnabled;
        var originalMealTemperatureEnabled = settings.MealTemperatureEnabled;
        var originalFoodPoisoningEffectScale = settings.FoodPoisoningEffectScale;
        var originalMaximumCustomPoisonChance = settings.MaximumCustomPoisonChance;
        var originalMicrowaveExtraPoisonChance = settings.MicrowaveExtraPoisonChance;

        try
        {
            settings.CulinaryQualityEnabled = true;
            settings.MealTemperatureEnabled = true;
            settings.FoodPoisoningEffectScale = 3f;
            settings.MaximumCustomPoisonChance = 1f;
            settings.MicrowaveExtraPoisonChance = 5f;
            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            cutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            meal.GetComp<CompCulinaryState>().ReplaceServings(new[]
            {
                new CulinaryServingRecord(
                    0,
                    -20f,
                    ContaminationSources.DirtyCookware |
                    ContaminationSources.DirtyPlate |
                    ContaminationSources.DirtyCutlery |
                    ContaminationSources.WildWaterCookware |
                    ContaminationSources.WildWaterPlate |
                    ContaminationSources.WildWaterCutlery,
                    20,
                    Find.TickManager.TicksGame)
            });
            AccessTools.Field(typeof(CompFoodPoisonable), "poisonPct")
                .SetValue(meal.GetComp<CompFoodPoisonable>(), 0f);
            IntegrationAssert.True(
                meal.GetComp<CompEmbeddedWare>().TryEmbedPlate(plate),
                "The animal exclusion fixture must start with a plated meal.");
            animal.inventory.innerContainer.TryAdd(meal, canMergeWithExistingStacks: false);
            animal.inventory.innerContainer.TryAdd(cutlery, canMergeWithExistingStacks: false);

            meal.Ingested(animal, 0.9f);
            caravan.RecacheInventory();

            IntegrationAssert.True(
                caravan.AllThings.Any(thing => ReferenceEquals(thing, plate)),
                "An animal eating a meal must return its exact unused plate to caravan inventory.");
            IntegrationAssert.True(
                caravan.AllThings.Any(thing => ReferenceEquals(thing, cutlery)),
                "Animal ingestion must not select or consume caravan cutlery.");
            IntegrationAssert.Equal(
                WashProvenance.Safe,
                plate.GetComp<CompSanitation>().WashProvenance,
                "An animal-excluded plate must retain its original wash provenance.");
            IntegrationAssert.Equal(
                WashProvenance.Safe,
                cutlery.GetComp<CompSanitation>().WashProvenance,
                "Animal-excluded cutlery must retain its original wash provenance.");
            IntegrationAssert.True(
                animal.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.FoodPoisoning) is null,
                "Animal ingestion must not apply Immersive Chefs' custom food-poisoning risk.");
        }
        finally
        {
            settings.CulinaryQualityEnabled = originalCulinaryQualityEnabled;
            settings.MealTemperatureEnabled = originalMealTemperatureEnabled;
            settings.FoodPoisoningEffectScale = originalFoodPoisoningEffectScale;
            settings.MaximumCustomPoisonChance = originalMaximumCustomPoisonChance;
            settings.MicrowaveExtraPoisonChance = originalMicrowaveExtraPoisonChance;
            if (!caravan.Destroyed)
            {
                caravan.Destroy();
            }

            if (!animal.Destroyed)
            {
                animal.Destroy(DestroyMode.Vanish);
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void MapAnimalsDoNotReserveUseOrDirtyTableware()
    {
        var map = Find.CurrentMap;
        var animal = PawnGenerator.GeneratePawn(
            DefDatabase<PawnKindDef>.GetNamed("Raccoon"),
            null);
        var animalCell = map.AllCells
            .Where(cell => cell.Standable(map) &&
                           cell.GetEdifice(map) is null &&
                           cell.GetThingList(map).Count == 0)
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .First(cell =>
            {
                var adjacent = new IntVec3(cell.x + 1, 0, cell.z);
                return adjacent.x < map.Size.x &&
                       adjacent.Standable(map) &&
                       adjacent.GetEdifice(map) is null &&
                       adjacent.GetThingList(map).Count == 0;
            });
        var mealCell = animalCell;
        var cutleryCell = new IntVec3(animalCell.x + 1, 0, animalCell.z);
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Plasteel);
        var cutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        var settings = ImmersiveChefsMod.Settings;
        var originalCulinaryQualityEnabled = settings.CulinaryQualityEnabled;
        var originalMealTemperatureEnabled = settings.MealTemperatureEnabled;
        var originalFoodPoisoningEffectScale = settings.FoodPoisoningEffectScale;
        var originalMaximumCustomPoisonChance = settings.MaximumCustomPoisonChance;

        try
        {
            settings.CulinaryQualityEnabled = true;
            settings.MealTemperatureEnabled = true;
            settings.FoodPoisoningEffectScale = 3f;
            settings.MaximumCustomPoisonChance = 1f;
            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            cutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            meal.GetComp<CompCulinaryState>().ReplaceServings(new[]
            {
                new CulinaryServingRecord(
                    0,
                    -20f,
                    ContaminationSources.DirtyCookware |
                    ContaminationSources.DirtyPlate |
                    ContaminationSources.DirtyCutlery,
                    20,
                    Find.TickManager.TicksGame)
            });
            AccessTools.Field(typeof(CompFoodPoisonable), "poisonPct")
                .SetValue(meal.GetComp<CompFoodPoisonable>(), 0f);
            IntegrationAssert.True(
                meal.GetComp<CompEmbeddedWare>().TryEmbedPlate(plate),
                "The map animal exclusion fixture must start with an exact embedded plate.");

            GenSpawn.Spawn(animal, animalCell, map);
            GenSpawn.Spawn(meal, mealCell, map);
            GenSpawn.Spawn(cutlery, cutleryCell, map);
            animal.needs.food.CurLevel = 0.01f;
            var originalCutleryPosition = cutlery.Position;
            var ingestJob = JobMaker.MakeJob(JobDefOf.Ingest, meal);
            var cutleryWasReserved = false;

            animal.jobs.StartJob(ingestJob, JobCondition.InterruptForced);
            var chewMethod = AccessTools.Method(typeof(Toils_Ingest), nameof(Toils_Ingest.ChewIngestible));
            var chewOwners = Harmony.GetPatchInfo(chewMethod)?.Owners
                .Count(owner => owner == ImmersiveChefsMod.PackageId) ?? 0;
            IntegrationAssert.Equal(
                1,
                chewOwners,
                "Animal map ingestion must run with exactly one Immersive Chefs chew-speed patch owner.");
            var plateSpeed = plate.GetComp<CompKitchenwareStats>().CurrentStats.CookingSpeedFactor;
            IntegrationAssert.True(
                Math.Abs(plateSpeed - 1f) > 0.1f,
                "The animal chew-speed fixture must use a plate with a distinguishable non-native factor.");
            var platedChew = Toils_Ingest.ChewIngestible(
                animal,
                1f,
                TargetIndex.A,
                TargetIndex.None);
            var embedded = meal.GetComp<CompEmbeddedWare>();
            var releasedPlate = embedded.ReleasePlateThing();
            IntegrationAssert.True(
                ReferenceEquals(plate, releasedPlate),
                "The chew-speed fixture must temporarily release the exact embedded plate.");
            var unplatedChew = Toils_Ingest.ChewIngestible(
                animal,
                1f,
                TargetIndex.A,
                TargetIndex.None);
            IntegrationAssert.True(
                embedded.TryEmbedPlate(plate),
                "The chew-speed fixture must restore its exact plate before native ingestion.");
            IntegrationAssert.Equal(
                unplatedChew.defaultDuration,
                platedChew.defaultDuration,
                "A non-humanlike animal's native chew duration must ignore a distinguishable plate speed factor.");
            for (var tick = 0; tick < 5000 && !meal.Destroyed; tick++)
            {
                animal.jobs.JobTrackerTick();
                cutleryWasReserved |= map.reservationManager.IsReserved(cutlery);
            }

            IntegrationAssert.True(
                meal.Destroyed,
                "A real animal JobDriver_Ingest must complete within the bounded fixture ticks.");
            IntegrationAssert.True(
                DiningSessionRegistry.CutleryFor(ingestJob) is null,
                "The real animal map-ingest job must not select nearby cutlery.");
            IntegrationAssert.True(
                DiningSessionRegistry.PlateFor(ingestJob) is null,
                "The real animal map-ingest job must not create a service-ware pickup session.");
            IntegrationAssert.True(
                !cutleryWasReserved,
                "The real animal map-ingest job must never reserve nearby cutlery.");

            IntegrationAssert.True(
                plate.Spawned && ReferenceEquals(plate.Map, map) && plate.Position == animal.Position,
                "Animal map ingestion must recover the exact embedded plate at the eating location.");
            IntegrationAssert.Equal(
                WashProvenance.Safe,
                plate.GetComp<CompSanitation>().WashProvenance,
                "The recovered animal plate must remain clean with unchanged safe provenance.");
            IntegrationAssert.True(
                !plate.GetComp<CompSanitation>().IsDirty,
                "The recovered animal plate must retain its clean sanitation flag.");
            IntegrationAssert.True(
                cutlery.Spawned && ReferenceEquals(cutlery.Map, map) &&
                cutlery.Position == originalCutleryPosition,
                "Nearby map cutlery must remain spawned at its original cell.");
            IntegrationAssert.Equal(
                WashProvenance.Safe,
                cutlery.GetComp<CompSanitation>().WashProvenance,
                "Nearby map cutlery must remain clean and untouched.");
            IntegrationAssert.True(
                !cutlery.GetComp<CompSanitation>().IsDirty,
                "Nearby map cutlery must retain its clean sanitation flag.");
            IntegrationAssert.True(
                animal.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.FoodPoisoning) is null,
                "Animal map ingestion must not apply Immersive Chefs' custom food-poisoning risk.");
        }
        finally
        {
            settings.CulinaryQualityEnabled = originalCulinaryQualityEnabled;
            settings.MealTemperatureEnabled = originalMealTemperatureEnabled;
            settings.FoodPoisoningEffectScale = originalFoodPoisoningEffectScale;
            settings.MaximumCustomPoisonChance = originalMaximumCustomPoisonChance;
            if (!meal.Destroyed)
            {
                meal.Destroy(DestroyMode.Vanish);
            }

            if (!plate.Destroyed)
            {
                plate.Destroy(DestroyMode.Vanish);
            }

            if (!cutlery.Destroyed)
            {
                cutlery.Destroy(DestroyMode.Vanish);
            }

            if (!animal.Destroyed)
            {
                animal.Destroy(DestroyMode.Vanish);
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void ActiveBiotechIndependentChildCompletesOrdinaryDiningWorkflow()
    {
        var biotechActive = LoadedModManager.RunningModsListForReading.Any(mod =>
            string.Equals(mod.PackageId, "ludeon.rimworld.biotech", StringComparison.OrdinalIgnoreCase));
        if (!biotechActive)
        {
            return;
        }

        var map = Find.CurrentMap;
        var fixtureCells = map.AllCells
            .Where(cell => cell.Standable(map) &&
                           cell.GetEdifice(map) is null &&
                           cell.GetThingList(map).Count == 0)
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .Take(2)
            .ToList();
        IntegrationAssert.Equal(2, fixtureCells.Count, "The loaded child fixture needs two clear map cells.");
        var fixtureCell = fixtureCells[0];
        var child = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            Faction.OfPlayer,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false,
            fixedBiologicalAge: 8f,
            fixedChronologicalAge: 8f,
            developmentalStages: DevelopmentalStage.Child,
            forceNoGear: true));
        var toddler = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            Faction.OfPlayer,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false,
            fixedBiologicalAge: 2f,
            fixedChronologicalAge: 2f,
            developmentalStages: DevelopmentalStage.Baby,
            forceNoGear: true));
        var meal = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("MealLavish"));
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Gold);
        var cutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Gold);
        var settings = ImmersiveChefsMod.Settings;
        var originalWareRequirementMode = settings.WareRequirementMode;
        var originalCulinaryQualityEnabled = settings.CulinaryQualityEnabled;
        var originalMealTemperatureEnabled = settings.MealTemperatureEnabled;
        var preexistingFoods = map.listerThings.AllThings
            .Where(thing => thing.Spawned && thing.def.IsNutritionGivingIngestible)
            .Select(thing => new { Thing = thing, Forbidden = thing.IsForbidden(Faction.OfPlayer) })
            .ToList();
        var preexistingCutlery = map.listerThings.AllThings
            .Where(thing => thing.def.GetModExtension<KitchenwareExtension>()?.product ==
                            KitchenwareProduct.Cutlery)
            .Select(thing => new { Thing = thing, Forbidden = thing.IsForbidden(Faction.OfPlayer) })
            .ToList();

        try
        {
            settings.WareRequirementMode = WareRequirementMode.Prefer;
            settings.CulinaryQualityEnabled = true;
            settings.MealTemperatureEnabled = true;
            foreach (var existing in preexistingFoods)
            {
                existing.Thing.SetForbidden(true, warnOnFail: false);
            }

            foreach (var existing in preexistingCutlery)
            {
                existing.Thing.SetForbidden(true, warnOnFail: false);
            }

            child.Name = new NameSingle("Loaded Independent Child Diner");
            toddler.Name = new NameSingle("Loaded Toddler Requiring Feeding");
            child.inventory.innerContainer.ClearAndDestroyContents();
            toddler.inventory.innerContainer.ClearAndDestroyContents();
            IntegrationAssert.Equal(
                DevelopmentalStage.Child,
                child.DevelopmentalStage,
                "Biotech must generate a real child rather than an adult with a child label.");
            IntegrationAssert.True(
                child.RaceProps.Humanlike && child.needs?.food is not null && child.jobs is not null,
                "The real child pawn must expose the ordinary self-feeding trackers.");
            IntegrationAssert.Equal(
                DevelopmentalStage.Baby,
                toddler.DevelopmentalStage,
                "Biotech must generate a real toddler-age baby rather than a self-feeding child.");
            var childFood = child.needs!.food!;
            var childJobs = child.jobs!;

            plate.GetComp<CompQuality>().SetQuality(QualityCategory.Excellent, ArtGenerationContext.Colony);
            cutlery.GetComp<CompQuality>().SetQuality(QualityCategory.Excellent, ArtGenerationContext.Colony);
            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            cutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            meal.GetComp<CompCulinaryState>().ReplaceServings(new[]
            {
                new CulinaryServingRecord(
                    95,
                    70f,
                    ContaminationSources.None,
                    0,
                    Find.TickManager.TicksGame)
            });
            IntegrationAssert.True(
                meal.GetComp<CompEmbeddedWare>().TryEmbedPlate(plate),
                "The child's lavish meal must begin with its exact clean plate embedded.");

            GenSpawn.Spawn(child, fixtureCell, map);
            GenSpawn.Spawn(toddler, fixtureCells[1], map);
            GenSpawn.Spawn(meal, fixtureCell, map);
            GenSpawn.Spawn(cutlery, fixtureCell, map);
            childFood.CurLevelPercentage = 0.15f;
            toddler.needs!.food!.CurLevelPercentage = 0.15f;
            var mealId = meal.ThingID;
            var plateId = plate.ThingID;
            var cutleryId = cutlery.ThingID;

            var childFoodGiver = child.thinker.TryGetMainTreeThinkNode<JobGiver_GetFood>();
            IntegrationAssert.NotNull(
                childFoodGiver,
                "A real Biotech child must inherit the vanilla humanlike self-feeding job giver.");
            var childFoodResult = child.thinker.MainThinkNodeRoot.TryIssueJobPackage(child, default);
            IntegrationAssert.True(
                childFoodResult.IsValid && childFoodResult.Job.def == JobDefOf.Ingest &&
                childFoodResult.SourceNode is JobGiver_GetFood &&
                ReferenceEquals(childFoodResult.Job.GetTarget(TargetIndex.A).Thing, meal),
                "The child's full vanilla think tree must choose the exact plated fixture meal through JobGiver_GetFood.");
            IntegrationAssert.True(
                toddler.thinker.TryGetMainTreeThinkNode<JobGiver_GetFood>() is null,
                "A toddler-age baby must keep Biotech's assisted-feeding think tree without self-feeding jobs.");
            var toddlerThinkResult = toddler.thinker.MainThinkNodeRoot.TryIssueJobPackage(toddler, default);
            IntegrationAssert.True(
                !toddlerThinkResult.IsValid || toddlerThinkResult.Job.def != JobDefOf.Ingest,
                "Biotech's toddler think tree must not issue an ordinary self-feeding ingest job.");

            var ingestJob = childFoodResult.Job;
            childJobs.StartJob(
                ingestJob,
                JobCondition.InterruptForced,
                childFoodResult.SourceNode,
                thinkTree: child.thinker.MainThinkTree);
            IntegrationAssert.Equal(
                JobDefOf.Ingest,
                child.CurJobDef,
                "Vanilla must accept an ordinary ingest job for the independent child.");
            IntegrationAssert.True(
                ReferenceEquals(DiningSessionRegistry.CutleryFor(ingestJob), cutlery),
                "Starting the real ingest job must reserve the exact clean fixture cutlery.");
            DiningSessionRegistry.Pickup(child);
            IntegrationAssert.True(
                ReferenceEquals(cutlery.holdingOwner, child.inventory.innerContainer),
                "The child dining session must acquire the exact clean cutlery before eating.");

            meal.Ingested(child, 0.9f);

            IntegrationAssert.True(meal.Destroyed, "The actual RimWorld ingestion boundary must consume the meal.");
            IntegrationAssert.Equal(mealId, meal.ThingID, "The native job must consume the exact fixture meal.");
            IntegrationAssert.True(
                plate.Spawned && plate.ThingID == plateId && ReferenceEquals(plate.Map, map),
                "The exact embedded plate must return to the map after the child eats.");
            IntegrationAssert.True(
                cutlery.Spawned && cutlery.ThingID == cutleryId && ReferenceEquals(cutlery.Map, map),
                "The exact acquired cutlery must return to the map after the child eats.");
            IntegrationAssert.True(
                plate.GetComp<CompSanitation>().IsDirty && cutlery.GetComp<CompSanitation>().IsDirty,
                "The child's returned plate and cutlery must both become dirty through actual dining.");

            var diningThought = DefDatabase<ThoughtDef>.GetNamed("ImmersiveChefs_DiningExperience");
            var culinaryThought = DefDatabase<ThoughtDef>.GetNamed("ImmersiveChefs_CulinaryQuality");
            var temperatureThought = DefDatabase<ThoughtDef>.GetNamed("ImmersiveChefs_MealTemperature");
            IntegrationAssert.Equal(
                0,
                child.needs.mood.thoughts.memories.GetFirstMemoryOfDef(diningThought)?.CurStageIndex ?? -1,
                "The child must receive the same proper-place-setting memory as an adult.");
            IntegrationAssert.Equal(
                6,
                child.needs.mood.thoughts.memories.GetFirstMemoryOfDef(culinaryThought)?.CurStageIndex ?? -1,
                "The child must receive the serving's legendary culinary-quality memory.");
            IntegrationAssert.Equal(
                0,
                child.needs.mood.thoughts.memories.GetFirstMemoryOfDef(temperatureThought)?.CurStageIndex ?? -1,
                "The child must receive the steaming-hot meal memory.");
        }
        finally
        {
            settings.WareRequirementMode = originalWareRequirementMode;
            settings.CulinaryQualityEnabled = originalCulinaryQualityEnabled;
            settings.MealTemperatureEnabled = originalMealTemperatureEnabled;
            foreach (var existing in preexistingFoods)
            {
                if (!existing.Thing.Destroyed)
                {
                    existing.Thing.SetForbidden(existing.Forbidden, warnOnFail: false);
                }
            }

            foreach (var existing in preexistingCutlery)
            {
                if (!existing.Thing.Destroyed)
                {
                    existing.Thing.SetForbidden(existing.Forbidden, warnOnFail: false);
                }
            }

            foreach (var thing in new Thing[] { meal, plate, cutlery, child, toddler })
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void CompletedMapDiningWithoutCutleryCreatesOneDirtEvent()
    {
        var map = Find.CurrentMap;
        var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var diningCell = map.AllCells
            .Where(cell => cell.Standable(map) &&
                           cell.GetEdifice(map) is null &&
                           cell.GetThingList(map).All(thing => thing.def != ThingDefOf.Filth_Dirt))
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .First(cell => FilthMaker.CanMakeFilth(cell, map, ThingDefOf.Filth_Dirt));
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var settings = ImmersiveChefsMod.Settings;
        var originalWareRequirementMode = settings.WareRequirementMode;
        var preexistingCutlery = map.listerThings.AllThings
            .Where(thing => thing.def.GetModExtension<KitchenwareExtension>()?.product ==
                            KitchenwareProduct.Cutlery)
            .Select(thing => new { Thing = thing, Forbidden = thing.IsForbidden(Faction.OfPlayer) })
            .ToList();
        var preexistingDirt = map.listerThings.ThingsOfDef(ThingDefOf.Filth_Dirt)
            .Cast<Filth>()
            .ToDictionary(filth => filth, filth => filth.thickness);

        try
        {
            settings.WareRequirementMode = WareRequirementMode.Prefer;
            foreach (var existing in preexistingCutlery)
            {
                existing.Thing.SetForbidden(true, warnOnFail: false);
            }

            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            meal.GetComp<CompCulinaryState>().ReplaceServings(new[]
            {
                new CulinaryServingRecord(
                    50,
                    35f,
                    ContaminationSources.None,
                    0,
                    Find.TickManager.TicksGame)
            });
            IntegrationAssert.True(
                meal.GetComp<CompEmbeddedWare>().TryEmbedPlate(plate),
                "The missing-cutlery fixture must start with an exact embedded plate.");

            GenSpawn.Spawn(pawn, diningCell, map);
            GenSpawn.Spawn(meal, diningCell, map);
            pawn.drafter.Drafted = true;
            pawn.needs.food.CurLevel = 0.01f;
            var dirtBefore = map.listerThings.ThingsOfDef(ThingDefOf.Filth_Dirt)
                .Cast<Filth>()
                .Sum(filth => filth.thickness);
            var ingestJob = JobMaker.MakeJob(JobDefOf.Ingest, meal);
            pawn.jobs.StartJob(ingestJob, JobCondition.InterruptForced);

            for (var tick = 0; tick < 5000 && !meal.Destroyed; tick++)
            {
                pawn.jobs.JobTrackerTick();
            }

            IntegrationAssert.True(
                meal.Destroyed,
                "A real colonist JobDriver_Ingest must complete within the bounded fixture ticks.");
            var dirtAfter = map.listerThings.ThingsOfDef(ThingDefOf.Filth_Dirt)
                .Cast<Filth>()
                .Sum(filth => filth.thickness);
            IntegrationAssert.Equal(
                dirtBefore + 1,
                dirtAfter,
                "Completed eligible map dining without cutlery must add exactly one dirt thickness.");
            IntegrationAssert.True(
                pawn.Position.GetThingList(map).Any(thing => thing.def == ThingDefOf.Filth_Dirt),
                "The native dirt event must occur at the diner's actual final eating location.");

            var diningThought = DefDatabase<ThoughtDef>.GetNamed("ImmersiveChefs_DiningExperience");
            var memory = pawn.needs.mood.thoughts.memories.GetFirstMemoryOfDef(diningThought);
            IntegrationAssert.NotNull(memory, "The diner must receive the combined dining thought.");
            IntegrationAssert.Equal(
                1,
                memory!.CurStageIndex,
                "A plated meal without cutlery must select the missing-cutlery thought stage.");
        }
        finally
        {
            settings.WareRequirementMode = originalWareRequirementMode;
            foreach (var existing in preexistingCutlery)
            {
                if (!existing.Thing.Destroyed)
                {
                    existing.Thing.SetForbidden(existing.Forbidden, warnOnFail: false);
                }
            }

            foreach (var filth in map.listerThings.ThingsOfDef(ThingDefOf.Filth_Dirt)
                         .Cast<Filth>()
                         .ToList())
            {
                if (!preexistingDirt.TryGetValue(filth, out var originalThickness))
                {
                    filth.Destroy(DestroyMode.Vanish);
                    continue;
                }

                while (!filth.Destroyed && filth.thickness > originalThickness)
                {
                    filth.ThinFilth();
                }
            }

            if (!meal.Destroyed)
            {
                meal.Destroy(DestroyMode.Vanish);
            }

            if (!plate.Destroyed)
            {
                plate.Destroy(DestroyMode.Vanish);
            }

            if (!pawn.Destroyed)
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void NativePatientFeedingUsesCutleryAndAssignsConsequencesToThePatient()
    {
        var map = Find.CurrentMap;
        var bedCell = map.AllCells
            .Where(cell => GenAdj.OccupiedRect(cell, Rot4.North, ThingDefOf.Bed.size)
                .Cells.All(occupied => occupied.x >= 0 && occupied.z >= 0 &&
                                       occupied.x < map.Size.x && occupied.z < map.Size.z &&
                                       occupied.Standable(map) &&
                                       occupied.GetEdifice(map) is null) &&
                           FilthMaker.CanMakeFilth(
                               BedUtility.GetSleepingSlotPos(0, cell, Rot4.North, ThingDefOf.Bed.size),
                               map,
                               ThingDefOf.Filth_Dirt))
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .First();
        var bed = (Building_Bed)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog);
        bed.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(bed, bedCell, map, Rot4.North);
        bed.Medical = true;

        var patient = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var feeder = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var patientStart = bed.GetSleepingSlotPos(0);
        var feederStart = patientStart;
        GenSpawn.Spawn(patient, patientStart, map);
        GenSpawn.Spawn(feeder, feederStart, map);

        var injuryPart = patient.health.hediffSet.GetNotMissingParts()
            .First(part => part.def == BodyPartDefOf.Torso);
        var injury = HediffMaker.MakeHediff(HediffDefOf.Cut, patient, injuryPart);
        injury.Severity = 0.1f;
        patient.health.AddHediff(injury);
        IntegrationAssert.True(
            HealthAIUtility.ShouldSeekMedicalRest(patient),
            "The conscious assisted-feeding fixture must genuinely require medical rest.");
        var layDown = JobMaker.MakeJob(JobDefOf.LayDown, bed);
        layDown.restUntilHealed = true;
        patient.jobs.StartJob(layDown, JobCondition.InterruptForced);
        for (var tick = 0; tick < 2000 && !patient.InBed(); tick++)
        {
            patient.jobs.JobTrackerTick();
        }

        IntegrationAssert.True(patient.InBed(), "The native patient must actually occupy the medical bed.");
        IntegrationAssert.True(patient.Awake(), "The first assisted-feeding pass must use a conscious patient.");

        var settings = ImmersiveChefsMod.Settings;
        var originalWareRequirementMode = settings.WareRequirementMode;
        var preexistingCutlery = map.listerThings.AllThings
            .Where(thing => thing.def.GetModExtension<KitchenwareExtension>()?.product ==
                            KitchenwareProduct.Cutlery)
            .Select(thing => new { Thing = thing, Forbidden = thing.IsForbidden(Faction.OfPlayer) })
            .ToList();
        var preexistingDirt = map.listerThings.ThingsOfDef(ThingDefOf.Filth_Dirt)
            .Cast<Filth>()
            .ToDictionary(filth => filth, filth => filth.thickness);
        var createdThings = new System.Collections.Generic.List<Thing> { bed, patient, feeder };
        var diningThought = DefDatabase<ThoughtDef>.GetNamed("ImmersiveChefs_DiningExperience");

        try
        {
            settings.WareRequirementMode = WareRequirementMode.Prefer;
            foreach (var existing in preexistingCutlery)
            {
                existing.Thing.SetForbidden(true, warnOnFail: false);
            }

            var cutlery = (ThingWithComps)ThingMaker.MakeThing(
                DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
                ThingDefOf.Steel);
            cutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            var interruptedCutleryCell = map.AllCells
                .Where(cell => cell.Standable(map) &&
                               cell.GetEdifice(map) is null &&
                               feeder.CanReach(cell, PathEndMode.Touch, Danger.Some) &&
                               cell.DistanceToSquared(feeder.Position) >= 9)
                .OrderBy(cell => cell.DistanceToSquared(feeder.Position))
                .First();
            GenSpawn.Spawn(cutlery, interruptedCutleryCell, map);
            createdThings.Add(cutlery);

            var interruptedMeal = CreatePatientMeal(out var interruptedPlate);
            createdThings.Add(interruptedMeal);
            createdThings.Add(interruptedPlate);
            GenSpawn.Spawn(interruptedMeal, patient.Position, map);
            var interruptedJob = JobMaker.MakeJob(JobDefOf.FeedPatient, interruptedMeal, patient);
            interruptedJob.count = 1;
            interruptedJob.SetTarget(TargetIndex.C, feeder);
            feeder.jobs.StartJob(interruptedJob, JobCondition.InterruptForced);
            var interruptedSession = DiningSessionRegistry.Current(patient);
            IntegrationAssert.True(
                ReferenceEquals(interruptedSession?.Cutlery, cutlery),
                "The interruption fixture must select the exact reachable cutlery.");
            IntegrationAssert.True(
                map.reservationManager.ReservedBy(cutlery, feeder, interruptedJob),
                "The interruption fixture must reserve the selected cutlery for the native FeedPatient job.");
            feeder.jobs.JobTrackerTick();

            IntegrationAssert.True(
                ReferenceEquals(interruptedJob.GetTarget(TargetIndex.C).Thing, feeder),
                "Starting tableware pickup must not borrow FeedPatient's native food-holder target C.");
            IntegrationAssert.True(
                ReferenceEquals(feeder.CurJob, interruptedJob) &&
                feeder.pather.Moving &&
                ReferenceEquals(feeder.pather.Destination.Thing, cutlery),
                "The interruption fixture must have an active native path to the selected cutlery.");
            IntegrationAssert.True(
                cutlery.Spawned && !feeder.inventory.innerContainer.Contains(cutlery),
                "The interruption fixture must stop while the feeder is pathing to reserved cutlery.");
            feeder.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: false);
            IntegrationAssert.True(
                cutlery.Spawned && cutlery.Position == interruptedCutleryCell,
                "Interrupted patient feeding must leave unused cutlery at its map position.");
            IntegrationAssert.True(
                !cutlery.GetComp<CompSanitation>().IsDirty,
                "Interrupted patient feeding must leave unused cutlery clean.");
            IntegrationAssert.True(
                !map.reservationManager.IsReserved(cutlery),
                "Interrupted patient feeding must release the cutlery reservation.");
            interruptedMeal.Destroy(DestroyMode.Vanish);
            cutlery.DeSpawn(DestroyMode.Vanish);
            GenSpawn.Spawn(cutlery, patient.Position, map);

            var microwaveCell = map.AllCells
                .Select(cell => new
                {
                    Cell = cell,
                    Interaction = new IntVec3(cell.x, cell.y, cell.z - 1)
                })
                .Where(candidate => candidate.Cell.Standable(map) &&
                                    candidate.Cell.GetEdifice(map) is null &&
                                    candidate.Interaction.x >= 0 &&
                                    candidate.Interaction.z >= 0 &&
                                    candidate.Interaction.x < map.Size.x &&
                                    candidate.Interaction.z < map.Size.z &&
                                    candidate.Interaction.Standable(map) &&
                                    candidate.Interaction.GetEdifice(map) is null &&
                                    feeder.CanReach(
                                        candidate.Interaction,
                                        PathEndMode.OnCell,
                                        Danger.Some))
                .OrderBy(candidate => candidate.Cell.DistanceToSquared(patient.Position))
                .First()
                .Cell;
            var microwave = ThingMaker.MakeThing(
                DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Microwave"));
            GenSpawn.Spawn(microwave, microwaveCell, map, Rot4.North);
            microwave.TryGetComp<CompPowerTrader>().PowerOn = true;
            createdThings.Add(microwave);

            var microwaveMeal = CreatePatientMeal(out var microwavePlate);
            microwaveMeal.GetComp<CompCulinaryState>().ReplaceServings(new[]
            {
                new CulinaryServingRecord(
                    50,
                    -5f,
                    ContaminationSources.None,
                    0,
                    Find.TickManager.TicksGame)
            });
            createdThings.Add(microwaveMeal);
            createdThings.Add(microwavePlate);
            GenSpawn.Spawn(microwaveMeal, patient.Position, map);
            settings.WareRequirementMode = WareRequirementMode.Off;
            var microwaveJob = JobMaker.MakeJob(JobDefOf.FeedPatient, microwaveMeal, patient);
            microwaveJob.count = 1;
            microwaveJob.SetTarget(TargetIndex.C, feeder);
            feeder.jobs.StartJob(microwaveJob, JobCondition.InterruptForced);
            var microwaveSession = DiningSessionRegistry.Current(patient);
            IntegrationAssert.True(
                ReferenceEquals(microwaveSession?.Microwave, microwave),
                "The cold assisted meal must select the real powered microwave.");

            var microwaveDriver = feeder.jobs.curDriver;
            var microwaveToils = microwaveDriver is null
                ? null
                : Traverse.Create(microwaveDriver)
                    .Field("toils")
                    .GetValue<System.Collections.Generic.List<Toil>>();
            var heatingToils = microwaveToils?
                .Where(toil => toil.defaultCompleteMode == ToilCompleteMode.Delay &&
                               toil.defaultDuration == microwave.TryGetComp<CompMicrowave>().HeatingTicks)
                .ToList() ?? new System.Collections.Generic.List<Toil>();
            IntegrationAssert.Equal(
                1,
                heatingToils.Count,
                "The real patched FeedPatient driver must contain exactly one captured-microwave heating toil.");
            var heatingToil = heatingToils[0];
            IntegrationAssert.True(
                ReferenceEquals(microwaveJob.GetTarget(TargetIndex.C).Thing, feeder),
                "Microwave routing must not borrow FeedPatient's native target C.");
            IntegrationAssert.True(
                heatingToil.handlingFacing && heatingToil.tickAction is not null,
                "Microwave heating must visibly keep the feeder facing the captured appliance.");
            IntegrationAssert.True(
                heatingToil.finishActions?.Count > 0,
                "Microwave heating must retain a visible progress effect with cleanup.");

            microwaveDriver!.JumpToToil(heatingToil);
            IntegrationAssert.Equal(
                microwaveToils!.IndexOf(heatingToil),
                microwaveDriver.CurToilIndex,
                "The real FeedPatient driver must enter the captured-microwave heating toil.");
            microwaveDriver.DriverTick();

            var closure = heatingToil.tickAction!.Target;
            var effecterField = closure?.GetType()
                .GetFields(
                    System.Reflection.BindingFlags.Instance |
                    System.Reflection.BindingFlags.Public |
                    System.Reflection.BindingFlags.NonPublic)
                .SingleOrDefault(field => typeof(Effecter).IsAssignableFrom(field.FieldType));
            var progressEffecter = effecterField?.GetValue(closure) as Effecter;
            var progressBar = progressEffecter?.children.OfType<SubEffecter_ProgressBar>().SingleOrDefault();
            IntegrationAssert.True(
                progressBar?.mote is { Spawned: true },
                "Active microwave heating must spawn a visible progress mote on the captured appliance.");

            var facingCell = feeder.Rotation.FacingCell;
            var microwaveDeltaX = microwave.Position.x - feeder.Position.x;
            var microwaveDeltaZ = microwave.Position.z - feeder.Position.z;
            IntegrationAssert.True(
                facingCell.x * microwaveDeltaX + facingCell.z * microwaveDeltaZ > 0,
                "Active microwave heating must face the feeder toward the captured appliance.");

            feeder.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: false);
            IntegrationAssert.True(
                progressBar!.mote.DestroyedOrNull(),
                "Interrupting active microwave heating must clean up its progress mote.");
            settings.WareRequirementMode = WareRequirementMode.Prefer;

            var servedMeal = CreatePatientMeal(out var servedPlate);
            createdThings.Add(servedMeal);
            createdThings.Add(servedPlate);
            GenSpawn.Spawn(servedMeal, patient.Position, map);
            patient.needs.food.CurLevel = 0.01f;
            var dirtBeforeServed = DirtThickness(map);
            var carriedCutlery = RunNativeFeed(feeder, patient, servedMeal, cutlery);

            IntegrationAssert.True(
                carriedCutlery,
                "The native feeder must carry the exact reserved cutlery in their inventory before feeding.");
            IntegrationAssert.True(
                cutlery.Spawned && cutlery.Position.DistanceToSquared(patient.Position) <= 4,
                "Completed assisted feeding must drop the exact cutlery beside the patient, not the feeder.");
            IntegrationAssert.True(
                cutlery.GetComp<CompSanitation>().IsDirty,
                "The cutlery used by the native feeder must become dirty after the patient eats.");
            IntegrationAssert.Equal(
                dirtBeforeServed,
                DirtThickness(map),
                "Feeding with cutlery must not create the missing-cutlery dirt event.");
            IntegrationAssert.True(
                feeder.needs.mood.thoughts.memories.GetFirstMemoryOfDef(diningThought) is null,
                "The feeder must never receive the patient's dining memory.");

            cutlery.Destroy(DestroyMode.Vanish);
            servedPlate.Destroy(DestroyMode.Vanish);
            patient.needs.mood.thoughts.memories.RemoveMemoriesOfDef(diningThought);

            var consciousMeal = CreatePatientMeal(out var consciousPlate);
            createdThings.Add(consciousMeal);
            createdThings.Add(consciousPlate);
            GenSpawn.Spawn(consciousMeal, patient.Position, map);
            patient.needs.food.CurLevel = 0.01f;
            var dirtBeforeConscious = DirtThickness(map);
            RunNativeFeed(feeder, patient, consciousMeal, expectedCutlery: null);

            IntegrationAssert.Equal(
                dirtBeforeConscious + 1,
                DirtThickness(map),
                "A completed conscious feed without cutlery must create one vanilla dirt thickness.");
            var consciousMemory = patient.needs.mood.thoughts.memories.GetFirstMemoryOfDef(diningThought);
            IntegrationAssert.NotNull(consciousMemory, "The conscious patient must own the dining memory.");
            IntegrationAssert.Equal(
                1,
                consciousMemory!.CurStageIndex,
                "A conscious plated patient fed without cutlery must receive the missing-cutlery stage.");
            IntegrationAssert.True(
                feeder.needs.mood.thoughts.memories.GetFirstMemoryOfDef(diningThought) is null,
                "The conscious patient's feeder must not receive a missing-cutlery memory.");

            consciousPlate.Destroy(DestroyMode.Vanish);
            patient.needs.mood.thoughts.memories.RemoveMemoriesOfDef(diningThought);
            var anesthetic = patient.health.AddHediff(HediffDefOf.Anesthetic);
            IntegrationAssert.True(!patient.Awake(), "The final assisted-feeding pass must use an unconscious patient.");
            IntegrationAssert.True(
                !patient.health.capacities.CanBeAwake,
                "The unconscious fixture must be medically incapable of consciousness, not merely asleep.");
            IntegrationAssert.True(patient.InBed(), "The unconscious patient must remain in the native medical bed.");

            var unconsciousMeal = CreatePatientMeal(out var unconsciousPlate);
            createdThings.Add(unconsciousMeal);
            createdThings.Add(unconsciousPlate);
            GenSpawn.Spawn(unconsciousMeal, patient.Position, map);
            patient.needs.food.CurLevel = 0.01f;
            var dirtBeforeUnconscious = DirtThickness(map);
            RunNativeFeed(feeder, patient, unconsciousMeal, expectedCutlery: null);

            IntegrationAssert.Equal(
                dirtBeforeUnconscious + 1,
                DirtThickness(map),
                "An unconscious patient fed without cutlery must still create the physical dirt event.");
            IntegrationAssert.True(
                patient.needs.mood.thoughts.memories.GetFirstMemoryOfDef(diningThought) is null,
                "An unconscious patient must not receive the missing-cutlery dining memory.");
            IntegrationAssert.True(
                feeder.needs.mood.thoughts.memories.GetFirstMemoryOfDef(diningThought) is null,
                "The unconscious patient's feeder must not receive the dining memory either.");
            patient.health.RemoveHediff(anesthetic);
        }
        finally
        {
            settings.WareRequirementMode = originalWareRequirementMode;
            foreach (var existing in preexistingCutlery)
            {
                if (!existing.Thing.Destroyed)
                {
                    existing.Thing.SetForbidden(existing.Forbidden, warnOnFail: false);
                }
            }

            foreach (var filth in map.listerThings.ThingsOfDef(ThingDefOf.Filth_Dirt)
                         .Cast<Filth>()
                         .ToList())
            {
                if (!preexistingDirt.TryGetValue(filth, out var originalThickness))
                {
                    filth.Destroy(DestroyMode.Vanish);
                    continue;
                }

                while (!filth.Destroyed && filth.thickness > originalThickness)
                {
                    filth.ThinFilth();
                }
            }

            foreach (var thing in createdThings.AsEnumerable().Reverse())
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }

    private static ThingWithComps CreatePatientMeal(out ThingWithComps plate)
    {
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        plate.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
        meal.GetComp<CompCulinaryState>().ReplaceServings(new[]
        {
            new CulinaryServingRecord(
                50,
                35f,
                ContaminationSources.None,
                0,
                Find.TickManager.TicksGame)
        });
        IntegrationAssert.True(
            meal.GetComp<CompEmbeddedWare>().TryEmbedPlate(plate),
            "Every patient-feeding fixture meal must start with its exact clean embedded plate.");
        return meal;
    }

    private static bool RunNativeFeed(
        Pawn feeder,
        Pawn patient,
        Thing meal,
        Thing? expectedCutlery)
    {
        var job = JobMaker.MakeJob(JobDefOf.FeedPatient, meal, patient);
        job.count = 1;
        var carriedCutlery = false;
        feeder.jobs.StartJob(job, JobCondition.InterruptForced);
        var session = DiningSessionRegistry.Current(patient);
        IntegrationAssert.NotNull(
            session,
            "Starting the real FeedPatient job must attach a dining session to the patient.");
        IntegrationAssert.True(
            session!.IsAssisted && ReferenceEquals(session.CarrierPawn, feeder),
            "The patient must own the dining outcome while the feeder owns tableware transport.");
        IntegrationAssert.True(
            ReferenceEquals(expectedCutlery, session.Cutlery),
            expectedCutlery is null
                ? "A no-cutlery patient feed must not retain tableware from an earlier feed."
                : "The assisted dining session must reserve the exact expected cutlery.");
        for (var tick = 0; tick < 6000 && !meal.Destroyed; tick++)
        {
            feeder.jobs.JobTrackerTick();
            carriedCutlery |= expectedCutlery is not null &&
                               feeder.inventory.innerContainer.Contains(expectedCutlery);
        }

        IntegrationAssert.True(
            meal.Destroyed,
            "The real JobDriver_FoodFeedPatient must complete within the bounded fixture ticks.");
        return carriedCutlery;
    }

    private static int DirtThickness(Map map) =>
        map.listerThings.ThingsOfDef(ThingDefOf.Filth_Dirt)
            .Cast<Filth>()
            .Sum(filth => filth.thickness);

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void FeederCleanupDuringAssistedIngestionPreservesThePatientLifecycle()
    {
        var map = Find.CurrentMap;
        var fixtureCell = map.AllCells
            .Where(cell => cell.Standable(map) && cell.GetEdifice(map) is null)
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .First();
        var feeder = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var patient = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var meal = CreatePatientMeal(out var plate);
        var cutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        var settings = ImmersiveChefsMod.Settings;
        var originalWareRequirementMode = settings.WareRequirementMode;
        var preexistingCutlery = map.listerThings.AllThings
            .Where(thing => thing.def.GetModExtension<KitchenwareExtension>()?.product ==
                            KitchenwareProduct.Cutlery)
            .Select(thing => new { Thing = thing, Forbidden = thing.IsForbidden(Faction.OfPlayer) })
            .ToList();

        try
        {
            settings.WareRequirementMode = WareRequirementMode.Prefer;
            foreach (var existing in preexistingCutlery)
            {
                existing.Thing.SetForbidden(true, warnOnFail: false);
            }

            cutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            GenSpawn.Spawn(feeder, fixtureCell, map);
            GenSpawn.Spawn(patient, fixtureCell, map);
            GenSpawn.Spawn(meal, fixtureCell, map);
            GenSpawn.Spawn(cutlery, fixtureCell, map);
            var job = JobMaker.MakeJob(JobDefOf.FeedPatient, meal, patient);
            IntegrationAssert.True(
                DiningSessionRegistry.TryAttachAssisted(feeder, patient, job, meal),
                "The regression fixture must attach the assisted dining session.");
            var session = DiningSessionRegistry.Current(patient);
            IntegrationAssert.NotNull(session, "The patient must own the attached dining session.");
            IntegrationAssert.True(
                ReferenceEquals(session!.Cutlery, cutlery),
                "The regression fixture must reserve its exact cutlery.");
            session.PickupCutlery();
            DiningSessionRegistry.BeginIngestion(patient);

            DiningSessionRegistry.Cleanup(feeder, job);

            IntegrationAssert.True(
                ReferenceEquals(DiningSessionRegistry.Current(patient), session),
                "Feeder cleanup during Thing.Ingested must not cancel the patient's active lifecycle.");
            IntegrationAssert.True(
                feeder.inventory.innerContainer.Contains(cutlery),
                "Nested feeder cleanup must not prematurely return the patient's in-use cutlery.");
            DiningSessionRegistry.Complete(patient);
            IntegrationAssert.True(
                cutlery.Spawned && cutlery.GetComp<CompSanitation>().IsDirty,
                "Patient completion after nested cleanup must still dirty and return the exact cutlery.");
        }
        finally
        {
            settings.WareRequirementMode = originalWareRequirementMode;
            feeder.ClearAllReservations(releaseDestinationsOnlyIfObsolete: false);
            foreach (var existing in preexistingCutlery)
            {
                if (!existing.Thing.Destroyed)
                {
                    existing.Thing.SetForbidden(existing.Forbidden, warnOnFail: false);
                }
            }

            foreach (var thing in new Thing[] { meal, plate, cutlery, patient, feeder })
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void SanitationStorageFiltersAreFinalizedAgainstKitchenware()
    {
        var cleanFilter = DefDatabase<SpecialThingFilterDef>.GetNamedSilentFail(
            "ImmersiveChefs_AllowCleanKitchenware");
        var dirtyFilter = DefDatabase<SpecialThingFilterDef>.GetNamedSilentFail(
            "ImmersiveChefs_AllowDirtyKitchenware");
        IntegrationAssert.NotNull(cleanFilter, "The clean kitchenware storage filter must finalize.");
        IntegrationAssert.NotNull(dirtyFilter, "The dirty kitchenware storage filter must finalize.");

        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        IntegrationAssert.NotNull(cleanFilter!.Worker, "The clean filter worker must instantiate.");
        IntegrationAssert.NotNull(dirtyFilter!.Worker, "The dirty filter worker must instantiate.");
        IntegrationAssert.True(
            cleanFilter.allowedByDefault && dirtyFilter.allowedByDefault,
            "Both sanitation filters must preserve vanilla storage behavior until a player disables one.");
        IntegrationAssert.True(
            cleanFilter.Worker.CanEverMatch(plateDef),
            "The finalized clean filter must recognize the plate Def.");
        IntegrationAssert.True(
            dirtyFilter.Worker.CanEverMatch(plateDef),
            "The finalized dirty filter must recognize the plate Def.");
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void UnpoweredDishwasherRejectsDirtyWareAtAdmission()
    {
        var map = Find.CurrentMap;
        var createdThings = new System.Collections.Generic.List<Thing>();
        var dishwasher = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Dishwasher"));
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var battery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("Battery"));
        var previousPreference = ImmersiveChefsMod.Settings.PreferDishwashers;

        try
        {
            var sanitation = plate.GetComp<CompSanitation>();
            var power = dishwasher.GetComp<CompPowerTrader>();
            var dishwasherComp = dishwasher.GetComp<CompDishwasher>();
            var processorType = AccessTools.TypeByName("ProcessorFramework.CompProcessor");
            var processorControlled = processorType is not null &&
                                      dishwasher.AllComps.Any(comp => processorType.IsInstanceOfType(comp));
            var processorWorkGiverType = processorControlled
                ? AccessTools.TypeByName("ProcessorFramework.WorkGiver_FillProcessor")
                : null;
            var processorWorkGiver = processorWorkGiverType is null
                ? null
                : Activator.CreateInstance(processorWorkGiverType);
            IntegrationAssert.NotNull(sanitation, "The finalized plate must expose sanitation state.");
            IntegrationAssert.NotNull(power, "The finalized dishwasher must expose its required power comp.");
            IntegrationAssert.NotNull(dishwasherComp, "The finalized dishwasher must expose its local cycle comp.");
            sanitation.MarkDirty();

            var fixtureCenter = map.AllCells
                .Where(cell => IsEmptyFixtureArea(map, cell, 4))
                .OrderBy(cell => cell.DistanceToSquared(map.Center))
                .First();
            var conduitDef = DefDatabase<ThingDef>.GetNamed("PowerConduit");
            for (var x = fixtureCenter.x - 3; x <= fixtureCenter.x + 3; x++)
            {
                var conduit = ThingMaker.MakeThing(conduitDef);
                conduit.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(conduit, new IntVec3(x, 0, fixtureCenter.z + 2), map);
                createdThings.Add(conduit);
            }

            dishwasher.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(
                dishwasher,
                new IntVec3(fixtureCenter.x - 2, 0, fixtureCenter.z + 2),
                map,
                Rot4.North);
            createdThings.Add(dishwasher);
            battery.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(
                battery,
                new IntVec3(fixtureCenter.x + 2, 0, fixtureCenter.z + 2),
                map,
                Rot4.North);
            createdThings.Add(battery);
            GenSpawn.Spawn(plate, new IntVec3(fixtureCenter.x, 0, fixtureCenter.z - 2), map);
            createdThings.Add(plate);
            plate.SetForbidden(true, warnOnFail: false);
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false));
            pawn.Name = new NameSingle("Dishwasher Test Worker");
            pawn.inventory.innerContainer.ClearAndDestroyContents();
            GenSpawn.Spawn(pawn, fixtureCenter, map);
            pawn.drafter.Drafted = true;
            createdThings.Add(pawn);
            var batteryComp = battery.GetComp<CompPowerBattery>();
            var flick = dishwasher.GetComp<CompFlickable>();
            IntegrationAssert.NotNull(batteryComp, "The real power fixture must expose battery storage.");
            IntegrationAssert.NotNull(flick, "The finalized dishwasher must expose its native power switch.");
            batteryComp.SetStoredEnergyPct(1f);
            map.powerNetManager.UpdatePowerNetsAndConnections_First();
            IntegrationAssert.NotNull(
                power.PowerNet,
                "The spawned dishwasher must attach to a real RimWorld power net.");
            IntegrationAssert.True(
                ReferenceEquals(power.PowerNet, batteryComp.PowerNet),
                "The spawned dishwasher and charged battery must share the same RimWorld power net.");
            for (var tick = 0; tick <= 200 && !power.PowerOn; tick++)
            {
                Find.TickManager.DoSingleTick();
            }
            IntegrationAssert.True(
                power.PowerOn,
                string.Format(
                    System.Globalization.CultureInfo.InvariantCulture,
                    "The charged connected battery must power the dishwasher through RimWorld's real power net " +
                    "(switch={0}, stored={1}, powerComps={2}, batteries={3}, activeSource={4}, output={5}).",
                    flick.SwitchIsOn,
                    batteryComp.StoredEnergy,
                    power.PowerNet.powerComps.Count,
                    power.PowerNet.batteryComps.Count,
                    power.PowerNet.HasActivePowerSource,
                    power.PowerOutput));

            flick.DoFlick();
            IntegrationAssert.True(
                !flick.SwitchIsOn && !power.PowerOn,
                "Using the native switch must leave the connected dishwasher genuinely unpowered.");
            IntegrationAssert.True(
                !dishwasherComp.CanAccept(plate),
                "An unpowered dishwasher must be ineligible for admission and Doing dishes selection.");

            ImmersiveChefsMod.Settings.PreferDishwashers = true;
            plate.SetForbidden(false, warnOnFail: false);
            var foundUnpowered = WorkGiver_DoDishes.TryFindDestination(pawn, plate, out var unpowered);
            IntegrationAssert.True(
                !foundUnpowered || !ReferenceEquals(unpowered.Target.Thing, dishwasher),
                "Doing dishes must exclude the unpowered dishwasher from destination selection.");
            if (processorControlled)
            {
                IntegrationAssert.NotNull(
                    processorWorkGiver,
                    "The active Processor Framework path must expose its fill work giver.");
                var processor = dishwasher.AllComps.Single(comp => processorType!.IsInstanceOfType(comp));
                var findIngredient = AccessTools.Method(processorWorkGiverType, "FindIngredient");
                var unpoweredIngredient = (Thing?)findIngredient!.Invoke(
                    processorWorkGiver,
                    new object[] { pawn, processor });
                IntegrationAssert.Null(
                    unpoweredIngredient,
                    "Processor Framework must not select dirty ware for an unpowered dishwasher.");
                var processorHasUnpoweredJob = (bool)AccessTools.Method(
                    processorWorkGiverType,
                    "HasJobOnThing")!.Invoke(
                    processorWorkGiver,
                    new object[] { pawn, dishwasher, false });
                IntegrationAssert.False(
                    processorHasUnpoweredJob,
                    "Processor Framework must not admit dirty ware while the dishwasher is unpowered.");
            }

            plate.SetForbidden(true, warnOnFail: false);
            flick.DoFlick();
            for (var tick = 0; tick <= 200 && !power.PowerOn; tick++)
            {
                Find.TickManager.DoSingleTick();
            }
            IntegrationAssert.True(
                flick.SwitchIsOn && power.PowerOn,
                "Using the native switch must restore power from the unchanged connected battery.");
            plate.SetForbidden(false, warnOnFail: false);
            if (processorControlled)
            {
                IntegrationAssert.False(
                    dishwasherComp.CanAccept(plate),
                    "The local admission path must remain disabled while Processor Framework owns the appliance.");
                var processor = dishwasher.AllComps.Single(comp => processorType!.IsInstanceOfType(comp));
                var poweredIngredient = (Thing?)AccessTools.Method(
                    processorWorkGiverType,
                    "FindIngredient")!.Invoke(
                    processorWorkGiver,
                    new object[] { pawn, processor });
                var enabled = (System.Collections.IDictionary)AccessTools.Field(
                    processorType,
                    "enabledProcesses")!.GetValue(processor);
                var processDiagnostics = string.Join(
                    ",",
                    enabled.Keys.Cast<object>().Select(process =>
                    {
                        var allows = (AccessTools.Field(process.GetType(), "ingredientFilter")!
                            .GetValue(process) as ThingFilter)?.Allows(plate.def) == true;
                        var space = AccessTools.Method(processorType, "SpaceLeftFor")!.Invoke(
                            processor,
                            new object[] { process, 1f });
                        return $"{((Def)process).defName}:allows={allows}:space={space}";
                    }));
                IntegrationAssert.True(
                    ReferenceEquals(poweredIngredient, plate),
                    $"The powered Processor dishwasher must select the exact dirty plate " +
                    $"(dirty={sanitation.IsDirty}, forbidden={plate.IsForbidden(pawn)}, " +
                    $"reachable={pawn.CanReach(plate, PathEndMode.Touch, Danger.Some)}, " +
                    $"reservable={pawn.CanReserve(plate)}, processes={processDiagnostics}).");
                var processorHasPoweredJob = (bool)AccessTools.Method(
                    processorWorkGiverType,
                    "HasJobOnThing")!.Invoke(
                    processorWorkGiver,
                    new object[] { pawn, dishwasher, false });
                IntegrationAssert.True(
                    processorHasPoweredJob,
                    "The same powered dishwasher must accept dirty ware through Processor Framework.");
                var processorJob = (Job?)AccessTools.Method(
                    processorWorkGiverType,
                    "JobOnThing")!.Invoke(
                    processorWorkGiver,
                    new object[] { pawn, dishwasher, false });
                IntegrationAssert.True(
                    processorJob is not null &&
                    ReferenceEquals(processorJob.GetTarget(TargetIndex.A).Thing, dishwasher) &&
                    ReferenceEquals(processorJob.GetTarget(TargetIndex.B).Thing, plate),
                    "The Processor job must persist the exact powered dishwasher and dirty ware targets.");
            }
            else
            {
                IntegrationAssert.True(
                    dishwasherComp.CanAccept(plate),
                    "The same powered dishwasher must accept the dirty plate when otherwise operational.");
                IntegrationAssert.True(
                    WorkGiver_DoDishes.TryFindDestination(pawn, plate, out var powered) &&
                    ReferenceEquals(powered.Target.Thing, dishwasher),
                    "Doing dishes must select the same reachable dishwasher once power is restored.");
                var job = new WorkGiver_DoDishes().JobOnThing(pawn, plate);
                IntegrationAssert.True(
                    job is not null && ReferenceEquals(job.GetTarget(TargetIndex.B).Thing, dishwasher),
                    "The native work giver job must persist the exact powered dishwasher destination.");
            }
        }
        finally
        {
            ImmersiveChefsMod.Settings.PreferDishwashers = previousPreference;
            for (var index = createdThings.Count - 1; index >= 0; index--)
            {
                if (!createdThings[index].Destroyed)
                {
                    createdThings[index].Destroy(DestroyMode.Vanish);
                }
            }
            map.powerNetManager.UpdatePowerNetsAndConnections_First();
        }
    }

    private static bool IsEmptyFixtureArea(Map map, IntVec3 center, int radius)
    {
        for (var x = center.x - radius; x <= center.x + radius; x++)
        {
            for (var z = center.z - radius; z <= center.z + radius; z++)
            {
                var cell = new IntVec3(x, 0, z);
                if (x < 0 || z < 0 || x >= map.Size.x || z >= map.Size.z ||
                    !cell.Standable(map) ||
                    cell.GetThingList(map).Count != 0 ||
                    map.zoneManager.ZoneAt(cell) is not null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void EmbeddedMealOwnsAndReleasesTheExactPlateThing()
    {
        var meal = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("MealSimple"));
        var plate = ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var plateId = plate.ThingID;
        var embedded = ((ThingWithComps)meal).GetComp<CompEmbeddedWare>();

        IntegrationAssert.True(embedded.TryEmbedPlate(plate), "The finalized meal must accept one physical plate.");
        ((ThingWithComps)plate).GetComp<CompSanitation>().MarkClean(WashProvenance.WildWater);
        IntegrationAssert.True(
            ReferenceEquals(plate, embedded.PeekPlateThing()),
            "Embedding must retain the original Thing instance.");
        IntegrationAssert.Equal(
            WashProvenance.WildWater,
            embedded.Bindings.Single().WashProvenance,
            "The lightweight plate binding must retain sanitation provenance.");
        var released = embedded.ReleasePlateThing();
        IntegrationAssert.True(
            ReferenceEquals(plate, released),
            "Releasing must return the exact original Thing instance.");
        IntegrationAssert.Equal(plateId, released!.ThingID, "The plate LoadID must remain unchanged.");

    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void RealStorageFiltersAndStackingTrackSpawnedSanitationTransitions()
    {
        var map = Find.CurrentMap;
        var pawn = map.mapPawns.FreeColonistsSpawned.First();
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        var cleanSpecial = DefDatabase<SpecialThingFilterDef>.GetNamed("ImmersiveChefs_AllowCleanKitchenware");
        var dirtySpecial = DefDatabase<SpecialThingFilterDef>.GetNamed("ImmersiveChefs_AllowDirtyKitchenware");
        var zoneCells = map.AllCells
            .Where(cell =>
                cell.Standable(map) &&
                map.zoneManager.ZoneAt(cell) is null &&
                cell.GetFirstItem(map) is null &&
                cell.GetEdifice(map) is null)
            .OrderBy(cell => cell.DistanceToSquared(pawn.Position))
            .Take(2)
            .ToList();
        IntegrationAssert.Equal(2, zoneCells.Count, "The quickstart map must provide two stockpile fixture cells.");

        var cleanZone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
        var dirtyZone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
        map.zoneManager.RegisterZone(cleanZone);
        map.zoneManager.RegisterZone(dirtyZone);
        cleanZone.AddCell(zoneCells[0]);
        dirtyZone.AddCell(zoneCells[1]);
        ConfigureSanitationStockpile(cleanZone, plateDef, cleanSpecial, dirtySpecial, clean: true);
        ConfigureSanitationStockpile(dirtyZone, plateDef, cleanSpecial, dirtySpecial, clean: false);

        var plate = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        var other = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        GenSpawn.Spawn(plate, zoneCells[0], map);

        try
        {
            var sanitation = plate.GetComp<CompSanitation>();
            var otherSanitation = other.GetComp<CompSanitation>();
            sanitation.MarkClean(WashProvenance.WildWater);
            otherSanitation.MarkClean(WashProvenance.Safe);

            IntegrationAssert.True(
                !sanitation.AllowStackWith(other),
                "Equal clean plates with different wash provenance must not stack and erase safety state.");

            var dirtyOnly = new ThingFilter();
            dirtyOnly.SetAllow(plateDef, allow: true);
            dirtyOnly.SetAllow(cleanSpecial, allow: false);
            dirtyOnly.SetAllow(dirtySpecial, allow: true);
            IntegrationAssert.True(!dirtyOnly.Allows(plate), "A clean plate must be excluded from dirty-only storage.");
            IntegrationAssert.True(
                !map.listerHaulables.ThingsPotentiallyNeedingHauling().Contains(plate),
                "A clean plate already in clean-only storage must not be queued for hauling.");

            sanitation.MarkDirty();
            IntegrationAssert.True(
                dirtyOnly.Allows(plate),
                "A spawned plate must enter dirty-only storage eligibility immediately after being dirtied.");
            IntegrationAssert.True(
                map.listerHaulables.ThingsPotentiallyNeedingHauling().Contains(plate),
                "Dirtifying a spawned plate must invalidate the haul cache for its clean-only stockpile.");
            var haulJob = HaulAIUtility.HaulToStorageJob(pawn, plate, forced: true);
            IntegrationAssert.NotNull(haulJob, "RimWorld must find the dirty-only stockpile after invalidation.");
            IntegrationAssert.Equal(
                zoneCells[1],
                haulJob!.GetTarget(Verse.AI.TargetIndex.B).Cell,
                "RimWorld's native hauling selector must route the dirty plate to dirty-only storage.");

            var cleanOnly = new ThingFilter();
            cleanOnly.SetAllow(plateDef, allow: true);
            cleanOnly.SetAllow(cleanSpecial, allow: true);
            cleanOnly.SetAllow(dirtySpecial, allow: false);
            IntegrationAssert.True(!cleanOnly.Allows(plate), "A dirty plate must be excluded from clean-only storage.");

            sanitation.MarkClean(WashProvenance.Safe);
            IntegrationAssert.True(
                cleanOnly.Allows(plate),
                "A spawned plate must enter clean-only storage eligibility immediately after being washed.");
        }
        finally
        {
            if (!plate.Destroyed)
            {
                plate.Destroy(DestroyMode.Vanish);
            }

            if (!other.Destroyed)
            {
                other.Destroy(DestroyMode.Vanish);
            }

            cleanZone.Delete();
            dirtyZone.Delete();
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void TypedWashSourcesWriteVanillaSerializableJobTargets()
    {
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        var dirtyWare = ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        var fixture = ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        try
        {
            var safe = WorkGiver_DoDishes.CreateJob(
                dirtyWare,
                DishwashingDestination.ForHandwashing(fixture, WashProvenance.Safe));
            var wild = WorkGiver_DoDishes.CreateJob(
                dirtyWare,
                DishwashingDestination.ForHandwashing(Find.CurrentMap.Center, WashProvenance.WildWater));

            IntegrationAssert.True(
                safe.GetTarget(Verse.AI.TargetIndex.C).HasThing,
                "A validated safe fixture must persist its provenance marker in vanilla Job target C.");
            IntegrationAssert.True(
                !wild.GetTarget(Verse.AI.TargetIndex.C).IsValid,
                "A wild-water destination must leave vanilla Job target C unset.");
        }
        finally
        {
            if (!dirtyWare.Destroyed)
            {
                dirtyWare.Destroy(DestroyMode.Vanish);
            }

            if (!fixture.Destroyed)
            {
                fixture.Destroy(DestroyMode.Vanish);
            }
        }
    }

    private static void ConfigureSanitationStockpile(
        Zone_Stockpile zone,
        ThingDef plateDef,
        SpecialThingFilterDef cleanSpecial,
        SpecialThingFilterDef dirtySpecial,
        bool clean)
    {
        zone.settings.Priority = StoragePriority.Critical;
        zone.settings.filter.SetDisallowAll();
        zone.settings.filter.SetAllow(plateDef, allow: true);
        zone.settings.filter.SetAllow(cleanSpecial, allow: clean);
        zone.settings.filter.SetAllow(dirtySpecial, allow: !clean);
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void EmbeddedMealTransfersTheExactPlateFromPawnInventory()
    {
        var pawn = Find.CurrentMap.mapPawns.FreeColonistsSpawned.First();
        var meal = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("MealSimple"));
        var plate = ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var embedded = ((ThingWithComps)meal).GetComp<CompEmbeddedWare>();

        IntegrationAssert.True(
            pawn.inventory.innerContainer.TryAdd(plate, canMergeWithExistingStacks: false),
            "The integration fixture must put the physical plate in a real pawn inventory.");
        IntegrationAssert.True(
            embedded.TryEmbedPlate(plate),
            "A cooking product must transfer its reserved plate out of Pawn_InventoryTracker.");
        IntegrationAssert.True(
            ReferenceEquals(plate, embedded.PeekPlateThing()),
            "Inventory transfer must retain the exact physical plate instance.");

        var released = embedded.ReleasePlateThing();
        IntegrationAssert.True(
            ReferenceEquals(plate, released),
            "The exact inventory-sourced plate must remain recoverable after embedding.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void CoreHarmonyOwnersAreInstalledExactlyOnce()
    {
        var cooking = AccessTools.Method(typeof(WorkGiver_DoBill), nameof(WorkGiver_DoBill.JobOnThing));
        var ingest = AccessTools.Method(typeof(JobDriver_Ingest), nameof(JobDriver_Ingest.TryMakePreToilReservations));
        var ingestOutcome = AccessTools.Method(
            typeof(Thing),
            nameof(Thing.Ingested),
            new[] { typeof(Pawn), typeof(float) });
        var chew = AccessTools.Method(typeof(Toils_Ingest), nameof(Toils_Ingest.ChewIngestible));
        var feedReservations = AccessTools.Method(
            typeof(JobDriver_FoodFeedPatient),
            nameof(JobDriver_FoodFeedPatient.TryMakePreToilReservations));
        var feedToils = AccessTools.Method(typeof(JobDriver_FoodFeedPatient), "MakeNewToils");
        foreach (var method in new[] { cooking, ingest, ingestOutcome, chew, feedReservations, feedToils })
        {
            var owners = Harmony.GetPatchInfo(method)?.Owners
                .Where(owner => owner == ImmersiveChefsMod.PackageId)
                .ToList() ?? new System.Collections.Generic.List<string>();
            IntegrationAssert.Equal(1, owners.Count, $"Expected one Immersive Chefs patch owner on {method.Name}.");
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ImmersiveChefsXmlProbeContainsItsFinalPatch()
    {
        var steel = DefDatabase<ThingDef>.GetNamedSilentFail("Steel");
        var probe = steel?.GetModExtension<ImmersiveChefsIntegrationProbeExtension>();

        IntegrationAssert.NotNull(
            probe,
            "Finalized Core Steel must contain the Immersive Chefs XML-patched mod extension.");
        IntegrationAssert.Equal(
            "patched-by-immersive-chefs-xml",
            probe!.marker,
            "The finalized probe must contain the exact Immersive Chefs PatchOperation result.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ActiveProcessorFrameworkUsesOneIdentityPreservingBridge()
    {
        var processorActive = LoadedModManager.RunningModsListForReading.Any(mod =>
            string.Equals(mod.PackageId, "syrchalis.processor.framework", StringComparison.OrdinalIgnoreCase));
        if (!processorActive)
        {
            return;
        }

        var processorType = AccessTools.TypeByName("ProcessorFramework.CompProcessor");
        IntegrationAssert.NotNull(processorType, "Active Processor Framework must expose CompProcessor.");
        foreach (var defName in new[]
                 {
                     "ImmersiveChefs_Dishwasher",
                     "ImmersiveChefs_IndustrialDishwasher"
                 })
        {
            var def = DefDatabase<ThingDef>.GetNamed(defName);
            var processorProperties = def.comps.Single(comp =>
                processorType!.IsAssignableFrom(comp.compClass));
            IntegrationAssert.Equal(
                1,
                def.comps.Count(comp => processorType!.IsAssignableFrom(comp.compClass)),
                $"{defName} must contain exactly one Processor Framework component.");
            IntegrationAssert.Equal(
                DrawerType.MapMeshAndRealTime,
                def.drawerType,
                $"{defName} must render Processor Framework progress.");

            var processes = (AccessTools.Field(processorProperties.GetType(), "processes")?
                                 .GetValue(processorProperties) as System.Collections.IEnumerable)?
                .Cast<object>()
                .ToList();
            IntegrationAssert.NotNull(processes, $"{defName} must expose its Processor process list.");
            foreach (var ware in DefDatabase<ThingDef>.AllDefsListForReading.Where(candidate =>
                         candidate.GetModExtension<KitchenwareExtension>()?.product is
                             KitchenwareProduct.Cookware or KitchenwareProduct.Plate or KitchenwareProduct.Cutlery))
            {
                var matches = processes!.Where(process =>
                        (AccessTools.Field(process.GetType(), "ingredientFilter")?.GetValue(process) as ThingFilter)?
                        .Allows(ware) == true)
                    .ToList();
                IntegrationAssert.Equal(
                    1,
                    matches.Count,
                    $"{defName} must expose exactly one process for {ware.defName}.");
                var actualFactor = Convert.ToSingle(
                    AccessTools.Field(matches[0].GetType(), "capacityFactor")!.GetValue(matches[0]));
                var expectedFactor = DishwasherCapacityPolicy.ProcessorCapacityFactor(
                    ware.GetModExtension<KitchenwareExtension>()?.plateEquivalent ?? 1f);
                IntegrationAssert.True(
                    Math.Abs(actualFactor - expectedFactor) < 0.0001f,
                    $"{defName}/{ware.defName} must consume {expectedFactor} plate-equivalents, got {actualFactor}.");
            }

        }

        var initialize = AccessTools.Method(processorType, "Initialize");
        var addIngredient = AccessTools.Method(processorType, "AddIngredient");
        var takeOut = AccessTools.Method(processorType, "TakeOutProduct");
        var addIngredientPatches = Harmony.GetPatchInfo(addIngredient);
        var owners = Harmony.GetPatchInfo(takeOut)?.Owners
            .Count(owner => owner == ImmersiveChefsMod.PackageId) ?? 0;
        IntegrationAssert.Equal(1, addIngredientPatches?.Prefixes
            .Count(patch => patch.owner == ImmersiveChefsMod.PackageId) ?? 0,
            "Processor admission must have one Immersive Chefs batch-gating prefix.");
        IntegrationAssert.Equal(1, addIngredientPatches?.Postfixes
            .Count(patch => patch.owner == ImmersiveChefsMod.PackageId) ?? 0,
            "Processor admission must have one Immersive Chefs persistence postfix.");
        IntegrationAssert.Equal(1, Harmony.GetPatchInfo(initialize)?.Postfixes
            .Count(patch => patch.owner == ImmersiveChefsMod.PackageId) ?? 0,
            "New dishwashers must have one Immersive Chefs process-enablement postfix.");
        IntegrationAssert.Equal(1, owners, "Processor completion must have one Immersive Chefs identity bridge.");
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void ActiveProcessorFrameworkEnablesEveryDishwasherWareProcess()
    {
        var processorType = AccessTools.TypeByName("ProcessorFramework.CompProcessor");
        if (processorType is null)
        {
            return;
        }

        foreach (var defName in new[]
                 {
                     "ImmersiveChefs_Dishwasher",
                     "ImmersiveChefs_IndustrialDishwasher"
                 })
        {
            var instance = (ThingWithComps)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(defName));
            var processor = instance.AllComps.Single(comp => processorType.IsInstanceOfType(comp));
            var enabledProcesses = AccessTools.Field(processorType, "enabledProcesses")?
                .GetValue(processor) as System.Collections.IDictionary;
            var processProperties = processor.props;
            var processes = (AccessTools.Field(processProperties.GetType(), "processes")?
                                 .GetValue(processProperties) as System.Collections.IEnumerable)?
                .Cast<object>()
                .ToList();
            IntegrationAssert.NotNull(processes, $"A new {defName} must expose its Processor processes.");
            IntegrationAssert.NotNull(
                enabledProcesses,
                $"A new {defName} must expose its enabled Processor filters.");
            IntegrationAssert.Equal(
                processes!.Count,
                enabledProcesses!.Count,
                $"A new {defName} must enable every ware process independently of Processor Framework's global first-only default.");
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ActiveDubsAddsOneValidatedPipeToEachDishwasher()
    {
        var dubsActive = LoadedModManager.RunningModsListForReading.Any(mod =>
            string.Equals(mod.PackageId, "dubwise.dubsbadhygiene", StringComparison.OrdinalIgnoreCase));
        if (!dubsActive)
        {
            return;
        }

        var pipeType = AccessTools.TypeByName("DubsBadHygiene.CompPipe");
        IntegrationAssert.NotNull(pipeType, "Active Dubs Bad Hygiene must expose CompPipe.");
        foreach (var defName in new[]
                 {
                     "ImmersiveChefs_Dishwasher",
                     "ImmersiveChefs_IndustrialDishwasher"
                 })
        {
            var def = DefDatabase<ThingDef>.GetNamed(defName);
            IntegrationAssert.Equal(
                1,
                def.comps.Count(comp => pipeType!.IsAssignableFrom(comp.compClass)),
                $"{defName} must contain exactly one validated Dubs pipe component.");
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ActiveCommonSenseUsesItsExactPublicCleaningShape()
    {
        var commonSenseLoaded = LoadedModManager.RunningModsListForReading.Any(mod =>
            string.Equals(mod.PackageId, "avilmask.commonsense", StringComparison.OrdinalIgnoreCase));
        if (!commonSenseLoaded)
        {
            IntegrationAssert.True(
                ImmersiveChefsMod.Integrations?.IsActive(OptionalIntegration.CommonSense) == false,
                "An absent Common Sense package must remain inactive.");
            IntegrationAssert.True(
                !CommonSenseAdapter.Enabled,
                "The Common Sense adapter must not bind when its package is absent.");
            return;
        }

        IntegrationAssert.True(
            ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.CommonSense),
            "The default Auto setting must enable an active Common Sense package.");
        IntegrationAssert.True(
            CommonSenseAdapter.Enabled,
            "The supported Common Sense matrix must bind the post-dining adapter.");

        var settingsType = AccessTools.TypeByName("CommonSense.Settings");
        var utilityType = AccessTools.TypeByName("CommonSense.Utility");
        IntegrationAssert.NotNull(settingsType, "Common Sense must expose public CommonSense.Settings.");
        IntegrationAssert.NotNull(utilityType, "Common Sense must expose public static CommonSense.Utility.");
        IntegrationAssert.Equal(
            "CommonSense",
            settingsType!.Assembly.GetName().Name,
            "The settings surface must come from the exact CommonSense assembly identity.");
        IntegrationAssert.Equal(
            settingsType.Assembly,
            utilityType!.Assembly,
            "The validated settings and cleaning utility must come from the same Common Sense assembly.");

        var ingestSetting = settingsType.GetField(
            "adv_cleaning_ingest",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
        IntegrationAssert.NotNull(
            ingestSetting,
            "The supported Common Sense shape must retain its public static ingestion-cleaning setting.");
        IntegrationAssert.Equal(
            typeof(bool),
            ingestSetting!.FieldType,
            "Common Sense adv_cleaning_ingest must remain a Boolean setting.");

        var incapableMethod = utilityType.GetMethod(
            "IncapableOfCleaning",
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static,
            binder: null,
            types: new[] { typeof(Pawn) },
            modifiers: null);
        IntegrationAssert.NotNull(
            incapableMethod,
            "The supported Common Sense shape must retain public static bool IncapableOfCleaning(Pawn).");
        IntegrationAssert.Equal(
            typeof(bool),
            incapableMethod!.ReturnType,
            "Common Sense cleaning capability must remain a Boolean predicate.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ActiveVanillaNutrientPasteExpandedUsesItsExactPipeBackedTap()
    {
        var activeIds = LoadedModManager.RunningModsListForReading
            .Select(mod => mod.PackageId)
            .ToList();
        var vnpeActive = activeIds.Any(id =>
            string.Equals(id, "vanillaexpanded.vnutriente", StringComparison.OrdinalIgnoreCase));
        if (!vnpeActive)
        {
            return;
        }

        var phase = "active package and setting validation";
        try
        {
            IntegrationAssert.True(
                activeIds.Any(id => string.Equals(
                    id,
                    "oskarpotocki.vanillafactionsexpanded.core",
                    StringComparison.OrdinalIgnoreCase)),
                "The supported VNPE matrix must load Vanilla Expanded Framework first.");
            IntegrationAssert.True(
                ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.VanillaNutrientPasteExpanded),
                "The exact active VNPE+VEF matrix must enable its Auto integration setting.");
            IntegrationAssert.True(
                VanillaNutrientPasteExpandedAdapter.Enabled,
                "The finalized-Def bootstrap must validate and enable the VNPE adapter.");

            phase = "finalized tap Def and runtime type validation";
            var tapDef = DefDatabase<ThingDef>.GetNamed("VNPE_NutrientPasteTap");
            var tapType = AccessTools.TypeByName("VNPE.Building_NutrientPasteTap");
            var pipePropertiesType = AccessTools.TypeByName("PipeSystem.CompProperties_Resource");
            IntegrationAssert.NotNull(tapType, "Active VNPE must expose its exact nutrient-paste tap type.");
            IntegrationAssert.NotNull(pipePropertiesType, "Active VEF must expose the exact pipe resource component.");
            IntegrationAssert.Equal(
                "VNPE",
                tapType!.Assembly.GetName().Name,
                "The supported nutrient-paste tap type must come from VNPE.dll.");
            IntegrationAssert.Equal(
                typeof(Building_NutrientPasteDispenser),
                tapType.BaseType,
                "The validated VNPE tap must directly subclass the vanilla dispenser.");
            IntegrationAssert.Equal(
                tapType,
                tapDef.thingClass,
                "The finalized VNPE tap Def must retain the validated runtime type.");
            IntegrationAssert.NotNull(
                tapDef.comps,
                "The finalized VNPE tap Def must retain its component list.");
            IntegrationAssert.Equal(
                1,
                tapDef.comps!.Count(properties => pipePropertiesType!.IsInstanceOfType(properties)),
                "The finalized VNPE tap must retain exactly one pipe-resource component.");

            phase = "finalized exact dispenser classification";
            var vanillaDef = DefDatabase<ThingDef>.GetNamed("NutrientPasteDispenser");
            IntegrationAssert.True(
                VanillaNutrientPasteExpandedAdapter.Controls(tapDef, tapType),
                "The exact finalized VNPE tap must be owned by the guarded adapter.");
            IntegrationAssert.True(
                PasteDispenserAdapter.Supports(tapDef, tapType),
                "Prepared-paste dispensing must accept the exact finalized VNPE tap.");
            IntegrationAssert.True(
                PasteDispenserAdapter.Supports(vanillaDef, vanillaDef.thingClass),
                "The base vanilla dispenser must remain supported in the VNPE matrix.");

            phase = "live setting opt-out validation";
            var originalVnpeMode = ImmersiveChefsMod.Settings.VanillaNutrientPasteExpanded;
            try
            {
                ImmersiveChefsMod.Settings.VanillaNutrientPasteExpanded = OptionalIntegrationMode.Off;
                IntegrationAssert.False(
                    VanillaNutrientPasteExpandedAdapter.Controls(tapDef, tapType),
                    "Turning the VNPE integration off must immediately release the native tap.");
                IntegrationAssert.False(
                    PasteDispenserAdapter.Supports(tapDef, tapType),
                    "Prepared-paste dispensing must immediately reject the VNPE tap while its setting is off.");
                IntegrationAssert.True(
                    PasteDispenserAdapter.Supports(vanillaDef, vanillaDef.thingClass),
                    "Turning VNPE support off must not disable the base vanilla dispenser.");
            }
            finally
            {
                ImmersiveChefsMod.Settings.VanillaNutrientPasteExpanded = originalVnpeMode;
            }

            phase = "native Harmony patch metadata validation";
            var canDispense = AccessTools.PropertyGetter(
                typeof(Building_NutrientPasteDispenser),
                nameof(Building_NutrientPasteDispenser.CanDispenseNow));
            var tryDispense = AccessTools.Method(
                typeof(Building_NutrientPasteDispenser),
                nameof(Building_NutrientPasteDispenser.TryDispenseFood));
            IntegrationAssert.NotNull(
                canDispense,
                "The vanilla dispenser CanDispenseNow getter must exist in the active game.");
            IntegrationAssert.NotNull(
                tryDispense,
                "The vanilla dispenser TryDispenseFood method must exist in the active game.");
            var canDispensePatches = Harmony.GetPatchInfo(canDispense!);
            var tryDispensePatches = Harmony.GetPatchInfo(tryDispense!);
            IntegrationAssert.NotNull(
                canDispensePatches,
                "The active VNPE matrix must patch the vanilla CanDispenseNow getter.");
            IntegrationAssert.NotNull(
                tryDispensePatches,
                "The active VNPE and Immersive Chefs matrix must patch TryDispenseFood.");
            IntegrationAssert.True(
                canDispensePatches!.Prefixes.Any(patch =>
                    patch.PatchMethod?.DeclaringType?.FullName ==
                    "VNPE.Building_NutrientPasteDispenser_CanDispenseNow") == true,
                "VNPE's native CanDispenseNow pipe-network prefix must remain installed.");
            IntegrationAssert.True(
                tryDispensePatches!.Prefixes.Any(patch =>
                    patch.PatchMethod?.DeclaringType?.FullName ==
                    "VNPE.Building_NutrientPasteDispenser_TryDispenseFood") == true,
                "VNPE's native TryDispenseFood pipe-network prefix must remain installed.");
            IntegrationAssert.Equal(
                1,
                tryDispensePatches.Owners.Count(owner => owner == ImmersiveChefsMod.PackageId),
                "Plate attachment must retain one Immersive Chefs postfix on the shared native dispense method.");
        }
        catch (IntegrationTestAssertionException)
        {
            throw;
        }
        catch (Exception exception)
        {
            IntegrationAssert.Fail(
                $"The active VNPE contract threw {exception.GetType().FullName} during {phase}.");
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ActiveGastronomyInstallsOneGuardedWaiterBridge()
    {
        var activeIds = LoadedModManager.RunningModsListForReading
            .Select(mod => mod.PackageId)
            .ToList();
        var gastronomyActive = activeIds.Any(id =>
            string.Equals(id, "orion.gastronomy", StringComparison.OrdinalIgnoreCase));
        if (!gastronomyActive)
        {
            return;
        }

        IntegrationAssert.True(
            activeIds.Any(id => string.Equals(id, "orion.cashregister", StringComparison.OrdinalIgnoreCase)),
            "The supported Gastronomy matrix must load Cash Register first.");
        var serveType = AccessTools.TypeByName("Gastronomy.Waiting.JobDriver_Serve");
        IntegrationAssert.NotNull(serveType, "Active Gastronomy must expose its waiter driver.");
        var makeToils = AccessTools.Method(serveType, "MakeNewToils");
        var owners = Harmony.GetPatchInfo(makeToils)?.Owners
            .Count(owner => owner == ImmersiveChefsMod.PackageId) ?? 0;
        IntegrationAssert.Equal(1, owners, "Gastronomy serving must have one guarded Immersive Chefs bridge.");
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void ActiveGastronomyServedColonyCutleryReturnsToTheMap()
    {
        var gastronomyActive = LoadedModManager.RunningModsListForReading.Any(mod =>
            string.Equals(mod.PackageId, "orion.gastronomy", StringComparison.OrdinalIgnoreCase));
        if (!gastronomyActive)
        {
            return;
        }

        var map = Find.CurrentMap;
        var cells = map.AllCells
            .Where(cell => cell.Standable(map) && cell.GetThingList(map).Count == 0)
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .Take(2)
            .ToList();
        IntegrationAssert.Equal(2, cells.Count, "The Gastronomy fixture needs two empty walkable cells.");
        var patron = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            Faction.OfPlayer,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false));
        var server = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            Faction.OfPlayer,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false));
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var cancelledCutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        var completedCutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        var cancelledJob = JobMaker.MakeJob(JobDefOf.Ingest, meal);
        var completedJob = JobMaker.MakeJob(JobDefOf.Ingest, meal);

        try
        {
            GenSpawn.Spawn(patron, cells[0], map);
            GenSpawn.Spawn(server, cells[1], map);
            cancelledCutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            completedCutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            IntegrationAssert.True(
                patron.inventory.innerContainer.TryAdd(cancelledCutlery, canMergeWithExistingStacks: false),
                "The waiter-delivered cancellation fixture must begin in the patron inventory.");

            DiningSessionRegistry.TryAttachServed(patron, cancelledJob, meal, cancelledCutlery, server);
            DiningSessionRegistry.Cleanup(patron, cancelledJob);

            IntegrationAssert.True(
                patron.inventory.innerContainer.TryAdd(completedCutlery, canMergeWithExistingStacks: false),
                "The waiter-delivered completion fixture must begin in the patron inventory.");
            DiningSessionRegistry.TryAttachServed(patron, completedJob, meal, completedCutlery, server);
            DiningSessionRegistry.Complete(patron);

            IntegrationAssert.True(
                cancelledCutlery.Spawned && !patron.inventory.innerContainer.Contains(cancelledCutlery),
                "Cancelling served dining must return exact unused colony cutlery to the map.");
            IntegrationAssert.False(
                cancelledCutlery.GetComp<CompSanitation>().IsDirty,
                "Cancelling served dining must leave the unused colony cutlery clean.");
            IntegrationAssert.True(
                completedCutlery.Spawned && !patron.inventory.innerContainer.Contains(completedCutlery),
                "Completing served dining must return exact used colony cutlery to the map.");
            IntegrationAssert.True(
                completedCutlery.GetComp<CompSanitation>().IsDirty,
                "Completing served dining must return the used colony cutlery dirty.");
        }
        finally
        {
            DiningSessionRegistry.Cleanup(patron, cancelledJob);
            DiningSessionRegistry.Cleanup(patron, completedJob);
            foreach (var thing in new Thing[] { cancelledCutlery, completedCutlery, meal, patron, server })
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void PersonalCutleryStackSplitPreservesSanitationAcrossCancelAndCompletion()
    {
        var map = Find.CurrentMap;
        var playerFaction = Faction.OfPlayer;
        var guestFaction = Find.FactionManager.AllFactionsListForReading.First(faction =>
            faction != playerFaction && !faction.HostileTo(playerFaction) && !faction.def.hidden);
        var guest = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            guestFaction,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false));
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var dirtyStack = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        ThingWithComps? completionStack = null;
        var cancellationJob = JobMaker.MakeJob(JobDefOf.Ingest, meal);
        Job? completionJob = null;
        var originalRequirementMode = ImmersiveChefsMod.Settings.WareRequirementMode;
        var originalDirtyFallback = ImmersiveChefsMod.Settings.DirtyWareFallback;

        try
        {
            ImmersiveChefsMod.Settings.WareRequirementMode = WareRequirementMode.Prefer;
            ImmersiveChefsMod.Settings.DirtyWareFallback = DirtyWareFallback.Always;
            var fixtureCell = map.AllCells
                .Where(cell => cell.Standable(map) && cell.GetThingList(map).Count == 0)
                .OrderBy(cell => cell.DistanceToSquared(map.Center))
                .First();
            GenSpawn.Spawn(guest, fixtureCell, map);
            GenSpawn.Spawn(meal, fixtureCell, map);

            dirtyStack.stackCount = 2;
            dirtyStack.GetComp<CompSanitation>().MarkClean(WashProvenance.WildWater);
            dirtyStack.GetComp<CompSanitation>().MarkDirty();
            IntegrationAssert.True(
                guest.inventory.innerContainer.TryAdd(dirtyStack, canMergeWithExistingStacks: false),
                "The cancellation fixture must start as a two-item personal stack.");
            IntegrationAssert.True(
                DiningSessionRegistry.TryAttach(guest, cancellationJob, meal),
                "The guest must attach a personal-stack cancellation session.");
            var cancellationSession = DiningSessionRegistry.Current(guest);
            IntegrationAssert.True(
                ReferenceEquals(cancellationSession?.Cutlery, dirtyStack),
                "The dirty personal stack must be the exact selected fallback.");
            cancellationSession!.PickupCutlery();
            var cancelledPiece = cancellationSession.CarriedCutlery as ThingWithComps;
            IntegrationAssert.NotNull(cancelledPiece, "Personal pickup must retain one exact split item.");
            DiningSessionRegistry.Cleanup(guest, cancellationJob);
            IntegrationAssert.True(
                guest.inventory.innerContainer.Contains(cancelledPiece!) &&
                cancelledPiece!.GetComp<CompSanitation>().IsDirty &&
                cancelledPiece.GetComp<CompSanitation>().WashProvenance == WashProvenance.WildWater,
                "Cancellation must preserve dirty wild-water sanitation on the split personal item.");

            guest.inventory.innerContainer.ClearAndDestroyContents();
            completionStack = (ThingWithComps)ThingMaker.MakeThing(
                DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
                ThingDefOf.Steel);
            completionStack.stackCount = 2;
            completionStack.GetComp<CompSanitation>().MarkClean(WashProvenance.WildWater);
            IntegrationAssert.True(
                guest.inventory.innerContainer.TryAdd(completionStack, canMergeWithExistingStacks: false),
                "The completion fixture must start as a two-item personal stack.");
            completionJob = JobMaker.MakeJob(JobDefOf.Ingest, meal);
            IntegrationAssert.True(
                DiningSessionRegistry.TryAttach(guest, completionJob, meal),
                "The guest must attach a personal-stack completion session.");
            var completionSession = DiningSessionRegistry.Current(guest);
            IntegrationAssert.True(
                ReferenceEquals(completionSession?.Cutlery, completionStack),
                "The clean personal stack must be the exact selected fallback.");
            completionSession!.PickupCutlery();
            var completedPiece = completionSession.CarriedCutlery as ThingWithComps;
            IntegrationAssert.NotNull(completedPiece, "Personal completion must retain one exact split item.");
            DiningSessionRegistry.Complete(guest);
            IntegrationAssert.True(
                guest.inventory.innerContainer.Contains(completedPiece!) &&
                completedPiece!.GetComp<CompSanitation>().IsDirty &&
                completedPiece.GetComp<CompSanitation>().WashProvenance == WashProvenance.WildWater,
                "Completed dining must dirty the split personal item without losing wild-water provenance.");
        }
        finally
        {
            ImmersiveChefsMod.Settings.WareRequirementMode = originalRequirementMode;
            ImmersiveChefsMod.Settings.DirtyWareFallback = originalDirtyFallback;
            DiningSessionRegistry.Cleanup(guest, cancellationJob);
            if (completionJob is not null)
            {
                DiningSessionRegistry.Cleanup(guest, completionJob);
            }

            guest.ClearAllReservations(releaseDestinationsOnlyIfObsolete: false);
            guest.inventory.innerContainer.ClearAndDestroyContents();
            foreach (var thing in new Thing[] { dirtyStack, meal, guest })
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }

            if (completionStack is not null && !completionStack.Destroyed)
            {
                completionStack.Destroy(DestroyMode.Vanish);
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void ActiveHospitalityValidatesTheExactArrivedGuestShape()
    {
        var hospitalityActive = LoadedModManager.RunningModsListForReading.Any(mod =>
            string.Equals(mod.PackageId, "orion.hospitality", StringComparison.OrdinalIgnoreCase));
        if (!hospitalityActive)
        {
            return;
        }

        var utilityType = AccessTools.TypeByName("Hospitality.Utilities.GuestUtility");
        IntegrationAssert.NotNull(
            utilityType,
            "Active Hospitality must expose its public GuestUtility type.");
        IntegrationAssert.True(
            HospitalityAdapter.TryBind(utilityType, out var predicate, out var reason),
            "Active Hospitality must match the validated IsArrivedGuest(Pawn, out CompGuest) shape: " + reason);
        IntegrationAssert.NotNull(
            predicate,
            "A compatible active Hospitality assembly must produce an arrived-guest predicate.");
        IntegrationAssert.True(
            ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.Hospitality),
            "The default Auto setting must enable the shape-compatible active Hospitality adapter.");
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void ActiveHospitalityGuestPrefersColonyCutleryAndRetainsPersonalFallback()
    {
        var hospitalityActive = LoadedModManager.RunningModsListForReading.Any(mod =>
            string.Equals(mod.PackageId, "orion.hospitality", StringComparison.OrdinalIgnoreCase));
        if (!hospitalityActive)
        {
            return;
        }

        var map = Find.CurrentMap;
        var playerFaction = Faction.OfPlayer;
        var guestFaction = Find.FactionManager.AllFactionsListForReading.First(faction =>
            faction != playerFaction && !faction.HostileTo(playerFaction) && !faction.def.hidden);
        var guest = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            guestFaction,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false));
        guest.Name = new NameSingle("Hospitality Ware Guest");
        guest.inventory.innerContainer.ClearAndDestroyContents();
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var colonyCutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        var personalCutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"),
            ThingDefOf.Steel);
        var originalRequirementMode = ImmersiveChefsMod.Settings.WareRequirementMode;
        var firstJob = JobMaker.MakeJob(JobDefOf.Ingest, meal);
        Job? fallbackJob = null;
        object? hospitalityMapComponent = null;
        Type? hospitalityMapComponentType = null;

        try
        {
            ImmersiveChefsMod.Settings.WareRequirementMode = WareRequirementMode.Prefer;
            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            colonyCutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            personalCutlery.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            IntegrationAssert.True(
                meal.GetComp<CompEmbeddedWare>().TryEmbedPlate(plate),
                "The Hospitality fixture meal must retain its exact clean plate.");

            var fixtureCell = map.AllCells
                .Where(cell => cell.Standable(map) && cell.GetThingList(map).Count == 0)
                .OrderBy(cell => cell.DistanceToSquared(map.Center))
                .First();
            GenSpawn.Spawn(guest, fixtureCell, map);
            GenSpawn.Spawn(meal, fixtureCell, map);
            GenSpawn.Spawn(colonyCutlery, fixtureCell, map);
            IntegrationAssert.True(
                guest.inventory.innerContainer.TryAdd(personalCutlery, canMergeWithExistingStacks: false),
                "The arrived guest must start with exact personal cutlery in its real inventory.");

            var compGuestType = AccessTools.TypeByName("Hospitality.CompGuest");
            IntegrationAssert.NotNull(compGuestType, "Active Hospitality must expose CompGuest.");
            var compGuest = guest.AllComps.FirstOrDefault(compGuestType!.IsInstanceOfType);
            IntegrationAssert.NotNull(
                compGuest,
                "Hospitality must attach CompGuest to the finalized human pawn Def.");
            hospitalityMapComponentType = AccessTools.TypeByName("Hospitality.Hospitality_MapComponent");
            IntegrationAssert.NotNull(
                hospitalityMapComponentType,
                "Active Hospitality must expose its map-owned guest registry.");
            hospitalityMapComponent = map.components.FirstOrDefault(
                hospitalityMapComponentType!.IsInstanceOfType);
            IntegrationAssert.NotNull(
                hospitalityMapComponent,
                "Hospitality must construct its real map component before guest dining.");
            AccessTools.Method(hospitalityMapComponentType, "OnGuestJoinedLate")
                .Invoke(hospitalityMapComponent, new object[] { guest });
            AccessTools.Method(compGuestType, "Arrive").Invoke(compGuest, Array.Empty<object>());
            IntegrationAssert.True(
                HospitalityAdapter.IsArrivedGuest(guest),
                "The real active Hospitality utility must recognize the fixture pawn as arrived.");

            IntegrationAssert.True(
                DiningSessionRegistry.TryAttach(guest, firstJob, meal),
                "The arrived guest must attach an ordinary dining session.");
            IntegrationAssert.True(
                ReferenceEquals(DiningSessionRegistry.Current(guest)?.Cutlery, colonyCutlery),
                "Reachable colony cutlery must outrank the guest's eligible personal cutlery.");
            DiningSessionRegistry.Cleanup(guest, firstJob);
            guest.ClearAllReservations(releaseDestinationsOnlyIfObsolete: false);
            colonyCutlery.Destroy(DestroyMode.Vanish);

            fallbackJob = JobMaker.MakeJob(JobDefOf.Ingest, meal);
            IntegrationAssert.True(
                DiningSessionRegistry.TryAttach(guest, fallbackJob, meal),
                "The arrived guest must attach its personal-fallback dining session.");
            var fallbackSession = DiningSessionRegistry.Current(guest);
            IntegrationAssert.True(
                ReferenceEquals(fallbackSession?.Cutlery, personalCutlery),
                "With no eligible colony setting, the guest must select its exact personal cutlery.");
            fallbackSession!.PickupCutlery();
            IntegrationAssert.True(
                guest.inventory.innerContainer.Contains(personalCutlery),
                "Personal cutlery must remain in the guest's inventory while in use.");
            DiningSessionRegistry.Cleanup(guest, fallbackJob);
            IntegrationAssert.True(
                guest.inventory.innerContainer.Contains(personalCutlery) &&
                !personalCutlery.GetComp<CompSanitation>().IsDirty &&
                !personalCutlery.Spawned,
                "Cancellation must retain the exact clean personal setting in guest inventory.");

            fallbackJob = JobMaker.MakeJob(JobDefOf.Ingest, meal);
            IntegrationAssert.True(
                DiningSessionRegistry.TryAttach(guest, fallbackJob, meal),
                "The arrived guest must reattach after a cancelled personal-fallback session.");
            fallbackSession = DiningSessionRegistry.Current(guest);
            IntegrationAssert.True(
                ReferenceEquals(fallbackSession?.Cutlery, personalCutlery),
                "The replacement session must select the same exact personal setting.");
            fallbackSession!.PickupCutlery();
            DiningSessionRegistry.Complete(guest);
            IntegrationAssert.True(
                guest.inventory.innerContainer.Contains(personalCutlery) &&
                personalCutlery.GetComp<CompSanitation>().IsDirty &&
                !personalCutlery.Spawned,
                "The exact personal setting must return dirty to the guest inventory after dining.");
        }
        finally
        {
            ImmersiveChefsMod.Settings.WareRequirementMode = originalRequirementMode;
            guest.ClearAllReservations(releaseDestinationsOnlyIfObsolete: false);
            if (fallbackJob is not null)
            {
                DiningSessionRegistry.Cleanup(guest, fallbackJob);
            }

            if (hospitalityMapComponent is not null && hospitalityMapComponentType is not null)
            {
                AccessTools.Method(hospitalityMapComponentType, "OnGuestAdopted")
                    .Invoke(hospitalityMapComponent, new object[] { guest });
            }

            foreach (var thing in new Thing[] { colonyCutlery, meal, plate, guest })
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }

            if (!personalCutlery.Destroyed)
            {
                personalCutlery.holdingOwner?.Remove(personalCutlery);
                personalCutlery.Destroy(DestroyMode.Vanish);
            }
        }
    }
}
