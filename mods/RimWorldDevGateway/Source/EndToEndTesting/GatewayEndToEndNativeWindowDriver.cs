using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway;

internal sealed class GatewayEndToEndNativeWindowDriver : IGatewayEndToEndStepDriver
{
    private readonly IGatewayEndToEndStepDriver other;
    private readonly GatewayAutomationRegistry registry;
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<GatewayEndToEndTestContext, PointerScopeCleanup> scopes = new();
    public GatewayEndToEndNativeWindowDriver(IGatewayEndToEndStepDriver other, GatewayAutomationRegistry registry)
    { this.other = other; this.registry = registry; }

    public IGatewayEndToEndStepOperation Begin(EndToEndStep step, IEndToEndContext context)
    {
        if (step is NativeMainTabActionStep tab)
        {
            try
            {
                var run = registry.StartRun(GatewayMainTabAutomation.Name, "e2e-tab-" + Guid.NewGuid().ToString("N"),
                    new Dictionary<string, object?> { ["version"] = 1, ["defName"] = tab.DefName, ["open"] = tab.Open },
                    timeout: TimeSpan.FromSeconds(15));
                var artifacts = new Dictionary<string, string>
                {
                    ["automationRunId"] = run.RunId, ["requestId"] = run.RequestId,
                    ["automationState"] = run.State, ["automationErrorCode"] = run.Error?.Code ?? string.Empty,
                    ["expectedFailureCode"] = tab.ExpectedFailureCode ?? string.Empty,
                    ["nativeMainTabReceipt"] = System.Text.Encoding.UTF8.GetString(GatewayJsonWriter.Write(run.Result)),
                    ["nativeMainTabObservations"] = System.Text.Encoding.UTF8.GetString(GatewayJsonWriter.Write(run.Artifacts))
                };
                if (tab.ExpectedFailureCode != null)
                    return GatewayEndToEndCompletedStepOperation.FromOutcome(run.State == "failed" && run.Error?.Code == tab.ExpectedFailureCode
                        ? GatewayEndToEndStepOutcome.Pass(artifacts)
                        : GatewayEndToEndStepOutcome.Fail("native_expected_rejection_missing",
                            "The native tab command did not return the expected rejection.", artifacts));
                return GatewayEndToEndCompletedStepOperation.FromOutcome(run.State == "succeeded"
                    ? GatewayEndToEndStepOutcome.Pass(artifacts)
                    : GatewayEndToEndStepOutcome.Fail(run.Error?.Code ?? "native_main_tab_failed", run.Error?.Message ?? run.State, artifacts));
            }
            catch (GatewayAutomationException exception)
            { return GatewayEndToEndCompletedStepOperation.Failed(exception.Code, exception.Message); }
        }
        if (step is NativePointerScopeBeginStep scope) return BeginPointerScope(scope, context);
        if (step is NativePointerScopeEndStep end)
        {
            if (context is not GatewayEndToEndTestContext owned || !scopes.TryGetValue(owned, out var cleanup) ||
                cleanup.Handle != end.ScopeHandle)
                return GatewayEndToEndCompletedStepOperation.Failed("native_pointer_scope_not_owned",
                    "This scenario does not own the requested pointer workflow.");
            return new PointerScopeEndOperation(cleanup);
        }
        if (step is NativeDeveloperLogActionStep log)
        {
            try
            {
                var run = registry.StartRun(GatewayDeveloperLogAutomation.Name, "e2e-log-" + Guid.NewGuid().ToString("N"),
                    new Dictionary<string, object?> { ["version"] = 1, ["open"] = log.Open }, timeout: TimeSpan.FromSeconds(15));
                var artifacts = new Dictionary<string, string>
                {
                    ["automationRunId"] = run.RunId, ["requestId"] = run.RequestId,
                    ["nativeLogReceipt"] = System.Text.Encoding.UTF8.GetString(GatewayJsonWriter.Write(run.Result)),
                    ["nativeLogObservations"] = System.Text.Encoding.UTF8.GetString(GatewayJsonWriter.Write(run.Artifacts))
                };
                return GatewayEndToEndCompletedStepOperation.FromOutcome(run.State == "succeeded"
                    ? GatewayEndToEndStepOutcome.Pass(artifacts)
                    : GatewayEndToEndStepOutcome.Fail(run.Error?.Code ?? "native_log_failed", run.Error?.Message ?? run.State, artifacts));
            }
            catch (GatewayAutomationException exception)
            { return GatewayEndToEndCompletedStepOperation.Failed(exception.Code, exception.Message); }
        }
        if (step is NativeWindowClickActionStep or NativeWindowReplaceTextActionStep)
        {
            var click = step as NativeWindowClickActionStep;
            var text = step as NativeWindowReplaceTextActionStep;
            if (context is not GatewayEndToEndTestContext ownedContext)
                return GatewayEndToEndCompletedStepOperation.Failed("native_input_cleanup_unavailable",
                    "Native input requires the owned asynchronous scenario cleanup context.");
            if (click?.PointerScopeHandle != null &&
                (!scopes.TryGetValue(ownedContext, out var pointerScope) || pointerScope.Closed ||
                    pointerScope.Handle != click.PointerScopeHandle))
                return GatewayEndToEndCompletedStepOperation.Failed("native_pointer_scope_not_owned",
                    "This scenario does not own the requested pointer workflow.");
            GatewayAutomationRun? retained = null;
            bool deferredAdmitted = false;
            ownedContext.DeferCleanup(() =>
            {
                if (retained == null) return true;
                if (!retained.IsTerminal) { retained.RequestCancellation(); return false; }
                bool needsReceipt = deferredAdmitted || retained.Result != null || retained.State == "succeeded" ||
                    retained.Error?.Code == "automation_cleanup_incomplete";
                if (needsReceipt && (retained.Result is not IReadOnlyDictionary<string, object?> receipt ||
                    !receipt.TryGetValue("cleanupComplete", out var clean) || clean is not true))
                    throw new InvalidOperationException("Native input cleanup was not confirmed.");
                return true;
            });
            try
            {
                var arguments = new Dictionary<string, object?>
                {
                    ["version"] = click?.PointerScopeHandle == null ? 1 : 2,
                    ["windowHandle"] = click?.WindowHandle ?? text!.WindowHandle
                };
                if (click != null)
                {
                    arguments["pixelX"] = click.PixelX; arguments["pixelY"] = click.PixelY;
                    if (click.PointerScopeHandle != null) arguments["pointerScopeHandle"] = click.PointerScopeHandle;
                }
                else { arguments["controlHandle"] = text!.ControlHandle; arguments["text"] = text.Text; }
                var run = registry.StartRun(click != null ? GatewayNativeWindowAutomations.ClickName : GatewayNativeWindowAutomations.ReplaceTextName,
                    "e2e-native-" + Guid.NewGuid().ToString("N"),
                    arguments, timeout: TimeSpan.FromSeconds(15));
                retained = run;
                deferredAdmitted = run.DeferredOperation != null;
                return new InputOperation(run, arguments, click?.ExpectedFailureCode ?? text?.ExpectedFailureCode);
            }
            catch (GatewayAutomationException exception)
            { return GatewayEndToEndCompletedStepOperation.Failed(exception.Code, exception.Message); }
        }
        if (step is not NativeWindowCaptureStep capture) return other.Begin(step, context);
        try
        {
            var run = registry.StartRun(GatewayNativeWindowAutomations.CaptureName, "e2e-native-" + Guid.NewGuid().ToString("N"),
                new Dictionary<string, object?> { ["version"] = 1 }, timeout: TimeSpan.FromSeconds(15));
            if (run.State != "succeeded" || run.Result is not IReadOnlyList<GatewayNativeWindowDescriptor> windows)
                return GatewayEndToEndCompletedStepOperation.Failed(run.Error?.Code ?? "native_capture_invalid_result",
                    run.Error?.Message ?? "Native capture did not return its window descriptors.");
            var artifacts = new Dictionary<string, string>
            {
                ["automationRunId"] = run.RunId, ["requestId"] = run.RequestId,
                ["nativeWindowInventory"] = System.Text.Encoding.UTF8.GetString(GatewayJsonWriter.Write(windows))
            };
            try
            {
                capture.Receive(windows.Select(window => new EndToEndNativeWindow(window.Handle, window.RuntimeType,
                    window.NativeId, window.X, window.Y, window.Width, window.Height, window.UiScale,
                    window.PixelWidth, window.PixelHeight, window.FocusedTextControlHandle)).ToArray());
            }
            catch (Exception exception)
            {
                return GatewayEndToEndCompletedStepOperation.FromOutcome(GatewayEndToEndStepOutcome.Fail(
                    "native_capture_observation_failed", "Native window observation failed: " +
                    exception.GetBaseException().GetType().Name, artifacts));
            }
            return GatewayEndToEndCompletedStepOperation.Passed(artifacts);
        }
        catch (GatewayAutomationException exception)
        { return GatewayEndToEndCompletedStepOperation.Failed(exception.Code, exception.Message); }
    }

