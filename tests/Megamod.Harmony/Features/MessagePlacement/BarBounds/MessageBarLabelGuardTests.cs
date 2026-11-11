using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Megamod.Features.MessagePlacement;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.MessagePlacement.BarBounds;

[TestFixture, NonParallelizable]
public sealed class MessageBarLabelGuardTests
{
    private static readonly Dictionary<string, MemberInfo> Members = new()
    {
        ["bar"] = typeof(Fixture).GetMethod(nameof(Fixture.Bar))!,
        ["scale"] = typeof(Fixture).GetMethod(nameof(Fixture.Scale))!,
        ["space"] = typeof(Fixture).GetMethod(nameof(Fixture.Space))!,
        ["center"] = typeof(Rectangle).GetProperty(nameof(Rectangle.Center))!.GetMethod!,
        ["bottom"] = typeof(Rectangle).GetProperty(nameof(Rectangle.Bottom))!.GetMethod!,
        ["width"] = typeof(Rectangle).GetProperty(nameof(Rectangle.Width))!.GetMethod!,
        ["x"] = typeof(Vector).GetField(nameof(Vector.X))!,
        ["vector"] = typeof(Vector).GetConstructor(new[] { typeof(float), typeof(float) })!,
        ["cache"] = typeof(Fixture).GetField(nameof(Fixture.Cache))!,
        ["positionLabel"] = typeof(Fixture).GetMethod(nameof(Fixture.Label))!
    };

    [Test]
    public void LabelOriginAndTruncationUseCurrentPortraitAndNativeScale()
    {
        var body = Caller();
        var result = new MessageBarLabelGuard(new(Members)).Describe(MessageBarLabelStage.Colonist, body);
        Assert.That(result.Failure, Is.Null);
        Assert.That(result.Required, Does.Contain(body.Count - 1));
        body[0].operand = 40f;
        Assert.That(new MessageBarLabelGuard(new(Members)).Describe(MessageBarLabelStage.Colonist, body).Failure, Is.Not.Null);
    }

    [Test]
    public void BenignInstructionsAndRenumberedLocalsPreserveTheSameLabelProjection()
    {
        var body = Caller();
        body.Insert(10, new(OpCodes.Nop));
        body.InsertRange(0, new[] { new CodeInstruction(OpCodes.Ldc_I4_3), new CodeInstruction(OpCodes.Pop) });
        Assert.That(new MessageBarLabelGuard(new(Members)).Describe(MessageBarLabelStage.Colonist, body).Failure, Is.Null);
    }

    [Test]
    public void NativeAlphaInitializationBeforeTheLabelProjectionIsNotALabelGeometryOverwrite()
    {
        var body = Caller();
        body.InsertRange(0, new[] { new CodeInstruction(OpCodes.Ldc_R4, 1f), new CodeInstruction(OpCodes.Stloc, 9) });
        Assert.That(new MessageBarLabelGuard(new(Members)).Describe(MessageBarLabelStage.Colonist, body).Failure, Is.Null);
    }

    [Test]
    public void LabelBackgroundUsesItsCurrentTinyTextPreferenceAndSuppliedPosition()
    {
        var body = PatchProcessor.GetOriginalInstructions(typeof(Fixture).GetMethod(nameof(Fixture.Position))!);
        var members = Extended();
        var guard = new MessageBarLabelGuard(new(members));
        Assert.That(guard.Describe(MessageBarLabelStage.Position, body).Failure, Is.Null);
        body.Single(instruction => instruction.opcode == OpCodes.Ldc_R4 && Equals(instruction.operand, 16f)).operand = 160f;
        Assert.That(guard.Describe(MessageBarLabelStage.Position, body).Failure, Is.Not.Null);
    }

    [Test]
    public void ActualLabelTextAndDraftedUnderlineRemainTiedToTheSuppliedBackground()
    {
        var body = PatchProcessor.GetOriginalInstructions(typeof(Fixture).GetMethod(nameof(Fixture.RectangleLabel))!);
        var guard = new MessageBarLabelGuard(new(Extended()));
        Assert.That(guard.Describe(MessageBarLabelStage.Rectangle, body).Failure, Is.Null);
        body.Single(instruction => instruction.opcode == OpCodes.Ldc_R4 && Equals(instruction.operand, 11f)).operand = 111f;
        Assert.That(guard.Describe(MessageBarLabelStage.Rectangle, body).Failure, Is.Not.Null);
    }

