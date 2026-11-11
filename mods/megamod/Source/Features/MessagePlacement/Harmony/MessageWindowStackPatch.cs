using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Megamod.Patching;

namespace Megamod.Features.MessagePlacement;

// Entry observation only: no native body, argument, callback or exception is replaced.
public sealed class MessageWindowStackPatch
{
    private static readonly Dictionary<MethodBase, MessageWindowStackPatch> Routes = new();
    private static readonly MethodInfo PrefixMethod = typeof(MessageWindowStackPatch).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic)!;
    private readonly MethodInfo target;
    private readonly Action<object, Func<bool>> beforeWindows;
    private readonly Action<Exception> reportFailure;
    private readonly OwnedHarmonyPatch receipt;
    private bool installed, enabled, faulted;

    public MessageWindowStackPatch(MethodInfo target, Action<object, Func<bool>> beforeWindows, Action<Exception> reportFailure)
    {
        this.target = target; this.beforeWindows = beforeWindows; this.reportFailure = reportFailure;
        receipt = new(target, HarmonyPatchType.Prefix, PrefixMethod, ReadOwnedSlot);
        Candidate = new("MSG-WINDOW-STACK", "MSG-WINDOW-STACK", target.ToString()!, "message window fallback",
            "native message placement", Validate, Install, Rollback);
    }
    public PatchCandidate Candidate { get; }
    public bool Available => installed && !faulted && InspectTarget(acquired: true) == null;
    public void Enable()
    {
        if (!Available) throw new InvalidOperationException("Native window entry observation is unavailable.");
        enabled = true;
    }
    private string? Validate()
    {
        if (!target.IsPublic || target.IsStatic || target.IsAbstract || target.IsGenericMethod || target.ContainsGenericParameters ||
            target.ReturnType != typeof(void) || target.GetParameters().Length != 0 || target.DeclaringType == null ||
            target.DeclaringType.IsValueType || target.CustomAttributes.Any(attribute => attribute.AttributeType.FullName is
                "System.Runtime.CompilerServices.AsyncStateMachineAttribute" or "System.Runtime.CompilerServices.IteratorStateMachineAttribute" or
                "System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute"))
            return "The synchronous native window-stack callable shape changed.";
        return InspectTarget(acquired: false);
    }
    private void Install()
    {
        if (Routes.ContainsKey(target)) throw new InvalidOperationException("The native window entry already has an owner.");
        Routes.Add(target, this);
        receipt.Install(); installed = true;
        if (!Available) throw new InvalidOperationException("Native window entry cannot be observed before foreign prefixes.");
    }
    private void Rollback()
    {
        enabled = false; installed = false;
        try { receipt.Rollback(); }
        finally { if (Routes.TryGetValue(target, out var owner) && ReferenceEquals(owner, this)) Routes.Remove(target); }
    }
    [HarmonyPriority(int.MaxValue)]
    private static void Prefix(object __instance, MethodBase __originalMethod)
    {
        if (!Routes.TryGetValue(__originalMethod, out var owner) || !owner.enabled) return;
        try
        {
            string? failure = owner.InspectTarget(acquired: true);
            if (failure != null) { owner.Fail(new InvalidOperationException(failure)); return; }
            owner.beforeWindows(__instance, () => owner.enabled && owner.Available);
        }
        catch (Exception failure) { owner.Fail(failure); }
    }
    private string? InspectTarget(bool acquired)
    {
        string? failure = null;
        bool inspected = HarmonyPublication.Inspect(target, snapshot =>
        {
            if (snapshot.Prefixes.Any(patch => patch.IsFactory))
            { failure = "A prefix factory prevents safe window entry ordering inspection."; return true; }
            var owned = snapshot.Prefixes.Where(patch => patch.Owner == OwnedHarmonyPatch.OwnerId).ToArray();
            if (!acquired)
            {
                if (owned.Length != 0) failure = "The owned window prefix slot already contains a patch; safe rollback cannot be acquired.";
                return true;
            }
            if (owned.Length != 1 || owned[0].Method != PrefixMethod)
            { failure = "The acquired window entry prefix is absent or duplicated."; return true; }
            var sorted = PatchProcessor.GetSortedPatchMethods(target, snapshot.Prefixes.Select(patch => patch.ToOrderingPatch()).ToArray());
            if (sorted.Count == 0 || sorted[0] != PrefixMethod) failure = "A foreign prefix precedes native window entry observation.";
            return true;
        });
        return inspected ? failure : "Published window entry metadata is busy or cannot be inspected during composition.";
    }
    private IReadOnlyList<Patch> ReadOwnedSlot()
    {
        Patch[]? owned = null;
        if (!HarmonyPublication.Inspect(target, snapshot =>
            { owned = snapshot.Prefixes.Where(patch => patch.Owner == OwnedHarmonyPatch.OwnerId).Select(patch => patch.ToOrderingPatch()).ToArray(); return true; }))
            throw new InvalidOperationException("Published window entry ownership metadata is busy or cannot be inspected during composition.");
        return owned!;
    }
    private void Fail(Exception failure)
    {
        enabled = false;
        if (faulted) return;
        faulted = true;
        try { reportFailure(failure); } catch (Exception) { /* Native window processing remains authoritative. */ }
    }
}
