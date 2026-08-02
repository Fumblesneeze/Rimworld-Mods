using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using LudeonTK;
using Verse;

namespace RimWorldDevGateway;

public enum GatewayGameSpeed
{
    Paused,
    Normal,
    Fast,
    Superfast,
    Ultrafast
}

public interface IGatewaySemanticActionOperations
{
    GatewaySemanticActionAvailability GetGameAvailability();

    GatewaySemanticActionAvailability GetWindowAvailability();

    GatewaySemanticActionAvailability GetDebugToolAvailability();

    bool IsPaused { get; }

    bool ForcePaused { get; }

    GatewayGameSpeed CurrentSpeed { get; }

    string? ActiveWindowType { get; }

    bool DebugToolActive { get; }

    void SetPaused(bool paused);

    void SetSpeed(GatewayGameSpeed speed);

    void AcceptWindow();

    void CancelWindow();

    void CancelDebugTool();
}

/// <summary>
/// Performs live RimWorld operations. Callers must invoke this adapter through the main-thread dispatcher.
/// </summary>
public sealed class VerseGatewaySemanticActionOperations : IGatewaySemanticActionOperations
{
    private const string GameUnavailableReason = "No active RimWorld game is available.";
    private const string WindowUnavailableReason = "No active RimWorld window is available.";
    private const string DebugToolUnavailableReason = "No native RimWorld debug tool is active.";

    public GatewaySemanticActionAvailability GetGameAvailability() =>
        Current.Game?.tickManager is null
            ? GatewaySemanticActionAvailability.Unavailable(GameUnavailableReason)
            : GatewaySemanticActionAvailability.Available();

    public GatewaySemanticActionAvailability GetWindowAvailability() =>
        GetActiveWindow() is null
            ? GatewaySemanticActionAvailability.Unavailable(WindowUnavailableReason)
            : GatewaySemanticActionAvailability.Available();

    public GatewaySemanticActionAvailability GetDebugToolAvailability() =>
        DebugToolActive
            ? GatewaySemanticActionAvailability.Available()
            : GatewaySemanticActionAvailability.Unavailable(DebugToolUnavailableReason);

    public bool IsPaused => RequireTickManager().Paused;

    public bool ForcePaused => RequireTickManager().ForcePaused;

    public GatewayGameSpeed CurrentSpeed => FromVerseSpeed(RequireTickManager().CurTimeSpeed);

    public string? ActiveWindowType
    {
        get
        {
            var window = GetActiveWindow();
            return window is null ? null : window.GetType().FullName ?? window.GetType().Name;
        }
    }

    public bool DebugToolActive => DebugTools.curTool is not null;

    public void SetPaused(bool paused)
    {
        var tickManager = RequireTickManager();
        if (paused)
        {
            tickManager.Pause();
            return;
        }

        if (tickManager.ForcePaused)
        {
            throw new InvalidOperationException(
                "RimWorld is force-paused by an active window or game state.");
        }

        if (tickManager.Paused)
        {
            tickManager.TogglePaused();
        }
    }

    public void SetSpeed(GatewayGameSpeed speed)
    {
        var tickManager = RequireTickManager();
        if (tickManager.ForcePaused && speed != GatewayGameSpeed.Paused)
        {
            throw new InvalidOperationException(
                "RimWorld is force-paused by an active window or game state.");
        }

        tickManager.CurTimeSpeed = ToVerseSpeed(speed);
    }

    public void AcceptWindow()
    {
        RequireWindow().OnAcceptKeyPressed();
    }

    public void CancelWindow()
    {
        RequireWindow().OnCancelKeyPressed();
    }

    public void CancelDebugTool()
    {
        if (!DebugToolActive)
        {
            throw new InvalidOperationException(DebugToolUnavailableReason);
        }

        DebugTools.curTool = null;
    }

    private static TickManager RequireTickManager() =>
        Current.Game?.tickManager ?? throw new InvalidOperationException(GameUnavailableReason);

    private static Window RequireWindow() =>
        GetActiveWindow() ?? throw new InvalidOperationException(WindowUnavailableReason);

