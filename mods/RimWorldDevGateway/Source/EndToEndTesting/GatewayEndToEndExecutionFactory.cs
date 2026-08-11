namespace RimWorldDevGateway;

public sealed class GatewayEndToEndExecutionFactory : IGatewayEndToEndExecutionFactory
{
    private readonly IGatewayEndToEndClock clock;
    private readonly Func<string, GatewayEndToEndTestContext> contextFactory;
    private readonly Func<string, IGatewayEndToEndStepDriver> stepDriverFactory;
    private readonly IGatewayEndToEndTestIsolation isolation;

    public GatewayEndToEndExecutionFactory(
        IGatewayEndToEndClock clock,
        Func<string, GatewayEndToEndTestContext> contextFactory,
        Func<string, IGatewayEndToEndStepDriver> stepDriverFactory,
        IGatewayEndToEndTestIsolation isolation)
    {
        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        this.contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        this.stepDriverFactory = stepDriverFactory ?? throw new ArgumentNullException(nameof(stepDriverFactory));
        this.isolation = isolation ?? throw new ArgumentNullException(nameof(isolation));
    }

    public IGatewayEndToEndExecutionMachine Create(
        IReadOnlyList<GatewayEndToEndRuntimeTestDescriptor> tests,
        string? sessionCredential,
        string artifactDirectory)
    {
        if (tests is null)
        {
            throw new ArgumentNullException(nameof(tests));
        }

        if (string.IsNullOrWhiteSpace(artifactDirectory))
        {
            throw new ArgumentException("An E2E artifact directory is required.", nameof(artifactDirectory));
        }

        var driver = stepDriverFactory(artifactDirectory) ??
                     throw new InvalidOperationException("The E2E step-driver factory returned null.");
        return new GatewayEndToEndExecutionStateMachine(
            tests,
            clock,
            () => contextFactory(artifactDirectory),
            driver,
            isolation,
            sessionCredential);
    }
}