    private IGatewayEndToEndStepOperation BeginPointerScope(NativePointerScopeBeginStep step, IEndToEndContext context)
    {
        if (context is not GatewayEndToEndTestContext owned || owned.WorkflowDeadlineUtc is not { } deadline)
            return GatewayEndToEndCompletedStepOperation.Failed("native_pointer_scope_deadline_unavailable",
                "Pointer workflows require the owning scenario's existing deadline and cleanup context.");
        if (scopes.TryGetValue(owned, out var previous))
        {
            if (!previous.Closed)
                return GatewayEndToEndCompletedStepOperation.Failed("native_pointer_scope_already_owned",
                    "Restore this scenario's current pointer workflow before starting another.");
            scopes.Remove(owned);
        }
        var cleanup = new PointerScopeCleanup(registry);
        owned.DeferCleanup(cleanup.Poll);
        try
        {
            var run = registry.StartRun(GatewayNativeWindowAutomations.BeginScopeName,
                "e2e-pointer-" + Guid.NewGuid().ToString("N"),
                new Dictionary<string, object?> { ["version"] = 1, ["deadlineUtc"] = deadline.ToString("O") },
                timeout: TimeSpan.FromSeconds(15));
            cleanup.ObserveBegin(run);
            if (cleanup.Handle != null) scopes.Add(owned, cleanup);
            var artifacts = new Dictionary<string, string>
            {
                ["automationRunId"] = run.RunId, ["requestId"] = run.RequestId,
                ["pointerScopeHandle"] = cleanup.Handle ?? string.Empty,
                ["pointerScopeReceipt"] = System.Text.Encoding.UTF8.GetString(GatewayJsonWriter.Write(run.Result)),
                ["pointerScopeObservations"] = System.Text.Encoding.UTF8.GetString(GatewayJsonWriter.Write(run.Artifacts))
            };
            GatewayEndToEndStepOutcome outcome;
            if (run.State != "succeeded")
                outcome = GatewayEndToEndStepOutcome.Fail(run.Error?.Code ?? "native_pointer_scope_begin_failed",
                    run.Error?.Message ?? run.State, artifacts);
            else if (cleanup.Handle == null)
                outcome = GatewayEndToEndStepOutcome.Fail("native_pointer_scope_invalid_result",
                    "Pointer scope admission did not retain one exact cleanup identity.", artifacts);
            else
            {
                try { step.Receive(cleanup.Handle); outcome = GatewayEndToEndStepOutcome.Pass(artifacts); }
                catch (Exception exception)
                {
                    outcome = GatewayEndToEndStepOutcome.Fail("native_pointer_scope_observation_failed",
                        "Pointer scope receiver failed: " + exception.GetBaseException().GetType().Name, artifacts);
                }
            }
            return new PointerScopeBeginOperation(outcome, cleanup);
        }
        catch (GatewayAutomationException exception)
        { return GatewayEndToEndCompletedStepOperation.Failed(exception.Code, exception.Message); }
    }

