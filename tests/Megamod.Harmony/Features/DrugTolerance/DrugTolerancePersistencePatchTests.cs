using System.Runtime.CompilerServices;
using System.Reflection;
using HarmonyLib;
using Megamod.Features.DrugTolerance;
using Megamod.Features.DrugTolerance.Core;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.DrugTolerance;

[TestFixture, NonParallelizable]
public sealed class DrugTolerancePersistencePatchTests
{
    private static Entry? replacement;
    private static Action? duringNative;
    private static Action? duringFinalization;
    private static readonly MethodInfo CopyTarget = typeof(Entry).GetMethod(nameof(Entry.CopyFrom))!;
    private static readonly MethodInfo ExposeTarget = typeof(Entry).GetMethod(nameof(Entry.ExposeData))!;
    public sealed class Saver
    {
        public bool Finalized;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void FinalizeSaving() { duringFinalization?.Invoke(); Finalized = true; }
    }
    public sealed class Loader
    {
        public bool Finalized;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void FinalizeLoading() { duringFinalization?.Invoke(); Finalized = true; }
    }
    public sealed class Entry
    {
        public string Native = "initial";
        public DrugToleranceChoice Choice = DrugToleranceChoice.Any;
        public int Exposed;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CopyFrom(Entry other) { duringNative?.Invoke(); Native = other.Native; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ExposeData() => Exposed++;
    }

    [Test]
    public void SuccessfulNativeCopyAndExposePreserveOwnedChoiceIndependently()
    {
        var reports = new List<string>();
        var exposed = new List<Entry>();
        var patch = Create(reports, exposed);
        try
        {
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(patch.Group, AdmissionPhase.Early), Is.True);
            patch.Enable();
            var source = new Entry { Native = "source", Choice = DrugToleranceChoice.Numeric(0f) };
            var copy = new Entry();
            copy.CopyFrom(source);
            Assert.That(copy.Native, Is.EqualTo("source"));
            Assert.That(copy.Choice.Kind, Is.EqualTo(DrugToleranceChoiceKind.Limit));
            Assert.That(copy.Choice.Limit, Is.Zero);
            copy.Choice = DrugToleranceChoice.Numeric(1f);
            Assert.That(source.Choice.Limit, Is.Zero);
            copy.ExposeData();
            Assert.That(copy.Exposed, Is.EqualTo(1));
            Assert.That(exposed, Is.EqualTo(new[] { copy }));
            Assert.That(reports, Is.Empty);
        }
        finally { foreach (var candidate in patch.Group.Patches.Reverse()) candidate.Rollback(); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NativeSkipOrThrowCannotCopyOwnedState(bool throwNative)
    {
        var reports = new List<string>();
        var patch = Create(reports, new());
        var foreign = new HarmonyLib.Harmony("tests.tolerance.native-skip");
        var failure = new InvalidOperationException("native failure");
        try
        {
            if (!throwNative) foreign.Patch(CopyTarget, prefix: new HarmonyMethod(typeof(DrugTolerancePersistencePatchTests), nameof(Skip)));
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(patch.Group, AdmissionPhase.Early), Is.True);
            patch.Enable();
            if (throwNative) duringNative = () => throw failure;
            var target = new Entry { Choice = DrugToleranceChoice.Numeric(0.75f) };
            var source = new Entry { Choice = DrugToleranceChoice.Numeric(0f) };
            if (throwNative) Assert.That(Assert.Throws<InvalidOperationException>(() => target.CopyFrom(source)), Is.SameAs(failure));
            else target.CopyFrom(source);
            Assert.That(target.Native, Is.EqualTo("initial"));
            Assert.That(target.Choice.Limit, Is.EqualTo(0.75f));
            Assert.That(reports, Is.Empty);
        }
        finally
        {
            duringNative = null;
            try { Cleanup(patch); } finally { foreign.Unpatch(CopyTarget, HarmonyPatchType.All, foreign.Id); }
        }
    }

