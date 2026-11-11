using System.Reflection;
using System.Reflection.Emit;
using System.Linq;
using HarmonyLib;
using Megamod.Patching;
using HarmonyInstance = HarmonyLib.Harmony;

namespace Megamod.Features.ContentSelection.Harmony;

public static class IncidentPatchOrder
{
    public static bool HasFactory(MethodBase target)
    {
        var metadata = HarmonyInstance.GetPatchInfo(target);
        return metadata != null && metadata.Prefixes.Concat(metadata.Postfixes).Concat(metadata.Transpilers)
            .Concat(metadata.Finalizers).Any(IsFactory);
    }

    public static string? ValidatePostfix(MethodBase target, MethodInfo ownedPostfix)
    {
        var metadata = HarmonyInstance.GetPatchInfo(target);
        if (metadata?.Finalizers.Any(patch => IsFactory(patch) || patch.PatchMethod.GetParameters().Any(parameter =>
            parameter.Name == "__result" && parameter.ParameterType.IsByRef)) == true)
            return "A finalizer may replace the owned incident result veto.";
        return Validate(target, ownedPostfix, metadata?.Postfixes.ToArray() ?? System.Array.Empty<Patch>(), false);
    }

    public static string? ValidatePrefix(MethodBase target, MethodInfo ownedPrefix) => Validate(target, ownedPrefix,
        HarmonyInstance.GetPatchInfo(target)?.Prefixes.ToArray() ?? System.Array.Empty<Patch>(), true);

    private static string? Validate(MethodBase target, MethodInfo callback, Patch[] patches, bool prefix)
    {
        if (patches.Any(IsFactory))
            return "Incident patch factories cannot be inspected without executing foreign code.";
        if (patches.Count(patch => patch.owner == OwnedHarmonyPatch.OwnerId) != 1 ||
            patches.Count(patch => patch.PatchMethod == callback) != 1 ||
            !patches.Any(patch => patch.owner == OwnedHarmonyPatch.OwnerId && patch.PatchMethod == callback))
            return "The owned incident decision is absent or duplicated.";
        // Harmony emits all void postfixes before its non-void pass-through chain, independently
        // of where those pass-through methods occur in the metadata sort. No such result writer
        // has an established false-preservation contract for this owned void veto.
        if (!prefix && patches.Any(patch => patch.PatchMethod.ReturnType != typeof(void)))
            return "A pass-through postfix may replace the owned incident result veto after all void postfixes.";
        var sorted = PatchProcessor.GetSortedPatchMethods(target, patches);
        foreach (var later in sorted.Skip(sorted.IndexOf(callback) + 1))
        {
            if (prefix && MayChangeIdentity(target, later))
                return "A later prefix may change the incident worker identity after the owned decision.";
            if (!prefix && later.GetParameters().Any(parameter =>
                parameter.Name == "__result" && parameter.ParameterType.IsByRef))
                return "A later postfix may replace the owned incident result veto.";
        }
        return null;
    }

    private static bool MayChangeIdentity(MethodBase target, MethodInfo prefix)
    {
        var parameters = prefix.GetParameters();
        if (parameters.Any(parameter => parameter.ParameterType.IsByRef &&
            (parameter.Name == "__instance" || parameter.Name == "___def"))) return true;
        if (!parameters.Any(parameter => parameter.Name == "__instance")) return false;
        if (prefix.GetMethodBody() == null) return true;
        // Read actual IL only; do not execute the observer or any foreign transpiler/factory.
        return PatchProcessor.GetOriginalInstructions(prefix).Any(instruction =>
            instruction.opcode.FlowControl == FlowControl.Call || instruction.opcode == OpCodes.Stobj ||
            instruction.opcode.Name!.StartsWith("stind", System.StringComparison.Ordinal) ||
            ((instruction.opcode == OpCodes.Stfld || instruction.opcode == OpCodes.Ldflda) &&
             instruction.operand is FieldInfo field && field.Name == "def" &&
             field.DeclaringType!.IsAssignableFrom(target.DeclaringType!)));
    }

    private static bool IsFactory(Patch patch)
    {
        MethodInfo method = patch.PatchMethod;
        return method.IsStatic && (method.ReturnType == typeof(MethodInfo) || method.ReturnType == typeof(DynamicMethod)) &&
            method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(new[] { typeof(MethodBase) });
    }
}
