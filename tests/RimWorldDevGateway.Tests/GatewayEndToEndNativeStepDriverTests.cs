using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayEndToEndNativeStepDriverTests
{
    [Test]
    public void Public_process_input_action_methods_warn_that_they_may_maximize_the_window()
    {
        static string[] DeclaredPublicMethods(Type type) => type
            .GetMethods(System.Reflection.BindingFlags.Instance |
                        System.Reflection.BindingFlags.Public |
                        System.Reflection.BindingFlags.DeclaredOnly)
            .Select(method => method.Name)
            .ToArray();

        var nativeActionMethods = DeclaredPublicMethods(typeof(IGatewayEndToEndNativeActions));
        var actionBackendMethods = DeclaredPublicMethods(typeof(IGatewayEndToEndActionBackend));
        var actionFacadeMethods = DeclaredPublicMethods(typeof(GatewayEndToEndNativeActions));
        var gatewayBackendMethods = DeclaredPublicMethods(typeof(GatewayEndToEndGatewayBackend));

        Assert.Multiple(() =>
        {
            Assert.That(nativeActionMethods, Does.Contain("BeginMayMaximizeWindowInput"));
            Assert.That(actionBackendMethods, Does.Contain("BeginMayMaximizeWindowInput"));
            Assert.That(actionFacadeMethods, Does.Contain("BeginMayMaximizeWindowInput"));
            Assert.That(gatewayBackendMethods, Does.Contain("BeginMayMaximizeWindowInput"));
            Assert.That(nativeActionMethods, Does.Not.Contain("BeginInput"));
            Assert.That(actionBackendMethods, Does.Not.Contain("BeginInput"));
            Assert.That(gatewayBackendMethods, Does.Not.Contain("BeginInput"));
            Assert.That(
                typeof(GatewayEndToEndNativeActions).GetMethods()
                    .Where(method => method.Name == "Begin")
                    .SelectMany(method => method.GetParameters())
                    .Select(parameter => parameter.ParameterType),
                Does.Not.Contain(typeof(MayMaximizeWindowInputActionStep)));
        });
    }

    [Test]
    public void Typed_steps_dispatch_to_exact_native_adapter_methods()
    {
        var actions = new RecordingNativeActions();
        var driver = new GatewayEndToEndNativeStepDriver(actions);
        var context = new GatewayEndToEndTestContext(() => 1, () => 2, _ => null);
        var steps = new EndToEndStep[]
        {
            new TimeControlActionStep("time", false, EndToEndGameSpeed.Fast),
            new SelectionActionStep("selection", new[] { "thing_1" }, false),
            new SupportingHitPointFixtureActionStep(
                "supporting-hit-points",
                new[] { new EndToEndHitPointFixture("thing_1", 0.5f) }),
            new CameraActionStep("camera", new[] { "thing_1" }, 10),
            new ScreenshotModeActionStep("screenshot-mode", enabled: true),
            new ShadowRenderingActionStep("shadow-rendering", enabled: false),
            new GizmoActionStep(
                "gizmo",
                new[] { "thing_1" },
                "Command_Action",
                EndToEndGizmoInteraction.Invoke,
                "stable-gizmo"),
            new FloatMenuActionStep("float", "pawn_1", "thing_1", "stable-option"),
            new MapFloatMenuOpenActionStep("map-float", "thing_1", Array.Empty<string>()),
            new NoPawnMapRightClickActionStep("no-pawn-right-click", "thing_1"),
            new CurrentFloatMenuActionStep("current-float", "For guests"),
            new SettlementTradeActionStep("settlement-trade", 41, 42),
            new IncidentActionStep("incident", "TraderCaravanArrival", 17),
            TradeDialogActionStep.AdjustTransfer("trade", "meal_1", -1),
            new DialogConfirmationActionStep(
                "dialog",
                "Example.Dialog"),
            new ArchitectCategoryActionStep("architect", "Production", open: true),
            new EscapeMenuActionStep("escape-menu", open: true),
            new PawnInspectTabActionStep("inspect-tab", "pawn_1", EndToEndPawnInspectTab.Gear),
            ThingInfoCardActionStep.Open("info-card", "thing_1"),
            new InspectPaneCloseActionStep("close-inspect", "thing_1", "Example.ContentsTab"),
            new WindowCancelActionStep("cancel-window", "Example.Dialog"),
            new WindowAcceptActionStep("accept-window", "Example.Dialog"),
            new ModSettingsActionStep("mod-settings", "fumblesneeze.immersivechefs"),
            new SaveLoadActionStep("save-load", "GatewayE2E"),
            MayMaximizeWindowInputActionStep.Click(
                "click",
                new EndToEndScreenPoint(10, 20),
                EndToEndMouseButton.Left),
            new ScreenshotStep("shot", new[] { "thing_1" }, 12)
        };

        var operations = steps.Select(step => driver.Begin(step, context)).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(actions.Calls, Is.EqualTo(new[]
            {
                "time", "selection", "supporting-hit-points", "camera", "screenshot-mode", "shadow-rendering", "gizmo", "float", "map-float", "no-pawn-right-click", "current-float", "settlement-trade", "incident", "trade", "dialog", "architect", "escape-menu", "inspect-tab", "info-card", "close-inspect", "cancel-window", "accept-window", "mod-settings", "save-load", "input", "screenshot"
            }));
            Assert.That(operations.Take(23).All(operation => operation.IsCompleted), Is.True);
            Assert.That(operations[23], Is.SameAs(actions.SaveLoadOperation));
            Assert.That(operations[24], Is.SameAs(actions.InputOperation));
            Assert.That(operations[25], Is.SameAs(actions.ScreenshotOperation));
        });
    }

    [Test]
    public void Non_native_step_fails_closed_without_calling_an_adapter()
    {
        var actions = new RecordingNativeActions();
        var operation = new GatewayEndToEndNativeStepDriver(actions).Begin(
            new AssertionStep("not driven", _ => { }),
            new GatewayEndToEndTestContext(() => 0, () => 0, _ => null));

        var outcome = operation.GetOutcome();
        Assert.Multiple(() =>
        {
            Assert.That(outcome.Passed, Is.False);
            Assert.That(outcome.FailureCode, Is.EqualTo("unsupported_e2e_step"));
            Assert.That(actions.Calls, Is.Empty);
        });
    }

    private sealed class RecordingNativeActions :
        IGatewayEndToEndNativeActions,
        IGatewayEndToEndDialogConfirmationNativeActions,
        IGatewayEndToEndArchitectCategoryNativeActions,
        IGatewayEndToEndEscapeMenuNativeActions,
        IGatewayEndToEndMapFloatMenuNativeActions,
        IGatewayEndToEndNoPawnMapRightClickNativeActions,
        IGatewayEndToEndCurrentFloatMenuNativeActions,
        IGatewayEndToEndInspectionNativeActions
    {
        public List<string> Calls { get; } = new();

        public IGatewayEndToEndStepOperation ScreenshotOperation { get; } =
            GatewayEndToEndCompletedStepOperation.Passed(
                new Dictionary<string, string> { ["screenshot"] = "evidence.png" });

        public IGatewayEndToEndStepOperation InputOperation { get; } =
            GatewayEndToEndCompletedStepOperation.Passed();

        public IGatewayEndToEndStepOperation SaveLoadOperation { get; } =
            GatewayEndToEndCompletedStepOperation.Passed();

        public GatewayEndToEndStepOutcome Apply(TimeControlActionStep step, IEndToEndContext context) =>
            Record("time");

        public GatewayEndToEndStepOutcome Apply(SelectionActionStep step, IEndToEndContext context) =>
            Record("selection");

        public GatewayEndToEndStepOutcome Apply(
            SupportingHitPointFixtureActionStep step,
            IEndToEndContext context) => Record("supporting-hit-points");

        public GatewayEndToEndStepOutcome Apply(CameraActionStep step, IEndToEndContext context) =>
            Record("camera");

        public GatewayEndToEndStepOutcome Apply(
            ScreenshotModeActionStep step,
            IEndToEndContext context) => Record("screenshot-mode");

        public GatewayEndToEndStepOutcome Apply(
            ShadowRenderingActionStep step,
            IEndToEndContext context) => Record("shadow-rendering");

        public GatewayEndToEndStepOutcome Apply(GizmoActionStep step, IEndToEndContext context) =>
            Record("gizmo");

        public GatewayEndToEndStepOutcome Apply(FloatMenuActionStep step, IEndToEndContext context) =>
            Record("float");

        GatewayEndToEndStepOutcome IGatewayEndToEndMapFloatMenuNativeActions.Apply(
            MapFloatMenuOpenActionStep step,
            IEndToEndContext context) => Record("map-float");

        GatewayEndToEndStepOutcome IGatewayEndToEndNoPawnMapRightClickNativeActions.Apply(
            NoPawnMapRightClickActionStep step,
            IEndToEndContext context) => Record("no-pawn-right-click");

        GatewayEndToEndStepOutcome IGatewayEndToEndCurrentFloatMenuNativeActions.Apply(
            CurrentFloatMenuActionStep step,
            IEndToEndContext context) => Record("current-float");

        public GatewayEndToEndStepOutcome Apply(SettlementTradeActionStep step, IEndToEndContext context) =>
            Record("settlement-trade");

        public GatewayEndToEndStepOutcome Apply(IncidentActionStep step, IEndToEndContext context) =>
            Record("incident");

        public GatewayEndToEndStepOutcome Apply(TradeDialogActionStep step, IEndToEndContext context) =>
            Record("trade");

        public GatewayEndToEndStepOutcome Apply(
            DialogConfirmationActionStep step,
            IEndToEndContext context) => Record("dialog");

        public GatewayEndToEndStepOutcome Apply(
            ArchitectCategoryActionStep step,
            IEndToEndContext context) => Record("architect");

        GatewayEndToEndStepOutcome IGatewayEndToEndEscapeMenuNativeActions.Apply(
            EscapeMenuActionStep step,
            IEndToEndContext context) => Record("escape-menu");

        public GatewayEndToEndStepOutcome Apply(
            PawnInspectTabActionStep step,
            IEndToEndContext context) => Record("inspect-tab");

        public GatewayEndToEndStepOutcome Apply(
            ThingInfoCardActionStep step,
            IEndToEndContext context) => Record("info-card");

        public GatewayEndToEndStepOutcome Apply(
            InspectPaneCloseActionStep step,
            IEndToEndContext context) => Record("close-inspect");

        public GatewayEndToEndStepOutcome Apply(
            WindowCancelActionStep step,
            IEndToEndContext context) => Record("cancel-window");

        public GatewayEndToEndStepOutcome Apply(
            WindowAcceptActionStep step,
            IEndToEndContext context) => Record("accept-window");

        public GatewayEndToEndStepOutcome Apply(
            ModSettingsActionStep step,
            IEndToEndContext context) => Record("mod-settings");

        public IGatewayEndToEndStepOperation BeginMayMaximizeWindowInput(
            MayMaximizeWindowInputActionStep step,
            IEndToEndContext context)
        {
            Calls.Add("input");
            return InputOperation;
        }

        public IGatewayEndToEndStepOperation Begin(SaveLoadActionStep step, IEndToEndContext context)
        {
            Calls.Add("save-load");
            return SaveLoadOperation;
        }

        public IGatewayEndToEndStepOperation Begin(ScreenshotStep step, IEndToEndContext context)
        {
            Calls.Add("screenshot");
            return ScreenshotOperation;
        }

        private GatewayEndToEndStepOutcome Record(string call)
        {
            Calls.Add(call);
            return GatewayEndToEndStepOutcome.Pass();
        }
    }
}
