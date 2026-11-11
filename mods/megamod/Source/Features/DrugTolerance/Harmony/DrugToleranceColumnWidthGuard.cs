using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Megamod.Features.DrugTolerance;

// Only the native policy column calculator: nine outputs must retain their affine width meanings.
public sealed class DrugToleranceColumnWidthGuard
{
    private readonly MethodInfo widthGetter;
    public DrugToleranceColumnWidthGuard(MethodInfo widthGetter) => this.widthGetter = widthGetter;
    private enum Kind { Number, Rectangle, RectangleAddress, OutputAddress, LocalAddress }
    private readonly record struct Value(Kind Kind, int Index = -1, double Scale = 0, double Constant = 0);

    public string? Validate(IReadOnlyList<CodeInstruction> body)
    {
        if (body.Count == 0 || body.Count > 1024 || widthGetter.IsStatic || widthGetter.ReturnType != typeof(float) ||
            widthGetter.GetParameters().Length != 0 || widthGetter.DeclaringType?.IsValueType != true)
            return "The native rectangle-width contract is absent or changed.";
        var stack = new Stack<Value>();
        var locals = new Dictionary<int, Value>();
        var outputs = new Dictionary<int, Value>();
        Value Pop() => stack.Count != 0 ? stack.Pop() : throw new InvalidOperationException("Column arithmetic stack underflow.");
        Value Local(int index) => locals.TryGetValue(index, out var value) ? value : throw new InvalidOperationException("A column local is read before its proved value.");
        Value ReadAddress(Value address) => address.Kind switch
        {
            Kind.RectangleAddress => new(Kind.Rectangle),
            Kind.LocalAddress => Local(address.Index),
            Kind.OutputAddress => outputs.TryGetValue(address.Index, out var value) ? value : throw new InvalidOperationException("An output is read before its proved write."),
            _ => throw new InvalidOperationException("A column address has no proved referent.")
        };
        try
        {
            for (int position = 0; position < body.Count; position++)
            {
                var instruction = body[position];
                var code = instruction.opcode;
                if (instruction.blocks.Count != 0) return "Exception control flow in the width calculator is unproved.";
                if (code == OpCodes.Nop) continue;
                if (code == OpCodes.Br || code == OpCodes.Br_S)
                {
                    var destinations = instruction.operand is Label label
                        ? Enumerable.Range(0, body.Count).Where(i => body[i].labels.Contains(label)).ToArray()
                        : Array.Empty<int>();
                    if (destinations.Length != 1 || destinations[0] <= position)
                        return "A calculator branch has no unique forward destination.";
                    position = destinations[0] - 1;
                    continue;
                }
                if (code == OpCodes.Ldc_R4)
                {
                    double value = Convert.ToDouble(instruction.operand);
                    if (!double.IsFinite(value)) return "A native column constant is nonfinite.";
                    stack.Push(new(Kind.Number, Constant: value)); continue;
                }
                if (Argument(instruction) is { } argument)
                {
                    if (code == OpCodes.Ldarga || code == OpCodes.Ldarga_S)
                    {
                        if (argument != 1) return "A non-rectangle argument address escapes the column contract.";
                        stack.Push(new(Kind.RectangleAddress));
                    }
                    else if (argument == 1) stack.Push(new(Kind.Rectangle));
                    else if (argument >= 2 && argument <= 10) stack.Push(new(Kind.OutputAddress, argument - 2));
                    else return "The width calculation consumes an unrelated argument.";
                    continue;
                }
                int local = LocalIndex(instruction);
                if (local >= 0)
                {
                    if (code.Name!.StartsWith("stloc", StringComparison.Ordinal)) locals[local] = Pop();
                    else stack.Push(code == OpCodes.Ldloca || code == OpCodes.Ldloca_S ? new(Kind.LocalAddress, local) : Local(local));
                    continue;
                }
                if (code == OpCodes.Dup) { var value = Pop(); stack.Push(value); stack.Push(value); continue; }
                if (code == OpCodes.Pop) { Pop(); continue; }
                if (instruction.Calls(widthGetter))
                {
                    if (ReadAddress(Pop()).Kind != Kind.Rectangle) return "The width getter does not consume the supplied rectangle.";
                    stack.Push(new(Kind.Number, Scale: 1)); continue;
                }
                if (code == OpCodes.Add || code == OpCodes.Sub || code == OpCodes.Mul || code == OpCodes.Div)
                {
                    var right = Pop(); var left = Pop();
                    if (left.Kind != Kind.Number || right.Kind != Kind.Number) return "Column arithmetic consumes an address or unrelated value.";
                    Value result;
                    if (left.Scale == 0 && right.Scale == 0)
                    {
                        float a = (float)left.Constant, b = (float)right.Constant;
                        float value = code == OpCodes.Add ? a + b : code == OpCodes.Sub ? a - b : code == OpCodes.Mul ? a * b : a / b;
                        result = new(Kind.Number, Constant: value);
                    }
                    else if ((code == OpCodes.Sub || code == OpCodes.Add) && left.Scale == 1 && right.Scale == 0)
                    {
                        double deduction = code == OpCodes.Sub ? right.Constant : -right.Constant;
                        // Only the native integer allocations (split or combined) are equivalent
                        // over the actual admitted GUI width range. Arbitrary real-number algebra
                        // is not a Single proof: width + 1e30f - 1e30f destroys the supplied width.
                        if (deduction != 27 && deduction != 108 && deduction != 135 ||
                            left.Constant - deduction < -135)
                            return "The width arithmetic introduces an unproved Single-precision transformation.";
                        result = new(Kind.Number, Scale: 1, Constant: left.Constant - deduction);
                    }
                    else if (code == OpCodes.Mul && ((left.Scale == 1 && left.Constant == -135 && right.Scale == 0) ||
                        (right.Scale == 1 && right.Constant == -135 && left.Scale == 0)))
                    {
                        double factor = left.Scale == 0 ? left.Constant : right.Constant;
                        if (factor != 0.25f && factor != 0.3f && factor != 0.15f)
                            return "The column multiplier is not a native Single allocation.";
                        result = new(Kind.Number, Scale: factor, Constant: -135d * factor);
                    }
                    else return "The column operations do not preserve the proved native Single calculation.";
                    if (!double.IsFinite(result.Scale) || !double.IsFinite(result.Constant)) return "Column arithmetic is not finite.";
                    stack.Push(result); continue;
                }
                if (code == OpCodes.Stind_R4)
                {
                    var value = Pop(); var destination = Pop();
                    if (value.Kind != Kind.Number) return "A column write does not contain a number.";
                    if (destination.Kind == Kind.OutputAddress) outputs[destination.Index] = value;
                    else if (destination.Kind == Kind.LocalAddress) locals[destination.Index] = value;
                    else return "A column write targets an unproved address.";
                    continue;
                }
                if (code == OpCodes.Ldind_R4) { stack.Push(ReadAddress(Pop())); continue; }
                if (code == OpCodes.Ret)
                    return position == body.Count - 1 && stack.Count == 0 && Expected(outputs) ? null : "Native column outputs do not retain their required meanings.";
                return "An operation outside the proved native width calculation was introduced: " + code.Name;
            }
            return "The native width calculation has no proved return.";
        }
        catch (Exception error) { return error.GetBaseException().Message; }
    }

