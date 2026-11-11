using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using Megamod.Features.DrugTolerance.Core;
using Megamod.Patching;

namespace Megamod.Features.DrugTolerance;

public sealed class DrugTolerancePersistencePatch
{
    private static readonly ConcurrentDictionary<MethodBase, Route> Routes = new();
    private static readonly MethodInfo CopyPrefixMethod = Callback(nameof(CopyPrefix));
    private static readonly MethodInfo ExposePrefixMethod = Callback(nameof(ExposePrefix));
    private static readonly MethodInfo FinalizerMethod = Callback(nameof(Finalizer));
    private static readonly MethodInfo GenerationMethod = Callback(nameof(ObserveGeneration));
    private static readonly MethodInfo FinalizePrefixMethod = Callback(nameof(FinalizePrefix));
    private static readonly MethodInfo FinalizeGuardMethod = Callback(nameof(FinalizeGuard));
    private readonly Func<object, DrugToleranceChoice> read;
    private readonly Action<object, DrugToleranceChoice> write;
    private readonly Action<object> expose;
    private readonly Action<string> report;
    private readonly Route copy, scribe, saveFinalize, loadFinalize;
    private volatile bool enabled;
    private int faulted;
    private Exception? persistenceFailure;

    public DrugTolerancePersistencePatch(Type entryType, Type saverType, Type loaderType, Func<object, DrugToleranceChoice> read,
        Action<object, DrugToleranceChoice> write, Action<object> expose, Action<string> report)
    {
        this.read = read; this.write = write; this.expose = expose; this.report = report;
        copy = new(this, entryType, "CopyFrom", "DT-POLICY-COPY", new[] { entryType }, CopyPrefixMethod);
        scribe = new(this, entryType, "ExposeData", "DT-POLICY-SCRIBE", Type.EmptyTypes, ExposePrefixMethod);
        saveFinalize = new(this, saverType, "FinalizeSaving", "DT-SAVE-FINALIZE", Type.EmptyTypes, FinalizePrefixMethod, true);
        loadFinalize = new(this, loaderType, "FinalizeLoading", "DT-LOAD-FINALIZE", Type.EmptyTypes, FinalizePrefixMethod, true);
        Group = new("drug-tolerance.persistence", AdmissionPhase.Early,
            new[] { saveFinalize.Candidate, loadFinalize.Candidate, copy.Candidate, scribe.Candidate });
    }

    public PatchGroup Group { get; }
    public bool CanUseChoices => enabled && Volatile.Read(ref faulted) == 0 && copy.ChoiceUsable() &&
        scribe.ChoiceUsable() && saveFinalize.ChoiceUsable() && loadFinalize.ChoiceUsable();
    public bool Available => Volatile.Read(ref faulted) == 0 && copy.Admitted(force: true) && scribe.Admitted(force: true) &&
        saveFinalize.Admitted(force: true) && loadFinalize.Admitted(force: true);
    public void Enable()
    {
        if (!Available) throw new InvalidOperationException("Tolerance policy persistence is unavailable.");
        enabled = true;
    }

    private sealed record Generation(int Version);
    private sealed record Invocation(Route Route, object Receiver, DrugToleranceChoice? Choice, Generation Generation);

    private sealed class Route
    {
        internal readonly DrugTolerancePersistencePatch Owner;
        internal readonly PatchCandidate Candidate;
        internal readonly string Id;
        private readonly Type entryType;
        private readonly Type[] parameters;
        private readonly string methodName;
        private readonly MethodInfo prefixMethod;
        private readonly MethodInfo finalizerMethod;
        private readonly bool finalizationGuard;
        private MethodInfo? target;
        private OwnedHarmonyPatch? prefix, finalizer, generationObserver;
        private volatile bool installed;
        private Generation? generation, admitted;
        private HarmonyPublication.Identity? identity;
        internal string? Failure;

        internal Route(DrugTolerancePersistencePatch owner, Type entryType, string methodName, string id,
            Type[] parameters, MethodInfo prefixMethod, bool finalizationGuard = false)
        {
            Owner = owner; this.entryType = entryType; this.methodName = methodName;
            Id = id; this.parameters = parameters; this.prefixMethod = prefixMethod;
            this.finalizationGuard = finalizationGuard;
            finalizerMethod = finalizationGuard ? FinalizeGuardMethod : FinalizerMethod;
            Candidate = new(id, "DT-POLICY-PERSISTENCE", entryType.FullName + "." + methodName,
                "owned drug-tolerance choice copy/save persistence", "native policy behavior; tolerance editor/enforcement unavailable",
                Validate, Install, Rollback);
        }

