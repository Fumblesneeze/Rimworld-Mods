using System;
using System.Collections.Generic;
using System.Linq;

namespace Megamod.Features.DrugPolicyMaintenance.Core;

// The adapter owns actual engine storage and restores both membership and entry values.
public interface IDrugPolicyStorage
{
    DrugPolicyStorageSnapshot Capture();
    void Apply(DrugPolicyPlan plan);
    void Restore(DrugPolicyStorageSnapshot snapshot);
}

public sealed class DrugPolicyStorageSnapshot
{
    public DrugPolicyStorageSnapshot(object? storageIdentity, IReadOnlyList<DrugPolicyRow?>? rows)
    {
        StorageIdentity = storageIdentity;
        Rows = rows == null ? null : Array.AsReadOnly(rows.ToArray());
    }

    public object? StorageIdentity { get; }
    public IReadOnlyList<DrugPolicyRow?>? Rows { get; }
}

public sealed class DrugPolicyRollbackException : AggregateException
{
    internal DrugPolicyRollbackException(Exception applicationFailure, IEnumerable<Exception> rollbackFailures)
        : base("Drug-policy application failed and prior state could not be fully restored.",
            new[] { applicationFailure }.Concat(rollbackFailures))
    {
    }
}

public static class DrugPolicyBatch
{
    public static IReadOnlyList<DrugPolicyPlan> Apply(
        IReadOnlyList<IDrugPolicyStorage> targets,
        IReadOnlyList<DrugCatalogEntry> catalog,
        Func<bool> isCurrent,
        Action<List<DrugPolicyRow>>? applyNativeOrdering = null)
    {
        var targetSnapshot = targets.ToArray();
        var catalogSnapshot = catalog.ToArray();
        var prepared = targetSnapshot.Select(target =>
        {
            var snapshot = target.Capture();
            var plan = DrugPolicyReconciler.Plan(snapshot.Rows, catalogSnapshot, applyNativeOrdering);
            return (Target: target, Snapshot: snapshot, Plan: plan);
        }).ToArray();

        var attempted = -1;
        try
        {
            for (var index = 0; index < prepared.Length; index++)
            {
                if (!isCurrent())
                    throw new InvalidOperationException("The drug catalog or save generation changed before a policy write.");
                attempted = index;
                prepared[index].Target.Apply(prepared[index].Plan);
            }
            if (!isCurrent())
                throw new InvalidOperationException("The drug catalog or save generation changed before publication.");
        }
        catch (Exception applicationFailure)
        {
            var rollbackFailures = new List<Exception>();
            for (var index = attempted; index >= 0; index--)
            {
                try
                {
                    prepared[index].Target.Restore(prepared[index].Snapshot);
                }
                catch (Exception rollbackFailure)
                {
                    rollbackFailures.Add(rollbackFailure);
                }
            }
            if (rollbackFailures.Count != 0)
                throw new DrugPolicyRollbackException(applicationFailure, rollbackFailures);
            throw;
        }

        return Array.AsReadOnly(prepared.Select(item => item.Plan).ToArray());
    }
}
