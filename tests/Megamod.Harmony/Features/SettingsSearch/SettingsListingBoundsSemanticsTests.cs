using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.SettingsSearch.Harmony;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.SettingsSearch;

[TestFixture, NonParallelizable]
public sealed class SettingsListingBoundsSemanticsTests
{
    private static readonly MethodInfo Early = typeof(Listing).GetMethod(nameof(Listing.EarlyReturn))!;
    private static readonly MethodInfo Getter = typeof(Listing).GetProperty(nameof(Listing.Bounds))!.GetMethod!;
    private static readonly MethodInfo HasValue = typeof(Box?).GetProperty(nameof(Nullable<Box>.HasValue))!.GetMethod!;
    private static readonly MethodInfo Value = typeof(Box?).GetProperty(nameof(Nullable<Box>.Value))!.GetMethod!;
    private static readonly MethodInfo Overlaps = typeof(Box).GetMethod(nameof(Box.Overlaps))!;
    private static readonly MethodInfo Draw = typeof(SettingsListingBoundsSemanticsTests).GetMethod(nameof(DrawBox))!;
    private static readonly MethodInfo DetailedDraw = typeof(SettingsListingBoundsSemanticsTests).GetMethod(nameof(DrawDetailed))!;
    private static int drawings, otherDrawings, observed, lastRectangle;

    [Test]
    public void ProvesNullAndOverlapEnterTheSameNativeDrawWhileDisjointBoundsCull()
    {
        var listing = new Listing();
        var row = new Box(10);
        drawings = 0;
        listing.Bounds = null;
        listing.EarlyReturn(row);
        Assert.That(drawings, Is.EqualTo(1), "The actual unbounded row must draw.");
        listing.Bounds = new Box(20);
        listing.EarlyReturn(row);
        Assert.That(drawings, Is.EqualTo(1), "The actual disjoint bound must cull.");
        listing.Bounds = row;
        listing.EarlyReturn(row);
        Assert.That(drawings, Is.EqualTo(2), "The actual overlapping row must draw.");

        var result = Checker().Describe(Early, PatchProcessor.GetOriginalInstructions(Early));
        Assert.That(result.Failure, Is.Null);
        Assert.That(result.Required, Is.Not.Null.And.Not.Empty);
    }

    private static SettingsListingBoundsSemantics Checker() => new(new(Getter, HasValue, Value, Overlaps,
        new[] { new SettingsListingBoundsSemantics.Draw(Draw) }));

    [TestCase(nameof(Listing.NativeDetailed), false)]
    [TestCase(nameof(Listing.NativeBlock), false)]
    [TestCase(nameof(Listing.NativeAlternatives), false)]
    [TestCase(nameof(Listing.NativeAlternatives), true)]
    [TestCase(nameof(Listing.BenignAlias), false)]
    [TestCase(nameof(Listing.BranchToProducerStart), false)]
    [TestCase(nameof(Listing.BranchToProducerStart), true)]
    public void ProvesNativeLocalRowsArgumentsAndBothLabelBranches(string name, bool flag)
    {
        MethodInfo method = typeof(Listing).GetMethod(name)!;
        var listing = new Listing();
        foreach (var bounds in new Box?[] { null, new Box(20), new Box(10) })
        {
            drawings = 0; lastRectangle = -1;
            listing.Bounds = bounds;
            method.Invoke(listing, new object[] { flag, false });
            bool shouldDraw = bounds == null || bounds.Value.Coordinate == 10;
            Assert.That(drawings, Is.EqualTo(shouldDraw ? 1 : 0), "Execute the actual native-shaped branch first.");
            if (shouldDraw) Assert.That(lastRectangle, Is.EqualTo(10));
        }
        var result = NativeChecker(name == nameof(Listing.NativeAlternatives)).Describe(method, PatchProcessor.GetOriginalInstructions(method));
        Assert.That(result.Failure, Is.Null);
        Assert.That(result.Required, Is.Not.Null.And.Not.Empty);
    }

