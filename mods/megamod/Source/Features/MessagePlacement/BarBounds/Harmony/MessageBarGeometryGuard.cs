using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Megamod.Features.MessagePlacement;

public sealed record MessageBarGeometryTargets(FieldInfo Locations, MethodInfo Size,
    FieldInfo VectorX, FieldInfo VectorY, ConstructorInfo RectangleConstructor,
    MethodInfo Clicks, MethodInfo Draw, FieldInfo EntryPawn, FieldInfo EntryMap, FieldInfo Drawer,
    IReadOnlyCollection<Type>? DrawingTypes = null);

public sealed record MessageBarGeometryDescription(string? Failure, ISet<int> Required,
    int IndexLocal, int RectangleLocal, int EntryLocal, IReadOnlyList<int> EntryConsumers);

// Target-local proof of the prepared bar's actual rectangle and entry call arguments.
public sealed class MessageBarGeometryGuard
{
    private readonly MessageBarGeometryTargets targets;
    private readonly MessageBarAdornmentGuard? adornments;
    public MessageBarGeometryGuard(MessageBarGeometryTargets targets, MessageBarAdornmentGuard? adornments = null)
    { this.targets = targets; this.adornments = adornments; }

    public Type RectangleType => targets.RectangleConstructor.DeclaringType!;

    public MessageBarGeometryDescription Describe(IReadOnlyList<CodeInstruction> body, int handoffEnd, int entriesLocal)
    {
        MessageBarGeometryDescription Deny(string reason) => new(reason, new HashSet<int>(), -1, -1, -1, Array.Empty<int>());
        if (handoffEnd < 0 || handoffEnd >= body.Count || entriesLocal < 0 || body.Count > 10000)
            return Deny("The prepared geometry boundary is missing or outside the bounded native body.");
        try
        {
            var graph = new Graph(body);
            var required = new HashSet<int>();
            var consumers = new[] { targets.Clicks, targets.Draw }.Select(method =>
                Enumerable.Range(0, body.Count).Where(index => body[index].Calls(method)).ToArray()).ToArray();
            if (consumers.Any(matches => matches.Length != 1 || matches[0] <= handoffEnd))
                return Deny("A native rectangle consumer is missing, ambiguous or precedes preparation.");
            var calls = consumers.Select(matches => matches[0]).ToArray();
            var arguments = calls.Select(index => Expression.Read(body, index)).ToArray();
            int rectangle = -1, entry = -1;
            for (int i = 0; i < arguments.Length; i++)
            {
                var args = arguments[i].Inputs;
                if (args.Length < (i == 0 ? 3 : 4) || !Field(args[0], targets.Drawer, This) ||
                    Local(args[1], false) < 0 || !Field(args[2], targets.EntryPawn, value => Local(value, true) >= 0))
                    return Deny("A native consumer no longer receives this drawer, its rectangle and the prepared entry pawn.");
                int rect = Local(args[1], false), row = Local(args[2].Inputs[0], true);
                if (i == 1 && (!Field(args[3], targets.EntryMap, value => Local(value, true) == row) ||
                    rect != rectangle || row != entry))
                    return Deny("Native drawing and click handling do not consume the same rectangle and entry.");
                rectangle = rect; entry = row;
                graph.ProtectExpression(arguments[i], required);
            }
            if (rectangle == entry || rectangle == entriesLocal || entry == entriesLocal)
                return Deny("Native geometry locals alias the prepared entries collection.");

            var rectangles = new List<(Expression Expression, int Write, int Index, int IndexRead)>();
            for (int i = handoffEnd + 1; i < body.Count; i++)
            {
                if (!Equals(body[i].operand, targets.RectangleConstructor) ||
                    (body[i].opcode != OpCodes.Call && body[i].opcode != OpCodes.Newobj)) continue;
                var expression = Expression.Read(body, i);
                bool constructed = body[i].opcode == OpCodes.Newobj;
                int next = Next(body, i);
                int write = constructed ? next : i;
                if ((constructed ? Stored(body[next]) : Address(expression.Inputs[0])) != rectangle) continue;
                var values = expression.Inputs.Skip(constructed ? 0 : 1).ToArray();
                if (values.Length != 4 || !Coordinate(values[0], targets.VectorX, out int xIndex, out int xRead) ||
                    !Coordinate(values[1], targets.VectorY, out int yIndex, out _) || xIndex != yIndex ||
                    !Dimension(values[2], targets.VectorX) || !Dimension(values[3], targets.VectorY)) continue;
                rectangles.Add((expression, write, xIndex, xRead));
            }
            if (rectangles.Count != 1) return Deny("The current locations and size do not define one unambiguous native rectangle.");
            var producer = rectangles[0];
            if (producer.Index == rectangle || producer.Index == entry || producer.Index == entriesLocal)
                return Deny("The entry index aliases a prepared geometry local.");
            graph.ProtectProducer(producer.Expression, producer.Write, required);

            var entries = new List<(Expression Expression, int Write)>();
            for (int i = handoffEnd + 1; i < body.Count; i++)
            {
                if (Stored(body[i]) != entry) continue;
                var value = Expression.Read(body, Previous(body, i));
                if (Item(value, node => Local(node, false) == entriesLocal, producer.Index)) entries.Add((value, i));
            }
            if (entries.Count != 1) return Deny("The native entry is not uniquely read from the prepared entries and rectangle index.");
            var entryProducer = entries[0];
            graph.ProtectProducer(entryProducer.Expression, entryProducer.Write, required);
            int constructorAddress = producer.Expression.Instruction.opcode == OpCodes.Call ? producer.Expression.Inputs[0].Index : -1;
            if (graph.EscapesAddress(new[] { entriesLocal, producer.Index, rectangle, entry }, constructorAddress, RectangleType))
                return Deny("A protected geometry local address escapes its proved immediate consumer.");
            // Every executable path must retain the proven local values. An address used by
            // another operation is conservatively a write; the guard does not infer purity.
            foreach (int call in calls)
            {
                if (!graph.UnchangedSince(producer.Write, call, rectangle, producer.Write) ||
                    !graph.UnchangedSince(entryProducer.Write, call, entry, -1) ||
                    !graph.UnchangedSince(producer.IndexRead, call, producer.Index, -1) ||
                    !graph.UnchangedSince(entryProducer.Expression.Inputs[1].Index, call, producer.Index, -1))
                    return Deny("A branch, local overwrite or address use can change the prepared consumer geometry.");
            }
            if (!graph.UnchangedSince(handoffEnd, entryProducer.Expression.Index, entriesLocal, -1))
                return Deny("The prepared entries collection can change before native entry selection.");
            if (adornments != null)
            {
                var adornment = adornments.Describe(body, handoffEnd, entriesLocal, entry, rectangle, calls[1]);
                if (adornment.Failure != null) return Deny(adornment.Failure);
                required.UnionWith(adornment.Required);
            }
            if (body.Any(instruction => instruction.operand is MethodInfo method && instruction.Calls(method) &&
                UnprovedDrawing(method)))
                return Deny("An additional direct drawing call has no proved geometry contribution.");
            return new(null, required, producer.Index, rectangle, entry, calls);
        }
        catch (InvalidOperationException error) { return Deny(error.Message); }
    }