    private static Window? GetActiveWindow()
    {
        if (Current.Root is null)
        {
            return null;
        }

        var windows = Find.WindowStack?.Windows;
        return windows is null || windows.Count == 0 ? null : windows[windows.Count - 1];
    }

    private static GatewayGameSpeed FromVerseSpeed(TimeSpeed speed)
    {
        switch (speed)
        {
            case TimeSpeed.Paused:
                return GatewayGameSpeed.Paused;
            case TimeSpeed.Normal:
                return GatewayGameSpeed.Normal;
            case TimeSpeed.Fast:
                return GatewayGameSpeed.Fast;
            case TimeSpeed.Superfast:
                return GatewayGameSpeed.Superfast;
            case TimeSpeed.Ultrafast:
                return GatewayGameSpeed.Ultrafast;
            default:
                throw new ArgumentOutOfRangeException(nameof(speed));
        }
    }

    private static TimeSpeed ToVerseSpeed(GatewayGameSpeed speed)
    {
        switch (speed)
        {
            case GatewayGameSpeed.Paused:
                return TimeSpeed.Paused;
            case GatewayGameSpeed.Normal:
                return TimeSpeed.Normal;
            case GatewayGameSpeed.Fast:
                return TimeSpeed.Fast;
            case GatewayGameSpeed.Superfast:
                return TimeSpeed.Superfast;
            case GatewayGameSpeed.Ultrafast:
                return TimeSpeed.Ultrafast;
            default:
                throw new ArgumentOutOfRangeException(nameof(speed));
        }
    }
}

public sealed class GatewaySemanticActionAvailability
{
    private GatewaySemanticActionAvailability(bool available, string? unavailableReason)
    {
        IsAvailable = available;
        UnavailableReason = unavailableReason;
    }

    public bool IsAvailable { get; }

    public string? UnavailableReason { get; }

    public static GatewaySemanticActionAvailability Available() => new(true, null);

    public static GatewaySemanticActionAvailability Unavailable(string reason)
    {
        if (string.IsNullOrWhiteSpace(reason))
        {
            throw new ArgumentException("An unavailable reason is required.", nameof(reason));
        }

        return new GatewaySemanticActionAvailability(false, reason);
    }
}

public sealed class GatewaySemanticActionArgumentDescriptor
{
    public GatewaySemanticActionArgumentDescriptor(
        string name,
        string type,
        bool required,
        string description,
        IEnumerable<string>? allowedValues = null)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Type = type ?? throw new ArgumentNullException(nameof(type));
        Required = required;
        Description = description ?? throw new ArgumentNullException(nameof(description));
        AllowedValues = new ReadOnlyCollection<string>(
            (allowedValues ?? Array.Empty<string>()).ToArray());
    }

    public string Name { get; }

    public string Type { get; }

    public bool Required { get; }

    public string Description { get; }

    public IReadOnlyList<string> AllowedValues { get; }
}

public sealed class GatewaySemanticActionDescriptor
{
    public GatewaySemanticActionDescriptor(
        string name,
        string version,
        string description,
        IReadOnlyList<GatewaySemanticActionArgumentDescriptor> argumentSchema,
        GatewaySemanticActionAvailability availability)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        if (argumentSchema is null)
        {
            throw new ArgumentNullException(nameof(argumentSchema));
        }

        ArgumentSchema = new ReadOnlyCollection<GatewaySemanticActionArgumentDescriptor>(
            argumentSchema.ToArray());
        if (availability is null)
        {
            throw new ArgumentNullException(nameof(availability));
        }

        Available = availability.IsAvailable;
        UnavailableReason = availability.UnavailableReason;
    }

    public string Name { get; }

    public string Version { get; }

    public string Description { get; }

    public IReadOnlyList<GatewaySemanticActionArgumentDescriptor> ArgumentSchema { get; }

    public bool Available { get; }

    public string? UnavailableReason { get; }
}

public sealed class GatewaySemanticActionResult
{
    public GatewaySemanticActionResult(string resolvedAction, object? before, object? after)
    {
        ResolvedAction = resolvedAction ?? throw new ArgumentNullException(nameof(resolvedAction));
        Before = before;
        After = after;
    }

    public string ResolvedAction { get; }

    public object? Before { get; }

    public object? After { get; }
}

