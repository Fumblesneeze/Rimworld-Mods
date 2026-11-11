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
    [AttributeUsage(AttributeTargets.Parameter)] public sealed class CountAttribute : Attribute { public CountAttribute() => attributes++; }
    public static void Prefix([Count] int value) { }
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Native(int value) => value;
}
