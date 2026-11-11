using Megamod.Features.ReadableNumbers;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Settings;

[TestFixture]
public sealed class SettingsStoreTests
{
    [Test]
    public void EffectiveOffFollowsSaveSwitchesWithoutRetainingThePreviousRequestedChoice()
    {
        var definition = ReadableNumbersSettings.Enabled;
        var store = new SettingsStore(new(new Dictionary<SettingDefinition<bool>, string>
        { [definition] = "original-active" }));
        store.Register(definition);
        store.SetGlobal(definition, true);
        var first = store.ActivateSave(new("save-a", new Dictionary<string, SaveSettingValue>
        { [definition.Id] = new(false, "false") }));
        Assert.That(store.ReadCurrentRequested(definition).Value, Is.False);
        first.Revoke();
        Assert.That(store.ReadCurrentRequested(definition).Value, Is.True);
        Assert.That(store.GetEffective(definition), Is.False);
        store.DeactivateSave();
        var second = store.ActivateSave(new("save-b", new Dictionary<string, SaveSettingValue>
        { [definition.Id] = new(false, "true") }));

        Assert.Multiple(() =>
        {
            Assert.That(store.CurrentSaveIdentity, Is.EqualTo("save-b"));
            Assert.That(store.ReadCurrentRequested(definition).Value, Is.True);
            Assert.That(store.GetEffective(definition), Is.False);
            Assert.That(store.SetSaveOverride(first, definition, false).Succeeded, Is.False);
            Assert.That(store.ExportSave(second).Values[definition.Id], Is.EqualTo(new SaveSettingValue(false, "true")));
        });
    }

    [Test]
    public void EffectiveOffRetainsRequestedEditorsOverrideSeedingAndLaterStartupChoices()
    {
        var definition = ReadableNumbersSettings.Enabled;
        var store = new SettingsStore(new(new Dictionary<SettingDefinition<bool>, string>
        { [definition] = "original-active" }));
        store.Register(definition);
        store.SetGlobal(definition, true);
        var binding = store.ActivateSave(new("save-a"));
        Assert.That(store.ReadCurrentRequested(definition).Value, Is.True);
        Assert.That(store.SetUseGlobal(binding, definition, false).Succeeded, Is.True);
        Assert.That(store.ExportSave(binding).Values[definition.Id], Is.EqualTo(new SaveSettingValue(false, "true")));
        Assert.That(store.SetSaveOverride(binding, definition, false).Succeeded, Is.True);
        Assert.That(store.SetUseGlobal(binding, definition, true).Succeeded, Is.True);
        var revision = store.Revision;
        var saved = store.ExportSave(binding);
        var globals = store.ExportGlobals();

        Assert.Multiple(() =>
        {
            Assert.That(store.ReadCurrentRequested(definition).Value, Is.True);
            Assert.That(store.ReadRequested(definition, saved).Value, Is.True);
            Assert.That(store.GetEffective(definition), Is.False);
            Assert.That(saved.Values[definition.Id], Is.EqualTo(new SaveSettingValue(true, "false")));
            Assert.That(store.Revision, Is.EqualTo(revision));
        });

        var laterStartup = new SettingsStore();
        laterStartup.Register(definition);
        laterStartup.LoadGlobals(globals);
        var restored = laterStartup.ActivateSave(saved);
        Assert.That(laterStartup.GetEffective(definition), Is.True);
        Assert.That(laterStartup.SetUseGlobal(restored, definition, false).Succeeded, Is.True);
        Assert.That(laterStartup.GetEffective(definition), Is.False);
        Assert.That(laterStartup.SetSaveOverride(restored, definition, true).Succeeded, Is.True);
        Assert.That(laterStartup.GetEffective(definition), Is.True);
        Assert.That(store.GetEffective(definition), Is.False);
    }

