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
public sealed class MessageConsumptionPatchTests
{
    private MessageDrawPatch draw = null!;
    private MessageConsumptionPatch? consumption;
    private NativeMessage message = null!;
    private static int queryCalls;
    private static NativeRect queryInput;
    private static Exception? nativeFailure;
    private static int foreignCalls;
    private static int factoryCalls;
    private static Action? guiBody;
    private static int parameterConstructions, methodConstructions, innerObservations, outerObservations;

    [TestCase(false)]
    [TestCase(true)]
    public void AdmissionAndEntryInspectionDoNotConstructForeignAttributes(bool collision)
    {
        MethodInfo target = collision ? typeof(MessageConsumptionPatchTests).GetMethod(nameof(Collides))! : draw.Shape!.Callback;
        var foreign = new HarmonyLib.Harmony("tests.messages.consumer-inspection-attributes");
        int invalidations = 0;
        bool invalidatedBeforeNativeBody = false;
        var failures = new List<Exception>();
        consumption = Create(() =>
        {
            invalidations++;
            invalidatedBeforeNativeBody = queryCalls == 0 && message.Window.CallbackCalls == 0;
        }, failures.Add);
        try
        {
            parameterConstructions = methodConstructions = innerObservations = outerObservations = 0;
            var observer = new HarmonyMethod(typeof(MessageConsumptionPatchTests), collision ? nameof(ObserveAlpha) : nameof(ObserveIncrement))
            { innerTarget = collision ? InnerTarget.Constant(0.75f) : InnerTarget.Constant(1) };
            foreign.CreateProcessor(target).AddInnerPostfix(observer).Patch();
            foreign.Patch(target, prefix: new HarmonyMethod(typeof(MessageConsumptionPatchTests), nameof(ObserveConsumerEntry))
                { after = [OwnedHarmonyPatch.OwnerId] });
            Assert.That(parameterConstructions, Is.GreaterThan(0), "Normal provider composition consumes the parameter attribute.");
            Assert.That(methodConstructions, Is.GreaterThan(0), "Normal provider registration consumes the method attribute.");
            parameterConstructions = methodConstructions = 0;

            Assert.That(consumption.Candidate.Validate(), Is.Null);
            Assert.That(parameterConstructions, Is.Zero, "Owned inspection must not construct foreign parameter attributes.");
            Assert.That(methodConstructions, Is.Zero);
            Assert.That(innerObservations + outerObservations, Is.Zero);
            Assert.That(Admit(consumption.Candidate), Is.True);
            // Normal provider installation may construct attributes; inspection must not.
            parameterConstructions = methodConstructions = 0;
            Assert.That(consumption.Available, Is.True);
            consumption.Enable();
            Assert.That(parameterConstructions + methodConstructions, Is.Zero);
            Assert.That(innerObservations + outerObservations, Is.Zero);

            Action callback = message.Window.Callback!;
            if (collision)
            {
                Assert.That(Collides(message.LastBounds, out float alpha), Is.True);
                Assert.That(alpha, Is.EqualTo(0.75f));
                Assert.That(queryInput, Is.EqualTo(message.LastBounds));
            }
            else callback();
            Assert.That(invalidations, Is.EqualTo(1));
            Assert.That(invalidatedBeforeNativeBody, Is.True);
            Assert.That(queryCalls, Is.EqualTo(collision ? 1 : 0));
            Assert.That(message.Window.CallbackCalls, Is.EqualTo(collision ? 0 : 1));
            Assert.That(message.Window.Callback, Is.SameAs(callback));
            Assert.That(message.Window.Bounds, Is.EqualTo(message.LastBounds));
            Assert.That(innerObservations, Is.EqualTo(1));
            Assert.That(outerObservations, Is.EqualTo(1));
            Assert.That(parameterConstructions + methodConstructions, Is.Zero);
            Assert.That(failures, Is.Empty);
        }
        finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }

    public static void ObserveAlpha([CountingParameter] float __result) => innerObservations++;
    public static void ObserveIncrement([CountingParameter] int __result) => innerObservations++;
    [CountingMethod]
    public static void ObserveConsumerEntry() => outerObservations++;
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

