using System.Runtime.CompilerServices;
using Megamod.Patching;
using Megamod.Settings;
using NUnit.Framework;
using HarmonyLib;

namespace Megamod.HarmonyTests.Settings;

[TestFixture]
public sealed class SettingsLifecyclePumpTests
{
    [Test]
    public void CallbackFailureIsBoundedAndLeavesNativeUpdatesRunning()
    {
        int calls = 0;
        var failures = new List<Exception>();
        var patch = new SettingsLifecyclePumpPatch(typeof(FixtureEvents),
            () => { calls++; throw new InvalidOperationException("wrong thread"); }, failures.Add);
        try
        {
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(new("settings-pump", AdmissionPhase.Early,
                new[] { patch.Candidate }), AdmissionPhase.Early), Is.True);
            patch.Enable();
            Assert.DoesNotThrow(() => FixtureEvents.LongEventsUpdate(out _));
            Assert.DoesNotThrow(() => FixtureEvents.LongEventsUpdate(out _));
            Assert.That(calls, Is.EqualTo(1));
            Assert.That(failures.Count, Is.EqualTo(1));
        }
        finally { patch.Candidate.Rollback(); }
    }

    [Test]
    public void ForeignPrefixAndRepeatedValidationPreserveOwnedRollback()
    {
        const string foreignId = "test.megamod.settings-pump.foreign";
        var foreign = new HarmonyLib.Harmony(foreignId);
        var method = typeof(FixtureEvents).GetMethod(nameof(FixtureEvents.LongEventsUpdate))!;
        int pumps = 0;
        var patch = new SettingsLifecyclePumpPatch(typeof(FixtureEvents), () => pumps++, _ => { });
        try
        {
            foreign.Patch(method, prefix: new HarmonyMethod(typeof(SettingsLifecyclePumpTests), nameof(SkipOriginal)));
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(new("settings-pump", AdmissionPhase.Early,
                new[] { patch.Candidate }), AdmissionPhase.Early), Is.True);
            patch.Enable();
            FixtureEvents.LongEventsUpdate(out _);
            Assert.That(pumps, Is.EqualTo(1));
            Assert.That(patch.Candidate.Validate(), Is.Not.Null);
            patch.Candidate.Rollback();
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(method)!.Owners, Does.Contain(foreignId));
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(method)!.Owners, Does.Not.Contain(OwnedHarmonyPatch.OwnerId));
        }
        finally { patch.Candidate.Rollback(); foreign.Unpatch(method, HarmonyPatchType.Prefix, foreignId); }
    }

    private static bool SkipOriginal() => false;

    [TestCase(typeof(RefEvents))]
    [TestCase(typeof(WrongReturnEvents))]
    [TestCase(typeof(GenericEvents))]
    public void ChangedCallableShapeCannotAdmitTheLifecyclePump(Type targetType)
    {
        var patch = new SettingsLifecyclePumpPatch(targetType, () => { }, _ => { });
        Assert.That(patch.Candidate.Validate(), Is.Not.Null);
    }

    public static class RefEvents { public static void LongEventsUpdate(ref bool changed) { } }
    public static class WrongReturnEvents { public static bool LongEventsUpdate(out bool changed) { changed = false; return true; } }
    public static class GenericEvents { public static void LongEventsUpdate<T>(out bool changed) { changed = false; } }

    [Test]
    public void AdmittedPumpRunsAfterTheNativeUpdateWithoutChangingItsResult()
    {
        var observed = new List<int>();
        var patch = new SettingsLifecyclePumpPatch(typeof(FixtureEvents), () => observed.Add(FixtureEvents.Calls), _ => { });
        FixtureEvents.Calls = 0;
        try
        {
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(new("settings-pump", AdmissionPhase.Early,
                new[] { patch.Candidate }), AdmissionPhase.Early), Is.True);
            FixtureEvents.LongEventsUpdate(out _);
            Assert.That(observed, Is.Empty);
            patch.Enable();
            FixtureEvents.LongEventsUpdate(out bool changed);
            Assert.That(observed, Is.EqualTo(new[] { 2 }));
            Assert.That(changed, Is.True);
        }
        finally { patch.Candidate.Rollback(); }
    }

    public static class FixtureEvents
    {
        public static int Calls;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void LongEventsUpdate(out bool sceneChanged) { Calls++; sceneChanged = true; }
    }
}
