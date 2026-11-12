using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Megamod.Patching;

namespace Megamod.Features.DrugPolicyGeneration;

/// <summary>Current contribution-flow receipt only. It does not admit the called native helper bodies.</summary>
public sealed class DrugGenerationDecayAdmission
{
    private static readonly ConcurrentDictionary<MethodBase, DrugGenerationDecayAdmission> Routes = new();
    private static readonly MethodInfo Observer = typeof(DrugGenerationDecayAdmission).GetMethod(nameof(Observe), BindingFlags.NonPublic | BindingFlags.Static)!;
    private readonly MethodInfo target;
    private readonly DrugGenerationDecayGuard guard;
    private readonly Dictionary<MethodBase, Node> nodes = new();
    private sealed record Body(int Version, CodeInstruction[] Instructions);
    private bool installed;
    private sealed class Node
    {
        internal readonly OwnedHarmonyPatch Patch;
        internal Body? Body;
        internal Node(MethodBase method) => Patch = new(method, HarmonyPatchType.Transpiler, Observer, () => Owned(method));
    }

    public DrugGenerationDecayAdmission(MethodInfo target, DrugGenerationDecayGuard guard)
    {
        this.target = target; this.guard = guard;
        Candidate = new("DG-DECAY-CONTRIBUTION", "DG-NATIVE-FACTS", target.ToString()!, "C1 decay contribution proof",
            "no generation recommendation", Validate, Install, Rollback);
    }
    public PatchCandidate Candidate { get; }
    // Inspection only: acquisition belongs to Install/Revalidate on the admission thread.
    public bool Available => installed && Check(null);
    public bool Revalidate()
    {
        if (!installed) return false;
        var acquired = new List<MethodBase>();
        try
        {
            for (int round = 0; round < 32; round++)
            {
                if (!PruneUnreachable()) break;
                var missing = new HashSet<MethodBase>();
                if (Check(missing)) return true;
                if (missing.Count == 0) break;
                foreach (var method in missing)
                {
                    if (nodes.Count >= 32) throw new InvalidOperationException("Decay callback dependencies exceed 32 methods.");
                    acquired.Add(method); Acquire(method);
                }
            }
        }
        catch (Exception error) { Fail(target, error.GetBaseException().Message); }
        var failure = Failure;
        foreach (var method in acquired.AsEnumerable().Reverse())
            try { Remove(method); } catch (Exception error) { failure += " Cleanup: " + error.GetBaseException().Message; }
        Failure = failure; return false;
    }
    public string? Failure { get; private set; }
    private string? Validate() => DrugGenerationDecayGuard.Same(target, guard.Target) ? guard.ValidateShape() : "The decay guard is bound to a different target.";
    private void Install()
    {
        var failure = Validate(); if (failure != null) throw new InvalidOperationException(failure);
        Acquire(target); installed = true;
        if (!Revalidate()) throw new InvalidOperationException(Failure);
    }
    private void Acquire(MethodBase method)
    {
        if (nodes.ContainsKey(method)) return;
        if (!Routes.TryAdd(method, this)) throw new InvalidOperationException("The decay target/callback already has an owned observation route.");
        var node = new Node(method); nodes.Add(method, node); node.Patch.Install();
    }
    private bool Check(HashSet<MethodBase>? missing)
    {
        Failure = null; var receipts = new List<HarmonyPublication.Identity>(); var completed = new HashSet<MethodBase>();
        try
        {
            bool CheckNode(MethodBase method, HashSet<MethodBase> path)
            {
                if (completed.Contains(method)) return true;
                if (path.Count >= 8 || !path.Add(method)) return Fail(method, "Decay callback dependencies are recursive or exceed eight levels.");
                if (!nodes.TryGetValue(method, out var node)) { missing?.Add(method); return Fail(method, "A callback needs an observed body; revalidate admission."); }
                var body = node.Body; if (body == null) return Fail(method, "No composed body was observed.");
                HarmonyPublication.Snapshot? published = null;
                if (!HarmonyPublication.Inspect(method, snapshot => { published = snapshot; return true; })) return Fail(method, "The decay publication is busy.");
                var snapshot = published!;
                if (snapshot.Version != body.Version || !ReferenceEquals(body, node.Body) ||
                    snapshot.Transpilers.Any(p => p.PatchMethod.ReturnType == typeof(MethodInfo) || p.PatchMethod.ReturnType == typeof(DynamicMethod)) ||
                    snapshot.Transpilers.Count(p => p.owner == OwnedHarmonyPatch.OwnerId) != 1 ||
                    PatchProcessor.GetSortedPatchMethods(method, snapshot.Transpilers).LastOrDefault() != Observer)
                    return Fail(method, "The final composed observer publication changed.");
                var failure = method == target ? guard.Validate(body.Instructions) : DrugPolicyCreationBodyGuard.Validate(method, body.Instructions, null);
                if (failure != null) return Fail(method, failure);
                var callbacks = new HashSet<MethodInfo>();
                foreach (var callback in snapshot.Prefixes.Concat(snapshot.Postfixes).Concat(snapshot.Finalizers))
                {
                    if (callback.IsFactory || callback.UncheckedReferenceBinding || !CallbackShape(callback.Method))
                        return Fail(method, "An outer callback can skip, replace or write unproved state: " + callback.Method.Name);
                    callbacks.Add(callback.Method);
                }
                foreach (var callback in snapshot.InnerCallbacks(Operations(body)))
                {
                    if (callback.SelectedOperations.Count == 0) continue;
                    if (callback.IsFactory || callback.OuterBinding || !CallbackShape(callback.Method))
                        return Fail(method, "An inner callback can skip, replace or write unproved state: " + callback.Method.Name);
                    callbacks.Add(callback.Method);
                }
                receipts.Add(snapshot.Identity);
                foreach (var callback in callbacks) if (!CheckNode(callback, new HashSet<MethodBase>(path))) return false;
                completed.Add(method); return true;
            }
            return CheckNode(target, new HashSet<MethodBase>()) &&
                (receipts.All(receipt => receipt.IsCurrent()) || Fail(target, "A consumed publication changed during decay admission."));
        }
        catch (Exception error) { return Fail(target, error.GetBaseException().Message); }
    }

