using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.DrugTolerance;
using Megamod.Patching;
using NUnit.Framework;
using Editor = Megamod.Harmony.Tests.Features.DrugTolerance.DrugToleranceEditorCallGuardTests.NativeEditor;
using Rectangle = Megamod.Harmony.Tests.Features.DrugTolerance.DrugToleranceEditorCallGuardTests.Rectangle;

namespace Megamod.Harmony.Tests.Features.DrugTolerance;

[TestFixture, NonParallelizable]
public sealed class DrugToleranceEditorPatchTests
{
    public sealed class Entry { }
    public sealed class RowEditor
    {
        public float Last;
        public Entry? LastEntry;
        public int Index;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Row(Rectangle rectangle, Entry entry, int index)
        { if (index == -2) throw new InvalidOperationException("native row failure"); Last = rectangle.Width; LastEntry = entry; Index = index; }
    }
    private static readonly MethodInfo Row = typeof(RowEditor).GetMethod(nameof(RowEditor.Row))!;

    [Test]
    public void TypedReservationFeedsNativeColumnsAndDrawsOnlyAfterTheirCompletion()
    {
        var target = typeof(Editor).GetMethod(nameof(Editor.Header))!;
        var width = typeof(Rectangle).GetProperty(nameof(Rectangle.Width))!;
        var guard = new DrugToleranceEditorCallGuard(typeof(Editor).GetMethod(nameof(Editor.Calculate))!, width.GetMethod!, width.SetMethod!, true);
        var reports = new List<string>();
        var editor = new Editor();
        float drawnFromWidth = 0, nativeAtDraw = 0;
        DrugToleranceEditorPatch? patch = null;
        patch = new("DT-test-header", target, true, Callbacks, guard.Validate,
            (manager, rectangle, row, index) => new(new Rectangle { Width = ((Rectangle)rectangle).Width - 120 }, () =>
            { drawnFromWidth = ((Rectangle)rectangle).Width; nativeAtDraw = ((Editor)manager).Last; }),
            () => patch!.Available, reports.Add);
        try
        {
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(new("editor", AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early), Is.True);
            editor.Header(new Rectangle { Width = 935 });
            Assert.That(editor.Last, Is.EqualTo(799).Within(0.001f));
            Assert.That(drawnFromWidth, Is.EqualTo(935));
            Assert.That(nativeAtDraw, Is.EqualTo(editor.Last));
            Assert.That(reports, Is.Empty);
        }
        finally { patch.Candidate.Rollback(); }
    }

