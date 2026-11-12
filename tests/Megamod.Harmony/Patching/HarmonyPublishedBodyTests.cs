using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Patching;

[TestFixture, NonParallelizable]
public sealed class HarmonyPublishedBodyTests
{
    private static readonly MethodInfo Target = Method(nameof(Native));
    private static int compositions, helperCalls, factories, attributes, innerCalls, innerValue;
    private static MethodInfo Method(string name) => typeof(HarmonyPublishedBodyTests).GetMethod(name)!;
    private static HarmonyMethod PatchMethod(string name) => new(Method(name));

    [Test]
    public void ReadsAlreadyPublishedReplacementWithoutReplayingComposition()
    {
        var owner = new HarmonyLib.Harmony("tests.published-body.constant");
        compositions = 0;
        try
        {
            owner.Patch(Target, transpiler: PatchMethod(nameof(Constant37)));
            Assert.That(Native(), Is.EqualTo(37));
            int composed = compositions;
            Assert.That(composed, Is.GreaterThan(0));
            for (int attempt = 0; attempt < 3; attempt++)
            {
                Assert.That(HarmonyPublishedBody.TryCapture(Target, out var body, out var failure), Is.True, failure.ToString());
                Assert.That(body!.PublishedTarget, Is.Not.EqualTo(Target));
                Assert.That(ContainsInteger(body, 37), Is.True);
                Assert.That(ContainsInteger(body, 7), Is.False);
            }
            Assert.That(compositions, Is.EqualTo(composed));
            Assert.That(Native(), Is.EqualTo(37));
        }
        finally { owner.Unpatch(Target, HarmonyPatchType.All, owner.Id); }
    }
    public static IEnumerable<CodeInstruction> Constant37(IEnumerable<CodeInstruction> source)
    {
        compositions++;
        return new[] { new CodeInstruction(OpCodes.Ldc_I4, 37), new CodeInstruction(OpCodes.Ret) };
    }
    [Test]
    public void RetainsExistingMethodTokenWithoutExecutingTheHelper()
    {
        var owner = new HarmonyLib.Harmony("tests.published-body.method-token");
        helperCalls = 0;
        try
        {
            owner.Patch(Target, transpiler: PatchMethod(nameof(CallHelper)));
            Assert.That(HarmonyPublishedBody.TryCapture(Target, out var body, out var failure), Is.True, failure.ToString());
            var call = body!.Operations.Single(operation => operation.Code == OpCodes.Call);
            var binding = body.Tokens[(int)call.Operand!];
            Assert.That(binding.Kind, Is.EqualTo(HarmonyPublishedBody.TokenKind.MethodHandle));
            Assert.That(binding.Value, Is.EqualTo(Method(nameof(Helper)).MethodHandle));
            Assert.That(binding.TypeContext, Is.EqualTo(typeof(HarmonyPublishedBodyTests).TypeHandle));
            Assert.That(helperCalls, Is.Zero);
            Assert.That(Native(), Is.EqualTo(41));
            Assert.That(helperCalls, Is.EqualTo(1));
        }
        finally { owner.Unpatch(Target, HarmonyPatchType.All, owner.Id); }
    }
    public static IEnumerable<CodeInstruction> CallHelper(IEnumerable<CodeInstruction> source) =>
        new[] { new CodeInstruction(OpCodes.Call, Method(nameof(Helper))), new CodeInstruction(OpCodes.Ret) };
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Helper() { helperCalls++; return 41; }
    [Test]
    public void RepatchInvalidatesReceiptWhileOldSnapshotRemainsImmutable()
    {
        var owner = new HarmonyLib.Harmony("tests.published-body.republication");
        MethodInfo unrelated = Method(nameof(OtherNative));
        try
        {
            owner.Patch(Target, transpiler: PatchMethod(nameof(Constant37)));
            var first = Capture(Target);
            var retained = first.Code.ToArray();
            Assert.Throws<NotSupportedException>(() => ((IList<byte>)first.Code)[0] = 0);
            owner.Patch(unrelated, transpiler: PatchMethod(nameof(Nops)));
            Assert.That(first.IsPublicationCurrent(), Is.True);
            owner.Unpatch(Target, HarmonyPatchType.Transpiler, owner.Id);
            Assert.That(first.IsPublicationCurrent(), Is.False);
            Assert.That(Native(), Is.EqualTo(7));
            var restored = Capture(Target);
            Assert.That(ContainsInteger(restored, 7), Is.True);
            owner.Patch(Target, transpiler: PatchMethod(nameof(Constant37)));
            Assert.That(first.IsPublicationCurrent(), Is.False);
            Assert.That(restored.IsPublicationCurrent(), Is.False);
            Assert.That(first.Code, Is.EqualTo(retained));
            Assert.That(Capture(Target).IsPublicationCurrent(), Is.True);
        }
        finally
        {
            try { owner.Unpatch(Target, HarmonyPatchType.All, owner.Id); }
            finally { owner.Unpatch(unrelated, HarmonyPatchType.All, owner.Id); }
        }
    }
    private static HarmonyPublishedBody.Snapshot Capture(MethodInfo target)
    {
        Assert.That(HarmonyPublishedBody.TryCapture(target, out var body, out var failure), Is.True, failure.ToString());
        Assert.That(failure, Is.EqualTo(HarmonyPublishedBody.Failure.None));
        return body!;
    }
    private static bool ContainsInteger(HarmonyPublishedBody.Snapshot body, int value) =>
        body.Operations.Any(operation => Integer(operation) == value);
    private static int? Integer(HarmonyPublishedBody.Operation operation)
    {
        if (operation.Code == OpCodes.Ldc_I4 || operation.Code == OpCodes.Ldc_I4_S) return Convert.ToInt32(operation.Operand);
        if (operation.Code == OpCodes.Ldc_I4_M1) return -1;
        if (operation.Code.Value >= OpCodes.Ldc_I4_0.Value && operation.Code.Value <= OpCodes.Ldc_I4_8.Value)
            return operation.Code.Value - OpCodes.Ldc_I4_0.Value;
        return null;
    }
    public static IEnumerable<CodeInstruction> Nops(IEnumerable<CodeInstruction> source)
    { compositions++; return new[] { new CodeInstruction(OpCodes.Nop) }.Concat(source); }
    [MethodImpl(MethodImplOptions.NoInlining)] public static int OtherNative() => 11;
    [Test]
    public void NoOpFactoryIsNotReplayedByInspection()
    {
        var owner = new HarmonyLib.Harmony("tests.published-body.no-op-factory");
        compositions = factories = 0;
        try
        {
            owner.Patch(Target, transpiler: PatchMethod(nameof(NoOpFactory)));
            Assert.That(Native(), Is.EqualTo(7));
            Assert.That(factories, Is.GreaterThan(0));
            Assert.That(compositions, Is.GreaterThan(0));
            var counts = (compositions, factories);
            for (int attempt = 0; attempt < 3; attempt++)
            {
                var body = Capture(Target);
                Assert.That(ContainsInteger(body, 7), Is.True);
                Assert.That(body.IsPublicationCurrent(), Is.True);
            }
            Assert.That((compositions, factories), Is.EqualTo(counts));
        }
        finally { owner.Unpatch(Target, HarmonyPatchType.All, owner.Id); }
    }

