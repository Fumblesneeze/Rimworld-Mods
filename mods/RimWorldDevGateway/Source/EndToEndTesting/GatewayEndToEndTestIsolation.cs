namespace RimWorldDevGateway;

public sealed class GatewayEndToEndIsolationBaseline
{
    public GatewayEndToEndIsolationBaseline(object state) =>
        State = state ?? throw new ArgumentNullException(nameof(state));

    public object State { get; }
}

public interface IGatewayEndToEndIsolationOperations
{
    GatewayEndToEndIsolationBaseline CaptureBaseline();

    void Pause();

    void ResetTransientState();

    bool VerifyEmpty();

    bool RestoreBaseline(GatewayEndToEndIsolationBaseline baseline);
}

public sealed class GatewayEndToEndTestIsolation : IGatewayEndToEndTestIsolation
{
    private readonly IGatewayEndToEndIsolationOperations operations;
    private GatewayEndToEndIsolationBaseline? baseline;

    public GatewayEndToEndTestIsolation(IGatewayEndToEndIsolationOperations operations) =>
        this.operations = operations ?? throw new ArgumentNullException(nameof(operations));

    public void Prepare(RimWorldDevGateway.EndToEndTesting.IEndToEndContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        if (baseline is not null)
        {
            throw new InvalidOperationException("The prior E2E isolation baseline has not been cleaned up.");
        }

        baseline = operations.CaptureBaseline() ??
                   throw new InvalidOperationException("The E2E isolation backend returned no baseline.");
        operations.Pause();
        operations.ResetTransientState();
        if (!operations.VerifyEmpty())
        {
            throw new InvalidOperationException("The E2E runner could not establish an empty baseline.");
        }
    }

    public bool Cleanup(RimWorldDevGateway.EndToEndTesting.IEndToEndContext context)
    {
        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        var captured = baseline;
        baseline = null;
        if (captured is null)
        {
            return false;
        }

        var trustworthy = Try(operations.Pause);
        trustworthy &= Try(operations.ResetTransientState);
        trustworthy &= Try(operations.VerifyEmpty);
        trustworthy &= Try(() => operations.RestoreBaseline(captured));
        trustworthy &= Try(operations.VerifyEmpty);
        return trustworthy;
    }

    private static bool Try(Action action)
    {
        try
        {
            action();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static bool Try(Func<bool> action)
    {
        try
        {
            return action();
        }
        catch
        {
            return false;
        }
    }
}
