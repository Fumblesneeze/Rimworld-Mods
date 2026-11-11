using System.Globalization;
using Megamod.Features.MessagePlacement;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.MessagePlacement;

[TestFixture]
public sealed class MessageOffsetInputTests
{
    [TestCase("en-US", "12.5", "-12.5")]
    [TestCase("de-DE", "12,5", "-12,5")]
    public void CompletePairUsesTheNativeUiCulture(string culture, string horizontal, string vertical)
    {
        Assert.That(MessageOffsetInput.TryParse(horizontal, vertical, CultureInfo.GetCultureInfo(culture), out var pair), Is.True);
        Assert.That(pair, Is.EqualTo(new MessageOffsets(12.5, -12.5)));
    }

    [TestCase("12", "")]
    [TestCase("NaN", "0")]
    [TestCase("0", "Infinity")]
    [TestCase("100.1", "0")]
    public void InvalidOrIncompletePairPublishesNoPartialCoordinates(string horizontal, string vertical)
    {
        Assert.That(MessageOffsetInput.TryParse(horizontal, vertical, CultureInfo.InvariantCulture, out var pair), Is.False);
        Assert.That(pair, Is.EqualTo(default(MessageOffsets)));
    }
}
