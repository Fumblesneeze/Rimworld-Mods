using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Xml;
using HarmonyLib;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Patching;

[TestFixture, NonParallelizable]
public sealed class NativeDefinitionConversionPublishedEffectsTests
{
    private static string? context;
    private static int entered;
    private static int observations, attributeConstructions;
    private static bool throwObserver, failRebuild;
    private static Action? duringParse;
    private static readonly Exception ForeignFailure = new InvalidOperationException("foreign observer failed");
    private static readonly List<string?> observed = new();
    private static readonly MethodInfo Target = typeof(Converter).GetMethod(nameof(Converter.Convert))!;
    private static MethodInfo Helper(string name) => typeof(NativeDefinitionConversionPatch).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;
    private static MethodInfo Callback(string name) => typeof(NativeDefinitionConversionPublishedEffectsTests).GetMethod(name)!;
    public sealed class Asset;
    public sealed record Result(string Name);
    private sealed class Token(Action dispose) : IDisposable { public void Dispose() => dispose(); }
    private static NativeDefinitionConversionPatch Create(Func<bool>? safe = null) => new(typeof(Converter), nameof(Converter.Convert), typeof(Asset), typeof(Result),
        typeof(Converter).GetMethod(nameof(Converter.Resolve))!, typeof(Func<XmlNode, bool, object>).GetMethod("Invoke")!, (node, _) =>
        {
            entered++;
            string? previous = context;
            context = node.Name;
            return new Token(() => context = previous);
        }, safe);
    private static XmlNode Node(string name)
    {
        var document = new XmlDocument();
        document.LoadXml("<" + name + " />");
        return document.DocumentElement!;
    }
    private static void Admit(NativeDefinitionConversionPatch route)
    {
        Assert.That(new AdmissionCoordinator(_ => { }).Apply(new PatchGroup("converter", AdmissionPhase.Early, [route.Candidate]), AdmissionPhase.Early), Is.True);
        route.Enable();
    }

