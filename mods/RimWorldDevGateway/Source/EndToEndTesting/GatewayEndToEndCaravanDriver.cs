using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway;

internal sealed class GatewayEndToEndCaravanDriver : IGatewayEndToEndStepDriver
{
    private readonly IGatewayEndToEndStepDriver other;
    private readonly GatewayAutomationRegistry registry;
    private readonly Func<double> monotonicSeconds;
    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<GatewayEndToEndTestContext, ViewCleanup> scopes = new();
    internal GatewayEndToEndCaravanDriver(IGatewayEndToEndStepDriver other, GatewayAutomationRegistry registry, Func<double>? monotonicSeconds = null)
    { this.other = other; this.registry = registry; this.monotonicSeconds = monotonicSeconds ?? (() => System.Diagnostics.Stopwatch.GetTimestamp() / (double)System.Diagnostics.Stopwatch.Frequency); }
    public IGatewayEndToEndStepOperation Begin(EndToEndStep step, IEndToEndContext context)
    {
        if (step is not NativeCaravanStep) return other.Begin(step, context);
        if (context is not GatewayEndToEndTestContext owned)
            return GatewayEndToEndCompletedStepOperation.Failed("native_world_cleanup_unavailable", "An owned scenario cleanup context is required.");
        try
        {
            if (step is NativeWorldViewCaptureStep capture)
            {
                if (scopes.TryGetValue(owned, out var previous) && !previous.Closed) throw Error("native_world_view_already_owned");
                var cleanup = new ViewCleanup(registry, monotonicSeconds);
                owned.DeferCleanup(cleanup.Poll); // Before admission or the receiver can fail.
                scopes.Remove(owned); scopes.Add(owned, cleanup);
                var run = Start("native-world.capture", new());
                cleanup.ObserveCapture(run);
                return new Operation(run, cleanup, () => capture.Receive(cleanup.Handle ?? throw Error("native_world_capture_invalid_result")));
            }
            if (!scopes.TryGetValue(owned, out var scope) || scope.Handle == null || scope.Closed) throw Error("native_world_view_not_owned");
            var args = new Dictionary<string, object?>(); string name; Action? receive = null; string? expected = null;
            GatewayAutomationRun? retained = null;
            switch (step)
            {
                case NativeWorldViewRestoreStep restore:
                    Require(scope.Handle == restore.ViewHandle);
                    return new Operation(scope.BeginRestore(), scope);
                case NativeWorldViewActionStep world:
                    Require(scope.Handle == world.ViewHandle); name = "native-world.set-open";
                    args["viewHandle"] = world.ViewHandle; args["open"] = world.Open; break;
                case NativeCaravanCaptureStep caravan:
                    Require(scope.Handle == caravan.ViewHandle); name = "native-caravan.capture";
                    args["viewHandle"] = caravan.ViewHandle; args["worldObjectLoadId"] = caravan.WorldObjectLoadId;
                    receive = () => { string handle = ResultHandle(retained!, "caravanHandle"); scope.Caravans.Add(handle); caravan.Receive(handle); }; break;
                case NativeCaravanSelectStep select:
                    Require(scope.Caravans.Contains(select.CaravanHandle)); name = "native-caravan.select";
                    args["caravanHandle"] = select.CaravanHandle; break;
                case NativeCaravanTabStep tab:
                    Require(scope.Caravans.Contains(tab.CaravanHandle)); name = "native-caravan.tab";
                    args["caravanHandle"] = tab.CaravanHandle; args["tab"] = tab.Tab == EndToEndCaravanTab.Items ? "items" : "health";
                    args["open"] = tab.Open; break;
                case NativeCaravanHealthCaptureStep health:
                    Require(scope.Caravans.Contains(health.CaravanHandle)); name = "native-caravan-health.capture";
                    args["caravanHandle"] = health.CaravanHandle; args["pawnLoadId"] = health.PawnLoadId;
                    receive = () => { string handle = ResultHandle(retained!, "controlHandle"); scope.Controls.Add(handle); health.Receive(handle); }; break;
                case NativeCaravanHealthClickStep click:
                    Require(scope.Controls.Contains(click.ControlHandle)); name = "native-caravan-health.click";
                    args["controlHandle"] = click.ControlHandle; expected = click.ExpectedFailureCode;
                    bool deferred = false;
                    owned.DeferCleanup(() =>
                    {
                        if (retained == null) return true;
                        if (!retained.IsTerminal)
                        {
                            retained.RequestCancellation();
                            scope.AwaitInputRetirement();
                            return false;
                        }
                        if ((deferred || retained.Result != null || retained.State == "succeeded") && !InputClean(retained))
                        {
                            scope.InputCleanupFailure ??= "native_caravan_input_cleanup_incomplete";
                            throw new InvalidOperationException("Native caravan input cleanup was not confirmed.");
                        }
                        return true;
                    });
                    retained = Start(name, args); deferred = retained.DeferredOperation != null;
                    return new Operation(retained, scope, expectedFailure: expected, input: true, deferred: deferred);
                default: throw Error("native_caravan_step_unsupported");
            }
            retained = Start(name, args);
            return new Operation(retained, scope, receive);
        }
        catch (GatewayAutomationException exception)
        { return GatewayEndToEndCompletedStepOperation.Failed(exception.Code, exception.Message); }
    }
    private GatewayAutomationRun Start(string name, Dictionary<string, object?> args)
    { args["version"] = 1; return registry.StartRun(name, "e2e-caravan-" + Guid.NewGuid().ToString("N"), args, timeout: TimeSpan.FromSeconds(15)); }
    private static void Require(bool condition) { if (!condition) throw Error("native_caravan_handle_not_owned"); }
    private static string ResultHandle(GatewayAutomationRun run, string key) => run.Result is IReadOnlyDictionary<string, object?> result &&
        result.TryGetValue(key, out var value) && value is string handle && !string.IsNullOrWhiteSpace(handle)
        ? handle : throw Error("native_caravan_capture_invalid_result");
    private static bool InputClean(GatewayAutomationRun run) => run.Result is IReadOnlyDictionary<string, object?> result &&
        result.TryGetValue("cleanupComplete", out var clean) && clean is true;
    private static GatewayAutomationException Error(string code) => new(code, "This scenario must own the exact native World and caravan capability.");

