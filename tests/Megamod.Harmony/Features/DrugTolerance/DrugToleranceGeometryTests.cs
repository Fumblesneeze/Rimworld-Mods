using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.DrugTolerance;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.DrugTolerance;

[TestFixture, NonParallelizable]
public sealed class DrugToleranceGeometryTests
{
    private static readonly MethodInfo Initial = typeof(Manager).GetProperty(nameof(Manager.InitialSize))!.GetMethod!;
    private static Dictionary<string, MemberInfo> Members() => new()
    {
        ["screenWidth"] = typeof(Screen).GetProperty(nameof(Screen.Width))!.GetMethod!,
        ["minimum"] = typeof(Math).GetMethod(nameof(Math.Min), new[] { typeof(int), typeof(int) })!,
        ["vector"] = typeof(Vector).GetConstructor(new[] { typeof(float), typeof(float) })!,
        ["vectorX"] = typeof(Vector).GetField(nameof(Vector.X))!,
        ["vectorY"] = typeof(Vector).GetField(nameof(Vector.Y))!,
        ["windowRect"] = typeof(Window).GetField(nameof(Window.WindowRect))!,
        ["margin"] = typeof(Window).GetProperty(nameof(Window.Margin))!.GetMethod!,
        ["atZero"] = typeof(Rectangle).GetMethod(nameof(Rectangle.AtZero))!,
        ["contract"] = typeof(Rectangle).GetMethod(nameof(Rectangle.Contract))!,
        ["contents"] = typeof(Window).GetMethod(nameof(Window.Contents))!,
        ["initial"] = typeof(Window).GetProperty(nameof(Window.Size))!.GetMethod!,
        ["rounded"] = typeof(Rectangle).GetMethod(nameof(Rectangle.Rounded))!,
        ["rightContents"] = typeof(Editor).GetMethod(nameof(Editor.Forward))!,
        ["config"] = typeof(Editor).GetMethod(nameof(Editor.Config))!,
        ["header"] = typeof(Editor).GetMethod(nameof(Editor.Header))!,
        ["row"] = typeof(Editor).GetMethod(nameof(Editor.Row))!,
        ["adjust"] = typeof(Editor).GetMethod(nameof(Editor.Adjust))!,
        ["width"] = typeof(Rectangle).GetProperty(nameof(Rectangle.Width))!.GetMethod!,
        ["setWidth"] = typeof(Rectangle).GetProperty(nameof(Rectangle.Width))!.SetMethod!,
        ["x"] = typeof(Rectangle).GetProperty(nameof(Rectangle.X))!.GetMethod!,
        ["setX"] = typeof(Rectangle).GetProperty(nameof(Rectangle.X))!.SetMethod!,
        ["xMax"] = typeof(Rectangle).GetProperty(nameof(Rectangle.XMax))!.GetMethod!,
        ["setXMax"] = typeof(Rectangle).GetProperty(nameof(Rectangle.XMax))!.SetMethod!,
        ["y"] = typeof(Rectangle).GetProperty(nameof(Rectangle.Y))!.GetMethod!,
        ["setY"] = typeof(Rectangle).GetProperty(nameof(Rectangle.Y))!.SetMethod!,
        ["height"] = typeof(Rectangle).GetProperty(nameof(Rectangle.Height))!.GetMethod!,
        ["setHeight"] = typeof(Rectangle).GetProperty(nameof(Rectangle.Height))!.SetMethod!,
        ["rectangle"] = typeof(Rectangle).GetConstructor(new[] { typeof(float), typeof(float), typeof(float), typeof(float) })!
    };

