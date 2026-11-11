using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.MessagePlacement;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.MessagePlacement.BarBounds;

[TestFixture, NonParallelizable]
public sealed class MessageBarAdornmentGuardTests
{
    private static MethodInfo Method(Type type, string name) => type.GetMethod(name)!;
    private static MethodInfo Getter(Type type, string name) => type.GetProperty(name)!.GetMethod!;
    private static FieldInfo Field(Type type, string name) => type.GetField(name)!;
    private static readonly MethodInfo Target = Method(typeof(Bar), nameof(Bar.OnGUI));
    private static readonly MethodInfo Clicks = Method(typeof(Drawer), nameof(Drawer.HandleClicks));
    private static readonly MethodInfo Draw = Method(typeof(Drawer), nameof(Drawer.DrawColonist));
    private static readonly MethodInfo Mode = Getter(typeof(Prefs), nameof(Prefs.Mode));
    private static readonly ConstructorInfo RectConstructor = typeof(Rectangle).GetConstructor(new[] { typeof(float), typeof(float), typeof(float), typeof(float) })!;
    private static readonly MessageBarAdornmentGuard Adornments = new(new(
        Field(typeof(Bar), nameof(Bar.Drawer)), Field(typeof(Entry), nameof(Entry.Pawn)), Field(typeof(Entry), nameof(Entry.Group)),
        Field(typeof(Pawn), nameof(Pawn.Equipment)), Getter(typeof(Equipment), nameof(Equipment.Primary)), Mode,
        Getter(typeof(Pawn), nameof(Pawn.Drafted)), Field(typeof(Item), nameof(Item.Definition)), Getter(typeof(Definition), nameof(Definition.IsWeapon)),
        Method(typeof(Drawer), nameof(Drawer.DrawGroupFrame)), Method(typeof(Drawer), nameof(Drawer.HandleGroupFrameClicks)),
        Method(typeof(Widgets), nameof(Widgets.ThingIcon)), Getter(typeof(Rectangle), nameof(Rectangle.X)), Getter(typeof(Rectangle), nameof(Rectangle.Y)),
        Getter(typeof(Rectangle), nameof(Rectangle.Width)), Getter(typeof(Rectangle), nameof(Rectangle.Height)), RectConstructor,
        Method(typeof(Widgets), nameof(Widgets.Scale))));
    private static readonly MessageBarGeometryGuard Geometry = new(new(
        Field(typeof(Bar), nameof(Bar.Locations)), Getter(typeof(Bar), nameof(Bar.Size)),
        Field(typeof(Vector), nameof(Vector.X)), Field(typeof(Vector), nameof(Vector.Y)), RectConstructor,
        Clicks, Draw, Field(typeof(Entry), nameof(Entry.Pawn)), Field(typeof(Entry), nameof(Entry.Map)), Field(typeof(Bar), nameof(Bar.Drawer)),
        new[] { typeof(Gui) }), Adornments);

    [Test]
    public void RelevantPublishedWeaponRewriteDeclinesGeometryButPreservesNativeAndManualEffects()
    {
        var bar = Populated();
        int prepared = 0;
        var failures = new List<Exception>();
        var patch = Patch(() => prepared++, failures.Add);
        var foreign = new HarmonyLib.Harmony("tests.message-bar.adornment-rewrite");
        try
        {
            Admit(patch);
            bar.OnGUI();
            Assert.That(patch.GeometryAvailable, Is.True, patch.GeometryFailure);
            Assert.That(Widgets.Icons, Is.Zero);
            var original = patch.CaptureGeometryAuthority();
            foreign.CreateProcessor(Target).AddInnerPostfix(new HarmonyMethod(typeof(MessageBarAdornmentGuardTests), nameof(Always))
                { innerMethod = new InnerMethod(Mode) }).Patch();
            Assert.That(patch.RefreshPublication(), Is.True);
            bar.OnGUI();
            Assert.Multiple(() =>
            {
                Assert.That(patch.GeometryAvailable, Is.False, "A changed weapon eligibility cannot authorize absent-adornment clearance.");
                Assert.That(original(), Is.False);
                Assert.That(patch.Available, Is.True);
                Assert.That(prepared, Is.EqualTo(2));
                Assert.That(Widgets.Icons, Is.EqualTo(1));
                Assert.That(bar.Drawer.Portraits, Is.EqualTo(2));
                Assert.That(failures, Is.Empty);
            });
        }
        finally
        {
            try { foreign.Unpatch(Target, HarmonyPatchType.InnerPostfix, foreign.Id); }
            finally { patch.Candidate.Rollback(); }
        }
    }

