using System.Globalization;
using System.Text;
using NUnit.Framework;
using RimWorldDevGateway.Contracts;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayScreenshotEvidenceTests
{
    [Test]
    public void Screenshot_endpoint_preserves_native_readback_proof_alongside_crop_headers()
    {
        var dispatcher = new GatewayDispatcher();
        var png = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
        var backend = new DescribedScreenshotBackend(png, requiresReadback: true);
        var router = new GatewayApiRouter(dispatcher, new StubStateProvider(), new GatewayLogBuffer(),
            new GatewayApiServices(screenshotService: new GatewayScreenshotService(dispatcher, backend, 1024)),
            responseTimeout: TimeSpan.FromSeconds(2));
        var task = Task.Run(() => router.Handle(Post("/api/v1/screenshots",
            "{\"widthPixels\":960,\"heightPixels\":540}"), "readback-proof"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount > 0, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.EndOfFrame);
        backend.Frame = 101;
        Assert.That(SpinWait.SpinUntil(() =>
        {
            dispatcher.Drain(DispatchPhase.EndOfFrame);
            return task.IsCompleted;
        }, 2000), Is.True);
        var response = task.GetAwaiter().GetResult();
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200), Encoding.UTF8.GetString(response.Body));
            Assert.That(response.Body, Is.EqualTo(png));
            Assert.That(response.Headers, Contains.Key("X-Gateway-Readback-Provider"));
            if (!response.Headers.ContainsKey("X-Gateway-Readback-Provider")) return;
            Assert.That(response.Headers["X-Gateway-Readback-Provider"], Is.EqualTo("native-fixture"));
            Assert.That(response.Headers["X-Gateway-Admission-Frame"], Is.EqualTo("100"));
            Assert.That(response.Headers["X-Gateway-Rendered-Frame"], Is.EqualTo("101"));
            Assert.That(response.Headers["X-Gateway-Readback-Utc"], Is.EqualTo("2026-11-08T00:00:00.0000000+00:00"));
            Assert.That(int.Parse(response.Headers["X-Gateway-Readback-Attempts"], System.Globalization.CultureInfo.InvariantCulture), Is.GreaterThanOrEqualTo(1));
            Assert.That(response.Headers["X-Gateway-Frame-Width"], Is.EqualTo("1600"));
            Assert.That(response.Headers["X-Gateway-Crop-Width"], Is.EqualTo("960"));
        });
    }


    [Test]
    public void Screenshot_step_persists_png_with_native_readback_and_crop_evidence()
    {
        var directory = Path.Combine(Path.GetTempPath(), "gateway-readback-evidence-" + Guid.NewGuid().ToString("N"));
        var dispatcher = new GatewayDispatcher();
        var png = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
        var source = new DescribedScreenshotBackend(png, requiresReadback: true);
        var unused = new UnusedGameOperations();
        var backend = new GatewayEndToEndGatewayBackend(
            new GatewayGameControlController(unused), new GatewayThingController(unused),
            new GatewayCameraController(unused), new GatewayGizmoRegistry(unused), unused, unused, unused,
            new GatewayWindowsInput(), new GatewayScreenshotService(dispatcher, source, 1024), directory);
        try
        {
            var operation = backend.BeginScreenshot(new ScreenshotStep("readback evidence", Array.Empty<string>(), 0),
                new GatewayEndToEndTestContext(() => 0, () => 0, _ => null));
            dispatcher.Drain(DispatchPhase.EndOfFrame);
            source.Frame = 101;
            Assert.That(SpinWait.SpinUntil(() =>
            {
                dispatcher.Drain(DispatchPhase.EndOfFrame);
                return operation.IsCompleted;
            }, 2000), Is.True);
            var outcome = operation.GetOutcome();
            Assert.That(outcome.Passed, Is.True, outcome.FailureCode + ": " + outcome.FailureMessage);
            Assert.That(File.ReadAllBytes(Path.Combine(directory, outcome.Artifacts["screenshot"])), Is.EqualTo(png));
            Assert.That(outcome.Artifacts, Contains.Key("screenshot.readbackProvider"));
            Assert.Multiple(() =>
            {
                Assert.That(outcome.Artifacts["screenshot.readbackProvider"], Is.EqualTo("native-fixture"));
                Assert.That(outcome.Artifacts["screenshot.admissionFrame"], Is.EqualTo("100"));
                Assert.That(outcome.Artifacts["screenshot.renderedFrame"], Is.EqualTo("101"));
                Assert.That(outcome.Artifacts["screenshot.readbackUtc"], Is.EqualTo("2026-11-08T00:00:00.0000000+00:00"));
                Assert.That(int.Parse(outcome.Artifacts["screenshot.readbackAttempts"], CultureInfo.InvariantCulture), Is.GreaterThanOrEqualTo(1));
                Assert.That(outcome.Artifacts["screenshot.frameWidth"], Is.EqualTo("1600"));
                Assert.That(outcome.Artifacts["screenshot.frameHeight"], Is.EqualTo("900"));
                Assert.That(outcome.Artifacts["screenshot.cropX"], Is.EqualTo("320"));
                Assert.That(outcome.Artifacts["screenshot.cropY"], Is.EqualTo("180"));
                Assert.That(outcome.Artifacts["screenshot.cropWidth"], Is.EqualTo("960"));
                Assert.That(outcome.Artifacts["screenshot.cropHeight"], Is.EqualTo("540"));
            });
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public void Failed_screenshot_step_retains_native_failure_and_does_not_restart_capture()
    {
        var directory = Path.Combine(Path.GetTempPath(), "gateway-readback-failure-" + Guid.NewGuid().ToString("N"));
        var dispatcher = new GatewayDispatcher();
        var source = new DescribedScreenshotBackend(Array.Empty<byte>(), requiresReadback: true)
        { Failure = new GatewayScreenshotException("capture_failed", "Fixture readback failed.") };
        var unused = new UnusedGameOperations();
        var backend = new GatewayEndToEndGatewayBackend(
            new GatewayGameControlController(unused), new GatewayThingController(unused),
            new GatewayCameraController(unused), new GatewayGizmoRegistry(unused), unused, unused, unused,
            new GatewayWindowsInput(), new GatewayScreenshotService(dispatcher, source, 1024), directory);
        try
        {
            var operation = backend.BeginScreenshot(new ScreenshotStep("failed evidence", Array.Empty<string>(), 0),
                new GatewayEndToEndTestContext(() => 0, () => 0, _ => null));
            Assert.That(SpinWait.SpinUntil(() =>
            {
                dispatcher.Drain(DispatchPhase.EndOfFrame);
                source.Frame++;
                return operation.IsCompleted;
            }, 2000), Is.True);
            var outcome = operation.GetOutcome();
            Assert.Multiple(() =>
            {
                Assert.That(outcome.Passed, Is.False);
                Assert.That(outcome.FailureCode, Is.EqualTo("capture_failed"));
                Assert.That(outcome.FailureMessage, Is.EqualTo("Fixture readback failed."));
                Assert.That(outcome.Artifacts, Is.Empty);
                Assert.That(source.Captures, Is.EqualTo(1));
                Assert.That(Directory.Exists(directory), Is.False);
            });
        }
        finally
        {
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Test]
    public void Pending_screenshot_shutdown_retains_dispatcher_failure_without_an_artifact()
    {
        var directory = Path.Combine(Path.GetTempPath(), "gateway-readback-shutdown-" + Guid.NewGuid().ToString("N"));
        var dispatcher = new GatewayDispatcher();
        var source = new DescribedScreenshotBackend(Array.Empty<byte>(), requiresReadback: true);
        var unused = new UnusedGameOperations();
        var backend = new GatewayEndToEndGatewayBackend(
            new GatewayGameControlController(unused), new GatewayThingController(unused),
            new GatewayCameraController(unused), new GatewayGizmoRegistry(unused), unused, unused, unused,
            new GatewayWindowsInput(), new GatewayScreenshotService(dispatcher, source, 1024), directory);
        try
        {
            var operation = backend.BeginScreenshot(new ScreenshotStep("pending shutdown", Array.Empty<string>(), 0),
                new GatewayEndToEndTestContext(() => 0, () => 0, _ => null));
            dispatcher.Drain(DispatchPhase.EndOfFrame);
            Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount > 0, 2000), Is.True);
            dispatcher.Stop();
            Assert.That(SpinWait.SpinUntil(() => operation.IsCompleted, 2000), Is.True);
            var outcome = operation.GetOutcome();
            Assert.Multiple(() =>
            {
                Assert.That(outcome.Passed, Is.False);
                Assert.That(outcome.FailureCode, Is.EqualTo("gateway_stopping"));
                Assert.That(outcome.FailureMessage, Does.Contain("stopping"));
                Assert.That(outcome.Artifacts, Is.Empty);
                Assert.That(source.Captures, Is.Zero);
                Assert.That(Directory.Exists(directory), Is.False);
            });
        }
        finally
        {
            dispatcher.Stop();
            if (Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    // These host seams reject every game operation; the test exercises only capture and persistence.
    private sealed class UnusedGameOperations : IGatewayGameControlOperations, IGatewayThingOperations,
        IGatewayCameraOperations, IGatewayGizmoSource, IGatewayEndToEndFloatMenuActions,
        IGatewayEndToEndSettlementTradeActions, IGatewayEndToEndTradeDialogActions
    {
        private static Exception Unexpected() => new AssertionException("Screenshot must not access game state.");
        public bool DevMode => throw Unexpected();
        public bool GodMode => throw Unexpected();
        public bool EffectiveGodMode => throw Unexpected();
        public bool DevModePermanentlyDisabled => throw Unexpected();
        public bool GameAvailable => throw Unexpected();
        public bool Paused => throw Unexpected();
        public bool ForcePaused => throw Unexpected();
        public GatewayGameSpeed Speed => throw Unexpected();
        public void SetDevMode(bool enabled) => throw Unexpected();
        public void SetGodMode(bool enabled) => throw Unexpected();
        public void SetPaused(bool paused) => throw Unexpected();
        public void SetSpeed(GatewayGameSpeed speed) => throw Unexpected();
        public GatewayThingWorldSnapshot CaptureWorld(bool includeViewRect) => throw Unexpected();
        public GatewayThingInspection CaptureInspection(string handle) => throw Unexpected();
        public IReadOnlyList<GatewayThingSummary> CaptureSelection() => throw Unexpected();
        public void ApplySelection(GatewaySelectionOperation operation, IReadOnlyList<string> handles) => throw Unexpected();
        public GatewayCameraSnapshot Capture() => throw Unexpected();
        public void Set(GatewayMapCell? center, float? rootSize) => throw Unexpected();
        public GatewayGizmoDiscovery Discover(GatewayGizmoSourceQuery query) => throw Unexpected();
        public GatewayEndToEndStepOutcome Apply(FloatMenuActionStep step) => throw Unexpected();
        public GatewayEndToEndStepOutcome Apply(SettlementTradeActionStep step) => throw Unexpected();
        public GatewayEndToEndStepOutcome Apply(TradeDialogActionStep step) => throw Unexpected();
    }

    private sealed class DescribedScreenshotBackend : IGatewayScreenshotBackend,
        IGatewayDescribedScreenshotBackend, IGatewayRenderedScreenshotBackend
    {
        private readonly byte[] png;
        public DescribedScreenshotBackend(byte[] png, bool requiresReadback)
        { this.png = png; RequiresReadback = requiresReadback; }
        public bool RequiresReadback { get; }
        public int Frame { get; set; } = 100;
        public int Captures { get; private set; }
        public GatewayScreenshotException? Failure { get; set; }
        public int CurrentFrame => Frame;
        public object Capture() => throw new AssertionException("Described capture required.");
        public GatewayScreenshotResource CaptureDescribed(GatewayScreenshotRequest request)
        {
            Captures++;
            if (Failure != null) throw Failure;
            return new(new object(), 1600, 900, new GatewayScreenshotCrop(320, 180, 960, 540),
                new GatewayRenderedFrameEvidence("native-fixture", Frame,
                    new DateTimeOffset(2026, 11, 8, 0, 0, 0, TimeSpan.Zero)));
        }
        public byte[] EncodePng(object resource) => png;
        public void Destroy(object resource) { }
    }

    private sealed class StubStateProvider : IGatewayStateProvider
    {
        public object CaptureStatus() => new { };
        public object CaptureUiState() => new { };
    }

    private static GatewayHttpRequest Post(string path, string body) =>
        new("POST", path, path, string.Empty,
            new Dictionary<string, string> { ["Content-Type"] = "application/json" },
            Encoding.UTF8.GetBytes(body));
}
