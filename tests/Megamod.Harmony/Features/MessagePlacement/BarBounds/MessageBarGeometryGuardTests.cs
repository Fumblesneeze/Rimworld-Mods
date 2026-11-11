using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.MessagePlacement;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.MessagePlacement.BarBounds;

[TestFixture, NonParallelizable]
public sealed class MessageBarGeometryGuardTests
{
    private static readonly MethodInfo Target = typeof(Bar).GetMethod(nameof(Bar.OnGUI))!;
    private static readonly MethodInfo Clicks = typeof(Drawer).GetMethod(nameof(Drawer.HandleClicks))!;
    private static readonly MethodInfo Draw = typeof(Drawer).GetMethod(nameof(Drawer.DrawColonist))!;
    private static readonly MessageBarGeometryGuard Guard = new(new(
        typeof(Bar).GetField(nameof(Bar.Locations))!, typeof(Bar).GetProperty(nameof(Bar.Size))!.GetMethod!,
        typeof(Vector).GetField(nameof(Vector.X))!, typeof(Vector).GetField(nameof(Vector.Y))!,
        typeof(Rectangle).GetConstructor(new[] { typeof(float), typeof(float), typeof(float), typeof(float) })!,
        Clicks, Draw, typeof(Entry).GetField(nameof(Entry.Pawn))!, typeof(Entry).GetField(nameof(Entry.Map))!,
        typeof(Bar).GetField(nameof(Bar.Drawer))!));