    public static void Always(ref int __result) => __result = 2;
    [Test]
    public void ChangedIncomingWeaponEligibilityDeclinesOnlyGeometry()
    {
        var bar = Populated();
        int prepared = 0;
        var failures = new List<Exception>();
        var patch = Patch(() => prepared++, failures.Add);
        var foreign = new HarmonyLib.Harmony("tests.message-bar.adornment-condition");
        try
        {
            foreign.Patch(Target, transpiler: new HarmonyMethod(typeof(MessageBarAdornmentGuardTests), nameof(ChangeAlwaysValue)));
            Admit(patch);
            bar.OnGUI();
            Assert.Multiple(() =>
            {
                Assert.That(patch.GeometryAvailable, Is.False);
                Assert.That(patch.Available, Is.True);
                Assert.That(prepared, Is.EqualTo(1));
                Assert.That(Widgets.Icons, Is.EqualTo(1));
                Assert.That(bar.Drawer.Portraits, Is.EqualTo(1));
                Assert.That(failures, Is.Empty);
            });
        }
        finally
        {
            try { patch.Candidate.Rollback(); }
            finally { foreign.Unpatch(Target, HarmonyPatchType.Transpiler, foreign.Id); }
        }
    }
    public static IEnumerable<CodeInstruction> ChangeAlwaysValue(IEnumerable<CodeInstruction> source)
    {
        var body = source.ToList();
        int mode = body.FindIndex(instruction => instruction.Calls(Mode));
        body[mode + 1] = new(OpCodes.Ldc_I4_0);
        return body;
    }
    [TestCase(0, false, 0)]
    [TestCase(1, false, 0)]
    [TestCase(1, true, 1)]
    [TestCase(2, false, 1)]
    public void NativeBooleanMergeRetainsOrdinaryWeaponEligibility(int mode, bool drafted, int icons)
    {
        var bar = Populated();
        Prefs.Value = mode; bar.Rows[0].Pawn!.IsDrafted = drafted;
        var failures = new List<Exception>();
        var patch = Patch(() => { }, failures.Add);
        var foreign = new HarmonyLib.Harmony("tests.message-bar.adornment-native-merge");
        try
        {
            foreign.Patch(Target, transpiler: new HarmonyMethod(typeof(MessageBarAdornmentGuardTests), nameof(NativeMerge)));
            Admit(patch);
            bar.OnGUI();
            Assert.Multiple(() =>
            {
                Assert.That(patch.GeometryAvailable, Is.True, patch.GeometryFailure);
                Assert.That(Widgets.Icons, Is.EqualTo(icons));
                Assert.That(bar.Drawer.Frames, Is.Zero);
                Assert.That(bar.Drawer.FrameClicks, Is.Zero);
                Assert.That(failures, Is.Empty);
            });
        }
        finally
        {
            try { patch.Candidate.Rollback(); }
            finally { foreign.Unpatch(Target, HarmonyPatchType.Transpiler, foreign.Id); }
        }
    }
    public static IEnumerable<CodeInstruction> NativeMerge(IEnumerable<CodeInstruction> source, ILGenerator generator)
    {
        var body = source.ToList();
        int mode = body.FindIndex(instruction => instruction.Calls(Mode));
        var falseValue = generator.DefineLabel(); var trueValue = generator.DefineLabel(); var check = generator.DefineLabel();
        object end = body[mode + 9].operand;
        body[mode + 2].operand = trueValue; body[mode + 5].operand = falseValue;
        body.RemoveAt(mode + 9);
        var native = new[] { new CodeInstruction(OpCodes.Br, check), new(OpCodes.Ldc_I4_0), new(OpCodes.Br, check),
            new(OpCodes.Ldc_I4_1), new(OpCodes.Brfalse, end) };
        native[1].labels.Add(falseValue); native[3].labels.Add(trueValue); native[4].labels.Add(check);
        body.InsertRange(mode + 9, native);
        return body;
    }

