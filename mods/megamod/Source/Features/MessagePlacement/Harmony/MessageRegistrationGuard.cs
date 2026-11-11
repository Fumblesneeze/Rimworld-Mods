using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using Megamod.Patching;

namespace Megamod.Features.MessagePlacement;

// This guard proves only the registration dataflow, not caller/consumer timing.
public sealed class MessageRegistrationGuard
{
    private static readonly Dictionary<MethodBase, MessageRegistrationGuard> Routes = new();
    private static readonly MethodInfo Observer = typeof(MessageRegistrationGuard).GetMethod(nameof(Observe), BindingFlags.NonPublic | BindingFlags.Static)!;
    private readonly MethodInfo register, addNew;
    private readonly FieldInfo rectangle, callback;
    private readonly OwnedHarmonyPatch[] receipts;
    private sealed record Incoming(int Version, IReadOnlyList<HarmonyPublication.Operation> Body, ISet<int> Protected);
    private Incoming? registerIncoming, addNewIncoming;
    private object generation = new();
    private bool installed;

    public MessageRegistrationGuard(MethodInfo register, MethodInfo addNew, FieldInfo rectangle, FieldInfo callback)
    {
        this.register = register; this.addNew = addNew; this.rectangle = rectangle; this.callback = callback;
        receipts = new[] { register, addNew }.Select(method => new OwnedHarmonyPatch(method, HarmonyPatchType.Transpiler, Observer,
            () => ReadOwnedSlot(method))).ToArray();
        Candidate = new("MSG-NATIVE-QUEUE", "MSG-NATIVE-FRAME", register.ToString()!, "message placement",
            "native placement", Validate, Install, Rollback);
    }
    public PatchCandidate Candidate { get; }
    public bool Available => AdmittedGeneration() != null;
    public object? Generation => AdmittedGeneration();

    private object? AdmittedGeneration()
    {
        var current = Volatile.Read(ref generation);
        var existing = Volatile.Read(ref registerIncoming);
        var added = Volatile.Read(ref addNewIncoming);
        if (!installed || existing == null || added == null) return null;
        try
        {
            // Both native paths must be admitted from the same provider publication interval.
            bool accepted = HarmonyPublication.InspectPair(register, addNew, (first, second) =>
                first.Version == existing.Version && second.Version == added.Version &&
                IsLastObserver(register, first.Transpilers) && IsLastObserver(addNew, second.Transpilers) &&
                first.FailureFor(existing.Body, existing.Protected) == null && second.FailureFor(added.Body, added.Protected) == null &&
                installed && ReferenceEquals(current, Volatile.Read(ref generation)) &&
                ReferenceEquals(existing, Volatile.Read(ref registerIncoming)) && ReferenceEquals(added, Volatile.Read(ref addNewIncoming)));
            return accepted ? current : null;
        }
        catch (Exception) { return null; }
    }

    private string? Validate()
    {
        Type[] arguments = { typeof(int), rectangle.FieldType, register.GetParameters().ElementAtOrDefault(2)?.ParameterType ?? typeof(void),
            typeof(Action), typeof(bool), typeof(bool), typeof(float), typeof(Action), typeof(bool) };
        if (register == addNew || register.DeclaringType != addNew.DeclaringType || !register.IsPublic || !addNew.IsPrivate ||
            new[] { register, addNew }.Any(method => method.IsStatic || method.IsGenericMethod || method.ContainsGenericParameters ||
                method.ReturnType != typeof(void) || !method.GetParameters().Select(p => p.ParameterType).SequenceEqual(arguments)) ||
            !rectangle.FieldType.IsValueType || rectangle.IsStatic || rectangle.IsInitOnly ||
            callback.IsStatic || callback.IsInitOnly || callback.FieldType != typeof(Action) ||
            rectangle.DeclaringType == null || callback.DeclaringType == null ||
            !rectangle.DeclaringType.IsAssignableFrom(callback.DeclaringType))
            return "Native immediate-window registration field or callable shape changed.";
        string? failure = null;
        bool inspected = HarmonyPublication.InspectPair(register, addNew, (first, second) =>
        {
            var patches = first.Transpilers.Concat(second.Transpilers).ToArray();
            failure = patches.Any(Factory) ? "A transpiler factory prevents safe registration ordering inspection." :
                patches.Any(patch => patch.owner == OwnedHarmonyPatch.OwnerId)
                    ? "An owned registration target/kind slot already contains a patch; safe rollback cannot be acquired." : null;
            return true;
        });
        return inspected ? failure : "Published registration metadata is busy or cannot be inspected during composition.";
    }

