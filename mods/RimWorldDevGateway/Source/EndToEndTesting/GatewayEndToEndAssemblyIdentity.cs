using System.Reflection;
using System.Text;

namespace RimWorldDevGateway;

public static class GatewayEndToEndAssemblyIdentity
{
    public static string FormatDefinition(AssemblyName assemblyName)
    {
        if (assemblyName is null)
        {
            throw new ArgumentNullException(nameof(assemblyName));
        }

        return Format(assemblyName, "PublicKey", assemblyName.GetPublicKey());
    }

    public static string FormatReference(AssemblyName assemblyName)
    {
        if (assemblyName is null)
        {
            throw new ArgumentNullException(nameof(assemblyName));
        }

        return Format(assemblyName, "PublicKeyToken", assemblyName.GetPublicKeyToken());
    }

    public static string FormatKey(byte[]? key)
    {
        if (key is null || key.Length == 0)
        {
            return "null";
        }

        var output = new StringBuilder(key.Length * 2);
        foreach (var value in key)
        {
            output.Append(value.ToString("x2", System.Globalization.CultureInfo.InvariantCulture));
        }

        return output.ToString();
    }

    private static string Format(AssemblyName assemblyName, string keyKind, byte[]? key)
    {
        var name = assemblyName.Name ?? string.Empty;
        var version = assemblyName.Version?.ToString() ?? "0.0.0.0";
        var culture = string.IsNullOrWhiteSpace(assemblyName.CultureName)
            ? "neutral"
            : assemblyName.CultureName;
        return name + ", Version=" + version + ", Culture=" + culture + ", " +
               keyKind + "=" + FormatKey(key);
    }
}
