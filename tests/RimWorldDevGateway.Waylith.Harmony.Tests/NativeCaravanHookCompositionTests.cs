using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using NUnit.Framework;
using Verse;

namespace RimWorldDevGateway.Waylith.Harmony.Tests;

[TestFixture, NonParallelizable]
public sealed class NativeCaravanHookCompositionTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void EntryAndCaravanDispatchAndPendingStatesCoexistAndRejectForeignFinalizers(bool throws)
    {
        var providerType = typeof(VerseGatewayNativeWindowInput);
        Assert.That(providerType.GetField("installed", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null), Is.Null);
        Assert.That(LoadedModManager.RunningModsListForReading, Is.Empty);
        Assert.That(DefDatabase<ThingDef>.AllDefsListForReading, Is.Empty);
        var harmony = new HarmonyLib.Harmony("gateway.caravan-coexistence." + Guid.NewGuid().ToString("N"));
        var dispatch = typeof(Host).GetMethod(nameof(Host.Dispatch))!;
        var pending = typeof(Host).GetMethod(nameof(Host.Pending))!;
        var entry = providerType.GetNestedType("EntryLayoutHooks", BindingFlags.NonPublic)!;
        var caravan = providerType.GetNestedType("CaravanLayoutHooks", BindingFlags.NonPublic)!;
        var caravanPending = providerType.GetNestedType("CaravanPendingHooks", BindingFlags.NonPublic)!;
        var entryFinish = entry.GetMethod("Finish")!; var caravanFinish = caravan.GetMethod("Finish")!;
        var foreign = typeof(NativeCaravanHookCompositionTests).GetMethod(nameof(ForeignFinalizer), BindingFlags.Static | BindingFlags.NonPublic)!;
        var provider = (VerseGatewayNativeWindowInput)RuntimeHelpers.GetUninitializedObject(providerType);
        providerType.GetField("entryHooks", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(provider,
            new List<(MethodInfo, MethodInfo)> { (dispatch, entryFinish) });
        providerType.GetField("caravanHooks", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(provider,
            new List<(MethodInfo, MethodInfo)> { (dispatch, caravanFinish) });
        var known = providerType.GetMethod("KnownLayoutFinalizer", BindingFlags.NonPublic | BindingFlags.Instance)!;
        bool Admitted() => HarmonyLib.Harmony.GetPatchInfo(dispatch)!.Finalizers.All(p =>
            (bool)known.Invoke(provider, new object[] { dispatch, p.PatchMethod })!);
        try
        {
            foreach (var hooks in new[] { entry, caravan })
                harmony.Patch(dispatch, prefix: new HarmonyMethod(hooks.GetMethod("Before")!),
                    postfix: new HarmonyMethod(hooks.GetMethod("After")!), finalizer: new HarmonyMethod(hooks.GetMethod("Finish")!));
            harmony.Patch(pending, prefix: new HarmonyMethod(Outer("BeforeEntryPending")),
                postfix: new HarmonyMethod(Outer("AfterEntryPending")), finalizer: new HarmonyMethod(Outer("FinalizeEntryPending")));
            harmony.Patch(pending, prefix: new HarmonyMethod(caravanPending.GetMethod("BeforeCaravanPending")!),
                postfix: new HarmonyMethod(caravanPending.GetMethod("AfterCaravanPending")!), finalizer: new HarmonyMethod(caravanPending.GetMethod("FinalizeCaravanPending")!));
            var host = new Host { Throws = throws };
            if (throws) Assert.Throws<OwnedException>(() => host.Dispatch(new object(), false));
            else Assert.DoesNotThrow(() => host.Dispatch(new object(), false));
            Assert.That(host.Calls, Is.EqualTo(1)); Assert.That(host.PendingCalls, Is.EqualTo(1));
            Assert.That(Admitted(), Is.True);
            harmony.Patch(dispatch, finalizer: new HarmonyMethod(foreign));
            Assert.That(Admitted(), Is.False, "Even an exception-suppressing foreign finalizer revokes publication authority.");
        }
        finally { harmony.UnpatchAll(harmony.Id); }
        foreach (var target in new[] { dispatch, pending })
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(target)?.Owners.Contains(harmony.Id) ?? false, Is.False);
        Assert.That(LoadedModManager.RunningModsListForReading, Is.Empty);
        Assert.That(DefDatabase<ThingDef>.AllDefsListForReading, Is.Empty);
    }
    private static MethodInfo Outer(string name) => typeof(VerseGatewayNativeWindowInput).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;
    private static Exception? ForeignFinalizer(Exception? __exception) => null;
    private sealed class Host
    {
        public int Calls, PendingCalls;
        public bool Throws;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Dispatch(object ignored, bool drawing) { Calls++; Pending(); if (Throws) throw new OwnedException(); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Pending() { PendingCalls++; }
    }
    private sealed class OwnedException : Exception { }
}
