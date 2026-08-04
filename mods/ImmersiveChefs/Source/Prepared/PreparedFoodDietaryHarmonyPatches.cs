using HarmonyLib;
using RimWorld;
using Verse;

namespace ImmersiveChefs;

[HarmonyPatch(typeof(Bill), nameof(Bill.IsFixedOrAllowedIngredient), typeof(Thing))]
internal static class PreparedFoodBillIngredientPatch
{
    private static void Postfix(Bill __instance, Thing __0, ref bool __result)
    {
        if (!__result ||
            (__0 as ThingWithComps)?.GetComp<CompPreparedFood>() is not { ExactSourcesHidden: true } prepared)
        {
            return;
        }

        __result = PreparedFoodDietaryPolicy.AllSourcesAllowed(
            prepared.Contributions.Select(contribution => contribution.DefName),
            sourceDefName =>
            {
                var sourceDef = DefDatabase<ThingDef>.GetNamedSilentFail(sourceDefName);
                return sourceDef is not null && __instance.IsFixedOrAllowedIngredient(sourceDef);
            });
    }
}

[HarmonyPatch(typeof(FoodPolicy), nameof(FoodPolicy.Allows), typeof(Thing))]
internal static class PreparedFoodPolicyPatch
{
    private static void Postfix(FoodPolicy __instance, Thing __0, ref bool __result)
    {
        if (!__result)
        {
            return;
        }

        var sourceDefNames = PreparedFoodDietaryRuntime.SourceDefNamesFor(__0);
        if (sourceDefNames.Count == 0)
        {
            return;
        }

        __result = PreparedFoodDietaryPolicy.AllSourcesAllowed(
            sourceDefNames,
            sourceDefName =>
            {
                var sourceDef = DefDatabase<ThingDef>.GetNamedSilentFail(sourceDefName);
                return sourceDef is not null && __instance.Allows(sourceDef);
            });
    }
}

[HarmonyPatch(
    typeof(FoodUtility),
    nameof(FoodUtility.ThoughtsFromIngesting),
    typeof(Pawn),
    typeof(Thing),
    typeof(ThingDef))]
internal static class PreparedFoodIngestThoughtPatch
{
    private static void Prefix(Thing __1, out HiddenIngredientScope __state)
    {
        __state = HiddenIngredientScope.Inject(__1);
    }

    private static Exception? Finalizer(Exception? __exception, HiddenIngredientScope? __state)
    {
        __state?.Dispose();
        return __exception;
    }

    private sealed class HiddenIngredientScope
    {
        private readonly CompIngredients? ingredients;
        private readonly IReadOnlyList<ThingDef> added;

        private HiddenIngredientScope(CompIngredients? ingredients, IReadOnlyList<ThingDef> added)
        {
            this.ingredients = ingredients;
            this.added = added;
        }

        internal static HiddenIngredientScope Inject(Thing food)
        {
            var compIngredients = (food as ThingWithComps)?.GetComp<CompIngredients>();
            var sourceDefNames = PreparedFoodDietaryRuntime.SourceDefNamesFor(food);
            if (compIngredients is null || sourceDefNames.Count == 0)
            {
                return new HiddenIngredientScope(null, Array.Empty<ThingDef>());
            }

            var addedDefs = new List<ThingDef>();
            foreach (var sourceDefName in sourceDefNames)
            {
                var sourceDef = DefDatabase<ThingDef>.GetNamedSilentFail(sourceDefName);
                if (sourceDef is null || compIngredients.ingredients.Contains(sourceDef))
                {
                    continue;
                }

                compIngredients.ingredients.Add(sourceDef);
                addedDefs.Add(sourceDef);
            }

            return new HiddenIngredientScope(compIngredients, addedDefs.AsReadOnly());
        }

        internal void Dispose()
        {
            if (ingredients is null)
            {
                return;
            }

            foreach (var addedDef in added)
            {
                ingredients.ingredients.Remove(addedDef);
            }
        }
    }
}

internal static class PreparedFoodDietaryRuntime
{
    internal static IReadOnlyList<string> SourceDefNamesFor(Thing thing)
    {
        var preparedSources = (thing as ThingWithComps)?.GetComp<CompPreparedFood>()?.Contributions
            .Select(contribution => contribution.DefName) ?? Enumerable.Empty<string>();
        var hiddenMealSources = (thing as ThingWithComps)?.GetComp<CompCulinaryState>()
            ?.PeekCurrentServingWithoutThermalUpdate()?.HiddenSourceDefNames ?? Array.Empty<string>();

        return preparedSources
            .Concat(hiddenMealSources)
            .Where(source => !string.IsNullOrWhiteSpace(source))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(source => source, StringComparer.OrdinalIgnoreCase)
            .ToList()
            .AsReadOnly();
    }
}
