using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using Megamod.Patching;

namespace Megamod.Features.DrugTolerance;

// Only the two native bool selection boundaries. Social candidate filtering and contextual
// rendering have separate consumed semantics and participate in the same owning runtime group.
public sealed class DrugToleranceDecisionPatch
{
    private static readonly ConcurrentDictionary<MethodBase, DrugToleranceDecisionPatch> Routes = new();
    private static readonly MethodInfo PostfixMethod = Callback(nameof(Postfix));
    private static readonly MethodInfo FinalizerMethod = Callback(nameof(Finalizer));
    private static readonly MethodInfo ObserverMethod = Callback(nameof(Observe));
    private readonly MethodInfo target;
    private readonly MethodInfo prefixMethod;
    private readonly bool trackerReceiver;
    private readonly Func<object, object, bool> extraAllowed;
    private readonly Func<bool> groupAvailable;
    private readonly Action<string> unavailable;
    private readonly OwnedHarmonyPatch prefix, postfix, finalizer, observer;
    private bool installed;
    private Generation? generation;
    private Receipt? receipt;
    private sealed record Generation(int Version);
    private sealed record Receipt(Generation Generation, HarmonyPublication.Identity Identity);
    private sealed class Invocation
    {
        internal readonly DrugToleranceDecisionPatch Route;
        internal readonly object Actor, Candidate;
        internal readonly Receipt Receipt;
        internal bool HasNativeResult, NativeResult;
        internal Invocation(DrugToleranceDecisionPatch route, object actor, object candidate, Receipt receipt)
        { Route = route; Actor = actor; Candidate = candidate; Receipt = receipt; }
    }

    public DrugToleranceDecisionPatch(string id, MethodInfo target, bool trackerReceiver,
        Func<object, object, bool> extraAllowed, Func<bool> groupAvailable, Action<string> unavailable)
    {
        this.target = target; this.trackerReceiver = trackerReceiver; this.extraAllowed = extraAllowed;
        this.groupAvailable = groupAvailable; this.unavailable = unavailable;
        prefixMethod = Callback(trackerReceiver ? nameof(ScheduledPrefix) : nameof(JoyPrefix));
        prefix = new(target, HarmonyPatchType.Prefix, prefixMethod, () => Owned(HarmonyPatchType.Prefix));
        postfix = new(target, HarmonyPatchType.Postfix, PostfixMethod, () => Owned(HarmonyPatchType.Postfix));
        finalizer = new(target, HarmonyPatchType.Finalizer, FinalizerMethod, () => Owned(HarmonyPatchType.Finalizer));
        observer = new(target, HarmonyPatchType.Transpiler, ObserverMethod, () => Owned(HarmonyPatchType.Transpiler));
        Candidate = new(id, "DT-ROUTINE-DECISIONS", target.ToString()!, "routine tolerance restrictions and contextual editing",
            "native eligibility; retained choices unavailable", Validate, Install, Rollback);
    }

    public PatchCandidate Candidate { get; }
    public string? Failure { get; private set; }
    public bool Available => Admitted() != null;

    private Receipt? Admitted()
    {
        var current = Volatile.Read(ref generation);
        if (!installed || current == null) return null;
        var admitted = Volatile.Read(ref receipt);
        if (ReferenceEquals(admitted?.Generation, current) && admitted!.Identity.IsCurrent()) return admitted;
        try
        {
            bool inspected = HarmonyPublication.Inspect(target, snapshot =>
            {
                if (snapshot.Version != current.Version || !ReferenceEquals(current, Volatile.Read(ref generation)))
                { Failure = "The observed selection generation no longer matches publication."; return false; }
                if (snapshot.Prefixes.Concat(snapshot.Postfixes).Concat(snapshot.Finalizers).Any(p => p.IsFactory))
                { Failure = "An outer factory prevents safe selection ordering inspection."; return false; }
                bool Slot(IReadOnlyList<HarmonyPublication.OuterRegistration> items, MethodInfo callback) =>
                    items.Count(p => p.Owner == OwnedHarmonyPatch.OwnerId) == 1 &&
                    items.Count(p => p.Owner == OwnedHarmonyPatch.OwnerId && p.Method == callback) == 1;
                if (!Slot(snapshot.Prefixes, prefixMethod) || !Slot(snapshot.Postfixes, PostfixMethod) ||
                    !Slot(snapshot.Finalizers, FinalizerMethod) || snapshot.Transpilers.Count(p => p.owner == OwnedHarmonyPatch.OwnerId) != 1 ||
                    !snapshot.Transpilers.Any(p => p.owner == OwnedHarmonyPatch.OwnerId && p.PatchMethod == ObserverMethod))
                { Failure = "An owned selection callback was removed or duplicated."; return false; }
                var prefixes = PatchProcessor.GetSortedPatchMethods(target, snapshot.Prefixes.Select(p => p.ToOrderingPatch()).ToArray());
                var postfixes = PatchProcessor.GetSortedPatchMethods(target, snapshot.Postfixes.Select(p => p.ToOrderingPatch()).ToArray());
                var finalizers = PatchProcessor.GetSortedPatchMethods(target, snapshot.Finalizers.Select(p => p.ToOrderingPatch()).ToArray());
                if (prefixes.LastOrDefault() != prefixMethod || postfixes.FirstOrDefault() != PostfixMethod || finalizers.LastOrDefault() != FinalizerMethod)
                { Failure = "Selection needs the last capture prefix, first result postfix and last restriction finalizer."; return false; }
                Volatile.Write(ref receipt, new(current, snapshot.Identity));
                Failure = null;
                return true;
            });
            return inspected ? Volatile.Read(ref receipt) : null;
        }
        catch (Exception error) { Failure = error.GetBaseException().Message; return null; }
    }

