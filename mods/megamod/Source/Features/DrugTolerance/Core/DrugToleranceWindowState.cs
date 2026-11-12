using System;
using System.Collections.Generic;

namespace Megamod.Features.DrugTolerance.Core;

// State for one still-open policy manager. Native identity/currentness stays with its caller.
public sealed class DrugToleranceWindowState
{
    private bool headerFailed;
    private readonly List<(object Policy, object Row)> failedRows = new();
    private readonly HashSet<string> notices = new(StringComparer.Ordinal);
    public void AssessHeader(bool fits) => headerFailed = !fits;
    public bool AssessHeader(bool allocationFits, bool viewportFits)
    {
        AssessHeader(allocationFits);
        return allocationFits && viewportFits;
    }
    public bool AssessRow(object policy, object row, bool allocationFits, bool viewportFits)
    {
        AssessRow(policy, row, allocationFits);
        return allocationFits && viewportFits;
    }
    public void AssessRow(object policy, object row, bool fits)
    {
        int found = failedRows.FindIndex(item => ReferenceEquals(item.Policy, policy) && ReferenceEquals(item.Row, row));
        if (fits) { if (found >= 0) failedRows.RemoveAt(found); }
        else if (found < 0) failedRows.Add((policy, row));
    }
    public bool HasFailure(Func<object, object, bool> isCurrent)
    {
        failedRows.RemoveAll(item => !isCurrent(item.Policy, item.Row));
        return headerFailed || failedRows.Count != 0;
    }
    public bool TakeNotice(string? reason) => reason != null && notices.Add(reason);
    public static bool Supports(object manager, Type nativeType) => manager.GetType() == nativeType;
}
