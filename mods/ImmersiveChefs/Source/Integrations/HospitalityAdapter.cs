using System.Reflection;
using HarmonyLib;
using Verse;

namespace ImmersiveChefs;

internal static class HospitalityAdapter
{
    private const string GuestUtilityTypeName = "Hospitality.Utilities.GuestUtility";
    private const string GuestCompTypeName = "Hospitality.CompGuest";
    private static Func<Pawn, bool>? isArrivedGuest;

    internal static bool TryInitialize(out string reason)
    {
        if (!TryBind(AccessTools.TypeByName(GuestUtilityTypeName), out var predicate, out reason))
        {
            return false;
        }

        isArrivedGuest = predicate;
        Log.Message("[ImmersiveChefs] Hospitality adapter active; arrived guests may use personal cutlery as fallback.");
        return true;
    }

    internal static bool IsArrivedGuest(Pawn pawn)
    {
        var predicate = isArrivedGuest;
        if (predicate is null)
        {
            return false;
        }

        try
        {
            return predicate(pawn);
        }
        catch (Exception exception)
        {
            isArrivedGuest = null;
            OptionalIntegrationDiagnostics.WarnOnce(
                OptionalIntegration.Hospitality,
                $"arrived-guest invocation failed ({exception.GetType().Name}: {exception.Message})");
            return false;
        }
    }

    internal static bool TryBind(
        Type? guestUtilityType,
        out Func<Pawn, bool>? predicate,
        out string reason)
    {
        predicate = null;
        reason = string.Empty;
        if (guestUtilityType?.FullName != GuestUtilityTypeName)
        {
            reason = $"missing public {GuestUtilityTypeName} type";
            return false;
        }

        var method = guestUtilityType
            .GetMethods(BindingFlags.Public | BindingFlags.Static)
            .SingleOrDefault(candidate => IsSupportedMethod(candidate));
        return TryBindMethod(method, out predicate, out reason);
    }

    internal static bool TryBindMethod(
        MethodInfo? method,
        out Func<Pawn, bool>? predicate,
        out string reason)
    {
        predicate = null;
        reason = string.Empty;
        if (method is null || !IsSupportedMethod(method))
        {
            reason =
                $"missing public static bool {GuestUtilityTypeName}.IsArrivedGuest" +
                $"(Verse.Pawn, out {GuestCompTypeName})";
            return false;
        }

        predicate = pawn =>
        {
            var arguments = new object?[] { pawn, null };
            return method.Invoke(null, arguments) is true;
        };
        return true;
    }

    private static bool IsSupportedMethod(MethodInfo method)
    {
        if (method.Name != "IsArrivedGuest" || method.ReturnType != typeof(bool) ||
            method.IsGenericMethodDefinition)
        {
            return false;
        }

        var parameters = method.GetParameters();
        return parameters.Length == 2 &&
               parameters[0].ParameterType == typeof(Pawn) &&
               parameters[1].IsOut &&
               parameters[1].ParameterType.IsByRef &&
               parameters[1].ParameterType.GetElementType()?.FullName == GuestCompTypeName;
    }
}
