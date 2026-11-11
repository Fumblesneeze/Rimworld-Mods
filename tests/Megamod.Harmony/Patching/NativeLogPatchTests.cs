using System.Runtime.CompilerServices;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Megamod.Patching;
using NUnit.Framework;
using HarmonyInstance = HarmonyLib.Harmony;

namespace Megamod.Harmony.Tests.Patching;

[TestFixture, NonParallelizable]
public sealed class NativeLogPatchTests
{
    private static int factoryCalls;
    private static int benignExecutions;

    [TestCase(false)]
    [TestCase(true)]
    public void CaptureExitFailureLeavesBaselineContextAdmittedOnTheSameRoute(bool capture)
    {
        var foreign = new HarmonyInstance("tests.megamod.log.suppressed-epilogue");
        MethodInfo target = typeof(LockedWarningLog).GetMethod("Warning")!;
        var patch = new NativeLogPatch(typeof(LockedWarningLog), "Warning", text => text + " [context]",
            capture: capture ? _ => false : null);
        try
        {
            LockedWarningLog.Reset();
            foreign.Patch(target, transpiler: new HarmonyMethod(typeof(NativeLogPatchTests), nameof(AddSuppressedEpilogue)));
            var coordinator = new AdmissionCoordinator(_ => { });
            Assert.That(coordinator.Apply(new PatchGroup("warning", AdmissionPhase.Early, [patch.Candidate]), AdmissionPhase.Early), Is.True);
            patch.Enable();
            Assert.That(patch.CaptureAvailable, Is.False);
            LockedWarningLog.Warning("ordinary");
            Assert.That(LockedWarningLog.Captured, Is.EqualTo(new[] { "ordinary [context]" }));
            Assert.That(LockedWarningLog.Effects, Is.EqualTo(2), "The owned adapter must preserve, not suppress, the foreign epilogue.");
        }
        finally
        {
            try { patch.Candidate.Rollback(); }
            finally { foreign.Unpatch(target, HarmonyPatchType.Transpiler, foreign.Id); LockedWarningLog.Reset(); }
        }
    }

    public static IEnumerable<CodeInstruction> AddSuppressedEpilogue(IEnumerable<CodeInstruction> instructions)
    {
        var incoming = instructions.ToList();
        int exit = incoming.FindIndex(instruction => instruction.opcode == OpCodes.Ret);
        var effect = new CodeInstruction(OpCodes.Call, typeof(NativeLogPatchTests).GetMethod(nameof(EpilogueEffect))!);
        effect.labels.AddRange(incoming[exit].labels);
        incoming[exit].labels.Clear();
        incoming.Insert(exit, effect);
        return incoming;
    }
    public static void EpilogueEffect() => LockedWarningLog.Effects++;

    [Test]
    public void MutingAfterNativeOnceEligibilityPreservesKeysSuppressionAndLockRelease()
    {
        int checks = 0;
        bool mute = true;
        var patch = new NativeLogPatch(typeof(LockedWarningLog), "Warning", text => text,
            capture: _ => { checks++; return !mute; });
        try
        {
            LockedWarningLog.Reset();
            var coordinator = new AdmissionCoordinator(_ => { });
            Assert.That(coordinator.Apply(new PatchGroup("warning", AdmissionPhase.Early, [patch.Candidate]), AdmissionPhase.Early), Is.True);
            LockedWarningLog.Warning("unpublished");
            patch.Enable();
            LockedWarningLog.Suppressed = true;
            LockedWarningLog.WarningOnce("natively rejected", 1);
            LockedWarningLog.Suppressed = false;
            LockedWarningLog.WarningOnce("muted once", 1);
            mute = false;
            LockedWarningLog.WarningOnce("must not replay", 1);
            LockedWarningLog.WarningOnce("new once", 2);
            Assert.That(LockedWarningLog.Captured, Is.EqualTo(new[] { "unpublished", "new once" }));
            Assert.That(LockedWarningLog.Effects, Is.EqualTo(2));
            Assert.That(checks, Is.EqualTo(2));
            bool released = Task.Run(() =>
            {
                if (!Monitor.TryEnter(LockedWarningLog.SyncRoot, TimeSpan.FromSeconds(1))) return false;
                Monitor.Exit(LockedWarningLog.SyncRoot);
                return true;
            }).GetAwaiter().GetResult();
            Assert.That(released, Is.True);
        }
        finally { patch.Candidate.Rollback(); LockedWarningLog.Reset(); }
    }

