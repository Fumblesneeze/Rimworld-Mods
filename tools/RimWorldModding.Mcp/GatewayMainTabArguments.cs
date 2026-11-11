using System.Text.Json;

namespace RimWorldModding.Mcp;

public static class GatewayMainTabArguments
{
    public static JsonElement Parse(JsonElement arguments)
    {
        if (arguments.ValueKind != JsonValueKind.Object)
            throw new ArgumentException("Main tab arguments must be one object with runId, defName and open.");
        var properties = arguments.EnumerateObject().ToArray();
        if (properties.Any(property => property.Name is not ("runId" or "defName" or "open")) ||
            properties.Select(property => property.Name).Distinct(StringComparer.Ordinal).Count() != properties.Length)
            throw new ArgumentException("Main tab accepts runId, defName and open exactly once, without extra fields.");
        string runId = Text("runId"), defName = Text("defName");
        if (!arguments.TryGetProperty("open", out var state) || state.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
            throw new ArgumentException("open must be a Boolean.");
        bool open = state.GetBoolean();
        return JsonSerializer.SerializeToElement(new
        {
            runId, kind = "automation", name = "native-main-tab.set-open",
            argumentsJson = JsonSerializer.Serialize(new { version = 1, defName, open })
        });

        string Text(string name) => arguments.TryGetProperty(name, out var value) &&
            value.ValueKind == JsonValueKind.String && !string.IsNullOrWhiteSpace(value.GetString())
                ? value.GetString()! : throw new ArgumentException(name + " must be a nonempty string.");
    }
}
