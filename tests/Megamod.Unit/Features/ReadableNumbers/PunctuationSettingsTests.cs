using Megamod.Features.ReadableNumbers;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Features.ReadableNumbers;

[TestFixture]
public sealed class PunctuationSettingsTests
{
    [TestCase("\u2800", true)]
    [TestCase("\u2800", false)]
    [TestCase("\u02d6", true)]
    [TestCase("\u02d6", false)]
    [TestCase("\u02d7", true)]
    [TestCase("\u02d7", false)]
    public void InvisibleBrailleAndModifierSignsRejectTheWholePreference(string mark, bool grouping)
    {
        var store = new SettingsStore();
        var setting = ReadableNumbersSettings.Punctuation;
        store.Register(setting);
        var valid = new PunctuationPreference(PunctuationPreset.Custom, "#", "@");
        Assert.That(store.SetGlobal(setting, valid).Succeeded, Is.True);
        var revision = store.Revision;
        string payload = store.ExportGlobals().Values[setting.Id];

        var result = store.SetGlobal(setting, grouping
            ? valid with { CustomGroup = mark }
            : valid with { CustomDecimal = mark });

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorKey, Is.EqualTo("Megamod_Numbers_PunctuationInvalid"));
            Assert.That(store.GetEffective(setting), Is.EqualTo(valid));
            Assert.That(store.Revision, Is.EqualTo(revision));
            Assert.That(store.ExportGlobals().Values[setting.Id], Is.EqualTo(payload));
        });
    }

    [Test]
    public void PresetChangeAndSaveReloadPreserveTheWholeDormantPreference()
    {
        var store = new SettingsStore();
        var setting = ReadableNumbersSettings.Punctuation;
        store.Register(setting);
        var global = new PunctuationPreference(PunctuationPreset.Custom, "\U0001f4a0", "@");
        Assert.That(store.SetGlobal(setting, global).Succeeded, Is.True);
        var binding = store.ActivateSave(new("punctuation-save"));
        Assert.That(store.SetUseGlobal(binding, setting, false).Succeeded, Is.True);
        var local = new PunctuationPreference(PunctuationPreset.Custom, "#", "@");
        Assert.That(store.SetSaveOverride(binding, setting, local).Succeeded, Is.True);
        store.SetUseGlobal(binding, setting, true);
        global = global with { Preset = PunctuationPreset.PeriodComma };
        Assert.That(store.SetGlobal(setting, global).Succeeded, Is.True);

        var restored = new SettingsStore();
        restored.Register(setting);
        restored.LoadGlobals(store.ExportGlobals());
        var restoredBinding = restored.ActivateSave(store.ExportSave(binding));
        Assert.That(restored.GetEffective(setting), Is.EqualTo(global));
        Assert.That(restored.SetUseGlobal(restoredBinding, setting, false).Succeeded, Is.True);
        Assert.That(restored.GetEffective(setting), Is.EqualTo(local));
        Assert.That(restored.GetGlobal(setting).CustomGroup, Is.EqualTo("\U0001f4a0"));
    }

    [TestCase("{\"CustomGroup\":\"#\",\"CustomDecimal\":\"@\"}")]
    [TestCase("{\"Preset\":99,\"CustomGroup\":\"#\",\"CustomDecimal\":\"@\"}")]
    [TestCase("{\"Preset\":6,\"Preset\":0,\"CustomGroup\":\"#\",\"CustomDecimal\":\"@\"}")]
    [TestCase("{\"Preset\":6,\"CustomGroup\":\"\\uD800\",\"CustomDecimal\":\"@\"}")]
    public void MalformedStoredPreferenceFallsBackAsAWholeWithoutRepairingItsPayload(string payload)
    {
        var store = new SettingsStore();
        var setting = ReadableNumbersSettings.Punctuation;
        store.Register(setting);
        store.LoadGlobals(new(new Dictionary<string, string> { [setting.Id] = payload }));
        var result = store.ReadGlobal(setting);
        Assert.Multiple(() =>
        {
            Assert.That(result.Value, Is.EqualTo(PunctuationPreference.BuiltIn));
            Assert.That(result.ErrorKey, Is.EqualTo("Megamod_Settings_InvalidStoredValue"));
            Assert.That(store.ExportGlobals().Values[setting.Id], Is.EqualTo(payload));
        });
    }

    [TestCase("", ".")]
    [TestCase("#", "")]
    [TestCase("##", ".")]
    [TestCase("1", ".")]
    [TestCase("a", ".")]
    [TestCase("+", ".")]
    [TestCase("-", ".")]
    [TestCase("\u2212", ".")]
    [TestCase("<", ".")]
    [TestCase(">", ".")]
    [TestCase("&", ".")]
    [TestCase("\n", ".")]
    [TestCase("\u200b", ".")]
    [TestCase("\u0301", ".")]
    [TestCase("\u00a0", ".")]
    [TestCase("#", " ")]
    [TestCase("\ud800", ".")]
    public void InvalidDormantMarksCannotBeSavedEvenWithAPresetSelected(string group, string mark)
    {
        var store = new SettingsStore();
        var setting = ReadableNumbersSettings.Punctuation;
        store.Register(setting);
        var result = store.SetGlobal(setting, new(PunctuationPreset.CommaPeriod, group, mark));
        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorKey, Is.EqualTo("Megamod_Numbers_PunctuationInvalid"));
            Assert.That(store.GetGlobal(setting), Is.EqualTo(PunctuationPreference.BuiltIn));
        });
    }

    [Test]
    public void RejectedCompletePairKeepsThePreviousPreferenceAndRevision()
    {
        var store = new SettingsStore();
        var setting = ReadableNumbersSettings.Punctuation;
        store.Register(setting);
        var valid = new PunctuationPreference(PunctuationPreset.Custom, "#", "@");
        Assert.That(store.SetGlobal(setting, valid).Succeeded, Is.True);
        var revision = store.Revision;
        string persisted = store.ExportGlobals().Values[setting.Id];

        var result = store.SetGlobal(setting, valid with { CustomDecimal = "#" });

        Assert.Multiple(() =>
        {
            Assert.That(result.Succeeded, Is.False);
            Assert.That(result.ErrorKey, Is.EqualTo("Megamod_Numbers_PunctuationInvalid"));
            Assert.That(store.GetEffective(setting), Is.EqualTo(valid));
            Assert.That(store.Revision, Is.EqualTo(revision));
            Assert.That(store.ExportGlobals().Values[setting.Id], Is.EqualTo(persisted));
        });
    }
}