    [Test]
    public void RevokedSaveTokenRejectsEditsAndUsesGlobalsBeforeMainThreadDetachment()
    {
        var store = new SettingsStore();
        var definition = ReadableNumbersSettings.MinimumIntegerDigits;
        store.Register(definition);
        store.SetGlobal(definition, 5);
        var binding = store.ActivateSave(new("revoked"));
        store.SetUseGlobal(binding, definition, false);
        store.SetSaveOverride(binding, definition, 8);
        var payload = store.ExportSave(binding).Values[definition.Id];

        Task.Run(binding.Revoke).GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(store.SetSaveOverride(binding, definition, 9).Succeeded, Is.False);
            Assert.That(store.SetUseGlobal(binding, definition, true).Succeeded, Is.False);
            Assert.That(store.GetEffective(definition), Is.EqualTo(5));
            Assert.That(store.UsesGlobal(definition), Is.True);
            Assert.That(store.CurrentSaveIdentity, Is.Null);
            Assert.That(store.ExportSave(binding).Values[definition.Id], Is.EqualTo(payload));
        });
    }

    [Test]
    public void SuccessfulEditsPublishCommittedValuesAndRejectedEditsDoNotPublish()
    {
        var store = new SettingsStore();
        var definition = ReadableNumbersSettings.MinimumIntegerDigits;
        store.Register(definition);
        int published = store.GetGlobal(definition);
        int notifications = 0;
        store.Changed += () => { published = store.GetGlobal(definition); notifications++; };

        store.SetGlobal(definition, 9);
        Assert.That(published, Is.EqualTo(9));
        store.SetGlobal(definition, 9);
        store.SetGlobal(definition, 3);
        Assert.That(notifications, Is.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MissingExplicitOverrideUsesBuiltInFallbackWithoutBecomingInheritance(bool detached)
    {
        var definition = ReadableNumbersSettings.MinimumIntegerDigits;
        var store = new SettingsStore();
        store.Register(definition);
        store.SetGlobal(definition, 9);
        var malformed = new SaveSettingsSnapshot("save-a", new System.Collections.Generic.Dictionary<string, SaveSettingValue>
        { [definition.Id] = new(false, null) });
        var binding = store.ActivateSave(malformed);

        var result = detached ? store.ReadRequested(definition, malformed) : store.ReadEffective(definition);

        Assert.That(result.Value, Is.EqualTo(4));
        Assert.That(result.ErrorKey, Is.EqualTo("Megamod_Settings_InvalidStoredValue"));
        Assert.That(store.ExportSave(binding).Values[definition.Id], Is.EqualTo(new SaveSettingValue(false, null)));
        Assert.That(store.GetGlobal(definition), Is.EqualTo(9));
    }

    [Test]
    public void RevisionsDistinguishChangedValuesAndSaveLifetimes()
    {
        var store = new SettingsStore();
        var definition = ReadableNumbersSettings.MinimumIntegerDigits;
        store.Register(definition);
        var initial = store.Revision;
        store.SetGlobal(definition, 6);
        var edited = store.Revision;
        Assert.That(edited, Is.Not.EqualTo(initial));
        store.SetGlobal(definition, 6);
        store.SetGlobal(definition, 3);
        Assert.That(store.Revision, Is.EqualTo(edited));
        var binding = store.ActivateSave(new SaveSettingsSnapshot("save-a"));
        var firstLoad = store.Revision;
        store.ActivateSave(store.ExportSave(binding));
        Assert.That(store.Revision, Is.Not.EqualTo(firstLoad));
    }

    [Test]
    public void LoadingPolicyCanReadADetachedSaveWithoutActivatingIt()
    {
        var definition = ReadableNumbersSettings.MinimumIntegerDigits;
        var store = new SettingsStore();
        store.Register(definition);
        store.SetGlobal(definition, 6);
        var active = store.ActivateSave(new SaveSettingsSnapshot("save-a"));
        store.SetUseGlobal(active, definition, false);
        store.SetSaveOverride(active, definition, 8);
        var loading = new SaveSettingsSnapshot("save-b", new System.Collections.Generic.Dictionary<string, SaveSettingValue>
        { [definition.Id] = new(false, "10") });

        Assert.That(store.ReadRequested(definition, loading).Value, Is.EqualTo(10));
        Assert.That(store.GetEffective(definition), Is.EqualTo(8));
        Assert.That(store.CurrentSaveIdentity, Is.EqualTo("save-a"));
    }

    [Test]
    public void MissingContentProviderDoesNotEraseItsDormantChoices()
    {
        const string id = "content.allowed/7:example9:ThingDef5:Thing";
        var store = new SettingsStore();
        store.LoadGlobals(new GlobalSettingsSnapshot(new System.Collections.Generic.Dictionary<string, string>
        { [id] = "false" }));
        var binding = store.ActivateSave(new SaveSettingsSnapshot("save-a",
            new System.Collections.Generic.Dictionary<string, SaveSettingValue> { [id] = new(true, "true") }));

        Assert.That(store.ExportGlobals().Values[id], Is.EqualTo("false"));
        Assert.That(store.ExportSave(binding).Values[id], Is.EqualTo(new SaveSettingValue(true, "true")));
        var returnedDefinition = new SettingDefinition<bool>(id, true, SettingScope.GlobalAndSave, "content", 0,
            SettingApplicationTiming.NewGameOrLoad, SettingCodecs.Boolean);
        store.Register(returnedDefinition);
        Assert.That(store.GetEffective(returnedDefinition), Is.False);
        store.SetUseGlobal(binding, returnedDefinition, false);
        Assert.That(store.GetEffective(returnedDefinition), Is.True);
    }

    [TestCase("3", "Megamod_Numbers_MinimumIntegerDigitsInvalid")]
    [TestCase("not-a-number", "Megamod_Settings_InvalidStoredValue")]
    public void InvalidStoredThresholdUsesItsBuiltInValueWithAnExplanation(string encoded, string reason)
    {
        var store = new SettingsStore();
        store.Register(ReadableNumbersSettings.MinimumIntegerDigits);
        store.LoadGlobals(new GlobalSettingsSnapshot(new System.Collections.Generic.Dictionary<string, string>
        {
            [ReadableNumbersSettings.MinimumIntegerDigits.Id] = encoded
        }));

        var result = store.ReadGlobal(ReadableNumbersSettings.MinimumIntegerDigits);

        Assert.That(result.Value, Is.EqualTo(4));
        Assert.That(result.ErrorKey, Is.EqualTo(reason));
    }

    [Test]
    public void InvalidThresholdEditRetainsTheLastValidPreference()
    {
        var store = new SettingsStore();
        store.Register(ReadableNumbersSettings.MinimumIntegerDigits);
        store.SetGlobal(ReadableNumbersSettings.MinimumIntegerDigits, 6);

        var result = store.SetGlobal(ReadableNumbersSettings.MinimumIntegerDigits, 3);

        Assert.That(result.Succeeded, Is.False);
        Assert.That(result.ErrorKey, Is.EqualTo("Megamod_Numbers_MinimumIntegerDigitsInvalid"));
        Assert.That(store.GetEffective(ReadableNumbersSettings.MinimumIntegerDigits), Is.EqualTo(6));
    }

    [Test]
    public void ACallbackCannotWriteAfterLeavingOrReloadingItsSave()
    {
        var store = new SettingsStore();
        store.Register(ReadableNumbersSettings.Enabled);
        var old = store.ActivateSave(new SaveSettingsSnapshot("save-a"));
        store.SetUseGlobal(old, ReadableNumbersSettings.Enabled, false);
        store.DeactivateSave();

        Assert.That(store.SetUseGlobal(old, ReadableNumbersSettings.Enabled, false).Succeeded, Is.False);
        Assert.That(store.SetSaveOverride(old, ReadableNumbersSettings.Enabled, false).Succeeded, Is.False);
        store.ActivateSave(new SaveSettingsSnapshot("save-a"));
        Assert.That(store.SetUseGlobal(old, ReadableNumbersSettings.Enabled, false).Succeeded, Is.False);
        Assert.That(store.SetSaveOverride(old, ReadableNumbersSettings.Enabled, false).Succeeded, Is.False);
        Assert.That(store.GetEffective(ReadableNumbersSettings.Enabled), Is.True);
    }

    [Test]
    public void DormantOverrideSurvivesInheritedSaveReload()
    {
        var store = new SettingsStore();
        store.Register(ReadableNumbersSettings.Enabled);
        var original = store.ActivateSave(new SaveSettingsSnapshot("save-a"));
        store.SetUseGlobal(original, ReadableNumbersSettings.Enabled, false);
        store.SetSaveOverride(original, ReadableNumbersSettings.Enabled, false);
        store.SetUseGlobal(original, ReadableNumbersSettings.Enabled, true);

        var saved = store.ExportSave(original);
        store.DeactivateSave();
        var restored = store.ActivateSave(saved);
        Assert.That(store.GetEffective(ReadableNumbersSettings.Enabled), Is.True);
        store.SetUseGlobal(restored, ReadableNumbersSettings.Enabled, false);

        Assert.That(store.GetEffective(ReadableNumbersSettings.Enabled), Is.False);
    }

    [Test]
    public void ACurrentSaveOverrideDoesNotChangeTheGlobalDefault()
    {
        var store = new SettingsStore();
        store.Register(ReadableNumbersSettings.Enabled);
        var binding = store.ActivateSave(new SaveSettingsSnapshot("save-a"));
        store.SetUseGlobal(binding, ReadableNumbersSettings.Enabled, false);
        store.SetSaveOverride(binding, ReadableNumbersSettings.Enabled, false);

        Assert.That(store.GetEffective(ReadableNumbersSettings.Enabled), Is.False);
        store.DeactivateSave();
        Assert.That(store.GetEffective(ReadableNumbersSettings.Enabled), Is.True);
    }

    [Test]
    public void GlobalSnapshotRestoresEditedPreferencesBeforeAnyGameExists()
    {
        var original = new SettingsStore();
        original.Register(ReadableNumbersSettings.Enabled);
        original.SetGlobal(ReadableNumbersSettings.Enabled, false);

        var restored = new SettingsStore();
        restored.Register(ReadableNumbersSettings.Enabled);
        restored.LoadGlobals(original.ExportGlobals());

        Assert.That(restored.GetEffective(ReadableNumbersSettings.Enabled), Is.False);
        Assert.That(restored.CurrentSaveIdentity, Is.Null);
    }

    [Test]
    public void MainMenuReadsTheEditedGlobalWithoutCreatingASave()
    {
        var store = new SettingsStore();
        store.Register(ReadableNumbersSettings.Enabled);

        store.SetGlobal(ReadableNumbersSettings.Enabled, false);

        Assert.That(store.GetEffective(ReadableNumbersSettings.Enabled), Is.False);
        Assert.That(store.CurrentSaveIdentity, Is.Null);
    }
}
