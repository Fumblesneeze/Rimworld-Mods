using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Megamod.Patching;

namespace Megamod.Features.MessagePlacement;

public sealed class MessageGuiScopePatch
{
    private static readonly Dictionary<MethodBase, MessageGuiScopePatch> Routes = new();
    private static readonly MethodInfo PrefixMethod = typeof(MessageGuiScopePatch).GetMethod(nameof(Prefix), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo FinalizerMethod = typeof(MessageGuiScopePatch).GetMethod(nameof(Finalizer), BindingFlags.NonPublic | BindingFlags.Static)!;
    private readonly MethodInfo target;
    private readonly Func<object?> begin;
    private readonly Action<object, bool> complete;
    private readonly Action<Exception> reportFailure;
    private readonly OwnedHarmonyPatch prefix, finalizer;
    private bool installed, enabled, faulted;

    public MessageGuiScopePatch(MethodInfo target, Func<object?> begin, Action<object, bool> complete, Action<Exception> reportFailure)
    {
        this.target = target; this.begin = begin; this.complete = complete; this.reportFailure = reportFailure;
        prefix = new(target, HarmonyPatchType.Prefix, PrefixMethod, () => ReadOwnedSlot(isFinalizer: false));
        finalizer = new(target, HarmonyPatchType.Finalizer, FinalizerMethod, () => ReadOwnedSlot(isFinalizer: true));
        Candidate = new("MSG-NATIVE-FRAME", "MSG-NATIVE-FRAME", target.ToString()!, "message placement",
            "native placement", Validate, Install, Rollback);
    }
    public PatchCandidate Candidate { get; }
    public bool Available => installed && !faulted && OrderingFailure() == null;
    public void Enable()
    {
        if (!Available) throw new InvalidOperationException("Native message scope is unavailable.");
        enabled = true;
    }

    private string? Validate()
    {
        if (!target.IsPublic || !target.IsStatic || target.ContainsGenericParameters || target.IsGenericMethod ||
            target.ReturnType != typeof(void) || target.GetParameters().Length != 0)
            return "The native message GUI entry point must be public static parameterless void.";
        return InspectTarget(acquired: false);
    }

    private void Install()
    {
        if (Routes.ContainsKey(target)) throw new InvalidOperationException("The native message scope already has an owner.");
        Routes.Add(target, this);
        prefix.Install();
        finalizer.Install();
        installed = true;
        if (!Available) throw new InvalidOperationException("The owned finalizer cannot observe the original exception before foreign handling.");
    }

    private void Rollback()
    {
        enabled = false; installed = false;
        var failures = new List<Exception>();
        try { finalizer.Rollback(); } catch (Exception error) { failures.Add(error); }
        try { prefix.Rollback(); } catch (Exception error) { failures.Add(error); }
        if (Routes.TryGetValue(target, out var current) && ReferenceEquals(current, this)) Routes.Remove(target);
        if (failures.Count > 0) throw new AggregateException("Native message scope cleanup failed.", failures);
    }

    private sealed record Scope(MessageGuiScopePatch Owner, object Token);

    [HarmonyPriority(int.MaxValue)]
    private static void Prefix(MethodBase __originalMethod, out Scope? __state)
    {
        __state = null;
        if (!Routes.TryGetValue(__originalMethod, out var owner) || !owner.enabled) return;
        try
        {
            string? failure = owner.OrderingFailure();
            if (failure != null) { owner.Fail(new InvalidOperationException(failure)); return; }
            if (owner.begin() is { } token) __state = new(owner, token);
        }
        catch (Exception error) { owner.Fail(error); }
    }

    [HarmonyPriority(int.MaxValue)]
    private static Exception? Finalizer(Exception? __exception, Scope? __state, bool __runOriginal)
    {
        if (__state != null)
        {
            var owner = __state.Owner;
            bool successful = false;
            try
            {
                string? failure = owner.OrderingFailure();
                if (failure != null) owner.Fail(new InvalidOperationException(failure));
                successful = __runOriginal && __exception == null && owner.enabled && failure == null;
            }
            catch (Exception error) { owner.Fail(error); }
            finally
            {
                // Acquired scope cleanup cannot depend on metadata remaining inspectable.
                try { owner.complete(__state.Token, successful); }
                catch (Exception error) { owner.Fail(error); }
            }
        }
        return __exception;
    }

    private string? OrderingFailure() => InspectTarget(acquired: true);

    private string? InspectTarget(bool acquired)
    {
        string? failure = null;
        bool inspected = HarmonyPublication.Inspect(target, snapshot =>
        {
            // Factory, exact slot and finalizer order decisions share one published snapshot.
            if (snapshot.Finalizers.Any(patch => patch.IsFactory))
            { failure = "A finalizer factory prevents safe message exception-order inspection."; return true; }
            if (!acquired)
            {
                if (snapshot.Prefixes.Concat(snapshot.Finalizers).Any(patch => patch.Owner == OwnedHarmonyPatch.OwnerId))
                    failure = "The owned target/kind slot already contains a patch; safe rollback cannot be acquired.";
                return true;
            }
            if (snapshot.Prefixes.Count(patch => patch.Owner == OwnedHarmonyPatch.OwnerId && patch.Method == PrefixMethod) != 1)
            { failure = "The acquired message scope prefix is absent or duplicated."; return true; }
            if (snapshot.Finalizers.Count(patch => patch.Owner == OwnedHarmonyPatch.OwnerId && patch.Method == FinalizerMethod) != 1)
            { failure = "The acquired message scope finalizer is absent or duplicated."; return true; }
            var sorted = PatchProcessor.GetSortedPatchMethods(target, snapshot.Finalizers.Select(patch => patch.ToOrderingPatch()).ToArray());
            if (sorted.Count == 0 || sorted[0] != FinalizerMethod)
                failure = "Foreign exception handling precedes the message scope finalizer; native success cannot be established.";
            return true;
        });
        return inspected ? failure : "Published message scope metadata is busy or cannot be inspected during composition.";
    }

    private IReadOnlyList<Patch> ReadOwnedSlot(bool isFinalizer)
    {
        Patch[]? owned = null;
        if (!HarmonyPublication.Inspect(target, snapshot =>
        {
            // Receipt cleanup must not resolve unrelated foreign factories.
            owned = (isFinalizer ? snapshot.Finalizers : snapshot.Prefixes)
                .Where(patch => patch.Owner == OwnedHarmonyPatch.OwnerId)
                .Select(patch => patch.ToOrderingPatch()).ToArray();
            return true;
        })) throw new InvalidOperationException("Published message scope ownership metadata is busy or cannot be inspected during composition.");
        return owned!;
    }

    private void Fail(Exception failure)
    {
        enabled = false;
        if (faulted) return;
        faulted = true;
        try { reportFailure(failure); } catch (Exception) { /* Native rendering/exceptions remain authoritative. */ }
    }
}
