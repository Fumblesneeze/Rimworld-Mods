using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using Megamod.Patching;
using UnityEngine;
using Verse;

namespace Megamod.Features.SettingsSearch.Runtime;

// Observe the nongeneric record's drawing scope; never detour shared generic TabDrawer code.
internal sealed class SettingsForeignTabScopes : IDisposable
{
    private readonly AdmissionCoordinator admission;
    private readonly List<(MethodInfo Target, HarmonyPatchType Kind, MethodInfo Callback, OwnedHarmonyPatch Receipt)> patches = new();
    private readonly List<PatchCandidate> candidates = new();
    private readonly PatchGroup group;
    private bool initialized;

    internal SettingsForeignTabScopes(Action<string> warning)
    {
        admission = new(warning);
        Bind();
        group = new("settings-search.foreign-tab-scopes", AdmissionPhase.GameReady, candidates.ToArray());
    }
    internal bool Initialize() => initialized = admission.Apply(group, AdmissionPhase.GameReady);
    internal bool Available
    {
        get
        {
            if (!initialized) return false;
            try { return patches.All(patch => Read(patch.Target, patch.Kind) is { Count: 1 } slot && slot[0].PatchMethod == patch.Callback); }
            catch (Exception) { return false; }
        }
    }
    public void Dispose()
    {
        initialized = false;
        List<Exception>? errors = null;
        for (int index = patches.Count - 1; index >= 0; index--)
            try { patches[index].Receipt.Rollback(); }
            catch (Exception error) { (errors ??= new()).Add(error); }
        if (errors != null) throw new AggregateException("Settings tab observation cleanup failed.", errors);
    }
    private void Bind()
    {
        var target = typeof(TabRecord).GetMethod(nameof(TabRecord.Draw), BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly,
            null, new[] { typeof(Rect) }, null);
        foreach (var pair in new[] { (HarmonyPatchType.Prefix, nameof(Enter)), (HarmonyPatchType.Finalizer, nameof(Exit)) })
        {
            var callback = typeof(SettingsForeignTabScopes).GetMethod(pair.Item2, BindingFlags.Static | BindingFlags.NonPublic)!;
            var receipt = target == null ? null : new OwnedHarmonyPatch(target, pair.Item1, callback, () => Read(target, pair.Item1));
            if (target != null) patches.Add((target, pair.Item1, callback, receipt!));
            string id = "settings-search.foreign-tabs.TabRecord.Draw." + pair.Item1;
            candidates.Add(new(id, "SS-FOREIGN-OBSERVATION", target?.ToString() ?? "TabRecord.Draw", "automatic tab navigation",
                "captured controls and ordinary settings pages", () => target?.GetMethodBody() == null || target.IsGenericMethod || target.ReturnType != typeof(void)
                    ? "The logical native tab callable is unavailable." : receipt!.ValidateOwnership(),
                () => receipt!.Install(), () => receipt?.Rollback()));
        }
    }
    private static IReadOnlyList<Patch> Read(MethodInfo target, HarmonyPatchType kind)
    {
        Patch[]? result = null;
        if (!HarmonyPublication.Inspect(target, snapshot =>
            {
                var slot = kind == HarmonyPatchType.Prefix ? snapshot.Prefixes : snapshot.Finalizers;
                result = slot.Where(patch => patch.Owner == OwnedHarmonyPatch.OwnerId).Select(patch => patch.ToOrderingPatch()).ToArray();
                return true;
            })) throw new InvalidOperationException("settings-foreign-tab-publication-unavailable");
        return result!;
    }
    private static void Enter(out IDisposable? __state) => __state = SettingsForeignRecorder.EnterTabScope();
    private static void Exit(IDisposable? __state, Exception? __exception)
    {
        if (__exception != null) SettingsForeignRecorder.TabScopeFailed();
        __state?.Dispose();
    }
}
