namespace PathfindingLab.Core;

public readonly record struct RoutingSnapshot(long Handled, long Partial, long Fallbacks);

// Monotonic per-owner counters; snapshots survive plan cancellation and mode changes.
public sealed class RoutingCounts
{
    public RoutingSnapshot Snapshot { get; private set; }
    public long Renewals { get; private set; }
    public int MinRenewalNodes { get; private set; } = int.MaxValue;
    public int MaxRenewalNodes { get; private set; }
    public int MaxSegmentSteps { get; private set; }
    public void Renew(int remainingNodes)
    {
        Renewals++;
        MinRenewalNodes = System.Math.Min(MinRenewalNodes, remainingNodes);
        MaxRenewalNodes = System.Math.Max(MaxRenewalNodes, remainingNodes);
    }
    public void Handle(bool partial, int steps = 0)
    {
        MaxSegmentSteps = System.Math.Max(MaxSegmentSteps, steps);
        Snapshot = Snapshot with { Handled = Snapshot.Handled + 1, Partial = Snapshot.Partial + (partial ? 1 : 0) };
    }
    public void Fallback() => Snapshot = Snapshot with { Fallbacks = Snapshot.Fallbacks + 1 };
}
