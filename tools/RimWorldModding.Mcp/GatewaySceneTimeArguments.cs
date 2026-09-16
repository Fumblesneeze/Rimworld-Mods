using System.Text.Json;

namespace RimWorldModding.Mcp;

public static class GatewaySceneTimeArguments
{
    public static JsonElement Parse(JsonElement arguments)
    {
        if (arguments.ValueKind != JsonValueKind.Object ||
            arguments.EnumerateObject().Any(property => property.Name is not
                ("runId" or "mapHandle" or "minuteOfDay" or "gameStartAbsTick")))
            throw new ArgumentException("Scene time accepts runId, mapHandle, minuteOfDay or gameStartAbsTick only.");
        return Create(Text("runId"), Text("mapHandle"), Integer("minuteOfDay"), Integer("gameStartAbsTick"));

        string Text(string name) => arguments.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()! : throw new ArgumentException(name + " must be a string.");
        int? Integer(string name)
        {
            if (!arguments.TryGetProperty(name, out var value) || value.ValueKind == JsonValueKind.Null) return null;
            return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var number)
                ? number : throw new ArgumentException(name + " must be an integer.");
        }
    }

    public static JsonElement Create(string runId, string mapHandle, int? minuteOfDay, int? gameStartAbsTick)
    {
        if (string.IsNullOrWhiteSpace(runId) || string.IsNullOrWhiteSpace(mapHandle) || mapHandle.Length > 256 ||
            minuteOfDay.HasValue == gameStartAbsTick.HasValue || minuteOfDay < 0 || minuteOfDay > 1439)
            throw new ArgumentException("Specify runId, mapHandle and exactly one of minuteOfDay (0–1439) or gameStartAbsTick.");
        var body = new Dictionary<string, object> { ["version"] = 1, ["mapHandle"] = mapHandle };
        if (minuteOfDay.HasValue) body["minuteOfDay"] = minuteOfDay.Value;
        else body["gameStartAbsTick"] = gameStartAbsTick!.Value;
        return JsonSerializer.SerializeToElement(new
        {
            runId, kind = "automation", name = "scene.time", argumentsJson = JsonSerializer.Serialize(body)
        });
    }
}
