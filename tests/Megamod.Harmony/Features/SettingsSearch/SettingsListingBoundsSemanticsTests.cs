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
    private static readonly List<string> projectionDraws = new();
    private static Type? genericDrawType;
    private static readonly MethodInfo ProjectedLabel = typeof(SettingsListingBoundsSemanticsTests).GetMethod(nameof(DrawProjectedLabel))!;
    private static readonly MethodInfo ProjectedButton = typeof(SettingsListingBoundsSemanticsTests).GetMethod(nameof(DrawProjectedButton))!;

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

    [TestCase(nameof(Listing.NativeHalves), false, "L101h10,L102h10")]
    [TestCase(nameof(Listing.NativeParts), false, "L1025h10,B2075h30")]
    [TestCase(nameof(Listing.ProjectionStartBranch), false, "L1025h10,B2075h30")]
    [TestCase(nameof(Listing.ProjectionStartBranch), true, "L1025h10,B2075h30")]
    public void ProvesEverySequentialDrawAndItsExactRectangleProducer(string name, bool flag, string expected)
    {
        MethodInfo method = typeof(Listing).GetMethod(name)!;
        foreach (Box? bounds in new Box?[] { null, new Box(20), new Box(10) })
        {
            projectionDraws.Clear();
            method.Invoke(new Listing { Bounds = bounds }, new object[] { flag, 0.25f });
            Assert.That(string.Join(",", projectionDraws), Is.EqualTo(bounds?.Coordinate == 20 ? "" : expected),
                "Execute the actual projected sequence before asking for admission.");
        }
        var result = ProjectionChecker(name == nameof(Listing.NativeHalves)).Describe(method, PatchProcessor.GetOriginalInstructions(method));
        Assert.That(result.Failure, Is.Null);
        Assert.That(result.Required, Is.Not.Null.And.Not.Empty);
    }

    [TestCase(nameof(Listing.HalvesMissing), "L101h10")]
    [TestCase(nameof(Listing.HalvesReordered), "L102h10,L101h10")]
    [TestCase(nameof(Listing.HalvesSubstituted), "L999h10,L102h10")]
    [TestCase(nameof(Listing.HalvesSkipSecond), "L101h10")]
    public void RefusesChangedSequentialDrawsAfterExecutingThem(string name, string expected)
    {
        MethodInfo method = typeof(Listing).GetMethod(name)!;
        projectionDraws.Clear();
        method.Invoke(new Listing { Bounds = null }, new object[] { true, 0.25f });
        Assert.That(string.Join(",", projectionDraws), Is.EqualTo(expected));
        Assert.That(ProjectionChecker(true).Describe(method, PatchProcessor.GetOriginalInstructions(method)).Failure, Is.Not.Null);
    }

    [TestCase(nameof(Listing.PartsWrongFraction), "L1025h10,B2025h30")]
    [TestCase(nameof(Listing.PartsWrongHeight), "L1025h10,B2075h40")]
    [TestCase(nameof(Listing.PartsAliasMutation), "L1025h10,B999h30")]
    [TestCase(nameof(Listing.PartsProducerBypass), "L1025h10,B999h10")]
    [TestCase(nameof(Listing.PartsRowReplacedBeforeCull), "L2025h10,B2075h30")]
    [TestCase(nameof(Listing.PartsSubstituted), "L1025h10,B999h30")]
    public void RefusesChangedStoredProjectionProvenanceAfterExecutingIt(string name, string expected)
    {
        MethodInfo method = typeof(Listing).GetMethod(name)!;
        projectionDraws.Clear();
        method.Invoke(new Listing { Bounds = null }, new object[] { true, 0.25f });
        Assert.That(string.Join(",", projectionDraws), Is.EqualTo(expected));
        Assert.That(ProjectionChecker(false).Describe(method, PatchProcessor.GetOriginalInstructions(method)).Failure, Is.Not.Null);
    }

    [Test]
    public void RefusesARepeatedFinalDrawBeforeAFreshCullingGate()
    {
        projectionDraws.Clear();
        new Listing { Bounds = null }.HalvesRepeatLast(true, 0.25f);
        Assert.That(string.Join(",", projectionDraws), Is.EqualTo("L101h10,L102h10,L102h10"));
        MethodInfo method = typeof(Listing).GetMethod(nameof(Listing.HalvesRepeatLast))!;
        Assert.That(ProjectionChecker(true).Describe(method, PatchProcessor.GetOriginalInstructions(method)).Failure, Is.Not.Null);
    }

    [Test]
    public void AllowsTheCompleteDrawSequenceAfterAFreshIntactCullingGate()
    {
        projectionDraws.Clear();
        new Listing { Bounds = null }.HalvesFreshGate(true, 0.25f);
        Assert.That(string.Join(",", projectionDraws), Is.EqualTo("L101h10,L102h10,L101h10,L102h10"));
        MethodInfo method = typeof(Listing).GetMethod(nameof(Listing.HalvesFreshGate))!;
        Assert.That(ProjectionChecker(true).Describe(method, PatchProcessor.GetOriginalInstructions(method)).Failure, Is.Null);
    }

    [TestCase(nameof(Listing.PartsScalarChanged))]
    [TestCase(nameof(Listing.PartsScalarAliasChanged))]
    public void RefusesASharedProjectionScalarChangedBetweenItsReads(string name)
    {
        projectionDraws.Clear();
        MethodInfo method = typeof(Listing).GetMethod(name)!;
        method.Invoke(new Listing { Bounds = null }, new object[] { true, 0.25f });
        Assert.That(string.Join(",", projectionDraws), Is.EqualTo("L1050h10,B2075h30"));
        Assert.That(ProjectionChecker(false).Describe(method, PatchProcessor.GetOriginalInstructions(method)).Failure, Is.Not.Null);
    }

    [TestCase(nameof(Listing.PartsScalarChangedAfterUse), 50)]
    [TestCase(nameof(Listing.PartsScalarAliasChangedAfterUse), 50)]
    [TestCase(nameof(Listing.PartsScalarAliasRead), 25)]
    public void AllowsScalarChangesAfterItsFinalProjectionAndHarmlessAliasReads(string name, int expectedObserved)
    {
        projectionDraws.Clear(); observed = -1;
        MethodInfo method = typeof(Listing).GetMethod(name)!;
        method.Invoke(new Listing { Bounds = null }, new object[] { true, 0.25f });
        Assert.That(string.Join(",", projectionDraws), Is.EqualTo("L1025h10,B2075h30"));
        Assert.That(observed, Is.EqualTo(expectedObserved));
        Assert.That(ProjectionChecker(false).Describe(method, PatchProcessor.GetOriginalInstructions(method)).Failure, Is.Null);
    }

    [Test]
    public void AllowsAChangedScalarAfterCompletingAndRebuildingTheProjectionCycle()
    {
        projectionDraws.Clear();
        new Listing { Bounds = null }.PartsFreshProjectionCycle(true, 0.25f);
        Assert.That(string.Join(",", projectionDraws), Is.EqualTo("L1025h10,B2075h30,L1050h10,B2050h30"));
        MethodInfo method = typeof(Listing).GetMethod(nameof(Listing.PartsFreshProjectionCycle))!;
        Assert.That(ProjectionChecker(false).Describe(method, PatchProcessor.GetOriginalInstructions(method)).Failure, Is.Null);
    }

    [Test]
    public void RefusesAChangedScalarWhenOnlyOneStoredProjectionIsRebuilt()
    {
        projectionDraws.Clear();
        new Listing { Bounds = null }.PartsPartialProjectionCycle(true, 0.25f);
        Assert.That(string.Join(",", projectionDraws), Is.EqualTo("L1025h10,B2075h30,L1050h10,B2075h30"));
        MethodInfo method = typeof(Listing).GetMethod(nameof(Listing.PartsPartialProjectionCycle))!;
        Assert.That(ProjectionChecker(false).Describe(method, PatchProcessor.GetOriginalInstructions(method)).Failure, Is.Not.Null);
    }

    [TestCase(nameof(Listing.PartsPartialRightCycle), "L1025h10,B2075h30,L1025h10,B2050h30")]
    [TestCase(nameof(Listing.PartsPartialAliasCycle), "L1025h10,B2075h30,L1050h10,B2075h30")]
    public void RefusesSymmetricAndAliasedPartialProjectionRebuilds(string name, string expected)
    {
        projectionDraws.Clear();
        MethodInfo method = typeof(Listing).GetMethod(name)!;
        method.Invoke(new Listing { Bounds = null }, new object[] { true, 0.25f });
        Assert.That(string.Join(",", projectionDraws), Is.EqualTo(expected));
        Assert.That(ProjectionChecker(false).Describe(method, PatchProcessor.GetOriginalInstructions(method)).Failure, Is.Not.Null);
    }

    [Test]
    public void AllowsKnownScalarPreparationBeforeCulling()
    {
        MethodInfo method = typeof(Listing).GetMethod(nameof(Listing.ScalarBeforeCull))!;
        foreach (Box? bounds in new Box?[] { null, new Box(20), new Box(15) })
        {
            bool selected = false;
            drawings = 0; lastRectangle = -1;
            new Listing { Bounds = bounds }.ScalarBeforeCull(false, ref selected);
            Assert.That(drawings, Is.EqualTo(bounds?.Coordinate == 20 ? 0 : 1));
            if (drawings != 0) Assert.That(lastRectangle, Is.EqualTo(15));
        }
        Assert.That(PreparedChecker().Describe(method, PatchProcessor.GetOriginalInstructions(method)).Failure, Is.Null);
    }

    [Test]
    public void RefusesTheSameScalarSetterBetweenCullingAndDrawing()
    {
        bool selected = false;
        drawings = 0; lastRectangle = -1;
        new Listing { Bounds = new Box(10) }.ScalarAfterCull(false, ref selected);
        Assert.That(drawings, Is.EqualTo(1));
        Assert.That(lastRectangle, Is.EqualTo(15));
        MethodInfo method = typeof(Listing).GetMethod(nameof(Listing.ScalarAfterCull))!;
        Assert.That(PreparedChecker().Describe(method, PatchProcessor.GetOriginalInstructions(method)).Failure, Is.Not.Null);
    }

    [Test]
    public void ProvesTheNativeInPlaceRectangleConstructorBeforeCulling()
    {
        MethodInfo method = typeof(Listing).GetMethod(nameof(Listing.ConstructedBeforeCull))!;
        foreach (Box? bounds in new Box?[] { null, new Box(20), new Box(10) })
        {
            bool selected = false;
            drawings = 0; lastRectangle = -1;
            new Listing { Bounds = bounds }.ConstructedBeforeCull(false, ref selected);
            Assert.That(drawings, Is.EqualTo(bounds?.Coordinate == 20 ? 0 : 1));
            if (drawings != 0) Assert.That(lastRectangle, Is.EqualTo(10));
        }
        var body = PatchProcessor.GetOriginalInstructions(method);
        int constructor = body.FindIndex(instruction => instruction.opcode == System.Reflection.Emit.OpCodes.Call &&
            Equals(instruction.operand, RectangleConstructor()));
        Assert.That(constructor, Is.GreaterThanOrEqualTo(5), "The owned fixture must retain native in-place constructor consumption.");
        Assert.That(body[constructor - 5].opcode, Is.EqualTo(System.Reflection.Emit.OpCodes.Ldloca_S).Or.EqualTo(System.Reflection.Emit.OpCodes.Ldloca));
        Assert.That(ConstructorChecker().Describe(method, body).Failure, Is.Null);
    }

    [Test]
    public void RefusesConstructorReplacementBetweenCullingAndDrawing()
    {
        bool selected = false;
        drawings = 0; lastRectangle = -1;
        new Listing { Bounds = new Box(10) }.ConstructedAfterCull(false, ref selected);
        Assert.That(drawings, Is.EqualTo(1));
        Assert.That(lastRectangle, Is.EqualTo(999));
        MethodInfo method = typeof(Listing).GetMethod(nameof(Listing.ConstructedAfterCull))!;
        Assert.That(ConstructorChecker().Describe(method, PatchProcessor.GetOriginalInstructions(method)).Failure, Is.Not.Null);
    }

    [TestCase(nameof(Listing.GenericNumeric), typeof(long))]
    [TestCase(nameof(Listing.GenericNumeric), typeof(OwnedNumber))]
    [TestCase(nameof(Listing.GenericNumericLabeled), typeof(OwnedNumber))]
    public void PreservesTheConsumerGenericSymbolAndEachInspectedClosure(string name, Type argument)
    {
        MethodInfo definition = typeof(Listing).GetMethod(name)!;
        MethodInfo closed = definition.MakeGenericMethod(argument);
        foreach (Box? bounds in new Box?[] { null, new Box(20), new Box(10) })
        {
            genericDrawType = null;
            closed.Invoke(new Listing { Bounds = bounds }, new[] { Activator.CreateInstance(argument), "buffer", (object)0f, 100f });
            Assert.That(genericDrawType, Is.EqualTo(bounds?.Coordinate == 20 ? null : argument));
        }
        var checker = GenericChecker(name == nameof(Listing.GenericNumericLabeled));
        Assert.That(checker.Describe(definition, GenericInstructions(definition)).Failure, Is.Null);
        Assert.That(checker.Describe(closed, GenericInstructions(closed)).Failure, Is.Null);
    }

    [TestCase(nameof(Listing.GenericFixedType))]
    [TestCase(nameof(Listing.GenericDifferentSymbol))]
    public void RefusesSubstitutedGenericArgumentsAfterExecutingTheDifferentType(string name)
    {
        MethodInfo definition = typeof(Listing).GetMethod(name)!;
        Type[] arguments = definition.GetGenericArguments().Length == 1 ? new[] { typeof(OwnedNumber) } : new[] { typeof(OwnedNumber), typeof(long) };
        MethodInfo closed = definition.MakeGenericMethod(arguments);
        genericDrawType = null;
        closed.Invoke(new Listing { Bounds = null }, new object[] { new OwnedNumber(42), "buffer", 0f, 100f });
        Assert.That(genericDrawType, Is.EqualTo(typeof(long)));
        Assert.That(GenericChecker(false).Describe(definition, GenericInstructions(definition)).Failure, Is.Not.Null);
        Assert.That(GenericChecker(false).Describe(closed, GenericInstructions(closed)).Failure, Is.Not.Null);
    }

    [Test]
    public void RefusesAnUnboundGenericArgumentRelationship()
    {
        MethodInfo definition = typeof(Listing).GetMethod(nameof(Listing.GenericNumeric))!;
        long number = 42;
        string buffer = "buffer";
        genericDrawType = null;
        new Listing { Bounds = null }.GenericNumeric(ref number, ref buffer, 0f, 100f);
        Assert.That(genericDrawType, Is.EqualTo(typeof(long)));
        Assert.That(GenericChecker(false, false).Describe(definition, GenericInstructions(definition)).Failure, Is.Not.Null);
    }

    private static List<CodeInstruction> GenericInstructions(MethodInfo target)
    {
        // Host fixture extraction only: Harmony's convenience generator tries importing the
        // open method. These instructions need labels/locals, not an invocable wrapper.
        var scratch = new System.Reflection.Emit.DynamicMethod("listing_fixture_instructions", typeof(void), Type.EmptyTypes,
            typeof(SettingsListingBoundsSemanticsTests).Module, true);
        return PatchProcessor.GetOriginalInstructions(target, scratch.GetILGenerator());
    }

    private static MethodInfo Helper(string name) => typeof(SettingsListingBoundsSemanticsTests).GetMethod(name)!;
    private static MethodInfo[] RectangleAccessors() => typeof(Box).GetProperties().SelectMany(property => new[] { property.GetMethod!, property.SetMethod! }).ToArray();
    private static SettingsListingBoundsSemantics PreparedChecker() => new(new(Getter, HasValue, Value, Overlaps,
        new[] { new SettingsListingBoundsSemantics.Draw(DetailedDraw) }, RectangleAccessors()));
    private static ConstructorInfo RectangleConstructor() => typeof(Box).GetConstructor(new[] { typeof(float), typeof(float), typeof(float), typeof(float) })!;
    private static SettingsListingBoundsSemantics ConstructorChecker() => new(new(Getter, HasValue, Value, Overlaps,
        new[] { new SettingsListingBoundsSemantics.Draw(DetailedDraw) }, RectangleConstructors: new[] { RectangleConstructor() }));
    private static SettingsListingBoundsSemantics ProjectionChecker(bool halves)
    {
        var left = new SettingsListingBoundsSemantics.RectangleProjection(Helper(halves ? nameof(LeftHalf) : nameof(LeftPart)),
            halves ? null : new(1));
        var right = new SettingsListingBoundsSemantics.RectangleProjection(Helper(halves ? nameof(RightHalf) : nameof(RightPart)),
            halves ? null : new(1, true), halves ? null : new(typeof(Box).GetProperty(nameof(Box.Height))!.SetMethod!, 30f));
        return new(new(Getter, HasValue, Value, Overlaps,
            new[] { new SettingsListingBoundsSemantics.Draw(ProjectedLabel, Projection: left),
                new SettingsListingBoundsSemantics.Draw(halves ? ProjectedLabel : ProjectedButton, Sequence: 1, Projection: right) }, RectangleAccessors()));
    }
    private static SettingsListingBoundsSemantics GenericChecker(bool labeled, bool mapping = true) => new(new(Getter, HasValue, Value, Overlaps,
        new[] { new SettingsListingBoundsSemantics.Draw(Helper(labeled ? nameof(DrawGenericLabeled) : nameof(DrawGeneric)),
            ConsumerGenericArguments: mapping ? new[] { 0 } : null) }));

    public readonly record struct OwnedNumber(int Value);

    public struct Box
    {
        public int Coordinate;
        private float height;
        public float Height { [MethodImpl(MethodImplOptions.NoInlining)] get => height; [MethodImpl(MethodImplOptions.NoInlining)] set => height = value; }
        public float Offset { [MethodImpl(MethodImplOptions.NoInlining)] get => Coordinate; [MethodImpl(MethodImplOptions.NoInlining)] set => Coordinate = (int)value; }
        public Box(int coordinate) { Coordinate = coordinate; height = 10f; }
        public Box(float x, float y, float width, float height) { Coordinate = (int)x; this.height = height; }
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

        [MethodImpl(MethodImplOptions.NoInlining)]
        public void NativeHalves(bool flag, float fraction)
        {
            Box row = GetRow();
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawProjectedLabel(LeftHalf(row), "left");
            DrawProjectedLabel(RightHalf(row), "right");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void NativeParts(bool flag, float fraction)
        {
            Box row = GetRow();
            Box right = RightPart(row, 1f - fraction);
            right.Height = 30f;
            ObserveNumber(7);
            Box left = LeftPart(row, fraction);
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawProjectedLabel(left, "left");
            DrawProjectedButton(right, NumberLabel(right.Height));
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ProjectionStartBranch(bool flag, float fraction)
        {
            Box row = GetRow();
            if (flag) goto Project;
            ObserveNumber(7);
        Project:
            Box right = RightPart(row, 1f - fraction);
            right.Height = 30f;
            Box left = LeftPart(row, fraction);
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawProjectedLabel(left, "left");
            DrawProjectedButton(right, NumberLabel(right.Height));
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void HalvesMissing(bool flag, float fraction)
        {
            Box row = GetRow();
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawProjectedLabel(LeftHalf(row), "left");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void HalvesReordered(bool flag, float fraction)
        {
            Box row = GetRow();
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawProjectedLabel(RightHalf(row), "right");
            DrawProjectedLabel(LeftHalf(row), "left");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void HalvesSubstituted(bool flag, float fraction)
        {
            Box row = GetRow();
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawProjectedLabel(OtherHalf(row), "left");
            DrawProjectedLabel(RightHalf(row), "right");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void HalvesSkipSecond(bool flag, float fraction)
        {
            Box row = GetRow();
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawProjectedLabel(LeftHalf(row), "left");
            if (flag) return;
            DrawProjectedLabel(RightHalf(row), "right");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void HalvesRepeatLast(bool flag, float fraction)
        {
            Box row = GetRow();
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawProjectedLabel(LeftHalf(row), "left");
        Repeat:
            DrawProjectedLabel(RightHalf(row), "right");
            if (flag) { flag = false; goto Repeat; }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void HalvesFreshGate(bool flag, float fraction)
        {
            Box row = GetRow();
        Read:
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawProjectedLabel(LeftHalf(row), "left");
            DrawProjectedLabel(RightHalf(row), "right");
            if (flag) { flag = false; goto Read; }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void PartsScalarChanged(bool flag, float fraction)
        {
            Box row = GetRow();
            Box right = RightPart(row, 1f - fraction);
            right.Height = 30f;
            fraction = 0.5f;
            Box left = LeftPart(row, fraction);
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawProjectedLabel(left, "left");
            DrawProjectedButton(right, "right");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void PartsScalarAliasChanged(bool flag, float fraction)
        {
            ref float alias = ref fraction;
            Box row = GetRow();
            Box right = RightPart(row, 1f - fraction);
            right.Height = 30f;
            alias = 0.5f;
            Box left = LeftPart(row, fraction);
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawProjectedLabel(left, "left");
            DrawProjectedButton(right, "right");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void PartsScalarChangedAfterUse(bool flag, float fraction)
        {
            Box row = GetRow();
            Box right = RightPart(row, 1f - fraction);
            right.Height = 30f;
            Box left = LeftPart(row, fraction);
            fraction = 0.5f;
            ObserveNumber((int)(fraction * 100f));
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawProjectedLabel(left, "left");
            DrawProjectedButton(right, "right");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void PartsScalarAliasChangedAfterUse(bool flag, float fraction)
        {
            ref float alias = ref fraction;
            Box row = GetRow();
            Box right = RightPart(row, 1f - fraction);
            right.Height = 30f;
            Box left = LeftPart(row, fraction);
            alias = 0.5f;
            ObserveNumber((int)(fraction * 100f));
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawProjectedLabel(left, "left");
            DrawProjectedButton(right, "right");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void PartsScalarAliasRead(bool flag, float fraction)
        {
            ref float alias = ref fraction;
            ObserveNumber((int)(alias * 100f));
            Box row = GetRow();
            Box right = RightPart(row, 1f - fraction);
            right.Height = 30f;
            Box left = LeftPart(row, fraction);
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawProjectedLabel(left, "left");
            DrawProjectedButton(right, "right");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void PartsFreshProjectionCycle(bool flag, float fraction)
        {
            Box row = GetRow();
        Project:
            Box right = RightPart(row, 1f - fraction);
            right.Height = 30f;
            Box left = LeftPart(row, fraction);
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawProjectedLabel(left, "left");
            DrawProjectedButton(right, "right");
            if (flag) { flag = false; fraction = 0.5f; goto Project; }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void PartsPartialProjectionCycle(bool flag, float fraction)
        {
            Box row = GetRow();
            Box right = RightPart(row, 1f - fraction);
            right.Height = 30f;
        ProjectLeft:
            Box left = LeftPart(row, fraction);
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawProjectedLabel(left, "left");
            DrawProjectedButton(right, "right");
            if (flag) { flag = false; fraction = 0.5f; goto ProjectLeft; }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void PartsPartialRightCycle(bool flag, float fraction)
        {
            Box row = GetRow();
            Box left = LeftPart(row, fraction);
        ProjectRight:
            Box right = RightPart(row, 1f - fraction);
            right.Height = 30f;
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawProjectedLabel(left, "left");
            DrawProjectedButton(right, "right");
            if (flag) { flag = false; fraction = 0.5f; goto ProjectRight; }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void PartsPartialAliasCycle(bool flag, float fraction)
        {
            ref float alias = ref fraction;
            Box row = GetRow();
            Box right = RightPart(row, 1f - fraction);
            right.Height = 30f;
        ProjectLeft:
            Box left = LeftPart(row, fraction);
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawProjectedLabel(left, "left");
            DrawProjectedButton(right, "right");
            if (flag) { flag = false; alias = 0.5f; goto ProjectLeft; }
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void PartsWrongFraction(bool flag, float fraction)
        {
            Box row = GetRow();
            Box right = RightPart(row, fraction);
            right.Height = 30f;
            Box left = LeftPart(row, fraction);
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawProjectedLabel(left, "left");
            DrawProjectedButton(right, "right");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void PartsWrongHeight(bool flag, float fraction)
        {
            Box row = GetRow();
            Box right = RightPart(row, 1f - fraction);
            right.Height = 40f;
            Box left = LeftPart(row, fraction);
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawProjectedLabel(left, "left");
            DrawProjectedButton(right, "right");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void PartsAliasMutation(bool flag, float fraction)
        {
            Box row = GetRow();
            Box right = default;
            ref Box alias = ref right;
            right = RightPart(row, 1f - fraction);
            right.Height = 30f;
            Box left = LeftPart(row, fraction);
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawProjectedLabel(left, "left");
            alias.Coordinate = 999;
            DrawProjectedButton(right, "right");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void PartsProducerBypass(bool flag, float fraction)
        {
            Box row = GetRow();
            Box right;
            if (flag) { right = new Box(999); goto Read; }
            right = RightPart(row, 1f - fraction);
            right.Height = 30f;
        Read:
            Box left = LeftPart(row, fraction);
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawProjectedLabel(left, "left");
            DrawProjectedButton(right, "right");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void PartsRowReplacedBeforeCull(bool flag, float fraction)
        {
            Box row = GetRow();
            Box right = RightPart(row, 1f - fraction);
            right.Height = 30f;
            row.Offset = 20f;
            Box left = LeftPart(row, fraction);
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawProjectedLabel(left, "left");
            DrawProjectedButton(right, "right");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void PartsSubstituted(bool flag, float fraction)
        {
            Box row = GetRow();
            Box right = OtherPart(row, 1f - fraction);
            right.Height = 30f;
            Box left = LeftPart(row, fraction);
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawProjectedLabel(left, "left");
            DrawProjectedButton(right, "right");
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ScalarBeforeCull(bool flag, ref bool selected)
        {
            Box row = GetRow();
            row.Offset += 5f;
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawDetailed(row, "prepared row", ref selected);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ScalarAfterCull(bool flag, ref bool selected)
        {
            Box row = GetRow();
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            row.Offset += 5f;
            DrawDetailed(row, "changed row", ref selected);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ConstructedBeforeCull(bool flag, ref bool selected)
        {
            Box row = new(10f, 0f, 20f, 10f);
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawDetailed(row, "constructed row", ref selected);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void ConstructedAfterCull(bool flag, ref bool selected)
        {
            Box row = GetRow();
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            row = new Box(999f, 0f, 20f, 10f);
            DrawDetailed(row, "changed row", ref selected);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void GenericNumeric<T>(ref T value, ref string buffer, float min, float max) where T : struct
        {
            Box row = GetRow();
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawGeneric(row, ref value, ref buffer, min, max);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void GenericNumericLabeled<T>(ref T value, ref string buffer, float min, float max) where T : struct
        {
            Box row = GetRow();
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            DrawGenericLabeled(row, "numeric", ref value, ref buffer, min, max);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void GenericFixedType<T>(ref T value, ref string buffer, float min, float max) where T : struct
        {
            Box row = GetRow();
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            long replacement = 42;
            DrawGeneric(row, ref replacement, ref buffer, min, max);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void GenericDifferentSymbol<T, TOther>(ref T value, ref string buffer, float min, float max)
            where T : struct where TOther : struct
        {
            Box row = GetRow();
            if (Bounds.HasValue && !row.Overlaps(Bounds.Value)) return;
            TOther replacement = default;
            DrawGeneric(row, ref replacement, ref buffer, min, max);
        }
    }
    [MethodImpl(MethodImplOptions.NoInlining)] public static void DrawBox(Box row) { drawings++; lastRectangle = row.Coordinate; }
    [MethodImpl(MethodImplOptions.NoInlining)] public static void DrawDetailed(Box row, string label, ref bool selected)
    { drawings++; lastRectangle = row.Coordinate; selected = true; }
    [MethodImpl(MethodImplOptions.NoInlining)] public static void DrawOther(Box row, string label, ref bool selected) => otherDrawings++;
    [MethodImpl(MethodImplOptions.NoInlining)] public static void ObserveNumber(int value) => observed = value;
    [MethodImpl(MethodImplOptions.NoInlining)] public static Box LeftHalf(Box row) => new(row.Coordinate * 10 + 1);
    [MethodImpl(MethodImplOptions.NoInlining)] public static Box RightHalf(Box row) => new(row.Coordinate * 10 + 2);
    [MethodImpl(MethodImplOptions.NoInlining)] public static Box OtherHalf(Box row) => new(999);
    [MethodImpl(MethodImplOptions.NoInlining)] public static Box LeftPart(Box row, float fraction) => new((int)(row.Coordinate * 100 + fraction * 100));
    [MethodImpl(MethodImplOptions.NoInlining)] public static Box RightPart(Box row, float fraction) => new((int)(row.Coordinate * 200 + fraction * 100));
    [MethodImpl(MethodImplOptions.NoInlining)] public static Box OtherPart(Box row, float fraction) => new(999);
    [MethodImpl(MethodImplOptions.NoInlining)] public static string NumberLabel(float value) => value.ToString(System.Globalization.CultureInfo.InvariantCulture);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void DrawProjectedLabel(Box row, string label) => projectionDraws.Add($"L{row.Coordinate}h{(int)row.Height}");
    [MethodImpl(MethodImplOptions.NoInlining)] public static bool DrawProjectedButton(Box row, string label)
    { projectionDraws.Add($"B{row.Coordinate}h{(int)row.Height}"); return true; }
    [MethodImpl(MethodImplOptions.NoInlining)] public static void DrawGeneric<T>(Box row, ref T value, ref string buffer, float min, float max) where T : struct
        => genericDrawType = typeof(T);
    [MethodImpl(MethodImplOptions.NoInlining)] public static void DrawGenericLabeled<T>(Box row, string label, ref T value, ref string buffer, float min, float max) where T : struct
        => genericDrawType = typeof(T);
}
