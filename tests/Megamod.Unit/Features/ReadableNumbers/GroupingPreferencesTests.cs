using Megamod.Features.ReadableNumbers;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Features.ReadableNumbers;

[TestFixture]
public sealed class GroupingPreferencesTests
{
    [Test]
    public void DefaultsAndIndependentSavePreferencesSurviveRoundtripAndInheritance()
    {
        var store = CreateStore();
        store.SetGlobal(ReadableNumbersSettings.Punctuation, new(PunctuationPreset.PeriodComma, ",", "."));
        Assert.That(NumberPresentation.Format(new("123456789.25"), store), Is.EqualTo("123.456.789,25"));
        Assert.That(NumberPresentation.Format(new("9.18"), store), Is.EqualTo("9,18"));
        store.SetGlobal(ReadableNumbersSettings.Grouping, NumberGrouping.FourDigits);
        store.SetGlobal(ReadableNumbersSettings.SmallDecimals, false);
        var binding = store.ActivateSave(new("A"));
        store.SetUseGlobal(binding, ReadableNumbersSettings.Grouping, false);
        store.SetSaveOverride(binding, ReadableNumbersSettings.Grouping, NumberGrouping.ThreeThenTwoDigits);
        store.SetUseGlobal(binding, ReadableNumbersSettings.SmallDecimals, false);
        store.SetSaveOverride(binding, ReadableNumbersSettings.SmallDecimals, true);
        var globals = store.ExportGlobals();
        var saved = store.ExportSave(binding);
        var loaded = CreateStore();
        loaded.LoadGlobals(globals);
        var loadedBinding = loaded.ActivateSave(saved);

        Assert.That(NumberPresentation.Format(new("123456789.25"), loaded), Is.EqualTo("12.34.56.789,25"));
        Assert.That(NumberPresentation.Format(new("123456789.25"), loaded, true), Is.EqualTo("1.2345.6789,25"));
        Assert.That(NumberPresentation.Format(new("9.18"), loaded), Is.EqualTo("9,18"));
        Assert.That(NumberPresentation.Format(new("9.18"), loaded, true), Is.EqualTo("9.18"));
        loaded.SetUseGlobal(loadedBinding, ReadableNumbersSettings.Grouping, true);
        loaded.SetUseGlobal(loadedBinding, ReadableNumbersSettings.SmallDecimals, true);
        loaded.SetGlobal(ReadableNumbersSettings.Grouping, NumberGrouping.None);
        Assert.That(NumberPresentation.Format(new("123456789.25"), loaded), Is.EqualTo("123456789,25"));
        Assert.That(NumberPresentation.Format(new("9.18"), loaded), Is.EqualTo("9.18"));
        loaded.SetUseGlobal(loadedBinding, ReadableNumbersSettings.Grouping, false);
        loaded.SetUseGlobal(loadedBinding, ReadableNumbersSettings.SmallDecimals, false);
        Assert.That(NumberPresentation.Format(new("123456789.25"), loaded), Is.EqualTo("12.34.56.789,25"));
        Assert.That(NumberPresentation.Format(new("9.18"), loaded), Is.EqualTo("9,18"));
        loaded.DeactivateSave();
        Assert.That(NumberPresentation.Format(new("9.18"), loaded), Is.EqualTo("9.18"));
        Assert.That(loaded.SetSaveOverride(loadedBinding, ReadableNumbersSettings.SmallDecimals, true).Succeeded, Is.False);
    }

    [TestCase(null)]
    [TestCase("invalid")]
    public void MalformedSmallDecimalOverrideFallsBackWithoutBecomingInheritance(string? encoded)
    {
        var store = CreateStore();
        store.SetGlobal(ReadableNumbersSettings.SmallDecimals, false);
        store.SetGlobal(ReadableNumbersSettings.Punctuation, new(PunctuationPreset.PeriodComma, ",", "."));
        string id = ReadableNumbersSettings.SmallDecimals.Id;
        var binding = store.ActivateSave(new("bad", new Dictionary<string, SaveSettingValue> { [id] = new(false, encoded) }));

        Assert.That(NumberPresentation.Format(new("9.18"), store), Is.EqualTo("9,18"));
        Assert.That(store.ReadEffective(ReadableNumbersSettings.SmallDecimals).ErrorKey, Is.EqualTo("Megamod_Settings_InvalidStoredValue"));
        Assert.That(store.ExportSave(binding).Values[id], Is.EqualTo(new SaveSettingValue(false, encoded)));
        Assert.That(store.GetGlobal(ReadableNumbersSettings.SmallDecimals), Is.False);
    }