    [TestCase(nameof(Listing.NullCulls), 0, 0, false)]
    [TestCase(nameof(Listing.NullReadsValue), 0, 0, true)]
    [TestCase(nameof(Listing.OtherGetter), 0, 0, false)]
    [TestCase(nameof(Listing.OtherDraw), 0, 1, false)]
    [TestCase(nameof(Listing.NullableAliasBeforeStore), 0, 0, true)]
    [TestCase(nameof(Listing.BranchIntoProducer), 0, 0, true)]
    public void RefusesChangedNullBehaviorAfterExecutingIt(string name, int expectedDraws, int expectedOther, bool throws)
    {
        MethodInfo method = typeof(Listing).GetMethod(name)!;
        var listing = new Listing { Bounds = null };
        drawings = otherDrawings = 0;
        if (throws)
        {
            var error = Assert.Throws<TargetInvocationException>(() => method.Invoke(listing, new object[] { true, false }));
            Assert.That(error!.InnerException, Is.TypeOf<InvalidOperationException>());
        }
        else method.Invoke(listing, new object[] { true, false });
        Assert.That(drawings, Is.EqualTo(expectedDraws));
        Assert.That(otherDrawings, Is.EqualTo(expectedOther));
        Assert.That(NativeChecker().Describe(method, PatchProcessor.GetOriginalInstructions(method)).Failure, Is.Not.Null);
    }

    [Test]
    public void RefusesAnAlternateOverlapOperationAfterObservingChangedCull()
    {
        var listing = new Listing { Bounds = new Box(10) };
        bool selection = false;
        drawings = 0;
        listing.OtherOverlap(false, ref selection);
        Assert.That(drawings, Is.Zero, "The substituted overlap reverses the native decision.");
        MethodInfo method = typeof(Listing).GetMethod(nameof(Listing.OtherOverlap))!;
        Assert.That(NativeChecker().Describe(method, PatchProcessor.GetOriginalInstructions(method)).Failure, Is.Not.Null);
    }

    [Test]
    public void RefusesAnAliasAcquiredBeforeProductionWhichChangesTheDrawnRectangle()
    {
        var listing = new Listing { Bounds = null };
        bool selection = false;
        drawings = 0; lastRectangle = -1;
        listing.RowAliasBeforeStore(false, ref selection);
        Assert.That(drawings, Is.EqualTo(1));
        Assert.That(lastRectangle, Is.EqualTo(999));
        MethodInfo method = typeof(Listing).GetMethod(nameof(Listing.RowAliasBeforeStore))!;
        Assert.That(NativeChecker().Describe(method, PatchProcessor.GetOriginalInstructions(method)).Failure, Is.Not.Null);
    }

    [Test]
    public void RefusesNullValueReentryAfterTheFirstDraw()
    {
        var listing = new Listing { Bounds = null };
        bool selection = false;
        drawings = 0;
        Assert.Throws<InvalidOperationException>(() => listing.PostDrawValue(true, ref selection));
        Assert.That(drawings, Is.EqualTo(1), "The first draw happened before null Value was reached through the backedge.");
        MethodInfo method = typeof(Listing).GetMethod(nameof(Listing.PostDrawValue))!;
        Assert.That(NativeChecker().Describe(method, PatchProcessor.GetOriginalInstructions(method)).Failure, Is.Not.Null);
    }

    [Test]
    public void RefusesTailBlockMutationWhichExecutesBeforeAnEarlierDrawInstruction()
    {
        var listing = new Listing { Bounds = null };
        bool selection = false;
        drawings = 0; lastRectangle = -1;
        listing.TailMutation(false, ref selection);
        Assert.That(drawings, Is.EqualTo(1));
        Assert.That(lastRectangle, Is.EqualTo(999), "The later physical block changed the row before control returned to draw it.");
        MethodInfo method = typeof(Listing).GetMethod(nameof(Listing.TailMutation))!;
        var body = PatchProcessor.GetOriginalInstructions(method);
        int draw = body.FindIndex(instruction => instruction.Calls(DetailedDraw));
        int mutation = body.FindIndex(instruction => instruction.opcode == System.Reflection.Emit.OpCodes.Stfld &&
            Equals(instruction.operand, typeof(Box).GetField(nameof(Box.Coordinate))));
        Assert.That(mutation, Is.GreaterThan(draw), "Retain the actual block-order counterexample, not only a source layout.");
        Assert.That(NativeChecker().Describe(method, body).Failure, Is.Not.Null);
    }

