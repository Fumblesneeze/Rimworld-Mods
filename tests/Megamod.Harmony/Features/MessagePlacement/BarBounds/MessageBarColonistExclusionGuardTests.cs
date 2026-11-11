using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.MessagePlacement;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.MessagePlacement.BarBounds;

[TestFixture, NonParallelizable]
public sealed class MessageBarColonistExclusionGuardTests
{
    private static MethodInfo Method(Type type, string name) => type.GetMethod(name)!;
    private static MethodInfo Getter(Type type, string name) => type.GetProperty(name)!.GetMethod!;
    private static readonly MethodInfo Target = Method(typeof(Drawer), nameof(Drawer.Draw));
    private static readonly MethodInfo Edge = Method(typeof(Moods), nameof(Moods.EdgeExpansion));
    private static readonly MessageBarColonistExclusionGuard Guard = new(new(
        Method(typeof(Moods), nameof(Moods.CurrentMood)), Edge, Method(typeof(Renderer), nameof(Renderer.ExpandedBy)),
        Method(typeof(Renderer), nameof(Renderer.DrawAtlas)), typeof(Drawer).GetField(nameof(Drawer.MoodAtlas))!,
        Getter(typeof(Pawn), nameof(Pawn.Dead)), Getter(typeof(Pawn), nameof(Pawn.Corpse)), Getter(typeof(Context), nameof(Context.Selector)),
        Getter(typeof(Selector), nameof(Selector.SelectedObjects)), Method(typeof(List<object>), nameof(List<object>.Contains)),
        Getter(typeof(Context), nameof(Context.WorldSelected)), Method(typeof(Drawer), nameof(Drawer.DrawSelection)),
        Method(typeof(Context), nameof(Context.GetCaravan)), Method(typeof(Drawer), nameof(Drawer.DrawCaravanSelection)),
        Method(typeof(Renderer), nameof(Renderer.ContractedBy)), Getter(typeof(Drawer), nameof(Drawer.ColonistBar)), Getter(typeof(Bar), nameof(Bar.Scale))));

    private static MessageBarLabelAdmission Admission() => new(
        new MessageBarLabelGuard(new(new Dictionary<string, MemberInfo>()), Guard), (Target, MessageBarLabelStage.AdornmentExclusions));
    private static void Admit(MessageBarLabelAdmission admission)
    {
        var warnings = new List<string>();
        Assert.That(new AdmissionCoordinator(warnings.Add).Apply(admission.Group, AdmissionPhase.Early), Is.True, string.Join("; ", warnings));
        Assert.That(admission.Available, Is.True);
    }
    private static void Cleanup(MessageBarLabelAdmission admission)
    { foreach (var candidate in admission.Group.Patches.Reverse()) candidate.Rollback(); }

    [Test]
    public void InnerExpansionRewriteRevokesExclusionAuthorityAndPreservesNativeDraw()
    {
        Reset();
        var drawer = new Drawer(); var pawn = new Pawn();
        var admission = Admission();
        var foreign = new HarmonyLib.Harmony("tests.message-bar.exclusion-inner");
        try
        {
            Admit(admission);
            drawer.Draw(new(0, 0, 48, 48), pawn, null, false, false);
            Assert.That(Renderer.Atlases, Is.Zero);
            var old = admission.CaptureAuthority();
            foreign.CreateProcessor(Target).AddInnerPostfix(new HarmonyMethod(typeof(MessageBarColonistExclusionGuardTests), nameof(Expand))
                { innerMethod = new InnerMethod(Edge) }).Patch();
            Assert.That(admission.Refresh(), Is.False);
            drawer.Draw(new(0, 0, 48, 48), pawn, null, false, false);
            Assert.Multiple(() =>
            {
                Assert.That(admission.Available, Is.False);
                Assert.That(old(), Is.False);
                Assert.That(Renderer.Atlases, Is.EqualTo(1));
                Assert.That(drawer.Completed, Is.EqualTo(2));
            });
        }
        finally
        {
            try { foreign.Unpatch(Target, HarmonyPatchType.InnerPostfix, foreign.Id); }
            finally { Cleanup(admission); }
        }
    }
    public static void Expand(ref float __result) => __result = 5;
    [Test]
    public void IncomingSelectionBypassIsDeclinedWithoutSuppressingItsNativeEffect()
    {
        Reset();
        var drawer = new Drawer(); var pawn = new Pawn();
        var admission = Admission();
        var foreign = new HarmonyLib.Harmony("tests.message-bar.exclusion-bypass");
        try
        {
            foreign.Patch(Target, transpiler: new HarmonyMethod(typeof(MessageBarColonistExclusionGuardTests), nameof(BypassSelection)));
            var warnings = new List<string>();
            bool admitted = new AdmissionCoordinator(warnings.Add).Apply(admission.Group, AdmissionPhase.Early);
            drawer.Draw(new(0, 0, 48, 48), pawn, null, false, false);
            Assert.Multiple(() =>
            {
                Assert.That(admitted, Is.False);
                Assert.That(drawer.Selections, Is.EqualTo(1));
                Assert.That(drawer.Completed, Is.EqualTo(1));
            });
        }
        finally
        {
            try { Cleanup(admission); }
            finally { foreign.Unpatch(Target, HarmonyPatchType.Transpiler, foreign.Id); }
        }
    }
    public static IEnumerable<CodeInstruction> BypassSelection(IEnumerable<CodeInstruction> source)
    {
        var body = source.ToList();
        int draw = body.FindIndex(instruction => instruction.Calls(Method(typeof(Drawer), nameof(Drawer.DrawSelection))));
        body[draw - 6].opcode = OpCodes.Pop; body[draw - 6].operand = null;
        return body;
    }

