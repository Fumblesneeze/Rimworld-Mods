using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Patching;
using NUnit.Framework;
using HarmonyInstance = HarmonyLib.Harmony;

namespace Megamod.Harmony.Tests.Patching;

[TestFixture, NonParallelizable]
public sealed class HarmonyPublicationOuterRolesTests
{
    private static readonly MethodInfo Target = Method(nameof(Sample));
    private static int constructions, methodConstructions, prefixCalls, finalizerCalls, innerCalls, factoryCalls;
    private static readonly List<string> orderingCalls = new();
    private static HarmonyPublication.Operation[] observedBody = Array.Empty<HarmonyPublication.Operation>();
    private static MethodInfo Method(string name) => typeof(HarmonyPublicationOuterRolesTests).GetMethod(name)!;

    [Test]
    public void PublishedOuterRolesRetainMetadataWithoutExecutingAttributesOrCallbacks()
    {
        var foreign = new HarmonyInstance("tests.megamod.publication.outer-metadata");
        try
        {
            constructions = methodConstructions = prefixCalls = finalizerCalls = innerCalls = 0;
            foreign.CreateProcessor(Target)
                .AddPrefix(new HarmonyMethod(Method(nameof(ObservePrefix)))
                    { priority = 123, before = ["before-prefix"], after = ["after-prefix"], uncheckedReferenceBinding = true })
                .AddFinalizer(new HarmonyMethod(Method(nameof(ObserveFinalizer)))
                    { priority = 456, before = ["before-finalizer"], after = ["after-finalizer"] })
                .AddInnerPostfix(new HarmonyMethod(Method(nameof(ObserveInner)))
                    { innerMethod = new InnerMethod(Method(nameof(Allowed))) }).Patch();
            Assert.That(constructions, Is.GreaterThan(0), "Actual provider composition consumes the owned fixture attributes.");
            Assert.That(methodConstructions, Is.GreaterThan(0), "The MethodInfo HarmonyMethod constructor consumes method attributes.");
            constructions = methodConstructions = 0;

            Assert.That(HarmonyPublication.Inspect(Target, snapshot =>
            {
                Assert.That(snapshot.Version, Is.GreaterThan(0));
                Assert.That(snapshot.Transpilers, Is.Empty);
                Assert.That(snapshot.Prefixes, Has.Count.EqualTo(1));
                Assert.That(snapshot.Finalizers, Has.Count.EqualTo(1));
                AssertRegistration(snapshot.Prefixes[0], foreign.Id, nameof(ObservePrefix), 123, "before-prefix", "after-prefix", true);
                AssertRegistration(snapshot.Finalizers[0], foreign.Id, nameof(ObserveFinalizer), 456, "before-finalizer", "after-finalizer", false);
                return true;
            }), Is.True);
            Assert.That(constructions, Is.Zero);
            Assert.That(methodConstructions, Is.Zero);
            Assert.That(prefixCalls + finalizerCalls + innerCalls, Is.Zero);
            Assert.That(Sample(4), Is.EqualTo(5));
            Assert.That(new[] { prefixCalls, finalizerCalls, innerCalls }, Is.EqualTo(new[] { 1, 1, 1 }));
        }
        finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); }
    }

    [TestCase(1, 0)]
    [TestCase(-1, 1)]
    public void InnerProjectionUsesActualSelectorsWithoutExecutingMetadataAndSurvivesRepatch(int position, int selected)
    {
        var target = Method(nameof(SampleTwice));
        var foreign = new HarmonyInstance("tests.megamod.publication.inner-projection");
        try
        {
            constructions = methodConstructions = innerCalls = 0;
            foreign.CreateProcessor(target)
                .AddTranspiler(new HarmonyMethod(Method(nameof(CaptureInnerBody))) { priority = int.MinValue })
                .AddInnerPostfix(new HarmonyMethod(Method(nameof(ObserveInner)))
                { innerMethod = new InnerMethod(Method(nameof(Allowed)), position), uncheckedReferenceBinding = true }).Patch();
            Assert.That(constructions, Is.GreaterThan(0));
            constructions = methodConstructions = 0;
            var expected = observedBody.Select((operation, index) => (operation, index))
                .Where(pair => pair.operation.Operand is MethodInfo method && method == Method(nameof(Allowed))).Select(pair => pair.index).ToArray();
            Assert.That(expected, Has.Length.EqualTo(2));
            HarmonyPublication.Snapshot? retained = null;
            IReadOnlyList<HarmonyPublication.InnerCallback>? callbacks = null;
            Assert.That(HarmonyPublication.Inspect(target, snapshot =>
            {
                retained = snapshot;
                callbacks = snapshot.InnerCallbacks(observedBody);
                Assert.That(callbacks, Has.Count.EqualTo(1));
                var callback = callbacks[0];
                Assert.Multiple(() =>
                {
                    Assert.That(callback.Method, Is.EqualTo(Method(nameof(ObserveInner))));
                    Assert.That(callback.Role, Is.EqualTo(HarmonyPatchType.InnerPostfix));
                    Assert.That(callback.OuterBinding, Is.True);
                    Assert.That(callback.IsFactory, Is.False);
                    Assert.That(callback.HasMatches, Is.True);
                    Assert.That(callback.SelectedOperations, Is.EqualTo(new[] { expected[selected] }));
                    Assert.Throws<NotSupportedException>(() => ((IList<int>)callback.SelectedOperations)[0] = -1);
                    Assert.Throws<NotSupportedException>(() => ((IList<HarmonyPublication.InnerCallback>)callbacks)[0] = callback);
                    Assert.That(snapshot.FailureFor(observedBody, new HashSet<int>()), Is.Not.Null,
                        "The existing outer-binding refusal must remain unchanged.");
                });
                return true;
            }), Is.True);
            Assert.That(retained!.Identity.IsCurrent(), Is.True);
            Assert.That(constructions + methodConstructions + innerCalls, Is.Zero);
            Assert.That(SampleTwice(), Is.EqualTo(2));
            Assert.That(innerCalls, Is.EqualTo(1));
            foreign.Unpatch(target, HarmonyPatchType.InnerPostfix, foreign.Id);
            Assert.That(retained.Identity.IsCurrent(), Is.False);
            Assert.That(callbacks!.Single().SelectedOperations, Is.EqualTo(new[] { expected[selected] }));
            Assert.That(HarmonyPublication.Inspect(target, snapshot =>
            {
                Assert.That(snapshot.Version, Is.GreaterThan(retained.Version));
                Assert.That(snapshot.InnerCallbacks(observedBody), Is.Empty);
                return true;
            }), Is.True);
        }
        finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }

    public static IEnumerable<CodeInstruction> CaptureInnerBody(IEnumerable<CodeInstruction> instructions)
    {
        var body = instructions.ToList();
        observedBody = body.Select(instruction => new HarmonyPublication.Operation(instruction.opcode, instruction.operand)).ToArray();
        return body;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int SampleTwice() => (Allowed() ? 1 : 0) + (Allowed() ? 1 : 0);

    [TestCase(HarmonyPatchType.InnerPrefix)]
    [TestCase(HarmonyPatchType.InnerPostfix)]
    [TestCase(HarmonyPatchType.InnerFinalizer)]
    public void InnerRoleProjectionPreservesNoMatchEvidenceWithoutExecutingCallbacks(HarmonyPatchType role)
    {
        var foreign = new HarmonyInstance("tests.megamod.publication.inner-roles");
        try
        {
            constructions = methodConstructions = factoryCalls = innerCalls = 0;
            var patch = new HarmonyMethod(Method(nameof(ObserveNoArgument))) { innerMethod = new InnerMethod(Method(nameof(Allowed))) };
            var processor = foreign.CreateProcessor(Target)
                .AddTranspiler(new HarmonyMethod(Method(nameof(CaptureInnerBody))) { priority = int.MinValue });
            if (role == HarmonyPatchType.InnerPrefix) processor.AddInnerPrefix(patch);
            else if (role == HarmonyPatchType.InnerPostfix) processor.AddInnerPostfix(patch);
            else processor.AddInnerFinalizer(patch);
            processor.Patch();
            Assert.That(methodConstructions, Is.GreaterThan(0));
            factoryCalls = constructions = methodConstructions = 0;
            Assert.That(HarmonyPublication.Inspect(Target, snapshot =>
            {
                var callback = snapshot.InnerCallbacks(observedBody).Single();
                Assert.Multiple(() =>
                {
                    Assert.That(callback.Method, Is.EqualTo(Method(nameof(ObserveNoArgument))));
                    Assert.That(callback.Role, Is.EqualTo(role));
                    Assert.That(callback.IsFactory, Is.False);
                    Assert.That(callback.OuterBinding, Is.False);
                    Assert.That(callback.HasMatches, Is.True);
                    Assert.That(callback.SelectedOperations, Has.Count.EqualTo(1));
                    var unmatched = snapshot.InnerCallbacks(Array.Empty<HarmonyPublication.Operation>()).Single();
                    Assert.That(unmatched.HasMatches, Is.False);
                    Assert.That(unmatched.SelectedOperations, Is.Empty);
                    Assert.That(snapshot.FailureFor(Array.Empty<HarmonyPublication.Operation>(), new HashSet<int>()), Is.Not.Null);
                });
                return true;
            }), Is.True);
            Assert.That(factoryCalls + constructions + methodConstructions + innerCalls, Is.Zero);
            Assert.That(Sample(4), Is.EqualTo(5));
            Assert.That(innerCalls, Is.EqualTo(1));
        }
        finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); }
    }

    [TestCase(HarmonyPatchType.InnerPrefix)]
    [TestCase(HarmonyPatchType.InnerPostfix)]
    [TestCase(HarmonyPatchType.InnerFinalizer)]
    public void ProviderRejectsInnerFactoriesWithoutExecutionAndMetadataStillIdentifiesTheirShape(HarmonyPatchType role)
    {
        var foreign = new HarmonyInstance("tests.megamod.publication.rejected-inner-factory");
        try
        {
            factoryCalls = 0;
            var factory = Method(nameof(InnerFactory));
            var patch = new HarmonyMethod(factory) { innerMethod = new InnerMethod(Method(nameof(Allowed))) };
            var processor = foreign.CreateProcessor(Target);
            if (role == HarmonyPatchType.InnerPrefix) processor.AddInnerPrefix(patch);
            else if (role == HarmonyPatchType.InnerPostfix) processor.AddInnerPostfix(patch);
            else processor.AddInnerFinalizer(patch);
            Assert.That(() => processor.Patch(), Throws.ArgumentException.With.Message.Contains("cannot be a method factory"));
            Assert.That(factoryCalls, Is.Zero);
            var defensiveProjection = new HarmonyPublication.InnerCallback(factory, role, false, true, new[] { 1 });
            Assert.That(defensiveProjection.IsFactory, Is.True);
            Assert.That(factoryCalls, Is.Zero, "Defensive metadata recognition must not resolve a rejected factory.");
        }
        finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); }
    }

    private static void AssertRegistration(HarmonyPublication.OuterRegistration registration, string owner,
        string callback, int priority, string before, string after, bool uncheckedBinding)
    {
        Assert.Multiple(() =>
        {
            Assert.That(registration.Method, Is.EqualTo(Method(callback)));
            Assert.That(registration.Owner, Is.EqualTo(owner));
            Assert.That(registration.Index, Is.Zero);
            Assert.That(registration.Priority, Is.EqualTo(priority));
            Assert.That(registration.Before, Is.EqualTo(new[] { before }));
            Assert.That(registration.After, Is.EqualTo(new[] { after }));
            Assert.That(registration.Debug, Is.False);
            Assert.That(registration.UncheckedReferenceBinding, Is.EqualTo(uncheckedBinding));
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MutatingAnOrderingProjectionCannotChangeItsPublishedSnapshot(bool finalizer)
    {
        var foreign = new HarmonyInstance("tests.megamod.publication.outer-copy");
        MethodInfo callback = Method(finalizer ? nameof(ObserveFinalizer) : nameof(ObservePrefix));
        try
        {
            constructions = methodConstructions = 0;
            var patch = new HarmonyMethod(callback)
                { priority = 321, before = ["before"], after = ["after"], uncheckedReferenceBinding = true };
            if (finalizer) foreign.Patch(Target, finalizer: patch);
            else foreign.Patch(Target, prefix: patch);
            Assert.That(methodConstructions, Is.GreaterThan(0), "Actual registration must exercise the method attribute counter.");
            constructions = methodConstructions = 0;
            Assert.That(HarmonyPublication.Inspect(Target, snapshot =>
            {
                var role = finalizer ? snapshot.Finalizers : snapshot.Prefixes;
                var registration = role.Single();
                Assert.Throws<NotSupportedException>(() => ((IList<HarmonyPublication.OuterRegistration>)role)[0] = registration);
                Assert.Throws<NotSupportedException>(() => ((IList<string>)registration.Before)[0] = "changed");
                Assert.Throws<NotSupportedException>(() => ((IList<string>)registration.After)[0] = "changed");
                var projection = registration.ToOrderingPatch();
                projection.PatchMethod = Method(nameof(OtherCallback));
                projection.before[0] = "changed-before";
                projection.after[0] = "changed-after";
                var next = registration.ToOrderingPatch();
                Assert.Multiple(() =>
                {
                    Assert.That(next, Is.Not.SameAs(projection));
                    Assert.That(next.PatchMethod, Is.EqualTo(callback));
                    Assert.That(next.owner, Is.EqualTo(foreign.Id));
                    Assert.That(next.index, Is.EqualTo(registration.Index));
                    Assert.That(next.priority, Is.EqualTo(321));
                    Assert.That(next.before, Is.EqualTo(new[] { "before" }));
                    Assert.That(next.after, Is.EqualTo(new[] { "after" }));
                    Assert.That(next.uncheckedReferenceBinding, Is.True);
                    Assert.That(registration.Before, Is.EqualTo(new[] { "before" }));
                    Assert.That(registration.After, Is.EqualTo(new[] { "after" }));
                    Assert.That(PatchProcessor.GetSortedPatchMethods(Target, new[] { next }), Is.EqualTo(new[] { callback }));
                });
                return true;
            }), Is.True);
            Assert.That(constructions, Is.Zero, "Projection and sorting must not inspect instantiated attributes.");
            Assert.That(methodConstructions, Is.Zero, "Projection and sorting must not import method attributes.");
        }
        finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void FactoryRegistrationsCannotBeProjectedForSorting(bool finalizer)
    {
        var foreign = new HarmonyInstance("tests.megamod.publication.outer-factory");
        try
        {
            constructions = methodConstructions = factoryCalls = 0;
            var factory = new HarmonyMethod(Method(finalizer ? nameof(FinalizerFactory) : nameof(PrefixFactory)));
            if (finalizer) foreign.Patch(Target, finalizer: factory);
            else foreign.Patch(Target, prefix: factory);
            Assert.That(factoryCalls, Is.GreaterThan(0), "Only actual provider composition may execute this fixture factory.");
            Assert.That(methodConstructions, Is.GreaterThan(0), "Actual registration must exercise the method attribute counter.");
            constructions = methodConstructions = factoryCalls = 0;
            Assert.That(HarmonyPublication.Inspect(Target, snapshot =>
            {
                var registration = (finalizer ? snapshot.Finalizers : snapshot.Prefixes).Single();
                Assert.Multiple(() =>
                {
                    Assert.That(registration.IsFactory, Is.True);
                    Assert.Throws<InvalidOperationException>(() => registration.ToOrderingPatch());
                });
                return true;
            }), Is.True);
            Assert.That(factoryCalls, Is.Zero);
            Assert.That(constructions, Is.Zero);
            Assert.That(methodConstructions, Is.Zero);
        }
        finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void OrderingUsesOnePublishedGenerationAndRetainedRolesSurviveRebuild(bool finalizer)
    {
        var first = new HarmonyInstance("tests.megamod.publication.outer-order-first");
        var second = new HarmonyInstance("tests.megamod.publication.outer-order-second");
        MethodInfo firstMethod = Method(finalizer ? nameof(OrderFirstFinalizer) : nameof(OrderFirstPrefix));
        MethodInfo secondMethod = Method(finalizer ? nameof(OrderSecondFinalizer) : nameof(OrderSecondPrefix));
        try
        {
            // Explicit owner ordering must override the opposite numeric priority order.
            constructions = methodConstructions = 0;
            var firstPatch = new HarmonyMethod(firstMethod) { priority = 100, before = [second.Id] };
            var secondPatch = new HarmonyMethod(secondMethod) { priority = 800, after = [first.Id] };
            if (finalizer) { second.Patch(Target, finalizer: secondPatch); first.Patch(Target, finalizer: firstPatch); }
            else { second.Patch(Target, prefix: secondPatch); first.Patch(Target, prefix: firstPatch); }
            Assert.That(methodConstructions, Is.GreaterThan(0), "Actual registration must exercise the method attribute counter.");
            constructions = methodConstructions = 0;
            orderingCalls.Clear();
            HarmonyPublication.Snapshot? retained = null;
            Assert.That(HarmonyPublication.Inspect(Target, snapshot =>
            {
                retained = snapshot;
                var roles = finalizer ? snapshot.Finalizers : snapshot.Prefixes;
                Assert.That(PatchProcessor.GetSortedPatchMethods(Target, roles.Select(role => role.ToOrderingPatch()).ToArray()),
                    Is.EqualTo(new[] { firstMethod, secondMethod }));
                return true;
            }), Is.True);
            Assert.That(constructions, Is.Zero);
            Assert.That(methodConstructions, Is.Zero);
            Assert.That(orderingCalls, Is.Empty);
            Assert.That(Sample(4), Is.EqualTo(5));
            Assert.That(orderingCalls, Is.EqualTo(new[] { "first", "second" }));

            first.Unpatch(Target, HarmonyPatchType.All, first.Id);
            Assert.That(HarmonyPublication.Inspect(Target, snapshot =>
            {
                Assert.That(snapshot.Version, Is.GreaterThan(retained!.Version));
                var current = finalizer ? snapshot.Finalizers : snapshot.Prefixes;
                var previous = finalizer ? retained.Finalizers : retained.Prefixes;
                Assert.That(current.Single().Owner, Is.EqualTo(second.Id));
                Assert.That(previous, Has.Count.EqualTo(2));
                Assert.That(previous.Single(role => role.Owner == first.Id).Before, Is.EqualTo(new[] { second.Id }));
                Assert.That(previous.Single(role => role.Owner == second.Id).After, Is.EqualTo(new[] { first.Id }));
                return true;
            }), Is.True);
        }
        finally
        {
            first.Unpatch(Target, HarmonyPatchType.All, first.Id);
            second.Unpatch(Target, HarmonyPatchType.All, second.Id);
        }
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class CountingAttribute : Attribute
    {
        public CountingAttribute() => constructions++;
    }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CountingMethodAttribute : Attribute
    {
        public CountingMethodAttribute() => methodConstructions++;
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Sample(int value) => Allowed() ? value + 1 : value;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Allowed() => true;
    [CountingMethod] public static void ObservePrefix([Counting] int value) => prefixCalls++;
    [CountingMethod] public static Exception? ObserveFinalizer([Counting] Exception? __exception) { finalizerCalls++; return __exception; }
    [CountingMethod] public static void ObserveInner([Counting] bool __result) => innerCalls++;
    [CountingMethod] public static MethodInfo InnerFactory(MethodBase original) { factoryCalls++; return Method(nameof(ObserveNoArgument)); }
    [CountingMethod] public static void ObserveNoArgument() => innerCalls++;
    public static void OtherCallback() { }
    [CountingMethod] public static MethodInfo PrefixFactory(MethodBase original) { factoryCalls++; return Method(nameof(ObservePrefix)); }
    [CountingMethod] public static MethodInfo FinalizerFactory(MethodBase original) { factoryCalls++; return Method(nameof(ObserveFinalizer)); }
    [CountingMethod] public static void OrderFirstPrefix([Counting] int value) => orderingCalls.Add("first");
    [CountingMethod] public static void OrderSecondPrefix([Counting] int value) => orderingCalls.Add("second");
    [CountingMethod] public static void OrderFirstFinalizer([Counting] Exception? __exception) => orderingCalls.Add("first");
    [CountingMethod] public static void OrderSecondFinalizer([Counting] Exception? __exception) => orderingCalls.Add("second");
}
