using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway;

public interface IGatewayEndToEndNativeActions
{
    GatewayEndToEndStepOutcome Apply(TimeControlActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(SelectionActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(CameraActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(GizmoActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(FloatMenuActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(SettlementTradeActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(IncidentActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(TradeDialogActionStep step, IEndToEndContext context);

    IGatewayEndToEndStepOperation Begin(ProcessInputActionStep step, IEndToEndContext context);

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

internal interface IGatewayEndToEndInspectionNativeActions
{
    GatewayEndToEndStepOutcome Apply(PawnInspectTabActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(ThingInfoCardActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(InspectPaneCloseActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(WindowCancelActionStep step, IEndToEndContext context);
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
            CameraActionStep camera => Complete(actions.Apply(camera, context)),
            GizmoActionStep gizmo => Complete(actions.Apply(gizmo, context)),
            FloatMenuActionStep floatMenu => Complete(actions.Apply(floatMenu, context)),
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
            SaveLoadActionStep saveLoad =>
                actions.Begin(saveLoad, context)
                ?? throw new InvalidOperationException("The save/load adapter returned no operation."),
            ProcessInputActionStep input =>
                actions.Begin(input, context)
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