    private sealed class PointerScopeCleanup
    {
        private readonly GatewayAutomationRegistry registry;
        private GatewayAutomationRun? begin;
        private GatewayAutomationRun? end;
        private bool ambiguous;
        public string? Handle { get; private set; }
        public bool Closed { get; private set; }
        public PointerScopeCleanup(GatewayAutomationRegistry registry) => this.registry = registry;

        public void ObserveBegin(GatewayAutomationRun run)
        {
            begin = run;
            var values = run.Artifacts.Where(artifact => artifact.Name == "native-pointer-scope")
                .Select(artifact => artifact.Value).Concat(new[] { run.Result });
            var handles = values.OfType<IReadOnlyDictionary<string, object?>>()
                .Where(value => value.TryGetValue("scopeHandle", out var handle) && handle is string text && !string.IsNullOrWhiteSpace(text))
                .Select(value => (string)value["scopeHandle"]!).Distinct(StringComparer.Ordinal).ToArray();
            ambiguous = handles.Length > 1;
            if (handles.Length == 1) Handle = handles[0];
        }

        public bool Poll()
        {
            if (Closed) return true;
            if (Handle == null)
            {
                if (ambiguous || begin?.State == "succeeded")
                    throw new InvalidOperationException("Pointer scope cleanup identity is unavailable.");
                return true;
            }
            end ??= registry.StartRun(GatewayNativeWindowAutomations.EndScopeName,
                "e2e-pointer-cleanup-" + Guid.NewGuid().ToString("N"),
                new Dictionary<string, object?> { ["version"] = 1, ["scopeHandle"] = Handle },
                timeout: TimeSpan.FromSeconds(15));
            if (!end.IsTerminal) return false;
            if (end.State != "succeeded" || end.Result is not IReadOnlyDictionary<string, object?> receipt ||
                !receipt.TryGetValue("scopeHandle", out var identity) || identity is not string handle || handle != Handle ||
                !receipt.TryGetValue("cleanupComplete", out var clean) || clean is not true ||
                !receipt.TryGetValue("pointerRestored", out var restored) || restored is not true ||
                !receipt.TryGetValue("pointerScopeState", out var state) || state is not "closed")
                throw new InvalidOperationException("Pointer workflow restoration was not confirmed.");
            Closed = true;
            return true;
        }