    [Test]
    public void ChangedEffectiveInitialWidthCannotAuthorizeTheOldProspectiveAllocation()
    {
        var admission = new DrugToleranceGeometryAdmission("INITIAL", Initial,
            new DrugToleranceGeometryGuard(Members(), DrugToleranceGeometryStage.InitialSize));
        var foreign = new HarmonyLib.Harmony("tests.drug-tolerance.geometry.initial");
        try
        {
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("geometry", AdmissionPhase.Early,
                new[] { admission.Candidate }), AdmissionPhase.Early), Is.True);
            Assert.That(admission.Available, Is.True, admission.Failure);
            Assert.That(new Manager().InitialSize.X, Is.EqualTo(1300));
            foreign.Patch(Initial, transpiler: new HarmonyMethod(typeof(DrugToleranceGeometryTests), nameof(NarrowInitial)));
            Assert.That(new Manager().InitialSize.X, Is.EqualTo(500));
            Assert.That(admission.Available, Is.False, "The actual changed getter cannot retain the former geometry proof.");
        }
        finally
        {
            try { foreign.Unpatch(Initial, HarmonyPatchType.All, foreign.Id); }
            finally { admission.Candidate.Rollback(); }
        }
    }
    public static IEnumerable<CodeInstruction> NarrowInitial(IEnumerable<CodeInstruction> body) => body.Select(i =>
        i.LoadsConstant(1300) ? new CodeInstruction(OpCodes.Ldc_I4, 500) : i);

    [Test]
    public void ActualWindowContractionProvesTheContentsWidthAndRejectsAChangedMarginUse()
    {
        var target = typeof(Window).GetMethod(nameof(Window.Inner))!;
        var guard = new DrugToleranceGeometryGuard(Members(), DrugToleranceGeometryStage.InnerWindow);
        var body = PatchProcessor.GetOriginalInstructions(target);
        Assert.That(guard.Describe(body).Failure, Is.Null);
        int contraction = body.FindIndex(i => i.Calls((MethodInfo)Members()["contract"]));
        body.InsertRange(contraction, new[] { new CodeInstruction(OpCodes.Pop), new CodeInstruction(OpCodes.Ldc_R4, 100f) });
        Assert.That(guard.Describe(body).Failure, Is.Not.Null);
    }
    [Test]
    public void OuterInstanceMutationCannotRetainGeometryAdmission()
    {
        var target = typeof(Window).GetMethod(nameof(Window.Inner))!;
        var admission = new DrugToleranceGeometryAdmission("WINDOW", target, new(Members(), DrugToleranceGeometryStage.InnerWindow));
        var foreign = new HarmonyLib.Harmony("tests.drug-tolerance.geometry.instance");
        try
        {
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("geometry", AdmissionPhase.Early, new[] { admission.Candidate }), AdmissionPhase.Early), Is.True);
            foreign.Patch(target, prefix: new HarmonyMethod(typeof(DrugToleranceGeometryTests), nameof(NarrowWindow)));
            var window = new Window(); window.Inner();
            Assert.That(window.LastWidth, Is.EqualTo(464));
            Assert.That(admission.Available, Is.False);
        }
        finally { try { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); } finally { admission.Candidate.Rollback(); } }
    }
    public static void NarrowWindow(Window __instance) => __instance.WindowRect.Width = 500;
    [Test]
    public void PublicationComposesWithObservationAndRecoversAfterTheConflictingWriterIsRemoved()
    {
        var admission = new DrugToleranceGeometryAdmission("INITIAL", Initial, new(Members(), DrugToleranceGeometryStage.InitialSize));
        var benign = new HarmonyLib.Harmony("tests.drug-tolerance.geometry.benign");
        var conflict = new HarmonyLib.Harmony("tests.drug-tolerance.geometry.conflict");
        try
        {
            benign.Patch(Initial, prefix: new HarmonyMethod(typeof(DrugToleranceGeometryTests), nameof(Observe)), transpiler: new HarmonyMethod(typeof(DrugToleranceGeometryTests), nameof(AddObservation)));
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("geometry", AdmissionPhase.Early, new[] { admission.Candidate }), AdmissionPhase.Early), Is.True);
            Assert.That(admission.Available, Is.True, admission.Failure);
            conflict.Patch(Initial, transpiler: new HarmonyMethod(typeof(DrugToleranceGeometryTests), nameof(NarrowInitial)));
            Assert.That(admission.Available, Is.False);
            conflict.Unpatch(Initial, HarmonyPatchType.All, conflict.Id);
            Assert.That(admission.Available, Is.True, admission.Failure);
            benign.Unpatch(Initial, HarmonyPatchType.Transpiler, OwnedHarmonyPatch.OwnerId);
            Assert.That(admission.Available, Is.False, "Removing the owned observer revokes its last receipt.");
        }
        finally
        { try { benign.Unpatch(Initial, HarmonyPatchType.All, benign.Id); conflict.Unpatch(Initial, HarmonyPatchType.All, conflict.Id); } finally { admission.Candidate.Rollback(); } }
    }
    public static void Observe() { }
    public static IEnumerable<CodeInstruction> AddObservation(IEnumerable<CodeInstruction> body) => new[] { CodeInstruction.Call(typeof(DrugToleranceGeometryTests), nameof(Observe)) }.Concat(body);
    [Test]
    public void InitialWidthMustActuallyReachTheStoredWindowRectangle()
    {
        var target = typeof(Window).GetMethod(nameof(Window.SetWindow))!;
        var body = PatchProcessor.GetOriginalInstructions(target);
        var guard = new DrugToleranceGeometryGuard(Members(), DrugToleranceGeometryStage.SetWindow);
        Assert.That(guard.Describe(body).Failure, Is.Null);
        int field = body.FindLastIndex(i => Equals(i.operand, Members()["vectorX"]));
        body.InsertRange(field + 1, new[] { new CodeInstruction(OpCodes.Pop), new CodeInstruction(OpCodes.Ldc_R4, 500f) });
        Assert.That(guard.Describe(body).Failure, Is.Not.Null);
    }
    [TestCase(true)]
    [TestCase(false)]
    public void FinalInnerPublicationDistinguishesObservationFromSkippingTheMargin(bool observation)
    {
        var target = typeof(Window).GetMethod(nameof(Window.Inner))!;
        var admission = new DrugToleranceGeometryAdmission("WINDOW", target, new(Members(), DrugToleranceGeometryStage.InnerWindow));
        var foreign = new HarmonyLib.Harmony("tests.drug-tolerance.geometry.inner");
        try
        {
            foreign.CreateProcessor(target).AddInnerPrefix(new HarmonyMethod(typeof(DrugToleranceGeometryTests), observation ? nameof(Observe) : nameof(Skip))
                { innerMethod = new InnerMethod((MethodInfo)Members()["margin"]) }).Patch();
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("geometry", AdmissionPhase.Early, new[] { admission.Candidate }), AdmissionPhase.Early), Is.EqualTo(observation));
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(target)!.Owners, Does.Contain(foreign.Id));
        }
        finally { try { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); } finally { admission.Candidate.Rollback(); } }
    }
    public static bool Skip() => false;
    [Test]
    public void ReviewRegressionVectorFieldWriteChangesActualWindowWidthAndRevokesAdmission()
    {
        var target = typeof(Window).GetMethod(nameof(Window.SetWindow))!;
        var admission = new DrugToleranceGeometryAdmission("SET", target, new(Members(), DrugToleranceGeometryStage.SetWindow));
        var foreign = new HarmonyLib.Harmony("tests.drug-tolerance.geometry.vector-write");
        try
        {
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("geometry", AdmissionPhase.Early, new[] { admission.Candidate }), AdmissionPhase.Early), Is.True);
            foreign.Patch(target, transpiler: new HarmonyMethod(typeof(DrugToleranceGeometryTests), nameof(WriteInitialVector)));
            var window = new Window(); window.SetWindow();
            Assert.That(window.WindowRect.Width, Is.EqualTo(500));
            Assert.That(admission.Available, Is.False, "The consumed vector field was changed after its admitted getter.");
        }
        finally { try { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); } finally { admission.Candidate.Rollback(); } }
    }
    public static IEnumerable<CodeInstruction> WriteInitialVector(IEnumerable<CodeInstruction> source)
    {
        bool changed = false;
        foreach (var instruction in source)
        {
            yield return instruction;
            if (!changed && instruction.opcode == OpCodes.Stloc_0)
            {
                changed = true;
                yield return new(OpCodes.Ldloca_S, (byte)0); yield return new(OpCodes.Ldc_R4, 500f);
                yield return new(OpCodes.Stfld, Members()["vectorX"]);
            }
        }
    }
    [TestCase(false, 0f)]
    [TestCase(true, 18f)]
    public void ReviewRegressionMarginArithmeticMatchesTheActualSingleResult(bool exact, float expected)
    {
        var target = (MethodInfo)Members()["margin"];
        var admission = new DrugToleranceGeometryAdmission("MARGIN", target, new(Members(), DrugToleranceGeometryStage.Margin));
        var foreign = new HarmonyLib.Harmony("tests.drug-tolerance.geometry.precision");
        try
        {
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("geometry", AdmissionPhase.Early, new[] { admission.Candidate }), AdmissionPhase.Early), Is.True);
            foreign.Patch(target, transpiler: new HarmonyMethod(typeof(DrugToleranceGeometryTests), exact ? nameof(ExactMarginArithmetic) : nameof(RoundedMarginArithmetic)));
            Assert.That(new Window().Margin, Is.EqualTo(expected));
            Assert.That(admission.Available, Is.EqualTo(exact), admission.Failure);
        }
        finally { try { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); } finally { admission.Candidate.Rollback(); } }
    }
    public static IEnumerable<CodeInstruction> ExactMarginArithmetic(IEnumerable<CodeInstruction> _) => new[]
    { new CodeInstruction(OpCodes.Ldc_R4, 9f), new(OpCodes.Ldc_R4, 9f), new(OpCodes.Add), new(OpCodes.Conv_R4), new(OpCodes.Ret) };
    public static IEnumerable<CodeInstruction> RoundedMarginArithmetic(IEnumerable<CodeInstruction> _) => new[]
    {
        new CodeInstruction(OpCodes.Ldc_R4, 65536f), new(OpCodes.Ldc_R4, .00390625f), new(OpCodes.Add), new(OpCodes.Conv_R4),
        new(OpCodes.Ldc_R4, 65536f), new(OpCodes.Sub), new(OpCodes.Conv_R4), new(OpCodes.Ldc_R4, 4608f), new(OpCodes.Mul), new(OpCodes.Ret)
    };
    [TestCase(true)]
    [TestCase(false)]
    public void ReviewRegressionVerticalResultWriterRemainsEligibleAndPreservesConsumedWidth(bool resultOnly)
    {
        var target = typeof(Window).GetMethod(nameof(Window.Inner))!;
        var admission = new DrugToleranceGeometryAdmission("WINDOW", target, new(Members(), DrugToleranceGeometryStage.InnerWindow));
        var foreign = new HarmonyLib.Harmony("tests.drug-tolerance.geometry.vertical-result");
        verticalObservations = 0;
        try
        {
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("geometry", AdmissionPhase.Early, new[] { admission.Candidate }), AdmissionPhase.Early), Is.True);
            foreign.CreateProcessor(target).AddInnerPostfix(new HarmonyMethod(typeof(DrugToleranceGeometryTests), resultOnly ? nameof(ChangeVerticalResult) : nameof(ChangeVerticalReceiver))
                { innerMethod = new InnerMethod((MethodInfo)Members()["y"]) }).Patch();
            var window = new Window(); window.Inner();
            Assert.That(verticalObservations, Is.GreaterThan(0));
            Assert.That(window.LastWidth, Is.EqualTo(resultOnly ? 1264 : 500));
            Assert.That(admission.Available, Is.EqualTo(resultOnly), admission.Failure);
        }
        finally { try { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); } finally { admission.Candidate.Rollback(); } }
    }
    private static int verticalObservations;
    public static void ChangeVerticalResult(ref float __result) { verticalObservations++; __result += 100; }
    public static void ChangeVerticalReceiver(ref Rectangle __instance) { verticalObservations++; __instance.Width = 500; }
    [TestCase(true)]
    [TestCase(false)]
    public void ReviewRegressionUnusedHorizontalReadAllowsResultChangesButRejectsReceiverMutation(bool resultOnly)
    {
        var target = typeof(Editor).GetMethod(nameof(Editor.ContentsWithUnusedHorizontalRead))!;
        var admission = new DrugToleranceGeometryAdmission("MANAGER", target, new(Members(), DrugToleranceGeometryStage.Manager));
        var foreign = new HarmonyLib.Harmony("tests.drug-tolerance.geometry.unused-horizontal");
        verticalObservations = 0;
        try
        {
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("geometry", AdmissionPhase.Early, new[] { admission.Candidate }), AdmissionPhase.Early), Is.True);
            foreign.CreateProcessor(target).AddInnerPostfix(new HarmonyMethod(typeof(DrugToleranceGeometryTests), resultOnly ? nameof(ChangeVerticalResult) : nameof(ChangeVerticalReceiver))
                { innerMethod = new InnerMethod((MethodInfo)Members()["xMax"], -1) }).Patch();
            var editor = new Editor(); editor.ContentsWithUnusedHorizontalRead(new(0, 0, 1300, 720));
            Assert.That(verticalObservations, Is.EqualTo(1));
            Assert.That(editor.LastHorizontal, Is.EqualTo(resultOnly ? 1400 : 1300));
            Assert.That(editor.LastWidth, Is.EqualTo(resultOnly ? 1088 : 498));
            Assert.That(admission.Available, Is.EqualTo(resultOnly), admission.Failure);
        }
        finally { try { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); } finally { admission.Candidate.Rollback(); } }
    }
    [TestCase(DrugToleranceGeometryStage.Margin, typeof(Window), nameof(Window.Margin))]
    [TestCase(DrugToleranceGeometryStage.Manager, typeof(Editor), nameof(Editor.Contents))]
    [TestCase(DrugToleranceGeometryStage.ForwardConfig, typeof(Editor), nameof(Editor.Forward))]
    [TestCase(DrugToleranceGeometryStage.Config, typeof(Editor), nameof(Editor.Config))]
    [TestCase(DrugToleranceGeometryStage.Scroll, typeof(Editor), nameof(Editor.Adjust))]
    public void NativeShapedProducerBodiesReachTheirActualConsumers(DrugToleranceGeometryStage stage, Type type, string name)
    {
        var method = type.GetMethod(name) ?? type.GetProperty(name)!.GetMethod!;
        Assert.That(new DrugToleranceGeometryGuard(Members(), stage).Describe(PatchProcessor.GetOriginalInstructions(method)).Failure, Is.Null);
    }
    [TestCase(DrugToleranceGeometryStage.Manager, nameof(Editor.Contents), 200f, 400f)]
    [TestCase(DrugToleranceGeometryStage.Config, nameof(Editor.Config), 1f, 12f)]
    [TestCase(DrugToleranceGeometryStage.Scroll, nameof(Editor.Adjust), 20f, 60f)]
    public void ChangedConsumedProducerWidthsAreRejected(DrugToleranceGeometryStage stage, string name, float before, float after)
    {
        var body = PatchProcessor.GetOriginalInstructions(typeof(Editor).GetMethod(name)!);
        var instruction = body.Single(i => i.opcode == OpCodes.Ldc_R4 && Equals(i.operand, before)); instruction.operand = after;
        Assert.That(new DrugToleranceGeometryGuard(Members(), stage).Describe(body).Failure, Is.Not.Null);
    }
    [TestCase(nameof(Window.InnerAlias), true)]
    [TestCase(nameof(Window.InnerAliasWrite), false)]
    public void AnEarlierRectangleAliasIsTrackedThroughTheConsumer(string method, bool accepted)
    {
        var guard = new DrugToleranceGeometryGuard(Members(), DrugToleranceGeometryStage.InnerWindow);
        Assert.That(guard.Describe(PatchProcessor.GetOriginalInstructions(typeof(Window).GetMethod(method)!)).Failure == null, Is.EqualTo(accepted));
    }
    public static class Screen { public static int Width { [MethodImpl(MethodImplOptions.NoInlining)] get => 1920; } }
    public readonly struct Vector(float x, float y) { public readonly float X = x, Y = y; }
    public struct Rectangle(float x, float y, float width, float height)
    {
        public float X { get; set; } = x;
        public float Y { get; set; } = y;
        public float Width { get; set; } = width;
        public float Height { get; set; } = height;
        public float XMax { get => X + Width; set => Width = value - X; }
        public static Rectangle AtZero(Rectangle value) => new(0, 0, value.Width, value.Height);
        public static Rectangle Contract(Rectangle value, float margin) => new(value.X + margin, value.Y + margin, value.Width - 2 * margin, value.Height - 2 * margin);
        public static Rectangle Rounded(Rectangle value) => value;
    }
    public class Window
    {
        public Rectangle WindowRect = new(0, 0, 1300, 720);
        public float LastWidth;
        public float Margin { [MethodImpl(MethodImplOptions.NoInlining)] get => 18; }
        public Vector Size { [MethodImpl(MethodImplOptions.NoInlining)] get => new(1300, 720); }
        [MethodImpl(MethodImplOptions.NoInlining)] public void SetWindow()
        {
            var size = Size;
            WindowRect = new Rectangle((1920 - size.X) / 2, (1080 - size.Y) / 2, size.X, size.Y);
            WindowRect = Rectangle.Rounded(WindowRect);
        }
        [MethodImpl(MethodImplOptions.NoInlining)] public void Contents(Rectangle rectangle) => LastWidth = rectangle.Width;
        [MethodImpl(MethodImplOptions.NoInlining)] public void Inner()
        {
            var full = Rectangle.AtZero(WindowRect);
            var inner = Rectangle.Contract(full, Margin);
            inner.Y += 25;
            try { Contents(Rectangle.AtZero(inner)); }
            catch (Exception) { LastWidth = -1; }
        }
        [MethodImpl(MethodImplOptions.NoInlining)] public void InnerAlias()
        {
            Rectangle inner = default; ref var alias = ref inner;
            inner = Rectangle.Contract(Rectangle.AtZero(WindowRect), Margin);
            alias.Y = 25;
            Contents(Rectangle.AtZero(inner));
        }
        [MethodImpl(MethodImplOptions.NoInlining)] public void InnerAliasWrite()
        {
            Rectangle inner = default; ref var alias = ref inner;
            inner = Rectangle.Contract(Rectangle.AtZero(WindowRect), Margin);
            alias.Width = 500;
            Contents(Rectangle.AtZero(inner));
        }
    }
    public sealed class Manager
    {
        public Vector InitialSize { [MethodImpl(MethodImplOptions.NoInlining)] get => new(Math.Min(Screen.Width - 50, 1300), 720); }
    }
    public sealed class Editor
    {
        public float LastWidth;
        public float LastHorizontal;
        public int Count = 3;
        [MethodImpl(MethodImplOptions.NoInlining)] public void Contents(Rectangle input)
        {
            var left = input; left.Width = 200;
            var right = left; right.X = left.XMax + 10; right.XMax = input.XMax;
            right.Y = 100;
            if (Count > 0) Forward(right);
        }
        [MethodImpl(MethodImplOptions.NoInlining)] public void Forward(Rectangle input) => Config(input);
        [MethodImpl(MethodImplOptions.NoInlining)] public void ContentsWithUnusedHorizontalRead(Rectangle input)
        {
            var left = input; left.Width = 200;
            var right = left; right.X = left.XMax + 10; right.XMax = input.XMax;
            LastHorizontal = right.XMax;
            var consumed = right;
            Forward(consumed);
        }
        [MethodImpl(MethodImplOptions.NoInlining)] public void Config(Rectangle input)
        {
            var header = input; header.Height = 36; Header(header);
            var section = Rectangle.Contract(input, 1);
            if (Count == 0) return;
            var view = new Rectangle(0, 0, section.Width, Count * 35);
            Adjust(input, ref section, ref view);
            for (int i = 0; i < Count; i++) { var row = new Rectangle(0, i * 35, view.Width, 35); Row(row); }
        }
        [MethodImpl(MethodImplOptions.NoInlining)] public void Header(Rectangle input) => LastWidth = input.Width;
        [MethodImpl(MethodImplOptions.NoInlining)] public void Row(Rectangle input) => LastWidth = input.Width;
        [MethodImpl(MethodImplOptions.NoInlining)] public static void Adjust(Rectangle parent, ref Rectangle outer, ref Rectangle view)
        { if (view.Height >= outer.Height) { view.Width -= 20; outer.XMax -= 4; outer.Y += 6; } }
    }
}
