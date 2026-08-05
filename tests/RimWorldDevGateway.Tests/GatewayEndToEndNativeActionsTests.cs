using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayEndToEndNativeActionsTests
{
    [Test]
    public void Time_and_selection_actions_preserve_declared_semantics()
    {
        var backend = new RecordingBackend();
        var actions = new GatewayEndToEndNativeActions(backend);
        var context = Context();

        var time = actions.Apply(
            new TimeControlActionStep("pause", paused: true, EndToEndGameSpeed.Fast),
            context);
        var selection = actions.Apply(
            new SelectionActionStep("select", new[] { "pawn_2", "pawn_1" }, additive: true),
            context);

        Assert.Multiple(() =>
        {
            Assert.That(time.Passed, Is.True);
            Assert.That(selection.Passed, Is.True);
            Assert.That(backend.Time, Is.EqualTo((true, EndToEndGameSpeed.Fast)));
            Assert.That(backend.Selection, Is.EqualTo((true, new[] { "pawn_2", "pawn_1" })));
        });
    }

    [Test]
    public void Camera_action_frames_the_union_with_pixel_padding_and_clamps_zoom()
    {
        var backend = new RecordingBackend
        {
            Viewport = new GatewayEndToEndCameraViewport("map_1", 200, 200, 1000, 500, 8f, 60f)
        };
        backend.Targets["a"] = new GatewayEndToEndTargetBounds(
            "map_1", new GatewayMapRect(10, 20, 14, 24));
        backend.Targets["b"] = new GatewayEndToEndTargetBounds(
            "map_1", new GatewayMapRect(30, 40, 34, 44));

        var outcome = new GatewayEndToEndNativeActions(backend).Apply(
            new CameraActionStep("frame", new[] { "a", "b" }, paddingPixels: 20),
            Context());

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.True);
            Assert.That(backend.Camera!.Value.MapHandle, Is.EqualTo("map_1"));
            Assert.That(backend.Camera.Value.Center.X, Is.EqualTo(22));
            Assert.That(backend.Camera.Value.Center.Z, Is.EqualTo(32));
            Assert.That(backend.Camera.Value.RootSize, Is.EqualTo(13.587f).Within(0.002f));
        });
    }

    [Test]
    public void Camera_action_fails_closed_when_targets_cross_maps()
    {
        var backend = new RecordingBackend();
        backend.Targets["a"] = new GatewayEndToEndTargetBounds(
            "map_1", new GatewayMapRect(1, 1, 1, 1));
        backend.Targets["b"] = new GatewayEndToEndTargetBounds(
            "map_2", new GatewayMapRect(2, 2, 2, 2));

        var outcome = new GatewayEndToEndNativeActions(backend).Apply(
            new CameraActionStep("frame", new[] { "a", "b" }, 0),
            Context());

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.False);
            Assert.That(outcome.FailureCode, Is.EqualTo("camera_targets_cross_maps"));
            Assert.That(backend.Camera, Is.Null);
        });
    }

    [Test]
    public void Camera_action_fails_when_maximum_zoom_cannot_contain_the_targets()
    {
        var backend = new RecordingBackend
        {
            Viewport = new GatewayEndToEndCameraViewport("map_1", 200, 200, 1000, 500, 8f, 10f)
        };
        backend.Targets["large"] = new GatewayEndToEndTargetBounds(
            "map_1", new GatewayMapRect(1, 1, 100, 100));

        var outcome = new GatewayEndToEndNativeActions(backend).Apply(
            new CameraActionStep("frame", new[] { "large" }, 0),
            Context());

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.False);
            Assert.That(outcome.FailureCode, Is.EqualTo("camera_targets_too_large"));
            Assert.That(backend.Camera, Is.Null);
        });
    }

    [Test]
    public void Gizmo_action_uses_type_and_stable_identity_instead_of_label_or_position()
    {
        var backend = new RecordingBackend();
        backend.Gizmos.Add(Gizmo("h1", "stable-a", "Command_Action"));
        backend.Gizmos.Add(Gizmo("h2", "stable-b", "Command_Action"));

        var outcome = new GatewayEndToEndNativeActions(backend).Apply(
            new GizmoActionStep(
                "invoke",
                new[] { "pawn_1" },
                "Command_Action",
                EndToEndGizmoInteraction.Invoke,
                "stable-b"),
            Context());

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.True);
            Assert.That(backend.InvokedHandle, Is.EqualTo("h2"));
        });
    }

    [Test]
    public void Ambiguous_gizmo_match_fails_without_invocation()
    {
        var backend = new RecordingBackend();
        backend.Gizmos.Add(Gizmo("h1", "one", "Command_Action"));
        backend.Gizmos.Add(Gizmo("h2", "two", "Command_Action"));

        var outcome = new GatewayEndToEndNativeActions(backend).Apply(
            new GizmoActionStep(
                "invoke",
                new[] { "pawn_1" },
                "Command_Action",
                EndToEndGizmoInteraction.Invoke),
            Context());

        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.False);
            Assert.That(outcome.FailureCode, Is.EqualTo("ambiguous_gizmo"));
            Assert.That(backend.InvokedHandle, Is.Null);
        });
    }

    [Test]
    public void Placement_and_drag_use_semantic_cells_and_complete_the_interaction()
    {
        var backend = new RecordingBackend();
        backend.Gizmos.Add(Gizmo(
            "place",
            "place",
            "Designator_Build",
            GatewayGizmoInteractionKind.Placement,
            GatewayInteractionInputKind.Cell));
        backend.Gizmos.Add(Gizmo(
            "drag",
            "drag",
            "Designator_Zone",
            GatewayGizmoInteractionKind.Drag,
            GatewayInteractionInputKind.Rectangle));
        var actions = new GatewayEndToEndNativeActions(backend);

        var placed = actions.Apply(
            new GizmoActionStep(
                "place",
                new[] { "architect" },
                "Designator_Build",
                EndToEndGizmoInteraction.Place,
                "place",
                new EndToEndMapCell(5, 6)),
            Context());
        var dragged = actions.Apply(
            new GizmoActionStep(
                "drag",
                new[] { "architect" },
                "Designator_Zone",
                EndToEndGizmoInteraction.Drag,
                "drag",
                new EndToEndMapCell(1, 2),
                new EndToEndMapCell(3, 4)),
            Context());

        Assert.Multiple(() =>
        {
            Assert.That(placed.Passed, Is.True);
            Assert.That(dragged.Passed, Is.True);
            Assert.That(backend.AppliedInputs[0].Kind, Is.EqualTo(GatewayInteractionInputKind.Cell));
            Assert.That(backend.AppliedInputs[0].Cells.Single().X, Is.EqualTo(5));
            Assert.That(backend.AppliedInputs[1].Kind, Is.EqualTo(GatewayInteractionInputKind.Rectangle));
            Assert.That(backend.AppliedInputs[1].Cells.Select(cell => (cell.X, cell.Z)),
                Is.EqualTo(new[] { (1, 2), (3, 4) }));
        });
    }

    [Test]
    public void Input_screenshot_and_float_menu_delegate_to_the_durable_backend_operations()
    {
        var backend = new RecordingBackend();
        var actions = new GatewayEndToEndNativeActions(backend);
        var input = ProcessInputActionStep.Key("key", "Space");
        var screenshot = new ScreenshotStep("shot", new[] { "pawn_1" }, 8);
        var menu = new FloatMenuActionStep("eat", "pawn_1", "meal_1", "consume");

        var inputOperation = actions.Begin(input, Context());
        var screenshotOperation = actions.Begin(screenshot, Context());
        var menuOutcome = actions.Apply(menu, Context());

        Assert.Multiple(() =>
        {
            Assert.That(inputOperation, Is.SameAs(backend.InputOperation));
            Assert.That(screenshotOperation, Is.SameAs(backend.ScreenshotOperation));
            Assert.That(menuOutcome.Passed, Is.True);
            Assert.That(backend.InputStep, Is.SameAs(input));
            Assert.That(backend.ScreenshotStep, Is.SameAs(screenshot));
            Assert.That(backend.FloatMenuStep, Is.SameAs(menu));
        });
    }

    private static GatewayEndToEndTestContext Context() => new(() => 0, () => 0, _ => null);

    private static GatewayGizmoDescriptor Gizmo(
        string handle,
        string identity,
        string runtimeType,
        GatewayGizmoInteractionKind kind = GatewayGizmoInteractionKind.Immediate,
        params GatewayInteractionInputKind[] acceptedInputs) =>
        new(
            handle,
            "revision",
            new GatewayGizmoCandidateSnapshot(
                identity,
                GatewayGizmoSource.ExplicitOwner,
                new[] { "owner" },
                runtimeType,
                identity,
                string.Empty,
                0,
                false,
                null,
                null,
                0,
                kind,
                null,
                acceptedInputs));

    private sealed class RecordingBackend : IGatewayEndToEndActionBackend
    {
        public GatewayEndToEndCameraViewport Viewport { get; set; } =
            new("map_1", 200, 200, 1000, 500, 8f, 60f);

        public Dictionary<string, GatewayEndToEndTargetBounds> Targets { get; } = new();

        public List<GatewayGizmoDescriptor> Gizmos { get; } = new();

        public List<GatewayInteractionInput> AppliedInputs { get; } = new();

        public (bool Paused, EndToEndGameSpeed Speed)? Time { get; private set; }

        public (bool Additive, string[] Handles)? Selection { get; private set; }

        public (string MapHandle, GatewayMapCell Center, float RootSize)? Camera { get; private set; }

        public string? InvokedHandle { get; private set; }

        public ProcessInputActionStep? InputStep { get; private set; }

        public ScreenshotStep? ScreenshotStep { get; private set; }

        public FloatMenuActionStep? FloatMenuStep { get; private set; }

        public IGatewayEndToEndStepOperation InputOperation { get; } =
            GatewayEndToEndCompletedStepOperation.Passed();

        public IGatewayEndToEndStepOperation ScreenshotOperation { get; } =
            GatewayEndToEndCompletedStepOperation.Passed();

        public void SetTime(bool paused, EndToEndGameSpeed speed) => Time = (paused, speed);

        public void SetSelection(IReadOnlyList<string> handles, bool additive) =>
            Selection = (additive, handles.ToArray());

        public GatewayEndToEndCameraViewport CaptureCameraViewport() => Viewport;

        public GatewayEndToEndTargetBounds ResolveTarget(string runtimeId) => Targets[runtimeId];

        public void SetCamera(string mapHandle, GatewayMapCell center, float rootSize) =>
            Camera = (mapHandle, center, rootSize);

        public IReadOnlyList<GatewayGizmoDescriptor> QueryGizmos(
            IReadOnlyList<string> targetRuntimeIds,
            IReadOnlyList<string> architectCategoryDefNames) =>
            Gizmos;

        public GatewayGizmoInvocationResult InvokeGizmo(string handle)
        {
            InvokedHandle = handle;
            var gizmo = Gizmos.Single(item => item.Handle == handle);
            var interaction = gizmo.InteractionKind is GatewayGizmoInteractionKind.Placement or GatewayGizmoInteractionKind.Drag
                ? new GatewayInteractionDescriptor(
                    "interaction-" + handle,
                    handle,
                    gizmo.InteractionKind,
                    gizmo.AcceptedInputs,
                    "map_1",
                    gizmo.OwnerHandles,
                    gizmo.Revision)
                : null;
            return new GatewayGizmoInvocationResult(
                handle,
                gizmo.InteractionKind,
                interaction is null,
                null,
                null,
                interaction);
        }

        public GatewayInteractionApplyResult ApplyGizmo(
            string interactionHandle,
            GatewayInteractionInput input)
        {
            AppliedInputs.Add(input);
            return new GatewayInteractionApplyResult(
                interactionHandle,
                Array.Empty<GatewayInteractionTarget>(),
                Array.Empty<GatewayRejectedInteractionTarget>(),
                completed: true);
        }

        public void CancelGizmo(string interactionHandle)
        {
        }

        public GatewayEndToEndStepOutcome ApplyFloatMenu(
            FloatMenuActionStep step,
            IEndToEndContext context)
        {
            FloatMenuStep = step;
            return GatewayEndToEndStepOutcome.Pass();
        }

        public IGatewayEndToEndStepOperation BeginInput(
            ProcessInputActionStep step,
            IEndToEndContext context)
        {
            InputStep = step;
            return InputOperation;
        }

        public IGatewayEndToEndStepOperation BeginScreenshot(
            ScreenshotStep step,
            IEndToEndContext context)
        {
            ScreenshotStep = step;
            return ScreenshotOperation;
        }
    }
}
