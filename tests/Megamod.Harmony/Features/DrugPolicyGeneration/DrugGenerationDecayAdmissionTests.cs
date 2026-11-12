using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.DrugPolicyGeneration;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.DrugPolicyGeneration;

[TestFixture, NonParallelizable]
public sealed class DrugGenerationDecayAdmissionTests
{
    private static MethodInfo Method(Type type, string name) => type.GetMethod(name, BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)!;
    private static MethodInfo Target => Method(typeof(Decay), nameof(Decay.Contribute));
    private static DrugGenerationDecayAdmission Create() => new(Target, new(Target, typeof(Comp).GetProperty(nameof(Comp.Pawn))!.GetMethod!,
        Method(typeof(Clock), nameof(Clock.IsInterval)), Method(typeof(Decay), nameof(Decay.Rate))));

    [TestCase(-0.016f, 0.125f)]
    [TestCase(-0.015f, 1.0000001f)]
    [TestCase(-0.020f, 0f)]
    public void ComposedContributionUsesTheSamePawnDeltaAndExactFloatAccumulator(float rate, float initial)
    {
        var admission = Create(); var reports = new List<string>();
        try
        {
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(new("decay", AdmissionPhase.Early,
                new[] { admission.Candidate }), AdmissionPhase.Early), Is.True, string.Join("; ", reports));
            Assert.That(admission.Available, Is.True, admission.Failure);
            var pawn = new Actor { Due = true }; var comp = new ConcreteDecay(pawn, rate);
            float accumulator = initial; comp.Contribute(ref accumulator, 11);
            float delta = rate * 0.0033333334f;
            Assert.That(BitConverter.SingleToInt32Bits(accumulator), Is.EqualTo(BitConverter.SingleToInt32Bits(initial + delta)));
            Assert.That(Clock.LastActor, Is.SameAs(pawn)); Assert.That(Clock.LastInterval, Is.EqualTo(200));
            Assert.That(Clock.LastDelta, Is.EqualTo(11)); Assert.That(comp.Calls, Is.EqualTo(1));
            pawn.Due = false; accumulator = initial; comp.Contribute(ref accumulator, 7);
            Assert.That(BitConverter.SingleToInt32Bits(accumulator), Is.EqualTo(BitConverter.SingleToInt32Bits(initial)));
            Assert.That(comp.Calls, Is.EqualTo(1));
        }
        finally { admission.Candidate.Rollback(); Clock.LastActor = null; }
    }

    [Test]
    public void BenignObserverRequiresExplicitAcquisitionAndDoesNotRunDuringInspection()
    {
        var admission = Create(); var foreign = new HarmonyLib.Harmony("tests.decay.observer");
        var callback = Method(typeof(Counters), nameof(Counters.Observe)); Counters.Observed = 0;
        try
        {
            admission.Candidate.Install();
            foreign.Patch(Target, postfix: new HarmonyMethod(callback));
            Assert.That(admission.Available, Is.False);
            Assert.That(HarmonyPublication.Inspect(callback, snapshot => snapshot.Transpilers.Length == 0), Is.True);
            Assert.That(admission.Revalidate(), Is.True, admission.Failure);
            Assert.That(admission.Available, Is.True, admission.Failure);
            Assert.That(Counters.Observed, Is.Zero);
            float accumulator = 0; new ConcreteDecay(new Actor { Due = true }, -.016f).Contribute(ref accumulator, 1);
            Assert.That(Counters.Observed, Is.EqualTo(1));
        }
        finally { try { admission.Candidate.Rollback(); } finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); Clock.LastActor = null; } }
        Assert.That(HarmonyPublication.Inspect(callback, snapshot => snapshot.Transpilers.All(p => p.owner != OwnedHarmonyPatch.OwnerId)), Is.True);
    }

    public static class Counters
    {
        public static int Observed;
        [MethodImpl(MethodImplOptions.NoInlining)] public static void Observe() => Observed++;
    }

    [TestCase(nameof(Effects.ChangeMultiplier))]
    [TestCase(nameof(Effects.RedirectAccumulator))]
    [TestCase(nameof(Effects.DuplicateContribution))]
    public void ChangedComposedContributionIsRefusedAndRemovalRestoresIt(string transpiler)
    {
        var admission = Create(); var foreign = new HarmonyLib.Harmony("tests.decay.changed-flow");
        Effects.Accumulator = 0; Effects.Compositions = 0;
        try
        {
            admission.Candidate.Install();
            foreign.Patch(Target, transpiler: new HarmonyMethod(Method(typeof(Effects), transpiler)));
            int compositions = Effects.Compositions;
            Assert.That(admission.Available, Is.False); Assert.That(admission.Revalidate(), Is.False);
            Assert.That(admission.Failure, Does.Contain(typeof(Decay).FullName));
            Assert.That(Effects.Compositions, Is.EqualTo(compositions), "Inspection must not rerun the foreign transpiler.");
            float accumulator = .125f; var comp = new ConcreteDecay(new Actor { Due = true }, -.016f);
            comp.Contribute(ref accumulator, 9);
            float ordinaryDelta = -.016f * .0033333334f;
            Assert.That(BitConverter.SingleToInt32Bits(accumulator), Is.Not.EqualTo(BitConverter.SingleToInt32Bits(.125f + ordinaryDelta)));
            if (transpiler == nameof(Effects.RedirectAccumulator)) Assert.That(Effects.Accumulator, Is.Not.Zero);
            if (transpiler == nameof(Effects.DuplicateContribution)) Assert.That(comp.Calls, Is.EqualTo(2));
            foreign.Unpatch(Target, HarmonyPatchType.Transpiler, foreign.Id);
            Assert.That(admission.Available, Is.True, admission.Failure);
        }
        finally { try { admission.Candidate.Rollback(); } finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); Clock.LastActor = null; } }
    }

    [Test]
    public void CallbackBodyRepatchRevokesTheReceiptAndRemovedCallbacksReleaseTheirObservers()
    {
        var admission = Create(); var foreign = new HarmonyLib.Harmony("tests.decay.callback-repatch");
        var callback = Method(typeof(Counters), nameof(Counters.Observe));
        try
        {
            foreign.Patch(Target, postfix: new HarmonyMethod(callback)); admission.Candidate.Install();
            Assert.That(admission.Available, Is.True, admission.Failure);
            foreign.Patch(callback, transpiler: new HarmonyMethod(Method(typeof(Effects), nameof(Effects.MutateActorBody))));
            Assert.That(admission.Available, Is.False, "A retained callback signature cannot authorize its changed native-reference effects.");
            Effects.Actor = new Actor { Due = true }; float accumulator = 0;
            new ConcreteDecay(Effects.Actor, -.016f).Contribute(ref accumulator, 1);
            Assert.That(Effects.Actor.Due, Is.False);
            foreign.Unpatch(callback, HarmonyPatchType.Transpiler, foreign.Id);
            Assert.That(admission.Available, Is.True, admission.Failure);
            foreign.Unpatch(Target, HarmonyPatchType.Postfix, foreign.Id);
            Assert.That(admission.Revalidate(), Is.True, admission.Failure);
            Assert.That(HarmonyPublication.Inspect(callback, snapshot => snapshot.Transpilers.All(p => p.owner != OwnedHarmonyPatch.OwnerId)), Is.True);
        }
        finally
        {
            Effects.Actor = null; Clock.LastActor = null;
            try { admission.Candidate.Rollback(); }
            finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); foreign.Unpatch(callback, HarmonyPatchType.All, foreign.Id); }
        }
    }

    [Test]
    public void FailedRevalidationRemovesItsNewCallbackObserverWithoutRemovingTheForeignPatch()
    {
        var admission = Create(); var foreign = new HarmonyLib.Harmony("tests.decay.failed-observer");
        var callback = Method(typeof(Effects), nameof(Effects.MutateActor));
        try
        {
            admission.Candidate.Install(); foreign.Patch(Target, postfix: new HarmonyMethod(callback));
            Assert.That(admission.Revalidate(), Is.False);
            Assert.That(HarmonyPublication.Inspect(callback, snapshot => snapshot.Transpilers.All(p => p.owner != OwnedHarmonyPatch.OwnerId)), Is.True);
            Assert.That(HarmonyPublication.Inspect(Target, snapshot => snapshot.Postfixes.Any(p => p.Method == callback)), Is.True);
            foreign.Unpatch(Target, HarmonyPatchType.Postfix, foreign.Id);
            Assert.That(admission.Available, Is.True, admission.Failure);
        }
        finally { try { admission.Candidate.Rollback(); } finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); } }
    }

    [Test]
    public void SelectedInnerScalarObserverIsAdmitted()
    {
        var admission = Create(); var foreign = new HarmonyLib.Harmony("tests.decay.inner"); Counters.Observed = 0;
        try
        {
            foreign.CreateProcessor(Target).AddInnerPostfix(new HarmonyMethod(Method(typeof(Counters), nameof(Counters.Observe)))
                { innerTarget = InnerTarget.Constant(.0033333334f) }).Patch();
            admission.Candidate.Install(); Assert.That(admission.Available, Is.True, admission.Failure);
            Assert.That(Counters.Observed, Is.Zero);
            float accumulator = 0; new ConcreteDecay(new Actor { Due = true }, -.016f).Contribute(ref accumulator, 1);
            Assert.That(Counters.Observed, Is.EqualTo(1));
        }
        finally { try { admission.Candidate.Rollback(); } finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); Clock.LastActor = null; } }
    }

    [Test]
    public void AccumulatorReadMustFollowTheRateCallAndProductRounding()
    {
        var admission = Create(); var foreign = new HarmonyLib.Harmony("tests.decay.early-read");
        var comp = new MutatingDecay(new Actor { Due = true });
        try
        {
            admission.Candidate.Install();
            comp.Accumulator = .125f; comp.Contribute(ref comp.Accumulator, 1);
            float ordinary = comp.Accumulator;
            foreign.Patch(Target, transpiler: new HarmonyMethod(Method(typeof(Effects), nameof(Effects.ReadBeforeRate))));
            comp.Accumulator = .125f; comp.Contribute(ref comp.Accumulator, 1);
            Assert.That(comp.Accumulator, Is.Not.EqualTo(ordinary), "The early read loses the value written by the actual virtual rate call.");
            Assert.That(admission.Available, Is.False);
        }
        finally { try { admission.Candidate.Rollback(); } finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); Clock.LastActor = null; } }
    }

    [Test]
    public void RemovingTheNativeSingleRateStoreIsNotAssumedPrecisionEquivalent()
    {
        var admission = Create(); var foreign = new HarmonyLib.Harmony("tests.decay.rate-precision");
        try
        {
            admission.Candidate.Install();
            foreign.Patch(Target, transpiler: new HarmonyMethod(Method(typeof(Effects), nameof(Effects.RemoveRateStore))));
            var comp = new ConcreteDecay(new Actor { Due = true }, -.016f); float accumulator = .125f;
            comp.Contribute(ref accumulator, 1); Assert.That(comp.Calls, Is.EqualTo(1), "The rewrite is executable; host equality would not prove native precision equivalence.");
            Assert.That(admission.Available, Is.False);
        }
        finally { try { admission.Candidate.Rollback(); } finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); Clock.LastActor = null; } }
    }

    [Test]
    public void ClosedGenericRateDoesNotMatchTheNativeNongenericCallableContract()
    {
        var target = Method(typeof(GenericDecay), nameof(GenericDecay.Contribute));
        var rate = Method(typeof(GenericDecay), nameof(GenericDecay.Rate)).MakeGenericMethod(typeof(int));
        var admission = new DrugGenerationDecayAdmission(target, new(target,
            typeof(Comp).GetProperty(nameof(Comp.Pawn))!.GetMethod!, Method(typeof(Clock), nameof(Clock.IsInterval)), rate));
        var comp = new GenericDecay(new Actor());
        Assert.That(comp.Rate<int>(), Is.Not.EqualTo(comp.Rate<string>()), "One generic definition can have different constructed behavior.");
        Assert.That(admission.Candidate.Validate(), Is.Not.Null, "Native contribution bindings are nongeneric; module/token identity cannot erase method arguments.");
    }

    public static class Effects
    {
        public static float Accumulator;
        public static int Compositions;
        public static Actor? Actor;
        public static IEnumerable<CodeInstruction> ChangeMultiplier(IEnumerable<CodeInstruction> instructions)
        {
            Compositions++;
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldc_R4 && Equals(instruction.operand, .0033333334f)) instruction.operand = .005f;
                yield return instruction;
            }
        }
        public static IEnumerable<CodeInstruction> RedirectAccumulator(IEnumerable<CodeInstruction> instructions)
        {
            Compositions++;
            foreach (var instruction in instructions)
            {
                if (instruction.IsLdarg() && instruction.ArgumentIndex() == 1)
                { instruction.opcode = OpCodes.Ldsflda; instruction.operand = typeof(Effects).GetField(nameof(Accumulator))!; }
                yield return instruction;
            }
        }
        public static IEnumerable<CodeInstruction> DuplicateContribution(IEnumerable<CodeInstruction> instructions)
        {
            Compositions++; var body = instructions.ToList();
            int branch = body.FindIndex(i => i.opcode == OpCodes.Brfalse || i.opcode == OpCodes.Brfalse_S);
            var duplicate = body.Skip(branch + 1).TakeWhile(i => i.opcode != OpCodes.Ret).Select(i => new CodeInstruction(i.opcode, i.operand)).ToArray();
            body.InsertRange(body.FindIndex(i => i.opcode == OpCodes.Ret), duplicate); return body;
        }
        public static IEnumerable<CodeInstruction> MutateActorBody(IEnumerable<CodeInstruction> instructions) =>
            PatchProcessor.GetOriginalInstructions(Method(typeof(Effects), nameof(MutateActor)));
        [MethodImpl(MethodImplOptions.NoInlining)] public static void MutateActor() => Actor!.Due = false;
        public static IEnumerable<CodeInstruction> ReadBeforeRate(IEnumerable<CodeInstruction> instructions)
        {
            var body = instructions.ToList(); int read = body.FindIndex(i => i.opcode == OpCodes.Ldind_R4);
            var addressAndRead = body.GetRange(read - 2, 3); body.RemoveRange(read - 2, 3);
            int branch = body.FindIndex(i => i.opcode == OpCodes.Brfalse || i.opcode == OpCodes.Brfalse_S);
            body.InsertRange(branch + 1, addressAndRead); return body;
        }
        public static IEnumerable<CodeInstruction> RemoveRateStore(IEnumerable<CodeInstruction> instructions)
        {
            var body = instructions.ToList(); int store = body.FindIndex(i => i.IsStloc());
            Assert.That(body[store + 1].IsLdloc(), Is.True); body.RemoveRange(store, 2); return body;
        }
    }

    public sealed class Actor { public bool Due; }
    public class Comp
    {
        private readonly Actor pawn;
        public Comp(Actor pawn) => this.pawn = pawn;
        public Actor Pawn { [MethodImpl(MethodImplOptions.NoInlining)] get => pawn; }
    }
    public abstract class Decay : Comp
    {
        protected Decay(Actor pawn) : base(pawn) { }
        public abstract float Rate();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public virtual void Contribute(ref float accumulator, int delta)
        {
            if (Clock.IsInterval(Pawn, 200, delta))
            {
                float amount = Rate(); amount *= 0.0033333334f; accumulator += amount;
            }
        }
    }
    public sealed class ConcreteDecay : Decay
    {
        private readonly float rate;
        public int Calls;
        public ConcreteDecay(Actor pawn, float rate) : base(pawn) => this.rate = rate;
        [MethodImpl(MethodImplOptions.NoInlining)] public override float Rate() { Calls++; return rate; }
    }
    public sealed class MutatingDecay : Decay
    {
        public float Accumulator;
        public MutatingDecay(Actor pawn) : base(pawn) { }
        [MethodImpl(MethodImplOptions.NoInlining)] public override float Rate() { Accumulator = .5f; return -.016f; }
    }
    public class GenericDecay : Comp
    {
        public GenericDecay(Actor pawn) : base(pawn) { }
        [MethodImpl(MethodImplOptions.NoInlining)] public virtual float Rate<T>() => typeof(T) == typeof(int) ? -.016f : -.032f;
        [MethodImpl(MethodImplOptions.NoInlining)] public virtual void Contribute(ref float accumulator, int delta)
        {
            if (Clock.IsInterval(Pawn, 200, delta))
            { float amount = Rate<int>(); amount *= .0033333334f; accumulator += amount; }
        }
    }
    public static class Clock
    {
        public static Actor? LastActor;
        public static int LastInterval, LastDelta;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool IsInterval(Actor actor, int interval, int delta)
        { LastActor = actor; LastInterval = interval; LastDelta = delta; return actor.Due; }
    }
}
