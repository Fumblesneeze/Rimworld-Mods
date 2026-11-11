using Megamod.Features.ContentSelection.Core;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.ContentSelection;

[TestFixture]
public sealed class ContentProviderPolicyTests
{
    [Test]
    public void ActiveOriginalUsesOrdinaryOffWhileRetainingTheRequestedExclusion()
    {
        var provider = ContentProviderPolicy.FromActivePackages(new[] { "OWLchemist.CherryPicker", "owlchemist.cherrypicker" });
        var store = new SettingsStore(provider.EffectiveOff);
        var settings = new ContentSettings(store);
        var key = new ContentKey("core", "Incident", "Test");
        var identity = new object();
        var catalog = new ContentCatalog(new[] { new ContentCatalogEntry(key, identity) });
        store.SetGlobal(ContentSettings.Enabled, true);
        store.SetGlobal(settings.Individual(key), false);
        var application = new ContentApplication(store);
        application.Publish(application.BeginBoundary(catalog, null), settings.CaptureRequested(catalog, null), new[] { key });
        Assert.Multiple(() =>
        {
            Assert.That(application.Applied!.IsAllowed(identity), Is.True);
            Assert.That(store.GetGlobal(ContentSettings.Enabled), Is.True);
            Assert.That(store.GetGlobal(settings.Individual(key)), Is.False);
            Assert.That(provider.StartupWarnings, Has.Count.EqualTo(1));
            Assert.That(provider.StartupWarnings[0], Does.Contain("best-effort").And.Contain("Owlchemist.CherryPicker"));
        });
        var restored = new SettingsStore(ContentProviderPolicy.FromActivePackages(Array.Empty<string>()).EffectiveOff);
        var restoredSettings = new ContentSettings(restored);
        restored.LoadGlobals(store.ExportGlobals());
        var next = new ContentApplication(restored);
        next.Publish(next.BeginBoundary(catalog, null), restoredSettings.CaptureRequested(catalog, null), new[] { key });
        Assert.That(next.Applied!.IsAllowed(identity), Is.False);
    }

    [Test]
    public void SimilarNamesDoNotDisableTheFeature() => Assert.That(
        ContentProviderPolicy.FromActivePackages(new[] { "owlchemist.cherrypicker.extra", "unrelated" }).StartupWarnings, Is.Empty);
}
