using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Threading;
using Verse;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayEndToEndGatewayBackendTests
{
    [Test]
    public void Concrete_backend_maps_time_selection_camera_and_architect_queries_to_gateway_services()
    {
        var game = new RecordingGameOperations();
        var thingOperations = new RecordingThingOperations();
        var cameraOperations = new RecordingCameraOperations();
        var gizmoSource = new RecordingGizmoSource();
        var backend = CreateBackend(game, thingOperations, cameraOperations, gizmoSource);

        backend.SetTime(paused: true, EndToEndGameSpeed.Fast);
        backend.SetSelection(new[] { "thing_1" }, additive: false);
        backend.SetCamera("map-7", new GatewayMapCell(20, 30), 18f);
        var target = backend.ResolveTarget("thing_1");
        backend.QueryGizmos(Array.Empty<string>(), new[] { "Zone" });

        Assert.Multiple(() =>
        {
            Assert.That(game.Speed, Is.EqualTo(GatewayGameSpeed.Paused));
            Assert.That(thingOperations.SelectionOperation, Is.EqualTo(GatewaySelectionOperation.Replace));
            Assert.That(thingOperations.SelectionHandles, Is.EqualTo(new[] { "thing_1" }));
            Assert.That(cameraOperations.Center!.X, Is.EqualTo(20));
            Assert.That(cameraOperations.Center.Z, Is.EqualTo(30));
            Assert.That(cameraOperations.RootSize, Is.EqualTo(18f));
            Assert.That(target.MapHandle, Is.EqualTo("map-7"));
            Assert.That(target.OccupiedRect.MaxX, Is.EqualTo(12));
            Assert.That(gizmoSource.LastQuery!.OwnerHandles, Is.Empty);
            Assert.That(gizmoSource.LastQuery.ArchitectCategoryDefNames, Is.EqualTo(new[] { "Zone" }));
        });
    }

    [Test]
    public void Concrete_backend_rejects_artifactless_construction()
    {
        var error = Assert.Throws<ArgumentException>(() => CreateBackend(
            new RecordingGameOperations(),
            new RecordingThingOperations(),
            new RecordingCameraOperations(),
            new RecordingGizmoSource(),
            artifactDirectory: " "));

        Assert.That(error!.ParamName, Is.EqualTo("artifactDirectory"));
    }

    [Test]
    public void Screenshot_operation_persists_png_before_reporting_the_relative_artifact()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "gateway-e2e-backend-" + Guid.NewGuid().ToString("N"));
        var dispatcher = new GatewayDispatcher();
        var png = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
        try
        {
            var backend = CreateBackend(
                new RecordingGameOperations(),
                new RecordingThingOperations(),
                new RecordingCameraOperations(),
                new RecordingGizmoSource(),
                directory,
                dispatcher,
                new FixedScreenshotBackend(png));

            var operation = backend.BeginScreenshot(
                new ScreenshotStep("evidence", Array.Empty<string>(), 0),
                new GatewayEndToEndTestContext(() => 0, () => 0, _ => null));
            dispatcher.Drain(DispatchPhase.EndOfFrame);
            Assert.That(SpinWait.SpinUntil(() => operation.IsCompleted, TimeSpan.FromSeconds(5)), Is.True);
            var outcome = operation.GetOutcome();
            Assert.That(
                outcome.Passed,
                Is.True,
                $"{outcome.FailureCode}: {outcome.FailureMessage}");
            var artifact = outcome.Artifacts["screenshot"];

            Assert.Multiple(() =>
            {
                Assert.That(outcome.Passed, Is.True);
                Assert.That(Path.IsPathRooted(artifact), Is.False);
                Assert.That(File.ReadAllBytes(Path.Combine(directory, artifact)), Is.EqualTo(png));
            });
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Test]
    public void Screenshot_operation_retries_one_transient_capture_on_a_fresh_end_of_frame()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "gateway-e2e-backend-retry-" + Guid.NewGuid().ToString("N"));
        var dispatcher = new GatewayDispatcher();
        var png = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
        var screenshots = new TransientScreenshotBackend(png);
        try
        {
            var backend = CreateBackend(
                new RecordingGameOperations(),
                new RecordingThingOperations(),
                new RecordingCameraOperations(),
                new RecordingGizmoSource(),
                directory,
                dispatcher,
                screenshots);

            var operation = backend.BeginScreenshot(
                new ScreenshotStep("retry evidence", Array.Empty<string>(), 0),
                new GatewayEndToEndTestContext(() => 0, () => 0, _ => null));
            Assert.That(
                SpinWait.SpinUntil(
                    () =>
                    {
                        dispatcher.Drain(DispatchPhase.EndOfFrame);
                        return operation.IsCompleted;
                    },
                    TimeSpan.FromSeconds(5)),
                Is.True);
            var outcome = operation.GetOutcome();

            Assert.Multiple(() =>
            {
                Assert.That(
                    outcome.Passed,
                    Is.True,
                    $"{outcome.FailureCode}: {outcome.FailureMessage}");
                Assert.That(screenshots.CaptureCount, Is.EqualTo(2));
                Assert.That(
                    File.ReadAllBytes(Path.Combine(directory, outcome.Artifacts["screenshot"])),
                    Is.EqualTo(png));
            });
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Test]
    public void Float_menu_catalog_projects_native_options_to_shared_stable_metadata()
    {
        var consume = CreateHostSafeFloatMenuOption("Consume simple meal", () => { });
        var forbidden = CreateHostSafeFloatMenuOption("Cannot consume: forbidden", null);

        var projected = VerseGatewayEndToEndFloatMenuActions.ProjectOptions(new[]
        {
            consume,
            forbidden
        });

        Assert.Multiple(() =>
        {
            Assert.That(projected.Select(option => option.Label), Is.EqualTo(new[]
            {
                "Consume simple meal",
                "Cannot consume: forbidden"
            }));
            Assert.That(projected.Select(option => option.Disabled), Is.EqualTo(new[] { false, true }));
            Assert.That(projected[0].StableId, Is.EqualTo(VerseGatewayEndToEndFloatMenuActions.StableIdentity(consume)));
            Assert.That(projected[1].StableId, Is.EqualTo(VerseGatewayEndToEndFloatMenuActions.StableIdentity(forbidden)));
        });
    }

    [Test]
    public void Float_menu_stable_identity_distinguishes_callbacks_behind_the_same_visible_label()
    {
        var first = CreateHostSafeFloatMenuOption("Consume meal", FirstFloatMenuAction);
        var second = CreateHostSafeFloatMenuOption("Consume meal", SecondFloatMenuAction);

        Assert.That(
            VerseGatewayEndToEndFloatMenuActions.StableIdentity(first),
            Is.Not.EqualTo(VerseGatewayEndToEndFloatMenuActions.StableIdentity(second)));
    }

    private static FloatMenuOption CreateHostSafeFloatMenuOption(string label, Action? action)
    {
        var option = (FloatMenuOption)FormatterServices.GetUninitializedObject(typeof(FloatMenuOption));
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
        typeof(FloatMenuOption).GetField("labelInt", fields)!
            .SetValue(option, label);
        typeof(FloatMenuOption).GetField("action", fields)!
            .SetValue(option, action);
        return option;
    }

    private static void FirstFloatMenuAction()
    {
    }

    private static void SecondFloatMenuAction()
    {
    }

    private static GatewayEndToEndGatewayBackend CreateBackend(
        RecordingGameOperations game,
        RecordingThingOperations things,
        RecordingCameraOperations camera,
        RecordingGizmoSource gizmos,
        string artifactDirectory = "e2e-artifacts",
        GatewayDispatcher? dispatcher = null,
        IGatewayScreenshotBackend? screenshotBackend = null) =>
        new(
            new GatewayGameControlController(game),
            new GatewayThingController(things),
            new GatewayCameraController(camera),
            new GatewayGizmoRegistry(gizmos),
            new PassingFloatMenus(),
            new PassingSettlementTrade(),
            new PassingTradeDialogs(),
            new GatewayWindowsInput(),
            new GatewayScreenshotService(
                dispatcher ?? new GatewayDispatcher(),
                screenshotBackend ?? new UnusedScreenshotBackend(),
                1024),
            artifactDirectory);

    private sealed class RecordingGameOperations : IGatewayGameControlOperations
    {
        public bool DevMode { get; private set; } = true;
        public bool GodMode { get; private set; }
        public bool EffectiveGodMode => DevMode && GodMode;
        public bool DevModePermanentlyDisabled => false;
        public bool GameAvailable => true;
        public bool Paused => Speed == GatewayGameSpeed.Paused;
        public bool ForcePaused => false;
        public GatewayGameSpeed Speed { get; private set; } = GatewayGameSpeed.Normal;
        public void SetDevMode(bool enabled) => DevMode = enabled;
        public void SetGodMode(bool enabled) => GodMode = enabled;
        public void SetPaused(bool paused) => Speed = paused ? GatewayGameSpeed.Paused : GatewayGameSpeed.Normal;
        public void SetSpeed(GatewayGameSpeed speed) => Speed = speed;
    }

    private sealed class TransientScreenshotBackend : IGatewayScreenshotBackend
    {
        private readonly byte[] png;

        public TransientScreenshotBackend(byte[] png)
        {
            this.png = png;
        }

        public int CaptureCount { get; private set; }

        public object Capture()
        {
            CaptureCount++;
            if (CaptureCount == 1)
            {
                throw new InvalidOperationException("Transient Unity capture failure.");
            }

            return new object();
        }

        public byte[] EncodePng(object resource) => png;

        public void Destroy(object resource)
        {
        }
    }

    private sealed class RecordingThingOperations : IGatewayThingOperations
    {
        private IReadOnlyList<GatewayThingSummary> selection = Array.Empty<GatewayThingSummary>();

        public GatewaySelectionOperation? SelectionOperation { get; private set; }
        public IReadOnlyList<string>? SelectionHandles { get; private set; }

        public GatewayThingWorldSnapshot CaptureWorld(bool includeViewRect) =>
            new("map-7", null, new[] { Summary("thing_1") });

        public GatewayThingInspection CaptureInspection(string handle) =>
            new(
                Summary(handle),
                string.Empty,
                string.Empty,
                Array.Empty<string>(),
                null,
                null,
                null,
                Array.Empty<GatewayInspectionWarning>());

        public IReadOnlyList<GatewayThingSummary> CaptureSelection() => selection;

        public void ApplySelection(GatewaySelectionOperation operation, IReadOnlyList<string> handles)
        {
            SelectionOperation = operation;
            SelectionHandles = handles.ToArray();
            selection = handles.Select(Summary).ToArray();
        }

        private static GatewayThingSummary Summary(string handle) =>
            new(
                handle,
                "thing",
                "Thing",
                "Thing",
                "item",
                "map-7",
                new GatewayMapCell(10, 11),
                new GatewayMapRect(10, 11, 12, 13),
                "North",
                1,
                null,
                null,
                null,
                false,
                false,
                false,
                null);
    }

    private sealed class RecordingCameraOperations : IGatewayCameraOperations
    {
        public GatewayMapCell? Center { get; private set; }
        public float? RootSize { get; private set; }

        public GatewayCameraSnapshot Capture() => new(
            "map-7",
            Center ?? new GatewayMapCell(5, 5),
            RootSize ?? 12f,
            "Middle",
            new GatewayMapRect(0, 0, 50, 50),
            100,
            100,
            8f,
            60f);

        public void Set(GatewayMapCell? center, float? rootSize)
        {
            Center = center;
            RootSize = rootSize;
        }
    }

    private sealed class RecordingGizmoSource : IGatewayGizmoSource
    {
        public GatewayGizmoSourceQuery? LastQuery { get; private set; }

        public GatewayGizmoDiscovery Discover(GatewayGizmoSourceQuery query)
        {
            LastQuery = query;
            return new GatewayGizmoDiscovery(
                "map-7",
                query.OwnerHandles,
                Array.Empty<IGatewayGizmoCandidate>(),
                false);
        }
    }

    private sealed class PassingFloatMenus : IGatewayEndToEndFloatMenuActions
    {
        public GatewayEndToEndStepOutcome Apply(FloatMenuActionStep step) =>
            GatewayEndToEndStepOutcome.Pass();
    }

    private sealed class PassingTradeDialogs : IGatewayEndToEndTradeDialogActions
    {
        public GatewayEndToEndStepOutcome Apply(TradeDialogActionStep step) =>
            GatewayEndToEndStepOutcome.Pass();
    }

    private sealed class PassingSettlementTrade : IGatewayEndToEndSettlementTradeActions
    {
        public GatewayEndToEndStepOutcome Apply(SettlementTradeActionStep step) =>
            GatewayEndToEndStepOutcome.Pass();
    }

    private sealed class UnusedScreenshotBackend : IGatewayScreenshotBackend
    {
        public object Capture() => new object();
        public byte[] EncodePng(object resource) => Array.Empty<byte>();
        public void Destroy(object resource) { }
    }

    private sealed class FixedScreenshotBackend : IGatewayScreenshotBackend
    {
        private readonly byte[] png;

        public FixedScreenshotBackend(byte[] png) => this.png = png;

        public object Capture() => new object();
        public byte[] EncodePng(object resource) => png;
        public void Destroy(object resource) { }
    }
}
