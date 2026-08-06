namespace ImmersiveChefs;

public enum MealComplexity
{
    Simple,
    Advanced,
    Elaborate
}

public sealed class MealClassificationShapeFailure
{
    internal MealClassificationShapeFailure(
        string packageId,
        IEnumerable<string> missingRecipeDefNames,
        IEnumerable<string> missingMealDefNames)
    {
        PackageId = packageId;
        MissingRecipeDefNames = Array.AsReadOnly(missingRecipeDefNames.ToArray());
        MissingMealDefNames = Array.AsReadOnly(missingMealDefNames.ToArray());
    }

    public string PackageId { get; }

    public IReadOnlyList<string> MissingRecipeDefNames { get; }

    public IReadOnlyList<string> MissingMealDefNames { get; }
}

public sealed class MealClassificationCatalogValidationResult
{
    internal MealClassificationCatalogValidationResult(
        MealClassificationCatalog catalog,
        IEnumerable<MealClassificationShapeFailure> failures)
    {
        Catalog = catalog;
        Failures = Array.AsReadOnly(failures.ToArray());
    }

    public MealClassificationCatalog Catalog { get; }

    public IReadOnlyList<MealClassificationShapeFailure> Failures { get; }
}

public sealed class MealClassificationCatalog
{
    public const string VanillaCookingExpandedPackageId = "VanillaExpanded.VCookE";
    public const string VanillaCookingExpandedBakeryPackageId = "VanillaExpanded.VCookEBakery";
    public const string VanillaCookingExpandedHautePackageId = "VanillaExpanded.VCookEHaute";
    public const string VanillaCookingExpandedStewsPackageId = "VanillaExpanded.VCookEStews";
    public const string VanillaCookingExpandedSushiPackageId = "VanillaExpanded.VCookESushi";
    public const string FriedMealsPackageId = "ucp.friedmeals";
    public const string FastMealsPackageId = "Argon.CheapMeals";
    public const string FoodTextureVarietyCorePackageId = "Goat.Food.Texture.Variety.Core";
    public const string FoodTextureVarietyPackageId = "Goat.Food.Texture.Variety";
    public const string RimCuisineCorePackageId = "Mlie.RC2.Core";
    public const string RimCuisineMealsPackageId = "Mlie.RC2.MaME";
    public const string NoVanillaMealsPackageId = "Mlie.NoVanillaMeals";
    public const string VanillaExpandedFrameworkPackageId =
        "OskarPotocki.VanillaFactionsExpanded.Core";
    public const string VanillaFishingExpandedPackageId = "VanillaExpanded.VCEF";
    public const string ProcessorFrameworkPackageId = "syrchalis.processor.framework";
    public const string ReplimatPackageId = "sumghai.Replimat";
    public const string ReplimatMealsPackageId = "sumghai.ReplimatMeals";