    public static class LockedWarningLog
    {
        private static readonly SampleCategory logCategory = new();
        public static readonly object SyncRoot = new();
        private static readonly HashSet<int> usedKeys = new();
        private static bool PreventLogging => Suppressed;
        public static bool Suppressed;
        public static int Effects;
        public static IReadOnlyList<string> Captured => logCategory.Captured;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Warning(string text)
        {
            lock (SyncRoot)
            {
                if (PreventLogging) return;
                logCategory.Warning(text, nameof(Warning), "fixture.cs", 1);
                Effects++;
            }
        }
        public static void WarningOnce(string text, int key)
        {
            lock (SyncRoot)
                if (!PreventLogging && usedKeys.Add(key)) Warning(text);
        }
        public static void Reset() { logCategory.Captured.Clear(); usedKeys.Clear(); Effects = 0; Suppressed = false; }
    }

    [Test]
    public void CaptureFailureFailsOpenOnceWithoutDisablingContext()
    {
        int attempts = 0;
        var warnings = new List<string>();
        var patch = new NativeLogPatch(typeof(SampleLog), nameof(SampleLog.Warning), text => text + " [context]", warnings.Add,
            capture: _ => { attempts++; throw new InvalidOperationException("capture broke"); });
        var foreign = new HarmonyInstance("tests.megamod.log.capture-fault-rebuild");
        MethodInfo target = typeof(SampleLog).GetMethod(nameof(SampleLog.Warning))!;
        try
        {
            SampleLog.Reset();
            var coordinator = new AdmissionCoordinator(_ => { });
            Assert.That(coordinator.Apply(new PatchGroup("warning", AdmissionPhase.Early, [patch.Candidate]), AdmissionPhase.Early), Is.True);
            patch.Enable();
            Assert.DoesNotThrow(() => SampleLog.Warning("first"));
            SampleLog.Warning("second");
            foreign.Patch(target, transpiler: new HarmonyMethod(typeof(NativeDiagnosticReportingTests), nameof(NativeDiagnosticReportingTests.Benign)));
            Assert.That(patch.RefreshPublication(), Is.True);
            SampleLog.Warning("after rebuild");
            Assert.That(SampleLog.Captured, Is.EqualTo(new[] { "first [context]", "second [context]", "after rebuild [context]" }));
            Assert.That(attempts, Is.EqualTo(1));
            Assert.That(patch.Available, Is.True);
            Assert.That(patch.CaptureAvailable, Is.False);
            Assert.That(patch.CaptureFailure, Is.EqualTo("capture broke"));
            Assert.That(warnings, Has.Count.EqualTo(1));
            Assert.That(warnings[0], Does.Contain("capture").And.Contain("ordinary native diagnostics remain"));
        }
        finally
        {
            try { patch.Candidate.Rollback(); }
            finally { foreign.Unpatch(target, HarmonyPatchType.Transpiler, foreign.Id); SampleLog.Reset(); }
        }
    }

    [Test]
    public void MutedWarningSkipsContextOutputAndNativeEffectsButUnmutingKeepsEarlierEvidence()
    {
        bool mute = false;
        int projections = 0;
        var patch = new NativeLogPatch(typeof(SampleLog), nameof(SampleLog.Warning), text => { projections++; return text + " [context]"; },
            capture: _ => !mute);
        try
        {
            SampleLog.Reset();
            var coordinator = new AdmissionCoordinator(_ => { });
            Assert.That(coordinator.Apply(new PatchGroup("warning", AdmissionPhase.Early, [patch.Candidate]), AdmissionPhase.Early), Is.True);
            patch.Enable();
            SampleLog.Warning("before");
            mute = true;
            SampleLog.Warning("lost");
            mute = false;
            SampleLog.Warning("after");
            Assert.That(SampleLog.Captured, Is.EqualTo(new[] { "before [context]", "after [context]" }));
            Assert.That(SampleLog.AcceptedEffects, Is.EqualTo(2));
            Assert.That(projections, Is.EqualTo(2));
        }
        finally { patch.Candidate.Rollback(); SampleLog.Reset(); }
    }

