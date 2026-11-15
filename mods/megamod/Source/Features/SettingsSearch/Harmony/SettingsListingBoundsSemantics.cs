using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Megamod.Features.SettingsSearch.Harmony;

// Original/composed instruction semantics only. Publication identity and native call-edge
// verification belong to the reader that consumes this proof; this class acquires no patches.
public sealed class SettingsListingBoundsSemantics
{
    public sealed record Draw(MethodInfo Method, int RectangleParameter = 0);
    public sealed record Bindings(MethodInfo BoundsGetter, MethodInfo HasValue, MethodInfo Value,
        MethodInfo Overlaps, IReadOnlyList<Draw> Draws);
    private readonly Bindings binding;
    public SettingsListingBoundsSemantics(Bindings binding) => this.binding = binding;

    public (ISet<int>? Required, string? Failure) Describe(MethodInfo consumer, IReadOnlyList<CodeInstruction> body)
    {
        try
        {
            if (consumer.IsStatic || consumer.DeclaringType == null || body.Count == 0 || body.Count > 4096 ||
                body.Any(instruction => instruction.blocks.Count != 0)) return Refuse("The listing consumer body is unavailable or outside the bounded proof.");
            Type? rectangle = Nullable.GetUnderlyingType(binding.BoundsGetter.ReturnType);
            if (rectangle == null || !rectangle.IsValueType || binding.BoundsGetter.IsStatic ||
                binding.BoundsGetter.GetParameters().Length != 0 || binding.BoundsGetter.DeclaringType != consumer.DeclaringType ||
                binding.HasValue.DeclaringType != binding.BoundsGetter.ReturnType || binding.HasValue.ReturnType != typeof(bool) ||
                binding.HasValue.GetParameters().Length != 0 || binding.Value.DeclaringType != binding.BoundsGetter.ReturnType ||
                binding.Value.ReturnType != rectangle || binding.Value.GetParameters().Length != 0 ||
                binding.Overlaps.IsStatic || binding.Overlaps.DeclaringType != rectangle || binding.Overlaps.ReturnType != typeof(bool) ||
                !binding.Overlaps.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(new[] { rectangle }) ||
                binding.Draws.Count == 0 || binding.Draws.Count > 8 ||
                binding.Draws.Select(draw => draw.Method).Distinct().Count() != binding.Draws.Count ||
                binding.Draws.Any(draw => !draw.Method.IsStatic || draw.RectangleParameter < 0 ||
                    draw.RectangleParameter >= draw.Method.GetParameters().Length ||
                    draw.Method.GetParameters()[draw.RectangleParameter].ParameterType != rectangle))
                return Refuse("The exact listing bounds and drawing callables are unproved.");

            var significant = Enumerable.Range(0, body.Count).Where(index => body[index].opcode != OpCodes.Nop).ToArray();
            var code = significant.Select(index => body[index]).ToArray();
            var labels = new Dictionary<Label, int>();
            for (int index = 0; index < body.Count; index++)
            {
                if (body[index].labels.Count > 4096 || labels.Count + body[index].labels.Count > 8192)
                    return Refuse("Listing branch metadata exceeds the bounded proof.");
                foreach (var label in body[index].labels)
                    labels.Add(label, Array.FindIndex(significant, meaningful => meaningful >= index));
            }
            int Target(int index) => code[index].operand is Label label && labels.TryGetValue(label, out int target) ? target : -1;
            int Single(MethodInfo method) => Enumerable.Range(0, code.Length).Where(index => Call(code[index], method)).Single();
            int[] getters = Enumerable.Range(0, code.Length).Where(index => Call(code[index], binding.BoundsGetter)).ToArray();
            if (getters.Length != 2) return Refuse("The listing bounds producers are missing or ambiguous.");
            int first = getters[0] - 1, second = getters[1] - 2;
            int firstBranch = first + 5, secondBranch = second + 7;
            if (first < 0 || second < 0 || secondBranch >= code.Length ||
                !Argument(code[first], 0) || !code[first + 2].IsStloc() || !LocalAddress(code[first + 3], code[first + 2].LocalIndex()) ||
                !Call(code[first + 4], binding.HasValue) || !Conditional(code[firstBranch]) ||
                !Argument(code[second + 1], 0) || !code[second + 3].IsStloc() ||
                !LocalAddress(code[second + 4], code[second + 3].LocalIndex()) ||
                !Call(code[second + 5], binding.Value) || !Call(code[second + 6], binding.Overlaps) ||
                !Conditional(code[secondBranch]))
                return Refuse("The listing nullable producer/consumer chain is unproved.");
            int TrueEdge(int index) => IsTrue(code[index]) ? Target(index) : index + 1;
            int FalseEdge(int index) => IsTrue(code[index]) ? index + 1 : Target(index);
            int drawStart = FalseEdge(firstBranch);
            if (TrueEdge(firstBranch) != second || TrueEdge(secondBranch) != drawStart || drawStart < 0)
                return Refuse("Null and successful overlap do not enter the same drawing continuation.");
            if (Single(binding.HasValue) != first + 4 || Single(binding.Value) != second + 5 || Single(binding.Overlaps) != second + 6)
                return Refuse("The exact nullable and overlap operations are ambiguous.");
            var drawCalls = new Dictionary<int, int>();
            foreach (var draw in binding.Draws)
            {
                int call = Single(draw.Method);
                int cursor = call - 1;
                for (int argument = draw.Method.GetParameters().Length - 1; argument > draw.RectangleParameter; argument--)
                    cursor = SkipExpression(code, cursor);
                if (cursor < 0 || !SameValueAndAddress(code[cursor], code[second]))
                    return Refuse("The native row rectangle does not reach the bound drawing operation.");
                drawCalls.Add(call, cursor);
            }
            var continuation = DrawingContinuation(drawStart, code, Target, drawCalls.Keys.ToHashSet());
            if (continuation == null)
                return Refuse("A drawing continuation exits, cycles or omits a required native drawing branch.");
            if (!CanReach(0, first, code, Target, new HashSet<int>()))
                return Refuse("The native row rectangle does not reach the bound drawing operation.");
            // A draw is not the end of the method. In particular, a later backedge must not
            // reenter Value/Overlaps after an earlier null branch already drew the row.
            var nullPath = Reachable(drawStart, code, Target, new HashSet<int>(), firstBranch, drawStart);
            if (nullPath.Contains(second + 5) || nullPath.Contains(second + 6))
                return Refuse("A null path reaches nullable Value or overlap after drawing.");
            var reachesDraw = PredecessorsOf(drawCalls.Keys, code, Target, new HashSet<int>());
            var interval = Reachable(drawStart, code, Target, new HashSet<int> { first });
            interval.IntersectWith(PredecessorsOf(drawCalls.Keys, code, Target, new HashSet<int> { first }));

            var expressions = new List<(int Start, int End)> { (first, firstBranch), (second, secondBranch) };
            expressions.AddRange(drawCalls.Select(draw => (draw.Value, draw.Key)));
            var required = Enumerable.Range(first, 6).Concat(Enumerable.Range(second, 8)).Concat(continuation).ToHashSet();
            var nullableLocals = new HashSet<int> { code[first + 2].LocalIndex(), code[second + 3].LocalIndex() };
            var rowAliases = RowAliases(code, code[second], code.Length - 1);
            for (int index = 0; index < code.Length; index++)
            {
                if (code[index].operand is Label)
                {
                    int destination = Target(index);
                    if (destination < 0 || expressions.Any(expression => destination > expression.Start && destination <= expression.End))
                        return Refuse("An incoming branch bypasses a protected listing producer.");
                    required.Add(index);
                }
                if (code[index].operand is Label[]) return Refuse("A switch enters unproved listing control flow.");
                // Physical IL order is not execution order: a tail block can jump backwards
                // into an earlier draw. Audit every predecessor of a draw, not a source prefix.
                if (!reachesDraw.Contains(index)) continue;
                if ((code[index].opcode == OpCodes.Starg || code[index].opcode == OpCodes.Starg_S) &&
                    code[index].ArgumentIndex() == 0) return Refuse("The listing receiver is replaced before drawing.");
                if (IsArgumentAddress(code[index]) && code[index].ArgumentIndex() == 0)
                    return Refuse("The listing receiver address escapes before drawing.");
                bool inNullableChain = index >= first && index <= firstBranch || index >= second && index <= secondBranch;
                if (!inNullableChain && (code[index].IsStloc() || IsLocalAddress(code[index])) &&
                    nullableLocals.Contains(code[index].LocalIndex()))
                    return Refuse("The listing nullable local can be overwritten or escape its consumed chain.");
                if (RowAddress(code[index], code[second], rowAliases) && index != second)
                {
                    int next = index + 1;
                    if (next >= code.Length) return Refuse("A row address escapes the listing body.");
                    bool copyAlias = code[next].IsStloc() && rowAliases.Contains(code[next].LocalIndex());
                    bool initialClear = !interval.Contains(index) && code[next].opcode == OpCodes.Initobj && Equals(code[next].operand, rectangle);
                    bool fieldRead = code[next].opcode == OpCodes.Ldfld && code[next].operand is FieldInfo field &&
                        !field.IsStatic && field.DeclaringType == rectangle && !field.FieldType.IsByRef;
                    if (!copyAlias && !initialClear && !fieldRead && !interval.Contains(index))
                    {
                        // Roslyn can retain a ref local on the evaluation stack across the later
                        // GetRow/store, then consume it with a plain field read. Track that one
                        // address below unrelated stack work; do not call every prior alias an escape.
                        int read = RetainedAddressFieldRead(code, index, code.Length, rectangle);
                        if (read >= 0)
                        {
                            fieldRead = true;
                            foreach (int part in Enumerable.Range(index, read - index + 1)) required.Add(part);
                            expressions.Add((index, read));
                        }
                    }
                    if (!copyAlias && !initialClear && !fieldRead)
                        return Refuse("The row rectangle address can escape or change after acquisition.");
                    required.Add(index); required.Add(next);
                }
                if (interval.Contains(index) && SameStore(code[index], code[second]))
                    return Refuse("The supplied row rectangle is overwritten before drawing.");
            }
            if (Enumerable.Range(0, code.Length).Where(index => code[index].operand is Label)
                .Any(index => expressions.Any(expression => Target(index) > expression.Start && Target(index) <= expression.End)))
                return Refuse("An incoming branch bypasses a protected row-address lifetime.");
            if (drawCalls.Keys.Any(draw => CanReach(FalseEdge(secondBranch), draw, code, Target, new HashSet<int>()) ||
                CanReach(0, draw, code, Target, new HashSet<int> { first })) ||
                CanReach(0, second, code, Target, new HashSet<int> { firstBranch }))
                return Refuse("A reachable path bypasses or reverses the native culling decision.");
            return (required.Select(index => significant[index]).ToHashSet(), null);
        }
        catch (Exception)
        {
            return Refuse("The listing semantic slice is missing, malformed or ambiguous.");
        }
    }

