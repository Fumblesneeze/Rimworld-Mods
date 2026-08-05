using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway;

public interface IGatewayEndToEndNativeActions
{
    GatewayEndToEndStepOutcome Apply(TimeControlActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(SelectionActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(CameraActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(GizmoActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(FloatMenuActionStep step, IEndToEndContext context);

    GatewayEndToEndStepOutcome Apply(ProcessInputActionStep step, IEndToEndContext context);

    IGatewayEndToEndStepOperation Begin(ScreenshotStep step, IEndToEndContext context);
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
            ProcessInputActionStep input => Complete(actions.Apply(input, context)),
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
