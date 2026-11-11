using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using Megamod.Patching;

namespace Megamod.Features.DrugTolerance;

public sealed class DrugToleranceSocialPatch
{
    private static readonly ConcurrentDictionary<MethodBase, DrugToleranceSocialPatch> Routes = new();
    private static readonly ConcurrentDictionary<object, DrugToleranceSocialPatch> Lists = new(ReferenceEqualityComparer.Instance);
    private static readonly MethodInfo Transpiler = Callback(nameof(Transpile));
    private static readonly MethodInfo Finalizer = Callback(nameof(Complete));
    private readonly MethodInfo target, add, clear, count, item, forward;
    private readonly FieldInfo candidateList;
    private readonly MethodInfo? shuffle;
    private readonly Func<object, object, bool> allowed;
    private readonly Func<bool> groupAvailable;
    private readonly Action<string> unavailable;
    private readonly OwnedHarmonyPatch transpiler, finalizer;
    private object? workingList;
    private bool installed;
    private Generation? generation;
    private Receipt? receipt;
    private sealed record Generation(int Version, IReadOnlyList<HarmonyPublication.Operation> Body, ISet<int> Protected);
    private sealed record Receipt(Generation Generation, HarmonyPublication.Identity Identity);

    public DrugToleranceSocialPatch(MethodInfo target, FieldInfo candidateList, MethodInfo? shuffle,
        Func<object, object, bool> allowed, Func<bool> groupAvailable, Action<string> unavailable)
    {
        this.target = target; this.candidateList = candidateList; this.shuffle = shuffle;
        this.allowed = allowed; this.groupAvailable = groupAvailable; this.unavailable = unavailable;
        var listType = candidateList.FieldType;
        add = listType.GetMethod("Add")!; clear = listType.GetMethod("Clear")!;
        count = listType.GetProperty("Count")!.GetMethod!; item = listType.GetProperty("Item")!.GetMethod!;
        forward = Callback(nameof(Append)).MakeGenericMethod(listType.GetGenericArguments()[0]);
        transpiler = new(target, HarmonyPatchType.Transpiler, Transpiler, () => Owned(HarmonyPatchType.Transpiler));
        finalizer = new(target, HarmonyPatchType.Finalizer, Finalizer, () => Owned(HarmonyPatchType.Finalizer));
        Candidate = new("DT-SOCIAL-DRINK", "DT-SOCIAL-DRINK", target.ToString()!, "social drink tolerance and contextual editing",
            "native social drinking; retained choices unavailable", Validate, Install, Rollback);
    }
    public PatchCandidate Candidate { get; }
    public bool Available => Admitted() != null && ReferenceEquals(workingList, candidateList.GetValue(null));
    public string? Failure { get; private set; }

