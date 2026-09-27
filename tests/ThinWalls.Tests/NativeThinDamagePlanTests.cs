using System.Linq;
using NUnit.Framework;
using ThinWalls.Rendering;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class NativeThinDamagePlanTests
{
    [TestCase(ThinWallDamageGrade.None, 0)]
    [TestCase(ThinWallDamageGrade.Moderate, 1)]
    [TestCase(ThinWallDamageGrade.Heavy, 2)]
    [TestCase(ThinWallDamageGrade.Severe, 3)]
    public void DamageGradesUseProgressivelyMoreCoreScratchMarks(ThinWallDamageGrade grade, int count)
    {
        Assert.That(NativeThinDamagePlan.Compile(grade, 173).Count, Is.EqualTo(count));
    }

    [Test]
    public void EveryRotatedScratchBoundingBoxStaysInsideTheCompleteThinBody()
    {
        foreach (int seed in Enumerable.Range(1, 64))
        foreach (NativeThinDamageMark mark in NativeThinDamagePlan.Compile(ThinWallDamageGrade.Severe, seed))
        {
            double radians = mark.RotationDegrees * System.Math.PI / 180d;
            double halfExtent = mark.Size *
                                (System.Math.Abs(System.Math.Cos(radians)) +
                                 System.Math.Abs(System.Math.Sin(radians))) * 0.5d;
            Assert.That(mark.NormalCenter - halfExtent,
                Is.GreaterThanOrEqualTo(-17f / 60f - 0.00001f));
            Assert.That(mark.NormalCenter + halfExtent,
                Is.LessThanOrEqualTo(17f / 60f + 0.00001f));
            Assert.That(mark.LongitudinalCenter - halfExtent,
                Is.GreaterThanOrEqualTo(-0.5f - 0.00001f));
            Assert.That(mark.LongitudinalCenter + halfExtent,
                Is.LessThanOrEqualTo(0.5f + 0.00001f));
            Assert.That(mark.CoreScratchIndex, Is.InRange(0, 2));
        }
    }

    [Test]
    public void PlanIsDeterministicAndGradesRetainEarlierMarks()
    {
        NativeThinDamageMark[] moderate = NativeThinDamagePlan
            .Compile(ThinWallDamageGrade.Moderate, 9137).ToArray();
        NativeThinDamageMark[] heavy = NativeThinDamagePlan
            .Compile(ThinWallDamageGrade.Heavy, 9137).ToArray();
        NativeThinDamageMark[] severe = NativeThinDamagePlan
            .Compile(ThinWallDamageGrade.Severe, 9137).ToArray();
        Assert.That(heavy.Take(moderate.Length), Is.EqualTo(moderate));
        Assert.That(severe.Take(heavy.Length), Is.EqualTo(heavy));
        Assert.That(NativeThinDamagePlan.Compile(ThinWallDamageGrade.Severe, 9137),
            Is.EqualTo(severe));
    }

    [Test]
    public void EveryGradeAddsAnOrdinaryZoomDiscernibleCoreScratch()
    {
        NativeThinDamageMark[] marks = NativeThinDamagePlan
            .Compile(ThinWallDamageGrade.Severe, 173).ToArray();
        Assert.That(marks, Has.All.Matches<NativeThinDamageMark>(mark => mark.Size >= 0.50f),
            "Core scratches below half-cell scale disappeared at ordinary in-game zoom.");
    }

    [Test]
    public void StaticDamageUsesTheNativeScratchDepthWithoutLeavingTheEdgeBody()
    {
        const float buildingAltitude = 5.55f;
        Assert.That(NativeThinDamagePlan.StaticOverlayAltitude(buildingAltitude),
            Is.EqualTo(buildingAltitude + 15f / 82f).Within(0.000001f));
        Assert.That(NativeThinDamagePlan.StaticOverlayAltitude(buildingAltitude) - buildingAltitude,
            Is.LessThan(17f / 60f),
            "The native transparent-depth lift must remain inside the complete edge body.");
    }
}