    private static readonly Lazy<MealClassificationCatalog> CompleteOptionalCatalog =
        new(CreateCompleteOptionalCatalog);
    private static readonly string[] ReplimatSimpleMealDefNames =
    {
        "ReplimatMeals_S_Borscht", "ReplimatMeals_S_CaesarSalad",
        "ReplimatMeals_S_CubanSandwich", "ReplimatMeals_S_Currywurst",
        "ReplimatMeals_S_Dumplings", "ReplimatMeals_S_FishAndChips",
        "ReplimatMeals_S_Gyros", "ReplimatMeals_S_Kakigori",
        "ReplimatMeals_S_MargheritaPizza", "ReplimatMeals_S_MincedPorkRice",
        "ReplimatMeals_S_Pancakes", "ReplimatMeals_S_PastramiOnRye",
        "ReplimatMeals_S_PorkBuns", "ReplimatMeals_S_Poutine",
        "ReplimatMeals_S_Sachertorte", "ReplimatMeals_S_ShrimpTacos",
        "ReplimatMeals_S_Takoyaki", "ReplimatMeals_S_Tiramisu",
        "ReplimatMeals_S_Weisswurst", "ReplimatMeals_S_Zongzi"
    };
    private static readonly string[] ReplimatAdvancedMealDefNames =
    {
        "ReplimatMeals_F_BeefNoodleSoup", "ReplimatMeals_F_BeefRouladen",
        "ReplimatMeals_F_BunnyChow", "ReplimatMeals_F_ChaliapinSteak",
        "ReplimatMeals_F_ChickenAndWaffles", "ReplimatMeals_F_ChickenKyiv",
        "ReplimatMeals_F_ChickenPotPie", "ReplimatMeals_F_ChickenTikkaMasala",
        "ReplimatMeals_F_JollofRice", "ReplimatMeals_F_Kasespatzle",
        "ReplimatMeals_F_KatsuCurry", "ReplimatMeals_F_Koshari",
        "ReplimatMeals_F_LocoMoco", "ReplimatMeals_F_Nachos",
        "ReplimatMeals_F_Omurice", "ReplimatMeals_F_Paella",
        "ReplimatMeals_F_Pho", "ReplimatMeals_F_Ramen",
        "ReplimatMeals_F_Ratatouille", "ReplimatMeals_F_ShepherdsPie",
        "ReplimatMeals_F_TagliatelleAlRagu", "ReplimatMeals_F_TenzaruSoba"
    };
    private static readonly string[] ReplimatElaborateMealDefNames =
    {
        "ReplimatMeals_L_FullEnglishBreakfast", "ReplimatMeals_L_Gumbo",
        "ReplimatMeals_L_Hangi", "ReplimatMeals_L_Kaiseki",
        "ReplimatMeals_L_LobsterThermidor", "ReplimatMeals_L_SundayRoast"
    };

    internal static IReadOnlyList<string> ReplimatMealDefNames { get; } = Array.AsReadOnly(
        ReplimatSimpleMealDefNames
            .Concat(ReplimatAdvancedMealDefNames)
            .Concat(ReplimatElaborateMealDefNames)
            .ToArray());

    private readonly Dictionary<string, MealComplexity> recipes =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, MealComplexity> meals =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> preserveOriginalWork =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> packageRecipes =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, HashSet<string>> packageMeals =
        new(StringComparer.OrdinalIgnoreCase);
    private string? recordingPackageId;

    private MealClassificationCatalog()
    {
    }