    private sealed class ViewCleanup
    {
        private readonly GatewayAutomationRegistry registry;
        private readonly Func<double> monotonicSeconds;
        private double? inputDeadline, viewDeadline;
        internal string? InputCleanupFailure, ViewCleanupFailure;
        internal GatewayAutomationRun? RestoreRun;
        internal string? Handle;
        internal bool Closed;
        internal readonly HashSet<string> Caravans = new(StringComparer.Ordinal), Controls = new(StringComparer.Ordinal);
        internal ViewCleanup(GatewayAutomationRegistry registry, Func<double> monotonicSeconds)
        { this.registry = registry; this.monotonicSeconds = monotonicSeconds; }
        internal void AwaitInputRetirement()
        {
            inputDeadline ??= monotonicSeconds() + 15;
            if (monotonicSeconds() < inputDeadline) return;
            InputCleanupFailure = "native_caravan_input_cleanup_deadline";
            // Throwing advances the existing LIFO cleanup, but does not retire or release this run.
            throw new InvalidOperationException("Native caravan input retirement exceeded its15s cleanup share; ownership remains incomplete.");
        }
        internal void ObserveCapture(GatewayAutomationRun run)
        {
            var values = run.Artifacts.Where(a => a.Name == "native-world-lease").Select(a => a.Value).Concat(new[] { run.Result });
            var handles = values.OfType<IReadOnlyDictionary<string, object?>>().Where(v => v.TryGetValue("viewHandle", out var h) && h is string)
                .Select(v => (string)v["viewHandle"]!).Distinct(StringComparer.Ordinal).ToArray();
            if (handles.Length == 1) Handle = handles[0];
            else if (handles.Length > 1 || run.State == "succeeded") throw Error("native_world_capture_invalid_result");
        }
        internal GatewayAutomationRun BeginRestore() => RestoreRun ??= registry.StartRun("native-world.restore",
            "e2e-world-cleanup-" + Guid.NewGuid().ToString("N"), new Dictionary<string, object?> { ["version"] = 1, ["viewHandle"] = Handle },
            timeout: TimeSpan.FromSeconds(15));
        internal bool Poll()
        {
            if (Closed || Handle == null) return true;
            viewDeadline ??= monotonicSeconds() + 10;
            var run = BeginRestore();
            if (!run.IsTerminal)
            {
                if (monotonicSeconds() < viewDeadline) return false;
                ViewCleanupFailure = "native_world_cleanup_deadline";
                throw new InvalidOperationException("World restoration exceeded its10s cleanup share; restoration remains incomplete.");
            }
            if (run.Result is not GatewayWorldRestoreState { Complete: true, Restored: true })
            {
                ViewCleanupFailure ??= "native_world_cleanup_incomplete";
                throw new InvalidOperationException("The captured World view restoration was not confirmed.");
            }
            Closed = true; return true;
        }
    }

