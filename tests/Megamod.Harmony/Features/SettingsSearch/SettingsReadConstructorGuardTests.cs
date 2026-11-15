using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.SettingsSearch.Core;
using Megamod.Features.SettingsSearch.Harmony;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.SettingsSearch;

[TestFixture, NonParallelizable]
public sealed class SettingsReadConstructorGuardTests
{
    private const string Reason = "settings-read-window-construction";
    private static readonly ConstructorInfo Target = typeof(BaseWindow).GetConstructor(new[] { typeof(IDrawing) })!;
    private static int baseCalls, derivedCalls, foreignCalls, postfixCalls, finalizerCalls;

    [Test]
    public void RefusalBeforeTheBaseConstructorBodyPreventsTheDerivedBodyAndPreservesOrdinaryConstruction()
    {
        baseCalls = derivedCalls = 0;
        var guard = new SettingsReadActionGuard(Target, "TEST-READ-CONSTRUCTOR", Reason);
        var drawing = new Drawing();
        try
        {
            var warnings = new List<string>();
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(new("read-constructor", AdmissionPhase.Early,
                new[] { guard.Candidate }), AdmissionPhase.Early), Is.True, string.Join("; ", warnings));
            guard.Enable();

            Assert.That(new DerivedWindow(drawing).Drawing, Is.SameAs(drawing));
            Assert.That((baseCalls, derivedCalls), Is.EqualTo((1, 1)));
            using (var scope = SettingsReadScope.Begin())
            {
                var failure = Assert.Throws<InvalidOperationException>(() => _ = new DerivedWindow(drawing));
                Assert.That(failure!.Message, Is.EqualTo(Reason));
                Assert.That(scope.Failed, Is.True);
                Assert.That(scope.FailureReason, Is.EqualTo(Reason));
                Assert.That((baseCalls, derivedCalls), Is.EqualTo((1, 1)),
                    "Throwing before the base body must also prevent the derived constructor's later writes.");
            }
            Assert.That(new DerivedWindow(drawing).Drawing, Is.SameAs(drawing));
            Assert.That((baseCalls, derivedCalls), Is.EqualTo((2, 2)));
        }
        finally { guard.Candidate.Rollback(); }

