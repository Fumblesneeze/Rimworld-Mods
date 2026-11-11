using Megamod.Features.ReadableNumbers;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Features.ReadableNumbers;

[TestFixture]
public sealed class NumberPresentationTests
{
    [Test]
    public void EachScopeUsesItsThresholdOnTheNextPresentation()
    {
        var store = new SettingsStore();
        ReadableNumbersSettings.Register(store);
        store.Register(ReadableNumbersSettings.Punctuation);
        store.SetGlobal(ReadableNumbersSettings.MinimumIntegerDigits, 6);
        store.SetGlobal(ReadableNumbersSettings.Punctuation, new(PunctuationPreset.Custom, "#", "@"));
        var binding = store.ActivateSave(new("A"));
        store.SetUseGlobal(binding, ReadableNumbersSettings.MinimumIntegerDigits, false);
        store.SetSaveOverride(binding, ReadableNumbersSettings.MinimumIntegerDigits, 4);
        var quantity = new RawDecimalQuantity("12345.00");

        Assert.That(NumberPresentation.Format(quantity, store, globalDefault: true), Is.EqualTo("12345@00"));
        Assert.That(NumberPresentation.Format(quantity, store), Is.EqualTo("12#345@00"));
        store.SetSaveOverride(binding, ReadableNumbersSettings.MinimumIntegerDigits, 6);
        Assert.That(NumberPresentation.Format(quantity, store), Is.EqualTo("12345@00"));
    }

    [Test]
    public void DisablingTheSelectedScopeRestoresOriginalDecimalAndGroupingTogether()
    {
        var store = new SettingsStore();
        ReadableNumbersSettings.Register(store);
        store.Register(ReadableNumbersSettings.Punctuation);
        store.SetGlobal(ReadableNumbersSettings.Punctuation, new(PunctuationPreset.PeriodComma, "#", "@"));
        store.SetGlobal(ReadableNumbersSettings.Enabled, false);
        var binding = store.ActivateSave(new("A"));
        store.SetUseGlobal(binding, ReadableNumbersSettings.Enabled, false);
        store.SetSaveOverride(binding, ReadableNumbersSettings.Enabled, true);
        var quantity = new RawDecimalQuantity("-12345.00", Suffix: " kg");

        Assert.That(NumberPresentation.Format(quantity, store, globalDefault: true), Is.EqualTo(quantity.OriginalText));
        Assert.That(NumberPresentation.Format(quantity, store), Is.EqualTo("-12.345,00 kg"));
        store.SetSaveOverride(binding, ReadableNumbersSettings.Enabled, false);
        Assert.That(NumberPresentation.Format(quantity, store), Is.EqualTo(quantity.OriginalText));
    }

    [Test]
    public void GlobalPreviewStaysGlobalWhileSavePreviewFollowsItsWholePreference()
    {
        var store = new SettingsStore();
        ReadableNumbersSettings.Register(store);
        store.Register(ReadableNumbersSettings.Punctuation);
        store.SetGlobal(ReadableNumbersSettings.Punctuation, new(PunctuationPreset.Custom, "#", "@"));
        var binding = store.ActivateSave(new("A"));
        store.SetUseGlobal(binding, ReadableNumbersSettings.Punctuation, false);
        store.SetSaveOverride(binding, ReadableNumbersSettings.Punctuation, new(PunctuationPreset.PeriodComma, "!", ":"));
        var quantity = new RawDecimalQuantity("12345.00");

        Assert.That(NumberPresentation.Format(quantity, store, globalDefault: true), Is.EqualTo("12#345@00"));
        Assert.That(NumberPresentation.Format(quantity, store), Is.EqualTo("12.345,00"));
        store.SetUseGlobal(binding, ReadableNumbersSettings.Punctuation, true);
        Assert.That(NumberPresentation.Format(quantity, store), Is.EqualTo("12#345@00"));
        store.SetUseGlobal(binding, ReadableNumbersSettings.Punctuation, false);
        Assert.That(NumberPresentation.Format(quantity, store), Is.EqualTo("12.345,00"));
        store.DeactivateSave();
        Assert.That(NumberPresentation.Format(quantity, store), Is.EqualTo("12#345@00"));
    }

    [TestCase(PunctuationPreset.CommaPeriod, "1,234,567.00")]
    [TestCase(PunctuationPreset.PeriodComma, "1.234.567,00")]
    [TestCase(PunctuationPreset.SpacePeriod, "1\u00a0234\u00a0567.00")]
    [TestCase(PunctuationPreset.SpaceComma, "1\u00a0234\u00a0567,00")]
    [TestCase(PunctuationPreset.ApostrophePeriod, "1'234'567.00")]
    [TestCase(PunctuationPreset.ApostropheComma, "1'234'567,00")]
    [TestCase(PunctuationPreset.Custom, "1#234#567@00")]
    public void SelectedPresetRendersWithoutReplacingTheDormantCustomPair(PunctuationPreset preset, string expected)
    {
        var store = new SettingsStore();
        ReadableNumbersSettings.Register(store);
        store.Register(ReadableNumbersSettings.Punctuation);
        var preference = new PunctuationPreference(preset, "#", "@");
        store.SetGlobal(ReadableNumbersSettings.Punctuation, preference);

        Assert.That(NumberPresentation.Format(new("1234567.00"), store), Is.EqualTo(expected));
        Assert.That(store.GetGlobal(ReadableNumbersSettings.Punctuation), Is.EqualTo(preference));
    }

    [Test]
    public void CustomSupplementaryMarksRenderAsWholeCharactersThroughTheCommonPreference()
    {
        var store = new SettingsStore();
        ReadableNumbersSettings.Register(store);
        store.Register(ReadableNumbersSettings.Punctuation);
        Assert.That(store.SetGlobal(ReadableNumbersSettings.Punctuation,
            new(PunctuationPreset.Custom, "\U0001f4a0", "\U0001f7e0")).Succeeded, Is.True);
        var quantity = new RawDecimalQuantity("-1234567.00", Suffix: " kg");

        Assert.That(NumberPresentation.Format(quantity, store),
            Is.EqualTo("-1\U0001f4a0234\U0001f4a0567\U0001f7e000 kg"));
        Assert.That(quantity.OriginalText, Is.EqualTo("-1234567.00 kg"));
    }
}
