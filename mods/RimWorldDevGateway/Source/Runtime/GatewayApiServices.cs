namespace RimWorldDevGateway;

public sealed class GatewayCapabilityException : Exception
{
    public GatewayCapabilityException(string code, string message) : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}

public sealed class GatewayApiServices
{
    public GatewayApiServices(
        GatewayAssemblyExecutor? assemblyExecutor = null,
        GatewayCSharpEvaluator? csharpEvaluator = null,
        GatewayScreenshotService? screenshotService = null,
        GatewayWindowsInput? windowsInput = null,
        GatewaySemanticActionRegistry? semanticActions = null,
        GatewayGameControlController? gameControl = null,
        GatewayCameraController? camera = null,
        GatewayThingController? things = null,
        GatewayDebugActionRegistry? debugActions = null,
        GatewayGizmoRegistry? gizmos = null,
        GatewayAutomationRegistry? automations = null,
        GatewayDefExporter? defExporter = null,
        Func<GatewayIntegrationTestSnapshot?>? integrationTestSnapshot = null,
        Func<GatewayEndToEndSnapshot?>? endToEndTestSnapshot = null,
        Action? requestShutdown = null)
    {
        AssemblyExecutor = assemblyExecutor;
        CSharpEvaluator = csharpEvaluator;
        ScreenshotService = screenshotService;
        WindowsInput = windowsInput;
        SemanticActions = semanticActions;
        GameControl = gameControl;
        Camera = camera;
        Things = things;
        DebugActions = debugActions;
        Gizmos = gizmos;
        Automations = automations;
        DefExporter = defExporter;
        IntegrationTestSnapshot = integrationTestSnapshot;
        EndToEndTestSnapshot = endToEndTestSnapshot;
        RequestShutdown = requestShutdown;
    }

    public GatewayAssemblyExecutor? AssemblyExecutor { get; }

    public GatewayCSharpEvaluator? CSharpEvaluator { get; }

    public GatewayScreenshotService? ScreenshotService { get; }

    public GatewayWindowsInput? WindowsInput { get; }

    public GatewaySemanticActionRegistry? SemanticActions { get; }

    public GatewayGameControlController? GameControl { get; }

    public GatewayCameraController? Camera { get; }

    public GatewayThingController? Things { get; }

    public GatewayDebugActionRegistry? DebugActions { get; }

    public GatewayGizmoRegistry? Gizmos { get; }

    public GatewayAutomationRegistry? Automations { get; }

    public GatewayDefExporter? DefExporter { get; }

    public Func<GatewayIntegrationTestSnapshot?>? IntegrationTestSnapshot { get; }

    public Func<GatewayEndToEndSnapshot?>? EndToEndTestSnapshot { get; }

    public Action? RequestShutdown { get; }
}
