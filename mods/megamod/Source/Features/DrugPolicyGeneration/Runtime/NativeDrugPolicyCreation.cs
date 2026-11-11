using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Megamod.Features.DrugPolicyGeneration.Core;
using Megamod.Features.DrugPolicyMaintenance.Core;
using Megamod.Features.DrugPolicyMaintenance.Runtime;
using RimWorld;

namespace Megamod.Features.DrugPolicyGeneration.Runtime;

/// <summary>One transient confirmation. The caller owns current game/catalog/effective-admission receipts.</summary>
public sealed class NativeDrugPolicyCreation
{
    private static readonly FieldInfo Policies = typeof(DrugPolicyDatabase).GetField("policies", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private readonly DrugPolicyDatabase database;
    private readonly IReadOnlyList<DrugCatalogEntry> catalog;
    private readonly IReadOnlyList<DrugGenerationEdit> edits;
    private readonly string name;
    private readonly Func<bool> isCurrentAndAdmitted;
    private readonly Action<List<DrugPolicyRow>> nativeOrdering;
    private DrugPolicy? prepared;
    private bool completed;
    private bool busy, failed;
    private List<DrugPolicy>? membership;
    private Prior[]? before;
    private Prior? intended;

    private sealed record Prior(DrugPolicy Policy, int Id, string Label, DrugPolicyDef? Source,
        DrugPolicyStorageSnapshot Storage, string?[] Buffers);

    public NativeDrugPolicyCreation(DrugPolicyDatabase database, IReadOnlyList<DrugCatalogEntry> catalog,
        IReadOnlyList<DrugGenerationEdit> edits, string name, Func<bool> isCurrentAndAdmitted,
        Action<List<DrugPolicyRow>> nativeOrdering)
    {
        this.database = database;
        this.catalog = Array.AsReadOnly(catalog.ToArray());
        this.edits = Array.AsReadOnly(edits.ToArray());
        this.name = name;
        this.isCurrentAndAdmitted = isCurrentAndAdmitted;
        this.nativeOrdering = nativeOrdering;
    }

    public DrugPolicy Prepare() => Run(PrepareCore);

    private DrugPolicy PrepareCore()
    {
        RequireCurrent();
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
        var byIdentity = edits.ToDictionary(edit => edit.DrugIdentity, ReferenceEqualityComparer.Instance);
        var rows = plan.Rows.Select(row => row with
        {
            Choices = byIdentity[row.DrugIdentity!].Choices,
            ToleranceChoice = byIdentity[row.DrugIdentity!].Tolerance
        }).ToArray();
        RequireCurrent();
        if (!PriorMatches(false)) throw new InvalidOperationException("Policy membership or choices changed during ordering.");
        var candidate = new DrugPolicy { id = nextId, label = name, sourceDef = null };
        new NativeDrugPolicyStorage(candidate).Apply(plan with { Rows = Array.AsReadOnly(rows) });
        RequireCurrent();
        if (!PriorMatches(false)) throw new InvalidOperationException("Policy state changed during preparation.");
        prepared = candidate;
        intended = Capture(candidate);
        return prepared;
    }

    public DrugPolicy Confirm() => Run(() =>
    {
        var result = PrepareCore();
        if (completed) return result;
        // No caller/native callback occurs from this final storage check through publication/postcheck.
        if (!PriorMatches(false) || !Matches(intended!)) throw new InvalidOperationException("Creation became stale.");
        var appended = false;
        try
        {
            membership!.Add(result);
            appended = true;
            if (!PriorMatches(true) || !Matches(intended!))
                throw new InvalidOperationException("The complete published policy could not be verified.");
            completed = true;
            return result;
        }
        catch (Exception failure)
        {
            if (appended)
            {
                if (!PriorMatches(true))
                    throw new AggregateException("Independent policy changes prevent owned-tail rollback.", failure);
                membership!.RemoveAt(membership.Count - 1);
                if (!PriorMatches(false))
                    throw new AggregateException("Owned policy rollback could not be verified.", failure);
            }
            throw;
        }
    });

    private DrugPolicy Run(Func<DrugPolicy> operation)
    {
        if (busy || failed) throw new InvalidOperationException("This confirmation is busy or no longer usable.");
        busy = true;
        try { return operation(); }
        catch { failed = true; throw; }
        finally { busy = false; }
    }

    private void RequireCurrent()
    {
        if (!isCurrentAndAdmitted()) throw new InvalidOperationException("Current creation admission is unavailable.");
    }

    private bool PriorMatches(bool published)
    {
        if (!ReferenceEquals(Policies.GetValue(database), membership) || before == null || membership == null ||
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
