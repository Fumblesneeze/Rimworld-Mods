using System.Globalization;
using System.Reflection;

namespace RimWorldDevGateway;

public static class GatewayMethodIdentity
{
    public static string? TryHandle(MethodBase method)
    {
        try { return Handle(method); }
        catch (Exception exception) when (exception is ArgumentException or NotSupportedException or InvalidOperationException)
        { return null; }
    }

    public static string Handle(MethodBase method)
    {
        if (method.IsGenericMethod || method.DeclaringType?.IsGenericType == true)
            throw new ArgumentException("Generic method/type contexts are not supported by exact diagnostic handles.");
        return method.Module.ModuleVersionId.ToString("N") + ":" + method.MetadataToken.ToString("X8", CultureInfo.InvariantCulture);
    }

    public static string Signature(MethodBase method) =>
        (method is MethodInfo info ? info.ReturnType.FullName + " " : "") +
        method.DeclaringType?.FullName + "." + method.Name + "(" +
        string.Join(", ", method.GetParameters().Select(parameter => parameter.ParameterType.ToString())) + ")";

    public static MethodBase Resolve(string handle)
    {
        var parts = (handle ?? string.Empty).Split(':');
        if (parts.Length != 2 || !Guid.TryParseExact(parts[0], "N", out var mvid) ||
            !int.TryParse(parts[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var token))
            throw new ArgumentException("Invalid exact method handle.");
        var modules = AppDomain.CurrentDomain.GetAssemblies().Where(assembly => !assembly.IsDynamic)
            .SelectMany(assembly => assembly.GetModules()).Where(module => module.ModuleVersionId == mvid).Take(2).ToArray();
        if (modules.Length != 1) throw new ArgumentException("Method module is stale or ambiguous in this process.");
        try
        {
            var method = modules[0].ResolveMethod(token) ?? throw new ArgumentException("Method token did not resolve.");
            _ = Handle(method);
            return method;
        }
        catch (Exception exception) when (exception is ArgumentException or BadImageFormatException)
        { throw new ArgumentException("Method token is invalid for the exact loaded module.", exception); }
    }
}
