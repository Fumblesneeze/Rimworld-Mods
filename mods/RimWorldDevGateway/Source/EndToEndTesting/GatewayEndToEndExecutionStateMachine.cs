using System.Reflection;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway;

public sealed class GatewayEndToEndExecutionStateMachine : IGatewayEndToEndExecutionMachine
{
    internal const int MaximumFailureMessageUtf8Bytes = 8 * 1024;
    private const int MaximumFailureIdentityUtf8Bytes = 1024;
    private const int MaximumFailureStackUtf8Bytes = 32 * 1024;
    private readonly GatewayEndToEndRuntimeTestDescriptor[] descriptors;
    private readonly IGatewayEndToEndClock clock;
    private readonly Func<GatewayEndToEndTestContext> contextFactory;
    private readonly IGatewayEndToEndStepDriver stepDriver;
    private readonly IGatewayEndToEndTestIsolation isolation;
    private readonly string? sessionCredential;
    private readonly List<TestRecord> completed = new();
    private GatewayEndToEndExecutionSnapshot snapshot = new("pending");
    private GatewayEndToEndExecutionSnapshot? persistenceCandidate;
    private ExecutionPhase phase = ExecutionPhase.Selecting;
    private int testIndex;
    private GatewayEndToEndRuntimeTestDescriptor? descriptor;
    private TestRecord? current;
    private StepRecord? currentStep;
    private GatewayEndToEndTestContext? context;
    private IRimWorldEndToEndTest? test;
    private IEnumerator<EndToEndStep>? enumerator;
    private IGatewayEndToEndStepOperation? stepOperation;
    private bool processTainted;
    private string plannedTerminalStatus = "passed";
    private bool cleanupEnumeratorDisposed;
    private bool cleanupTrustworthy;
    private long cleanupStartedFrame;
    private int cleanupStartedGameTick;
    private DateTimeOffset cleanupStartedUtc;

    public GatewayEndToEndExecutionStateMachine(
        IEnumerable<GatewayEndToEndRuntimeTestDescriptor> tests,
        IGatewayEndToEndClock clock,
        Func<GatewayEndToEndTestContext> contextFactory,
        IGatewayEndToEndStepDriver stepDriver,
        IGatewayEndToEndTestIsolation isolation,
        string? sessionCredential = null)
    {
        if (tests is null)
        {
            throw new ArgumentNullException(nameof(tests));
        }

        descriptors = tests.OrderBy(test => test.Id, StringComparer.Ordinal).ToArray();
        if (descriptors.Length == 0)
        {
            throw new ArgumentException("At least one admitted E2E test is required.", nameof(tests));
        }

        if (descriptors.GroupBy(test => test.Id, StringComparer.Ordinal).Any(group => group.Count() > 1))
        {
            throw new ArgumentException("Admitted E2E test IDs must be unique.", nameof(tests));
        }

        this.clock = clock ?? throw new ArgumentNullException(nameof(clock));
        this.contextFactory = contextFactory ?? throw new ArgumentNullException(nameof(contextFactory));
        this.stepDriver = stepDriver ?? throw new ArgumentNullException(nameof(stepDriver));
        this.isolation = isolation ?? throw new ArgumentNullException(nameof(isolation));
        this.sessionCredential = string.IsNullOrEmpty(sessionCredential) ? null : sessionCredential;
    }

    public GatewayEndToEndExecutionSnapshot Snapshot => snapshot;

    public bool PersistencePending => persistenceCandidate is not null;

    public void ConfirmPersisted(GatewayEndToEndExecutionSnapshot exactSnapshot)
    {
        if (persistenceCandidate is null)
        {
            throw new InvalidOperationException("No E2E execution snapshot is awaiting persistence.");
        }

        if (!ReferenceEquals(exactSnapshot, persistenceCandidate))
        {
            throw new InvalidOperationException("Only the exact pending E2E snapshot can release persistence.");
        }

        persistenceCandidate = null;
    }

