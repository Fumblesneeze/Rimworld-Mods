using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Megamod.Features.DrugPolicyGeneration;

/// <summary>
/// Conditional producer proof only: already initialized runtime; same nonnull comp/props;
/// finite negative rate; exact zero range endpoints and reversal. F0/F2 must supply those premises.
/// This validates actual bodies, not the premises, PostAdd dispatch or perpetual instance state.
/// </summary>
public sealed class DrugGenerationSeverityInitializationGuard
{
    public sealed record Bindings(MethodInfo Initialize, MethodInfo BaseInitialize, MethodInfo PropertiesGetter,
        MethodInfo Calculate, MethodInfo RangeGetter, MethodInfo Range, MethodInfo Chance,
        FieldInfo Properties, FieldInfo StoredRate, FieldInfo Rate, FieldInfo RangeField,
        FieldInfo Minimum, FieldInfo Maximum, FieldInfo Reversal);
    private readonly Bindings binding;
    public DrugGenerationSeverityInitializationGuard(Bindings binding) => this.binding = binding;
    internal MethodInfo[] Methods => new[] { binding.Range, binding.Chance, binding.RangeGetter, binding.Calculate,
        binding.PropertiesGetter, binding.BaseInitialize, binding.Initialize };
    internal string? ValidateShape()
    {
        bool Instance(MethodInfo method, Type result, params Type[] parameters) => !method.IsStatic && method.ReturnType == result &&
            method.GetParameters().Select(p => p.ParameterType).SequenceEqual(parameters);
        bool FloatField(FieldInfo field, Type? declaring) => !field.IsStatic && field.FieldType == typeof(float) && field.DeclaringType == declaring;
        var parameters = binding.Initialize.GetParameters();
        if (Methods.Any(m => m.IsGenericMethod || m.ContainsGenericParameters || m.IsAbstract) ||
            Methods.Where((method, index) => Methods.Take(index).Any(previous => DrugGenerationDecayGuard.Same(previous, method))).Any() ||
            binding.Initialize.DeclaringType?.IsClass != true || binding.Calculate.DeclaringType?.IsClass != true ||
            parameters.Length != 1 || Nullable.GetUnderlyingType(parameters[0].ParameterType) == null ||
            !Instance(binding.Initialize, typeof(void), parameters[0].ParameterType) || !Instance(binding.BaseInitialize, typeof(void), parameters[0].ParameterType) ||
            binding.BaseInitialize.DeclaringType?.IsAssignableFrom(binding.Initialize.DeclaringType) != true ||
            !Instance(binding.PropertiesGetter, binding.Calculate.DeclaringType!) || binding.PropertiesGetter.DeclaringType != binding.Initialize.DeclaringType ||
            binding.PropertiesGetter.IsVirtual || binding.Calculate.IsVirtual || binding.RangeGetter.IsVirtual ||
            !Instance(binding.Calculate, typeof(float)) || !Instance(binding.RangeGetter, typeof(float)) || binding.RangeGetter.DeclaringType?.IsValueType != true ||
            !binding.Range.IsStatic || binding.Range.ReturnType != typeof(float) || !binding.Range.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(float), typeof(float) }) ||
            !binding.Chance.IsStatic || binding.Chance.ReturnType != typeof(bool) || !binding.Chance.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(float) }) ||
            binding.Properties.IsStatic || binding.Properties.DeclaringType?.IsAssignableFrom(binding.Initialize.DeclaringType) != true ||
            !binding.Properties.FieldType.IsAssignableFrom(binding.Calculate.DeclaringType) ||
            !FloatField(binding.StoredRate, binding.Initialize.DeclaringType) || !FloatField(binding.Rate, binding.Calculate.DeclaringType) ||
            !FloatField(binding.Reversal, binding.Calculate.DeclaringType) || binding.RangeField.IsStatic || binding.RangeField.DeclaringType != binding.Calculate.DeclaringType ||
            binding.RangeField.FieldType != binding.RangeGetter.DeclaringType || !FloatField(binding.Minimum, binding.RangeGetter.DeclaringType) ||
            !FloatField(binding.Maximum, binding.RangeGetter.DeclaringType) ||
            DrugGenerationPawnBodyGuard.SameField(binding.Rate, binding.Reversal) ||
            DrugGenerationPawnBodyGuard.SameField(binding.Minimum, binding.Maximum))
            return "The bound SeverityPerDay initialization callable/field shapes changed.";
        return null;
    }

    public string? Validate(MethodInfo target, IReadOnlyList<CodeInstruction> body)
    {
        var shape = ValidateShape(); if (shape != null) return shape;
        if (body.Count == 0 || body.Count > 256 || body.Any(i => i.blocks.Count != 0)) return "The initialization body is empty, unbounded or has exception regions.";
        var code = body.Where(i => i.opcode != OpCodes.Nop).ToArray();
        bool Arg(int index, int argument) => index < code.Length && code[index].IsLdarg() && code[index].ArgumentIndex() == argument;
        bool Field(int index, OpCode operation, FieldInfo field) => index < code.Length && code[index].opcode == operation &&
            DrugGenerationPawnBodyGuard.SameField(code[index].operand, field);
        bool Call(int index, OpCode operation, MethodInfo method) => index < code.Length && code[index].opcode == operation &&
            DrugGenerationDecayGuard.Same(code[index].operand, method);
        int Destination(int index)
        {
            if (index >= code.Length || code[index].operand is not Label label || body.Count(i => i.labels.Contains(label)) != 1) return -1;
            var destination = body.SkipWhile(i => !i.labels.Contains(label)).FirstOrDefault(i => i.opcode != OpCodes.Nop);
            return destination == null ? -1 : Array.IndexOf(code, destination);
        }
        bool ZeroBranch(int index) => index < code.Length && (code[index].opcode == OpCodes.Bgt_Un || code[index].opcode == OpCodes.Bgt_Un_S) && Destination(index) > 4;
        Type? LocalType(CodeInstruction instruction)
        {
            if (instruction.operand is LocalBuilder local) return local.LocalType;
            var declared = target.GetMethodBody()?.LocalVariables; int index = instruction.LocalIndex();
            return declared != null && index >= 0 && index < declared.Count ? declared[index].LocalType : null;
        }

        // The first comparison is false for the admitted +/-0 inputs. No reachable edge enters
        // the tail; it is not evaluated or recursively observed as a new ordinary-call dependency.
        // Existing bundle metadata remains conservative: all selected inner callbacks are checked.
        if (DrugGenerationDecayGuard.Same(target, binding.Range))
            return code.Length >= 6 && Arg(0, 1) && Arg(1, 0) && ZeroBranch(2) && Arg(3, 0) && code[4].opcode == OpCodes.Ret
                ? null : "The actual zero-range early return is unproved.";
        if (DrugGenerationDecayGuard.Same(target, binding.Chance))
            return code.Length >= 6 && Arg(0, 0) && code[1].opcode == OpCodes.Ldc_R4 && Equals(code[1].operand, 0f) &&
                ZeroBranch(2) && code[3].LoadsConstant(0) && code[4].opcode == OpCodes.Ret
                ? null : "The actual zero-chance false return is unproved.";
        if (DrugGenerationDecayGuard.Same(target, binding.RangeGetter))
            return code.Length == 6 && Arg(0, 0) && Field(1, OpCodes.Ldfld, binding.Minimum) &&
                Arg(2, 0) && Field(3, OpCodes.Ldfld, binding.Maximum) && Call(4, OpCodes.Call, binding.Range) && code[5].opcode == OpCodes.Ret
                ? null : "The same range address/minimum/maximum flow is unproved.";
        if (DrugGenerationDecayGuard.Same(target, binding.Calculate))
        {
            // Preserve the native rate read before the range call and the Single store before
            // Chance. This is one bounded producer pattern, not an arithmetic/branch interpreter.
            int result = Destination(10);
            return code.Length >= 13 && Arg(0, 0) && Field(1, OpCodes.Ldfld, binding.Rate) &&
                Arg(2, 0) && Field(3, OpCodes.Ldflda, binding.RangeField) && Call(4, OpCodes.Call, binding.RangeGetter) &&
                code[5].opcode == OpCodes.Add && code[6].IsStloc() && LocalType(code[6]) == typeof(float) &&
                Arg(7, 0) && Field(8, OpCodes.Ldfld, binding.Reversal) && Call(9, OpCodes.Call, binding.Chance) &&
                (code[10].opcode == OpCodes.Brfalse || code[10].opcode == OpCodes.Brfalse_S) && result > 10 && result == code.Length - 2 &&
                code[result].IsLdloc() && code[result].LocalIndex() == code[6].LocalIndex() && LocalType(code[result]) == typeof(float) &&
                code[result + 1].opcode == OpCodes.Ret
                ? null : "The same properties/range-address/Single-store/zero-chance result is unproved.";
        }
        if (DrugGenerationDecayGuard.Same(target, binding.PropertiesGetter))
            return code.Length == 4 && Arg(0, 0) && Field(1, OpCodes.Ldfld, binding.Properties) &&
                code[2].opcode == OpCodes.Castclass && Equals(code[2].operand, binding.Calculate.DeclaringType) && code[3].opcode == OpCodes.Ret
                ? null : "The actual comp properties projection is unproved.";
        if (DrugGenerationDecayGuard.Same(target, binding.BaseInitialize))
            return code.Length == 1 && code[0].opcode == OpCodes.Ret ? null : "The base initialization has unproved effects.";
        if (DrugGenerationDecayGuard.Same(target, binding.Initialize))
            return code.Length == 9 && Arg(0, 0) && Arg(1, 1) && Call(2, OpCodes.Call, binding.BaseInitialize) &&
                Arg(3, 0) && Arg(4, 0) && Call(5, OpCodes.Call, binding.PropertiesGetter) && Call(6, OpCodes.Callvirt, binding.Calculate) &&
                Field(7, OpCodes.Stfld, binding.StoredRate) && code[8].opcode == OpCodes.Ret
                ? null : "The same comp/input/base-hook/calculation/Single-field initialization is unproved.";
        return "This method is not one of the seven bound initialization producers.";
    }
}