        private string? Validate()
        {
            target = entryType.GetMethod(methodName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
                null, parameters, null);
            if (!entryType.IsClass || target == null || target.ReturnType != typeof(void) ||
                target.IsGenericMethod || target.ContainsGenericParameters)
                return "The required public instance nongeneric policy-entry callable changed.";
            prefix ??= new(target, HarmonyPatchType.Prefix, prefixMethod, () => OwnedSlot(HarmonyPatchType.Prefix));
            finalizer ??= new(target, HarmonyPatchType.Finalizer, finalizerMethod, () => OwnedSlot(HarmonyPatchType.Finalizer));
            generationObserver ??= new(target, HarmonyPatchType.Transpiler, GenerationMethod, () => OwnedSlot(HarmonyPatchType.Transpiler));
            return prefix.ValidateOwnership() ?? finalizer?.ValidateOwnership() ?? generationObserver.ValidateOwnership();
        }

        private void Install()
        {
            if (target == null || prefix == null || generationObserver == null)
                throw new InvalidOperationException("Persistence target was not validated.");
            if (!Routes.TryAdd(target, this)) throw new InvalidOperationException("Persistence target already has an owner.");
            prefix.Install();
            finalizer?.Install();
            generationObserver.Install();
            installed = true;
            if (!Admitted(force: true)) throw new InvalidOperationException(Failure ?? "Persistence ordering is unavailable.");
        }

        private void Rollback()
        {
            Owner.enabled = false;
            installed = false;
            Volatile.Write(ref admitted, null);
            var failures = new List<Exception>();
            try { generationObserver?.Rollback(); } catch (Exception error) { failures.Add(error); }
            try { finalizer?.Rollback(); } catch (Exception error) { failures.Add(error); }
            try { prefix?.Rollback(); } catch (Exception error) { failures.Add(error); }
            if (target != null && Routes.TryGetValue(target, out var current) && ReferenceEquals(current, this))
                Routes.TryRemove(target, out _);
            if (failures.Count != 0) throw new AggregateException("Tolerance persistence cleanup failed.", failures);
        }

        internal bool ChoiceUsable()
        {
            var current = Volatile.Read(ref generation);
            if (current != null && ReferenceEquals(current, Volatile.Read(ref admitted)) &&
                Volatile.Read(ref identity)?.IsCurrent() == true) return true;
            return Admitted(force: true, choiceUse: true);
        }

