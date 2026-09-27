using NUnit.Framework;
using ThinWalls.Rendering;
using UnityEngine;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class BuildingAppearanceTests
{
    [TestCase(0, false)]
    [TestCase(6, true)]
    public void LegacySavedOffsetsInferManualChoiceOnlyWhenNonzero(int offset, bool manual)
    {
        var legacy = new BuildingAppearance(3, offset);
        Assert.That(legacy.OffsetIsManual, Is.EqualTo(manual));
        Assert.That(legacy.WithAutomaticOffset(5).OffsetStep, Is.EqualTo(manual ? offset : 5));
        Assert.That(legacy.ScalePercent, Is.EqualTo(70));
    }

    [TestCase(6, false, 5)]
    [TestCase(0, true, 0)]
    public void PersistedFlagDistinguishesAutomaticOffsetAndExplicitCenter(int offset, bool manual, int expected)
    {
        var saved = new BuildingAppearance(2, offset, manual);
        var loaded = new BuildingAppearance(saved.ScaleStep, saved.OffsetStep, saved.OffsetIsManual);
        Assert.That(loaded.WithAutomaticOffset(5).OffsetStep, Is.EqualTo(expected));
        Assert.That(loaded.ScalePercent, Is.EqualTo(80));
    }

    [Test]
    public void RemovingWallsRestoresAutomaticCenterWithoutLosingScale()
    {
        var automatic = new BuildingAppearance(2).WithAutomaticOffset(5).WithAutomaticOffset(0);
        Assert.That(automatic.OffsetStep, Is.Zero);
        Assert.That(automatic.OffsetIsManual, Is.False);
        Assert.That(automatic.ScalePercent, Is.EqualTo(80));
    }

    [Test]
    public void AutomaticOffsetUpdatesWithoutChangingShrinkButManualCenterStaysCentered()
    {
        var appearance = new BuildingAppearance().WithAutomaticOffset(6).NextScale();
        Assert.That((appearance.ScalePercent, appearance.OffsetStep), Is.EqualTo((90, 6)));
        appearance = appearance.WithAutomaticOffset(5);
        Assert.That((appearance.ScalePercent, appearance.OffsetStep), Is.EqualTo((90, 5)));
        for (int i = 0; i < 4; i++) appearance = appearance.NextOffset();
        Assert.That(appearance.OffsetStep, Is.Zero);
        Assert.That(appearance.OffsetIsManual, Is.True);
        Assert.That(appearance.WithAutomaticOffset(6).OffsetStep, Is.Zero);
    }

    [TestCase(false, true)]
    [TestCase(true, false)]
    public void ActualPawnsAreIndependentButStatueArtworkKeepsTheBuildingTransform(bool statueArtwork, bool independent)
    {
        Assert.That(BuildingAppearanceDrawScope.IsIndependentPawn(statueArtwork), Is.EqualTo(independent));
    }

    [Test]
    public void MeshTransformScalesEveryOrientedPartAroundTheOwnerNotItsOwnOrigin()
    {
        Matrix4x4 original = Matrix4x4.identity;
        original.m00 = 0; original.m02 = 2;
        original.m20 = -3; original.m22 = 0;
        original.m03 = 12; original.m13 = 7; original.m23 = 18;
        var appearance = new BuildingAppearance(5, 2);
        var pivot = new Vector3(10, 3, 20);
        Matrix4x4 transformed = appearance.TransformMatrix(original, pivot);
        foreach (Vector3 local in new[] { Vector3.zero, new Vector3(1, 2, 3), new Vector3(-2, 1, 4) })
        {
            Vector3 expected = appearance.Transform(original.MultiplyPoint3x4(local), pivot);
            Assert.That((transformed.MultiplyPoint3x4(local) - expected).sqrMagnitude, Is.LessThan(.000001f));
        }
        Assert.That(new BuildingAppearance().TransformMatrix(original, pivot), Is.EqualTo(original));
    }

    [Test]
    public void ShrinkCyclesInTenPercentStepsAndReturnsExactlyToDefault()
    {
        var appearance = new BuildingAppearance();
        foreach (int percent in new[] { 90, 80, 70, 60, 50, 100 })
        {
            appearance = appearance.NextScale();
            Assert.That(appearance.ScalePercent, Is.EqualTo(percent));
        }
        Assert.That(appearance.IsDefault, Is.True);
    }

    [Test]
    public void OffsetVisitsEightNeighborsThenCenterWithoutChangingScale()
    {
        var appearance = new BuildingAppearance().NextScale();
        var offsets = new[] { (0f, .2f), (.2f, .2f), (.2f, 0f), (.2f, -.2f),
            (0f, -.2f), (-.2f, -.2f), (-.2f, 0f), (-.2f, .2f), (0f, 0f) };
        foreach (var expected in offsets)
        {
            appearance = appearance.NextOffset();
            Assert.That((appearance.OffsetX, appearance.OffsetZ), Is.EqualTo(expected));
            Assert.That(appearance.ScalePercent, Is.EqualTo(90));
        }
        Assert.That(appearance.OffsetStep, Is.Zero);
    }

    [Test]
    public void TransformScalesAboutNativeCenterBeforeOffsetWithoutChangingAltitude()
    {
        var appearance = new BuildingAppearance(5, 2);
        Vector3 result = appearance.Transform(new Vector3(12f, 7f, 18f), new Vector3(10f, 3f, 20f));
        Assert.That(result.x, Is.EqualTo(11.2f).Within(.00001f));
        Assert.That(result.z, Is.EqualTo(19.2f).Within(.00001f));
        Assert.That(result.y, Is.EqualTo(7f));
        Assert.That(new BuildingAppearance(-1, 99).IsDefault, Is.True);
        Assert.That(new BuildingAppearance().Transform(result, Vector3.zero), Is.EqualTo(result));
    }
}
