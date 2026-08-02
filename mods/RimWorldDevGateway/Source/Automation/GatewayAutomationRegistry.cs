using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace RimWorldDevGateway;

public sealed class GatewayAutomationException : Exception
{
    public GatewayAutomationException(string code, string message) : base(message)
    {
        Code = code;
    }

    public GatewayAutomationException(string code, string message, Exception innerException) : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}

public sealed class GatewayAutomationAvailability
{
    public GatewayAutomationAvailability(bool available, string? reason = null)
    {
        Available = available;
        Reason = available ? null : reason ?? "Unavailable.";
    }

    public bool Available { get; }

    public string? Reason { get; }

    public static GatewayAutomationAvailability AvailableNow { get; } = new(true);
}

public sealed class GatewayAutomationDescriptor
{
    public GatewayAutomationDescriptor(
        string name,
        string version,
        string description,
        IReadOnlyDictionary<string, object?> argumentSchema,
        IEnumerable<string>? prerequisites,
        bool mutating,
        GatewayAutomationAvailability? availability = null,
        bool sessionScoped = false)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length > 128)
        {
            throw new ArgumentException("An automation name of at most 128 characters is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(version) || version.Length > 64)
        {
            throw new ArgumentException("An automation version of at most 64 characters is required.", nameof(version));
        }

        if (string.IsNullOrWhiteSpace(description) || description.Length > 2_000)
        {
            throw new ArgumentException("An automation description of at most 2,000 characters is required.", nameof(description));
        }

        Name = name;
        Version = version;
        Description = description;
        if (argumentSchema is null)
        {
            throw new ArgumentNullException(nameof(argumentSchema));
        }

        var schemaCopy = new SortedDictionary<string, object?>(StringComparer.Ordinal);
        foreach (var entry in argumentSchema)
        {
            schemaCopy.Add(entry.Key, entry.Value);
        }

        ArgumentSchema = new ReadOnlyDictionary<string, object?>(schemaCopy);
        Prerequisites = new ReadOnlyCollection<string>((prerequisites ?? Array.Empty<string>()).ToArray());
        Mutating = mutating;
        Availability = availability ?? GatewayAutomationAvailability.AvailableNow;
        SessionScoped = sessionScoped;
    }

    public string Name { get; }

    public string Version { get; }

    public string Description { get; }

    public IReadOnlyDictionary<string, object?> ArgumentSchema { get; }

    public IReadOnlyList<string> Prerequisites { get; }

    public bool Mutating { get; }

    public bool SessionScoped { get; }

    public GatewayAutomationAvailability Availability { get; }

    internal GatewayAutomationDescriptor Snapshot(GatewayAutomationAvailability availability, bool sessionScoped) =>
        new(Name, Version, Description, ArgumentSchema, Prerequisites, Mutating, availability, sessionScoped);
}

public sealed class GatewayAutomationProgress
{
    internal GatewayAutomationProgress(long sequence, DateTimeOffset timestampUtc, string step, string message, double? fraction)
    {
        Sequence = sequence;
        TimestampUtc = timestampUtc;
        Step = step;
        Message = message;
        Fraction = fraction;
    }

    public long Sequence { get; }
    public DateTimeOffset TimestampUtc { get; }
    public string Step { get; }
    public string Message { get; }
    public double? Fraction { get; }
}

public sealed class GatewayAutomationArtifact
{
    public GatewayAutomationArtifact(string name, string kind, object? value)
    {
        Name = string.IsNullOrWhiteSpace(name) ? throw new ArgumentException("An artifact name is required.", nameof(name)) : name;
        Kind = string.IsNullOrWhiteSpace(kind) ? throw new ArgumentException("An artifact kind is required.", nameof(kind)) : kind;
        Value = value;
    }

    public string Name { get; }
    public string Kind { get; }
    public object? Value { get; }
}

public sealed class GatewayAutomationError
{
    internal GatewayAutomationError(string code, string message)
    {
        Code = code;
        Message = message;
    }

    public string Code { get; }
    public string Message { get; }
}

