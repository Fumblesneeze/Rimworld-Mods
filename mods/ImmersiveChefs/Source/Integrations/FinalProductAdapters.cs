using System.Collections;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

internal static class AdaptiveMealBillCompatibility
{
    internal const string AssemblyName = "AdaptiveMealBill";
    internal const string AdaptiveRecipeTypeName = "AdaptiveMealBill.AdaptiveRecipeDef";
    internal const string HarmonyPatchesTypeName = "AdaptiveMealBill.HarmonyPatches";
    internal const string SelectedRecipeFieldName = "activeSubRecipes";
    internal const string MakeProductsPrefixTypeName = "AdaptiveMealBill.Patch_MakeRecipeProducts";
    internal const string MakeProductsPrefixMethodName = "Prefix";
    internal const string MakeProductsPatchOwner = "rabiosus.AdaptiveMealBill";

    internal static bool IsSafeToInitialize(
        string? assemblyName,
        string? assemblyVersion,
        string? adaptiveRecipeTypeName,
        bool adaptiveRecipeIsRecipeDef,
        string? selectedRecipeFieldName,
        bool selectedRecipeDictionaryShape,
        string? makeProductsPrefixTypeName,
        bool makeProductsPrefixShape,
        bool allTypesShareAssembly)
    {
        return assemblyName == AssemblyName &&
               adaptiveRecipeTypeName == AdaptiveRecipeTypeName &&
               adaptiveRecipeIsRecipeDef &&
               selectedRecipeFieldName == SelectedRecipeFieldName &&
               selectedRecipeDictionaryShape &&
               makeProductsPrefixTypeName == MakeProductsPrefixTypeName &&
               makeProductsPrefixShape &&
               allTypesShareAssembly;
    }

    internal static bool IsSupported(
        string? assemblyName,
        string? assemblyVersion,
        string? adaptiveRecipeTypeName,
        bool adaptiveRecipeIsRecipeDef,
        string? selectedRecipeFieldName,
        bool selectedRecipeDictionaryShape,
        string? makeProductsPrefixTypeName,
        bool makeProductsPrefixShape,
        string? makeProductsPatchOwner)
    {
        return IsSafeToInitialize(
                   assemblyName,
                   assemblyVersion,
                   adaptiveRecipeTypeName,
                   adaptiveRecipeIsRecipeDef,
                   selectedRecipeFieldName,
                   selectedRecipeDictionaryShape,
                   makeProductsPrefixTypeName,
                   makeProductsPrefixShape,
                   allTypesShareAssembly: true) &&
               makeProductsPatchOwner == MakeProductsPatchOwner;
    }
}

internal static class AdaptiveMealBillAdapter
{
    private static Type? adaptiveRecipeType;
    private static FieldInfo? selectedRecipesField;

    internal static bool Enabled { get; private set; }

    internal static bool TryInitialize(out string reason)
    {
        try
        {
            return TryInitializeCore(out reason);
        }
        catch (Exception exception)
        {
            Enabled = false;
            adaptiveRecipeType = null;
            selectedRecipesField = null;
            reason = $"Adaptive Meal Bill shape validation failed ({exception.GetType().Name}: {exception.Message})";
            return false;
        }
    }

