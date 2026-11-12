using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Megamod.Features.DrugPolicyGeneration;

public sealed class DrugGenerationPawnBodyGuard
{
    private readonly MethodInfo getter;
    private readonly FieldInfo parent, pawn;
    internal MethodInfo Getter => getter;
    private enum Value { Comp, Parent, Pawn }
    public DrugGenerationPawnBodyGuard(MethodInfo getter, FieldInfo parent, FieldInfo pawn)
    { this.getter = getter; this.parent = parent; this.pawn = pawn; }
    internal string? ValidateShape() => getter.IsStatic || getter.IsGenericMethod || getter.ContainsGenericParameters ||
        getter.GetParameters().Length != 0 || getter.ReturnType != pawn.FieldType || pawn.FieldType.IsValueType ||
        parent.IsStatic || pawn.IsStatic || parent.DeclaringType?.IsAssignableFrom(getter.DeclaringType) != true ||
        pawn.DeclaringType?.IsAssignableFrom(parent.FieldType) != true || getter.DeclaringType?.TypeInitializer != null
        ? "The bound comp/parent/pawn field shape changed." : null;

    public string? Validate(IReadOnlyList<CodeInstruction> body)
    {
        var failure = ValidateShape(); if (failure != null) return failure;
        if (body.Count == 0 || body.Count > 256 || body.Any(i => i.blocks.Count != 0)) return "The pawn projection is empty, unbounded or has exception regions.";
        var stack = new Stack<Value>(); var locals = new Dictionary<int, Value>(); bool returned = false;
        try
        {
            foreach (var instruction in body)
            {
                var op = instruction.opcode;
                if (op == OpCodes.Nop) continue;
                if (returned) return "Instructions follow the pawn projection return.";
                if (instruction.IsLdarg() && instruction.ArgumentIndex() == 0) { stack.Push(Value.Comp); continue; }
                if (op == OpCodes.Dup) { stack.Push(stack.Peek()); continue; }
                if (op == OpCodes.Pop) { stack.Pop(); continue; }
                if (instruction.IsStloc())
                {
                    var value = stack.Pop(); var type = value == Value.Comp ? getter.DeclaringType! : value == Value.Parent ? parent.FieldType : pawn.FieldType;
                    int index = instruction.LocalIndex(); var declared = getter.GetMethodBody()?.LocalVariables;
                    var localType = instruction.operand is LocalBuilder local ? local.LocalType : declared != null && index < declared.Count ? declared[index].LocalType : null;
                    if (localType?.IsAssignableFrom(type) != true) return "A pawn projection local changes its reference type.";
                    locals[index] = value; continue;
                }
                if (instruction.IsLdloc()) { stack.Push(locals[instruction.LocalIndex()]); continue; }
                if (op == OpCodes.Ldfld && SameField(instruction.operand, parent) && stack.Pop() == Value.Comp) { stack.Push(Value.Parent); continue; }
                if (op == OpCodes.Ldfld && SameField(instruction.operand, pawn) && stack.Pop() == Value.Parent) { stack.Push(Value.Pawn); continue; }
                if (op == OpCodes.Ret && stack.Pop() == Value.Pawn && stack.Count == 0) { returned = true; continue; }
                return "Unproved pawn projection operation: " + op.Name;
            }
            return returned ? null : "The actual parent/pawn return is absent.";
        }
        catch (Exception) { return "The pawn projection reference/local flow is unproved."; }
    }
    internal static bool SameField(object? operand, FieldInfo field) => operand is FieldInfo candidate && candidate.Module == field.Module &&
        candidate.MetadataToken == field.MetadataToken && candidate.DeclaringType == field.DeclaringType;
}
