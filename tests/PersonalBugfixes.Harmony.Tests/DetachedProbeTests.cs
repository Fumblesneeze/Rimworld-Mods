using System.Runtime.CompilerServices;
using System.Reflection;
using System.Reflection.Emit;
using NUnit.Framework;
using PersonalBugfixes.Exploration;

namespace PersonalBugfixes.Harmony.Tests;

// Plain synthetic model. No mod activation, Verse Def databases or real World constructors.
public static class ProbeModel
{
    public enum UpdateKind { None, Fog, Full, Planet }
    public enum ProgramState { Entry, Loading, Playing }
    public sealed class Layer { }
    public sealed class Renderer { public void SetDirty<T>(Layer layer) { LiveCalls++; } }
    public sealed class Feature
    {
        public IEnumerable<int> Tiles { get { LiveCalls++; return Array.Empty<int>(); } }
        public bool IsVisible(int tile) { LiveCalls++; return true; }
    }
    public sealed class Features { public List<Feature> features = new(); }
    public sealed class World
    {
        public Features features = new();
        public Renderer renderer = new();
        public T GetComponent<T>() { LiveCalls++; return default!; }
    }
    public sealed class Component : IDisposable, IEnumerable<bool>
    {
        public List<bool> learnedFeatures = new();
        public void Dispose() { LiveCalls++; }
        public IEnumerator<bool> GetEnumerator() { LiveCalls++; return Enumerable.Empty<bool>().GetEnumerator(); }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() => GetEnumerator();
    }
    public static class Find { public static World World { get { LiveCalls++; return new(); } } }
    public static class Current { public static ProgramState ProgramState { get { LiveCalls++; return ProgramState.Playing; } } }
    public static int LiveCalls;
    public static class Vulnerable
    {
        public static UpdateKind _updateType = UpdateKind.Full;
        public static bool RevealAll;
        public static Layer VisibilityLayer = new();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void UpdateGraphics()
        {
            if (Find.World == null) return;
            if (_updateType >= UpdateKind.Fog) Find.World.renderer.SetDirty<Feature>(VisibilityLayer);
            if (_updateType >= UpdateKind.Full && Current.ProgramState == ProgramState.Playing && !RevealAll)
            {
                int i = 0;
                foreach (var feature in Find.World.features.features)
                {
                    var component = Find.World.GetComponent<Component>();
                    if (!component.learnedFeatures[i])
                    {
                        var tiles = feature.Tiles.ToList();
                        if ((float)tiles.FindAll(tile => feature.IsVisible(tile)).Count / tiles.Count > 0.25f)
                            component.learnedFeatures[i] = true;
                    }
                    i++;
                }
            }
            _updateType = UpdateKind.None;
        }
    }
    public static class Fixed
    {
        public static UpdateKind _updateType = UpdateKind.Full;
        public static bool RevealAll;
        public static Layer VisibilityLayer = new();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void UpdateGraphics()
        {
            if (Find.World == null) return;
            if (_updateType >= UpdateKind.Fog) Find.World.renderer.SetDirty<Feature>(VisibilityLayer);
            int i = 0;
            foreach (var feature in Find.World.features.features)
            {
                var component = Find.World.GetComponent<Component>();
                while (component.learnedFeatures.Count <= i) component.learnedFeatures.Add(false);
                if (!component.learnedFeatures[i]) { var tiles = feature.Tiles.ToList(); }
                i++;
            }
            _updateType = UpdateKind.None;
        }
    }
    public static class UnsafeDispose
    {
        public static UpdateKind _updateType = UpdateKind.Full;
        public static bool RevealAll;
        public static Layer VisibilityLayer = new();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void UpdateGraphics()
        {
            ((IDisposable)Find.World.GetComponent<Component>()).Dispose();
            int i = 0;
            foreach (var feature in Find.World.features.features)
            {
                if (!Find.World.GetComponent<Component>().learnedFeatures[i]) { var tiles = feature.Tiles.ToList(); }
                i++;
            }
            _updateType = UpdateKind.None;
        }
    }
    public static class Endless
    {
        public static UpdateKind _updateType = UpdateKind.Full;
        public static bool RevealAll;
        public static Layer VisibilityLayer = new();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void UpdateGraphics()
        {
            try { while (Find.World != null) { } }
            finally { _updateType = UpdateKind.None; }
        }
    }
    public static class UnsafeEnumerable
    {
        public static UpdateKind _updateType = UpdateKind.Full;
        public static bool RevealAll;
        public static Layer VisibilityLayer = new();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void UpdateGraphics()
        {
            var unsafeList = Find.World.GetComponent<Component>().ToList();
            _updateType = UpdateKind.None;
        }
    }
    public static class SwallowedRejection
    {
        public static UpdateKind _updateType = UpdateKind.Full;
        public static bool RevealAll;
        public static Layer VisibilityLayer = new();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static void UpdateGraphics()
        {
            try { var list = Find.World.GetComponent<Component>().ToList(); }
            catch (InvalidOperationException) { _updateType = UpdateKind.None; }
        }
    }
    public static ExplorationTarget Target(Type? visibility = null) => new(visibility ?? typeof(Vulnerable),
        typeof(Component), typeof(World), typeof(Feature), typeof(Find), typeof(Current));
}

