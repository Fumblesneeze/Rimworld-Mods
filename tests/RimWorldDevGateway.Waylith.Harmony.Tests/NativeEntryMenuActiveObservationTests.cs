using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using NUnit.Framework;
using Verse;

namespace RimWorldDevGateway.Waylith.Harmony.Tests;

[TestFixture, NonParallelizable]
public sealed class NativeEntryMenuActiveObservationTests
{
    [TestCase("none", true)]
    [TestCase("late-menu-exception", false)]
    [TestCase("late-layout-exception", false)]
    [TestCase("late-event-replacement", false)]
    public void ActiveObservationSurvivesOnlyTheCompleteNativeStyleLayout(string fault, bool accepted)
    {
        Assert.That(LoadedModManager.RunningModsListForReading, Is.Empty);
        Assert.That(DefDatabase<ThingDef>.AllDefsListForReading, Is.Empty);
        var harmony = new HarmonyLib.Harmony("gateway.entry-active." + Guid.NewGuid().ToString("N"));
        var dispatch = typeof(OwnedHost).GetMethod(nameof(OwnedHost.Dispatch))!;
        var menu = typeof(OwnedHost).GetMethod(nameof(OwnedHost.Menu))!;
        var late = typeof(OwnedHost).GetMethod(nameof(OwnedHost.LateOnGui))!;
        try
        {
            harmony.Patch(menu, postfix: new HarmonyMethod(Callback(nameof(LateMenuPostfix))));
            harmony.Patch(late, postfix: new HarmonyMethod(Callback(nameof(LateLayoutPostfix))));
            harmony.Patch(dispatch, finalizer: new HarmonyMethod(Callback(nameof(FinishLayout))));
            var host = new OwnedHost(fault);
            Assert.DoesNotThrow(host.Dispatch, "The native-style dispatcher swallows OnGUI failures.");
            Assert.That(host.Inventory.Find("options") != null, Is.EqualTo(accepted));
            Assert.That(host.Caught, Is.EqualTo(fault.EndsWith("exception", StringComparison.Ordinal)));
        }
        finally { harmony.UnpatchAll(harmony.Id); }
        foreach (var target in new[] { dispatch, menu, late })
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(target)?.Owners.Contains(harmony.Id) ?? false, Is.False);
    }

    private static MethodInfo Callback(string name) => typeof(NativeEntryMenuActiveObservationTests)
        .GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;
    private static void LateMenuPostfix(OwnedHost __instance)
    { if (__instance.Fault == "late-menu-exception") throw new OwnedException(); }
    private static void LateLayoutPostfix(OwnedHost __instance)
    {
        if (__instance.Fault == "late-layout-exception") throw new OwnedException();
        if (__instance.Fault == "late-event-replacement") __instance.Input = new();
    }
    private static Exception? FinishLayout(OwnedHost __instance, Exception? __exception)
    {
        __instance.Observation.Complete(__instance.Input, __instance, __instance, false, 2, 2,
            __exception == null, true);
        return __exception;
    }

    // Owned host models the inspected native catch/finally control flow. The observation is
    // the production policy object, active throughout the nested and enclosing patched calls.
    private sealed class OwnedHost
    {
        public readonly GatewayNativeEntryMenuInventory Inventory = new();
        public readonly GatewayNativeEntryMenuObservation Observation;
        public object Input = new();
        public readonly string Fault;
        public bool Caught;
        public OwnedHost(string fault)
        { Fault = fault; Observation = new(Inventory, 1, Input, this, this, false, 2, 2); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Dispatch()
        {
            try
            {
                Menu(); LateOnGui();
                Observation.BeginPendingWindows();
                try { }
                finally { Observation.ClearPendingWindows(); }
                Observation.EndPendingWindows(true);
            }
            catch (OwnedException)
            { Caught = true; Observation.ClearPendingWindows(); }
            Assert.That(Inventory.Find("options"), Is.Null, "Even a successful menu remains provisional until dispatch finalization.");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Menu()
        {
            Observation.Observe(new(this, this, this, this, this, this, true, 1600, 900, 1,
                "options", "Options", (Action)(() => { }), 300, 400, 170, 45, true, 1));
            Observation.MenuReturned(true);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void LateOnGui() { }
    }
    private sealed class OwnedException : Exception { }
}
