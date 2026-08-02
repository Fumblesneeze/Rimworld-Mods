using System.Reflection;

namespace RimWorldDevGateway;

/// <summary>
/// Produces diagnostic markers without invoking virtual text members supplied by mod objects.
/// Only the CLR/Mono runtime's concrete Type implementation is trusted for reflection metadata.
/// </summary>
internal static class GatewayDefSafeText
{
    public const string UnsupportedTypeName = "<unsupported-type>";

    private static readonly Type RuntimeTypeImplementation = typeof(string).GetType();

    public static bool IsRuntimeType(Type? type) =>
        type is not null && ReferenceEquals(type.GetType(), RuntimeTypeImplementation);

    public static string TypeName(Type? type)
    {
        if (!IsRuntimeType(type))
        {
            return UnsupportedTypeName;
        }

        try
        {
            return type!.FullName ?? type.Name;
        }
        catch (Exception)
        {
            return UnsupportedTypeName;
        }
    }

    public static bool HasAttribute(FieldInfo field, string attributeTypeName)
    {
        try
        {
            return CustomAttributeData.GetCustomAttributes(field).Any(attribute =>
                string.Equals(TypeName(attribute.AttributeType), attributeTypeName, StringComparison.Ordinal));
        }
        catch (Exception)
        {
            return true;
        }
    }

    public static string ExceptionMarker(Exception exception)
    {
        var typeName = TypeName(exception.GetType());
        return typeName + ": diagnostic message suppressed";
    }
}
