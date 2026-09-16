using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;
using System.Threading;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewaySceneClockTests
{
    [Test]
    public void Serialized_http_arguments_preserve_integer_clock_values()
    {
        var request = GatewayAutomationRequestJson.Read(
            "{\"arguments\":{\"version\":1,\"mapHandle\":\"map-7\",\"minuteOfDay\":720}}");
        var world = new ClockWorld();
        var registry = new GatewayAutomationRegistry();
        GatewaySceneTimeAutomation.Register(registry, world);
        var run = registry.StartRun("scene.time", "serialized-clock", request.Arguments);
        Assert.That(run.State, Is.EqualTo("succeeded"),
            string.Join(", ", request.Arguments.Select(pair => pair.Key + "=" + pair.Value?.GetType().FullName)) + "; " + run.Error?.Message);
        Assert.That(((GatewaySceneClockResult)run.Result!).After.LocalDayTick, Is.EqualTo(30000));
    }

    [Test]
    public void Native_uninitialized_zero_offset_is_rejected_for_both_modes()
    {
        var world = new ClockWorld { Offset = 15000 };
        var controller = new GatewaySceneClockController(world);
        Assert.Throws<GatewayAutomationException>(() => controller.Apply("map-7", null, 0));
        Assert.Throws<GatewayAutomationException>(() => controller.Apply("map-7", 360, null));
        Assert.That(world.Writes, Is.Zero);
    }

    [TestCase("720.5")]
    [TestCase("2147483648")]
    [TestCase("\"720\"")]
    public void Serialized_invalid_clock_numbers_are_rejected_without_writes(string token)
    {
        var request = GatewayAutomationRequestJson.Read(
            "{\"arguments\":{\"version\":1,\"mapHandle\":\"map-7\",\"minuteOfDay\":" + token + "}}");
        var world = new ClockWorld();
        var registry = new GatewayAutomationRegistry();
        GatewaySceneTimeAutomation.Register(registry, world);
        var run = registry.StartRun("scene.time", "invalid-serialized-clock", request.Arguments);
        Assert.That(run.Error!.Code, Is.EqualTo("invalid_scene_time"));
        Assert.That(world.Writes, Is.Zero);
    }

    [Test]
    public void Cancellation_after_write_restores_offset_and_retains_recovery_artifacts()
    {
        using var cancellation = new CancellationTokenSource();
        var world = new ClockWorld { OnWrite = cancellation.Cancel };
        var registry = new GatewayAutomationRegistry();
        GatewaySceneTimeAutomation.Register(registry, world);
        var run = registry.StartRun("scene.time", "cancel-after-write", new Dictionary<string, object?>
            { ["version"] = 1, ["mapHandle"] = "map-7", ["minuteOfDay"] = 720 },
            cancellationToken: cancellation.Token);
        Assert.That(run.State, Is.EqualTo("cancelled"));
        Assert.That(world.Offset, Is.EqualTo(3600000));
        Assert.That(run.Artifacts.Any(artifact => artifact.Name == "scene-clock-before"), Is.True);
        Assert.That(run.Artifacts.Any(artifact => artifact.Name == "scene-clock-restored"), Is.True);
    }

    [Test]
    public void Cancellation_at_final_registry_check_retains_before_and_applied_clock()
    {
        using var cancellation = new CancellationTokenSource();
        var world = new ClockWorld();
        var checksAfterWrite = 0;
        var registry = new GatewayAutomationRegistry(utcNow: () =>
        {
            if (world.Writes == 1 && ++checksAfterWrite == 2) cancellation.Cancel();
            return DateTimeOffset.UtcNow;
        });
        GatewaySceneTimeAutomation.Register(registry, world);
        var run = registry.StartRun("scene.time", "cancel-final-check", new Dictionary<string, object?>
            { ["version"] = 1, ["mapHandle"] = "map-7", ["minuteOfDay"] = 720 },
            cancellationToken: cancellation.Token);
        Assert.That(run.State, Is.EqualTo("cancelled"));
        Assert.That(run.Result, Is.Null);
        var before = (GatewaySceneClockSnapshot)run.Artifacts.Single(artifact => artifact.Name == "scene-clock-before").Value!;
        var after = (GatewaySceneClockSnapshot)run.Artifacts.Single(artifact => artifact.Name == "scene-clock-after").Value!;
        Assert.That(before.GameStartAbsTick, Is.EqualTo(3600000));
        Assert.That(after.GameStartAbsTick, Is.EqualTo(world.Offset));
        new GatewaySceneClockController(world).Apply("map-7", null, before.GameStartAbsTick);
        Assert.That(world.Offset, Is.EqualTo(3600000));
    }
    [Test]
    public void Typed_fixture_records_setup_and_restores_after_test_failure()
    {
        var world = new ClockWorld();
        var context = new GatewayEndToEndTestContext(() => 0, () => world.Ticks, _ => null);
        var outcome = new GatewaySceneTimeFixture(world).Apply(
            new SupportingSceneTimeActionStep("noon", "map-7", 720), context);
        Assert.That(world.Capture().LocalDayTick, Is.EqualTo(30000));
        Assert.That(outcome.Artifacts["supportingFixture"], Is.EqualTo("calendar-offset-only"));
        world.Ticks += 60;
        context.RunDeferredCleanup();
        Assert.That(world.Offset, Is.EqualTo(3600000));
        Assert.That(world.Ticks, Is.EqualTo(180));
    }

    [Test]
    public void Named_automation_is_discoverable_and_cancelled_requests_never_mutate()
    {
        var world = new ClockWorld();
        var registry = new GatewayAutomationRegistry();
        GatewaySceneTimeAutomation.Register(registry, world);
        Assert.That(registry.Describe().Single().Name, Is.EqualTo("scene.time"));
        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        var run = registry.StartRun("scene.time", "cancel-clock", new Dictionary<string, object?>
            { ["version"] = 1, ["mapHandle"] = "map-7", ["minuteOfDay"] = 720 },
            cancellationToken: cancelled.Token);
        Assert.That(run.State, Is.EqualTo("cancelled"));
        Assert.That(world.Writes, Is.Zero);
    }

    [Test]
    public void Named_automation_rejects_unknown_fields_and_records_before_after()
    {
        var world = new ClockWorld();
        var registry = new GatewayAutomationRegistry();
        GatewaySceneTimeAutomation.Register(registry, world);
        var arguments = new Dictionary<string, object?>
            { ["version"] = 1, ["mapHandle"] = "map-7", ["minuteOfDay"] = 720, ["weather"] = "Clear" };
        var invalid = registry.StartRun("scene.time", "invalid-clock", arguments);
        Assert.That(invalid.Error!.Code, Is.EqualTo("invalid_scene_time"));
        Assert.That(world.Writes, Is.Zero);
        arguments.Remove("weather");
        var valid = registry.StartRun("scene.time", "valid-clock", arguments);
        Assert.That(valid.State, Is.EqualTo("succeeded"));
        Assert.That(((GatewaySceneClockResult)valid.Result!).After.LocalDayTick, Is.EqualTo(30000));
    }
    [TestCase(0, 0)]
    [TestCase(720, 30000)]
    [TestCase(1439, 59958)]
    public void Setting_local_minute_preserves_simulation_ticks_and_returns_restorable_offset(int minute, int targetTick)
    {
        var world = new ClockWorld();
        var result = new GatewaySceneClockController(world).Apply("map-7", minute, null);
        Assert.That(result.Before.GameStartAbsTick, Is.EqualTo(3600000));
        Assert.That(result.After.GameStartAbsTick, Is.EqualTo(3600000 + targetTick - 15000));
        Assert.That(result.After.LocalDayTick, Is.EqualTo(targetTick));
        Assert.That(result.After.TicksGame, Is.EqualTo(120));
        Assert.That(world.Writes, Is.EqualTo(1));
    }

    [Test]
    public void Restore_keeps_intervening_simulation_ticks()
    {
        var world = new ClockWorld();
        var controller = new GatewaySceneClockController(world);
        var changed = controller.Apply("map-7", 720, null);
        world.Ticks += 60;
        var restored = controller.Apply("map-7", null, changed.Before.GameStartAbsTick);
        Assert.That(restored.After.GameStartAbsTick, Is.EqualTo(3600000));
        Assert.That(restored.After.TicksGame, Is.EqualTo(180));
        Assert.That(restored.After.LocalDayTick, Is.EqualTo(15060));
    }

    [TestCase(-1, null, "map-7", "invalid_scene_time")]
    [TestCase(1440, null, "map-7", "invalid_scene_time")]
    [TestCase(null, null, "map-7", "invalid_scene_time")]
    [TestCase(720, 3600000, "map-7", "invalid_scene_time")]
    [TestCase(720, null, "map-8", "stale_map_handle")]
    [TestCase(null, int.MaxValue, "map-7", "invalid_scene_time")]
    public void Invalid_requests_do_not_mutate(int? minute, int? offset, string map, string code)
    {
        var world = new ClockWorld();
        var error = Assert.Throws<GatewayAutomationException>(() =>
            new GatewaySceneClockController(world).Apply(map, minute, offset));
        Assert.That(error!.Code, Is.EqualTo(code));
        Assert.That(world.Writes, Is.Zero);
    }

    private sealed class ClockWorld : IGatewaySceneClockWorld
    {
        public int Offset = 3600000;
        public int Ticks = 120;
        public int Writes;
        public Action? OnWrite;
        public GatewaySceneClockSnapshot Capture() => new("map-7", Offset, Ticks,
            ((15000 + Offset - 3600000 + Ticks - 120) % 60000 + 60000) % 60000);
        public void SetGameStartAbsTick(int value) { Offset = value; Writes++; OnWrite?.Invoke(); }
    }
}