    [Test]
    public void LocalOverwriteBetweenLabelMeasurementsAndDrawingIsRejected()
    {
        var body = PatchProcessor.GetOriginalInstructions(typeof(Fixture).GetMethod(nameof(Fixture.RectangleLabel))!);
        int width = body.FindIndex(instruction => instruction.Calls((MethodInfo)Extended()["nameWidth"]));
        var store = new CodeInstruction(body[width + 1]);
        body.InsertRange(width + 2, new[] { new CodeInstruction(OpCodes.Ldc_R4, 700f), store });
        Assert.That(new MessageBarLabelGuard(new(Extended())).Describe(MessageBarLabelStage.Rectangle, body).Failure, Is.Not.Null);
    }

    [TestCase(MessageBarLabelStage.Colonist)]
    [TestCase(MessageBarLabelStage.Position)]
    [TestCase(MessageBarLabelStage.Rectangle)]
    public void SuppliedGeometryAddressCannotEscapeBeforeTheRequiredProjection(MessageBarLabelStage stage)
    {
        var body = stage == MessageBarLabelStage.Colonist ? Caller() : PatchProcessor.GetOriginalInstructions(typeof(Fixture)
            .GetMethod(stage == MessageBarLabelStage.Position ? nameof(Fixture.Position) : nameof(Fixture.RectangleLabel))!);
        body.InsertRange(0, new[] { new CodeInstruction(OpCodes.Ldarga, 1), new CodeInstruction(OpCodes.Pop) });
        Assert.That(new MessageBarLabelGuard(new(Extended())).Describe(stage, body).Failure, Is.Not.Null);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ActualVisibleBackgroundAndHealthConsumersMustUseMeasuredRectangles(bool health)
    {
        var body = PatchProcessor.GetOriginalInstructions(typeof(Fixture).GetMethod(nameof(Fixture.RectangleLabel))!);
        var members = Extended();
        var guard = new MessageBarLabelGuard(new(members));
        Assert.That(guard.Describe(MessageBarLabelStage.Rectangle, body).Failure, Is.Null);
        if (health)
            body.Single(instruction => instruction.Calls((MethodInfo)members["contract"])).operand = typeof(Fixture).GetMethod(nameof(Fixture.Expand));
        else
            body.Single(instruction => instruction.Calls((MethodInfo)members["drawTexture"])).operand = typeof(Fixture).GetMethod(nameof(Fixture.DrawElsewhere));
        Assert.That(guard.Describe(MessageBarLabelStage.Rectangle, body).Failure, Is.Not.Null);
    }

    [Test]
    public void RemovingOurObserverCannotKeepItsOldAuthorityThroughAForeignRewrite()
    {
        var target = typeof(Fixture).GetMethod(nameof(Fixture.Position))!;
        var admission = new MessageBarLabelAdmission(new(new(Extended())), (target, MessageBarLabelStage.Position));
        var foreign = new HarmonyLib.Harmony("tests.messages.label-observer-removal");
        try
        {
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(admission.Group, AdmissionPhase.Early), Is.True);
            var authority = admission.CaptureAuthority();
            Assert.That(authority(), Is.True);
            new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId).Unpatch(target, HarmonyPatchType.Transpiler, OwnedHarmonyPatch.OwnerId);
            foreign.Patch(target, transpiler: new(typeof(MessageBarLabelGuardTests), nameof(ChangeBackgroundHeight)));
            Assert.That(admission.Available, Is.False);
            Assert.That(authority(), Is.False);
            Assert.That(admission.Refresh(), Is.False);
        }
        finally
        {
            foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id);
            foreach (var patch in admission.Group.Patches.Reverse()) patch.Rollback();
        }
    }

    [Test]
    public void RefreshingAnUnchangedPublicationRetainsTheSameCapturedGeometryAuthority()
    {
        var target = typeof(Fixture).GetMethod(nameof(Fixture.Position))!;
        var admission = new MessageBarLabelAdmission(new(new(Extended())), (target, MessageBarLabelStage.Position));
        try
        {
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(admission.Group, AdmissionPhase.Early), Is.True);
            var authority = admission.CaptureAuthority();
            Assert.That(authority(), Is.True);
            Assert.That(admission.Refresh(), Is.True);
            Assert.That(admission.Refresh(), Is.True);
            Assert.That(authority(), Is.True, "Unchanged provider publication must not trigger recache as if geometry was replaced.");
        }
        finally { foreach (var patch in admission.Group.Patches.Reverse()) patch.Rollback(); }
    }

    [Test]
    public void ExecutedForeignByReferenceMutationCannotRetainTheNativeLabelReceipt()
    {
        var target = typeof(Fixture).GetMethod(nameof(Fixture.Position))!;
        var admission = new MessageBarLabelAdmission(new(new(Extended())), (target, MessageBarLabelStage.Position));
        var foreign = new HarmonyLib.Harmony("tests.messages.label-address-rewrite");
        try
        {
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(admission.Group, AdmissionPhase.Early), Is.True);
            foreign.Patch(target, transpiler: new(typeof(MessageBarLabelGuardTests), nameof(ChangeSuppliedPosition)));
            Fixture.Position(new Pawn(), new(10, 20), 1, 50, new(), 0, true, true);
            Assert.That(Fixture.LastBackground.Y, Is.EqualTo(700), "The fixture IL is executable and changes the actual drawing argument.");
            Assert.That(admission.Refresh(), Is.False);
            Assert.That(admission.Available, Is.False);
        }
        finally
        {
            foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id);
            foreach (var patch in admission.Group.Patches.Reverse()) patch.Rollback();
        }
    }
    public static IEnumerable<CodeInstruction> ChangeSuppliedPosition(IEnumerable<CodeInstruction> instructions)
    {
        yield return new(OpCodes.Ldarga, 1);
        yield return new(OpCodes.Call, typeof(MessageBarLabelGuardTests).GetMethod(nameof(MovePosition))!);
        foreach (var instruction in instructions) yield return instruction;
    }
    public static void MovePosition(ref Vector position) => position.Y = 700;

    [Test]
    public void ExecutedExtraDrawingBelowTheMeasuredLabelRevokesAutomaticAuthority()
    {
        var target = typeof(Fixture).GetMethod(nameof(Fixture.RectangleLabel))!;
        var admission = new MessageBarLabelAdmission(new(new(Extended(), new[] { typeof(Fixture) })),
            (target, MessageBarLabelStage.Rectangle));
        var foreign = new HarmonyLib.Harmony("tests.messages.extra-label-drawing");
        try
        {
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(admission.Group, AdmissionPhase.Early), Is.True);
            var old = admission.CaptureAuthority();
            foreign.Patch(target, transpiler: new(typeof(MessageBarLabelGuardTests), nameof(AppendLowerDrawing)));
            Fixture.RectangleLabel(new Pawn(), new(10, 20, 50, 12), 1, 50, new(), 0, true, true);
            Assert.That(Fixture.LastBackground.Y, Is.EqualTo(700), "The real patched fixture rendered below its measured label.");
            Assert.That(admission.Refresh(), Is.False);
            Assert.That(old(), Is.False);
        }
        finally
        {
            foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id);
            foreach (var patch in admission.Group.Patches.Reverse()) patch.Rollback();
        }
    }

    [Test]
    public void ExecutedFontRewriteBetweenMeasurementAndTextDrawingRevokesAutomaticAuthority()
    {
        var target = typeof(Fixture).GetMethod(nameof(Fixture.RectangleLabel))!;
        var admission = new MessageBarLabelAdmission(new(new(Extended(), new[] { typeof(Fixture) })),
            (target, MessageBarLabelStage.Rectangle));
        var foreign = new HarmonyLib.Harmony("tests.messages.label-font-rewrite");
        try
        {
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(admission.Group, AdmissionPhase.Early), Is.True);
            foreign.Patch(target, transpiler: new(typeof(MessageBarLabelGuardTests), nameof(ChangeFontAfterMeasurement)));
            Fixture.RectangleLabel(new Pawn(), new(10, 20, 50, 12), 1, 50, new(), 0, true, true);
            Assert.That(Fixture.LastTextHeight, Is.EqualTo(36), "The actual text consumer used a larger font than its 12-unit Tiny measurement.");
            Assert.That(admission.Refresh(), Is.False);
        }
        finally
        {
            foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id);
            foreach (var patch in admission.Group.Patches.Reverse()) patch.Rollback();
            Fixture.Font(0);
        }
    }

    [Test]
    public void AnExecutedFontRestoreAfterAllMeasuredDrawingRemainsCompatible()
    {
        var target = typeof(Fixture).GetMethod(nameof(Fixture.RectangleLabel))!;
        var admission = new MessageBarLabelAdmission(new(new(Extended(), new[] { typeof(Fixture) })),
            (target, MessageBarLabelStage.Rectangle));
        var foreign = new HarmonyLib.Harmony("tests.messages.label-font-restore");
        try
        {
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(admission.Group, AdmissionPhase.Early), Is.True);
            foreign.Patch(target, transpiler: new(typeof(MessageBarLabelGuardTests), nameof(RestoreFontAfterDrawing)));
            Fixture.RectangleLabel(new Pawn(), new(10, 20, 50, 12), 1, 50, new(), 0, true, true);
            Assert.That(Fixture.LastTextHeight, Is.EqualTo(12));
            Assert.That(Fixture.CurrentFont, Is.EqualTo(2));
            Assert.That(admission.Refresh(), Is.True);
        }
        finally
        {
            foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id);
            foreach (var patch in admission.Group.Patches.Reverse()) patch.Rollback();
            Fixture.Font(0);
        }
    }

    [TestCase(MessageBarLabelStage.Position, nameof(Fixture.Position))]
    [TestCase(MessageBarLabelStage.Rectangle, nameof(Fixture.RectangleLabel))]
    [TestCase(MessageBarLabelStage.Icons, nameof(Fixture.Icons))]
    [TestCase(MessageBarLabelStage.Icon, nameof(Fixture.Icon))]
    [TestCase(MessageBarLabelStage.PortraitTexture, nameof(Fixture.PortraitTexture))]
    public void EveryAdmittedHelperRejectsAnExtraPrimitiveDraw(MessageBarLabelStage stage, string method)
    {
        var body = PatchProcessor.GetOriginalInstructions(typeof(Fixture).GetMethod(method)!);
        var guard = new MessageBarLabelGuard(new(Extended(), new[] { typeof(Fixture) }));
        Assert.That(guard.Describe(stage, body).Failure, Is.Null);
        body.InsertRange(0, LowerDrawing());
        Assert.That(guard.Describe(stage, body).Failure, Is.Not.Null);
    }

    [TestCase(nameof(Fixture.EndClip))]
    [TestCase(nameof(Fixture.EndScrollView))]
    public void AHelperCannotEscapeTheCallerClippingBoundary(string method)
    {
        var body = PatchProcessor.GetOriginalInstructions(typeof(Fixture).GetMethod(nameof(Fixture.RectangleLabel))!);
        body.Insert(0, new(OpCodes.Call, typeof(Fixture).GetMethod(method)!));
        Assert.That(new MessageBarLabelGuard(new(Extended(), new[] { typeof(Fixture) }))
            .Describe(MessageBarLabelStage.Rectangle, body).Failure, Is.Not.Null);
    }

    public static IEnumerable<CodeInstruction> AppendLowerDrawing(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ret)
            {
                var extra = LowerDrawing(); extra[0].MoveLabelsFrom(instruction);
                foreach (var op in extra) yield return op;
            }
            yield return instruction;
        }
    }
    private static CodeInstruction[] LowerDrawing() => new[]
    {
        new CodeInstruction(OpCodes.Ldc_R4, 0f), new(OpCodes.Ldc_R4, 700f), new(OpCodes.Ldc_R4, 40f), new(OpCodes.Ldc_R4, 40f),
        new(OpCodes.Newobj, Extended()["rectangle"]), new(OpCodes.Ldsfld, Extended()["textBackground"]),
        new(OpCodes.Call, Extended()["drawTexture"])
    };
    public static IEnumerable<CodeInstruction> ChangeFontAfterMeasurement(IEnumerable<CodeInstruction> instructions)
    {
        bool width = false, emitAfterStore = false;
        foreach (var instruction in instructions)
        {
            yield return instruction;
            if (emitAfterStore)
            {
                yield return new(OpCodes.Ldc_I4_2); yield return new(OpCodes.Call, Extended()["font"]);
                emitAfterStore = false;
            }
            if (!width && instruction.Calls((MethodInfo)Extended()["nameWidth"])) { width = true; emitAfterStore = true; }
        }
    }
    public static IEnumerable<CodeInstruction> RestoreFontAfterDrawing(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ret)
            {
                yield return new CodeInstruction(OpCodes.Ldc_I4_2).MoveLabelsFrom(instruction);
                yield return new(OpCodes.Call, Extended()["font"]);
            }
            yield return instruction;
        }
    }

    [Test]
    public void NativePortraitTextureLowerExtentRemainsInsideTheCurrentPortrait()
    {
        var body = PatchProcessor.GetOriginalInstructions(typeof(Fixture).GetMethod(nameof(Fixture.PortraitTexture))!);
        var guard = new MessageBarLabelGuard(new(Extended()));
        Assert.That(guard.Describe(MessageBarLabelStage.PortraitTexture, body).Failure, Is.Null);
        body.Last(instruction => instruction.opcode == OpCodes.Ldc_R4 && Equals(instruction.operand, 1f)).operand = -1f;
        Assert.That(guard.Describe(MessageBarLabelStage.PortraitTexture, body).Failure, Is.Not.Null);
    }

    [Test]
    public void NativeIconUsesItsSuppliedExtentAndOnlyAdvancesTheHorizontalCursor()
    {
        var body = PatchProcessor.GetOriginalInstructions(typeof(Fixture).GetMethod(nameof(Fixture.Icon))!);
        var guard = new MessageBarLabelGuard(new(Extended()));
        Assert.That(guard.Describe(MessageBarLabelStage.Icon, body).Failure, Is.Null);
        body.Single(instruction => instruction.opcode == OpCodes.Ldflda).operand = typeof(Vector).GetField(nameof(Vector.Y));
        Assert.That(guard.Describe(MessageBarLabelStage.Icon, body).Failure, Is.Not.Null);
    }

    [Test]
    public void ACompleteLookingProjectionAfterAnEarlyNativeReturnCannotAuthorizeVisibleBounds()
    {
        var body = PatchProcessor.GetOriginalInstructions(typeof(Fixture).GetMethod(nameof(Fixture.Position))!);
        body.Insert(0, new(OpCodes.Ret));
        Assert.That(new MessageBarLabelGuard(new(Extended())).Describe(MessageBarLabelStage.Position, body).Failure, Is.Not.Null);
    }

    [Test]
    public void NativeIconsUseTheCurrentPortraitBottomAndTheSamePreparedSize()
    {
        var body = PatchProcessor.GetOriginalInstructions(typeof(Fixture).GetMethod(nameof(Fixture.Icons))!);
        var guard = new MessageBarLabelGuard(new(Extended()));
        Assert.That(guard.Describe(MessageBarLabelStage.Icons, body).Failure, Is.Null);
        int vector = body.FindIndex(instruction => Equals(instruction.operand, Members["vector"]));
        body.Take(vector).Last(instruction => instruction.opcode == OpCodes.Ldc_R4 && Equals(instruction.operand, 1f)).operand = -100f;
        Assert.That(guard.Describe(MessageBarLabelStage.Icons, body).Failure, Is.Not.Null);
    }

    [TestCase("contract")]
    [TestCase("beginGroup")]
    [TestCase("textureRect")]
    public void PortraitPaintingKeepsItsMeasuredRectangleOrCurrentClippingBoundary(string changed)
    {
        var body = PatchProcessor.GetOriginalInstructions(typeof(Fixture).GetMethod(nameof(Fixture.Paint))!);
        var members = Extended();
        var guard = new MessageBarLabelGuard(new(members));
        Assert.That(guard.Describe(MessageBarLabelStage.PortraitPaint, body).Failure, Is.Null);
        body.First(instruction => instruction.Calls((MethodInfo)members[changed])).operand = typeof(Fixture).GetMethod(
            changed == "contract" ? nameof(Fixture.Expand) : changed == "beginGroup" ? nameof(Fixture.BeginElsewhere) : nameof(Fixture.PortraitElsewhere));
        Assert.That(guard.Describe(MessageBarLabelStage.PortraitPaint, body).Failure, Is.Not.Null);
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ExtraPrimitiveGeometryAndMatrixChangesCannotKeepThePortraitReceipt(bool matrix)
    {
        var body = PatchProcessor.GetOriginalInstructions(typeof(Fixture).GetMethod(nameof(Fixture.Paint))!);
        var guard = new MessageBarLabelGuard(new(Extended(), new[] { typeof(Fixture) }));
        Assert.That(guard.Describe(MessageBarLabelStage.PortraitPaint, body).Failure, Is.Null);
        body.InsertRange(0, matrix ? new[] { new CodeInstruction(OpCodes.Ldnull),
            new CodeInstruction(OpCodes.Call, typeof(Fixture).GetProperty(nameof(Fixture.matrix))!.SetMethod!) } :
            new[] { new CodeInstruction(OpCodes.Ldarg_1), new CodeInstruction(OpCodes.Call, typeof(Fixture).GetMethod(nameof(Fixture.Unexpected))!) });
        Assert.That(guard.Describe(MessageBarLabelStage.PortraitPaint, body).Failure, Is.Not.Null);
    }

    [Test]
    public void ActualPublishedLabelRewriteRevokesOnlyItsAutomaticGeometryReceipt()
    {
        var target = typeof(Fixture).GetMethod(nameof(Fixture.Position))!;
        var admission = new MessageBarLabelAdmission(new(new(Extended())), (target, MessageBarLabelStage.Position));
        var foreign = new HarmonyLib.Harmony("tests.messages.label-rewrite");
        try
        {
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(admission.Group, AdmissionPhase.Early), Is.True);
            Assert.That(admission.Available, Is.True);
            foreign.Patch(target, transpiler: new(typeof(MessageBarLabelGuardTests), nameof(ChangeBackgroundHeight)));
            Assert.That(admission.Available, Is.False);
            Assert.That(admission.Refresh(), Is.False);
            foreign.Unpatch(target, HarmonyPatchType.Transpiler, foreign.Id);
            Assert.That(admission.Refresh(), Is.True);
            Assert.That(admission.Available, Is.True);
        }
        finally
        {
            foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id);
            foreach (var patch in admission.Group.Patches.Reverse()) patch.Rollback();
        }
    }
    public static IEnumerable<CodeInstruction> ChangeBackgroundHeight(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
            yield return instruction.opcode == OpCodes.Ldc_R4 && Equals(instruction.operand, 16f) ?
                new CodeInstruction(OpCodes.Ldc_R4, 160f) : instruction;
    }

    private static Dictionary<string, MemberInfo> Extended()
    {
        var result = new Dictionary<string, MemberInfo>(Members);
        void Method(string key, string method) => result[key] = typeof(Fixture).GetMethod(method)!;
        Method("nameWidth", nameof(Fixture.NameWidth)); Method("getLabel", nameof(Fixture.GetLabel));
        Method("disableTiny", nameof(Fixture.DisableTiny)); Method("font", nameof(Fixture.Font));
        Method("anchor", nameof(Fixture.Anchor)); Method("calcSize", nameof(Fixture.CalcSize));
        Method("label", nameof(Fixture.DrawText)); Method("tiny", nameof(Fixture.Tiny));
        Method("line", nameof(Fixture.Line)); Method("rectangleLabel", nameof(Fixture.RectangleLabel));
        Method("drawTexture", nameof(Fixture.DrawTexture)); Method("fill", nameof(Fixture.Fill)); Method("contract", nameof(Fixture.Contract));
        Method("size", nameof(Fixture.Size)); Method("tip", nameof(Fixture.Tip)); Method("tipRegion", nameof(Fixture.TipRegion));
        result["multiplyVector"] = typeof(Vector).GetMethod("op_Multiply")!;
        result["textureSize"] = typeof(Fixture).GetField(nameof(Fixture.TextureSize))!;
        result["iconArea"] = typeof(Fixture).GetField(nameof(Fixture.IconArea))!;
        result["iconMax"] = typeof(Fixture).GetField(nameof(Fixture.IconMax))!;
        result["iconList"] = typeof(Fixture).GetField(nameof(Fixture.IconList))!;
        result["iconCount"] = typeof(List<IconItem>).GetProperty(nameof(List<IconItem>.Count))!.GetMethod!;
        result["min"] = typeof(MathF).GetMethod(nameof(MathF.Min), new[] { typeof(float), typeof(float) })!;
        result["iconTexture"] = typeof(IconItem).GetField(nameof(IconItem.Texture))!;
        result["iconTooltip"] = typeof(IconItem).GetField(nameof(IconItem.Tooltip))!;
        Method("drawIcon", nameof(Fixture.Icon));
        Method("drawIcons", nameof(Fixture.Icons)); Method("textureRect", nameof(Fixture.PortraitTexture));
        Method("beginGroup", nameof(Fixture.BeginGroup)); Method("endGroup", nameof(Fixture.EndGroup));
        Method("atZero", nameof(Fixture.AtZero)); Method("setColor", nameof(Fixture.SetColor));
        Method("box", nameof(Fixture.Box)); Method("boxSolid", nameof(Fixture.BoxSolid));
        Method("portrait", nameof(Fixture.Portrait));
        result["backgroundTexture"] = typeof(Fixture).GetField(nameof(Fixture.BackgroundTexture))!;
        result["moodTexture"] = typeof(Fixture).GetField(nameof(Fixture.MoodTexture))!;
        result["gradientTexture"] = typeof(Fixture).GetField(nameof(Fixture.GradientTexture))!;
        result["deadTexture"] = typeof(Fixture).GetField(nameof(Fixture.DeadTexture))!;
        result["needs"] = typeof(Pawn).GetField(nameof(Pawn.Needs))!;
        result["mood"] = typeof(Needs).GetField(nameof(Needs.Mood))!;
        result["level"] = typeof(Mood).GetProperty(nameof(Mood.Level))!.GetMethod!;
        result["height"] = typeof(Rectangle).GetProperty(nameof(Rectangle.Height))!.GetMethod!;
        result["setHeight"] = typeof(Rectangle).GetProperty(nameof(Rectangle.Height))!.SetMethod!;
        result["setYMin"] = typeof(Rectangle).GetProperty(nameof(Rectangle.YMin))!.SetMethod!;
        result["position"] = typeof(Rectangle).GetProperty(nameof(Rectangle.Position))!.GetMethod!;
        result["textBackground"] = typeof(Fixture).GetField(nameof(Fixture.TextBackground))!;
        result["healthTexture"] = typeof(Fixture).GetField(nameof(Fixture.HealthTexture))!;
        result["clearTexture"] = typeof(Fixture).GetField(nameof(Fixture.ClearTexture))!;
        result["drafted"] = typeof(Pawn).GetProperty(nameof(Pawn.Drafted))!.GetMethod!;
        result["rectX"] = typeof(Rectangle).GetProperty(nameof(Rectangle.X))!.GetMethod!;
        result["rectY"] = typeof(Rectangle).GetProperty(nameof(Rectangle.Y))!.GetMethod!;
        result["y"] = typeof(Vector).GetField(nameof(Vector.Y))!;
        result["rectangle"] = typeof(Rectangle).GetConstructor(new[] { typeof(float), typeof(float), typeof(float), typeof(float) })!;
        return result;
    }

    private static List<CodeInstruction> Caller() => new()
    {
        new(OpCodes.Ldc_R4, 4f), new(OpCodes.Ldarg_0), new(OpCodes.Call, Members["bar"]), new(OpCodes.Callvirt, Members["scale"]),
        new(OpCodes.Mul), new(OpCodes.Stloc, 17), new(OpCodes.Ldloca, 23), new(OpCodes.Ldarga, 1),
        new(OpCodes.Call, Members["center"]), new(OpCodes.Ldfld, Members["x"]), new(OpCodes.Ldarga, 1),
        new(OpCodes.Call, Members["bottom"]), new(OpCodes.Ldloc, 17), new(OpCodes.Sub), new(OpCodes.Call, Members["vector"]),
        new(OpCodes.Ldarg_2), new(OpCodes.Ldloc, 23), new(OpCodes.Ldloc, 9), new(OpCodes.Ldarga, 1),
        new(OpCodes.Call, Members["width"]), new(OpCodes.Ldarg_0), new(OpCodes.Call, Members["bar"]),
        new(OpCodes.Callvirt, Members["space"]), new(OpCodes.Add), new(OpCodes.Ldc_R4, 2f), new(OpCodes.Sub),
        new(OpCodes.Ldarg_0), new(OpCodes.Ldfld, Members["cache"]), new(OpCodes.Ldc_I4_0), new(OpCodes.Ldc_I4_1),
        new(OpCodes.Ldc_I4_1), new(OpCodes.Call, Members["positionLabel"])
    };

    public struct Vector(float x, float y)
    {
        public float X = x, Y = y;
        public static Vector operator *(Vector vector, float factor) => new(vector.X * factor, vector.Y * factor);
    }
    public struct Rectangle(float x, float y, float width, float height)
    {
        public Vector Center => new(x + width / 2, y + height / 2);
        public float Bottom => y + height;
        public float Width => width;
        public float X => x;
        public float Y => y;
        public float Height { get => height; set => height = value; }
        public float YMin { get => y; set { height += y - value; y = value; } }
        public Vector Position => new(x, y);
    }
    public sealed class Pawn { public bool Drafted => true; public bool Dead => false; public float Health => 0.7f; public Needs Needs = new(); }
    public sealed class Needs { public Mood Mood = new(); }
    public sealed class Mood { public float Level => 0.7f; }
    public struct IconItem { public object Texture; public string Tooltip; }
    public sealed class Fixture
    {
        public Dictionary<string, string> Cache = new();
        public static object TextBackground = new(), HealthTexture = new(), ClearTexture = new();
        public static object BackgroundTexture = new(), MoodTexture = new(), GradientTexture = new(), DeadTexture = new();
        public static Rectangle LastBackground;
        public static Vector TextureSize = new(48, 70);
        public static float IconArea = 48, IconMax = 16;
        public static List<IconItem> IconList = new();
        public Fixture Bar() => this;
        public float Scale() => 1;
        public float Space() => 24;
        public Vector Size() => new(48, 48);
        public static void Label(object pawn, Vector position, float alpha, float width, Dictionary<string, string> cache,
            int font, bool background, bool center) { }
        public static float NameWidth(Pawn pawn, float width, Dictionary<string, string> cache, int font) => 30;
        public static string GetLabel(Pawn pawn, float width, Dictionary<string, string> cache, int font) => "Pawn";
        public static bool DisableTiny() => false;
        public static int CurrentFont, LastTextHeight;
        public static void Font(int font) => CurrentFont = font;
        public static void Anchor(int anchor) { }
        public static Vector CalcSize(string text) => new(30, 12);
        public static void DrawText(Rectangle rect, string text) => LastTextHeight = CurrentFont == 0 ? 12 : 36;
        public static bool Tiny() => true;
        public static void Line(float x, float y, float width) { }
        public static void DrawTexture(Rectangle rect, object texture) => LastBackground = rect;
        public static void DrawElsewhere(Rectangle rect, object texture) { }
        public static object? matrix { get; set; }
        public static void Unexpected(Rectangle rect) { }
        public static void BeginGroup(Rectangle rect) { }
        public static void BeginElsewhere(Rectangle rect) { }
        public Rectangle PortraitElsewhere(Vector position) => new(position.X, position.Y + 700, 48, 48);
        public static void EndGroup() { }
        public static void EndClip() { }
        public static void EndScrollView() { }
        public static Rectangle AtZero(Rectangle rect) => new(0, 0, rect.Width, rect.Height);
        public static void SetColor(object color) { }
        public static void Box(Rectangle rect, int thickness, object? texture) { }
        public static void BoxSolid(Rectangle rect, object color) { }
        public static object Portrait(Pawn pawn) => new();
        public void Paint(Rectangle rect, Pawn pawn, bool gradient)
        {
            object color = new(), normal = new();
            DrawTexture(rect, BackgroundTexture);
            if (pawn.Needs != null && pawn.Needs.Mood != null)
            {
                var fill = Contract(rect, 2);
                float height = fill.Height * pawn.Needs.Mood.Level;
                fill.YMin = fill.Bottom - height;
                fill.Height = height;
                DrawTexture(fill, MoodTexture);
            }
            BoxSolid(rect, color); Box(rect, 2, null);
            DrawTexture(PortraitTexture(rect.Position), Portrait(pawn));
            if (gradient)
            {
                var clip = Contract(rect, 1);
                BeginGroup(clip);
                var fill = AtZero(clip);
                fill.YMin = fill.Bottom - 35;
                SetColor(color); DrawTexture(fill, GradientTexture); SetColor(normal); EndGroup();
            }
            Icons(rect, pawn);
            if (pawn.Dead) DrawTexture(rect, DeadTexture);
        }
        public static Rectangle Contract(Rectangle rect, float value) => rect;
        public static Rectangle Expand(Rectangle rect, float value) => rect;
        public static Rectangle Fill(Rectangle rect, float level, object full, object empty, bool border) => rect;
        public static object Tip(string tooltip) => tooltip;
        public static void TipRegion(Rectangle rect, object tooltip) { }
        public Rectangle PortraitTexture(Vector position)
        {
            float x = position.X, y = position.Y;
            Vector texture = TextureSize * Bar().Scale();
            return Contract(new Rectangle(x + 1, y - (texture.Y - Bar().Size().Y) - 1, texture.X, texture.Y), 1);
        }
        public void Icon(object texture, ref Vector position, float size, string? tooltip)
        {
            var rect = new Rectangle(position.X, position.Y, size, size);
            DrawTexture(rect, texture);
            if (tooltip != null) TipRegion(rect, Tip(tooltip));
            position.X += size;
        }
        public void Icons(Rectangle rect, Pawn pawn)
        {
            if (pawn.Dead) return;
            float size = MathF.Min(IconArea / IconList.Count, IconMax) * Bar().Scale();
            var position = new Vector(rect.X + 1, rect.Bottom - size - 1);
            foreach (var item in IconList) Icon(item.Texture, ref position, size, item.Tooltip);
        }
        public static void Position(Pawn pawn, Vector position, float alpha, float width, Dictionary<string, string> cache,
            int font, bool background, bool center)
        {
            float nameWidth = NameWidth(pawn, width, cache, font);
            float padding = DisableTiny() ? 6f : 4f;
            float height = DisableTiny() ? 16f : 12f;
            var rect = new Rectangle(position.X - nameWidth / 2 - padding, position.Y, nameWidth + padding * 2, height);
            RectangleLabel(pawn, rect, alpha, width, cache, font, background, center);
        }
        public static void RectangleLabel(Pawn pawn, Rectangle bg, float alpha, float width, Dictionary<string, string> cache,
            int font, bool background, bool center)
        {
            Font(font);
            string label = GetLabel(pawn, width, cache, font);
            float nameWidth = NameWidth(pawn, width, cache, font);
            float health = pawn.Health;
            if (background || health < 0.999f) DrawTexture(bg, TextBackground);
            if (health < 0.999f) Fill(Contract(bg, 1), health, HealthTexture, ClearTexture, false);
            Rectangle rect;
            if (center)
            {
                Anchor(1);
                rect = new(bg.Center.X - nameWidth / 2, bg.Y - 2, nameWidth, 100);
            }
            else
            {
                Anchor(0);
                rect = new(bg.X + 2, bg.Center.Y - CalcSize(label).Y / 2, nameWidth, 100);
            }
            DrawText(rect, label);
            if (pawn.Drafted) Line(bg.Center.X - nameWidth / 2, bg.Y + 11 + (Tiny() ? 0 : 3), nameWidth);
        }
    }
}
