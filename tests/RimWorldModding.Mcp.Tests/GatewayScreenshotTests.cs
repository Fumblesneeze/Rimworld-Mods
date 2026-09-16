using System.Text.Json;
using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class GatewayScreenshotTests
{
    [Test]
    public void CaptureResult_HasDeterministicJsonWithoutCredentials()
    {
        var result = new GatewayScreenshotResult("run", 123, DateTimeOffset.UnixEpoch,
            "evidence.png", 100, "ABC");
        using var json = JsonDocument.Parse(OperationJson.Serialize(result));
        Assert.Multiple(() =>
        {
            Assert.That(json.RootElement.GetProperty("gameProcessId").GetInt32(), Is.EqualTo(123));
            Assert.That(json.RootElement.GetProperty("sha256").GetString(), Is.EqualTo("ABC"));
            Assert.That(json.RootElement.EnumerateObject().Count(), Is.EqualTo(6));
        });
    }

    [Test]
    public void Capture_IsDiscoverableAsBoundedEvidenceWrite()
    {
        var descriptor = OperationRegistry.CreateDefault(TestRepository.FindRoot()).Descriptors
            .SingleOrDefault(item => item.Name == "gateway_screenshot");
        Assert.That(descriptor, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(descriptor!.Risk, Is.EqualTo(OperationRisk.WorkspaceWrite));
            Assert.That(descriptor.LongRunning, Is.False);
            Assert.That(descriptor.TimeoutSeconds, Is.InRange(1, 180));
        });
    }

    [TestCase("nonexistent-screenshot-run")]
    [TestCase("20000101T000000000Z-00000000000000000000000000000000")]
    public void Capture_RejectsMissingRunInsteadOfSelectingAnotherProcess(string runId)
    {
        var registry = OperationRegistry.CreateDefault(TestRepository.FindRoot());
        var exception = Assert.ThrowsAsync<ArgumentException>(async () => await registry.InvokeAsync(
            "gateway_screenshot", JsonSerializer.SerializeToElement(new { runId }),
            CancellationToken.None));
        Assert.That(exception!.Message, Does.Contain("run").IgnoreCase);
        Assert.That(exception.Message, Does.Not.Contain("Unknown operation"));
    }
}