    [Test]
    public void InnerCallbackAttributeCanaryDetectsProviderDeserializationButNotReaderInspection()
    {
        MethodInfo target = Method(nameof(CallsHelper));
        var owner = new HarmonyLib.Harmony("tests.published-body.inner-attribute");
        attributes = helperCalls = innerCalls = innerValue = 0;
        try
        {
            owner.CreateProcessor(target)
                .AddInnerPostfix(new HarmonyMethod(Method(nameof(ObserveInner)))
                { innerMethod = new InnerMethod(Method(nameof(Helper))) }).Patch();
            Assert.That(attributes, Is.GreaterThan(0),
                "Legitimate provider processing must activate the counted parameter attribute.");
            int registered = attributes;
            // Positive control only: inspected Deserialize/GetRequiredInfixVersion
            // constructs attributes on inner callback parameters. Keep default
            // checked binding and this simple version-1 infix; no version-5 bypass.
            // Production capture/currentness must never call this API.
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(target), Is.Not.Null);
            Assert.That(attributes, Is.GreaterThan(registered),
                "The canary must detect the exact unsafe metadata-read alternative.");
            int constructed = attributes;
            for (int attempt = 0; attempt < 3; attempt++)
            {
                var body = Capture(target);
                Assert.That(body.IsPublicationCurrent(), Is.True);
                Assert.That(attributes, Is.EqualTo(constructed));
                Assert.That(helperCalls, Is.Zero);
                Assert.That(innerCalls, Is.Zero);
            }
            Assert.That(CallsHelper(), Is.EqualTo(41));
            Assert.That(helperCalls, Is.EqualTo(1));
            Assert.That(innerCalls, Is.EqualTo(1));
            Assert.That(innerValue, Is.EqualTo(41));
            Assert.That(attributes, Is.EqualTo(constructed));
        }
        finally { owner.Unpatch(target, HarmonyPatchType.All, owner.Id); }
    }

    [Test]
    public void ReadsBakedResolverAfterOwnedPublishedBuilderIsChanged()
    {
        var owner = new HarmonyLib.Harmony("tests.published-body.baked-resolver");
        try
        {
            owner.Patch(Target, transpiler: PatchMethod(nameof(Constant37)));
            Assert.That(Native(), Is.EqualTo(37));
            var before = Capture(Target);
            Assert.That(before.PublishedTarget, Is.TypeOf<DynamicMethod>(),
                "The actual provider must have selected this representation for this tracer.");
            // This is the provider-created replacement of this test's owned target.
            // Only the test changes its builder. SetCode replaces builder storage;
            // the already-baked resolver/descriptor remains the executable method.
            var published = (DynamicMethod)before.PublishedTarget;
            published.GetDynamicILInfo().SetCode(new byte[] { 0x1f, 77, 0x2a }, 1);
            Assert.That(Native(), Is.EqualTo(37));
            var after = Capture(Target);
            Assert.That(after.Code, Is.EqualTo(before.Code));
            Assert.That(ContainsInteger(after, 37), Is.True);
            Assert.That(ContainsInteger(after, 77), Is.False);
            Assert.That(before.IsPublicationCurrent(), Is.True);
        }
        finally { owner.Unpatch(Target, HarmonyPatchType.All, owner.Id); }
    }
    public static MethodInfo NoOpFactory(MethodBase original) { factories++; return Method(nameof(Nops)); }
    [AttributeUsage(AttributeTargets.Parameter)] public sealed class CountAttribute : Attribute
    { public CountAttribute() => attributes++; }
    public static void ObserveInner([Count] int __result) { innerCalls++; innerValue = __result; }
    [MethodImpl(MethodImplOptions.NoInlining)] public static int CallsHelper() => Helper();
    [Test]
    public void ReentryAndContentionAreBoundedAndDoNotReadAnInProgressPublication()
    {
        var owner = new HarmonyLib.Harmony("tests.published-body.busy");
        try
        {
            owner.Patch(Target, transpiler: PatchMethod(nameof(Constant37)));
            var captured = Capture(Target);
            Assert.That(HarmonyPublication.InspectIdentity(Target, _ =>
            {
                Assert.That(HarmonyPublishedBody.TryCapture(Target, out var body, out var failure), Is.False);
                Assert.That(body, Is.Null);
                Assert.That(failure, Is.EqualTo(HarmonyPublishedBody.Failure.Busy));
                Assert.That(captured.IsPublicationCurrent(), Is.False);
                var attempt = Task.Run(() =>
                {
                    bool result = HarmonyPublishedBody.TryCapture(Target, out var other, out var reason);
                    return (result, other, reason);
                });
                Assert.That(attempt.Wait(TimeSpan.FromSeconds(3)), Is.True);
                Assert.That(attempt.Result.result, Is.False);
                Assert.That(attempt.Result.other, Is.Null);
                Assert.That(attempt.Result.reason, Is.EqualTo(HarmonyPublishedBody.Failure.Busy));
                return true;
            }), Is.True);
            Assert.That(captured.IsPublicationCurrent(), Is.True);
        }
        finally { owner.Unpatch(Target, HarmonyPatchType.All, owner.Id); }
    }

    [Test]
    public void ReportsActualExceptionRegionsThenRecoversOnAnEhFreePublication()
    {
        MethodInfo target = Method(nameof(WithException));
        var owner = new HarmonyLib.Harmony("tests.published-body.eh-boundary");
        try
        {
            owner.Patch(target, transpiler: PatchMethod(nameof(Nops)));
            Assert.That(WithException(-1), Is.EqualTo(13));
            Assert.That(HarmonyPublishedBody.TryCapture(target, out var body, out var failure), Is.False);
            Assert.That(body, Is.Null);
            Assert.That(failure, Is.EqualTo(HarmonyPublishedBody.Failure.ExceptionRegions));
            owner.Unpatch(target, HarmonyPatchType.Transpiler, owner.Id);
            owner.Patch(target, transpiler: PatchMethod(nameof(Constant37)));
            Assert.That(WithException(-1), Is.EqualTo(37));
            Assert.That(ContainsInteger(Capture(target), 37), Is.True);
        }
        finally { owner.Unpatch(target, HarmonyPatchType.All, owner.Id); }
    }

    [Test]
    public void NeverSubstitutesOriginalIlForAnUnpublishedRoot()
    {
        Assert.That(HarmonyPublishedBody.TryCapture(Method(nameof(NeverPatched)), out var body, out var failure), Is.False);
        Assert.That(body, Is.Null);
        Assert.That(failure, Is.EqualTo(HarmonyPublishedBody.Failure.NotPublished));
        Assert.That(NeverPatched(), Is.EqualTo(19));
    }
    [MethodImpl(MethodImplOptions.NoInlining)] public static int NeverPatched() => 19;
    [MethodImpl(MethodImplOptions.NoInlining)] public static int WithException(int value)
    {
        try { if (value < 0) throw new InvalidOperationException(); return value; }
        catch (InvalidOperationException) { return 13; }
    }
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Native() => 7;
    private static int tokenInitializations, largeCount;

    [Test]
    public void PreservesSwitchBranchesLocalsScalarsAndInertFieldTypeStringBindings()
    {
        var owner = new HarmonyLib.Harmony("tests.published-body.operands");
        MethodInfo target = Method(nameof(WithArgument));
        try
        {
            owner.Patch(target, transpiler: PatchMethod(nameof(OperandBody)));
            var body = Capture(target);
            Assert.That(tokenInitializations, Is.Zero, "Reading token identity cannot run the field's type initializer.");
            var branch = body.Operations.Single(operation => operation.Code == OpCodes.Switch);
            var destinations = (IReadOnlyList<int>)branch.Operand!;
            Assert.That(destinations, Has.Count.EqualTo(2));
            Assert.That(destinations.All(offset => body.Operations.Any(operation => operation.Offset == offset)), Is.True);
            Assert.Throws<NotSupportedException>(() => ((IList<int>)destinations)[0] = 0);
            Assert.Throws<NotSupportedException>(() => ((IList<HarmonyPublishedBody.Operation>)body.Operations).Clear());
            Assert.Throws<NotSupportedException>(() => ((IDictionary<int, HarmonyPublishedBody.TokenBinding>)body.Tokens).Clear());
            Assert.That(body.LocalSignature.Count, Is.GreaterThan(0));
            Assert.That(body.MaxStack, Is.GreaterThan(0));
            Assert.That(body.Operations.Any(operation => operation.Code == OpCodes.Ldc_I8 && Equals(operation.Operand, long.MinValue + 7)), Is.True);
            Assert.That(body.Operations.Any(operation => operation.Code == OpCodes.Ldc_R8 && Equals(operation.Operand, 1.25d)), Is.True);
            Assert.That(ContainsInteger(body, -7), Is.True);
            var field = body.Tokens.Values.Single(binding => binding.Kind == HarmonyPublishedBody.TokenKind.FieldHandle);
            Assert.That(field.Value, Is.EqualTo(typeof(TokenStorage).GetField(nameof(TokenStorage.Value))!.FieldHandle));
            Assert.That(field.TypeContext, Is.EqualTo(typeof(TokenStorage).TypeHandle));
            Assert.That(body.Tokens.Values.Any(binding => binding.Kind == HarmonyPublishedBody.TokenKind.TypeHandle && Equals(binding.Value, typeof(TokenStorage).TypeHandle)), Is.True);
            Assert.That(body.Tokens.Values.Any(binding => binding.Kind == HarmonyPublishedBody.TokenKind.String && Equals(binding.Value, "published-token")), Is.True);
            Assert.That(WithArgument(0), Is.EqualTo(17));
            Assert.That(WithArgument(1), Is.EqualTo(-7));
            Assert.That(WithArgument(2), Is.EqualTo(-9));
            Assert.That(tokenInitializations, Is.EqualTo(1));
            Assert.That(body.IsPublicationCurrent(), Is.True);
        }
        finally { owner.Unpatch(target, HarmonyPatchType.All, owner.Id); }
    }
    public static IEnumerable<CodeInstruction> OperandBody(IEnumerable<CodeInstruction> source, ILGenerator generator)
    {
        Label zero = generator.DefineLabel(), one = generator.DefineLabel(), end = generator.DefineLabel();
        LocalBuilder local = generator.DeclareLocal(typeof(int));
        var first = new CodeInstruction(OpCodes.Ldstr, "published-token"); first.labels.Add(zero);
        var second = new CodeInstruction(OpCodes.Ldc_I8, long.MinValue + 7); second.labels.Add(one);
        var result = new CodeInstruction(OpCodes.Ret); result.labels.Add(end);
        return new[]
        {
            new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Switch, new[] { zero, one }),
            new CodeInstruction(OpCodes.Ldc_I4, -9), new CodeInstruction(OpCodes.Br, end),
            first, new CodeInstruction(OpCodes.Pop), new CodeInstruction(OpCodes.Ldtoken, typeof(TokenStorage)), new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ldsfld, typeof(TokenStorage).GetField(nameof(TokenStorage.Value))!), new CodeInstruction(OpCodes.Br, end),
            second, new CodeInstruction(OpCodes.Pop), new CodeInstruction(OpCodes.Ldc_R8, 1.25d), new CodeInstruction(OpCodes.Pop),
            new CodeInstruction(OpCodes.Ldc_I4_S, (sbyte)-7), new CodeInstruction(OpCodes.Stloc, local), new CodeInstruction(OpCodes.Ldloc, local), result
        };
    }
    public static class TokenStorage
    {
        public static int Value;
        static TokenStorage() { tokenInitializations++; Value = 17; }
    }
    [MethodImpl(MethodImplOptions.NoInlining)] public static int WithArgument(int value) => value;

    [TestCase(false)] [TestCase(true)]
    public void OversizedPublishedBodiesOrScopesHaveAnExplicitLimitAndRecover(bool scope)
    {
        var owner = new HarmonyLib.Harmony("tests.published-body.limit");
        largeCount = scope ? 4096 : 32768;
        try
        {
            owner.Patch(Target, transpiler: PatchMethod(scope ? nameof(ManyStrings) : nameof(ManyOperations)));
            Assert.That(Native(), Is.EqualTo(37));
            Assert.That(HarmonyPublishedBody.TryCapture(Target, out var body, out var failure), Is.False);
            Assert.That(body, Is.Null);
            Assert.That(failure, Is.EqualTo(HarmonyPublishedBody.Failure.Limit));
            owner.Unpatch(Target, HarmonyPatchType.Transpiler, owner.Id);
            largeCount = scope ? 8 : 20;
            owner.Patch(Target, transpiler: PatchMethod(scope ? nameof(ManyStrings) : nameof(ManyOperations)));
            Assert.That(Native(), Is.EqualTo(37));
            Assert.That(ContainsInteger(Capture(Target), 37), Is.True);
        }
        finally { owner.Unpatch(Target, HarmonyPatchType.All, owner.Id); }
    }
    public static IEnumerable<CodeInstruction> ManyOperations(IEnumerable<CodeInstruction> source)
    {
        for (int i = 0; i < largeCount; i++) { yield return new CodeInstruction(OpCodes.Ldc_I4_0); yield return new CodeInstruction(OpCodes.Pop); }
        yield return new CodeInstruction(OpCodes.Ldc_I4, 37); yield return new CodeInstruction(OpCodes.Ret);
    }
    public static IEnumerable<CodeInstruction> ManyStrings(IEnumerable<CodeInstruction> source)
    {
        for (int i = 0; i < largeCount; i++) { yield return new CodeInstruction(OpCodes.Ldstr, "owned-token-" + i); yield return new CodeInstruction(OpCodes.Pop); }
        yield return new CodeInstruction(OpCodes.Ldc_I4, 37); yield return new CodeInstruction(OpCodes.Ret);
    }
    [Test]
    public void GenericAndInheritedRootsAreRefusedBeforeAnyAcquisition()
    {
        MethodInfo[] roots = { Method(nameof(Generic)).MakeGenericMethod(typeof(string)),
            typeof(GenericHolder<string>).GetMethod(nameof(GenericHolder<string>.Value))!, typeof(Derived).GetMethod(nameof(Base.Value))! };
        foreach (var root in roots)
        {
            Assert.That(HarmonyPublishedBody.TryCapture(root, out var body, out var failure), Is.False);
            Assert.That(body, Is.Null); Assert.That(failure, Is.EqualTo(HarmonyPublishedBody.Failure.InvalidRoot));
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(root), Is.Null);
        }
    }
    public static T Generic<T>(T value) => value;
    public sealed class GenericHolder<T> { public static int Value() => 1; }
    public class Base { public int Value() => 1; }
    public sealed class Derived : Base { }

    [TestCase("state")] [TestCase("detours")] [TestCase("box")] [TestCase("sync")]
    public void EveryRetainedProviderMonitorRefusesContentionWithoutBlocking(string monitor)
    {
        var owner = new HarmonyLib.Harmony("tests.published-body.monitor");
        try
        {
            owner.Patch(Target, transpiler: PatchMethod(nameof(Constant37)));
            var body = Capture(Target);
            Assembly provider = typeof(PatchProcessor).Assembly;
            object state = provider.GetType("HarmonyLib.HarmonySharedState")!.GetField("state", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            var detours = (System.Collections.IDictionary)provider.GetType("HarmonyLib.PatchTools")!.GetField("detours", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            object detour = detours[Target]!;
            object box = detour.GetType().BaseType!.GetField("DetourBox", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(detour)!;
            object sync = box.GetType().BaseType!.GetField("Sync", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(box)!;
            object held = monitor switch { "state" => state, "detours" => detours, "box" => box, _ => sync };
            Monitor.Enter(held);
            try
            {
                var attempt = Task.Run(() =>
                {
                    bool read = HarmonyPublishedBody.TryCapture(Target, out var captured, out var failure);
                    return (read, captured, failure, current: body.IsPublicationCurrent());
                });
                Assert.That(attempt.Wait(TimeSpan.FromSeconds(3)), Is.True);
                Assert.That(attempt.Result.read, Is.False); Assert.That(attempt.Result.captured, Is.Null);
                Assert.That(attempt.Result.failure, Is.EqualTo(HarmonyPublishedBody.Failure.Busy));
                Assert.That(attempt.Result.current, Is.False);
            }
            finally { Monitor.Exit(held); }
            Assert.That(body.IsPublicationCurrent(), Is.True);
        }
        finally { owner.Unpatch(Target, HarmonyPatchType.All, owner.Id); }
    }
    [Test]
    public void SwitchTableLimitRefusesAnActualPublishedBodyWithoutTruncatingIt()
    {
        var owner = new HarmonyLib.Harmony("tests.published-body.switch-limit");
        MethodInfo target = Method(nameof(WithArgument)); largeCount = 257;
        try
        {
            owner.Patch(target, transpiler: PatchMethod(nameof(ManyBranches)));
            Assert.That(WithArgument(256), Is.EqualTo(293));
            Assert.That(HarmonyPublishedBody.TryCapture(target, out var body, out var failure), Is.False);
            Assert.That(body, Is.Null); Assert.That(failure, Is.EqualTo(HarmonyPublishedBody.Failure.Limit));
            owner.Unpatch(target, HarmonyPatchType.Transpiler, owner.Id);
            largeCount = 256;
            owner.Patch(target, transpiler: PatchMethod(nameof(ManyBranches)));
            var admitted = Capture(target);
            var table = (IReadOnlyList<int>)admitted.Operations.Single(operation => operation.Code == OpCodes.Switch).Operand!;
            Assert.That(table, Has.Count.EqualTo(256)); Assert.That(WithArgument(255), Is.EqualTo(292));
        }
        finally { owner.Unpatch(target, HarmonyPatchType.All, owner.Id); }
    }
    public static IEnumerable<CodeInstruction> ManyBranches(IEnumerable<CodeInstruction> source, ILGenerator generator)
    {
        Label[] labels = Enumerable.Range(0, largeCount).Select(_ => generator.DefineLabel()).ToArray();
        var code = new List<CodeInstruction> { new(OpCodes.Ldarg_0), new(OpCodes.Switch, labels), new(OpCodes.Ldc_I4_M1), new(OpCodes.Ret) };
        for (int i = 0; i < labels.Length; i++)
        {
            var value = new CodeInstruction(OpCodes.Ldc_I4, 37 + i); value.labels.Add(labels[i]);
            code.Add(value); code.Add(new CodeInstruction(OpCodes.Ret));
        }
        return code;
    }
}
