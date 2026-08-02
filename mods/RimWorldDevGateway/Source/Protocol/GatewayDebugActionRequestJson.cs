using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;
using Swan.Formatters;

namespace RimWorldDevGateway;

public static class GatewayDebugActionRequestJson
{
    private static readonly HashSet<string> AllowedProperties = new(StringComparer.Ordinal)
    {
        "search",
        "categories",
        "allowedGameStates",
        "modes",
        "after",
        "limit"
    };

    public static GatewayDebugActionQuery ReadQuery(string json)
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
            throw new SerializationException("Debug-action query body is not valid JSON.", exception);
        }

        if (value is not IDictionary dictionary)
        {
            throw new SerializationException("Debug-action query JSON must contain an object.");
        }

        var properties = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (DictionaryEntry entry in dictionary)
        {
            if (entry.Key is not string name || !AllowedProperties.Contains(name))
            {
                throw new SerializationException(
                    $"Unknown debug-action query property '{entry.Key}'.");
            }

            properties.Add(name, entry.Value);
        }

        return new GatewayDebugActionQuery(
            ReadNullableString(properties, "search"),
            ReadStrings(properties, "categories"),
            ReadModes(properties),
            ReadNullableString(properties, "after"),
            ReadInt32(properties, "limit", 100),
            ReadStrings(properties, "allowedGameStates"));
    }

    private static IReadOnlyList<string> ReadStrings(
        IReadOnlyDictionary<string, object?> properties,
        string name)
    {
        if (!properties.TryGetValue(name, out var value) || value is null)
        {
            return Array.Empty<string>();
        }

        if (value is string || value is not IEnumerable sequence)
        {
            throw new SerializationException(
                $"Debug-action query property '{name}' must be an array of strings.");
        }

        var values = new List<string>();
        foreach (var item in sequence)
        {
            if (item is not string text)
            {
                throw new SerializationException(
                    $"Debug-action query property '{name}' must contain only strings.");
            }

            values.Add(text);
        }

        return values;
    }

    private static IReadOnlyList<GatewayDebugActionMode> ReadModes(
        IReadOnlyDictionary<string, object?> properties)
    {
        var values = ReadStrings(properties, "modes");
        var result = new List<GatewayDebugActionMode>(values.Count);
        foreach (var value in values)
        {
            if (!Enum.GetNames(typeof(GatewayDebugActionMode)).Any(name =>
                    string.Equals(name, value, StringComparison.OrdinalIgnoreCase)) ||
                !Enum.TryParse<GatewayDebugActionMode>(value, ignoreCase: true, out var mode) ||
                !Enum.IsDefined(typeof(GatewayDebugActionMode), mode))
            {
                throw new SerializationException(
                    $"Unknown debug-action invocation mode '{value}'.");
            }

            result.Add(mode);
        }

        return result;
    }

    private static string? ReadNullableString(
        IReadOnlyDictionary<string, object?> properties,
        string name)
    {
        if (!properties.TryGetValue(name, out var value) || value is null)
        {
            return null;
        }

        return value as string ?? throw new SerializationException(
            $"Debug-action query property '{name}' must be a string or null.");
    }

    private static int ReadInt32(
        IReadOnlyDictionary<string, object?> properties,
        string name,
        int defaultValue)
    {
        if (!properties.TryGetValue(name, out var value))
        {
            return defaultValue;
        }

        try
        {
            var result = Convert.ToInt32(value, CultureInfo.InvariantCulture);
            if (value is float or double or decimal &&
                Convert.ToDecimal(value, CultureInfo.InvariantCulture) != result)
            {
                throw new OverflowException();
            }

            return result;
        }
        catch (Exception exception) when (exception is FormatException or InvalidCastException or OverflowException)
        {
            throw new SerializationException(
                $"Debug-action query property '{name}' must be a 32-bit integer.",
                exception);
        }
    }
}