    private static bool TryInitializeCore(out string reason)
    {
        if (Enabled)
        {
            reason = string.Empty;
            return true;
        }

        var recipeType = AccessTools.TypeByName(AdaptiveMealBillCompatibility.AdaptiveRecipeTypeName);
        var patchesType = AccessTools.TypeByName(AdaptiveMealBillCompatibility.HarmonyPatchesTypeName);
        var prefixType = AccessTools.TypeByName(AdaptiveMealBillCompatibility.MakeProductsPrefixTypeName);
        var selectedField = patchesType?.GetField(
            AdaptiveMealBillCompatibility.SelectedRecipeFieldName,
            BindingFlags.NonPublic | BindingFlags.Static);
        var prefix = prefixType?.GetMethod(
            AdaptiveMealBillCompatibility.MakeProductsPrefixMethodName,
            BindingFlags.NonPublic | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(RecipeDef).MakeByRefType(), typeof(Pawn) },
            modifiers: null);
        var assembly = recipeType?.Assembly;
        var fieldShape = selectedField?.FieldType.IsGenericType == true &&
                         selectedField.FieldType.GetGenericTypeDefinition() == typeof(Dictionary<,>) &&
                         selectedField.FieldType.GetGenericArguments() is { Length: 2 } arguments &&
                         arguments[0] == typeof(Bill) && arguments[1] == typeof(RecipeDef);
        var prefixShape = prefix?.ReturnType == typeof(void) &&
                          prefix.DeclaringType == prefixType;
        var allTypesShareAssembly = assembly is not null &&
                                    patchesType?.Assembly == assembly &&
                                    prefixType?.Assembly == assembly;
        var safeToInitialize = AdaptiveMealBillCompatibility.IsSafeToInitialize(
            assembly?.GetName().Name,
            assembly?.GetName().Version?.ToString(),
            recipeType?.FullName,
            recipeType is not null && typeof(RecipeDef).IsAssignableFrom(recipeType),
            selectedField?.Name,
            fieldShape,
            prefixType?.FullName,
            prefixShape,
            allTypesShareAssembly);
        if (!safeToInitialize)
        {
            reason =
                "the installed Adaptive Meal Bill passive shape differs from the validated 1.6 contract " +
                $"(assembly={assembly?.GetName().Name ?? "missing"}; " +
                $"version={assembly?.GetName().Version?.ToString() ?? "missing"}; " +
                $"recipeType={recipeType?.FullName ?? "missing"}; " +
                $"field={selectedField?.Name ?? "missing"}; fieldShape={fieldShape}; " +
                $"prefix={prefixType?.FullName ?? "missing"}; prefixShape={prefixShape}; " +
                $"sameAssembly={allTypesShareAssembly})";
            return false;
        }

        if (patchesType is not null)
        {
            // StaticConstructorOnStartup normally reaches this later in startup. The adapter's
            // finalized-Def callback needs the upstream patch ledger now. Passive assembly/type/
            // field/method validation above must succeed before invoking third-party code.
            RuntimeHelpers.RunClassConstructor(patchesType.TypeHandle);
        }

        var target = AccessTools.Method(
            typeof(GenRecipe),
            nameof(GenRecipe.MakeRecipeProducts),
            new[]
            {
                typeof(RecipeDef), typeof(Pawn), typeof(List<Thing>), typeof(Thing),
                typeof(IBillGiver), typeof(Precept_ThingStyle), typeof(ThingStyleDef),
                typeof(int?)
            });
        var patchOwner = target is null || prefix is null
            ? null
            : Harmony.GetPatchInfo(target)?.Prefixes
                .SingleOrDefault(patch =>
                    patch.PatchMethod is { } patchMethod &&
                    patchMethod.DeclaringType == prefixType &&
                    patchMethod.Name == prefix.Name)?.owner;
        var supported = AdaptiveMealBillCompatibility.IsSupported(
            assembly?.GetName().Name,
            assembly?.GetName().Version?.ToString(),
            recipeType?.FullName,
            recipeType is not null && typeof(RecipeDef).IsAssignableFrom(recipeType),
            selectedField?.Name,
            fieldShape,
            prefixType?.FullName,
            prefixShape,
            patchOwner);
        var selectedDictionaryShape = selectedField?.GetValue(null) is IDictionary;
        if (!supported || selectedField is null || recipeType is null ||
            !selectedDictionaryShape)
        {
            reason =
                "the installed Adaptive Meal Bill shape differs from the validated 1.6 contract " +
                $"(assembly={assembly?.GetName().Name ?? "missing"}; " +
                $"version={assembly?.GetName().Version?.ToString() ?? "missing"}; " +
                $"recipeType={recipeType?.FullName ?? "missing"}; " +
                $"recipeBase={recipeType is not null && typeof(RecipeDef).IsAssignableFrom(recipeType)}; " +
                $"field={selectedField?.Name ?? "missing"}; fieldShape={fieldShape}; " +
                $"dictionary={selectedDictionaryShape}; prefix={prefixType?.FullName ?? "missing"}; " +
                $"prefixShape={prefixShape}; owner={patchOwner ?? "missing"})";
            return false;
        }

