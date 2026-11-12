using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.DrugPolicyGeneration;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.DrugPolicyGeneration;

[TestFixture, NonParallelizable]
public sealed class DrugGenerationStageAdmissionTests
{
    private static MethodInfo Method(Type type, string name) => type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)!;
    private static FieldInfo Field(Type type, string name) => type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)!;
    private static DrugGenerationStageBodyGuard.Bindings Bindings() => new(Method(typeof(Hediff), "get_Severity"), Field(typeof(Hediff), "severity"));
    private static DrugGenerationObservedBodies Observe(DrugGenerationStageBodyGuard guard) =>
        new(guard.Methods.Select(method => new DrugGenerationObservedBodies.Root(method, body => guard.Validate(method, body))).ToArray());
    private static int Bits(float value) => BitConverter.SingleToInt32Bits(value);

    [TestCase(0f)] [TestCase(-0f)] [TestCase(.35f)] [TestCase(float.MaxValue)]
    public void ActualSeverityLeafReturnsThisHediffsCurrentSingleValue(float initial)
    {
        var guard = new DrugGenerationStageBodyGuard(Bindings()); var observation = Observe(guard); var hediff = new Hediff(initial);
        try
        {
            observation.Install(); Assert.That(observation.Available, Is.True, observation.Failure);
            Assert.That(Bits(hediff.Severity), Is.EqualTo(Bits(initial)));
            hediff.SetSeverity(.75f); Assert.That(Bits(hediff.Severity), Is.EqualTo(Bits(.75f)));
        }
        finally { observation.Rollback(); }
    }

    [TestCase(SeverityMutation.OtherField)] [TestCase(SeverityMutation.OtherReceiver)]
    [TestCase(SeverityMutation.Escape)] [TestCase(SeverityMutation.Write)]
    public void ChangedSeverityReadOrReferenceEffectsExecuteAndRevokeTheLeaf(SeverityMutation mutation)
    {
        var bindings = Bindings(); var guard = new DrugGenerationStageBodyGuard(bindings); var observation = Observe(guard);
        var foreign = new HarmonyLib.Harmony("tests.generation.stage-severity-change"); var hediff = new Hediff(.35f);
        Effects.Change = mutation; Effects.Other = new Hediff(.5f); Effects.Escaped = null;
        try
        {
            observation.Install(); foreign.Patch(bindings.SeverityGetter, transpiler: new HarmonyMethod(Method(typeof(Effects), nameof(Effects.ChangeSeverity))));
            float expected = mutation switch { SeverityMutation.OtherField => 1.25f, SeverityMutation.OtherReceiver => .5f, SeverityMutation.Write => .75f, _ => .35f };
            Assert.That(Bits(hediff.Severity), Is.EqualTo(Bits(expected)));
            Assert.That(Effects.Escaped, Is.SameAs(mutation == SeverityMutation.Escape ? hediff : null));
            Assert.That(observation.Available, Is.False); Assert.That(observation.Failure, Does.Contain("get_Severity"));
            foreign.Unpatch(bindings.SeverityGetter, HarmonyPatchType.Transpiler, foreign.Id);
            Assert.That(Bits(hediff.Severity), Is.EqualTo(Bits(mutation == SeverityMutation.Write ? .75f : .35f)));
            Assert.That(observation.Available, Is.True, observation.Failure);
        }
        finally
        {
            try { observation.Rollback(); }
            finally { foreign.Unpatch(bindings.SeverityGetter, HarmonyPatchType.All, foreign.Id); Effects.Other = null; Effects.Escaped = null; }
        }
    }

    [Test]
    public void NopsAndAScalarObserverRemainPositiveUntilTheObserverGainsAReferenceWrite()
    {
        var bindings = Bindings(); var guard = new DrugGenerationStageBodyGuard(bindings); var observation = Observe(guard);
        var foreign = new HarmonyLib.Harmony("tests.generation.stage-severity-observer"); var callback = Method(typeof(Effects), nameof(Effects.Observe));
        Effects.Seen = 0; Effects.Escaped = null; Effects.Other = new Hediff(.2f);
        try
        {
            foreign.Patch(bindings.SeverityGetter, transpiler: new HarmonyMethod(Method(typeof(Effects), nameof(Effects.Nops))));
            observation.Install(); foreign.Patch(bindings.SeverityGetter, postfix: new HarmonyMethod(callback));
            Assert.That(observation.Available, Is.False); Assert.That(Effects.Seen, Is.Zero);
            Assert.That(observation.Revalidate(), Is.True, observation.Failure); Assert.That(Effects.Seen, Is.Zero);
            Assert.That(Bits(new Hediff(.35f).Severity), Is.EqualTo(Bits(.35f))); Assert.That(Effects.Seen, Is.EqualTo(1));
            foreign.Patch(callback, transpiler: new HarmonyMethod(Method(typeof(Effects), nameof(Effects.ObserverWritesReference))));
            Assert.That(observation.Available, Is.False); Assert.That(Effects.Escaped, Is.Null);
            Assert.That(Bits(new Hediff(.35f).Severity), Is.EqualTo(Bits(.35f)));
            Assert.That(Effects.Escaped, Is.SameAs(Effects.Other));
            Assert.That(observation.Revalidate(), Is.False);
            foreign.Unpatch(callback, HarmonyPatchType.Transpiler, foreign.Id);
            Assert.That(observation.Available, Is.True, observation.Failure);
        }
        finally
        {
            try { observation.Rollback(); }
            finally { foreign.Unpatch(callback, HarmonyPatchType.All, foreign.Id); foreign.Unpatch(bindings.SeverityGetter, HarmonyPatchType.All, foreign.Id); Effects.Escaped = null; Effects.Other = null; }
        }
    }

    public enum SeverityMutation { OtherField, OtherReceiver, Escape, Write }
    public static class Effects
    {
        public static SeverityMutation Change;
        public static Hediff? Other, Escaped;
        public static int Seen;
        [MethodImpl(MethodImplOptions.NoInlining)] public static void Observe() => Seen++;
        public static IEnumerable<CodeInstruction> ChangeSeverity(IEnumerable<CodeInstruction> source)
        {
            var code = source.Select(instruction => new CodeInstruction(instruction)).ToList();
            switch (Change)
            {
                case SeverityMutation.OtherField: code[1].operand = Field(typeof(Hediff), nameof(Hediff.OtherSeverity)); break;
                case SeverityMutation.OtherReceiver: code[0].opcode = OpCodes.Ldsfld; code[0].operand = Field(typeof(Effects), nameof(Other)); break;
                case SeverityMutation.Escape: code.InsertRange(0, new[] { new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Stsfld, Field(typeof(Effects), nameof(Escaped))) }); break;
                case SeverityMutation.Write: code.InsertRange(0, new[] { new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldc_R4, .75f), new CodeInstruction(OpCodes.Stfld, Field(typeof(Hediff), "severity")) }); break;
            }
            return code;
        }
        public static IEnumerable<CodeInstruction> Nops(IEnumerable<CodeInstruction> source, ILGenerator generator)
        {
            var nop = new CodeInstruction(OpCodes.Nop); nop.labels.Add(generator.DefineLabel());
            return new[] { nop }.Concat(source);
        }
        public static IEnumerable<CodeInstruction> ObserverWritesReference(IEnumerable<CodeInstruction> source) =>
            new[] { new CodeInstruction(OpCodes.Ldsfld, Field(typeof(Effects), nameof(Other))), new CodeInstruction(OpCodes.Stsfld, Field(typeof(Effects), nameof(Escaped))), new CodeInstruction(OpCodes.Ret) };
    }

    public class Hediff
    {
        private float severity;
        public float OtherSeverity = 1.25f;
        public Hediff(float value) => severity = value;
        public void SetSeverity(float value) => severity = value;
        public virtual float Severity { [MethodImpl(MethodImplOptions.NoInlining)] get => severity; }
    }
}
