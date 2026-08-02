namespace ImmersiveChefs;

public sealed class IngestionLifecycleState
{
    private int depth;

    public bool ShouldRecoverEmbeddedWareOnDestroy => depth == 0;
    public bool ShouldCancelDiningSessionOnJobCleanup => depth == 0;

    public void Begin()
    {
        depth++;
    }

    public void End()
    {
        if (depth > 0)
        {
            depth--;
        }
    }
}