    private static bool Expected(IReadOnlyDictionary<int, Value> values)
    {
        var scales = new[] { 0f, 0f, 0f, 0f, 0.25f, 0.3f, 0.15f, 0.15f, 0.15f };
        var fixedWidths = new[] { 36d, 36d, 36d, 27d };
        return values.Count == 9 && Enumerable.Range(0, 9).All(index => values.TryGetValue(index, out var value) &&
            value.Kind == Kind.Number && Math.Abs(value.Scale - scales[index]) <= 1e-9 &&
            Math.Abs(value.Constant - (index < 4 ? fixedWidths[index] : -135d * scales[index])) <= 1e-6);
    }
    private static int? Argument(CodeInstruction instruction)
    {
        if (instruction.opcode == OpCodes.Ldarg_0) return 0;
        if (instruction.opcode == OpCodes.Ldarg_1) return 1;
        if (instruction.opcode == OpCodes.Ldarg_2) return 2;
        if (instruction.opcode == OpCodes.Ldarg_3) return 3;
        if (instruction.opcode != OpCodes.Ldarg && instruction.opcode != OpCodes.Ldarg_S &&
            instruction.opcode != OpCodes.Ldarga && instruction.opcode != OpCodes.Ldarga_S) return null;
        return instruction.operand is ParameterInfo parameter ? parameter.Position + 1 : Convert.ToInt32(instruction.operand);
    }
    private static int LocalIndex(CodeInstruction instruction)
    {
        var code = instruction.opcode;
        if (code == OpCodes.Ldloc_0 || code == OpCodes.Stloc_0) return 0;
        if (code == OpCodes.Ldloc_1 || code == OpCodes.Stloc_1) return 1;
        if (code == OpCodes.Ldloc_2 || code == OpCodes.Stloc_2) return 2;
        if (code == OpCodes.Ldloc_3 || code == OpCodes.Stloc_3) return 3;
        if (code != OpCodes.Ldloc && code != OpCodes.Ldloc_S && code != OpCodes.Ldloca && code != OpCodes.Ldloca_S &&
            code != OpCodes.Stloc && code != OpCodes.Stloc_S) return -1;
        return instruction.operand is LocalBuilder local ? local.LocalIndex :
            instruction.operand is LocalVariableInfo variable ? variable.LocalIndex : Convert.ToInt32(instruction.operand);
    }
}