public sealed class GatewaySemanticActionError
{
    public GatewaySemanticActionError(
        string code,
        string message,
        bool retryable = false,
        IReadOnlyDictionary<string, object?>? details = null)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Message = message ?? throw new ArgumentNullException(nameof(message));
        Retryable = retryable;
        if (details is not null)
        {
            var copy = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (var detail in details)
            {
                copy.Add(detail.Key, detail.Value);
            }

            Details = new ReadOnlyDictionary<string, object?>(copy);
        }
    }

    public string Code { get; }

    public string Message { get; }

    public bool Retryable { get; }

    public IReadOnlyDictionary<string, object?>? Details { get; }
}

public sealed class GatewaySemanticActionInvocationResult
{
    private GatewaySemanticActionInvocationResult(
        string action,
        string version,
        GatewaySemanticActionResult? result,
        GatewaySemanticActionError? error)
    {
        Action = action ?? throw new ArgumentNullException(nameof(action));
        Version = version ?? throw new ArgumentNullException(nameof(version));
        Result = result;
        Error = error;
    }

    public string Action { get; }

    public string Version { get; }

    public bool Ok => Error is null;

    public GatewaySemanticActionResult? Result { get; }

    public GatewaySemanticActionError? Error { get; }

    public static GatewaySemanticActionInvocationResult Success(
        string action,
        string version,
        GatewaySemanticActionResult result) =>
        new(action, version, result ?? throw new ArgumentNullException(nameof(result)), null);

    public static GatewaySemanticActionInvocationResult Failure(
        string action,
        string version,
        GatewaySemanticActionError error) =>
        new(action, version, null, error ?? throw new ArgumentNullException(nameof(error)));
}

public sealed class GatewaySemanticActionRegistry
{
    private const string ActionVersion = "1";
    private static readonly IReadOnlyList<ActionDefinition> Definitions = CreateDefinitions();

    private readonly GatewayDispatcher dispatcher;
    private readonly IGatewaySemanticActionOperations operations;
    private readonly TimeSpan dispatchTimeout;
    private readonly GatewayLogBuffer? diagnostics;

