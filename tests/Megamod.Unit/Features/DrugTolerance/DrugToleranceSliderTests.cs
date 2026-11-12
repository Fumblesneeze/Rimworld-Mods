using Megamod.Features.DrugTolerance.Core;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.DrugTolerance;

[TestFixture]
public sealed class DrugToleranceSliderTests
{
    [TestCase(0f, DrugToleranceChoiceKind.Limit, 0f)]
    [TestCase(20f, DrugToleranceChoiceKind.Limit, .2f)]
    [TestCase(99.8f, DrugToleranceChoiceKind.Limit, 1f)]
    [TestCase(100f, DrugToleranceChoiceKind.Limit, 1f)]
    [TestCase(101f, DrugToleranceChoiceKind.Any, 0f)]
    public void Explicit_native_slider_selection_repairs_only_the_selected_choice(float position, DrugToleranceChoiceKind kind, float limit)
    {
        var original = DrugToleranceChoice.Decode("future:unknown");
        var result = DrugToleranceSlider.Apply(original, position, true);
        Assert.That(result.Kind, Is.EqualTo(kind));
        Assert.That(result.Limit, Is.EqualTo(limit));
        Assert.That(original.Encoding, Is.EqualTo("future:unknown"));
    }

    [Test]
    public void Painting_preserves_exact_fraction_and_invalid_encoding()
    {
        foreach (var original in new[] { DrugToleranceChoice.Numeric(.125f), DrugToleranceChoice.Decode("future:unknown"), DrugToleranceChoice.Decode(null) })
            Assert.That(DrugToleranceSlider.Apply(original, DrugToleranceSlider.Position(original), false), Is.SameAs(original));
        Assert.That(DrugToleranceSlider.Position(DrugToleranceChoice.Numeric(1)), Is.EqualTo(100));
        Assert.That(DrugToleranceSlider.Position(DrugToleranceChoice.Any), Is.EqualTo(101));
    }

    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(-1f)]
    [TestCase(102f)]
    public void Invalid_native_value_preserves_original(float position)
    {
        var original = DrugToleranceChoice.Numeric(.125f);
        Assert.That(DrugToleranceSlider.Apply(original, position, true), Is.SameAs(original));
    }
}
