using Megamod.Features.ContentSelection.Core;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.ContentSelection;

[TestFixture]
public sealed class ContentSettingsTests
{
    [Test]
    public async Task PreparedLoadInputsResolveOnWorkerFromPinnedGlobalsAndDetachedSave()
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var key = new ContentKey("ludeon.rimworld", "RimWorld.IncidentDef", "ShipChunkDrop");
        var presetKey = new ContentKey("author.provider", "Example.PresetDef", "Events");
        var individual = settings.Individual(key);
        var preset = settings.Preset(presetKey);
        store.SetGlobal(individual, false);
        var globals = new GlobalSettingsReadSnapshot(store.ExportGlobals(), store.Revision.Global);
        var catalog = new ContentCatalog(new[] { new ContentCatalogEntry(key, new object()) });
        var inputs = settings.PrepareLoadInputs(catalog, globals, new[] { presetKey });
        var save = new SaveSettingsSnapshot("loading", new Dictionary<string, SaveSettingValue>
        {
            [preset.Id] = new(false, "true")
        });
        store.SetGlobal(individual, true);
        store.ActivateSave(new SaveSettingsSnapshot("unrelated-active-save"));

        var read = await Task.Run(() => (inputs.ReadIndividual(key, save), inputs.ReadPreset(presetKey, save)));
        Assert.Multiple(() =>
        {
            Assert.That(read.Item1.Value, Is.False);
            Assert.That(read.Item2.Value, Is.True);
            Assert.That(inputs.Globals, Is.SameAs(globals));
            Assert.That(inputs.Catalog, Is.SameAs(catalog));
            Assert.That(store.GetGlobal(individual), Is.True);
            Assert.That(store.CurrentSaveIdentity, Is.EqualTo("unrelated-active-save"));
        });
    }

    [Test]
    public void NewSetupUsesGlobalsAndMissingCatalogEntriesKeepTheirStoredChoices()
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var key = new ContentKey("author.provider", "RimWorld.IncidentDef", "AnimalDisease");
        var definition = settings.Individual(key);
        store.SetGlobal(definition, false);
        store.ActivateSave(new SaveSettingsSnapshot("previous-save", new Dictionary<string, SaveSettingValue>
        {
            [definition.Id] = new(false, "true")
        }));
        var absent = settings.CaptureRequested(new ContentCatalog(Array.Empty<ContentCatalogEntry>()), null);
        var present = new ContentCatalog(new[] { new ContentCatalogEntry(key, new object()) });
        var captured = settings.CaptureRequested(present, null);

        Assert.Multiple(() =>
        {
            Assert.That(absent.Individual, Is.Empty);
            Assert.That(captured.Save, Is.Null);
            Assert.That(captured.Individual[key].Value, Is.False);
            Assert.That(store.ExportGlobals().Values[definition.Id], Is.EqualTo("false"));
            Assert.That(store.ReadEffective(definition).Value, Is.True);
            Assert.Throws<NotSupportedException>(() =>
                ((IDictionary<ContentKey, SettingReadResult<bool>>)captured.Individual)[key] = new(true));
        });
    }

    [Test]
    public void InvalidSavedChoiceRetainsCommonValidationErrorWithoutRewritingStoredValue()
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var key = new ContentKey("author.provider", "RimWorld.IncidentDef", "AnimalDisease");
        var definition = settings.Individual(key);
        var loading = new SaveSettingsSnapshot("loading-save", new Dictionary<string, SaveSettingValue>
        {
            [definition.Id] = new(false, "malformed")
        });
        var catalog = new ContentCatalog(new[] { new ContentCatalogEntry(key, new object()) });
        var captured = settings.CaptureRequested(catalog, loading);

        Assert.Multiple(() =>
        {
            Assert.That(captured.Individual[key].Value, Is.True);
            Assert.That(captured.Individual[key].ErrorKey, Is.EqualTo("Megamod_Settings_InvalidStoredValue"));
            Assert.That(loading.Values[definition.Id].Override, Is.EqualTo("malformed"));
        });
    }

    [Test]
    public void CapturedPresetChoicesUseLoadingSaveWithoutChangingDirectChoices()
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var key = new ContentKey("author.provider", "Example.PresetDef", "MyPreset");
        var preset = settings.Preset(key);
        var loading = new SaveSettingsSnapshot("loading-save", new Dictionary<string, SaveSettingValue>
        {
            [preset.Id] = new(false, "true")
        });
        var catalog = new ContentCatalog(new[] { new ContentCatalogEntry(key, new object()) });
        var keys = new List<ContentKey> { key, key };

        var captured = settings.CaptureRequested(catalog, loading, keys);
        keys.Clear();

        Assert.Multiple(() =>
        {
            Assert.That(captured.Presets, Has.Count.EqualTo(1));
            Assert.That(captured.Presets.TryGetValue(key, out var value) && value.Value, Is.True);
            Assert.That(captured.Individual[key].Value, Is.True);
            Assert.That(store.ReadGlobal(preset).Value, Is.False);
        });
    }

    [Test]
    public void PresetActivationDefaultsOffAndDoesNotOverwriteTheIndividualChoice()
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var key = new ContentKey("author.provider", "Example.PresetDef", "MyPreset");
        var individual = settings.Individual(key);
        var preset = settings.Preset(key);
        Assert.That(store.ReadGlobal(preset).Value, Is.False);
        store.SetGlobal(individual, false);
        store.SetGlobal(preset, true);
        store.SetGlobal(settings.Preset(key), false);

        Assert.Multiple(() =>
        {
            Assert.That(settings.Preset(key), Is.SameAs(preset));
            Assert.That(store.ReadGlobal(individual).Value, Is.False);
            Assert.That(store.ReadGlobal(preset).Value, Is.False);
            Assert.That(preset.Id, Is.EqualTo("content.preset-active/" + key.ToStorageToken()));
            Assert.That(preset.Scope, Is.EqualTo(SettingScope.GlobalAndSave));
            Assert.That(preset.ApplicationTiming, Is.EqualTo(SettingApplicationTiming.NewGameOrLoad));
        });
    }

    [Test]
    public void LoadingSaveUsesItsOwnChoicesAndCapturedRequestsDoNotFollowLaterEdits()
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var key = new ContentKey("author.provider", "Verse.ThingDef", "Medicine");
        var definition = settings.Individual(key);
        store.SetGlobal(definition, false);
        store.ActivateSave(new SaveSettingsSnapshot("previous-save", new Dictionary<string, SaveSettingValue>
        {
            [definition.Id] = new(false, "true")
        }));
        var loading = new SaveSettingsSnapshot("loading-save");
        var catalog = new ContentCatalog(new[] { new ContentCatalogEntry(key, new object()) });
        var revision = store.Revision;

        var captured = settings.CaptureRequested(catalog, loading);
        store.SetGlobal(definition, true);

        Assert.Multiple(() =>
        {
            Assert.That(captured.Individual[key].Value, Is.False);
            Assert.That(captured.Catalog, Is.SameAs(catalog));
            Assert.That(captured.Save, Is.SameAs(loading));
            Assert.That(captured.Revision, Is.EqualTo(revision));
            Assert.That(store.CurrentSaveIdentity, Is.EqualTo("previous-save"));
            Assert.That(store.ReadEffective(definition).Value, Is.True);
        });
    }

    [Test]
    public void IndividualChoiceUsesOneRegisteredCommonDefinitionAcrossCatalogRefreshes()
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var key = new ContentKey("author.provider", "Verse.ThingDef", "Medicine");
        var definition = settings.Individual(key);

        Assert.That(store.ReadGlobal(definition).Value, Is.True);
        Assert.That(store.SetGlobal(definition, false).Succeeded, Is.True);
        var refreshed = settings.Individual(new ContentKey("AUTHOR.PROVIDER", "Verse.ThingDef", "Medicine"));

        Assert.Multiple(() =>
        {
            Assert.That(refreshed, Is.SameAs(definition));
            Assert.That(store.ReadRequested(refreshed, new SaveSettingsSnapshot("loading-save")).Value, Is.False);
            Assert.That(definition.Scope, Is.EqualTo(SettingScope.GlobalAndSave));
            Assert.That(definition.ApplicationTiming, Is.EqualTo(SettingApplicationTiming.NewGameOrLoad));
            Assert.That(definition.Id, Is.EqualTo("content.allowed/" + key.ToStorageToken()));
        });
    }
}