    private static (ISet<int>?, string?) Refuse(string reason) => (null, reason);
    private static bool Call(CodeInstruction instruction, MethodInfo method) =>
        (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) && Equals(instruction.operand, method);
    private static bool Argument(CodeInstruction instruction, int argument) => instruction.IsLdarg() && instruction.ArgumentIndex() == argument;
    private static bool IsLocalAddress(CodeInstruction instruction) => instruction.opcode == OpCodes.Ldloca || instruction.opcode == OpCodes.Ldloca_S;
    private static bool IsArgumentAddress(CodeInstruction instruction) => instruction.opcode == OpCodes.Ldarga || instruction.opcode == OpCodes.Ldarga_S;
    private static bool LocalAddress(CodeInstruction instruction, int local) => IsLocalAddress(instruction) && instruction.LocalIndex() == local;
    private static bool IsTrue(CodeInstruction instruction) => instruction.opcode == OpCodes.Brtrue || instruction.opcode == OpCodes.Brtrue_S;
    private static bool Conditional(CodeInstruction instruction) => IsTrue(instruction) || instruction.opcode == OpCodes.Brfalse || instruction.opcode == OpCodes.Brfalse_S;
    private static bool SameValueAndAddress(CodeInstruction value, CodeInstruction address) =>
        value.IsLdloc() && IsLocalAddress(address) && value.LocalIndex() == address.LocalIndex() ||
        value.IsLdarg() && IsArgumentAddress(address) && value.ArgumentIndex() == address.ArgumentIndex();
    private static bool SameAddress(CodeInstruction first, CodeInstruction second) =>
        IsLocalAddress(first) && IsLocalAddress(second) && first.LocalIndex() == second.LocalIndex() ||
        IsArgumentAddress(first) && IsArgumentAddress(second) && first.ArgumentIndex() == second.ArgumentIndex();
    private static bool SameStore(CodeInstruction store, CodeInstruction address) =>
        store.IsStloc() && IsLocalAddress(address) && store.LocalIndex() == address.LocalIndex() ||
        (store.opcode == OpCodes.Starg || store.opcode == OpCodes.Starg_S) && IsArgumentAddress(address) && store.ArgumentIndex() == address.ArgumentIndex();