        adaptiveRecipeType = recipeType;
        selectedRecipesField = selectedField;
        Enabled = true;
        reason = string.Empty;
        Log.Message("[ImmersiveChefs] Adaptive Meal Bill adapter active; ware reservation follows its selected concrete recipe.");
        return true;
    }

    internal static bool Controls(RecipeDef? recipe)
    {
        return Enabled &&
               ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.AdaptiveMealBill) &&
               recipe is not null &&
               adaptiveRecipeType?.IsInstanceOfType(recipe) == true;
    }

    internal static bool TryResolveConcreteRecipe(Job job, out RecipeDef? recipe)
    {
        recipe = job.RecipeDef;
        if (!Controls(recipe))
        {
            return recipe is not null;
        }

        try
        {
            var selected = selectedRecipesField?.GetValue(null) as IDictionary;
            if (job.bill is null || selected is null || !selected.Contains(job.bill) ||
                selected[job.bill] is not RecipeDef concrete ||
                adaptiveRecipeType?.IsInstanceOfType(concrete) == true)
            {
                recipe = null;
                return false;
            }

            recipe = concrete;
            return true;
        }
        catch (Exception exception)
        {
            DisableAfterInvocationFailure(exception);
            recipe = null;
            return false;
        }
    }

    private static void DisableAfterInvocationFailure(Exception exception)
    {
        Enabled = false;
        adaptiveRecipeType = null;
        selectedRecipesField = null;
        OptionalIntegrationDiagnostics.WarnOnce(
            OptionalIntegration.AdaptiveMealBill,
            $"selected-recipe lookup failed ({exception.GetType().Name}: {exception.Message})");
    }
}

internal static class OvercookedMealsCompatibility
{
    internal const string AssemblyName = "OvercookedMeals";
    internal const string PrefixTypeName = "OvercookedMeals.HarmonyPatches";
    internal const string PrefixMethodName = "TryMakeOvercookedPrefix";
    internal const string PatchOwner = "binchcannon.rimworld.overcookedmeals";
    internal const string FinalMealDefName = "OvercookedMeals_MealOvercooked";

    internal static bool IsSafeToInitialize(
        string? assemblyName,
        string? assemblyVersion,
        string? prefixTypeName,
        string? prefixMethodName,
        bool prefixShape)
    {
        return assemblyName == AssemblyName &&
               prefixTypeName == PrefixTypeName &&
               prefixMethodName == PrefixMethodName &&
               prefixShape;
    }

    internal static bool IsSupported(
        string? assemblyName,
        string? assemblyVersion,
        string? prefixTypeName,
        string? prefixMethodName,
        bool prefixShape,
        string? postProcessPatchOwner,
        string? finalMealDefName)
    {
        return IsSafeToInitialize(
                   assemblyName,
                   assemblyVersion,
                   prefixTypeName,
                   prefixMethodName,
                   prefixShape) &&
               postProcessPatchOwner == PatchOwner &&
               finalMealDefName == FinalMealDefName;
    }
}

internal static class OvercookedMealQualityPolicy
{
    internal const int Penalty = 35;

    internal static int Apply(int quality) => Math.Max(0, Math.Min(100, quality) - Penalty);
}

internal static class OvercookedMealsAdapter
{
    private static ThingDef? finalMealDef;

    internal static bool Enabled { get; private set; }

    internal static bool TryInitialize(out string reason)
    {
        try
        {
            return TryInitializeCore(out reason);
        }
        catch (Exception exception)
        {
            Enabled = false;
            finalMealDef = null;
            reason = $"Overcooked Meals shape validation failed ({exception.GetType().Name}: {exception.Message})";
            return false;
        }
    }

