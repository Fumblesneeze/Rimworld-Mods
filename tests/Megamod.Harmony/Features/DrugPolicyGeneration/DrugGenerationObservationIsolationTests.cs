using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.DrugPolicyGeneration;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.DrugPolicyGeneration;

[TestFixture, NonParallelizable]
public sealed class DrugGenerationObservationIsolationTests
{
    [TestCase("method")]
    [TestCase("declaring type")]
    public void UnprovedGenericRootRefusesWholeBundleBeforeAnyPatch(string kind)
    {
        var ordinary = typeof(Targets).GetMethod(nameof(Targets.Ordinary))!;
        var generic = kind == "method"
            ? typeof(Targets).GetMethod(nameof(Targets.Generic))!.MakeGenericMethod(typeof(Marker))
            : typeof(GenericOwner<Marker>).GetMethod(nameof(GenericOwner<Marker>.Read))!;
        var observation = new DrugGenerationObservedBodies(new[]
        {
            new DrugGenerationObservedBodies.Root(ordinary, _ => null),
            new DrugGenerationObservedBodies.Root(generic, _ => null)
        });
        try
        {
            Assert.Throws<InvalidOperationException>(() => observation.Install());
            Assert.That(observation.Failure, Does.Contain("generic").And.Contain("isolation"));
            Assert.That(observation.Available, Is.False);
            AssertUnpatched(ordinary);
            AssertUnpatched(generic);
            Assert.That(Targets.Ordinary(17), Is.EqualTo(17));
        }
        finally { observation.Rollback(); }
    }

    private static void AssertUnpatched(MethodInfo method) => Assert.That(
        HarmonyPublication.Inspect(method, snapshot => snapshot.Transpilers.All(patch => patch.owner != OwnedHarmonyPatch.OwnerId)),
        Is.True, "Even an earlier ordinary root must remain untouched when the bundle cannot be isolated.");

    public sealed class Marker { }
    public static class Targets
    {
        [MethodImpl(MethodImplOptions.NoInlining)] public static int Ordinary(int value) => value;
        [MethodImpl(MethodImplOptions.NoInlining)] public static int Generic<T>(int value) => value;
    }
    public static class GenericOwner<T>
    {
        [MethodImpl(MethodImplOptions.NoInlining)] public static int Read(int value) => value;
    }
}
