using System.Reflection;
using System.Runtime.CompilerServices;
using System.Reflection.Emit;
using HarmonyLib;
using Megamod.Features.DrugTolerance;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.DrugTolerance;

[TestFixture, NonParallelizable]
public sealed class DrugToleranceEditorCallGuardTests
{
    public struct Rectangle { public float Width { get; set; } }
    public sealed class NativeEditor
    {
        public float Last;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Header(Rectangle rect)
        {
            rect.Width -= 16;
            Calculate(rect, out float a, out float b, out float c, out float d, out float e,
                out float f, out float g, out float h, out float i);
            Last = a + b + c + d + e + f + g + h + i;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Row(Rectangle rect)
        {
            Calculate(rect, out float a, out float b, out float c, out float d, out float e,
                out float f, out float g, out float h, out float i);
            Last = a + b + c + d + e + f + g + h + i;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CopiedRow(Rectangle rect)
        {
            var copy = rect;
            Calculate(copy, out float a, out float b, out float c, out float d, out float e,
                out float f, out float g, out float h, out float i);
            Last = a + b + c + d + e + f + g + h + i + copy.Width;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Calculate(Rectangle rect, out float addiction, out float joy, out float scheduled,
            out float icon, out float name, out float frequency, out float mood, out float recreation, out float inventory)
        {
            float variable = rect.Width - 27f - 108f;
            icon = 27f; name = variable * 0.25f;
            addiction = 36f; joy = 36f; scheduled = 36f;
            frequency = variable * 0.3f; mood = variable * 0.15f;
            recreation = variable * 0.15f; inventory = variable * 0.15f;
        }
    }
    private static string? failure;
    [TestCase(false)]
    [TestCase(true)]
    public void CopiedRectangleCanBeReadButCannotBeOverwrittenAfterCalculation(bool overwrite)
    {
        var target = typeof(NativeEditor).GetMethod(nameof(NativeEditor.CopiedRow))!;
        var owner = new HarmonyLib.Harmony("tests.tolerance.editor-call.copy-address");
        failure = "Observer did not execute.";
        try
        {
            if (overwrite) owner.Patch(target, transpiler: new HarmonyMethod(typeof(DrugToleranceEditorCallGuardTests), nameof(OverwriteCopy)) { priority = Priority.First });
            owner.Patch(target, transpiler: new HarmonyMethod(typeof(DrugToleranceEditorCallGuardTests), nameof(Inspect)) { priority = Priority.Last });
            var editor = new NativeEditor();
            editor.CopiedRow(new Rectangle { Width = 935 });
            Assert.That(editor.Last, Is.EqualTo(overwrite ? 977 : 1870).Within(0.001f));
            if (overwrite) Assert.That(failure, Is.Not.Null, "The escaped copied rectangle must revoke the reservation.");
            else Assert.That(failure, Is.Null);
        }
        finally { owner.Unpatch(target, HarmonyPatchType.All, owner.Id); }
    }
    public static IEnumerable<CodeInstruction> OverwriteCopy(IEnumerable<CodeInstruction> instructions)
    {
        var body = instructions.ToList();
        int call = body.FindIndex(i => i.Calls(typeof(NativeEditor).GetMethod(nameof(NativeEditor.Calculate))!));
        var address = body.Skip(call + 1).First(i => i.opcode == OpCodes.Ldloca || i.opcode == OpCodes.Ldloca_S);
        body.InsertRange(call + 1, new[] { new CodeInstruction(address.opcode, address.operand), new(OpCodes.Ldc_R4, 42f),
            new(OpCodes.Call, typeof(Rectangle).GetProperty(nameof(Rectangle.Width))!.SetMethod!) });
        return body;
    }
    [TestCase(false)]
    [TestCase(true)]
    public void FloatingCancellationCannotAuthorizeAWidthThatTheExecutedBodyLost(bool header)
    {
        var target = typeof(NativeEditor).GetMethod(header ? nameof(NativeEditor.Header) : nameof(NativeEditor.Row))!;
        var owner = new HarmonyLib.Harmony("tests.tolerance.editor-call.cancellation");
        failure = "Observer did not execute.";
        try
        {
            owner.Patch(target, transpiler: new HarmonyMethod(typeof(DrugToleranceEditorCallGuardTests), nameof(CancelWidth)) { priority = Priority.First });
            owner.Patch(target, transpiler: new HarmonyMethod(typeof(DrugToleranceEditorCallGuardTests), nameof(Inspect)) { priority = Priority.Last });
            var editor = new NativeEditor();
            if (header) editor.Header(new Rectangle { Width = 935 }); else editor.Row(new Rectangle { Width = 935 });
            Assert.That(editor.Last, Is.EqualTo(header ? -16 : 0).Within(0.001f), "The real Single operations must reproduce the lost width.");
            Assert.That(failure, Is.Not.Null, "Lost width must make reservation unavailable.");
        }
        finally { owner.Unpatch(target, HarmonyPatchType.All, owner.Id); }
    }

    public static IEnumerable<CodeInstruction> CancelWidth(IEnumerable<CodeInstruction> instructions)
    {
        var width = typeof(Rectangle).GetProperty(nameof(Rectangle.Width))!;
        yield return new(OpCodes.Ldarga_S, (byte)1);
        yield return new(OpCodes.Dup);
        yield return new(OpCodes.Call, width.GetMethod!);
        yield return new(OpCodes.Ldc_R4, 1e30f);
        yield return new(OpCodes.Add);
        yield return new(OpCodes.Ldc_R4, 1e30f);
        yield return new(OpCodes.Sub);
        yield return new(OpCodes.Call, width.SetMethod!);
        foreach (var instruction in instructions) yield return instruction;
    }

    [TestCase(false)]
    [TestCase(true)]
    public void DeadOutputReadsCannotAuthorizeAReservationWhenDrawingIsSkipped(bool header)
    {
        var target = typeof(NativeEditor).GetMethod(header ? nameof(NativeEditor.Header) : nameof(NativeEditor.Row))!;
        var owner = new HarmonyLib.Harmony("tests.tolerance.editor-call.dead-reads");
        failure = "Observer did not execute.";
        try
        {
            owner.Patch(target, transpiler: new HarmonyMethod(typeof(DrugToleranceEditorCallGuardTests), nameof(SkipDrawing)) { priority = Priority.First });
            owner.Patch(target, transpiler: new HarmonyMethod(typeof(DrugToleranceEditorCallGuardTests), nameof(Inspect)) { priority = Priority.Last });
            var editor = new NativeEditor { Last = -777 };
            if (header) editor.Header(new Rectangle { Width = 935 }); else editor.Row(new Rectangle { Width = 935 });
            Assert.That(editor.Last, Is.EqualTo(-777), "The real branch must bypass all output consumers.");
            Assert.That(failure, Is.Not.Null, "Dead reads must not prove native drawing consumes the reservation.");
        }
        finally { owner.Unpatch(target, HarmonyPatchType.All, owner.Id); }
    }

    public static IEnumerable<CodeInstruction> SkipDrawing(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var body = instructions.ToList();
        var exit = generator.DefineLabel();
        body.Last(i => i.opcode == OpCodes.Ret).labels.Add(exit);
        int call = body.FindIndex(i => i.Calls(typeof(NativeEditor).GetMethod(nameof(NativeEditor.Calculate))!));
        body.Insert(call + 1, new(OpCodes.Br, exit));
        return body;
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ForwardBranchIntoTheCompleteProducerRetainsNativeDrawing(bool header)
    {
        var target = typeof(NativeEditor).GetMethod(header ? nameof(NativeEditor.Header) : nameof(NativeEditor.Row))!;
        var owner = new HarmonyLib.Harmony("tests.tolerance.editor-call.complete-branch");
        failure = "Observer did not execute.";
        try
        {
            owner.Patch(target, transpiler: new HarmonyMethod(typeof(DrugToleranceEditorCallGuardTests), nameof(BranchToProducer)) { priority = Priority.First });
            owner.Patch(target, transpiler: new HarmonyMethod(typeof(DrugToleranceEditorCallGuardTests), nameof(Inspect)) { priority = Priority.Last });
            var editor = new NativeEditor();
            if (header) editor.Header(new Rectangle { Width = 935 }); else editor.Row(new Rectangle { Width = 935 });
            Assert.That(editor.Last, Is.EqualTo(header ? 919 : 935).Within(0.001f));
            Assert.That(failure, Is.Null);
        }
        finally { owner.Unpatch(target, HarmonyPatchType.All, owner.Id); }
    }

    public static IEnumerable<CodeInstruction> BranchToProducer(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var body = instructions.ToList();
        var start = generator.DefineLabel();
        body[0].labels.Add(start);
        body.Insert(0, new(OpCodes.Nop));
        body.Insert(0, new(OpCodes.Br, start));
        return body;
    }
    [TestCase(false)]
    [TestCase(true)]
    public void NativeDrawUsesTheSuppliedRectangleAndDistinctColumnOutputs(bool header)
    {
        var target = typeof(NativeEditor).GetMethod(header ? nameof(NativeEditor.Header) : nameof(NativeEditor.Row))!;
        var owner = new HarmonyLib.Harmony("tests.tolerance.editor-call");
        failure = "Observer did not execute.";
        try
        {
            owner.Patch(target, transpiler: new HarmonyMethod(typeof(DrugToleranceEditorCallGuardTests), nameof(Inspect)));
            var editor = new NativeEditor();
            if (header) editor.Header(new Rectangle { Width = 935 }); else editor.Row(new Rectangle { Width = 935 });
            Assert.That(editor.Last, Is.EqualTo(header ? 919 : 935).Within(0.001f));
            Assert.That(failure, Is.Null);
        }
        finally { owner.Unpatch(target, HarmonyPatchType.All, owner.Id); }
    }
    public static IEnumerable<CodeInstruction> Inspect(IEnumerable<CodeInstruction> instructions, MethodBase __originalMethod)
    {
        var body = instructions.ToList();
        var width = typeof(Rectangle).GetProperty(nameof(Rectangle.Width))!;
        var guard = new DrugToleranceEditorCallGuard(typeof(NativeEditor).GetMethod(nameof(NativeEditor.Calculate))!,
            width.GetMethod!, width.SetMethod!, __originalMethod.Name == nameof(NativeEditor.Header));
        failure = guard.Validate(body);
        return body;
    }
}
