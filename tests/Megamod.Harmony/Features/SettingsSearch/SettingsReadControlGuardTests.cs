using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.SettingsSearch.Core;
using Megamod.Features.SettingsSearch.Harmony;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.SettingsSearch;

[TestFixture, NonParallelizable]
public sealed class SettingsReadControlGuardTests
{
    private static readonly MethodInfo TextTarget = typeof(Controls).GetMethod(nameof(Controls.Text))!;
    private static readonly MethodInfo IntTarget = typeof(Controls).GetMethod(nameof(Controls.Numeric))!.MakeGenericMethod(typeof(int));
    private static readonly MethodInfo FloatTarget = typeof(Controls).GetMethod(nameof(Controls.Numeric))!.MakeGenericMethod(typeof(float));
    private static readonly MethodInfo GateTarget = typeof(Controls).GetMethod(nameof(Controls.Gate))!;
    private readonly List<SettingsReadControlGuard> guards = new();
    private readonly HarmonyLib.Harmony foreign = new("tests.settings-read-controls.foreign");
    private static bool suppress;
    private static int factoryCalls, attributeCalls, gateFinalizerCalls;

    [SetUp]
    public void SetUp()
    {
        Controls.ThrowNumbers = suppress = false;
        Controls.GateCalls = Controls.TextCalls = factoryCalls = attributeCalls = gateFinalizerCalls = 0;
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            foreach (var guard in guards.AsEnumerable().Reverse())
                foreach (var candidate in guard.Candidates.Reverse()) candidate.Rollback();
        }
        finally
        {
            guards.Clear();
            foreach (var target in new[] { TextTarget, IntTarget, FloatTarget, GateTarget }) foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id);
            new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId).Unpatch(TextTarget, HarmonyPatchType.Transpiler, OwnedHarmonyPatch.OwnerId);
        }
    }

    [Test]
    public void AReadRetainsOriginalTextWhileOrdinaryNativeCallsKeepTheirResult()
    {
        var guard = Create(TextTarget, typeof(TextCallbacks));
        Admit(guard);
        Assert.That(Controls.Text("original"), Is.EqualTo("ori"));
        using (var scope = SettingsReadScope.Begin())
        {
            Assert.That(Controls.Text("original"), Is.EqualTo("original"));
            Assert.That(Controls.Text(null), Is.Null);
            Assert.That(scope.Failed, Is.False);
        }
        Assert.That(Controls.Text(null), Is.EqualTo(string.Empty));
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void RequiredOuterOrderIsCheckedAtAdmissionAndAfterRepatch(bool finalizer, bool laterRepatch)
    {
        var guard = Create(TextTarget, typeof(TextCallbacks));
        if (laterRepatch) Admit(guard);
        if (finalizer)
            foreign.Patch(TextTarget, finalizer: new HarmonyMethod(typeof(SettingsReadControlGuardTests), nameof(ForeignFinalizer))
                { priority = int.MinValue, after = new[] { OwnedHarmonyPatch.OwnerId } });
        else
            foreign.Patch(TextTarget, prefix: new HarmonyMethod(typeof(SettingsReadControlGuardTests), nameof(ForeignPrefix))
                { priority = int.MaxValue, before = new[] { OwnedHarmonyPatch.OwnerId } });
        if (!laterRepatch)
        {
            var warnings = new List<string>();
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(new("bad-order", AdmissionPhase.Early,
                guard.Candidates), AdmissionPhase.Early), Is.False);
            Assert.That(warnings, Has.Count.EqualTo(2));
        }
        Assert.That(guard.Available, Is.False);
        Assert.That(Controls.Text("original"), Is.EqualTo(finalizer ? "foreign exit" : "for"));
    }

    public static void ForeignPrefix(ref string? __0) => __0 = "foreign entry";
    public static void ForeignFinalizer(ref string? __result) => __result = "foreign exit";

    [TestCase(HarmonyPatchType.Prefix, false)]
    [TestCase(HarmonyPatchType.Prefix, true)]
    [TestCase(HarmonyPatchType.Finalizer, false)]
    [TestCase(HarmonyPatchType.Finalizer, true)]
    public void ForeignReuseOfPairedCallbacksIsRejectedAndSurvivesRollback(HarmonyPatchType kind, bool laterRepatch)
    {
        var guard = Create(TextTarget, typeof(TextCallbacks));
        if (laterRepatch) Admit(guard);
        var callback = typeof(TextCallbacks).GetMethod(kind == HarmonyPatchType.Prefix ? "Prefix" : "Finalizer")!;
        if (kind == HarmonyPatchType.Prefix)
        {
            foreign.Patch(TextTarget, prefix: new HarmonyMethod(typeof(SettingsReadControlGuardTests), nameof(ForeignPrefix)));
            foreign.Patch(TextTarget, prefix: new HarmonyMethod(callback) { priority = Priority.Last });
        }
        else
        {
            foreign.Patch(TextTarget, finalizer: new HarmonyMethod(callback) { priority = Priority.First });
            foreign.Patch(TextTarget, finalizer: new HarmonyMethod(typeof(SettingsReadControlGuardTests), nameof(ForeignFinalizer)));
        }

        if (!laterRepatch)
        {
            var warnings = new List<string>();
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(new("shared-state", AdmissionPhase.Early,
                guard.Candidates), AdmissionPhase.Early), Is.False);
            Assert.That(warnings, Has.Count.EqualTo(2));
        }
        Assert.That(guard.Available, Is.False);
        if (laterRepatch)
        {
            using var scope = SettingsReadScope.Begin();
            Assert.Throws<InvalidOperationException>(() => Controls.Text("original"));
            Assert.That(scope.FailureReason, Is.EqualTo("settings-read-control-guard-unavailable"));
            Assert.That(Controls.TextCalls, Is.Zero);
        }

        foreach (var candidate in guard.Candidates.Reverse()) candidate.Rollback();
        var patches = HarmonyLib.Harmony.GetPatchInfo(TextTarget)!;
        var slot = kind == HarmonyPatchType.Prefix ? patches.Prefixes : patches.Finalizers;
        Assert.That(slot.Count(patch => patch.owner == foreign.Id && patch.PatchMethod == callback), Is.EqualTo(1));
        Assert.That(patches.Prefixes.Concat(patches.Finalizers).Any(patch => patch.owner == OwnedHarmonyPatch.OwnerId), Is.False);
        using var ordinaryScope = SettingsReadScope.Begin();
        Assert.That(Controls.Text("original"), Is.EqualTo(kind == HarmonyPatchType.Prefix ? "for" : "foreign exit"));
        Assert.That(ordinaryScope.Failed, Is.False, "Rollback must disable the route even though the foreign callback remains.");
    }

    [TestCase(HarmonyPatchType.Prefix, false)]
    [TestCase(HarmonyPatchType.Prefix, true)]
    [TestCase(HarmonyPatchType.Postfix, false)]
    [TestCase(HarmonyPatchType.Postfix, true)]
    [TestCase(HarmonyPatchType.Finalizer, false)]
    [TestCase(HarmonyPatchType.Finalizer, true)]
    public void DistinctForeignCallbacksSharingThePairedStateSlotInvalidateTheGuard(HarmonyPatchType kind, bool laterRepatch)
    {
        var guard = Create(TextTarget, typeof(TextCallbacks));
        if (laterRepatch) Admit(guard);
        var callback = typeof(TextCallbacks).GetMethod(nameof(TextCallbacks.ReplaceState))!;
        var method = new HarmonyMethod(callback) { priority = Priority.Normal };
        if (kind == HarmonyPatchType.Prefix) foreign.Patch(TextTarget, prefix: method);
        else if (kind == HarmonyPatchType.Postfix) foreign.Patch(TextTarget, postfix: method);
        else foreign.Patch(TextTarget, finalizer: method);

        if (!laterRepatch)
        {
            var warnings = new List<string>();
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(new("state-slot", AdmissionPhase.Early,
                guard.Candidates), AdmissionPhase.Early), Is.False);
            Assert.That(warnings, Has.Count.EqualTo(2));
        }
        Assert.That(guard.Available, Is.False);
        if (laterRepatch)
        {
            using var scope = SettingsReadScope.Begin();
            Assert.Throws<InvalidOperationException>(() => Controls.Text("original"));
            Assert.That(scope.FailureReason, Is.EqualTo("settings-read-control-guard-unavailable"));
            Assert.That(Controls.TextCalls, Is.Zero);
        }
        foreach (var candidate in guard.Candidates.Reverse()) candidate.Rollback();
        var patches = HarmonyLib.Harmony.GetPatchInfo(TextTarget)!;
        Assert.That(patches.Prefixes.Concat(patches.Postfixes).Concat(patches.Finalizers)
            .Count(patch => patch.owner == foreign.Id && patch.PatchMethod == callback), Is.EqualTo(1));
        Assert.That(Controls.Text("original"), Is.EqualTo("ori"));
    }

    [Test]
    public void ACallbackOnTheSameDeclaringTypeWithoutStateRemainsCompatible()
    {
        foreign.Patch(TextTarget, postfix: new HarmonyMethod(typeof(TextCallbacks), nameof(TextCallbacks.ObserveWithoutState)));
        var guard = Create(TextTarget, typeof(TextCallbacks));
        Admit(guard);
        using var scope = SettingsReadScope.Begin();
        Assert.That(guard.Available, Is.True);
        Assert.That(Controls.Text("original"), Is.EqualTo("original"));
        Assert.That(scope.Failed, Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void UnresolvedPostfixFactoriesRefusePairedReadsWithoutExecution(bool laterRepatch)
    {
        var guard = Create(TextTarget, typeof(TextCallbacks));
        if (laterRepatch) Admit(guard);
        var factory = typeof(SettingsReadControlGuardTests).GetMethod(nameof(TextStateFactory))!;
        foreign.Patch(TextTarget, postfix: new HarmonyMethod(factory));
        Assert.That(factoryCalls, Is.GreaterThan(0), "Actual Harmony composition must resolve the state-writing callback.");
        factoryCalls = 0;

        if (!laterRepatch)
        {
            var warnings = new List<string>();
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(new("postfix-factory", AdmissionPhase.Early,
                guard.Candidates), AdmissionPhase.Early), Is.False);
            Assert.That(warnings, Has.Count.EqualTo(2));
        }
        Assert.That(guard.Available, Is.False);
        if (laterRepatch)
        {
            using var scope = SettingsReadScope.Begin();
            Assert.Throws<InvalidOperationException>(() => Controls.Text("original"));
            Assert.That(scope.FailureReason, Is.EqualTo("settings-read-control-guard-unavailable"));
            Assert.That(Controls.TextCalls, Is.Zero);
        }
        Assert.That(factoryCalls, Is.Zero, "Guard inspection must not execute the unresolved factory.");
        foreach (var candidate in guard.Candidates.Reverse()) candidate.Rollback();
        Assert.That(HarmonyLib.Harmony.GetPatchInfo(TextTarget)!.Postfixes
            .Count(patch => patch.owner == foreign.Id && patch.PatchMethod == factory), Is.EqualTo(1));
        Assert.That(Controls.Text("original"), Is.EqualTo("ori"));
    }

    public static MethodInfo TextStateFactory(MethodBase original)
    { factoryCalls++; return typeof(TextCallbacks).GetMethod(nameof(TextCallbacks.ReplaceState))!; }

    [TestCase(HarmonyPatchType.Prefix)]
    [TestCase(HarmonyPatchType.Finalizer)]
    public void RollbackFailureDisablesTheRouteWhilePreservingTheChangedOwnedSlot(HarmonyPatchType kind)
    {
        var guard = Create(TextTarget, typeof(TextCallbacks));
        Admit(guard);
        var sameOwner = new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId);
        try
        {
            if (kind == HarmonyPatchType.Prefix)
                sameOwner.Patch(TextTarget, prefix: new HarmonyMethod(typeof(SettingsReadControlGuardTests), nameof(ForeignPrefix)));
            else
                sameOwner.Patch(TextTarget, finalizer: new HarmonyMethod(typeof(SettingsReadControlGuardTests), nameof(ForeignFinalizer)));

            Assert.Throws<InvalidOperationException>(() => guard.Candidates[kind == HarmonyPatchType.Prefix ? 0 : 1].Rollback());
            Assert.That(guard.Available, Is.False);
            var patches = HarmonyLib.Harmony.GetPatchInfo(TextTarget)!;
            Assert.That(kind == HarmonyPatchType.Prefix ? patches.Prefixes : patches.Finalizers, Has.Count.EqualTo(2));
            using var scope = SettingsReadScope.Begin();
            Assert.That(SettingsReadControlGuard.ShouldCapture(TextTarget), Is.False);
            Assert.That(Controls.Text("original"), Is.EqualTo(kind == HarmonyPatchType.Prefix ? "for" : "foreign exit"));
            Assert.That(scope.Failed, Is.False);
        }
        finally
        {
            // This fixture owns both colliding registrations; the guard must not remove either.
            sameOwner.Unpatch(TextTarget, kind, OwnedHarmonyPatch.OwnerId);
        }
    }

    [TestCaseSource(nameof(UnsupportedTargets))]
    public void UnsupportedTargetShapeIsRejectedBeforeAcquisition(MethodInfo target)
    {
        var guard = Create(target, typeof(TextCallbacks));
        Assert.That(guard.Candidates[0].Validate(), Is.Not.Null);
        Assert.That(guard.Available, Is.False);
    }

    private static IEnumerable<MethodInfo> UnsupportedTargets()
    {
        yield return typeof(Controls).GetMethod(nameof(Controls.Numeric))!;
        yield return typeof(Controls).GetMethod(nameof(Controls.Numeric))!.MakeGenericMethod(typeof(string));
        yield return typeof(Controls).GetMethod(nameof(Controls.Numeric))!.MakeGenericMethod(typeof(decimal));
        yield return typeof(Controls).GetMethod(nameof(Controls.Async))!;
        yield return typeof(Controls).GetMethod(nameof(Controls.Iterator))!;
        yield return typeof(AbstractControl).GetMethod(nameof(AbstractControl.Invoke))!;
        yield return typeof(GenericControl<>).GetMethod(nameof(GenericControl<int>.Invoke))!;
        yield return typeof(GenericControl<int>).GetMethod(nameof(GenericControl<int>.Invoke))!;
    }

    [Test]
    public void PairedCallbacksRequireTheSameConcreteStateDeclaration()
    {
        var guard = new SettingsReadControlGuard(TextTarget, "TEST-BAD-STATE", typeof(TextCallbacks).GetMethod("Prefix")!,
            typeof(SettingsReadControlGuardTests).GetMethod(nameof(ForeignFinalizer))!);
        Assert.That(guard.Candidates[0].Validate(), Is.Not.Null);
    }

    [Test]
    public void ClosedIntAndFloatTargetsRemainIndependentThroughInvocationRepatchAndCleanup()
    {
        var intGuard = Create(IntTarget, typeof(IntCallbacks));
        var floatGuard = Create(FloatTarget, typeof(FloatCallbacks));
        Admit(intGuard);
        Admit(floatGuard);
        int integer = 17;
        float number = 4.25f;
        string? integerBuffer = null, numberBuffer = null;
        Controls.Numeric(ref integer, ref integerBuffer);
        Controls.Numeric(ref number, ref numberBuffer);
        Assert.That((integer, number, integerBuffer, numberBuffer), Is.EqualTo((0, 0f, "native", "native")));
        AssertScopedNumbers();
        Assert.That(IntCallbacks.Seen, Is.EqualTo(IntTarget));
        Assert.That(FloatCallbacks.Seen, Is.EqualTo(FloatTarget));

        foreign.Patch(IntTarget, prefix: new HarmonyMethod(typeof(SettingsReadControlGuardTests), nameof(ForeignInt))
            { after = new[] { OwnedHarmonyPatch.OwnerId } });
        Assert.That(intGuard.Available && floatGuard.Available, Is.True);
        AssertScopedNumbers();
        Controls.Numeric(ref integer, ref integerBuffer);
        Controls.Numeric(ref number, ref numberBuffer);
        Assert.That((integer, number, integerBuffer, numberBuffer), Is.EqualTo((84, 0f, "foreign integer", "native")));

        foreach (var candidate in intGuard.Candidates.Reverse()) candidate.Rollback();
        Assert.That(!intGuard.Available && floatGuard.Available, Is.True);
        using (var scope = SettingsReadScope.Begin())
        {
            integer = 17; integerBuffer = null;
            Controls.Numeric(ref integer, ref integerBuffer);
            Assert.That((integer, integerBuffer), Is.EqualTo((84, "foreign integer")));
            number = 4.25f; numberBuffer = null;
            Controls.Numeric(ref number, ref numberBuffer);
            Assert.That((number, numberBuffer), Is.EqualTo((4.25f, (string?)null)));
        }
        foreign.Unpatch(IntTarget, HarmonyPatchType.Prefix, foreign.Id);
        Assert.That(floatGuard.Available, Is.True);
        using (var scope = SettingsReadScope.Begin())
        {
            integer = 17; integerBuffer = null;
            Controls.Numeric(ref integer, ref integerBuffer);
            Assert.That((integer, integerBuffer), Is.EqualTo((0, "native")));
            number = 4.25f; numberBuffer = null;
            Controls.Numeric(ref number, ref numberBuffer);
            Assert.That((number, numberBuffer), Is.EqualTo((4.25f, (string?)null)));
        }
    }

    private static void AssertScopedNumbers()
    {
        int integer = -17;
        float number = BitConverter.Int32BitsToSingle(unchecked((int)0x80000000));
        string? integerBuffer = null, numberBuffer = "";
        using var scope = SettingsReadScope.Begin();
        Controls.Numeric(ref integer, ref integerBuffer);
        Controls.Numeric(ref number, ref numberBuffer);
        Assert.That((integer, integerBuffer, numberBuffer), Is.EqualTo((-17, (string?)null, "")));
        Assert.That(BitConverter.SingleToInt32Bits(number), Is.EqualTo(unchecked((int)0x80000000)));
        Assert.That(scope.Failed, Is.False);
    }

    public static bool ForeignInt(ref int __0, ref string? __1)
    { __0 = 84; __1 = "foreign integer"; return false; }

    [Test]
    public void InheritedLookupProtectsOnlyItsActualDeclaration()
    {
        var target = typeof(DerivedControl).GetMethod(nameof(BaseControl.Text))!;
        var guard = Create(target, typeof(TextCallbacks));
        Admit(guard);
        var instance = new DerivedControl();
        Assert.That(instance.Text("original"), Is.EqualTo("ori"));
        using (var scope = SettingsReadScope.Begin())
        {
            Assert.That(instance.Text("original"), Is.EqualTo("original"));
            Assert.That(new OverrideControl().Text("original"), Is.EqualTo("override"));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RefValuesAreRestoredAfterBodyFailureAndEarlierFinalizerWrites(bool suppressFailure)
    {
        foreign.Patch(IntTarget, finalizer: new HarmonyMethod(typeof(SettingsReadControlGuardTests), nameof(RewriteNumberFinalizer)));
        var guard = Create(IntTarget, typeof(IntCallbacks));
        Admit(guard);
        suppress = suppressFailure;
        Controls.ThrowNumbers = true;
        int value = 17;
        string? buffer = null;
        using (var scope = SettingsReadScope.Begin())
        {
            if (suppressFailure) Assert.DoesNotThrow(() => Controls.Numeric(ref value, ref buffer));
            else Assert.That(Assert.Throws<InvalidOperationException>(() => Controls.Numeric(ref value, ref buffer))!.Message,
                Is.EqualTo("control-body"));
            Assert.That((value, buffer), Is.EqualTo((17, (string?)null)));
        }
        Controls.ThrowNumbers = false;
        Controls.Numeric(ref value, ref buffer);
        Assert.That((value, buffer), Is.EqualTo((900, "foreign finalizer")));
    }

    public static Exception? RewriteNumberFinalizer(Exception? __exception, ref int __0, ref string? __1)
    { __0 = 900; __1 = "foreign finalizer"; return suppress ? null : __exception; }

    [Test]
    public void PrefixOnlyGateSkipsOnlyTheNativeBodyAndDoesNotExecuteForeignMetadata()
    {
        foreign.Patch(GateTarget,
            prefix: new HarmonyMethod(typeof(SettingsReadControlGuardTests), nameof(GateObserver)) { after = new[] { OwnedHarmonyPatch.OwnerId } },
            postfix: new HarmonyMethod(typeof(SettingsReadControlGuardTests), nameof(GatePostfixFactory)),
            finalizer: new HarmonyMethod(typeof(SettingsReadControlGuardTests), nameof(GateFinalizerFactory)));
        foreign.Patch(GateTarget, prefix: new HarmonyMethod(typeof(GateCallbacks), nameof(GateCallbacks.Prefix))
            { priority = Priority.Normal, after = new[] { OwnedHarmonyPatch.OwnerId } });
        var guard = Create(GateTarget, typeof(GateCallbacks), hasFinalizer: false);
        Admit(guard);
        Assert.That(guard.Candidates, Has.Count.EqualTo(1));
        Assert.That(factoryCalls, Is.GreaterThan(0));
        Assert.That(attributeCalls, Is.GreaterThan(0));
        factoryCalls = attributeCalls = 0;
        Controls.Gate();
        using (var scope = SettingsReadScope.Begin())
        {
            Assert.That(guard.Available, Is.True);
            Controls.Gate();
            Assert.That(scope.Failed, Is.False);
        }
        Controls.Gate();
        Assert.That(Controls.GateCalls, Is.EqualTo(2));
        Assert.That(gateFinalizerCalls, Is.EqualTo(3), "Foreign finalizers retain their native behavior even when the body is skipped.");
        Assert.That(factoryCalls + attributeCalls, Is.Zero, "Admission must not execute foreign factories or attributes.");
    }

    public static MethodInfo GateFinalizerFactory(MethodBase original)
    { factoryCalls++; return typeof(SettingsReadControlGuardTests).GetMethod(nameof(GateFinalizer))!; }
    public static MethodInfo GatePostfixFactory(MethodBase original)
    { factoryCalls++; return typeof(SettingsReadControlGuardTests).GetMethod(nameof(GateObserver))!; }
    [Counting]
    public static void GateObserver() { }
    public static void GateFinalizer() => gateFinalizerCalls++;
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CountingAttribute : Attribute { public CountingAttribute() => attributeCalls++; }

    [Test]
    public void FailedFinalizerInstallRollsBackThePairButPreservesOtherOwnersAndOwnedKinds()
    {
        foreign.Patch(TextTarget, prefix: new HarmonyMethod(typeof(SettingsReadControlGuardTests), nameof(ForeignPrefix))
            { after = new[] { OwnedHarmonyPatch.OwnerId } });
        new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId).Patch(TextTarget,
            transpiler: new HarmonyMethod(typeof(SettingsReadControlGuardTests), nameof(BenignTranspiler)));
        var guard = Create(TextTarget, typeof(BrokenTextCallbacks));
        var warnings = new List<string>();
        var coordinator = new AdmissionCoordinator(warnings.Add);
        var group = new PatchGroup("broken-finalizer", AdmissionPhase.Early, guard.Candidates);
        Assert.That(coordinator.Apply(group, AdmissionPhase.Early), Is.False);
        Assert.That(coordinator.Apply(group, AdmissionPhase.Early), Is.False);
        Assert.That(warnings, Has.Count.EqualTo(2));
        Assert.That(guard.Available, Is.False);
        Assert.That(Controls.Text("original"), Is.EqualTo("for"));
        Assert.That(HarmonyLib.Harmony.GetPatchInfo(TextTarget)!.Transpilers.Single().owner, Is.EqualTo(OwnedHarmonyPatch.OwnerId));
        var good = Create(TextTarget, typeof(TextCallbacks));
        Admit(good);
        using var scope = SettingsReadScope.Begin();
        Assert.That(Controls.Text("original"), Is.EqualTo("original"));
    }

    public static IEnumerable<CodeInstruction> BenignTranspiler(IEnumerable<CodeInstruction> instructions) => instructions;

    [Test]
    public void AFinalizerSuppressingAnUnavailableGuardCannotEraseItsLatchedFailure()
    {
        var guard = Create(TextTarget, typeof(TextCallbacks));
        Admit(guard);
        foreign.Patch(TextTarget, finalizer: new HarmonyMethod(typeof(SettingsReadControlGuardTests), nameof(SuppressAndRewrite))
            { priority = int.MinValue, after = new[] { OwnedHarmonyPatch.OwnerId } });
        Assert.That(guard.Available, Is.False);
        using var scope = SettingsReadScope.Begin();
        Assert.That(Controls.Text("original"), Is.EqualTo("suppressed"));
        Assert.That(Controls.TextCalls, Is.Zero);
        Assert.That(scope.Failed, Is.True);
        Assert.That(scope.FailureReason, Is.EqualTo("settings-read-control-guard-unavailable"));
        scope.Refuse("later");
        Assert.That(scope.FailureReason, Is.EqualTo("settings-read-control-guard-unavailable"));
    }

    public static Exception? SuppressAndRewrite(Exception? __exception, ref string? __result)
    { __result = "suppressed"; return null; }

    [TestCase(false)]
    [TestCase(true)]
    public void ConsumedFactoriesAreRejectedWithoutExecutingThemDuringAdmission(bool finalizer)
    {
        if (finalizer) foreign.Patch(TextTarget, finalizer: new HarmonyMethod(typeof(SettingsReadControlGuardTests), nameof(TextFinalizerFactory)));
        else foreign.Patch(TextTarget, prefix: new HarmonyMethod(typeof(SettingsReadControlGuardTests), nameof(TextPrefixFactory)));
        Assert.That(factoryCalls, Is.GreaterThan(0));
        factoryCalls = 0;
        var guard = Create(TextTarget, typeof(TextCallbacks));
        Assert.That(guard.Candidates[0].Validate(), Does.Contain("factory"));
        Assert.That(factoryCalls, Is.Zero);
        Assert.That(guard.Available, Is.False);
    }

    public static MethodInfo TextPrefixFactory(MethodBase original)
    { factoryCalls++; return typeof(SettingsReadControlGuardTests).GetMethod(nameof(ForeignPrefix))!; }
    public static MethodInfo TextFinalizerFactory(MethodBase original)
    { factoryCalls++; return typeof(SettingsReadControlGuardTests).GetMethod(nameof(ForeignFinalizer))!; }

    [TestCase(HarmonyPatchType.Prefix)]
    [TestCase(HarmonyPatchType.Finalizer)]
    public void RemovedOwnedMemberInvalidatesThePair(HarmonyPatchType kind)
    {
        var guard = Create(TextTarget, typeof(TextCallbacks));
        Admit(guard);
        new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId).Unpatch(TextTarget, kind, OwnedHarmonyPatch.OwnerId);
        Assert.That(guard.Available, Is.False);
        if (kind == HarmonyPatchType.Finalizer)
        {
            using var scope = SettingsReadScope.Begin();
            Assert.Throws<InvalidOperationException>(() => Controls.Text("original"));
            Assert.That(scope.Failed, Is.True);
            Assert.That(Controls.TextCalls, Is.Zero);
        }
        else Assert.That(Controls.Text("original"), Is.EqualTo("ori"));
    }

    private SettingsReadControlGuard Create(MethodInfo target, Type callbacks, bool hasFinalizer = true)
    {
        var guard = new SettingsReadControlGuard(target, "TEST-READ-" + target.Name,
            callbacks.GetMethod("Prefix")!, hasFinalizer ? callbacks.GetMethod("Finalizer") : null);
        guards.Add(guard);
        return guard;
    }

    private static void Admit(SettingsReadControlGuard guard)
    {
        var warnings = new List<string>();
        var coordinator = new AdmissionCoordinator(warnings.Add);
        var group = new PatchGroup("read-controls", AdmissionPhase.Early, guard.Candidates);
        Assert.That(coordinator.Apply(group, AdmissionPhase.Early), Is.True, string.Join("; ", warnings));
        Assert.That(coordinator.Apply(group, AdmissionPhase.Early), Is.True);
        Assert.That(warnings, Is.Empty);
        guard.Enable();
        Assert.That(guard.Available, Is.True);
    }

    public static class Controls
    {
        public static bool ThrowNumbers;
        public static int TextCalls, GateCalls;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string Text(string? value)
        { TextCalls++; return value == null ? string.Empty : value.Substring(0, Math.Min(3, value.Length)); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Numeric<T>(ref T value, ref string? buffer)
        {
            value = default!;
            buffer = "native";
            if (ThrowNumbers) throw new InvalidOperationException("control-body");
        }
        [MethodImpl(MethodImplOptions.NoInlining)] public static void Gate() => GateCalls++;
        public static async Task Async() => await Task.Yield();
        public static IEnumerable<int> Iterator() { yield return 1; }
    }

    public abstract class AbstractControl { public abstract void Invoke(); }
    public class GenericControl<T> { public void Invoke() { } }
    public class BaseControl
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual string Text(string value) => value.Substring(0, 3);
    }
    public sealed class DerivedControl : BaseControl { }
    public sealed class OverrideControl : BaseControl
    {
        [MethodImpl(MethodImplOptions.NoInlining)] public override string Text(string value) => "override";
    }

    public static class TextCallbacks
    {
        public sealed record State(string? Text);
        public static void ReplaceState(ref State? __state) => __state = new State("foreign state");
        public static void ObserveWithoutState() { }
        [HarmonyPriority(int.MaxValue)]
        public static void Prefix(MethodBase __originalMethod, string? __0, out State? __state) =>
            __state = SettingsReadControlGuard.ShouldCapture(__originalMethod) ? new State(__0) : null;
        [HarmonyPriority(int.MinValue)]
        public static void Finalizer(ref string? __result, State? __state)
        {
            if (__state != null) __result = __state.Text;
        }
    }

    public static class BrokenTextCallbacks
    {
        public sealed record State(string? Text);
        [HarmonyPriority(int.MaxValue)]
        public static void Prefix(MethodBase __originalMethod, string? __0, out State? __state) =>
            __state = SettingsReadControlGuard.ShouldCapture(__originalMethod) ? new State(__0) : null;
        [HarmonyPriority(int.MinValue)]
        public static void Finalizer(ref string? __result, State? __state, int __999)
        { if (__state != null) __result = __state.Text; }
    }

    public static class GateCallbacks
    {
        [HarmonyPriority(int.MaxValue)]
        public static bool Prefix(MethodBase __originalMethod) => !SettingsReadControlGuard.ShouldCapture(__originalMethod);
    }

    public static class IntCallbacks
    {
        public static MethodBase? Seen;
        public sealed record State(int Value, string? Buffer);
        [HarmonyPriority(int.MaxValue)]
        public static void Prefix(MethodBase __originalMethod, int __0, string? __1, out State? __state)
        {
            Seen = __originalMethod;
            __state = SettingsReadControlGuard.ShouldCapture(__originalMethod) ? new State(__0, __1) : null;
        }
        [HarmonyPriority(int.MinValue)]
        public static void Finalizer(ref int __0, ref string? __1, State? __state)
        { if (__state != null) { __0 = __state.Value; __1 = __state.Buffer; } }
    }

    public static class FloatCallbacks
    {
        public static MethodBase? Seen;
        public sealed record State(float Value, string? Buffer);
        [HarmonyPriority(int.MaxValue)]
        public static void Prefix(MethodBase __originalMethod, float __0, string? __1, out State? __state)
        {
            Seen = __originalMethod;
            __state = SettingsReadControlGuard.ShouldCapture(__originalMethod) ? new State(__0, __1) : null;
        }
        [HarmonyPriority(int.MinValue)]
        public static void Finalizer(ref float __0, ref string? __1, State? __state)
        { if (__state != null) { __0 = __state.Value; __1 = __state.Buffer; } }
    }
}
