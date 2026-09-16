using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class DiningStandardsTests
{
    [TestCase(KitchenMaterialKind.Steel, false, false, true)]
    [TestCase(KitchenMaterialKind.Steel, false, true, false)]
    [TestCase(KitchenMaterialKind.Steel, true, false, false)]
    [TestCase(KitchenMaterialKind.Wood, false, false, false)]
    public void Refined_fallback_uses_clean_steel_without_a_crafting_grade(
        KitchenMaterialKind material, bool dirty, bool refinedAvailable, bool expected)
    {
        Assert.That(DiningStandardPolicy.MeetsMaterial(material, dirty, ServiceMaterialTier.Refined,
            refinedAvailable), Is.EqualTo(expected));
    }

    [TestCase(0, ServiceMaterialTier.Basic, 0f, MealComplexity.Simple, 0)]
    [TestCase(2, ServiceMaterialTier.Durable, 0.2f, MealComplexity.Simple, 35)]
    [TestCase(4, ServiceMaterialTier.Refined, 0.45f, MealComplexity.Advanced, 50)]
    [TestCase(99, ServiceMaterialTier.Refined, 0.6f, MealComplexity.Advanced, 65)]
    public void ColonyRows_AreDeterministic(int order, ServiceMaterialTier material, float comfort, MealComplexity complexity, int score)
    {
        var requirement = DiningStandardPolicy.ForExpectationOrder(order);

        Assert.Multiple(() =>
        {
            Assert.That(requirement.Material, Is.EqualTo(material));
            Assert.That(requirement.MinimumComfort, Is.EqualTo(comfort));
            Assert.That(requirement.Complexity, Is.EqualTo(complexity));
            Assert.That(requirement.MinimumQuality, Is.EqualTo(score));
        });
    }

    [TestCase(0, ServiceMaterialTier.Basic, 0f, MealComplexity.Simple, 0)]
    [TestCase(1, ServiceMaterialTier.Refined, 0.4f, MealComplexity.Advanced, 50)]
    [TestCase(3, ServiceMaterialTier.Silver, 0.55f, MealComplexity.Advanced, 65)]
    [TestCase(5, ServiceMaterialTier.Silver, 0.7f, MealComplexity.Elaborate, 65)]
    [TestCase(99, ServiceMaterialTier.Gold, 0.8f, MealComplexity.Elaborate, 80)]
    public void RoyaltyRows_AreDeterministic(int seniority, ServiceMaterialTier material, float comfort, MealComplexity complexity, int score)
    {
        var requirement = DiningStandardPolicy.ForTitleSeniority(seniority);

        Assert.That(requirement, Is.EqualTo(new DiningRequirement(material, comfort, complexity, score)));
    }

    [Test]
    public void CombiningRows_NeverLowersARequirement()
    {
        var combined = DiningStandardPolicy.Combine(
            new DiningRequirement(ServiceMaterialTier.Refined, 0.6f, MealComplexity.Advanced, 65),
            new DiningRequirement(ServiceMaterialTier.Gold, 0.8f, MealComplexity.Elaborate, 80));

        Assert.That(combined, Is.EqualTo(new DiningRequirement(ServiceMaterialTier.Gold, 0.8f, MealComplexity.Elaborate, 80)));
    }
}
