using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Megamod.Features.DrugPolicyGeneration.Core;
using Megamod.Features.DrugPolicyMaintenance.Core;
using Megamod.Features.DrugPolicyMaintenance.Runtime;
using Megamod.Features.DrugTolerance.Core;
using RimWorld;
using Verse;

namespace Megamod.Features.DrugPolicyGeneration.Runtime;

/// <summary>One transient confirmation. The caller owns current game/catalog/effective-admission receipts.</summary>
public sealed class NativeDrugPolicyCreation : IDisposable
{
    private static readonly FieldInfo Policies = typeof(DrugPolicyDatabase).GetField("policies", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly Regex ValidName = new(@"\A[\p{L}0-9 '\-]+\z", RegexOptions.CultureInvariant);
    private DrugPolicyDatabase database;
    private IReadOnlyList<DrugCatalogEntry> catalog;
    private IReadOnlyList<DrugGenerationEdit> edits;
    private string name;
    private Func<bool> isCurrentAndAdmitted;
    private Action<List<DrugPolicyRow>> nativeOrdering;
    private readonly bool toleranceAvailable;
    private Func<DrugPolicy, IDrugPolicyStorage> preparationStorage;
    private DrugPolicy? prepared;
    private bool completed;
    private bool busy, failed, disposed;
    private List<DrugPolicy>? membership;
    private Prior[]? before;
    private Prior? intended;

    private sealed record Prior(DrugPolicy Policy, int Id, string Label, DrugPolicyDef? Source,
        DrugPolicyStorageSnapshot Storage, string?[] Buffers);

    public NativeDrugPolicyCreation(DrugPolicyDatabase database, IReadOnlyList<DrugCatalogEntry> catalog,
        IReadOnlyList<DrugGenerationEdit> edits, string name, Func<bool> isCurrentAndAdmitted,
        Action<List<DrugPolicyRow>> nativeOrdering, bool toleranceAvailable)
        : this(database, catalog, edits, name, isCurrentAndAdmitted, nativeOrdering, toleranceAvailable,
            policy => new NativeDrugPolicyStorage(policy)) { }

    internal NativeDrugPolicyCreation(DrugPolicyDatabase database, IReadOnlyList<DrugCatalogEntry> catalog,
        IReadOnlyList<DrugGenerationEdit> edits, string name, Func<bool> isCurrentAndAdmitted,
        Action<List<DrugPolicyRow>> nativeOrdering, bool toleranceAvailable,
        Func<DrugPolicy, IDrugPolicyStorage> preparationStorage)
    {
        this.database = database;
        this.catalog = Array.AsReadOnly(catalog.ToArray());
        this.edits = Array.AsReadOnly(edits.ToArray());
        this.name = name;
        this.isCurrentAndAdmitted = isCurrentAndAdmitted;
        this.nativeOrdering = nativeOrdering;
        this.toleranceAvailable = toleranceAvailable;
        this.preparationStorage = preparationStorage;
    }

    public DrugPolicy Prepare() => Run(PrepareCore);

    public void Dispose()
    {
        disposed = true;
        database = null!;
        catalog = Array.Empty<DrugCatalogEntry>(); edits = Array.Empty<DrugGenerationEdit>(); name = string.Empty;
        isCurrentAndAdmitted = null!; nativeOrdering = null!; preparationStorage = null!;
        prepared = null; membership = null; before = null; intended = null;
    }

    private DrugPolicy PrepareCore()
    {
        RequireCurrent();
        var byIdentity = ValidateInputs();
        if (prepared != null)
        {
            if (!PriorMatches(completed) || !Matches(intended!))
                throw new InvalidOperationException("The reviewed policy state changed.");
            return prepared;
        }
        membership = database.AllPolicies;
        if (membership == null || membership.Count == 0 || membership.Any(policy => policy == null))
            throw new InvalidOperationException("Creation requires an existing native default policy.");
        before = membership.Select(Capture).ToArray();
        var nextId = checked(Math.Max(0, membership.Max(policy => policy.id)) + 1);
        var plan = DrugPolicyReconciler.Plan(null, catalog, nativeOrdering);
        var rows = plan.Rows.Select(row => row with
        {
            Choices = byIdentity[row.DrugIdentity!].Choices,
            ToleranceChoice = byIdentity[row.DrugIdentity!].Tolerance
        }).ToArray();
        RequireCurrent();
        ValidateInputs();
        if (!PriorMatches(false)) throw new InvalidOperationException("Policy membership or choices changed during ordering.");
        var candidate = new DrugPolicy { id = nextId, label = name, sourceDef = null };
        preparationStorage(candidate).Apply(plan with { Rows = Array.AsReadOnly(rows) });
        RequireCurrent();
        ValidateInputs();
        if (!PriorMatches(false)) throw new InvalidOperationException("Policy state changed during preparation.");
        var actual = Capture(candidate);
        VerifyPayload(actual, nextId, rows);
        prepared = candidate;
        intended = actual;
        return prepared;
    }

    public DrugPolicy Confirm() => Run(() =>
    {
        var result = PrepareCore();
        if (completed) return result;
        // No caller/native callback occurs from this final storage check through publication/postcheck.
        var publication = CreatePublication();
        publication.Append();
        publication.VerifyOrRollback();
        return result;
    });

    internal Publication CreatePublication()
    {
        if (disposed || failed || completed || prepared == null || intended == null)
            throw new InvalidOperationException("There is no current unpublished preparation.");
        return new Publication(this);
    }

    // This split permits constructed-Defs verification of the exact production postcheck/rollback.
    // Confirm invokes both methods synchronously; there is no runtime callback between them.
    internal sealed class Publication
    {
        private NativeDrugPolicyCreation? owner;
        private bool appended;
        internal Publication(NativeDrugPolicyCreation owner) => this.owner = owner;

        internal void Append()
        {
            var current = owner ?? throw new InvalidOperationException("This publication is finished.");
            if (appended) throw new InvalidOperationException("This publication was already appended.");
            try
            {
                if (current.disposed || current.failed || !current.PriorMatches(false) || !Matches(current.intended!))
                    throw new InvalidOperationException("Creation became stale.");
                current.membership!.Add(current.prepared!);
                appended = true;
            }
            catch { current.failed = true; owner = null; throw; }
        }

        internal void VerifyOrRollback()
        {
            var current = owner ?? throw new InvalidOperationException("This publication is finished.");
            if (!appended) throw new InvalidOperationException("This publication has not appended its policy.");
            try
            {
                if (current.disposed || !current.PriorMatches(true) || !Matches(current.intended!))
                    throw new InvalidOperationException("The complete published policy could not be verified.");
                current.completed = true;
            }
            catch (Exception failure)
            {
                current.failed = true;
                if (!current.PriorMatches(true))
                    throw new AggregateException("Independent policy changes prevent owned-tail rollback.", failure);
                current.membership!.RemoveAt(current.membership.Count - 1);
                if (!current.PriorMatches(false))
                    throw new AggregateException("Owned policy rollback could not be verified.", failure);
                throw;
            }
            finally { owner = null; }
        }
    }

    private DrugPolicy Run(Func<DrugPolicy> operation)
    {
        if (disposed) throw new ObjectDisposedException(nameof(NativeDrugPolicyCreation));
        if (busy || failed) throw new InvalidOperationException("This confirmation is busy or no longer usable.");
        busy = true;
        try { return operation(); }
        catch { failed = true; throw; }
        finally { busy = false; }
    }

    private void RequireCurrent()
    {
        if (disposed) throw new ObjectDisposedException(nameof(NativeDrugPolicyCreation));
        if (!isCurrentAndAdmitted() || disposed) throw new InvalidOperationException("Current creation admission is unavailable.");
    }

    private Dictionary<object, DrugGenerationEdit> ValidateInputs()
    {
        if (name == null || name.Length == 0 || name.Length >= 28 || !ValidName.IsMatch(name))
            throw new InvalidOperationException("The reviewed policy name is outside the native rename contract.");
        if (catalog.Count == 0 || edits.Count != catalog.Count)
            throw new InvalidOperationException("Creation requires one edit for every current catalog drug.");
        var byIdentity = new Dictionary<object, DrugGenerationEdit>(ReferenceEqualityComparer.Instance);
        foreach (var edit in edits)
        {
            if (edit == null || edit.DrugIdentity == null || edit.Choices == null || edit.Tolerance == null ||
                !byIdentity.TryAdd(edit.DrugIdentity, edit))
                throw new InvalidOperationException("A reviewed edit is missing or duplicated.");
        }
        var identities = new HashSet<object>(ReferenceEqualityComparer.Instance);
        foreach (var entry in catalog)
        {
            if (entry == null || entry.Identity is not ThingDef drug || !identities.Add(drug) ||
                drug.category != ThingCategory.Item || !drug.IsDrug || drug.comps == null ||
                drug.GetCompProperties<CompProperties_Drug>() == null || entry.Addictive != drug.IsAddictiveDrug ||
                entry.Pleasure != drug.IsPleasureDrug || !byIdentity.TryGetValue(drug, out var edit))
                throw new InvalidOperationException("The reviewed edit set or current catalog capability changed.");
            var choices = edit.Choices;
            int maximumInventory = PawnUtility.GetMaxAllowedToPickUp(drug);
            if ((choices.Addiction && !entry.Addictive) || (choices.Recreation && !entry.Pleasure) ||
                !float.IsFinite(choices.DaysFrequency) || choices.DaysFrequency < .1f || choices.DaysFrequency > 25f ||
                !Threshold(choices.MoodThreshold) || !Threshold(choices.RecreationThreshold) ||
                maximumInventory < 0 || choices.InventoryTarget < 0 || choices.InventoryTarget > maximumInventory ||
                edit.Tolerance.Kind == DrugToleranceChoiceKind.NeedsReview ||
                (!toleranceAvailable && edit.Tolerance.Kind != DrugToleranceChoiceKind.Any))
                throw new InvalidOperationException("A reviewed policy choice is invalid or outside its current native bound.");
        }
        return byIdentity;
    }

    private static bool Threshold(float value) => float.IsFinite(value) && value >= 0 && value <= 1;

    private void VerifyPayload(Prior actual, int nextId, IReadOnlyList<DrugPolicyRow> rows)
    {
        if (actual.Id != nextId || actual.Label != name || actual.Source != null ||
            actual.Storage.Rows == null || actual.Storage.Rows.Count != rows.Count ||
            actual.Buffers.Any(buffer => buffer != null) ||
            before!.Any(previous => ReferenceEquals(previous.Storage.StorageIdentity, actual.Storage.StorageIdentity)))
            throw new InvalidOperationException("Prepared policy identity or storage differs from the reviewed payload.");
        var borrowed = new HashSet<object>(before!.SelectMany(previous => previous.Storage.Rows ?? Array.Empty<DrugPolicyRow?>())
            .Where(row => row?.RowIdentity != null).Select(row => row!.RowIdentity!), ReferenceEqualityComparer.Instance);
        var fresh = new HashSet<object>(ReferenceEqualityComparer.Instance);
        for (int index = 0; index < rows.Count; index++)
        {
            var expected = rows[index]; var observed = actual.Storage.Rows[index];
            if (observed?.RowIdentity is not DrugPolicyEntry identity || !fresh.Add(identity) || borrowed.Contains(identity) ||
                !ReferenceEquals(observed.DrugIdentity, expected.DrugIdentity) || observed.Choices != expected.Choices ||
                observed.ToleranceChoice == null || expected.ToleranceChoice == null ||
                observed.ToleranceChoice.Kind != expected.ToleranceChoice.Kind ||
                observed.ToleranceChoice.Limit != expected.ToleranceChoice.Limit ||
                observed.ToleranceChoice.Encoding != expected.ToleranceChoice.Encoding)
                throw new InvalidOperationException("Prepared rows differ from the complete ordered fresh reviewed choices.");
        }
    }

    private bool PriorMatches(bool published)
    {
        if (disposed || !ReferenceEquals(Policies.GetValue(database), membership) || before == null || membership == null ||
            membership.Count != before.Length + (published ? 1 : 0)) return false;
        for (var i = 0; i < before.Length; i++)
            if (!ReferenceEquals(membership[i], before[i].Policy) || !Matches(before[i])) return false;
        return !published || ReferenceEquals(membership[membership.Count - 1], prepared);
    }

    private static Prior Capture(DrugPolicy policy)
    {
        var storage = new NativeDrugPolicyStorage(policy).Capture();
        var buffers = storage.Rows?.Select(row => (row?.RowIdentity as DrugPolicyEntry)?.takeToInventoryTempBuffer).ToArray()
            ?? Array.Empty<string?>();
        return new(policy, policy.id, policy.label, policy.sourceDef, storage, buffers);
    }

    private static bool Matches(Prior expected)
    {
        var current = Capture(expected.Policy);
        if (current.Id != expected.Id || current.Label != expected.Label || !ReferenceEquals(current.Source, expected.Source) ||
            !ReferenceEquals(current.Storage.StorageIdentity, expected.Storage.StorageIdentity) ||
            !current.Buffers.SequenceEqual(expected.Buffers)) return false;
        var first = current.Storage.Rows;
        var second = expected.Storage.Rows;
        if (first == null || second == null) return first == null && second == null;
        if (first.Count != second.Count) return false;
        for (var i = 0; i < first.Count; i++)
        {
            var a = first[i]; var b = second[i];
            if (a == null || b == null) { if (a != null || b != null) return false; else continue; }
            if (!ReferenceEquals(a.RowIdentity, b.RowIdentity) || !ReferenceEquals(a.DrugIdentity, b.DrugIdentity) ||
                a.Choices != b.Choices || !ReferenceEquals(a.ToleranceChoice, b.ToleranceChoice)) return false;
        }
        return true;
    }
}
