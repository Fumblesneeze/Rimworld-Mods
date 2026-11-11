using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using NUnit.Framework;

namespace RimWorldDevGateway.Waylith.Harmony.Tests;

[TestFixture, NonParallelizable]
public sealed class NativeDispatchHookCoexistenceTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void ProductionClickAndTextCallbacksCoexistAndCleanUpOnOneTarget(bool throws)
    {
        // No provider is constructed: the production callbacks exercise their absent-provider
        // path, while Harmony must still compile their real typed __state locals and finalizers.
        Assert.That(typeof(VerseGatewayNativeWindowInput).GetField("installed", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null), Is.Null);
        Assert.That(Verse.LoadedModManager.RunningModsListForReading, Is.Empty);
        Assert.That(Verse.DefDatabase<Verse.ThingDef>.AllDefsListForReading, Is.Empty);
        var owner = "gateway.native-text.coexistence." + Guid.NewGuid().ToString("N");
        var harmony = new HarmonyLib.Harmony(owner);
        var target = typeof(HostDispatch).GetMethod(nameof(HostDispatch.DispatchInput))!;
        var original = new HostDispatch { Throws = throws };
        try
        {
            harmony.Patch(target, prefix: new HarmonyMethod(Callback("BeforeNativeDispatch")) { priority = Priority.Last },
                postfix: new HarmonyMethod(Callback("AfterNativeDispatch")) { priority = Priority.First },
                finalizer: new HarmonyMethod(Callback("FinalizeNativeDispatch")) { priority = Priority.Last });
            harmony.Patch(target, prefix: new HarmonyMethod(Callback("BeforeNativeTextDispatch")) { priority = int.MinValue },
                postfix: new HarmonyMethod(Callback("AfterNativeTextDispatch")) { priority = Priority.First },
                finalizer: new HarmonyMethod(Callback("FinalizeNativeTextDispatch")) { priority = Priority.Last });
            if (throws) Assert.Throws<OwnedDispatchException>(() => original.DispatchInput(new object()));
            else Assert.DoesNotThrow(() => original.DispatchInput(new object()));
            Assert.That(original.Calls, Is.EqualTo(1));
        }
        finally { harmony.UnpatchAll(owner); }
        Assert.That(HarmonyLib.Harmony.GetPatchInfo(target)?.Owners.Contains(owner) ?? false, Is.False);
        original.Throws = false; original.DispatchInput(new object());
        Assert.That(original.Calls, Is.EqualTo(2), "The unpatched target remains callable.");
        Assert.That(Verse.LoadedModManager.RunningModsListForReading, Is.Empty);
        Assert.That(Verse.DefDatabase<Verse.ThingDef>.AllDefsListForReading, Is.Empty);
    }
    private static MethodInfo Callback(string name)
    {
        static IEnumerable<Type> Types(Type type) => new[] { type }.Concat(type.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic).SelectMany(Types));
        return Types(typeof(VerseGatewayNativeWindowInput)).SelectMany(type => type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
            .Single(method => method.Name == name);
    }
    private sealed class HostDispatch
    {
        public int Calls;
        public bool Throws;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void DispatchInput(object host) { Calls++; if (Throws) throw new OwnedDispatchException(); }
    }
    private sealed class OwnedDispatchException : Exception { }
}