    [Test]
    public void RowReservationCapturesTheFinalTypedArgumentsAndExactRow()
    {
        var reports = new List<string>();
        var foreign = new HarmonyLib.Harmony("tests.tolerance.editor-row-input");
        var editor = new RowEditor(); var entry = new Entry();
        object? drawnEntry = null; int drawnIndex = -1;
        var patch = MakeRow((manager, rectangle, row, index) => new(new Rectangle { Width = ((Rectangle)rectangle).Width - 120 },
            () => { drawnEntry = row; drawnIndex = index; }), () => true, reports.Add);
        try
        {
            foreign.Patch(Row, prefix: new HarmonyMethod(Method(nameof(Widen))));
            Apply(patch, reports);
            editor.Row(new Rectangle { Width = 935 }, entry, 3);
            Assert.That(editor.Last, Is.EqualTo(825));
            Assert.That(drawnEntry, Is.SameAs(entry)); Assert.That(drawnIndex, Is.EqualTo(3));
        }
        finally { try { patch.Candidate.Rollback(); } finally { foreign.Unpatch(Row, HarmonyPatchType.All, foreign.Id); } }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void SkippedOrThrowingNativeRowsNeverDraw(bool throws)
    {
        var reports = new List<string>(); int draws = 0;
        var foreign = new HarmonyLib.Harmony("tests.tolerance.editor-row-skip");
        var patch = MakeRow((_, rectangle, _, _) => new(rectangle, () => draws++), () => true, reports.Add);
        try
        {
            if (!throws) foreign.Patch(Row, prefix: new HarmonyMethod(Method(nameof(Skip))));
            Apply(patch, reports);
            var editor = new RowEditor();
            if (throws) Assert.That(() => editor.Row(new Rectangle { Width = 935 }, new Entry(), -2), Throws.InvalidOperationException.With.Message.EqualTo("native row failure"));
            else editor.Row(new Rectangle { Width = 935 }, new Entry(), 0);
            Assert.That(draws, Is.Zero);
        }
        finally { try { patch.Candidate.Rollback(); } finally { foreign.Unpatch(Row, HarmonyPatchType.All, foreign.Id); } }
    }

    [Test]
    public void RemovedObserverRevokesReservationAndPreservesNativeWidth()
    {
        var reports = new List<string>(); int draws = 0;
        var patch = MakeRow((_, rectangle, _, _) => new(new Rectangle { Width = ((Rectangle)rectangle).Width - 120 }, () => draws++), () => true, reports.Add);
        try
        {
            Apply(patch, reports);
            var editor = new RowEditor();
            editor.Row(new Rectangle { Width = 935 }, new Entry(), 0);
            Assert.That(editor.Last, Is.EqualTo(815));
            new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId).Unpatch(Row, HarmonyPatchType.Transpiler, OwnedHarmonyPatch.OwnerId);
            editor.Row(new Rectangle { Width = 935 }, new Entry(), 0);
            Assert.That(editor.Last, Is.EqualTo(935)); Assert.That(draws, Is.EqualTo(1));
            Assert.That(patch.Available, Is.False);
        }
        finally { patch.Candidate.Rollback(); }
    }

    private static DrugToleranceEditorPatch MakeRow(Func<object, object, object?, int, DrugToleranceEditorReservation?> prepare,
        Func<bool> available, Action<string> report) => new("DT-test-row", Row, false,
            new(Method(nameof(RowPrefix)), Method(nameof(Postfix)), Method(nameof(Finalizer))), _ => null, prepare, available, report);
    private static void Apply(DrugToleranceEditorPatch patch, List<string> reports) =>
        Assert.That(new AdmissionCoordinator(reports.Add).Apply(new("editor-row", AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early), Is.True, string.Join("; ", reports));
    private static DrugToleranceEditorCallbacks Callbacks => new(Method(nameof(HeaderPrefix)), Method(nameof(Postfix)), Method(nameof(Finalizer)));
    private static MethodInfo Method(string name) => typeof(DrugToleranceEditorPatchTests).GetMethod(name, BindingFlags.Public | BindingFlags.Static)!;
    [HarmonyPriority(int.MinValue)]
    public static void HeaderPrefix(MethodBase __originalMethod, object __instance, ref Rectangle __0, out object? __state)
    {
        __state = DrugToleranceEditorPatch.Prepare(__originalMethod, __instance, __0, null, -1, out var narrowed);
        __0 = (Rectangle)narrowed;
    }
    [HarmonyPriority(int.MinValue)]
    public static void RowPrefix(MethodBase __originalMethod, object __instance, ref Rectangle __0, object __1, int __2, out object? __state)
    {
        __state = DrugToleranceEditorPatch.Prepare(__originalMethod, __instance, __0, __1, __2, out var narrowed);
        __0 = (Rectangle)narrowed;
    }
    public static void Widen(ref Rectangle __0) { __0 = new Rectangle { Width = __0.Width + 10 }; }
    public static bool Skip() => false;
    [HarmonyPriority(int.MaxValue)]
    public static void Postfix(bool __runOriginal, object? __state) => DrugToleranceEditorPatch.CompleteNative(__runOriginal, __state);
    [HarmonyPriority(int.MaxValue)]
    public static Exception? Finalizer(Exception? __exception, bool __runOriginal, object? __state) => DrugToleranceEditorPatch.Draw(__exception, __runOriginal, __state);
}