    [Test]
    public void SkippedDisposalPreventsAcquisitionAndRemovalAdmitsFreshScope()
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.converter.inner-skip");
        var route = Create();
        try
        {
            context = null; entered = 0; observed.Clear();
            Admit(route);
            foreign.CreateProcessor(Target).AddInnerPrefix(new HarmonyMethod(Callback(nameof(Skip))) { innerMethod = new InnerMethod(Helper("Exit")) }).Patch();
            Assert.That(Converter.Convert(Node("Blocked"), null).Name, Is.EqualTo("Blocked"));
            Assert.Multiple(() =>
            {
                Assert.That(entered, Is.Zero);
                Assert.That(observed, Is.EqualTo(new string?[] { null }));
                Assert.That(context, Is.Null, "A skipped disposal must never strand a newly acquired context.");
            });
            foreign.Unpatch(Target, HarmonyPatchType.InnerPrefix, foreign.Id);
            Converter.Convert(Node("Fresh"), null);
            Assert.That(observed, Is.EqualTo(new string?[] { null, "Fresh" }));
            Assert.That(context, Is.Null);
        }
        finally
        {
            try { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); }
            finally { route.Candidate.Rollback(); context = null; }
        }
    }
    public static bool Skip() => false;
    private static readonly ManualResetEventSlim CompositionEntered = new(false), ReleaseComposition = new(false);
    [TestCase(true, true)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    public void BusyProviderPreservesOnlyAlreadyAdmittedSafeGeneration(bool published, bool safe)
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.converter.busy");
        var route = Create(() => safe);
        Task? composition = null;
        MethodInfo probe = typeof(Converter).GetMethod(nameof(Converter.Probe))!;
        try
        {
            context = null; entered = 0; observed.Clear();
            Admit(route);
            if (!published) foreign.CreateProcessor(Target).Patch();
            CompositionEntered.Reset(); ReleaseComposition.Reset();
            composition = Task.Run(() => foreign.Patch(probe, transpiler: new HarmonyMethod(Callback(nameof(HoldComposition)))));
            Assert.That(CompositionEntered.Wait(TimeSpan.FromSeconds(3)), Is.True);
            Converter.Convert(Node("Busy"), null);
            Assert.That(observed, Is.EqualTo(new string?[] { published && safe ? "Busy" : null }));
            ReleaseComposition.Set();
            Assert.That(composition.Wait(TimeSpan.FromSeconds(3)), Is.True);
            Converter.Convert(Node("Retry"), null);
            Assert.That(observed.Last(), Is.EqualTo(safe ? "Retry" : null));
            Assert.That(context, Is.Null);
        }
        finally
        {
            ReleaseComposition.Set();
            try { if (composition != null) Assert.That(composition.Wait(TimeSpan.FromSeconds(3)), Is.True); }
            finally
            {
                try { foreign.Unpatch(probe, HarmonyPatchType.All, foreign.Id); }
                finally { route.Candidate.Rollback(); context = null; }
            }
        }
    }
    public static IEnumerable<CodeInstruction> HoldComposition(IEnumerable<CodeInstruction> body)
    {
        CompositionEntered.Set();
        if (!ReleaseComposition.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Bounded composition was not released.");
        return body;
    }
    [Test]
    public void OversizedPublishedMetadataCannotPreventForeignRemovalAndLaterRecovery()
    {
        var foreign = new HarmonyLib.Harmony(new string('x', 4 * 1024 * 1024));
        var route = Create();
        try
        {
            context = null; entered = 0; observed.Clear();
            Admit(route);
            foreign.Patch(Target, transpiler: new HarmonyMethod(Callback(nameof(Identity))));
            Converter.Convert(Node("Oversized"), null);
            Assert.That(observed, Is.EqualTo(new string?[] { null }));
            Assert.That(route.Failure, Does.Contain("4 MiB"));
            Assert.DoesNotThrow(() => foreign.Unpatch(Target, HarmonyPatchType.Transpiler, foreign.Id));
            Converter.Convert(Node("Removed"), null);
            Assert.That(observed.Last(), Is.Null, "Failed inspection must leave this rebuild unchanged and unadmitted.");
            new HarmonyLib.Harmony("tests.megamod.converter.recover").CreateProcessor(Target).Patch();
            Converter.Convert(Node("Recovered"), null);
            Assert.That(observed.Last(), Is.EqualTo("Recovered"));
            Assert.That(context, Is.Null);
        }
        finally
        {
            // Even the pre-fix reader failure must not strand this fixture's owned adapter.
            // Only this known fixture target/transpiler owner is removed, never another target.
            try { new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId).Unpatch(Target, HarmonyPatchType.Transpiler, OwnedHarmonyPatch.OwnerId); }
            finally
            {
                try { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); }
                finally { route.Candidate.Rollback(); context = null; }
            }
        }
    }
    public static IEnumerable<CodeInstruction> Identity(IEnumerable<CodeInstruction> body) => body;
    [Test]
    public void ThrowingDisposalObserverCannotStrandContext()
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.converter.inner-throw-exit");
        var route = Create();
        try
        {
            context = null; entered = 0; observed.Clear();
            Admit(route);
            foreign.CreateProcessor(Target).AddInnerPrefix(new HarmonyMethod(Callback(nameof(Throw))) { innerMethod = new InnerMethod(Helper("Exit")) }).Patch();
            Assert.That(Assert.Throws<InvalidOperationException>(() => Converter.Convert(Node("Blocked"), null)), Is.SameAs(ForeignFailure));
            Assert.Multiple(() => { Assert.That(entered, Is.Zero); Assert.That(context, Is.Null); });
        }
        finally
        {
            try { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); }
            finally { route.Candidate.Rollback(); context = null; }
        }
    }
    public static void Throw() => throw ForeignFailure;
    [TestCase("Enter", true, false)]
    [TestCase("Enter", true, true)]
    [TestCase("Exit", false, false)]
    [TestCase("Exit", false, true)]
    [TestCase("Resolve", false, false)]
    [TestCase("Resolve", false, true)]
    public void CompatibleObservationPreservesCleanupAndForeignExceptions(string site, bool prefix, bool throws)
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.converter.observer");
        var route = Create();
        try
        {
            context = null; entered = observations = 0; throwObserver = throws; observed.Clear();
            Admit(route);
            var observation = new HarmonyMethod(Callback(nameof(Observe)))
                { innerMethod = new InnerMethod(site == "Resolve" ? typeof(Converter).GetMethod(nameof(Converter.Resolve))! : Helper(site)) };
            var processor = foreign.CreateProcessor(Target);
            if (prefix) processor.AddInnerPrefix(observation); else processor.AddInnerPostfix(observation);
            processor.Patch();
            Assert.That(observations, Is.Zero);
            if (throws) Assert.That(Assert.Throws<InvalidOperationException>(() => Converter.Convert(Node("Observed"), null)), Is.SameAs(ForeignFailure));
            else Assert.That(Converter.Convert(Node("Observed"), null).Name, Is.EqualTo("Observed"));
            Assert.That(entered, Is.EqualTo(throws && site == "Enter" ? 0 : 1));
            Assert.That(context, Is.Null);
            Assert.That(observations, Is.EqualTo(1));
            if (!throws) Assert.That(observed, Is.EqualTo(new[] { "Observed" }));
        }
        finally
        {
            throwObserver = false;
            try { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); }
            finally { route.Candidate.Rollback(); context = null; }
        }
    }
    public static void Observe() { observations++; if (throwObserver) throw ForeignFailure; }

    [Test]
    public void HarmlessNopsCannotHideAChangedRequiredParserArgument()
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.converter.parse-argument");
        var route = Create();
        try
        {
            context = null; entered = 0; observed.Clear();
            Admit(route);
            foreign.CreateProcessor(Target)
                .AddTranspiler(new HarmonyMethod(Callback(nameof(SeparateArguments))))
                .AddInnerPostfix(new HarmonyMethod(Callback(nameof(ChangeFlag))) { innerTarget = InnerTarget.Constant(1) }).Patch();
            Converter.Convert(Node("Changed"), null);
            Assert.That(entered, Is.Zero);
            Assert.That(observed, Is.EqualTo(new string?[] { null }));
            Assert.That(context, Is.Null);
            foreign.Unpatch(Target, HarmonyPatchType.InnerPostfix, foreign.Id);
            Converter.Convert(Node("OnlyNops"), null);
            Assert.That(observed, Is.EqualTo(new string?[] { null, "OnlyNops" }));
            Assert.That(context, Is.Null);
        }
        finally
        {
            try { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); }
            finally { route.Candidate.Rollback(); context = null; }
        }
    }
    public static void ChangeFlag(ref int __result) => __result = 0;
    public static IEnumerable<CodeInstruction> SeparateArguments(IEnumerable<CodeInstruction> body)
    {
        foreach (var instruction in body)
        {
            if (instruction.Calls(typeof(Func<XmlNode, bool, object>).GetMethod("Invoke")))
                for (int i = 0; i < 6; i++) yield return new CodeInstruction(OpCodes.Nop);
            yield return instruction;
        }
    }

    [Test]
    public void PreflightAndEntryInspectionDoNotConstructForeignAttributes()
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.converter.attribute-observer");
        var route = Create();
        try
        {
            context = null; entered = observations = attributeConstructions = 0; observed.Clear();
            foreign.CreateProcessor(Target).AddInnerPostfix(new HarmonyMethod(Callback(nameof(ObserveScalar)))
                { innerMethod = new InnerMethod(typeof(Converter).GetMethod(nameof(Converter.Probe))!) }).Patch();
            Assert.That(attributeConstructions, Is.GreaterThan(0));
            attributeConstructions = 0;
            Assert.That(route.Candidate.Validate(), Is.Null);
            Assert.That(attributeConstructions, Is.Zero, "Public candidate and receipt preflight must be metadata-only.");
            Admit(route);
            attributeConstructions = 0;
            Converter.Convert(Node("Inspected"), null);
            Assert.That(attributeConstructions, Is.Zero);
            Assert.That(observations, Is.EqualTo(1));
            Assert.That(observed, Is.EqualTo(new[] { "Inspected" }));
            Assert.That(context, Is.Null);
        }
        finally
        {
            try { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); }
            finally { route.Candidate.Rollback(); context = null; }
        }
    }
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Method)]
    public sealed class CountAttribute : Attribute { public CountAttribute() => attributeConstructions++; }
    [Count] public static void ObserveScalar([Count] int __result) => observations++;

    [Test]
    public void OlderBodyCannotBorrowFreshConversionPublication()
    {
        var composer = new HarmonyLib.Harmony("tests.megamod.converter.old-body");
        var route = Create();
        try
        {
            context = null; entered = 0; observed.Clear();
            Admit(route);
            MethodInfo older = composer.CreateProcessor(Target).Patch();
            composer.CreateProcessor(Target).Patch();
            Converter.Convert(Node("Current"), null);
            older.Invoke(null, new object?[] { Node("Old"), null });
            Assert.That(observed, Is.EqualTo(new string?[] { "Current", null }));
            Assert.That(entered, Is.EqualTo(1));
            Assert.That(context, Is.Null);
        }
        finally { route.Candidate.Rollback(); context = null; }
    }

    [Test]
    public void AcquiredTokenRestoresAfterPublicationRevocationAndNativeFailure()
    {
        var route = Create();
        try
        {
            context = "caller"; entered = 0; observed.Clear();
            Admit(route);
            duringParse = () => { route.Candidate.Rollback(); throw ForeignFailure; };
            Assert.That(Assert.Throws<InvalidOperationException>(() => Converter.Convert(Node("Active"), null)), Is.SameAs(ForeignFailure));
            Assert.That(observed, Is.EqualTo(new[] { "Active" }));
            Assert.That(context, Is.EqualTo("caller"));
        }
        finally { duringParse = null; route.Candidate.Rollback(); context = null; }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ReentrantOrFailedCompositionCannotAcquireUsingOldPublication(bool fail)
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.converter.reentrant");
        var route = Create();
        try
        {
            context = null; entered = 0; observed.Clear(); failRebuild = fail;
            Admit(route);
            var late = new HarmonyMethod(Callback(nameof(Reenter))) { priority = int.MinValue, after = [OwnedHarmonyPatch.OwnerId] };
            if (fail) Assert.Catch(() => foreign.Patch(Target, transpiler: late));
            else foreign.Patch(Target, transpiler: late);
            Converter.Convert(Node("Unpublished"), null);
            Assert.That(entered, Is.Zero);
            Assert.That(observed, Is.EqualTo(new string?[] { null, null }));
            Assert.That(context, Is.Null);
            failRebuild = false;
            foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id);
            Converter.Convert(Node("Recovered"), null);
            Assert.That(observed.Last(), Is.EqualTo("Recovered"));
            Assert.That(context, Is.Null);
        }
        finally
        {
            failRebuild = false;
            try { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); }
            finally { route.Candidate.Rollback(); context = null; }
        }
    }
    public static IEnumerable<CodeInstruction> Reenter(IEnumerable<CodeInstruction> body)
    {
        Converter.Convert(Node("DuringComposition"), null);
        if (failRebuild) throw ForeignFailure;
        return body;
    }
    [Test]
    public void ThrowingAcquisitionPostfixCannotLoseAnAcquiredToken()
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.converter.inner-throw-enter");
        var route = Create();
        try
        {
            context = null; entered = 0; observed.Clear();
            Admit(route);
            foreign.CreateProcessor(Target).AddInnerPostfix(new HarmonyMethod(Callback(nameof(Throw))) { innerMethod = new InnerMethod(Helper("Enter")) }).Patch();
            Assert.That(Assert.Throws<InvalidOperationException>(() => Converter.Convert(Node("Blocked"), null)), Is.SameAs(ForeignFailure));
            Assert.Multiple(() => { Assert.That(entered, Is.Zero); Assert.That(context, Is.Null); });
        }
        finally
        {
            try { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); }
            finally { route.Candidate.Rollback(); context = null; }
        }
    }
    public static class Converter
    {
        public static XmlNode Resolve(XmlNode node) => node;
        public static int Probe() => 1;
        public static readonly Func<XmlNode, bool, object> Parse = (node, _) => { observed.Add(context); duringParse?.Invoke(); return new Result(node.Name); };
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static Result Convert(XmlNode node, Asset? asset)
        {
            _ = Probe();
            _ = Resolve(node);
            return (Result)Parse(node, true);
        }
    }
}