    public static MealClassificationCatalog Create(IEnumerable<string> loadedPackageIds)
    {
        if (loadedPackageIds is null)
        {
            throw new ArgumentNullException(nameof(loadedPackageIds));
        }

        var catalog = new MealClassificationCatalog();
        var packages = new HashSet<string>(loadedPackageIds, StringComparer.OrdinalIgnoreCase);
        var noVanillaMeals = packages.Contains(NoVanillaMealsPackageId);
        if (!noVanillaMeals)
        {
            catalog.AddVanilla();
        }

        var vanillaExpandedFramework = packages.Contains(VanillaExpandedFrameworkPackageId);
        var vanillaCookingExpanded = vanillaExpandedFramework &&
                                     packages.Contains(VanillaCookingExpandedPackageId);
        var processorFramework = packages.Contains(ProcessorFrameworkPackageId);
        var rimCuisineCore = processorFramework && packages.Contains(RimCuisineCorePackageId);

        if (vanillaCookingExpanded)
        {
            catalog.AddPackage(VanillaCookingExpandedPackageId, catalog.AddVanillaCookingExpanded);
        }

        if (vanillaCookingExpanded && packages.Contains(VanillaCookingExpandedBakeryPackageId))
        {
            catalog.AddPackage(VanillaCookingExpandedBakeryPackageId, static () => { });
        }

        if (vanillaCookingExpanded && packages.Contains(VanillaCookingExpandedHautePackageId))
        {
            catalog.AddPackage(
                VanillaCookingExpandedHautePackageId,
                () => catalog.Add(
                    MealComplexity.Elaborate,
                    new[] { "VCE_CookMealHaute" },
                    new[] { "VCE_MealHaute" }));
        }

        if (vanillaCookingExpanded && packages.Contains(VanillaCookingExpandedStewsPackageId))
        {
            catalog.AddPackage(
                VanillaCookingExpandedStewsPackageId,
                catalog.AddVanillaCookingExpandedStews);
        }

        if (vanillaCookingExpanded &&
            packages.Contains(VanillaFishingExpandedPackageId) &&
            packages.Contains(VanillaCookingExpandedSushiPackageId))
        {
            catalog.AddPackage(
                VanillaCookingExpandedSushiPackageId,
                catalog.AddVanillaCookingExpandedSushi);
        }

        if (vanillaExpandedFramework && packages.Contains(FriedMealsPackageId))
        {
            catalog.AddPackage(
                FriedMealsPackageId,
                () => catalog.AddFriedMeals(vanillaCookingExpanded));
        }

        if (packages.Contains(FastMealsPackageId))
        {
            catalog.AddPackage(FastMealsPackageId, catalog.AddFastMeals);
        }

        if (packages.Contains(FoodTextureVarietyCorePackageId) &&
            packages.Contains(FoodTextureVarietyPackageId))
        {
            catalog.AddPackage(
                FoodTextureVarietyPackageId,
                catalog.AddFoodTextureVariety);
        }

        if (rimCuisineCore)
        {
            catalog.AddPackage(RimCuisineCorePackageId, catalog.AddRimCuisineCore);
        }

        if (rimCuisineCore && packages.Contains(RimCuisineMealsPackageId))
        {
            catalog.AddPackage(
                RimCuisineMealsPackageId,
                () => catalog.AddRimCuisineMeals(includeVanillaBulkRecipes: !noVanillaMeals));
        }

        if (packages.Contains(ReplimatPackageId) && packages.Contains(ReplimatMealsPackageId))
        {
            catalog.AddPackage(ReplimatMealsPackageId, catalog.AddReplimatMeals);
        }

        return catalog;
    }

    public static MealClassificationCatalogValidationResult CreateValidated(
        IEnumerable<string> loadedPackageIds,
        Func<string, bool> recipeDefExists,
        Func<string, bool> mealDefExists)
    {
        if (loadedPackageIds is null)
        {
            throw new ArgumentNullException(nameof(loadedPackageIds));
        }

        if (recipeDefExists is null)
        {
            throw new ArgumentNullException(nameof(recipeDefExists));
        }

        if (mealDefExists is null)
        {
            throw new ArgumentNullException(nameof(mealDefExists));
        }

        var loaded = loadedPackageIds.ToArray();
        var optimistic = Create(loaded);
        var effective = new HashSet<string>(loaded, StringComparer.OrdinalIgnoreCase);
        var failures = new List<MealClassificationShapeFailure>();
        foreach (var packageId in optimistic.packageRecipes.Keys
                     .Union(optimistic.packageMeals.Keys, StringComparer.OrdinalIgnoreCase)
                     .OrderBy(value => value, StringComparer.OrdinalIgnoreCase))
        {
            var missingRecipes = optimistic.packageRecipes[packageId]
                .Where(recipeDefName => !recipeDefExists(recipeDefName))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            var missingMeals = optimistic.packageMeals[packageId]
                .Where(mealDefName => !mealDefExists(mealDefName))
                .OrderBy(value => value, StringComparer.Ordinal)
                .ToArray();
            if (missingRecipes.Length == 0 && missingMeals.Length == 0)
            {
                continue;
            }

            effective.Remove(packageId);
            failures.Add(new MealClassificationShapeFailure(
                packageId,
                missingRecipes,
                missingMeals));
        }

        return new MealClassificationCatalogValidationResult(Create(effective), failures);
    }

    public static bool OwnsExplicitMealRegistry(string? packageId)
    {
        return packageId is not null &&
               CompleteOptionalCatalog.Value.packageMeals.ContainsKey(packageId);
    }

    public static bool IsMealRegisteredForPackage(string? packageId, string? mealDefName)
    {
        return packageId is not null && mealDefName is not null &&
               CompleteOptionalCatalog.Value.packageMeals.TryGetValue(packageId, out var mealsForPackage) &&
               mealsForPackage.Contains(mealDefName);
    }