    private bool UnprovedDrawing(MethodInfo method)
    {
        if (method == targets.Clicks || method == targets.Draw || adornments?.IsProvedCall(method) == true) return false;
        bool family = method.DeclaringType == targets.Draw.DeclaringType ||
            adornments?.IsDrawingFamily(method.DeclaringType) == true ||
            method.DeclaringType != null && targets.DrawingTypes?.Contains(method.DeclaringType) == true;
        return family && (method.Name.StartsWith("Draw", StringComparison.Ordinal) ||
            method.Name is "set_matrix" or "ScaleAroundPivot" or "RotateAroundPivot" or "EndGroup" or "EndClip" or "EndScrollView" ||
            method.GetParameters().Any(parameter => parameter.ParameterType == RectangleType ||
                parameter.ParameterType == RectangleType.MakeByRefType()));
    }

    private bool Coordinate(Expression expression, FieldInfo component, out int index, out int read)
    {
        index = read = -1;
        if (!Field(expression, component, _ => true)) return false;
        var item = expression.Inputs[0];
        if (item.Inputs.Length != 2) return false;
        index = Local(item.Inputs[1], false); read = item.Inputs[1].Index;
        return index >= 0 && Item(item, value => Field(value, targets.Locations, This), index);
    }
    private bool Dimension(Expression expression, FieldInfo component) =>
        Field(expression, component, value => value.Instruction.Calls(targets.Size) &&
            value.Inputs.Length == 1 && This(value.Inputs[0]));
    private static bool Item(Expression value, Func<Expression, bool> receiver, int index) =>
        value.Instruction.opcode is var code && (code == OpCodes.Call || code == OpCodes.Callvirt) &&
        value.Instruction.operand is MethodInfo method && !method.IsStatic && method.Name == "get_Item" &&
        method.DeclaringType?.IsGenericType == true && method.DeclaringType.GetGenericTypeDefinition() == typeof(List<>) &&
        method.GetParameters() is { Length: 1 } parameters && parameters[0].ParameterType == typeof(int) &&
        value.Inputs.Length == 2 && receiver(value.Inputs[0]) && Local(value.Inputs[1], false) == index;
    private static bool Field(Expression value, FieldInfo field, Func<Expression, bool> receiver) =>
        value.Instruction.opcode == OpCodes.Ldfld && Equals(value.Instruction.operand, field) &&
        value.Inputs.Length == 1 && receiver(value.Inputs[0]);
    private static bool This(Expression value) => value.Instruction.opcode == OpCodes.Ldarg_0;
    private static int Address(Expression value) => LocalNumber(value.Instruction, OpCodes.Ldloca, OpCodes.Ldloca_S);
    private static int Local(Expression value, bool allowAddress)
    {
        var code = value.Instruction.opcode;
        if (code == OpCodes.Ldloc_0) return 0;
        if (code == OpCodes.Ldloc_1) return 1;
        if (code == OpCodes.Ldloc_2) return 2;
        if (code == OpCodes.Ldloc_3) return 3;
        int local = LocalNumber(value.Instruction, OpCodes.Ldloc, OpCodes.Ldloc_S);
        return local >= 0 || !allowAddress ? local : Address(value);
    }
    private static int Stored(CodeInstruction instruction)
    {
        if (instruction.opcode == OpCodes.Stloc_0) return 0;
        if (instruction.opcode == OpCodes.Stloc_1) return 1;
        if (instruction.opcode == OpCodes.Stloc_2) return 2;
        if (instruction.opcode == OpCodes.Stloc_3) return 3;
        return LocalNumber(instruction, OpCodes.Stloc, OpCodes.Stloc_S);
    }
    private static int LocalNumber(CodeInstruction instruction, OpCode full, OpCode small) =>
        instruction.opcode != full && instruction.opcode != small ? -1 :
        instruction.operand is LocalBuilder local ? local.LocalIndex : Convert.ToInt32(instruction.operand);
    private static int Previous(IReadOnlyList<CodeInstruction> body, int index)
    {
        do { index--; } while (index >= 0 && body[index].opcode == OpCodes.Nop);
        if (index < 0) throw new InvalidOperationException("A native argument producer is missing.");
        return index;
    }
    private static int Next(IReadOnlyList<CodeInstruction> body, int index)
    {
        do { index++; } while (index < body.Count && body[index].opcode == OpCodes.Nop);
        if (index == body.Count) throw new InvalidOperationException("A native argument destination is missing.");
        return index;
    }