    private sealed class Operation : IGatewayEndToEndStepOperation, IGatewayEndToEndStepEvidence
    {
        private readonly GatewayAutomationRun run;
        private readonly ViewCleanup scope;
        private readonly Action? receive;
        private readonly string? expected;
        private readonly bool input, deferred;
        private GatewayEndToEndStepOutcome? outcome;
        internal Operation(GatewayAutomationRun run, ViewCleanup scope, Action? receive = null, string? expectedFailure = null,
            bool input = false, bool deferred = false)
        { this.run = run; this.scope = scope; this.receive = receive; expected = expectedFailure; this.input = input; this.deferred = deferred; }
        public bool IsCompleted => run.IsTerminal;
        public GatewayEndToEndStepOutcome GetOutcome()
        {
            if (!IsCompleted) throw new InvalidOperationException("Native caravan input and cleanup are still pending.");
            if (outcome != null) return outcome;
            bool clean = !input || InputClean(run) || !deferred && run.Result == null && run.State == "failed";
            if (expected != null) return outcome = run.State == "failed" && run.Error?.Code == expected && clean
                ? GatewayEndToEndStepOutcome.Pass(CaptureArtifacts()) : GatewayEndToEndStepOutcome.Fail("native_expected_rejection_missing",
                    "The exact expected rejection and owned cleanup were not observed.", CaptureArtifacts());
            if (run.State != "succeeded" || !clean) return outcome = GatewayEndToEndStepOutcome.Fail(run.Error?.Code ?? "native_caravan_action_failed",
                run.Error?.Message ?? "Native caravan action or cleanup failed.", CaptureArtifacts());
            try { receive?.Invoke(); }
            catch (Exception exception) { return outcome = GatewayEndToEndStepOutcome.Fail("native_caravan_observation_failed",
                exception.GetBaseException().GetType().Name, CaptureArtifacts()); }
            if (ReferenceEquals(run, scope.RestoreRun)) scope.Poll();
            return outcome = GatewayEndToEndStepOutcome.Pass(CaptureArtifacts());
        }
        public IReadOnlyDictionary<string, string> CaptureArtifacts() => new Dictionary<string, string>
        {
            ["automationRunId"] = run.RunId, ["requestId"] = run.RequestId, ["automationState"] = run.State,
            ["automationErrorCode"] = run.Error?.Code ?? string.Empty, ["expectedFailureCode"] = expected ?? string.Empty,
            ["nativeInputAdmitted"] = deferred ? "true" : "false",
            ["nativeInputCleanupFailure"] = scope.InputCleanupFailure ?? string.Empty,
            ["worldRestorationCleanupFailure"] = scope.ViewCleanupFailure ?? string.Empty,
            ["nativeCaravanReceipt"] = Json(run.Result), ["nativeCaravanObservations"] = Json(run.Artifacts),
            ["worldViewHandle"] = scope.Handle ?? string.Empty, ["worldRestorationRunId"] = scope.RestoreRun?.RunId ?? string.Empty,
            ["worldRestorationRequestId"] = scope.RestoreRun?.RequestId ?? string.Empty,
            ["worldRestorationState"] = scope.RestoreRun?.State ?? string.Empty, ["worldRestorationReceipt"] = Json(scope.RestoreRun?.Result)
        };
        private static string Json(object? value) => System.Text.Encoding.UTF8.GetString(GatewayJsonWriter.Write(value));
    }
}