        internal bool Admitted(bool force = false, bool choiceUse = false)
        {
            var current = Volatile.Read(ref generation);
            if (!installed || current == null || Volatile.Read(ref Owner.faulted) != 0) return false;
            if (!force && ReferenceEquals(current, Volatile.Read(ref admitted))) return true;
            // A busy choice-use query must not revoke an otherwise healthy serialization receipt.
            // Native copy/Scribe calls retain their stricter original admission/failure behavior.
            if (!choiceUse) Volatile.Write(ref admitted, null);
            bool examined = false;
            try
            {
                var inspected = HarmonyPublication.Inspect(target!, snapshot =>
                {
                    examined = true;
                    if (snapshot.Version != current.Version || !ReferenceEquals(current, Volatile.Read(ref generation)))
                    { Failure = "The compiled persistence generation no longer matches publication."; return false; }
                    if (snapshot.Prefixes.Any(patch => patch.IsFactory) || snapshot.Finalizers.Any(patch => patch.IsFactory))
                    { Failure = "A prefix/finalizer factory prevents safe persistence ordering inspection."; return false; }
                    if (snapshot.Prefixes.Count(p => p.Owner == OwnedHarmonyPatch.OwnerId && p.Method == prefixMethod) != 1 ||
                        snapshot.Finalizers.Count(p => p.Owner == OwnedHarmonyPatch.OwnerId && p.Method == finalizerMethod) != 1 ||
                        snapshot.Transpilers.Count(p => p.owner == OwnedHarmonyPatch.OwnerId && p.PatchMethod == GenerationMethod) != 1)
                    { Failure = "An owned persistence callback is absent or duplicated."; return false; }
                    var prefixes = PatchProcessor.GetSortedPatchMethods(target!, snapshot.Prefixes.Select(p => p.ToOrderingPatch()).ToArray());
                    var finalizers = PatchProcessor.GetSortedPatchMethods(target!, snapshot.Finalizers.Select(p => p.ToOrderingPatch()).ToArray());
                    if (finalizationGuard && (prefixes.FirstOrDefault() != prefixMethod ||
                        snapshot.Finalizers.Where(p => p.Owner != OwnedHarmonyPatch.OwnerId || p.Method != finalizerMethod)
                            .Any(p => p.Method.ReturnType != typeof(void) || p.Method.GetParameters().Any(parameter => parameter.ParameterType.IsByRef))))
                    { Failure = "The finalization guard must run first and its abort exception cannot be changed by foreign finalizers."; return false; }
                    if ((!finalizationGuard && prefixes.LastOrDefault() != prefixMethod) || finalizers.FirstOrDefault() != finalizerMethod)
                    { Failure = "Persistence requires its last prefix and first finalizer around the native call."; return false; }
                    Volatile.Write(ref identity, snapshot.Identity);
                    Volatile.Write(ref admitted, current);
                    Failure = null;
                    return true;
                });
                if (!inspected)
                {
                    if (examined)
                    {
                        Volatile.Write(ref admitted, null);
                        if (choiceUse) Owner.Fail(this, Failure ?? "The owned persistence publication changed.");
                    }
                    else if (!choiceUse) Failure ??= "Persistence publication metadata is busy or unavailable.";
                }
                return inspected;
            }
            catch (Exception error)
            {
                // Inspect returns false for transient monitor contention. An exception instead
                // means the published metadata could not be safely read; no old receipt survives.
                Volatile.Write(ref admitted, null);
                Failure = error.GetBaseException().Message;
                if (choiceUse) Owner.Fail(this, Failure);
                return false;
            }
        }

        internal Invocation? Begin(object receiver, object? source)
        {
            if (!Owner.enabled || Volatile.Read(ref Owner.faulted) != 0) return null;
            if (!Owner.CheckSupport()) return null;
            var token = Volatile.Read(ref admitted)!;
            // Null copy sources are native responsibility, never an ExposeData invocation.
            if (parameters.Length != 0 && source == null) return null;
            var choice = source == null ? null : Owner.read(source);
            if (parameters.Length != 0 && choice == null) throw new InvalidOperationException("The owned source choice was absent.");
            return new(this, receiver, choice, token);
        }

        internal bool Matches(Generation expected) =>
            ReferenceEquals(expected, Volatile.Read(ref generation)) && Admitted();

        internal void Compiling()
        {
            Volatile.Write(ref admitted, null);
            Volatile.Write(ref generation, null);
            try { Volatile.Write(ref generation, new Generation(HarmonyPublication.ExpectedVersion(target!))); }
            catch (Exception error) { Failure = error.GetBaseException().Message; }
        }

        private IReadOnlyList<Patch> OwnedSlot(HarmonyPatchType kind)
        {
            Patch[]? owned = null;
            if (!HarmonyPublication.Inspect(target!, snapshot =>
            {
                owned = kind == HarmonyPatchType.Transpiler
                    ? snapshot.Transpilers.Where(p => p.owner == OwnedHarmonyPatch.OwnerId).ToArray()
                    : (kind == HarmonyPatchType.Prefix ? snapshot.Prefixes : snapshot.Finalizers)
                        .Where(p => p.Owner == OwnedHarmonyPatch.OwnerId).Select(p => p.ToOrderingPatch()).ToArray();
                return true;
            })) throw new InvalidOperationException("Persistence ownership metadata is busy or unavailable.");
            return owned!;
        }
    }

    [HarmonyPriority(int.MinValue)]
    private static void CopyPrefix(MethodBase __originalMethod, object __instance, object __0, out Invocation? __state)
    {
        __state = null;
        if (!Routes.TryGetValue(__originalMethod, out var route)) return;
        try { __state = route.Begin(__instance, __0); }
        catch (Exception error) { route.Owner.Fail(route, error.GetBaseException().Message); }
    }

