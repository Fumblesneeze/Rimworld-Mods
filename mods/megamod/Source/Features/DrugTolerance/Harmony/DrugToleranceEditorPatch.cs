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

public sealed record DrugToleranceEditorReservation(object NativeRectangle, Action Draw);
public sealed record DrugToleranceEditorCallbacks(MethodInfo Prefix, MethodInfo Postfix, MethodInfo Finalizer);

public sealed class DrugToleranceEditorPatch
{
    private static readonly ConcurrentDictionary<MethodBase, DrugToleranceEditorPatch> Routes = new();
    private readonly MethodInfo target, prefixMethod;
    private readonly bool header;
    private readonly Func<IReadOnlyList<CodeInstruction>, string?> validateBody;
    private readonly Func<object, object, object?, int, DrugToleranceEditorReservation?> prepare;
    private readonly Func<bool> groupAvailable;
    private readonly Action<string> unavailable;
    private readonly OwnedHarmonyPatch prefix, postfix, finalizer, observer;
    private readonly MethodInfo postfixMethod, finalizerMethod;
    private static readonly MethodInfo ObserverMethod = Callback(nameof(Observe));
    private bool installed;
    private Generation? generation;
    private Receipt? receipt;
    private sealed record Generation(int Version, IReadOnlyList<HarmonyPublication.Operation> Body, ISet<int> Protected);
    private sealed record Receipt(Generation Generation, HarmonyPublication.Identity Identity);
    private sealed class Invocation
    {
        internal readonly DrugToleranceEditorPatch Route;
        internal readonly Receipt Receipt;
        internal readonly Action Draw;
        internal bool NativeCompleted;
        internal Invocation(DrugToleranceEditorPatch route, Receipt receipt, Action draw)
        { Route = route; Receipt = receipt; Draw = draw; }
    }
    public DrugToleranceEditorPatch(string id, MethodInfo target, bool header, DrugToleranceEditorCallbacks callbacks,
        Func<IReadOnlyList<CodeInstruction>, string?> validateBody,
        Func<object, object, object?, int, DrugToleranceEditorReservation?> prepare,
        Func<bool> groupAvailable, Action<string> unavailable)
    {
        this.target = target; this.header = header; this.validateBody = validateBody; this.prepare = prepare;
        this.groupAvailable = groupAvailable; this.unavailable = unavailable;
        prefixMethod = callbacks.Prefix; postfixMethod = callbacks.Postfix; finalizerMethod = callbacks.Finalizer;
        prefix = new(target, HarmonyPatchType.Prefix, prefixMethod, () => Owned(HarmonyPatchType.Prefix));
        postfix = new(target, HarmonyPatchType.Postfix, postfixMethod, () => Owned(HarmonyPatchType.Postfix));
        finalizer = new(target, HarmonyPatchType.Finalizer, finalizerMethod, () => Owned(HarmonyPatchType.Finalizer));
        observer = new(target, HarmonyPatchType.Transpiler, ObserverMethod, () => Owned(HarmonyPatchType.Transpiler));
        Candidate = new(id, "DT-POLICY-EDITOR", target.ToString()!, "tolerance editing and enforcement",
            "native policy editor; retained choices unavailable", Validate, Install, Rollback);
    }
    public PatchCandidate Candidate { get; }
    public bool Available => Admitted() != null;
    public string? Failure { get; private set; }

