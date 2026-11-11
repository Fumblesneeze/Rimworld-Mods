using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Megamod.Patching;

namespace Megamod.Features.MessagePlacement;

public sealed class MessageGuiEventPatch
{
    private static readonly Dictionary<MethodBase, MessageGuiEventPatch> Routes = new();
    private static readonly MethodInfo PrefixMethod = typeof(MessageGuiEventPatch).GetMethod(nameof(Prefix), BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly MethodInfo FinalizerMethod = typeof(MessageGuiEventPatch).GetMethod(nameof(Finalizer), BindingFlags.NonPublic | BindingFlags.Static)!;
    private readonly MethodInfo baseGui;
    private readonly MethodInfo[] targets;
    private readonly OwnedHarmonyPatch[] receipts;
    private readonly MessageGuiEvent timing;
    private readonly Func<object, bool> isCurrentGuiContext;
    private readonly Action<Exception> reportFailure;
    private readonly Action<MessageGuiEvent.Envelope>? exited;
    private bool installed, enabled, faulted;
    public MessageGuiEventPatch(MethodInfo baseGui, MethodInfo entryGui, MethodInfo playGui, MessageGuiEvent timing,
        Func<object, bool> isCurrentGuiContext, Action<Exception> reportFailure,
        Action<MessageGuiEvent.Envelope>? exited = null)
    {
        this.baseGui = baseGui; targets = new[] { entryGui, playGui }; this.timing = timing;
        this.isCurrentGuiContext = isCurrentGuiContext; this.reportFailure = reportFailure;
        this.exited = exited;
        receipts = targets.SelectMany(target => new[]
        {
            new OwnedHarmonyPatch(target, HarmonyPatchType.Prefix, PrefixMethod, () => ReadOwnedSlot(target, finalizer: false)),
            new OwnedHarmonyPatch(target, HarmonyPatchType.Finalizer, FinalizerMethod, () => ReadOwnedSlot(target, finalizer: true))
        }).ToArray();
        Candidate = new("MSG-NATIVE-EVENT", "MSG-NATIVE-FRAME", baseGui.ToString()!, "message placement", "native placement",
            Validate, Install, Rollback);
    }
    public PatchCandidate Candidate { get; }
    public bool Available => installed && !faulted && OrderingFailure() == null;
    public void Enable()
    {
        if (!Available) throw new InvalidOperationException("The native GUI invocation envelope is unavailable.");
        enabled = true;
    }
    private string? Validate()
    {
        if (!GuiShape(baseGui) || targets[0] == targets[1] || targets.Any(target => !GuiShape(target) ||
            target.DeclaringType == baseGui.DeclaringType || target.GetBaseDefinition() != baseGui))
            return "The exact native GUI override/base callable relationship changed.";
        return targets.Select(target => InspectTarget(target, acquired: false)).FirstOrDefault(failure => failure != null);
    }
    private static bool GuiShape(MethodInfo method) => method.IsPublic && !method.IsStatic && method.IsVirtual &&
        !method.IsAbstract && !method.IsGenericMethod && !method.ContainsGenericParameters && method.ReturnType == typeof(void) &&
        method.GetParameters().Length == 0;
    private void Install()
    {
        if (targets.Any(target => Routes.ContainsKey(target))) throw new InvalidOperationException("The GUI invocation route already has an owner.");
        foreach (var target in targets) Routes.Add(target, this);
        foreach (var receipt in receipts) receipt.Install();
        installed = true;
        if (!Available) throw new InvalidOperationException("The outer native GUI envelope ordering is unavailable.");
    }
    private void Rollback()
    {
        enabled = false; installed = false;
        var failures = new List<Exception>();
        foreach (var receipt in receipts.Reverse())
            try { receipt.Rollback(); } catch (Exception failure) { failures.Add(failure); }
        foreach (var target in targets)
            if (Routes.TryGetValue(target, out var owner) && ReferenceEquals(owner, this)) Routes.Remove(target);
        if (failures.Count != 0) throw new AggregateException("GUI invocation observer cleanup failed.", failures);
    }
    private sealed record Scope(MessageGuiEventPatch Owner, MessageGuiEvent.Envelope Envelope);
    [HarmonyPriority(int.MaxValue)]
    private static void Prefix(MethodBase __originalMethod, object __instance, out Scope? __state)
    {
        __state = null;
        if (!Routes.TryGetValue(__originalMethod, out var owner) || !owner.enabled) return;
        try
        {
            string? orderingFailure = owner.OrderingFailure();
            if (orderingFailure != null) { owner.Fail(new InvalidOperationException(orderingFailure)); return; }
            if (__instance.GetType() != __originalMethod.DeclaringType || !owner.isCurrentGuiContext(__instance))
            { owner.Fail(new InvalidOperationException("The exact current native GUI context is unavailable.")); return; }
            __state = new(owner, owner.timing.Enter(__instance));
        }
        catch (Exception failure) { owner.Fail(failure); }
    }
    [HarmonyPriority(int.MaxValue)]
    private static Exception? Finalizer(Exception? __exception, Scope? __state)
    {
        if (__state != null)
            try
            {
                try { __state.Owner.timing.Exit(__state.Envelope); }
                finally { __state.Owner.exited?.Invoke(__state.Envelope); }
            }
            catch (Exception failure) { __state.Owner.Fail(failure); }
        return __exception;
    }
    private string? OrderingFailure()
    {
        foreach (var target in targets)
        {
            string? failure = InspectTarget(target, acquired: true);
            if (failure != null) return failure;
        }
        return null;
    }
    private static string? InspectTarget(MethodInfo target, bool acquired)
    {
        string? failure = null;
        bool inspected = HarmonyPublication.Inspect(target, snapshot =>
        {
            // Factory, exact slot and ordering decisions use this one published target snapshot.
            if (snapshot.Prefixes.Concat(snapshot.Finalizers).Any(patch => patch.IsFactory))
            { failure = "A GUI observer factory prevents safe envelope ordering inspection."; return true; }
            foreach (var slot in new[] { (snapshot.Prefixes, PrefixMethod), (snapshot.Finalizers, FinalizerMethod) })
            {
                if (!acquired)
                {
                    if (slot.Item1.Any(patch => patch.Owner == OwnedHarmonyPatch.OwnerId))
                    { failure = "The owned target/kind slot already contains a patch; safe rollback cannot be acquired."; return true; }
                    continue;
                }
                if (slot.Item1.Count(patch => patch.Owner == OwnedHarmonyPatch.OwnerId && patch.Method == slot.Item2) != 1)
                { failure = "The acquired outer GUI observer is absent or duplicated."; return true; }
                var sorted = PatchProcessor.GetSortedPatchMethods(target, slot.Item1.Select(patch => patch.ToOrderingPatch()).ToArray());
                if (sorted.Count == 0 || sorted[0] != slot.Item2)
                { failure = "Foreign GUI observer ordering escapes the owned invocation envelope."; return true; }
            }
            return true;
        });
        return inspected ? failure : "Published outer GUI metadata is busy or cannot be inspected during composition.";
    }
    private static IReadOnlyList<Patch> ReadOwnedSlot(MethodInfo target, bool finalizer)
    {
        Patch[]? owned = null;
        if (!HarmonyPublication.Inspect(target, snapshot =>
        {
            // Receipts only need this owner's slot. Never resolve unrelated foreign factories
            // merely to remove the exact receipt previously acquired by this helper.
            owned = (finalizer ? snapshot.Finalizers : snapshot.Prefixes)
                .Where(patch => patch.Owner == OwnedHarmonyPatch.OwnerId)
                .Select(patch => patch.ToOrderingPatch()).ToArray();
            return true;
        })) throw new InvalidOperationException("Published outer GUI ownership metadata is busy or cannot be inspected during composition.");
        return owned!;
    }
    private void Fail(Exception failure)
    {
        enabled = false;
        if (faulted) return;
        faulted = true;
        try { reportFailure(failure); } catch (Exception) { /* Native GUI behavior remains authoritative. */ }
    }
}
