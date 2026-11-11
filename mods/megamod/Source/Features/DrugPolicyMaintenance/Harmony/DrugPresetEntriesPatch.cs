using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using Megamod.Patching;

namespace Megamod.Features.DrugPolicyMaintenance.Harmony;

public sealed class DrugPresetEntriesPatch
{
    private static readonly ConcurrentDictionary<MethodBase, DrugPresetEntriesPatch> Routes = new();
    private static long nextGeneration;
    private readonly MethodInfo target, createPolicy, lookup, copyFrom, listItem, listCount;
    private readonly FieldInfo entries, drug;
    private readonly Func<PresetEntriesRead?> captureBaseline;
    private readonly Action<string> reportUnavailable;
    private readonly OwnedHarmonyPatch receipt;
    private sealed record Compilation(bool Accepted, long Id = 0, int Version = 0,
        IReadOnlyList<HarmonyPublication.Operation>? Body = null, ISet<int>? Required = null);
    private Compilation incoming = new(false);
    private Compilation? publication, attempted;
    private volatile bool installed, enabled;
    private int reported;

    public DrugPresetEntriesPatch(MethodInfo target, MethodInfo createPolicy, FieldInfo entries, FieldInfo drug,
        MethodInfo lookup, MethodInfo copyFrom, Func<PresetEntriesRead?> captureBaseline, Action<string> reportUnavailable)
    {
        this.target = target; this.createPolicy = createPolicy; this.entries = entries; this.drug = drug;
        this.lookup = lookup; this.copyFrom = copyFrom;
        this.captureBaseline = captureBaseline; this.reportUnavailable = reportUnavailable;
        listItem = entries.FieldType.GetMethod("get_Item", new[] { typeof(int) })!;
        listCount = entries.FieldType.GetMethod("get_Count", Type.EmptyTypes)!;
        receipt = new OwnedHarmonyPatch(target, HarmonyPatchType.Transpiler, TranspilerMethod, Patches);
        Candidate = new PatchCandidate("MM.DRUG.PresetEntries", "MM.DRUG.PresetEntries", target.DeclaringType!.FullName + "." + target.Name,
            "Unavailable-drug preset entries", "Native preset behavior", Validate, Install, Rollback);
    }
    public PatchCandidate Candidate { get; }
    public string? Failure { get; private set; }
    public bool Available => installed && ReferenceEquals(Volatile.Read(ref incoming), Volatile.Read(ref publication));
    public bool NeedsPublicationRefresh => installed && !ReferenceEquals(Volatile.Read(ref incoming), Volatile.Read(ref attempted));
    public void Enable()
    {
        if (!Available) throw new InvalidOperationException("Preset entry guard is unavailable.");
        enabled = true;
    }
    public void Disable() => enabled = false;

    private static MethodInfo Method(string name) => typeof(DrugPresetEntriesPatch).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!;
    private static MethodInfo TranspilerMethod => Method(nameof(Transpile));
    private static MethodInfo CaptureMethod => Method(nameof(Capture));
    private static MethodInfo DecisionMethod => Method(nameof(ShouldCopy));
    private string? Validate()
    {
        Type policy = target.ReturnType;
        Type[] parameters = target.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
        if (target.IsStatic || target.IsGenericMethod || target.DeclaringType!.IsGenericType || policy.IsValueType ||
            parameters.Length != 1 || parameters[0].IsValueType || target.GetMethodBody() == null ||
            entries.IsStatic || entries.DeclaringType != parameters[0] || !entries.FieldType.IsGenericType ||
            entries.FieldType.GetGenericTypeDefinition() != typeof(List<>) || drug.IsStatic || drug.FieldType.IsValueType ||
            entries.FieldType.GetGenericArguments()[0] != drug.DeclaringType ||
            createPolicy.IsStatic || createPolicy.DeclaringType != target.DeclaringType || createPolicy.IsGenericMethod ||
            createPolicy.ReturnType != policy || createPolicy.GetParameters().Length != 0 ||
            lookup.IsStatic || lookup.IsGenericMethod || lookup.DeclaringType != policy || lookup.ReturnType != drug.DeclaringType ||
            !lookup.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(new[] { drug.FieldType }) ||
            copyFrom.IsStatic || copyFrom.IsGenericMethod || copyFrom.DeclaringType != drug.DeclaringType || copyFrom.ReturnType != typeof(void) ||
            !copyFrom.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(new[] { drug.DeclaringType }) ||
            listItem == null || listCount == null)
            return "The native preset, list, drug, creation, lookup or copy callable changed.";
        if (Patches().Any(IsFactory)) return "Preset transpiler factories cannot be inspected safely.";
        return receipt.ValidateOwnership();
    }
    private void Install()
    {
        if (Patches().Any(IsFactory) || !Routes.TryAdd(target, this)) throw new InvalidOperationException("Preset target ownership is unavailable.");
        receipt.Install();
        installed = true;
        if (!RefreshPublication()) throw new InvalidOperationException(Failure ?? "The preset entry stream was not admitted.");
    }
    private void Rollback()
    {
        enabled = installed = false;
        Volatile.Write(ref publication, null);
        try { receipt.Rollback(); }
        finally { if (Routes.TryGetValue(target, out var route) && ReferenceEquals(route, this)) Routes.TryRemove(target, out _); }
    }
    private IReadOnlyList<Patch> Patches()
    {
        Patch[]? result = null;
        if (!HarmonyPublication.Inspect(target, snapshot => { result = snapshot.Transpilers; return true; }))
            throw new InvalidOperationException("Preset publication metadata is busy.");
        return result!;
    }
    private static bool IsFactory(Patch patch) => patch.PatchMethod.IsStatic &&
        (patch.PatchMethod.ReturnType == typeof(MethodInfo) || patch.PatchMethod.ReturnType == typeof(DynamicMethod)) &&
        patch.PatchMethod.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(new[] { typeof(MethodBase) });

