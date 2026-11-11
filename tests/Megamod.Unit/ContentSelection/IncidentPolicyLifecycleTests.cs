using Megamod.Features.ContentSelection.Core;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.ContentSelection;

[TestFixture]
public sealed class IncidentPolicyLifecycleTests
{
    [Test]
    public void ReceiptChangedDuringPreparationNeverPublishesEvenInsideApplication()
    {
        var store = new SettingsStore();
        var owner = new object();
        var receipt = new SettingsLifecycleRequest<object>(owner, 1, true, new SaveSettingsSnapshot("save"));
        var application = new ContentApplication(store);
        var lifecycle = new IncidentPolicyLifecycle<object>(new ContentSettings(store), application,
            new ContentCatalog(Array.Empty<ContentCatalogEntry>()), Array.Empty<ContentKey>(), () => receipt, () =>
            {
                receipt = receipt with { Generation = receipt.Generation + 1 };
                return owner;
            });
        Assert.That(lifecycle.Process(true, true), Is.False);
        Assert.That(application.Applied, Is.Null);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void StoredUnimplementedPresetPreventsPublicationAndCannotBeRetriedInTheSameBoundary(bool saved)
    {
        var preset = new ContentKey("provider", "CherryPicker.DefList", "Preset");
        string id = "content.preset-active/" + preset.ToStorageToken();
        var store = new SettingsStore();
        if (!saved) store.LoadGlobals(new GlobalSettingsSnapshot(new Dictionary<string, string> { [id] = "true" }));
        var save = new SaveSettingsSnapshot("save", saved ? new Dictionary<string, SaveSettingValue> { [id] = new(false, "true") } : null);
        var settings = new ContentSettings(store);
        var owner = new object();
        var receipt = new SettingsLifecycleRequest<object>(owner, 1, true, save);
        var lifecycle = new IncidentPolicyLifecycle<object>(settings, new ContentApplication(store),
            new ContentCatalog(Array.Empty<ContentCatalogEntry>()), Array.Empty<ContentKey>(), () => receipt, () => owner);
        Assert.Throws<InvalidOperationException>(() => lifecycle.Process(true, true));
        Assert.That(lifecycle.ReadApplied(), Is.Null);
        store.SetGlobal(settings.Preset(preset), false);
        Assert.That(lifecycle.Process(true, true), Is.False, "A rejected load boundary cannot apply later UI edits implicitly.");
    }

    [Test]
    public void ReceiptChangeDuringNativeOwnerReadCannotExposeThePreviousPolicy()
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var owner = new object();
        var receipt = new SettingsLifecycleRequest<object>(owner, 1, true, new SaveSettingsSnapshot("save"));
        bool switchDuringRead = false;
        var lifecycle = new IncidentPolicyLifecycle<object>(settings, new ContentApplication(store),
            new ContentCatalog(Array.Empty<ContentCatalogEntry>()), Array.Empty<ContentKey>(), () => receipt, () =>
            {
                if (switchDuringRead) { receipt = receipt with { Generation = 2 }; switchDuringRead = false; }
                return owner;
            });
        Assert.That(lifecycle.Process(true, true), Is.True);
        switchDuringRead = true;
        Assert.That(lifecycle.ReadApplied(), Is.Null);
    }

    [TestCase("native")]
    [TestCase("patches")]
    [TestCase("unavailable")]
    [TestCase("owner")]
    [TestCase("save")]
    [TestCase("null-owner")]
    public void IncompleteBoundaryCannotPublishAndDoesNotConsumeTheLoadingChoice(string missing)
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var key = new ContentKey("ludeon.rimworld", "RimWorld.IncidentDef", "ShipChunkDrop");
        var identity = new object();
        var catalog = new ContentCatalog(new[] { new ContentCatalogEntry(key, identity) });
        var owner = new object();
        object? actual = missing == "owner" ? new object() : missing == "null-owner" ? null : owner;
        var save = new SaveSettingsSnapshot("save");
        var receipt = new SettingsLifecycleRequest<object>(owner, 1, missing != "unavailable", missing == "save" ? null : save);
        var lifecycle = new IncidentPolicyLifecycle<object>(settings, new ContentApplication(store), catalog,
            new[] { key }, () => receipt, () => actual);
        Assert.That(lifecycle.Process(missing != "native", missing != "patches"), Is.False);
        Assert.That(lifecycle.ReadApplied(), Is.Null);
        store.SetGlobal(settings.Individual(key), false);
        actual = owner;
        receipt = new(owner, 1, true, save);
        Assert.That(lifecycle.Process(true, true), Is.True);
        Assert.That(lifecycle.ReadApplied()!.IsAllowed(identity), Is.False);
    }

    [Test]
    public void RevalidatedPatchesRestoreSamePolicyWithoutApplyingPendingEdits()
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var key = new ContentKey("ludeon.rimworld", "RimWorld.IncidentDef", "ShipChunkDrop");
        var identity = new object();
        var catalog = new ContentCatalog(new[] { new ContentCatalogEntry(key, identity) });
        var owner = new object();
        var receipt = new SettingsLifecycleRequest<object>(owner, 1, true, new SaveSettingsSnapshot("save"));
        var lifecycle = new IncidentPolicyLifecycle<object>(settings, new ContentApplication(store), catalog,
            new[] { key }, () => receipt, () => owner);
        store.SetGlobal(settings.Individual(key), false);
        Assert.That(lifecycle.Process(true, true), Is.True);
        var applied = lifecycle.ReadApplied();
        store.SetGlobal(settings.Individual(key), true);
        Assert.That(lifecycle.Process(true, false), Is.False);
        Assert.That(lifecycle.ReadApplied(), Is.Null);
        Assert.That(lifecycle.Process(true, true), Is.True);
        Assert.That(lifecycle.ReadApplied(), Is.SameAs(applied));
    }

    [Test]
    public void AppliesOncePerExactGenerationAndLeavesSubsequentEditsPending()
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var key = new ContentKey("ludeon.rimworld", "RimWorld.IncidentDef", "ShipChunkDrop");
        var identity = new object();
        var catalog = new ContentCatalog(new[] { new ContentCatalogEntry(key, identity) });
        var owner = new object();
        var save = new SaveSettingsSnapshot("save");
        var receipt = new SettingsLifecycleRequest<object>(owner, 1, true, save);
        var lifecycle = new IncidentPolicyLifecycle<object>(settings, new ContentApplication(store), catalog,
            new[] { key }, () => receipt, () => owner);
        store.SetGlobal(settings.Individual(key), false);
        Assert.That(lifecycle.Process(true, true), Is.True);
        var first = lifecycle.ReadApplied();
        Assert.That(first!.IsAllowed(identity), Is.False);
        store.SetGlobal(settings.Individual(key), true);
        Assert.That(lifecycle.Process(true, true), Is.True);
        Assert.That(lifecycle.ReadApplied(), Is.SameAs(first));
        receipt = receipt with { Generation = 2 };
        Assert.That(lifecycle.ReadApplied(), Is.Null, "A worker receipt change revokes old availability before another main-thread pump.");
        Assert.That(lifecycle.Process(true, true), Is.True);
        Assert.That(lifecycle.ReadApplied()!.IsAllowed(identity), Is.True);
        Assert.That(first.IsAllowed(identity), Is.False, "The earlier published value remains immutable.");
    }
}