public sealed class GatewayAutomationRun
{
    private readonly object sync = new();
    private readonly int maximumProgress;
    private readonly int maximumArtifacts;
    private readonly List<GatewayAutomationProgress> progress = new();
    private readonly List<GatewayAutomationArtifact> artifacts = new();
    private readonly CancellationTokenSource cancellation = new();
    private long progressSequence;
    private string state = "queued";

    internal GatewayAutomationRun(
        string runId,
        GatewayAutomationDescriptor descriptor,
        string requestId,
        string? idempotencyKey,
        string argumentFingerprint,
        DateTimeOffset createdUtc,
        DateTimeOffset? deadlineUtc,
        int maximumProgress,
        int maximumArtifacts)
    {
        RunId = runId;
        Name = descriptor.Name;
        Version = descriptor.Version;
        RequestId = requestId;
        IdempotencyKey = idempotencyKey;
        ArgumentFingerprint = argumentFingerprint;
        CreatedUtc = createdUtc;
        DeadlineUtc = deadlineUtc;
        this.maximumProgress = maximumProgress;
        this.maximumArtifacts = maximumArtifacts;
    }

    public string RunId { get; }
    public string Name { get; }
    public string Version { get; }
    public string RequestId { get; }
    public string? IdempotencyKey { get; }
    public DateTimeOffset CreatedUtc { get; }
    public DateTimeOffset? DeadlineUtc { get; }
    public DateTimeOffset? StartedUtc { get; private set; }
    public DateTimeOffset? CompletedUtc { get; private set; }
    public object? Result { get; private set; }
    public GatewayAutomationError? Error { get; private set; }
    public int ProgressEvicted { get; private set; }
    public int ArtifactsEvicted { get; private set; }

    public string State { get { lock (sync) { return state; } } }

    public IReadOnlyList<GatewayAutomationProgress> Progress
    {
        get { lock (sync) { return new ReadOnlyCollection<GatewayAutomationProgress>(progress.ToArray()); } }
    }

    public IReadOnlyList<GatewayAutomationArtifact> Artifacts
    {
        get { lock (sync) { return new ReadOnlyCollection<GatewayAutomationArtifact>(artifacts.ToArray()); } }
    }

    internal string ArgumentFingerprint { get; }
    internal CancellationToken CancellationToken => cancellation.Token;
    internal bool IsTerminal => State is "succeeded" or "failed" or "cancelled" or "timed-out";

    internal void MarkRunning(DateTimeOffset now)
    {
        lock (sync) { state = "running"; StartedUtc = now; }
    }

    internal void Complete(string terminalState, DateTimeOffset now, object? result = null, GatewayAutomationError? error = null)
    {
        lock (sync)
        {
            state = terminalState;
            Result = result;
            Error = error;
            CompletedUtc = now;
        }
    }

    internal bool RequestCancellation()
    {
        lock (sync)
        {
            if (IsTerminal)
            {
                return false;
            }

            state = "cancel-requested";
            cancellation.Cancel();
            return true;
        }
    }

    internal void AddProgress(DateTimeOffset now, string step, string message, double? fraction)
    {
        lock (sync)
        {
            if (progress.Count == maximumProgress)
            {
                progress.RemoveAt(0);
                ProgressEvicted++;
            }

            progress.Add(new GatewayAutomationProgress(++progressSequence, now, step, message, fraction));
        }
    }

    internal void AddArtifact(GatewayAutomationArtifact artifact)
    {
        lock (sync)
        {
            if (artifacts.Count == maximumArtifacts)
            {
                artifacts.RemoveAt(0);
                ArtifactsEvicted++;
            }

            artifacts.Add(artifact);
        }
    }

    internal void ThrowIfStopRequested(DateTimeOffset now)
    {
        CancellationToken.ThrowIfCancellationRequested();
        if (DeadlineUtc.HasValue && now >= DeadlineUtc.Value)
        {
            throw new GatewayAutomationTimeoutException();
        }
    }
}

internal sealed class GatewayAutomationTimeoutException : Exception
{
}

public sealed class GatewayAutomationContext
{
    private readonly GatewayAutomationRun run;
    private readonly Func<DateTimeOffset> utcNow;

