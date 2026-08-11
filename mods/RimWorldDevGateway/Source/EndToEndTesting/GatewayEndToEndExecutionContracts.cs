using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway;

public interface IGatewayEndToEndClock
{
    long FrameCount { get; }

    int GameTick { get; }

    DateTimeOffset UtcNow { get; }
}

public interface IGatewayEndToEndStepDriver
{
    IGatewayEndToEndStepOperation Begin(EndToEndStep step, IEndToEndContext context);
}

public interface IGatewayEndToEndStepOperation
{
    bool IsCompleted { get; }

    GatewayEndToEndStepOutcome GetOutcome();
}

public sealed class GatewayEndToEndStepOutcome
{
    private GatewayEndToEndStepOutcome(
        bool passed,
        string? failureCode,
        string? failureMessage,
        IReadOnlyDictionary<string, string>? artifacts)
    {
        Passed = passed;
        FailureCode = failureCode;
        FailureMessage = failureMessage;
        Artifacts = artifacts ?? new Dictionary<string, string>();
    }

    public bool Passed { get; }

    public string? FailureCode { get; }

    public string? FailureMessage { get; }

    public IReadOnlyDictionary<string, string> Artifacts { get; }

    public static GatewayEndToEndStepOutcome Pass(
        IReadOnlyDictionary<string, string>? artifacts = null) =>
        new(true, null, null, artifacts);

    public static GatewayEndToEndStepOutcome Fail(string code, string message) =>
        new(
            false,
            string.IsNullOrWhiteSpace(code)
                ? throw new ArgumentException("A step failure code is required.", nameof(code))
                : code,
            message ?? throw new ArgumentNullException(nameof(message)),
            null);
}

public sealed class GatewayEndToEndCompletedStepOperation : IGatewayEndToEndStepOperation
{
    private readonly GatewayEndToEndStepOutcome outcome;

    private GatewayEndToEndCompletedStepOperation(GatewayEndToEndStepOutcome outcome) =>
        this.outcome = outcome;

    public bool IsCompleted => true;

    public GatewayEndToEndStepOutcome GetOutcome() => outcome;

    public static GatewayEndToEndCompletedStepOperation Passed(
        IReadOnlyDictionary<string, string>? artifacts = null) =>
        new(GatewayEndToEndStepOutcome.Pass(artifacts));

    public static GatewayEndToEndCompletedStepOperation Failed(string code, string message) =>
        new(GatewayEndToEndStepOutcome.Fail(code, message));

    public static GatewayEndToEndCompletedStepOperation FromOutcome(
        GatewayEndToEndStepOutcome outcome) =>
        new(outcome ?? throw new ArgumentNullException(nameof(outcome)));
}

public interface IGatewayEndToEndTestIsolation
{
    bool IsReady { get; }

    void Prepare(IEndToEndContext context);

    bool Cleanup(IEndToEndContext context);
}

public interface IGatewayEndToEndExecutionMachine
{
    GatewayEndToEndExecutionSnapshot Snapshot { get; }

    bool PersistencePending { get; }

    void ConfirmPersisted(GatewayEndToEndExecutionSnapshot exactSnapshot);

    void Advance();
}

public interface IGatewayEndToEndExecutionFactory
{
    IGatewayEndToEndExecutionMachine Create(
        IReadOnlyList<GatewayEndToEndRuntimeTestDescriptor> tests,
        string? sessionCredential,
        string artifactDirectory);
}

public interface IGatewayEndToEndExecutionReadiness
{
    bool IsPlayableMapReady();
}

public sealed class GatewayEndToEndTestContext : IEndToEndContext
{
    private readonly Func<long> frameCount;
    private readonly Func<int> gameTick;
    private readonly Func<Type, object?> serviceProvider;
    private readonly List<Func<bool>> cleanupActions = new();
    private bool cleaning;
    private bool cleanupFailed;

    public GatewayEndToEndTestContext(
        Func<long> frameCount,
        Func<int> gameTick,
        Func<Type, object?> serviceProvider)
    {
        this.frameCount = frameCount ?? throw new ArgumentNullException(nameof(frameCount));
        this.gameTick = gameTick ?? throw new ArgumentNullException(nameof(gameTick));
        this.serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    public long FrameCount => frameCount();

    public int GameTick => gameTick();

    public object? GetService(Type serviceType)
    {
        if (serviceType is null)
        {
            throw new ArgumentNullException(nameof(serviceType));
        }

        return serviceProvider(serviceType);
    }

    public void DeferCleanup(Action cleanupAction)
    {
        if (cleanupAction is null)
        {
            throw new ArgumentNullException(nameof(cleanupAction));
        }

        if (cleaning)
        {
            throw new InvalidOperationException("E2E cleanup has already started.");
        }

        cleanupActions.Add(() =>
        {
            cleanupAction();
            return true;
        });
    }

    internal void DeferCleanup(Func<bool> cleanupPoll)
    {
        if (cleanupPoll is null) throw new ArgumentNullException(nameof(cleanupPoll));
        if (cleaning) throw new InvalidOperationException("E2E cleanup has already started.");
        cleanupActions.Add(cleanupPoll);
    }

    internal DeferredCleanupStatus RunDeferredCleanup()
    {
        cleaning = true;
        while (cleanupActions.Count != 0)
        {
            var index = cleanupActions.Count - 1;
            try
            {
                if (!cleanupActions[index]()) return DeferredCleanupStatus.Pending;
                cleanupActions.RemoveAt(index);
            }
            catch
            {
                cleanupActions.RemoveAt(index);
                cleanupFailed = true;
            }
        }
        return cleanupFailed ? DeferredCleanupStatus.Failed : DeferredCleanupStatus.Completed;
    }

    internal void AbandonDeferredCleanup() => cleanupActions.Clear();
}

internal enum DeferredCleanupStatus
{
    Pending,
    Completed,
    Failed
}
