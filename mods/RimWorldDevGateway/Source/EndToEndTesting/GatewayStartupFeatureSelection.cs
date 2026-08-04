namespace RimWorldDevGateway;

public sealed class GatewayStartupFeatureSelection
{
    public const string IntegrationFlag = "devGatewayRunIntegrationTests";
    public const string EndToEndFlag = "devGatewayRunEndToEndTests";

    private GatewayStartupFeatureSelection(bool runIntegrationTests, bool runEndToEndTests)
    {
        RunIntegrationTests = runIntegrationTests;
        RunEndToEndTests = runEndToEndTests;
    }

    public bool RunIntegrationTests { get; }

    public bool RunEndToEndTests { get; }

    public static GatewayStartupFeatureSelection Capture(Func<string, bool> argumentPassed)
    {
        if (argumentPassed is null)
        {
            throw new ArgumentNullException(nameof(argumentPassed));
        }

        return new GatewayStartupFeatureSelection(
            argumentPassed(IntegrationFlag),
            argumentPassed(EndToEndFlag));
    }
}
