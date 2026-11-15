using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Megamod.Features.SettingsSearch.Core;
using Megamod.Patching;

namespace Megamod.Features.SettingsSearch.Harmony;

public sealed class SettingsReadControlGuard
{
    private static readonly Dictionary<MethodBase, SettingsReadControlGuard> Routes = new();
    private readonly MethodInfo target;
    private readonly MethodInfo prefix;
    private readonly MethodInfo? finalizer;
    private readonly OwnedHarmonyPatch prefixReceipt;
    private readonly OwnedHarmonyPatch? finalizerReceipt;
    private bool prefixInstalled, finalizerInstalled, enabled;

    public SettingsReadControlGuard(MethodInfo target, string patchId, MethodInfo prefix, MethodInfo? finalizer = null)
    {
        target = NormalizeDeclaration(target);
        this.target = target;
        this.prefix = prefix;
        this.finalizer = finalizer;
        prefixReceipt = new(target, HarmonyPatchType.Prefix, prefix, () => ReadOwnedSlot(HarmonyPatchType.Prefix));
        var candidates = new List<PatchCandidate>
        {
            new(patchId + ".prefix", "SS-READ-CONTROL-ENTRY", target.ToString()!, "automatic settings discovery",
                "ordinary settings pages", Validate, InstallPrefix, RollbackPrefix)
        };
        if (finalizer != null)
        {
            finalizerReceipt = new(target, HarmonyPatchType.Finalizer, finalizer, () => ReadOwnedSlot(HarmonyPatchType.Finalizer));
            candidates.Add(new(patchId + ".finalizer", "SS-READ-CONTROL-EXIT", target.ToString()!, "automatic settings discovery",
                "ordinary settings pages", Validate, InstallFinalizer, RollbackFinalizer));
        }
        Candidates = candidates.AsReadOnly();
    }
    public IReadOnlyList<PatchCandidate> Candidates { get; }
    public bool Available
    {
        get
        {
            try { return prefixInstalled && (finalizer == null || finalizerInstalled) && InspectTarget(true, true) == null; }
            catch (Exception) { return false; }
        }
    }
    public void Enable()
    {
        if (!Available) throw new InvalidOperationException("The settings-read control guard is unavailable.");
        enabled = true;
    }
    public static bool ShouldCapture(MethodBase originalMethod)
    {
        var scope = SettingsReadScope.Current;
        if (scope == null || !Routes.TryGetValue(originalMethod, out var owner) || !owner.enabled) return false;
        if (!owner.Available) throw scope.Refuse("settings-read-control-guard-unavailable");
        return true;
    }

    private void InstallPrefix()
    {
        Routes.Add(target, this);
        prefixReceipt.Install();
        prefixInstalled = true;
        string? failure = InspectTarget(true, false);
        if (failure != null) throw new InvalidOperationException(failure);
    }
    private void InstallFinalizer()
    {
        finalizerReceipt!.Install(); finalizerInstalled = true;
        if (!Available) throw new InvalidOperationException("The settings-read control exit order is unavailable.");
    }
    private void RollbackPrefix()
    {
        enabled = prefixInstalled = false;
        try { prefixReceipt.Rollback(); }
        finally { if (Routes.TryGetValue(target, out var owner) && ReferenceEquals(owner, this)) Routes.Remove(target); }
    }
    private void RollbackFinalizer() { enabled = finalizerInstalled = false; finalizerReceipt!.Rollback(); }

    private string? Validate()
    {
        if (target.IsAbstract || target.ContainsGenericParameters ||
            (target.IsGenericMethod && !SupportedNumericClosure(target)) ||
            target.DeclaringType == null || target.DeclaringType.IsGenericType || target.GetMethodBody() == null ||
            target.CustomAttributes.Any(attribute => attribute.AttributeType.FullName is
                "System.Runtime.CompilerServices.AsyncStateMachineAttribute" or "System.Runtime.CompilerServices.IteratorStateMachineAttribute" or
                "System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute"))
            return "The settings-read control requires a concrete synchronous managed target.";
        if (!IsConcreteCallback(prefix) || (prefix.ReturnType != typeof(void) && prefix.ReturnType != typeof(bool)))
            return "The settings-read control prefix must be a concrete static callback.";
        if (finalizer != null)
        {
            if (!IsConcreteCallback(finalizer) || finalizer.DeclaringType != prefix.DeclaringType ||
                (finalizer.ReturnType != typeof(void) && finalizer.ReturnType != typeof(Exception)))
                return "Paired control callbacks require the same concrete declaring type.";
            var entryState = prefix.GetParameters().SingleOrDefault(parameter => parameter.Name == "__state");
            var exitState = finalizer.GetParameters().SingleOrDefault(parameter => parameter.Name == "__state");
            if (entryState == null || exitState == null || !entryState.ParameterType.IsByRef ||
                entryState.ParameterType.GetElementType() != (exitState.ParameterType.IsByRef
                    ? exitState.ParameterType.GetElementType() : exitState.ParameterType))
                return "Paired control callbacks require the same explicit state type.";
        }
        return InspectTarget(false, false);
    }