    internal bool ContainsRegisteredMeal(string? packageId, string? mealDefName)
    {
        return packageId is not null && mealDefName is not null &&
               packageMeals.TryGetValue(packageId, out var mealsForPackage) &&
               mealsForPackage.Contains(mealDefName);
    }

    public MealComplexity? ClassifyRecipe(string recipeDefName)
    {
        return recipeDefName is not null && recipes.TryGetValue(recipeDefName, out var complexity)
            ? complexity
            : null;
    }

    public MealComplexity? ClassifyMeal(string mealDefName)
    {
        return mealDefName is not null && meals.TryGetValue(mealDefName, out var complexity)
            ? complexity
            : null;
    }

    public bool PreservesOriginalWorkAmount(string recipeDefName)
    {
        return recipeDefName is not null && preserveOriginalWork.Contains(recipeDefName);
    }

    public float AdjustWorkAmount(
        string recipeDefName,
        float originalWorkAmount,
        ImmersiveChefsSettings settings)
    {
        if (settings is null)
        {
            throw new ArgumentNullException(nameof(settings));
        }

        if (PreservesOriginalWorkAmount(recipeDefName))
        {
            return originalWorkAmount;
        }

        var complexity = ClassifyRecipe(recipeDefName);
        if (!complexity.HasValue)
        {
            return originalWorkAmount;
        }

        var multiplier = complexity.Value switch
        {
            MealComplexity.Simple => settings.SimpleRecipeTimeMultiplier,
            MealComplexity.Advanced => settings.AdvancedRecipeTimeMultiplier,
            MealComplexity.Elaborate => settings.ElaborateRecipeTimeMultiplier,
            _ => 1f
        };
        return originalWorkAmount * multiplier;
    }

    private void AddVanilla()
    {
        Add(
            MealComplexity.Simple,
            new[] { "CookMealSimple", "CookMealSimpleBulk" },
            new[] { "MealSimple" });
        Add(
            MealComplexity.Advanced,
            new[]
            {
                "CookMealFine", "CookMealFine_Veg", "CookMealFine_Meat",
                "CookMealFineBulk", "CookMealFineBulk_Meat", "CookMealFineBulk_Veg"
            },
            new[] { "MealFine", "MealFine_Meat", "MealFine_Veg" });
        Add(
            MealComplexity.Elaborate,
            new[]
            {
                "CookMealLavish", "CookMealLavish_Meat", "CookMealLavish_Veg",
                "CookMealLavishBulk", "CookMealLavishBulk_Veg", "CookMealLavishBulk_Meat"
            },
            new[] { "MealLavish", "MealLavish_Meat", "MealLavish_Veg" });
    }

    private static MealClassificationCatalog CreateCompleteOptionalCatalog()
    {
        return Create(new[]
        {
            VanillaExpandedFrameworkPackageId,
            VanillaCookingExpandedPackageId,
            VanillaCookingExpandedBakeryPackageId,
            VanillaCookingExpandedHautePackageId,
            VanillaCookingExpandedStewsPackageId,
            VanillaFishingExpandedPackageId,
            VanillaCookingExpandedSushiPackageId,
            FriedMealsPackageId,
            FastMealsPackageId,
            FoodTextureVarietyCorePackageId,
            FoodTextureVarietyPackageId,
            ProcessorFrameworkPackageId,
            RimCuisineCorePackageId,
            RimCuisineMealsPackageId,
            ReplimatPackageId,
            ReplimatMealsPackageId
        });
    }

