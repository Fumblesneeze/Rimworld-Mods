using System.Collections.ObjectModel;

namespace RimWorldDevGateway;

public sealed class GatewayEndToEndExecutionSnapshot
{
    public GatewayEndToEndExecutionSnapshot(
        string groupState,
        GatewayEndToEndTestExecutionSnapshot? currentTest = null,
        IEnumerable<GatewayEndToEndTestExecutionSnapshot>? results = null,
        bool processTainted = false)
    {
        GroupState = string.IsNullOrWhiteSpace(groupState)
            ? throw new ArgumentException("An E2E group state is required.", nameof(groupState))
            : groupState;
        CurrentTest = currentTest;
        Results = new ReadOnlyCollection<GatewayEndToEndTestExecutionSnapshot>(
            (results ?? Array.Empty<GatewayEndToEndTestExecutionSnapshot>()).ToArray());
        ProcessTainted = processTainted;
    }

    public string GroupState { get; }

    public GatewayEndToEndTestExecutionSnapshot? CurrentTest { get; }

    public GatewayEndToEndStepExecutionSnapshot? CurrentStep => CurrentTest?.CurrentStep;

    public IReadOnlyList<GatewayEndToEndTestExecutionSnapshot> Results { get; }

    public bool ProcessTainted { get; }

    public bool IsTerminal =>
        GroupState == "completed" ||
        GroupState == "completed_with_failures" ||
        GroupState == "infrastructure_failed" ||
        GroupState == "aborted";
}

public sealed class GatewayEndToEndTestExecutionSnapshot
{
    internal GatewayEndToEndTestExecutionSnapshot(
        string id,
        string status,
        long startedFrame,
        int startedGameTick,
        DateTimeOffset startedUtc,
        long? completedFrame,
        int? completedGameTick,
        DateTimeOffset? completedUtc,
        string cleanupState,
        GatewayEndToEndStepExecutionSnapshot? currentStep,
        IEnumerable<GatewayEndToEndStepExecutionSnapshot> steps,
        GatewayEndToEndExecutionFailureSnapshot? failure,
        GatewayEndToEndExecutionFailureSnapshot? cleanupFailure)
    {
        Id = id;
        Status = status;
        StartedFrame = startedFrame;
        StartedGameTick = startedGameTick;
        StartedUtc = startedUtc;
        CompletedFrame = completedFrame;
        CompletedGameTick = completedGameTick;
        CompletedUtc = completedUtc;
        CleanupState = cleanupState;
        CurrentStep = currentStep;
        Steps = new ReadOnlyCollection<GatewayEndToEndStepExecutionSnapshot>(steps.ToArray());
        Failure = failure;
        CleanupFailure = cleanupFailure;
    }

    public string Id { get; }
    public string Status { get; }
    public long StartedFrame { get; }
    public int StartedGameTick { get; }
    public DateTimeOffset StartedUtc { get; }
    public long? CompletedFrame { get; }
    public int? CompletedGameTick { get; }
    public DateTimeOffset? CompletedUtc { get; }
    public string CleanupState { get; }
    public GatewayEndToEndStepExecutionSnapshot? CurrentStep { get; }
    public IReadOnlyList<GatewayEndToEndStepExecutionSnapshot> Steps { get; }
    public GatewayEndToEndExecutionFailureSnapshot? Failure { get; }
    public GatewayEndToEndExecutionFailureSnapshot? CleanupFailure { get; }
}

public sealed class GatewayEndToEndStepExecutionSnapshot
{
    internal GatewayEndToEndStepExecutionSnapshot(
        string name,
        string kind,
        string status,
        long startedFrame,
        int startedGameTick,
        DateTimeOffset startedUtc,
        long? completedFrame,
        int? completedGameTick,
        DateTimeOffset? completedUtc,
        IReadOnlyDictionary<string, string>? artifacts)
    {
        Name = name;
        Kind = kind;
        Status = status;
        StartedFrame = startedFrame;
        StartedGameTick = startedGameTick;
        StartedUtc = startedUtc;
        CompletedFrame = completedFrame;
        CompletedGameTick = completedGameTick;
        CompletedUtc = completedUtc;
        Artifacts = new ReadOnlyDictionary<string, string>(
            (artifacts ?? new Dictionary<string, string>())
            .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal));
    }

    public string Name { get; }
    public string Kind { get; }
    public string Status { get; }
    public long StartedFrame { get; }
    public int StartedGameTick { get; }
    public DateTimeOffset StartedUtc { get; }
    public long? CompletedFrame { get; }
    public int? CompletedGameTick { get; }
    public DateTimeOffset? CompletedUtc { get; }
    public IReadOnlyDictionary<string, string> Artifacts { get; }
}

public sealed class GatewayEndToEndExecutionFailureSnapshot
{
    internal GatewayEndToEndExecutionFailureSnapshot(
        string kind,
        string code,
        string message,
        string? exceptionType,
        string? stackTrace = null)
    {
        Kind = kind;
        Code = code;
        Message = message;
        ExceptionType = exceptionType;
        StackTrace = stackTrace;
    }

    public string Kind { get; }
    public string Code { get; }
    public string Message { get; }
    public string? ExceptionType { get; }
    public string? StackTrace { get; }
}
