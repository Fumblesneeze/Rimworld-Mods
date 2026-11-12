using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Megamod.Features.DrugPolicyGeneration;

/// <summary>
/// Conditional age/Core-flag producers and ordered rate consumer. Current nonnull native
/// identities, initialized types, a false flag and admitted Pawn/Props/CurStage bodies are
/// separate premises. This does not prove stage membership, permanent state or native call edges.
/// </summary>
public sealed class DrugGenerationRateBodyGuard
{
    public sealed record Bindings(MethodInfo AgeYearsGetter, FieldInfo BiologicalTicks,
        MethodInfo BiotechGetter, FieldInfo BiotechActive, MethodInfo Rate, MethodInfo PawnGetter,
        MethodInfo PropertiesGetter, MethodInfo StageGetter, FieldInfo AgeTracker, FieldInfo StoredRate,
        FieldInfo Parent, FieldInfo MinimumAge, FieldInfo StageFactor);
    private readonly Bindings binding;
    public DrugGenerationRateBodyGuard(Bindings binding) => this.binding = binding;
    internal MethodInfo[] Methods => new[] { binding.AgeYearsGetter, binding.BiotechGetter, binding.Rate };
    internal string? ValidateShape()
    {
        var methods = new[] { binding.AgeYearsGetter, binding.BiotechGetter, binding.Rate, binding.PawnGetter, binding.PropertiesGetter, binding.StageGetter };
        bool Instance(MethodInfo method, Type result) => !method.IsStatic && method.ReturnType == result && method.DeclaringType?.IsClass == true;
        bool Field(FieldInfo field, Type type, Type? receiver) => !field.IsStatic && field.FieldType == type && field.DeclaringType?.IsAssignableFrom(receiver) == true;
        if (methods.Any(method => method.IsGenericMethod || method.ContainsGenericParameters || method.IsAbstract || method.GetParameters().Length != 0) ||
            methods.Where((method, index) => methods.Take(index).Any(previous => DrugGenerationDecayGuard.Same(previous, method))).Any() ||
            binding.AgeYearsGetter.IsStatic || binding.AgeYearsGetter.IsVirtual || binding.AgeYearsGetter.ReturnType != typeof(float) ||
            binding.AgeYearsGetter.DeclaringType?.IsClass != true || binding.BiologicalTicks.IsStatic || binding.BiologicalTicks.FieldType != typeof(long) ||
            binding.BiologicalTicks.DeclaringType?.IsAssignableFrom(binding.AgeYearsGetter.DeclaringType) != true ||
            !binding.BiotechGetter.IsStatic || binding.BiotechGetter.ReturnType != typeof(bool) || !binding.BiotechActive.IsStatic ||
            binding.BiotechActive.FieldType != typeof(bool) || binding.BiotechActive.DeclaringType != binding.BiotechGetter.DeclaringType)
            return "The bound age/Core-flag callable or field shape changed.";
        // Existing callbacks can write only own scalar statics on no-cctor types. This
        // metadata premise protects this particular consumed flag; it does not run its cctor.
        if (binding.BiotechActive.DeclaringType?.TypeInitializer == null)
            return "The consumed Core flag is not protected by the current callback declaring-type rule.";
        if (!Instance(binding.Rate, typeof(float)) || !binding.Rate.IsVirtual ||
            !Instance(binding.PawnGetter, binding.AgeTracker.DeclaringType!) || binding.PawnGetter.IsVirtual ||
            binding.PawnGetter.DeclaringType?.IsAssignableFrom(binding.Rate.DeclaringType) != true ||
            !Instance(binding.PropertiesGetter, binding.MinimumAge.DeclaringType!) || binding.PropertiesGetter.IsVirtual ||
            binding.PropertiesGetter.DeclaringType != binding.Rate.DeclaringType ||
            !Instance(binding.StageGetter, binding.StageFactor.DeclaringType!) || !binding.StageGetter.IsVirtual ||
            binding.StageGetter.DeclaringType?.IsAssignableFrom(binding.Parent.FieldType) != true ||
            !Field(binding.AgeTracker, binding.AgeYearsGetter.DeclaringType!, binding.PawnGetter.ReturnType) ||
            !Field(binding.StoredRate, typeof(float), binding.Rate.DeclaringType) ||
            binding.Parent.IsStatic || !binding.Parent.FieldType.IsClass || binding.Parent.DeclaringType?.IsAssignableFrom(binding.Rate.DeclaringType) != true ||
            !Field(binding.MinimumAge, typeof(float), binding.PropertiesGetter.ReturnType) ||
            !Field(binding.StageFactor, typeof(float), binding.StageGetter.ReturnType))
            return "The bound rate/pawn/tracker/properties/parent/stage callable or field shape changed.";
        return null;
    }