    private static bool RowAddress(CodeInstruction instruction, CodeInstruction row, ISet<int> aliases) =>
        SameAddress(instruction, row) || instruction.IsLdloc() && aliases.Contains(instruction.LocalIndex());

    private static HashSet<int> RowAliases(IReadOnlyList<CodeInstruction> body, CodeInstruction row, int last)
    {
        var aliases = new HashSet<int>();
        bool changed;
        do
        {
            changed = false;
            for (int index = 0; index < last; index++)
                if (RowAddress(body[index], row, aliases) && body[index + 1].IsStloc())
                    changed |= aliases.Add(body[index + 1].LocalIndex());
        } while (changed);
        return aliases;
    }

    private static int RetainedAddressFieldRead(IReadOnlyList<CodeInstruction> body, int origin, int limit, Type rectangle)
    {
        int depth = 1;
        for (int index = origin + 1; index < limit; index++)
        {
            var instruction = body[index];
            if (instruction.opcode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch or FlowControl.Return or FlowControl.Throw)
                return -1;
            var (pop, push) = StackEffect(instruction);
            if (pop >= depth)
                return pop == depth && depth == 1 && instruction.opcode == OpCodes.Ldfld && instruction.operand is FieldInfo field &&
                    !field.IsStatic && field.DeclaringType == rectangle && !field.FieldType.IsByRef ? index : -1;
            depth = depth - pop + push;
        }
        return -1;
    }

