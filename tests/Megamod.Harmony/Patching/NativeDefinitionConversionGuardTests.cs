using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using System.Xml;
using HarmonyLib;
using Megamod.Features.Diagnostics;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Patching;

[TestFixture, NonParallelizable]
public sealed class NativeDefinitionConversionGuardTests
{
    private static readonly MethodInfo Current = typeof(CurrentConverter).GetMethod(nameof(CurrentConverter.DefFromNodeNew))!;
    private static readonly MethodInfo Legacy = typeof(LegacyConverter).GetMethod(nameof(LegacyConverter.DefFromNode))!;
    private static readonly MethodInfo Exit = typeof(NativeDefinitionConversionPatch).GetMethod("Exit", BindingFlags.NonPublic | BindingFlags.Static)!;
    private static readonly DefinitionContextScope Scope = new();
    private static readonly List<string?> Observed = new();
    private static Action<string>? parsing;
    private static NativeDefinitionConversionGuard? coverage;
    private static bool grandchildIsCurrent;
    private static int factoryCalls;
    private static Action<MethodBase>? afterGuard;
    public sealed class Asset;
    public sealed record Result(string Name);
    private static IDisposable? Enter(XmlNode node, object? _)
    {
        var remainsValid = coverage?.CaptureValidity();
        return remainsValid == null ? null : Scope.Enter(Context(node.Name), remainsValid);
    }
    private static DefinitionContext Context(string name) => new(name, null, null, null, null, null);
    private static NativeDefinitionConversionPatch Route(bool current) => new(current ? typeof(CurrentConverter) : typeof(LegacyConverter),
        current ? nameof(CurrentConverter.DefFromNodeNew) : nameof(LegacyConverter.DefFromNode), typeof(Asset), typeof(Result),
        typeof(NativeDefinitionConversionGuardTests).GetMethod(nameof(Resolve))!,
        current ? typeof(CurrentConverter.Parser).GetMethod("Invoke")! : typeof(Func<XmlNode, bool, object>).GetMethod("Invoke")!, Enter,
        rejectedEntry: Scope.InvalidateCurrent);
    private static Result Invoke(bool current, string name)
    {
        var document = new XmlDocument(); document.LoadXml("<" + name + " />");
        return current ? CurrentConverter.DefFromNodeNew(document.DocumentElement!, null) : LegacyConverter.DefFromNode(document.DocumentElement!, null);
    }
    private static void Admit(NativeDefinitionConversionPatch current, NativeDefinitionConversionPatch legacy)
    {
        coverage = new NativeDefinitionConversionGuard(current, legacy, () => Scope.HasActiveFrame, Scope.InvalidateCurrent, () => true);
        Assert.That(new AdmissionCoordinator(_ => { }).Apply(new PatchGroup("definition-family", AdmissionPhase.Early,
            new[] { current.Candidate, legacy.Candidate }.Concat(coverage.Candidates).ToArray()), AdmissionPhase.Early), Is.True);
        current.Enable(); legacy.Enable(); coverage.Enable();
    }
    private static void Cleanup(NativeDefinitionConversionPatch current, NativeDefinitionConversionPatch legacy)
    {
        parsing = null; afterGuard = null;
        try { if (coverage != null) foreach (var candidate in coverage.Candidates.Reverse()) candidate.Rollback(); }
        finally { try { current.Candidate.Rollback(); } finally { legacy.Candidate.Rollback(); coverage = null; } }
    }
    [TestCase(true, false)]
    [TestCase(false, false)]
    [TestCase(true, true)]
    [TestCase(false, true)]
    public void RejectedLoaderNeverBorrowsParentContext(bool parentIsCurrent, bool sameLoader)
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.guard.nested");
        var current = Route(true); var legacy = Route(false);
        bool childIsCurrent = sameLoader ? parentIsCurrent : !parentIsCurrent;
        MethodInfo child = childIsCurrent ? Current : Legacy;
        try
        {
            Observed.Clear(); Admit(current, legacy);
            parsing = name =>
            {
                if (name != "Parent") return;
                foreign.CreateProcessor(child).AddInnerPrefix(new HarmonyMethod(typeof(NativeDefinitionConversionGuardTests).GetMethod(nameof(Skip))!)
                    { innerMethod = new InnerMethod(Exit) }).Patch();
                Invoke(childIsCurrent, "Child");
                Observed.Add(Scope.Current?.DefinitionName);
            };
            using (Scope.Enter(Context("Caller")))
            {
                Invoke(parentIsCurrent, "Parent");
                Assert.That(Observed, Is.EqualTo(new string?[] { "Parent", null, null }));
                Assert.That(Scope.Current?.DefinitionName, Is.EqualTo("Caller"));
            }
            Assert.That(Scope.Current, Is.Null);
        }
        finally { try { foreign.Unpatch(child, HarmonyPatchType.All, foreign.Id); } finally { Cleanup(current, legacy); } }
    }
    public static bool Skip() => false;
    [TestCase(true)]
    [TestCase(false)]
    public void RemovedChildGuardCannotReuseThePreviousPairPublication(bool parentIsCurrent)
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.guard.removed-prefix");
        var current = Route(true); var legacy = Route(false);
        MethodInfo child = parentIsCurrent ? Legacy : Current;
        try
        {
            Observed.Clear(); Admit(current, legacy);
            foreign.Unpatch(child, HarmonyPatchType.Prefix, OwnedHarmonyPatch.OwnerId);
            Invoke(!parentIsCurrent, "UncoveredChild");
            Assert.That(Observed, Is.EqualTo(new string?[] { null }));
            Assert.That(coverage!.Available, Is.False);
            Invoke(parentIsCurrent, "UncoveredParent");
            Assert.That(Observed, Is.EqualTo(new string?[] { null, null }));
        }
        finally { Cleanup(current, legacy); }
    }
    [TestCase(true)]
    [TestCase(false)]
    public void EarlierChildPrefixCannotBorrowParentContextThroughTheOtherLoader(bool parentIsCurrent)
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.guard.earlier");
        var current = Route(true); var legacy = Route(false);
        MethodInfo child = parentIsCurrent ? Legacy : Current;
        try
        {
            Observed.Clear(); Admit(current, legacy); grandchildIsCurrent = parentIsCurrent;
            parsing = name =>
            {
                if (name != "Parent") return;
                foreign.Patch(child, prefix: new HarmonyMethod(typeof(NativeDefinitionConversionGuardTests).GetMethod(nameof(BeforeChild))!)
                    { priority = int.MaxValue, before = [OwnedHarmonyPatch.OwnerId] });
                Invoke(!parentIsCurrent, "Child");
                Observed.Add(Scope.Current?.DefinitionName);
            };
            Invoke(parentIsCurrent, "Parent");
            Assert.That(Observed, Is.EqualTo(new string?[] { "Parent", null, null, null }));
            Assert.That(Scope.Current, Is.Null);
        }
        finally { try { foreign.Unpatch(child, HarmonyPatchType.All, foreign.Id); } finally { Cleanup(current, legacy); } }
    }
    public static void BeforeChild(XmlNode node) { if (node.Name == "Child") Invoke(grandchildIsCurrent, "Grandchild"); }
    [TestCase(true)]
    [TestCase(false)]
    public void EarlierChildPrefixReportsWithoutBorrowingTheActiveParent(bool parentIsCurrent)
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.guard.early-report");
        var current = Route(true); var legacy = Route(false);
        MethodInfo child = parentIsCurrent ? Legacy : Current;
        try
        {
            Observed.Clear(); Admit(current, legacy);
            parsing = name =>
            {
                if (name != "Parent") return;
                foreign.Patch(child, prefix: new HarmonyMethod(typeof(NativeDefinitionConversionGuardTests).GetMethod(nameof(ReportBeforeChild))!)
                    { priority = int.MaxValue, before = [OwnedHarmonyPatch.OwnerId] });
                Invoke(!parentIsCurrent, "Child");
                Observed.Add(Scope.Current?.DefinitionName);
            };
            using (Scope.Enter(Context("Caller")))
            {
                Invoke(parentIsCurrent, "Parent");
                Assert.That(Observed, Is.EqualTo(new string?[] { "Parent", null, null, null }));
                Assert.That(Scope.Current?.DefinitionName, Is.EqualTo("Caller"));
            }
        }
        finally { try { foreign.Unpatch(child, HarmonyPatchType.All, foreign.Id); } finally { Cleanup(current, legacy); } }
    }
    public static void ReportBeforeChild() => Observed.Add(Scope.Current?.DefinitionName);
    [TestCase(true)]
    [TestCase(false)]
    public void RetainedChildBodyCannotBorrowFreshParentContext(bool parentIsCurrent)
    {
        var composer = new HarmonyLib.Harmony("tests.megamod.guard.old-body");
        var current = Route(true); var legacy = Route(false);
        MethodInfo child = parentIsCurrent ? Legacy : Current;
        try
        {
            Observed.Clear(); Admit(current, legacy);
            MethodInfo older = composer.CreateProcessor(child).Patch();
            composer.CreateProcessor(child).Patch();
            parsing = name =>
            {
                if (name != "Parent") return;
                var document = new XmlDocument(); document.LoadXml("<Child />");
                older.Invoke(null, [document.DocumentElement!, null]);
                Observed.Add(Scope.Current?.DefinitionName);
            };
            using (Scope.Enter(Context("Caller")))
            {
                Invoke(parentIsCurrent, "Parent");
                Assert.That(Observed, Is.EqualTo(new string?[] { "Parent", null, null }));
                Assert.That(Scope.Current?.DefinitionName, Is.EqualTo("Caller"));
            }
        }
        finally { Cleanup(current, legacy); }
    }
    [TestCase(true)]
    [TestCase(false)]
    public void RebuildBetweenGuardAndBodyHidesChildAndParentRemainder(bool parentIsCurrent)
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.guard.after-guard");
        var current = Route(true); var legacy = Route(false);
        MethodInfo child = parentIsCurrent ? Legacy : Current;
        int rebuilt = 0;
        try
        {
            Observed.Clear(); Admit(current, legacy);
            foreign.Patch(child, prefix: new HarmonyMethod(typeof(NativeDefinitionConversionGuardTests).GetMethod(nameof(AfterGuard))!)
                { priority = int.MinValue, after = [OwnedHarmonyPatch.OwnerId] });
            afterGuard = target => { rebuilt++; foreign.CreateProcessor(target).Patch(); };
            parsing = name =>
            {
                if (name != "Parent") return;
                Invoke(!parentIsCurrent, "Child");
                Observed.Add(Scope.Current?.DefinitionName);
            };
            using (Scope.Enter(Context("Caller")))
            {
                Invoke(parentIsCurrent, "Parent");
                Assert.That(rebuilt, Is.EqualTo(1));
                Assert.That(Observed, Is.EqualTo(new string?[] { "Parent", null, null }));
                Assert.That(Scope.Current?.DefinitionName, Is.EqualTo("Caller"));
            }
        }
        finally { try { foreign.Unpatch(child, HarmonyPatchType.All, foreign.Id); } finally { Cleanup(current, legacy); } }
    }
    public static void AfterGuard(MethodBase __originalMethod) => afterGuard?.Invoke(__originalMethod);
    [TestCase(true)]
    [TestCase(false)]
    public void FreshCompatiblePairAdmitsChildWithoutRevivingAnOlderParent(bool parentIsCurrent)
    {
        var composer = new HarmonyLib.Harmony("tests.megamod.guard.fresh-pair");
        var current = Route(true); var legacy = Route(false);
        MethodInfo child = parentIsCurrent ? Legacy : Current;
        try
        {
            Observed.Clear(); Admit(current, legacy);
            parsing = name =>
            {
                if (name != "Parent") return;
                composer.CreateProcessor(child).Patch();
                Invoke(!parentIsCurrent, "FreshChild");
                Observed.Add(Scope.Current?.DefinitionName);
            };
            Invoke(parentIsCurrent, "Parent");
            Invoke(parentIsCurrent, "FreshParent");
            Assert.That(Observed, Is.EqualTo(new string?[] { "Parent", "FreshChild", null, "FreshParent" }));
            Assert.That(Scope.Current, Is.Null);
        }
        finally { Cleanup(current, legacy); }
    }
    [TestCase(true)]
    [TestCase(false)]
    public void GuardOnlyRemovalDuringParentCannotPublishANewChildScope(bool parentIsCurrent)
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.guard.nested-prefix-removal");
        var current = Route(true); var legacy = Route(false);
        MethodInfo child = parentIsCurrent ? Legacy : Current;
        try
        {
            Observed.Clear(); Admit(current, legacy);
            parsing = name =>
            {
                if (name != "Parent") return;
                foreign.Unpatch(child, HarmonyPatchType.Prefix, OwnedHarmonyPatch.OwnerId);
                Invoke(!parentIsCurrent, "Child");
                Observed.Add(Scope.Current?.DefinitionName);
            };
            using (Scope.Enter(Context("Caller")))
            {
                Invoke(parentIsCurrent, "Parent");
                Assert.That(Observed, Is.EqualTo(new string?[] { "Parent", null, null }));
                Assert.That(coverage!.Available, Is.False);
                Assert.That(Scope.Current?.DefinitionName, Is.EqualTo("Caller"));
            }
        }
        finally { Cleanup(current, legacy); }
    }
    [Test]
    public void GuardFactoryPreflightRefusesWithoutExecutingTheFactory()
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.guard.factory");
        var current = Route(true); var legacy = Route(false);
        try
        {
            factoryCalls = 0;
            foreign.Patch(Current, prefix: new HarmonyMethod(typeof(NativeDefinitionConversionGuardTests).GetMethod(nameof(PrefixFactory))!));
            Assert.That(factoryCalls, Is.GreaterThan(0));
            factoryCalls = 0;
            coverage = new NativeDefinitionConversionGuard(current, legacy, () => Scope.HasActiveFrame, Scope.InvalidateCurrent, () => true);
            Assert.That(coverage.Candidates[0].Validate(), Is.Not.Null);
            Assert.That(factoryCalls, Is.Zero);
        }
        finally { try { foreign.Unpatch(Current, HarmonyPatchType.All, foreign.Id); } finally { Cleanup(current, legacy); } }
    }
    public static MethodInfo PrefixFactory(MethodBase original) { factoryCalls++; return typeof(NativeDefinitionConversionGuardTests).GetMethod(nameof(ObservePrefix))!; }
    public static void ObservePrefix() { }
    [TestCase(true, false, false)]
    [TestCase(false, false, false)]
    [TestCase(true, true, false)]
    [TestCase(false, true, false)]
    [TestCase(true, true, true)]
    [TestCase(false, true, true)]
    public void SurvivingGuardCoversUnwrappedChildAndPreservesOuterCleanup(bool parentIsCurrent, bool removed, bool throws)
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.guard.unwrapped");
        var current = Route(true); var legacy = Route(false);
        MethodInfo child = parentIsCurrent ? Legacy : Current;
        var failure = new InvalidOperationException("native child failure");
        try
        {
            Observed.Clear(); Admit(current, legacy);
            parsing = name =>
            {
                if (name == "Child") { if (throws) throw failure; return; }
                if (name != "Parent") return;
                if (removed) foreign.Unpatch(child, HarmonyPatchType.Transpiler, OwnedHarmonyPatch.OwnerId);
                else foreign.Patch(child, transpiler: new HarmonyMethod(typeof(NativeDefinitionConversionGuardTests).GetMethod(nameof(ChangeResolve))!));
                Invoke(!parentIsCurrent, "Child");
                Observed.Add(Scope.Current?.DefinitionName);
            };
            using (Scope.Enter(Context("Caller")))
            {
                if (throws) Assert.That(Assert.Throws<InvalidOperationException>(() => Invoke(parentIsCurrent, "Parent")), Is.SameAs(failure));
                else Invoke(parentIsCurrent, "Parent");
                Assert.That(Observed, Is.EqualTo(throws ? new string?[] { "Parent", null } : new string?[] { "Parent", null, null }));
                Assert.That(Scope.Current?.DefinitionName, Is.EqualTo("Caller"));
            }
        }
        finally { try { foreign.Unpatch(child, HarmonyPatchType.All, foreign.Id); } finally { Cleanup(current, legacy); } }
    }
    [TestCase(true)]
    [TestCase(false)]
    public void InitiallyUnavailableChildRejectsAllContextCoverage(bool childIsCurrent)
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.guard.initial-body");
        var current = Route(true); var legacy = Route(false);
        MethodInfo child = childIsCurrent ? Current : Legacy;
        try
        {
            Observed.Clear();
            foreign.Patch(child, transpiler: new HarmonyMethod(typeof(NativeDefinitionConversionGuardTests).GetMethod(nameof(ChangeResolve))!));
            coverage = new NativeDefinitionConversionGuard(current, legacy, () => Scope.HasActiveFrame, Scope.InvalidateCurrent, () => true);
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(new PatchGroup("definition-family", AdmissionPhase.Early,
                new[] { current.Candidate, legacy.Candidate }.Concat(coverage.Candidates).ToArray()), AdmissionPhase.Early), Is.False);
            Invoke(!childIsCurrent, "OrdinaryParent"); Invoke(childIsCurrent, "OrdinaryChild");
            Assert.That(Observed, Is.EqualTo(new string?[] { null, null }));
            Assert.That(coverage.Available, Is.False);
        }
        finally { try { foreign.Unpatch(child, HarmonyPatchType.All, foreign.Id); } finally { Cleanup(current, legacy); } }
    }
    [TestCase(true)]
    [TestCase(false)]
    public void CompatibleNestedLoadersRestoreTheirParents(bool parentIsCurrent)
    {
        var current = Route(true); var legacy = Route(false);
        try
        {
            Observed.Clear(); Admit(current, legacy);
            parsing = name => { if (name == "Parent") { Invoke(!parentIsCurrent, "Child"); Observed.Add(Scope.Current?.DefinitionName); } };
            Invoke(parentIsCurrent, "Parent");
            Assert.That(Observed, Is.EqualTo(new[] { "Parent", "Child", "Parent" }));
            Assert.That(Scope.Current, Is.Null);
        }
        finally { Cleanup(current, legacy); }
    }
    public static IEnumerable<CodeInstruction> ChangeResolve(IEnumerable<CodeInstruction> body)
    {
        foreach (var instruction in body)
        {
            if (instruction.Calls(typeof(NativeDefinitionConversionGuardTests).GetMethod(nameof(Resolve))))
                instruction.operand = typeof(NativeDefinitionConversionGuardTests).GetMethod(nameof(ResolveAlias));
            yield return instruction;
        }
    }
    public static XmlNode ResolveAlias(XmlNode node) => node;
    [TestCase(true)]
    [TestCase(false)]
    public void InitialUntrustedPrefixOrderRejectsTheWholeContextGroup(bool childIsCurrent)
    {
        var foreign = new HarmonyLib.Harmony("tests.megamod.guard.initial-order");
        var current = Route(true); var legacy = Route(false);
        MethodInfo child = childIsCurrent ? Current : Legacy;
        try
        {
            foreign.Patch(child, prefix: new HarmonyMethod(typeof(NativeDefinitionConversionGuardTests).GetMethod(nameof(BeforeChild))!)
                { priority = int.MaxValue, before = [OwnedHarmonyPatch.OwnerId] });
            coverage = new NativeDefinitionConversionGuard(current, legacy, () => Scope.HasActiveFrame, Scope.InvalidateCurrent, () => true);
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(new PatchGroup("definition-family", AdmissionPhase.Early,
                new[] { current.Candidate, legacy.Candidate }.Concat(coverage.Candidates).ToArray()), AdmissionPhase.Early), Is.False);
            Assert.That(coverage.Available, Is.False);
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(Current)?.Owners ?? [], Does.Not.Contain(OwnedHarmonyPatch.OwnerId));
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(Legacy)?.Owners ?? [], Does.Not.Contain(OwnedHarmonyPatch.OwnerId));
        }
        finally { try { foreign.Unpatch(child, HarmonyPatchType.All, foreign.Id); } finally { Cleanup(current, legacy); } }
    }
    public static XmlNode Resolve(XmlNode node) => node;
    private static Result Parsed(XmlNode node)
    {
        Observed.Add(Scope.Current?.DefinitionName); parsing?.Invoke(node.Name); return new Result(node.Name);
    }
    public static class LegacyConverter
    {
        private static readonly Func<XmlNode, bool, object> Parse = (node, _) => Parsed(node);
        [MethodImpl(MethodImplOptions.NoInlining)] public static Result DefFromNode(XmlNode node, Asset? asset)
        { _ = Resolve(node); return (Result)Parse(node, true); }
    }
    public static class CurrentConverter
    {
        public delegate Result Parser(int first, int second, XmlNode node, Type type);
        private static readonly Parser Parse = (_, _, node, _) => Parsed(node);
        [MethodImpl(MethodImplOptions.NoInlining)] public static Result DefFromNodeNew(XmlNode node, Asset? asset)
        { _ = Resolve(node); Type type = typeof(Result); return Parse(0, 0, node, type); }
    }
}