    private string? Validate()
    {
        var parameters = target.GetParameters();
        if (!target.IsStatic || target.IsGenericMethod || target.ReturnType != typeof(bool) || parameters.Length != 3 ||
            !parameters[0].ParameterType.IsValueType || !parameters[1].ParameterType.IsClass ||
            !parameters[2].ParameterType.IsByRef || parameters[2].ParameterType.GetElementType()?.IsClass != true ||
            !candidateList.IsStatic || !candidateList.IsInitOnly || !candidateList.FieldType.IsGenericType ||
            candidateList.FieldType.GetGenericTypeDefinition() != typeof(List<>)) return "The native social selector/working list shape changed.";
        return transpiler.ValidateOwnership() ?? finalizer.ValidateOwnership();
    }
    private void Install()
    {
        workingList = candidateList.GetValue(null) ?? throw new InvalidOperationException("Native social candidate list is absent.");
        if (!Routes.TryAdd(target, this) || !Lists.TryAdd(workingList, this)) throw new InvalidOperationException("Social selector already has an owned route.");
        finalizer.Install(); transpiler.Install(); installed = true;
        if (!Available) throw new InvalidOperationException(Failure ?? "The effective native candidate append could not be admitted.");
    }
    private void Rollback()
    {
        installed = false; Volatile.Write(ref receipt, null); Volatile.Write(ref generation, null);
        try { transpiler.Rollback(); }
        finally
        {
            try { finalizer.Rollback(); }
            finally
            {
                if (Routes.TryGetValue(target, out var route) && ReferenceEquals(route, this)) Routes.TryRemove(target, out _);
                if (workingList != null && Lists.TryGetValue(workingList, out route) && ReferenceEquals(route, this)) Lists.TryRemove(workingList, out _);
                workingList = null;
            }
        }
    }
    private Receipt? Admitted()
    {
        var current = Volatile.Read(ref generation);
        if (!installed || current == null) return null;
        var existing = Volatile.Read(ref receipt);
        if (ReferenceEquals(existing?.Generation, current) && existing!.Identity.IsCurrent()) return existing;
        try
        {
            bool result = HarmonyPublication.Inspect(target, snapshot =>
            {
                if (snapshot.Version != current.Version || !ReferenceEquals(current, Volatile.Read(ref generation)) ||
                    snapshot.Transpilers.Any(Factory) || snapshot.Transpilers.Count(p => p.owner == OwnedHarmonyPatch.OwnerId) != 1 ||
                    !snapshot.Transpilers.Any(p => p.owner == OwnedHarmonyPatch.OwnerId && p.PatchMethod == Transpiler) ||
                    PatchProcessor.GetSortedPatchMethods(target, snapshot.Transpilers).LastOrDefault() != Transpiler)
                { Failure = "The candidate filter is not the final admitted transpiler generation."; return false; }
                if (snapshot.Finalizers.Count(p => p.Owner == OwnedHarmonyPatch.OwnerId && p.Method == Finalizer) != 1 ||
                    snapshot.Postfixes.Concat(snapshot.Finalizers).Any(p => p.IsFactory ||
                        p.Method.ReturnType != typeof(void) || p.Method.GetParameters().Any(WritesOutput)))
                { Failure = "A social completion callback can replace the filtered output or cannot be inspected."; return false; }
                Failure = snapshot.FailureFor(current.Body, current.Protected);
                if (Failure != null) return false;
                Volatile.Write(ref receipt, new(current, snapshot.Identity));
                return true;
            });
            return result ? Volatile.Read(ref receipt) : null;
        }
        catch (Exception error) { Failure = error.GetBaseException().Message; return null; }
    }
    private bool WritesOutput(ParameterInfo parameter)
    {
        var binding = Binding(parameter);
        return binding.Name == "__args" || (parameter.ParameterType.IsByRef &&
            (binding.Name == "__result" || binding.Name == "__resultRef" ||
             binding.Index == 2 || binding.Name == "___" + candidateList.Name));
    }
    private sealed record ArgumentAlias(string? Original, int Index, string? NewName);
    // The inspected provider resolves injected names before original-argument indexes. Read its
    // attribute metadata only; GetCustomAttributes/GetArgumentAttribute would execute constructors.
    private (string Name, int Index) Binding(ParameterInfo parameter)
    {
        var aliases = new List<ArgumentAlias>();
        void Add(IEnumerable<CustomAttributeData> attributes)
        {
            foreach (var attribute in attributes.Where(a => a.AttributeType.Name == nameof(HarmonyArgument)))
            {
                if (aliases.Count >= 64) throw new InvalidOperationException("Social completion argument metadata exceeds its bound.");
                aliases.Add(ParseAlias(attribute));
            }
        }
        var direct = parameter.CustomAttributes.Where(a => a.AttributeType.Name == nameof(HarmonyArgument)).FirstOrDefault();
        ArgumentAlias? specific = direct == null ? null : ParseAlias(direct);
        var callback = (MethodInfo)parameter.Member;
        Add(callback.CustomAttributes);
        int depth = 0;
        for (var type = callback.DeclaringType; type != null; type = type.BaseType)
        {
            if (++depth > 32) throw new InvalidOperationException("Social completion attribute inheritance exceeds its bound.");
            Add(type.CustomAttributes);
        }
        string raw = parameter.Name ?? string.Empty;
        var mapped = aliases.FirstOrDefault(a => a.Original == raw);
        string mappedName = !string.IsNullOrEmpty(mapped?.NewName) ? mapped!.NewName! : raw;
        string name = specific != null ? specific.Original ?? raw : mappedName;
        var originals = target.GetParameters();
        int index;
        if (specific != null && !string.IsNullOrEmpty(specific.Original))
            index = Array.FindIndex(originals, p => p.Name == specific.Original);
        else if (specific != null && specific.Index >= 0 && specific.Index < originals.Length) index = specific.Index;
        else index = Array.FindIndex(originals, p => p.Name == mappedName);
        // MethodCreatorTools.ResolveArgumentIndex parses the effective name, including aliases,
        // rather than comparing a canonical spelling: __02 also binds original argument two.
        if (name.StartsWith("__", StringComparison.Ordinal) && int.TryParse(name.Substring(2), out int numbered) &&
            numbered >= 0 && numbered < originals.Length) index = numbered;
        return (name, index);
    }
    private static ArgumentAlias ParseAlias(CustomAttributeData attribute)
    {
        var arguments = attribute.ConstructorArguments;
        if (attribute.AttributeType != typeof(HarmonyArgument) || attribute.NamedArguments.Count != 0 ||
            arguments.Count < 1 || arguments.Count > 2)
            throw new InvalidOperationException("Social completion argument alias has an unproved metadata shape.");
        string? original = null, replacement = null; int index = -1;
        if (arguments[0].ArgumentType == typeof(string)) original = (string?)arguments[0].Value;
        else if (arguments[0].ArgumentType == typeof(int)) index = (int)arguments[0].Value!;
        else throw new InvalidOperationException("Social completion alias source is unproved.");
        if (arguments.Count == 2)
        {
            if (arguments[1].ArgumentType == typeof(string)) replacement = (string?)arguments[1].Value;
            else if (arguments[0].ArgumentType != typeof(string) || arguments[1].ArgumentType != typeof(ArgumentMode) ||
                Convert.ToInt32(arguments[1].Value) != (int)ArgumentMode.Default)
                throw new InvalidOperationException("Social completion uses an unsupported argument-binding mode.");
        }
        return new(original, index, replacement);
    }
    private static bool Factory(Patch patch) => patch.PatchMethod.ReturnType == typeof(MethodInfo) || patch.PatchMethod.ReturnType == typeof(DynamicMethod);
    private IReadOnlyList<Patch> Owned(HarmonyPatchType kind)
    {
        Patch[]? found = null;
        if (!HarmonyPublication.Inspect(target, snapshot =>
        {
            found = kind == HarmonyPatchType.Transpiler ? snapshot.Transpilers.Where(p => p.owner == OwnedHarmonyPatch.OwnerId).ToArray() :
                snapshot.Finalizers.Where(p => p.Owner == OwnedHarmonyPatch.OwnerId).Select(p => p.ToOrderingPatch()).ToArray();
            return true;
        })) throw new InvalidOperationException("Social selector ownership publication is busy.");
        return found!;
    }
    [HarmonyPriority(int.MinValue)]
    private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var source = instructions.ToList();
        if (!Routes.TryGetValue(__originalMethod, out var route)) return source;
        Volatile.Write(ref route.receipt, null); Volatile.Write(ref route.generation, null);
        try
        {
            if (!route.Match(source, out int append, out var protectedIndexes)) return source;
            int version = HarmonyPublication.ExpectedVersion(__originalMethod);
            var changed = source.Select(instruction => new CodeInstruction(instruction)).ToList();
            var actor = new CodeInstruction(OpCodes.Ldarg_1);
            actor.labels.AddRange(changed[append].labels); changed[append].labels.Clear();
            changed[append].opcode = OpCodes.Call; changed[append].operand = route.forward;
            changed.Insert(append, actor);
            var shifted = new HashSet<int>(protectedIndexes.Select(index => index >= append ? index + 1 : index)) { append };
            Volatile.Write(ref route.generation, new(version, changed.Select(i => new HarmonyPublication.Operation(i.opcode, i.operand)).ToArray(), shifted));
            return changed;
        }
        catch (Exception error) { route.Failure = error.GetBaseException().Message; return source; }
    }
    private bool Match(IReadOnlyList<CodeInstruction> code, out int append, out HashSet<int> protectedIndexes)
    {
        append = -1; protectedIndexes = new();
        int clearAt = -1;
        var consumers = new List<int>();
        for (int index = 0; index < code.Count; index++)
        {
            var instruction = code[index];
            if ((instruction.opcode == OpCodes.Stsfld || instruction.opcode == OpCodes.Ldsflda) && Equals(instruction.operand, candidateList))
            { Failure = "The social working-list identity can be changed or escaped."; return false; }
            if ((instruction.opcode == OpCodes.Starg || instruction.opcode == OpCodes.Starg_S || instruction.opcode == OpCodes.Ldarga || instruction.opcode == OpCodes.Ldarga_S) &&
                Convert.ToInt32(instruction.operand) == 1 && !ImmediatePawnRead(code, index))
            { Failure = "The actual social recipient argument can be reassigned or escaped."; return false; }
            if (instruction.opcode != OpCodes.Ldsfld || !Equals(instruction.operand, candidateList)) continue;
            int height = 1, consumer = -1;
            for (int next = index + 1; next < code.Count; next++)
            {
                var value = code[next];
                if (value.labels.Count != 0 || value.blocks.Count != 0 || value.opcode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch or FlowControl.Return or FlowControl.Throw ||
                    (value.opcode == OpCodes.Dup && height == 1)) break;
                if (!StackChange(value, out int pop, out int push) || pop > height) break;
                if (pop == height)
                {
                    if (value.operand is MethodInfo method && (method == add || method == clear || method == count || method == item || method == shuffle)) consumer = next;
                    break;
                }
                height += push - pop;
            }
            if (consumer < 0) { Failure = "The social working list escapes its proved native consumer."; return false; }
            consumers.Add(consumer);
            for (int part = index; part <= consumer; part++) protectedIndexes.Add(part);
            if (code[consumer].Calls(add)) { if (append >= 0) return false; append = consumer; }
            if (code[consumer].Calls(clear)) { if (clearAt >= 0) return false; clearAt = consumer; }
        }
        if (append < 0 || clearAt < 0 || !Dominates(code, clearAt, consumers.Where(index => index != clearAt)))
        { Failure = "One native candidate append and a dominating working-list clear are required."; return false; }
        return true;
    }
    private bool ImmediatePawnRead(IReadOnlyList<CodeInstruction> code, int address)
    {
        if (code[address].opcode != OpCodes.Ldarga && code[address].opcode != OpCodes.Ldarga_S) return false;
        int next = address + 1;
        while (next < code.Count && code[next].opcode == OpCodes.Nop) next++;
        return next < code.Count && (code[next].opcode == OpCodes.Ldind_Ref ||
            code[next].opcode == OpCodes.Ldobj && Equals(code[next].operand, target.GetParameters()[1].ParameterType));
    }
    private static bool StackChange(CodeInstruction instruction, out int pop, out int push)
    {
        if (instruction.operand is MethodBase method && instruction.opcode.FlowControl == FlowControl.Call)
        {
            pop = method.GetParameters().Length + (instruction.opcode == OpCodes.Newobj || method.IsStatic ? 0 : 1);
            push = instruction.opcode == OpCodes.Newobj || method is MethodInfo info && info.ReturnType != typeof(void) ? 1 : 0;
            return true;
        }
        pop = instruction.opcode.StackBehaviourPop switch
        {
            StackBehaviour.Pop0 => 0, StackBehaviour.Pop1 or StackBehaviour.Popi or StackBehaviour.Popref => 1,
            StackBehaviour.Pop1_pop1 or StackBehaviour.Popi_pop1 or StackBehaviour.Popi_popi or StackBehaviour.Popi_popi8 or
                StackBehaviour.Popi_popr4 or StackBehaviour.Popi_popr8 or StackBehaviour.Popref_pop1 or StackBehaviour.Popref_popi => 2,
            _ => -1
        };
        push = instruction.opcode.StackBehaviourPush switch
        {
            StackBehaviour.Push0 => 0, StackBehaviour.Push1 or StackBehaviour.Pushi or StackBehaviour.Pushi8 or StackBehaviour.Pushr4 or
                StackBehaviour.Pushr8 or StackBehaviour.Pushref => 1, StackBehaviour.Push1_push1 => 2, _ => -1
        };
        return pop >= 0 && push >= 0;
    }
    private static bool Dominates(IReadOnlyList<CodeInstruction> code, int clear, IEnumerable<int> required)
    {
        var labels = new Dictionary<Label, int>();
        for (int index = 0; index < code.Count; index++) foreach (var label in code[index].labels) if (!labels.TryAdd(label, index)) return false;
        // An exception region spanning Clear could recover after its failure without clearing.
        int nesting = 0;
        for (int index = 0; index <= clear; index++)
            foreach (var block in code[index].blocks)
            {
                if (block.blockType == ExceptionBlockType.BeginExceptionBlock) nesting++;
                if (block.blockType == ExceptionBlockType.EndExceptionBlock) nesting--;
            }
        if (nesting != 0) return false;
        var needed = new HashSet<int>(required);
        var seen = new HashSet<int>(); var pending = new Stack<int>(); pending.Push(0);
        while (pending.Count != 0)
        {
            int index = pending.Pop();
            if (index == clear || index >= code.Count || !seen.Add(index)) continue;
            if (needed.Contains(index)) return false;
            var instruction = code[index];
            if (instruction.operand is Label destination)
            { if (!labels.TryGetValue(destination, out int target)) return false; pending.Push(target); }
            if (instruction.operand is Label[] alternatives)
                foreach (var label in alternatives) { if (!labels.TryGetValue(label, out int target)) return false; pending.Push(target); }
            if (instruction.opcode.FlowControl is not (FlowControl.Branch or FlowControl.Return or FlowControl.Throw)) pending.Push(index + 1);
        }
        return true;
    }
    private static void Append<T>(List<T> list, T drug, object pawn)
    {
        if (!Lists.TryGetValue(list, out var route)) { list.Add(drug); return; }
        bool include = true;
        try { if (route.Available && route.groupAvailable()) include = route.allowed(pawn, drug!); }
        catch (Exception error) { route.Refuse(error.GetBaseException().Message); }
        if (include) list.Add(drug);
    }
    [HarmonyPriority(int.MinValue)]
    private static void Complete(MethodBase __originalMethod, bool __runOriginal)
    {
        if (!__runOriginal && Routes.TryGetValue(__originalMethod, out var route)) route.Refuse("The native social selection body was skipped.");
    }
    private void Refuse(string reason)
    {
        Failure = reason;
        try { unavailable(reason); } catch (Exception) { }
    }
    private static MethodInfo Callback(string name) => typeof(DrugToleranceSocialPatch).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;
}
