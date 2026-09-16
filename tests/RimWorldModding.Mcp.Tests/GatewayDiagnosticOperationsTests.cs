using System.Text.Json;
using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class GatewayDiagnosticOperationsTests
{
    [TestCase("gateway_logs")]
    [TestCase("gateway_errors")]
    [TestCase("gateway_methods")]
    [TestCase("gateway_decompile")]
    [TestCase("gateway_error_report")]
    public void DiagnosticOperation_IsDiscoverableAndRejectsMissingRun(string operation)
    {
        var registry = OperationRegistry.CreateDefault(TestRepository.FindRoot());
        Assert.That(registry.Descriptors.Any(item => item.Name == operation), Is.True);
        var exception = Assert.ThrowsAsync<ArgumentException>(async () => await registry.InvokeAsync(operation,
            JsonSerializer.SerializeToElement(new { runId = "nonexistent-diagnostics-run", methodHandle = "missing", errorId = "missing", typeName = "Verse.Log" }),
            CancellationToken.None));
        Assert.That(exception!.Message, Does.Contain("run").IgnoreCase);
    }
}
