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
    public sealed record ProjectionScalar(int ConsumerParameter, bool OneMinus = false);
    public sealed record RectangleAssignment(MethodInfo Setter, float Value);
    public sealed record RectangleProjection(MethodInfo Method, ProjectionScalar? Scalar = null,
        RectangleAssignment? Assignment = null);
    public sealed record Draw(MethodInfo Method, int RectangleParameter = 0, int Sequence = 0,
        RectangleProjection? Projection = null, IReadOnlyList<int>? ConsumerGenericArguments = null);
    public sealed record Bindings(MethodInfo BoundsGetter, MethodInfo HasValue, MethodInfo Value,
        MethodInfo Overlaps, IReadOnlyList<Draw> Draws, IReadOnlyList<MethodInfo>? RectangleAccessors = null,
        IReadOnlyList<ConstructorInfo>? RectangleConstructors = null);
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
                binding.Draws.Any(draw => !draw.Method.IsStatic || draw.RectangleParameter < 0 ||
                    draw.RectangleParameter >= draw.Method.GetParameters().Length ||
                    draw.Method.GetParameters()[draw.RectangleParameter].ParameterType != rectangle || draw.Sequence < 0 || draw.Sequence >= 8))
                return Refuse("The exact listing bounds and drawing callables are unproved.");
            var accessors = (binding.RectangleAccessors ?? Array.Empty<MethodInfo>()).ToHashSet();
            if (accessors.Count > 32 || accessors.Any(method => !RectangleAccessor(method, rectangle)))
                return Refuse("The rectangle preparation callables are unproved.");
            var constructors = (binding.RectangleConstructors ?? Array.Empty<ConstructorInfo>()).ToHashSet();
            if (constructors.Count > 4 || constructors.Any(constructor => constructor.IsStatic || constructor.DeclaringType != rectangle ||
                !constructor.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(Enumerable.Repeat(typeof(float), 4))))
                return Refuse("The native rectangle constructors are unproved.");
            var sequences = binding.Draws.Select(draw => draw.Sequence).Distinct().OrderBy(value => value).ToArray();
            if (!sequences.SequenceEqual(Enumerable.Range(0, sequences.Length)))
                return Refuse("The required drawing sequence has a missing step.");
            var methods = binding.Draws.Select(draw => ResolveDraw(draw, consumer)).ToArray();

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
            var proofs = new List<DrawProof>();
            for (int drawIndex = 0; drawIndex < binding.Draws.Count; drawIndex++)
            {
                Draw draw = binding.Draws[drawIndex];
                var matches = Enumerable.Range(0, code.Length).Where(index => Call(code[index], methods[drawIndex]))
                    .Select(index => MatchDraw(draw, index, consumer, code, code[second], rectangle, accessors))
                    .Where(proof => proof != null).Cast<DrawProof>().ToArray();
                if (matches.Length != 1 || proofs.Any(proof => proof.Call == matches[0].Call))
                    return Refuse("The native row rectangle does not reach the bound drawing operation.");
                proofs.Add(matches[0]);
            }
            var drawCalls = proofs.ToDictionary(proof => proof.Call, proof => proof.Draw.Sequence);
            if (Enumerable.Range(0, code.Length).Any(index => methods.Any(method => Call(code[index], method)) && !drawCalls.ContainsKey(index)))
                return Refuse("An undeclared call changes the required drawing sequence.");
            var continuation = DrawingContinuation(drawStart, code, Target, drawCalls, sequences.Length);
            if (continuation == null)
                return Refuse("A drawing continuation exits, cycles or omits a required native drawing branch.");
            foreach (var proof in proofs.Where(proof => proof.Draw.Sequence == sequences.Length - 1))
                if (Reachable(proof.Call + 1, code, Target, new HashSet<int> { first }).Any(drawCalls.ContainsKey))
                    return Refuse("A completed drawing sequence repeats before a fresh culling gate.");
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
            expressions.AddRange(proofs.Select(proof => (proof.LoadStart, proof.Call)));
            expressions.AddRange(proofs.Where(proof => proof.Draw.Projection != null).Select(proof => (proof.ProducerStart, proof.ProducerEnd)));
            var required = Enumerable.Range(first, 6).Concat(Enumerable.Range(second, 8)).Concat(continuation).ToHashSet();
            foreach (var proof in proofs.Where(proof => proof.Draw.Projection != null))
            {
                if (CanReach(0, proof.Call, code, Target, new HashSet<int> { proof.ProducerStart }))
                    return Refuse("A path bypasses the required rectangle projection producer.");
                foreach (int part in Enumerable.Range(proof.ProducerStart, proof.ProducerEnd - proof.ProducerStart + 1)) required.Add(part);
                // A projection captured before culling must still describe the row that is culled.
                var beforeGate = Reachable(proof.ProducerEnd + 1, code, Target, new HashSet<int> { first });
                beforeGate.IntersectWith(PredecessorsOf(new[] { first }, code, Target, new HashSet<int>()));
                interval.UnionWith(beforeGate);
            }
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
                    int accessor = AddressAccessor(code, index, accessors, constructors);
                    bool knownAccessor = accessor >= 0 && (code[accessor].operand is MethodInfo getterMethod && IsRead(getterMethod) ||
                        !interval.Contains(index) && !interval.Contains(accessor));
                    if (knownAccessor)
                    {
                        expressions.Add((index, accessor));
                        foreach (int part in Enumerable.Range(index, accessor - index + 1)) required.Add(part);
                    }
                    if (!copyAlias && !initialClear && !fieldRead && !knownAccessor && !interval.Contains(index))
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
                    if (!copyAlias && !initialClear && !fieldRead && !knownAccessor)
                        return Refuse("The row rectangle address can escape or change after acquisition.");
                    required.Add(index); required.Add(next);
                }
                if (interval.Contains(index) && SameStore(code[index], code[second]))
                    return Refuse("The supplied row rectangle is overwritten before drawing.");
            }
            foreach (var proof in proofs.Where(proof => proof.Local.HasValue))
            {
                int local = proof.Local!.Value;
                var address = new CodeInstruction(OpCodes.Ldloca, local);
                var aliases = RowAliases(code, address, code.Length - 1);
                var lifetime = Reachable(proof.ProducerEnd + 1, code, Target, new HashSet<int> { proof.ProducerStart });
                lifetime.IntersectWith(PredecessorsOf(new[] { proof.Call }, code, Target, new HashSet<int> { proof.ProducerStart }));
                var predecessors = PredecessorsOf(new[] { proof.Call }, code, Target, new HashSet<int>());
                for (int index = 0; index < code.Length; index++)
                {
                    if (!predecessors.Contains(index)) continue;
                    if (lifetime.Contains(index) && code[index].IsStloc() && code[index].LocalIndex() == local)
                        return Refuse("A derived rectangle is replaced before its drawing operation.");
                    if (!RowAddress(code[index], address, aliases)) continue;
                    if (index == proof.AssignmentAddress) { required.Add(index); continue; }
                    bool alias = index + 1 < code.Length && code[index + 1].IsStloc() && aliases.Contains(code[index + 1].LocalIndex());
                    bool clear = !lifetime.Contains(index) && index + 1 < code.Length && code[index + 1].opcode == OpCodes.Initobj && Equals(code[index + 1].operand, rectangle);
                    bool field = index + 1 < code.Length && code[index + 1].opcode == OpCodes.Ldfld && code[index + 1].operand is FieldInfo member &&
                        member.DeclaringType == rectangle && !member.IsStatic && !member.FieldType.IsByRef;
                    int accessor = AddressAccessor(code, index, accessors);
                    bool read = accessor >= 0 && IsRead((MethodInfo)code[accessor].operand);
                    if (!alias && !clear && !field && !read)
                        return Refuse("A derived rectangle address can escape or change before drawing.");
                    if (read)
                    {
                        expressions.Add((index, accessor));
                        foreach (int part in Enumerable.Range(index, accessor - index + 1)) required.Add(part);
                    }
                    required.Add(index);
                }
            }
            foreach (var group in proofs.Where(proof => proof.Draw.Projection?.Scalar != null)
                .GroupBy(proof => proof.Draw.Projection!.Scalar!.ConsumerParameter + 1))
            {
                int argument = group.Key;
                int[] reads = group.Select(proof => proof.ScalarRead).Distinct().ToArray();
                var scalarInterval = new HashSet<int>();
                foreach (int read in reads)
                {
                    // A completed row ends this consumption cycle. A later pass may change
                    // the scalar and rebuild every projection; a getter alone is not a safe
                    // boundary because stored and inline projections can straddle that gate.
                    var barriers = proofs.Where(proof => proof.Draw.Sequence == sequences.Length - 1)
                        .Select(proof => proof.Call).ToHashSet();
                    barriers.Add(read);
                    var paths = Reachable(read + 1, code, Target, barriers);
                    paths.IntersectWith(PredecessorsOf(reads.Where(other => other != read), code, Target, barriers));
                    scalarInterval.UnionWith(paths);
                }
                var address = new CodeInstruction(OpCodes.Ldarga, argument);
                var aliases = RowAliases(code, address, code.Length - 1);
                var predecessors = PredecessorsOf(reads, code, Target, new HashSet<int>());
                var writes = new HashSet<int>();
                for (int index = 0; index < code.Length; index++)
                {
                    if (SameStore(code[index], address))
                    {
                        writes.Add(index);
                        if (scalarInterval.Contains(index))
                            return Refuse("The shared projection scalar changes between its consumed reads.");
                    }
                    if (!predecessors.Contains(index) || !RowAddress(code[index], address, aliases)) continue;
                    if (index + 1 < code.Length && code[index + 1].IsStloc() && aliases.Contains(code[index + 1].LocalIndex()))
                    { required.Add(index); required.Add(index + 1); continue; }
                    var use = ScalarAddressUse(code, index);
                    if (use.End < 0 || use.Write && scalarInterval.Contains(use.End))
                        return Refuse("The shared projection scalar address can escape or change between reads.");
                    if (use.Write) writes.Add(use.End);
                    expressions.Add((index, use.End));
                    foreach (int part in Enumerable.Range(index, use.End - index + 1)) required.Add(part);
                }
                // Completion ends the current sequence, not the provenance of a stored rectangle.
                // A later peer read must not combine a changed scalar with a still-live old
                // projection. Only traversing that projection's intact producer refreshes it.
                foreach (var stored in group.Where(proof => proof.Local.HasValue))
                {
                    var refresh = new HashSet<int> { stored.ProducerStart };
                    var live = Reachable(stored.ScalarRead + 1, code, Target, refresh);
                    foreach (var peer in group.Where(proof => proof.ScalarRead != stored.ScalarRead))
                    {
                        if (!CanReach(peer.ScalarRead + 1, stored.Call, code, Target, refresh)) continue;
                        var beforePeer = PredecessorsOf(new[] { peer.ScalarRead }, code, Target, refresh);
                        if (writes.Any(write => live.Contains(write) && beforePeer.Contains(write)))
                            return Refuse("A later scalar read is combined with an unrefreshed stored projection.");
                    }
                }
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
    private sealed record DrawProof(Draw Draw, int Call, int LoadStart, int ProducerStart, int ProducerEnd,
        int? Local = null, int AssignmentAddress = -1, int ScalarRead = -1);

    private static MethodInfo ResolveDraw(Draw draw, MethodInfo consumer)
    {
        if (!draw.Method.IsGenericMethodDefinition)
        {
            if (draw.Method.IsGenericMethod || draw.ConsumerGenericArguments != null)
                throw new InvalidOperationException("A generic drawing call needs its definition and consumer mapping.");
            return draw.Method;
        }
        var mapping = draw.ConsumerGenericArguments;
        Type[] arguments = consumer.GetGenericArguments();
        if (!consumer.IsGenericMethod || mapping == null || mapping.Count != draw.Method.GetGenericArguments().Length ||
            mapping.Any(index => index < 0 || index >= arguments.Length))
            throw new InvalidOperationException("Missing drawing generic parameter relationship.");
        return draw.Method.MakeGenericMethod(mapping.Select(index => arguments[index]).ToArray());
    }

    private static bool IsRead(MethodInfo method) => method.ReturnType == typeof(float);
    private static bool RectangleAccessor(MethodInfo method, Type rectangle) =>
        !method.IsStatic && !method.IsGenericMethod && method.DeclaringType == rectangle && method.IsSpecialName &&
        (IsRead(method) && method.GetParameters().Length == 0 || method.ReturnType == typeof(void) &&
            method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(new[] { typeof(float) })) &&
        rectangle.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
            .Any(property => Equals(property.GetMethod, method) || Equals(property.SetMethod, method));

    private static DrawProof? MatchDraw(Draw draw, int call, MethodInfo consumer, IReadOnlyList<CodeInstruction> body,
        CodeInstruction row, Type rectangle, ISet<MethodInfo> accessors)
    {
        int cursor = call - 1;
        for (int argument = draw.Method.GetParameters().Length - 1; argument > draw.RectangleParameter; argument--)
            cursor = SkipExpression(body, cursor);
        if (cursor < 0) return null;
        if (draw.Projection == null)
            return SameValueAndAddress(body[cursor], row) ? new(draw, call, cursor, cursor, cursor) : null;
        RectangleProjection projection = draw.Projection;
        Type[] parameters = projection.Scalar == null ? new[] { rectangle } : new[] { rectangle, typeof(float) };
        if (!projection.Method.IsStatic || projection.Method.IsGenericMethod || projection.Method.ReturnType != rectangle ||
            !projection.Method.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(parameters)) return null;
        if (projection.Scalar != null && (projection.Scalar.ConsumerParameter < 0 ||
            projection.Scalar.ConsumerParameter >= consumer.GetParameters().Length ||
            consumer.GetParameters()[projection.Scalar.ConsumerParameter].ParameterType != typeof(float))) return null;
        if (projection.Assignment != null && (!accessors.Contains(projection.Assignment.Setter) || IsRead(projection.Assignment.Setter))) return null;

        int projectionCall = cursor;
        int? local = null;
        if (body[cursor].IsLdloc())
        {
            local = body[cursor].LocalIndex();
            var stores = Enumerable.Range(1, body.Count - 1).Where(index => body[index].IsStloc() && body[index].LocalIndex() == local &&
                Call(body[index - 1], projection.Method)).ToArray();
            if (stores.Length != 1) return null;
            projectionCall = stores[0] - 1;
        }
        if (!Call(body[projectionCall], projection.Method)) return null;
        int start = projectionCall - 1;
        int scalarRead = -1;
        if (projection.Scalar != null)
        {
            if (projection.Scalar.OneMinus)
            {
                if (start < 2 || body[start].opcode != OpCodes.Sub ||
                    !Argument(body[start - 1], projection.Scalar.ConsumerParameter + 1) || !SingleConstant(body[start - 2], 1f)) return null;
                scalarRead = start - 1;
                start -= 3;
            }
            else
            {
                if (start < 0 || !Argument(body[start], projection.Scalar.ConsumerParameter + 1)) return null;
                scalarRead = start;
                start--;
            }
        }
        if (start < 0 || !SameValueAndAddress(body[start], row)) return null;
        int end = local.HasValue ? projectionCall + 1 : projectionCall;
        int assignment = -1;
        if (projection.Assignment != null)
        {
            if (!local.HasValue || end + 3 >= body.Count || !LocalAddress(body[end + 1], local.Value) ||
                !SingleConstant(body[end + 2], projection.Assignment.Value) || !Call(body[end + 3], projection.Assignment.Setter)) return null;
            assignment = end + 1;
            end += 3;
        }
        return new(draw, call, local.HasValue ? cursor : start, start, end, local, assignment, scalarRead);
    }

    private static bool SingleConstant(CodeInstruction instruction, float value) =>
        instruction.opcode == OpCodes.Ldc_R4 && instruction.operand is float actual && actual.Equals(value);

    // Follow just one managed rectangle address beneath straight-line scalar work. The only
    // supported duplication is the native property-update form: dup immediately read by a getter.
    private static int AddressAccessor(IReadOnlyList<CodeInstruction> body, int origin, ISet<MethodInfo> accessors,
        ISet<ConstructorInfo>? constructors = null)
    {
        if (accessors.Count == 0 && (constructors == null || constructors.Count == 0)) return -1;
        int depth = 1;
        for (int index = origin + 1; index < body.Count; index++)
        {
            var instruction = body[index];
            if (instruction.opcode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch or FlowControl.Return or FlowControl.Throw) return -1;
            if (instruction.opcode == OpCodes.Dup)
            {
                if (depth != 1 || index + 1 >= body.Count || body[index + 1].operand is not MethodInfo getter ||
                    !Call(body[index + 1], getter) || !accessors.Contains(getter) || !IsRead(getter)) return -1;
                depth++;
                continue;
            }
            (int pop, int push) = StackEffect(instruction);
            if (pop >= depth)
                return pop == depth && (instruction.operand is MethodInfo method && Call(instruction, method) && accessors.Contains(method) ||
                    instruction.opcode == OpCodes.Call && instruction.operand is ConstructorInfo constructor && constructors != null && constructors.Contains(constructor))
                    ? index : -1;
            depth = depth - pop + push;
        }
        return -1;
    }

    // A scalar address may be retained beneath projection work. Track its eventual read/write,
    // so acquisition before the first read does not hide a later write or forbid a post-use write.
    private static (int End, bool Write) ScalarAddressUse(IReadOnlyList<CodeInstruction> body, int origin)
    {
        int depth = 1;
        for (int index = origin + 1; index < body.Count; index++)
        {
            var instruction = body[index];
            if (instruction.opcode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch or FlowControl.Return or FlowControl.Throw ||
                instruction.opcode == OpCodes.Dup) return (-1, false);
            (int pop, int push) = StackEffect(instruction);
            if (pop >= depth)
            {
                if (pop != depth) return (-1, false);
                if (instruction.opcode == OpCodes.Ldind_R4 || instruction.opcode == OpCodes.Ldobj && Equals(instruction.operand, typeof(float)))
                    return (index, false);
                if (instruction.opcode == OpCodes.Stind_R4 ||
                    (instruction.opcode == OpCodes.Stobj || instruction.opcode == OpCodes.Initobj) && Equals(instruction.operand, typeof(float)))
                    return (index, true);
                return (-1, false);
            }
            depth = depth - pop + push;
        }
        return (-1, false);
    }

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

    // The bounded state is only the next required drawing step, not arbitrary IL values.
    // Alternatives share a step; every ordinary path must complete the entire ordered sequence.
    private static HashSet<int>? DrawingContinuation(int start, IReadOnlyList<CodeInstruction> body,
        Func<int, int> target, IReadOnlyDictionary<int, int> draws, int steps)
    {
        var reached = new HashSet<(int Index, int Step)>();
        var completed = new HashSet<(int Index, int Step)>();
        var predecessors = new Dictionary<(int Index, int Step), List<(int Index, int Step)>>();
        var remaining = new Dictionary<(int Index, int Step), int>();
        var pending = new Queue<(int Index, int Step)>(); pending.Enqueue((start, 0));
        while (pending.Count != 0)
        {
            var state = pending.Dequeue();
            int index = state.Index;
            if (index < 0 || index >= body.Count) return null;
            if (!reached.Add(state)) continue;
            int nextStep = state.Step;
            if (draws.TryGetValue(index, out int step))
            {
                if (step != state.Step) return null;
                if (++nextStep == steps) { completed.Add(state); continue; }
            }
            var flow = body[index].opcode.FlowControl;
            if (flow is FlowControl.Return or FlowControl.Throw) return null;
            var successors = new HashSet<(int Index, int Step)>();
            if (flow is FlowControl.Branch or FlowControl.Cond_Branch) successors.Add((target(index), nextStep));
            if (flow != FlowControl.Branch) successors.Add((index + 1, nextStep));
            remaining[state] = successors.Count;
            foreach (var next in successors)
            {
                if (!predecessors.TryGetValue(next, out var previous)) predecessors.Add(next, previous = new());
                previous.Add(state); pending.Enqueue(next);
            }
        }
        var instructions = reached.Select(state => state.Index).ToHashSet();
        if (!draws.Keys.All(instructions.Contains)) return null;
        var proved = new HashSet<(int Index, int Step)>(completed);
        foreach (var state in completed) pending.Enqueue(state);
        while (pending.Count != 0)
        {
            var state = pending.Dequeue();
            if (!predecessors.TryGetValue(state, out var previous)) continue;
            foreach (var prior in previous)
                if (--remaining[prior] == 0 && proved.Add(prior)) pending.Enqueue(prior);
        }
        return proved.Contains((start, 0)) ? instructions : null;
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