[TestFixture, NonParallelizable]
public sealed class DetachedProbeTests
{
    [Test]
    public void Unknown_interface_dispatch_is_rejected_before_any_live_effect()
    {
        ProbeModel.LiveCalls = 0;
        Assert.That(DetachedProbe.Run(ProbeModel.Target(typeof(ProbeModel.UnsafeDispose))),
            Is.EqualTo(ProbeOutcome.Inconclusive), DetachedProbe.LastDetail);
        Assert.That(ProbeModel.LiveCalls, Is.Zero);
    }

    [Test]
    public void Unknown_enumerable_is_rejected_before_executing_its_enumerator()
    {
        ProbeModel.LiveCalls = 0;
        Assert.That(DetachedProbe.Run(ProbeModel.Target(typeof(ProbeModel.UnsafeEnumerable))),
            Is.EqualTo(ProbeOutcome.Inconclusive), DetachedProbe.LastDetail);
        Assert.That(ProbeModel.LiveCalls, Is.Zero);
    }

    [Test]
    public void Copied_code_cannot_swallow_a_safety_error_and_report_healthy()
    {
        Assert.That(DetachedProbe.Run(ProbeModel.Target(typeof(ProbeModel.SwallowedRejection))),
            Is.EqualTo(ProbeOutcome.Inconclusive), DetachedProbe.LastDetail);
    }

    [Test]
    public void Endless_fixture_control_flow_exhausts_budget_instead_of_hanging()
    {
        Assert.That(DetachedProbe.Run(ProbeModel.Target(typeof(ProbeModel.Endless))),
            Is.EqualTo(ProbeOutcome.Inconclusive), DetachedProbe.LastDetail);
        Assert.That(DetachedProbe.LastDetail, Does.Contain("budget exhausted"));
    }