    [TestCase("frame-and", 0, 1, 0)]
    [TestCase("frame-clicks", 0, 0, 1)]
    [TestCase("frame-local", 0, 1, 1)]
    [TestCase("weapon-bypass", 1, 0, 0)]
    [TestCase("weapon-offset", 1, 0, 0)]
    [TestCase("primary-escape", 0, 0, 0)]
    [TestCase("primary-overwrite", 0, 0, 0)]
    public void ChangedIncomingAdornmentPathDeclinesGeometryAndRetainsNativeEffects(string change, int icons, int frames, int clicks)
    {
        var bar = Populated();
        if (change == "weapon-offset") Prefs.Value = 2;
        int prepared = 0;
        var failures = new List<Exception>();
        var patch = Patch(() => prepared++, failures.Add);
        var foreign = new HarmonyLib.Harmony("tests.message-bar.adornment-shape");
        currentChange = change;
        try
        {
            foreign.Patch(Target, transpiler: new HarmonyMethod(typeof(MessageBarAdornmentGuardTests), nameof(ChangePath)));
            Admit(patch);
            bar.OnGUI();
            Assert.Multiple(() =>
            {
                Assert.That(patch.GeometryAvailable, Is.False, change);
                Assert.That(patch.Available, Is.True);
                Assert.That(prepared, Is.EqualTo(1));
                Assert.That(Widgets.Icons, Is.EqualTo(icons));
                Assert.That(bar.Drawer.Frames, Is.EqualTo(frames));
                Assert.That(bar.Drawer.FrameClicks, Is.EqualTo(clicks));
                Assert.That(bar.Drawer.Portraits, Is.EqualTo(1));
                Assert.That(failures, Is.Empty);
            });
        }
        finally
        {
            try { patch.Candidate.Rollback(); }
            finally { foreign.Unpatch(Target, HarmonyPatchType.Transpiler, foreign.Id); currentChange = null; }
        }
    }
    private static string? currentChange;
    public static IEnumerable<CodeInstruction> ChangePath(IEnumerable<CodeInstruction> source, ILGenerator generator)
    {
        var body = source.ToList();
        int frame = body.FindIndex(instruction => instruction.Calls(Method(typeof(Drawer), nameof(Drawer.DrawGroupFrame))));
        int primary = body.FindIndex(instruction => instruction.Calls(Getter(typeof(Equipment), nameof(Equipment.Primary))));
        int mode = body.FindIndex(instruction => instruction.Calls(Mode));
        switch (currentChange)
        {
            case "frame-and": body[frame - 6].opcode = OpCodes.Or; break;
            case "frame-clicks":
                int gate = body.FindLastIndex(instruction => instruction.opcode == OpCodes.Ldloc_2) + 1;
                body[gate] = new(OpCodes.Pop); break;
            case "frame-local":
                int flag = body.FindIndex(instruction => instruction.Calls(Getter(typeof(Bar), nameof(Bar.ShowFrames))));
                body.InsertRange(flag + 2, new[] { new CodeInstruction(OpCodes.Ldc_I4_1), new CodeInstruction(body[flag + 1]) }); break;
            case "weapon-bypass":
                int rectangle = body.FindIndex(mode, instruction => instruction.Calls(Getter(typeof(Rectangle), nameof(Rectangle.X)))) - 1;
                var label = generator.DefineLabel(); body[rectangle].labels.Add(label); body[mode + 5].operand = label; break;
            case "weapon-offset":
                body[body.FindIndex(instruction => instruction.opcode == OpCodes.Ldc_R4 && Equals(instruction.operand, 1.05f))].operand = 1.5f; break;
            case "primary-escape":
                body.InsertRange(primary - 8, new[] { new CodeInstruction(OpCodes.Ldloca, body[primary + 1].operand), new CodeInstruction(OpCodes.Pop) }); break;
            case "primary-overwrite":
                body.InsertRange(primary + 2, new[] { new CodeInstruction(OpCodes.Ldnull), new CodeInstruction(body[primary + 1]) }); break;
        }
        return body;
    }

    [Test]
    public void UnrelatedEffectAndNopsPreserveGeometryAndNativeEffects()
    {
        var bar = Populated();
        var failures = new List<Exception>();
        var patch = Patch(() => { }, failures.Add);
        var foreign = new HarmonyLib.Harmony("tests.message-bar.adornment-benign");
        unrelatedEffects = 0;
        Widgets.Observations = 0;
        try
        {
            foreign.Patch(Target, transpiler: new HarmonyMethod(typeof(MessageBarAdornmentGuardTests), nameof(Benign)));
            Admit(patch); bar.OnGUI();
            Assert.Multiple(() =>
            {
                Assert.That(patch.GeometryAvailable, Is.True, patch.GeometryFailure);
                Assert.That(unrelatedEffects, Is.EqualTo(1));
                Assert.That(Widgets.Observations, Is.EqualTo(1));
                Assert.That(bar.Drawer.Portraits, Is.EqualTo(1));
                Assert.That(failures, Is.Empty);
            });
        }
        finally
        {
            try { patch.Candidate.Rollback(); }
            finally { foreign.Unpatch(Target, HarmonyPatchType.Transpiler, foreign.Id); }
        }
    }
    private static int unrelatedEffects;
    public static void Unrelated() => unrelatedEffects++;
    public static IEnumerable<CodeInstruction> Benign(IEnumerable<CodeInstruction> source)
    {
        return new[] { new CodeInstruction(OpCodes.Call, Method(typeof(MessageBarAdornmentGuardTests), nameof(Unrelated))),
                new(OpCodes.Call, Method(typeof(Widgets), nameof(Widgets.Observe))) }
            .Concat(source.SelectMany(instruction => new[] { instruction, new CodeInstruction(OpCodes.Nop) }));
    }