    public void Advance()
    {
        if (PersistencePending || snapshot.IsTerminal)
        {
            return;
        }

        switch (phase)
        {
            case ExecutionPhase.Selecting:
                BeginNextTestOrComplete();
                return;
            case ExecutionPhase.Arranging:
                ArrangeCurrentTest();
                return;
            case ExecutionPhase.StartingExecution:
                StartExecution();
                return;
            case ExecutionPhase.PullingStep:
                PullNextStep();
                return;
            case ExecutionPhase.RunningStep:
                AdvanceCurrentStep();
                return;
            case ExecutionPhase.Cleaning:
                CleanupCurrentTest();
                return;
            case ExecutionPhase.AfterTest:
                BeginNextTestOrComplete();
                return;
            default:
                throw new InvalidOperationException("The E2E execution state is invalid.");
        }
    }

    private void BeginNextTestOrComplete()
    {
        if (processTainted)
        {
            while (testIndex < descriptors.Length)
            {
                var skipped = new TestRecord(descriptors[testIndex++], clock)
                {
                    Status = "skipped",
                    CompletedFrame = clock.FrameCount,
                    CompletedGameTick = clock.GameTick,
                    CompletedUtc = clock.UtcNow,
                    CleanupState = "not_run"
                };
                AssignFailure(
                    skipped,
                    new GatewayEndToEndExecutionFailureSnapshot(
                        "infrastructure",
                        "process_tainted",
                        "The E2E process was tainted by an unverifiable prior cleanup.",
                        null));
                completed.Add(skipped);
            }

            current = null;
            phase = ExecutionPhase.Complete;
            Publish("infrastructure_failed");
            return;
        }

        if (testIndex >= descriptors.Length)
        {
            current = null;
            phase = ExecutionPhase.Complete;
            Publish(completed.All(test => test.Status == "passed")
                ? "completed"
                : "completed_with_failures");
            return;
        }

        descriptor = descriptors[testIndex++];
        current = new TestRecord(descriptor, clock) { Status = "arranging" };
        currentStep = null;
        context = null;
        test = null;
        enumerator = null;
        stepOperation = null;
        plannedTerminalStatus = "passed";
        phase = ExecutionPhase.Arranging;
        Publish("running");
    }

    private void ArrangeCurrentTest()
    {
        if (!isolation.IsReady)
        {
            if (TestDeadlineExceeded())
            {
                processTainted = true;
                current!.Status = "infrastructure_failed";
                current.CleanupState = "not_run";
                AssignFailure(
                    current,
                    new GatewayEndToEndExecutionFailureSnapshot(
                        "infrastructure",
                        "player_control_not_ready",
                        "RimWorld did not grant player control before the E2E test deadline.",
                        null));
                current.CompletedFrame = clock.FrameCount;
                current.CompletedGameTick = clock.GameTick;
                current.CompletedUtc = clock.UtcNow;
                completed.Add(current);
                phase = ExecutionPhase.AfterTest;
                Publish("running");
            }

            return;
        }

        try
        {
            context = contextFactory() ??
                throw new InvalidOperationException("The E2E context factory returned null.");
        }
        catch (Exception exception)
        {
            processTainted = true;
            EnterCleaning(FormatFailure("infrastructure", "context_creation_failed", exception), "infrastructure_failed");
            return;
        }

        try
        {
            isolation.Prepare(context);
        }
        catch (Exception exception)
        {
            processTainted = true;
            EnterCleaning(FormatFailure("infrastructure", "pre_test_isolation_failed", exception), "infrastructure_failed");
            return;
        }

        try
        {
            test = descriptor!.CreateTest();
            test.Arrange(context);
            current!.Status = "running";
            phase = ExecutionPhase.StartingExecution;
            Publish("running");
        }
        catch (Exception exception)
        {
            EnterCleaning(FormatFailure("exception", "arrange_failed", exception), "failed");
        }
    }

    private void StartExecution()
    {
        if (TestDeadlineExceeded())
        {
            EnterCleaning(Timeout("test_deadline_exceeded", "The E2E test exceeded its declared deadline."), "failed");
            return;
        }

        try
        {
            enumerator = test!.Execute(context!) ??
                throw new InvalidOperationException("The E2E test returned a null step iterator.");
            phase = ExecutionPhase.PullingStep;
        }
        catch (Exception exception)
        {
            EnterCleaning(FormatFailure("exception", "execute_start_failed", exception), "failed");
        }
    }

