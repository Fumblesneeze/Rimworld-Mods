using System.Text.Json;
using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class GatewayDiagnosticOperationsTests
{
    [TestCase("gateway_automation_status", OperationRisk.Read)]
    [TestCase("gateway_automation_cancel", OperationRisk.DestructiveLocal)]
    public void AutomationLifecycleOperation_RequiresAnExactLiveRun(string operation, OperationRisk risk)
    {
        var registry = OperationRegistry.CreateDefault(TestRepository.FindRoot());
        var descriptor = registry.Descriptors.SingleOrDefault(item => item.Name == operation);
        Assert.That(descriptor, Is.Not.Null);
        Assert.That(descriptor!.Risk, Is.EqualTo(risk));
        var exception = Assert.ThrowsAsync<ArgumentException>(async () => await registry.InvokeAsync(operation,
            JsonSerializer.SerializeToElement(new { runId = "nonexistent-automation-run", automationRunId = "gesture-1" }),
            CancellationToken.None));
        Assert.That(exception!.Message, Does.Contain("run").IgnoreCase);
    }

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