    private void Install()
    {
        foreach (MethodInfo method in new[] { register, addNew })
            if (Routes.ContainsKey(method)) throw new InvalidOperationException("The native registration guard already has an owner.");
        Routes.Add(register, this); Routes.Add(addNew, this);
        foreach (var receipt in receipts) receipt.Install();
        installed = true;
        if (!Available) throw new InvalidOperationException("The incoming window registration does not retain the required rectangle/callback relationships.");
    }

    private void Rollback()
    {
        installed = false;
        Volatile.Write(ref generation, new());
        Volatile.Write(ref registerIncoming, null); Volatile.Write(ref addNewIncoming, null);
        var failures = new List<Exception>();
        foreach (var receipt in receipts.Reverse())
            try { receipt.Rollback(); } catch (Exception error) { failures.Add(error); }
        foreach (MethodInfo method in new[] { register, addNew })
            if (Routes.TryGetValue(method, out var owner) && ReferenceEquals(owner, this)) Routes.Remove(method);
        if (failures.Count != 0) throw new AggregateException("Registration observer rollback failed.", failures);
    }

    private static bool Factory(Patch patch) => patch.PatchMethod.IsStatic &&
        (patch.PatchMethod.ReturnType == typeof(MethodInfo) || patch.PatchMethod.ReturnType == typeof(DynamicMethod)) &&
        patch.PatchMethod.GetParameters().Select(p => p.ParameterType).SequenceEqual(new[] { typeof(MethodBase) });
    private static IReadOnlyList<Patch> ReadOwnedSlot(MethodInfo method)
    {
        Patch[]? owned = null;
        if (!HarmonyPublication.Inspect(method, snapshot =>
            { owned = snapshot.Transpilers.Where(patch => patch.owner == OwnedHarmonyPatch.OwnerId).ToArray(); return true; }))
            throw new InvalidOperationException("Published registration metadata is busy or cannot be inspected during composition.");
        return owned!;
    }
    private static bool IsLastObserver(MethodInfo method, Patch[] patches)
    {
        if (patches.Any(Factory) || patches.Count(p => p.owner == OwnedHarmonyPatch.OwnerId) != 1 ||
            patches.Count(p => p.owner == OwnedHarmonyPatch.OwnerId && p.PatchMethod == Observer) != 1) return false;
        var sorted = PatchProcessor.GetSortedPatchMethods(method, patches);
        return sorted.Count > 0 && sorted[sorted.Count - 1] == Observer;
    }

