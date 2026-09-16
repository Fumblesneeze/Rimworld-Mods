using RimWorldDevGateway.EndToEndTesting;
using System.Globalization;

namespace RimWorldDevGateway;

internal interface IGatewayEndToEndSceneTimeNativeActions
{
    GatewayEndToEndStepOutcome Apply(SupportingSceneTimeActionStep step, IEndToEndContext context);
}

public sealed class GatewaySceneTimeFixture
{
    private readonly GatewaySceneClockController clock;
    public GatewaySceneTimeFixture(IGatewaySceneClockWorld world) => clock = new GatewaySceneClockController(world);
    public GatewayEndToEndStepOutcome Apply(SupportingSceneTimeActionStep step, IEndToEndContext context)
    {
        if (step is null) throw new ArgumentNullException(nameof(step));
        if (context is null) throw new ArgumentNullException(nameof(context));
        var before = clock.Capture();
        if (before.MapHandle != step.MapHandle)
            return GatewayEndToEndStepOutcome.Fail("stale_map_handle", "The requested map is no longer current.");
        context.DeferCleanup(() => clock.Apply(before.MapHandle, null, before.GameStartAbsTick));
        var result = clock.Apply(step.MapHandle, step.MinuteOfDay, null);
        return GatewayEndToEndStepOutcome.Pass(new Dictionary<string, string>
        {
            ["supportingFixture"] = "calendar-offset-only",
            ["mapHandle"] = result.After.MapHandle,
            ["beforeGameStartAbsTick"] = result.Before.GameStartAbsTick.ToString(CultureInfo.InvariantCulture),
            ["afterGameStartAbsTick"] = result.After.GameStartAbsTick.ToString(CultureInfo.InvariantCulture),
            ["ticksGame"] = result.After.TicksGame.ToString(CultureInfo.InvariantCulture),
            ["beforeLocalDayTick"] = result.Before.LocalDayTick.ToString(CultureInfo.InvariantCulture),
            ["afterLocalDayTick"] = result.After.LocalDayTick.ToString(CultureInfo.InvariantCulture)
        });
    }
}
