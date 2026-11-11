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

public sealed class DrugToleranceAllocationAdmission
{
    private static readonly ConcurrentDictionary<MethodBase, DrugToleranceAllocationAdmission> Routes = new();
    private static readonly MethodInfo Observer = typeof(DrugToleranceAllocationAdmission).GetMethod(nameof(Observe), BindingFlags.Static | BindingFlags.NonPublic)!;
    private readonly MethodInfo target;
    private readonly DrugToleranceColumnWidthGuard guard;
    private readonly OwnedHarmonyPatch observer;
    private bool installed;
    private Generation? generation;
    private Receipt? receipt;
    private sealed record Generation(int Version, IReadOnlyList<HarmonyPublication.Operation> Body, ISet<int> Protected);
    private sealed record Receipt(Generation Generation, HarmonyPublication.Identity Identity);
    public DrugToleranceAllocationAdmission(MethodInfo calculator, DrugToleranceColumnWidthGuard guard)
    {
        target = calculator; this.guard = guard;
        observer = new(target, HarmonyPatchType.Transpiler, Observer, Owned);
        Candidate = new("DT-COLUMN-ALLOCATION", "DT-POLICY-EDITOR", calculator.ToString()!, "tolerance editor and enforcement", "native policy editor", Validate, Install, Rollback);
    }
    public PatchCandidate Candidate { get; }
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
                    { Failure = "The calculator observer is not the final published generation."; return false; }
                    // Effective __args is mutable even when injected as object/an interface. Private
                    // __state belongs to the callback pair and does not replace native allocation.
                    if (snapshot.Prefixes.Concat(snapshot.Postfixes).Concat(snapshot.Finalizers).Any(p => p.IsFactory ||
                        p.Method.ReturnType != typeof(void) || p.Method.GetParameters().Any(WritesAllocation)))
                    { Failure = "An outer calculator callback can replace or skip the consumed allocation."; return false; }
                    Failure = snapshot.FailureFor(current.Body, current.Protected);
                    if (Failure != null) return false;
                    Volatile.Write(ref receipt, new(current, snapshot.Identity)); return true;
                });
            }
            catch (Exception error) { Failure = error.GetBaseException().Message; return false; }
        }
    }
    public string? Failure { get; private set; }
    private bool WritesAllocation(ParameterInfo parameter)
    {
        var binding = DrugToleranceArgumentBinding.Resolve(target, parameter);
        return binding.Name == "__args" || parameter.ParameterType.IsByRef && binding.Name != "__state";
    }
    private string? Validate()
    {
        var p = target.GetParameters();
        return target.IsStatic || target.IsGenericMethod || target.ReturnType != typeof(void) || p.Length != 10 ||
            !p[0].ParameterType.IsValueType || p.Skip(1).Any(x => x.ParameterType != typeof(float).MakeByRefType())
            ? "The native calculator callable changed." : observer.ValidateOwnership();
    }
    private void Install()
    {
        if (!Routes.TryAdd(target, this)) throw new InvalidOperationException("The calculator already has an owned route.");
        observer.Install(); installed = true;
        if (!Available) throw new InvalidOperationException(Failure ?? "The native column allocation is unavailable.");
    }
    private void Rollback()
    {
        installed = false; Volatile.Write(ref receipt, null); Volatile.Write(ref generation, null);
        try { observer.Rollback(); }
        finally { if (Routes.TryGetValue(target, out var current) && ReferenceEquals(current, this)) Routes.TryRemove(target, out _); }
    }
    private IReadOnlyList<Patch> Owned()
    {
        Patch[]? patches = null;
        if (!HarmonyPublication.Inspect(target, snapshot => { patches = snapshot.Transpilers.Where(p => p.owner == OwnedHarmonyPatch.OwnerId).ToArray(); return true; }))
            throw new InvalidOperationException("Calculator ownership publication is busy.");
        return patches!;
    }
    [HarmonyPriority(int.MinValue)]
    private static IEnumerable<CodeInstruction> Observe(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var body = instructions.ToList();
        if (!Routes.TryGetValue(__originalMethod, out var route)) return body;
        Volatile.Write(ref route.receipt, null); Volatile.Write(ref route.generation, null);
        try
        {
            route.Failure = route.guard.Validate(body);
            if (route.Failure == null) Volatile.Write(ref route.generation, new(HarmonyPublication.ExpectedVersion(__originalMethod),
                body.Select(i => new HarmonyPublication.Operation(i.opcode, i.operand)).ToArray(), new HashSet<int>(Enumerable.Range(0, body.Count))));
        }
        catch (Exception error) { route.Failure = error.GetBaseException().Message; }
        return body;
    }
}
