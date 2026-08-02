namespace RimWorldDevGateway.Client;

public static class Program
{
    public static int Main(string[] args)
    {
        var app = new GatewayCliApp(
            new FileGatewaySessionProvider(new GatewayProcessInspector()),
            new GatewayHttpTransport(),
            new DotNetGatewaySourceCompiler(),
            new GatewayClientFileSystem());
        return app.Run(args, System.Console.Out, System.Console.Error);
    }
}