    private void PullNextStep()
    {
        if (TestDeadlineExceeded())
        {
            EnterCleaning(Timeout("test_deadline_exceeded", "The E2E test exceeded its declared deadline."), "failed");
            return;
        }

        try
        {
            if (!enumerator!.MoveNext())
            {
                EnterCleaning(null, "passed");
                return;
            }

            var step = enumerator.Current ??
                throw new InvalidOperationException("The E2E iterator yielded a null step.");
            currentStep = new StepRecord(step, clock);
            current!.Steps.Add(currentStep);
            stepOperation = null;
            phase = ExecutionPhase.RunningStep;
            Publish("running");
        }
        catch (Exception exception)
        {
            EnterCleaning(FormatFailure("exception", "step_iterator_failed", exception), "failed");
        }
    }

    private void AdvanceCurrentStep()
    {
        if (TestDeadlineExceeded())
        {
            TimeoutCurrentStep("test_deadline_exceeded", "The E2E test exceeded its declared deadline.");
            return;
        }

        try
        {
            var step = currentStep!.Step;
            switch (step)
            {
                case WaitUntilStep wait:
                    if (wait.Predicate(context!))
                    {
                        PassCurrentStep();
                    }
                    else if (DeadlineExceeded(
                                 currentStep.StartedFrame,
                                 currentStep.StartedGameTick,
                                 currentStep.StartedUtc,
                                 wait.Deadline))
                    {
                        TimeoutCurrentStep(
                            "step_deadline_exceeded",
                            "The E2E wait step exceeded its declared deadline.");
                    }

                    return;
                case AssertionStep assertion:
                    assertion.Assertion(context!);
                    PassCurrentStep();
                    return;
                case CheckpointStep checkpoint:
                    var captured = checkpoint.Capture(context!) ??
                        throw new InvalidOperationException("The E2E checkpoint returned null.");
                    PassCurrentStep(captured);
                    return;
                default:
                    AdvanceDrivenStep(step);
                    return;
            }
        }
        catch (EndToEndAssertionException exception)
        {
            FailCurrentStep(FormatFailure("assertion", "assertion_failed", exception));
        }
        catch (Exception exception)
        {
            FailCurrentStep(FormatFailure("exception", "step_execution_failed", exception));
        }
    }

    private void AdvanceDrivenStep(EndToEndStep step)
    {
        if (stepOperation is null)
        {
            stepOperation = stepDriver.Begin(step, context!) ??
                throw new InvalidOperationException("The E2E step driver returned a null operation.");
        }

        if (!stepOperation.IsCompleted)
        {
            return;
        }

        var outcome = stepOperation.GetOutcome() ??
            throw new InvalidOperationException("The E2E step operation returned a null outcome.");
        if (outcome.Passed)
        {
            PassCurrentStep(outcome.Artifacts);
            return;
        }

        var failure = new GatewayEndToEndExecutionFailureSnapshot(
            "exception",
            outcome.FailureCode ?? "step_action_failed",
            outcome.FailureMessage ?? "The E2E action failed.",
            null);
        FailCurrentStep(failure);
    }

    private void PassCurrentStep(IReadOnlyDictionary<string, string>? artifacts = null)
    {
        currentStep!.Status = "passed";
        currentStep.CompletedFrame = clock.FrameCount;
        currentStep.CompletedGameTick = clock.GameTick;
        currentStep.CompletedUtc = clock.UtcNow;
        currentStep.Artifacts = artifacts?.ToDictionary(
            pair => pair.Key,
            pair => pair.Value,
            StringComparer.Ordinal);
        stepOperation = null;
        phase = ExecutionPhase.PullingStep;
        Publish("running");
    }

    private void TimeoutCurrentStep(string code, string message)
    {
        currentStep!.Status = "timed_out";
        currentStep.CompletedFrame = clock.FrameCount;
        currentStep.CompletedGameTick = clock.GameTick;
        currentStep.CompletedUtc = clock.UtcNow;
        EnterCleaning(Timeout(code, message), "failed");
    }

    private void FailCurrentStep(GatewayEndToEndExecutionFailureSnapshot failure)
    {
        currentStep!.Status = "failed";
        currentStep.CompletedFrame = clock.FrameCount;
        currentStep.CompletedGameTick = clock.GameTick;
        currentStep.CompletedUtc = clock.UtcNow;
        EnterCleaning(failure, "failed");
    }

