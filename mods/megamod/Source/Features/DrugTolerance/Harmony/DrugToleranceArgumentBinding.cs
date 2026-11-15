using System.Reflection;
using Megamod.Patching;

namespace Megamod.Features.DrugTolerance;

// Retain the S138 entrypoint while all consumers share the inspected provider binding policy.
internal static class DrugToleranceArgumentBinding
{
    internal static (string Name, int Index) Resolve(MethodInfo target, ParameterInfo parameter) =>
        HarmonyArgumentBinding.Resolve(target, parameter);
}
