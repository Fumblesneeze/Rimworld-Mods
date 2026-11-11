using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Patching;
using NUnit.Framework;
using HarmonyInstance = HarmonyLib.Harmony;

namespace Megamod.Harmony.Tests.Patching;

[TestFixture, NonParallelizable]
public sealed class OwnedHarmonyPatchTests
{
    [Test]
    public void AtomicGroupRemovesNewMembersAfterHarmonyInstallationThrows()
    {
        const string foreignOwner = "tests.megamod.harmony.atomic-foreign";
        var foreign = new HarmonyInstance(foreignOwner);
        MethodInfo firstTarget = typeof(OwnedHarmonyPatchTests).GetMethod(nameof(Sample))!;
        MethodInfo secondTarget = typeof(OwnedHarmonyPatchTests).GetMethod(nameof(OtherSample))!;
        MethodInfo callback = typeof(OwnedHarmonyPatchTests).GetMethod(nameof(Increment))!;
        MethodInfo brokenCallback = typeof(OwnedHarmonyPatchTests).GetMethod(nameof(BrokenTranspiler))!;
        var first = new OwnedHarmonyPatch(firstTarget, HarmonyPatchType.Postfix, callback);
        var second = new OwnedHarmonyPatch(secondTarget, HarmonyPatchType.Transpiler, brokenCallback);
        var warnings = new List<string>();
        var coordinator = new AdmissionCoordinator(warnings.Add);
        var group = new PatchGroup("diagnostics.definition-reporting", AdmissionPhase.Early,
            [new("DDI-REPORT-WARNING", "DDI-REPORT-WARNING", "host warning fixture", "context", "native",
                 first.ValidateOwnership, first.Install, first.Rollback),
             new("DDI-REPORT-ERROR", "DDI-REPORT-ERROR", "host error fixture", "context", "native",
                 second.ValidateOwnership, second.Install, second.Rollback)]);
        try
        {
            foreign.Patch(firstTarget, postfix: new HarmonyMethod(callback));

            Assert.That(coordinator.Apply(group, AdmissionPhase.Early), Is.False);
            Assert.That(coordinator.IsAdmitted(group.Id), Is.False);
            Assert.That(Sample(1), Is.EqualTo(2));
            Assert.That(OtherSample(1), Is.EqualTo(1));
            Assert.That(HarmonyInstance.GetPatchInfo(firstTarget).Postfixes.Select(patch => patch.owner),
                Is.EquivalentTo(new[] { foreignOwner }));
            Assert.That(HarmonyInstance.GetPatchInfo(secondTarget)?.Transpilers.Any(patch => patch.owner == OwnedHarmonyPatch.OwnerId),
                Is.Not.True);
            Assert.That(warnings, Has.Count.EqualTo(2));
            Assert.That(warnings[0], Does.Contain("fixture installation failure"));
        }
        finally
        {
            var own = new HarmonyInstance(OwnedHarmonyPatch.OwnerId);
            try { own.Unpatch(firstTarget, HarmonyPatchType.Postfix, OwnedHarmonyPatch.OwnerId); }
            finally
            {
                try { own.Unpatch(secondTarget, HarmonyPatchType.Transpiler, OwnedHarmonyPatch.OwnerId); }
                finally { foreign.Unpatch(firstTarget, HarmonyPatchType.Postfix, foreignOwner); }
            }
        }
    }