    private bool PruneUnreachable()
    {
        var reachable = new HashSet<MethodBase> { target }; var pending = new Queue<MethodBase>(reachable);
        var receipts = new List<HarmonyPublication.Identity>();
        while (pending.Count != 0)
        {
            var method = pending.Dequeue();
            if (!nodes.TryGetValue(method, out var node)) continue;
            var body = node.Body; if (body == null) return Fail(method, "No composed body was observed.");
            if (!HarmonyPublication.Inspect(method, snapshot =>
            {
                if (snapshot.Version != body.Version || !ReferenceEquals(body, node.Body)) return Fail(method, "The observed dependency publication changed.");
                var callbacks = snapshot.Prefixes.Concat(snapshot.Postfixes).Concat(snapshot.Finalizers).Select(callback => callback.Method)
                    .Concat(snapshot.InnerCallbacks(Operations(body)).Where(callback => callback.SelectedOperations.Count != 0).Select(callback => callback.Method));
                foreach (var callback in callbacks) if (reachable.Add(callback)) pending.Enqueue(callback);
                receipts.Add(snapshot.Identity); return true;
            })) return Failure != null ? false : Fail(method, "The dependency publication is busy.");
        }
        if (!receipts.All(receipt => receipt.IsCurrent())) return Fail(target, "A dependency publication changed before observer cleanup.");
        foreach (var method in nodes.Keys.Where(method => !reachable.Contains(method)).ToArray()) Remove(method);
        return true;
    }

    private static HarmonyPublication.Operation[] Operations(Body body) => body.Instructions.Select(i => new HarmonyPublication.Operation(i.opcode, i.operand)).ToArray();
    private static bool CallbackShape(MethodInfo callback) => callback.IsStatic && !callback.ContainsGenericParameters && callback.DeclaringType?.TypeInitializer == null &&
        callback.ReturnType == typeof(void) && callback.GetParameters().All(parameter => !parameter.ParameterType.IsByRef) && callback != Observer;
    private bool Fail(MethodBase method, string reason) { Failure = method.DeclaringType?.FullName + "." + method.Name + ": " + reason; return false; }
    private void Rollback()
    {
        installed = false; var errors = new List<Exception>();
        foreach (var method in nodes.Keys.Reverse().ToArray())
            try { Remove(method); } catch (Exception error) { errors.Add(error); }
        if (errors.Count != 0) throw new AggregateException("Decay observer cleanup failed.", errors);
    }
    private void Remove(MethodBase method)
    {
        if (!nodes.TryGetValue(method, out var node)) return;
        node.Patch.Rollback(); Routes.TryRemove(method, out _); nodes.Remove(method);
    }
    private static IReadOnlyList<Patch> Owned(MethodBase method)
    {
        Patch[]? owned = null;
        if (!HarmonyPublication.Inspect(method, snapshot => { owned = snapshot.Transpilers.Where(p => p.owner == OwnedHarmonyPatch.OwnerId).ToArray(); return true; }))
            throw new InvalidOperationException("The decay contribution publication is busy.");
        return owned!;
    }
    [HarmonyPriority(int.MinValue)]
    private static IEnumerable<CodeInstruction> Observe(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var incoming = instructions.ToArray();
        if (!Routes.TryGetValue(__originalMethod, out var route) || !route.nodes.TryGetValue(__originalMethod, out var node)) return incoming;
        node.Body = null;
        try { node.Body = new(HarmonyPublication.ExpectedVersion(__originalMethod), incoming.Select(i => new CodeInstruction(i)).ToArray()); }
        catch (Exception error) { route.Fail(__originalMethod, error.GetBaseException().Message); }
        return incoming;
    }
}
