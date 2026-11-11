using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.MessagePlacement;
using Megamod.Patching;
using NUnit.Framework;
using NativeMessage = Megamod.Harmony.Tests.Features.MessagePlacement.MessageDrawPatchTests.NativeMessage;
using NativeWindow = Megamod.Harmony.Tests.Features.MessagePlacement.MessageDrawPatchTests.NativeWindow;
using NativeRect = Megamod.Harmony.Tests.Features.MessagePlacement.MessageDrawPatchTests.NativeRect;

namespace Megamod.Harmony.Tests.Features.MessagePlacement;

[TestFixture, NonParallelizable]
public sealed class MessageGuiEventPatchTests
{
    private static Action? beforeMessages, collect, preparedBarrier, afterMessages;
    private MessageDrawPatch draw = null!;
    private MessageGuiEventPatch envelope = null!;
    private MessageConsumptionPatch consumption = null!;
    private MessageGuiScopePatch messages = null!;
    private MessageGuiPass pass = null!;
    private NativeMessage message = null!;
    private NativeRoot currentRoot = null!;
    private readonly List<string> warnings = new();
    private readonly List<Exception> failures = new();
    private int writes;
    private Func<object, bool> context = null!;
    private static int nativeCalls;
    private static int factoryCalls;
    private static int parameterConstructions, methodConstructions, innerObservations, outerObservations;

    [Test]
    public void AdmissionInspectionDoesNotConstructInnerParameterAttributes()
    {
        currentRoot = new EntryRoot();
        var foreign = new HarmonyLib.Harmony("tests.messages.outer-inspection-attributes");
        var target = typeof(EntryRoot).GetMethod(nameof(EntryRoot.OnGUI))!;
        parameterConstructions = methodConstructions = innerObservations = outerObservations = 0;
        try
        {
            foreign.CreateProcessor(target).AddInnerPostfix(new HarmonyMethod(typeof(MessageGuiEventPatchTests), nameof(ObserveBaseCall))
                { innerMethod = new InnerMethod(typeof(NativeRoot).GetMethod(nameof(NativeRoot.OnGUI))!) }).Patch();
            foreign.Patch(target, prefix: new HarmonyMethod(typeof(MessageGuiEventPatchTests), nameof(ObserveOuterCall))
                { after = [OwnedHarmonyPatch.OwnerId] });
            Assert.That(parameterConstructions, Is.GreaterThan(0), "Normal provider composition consumes the parameter attribute.");
            Assert.That(methodConstructions, Is.GreaterThan(0), "Normal provider registration consumes the method attribute.");
            parameterConstructions = methodConstructions = 0;
            Assert.That(envelope.Candidate.Validate(), Is.Null);
            Assert.That(parameterConstructions, Is.Zero, "Owned admission inspection must read metadata without constructing foreign attributes.");
            Assert.That(methodConstructions, Is.Zero);
            Assert.That(innerObservations, Is.Zero);
            Assert.That(outerObservations, Is.Zero);
            Start(); // Normal provider Patch may construct attributes; it is not inspection.
            parameterConstructions = methodConstructions = 0;
            Assert.That(envelope.Available, Is.True);
            Assert.That(parameterConstructions, Is.Zero);
            Assert.That(methodConstructions, Is.Zero);
            Assert.That(innerObservations, Is.Zero);
            Assert.That(outerObservations, Is.Zero);
            currentRoot.OnGUI();
            Assert.That(writes, Is.EqualTo(1));
            Assert.That(nativeCalls, Is.EqualTo(1));
            Assert.That(innerObservations, Is.EqualTo(1));
            Assert.That(outerObservations, Is.EqualTo(1));
            Assert.That(parameterConstructions, Is.Zero);
            Assert.That(methodConstructions, Is.Zero);
            Assert.That(failures, Is.Empty);
        }
        finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }
    public static void ObserveBaseCall([CountingParameter] NativeRoot __instance) => innerObservations++;
    [CountingMethod]
    public static void ObserveOuterCall() => outerObservations++;
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
    public void OwnedRollbackSurvivesALaterForeignFactoryAndLeavesItsPatchInstalled(bool finalizer)
    {
        currentRoot = new EntryRoot();
        Start();
        var foreign = new HarmonyLib.Harmony("tests.messages.outer-late-factory");
        var target = typeof(EntryRoot).GetMethod(nameof(EntryRoot.OnGUI))!;
        try
        {
            var factory = new HarmonyMethod(typeof(MessageGuiEventPatchTests), finalizer ? nameof(FinalizerFactory) : nameof(PrefixFactory));
            foreign.Patch(target, prefix: finalizer ? null : factory, finalizer: finalizer ? factory : null);
            int afterComposition = factoryCalls;
            Assert.That(envelope.Available, Is.False);
            Assert.That(factoryCalls, Is.EqualTo(afterComposition), "Inspection cannot resolve the foreign factory.");
            Assert.DoesNotThrow(() => envelope.Candidate.Rollback());
            int afterRollback = factoryCalls; // Provider Unpatch may legitimately rebuild the foreign patch.
            Assert.That(HarmonyPublication.Inspect(target, snapshot =>
            {
                var role = finalizer ? snapshot.Finalizers : snapshot.Prefixes;
                Assert.That(role.Any(patch => patch.Owner == OwnedHarmonyPatch.OwnerId), Is.False);
                Assert.That(role.Any(patch => patch.Owner == foreign.Id && patch.IsFactory), Is.True);
                return true;
            }), Is.True);
            Assert.That(factoryCalls, Is.EqualTo(afterRollback));
            currentRoot.OnGUI();
            Assert.That(nativeCalls, Is.EqualTo(1));
            Assert.That(writes, Is.Zero);
        }
        finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }

