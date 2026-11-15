using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Patching;

[TestFixture, NonParallelizable]
public sealed class HarmonyOriginalBodyTests
{
    private static int helperCalls, attributes, factories, compositions, callbacks;
    private static MethodInfo Method(string name) => typeof(HarmonyOriginalBodyTests).GetMethod(name)!;
    private static HarmonyMethod Patch(string name) => new(Method(name));

    [Test]
    public void ReadsOriginalGenericLocalsCallsScalarsAndBranchTargetsWithoutExecutingThem()
    {
        var definition = Method(nameof(Generic));
        helperCalls = 0;
        Assert.That(HarmonyOriginalBody.TryRead(definition, out var body, out var failure), Is.True, failure.ToString());
        Assert.That(body!.Original, Is.EqualTo(definition));
        var parameter = definition.GetGenericArguments().Single();
        Assert.That(body.LocalTypes, Does.Contain(parameter));
        var instructions = body.CopyInstructions();
        var calls = instructions.Where(code => code.opcode == OpCodes.Call).Select(code => (MethodInfo)code.operand).ToArray();
        Assert.That(calls.Select(call => call.GetGenericMethodDefinition()), Is.EquivalentTo(new[] { Method(nameof(Keep)), Method(nameof(Consume)) }));
        Assert.That(calls.All(call => call.GetGenericArguments().SequenceEqual(new[] { parameter })), Is.True);
        Assert.That(instructions.Any(code => code.opcode == OpCodes.Ldc_R4 && Equals(code.operand, 1f)), Is.True);
        var branches = instructions.Where(code => code.opcode.OperandType is OperandType.InlineBrTarget or OperandType.ShortInlineBrTarget).ToArray();
        Assert.That(branches, Is.Not.Empty);
        foreach (var branch in branches)
        {
            Assert.That(branch.operand, Is.TypeOf<Label>());
            Assert.That(instructions.Count(code => code.labels.Contains((Label)branch.operand)), Is.EqualTo(1));
        }
        Assert.That(helperCalls, Is.Zero);
        Assert.That(Generic(true, 9, 2f), Is.EqualTo(9), "The fixture contains executable generic operations, not a synthetic instruction list.");
        Assert.That(helperCalls, Is.EqualTo(2));
    }

