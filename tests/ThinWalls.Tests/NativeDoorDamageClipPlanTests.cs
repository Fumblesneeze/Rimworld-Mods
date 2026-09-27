using NUnit.Framework;
using ThinWalls.Rendering;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class NativeDoorDamageClipPlanTests
{
    [TestCase(0f)]
    [TestCase(0.5f)]
    [TestCase(1f)]
    public void EveryDamageGradeIsClippedToItsMovingLeafWithoutStretchingSource(float open)
    {
        NativeDoorMoverPlan mover = NativeDoorMoverPlan.Compile(open);
        foreach (NativeThinDamageMark mark in NativeThinDamagePlan.Compile(ThinWallDamageGrade.Severe, 173))
        {
            bool left = mark.LongitudinalCenter < 0f;
            NativeDoorLeafPlan leaf = left ? mover.Left : mover.Right;
            float moved = mark.LongitudinalCenter +
                          (left ? -1f : 1f) * open * NativeDoorMoverPlan.MaximumSlide;
            Assert.That(NativeDoorDamageClipPlan.TryClip(mark, moved, leaf, out NativeDoorDamageClip clip),
                Is.True);
            Assert.That(clip.VisibleMin, Is.GreaterThanOrEqualTo(leaf.LongitudinalMin - 0.00001f));
            Assert.That(clip.VisibleMax, Is.LessThanOrEqualTo(leaf.LongitudinalMax + 0.00001f));
            Assert.That(clip.SourceMin, Is.InRange(0f, 1f));
            Assert.That(clip.SourceMax, Is.InRange(0f, 1f));
            Assert.That(clip.SourceMax - clip.SourceMin,
                Is.EqualTo(clip.Width / mark.Size).Within(0.00001f));
        }
    }
}