    [Test]
    public void CapturedFinalPrefixSourceSurvivesLaterPostfixArgumentChange()
    {
        var reports = new List<string>();
        var patch = Create(reports, new());
        var foreign = new HarmonyLib.Harmony("tests.tolerance.arguments");
        try
        {
            replacement = new Entry { Native = "replacement", Choice = DrugToleranceChoice.Numeric(0.3f) };
            foreign.Patch(CopyTarget,
                prefix: new HarmonyMethod(typeof(DrugTolerancePersistencePatchTests), nameof(ReplaceSource)),
                postfix: new HarmonyMethod(typeof(DrugTolerancePersistencePatchTests), nameof(ChangeSourceAfterBody)));
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(patch.Group, AdmissionPhase.Early), Is.True);
            patch.Enable();
            var destination = new Entry();
            destination.CopyFrom(new Entry { Choice = DrugToleranceChoice.Numeric(0.9f) });
            Assert.That(destination.Native, Is.EqualTo("replacement"));
            Assert.That(destination.Choice.Limit, Is.EqualTo(0.3f));
            Assert.That(reports, Is.Empty);
        }
        finally
        {
            replacement = null;
            try { Cleanup(patch); } finally { foreign.Unpatch(CopyTarget, HarmonyPatchType.All, foreign.Id); }
        }
    }