    private void EnterCleaning(
        GatewayEndToEndExecutionFailureSnapshot? failure,
        string terminalStatus)
    {
        current!.Status = "cleaning";
        if (failure is not null)
        {
            AssignFailure(current, failure);
        }

        plannedTerminalStatus = terminalStatus;
        cleanupEnumeratorDisposed = false;
        cleanupTrustworthy = true;
        cleanupStartedFrame = clock.FrameCount;
        cleanupStartedGameTick = clock.GameTick;
        cleanupStartedUtc = clock.UtcNow;
        phase = ExecutionPhase.Cleaning;
        Publish("running");
    }

    private void CleanupCurrentTest()
    {
        if (!cleanupEnumeratorDisposed)
        {
            try
            {
                enumerator?.Dispose();
            }
            catch
            {
                cleanupTrustworthy = false;
            }
            cleanupEnumeratorDisposed = true;
        }

        if (context is not null)
        {
            var deferred = context.RunDeferredCleanup();
            if (deferred == DeferredCleanupStatus.Pending)
            {
                if (!CleanupDeadlineExceeded()) return;
                context.AbandonDeferredCleanup();
                cleanupTrustworthy = false;
            }
            else if (deferred == DeferredCleanupStatus.Failed)
            {
                cleanupTrustworthy = false;
            }
            try
            {
                cleanupTrustworthy &= isolation.Cleanup(context);
            }
            catch
            {
                cleanupTrustworthy = false;
            }
        }
        else
        {
            cleanupTrustworthy = false;
        }

        if (!cleanupTrustworthy)
        {
            processTainted = true;
            current!.Status = "infrastructure_failed";
            current.CleanupState = "unverifiable";
            var cleanupFailure = new GatewayEndToEndExecutionFailureSnapshot(
                "infrastructure",
                "cleanup_unverifiable",
                "The E2E cleanup could not prove a reusable empty baseline.",
                null);
            if (current.Failure is null)
            {
                AssignFailure(current, cleanupFailure);
            }
            else
            {
                AssignFailure(current, cleanupFailure, cleanup: true);
            }
        }
        else
        {
            current!.Status = plannedTerminalStatus;
            current.CleanupState = "passed";
        }

        current.CompletedFrame = clock.FrameCount;
        current.CompletedGameTick = clock.GameTick;
        current.CompletedUtc = clock.UtcNow;
        completed.Add(current);
        phase = ExecutionPhase.AfterTest;
        Publish("running");
    }

    private bool CleanupDeadlineExceeded() => DeadlineExceeded(
        cleanupStartedFrame,
        cleanupStartedGameTick,
        cleanupStartedUtc,
        new EndToEndDeadline(3_600, 6_000, TimeSpan.FromSeconds(30)));

    private bool TestDeadlineExceeded() => DeadlineExceeded(
        current!.StartedFrame,
        current.StartedGameTick,
        current.StartedUtc,
        new EndToEndDeadline(
            descriptor!.MaxFrames,
            descriptor.MaxGameTicks,
            TimeSpan.FromSeconds(descriptor.MaxWallClockSeconds)));

    private bool DeadlineExceeded(
        long startedFrame,
        int startedGameTick,
        DateTimeOffset startedUtc,
        EndToEndDeadline deadline) =>
        clock.FrameCount - startedFrame > deadline.MaxFrames ||
        (long)clock.GameTick - startedGameTick > deadline.MaxGameTicks ||
        clock.UtcNow - startedUtc > deadline.MaxWallClock;

    private GatewayEndToEndExecutionFailureSnapshot FormatFailure(
        string kind,
        string code,
        Exception exception)
    {
        if (exception.GetType() == typeof(TargetInvocationException) &&
            exception.InnerException is not null)
        {
            exception = exception.InnerException;
        }

        var details = GatewayIntegrationTestExceptionFormatter.Format(exception, sessionCredential);
        return new GatewayEndToEndExecutionFailureSnapshot(
            kind,
            code,
            details.Message,
            details.Type,
            details.StackTrace);
    }