    private static bool TryInitializeCore(out string reason)
    {
        if (Enabled)
        {
            reason = string.Empty;
            return true;
        }

        var prefixType = AccessTools.TypeByName(OvercookedMealsCompatibility.PrefixTypeName);
        var prefix = prefixType?.GetMethod(
            OvercookedMealsCompatibility.PrefixMethodName,
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[]
            {
                typeof(Thing).MakeByRefType(), typeof(RecipeDef), typeof(Pawn),
                typeof(Precept_ThingStyle), typeof(ThingStyleDef), typeof(int?)
            },
            modifiers: null);
        var assembly = prefixType?.Assembly;
        var prefixShape = prefix?.ReturnType == typeof(void) && prefix.DeclaringType == prefixType;
        var safeToInitialize = OvercookedMealsCompatibility.IsSafeToInitialize(
            assembly?.GetName().Name,
            assembly?.GetName().Version?.ToString(),
            prefixType?.FullName,
            prefix?.Name,
            prefixShape);
        if (!safeToInitialize)
        {
            reason =
                "the installed Overcooked Meals passive shape differs from the validated 1.6 contract " +
                $"(assembly={assembly?.GetName().Name ?? "missing"}; " +
                $"version={assembly?.GetName().Version?.ToString() ?? "missing"}; " +
                $"prefix={prefixType?.FullName ?? "missing"}.{prefix?.Name ?? "missing"}; " +
                $"prefixShape={prefixShape})";
            return false;
        }

        if (prefixType is not null)
        {
            RuntimeHelpers.RunClassConstructor(prefixType.TypeHandle);
        }

        var target = AccessTools.Method(
            typeof(GenRecipe),
            "PostProcessProduct",
            new[]
            {
                typeof(Thing), typeof(RecipeDef), typeof(Pawn),
                typeof(Precept_ThingStyle), typeof(ThingStyleDef), typeof(int?)
            });
        var patchOwner = target is null || prefix is null
            ? null
            : Harmony.GetPatchInfo(target)?.Prefixes
                .SingleOrDefault(patch =>
                    patch.PatchMethod is { } patchMethod &&
                    patchMethod.DeclaringType == prefixType &&
                    patchMethod.Name == prefix.Name)?.owner;
        var mealDef = DefDatabase<ThingDef>.GetNamedSilentFail(
            OvercookedMealsCompatibility.FinalMealDefName);
        var supported = OvercookedMealsCompatibility.IsSupported(
            assembly?.GetName().Name,
            assembly?.GetName().Version?.ToString(),
            prefixType?.FullName,
            prefix?.Name,
            prefixShape,
            patchOwner,
            mealDef?.defName);
        if (!supported || mealDef?.modContentPack?.PackageId is not { } ownerPackageId ||
            !ownerPackageId.Equals("binchcannon.overcookedmeals", StringComparison.OrdinalIgnoreCase))
        {
            reason =
                "the installed Overcooked Meals shape differs from the validated 1.6 contract " +
                $"(assembly={assembly?.GetName().Name ?? "missing"}; " +
                $"version={assembly?.GetName().Version?.ToString() ?? "missing"}; " +
                $"prefix={prefixType?.FullName ?? "missing"}.{prefix?.Name ?? "missing"}; " +
                $"prefixShape={prefixShape}; owner={patchOwner ?? "missing"}; " +
                $"finalDef={mealDef?.defName ?? "missing"}; " +
                $"defOwner={mealDef?.modContentPack?.PackageId ?? "missing"})";
            return false;
        }

        finalMealDef = mealDef;
        Enabled = true;
        reason = string.Empty;
        Log.Message("[ImmersiveChefs] Overcooked Meals adapter active; final survivors receive one severe culinary-quality penalty.");
        return true;
    }

    internal static bool IsFinalSurvivor(Thing product)
    {
        return Enabled &&
               ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.OvercookedMeals) &&
               ReferenceEquals(product.def, finalMealDef);
    }
}

internal sealed class FinalProductLedger<T> where T : class
{
    private readonly HashSet<T> finalized = new(ReferenceComparer.Instance);

    internal bool TryBegin(T value) => finalized.Add(value);

    private sealed class ReferenceComparer : IEqualityComparer<T>
    {
        internal static readonly ReferenceComparer Instance = new();

        public bool Equals(T? x, T? y) => ReferenceEquals(x, y);

        public int GetHashCode(T obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