    [TestCase(0)]
    [TestCase(1)]
    [TestCase(2)]
    [TestCase(3)]
    public void AddedOutwardDrawingOutsideTheProvedChainsRevokesOnlyAutomaticGeometry(int additionalFamily)
    {
        var bar = Populated();
        var failures = new List<Exception>();
        int prepared = 0;
        var patch = Patch(() => prepared++, failures.Add);
        var foreign = new HarmonyLib.Harmony("tests.message-bar.extra-drawing");
        Widgets.ExtraDraws = 0;
        extraDrawingKind = additionalFamily;
        try
        {
            foreign.Patch(Target, transpiler: new HarmonyMethod(typeof(MessageBarAdornmentGuardTests), nameof(ExtraDrawing)));
            Admit(patch); bar.OnGUI();
            Assert.Multiple(() =>
            {
                Assert.That(Widgets.ExtraDraws, Is.EqualTo(additionalFamily >= 2 ? 0 : 1));
                Assert.That(bar.Drawer.LastPortraitY, Is.EqualTo(additionalFamily >= 2 ? 434 : 34));
                Assert.That(patch.GeometryAvailable, Is.False, "New outward drawing is outside the measured native contribution.");
                Assert.That(patch.Available, Is.True);
                Assert.That(prepared, Is.EqualTo(1));
                Assert.That(bar.Drawer.Portraits, Is.EqualTo(1));
                Assert.That(failures, Is.Empty);
            });
        }
        finally
        {
            try { patch.Candidate.Rollback(); }
            finally { foreign.Unpatch(Target, HarmonyPatchType.Transpiler, foreign.Id); extraDrawingKind = 0; Gui.matrix = 0; }
        }
    }

