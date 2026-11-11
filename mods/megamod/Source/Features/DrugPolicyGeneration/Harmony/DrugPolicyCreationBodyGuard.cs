using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Megamod.Features.DrugPolicyGeneration;

// A proof for the small native creation path, not an evaluator for arbitrary engine code.
internal static class DrugPolicyCreationBodyGuard
{
    private enum Value { Receiver, Scalar, Null, List }
    internal static string? Validate(MethodBase target, IReadOnlyList<CodeInstruction> body, FieldInfo? list)
    {
        if (body.Count == 0 || body.Count > 256 || body.Any(i => i.blocks.Count != 0)) return "The creation body is empty, unbounded or has exception regions.";
        bool callback = target is MethodInfo && list == null;
        var stack = new Stack<Value>(); var locals = new Dictionary<int, Value>(); var baseCalls = 0; var returns = 0;
        try
        {
            foreach (var instruction in body)
            {
                var op = instruction.opcode; var operand = instruction.operand;
                if (returns != 0) { if (op == OpCodes.Nop) continue; return "Instructions follow the consumed return."; }
                if (op == OpCodes.Nop) continue;
                if (instruction.IsLdarg())
                {
                    int argument = instruction.ArgumentIndex();
                    if (!callback && argument == 0) { stack.Push(Value.Receiver); continue; }
                    if (callback && argument < target.GetParameters().Length && Scalar(target.GetParameters()[argument].ParameterType))
                    { stack.Push(Value.Scalar); continue; }
                    return "A callback reads an unproved native reference.";
                }
                if (op == OpCodes.Ldnull) { stack.Push(Value.Null); continue; }
                if (op.Name?.StartsWith("ldc.", StringComparison.Ordinal) == true) { stack.Push(Value.Scalar); continue; }
                if (op == OpCodes.Dup) { stack.Push(stack.Peek()); continue; }
                if (op == OpCodes.Pop) { stack.Pop(); continue; }
                if (instruction.IsStloc()) { locals[instruction.LocalIndex()] = stack.Pop(); continue; }
                if (instruction.IsLdloc()) { stack.Push(locals[instruction.LocalIndex()]); continue; }
                if (callback && (op == OpCodes.Ldsfld || op == OpCodes.Stsfld) && operand is FieldInfo scalar && Scalar(scalar.FieldType) &&
                    scalar.DeclaringType == target.DeclaringType && scalar.DeclaringType!.TypeInitializer == null)
                {
                    if (op == OpCodes.Ldsfld) stack.Push(Value.Scalar);
                    else if (stack.Pop() != Value.Scalar) return "A callback writes an unproved value.";
                    continue;
                }
                if (op == OpCodes.Add || op == OpCodes.Sub || op == OpCodes.Mul || op == OpCodes.And || op == OpCodes.Or || op == OpCodes.Xor)
                {
                    if (stack.Pop() != Value.Scalar || stack.Pop() != Value.Scalar) return "A callback transforms a native reference.";
                    stack.Push(Value.Scalar); continue;
                }
                if (op == OpCodes.Call && operand is ConstructorInfo constructor && list == null && target is ConstructorInfo &&
                    constructor.DeclaringType == target.DeclaringType!.BaseType && constructor.GetParameters().Length == 0 && stack.Pop() == Value.Receiver)
                { baseCalls++; continue; }
                if (op == OpCodes.Stfld && operand is FieldInfo field && list == null &&
                    field.DeclaringType!.IsAssignableFrom(target.DeclaringType))
                {
                    var value = stack.Pop(); var receiver = stack.Pop();
                    if (receiver != Value.Receiver || !(Scalar(field.FieldType) && value == Value.Scalar || !field.FieldType.IsValueType && value == Value.Null))
                        return "A constructor field write can escape or replace a native reference.";
                    continue;
                }
                if (op == OpCodes.Ldfld && Equals(operand, list) && list != null && stack.Pop() == Value.Receiver) { stack.Push(Value.List); continue; }
                if (op == OpCodes.Ret)
                {
                    if (list != null && stack.Pop() != Value.List || stack.Count != 0 || target is ConstructorInfo && baseCalls != 1)
                        return "The constructor chain or actual-list return is unproved.";
                    returns++; continue;
                }
                return "Unproved creation operation: " + op.Name;
            }
            return returns == 1 ? null : "The consumed creation return is absent.";
        }
        catch (Exception) { return "The creation stack/local flow is unproved."; }
    }
    private static bool Scalar(Type type) => type.IsPrimitive || type.IsEnum;
}
