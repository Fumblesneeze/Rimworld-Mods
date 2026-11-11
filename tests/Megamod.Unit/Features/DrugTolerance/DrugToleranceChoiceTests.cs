using Megamod.Features.DrugTolerance.Core;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.DrugTolerance;

[TestFixture]
public sealed class DrugToleranceChoiceTests
{
    [Test]
    public void NumericZeroAndOneRoundTripIndependentlyOfAny()
    {
        foreach (var limit in new[] { 0f, 0.2f, 1f })
        {
            var choice = DrugToleranceChoice.Decode(DrugToleranceChoice.Numeric(limit).Encoding);
            Assert.That(choice.Kind, Is.EqualTo(DrugToleranceChoiceKind.Limit));
            Assert.That(choice.Limit, Is.EqualTo(limit));
        }
        Assert.That(DrugToleranceChoice.Decode(DrugToleranceChoice.Any.Encoding).Kind,
            Is.EqualTo(DrugToleranceChoiceKind.Any));
    }

    [TestCase("")]
    [TestCase("0.2")]
    [TestCase("2:limit:0.2")]
    [TestCase("1:unknown")]
    [TestCase("1:any:extra")]
    [TestCase("1:limit:NaN")]
    [TestCase("1:limit:Infinity")]
    [TestCase("1:limit:-0.01")]
    [TestCase("1:limit:1.01")]
    [TestCase("1:limit:0,2")]
    public void InvalidAndUnknownSavedChoicesRetainExactEncodingForReview(string encoding)
    {
        var choice = DrugToleranceChoice.Decode(encoding);
        Assert.That(choice.Kind, Is.EqualTo(DrugToleranceChoiceKind.NeedsReview));
        Assert.That(choice.Encoding, Is.EqualTo(encoding));
    }

    [Test]
    public void MissingFieldIsAnyAndKnownEncodingIsNotRewritten()
    {
        Assert.That(DrugToleranceChoice.Decode(null).Kind, Is.EqualTo(DrugToleranceChoiceKind.Any));
        Assert.That(DrugToleranceChoice.Decode(null).Encoding, Is.Null);
        Assert.That(DrugToleranceChoice.Decode("1:limit:2E-1").Encoding, Is.EqualTo("1:limit:2E-1"));
    }

    [TestCase(float.NaN)]
    [TestCase(float.PositiveInfinity)]
    [TestCase(-0.01f)]
    [TestCase(1.01f)]
    public void ExplicitInvalidEditsAreRejected(float value) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => DrugToleranceChoice.Numeric(value));

    [Test]
    public void CodecIsIndependentOfCurrentCulture()
    {
        var previous = System.Globalization.CultureInfo.CurrentCulture;
        try
        {
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("de-DE");
            var choice = DrugToleranceChoice.Numeric(0.25f);
            Assert.That(choice.Encoding, Is.EqualTo("1:limit:0.25"));
            Assert.That(DrugToleranceChoice.Decode(choice.Encoding).Limit, Is.EqualTo(0.25f));
        }
        finally { System.Globalization.CultureInfo.CurrentCulture = previous; }
    }
}