    private sealed record Expression(int Index, CodeInstruction Instruction, Expression[] Inputs)
    {
        internal int Start => Inputs.Length == 0 ? Index : Inputs[0].Start;
        internal static Expression Read(IReadOnlyList<CodeInstruction> body, int index, int depth = 0)
        {
            if (depth > 128) throw new InvalidOperationException("A native argument exceeds the bounded expression depth.");
            var instruction = body[index];
            int count;
            if (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt || instruction.opcode == OpCodes.Newobj)
            {
                if (instruction.operand is not MethodBase method) throw new InvalidOperationException("An argument callable is unreadable.");
                count = method.GetParameters().Length + (instruction.opcode == OpCodes.Newobj || method.IsStatic ? 0 : 1);
            }
            else
            {
                if (instruction.opcode.StackBehaviourPush is not (StackBehaviour.Push1 or StackBehaviour.Pushi or
                    StackBehaviour.Pushi8 or StackBehaviour.Pushr4 or StackBehaviour.Pushr8 or StackBehaviour.Pushref))
                    throw new InvalidOperationException("A native argument crosses an unsupported stack operation.");
                count = instruction.opcode.StackBehaviourPop switch
                {
                    StackBehaviour.Pop0 => 0,
                    StackBehaviour.Pop1 or StackBehaviour.Popi or StackBehaviour.Popref => 1,
                    StackBehaviour.Pop1_pop1 or StackBehaviour.Popi_pop1 or StackBehaviour.Popi_popi or
                        StackBehaviour.Popi_popi8 or StackBehaviour.Popi_popr4 or StackBehaviour.Popi_popr8 or
                        StackBehaviour.Popref_pop1 or StackBehaviour.Popref_popi => 2,
                    _ => throw new InvalidOperationException("A native argument has unsupported stack inputs.")
                };
            }
            var inputs = new Expression[count];
            int cursor = index;
            for (int i = count - 1; i >= 0; i--)
            {
                inputs[i] = Read(body, Previous(body, cursor), depth + 1);
                cursor = inputs[i].Start;
            }
            return new(index, instruction, inputs);
        }
        internal IEnumerable<int> Operations => Inputs.SelectMany(input => input.Operations).Append(Index);
    }

