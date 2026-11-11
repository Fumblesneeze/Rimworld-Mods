using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Patching;

[TestFixture, NonParallelizable]
public sealed class HarmonyPublicationIdentityTests
{
    private static readonly MethodInfo Target = typeof(HarmonyPublicationIdentityTests).GetMethod(nameof(Native))!;
    private static int attributes;

    [Test]
    public void IdentityRemainsCurrentOnlyUntilThatTargetIsRepublishedWithoutRunningForeignCode()
    {
        var owner = new HarmonyLib.Harmony("tests.publication.identity");
        try
        {
            owner.Patch(Target, prefix: new HarmonyMethod(typeof(HarmonyPublicationIdentityTests), nameof(Prefix)));
            var first = Capture();
            attributes = 0;
            for (int count = 0; count < 20; count++) Assert.That(first.IsCurrent(), Is.True);
            Assert.That(attributes, Is.Zero);
            owner.Unpatch(Target, HarmonyPatchType.Prefix, owner.Id);
            Assert.That(first.IsCurrent(), Is.False);
            owner.Patch(Target, prefix: new HarmonyMethod(typeof(HarmonyPublicationIdentityTests), nameof(Prefix)));
            var second = Capture();
            Assert.That(first.IsCurrent(), Is.False, "Restored metadata does not revive an older publication.");
            Assert.That(second.IsCurrent(), Is.True);
            Assert.That(HarmonyPublication.Inspect(Target, _ =>
            {
                Assert.That(second.IsCurrent(), Is.False, "No reentrant composition authority.");
                var other = Task.Run(second.IsCurrent);
                Assert.That(other.Wait(TimeSpan.FromSeconds(3)), Is.True);
                Assert.That(other.Result, Is.False, "Busy composition is bounded and unavailable.");
                return true;
            }), Is.True);
        }
        finally { owner.Unpatch(Target, HarmonyPatchType.All, owner.Id); }
    }
    private static HarmonyPublication.Identity Capture()
    {
        HarmonyPublication.Identity? result = null;
        Assert.That(HarmonyPublication.Inspect(Target, snapshot => { result = snapshot.Identity; return true; }), Is.True);
        return result!;
    }

    [TestCase(false)]
    [TestCase(true)]
    public void BusyStateMonitorRefusesInspectionWithoutMisclassifyingMetadata(bool pair)
    {
        var owner = new HarmonyLib.Harmony("tests.publication.state-contention");
        owner.Patch(Target, prefix: new HarmonyMethod(typeof(HarmonyPublicationIdentityTests), nameof(Prefix)));
        var shared = typeof(PatchProcessor).Assembly.GetType("HarmonyLib.HarmonySharedState")!;
        object state = shared.GetField("state", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
        int callbacks = 0;
        try
        {
            Monitor.Enter(state);
            try
            {
                var attempt = Task.Run(() =>
                {
                    try
                    {
                        bool result = pair
                            ? HarmonyPublication.InspectPair(Target, Target, (_, _) => { callbacks++; return true; })
                            : HarmonyPublication.Inspect(Target, _ => { callbacks++; return true; });
                        return (Result: result, Error: (Exception?)null);
                    }
                    catch (Exception error) { return (Result: false, Error: error); }
                });
                Assert.That(attempt.Wait(TimeSpan.FromSeconds(3)), Is.True, "State contention must not block the caller.");
                Assert.That(attempt.Result.Error, Is.Null, "A busy monitor is temporary unavailability, not malformed metadata.");
                Assert.That(attempt.Result.Result, Is.False);
                Assert.That(callbacks, Is.Zero);
            }
            finally { Monitor.Exit(state); }
            Assert.That(HarmonyPublication.Inspect(Target, _ => true), Is.True);
        }
        finally { owner.Unpatch(Target, HarmonyPatchType.All, owner.Id); }
    }
    [AttributeUsage(AttributeTargets.Parameter)] public sealed class CountAttribute : Attribute { public CountAttribute() => attributes++; }
    public static void Prefix([Count] int value) { }
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Native(int value) => value;
}
