using Verse.AI;

namespace ImmersiveChefs;

internal sealed class StableJobIdentity
{
    private readonly Job job;
    private readonly int loadId;

    internal StableJobIdentity(Job job)
    {
        this.job = job;
        loadId = job.loadID;
    }

    internal bool Matches(Job? current) =>
        ReferenceEquals(current, job) && current.loadID == loadId;
}