    [TestCase(nameof(Listing.PostDrawObserver), 1)]
    [TestCase(nameof(Listing.PostDrawRecheck), 2)]
    [TestCase(nameof(Listing.TailObserver), 1)]
    public void AllowsBenignPostDrawAndTailControlFlow(string name, int expectedDraws)
    {
        var listing = new Listing { Bounds = null };
        drawings = 0; lastRectangle = -1;
        MethodInfo method = typeof(Listing).GetMethod(name)!;
        method.Invoke(listing, new object[] { true, false });
        Assert.That(drawings, Is.EqualTo(expectedDraws));
        Assert.That(lastRectangle, Is.EqualTo(10));
        var body = PatchProcessor.GetOriginalInstructions(method);
        if (name == nameof(Listing.TailObserver))
            Assert.That(body.FindIndex(instruction => instruction.Calls(typeof(SettingsListingBoundsSemanticsTests).GetMethod(nameof(ObserveNumber))!)),
                Is.GreaterThan(body.FindIndex(instruction => instruction.Calls(DetailedDraw))));
        Assert.That(NativeChecker().Describe(method, body).Failure, Is.Null);
    }

    private static SettingsListingBoundsSemantics NativeChecker(bool alternatives = false) => new(new(Getter, HasValue, Value, Overlaps,
        alternatives ? new[] { new SettingsListingBoundsSemantics.Draw(DetailedDraw), new SettingsListingBoundsSemantics.Draw(Draw) }
            : new[] { new SettingsListingBoundsSemantics.Draw(DetailedDraw) }));

    public struct Box
    {
        public int Coordinate;
        public Box(int coordinate) => Coordinate = coordinate;
        [MethodImpl(MethodImplOptions.NoInlining)] public bool Overlaps(Box other) => Coordinate == other.Coordinate;
        [MethodImpl(MethodImplOptions.NoInlining)] public bool OtherOverlaps(Box other) => Coordinate != other.Coordinate;
    }
    public sealed class Listing
    {
        public Box? Bounds { [MethodImpl(MethodImplOptions.NoInlining)] get; set; }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void EarlyReturn(Box row)
        {
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawBox(row);
        }