    [HarmonyPriority(int.MinValue)]
    private static void ExposePrefix(MethodBase __originalMethod, object __instance, out Invocation? __state)
    {
        __state = null;
        if (!Routes.TryGetValue(__originalMethod, out var route)) return;
        try { __state = route.Begin(__instance, null); }
        catch (Exception error) { route.Owner.Fail(route, error.GetBaseException().Message); }
    }

    [HarmonyPriority(int.MaxValue)]
    private static Exception? Finalizer(Exception? __exception, bool __runOriginal, Invocation? __state)
    {
        if (__state is not { } invocation) return __exception;
        var route = invocation.Route;
        var owner = route.Owner;
        try
        {
            if (!owner.CheckSupport() || !route.Matches(invocation.Generation))
                owner.Fail(route, route.Failure ?? "Persistence generation changed during the native call.");
            else if (__runOriginal && __exception == null && owner.enabled && Volatile.Read(ref owner.faulted) == 0)
            {
                if (invocation.Choice != null) owner.write(invocation.Receiver, invocation.Choice);
                else owner.expose(invocation.Receiver);
            }
        }
        catch (Exception error)
        {
            owner.Fail(route, error.GetBaseException().Message);
            // Row serialization callers may swallow this. The sticky finalization guard still
            // aborts the enclosing save/load instead of accepting a file with missing owned data.
            if (invocation.Choice == null && __exception == null) return error;
        }
        return __exception;
    }

    [HarmonyPriority(int.MaxValue)]
    private static void FinalizePrefix(MethodBase __originalMethod)
    {
        if (!Routes.TryGetValue(__originalMethod, out var route)) return;
        if (route.Owner.FinalizationFailure() is { } failure) throw failure;
    }

    [HarmonyPriority(int.MaxValue)]
    private static Exception? FinalizeGuard(MethodBase __originalMethod, Exception? __exception)
    {
        if (!Routes.TryGetValue(__originalMethod, out var route)) return __exception;
        // The native loader may first encounter and swallow an owned error in its cross-reference
        // or post-load phases inside the body. Check again before reporting successful completion.
        var failure = route.Owner.FinalizationFailure();
        return __exception ?? failure;
    }

    private Exception? FinalizationFailure()
    {
        if (enabled && Volatile.Read(ref faulted) == 0) CheckSupport();
        return Volatile.Read(ref faulted) == 0 ? null : new InvalidOperationException(
            "Owned drug-tolerance state could not be preserved. Saving/loading was aborted; restart before retrying.",
            Volatile.Read(ref persistenceFailure));
    }

    [HarmonyPriority(int.MinValue)]
    private static IEnumerable<CodeInstruction> ObserveGeneration(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        if (Routes.TryGetValue(__originalMethod, out var route)) route.Compiling();
        return instructions;
    }

    private void Fail(Route route, string reason)
    {
        enabled = false;
        if (Interlocked.Exchange(ref faulted, 1) != 0) return;
        string bounded = new(reason.Take(256).Select(c => char.IsControl(c) ? ' ' : c).ToArray());
        Volatile.Write(ref persistenceFailure, new InvalidOperationException(bounded));
        try { report("[fumblesneeze.megamod] Patch " + route.Id + " unavailable; target=" + route.Candidate.Target +
            "; targetSpec=DT-POLICY-PERSISTENCE; reason=" + bounded +
            "; unavailable=owned tolerance persistence; fallback=tolerance editor/enforcement unavailable; save/load aborted until restart."); }
        catch (Exception) { /* A diagnostic cannot replace a native policy exception/result. */ }
    }

    private bool CheckSupport()
    {
        if (!copy.Admitted()) { Fail(copy, copy.Failure ?? "Copy support is unavailable."); return false; }
        if (!scribe.Admitted()) { Fail(scribe, scribe.Failure ?? "Scribe support is unavailable."); return false; }
        if (!saveFinalize.Admitted()) { Fail(saveFinalize, saveFinalize.Failure ?? "Save finalization support is unavailable."); return false; }
        if (!loadFinalize.Admitted()) { Fail(loadFinalize, loadFinalize.Failure ?? "Load finalization support is unavailable."); return false; }
        return true;
    }

    private static MethodInfo Callback(string name) => typeof(DrugTolerancePersistencePatch).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!;
}
