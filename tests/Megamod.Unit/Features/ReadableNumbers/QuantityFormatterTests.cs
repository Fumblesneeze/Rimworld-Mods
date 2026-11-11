using Megamod.Features.ReadableNumbers;
using NUnit.Framework;

namespace Megamod.Unit.Features.ReadableNumbers;

[TestFixture]
public sealed class QuantityFormatterTests
{
    [TestCase("#", "@", "1#000#000@00")]
    [TestCase(".", ",", "1.000.000,00")]
    [TestCase(" ", ".", "1\u00a0000\u00a0000.00")]
    public void ChosenPunctuationPreservesDigitsAndMakesSpaceGroupsNonbreaking(string group, string mark, string expected)
    {
        Assert.That(QuantityFormatter.Format(new("1000000.00"),
            new(GroupSeparator: group, DecimalMark: mark)), Is.EqualTo(expected));
    }

    [TestCase(true, "9,18 kg")]
    [TestCase(false, "9.18 kg")]
    public void DecimalChoiceIsIndependentOfGroupingEligibility(bool applyToSmall, string smallExpected)
    {
        var options = new NumberFormatOptions(NumberGrouping.None, GroupSeparator: ".", DecimalMark: ",",
            ApplyDecimalToSmallNumbers: applyToSmall);
        Assert.Multiple(() =>
        {
            Assert.That(QuantityFormatter.Format(new("9.18", Suffix: " kg"), options), Is.EqualTo(smallExpected));
            Assert.That(QuantityFormatter.Format(new("12345.50"), options), Is.EqualTo("12345,50"));
            Assert.That(QuantityFormatter.Format(new("12345"), options), Is.EqualTo("12345"));
        });
    }

    [TestCase("M1911")]
    [TestCase("1,234")]
    [TestCase("1e1234")]
    [TestCase("0x1234")]
    [TestCase("1234.5.6")]
    [TestCase("1234.")]
    [TestCase("12-345")]
    public void UnsupportedTokensRetainTheCompleteOriginalLabel(string token)
    {
        var quantity = new RawDecimalQuantity(token, "Value: ", " kg");
        Assert.That(QuantityFormatter.Format(quantity), Is.EqualTo(quantity.OriginalText));
    }

    [Test]
    public void EachQuantityMustMeetTheIntegerDigitThreshold()
    {
        var options = new NumberFormatOptions(MinimumIntegerDigits: 5);
        string label = QuantityFormatter.Format(new("1234"), options) + " / " +
            QuantityFormatter.Format(new("12345"), options);
        Assert.That(label, Is.EqualTo("1234 / 12,345"));
        Assert.That(QuantityFormatter.Format(new("-1234.56789", Suffix: " kg"), options),
            Is.EqualTo("-1234.56789 kg"));
    }

    [TestCase(NumberGrouping.ThreeDigits, "123,456,789.25")]
    [TestCase(NumberGrouping.ThreeThenTwoDigits, "12,34,56,789.25")]
    [TestCase(NumberGrouping.FourDigits, "1,2345,6789.25")]
    [TestCase(NumberGrouping.None, "123456789.25")]
    public void GroupingPatternOnlySeparatesIntegerDigits(NumberGrouping grouping, string expected)
    {
        Assert.That(QuantityFormatter.Format(new("123456789.25"), new(grouping)), Is.EqualTo(expected));
    }

    [Test]
    public void SignedAmountKeepsItsFractionalZerosAndUnit()
    {
        var quantity = new RawDecimalQuantity("-1234567.00", Suffix: " kg");

        Assert.That(QuantityFormatter.Format(quantity), Is.EqualTo("-1,234,567.00 kg"));
        Assert.That(quantity.OriginalText, Is.EqualTo("-1234567.00 kg"));
    }
}
