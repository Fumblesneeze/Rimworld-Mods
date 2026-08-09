using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;
using Swan.Formatters;

namespace RimWorldDevGateway;

public static class GatewayGizmoRequestJson
{
    private static readonly HashSet<string> QueryProperties = new(StringComparer.Ordinal)
    {
        "ownerScope",
        "ownerHandles",
        "architectCategoryDefNames",
        "limit"
    };

    private static readonly HashSet<string> InputProperties = new(StringComparer.Ordinal)
    {
        "kind",
        "thingHandle",
        "cell",
        "cells",
        "start",
        "end",
        "cornerA",
        "cornerB",
        "rotation"
    };

    public static GatewayGizmoQuery ReadQuery(string json)
    {
        var properties = ReadObject(json, "Gizmo query", QueryProperties);
        var scope = ReadString(properties, "ownerScope", required: false) ?? "selection";
        var owners = ReadStrings(properties, "ownerHandles");
        var categories = ReadStrings(properties, "architectCategoryDefNames");
        var limit = ReadInt32(properties, "limit", GatewayGizmoRegistry.MaximumResults);
        if (string.Equals(scope, "selection", StringComparison.OrdinalIgnoreCase))
        {
            if (owners.Count != 0)
            {
                throw Error("A selection-scope gizmo query cannot include ownerHandles.");
            }

            return GatewayGizmoQuery.ForSelection(limit, categories);
        }

        if (string.Equals(scope, "explicitOwners", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(scope, "owners", StringComparison.OrdinalIgnoreCase))
        {
            return GatewayGizmoQuery.ForOwners(owners, limit, categories);
        }

        throw Error("Gizmo ownerScope must be 'selection' or 'explicitOwners'.");
    }

    public static GatewayInteractionInput ReadInteractionInput(string json)
    {
        var properties = ReadObject(json, "Interaction input", InputProperties);
        var kindText = ReadString(properties, "kind", required: true)!;
        if (!Enum.GetNames(typeof(GatewayInteractionInputKind)).Any(name =>
                string.Equals(name, kindText, StringComparison.OrdinalIgnoreCase)) ||
            !Enum.TryParse<GatewayInteractionInputKind>(kindText, ignoreCase: true, out var kind) ||
            !Enum.IsDefined(typeof(GatewayInteractionInputKind), kind))
        {
            throw Error($"Unknown interaction input kind '{kindText}'.");
        }

        return kind switch
        {
            GatewayInteractionInputKind.Thing =>
                GatewayInteractionInput.ForThing(
                    RequireOnlyString(properties, "thingHandle", kind)),
            GatewayInteractionInputKind.Cell =>
                GatewayInteractionInput.ForCell(
                    RequireOnlyCell(properties, "cell", kind, "rotation"),
                    ReadCardinalRotation(properties)),
            GatewayInteractionInputKind.Cells =>
                GatewayInteractionInput.ForCells(
                    RequireOnlyCells(properties, "cells", kind)),
            GatewayInteractionInputKind.Line =>
                GatewayInteractionInput.ForLine(
                    RequireCell(properties, "start"),
                    RequireOnlyCell(properties, "end", kind, "start")),
            GatewayInteractionInputKind.Rectangle =>
                GatewayInteractionInput.ForRectangle(
                    RequireCell(properties, "cornerA"),
                    RequireOnlyCell(properties, "cornerB", kind, "cornerA")),
            _ => throw Error($"Interaction input kind '{kind}' is unsupported.")
        };
    }

    private static Dictionary<string, object?> ReadObject(
        string json,
        string description,
        ISet<string> allowedProperties)
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
            throw new SerializationException(description + " body is not valid JSON.", exception);
        }

        if (value is not IDictionary dictionary)
        {
            throw Error(description + " JSON must contain an object.");
        }

        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (DictionaryEntry entry in dictionary)
        {
            if (entry.Key is not string name || !allowedProperties.Contains(name))
            {
                throw Error($"Unknown {description.ToLowerInvariant()} property '{entry.Key}'.");
            }

            result.Add(name, entry.Value);
        }

