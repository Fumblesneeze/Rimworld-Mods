using NUnit.Framework;
using ThinWalls.Rendering;
using UnityEngine;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class BuildingAppearanceIconTests
{
    [Test]
    public void QuarterTurnsKeepTranslatedAndScaledIconsInsideTheirButton()
    {
        Matrix4x4 parent = Matrix4x4.identity;
        parent.m00 = parent.m11 = 1.25f;
        parent.m03 = 700f;
        parent.m13 = 800f;
        for (int quarter = 0; quarter < 4; quarter++)
        {
            Matrix4x4 transform = parent * BuildingAppearanceIcons.RotationAround(new Vector2(20f, 20f), quarter * 90f);
            Vector3 center = transform.MultiplyPoint3x4(new Vector3(20f, 20f));
            Vector3 tip = transform.MultiplyPoint3x4(new Vector3(39f, 20f));
            Assert.That(center.x, Is.EqualTo(725f).Within(0.001f));
            Assert.That(center.y, Is.EqualTo(825f).Within(0.001f));
            float[] x = { 748.75f, 725f, 701.25f, 725f };
            float[] y = { 825f, 848.75f, 825f, 801.25f };
            Assert.That(tip.x, Is.EqualTo(x[quarter]).Within(0.001f));
            Assert.That(tip.y, Is.EqualTo(y[quarter]).Within(0.001f));
        }
    }

    [Test]
    public void ShrinkPointsInwardWhileOffsetMovesOneUnscaledObject()
    {
        for (int i = 0; i < 2; i++)
        {
            BuildingAppearanceIcons.ShrinkArrow(i, out Vector2 tail, out Vector2 tip);
            Assert.That((tip - new Vector2(20f, 20f)).sqrMagnitude,
                Is.LessThan((tail - new Vector2(20f, 20f)).sqrMagnitude));
            Assert.That(tail.x, Is.InRange(3.5f, 36.5f));
            Assert.That(tail.y, Is.InRange(3.5f, 36.5f));
        }
        BuildingAppearanceIcons.OffsetArrow(out Vector2 source, out Vector2 destination);
        Assert.That(source, Is.EqualTo(new Vector2(22f, 20f)));
        Assert.That(destination, Is.EqualTo(new Vector2(40f, 20f)));
        Assert.That(BuildingAppearanceIcons.OffsetObject.xMax, Is.LessThan(source.x));
        Assert.That(BuildingAppearanceIcons.OffsetObject.center.y, Is.EqualTo(source.y));
    }

    [Test]
    public void SymbolsStayBetweenNativeStateBadgeAndLabelAtBothUiScales()
    {
        foreach (float scale in new[] { 1f, 1.25f })
        {
            var bounds = BuildingAppearanceIcons.SymbolBounds(75f * scale, 15f * scale, 15f * scale);
            Assert.That(bounds.width, Is.EqualTo(54f * scale));
            Assert.That(bounds.height, Is.EqualTo(46f * scale));
            Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(10f * scale));
            Assert.That(bounds.xMax, Is.LessThanOrEqualTo(65f * scale));
            Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(22f * scale));
            Assert.That(bounds.yMax, Is.LessThanOrEqualTo(68f * scale));
        }
    }

    [TestCase(22f, 22f)]
    [TestCase(40f, 22f)]
    public void LargeOrWrappedLabelsAndBadgesKeepTheirClearance(float labelHeight, float badgeHeight)
    {
        Rect bounds = BuildingAppearanceIcons.SymbolBounds(75f, labelHeight, badgeHeight);
        Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(3f + badgeHeight + 2f));
        Assert.That(bounds.yMax, Is.LessThanOrEqualTo(75f - labelHeight + 12f - 4f));
        Assert.That(bounds.width / bounds.height, Is.EqualTo(54f / 46f).Within(0.001f));
        Assert.That(bounds.center.x, Is.EqualTo(37.5f));
    }
}
