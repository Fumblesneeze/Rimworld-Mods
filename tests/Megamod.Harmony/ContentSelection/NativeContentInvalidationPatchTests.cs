using System.Runtime.CompilerServices;
using System.Reflection;
using HarmonyLib;
using Megamod.Features.ContentSelection.Harmony;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.HarmonyTests.ContentSelection;

[TestFixture, NonParallelizable]
public sealed class NativeContentInvalidationPatchTests
{
    private static Action? nativeBody;
    private static int factoryCalls;

    [Test]
    public void CompatibleForeignFactoryDoesNotPreventObservationalInvalidation()
    {
        var guard = new NativeContentInvalidationPatch(typeof(Loader));
        var target = typeof(Loader).GetMethod(nameof(Loader.HotReloadDefs))!;
        var foreign = new HarmonyLib.Harmony("test.content.definition-observer-factory");
        int effects = 0;
        factoryCalls = 0;
        nativeBody = () => { Assert.That(guard.InvalidatedBy, Is.EqualTo(nameof(Loader.HotReloadDefs))); effects++; };
        try
        {
            foreign.Patch(target, prefix: new HarmonyMethod(typeof(NativeContentInvalidationPatchTests), nameof(ObserverFactory)));
            int afterComposition = factoryCalls;
            foreach (var candidate in guard.Group.Patches) Assert.That(candidate.Validate(), Is.Null);
            Assert.That(factoryCalls, Is.EqualTo(afterComposition), "Admission cannot execute a foreign factory to inspect it.");
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(guard.Group, AdmissionPhase.DefsReady), Is.True);
            Loader.HotReloadDefs();
            Assert.That(effects, Is.EqualTo(1));
            Assert.That(guard.Available, Is.True);
        }
        finally
        {
            foreach (var candidate in guard.Group.Patches.Reverse()) candidate.Rollback();
            foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id);
            nativeBody = null;
        }
    }

    public static MethodInfo ObserverFactory(MethodBase target)
    {
        factoryCalls++;
        return typeof(NativeContentInvalidationPatchTests).GetMethod(nameof(ObserveForeign))!;
    }
    public static void ObserveForeign() { }

    [Test]
    public void WorkerExceptionAndLaterEntriesCannotClearTheFirstInvalidation()
    {
        var guard = new NativeContentInvalidationPatch(typeof(Loader));
        var failure = new InvalidOperationException("native failed");
        nativeBody = () => throw failure;
        try
        {
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(guard.Group, AdmissionPhase.DefsReady), Is.True);
            var observed = Task.Run(() => Assert.Throws<InvalidOperationException>(() => Loader.HotReloadDefs())).GetAwaiter().GetResult();
            Assert.That(observed, Is.SameAs(failure));
            nativeBody = () => { };
            Loader.ClearAllPlayData();
            Assert.That(guard.InvalidatedBy, Is.EqualTo(nameof(Loader.HotReloadDefs)));
        }
        finally
        {
            foreach (var candidate in guard.Group.Patches.Reverse()) candidate.Rollback();
            nativeBody = null;
        }
        Assert.That(guard.Available, Is.False);
        Assert.That(guard.InvalidatedBy, Is.EqualTo(nameof(Loader.HotReloadDefs)), "Rollback cannot certify recovered Defs.");
    }

    [Test]
    public void ForeignSkipStillInvalidatesConservativelyWithoutRunningNativeEffects()
    {
        var guard = new NativeContentInvalidationPatch(typeof(Loader));
        var foreign = new HarmonyLib.Harmony("test.content.definition-skip");
        int effects = 0;
        nativeBody = () => effects++;
        try
        {
            foreign.Patch(typeof(Loader).GetMethod(nameof(Loader.HotReloadDefs))!,
                prefix: new HarmonyMethod(typeof(NativeContentInvalidationPatchTests), nameof(Skip)) { before = new[] { OwnedHarmonyPatch.OwnerId } });
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(guard.Group, AdmissionPhase.DefsReady), Is.True);
            Loader.HotReloadDefs();
            Assert.That(guard.InvalidatedBy, Is.EqualTo(nameof(Loader.HotReloadDefs)));
            Assert.That(effects, Is.Zero);
        }
        finally
        {
            foreach (var candidate in guard.Group.Patches.Reverse()) candidate.Rollback();
            foreign.UnpatchAll(foreign.Id);
            nativeBody = null;
        }
    }
    private static bool Skip() => false;

    [Test]
    public void ChangedSecondCallableRejectsBothBeforeAcquiringAnyPatch()
    {
        var guard = new NativeContentInvalidationPatch(typeof(ChangedLoader));
        try
        {
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(guard.Group, AdmissionPhase.DefsReady), Is.False);
            Assert.That(guard.Available, Is.False);
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(typeof(ChangedLoader).GetMethod(nameof(ChangedLoader.HotReloadDefs))!)?.Owners
                .Contains(OwnedHarmonyPatch.OwnerId) ?? false, Is.False);
        }
        finally { foreach (var candidate in guard.Group.Patches.Reverse()) candidate.Rollback(); }
    }
    private static class ChangedLoader
    {
        [MethodImpl(MethodImplOptions.NoInlining)] public static void HotReloadDefs() { }
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool ClearAllPlayData() => true;
    }
    [TestCase(nameof(Loader.HotReloadDefs))]
    [TestCase(nameof(Loader.ClearAllPlayData))]
    public void NativeEntryInvalidatesBeforeBodyWithoutChangingNativeEffects(string method)
    {
        var guard = new NativeContentInvalidationPatch(typeof(Loader));
        int effects = 0;
        nativeBody = () => { Assert.That(guard.InvalidatedBy, Is.EqualTo(method)); effects++; };
        try
        {
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(guard.Group, AdmissionPhase.DefsReady), Is.True);
            Assert.That(guard.Available, Is.True);
            if (method == nameof(Loader.HotReloadDefs)) Loader.HotReloadDefs(); else Loader.ClearAllPlayData();
            Assert.That(effects, Is.EqualTo(1));
            Assert.That(guard.InvalidatedBy, Is.EqualTo(method));
        }
        finally
        {
            foreach (var candidate in guard.Group.Patches.Reverse()) candidate.Rollback();
            nativeBody = null;
        }
    }
    private static class Loader
    {
        [MethodImpl(MethodImplOptions.NoInlining)] public static void HotReloadDefs() => nativeBody?.Invoke();
        [MethodImpl(MethodImplOptions.NoInlining)] public static void ClearAllPlayData() => nativeBody?.Invoke();
    }
}
