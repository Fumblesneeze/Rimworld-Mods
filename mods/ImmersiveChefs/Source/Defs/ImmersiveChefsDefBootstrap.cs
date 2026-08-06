using Verse;

namespace ImmersiveChefs;

internal static class ImmersiveChefsDefBootstrap
{
    private static bool scheduled;
    private static bool applied;

    public static void Apply()
    {
        if (scheduled)
        {
            return;
        }

        scheduled = true;
        LongEventHandler.ExecuteWhenFinished(ApplyFinalizedDefs);
    }

    private static void ApplyFinalizedDefs()
    {
        if (applied)
        {
            return;
        }

        applied = true;
        TemperatureOwnership.ValidateActiveProviderShape();
        OptionalMaterialAdapter.ValidateAndConfigure();
        var classifier = OptionalMaterialAdapter.CreateClassifier();
        foreach (var recipe in DefDatabase<RecipeDef>.AllDefsListForReading)
        {
            var extension = recipe.GetModExtension<KitchenwareRecipeExtension>();
            if (extension is null || recipe.ingredients is null || recipe.ingredients.Count == 0)
            {
                continue;
            }

            var materialFilter = recipe.ingredients[0].filter;
            foreach (var thingDef in DefDatabase<ThingDef>.AllDefsListForReading)
            {
                var allowed = AllowsMaterial(classifier, extension, thingDef);
                materialFilter.SetAllow(thingDef, allowed);
                recipe.fixedIngredientFilter?.SetAllow(thingDef, allowed || thingDef.defName == "WoodLog");
            }
        }

        // Validate package-attributed meal registries before any finalized Def is mutated. A
        // changed optional-mod shape must fail closed for both timing and the ware lifecycle.
        RecipeWorkRuntime.Initialize(ImmersiveChefsMod.Settings);
        AddMealComponents();
        EnablePreparedIngredients();
        ApplyDishwasherCapacityScale();

        PreparedFoodRuntime.Initialize(ImmersiveChefsMod.Settings.PreparedRotMultiplier);
        InitializeOptionalAdapters();
    }

    private static void ApplyDishwasherCapacityScale()
    {
        foreach (var dishwasher in new[]
                 {
                     ImmersiveChefsDefOf.ImmersiveChefs_Dishwasher,
                     ImmersiveChefsDefOf.ImmersiveChefs_IndustrialDishwasher
                 })
        {
            var properties = dishwasher.comps?
                .OfType<CompProperties_Dishwasher>()
                .FirstOrDefault();
            if (properties is not null)
            {
                properties.basePlateCapacity = DishwasherCapacityPolicy.ScaleForRestart(
                    properties.basePlateCapacity,
                    ImmersiveChefsMod.Settings.DishwasherCapacityScale);
            }
        }
    }

    private static void InitializeOptionalAdapters()
    {
        var integrations = ImmersiveChefsMod.Integrations;
        if (integrations is null || ImmersiveChefsMod.HarmonyInstance is null)
        {
            return;
        }

        if (ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.DubsBadHygiene) &&
            !DubsWaterAdapter.TryInitializeDishwasherDefs(out var dubsReason))
        {
            OptionalIntegrationDiagnostics.WarnOnce(OptionalIntegration.DubsBadHygiene, dubsReason);
        }

