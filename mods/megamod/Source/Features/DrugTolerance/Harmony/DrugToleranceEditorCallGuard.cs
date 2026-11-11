using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Megamod.Features.DrugTolerance;

public sealed class DrugToleranceEditorCallGuard
{
    private readonly MethodInfo calculator, widthGetter, widthSetter;
    private readonly bool header;
    private readonly HashSet<MethodInfo> rectangleReaders;
    private enum Kind { Receiver, Rectangle, RectangleAddress, LocalAddress, Number }
    private readonly record struct Value(Kind Kind, int Index = -1, int Scale = 0, float Offset = 0);
    public DrugToleranceEditorCallGuard(MethodInfo calculator, MethodInfo widthGetter, MethodInfo widthSetter, bool header,
        IEnumerable<MethodInfo>? rectangleReaders = null)
    {
        this.calculator = calculator; this.widthGetter = widthGetter; this.widthSetter = widthSetter; this.header = header;
        this.rectangleReaders = new(rectangleReaders ?? new[] { widthGetter });
    }

    public string? Validate(IReadOnlyList<CodeInstruction> body)
    {
        var parameters = calculator.GetParameters();
        if (calculator.IsStatic || calculator.ReturnType != typeof(void) || parameters.Length != 10 ||
            parameters[0].ParameterType != widthGetter.DeclaringType ||
            parameters.Skip(1).Any(p => p.ParameterType != typeof(float).MakeByRefType()) ||
            widthGetter.IsStatic || widthGetter.ReturnType != typeof(float) || widthGetter.GetParameters().Length != 0 ||
            widthGetter.DeclaringType?.IsValueType != true || widthSetter.DeclaringType != widthGetter.DeclaringType ||
            widthSetter.IsStatic || widthSetter.ReturnType != typeof(void) ||
            !widthSetter.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(float) }) ||
            rectangleReaders.Any(m => m.DeclaringType != widthGetter.DeclaringType || m.IsStatic ||
                m.ReturnType != typeof(float) || m.GetParameters().Length != 0))
            return "The rectangle or column callable changed.";
        if (body.Count == 0 || body.Count > 4096 || body.Any(i => i.blocks.Count != 0))
            return "The native editor body is absent, oversized or has unproved exception flow.";
        var calls = Enumerable.Range(0, body.Count).Where(i => body[i].Calls(calculator)).ToArray();
        if (calls.Length != 1) return "There is no unique native column calculation.";
        int call = calls[0];
        var branchEntries = new HashSet<int>();
        var destinations = new Dictionary<Label, int>();
        foreach (int position in Enumerable.Range(0, body.Count))
            foreach (var label in body[position].labels)
            {
                if (destinations.ContainsKey(label)) return "A branch destination is duplicated.";
                destinations[label] = position;
            }
        foreach (int position in Enumerable.Range(0, body.Count))
        {
            var labels = body[position].operand is Label label ? new[] { label } : body[position].operand as Label[] ?? Array.Empty<Label>();
            foreach (var target in labels)
            {
                if (!destinations.TryGetValue(target, out int destination)) return "An editor branch has no destination.";
                branchEntries.Add(destination);
                if (position > call && destination <= call) return "A later branch reenters the protected column producer.";
            }
        }
        var stack = new Stack<Value>();
        var locals = new Dictionary<int, Value>();
        var rectangle = new Value(Kind.Rectangle);
        Value Pop() => stack.Count == 0 ? throw new InvalidOperationException("The rectangle producer stack underflows.") : stack.Pop();
        Value Local(int id) => locals.TryGetValue(id, out var value) ? value : throw new InvalidOperationException("An editor local has no proved value.");
        Value Read(Value address) => address.Kind == Kind.RectangleAddress ? rectangle :
            address.Kind == Kind.LocalAddress ? Local(address.Index) : throw new InvalidOperationException("The rectangle receiver is not a proved address.");
        try
        {
            for (int position = 0; position < call; position++)
            {
                var instruction = body[position]; var code = instruction.opcode;
                if (code == OpCodes.Nop) continue;
                if (code == OpCodes.Br || code == OpCodes.Br_S)
                {
                    if (instruction.operand is not Label label || !destinations.TryGetValue(label, out int destination) ||
                        destination <= position || destination > call) return "The column producer has unproved control flow.";
                    position = destination - 1; continue;
                }
                if (code == OpCodes.Ldarg_0) { stack.Push(new(Kind.Receiver)); continue; }
                if (Argument(instruction) == 1)
                {
                    if (code == OpCodes.Ldarga || code == OpCodes.Ldarga_S) stack.Push(new(Kind.RectangleAddress));
                    else if (code == OpCodes.Starg || code == OpCodes.Starg_S)
                    { rectangle = Pop(); if (rectangle.Kind != Kind.Rectangle) return "The supplied rectangle was replaced with an unknown value."; }
                    else stack.Push(rectangle);
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
                if (code == OpCodes.Ldc_R4) { stack.Push(new(Kind.Number, Offset: Convert.ToSingle(instruction.operand))); continue; }
                if (instruction.Calls(widthGetter))
                {
                    var value = Read(Pop()); if (value.Kind != Kind.Rectangle) return "Width is read from an unrelated value.";
                    stack.Push(new(Kind.Number, Scale: 1, Offset: value.Offset)); continue;
                }
                if (instruction.Calls(widthSetter))
                {
                    var value = Pop(); var address = Pop();
                    if (value.Kind != Kind.Number || value.Scale != 1 || !float.IsFinite(value.Offset) || Read(address).Kind != Kind.Rectangle)
                        return "The native rectangle width no longer retains the supplied width.";
                    if (address.Kind == Kind.RectangleAddress) rectangle = new(Kind.Rectangle, Offset: value.Offset);
                    else locals[address.Index] = new(Kind.Rectangle, Offset: value.Offset);
                    continue;
                }
                if (code == OpCodes.Add || code == OpCodes.Sub)
                {
                    var right = Pop(); var left = Pop();
                    if (right.Kind != Kind.Number || left.Kind != Kind.Number) return "Rectangle arithmetic consumes an unproved value.";
                    // Constants use actual Single arithmetic. A variable width permits only the native
                    // header deduction (or an identity operation), never ideal affine cancellation.
                    if (left.Scale == 0 && right.Scale == 0)
                        stack.Push(new(Kind.Number, Offset: code == OpCodes.Add ? left.Offset + right.Offset : left.Offset - right.Offset));
                    else if (left.Scale == 1 && right.Scale == 0 && right.Offset == 0)
                        stack.Push(left);
                    else if (header && left.Scale == 1 && left.Offset == 0 && right.Scale == 0 &&
                        code == OpCodes.Sub && right.Offset == 16)
                        stack.Push(new(Kind.Number, Scale: 1, Offset: -16));
                    else return "The rectangle width uses unproved Single arithmetic.";
                    continue;
                }
                return "An operation before the native column calculation is unproved: " + code.Name;
            }
            if (stack.Count != 11) return "The native calculator arguments are not a complete producer chain.";
            var outputs = new HashSet<int>();
            for (int i = 0; i < 9; i++)
            {
                var value = Pop();
                if (value.Kind != Kind.LocalAddress || !outputs.Add(value.Index)) return "Column outputs do not have distinct native locals.";
            }
            var supplied = Pop();
            if (supplied.Kind != Kind.Rectangle || supplied.Offset != (header ? -16 : 0) || Pop().Kind != Kind.Receiver)
                return "The calculator does not consume this manager and its expected input rectangle.";

            bool ImmediateRead(int position, bool rectangleRead)
            {
                int consumer = position + 1;
                while (consumer < body.Count && body[consumer].opcode == OpCodes.Nop) consumer++;
                if (consumer >= body.Count || Enumerable.Range(position + 1, consumer - position).Any(branchEntries.Contains)) return false;
                return rectangleRead ? body[consumer].operand is MethodInfo method && body[consumer].Calls(method) && rectangleReaders.Contains(method)
                    : body[consumer].opcode == OpCodes.Ldind_R4;
            }
            var outputBits = outputs.Select((local, index) => (local, bit: 1 << index)).ToDictionary(x => x.local, x => x.bit);
            var incoming = new Dictionary<int, int>();
            var pending = new Queue<int>();
            void Visit(int position, int reads)
            {
                if (position <= call || position >= body.Count) throw new InvalidOperationException("The editor exits its proved output consumer flow.");
                if (incoming.TryGetValue(position, out int previous))
                {
                    int common = previous & reads;
                    if (common == previous) return;
                    incoming[position] = common;
                }
                else incoming[position] = reads;
                pending.Enqueue(position);
            }
            Visit(call + 1, 0);
            bool returns = false;
            while (pending.Count != 0)
            {
                int position = pending.Dequeue(), reads = incoming[position];
                var instruction = body[position]; var code = instruction.opcode; int local = LocalIndex(instruction);
                if (Argument(instruction) == 1 && (code == OpCodes.Starg || code == OpCodes.Starg_S ||
                    ((code == OpCodes.Ldarga || code == OpCodes.Ldarga_S) && !ImmediateRead(position, true))))
                    return "The supplied rectangle is replaced or its address escapes after allocation.";
                if (outputs.Contains(local))
                {
                    if (code.Name!.StartsWith("stloc", StringComparison.Ordinal) ||
                        ((code == OpCodes.Ldloca || code == OpCodes.Ldloca_S) && !ImmediateRead(position, false)))
                        return "A native column output is overwritten or its address escapes.";
                    reads |= outputBits[local];
                }
                if (local >= 0 && locals.TryGetValue(local, out var alias) &&
                    ((alias.Kind == Kind.Rectangle && (code.Name!.StartsWith("stloc", StringComparison.Ordinal) ||
                        ((code == OpCodes.Ldloca || code == OpCodes.Ldloca_S) && !ImmediateRead(position, true)))) ||
                    (!code.Name!.StartsWith("stloc", StringComparison.Ordinal) &&
                    ((alias.Kind == Kind.RectangleAddress && !ImmediateRead(position, true)) ||
                     (alias.Kind == Kind.LocalAddress && outputs.Contains(alias.Index) && !ImmediateRead(position, false))))
                    )) return "An earlier rectangle copy or captured address is overwritten or escapes after allocation.";
                if (code == OpCodes.Ret)
                {
                    returns = true;
                    if (reads != (1 << 9) - 1) return "A native return bypasses calculated column reads.";
                    continue;
                }
                if (code.FlowControl == FlowControl.Throw) continue;
                if (instruction.operand is Label destinationLabel &&
                    (code.FlowControl == FlowControl.Branch || code.FlowControl == FlowControl.Cond_Branch))
                {
                    Visit(destinations[destinationLabel], reads);
                    if (code.FlowControl == FlowControl.Branch) continue;
                }
                else if (instruction.operand is Label[] switchLabels && code == OpCodes.Switch)
                    foreach (var switchLabel in switchLabels) Visit(destinations[switchLabel], reads);
                else if (code.FlowControl == FlowControl.Branch || code.FlowControl == FlowControl.Cond_Branch)
                    return "The native output consumer has unproved control flow.";
                Visit(position + 1, reads);
            }
            return returns ? null : "The native output consumer has no proved return.";
        }
        catch (Exception error) { return error.GetBaseException().Message; }
    }

    private static int? Argument(CodeInstruction instruction)
    {
        if (instruction.opcode == OpCodes.Ldarg_0) return 0;
        if (instruction.opcode == OpCodes.Ldarg_1) return 1;
        if (instruction.opcode == OpCodes.Ldarg_2) return 2;
        if (instruction.opcode == OpCodes.Ldarg_3) return 3;
        if (instruction.opcode != OpCodes.Ldarg && instruction.opcode != OpCodes.Ldarg_S &&
            instruction.opcode != OpCodes.Ldarga && instruction.opcode != OpCodes.Ldarga_S &&
            instruction.opcode != OpCodes.Starg && instruction.opcode != OpCodes.Starg_S) return null;
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
