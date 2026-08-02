using System.Collections;
using System.Runtime.Serialization;
using RimWorldDevGateway.Contracts;
using Swan.Formatters;

namespace RimWorldDevGateway;

/// <summary>
/// Reads the one gateway request whose argument values are deliberately recursive.
/// Other contract DTOs continue to use DataContractJsonSerializer.
/// </summary>
public static class GatewayAutomationRequestJson
{
    public const int MaximumCharacters = 32 * 1024 * 1024;
    public const int MaximumDepth = 32;
    public const int MaximumNodes = 100_000;

    public static GatewayAutomationRunRequest Read(string json)
    {
        if (json is null)
        {
            throw new ArgumentNullException(nameof(json));
        }

        if (json.Length > MaximumCharacters)
        {
            throw Error("Automation request JSON exceeds the 32 MiB character limit.");
        }

        ValidateStructuralDepth(json);

        object? value;
        try
        {
            value = Json.Deserialize(json);
        }
        catch (Exception exception)
        {
            throw new SerializationException("Automation request body is not valid JSON.", exception);
        }

        var nodes = 0;
        var root = Normalize(value, 0, ref nodes) as Dictionary<string, object?> ??
            throw Error("Automation request JSON must contain an object.");
        var request = new GatewayAutomationRunRequest();
        if (root.TryGetValue("arguments", out var arguments))
        {
            request.Arguments = arguments as Dictionary<string, object?> ??
                throw Error("Automation request 'arguments' must be an object.");
        }

        if (root.TryGetValue("idempotencyKey", out var idempotencyKey))
        {
            request.IdempotencyKey = idempotencyKey switch
            {
                null => null,
                string text => text,
                _ => throw Error("Automation request 'idempotencyKey' must be a string or null.")
            };
        }

        return request;
    }

    private static object? Normalize(object? value, int depth, ref int nodes)
    {
        if (depth > MaximumDepth)
        {
            throw Error($"Automation request JSON exceeds the maximum depth of {MaximumDepth}.");
        }

        nodes++;
        if (nodes > MaximumNodes)
        {
            throw Error($"Automation request JSON exceeds the maximum node count of {MaximumNodes}.");
        }

        if (value is null || value is string || value is bool || IsFiniteNumber(value))
        {
            return value;
        }

        if (value is IDictionary dictionary)
        {
            var result = new Dictionary<string, object?>(StringComparer.Ordinal);
            foreach (DictionaryEntry entry in dictionary)
            {
                if (entry.Key is not string key)
                {
                    throw Error("Automation request object property names must be strings.");
                }

                result.Add(key, Normalize(entry.Value, depth + 1, ref nodes));
            }

            return result;
        }

        if (value is IEnumerable sequence)
        {
            var result = new List<object?>();
            foreach (var item in sequence)
            {
                result.Add(Normalize(item, depth + 1, ref nodes));
            }

            return result.ToArray();
        }

        throw Error($"Automation request JSON produced unsupported value type '{value.GetType().FullName}'.");
    }

    private static bool IsFiniteNumber(object value)
    {
        return value switch
        {
            sbyte or byte or short or ushort or int or uint or long or ulong or decimal => true,
            float single => !float.IsNaN(single) && !float.IsInfinity(single),
            double floating => !double.IsNaN(floating) && !double.IsInfinity(floating),
            _ => false
        };
    }

    private static void ValidateStructuralDepth(string json)
    {
        var depth = 0;
        var inString = false;
        var escaped = false;
        for (var index = 0; index < json.Length; index++)
        {
            var character = json[index];
            if (inString)
            {
                if (escaped)
                {
                    escaped = false;
                }
                else if (character == '\\')
                {
                    escaped = true;
                }
                else if (character == '"')
                {
                    inString = false;
                }

                continue;
            }

            if (character == '"')
            {
                inString = true;
            }
            else if (character is '{' or '[')
            {
                depth++;
                if (depth > MaximumDepth)
                {
                    throw Error($"Automation request JSON exceeds the maximum depth of {MaximumDepth}.");
                }
            }
            else if (character is '}' or ']')
            {
                depth--;
                if (depth < 0)
                {
                    throw Error("Automation request JSON contains an unmatched closing delimiter.");
                }
            }
        }
    }

    private static SerializationException Error(string message) => new(message);
}