        return result;
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
            throw Error($"Property '{name}' must be an array of strings.");
        }

        var result = new List<string>();
        foreach (var item in sequence)
        {
            if (item is not string text)
            {
                throw Error($"Property '{name}' must contain only strings.");
            }

            result.Add(text);
        }

        return result;
    }

    private static string? ReadString(
        IReadOnlyDictionary<string, object?> properties,
        string name,
        bool required)
    {
        if (!properties.TryGetValue(name, out var value) || value is null)
        {
            if (!required)
            {
                return null;
            }

            throw Error($"Property '{name}' is required.");
        }

        return value as string ?? throw Error($"Property '{name}' must be a string.");
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
                $"Property '{name}' must be a 32-bit integer.",
                exception);
        }
    }

    private static string RequireOnlyString(
        IReadOnlyDictionary<string, object?> properties,
        string name,
        GatewayInteractionInputKind kind)
    {
        RejectExtraneousShapeProperties(properties, kind, name);
        return ReadString(properties, name, required: true)!;
    }

    private static GatewayMapCell RequireOnlyCell(
        IReadOnlyDictionary<string, object?> properties,
        string name,
        GatewayInteractionInputKind kind,
        params string[] additionalAllowed)
    {
        RejectExtraneousShapeProperties(
            properties,
            kind,
            new[] { name }.Concat(additionalAllowed).ToArray());
        return RequireCell(properties, name);
    }

    private static IReadOnlyList<GatewayMapCell> RequireOnlyCells(
        IReadOnlyDictionary<string, object?> properties,
        string name,
        GatewayInteractionInputKind kind)
    {
        RejectExtraneousShapeProperties(properties, kind, name);
        if (!properties.TryGetValue(name, out var value) ||
            value is string || value is not IEnumerable sequence)
        {
            throw Error($"Property '{name}' must be an array of map cells.");
        }

        var result = new List<GatewayMapCell>();
        foreach (var item in sequence)
        {
            result.Add(ReadCell(item, name));
        }

        return result;
    }

    private static GatewayMapCell RequireCell(
        IReadOnlyDictionary<string, object?> properties,
        string name)
    {
        if (!properties.TryGetValue(name, out var value))
        {
            throw Error($"Property '{name}' is required.");
        }

        return ReadCell(value, name);
    }

    private static GatewayMapCell ReadCell(object? value, string name)
    {
        if (value is not IDictionary dictionary)
        {
            throw Error($"Property '{name}' must contain an x/z map-cell object.");
        }

        var coordinates = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (DictionaryEntry entry in dictionary)
        {
            if (entry.Key is not string key || key is not ("x" or "z"))
            {
                throw Error($"Unknown map-cell property '{entry.Key}' in '{name}'.");
            }

            coordinates.Add(key, entry.Value);
        }

        if (!coordinates.ContainsKey("x") || !coordinates.ContainsKey("z"))
        {
            throw Error($"Property '{name}' requires integer x and z coordinates.");
        }

        return new GatewayMapCell(
            ReadInt32(coordinates, "x", 0),
            ReadInt32(coordinates, "z", 0));
    }

    private static GatewayCardinalRotation? ReadCardinalRotation(
        IReadOnlyDictionary<string, object?> properties)
    {
        var text = ReadString(properties, "rotation", required: false);
        if (text is null)
        {
            return null;
        }

        if (!Enum.TryParse<GatewayCardinalRotation>(text, ignoreCase: true, out var rotation) ||
            !Enum.IsDefined(typeof(GatewayCardinalRotation), rotation))
        {
            throw Error(
                "Property 'rotation' must name one cardinal rotation: North, East, South, or West.");
        }

        return rotation;
    }

    private static void RejectExtraneousShapeProperties(
        IReadOnlyDictionary<string, object?> properties,
        GatewayInteractionInputKind kind,
        params string[] allowed)
    {
        var allowedSet = new HashSet<string>(allowed, StringComparer.Ordinal) { "kind" };
        var extra = properties.Keys.FirstOrDefault(name => !allowedSet.Contains(name));
        if (extra is not null)
        {
            throw Error($"Interaction kind '{kind}' does not accept property '{extra}'.");
        }
    }

    private static SerializationException Error(string message) => new(message);
}