    internal GatewayAutomationContext(GatewayAutomationRun run, Func<DateTimeOffset> utcNow)
    {
        this.run = run;
        this.utcNow = utcNow;
        CallerThreadId = Thread.CurrentThread.ManagedThreadId;
    }

    public string RunId => run.RunId;
    public string RequestId => run.RequestId;
    public int CallerThreadId { get; }
    public CancellationToken CancellationToken => run.CancellationToken;

    public void ReportProgress(string step, string message, double? fraction = null)
    {
        if (string.IsNullOrWhiteSpace(step) || string.IsNullOrWhiteSpace(message) ||
            (fraction.HasValue && (fraction < 0 || fraction > 1)))
        {
            throw new GatewayAutomationException("invalid_progress", "Automation progress is invalid.");
        }

        run.AddProgress(utcNow(), step, message, fraction);
    }

    public void AddArtifact(string name, string kind, object? value)
    {
        run.AddArtifact(new GatewayAutomationArtifact(name, kind, value));
    }

    public T RunStep<T>(string step, Func<T> action)
    {
        if (string.IsNullOrWhiteSpace(step) || action is null)
        {
            throw new GatewayAutomationException("invalid_step", "An automation step and action are required.");
        }

        run.ThrowIfStopRequested(utcNow());
        var result = action();
        run.ThrowIfStopRequested(utcNow());
        return result;
    }

    public void RunStep(string step, Action action)
    {
        if (action is null)
        {
            throw new GatewayAutomationException("invalid_step", "An automation step and action are required.");
        }

        RunStep(step, () => { action(); return true; });
    }
}

public delegate object? GatewayAutomationHandler(
    GatewayAutomationContext context,
    IReadOnlyDictionary<string, object?> arguments);

public sealed class GatewayAutomationRegistry
{
    private readonly object sync = new();
    private readonly Dictionary<string, Registration> registrations = new(StringComparer.Ordinal);
    private readonly Dictionary<string, GatewayAutomationRun> runs = new(StringComparer.Ordinal);
    private readonly LinkedList<string> history = new();
    private readonly Dictionary<string, GatewayAutomationRun> idempotency = new(StringComparer.Ordinal);
    private readonly int historyCapacity;
    private readonly int maximumProgress;
    private readonly int maximumArtifacts;
    private readonly Func<DateTimeOffset> utcNow;
    private readonly Func<string> runIdFactory;
    private readonly GatewayLogBuffer? diagnostics;

    public GatewayAutomationRegistry(
        int historyCapacity = 128,
        int maximumProgress = 256,
        int maximumArtifacts = 64,
        Func<DateTimeOffset>? utcNow = null,
        Func<string>? runIdFactory = null,
        GatewayLogBuffer? diagnostics = null)
    {
        if (historyCapacity <= 0 || maximumProgress <= 0 || maximumArtifacts <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(historyCapacity));
        }

