namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayDebugActionsTests
{
    [Test]
    public void Query_filters_native_debug_leaves_and_invokes_the_exact_immediate_handle()
    {
        var invoked = 0;
        var source = new FakeDebugActionSource(
            new FakeDebugActionCandidate(
                new GatewayDebugActionCandidateSnapshot(
                    identity: "native-spawn-weapon",
                    path: "Spawning\\Spawn weapon",
                    label: "Spawn weapon",
                    category: "Spawning",
                    allowedGameStates: "PlayingOnMap",
                    runtimeType: "LudeonTK.DebugActionNode",
                    mode: GatewayDebugActionMode.Immediate,
                    visible: true,
                    active: true,
                    on: false),
                () => invoked++),
            new FakeDebugActionCandidate(
                new GatewayDebugActionCandidateSnapshot(
                    identity: "native-log-memory",
                    path: "General\\Log memory",
                    label: "Log memory",
                    category: "General",
                    allowedGameStates: "Entry",
                    runtimeType: "LudeonTK.DebugActionNode",
                    mode: GatewayDebugActionMode.Immediate,
                    visible: true,
                    active: true,
                    on: false),
                () => { }));
        var registry = new GatewayDebugActionRegistry(source);

        var page = registry.Query(new GatewayDebugActionQuery(
            search: "spawn",
            categories: new[] { "Spawning" },
            modes: new[] { GatewayDebugActionMode.Immediate },
            after: null,
            limit: 10));
        var result = registry.Invoke(page.Items.Single().Handle);

        Assert.Multiple(() =>
        {
            Assert.That(page.Items, Has.Count.EqualTo(1));
            Assert.That(page.Items[0].Path, Is.EqualTo("Spawning\\Spawn weapon"));
            Assert.That(page.Items[0].Mode, Is.EqualTo(GatewayDebugActionMode.Immediate));
            Assert.That(page.PageTruncated, Is.False);
            Assert.That(result.Completed, Is.True);
            Assert.That(result.Path, Is.EqualTo("Spawning\\Spawn weapon"));
            Assert.That(invoked, Is.EqualTo(1));
        });
    }

    [Test]
    public void Native_map_tool_activation_reports_that_a_process_scoped_pointer_is_required()
    {
        var invoked = 0;
        var registry = new GatewayDebugActionRegistry(new FakeDebugActionSource(
            new FakeDebugActionCandidate(
                Snapshot("tool", "Spawning\\Spawn thing", GatewayDebugActionMode.MapPointer),
                () => invoked++)));
        var handle = registry.Query(new GatewayDebugActionQuery(
            null,
            null,
            new[] { GatewayDebugActionMode.MapPointer },
            null,
            10)).Items.Single().Handle;

        var result = registry.Invoke(handle);

        Assert.Multiple(() =>
        {
            Assert.That(invoked, Is.EqualTo(1));
            Assert.That(result.Mode, Is.EqualTo(GatewayDebugActionMode.MapPointer));
            Assert.That(result.Completed, Is.False);
            Assert.That(result.PointerRequired, Is.True);
        });
    }

    [Test]
    public void Stale_or_disabled_debug_action_never_invokes_a_neighboring_leaf()
    {
        var invoked = 0;
        var candidate = new MutableDebugActionCandidate(
            Snapshot("first", "General\\First", GatewayDebugActionMode.Immediate),
            () => invoked++);
        var registry = new GatewayDebugActionRegistry(new FakeDebugActionSource(candidate));
        var handle = registry.Query(new GatewayDebugActionQuery(
            null,
            null,
            null,
            null,
            10)).Items.Single().Handle;
        candidate.Snapshot = Snapshot(
            "replacement",
            "General\\Replacement",
            GatewayDebugActionMode.Immediate);

        var stale = Assert.Throws<GatewayDebugActionException>(() => registry.Invoke(handle));

        candidate.Snapshot = new GatewayDebugActionCandidateSnapshot(
            "disabled",
            "General\\Disabled",
            "Disabled",
            "General",
            "PlayingOnMap",
            "LudeonTK.DebugActionNode",
            GatewayDebugActionMode.Immediate,
            visible: true,
            active: false,
            on: false);
        var disabledHandle = registry.Query(new GatewayDebugActionQuery(
            null,
            null,
            null,
            null,
            10)).Items.Single().Handle;
        var disabled = Assert.Throws<GatewayDebugActionException>(
            () => registry.Invoke(disabledHandle));

        Assert.Multiple(() =>
        {
            Assert.That(stale?.Code, Is.EqualTo("stale_debug_action_handle"));
            Assert.That(disabled?.Code, Is.EqualTo("debug_action_unavailable"));
            Assert.That(invoked, Is.Zero);
        });
    }

    [TestCase("0")]
    [TestCase("Immediate,MapPointer")]
    public void Query_json_accepts_only_named_single_debug_modes(string mode)
    {
        var error = Assert.Throws<System.Runtime.Serialization.SerializationException>(() =>
            GatewayDebugActionRequestJson.ReadQuery(
                "{\"modes\":[\"" + mode + "\"]}"));

        Assert.That(error?.Message, Does.Contain("Unknown debug-action invocation mode"));
    }

    [Test]
    public void Discovery_exposes_the_debug_action_source_type_separately_from_the_node_type()
    {
        var source = new FakeDebugActionSource(new FakeDebugActionCandidate(
            new GatewayDebugActionCandidateSnapshot(
                identity: "native-log-pathfinder",
                path: "Pathing\\Log pathfinder state",
                label: "Log pathfinder state",
                category: "Pathing",
                allowedGameStates: "PlayingOnMap",
                runtimeType: "LudeonTK.DebugActionNode",
                mode: GatewayDebugActionMode.Immediate,
                visible: true,
                active: true,
                on: false,
                sourceType: "Verse.DebugActionsPathfinding"),
            () => { }));
        var registry = new GatewayDebugActionRegistry(source);

        var descriptor = registry.Query(new GatewayDebugActionQuery(
            null, null, null, null, 10)).Items.Single();

        Assert.Multiple(() =>
        {
            Assert.That(descriptor.RuntimeType, Is.EqualTo("LudeonTK.DebugActionNode"));
            Assert.That(descriptor.SourceType, Is.EqualTo("Verse.DebugActionsPathfinding"));
        });
    }

    [Test]
    public void Native_discovery_does_not_let_one_generated_submenu_starve_later_top_level_actions()
    {
        var root = new LudeonTK.DebugActionNode("Root");
        var generatedSubmenu = new LudeonTK.DebugActionNode("Generated submenu");
        for (var index = 0; index < 20; index++)
        {
            generatedSubmenu.AddChild(new LudeonTK.DebugActionNode(
                "Generated " + index,
                LudeonTK.DebugActionType.Action,
                () => { }));
        }

        root.AddChild(generatedSubmenu);
        root.AddChild(new LudeonTK.DebugActionNode(
            "Safe later action",
            LudeonTK.DebugActionType.Action,
            () => { }));

        var candidates = VerseGatewayDebugActionSource.DiscoverBreadthFirst(root, 2);
        var paths = candidates.Select(candidate => candidate.Capture().Path).ToArray();

        Assert.That(paths, Does.Contain("Safe later action"));
    }

    [Test]
    public void Native_discovery_never_expands_lazy_generated_submenus()
    {
        var getterCalls = 0;
        var root = new LudeonTK.DebugActionNode("Root");
        root.AddChild(new LudeonTK.DebugActionNode("Potentially huge submenu")
        {
            childGetter = () =>
            {
                getterCalls++;
                throw new InvalidOperationException("must not run during discovery");
            }
        });
        root.AddChild(new LudeonTK.DebugActionNode(
            "Safe action",
            LudeonTK.DebugActionType.Action,
            () => { }));

        var snapshots = VerseGatewayDebugActionSource.DiscoverBreadthFirst(root, 10)
            .Select(candidate => candidate.Capture())
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(getterCalls, Is.Zero);
            Assert.That(snapshots.Single(item => item.Path == "Potentially huge submenu").Mode,
                Is.EqualTo(GatewayDebugActionMode.Unsupported));
            Assert.That(snapshots.Single(item => item.Path == "Safe action").Mode,
                Is.EqualTo(GatewayDebugActionMode.Immediate));
        });
    }

    [Test]
    public void Misbehaving_mod_visibility_getter_is_isolated_in_the_descriptor()
    {
        var root = new LudeonTK.DebugActionNode(
            "Broken mod action",
            LudeonTK.DebugActionType.Action,
            () => { })
        {
            visibilityGetter = () => throw new InvalidOperationException("mod visibility failed")
        };

        var snapshot = VerseGatewayDebugActionSource.DiscoverBreadthFirst(root, 10)
            .Single()
            .Capture();

        Assert.Multiple(() =>
        {
            Assert.That(snapshot.Visible, Is.False);
            Assert.That(snapshot.DiscoveryError, Does.Contain("visibility"));
            Assert.That(snapshot.DiscoveryError, Does.Contain("mod visibility failed"));
        });
    }

    private static GatewayDebugActionCandidateSnapshot Snapshot(
        string identity,
        string path,
        GatewayDebugActionMode mode) =>
        new(
            identity,
            path,
            path.Substring(path.LastIndexOf('\\') + 1),
            path.Substring(0, path.IndexOf('\\')),
            "PlayingOnMap",
            "LudeonTK.DebugActionNode",
            mode,
            visible: true,
            active: true,
            on: false);

    private sealed class FakeDebugActionSource : IGatewayDebugActionSource
    {
        private readonly IReadOnlyList<IGatewayDebugActionCandidate> candidates;

        public FakeDebugActionSource(params IGatewayDebugActionCandidate[] candidates)
        {
            this.candidates = candidates;
        }

        public IReadOnlyList<IGatewayDebugActionCandidate> Discover(int maximumCandidates) =>
            candidates.Take(maximumCandidates).ToArray();
    }

    private sealed class FakeDebugActionCandidate : IGatewayDebugActionCandidate
    {
        private readonly GatewayDebugActionCandidateSnapshot snapshot;
        private readonly Action invoke;

        public FakeDebugActionCandidate(
            GatewayDebugActionCandidateSnapshot snapshot,
            Action invoke)
        {
            this.snapshot = snapshot;
            this.invoke = invoke;
        }

        public GatewayDebugActionCandidateSnapshot Capture() => snapshot;

        public void Invoke() => invoke();
    }

    private sealed class MutableDebugActionCandidate : IGatewayDebugActionCandidate
    {
        private readonly Action invoke;

        public MutableDebugActionCandidate(
            GatewayDebugActionCandidateSnapshot snapshot,
            Action invoke)
        {
            Snapshot = snapshot;
            this.invoke = invoke;
        }

        public GatewayDebugActionCandidateSnapshot Snapshot { get; set; }

        public GatewayDebugActionCandidateSnapshot Capture() => Snapshot;

        public void Invoke() => invoke();
    }
}
