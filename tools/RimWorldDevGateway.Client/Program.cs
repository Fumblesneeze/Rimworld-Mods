namespace RimWorldDevGateway.Client;

public static class Program
{
    public static int Main(string[] args)
    {
        if (GatewayCliApp.UsesUtf8Console(args))
            System.Console.OutputEncoding = new System.Text.UTF8Encoding(false);
        var app = new GatewayCliApp(
            new FileGatewaySessionProvider(new GatewayProcessInspector()),
            new GatewayHttpTransport(),
            new DotNetGatewaySourceCompiler(),
            new GatewayClientFileSystem());
        return app.Run(args, System.Console.Out, System.Console.Error);
    }
}
