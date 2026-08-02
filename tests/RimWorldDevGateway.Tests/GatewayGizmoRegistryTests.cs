namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayGizmoRegistryTests
{
    [Test]
    public void Selection_discovery_returns_a_stable_bounded_descriptor()
    {
        var candidate = FakeCandidate.Toggle("power", "Thing_SolarGenerator1", active: false);
        var source = new FakeSource(candidate);
        var registry = new GatewayGizmoRegistry(source);

        var first = registry.Query(GatewayGizmoQuery.ForSelection(limit: 10));
        var second = registry.Query(GatewayGizmoQuery.ForSelection(limit: 10));

        Assert.Multiple(() =>
        {
            Assert.That(first.Items, Has.Count.EqualTo(1));
            Assert.That(first.Items[0].Handle, Is.EqualTo(second.Items[0].Handle));
            Assert.That(first.Items[0].Revision, Is.EqualTo(first.Revision));
            Assert.That(first.Items[0].OwnerHandles, Is.EqualTo(new[] { "Thing_SolarGenerator1" }));
            Assert.That(first.Items[0].InteractionKind, Is.EqualTo(GatewayGizmoInteractionKind.Toggle));
            Assert.That(first.Items[0].ToggleState, Is.False);
            Assert.That(first.PageTruncated, Is.False);
            Assert.That(source.LastQuery!.OwnerScope, Is.EqualTo(GatewayGizmoOwnerScope.Selection));
            Assert.That(source.LastQuery.MaximumCandidates, Is.EqualTo(11));
        });
    }

    [Test]
    public void Broken_candidate_capture_does_not_hide_healthy_gizmos_or_break_revalidation()
    {
        var broken = FakeCandidate.Broken("Broken modded gizmo metadata.");
        var healthy = FakeCandidate.Immediate("cancel-job", "Thing_Colonist1");
        var source = new FakeSource(broken, healthy);
        var registry = new GatewayGizmoRegistry(source);

        var query = registry.Query(GatewayGizmoQuery.ForSelection());
        var result = registry.Invoke(query.Items.Single().Handle);

        Assert.Multiple(() =>
        {
            Assert.That(query.Items.Select(item => item.Label), Is.EqualTo(new[] { "Cancel job" }));
            Assert.That(result.Completed, Is.True);
            Assert.That(healthy.InvocationCount, Is.EqualTo(1));
            Assert.That(source.DiscoveryCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void Immediate_invocation_revalidates_and_runs_the_exact_action_once()
    {
        var candidate = FakeCandidate.Immediate("cancel-job", "Thing_Colonist1");
        var source = new FakeSource(candidate);
        var registry = new GatewayGizmoRegistry(source);
        var handle = registry.Query(GatewayGizmoQuery.ForSelection()).Items.Single().Handle;

        var result = registry.Invoke(handle);

        Assert.Multiple(() =>
        {
            Assert.That(result.Completed, Is.True);
            Assert.That(result.Interaction, Is.Null);
            Assert.That(candidate.InvocationCount, Is.EqualTo(1));
            Assert.That(source.DiscoveryCount, Is.EqualTo(2));
            Assert.That(registry.CurrentInteraction, Is.Null);
        });
    }

    [Test]
    public void Toggle_invocation_reports_the_observed_transition_and_runs_once()
    {
        var candidate = FakeCandidate.Toggle("power", "Thing_SolarGenerator1", active: false);
        var registry = new GatewayGizmoRegistry(new FakeSource(candidate));
        var handle = registry.Query(GatewayGizmoQuery.ForSelection()).Items.Single().Handle;

        var result = registry.Invoke(handle);

        Assert.Multiple(() =>
        {
            Assert.That(result.Completed, Is.True);
            Assert.That(result.ToggleBefore, Is.False);
            Assert.That(result.ToggleAfter, Is.True);
            Assert.That(candidate.InvocationCount, Is.EqualTo(1));
            Assert.That(registry.CurrentInteraction, Is.Null);
        });
    }

    [Test]
    public void Target_invocation_starts_the_only_active_interaction()
    {
        var candidate = FakeCandidate.Target("set-target", "Thing_Turret1");
        var registry = new GatewayGizmoRegistry(new FakeSource(candidate));
        var gizmoHandle = registry.Query(GatewayGizmoQuery.ForSelection()).Items.Single().Handle;

        var result = registry.Invoke(gizmoHandle);
        var error = Assert.Throws<GatewayGizmoException>(() => registry.Invoke(gizmoHandle));

        Assert.Multiple(() =>
        {
            Assert.That(result.Completed, Is.False);
            Assert.That(result.Interaction, Is.SameAs(registry.CurrentInteraction));
            Assert.That(result.Interaction!.SourceHandle, Is.EqualTo(gizmoHandle));
            Assert.That(result.Interaction.Kind, Is.EqualTo(GatewayGizmoInteractionKind.Target));
            Assert.That(result.Interaction.AcceptedInputs,
                Is.EqualTo(new[] { GatewayInteractionInputKind.Thing, GatewayInteractionInputKind.Cell }));
            Assert.That(error!.Code, Is.EqualTo("interaction_in_progress"));
            Assert.That(candidate.InvocationCount, Is.Zero);
        });
    }

    [Test]
    public void Target_apply_preflights_and_delivers_the_exact_thing_then_completes()
    {
        var candidate = FakeCandidate.Target("set-target", "Thing_Turret1");
        var registry = new GatewayGizmoRegistry(new FakeSource(candidate));
        var gizmoHandle = registry.Query(GatewayGizmoQuery.ForSelection()).Items.Single().Handle;
        var interaction = registry.Invoke(gizmoHandle).Interaction!;

        var result = registry.Apply(
            interaction.Handle,
            GatewayInteractionInput.ForThing("Thing_Raider42"));

        Assert.Multiple(() =>
        {
            Assert.That(result.Completed, Is.True);
            Assert.That(result.Accepted, Has.Count.EqualTo(1));
            Assert.That(result.Rejected, Is.Empty);
            Assert.That(candidate.Preflighted.Select(target => target.ThingHandle),
                Is.EqualTo(new[] { "Thing_Raider42" }));
            Assert.That(candidate.Applied.Select(target => target.ThingHandle),
                Is.EqualTo(new[] { "Thing_Raider42" }));
            Assert.That(registry.CurrentInteraction, Is.Null);
        });
    }

    [Test]
    public void Cancel_refuses_a_non_current_handle_then_cancels_the_matching_interaction()
    {
        var candidate = FakeCandidate.Target("set-target", "Thing_Turret1");
        var registry = new GatewayGizmoRegistry(new FakeSource(candidate));
        var gizmoHandle = registry.Query(GatewayGizmoQuery.ForSelection()).Items.Single().Handle;
        var interaction = registry.Invoke(gizmoHandle).Interaction!;

        var error = Assert.Throws<GatewayGizmoException>(() => registry.Cancel("interaction_other"));
        var result = registry.Cancel(interaction.Handle);

        Assert.Multiple(() =>
        {
            Assert.That(error!.Code, Is.EqualTo("interaction_not_current"));
            Assert.That(result.Cancelled, Is.True);
            Assert.That(result.InteractionHandle, Is.EqualTo(interaction.Handle));
            Assert.That(candidate.CancelCount, Is.EqualTo(1));
            Assert.That(registry.CurrentInteraction, Is.Null);
        });
    }

    [Test]
    public void Line_input_expands_deterministically_and_preflights_every_cell_before_apply()
    {
        var candidate = FakeCandidate.Drag(
            "build-wall",
            target => target.Cell!.X == 2
                ? new GatewayTargetAcceptance(false, "Blocked")
                : new GatewayTargetAcceptance(true));
        var registry = new GatewayGizmoRegistry(new FakeSource(candidate));
        var gizmoHandle = registry.Query(GatewayGizmoQuery.ForSelection()).Items.Single().Handle;
        var interaction = registry.Invoke(gizmoHandle).Interaction!;

        var result = registry.Apply(
            interaction.Handle,
            GatewayInteractionInput.ForLine(new GatewayMapCell(1, 3), new GatewayMapCell(4, 3)));

        Assert.Multiple(() =>
        {
            Assert.That(candidate.Preflighted.Select(CellText),
                Is.EqualTo(new[] { "1,3", "2,3", "3,3", "4,3" }));
            Assert.That(candidate.Applied.Select(CellText),
                Is.EqualTo(new[] { "1,3", "3,3", "4,3" }));
            Assert.That(result.Rejected.Select(item => CellText(item.Target)),
                Is.EqualTo(new[] { "2,3" }));
            Assert.That(result.Rejected[0].Reason, Is.EqualTo("Blocked"));
            Assert.That(result.Completed, Is.True);
        });
    }

    [Test]
    public void Rectangle_input_expands_to_each_unique_cell_in_row_major_order()
    {
        var candidate = FakeCandidate.Drag(
            "create-zone",
            _ => new GatewayTargetAcceptance(true));
        var registry = new GatewayGizmoRegistry(new FakeSource(candidate));
        var gizmoHandle = registry.Query(GatewayGizmoQuery.ForSelection()).Items.Single().Handle;
        var interaction = registry.Invoke(gizmoHandle).Interaction!;

        var result = registry.Apply(
            interaction.Handle,
            GatewayInteractionInput.ForRectangle(
                new GatewayMapCell(2, 4),
                new GatewayMapCell(1, 3)));

        Assert.Multiple(() =>
        {
            Assert.That(candidate.Preflighted.Select(CellText),
                Is.EqualTo(new[] { "1,3", "2,3", "1,4", "2,4" }));
            Assert.That(candidate.Applied.Select(CellText),
                Is.EqualTo(new[] { "1,3", "2,3", "1,4", "2,4" }));
            Assert.That(result.Accepted, Has.Count.EqualTo(4));
            Assert.That(result.Rejected, Is.Empty);
        });
    }

    [Test]
    public void Invocation_rejects_a_reordered_ambiguous_command_list_as_stale()
    {
        var first = FakeCandidate.Immediate("cancel-job-a", "Thing_Colonist1");
        var second = FakeCandidate.Immediate("cancel-job-b", "Thing_Colonist1");
        var source = new FakeSource(first, second);
        var registry = new GatewayGizmoRegistry(source);
        var handle = registry.Query(GatewayGizmoQuery.ForSelection()).Items[0].Handle;
        source.Replace(second, first);

        var error = Assert.Throws<GatewayGizmoException>(() => registry.Invoke(handle));

        Assert.Multiple(() =>
        {
            Assert.That(error!.Code, Is.EqualTo("stale_gizmo_handle"));
            Assert.That(first.InvocationCount, Is.Zero);
            Assert.That(second.InvocationCount, Is.Zero);
        });
    }

    [Test]
    public void Current_interaction_revalidates_and_clears_a_stale_source()
    {
        var original = FakeCandidate.Target("set-target", "Thing_Turret1");
        var replacement = FakeCandidate.Target("replacement-target", "Thing_Turret1");
        var source = new FakeSource(original);
        var registry = new GatewayGizmoRegistry(source);
        var handle = registry.Query(GatewayGizmoQuery.ForSelection()).Items.Single().Handle;
        registry.Invoke(handle);
        source.Replace(replacement);

        var error = Assert.Throws<GatewayGizmoException>(() =>
        {
            _ = registry.CurrentInteraction;
        });

        Assert.Multiple(() =>
        {
            Assert.That(error?.Code, Is.EqualTo("stale_interaction"));
            Assert.That(registry.CurrentInteraction, Is.Null);
            Assert.That(original.InvocationCount, Is.Zero);
            Assert.That(replacement.InvocationCount, Is.Zero);
        });
    }

    [Test]
    public void Discovery_rejects_more_than_the_bounded_owner_count()
    {
        var source = new FakeSource(FakeCandidate.Immediate("noop", "Thing_1"))
        {
            ResolvedOwnerHandles = Enumerable.Range(1, GatewayGizmoRegistry.MaximumOwners + 1)
                .Select(index => "Thing_" + index)
                .ToArray()
        };
        var registry = new GatewayGizmoRegistry(source);

        var error = Assert.Throws<GatewayGizmoException>(() =>
            registry.Query(GatewayGizmoQuery.ForSelection()));

        Assert.That(error!.Code, Is.EqualTo("too_many_gizmo_owners"));
    }

    [Test]
    public void Oversized_rectangle_is_rejected_before_any_native_preflight()
    {
        var candidate = FakeCandidate.Drag(
            "create-zone",
            _ => new GatewayTargetAcceptance(true));
        var registry = new GatewayGizmoRegistry(new FakeSource(candidate));
        var gizmoHandle = registry.Query(GatewayGizmoQuery.ForSelection()).Items.Single().Handle;
        var interaction = registry.Invoke(gizmoHandle).Interaction!;

        var error = Assert.Throws<GatewayGizmoException>(() => registry.Apply(
            interaction.Handle,
            GatewayInteractionInput.ForRectangle(
                new GatewayMapCell(int.MinValue, int.MinValue),
                new GatewayMapCell(int.MaxValue, int.MaxValue))));

        Assert.Multiple(() =>
        {
            Assert.That(error!.Code, Is.EqualTo("interaction_target_limit"));
            Assert.That(candidate.Preflighted, Is.Empty);
            Assert.That(candidate.Applied, Is.Empty);
            Assert.That(registry.CurrentInteraction, Is.Not.Null);
        });
    }

    private static string CellText(GatewayInteractionTarget target) =>
        target.Cell!.X + "," + target.Cell.Z;

    private sealed class FakeSource : IGatewayGizmoSource
    {
        private IReadOnlyList<IGatewayGizmoCandidate> candidates;

        public FakeSource(params IGatewayGizmoCandidate[] candidates)
        {
            this.candidates = candidates;
        }

        public GatewayGizmoSourceQuery? LastQuery { get; private set; }

        public int DiscoveryCount { get; private set; }

        public IReadOnlyList<string> ResolvedOwnerHandles { get; set; } =
            new[] { "Thing_SolarGenerator1" };

        public void Replace(params IGatewayGizmoCandidate[] replacements)
        {
            candidates = replacements;
        }

        public GatewayGizmoDiscovery Discover(GatewayGizmoSourceQuery query)
        {
            DiscoveryCount++;
            LastQuery = query;
            return new GatewayGizmoDiscovery(
                "Map_7",
                ResolvedOwnerHandles,
                candidates,
                pageTruncated: false);
        }
    }

    private sealed class FakeCandidate : IGatewayGizmoCandidate
    {
        private readonly Func<GatewayGizmoCandidateSnapshot> capture;
        private readonly Action invoke;
        private readonly Func<GatewayInteractionTarget, GatewayTargetAcceptance> preflight;
        private readonly Func<IReadOnlyList<GatewayInteractionTarget>, GatewayNativeApplyResult> apply;
        private readonly Action cancel;

        private FakeCandidate(
            GatewayGizmoCandidateSnapshot snapshot,
            Action? invoke = null,
            Func<GatewayInteractionTarget, GatewayTargetAcceptance>? preflight = null,
            Func<IReadOnlyList<GatewayInteractionTarget>, GatewayNativeApplyResult>? apply = null,
            Action? cancel = null)
            : this(() => snapshot, invoke, preflight, apply, cancel)
        {
        }

        private FakeCandidate(
            Func<GatewayGizmoCandidateSnapshot> capture,
            Action? invoke = null,
            Func<GatewayInteractionTarget, GatewayTargetAcceptance>? preflight = null,
            Func<IReadOnlyList<GatewayInteractionTarget>, GatewayNativeApplyResult>? apply = null,
            Action? cancel = null)
        {
            this.capture = capture;
            this.invoke = invoke ?? (() => throw new NotSupportedException());
            this.preflight = preflight ?? (_ => throw new NotSupportedException());
            this.apply = apply ?? (_ => throw new NotSupportedException());
            this.cancel = cancel ?? (() => throw new NotSupportedException());
        }

        public int InvocationCount { get; private set; }

        public List<GatewayInteractionTarget> Preflighted { get; } = new();

        public List<GatewayInteractionTarget> Applied { get; } = new();

        public int CancelCount { get; private set; }

        public static FakeCandidate Immediate(string identity, string ownerHandle)
        {
            FakeCandidate? candidate = null;
            candidate = new FakeCandidate(
                new GatewayGizmoCandidateSnapshot(
                    identity,
                    GatewayGizmoSource.Selection,
                    new[] { ownerHandle },
                    "Verse.Command_Action",
                    "Cancel job",
                    "Cancel the selected pawn's current job.",
                    order: 5f,
                    disabled: false,
                    disabledReason: null,
                    hotKey: null,
                    groupKey: -1,
                    GatewayGizmoInteractionKind.Immediate,
                    toggleState: null,
                    Array.Empty<GatewayInteractionInputKind>()),
                () => candidate!.InvocationCount++);
            return candidate;
        }

        public static FakeCandidate Broken(string message) =>
            new(() => throw new InvalidOperationException(message));

        public static FakeCandidate Target(string identity, string ownerHandle)
        {
            FakeCandidate? candidate = null;
            candidate = new FakeCandidate(new GatewayGizmoCandidateSnapshot(
                identity,
                GatewayGizmoSource.Selection,
                new[] { ownerHandle },
                "Verse.Command_Target",
                "Set target",
                "Choose a thing or cell to target.",
                order: 20f,
                disabled: false,
                disabledReason: null,
                hotKey: null,
                groupKey: -1,
                GatewayGizmoInteractionKind.Target,
                toggleState: null,
                new[] { GatewayInteractionInputKind.Thing, GatewayInteractionInputKind.Cell }),
                preflight: target =>
                {
                    candidate!.Preflighted.Add(target);
                    return new GatewayTargetAcceptance(accepted: true);
                },
                apply: targets =>
                {
                    candidate!.Applied.AddRange(targets);
                    return new GatewayNativeApplyResult(completed: true);
                },
                cancel: () => candidate!.CancelCount++);
            return candidate;
        }

        public static FakeCandidate Drag(
            string identity,
            Func<GatewayInteractionTarget, GatewayTargetAcceptance> validator)
        {
            FakeCandidate? candidate = null;
            candidate = new FakeCandidate(new GatewayGizmoCandidateSnapshot(
                identity,
                GatewayGizmoSource.Architect,
                Array.Empty<string>(),
                "RimWorld.Designator_Build",
                "Granite wall",
                "Designate a wall line.",
                order: 30f,
                disabled: false,
                disabledReason: null,
                hotKey: null,
                groupKey: -1,
                GatewayGizmoInteractionKind.Drag,
                toggleState: null,
                new[]
                {
                    GatewayInteractionInputKind.Cells,
                    GatewayInteractionInputKind.Line,
                    GatewayInteractionInputKind.Rectangle
                }),
                preflight: target =>
                {
                    candidate!.Preflighted.Add(target);
                    return validator(target);
                },
                apply: targets =>
                {
                    candidate!.Applied.AddRange(targets);
                    return new GatewayNativeApplyResult(completed: true);
                },
                cancel: () => candidate!.CancelCount++);
            return candidate;
        }

        public static FakeCandidate Toggle(string identity, string ownerHandle, bool active)
        {
            var current = active;
            FakeCandidate? candidate = null;
            candidate = new FakeCandidate(
                () => new GatewayGizmoCandidateSnapshot(
                    identity,
                    GatewayGizmoSource.Selection,
                    new[] { ownerHandle },
                    "Verse.Command_Toggle",
                    "Power",
                    "Toggle power.",
                    order: 10f,
                    disabled: false,
                    disabledReason: null,
                    hotKey: "Command_TogglePower",
                    groupKey: 42,
                    GatewayGizmoInteractionKind.Toggle,
                    current,
                    Array.Empty<GatewayInteractionInputKind>()),
                () =>
                {
                    candidate!.InvocationCount++;
                    current = !current;
                });
            return candidate;
        }

        public GatewayGizmoCandidateSnapshot Capture() => capture();

        public void Invoke() => invoke();

        public GatewayTargetAcceptance Preflight(GatewayInteractionTarget target) => preflight(target);

        public GatewayNativeApplyResult Apply(IReadOnlyList<GatewayInteractionTarget> targets) =>
            apply(targets);

        public void Cancel() => cancel();
    }
}
