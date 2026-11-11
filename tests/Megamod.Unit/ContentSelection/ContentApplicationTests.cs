using Megamod.Features.ContentSelection.Core;
using Megamod.Features.DrugPolicyMaintenance.Core;
using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.ContentSelection;

[TestFixture]
public sealed class ContentApplicationTests
{
    [Test]
    public void AppliedKeyDecisionsStayImmutableAfterPendingEditsAndBoundaryInvalidation()
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var key = new ContentKey("source", "Incident", "first");
        var other = new ContentKey("source", "Incident", "second");
        var catalog = new ContentCatalog(new[] { new ContentCatalogEntry(key, new object()), new ContentCatalogEntry(other, new object()) });
        store.SetGlobal(settings.Individual(key), false);
        var application = new ContentApplication(store);
        application.Publish(application.BeginBoundary(catalog, null), settings.CaptureRequested(catalog, null), new[] { key, other });
        var previous = application.Applied!;
        store.SetGlobal(settings.Individual(key), true);
        application.EndBoundary();
        Assert.Multiple(() =>
        {
            Assert.That(previous.KeyDecisions[key], Is.False);
            Assert.That(previous.KeyDecisions[other], Is.True);
            Assert.Throws<NotSupportedException>(() => ((IDictionary<ContentKey, bool>)previous.KeyDecisions)[key] = true);
            Assert.That(store.GetGlobal(settings.Individual(key)), Is.True);
            Assert.That(application.Applied, Is.Null);
        });
    }

    [Test]
    public void FailedRestorationOfOldBoundaryDoesNotInvalidateANewerBoundary()
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var application = new ContentApplication(store);
        var key = new ContentKey("ludeon.rimworld", "RimWorld.IncidentDef", "ShipChunkDrop");
        var identity = new object();
        var catalog = new ContentCatalog(new[] { new ContentCatalogEntry(key, identity) });
        var oldBoundary = application.BeginBoundary(catalog, null);
        var oldRequest = settings.CaptureRequested(catalog, null);
        application.Publish(oldBoundary, oldRequest, new[] { key });
        var newSave = new SaveSettingsSnapshot("new-save", new Dictionary<string, SaveSettingValue>
        {
            [settings.Individual(key).Id] = new(false, "false")
        });
        AppliedContentPolicy? newerApplied = null;
        var brokenOldStorage = new Storage(() => throw new InvalidOperationException("old write failed"), () =>
        {
            var newerBoundary = application.BeginBoundary(catalog, newSave);
            application.Publish(newerBoundary, settings.CaptureRequested(catalog, newSave), new[] { key });
            newerApplied = application.Applied;
            throw new InvalidOperationException("old restore failed");
        });

        Assert.Throws<DrugPolicyRollbackException>(() => application.Publish(oldBoundary, oldRequest,
            new[] { key }, new[] { brokenOldStorage }, Array.Empty<DrugCatalogEntry>()));
        Assert.That(newerApplied, Is.Not.Null);
        Assert.That(application.Applied, Is.SameAs(newerApplied));
        Assert.That(application.Applied!.IsAllowed(identity), Is.False);
        Assert.Throws<InvalidOperationException>(() => application.Publish(oldBoundary, oldRequest, new[] { key }));
    }

    [Test]
    public void FailedRestorationInvalidatesPublishedPolicyAndRejectsRetryUntilFreshBoundary()
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var application = new ContentApplication(store);
        var key = new ContentKey("ludeon.rimworld", "RimWorld.IncidentDef", "ShipChunkDrop");
        var identity = new object();
        var catalog = new ContentCatalog(new[] { new ContentCatalogEntry(key, identity) });
        var boundary = application.BeginBoundary(catalog, null);
        store.SetGlobal(settings.Individual(key), false);
        application.Publish(boundary, settings.CaptureRequested(catalog, null), new[] { key });
        var prior = application.Applied;
        Assert.That(prior!.IsAllowed(identity), Is.False);

        store.SetGlobal(settings.Individual(key), true);
        var requested = settings.CaptureRequested(catalog, null);
        var writeFailure = new InvalidOperationException("native write failed after mutation");
        var restoreFailure = new InvalidOperationException("native restoration failed");
        var brokenStorage = new Storage(() => throw writeFailure, () => throw restoreFailure);
        var exception = Assert.Throws<DrugPolicyRollbackException>(() => application.Publish(boundary, requested,
            new[] { key }, new[] { brokenStorage }, Array.Empty<DrugCatalogEntry>()))!;
        var retryStorage = new Storage(() => { });

        Assert.Multiple(() =>
        {
            Assert.That(exception.InnerExceptions, Is.EqualTo(new[] { writeFailure, restoreFailure }));
            Assert.That(brokenStorage.State, Is.EqualTo(11), "The failed restoration leaves native state uncertain.");
            Assert.That(application.Applied, Is.Null, "Prior publication is no longer valid after failed restoration.");
            Assert.Throws<InvalidOperationException>(() => application.Publish(boundary, requested,
                new[] { key }, new[] { retryStorage }, Array.Empty<DrugCatalogEntry>()));
            Assert.That(retryStorage.Applied, Is.False, "A poisoned boundary must refuse before touching storage.");
        });

        var freshBoundary = application.BeginBoundary(catalog, null);
        application.Publish(freshBoundary, requested, new[] { key }, new[] { retryStorage }, Array.Empty<DrugCatalogEntry>());
        Assert.That(application.Applied!.IsAllowed(identity), Is.True);
        Assert.That(retryStorage.Applied, Is.True);
        Assert.That(prior.IsAllowed(identity), Is.False, "Invalidation must not mutate a detached historical snapshot.");
    }

    [Test]
    public void SuccessfulBatchPublishesOnlyAfterAllWritesAndTeardownClearsAppliedState()
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var application = new ContentApplication(store);
        var key = new ContentKey("ludeon.rimworld", "RimWorld.IncidentDef", "ShipChunkDrop");
        var identity = new object();
        var catalog = new ContentCatalog(new[] { new ContentCatalogEntry(key, identity) });
        var boundary = application.BeginBoundary(catalog, null);
        store.SetGlobal(settings.Individual(key), false);
        var request = settings.CaptureRequested(catalog, null);
        var first = new Storage(() => Assert.That(application.Applied, Is.Null));
        var second = new Storage(() => Assert.That(application.Applied, Is.Null));

        application.Publish(boundary, request, new[] { key }, new[] { first, second }, Array.Empty<DrugCatalogEntry>());
        var applied = application.Applied;
        Assert.That(first.State, Is.EqualTo(11));
        Assert.That(second.State, Is.EqualTo(11));
        Assert.That(applied!.IsAllowed(identity), Is.False);
        application.EndBoundary();
        Assert.That(application.Applied, Is.Null);
        Assert.That(applied.IsAllowed(identity), Is.False);
    }

    [TestCase("unadmitted")]
    [TestCase("ambiguous")]
    [TestCase("active-preset")]
    [TestCase("invalid-setting")]
    public void IncompleteApplicationIsRejectedBeforeAnyNativeWrite(string unavailable)
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var application = new ContentApplication(store);
        var key = new ContentKey("ludeon.rimworld", "RimWorld.IncidentDef", "ShipChunkDrop");
        var entries = new List<ContentCatalogEntry> { new(key, new object()) };
        if (unavailable == "ambiguous") entries.Add(new(key, new object()));
        var catalog = new ContentCatalog(entries);
        var definition = settings.Individual(key);
        store.SetGlobal(definition, false);
        if (unavailable == "active-preset") store.SetGlobal(settings.Preset(key), true);
        if (unavailable == "invalid-setting")
            store.LoadGlobals(new GlobalSettingsSnapshot(new Dictionary<string, string> { [definition.Id] = "invalid" }));
        var boundary = application.BeginBoundary(catalog, null);
        var request = settings.CaptureRequested(catalog, null, new[] { key });
        var storage = new Storage(() => { });
        var admitted = unavailable == "unadmitted" ? Array.Empty<ContentKey>() : new[] { key };

        Assert.Throws<InvalidOperationException>(() => application.Publish(boundary, request, admitted,
            new[] { storage }, Array.Empty<DrugCatalogEntry>()));
        Assert.That(storage.Applied, Is.False);
        Assert.That(application.Applied, Is.Null);
    }

    [TestCase("write-failure")]
    [TestCase("settings-change")]
    [TestCase("boundary-change")]
    public void FailedNativeBatchNeverPublishesAndRestoresAttemptedStorage(string failure)
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var application = new ContentApplication(store);
        var key = new ContentKey("ludeon.rimworld", "RimWorld.IncidentDef", "ShipChunkDrop");
        var identity = new object();
        var catalog = new ContentCatalog(new[] { new ContentCatalogEntry(key, identity) });
        var boundary = application.BeginBoundary(catalog, null);
        application.Publish(boundary, settings.CaptureRequested(catalog, null), new[] { key });
        var prior = application.Applied;
        store.SetGlobal(settings.Individual(key), false);
        var requested = settings.CaptureRequested(catalog, null);
        var storage = new Storage(() =>
        {
            Assert.That(application.Applied, Is.SameAs(prior), "Candidate must not publish during native writes.");
            if (failure == "write-failure") throw new InvalidOperationException("write failed");
            if (failure == "settings-change") store.SetGlobal(settings.Individual(key), true);
            if (failure == "boundary-change") application.BeginBoundary(catalog, null);
        });

        Assert.Throws<InvalidOperationException>(() => application.Publish(boundary, requested, new[] { key },
            new[] { storage }, Array.Empty<DrugCatalogEntry>()));
        Assert.Multiple(() =>
        {
            Assert.That(storage.Applied, Is.True);
            Assert.That(storage.State, Is.EqualTo(7));
            if (failure == "boundary-change") Assert.That(application.Applied, Is.Null);
            else Assert.That(application.Applied, Is.SameAs(prior));
        });
    }

    private sealed class Storage(Action duringApply, Action? duringRestore = null) : IDrugPolicyStorage
    {
        public int State { get; private set; } = 7;
        public bool Applied { get; private set; }
        public DrugPolicyStorageSnapshot Capture() => new(State, null);
        public void Apply(DrugPolicyPlan plan)
        {
            State = 11;
            Applied = true;
            duringApply();
        }
        public void Restore(DrugPolicyStorageSnapshot snapshot)
        {
            duringRestore?.Invoke();
            State = (int)snapshot.StorageIdentity!;
        }
    }

    [TestCase("settings")]
    [TestCase("reload-same-save")]
    [TestCase("other-save")]
    [TestCase("other-catalog")]
    [TestCase("ended")]
    public void RejectsStaleRequestsAndBoundaryGenerations(string changed)
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var application = new ContentApplication(store);
        var key = new ContentKey("ludeon.rimworld", "RimWorld.IncidentDef", "ShipChunkDrop");
        var catalog = new ContentCatalog(new[] { new ContentCatalogEntry(key, new object()) });
        var save = new SaveSettingsSnapshot("same-save");
        var boundary = application.BeginBoundary(catalog, save);
        var requested = settings.CaptureRequested(catalog, save);
        switch (changed)
        {
            case "settings": store.SetGlobal(settings.Individual(key), false); break;
            case "reload-same-save": application.BeginBoundary(catalog, save); break;
            case "other-save": requested = settings.CaptureRequested(catalog, new SaveSettingsSnapshot("other-save")); break;
            case "other-catalog": requested = settings.CaptureRequested(new ContentCatalog(catalog.Entries), save); break;
            case "ended": application.EndBoundary(); break;
        }
        Assert.Throws<InvalidOperationException>(() => application.Publish(boundary, requested, new[] { key }));
        Assert.That(application.Applied, Is.Null);
    }

    [Test]
    public void RequestedChangesRemainPendingUntilExplicitPublication()
    {
        var store = new SettingsStore();
        var settings = new ContentSettings(store);
        var application = new ContentApplication(store);
        var key = new ContentKey("ludeon.rimworld", "RimWorld.IncidentDef", "ShipChunkDrop");
        var identity = new object();
        var catalog = new ContentCatalog(new[] { new ContentCatalogEntry(key, identity) });
        var boundary = application.BeginBoundary(catalog, null);
        var allowed = settings.CaptureRequested(catalog, null);
        Assert.That(application.Applied, Is.Null);
        application.Publish(boundary, allowed, new[] { key });
        var prior = application.Applied;

        store.SetGlobal(settings.Individual(key), false);
        var pending = settings.CaptureRequested(catalog, null);
        Assert.That(application.Applied!.IsAllowed(identity), Is.True);
        application.Publish(boundary, pending, new[] { key });

        Assert.Multiple(() =>
        {
            Assert.That(application.Applied!.IsAllowed(identity), Is.False);
            Assert.That(prior!.IsAllowed(identity), Is.True);
            Assert.That(application.Applied.IsAllowed(new object()), Is.True);
        });
    }
}
