using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using HarmonyLib;
using Megamod.Patching;

namespace Megamod.Features.ContentSelection.Harmony;

// A one-way safety latch. A successful Def-generation/reconciliation boundary is still required.
public sealed class NativeContentInvalidationPatch
{
    private static readonly ConcurrentDictionary<MethodBase, NativeContentInvalidationPatch> Routes = new();
    private readonly Registration[] registrations;
    private string? invalidatedBy;
    private int available;
    public NativeContentInvalidationPatch(Type loader)
    {
        registrations = new[] { new Registration("HotReloadDefs"), new Registration("ClearAllPlayData") };
        Group = new PatchGroup("MM.CONTENT.DefinitionLifecycle", AdmissionPhase.DefsReady,
            registrations.Select(member => new PatchCandidate("MM.CONTENT." + member.Name, "MM.CONTENT." + member.Name,
                loader.FullName + "." + member.Name, "Incident exclusions and drug preset filtering during Def rebuilding", "Native incident and preset behavior",
                () => Validate(loader, member), () => Install(member), () => Rollback(member))).ToArray());
    }
    public PatchGroup Group { get; }
    public string? InvalidatedBy => Volatile.Read(ref invalidatedBy);
    public bool Available => Volatile.Read(ref available) != 0;
    private static MethodInfo Callback => typeof(NativeContentInvalidationPatch).GetMethod(nameof(ObserveEntry), BindingFlags.Static | BindingFlags.NonPublic)!;

    private string? Validate(Type loader, Registration member)
    {
        member.Target = loader.GetMethod(member.Name, BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
            null, Type.EmptyTypes, null);
        if (loader.IsGenericType || member.Target == null || member.Target.ReturnType != typeof(void) || member.Target.IsGenericMethod ||
            member.Target.GetMethodBody() == null) return "The exact native Def lifecycle callable changed.";
        member.Receipt ??= new OwnedHarmonyPatch(member.Target, HarmonyPatchType.Prefix, Callback,
            () => ReadOwnedPrefixes(member.Target));
        return member.Receipt.ValidateOwnership();
    }
    private void Install(Registration member)
    {
        if (member.Target == null || member.Receipt == null)
            throw new InvalidOperationException("Def lifecycle target was not validated.");
        if (!Routes.TryAdd(member.Target, this)) throw new InvalidOperationException("Def lifecycle target already has an owned route.");
        member.Receipt.Install();
        member.Installed = true;
        if (registrations.All(entry => entry.Installed)) Volatile.Write(ref available, 1);
    }
    private static IReadOnlyList<Patch> ReadOwnedPrefixes(MethodBase target)
    {
        Patch[]? owned = null;
        if (!HarmonyPublication.Inspect(target, snapshot =>
        {
            // This observer consumes only its own receipt, not foreign order or bodies.
            owned = snapshot.Prefixes.Where(patch => patch.Owner == OwnedHarmonyPatch.OwnerId)
                .Select(patch => patch.ToOrderingPatch()).ToArray();
            return true;
        })) throw new InvalidOperationException("Published Def lifecycle ownership metadata is busy.");
        return owned!;
    }
    private void Rollback(Registration member)
    {
        Volatile.Write(ref available, 0);
        member.Installed = false;
        try { member.Receipt?.Rollback(); }
        finally
        {
            if (member.Target != null && Routes.TryGetValue(member.Target, out var route) && ReferenceEquals(route, this))
                Routes.TryRemove(member.Target, out _);
        }
    }
    [HarmonyPriority(int.MaxValue)]
    private static void ObserveEntry(MethodBase __originalMethod)
    {
        if (Routes.TryGetValue(__originalMethod, out var route))
            Interlocked.CompareExchange(ref route.invalidatedBy, __originalMethod.Name, null);
    }
    private sealed class Registration
    {
        public Registration(string name) => Name = name;
        public string Name { get; }
        public MethodInfo? Target;
        public OwnedHarmonyPatch? Receipt;
        public bool Installed;
    }
}
