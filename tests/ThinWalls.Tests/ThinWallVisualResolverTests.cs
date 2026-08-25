using NUnit.Framework;
using ThinWalls.Rendering;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class ThinWallVisualResolverTests
{
    [TestCase("Stony", ThinWallMaterialFamily.Stone)]
    [TestCase("Woody", ThinWallMaterialFamily.Wood)]
    [TestCase("Metallic", ThinWallMaterialFamily.Metal)]
    public void StuffCategorySelectsItsConstructionFamily(
        string category,
        ThinWallMaterialFamily expected)
    {
        Assert.That(ThinWallVisualResolver.ResolveMaterialFamily(new[] { category }), Is.EqualTo(expected));
    }

    [Test]
    public void UnknownOrMissingStuffUsesTheNeutralStoneFallback()
    {
        Assert.Multiple(() =>
        {
            Assert.That(ThinWallVisualResolver.ResolveMaterialFamily(null),
                Is.EqualTo(ThinWallMaterialFamily.Stone));
            Assert.That(ThinWallVisualResolver.ResolveMaterialFamily(new[] { "Fabric" }),
                Is.EqualTo(ThinWallMaterialFamily.Stone));
        });
    }

    [TestCase(100, 100, ThinWallDamageGrade.None)]
    [TestCase(76, 100, ThinWallDamageGrade.None)]
    [TestCase(75, 100, ThinWallDamageGrade.Moderate)]
    [TestCase(51, 100, ThinWallDamageGrade.Moderate)]
    [TestCase(50, 100, ThinWallDamageGrade.Heavy)]
    [TestCase(26, 100, ThinWallDamageGrade.Heavy)]
    [TestCase(25, 100, ThinWallDamageGrade.Severe)]
    [TestCase(1, 100, ThinWallDamageGrade.Severe)]
    public void HitPointRatioSelectsThreeProgressiveDamageGrades(
        int hitPoints,
        int maxHitPoints,
        ThinWallDamageGrade expected)
    {
        Assert.That(ThinWallVisualResolver.DamageGrade(hitPoints, maxHitPoints), Is.EqualTo(expected));
    }
}