    [TestCase(false, false, false, Threshold.None, 0, 0, 0)]
    [TestCase(false, true, false, Threshold.None, 0, 1, 0)]
    [TestCase(true, true, false, Threshold.None, 0, 1, 0)]
    [TestCase(false, false, true, Threshold.None, 0, 0, 1)]
    [TestCase(false, false, false, Threshold.Major, 1, 0, 0)]
    public void SupportedNativeExclusionPathsKeepTheirActualEffects(bool dead, bool selected, bool world, Threshold mood,
        int atlases, int selections, int caravans)
    {
        Reset();
        var drawer = new Drawer(); var pawn = new Pawn { IsDead = dead, Mood = mood };
        Context.World = world;
        if (selected) Context.Selection.SelectedObjects.Add(dead ? pawn.Corpse : pawn);
        var admission = Admission();
        try
        {
            Admit(admission); drawer.Draw(new(0, 0, 48, 48), pawn, null, false, false);
            Assert.Multiple(() =>
            {
                Assert.That(admission.Available, Is.True);
                Assert.That(Renderer.Atlases, Is.EqualTo(atlases));
                Assert.That(drawer.Selections, Is.EqualTo(selections));
                Assert.That(drawer.CaravanSelections, Is.EqualTo(caravans));
                Assert.That(drawer.Completed, Is.EqualTo(1));
            });
        }
        finally { Cleanup(admission); }
    }

