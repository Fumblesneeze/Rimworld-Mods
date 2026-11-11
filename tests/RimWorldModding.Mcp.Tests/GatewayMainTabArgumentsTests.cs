using System.Text.Json;
using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class GatewayMainTabArgumentsTests
{
    [Test]
    public void PublicRegistryRegistersTheMutationAndValidatesArgumentsBeforeTransport()
    {
        var registry = OperationRegistry.CreateDefault(TestRepository.FindRoot());
        Assert.That(registry.Descriptors.SingleOrDefault(item => item.Name == "gateway_main_tab")?.Risk,
            Is.EqualTo(OperationRisk.DestructiveLocal));
        using var invalid = JsonDocument.Parse("{\"runId\":\"not-a-lease\",\"defName\":\"Assign\",\"open\":true,\"force\":true}");
        var failure = Assert.ThrowsAsync<ArgumentException>(async () =>
            await registry.InvokeAsync("gateway_main_tab", invalid.RootElement, CancellationToken.None));
        Assert.That(failure!.Message, Is.EqualTo("Main tab accepts runId, defName and open exactly once, without extra fields."));
    }

    [TestCase("{\"runId\":\"run\",\"defName\":\"Assign\",\"open\":true,\"force\":true}")]
    [TestCase("{\"runId\":\"run\",\"defName\":\"Assign\",\"open\":\"true\"}")]
    [TestCase("{\"runId\":\"run\",\"defName\":\"Assign\",\"open\":null}")]
    [TestCase("{\"runId\":\"run\",\"defName\":\"Assign\"}")]
    [TestCase("{\"runId\":\" \",\"defName\":\"Assign\",\"open\":true}")]
    [TestCase("{\"runId\":\"run\",\"defName\":\" \",\"open\":true}")]
    [TestCase("{\"runId\":\"run\",\"defName\":\"Assign\",\"open\":true,\"open\":false}")]
    [TestCase("[]")]
    public void InvalidPublicArgumentsFailBeforeLeaseOrTransport(string json)
    {
        using var input = JsonDocument.Parse(json);
        Assert.Throws<ArgumentException>(() => GatewayMainTabArguments.Parse(input.RootElement));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ExactAssignRequestSelectsTheVersionedNativeTabAutomation(bool open)
    {
        using var input = JsonDocument.Parse(JsonSerializer.Serialize(new { runId = "exact-run", defName = "Assign", open }));
        var request = GatewayMainTabArguments.Parse(input.RootElement);
        Assert.That(request.TryGetProperty("kind", out var kind), Is.True,
            "The typed operation must select the existing automation mutation path.");
        Assert.That(kind.GetString(), Is.EqualTo("automation"));
        Assert.That(request.GetProperty("name").GetString(), Is.EqualTo("native-main-tab.set-open"));
        Assert.That(request.GetProperty("runId").GetString(), Is.EqualTo("exact-run"));
        using var body = JsonDocument.Parse(request.GetProperty("argumentsJson").GetString()!);
        Assert.That(body.RootElement.GetProperty("version").GetInt32(), Is.EqualTo(1));
        Assert.That(body.RootElement.GetProperty("defName").GetString(), Is.EqualTo("Assign"));
        Assert.That(body.RootElement.GetProperty("open").GetBoolean(), Is.EqualTo(open));
        Assert.That(body.RootElement.EnumerateObject().Count(), Is.EqualTo(3));
    }
}
