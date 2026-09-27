using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayMapPointerSelectionTests
{
    [Test]
    public void Pointer_action_is_discoverable_and_valid_input_is_queued()
    {
        var dispatcher = new GatewayDispatcher();
        var registry = new GatewaySemanticActionRegistry(dispatcher, new VerseGatewaySemanticActionOperations());
        var discovery = registry.DiscoverAsync("pointer-discover");
        dispatcher.Drain(DispatchPhase.Update);
        var action = discovery.Result.Single(x => x.Name == "map.pointer.select");
        Assert.That(action.ArgumentSchema.Select(x => x.Name), Is.EqualTo(new[] { "x", "z" }));
        Assert.That(action.Available, Is.False, "An empty host is not a playable map.");
        var pending = registry.InvokeAsync("pointer-click", "map.pointer.select",
            new Dictionary<string, object?> { ["x"] = 20.25d, ["z"] = 30.125d });
        Assert.That(pending.IsCompleted, Is.False);
        dispatcher.Drain(DispatchPhase.Update);
        Assert.That(pending.Result.Error?.Code, Is.EqualTo("action_unavailable"), pending.Result.Error?.Message);
    }

    [Test]
    public void Invalid_pointer_coordinates_are_rejected_before_dispatch()
    {
        var registry = new GatewaySemanticActionRegistry(new GatewayDispatcher(), new VerseGatewaySemanticActionOperations());
        foreach (var args in new[]
        {
            new Dictionary<string, object?>(),
            new Dictionary<string, object?> { ["x"] = 2d, ["z"] = double.NaN },
            new Dictionary<string, object?> { ["x"] = "2", ["z"] = 3d },
            new Dictionary<string, object?> { ["x"] = 2d, ["z"] = 3d, ["extra"] = true }
        })
        {
            var pending = registry.InvokeAsync("invalid-pointer", "map.pointer.select", args);
            Assert.That(pending.IsCompleted, Is.True);
            Assert.That(pending.Result.Error?.Code, Is.EqualTo("invalid_argument"));
        }
    }
}