        public Box? AlternateBounds => new Box(20);
        [MethodImpl(MethodImplOptions.NoInlining)] public Box GetRow() => new(10);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void NativeDetailed(bool flag, ref bool selected)
        {
            Box row = GetRow();
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawDetailed(row, "actual row", ref selected);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void NativeBlock(bool flag, ref bool selected)
        {
            Box row = GetRow();
            if (!Bounds.HasValue || row.Overlaps(Bounds.Value)) DrawDetailed(row, "actual row", ref selected);
            ObserveNumber(5);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void NativeAlternatives(bool flag, ref bool selected)
        {
            Box row = GetRow();
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            if (flag) DrawDetailed(row, "long label", ref selected);
            else DrawBox(row);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void BenignAlias(bool flag, ref bool selected)
        {
            Box row = default;
            ref Box alias = ref row;
            row = GetRow();
            ObserveNumber(alias.Coordinate);
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawDetailed(row, "actual row", ref selected);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void BranchToProducerStart(bool flag, ref bool selected)
        {
            Box row = GetRow();
            if (flag) goto Read;
            ObserveNumber(4);
        Read:
            Box? bounds = Bounds;
            if (bounds.HasValue && !row.Overlaps(Bounds!.Value)) return;
            DrawDetailed(row, "actual row", ref selected);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void NullCulls(bool flag, ref bool selected)
        {
            Box row = GetRow();
            if (!Bounds.HasValue || !row.Overlaps(Bounds.Value)) return;
            DrawDetailed(row, "changed row", ref selected);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void NullReadsValue(bool flag, ref bool selected)
        {
            Box row = GetRow();
            // Deliberately execute the invalid Value path; the test observes the native throw.
            if (!Bounds.HasValue && !row.Overlaps(Bounds!.Value)) return;
            DrawDetailed(row, "changed row", ref selected);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void OtherGetter(bool flag, ref bool selected)
        {
            Box row = GetRow();
            if (AlternateBounds.HasValue && !row.Overlaps(AlternateBounds.Value)) return;
            DrawDetailed(row, "changed row", ref selected);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void OtherOverlap(bool flag, ref bool selected)
        {
            Box row = GetRow();
            if (Bounds.HasValue && !row.OtherOverlaps(Bounds.Value)) return;
            DrawDetailed(row, "changed row", ref selected);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void OtherDraw(bool flag, ref bool selected)
        {
            Box row = GetRow();
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawOther(row, "changed row", ref selected);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void RowAliasBeforeStore(bool flag, ref bool selected)
        {
            Box row = default;
            ref Box alias = ref row;
            row = GetRow();
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            alias.Coordinate = 999;
            DrawDetailed(row, "changed row", ref selected);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void NullableAliasBeforeStore(bool flag, ref bool selected)
        {
            Box row = GetRow();
            Box? bounds = default;
            ref Box? alias = ref bounds;
            bounds = Bounds;
            alias = new Box(999);
            if (bounds.HasValue && !row.Overlaps(Bounds!.Value)) return;
            DrawDetailed(row, "changed row", ref selected);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void BranchIntoProducer(bool flag, ref bool selected)
        {
            Box row = GetRow();
            Box? bounds;
            if (flag) { bounds = new Box(999); goto Test; }
            bounds = Bounds;
        Test:
            if (bounds.HasValue && !row.Overlaps(Bounds!.Value)) return;
            DrawDetailed(row, "changed row", ref selected);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void PostDrawValue(bool flag, ref bool selected)
        {
            Box row = GetRow();
            Box? bounds = Bounds;
            if (!bounds.HasValue) goto Draw;
        Overlap:
            if (!row.Overlaps(Bounds!.Value)) return;
        Draw:
            DrawDetailed(row, "first draw", ref selected);
            if (flag) { flag = false; goto Overlap; }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void TailMutation(bool flag, ref bool selected)
        {
            Box row = GetRow();
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            goto Replace;
        Draw:
            DrawDetailed(row, "changed row", ref selected);
            return;
        Replace:
            row.Coordinate = 999;
            goto Draw;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void PostDrawObserver(bool flag, ref bool selected)
        {
            Box row = GetRow();
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawDetailed(row, "actual row", ref selected);
            ObserveNumber(7);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void PostDrawRecheck(bool flag, ref bool selected)
        {
            Box row = GetRow();
        Read:
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawDetailed(row, "actual row", ref selected);
            if (flag) { flag = false; goto Read; }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void TailObserver(bool flag, ref bool selected)
        {
            Box row = GetRow();
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            goto Observe;
        Draw:
            DrawDetailed(row, "actual row", ref selected);
            return;
        Observe:
            ObserveNumber(5);
            goto Draw;
        }
    }
    [MethodImpl(MethodImplOptions.NoInlining)] public static void DrawBox(Box row) { drawings++; lastRectangle = row.Coordinate; }
    [MethodImpl(MethodImplOptions.NoInlining)] public static void DrawDetailed(Box row, string label, ref bool selected)
    { drawings++; lastRectangle = row.Coordinate; selected = true; }
    [MethodImpl(MethodImplOptions.NoInlining)] public static void DrawOther(Box row, string label, ref bool selected) => otherDrawings++;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void ObserveNumber(int value) => observed = value;
}