    [HarmonyPriority(int.MinValue)]
    private static IEnumerable<CodeInstruction> Observe(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var incoming = instructions.ToList();
        if (!Routes.TryGetValue(__originalMethod, out var guard)) return incoming;
        Volatile.Write(ref guard.generation, new());
        bool existing = __originalMethod == guard.register;
        if (existing) Volatile.Write(ref guard.registerIncoming, null);
        else Volatile.Write(ref guard.addNewIncoming, null);
        int version;
        try { version = HarmonyPublication.ExpectedVersion(__originalMethod); }
        catch (Exception) { return incoming; } // Preserve native recomposition when inspection cannot establish authority.
        var meaningful = incoming.Where(instruction => instruction.opcode != OpCodes.Nop).ToArray();
        bool accepted = StoresOnce(meaningful, guard.rectangle) &&
            StoresOnce(meaningful, guard.callback) && guard.SameWindow(incoming, __originalMethod == guard.register) &&
            (__originalMethod != guard.register || meaningful.Count(instruction => instruction.Calls(guard.addNew)) == 1);
        if (accepted)
        {
            // Protect only the consumed stores and unchanged forwarding; runtime binding and
            // consumer vetoes still establish the actual window, callback and placement timing.
            var required = Enumerable.Range(0, incoming.Count).Where(index =>
                (incoming[index].opcode == OpCodes.Stfld &&
                    (Equals(incoming[index].operand, guard.rectangle) || Equals(incoming[index].operand, guard.callback))) ||
                (existing && incoming[index].Calls(guard.addNew))).ToHashSet();
            var value = new Incoming(version,
                incoming.Select(instruction => new HarmonyPublication.Operation(instruction.opcode, instruction.operand)).ToArray(), required);
            if (existing) Volatile.Write(ref guard.registerIncoming, value);
            else Volatile.Write(ref guard.addNewIncoming, value);
        }
        return incoming;
    }

    // Track aliases only within straight-line blocks. Unknown/merged values never establish identity.
    // This is deliberately not a general IL interpreter or a proof that called helpers cannot draw.
    private bool SameWindow(IReadOnlyList<CodeInstruction> instructions, bool forwards)
    {
        var stack = new Stack<object?>();
        var locals = new Dictionary<int, object?>();
        // A retained managed address can mutate a local after any later assignment, including across blocks.
        var exposedLocals = new HashSet<int>(instructions.Where(instruction =>
            instruction.opcode == OpCodes.Ldloca || instruction.opcode == OpCodes.Ldloca_S)
            .Select(instruction => LocalIndex(instruction.operand)));
        var arguments = Enumerable.Range(0, 10).Select(_ => new object()).ToArray();
        bool forwarded = !forwards, directlyConsumed = false, storedValues = true;
        object? rectangleReceiver = null, callbackReceiver = null;
        object? Pop() => stack.Count == 0 ? null : stack.Pop();
        foreach (var instruction in instructions)
        {
            if (instruction.labels.Count != 0 || instruction.blocks.Count != 0) { stack.Clear(); locals.Clear(); }
            OpCode op = instruction.opcode;
            if (op == OpCodes.Nop) continue;
            if ((op == OpCodes.Starg || op == OpCodes.Starg_S || op == OpCodes.Ldarga || op == OpCodes.Ldarga_S) &&
                Convert.ToInt32(instruction.operand) is 0 or 2 or 4)
                return false;
            if (Argument(instruction) is int argument) stack.Push(arguments[argument]);
            else if (op == OpCodes.Ldloca || op == OpCodes.Ldloca_S)
            {
                locals[LocalIndex(instruction.operand)] = null;
                stack.Push(null);
            }
            else if (Local(instruction, false) is int load)
                stack.Push(locals.TryGetValue(load, out var value) ? value : null);
            else if (Local(instruction, true) is int store)
            {
                var value = Pop();
                locals[store] = exposedLocals.Contains(store) ? null : value;
            }
            else if (op == OpCodes.Dup) { var value = Pop(); stack.Push(value); stack.Push(value); }
            else if (op == OpCodes.Castclass) { var value = Pop(); stack.Push(value); }
            else if (op == OpCodes.Stfld)
            {
                var value = Pop(); var receiver = Pop();
                if (Equals(instruction.operand, rectangle))
                { rectangleReceiver = receiver; storedValues &= ReferenceEquals(value, arguments[2]); }
                if (Equals(instruction.operand, callback))
                { callbackReceiver = receiver; storedValues &= ReferenceEquals(value, arguments[4]); }
            }
            else if (instruction.operand is MethodBase method &&
                (op == OpCodes.Call || op == OpCodes.Callvirt || op == OpCodes.Newobj))
            {
                var inputs = new object?[method.GetParameters().Length];
                for (int index = inputs.Length - 1; index >= 0; index--) inputs[index] = Pop();
                object? receiver = op != OpCodes.Newobj && !method.IsStatic ? Pop() : null;
                if (Equals(method, typeof(Action).GetMethod(nameof(Action.Invoke))) && ReferenceEquals(receiver, arguments[4]))
                    directlyConsumed = true;
                if (Equals(method, addNew)) forwarded = ReferenceEquals(receiver, arguments[0]) &&
                    ReferenceEquals(inputs[1], arguments[2]) && ReferenceEquals(inputs[3], arguments[4]);
                if (op == OpCodes.Newobj || method is MethodInfo info && info.ReturnType != typeof(void)) stack.Push(new object());
            }
            else
            {
                int pop = PopCount(op.StackBehaviourPop);
                if (pop < 0) { stack.Clear(); locals.Clear(); }
                else for (int index = 0; index < pop; index++) Pop();
                if (op.StackBehaviourPush != StackBehaviour.Push0) stack.Push(new object());
            }
            if (op.FlowControl == FlowControl.Branch || op.FlowControl == FlowControl.Cond_Branch ||
                op.FlowControl == FlowControl.Return || op.FlowControl == FlowControl.Throw) { stack.Clear(); locals.Clear(); }
        }
        return storedValues && !directlyConsumed && forwarded && rectangleReceiver != null && ReferenceEquals(rectangleReceiver, callbackReceiver);
    }