    private sealed class Graph
    {
        private readonly IReadOnlyList<CodeInstruction> body;
        private readonly List<int>[] predecessors;
        private readonly HashSet<int> destinations = new();
        internal Graph(IReadOnlyList<CodeInstruction> body)
        {
            this.body = body;
            predecessors = Enumerable.Range(0, body.Count).Select(_ => new List<int>()).ToArray();
            if (body.Any(instruction => instruction.blocks.Count != 0 || instruction.opcode == OpCodes.Jmp))
                throw new InvalidOperationException("Native geometry has an unsupported exception or tail-transfer graph.");
            var labels = new Dictionary<Label, int>();
            for (int i = 0; i < body.Count; i++)
                foreach (var label in body[i].labels)
                    if (!labels.TryAdd(label, i)) throw new InvalidOperationException("Native geometry has duplicate branch labels.");
            for (int i = 0; i < body.Count; i++)
            {
                var instruction = body[i];
                if (instruction.opcode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch)
                {
                    var branches = instruction.operand is Label label ? new[] { label } : instruction.operand as Label[];
                    if (branches == null) throw new InvalidOperationException("A native geometry branch is unreadable.");
                    foreach (var branch in branches)
                    {
                        if (!labels.TryGetValue(branch, out int target)) throw new InvalidOperationException("A native geometry branch has no destination.");
                        predecessors[target].Add(i); destinations.Add(target);
                    }
                    if (instruction.opcode.FlowControl == FlowControl.Branch) continue;
                }
                if (i + 1 < body.Count && instruction.opcode.FlowControl is not (FlowControl.Return or FlowControl.Throw))
                    predecessors[i + 1].Add(i);
            }
            // An inserted NOP after an unconditional branch has no executable incoming
            // path. Its syntactic fallthrough must not manufacture a producer-bypass path.
            var successors = Enumerable.Range(0, body.Count).Select(_ => new List<int>()).ToArray();
            for (int target = 0; target < body.Count; target++)
                foreach (int source in predecessors[target]) successors[source].Add(target);
            var reachable = new HashSet<int>();
            var pending = new Stack<int>();
            pending.Push(0);
            while (pending.Count != 0)
            {
                int index = pending.Pop();
                if (!reachable.Add(index)) continue;
                foreach (int next in successors[index]) pending.Push(next);
            }
            foreach (var incoming in predecessors) incoming.RemoveAll(index => !reachable.Contains(index));
        }
        internal void ProtectExpression(Expression expression, ISet<int> required)
        {
            if (Enumerable.Range(expression.Start + 1, expression.Index - expression.Start).Any(destinations.Contains))
                throw new InvalidOperationException("A branch enters the middle of a native geometry expression.");
            foreach (int index in expression.Operations) required.Add(index);
        }
        internal void ProtectProducer(Expression expression, int write, ISet<int> required)
        {
            ProtectExpression(expression, required);
            // A jump into the store (including a labeled intervening NOP) can substitute
            // another stack value even when the producer expression itself is unchanged.
            for (int index = expression.Index + 1; index <= write; index++)
            {
                if (destinations.Contains(index)) throw new InvalidOperationException("A branch bypasses a native geometry producer before its store.");
                required.Add(index);
            }
        }
        internal bool EscapesAddress(IReadOnlyCollection<int> locals, int constructorAddress, Type rectangleType)
        {
            for (int index = 0; index < body.Count; index++)
            {
                int local = LocalNumber(body[index], OpCodes.Ldloca, OpCodes.Ldloca_S);
                if (local < 0 || !locals.Contains(local) || index == constructorAddress) continue;
                var next = body[Next(body, index)];
                if (next.opcode == OpCodes.Ldfld) continue;
                // Native rectangle accessors consume the address immediately and cannot
                // return a managed pointer. UnchangedSince still treats them as possible
                // writes between production and consumption; later weapon reads are fine.
                if (next.opcode == OpCodes.Call && next.operand is MethodInfo method &&
                    method.DeclaringType == rectangleType && !method.IsStatic && method.IsSpecialName &&
                    method.Name.StartsWith("get_", StringComparison.Ordinal) && method.ReturnType == typeof(float) &&
                    method.GetParameters().Length == 0) continue;
                return true;
            }
            return false;
        }
        internal bool UnchangedSince(int origin, int use, int local, int constructorWrite)
        {
            var seen = new HashSet<int>();
            var pending = new Stack<int>(predecessors[use]);
            bool reached = false;
            while (pending.Count != 0)
            {
                int index = pending.Pop();
                if (index == origin) { reached = true; continue; }
                if (!seen.Add(index)) continue;
                var instruction = body[index];
                if (Stored(instruction) == local || index == constructorWrite) return false;
                if (LocalNumber(instruction, OpCodes.Ldloca, OpCodes.Ldloca_S) == local)
                {
                    // A direct field read does not expose the address to another callable.
                    int next = Next(body, index);
                    if (body[next].opcode != OpCodes.Ldfld) return false;
                }
                if (predecessors[index].Count == 0) return false;
                foreach (int predecessor in predecessors[index]) pending.Push(predecessor);
            }
            return reached;
        }
    }
}
