using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;

namespace RimWorldDevGateway.Contracts;

public static class GatewayContractJson
{
    public static string Write<T>(T value)
    {
        using var stream = new MemoryStream();
        CreateSerializer<T>().WriteObject(stream, value);
        return System.Text.Encoding.UTF8.GetString(stream.ToArray());
    }

    public static T Read<T>(string json)
    {
        using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json));
        var value = CreateSerializer<T>().ReadObject(stream);
        if (value is not T typed)
        {
            throw new SerializationException($"JSON did not contain a {typeof(T).FullName} value.");
        }

        return typed;
    }

    public static T ReadFile<T>(string path) => Read<T>(File.ReadAllText(path));

    private static DataContractJsonSerializer CreateSerializer<T>() =>
        new(typeof(T), new DataContractJsonSerializerSettings
        {
            UseSimpleDictionaryFormat = true
        });
}
