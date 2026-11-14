using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.DrugPolicyMaintenance.Core;
using Megamod.Features.DrugPolicyMaintenance.Harmony;
using Megamod.Features.DrugTolerance;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.DrugPolicyMaintenance;

[TestFixture, NonParallelizable]
public sealed class ScheduledDrugDecisionPatchTests
{
    public sealed class Drug { public bool NativeAllowed = true; }
    public sealed class Tracker
    {
        [MethodImpl(MethodImplOptions.NoInlining)] public bool Scheduled(Drug drug)
        { duringNative?.Invoke(); return drug.NativeAllowed; }
    }
    public sealed class Pawn { }
    public sealed class Joy
    {
        [MethodImpl(MethodImplOptions.NoInlining)] public bool Eligible(Pawn pawn, Drug drug) => drug.NativeAllowed;
    }
    private static Action? duringNative;
    private static readonly MethodInfo Target = typeof(Tracker).GetMethod(nameof(Tracker.Scheduled))!;

    [Test]
    public void RecoveringMoodWithholdsANativeEligibleDoseDespitePermittedTolerance()
    {
        int moodCalls = 0, toleranceCalls = 0;
        var patch = new ScheduledDrugDecisionPatch(Target,
            (_, _) => { moodCalls++; return ScheduledDrugReadResult.Rejected; },
            (_, _) => { toleranceCalls++; return ScheduledDrugReadResult.Allowed; }, () => true, _ => { });
        try
        {
            Apply(patch);
            Assert.That(new Tracker().Scheduled(new Drug()), Is.False);
            Assert.That(moodCalls, Is.EqualTo(1)); Assert.That(toleranceCalls, Is.EqualTo(1));
            Assert.That(patch.Available, Is.True);
        }
        finally { patch.Candidate.Rollback(); }
    }

    [TestCase(ScheduledDrugReadResult.Allowed, ScheduledDrugReadResult.Allowed, true)]
    [TestCase(ScheduledDrugReadResult.Allowed, ScheduledDrugReadResult.Rejected, false)]
    [TestCase(ScheduledDrugReadResult.Allowed, ScheduledDrugReadResult.Unavailable, true)]
    [TestCase(ScheduledDrugReadResult.Rejected, ScheduledDrugReadResult.Allowed, false)]
    [TestCase(ScheduledDrugReadResult.Rejected, ScheduledDrugReadResult.Rejected, false)]
    [TestCase(ScheduledDrugReadResult.Rejected, ScheduledDrugReadResult.Unavailable, false)]
    [TestCase(ScheduledDrugReadResult.Unavailable, ScheduledDrugReadResult.Allowed, true)]
    [TestCase(ScheduledDrugReadResult.Unavailable, ScheduledDrugReadResult.Rejected, false)]
    [TestCase(ScheduledDrugReadResult.Unavailable, ScheduledDrugReadResult.Unavailable, true)]
    public void IndependentLocalOutcomesContributeOnlyTheirOwnVeto(ScheduledDrugReadResult mood,
        ScheduledDrugReadResult tolerance, bool expected)
    {
        int moodCalls = 0, toleranceCalls = 0;
        var patch = new ScheduledDrugDecisionPatch(Target, (_, _) => { moodCalls++; return mood; },
            (_, _) => { toleranceCalls++; return tolerance; }, () => true, _ => { });
        try
        {
            Apply(patch);
            Assert.That(new Tracker().Scheduled(new Drug()), Is.EqualTo(expected));
            Assert.That((moodCalls, toleranceCalls), Is.EqualTo((1, 1)));
        }
        finally { patch.Candidate.Rollback(); }
    }