    [SetUp]
    public void PrepareStructurallyResolvedNativeCallback()
    {
        queryCalls = 0; nativeFailure = null; foreignCalls = 0; factoryCalls = 0;
        draw = new(typeof(NativeMessage).GetMethod(nameof(NativeMessage.Draw))!,
            typeof(NativeMessage).GetMethod(nameof(NativeMessage.CalculateRect))!,
            typeof(NativeWindow).GetMethod(nameof(NativeWindow.Register))!,
            typeof(MessageConsumptionPatchTests).GetMethod(nameof(Forward))!);
        try
        {
            Assert.That(Admit(draw.Candidate), Is.True);
            message = new NativeMessage();
            message.Draw(19, 37);
        }
        catch { draw.Candidate.Rollback(); throw; }
    }
    [TearDown]
    public void RemoveOnlyAcquiredPatches()
    {
        try { consumption?.Candidate.Rollback(); }
        finally { consumption = null; nativeFailure = null; guiBody = null; draw.Candidate.Rollback(); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NativeConsumerEntryInvalidatesBeforeItsBodyWithoutChangingNativeBehavior(bool collision)
    {
        int invalidations = 0;
        consumption = Create(() =>
        {
            Assert.That(queryCalls, Is.Zero);
            Assert.That(message.Window.CallbackCalls, Is.Zero);
            invalidations++;
        });
        Assert.That(Admit(consumption.Candidate), Is.True);
        consumption.Enable();
        Action callback = message.Window.Callback!;
        if (collision)
        {
            Assert.That(Collides(message.LastBounds, out float alpha), Is.True);
            Assert.That(alpha, Is.EqualTo(0.75f));
            Assert.That(queryCalls, Is.EqualTo(1));
            Assert.That(queryInput, Is.EqualTo(message.LastBounds));
        }
        else
        {
            callback();
            Assert.That(message.Window.CallbackCalls, Is.EqualTo(1));
            Assert.That(message.Window.Callback, Is.SameAs(callback));
            Assert.That(message.Window.Bounds, Is.EqualTo(message.LastBounds));
        }
        Assert.That(invalidations, Is.EqualTo(1));
        Assert.That(consumption.Available, Is.True);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ObserverFailureRevokesAuthorityOnceAndPreservesNativeResultOrException(bool throwsNative)
    {
        int invalidations = 0, reports = 0;
        consumption = Create(() => { invalidations++; throw new ApplicationException("owned observation"); },
            _ => { reports++; throw new ApplicationException("owned diagnostic"); });
        Assert.That(Admit(consumption.Candidate), Is.True);
        consumption.Enable();
        nativeFailure = throwsNative ? new InvalidOperationException("native collision") : null;
        for (int call = 0; call < 2; call++)
        {
            if (throwsNative) Assert.That(Assert.Throws<InvalidOperationException>(() => Collides(message.LastBounds, out _)), Is.SameAs(nativeFailure));
            else Assert.That(Collides(message.LastBounds, out _), Is.True);
        }
        Assert.That(queryCalls, Is.EqualTo(2));
        Assert.That(invalidations, Is.EqualTo(1));
        Assert.That(reports, Is.EqualTo(1));
        Assert.That(consumption.Available, Is.False);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ForeignPrefixForcedBeforeObservationRevokesAuthorityWithoutChangingNativeExecution(bool collision)
    {
        int reports = 0;
        consumption = Create(() => {}, _ => reports++);
        var foreign = new HarmonyLib.Harmony("tests.messages.early-consumer-prefix");
        MethodInfo target = collision ? typeof(MessageConsumptionPatchTests).GetMethod(nameof(Collides))! : draw.Shape!.Callback;
        try
        {
            Assert.That(Admit(consumption.Candidate), Is.True);
            consumption.Enable();
            foreign.Patch(target, prefix: new HarmonyMethod(typeof(MessageConsumptionPatchTests), nameof(ForeignPrefix))
                { before = [OwnedHarmonyPatch.OwnerId] });
            Assert.That(consumption.Available, Is.False);
            if (collision) Assert.That(Collides(message.LastBounds, out _), Is.True);
            else message.Window.Callback!();
            Assert.That(queryCalls + message.Window.CallbackCalls, Is.EqualTo(1));
            Assert.That(foreignCalls, Is.EqualTo(1));
            Assert.That(reports, Is.EqualTo(1));
            consumption.Candidate.Rollback();
            if (collision) Collides(message.LastBounds, out _);
            else message.Window.Callback!();
            Assert.That(foreignCalls, Is.EqualTo(2));
        }
        finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }
    public static void ForeignPrefix() => foreignCalls++;

    [TestCase(false)]
    [TestCase(true)]
    public void PrefixFactoryIsRejectedBeforeOwnedInstallationCanExecuteItAgain(bool collision)
    {
        consumption = Create(() => {});
        var foreign = new HarmonyLib.Harmony("tests.messages.consumer-factory");
        MethodInfo target = collision ? typeof(MessageConsumptionPatchTests).GetMethod(nameof(Collides))! : draw.Shape!.Callback;
        try
        {
            foreign.Patch(target, prefix: new(typeof(MessageConsumptionPatchTests), nameof(ForeignPrefixFactory)));
            int before = factoryCalls;
            Assert.That(before, Is.GreaterThan(0));
            Assert.That(Admit(consumption.Candidate), Is.False);
            Assert.That(factoryCalls, Is.EqualTo(before));
            Assert.That(HarmonyPublication.Inspect(target, snapshot =>
            {
                Assert.That(snapshot.Prefixes.Any(patch => patch.Owner == OwnedHarmonyPatch.OwnerId), Is.False);
                return true;
            }), Is.True);
            if (collision) Assert.That(Collides(message.LastBounds, out _), Is.True);
            else message.Window.Callback!();
            Assert.That(foreignCalls, Is.EqualTo(1));
        }
        finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }
    public static MethodInfo ForeignPrefixFactory(MethodBase original)
    {
        factoryCalls++;
        return typeof(MessageConsumptionPatchTests).GetMethod(nameof(ForeignPrefix))!;
    }

    [TestCase(false)]
    [TestCase(true)]
    public void LatePrefixFactoryRevokesObservationButSurvivesBothOwnedReceiptRollbacks(bool collision)
    {
        int invalidations = 0;
        consumption = Create(() => invalidations++);
        var foreign = new HarmonyLib.Harmony("tests.messages.consumer-late-factory");
        var query = typeof(MessageConsumptionPatchTests).GetMethod(nameof(Collides))!;
        var callback = draw.Shape!.Callback;
        var target = collision ? query : callback;
        try
        {
            Assert.That(Admit(consumption.Candidate), Is.True);
            consumption.Enable();
            foreign.Patch(target, prefix: new HarmonyMethod(typeof(MessageConsumptionPatchTests), nameof(ForeignPrefixFactory)));
            int afterComposition = factoryCalls;
            Assert.That(afterComposition, Is.GreaterThan(0));
            Assert.That(consumption.Available, Is.False);
            Assert.That(factoryCalls, Is.EqualTo(afterComposition), "Inspection must not resolve the foreign factory.");
            Assert.DoesNotThrow(() => consumption.Candidate.Rollback());
            int afterRollback = factoryCalls; // Provider Unpatch may compose the retained foreign patch.
            foreach (var consumerTarget in new[] { callback, query })
                Assert.That(HarmonyPublication.Inspect(consumerTarget, snapshot =>
                {
                    Assert.That(snapshot.Prefixes.Any(patch => patch.Owner == OwnedHarmonyPatch.OwnerId), Is.False);
                    if (consumerTarget == target)
                        Assert.That(snapshot.Prefixes.Any(patch => patch.Owner == foreign.Id && patch.IsFactory), Is.True);
                    return true;
                }), Is.True);
            Assert.That(factoryCalls, Is.EqualTo(afterRollback));
            message.Window.Callback!();
            Assert.That(Collides(message.LastBounds, out float alpha), Is.True);
            Assert.That(alpha, Is.EqualTo(0.75f));
            Assert.That(queryCalls, Is.EqualTo(1));
            Assert.That(message.Window.CallbackCalls, Is.EqualTo(1));
            Assert.That(foreignCalls, Is.EqualTo(1));
            Assert.That(invalidations, Is.Zero);
        }
        finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }

    [Test]
    public void CompatibleSkippingPrefixKeepsItsResultAndSurvivesOwnedRollback()
    {
        int invalidations = 0;
        consumption = Create(() => { Assert.That(foreignCalls, Is.Zero); invalidations++; });
        var foreign = new HarmonyLib.Harmony("tests.messages.compatible-consumer-skip");
        MethodInfo target = typeof(MessageConsumptionPatchTests).GetMethod(nameof(Collides))!;
        try
        {
            foreign.Patch(target, prefix: new(typeof(MessageConsumptionPatchTests), nameof(SkipQuery)));
            Assert.That(Admit(consumption.Candidate), Is.True);
            consumption.Enable();
            Assert.That(Collides(message.LastBounds, out float alpha), Is.False);
            Assert.That(alpha, Is.EqualTo(0.25f));
            Assert.That(invalidations, Is.EqualTo(1));
            Assert.That(queryCalls, Is.Zero);
            consumption.Candidate.Rollback();
            Assert.That(Collides(message.LastBounds, out alpha), Is.False);
            Assert.That(alpha, Is.EqualTo(0.25f));
            Assert.That(foreignCalls, Is.EqualTo(2));
            Assert.That(invalidations, Is.EqualTo(1));
        }
        finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }
    public static bool SkipQuery(ref bool __result, ref float alpha)
    {
        foreignCalls++; __result = false; alpha = 0.25f; return false;
    }

    [Test]
    public void ConsumerObservationIsDormantUntilEnabledAndStopsAfterRollback()
    {
        int invalidations = 0;
        consumption = Create(() => invalidations++);
        Assert.That(Admit(consumption.Candidate), Is.True);
        message.Window.Callback!();
        Assert.That(invalidations, Is.Zero);
        consumption.Enable();
        message.Window.Callback!();
        Assert.That(invalidations, Is.EqualTo(1));
        consumption.Candidate.Rollback();
        message.Window.Callback!();
        Assert.That(invalidations, Is.EqualTo(1));
        Assert.That(message.Window.CallbackCalls, Is.EqualTo(3));
        Assert.That(consumption.Available, Is.False);
    }

    [TestCase(nameof(WrongRectangleQuery))]
    [TestCase(nameof(RefAlphaQuery))]
    public void IncompatibleCollisionCallableIsRejectedWithoutAcquiringConsumerPrefixes(string method)
    {
        var target = typeof(MessageConsumptionPatchTests).GetMethod(method)!;
        consumption = new(draw.Shape!, target, () => {}, _ => {});
        Assert.That(Admit(consumption.Candidate), Is.False);
        Assert.That(HarmonyLib.Harmony.GetPatchInfo(draw.Shape!.Callback)?.Prefixes.Any(patch => patch.owner == OwnedHarmonyPatch.OwnerId) ?? false, Is.False);
        Assert.That(HarmonyLib.Harmony.GetPatchInfo(target)?.Prefixes.Any(patch => patch.owner == OwnedHarmonyPatch.OwnerId) ?? false, Is.False);
    }
    public static bool WrongRectangleQuery(int rectangle, out float alpha) { alpha = 0; return false; }
    public static bool RefAlphaQuery(NativeRect rectangle, ref float alpha) => false;

    [TestCase(false, 0)]
    [TestCase(true, 0)]
    [TestCase(false, 1)]
    [TestCase(true, 1)]
    [TestCase(false, 2)]
    [TestCase(true, 2)]
    public void ActualPassCompositionVetoesEarlyConsumptionAndLeavesLaterPassesUsable(bool collision, int timing)
    {
        var pass = new MessageGuiPass();
        int writes = 0;
        bool? completed = null;
        consumption = Create(pass.Invalidate);
        var scope = new MessageGuiScopePatch(typeof(MessageConsumptionPatchTests).GetMethod(nameof(NativeMessagesGui))!,
            () => pass.Begin(), (token, successful) => completed = pass.Complete((MessageGuiPass.Token)token,
                successful && consumption.Available, 1000, 800, null, new(10, 0)), _ => {});
        void Consume()
        {
            if (collision) Assert.That(Collides(message.LastBounds, out _), Is.True);
            else message.Window.Callback!();
        }
        void Observe() => pass.Observe(message, new(new(19, 37, 100, 25), _ => writes++, () => {}));
        try
        {
            Assert.That(Admit(consumption.Candidate), Is.True);
            Assert.That(Admit(scope.Candidate), Is.True);
            consumption.Enable(); scope.Enable();
            guiBody = () =>
            {
                if (timing == 0) Consume(); // Veto before any surface has been observed.
                Observe();
                if (timing == 1) Consume();
            };
            NativeMessagesGui();
            if (timing == 2) Consume(); // Native window/bar consumers normally run after completion.
            Assert.That(completed, Is.EqualTo(timing == 2));
            Assert.That(writes, Is.EqualTo(timing == 2 ? 1 : 0));
            Assert.That(queryCalls + message.Window.CallbackCalls, Is.EqualTo(1));
            guiBody = Observe;
            NativeMessagesGui();
            Assert.That(completed, Is.True);
            Assert.That(writes, Is.EqualTo(timing == 2 ? 2 : 1));
        }
        finally { scope.Candidate.Rollback(); guiBody = null; }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static void NativeMessagesGui() => guiBody!();

    private MessageConsumptionPatch Create(Action invalidate, Action<Exception>? reportFailure = null) =>
        new(draw.Shape!, typeof(MessageConsumptionPatchTests).GetMethod(nameof(Collides))!, invalidate, reportFailure ?? (_ => {}));
    private static bool Admit(PatchCandidate candidate) => new AdmissionCoordinator(_ => {}).Apply(
        new("consumer-test", AdmissionPhase.Early, [candidate]), AdmissionPhase.Early);

    public static void Forward(NativeWindow window, int id, NativeRect rectangle, Action callback, Action? outside, NativeMessage message) =>
        window.Register(id, rectangle, callback, outside);
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Collides(NativeRect rectangle, out float alpha)
    {
        queryCalls++;
        queryInput = rectangle;
        if (nativeFailure != null) throw nativeFailure;
        alpha = 0.75f;
        return true;
    }
}
