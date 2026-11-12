using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Megamod.Features.DrugPolicyGeneration;

/// <summary>Partial current stage producers; current native inputs and dispatch remain separate premises.</summary>
public sealed class DrugGenerationStageBodyGuard
{
    public sealed record Bindings(MethodInfo SeverityGetter, FieldInfo Severity);
    private readonly Bindings binding;
    public DrugGenerationStageBodyGuard(Bindings binding) => this.binding = binding;
    internal MethodInfo[] Methods => new[] { binding.SeverityGetter };
    internal string? ValidateShape() => binding.SeverityGetter.IsStatic || !binding.SeverityGetter.IsVirtual ||
        binding.SeverityGetter.IsAbstract || binding.SeverityGetter.IsGenericMethod || binding.SeverityGetter.ContainsGenericParameters ||
        binding.SeverityGetter.GetParameters().Length != 0 || binding.SeverityGetter.ReturnType != typeof(float) ||
        binding.SeverityGetter.DeclaringType?.IsClass != true || binding.Severity.IsStatic || binding.Severity.FieldType != typeof(float) ||
        binding.Severity.DeclaringType?.IsAssignableFrom(binding.SeverityGetter.DeclaringType) != true
        ? "The bound severity getter/field shape changed." : null;

    public string? Validate(MethodInfo target, IReadOnlyList<CodeInstruction> body)
    {
        var shape = ValidateShape(); if (shape != null) return shape;
        if (!DrugGenerationDecayGuard.Same(target, binding.SeverityGetter)) return "This method is not the bound severity getter.";
        if (body.Count == 0 || body.Count > 256 || body.Any(instruction => instruction.blocks.Count != 0))
            return "The severity body is empty, unbounded or has exception regions.";
        var code = body.Where(instruction => instruction.opcode != OpCodes.Nop).ToArray();
        return code.Length == 3 && code[0].IsLdarg() && code[0].ArgumentIndex() == 0 &&
            code[1].opcode == OpCodes.Ldfld && DrugGenerationPawnBodyGuard.SameField(code[1].operand, binding.Severity) && code[2].opcode == OpCodes.Ret
            ? null : "The same-hediff current Single severity field return is unproved.";
    }
}