    [TestCase(true, false)]
    [TestCase(false, false)]
    [TestCase(true, true)]
    [TestCase(false, true)]
    public void ActualReadExceptionAndNoticeFailureCannotDisableOtherRestriction(bool failMood, bool failNotice)
    {
        var error = new InvalidOperationException("native local reader");
        var failures = new List<Exception>();
        var reports = new List<string>();
        bool throwing = true;
        int moodCalls = 0, toleranceCalls = 0;
        ScheduledDrugReadResult Read(bool failing)
        {
            return ScheduledDrugReadBoundary.Evaluate(() =>
            {
                if (failing && throwing) throw error;
                return ScheduledDrugReadResult.Rejected;
            }, found => { failures.Add(found); if (failNotice) throw new ApplicationException("notice"); });
        }
        var patch = new ScheduledDrugDecisionPatch(Target, (_, _) => { moodCalls++; return Read(failMood); },
            (_, _) => { toleranceCalls++; return Read(!failMood); }, () => true, reports.Add);
        try
        {
            Apply(patch);
            Assert.That(new Tracker().Scheduled(new Drug()), Is.False);
            Assert.That(failures, Is.EqualTo(new[] { error }));
            Assert.That((moodCalls, toleranceCalls), Is.EqualTo((1, 1)));
            Assert.That(patch.Available, Is.True); Assert.That(patch.Failure, Is.Null);
            Assert.That(reports, Is.Empty);
            throwing = false;
            Assert.That(new Tracker().Scheduled(new Drug()), Is.False);
            Assert.That((moodCalls, toleranceCalls), Is.EqualTo((2, 2)));
            Assert.That(failures, Has.Count.EqualTo(1));
        }
        finally { patch.Candidate.Rollback(); }
    }

    [Test]
    public void BothLocalFailuresPreserveNativePermissionAndSupportedNextReadRecovers()
    {
        bool fail = true;
        var reports = new List<string>();
        ScheduledDrugReadResult Read() => ScheduledDrugReadBoundary.Evaluate(
            () => fail ? throw new InvalidOperationException("read") : ScheduledDrugReadResult.Rejected, _ => { });
        var patch = new ScheduledDrugDecisionPatch(Target, (_, _) => Read(), (_, _) => Read(), () => true, reports.Add);
        try
        {
            Apply(patch);
            Assert.That(new Tracker().Scheduled(new Drug()), Is.True);
            fail = false;
            Assert.That(new Tracker().Scheduled(new Drug()), Is.False);
            Assert.That(patch.Available, Is.True); Assert.That(reports, Is.Empty);
        }
        finally { patch.Candidate.Rollback(); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NativeAndForeignRejectionsNeverInvokeEitherRead(bool foreignRejects)
    {
        var foreign = new HarmonyLib.Harmony("tests.scheduled-composition.reject");
        int calls = 0;
        var patch = new ScheduledDrugDecisionPatch(Target, (_, _) => { calls++; return ScheduledDrugReadResult.Allowed; },
            (_, _) => { calls++; return ScheduledDrugReadResult.Allowed; }, () => true, _ => { });
        try
        {
            if (foreignRejects) foreign.Patch(Target, postfix: Method(nameof(Reject)));
            Apply(patch);
            Assert.That(new Tracker().Scheduled(new Drug { NativeAllowed = foreignRejects }), Is.False);
            Assert.That(calls, Is.Zero);
        }
        finally { try { patch.Candidate.Rollback(); } finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); } }
    }

