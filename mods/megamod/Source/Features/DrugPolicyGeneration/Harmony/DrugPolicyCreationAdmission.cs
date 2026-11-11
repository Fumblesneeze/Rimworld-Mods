using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Megamod.Patching;

namespace Megamod.Features.DrugPolicyGeneration;

public sealed class DrugPolicyCreationAdmission
{
    // Install/Revalidate/Rollback belong to the game-thread admission lifecycle. Available is
    // inspection only; it never installs observers or executes a foreign composition as a probe.
    private static readonly ConcurrentDictionary<MethodBase, DrugPolicyCreationAdmission> Routes = new();
    private static readonly MethodInfo Observer = typeof(DrugPolicyCreationAdmission).GetMethod(nameof(Observe), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly ConstructorInfo ObjectConstructor = typeof(object).GetConstructor(Type.EmptyTypes)!;
    private readonly MethodBase[] roots;
    private readonly FieldInfo policies;
    private readonly Dictionary<MethodBase, Node> nodes = new();
    private bool installed;
    private sealed record Body(int Version, CodeInstruction[] Instructions);
    private sealed class Node
    {
        internal readonly OwnedHarmonyPatch Patch;
        internal Body? Body;
        internal Node(MethodBase target) => Patch = new(target, HarmonyPatchType.Transpiler, Observer, () => Owned(target));
    }

    public DrugPolicyCreationAdmission(ConstructorInfo policy, ConstructorInfo basePolicy, ConstructorInfo entry,
        MethodInfo allPolicies, FieldInfo policies)
    {
        roots = new MethodBase[] { policy, basePolicy, entry, allPolicies }; this.policies = policies;
        Candidate = new("DG-POLICY-CREATE", "DG-POLICY-CREATE", policy.ToString()!, "Generate", "native policy controls",
            Validate, Install, Rollback);
    }
    public PatchCandidate Candidate { get; }
    public string? Failure { get; private set; }
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
                foreach (var target in missing)
                {
                    if (nodes.Count >= 32) throw new InvalidOperationException("Creation callback dependencies exceed 32 methods.");
                    acquired.Add(target); Acquire(target);
                }
            }
        }
        catch (Exception error) { Failure = error.GetBaseException().Message; }
        var failure = Failure;
        foreach (var target in acquired.AsEnumerable().Reverse())
            try { Remove(target); } catch (Exception error) { failure += " Cleanup: " + error.GetBaseException().Message; }
        Failure = failure; return false;
    }

    private bool PruneUnreachable()
    {
        var reachable = new HashSet<MethodBase>(roots); var pending = new Queue<MethodBase>(roots);
        var receipts = new List<HarmonyPublication.Identity>();
        while (pending.Count != 0)
        {
            var target = pending.Dequeue();
            // New dependencies reveal their children only after observer acquisition. An old
            // dependency removed here can be reacquired in that subsequent round.
            if (!nodes.TryGetValue(target, out var node)) continue;
            var body = node.Body;
            if (body == null) return Fail(target, "No composed body was observed.");
            if (!HarmonyPublication.Inspect(target, snapshot =>
            {
                if (snapshot.Version != body.Version || !ReferenceEquals(body, node.Body)) return Fail(target, "The observed dependency publication changed.");
                var methods = snapshot.Prefixes.Concat(snapshot.Postfixes).Concat(snapshot.Finalizers).Select(callback => callback.Method)
                    .Concat(snapshot.InnerCallbacks(body.Instructions.Select(i => new HarmonyPublication.Operation(i.opcode, i.operand)).ToArray())
                        .Where(callback => callback.SelectedOperations.Count != 0).Select(callback => callback.Method));
                foreach (var method in methods) if (reachable.Add(method)) pending.Enqueue(method);
                receipts.Add(snapshot.Identity); return true;
            })) return Failure != null ? false : Fail(target, "The dependency publication is busy.");
        }
        if (!receipts.All(receipt => receipt.IsCurrent())) return Fail(roots[0], "A dependency publication changed before observer cleanup.");
        foreach (var target in nodes.Keys.Where(target => !reachable.Contains(target)).ToArray()) Remove(target);
        return true;
    }

    private string? Validate()
    {
        if (roots.Any(m => m.DeclaringType?.TypeInitializer != null) || roots.Distinct().Count() != roots.Length || roots.Take(3).Any(m => m.IsStatic || m.ContainsGenericParameters || m.GetParameters().Length != 0) ||
            roots[0].DeclaringType?.BaseType != roots[1].DeclaringType || roots[1].DeclaringType?.BaseType != typeof(object) ||
            roots[2].DeclaringType?.BaseType != typeof(object) || roots[3] is not MethodInfo getter || getter.IsStatic || getter.IsGenericMethod || getter.GetParameters().Length != 0 ||
            policies.IsStatic || policies.DeclaringType != getter.DeclaringType || getter.ReturnType != policies.FieldType ||
            !policies.FieldType.IsGenericType || policies.FieldType.GetGenericTypeDefinition() != typeof(List<>) || policies.FieldType.GetGenericArguments()[0] != roots[0].DeclaringType)
            return "The native constructor/getter callable or actual-list field shape changed.";
        return null;
    }

    private void Install()
    {
        var failure = Validate();
        if (failure != null) throw new InvalidOperationException(failure);
        foreach (var target in roots) Acquire(target);
        installed = true;
        if (!Revalidate()) throw new InvalidOperationException(Failure);
    }

    private void Acquire(MethodBase target)
    {
        if (nodes.ContainsKey(target)) return;
        if (!Routes.TryAdd(target, this)) throw new InvalidOperationException("The constructor/getter already has an owned observation route.");
        var node = new Node(target); nodes.Add(target, node);
        node.Patch.Install();
    }

    private bool Check(HashSet<MethodBase>? missing)
    {
        Failure = null;
        var receipts = new List<HarmonyPublication.Identity>();
        try
        {
            if (!HarmonyPublication.Inspect(ObjectConstructor, snapshot =>
            {
                if (snapshot.Transpilers.Length != 0 || snapshot.Prefixes.Count + snapshot.Postfixes.Count + snapshot.Finalizers.Count != 0 ||
                    snapshot.InnerCallbacks(Array.Empty<HarmonyPublication.Operation>()).Count != 0 ||
                    PatchProcessor.GetOriginalInstructions(ObjectConstructor).Any(i => i.opcode != OpCodes.Nop && i.opcode != OpCodes.Ret))
                    return Fail(ObjectConstructor, "The root object constructor has unproved effects.");
                receipts.Add(snapshot.Identity); return true;
            })) return Failure != null ? false : Fail(ObjectConstructor, "The root constructor publication is busy.");
            var completed = new HashSet<MethodBase>();
            bool CheckNode(MethodBase target, HashSet<MethodBase> path)
            {
                if (completed.Contains(target)) return true;
                if (path.Count >= 8 || !path.Add(target)) return Fail(target, "Creation callback dependencies are recursive or exceed eight levels.");
                if (!nodes.TryGetValue(target, out var node)) { missing?.Add(target); return Fail(target, "A callback requires an observed composed body; revalidate admission."); }
                if (node.Body == null) return Fail(target, "No composed body was observed.");
                var body = node.Body;
                HarmonyPublication.Snapshot? published = null;
                if (!HarmonyPublication.Inspect(target, snapshot => { published = snapshot; return true; })) return Fail(target, "The creation publication is busy.");
                var snapshot = published!;
                if (snapshot.Version != body.Version || !ReferenceEquals(body, node.Body) ||
                    snapshot.Transpilers.Any(p => p.PatchMethod.ReturnType == typeof(MethodInfo) || p.PatchMethod.ReturnType == typeof(DynamicMethod)) ||
                    snapshot.Transpilers.Count(p => p.owner == OwnedHarmonyPatch.OwnerId) != 1 ||
                    PatchProcessor.GetSortedPatchMethods(target, snapshot.Transpilers).LastOrDefault() != Observer)
                    return Fail(target, "The final composed observer publication changed.");
                var failure = DrugPolicyCreationBodyGuard.Validate(target, body.Instructions, target == roots[3] ? policies : null);
                if (failure != null) return Fail(target, failure);
                var callbacks = new HashSet<MethodInfo>();
                foreach (var callback in snapshot.Prefixes.Concat(snapshot.Postfixes).Concat(snapshot.Finalizers))
                {
                    if (callback.IsFactory || callback.UncheckedReferenceBinding || !CallbackShape(callback.Method))
                        return Fail(target, "An outer callback can skip, replace or write unproved native state: " + callback.Method.Name);
                    callbacks.Add(callback.Method);
                }
                foreach (var callback in snapshot.InnerCallbacks(body.Instructions.Select(i => new HarmonyPublication.Operation(i.opcode, i.operand)).ToArray()))
                {
                    if (callback.SelectedOperations.Count == 0) continue;
                    if (callback.IsFactory || callback.OuterBinding || !CallbackShape(callback.Method))
                        return Fail(target, "An inner callback can skip, replace or write unproved native state: " + callback.Method.Name);
                    callbacks.Add(callback.Method);
                }
                receipts.Add(snapshot.Identity);
                foreach (var callback in callbacks) if (!CheckNode(callback, new HashSet<MethodBase>(path))) return false;
                completed.Add(target); return true;
            }
            foreach (var target in roots) if (!CheckNode(target, new HashSet<MethodBase>())) return false;
            return receipts.All(receipt => receipt.IsCurrent()) || Fail(roots[0], "A consumed publication changed during creation admission.");
        }
        catch (Exception error) { Failure = error.GetBaseException().Message; return false; }
    }

    private static bool CallbackShape(MethodInfo callback) => callback.IsStatic && !callback.ContainsGenericParameters && callback.DeclaringType?.TypeInitializer == null &&
        callback.ReturnType == typeof(void) && callback.GetParameters().All(parameter => !parameter.ParameterType.IsByRef) && callback != Observer;

    private bool Fail(MethodBase target, string reason) { Failure = target.DeclaringType?.FullName + "." + target.Name + ": " + reason; return false; }
    private void Rollback()
    {
        installed = false;
        var errors = new List<Exception>();
        foreach (var target in nodes.Keys.Reverse().ToArray())
            try { Remove(target); } catch (Exception error) { errors.Add(error); }
        if (errors.Count != 0) throw new AggregateException("Creation observer cleanup failed.", errors);
    }
    private void Remove(MethodBase target)
    {
        if (!nodes.TryGetValue(target, out var node)) return;
        node.Patch.Rollback();
        Routes.TryRemove(target, out _); nodes.Remove(target);
    }
    private static IReadOnlyList<Patch> Owned(MethodBase target)
    {
        Patch[]? owned = null;
        if (!HarmonyPublication.Inspect(target, snapshot => { owned = snapshot.Transpilers.Where(p => p.owner == OwnedHarmonyPatch.OwnerId).ToArray(); return true; }))
            throw new InvalidOperationException("The constructor/getter publication is busy.");
        return owned!;
    }
    [HarmonyPriority(int.MinValue)]
    private static IEnumerable<CodeInstruction> Observe(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var body = instructions.ToArray();
        if (!Routes.TryGetValue(__originalMethod, out var route) || !route.nodes.TryGetValue(__originalMethod, out var node)) return body;
        node.Body = null;
        try { node.Body = new(HarmonyPublication.ExpectedVersion(__originalMethod), body.Select(i => new CodeInstruction(i)).ToArray()); }
        catch (Exception error) { route.Fail(__originalMethod, error.GetBaseException().Message); }
        return body;
    }
}
