using System.Runtime.CompilerServices;
using System.Reflection;
using HarmonyLib;
using Megamod.Features.MessagePlacement;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.MessagePlacement;

[TestFixture, NonParallelizable]
public sealed class MessageGuiScopePatchTests
{
    private static readonly List<string> Events = new();
    private static Exception? nativeFailure;
    private static int factoryCalls;
    private static MessageDrawPatchTests.NativeMessage? replacementMessage;
    private static Action? observeWindow;
    private static Action? duringNative;
    private static int parameterConstructions, methodConstructions, innerObservations, outerObservations, finalizerObservations;

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void AcquiredScopeCompletesUnsuccessfullyWhenPublishedMetadataBecomesUnreadable(bool nativeThrows, bool completionThrows)
    {
        var target = typeof(MessageGuiScopePatchTests).GetMethod(nameof(NativeMessagesGui))!;
        var foreign = new HarmonyLib.Harmony(new string('x', 4 * 1024 * 1024));
        object token = new();
        object? activeToken = null;
        int begins = 0;
        bool repatchedAfterAcquisition = false, repatchCompleted = false;
        var completions = new List<(object Token, bool Success)>();
        var reports = new List<Exception>();
        var patch = new MessageGuiScopePatch(target, () => { begins++; activeToken = token; return token; },
            (received, success) =>
            {
                completions.Add((received, success));
                activeToken = null;
                if (completionThrows) throw new ApplicationException("completion failed after clearing the token");
            }, error => { reports.Add(error); throw new ApplicationException("reporting failed"); });
        try
        {
            Events.Clear(); nativeFailure = nativeThrows ? new InvalidOperationException("native") : null;
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("message-frame", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.True);
            patch.Enable();
            duringNative = () =>
            {
                repatchedAfterAcquisition = ReferenceEquals(activeToken, token);
                foreign.Patch(target, prefix: new HarmonyMethod(typeof(MessageGuiScopePatchTests), nameof(ForeignMetadataObserver)));
                repatchCompleted = true;
            };
            if (nativeThrows) Assert.That(Assert.Throws<InvalidOperationException>(NativeMessagesGui), Is.SameAs(nativeFailure));
            else Assert.DoesNotThrow(NativeMessagesGui);
            Assert.That(repatchedAfterAcquisition && repatchCompleted, Is.True, "The real provider must publish the oversized registration after scope acquisition.");
            Assert.That(completions, Has.Count.EqualTo(1));
            Assert.That(completions[0].Token, Is.SameAs(token));
            Assert.That(completions[0].Success, Is.False);
            Assert.That(activeToken, Is.Null);
            Assert.That(reports, Has.Count.EqualTo(1));
            Assert.That(reports[0].Message, Does.Contain("4 MiB inspection limit"));

            duringNative = null; nativeFailure = null;
            Assert.DoesNotThrow(NativeMessagesGui);
            Assert.That(begins, Is.EqualTo(1));
            Assert.That(completions, Has.Count.EqualTo(1));
            Assert.That(reports, Has.Count.EqualTo(1));
            Assert.That(Events, Is.EqualTo(new[] { "native", "native" }));
        }
        finally
        {
            duringNative = null; nativeFailure = null;
            // Restore inspectable metadata before asking the owned receipt reader to roll back.
            try { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
            finally { try { patch.Candidate.Rollback(); } finally { Events.Clear(); } }
        }
    }

    public static void ForeignMetadataObserver() { }

    [Test]
    public void ScopeAdmissionAndInvocationInspectionDoNotConstructForeignAttributes()
    {
        var target = typeof(MessageGuiScopePatchTests).GetMethod(nameof(NativeMessagesGui))!;
        var foreign = new HarmonyLib.Harmony("tests.messages.scope-inspection-attributes");
        var failures = new List<Exception>();
        var patch = new MessageGuiScopePatch(target, () => { Events.Add("begin"); return new object(); },
            (_, success) => Events.Add(success ? "success" : "abort"), failures.Add);
        try
        {
            Events.Clear(); nativeFailure = null;
            parameterConstructions = methodConstructions = innerObservations = outerObservations = finalizerObservations = 0;
            foreign.CreateProcessor(target).AddInnerPostfix(new HarmonyMethod(typeof(MessageGuiScopePatchTests), nameof(ObserveNativeEvent))
                { innerMethod = new InnerMethod(typeof(List<string>).GetMethod(nameof(List<string>.Add))!) }).Patch();
            foreign.Patch(target, prefix: new HarmonyMethod(typeof(MessageGuiScopePatchTests), nameof(ObserveOuterCall))
                { after = [OwnedHarmonyPatch.OwnerId] },
                finalizer: new HarmonyMethod(typeof(MessageGuiScopePatchTests), nameof(ObserveOuterFinalizer)));
            Assert.That(parameterConstructions, Is.GreaterThan(0), "Normal provider composition consumes the parameter attribute.");
            Assert.That(methodConstructions, Is.GreaterThan(0), "Normal provider registration consumes the method attribute.");
            parameterConstructions = methodConstructions = 0;

            Assert.That(patch.Candidate.Validate(), Is.Null);
            Assert.That(parameterConstructions, Is.Zero, "Owned inspection must not construct foreign parameter attributes.");
            Assert.That(methodConstructions, Is.Zero);
            Assert.That(innerObservations + outerObservations + finalizerObservations, Is.Zero);
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("message-frame", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.True);
            // Provider installation may legitimately construct attributes. Only inspection is measured.
            parameterConstructions = methodConstructions = 0;
            Assert.That(patch.Available, Is.True);
            patch.Enable();
            Assert.That(parameterConstructions + methodConstructions, Is.Zero);
            Assert.That(innerObservations + outerObservations + finalizerObservations, Is.Zero);
            NativeMessagesGui();
            Assert.That(Events, Is.EqualTo(new[] { "begin", "native", "success" }));
            Assert.That(innerObservations, Is.EqualTo(1));
            Assert.That(outerObservations, Is.EqualTo(1));
            Assert.That(finalizerObservations, Is.EqualTo(1));
            Assert.That(parameterConstructions + methodConstructions, Is.Zero);
            Assert.That(failures, Is.Empty);
        }
        finally
        {
            try { patch.Candidate.Rollback(); }
            finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); nativeFailure = null; Events.Clear(); }
        }
    }

    public static void ObserveNativeEvent([CountingParameter] List<string> __instance) => innerObservations++;
    [CountingMethod]
    public static void ObserveOuterCall() => outerObservations++;
    [CountingMethod]
    public static Exception? ObserveOuterFinalizer(Exception? __exception) { finalizerObservations++; return __exception; }
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class CountingParameterAttribute : Attribute
    {
        public CountingParameterAttribute() => parameterConstructions++;
    }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CountingMethodAttribute : Attribute
    {
        public CountingMethodAttribute() => methodConstructions++;
    }

    [TestCase(false)]
    [TestCase(true)]
    public void LaterForeignFactoriesAreNotResolvedByInspectionAndSurviveOwnedRollback(bool isFinalizer)
    {
        var target = typeof(MessageGuiScopePatchTests).GetMethod(nameof(NativeMessagesGui))!;
        var foreign = new HarmonyLib.Harmony("tests.messages.scope-late-factory");
        int begins = 0, completions = 0;
        var patch = new MessageGuiScopePatch(target, () => { begins++; return new object(); }, (_, _) => completions++, _ => {});
        try
        {
            Events.Clear(); nativeFailure = null; factoryCalls = 0;
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("message-frame", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.True);
            patch.Enable();
            var factory = new HarmonyMethod(typeof(MessageGuiScopePatchTests), isFinalizer ? nameof(FinalizerFactory) : nameof(PrefixFactory));
            foreign.Patch(target, prefix: isFinalizer ? null : factory, finalizer: isFinalizer ? factory : null);
            int afterComposition = factoryCalls;
            // Scope only needs prefix presence; unlike the outer envelope it does not sort prefixes.
            Assert.That(patch.Available, Is.EqualTo(!isFinalizer));
            Assert.That(factoryCalls, Is.EqualTo(afterComposition));
            Assert.DoesNotThrow(() => patch.Candidate.Rollback());
            int afterRollback = factoryCalls; // Provider Unpatch may legitimately compose remaining factories.
            Assert.That(HarmonyPublication.Inspect(target, snapshot =>
            {
                Assert.That(snapshot.Prefixes.Concat(snapshot.Finalizers).Any(registration => registration.Owner == OwnedHarmonyPatch.OwnerId), Is.False);
                Assert.That((isFinalizer ? snapshot.Finalizers : snapshot.Prefixes)
                    .Any(registration => registration.Owner == foreign.Id && registration.IsFactory), Is.True);
                return true;
            }), Is.True);
            Assert.That(factoryCalls, Is.EqualTo(afterRollback));
            nativeFailure = isFinalizer ? new InvalidOperationException("native") : null;
            Assert.DoesNotThrow(NativeMessagesGui);
            Assert.That(Events, Is.EqualTo(new[] { isFinalizer ? "native" : "foreign" }));
            Assert.That(begins + completions, Is.Zero);
        }
        finally
        {
            try { patch.Candidate.Rollback(); }
            finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); nativeFailure = null; Events.Clear(); }
        }
    }

    public static MethodInfo PrefixFactory(MethodBase target)
    {
        factoryCalls++;
        return typeof(MessageGuiScopePatchTests).GetMethod(nameof(SkipNative))!;
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ExistingOwnedSlotRejectsAdmissionWithoutRemovingItsRegistration(bool isFinalizer)
    {
        var target = typeof(MessageGuiScopePatchTests).GetMethod(nameof(NativeMessagesGui))!;
        var existing = new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId);
        var callback = typeof(MessageGuiScopePatchTests).GetMethod(isFinalizer ? nameof(SuppressNativeException) : nameof(SkipNative))!;
        var patch = new MessageGuiScopePatch(target, () => new object(), (_, _) => Assert.Fail("Rejected scope ran."), _ => {});
        try
        {
            existing.Patch(target, prefix: isFinalizer ? null : new HarmonyMethod(callback), finalizer: isFinalizer ? new HarmonyMethod(callback) : null);
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("message-frame", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.False);
            patch.Candidate.Rollback();
            Assert.That(HarmonyPublication.Inspect(target, snapshot =>
            {
                var owned = snapshot.Prefixes.Concat(snapshot.Finalizers).Where(registration => registration.Owner == existing.Id).ToArray();
                Assert.That(owned, Has.Length.EqualTo(1));
                Assert.That(owned[0].Method, Is.EqualTo(callback));
                return true;
            }), Is.True);
        }
        finally
        {
            try { patch.Candidate.Rollback(); }
            finally { existing.Unpatch(target, HarmonyPatchType.All, existing.Id); Events.Clear(); }
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NativeInvocationGetsOneScopeAndFinalizerPreservesItsException(bool fail)
    {
        object token = new();
        var failures = new List<Exception>();
        var patch = new MessageGuiScopePatch(typeof(MessageGuiScopePatchTests).GetMethod(nameof(NativeMessagesGui))!,
            () => { Events.Add("begin"); return token; },
            (received, successful) => { Assert.That(received, Is.SameAs(token)); Events.Add(successful ? "success" : "abort"); }, failures.Add);
        try
        {
            Events.Clear();
            nativeFailure = fail ? new InvalidOperationException("native") : null;
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("message-frame", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.True);
            patch.Enable();
            if (fail) Assert.That(Assert.Throws<InvalidOperationException>(NativeMessagesGui), Is.SameAs(nativeFailure));
            else NativeMessagesGui();
            Assert.That(Events, Is.EqualTo(new[] { "begin", "native", fail ? "abort" : "success" }));
            Assert.That(failures, Is.Empty);
        }
        finally { patch.Candidate.Rollback(); nativeFailure = null; Events.Clear(); }
    }

    [TestCase(true)]
    [TestCase(false)]
    public void OwnedObservationFailureDisablesTheScopeOnceAndLeavesNativeCallsUsable(bool failBegin)
    {
        int begins = 0, completions = 0, reports = 0;
        var patch = new MessageGuiScopePatch(typeof(MessageGuiScopePatchTests).GetMethod(nameof(NativeMessagesGui))!,
            () => { begins++; if (failBegin) throw new InvalidOperationException("begin"); return new object(); },
            (_, _) => { completions++; throw new InvalidOperationException("finish"); },
            _ => { reports++; throw new InvalidOperationException("diagnostic failed"); });
        try
        {
            Events.Clear(); nativeFailure = null;
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("message-frame", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.True);
            patch.Enable();
            Assert.DoesNotThrow(NativeMessagesGui);
            Assert.DoesNotThrow(NativeMessagesGui);
            Assert.That(begins, Is.EqualTo(1));
            Assert.That(completions, Is.EqualTo(failBegin ? 0 : 1));
            Assert.That(reports, Is.EqualTo(1));
            Assert.That(Events, Is.EqualTo(new[] { "native", "native" }));
        }
        finally { patch.Candidate.Rollback(); nativeFailure = null; Events.Clear(); }
    }

    [Test]
    public void OwnedCleanupFailureCannotReplaceTheOriginalNativeException()
    {
        var patch = new MessageGuiScopePatch(typeof(MessageGuiScopePatchTests).GetMethod(nameof(NativeMessagesGui))!,
            () => new object(), (_, _) => throw new ApplicationException("owned"), _ => {});
        try
        {
            Events.Clear(); nativeFailure = new InvalidOperationException("native");
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("message-frame", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.True);
            patch.Enable();
            Assert.That(Assert.Throws<InvalidOperationException>(NativeMessagesGui), Is.SameAs(nativeFailure));
        }
        finally { patch.Candidate.Rollback(); nativeFailure = null; Events.Clear(); }
    }

    [Test]
    public void ForeignSkipOriginalAbortsAnEmptyScopeAndSurvivesOwnedRollback()
    {
        var foreign = new HarmonyLib.Harmony("tests.messages.skip-original");
        var target = typeof(MessageGuiScopePatchTests).GetMethod(nameof(NativeMessagesGui))!;
        var patch = new MessageGuiScopePatch(target, () => { Events.Add("begin"); return new object(); },
            (_, success) => Events.Add(success ? "empty" : "abort"), _ => {});
        try
        {
            Events.Clear(); nativeFailure = null;
            foreign.Patch(target, prefix: new(typeof(MessageGuiScopePatchTests), nameof(SkipNative)));
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("message-frame", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.True);
            patch.Enable();
            NativeMessagesGui();
            Assert.That(Events, Is.EqualTo(new[] { "begin", "foreign", "abort" }));
            patch.Candidate.Rollback();
            Events.Clear(); NativeMessagesGui();
            Assert.That(Events, Is.EqualTo(new[] { "foreign" }));
        }
        finally { patch.Candidate.Rollback(); foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); Events.Clear(); }
    }

    [Test]
    public void LaterForeignExceptionSuppressionDoesNotCommitTheAbortedOwnedPass()
    {
        var foreign = new HarmonyLib.Harmony("tests.messages.suppress-exception");
        var target = typeof(MessageGuiScopePatchTests).GetMethod(nameof(NativeMessagesGui))!;
        bool? successful = null;
        var patch = new MessageGuiScopePatch(target, () => new object(), (_, success) => successful = success, _ => {});
        try
        {
            Events.Clear(); nativeFailure = new InvalidOperationException("native");
            foreign.Patch(target, finalizer: new(typeof(MessageGuiScopePatchTests), nameof(SuppressNativeException)));
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("message-frame", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.True);
            patch.Enable();
            Assert.DoesNotThrow(NativeMessagesGui);
            Assert.That(successful, Is.False);
        }
        finally { patch.Candidate.Rollback(); foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); nativeFailure = null; Events.Clear(); }
    }

    public static bool SkipNative() { Events.Add("foreign"); return false; }
    public static Exception? SuppressNativeException(Exception? __exception) => null;

    [TestCase(false)]
    [TestCase(true)]
    public void SuppressedOriginalCannotAuthorizeNonemptyObservedReplacementDraws(bool drawInPostfix)
    {
        var foreign = new HarmonyLib.Harmony("tests.messages.suppressed-replacement");
        var target = typeof(MessageGuiScopePatchTests).GetMethod(nameof(NativeMessagesGui))!;
        int observations = 0, commits = 0, completions = 0;
        var message = new MessageDrawPatchTests.NativeMessage();
        var draw = new MessageDrawPatch(typeof(MessageDrawPatchTests.NativeMessage).GetMethod(nameof(MessageDrawPatchTests.NativeMessage.Draw))!,
            typeof(MessageDrawPatchTests.NativeMessage).GetMethod(nameof(MessageDrawPatchTests.NativeMessage.CalculateRect))!,
            typeof(MessageDrawPatchTests.NativeWindow).GetMethod(nameof(MessageDrawPatchTests.NativeWindow.Register))!,
            typeof(MessageGuiScopePatchTests).GetMethod(nameof(ForwardObservedWindow))!);
        var scope = new MessageGuiScopePatch(target, () => new object(), (_, success) =>
        {
            completions++;
            if (success) { commits++; message.Window.Bounds = new(119, 37, 100, 25); }
        }, error => Assert.Fail(error.ToString()));
        try
        {
            Events.Clear(); nativeFailure = null;
            replacementMessage = message;
            observeWindow = () => { Assert.That(draw.Available, Is.True); observations++; };
            foreign.Patch(target,
                prefix: new(typeof(MessageGuiScopePatchTests), drawInPostfix ? nameof(SkipNative) : nameof(SkipNativeAfterDrawing)),
                postfix: drawInPostfix ? new(typeof(MessageGuiScopePatchTests), nameof(ReplacementPostfixDraw)) : null);
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("nonempty-message-frame", AdmissionPhase.Early,
                [draw.Candidate, scope.Candidate]), AdmissionPhase.Early), Is.True);
            scope.Enable();
            NativeMessagesGui();
            Assert.That(observations, Is.EqualTo(1), "The real admitted draw observer must have captured a nonempty pass.");
            Assert.That(completions, Is.EqualTo(1), "A skipped original must still clean up its scope.");
            Assert.That(commits, Is.Zero);
            Assert.That(message.Window.Bounds, Is.EqualTo(new MessageDrawPatchTests.NativeRect(19, 37, 100, 25)));
            Assert.That(Events, Does.Not.Contain("native"));
        }
        finally
        {
            try { scope.Candidate.Rollback(); }
            finally
            {
                try { draw.Candidate.Rollback(); }
                finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); observeWindow = null; replacementMessage = null; Events.Clear(); }
            }
        }
    }

    public static bool SkipNativeAfterDrawing() { replacementMessage!.Draw(19, 37); return false; }
    public static void ReplacementPostfixDraw() => replacementMessage!.Draw(19, 37);
    public static void ForwardObservedWindow(MessageDrawPatchTests.NativeWindow window, int id,
        MessageDrawPatchTests.NativeRect rectangle, Action callback, Action? outside, MessageDrawPatchTests.NativeMessage message)
    {
        window.Register(id, rectangle, callback, outside);
        observeWindow?.Invoke();
    }

    [Test]
    public void FinalizerForcedAheadOfTheScopeCannotMakeAnAbortedNativePassLookSuccessful()
    {
        var foreign = new HarmonyLib.Harmony("tests.messages.suppress-before-scope");
        var target = typeof(MessageGuiScopePatchTests).GetMethod(nameof(NativeMessagesGui))!;
        int begins = 0, completions = 0, reports = 0;
        var patch = new MessageGuiScopePatch(target, () => { begins++; return new object(); },
            (_, _) => completions++, _ => reports++);
        try
        {
            Events.Clear(); nativeFailure = new InvalidOperationException("native");
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("message-frame", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.True);
            patch.Enable();
            foreign.Patch(target, finalizer: new HarmonyMethod(typeof(MessageGuiScopePatchTests), nameof(SuppressNativeException))
                { before = [OwnedHarmonyPatch.OwnerId] });
            Assert.DoesNotThrow(NativeMessagesGui);
            Assert.DoesNotThrow(NativeMessagesGui);
            Assert.That(begins, Is.Zero);
            Assert.That(completions, Is.Zero);
            Assert.That(reports, Is.EqualTo(1));
            Assert.That(Events, Is.EqualTo(new[] { "native", "native" }));
        }
        finally { patch.Candidate.Rollback(); foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); nativeFailure = null; Events.Clear(); }
    }

    [Test]
    public void UnknownFinalizerFactoryIsRejectedBeforeOwnedInstallationReexecutesIt()
    {
        var foreign = new HarmonyLib.Harmony("tests.messages.finalizer-factory");
        var target = typeof(MessageGuiScopePatchTests).GetMethod(nameof(NativeMessagesGui))!;
        var patch = new MessageGuiScopePatch(target, () => new object(), (_, _) => {}, _ => {});
        try
        {
            factoryCalls = 0;
            foreign.Patch(target, finalizer: new(typeof(MessageGuiScopePatchTests), nameof(FinalizerFactory)));
            int baseline = factoryCalls;
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("message-frame", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.False);
            Assert.That(factoryCalls, Is.EqualTo(baseline));
        }
        finally { patch.Candidate.Rollback(); foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }

    public static MethodInfo FinalizerFactory(MethodBase target)
    {
        factoryCalls++;
        return typeof(MessageGuiScopePatchTests).GetMethod(nameof(SuppressNativeException))!;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void NativeMessagesGui()
    {
        Events.Add("native");
        duringNative?.Invoke();
        if (nativeFailure != null) throw nativeFailure;
    }
}