    [Test]
    public void UnsupportedLaterScribeOrderDisablesCopyBeforeScribeIsCalled()
    {
        var reports = new List<string>();
        var patch = Create(reports, new());
        var foreign = new HarmonyLib.Harmony("tests.tolerance.later-order");
        try
        {
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(patch.Group, AdmissionPhase.Early), Is.True);
            patch.Enable();
            foreign.Patch(ExposeTarget, prefix: new HarmonyMethod(typeof(DrugTolerancePersistencePatchTests), nameof(Observe))
                { after = new[] { OwnedHarmonyPatch.OwnerId } });
            var destination = new Entry();
            destination.CopyFrom(new Entry { Native = "native still copied", Choice = DrugToleranceChoice.Numeric(0f) });
            Assert.That(destination.Native, Is.EqualTo("native still copied"));
            Assert.That(destination.Choice.Kind, Is.EqualTo(DrugToleranceChoiceKind.Any));
            Assert.That(reports, Has.Count.EqualTo(1));
            Assert.That(reports[0], Does.Contain("DT-POLICY-SCRIBE"));
            destination.CopyFrom(new Entry());
            Assert.That(reports, Has.Count.EqualTo(1));
        }
        finally { try { foreign.Unpatch(ExposeTarget, HarmonyPatchType.All, foreign.Id); } finally { Cleanup(patch); } }
    }

    [Test]
    public void ExplicitAvailabilityRefusalRevokesCachedReceiptAfterObserverRemoval()
    {
        var reports = new List<string>();
        var patch = Create(reports, new());
        try
        {
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(patch.Group, AdmissionPhase.Early), Is.True);
            patch.Enable();
            new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId).Unpatch(CopyTarget, HarmonyPatchType.Transpiler, OwnedHarmonyPatch.OwnerId);
            Assert.That(patch.Available, Is.False);
            var destination = new Entry();
            destination.CopyFrom(new Entry { Choice = DrugToleranceChoice.Numeric(0f) });
            Assert.That(destination.Choice.Kind, Is.EqualTo(DrugToleranceChoiceKind.Any));
            Assert.That(reports, Has.Count.EqualTo(1));
        }
        finally { Cleanup(patch); }
    }

    [Test]
    public void ChoiceUseDetectsRemovedGenerationObserverWithoutAnExplicitAvailabilityRefresh()
    {
        var reports = new List<string>();
        var patch = Create(reports, new());
        try
        {
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(patch.Group, AdmissionPhase.Early), Is.True);
            patch.Enable();
            Assert.That(patch.CanUseChoices, Is.True);
            new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId).Unpatch(CopyTarget, HarmonyPatchType.Transpiler, OwnedHarmonyPatch.OwnerId);
            Assert.That(patch.CanUseChoices, Is.False);
            Assert.That(reports, Has.Count.EqualTo(1));
            Assert.Throws<InvalidOperationException>(() => new Saver().FinalizeSaving());
        }
        finally { Cleanup(patch); }
    }

    [Test]
    public void BusyPublicationWithholdsChoiceUseWithoutPermanentlyFaultingHealthyPersistence()
    {
        var reports = new List<string>();
        var patch = Create(reports, new());
        var gate = typeof(PatchProcessor).GetField("locker", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        try
        {
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(patch.Group, AdmissionPhase.Early), Is.True);
            patch.Enable();
            lock (gate) Assert.That(patch.CanUseChoices, Is.False);
            Assert.That(patch.CanUseChoices, Is.True);
            Assert.DoesNotThrow(() => new Saver().FinalizeSaving());
            Assert.That(reports, Is.Empty);
        }
        finally { Cleanup(patch); }
    }

    [Test]
    public void DiagnosticInstructionReadCannotAdmitAnAbsentCompiledGeneration()
    {
        var reports = new List<string>();
        var patch = Create(reports, new());
        try
        {
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(patch.Group, AdmissionPhase.Early), Is.True);
            patch.Enable();
            Assert.That(patch.CanUseChoices, Is.True);
            Assert.That(PatchProcessor.GetCurrentInstructions(CopyTarget), Is.Not.Empty);
            Assert.That(patch.CanUseChoices, Is.False,
                "A diagnostic invocation of transpilers is not a composed publication receipt.");
            var destination = new Entry();
            destination.CopyFrom(new Entry { Choice = DrugToleranceChoice.Numeric(0f) });
            Assert.That(destination.Choice.Kind, Is.EqualTo(DrugToleranceChoiceKind.Any));
            Assert.That(reports, Has.Count.EqualTo(1));
            Assert.Throws<InvalidOperationException>(() => new Saver().FinalizeSaving());
        }
        finally { Cleanup(patch); }
    }

    [Test]
    public void UnreadableChangedPublicationRevokesChoiceAndSerializationReceipts()
    {
        var reports = new List<string>();
        var exposed = new List<Entry>();
        var patch = Create(reports, exposed);
        var foreign = new HarmonyLib.Harmony(new string('x', 4 * 1024 * 1024));
        try
        {
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(patch.Group, AdmissionPhase.Early), Is.True);
            patch.Enable();
            new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId).Unpatch(ExposeTarget, HarmonyPatchType.Transpiler, OwnedHarmonyPatch.OwnerId);
            foreign.Patch(ExposeTarget, prefix: new HarmonyMethod(typeof(DrugTolerancePersistencePatchTests), nameof(Observe)));
            Assert.That(patch.CanUseChoices, Is.False);
            var entry = new Entry();
            entry.ExposeData();
            Assert.That(entry.Exposed, Is.EqualTo(1), "Native row behavior remains intact.");
            Assert.That(exposed, Is.Empty, "The stale receipt cannot authorize owned serialization.");
            Assert.That(reports, Has.Count.EqualTo(1));
            Assert.That(reports[0], Does.Contain("4 MiB"));
            Assert.Throws<InvalidOperationException>(() => new Saver().FinalizeSaving());
        }
        finally { try { foreign.Unpatch(ExposeTarget, HarmonyPatchType.All, foreign.Id); } finally { Cleanup(patch); } }
    }

    [Test]
    public void ChoiceUseReadmitsCompatibleRepatch()
    {
        var reports = new List<string>();
        var patch = Create(reports, new());
        var foreign = new HarmonyLib.Harmony("tests.tolerance.choice-repatch");
        try
        {
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(patch.Group, AdmissionPhase.Early), Is.True);
            patch.Enable();
            Assert.That(patch.CanUseChoices, Is.True);
            foreign.Patch(CopyTarget, prefix: new HarmonyMethod(typeof(DrugTolerancePersistencePatchTests), nameof(Observe)));
            Assert.That(patch.CanUseChoices, Is.True);
            Assert.That(reports, Is.Empty);
        }
        finally { try { foreign.Unpatch(CopyTarget, HarmonyPatchType.All, foreign.Id); } finally { Cleanup(patch); } }
    }

    [Test]
    public void FailedSecondTargetInstallationRollsBackOnlyOwnedMembersAndReportsBothTargets()
    {
        var reports = new List<string>();
        var patch = Create(reports, new());
        var foreign = new HarmonyLib.Harmony("tests.tolerance.startup-order");
        try
        {
            foreign.Patch(ExposeTarget, prefix: new HarmonyMethod(typeof(DrugTolerancePersistencePatchTests), nameof(Observe))
                { after = new[] { OwnedHarmonyPatch.OwnerId } });
            var coordinator = new AdmissionCoordinator(reports.Add);
            Assert.That(coordinator.Apply(patch.Group, AdmissionPhase.Early), Is.False);
            Assert.That(coordinator.Apply(patch.Group, AdmissionPhase.Early), Is.False);
            Assert.That(reports, Has.Count.EqualTo(4));
            Assert.That(reports[2], Does.Contain("DT-POLICY-COPY").And.Contain("targetSpec=DT-POLICY-PERSISTENCE"));
            Assert.That(reports[3], Does.Contain("DT-POLICY-SCRIBE"));
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(CopyTarget)?.Owners.ToArray() ?? Array.Empty<string>(),
                Does.Not.Contain(OwnedHarmonyPatch.OwnerId));
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(ExposeTarget)!.Owners, Is.EqualTo(new[] { foreign.Id }));
        }
        finally { try { foreign.Unpatch(ExposeTarget, HarmonyPatchType.All, foreign.Id); } finally { Cleanup(patch); } }
    }

    [Test]
    public void GenerationChangeDuringNativeCopyDoesNotCommitACapturedChoice()
    {
        var reports = new List<string>();
        var patch = Create(reports, new());
        var foreign = new HarmonyLib.Harmony("tests.tolerance.mid-call");
        try
        {
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(patch.Group, AdmissionPhase.Early), Is.True);
            patch.Enable();
            duringNative = () => foreign.Patch(CopyTarget, prefix: new HarmonyMethod(typeof(DrugTolerancePersistencePatchTests), nameof(Observe)));
            var destination = new Entry();
            destination.CopyFrom(new Entry { Native = "copied", Choice = DrugToleranceChoice.Numeric(0f) });
            Assert.That(destination.Native, Is.EqualTo("copied"));
            Assert.That(destination.Choice.Kind, Is.EqualTo(DrugToleranceChoiceKind.Any));
            Assert.That(reports, Has.Count.EqualTo(1));
        }
        finally
        {
            duringNative = null;
            try { foreign.Unpatch(CopyTarget, HarmonyPatchType.All, foreign.Id); } finally { Cleanup(patch); }
        }
    }

    [Test]
    public void CompatibleRepatchBetweenCallsRevalidatesWithoutLosingChoices()
    {
        var reports = new List<string>();
        var patch = Create(reports, new());
        var foreign = new HarmonyLib.Harmony("tests.tolerance.compatible-repatch");
        try
        {
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(patch.Group, AdmissionPhase.Early), Is.True);
            patch.Enable();
            foreign.Patch(CopyTarget, prefix: new HarmonyMethod(typeof(DrugTolerancePersistencePatchTests), nameof(Observe)));
            var destination = new Entry();
            destination.CopyFrom(new Entry { Choice = DrugToleranceChoice.Numeric(0.4f) });
            Assert.That(destination.Choice.Limit, Is.EqualTo(0.4f));
            Assert.That(patch.Available, Is.True);
            Assert.That(reports, Is.Empty);
        }
        finally { try { foreign.Unpatch(CopyTarget, HarmonyPatchType.All, foreign.Id); } finally { Cleanup(patch); } }
    }

    [Test]
    public void CaptureFailureIsBoundedAndCannotReplaceTheNativeOutcome()
    {
        int reports = 0;
        var patch = new DrugTolerancePersistencePatch(typeof(Entry), typeof(Saver), typeof(Loader), _ => throw new InvalidOperationException(new string('x', 600)),
            (_, _) => throw new InvalidOperationException("must not write"), _ => throw new InvalidOperationException("must not expose"),
            message => { reports++; Assert.That(message.Length, Is.LessThan(800)); throw new Exception("sink failed"); });
        try
        {
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(patch.Group, AdmissionPhase.Early), Is.True);
            patch.Enable();
            var destination = new Entry();
            Assert.DoesNotThrow(() => destination.CopyFrom(new Entry { Native = "first" }));
            Assert.That(destination.Native, Is.EqualTo("first"));
            Assert.DoesNotThrow(() => destination.CopyFrom(new Entry { Native = "second" }));
            Assert.That(destination.Native, Is.EqualTo("second"));
            Assert.That(destination.Choice.Kind, Is.EqualTo(DrugToleranceChoiceKind.Any));
            Assert.That(reports, Is.EqualTo(1));
            Assert.That(patch.Available, Is.False);
        }
        finally { Cleanup(patch); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SwallowedRowFailureStillAbortsNativeFinalizationAndSubsequentAttempts(bool loading)
    {
        var failure = new InvalidOperationException("owned scribe failure");
        var reports = new List<string>();
        var patch = new DrugTolerancePersistencePatch(typeof(Entry), typeof(Saver), typeof(Loader), entry => ((Entry)entry).Choice,
            (entry, choice) => ((Entry)entry).Choice = choice, _ => throw failure, reports.Add);
        try
        {
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(patch.Group, AdmissionPhase.Early), Is.True);
            patch.Enable();
            // Native Scribe_Deep catches ordinary per-row errors; this fixture explicitly models
            // that catch, then invokes the independently patched native finalization boundary.
            try { new Entry().ExposeData(); } catch (Exception) { }
            var saver = new Saver();
            var loader = new Loader();
            TestDelegate finalize = loading ? loader.FinalizeLoading : saver.FinalizeSaving;
            var rejected = Assert.Throws<InvalidOperationException>(finalize);
            Assert.That(rejected!.Message, Does.Contain("aborted").And.Contain("restart"));
            Assert.Throws<InvalidOperationException>(finalize);
            Assert.That(saver.Finalized || loader.Finalized, Is.False);
            Assert.That(reports, Has.Count.EqualTo(1));
        }
        finally { Cleanup(patch); }
    }

    [Test]
    public void HealthyFinalizationKeepsNativeSaveAndLoadBehavior()
    {
        var reports = new List<string>();
        var patch = Create(reports, new());
        try
        {
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(patch.Group, AdmissionPhase.Early), Is.True);
            patch.Enable();
            var saver = new Saver();
            var loader = new Loader();
            saver.FinalizeSaving();
            loader.FinalizeLoading();
            Assert.That(saver.Finalized && loader.Finalized, Is.True);
            Assert.That(reports, Is.Empty);
        }
        finally { Cleanup(patch); }
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void FailureFirstCaughtInsideFinalizationStillRejectsCompletionAndPreservesNativeException(bool loading, bool nativeThrows)
    {
        var reports = new List<string>();
        var ownedFailure = new InvalidOperationException("late owned phase failure");
        var nativeFailure = new ArgumentException("native finalization failure");
        var patch = new DrugTolerancePersistencePatch(typeof(Entry), typeof(Saver), typeof(Loader), entry => ((Entry)entry).Choice,
            (entry, choice) => ((Entry)entry).Choice = choice, _ => throw ownedFailure, reports.Add);
        try
        {
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(patch.Group, AdmissionPhase.Early), Is.True);
            patch.Enable();
            duringFinalization = () =>
            {
                // ResolveAllCrossReferences and DoAllPostLoadInits both catch row errors.
                try { new Entry().ExposeData(); } catch (Exception) { }
                if (nativeThrows) throw nativeFailure;
            };
            TestDelegate finalize = loading ? new Loader().FinalizeLoading : new Saver().FinalizeSaving;
            if (nativeThrows) Assert.That(Assert.Throws<ArgumentException>(finalize), Is.SameAs(nativeFailure));
            else Assert.That(Assert.Throws<InvalidOperationException>(finalize)!.Message, Does.Contain("aborted"));
            Assert.That(patch.Available, Is.False);
            Assert.That(reports, Has.Count.EqualTo(1));
        }
        finally { duringFinalization = null; Cleanup(patch); }
    }

    [Test]
    public void ExceptionChangingFinalizeHandlerRefusesTheWholeSupportGroup()
    {
        var reports = new List<string>();
        var patch = Create(reports, new());
        var target = typeof(Saver).GetMethod(nameof(Saver.FinalizeSaving))!;
        var foreign = new HarmonyLib.Harmony("tests.tolerance.finalize-suppression");
        try
        {
            foreign.Patch(target, finalizer: new HarmonyMethod(typeof(DrugTolerancePersistencePatchTests), nameof(Suppress)));
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(patch.Group, AdmissionPhase.Early), Is.False);
            Assert.That(reports, Has.Count.EqualTo(4));
            Assert.That(reports[0], Does.Contain("abort exception"));
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(target)!.Owners, Is.EqualTo(new[] { foreign.Id }));
        }
        finally { try { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); } finally { Cleanup(patch); } }
    }

    public static bool Skip() => false;
    public static Exception? Suppress(Exception? __exception) => null;
    public static void Observe() { }
    public static void ReplaceSource(ref Entry __0) => __0 = replacement!;
    public static void ChangeSourceAfterBody(ref Entry __0) => __0 = new Entry { Choice = DrugToleranceChoice.Numeric(1f) };

    private static void Cleanup(DrugTolerancePersistencePatch patch)
    {
        foreach (var candidate in patch.Group.Patches.Reverse()) candidate.Rollback();
    }

    private static DrugTolerancePersistencePatch Create(List<string> reports, List<Entry> exposed) =>
        new(typeof(Entry), typeof(Saver), typeof(Loader), entry => ((Entry)entry).Choice,
            (entry, choice) => ((Entry)entry).Choice = choice, entry => exposed.Add((Entry)entry), reports.Add);
}