    // Explicit composition-owner operation; never inspect metadata from a factory callback.
    public bool RefreshPublication()
    {
        Volatile.Write(ref publication, null);
        var generation = Volatile.Read(ref incoming);
        Volatile.Write(ref attempted, generation);
        if (!installed || !generation.Accepted) return false;
        try
        {
            return HarmonyPublication.Inspect(target, snapshot =>
            {
                var patches = snapshot.Transpilers;
                if (snapshot.Version != generation.Version || patches.Any(IsFactory) ||
                    patches.Count(patch => patch.owner == OwnedHarmonyPatch.OwnerId) != 1 ||
                    patches.Count(patch => patch.PatchMethod == TranspilerMethod) != 1 ||
                    !patches.Any(patch => patch.owner == OwnedHarmonyPatch.OwnerId && patch.PatchMethod == TranspilerMethod))
                { Failure = "The preset compilation has not published or its owned receipt changed."; return false; }
                var sorted = PatchProcessor.GetSortedPatchMethods(target, patches);
                if (sorted.Count == 0 || sorted[sorted.Count - 1] != TranspilerMethod)
                { Failure = "The preset guard is not last in ordinary composition."; return false; }
                string? failure = snapshot.FailureFor(generation.Body!, generation.Required!);
                if (failure != null) { Failure = failure; return false; }
                if (!installed || !ReferenceEquals(generation, Volatile.Read(ref incoming))) return false;
                Failure = null;
                Volatile.Write(ref publication, generation);
                return true;
            });
        }
        catch (Exception exception) { Failure = Bound(exception.GetBaseException().Message); return false; }
    }

