using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.SettingsSearch.Harmony;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.SettingsSearch;

[TestFixture, NonParallelizable]
public sealed class SettingsListingBoundsAdmissionTests
{
    private static readonly MethodInfo Target = typeof(Listing).GetMethod(nameof(Listing.Draw))!;
    private static int drawings, observations, lastRow, attributeCalls, compositions;

    [Test]
    public void CertifiesOriginalDrawingWithActualOwnedRestorationAndForeignObservation()
    {
        MethodInfo prefix = typeof(Preserve).GetMethod(nameof(Preserve.Prefix))!;
        MethodInfo finalizer = typeof(Preserve).GetMethod(nameof(Preserve.Finalizer))!;
        var guard = new SettingsReadControlGuard(Target, "tests.listing-admission.preserve", prefix, finalizer);
        var foreign = new HarmonyLib.Harmony("tests.listing-admission.observer");
        drawings = observations = 0;
        try
        {
            foreign.Patch(Target, prefix: new HarmonyMethod(typeof(SettingsListingBoundsAdmissionTests), nameof(Observe)));
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(new("listing-admission", AdmissionPhase.Early,
                guard.Candidates), AdmissionPhase.Early), Is.True);
            var listing = new Listing { Bounds = null };
            int value = 4;
            listing.Draw(new Box(10), ref value);
            Assert.That(drawings, Is.EqualTo(1), "The actual original null-bound path draws.");
            Assert.That(observations, Is.EqualTo(1), "The foreign observer is really installed.");
            Assert.That(value, Is.EqualTo(4), "The real owned finalizer restores the value after the original increments it.");

            int pairedChecks = 0;
            var preservation = new SettingsListingBoundsAdmission.Preservation(prefix, finalizer, () =>
            {
                pairedChecks++;
                // Available reads the real provider; a nested publication monitor would refuse.
                return guard.Available;
            });
            Assert.That(SettingsListingBoundsAdmission.TryCapture(new[]
            {
                new SettingsListingBoundsAdmission.Consumer(Target, Checker(), preservation)
            }, out var certificate, out var failure), Is.True, failure);
            Assert.That(certificate!.IsCurrent(), Is.True);
            Assert.That(pairedChecks, Is.GreaterThan(0));
            Assert.That((drawings, observations, value), Is.EqualTo((1, 1, 4)), "Inspection must not invoke drawing or callbacks.");
        }
        finally
        {
            try { RollbackAll(guard); }
            finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); }
        }
        int ordinary = 4;
        new Listing { Bounds = null }.Draw(new Box(10), ref ordinary);
        Assert.That(ordinary, Is.EqualTo(5), "Guaranteed owner cleanup restores ordinary behavior.");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AdmitsEffectiveReadonlyAliasesWithoutConstructingForeignAttributes(bool inner)
    {
        WithGuard((guard, foreign) =>
        {
            if (inner)
                foreign.CreateProcessor(Target).AddInnerPostfix(new HarmonyMethod(Callback(nameof(ObserveNullable)))
                { innerMethod = new InnerMethod(typeof(Box?).GetProperty(nameof(Nullable<Box>.HasValue))!.GetMethod!) }).Patch();
            else foreign.Patch(Target, prefix: new HarmonyMethod(Callback(nameof(ObserveRow))));
            int value = 4;
            new Listing { Bounds = null }.Draw(new Box(10), ref value);
            Assert.That((drawings, observations, value), Is.EqualTo((1, 1, 4)));
            if (inner) Assert.That(attributeCalls, Is.GreaterThan(0), "Normal provider acquisition must activate the counted attribute canary.");
            attributeCalls = 0;
            var certificate = Capture(Consumer(guard));
            Assert.That(certificate.IsCurrent(), Is.True);
            Assert.That(attributeCalls, Is.Zero);
            Assert.That((drawings, observations), Is.EqualTo((1, 1)));
        });
    }

    [Test]
    public void AWriterOfAnUnconsumedArgumentDoesNotReplaceTheDrawingRelationship()
    {
        WithGuard((guard, foreign) =>
        {
            foreign.Patch(Target, prefix: new HarmonyMethod(Callback(nameof(ChangeValue))));
            int value = 4;
            new Listing { Bounds = null }.Draw(new Box(10), ref value);
            Assert.That((drawings, lastRow, value), Is.EqualTo((1, 10, 4)));
            Assert.That(Capture(Consumer(guard)).IsCurrent(), Is.True);
        });
    }

    [Test]
    public void AliasedMutableArgumentArraysCannotMasqueradeAsReadonlyObjects()
    {
        WithGuard((guard, foreign) =>
        {
            foreign.Patch(Target, prefix: new HarmonyMethod(Callback(nameof(ChangeArguments))));
            int value = 4;
            new Listing { Bounds = null }.Draw(new Box(10), ref value);
            Assert.That((drawings, lastRow), Is.EqualTo((1, 99)), "The real object-typed injection edits the consumed row.");
            Assert.That(SettingsListingBoundsAdmission.TryCapture(new[] { Consumer(guard) }, out _, out _), Is.False);
        });
    }

    [Test]
    public void OuterSkippingAndSuppressedPredrawExceptionsAreRefused()
    {
        WithGuard((guard, foreign) =>
        {
            foreign.Patch(Target, prefix: new HarmonyMethod(Callback(nameof(Skip))));
            int value = 4;
            new Listing { Bounds = null }.Draw(new Box(10), ref value);
            Assert.That(drawings, Is.Zero);
            Assert.That(SettingsListingBoundsAdmission.TryCapture(new[] { Consumer(guard) }, out _, out _), Is.False);
            foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id);
            foreign.Patch(Target, prefix: new HarmonyMethod(Callback(nameof(ThrowBeforeDraw))),
                finalizer: new HarmonyMethod(Callback(nameof(Suppress))));
            Assert.DoesNotThrow(() => new Listing { Bounds = null }.Draw(new Box(10), ref value));
            Assert.That(drawings, Is.Zero, "Exception suppression can falsely resemble a completed draw.");
            Assert.That(SettingsListingBoundsAdmission.TryCapture(new[] { Consumer(guard) }, out _, out _), Is.False);
        });
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ConsumedInnerResultAndSkipEffectsAreRefusedAfterActualExecution(bool skipDraw)
    {
        WithGuard((guard, foreign) =>
        {
            if (skipDraw)
                foreign.CreateProcessor(Target).AddInnerPrefix(new HarmonyMethod(Callback(nameof(Skip)))
                { innerMethod = new InnerMethod(Callback(nameof(DrawBox))) }).Patch();
            else
                foreign.CreateProcessor(Target).AddInnerPostfix(new HarmonyMethod(Callback(nameof(ChangeHasValue)))
                { innerMethod = new InnerMethod(typeof(Box?).GetProperty(nameof(Nullable<Box>.HasValue))!.GetMethod!) }).Patch();
            int value = 4;
            if (skipDraw) new Listing { Bounds = null }.Draw(new Box(10), ref value);
            else Assert.Throws<InvalidOperationException>(() => new Listing { Bounds = null }.Draw(new Box(10), ref value));
            Assert.That(drawings, Is.Zero);
            Assert.That(SettingsListingBoundsAdmission.TryCapture(new[] { Consumer(guard) }, out _, out _), Is.False);
        });
    }

    [Test]
    public void UnrelatedPublicationIsCompatibleButAConsumedRepatchPermanentlyInvalidatesTheReceipt()
    {
        WithGuard((guard, foreign) =>
        {
            var certificate = Capture(Consumer(guard));
            MethodInfo unrelated = Callback(nameof(Unrelated));
            try
            {
                foreign.Patch(unrelated, prefix: new HarmonyMethod(Callback(nameof(Observe))));
                Unrelated();
                Assert.That(observations, Is.EqualTo(1));
                Assert.That(certificate.IsCurrent(), Is.True);
                foreign.Patch(Target, prefix: new HarmonyMethod(Callback(nameof(Observe))));
                int value = 4;
                new Listing { Bounds = null }.Draw(new Box(10), ref value);
                Assert.That((drawings, observations, value), Is.EqualTo((1, 2, 4)));
                Assert.That(certificate.IsCurrent(), Is.False);
                foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id);
                Assert.That(Capture(Consumer(guard)).IsCurrent(), Is.True);
                Assert.That(certificate.IsCurrent(), Is.False);
            }
            finally { foreign.Unpatch(unrelated, HarmonyPatchType.All, foreign.Id); }
        });
    }

    [Test]
    public void AnObservedPairedGuardLossCannotReviveTheSameCertificate()
    {
        WithGuard((guard, _) =>
        {
            int value = 4;
            new Listing { Bounds = null }.Draw(new Box(10), ref value);
            Assert.That((drawings, value), Is.EqualTo((1, 4)));
            bool retainedGuard = true;
            var consumer = Consumer(guard, () => retainedGuard && guard.Available);
            var certificate = Capture(consumer);
            retainedGuard = false;
            Assert.That(certificate.IsCurrent(), Is.False);
            retainedGuard = true;
            Assert.That(Capture(consumer).IsCurrent(), Is.True);
            Assert.That(certificate.IsCurrent(), Is.False);
        });
    }

    [Test]
    public void AnAliasedForeignWriterCannotShareTheOwnedPreservationStateSlot()
    {
        WithGuard((guard, foreign) =>
        {
            var before = Capture(Consumer(guard));
            foreign.Patch(Target, postfix: new HarmonyMethod(typeof(Preserve).GetMethod(nameof(Preserve.ChangeAliasedState))!));
            int value = 4;
            new Listing { Bounds = null }.Draw(new Box(10), ref value);
            Assert.That((drawings, value), Is.EqualTo((1, 99)),
                "The actual aliased postfix overwrites the acquired state used by the owned finalizer.");
            Assert.Multiple(() =>
            {
                Assert.That(guard.Available, Is.False, "Paired-state inspection must use the effective injected binding.");
                Assert.That(SettingsListingBoundsAdmission.TryCapture(new[] { Consumer(guard) }, out _, out _), Is.False,
                    "Unsafe restoration must not become an admitted listing read.");
            });
            Assert.That(before.IsCurrent(), Is.False);
            foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id);
            value = 4;
            new Listing { Bounds = null }.Draw(new Box(10), ref value);
            Assert.That(value, Is.EqualTo(4), "Removing only the foreign owner restores the owned preservation path.");
            Assert.That(Capture(Consumer(guard)).IsCurrent(), Is.True);
            Assert.That(before.IsCurrent(), Is.False);
        });
    }

    [Test]
    public void ASeparateDeclaringTypesAliasedPrivateStateRemainsCompatible()
    {
        WithGuard((guard, foreign) =>
        {
            foreign.Patch(Target, prefix: new HarmonyMethod(typeof(IndependentState).GetMethod(nameof(IndependentState.Prefix))!),
                postfix: new HarmonyMethod(typeof(IndependentState).GetMethod(nameof(IndependentState.Postfix))!));
            int value = 4;
            new Listing { Bounds = null }.Draw(new Box(10), ref value);
            Assert.That((drawings, observations, value), Is.EqualTo((1, 17, 4)),
                "The foreign state is read and replaced without overwriting the owned state's value.");
            Assert.That(guard.Available, Is.True);
            Assert.That(Capture(Consumer(guard)).IsCurrent(), Is.True);
        });
    }

    [TestCase(15, true)]
    [TestCase(17, false)]
    public void PayloadInspectionHasAnAggregateFieldBudgetBeyondEachIndividualType(int children, bool supported)
    {
        var fixture = PayloadFixture(children);
        var foreign = new HarmonyLib.Harmony("tests.listing-admission.payload-budget");
        drawings = 0;
        try
        {
            foreign.Patch(fixture.Target, prefix: new HarmonyMethod(fixture.Prefix));
            fixture.Target.Invoke(Activator.CreateInstance(fixture.Target.DeclaringType!), new object[] { new Box(10) });
            Assert.That(drawings, Is.EqualTo(1), "The real by-value field injection preserves the native drawing path.");
            bool admitted = SettingsListingBoundsAdmission.TryCapture(new[]
            {
                new SettingsListingBoundsAdmission.Consumer(fixture.Target, Checker(fixture.Getter))
            }, out var certificate, out _);
            Assert.That(admitted, Is.EqualTo(supported),
                $"The payload has {children + children * 64} distinct instance fields; each type has at most64 and depth is only2.");
            if (supported) Assert.That(certificate!.IsCurrent(), Is.True);
            Assert.That(drawings, Is.EqualTo(1), "Budget inspection does not execute the fixture.");
        }
        finally { foreign.Unpatch(fixture.Target, HarmonyPatchType.All, foreign.Id); }
    }

    [TestCase(16, true)]
    [TestCase(17, false)]
    public void RepeatedOverlappingSubtypesAreMemoizedWithoutLosingTheDepthLimit(int depth, bool supported)
    {
        var module = PayloadModule();
        Type payload = NestedPayload(module, "Overlap", depth, 8, typeof(int));
        AssertPayload(PayloadFixture(module, payload), supported);
    }

    [TestCase(7, true)]
    [TestCase(8, false)]
    public void APreviouslyInspectedSubtypeRetainsItsHeightWhenReusedDeeper(int wrappers, bool supported)
    {
        var module = PayloadModule();
        Type shared = NestedPayload(module, "Shared", 8, 1, typeof(int));
        Type deeper = NestedPayload(module, "Wrapper", wrappers, 1, shared);
        var root = module.DefineType("ReusedPayload", TypeAttributes.Public | TypeAttributes.ExplicitLayout | TypeAttributes.Sealed,
            typeof(ValueType), PackingSize.Size1, 4);
        root.DefineField("First", shared, FieldAttributes.Public).SetOffset(0);
        root.DefineField("Second", deeper, FieldAttributes.Public).SetOffset(0);
        Type payload = root.CreateType()!;
        Assert.That(payload.GetFields(BindingFlags.Instance | BindingFlags.Public).Select(field => field.Name),
            Is.EqualTo(new[] { "First", "Second" }), "The fixture must inspect the shared shape at the shallower position first.");
        AssertPayload(PayloadFixture(module, payload), supported);
    }

    private static void AssertPayload((MethodInfo Target, MethodInfo Prefix, MethodInfo Getter) fixture, bool supported)
    {
        var foreign = new HarmonyLib.Harmony("tests.listing-admission.payload-shape");
        drawings = 0;
        try
        {
            foreign.Patch(fixture.Target, prefix: new HarmonyMethod(fixture.Prefix));
            fixture.Target.Invoke(Activator.CreateInstance(fixture.Target.DeclaringType!), new object[] { new Box(10) });
            Assert.That(drawings, Is.EqualTo(1));
            Assert.That(SettingsListingBoundsAdmission.TryCapture(new[]
            {
                new SettingsListingBoundsAdmission.Consumer(fixture.Target, Checker(fixture.Getter))
            }, out var certificate, out _), Is.EqualTo(supported));
            if (supported) Assert.That(certificate!.IsCurrent(), Is.True);
            Assert.That(drawings, Is.EqualTo(1));
        }
        finally { foreign.Unpatch(fixture.Target, HarmonyPatchType.All, foreign.Id); }
    }

    [Test]
    public void OriginalMetadataDoesNotApproveOrReplayATransformedConsumer()
    {
        WithGuard((guard, foreign) =>
        {
            foreign.Patch(Target, transpiler: new HarmonyMethod(Callback(nameof(NoChange))));
            int value = 4;
            new Listing { Bounds = null }.Draw(new Box(10), ref value);
            Assert.That(drawings, Is.EqualTo(1));
            Assert.That(compositions, Is.GreaterThan(0));
            compositions = 0;
            Assert.That(SettingsListingBoundsAdmission.TryCapture(new[] { Consumer(guard) }, out _, out var failure), Is.False);
            Assert.That(failure, Does.Contain("effective-body"));
            Assert.That(compositions, Is.Zero);
        });
    }

    [Test]
    public void SymbolicGenericProofIncludesEveryActualFamilyMemberWithoutATypeList()
    {
        var definition = typeof(Listing).GetMethod(nameof(Listing.Generic))!;
        var custom = definition.MakeGenericMethod(typeof(UnknownValue));
        var sibling = definition.MakeGenericMethod(typeof(decimal));
        var foreign = new HarmonyLib.Harmony("tests.listing-admission.generic");
        var consumer = new SettingsListingBoundsAdmission.Consumer(definition, GenericChecker());
        drawings = observations = 0;
        try
        {
            var listing = new Listing { Bounds = null };
            listing.Generic(new Box(10), new UnknownValue(7));
            Assert.That(drawings, Is.EqualTo(1));
            var empty = Capture(consumer);
            foreign.Patch(custom, prefix: new HarmonyMethod(Callback(nameof(Observe))));
            listing.Generic(new Box(10), new UnknownValue(7));
            Assert.That((drawings, observations), Is.EqualTo((2, 1)));
            Assert.That(empty.IsCurrent(), Is.False);
            var compatible = Capture(consumer);
            foreign.Patch(sibling, prefix: new HarmonyMethod(Callback(nameof(Skip))));
            listing.Generic(new Box(10), 7m);
            Assert.That(drawings, Is.EqualTo(2));
            Assert.That(compatible.IsCurrent(), Is.False);
            Assert.That(SettingsListingBoundsAdmission.TryCapture(new[] { consumer }, out _, out _), Is.False);
            foreign.Unpatch(sibling, HarmonyPatchType.All, foreign.Id);
            listing.Generic(new Box(10), 7m);
            Assert.That(drawings, Is.EqualTo(3));
            Assert.That(Capture(consumer).IsCurrent(), Is.True);
            Assert.That(compatible.IsCurrent(), Is.False);
        }
        finally
        {
            try { foreign.Unpatch(custom, HarmonyPatchType.All, foreign.Id); }
            finally { foreign.Unpatch(sibling, HarmonyPatchType.All, foreign.Id); }
        }
    }

    private static MethodInfo Callback(string name) => typeof(SettingsListingBoundsAdmissionTests).GetMethod(name)!;
    private static SettingsListingBoundsAdmission.Consumer Consumer(SettingsReadControlGuard guard, Func<bool>? current = null) => new(Target,
        Checker(), new(typeof(Preserve).GetMethod(nameof(Preserve.Prefix))!, typeof(Preserve).GetMethod(nameof(Preserve.Finalizer))!, current ?? (() => guard.Available)));
    private static SettingsListingBoundsAdmission.Certificate Capture(SettingsListingBoundsAdmission.Consumer consumer)
    {
        Assert.That(SettingsListingBoundsAdmission.TryCapture(new[] { consumer }, out var certificate, out var failure), Is.True, failure);
        return certificate!;
    }
    private static void WithGuard(Action<SettingsReadControlGuard, HarmonyLib.Harmony> test)
    {
        var guard = new SettingsReadControlGuard(Target, "tests.listing-admission.preserve", typeof(Preserve).GetMethod(nameof(Preserve.Prefix))!,
            typeof(Preserve).GetMethod(nameof(Preserve.Finalizer))!);
        var foreign = new HarmonyLib.Harmony("tests.listing-admission.effects");
        drawings = observations = lastRow = attributeCalls = compositions = 0;
        try
        {
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(new("listing-admission", AdmissionPhase.Early,
                guard.Candidates), AdmissionPhase.Early), Is.True);
            test(guard, foreign);
        }
        finally
        {
            try { RollbackAll(guard); }
            finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); }
        }
    }

    private static SettingsListingBoundsSemantics GenericChecker() => new(new(
        typeof(Listing).GetProperty(nameof(Listing.Bounds))!.GetMethod!,
        typeof(Box?).GetProperty(nameof(Nullable<Box>.HasValue))!.GetMethod!,
        typeof(Box?).GetProperty(nameof(Nullable<Box>.Value))!.GetMethod!,
        typeof(Box).GetMethod(nameof(Box.Overlaps))!,
        new[] { new SettingsListingBoundsSemantics.Draw(Callback(nameof(DrawGeneric)), ConsumerGenericArguments: new[] { 0 }) }));

    private static SettingsListingBoundsSemantics Checker(MethodInfo? getter = null) => new(new(
        getter ?? typeof(Listing).GetProperty(nameof(Listing.Bounds))!.GetMethod!,
        typeof(Box?).GetProperty(nameof(Nullable<Box>.HasValue))!.GetMethod!,
        typeof(Box?).GetProperty(nameof(Nullable<Box>.Value))!.GetMethod!,
        typeof(Box).GetMethod(nameof(Box.Overlaps))!,
        new[] { new SettingsListingBoundsSemantics.Draw(typeof(SettingsListingBoundsAdmissionTests).GetMethod(nameof(DrawBox))!) }));

    private static void RollbackAll(SettingsReadControlGuard guard)
    {
        var failures = new List<Exception>();
        foreach (var candidate in guard.Candidates.Reverse())
            try { candidate.Rollback(); }
            catch (Exception error) { failures.Add(error); }
        if (failures.Count != 0) throw new AggregateException(failures);
    }

    private static (MethodInfo Target, MethodInfo Prefix, MethodInfo Getter) PayloadFixture(int children)
    {
        var module = PayloadModule();
        var root = module.DefineType("Payload", TypeAttributes.Public | TypeAttributes.SequentialLayout | TypeAttributes.Sealed, typeof(ValueType));
        for (int child = 0; child < children; child++)
        {
            var leaf = module.DefineType("Child" + child, TypeAttributes.Public | TypeAttributes.SequentialLayout | TypeAttributes.Sealed, typeof(ValueType));
            for (int field = 0; field < 64; field++) leaf.DefineField("Value" + field, typeof(int), FieldAttributes.Public);
            root.DefineField("Child" + child, leaf.CreateType()!, FieldAttributes.Public);
        }
        Type payload = root.CreateType()!;
        return PayloadFixture(module, payload);
    }
    private static ModuleBuilder PayloadModule() => AssemblyBuilder.DefineDynamicAssembly(
        new AssemblyName("ListingPayload" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.RunAndCollect).DefineDynamicModule("fixture");
    private static Type NestedPayload(ModuleBuilder module, string name, int depth, int branches, Type child)
    {
        for (int level = 0; level < depth; level++)
        {
            var next = module.DefineType(name + level, TypeAttributes.Public | TypeAttributes.ExplicitLayout | TypeAttributes.Sealed,
                typeof(ValueType), PackingSize.Size1, 4);
            for (int field = 0; field < branches; field++) next.DefineField("Child" + field, child, FieldAttributes.Public).SetOffset(0);
            child = next.CreateType()!;
        }
        return child;
    }
    private static (MethodInfo Target, MethodInfo Prefix, MethodInfo Getter) PayloadFixture(ModuleBuilder module, Type payload)
    {
        var listing = module.DefineType("ListingFixture", TypeAttributes.Public | TypeAttributes.Sealed);
        listing.DefineDefaultConstructor(MethodAttributes.Public);
        listing.DefineField("Payload", payload, FieldAttributes.Private);
        var bounds = listing.DefineField("Bounds", typeof(Box?), FieldAttributes.Public);
        var getter = listing.DefineMethod("get_Bounds", MethodAttributes.Public | MethodAttributes.SpecialName | MethodAttributes.HideBySig, typeof(Box?), Type.EmptyTypes);
        var get = getter.GetILGenerator();
        get.Emit(OpCodes.Ldarg_0); get.Emit(OpCodes.Ldfld, bounds); get.Emit(OpCodes.Ret);
        var draw = listing.DefineMethod("Draw", MethodAttributes.Public, typeof(void), new[] { typeof(Box) });
        var il = draw.GetILGenerator();
        var local = il.DeclareLocal(typeof(Box?));
        var drawing = il.DefineLabel();
        il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Call, getter); il.Emit(OpCodes.Stloc, local);
        il.Emit(OpCodes.Ldloca, local); il.Emit(OpCodes.Call, typeof(Box?).GetProperty(nameof(Nullable<Box>.HasValue))!.GetMethod!);
        il.Emit(OpCodes.Brfalse, drawing);
        il.Emit(OpCodes.Ldarga_S, (byte)1); il.Emit(OpCodes.Ldarg_0); il.Emit(OpCodes.Call, getter); il.Emit(OpCodes.Stloc, local);
        il.Emit(OpCodes.Ldloca, local); il.Emit(OpCodes.Call, typeof(Box?).GetProperty(nameof(Nullable<Box>.Value))!.GetMethod!);
        il.Emit(OpCodes.Call, typeof(Box).GetMethod(nameof(Box.Overlaps))!); il.Emit(OpCodes.Brtrue, drawing); il.Emit(OpCodes.Ret);
        il.MarkLabel(drawing); il.Emit(OpCodes.Ldarg_1); il.Emit(OpCodes.Call, Callback(nameof(DrawBox))); il.Emit(OpCodes.Ret);
        Type listingType = listing.CreateType()!;
        var observer = module.DefineType("Observer", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
        var prefix = observer.DefineMethod("Prefix", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new[] { payload });
        prefix.DefineParameter(1, ParameterAttributes.None, "___Payload");
        prefix.GetILGenerator().Emit(OpCodes.Ret);
        return (listingType.GetMethod("Draw")!, observer.CreateType()!.GetMethod("Prefix")!, listingType.GetMethod("get_Bounds")!);
    }
    public static void Observe() { observations++; }
    public static void ObserveRow([HarmonyArgument("__0"), Counted] Box row) { observations++; }
    public static void ObserveNullable([HarmonyArgument("__result"), Counted] bool value) { observations++; }
    public static void ChangeArguments([HarmonyArgument("__args")] object arguments) { ((object[])arguments)[0] = new Box(99); }
    public static void ChangeValue(ref int __1) { __1 = 99; }
    public static void ChangeHasValue(ref bool __result) { __result = true; }
    public static bool Skip() => false;
    public static void ThrowBeforeDraw() => throw new InvalidOperationException("Owned fixture failure before drawing.");
    public static Exception? Suppress(Exception __exception) => null;
    public static IEnumerable<CodeInstruction> NoChange(IEnumerable<CodeInstruction> source) { compositions++; return source; }
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Unrelated() { }
    [MethodImpl(MethodImplOptions.NoInlining)] public static void DrawBox(Box row) { drawings++; lastRow = row.Coordinate; }
    [MethodImpl(MethodImplOptions.NoInlining)] public static void DrawGeneric<T>(Box row, T value) where T : struct { drawings++; lastRow = row.Coordinate; }
    public readonly record struct UnknownValue(int Value);
    [AttributeUsage(AttributeTargets.Parameter)] public sealed class CountedAttribute : Attribute { public CountedAttribute() { attributeCalls++; } }
    public struct Box
    {
        public int Coordinate;
        public Box(int coordinate) { Coordinate = coordinate; }
        [MethodImpl(MethodImplOptions.NoInlining)] public bool Overlaps(Box other) => Coordinate == other.Coordinate;
    }
    public sealed class Listing
    {
        public Box? Bounds { get; set; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Draw(Box row, ref int value)
        {
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawBox(row);
            value++;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Generic<T>(Box row, T value) where T : struct
        {
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawGeneric(row, value);
        }
    }
    public static class Preserve
    {
        [HarmonyPriority(Priority.First)] public static void Prefix(ref int __1, out int __state) { __state = __1; }
        [HarmonyPriority(Priority.Last)] public static void Finalizer(ref int __1, int __state) { __1 = __state; }
        public static void ChangeAliasedState([HarmonyArgument("__state")] ref int saved) { saved = 99; }
    }
    public static class IndependentState
    {
        public static void Prefix(out int __state) { __state = 17; }
        public static void Postfix([HarmonyArgument("__state")] ref int saved) { observations = saved; saved = 99; }
    }
}
