using System.Threading;
using RimWorldDevGateway.Contracts;
using Verse;

namespace GatewayAssemblyExecutionSmoke;

public static class Entry
{
    public static string Execute(
        string requestJson,
        GatewayAssemblyExecutionContext context)
    {
        context.CancellationToken.ThrowIfCancellationRequested();
        context.RuntimeExtensions.RegisterSessionAutomation(
            new GatewayAssemblyAutomationDescriptor(
                "gateway.uploaded-session-probe",
                "1",
                "Reports the live program state and Unity-thread identity from uploaded code.",
                mutating: false,
                argumentSchema: new System.Collections.Generic.Dictionary<string, string>
                {
                    ["label"] = "string"
                },
                prerequisites: new[] { "RimWorld is running" }),
            RunProbe);
        return "registered;requestId=" + context.RequestId +
            ";programState=" + Current.ProgramState +
            ";thread=" + Thread.CurrentThread.ManagedThreadId +
            ";request=" + requestJson;
    }

    private static string RunProbe(
        string argumentsJson,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return "programState=" + Current.ProgramState +
            ";thread=" + Thread.CurrentThread.ManagedThreadId +
            ";arguments=" + argumentsJson;
    }
}
