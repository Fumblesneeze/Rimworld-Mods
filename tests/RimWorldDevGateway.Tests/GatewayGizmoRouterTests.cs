using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayGizmoRouterTests
{
    [Test]
    public void Gizmo_routes_query_toggle_and_apply_a_typed_rectangle_interaction()
    {
        var toggle = new FakeCandidate(
            "draft-toggle",
            "Draft",
            GatewayGizmoInteractionKind.Toggle,
            Array.Empty<GatewayInteractionInputKind>());
        var drag = new FakeCandidate(
            "zone-drag",
            "Growing zone",
            GatewayGizmoInteractionKind.Drag,
            new[]
            {
                GatewayInteractionInputKind.Cell,
                GatewayInteractionInputKind.Cells,
                GatewayInteractionInputKind.Line,
                GatewayInteractionInputKind.Rectangle
            });
        var registry = new GatewayGizmoRegistry(new FakeSource(toggle, drag));
        var dispatcher = new GatewayDispatcher();
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(gizmos: registry),
            responseTimeout: TimeSpan.FromSeconds(2));

        var queryResponse = Dispatch(
            dispatcher,
            router,
            Post(
                "/api/v1/gizmos/query",
                "{\"ownerScope\":\"selection\"," +
                "\"architectCategoryDefNames\":[\"Zone\"],\"limit\":10}"),
            "query-gizmos");
        var handles = registry.Query(GatewayGizmoQuery.ForSelection(
            10,
            new[] { "Zone" })).Items.ToDictionary(item => item.Label, item => item.Handle);

        var toggleResponse = Dispatch(
            dispatcher,
            router,
            Post("/api/v1/gizmos/" + handles["Draft"] + "/invoke", "{}"),
            "toggle-gizmo");
        var startResponse = Dispatch(
            dispatcher,
            router,
            Post("/api/v1/gizmos/" + handles["Growing zone"] + "/invoke", "{}"),
            "start-zone-drag");
        var interaction = registry.CurrentInteraction;
        Assert.That(interaction, Is.Not.Null);

        var currentResponse = Dispatch(
            dispatcher,
            router,
            Get("/api/v1/interactions/current"),
            "get-current-interaction");
        var applyResponse = Dispatch(
            dispatcher,
            router,
            Post(
                "/api/v1/interactions/" + interaction!.Handle + "/apply",
                "{\"kind\":\"rectangle\",\"cornerA\":{\"x\":3,\"z\":4}," +
                "\"cornerB\":{\"x\":4,\"z\":5}}"),
            "apply-zone-drag");
        var restartResponse = Dispatch(
            dispatcher,
            router,
            Post("/api/v1/gizmos/" + handles["Growing zone"] + "/invoke", "{}"),
            "restart-zone-drag");
        var cancelledInteraction = registry.CurrentInteraction;
        Assert.That(cancelledInteraction, Is.Not.Null);
        var cancelResponse = Dispatch(
            dispatcher,
            router,
            Post(
                "/api/v1/interactions/" + cancelledInteraction!.Handle + "/cancel",
                "{}"),
            "cancel-zone-drag");

        var queryBody = Encoding.UTF8.GetString(queryResponse.Body);
        var toggleBody = Encoding.UTF8.GetString(toggleResponse.Body);
        var startBody = Encoding.UTF8.GetString(startResponse.Body);
        var currentBody = Encoding.UTF8.GetString(currentResponse.Body);
        var applyBody = Encoding.UTF8.GetString(applyResponse.Body);
        var restartBody = Encoding.UTF8.GetString(restartResponse.Body);
        var cancelBody = Encoding.UTF8.GetString(cancelResponse.Body);
        Assert.Multiple(() =>
        {
            Assert.That(queryResponse.StatusCode, Is.EqualTo(200), queryBody);
            Assert.That(queryBody, Does.Contain("Growing zone"));
            Assert.That(toggleResponse.StatusCode, Is.EqualTo(200), toggleBody);
            Assert.That(toggleBody, Does.Contain("\"ToggleBefore\":false"));
            Assert.That(toggleBody, Does.Contain("\"ToggleAfter\":true"));
            Assert.That(startResponse.StatusCode, Is.EqualTo(200), startBody);
            Assert.That(startBody, Does.Contain(interaction.Handle));
            Assert.That(currentResponse.StatusCode, Is.EqualTo(200), currentBody);
            Assert.That(currentBody, Does.Contain(interaction.Handle));
            Assert.That(applyResponse.StatusCode, Is.EqualTo(200), applyBody);
            Assert.That(applyBody, Does.Contain("\"Completed\":true"));
            Assert.That(restartResponse.StatusCode, Is.EqualTo(200), restartBody);
            Assert.That(cancelResponse.StatusCode, Is.EqualTo(200), cancelBody);
            Assert.That(cancelBody, Does.Contain("\"Cancelled\":true"));
            Assert.That(drag.AppliedTargets.Select(TargetText), Is.EquivalentTo(new[]
            {
                "3,4", "4,4", "3,5", "4,5"
            }));
            Assert.That(registry.CurrentInteraction, Is.Null);
        });
    }

    [TestCase("0")]
    [TestCase("Cell,Rectangle")]
    public void Interaction_json_accepts_only_named_single_input_kinds(string kind)
    {
        var error = Assert.Throws<System.Runtime.Serialization.SerializationException>(() =>
            GatewayGizmoRequestJson.ReadInteractionInput(
                "{\"kind\":\"" + kind + "\",\"cell\":{\"x\":1,\"z\":2}}"));

        Assert.That(error?.Message, Does.Contain("Unknown interaction input kind"));
    }

    [Test]
    public void Interaction_json_accepts_one_cardinal_rotation_for_cell_or_line_input()
    {
        var input = GatewayGizmoRequestJson.ReadInteractionInput(
            "{\"kind\":\"cell\",\"cell\":{\"x\":5,\"z\":6},\"rotation\":\"East\"}");
        var line = GatewayGizmoRequestJson.ReadInteractionInput(
            "{\"kind\":\"line\",\"start\":{\"x\":5,\"z\":6},\"end\":{\"x\":2,\"z\":6},\"rotation\":\"North\"}");
        var diagonal = Assert.Throws<System.Runtime.Serialization.SerializationException>(() =>
            GatewayGizmoRequestJson.ReadInteractionInput(
                "{\"kind\":\"line\",\"start\":{\"x\":5,\"z\":6},\"end\":{\"x\":2,\"z\":3},\"rotation\":\"North\"}"));
        var wrongShape = Assert.Throws<System.Runtime.Serialization.SerializationException>(() =>
            GatewayGizmoRequestJson.ReadInteractionInput(
                "{\"kind\":\"rectangle\",\"cornerA\":{\"x\":1,\"z\":2}," +
                "\"cornerB\":{\"x\":3,\"z\":4},\"rotation\":\"West\"}"));
        var invalid = Assert.Throws<System.Runtime.Serialization.SerializationException>(() =>
            GatewayGizmoRequestJson.ReadInteractionInput(
                "{\"kind\":\"cell\",\"cell\":{\"x\":5,\"z\":6},\"rotation\":\"Diagonal\"}"));

        Assert.Multiple(() =>
        {
            Assert.That(input.Rotation, Is.EqualTo(GatewayCardinalRotation.East));
            Assert.That(line.Rotation, Is.EqualTo(GatewayCardinalRotation.North));
            Assert.That(diagonal?.Message, Does.Contain("cardinal"));
            Assert.That(wrongShape?.Message, Does.Contain("rotation"));
            Assert.That(invalid?.Message, Does.Contain("cardinal rotation"));
        });
    }

    [Test]
    public void Gizmo_discovery_failure_returns_500_and_logs_the_preserved_cause()
    {
        var dispatcher = new GatewayDispatcher();
        var logs = new GatewayLogBuffer();
        var registry = new GatewayGizmoRegistry(new ThrowingSource());
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            logs,
            new GatewayApiServices(gizmos: registry),
            responseTimeout: TimeSpan.FromSeconds(2));

        var response = Dispatch(
            dispatcher,
            router,
            Post(
                "/api/v1/gizmos/query",
                "{\"ownerScope\":\"selection\",\"limit\":10}"),
            "gizmo-discovery-failure");
        var body = Encoding.UTF8.GetString(response.Body);
        var diagnostic = logs.Read(0, 10).Entries.Single();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(500), body);
            Assert.That(body, Does.Contain("gizmo_discovery_failed"));
            Assert.That(diagnostic.Severity, Is.EqualTo("Error"));
            Assert.That(diagnostic.RequestId, Is.EqualTo("gizmo-discovery-failure"));
            Assert.That(diagnostic.Message, Does.Contain("gizmo discovery failed"));
            Assert.That(diagnostic.Stack, Does.Contain(nameof(InvalidOperationException)));
            Assert.That(diagnostic.Stack, Does.Contain("native gizmo failure"));
        });
    }

    private static string TargetText(GatewayInteractionTarget target) =>
        target.Cell!.X + "," + target.Cell.Z;

    private static GatewayHttpResponse Dispatch(
        GatewayDispatcher dispatcher,
        GatewayApiRouter router,
        GatewayHttpRequest request,
        string requestId)
    {
        var responseTask = Task.Run(() => router.Handle(request, requestId));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.Update);
        return responseTask.GetAwaiter().GetResult();
    }

    private static GatewayHttpRequest Post(string path, string json) => new(
        "POST",
        path,
        path,
        string.Empty,
        new Dictionary<string, string>(),
        Encoding.UTF8.GetBytes(json));

    private static GatewayHttpRequest Get(string path) => new(
        "GET",
        path,
        path,
        string.Empty,
        new Dictionary<string, string>(),
        Array.Empty<byte>());

    private sealed class StubStateProvider : IGatewayStateProvider
    {
        public object CaptureStatus() => new { };

        public object CaptureUiState() => new { };
    }

    private sealed class FakeSource : IGatewayGizmoSource
    {
        private readonly IReadOnlyList<IGatewayGizmoCandidate> candidates;

        public FakeSource(params IGatewayGizmoCandidate[] candidates)
        {
            this.candidates = candidates;
        }

        public GatewayGizmoDiscovery Discover(GatewayGizmoSourceQuery query) =>
            new(
                "map-1",
                new[] { "Pawn_1" },
                candidates.Take(query.MaximumCandidates).ToArray(),
                pageTruncated: candidates.Count > query.MaximumCandidates);
    }

    private sealed class ThrowingSource : IGatewayGizmoSource
    {
        public GatewayGizmoDiscovery Discover(GatewayGizmoSourceQuery query) =>
            throw new GatewayGizmoException(
                "gizmo_discovery_failed",
                "Gizmo discovery failed for a modded command.",
                new InvalidOperationException("native gizmo failure"));
    }

    private sealed class FakeCandidate : IGatewayGizmoCandidate
    {
        private readonly string identity;
        private readonly string label;
        private readonly GatewayGizmoInteractionKind kind;
        private readonly IReadOnlyList<GatewayInteractionInputKind> acceptedInputs;
        private bool toggleState;

        public FakeCandidate(
            string identity,
            string label,
            GatewayGizmoInteractionKind kind,
            IReadOnlyList<GatewayInteractionInputKind> acceptedInputs)
        {
            this.identity = identity;
            this.label = label;
            this.kind = kind;
            this.acceptedInputs = acceptedInputs;
        }

        public IReadOnlyList<GatewayInteractionTarget> AppliedTargets { get; private set; } =
            Array.Empty<GatewayInteractionTarget>();

        public GatewayGizmoCandidateSnapshot Capture() => new(
            identity,
            GatewayGizmoSource.Selection,
            new[] { "Pawn_1" },
            kind == GatewayGizmoInteractionKind.Drag
                ? "RimWorld.Designator_ZoneAdd_Growing"
                : "Verse.Command_Toggle",
            label,
            label + " description",
            order: 1f,
            disabled: false,
            disabledReason: null,
            hotKey: null,
            groupKey: -1,
            kind,
            kind == GatewayGizmoInteractionKind.Toggle ? toggleState : null,
            acceptedInputs);

        public void Invoke()
        {
            if (kind == GatewayGizmoInteractionKind.Toggle)
            {
                toggleState = !toggleState;
            }
        }

        public void Prepare(GatewayInteractionInput input)
        {
        }

        public GatewayTargetAcceptance Preflight(GatewayInteractionTarget target) =>
            new(true);

        public GatewayNativeApplyResult Apply(IReadOnlyList<GatewayInteractionTarget> targets)
        {
            AppliedTargets = targets.ToArray();
            return new GatewayNativeApplyResult(completed: true);
        }

        public void Cancel()
        {
        }
    }
}
