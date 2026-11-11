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

// Passive target-local proof. It never invokes a layout/drawing method to obtain admission.
public sealed class DrugToleranceGeometryAdmission
{
    private static readonly ConcurrentDictionary<MethodBase, DrugToleranceGeometryAdmission> Routes = new();
    private static readonly MethodInfo Observer = typeof(DrugToleranceGeometryAdmission).GetMethod(nameof(Observe), BindingFlags.Static | BindingFlags.NonPublic)!;
    private readonly MethodInfo target;
    private readonly DrugToleranceGeometryGuard guard;
    private readonly OwnedHarmonyPatch patch;
    private bool installed;
    private Generation? generation;
    private Receipt? receipt;
    private sealed record Generation(int Version, IReadOnlyList<HarmonyPublication.Operation> Body, ISet<int> Required);
    private sealed record Receipt(Generation Generation, HarmonyPublication.Identity Identity);
    public DrugToleranceGeometryAdmission(string id, MethodInfo target, DrugToleranceGeometryGuard guard)
    {
        this.target = target; this.guard = guard;
        patch = new(target, HarmonyPatchType.Transpiler, Observer, Owned);
        Candidate = new("DT-GEOMETRY-" + id, "DT-POLICY-EDITOR", target.ToString()!,
            "tolerance editor and enforcement", "native policy editor; retained tolerance choices", Validate, Install, Rollback);
    }
    public PatchCandidate Candidate { get; }
    public string? Failure { get; private set; }
    public bool Available
    {
        get
        {
            var current = Volatile.Read(ref generation);
            if (!installed || current == null) return false;
            var known = Volatile.Read(ref receipt);
            if (ReferenceEquals(known?.Generation, current) && known!.Identity.IsCurrent()) return true;
            try
            {
                return HarmonyPublication.Inspect(target, snapshot =>
                {
                    if (snapshot.Version != current.Version || !ReferenceEquals(current, Volatile.Read(ref generation)) ||
                        snapshot.Transpilers.Any(p => p.PatchMethod.ReturnType == typeof(MethodInfo) || p.PatchMethod.ReturnType == typeof(DynamicMethod)) ||
                        snapshot.Transpilers.Count(p => p.owner == OwnedHarmonyPatch.OwnerId) != 1 ||
                        !snapshot.Transpilers.Any(p => p.owner == OwnedHarmonyPatch.OwnerId && p.PatchMethod == Observer) ||
                        PatchProcessor.GetSortedPatchMethods(target, snapshot.Transpilers).LastOrDefault() != Observer)
                    { Failure = "The native geometry observer is not the current final publication."; return false; }
                    if (snapshot.Prefixes.Concat(snapshot.Postfixes).Concat(snapshot.Finalizers).Any(p =>
                        p.IsFactory || p.UncheckedReferenceBinding || p.Method.ReturnType != typeof(void) ||
                        p.Method.GetParameters().Any(WritesGeometry)))
                    { Failure = "An outer native geometry callback can skip or replace the consumed width."; return false; }
                    Failure = snapshot.FailureFor(current.Body, current.Required);
                    if (Failure != null) return false;
                    foreach (var callback in snapshot.InnerCallbacks(current.Body))
                        foreach (int index in callback.SelectedOperations)
                            if (!current.Required.Contains(index) && current.Body[index].Operand is MemberInfo member && guard.HasGeometryReceiver(member) &&
                                (callback.IsFactory || callback.Method.GetParameters().Any(parameter => WritesGeometryReceiver(member, parameter))))
                            { Failure = "An inner callback can write its containing geometry value."; return false; }
                    Volatile.Write(ref receipt, new(current, snapshot.Identity)); return true;
                });
            }
            catch (Exception error) { Failure = error.GetBaseException().Message; return false; }
        }
    }
    private bool WritesGeometry(ParameterInfo parameter)
    {
        var binding = DrugToleranceArgumentBinding.Resolve(target, parameter);
        return binding.Name is "__args" or "__instance" || parameter.ParameterType.IsByRef && binding.Name != "__state";
    }
    private bool WritesGeometryReceiver(MemberInfo member, ParameterInfo parameter)
    {
        // Field selectors have no ordinary arguments; the outer target is used only to decode
        // attribute aliases for special bindings, never to authorize a field-selector argument.
        var binding = DrugToleranceArgumentBinding.Resolve(member as MethodInfo ?? target, parameter);
        if (binding.Name == "__state") return false;
        if (binding.Name == "__result" || member is MethodInfo && binding.Index >= 0)
            return parameter.ParameterType != typeof(float) && parameter.ParameterType != typeof(float).MakeByRefType();
        if (binding.Name == "__instance") return parameter.ParameterType.IsByRef || !parameter.ParameterType.IsValueType;
        return binding.Name == "__args" || parameter.ParameterType.IsByRef;
    }
    private string? Validate() => target.IsAbstract || target.ContainsGenericParameters || target.GetMethodBody() == null ?
        "The native geometry target has no closed managed body." : patch.ValidateOwnership();
    private void Install()
    {
        if (!Routes.TryAdd(target, this)) throw new InvalidOperationException("The native geometry target already has an owned route.");
        patch.Install(); installed = true;
        if (!Available) throw new InvalidOperationException(Failure ?? "Native geometry is unproved.");
    }
    private void Rollback()
    {
        installed = false; Volatile.Write(ref receipt, null); Volatile.Write(ref generation, null);
        try { patch.Rollback(); }
        finally { if (Routes.TryGetValue(target, out var owner) && ReferenceEquals(owner, this)) Routes.TryRemove(target, out _); }
    }
    private IReadOnlyList<Patch> Owned()
    {
        Patch[]? result = null;
        if (!HarmonyPublication.Inspect(target, snapshot =>
        { result = snapshot.Transpilers.Where(p => p.owner == OwnedHarmonyPatch.OwnerId).ToArray(); return true; }))
            throw new InvalidOperationException("Native geometry ownership publication is busy.");
        return result!;
    }
    [HarmonyPriority(int.MinValue)]
    private static IEnumerable<CodeInstruction> Observe(IEnumerable<CodeInstruction> source, MethodBase __originalMethod)
    {
        var body = source.ToList();
        if (!Routes.TryGetValue(__originalMethod, out var owner)) return body;
        Volatile.Write(ref owner.receipt, null); Volatile.Write(ref owner.generation, null);
        try
        {
            var proof = owner.guard.Describe(body);
            owner.Failure = proof.Failure;
            if (proof.Failure == null && proof.Required != null)
                Volatile.Write(ref owner.generation, new(HarmonyPublication.ExpectedVersion(__originalMethod),
                    body.Select(i => new HarmonyPublication.Operation(i.opcode, i.operand)).ToArray(), proof.Required));
        }
        catch (Exception error) { owner.Failure = error.GetBaseException().Message; }
        return body;
    }
}
