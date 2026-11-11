using Megamod.Features.ContentSelection.Core;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.ContentSelection;

[TestFixture]
public sealed class ContentFeatureSwitchTests
{
    [Test]
    public void DisabledMasterIgnoresMalformedMembersButRejectsAStaleBoundary()
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var key = new ContentKey("core", "Incident", "Test");
        var catalog = new ContentCatalog(new[] { new ContentCatalogEntry(key, new object()) });
        store.LoadGlobals(new GlobalSettingsSnapshot(new Dictionary<string, string>
        { [ContentSettings.Enabled.Id] = "false", [settings.Individual(key).Id] = "invalid" }));
        var application = new ContentApplication(store);
        var boundary = application.BeginBoundary(catalog, null);
        var requested = settings.CaptureRequested(catalog, null);
        application.Publish(boundary, requested, Array.Empty<ContentKey>());
        Assert.That(application.Applied!.Enabled, Is.False);
        Assert.That(store.ExportGlobals().Values[settings.Individual(key).Id], Is.EqualTo("invalid"));
        application.EndBoundary();
        Assert.Throws<InvalidOperationException>(() => application.Publish(boundary, requested, Array.Empty<ContentKey>()));
    }

    [Test]
    public void SaveMasterInheritanceRetainsDormantChoiceAcrossGlobalChanges()
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var catalog = new ContentCatalog(Array.Empty<ContentCatalogEntry>());
        var binding = store.ActivateSave(new SaveSettingsSnapshot("save"));
        store.SetUseGlobal(binding, ContentSettings.Enabled, false);
        store.SetSaveOverride(binding, ContentSettings.Enabled, false);
        store.SetUseGlobal(binding, ContentSettings.Enabled, true);
        Assert.That(settings.CaptureRequested(catalog, store.ExportSave(binding)).Enabled.Value, Is.True);
        store.SetGlobal(ContentSettings.Enabled, false);
        Assert.That(settings.CaptureRequested(catalog, store.ExportSave(binding)).Enabled.Value, Is.False);
        store.SetGlobal(ContentSettings.Enabled, true);
        store.SetUseGlobal(binding, ContentSettings.Enabled, false);
        Assert.That(settings.CaptureRequested(catalog, store.ExportSave(binding)).Enabled.Value, Is.False);
        Assert.That(store.GetGlobal(ContentSettings.Enabled), Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DisabledBoundaryAllowsContentAndRetainsIndividualAndPresetChoices(bool saveOverride)
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var key = new ContentKey("core", "Incident", "Test");
        var identity = new object();
        var catalog = new ContentCatalog(new[] { new ContentCatalogEntry(key, identity) });
        var allowed = settings.Individual(key);
        var preset = settings.Preset(key);
        store.LoadGlobals(new GlobalSettingsSnapshot(new Dictionary<string, string>
        {
            ["content.enabled"] = saveOverride ? "true" : "false",
            [allowed.Id] = "false", [preset.Id] = "true"
        }));
        var save = saveOverride ? new SaveSettingsSnapshot("save", new Dictionary<string, SaveSettingValue>
        { ["content.enabled"] = new(false, "false") }) : null;
        var application = new ContentApplication(store);
        application.Publish(application.BeginBoundary(catalog, save), settings.CaptureRequested(catalog, save), Array.Empty<ContentKey>());
        Assert.Multiple(() =>
        {
            Assert.That(application.Applied!.IsAllowed(identity), Is.True);
            Assert.That(application.Applied.KeyDecisions[key], Is.True);
            Assert.That(store.GetGlobal(allowed), Is.False);
            Assert.That(store.GetGlobal(preset), Is.True);
            Assert.That(save?.Values["content.enabled"].Override, Is.EqualTo(saveOverride ? "false" : null));
        });
    }

    [Test]
    public void DisabledAndReenabledMasterOnlyTakeEffectAtFreshLoadBoundary()
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var key = new ContentKey("core", "Incident", "Test");
        var identity = new object();
        var catalog = new ContentCatalog(new[] { new ContentCatalogEntry(key, identity) });
        var owner = new object();
        var receipt = new SettingsLifecycleRequest<object>(owner, 1, true, new SaveSettingsSnapshot("save"));
        var lifecycle = new IncidentPolicyLifecycle<object>(settings, new ContentApplication(store), catalog,
            new[] { key }, () => receipt, () => owner);
        store.SetGlobal(settings.Individual(key), false);
        Assert.That(lifecycle.Process(true, true), Is.True);
        Assert.That(lifecycle.ReadApplied()!.IsAllowed(identity), Is.False);
        store.LoadGlobals(new GlobalSettingsSnapshot(new Dictionary<string, string>
        { ["content.enabled"] = "false", [settings.Individual(key).Id] = "false" }));
        Assert.That(lifecycle.Process(true, true), Is.True);
        Assert.That(lifecycle.ReadApplied()!.IsAllowed(identity), Is.False);
        receipt = receipt with { Generation = 2 };
        Assert.That(lifecycle.Process(true, true), Is.True);
        Assert.That(lifecycle.ReadApplied()!.IsAllowed(identity), Is.True);
        store.LoadGlobals(new GlobalSettingsSnapshot(new Dictionary<string, string>
        { ["content.enabled"] = "true", [settings.Individual(key).Id] = "false" }));
        Assert.That(lifecycle.Process(true, true), Is.True);
        Assert.That(lifecycle.ReadApplied()!.IsAllowed(identity), Is.True);
        receipt = receipt with { Generation = 3 };
        Assert.That(lifecycle.Process(true, true), Is.True);
        Assert.That(lifecycle.ReadApplied()!.IsAllowed(identity), Is.False);
    }
}