    [TestCase("ThreeDigits,FourDigits")]
    [TestCase("99")]
    [TestCase("unexpected")]
    public void MalformedStoredGroupingUsesItsOwnBuiltInAndPreservesThePayload(string encoded)
    {
        var store = CreateStore();
        string id = ReadableNumbersSettings.Grouping.Id;
        store.LoadGlobals(new(new Dictionary<string, string> { [id] = encoded }));
        Assert.That(store.ReadGlobal(ReadableNumbersSettings.Grouping), Is.EqualTo(
            new SettingReadResult<NumberGrouping>(NumberGrouping.ThreeDigits, "Megamod_Settings_InvalidStoredValue")));
        Assert.That(store.ExportGlobals().Values[id], Is.EqualTo(encoded));
        store.SetGlobal(ReadableNumbersSettings.Grouping, NumberGrouping.None);
        var save = new SaveSettingsSnapshot("bad", new Dictionary<string, SaveSettingValue> { [id] = new(false, encoded) });
        var binding = store.ActivateSave(save);

        Assert.Multiple(() =>
        {
            Assert.That(store.ReadEffective(ReadableNumbersSettings.Grouping), Is.EqualTo(
                new SettingReadResult<NumberGrouping>(NumberGrouping.ThreeDigits, "Megamod_Settings_InvalidStoredValue")));
            Assert.That(store.ReadRequested(ReadableNumbersSettings.Grouping, save).Value, Is.EqualTo(NumberGrouping.ThreeDigits));
            Assert.That(store.ExportSave(binding).Values[id], Is.EqualTo(new SaveSettingValue(false, encoded)));
            Assert.That(store.GetGlobal(ReadableNumbersSettings.Grouping), Is.EqualTo(NumberGrouping.None));
        });
    }

    [TestCase(true, "9,18 kg")]
    [TestCase(false, "9.18 kg")]
    public void SmallDecimalChoiceOnlyChangesBelowThresholdPunctuation(bool apply, string expected)
    {
        var store = CreateStore();
        store.SetGlobal(ReadableNumbersSettings.SmallDecimals, apply);
        store.SetGlobal(ReadableNumbersSettings.Grouping, NumberGrouping.None);
        store.SetGlobal(ReadableNumbersSettings.Punctuation, new(PunctuationPreset.PeriodComma, ",", "."));

        Assert.Multiple(() =>
        {
            Assert.That(NumberPresentation.Format(new("9.18", Suffix: " kg"), store), Is.EqualTo(expected));
            Assert.That(NumberPresentation.Format(new("12345.50"), store), Is.EqualTo("12345,50"));
            Assert.That(NumberPresentation.Format(new("12345"), store), Is.EqualTo("12345"));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void InvalidGroupingCannotReplaceAValidPreference(bool saveScope)
    {
        var store = CreateStore();
        store.SetGlobal(ReadableNumbersSettings.Grouping, NumberGrouping.FourDigits);
        var binding = store.ActivateSave(new("A"));
        store.SetUseGlobal(binding, ReadableNumbersSettings.Grouping, false);
        var before = store.Revision;
        var globalPayload = store.ExportGlobals().Values[ReadableNumbersSettings.Grouping.Id];
        var savePayload = store.ExportSave(binding).Values[ReadableNumbersSettings.Grouping.Id];

        var result = saveScope
            ? store.SetSaveOverride(binding, ReadableNumbersSettings.Grouping, (NumberGrouping)99)
            : store.SetGlobal(ReadableNumbersSettings.Grouping, (NumberGrouping)99);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(new SettingEditResult(false, "Megamod_Numbers_GroupingInvalid")));
            Assert.That(store.Revision, Is.EqualTo(before));
            Assert.That(store.ExportGlobals().Values[ReadableNumbersSettings.Grouping.Id], Is.EqualTo(globalPayload));
            Assert.That(store.ExportSave(binding).Values[ReadableNumbersSettings.Grouping.Id], Is.EqualTo(savePayload));
        });
    }

    [TestCase(NumberGrouping.ThreeDigits, "123,456,789.25")]
    [TestCase(NumberGrouping.ThreeThenTwoDigits, "12,34,56,789.25")]
    [TestCase(NumberGrouping.FourDigits, "1,2345,6789.25")]
    [TestCase(NumberGrouping.None, "123456789.25")]
    public void StoredGroupingControlsTheNextPresentationWithoutChangingTheQuantity(NumberGrouping grouping, string expected)
    {
        var store = CreateStore();
        store.SetGlobal(ReadableNumbersSettings.Grouping, grouping);
        var quantity = new RawDecimalQuantity("123456789.25");

        Assert.That(NumberPresentation.Format(quantity, store), Is.EqualTo(expected));
        Assert.That(quantity.OriginalText, Is.EqualTo("123456789.25"));
    }

    private static SettingsStore CreateStore()
    {
        var store = new SettingsStore();
        ReadableNumbersSettings.Register(store);
        store.Register(ReadableNumbersSettings.Punctuation);
        return store;
    }
}
