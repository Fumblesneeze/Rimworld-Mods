using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.MessagePlacement;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.MessagePlacement;

[TestFixture, NonParallelizable]
public sealed class MessageWindowStackPatchTests
{
    private static readonly MethodInfo Target = typeof(Stack).GetMethod(nameof(Stack.OnGUI))!;
    private static readonly List<string> Events = new();
    private static bool skipNative;
    private static Exception? nativeFailure;
    private static int methodAttributes, parameterAttributes, factories;
    private MessageWindowStackPatch? patch;
    private HarmonyLib.Harmony foreign = null!;

    [SetUp]
    public void SetUp()
    {
        Events.Clear(); skipNative = false; nativeFailure = null;
        methodAttributes = parameterAttributes = factories = 0;
        foreign = new("tests.messages.window-entry");
    }

    [TearDown]
    public void TearDown()
    {
        try { patch?.Candidate.Rollback(); }
        finally { patch = null; foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); }
    }

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    public void ObservesActualReceiverBeforeConsumersAndPreservesNativeSkipAndException(bool skip, bool throws)
    {
        var stack = new Stack();
        var failures = new List<Exception>();
        patch = new(Target, (actual, current) =>
        {
            Assert.That(actual, Is.SameAs(stack));
            Assert.That(current(), Is.True);
            Events.Add("owned");
        }, failures.Add);
        foreign.Patch(Target, prefix: new HarmonyMethod(typeof(MessageWindowStackPatchTests), nameof(ForeignPrefix))
            { after = [OwnedHarmonyPatch.OwnerId] });
        Admit(); skipNative = skip;
        if (throws) nativeFailure = new InvalidOperationException("native window failed");
        if (throws) Assert.That(Assert.Throws<InvalidOperationException>(stack.OnGUI), Is.SameAs(nativeFailure));
        else stack.OnGUI();
        Assert.That(Events, Is.EqualTo(skip ? new[] { "owned", "foreign" } : new[] { "owned", "foreign", "native" }));
        Assert.That(stack.Passes, Is.EqualTo(skip ? 0 : 1));
        Assert.That(failures, Is.Empty);
    }

    [Test]
    public void RepatchBeforeOwnedObserverDisablesOnlyContributionAndKeepsForeignAndNativeEffects()
    {
        var failures = new List<Exception>();
        patch = new(Target, (_, _) => Events.Add("owned"), failures.Add);
        Admit();
        foreign.Patch(Target, prefix: new HarmonyMethod(typeof(MessageWindowStackPatchTests), nameof(ForeignPrefix))
            { priority = int.MaxValue, before = [OwnedHarmonyPatch.OwnerId] });
        var stack = new Stack();
        stack.OnGUI(); stack.OnGUI();
        Assert.That(Events, Is.EqualTo(new[] { "foreign", "native", "foreign", "native" }));
        Assert.That(stack.Passes, Is.EqualTo(2));
        Assert.That(patch.Available, Is.False);
        Assert.That(failures, Has.Count.EqualTo(1));
        patch.Candidate.Rollback();
        Events.Clear(); stack.OnGUI();
        Assert.That(Events, Is.EqualTo(new[] { "foreign", "native" }));
    }

    [Test]
    public void CallbackAndReporterFailuresCannotEscapeOrPreventNativeWindows()
    {
        var ownedFailure = new InvalidOperationException("owned callback failed");
        int reports = 0;
        Exception? reported = null;
        patch = new(Target, (_, _) => throw ownedFailure, failure =>
        {
            reports++; reported = failure; throw new Exception("reporter failed");
        });
        Admit();
        var stack = new Stack();
        Assert.DoesNotThrow(stack.OnGUI);
        Assert.DoesNotThrow(stack.OnGUI);
        Assert.That(stack.Passes, Is.EqualTo(2));
        Assert.That(reported, Is.SameAs(ownedFailure));
        Assert.That(reports, Is.EqualTo(1));
        Assert.That(patch.Available, Is.False);
    }

    [Test]
    public void InspectionDoesNotConstructForeignAttributesOrResolveUnconsumedFinalizerFactory()
    {
        foreign.Patch(Target, prefix: new HarmonyMethod(typeof(MessageWindowStackPatchTests), nameof(ForeignPrefix))
            { after = [OwnedHarmonyPatch.OwnerId] },
            finalizer: new HarmonyMethod(typeof(MessageWindowStackPatchTests), nameof(FinalizerFactory)));
        Assert.That(methodAttributes + parameterAttributes, Is.GreaterThan(0));
        Assert.That(factories, Is.GreaterThan(0));
        patch = new(Target, (_, _) => Events.Add("owned"), _ => Assert.Fail("Unexpected failure"));
        methodAttributes = parameterAttributes = factories = 0;
        Assert.That(patch.Candidate.Validate(), Is.Null);
        Assert.That(methodAttributes + parameterAttributes + factories, Is.Zero);
        Admit(); // Actual provider composition is allowed to consume attributes and factories.
        methodAttributes = parameterAttributes = factories = 0;
        Assert.That(patch.Available, Is.True);
        patch.Enable();
        var stack = new Stack(); stack.OnGUI();
        Assert.That(Events, Is.EqualTo(new[] { "owned", "foreign", "native", "finalizer" }));
        Assert.That(methodAttributes + parameterAttributes + factories, Is.Zero);
        patch.Candidate.Rollback();
        Events.Clear(); stack.OnGUI();
        Assert.That(Events, Is.EqualTo(new[] { "foreign", "native", "finalizer" }));
    }

    private void Admit()
    {
        var warnings = new List<string>();
        Assert.That(new AdmissionCoordinator(warnings.Add).Apply(new("windows", AdmissionPhase.Early,
            new[] { patch!.Candidate }), AdmissionPhase.Early), Is.True, string.Join("; ", warnings));
        patch.Enable();
    }

    [CountingMethod]
    public static bool ForeignPrefix([CountingParameter] Stack __instance) { Events.Add("foreign"); return !skipNative; }
    public static MethodInfo FinalizerFactory(MethodBase original) { factories++; return typeof(MessageWindowStackPatchTests).GetMethod(nameof(ForeignFinalizer))!; }
    public static void ForeignFinalizer() => Events.Add("finalizer");
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CountingMethodAttribute : Attribute { public CountingMethodAttribute() => methodAttributes++; }
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class CountingParameterAttribute : Attribute { public CountingParameterAttribute() => parameterAttributes++; }
    public sealed class Stack
    {
        public int Passes;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void OnGUI() { Passes++; Events.Add("native"); if (nativeFailure != null) throw nativeFailure; }
    }
}
