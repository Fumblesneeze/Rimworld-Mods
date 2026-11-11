using System.Reflection;

namespace Megamod.Features.DrugTolerance.Core;

public static class DrugToleranceJoyProvider
{
    // ReflectedType changes when the inherited member is looked up through a worker subclass.
    // Preserve the actual declaration; GetBaseDefinition would incorrectly admit overrides.
    public static bool UsesNativeDecision(MethodInfo? resolved, MethodInfo native) => resolved != null &&
        !resolved.IsGenericMethod && !native.IsGenericMethod && resolved.Module == native.Module &&
        resolved.MetadataToken == native.MetadataToken && resolved.DeclaringType == native.DeclaringType;
}
