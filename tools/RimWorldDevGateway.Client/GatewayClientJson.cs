using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace RimWorldDevGateway.Client;

internal static class GatewayClientJson
{
    private const int MaximumJsonLength = 4 * 1024 * 1024;

    public static string Serialize(object value)
    {
        var serializer = CreateSerializer();
        return serializer.Serialize(value);
    }

    public static Dictionary<string, object?> ParseObject(string json, string description)
    {
        try
        {
            var parsed = CreateSerializer().DeserializeObject(json);
            if (parsed is not Dictionary<string, object> dictionary)
            {
                throw new GatewayCliUsageException($"{description} must be a JSON object.");
            }

            return dictionary.ToDictionary(
                pair => pair.Key,
                pair => (object?)pair.Value,
                StringComparer.Ordinal);
        }
        catch (GatewayCliUsageException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new GatewayCliUsageException($"{description} is not valid JSON: {Bound(exception.Message, 512)}");
        }
    }

    public static void WriteTable(TextWriter output, object value)
    {
        output.WriteLine("FIELD\tVALUE");
        if (value is IDictionary<string, object?> nullableDictionary)
        {
            foreach (var pair in nullableDictionary.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                output.WriteLine(pair.Key + "\t" + FormatValue(pair.Value));
            }

            return;
        }

        if (value is IDictionary dictionary)
        {
            var rows = new List<KeyValuePair<string, object?>>();
            foreach (DictionaryEntry entry in dictionary)
            {
                rows.Add(new KeyValuePair<string, object?>(Convert.ToString(entry.Key, CultureInfo.InvariantCulture) ?? string.Empty, entry.Value));
            }

            foreach (var row in rows.OrderBy(pair => pair.Key, StringComparer.Ordinal))
            {
                output.WriteLine(row.Key + "\t" + FormatValue(row.Value));
            }

            return;
        }

        output.WriteLine("result\t" + FormatValue(value));
    }

    public static object ParseValue(string json)
    {
        try
        {
            return CreateSerializer().DeserializeObject(json) ?? new Dictionary<string, object>();
        }
        catch (Exception exception)
        {
            throw new GatewayClientException("Gateway returned invalid JSON: " + Bound(exception.Message, 512), exception);
        }
    }

    private static JavaScriptSerializer CreateSerializer() => new() { MaxJsonLength = MaximumJsonLength, RecursionLimit = 64 };

    private static string FormatValue(object? value)
    {
        if (value is null)
        {
            return string.Empty;
        }

        if (value is string text)
        {
            return text.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t");
        }

        if (value is bool boolean)
        {
            return boolean ? "true" : "false";
        }

        if (value is IDictionary || value is IEnumerable and not string)
        {
            return Serialize(value);
        }

        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    private static string Bound(string value, int maximumLength) =>
        value.Length <= maximumLength ? value : value.Substring(0, maximumLength) + "...";
}