    private static int? Argument(CodeInstruction instruction)
    {
        if (instruction.opcode == OpCodes.Ldarg_0) return 0;
        if (instruction.opcode == OpCodes.Ldarg_1) return 1;
        if (instruction.opcode == OpCodes.Ldarg_2) return 2;
        if (instruction.opcode == OpCodes.Ldarg_3) return 3;
        return instruction.opcode == OpCodes.Ldarg || instruction.opcode == OpCodes.Ldarg_S ? Convert.ToInt32(instruction.operand) : null;
    }

    private static int? Local(CodeInstruction instruction, bool store)
    {
        OpCode op = instruction.opcode;
        if (op == (store ? OpCodes.Stloc_0 : OpCodes.Ldloc_0)) return 0;
        if (op == (store ? OpCodes.Stloc_1 : OpCodes.Ldloc_1)) return 1;
        if (op == (store ? OpCodes.Stloc_2 : OpCodes.Ldloc_2)) return 2;
        if (op == (store ? OpCodes.Stloc_3 : OpCodes.Ldloc_3)) return 3;
        if (op != (store ? OpCodes.Stloc : OpCodes.Ldloc) && op != (store ? OpCodes.Stloc_S : OpCodes.Ldloc_S)) return null;
        return LocalIndex(instruction.operand);
    }

    private static int LocalIndex(object operand) => operand is LocalBuilder builder ? builder.LocalIndex :
        operand is LocalVariableInfo variable ? variable.LocalIndex : Convert.ToInt32(operand);

    private static int PopCount(StackBehaviour behavior) => behavior switch
    {
        StackBehaviour.Pop0 => 0,
        StackBehaviour.Pop1 or StackBehaviour.Popi or StackBehaviour.Popref => 1,
        StackBehaviour.Pop1_pop1 or StackBehaviour.Popi_pop1 or StackBehaviour.Popi_popi or StackBehaviour.Popi_popi8 or
            StackBehaviour.Popi_popr4 or StackBehaviour.Popi_popr8 or StackBehaviour.Popref_pop1 or StackBehaviour.Popref_popi => 2,
        StackBehaviour.Popi_popi_popi or StackBehaviour.Popref_popi_popi or StackBehaviour.Popref_popi_popi8 or
            StackBehaviour.Popref_popi_popr4 or StackBehaviour.Popref_popi_popr8 or StackBehaviour.Popref_popi_popref or
            StackBehaviour.Popref_popi_pop1 => 3,
        _ => -1
    };

    private static bool StoresOnce(IEnumerable<CodeInstruction> instructions, FieldInfo field) =>
        instructions.Count(instruction => instruction.opcode == OpCodes.Stfld && Equals(instruction.operand, field)) == 1;
}
