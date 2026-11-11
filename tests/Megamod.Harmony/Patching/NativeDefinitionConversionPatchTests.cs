using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Xml;
using HarmonyLib;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Patching;

[TestFixture, NonParallelizable]
public sealed class NativeDefinitionConversionPatchTests
{
    private static string? current;
    private static readonly List<string?> observed = new();
    private static Action<XmlNode>? duringParse;
    public sealed class Asset;
    public sealed record Result(string Name);
    private sealed class Exit(Action exit) : IDisposable { public void Dispose() => exit(); }

    [Test]
    public void ABranchCannotSubstituteTheParserNodeAfterTheProvedArgumentLoad()
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.converter.binding");
        var target = typeof(Converter).GetMethod(nameof(Converter.Convert))!;
        var patch = Create();
        try
        {
            foreign.Patch(target, transpiler: new HarmonyMethod(typeof(NativeDefinitionConversionPatchTests), nameof(BypassNode)));
            var coordinator = new AdmissionCoordinator(_ => { });
            Assert.That(coordinator.Apply(new PatchGroup("converter", AdmissionPhase.Early, [patch.Candidate]), AdmissionPhase.Early), Is.False);
        }
        finally
        {
            try { patch.Candidate.Rollback(); }
            finally { foreign.Unpatch(target, HarmonyPatchType.Transpiler, foreign.Id); }
        }
    }

    public static IEnumerable<CodeInstruction> BypassNode(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var body = instructions.ToList();
        int invoke = body.FindIndex(i => i.Calls(typeof(Func<XmlNode, bool, object>).GetMethod("Invoke")));
        Label afterNode = generator.DefineLabel();
        body[invoke - 1].labels.Add(afterNode);
        body.InsertRange(invoke - 2, new[] { new CodeInstruction(OpCodes.Ldnull), new CodeInstruction(OpCodes.Br, afterNode) });
        return body;
    }

    private static NativeDefinitionConversionPatch Create(Type? converter = null, MethodInfo? parse = null) => new(converter ?? typeof(Converter), nameof(Converter.Convert),
        typeof(Asset), typeof(Result), typeof(Converter).GetMethod(nameof(Converter.Resolve))!,
        parse ?? typeof(Func<XmlNode, bool, object>).GetMethod("Invoke")!, (node, asset) =>
        {
            string? previous = current;
            current = node.Name + (asset == null ? ":unknown" : ":asset");
            return new Exit(() => current = previous);
        });

    [Test]
    public void SkippingTheOriginalNeverCreatesAConversionScope()
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.converter.skip");
        MethodInfo target = typeof(Converter).GetMethod(nameof(Converter.Convert))!;
        var patch = Create();
        try
        {
            current = null;
            observed.Clear();
            foreign.Patch(target, prefix: new HarmonyMethod(typeof(NativeDefinitionConversionPatchTests), nameof(Skip)));
            var coordinator = new AdmissionCoordinator(_ => { });
            Assert.That(coordinator.Apply(new PatchGroup("converter", AdmissionPhase.Early, [patch.Candidate]), AdmissionPhase.Early), Is.True);
            patch.Enable();
            Assert.That(Converter.Convert(Node("Skipped"), null), Is.Null);
            Assert.That(current, Is.Null);
            Assert.That(observed, Is.Empty);
        }
        finally
        {
            try { patch.Candidate.Rollback(); }
            finally { foreign.Unpatch(target, HarmonyPatchType.Prefix, foreign.Id); }
        }
    }
    public static bool Skip() => false;

    [TestCase(false)]
    [TestCase(true)]
    public void LaterRebuildRefreshesOnlyAProvedFinalComposition(bool afterOwned)
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.converter.rebuild");
        var target = typeof(Converter).GetMethod(nameof(Converter.Convert))!;
        var patch = Create();
        try
        {
            current = null;
            observed.Clear();
            var coordinator = new AdmissionCoordinator(_ => { });
            Assert.That(coordinator.Apply(new PatchGroup("converter", AdmissionPhase.Early, [patch.Candidate]), AdmissionPhase.Early), Is.True);
            patch.Enable();
            var method = new HarmonyMethod(typeof(NativeDefinitionConversionPatchTests), nameof(Benign));
            if (afterOwned) method.after = new[] { OwnedHarmonyPatch.OwnerId };
            foreign.Patch(target, transpiler: method);
            Converter.Convert(Node("Rebuilt"), null);
            Assert.That(observed, Is.EqualTo(new string?[] { afterOwned ? null : "Rebuilt:unknown" }));
            Assert.That(current, Is.Null);
        }
        finally
        {
            try { patch.Candidate.Rollback(); }
            finally { foreign.Unpatch(target, HarmonyPatchType.Transpiler, foreign.Id); current = null; }
        }
    }
    public static IEnumerable<CodeInstruction> Benign(IEnumerable<CodeInstruction> body) => new[] { new CodeInstruction(OpCodes.Nop) }.Concat(body);
    private static XmlNode Node(string name)
    {
        var document = new XmlDocument();
        document.LoadXml("<" + name + " />");
        return document.DocumentElement!;
    }

    [Test]
    public void NestedUnknownAssetRestoresOuterContextAndAnEscapingExceptionRestoresCaller()
    {
        var patch = Create();
        var expected = new InvalidOperationException("native failure");
        try
        {
            observed.Clear();
            current = "caller";
            duringParse = node =>
            {
                if (node.Name == "Outer")
                {
                    Converter.Convert(Node("Inner"), null);
                    observed.Add(current);
                    throw expected;
                }
            };
            var coordinator = new AdmissionCoordinator(_ => { });
            Assert.That(coordinator.Apply(new PatchGroup("converter", AdmissionPhase.Early, [patch.Candidate]), AdmissionPhase.Early), Is.True);
            patch.Enable();
            Assert.That(Assert.Throws<InvalidOperationException>(() => Converter.Convert(Node("Outer"), new Asset())), Is.SameAs(expected));
            Assert.That(observed, Is.EqualTo(new[] { "Outer:asset", "Inner:unknown", "Outer:asset" }));
            Assert.That(current, Is.EqualTo("caller"));
        }
        finally { patch.Candidate.Rollback(); duringParse = null; current = null; }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NativeNewDelegateAndExistingCatchFinallyPreserveOutcome(bool fail)
    {
        var patch = Create(typeof(NewConverter), typeof(NewConverter.Parser).GetMethod("Invoke")!);
        var expected = new InvalidOperationException("native new failure");
        try
        {
            current = null;
            observed.Clear();
            duringParse = _ => { if (fail) throw expected; };
            var coordinator = new AdmissionCoordinator(_ => { });
            Assert.That(coordinator.Apply(new PatchGroup("converter", AdmissionPhase.Early, [patch.Candidate]), AdmissionPhase.Early), Is.True);
            patch.Enable();
            if (fail) Assert.That(Assert.Throws<InvalidOperationException>(() => NewConverter.Convert(Node("New"), null)), Is.SameAs(expected));
            else Assert.That(NewConverter.Convert(Node("New"), null).Name, Is.EqualTo("New"));
            Assert.That(observed, Is.EqualTo(fail ? new[] { "New:unknown", "catch:New:unknown", "finally:New:unknown" }
                : new[] { "New:unknown", "finally:New:unknown" }));
            Assert.That(current, Is.Null);
        }
        finally { patch.Candidate.Rollback(); duringParse = null; current = null; }
    }

    public static class NewConverter
    {
        public delegate Result Parser(int unused, int unused2, XmlNode node, Type type);
        public static readonly Parser Parse = (_, _, node, _) => (Result)Converter.Parse(node, true);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Result Convert(XmlNode node, Asset? asset)
        {
            if (node.NodeType != XmlNodeType.Element) return null!;
            _ = Converter.Resolve(node);
            Type type = typeof(Result);
            try { return Parse(0, 0, node, type); }
            catch { observed.Add("catch:" + current); throw; }
            finally { observed.Add("finally:" + current); }
        }
    }

    [Test]
    public void ActualBodyOwnsContextUntilReturnAndPreservesResult()
    {
        var patch = Create();
        try
        {
            current = null;
            observed.Clear();
            var coordinator = new AdmissionCoordinator(_ => { });
            Assert.That(coordinator.Apply(new PatchGroup("converter", AdmissionPhase.Early, [patch.Candidate]), AdmissionPhase.Early), Is.True);
            patch.Enable();
            var xml = new XmlDocument();
            xml.LoadXml("<Actual />");
            Result result = Converter.Convert(xml.DocumentElement!, new Asset());
            Assert.That(observed, Is.EqualTo(new[] { "Actual:asset" }));
            Assert.That(result.Name, Is.EqualTo("Actual"));
            Assert.That(current, Is.Null);
        }
        finally { patch.Candidate.Rollback(); current = null; }
    }

    public static class Converter
    {
        public static XmlNode Resolve(XmlNode node) => node;
        public static readonly Func<XmlNode, bool, object> Parse = (node, _) =>
        {
            observed.Add(current);
            duringParse?.Invoke(node);
            return new Result(node.Name);
        };
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Result Convert(XmlNode node, Asset? asset)
        {
            _ = Resolve(node);
            return (Result)Parse(node, true);
        }
    }
}