    [Test]
    public void FailedToleranceInstallationAndItsRollbackLeaveSharedMoodAndSeparateJoyOwnershipIntact()
    {
        var patch = new ScheduledDrugDecisionPatch(Target, (_, _) => ScheduledDrugReadResult.Rejected,
            (_, _) => ScheduledDrugReadResult.Unavailable, () => true, _ => { });
        var joyTarget = typeof(Joy).GetMethod(nameof(Joy.Eligible))!;
        var joy = new DrugToleranceDecisionPatch("DT-test-joy", joyTarget, false, (_, _) => false, () => true, _ => { });
        try
        {
            Apply(patch);
            var failing = new PatchCandidate("failure", "test", "test", "test", "test", () => null,
                () => throw new InvalidOperationException("tolerance editor install"), () => { });
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(new("test.tolerance", AdmissionPhase.Early,
                new[] { patch.S138Dependency, joy.Candidate, failing }), AdmissionPhase.Early), Is.False);
            Assert.That(new Tracker().Scheduled(new Drug()), Is.False);
            Assert.That(new Joy().Eligible(new Pawn(), new Drug()), Is.True);
            Assert.That(patch.Available, Is.True);
            patch.S138Dependency.Rollback();
            Assert.That(new Tracker().Scheduled(new Drug()), Is.False);
        }
        finally { try { joy.Candidate.Rollback(); } finally { patch.Candidate.Rollback(); } }
    }

    [Test]
    public void CurrentContextAndConsumerChangesNeedNoNewOwnedPatch()
    {
        bool context = false, moodOn = false, toleranceOn = false;
        var patch = new ScheduledDrugDecisionPatch(Target, (_, _) => moodOn ? ScheduledDrugReadResult.Rejected : ScheduledDrugReadResult.Allowed,
            (_, _) => toleranceOn ? ScheduledDrugReadResult.Rejected : ScheduledDrugReadResult.Allowed, () => context, _ => { });
        try
        {
            Apply(patch);
            var before = HarmonyLib.Harmony.GetPatchInfo(Target)!;
            Assert.That(new Tracker().Scheduled(new Drug()), Is.True);
            context = true;
            Assert.That(new Tracker().Scheduled(new Drug()), Is.True);
            moodOn = true;
            Assert.That(new Tracker().Scheduled(new Drug()), Is.False);
            moodOn = false; toleranceOn = true;
            Assert.That(new Tracker().Scheduled(new Drug()), Is.False);
            toleranceOn = false;
            Assert.That(new Tracker().Scheduled(new Drug()), Is.True);
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(Target)!.Prefixes.Select(p => p.index), Is.EqualTo(before.Prefixes.Select(p => p.index)));
            Assert.That(patch.S138Dependency.Validate(), Is.Null);
        }
        finally { patch.Candidate.Rollback(); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SkippedOriginalInvalidatesSharedDependencyWhileNativeExceptionIsPreserved(bool throwNative)
    {
        var foreign = new HarmonyLib.Harmony("tests.scheduled-composition.skip");
        var error = new ArgumentException("native");
        int calls = 0;
        var patch = new ScheduledDrugDecisionPatch(Target, (_, _) => { calls++; return ScheduledDrugReadResult.Rejected; },
            (_, _) => { calls++; return ScheduledDrugReadResult.Rejected; }, () => true, _ => { });
        try
        {
            if (!throwNative) foreign.Patch(Target, prefix: Method(nameof(Skip)));
            Apply(patch);
            if (throwNative)
            {
                duringNative = () => throw error;
                Assert.That(Assert.Throws<ArgumentException>(() => new Tracker().Scheduled(new Drug())), Is.SameAs(error));
                Assert.That(patch.Available, Is.True);
            }
            else
            {
                Assert.That(new Tracker().Scheduled(new Drug()), Is.True);
                Assert.That(patch.Available, Is.False);
                Assert.That(patch.S138Dependency.Validate(), Is.Not.Null);
            }
            Assert.That(calls, Is.Zero);
        }
        finally { duringNative = null; try { patch.Candidate.Rollback(); } finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); } }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RepeatedSkippedOriginalReportsOneContextualWarningAndRecovers(bool reportThrows)
    {
        var foreign = new HarmonyLib.Harmony("tests.scheduled-composition.repeated-skip");
        var reports = new List<string>();
        int calls = 0;
        var patch = new ScheduledDrugDecisionPatch(Target,
            (_, _) => { calls++; return ScheduledDrugReadResult.Rejected; },
            (_, _) => { calls++; return ScheduledDrugReadResult.Allowed; }, () => true,
            message => { reports.Add(message); if (reportThrows) throw new InvalidOperationException("diagnostic sink"); });
        try
        {
            foreign.Patch(Target, prefix: Method(nameof(Skip)));
            Apply(patch);
            for (int i = 0; i < 8; i++) Assert.That(new Tracker().Scheduled(new Drug()), Is.True);
            Assert.That(calls, Is.Zero);
            Assert.That(patch.Available, Is.False);
            Assert.That(patch.S138Dependency.Validate(), Is.Not.Null);
            Assert.That(reports, Has.Count.EqualTo(1));
            Assert.That(reports[0], Does.Contain("[fumblesneeze.megamod]").And.Contain("patch=DRUG-SCHEDULED;")
                .And.Contain("target=" + Target.DeclaringType!.FullName + "." + Target.Name)
                .And.Contain("targetSpec=DRUG-SCHEDULED-COMPOSITION;")
                .And.Contain("reason=The native selection body was skipped.")
                .And.Contain("unavailable=scheduled mood restrictions and atomic tolerance editing/enforcement;")
                .And.Contain("fallback=native scheduled permission; retained feature and policy choices"));

            foreign.Unpatch(Target, HarmonyPatchType.Prefix, foreign.Id);
            Assert.That(new Tracker().Scheduled(new Drug()), Is.False);
            Assert.That(calls, Is.EqualTo(2));
            Assert.That(patch.Available, Is.True);
            Assert.That(patch.Failure, Is.Null);
            Assert.That(patch.S138Dependency.Validate(), Is.Null);

            foreign.Patch(Target, prefix: Method(nameof(Skip)));
            for (int i = 0; i < 4; i++)
            {
                Assert.That(new Tracker().Scheduled(new Drug()), Is.True);
                Assert.That(patch.Available, Is.False);
                Assert.That(patch.S138Dependency.Validate(), Is.Not.Null);
                Assert.That(patch.Failure, Is.EqualTo("The native selection body was skipped."));
            }
            Assert.That(calls, Is.EqualTo(2));
            Assert.That(reports, Has.Count.EqualTo(1));
        }
        finally { try { patch.Candidate.Rollback(); } finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); } }
    }

    [Test]
    public void CompatiblePublicationRemainsUsableButRemovedOwnedObserverRevokesDependency()
    {
        var foreign = new HarmonyLib.Harmony("tests.scheduled-composition.publication");
        var patch = new ScheduledDrugDecisionPatch(Target, (_, _) => ScheduledDrugReadResult.Rejected,
            (_, _) => ScheduledDrugReadResult.Allowed, () => true, _ => { });
        try
        {
            Apply(patch);
            foreign.Patch(Target, prefix: Method(nameof(Observe)));
            Assert.That(new Tracker().Scheduled(new Drug()), Is.False);
            Assert.That(patch.S138Dependency.Validate(), Is.Null);
            new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId).Unpatch(Target, HarmonyPatchType.Transpiler, OwnedHarmonyPatch.OwnerId);
            Assert.That(patch.Available, Is.False);
            Assert.That(patch.S138Dependency.Validate(), Is.Not.Null);
            Assert.That(new Tracker().Scheduled(new Drug()), Is.True);
        }
        finally { try { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); } finally { patch.Candidate.Rollback(); } }
    }

    [TestCase(ScheduledDrugReadResult.Allowed)]
    [TestCase(ScheduledDrugReadResult.Rejected)]
    public void DepartedIdentityDuringMoodReadCannotPublishEitherOldContextVeto(ScheduledDrugReadResult mood)
    {
        object current = new();
        int toleranceCalls = 0;
        var tracker = new Tracker(); var drug = new Drug();
        var patch = new ScheduledDrugDecisionPatch(Target, (_, _) => { current = new(); return mood; },
            (_, _) => { toleranceCalls++; return ScheduledDrugReadResult.Rejected; }, () => true, _ => { },
            (actor, candidate, evaluate) =>
            {
                Assert.That(actor, Is.SameAs(tracker)); Assert.That(candidate, Is.SameAs(drug));
                var captured = current;
                bool permitted = evaluate();
                return ReferenceEquals(current, captured) ? permitted : true;
            });
        try
        {
            Apply(patch);
            Assert.That(tracker.Scheduled(drug), Is.True);
            Assert.That(toleranceCalls, Is.EqualTo(1));
            Assert.That(patch.Available, Is.True); Assert.That(patch.Failure, Is.Null);
        }
        finally { patch.Candidate.Rollback(); }
    }

    [TestCase(true)]
    [TestCase(false)]
    public void CurrentSynchronousIdentityBoundaryRetainsIndependentDecisions(bool rejectMood)
    {
        int calls = 0;
        var patch = new ScheduledDrugDecisionPatch(Target,
            (_, _) => { calls++; return rejectMood ? ScheduledDrugReadResult.Rejected : ScheduledDrugReadResult.Allowed; },
            (_, _) => { calls++; return rejectMood ? ScheduledDrugReadResult.Allowed : ScheduledDrugReadResult.Rejected; },
            () => true, _ => { }, (_, _, evaluate) => evaluate());
        try
        {
            Apply(patch);
            Assert.That(new Tracker().Scheduled(new Drug()), Is.False);
            Assert.That(calls, Is.EqualTo(2));
        }
        finally { patch.Candidate.Rollback(); }
    }

    public static void Reject(ref bool __result) => __result = false;
    public static bool Skip(ref bool __result) { __result = true; return false; }
    public static void Observe() { }
    private static HarmonyMethod Method(string name) => new(typeof(ScheduledDrugDecisionPatchTests), name);

    private static void Apply(ScheduledDrugDecisionPatch patch) =>
        Assert.That(new AdmissionCoordinator(_ => { }).Apply(new("test.shared-scheduled", AdmissionPhase.Early,
            new[] { patch.Candidate }), AdmissionPhase.Early), Is.True, patch.Failure);
}