    public string? Validate(MethodInfo target, IReadOnlyList<CodeInstruction> body)
    {
        var failure = ValidateShape(); if (failure != null) return failure;
        if (body.Count == 0 || body.Count > 256 || body.Any(instruction => instruction.blocks.Count != 0))
            return "The age/Core-flag projection is empty, unbounded or has exception regions.";
        var code = body.Where(instruction => instruction.opcode != OpCodes.Nop).ToArray();
        if (DrugGenerationDecayGuard.Same(target, binding.AgeYearsGetter))
            return code.Length == 6 && code[0].IsLdarg() && code[0].ArgumentIndex() == 0 &&
                code[1].opcode == OpCodes.Ldfld && DrugGenerationPawnBodyGuard.SameField(code[1].operand, binding.BiologicalTicks) &&
                code[2].opcode == OpCodes.Conv_R4 && code[3].opcode == OpCodes.Ldc_R4 && Equals(code[3].operand, 3600000f) &&
                code[4].opcode == OpCodes.Div && code[5].opcode == OpCodes.Ret
                ? null : "The current same-tracker Int64-to-Single age projection is unproved.";
        if (DrugGenerationDecayGuard.Same(target, binding.BiotechGetter))
            return code.Length == 2 && code[0].opcode == OpCodes.Ldsfld &&
                DrugGenerationPawnBodyGuard.SameField(code[0].operand, binding.BiotechActive) && code[1].opcode == OpCodes.Ret
                ? null : "The current bound Core-flag field return is unproved.";
        if (DrugGenerationDecayGuard.Same(target, binding.Rate))
        {
            bool Arg(int index) => code[index].IsLdarg() && code[index].ArgumentIndex() == 0;
            bool Field(int index, FieldInfo field) => code[index].opcode == OpCodes.Ldfld && DrugGenerationPawnBodyGuard.SameField(code[index].operand, field);
            bool Call(int index, OpCode operation, MethodInfo method) => code[index].opcode == operation && DrugGenerationDecayGuard.Same(code[index].operand, method);
            int Destination(int index)
            {
                if (code[index].operand is not Label label || body.Count(i => i.labels.Contains(label)) != 1) return -1;
                var destination = body.SkipWhile(i => !i.labels.Contains(label)).FirstOrDefault(i => i.opcode != OpCodes.Nop);
                return destination == null ? -1 : Array.IndexOf(code, destination);
            }
            bool Branch(int index, OpCode wide, OpCode shortForm, int destination) =>
                (code[index].opcode == wide || code[index].opcode == shortForm) && Destination(index) == destination;
            Type? LocalType(CodeInstruction instruction)
            {
                if (instruction.operand is LocalBuilder local) return local.LocalType;
                var declared = target.GetMethodBody()?.LocalVariables; int index = instruction.LocalIndex();
                return declared != null && index >= 0 && index < declared.Count ? declared[index].LocalType : null;
            }
            // Exact consumed paths only: age then minimum; rate before the virtual stage call;
            // nullable factor; Single store; false Core flag skips the unproved Biotech tail.
            // All selected inner callbacks remain conservatively checked by the bundle.
            return code.Length >= 27 && Arg(0) && Call(1, OpCodes.Call, binding.PawnGetter) && Field(2, binding.AgeTracker) &&
                Call(3, OpCodes.Callvirt, binding.AgeYearsGetter) && Arg(4) && Call(5, OpCodes.Call, binding.PropertiesGetter) && Field(6, binding.MinimumAge) &&
                Branch(7, OpCodes.Bge_Un, OpCodes.Bge_Un_S, 10) && code[8].opcode == OpCodes.Ldc_R4 && Equals(code[8].operand, 0f) && code[9].opcode == OpCodes.Ret &&
                Arg(10) && Field(11, binding.StoredRate) && Arg(12) && Field(13, binding.Parent) && Call(14, OpCodes.Callvirt, binding.StageGetter) &&
                code[15].opcode == OpCodes.Dup && Branch(16, OpCodes.Brtrue, OpCodes.Brtrue_S, 20) && code[17].opcode == OpCodes.Pop &&
                code[18].opcode == OpCodes.Ldc_R4 && Equals(code[18].operand, 1f) && Branch(19, OpCodes.Br, OpCodes.Br_S, 21) && Field(20, binding.StageFactor) &&
                code[21].opcode == OpCodes.Mul && code[22].IsStloc() && LocalType(code[22]) == typeof(float) && Call(23, OpCodes.Call, binding.BiotechGetter) &&
                Branch(24, OpCodes.Brfalse, OpCodes.Brfalse_S, code.Length - 2) && code[^2].IsLdloc() &&
                code[^2].LocalIndex() == code[22].LocalIndex() && LocalType(code[^2]) == typeof(float) && code[^1].opcode == OpCodes.Ret
                ? null : "The ordered same-comp age/stage/Single-store/Core-false rate path is unproved.";
        }
        return "This method is not a bound age/Core-flag/rate body.";
    }
}
