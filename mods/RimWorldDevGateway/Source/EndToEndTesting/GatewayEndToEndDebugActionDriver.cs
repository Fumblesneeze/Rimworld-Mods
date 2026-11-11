using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway;

public sealed class GatewayEndToEndDebugActionDriver : IGatewayEndToEndStepDriver
{
    private readonly IGatewayEndToEndStepDriver other;
    private readonly GatewayDebugActionRegistry registry;
    public GatewayEndToEndDebugActionDriver(IGatewayEndToEndStepDriver other, GatewayDebugActionRegistry registry)
    {
        this.other = other ?? throw new ArgumentNullException(nameof(other));
        this.registry = registry ?? throw new ArgumentNullException(nameof(registry));
    }

    public IGatewayEndToEndStepOperation Begin(EndToEndStep step, IEndToEndContext context)
    {
        if (step is not NativeDebugActionStep action) return other.Begin(step, context);
        var artifacts = new Dictionary<string, string> { ["debugActionHandle"] = action.Handle };
        try
        {
            var result = registry.InvokeImmediate(action.Handle);
            artifacts["debugActionPath"] = result.Path;
            artifacts["debugActionMode"] = result.Mode.ToString();
            artifacts["debugActionDelegateReturned"] = result.Completed.ToString();
            artifacts["debugActionPointerRequired"] = result.PointerRequired.ToString();
            return GatewayEndToEndCompletedStepOperation.Passed(artifacts);
        }
        catch (GatewayDebugActionException exception)
        {
            return GatewayEndToEndCompletedStepOperation.FromOutcome(GatewayEndToEndStepOutcome.Fail(
                exception.Code, "Native debug-action invocation failed; it was not retried.", artifacts));
        }
        catch (Exception exception)
        {
            return GatewayEndToEndCompletedStepOperation.FromOutcome(GatewayEndToEndStepOutcome.Fail(
                "native_debug_action_failed", "Native debug-action invocation failed with " +
                exception.GetBaseException().GetType().Name + "; it was not retried.", artifacts));
        }
    }
}
