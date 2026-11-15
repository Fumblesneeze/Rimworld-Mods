using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Megamod.Features.SettingsSearch.Core;
using Megamod.Patching;

namespace Megamod.Features.SettingsSearch.Harmony;

public sealed class SettingsReadActionGuard
{
    private static readonly Dictionary<MethodBase, SettingsReadActionGuard> Routes = new();
    private static readonly MethodInfo PrefixMethod = typeof(SettingsReadActionGuard).GetMethod(nameof(Prefix), BindingFlags.Static | BindingFlags.NonPublic)!;
    private readonly MethodBase target;
    private readonly string failureReason;
    private readonly OwnedHarmonyPatch receipt;
    private bool installed, enabled;

    public SettingsReadActionGuard(MethodBase target, string patchId, string failureReason)
    {
        // A lookup through a subclass may change ReflectedType without changing the declaration.
        // Normalize only that exact declaration, never GetBaseDefinition (which conflates overrides).
        this.target = target;
        if (target is MethodInfo) this.target = target.DeclaringType?.GetMethods(BindingFlags.Public | BindingFlags.NonPublic |
            BindingFlags.Static | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .SingleOrDefault(method => method.Module == target.Module && method.MetadataToken == target.MetadataToken &&
                method.DeclaringType == target.DeclaringType && method.IsGenericMethod == target.IsGenericMethod) ?? target;
        this.failureReason = failureReason;
        receipt = new(this.target, HarmonyPatchType.Prefix, PrefixMethod, ReadOwnedSlot);
        Candidate = new(patchId, patchId, this.target.ToString()!, "automatic settings discovery",
            "ordinary settings pages", Validate, Install, Rollback);
    }
    public PatchCandidate Candidate { get; }
    public bool Available
    {
        get
        {
            try { return installed && InspectTarget(acquired: true) == null; }
            catch (Exception) { return false; }
        }
    }
    public void Enable()
    {
        if (!Available) throw new InvalidOperationException("The settings-read action guard is unavailable.");
        enabled = true;
    }

    private string? Validate()
    {
        if ((target is not MethodInfo && target is not ConstructorInfo) ||
            (target is ConstructorInfo && target.IsStatic))
            return "The settings-read action requires a runtime method or instance constructor.";
        if (target.IsAbstract || target.IsGenericMethod || target.ContainsGenericParameters ||
            target.DeclaringType == null || target.DeclaringType.IsGenericType || target.GetMethodBody() == null ||
            target.CustomAttributes.Any(attribute => attribute.AttributeType.FullName is
                "System.Runtime.CompilerServices.AsyncStateMachineAttribute" or "System.Runtime.CompilerServices.IteratorStateMachineAttribute" or
                "System.Runtime.CompilerServices.AsyncIteratorStateMachineAttribute"))
            return "The settings-read action requires a concrete synchronous nongeneric managed target.";
        return InspectTarget(acquired: false);
    }

    private void Install()
    {
        Routes.Add(target, this);
        receipt.Install();
        installed = true;
        if (!Available) throw new InvalidOperationException("The settings-read guard cannot run before other prefixes.");
    }

    private void Rollback()
    {
        enabled = false;
        installed = false;
        try { receipt.Rollback(); }
        finally
        {
            if (Routes.TryGetValue(target, out var owner) && ReferenceEquals(owner, this)) Routes.Remove(target);
        }
    }

    [HarmonyPriority(int.MaxValue)]
    private static void Prefix(MethodBase __originalMethod)
    {
        var scope = SettingsReadScope.Current;
        if (scope == null || !Routes.TryGetValue(__originalMethod, out var owner) || !owner.enabled) return;
        if (!owner.Available) throw scope.Refuse("settings-read-action-guard-unavailable");
        throw scope.Refuse(owner.failureReason);
    }

    private string? InspectTarget(bool acquired)
    {
        string? failure = null;
        bool inspected = HarmonyPublication.Inspect(target, snapshot =>
        {
            // Suppressing a base-constructor refusal could resume the derived constructor body.
            if (target is ConstructorInfo && snapshot.Finalizers.Count != 0)
            { failure = "A constructor finalizer can suppress the settings-read refusal."; return true; }
            if (snapshot.Prefixes.Any(patch => patch.IsFactory))
            { failure = "A prefix factory prevents safe settings-read action ordering inspection."; return true; }
            var owned = snapshot.Prefixes.Where(patch => patch.Owner == OwnedHarmonyPatch.OwnerId).ToArray();
            if (!acquired)
            {
                if (owned.Length != 0) failure = "The owned settings-read action prefix slot is already occupied.";
                return true;
            }
            if (owned.Length != 1 || owned[0].Method != PrefixMethod)
            { failure = "The acquired settings-read action prefix is absent or duplicated."; return true; }
            var sorted = PatchProcessor.GetSortedPatchMethods(target, snapshot.Prefixes.Select(patch => patch.ToOrderingPatch()).ToArray());
            if (sorted.Count == 0 || sorted[0] != PrefixMethod)
                failure = "A foreign prefix precedes the settings-read action guard.";
            return true;
        });
        return inspected ? failure : "Published settings-read action metadata is busy or unavailable.";
    }

    private IReadOnlyList<Patch> ReadOwnedSlot()
    {
        Patch[]? owned = null;
        if (!HarmonyPublication.Inspect(target, snapshot =>
            { owned = snapshot.Prefixes.Where(patch => patch.Owner == OwnedHarmonyPatch.OwnerId).Select(patch => patch.ToOrderingPatch()).ToArray(); return true; }))
            throw new InvalidOperationException("Published settings-read action ownership is busy or unavailable.");
        return owned!;
    }
}
