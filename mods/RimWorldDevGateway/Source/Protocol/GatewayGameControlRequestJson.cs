using System.Collections;
using System.Runtime.Serialization;
using RimWorldDevGateway.Contracts;
using Swan.Formatters;

namespace RimWorldDevGateway;

public static class GatewayGameControlRequestJson
{
    private static readonly HashSet<string> AllowedProperties = new(StringComparer.Ordinal)
    {
        "devMode",
        "godMode",
        "paused",
        "speed"
    };

    public static GatewayGameStateMutationRequest Read(string json)
    {
        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        object? value;
        try
        {
            value = Json.Deserialize(json);
        }
        catch (Exception exception)
        {
            throw new SerializationException("Game-state request body is not valid JSON.", exception);
        }

        if (value is not IDictionary dictionary)
        {
            throw new SerializationException("Game-state request JSON must contain an object.");
        }

        var properties = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (DictionaryEntry entry in dictionary)
        {
            if (entry.Key is not string name || !AllowedProperties.Contains(name))
            {
                throw new SerializationException(
                    $"Unknown game-state request property '{entry.Key}'.");
            }

            properties.Add(name, entry.Value);
        }

        return new GatewayGameStateMutationRequest
        {
            DevMode = ReadNullableBoolean(properties, "devMode"),
            GodMode = ReadNullableBoolean(properties, "godMode"),
            Paused = ReadNullableBoolean(properties, "paused"),
            Speed = ReadNullableString(properties, "speed")
        };
    }

    private static bool? ReadNullableBoolean(
        IReadOnlyDictionary<string, object?> properties,
        string name)
    {
        if (!properties.TryGetValue(name, out var value) || value is null)
        {
            return null;
        }

        if (value is bool boolean)
        {
            return boolean;
        }

        throw new SerializationException($"Game-state property '{name}' must be a boolean or null.");
    }

    private static string? ReadNullableString(
        IReadOnlyDictionary<string, object?> properties,
        string name)
    {
        if (!properties.TryGetValue(name, out var value) || value is null)
        {
            return null;
        }

        if (value is string text)
        {
            return text;
        }

        throw new SerializationException($"Game-state property '{name}' must be a string or null.");
    }
}