    // Skip one straight-line argument expression backwards. This tracks evaluation-stack shape,
    // not arbitrary values; the consumed rectangle itself must still be the exact row load.
    private static int SkipExpression(IReadOnlyList<CodeInstruction> body, int cursor)
    {
        int needed = 1;
        while (needed > 0)
        {
            if (cursor < 0) throw new InvalidOperationException("Missing draw argument producer.");
            var instruction = body[cursor--];
            if (instruction.opcode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch or FlowControl.Return or FlowControl.Throw)
                throw new InvalidOperationException("A draw argument has unproved control flow.");
            var (pop, push) = StackEffect(instruction);
            needed = needed - push + pop;
            if (needed < 0) throw new InvalidOperationException("A draw argument shares an unproved stack producer.");
        }
        return cursor;
    }

    private static (int Pop, int Push) StackEffect(CodeInstruction instruction)
    {
        if (instruction.operand is MethodBase method && instruction.opcode.FlowControl == FlowControl.Call)
            return (method.GetParameters().Length + (instruction.opcode == OpCodes.Newobj || method.IsStatic ? 0 : 1),
                instruction.opcode == OpCodes.Newobj || method is MethodInfo info && info.ReturnType != typeof(void) ? 1 : 0);
        int pop = instruction.opcode.StackBehaviourPop switch
        {
            StackBehaviour.Pop0 => 0,
            StackBehaviour.Pop1 or StackBehaviour.Popi or StackBehaviour.Popref => 1,
            StackBehaviour.Pop1_pop1 or StackBehaviour.Popi_pop1 or StackBehaviour.Popi_popi or StackBehaviour.Popi_popi8 or
                StackBehaviour.Popi_popr4 or StackBehaviour.Popi_popr8 or StackBehaviour.Popref_pop1 or StackBehaviour.Popref_popi => 2,
            _ => throw new InvalidOperationException("Unproved draw argument pop shape.")
        };
        int push = instruction.opcode.StackBehaviourPush switch
        {
            StackBehaviour.Push0 => 0,
            StackBehaviour.Push1 or StackBehaviour.Pushi or StackBehaviour.Pushi8 or StackBehaviour.Pushr4 or
                StackBehaviour.Pushr8 or StackBehaviour.Pushref => 1,
            _ => throw new InvalidOperationException("Unproved draw argument push shape.")
        };
        return (pop, push);
    }