    [Test]
    public void Branch_at_exception_boundary_is_still_budgeted()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("ProbeBoundary"), AssemblyBuilderAccess.Run);
        var type = assembly.DefineDynamicModule("fixture").DefineType("BoundaryVisibility", TypeAttributes.Public);
        var update = type.DefineField("_updateType", typeof(ProbeModel.UpdateKind), FieldAttributes.Public | FieldAttributes.Static);
        type.DefineField("RevealAll", typeof(bool), FieldAttributes.Public | FieldAttributes.Static);
        type.DefineField("VisibilityLayer", typeof(ProbeModel.Layer), FieldAttributes.Public | FieldAttributes.Static);
        var method = type.DefineMethod("UpdateGraphics", MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes);
        var il = method.GetILGenerator();
        il.BeginExceptionBlock();
        var loop = il.DefineLabel();
        il.MarkLabel(loop);
        il.Emit(OpCodes.Br, loop); // This sole loop branch also owns BeginExceptionBlock metadata.
        il.BeginFinallyBlock();
        il.Emit(OpCodes.Ldc_I4_0); il.Emit(OpCodes.Stsfld, update);
        il.EndExceptionBlock(); il.Emit(OpCodes.Ret);
        var target = ProbeModel.Target(type.CreateType());
        Assert.That(DetachedProbe.Run(target), Is.EqualTo(ProbeOutcome.Inconclusive), DetachedProbe.LastDetail);
        Assert.That(DetachedProbe.LastDetail, Does.Contain("budget exhausted"));
    }

    private sealed class RejectInstalled : IPersonalFix
    {
        private readonly ExplorationFix fix;
        public RejectInstalled(ExplorationFix fix) { this.fix = fix; }
        public string Id => fix.Id;
        public bool TargetPresent => fix.TargetPresent;
        public string TargetDescription => fix.TargetDescription;
        public string? Inspect() => fix.Inspect();
        public ProbeOutcome Probe() => fix.Probe();
        public void Apply() => fix.Apply();
        public bool VerifyInstalled() => false;
        public void Remove() => fix.Remove();
    }

    [Test]
    public void Real_harmony_patch_is_removed_when_a_postcondition_fails()
    {
        var fix = new ExplorationFix(true, () => ProbeModel.Target());
        try
        {
            var result = new FixLifecycle().Evaluate(new RejectInstalled(fix));
            Assert.That(result.State, Is.EqualTo(FixState.Failed), result.ToString());
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(ProbeModel.Target().Update)?.Owners, Is.Null.Or.Empty);
            Assert.That(DetachedProbe.Run(ProbeModel.Target()), Is.EqualTo(ProbeOutcome.BugPresent));
        }
        finally { fix.Remove(); }
    }

    [Test]
    public void Actual_target_copy_reproduces_bounds_error_without_touching_live_accessors()
    {
        ProbeModel.LiveCalls = 0;
        var result = DetachedProbe.Run(ProbeModel.Target());
        Assert.That(result, Is.EqualTo(ProbeOutcome.BugPresent), DetachedProbe.LastDetail);
        Assert.That(ProbeModel.LiveCalls, Is.Zero);
        Assert.That(ProbeModel.Vulnerable._updateType, Is.EqualTo(ProbeModel.UpdateKind.Full));
    }

    [Test]
    public void Upstream_fix_passes_the_actual_target_probe_and_stays_unpatched()
    {
        var fix = new ExplorationFix(true, () => ProbeModel.Target(typeof(ProbeModel.Fixed)));
        try
        {
            var result = new FixLifecycle().Evaluate(fix);
            Assert.That(result.State, Is.EqualTo(FixState.NotRequired), result.ToString());
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(ProbeModel.Target(typeof(ProbeModel.Fixed)).Update)?.Owners, Is.Null.Or.Empty);
        }
        finally { fix.Remove(); }
    }

    [Test]
    public void Installed_patch_is_present_in_probe_copy_and_fixes_the_reproduced_bug()
    {
        var fix = new ExplorationFix(true, () => ProbeModel.Target());
        ProbeModel.LiveCalls = 0;
        try
        {
            var result = new FixLifecycle().Evaluate(fix);
            Assert.That(result.State, Is.EqualTo(FixState.Applied), result.ToString());
            Assert.That(fix.VerifyInstalled(), Is.True);
            Assert.That(ProbeModel.LiveCalls, Is.Zero);
        }
        finally { fix.Remove(); }
        Assert.That(HarmonyLib.Harmony.GetPatchInfo(ProbeModel.Target().Update)?.Owners, Is.Null.Or.Empty);
    }

    [Test]
    public void Foreign_patch_is_left_alone_and_personal_fix_is_not_applied()
    {
        var target = ProbeModel.Target();
        var foreign = new HarmonyLib.Harmony("personal-tests.foreign");
        var fix = new ExplorationFix(true, () => target);
        try
        {
            foreign.Patch(target.Update, prefix: new HarmonyLib.HarmonyMethod(typeof(DetachedProbeTests).GetMethod(nameof(ForeignPrefix))));
            var result = new FixLifecycle().Evaluate(fix);
            Assert.That(result.State, Is.EqualTo(FixState.Incompatible));
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(target.Update)!.Owners, Is.EqualTo(new[] { "personal-tests.foreign" }));
        }
        finally { fix.Remove(); foreign.Unpatch(target.Update, HarmonyLib.HarmonyPatchType.All, foreign.Id); }
    }

    public static void ForeignPrefix() { }
}