    private GatewayEndToEndExecutionFailureSnapshot SanitizeFailure(
        GatewayEndToEndExecutionFailureSnapshot failure) =>
        new(
            GatewayIntegrationTestExceptionFormatter.RedactAndBoundUtf8(
                failure.Kind,
                sessionCredential,
                MaximumFailureIdentityUtf8Bytes),
            GatewayIntegrationTestExceptionFormatter.RedactAndBoundUtf8(
                failure.Code,
                sessionCredential,
                MaximumFailureIdentityUtf8Bytes),
            GatewayIntegrationTestExceptionFormatter.RedactAndBoundUtf8(
                failure.Message,
                sessionCredential,
                MaximumFailureMessageUtf8Bytes),
            failure.ExceptionType is null
                ? null
                : GatewayIntegrationTestExceptionFormatter.RedactAndBoundUtf8(
                    failure.ExceptionType,
                    sessionCredential,
                    MaximumFailureIdentityUtf8Bytes),
            failure.StackTrace is null
                ? null
                : GatewayIntegrationTestExceptionFormatter.RedactAndBoundUtf8(
                    failure.StackTrace,
                    sessionCredential,
                    MaximumFailureStackUtf8Bytes));

    private void AssignFailure(
        TestRecord record,
        GatewayEndToEndExecutionFailureSnapshot failure,
        bool cleanup = false)
    {
        var sanitized = SanitizeFailure(failure);
        if (cleanup)
        {
            record.CleanupFailure = sanitized;
        }
        else
        {
            record.Failure = sanitized;
        }
    }

    private static GatewayEndToEndExecutionFailureSnapshot Timeout(string code, string message) =>
        new("timeout", code, message, null);

    private void Publish(string groupState)
    {
        var currentSnapshot = current?.Snapshot(currentStep);
        snapshot = new GatewayEndToEndExecutionSnapshot(
            groupState,
            currentSnapshot,
            completed.Select(test => test.Snapshot(
                ReferenceEquals(test, current) ? currentStep : null)),
            processTainted);
        persistenceCandidate = snapshot;
    }

    private enum ExecutionPhase
    {
        Selecting,
        Arranging,
        StartingExecution,
        PullingStep,
        RunningStep,
        Cleaning,
        AfterTest,
        Complete
    }

    private sealed class TestRecord
    {
        public TestRecord(GatewayEndToEndRuntimeTestDescriptor descriptor, IGatewayEndToEndClock clock)
        {
            Descriptor = descriptor;
            StartedFrame = clock.FrameCount;
            StartedGameTick = clock.GameTick;
            StartedUtc = clock.UtcNow;
        }

        public GatewayEndToEndRuntimeTestDescriptor Descriptor { get; }
        public string Status { get; set; } = "pending";
        public long StartedFrame { get; }
        public int StartedGameTick { get; }
        public DateTimeOffset StartedUtc { get; }
        public long? CompletedFrame { get; set; }
        public int? CompletedGameTick { get; set; }
        public DateTimeOffset? CompletedUtc { get; set; }
        public string CleanupState { get; set; } = "pending";
        public List<StepRecord> Steps { get; } = new();
        public GatewayEndToEndExecutionFailureSnapshot? Failure { get; set; }
        public GatewayEndToEndExecutionFailureSnapshot? CleanupFailure { get; set; }

        public GatewayEndToEndTestExecutionSnapshot Snapshot(StepRecord? currentStep) => new(
            Descriptor.Id,
            Status,
            StartedFrame,
            StartedGameTick,
            StartedUtc,
            CompletedFrame,
            CompletedGameTick,
            CompletedUtc,
            CleanupState,
            currentStep?.Snapshot(),
            Steps.Select(step => step.Snapshot()),
            Failure,
            CleanupFailure);
    }

    private sealed class StepRecord
    {
        public StepRecord(EndToEndStep step, IGatewayEndToEndClock clock)
        {
            Step = step;
            StartedFrame = clock.FrameCount;
            StartedGameTick = clock.GameTick;
            StartedUtc = clock.UtcNow;
        }

        public EndToEndStep Step { get; }
        public string Status { get; set; } = "running";
        public long StartedFrame { get; }
        public int StartedGameTick { get; }
        public DateTimeOffset StartedUtc { get; }
        public long? CompletedFrame { get; set; }
        public int? CompletedGameTick { get; set; }
        public DateTimeOffset? CompletedUtc { get; set; }
        public IReadOnlyDictionary<string, string>? Artifacts { get; set; }

        public GatewayEndToEndStepExecutionSnapshot Snapshot() => new(
            Step.Name,
            Step.Kind.ToString().ToLowerInvariant(),
            Status,
            StartedFrame,
            StartedGameTick,
            StartedUtc,
            CompletedFrame,
            CompletedGameTick,
            CompletedUtc,
            Artifacts);
    }
}