        this.historyCapacity = historyCapacity;
        this.maximumProgress = maximumProgress;
        this.maximumArtifacts = maximumArtifacts;
        this.utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        this.runIdFactory = runIdFactory ?? (() => Guid.NewGuid().ToString("N"));
        this.diagnostics = diagnostics;
    }

    public void RegisterBuiltIn(
        GatewayAutomationDescriptor descriptor,
        GatewayAutomationHandler handler,
        Func<GatewayAutomationAvailability>? availability = null) =>
        Register(descriptor, handler, availability, sessionScoped: false);

    public void RegisterSession(
        GatewayAutomationDescriptor descriptor,
        GatewayAutomationHandler handler,
        Func<GatewayAutomationAvailability>? availability = null) =>
        Register(descriptor, handler, availability, sessionScoped: true);

    public IReadOnlyList<GatewayAutomationDescriptor> Describe()
    {
        Registration[] snapshot;
        lock (sync) { snapshot = registrations.Values.OrderBy(value => value.Descriptor.Name, StringComparer.Ordinal).ToArray(); }
        return new ReadOnlyCollection<GatewayAutomationDescriptor>(snapshot
            .Select(value => value.Descriptor.Snapshot(value.Availability(), value.SessionScoped))
            .ToArray());
    }

    public GatewayAutomationRun StartRun(
        string name,
        string requestId,
        IReadOnlyDictionary<string, object?> arguments,
        string? idempotencyKey = null,
        TimeSpan? timeout = null)
    {
        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new ArgumentException("A request ID is required.", nameof(requestId));
        }

        if (timeout.HasValue && timeout.Value <= TimeSpan.Zero)
        {
            throw Error("invalid_automation_timeout", "An automation timeout must be greater than zero.");
        }

        Registration registration;
        lock (sync)
        {
            if (!registrations.TryGetValue(name, out registration!))
            {
                throw Error("automation_not_found", $"Automation '{name}' is not registered.");
            }
        }

        var argumentSnapshot = SnapshotArguments(arguments ?? throw new ArgumentNullException(nameof(arguments)));
        var fingerprint = Fingerprint(argumentSnapshot);
        var idempotencyIndex = IdempotencyIndex(name, idempotencyKey);
        if (idempotencyIndex is not null)
        {
            lock (sync)
            {
                if (TryResolveIdempotentRun(idempotencyIndex, fingerprint, out var replay))
                {
                    return replay!;
                }
            }
        }

        var availability = registration.Availability();
        if (!availability.Available)
        {
            throw Error("automation_unavailable", availability.Reason ?? "The automation is unavailable.");
        }

        GatewayAutomationRun run;
        lock (sync)
        {
            if (idempotencyIndex is not null &&
                TryResolveIdempotentRun(idempotencyIndex, fingerprint, out var replay))
            {
                return replay!;
            }

            var runId = runIdFactory();
            if (string.IsNullOrWhiteSpace(runId) || runs.ContainsKey(runId))
            {
                throw Error("run_id_conflict", "The automation run ID is empty or already exists.");
            }

            var createdUtc = utcNow();
            DateTimeOffset? deadlineUtc;
            try
            {
                deadlineUtc = timeout.HasValue ? createdUtc.Add(timeout.Value) : null;
            }
            catch (ArgumentOutOfRangeException exception)
            {
                throw new GatewayAutomationException(
                    "invalid_automation_timeout",
                    "The automation timeout exceeds the supported date range.",
                    exception);
            }

            run = new GatewayAutomationRun(
                runId,
                registration.Descriptor,
                requestId,
                idempotencyKey,
                fingerprint,
                createdUtc,
                deadlineUtc,
                maximumProgress,
                maximumArtifacts);
            runs.Add(runId, run);
            history.AddLast(runId);
            if (idempotencyIndex is not null)
            {
                idempotency.Add(idempotencyIndex, run);
            }
        }

        Execute(registration, run, argumentSnapshot);
        TrimHistory();
        return run;
    }

    public GatewayAutomationRun GetRun(string runId)
    {
        lock (sync)
        {
            return runs.TryGetValue(runId, out var run)
                ? run
                : throw Error("automation_run_not_found", $"Automation run '{runId}' was not retained.");
        }
    }

    public IReadOnlyList<GatewayAutomationRun> ListRuns()
    {
        lock (sync)
        {
            return new ReadOnlyCollection<GatewayAutomationRun>(history.Select(runId => runs[runId]).ToArray());
        }
    }

    public bool Cancel(string runId)
    {
        return GetRun(runId).RequestCancellation();
    }

    private void Register(
        GatewayAutomationDescriptor descriptor,
        GatewayAutomationHandler handler,
        Func<GatewayAutomationAvailability>? availability,
        bool sessionScoped)
    {
        if (descriptor is null) throw new ArgumentNullException(nameof(descriptor));
        if (handler is null) throw new ArgumentNullException(nameof(handler));
        lock (sync)
        {
            if (registrations.ContainsKey(descriptor.Name))
            {
                throw Error("automation_already_registered", $"Automation '{descriptor.Name}' is already registered.");
            }

            registrations.Add(descriptor.Name, new Registration(
                descriptor,
                handler,
                availability ?? (() => descriptor.Availability),
                sessionScoped));
        }
    }

    private void Execute(Registration registration, GatewayAutomationRun run, IReadOnlyDictionary<string, object?> arguments)
    {
        run.MarkRunning(utcNow());
        try
        {
            var context = new GatewayAutomationContext(run, utcNow);
            run.ThrowIfStopRequested(utcNow());
            var result = registration.Handler(context, arguments);
            run.ThrowIfStopRequested(utcNow());
            run.Complete("succeeded", utcNow(), result);
        }
        catch (GatewayAutomationTimeoutException)
        {
            run.Complete("timed-out", utcNow(), error: new GatewayAutomationError(
                "automation_timed_out",
                "The automation exceeded its timeout."));
        }
        catch (OperationCanceledException)
        {
            run.Complete("cancelled", utcNow(), error: new GatewayAutomationError("automation_cancelled", "The automation was cancelled."));
        }
        catch (GatewayAutomationException exception)
        {
            run.Complete("failed", utcNow(), error: new GatewayAutomationError(exception.Code, exception.Message));
        }
        catch (Exception exception)
        {
            diagnostics?.Append(
                "Error",
                $"Automation '{run.Name}' run '{run.RunId}' failed unexpectedly.",
                exception.ToString(),
                requestId: run.RequestId);
            run.Complete("failed", utcNow(), error: new GatewayAutomationError("automation_failed", exception.Message));
        }
    }

    private void TrimHistory()
    {
        lock (sync)
        {
            while (history.Count > historyCapacity)
            {
                var candidate = history.First!;
                var run = runs[candidate.Value];
                if (!run.IsTerminal)
                {
                    return;
                }

                history.RemoveFirst();
                runs.Remove(candidate.Value);
                if (run.IdempotencyKey is not null)
                {
                    idempotency.Remove(IdempotencyIndex(run.Name, run.IdempotencyKey)!);
                }
            }
        }
    }

    private static string Fingerprint(IReadOnlyDictionary<string, object?> arguments)
    {
        try
        {
            using var sha256 = SHA256.Create();
            return Convert.ToBase64String(sha256.ComputeHash(GatewayJsonWriter.Write(arguments)));
        }
        catch (Exception exception) when (exception is not GatewayAutomationException)
        {
            throw new GatewayAutomationException("invalid_automation_arguments", "Automation arguments cannot be normalized.", exception);
        }
    }

    private static IReadOnlyDictionary<string, object?> SnapshotArguments(
        IReadOnlyDictionary<string, object?> arguments)
    {
        try
        {
            var copy = new SortedDictionary<string, object?>(StringComparer.Ordinal);
            foreach (var argument in arguments)
            {
                copy.Add(argument.Key, argument.Value);
            }

            return new ReadOnlyDictionary<string, object?>(copy);
        }
        catch (Exception exception)
        {
            throw new GatewayAutomationException(
                "invalid_automation_arguments",
                "Automation arguments cannot be read.",
                exception);
        }
    }

    private static string? IdempotencyIndex(string name, string? key)
    {
        if (key is null)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(key) || key.Length > 128)
        {
            throw Error("invalid_idempotency_key", "An idempotency key must contain 1 to 128 characters.");
        }

        return name + "\n" + key;
    }

    private bool TryResolveIdempotentRun(
        string idempotencyIndex,
        string argumentFingerprint,
        out GatewayAutomationRun? run)
    {
        if (!idempotency.TryGetValue(idempotencyIndex, out run))
        {
            return false;
        }

        if (!string.Equals(run.ArgumentFingerprint, argumentFingerprint, StringComparison.Ordinal))
        {
            throw Error("idempotency_conflict", "The idempotency key was already used with different arguments.");
        }

        return true;
    }

    private static GatewayAutomationException Error(string code, string message) => new(code, message);

    private sealed class Registration
    {
        public Registration(
            GatewayAutomationDescriptor descriptor,
            GatewayAutomationHandler handler,
            Func<GatewayAutomationAvailability> availability,
            bool sessionScoped)
        {
            Descriptor = descriptor;
            Handler = handler;
            Availability = availability;
            SessionScoped = sessionScoped;
        }

        public GatewayAutomationDescriptor Descriptor { get; }
        public GatewayAutomationHandler Handler { get; }
        public Func<GatewayAutomationAvailability> Availability { get; }
        public bool SessionScoped { get; }
    }
}