    private static bool IsConcreteCallback(MethodInfo callback) => callback.IsStatic && !callback.IsAbstract &&
        !callback.IsGenericMethod && !callback.ContainsGenericParameters && callback.DeclaringType != null &&
        !callback.DeclaringType.IsGenericType && callback.GetMethodBody() != null;

    private static bool SupportedNumericClosure(MethodInfo method)
    {
        Type[] arguments = method.GetGenericArguments();
        return !method.IsGenericMethodDefinition && arguments.Length == 1 &&
            (arguments[0] == typeof(int) || arguments[0] == typeof(float));
    }

    private static MethodInfo NormalizeDeclaration(MethodInfo method)
    {
        var declaration = method.DeclaringType?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .SingleOrDefault(candidate => candidate.Module == method.Module && candidate.MetadataToken == method.MetadataToken &&
                candidate.DeclaringType == method.DeclaringType && candidate.IsGenericMethod == method.IsGenericMethod);
        if (declaration == null) return method;
        return method.IsGenericMethod && !method.IsGenericMethodDefinition
            ? declaration.MakeGenericMethod(method.GetGenericArguments()) : declaration;
    }

    private string? InspectTarget(bool prefixAcquired, bool finalizerAcquired)
    {
        string? failure = null;
        bool inspected = HarmonyPublication.Inspect(target, snapshot =>
        {
            if (finalizer != null && snapshot.Postfixes.Any(patch => patch.IsFactory))
            {
                failure = "An unresolved postfix factory prevents safe paired settings-read control state inspection.";
                return true;
            }
            if (finalizer != null && (snapshot.Prefixes.Any(patch => SharesPairedState(patch, prefix)) ||
                snapshot.Postfixes.Any(patch => SharesPairedState(patch, null)) ||
                snapshot.Finalizers.Any(patch => SharesPairedState(patch, finalizer))))
            {
                failure = "Another registration shares the paired settings-read control callback state.";
                return true;
            }
            failure = InspectSlot(snapshot.Prefixes, prefix, prefixAcquired, first: true);
            if (failure == null && finalizer != null)
                failure = InspectSlot(snapshot.Finalizers, finalizer, finalizerAcquired, first: false);
            return true;
        });
        return inspected ? failure : "Published settings-read control metadata is busy or unavailable.";
    }

    private bool SharesPairedState(HarmonyPublication.OuterRegistration patch, MethodInfo? expected) =>
        !patch.IsFactory && patch.Method.DeclaringType == prefix.DeclaringType &&
        patch.Method.GetParameters().Any(parameter => parameter.Name == "__state") &&
        (patch.Owner != OwnedHarmonyPatch.OwnerId || patch.Method != expected);

    private string? InspectSlot(IReadOnlyList<HarmonyPublication.OuterRegistration> slot, MethodInfo callback, bool acquired, bool first)
    {
        if (slot.Any(patch => patch.IsFactory)) return "A consumed control prefix/finalizer factory prevents safe ordering inspection.";
        var owned = slot.Where(patch => patch.Owner == OwnedHarmonyPatch.OwnerId).ToArray();
        if (!acquired) return owned.Length == 0 ? null : "The owned settings-read control slot is already occupied.";
        if (owned.Length != 1 || owned[0].Method != callback) return "The acquired settings-read control callback is absent or duplicated.";
        var sorted = PatchProcessor.GetSortedPatchMethods(target, slot.Select(patch => patch.ToOrderingPatch()).ToArray());
        return sorted.Count > 0 && (first ? sorted[0] : sorted[sorted.Count - 1]) == callback
            ? null : "The settings-read control callback is not at its required outer boundary.";
    }

    private IReadOnlyList<Patch> ReadOwnedSlot(HarmonyPatchType kind)
    {
        Patch[]? owned = null;
        if (!HarmonyPublication.Inspect(target, snapshot =>
            {
                var slot = kind == HarmonyPatchType.Prefix ? snapshot.Prefixes : snapshot.Finalizers;
                owned = slot.Where(patch => patch.Owner == OwnedHarmonyPatch.OwnerId).Select(patch => patch.ToOrderingPatch()).ToArray();
                return true;
            })) throw new InvalidOperationException("Published settings-read control ownership is busy or unavailable.");
        return owned!;
    }
}