        public IReadOnlyDictionary<string, string> CaptureArtifacts() => new Dictionary<string, string>
        {
            ["pointerScopeHandle"] = Handle ?? string.Empty,
            ["automationRunId"] = end?.RunId ?? string.Empty,
            ["requestId"] = end?.RequestId ?? string.Empty,
            ["automationState"] = end?.State ?? "not_started",
            ["pointerScopeReceipt"] = System.Text.Encoding.UTF8.GetString(GatewayJsonWriter.Write(end?.Result))
        };
    }

    private sealed class PointerScopeEndOperation : IGatewayEndToEndStepOperation, IGatewayEndToEndStepEvidence
    {
        private readonly PointerScopeCleanup cleanup;
        private GatewayEndToEndStepOutcome? outcome;
        public PointerScopeEndOperation(PointerScopeCleanup cleanup) => this.cleanup = cleanup;
        public bool IsCompleted
        {
            get
            {
                if (outcome != null) return true;
                try
                {
                    if (cleanup.Poll()) outcome = GatewayEndToEndStepOutcome.Pass(CaptureArtifacts());
                }
                catch (Exception exception)
                {
                    outcome = GatewayEndToEndStepOutcome.Fail("native_pointer_scope_cleanup_failed",
                        "Pointer workflow restoration failed: " + exception.GetBaseException().GetType().Name,
                        CaptureArtifacts());
                }
                return outcome != null;
            }
        }
        public GatewayEndToEndStepOutcome GetOutcome() => IsCompleted ? outcome!
            : throw new InvalidOperationException("Pointer workflow cleanup has not completed.");
        public IReadOnlyDictionary<string, string> CaptureArtifacts() => cleanup.CaptureArtifacts();
    }