    private void AddVanillaCookingExpanded()
    {
        Add(
            MealComplexity.Simple,
            new[]
            {
                "VCE_CookBakeSimple", "VCE_CookBakeSimpleBulk",
                "VCE_CookGrillSimple", "VCE_CookGrillSimpleBulk",
                "VCE_CookSoupSimple"
            },
            new[]
            {
                "VCE_SimpleBake", "VCE_SimpleGrill", "VCE_RuinedSimpleGrill",
                "VCE_CookedSoupSimple"
            });
        Add(
            MealComplexity.Advanced,
            new[]
            {
                "VCE_CookBakeFine", "VCE_CookBakeFineBulk",
                "VCE_CookGrillFine", "VCE_CookGrillFineBulk",
                "VCE_CookSoupFine"
            },
            new[]
            {
                "VCE_FineBake", "VCE_FineGrill", "VCE_RuinedFineGrill",
                "VCE_CookedSoupFine"
            });
        Add(
            MealComplexity.Elaborate,
            new[]
            {
                "VCE_CookBakeLavish", "VCE_CookBakeLavishBulk", "VCE_CookBakeGourmet",
                "VCE_CookGrillLavish", "VCE_CookGrillLavishhBulk", "VCE_CookGrillGourmet",
                "VCE_CookMealGourmet", "VCE_CookSoupLavish", "VCE_CookSoupGourmet"
            },
            new[]
            {
                "VCE_LavishBake", "VCE_GourmetBake",
                "VCE_LavishGrill", "VCE_GourmetGrill",
                "VCE_RuinedLavishGrill", "VCE_RuinedGourmetGrill",
                "VCE_MealGourmet", "VCE_CookedSoupLavish", "VCE_CookedSoupGourmet"
            });
    }

    private void AddVanillaCookingExpandedStews()
    {
        Add(
            MealComplexity.Simple,
            new[] { "VCE_CookStewSimple" },
            new[] { "VCE_CookedStewSimple" });
        Add(
            MealComplexity.Advanced,
            new[] { "VCE_CookStewFine" },
            new[] { "VCE_CookedStewFine" });
        Add(
            MealComplexity.Elaborate,
            new[] { "VCE_CookStewLavish" },
            new[] { "VCE_CookedStewLavish" });
    }

    private void AddVanillaCookingExpandedSushi()
    {
        Add(
            MealComplexity.Simple,
            new[]
            {
                "VCE_CookChirashizushiSimple", "VCE_CookChirashizushiSimpleBulk",
                "VCE_CookNorimakiSimple", "VCE_CookNorimakiSimpleBulk"
            },
            new[] { "VCE_Chirashizushi", "VCE_Norimaki" });
        Add(
            MealComplexity.Advanced,
            new[]
            {
                "VCE_CookUramakiFine", "VCE_CookUramakiFineBulk",
                "VCE_CookNigiriFine", "VCE_CookNigiriFineBulk"
            },
            new[] { "VCE_Uramaki", "VCE_Nigiri" });
        Add(
            MealComplexity.Elaborate,
            new[]
            {
                "VCE_CookTemakiLavish", "VCE_CookTemakiLavishBulk",
                "VCE_CookFutomakiLavish", "VCE_CookFutomakiLavishBulk",
                "VCE_CookGunkanmakiGourmet", "VCE_CookOshizushiiGourmet"
            },
            new[] { "VCE_Temaki", "VCE_Futomaki", "VCE_Gunkanmaki", "VCE_Oshizushi" });
    }

    private void AddFriedMeals(bool includeGourmet)
    {
        Add(
            MealComplexity.Simple,
            new[] { "CookFritterSimple", "CookFritterSimpleBulk" },
            new[] { "ucp_SimpleFritter" });
        Add(
            MealComplexity.Advanced,
            new[] { "CookFritterFine", "CookFritterFineBulk" },
            new[] { "ucp_FineFritter" });
        Add(
            MealComplexity.Elaborate,
            new[] { "CookFritterLavish", "CookFritterLavishBulk" },
            new[] { "ucp_LavishFritter" });
        if (includeGourmet)
        {
            Add(
                MealComplexity.Elaborate,
                new[] { "VCE_CookFritterGourmet" },
                new[] { "ucp_GourmetFritter" });
        }
    }

