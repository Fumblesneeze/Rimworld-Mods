using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayThingControlRouterTests
{
    [Test]
    public void View_query_returns_only_matching_pawns_intersecting_the_camera_view()
    {
        var dispatcher = new GatewayDispatcher();
        var operations = new FakeThingOperations(
            Thing("pawn-003", "Alice", "Pawn", "Verse.Pawn", "pawn", Rect(20, 20, 20, 20)),
            Thing("pawn-001", "Alice outside", "Pawn", "Verse.Pawn", "pawn", Rect(80, 80, 80, 80)),
            Thing("building-002", "Alice's table", "Table2x2c", "Verse.Building", "building", Rect(21, 21, 22, 22)),
            Thing("pawn-004", "Bob", "Pawn", "Verse.Pawn", "pawn", Rect(23, 23, 23, 23)));
        var router = Router(dispatcher, operations);
        var request = new GatewayThingQueryRequest
        {
            Scope = "view",
            Kinds = new List<string> { "pawn" },
            LabelContains = "ALI",
            Limit = 10
        };

        var responseTask = Task.Run(() => router.Handle(
            Post("/api/v1/things/query", GatewayContractJson.Write(request)),
            "query-visible-pawns"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.Update);
        var response = responseTask.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(body, Does.Contain("\"Handle\":\"pawn-003\""));
            Assert.That(body, Does.Contain("\"PageTruncated\":false"));
            Assert.That(body, Does.Not.Contain("pawn-001"));
            Assert.That(body, Does.Not.Contain("building-002"));
            Assert.That(body, Does.Not.Contain("pawn-004"));
            Assert.That(operations.CaptureThreadId, Is.EqualTo(Thread.CurrentThread.ManagedThreadId));
        });
    }

    [Test]
    public void Map_query_pages_in_stable_handle_order_without_repeating_the_cursor()
    {
        var dispatcher = new GatewayDispatcher();
        var operations = new FakeThingOperations(
            Thing("thing-c", "C", "Steel", "Verse.Thing", "item", Rect(20, 20, 20, 20)),
            Thing("thing-a", "A", "Steel", "Verse.Thing", "item", Rect(21, 21, 21, 21)),
            Thing("thing-b", "B", "Steel", "Verse.Thing", "item", Rect(22, 22, 22, 22)));
        var router = Router(dispatcher, operations);

        var first = Query(
            router,
            dispatcher,
            new GatewayThingQueryRequest { Scope = "map", Limit = 2 },
            "things-page-1");
        var second = Query(
            router,
            dispatcher,
            new GatewayThingQueryRequest { Scope = "map", Limit = 2, After = "thing-b" },
            "things-page-2");

        Assert.Multiple(() =>
        {
            Assert.That(first.StatusCode, Is.EqualTo(200));
            Assert.That(first.Body, Does.Contain("\"Handle\":\"thing-a\""));
            Assert.That(first.Body, Does.Contain("\"Handle\":\"thing-b\""));
            Assert.That(first.Body, Does.Not.Contain("thing-c"));
            Assert.That(first.Body.IndexOf("thing-a", StringComparison.Ordinal),
                Is.LessThan(first.Body.IndexOf("thing-b", StringComparison.Ordinal)));
            Assert.That(first.Body, Does.Contain("\"PageTruncated\":true"));
            Assert.That(second.StatusCode, Is.EqualTo(200));
            Assert.That(second.Body, Does.Contain("\"Handle\":\"thing-c\""));
            Assert.That(second.Body, Does.Not.Contain("thing-a"));
            Assert.That(second.Body, Does.Not.Contain("thing-b"));
            Assert.That(second.Body, Does.Contain("\"PageTruncated\":false"));
        });
    }

    [Test]
    public void Query_skips_failed_summaries_and_does_not_capture_candidates_outside_the_page()
    {
        var beforeCursor = Candidate(Thing(
            "thing-001", "Before", "Steel", "Verse.Thing", "item", Rect(20, 20, 20, 20)));
        var broken = new FakeThingCandidate(
            "thing-011",
            Rect(21, 21, 21, 21),
            () => throw new InvalidOperationException("Broken modded thing summary."));
        var first = Candidate(Thing(
            "thing-012", "First", "Steel", "Verse.Thing", "item", Rect(22, 22, 22, 22)));
        var overflow = Candidate(Thing(
            "thing-013", "Overflow", "Steel", "Verse.Thing", "item", Rect(23, 23, 23, 23)));
        var beyondPage = Candidate(Thing(
            "thing-014", "Beyond", "Steel", "Verse.Thing", "item", Rect(24, 24, 24, 24)));
        var controller = new GatewayThingController(new CandidateThingOperations(
            beforeCursor,
            broken,
            first,
            overflow,
            beyondPage));

        var result = controller.Query(new GatewayThingQueryRequest
        {
            Scope = "map",
            After = "thing-010",
            Limit = 1
        });

        Assert.Multiple(() =>
        {
            Assert.That(result.Things.Select(thing => thing.Handle), Is.EqualTo(new[] { "thing-012" }));
            Assert.That(result.PageTruncated, Is.True);
            Assert.That(beforeCursor.CaptureCount, Is.Zero);
            Assert.That(broken.CaptureCount, Is.EqualTo(1));
            Assert.That(first.CaptureCount, Is.EqualTo(1));
            Assert.That(overflow.CaptureCount, Is.EqualTo(1));
            Assert.That(beyondPage.CaptureCount, Is.Zero);
        });
    }

    [Test]
    public void Selection_snapshot_skips_a_failed_thing_summary_and_keeps_healthy_things()
    {
        var logs = new GatewayLogBuffer();
        var broken = new FakeThingCandidate(
            "thing-broken",
            Rect(20, 20, 20, 20),
            () => throw new InvalidOperationException("Broken modded selected Thing summary."));
        var healthy = Candidate(Thing(
            "thing-healthy",
            "Healthy",
            "Steel",
            "Verse.Thing",
            "item",
            Rect(21, 21, 21, 21)));
        var snapshot = new GatewayThingWorldSnapshot(
            "map-17",
            viewRect: null,
            new IGatewayThingCandidate[] { broken, healthy },
            diagnostics: logs);

        GatewayRequestScope.CurrentRequestId = "selected-summary-request";
        IReadOnlyList<GatewayThingSummary> selected;
        try
        {
            selected = snapshot.Things;
        }
        finally
        {
            GatewayRequestScope.CurrentRequestId = null;
        }

        var diagnostic = logs.Read(0, 10).Entries.Single();

        Assert.Multiple(() =>
        {
            Assert.That(selected.Select(thing => thing.Handle), Is.EqualTo(new[] { "thing-healthy" }));
            Assert.That(broken.CaptureCount, Is.EqualTo(1));
            Assert.That(healthy.CaptureCount, Is.EqualTo(1));
            Assert.That(diagnostic.Severity, Is.EqualTo("Warning"));
            Assert.That(diagnostic.RequestId, Is.EqualTo("selected-summary-request"));
            Assert.That(diagnostic.Message, Does.Contain("thing-broken"));
            Assert.That(diagnostic.Stack, Does.Contain(nameof(InvalidOperationException)));
            Assert.That(diagnostic.Stack, Does.Contain("Broken modded selected Thing summary."));
        });
    }

    [Test]
    public void Map_query_and_selection_do_not_require_a_camera()
    {
        var dispatcher = new GatewayDispatcher();
        var operations = new FakeThingOperations(
            Thing("thing-a", "A", "Steel", "Verse.Thing", "item", Rect(20, 20, 20, 20)),
            Thing("thing-b", "B", "Steel", "Verse.Thing", "item", Rect(21, 21, 21, 21)))
        {
            CameraAvailable = false
        };
        var router = Router(dispatcher, operations);

        var mapQuery = Query(
            router,
            dispatcher,
            new GatewayThingQueryRequest { Scope = "map", Limit = 10 },
            "map-query-without-camera");
        var selectionTask = Task.Run(() => router.Handle(
            Post(
                "/api/v1/selection",
                GatewayContractJson.Write(new GatewaySelectionRequest
                {
                    Operation = "replace",
                    Handles = new List<string> { "thing-b" }
                })),
            "selection-without-camera"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.Update);
        var selection = selectionTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(mapQuery.StatusCode, Is.EqualTo(200), mapQuery.Body);
            Assert.That(selection.StatusCode, Is.EqualTo(200), Encoding.UTF8.GetString(selection.Body));
            Assert.That(operations.SelectedHandles, Is.EqualTo(new[] { "thing-b" }));
        });
    }

    [Test]
    public void Query_excludes_overlong_opaque_handles_instead_of_truncating_them()
    {
        var overlong = new string('h', 257);
        var dispatcher = new GatewayDispatcher();
        var operations = new FakeThingOperations(
            Thing(overlong, "Too long", "Steel", "Verse.Thing", "item", Rect(20, 20, 20, 20)),
            Thing("thing-safe", "Safe", "Steel", "Verse.Thing", "item", Rect(21, 21, 21, 21)));
        var router = Router(dispatcher, operations);

        var response = Query(
            router,
            dispatcher,
            new GatewayThingQueryRequest { Scope = "map", Limit = 10 },
            "exclude-overlong-handle");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200), response.Body);
            Assert.That(response.Body, Does.Contain("\"Handle\":\"thing-safe\""));
            Assert.That(response.Body, Does.Contain("\"ExcludedUnaddressableCount\":1"));
            Assert.That(response.Body, Does.Not.Contain("[truncated]"));
            Assert.That(response.Body, Does.Not.Contain(overlong.Substring(0, 200)));
        });
    }

    [Test]
    public void Map_query_combines_exact_identity_faction_fog_and_selection_filters()
    {
        var dispatcher = new GatewayDispatcher();
        var player = new GatewayFactionSummary("Player colony", "PlayerColony", "Player");
        var operations = new FakeThingOperations(
            Thing("target", "Steel x75", "Steel", "Verse.Thing", "item", Rect(20, 20, 20, 20),
                faction: player, selected: true),
            Thing("wrong-def", "Wood x75", "WoodLog", "Verse.Thing", "item", Rect(21, 21, 21, 21),
                faction: player, selected: true),
            Thing("wrong-fog", "Steel x75", "Steel", "Verse.Thing", "item", Rect(22, 22, 22, 22),
                faction: player, fogged: true, selected: true),
            Thing("wrong-relation", "Steel x75", "Steel", "Verse.Thing", "item", Rect(23, 23, 23, 23),
                faction: new GatewayFactionSummary("Outlanders", "OutlanderCivil", "Ally"), selected: true),
            Thing("wrong-selection", "Steel x75", "Steel", "Verse.Thing", "item", Rect(24, 24, 24, 24),
                faction: player));
        var router = Router(dispatcher, operations);
        var request = new GatewayThingQueryRequest
        {
            Scope = "map",
            DefNames = new List<string> { "Steel" },
            Kinds = new List<string> { "item" },
            RuntimeTypes = new List<string> { "Verse.Thing" },
            FactionNames = new List<string> { "Player colony" },
            FactionRelations = new List<string> { "Player" },
            Fogged = false,
            Selected = true,
            Limit = 10
        };

        var response = Query(router, dispatcher, request, "things-exact-filters");

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(response.Body, Does.Contain("\"Handle\":\"target\""));
            Assert.That(response.Body, Does.Not.Contain("wrong-def"));
            Assert.That(response.Body, Does.Not.Contain("wrong-fog"));
            Assert.That(response.Body, Does.Not.Contain("wrong-relation"));
            Assert.That(response.Body, Does.Not.Contain("wrong-selection"));
        });
    }

    [Test]
    public void Thing_inspection_returns_bounded_scalar_pawn_details()
    {
        var dispatcher = new GatewayDispatcher();
        var summary = Thing(
            "pawn-003",
            "Alice",
            "Human",
            "Verse.Pawn",
            "pawn",
            Rect(20, 20, 20, 20));
        var components = Enumerable.Range(0, 80).Select(index => "Example.Component" + index).ToArray();
        var operations = new FakeThingOperations(summary)
        {
            Inspection = new GatewayThingInspection(
                summary,
                new string('d', 9000) + "DESCRIPTION-END",
                new string('i', 9000) + "INSPECT-END",
                components,
                item: null,
                building: null,
                pawn: new GatewayPawnDetails(
                    "Colonist",
                    "Human",
                    null,
                    "Female",
                    31.5f,
                    34.25f,
                    dead: false,
                    downed: false,
                    drafted: true,
                    "Cook",
                    new[] { new GatewayPawnSkillSummary("Cooking", 8) },
                    new[] { new GatewayPawnHealthConditionSummary("Flu", "Flu", 0.2f) }),
                new[] { new GatewayInspectionWarning("inspectText", "example warning") })
        };
        var router = Router(dispatcher, operations);

        var responseTask = Task.Run(() => router.Handle(
            Get("/api/v1/things/pawn-003"),
            "inspect-pawn"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.Update);
        var response = responseTask.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(body, Does.Contain("\"Handle\":\"pawn-003\""));
            Assert.That(body, Does.Contain("\"KindDefName\":\"Colonist\""));
            Assert.That(body, Does.Contain("\"DefName\":\"Cooking\""));
            Assert.That(body, Does.Contain("\"Level\":8"));
            Assert.That(body, Does.Contain("\"Label\":\"Flu\""));
            Assert.That(body, Does.Contain("[truncated]"));
            Assert.That(body, Does.Not.Contain("DESCRIPTION-END"));
            Assert.That(body, Does.Not.Contain("INSPECT-END"));
            Assert.That(body, Does.Contain("Example.Component63"));
            Assert.That(body, Does.Not.Contain("Example.Component64"));
            Assert.That(body, Does.Contain("example warning"));
        });
    }

    [Test]
    public void Selection_with_one_stale_handle_leaves_the_complete_prior_selection_unchanged()
    {
        var dispatcher = new GatewayDispatcher();
        var operations = new FakeThingOperations(
            Thing("thing-a", "A", "Steel", "Verse.Thing", "item", Rect(20, 20, 20, 20)),
            Thing("thing-b", "B", "Steel", "Verse.Thing", "item", Rect(21, 21, 21, 21)));
        operations.SelectedHandles.Add("thing-a");
        var router = Router(dispatcher, operations);
        var request = new GatewaySelectionRequest
        {
            Operation = "add",
            Handles = new List<string> { "thing-b", "missing" }
        };

        var responseTask = Task.Run(() => router.Handle(
            Post("/api/v1/selection", GatewayContractJson.Write(request)),
            "selection-stale"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.Update);
        var response = responseTask.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(404));
            Assert.That(body, Does.Contain("\"code\":\"thing_not_found\""));
            Assert.That(operations.ApplySelectionCount, Is.Zero);
            Assert.That(operations.SelectedHandles, Is.EqualTo(new[] { "thing-a" }));
        });
    }

    [TestCase("1")]
    [TestCase("add,remove")]
    public void Selection_accepts_only_named_single_operation_values(string operation)
    {
        var operations = new FakeThingOperations(
            Thing("thing-a", "A", "Steel", "Verse.Thing", "item", Rect(20, 20, 20, 20)));
        var controller = new GatewayThingController(operations);

        var error = Assert.Throws<GatewayThingControlException>(() => controller.MutateSelection(
            new GatewaySelectionRequest
            {
                Operation = operation,
                Handles = new List<string> { "thing-a" }
            }));

        Assert.Multiple(() =>
        {
            Assert.That(error?.Code, Is.EqualTo("invalid_selection_operation"));
            Assert.That(operations.SelectedHandles, Is.Empty);
        });
    }

    [Test]
    public void Selection_replace_removes_duplicates_and_preserves_caller_order()
    {
        var dispatcher = new GatewayDispatcher();
        var operations = new FakeThingOperations(
            Thing("thing-a", "A", "Steel", "Verse.Thing", "item", Rect(20, 20, 20, 20)),
            Thing("thing-b", "B", "Steel", "Verse.Thing", "item", Rect(21, 21, 21, 21)),
            Thing("thing-c", "C", "Steel", "Verse.Thing", "item", Rect(22, 22, 22, 22)));
        operations.SelectedHandles.Add("thing-a");
        var router = Router(dispatcher, operations);
        var request = new GatewaySelectionRequest
        {
            Operation = "replace",
            Handles = new List<string> { "thing-c", "thing-b", "thing-c" }
        };

        var responseTask = Task.Run(() => router.Handle(
            Post("/api/v1/selection", GatewayContractJson.Write(request)),
            "selection-replace"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.Update);
        var response = responseTask.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(operations.ApplySelectionCount, Is.EqualTo(1));
            Assert.That(operations.SelectedHandles, Is.EqualTo(new[] { "thing-c", "thing-b" }));
            Assert.That(body, Does.Contain("\"RequestedHandles\":[\"thing-c\",\"thing-b\"]"));
            Assert.That(body, Does.Contain("\"Before\":[{\"DefName\":\"Steel\""));
            Assert.That(body.IndexOf("\"Handle\":\"thing-c\"", StringComparison.Ordinal),
                Is.LessThan(body.IndexOf("\"Handle\":\"thing-b\"", StringComparison.Ordinal)));
        });
    }

    [Test]
    public void Selection_accepts_the_native_limit_of_two_hundred_handles()
    {
        var things = Enumerable.Range(0, 200)
            .Select(index => Thing(
                "thing-" + index.ToString("D3"),
                "Thing " + index,
                "Steel",
                "Verse.Thing",
                "item",
                Rect(20, 20, 20, 20)))
            .ToArray();
        var operations = new FakeThingOperations(things);
        var controller = new GatewayThingController(operations);

        var result = controller.MutateSelection(new GatewaySelectionRequest
        {
            Operation = "replace",
            Handles = things.Select(thing => thing.Handle).ToList()
        });

        Assert.That(result.After, Has.Count.EqualTo(200));
    }

    [TestCase(201)]
    [TestCase(256)]
    public void Selection_rejects_requests_above_the_native_limit_before_world_access(int count)
    {
        var operations = new FakeThingOperations();
        var controller = new GatewayThingController(operations);
        var handles = Enumerable.Range(0, count)
            .Select(index => "thing-" + index.ToString("D3"))
            .ToList();

        var error = Assert.Throws<GatewayThingControlException>(() => controller.MutateSelection(
            new GatewaySelectionRequest { Operation = "replace", Handles = handles }));

        Assert.Multiple(() =>
        {
            Assert.That(error?.Code, Is.EqualTo("selection_handle_limit_exceeded"));
            Assert.That(operations.CaptureWorldCount, Is.Zero);
        });
    }

    private static GatewayApiRouter Router(
        GatewayDispatcher dispatcher,
        IGatewayThingOperations operations) =>
        new(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(things: new GatewayThingController(operations)),
            responseTimeout: TimeSpan.FromSeconds(2));

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

    private static RoutedResponse Query(
        GatewayApiRouter router,
        GatewayDispatcher dispatcher,
        GatewayThingQueryRequest request,
        string requestId)
    {
        var responseTask = Task.Run(() => router.Handle(
            Post("/api/v1/things/query", GatewayContractJson.Write(request)),
            requestId));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.Update);
        var response = responseTask.GetAwaiter().GetResult();
        return new RoutedResponse(response.StatusCode, Encoding.UTF8.GetString(response.Body));
    }

    private static GatewayThingSummary Thing(
        string handle,
        string label,
        string defName,
        string runtimeType,
        string kind,
        GatewayMapRect occupiedRect,
        GatewayFactionSummary? faction = null,
        bool fogged = false,
        bool selected = false) =>
        new(
            handle,
            label,
            defName,
            runtimeType,
            kind,
            "map-17",
            new GatewayMapCell(occupiedRect.MinX, occupiedRect.MinZ),
            occupiedRect,
            "North",
            1,
            faction,
            hitPoints: null,
            quality: null,
            fogged,
            forbidden: false,
            selected,
            pawnFlags: kind == "pawn" ? new GatewayPawnFlags(false, false, true, false, false) : null);

    private static GatewayMapRect Rect(int minX, int minZ, int maxX, int maxZ) =>
        new(minX, minZ, maxX, maxZ);

    private static FakeThingCandidate Candidate(GatewayThingSummary summary) =>
        new(summary.Handle, summary.OccupiedRect, () => summary);

    private sealed class FakeThingCandidate : IGatewayThingCandidate
    {
        private readonly Func<GatewayThingSummary> capture;

        public FakeThingCandidate(
            string handle,
            GatewayMapRect occupiedRect,
            Func<GatewayThingSummary> capture)
        {
            Handle = handle;
            OccupiedRect = occupiedRect;
            this.capture = capture;
        }

        public string Handle { get; }

        public GatewayMapRect OccupiedRect { get; }

        public int CaptureCount { get; private set; }

        public GatewayThingSummary Capture()
        {
            CaptureCount++;
            return capture();
        }
    }

    private sealed class CandidateThingOperations : IGatewayThingOperations
    {
        private readonly IReadOnlyList<IGatewayThingCandidate> candidates;

        public CandidateThingOperations(params IGatewayThingCandidate[] candidates)
        {
            this.candidates = candidates;
        }

        public GatewayThingWorldSnapshot CaptureWorld(bool includeViewRect) =>
            new("map-17", includeViewRect ? Rect(10, 10, 40, 40) : null, candidates);

        public GatewayThingInspection CaptureInspection(string handle) =>
            throw new NotSupportedException();

        public IReadOnlyList<GatewayThingSummary> CaptureSelection() =>
            Array.Empty<GatewayThingSummary>();

        public void ApplySelection(
            GatewaySelectionOperation operation,
            IReadOnlyList<string> handles) => throw new NotSupportedException();
    }

    private sealed class FakeThingOperations : IGatewayThingOperations
    {
        private readonly IReadOnlyList<GatewayThingSummary> things;

        public FakeThingOperations(params GatewayThingSummary[] things)
        {
            this.things = things;
        }

        public int CaptureThreadId { get; private set; }

        public GatewayThingInspection? Inspection { get; set; }

        public List<string> SelectedHandles { get; } = new();

        public int ApplySelectionCount { get; private set; }

        public int CaptureWorldCount { get; private set; }

        public bool CameraAvailable { get; set; } = true;

        public GatewayThingWorldSnapshot CaptureWorld(bool includeViewRect)
        {
            CaptureWorldCount++;
            if (includeViewRect && !CameraAvailable)
            {
                throw new GatewayThingControlException(
                    "camera_unavailable",
                    "The fake map has no camera.");
            }

            CaptureThreadId = Thread.CurrentThread.ManagedThreadId;
            return new GatewayThingWorldSnapshot(
                "map-17",
                includeViewRect ? Rect(10, 10, 40, 40) : null,
                things);
        }

        public GatewayThingInspection CaptureInspection(string handle) =>
            Inspection ?? throw new GatewayThingControlException(
                "thing_not_found",
                $"Thing '{handle}' is not on the current map.");

        public IReadOnlyList<GatewayThingSummary> CaptureSelection() =>
            SelectedHandles
                .Select(handle => things.Single(thing => thing.Handle == handle))
                .ToList();

        public void ApplySelection(
            GatewaySelectionOperation operation,
            IReadOnlyList<string> handles)
        {
            ApplySelectionCount++;
            switch (operation)
            {
                case GatewaySelectionOperation.Replace:
                    SelectedHandles.Clear();
                    SelectedHandles.AddRange(handles);
                    break;
                case GatewaySelectionOperation.Add:
                    foreach (var handle in handles)
                    {
                        if (!SelectedHandles.Contains(handle))
                        {
                            SelectedHandles.Add(handle);
                        }
                    }

                    break;
                case GatewaySelectionOperation.Remove:
                    SelectedHandles.RemoveAll(handles.Contains);
                    break;
                case GatewaySelectionOperation.Toggle:
                    foreach (var handle in handles)
                    {
                        if (!SelectedHandles.Remove(handle))
                        {
                            SelectedHandles.Add(handle);
                        }
                    }

                    break;
                case GatewaySelectionOperation.Clear:
                    SelectedHandles.Clear();
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(operation));
            }
        }
    }

    private sealed class StubStateProvider : IGatewayStateProvider
    {
        public object CaptureStatus() => new { };

        public object CaptureUiState() => new { };
    }

    private sealed class RoutedResponse
    {
        public RoutedResponse(int statusCode, string body)
        {
            StatusCode = statusCode;
            Body = body;
        }

        public int StatusCode { get; }

        public string Body { get; }
    }
}
