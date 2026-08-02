using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

internal static class PreparedFoodRuntime
{
    private static float activeRotMultiplier = 4f;

    internal static void Initialize(float rotMultiplier)
    {
        activeRotMultiplier = Math.Max(1f, Math.Min(10f, rotMultiplier));
    }

    internal static float ActiveRotMultiplier => activeRotMultiplier;

    internal static IEnumerable<Thing> ApplyProducts(
        IEnumerable<Thing> products,
        RecipeDef recipeDef,
        Pawn worker,
        IReadOnlyList<Thing> ingredients)
    {
        foreach (var product in products)
        {
            if (recipeDef.defName == "ImmersiveChefs_PrepareIngredients" &&
                product is ThingWithComps withComps &&
                withComps.GetComp<CompPreparedFood>() is { } prepared)
            {
                var perProduct = Math.Max(1, product.stackCount);
                var contributions = ingredients.Select(ingredient => new IngredientContribution(
                    ingredient.def.defName,
                    ingredient.GetStatValue(StatDefOf.Nutrition) * ingredient.stackCount / perProduct,
                    Math.Max(1, ingredient.stackCount),
                    CraftsmanshipScore(ingredient)));
                prepared.Initialize(new PreparedFoodState(
                    contributions,
                    PreparedFoodCalculator.SkillQuality(worker.skills?.GetSkill(SkillDefOf.Cooking).Level ?? 0),
                    worker.ThingID,
                    DietaryClassification.For(ingredients),
                    exactSourcesHidden: false,
                    ingredientPoisonChance: ingredients
                        .OfType<ThingWithComps>()
                        .Select(value => value.GetComp<CompFoodPoisonable>())
                        .Where(value => value is not null)
                        .Sum(_ => 0f)));
            }

            yield return product;
        }
    }

    private static int CraftsmanshipScore(Thing ingredient)
    {
        var quality = QualityUtility.TryGetQuality(ingredient, out var found)
            ? found
            : QualityCategory.Normal;
        return quality switch
        {
            QualityCategory.Awful => 0,
            QualityCategory.Poor => 25,
            QualityCategory.Normal => 50,
            QualityCategory.Good => 65,
            QualityCategory.Excellent => 80,
            QualityCategory.Masterwork => 90,
            QualityCategory.Legendary => 100,
            _ => 50
        };
    }

    internal static float PreparedNutritionFraction(Job job)
    {
        var targets = job.targetQueueB;
        if (targets is null || targets.Count == 0)
        {
            return 0f;
        }

        var total = 0f;
        var prepared = 0f;
        for (var index = 0; index < targets.Count; index++)
        {
            var thing = targets[index].Thing;
            if (thing is null)
            {
                continue;
            }

            var count = job.countQueue is { Count: > 0 } && index < job.countQueue.Count
                ? job.countQueue[index]
                : thing.stackCount;
            var nutrition = thing.GetStatValue(StatDefOf.Nutrition) * count;
            total += nutrition;
            if ((thing as ThingWithComps)?.GetComp<CompPreparedFood>() is not null)
            {
                prepared += nutrition;
            }
        }

        return total <= 0f ? 0f : Math.Max(0f, Math.Min(1f, prepared / total));
    }
}

public static class DietaryClassification
{
    public static DietaryFlags For(IEnumerable<Thing> ingredients)
    {
        var flags = DietaryFlags.None;
        foreach (var ingredient in ingredients)
        {
            flags |= ForFoodType(
                ingredient.def.ingestible?.foodType ?? FoodTypeFlags.None,
                ingredient.def.defName);
        }

        if ((flags & (DietaryFlags.Animal | DietaryFlags.HumanMeat | DietaryFlags.InsectMeat)) == 0)
        {
            flags |= DietaryFlags.VegetarianCompatible;
        }

        return flags;
    }

    public static DietaryFlags ForFoodType(FoodTypeFlags foodType, string defName)
    {
        var flags = (foodType & (FoodTypeFlags.Meat | FoodTypeFlags.AnimalProduct)) != 0
            ? DietaryFlags.Animal
            : DietaryFlags.Plant;
        if (defName.IndexOf("Human", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            flags |= DietaryFlags.HumanMeat;
        }

        if (defName.IndexOf("Insect", StringComparison.OrdinalIgnoreCase) >= 0)
        {
            flags |= DietaryFlags.InsectMeat;
        }

        return flags;
    }

    internal static DietaryFlags ForDefs(IEnumerable<ThingDef> ingredients)
    {
        var flags = DietaryFlags.None;
        foreach (var ingredient in ingredients)
        {
            var name = ingredient.defName;
            var foodType = ingredient.ingestible?.foodType ?? FoodTypeFlags.None;
            flags |= (foodType & (FoodTypeFlags.Meat | FoodTypeFlags.AnimalProduct)) != 0
                ? DietaryFlags.Animal
                : DietaryFlags.Plant;
            if (name.IndexOf("Human", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                flags |= DietaryFlags.HumanMeat;
            }

            if (name.IndexOf("Insect", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                flags |= DietaryFlags.InsectMeat;
            }
        }

        if ((flags & (DietaryFlags.Animal | DietaryFlags.HumanMeat | DietaryFlags.InsectMeat)) == 0)
        {
            flags |= DietaryFlags.VegetarianCompatible;
        }

        return flags;
    }
}

[HarmonyPatch(typeof(Thing), nameof(Thing.CanStackWith))]
internal static class PreparedFoodStackPatch
{
    private static void Postfix(Thing __instance, Thing other, ref bool __result)
    {
        if (!__result)
        {
            return;
        }

        var left = (__instance as ThingWithComps)?.GetComp<CompPreparedFood>();
        var right = (other as ThingWithComps)?.GetComp<CompPreparedFood>();
        if (left is not null || right is not null)
        {
            __result = left is not null && right is not null && left.CompatibleWith(right);
        }
    }
}

[HarmonyPatch(typeof(CompRottable), nameof(CompRottable.CompTickRare))]
internal static class PreparedFoodRotPatch
{
    private static void Prefix(CompRottable __instance, out float __state)
    {
        __state = __instance.RotProgress;
    }

    private static void Postfix(CompRottable __instance, float __state)
    {
        if ((__instance.parent as ThingWithComps)?.GetComp<CompPreparedFood>() is null)
        {
            return;
        }

        var vanillaDelta = Math.Max(0f, __instance.RotProgress - __state);
        __instance.RotProgress += vanillaDelta * (PreparedFoodRuntime.ActiveRotMultiplier - 1f);
    }
}