    private string? Validate()
    {
        var p = target.GetParameters();
        if (target.IsStatic || target.IsGenericMethod || target.ReturnType != typeof(void) ||
            p.Length != (header ? 1 : 3) || !p[0].ParameterType.IsValueType ||
            (!header && (!p[1].ParameterType.IsClass || p[2].ParameterType != typeof(int))))
            return "The native editor callable changed.";
        var expectedPrefix = new List<Type> { typeof(MethodBase), typeof(object), p[0].ParameterType.MakeByRefType() };
        if (!header) expectedPrefix.AddRange(new[] { typeof(object), typeof(int) });
        expectedPrefix.Add(typeof(object).MakeByRefType());
        bool Shape(MethodInfo method, Type result, IEnumerable<Type> args) => method.IsStatic && !method.ContainsGenericParameters &&
            method.ReturnType == result && method.GetParameters().Select(x => x.ParameterType).SequenceEqual(args);
        if (prefixMethod.DeclaringType != postfixMethod.DeclaringType || prefixMethod.DeclaringType != finalizerMethod.DeclaringType ||
            !Shape(prefixMethod, typeof(void), expectedPrefix) ||
            !Shape(postfixMethod, typeof(void), new[] { typeof(bool), typeof(object) }) ||
            !Shape(finalizerMethod, typeof(Exception), new[] { typeof(Exception), typeof(bool), typeof(object) }))
            return "The owned concrete rectangle callbacks changed.";
        return prefix.ValidateOwnership() ?? postfix.ValidateOwnership() ?? finalizer.ValidateOwnership() ?? observer.ValidateOwnership();
    }
    private void Install()
    {
        if (!Routes.TryAdd(target, this)) throw new InvalidOperationException("The editor target already has an owned route.");
        prefix.Install(); postfix.Install(); finalizer.Install(); observer.Install(); installed = true;
        if (!Available) throw new InvalidOperationException(Failure ?? "The native editor allocation is unproved.");
    }
    private void Rollback()
    {
        installed = false; Volatile.Write(ref receipt, null); Volatile.Write(ref generation, null);
        var failures = new List<Exception>();
        foreach (var slot in new[] { observer, finalizer, postfix, prefix })
            try { slot.Rollback(); } catch (Exception error) { failures.Add(error); }
        if (Routes.TryGetValue(target, out var current) && ReferenceEquals(current, this)) Routes.TryRemove(target, out _);
        if (failures.Count != 0) throw new AggregateException("Editor cleanup failed.", failures);
    }
    private Receipt? Admitted()
    {
        var current = Volatile.Read(ref generation);
        if (!installed || current == null) return null;
        var existing = Volatile.Read(ref receipt);
        if (ReferenceEquals(existing?.Generation, current) && existing!.Identity.IsCurrent()) return existing;
        try
        {
            bool accepted = HarmonyPublication.Inspect(target, snapshot =>
            {
                if (snapshot.Version != current.Version || !ReferenceEquals(current, Volatile.Read(ref generation)) ||
                    snapshot.Transpilers.Any(p => p.PatchMethod.ReturnType == typeof(MethodInfo) || p.PatchMethod.ReturnType == typeof(DynamicMethod)) ||
                    snapshot.Prefixes.Concat(snapshot.Postfixes).Concat(snapshot.Finalizers).Any(p => p.IsFactory))
                { Failure = "The editor generation or outer ordering is unproved."; return false; }
                bool Slot(IReadOnlyList<HarmonyPublication.OuterRegistration> entries, MethodInfo callback) =>
                    entries.Count(p => p.Owner == OwnedHarmonyPatch.OwnerId) == 1 &&
                    entries.Any(p => p.Owner == OwnedHarmonyPatch.OwnerId && p.Method == callback);
                if (!Slot(snapshot.Prefixes, prefixMethod) || !Slot(snapshot.Postfixes, postfixMethod) || !Slot(snapshot.Finalizers, finalizerMethod) ||
                    snapshot.Transpilers.Count(p => p.owner == OwnedHarmonyPatch.OwnerId) != 1 ||
                    !snapshot.Transpilers.Any(p => p.owner == OwnedHarmonyPatch.OwnerId && p.PatchMethod == ObserverMethod) ||
                    PatchProcessor.GetSortedPatchMethods(target, snapshot.Transpilers).LastOrDefault() != ObserverMethod ||
                    PatchProcessor.GetSortedPatchMethods(target, snapshot.Prefixes.Select(p => p.ToOrderingPatch()).ToArray()).LastOrDefault() != prefixMethod ||
                    PatchProcessor.GetSortedPatchMethods(target, snapshot.Postfixes.Select(p => p.ToOrderingPatch()).ToArray()).FirstOrDefault() != postfixMethod ||
                    PatchProcessor.GetSortedPatchMethods(target, snapshot.Finalizers.Select(p => p.ToOrderingPatch()).ToArray()).FirstOrDefault() != finalizerMethod)
                { Failure = "The editor needs its last argument capture, first completion observer and first finalizer."; return false; }
                Failure = snapshot.FailureFor(current.Body, current.Protected);
                if (Failure != null) return false;
                Volatile.Write(ref receipt, new(current, snapshot.Identity)); return true;
            });
            return accepted ? Volatile.Read(ref receipt) : null;
        }
        catch (Exception error) { Failure = error.GetBaseException().Message; return null; }
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
        })) throw new InvalidOperationException("Editor ownership publication is busy.");
        return found!;
    }
    // Concrete native/fixture wrappers supply the ref-rectangle binding. This provider serializes
    // callback module/token only and cannot retain a constructed generic callback's type arguments.
    public static object? Prepare(MethodBase original, object manager, object rectangle, object? row, int index, out object nativeRectangle)
    {
        nativeRectangle = rectangle;
        if (!Routes.TryGetValue(original, out var route)) return null;
        try
        {
            var receipt = route.Admitted();
            if (receipt == null || !route.groupAvailable()) return null;
            var reservation = route.prepare(manager, rectangle, row, index);
            if (reservation == null || !ReferenceEquals(receipt, route.Admitted()) || !route.groupAvailable()) return null;
            if (reservation.NativeRectangle.GetType() != rectangle.GetType()) throw new InvalidOperationException("The reserved rectangle type changed.");
            nativeRectangle = reservation.NativeRectangle;
            return new Invocation(route, receipt, reservation.Draw);
        }
        catch (Exception error) { route.Refuse(error.GetBaseException().Message); return null; }
    }
    public static void CompleteNative(bool runOriginal, object? state)
    { if (state is Invocation invocation) invocation.NativeCompleted = runOriginal; }
    public static Exception? Draw(Exception? exception, bool runOriginal, object? state)
    {
        if (exception != null || !runOriginal || state is not Invocation { NativeCompleted: true } invocation) return exception;
        try
        {
            if (ReferenceEquals(invocation.Receipt, invocation.Route.Admitted()) && invocation.Route.groupAvailable()) invocation.Draw();
        }
        catch (Exception error) { invocation.Route.Refuse(error.GetBaseException().Message); }
        return exception;
    }
    private void Refuse(string reason)
    { Failure = reason; try { unavailable(reason); } catch (Exception) { } }
    [HarmonyPriority(int.MinValue)]
    private static IEnumerable<CodeInstruction> Observe(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var body = instructions.ToList();
        if (!Routes.TryGetValue(__originalMethod, out var route)) return body;
        Volatile.Write(ref route.receipt, null); Volatile.Write(ref route.generation, null);
        try
        {
            route.Failure = route.validateBody(body);
            if (route.Failure == null)
                Volatile.Write(ref route.generation, new(HarmonyPublication.ExpectedVersion(__originalMethod),
                    body.Select(i => new HarmonyPublication.Operation(i.opcode, i.operand)).ToArray(), new HashSet<int>(Enumerable.Range(0, body.Count))));
        }
        catch (Exception error) { route.Failure = error.GetBaseException().Message; }
        return body;
    }
    private static MethodInfo Callback(string name) => typeof(DrugToleranceEditorPatch).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;
}