    private string? Validate()
    {
        if (target.IsStatic || target.IsGenericMethod || target.ReturnType != typeof(bool) ||
            target.GetParameters().Length != (trackerReceiver ? 1 : 2) || target.GetParameters().Any(p => !p.ParameterType.IsClass))
            return "The native instance bool selection callable changed.";
        return prefix.ValidateOwnership() ?? postfix.ValidateOwnership() ?? finalizer.ValidateOwnership() ?? observer.ValidateOwnership();
    }
    private void Install()
    {
        if (!Routes.TryAdd(target, this)) throw new InvalidOperationException("The decision target already has an owned route.");
        prefix.Install(); postfix.Install(); finalizer.Install(); observer.Install(); installed = true;
        if (!Available) throw new InvalidOperationException(Failure ?? "Selection publication is unavailable.");
    }
    private void Rollback()
    {
        installed = false; Volatile.Write(ref receipt, null); Volatile.Write(ref generation, null);
        var failures = new List<Exception>();
        foreach (var slot in new[] { observer, finalizer, postfix, prefix })
            try { slot.Rollback(); } catch (Exception error) { failures.Add(error); }
        if (Routes.TryGetValue(target, out var current) && ReferenceEquals(current, this)) Routes.TryRemove(target, out _);
        if (failures.Count != 0) throw new AggregateException("Decision cleanup failed.", failures);
    }
    private IReadOnlyList<Patch> Owned(HarmonyPatchType kind)
    {
        Patch[]? found = null;
        if (!HarmonyPublication.Inspect(target, snapshot =>
        {
            found = kind == HarmonyPatchType.Transpiler ? snapshot.Transpilers.Where(p => p.owner == OwnedHarmonyPatch.OwnerId).ToArray() :
                (kind == HarmonyPatchType.Prefix ? snapshot.Prefixes : kind == HarmonyPatchType.Postfix ? snapshot.Postfixes : snapshot.Finalizers)
                .Where(p => p.Owner == OwnedHarmonyPatch.OwnerId).Select(p => p.ToOrderingPatch()).ToArray();
            return true;
        })) throw new InvalidOperationException("Selection ownership publication is busy.");
        return found!;
    }
    [HarmonyPriority(int.MinValue)]
    private static void ScheduledPrefix(MethodBase __originalMethod, object __instance, object __0, out Invocation? __state) =>
        __state = Capture(__originalMethod, __instance, __0);
    [HarmonyPriority(int.MinValue)]
    private static void JoyPrefix(MethodBase __originalMethod, object __0, object __1, out Invocation? __state) =>
        __state = Capture(__originalMethod, __0, __1);
    private static Invocation? Capture(MethodBase original, object actor, object candidate)
    {
        if (!Routes.TryGetValue(original, out var route)) return null;
        try
        {
            var admitted = route.Admitted();
            if (admitted == null || !route.groupAvailable()) return null;
            if (actor == null || candidate == null) { route.Refuse("Native selection inputs are absent."); return null; }
            return new(route, actor, candidate, admitted);
        }
        catch (Exception error) { route.Refuse(error.GetBaseException().Message); }
        return null;
    }
    [HarmonyPriority(int.MaxValue)]
    private static void Postfix(MethodBase __originalMethod, bool __result, bool __runOriginal, Invocation? __state)
    {
        if (!__runOriginal)
        {
            if (Routes.TryGetValue(__originalMethod, out var route)) route.Refuse("The native selection body was skipped.");
            return;
        }
        if (__state == null) return;
        __state.HasNativeResult = true;
        __state.NativeResult = __result;
    }
    [HarmonyPriority(int.MinValue)]
    private static Exception? Finalizer(Exception? __exception, bool __runOriginal, ref bool __result, Invocation? __state)
    {
        if (__exception != null || !__runOriginal || __state is not { HasNativeResult: true } invocation) return __exception;
        var route = invocation.Route;
        try
        {
            if (!ReferenceEquals(invocation.Receipt, route.Admitted()) || !route.groupAvailable()) return __exception;
            __result = invocation.NativeResult && __result && route.extraAllowed(invocation.Actor, invocation.Candidate);
        }
        catch (Exception error) { route.Refuse(error.GetBaseException().Message); }
        return __exception;
    }
    private void Refuse(string reason)
    {
        Failure = reason;
        try { unavailable(reason); } catch (Exception) { /* Owned diagnostics cannot replace a native result/exception. */ }
    }
    [HarmonyPriority(int.MinValue)]
    private static IEnumerable<CodeInstruction> Observe(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        if (Routes.TryGetValue(__originalMethod, out var route))
        {
            Volatile.Write(ref route.receipt, null); Volatile.Write(ref route.generation, null);
            try { Volatile.Write(ref route.generation, new(HarmonyPublication.ExpectedVersion(__originalMethod))); }
            catch (Exception error) { route.Failure = error.GetBaseException().Message; }
        }
        return instructions;
    }
    private static MethodInfo Callback(string name) => typeof(DrugToleranceDecisionPatch).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!;
}