    private static int extraDrawingKind;
    public static IEnumerable<CodeInstruction> ExtraDrawing(IEnumerable<CodeInstruction> source) =>
        (extraDrawingKind == 3 ? new[] { new CodeInstruction(OpCodes.Call, Method(typeof(Gui), nameof(Gui.EndGroup))) }
            : extraDrawingKind == 2
            ? new[] { new CodeInstruction(OpCodes.Ldc_R4, 400f), new(OpCodes.Call, typeof(Gui).GetProperty(nameof(Gui.matrix))!.SetMethod!) }
            : new[] { new CodeInstruction(OpCodes.Ldc_R4, 0f), new(OpCodes.Ldc_R4, 400f), new(OpCodes.Ldc_R4, 20f),
                new(OpCodes.Ldc_R4, 20f), new(OpCodes.Newobj, RectConstructor),
                new(OpCodes.Call, Method(extraDrawingKind == 1 ? typeof(Gui) : typeof(Widgets), nameof(Widgets.DrawTexture))) }).Concat(source);
    private static Bar Populated()
    {
        Widgets.Icons = 0; Prefs.Value = 0; Gui.matrix = 0;
        var bar = new Bar();
        bar.Rows.Add(new() { Pawn = new Pawn { Equipment = new Equipment() }, Map = new object(), Group = 3 });
        bar.Locations.Add(new(12, 34));
        return bar;
    }
    private static MessageBarPreparedPatch Patch(Action prepared, Action<Exception> failure) =>
        new(new(Target, Getter(typeof(Bar), nameof(Bar.Entries)), Getter(typeof(Bar), nameof(Bar.ShowFrames))),
            (_, _, _, _) => prepared(), failure, new(typeof(Rectangle), Clicks, Draw), Geometry, _ => { }, (_, _, _) => { });
    private static void Admit(MessageBarPreparedPatch patch)
    {
        var warnings = new List<string>();
        Assert.That(new AdmissionCoordinator(warnings.Add).Apply(new("bar-adornments", AdmissionPhase.Early,
            new[] { patch.Candidate }), AdmissionPhase.Early), Is.True, string.Join("; ", warnings));
        patch.Enable();
    }
    public readonly struct Vector(float x, float y) { public readonly float X = x, Y = y; }
    public struct Rectangle(float x, float y, float width, float height)
    { public float X { get; } = x; public float Y { get; } = y; public float Width { get; } = width; public float Height { get; } = height; }
    public sealed class Definition { public bool IsWeapon { [MethodImpl(MethodImplOptions.NoInlining)] get => true; } }
    public sealed class Item { public Definition Definition = new(); }
    public sealed class Equipment { public Item? Primary { [MethodImpl(MethodImplOptions.NoInlining)] get => new Item(); } }
    public sealed class Pawn { public Equipment? Equipment; public bool IsDrafted; public bool Drafted { [MethodImpl(MethodImplOptions.NoInlining)] get => IsDrafted; } }
    public struct Entry { public Pawn? Pawn; public object? Map; public int Group; }
    public static class Prefs { public static int Value; public static int Mode { [MethodImpl(MethodImplOptions.NoInlining)] get => Value; } }
    public static class Widgets
    {
        public static int Icons;
        public static int ExtraDraws;
        public static int Observations;
        [MethodImpl(MethodImplOptions.NoInlining)] public static void Observe() => Observations++;
        [MethodImpl(MethodImplOptions.NoInlining)] public static void DrawTexture(Rectangle rectangle) => ExtraDraws++;
        [MethodImpl(MethodImplOptions.NoInlining)] public static Rectangle Scale(Rectangle rectangle, float scale) => rectangle;
        [MethodImpl(MethodImplOptions.NoInlining)] public static void ThingIcon(Rectangle rectangle, Item item,
            float alpha, Rotation? rotation, bool stackOfOne, float scale = 1f, bool grayscale = false) => Icons++;
    }
    public static class Gui
    {
        public static float matrix { get; set; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static void EndGroup() => matrix += 400;
        [MethodImpl(MethodImplOptions.NoInlining)] public static void DrawTexture(Rectangle rectangle) => Widgets.ExtraDraws++;
    }
    public enum Rotation { North }
    public sealed class Drawer
    {
        public int Portraits, Frames, FrameClicks;
        public float LastPortraitY;
        [MethodImpl(MethodImplOptions.NoInlining)] public void HandleClicks(Rectangle rectangle, Pawn pawn, int group, out bool reordering) => reordering = false;
        [MethodImpl(MethodImplOptions.NoInlining)] public void DrawColonist(Rectangle rectangle, Pawn pawn, object? map, bool highlighted, bool reordering)
        { Portraits++; LastPortraitY = rectangle.Y + Gui.matrix; }
        [MethodImpl(MethodImplOptions.NoInlining)] public void DrawGroupFrame(int group) => Frames++;
        [MethodImpl(MethodImplOptions.NoInlining)] public void HandleGroupFrameClicks(int group) => FrameClicks++;
    }
    public sealed class Bar
    {
        public readonly List<Vector> Locations = new();
        public readonly List<Entry> Rows = new();
        public readonly Drawer Drawer = new();
        public bool Frames;
        public List<Entry> Entries { [MethodImpl(MethodImplOptions.NoInlining)] get => Rows; }
        public Vector Size { [MethodImpl(MethodImplOptions.NoInlining)] get => new(48, 48); }
        public bool ShowFrames { [MethodImpl(MethodImplOptions.NoInlining)] get => Frames; }
        [MethodImpl(MethodImplOptions.NoInlining)] public void OnGUI()
        {
            var entries = Entries;
            int previous = -1;
            bool frames = ShowFrames;
            for (int index = 0; index < Locations.Count; index++)
            {
                var rectangle = new Rectangle(Locations[index].X, Locations[index].Y, Size.X, Size.Y);
                var entry = entries[index];
                bool first = previous != entry.Group;
                previous = entry.Group;
                bool reordering;
                if (entry.Pawn != null) Drawer.HandleClicks(rectangle, entry.Pawn, 0, out reordering);
                else reordering = false;
                if (first & frames) Drawer.DrawGroupFrame(entry.Group);
                if (entry.Pawn != null)
                {
                    Drawer.DrawColonist(rectangle, entry.Pawn, entry.Map, false, reordering);
                    Item? primary = entry.Pawn.Equipment?.Primary;
                    if ((Prefs.Mode == 2 || Prefs.Mode == 1 && entry.Pawn.Drafted) && primary != null && primary.Definition.IsWeapon)
                        Widgets.ThingIcon(Widgets.Scale(new Rectangle(rectangle.X, rectangle.Y + rectangle.Height * 1.05f,
                            rectangle.Width, rectangle.Height), 0.75f), primary, 1f, null, stackOfOne: true);
                }
            }
            previous = -1;
            if (frames)
                for (int index = 0; index < Locations.Count; index++)
                {
                    var entry = entries[index];
                    bool first = previous != entry.Group;
                    previous = entry.Group;
                    if (first) Drawer.HandleGroupFrameClicks(entry.Group);
                }
        }
    }
}
