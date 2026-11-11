using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Patching;

[TestFixture, NonParallelizable]
public sealed class HarmonyPublicationPairTests
{
    private static readonly MethodInfo First = typeof(HarmonyPublicationPairTests).GetMethod(nameof(One))!;
    private static readonly MethodInfo Second = typeof(HarmonyPublicationPairTests).GetMethod(nameof(Two))!;
    private static int constructions;
    [Test]
    public void BothPublishedTargetsAreInspectedWithinOneNonreentrantInterval()
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.pair");
        using var trying = new ManualResetEventSlim();
        using var finished = new ManualResetEventSlim();
        Task? contender = null;
        try
        {
            var prefix = new HarmonyMethod(typeof(HarmonyPublicationPairTests).GetMethod(nameof(Observe))!);
            foreign.Patch(First, prefix: prefix); foreign.Patch(Second, prefix: prefix);
            constructions = 0;
            Assert.That(HarmonyPublication.InspectPair(First, Second, (one, two) =>
            {
                Assert.That(one.Prefixes.Single().Owner, Is.EqualTo(foreign.Id));
                Assert.That(two.Prefixes.Single().Owner, Is.EqualTo(foreign.Id));
                Assert.That(HarmonyPublication.InspectPair(First, Second, (_, _) => throw new AssertionException("reentry")), Is.False);
                var busy = Task.Run(() => HarmonyPublication.InspectPair(First, Second, (_, _) => throw new AssertionException("busy")));
                Assert.That(busy.Wait(TimeSpan.FromSeconds(3)), Is.True);
                Assert.That(busy.Result, Is.False);
                Assert.That(constructions, Is.Zero);
                contender = Task.Run(() => { trying.Set(); foreign.Unpatch(Second, HarmonyPatchType.Prefix, foreign.Id); finished.Set(); });
                Assert.That(trying.Wait(TimeSpan.FromSeconds(3)), Is.True);
                Assert.That(finished.Wait(TimeSpan.FromMilliseconds(50)), Is.False, "The second target cannot publish while the pair callback owns the provider interval.");
                return true;
            }), Is.True);
            Assert.That(finished.Wait(TimeSpan.FromSeconds(3)), Is.True);
        }
        finally
        {
            try { if (contender != null) Assert.That(contender.Wait(TimeSpan.FromSeconds(3)), Is.True); }
            finally { foreign.Unpatch(First, HarmonyPatchType.All, foreign.Id); foreign.Unpatch(Second, HarmonyPatchType.All, foreign.Id); }
        }
    }
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class CountAttribute : Attribute { public CountAttribute() => constructions++; }
    public static void Observe([Count] int value) { }
    [MethodImpl(MethodImplOptions.NoInlining)] public static int One(int value) => value;
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Two(int value) => value;
}
