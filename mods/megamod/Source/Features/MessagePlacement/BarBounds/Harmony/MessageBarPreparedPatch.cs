using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading;
using HarmonyLib;
using Megamod.Patching;

namespace Megamod.Features.MessagePlacement;

public sealed record MessageBarPreparedTargets(MethodInfo Render, MethodInfo Entries, MethodInfo ShowGroupFrames,
    IReadOnlyList<MethodInfo>? Consumers = null);

// Observes the actual prepared locals. This is not a geometry or renderer-consumption receipt.
public sealed class MessageBarPreparedPatch
{
    private static readonly ConcurrentDictionary<MethodBase, MessageBarPreparedPatch> Routes = new();
    private static readonly MethodInfo Transpiler = typeof(MessageBarPreparedPatch).GetMethod(nameof(Transpile), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo Observer = typeof(MessageBarPreparedPatch).GetMethod(nameof(Observe), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo PreparingObserver = typeof(MessageBarPreparedPatch).GetMethod(nameof(ObservePreparing), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo ConsumerObserver = typeof(MessageBarPreparedPatch).GetMethod(nameof(ObserveConsumer), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static long nextGeneration;
    private readonly MessageBarPreparedTargets targets;
    private readonly Action<object, object, bool, Func<bool>> prepared;
    private readonly Action<Exception> reportFailure;
    private readonly OwnedHarmonyPatch receipt;
    private readonly MessageBarEntryConsumptionPatch? entryConsumption;
    private readonly MessageBarGeometryGuard? geometry;
    private readonly Action<object>? preparing;
    private readonly Action<object, int, object>? consuming;
    private readonly Action<Exception> geometryFailureReporter;
    private sealed record Incoming(int Version, long Token, IReadOnlyList<HarmonyPublication.Operation> Body,
        ISet<int> Protected, ISet<int>? EntryArguments, string? EntryFailure,
        ISet<int>? GeometryOperations, string? GeometryFailure);
    private sealed record Publication(Incoming Generation, string? EntryFailure, string? GeometryFailure, HarmonyPublication.Identity Identity);
    private Incoming? incoming;
    private Publication? publication;
    private bool installed, enabled, faulted, geometryFaulted;

    public MessageBarPreparedPatch(MessageBarPreparedTargets targets,
        Action<object, object, bool, Func<bool>> prepared, Action<Exception> reportFailure,
        MessageBarEntryConsumptionPatch? entryConsumption = null, MessageBarGeometryGuard? geometry = null,
        Action<object>? preparing = null, Action<object, int, object>? consuming = null,
        Action<Exception>? geometryFailureReporter = null)
    {
        this.targets = targets; this.prepared = prepared; this.reportFailure = reportFailure;
        this.entryConsumption = entryConsumption;
        this.geometry = geometry; this.preparing = preparing; this.consuming = consuming;
        this.geometryFailureReporter = geometryFailureReporter ?? reportFailure;
        receipt = new(targets.Render, HarmonyPatchType.Transpiler, Transpiler, ReadTranspilers);
        Candidate = new("MSG-BAR-PREPARED", "MSG-BAR-LAYOUT-PREP", targets.Render.ToString()!,
            "prepared message placement", "native message placement", Validate, Install, Rollback);
    }
    public PatchCandidate Candidate { get; }
    public string? Failure { get; private set; }
    public bool GeometryAvailable => Available && !geometryFaulted && geometry != null &&
        Volatile.Read(ref publication) is { GeometryFailure: null } geometryPublication && geometryPublication.Identity.IsCurrent();
    public string? GeometryFailure => Volatile.Read(ref publication)?.GeometryFailure ??
        (GeometryAvailable ? null : "Native geometry authority is unavailable.");
    public Func<bool> CaptureGeometryAuthority()
    {
        var admitted = Volatile.Read(ref publication);
        if (admitted == null || !GeometryAvailable) return () => false;
        return () => GeometryAvailable && ReferenceEquals(admitted.Generation, Volatile.Read(ref publication)?.Generation);
    }
    public bool Available => installed && !faulted && Volatile.Read(ref publication) is { } value &&
        ReferenceEquals(value.Generation, Volatile.Read(ref incoming));
    // Selected consumer arguments only: this does not admit source-field geometry or GUI lifetime.
    public bool EntryArgumentsAvailable => Available && Volatile.Read(ref publication)?.EntryFailure == null && entryConsumption != null;
    public string? EntryArgumentsFailure => Volatile.Read(ref publication)?.EntryFailure ??
        (EntryArgumentsAvailable ? null : "Selected entry-call argument admission is unavailable.");
    public Func<bool> CaptureEntryArgumentsAuthority()
    {
        var admitted = Volatile.Read(ref publication);
        if (admitted == null || admitted.EntryFailure != null || !EntryArgumentsAvailable) return () => false;
        return () => EntryArgumentsAvailable && ReferenceEquals(admitted.Generation, Volatile.Read(ref publication)?.Generation);
    }
    public void Enable()
    {
        if (!Available) throw new InvalidOperationException("The prepared bar handoff is unavailable.");
        enabled = true;
    }
    public bool RefreshPublication()
    {
        Volatile.Write(ref publication, null);
        var generation = Volatile.Read(ref incoming);
        if (!installed || faulted || generation == null) return false;
        try
        {
            bool accepted = HarmonyPublication.Inspect(targets.Render, snapshot =>
            {
                if (snapshot.Version != generation.Version)
                { Failure = "The prepared handoff generation has not published."; return false; }
                Failure = OrderingFailure(snapshot.Transpilers) ?? snapshot.FailureFor(generation.Body, generation.Protected);
                if (Failure != null || !installed || faulted || !ReferenceEquals(generation, Volatile.Read(ref incoming))) return false;
                string? entryFailure = generation.EntryFailure;
                if (entryFailure == null && generation.EntryArguments is { } required)
                {
                    try { entryFailure = snapshot.FailureFor(generation.Body, required); }
                    catch (Exception error) { entryFailure = error.GetBaseException().Message; }
                }
                string? geometryFailure = generation.GeometryFailure;
                if (geometryFailure == null && generation.GeometryOperations is { } geometryRequired)
                {
                    try { geometryFailure = snapshot.FailureFor(generation.Body, geometryRequired); }
                    catch (Exception error) { geometryFailure = error.GetBaseException().Message; }
                }
                Volatile.Write(ref publication, new Publication(generation, entryFailure, geometryFailure, snapshot.Identity));
                return true;
            });
            if (!accepted && Failure == null) Failure = "Published prepared-handoff metadata is busy.";
            return accepted;
        }
        catch (Exception error) { Failure = error.GetBaseException().Message; return false; }
    }
    private string? Validate()
    {
        bool Callable(MethodInfo method) => !method.IsStatic && !method.IsAbstract && !method.IsGenericMethod &&
            !method.ContainsGenericParameters && method.GetParameters().Length == 0 &&
            method.DeclaringType?.IsAssignableFrom(targets.Render.DeclaringType) == true &&
            !method.CustomAttributes.Any(attribute => attribute.AttributeType.FullName is
                "System.Runtime.CompilerServices.AsyncStateMachineAttribute" or
                "System.Runtime.CompilerServices.IteratorStateMachineAttribute" or
                "System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute");
        if (!Callable(targets.Render) || !targets.Render.IsPublic || targets.Render.ReturnType != typeof(void) ||
            !Callable(targets.Entries) || !targets.Entries.IsPublic || !targets.Entries.ReturnType.IsGenericType ||
            targets.Entries.ReturnType.GetGenericTypeDefinition() != typeof(List<>) ||
            !Callable(targets.ShowGroupFrames) || targets.ShowGroupFrames.ReturnType != typeof(bool))
            return "The synchronous native bar preparation callable shapes changed.";
        if (ReadTranspilers().Any(IsFactory)) return "A prepared-handoff transpiler factory prevents safe inspection.";
        return receipt.ValidateOwnership();
    }
    private void Install()
    {
        if (!Routes.TryAdd(targets.Render, this)) throw new InvalidOperationException("The prepared bar handoff already has an owner.");
        receipt.Install(); installed = true;
        if (!RefreshPublication()) throw new InvalidOperationException(Failure ?? "The native prepared locals are missing or ambiguous.");
    }
    private void Rollback()
    {
        enabled = false; installed = false; Volatile.Write(ref publication, null);
        try { receipt.Rollback(); }
        finally
        {
            if (Routes.TryGetValue(targets.Render, out var owner) && ReferenceEquals(owner, this))
                Routes.TryRemove(targets.Render, out _);
        }
    }
    private Patch[] ReadTranspilers()
    {
        Patch[]? result = null;
        if (!HarmonyPublication.Inspect(targets.Render, snapshot => { result = snapshot.Transpilers; return true; }))
            throw new InvalidOperationException("Published prepared-handoff metadata is busy or cannot be inspected during composition.");
        return result!;
    }
    private static bool IsFactory(Patch patch) => patch.PatchMethod.IsStatic &&
        (patch.PatchMethod.ReturnType == typeof(MethodInfo) || patch.PatchMethod.ReturnType == typeof(DynamicMethod)) &&
        patch.PatchMethod.GetParameters().Select(parameter => parameter.ParameterType).SequenceEqual(new[] { typeof(MethodBase) });
    private string? OrderingFailure(Patch[] patches)
    {
        if (patches.Any(IsFactory)) return "A prepared-handoff transpiler factory prevents safe ordering inspection.";
        if (patches.Count(patch => patch.owner == OwnedHarmonyPatch.OwnerId) != 1 ||
            patches.Count(patch => patch.owner == OwnedHarmonyPatch.OwnerId && patch.PatchMethod == Transpiler) != 1)
            return "The acquired prepared-handoff transpiler is absent or duplicated.";
        var sorted = PatchProcessor.GetSortedPatchMethods(targets.Render, patches);
        return sorted.Count != 0 && sorted[sorted.Count - 1] == Transpiler ? null :
            "Another transpiler follows the prepared-handoff observer.";
    }
    [HarmonyPriority(int.MinValue)]
    private static IEnumerable<CodeInstruction> Transpile(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var body = instructions.ToList();
        if (!Routes.TryGetValue(__originalMethod, out var owner)) return body;
        Volatile.Write(ref owner.publication, null); Volatile.Write(ref owner.incoming, null);
        int version;
        try { version = HarmonyPublication.ExpectedVersion(__originalMethod); }
        catch (Exception)
        {
            // Unreadable metadata must not obstruct native rebuilding or foreign patch removal.
            // This revoked generation contributes no handoff and preserves the incoming stream.
            return body;
        }
        var meaningful = Enumerable.Range(0, body.Count).Where(index => body[index].opcode != OpCodes.Nop).ToArray();
        var entriesCalls = meaningful.Where(index => body[index].Calls(owner.targets.Entries)).ToArray();
        var framesCalls = meaningful.Where(index => body[index].Calls(owner.targets.ShowGroupFrames)).ToArray();
        if (entriesCalls.Length != 1 || framesCalls.Length != 1) return body;
        int entriesAt = Array.IndexOf(meaningful, entriesCalls[0]), framesAt = Array.IndexOf(meaningful, framesCalls[0]);
        if (entriesAt < 1 || framesAt <= entriesAt + 1 || framesAt + 1 >= meaningful.Length ||
            body[meaningful[entriesAt - 1]].opcode != OpCodes.Ldarg_0 ||
            body[meaningful[framesAt - 1]].opcode != OpCodes.Ldarg_0) return body;
        int entriesLocal = StoredLocal(body[meaningful[entriesAt + 1]]), framesLocal = StoredLocal(body[meaningful[framesAt + 1]]);
        if (entriesLocal < 0 || framesLocal < 0 || entriesLocal == framesLocal) return body;
        int start = meaningful[entriesAt - 1], end = meaningful[framesAt + 1];
        for (int index = start; index <= end; index++)
        {
            if (body[index].blocks.Count != 0 || body[index].opcode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch or FlowControl.Return or FlowControl.Throw)
                return body;
            if (index > meaningful[entriesAt + 1] && StoredLocal(body[index]) == entriesLocal) return body;
        }
        // No jump may enter the preparation sequence after its actual receiver was loaded.
        var interior = body.Skip(start + 1).Take(end - start).SelectMany(instruction => instruction.labels).ToHashSet();
        if (body.Any(instruction => instruction.operand is Label label && interior.Contains(label) ||
            instruction.operand is Label[] labels && labels.Any(interior.Contains))) return body;
        long token = Interlocked.Increment(ref nextGeneration);
        MessageBarGeometryDescription? description = null;
        string? geometryFailure = "No native geometry participant was selected.";
        if (owner.geometry != null && owner.preparing != null && owner.consuming != null)
        {
            try
            {
                description = owner.geometry.Describe(body, end, entriesLocal);
                geometryFailure = description.Failure;
            }
            catch (Exception error) { geometryFailure = error.GetBaseException().Message; }
        }
        var emitted = new List<CodeInstruction>();
        var mapped = new int[body.Count];
        var geometryRequired = geometryFailure == null ? new HashSet<int>() : null;
        var injected = new[]
        {
            new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldloc, entriesLocal),
            new CodeInstruction(OpCodes.Ldloc, framesLocal), new CodeInstruction(OpCodes.Ldtoken, __originalMethod),
            new CodeInstruction(OpCodes.Ldc_I8, token), new CodeInstruction(OpCodes.Call, Observer)
        };
        var required = new HashSet<int>();
        int handoff = -1;
        for (int index = 0; index < body.Count; index++)
        {
            var original = new CodeInstruction(body[index]);
            if (index == start && owner.preparing != null)
            {
                var before = new[]
                {
                    new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldtoken, __originalMethod),
                    new CodeInstruction(OpCodes.Ldc_I8, token), new CodeInstruction(OpCodes.Call, PreparingObserver)
                };
                before[0].labels.AddRange(original.labels); original.labels.Clear();
                geometryRequired?.UnionWith(Enumerable.Range(emitted.Count, before.Length));
                emitted.AddRange(before);
            }
            if (geometryRequired != null && description!.EntryConsumers.Contains(index))
            {
                var consume = new[]
                {
                    new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldloc, description.IndexLocal),
                    new CodeInstruction(OpCodes.Ldloc, description.RectangleLocal), new CodeInstruction(OpCodes.Box, owner.geometry!.RectangleType),
                    new CodeInstruction(OpCodes.Ldtoken, __originalMethod), new CodeInstruction(OpCodes.Ldc_I8, token),
                    new CodeInstruction(OpCodes.Call, ConsumerObserver)
                };
                consume[0].labels.AddRange(original.labels); original.labels.Clear();
                geometryRequired.UnionWith(Enumerable.Range(emitted.Count, consume.Length));
                emitted.AddRange(consume);
            }
            mapped[index] = emitted.Count;
            emitted.Add(original);
            if (index == end)
            {
                required.UnionWith(Enumerable.Range(emitted.Count, injected.Length));
                emitted.AddRange(injected);
                handoff = emitted.Count - 1;
            }
        }
        if (geometryRequired != null)
            geometryRequired.UnionWith(description!.Required.Select(index => mapped[index]));
        if (owner.targets.Consumers is { } consumers &&
            !ProtectConsumerOrder(emitted, handoff, consumers, required)) return body;
        (ISet<int>? Required, string? Failure) entry;
        try
        {
            entry = owner.entryConsumption?.Describe(emitted, handoff) ??
                (null, "No entry-call participant was selected.");
        }
        catch (Exception error) { entry = (null, error.GetBaseException().Message); }
        Volatile.Write(ref owner.incoming, new Incoming(version, token,
            emitted.Select(instruction => new HarmonyPublication.Operation(instruction.opcode, instruction.operand)).ToArray(),
            required, entry.Required, entry.Failure, geometryRequired, geometryFailure));
        return emitted;
    }

    // Removing the handoff from the graph must make every selected consumer unreachable.
    // This admits ordinary loops/switches without confusing emitted order with execution order.
    private static bool ProtectConsumerOrder(IReadOnlyList<CodeInstruction> body, int handoff,
        IReadOnlyList<MethodInfo> consumers, ISet<int> required)
    {
        if (consumers.Count == 0 || consumers.Distinct().Count() != consumers.Count) return false;
        var uses = new HashSet<int>();
        foreach (var consumer in consumers)
        {
            var calls = Enumerable.Range(0, body.Count).Where(index => body[index].Calls(consumer)).ToArray();
            if (calls.Length != 1) return false;
            uses.Add(calls[0]);
        }
        // Exception transfers need a different proof than this ordinary branch graph.
        // Native ColonistBarOnGUI has no protected regions; keep its body unchanged on uncertainty.
        if (body.Any(instruction => instruction.blocks.Count != 0)) return false;
        var labels = new Dictionary<Label, int>();
        for (int index = 0; index < body.Count; index++)
            foreach (var label in body[index].labels)
                if (!labels.TryAdd(label, index)) return false;
        var pending = new Stack<int>();
        var seen = new HashSet<int>();
        pending.Push(0);
        while (pending.Count != 0)
        {
            int index = pending.Pop();
            if (index == handoff || index == body.Count || !seen.Add(index)) continue;
            if (index < 0 || index > body.Count || uses.Contains(index)) return false;
            var instruction = body[index];
            if (instruction.opcode.FlowControl is FlowControl.Branch or FlowControl.Cond_Branch)
            {
                var destinations = instruction.operand is Label label ? new[] { label } : instruction.operand as Label[];
                if (destinations == null) return false;
                foreach (var destination in destinations)
                {
                    if (!labels.TryGetValue(destination, out int target)) return false;
                    pending.Push(target);
                }
                if (instruction.opcode.FlowControl == FlowControl.Branch) continue;
            }
            if (instruction.opcode.FlowControl is FlowControl.Return or FlowControl.Throw) continue;
            if (instruction.opcode == OpCodes.Jmp) return false;
            pending.Push(index + 1);
        }
        foreach (int use in uses) required.Add(use);
        return true;
    }
    private static int StoredLocal(CodeInstruction instruction)
    {
        if (instruction.opcode == OpCodes.Stloc_0) return 0;
        if (instruction.opcode == OpCodes.Stloc_1) return 1;
        if (instruction.opcode == OpCodes.Stloc_2) return 2;
        if (instruction.opcode == OpCodes.Stloc_3) return 3;
        if (instruction.opcode != OpCodes.Stloc && instruction.opcode != OpCodes.Stloc_S) return -1;
        return instruction.operand is LocalBuilder local ? local.LocalIndex : Convert.ToInt32(instruction.operand);
    }
    private static void Observe(object bar, object entries, bool frames, RuntimeMethodHandle method, long token)
    {
        if (!Routes.TryGetValue(MethodBase.GetMethodFromHandle(method)!, out var owner) || !owner.enabled) return;
        try
        {
            // Successful admission belongs to the immutable incoming generation. Normal provider
            // recomposition revokes it in Transpile; busy/unpublished generations remain retryable.
            if (!owner.Available && !owner.RefreshPublication()) return;
            var generation = Volatile.Read(ref owner.publication)?.Generation;
            if (generation == null || generation.Token != token) return;
            owner.prepared(bar, entries, frames, () => owner.Available &&
                ReferenceEquals(generation, Volatile.Read(ref owner.publication)?.Generation));
        }
        catch (Exception error)
        {
            owner.faulted = true; owner.enabled = false; Volatile.Write(ref owner.publication, null);
            try { owner.reportFailure(error); } catch (Exception) { /* Native rendering remains authoritative. */ }
        }
    }

    private static void ObservePreparing(object bar, RuntimeMethodHandle method, long token) =>
        ObserveGeometry(method, token, owner => owner.preparing?.Invoke(bar), requireGeometry: false);

    private static void ObserveConsumer(object bar, int index, object rectangle, RuntimeMethodHandle method, long token) =>
        ObserveGeometry(method, token, owner => owner.consuming?.Invoke(bar, index, rectangle), requireGeometry: true);

    private static void ObserveGeometry(RuntimeMethodHandle method, long token, Action<MessageBarPreparedPatch> action, bool requireGeometry)
    {
        if (!Routes.TryGetValue(MethodBase.GetMethodFromHandle(method)!, out var owner) || !owner.enabled || owner.geometryFaulted) return;
        try
        {
            if (!owner.Available && !owner.RefreshPublication()) return;
            if (Volatile.Read(ref owner.publication)?.Generation.Token != token ||
                requireGeometry && !owner.GeometryAvailable) return;
            action(owner);
        }
        catch (Exception error)
        {
            owner.geometryFaulted = true;
            try { owner.geometryFailureReporter(error); } catch (Exception) { /* Preserve native and manual rendering. */ }
        }
    }
}