    [Test]
    public void ChangedOwnedSlotDoesNotAuthorizeRemovingAnotherMember()
    {
        var otherOwned = new HarmonyInstance(OwnedHarmonyPatch.OwnerId);
        MethodInfo target = typeof(OwnedHarmonyPatchTests).GetMethod(nameof(Sample))!;
        MethodInfo callback = typeof(OwnedHarmonyPatchTests).GetMethod(nameof(Increment))!;
        MethodInfo otherCallback = typeof(OwnedHarmonyPatchTests).GetMethod(nameof(AddTen))!;
        var candidate = new OwnedHarmonyPatch(target, HarmonyPatchType.Postfix, callback);
        try
        {
            candidate.Install();
            otherOwned.Patch(target, postfix: new HarmonyMethod(otherCallback));

            Assert.Throws<InvalidOperationException>(candidate.Rollback);
            Assert.That(Sample(1), Is.EqualTo(12));
            Assert.That(HarmonyInstance.GetPatchInfo(target).Postfixes, Has.Count.EqualTo(2));
        }
        finally
        {
            otherOwned.Unpatch(target, HarmonyPatchType.Postfix, OwnedHarmonyPatch.OwnerId);
        }
        Assert.That(Sample(1), Is.EqualTo(1));
    }

    [Test]
    public void PreexistingOwnedSlotCannotBeAcquiredOrRemoved()
    {
        var existing = new HarmonyInstance(OwnedHarmonyPatch.OwnerId);
        MethodInfo target = typeof(OwnedHarmonyPatchTests).GetMethod(nameof(Sample))!;
        MethodInfo callback = typeof(OwnedHarmonyPatchTests).GetMethod(nameof(Increment))!;
        var candidate = new OwnedHarmonyPatch(target, HarmonyPatchType.Postfix, callback);
        try
        {
            existing.Patch(target, postfix: new HarmonyMethod(callback));

            string? reason = candidate.ValidateOwnership();
            Assert.That(reason, Is.Not.Null);
            Assert.That(reason, Does.Contain("safe rollback"));
            Assert.Throws<InvalidOperationException>(candidate.Install);
            candidate.Rollback();

            Assert.That(Sample(1), Is.EqualTo(2));
            Assert.That(HarmonyInstance.GetPatchInfo(target).Postfixes, Has.Count.EqualTo(1));
        }
        finally
        {
            existing.Unpatch(target, HarmonyPatchType.Postfix, OwnedHarmonyPatch.OwnerId);
        }
        Assert.That(Sample(1), Is.EqualTo(1));
    }

    [Test]
    public void RollbackPreservesTheSameCallbackRegisteredByAForeignOwner()
    {
        const string foreignOwner = "tests.megamod.harmony.foreign";
        var foreign = new HarmonyInstance(foreignOwner);
        MethodInfo target = typeof(OwnedHarmonyPatchTests).GetMethod(nameof(Sample))!;
        MethodInfo callback = typeof(OwnedHarmonyPatchTests).GetMethod(nameof(Increment))!;
        var owned = new OwnedHarmonyPatch(target, HarmonyPatchType.Postfix, callback);
        try
        {
            foreign.Patch(target, postfix: new HarmonyMethod(callback));
            Assert.That(Sample(1), Is.EqualTo(2));
            Assert.That(owned.ValidateOwnership(), Is.Null);

            owned.Install();
            Assert.That(Sample(1), Is.EqualTo(3));
            owned.Rollback();

            Assert.That(Sample(1), Is.EqualTo(2));
            Assert.That(HarmonyInstance.GetPatchInfo(target).Postfixes.Select(patch => patch.owner),
                Is.EquivalentTo(new[] { foreignOwner }));
        }
        finally
        {
            try { new HarmonyInstance(OwnedHarmonyPatch.OwnerId).Unpatch(target, HarmonyPatchType.Postfix, OwnedHarmonyPatch.OwnerId); }
            finally { foreign.Unpatch(target, HarmonyPatchType.Postfix, foreignOwner); }
        }
        Assert.That(Sample(1), Is.EqualTo(1));
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Sample(int value) => value;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int OtherSample(int value) => value;

    public static void Increment(ref int __result) => __result++;
    public static void AddTen(ref int __result) => __result += 10;
    public static IEnumerable<CodeInstruction> BrokenTranspiler(IEnumerable<CodeInstruction> instructions) =>
        throw new InvalidOperationException("fixture installation failure");
}
