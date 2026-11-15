using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.SettingsSearch.Core;
using Megamod.Features.SettingsSearch.Harmony;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.SettingsSearch;

[TestFixture, NonParallelizable]
public sealed class SettingsReadActionGuardTests
{
    private const string Reason = "Settings reads cannot invoke this action.";
    private static readonly MethodInfo Target = typeof(Actions).GetMethod(nameof(Actions.Invoke))!;
    private SettingsReadActionGuard? guard;
    private HarmonyLib.Harmony foreign = null!;
    private static int foreignCalls, postfixCalls, factoryCalls, attributeCalls;
    private static bool suppressException;

    [SetUp]
    public void SetUp()
    {
        Actions.Calls = foreignCalls = postfixCalls = factoryCalls = attributeCalls = 0;
        suppressException = false;
        foreign = new("tests.settings-read-actions.foreign");
    }

    [TearDown]
    public void TearDown()
    {
        try { guard?.Candidate.Rollback(); }
        finally
        {
            guard = null;
            foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id);
            new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId).Unpatch(Target, HarmonyPatchType.Transpiler, OwnedHarmonyPatch.OwnerId);
        }
    }

    [Test]
    public void ReadScopeRefusesActionBeforeItsBodyAndLeavesOrdinaryCallsIntact()
    {
        guard = new(Target, "TEST-READ-ACTION", Reason);
        Admit(guard);
        Actions.Invoke();
        Assert.That(Actions.Calls, Is.EqualTo(1));
        using (var scope = SettingsReadScope.Begin())
        {
            var error = Assert.Throws<InvalidOperationException>(Actions.Invoke);
            Assert.That(error!.Message, Is.EqualTo(Reason));
            Assert.That(scope.Failed, Is.True);
            Assert.That(scope.FailureReason, Is.EqualTo(Reason));
            Assert.That(Actions.Calls, Is.EqualTo(1));
        }
        Actions.Invoke();
        Assert.That(Actions.Calls, Is.EqualTo(2));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ForeignPrefixAheadOfGuardPreventsEntryWithoutRemovingForeignPatch(bool laterRepatch)
    {
        guard = new(Target, "TEST-READ-ACTION", Reason);
        if (laterRepatch) Admit(guard);
        foreign.Patch(Target, prefix: new HarmonyMethod(typeof(SettingsReadActionGuardTests), nameof(ForeignPrefix))
            { priority = int.MaxValue, before = [OwnedHarmonyPatch.OwnerId] });
        if (!laterRepatch)
        {
            var warnings = new List<string>();
            var coordinator = new AdmissionCoordinator(warnings.Add);
            var group = new PatchGroup("unsafe-read-actions", AdmissionPhase.Early, new[] { guard.Candidate });
            Assert.That(coordinator.Apply(group, AdmissionPhase.Early), Is.False);
            Assert.That(coordinator.Apply(group, AdmissionPhase.Early), Is.False);
            Assert.That(warnings, Has.Count.EqualTo(1));
            Assert.That(warnings[0], Does.Contain("TEST-READ-ACTION").And.Contain("ordinary settings pages"));
        }
        Assert.That(guard.Available, Is.False);
        Assert.Throws<InvalidOperationException>(guard.Enable);
        guard.Candidate.Rollback();
        Actions.Invoke();
        Assert.That(foreignCalls, Is.EqualTo(1));
        Assert.That(Actions.Calls, Is.EqualTo(1));
    }

    [Counting]
    public static void ForeignPrefix() => foreignCalls++;

    [TestCaseSource(nameof(UnsupportedTargets))]
    public void UnsupportedCallableShapeIsRejectedBeforeAcquisition(MethodInfo target)
    {
        var candidate = new SettingsReadActionGuard(target, "TEST-READ-SHAPE", Reason);
        Assert.That(candidate.Candidate.Validate(), Is.Not.Null);
        Assert.That(candidate.Available, Is.False);
        Assert.Throws<InvalidOperationException>(candidate.Enable);
    }

    private static IEnumerable<MethodInfo> UnsupportedTargets()
    {
        yield return typeof(ShapeTargets).GetMethod(nameof(ShapeTargets.Generic))!;
        yield return typeof(ShapeTargets).GetMethod(nameof(ShapeTargets.Generic))!.MakeGenericMethod(typeof(int));
        yield return typeof(ShapeTargets).GetMethod(nameof(ShapeTargets.Asynchronous))!;
        yield return typeof(ShapeTargets).GetMethod(nameof(ShapeTargets.Iterator))!;
        yield return typeof(AbstractTarget).GetMethod(nameof(AbstractTarget.Invoke))!;
        yield return typeof(GenericTarget<>).GetMethod(nameof(GenericTarget<int>.Invoke))!;
        yield return typeof(GenericTarget<int>).GetMethod(nameof(GenericTarget<int>.Invoke))!;
    }

    [Test]
    public void InheritedLookupProtectsTheActualDeclarationWithoutConfusingOverrides()
    {
        var inherited = typeof(DerivedAction).GetMethod(nameof(BaseAction.Invoke))!;
        Assert.That(inherited.DeclaringType, Is.EqualTo(typeof(BaseAction)));
        Assert.That(inherited.ReflectedType, Is.EqualTo(typeof(DerivedAction)));
        guard = new(inherited, "TEST-READ-INHERITED", Reason);
        Admit(guard);
        var instance = new DerivedAction();
        instance.Invoke();
        using (var scope = SettingsReadScope.Begin())
        {
            Assert.Throws<InvalidOperationException>(instance.Invoke);
            Assert.That(scope.Failed, Is.True);
            new OverrideAction().Invoke();
        }
        Assert.That(instance.Calls, Is.EqualTo(1));
        guard.Candidate.Rollback();
        instance.Invoke();
        Assert.That(instance.Calls, Is.EqualTo(2));
    }

    [Test]
    public void CompatiblePatchesAndOtherOwnedKindSurviveGuardAndRollback()
    {
        foreign.Patch(Target,
            prefix: new HarmonyMethod(typeof(SettingsReadActionGuardTests), nameof(ForeignPrefix)) { after = [OwnedHarmonyPatch.OwnerId] },
            postfix: new HarmonyMethod(typeof(SettingsReadActionGuardTests), nameof(ForeignPostfix)));
        new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId).Patch(Target,
            transpiler: new HarmonyMethod(typeof(SettingsReadActionGuardTests), nameof(BenignTranspiler)));
        guard = new(Target, "TEST-READ-COMPOSITION", Reason);
        Admit(guard);
        Actions.Invoke();
        Assert.That((Actions.Calls, foreignCalls, postfixCalls), Is.EqualTo((1, 1, 1)));
        using (var scope = SettingsReadScope.Begin()) Assert.Throws<InvalidOperationException>(Actions.Invoke);
        Assert.That((Actions.Calls, foreignCalls, postfixCalls), Is.EqualTo((1, 1, 1)));
        guard.Candidate.Rollback();
        Actions.Invoke();
        Assert.That((Actions.Calls, foreignCalls, postfixCalls), Is.EqualTo((2, 2, 2)));
        Assert.That(HarmonyLib.Harmony.GetPatchInfo(Target)!.Transpilers.Single().owner, Is.EqualTo(OwnedHarmonyPatch.OwnerId));
    }

    [Test]
    public void SuppressedExceptionStillFailsScopeAndInspectionDoesNotExecuteForeignMetadata()
    {
        foreign.Patch(Target,
            prefix: new HarmonyMethod(typeof(SettingsReadActionGuardTests), nameof(ForeignPrefix)) { after = [OwnedHarmonyPatch.OwnerId] },
            finalizer: new HarmonyMethod(typeof(SettingsReadActionGuardTests), nameof(FinalizerFactory)));
        Assert.That(factoryCalls, Is.GreaterThan(0));
        Assert.That(attributeCalls, Is.GreaterThan(0));
        factoryCalls = attributeCalls = 0;
        guard = new(Target, "TEST-READ-SUPPRESSED", Reason);
        Assert.That(guard.Candidate.Validate(), Is.Null);
        Assert.That(factoryCalls + attributeCalls, Is.Zero);
        Admit(guard); // Actual provider composition may consume attributes/factories.
        factoryCalls = attributeCalls = 0;
        suppressException = true;
        using (var scope = SettingsReadScope.Begin())
        {
            Assert.That(guard.Available, Is.True);
            Assert.DoesNotThrow(Actions.Invoke);
            Assert.That(scope.Failed, Is.True);
            Assert.That(scope.FailureReason, Is.EqualTo(Reason));
            scope.Refuse("later-failure");
            Assert.That(scope.FailureReason, Is.EqualTo(Reason));
        }
        Assert.That(Actions.Calls + foreignCalls + factoryCalls + attributeCalls, Is.Zero);
        guard.Candidate.Rollback();
        Actions.Invoke();
        Assert.That(Actions.Calls, Is.EqualTo(1));
    }

    [Test]
    public void PrefixFactoryIsRefusedWithoutInvokingFactoryDuringInspection()
    {
        foreign.Patch(Target, prefix: new HarmonyMethod(typeof(SettingsReadActionGuardTests), nameof(PrefixFactory)));
        Assert.That(factoryCalls, Is.GreaterThan(0));
        factoryCalls = 0;
        guard = new(Target, "TEST-READ-FACTORY", Reason);
        Assert.That(guard.Candidate.Validate(), Does.Contain("factory"));
        Assert.That(factoryCalls, Is.Zero);
        Assert.That(guard.Available, Is.False);
    }

    [Test]
    public void ReachedGuardStillFailsScopeAfterAnUnsafeLaterRepatch()
    {
        guard = new(Target, "TEST-READ-REPATCH", Reason);
        Admit(guard);
        using var scope = SettingsReadScope.Begin();
        // Mid-callback repatching is outside transactional guarantees. The reached guard still
        // blocks its native body and cannot let this page be published as a successful read.
        foreign.Patch(Target, prefix: new HarmonyMethod(typeof(SettingsReadActionGuardTests), nameof(ForeignPrefix))
            { priority = int.MaxValue, before = [OwnedHarmonyPatch.OwnerId] });
        Assert.Throws<InvalidOperationException>(Actions.Invoke);
        Assert.That(scope.Failed, Is.True);
        Assert.That(scope.FailureReason, Is.EqualTo("settings-read-action-guard-unavailable"));
        Assert.That(Actions.Calls, Is.Zero);
        Assert.That(foreignCalls, Is.EqualTo(1));
    }

    [Test]
    public void RemovedGuardInvalidatesEntryAndPreexistingOwnedPrefixIsNeverAcquired()
    {
        guard = new(Target, "TEST-READ-REMOVED", Reason);
        Admit(guard);
        var own = new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId);
        own.Unpatch(Target, HarmonyPatchType.Prefix, OwnedHarmonyPatch.OwnerId);
        Assert.That(guard.Available, Is.False);
        Assert.Throws<InvalidOperationException>(guard.Enable);
        guard.Candidate.Rollback();
        own.Patch(Target, prefix: new HarmonyMethod(typeof(SettingsReadActionGuardTests), nameof(ForeignPrefix)));
        try
        {
            var rejected = new SettingsReadActionGuard(Target, "TEST-READ-OCCUPIED", Reason);
            Assert.That(rejected.Candidate.Validate(), Does.Contain("occupied"));
            rejected.Candidate.Rollback();
            Actions.Invoke();
            Assert.That((Actions.Calls, foreignCalls), Is.EqualTo((1, 1)));
        }
        finally { own.Unpatch(Target, HarmonyPatchType.Prefix, OwnedHarmonyPatch.OwnerId); }
    }

    [Test]
    public void RepeatedAdmissionIsIdempotentAndDuplicateInstallRollsBackOnlyOwnedPrefix()
    {
        foreign.Patch(Target, prefix: new HarmonyMethod(typeof(SettingsReadActionGuardTests), nameof(ForeignPrefix)) { after = [OwnedHarmonyPatch.OwnerId] });
        guard = new(Target, "TEST-READ-FIRST", Reason);
        var second = new SettingsReadActionGuard(Target, "TEST-READ-SECOND", Reason);
        var warnings = new List<string>();
        var coordinator = new AdmissionCoordinator(warnings.Add);
        var failedGroup = new PatchGroup("duplicate-read-actions", AdmissionPhase.Early, new[] { guard.Candidate, second.Candidate });
        Assert.That(coordinator.Apply(failedGroup, AdmissionPhase.Early), Is.False);
        Assert.That(coordinator.Apply(failedGroup, AdmissionPhase.Early), Is.False);
        Assert.That(warnings, Has.Count.EqualTo(2));
        Assert.That(guard.Available || second.Available, Is.False);
        Actions.Invoke();
        Assert.That((Actions.Calls, foreignCalls), Is.EqualTo((1, 1)));
        var success = new PatchGroup("retry-read-actions", AdmissionPhase.Early, new[] { guard.Candidate });
        Assert.That(coordinator.Apply(success, AdmissionPhase.Early), Is.True);
        Assert.That(coordinator.Apply(success, AdmissionPhase.Early), Is.True);
        guard.Enable();
        using (var scope = SettingsReadScope.Begin()) Assert.Throws<InvalidOperationException>(Actions.Invoke);
        Assert.That((Actions.Calls, foreignCalls), Is.EqualTo((1, 1)));
    }

    public static void ForeignPostfix() => postfixCalls++;
    public static IEnumerable<CodeInstruction> BenignTranspiler(IEnumerable<CodeInstruction> instructions) => instructions;
    public static MethodInfo FinalizerFactory(MethodBase original)
    { factoryCalls++; return typeof(SettingsReadActionGuardTests).GetMethod(nameof(ForeignFinalizer))!; }
    public static MethodInfo PrefixFactory(MethodBase original)
    { factoryCalls++; return typeof(SettingsReadActionGuardTests).GetMethod(nameof(ForeignPrefix))!; }
    public static Exception? ForeignFinalizer(Exception? __exception) => suppressException ? null : __exception;
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CountingAttribute : Attribute { public CountingAttribute() => attributeCalls++; }

    private static void Admit(SettingsReadActionGuard candidate)
    {
        var warnings = new List<string>();
        Assert.That(new AdmissionCoordinator(warnings.Add).Apply(new("read-actions", AdmissionPhase.Early,
            new[] { candidate.Candidate }), AdmissionPhase.Early), Is.True, string.Join("; ", warnings));
        candidate.Enable();
    }

    public static class Actions
    {
        public static int Calls;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Invoke() => Calls++;
    }

    public static class ShapeTargets
    {
        public static void Generic<T>() { }
        public static async Task Asynchronous() => await Task.Yield();
        public static IEnumerable<int> Iterator() { yield return 1; }
    }

    public abstract class AbstractTarget { public abstract void Invoke(); }
    public class GenericTarget<T> { public void Invoke() { } }
    public class BaseAction
    {
        public int Calls;
        [MethodImpl(MethodImplOptions.NoInlining)] public virtual void Invoke() => Calls++;
    }
    public sealed class DerivedAction : BaseAction { }
    public sealed class OverrideAction : BaseAction
    {
        [MethodImpl(MethodImplOptions.NoInlining)] public override void Invoke() => Calls++;
    }
}