    [SetUp]
    public void PrepareDirectOwnedComposition()
    {
        writes = 0; nativeCalls = 0; factoryCalls = 0; warnings.Clear(); failures.Clear();
        beforeMessages = null; afterMessages = null;
        pass = new();
        var timing = new MessageGuiEvent(pass, warnings.Add);
        MessageGuiEvent.Collection? pending = null;
        context = root => ReferenceEquals(root, currentRoot);
        draw = new(typeof(NativeMessage).GetMethod(nameof(NativeMessage.Draw))!,
            typeof(NativeMessage).GetMethod(nameof(NativeMessage.CalculateRect))!,
            typeof(NativeWindow).GetMethod(nameof(NativeWindow.Register))!,
            typeof(MessageGuiEventPatchTests).GetMethod(nameof(Forward))!);
        try
        {
            Assert.That(Admit(draw.Candidate), Is.True);
            message = new(); message.Draw(19, 37);
            envelope = new(typeof(NativeRoot).GetMethod(nameof(NativeRoot.OnGUI))!,
                typeof(EntryRoot).GetMethod(nameof(EntryRoot.OnGUI))!, typeof(PlayRoot).GetMethod(nameof(PlayRoot.OnGUI))!,
                timing, root => context(root), failures.Add);
            consumption = new(draw.Shape!, typeof(MessageGuiEventPatchTests).GetMethod(nameof(Collides))!, timing.NotifyConsumption, failures.Add);
            messages = new(typeof(MessageGuiEventPatchTests).GetMethod(nameof(MessagesGui))!, () => pending = timing.BeginMessages(currentRoot),
                (token, succeeded) => timing.CompleteMessages((MessageGuiEvent.Collection)token, succeeded &&
                    envelope.Available && consumption.Available), failures.Add);
            preparedBarrier = () =>
            {
                if (pending != null) timing.CommitPrepared(pending, 1000, 800, null, new(10, 0));
            };
            collect = () => pass.Observe(message, new(new(19, 37, 100, 25), _ => writes++, () => writes--));
        }
        catch { draw.Candidate.Rollback(); throw; }
    }
    [TearDown]
    public void CleanupOwnedReceipts()
    {
        try { messages?.Candidate.Rollback(); }
        finally
        {
            try { consumption?.Candidate.Rollback(); }
            finally
            {
                try { envelope?.Candidate.Rollback(); }
                finally { draw.Candidate.Rollback(); beforeMessages = collect = preparedBarrier = afterMessages = null; }
            }
        }
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void ExactNativeRootForwardingRecordsConsumerBeforeCollectionAndAllowsNormalLaterConsumption(bool play, bool early)
    {
        currentRoot = play ? new PlayRoot() : new EntryRoot();
        Start();
        if (early) beforeMessages = () => Collides(default, out _);
        afterMessages = () => message.Window.Callback!();
        currentRoot.OnGUI();
        Assert.That(writes, Is.EqualTo(early ? 0 : 1));
        Assert.That(message.Window.CallbackCalls, Is.EqualTo(1));
        Assert.That(warnings.Count, Is.EqualTo(early ? 1 : 0));
        Assert.That(failures, Is.Empty);
        beforeMessages = null;
        currentRoot.OnGUI();
        Assert.That(writes, Is.EqualTo(early ? 1 : 2));
    }

    [TestCase("receiver")]
    [TestCase("predicate")]
    [TestCase("exception")]
    public void UnprovenOrFailingCurrentContextCannotAcquirePlacementAuthorityOrReplaceNativeExecution(string problem)
    {
        currentRoot = problem == "receiver" ? new ExtendedEntryRoot() : new EntryRoot();
        if (problem == "predicate") context = _ => false;
        if (problem == "exception") context = _ => throw new InvalidOperationException("context");
        Start();
        Assert.DoesNotThrow(currentRoot.OnGUI);
        Assert.DoesNotThrow(currentRoot.OnGUI);
        Assert.That(writes, Is.Zero);
        Assert.That(nativeCalls, Is.EqualTo(2));
        Assert.That(envelope.Available, Is.False);
        Assert.That(failures, Has.Count.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ForeignObserverOrderingThatEscapesTheEnvelopeCannotAuthorizeCommit(bool finalizer)
    {
        currentRoot = new EntryRoot();
        Start();
        var foreign = new HarmonyLib.Harmony("tests.messages.outer-order");
        var target = typeof(EntryRoot).GetMethod(nameof(EntryRoot.OnGUI))!;
        var patch = new HarmonyMethod(typeof(MessageGuiEventPatchTests), finalizer ? nameof(EarlyFinalizer) : nameof(EarlyConsumerPrefix))
            { before = [OwnedHarmonyPatch.OwnerId] };
        try
        {
            foreign.Patch(target, prefix: finalizer ? null : patch, finalizer: finalizer ? patch : null);
            Assert.That(envelope.Available, Is.False);
            currentRoot.OnGUI();
            Assert.That(writes, Is.Zero);
            Assert.That(nativeCalls, Is.EqualTo(1));
            Assert.That(failures, Has.Count.EqualTo(1));
        }
        finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }
    public static void EarlyConsumerPrefix() => Collides(default, out _);
    public static Exception? EarlyFinalizer(Exception? __exception) { MessagesGui(); return __exception; }

    [TestCase(false)]
    [TestCase(true)]
    public void ObserverFactoriesAreRejectedBeforeAnyOwnedReceiptReexecutesThem(bool finalizer)
    {
        currentRoot = new EntryRoot();
        var foreign = new HarmonyLib.Harmony("tests.messages.outer-factory");
        var target = typeof(EntryRoot).GetMethod(nameof(EntryRoot.OnGUI))!;
        var patch = new HarmonyMethod(typeof(MessageGuiEventPatchTests), finalizer ? nameof(FinalizerFactory) : nameof(PrefixFactory));
        try
        {
            foreign.Patch(target, prefix: finalizer ? null : patch, finalizer: finalizer ? patch : null);
            int before = factoryCalls;
            Assert.That(Admit(envelope.Candidate), Is.False);
            Assert.That(factoryCalls, Is.EqualTo(before));
            currentRoot.OnGUI();
            Assert.That(nativeCalls, Is.EqualTo(1));
        }
        finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }
    public static MethodInfo PrefixFactory(MethodBase method) { factoryCalls++; return typeof(MessageGuiEventPatchTests).GetMethod(nameof(EarlyConsumerPrefix))!; }
    public static MethodInfo FinalizerFactory(MethodBase method) { factoryCalls++; return typeof(MessageGuiEventPatchTests).GetMethod(nameof(EarlyFinalizer))!; }

    [TestCase(false)]
    [TestCase(true)]
    public void NativeExceptionCleansTheEnvelopeAndDoesNotUndoAnAlreadyCompletedPass(bool late)
    {
        currentRoot = new EntryRoot(); Start();
        var native = new InvalidOperationException("native GUI");
        if (late) afterMessages = () => throw native;
        else beforeMessages = () => throw native;
        Assert.That(Assert.Throws<InvalidOperationException>(currentRoot.OnGUI), Is.SameAs(native));
        Assert.That(writes, Is.EqualTo(late ? 1 : 0));
        beforeMessages = afterMessages = null;
        MessagesGui(); // No outer envelope; cannot use a token left by the failed invocation.
        Assert.That(writes, Is.EqualTo(late ? 1 : 0));
        currentRoot.OnGUI();
        Assert.That(writes, Is.EqualTo(late ? 2 : 1));
        Assert.That(failures, Is.Empty);
    }
    [TestCase(false)]
    [TestCase(true)]
    public void ReentrantCallerDeclinesFurtherCommitsAndPreservesCompletedNativeEffects(bool late)
    {
        currentRoot = new PlayRoot(); Start();
        if (late) afterMessages = () => { afterMessages = null; currentRoot.OnGUI(); };
        else beforeMessages = () => { beforeMessages = null; currentRoot.OnGUI(); };
        currentRoot.OnGUI();
        Assert.That(nativeCalls, Is.EqualTo(2));
        Assert.That(writes, Is.EqualTo(late ? 1 : 0));
        currentRoot.OnGUI();
        Assert.That(writes, Is.EqualTo(late ? 2 : 1));
        Assert.That(warnings, Has.Count.EqualTo(1));
    }
    [Test]
    public void CompatibleReplacementCallerRemainsAnEnvelopeWhileActualMessageOriginalMustRun()
    {
        currentRoot = new EntryRoot();
        var foreign = new HarmonyLib.Harmony("tests.messages.replacement-caller");
        var target = typeof(EntryRoot).GetMethod(nameof(EntryRoot.OnGUI))!;
        try
        {
            foreign.Patch(target, prefix: new(typeof(MessageGuiEventPatchTests), nameof(ReplacementCaller)));
            Start(); currentRoot.OnGUI();
            Assert.That(writes, Is.EqualTo(1));
            Assert.That(nativeCalls, Is.EqualTo(1));
            Assert.That(warnings, Is.Empty);
            envelope.Candidate.Rollback();
            currentRoot.OnGUI();
            Assert.That(nativeCalls, Is.EqualTo(2), "Owned cleanup retains the foreign caller.");
            Assert.That(writes, Is.EqualTo(1));
        }
        finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }
    public static bool ReplacementCaller(EntryRoot __instance) { __instance.ForwardBase(); return false; }
    private void Start()
    {
        Assert.That(Admit(envelope.Candidate, consumption.Candidate, messages.Candidate), Is.True);
        envelope.Enable(); consumption.Enable(); messages.Enable();
    }
    private static bool Admit(params PatchCandidate[] candidates) => new AdmissionCoordinator(_ => {}).Apply(
        new("event-test", AdmissionPhase.Early, candidates), AdmissionPhase.Early);
    public static void Forward(NativeWindow window, int id, NativeRect rectangle, Action callback, Action? outside, NativeMessage message) =>
        window.Register(id, rectangle, callback, outside);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Collides(NativeRect rectangle, out float alpha) { alpha = 0.75f; return true; }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void MessagesGui() => collect?.Invoke();
    public class NativeRoot
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual void OnGUI() { nativeCalls++; beforeMessages?.Invoke(); MessagesGui(); preparedBarrier?.Invoke(); }
    }
    public class EntryRoot : NativeRoot
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void OnGUI() { base.OnGUI(); afterMessages?.Invoke(); }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ForwardBase() => base.OnGUI();
    }
    public class PlayRoot : NativeRoot
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override void OnGUI() { base.OnGUI(); afterMessages?.Invoke(); }
    }
    public sealed class ExtendedEntryRoot : EntryRoot { }
}