    public GatewaySemanticActionRegistry(
        GatewayDispatcher dispatcher,
        IGatewaySemanticActionOperations operations,
        TimeSpan? dispatchTimeout = null,
        GatewayLogBuffer? diagnostics = null)
    {
        this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        this.operations = operations ?? throw new ArgumentNullException(nameof(operations));
        this.dispatchTimeout = dispatchTimeout ?? TimeSpan.FromSeconds(15);
        this.diagnostics = diagnostics;
        if (this.dispatchTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(dispatchTimeout));
        }
    }

    public Task<IReadOnlyList<GatewaySemanticActionDescriptor>> DiscoverAsync(
        string requestId,
        CancellationToken cancellationToken = default)
    {
        return DiscoverOperation(requestId, cancellationToken).Completion;
    }

    public GatewayDispatchOperation<IReadOnlyList<GatewaySemanticActionDescriptor>> DiscoverOperation(
        string requestId,
        CancellationToken cancellationToken = default)
    {
        return dispatcher.EnqueueOperation(
            requestId,
            "semantic-actions.discover",
            dispatchTimeout,
            _ => DescribeActions(),
            cancellationToken: cancellationToken);
    }

    public Task<GatewaySemanticActionInvocationResult> InvokeAsync(
        string requestId,
        string action,
        IReadOnlyDictionary<string, object?>? arguments = null,
        CancellationToken cancellationToken = default)
    {
        return InvokeOperation(requestId, action, arguments, cancellationToken).Completion;
    }

    public GatewayDispatchOperation<GatewaySemanticActionInvocationResult> InvokeOperation(
        string requestId,
        string action,
        IReadOnlyDictionary<string, object?>? arguments = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new ArgumentException("A request ID is required.", nameof(requestId));
        }

        if (string.Equals(action, "game.speed", StringComparison.Ordinal))
        {
            ReadRequiredSpeed(arguments, out var speed, out var speedError);
            if (speedError is not null)
            {
                return Completed(GatewaySemanticActionInvocationResult.Failure(
                    action,
                    ActionVersion,
                    speedError));
            }

            return dispatcher.EnqueueOperation(
                requestId,
                action,
                dispatchTimeout,
                _ => InvokeSafely(requestId, action, () => InvokeSpeed(action, speed)),
                cancellationToken: cancellationToken);
        }

        if (string.Equals(action, "window.accept", StringComparison.Ordinal) ||
            string.Equals(action, "window.cancel", StringComparison.Ordinal) ||
            string.Equals(action, "debug.tool.cancel", StringComparison.Ordinal))
        {
            if (arguments is not null && arguments.Count != 0)
            {
                return Completed(GatewaySemanticActionInvocationResult.Failure(
                    action,
                    ActionVersion,
                    new GatewaySemanticActionError(
                        "invalid_argument",
                        $"Action '{action}' does not accept arguments.")));
            }

            return dispatcher.EnqueueOperation(
                requestId,
                action,
                dispatchTimeout,
                _ => InvokeSafely(
                    requestId,
                    action,
                    () => string.Equals(action, "debug.tool.cancel", StringComparison.Ordinal)
                        ? InvokeDebugToolCancel(action)
                        : InvokeWindow(action)),
                cancellationToken: cancellationToken);
        }

        if (!string.Equals(action, "game.pause", StringComparison.Ordinal))
        {
            return Completed(GatewaySemanticActionInvocationResult.Failure(
                action ?? string.Empty,
                ActionVersion,
                new GatewaySemanticActionError(
                    "action_not_found",
                    $"No semantic action named '{action}' is registered.")));
        }

        var pauseArgument = ReadOptionalBoolean(arguments, "paused", out var argumentError);
        if (argumentError is not null)
        {
            return Completed(GatewaySemanticActionInvocationResult.Failure(
                action,
                ActionVersion,
                argumentError));
        }

        return dispatcher.EnqueueOperation(
            requestId,
            action,
            dispatchTimeout,
            _ => InvokeSafely(requestId, action, () => InvokePause(action, pauseArgument)),
            cancellationToken: cancellationToken);
    }

    private static GatewayDispatchOperation<GatewaySemanticActionInvocationResult> Completed(
        GatewaySemanticActionInvocationResult result) =>
        new(Task.FromResult(result));

    private GatewaySemanticActionInvocationResult InvokeSafely(
        string requestId,
        string action,
        Func<GatewaySemanticActionInvocationResult> invocation)
    {
        try
        {
            return invocation();
        }
        catch (Exception exception)
        {
            diagnostics?.Append(
                "Error",
                $"Semantic action '{action}' failed.",
                exception.ToString(),
                requestId: requestId);
            var message = string.IsNullOrWhiteSpace(exception.Message)
                ? exception.GetType().Name
                : exception.Message;
            if (message.Length > 512)
            {
                message = message.Substring(0, 512);
            }

            var details = new ReadOnlyDictionary<string, object?>(
                new Dictionary<string, object?>
                {
                    ["exceptionType"] = exception.GetType().FullName ?? exception.GetType().Name
                });
            return GatewaySemanticActionInvocationResult.Failure(
                action,
                ActionVersion,
                new GatewaySemanticActionError(
                    "action_failed",
                    $"Semantic action '{action}' failed: {message}",
                    details: details));
        }
    }

    private GatewaySemanticActionInvocationResult InvokePause(string action, bool? requestedPause)
    {
        var availability = operations.GetGameAvailability();
        if (!availability.IsAvailable)
        {
            return GatewaySemanticActionInvocationResult.Failure(
                action,
                ActionVersion,
                new GatewaySemanticActionError(
                    "action_unavailable",
                    availability.UnavailableReason ?? "The action is unavailable."));
        }

        var before = operations.IsPaused;
        var desiredPause = requestedPause ?? !before;
        if (!desiredPause && operations.ForcePaused)
        {
            return GatewaySemanticActionInvocationResult.Failure(
                action,
                ActionVersion,
                new GatewaySemanticActionError(
                    "game_force_paused",
                    "RimWorld is force-paused and cannot currently be unpaused."));
        }

        operations.SetPaused(desiredPause);
        return GatewaySemanticActionInvocationResult.Success(
            action,
            ActionVersion,
            new GatewaySemanticActionResult(
                requestedPause.HasValue ? "game.pause.set" : "game.pause.toggle",
                before,
                operations.IsPaused));
    }

    private GatewaySemanticActionInvocationResult InvokeWindow(string action)
    {
        var availability = operations.GetWindowAvailability();
        if (!availability.IsAvailable)
        {
            return GatewaySemanticActionInvocationResult.Failure(
                action,
                ActionVersion,
                new GatewaySemanticActionError(
                    "action_unavailable",
                    availability.UnavailableReason ?? "The action is unavailable."));
        }

        var before = operations.ActiveWindowType;
        if (string.Equals(action, "window.accept", StringComparison.Ordinal))
        {
            operations.AcceptWindow();
        }
        else
        {
            operations.CancelWindow();
        }

        return GatewaySemanticActionInvocationResult.Success(
            action,
            ActionVersion,
            new GatewaySemanticActionResult(action, before, operations.ActiveWindowType));
    }

    private GatewaySemanticActionInvocationResult InvokeDebugToolCancel(string action)
    {
        var availability = operations.GetDebugToolAvailability();
        if (!availability.IsAvailable)
        {
            return GatewaySemanticActionInvocationResult.Failure(
                action,
                ActionVersion,
                new GatewaySemanticActionError(
                    "action_unavailable",
                    availability.UnavailableReason ?? "The action is unavailable."));
        }

        var before = operations.DebugToolActive;
        operations.CancelDebugTool();
        return GatewaySemanticActionInvocationResult.Success(
            action,
            ActionVersion,
            new GatewaySemanticActionResult(
                action,
                before,
                operations.DebugToolActive));
    }

    private GatewaySemanticActionInvocationResult InvokeSpeed(
        string action,
        GatewayGameSpeed requestedSpeed)
    {
        var availability = operations.GetGameAvailability();
        if (!availability.IsAvailable)
        {
            return GatewaySemanticActionInvocationResult.Failure(
                action,
                ActionVersion,
                new GatewaySemanticActionError(
                    "action_unavailable",
                    availability.UnavailableReason ?? "The action is unavailable."));
        }

        var before = SpeedName(operations.CurrentSpeed);
        if (requestedSpeed != GatewayGameSpeed.Paused && operations.ForcePaused)
        {
            return GatewaySemanticActionInvocationResult.Failure(
                action,
                ActionVersion,
                new GatewaySemanticActionError(
                    "game_force_paused",
                    "RimWorld is force-paused and cannot currently change to a running speed."));
        }

        operations.SetSpeed(requestedSpeed);
        return GatewaySemanticActionInvocationResult.Success(
            action,
            ActionVersion,
            new GatewaySemanticActionResult(
                "game.speed.set",
                before,
                SpeedName(operations.CurrentSpeed)));
    }

    private static bool? ReadOptionalBoolean(
        IReadOnlyDictionary<string, object?>? arguments,
        string name,
        out GatewaySemanticActionError? error)
    {
        error = null;
        if (arguments is null || arguments.Count == 0)
        {
            return null;
        }

        if (arguments.Count != 1 || !arguments.TryGetValue(name, out var value))
        {
            error = new GatewaySemanticActionError(
                "invalid_argument",
                $"Action 'game.pause' accepts only the optional boolean argument '{name}'.");
            return null;
        }

        if (value is bool boolean)
        {
            return boolean;
        }

        error = new GatewaySemanticActionError(
            "invalid_argument",
            $"Argument '{name}' must be a boolean.");
        return null;
    }

    private static void ReadRequiredSpeed(
        IReadOnlyDictionary<string, object?>? arguments,
        out GatewayGameSpeed speed,
        out GatewaySemanticActionError? error)
    {
        speed = default;
        error = null;
        if (arguments is null ||
            arguments.Count != 1 ||
            !arguments.TryGetValue("speed", out var value))
        {
            error = new GatewaySemanticActionError(
                "invalid_argument",
                "Action 'game.speed' requires exactly one string argument named 'speed'.");
            return;
        }

        if (value is not string text)
        {
            error = new GatewaySemanticActionError(
                "invalid_argument",
                "Argument 'speed' must be a string.");
            return;
        }

        switch (text)
        {
            case "paused":
                speed = GatewayGameSpeed.Paused;
                return;
            case "normal":
                speed = GatewayGameSpeed.Normal;
                return;
            case "fast":
                speed = GatewayGameSpeed.Fast;
                return;
            case "superfast":
                speed = GatewayGameSpeed.Superfast;
                return;
            case "ultrafast":
                speed = GatewayGameSpeed.Ultrafast;
                return;
            default:
                error = new GatewaySemanticActionError(
                    "invalid_argument",
                    "Argument 'speed' must be one of: paused, normal, fast, superfast, ultrafast.");
                return;
        }
    }

    private static string SpeedName(GatewayGameSpeed speed)
    {
        switch (speed)
        {
            case GatewayGameSpeed.Paused:
                return "paused";
            case GatewayGameSpeed.Normal:
                return "normal";
            case GatewayGameSpeed.Fast:
                return "fast";
            case GatewayGameSpeed.Superfast:
                return "superfast";
            case GatewayGameSpeed.Ultrafast:
                return "ultrafast";
            default:
                throw new ArgumentOutOfRangeException(nameof(speed));
        }
    }

    private IReadOnlyList<GatewaySemanticActionDescriptor> DescribeActions()
    {
        var descriptors = new List<GatewaySemanticActionDescriptor>(Definitions.Count);
        foreach (var definition in Definitions)
        {
            descriptors.Add(new GatewaySemanticActionDescriptor(
                definition.Name,
                ActionVersion,
                definition.Description,
                definition.ArgumentSchema,
                definition.GetAvailability(operations)));
        }

        return new ReadOnlyCollection<GatewaySemanticActionDescriptor>(descriptors);
    }

    private static IReadOnlyList<ActionDefinition> CreateDefinitions()
    {
        var paused = new GatewaySemanticActionArgumentDescriptor(
            "paused",
            "boolean",
            required: false,
            "Set the pause state; omit to toggle it.");
        var speed = new GatewaySemanticActionArgumentDescriptor(
            "speed",
            "string",
            required: true,
            "The requested RimWorld time speed.",
            new[] { "paused", "normal", "fast", "superfast", "ultrafast" });

        return new ReadOnlyCollection<ActionDefinition>(new[]
        {
            new ActionDefinition(
                "game.pause",
                "Pause, resume, or toggle the current game.",
                new[] { paused },
                runtime => runtime.GetGameAvailability()),
            new ActionDefinition(
                "game.speed",
                "Set the current game speed.",
                new[] { speed },
                runtime => runtime.GetGameAvailability()),
            new ActionDefinition(
                "window.accept",
                "Invoke the accept handler on the topmost RimWorld window.",
                Array.Empty<GatewaySemanticActionArgumentDescriptor>(),
                runtime => runtime.GetWindowAvailability()),
            new ActionDefinition(
                "window.cancel",
                "Invoke the cancel handler on the topmost RimWorld window.",
                Array.Empty<GatewaySemanticActionArgumentDescriptor>(),
                runtime => runtime.GetWindowAvailability()),
            new ActionDefinition(
                "debug.tool.cancel",
                "Cancel the currently active native RimWorld debug pointer tool.",
                Array.Empty<GatewaySemanticActionArgumentDescriptor>(),
                runtime => runtime.GetDebugToolAvailability())
        });
    }

    private sealed class ActionDefinition
    {
        private readonly Func<IGatewaySemanticActionOperations, GatewaySemanticActionAvailability>
            getAvailability;

        public ActionDefinition(
            string name,
            string description,
            IReadOnlyList<GatewaySemanticActionArgumentDescriptor> argumentSchema,
            Func<IGatewaySemanticActionOperations, GatewaySemanticActionAvailability> getAvailability)
        {
            Name = name;
            Description = description;
            ArgumentSchema = argumentSchema;
            this.getAvailability = getAvailability;
        }

        public string Name { get; }

        public string Description { get; }

        public IReadOnlyList<GatewaySemanticActionArgumentDescriptor> ArgumentSchema { get; }

        public GatewaySemanticActionAvailability GetAvailability(
            IGatewaySemanticActionOperations runtime) => getAvailability(runtime);
    }
}
