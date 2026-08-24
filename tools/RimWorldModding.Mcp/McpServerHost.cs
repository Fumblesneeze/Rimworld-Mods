using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ModelContextProtocol.Server;

namespace RimWorldModding.Mcp;

internal static class McpServerHost
{
    public static async Task RunAsync(string repositoryRoot, CancellationToken cancellationToken)
    {
        var builder = Host.CreateApplicationBuilder();
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
        builder.Services.AddSingleton(OperationRegistry.CreateDefault(repositoryRoot));
        builder.Services
            .AddMcpServer(options => options.ServerInfo = new() { Name = "rimworld-modding", Version = "0.1.0" })
            .WithStdioServerTransport()
            .WithToolsFromAssembly();
        await builder.Build().RunAsync(cancellationToken);
    }
}