    // All ordinary paths from the shared continuation must reach a supplied draw before exit.
    // Draws are absorbing here: sequential distinct draws need a separate contract, not a union.
    private static HashSet<int>? DrawingContinuation(int start, IReadOnlyList<CodeInstruction> body,
        Func<int, int> target, ISet<int> draws)
    {
        var reached = new HashSet<int>();
        var predecessors = new Dictionary<int, List<int>>();
        var remaining = new Dictionary<int, int>();
        var pending = new Queue<int>(); pending.Enqueue(start);
        while (pending.Count != 0)
        {
            int index = pending.Dequeue();
            if (index < 0 || index >= body.Count) return null;
            if (!reached.Add(index) || draws.Contains(index)) continue;
            var flow = body[index].opcode.FlowControl;
            if (flow is FlowControl.Return or FlowControl.Throw) return null;
            var successors = new HashSet<int>();
            if (flow is FlowControl.Branch or FlowControl.Cond_Branch) successors.Add(target(index));
            if (flow != FlowControl.Branch) successors.Add(index + 1);
            remaining[index] = successors.Count;
            foreach (int next in successors)
            {
                if (!predecessors.TryGetValue(next, out var previous)) predecessors.Add(next, previous = new());
                previous.Add(index); pending.Enqueue(next);
            }
        }
        if (!draws.All(reached.Contains)) return null;
        var proved = new HashSet<int>();
        foreach (int draw in draws) { proved.Add(draw); pending.Enqueue(draw); }
        while (pending.Count != 0)
        {
            int index = pending.Dequeue();
            if (!predecessors.TryGetValue(index, out var previous)) continue;
            foreach (int prior in previous)
                if (--remaining[prior] == 0 && proved.Add(prior)) pending.Enqueue(prior);
        }
        return proved.Contains(start) ? reached : null;
    }

    private static bool CanReach(int start, int goal, IReadOnlyList<CodeInstruction> body, Func<int, int> target, ISet<int> barriers)
        => Reachable(start, body, target, barriers).Contains(goal);

    private static HashSet<int> Reachable(int start, IReadOnlyList<CodeInstruction> body, Func<int, int> target,
        ISet<int> barriers, int forcedBranch = -1, int forcedTarget = -1)
    {
        var pending = new Stack<int>();
        var visited = new HashSet<int>();
        pending.Push(start);
        while (pending.Count != 0)
        {
            int index = pending.Pop();
            if (index < 0 || index >= body.Count) throw new InvalidOperationException("Invalid listing branch target.");
            if (barriers.Contains(index) || !visited.Add(index)) continue;
            if (index == forcedBranch) { pending.Push(forcedTarget); continue; }
            var flow = body[index].opcode.FlowControl;
            if (flow == FlowControl.Return || flow == FlowControl.Throw) continue;
            if (flow == FlowControl.Branch || flow == FlowControl.Cond_Branch) pending.Push(target(index));
            if (flow != FlowControl.Branch) pending.Push(index + 1);
        }
        return visited;
    }

    private static HashSet<int> PredecessorsOf(IEnumerable<int> goals, IReadOnlyList<CodeInstruction> body,
        Func<int, int> target, ISet<int> barriers)
    {
        var predecessors = new Dictionary<int, List<int>>();
        for (int index = 0; index < body.Count; index++)
        {
            if (barriers.Contains(index)) continue;
            var flow = body[index].opcode.FlowControl;
            if (flow is FlowControl.Return or FlowControl.Throw) continue;
            void Edge(int next)
            {
                if (next < 0 || next >= body.Count) throw new InvalidOperationException("Invalid listing branch target.");
                if (barriers.Contains(next)) return;
                if (!predecessors.TryGetValue(next, out var incoming)) predecessors.Add(next, incoming = new());
                incoming.Add(index);
            }
            if (flow is FlowControl.Branch or FlowControl.Cond_Branch) Edge(target(index));
            if (flow != FlowControl.Branch) Edge(index + 1);
        }
        var reached = new HashSet<int>();
        var pending = new Queue<int>(goals);
        while (pending.Count != 0)
        {
            int index = pending.Dequeue();
            if (barriers.Contains(index) || !reached.Add(index)) continue;
            if (predecessors.TryGetValue(index, out var incoming))
                foreach (int prior in incoming) pending.Enqueue(prior);
        }
        return reached;
    }
}
