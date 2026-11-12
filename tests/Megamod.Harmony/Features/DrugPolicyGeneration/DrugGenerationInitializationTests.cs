using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.DrugPolicyGeneration;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.DrugPolicyGeneration;

[TestFixture, NonParallelizable]
public sealed class DrugGenerationInitializationTests
{
    private static MethodInfo Method(Type type, string name) => type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)!;
    private static FieldInfo Field(Type type, string name) => type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)!;
    private static DrugGenerationSeverityInitializationGuard.Bindings Bindings() => new(
        Method(typeof(Decay), nameof(Decay.Initialize)), Method(typeof(Comp), nameof(Comp.Initialize)), Method(typeof(Decay), "get_ActualProperties"),
        Method(typeof(Properties), nameof(Properties.Calculate)), Method(typeof(Interval), "get_Random"), Method(typeof(RandomHelpers), nameof(RandomHelpers.Range)),
        Method(typeof(RandomHelpers), nameof(RandomHelpers.Chance)), Field(typeof(Comp), nameof(Comp.Properties)), Field(typeof(Decay), nameof(Decay.StoredRate)),
        Field(typeof(Properties), nameof(Properties.Rate)), Field(typeof(Properties), nameof(Properties.Range)), Field(typeof(Interval), nameof(Interval.Min)),
        Field(typeof(Interval), nameof(Interval.Max)), Field(typeof(Properties), nameof(Properties.Reversal)));

    private static DrugGenerationObservedBodies Observe(DrugGenerationSeverityInitializationGuard guard, params MethodInfo[] methods) =>
        new(methods.Select(method => new DrugGenerationObservedBodies.Root(method, body => guard.Validate(method, body))).ToArray());
    private static float Zero(bool negative) => negative ? BitConverter.Int32BitsToSingle(int.MinValue) : 0f;
    private static int Bits(float value) => BitConverter.SingleToInt32Bits(value);

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public void ActualZeroHelperPathsReturnTheOriginalMinimumAndFalseWithoutEnteringRandomTail(bool negativeMinimum, bool negativeMaximum)
    {
        float minimum = Zero(negativeMinimum), maximum = Zero(negativeMaximum);
        var bindings = Bindings(); var guard = new DrugGenerationSeverityInitializationGuard(bindings);
        var observation = Observe(guard, bindings.Range, bindings.Chance);
        try
        {
            observation.Install(); Assert.That(observation.Available, Is.True, observation.Failure);
            Assert.That(Bits(RandomHelpers.Range(minimum, maximum)), Is.EqualTo(Bits(minimum)));
            Assert.That(RandomHelpers.Chance(minimum), Is.False);
        }
        finally { observation.Rollback(); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ActualZeroHelperChangesRevokeTheProducerReceipt(bool chance)
    {
        var bindings = Bindings(); var guard = new DrugGenerationSeverityInitializationGuard(bindings);
        var observation = Observe(guard, bindings.Range, bindings.Chance);
        var target = chance ? bindings.Chance : bindings.Range;
        var foreign = new HarmonyLib.Harmony("tests.generation.initialization-zero-helper");
        try
        {
            observation.Install(); Assert.That(observation.Available, Is.True, observation.Failure);
            foreign.Patch(target, transpiler: new HarmonyMethod(Method(typeof(Effects), chance ? nameof(Effects.TrueChance) : nameof(Effects.NonzeroRange))));
            if (chance) Assert.That(RandomHelpers.Chance(0f), Is.True);
            else Assert.That(RandomHelpers.Range(0f, 0f), Is.EqualTo(.25f));
            Assert.That(observation.Available, Is.False); Assert.That(observation.Failure, Does.Contain(target.Name));
            foreign.Unpatch(target, HarmonyPatchType.Transpiler, foreign.Id);
            Assert.That(observation.Available, Is.True, observation.Failure);
        }
        finally { try { observation.Rollback(); } finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); } }
    }

    [Test]
    public void CalculatorReturnsItsStoredSingleSumWithoutSamplingTheRandomTail()
    {
        var bindings = Bindings(); var guard = new DrugGenerationSeverityInitializationGuard(bindings);
        var observation = Observe(guard, bindings.Range, bindings.Chance, bindings.RangeGetter, bindings.Calculate);
        try
        {
            observation.Install(); Assert.That(observation.Available, Is.True, observation.Failure);
            for (int zeroSigns = 0; zeroSigns < 8; zeroSigns++)
            {
                var props = new Properties { Rate = -.03f, Range = new() { Min = Zero((zeroSigns & 1) != 0), Max = Zero((zeroSigns & 2) != 0) }, Reversal = Zero((zeroSigns & 4) != 0) };
                Assert.That(Bits(props.Calculate()), Is.EqualTo(Bits(props.Rate)));
            }
        }
        finally { observation.Rollback(); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void AChangedZeroBranchEntersTheRandomTailAndRevokesAdmission(bool chance)
    {
        var bindings = Bindings(); var guard = new DrugGenerationSeverityInitializationGuard(bindings);
        var target = chance ? bindings.Chance : bindings.Range; var observation = Observe(guard, target);
        var foreign = new HarmonyLib.Harmony("tests.generation.initialization-zero-branch");
        try
        {
            observation.Install(); foreign.Patch(target, transpiler: new HarmonyMethod(Method(typeof(Effects), nameof(Effects.EnterRandomTail))));
            Assert.Throws<InvalidOperationException>(() => { if (chance) RandomHelpers.Chance(0f); else RandomHelpers.Range(0f, 0f); });
            Assert.That(observation.Available, Is.False); Assert.That(observation.Failure, Does.Contain(target.Name));
        }
        finally { try { observation.Rollback(); } finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); } }
    }

    [TestCase(CalculatorMutation.OtherRate)]
    [TestCase(CalculatorMutation.RangeAddress)]
    [TestCase(CalculatorMutation.RemoveStore)]
    [TestCase(CalculatorMutation.OtherLocal)]
    [TestCase(CalculatorMutation.FlipBranch)]
    [TestCase(CalculatorMutation.LateRateRead)]
    public void ChangedCalculatorFlowIsExecutedAndRefused(CalculatorMutation mutation)
    {
        var bindings = Bindings(); var guard = new DrugGenerationSeverityInitializationGuard(bindings);
        var observation = Observe(guard, bindings.Range, bindings.Chance, bindings.RangeGetter, bindings.Calculate);
        var foreign = new HarmonyLib.Harmony("tests.generation.initialization-calculator"); Effects.Change = mutation;
        try
        {
            observation.Install(); foreign.Patch(bindings.Calculate, transpiler: new HarmonyMethod(Method(typeof(Effects), nameof(Effects.ChangeCalculator))));
            var props = new Properties { Rate = -.03f, OtherRate = -.12f };
            float actual = props.Calculate();
            float expected = mutation switch { CalculatorMutation.OtherRate or CalculatorMutation.OtherLocal => -.12f,
                CalculatorMutation.RangeAddress => (float)(-.03f + 1f), CalculatorMutation.FlipBranch => .03f, _ => -.03f };
            Assert.That(Bits(actual), Is.EqualTo(Bits(expected)));
            Assert.That(props.Range.Min, Is.EqualTo(mutation == CalculatorMutation.RangeAddress ? 1f : 0f));
            Assert.That(observation.Available, Is.False); Assert.That(observation.Failure, Does.Contain(nameof(Properties.Calculate)));
            foreign.Unpatch(bindings.Calculate, HarmonyPatchType.Transpiler, foreign.Id);
            Assert.That(observation.Available, Is.True, observation.Failure);
        }
        finally { try { observation.Rollback(); } finally { foreign.Unpatch(bindings.Calculate, HarmonyPatchType.All, foreign.Id); } }
    }

    [TestCase(0)] [TestCase(1)] [TestCase(2)] [TestCase(3)]
    [TestCase(4)] [TestCase(5)] [TestCase(6)] [TestCase(7)]
    public void InitializationStoresTheSameRateAndLeavesItsSourceObjectUnchanged(int zeroSigns)
    {
        var bindings = Bindings(); var guard = new DrugGenerationSeverityInitializationGuard(bindings);
        var observation = Observe(guard, guard.Methods);
        try
        {
            observation.Install(); Assert.That(observation.Available, Is.True, observation.Failure);
            // Declared host scalar examples, not loaded Core profiles or their native acceptance.
            foreach (float rate in new[] { -.03f, -.08f, -300f, -1e-20f })
            {
                var props = new Properties { Rate = rate, Range = new() { Min = Zero((zeroSigns & 1) != 0), Max = Zero((zeroSigns & 2) != 0) }, Reversal = Zero((zeroSigns & 4) != 0) };
                var comp = new Decay { Properties = props, StoredRate = 42f }; int? info = 17;
                comp.Initialize(info);
                Assert.That(Bits(comp.StoredRate), Is.EqualTo(Bits(rate)));
                Assert.That(comp.Properties, Is.SameAs(props)); Assert.That(info, Is.EqualTo(17));
                Assert.That(Bits(props.Rate), Is.EqualTo(Bits(rate)));
                Assert.That(Bits(props.Range.Min), Is.EqualTo(Bits(Zero((zeroSigns & 1) != 0))));
                Assert.That(Bits(props.Range.Max), Is.EqualTo(Bits(Zero((zeroSigns & 2) != 0))));
                Assert.That(Bits(props.Reversal), Is.EqualTo(Bits(Zero((zeroSigns & 4) != 0))));
            }
        }
        finally { observation.Rollback(); }
    }

    [Test]
    public void AHarmlessCalculatorObserverNeedsExplicitAcquisitionAndRemainsCurrent()
    {
        var bindings = Bindings(); var guard = new DrugGenerationSeverityInitializationGuard(bindings);
        var observation = Observe(guard, guard.Methods); var foreign = new HarmonyLib.Harmony("tests.generation.initialization-observer");
        var callback = Method(typeof(Effects), nameof(Effects.Observe)); Effects.Observed = 0; Effects.Escaped = null;
        try
        {
            observation.Install(); foreign.Patch(bindings.Calculate, postfix: new HarmonyMethod(callback));
            Assert.That(observation.Available, Is.False); Assert.That(Effects.Observed, Is.Zero);
            Assert.That(observation.Revalidate(), Is.True, observation.Failure); Assert.That(Effects.Observed, Is.Zero);
            var props = new Properties { Rate = -.03f }; var comp = new Decay { Properties = props };
            comp.Initialize(null); Assert.That(Effects.Observed, Is.EqualTo(1)); Assert.That(Bits(comp.StoredRate), Is.EqualTo(Bits(props.Rate)));
            foreign.Patch(callback, transpiler: new HarmonyMethod(Method(typeof(Effects), nameof(Effects.EscapeProperties))));
            Assert.That(observation.Available, Is.False); Assert.That(observation.Failure, Does.Contain(nameof(Effects.Observe)));
            Assert.That(Effects.Escaped, Is.Null); // Inspection must not invoke the changed callback.
            foreign.Unpatch(callback, HarmonyPatchType.Transpiler, foreign.Id);
            Assert.That(observation.Available, Is.True, observation.Failure);
            foreign.Unpatch(bindings.Calculate, HarmonyPatchType.Postfix, foreign.Id);
            Assert.That(observation.Revalidate(), Is.True, observation.Failure);
        }
        finally
        {
            try { observation.Rollback(); }
            finally { foreign.Unpatch(callback, HarmonyPatchType.All, foreign.Id); foreign.Unpatch(bindings.Calculate, HarmonyPatchType.All, foreign.Id); Effects.Escaped = null; }
        }
    }

    [TestCase(InitializationMutation.PropertiesProjection)]
    [TestCase(InitializationMutation.BaseMutation)]
    [TestCase(InitializationMutation.SkipBase)]
    [TestCase(InitializationMutation.SkipWrite)]
    [TestCase(InitializationMutation.DuplicateCalculation)]
    [TestCase(InitializationMutation.OtherComp)]
    [TestCase(InitializationMutation.OtherField)]
    public void ChangedInitializationExecutesItsActualReceiverAndCallEffectsBeforeRefusal(InitializationMutation mutation)
    {
        var bindings = Bindings(); var guard = new DrugGenerationSeverityInitializationGuard(bindings);
        var observation = Observe(guard, guard.Methods); var foreign = new HarmonyLib.Harmony("tests.generation.initialization-hook");
        var target = mutation == InitializationMutation.PropertiesProjection ? bindings.PropertiesGetter : mutation == InitializationMutation.BaseMutation ? bindings.BaseInitialize : bindings.Initialize;
        Effects.InitializationChange = mutation; Effects.BaseCalls = 0; Effects.Calculations = 0;
        Effects.OtherProperties = new Properties { Rate = -.12f }; Effects.OtherComp = new Decay { StoredRate = 99f };
        try
        {
            foreign.Patch(bindings.BaseInitialize, postfix: new HarmonyMethod(Method(typeof(Effects), nameof(Effects.CountBase))));
            foreign.Patch(bindings.Calculate, postfix: new HarmonyMethod(Method(typeof(Effects), nameof(Effects.CountCalculation))));
            observation.Install(); Assert.That(observation.Available, Is.True, observation.Failure);
            foreign.Patch(target, transpiler: new HarmonyMethod(Method(typeof(Effects), nameof(Effects.ChangeInitialization))));
            var props = new Properties { Rate = -.03f }; var comp = new Decay { Properties = props, StoredRate = 42f };
            comp.Initialize(17);
            float expected = mutation switch { InitializationMutation.PropertiesProjection or InitializationMutation.BaseMutation => -.12f,
                InitializationMutation.SkipWrite or InitializationMutation.OtherComp or InitializationMutation.OtherField => 42f, _ => -.03f };
            Assert.That(Bits(comp.StoredRate), Is.EqualTo(Bits(expected)));
            Assert.That(Effects.BaseCalls, Is.EqualTo(mutation == InitializationMutation.SkipBase ? 0 : 1));
            Assert.That(Effects.Calculations, Is.EqualTo(mutation == InitializationMutation.DuplicateCalculation ? 2 : 1));
            Assert.That(comp.Properties, Is.SameAs(mutation == InitializationMutation.BaseMutation ? Effects.OtherProperties : props));
            Assert.That(Effects.OtherComp.StoredRate, Is.EqualTo(mutation == InitializationMutation.OtherComp ? -.03f : 99f));
            Assert.That(comp.OtherStoredRate, Is.EqualTo(mutation == InitializationMutation.OtherField ? -.03f : 0f));
            Assert.That(observation.Available, Is.False); Assert.That(observation.Failure, Does.Contain(target.Name));
            foreign.Unpatch(target, HarmonyPatchType.Transpiler, foreign.Id);
            Assert.That(observation.Available, Is.True, observation.Failure);
        }
        finally
        {
            try { observation.Rollback(); }
            finally { foreach (var method in guard.Methods) foreign.Unpatch(method, HarmonyPatchType.All, foreign.Id); Effects.OtherProperties = null; Effects.OtherComp = null; }
        }
    }

    [Test]
    public void LabeledNopsAcrossAllSevenRootsPreserveTheActualResult()
    {
        var guard = new DrugGenerationSeverityInitializationGuard(Bindings()); var observation = Observe(guard, guard.Methods);
        var foreign = new HarmonyLib.Harmony("tests.generation.initialization-nops");
        try
        {
            observation.Install();
            foreach (var method in guard.Methods) foreign.Patch(method, transpiler: new HarmonyMethod(Method(typeof(Effects), nameof(Effects.Nops))));
            Assert.That(observation.Available, Is.True, observation.Failure);
            var comp = new Decay { Properties = new Properties { Rate = -.08f } }; comp.Initialize(null);
            Assert.That(Bits(comp.StoredRate), Is.EqualTo(Bits(-.08f)));
        }
        finally { try { observation.Rollback(); } finally { foreach (var method in guard.Methods) foreign.Unpatch(method, HarmonyPatchType.All, foreign.Id); } }
    }

    [Test]
    public void ARangeGetterWriteChangesTheActualPropertiesRangeAndRevokesAdmission()
    {
        var bindings = Bindings(); var guard = new DrugGenerationSeverityInitializationGuard(bindings);
        var observation = Observe(guard, bindings.Range, bindings.Chance, bindings.RangeGetter, bindings.Calculate);
        var foreign = new HarmonyLib.Harmony("tests.generation.initialization-range-address");
        try
        {
            observation.Install(); foreign.Patch(bindings.RangeGetter, transpiler: new HarmonyMethod(Method(typeof(Effects), nameof(Effects.WriteRange))));
            var props = new Properties { Rate = -.03f };
            Assert.That(Bits(props.Calculate()), Is.EqualTo(Bits((float)(-.03f + .5f)))); Assert.That(props.Range.Min, Is.EqualTo(.5f));
            Assert.That(observation.Available, Is.False); Assert.That(observation.Failure, Does.Contain(bindings.RangeGetter.Name));
        }
        finally { try { observation.Rollback(); } finally { foreign.Unpatch(bindings.RangeGetter, HarmonyPatchType.All, foreign.Id); } }
    }

    [Test]
    public void AnUnboundActualMethodDoesNotAcquireAnInitializationClaim()
    {
        var guard = new DrugGenerationSeverityInitializationGuard(Bindings()); var target = Method(typeof(Effects), nameof(Effects.Unbound));
        var observation = Observe(guard, target);
        try
        {
            Assert.Throws<InvalidOperationException>(observation.Install);
            Assert.That(Effects.Unbound(), Is.EqualTo(17f)); Assert.That(observation.Available, Is.False);
            Assert.That(observation.Failure, Does.Contain("not one of the seven"));
        }
        finally { observation.Rollback(); }
    }

    [TestCase("generic")]
    [TestCase("rate-reversal")]
    [TestCase("endpoints")]
    [TestCase("duplicate-method")]
    public void ChangedBoundCallableOrFieldRelationshipsAreRefused(string change)
    {
        var bindings = Bindings(); bindings = change switch
        {
            "generic" => bindings with { Range = Method(typeof(Effects), nameof(Effects.GenericRange)).MakeGenericMethod(typeof(int)) },
            "rate-reversal" => bindings with { Reversal = bindings.Rate },
            "endpoints" => bindings with { Maximum = bindings.Minimum },
            _ => bindings with { BaseInitialize = bindings.Initialize }
        };
        var guard = new DrugGenerationSeverityInitializationGuard(bindings);
        Assert.That(guard.ValidateShape(), Is.Not.Null);
    }

    [TestCase(CallEdge.Range)]
    [TestCase(CallEdge.Chance)]
    [TestCase(CallEdge.BaseInitialize)]
    [TestCase(CallEdge.PropertiesGetter)]
    [TestCase(CallEdge.Calculate)]
    public void SameShapedReplacementCallsExecuteAndRevokeTheConsumingRole(CallEdge edge)
    {
        var bindings = Bindings(); var guard = new DrugGenerationSeverityInitializationGuard(bindings);
        var observation = Observe(guard, guard.Methods); var foreign = new HarmonyLib.Harmony("tests.generation.initialization-call-edge");
        var target = edge == CallEdge.Range ? bindings.RangeGetter : edge == CallEdge.Chance ? bindings.Calculate : bindings.Initialize;
        Effects.ReplacementEdge = edge; Effects.OtherProperties = new Properties { Rate = -.12f };
        Effects.ReplacementReceiver = null; Effects.ReplacementInput = null;
        try
        {
            observation.Install(); Assert.That(observation.Available, Is.True, observation.Failure);
            foreign.Patch(target, transpiler: new HarmonyMethod(Method(typeof(Effects), nameof(Effects.ReplaceCall))));
            var comp = new Decay { Properties = new Properties { Rate = -.03f, OtherRate = -.12f }, StoredRate = 42f };
            comp.Initialize(17);
            float expected = edge switch { CallEdge.Range => (float)(-.03f + .25f), CallEdge.Chance => .03f,
                CallEdge.PropertiesGetter or CallEdge.Calculate => -.12f, _ => -.03f };
            Assert.That(Bits(comp.StoredRate), Is.EqualTo(Bits(expected)));
            Assert.That(Effects.ReplacementReceiver, Is.SameAs(edge == CallEdge.BaseInitialize ? comp : null));
            Assert.That(Effects.ReplacementInput, Is.EqualTo(edge == CallEdge.BaseInitialize ? (int?)17 : null));
            Assert.That(observation.Available, Is.False); Assert.That(observation.Failure, Does.Contain(target.Name));
            foreign.Unpatch(target, HarmonyPatchType.Transpiler, foreign.Id);
            Assert.That(observation.Available, Is.True, observation.Failure);
            Effects.ReplacementReceiver = null; Effects.ReplacementInput = null; comp.Initialize(29);
            Assert.That(Bits(comp.StoredRate), Is.EqualTo(Bits(-.03f)));
            Assert.That(Effects.ReplacementReceiver, Is.Null); Assert.That(Effects.ReplacementInput, Is.Null);
        }
        finally
        {
            try { observation.Rollback(); }
            finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); Effects.OtherProperties = null; Effects.ReplacementReceiver = null; Effects.ReplacementInput = null; }
        }
    }

    public enum CallEdge { Range, Chance, BaseInitialize, PropertiesGetter, Calculate }
    public enum InitializationMutation { PropertiesProjection, BaseMutation, SkipBase, SkipWrite, DuplicateCalculation, OtherComp, OtherField }
    public enum CalculatorMutation { OtherRate, RangeAddress, RemoveStore, OtherLocal, FlipBranch, LateRateRead }
    public static class Effects
    {
        public static CalculatorMutation Change;
        public static InitializationMutation InitializationChange;
        public static CallEdge ReplacementEdge;
        public static Comp? ReplacementReceiver;
        public static int? ReplacementInput;
        public static Properties? OtherProperties;
        public static Decay? OtherComp;
        public static int BaseCalls, Calculations;
        [MethodImpl(MethodImplOptions.NoInlining)] public static void CountBase() => BaseCalls++;
        [MethodImpl(MethodImplOptions.NoInlining)] public static void CountCalculation() => Calculations++;
        public static float GenericRange<T>(float min, float max) => min;
        [MethodImpl(MethodImplOptions.NoInlining)] public static float Unbound() => 17f;
        public static int Observed;
        public static Properties? Escaped;
        [MethodImpl(MethodImplOptions.NoInlining)] public static void Observe(Properties __instance) => Observed++;
        public static IEnumerable<CodeInstruction> NonzeroRange(IEnumerable<CodeInstruction> instructions) =>
            new[] { new CodeInstruction(OpCodes.Ldc_R4, .25f), new CodeInstruction(OpCodes.Ret) };
        public static IEnumerable<CodeInstruction> TrueChance(IEnumerable<CodeInstruction> instructions) =>
            new[] { new CodeInstruction(OpCodes.Ldc_I4_1), new CodeInstruction(OpCodes.Ret) };
        public static IEnumerable<CodeInstruction> EscapeProperties(IEnumerable<CodeInstruction> instructions) =>
            new[] { new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Stsfld, Field(typeof(Effects), nameof(Escaped))), new CodeInstruction(OpCodes.Ret) };
        public static IEnumerable<CodeInstruction> EnterRandomTail(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.Select(i => new CodeInstruction(i)).ToArray();
            code.First(i => i.opcode == OpCodes.Bgt_Un || i.opcode == OpCodes.Bgt_Un_S).opcode = OpCodes.Ble;
            return code;
        }
        [MethodImpl(MethodImplOptions.NoInlining)] public static float MutateRange(ref Interval range) { range.Min = 1f; return range.Min; }
        public static IEnumerable<CodeInstruction> ChangeCalculator(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var code = instructions.Select(i => new CodeInstruction(i)).ToList();
            switch (Change)
            {
                case CalculatorMutation.OtherRate:
                    code.First(i => i.opcode == OpCodes.Ldfld && Equals(i.operand, Field(typeof(Properties), nameof(Properties.Rate)))).operand = Field(typeof(Properties), nameof(Properties.OtherRate)); break;
                case CalculatorMutation.RangeAddress:
                    code.First(i => i.opcode == OpCodes.Call && Equals(i.operand, Method(typeof(Interval), "get_Random"))).operand = Method(typeof(Effects), nameof(MutateRange)); break;
                case CalculatorMutation.RemoveStore:
                    RemoveAt(code, code.FindIndex(i => i.IsStloc())); RemoveAt(code, code.FindLastIndex(i => i.IsLdloc())); break;
                case CalculatorMutation.OtherLocal:
                    var other = generator.DeclareLocal(typeof(float)); var load = code.Last(i => i.IsLdloc());
                    load.opcode = OpCodes.Ldloc; load.operand = other;
                    code.InsertRange(0, new[] { new CodeInstruction(OpCodes.Ldc_R4, -.12f), new CodeInstruction(OpCodes.Stloc, other) }); break;
                case CalculatorMutation.FlipBranch:
                    code.First(i => i.opcode == OpCodes.Brfalse || i.opcode == OpCodes.Brfalse_S).opcode = OpCodes.Brtrue; break;
                case CalculatorMutation.LateRateRead:
                    var read = code.Take(2).ToArray(); code.RemoveRange(0, 2);
                    code.InsertRange(code.FindIndex(i => i.opcode == OpCodes.Add), read); break;
            }
            return code;
        }
        private static void RemoveAt(List<CodeInstruction> code, int index)
        { code[index + 1].labels.AddRange(code[index].labels); code.RemoveAt(index); }
        public static IEnumerable<CodeInstruction> ChangeInitialization(IEnumerable<CodeInstruction> instructions)
        {
            var code = instructions.Select(i => new CodeInstruction(i)).ToList();
            switch (InitializationChange)
            {
                case InitializationMutation.PropertiesProjection:
                    return new[] { new CodeInstruction(OpCodes.Ldsfld, Field(typeof(Effects), nameof(OtherProperties))), new CodeInstruction(OpCodes.Ret) };
                case InitializationMutation.BaseMutation:
                    return new[] { new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldsfld, Field(typeof(Effects), nameof(OtherProperties))),
                        new CodeInstruction(OpCodes.Stfld, Field(typeof(Comp), nameof(Comp.Properties))), new CodeInstruction(OpCodes.Ret) };
                case InitializationMutation.SkipBase: code.RemoveRange(0, 3); break;
                case InitializationMutation.SkipWrite:
                    int store = code.FindIndex(i => i.opcode == OpCodes.Stfld);
                    code[store].opcode = OpCodes.Pop; code[store].operand = null; code.Insert(store + 1, new(OpCodes.Pop)); break;
                case InitializationMutation.DuplicateCalculation:
                    code.InsertRange(3, new[] { new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Call, Method(typeof(Decay), "get_ActualProperties")),
                        new CodeInstruction(OpCodes.Callvirt, Method(typeof(Properties), nameof(Properties.Calculate))), new CodeInstruction(OpCodes.Pop) }); break;
                case InitializationMutation.OtherComp: code[3].opcode = OpCodes.Ldsfld; code[3].operand = Field(typeof(Effects), nameof(OtherComp)); break;
                case InitializationMutation.OtherField: code.First(i => i.opcode == OpCodes.Stfld).operand = Field(typeof(Decay), nameof(Decay.OtherStoredRate)); break;
            }
            return code;
        }
        public static IEnumerable<CodeInstruction> Nops(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                var original = new CodeInstruction(instruction); var nop = new CodeInstruction(OpCodes.Nop);
                nop.labels.AddRange(original.labels); original.labels.Clear(); yield return nop; yield return original;
            }
        }
        public static IEnumerable<CodeInstruction> WriteRange(IEnumerable<CodeInstruction> instructions) =>
            new[] { new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldc_R4, .5f), new CodeInstruction(OpCodes.Stfld, Field(typeof(Interval), nameof(Interval.Min))),
                new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Ldfld, Field(typeof(Interval), nameof(Interval.Min))), new CodeInstruction(OpCodes.Ret) };
        public static IEnumerable<CodeInstruction> ReplaceCall(IEnumerable<CodeInstruction> instructions)
        {
            var bindings = Bindings();
            (MethodInfo original, MethodInfo replacement) = ReplacementEdge switch
            {
                CallEdge.Range => (bindings.Range, Method(typeof(RandomHelpers), nameof(RandomHelpers.ReplacementRange))),
                CallEdge.Chance => (bindings.Chance, Method(typeof(RandomHelpers), nameof(RandomHelpers.ReplacementChance))),
                CallEdge.BaseInitialize => (bindings.BaseInitialize, Method(typeof(Comp), nameof(Comp.ReplacementInitialize))),
                CallEdge.PropertiesGetter => (bindings.PropertiesGetter, Method(typeof(Decay), "get_ReplacementProperties")),
                _ => (bindings.Calculate, Method(typeof(Properties), nameof(Properties.ReplacementCalculate)))
            };
            var code = instructions.Select(i => new CodeInstruction(i)).ToArray();
            code.Single(i => (i.opcode == OpCodes.Call || i.opcode == OpCodes.Callvirt) && Equals(i.operand, original)).operand = replacement;
            return code;
        }
    }

    public class Comp
    {
        public BaseProperties Properties = null!;
        [MethodImpl(MethodImplOptions.NoInlining)] public virtual void Initialize(int? info) { }
        [MethodImpl(MethodImplOptions.NoInlining)] public virtual void ReplacementInitialize(int? info)
        { Effects.ReplacementReceiver = this; Effects.ReplacementInput = info; }
    }
    public class BaseProperties { }
    public sealed class Properties : BaseProperties
    {
        public float Rate, Reversal, OtherRate;
        public Interval Range;
        [MethodImpl(MethodImplOptions.NoInlining)] public float ReplacementCalculate() => OtherRate;
        [MethodImpl(MethodImplOptions.NoInlining)] public float Calculate()
        {
            float result = Rate + Range.Random;
            if (RandomHelpers.Chance(Reversal)) result *= -1f;
            return result;
        }
    }
    public struct Interval
    {
        public float Min, Max;
        public float Random { [MethodImpl(MethodImplOptions.NoInlining)] get => RandomHelpers.Range(Min, Max); }
    }
    public sealed class Decay : Comp
    {
        public float StoredRate, OtherStoredRate;
        private Properties ActualProperties { [MethodImpl(MethodImplOptions.NoInlining)] get => (Properties)Properties; }
        private Properties ReplacementProperties { [MethodImpl(MethodImplOptions.NoInlining)] get => Effects.OtherProperties!; }
        [MethodImpl(MethodImplOptions.NoInlining)] public override void Initialize(int? info)
        { base.Initialize(info); StoredRate = ActualProperties.Calculate(); }
    }
    public static class RandomHelpers
    {
        [MethodImpl(MethodImplOptions.NoInlining)] public static float ReplacementRange(float min, float max) => .25f;
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool ReplacementChance(float chance) => true;
        [MethodImpl(MethodImplOptions.NoInlining)] public static float Range(float min, float max)
        { if (max <= min) return min; return Value() * (max - min) + min; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool Chance(float chance)
        { if (chance <= 0f) return false; if (chance >= 1f) return true; return Value() < chance; }
        [MethodImpl(MethodImplOptions.NoInlining)] private static float Value() => throw new InvalidOperationException("The random tail is unreachable for admitted zero inputs.");
    }
}