    [Test]
    public void PublishedGeometryCallbacksSurroundActualPreparationAndConsumers()
    {
        var order = new List<string>();
        var failures = new List<Exception>();
        var bar = new Bar { ReadingEntries = () => order.Add("native entries") };
        bar.Rows.Add(new() { Pawn = new Pawn(), Map = new Map() });
        bar.Locations.Add(new(12, 34));
        MessageBarPreparedPatch? patch = null;
        patch = new(new(Target, typeof(Bar).GetProperty(nameof(Bar.Entries))!.GetMethod!,
                typeof(Bar).GetProperty(nameof(Bar.ShowFrames))!.GetMethod!),
            (_, _, _, authority) => { Assert.That(authority(), Is.True); order.Add("prepared"); }, failures.Add,
            new(typeof(Rectangle), Clicks, Draw), Guard,
            _ => order.Add("preparing"), (_, index, rectangle) =>
            {
                Assert.That(patch!.CaptureGeometryAuthority()(), Is.True);
                Assert.That(index, Is.Zero);
                Assert.That(((Rectangle)rectangle).Y, Is.EqualTo(34));
                order.Add("consumer");
            });
        try
        {
            var warnings = new List<string>();
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(
                new("bar-geometry", AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early), Is.True, string.Join("; ", warnings));
            patch.Enable();
            bar.OnGUI();
            Assert.Multiple(() =>
            {
                Assert.That(patch.GeometryAvailable, Is.True, patch.GeometryFailure);
                Assert.That(order, Is.EqualTo(new[] { "preparing", "native entries", "prepared", "consumer", "consumer" }));
                Assert.That(failures, Is.Empty);
            });
        }
        finally { patch.Candidate.Rollback(); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void GeometryCallbackFailurePreservesManualPreparationAndNativeDrawing(bool duringPreparation)
    {
        int prepared = 0, generalFailures = 0, geometryFailures = 0;
        var bar = PopulatedBar();
        var patch = Patch((_, _, _, _) => prepared++, _ => generalFailures++,
            _ => { if (duringPreparation) throw new InvalidOperationException("geometry fixture"); },
            (_, _, _) => { if (!duringPreparation) throw new InvalidOperationException("geometry fixture"); },
            _ => geometryFailures++);
        try
        {
            Admit(patch);
            var authority = patch.CaptureGeometryAuthority();
            bar.OnGUI(); bar.OnGUI();
            Assert.Multiple(() =>
            {
                Assert.That(prepared, Is.EqualTo(2));
                Assert.That(bar.Drawer.DrawnY, Is.EqualTo(new[] { 34f, 34f }));
                Assert.That(patch.Available, Is.True);
                Assert.That(patch.GeometryAvailable, Is.False);
                Assert.That(authority(), Is.False);
                Assert.That(generalFailures, Is.Zero);
                Assert.That(geometryFailures, Is.EqualTo(1));
            });
        }
        finally { patch.Candidate.Rollback(); }
    }

    [Test]
    public void InnerRectangleRewriteRevokesGeometryForThatGenerationAndKeepsNativeEffects()
    {
        int prepared = 0, consumed = 0;
        var failures = new List<Exception>();
        var bar = PopulatedBar();
        var patch = Patch((_, _, _, _) => prepared++, failures.Add, _ => { }, (_, _, _) => consumed++, failures.Add);
        var foreign = new HarmonyLib.Harmony("tests.message-bar.geometry-rewrite");
        try
        {
            Admit(patch);
            var original = patch.CaptureGeometryAuthority();
            bar.OnGUI();
            foreign.CreateProcessor(Target).AddInnerPrefix(new HarmonyMethod(typeof(MessageBarGeometryGuardTests), nameof(RewriteRectangle))
                { innerMethod = new InnerMethod(Draw) }).Patch();
            Assert.That(patch.RefreshPublication(), Is.True);
            var denied = patch.CaptureGeometryAuthority();
            bar.OnGUI();
            Assert.Multiple(() =>
            {
                Assert.That(original(), Is.False);
                Assert.That(denied(), Is.False);
                Assert.That(patch.GeometryAvailable, Is.False);
                Assert.That(patch.GeometryFailure, Is.Not.Empty);
                Assert.That(bar.Drawer.DrawnY, Is.EqualTo(new[] { 34f, 134f }));
                Assert.That(prepared, Is.EqualTo(2));
                Assert.That(consumed, Is.EqualTo(2), "The changed body contributes no authoritative geometry callbacks.");
            });
            foreign.Unpatch(Target, HarmonyPatchType.InnerPrefix, foreign.Id);
            Assert.That(patch.RefreshPublication(), Is.True);
            bar.OnGUI();
            Assert.Multiple(() =>
            {
                Assert.That(patch.GeometryAvailable, Is.True);
                Assert.That(original(), Is.False);
                Assert.That(denied(), Is.False);
                Assert.That(consumed, Is.EqualTo(4));
                Assert.That(bar.Drawer.DrawnY.Last(), Is.EqualTo(34));
                Assert.That(failures, Is.Empty);
            });
        }
        finally
        {
            try { foreign.Unpatch(Target, HarmonyPatchType.InnerPrefix, foreign.Id); }
            finally { patch.Candidate.Rollback(); }
        }
    }

    public static void RewriteRectangle(ref Rectangle __0) => __0 = new(__0.X, __0.Y + 100, __0.Width, __0.Height);

    [Test]
    public void RemovedPreparedObserverCannotLeavePublishedGeometryAuthorityAlive()
    {
        int prepared = 0;
        var failures = new List<Exception>();
        var bar = PopulatedBar();
        var patch = Patch((_, _, _, _) => prepared++, failures.Add, _ => { }, (_, _, _) => { }, failures.Add);
        var remover = new HarmonyLib.Harmony("tests.message-bar.remove-owned-observer");
        try
        {
            Admit(patch);
            var captured = patch.CaptureGeometryAuthority();
            bar.OnGUI();
            remover.Unpatch(Target, HarmonyPatchType.Transpiler, OwnedHarmonyPatch.OwnerId);
            bar.OnGUI();
            Assert.Multiple(() =>
            {
                Assert.That(patch.GeometryAvailable, Is.False);
                Assert.That(captured(), Is.False);
                Assert.That(prepared, Is.EqualTo(1));
                Assert.That(bar.Drawer.DrawnY, Is.EqualTo(new[] { 34f, 34f }));
                Assert.That(failures, Is.Empty);
            });
        }
        finally { patch.Candidate.Rollback(); }
    }
    private static string mutation = "";
    private static MessageBarGeometryDescription? mutatedDescription;
    private static readonly Pawn ReplacementPawn = new();

    [TestCase("entry-store")]
    [TestCase("rectangle-store")]
    [TestCase("escaped-entry")]
    [TestCase("index-before-rectangle")]
    public void ExecutableAlternateProducerOrEscapedAddressCannotAcquireAuthority(string kind)
    {
        var owner = new HarmonyLib.Harmony("tests.message-bar.geometry-provenance");
        var bar = PopulatedBar();
        mutation = kind; mutatedDescription = null;
        if (kind == "index-before-rectangle")
        {
            bar.Rows.Add(new() { Pawn = new Pawn(), Map = new Map() });
            bar.Locations.Add(new(12, 134));
        }
        try
        {
            owner.Patch(Target, transpiler: new HarmonyMethod(typeof(MessageBarGeometryGuardTests), nameof(ChangeProvenance)));
            bar.OnGUI();
            Assert.That(bar.Drawer.DrawnY, Has.Count.EqualTo(1), "The changed IL remains a working native renderer.");
            if (kind == "rectangle-store") Assert.That(bar.Drawer.DrawnY[0], Is.EqualTo(234));
            else if (kind == "index-before-rectangle")
            {
                Assert.That(bar.Drawer.DrawnY[0], Is.EqualTo(134));
                Assert.That(bar.Drawer.DrawnPawns[0], Is.SameAs(bar.Rows[0].Pawn));
            }
            else Assert.That(bar.Drawer.DrawnPawns[0], Is.SameAs(ReplacementPawn));
            Assert.That(mutatedDescription?.Failure, Is.Not.Null, "The actual alternate value must not receive geometry authority.");
        }
        finally { owner.Unpatch(Target, HarmonyPatchType.Transpiler, owner.Id); mutation = ""; mutatedDescription = null; }
    }

    public static IEnumerable<CodeInstruction> ChangeProvenance(IEnumerable<CodeInstruction> source, ILGenerator generator)
    {
        var body = source.ToList();
        var initial = Describe(body);
        if (mutation == "index-before-rectangle")
        {
            int entry = body.FindIndex(instruction => instruction.operand is MethodInfo method &&
                method.Name == "get_Item" && method.DeclaringType == typeof(List<Entry>));
            var selection = body.GetRange(entry - 2, 4);
            body.RemoveRange(entry - 2, 4);
            int rectangle = body.FindIndex(instruction =>
                (instruction.opcode == OpCodes.Ldloca || instruction.opcode == OpCodes.Ldloca_S) &&
                Local(instruction) == initial.RectangleLocal);
            selection[0].labels.AddRange(body[rectangle].labels);
            body[rectangle].labels.Clear();
            selection.AddRange(new[] { new CodeInstruction(OpCodes.Ldloc, initial.IndexLocal), new(OpCodes.Ldc_I4_1),
                new(OpCodes.Add), new(OpCodes.Stloc, initial.IndexLocal) });
            body.InsertRange(rectangle, selection);
        }
        else if (mutation == "escaped-entry")
        {
            var alias = generator.DeclareLocal(typeof(Entry).MakeByRefType());
            int write = body.FindIndex(instruction => LocalStore(instruction) == initial.EntryLocal);
            body.InsertRange(write + 1, new[] { new CodeInstruction(OpCodes.Ldloc, alias),
                new CodeInstruction(OpCodes.Call, typeof(MessageBarGeometryGuardTests).GetMethod(nameof(ChangeEntry))!) });
            body.InsertRange(0, new[] { new CodeInstruction(OpCodes.Ldloca, initial.EntryLocal), new CodeInstruction(OpCodes.Stloc, alias) });
        }
        else
        {
            int producer, start;
            MethodInfo replacement;
            if (mutation == "entry-store")
            {
                producer = body.FindIndex(instruction => instruction.operand is MethodInfo method && method.Name == "get_Item" && method.DeclaringType == typeof(List<Entry>));
                start = producer - 2;
                replacement = typeof(MessageBarGeometryGuardTests).GetMethod(nameof(OtherEntry))!;
            }
            else
            {
                producer = body.FindIndex(instruction => instruction.operand is ConstructorInfo info && info.DeclaringType == typeof(Rectangle));
                int address = body.FindLastIndex(producer, instruction =>
                    (instruction.opcode == OpCodes.Ldloca || instruction.opcode == OpCodes.Ldloca_S) && Local(instruction) == initial.RectangleLocal);
                Assert.That(address, Is.GreaterThanOrEqualTo(0), "The fixture's direct ctor address is converted to an equivalent newobj/store.");
                body[address + 1].labels.AddRange(body[address].labels);
                body.RemoveAt(address); producer--;
                body[producer].opcode = OpCodes.Newobj;
                body.Insert(producer + 1, new(OpCodes.Stloc, initial.RectangleLocal));
                start = address;
                replacement = typeof(MessageBarGeometryGuardTests).GetMethod(nameof(OtherRectangle))!;
            }
            var alternate = generator.DefineLabel();
            var store = generator.DefineLabel();
            var replace = new CodeInstruction(OpCodes.Call, replacement); replace.labels.Add(alternate);
            var enterStore = new CodeInstruction(OpCodes.Nop); enterStore.labels.Add(store);
            body.Insert(producer + 1, enterStore);
            // The alternate producer is outside the contiguous native expression, then enters
            // its destination through a labeled NOP. Both routes execute valid native IL.
            body.Add(replace); body.Add(new(OpCodes.Br, store));
            var choose = new CodeInstruction(OpCodes.Ldc_I4_1); choose.labels.AddRange(body[start].labels); body[start].labels.Clear();
            body.InsertRange(start, new[] { choose, new CodeInstruction(OpCodes.Brtrue, alternate) });
        }
        mutatedDescription = Describe(body);
        return body;
    }
    [MethodImpl(MethodImplOptions.NoInlining)] public static Entry OtherEntry() => new() { Pawn = ReplacementPawn, Map = new Map() };
    [MethodImpl(MethodImplOptions.NoInlining)] public static Rectangle OtherRectangle() => new(12, 234, 48, 48);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void ChangeEntry(ref Entry entry) => entry = OtherEntry();
    private static Bar PopulatedBar()
    {
        var bar = new Bar();
        bar.Rows.Add(new() { Pawn = new Pawn(), Map = new Map() });
        bar.Locations.Add(new(12, 34));
        return bar;
    }
    private static MessageBarPreparedPatch Patch(Action<object, object, bool, Func<bool>> prepared, Action<Exception> report,
        Action<object> preparing, Action<object, int, object> consuming, Action<Exception> geometryFailure) =>
        new(new(Target, typeof(Bar).GetProperty(nameof(Bar.Entries))!.GetMethod!, typeof(Bar).GetProperty(nameof(Bar.ShowFrames))!.GetMethod!),
            prepared, report, new(typeof(Rectangle), Clicks, Draw), Guard, preparing, consuming, geometryFailure);
    private static void Admit(MessageBarPreparedPatch patch)
    {
        var warnings = new List<string>();
        Assert.That(new AdmissionCoordinator(warnings.Add).Apply(
            new("bar-geometry", AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early), Is.True, string.Join("; ", warnings));
        patch.Enable();
    }

    [Test]
    public void ActualLoopProvesSamePreparedEntryAndRectangleAtBothConsumers()
    {
        var body = PatchProcessor.GetOriginalInstructions(Target);
        var result = Describe(body);
        Assert.Multiple(() =>
        {
            Assert.That(result.Failure, Is.Null);
            Assert.That(result.EntryConsumers.Select(index => body[index].operand), Is.EqualTo(new[] { Clicks, Draw }));
            Assert.That(result.Required, Does.Contain(body.FindIndex(instruction => instruction.Calls(Clicks))));
            Assert.That(result.Required, Does.Contain(body.FindIndex(instruction => instruction.Calls(Draw))));
            Assert.That(result.IndexLocal, Is.GreaterThanOrEqualTo(0));
            Assert.That(result.RectangleLocal, Is.Not.EqualTo(result.IndexLocal));
        });
    }

    [TestCase("location component")]
    [TestCase("location index")]
    [TestCase("size component")]
    [TestCase("entry index")]
    [TestCase("consumer entry")]
    public void ChangedSourceOrConsumerCannotAcquireGeometryAuthority(string change)
    {
        var body = PatchProcessor.GetOriginalInstructions(Target);
        var admitted = Describe(body);
        var vectorY = typeof(Vector).GetField(nameof(Vector.Y))!;
        var itemCalls = Enumerable.Range(0, body.Count).Where(index => body[index].operand is MethodInfo method && method.Name == "get_Item").ToArray();
        switch (change)
        {
            case "location component": body[body.FindIndex(instruction => Equals(instruction.operand, vectorY))].operand = typeof(Vector).GetField(nameof(Vector.X))!; break;
            case "location index": body[itemCalls[1] - 1] = new(OpCodes.Ldc_I4_0); break;
            case "size component": body[body.FindLastIndex(instruction => Equals(instruction.operand, vectorY))].operand = typeof(Vector).GetField(nameof(Vector.X))!; break;
            case "entry index": body[itemCalls[2] - 1] = new(OpCodes.Ldc_I4_0); break;
            case "consumer entry":
                int map = body.FindIndex(instruction => Equals(instruction.operand, typeof(Entry).GetField(nameof(Entry.Map))!));
                body[map - 1] = new(OpCodes.Ldloc, admitted.IndexLocal); break;
        }
        Assert.That(Describe(body).Failure, Is.Not.Null, change);
    }

    [TestCase("rectangle")]
    [TestCase("entry")]
    [TestCase("index")]
    [TestCase("entries")]
    public void LocalMutationAfterPreparationRevokesGeometry(string changed)
    {
        var body = PatchProcessor.GetOriginalInstructions(Target);
        var admitted = Describe(body);
        int constructor = body.FindIndex(instruction => instruction.operand is ConstructorInfo info && info.DeclaringType == typeof(Rectangle));
        int at = constructor + (body[constructor].opcode == OpCodes.Newobj ? 2 : 1);
        int local = changed switch
        {
            "rectangle" => admitted.RectangleLocal,
            "entry" => admitted.EntryLocal,
            "index" => admitted.IndexLocal,
            _ => Local(body[body.FindIndex(instruction => instruction.Calls(typeof(Bar).GetProperty(nameof(Bar.Entries))!.GetMethod!)) + 1])
        };
        if (changed == "entry")
            at = body.FindIndex(at, instruction => LocalStore(instruction) == local) + 1;
        var mutation = changed is "rectangle" or "entry"
            ? new[] { new CodeInstruction(OpCodes.Ldloca, local), new CodeInstruction(OpCodes.Initobj, changed == "rectangle" ? typeof(Rectangle) : typeof(Entry)) }
            : new[] { new CodeInstruction(changed == "index" ? OpCodes.Ldc_I4_0 : OpCodes.Ldnull), new CodeInstruction(OpCodes.Stloc, local) };
        body.InsertRange(at, mutation);
        Assert.That(Describe(body).Failure, Is.Not.Null, changed);
    }

    [Test]
    public void BranchBypassingRectangleProductionIsRejected()
    {
        var body = PatchProcessor.GetOriginalInstructions(Target);
        int constructor = body.FindIndex(instruction => instruction.operand is ConstructorInfo info && info.DeclaringType == typeof(Rectangle));
        int after = constructor + (body[constructor].opcode == OpCodes.Newobj ? 2 : 1);
        var generator = new DynamicMethod("labels", typeof(void), Type.EmptyTypes).GetILGenerator();
        var destination = generator.DefineLabel();
        body[after].labels.Add(destination);
        int frames = body.FindIndex(instruction => instruction.Calls(typeof(Bar).GetProperty(nameof(Bar.ShowFrames))!.GetMethod!));
        body.InsertRange(frames + 2, new[] { new CodeInstruction(OpCodes.Ldc_I4_0), new CodeInstruction(OpCodes.Brtrue, destination) });
        Assert.That(Describe(body).Failure, Is.Not.Null);
    }

    [Test]
    public void UnrelatedNativeEffectOutsideRequiredExpressionsRemainsAvailable()
    {
        var body = PatchProcessor.GetOriginalInstructions(Target);
        int constructor = body.FindIndex(instruction => instruction.operand is ConstructorInfo info && info.DeclaringType == typeof(Rectangle));
        int after = constructor + (body[constructor].opcode == OpCodes.Newobj ? 2 : 1);
        var probe = typeof(MessageBarGeometryGuardTests).GetMethod(nameof(UnrelatedEffect))!;
        body.Insert(after, new(OpCodes.Call, probe));
        body.Insert(after + 1, new(OpCodes.Nop));
        var result = Describe(body);
        Assert.That(result.Failure, Is.Null);
        Assert.That(result.Required, Does.Not.Contain(after), "Unrelated effects do not become owned geometry operations.");
    }

    [MethodImpl(MethodImplOptions.NoInlining)] public static void UnrelatedEffect() { }
    private static int LocalStore(CodeInstruction instruction) => instruction.opcode.Name?.StartsWith("stloc", StringComparison.Ordinal) == true ? Local(instruction) : -1;

    private static MessageBarGeometryDescription Describe(List<CodeInstruction> body)
    {
        int entries = body.FindIndex(instruction => instruction.Calls(typeof(Bar).GetProperty(nameof(Bar.Entries))!.GetMethod!));
        int frames = body.FindIndex(instruction => instruction.Calls(typeof(Bar).GetProperty(nameof(Bar.ShowFrames))!.GetMethod!));
        return Guard.Describe(body, frames + 1, Local(body[entries + 1]));
    }

    private static int Local(CodeInstruction instruction) => instruction.opcode == OpCodes.Stloc_0 ? 0 :
        instruction.opcode == OpCodes.Stloc_1 ? 1 : instruction.opcode == OpCodes.Stloc_2 ? 2 :
        instruction.opcode == OpCodes.Stloc_3 ? 3 : instruction.operand is LocalBuilder local ? local.LocalIndex : Convert.ToInt32(instruction.operand);

    public readonly struct Vector(float x, float y) { public readonly float X = x, Y = y; }
    public readonly struct Rectangle(float x, float y, float width, float height)
    { public readonly float X = x, Y = y, Width = width, Height = height; }
    public sealed class Pawn;
    public sealed class Map;
    public struct Entry { public Pawn? Pawn; public Map? Map; }
    public sealed class Drawer
    {
        public readonly List<float> DrawnY = new();
        public readonly List<Pawn> DrawnPawns = new();
        [MethodImpl(MethodImplOptions.NoInlining)] public void HandleClicks(Rectangle rect, Pawn pawn, int group, out bool reordering) => reordering = false;
        [MethodImpl(MethodImplOptions.NoInlining)] public void DrawColonist(Rectangle rect, Pawn pawn, Map? map, bool highlighted, bool reordering)
        { DrawnY.Add(rect.Y); DrawnPawns.Add(pawn); }
    }
    public sealed class Bar
    {
        public readonly List<Vector> Locations = new();
        public readonly Drawer Drawer = new();
        public readonly List<Entry> Rows = new();
        public Action? ReadingEntries;
        public List<Entry> Entries { [MethodImpl(MethodImplOptions.NoInlining)] get { ReadingEntries?.Invoke(); return Rows; } }
        public Vector Size { [MethodImpl(MethodImplOptions.NoInlining)] get => new(48, 48); }
        public bool ShowFrames { [MethodImpl(MethodImplOptions.NoInlining)] get => true; }
        [MethodImpl(MethodImplOptions.NoInlining)] public void OnGUI()
        {
            var entries = Entries;
            bool frames = ShowFrames;
            for (int index = 0; index < Locations.Count; index++)
            {
                var rect = new Rectangle(Locations[index].X, Locations[index].Y, Size.X, Size.Y);
                var entry = entries[index];
                if (entry.Pawn == null) continue;
                Drawer.HandleClicks(rect, entry.Pawn, 0, out bool reordering);
                if (frames) Drawer.DrawColonist(rect, entry.Pawn, entry.Map, false, reordering);
            }
        }
    }
}