        Assert.That(new DerivedWindow(drawing).Drawing, Is.SameAs(drawing));
        Assert.That((baseCalls, derivedCalls), Is.EqualTo((3, 3)), "Exact-owner cleanup restores ordinary construction.");
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AForeignFinalizerRefusesAdmissionOrLaterAvailabilityAndSurvivesCleanup(bool laterRepatch)
    {
        baseCalls = derivedCalls = finalizerCalls = 0;
        var foreign = new HarmonyLib.Harmony("tests.settings-read-constructor.finalizer");
        var guard = new SettingsReadActionGuard(Target, "TEST-READ-CONSTRUCTOR-FINALIZER", Reason);
        try
        {
            if (laterRepatch) Admit(guard);
            foreign.Patch(Target, finalizer: new HarmonyMethod(typeof(SettingsReadConstructorGuardTests), nameof(SuppressingFinalizer)));
            if (!laterRepatch)
            {
                string? failure = guard.Candidate.Validate();
                Assert.That(failure, Is.Not.Null);
                Assert.That(failure, Does.Contain("finalizer"));
                Assert.That(new AdmissionCoordinator(_ => { }).Apply(new("constructor-finalizer", AdmissionPhase.Early,
                    new[] { guard.Candidate }), AdmissionPhase.Early), Is.False);
            }
            Assert.That(guard.Available, Is.False);
            Assert.Throws<InvalidOperationException>(guard.Enable);
            Assert.That(finalizerCalls, Is.Zero, "Inspecting the published finalizer must not invoke it.");
            guard.Candidate.Rollback();
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(Target)!.Finalizers.Single().owner, Is.EqualTo(foreign.Id));
            _ = new DerivedWindow(new Drawing());
            Assert.That((baseCalls, derivedCalls, finalizerCalls), Is.EqualTo((1, 1, 1)));
        }
        finally
        {
            try { guard.Candidate.Rollback(); }
            finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); }
        }
    }

    [TestCaseSource(nameof(UnsupportedConstructors))]
    public void UnsupportedConstructorShapeIsRejectedWithoutAcquiringPatches(ConstructorInfo target)
    {
        var guard = new SettingsReadActionGuard(target, "TEST-READ-CONSTRUCTOR-SHAPE", Reason);
        try
        {
            Assert.That(guard.Candidate.Validate(), Is.Not.Null);
            Assert.That(guard.Available, Is.False);
            Assert.Throws<InvalidOperationException>(guard.Enable);
        }
        finally { guard.Candidate.Rollback(); }
        Assert.That(HarmonyLib.Harmony.GetPatchInfo(target)?.Owners.Count ?? 0, Is.Zero);
    }

    [Test]
    public void CompatibleForeignPatchesAndAnotherOwnedKindSurviveConstructorGuardCleanup()
    {
        baseCalls = derivedCalls = foreignCalls = postfixCalls = 0;
        var foreign = new HarmonyLib.Harmony("tests.settings-read-constructor.composition");
        var own = new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId);
        var guard = new SettingsReadActionGuard(Target, "TEST-READ-CONSTRUCTOR-COMPOSITION", Reason);
        try
        {
            foreign.Patch(Target,
                prefix: new HarmonyMethod(typeof(SettingsReadConstructorGuardTests), nameof(ForeignPrefix))
                    { after = [OwnedHarmonyPatch.OwnerId] },
                postfix: new HarmonyMethod(typeof(SettingsReadConstructorGuardTests), nameof(ForeignPostfix)));
            own.Patch(Target, transpiler: new HarmonyMethod(typeof(SettingsReadConstructorGuardTests), nameof(BenignTranspiler)));
            Admit(guard);
            _ = new DerivedWindow(new Drawing());
            Assert.That((baseCalls, derivedCalls, foreignCalls, postfixCalls), Is.EqualTo((1, 1, 1, 1)));
            using (var scope = SettingsReadScope.Begin())
            {
                Assert.Throws<InvalidOperationException>(() => _ = new DerivedWindow(new Drawing()));
                Assert.That(scope.FailureReason, Is.EqualTo(Reason));
            }
            Assert.That((baseCalls, derivedCalls, foreignCalls, postfixCalls), Is.EqualTo((1, 1, 1, 1)));
            guard.Candidate.Rollback();
            guard.Candidate.Rollback();
            _ = new DerivedWindow(new Drawing());
            Assert.That((baseCalls, derivedCalls, foreignCalls, postfixCalls), Is.EqualTo((2, 2, 2, 2)));
            var remaining = HarmonyLib.Harmony.GetPatchInfo(Target)!;
            Assert.That(remaining.Prefixes.Single().owner, Is.EqualTo(foreign.Id));
            Assert.That(remaining.Postfixes.Single().owner, Is.EqualTo(foreign.Id));
            Assert.That(remaining.Transpilers.Single().owner, Is.EqualTo(OwnedHarmonyPatch.OwnerId));
        }
        finally
        {
            try { guard.Candidate.Rollback(); }
            finally
            {
                try { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); }
                finally { own.Unpatch(Target, HarmonyPatchType.Transpiler, OwnedHarmonyPatch.OwnerId); }
            }
        }
    }

    private static IEnumerable<ConstructorInfo> UnsupportedConstructors()
    {
        yield return typeof(StaticInitialization).TypeInitializer!;
        yield return typeof(GenericWindow<>).GetConstructor(Type.EmptyTypes)!;
        yield return typeof(GenericWindow<int>).GetConstructor(Type.EmptyTypes)!;
    }

    private static void Admit(SettingsReadActionGuard guard)
    {
        var warnings = new List<string>();
        Assert.That(new AdmissionCoordinator(warnings.Add).Apply(new("read-constructor", AdmissionPhase.Early,
            new[] { guard.Candidate }), AdmissionPhase.Early), Is.True, string.Join("; ", warnings));
        guard.Enable();
    }

    public static void ForeignPrefix() => foreignCalls++;
    public static void ForeignPostfix() => postfixCalls++;
    public static Exception? SuppressingFinalizer(Exception? __exception) { finalizerCalls++; return null; }
    public static IEnumerable<CodeInstruction> BenignTranspiler(IEnumerable<CodeInstruction> instructions) => instructions;

    public interface IDrawing { }
    public sealed class Drawing : IDrawing { }
    public abstract class BaseWindow
    {
        public IDrawing? Drawing { get; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public BaseWindow(IDrawing? drawing = null) { baseCalls++; Drawing = drawing; }
    }
    public sealed class DerivedWindow : BaseWindow
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public DerivedWindow(IDrawing? drawing = null) : base(drawing) { derivedCalls++; }
    }
    public class GenericWindow<T> { public GenericWindow() { } }
    public class StaticInitialization
    {
        public static int Calls;
        static StaticInitialization() => Calls++;
    }
}
