using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Megamod.Features.DrugPolicyMaintenance.Core;
using Megamod.Settings;

namespace Megamod.Features.ContentSelection.Core;

public sealed class ContentBoundary
{
    internal ContentBoundary(ContentCatalog catalog, SaveSettingsSnapshot? save)
    {
        Catalog = catalog;
        Save = save;
    }
    public ContentCatalog Catalog { get; }
    public SaveSettingsSnapshot? Save { get; }
}

public sealed class AppliedContentPolicy
{
    private readonly HashSet<object> excluded;
    internal AppliedContentPolicy(IEnumerable<object> excluded, IDictionary<ContentKey, bool> keyDecisions, bool enabled = true)
    {
        Enabled = enabled;
        this.excluded = new(excluded, ReferenceEqualityComparer.Instance);
        KeyDecisions = new ReadOnlyDictionary<ContentKey, bool>(new Dictionary<ContentKey, bool>(keyDecisions));
    }
    public IReadOnlyDictionary<ContentKey, bool> KeyDecisions { get; }
    public bool Enabled { get; }
    public bool IsAllowed(object identity) => !excluded.Contains(identity);
}

// One game-thread owner. Requests and catalog identities never become availability implicitly.
public sealed class ContentApplication
{
    private readonly SettingsStore store;
    private ContentBoundary? current;
    public ContentApplication(SettingsStore store) => this.store = store;
    public AppliedContentPolicy? Applied { get; private set; }

    public ContentBoundary BeginBoundary(ContentCatalog catalog, SaveSettingsSnapshot? save)
    {
        current = new(catalog, save);
        Applied = null;
        return current;
    }

    public void EndBoundary()
    {
        current = null;
        Applied = null;
    }

    public void Publish(ContentBoundary boundary, RequestedContentSnapshot requested,
        IReadOnlyCollection<ContentKey> admittedKeys,
        IReadOnlyList<IDrugPolicyStorage>? drugPolicies = null,
        IReadOnlyList<DrugCatalogEntry>? drugCatalog = null,
        Action<List<DrugPolicyRow>>? nativeDrugOrdering = null)
    {
        bool IsCurrent() => ReferenceEquals(current, boundary) && ReferenceEquals(boundary.Catalog, requested.Catalog) &&
            ReferenceEquals(boundary.Save, requested.Save) && store.Revision == requested.Revision;
        if (!IsCurrent()) throw new InvalidOperationException("The requested content policy belongs to a stale boundary or settings revision.");
        if (requested.Enabled.ErrorKey != null)
            throw new InvalidOperationException("The requested content master switch is invalid.");
        if (!requested.Enabled.Value)
        {
            // Off is an absence of owned exclusions, never false for each allowed choice.
            // Retained unsupported presets and malformed member choices cannot prevent disablement.
            Applied = new AppliedContentPolicy(Array.Empty<object>(),
                requested.Individual.ToDictionary(pair => pair.Key, _ => true), false);
            return;
        }
        foreach (var preset in requested.Presets.Values)
            if (preset.ErrorKey != null || preset.Value)
                throw new InvalidOperationException("Preset application is not yet available; its requested choice remains stored.");
        var admitted = new HashSet<ContentKey>(admittedKeys);
        var excluded = new List<object>();
        foreach (var pair in requested.Individual)
        {
            if (pair.Value.ErrorKey != null)
                throw new InvalidOperationException("A requested content value is invalid.");
            if (pair.Value.Value) continue;
            var resolution = requested.Catalog.Resolve(pair.Key);
            if (!admitted.Contains(pair.Key) || resolution.Status != ContentResolutionStatus.Present || resolution.Identity == null)
                throw new InvalidOperationException("A requested exclusion is unresolved, ambiguous or not admitted.");
            excluded.Add(resolution.Identity);
        }
        var candidate = new AppliedContentPolicy(excluded, requested.Individual.ToDictionary(pair => pair.Key, pair => pair.Value.Value));
        if (!IsCurrent()) throw new InvalidOperationException("The content boundary changed before publication.");
        if (drugPolicies != null)
        {
            try
            {
                DrugPolicyBatch.Apply(drugPolicies, drugCatalog ?? throw new ArgumentException("A native drug catalog is required with policy storage."),
                    IsCurrent, nativeDrugOrdering);
            }
            catch (DrugPolicyRollbackException)
            {
                // Restoration failed: this boundary cannot claim a valid prior policy or retry.
                if (ReferenceEquals(current, boundary)) EndBoundary();
                throw;
            }
        }
        Applied = candidate;
    }
}