    [Test]
    public void BenignRebuildWaitsForSafeRefreshAndDoesNotPublishAnUnenabledGroup()
    {
        var foreign = new HarmonyInstance("tests.megamod.log.refresh");
        MethodInfo target = typeof(SampleLog).GetMethod(nameof(SampleLog.Warning))!;
        var patch = new NativeLogPatch(typeof(SampleLog), nameof(SampleLog.Warning), text => text + " [context]");
        try
        {
            SampleLog.Reset();
            var coordinator = new AdmissionCoordinator(_ => { });
            Assert.That(coordinator.Apply(new PatchGroup("reporting", AdmissionPhase.Early, [patch.Candidate]), AdmissionPhase.Early), Is.True);
            Assert.That(patch.RefreshPublication(), Is.True);
            SampleLog.Warning("unpublished");
            patch.Enable();
            foreign.Patch(target, transpiler: new HarmonyMethod(typeof(NativeLogPatchTests), nameof(Benign)));
            Assert.That(patch.Available, Is.False);
            SampleLog.Warning("awaiting refresh");
            Assert.That(patch.RefreshPublication(), Is.True);
            SampleLog.Warning("refreshed");
            Assert.That(SampleLog.Captured, Is.EqualTo(new[] { "unpublished", "awaiting refresh", "refreshed [context]" }));
        }
        finally
        {
            try { patch.Candidate.Rollback(); }
            finally { foreign.Unpatch(target, HarmonyPatchType.Transpiler, foreign.Id); SampleLog.Reset(); }
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EnteringSuppressionBranchAfterItsGetterRejectsOrDeactivatesContext(bool patchLater)
    {
        var foreign = new HarmonyInstance("tests.megamod.log.bypass");
        MethodInfo target = typeof(SampleLog).GetMethod(nameof(SampleLog.Warning))!;
        var patch = new NativeLogPatch(typeof(SampleLog), nameof(SampleLog.Warning), text => text + " [context]");
        try
        {
            SampleLog.Reset();
            if (!patchLater) foreign.Patch(target, transpiler: new HarmonyMethod(typeof(NativeLogPatchTests), nameof(BypassSuppressionGetter)));
            var coordinator = new AdmissionCoordinator(_ => { });
            bool admitted = coordinator.Apply(new PatchGroup("reporting", AdmissionPhase.Early, [patch.Candidate]), AdmissionPhase.Early);
            Assert.That(admitted, Is.EqualTo(patchLater));
            if (patchLater)
            {
                patch.Enable();
                foreign.Patch(target, transpiler: new HarmonyMethod(typeof(NativeLogPatchTests), nameof(BypassSuppressionGetter)));
                Assert.That(patch.RefreshPublication(), Is.False);
            }
            Assert.That(patch.Available, Is.False);
            SampleLog.Suppressed = true;
            SampleLog.Warning("foreign accepted");
            Assert.That(SampleLog.Captured, Is.EqualTo(new[] { "foreign accepted" }));
        }
        finally
        {
            try { patch.Candidate.Rollback(); }
            finally { foreign.Unpatch(target, HarmonyPatchType.Transpiler, foreign.Id); SampleLog.Reset(); }
        }
    }

    public static IEnumerable<CodeInstruction> BypassSuppressionGetter(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var incoming = instructions.ToList();
        MethodInfo getter = typeof(SampleLog).GetMethod("get_PreventLogging", BindingFlags.NonPublic | BindingFlags.Static)!;
        int branch = incoming.FindIndex(instruction => instruction.Calls(getter)) + 1;
        while (incoming[branch].opcode == OpCodes.Nop) branch++;
        Label bypass = generator.DefineLabel();
        incoming[branch].labels.Add(bypass);
        incoming.InsertRange(0, new[] { new CodeInstruction(OpCodes.Ldc_I4_0), new CodeInstruction(OpCodes.Br, bypass) });
        return incoming;
    }

    [Test]
    public void LoggingWhileAnotherTranspilerHoldsHarmonyLockDoesNotWaitForPatchMetadata()
    {
        var foreign = new HarmonyInstance("tests.megamod.log.concurrent");
        MethodInfo target = typeof(ConcurrentLog).GetMethod(nameof(ConcurrentLog.Error))!;
        var patch = new NativeLogPatch(typeof(ConcurrentLog), nameof(ConcurrentLog.Error), text => text + " [context]");
        Task? logging = null;
        Task? patching = null;
        try
        {
            ConcurrentLog.Reset();
            var coordinator = new AdmissionCoordinator(_ => { });
            Assert.That(coordinator.Apply(new PatchGroup("reporting", AdmissionPhase.Early, [patch.Candidate]), AdmissionPhase.Early), Is.True);
            patch.Enable();
            ConcurrentLog.Coordinate = true;
            logging = Task.Run(() => ConcurrentLog.Error("worker"));
            Assert.That(ConcurrentLog.LoggerEntered.Wait(TimeSpan.FromSeconds(5)), Is.True);
            patching = Task.Run(() => foreign.Patch(target,
                transpiler: new HarmonyMethod(typeof(NativeLogPatchTests), nameof(LoggingTransform))));
            Assert.That(Task.WaitAll(new[] { logging, patching }, TimeSpan.FromSeconds(10)), Is.True);
            Assert.That(ConcurrentLog.TranspilerAcquiredLogLock, Is.True,
                "The output path must finish while the foreign transpiler holds Harmony's registry lock.");
            Assert.That(ConcurrentLog.Captured, Does.Contain("worker [context]").And.Contain("during patch [context]"));
        }
        finally
        {
            ConcurrentLog.TranspilerEntered.Set();
            try { if (logging != null) logging.Wait(TimeSpan.FromSeconds(10)); }
            finally
            {
                try { if (patching != null) patching.Wait(TimeSpan.FromSeconds(10)); }
                finally
                {
                    ConcurrentLog.Coordinate = false;
                    try { patch.Candidate.Rollback(); }
                    finally { foreign.Unpatch(target, HarmonyPatchType.Transpiler, foreign.Id); }
                }
            }
        }
    }

    public static IEnumerable<CodeInstruction> LoggingTransform(IEnumerable<CodeInstruction> incoming)
    {
        ConcurrentLog.TranspilerEntered.Set();
        // Bound the opposing lock attempt so a broken adapter produces a failure, not a hung runner.
        bool entered = Monitor.TryEnter(ConcurrentLog.SyncRoot, TimeSpan.FromSeconds(2));
        ConcurrentLog.TranspilerAcquiredLogLock = entered;
        if (entered)
        {
            try { ConcurrentLog.Error("during patch"); }
            finally { Monitor.Exit(ConcurrentLog.SyncRoot); }
        }
        return incoming;
    }

    public static class ConcurrentLog
    {
        private static readonly SampleCategory logCategory = new();
        public static readonly object SyncRoot = new();
        public static readonly ManualResetEventSlim LoggerEntered = new();
        public static readonly ManualResetEventSlim TranspilerEntered = new();
        public static bool Coordinate;
        public static bool TranspilerAcquiredLogLock;
        public static IReadOnlyList<string> Captured => logCategory.Captured;
        private static bool PreventLogging
        {
            get
            {
                if (Coordinate)
                {
                    LoggerEntered.Set();
                    if (!TranspilerEntered.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Transpiler did not enter.");
                }
                return false;
            }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Error(string text)
        {
            lock (SyncRoot)
            {
                if (PreventLogging) return;
                logCategory.Error(text, nameof(Error), "fixture.cs", 1);
            }
        }
        public static void Reset()
        {
            logCategory.Captured.Clear();
            LoggerEntered.Reset();
            TranspilerEntered.Reset();
            Coordinate = false;
            TranspilerAcquiredLogLock = false;
        }
    }

    [Test]
    public void SkipOriginalPrefixDoesNotCreateContextOrNativeOutput()
    {
        var foreign = new HarmonyInstance("tests.megamod.log.skip");
        MethodInfo target = typeof(SampleLog).GetMethod(nameof(SampleLog.Warning))!;
        int projections = 0;
        var patch = new NativeLogPatch(typeof(SampleLog), nameof(SampleLog.Warning), text => { projections++; return text; });
        try
        {
            SampleLog.Reset();
            foreign.Patch(target, prefix: new HarmonyMethod(typeof(NativeLogPatchTests), nameof(Skip)));
            var coordinator = new AdmissionCoordinator(_ => { });
            Assert.That(coordinator.Apply(new PatchGroup("reporting", AdmissionPhase.Early, [patch.Candidate]), AdmissionPhase.Early), Is.True);
            patch.Enable();
            SampleLog.Warning("skipped");
            Assert.That(projections, Is.Zero);
            Assert.That(SampleLog.Captured, Is.Empty);
            Assert.That(SampleLog.AcceptedEffects, Is.Zero);
        }
        finally
        {
            try { patch.Candidate.Rollback(); }
            finally { foreign.Unpatch(target, HarmonyPatchType.Prefix, foreign.Id); SampleLog.Reset(); }
        }
    }

    public static bool Skip() => false;

    [Test]
    public void ClosedGenericDeclaringTypeCannotAcquireTheNonGenericNativeRoute()
    {
        var patch = new NativeLogPatch(typeof(GenericLog<int>), "Warning", text => text);
        Assert.That(patch.Candidate.Validate(), Is.Not.Null);
    }

    public static class GenericLog<T>
    {
        private static readonly SampleCategory logCategory = new();
        private static bool PreventLogging => SampleLog.Suppressed;
        public static void Warning(string text)
        {
            if (PreventLogging) return;
            logCategory.Warning(text, "Warning", "fixture.cs", 1);
        }
    }

    [Test]
    public void AnotherUseOfTheInputCannotHideAnUnprovenSecondaryDiagnostic()
    {
        var patch = new NativeLogPatch(typeof(ChangedTextLog), nameof(ChangedTextLog.Warning), text => text + " [context]");
        try
        {
            var coordinator = new AdmissionCoordinator(_ => { });
            Assert.That(coordinator.Apply(new PatchGroup("reporting", AdmissionPhase.Early, [patch.Candidate]), AdmissionPhase.Early), Is.False);
        }
        finally { patch.Candidate.Rollback(); }
    }

    public static class ChangedTextLog
    {
        private static readonly SampleCategory logCategory = new();
        private static bool PreventLogging => SampleLog.Suppressed;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Warning(string text)
        {
            if (PreventLogging) return;
            logCategory.Warning(text, "Warning", "fixture.cs", 1);
            logCategory.Warning("changed " + text, "Warning", "fixture.cs", 2);
        }
    }

    [Test]
    public void CompatibleForeignTransformIsNotReexecutedByAdmissionProbesOrReads()
    {
        var foreign = new HarmonyInstance("tests.megamod.log.benign");
        MethodInfo target = typeof(SampleLog).GetMethod(nameof(SampleLog.Warning))!;
        var patch = new NativeLogPatch(typeof(SampleLog), nameof(SampleLog.Warning), text => text + " [context]");
        try
        {
            SampleLog.Reset();
            benignExecutions = 0;
            foreign.Patch(target, transpiler: new HarmonyMethod(typeof(NativeLogPatchTests), nameof(Benign)));
            Assert.That(benignExecutions, Is.EqualTo(1));
            var coordinator = new AdmissionCoordinator(_ => { });
            var group = new PatchGroup("reporting", AdmissionPhase.Early, [patch.Candidate]);
            Assert.That(coordinator.Apply(group, AdmissionPhase.Early), Is.True);
            Assert.That(coordinator.Apply(group, AdmissionPhase.Early), Is.True);
            patch.Enable();
            for (int index = 0; index < 3; index++) Assert.That(patch.Available, Is.True);
            SampleLog.Warning("accepted");
            Assert.That(SampleLog.Captured, Is.EqualTo(new[] { "accepted [context]" }));
            Assert.That(benignExecutions, Is.EqualTo(2), "Only the foreign install and ordinary owned install may run its transform.");
        }
        finally
        {
            try { patch.Candidate.Rollback(); }
            finally { foreign.Unpatch(target, HarmonyPatchType.Transpiler, foreign.Id); SampleLog.Reset(); }
        }
    }

    [Test]
    public void ErrorExceptionOverloadDoesNotRejectTheConsumedStringOutput()
    {
        var patch = new NativeLogPatch(typeof(OverloadedErrorLog), nameof(OverloadedErrorLog.Error), text => text + " [context]");
        try
        {
            OverloadedErrorLog.Captured.Clear();
            var coordinator = new AdmissionCoordinator(_ => { });
            Assert.That(coordinator.Apply(new PatchGroup("overloaded-error", AdmissionPhase.Early, [patch.Candidate]), AdmissionPhase.Early), Is.True);
            patch.Enable();
            OverloadedErrorLog.Error("native string error");
            Assert.That(OverloadedErrorLog.Captured, Is.EqualTo(new[] { "native string error [context]" }));
        }
        finally { patch.Candidate.Rollback(); OverloadedErrorLog.Captured.Clear(); }
    }

    public sealed class OverloadedErrorCategory
    {
        public readonly List<string> Captured = new();
        public void Error(LogValue<ErrorLevel> text, string member, string file, int line) => Captured.Add(text.Text);
        public void Error(Exception exception, string member, string file, int line) => throw new InvalidOperationException("Unconsumed Exception overload must not be invoked.");
    }

    public static class OverloadedErrorLog
    {
        private static readonly OverloadedErrorCategory logCategory = new();
        private static bool PreventLogging => false;
        public static List<string> Captured => logCategory.Captured;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Error(string text)
        {
            if (PreventLogging) return;
            logCategory.Error(text, nameof(Error), "fixture.cs", 1);
        }
    }

    [Test]
    public void MultipleConvertibleErrorOutputsRemainUnavailable()
    {
        var patch = new NativeLogPatch(typeof(CompetingErrorLog), nameof(CompetingErrorLog.Error), text => text + " [context]");
        var notices = new List<string>();
        try
        {
            CompetingErrorLog.Captured.Clear();
            var coordinator = new AdmissionCoordinator(notices.Add);
            Assert.That(coordinator.Apply(new PatchGroup("competing-error", AdmissionPhase.Early, [patch.Candidate]), AdmissionPhase.Early), Is.False);
            Assert.That(patch.Available, Is.False);
            CompetingErrorLog.Error("ordinary error");
            Assert.That(CompetingErrorLog.Captured, Is.EqualTo(new[] { "ordinary error" }));
            Assert.That(notices, Has.Count.EqualTo(1));
            Assert.That(notices[0], Does.Contain("ambiguous"));
        }
        finally { patch.Candidate.Rollback(); CompetingErrorLog.Captured.Clear(); }
    }

    public sealed class CompetingErrorCategory
    {
        public readonly List<string> Captured = new();
        public void Error(LogValue<ErrorLevel> text, string member, string file, int line) => Captured.Add(text.Text);
        public void Error(LogValue<WarningLevel> text, string member, string file, int line) => throw new InvalidOperationException("Competing output must not be invoked during admission.");
    }

    public static class CompetingErrorLog
    {
        private static readonly CompetingErrorCategory logCategory = new();
        private static bool PreventLogging => false;
        public static List<string> Captured => logCategory.Captured;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Error(string text)
        {
            if (PreventLogging) return;
            logCategory.Error((LogValue<ErrorLevel>)text, nameof(Error), "fixture.cs", 1);
        }
    }

    [Test]
    public void ErrorProjectionLeavesTheSeparateNativeLoggingFailureOutputAlone()
    {
        var patch = new NativeLogPatch(typeof(SampleLog), nameof(SampleLog.Error), text => text + " [context]");
        try
        {
            SampleLog.Reset();
            var coordinator = new AdmissionCoordinator(_ => { });
            Assert.That(coordinator.Apply(new PatchGroup("reporting", AdmissionPhase.Early, [patch.Candidate]), AdmissionPhase.Early), Is.True);
            patch.Enable();
            SampleLog.Error("accepted error");
            Assert.That(SampleLog.Captured, Is.EqualTo(new[] { "accepted error [context]", "secondary logging failure" }));
        }
        finally { patch.Candidate.Rollback(); SampleLog.Reset(); }
    }

    [Test]
    public void ReportingGroupRejectionRemovesTheFirstRouteWithoutPublication()
    {
        var first = new NativeLogPatch(typeof(SampleLog), nameof(SampleLog.Warning), text => text + " [context]");
        var second = new NativeLogPatch(typeof(AmbiguousLog), nameof(AmbiguousLog.Warning), text => text + " [context]");
        var warnings = new List<string>();
        try
        {
            SampleLog.Reset();
            var coordinator = new AdmissionCoordinator(warnings.Add);
            Assert.That(coordinator.Apply(new PatchGroup("reporting", AdmissionPhase.Early, [first.Candidate, second.Candidate]), AdmissionPhase.Early), Is.False);
            Assert.That(first.Available, Is.False);
            Assert.That(second.Available, Is.False);
            SampleLog.Warning("ordinary");
            Assert.That(SampleLog.Captured, Is.EqualTo(new[] { "ordinary" }));
            Assert.That(warnings, Has.Count.EqualTo(2));
            Assert.That(HarmonyInstance.GetPatchInfo(typeof(SampleLog).GetMethod(nameof(SampleLog.Warning))!)?.Transpilers.Any(patch => patch.owner == OwnedHarmonyPatch.OwnerId), Is.Not.True);
        }
        finally
        {
            try { first.Candidate.Rollback(); }
            finally { second.Candidate.Rollback(); SampleLog.Reset(); }
        }
    }

    [Test]
    public void MultipleSuppliedTextOutputsAreAmbiguousEvenWhenOnlyTheFirstFollowsTheGuard()
    {
        var patch = new NativeLogPatch(typeof(AmbiguousLog), nameof(AmbiguousLog.Warning), text => text + " [context]");
        try
        {
            var coordinator = new AdmissionCoordinator(_ => { });
            Assert.That(coordinator.Apply(new PatchGroup("reporting", AdmissionPhase.Early, [patch.Candidate]), AdmissionPhase.Early), Is.False);
            Assert.That(patch.Available, Is.False);
        }
        finally { patch.Candidate.Rollback(); }
    }

    public static class AmbiguousLog
    {
        private static readonly SampleCategory logCategory = new();
        private static bool PreventLogging => SampleLog.Suppressed;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Warning(string text)
        {
            if (PreventLogging) return;
            logCategory.Warning(text, "Warning", "fixture.cs", 1);
            logCategory.Warning(text, "Warning", "fixture.cs", 2);
        }
    }

    [Test]
    public void ProjectionFailureKeepsOriginalOutputAndExplainsOnlyOnce()
    {
        int attempts = 0;
        var failures = new List<string>();
        var patch = new NativeLogPatch(typeof(SampleLog), nameof(SampleLog.Warning), _ =>
        {
            attempts++;
            throw new InvalidOperationException("formatting failed\n" + new string('x', 1024));
        }, failure => { failures.Add(failure); throw new InvalidOperationException("sink unavailable"); });
        try
        {
            SampleLog.Reset();
            var coordinator = new AdmissionCoordinator(_ => { });
            Assert.That(coordinator.Apply(new PatchGroup("reporting", AdmissionPhase.Early, [patch.Candidate]), AdmissionPhase.Early), Is.True);
            patch.Enable();
            SampleLog.Warning("first native");
            SampleLog.Warning("second native");

            Assert.That(SampleLog.Captured, Is.EqualTo(new[] { "first native", "second native" }));
            Assert.That(attempts, Is.EqualTo(1));
            Assert.That(failures, Has.Count.EqualTo(1));
            Assert.That(failures[0], Does.Contain("DDI-REPORT-WARNING").And.Contain("formatting failed").And.Not.Contain("\n"));
            Assert.That(failures[0].Length, Is.LessThan(512));
            Assert.That(patch.Available, Is.False);
        }
        finally { patch.Candidate.Rollback(); SampleLog.Reset(); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ATransformOrderedAfterTheOwnedProjectionCannotRemainAdmitted(bool patchLater)
    {
        var foreign = new HarmonyInstance("tests.megamod.log.after");
        MethodInfo target = typeof(SampleLog).GetMethod(nameof(SampleLog.Warning))!;
        var patch = new NativeLogPatch(typeof(SampleLog), nameof(SampleLog.Warning), text => text + " [context]");
        var after = new HarmonyMethod(typeof(NativeLogPatchTests), nameof(Benign)) { after = new[] { OwnedHarmonyPatch.OwnerId } };
        try
        {
            SampleLog.Reset();
            if (!patchLater) foreign.Patch(target, transpiler: after);
            var coordinator = new AdmissionCoordinator(_ => { });
            bool admitted = coordinator.Apply(new PatchGroup("reporting", AdmissionPhase.Early, [patch.Candidate]), AdmissionPhase.Early);
            Assert.That(admitted, Is.EqualTo(patchLater));
            if (patchLater) { patch.Enable(); foreign.Patch(target, transpiler: after); }

            Assert.That(patch.RefreshPublication(), Is.False);

            SampleLog.Warning("ordinary");
            Assert.That(SampleLog.Captured, Is.EqualTo(new[] { "ordinary" }));
            Assert.That(patch.Available, Is.False);
        }
        finally
        {
            try { patch.Candidate.Rollback(); }
            finally { foreign.Unpatch(target, HarmonyPatchType.Transpiler, foreign.Id); SampleLog.Reset(); }
        }
    }

    [Test]
    public void FactoryRegistrationIsRejectedWithoutInvokingItAgain()
    {
        var foreign = new HarmonyInstance("tests.megamod.log.factory");
        MethodInfo target = typeof(SampleLog).GetMethod(nameof(SampleLog.Warning))!;
        var patch = new NativeLogPatch(typeof(SampleLog), nameof(SampleLog.Warning), text => text + " [context]");
        try
        {
            factoryCalls = 0;
            foreign.Patch(target, transpiler: new HarmonyMethod(typeof(NativeLogPatchTests), nameof(Factory)));
            Assert.That(factoryCalls, Is.EqualTo(1));

            var coordinator = new AdmissionCoordinator(_ => { });
            Assert.That(coordinator.Apply(new PatchGroup("reporting", AdmissionPhase.Early, [patch.Candidate]), AdmissionPhase.Early), Is.False);
            Assert.That(factoryCalls, Is.EqualTo(1));
            Assert.That(HarmonyInstance.GetPatchInfo(target).Transpilers.Select(patch => patch.owner), Is.EqualTo(new[] { foreign.Id }));
        }
        finally
        {
            try { patch.Candidate.Rollback(); }
            finally { foreign.Unpatch(target, HarmonyPatchType.Transpiler, foreign.Id); SampleLog.Reset(); }
        }
    }

    public static MethodInfo Factory(MethodBase original)
    {
        factoryCalls++;
        return typeof(NativeLogPatchTests).GetMethod(nameof(Benign))!;
    }
    public static IEnumerable<CodeInstruction> Benign(IEnumerable<CodeInstruction> incoming)
    {
        benignExecutions++;
        return new[] { new CodeInstruction(OpCodes.Nop) }.Concat(incoming);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RemovingNativeSuppressionRejectsOrDeactivatesContext(bool patchLater)
    {
        var foreign = new HarmonyInstance("tests.megamod.log.suppression");
        MethodInfo target = typeof(SampleLog).GetMethod(nameof(SampleLog.Warning))!;
        var patch = new NativeLogPatch(typeof(SampleLog), nameof(SampleLog.Warning), text => text + " [context]");
        var coordinator = new AdmissionCoordinator(_ => { });
        try
        {
            SampleLog.Reset();
            if (!patchLater) foreign.Patch(target, transpiler: new HarmonyMethod(typeof(NativeLogPatchTests), nameof(RemoveSuppression)));
            bool admitted = coordinator.Apply(new PatchGroup("reporting", AdmissionPhase.Early, [patch.Candidate]), AdmissionPhase.Early);
            Assert.That(admitted, Is.EqualTo(patchLater));
            if (patchLater)
            {
                patch.Enable();
                foreign.Patch(target, transpiler: new HarmonyMethod(typeof(NativeLogPatchTests), nameof(RemoveSuppression)));
            }

            Assert.That(patch.Available, Is.False);
            Assert.That(patch.RefreshPublication(), Is.False);
            SampleLog.Suppressed = true;
            SampleLog.Warning("foreign accepted");
            Assert.That(SampleLog.Captured, Is.EqualTo(new[] { "foreign accepted" }));
        }
        finally
        {
            try { patch.Candidate.Rollback(); }
            finally { foreign.Unpatch(target, HarmonyPatchType.Transpiler, foreign.Id); SampleLog.Reset(); }
        }
    }

    public static IEnumerable<CodeInstruction> RemoveSuppression(IEnumerable<CodeInstruction> incoming)
    {
        MethodInfo getter = typeof(SampleLog).GetMethod("get_PreventLogging", BindingFlags.NonPublic | BindingFlags.Static)!;
        return incoming.Select(instruction => instruction.Calls(getter)
            ? new CodeInstruction(OpCodes.Ldc_I4_0) : instruction);
    }

    [Test]
    public void ContextProjectsOneAcceptedNativeOutputOnlyAfterGroupPublication()
    {
        int projections = 0;
        var patch = new NativeLogPatch(typeof(SampleLog), nameof(SampleLog.Warning), text =>
        {
            projections++;
            return text + " [context]";
        });
        var coordinator = new AdmissionCoordinator(_ => { });
        try
        {
            SampleLog.Reset();
            Assert.That(coordinator.Apply(new PatchGroup("diagnostics.definition-reporting", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.True);
            SampleLog.Warning("before publication");
            Assert.That(SampleLog.Captured, Is.EqualTo(new[] { "before publication" }));
            Assert.That(projections, Is.Zero);

            patch.Enable();
            SampleLog.Reset();
            SampleLog.Warning("accepted");
            SampleLog.Suppressed = true;
            SampleLog.Warning("suppressed");

            Assert.That(SampleLog.Captured, Is.EqualTo(new[] { "accepted [context]" }));
            Assert.That(projections, Is.EqualTo(1));
            Assert.That(SampleLog.AcceptedEffects, Is.EqualTo(1));
        }
        finally
        {
            try { patch.Candidate.Rollback(); }
            finally
            {
                new HarmonyInstance(OwnedHarmonyPatch.OwnerId).Unpatch(
                    typeof(SampleLog).GetMethod(nameof(SampleLog.Warning))!, HarmonyPatchType.Transpiler, OwnedHarmonyPatch.OwnerId);
                SampleLog.Reset();
            }
        }
    }

    public readonly record struct WarningLevel;
    public readonly record struct ErrorLevel;
    public readonly record struct LogValue<T>(string Text)
    {
        public static implicit operator LogValue<T>(string text) => new(text);
    }

    public sealed class SampleCategory
    {
        public readonly List<string> Captured = new();
        public void Warning(LogValue<WarningLevel> text, string member, string file, int line) => Captured.Add(text.Text);
        public void Error(LogValue<ErrorLevel> text, string member, string file, int line) => Captured.Add(text.Text);
    }

    public static class SampleLog
    {
        private static readonly SampleCategory logCategory = new();
        private static readonly object logLock = new();
        private static bool PreventLogging => Suppressed;
        public static bool Suppressed;
        public static int AcceptedEffects;
        public static IReadOnlyList<string> Captured => logCategory.Captured;

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Warning(string text)
        {
            if (PreventLogging) return;
            logCategory.Warning(text, nameof(Warning), "fixture.cs", 1);
            AcceptedEffects++;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void Error(string text)
        {
            lock (logLock)
            {
                if (PreventLogging) return;
                logCategory.Error(text, nameof(Error), "fixture.cs", 1);
                logCategory.Error("secondary logging failure", nameof(Error), "fixture.cs", 2);
            }
        }

        public static void Reset()
        {
            logCategory.Captured.Clear();
            Suppressed = false;
            AcceptedEffects = 0;
        }
    }
}
