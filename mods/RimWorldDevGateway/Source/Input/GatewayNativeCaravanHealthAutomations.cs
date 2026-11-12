namespace RimWorldDevGateway;

internal static class GatewayNativeCaravanHealthAutomations
{
    internal static void Register(GatewayAutomationRegistry registry, GatewayCaravanInspectionAutomation caravans,
        Func<IGatewayCaravanInspection, string, Func<IGatewayCaravanInspection>, object> capture,
        Func<string, GatewayAutomationContext, IGatewayDeferredAutomationOperation> click,
        Func<GatewayAutomationAvailability> availability)
    {
        registry.RegisterBuiltIn(new GatewayAutomationDescriptor("native-caravan-health.capture", "1.0",
            "Capture the one exact pawn row Health button observed in a completed native layout.",
            GatewayCaravanArguments.Schema("caravanHandle", "pawnLoadId"), new[] { "current-caravan", "observed-health-row" }, false),
            (_, args) =>
            {
                GatewayCaravanArguments.Validate(args, "caravanHandle", "pawnLoadId");
                string handle = (string)args["caravanHandle"]!;
                return capture(caravans.Require(handle), (string)args["pawnLoadId"]!, () => caravans.Require(handle));
            }, availability);
        registry.RegisterBuiltIn(new GatewayAutomationDescriptor("native-caravan-health.click", "1.0",
            "Click an exact captured native Health row button through the shared owned input lane.",
            GatewayCaravanArguments.Schema("controlHandle"), new[] { "current-health-control", "neutral-input" }, true),
            (context, args) =>
            {
                GatewayCaravanArguments.Validate(args, "controlHandle");
                context.AddArtifact("native-caravan-health-click-request", "native-input-request", args);
                return click((string)args["controlHandle"]!, context);
            }, availability);
    }
}