    [HarmonyPriority(int.MinValue)]
    private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, ILGenerator generator, MethodBase __originalMethod)
    {
        var body = instructions.ToList();
        if (!Routes.TryGetValue(__originalMethod, out var route)) return body;
        Volatile.Write(ref route.publication, null);
        Volatile.Write(ref route.incoming, new Compilation(false));
        Interlocked.Exchange(ref route.reported, 0);
        try
        {
            int version = HarmonyPublication.ExpectedVersion(__originalMethod);
            if (!route.FindLoop(body, out int start, out int drugLoad, out int increment, out int loopEnd))
            { route.Failure = "The unique preset source/drug/lookup/copy loop changed."; return body; }
            long id = Interlocked.Increment(ref nextGeneration);
            LocalBuilder read = generator.DeclareLocal(typeof(PresetEntriesRead));
            Label next = generator.DefineLabel(), copy = generator.DefineLabel();
            var output = new List<CodeInstruction>
            {
                new(OpCodes.Ldtoken, __originalMethod), new(OpCodes.Ldc_I8, id), new(OpCodes.Call, CaptureMethod), new(OpCodes.Stloc, read)
            };
            var required = new HashSet<int> { 0, 1, 2 };
            for (int index = 0; index < body.Count; index++)
            {
                var instruction = new CodeInstruction(body[index]);
                if (index == increment) instruction.labels.Add(next);
                if (index == drugLoad + 1) instruction.labels.Add(copy);
                if (index >= start && index <= loopEnd) required.Add(output.Count);
                output.Add(instruction);
                if (index != drugLoad) continue;
                // Stack already holds native policy and drug. Keep both on the normal branch.
                int first = output.Count;
                output.Add(new CodeInstruction(OpCodes.Ldloc, read));
                output.Add(new CodeInstruction(OpCodes.Brfalse, copy));
                output.Add(new CodeInstruction(OpCodes.Dup));
                output.Add(new CodeInstruction(OpCodes.Ldloc, read));
                output.Add(new CodeInstruction(OpCodes.Call, DecisionMethod));
                output.Add(new CodeInstruction(OpCodes.Brtrue, copy));
                output.Add(new CodeInstruction(OpCodes.Pop));
                output.Add(new CodeInstruction(OpCodes.Pop));
                output.Add(new CodeInstruction(OpCodes.Br, next));
                for (int position = first; position < output.Count; position++) required.Add(position);
            }
            Volatile.Write(ref route.incoming, new Compilation(true, id, version,
                Array.AsReadOnly(output.Select(instruction => new HarmonyPublication.Operation(instruction.opcode, instruction.operand)).ToArray()), required));
            return output;
        }
        catch (Exception exception)
        {
            route.Failure = "Preset stream admission failed: " + Bound(exception.GetBaseException().Message);
            return body;
        }
    }

    private bool FindLoop(List<CodeInstruction> body, out int start, out int drugLoad, out int increment, out int loopEnd)
    {
        start = drugLoad = increment = loopEnd = -1;
        var code = body.Where(instruction => instruction.opcode != OpCodes.Nop).ToList();
        if (code.Count(instruction => instruction.Calls(createPolicy)) != 1 || code.Count(instruction => instruction.Calls(lookup)) != 1 ||
            code.Count(instruction => instruction.Calls(copyFrom)) != 1) return false;
        int call = code.FindIndex(instruction => instruction.Calls(copyFrom));
        if (call < 11 || call + 9 >= code.Count) return false;
        int first = call - 11;
        int policy = LoadLocal(code[first]);
        int index = LoadLocal(code[first + 3]);
        int create = code.FindIndex(instruction => instruction.Calls(createPolicy));
        var aliases = new HashSet<int>();
        // Accept immutable argument aliases established in the straight-line entry before creation.
        for (int position = 1; position < create; position++)
        {
            if (code[position - 1].opcode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch) break;
            int slot = StoreLocal(code[position]);
            if (slot >= 0 && IsPreset(code[position - 1]) && code.Count(instruction => StoreLocal(instruction) == slot) == 1 &&
                !code.Any(instruction => AddressesLocal(instruction, slot))) aliases.Add(slot);
        }
        bool IsPreset(CodeInstruction instruction) => instruction.opcode == OpCodes.Ldarg_1 || aliases.Contains(LoadLocal(instruction));
        if (code.Any(instruction => instruction.opcode == OpCodes.Starg || instruction.opcode == OpCodes.Starg_S ||
            instruction.opcode == OpCodes.Ldarga || instruction.opcode == OpCodes.Ldarga_S)) return false;
        if (policy < 0 || index < 0 || !IsPreset(code[first + 1]) || !code[first + 2].LoadsField(entries) ||
            !code[first + 4].Calls(listItem) || !code[first + 5].LoadsField(drug) || !code[first + 6].Calls(lookup) ||
            !IsPreset(code[first + 7]) || !code[first + 8].LoadsField(entries) ||
            LoadLocal(code[first + 9]) != index || !code[first + 10].Calls(listItem) ||
            LoadLocal(code[call + 1]) != index || !code[call + 2].LoadsConstant(1) || code[call + 3].opcode != OpCodes.Add ||
            StoreLocal(code[call + 4]) != index || LoadLocal(code[call + 5]) != index ||
            !IsPreset(code[call + 6]) || !code[call + 7].LoadsField(entries) || !code[call + 8].Calls(listCount) ||
            (code[call + 9].opcode != OpCodes.Blt && code[call + 9].opcode != OpCodes.Blt_S) ||
            code[call + 9].operand is not Label loop || NextMeaningful(body, loop) != body.IndexOf(code[first])) return false;
        if (create < 1 || code[create - 1].opcode != OpCodes.Ldarg_0 || create + 1 >= first || StoreLocal(code[create + 1]) != policy ||
            code.Count(instruction => StoreLocal(instruction) == policy) != 1 || code.Any(instruction => AddressesLocal(instruction, policy))) return false;
        start = body.IndexOf(code[first]); drugLoad = body.IndexOf(code[first + 5]); increment = body.IndexOf(code[call + 1]);
        loopEnd = body.IndexOf(code[call + 9]);
        if (!CreationDominatesLoop(body, body.IndexOf(code[create - 1]), body.IndexOf(code[create + 1]), start)) return false;
        var interior = body.Skip(start + 1).Take(increment - start - 1).ToArray();
        if (body.Skip(start).Take(increment - start + 1).Any(instruction => instruction.blocks.Count != 0)) return false;
        var labels = new HashSet<Label>(interior.SelectMany(instruction => instruction.labels));
        return !body.Any(instruction => instruction.operand is Label label && labels.Contains(label) ||
            instruction.operand is Label[] targets && targets.Any(labels.Contains));
    }
    private static int LoadLocal(CodeInstruction instruction) => Local(instruction, false);
    private static bool CreationDominatesLoop(List<CodeInstruction> body, int receiverLoad, int createdStore, int loopStart)
    {
        // This native factory has no handler entry before its explicit loop. Such a changed
        // exception path needs its own proof rather than treating ordinary branch edges as complete.
        if (body.Take(loopStart + 1).Any(instruction => instruction.blocks.Count != 0)) return false;
        // Entry at the receiver still executes the complete creation chain. Entry after it
        // could supply another receiver or result, including via nops before the call/store.
        var creationLabels = new HashSet<Label>(body.Skip(receiverLoad + 1).Take(createdStore - receiverLoad)
            .SelectMany(instruction => instruction.labels));
        if (body.Any(instruction => instruction.operand is Label label && creationLabels.Contains(label) ||
            instruction.operand is Label[] labels && labels.Any(creationLabels.Contains))) return false;
        var pending = new Stack<int>();
        var visited = new HashSet<int>();
        pending.Push(0);
        while (pending.Count != 0)
        {
            int index = pending.Pop();
            if (index == createdStore || index < 0 || index >= body.Count || !visited.Add(index)) continue;
            if (index == loopStart) return false;
            var instruction = body[index];
            if (instruction.operand is Label label) pending.Push(body.FindIndex(item => item.labels.Contains(label)));
            if (instruction.operand is Label[] labels)
                foreach (var targetLabel in labels) pending.Push(body.FindIndex(item => item.labels.Contains(targetLabel)));
            if (instruction.opcode.FlowControl is not (FlowControl.Branch or FlowControl.Return or FlowControl.Throw)) pending.Push(index + 1);
        }
        return true;
    }
    private static int StoreLocal(CodeInstruction instruction) => Local(instruction, true);
    private static bool AddressesLocal(CodeInstruction instruction, int slot) =>
        (instruction.opcode == OpCodes.Ldloca || instruction.opcode == OpCodes.Ldloca_S) && OperandLocal(instruction) == slot;
    private static int NextMeaningful(List<CodeInstruction> body, Label label)
    {
        int index = body.FindIndex(instruction => instruction.labels.Contains(label));
        while (index >= 0 && index < body.Count && body[index].opcode == OpCodes.Nop) index++;
        return index;
    }
    private static int Local(CodeInstruction instruction, bool store)
    {
        OpCode code = instruction.opcode;
        short first = store ? OpCodes.Stloc_0.Value : OpCodes.Ldloc_0.Value;
        if (code.Value >= first && code.Value <= first + 3) return code.Value - first;
        if (code != (store ? OpCodes.Stloc : OpCodes.Ldloc) && code != (store ? OpCodes.Stloc_S : OpCodes.Ldloc_S)) return -1;
        return OperandLocal(instruction);
    }
    private static int OperandLocal(CodeInstruction instruction) => instruction.operand is LocalBuilder builder ? builder.LocalIndex :
            instruction.operand is LocalVariableInfo variable ? variable.LocalIndex : Convert.ToInt32(instruction.operand);
    private static PresetEntriesRead? Capture(RuntimeMethodHandle handle, long generation)
    {
        DrugPresetEntriesPatch? route = null;
        try
        {
            var target = MethodBase.GetMethodFromHandle(handle);
            if (target == null || !Routes.TryGetValue(target, out route) || !route.enabled || !route.Available ||
                Volatile.Read(ref route.incoming).Id != generation) return null;
            var read = route.captureBaseline();
            if (read?.IsCurrent == true && route.enabled && route.Available && Volatile.Read(ref route.incoming).Id == generation) return read;
            route.Report("The finalized native drug baseline is absent or expired.", false);
            return null;
        }
        catch (Exception exception) { route?.Report(exception.GetBaseException().Message, true); return null; }
    }
    private static bool ShouldCopy(object? drug, PresetEntriesRead? read) => read == null || read.Contains(drug);
    private void Report(string reason, bool disable)
    {
        if (disable) enabled = false;
        Failure = Bound(reason);
        if (Interlocked.Exchange(ref reported, 1) != 0) return;
        try { reportUnavailable("[fumblesneeze.megamod] Patch MM.DRUG.PresetEntries unavailable; target=" +
            target.DeclaringType!.FullName + "." + target.Name + "; reason=" + Failure +
            "; unavailable=unavailable-drug preset omission; fallback=native/provider preset behavior."); }
        catch (Exception) { /* Reporting cannot replace the native preset outcome. */ }
    }
    private static string Bound(string text) => new(text.Take(256).Select(character => char.IsControl(character) ? ' ' : character).ToArray());
}
