using Megamod.Features.ContentSelection.Core;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.ContentSelection;

[TestFixture]
public sealed class ContentChoiceRowTests
{
    [Test]
    public void LoadedGameWithoutSaveBindingShowsUnavailableWhileGlobalChoicesRemainEditable()
    {
        var store = new SettingsStore();
        var row = new ContentChoiceRow(new ContentSettings(store), new("source", "Incident", "test"), new object(),
            "Test", "Incident", "Source");
        // Native game presence is independent of whether the common save lifecycle is available.
        // This host test supplies that observed fact; it does not construct or fake a Verse.Game.
        var before = row.ReadState(store, null, hasActiveSave: true);
        var edit = store.SetGlobal(row.Definition, false);
        var after = row.ReadState(store, null, hasActiveSave: true);
        Assert.Multiple(() =>
        {
            Assert.That(store.CurrentSaveIdentity, Is.Null);
            Assert.That(before.Status, Is.EqualTo(ContentChoiceStatus.Unavailable));
            Assert.That(before.UnavailableReasonKey, Is.EqualTo("Megamod_Content_ApplicationUnavailable"));
            Assert.That(before.RequestedAllowed, Is.True);
            Assert.That(edit.Succeeded, Is.True);
            Assert.That(after.RequestedAllowed, Is.False);
            Assert.That(after.Status, Is.EqualTo(ContentChoiceStatus.Unavailable));
            Assert.That(after.AppliedAllowed, Is.Null);
            Assert.That(row.ReadState(store, null, hasActiveSave: false).Status, Is.EqualTo(ContentChoiceStatus.NextLoad),
                "Only a genuinely inactive game gets the main-menu next-load status.");
        });
    }

    [Test]
    public void MalformedRequestedChoiceReportsItsValidationErrorWithoutClaimingAPendingApplication()
    {
        var store = new SettingsStore();
        var row = new ContentChoiceRow(new ContentSettings(store), new("source", "Incident", "test"), new object(),
            "Test", "Incident", "Source");
        store.LoadGlobals(new GlobalSettingsSnapshot(new Dictionary<string, string> { [row.Definition.Id] = "broken" }));
        var before = store.Revision;
        var shown = row.ReadState(store, null, false);
        Assert.Multiple(() =>
        {
            Assert.That(shown.Status, Is.EqualTo(ContentChoiceStatus.InvalidRequested));
            Assert.That(shown.ValueErrorKey, Is.EqualTo("Megamod_Settings_InvalidStoredValue"));
            Assert.That(shown.AppliedAllowed, Is.Null);
            Assert.That(shown.UnavailableReasonKey, Is.Null, "A validation error does not disable the feature editor needed to repair it.");
            Assert.That(store.ExportGlobals().Values[row.Definition.Id], Is.EqualTo("broken"));
            Assert.That(store.Revision, Is.EqualTo(before));
        });
    }

    [Test]
    public void MainMenuPresentationCannotReadAnEarlierActiveSaveOverride()
    {
        var store = new SettingsStore();
        var row = new ContentChoiceRow(new ContentSettings(store), new("source", "Incident", "test"), new object(),
            "Test", "Incident", "Source");
        var binding = store.ActivateSave(new SaveSettingsSnapshot("earlier-save"));
        store.SetUseGlobal(binding, row.Definition, false);
        store.SetSaveOverride(binding, row.Definition, false);
        var before = store.Revision;
        var shown = row.ReadState(store, null, false);
        Assert.Multiple(() =>
        {
            Assert.That(shown.RequestedAllowed, Is.True, "Main-menu presentation uses the global value, never a previous-save target.");
            Assert.That(shown.AppliedAllowed, Is.Null);
            Assert.That(shown.Status, Is.EqualTo(ContentChoiceStatus.NextLoad));
            Assert.That(store.Revision, Is.EqualTo(before), "Presentation does not own lifecycle activation or detachment.");
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void UnavailableFeatureReasonWinsOverOldAppliedOrNextLoadState(bool hasSave)
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var key = new ContentKey("source", "Incident", "test");
        var identity = new object();
        var row = new ContentChoiceRow(settings, key, identity, "Test", "Incident", "Source");
        var catalog = new ContentCatalog(new[] { new ContentCatalogEntry(key, identity) });
        var application = new ContentApplication(store);
        application.Publish(application.BeginBoundary(catalog, null), settings.CaptureRequested(catalog, null), new[] { key });
        var shown = row.ReadState(store, application.Applied, hasSave, "Megamod_Content_Unsupported");
        Assert.Multiple(() =>
        {
            Assert.That(shown.Status, Is.EqualTo(ContentChoiceStatus.Unavailable));
            Assert.That(shown.AppliedAllowed, Is.Null);
            Assert.That(shown.UnavailableReasonKey, Is.EqualTo("Megamod_Content_Unsupported"));
            Assert.That(store.GetGlobal(row.Definition), Is.True);
        });
    }

    [TestCase(false, ContentChoiceStatus.NextLoad)]
    [TestCase(true, ContentChoiceStatus.Unavailable)]
    public void MissingAppliedPolicyIsNeverPresentedAsARequestedValue(bool hasSave, ContentChoiceStatus expected)
    {
        var store = new SettingsStore();
        var row = new ContentChoiceRow(new ContentSettings(store), new("source", "Incident", "test"), new object(),
            "Test", "Incident", "Source");
        store.SetGlobal(row.Definition, false);
        if (hasSave) store.ActivateSave(new SaveSettingsSnapshot("colony"));
        var shown = row.ReadState(store, null, hasSave);
        Assert.Multiple(() =>
        {
            Assert.That(shown.RequestedAllowed, Is.False);
            Assert.That(shown.AppliedAllowed, Is.Null);
            Assert.That(shown.Status, Is.EqualTo(expected));
            Assert.That(store.CurrentSaveIdentity, Is.EqualTo(hasSave ? "colony" : null));
        });
    }

    [Test]
    public void SharedSettingEditShowsPendingWithoutChangingTheAppliedPolicy()
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var key = new ContentKey("ludeon.rimworld", "RimWorld.IncidentDef", "ShipChunkDrop");
        var identity = new object();
        var row = new ContentChoiceRow(settings, key, identity, "Ship chunk", "Incident", "Core");
        var catalog = new ContentCatalog(new[] { new ContentCatalogEntry(key, identity) });
        var save = new SaveSettingsSnapshot("colony");
        store.ActivateSave(save);
        store.SetGlobal(row.Definition, false);
        var application = new ContentApplication(store);
        application.Publish(application.BeginBoundary(catalog, save), settings.CaptureRequested(catalog, save), new[] { key });
        store.SetGlobal(row.Definition, true);

        var shown = row.ReadState(store, application.Applied, true);

        Assert.Multiple(() =>
        {
            Assert.That(row.Definition, Is.SameAs(settings.Individual(key)));
            Assert.That(shown.RequestedAllowed, Is.True);
            Assert.That(shown.AppliedAllowed, Is.False);
            Assert.That(shown.Status, Is.EqualTo(ContentChoiceStatus.PendingAllow));
            Assert.That(application.Applied!.IsAllowed(identity), Is.False);
        });
    }
}
