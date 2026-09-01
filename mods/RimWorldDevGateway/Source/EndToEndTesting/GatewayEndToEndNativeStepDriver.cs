using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway;

public interface IGatewayEndToEndNativeActions
{
    GatewayEndToEndStepOutcome Apply(TimeControlActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(SelectionActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(
        SupportingHitPointFixtureActionStep step,
        IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(CameraActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(ScreenshotModeActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(ShadowRenderingActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(GizmoActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(FloatMenuActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(SettlementTradeActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(IncidentActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(TradeDialogActionStep step, IEndToEndContext context);

    IGatewayEndToEndStepOperation BeginMayMaximizeWindowInput(
        MayMaximizeWindowInputActionStep step,
        IEndToEndContext context);

    IGatewayEndToEndStepOperation Begin(SaveLoadActionStep step, IEndToEndContext context);

    IGatewayEndToEndStepOperation Begin(ScreenshotStep step, IEndToEndContext context);
}

internal interface IGatewayEndToEndDialogConfirmationNativeActions
{
    GatewayEndToEndStepOutcome Apply(DialogConfirmationActionStep step, IEndToEndContext context);
}

internal interface IGatewayEndToEndArchitectCategoryNativeActions
{
    GatewayEndToEndStepOutcome Apply(ArchitectCategoryActionStep step, IEndToEndContext context);
}

internal interface IGatewayEndToEndEscapeMenuNativeActions
{
    GatewayEndToEndStepOutcome Apply(EscapeMenuActionStep step, IEndToEndContext context);
}

internal interface IGatewayEndToEndCurrentFloatMenuNativeActions
{
    GatewayEndToEndStepOutcome Apply(CurrentFloatMenuActionStep step, IEndToEndContext context);
}

internal interface IGatewayEndToEndMapFloatMenuNativeActions
{
    GatewayEndToEndStepOutcome Apply(MapFloatMenuOpenActionStep step, IEndToEndContext context);
}

internal interface IGatewayEndToEndNoPawnMapRightClickNativeActions
{
    GatewayEndToEndStepOutcome Apply(NoPawnMapRightClickActionStep step, IEndToEndContext context);
}

internal interface IGatewayEndToEndDesignatorSessionNativeActions
{
    GatewayEndToEndStepOutcome Apply(DesignatorSessionActionStep step, IEndToEndContext context);
}

internal interface IGatewayEndToEndInspectionNativeActions
{
    GatewayEndToEndStepOutcome Apply(PawnInspectTabActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(ThingInfoCardActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(InspectPaneCloseActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(WindowCancelActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(WindowAcceptActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(ModSettingsActionStep step, IEndToEndContext context);
}

public sealed class GatewayEndToEndNativeStepDriver : IGatewayEndToEndStepDriver
{
    private readonly IGatewayEndToEndNativeActions actions;

    public GatewayEndToEndNativeStepDriver(IGatewayEndToEndNativeActions actions) =>
        this.actions = actions ?? throw new ArgumentNullException(nameof(actions));

    public IGatewayEndToEndStepOperation Begin(EndToEndStep step, IEndToEndContext context)
    {
        if (step is null)
        {
            throw new ArgumentNullException(nameof(step));
        }

        if (context is null)
        {
            throw new ArgumentNullException(nameof(context));
        }

        return step switch
        {
            TimeControlActionStep time => Complete(actions.Apply(time, context)),
            SelectionActionStep selection => Complete(actions.Apply(selection, context)),
            SupportingHitPointFixtureActionStep fixture => Complete(actions.Apply(fixture, context)),
            CameraActionStep camera => Complete(actions.Apply(camera, context)),
            ScreenshotModeActionStep screenshotMode => Complete(actions.Apply(screenshotMode, context)),
            ShadowRenderingActionStep shadowRendering => Complete(actions.Apply(shadowRendering, context)),
            GizmoActionStep gizmo => Complete(actions.Apply(gizmo, context)),
            DesignatorSessionActionStep designatorSession => actions is
                IGatewayEndToEndDesignatorSessionNativeActions designatorActions
                    ? Complete(designatorActions.Apply(designatorSession, context))
                    : GatewayEndToEndCompletedStepOperation.Failed(
                        "unsupported_e2e_step",
                        "The native E2E adapter does not support persistent designator previews."),
            FloatMenuActionStep floatMenu => Complete(actions.Apply(floatMenu, context)),
            MapFloatMenuOpenActionStep mapFloatMenu => actions is
                IGatewayEndToEndMapFloatMenuNativeActions mapFloatMenuActions
                    ? Complete(mapFloatMenuActions.Apply(mapFloatMenu, context))
                    : GatewayEndToEndCompletedStepOperation.Failed(
                        "unsupported_e2e_step",
                        "The native E2E adapter does not support opening an exact map float menu."),
            NoPawnMapRightClickActionStep noPawnRightClick => actions is
                IGatewayEndToEndNoPawnMapRightClickNativeActions noPawnRightClickActions
                    ? Complete(noPawnRightClickActions.Apply(noPawnRightClick, context))
                    : GatewayEndToEndCompletedStepOperation.Failed(
                        "unsupported_e2e_step",
                        "The native E2E adapter does not support a minimized no-pawn map right-click."),
            CurrentFloatMenuActionStep currentFloatMenu => actions is
                IGatewayEndToEndCurrentFloatMenuNativeActions currentFloatMenuActions
                    ? Complete(currentFloatMenuActions.Apply(currentFloatMenu, context))
                    : GatewayEndToEndCompletedStepOperation.Failed(
                        "unsupported_e2e_step",
                        "The native E2E adapter does not support an already-open float menu."),
            SettlementTradeActionStep settlementTrade => Complete(actions.Apply(settlementTrade, context)),
            IncidentActionStep incident => Complete(actions.Apply(incident, context)),
            TradeDialogActionStep tradeDialog => Complete(actions.Apply(tradeDialog, context)),
            DialogConfirmationActionStep dialogConfirmation => actions is
                IGatewayEndToEndDialogConfirmationNativeActions dialogActions
                    ? Complete(dialogActions.Apply(dialogConfirmation, context))
                    : GatewayEndToEndCompletedStepOperation.Failed(
                        "unsupported_e2e_step",
                        "The native E2E adapter does not support dialog confirmation."),
            ArchitectCategoryActionStep architectCategory => actions is
                IGatewayEndToEndArchitectCategoryNativeActions architectActions
                    ? Complete(architectActions.Apply(architectCategory, context))
                    : GatewayEndToEndCompletedStepOperation.Failed(
                        "unsupported_e2e_step",
                        "The native E2E adapter does not support Architect-category actions."),
            EscapeMenuActionStep escapeMenu => actions is
                IGatewayEndToEndEscapeMenuNativeActions escapeMenuActions
                    ? Complete(escapeMenuActions.Apply(escapeMenu, context))
                    : GatewayEndToEndCompletedStepOperation.Failed(
                        "unsupported_e2e_step",
                        "The native E2E adapter does not support Escape-menu actions."),
            PawnInspectTabActionStep inspectTab => actions is
                IGatewayEndToEndInspectionNativeActions inspectionActions
                    ? Complete(inspectionActions.Apply(inspectTab, context))
                    : GatewayEndToEndCompletedStepOperation.Failed(
                        "unsupported_e2e_step",
                        "The native E2E adapter does not support pawn inspect-tab actions."),
            ThingInfoCardActionStep infoCard => actions is
                IGatewayEndToEndInspectionNativeActions inspectionActions
                    ? Complete(inspectionActions.Apply(infoCard, context))
                    : GatewayEndToEndCompletedStepOperation.Failed(
                        "unsupported_e2e_step",
                        "The native E2E adapter does not support Thing info-card actions."),
            InspectPaneCloseActionStep closeInspect => actions is
                IGatewayEndToEndInspectionNativeActions inspectionActions
                    ? Complete(inspectionActions.Apply(closeInspect, context))
                    : GatewayEndToEndCompletedStepOperation.Failed(
                        "unsupported_e2e_step",
                        "The native E2E adapter does not support inspect-pane close actions."),
            WindowCancelActionStep cancelWindow => actions is
                IGatewayEndToEndInspectionNativeActions inspectionActions
                    ? Complete(inspectionActions.Apply(cancelWindow, context))
                    : GatewayEndToEndCompletedStepOperation.Failed(
                        "unsupported_e2e_step",
                        "The native E2E adapter does not support exact-window cancel actions."),
            WindowAcceptActionStep acceptWindow => actions is
                IGatewayEndToEndInspectionNativeActions inspectionActions
                    ? Complete(inspectionActions.Apply(acceptWindow, context))
                    : GatewayEndToEndCompletedStepOperation.Failed(
                        "unsupported_e2e_step",
                        "The native E2E adapter does not support exact-window accept actions."),
            ModSettingsActionStep modSettings => actions is
                IGatewayEndToEndInspectionNativeActions inspectionActions
                    ? Complete(inspectionActions.Apply(modSettings, context))
                    : GatewayEndToEndCompletedStepOperation.Failed(
                        "unsupported_e2e_step",
                        "The native E2E adapter does not support mod-settings actions."),
            SaveLoadActionStep saveLoad =>
                actions.Begin(saveLoad, context)
                ?? throw new InvalidOperationException("The save/load adapter returned no operation."),
            MayMaximizeWindowInputActionStep input =>
                actions.BeginMayMaximizeWindowInput(input, context)
                ?? throw new InvalidOperationException("The input adapter returned no operation."),
            ScreenshotStep screenshot =>
                actions.Begin(screenshot, context)
                ?? throw new InvalidOperationException("The screenshot adapter returned no operation."),
            _ => GatewayEndToEndCompletedStepOperation.Failed(
                "unsupported_e2e_step",
                $"The native E2E driver does not support '{step.GetType().FullName}'.")
        };
    }

    private static IGatewayEndToEndStepOperation Complete(GatewayEndToEndStepOutcome outcome) =>
        GatewayEndToEndCompletedStepOperation.FromOutcome(
            outcome ?? throw new InvalidOperationException("The native E2E adapter returned no outcome."));
}
