using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayGameControlRouterTests
{
    [Test]
    public void Game_state_route_reads_and_atomically_sets_developer_god_pause_and_speed()
    {
        var dispatcher = new GatewayDispatcher();
        var operations = new FakeGameControlOperations();
        var controller = new GatewayGameControlController(operations);
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(gameControl: controller),
            responseTimeout: TimeSpan.FromSeconds(2));
        var request = new GatewayGameStateMutationRequest
        {
            DevMode = true,
            GodMode = true,
            Speed = "Fast"
        };

        var responseTask = Task.Run(() => router.Handle(
            Post("/api/v1/game-state", GatewayContractJson.Write(request)),
            "set-game-state"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.Update);
        var response = responseTask.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(body, Does.Contain("\"DevMode\":false"));
            Assert.That(body, Does.Contain("\"GodMode\":false"));
            Assert.That(body, Does.Contain("\"Speed\":\"Normal\""));
            Assert.That(body, Does.Contain("\"DevMode\":true"));
            Assert.That(body, Does.Contain("\"GodMode\":true"));
            Assert.That(body, Does.Contain("\"EffectiveGodMode\":true"));
            Assert.That(body, Does.Contain("\"Speed\":\"Fast\""));
            Assert.That(operations.WriteOrder, Is.EqualTo(new[] { "dev:True", "god:True", "speed:Fast" }));
        });
    }

    [Test]
    public void Game_state_mutation_rejects_unknown_fields_before_dispatch()
    {
        var dispatcher = new GatewayDispatcher();
        var operations = new FakeGameControlOperations();
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(
                gameControl: new GatewayGameControlController(operations)),
            responseTimeout: TimeSpan.FromSeconds(2));

        var response = router.Handle(
            Post("/api/v1/game-state", "{\"devMode\":true,\"surprise\":42}"),
            "unknown-game-state-field");
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(400));
            Assert.That(body, Does.Contain("\"code\":\"invalid_json\""));
            Assert.That(operations.DevMode, Is.False);
            Assert.That(dispatcher.PendingCount, Is.Zero);
        });
    }

    [Test]
    public void Camera_route_sets_an_absolute_center_and_zoom_and_returns_the_resulting_view()
    {
        var dispatcher = new GatewayDispatcher();
        var camera = new FakeCameraOperations();
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(camera: new GatewayCameraController(camera)),
            responseTimeout: TimeSpan.FromSeconds(2));
        var request = new GatewayCameraMutationRequest
        {
            MapHandle = "map-17",
            Center = new GatewayMapCellRequest { X = 30, Z = 40 },
            RootSize = 22f
        };

        var responseTask = Task.Run(() => router.Handle(
            Post("/api/v1/camera", GatewayContractJson.Write(request)),
            "set-camera"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.Update);
        var response = responseTask.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(camera.SetCalls, Is.EqualTo(new[] { "30,40@22" }));
            Assert.That(body, Does.Contain("\"MapHandle\":\"map-17\""));
            Assert.That(body, Does.Contain("\"Center\":{\"X\":30,\"Z\":40}"));
            Assert.That(body, Does.Contain("\"RootSize\":22"));
            Assert.That(body, Does.Contain("\"ViewRect\":{\"MaxX\":35,\"MaxZ\":45,\"MinX\":25,\"MinZ\":35}"));
        });
    }

    [Test]
    public void Camera_center_only_resolves_the_current_root_size_for_an_immediate_absolute_move()
    {
        var operations = new FakeCameraOperations();
        var controller = new GatewayCameraController(operations);

        var result = controller.Mutate(new GatewayCameraMutationRequest
        {
            MapHandle = "map-17",
            Center = new GatewayMapCellRequest { X = 30, Z = 40 }
        });

        Assert.Multiple(() =>
        {
            Assert.That(operations.RequestedRootSizes, Is.EqualTo(new float?[] { 40f }));
            Assert.That(result.After.Center.X, Is.EqualTo(30));
            Assert.That(result.After.Center.Z, Is.EqualTo(40));
            Assert.That(result.After.RootSize, Is.EqualTo(40f));
        });
    }

    [Test]
    public void Game_state_rejects_god_mode_without_developer_mode_and_force_paused_unpause()
    {
        var operations = new FakeGameControlOperations();
        var controller = new GatewayGameControlController(operations);

        var godError = Assert.Throws<GatewayGameControlException>(() => controller.Mutate(
            new GatewayGameStateMutationRequest { GodMode = true }));
        operations.SetPaused(true);
        operations.WriteOrder.Clear();
        operations.ForcePausedValue = true;
        var pauseError = Assert.Throws<GatewayGameControlException>(() => controller.Mutate(
            new GatewayGameStateMutationRequest { Paused = false }));

        Assert.Multiple(() =>
        {
            Assert.That(godError?.Code, Is.EqualTo("invalid_game_state"));
            Assert.That(operations.DevMode, Is.False);
            Assert.That(operations.GodMode, Is.False);
            Assert.That(pauseError?.Code, Is.EqualTo("game_force_paused"));
            Assert.That(operations.Paused, Is.True);
            Assert.That(operations.WriteOrder, Is.Empty);
        });
    }

    [TestCase("1")]
    [TestCase("Fast,Superfast")]
    public void Game_state_accepts_only_named_single_speed_values(string speed)
    {
        var operations = new FakeGameControlOperations();
        var controller = new GatewayGameControlController(operations);

        var error = Assert.Throws<GatewayGameControlException>(() => controller.Mutate(
            new GatewayGameStateMutationRequest { Speed = speed }));

        Assert.Multiple(() =>
        {
            Assert.That(error?.Code, Is.EqualTo("invalid_game_speed"));
            Assert.That(operations.Speed, Is.EqualTo(GatewayGameSpeed.Normal));
            Assert.That(operations.WriteOrder, Is.Empty);
        });
    }

    [Test]
    public void Disabling_developer_mode_enforces_god_mode_off_even_for_a_non_cascading_adapter()
    {
        var operations = new FakeGameControlOperations
        {
            DevMode = false,
            GodMode = true,
            AutoClearGodModeOnDisable = false
        };
        var controller = new GatewayGameControlController(operations);

        var result = controller.Mutate(new GatewayGameStateMutationRequest { DevMode = false });

        Assert.Multiple(() =>
        {
            Assert.That(result.After.DevMode, Is.False);
            Assert.That(result.After.GodMode, Is.False);
            Assert.That(operations.WriteOrder, Is.EqualTo(new[] { "god:False" }));
        });
    }

    [Test]
    public void Camera_rejects_a_stale_map_handle_before_mutation()
    {
        var operations = new FakeCameraOperations();
        var controller = new GatewayCameraController(operations);

        var error = Assert.Throws<GatewayCameraException>(() => controller.Mutate(
            new GatewayCameraMutationRequest
            {
                MapHandle = "map-old",
                Center = new GatewayMapCellRequest { X = 20, Z = 20 }
            }));

        Assert.Multiple(() =>
        {
            Assert.That(error?.Code, Is.EqualTo("stale_map_handle"));
            Assert.That(operations.SetCalls, Is.Empty);
        });
    }

    [Test]
    public void Game_state_get_includes_nullable_map_camera_and_bounded_selection()
    {
        var dispatcher = new GatewayDispatcher();
        var game = new FakeGameControlOperations { DevMode = true, GodMode = true };
        var camera = new FakeCameraOperations();
        var things = new FakeThingOperations();
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(
                gameControl: new GatewayGameControlController(game),
                camera: new GatewayCameraController(camera),
                things: new GatewayThingController(things)),
            responseTimeout: TimeSpan.FromSeconds(2));
        var request = new GatewayHttpRequest(
            "GET",
            "/api/v1/game-state",
            "/api/v1/game-state",
            string.Empty,
            new Dictionary<string, string>(),
            Array.Empty<byte>());

        var responseTask = Task.Run(() => router.Handle(request, "get-expanded-game-state"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.Update);
        var response = responseTask.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200), body);
            Assert.That(body, Does.Contain("\"DevMode\":true"));
            Assert.That(body, Does.Contain("\"CurrentMapHandle\":\"map-17\""));
            Assert.That(body, Does.Contain("\"Camera\":{"));
            Assert.That(body, Does.Contain("\"Center\":{\"X\":10,\"Z\":10}"));
            Assert.That(body, Does.Contain("\"Selection\":[{"));
            Assert.That(body, Does.Contain("\"Handle\":\"Pawn_17\""));
        });
    }

    private static GatewayHttpRequest Post(string path, string json) => new(
        "POST",
        path,
        path,
        string.Empty,
        new Dictionary<string, string>(),
        Encoding.UTF8.GetBytes(json));

    private sealed class StubStateProvider : IGatewayStateProvider
    {
        public object CaptureStatus() => new { };

        public object CaptureUiState() => new { };
    }

    private sealed class FakeGameControlOperations : IGatewayGameControlOperations
    {
        public List<string> WriteOrder { get; } = new();

        public bool DevMode { get; set; }

        public bool GodMode { get; set; }

        public bool EffectiveGodMode => DevMode && GodMode;

        public bool DevModePermanentlyDisabled => false;

        public bool GameAvailable => true;

        public bool Paused { get; private set; }

        public bool ForcePaused => ForcePausedValue;

        public bool ForcePausedValue { get; set; }

        public bool AutoClearGodModeOnDisable { get; set; } = true;

        public GatewayGameSpeed Speed { get; private set; } = GatewayGameSpeed.Normal;

        public void SetDevMode(bool enabled)
        {
            WriteOrder.Add("dev:" + enabled);
            DevMode = enabled;
            if (!enabled && AutoClearGodModeOnDisable)
            {
                GodMode = false;
            }
        }

        public void SetGodMode(bool enabled)
        {
            WriteOrder.Add("god:" + enabled);
            GodMode = enabled;
        }

        public void SetPaused(bool paused)
        {
            WriteOrder.Add("paused:" + paused);
            Paused = paused;
            if (paused)
            {
                Speed = GatewayGameSpeed.Paused;
            }
            else if (Speed == GatewayGameSpeed.Paused)
            {
                Speed = GatewayGameSpeed.Normal;
            }
        }

        public void SetSpeed(GatewayGameSpeed speed)
        {
            WriteOrder.Add("speed:" + speed);
            Speed = speed;
            Paused = speed == GatewayGameSpeed.Paused;
        }
    }

    private sealed class FakeCameraOperations : IGatewayCameraOperations
    {
        private GatewayCameraSnapshot snapshot = new(
            "map-17",
            new GatewayMapCell(10, 10),
            40f,
            "Middle",
            new GatewayMapRect(0, 0, 20, 20),
            100,
            100,
            11f,
            60f);

        public List<string> SetCalls { get; } = new();

        public List<float?> RequestedRootSizes { get; } = new();

        public GatewayCameraSnapshot Capture() => snapshot;

        public void Set(GatewayMapCell? center, float? rootSize)
        {
            RequestedRootSizes.Add(rootSize);
            var nextCenter = center ?? snapshot.Center;
            var nextSize = rootSize ?? snapshot.RootSize;
            SetCalls.Add(nextCenter.X + "," + nextCenter.Z + "@" + nextSize);
            snapshot = new GatewayCameraSnapshot(
                snapshot.MapHandle,
                nextCenter,
                nextSize,
                "Close",
                new GatewayMapRect(
                    nextCenter.X - 5,
                    nextCenter.Z - 5,
                    nextCenter.X + 5,
                    nextCenter.Z + 5),
                snapshot.MapWidth,
                snapshot.MapHeight,
                snapshot.MinimumRootSize,
                snapshot.MaximumRootSize);
        }
    }

    private sealed class FakeThingOperations : IGatewayThingOperations
    {
        private readonly IReadOnlyList<GatewayThingSummary> selection = new[]
        {
            new GatewayThingSummary(
                "Pawn_17",
                "Ada",
                "Human",
                "Verse.Pawn",
                "pawn",
                "map-17",
                new GatewayMapCell(10, 10),
                new GatewayMapRect(10, 10, 10, 10),
                "South",
                1,
                faction: null,
                hitPoints: new GatewayHitPointSummary(100, 100),
                quality: null,
                fogged: false,
                forbidden: false,
                selected: true,
                pawnFlags: new GatewayPawnFlags(true, false, true, false, false))
        };

        public GatewayThingWorldSnapshot CaptureWorld(bool includeViewRect) =>
            throw new NotSupportedException();

        public GatewayThingInspection CaptureInspection(string handle) =>
            throw new NotSupportedException();

        public IReadOnlyList<GatewayThingSummary> CaptureSelection() => selection;

        public void ApplySelection(GatewaySelectionOperation operation, IReadOnlyList<string> handles) =>
            throw new NotSupportedException();
    }
}
