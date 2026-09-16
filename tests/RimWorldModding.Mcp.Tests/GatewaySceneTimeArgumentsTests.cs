using System.Text.Json;
using NUnit.Framework;

namespace RimWorldModding.Mcp.Tests;

[TestFixture]
public sealed class GatewaySceneTimeArgumentsTests
{
    [TestCase("{\"runId\":\"run\",\"mapHandle\":\"map-7\",\"minuteOfDay\":720,\"weather\":\"Clear\"}")]
    [TestCase("{\"runId\":\"run\",\"mapHandle\":\"map-7\",\"minuteOfDay\":\"720\"}")]
    [TestCase("{\"runId\":\"run\",\"mapHandle\":\"map-7\",\"minuteOfDay\":720.5}")]
    public void Public_operation_rejects_unknown_fields_and_noninteger_minutes(string json)
    {
        using var document = JsonDocument.Parse(json);
        Assert.Throws<ArgumentException>(() => GatewaySceneTimeArguments.Parse(document.RootElement));
    }

    [Test]
    public void Typed_noon_and_restore_calls_select_the_versioned_automation()
    {
        var set = GatewaySceneTimeArguments.Create("exact-run", "map-7", 720, null);
        var restore = GatewaySceneTimeArguments.Create("exact-run", "map-7", null, 3600000);
        Assert.That(set.GetProperty("name").GetString(), Is.EqualTo("scene.time"));
        Assert.That(set.GetProperty("kind").GetString(), Is.EqualTo("automation"));
        Assert.That(set.GetProperty("runId").GetString(), Is.EqualTo("exact-run"));
        using var setBody = JsonDocument.Parse(set.GetProperty("argumentsJson").GetString()!);
        using var restoreBody = JsonDocument.Parse(restore.GetProperty("argumentsJson").GetString()!);
        Assert.That(setBody.RootElement.GetProperty("version").GetInt32(), Is.EqualTo(1));
        Assert.That(setBody.RootElement.GetProperty("minuteOfDay").GetInt32(), Is.EqualTo(720));
        Assert.That(setBody.RootElement.TryGetProperty("gameStartAbsTick", out _), Is.False);
        Assert.That(restoreBody.RootElement.GetProperty("gameStartAbsTick").GetInt32(), Is.EqualTo(3600000));
        Assert.That(restoreBody.RootElement.TryGetProperty("minuteOfDay", out _), Is.False);
    }

    [TestCase(-1, null)]
    [TestCase(1440, null)]
    [TestCase(null, null)]
    [TestCase(720, 3600000)]
    public void Invalid_modes_fail_before_lease_or_transport(int? minute, int? offset) =>
        Assert.Throws<ArgumentException>(() => GatewaySceneTimeArguments.Create("run", "map-7", minute, offset));
}
