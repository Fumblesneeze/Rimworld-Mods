using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Megamod.Features.DrugPolicyGeneration;

/// <summary>The contribution call/float/ref flow only; called helpers require separate admission.</summary>
public sealed class DrugGenerationDecayGuard
{
    private readonly MethodInfo pawnGetter, intervalCheck, dailyRate;
    internal MethodInfo Target { get; }
    private enum Value { Receiver, Accumulator, Delta, Pawn, Interval, Due, Rate, RoundedRate, Multiplier, Product, RoundedProduct, Previous, Sum }

    public DrugGenerationDecayGuard(MethodInfo target, MethodInfo pawnGetter, MethodInfo intervalCheck, MethodInfo dailyRate)
    { Target = target; this.pawnGetter = pawnGetter; this.intervalCheck = intervalCheck; this.dailyRate = dailyRate; }

    internal string? ValidateShape()
    {
        bool Instance(MethodInfo method) => !method.IsStatic && !method.IsGenericMethod && !method.ContainsGenericParameters &&
            method.DeclaringType?.IsAssignableFrom(Target.DeclaringType) == true;
        if (!Instance(Target) || Target.ReturnType != typeof(void) || Target.DeclaringType?.TypeInitializer != null ||
            !Target.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(float).MakeByRefType(), typeof(int) }) ||
            !Instance(pawnGetter) || pawnGetter.GetParameters().Length != 0 || pawnGetter.ReturnType.IsValueType || pawnGetter.ReturnType.IsByRef ||
            !intervalCheck.IsStatic || intervalCheck.IsGenericMethod || intervalCheck.ContainsGenericParameters || intervalCheck.ReturnType != typeof(bool) ||
            intervalCheck.GetParameters().Length != 3 || !intervalCheck.GetParameters()[0].ParameterType.IsAssignableFrom(pawnGetter.ReturnType) ||
            !intervalCheck.GetParameters().Skip(1).Select(p => p.ParameterType).SequenceEqual(new[] { typeof(int), typeof(int) }) ||
            !Instance(dailyRate) || !dailyRate.IsVirtual || dailyRate.ReturnType != typeof(float) || dailyRate.GetParameters().Length != 0)
            return "The bound decay callable shapes changed.";
        return null;
    }

    // This recognizes the one native conditional contribution, not arbitrary control flow.
    // Local aliases and nops are harmless; every call and ref read/write has a fixed provenance.
    public string? Validate(IReadOnlyList<CodeInstruction> body)
    {
        var shape = ValidateShape(); if (shape != null) return shape;
        if (body.Count == 0 || body.Count > 256 || body.Any(i => i.blocks.Count != 0)) return "The decay body is empty, unbounded or has exception regions.";
        var stack = new Stack<Value>(); var locals = new Dictionary<int, Value>();
        int pawns = 0, intervals = 0, rates = 0, products = 0, reads = 0, adds = 0, writes = 0, phase = 0;
        bool productRounded = false;
        Label? skip = null;
        Type? LocalType(CodeInstruction instruction)
        {
            if (instruction.operand is LocalBuilder local) return local.LocalType;
            var declared = Target.GetMethodBody()?.LocalVariables; int index = instruction.LocalIndex();
            return declared != null && index >= 0 && index < declared.Count ? declared[index].LocalType : null;
        }
        bool LocalAccepts(Type? type, Value value) => value switch
        {
            Value.Receiver => type?.IsAssignableFrom(Target.DeclaringType) == true,
            Value.Pawn => type?.IsAssignableFrom(pawnGetter.ReturnType) == true,
            Value.Accumulator => type == typeof(float).MakeByRefType(),
            Value.Delta or Value.Interval => type == typeof(int),
            Value.Due => type == typeof(bool),
            _ => type == typeof(float)
        };
        try
        {
            for (int index = 0; index < body.Count; index++)
            {
                var instruction = body[index]; var op = instruction.opcode;
                if (phase == 2 && op != OpCodes.Nop) return "Instructions follow the consumed decay return.";
                if (op == OpCodes.Nop) continue;
                if (instruction.IsLdarg())
                {
                    stack.Push(instruction.ArgumentIndex() switch { 0 => Value.Receiver, 1 => Value.Accumulator, 2 => Value.Delta, _ => throw new InvalidOperationException() });
                    continue;
                }
                if (instruction.IsStloc())
                {
                    var value = stack.Pop();
                    if (!LocalAccepts(LocalType(instruction), value)) return "A decay local changes the value type or float rounding.";
                    if (value == Value.Product) productRounded = true;
                    locals[instruction.LocalIndex()] = value switch { Value.Rate => Value.RoundedRate, Value.Product => Value.RoundedProduct, _ => value }; continue;
                }
                if (instruction.IsLdloc()) { stack.Push(locals[instruction.LocalIndex()]); continue; }
                if (op == OpCodes.Dup) { stack.Push(stack.Peek()); continue; }
                if (op == OpCodes.Pop) { stack.Pop(); continue; }
                if (instruction.LoadsConstant(200)) { stack.Push(Value.Interval); continue; }
                if (op == OpCodes.Ldc_R4 && instruction.operand is float multiplier && multiplier == 0.0033333334f)
                { stack.Push(Value.Multiplier); continue; }
                if (op == OpCodes.Call && Same(instruction.operand, pawnGetter) && phase == 0 && stack.Pop() == Value.Receiver)
                { pawns++; stack.Push(Value.Pawn); continue; }
                if (op == OpCodes.Call && Same(instruction.operand, intervalCheck) && phase == 0 &&
                    stack.Pop() == Value.Delta && stack.Pop() == Value.Interval && stack.Pop() == Value.Pawn)
                { intervals++; stack.Push(Value.Due); continue; }
                if ((op == OpCodes.Brfalse || op == OpCodes.Brfalse_S) && phase == 0 && instruction.operand is Label label &&
                    stack.Pop() == Value.Due && stack.Count == 0 && pawns == 1 && intervals == 1)
                {
                    // The sole false edge must reach the final return (possibly through nops).
                    var destinations = body.Select((i, n) => (i, n)).Where(pair => pair.i.labels.Contains(label)).ToArray();
                    if (destinations.Length != 1 || destinations[0].n <= index ||
                        body.Skip(destinations[0].n).FirstOrDefault(i => i.opcode != OpCodes.Nop)?.opcode != OpCodes.Ret)
                        return "The interval false path does not skip the whole contribution.";
                    skip = label; phase = 1; continue;
                }
                if (op == OpCodes.Callvirt && Same(instruction.operand, dailyRate) && phase == 1 && stack.Pop() == Value.Receiver)
                { rates++; stack.Push(Value.Rate); continue; }
                if (op == OpCodes.Mul && phase == 1 && stack.Pop() == Value.Multiplier && stack.Pop() == Value.RoundedRate)
                { products++; stack.Push(Value.Product); continue; }
                if (op == OpCodes.Ldind_R4 && phase == 1 && rates == 1 && productRounded && stack.Pop() == Value.Accumulator)
                { reads++; stack.Push(Value.Previous); continue; }
                if (op == OpCodes.Add && phase == 1 && stack.Pop() == Value.RoundedProduct && stack.Pop() == Value.Previous)
                { adds++; stack.Push(Value.Sum); continue; }
                if (op == OpCodes.Stind_R4 && phase == 1 && stack.Pop() == Value.Sum && stack.Pop() == Value.Accumulator)
                { writes++; continue; }
                if (op == OpCodes.Ret && phase == 1 && skip != null && stack.Count == 0 && rates == 1 && products == 1 && reads == 1 && adds == 1 && writes == 1)
                { phase = 2; continue; }
                return "Unproved decay contribution operation: " + op.Name;
            }
            return phase == 2 ? null : "The exactly-once decay contribution is unproved.";
        }
        catch (Exception) { return "The decay receiver/ref/local flow is unproved."; }
    }

    internal static bool Same(object? operand, MethodInfo method) => operand is MethodInfo candidate &&
        candidate.Module == method.Module && candidate.MetadataToken == method.MetadataToken && candidate.DeclaringType == method.DeclaringType;
}