    [TestCase("mood-store", 1, 0, 0)]
    [TestCase("expansion-store", 1, 0, 0)]
    [TestCase("expansion-overwrite", 1, 0, 0)]
    [TestCase("mood-gate", 1, 0, 0)]
    [TestCase("world-gate", 0, 0, 1)]
    [TestCase("corpse-input", 0, 1, 0)]
    [TestCase("pawn-address", 1, 0, 0)]
    public void AlteredExclusionPathIsRejectedWithoutChangingItsNativeEffects(string change, int atlases, int selections, int caravans)
    {
        Reset();
        var drawer = new Drawer(); var pawn = new Pawn();
        if (change == "corpse-input") { pawn.IsDead = true; Context.Selection.SelectedObjects.Add(pawn); }
        var admission = Admission();
        var foreign = new HarmonyLib.Harmony("tests.message-bar.exclusion-incoming");
        incomingChange = change;
        try
        {
            foreign.Patch(Target, transpiler: new HarmonyMethod(typeof(MessageBarColonistExclusionGuardTests), nameof(AlterExclusion)));
            var warnings = new List<string>();
            bool admitted = new AdmissionCoordinator(warnings.Add).Apply(admission.Group, AdmissionPhase.Early);
            drawer.Draw(new(0, 0, 48, 48), pawn, null, false, false);
            Assert.Multiple(() =>
            {
                Assert.That(admitted, Is.False, change);
                Assert.That(warnings, Is.Not.Empty);
                Assert.That(Renderer.Atlases, Is.EqualTo(atlases));
                Assert.That(drawer.Selections, Is.EqualTo(selections));
                Assert.That(drawer.CaravanSelections, Is.EqualTo(caravans));
                Assert.That(drawer.Completed, Is.EqualTo(1));
            });
        }
        finally
        {
            try { Cleanup(admission); }
            finally { foreign.Unpatch(Target, HarmonyPatchType.Transpiler, foreign.Id); incomingChange = null; }
        }
    }
    private static string? incomingChange;
    public static void ReplacePawn(ref Pawn pawn) => pawn = new() { Mood = Threshold.Major };
    public static IEnumerable<CodeInstruction> AlterExclusion(IEnumerable<CodeInstruction> source, ILGenerator generator)
    {
        var body = source.ToList();
        int mood = body.FindIndex(instruction => instruction.Calls(Method(typeof(Moods), nameof(Moods.CurrentMood))));
        int edge = body.FindIndex(instruction => instruction.Calls(Edge));
        switch (incomingChange)
        {
            case "mood-store":
            case "expansion-store":
                int store = incomingChange == "mood-store" ? mood + 1 : edge + 1;
                var destination = generator.DefineLabel(); body[store].labels.Add(destination);
                body.InsertRange(0, new[] { incomingChange == "mood-store" ? new CodeInstruction(OpCodes.Ldc_I4_2) : new CodeInstruction(OpCodes.Ldc_R4, 5f),
                    new CodeInstruction(OpCodes.Br, destination) }); break;
            case "expansion-overwrite":
                body.InsertRange(edge + 2, new[] { new CodeInstruction(OpCodes.Ldc_R4, 5f), new CodeInstruction(body[edge + 1]) }); break;
            case "mood-gate":
                body[edge + 4].opcode = OpCodes.Pop; body[edge + 4].operand = null; body.Insert(edge + 5, new(OpCodes.Pop)); break;
            case "world-gate":
                int world = body.FindLastIndex(instruction => instruction.Calls(Getter(typeof(Context), nameof(Context.WorldSelected))));
                body[world + 1].opcode = OpCodes.Pop; body[world + 1].operand = null; break;
            case "corpse-input":
                int corpse = body.FindIndex(instruction => instruction.Calls(Getter(typeof(Pawn), nameof(Pawn.Corpse))));
                body[corpse].opcode = OpCodes.Pop; body[corpse].operand = null; body.Insert(corpse + 1, new(OpCodes.Ldarg_2)); break;
            case "pawn-address":
                body.InsertRange(0, new[] { new CodeInstruction(OpCodes.Ldarga, 2), new CodeInstruction(OpCodes.Call,
                    Method(typeof(MessageBarColonistExclusionGuardTests), nameof(ReplacePawn))) }); break;
        }
        return body;
    }

    [Test]
    public void UnrelatedEffectAndNopsRemainEligible()
    {
        Reset();
        var drawer = new Drawer(); var admission = Admission();
        var foreign = new HarmonyLib.Harmony("tests.message-bar.exclusion-benign");
        unrelatedEffects = 0;
        try
        {
            foreign.Patch(Target, transpiler: new HarmonyMethod(typeof(MessageBarColonistExclusionGuardTests), nameof(Benign)));
            Admit(admission); drawer.Draw(new(0, 0, 48, 48), new Pawn(), null, false, false);
            Assert.Multiple(() =>
            {
                Assert.That(admission.Available, Is.True);
                Assert.That(unrelatedEffects, Is.EqualTo(1));
                Assert.That(drawer.Completed, Is.EqualTo(1));
            });
        }
        finally
        {
            try { Cleanup(admission); }
            finally { foreign.Unpatch(Target, HarmonyPatchType.Transpiler, foreign.Id); }
        }
    }
    private static int unrelatedEffects;
    public static void Unrelated() => unrelatedEffects++;
    public static IEnumerable<CodeInstruction> Benign(IEnumerable<CodeInstruction> body) =>
        new[] { new CodeInstruction(OpCodes.Call, Method(typeof(MessageBarColonistExclusionGuardTests), nameof(Unrelated))) }
            .Concat(body.SelectMany(instruction => new[] { instruction, new CodeInstruction(OpCodes.Nop) }));
    private static void Reset() { Context.World = false; Context.Selection.SelectedObjects.Clear(); Renderer.Atlases = 0; }