        if (ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.VanillaNutrientPasteExpanded) &&
            !VanillaNutrientPasteExpandedAdapter.TryInitialize(out var vnpeReason))
        {
            OptionalIntegrationDiagnostics.WarnOnce(
                OptionalIntegration.VanillaNutrientPasteExpanded,
                vnpeReason);
        }

        if (ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.ProcessorFramework) &&
            !ProcessorFrameworkAdapter.TryInitialize(ImmersiveChefsMod.HarmonyInstance, out var reason))
        {
            OptionalIntegrationDiagnostics.WarnOnce(OptionalIntegration.ProcessorFramework, reason);
        }

        if (ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.Gastronomy) &&
            !GastronomyAdapter.TryInitialize(ImmersiveChefsMod.HarmonyInstance, out var gastronomyReason))
        {
            OptionalIntegrationDiagnostics.WarnOnce(OptionalIntegration.Gastronomy, gastronomyReason);
        }

        if (ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.CommonSense) &&
            !CommonSenseAdapter.TryInitialize(out var commonSenseReason))
        {
            OptionalIntegrationDiagnostics.WarnOnce(OptionalIntegration.CommonSense, commonSenseReason);
        }

        if (ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.Hospitality) &&
            !HospitalityAdapter.TryInitialize(out var hospitalityReason))
        {
            OptionalIntegrationDiagnostics.WarnOnce(OptionalIntegration.Hospitality, hospitalityReason);
        }

        if (ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.AdaptiveMealBill) &&
            !AdaptiveMealBillAdapter.TryInitialize(out var adaptiveReason))
        {
            OptionalIntegrationDiagnostics.WarnOnce(OptionalIntegration.AdaptiveMealBill, adaptiveReason);
        }

        if (ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.OvercookedMeals) &&
            !OvercookedMealsAdapter.TryInitialize(out var overcookedReason))
        {
            OptionalIntegrationDiagnostics.WarnOnce(OptionalIntegration.OvercookedMeals, overcookedReason);
        }

        if (ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.MealsOnWheels) &&
            !MealsOnWheelsAdapter.TryInitialize(out var mealsOnWheelsReason))
        {
            OptionalIntegrationDiagnostics.WarnOnce(OptionalIntegration.MealsOnWheels, mealsOnWheelsReason);
        }

        if (ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.PrioritizeMeals) &&
            !PrioritizeMealsAdapter.TryInitialize(out var prioritizeMealsReason))
        {
            OptionalIntegrationDiagnostics.WarnOnce(OptionalIntegration.PrioritizeMeals, prioritizeMealsReason);
        }

        if (ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.Replimat) &&
            !ReplimatAdapter.TryInitialize(ImmersiveChefsMod.HarmonyInstance, out var replimatReason))
        {
            OptionalIntegrationDiagnostics.WarnOnce(OptionalIntegration.Replimat, replimatReason);
        }

        if (ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.MealPrinter) &&
            !MealPrinterAdapter.TryInitialize(ImmersiveChefsMod.HarmonyInstance, out var mealPrinterReason))
        {
            OptionalIntegrationDiagnostics.WarnOnce(OptionalIntegration.MealPrinter, mealPrinterReason);
        }
    }

    private static void EnablePreparedIngredients()
    {
        var preparedDef = DefDatabase<ThingDef>.GetNamedSilentFail("ImmersiveChefs_PreparedFood");
        if (preparedDef is null)
        {
            return;
        }

        foreach (var recipe in DefDatabase<RecipeDef>.AllDefsListForReading.Where(MealCoveragePolicy.IsCovered))
        {
            recipe.fixedIngredientFilter?.SetAllow(preparedDef, true);
            recipe.defaultIngredientFilter?.SetAllow(preparedDef, true);
            foreach (var ingredient in recipe.ingredients ?? Enumerable.Empty<IngredientCount>())
            {
                ingredient.filter.SetAllow(preparedDef, true);
            }
        }

        var prepRecipe = DefDatabase<RecipeDef>.GetNamedSilentFail("ImmersiveChefs_PrepareIngredients");
        prepRecipe?.fixedIngredientFilter?.SetAllow(preparedDef, false);
        foreach (var ingredient in prepRecipe?.ingredients ?? Enumerable.Empty<IngredientCount>())
        {
            ingredient.filter.SetAllow(preparedDef, false);
        }
    }

    private static void AddMealComponents()
    {
        foreach (var thingDef in DefDatabase<ThingDef>.AllDefsListForReading.Where(MealCoveragePolicy.IsCovered))
        {
            if (thingDef.thingClass is null || !typeof(ThingWithComps).IsAssignableFrom(thingDef.thingClass))
            {
                continue;
            }

            thingDef.comps ??= new List<CompProperties>();
            if (thingDef.comps.All(properties => properties.compClass != typeof(CompEmbeddedWare)))
            {
                thingDef.comps.Add(new CompProperties_EmbeddedWare());
            }

            if (thingDef.comps.All(properties => properties.compClass != typeof(CompCulinaryState)))
            {
                thingDef.comps.Add(new CompProperties_CulinaryState());
            }
        }
    }

    private static bool AllowsMaterial(
        KitchenMaterialClassifier classifier,
        KitchenwareRecipeExtension recipe,
        ThingDef material)
    {
        if (material.stuffProps is null)
        {
            return false;
        }

        var categories = material.stuffProps.categories;
        var descriptor = new KitchenMaterialDescriptor(
            material.defName,
            categories?.Any(category => category.defName.Equals("Metallic", StringComparison.OrdinalIgnoreCase)) == true,
            categories?.Any(category => category.defName.Equals("Woody", StringComparison.OrdinalIgnoreCase)) == true,
            categories?.Any(category => category.defName.Equals("Stony", StringComparison.OrdinalIgnoreCase)) == true);
        var classification = classifier.Classify(descriptor, recipe.product);
        if (classification is null)
        {
            return false;
        }

        return KitchenMaterialFabricationPolicy.Allows(
            recipe.product,
            recipe.fabricationTier,
            classification);
    }
}
