using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.DrugTolerance;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.DrugTolerance;

[TestFixture, NonParallelizable]
public sealed class DrugToleranceDecisionPatchTests
{
    private static Action? duringNative;
    private static Drug? replacement;
    private static readonly MethodInfo Scheduled = typeof(Tracker).GetMethod(nameof(Tracker.Scheduled))!;
    public sealed class Drug { public bool Allowed = true; }
    public sealed class Pawn { public bool Below = true; }
    public sealed class Tracker
    {
        public Pawn Pawn = new();
        [MethodImpl(MethodImplOptions.NoInlining)] public bool Scheduled(Drug drug) { duringNative?.Invoke(); return drug.Allowed; }
    }
    public sealed class Joy
    {
        [MethodImpl(MethodImplOptions.NoInlining)] public bool Eligible(Pawn pawn, Drug drug) => drug.Allowed;
    }

    [TestCase(true)]
    [TestCase(false)]
    public void RealSelectionCallKeepsNativeRejectionAndAddsOnlyTheCurrentOwnedRule(bool scheduled)
    {
        var tracker = new Tracker();
        var drug = new Drug();
        var joy = new Joy();
        var reports = new List<string>();
        var method = scheduled ? typeof(Tracker).GetMethod(nameof(Tracker.Scheduled))! : typeof(Joy).GetMethod(nameof(Joy.Eligible))!;
        DrugToleranceDecisionPatch? patch = null;
        patch = new("DT-test", method, scheduled,
            (actor, _) => scheduled ? ((Tracker)actor).Pawn.Below : ((Pawn)actor).Below,
            () => patch!.Available, reports.Add);
        try
        {
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(new("test-decisions", AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early), Is.True);
            bool Select() => scheduled ? tracker.Scheduled(drug) : joy.Eligible(tracker.Pawn, drug);
            Assert.That(Select(), Is.True);
            tracker.Pawn.Below = false;
            Assert.That(Select(), Is.False);
            tracker.Pawn.Below = true; drug.Allowed = false;
            Assert.That(Select(), Is.False);
            Assert.That(reports, Is.Empty);
        }
        finally { patch.Candidate.Rollback(); }
    }

    [Test]
    public void FinalPrefixInputIsRetainedDespiteALaterPostfixArgumentEdit()
    {
        var reports = new List<string>();
        var foreign = new HarmonyLib.Harmony("tests.tolerance.decision-args");
        replacement = new Drug();
        var captured = replacement;
        DrugToleranceDecisionPatch? patch = null;
        patch = new("DT-test", Scheduled, true, (_, candidate) => !ReferenceEquals(candidate, captured), () => patch!.Available, reports.Add);
        try
        {
            foreign.Patch(Scheduled, prefix: Method(nameof(ReplaceDrug)), postfix: Method(nameof(ChangeDrugAfter)));
            Apply(patch, reports);
            Assert.That(new Tracker().Scheduled(new Drug()), Is.False);
            Assert.That(reports, Is.Empty);
        }
        finally { replacement = null; try { patch.Candidate.Rollback(); } finally { foreign.Unpatch(Scheduled, HarmonyPatchType.All, foreign.Id); } }
    }

    [Test]
    public void LaterPostfixCannotTurnNativeRejectionIntoOwnedPermission()
    {
        var reports = new List<string>();
        var foreign = new HarmonyLib.Harmony("tests.tolerance.decision-result");
        DrugToleranceDecisionPatch? patch = null;
        patch = new("DT-test", Scheduled, true, (_, _) => true, () => patch!.Available, reports.Add);
        try
        {
            foreign.Patch(Scheduled, postfix: Method(nameof(Grant)));
            Apply(patch, reports);
            Assert.That(new Tracker().Scheduled(new Drug { Allowed = false }), Is.False);
            Assert.That(reports, Is.Empty);
        }
        finally { try { patch.Candidate.Rollback(); } finally { foreign.Unpatch(Scheduled, HarmonyPatchType.All, foreign.Id); } }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void UnavailableGroupAndOwnedFailureLeaveNativeResultIntact(bool failOwned)
    {
        var reports = new List<string>();
        bool enabled = failOwned;
        var patch = new DrugToleranceDecisionPatch("DT-test", Scheduled, true,
            (_, _) => throw new InvalidOperationException("owned selection failure"), () => enabled,
            message => { enabled = false; reports.Add(message); });
        try
        {
            Apply(patch, reports);
            Assert.That(new Tracker().Scheduled(new Drug()), Is.True);
            Assert.That(new Tracker().Scheduled(new Drug()), Is.True);
            Assert.That(reports, Has.Count.EqualTo(failOwned ? 1 : 0));
        }
        finally { patch.Candidate.Rollback(); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SkipAndNativeThrowNeverInvokeTheExtraRule(bool throwNative)
    {
        var reports = new List<string>();
        var foreign = new HarmonyLib.Harmony("tests.tolerance.decision-skip");
        var native = new ArgumentException("native exception");
        int calls = 0;
        var patch = new DrugToleranceDecisionPatch("DT-test", Scheduled, true, (_, _) => { calls++; return false; }, () => true, reports.Add);
        try
        {
            if (!throwNative) foreign.Patch(Scheduled, prefix: Method(nameof(Skip)));
            Apply(patch, reports);
            if (throwNative)
            {
                duringNative = () => throw native;
                Assert.That(Assert.Throws<ArgumentException>(() => new Tracker().Scheduled(new Drug())), Is.SameAs(native));
            }
            else Assert.That(new Tracker().Scheduled(new Drug()), Is.True);
            Assert.That(calls, Is.Zero);
            Assert.That(reports, Has.Count.EqualTo(throwNative ? 0 : 1));
        }
        finally { duringNative = null; try { patch.Candidate.Rollback(); } finally { foreign.Unpatch(Scheduled, HarmonyPatchType.All, foreign.Id); } }
    }

    [Test]
    public void CallbackRemovalRevokesUseAndCompatibleRepatchReadmits()
    {
        var reports = new List<string>();
        var foreign = new HarmonyLib.Harmony("tests.tolerance.decision-generation");
        DrugToleranceDecisionPatch? patch = null;
        patch = new("DT-test", Scheduled, true, (_, _) => false, () => patch!.Available, reports.Add);
        try
        {
            Apply(patch, reports);
            foreign.Patch(Scheduled, prefix: Method(nameof(Observe)));
            Assert.That(new Tracker().Scheduled(new Drug()), Is.False);
            new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId).Unpatch(Scheduled, HarmonyPatchType.Transpiler, OwnedHarmonyPatch.OwnerId);
            Assert.That(patch.Available, Is.False);
            Assert.That(new Tracker().Scheduled(new Drug()), Is.True);
        }
        finally { try { foreign.Unpatch(Scheduled, HarmonyPatchType.All, foreign.Id); } finally { patch.Candidate.Rollback(); } }
    }

    public static void ReplaceDrug(ref Drug __0) => __0 = replacement!;
    public static void ChangeDrugAfter(ref Drug __0) => __0 = new Drug();
    public static void Grant(ref bool __result) => __result = true;
    public static bool Skip(ref bool __result) { __result = true; return false; }
    public static void Observe() { }
    private static HarmonyMethod Method(string name) => new(typeof(DrugToleranceDecisionPatchTests), name);
    private static void Apply(DrugToleranceDecisionPatch patch, List<string> reports) => Assert.That(
        new AdmissionCoordinator(reports.Add).Apply(new("test-decisions", AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early), Is.True);
}