    public readonly record struct Rectangle(float X, float Y, float Width, float Height);
    public enum Threshold { None, Minor, Major, Extreme }
    public sealed class Pawn
    {
        public bool IsDead; public Threshold Mood; public readonly object DeadBody = new();
        public bool Dead { [MethodImpl(MethodImplOptions.NoInlining)] get => IsDead; }
        public object Corpse { [MethodImpl(MethodImplOptions.NoInlining)] get => DeadBody; }
    }
    public static class Moods
    {
        [MethodImpl(MethodImplOptions.NoInlining)] public static Threshold CurrentMood(Pawn pawn) => pawn.Mood;
        [MethodImpl(MethodImplOptions.NoInlining)] public static float EdgeExpansion(Threshold threshold) => threshold < Threshold.Major ? 0 : 5;
    }
    public sealed class Selector
    {
        private readonly List<object> selected = new();
        public List<object> SelectedObjects { [MethodImpl(MethodImplOptions.NoInlining)] get => selected; }
    }
    public sealed class Caravan;
    public static class Context
    {
        public static bool World;
        public static readonly Selector Selection = new();
        public static readonly Caravan Caravan = new();
        public static Selector Selector { [MethodImpl(MethodImplOptions.NoInlining)] get => Selection; }
        public static bool WorldSelected { [MethodImpl(MethodImplOptions.NoInlining)] get => World; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool Member(Pawn pawn) => true;
        [MethodImpl(MethodImplOptions.NoInlining)] public static Caravan GetCaravan(Pawn pawn) => Caravan;
        [MethodImpl(MethodImplOptions.NoInlining)] public static bool CaravanSelected(Caravan caravan) => true;
    }
    public static class Renderer
    {
        public static int Atlases;
        [MethodImpl(MethodImplOptions.NoInlining)] public static Rectangle ExpandedBy(Rectangle rect, float expansion) => new(rect.X - expansion, rect.Y - expansion, rect.Width + 2 * expansion, rect.Height + 2 * expansion);
        [MethodImpl(MethodImplOptions.NoInlining)] public static Rectangle ContractedBy(Rectangle rect, float contraction) => rect;
        [MethodImpl(MethodImplOptions.NoInlining)] public static void DrawAtlas(Rectangle rectangle, object texture) => Atlases++;
        [MethodImpl(MethodImplOptions.NoInlining)] public static void Color(int value) { }
    }
    public sealed class Bar { public float Scale { [MethodImpl(MethodImplOptions.NoInlining)] get => 1; } }
    public sealed class Drawer
    {
        public static readonly object MoodAtlas = new();
        private readonly Bar bar = new();
        public int Selections, CaravanSelections, Completed;
        public bool VisibleMood = true;
        public Bar ColonistBar { [MethodImpl(MethodImplOptions.NoInlining)] get => bar; }
        [MethodImpl(MethodImplOptions.NoInlining)] public void DrawSelection(Pawn pawn, Rectangle rectangle) => Selections++;
        [MethodImpl(MethodImplOptions.NoInlining)] public void DrawCaravanSelection(Caravan caravan, Rectangle rectangle) => CaravanSelections++;
        [MethodImpl(MethodImplOptions.NoInlining)] public void Draw(Rectangle rectangle, Pawn pawn, object? map, bool highlight, bool reordering)
        {
            Threshold threshold = Moods.CurrentMood(pawn);
            if (VisibleMood)
            {
                float expansion = Moods.EdgeExpansion(threshold);
                if (expansion > 0)
                {
                    Renderer.Color(1);
                    Renderer.DrawAtlas(Renderer.ExpandedBy(rectangle, expansion), MoodAtlas);
                    Renderer.Color(0);
                }
            }
            var selectedRectangle = Renderer.ContractedBy(rectangle, -2 * ColonistBar.Scale);
            if ((pawn.Dead ? Context.Selector.SelectedObjects.Contains(pawn.Corpse) : Context.Selector.SelectedObjects.Contains(pawn)) && !Context.WorldSelected)
                DrawSelection(pawn, selectedRectangle);
            else if (Context.WorldSelected && Context.Member(pawn) && Context.CaravanSelected(Context.GetCaravan(pawn)))
                DrawCaravanSelection(Context.GetCaravan(pawn), selectedRectangle);
            Completed++;
        }
    }
}
