using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class EmbeddedPlateDestructionPolicyTests
{
    [TestCase(false, 0f, false)]
    [TestCase(false, 1f, false)]
    [TestCase(true, 0f, false)]
    [TestCase(true, 0.0001f, true)]
    [TestCase(true, 1f, true)]
    public void Only_fire_destroys_a_plate_with_positive_effective_flammability(
        bool causedByFire,
        float effectiveFlammability,
        bool expected)
    {
        Assert.That(
            EmbeddedPlateDestructionPolicy.ShouldDestroy(causedByFire, effectiveFlammability),
            Is.EqualTo(expected));
    }

    [Test]
    public void Invalid_effective_flammability_is_treated_as_nonflammable()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                EmbeddedPlateDestructionPolicy.ShouldDestroy(true, float.NaN),
                Is.False);
            Assert.That(
                EmbeddedPlateDestructionPolicy.ShouldDestroy(true, float.NegativeInfinity),
                Is.False);
        });
    }
}
