using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class RecipeClassificationTests
{
    [TestCase("CookMealSimple", MealComplexity.Simple, 75f)]
    [TestCase("CookMealFine_Veg", MealComplexity.Advanced, 200f)]
    [TestCase("CookMealLavishBulk_Meat", MealComplexity.Elaborate, 300f)]
    public void Exact_vanilla_recipe_table_applies_the_configured_multiplier(
        string recipeDefName,
        MealComplexity expectedComplexity,
        float expectedWork)
    {
        var settings = new ImmersiveChefsSettings();
        var classifier = RecipeClassificationCatalog.CreateVanilla();

        var classification = classifier.Classify(recipeDefName);

        Assert.Multiple(() =>
        {
            Assert.That(classification, Is.EqualTo(expectedComplexity));
            Assert.That(classifier.AdjustWorkAmount(recipeDefName, 100f, settings), Is.EqualTo(expectedWork));
            Assert.That(classifier.AdjustWorkAmount("SomeMod_UnclassifiedMeal", 100f, settings), Is.EqualTo(100f));
        });
    }
}
