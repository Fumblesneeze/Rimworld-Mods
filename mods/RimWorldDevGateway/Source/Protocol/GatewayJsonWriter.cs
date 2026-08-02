using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace RimWorldDevGateway;

public sealed class GatewayJsonSerializationException : Exception
{
    public GatewayJsonSerializationException(string message)
        : base(message)
    {
    }

    public GatewayJsonSerializationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

internal readonly struct GatewayJsonMeasurement
{
    public GatewayJsonMeasurement(int utf8Bytes, int nodes)
    {
        Utf8Bytes = utf8Bytes;
        Nodes = nodes;
    }

    public int Utf8Bytes { get; }

    public int Nodes { get; }
}

/// <summary>
/// Writes the deliberately small JSON subset used by the local development gateway.
/// The serializer has no dependency on a game- or framework-provided JSON package.
/// </summary>
public static class GatewayJsonWriter
{
    public static byte[] Write(
        object? value,
        int maxDepth = 16,
        int maxNodes = 100_000,
        int maxUtf8Bytes = 4 * 1024 * 1024)
    {
        var output = Serialize(value, maxDepth, maxNodes, maxUtf8Bytes, out _);
        return output.ToUtf8Bytes();
    }

    internal static GatewayJsonMeasurement Measure(
        object? value,
        int maxDepth = 16,
        int maxNodes = 100_000,
        int maxUtf8Bytes = 4 * 1024 * 1024)
    {
        var output = Serialize(value, maxDepth, maxNodes, maxUtf8Bytes, out var nodes);
        return new GatewayJsonMeasurement(output.Utf8Bytes, nodes);
    }

    private static BoundedOutput Serialize(
        object? value,
        int maxDepth,
        int maxNodes,
        int maxUtf8Bytes,
        out int nodes)
    {
        if (maxDepth < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxDepth));
        }