    [Test]
    public void PublishedFactoriesAttributesAndCallbacksAreNotReplayedByOriginalInspection()
    {
        var target = Method(nameof(Original));
        var owner = new HarmonyLib.Harmony("tests.original-body.passive");
        helperCalls = attributes = factories = compositions = callbacks = 0;
        try
        {
            owner.CreateProcessor(target)
                .AddTranspiler(Patch(nameof(Factory)))
                .AddInnerPostfix(new HarmonyMethod(Method(nameof(Observe))) { innerMethod = new InnerMethod(Method(nameof(Helper))) })
                .Patch();
            Assert.That(factories, Is.GreaterThan(0), "Actual provider acquisition activates the factory canary.");
            Assert.That(compositions, Is.GreaterThan(0));
            Assert.That(attributes, Is.GreaterThan(0));
            Assert.That(Original(), Is.EqualTo(37));
            Assert.That(callbacks, Is.EqualTo(1));
            HarmonyPublication.Identity? publication = null;
            Assert.That(HarmonyPublication.InspectIdentity(target, identity =>
            {
                Assert.That(identity.HasPublication, Is.True);
                publication = identity;
                return true;
            }), Is.True);
            factories = compositions = attributes = callbacks = helperCalls = 0;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                Assert.That(HarmonyOriginalBody.TryRead(target, out var body, out var failure), Is.True, failure.ToString());
                var instructions = body!.CopyInstructions();
                Assert.That(instructions.Any(code => code.opcode == OpCodes.Call && Equals(code.operand, Method(nameof(Helper)))), Is.True);
                Assert.That(instructions.Any(code => code.opcode == OpCodes.Ldc_I4_7), Is.True, "The original constant survives despite its published replacement.");
                Assert.That(instructions.Any(code => code.opcode == OpCodes.Ldc_I4 && Equals(code.operand, 37)), Is.False);
            }
            Assert.That((factories, compositions, attributes, callbacks, helperCalls), Is.EqualTo((0, 0, 0, 0, 0)));
            Assert.That(publication!.IsCurrent(), Is.True, "Original inspection must not acquire or republish any patch.");
            Assert.That(Original(), Is.EqualTo(37));
        }
        finally { owner.Unpatch(target, HarmonyPatchType.All, owner.Id); }
        Assert.That(Original(), Is.EqualTo(7));
    }

    [Test]
    public void ClosedGenericBodyRetainsItsActualTypeArgument()
    {
        var target = Method(nameof(Generic)).MakeGenericMethod(typeof(UnknownValue));
        var body = Read(target);
        Assert.That(body.LocalTypes, Does.Contain(typeof(UnknownValue)));
        var calls = body.CopyInstructions().Where(code => code.opcode == OpCodes.Call)
            .Select(code => (MethodInfo)code.operand).ToArray();
        Assert.That(calls.All(call => call.GetGenericArguments().SequenceEqual(new[] { typeof(UnknownValue) })), Is.True);
        Assert.That(Generic(true, new UnknownValue(13), 2f).Value, Is.EqualTo(13));
    }

    [Test]
    public void SwitchAndBranchTargetsRemainDetachedWhenACallerChangesItsCopy()
    {
        var target = Emit(il =>
        {
            var result = il.DefineLabel();
            il.Emit(OpCodes.Ldc_I4_M1);
            il.Emit(OpCodes.Switch, new[] { result, result });
            il.Emit(OpCodes.Br, result);
            il.MarkLabel(result);
            il.Emit(OpCodes.Ret);
        });
        var body = Read(target);
        var first = body.CopyInstructions();
        var originalTargets = (Label[])first.Single(code => code.opcode == OpCodes.Switch).operand;
        Assert.That(originalTargets[0], Is.EqualTo(originalTargets[1]));
        var branch = first.Single(code => code.opcode == OpCodes.Br);
        Assert.That(branch.operand, Is.EqualTo(originalTargets[0]));
        Assert.That(first.Single(code => code.opcode == OpCodes.Ret).labels, Does.Contain(originalTargets[0]));

        var otherLabels = new DynamicMethod("OtherLabels", typeof(void), Type.EmptyTypes).GetILGenerator();
        otherLabels.DefineLabel();
        originalTargets[0] = otherLabels.DefineLabel();
        first.Single(code => code.opcode == OpCodes.Ret).labels.Clear();
        branch.opcode = OpCodes.Throw;
        var second = body.CopyInstructions();
        var targets = (Label[])second.Single(code => code.opcode == OpCodes.Switch).operand;
        Assert.That(targets[0], Is.EqualTo(targets[1]));
        Assert.That(second.Single(code => code.opcode == OpCodes.Br).operand, Is.EqualTo(targets[0]));
        Assert.That(second.Single(code => code.opcode == OpCodes.Ret).labels, Does.Contain(targets[0]));
        Assert.DoesNotThrow(() => target.Invoke(null, null));
    }

    [TestCase(4096, true)]
    [TestCase(4097, false)]
    public void OperationBudgetIsCheckedOnRealManagedBodies(int count, bool accepted)
    {
        var target = Emit(il =>
        {
            for (int index = 1; index < count; index++) il.Emit(OpCodes.Nop);
            il.Emit(OpCodes.Ret);
        });
        AssertBudget(target, accepted);
        if (accepted) Assert.That(Read(target).CopyInstructions(), Has.Count.EqualTo(count));
    }

    [TestCase(256, true)]
    [TestCase(257, false)]
    public void LocalBudgetIsCheckedBeforeProviderProjection(int count, bool accepted)
    {
        var target = Emit(il =>
        {
            LocalBuilder? last = null;
            for (int index = 0; index < count; index++) last = il.DeclareLocal(typeof(int));
            il.Emit(OpCodes.Ldc_I4_3);
            il.Emit(OpCodes.Stloc, last!);
            il.Emit(OpCodes.Ldloc, last!);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);
        });
        AssertBudget(target, accepted);
        if (accepted)
        {
            var body = Read(target);
            Assert.That(body.LocalTypes, Has.Count.EqualTo(count));
            Assert.That(body.CopyInstructions().Where(code => code.IsLdloc() || code.IsStloc())
                .All(code => code.LocalIndex() == count - 1), Is.True);
        }
    }

    [TestCase(256, true)]
    [TestCase(257, false)]
    public void SwitchBudgetIsCheckedBeforeProviderAllocation(int count, bool accepted)
    {
        var target = Emit(il =>
        {
            var done = il.DefineLabel();
            il.Emit(OpCodes.Ldc_I4_M1);
            il.Emit(OpCodes.Switch, Enumerable.Repeat(done, count).ToArray());
            il.MarkLabel(done);
            il.Emit(OpCodes.Ret);
        });
        AssertBudget(target, accepted);
    }

    [TestCase(65536, true)]
    [TestCase(65537, false)]
    public void ByteBudgetAllowsTheBoundaryIndependentlyOfOperationAndSwitchBudgets(int bytes, bool accepted)
    {
        var target = Emit(il =>
        {
            var done = il.DefineLabel();
            // 63 bounded switches occupy 64890 bytes but only126 operations. Each receives
            // -1 and falls through, so the fixture is executable at the exact byte boundary.
            for (int index = 0; index < 63; index++)
            {
                il.Emit(OpCodes.Ldc_I4_M1);
                il.Emit(OpCodes.Switch, Enumerable.Repeat(done, 256).ToArray());
            }
            while (il.ILOffset < bytes - 1) il.Emit(OpCodes.Nop);
            il.MarkLabel(done);
            il.Emit(OpCodes.Ret);
        });
        Assert.That(target.GetMethodBody()!.GetILAsByteArray(), Has.Length.EqualTo(bytes));
        AssertBudget(target, accepted);
    }

    [Test]
    public void OriginalExceptionRegionsRemainUnsupportedWithoutExecutingTheBody()
    {
        helperCalls = 0;
        Assert.That(HarmonyOriginalBody.TryRead(Method(nameof(WithFinally)), out var body, out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(HarmonyOriginalBody.Failure.UnsupportedBody));
        Assert.That(body, Is.Null);
        Assert.That(helperCalls, Is.Zero);
        Assert.That(WithFinally(), Is.EqualTo(11));
        Assert.That(helperCalls, Is.EqualTo(2));
    }

    [Test]
    public void OriginalInlineSignaturesRemainUnsupportedWithoutExecutingTheCallee()
    {
        var target = Emit(il =>
        {
            il.Emit(OpCodes.Ldftn, Method(nameof(Helper)));
            il.EmitCalli(OpCodes.Calli, CallingConventions.Standard, typeof(int), Type.EmptyTypes, null);
            il.Emit(OpCodes.Pop);
            il.Emit(OpCodes.Ret);
        });
        helperCalls = 0;
        Assert.That(HarmonyOriginalBody.TryRead(target, out var body, out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(HarmonyOriginalBody.Failure.UnsupportedBody));
        Assert.That(body, Is.Null);
        Assert.That(helperCalls, Is.Zero);
        Assert.DoesNotThrow(() => target.Invoke(null, null));
        Assert.That(helperCalls, Is.EqualTo(1));
    }

    [Test]
    public void MissingBodiesAndGenericDeclaringTypesDoNotBecomeOriginalProofs()
    {
        Assert.That(HarmonyOriginalBody.TryRead(typeof(AbstractFixture).GetMethod(nameof(AbstractFixture.Native))!, out _, out _), Is.False);
        Assert.That(HarmonyOriginalBody.TryRead(typeof(GenericFixture<int>).GetMethod(nameof(GenericFixture<int>.Native))!, out _, out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(HarmonyOriginalBody.Failure.UnsupportedMethod));
        Assert.That(HarmonyOriginalBody.TryRead(new DynamicMethod("OpaqueDynamic", typeof(void), Type.EmptyTypes), out _, out failure), Is.False);
        Assert.That(failure, Is.EqualTo(HarmonyOriginalBody.Failure.UnsupportedMethod));
    }

    [Test]
    public void UnknownMethodWrapperIsRejectedBeforeItsMetadataCanRun()
    {
        var target = new OpaqueMethod();
        Assert.That(HarmonyOriginalBody.TryRead(target, out _, out var failure), Is.False);
        Assert.That(failure, Is.EqualTo(HarmonyOriginalBody.Failure.UnsupportedMethod));
        Assert.That(target.Reads, Is.Zero);
    }

    private static HarmonyOriginalBody.Snapshot Read(MethodInfo target)
    {
        Assert.That(HarmonyOriginalBody.TryRead(target, out var body, out var failure), Is.True, failure.ToString());
        return body!;
    }
    private static void AssertBudget(MethodInfo target, bool accepted)
    {
        Assert.That(HarmonyOriginalBody.TryRead(target, out var body, out var failure), Is.EqualTo(accepted), failure.ToString());
        Assert.That(failure, Is.EqualTo(accepted ? HarmonyOriginalBody.Failure.None : HarmonyOriginalBody.Failure.Limit));
        Assert.That(body != null, Is.EqualTo(accepted));
        Assert.DoesNotThrow(() => target.Invoke(null, null), "The budget fixture itself is a real executable managed body.");
    }
    private static MethodInfo Emit(Action<ILGenerator> emit)
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("OriginalBodyFixture" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.RunAndCollect);
        var type = assembly.DefineDynamicModule("fixture").DefineType("NativeFixture", TypeAttributes.Public);
        var method = type.DefineMethod("Native", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
        emit(method.GetILGenerator());
        return type.CreateType()!.GetMethod("Native")!;
    }
    public readonly record struct UnknownValue(int Value);
    public abstract class AbstractFixture { public abstract void Native(); }
    public sealed class GenericFixture<T> { public void Native() { } }
    [MethodImpl(MethodImplOptions.NoInlining)] public static int WithFinally() { try { return Helper(); } finally { helperCalls++; } }
    private sealed class OpaqueMethod : MethodInfo
    {
        internal int Reads;
        private Exception Access() { Reads++; return new InvalidOperationException("Untrusted metadata invoked."); }
        public override MethodAttributes Attributes => throw Access();
        public override RuntimeMethodHandle MethodHandle => throw Access();
        public override Type? DeclaringType => throw Access();
        public override string Name => throw Access();
        public override Type? ReflectedType => throw Access();
        public override ICustomAttributeProvider ReturnTypeCustomAttributes => throw Access();
        public override MethodInfo GetBaseDefinition() => throw Access();
        public override MethodImplAttributes GetMethodImplementationFlags() => throw Access();
        public override ParameterInfo[] GetParameters() => throw Access();
        public override object[] GetCustomAttributes(bool inherit) => throw Access();
        public override object[] GetCustomAttributes(Type attributeType, bool inherit) => throw Access();
        public override bool IsDefined(Type attributeType, bool inherit) => throw Access();
        public override object? Invoke(object? obj, BindingFlags invokeAttr, Binder? binder, object?[]? parameters, System.Globalization.CultureInfo? culture) => throw Access();
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static T Generic<T>(bool enabled, T value, float scalar) where T : struct
    {
        T copy = value;
        Keep(ref copy);
        if (scalar > 1f && enabled) return Consume(copy);
        return default;
    }
    [MethodImpl(MethodImplOptions.NoInlining)] public static void Keep<T>(ref T value) where T : struct { helperCalls++; }
    [MethodImpl(MethodImplOptions.NoInlining)] public static T Consume<T>(T value) where T : struct { helperCalls++; return value; }
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Original() { Helper(); return 7; }
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Helper() { helperCalls++; return 11; }
    public static MethodInfo Factory(MethodBase original) { factories++; return Method(nameof(ReplaceConstant)); }
    public static IEnumerable<CodeInstruction> ReplaceConstant(IEnumerable<CodeInstruction> instructions)
    {
        compositions++;
        return instructions.Select(code => code.opcode == OpCodes.Ldc_I4_7 ? new CodeInstruction(OpCodes.Ldc_I4, 37) : code);
    }
    public static void Observe([Counted] int __result) { callbacks++; }
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class CountedAttribute : Attribute
    { public CountedAttribute() { attributes++; } }
}
