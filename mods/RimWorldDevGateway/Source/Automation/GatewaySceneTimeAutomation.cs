namespace RimWorldDevGateway;

public static class GatewaySceneTimeAutomation
{
    public const string Name = "scene.time";

    public static void Register(GatewayAutomationRegistry registry, IGatewaySceneClockWorld? world = null)
    {
        var clock = new GatewaySceneClockController(world ?? new VerseGatewaySceneClockWorld());
        registry.RegisterBuiltIn(new GatewayAutomationDescriptor(Name, "1.0",
                "Set current-map local time or restore a captured global clock offset without advancing simulation ticks.",
                new Dictionary<string, object?>
                {
                    ["type"] = "object", ["additionalProperties"] = false,
                    ["required"] = new[] { "version", "mapHandle" },
                    ["properties"] = new Dictionary<string, object?>
                    {
                        ["version"] = new Dictionary<string, object?> { ["type"] = "integer", ["const"] = 1 },
                        ["mapHandle"] = new Dictionary<string, object?> { ["type"] = "string" },
                        ["minuteOfDay"] = new Dictionary<string, object?>
                            { ["type"] = "integer", ["minimum"] = 0, ["maximum"] = 1439 },
                        ["gameStartAbsTick"] = new Dictionary<string, object?> { ["type"] = "integer" }
                    },
                    ["oneOf"] = new object[]
                    {
                        new Dictionary<string, object?> { ["required"] = new[] { "minuteOfDay" } },
                        new Dictionary<string, object?> { ["required"] = new[] { "gameStartAbsTick" } }
                    }
                }, new[] { "program-state-playing", "current-map", "no-long-event" }, true),
            (context, arguments) =>
            {
                if (arguments.Keys.Any(key => key is not ("version" or "mapHandle" or "minuteOfDay" or "gameStartAbsTick")) ||
                    Integer(arguments, "version") != 1 ||
                    !arguments.TryGetValue("mapHandle", out var map) || map is not string mapHandle)
                    throw new GatewayAutomationException("invalid_scene_time", "Unknown field, missing map handle or unsupported version.");
                var minute = Integer(arguments, "minuteOfDay");
                var offset = Integer(arguments, "gameStartAbsTick");
                var before = clock.Capture();
                context.AddArtifact("scene-clock-before", "clock-snapshot", before);
                var applied = false;
                try
                {
                    return context.RunStep("set-scene-time", () =>
                    {
                        var result = clock.Apply(mapHandle, minute, offset);
                        applied = true;
                        // Artifacts survive the registry's final cancellation/deadline check,
                        // even when it discards the normal Result after this handler returns.
                        context.AddArtifact("scene-clock-after", "clock-snapshot", result.After);
                        return result;
                    });
                }
                catch
                {
                    if (applied)
                    {
                        var restored = clock.Apply(mapHandle, null, before.GameStartAbsTick);
                        context.AddArtifact("scene-clock-restored", "clock-snapshot", restored.After);
                    }
                    throw;
                }
            });
    }

    private static int? Integer(IReadOnlyDictionary<string, object?> arguments, string name)
    {
        if (!arguments.TryGetValue(name, out var value)) return null;
        if (value is int integer) return integer;
        if (value is long longer && longer >= int.MinValue && longer <= int.MaxValue) return (int)longer;
        // Swan's HTTP JSON reader represents numeric tokens as decimal.
        if (value is decimal number && number >= int.MinValue && number <= int.MaxValue &&
            decimal.Truncate(number) == number) return (int)number;
        throw new GatewayAutomationException("invalid_scene_time", name + " must be an integer.");
    }
}
