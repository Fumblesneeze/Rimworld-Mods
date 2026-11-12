using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using NUnit.Framework;
using UnityEngine;
using Verse;

namespace RimWorldDevGateway.Waylith.Harmony.Tests;

[TestFixture, NonParallelizable]
public sealed class NativeEntryMenuHookCompositionTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void ProductionEntryObserversPreserveHostCallsAndExceptions(bool throws)
    {
        Assert.That(typeof(VerseGatewayNativeWindowInput).GetField("installed", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null), Is.Null);
        Assert.That(LoadedModManager.RunningModsListForReading, Is.Empty);
        Assert.That(DefDatabase<ThingDef>.AllDefsListForReading, Is.Empty);
        string owner = "gateway.entry-observer." + Guid.NewGuid().ToString("N");
        var harmony = new HarmonyLib.Harmony(owner);
        var menu = typeof(HostMenu).GetMethod(nameof(HostMenu.Draw))!;
        var button = typeof(HostMenu).GetMethod(nameof(HostMenu.Button))!;
        var option = typeof(HostOption).GetMethod(nameof(HostOption.DrawOption))!;
        var dispatch = typeof(HostMenu).GetMethod(nameof(HostMenu.Dispatch))!;
        var pending = typeof(HostMenu).GetMethod(nameof(HostMenu.Pending))!;
        var clear = typeof(HostMenu).GetMethod(nameof(HostMenu.Clear))!;
        try
        {
            harmony.Patch(button, postfix: new HarmonyMethod(Callback("ObserveEntryButton")));
            harmony.Patch(option, prefix: new HarmonyMethod(Callback("BeforeEntryOption")),
                postfix: new HarmonyMethod(Callback("AfterEntryOption")), finalizer: new HarmonyMethod(Callback("FinalizeEntryOption")));
            harmony.Patch(menu, prefix: new HarmonyMethod(Callback("BeforeEntryMenu")),
                postfix: new HarmonyMethod(Callback("AfterEntryMenu")), finalizer: new HarmonyMethod(Callback("FinalizeEntryMenu")));
            harmony.Patch(pending, prefix: new HarmonyMethod(Callback("BeforeEntryPending")),
                postfix: new HarmonyMethod(Callback("AfterEntryPending")), finalizer: new HarmonyMethod(Callback("FinalizeEntryPending")));
            harmony.Patch(clear, prefix: new HarmonyMethod(Callback("BeforeEntryClearPending")));
            var layoutHooks = typeof(VerseGatewayNativeWindowInput).GetNestedType("EntryLayoutHooks", BindingFlags.NonPublic)!;
            harmony.Patch(dispatch, prefix: new HarmonyMethod(layoutHooks.GetMethod("Before")!),
                postfix: new HarmonyMethod(layoutHooks.GetMethod("After")!), finalizer: new HarmonyMethod(layoutHooks.GetMethod("Finish")!));
            var target = new HostMenu(); var child = new HostOption(throws);
            if (throws) Assert.Throws<OwnedException>(() => target.Dispatch(child, false));
            else Assert.That(target.Dispatch(child, false), Is.EqualTo(45));
            Assert.That(target.Calls, Is.EqualTo(1)); Assert.That(child.Calls, Is.EqualTo(1));
        }
        finally { harmony.UnpatchAll(owner); }
        foreach (var target in new[] { menu, option, button, dispatch, pending, clear })
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(target)?.Owners.Contains(owner) ?? false, Is.False);
        Assert.That(LoadedModManager.RunningModsListForReading, Is.Empty);
        Assert.That(DefDatabase<ThingDef>.AllDefsListForReading, Is.Empty);
    }

    private static MethodInfo Callback(string name) => typeof(VerseGatewayNativeWindowInput)
        .GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;
    private sealed class HostMenu
    {
        public int Calls;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public float Dispatch(HostOption option, bool drawing) { float result = Draw(option); Pending(); return result; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Pending() { Clear(); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Clear() { }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public float Draw(HostOption option) { Calls++; return option.DrawOption(default, 170); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool Button(Rect rect, string label, bool background, bool sound, bool active) => active;
    }
    private sealed class HostOption : ListableOption
    {
        private readonly bool throws;
        public int Calls;
        public HostOption(bool throws) : base("Options", () => { }) => this.throws = throws;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override float DrawOption(Vector2 pos, float width)
        { Calls++; HostMenu.Button(default, label, true, true, true); if (throws) throw new OwnedException(); return 45; }
    }
    private sealed class OwnedException : Exception { }
}
