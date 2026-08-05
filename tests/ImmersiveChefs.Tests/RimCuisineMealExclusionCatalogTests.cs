using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class RimCuisineMealExclusionCatalogTests
{
    [TestCase("Mlie.RC2.Core", "RC2_Hardtack")]
    [TestCase("Mlie.RC2.Core", "RC2_MealCandy")]
    [TestCase("Mlie.RC2.Core", "RC2_MealPastry")]
    [TestCase("Mlie.RC2.Core", "RC2_MealCrustyPeanuts")]
    [TestCase("Mlie.RC2.MaME", "RC2_CannedMeal")]
    [TestCase("Mlie.RC2.MaME", "RC2_CannedMeat")]
    [TestCase("Mlie.RC2.MaME", "RC2_CannedFruit")]
    [TestCase("Mlie.RC2.MaME", "RC2_CannedVegetables")]
    [TestCase("Mlie.RC2.MaME", "RC2_MealIceCream")]
    [TestCase("Mlie.RC2.MaME", "RC2_MealChocolateIceCream")]
    [TestCase("Mlie.RC2.MaME", "RC2_MealCrisps")]
    [TestCase("Mlie.RC2.MaME", "RC2_MealCupcake")]
    public void Audited_preserved_and_snack_products_are_excluded(
        string packageId,
        string defName)
    {
        Assert.That(RimCuisineMealExclusionCatalog.IsExcluded(packageId, defName), Is.True);
    }

    [TestCase("Mlie.RC2.Core", "RC2_ThinPottage")]
    [TestCase("Mlie.RC2.Core", "RC2_ThickPottage")]
    [TestCase("Mlie.RC2.MaME", "RC2_Rubaboo")]
    [TestCase("Mlie.RC2.MaME", "RC2_Pizza")]
    [TestCase("Mlie.RC2.MaME", "RC2_ExtravagantMeal")]
    [TestCase("Mlie.RC2.MaME.Lookalike", "RC2_CannedMeal")]
    [TestCase("Some.Other.Mod", "RC2_Hardtack")]
    public void Registered_meals_and_wrong_package_owners_are_not_excluded(
        string packageId,
        string defName)
    {
        Assert.That(RimCuisineMealExclusionCatalog.IsExcluded(packageId, defName), Is.False);
    }
}