    private sealed class PointerScopeBeginOperation : IGatewayEndToEndStepOperation, IGatewayEndToEndStepEvidence
    {
        private readonly GatewayEndToEndStepOutcome outcome;
        private readonly PointerScopeCleanup cleanup;
        public PointerScopeBeginOperation(GatewayEndToEndStepOutcome outcome, PointerScopeCleanup cleanup)
        { this.outcome = outcome; this.cleanup = cleanup; }
        public bool IsCompleted => true;
        public GatewayEndToEndStepOutcome GetOutcome() => outcome;
        public IReadOnlyDictionary<string, string> CaptureArtifacts()
        {
            var artifacts = outcome.Artifacts.ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
            var restoration = cleanup.CaptureArtifacts();
            if (!string.IsNullOrEmpty(restoration["automationRunId"]))
            {
                artifacts["pointerScopeCleanupReceipt"] = restoration["pointerScopeReceipt"];
                artifacts["pointerScopeCleanupAutomationRunId"] = restoration["automationRunId"];
                artifacts["pointerScopeCleanupRequestId"] = restoration["requestId"];
                artifacts["pointerScopeCleanupState"] = restoration["automationState"];
            }
            return artifacts;
        }
    }

    private sealed class InputOperation : IGatewayEndToEndStepOperation, IGatewayEndToEndStepEvidence
    {
        private readonly GatewayAutomationRun run;
        private readonly IReadOnlyDictionary<string, object?> request;
        private readonly string? expectedFailure;
        public InputOperation(GatewayAutomationRun run, IReadOnlyDictionary<string, object?> request, string? expectedFailure)
        { this.run = run; this.request = request; this.expectedFailure = expectedFailure; }
        public bool IsCompleted => run.IsTerminal;
        public GatewayEndToEndStepOutcome GetOutcome()
        {
            if (!IsCompleted) throw new InvalidOperationException("Native input has not finished cleanup.");
            var artifacts = CaptureArtifacts();
            if (expectedFailure != null)
            {
                if (run.State == "failed" && run.Error?.Code == expectedFailure &&
                    run.Result is IReadOnlyDictionary<string, object?> receipt &&
                    receipt.TryGetValue("cleanupComplete", out var clean) && clean is true)
                    return GatewayEndToEndStepOutcome.Pass(artifacts);
                return GatewayEndToEndStepOutcome.Fail("native_expected_rejection_missing",
                    "Native input did not return the expected rejection with confirmed cleanup.", artifacts);
            }
            return run.State == "succeeded" ? GatewayEndToEndStepOutcome.Pass(artifacts)
                : GatewayEndToEndStepOutcome.Fail(run.Error?.Code ?? "native_input_failed", run.Error?.Message ?? run.State, artifacts);
        }
        public IReadOnlyDictionary<string, string> CaptureArtifacts()
        {
            var artifacts = new Dictionary<string, string>
            {
                ["automationRunId"] = run.RunId, ["requestId"] = run.RequestId,
                ["automationState"] = run.State, ["automationErrorCode"] = run.Error?.Code ?? string.Empty,
                ["expectedFailureCode"] = expectedFailure ?? string.Empty,
                ["nativeInputReceipt"] = System.Text.Encoding.UTF8.GetString(GatewayJsonWriter.Write(run.Result))
            };
            foreach (var pair in request)
                artifacts[pair.Key] = pair.Value is double number ? number.ToString("R", System.Globalization.CultureInfo.InvariantCulture)
                    : Convert.ToString(pair.Value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
            return artifacts;
        }
    }
}