        if (maxNodes < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxNodes));
        }

        if (maxUtf8Bytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxUtf8Bytes));
        }

        var output = new BoundedOutput(maxUtf8Bytes);
        var context = new WriteContext(output, maxDepth, maxNodes);
        context.WriteValue(value, 0);
        nodes = context.Nodes;
        return output;
    }

    private sealed class WriteContext
    {
        private readonly BoundedOutput _builder;
        private readonly int _maxDepth;
        private readonly int _maxNodes;
        private readonly HashSet<object> _ancestors = new HashSet<object>(ReferenceComparer.Instance);
        private int _nodes;

        public WriteContext(BoundedOutput builder, int maxDepth, int maxNodes)
        {
            _builder = builder;
            _maxDepth = maxDepth;
            _maxNodes = maxNodes;
        }

        public int Nodes => _nodes;

        public void WriteValue(object? value, int depth)
        {
            CountNode(depth);
            if (value is null)
            {
                _builder.Append("null");
                return;
            }

            if (TryWriteScalar(value))
            {
                return;
            }

            var trackReference = !value.GetType().IsValueType;
            if (trackReference && !_ancestors.Add(value))
            {
                throw new GatewayJsonSerializationException("A reference cycle was found while writing JSON.");
            }

            try
            {
                if (value is IDictionary dictionary)
                {
                    WriteDictionary(dictionary, depth);
                    return;
                }

                if (value is IEnumerable enumerable)
                {
                    WriteEnumerable(enumerable, depth);
                    return;
                }

                WriteObject(value, depth);
            }
            finally
            {
                if (trackReference)
                {
                    _ancestors.Remove(value);
                }
            }
        }

        private void CountNode(int depth)
        {
            if (depth > _maxDepth)
            {
                throw new GatewayJsonSerializationException(
                    "The JSON value exceeds the configured maximum depth of " +
                    _maxDepth.ToString(CultureInfo.InvariantCulture) + ".");
            }

            _nodes++;
            if (_nodes > _maxNodes)
            {
                throw new GatewayJsonSerializationException(
                    "The JSON value exceeds the configured maximum node count of " +
                    _maxNodes.ToString(CultureInfo.InvariantCulture) + ".");
            }
        }

        private bool TryWriteScalar(object value)
        {
            switch (value)
            {
                case string text:
                    WriteString(text);
                    return true;
                case char character:
                    WriteString(character.ToString());
                    return true;
                case Guid guid:
                    WriteString(guid.ToString("D", CultureInfo.InvariantCulture));
                    return true;
                case DateTime dateTime:
                    WriteString(dateTime.ToString("O", CultureInfo.InvariantCulture));
                    return true;
                case DateTimeOffset dateTimeOffset:
                    WriteString(dateTimeOffset.ToString("O", CultureInfo.InvariantCulture));
                    return true;
                case bool boolean:
                    _builder.Append(boolean ? "true" : "false");
                    return true;
                case sbyte signedByte:
                    _builder.Append(signedByte.ToString(CultureInfo.InvariantCulture));
                    return true;
                case byte unsignedByte:
                    _builder.Append(unsignedByte.ToString(CultureInfo.InvariantCulture));
                    return true;
                case short signedShort:
                    _builder.Append(signedShort.ToString(CultureInfo.InvariantCulture));
                    return true;
                case ushort unsignedShort:
                    _builder.Append(unsignedShort.ToString(CultureInfo.InvariantCulture));
                    return true;
                case int signedInteger:
                    _builder.Append(signedInteger.ToString(CultureInfo.InvariantCulture));
                    return true;
                case uint unsignedInteger:
                    _builder.Append(unsignedInteger.ToString(CultureInfo.InvariantCulture));
                    return true;
                case long signedLong:
                    _builder.Append(signedLong.ToString(CultureInfo.InvariantCulture));
                    return true;
                case ulong unsignedLong:
                    _builder.Append(unsignedLong.ToString(CultureInfo.InvariantCulture));
                    return true;
                case float single:
                    WriteSingle(single);
                    return true;
                case double floatingPoint:
                    WriteDouble(floatingPoint);
                    return true;
                case decimal decimalValue:
                    _builder.Append(decimalValue.ToString(CultureInfo.InvariantCulture));
                    return true;
                case IntPtr pointer:
                    _builder.Append(pointer.ToInt64().ToString(CultureInfo.InvariantCulture));
                    return true;
                case UIntPtr unsignedPointer:
                    _builder.Append(unsignedPointer.ToUInt64().ToString(CultureInfo.InvariantCulture));
                    return true;
                case Enum enumValue:
                    WriteString(enumValue.ToString());
                    return true;
                default:
                    return false;
            }
        }

        private void WriteSingle(float value)
        {
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new GatewayJsonSerializationException("JSON cannot represent a non-finite number.");
            }

            _builder.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        private void WriteDouble(double value)
        {
            if (double.IsNaN(value) || double.IsInfinity(value))
            {
                throw new GatewayJsonSerializationException("JSON cannot represent a non-finite number.");
            }

            _builder.Append(value.ToString("R", CultureInfo.InvariantCulture));
        }

        private void WriteDictionary(IDictionary dictionary, int depth)
        {
            var entries = ReadDictionaryEntries(dictionary, _maxNodes - _nodes);
            entries.Sort((left, right) => StringComparer.Ordinal.Compare(left.Key, right.Key));

            _builder.Append('{');
            for (var index = 0; index < entries.Count; index++)
            {
                if (index > 0)
                {
                    _builder.Append(',');
                }

                WriteString(entries[index].Key);
                _builder.Append(':');
                WriteValue(entries[index].Value, depth + 1);
            }

            _builder.Append('}');
        }

        private List<DictionaryValue> ReadDictionaryEntries(IDictionary dictionary, int maximumEntries)
        {
            var entries = new List<DictionaryValue>();
            var keys = new HashSet<string>(StringComparer.Ordinal);
            try
            {
                var enumerator = dictionary.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    if (entries.Count >= maximumEntries)
                    {
                        throw new GatewayJsonSerializationException(
                            "The JSON value exceeds the configured maximum node count of " +
                            _maxNodes.ToString(CultureInfo.InvariantCulture) + ".");
                    }

                    var key = ConvertDictionaryKey(enumerator.Key);
                    if (!keys.Add(key))
                    {
                        throw new GatewayJsonSerializationException(
                            "Multiple dictionary keys convert to the JSON property name '" + key + "'.");
                    }

                    entries.Add(new DictionaryValue(key, enumerator.Value));
                }
            }
            catch (GatewayJsonSerializationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new GatewayJsonSerializationException("A dictionary could not be read while writing JSON.", exception);
            }

            return entries;
        }

        private static string ConvertDictionaryKey(object? key)
        {
            if (key is null)
            {
                return "null";
            }

            try
            {
                switch (key)
                {
                    case DateTime dateTime:
                        return dateTime.ToString("O", CultureInfo.InvariantCulture);
                    case DateTimeOffset dateTimeOffset:
                        return dateTimeOffset.ToString("O", CultureInfo.InvariantCulture);
                    case Guid guid:
                        return guid.ToString("D", CultureInfo.InvariantCulture);
                    case IFormattable formattable:
                        return formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty;
                    default:
                        return key.ToString() ?? string.Empty;
                }
            }
            catch (Exception exception)
            {
                throw new GatewayJsonSerializationException("A dictionary key could not be converted to a string.", exception);
            }
        }

        private void WriteEnumerable(IEnumerable enumerable, int depth)
        {
            _builder.Append('[');
            var first = true;
            try
            {
                foreach (var item in enumerable)
                {
                    if (!first)
                    {
                        _builder.Append(',');
                    }

                    first = false;
                    WriteValue(item, depth + 1);
                }
            }
            catch (GatewayJsonSerializationException)
            {
                throw;
            }
            catch (Exception exception)
            {
                throw new GatewayJsonSerializationException("A sequence could not be read while writing JSON.", exception);
            }

            _builder.Append(']');
        }

        private void WriteObject(object value, int depth)
        {
            PropertyInfo[] properties;
            try
            {
                properties = value.GetType()
                    .GetProperties(BindingFlags.Instance | BindingFlags.Public)
                    .Where(property => property.CanRead && property.GetMethod is not null && property.GetMethod.IsPublic)
                    .OrderBy(property => property.Name, StringComparer.Ordinal)
                    .ToArray();
            }
            catch (Exception exception)
            {
                throw new GatewayJsonSerializationException("Public properties could not be inspected while writing JSON.", exception);
            }

            for (var index = 0; index < properties.Length; index++)
            {
                if (properties[index].GetIndexParameters().Length != 0)
                {
                    throw new GatewayJsonSerializationException(
                        "Indexed property '" + properties[index].Name + "' cannot be written as JSON.");
                }

                if (index > 0 && string.Equals(properties[index - 1].Name, properties[index].Name, StringComparison.Ordinal))
                {
                    throw new GatewayJsonSerializationException(
                        "Multiple public properties have the JSON name '" + properties[index].Name + "'.");
                }
            }

            _builder.Append('{');
            for (var index = 0; index < properties.Length; index++)
            {
                if (index > 0)
                {
                    _builder.Append(',');
                }

                var property = properties[index];
                WriteString(property.Name);
                _builder.Append(':');
                WriteValue(ReadProperty(value, property), depth + 1);
            }

            _builder.Append('}');
        }

        private static object? ReadProperty(object owner, PropertyInfo property)
        {
            try
            {
                return property.GetValue(owner, null);
            }
            catch (TargetInvocationException exception)
            {
                throw new GatewayJsonSerializationException(
                    "Property '" + property.Name + "' threw while its JSON value was read.",
                    exception.InnerException ?? exception);
            }
            catch (Exception exception)
            {
                throw new GatewayJsonSerializationException(
                    "Property '" + property.Name + "' could not be read while writing JSON.",
                    exception);
            }
        }

        private void WriteString(string value)
        {
            _builder.Append('"');
            foreach (var character in value)
            {
                switch (character)
                {
                    case '"':
                        _builder.Append("\\\"");
                        break;
                    case '\\':
                        _builder.Append("\\\\");
                        break;
                    case '\b':
                        _builder.Append("\\b");
                        break;
                    case '\f':
                        _builder.Append("\\f");
                        break;
                    case '\n':
                        _builder.Append("\\n");
                        break;
                    case '\r':
                        _builder.Append("\\r");
                        break;
                    case '\t':
                        _builder.Append("\\t");
                        break;
                    default:
                        if (character < 0x20 || char.IsSurrogate(character))
                        {
                            _builder.Append("\\u");
                            _builder.Append(((int)character).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            _builder.Append(character);
                        }

                        break;
                }
            }

            _builder.Append('"');
        }
    }

    private sealed class BoundedOutput
    {
        private readonly StringBuilder builder = new();
        private readonly int maximumUtf8Bytes;
        private int utf8Bytes;

        public BoundedOutput(int maximumUtf8Bytes)
        {
            this.maximumUtf8Bytes = maximumUtf8Bytes;
        }

        public void Append(char value)
        {
            var addedBytes = value <= 0x7f ? 1 : value <= 0x7ff ? 2 : 3;
            Reserve(addedBytes);
            builder.Append(value);
        }

        public void Append(string value)
        {
            if (value is null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            Reserve(Encoding.UTF8.GetByteCount(value));
            builder.Append(value);
        }

        public byte[] ToUtf8Bytes() => Encoding.UTF8.GetBytes(builder.ToString());

        public int Utf8Bytes => utf8Bytes;

        private void Reserve(int addedBytes)
        {
            if (addedBytes > maximumUtf8Bytes - utf8Bytes)
            {
                throw new GatewayJsonSerializationException(
                    "The JSON value exceeds the configured UTF-8 byte limit of " +
                    maximumUtf8Bytes.ToString(CultureInfo.InvariantCulture) + ".");
            }

            utf8Bytes += addedBytes;
        }
    }

    private sealed class DictionaryValue
    {
        public DictionaryValue(string key, object? value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; }

        public object? Value { get; }
    }

    private sealed class ReferenceComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceComparer Instance = new ReferenceComparer();

        bool IEqualityComparer<object>.Equals(object? left, object? right)
        {
            return ReferenceEquals(left, right);
        }

        int IEqualityComparer<object>.GetHashCode(object value)
        {
            return RuntimeHelpers.GetHashCode(value);
        }
    }
}
