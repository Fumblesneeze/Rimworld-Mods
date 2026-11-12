using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.DrugPolicyGeneration;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.DrugPolicyGeneration;

[TestFixture, NonParallelizable]
public sealed class DrugGenerationRateAdmissionTests
{
    private static MethodInfo Method(Type type, string name) => type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)!;
    private static FieldInfo Field(Type type, string name) => type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)!;
    private static DrugGenerationRateBodyGuard.Bindings Bindings() => new(Method(typeof(AgeTracker), "get_Years"), Field(typeof(AgeTracker), "ticks"),
        Method(typeof(CoreFlags), "get_Biotech"), Field(typeof(CoreFlags), nameof(CoreFlags.Active)),
        Method(typeof(Decay), nameof(Decay.Rate)), Method(typeof(Comp), "get_Pawn"), Method(typeof(Decay), "get_Props"),
        Method(typeof(Hediff), "get_CurStage"), Field(typeof(Pawn), nameof(Pawn.Tracker)), Field(typeof(Decay), nameof(Decay.StoredRate)),
        Field(typeof(Comp), nameof(Comp.Parent)), Field(typeof(Properties), nameof(Properties.MinAge)), Field(typeof(Stage), nameof(Stage.Factor)));
    private static DrugGenerationObservedBodies Observe(DrugGenerationRateBodyGuard guard, params MethodInfo[] roots) =>
        new(roots.Select(method => new DrugGenerationObservedBodies.Root(method, body => guard.Validate(method, body))).ToArray());
    private static int Bits(float value) => BitConverter.SingleToInt32Bits(value);

    [TestCase(-1L, -1f / 3600000f)]
    [TestCase(0L, 0f)]
    [TestCase(3600000L, 1f)]
    [TestCase(64800000L, 18f)]
    public void ActualAgeProjectionReadsTheCurrentTrackerTicks(long ticks, float expectedYears)
    {
        var bindings = Bindings(); var guard = new DrugGenerationRateBodyGuard(bindings);
        var observation = Observe(guard, bindings.AgeYearsGetter); var tracker = new AgeTracker(ticks);
        try
        {
            observation.Install(); Assert.That(observation.Available, Is.True, observation.Failure);
            Assert.That(Bits(tracker.Years), Is.EqualTo(Bits(expectedYears)));
            tracker.SetTicks(3600000L); Assert.That(Bits(tracker.Years), Is.EqualTo(Bits(1f)));
        }
        finally { observation.Rollback(); }
    }

    [Test]
    public void ActualFlagProjectionDoesNotCertifyItsCurrentFalsePremise()
    {
        CoreFlags.SetForFixture(false); int initialized = FixtureCounts.Initializations;
        var bindings = Bindings(); var guard = new DrugGenerationRateBodyGuard(bindings);
        var observation = Observe(guard, bindings.BiotechGetter);
        try
        {
            observation.Install(); Assert.That(observation.Available, Is.True, observation.Failure);
            Assert.That(CoreFlags.Biotech, Is.False);
            CoreFlags.SetForFixture(true); Assert.That(CoreFlags.Biotech, Is.True);
            Assert.That(observation.Available, Is.True, observation.Failure);
            Assert.That(FixtureCounts.Initializations, Is.EqualTo(initialized));
        }
        finally { try { observation.Rollback(); } finally { CoreFlags.SetForFixture(false); } }
    }

    [Test]
    public void AnActualProtectedFlagWriterRevokesTheProjectionReceipt()
    {
        CoreFlags.SetForFixture(false); var bindings = Bindings(); var guard = new DrugGenerationRateBodyGuard(bindings);
        var observation = Observe(guard, bindings.BiotechGetter); var foreign = new HarmonyLib.Harmony("tests.generation.rate-flag-writer");
        try
        {
            observation.Install(); foreign.Patch(bindings.BiotechGetter, prefix: new HarmonyMethod(Method(typeof(Effects), nameof(Effects.WriteFlag))));
            Assert.That(CoreFlags.Biotech, Is.True);
            Assert.That(observation.Available, Is.False); Assert.That(observation.Revalidate(), Is.False);
            foreign.Unpatch(bindings.BiotechGetter, HarmonyPatchType.Prefix, foreign.Id);
            CoreFlags.SetForFixture(false); Assert.That(CoreFlags.Biotech, Is.False);
            Assert.That(observation.Available, Is.True, observation.Failure);
        }
        finally { try { observation.Rollback(); } finally { foreign.Unpatch(bindings.BiotechGetter, HarmonyPatchType.All, foreign.Id); CoreFlags.SetForFixture(false); } }
    }

    [Test]
    public void ANoInitializerFlagTypeCannotUseTheSameProtectionArgument()
    {
        NoInitializerFlags.Active = false;
        var bindings = Bindings() with { BiotechGetter = Method(typeof(NoInitializerFlags), "get_Biotech"), BiotechActive = Field(typeof(NoInitializerFlags), nameof(NoInitializerFlags.Active)) };
        var guard = new DrugGenerationRateBodyGuard(bindings); var observation = Observe(guard, bindings.BiotechGetter);
        var foreign = new HarmonyLib.Harmony("tests.generation.rate-unprotected-flag");
        try
        {
            foreign.Patch(bindings.BiotechGetter, prefix: new HarmonyMethod(Method(typeof(NoInitializerFlags), nameof(NoInitializerFlags.WriteOwnFlag))));
            Assert.That(NoInitializerFlags.Biotech, Is.True);
            Assert.Throws<InvalidOperationException>(observation.Install);
            Assert.That(observation.Failure, Does.Contain("not protected"));
        }
        finally { try { observation.Rollback(); } finally { foreign.Unpatch(bindings.BiotechGetter, HarmonyPatchType.All, foreign.Id); NoInitializerFlags.Active = false; } }
    }

    [TestCase(LeafMutation.OtherTicks)] [TestCase(LeafMutation.Divisor)]
    [TestCase(LeafMutation.DoubleConversion)] [TestCase(LeafMutation.Return)]
    [TestCase(LeafMutation.OtherFlag)] [TestCase(LeafMutation.FlagReturn)]
    public void ChangedLeafEffectsExecuteAndRevokeTheCurrentReceipt(LeafMutation mutation)
    {
        CoreFlags.SetForFixture(false); CoreFlags.Other = true;
        var bindings = Bindings(); var guard = new DrugGenerationRateBodyGuard(bindings);
        var target = mutation >= LeafMutation.OtherFlag ? bindings.BiotechGetter : bindings.AgeYearsGetter;
        var observation = Observe(guard, target); var foreign = new HarmonyLib.Harmony("tests.generation.rate-leaf"); Effects.LeafChange = mutation;
        try
        {
            observation.Install(); foreign.Patch(target, transpiler: new HarmonyMethod(Method(typeof(Effects), nameof(Effects.ChangeLeaf))));
            // This boundary differs by one Single ulp when conversion is moved after division.
            var tracker = new AgeTracker(16777219L) { OtherTicks = 3600000L };
            if (mutation >= LeafMutation.OtherFlag) Assert.That(CoreFlags.Biotech, Is.True);
            else
            {
                float expected = mutation switch { LeafMutation.OtherTicks => 1f, LeafMutation.Divisor => (float)16777219L / 1800000f,
                    LeafMutation.DoubleConversion => (float)(16777219d / 3600000d), _ => 42f };
                Assert.That(Bits(tracker.Years), Is.EqualTo(Bits(expected)));
                Assert.That(Bits(tracker.Years), Is.Not.EqualTo(Bits((float)16777219L / 3600000f)));
            }
            Assert.That(observation.Available, Is.False); Assert.That(observation.Failure, Does.Contain(target.Name));
            foreign.Unpatch(target, HarmonyPatchType.Transpiler, foreign.Id);
            Assert.That(observation.Available, Is.True, observation.Failure);
        }
        finally { try { observation.Rollback(); } finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); CoreFlags.SetForFixture(false); CoreFlags.Other = false; } }
    }

    [TestCase(false)] [TestCase(true)]
    public void CoreRateUsesTheCurrentPawnAndStoredRateWithNullOrUnitStage(bool stage)
    {
        CoreFlags.SetForFixture(false); var bindings = Bindings(); var guard = new DrugGenerationRateBodyGuard(bindings);
        var observation = Observe(guard, guard.Methods); var comp = Fixture(stage);
        try
        {
            observation.Install(); Assert.That(observation.Available, Is.True, observation.Failure);
            Assert.That(Bits(comp.Rate()), Is.EqualTo(Bits(-.03f)));
            comp.Parent.Pawn.Tracker = new AgeTracker(-1L);
            Assert.That(Bits(comp.Rate()), Is.EqualTo(Bits(0f)));
            comp.Parent.Pawn.Tracker = new AgeTracker(0L);
            comp.StoredRate = -.12f;
            Assert.That(Bits(comp.Rate()), Is.EqualTo(Bits(-.12f)));
        }
        finally { observation.Rollback(); }
    }

    [TestCase(RateMutation.OtherPawn)] [TestCase(RateMutation.OtherAge)] [TestCase(RateMutation.OtherProperties)]
    [TestCase(RateMutation.OtherStage)] [TestCase(RateMutation.OtherFlag)] [TestCase(RateMutation.OtherTracker)]
    [TestCase(RateMutation.OtherRate)] [TestCase(RateMutation.OtherParent)] [TestCase(RateMutation.OtherMinimum)]
    [TestCase(RateMutation.OtherFactor)] [TestCase(RateMutation.AgeBranch)] [TestCase(RateMutation.ZeroReturn)]
    [TestCase(RateMutation.NullFactor)] [TestCase(RateMutation.StageBranch)] [TestCase(RateMutation.FlagBranch)]
    [TestCase(RateMutation.OtherLocal)] [TestCase(RateMutation.DoubleStore)] [TestCase(RateMutation.RateAddressWrite)]
    public void ChangedRateConsumerExecutesItsEffectsAndRevokesTheReceipt(RateMutation mutation)
    {
        CoreFlags.SetForFixture(false); var bindings = Bindings(); var guard = new DrugGenerationRateBodyGuard(bindings);
        var observation = Observe(guard, guard.Methods); var foreign = new HarmonyLib.Harmony("tests.generation.rate-consumer");
        var comp = Fixture(mutation != RateMutation.NullFactor); Effects.RateChange = mutation;
        if (mutation == RateMutation.OtherAge) ((Properties)comp.Properties).MinAge = 20f;
        if (mutation == RateMutation.ZeroReturn) comp.Parent.Pawn.Tracker.SetTicks(-1);
        if (mutation == RateMutation.StageBranch) comp.Parent.Stage!.Factor = 2f;
        try
        {
            observation.Install(); foreign.Patch(bindings.Rate, transpiler: new HarmonyMethod(Method(typeof(Effects), nameof(Effects.ChangeRate))));
            if (mutation is RateMutation.OtherFlag or RateMutation.FlagBranch)
                Assert.Throws<InvalidOperationException>(() => comp.Rate(), "The executed changed path reaches the unproved tail.");
            else
            {
                float expected = mutation switch
                {
                    RateMutation.OtherPawn or RateMutation.OtherTracker or RateMutation.OtherProperties or RateMutation.OtherMinimum or RateMutation.AgeBranch => 0f,
                    RateMutation.OtherRate or RateMutation.OtherLocal or RateMutation.RateAddressWrite => -.12f,
                    RateMutation.OtherStage or RateMutation.OtherParent or RateMutation.OtherFactor or RateMutation.NullFactor => -.06f,
                    RateMutation.ZeroReturn => 42f, _ => -.03f
                };
                Assert.That(Bits(comp.Rate()), Is.EqualTo(Bits(expected)));
                if (mutation == RateMutation.RateAddressWrite) Assert.That(Bits(comp.StoredRate), Is.EqualTo(Bits(-.12f)));
            }
            Assert.That(observation.Available, Is.False); Assert.That(observation.Failure, Does.Contain(nameof(Decay.Rate)));
            foreign.Unpatch(bindings.Rate, HarmonyPatchType.Transpiler, foreign.Id);
            Assert.That(observation.Available, Is.True, observation.Failure);
        }
        finally { try { observation.Rollback(); } finally { foreign.Unpatch(bindings.Rate, HarmonyPatchType.All, foreign.Id); CoreFlags.SetForFixture(false); } }
    }

    [Test]
    public void MovingTheRateReadAcrossAnAliasingStageCallChangesTheActualResultAndIsRefused()
    {
        CoreFlags.SetForFixture(false); var bindings = Bindings(); var guard = new DrugGenerationRateBodyGuard(bindings);
        var observation = Observe(guard, guard.Methods); var foreign = new HarmonyLib.Harmony("tests.generation.rate-order");
        var comp = Fixture(); comp.Parent = new MutatingStage { Pawn = comp.Parent.Pawn, Stage = comp.Parent.Stage, Comp = comp };
        try
        {
            // The stage's separate no-effect prerequisite is deliberately absent here. This
            // diagnoses caller ordering only, not admission of that mutating virtual body.
            observation.Install(); Assert.That(observation.Available, Is.True, observation.Failure);
            Assert.That(Bits(comp.Rate()), Is.EqualTo(Bits(-.03f))); Assert.That(Bits(comp.StoredRate), Is.EqualTo(Bits(-.12f)));
            comp.StoredRate = -.03f;
            foreign.Patch(bindings.Rate, transpiler: new HarmonyMethod(Method(typeof(Effects), nameof(Effects.LateRateRead))));
            Assert.That(Bits(comp.Rate()), Is.EqualTo(Bits(-.12f)));
            Assert.That(observation.Available, Is.False);
        }
        finally { try { observation.Rollback(); } finally { foreign.Unpatch(bindings.Rate, HarmonyPatchType.All, foreign.Id); } }
    }

    [Test]
    public void LabeledNopsAndAnAcquiredScalarObserverRemainPositiveUntilItsBodyWritesTheFlag()
    {
        CoreFlags.SetForFixture(false); Effects.Observed = 0; var bindings = Bindings(); var guard = new DrugGenerationRateBodyGuard(bindings);
        var observation = Observe(guard, guard.Methods); var foreign = new HarmonyLib.Harmony("tests.generation.rate-observer");
        var callback = Method(typeof(Effects), nameof(Effects.Observe));
        try
        {
            foreach (var method in guard.Methods) foreign.Patch(method, transpiler: new HarmonyMethod(Method(typeof(Effects), nameof(Effects.Nops))));
            observation.Install(); foreign.Patch(bindings.Rate, postfix: new HarmonyMethod(callback));
            Assert.That(observation.Available, Is.False); Assert.That(Effects.Observed, Is.Zero);
            Assert.That(observation.Revalidate(), Is.True, observation.Failure); Assert.That(Effects.Observed, Is.Zero);
            Assert.That(Bits(Fixture().Rate()), Is.EqualTo(Bits(-.03f))); Assert.That(Effects.Observed, Is.EqualTo(1));
            foreign.Patch(callback, transpiler: new HarmonyMethod(Method(typeof(Effects), nameof(Effects.ObserverWritesFlag))));
            Assert.That(CoreFlags.Biotech, Is.False); Assert.That(observation.Available, Is.False);
            Assert.That(Bits(Fixture().Rate()), Is.EqualTo(Bits(-.03f))); Assert.That(CoreFlags.Biotech, Is.True);
            Assert.That(observation.Revalidate(), Is.False);
            foreign.Unpatch(callback, HarmonyPatchType.Transpiler, foreign.Id); CoreFlags.SetForFixture(false);
            Assert.That(observation.Available, Is.True, observation.Failure);
        }
        finally
        {
            try { observation.Rollback(); }
            finally { foreign.Unpatch(callback, HarmonyPatchType.All, foreign.Id); foreach (var method in guard.Methods) foreign.Unpatch(method, HarmonyPatchType.All, foreign.Id); CoreFlags.SetForFixture(false); }
        }
    }

    private static Decay Fixture(bool stage = true) => new()
    {
        StoredRate = -.03f, Properties = new Properties(),
        Parent = new HediffWithComps { Pawn = new Pawn { Tracker = new AgeTracker(64800000L) }, Stage = stage ? new Stage { Factor = 1f } : null }
    };

    public sealed class Pawn { public AgeTracker Tracker = null!; public AgeTracker OtherTracker = new(-1); }
    public sealed class Stage { public float Factor; public float OtherFactor = 2f; }
    public class Hediff
    {
        public Pawn Pawn = null!;
        public Stage? Stage;
        public virtual Stage? CurStage { [MethodImpl(MethodImplOptions.NoInlining)] get => Stage; }
        public Stage? OtherStage { [MethodImpl(MethodImplOptions.NoInlining)] get => new Stage { Factor = 2f }; }
    }
    public class HediffWithComps : Hediff { }
    public sealed class MutatingStage : HediffWithComps
    {
        public Decay Comp = null!;
        public override Stage? CurStage { [MethodImpl(MethodImplOptions.NoInlining)] get { Comp.StoredRate = -.12f; return Stage; } }
    }
    public class Comp
    {
        public HediffWithComps Parent = null!;
        public HediffWithComps OtherParent = new() { Stage = new Stage { Factor = 2f } };
        public object Properties = null!;
        public Pawn Pawn { [MethodImpl(MethodImplOptions.NoInlining)] get => Parent.Pawn; }
        public Pawn OtherPawn { [MethodImpl(MethodImplOptions.NoInlining)] get => new Pawn { Tracker = new AgeTracker(-1) }; }
        [MethodImpl(MethodImplOptions.NoInlining)] public virtual float Rate() => 0;
    }
    public sealed class Properties { public float MinAge; public float OtherMinimum = 100f; public float MechanitorFactor = 1f; }
    public sealed class Decay : Comp
    {
        public float StoredRate;
        public float OtherRate = -.12f;
        private Properties Props { [MethodImpl(MethodImplOptions.NoInlining)] get => (Properties)Properties; }
        private Properties OtherProperties { [MethodImpl(MethodImplOptions.NoInlining)] get => new() { MinAge = 100f }; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public override float Rate()
        {
            if (Pawn.Tracker.Years < Props.MinAge) return 0f;
            float rate = StoredRate * (Parent.CurStage?.Factor ?? 1f);
            if (CoreFlags.Biotech && IsMechanitor(Pawn)) rate *= Props.MechanitorFactor;
            return rate;
        }
        [MethodImpl(MethodImplOptions.NoInlining)] private static bool IsMechanitor(Pawn pawn) => throw new InvalidOperationException("Unproved Biotech tail reached.");
    }

    public enum LeafMutation { OtherTicks, Divisor, DoubleConversion, Return, OtherFlag, FlagReturn }
    public enum RateMutation { OtherPawn, OtherAge, OtherProperties, OtherStage, OtherFlag, OtherTracker, OtherRate, OtherParent,
        OtherMinimum, OtherFactor, AgeBranch, ZeroReturn, NullFactor, StageBranch, FlagBranch, OtherLocal, DoubleStore, RateAddressWrite }
    public static class Effects
    {
        public static LeafMutation LeafChange;
        public static RateMutation RateChange;
        public static int Observed;
        [MethodImpl(MethodImplOptions.NoInlining)] public static void Observe() => Observed++;
        [MethodImpl(MethodImplOptions.NoInlining)] public static void WriteFlag() => CoreFlags.Active = true;
        public static IEnumerable<CodeInstruction> Nops(IEnumerable<CodeInstruction> source)
        {
            foreach (var instruction in source)
            {
                var copy = new CodeInstruction(instruction); var nop = new CodeInstruction(OpCodes.Nop);
                nop.labels.AddRange(copy.labels); copy.labels.Clear(); yield return nop; yield return copy;
            }
        }
        public static IEnumerable<CodeInstruction> ObserverWritesFlag(IEnumerable<CodeInstruction> source) =>
            new[] { new CodeInstruction(OpCodes.Ldc_I4_1), new CodeInstruction(OpCodes.Stsfld, Field(typeof(CoreFlags), nameof(CoreFlags.Active))), new CodeInstruction(OpCodes.Ret) };
        public static IEnumerable<CodeInstruction> ChangeRate(IEnumerable<CodeInstruction> source, ILGenerator generator)
        {
            var code = source.Select(i => new CodeInstruction(i)).ToList(); var bindings = Bindings();
            void ReplaceMethod(MethodInfo old, MethodInfo replacement) => code.First(i => Equals(i.operand, old)).operand = replacement;
            void ReplaceField(FieldInfo old, FieldInfo replacement) => code.First(i => Equals(i.operand, old)).operand = replacement;
            switch (RateChange)
            {
                case RateMutation.OtherPawn: ReplaceMethod(bindings.PawnGetter, Method(typeof(Comp), "get_OtherPawn")); break;
                case RateMutation.OtherAge: ReplaceMethod(bindings.AgeYearsGetter, Method(typeof(AgeTracker), "get_OtherYears")); break;
                case RateMutation.OtherProperties: ReplaceMethod(bindings.PropertiesGetter, Method(typeof(Decay), "get_OtherProperties")); break;
                case RateMutation.OtherStage: ReplaceMethod(bindings.StageGetter, Method(typeof(Hediff), "get_OtherStage")); break;
                case RateMutation.OtherFlag: ReplaceMethod(bindings.BiotechGetter, Method(typeof(CoreFlags), "get_OtherBiotech")); break;
                case RateMutation.OtherTracker: ReplaceField(bindings.AgeTracker, Field(typeof(Pawn), nameof(Pawn.OtherTracker))); break;
                case RateMutation.OtherRate: ReplaceField(bindings.StoredRate, Field(typeof(Decay), nameof(Decay.OtherRate))); break;
                case RateMutation.OtherParent: ReplaceField(bindings.Parent, Field(typeof(Comp), nameof(Comp.OtherParent))); break;
                case RateMutation.OtherMinimum: ReplaceField(bindings.MinimumAge, Field(typeof(Properties), nameof(Properties.OtherMinimum))); break;
                case RateMutation.OtherFactor: ReplaceField(bindings.StageFactor, Field(typeof(Stage), nameof(Stage.OtherFactor))); break;
                case RateMutation.AgeBranch: code[7].opcode = OpCodes.Ble_Un_S; break;
                case RateMutation.ZeroReturn: code[8].operand = 42f; break;
                case RateMutation.NullFactor: code[18].operand = 2f; break;
                case RateMutation.StageBranch: code[16].opcode = OpCodes.Brfalse_S; break;
                case RateMutation.FlagBranch: code[24].opcode = OpCodes.Brtrue_S; break;
                case RateMutation.OtherLocal:
                    code[^2].opcode = OpCodes.Ldarg_0; code[^2].operand = null;
                    code.Insert(code.Count - 1, new(OpCodes.Ldfld, Field(typeof(Decay), nameof(Decay.OtherRate)))); break;
                case RateMutation.DoubleStore:
                    var local = generator.DeclareLocal(typeof(double));
                    foreach (var instruction in code.Where(i => i.IsStloc() || i.IsLdloc()))
                    { instruction.opcode = instruction.IsStloc() ? OpCodes.Stloc : OpCodes.Ldloc; instruction.operand = local; }
                    // Keep the consumed operation sequence intact: this must exercise the
                    // typed-local boundary, not fail early on an extra conversion opcode.
                    break;
                case RateMutation.RateAddressWrite:
                    var first = new CodeInstruction(OpCodes.Ldarg_0); first.labels.AddRange(code[10].labels); code[10].labels.Clear();
                    code.InsertRange(10, new[] { first, new CodeInstruction(OpCodes.Ldflda, bindings.StoredRate), new CodeInstruction(OpCodes.Ldc_R4, -.12f), new CodeInstruction(OpCodes.Stind_R4) }); break;
            }
            return code;
        }
        public static IEnumerable<CodeInstruction> LateRateRead(IEnumerable<CodeInstruction> source)
        {
            var code = source.Select(i => new CodeInstruction(i)).ToList();
            var read = code.GetRange(10, 2); code[12].labels.AddRange(read[0].labels); read[0].labels.Clear(); code.RemoveRange(10, 2);
            int multiply = code.FindIndex(i => i.opcode == OpCodes.Mul);
            read[0].labels.AddRange(code[multiply].labels); code[multiply].labels.Clear(); code.InsertRange(multiply, read);
            return code;
        }
        public static IEnumerable<CodeInstruction> ChangeLeaf(IEnumerable<CodeInstruction> source)
        {
            var code = source.Select(i => new CodeInstruction(i)).ToList();
            switch (LeafChange)
            {
                case LeafMutation.OtherTicks: code.Single(i => i.opcode == OpCodes.Ldfld).operand = Field(typeof(AgeTracker), nameof(AgeTracker.OtherTicks)); break;
                case LeafMutation.Divisor: code.Single(i => i.opcode == OpCodes.Ldc_R4).operand = 1800000f; break;
                case LeafMutation.DoubleConversion:
                    code.Single(i => i.opcode == OpCodes.Conv_R4).opcode = OpCodes.Conv_R8;
                    var constant = code.Single(i => i.opcode == OpCodes.Ldc_R4); constant.opcode = OpCodes.Ldc_R8; constant.operand = 3600000d;
                    code.Insert(code.Count - 1, new(OpCodes.Conv_R4)); break;
                case LeafMutation.Return: return new[] { new CodeInstruction(OpCodes.Ldc_R4, 42f), new CodeInstruction(OpCodes.Ret) };
                case LeafMutation.OtherFlag: code.Single(i => i.opcode == OpCodes.Ldsfld).operand = Field(typeof(CoreFlags), nameof(CoreFlags.Other)); break;
                case LeafMutation.FlagReturn: return new[] { new CodeInstruction(OpCodes.Ldc_I4_1), new CodeInstruction(OpCodes.Ret) };
            }
            return code;
        }
    }
    public static class NoInitializerFlags
    {
        public static bool Active;
        public static bool Biotech { [MethodImpl(MethodImplOptions.NoInlining)] get => Active; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static void WriteOwnFlag() => Active = true;
    }
    public sealed class AgeTracker
    {
        private long ticks;
        public long OtherTicks;
        public AgeTracker(long initial) => ticks = initial;
        public void SetTicks(long value) => ticks = value;
        public float Years { [MethodImpl(MethodImplOptions.NoInlining)] get => (float)ticks / 3600000f; }
        public float OtherYears { [MethodImpl(MethodImplOptions.NoInlining)] get => 42f; }
    }
    public static class FixtureCounts { public static int Initializations; }
    public static class CoreFlags
    {
        public static bool Active;
        public static bool Other;
        static CoreFlags() { Active = false; FixtureCounts.Initializations++; }
        public static void SetForFixture(bool active) => Active = active;
        public static bool Biotech { [MethodImpl(MethodImplOptions.NoInlining)] get => Active; }
        public static bool OtherBiotech { [MethodImpl(MethodImplOptions.NoInlining)] get => true; }
    }
}
