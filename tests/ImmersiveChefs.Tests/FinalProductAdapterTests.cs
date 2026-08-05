using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class FinalProductAdapterTests
{
    [Test]
    public void Optional_patch_static_initializers_run_only_after_exact_passive_shape_validation()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                AdaptiveMealBillCompatibility.IsSafeToInitialize(
                    "AdaptiveMealBill",
                    "1.0.0.0",
                    "AdaptiveMealBill.AdaptiveRecipeDef",
                    adaptiveRecipeIsRecipeDef: true,
                    "activeSubRecipes",
                    selectedRecipeDictionaryShape: true,
                    "AdaptiveMealBill.Patch_MakeRecipeProducts",
                    makeProductsPrefixShape: true,
                    allTypesShareAssembly: true),
                Is.True);
            Assert.That(
                AdaptiveMealBillCompatibility.IsSafeToInitialize(
                    "Lookalike",
                    "1.0.0.0",
                    "AdaptiveMealBill.AdaptiveRecipeDef",
                    adaptiveRecipeIsRecipeDef: true,
                    "activeSubRecipes",
                    selectedRecipeDictionaryShape: true,
                    "AdaptiveMealBill.Patch_MakeRecipeProducts",
                    makeProductsPrefixShape: true,
                    allTypesShareAssembly: true),
                Is.False);
            Assert.That(
                OvercookedMealsCompatibility.IsSafeToInitialize(
                    "OvercookedMeals",
                    "1.0.0.0",
                    "OvercookedMeals.HarmonyPatches",
                    "TryMakeOvercookedPrefix",
                    prefixShape: true),
                Is.True);
            Assert.That(
                OvercookedMealsCompatibility.IsSafeToInitialize(
                    "Lookalike",
                    "1.0.0.0",
                    "OvercookedMeals.HarmonyPatches",
                    "TryMakeOvercookedPrefix",
                    prefixShape: true),
                Is.False);
        });
    }

    [Test]
    public void Exact_adaptive_meal_bill_shape_is_supported()
    {
        Assert.That(
            AdaptiveMealBillCompatibility.IsSupported(
                assemblyName: "AdaptiveMealBill",
                assemblyVersion: "1.0.0.0",
                adaptiveRecipeTypeName: "AdaptiveMealBill.AdaptiveRecipeDef",
                adaptiveRecipeIsRecipeDef: true,
                selectedRecipeFieldName: "activeSubRecipes",
                selectedRecipeDictionaryShape: true,
                makeProductsPrefixTypeName: "AdaptiveMealBill.Patch_MakeRecipeProducts",
                makeProductsPrefixShape: true,
                makeProductsPatchOwner: "rabiosus.AdaptiveMealBill"),
            Is.True);
    }

    [TestCase("Lookalike", "1.0.0.0", "AdaptiveMealBill.AdaptiveRecipeDef", true, "activeSubRecipes", true, "AdaptiveMealBill.Patch_MakeRecipeProducts", true, "rabiosus.AdaptiveMealBill")]
    [TestCase("AdaptiveMealBill", "2.0.0.0", "AdaptiveMealBill.AdaptiveRecipeDef", true, "activeSubRecipes", true, "AdaptiveMealBill.Patch_MakeRecipeProducts", true, "rabiosus.AdaptiveMealBill")]
    [TestCase("AdaptiveMealBill", "1.0.0.0", "Changed.AdaptiveRecipeDef", true, "activeSubRecipes", true, "AdaptiveMealBill.Patch_MakeRecipeProducts", true, "rabiosus.AdaptiveMealBill")]
    [TestCase("AdaptiveMealBill", "1.0.0.0", "AdaptiveMealBill.AdaptiveRecipeDef", false, "activeSubRecipes", true, "AdaptiveMealBill.Patch_MakeRecipeProducts", true, "rabiosus.AdaptiveMealBill")]
    [TestCase("AdaptiveMealBill", "1.0.0.0", "AdaptiveMealBill.AdaptiveRecipeDef", true, "changed", true, "AdaptiveMealBill.Patch_MakeRecipeProducts", true, "rabiosus.AdaptiveMealBill")]
    [TestCase("AdaptiveMealBill", "1.0.0.0", "AdaptiveMealBill.AdaptiveRecipeDef", true, "activeSubRecipes", false, "AdaptiveMealBill.Patch_MakeRecipeProducts", true, "rabiosus.AdaptiveMealBill")]
    [TestCase("AdaptiveMealBill", "1.0.0.0", "AdaptiveMealBill.AdaptiveRecipeDef", true, "activeSubRecipes", true, "Changed.Prefix", true, "rabiosus.AdaptiveMealBill")]
    [TestCase("AdaptiveMealBill", "1.0.0.0", "AdaptiveMealBill.AdaptiveRecipeDef", true, "activeSubRecipes", true, "AdaptiveMealBill.Patch_MakeRecipeProducts", false, "rabiosus.AdaptiveMealBill")]
    [TestCase("AdaptiveMealBill", "1.0.0.0", "AdaptiveMealBill.AdaptiveRecipeDef", true, "activeSubRecipes", true, "AdaptiveMealBill.Patch_MakeRecipeProducts", true, "lookalike.owner")]
    public void Changed_or_lookalike_adaptive_shape_fails_closed(
        string assemblyName,
        string assemblyVersion,
        string adaptiveRecipeTypeName,
        bool adaptiveRecipeIsRecipeDef,
        string selectedRecipeFieldName,
        bool selectedRecipeDictionaryShape,
        string makeProductsPrefixTypeName,
        bool makeProductsPrefixShape,
        string makeProductsPatchOwner)
    {
        Assert.That(
            AdaptiveMealBillCompatibility.IsSupported(
                assemblyName,
                assemblyVersion,
                adaptiveRecipeTypeName,
                adaptiveRecipeIsRecipeDef,
                selectedRecipeFieldName,
                selectedRecipeDictionaryShape,
                makeProductsPrefixTypeName,
                makeProductsPrefixShape,
                makeProductsPatchOwner),
            Is.False);
    }

    [Test]
    public void Exact_overcooked_meals_shape_is_supported()
    {
        Assert.That(
            OvercookedMealsCompatibility.IsSupported(
                assemblyName: "OvercookedMeals",
                assemblyVersion: "1.0.0.0",
                prefixTypeName: "OvercookedMeals.HarmonyPatches",
                prefixMethodName: "TryMakeOvercookedPrefix",
                prefixShape: true,
                postProcessPatchOwner: "binchcannon.rimworld.overcookedmeals",
                finalMealDefName: "OvercookedMeals_MealOvercooked"),
            Is.True);
    }

    [TestCase("Lookalike", "1.0.0.0", "OvercookedMeals.HarmonyPatches", "TryMakeOvercookedPrefix", true, "binchcannon.rimworld.overcookedmeals", "OvercookedMeals_MealOvercooked")]
    [TestCase("OvercookedMeals", "2.0.0.0", "OvercookedMeals.HarmonyPatches", "TryMakeOvercookedPrefix", true, "binchcannon.rimworld.overcookedmeals", "OvercookedMeals_MealOvercooked")]
    [TestCase("OvercookedMeals", "1.0.0.0", "Changed.Patches", "TryMakeOvercookedPrefix", true, "binchcannon.rimworld.overcookedmeals", "OvercookedMeals_MealOvercooked")]
    [TestCase("OvercookedMeals", "1.0.0.0", "OvercookedMeals.HarmonyPatches", "Changed", true, "binchcannon.rimworld.overcookedmeals", "OvercookedMeals_MealOvercooked")]
    [TestCase("OvercookedMeals", "1.0.0.0", "OvercookedMeals.HarmonyPatches", "TryMakeOvercookedPrefix", false, "binchcannon.rimworld.overcookedmeals", "OvercookedMeals_MealOvercooked")]
    [TestCase("OvercookedMeals", "1.0.0.0", "OvercookedMeals.HarmonyPatches", "TryMakeOvercookedPrefix", true, "lookalike.owner", "OvercookedMeals_MealOvercooked")]
    [TestCase("OvercookedMeals", "1.0.0.0", "OvercookedMeals.HarmonyPatches", "TryMakeOvercookedPrefix", true, "binchcannon.rimworld.overcookedmeals", "ChangedMeal")]
    public void Changed_or_lookalike_overcooked_shape_fails_closed(
        string assemblyName,
        string assemblyVersion,
        string prefixTypeName,
        string prefixMethodName,
        bool prefixShape,
        string postProcessPatchOwner,
        string finalMealDefName)
    {
        Assert.That(
            OvercookedMealsCompatibility.IsSupported(
                assemblyName,
                assemblyVersion,
                prefixTypeName,
                prefixMethodName,
                prefixShape,
                postProcessPatchOwner,
                finalMealDefName),
            Is.False);
    }

    [TestCase(100, 65)]
    [TestCase(60, 25)]
    [TestCase(20, 0)]
    public void Overcooked_quality_penalty_is_explicit_severe_and_bounded(int original, int expected)
    {
        Assert.Multiple(() =>
        {
            Assert.That(OvercookedMealQualityPolicy.Penalty, Is.EqualTo(35));
            Assert.That(OvercookedMealQualityPolicy.Apply(original), Is.EqualTo(expected));
        });
    }

    [Test]
    public void Final_product_ledger_accepts_each_surviving_object_once_by_identity()
    {
        var ledger = new FinalProductLedger<object>();
        var survivor = new object();

        Assert.Multiple(() =>
        {
            Assert.That(ledger.TryBegin(survivor), Is.True);
            Assert.That(ledger.TryBegin(survivor), Is.False);
            Assert.That(ledger.TryBegin(new object()), Is.True);
        });
    }
}