    private void AddFastMeals()
    {
        var simpleRecipes = new[] { "CM_CookFastMeal", "CM_CookFastMealBulk" };
        var advancedRecipes = new[]
        {
            "CM_CookFastMealDeluxe", "CM_CookFastMealDeluxe_Meat", "CM_CookFastMealDeluxe_Veg",
            "CM_CookFastMealDeluxeBulk", "CM_CookFastMealDeluxeBulk_Meat",
            "CM_CookFastMealDeluxeBulk_Veg"
        };
        Add(MealComplexity.Simple, simpleRecipes, new[] { "CM_SimpleFastMeal" });
        Add(
            MealComplexity.Advanced,
            advancedRecipes,
            new[] { "CM_DeluxeFastMeal", "CM_DeluxeFastMeal_Meat", "CM_DeluxeFastMeal_Veg" });
        preserveOriginalWork.UnionWith(simpleRecipes);
        preserveOriginalWork.UnionWith(advancedRecipes);
    }

    private void AddFoodTextureVariety()
    {
        Add(
            MealComplexity.Simple,
            new[] { "FTV_CookMealSimple", "FTV_CookMealSimpleBulk" },
            new[] { "FTV_MealSimple" });
        Add(
            MealComplexity.Advanced,
            new[] { "FTV_CookMealFine", "FTV_CookMealFineBulk" },
            new[] { "FTV_MealFine" });
        Add(
            MealComplexity.Elaborate,
            new[] { "FTV_CookMealLavish", "FTV_CookMealLavishBulk" },
            new[] { "FTV_MealLavish" });
    }

    private void AddRimCuisineCore()
    {
        Add(
            MealComplexity.Simple,
            new[] { "CookThinPottage" },
            new[] { "RC2_ThinPottage" });
        Add(
            MealComplexity.Advanced,
            new[] { "RC2_CookThickPottage" },
            new[] { "RC2_ThickPottage" });
    }

    private void AddRimCuisineMeals(bool includeVanillaBulkRecipes)
    {
        Add(
            MealComplexity.Simple,
            new[] { "RC2_CookRubaboo" },
            new[] { "RC2_Rubaboo" });
        if (includeVanillaBulkRecipes)
        {
            Add(
                MealComplexity.Advanced,
                new[] { "RC2_CookFineMealBulk" },
                Array.Empty<string>());
        }

        Add(
            MealComplexity.Elaborate,
            includeVanillaBulkRecipes
                ? new[]
                {
                    "RC2_CookLavishMealBulk", "RC2_CookExtravagantMeal",
                    "RC2_CookExtravagantMealBulk"
                }
                : new[] { "RC2_CookExtravagantMeal", "RC2_CookExtravagantMealBulk" },
            new[] { "RC2_Pizza", "RC2_ExtravagantMeal" });
    }

    private void AddReplimatMeals()
    {
        Add(
            MealComplexity.Simple,
            Array.Empty<string>(),
            ReplimatSimpleMealDefNames);
        Add(
            MealComplexity.Advanced,
            Array.Empty<string>(),
            ReplimatAdvancedMealDefNames);
        Add(
            MealComplexity.Elaborate,
            Array.Empty<string>(),
            ReplimatElaborateMealDefNames);
    }

    private void Add(
        MealComplexity complexity,
        IEnumerable<string> recipeDefNames,
        IEnumerable<string> mealDefNames)
    {
        foreach (var recipeDefName in recipeDefNames)
        {
            recipes[recipeDefName] = complexity;
            if (recordingPackageId is not null)
            {
                packageRecipes[recordingPackageId].Add(recipeDefName);
            }
        }

        foreach (var mealDefName in mealDefNames)
        {
            meals[mealDefName] = complexity;
            if (recordingPackageId is not null)
            {
                packageMeals[recordingPackageId].Add(mealDefName);
            }
        }
    }

    private void AddPackage(string packageId, Action add)
    {
        if (!packageRecipes.ContainsKey(packageId))
            packageRecipes.Add(packageId, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        if (!packageMeals.ContainsKey(packageId))
            packageMeals.Add(packageId, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        var previous = recordingPackageId;
        recordingPackageId = packageId;
        try
        {
            add();
        }
        finally
        {
            recordingPackageId = previous;
        }
    }
}
