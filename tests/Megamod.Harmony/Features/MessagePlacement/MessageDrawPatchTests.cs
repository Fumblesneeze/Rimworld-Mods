using System.Runtime.CompilerServices;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Megamod.Features.MessagePlacement;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.MessagePlacement;

[TestFixture, NonParallelizable]
public sealed class MessageDrawPatchTests
{
    private static int observations;
    private static int parameterConstructions, methodConstructions, innerObservations, entryObservations, forwardObservations, factoryCalls;

    [Test]
    public void OversizedPublishedMetadataCannotPreventForeignRemovalAndLaterRecovery()
    {
        var foreign = new HarmonyLib.Harmony(new string('x', 4 * 1024 * 1024));
        MethodInfo target = typeof(NativeMessage).GetMethod(nameof(NativeMessage.Draw))!;
        var patch = Create();
        try
        {
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("messages", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.True);
            foreign.Patch(target, transpiler: new(typeof(MessageDrawPatchTests), nameof(AddNops)));
            Assert.That(patch.Available, Is.False);
            Assert.DoesNotThrow(() => foreign.Unpatch(target, HarmonyPatchType.Transpiler, foreign.Id));
            observations = 0;
            var message = new NativeMessage();
            message.Draw(19, 37);
            Assert.That(observations, Is.Zero, "Uninspectable rebuild returns the untouched native registration stream.");
            Assert.That(message.Window.Bounds, Is.EqualTo(new NativeRect(19, 37, 100, 25)));
            message.Window.Callback!();
            Assert.That(message.Window.CallbackCalls, Is.EqualTo(1));
            Assert.That(patch.Available, Is.False);
            Assert.That(patch.Shape, Is.Null);

            new HarmonyLib.Harmony("tests.messages.metadata-recovery").CreateProcessor(target).Patch();
            Assert.That(patch.Available, Is.True);
            message.Draw(20, 38);
            Assert.That(observations, Is.EqualTo(1));
            Assert.That(message.Window.Bounds, Is.EqualTo(new NativeRect(20, 38, 100, 25)));
        }
        finally
        {
            // Pre-fix removal can fail while metadata is oversized. Remove only this fixture's
            // owned transpiler before removing the foreign registration, then release its receipt.
            try { new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId).Unpatch(target, HarmonyPatchType.Transpiler, OwnedHarmonyPatch.OwnerId); }
            finally
            {
                try { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
                finally { patch.Candidate.Rollback(); }
            }
        }
    }

    [Test]
    public void PublicInstructionInspectionOutsideCompositionReturnsNativeStreamAndRevokesAuthority()
    {
        MethodInfo target = typeof(NativeMessage).GetMethod(nameof(NativeMessage.Draw))!;
        var patch = Create();
        try
        {
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("messages", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.True);
            List<CodeInstruction>? inspected = null;
            Assert.DoesNotThrow(() => inspected = PatchProcessor.GetCurrentInstructions(target));
            Assert.That(inspected!.Count(instruction => instruction.Calls(typeof(NativeWindow).GetMethod(nameof(NativeWindow.Register)))), Is.EqualTo(1));
            Assert.That(inspected.Count(instruction => instruction.Calls(typeof(NativeMessage).GetMethod(nameof(NativeMessage.CalculateRect)))), Is.EqualTo(1));
            Assert.That(inspected.Any(instruction => instruction.Calls(typeof(MessageDrawPatchTests).GetMethod(nameof(Forward)))), Is.False);
            Assert.That(patch.Available, Is.False);
            Assert.That(patch.Shape, Is.Null);
            var message = new NativeMessage();
            message.Draw(19, 37);
            Assert.That(message.Window.Bounds, Is.EqualTo(new NativeRect(19, 37, 100, 25)));
            message.Window.Callback!();
            Assert.That(message.Window.CallbackCalls, Is.EqualTo(1));

            new HarmonyLib.Harmony("tests.messages.inspection-recovery").CreateProcessor(target).Patch();
            Assert.That(patch.Available, Is.True);
            Assert.That(patch.Shape, Is.Not.Null);
        }
        finally { patch.Candidate.Rollback(); }
    }

    [Test]
    public void LateInnerSkipOfForwardingCallRevokesAdmissionAndRetainsForeignBehavior()
    {
        var foreign = new HarmonyLib.Harmony("tests.messages.inner-skip-forward");
        MethodInfo target = typeof(NativeMessage).GetMethod(nameof(NativeMessage.Draw))!;
        var patch = Create();
        try
        {
            observations = 0;
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("messages", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.True);
            foreign.CreateProcessor(target).AddInnerPrefix(new HarmonyMethod(typeof(MessageDrawPatchTests), nameof(Skip))
                { innerMethod = new InnerMethod(typeof(MessageDrawPatchTests).GetMethod(nameof(Forward))!) }).Patch();
            var message = new NativeMessage();
            message.Draw(19, 37);
            Assert.That(message.LastBounds, Is.EqualTo(new NativeRect(19, 37, 100, 25)));
            Assert.That(message.Window.Callback, Is.Null, "The actual foreign skip remains effective.");
            Assert.That(observations, Is.Zero);
            Assert.That(patch.Available, Is.False, "Skipping the admitted native registration invalidates its publication.");
        }
        finally
        {
            // The foreign selector names the call emitted by this owned transpiler.
            try { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
            finally { patch.Candidate.Rollback(); }
        }
    }

    public static bool Skip() => false;

    [Test]
    public void LateInnerRectangleReadReplacementRevokesAdmissionWithoutUndoingForeignValue()
    {
        var foreign = new HarmonyLib.Harmony("tests.messages.inner-rectangle-read");
        MethodInfo target = typeof(NativeMessage).GetMethod(nameof(NativeMessage.Draw))!;
        var patch = Create();
        try
        {
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("messages", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.True);
            FieldInfo capturedRectangle = patch.Shape!.RectangleField;
            foreign.CreateProcessor(target).AddInnerPostfix(new HarmonyMethod(typeof(MessageDrawPatchTests), nameof(ReplaceRectangle))
                { innerTarget = new InnerTarget(capturedRectangle, InnerTargetKind.FieldRead) }).Patch();
            var message = new NativeMessage();
            message.Draw(19, 37);
            Assert.That(message.Window.Bounds, Is.EqualTo(new NativeRect(90, 91, 92, 93)));
            message.Window.Callback!();
            Assert.That(message.Window.Bounds, Is.EqualTo(message.LastBounds), "The callback still consumes the distinct original capture.");
            Assert.That(patch.Available, Is.False);
        }
        finally
        {
            try { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
            finally { patch.Candidate.Rollback(); }
        }
    }

    public static void ReplaceRectangle(ref NativeRect __result) => __result = new(90, 91, 92, 93);

    [TestCase("message")]
    [TestCase("rectangle")]
    [TestCase("delegate")]
    public void SkippingCapturedWindowProvenanceRevokesAdmission(string skipped)
    {
        var foreign = new HarmonyLib.Harmony("tests.messages.inner-provenance." + skipped);
        MethodInfo target = typeof(NativeMessage).GetMethod(nameof(NativeMessage.Draw))!;
        var patch = Create();
        try
        {
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("messages", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.True);
            var shape = patch.Shape!;
            InnerTarget selected = skipped == "delegate"
                ? new InnerTarget(typeof(Action).GetConstructor([typeof(object), typeof(IntPtr)])!)
                : new InnerTarget(skipped == "message" ? shape.MessageField : shape.RectangleField, InnerTargetKind.FieldWrite);
            foreign.CreateProcessor(target).AddInnerPrefix(new HarmonyMethod(typeof(MessageDrawPatchTests), nameof(Skip))
                { innerTarget = selected }).Patch();
            var message = new NativeMessage();
            message.Draw(19, 37);
            if (skipped == "delegate") Assert.That(message.Window.Callback, Is.Null);
            else if (skipped == "message") Assert.That(shape.MessageField.GetValue(message.Window.Callback!.Target), Is.Null);
            else Assert.That(message.Window.Bounds, Is.EqualTo(default(NativeRect)));
            Assert.That(patch.Available, Is.False);
        }
        finally
        {
            try { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
            finally { patch.Candidate.Rollback(); }
        }
    }

    [Test]
    public void CompatibleInnerChangesAndObserversSurviveWithoutInspectionExecutingAttributes()
    {
        var foreign = new HarmonyLib.Harmony("tests.messages.inner-benign");
        MethodInfo target = typeof(NativeMessage).GetMethod(nameof(NativeMessage.Draw))!;
        var patch = Create();
        try
        {
            parameterConstructions = methodConstructions = innerObservations = entryObservations = forwardObservations = observations = 0;
            foreign.CreateProcessor(target).AddInnerPostfix(new HarmonyMethod(typeof(MessageDrawPatchTests), nameof(ChangeId))
                { innerTarget = InnerTarget.Constant(7) }).Patch();
            foreign.CreateProcessor(target).AddInnerPostfix(new HarmonyMethod(typeof(MessageDrawPatchTests), nameof(ObserveId))
                { innerTarget = InnerTarget.Constant(7) }).Patch();
            foreign.Patch(target, prefix: new(typeof(MessageDrawPatchTests), nameof(ObserveEntry)));
            Assert.That(parameterConstructions, Is.GreaterThan(0));
            Assert.That(methodConstructions, Is.GreaterThan(0));
            parameterConstructions = methodConstructions = 0;
            Assert.That(patch.Candidate.Validate(), Is.Null);
            Assert.That(parameterConstructions + methodConstructions, Is.Zero);
            Assert.That(innerObservations + entryObservations, Is.Zero);
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("messages", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.True);
            foreign.CreateProcessor(target).AddInnerPrefix(new HarmonyMethod(typeof(MessageDrawPatchTests), nameof(ObserveForward))
                { innerMethod = new InnerMethod(typeof(MessageDrawPatchTests).GetMethod(nameof(Forward))!) }).Patch();
            parameterConstructions = methodConstructions = 0;
            Assert.That(patch.Available, Is.True);
            Assert.That(patch.Shape, Is.Not.Null);
            Assert.That(parameterConstructions + methodConstructions, Is.Zero);
            Assert.That(innerObservations + entryObservations + forwardObservations, Is.Zero);
            var message = new NativeMessage();
            message.Draw(19, 37);
            Assert.That(message.Window.Id, Is.EqualTo(8));
            Assert.That(message.Window.Bounds, Is.EqualTo(message.LastBounds));
            Assert.That(innerObservations, Is.EqualTo(1));
            Assert.That(entryObservations, Is.EqualTo(1));
            Assert.That(forwardObservations, Is.EqualTo(1));
            Assert.That(observations, Is.EqualTo(1));
            Assert.That(parameterConstructions + methodConstructions, Is.Zero);

            foreign.Unpatch(target, HarmonyPatchType.InnerPrefix, foreign.Id); // Remove selector of the owned emitted call.
            parameterConstructions = methodConstructions = 0;
            patch.Candidate.Rollback();
            message.Draw(20, 38);
            Assert.That(message.Window.Id, Is.EqualTo(8));
            Assert.That(innerObservations, Is.EqualTo(2));
            Assert.That(entryObservations, Is.EqualTo(2));
            Assert.That(observations, Is.EqualTo(1), "Rollback removes owned forwarding only.");
        }
        finally
        {
            try { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
            finally { patch.Candidate.Rollback(); }
        }
    }

    public static void ChangeId(ref int __result) => __result++;
    public static void ObserveId([CountParameter] int __result) => innerObservations++;
    [CountMethod]
    public static void ObserveEntry() => entryObservations++;
    public static void ObserveForward() => forwardObservations++;
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class CountParameterAttribute : Attribute { public CountParameterAttribute() => parameterConstructions++; }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CountMethodAttribute : Attribute { public CountMethodAttribute() => methodConstructions++; }

    [TestCase(false)]
    [TestCase(true)]
    public void TranspilerFactoryInspectionNeverExecutesFactory(bool late)
    {
        var foreign = new HarmonyLib.Harmony("tests.messages.factory");
        MethodInfo target = typeof(NativeMessage).GetMethod(nameof(NativeMessage.Draw))!;
        var patch = Create();
        try
        {
            if (late) Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("messages", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.True);
            factoryCalls = 0;
            foreign.Patch(target, transpiler: new(typeof(MessageDrawPatchTests), nameof(TranspilerFactory)));
            Assert.That(factoryCalls, Is.GreaterThan(0));
            factoryCalls = 0;
            if (late) Assert.That(patch.Available, Is.False);
            else Assert.That(patch.Candidate.Validate(), Is.Not.Null);
            Assert.That(factoryCalls, Is.Zero);
            patch.Candidate.Rollback();
            var message = new NativeMessage();
            message.Draw(19, 37);
            Assert.That(message.Window.Bounds, Is.EqualTo(message.LastBounds));
        }
        finally
        {
            try { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
            finally { patch.Candidate.Rollback(); }
        }
    }

    public static MethodInfo TranspilerFactory(MethodBase original)
    {
        factoryCalls++;
        return typeof(MessageDrawPatchTests).GetMethod(nameof(AddNops))!;
    }

    [Test]
    public void AdditionalOwnedTranspilerRevokesAuthorityWithoutRemovingSlot()
    {
        var sameOwner = new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId);
        MethodInfo target = typeof(NativeMessage).GetMethod(nameof(NativeMessage.Draw))!;
        MethodInfo additional = typeof(MessageDrawPatchTests).GetMethod(nameof(AddNops))!;
        var patch = Create();
        try
        {
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("messages", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.True);
            sameOwner.Patch(target, transpiler: new(additional));
            Assert.That(patch.Available, Is.False);
            Assert.Throws<InvalidOperationException>(() => patch.Candidate.Rollback());
            Assert.That(HarmonyPublication.Inspect(target, snapshot =>
                snapshot.Transpilers.Any(item => item.owner == OwnedHarmonyPatch.OwnerId && item.PatchMethod == additional)), Is.True);
        }
        finally
        {
            try { sameOwner.Unpatch(target, additional); }
            finally { patch.Candidate.Rollback(); }
        }
    }

    [Test]
    public void CompatibleForeignPrefixAndNopsRemainInstalledAndEffective()
    {
        var foreign = new HarmonyLib.Harmony("tests.messages.benign");
        MethodInfo target = typeof(NativeMessage).GetMethod(nameof(NativeMessage.Draw))!;
        var patch = Create();
        try
        {
            observations = 0;
            foreign.Patch(target, prefix: new(typeof(MessageDrawPatchTests), nameof(ShiftNativeOrigin)),
                transpiler: new(typeof(MessageDrawPatchTests), nameof(AddNops)));
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("messages", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.True);
            var message = new NativeMessage();
            message.Draw(19, 37);
            Assert.That(observations, Is.EqualTo(1));
            Assert.That(message.Window.Bounds.X, Is.EqualTo(24));
            patch.Candidate.Rollback();
            message.Draw(30, 40);
            Assert.That(message.Window.Bounds.X, Is.EqualTo(35));
            Assert.That(observations, Is.EqualTo(1));
        }
        finally { patch.Candidate.Rollback(); foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }

    [Test]
    public void ChangedCapturedRectangleIsRejectedWithoutChangingForeignRegistration()
    {
        var foreign = new HarmonyLib.Harmony("tests.messages.changed-rectangle");
        MethodInfo target = typeof(NativeMessage).GetMethod(nameof(NativeMessage.Draw))!;
        var patch = Create();
        try
        {
            observations = 0;
            foreign.Patch(target, transpiler: new(typeof(MessageDrawPatchTests), nameof(ChangeMeasure)));
            var warnings = new List<string>();
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(new("messages", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.False);
            Assert.That(warnings, Has.Count.EqualTo(1));
            var message = new NativeMessage();
            message.Draw(19, 37);
            Assert.That(message.Window.Bounds.Width, Is.EqualTo(400));
            Assert.That(observations, Is.Zero);
        }
        finally { patch.Candidate.Rollback(); foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }

    [Test]
    public void LaterIncompatibleRepatchRevokesPublicationAndDoesNotRunObservation()
    {
        var foreign = new HarmonyLib.Harmony("tests.messages.later-change");
        MethodInfo target = typeof(NativeMessage).GetMethod(nameof(NativeMessage.Draw))!;
        var patch = Create();
        try
        {
            observations = 0;
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("messages", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.True);
            foreign.Patch(target, transpiler: new HarmonyMethod(typeof(MessageDrawPatchTests), nameof(ChangeMeasure))
                { before = [OwnedHarmonyPatch.OwnerId] });
            Assert.That(patch.Available, Is.False);
            new NativeMessage().Draw(19, 37);
            Assert.That(observations, Is.Zero);
        }
        finally { patch.Candidate.Rollback(); foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }

    [Test]
    public void CallbackMovedToOutsideClickArgumentDoesNotProveMessageWindowRendering()
    {
        var foreign = new HarmonyLib.Harmony("tests.messages.wrong-callback-argument");
        MethodInfo target = typeof(NativeMessage).GetMethod(nameof(NativeMessage.Draw))!;
        var patch = Create();
        try
        {
            foreign.Patch(target, transpiler: new(typeof(MessageDrawPatchTests), nameof(MoveCallbackToOutside)));
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("messages", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.False);
        }
        finally { patch.Candidate.Rollback(); foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }

    public static IEnumerable<CodeInstruction> MoveCallbackToOutside(IEnumerable<CodeInstruction> instructions)
    {
        var incoming = instructions.ToList();
        int function = incoming.FindIndex(i => i.opcode == OpCodes.Ldftn);
        int call = incoming.FindIndex(i => i.Calls(typeof(NativeWindow).GetMethod(nameof(NativeWindow.Register))));
        incoming.RemoveAt(call - 1); // Remove the ordinary null outside-click argument.
        incoming.Insert(function - 1, new(OpCodes.Ldnull)); // Drawing callback becomes null; real Action goes to outside-click.
        return incoming;
    }

    [Test]
    public void CallbackFromDifferentCaptureObjectDoesNotProveCoherentRectangle()
    {
        var foreign = new HarmonyLib.Harmony("tests.messages.other-capture");
        MethodInfo target = typeof(NativeMessage).GetMethod(nameof(NativeMessage.Draw))!;
        var patch = Create();
        try
        {
            foreign.Patch(target, transpiler: new(typeof(MessageDrawPatchTests), nameof(ChangeCallbackCapture)));
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("messages", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.False);
        }
        finally { patch.Candidate.Rollback(); foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }

    public static IEnumerable<CodeInstruction> ChangeCallbackCapture(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var incoming = instructions.ToList();
        int function = incoming.FindIndex(i => i.opcode == OpCodes.Ldftn);
        Type captureType = ((MethodInfo)incoming[function].operand).DeclaringType!;
        var capture = generator.DeclareLocal(captureType);
        incoming[function - 1].opcode = OpCodes.Ldloc;
        incoming[function - 1].operand = capture;
        incoming.InsertRange(0, [new(OpCodes.Newobj, captureType.GetConstructor(Type.EmptyTypes)!), new(OpCodes.Stloc, capture)]);
        return incoming;
    }

    [Test]
    public void UnrelatedOutsideClickCallbackLoadedFromLocalIsForwardedUnchanged()
    {
        var foreign = new HarmonyLib.Harmony("tests.messages.unrelated-local");
        MethodInfo target = typeof(NativeMessage).GetMethod(nameof(NativeMessage.Draw))!;
        var patch = Create();
        try
        {
            foreign.Patch(target, transpiler: new(typeof(MessageDrawPatchTests), nameof(OutsideClickFromLocal)));
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("messages", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.True);
            observations = 0;
            new NativeMessage().Draw(19, 37);
            Assert.That(observations, Is.EqualTo(1));
        }
        finally { patch.Candidate.Rollback(); foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }

    [Test]
    public void LaterCompatibleTranspilerRunsBeforeValidationAndKeepsObservationAvailable()
    {
        var foreign = new HarmonyLib.Harmony("tests.messages.later-benign");
        MethodInfo target = typeof(NativeMessage).GetMethod(nameof(NativeMessage.Draw))!;
        var patch = Create();
        try
        {
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("messages", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.True);
            foreign.Patch(target, transpiler: new(typeof(MessageDrawPatchTests), nameof(AddNops)));
            Assert.That(patch.Available, Is.True);
            observations = 0;
            new NativeMessage().Draw(19, 37);
            Assert.That(observations, Is.EqualTo(1));
        }
        finally { patch.Candidate.Rollback(); foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }

    [Test]
    public void ExplicitForeignTransformAfterValidationLeavesNativeCallButRevokesAvailability()
    {
        var foreign = new HarmonyLib.Harmony("tests.messages.forced-after");
        MethodInfo target = typeof(NativeMessage).GetMethod(nameof(NativeMessage.Draw))!;
        var patch = Create();
        try
        {
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("messages", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.True);
            foreign.Patch(target, transpiler: new HarmonyMethod(typeof(MessageDrawPatchTests), nameof(AddNops))
                { after = [OwnedHarmonyPatch.OwnerId] });
            Assert.That(patch.Available, Is.False);
            var message = new NativeMessage();
            message.Draw(19, 37);
            Assert.That(message.Window.Bounds, Is.EqualTo(new NativeRect(19, 37, 100, 25)));
        }
        finally { patch.Candidate.Rollback(); foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }

    public static IEnumerable<CodeInstruction> OutsideClickFromLocal(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var incoming = instructions.ToList();
        var local = generator.DeclareLocal(typeof(Action));
        int call = incoming.FindIndex(i => i.Calls(typeof(NativeWindow).GetMethod(nameof(NativeWindow.Register))));
        incoming[call - 1].opcode = OpCodes.Ldloc;
        incoming[call - 1].operand = local;
        incoming.InsertRange(0, [new(OpCodes.Ldnull), new(OpCodes.Stloc, local)]);
        return incoming;
    }

    public static void ShiftNativeOrigin(ref int x) => x += 5;
    public static IEnumerable<CodeInstruction> AddNops(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions) { yield return instruction; yield return new(OpCodes.Nop); }
    }
    public static IEnumerable<CodeInstruction> ChangeMeasure(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.Calls(typeof(NativeMessage).GetMethod(nameof(NativeMessage.CalculateRect))))
                instruction.operand = typeof(NativeMessage).GetMethod(nameof(NativeMessage.OtherRectangle));
            yield return instruction;
        }
    }

    [Test]
    public void NativeRegistrationAndCallbackArePreservedAndObservedExactlyOnce()
    {
        var patch = Create();
        try
        {
            observations = 0;
            Assert.That(new AdmissionCoordinator(_ => {}).Apply(new("messages", AdmissionPhase.Early,
                [patch.Candidate]), AdmissionPhase.Early), Is.True);
            var message = new NativeMessage();
            message.Draw(19, 37);
            Assert.That(observations, Is.EqualTo(1));
            Assert.That(message.Window.Bounds, Is.EqualTo(new NativeRect(19, 37, 100, 25)));
            Assert.That(message.Window.CallbackCalls, Is.Zero);
            message.Window.Callback!();
            Assert.That(message.Window.CallbackCalls, Is.EqualTo(1));
            Assert.That(patch.Shape!.MessageField.GetValue(message.Window.Callback.Target), Is.SameAs(message));
            Assert.That(patch.Shape.RectangleField.GetValue(message.Window.Callback.Target), Is.EqualTo(message.Window.Bounds));
        }
        finally { patch.Candidate.Rollback(); }
    }

    private static MessageDrawPatch Create() => new(typeof(NativeMessage).GetMethod(nameof(NativeMessage.Draw))!,
        typeof(NativeMessage).GetMethod(nameof(NativeMessage.CalculateRect))!,
        typeof(NativeWindow).GetMethod(nameof(NativeWindow.Register))!,
        typeof(MessageDrawPatchTests).GetMethod(nameof(Forward))!);

    public static void Forward(NativeWindow window, int id, NativeRect rectangle, Action callback, Action? outside, NativeMessage message)
    {
        window.Register(id, rectangle, callback, outside);
        observations++;
    }

    public readonly record struct NativeRect(float X, float Y, float Width, float Height);

    public sealed class NativeWindow
    {
        public int Id;
        public NativeRect Bounds;
        public Action? Callback;
        public int CallbackCalls;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Register(int id, NativeRect rectangle, Action callback, Action? outside) { Id = id; Bounds = rectangle; Callback = callback; }
    }

    public sealed class NativeMessage
    {
        public NativeWindow Window = new();
        public NativeRect LastBounds;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public NativeRect CalculateRect(float x, float y) => LastBounds = new(x, y, 100, 25);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public NativeRect OtherRectangle(float x, float y) => LastBounds = new(x, y, 400, 25);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Draw(int x, int y)
        {
            var rectangle = CalculateRect(x, y);
            Window.Register(7, rectangle, () => { Window.Bounds = rectangle; Window.CallbackCalls++; }, null);
        }
    }
}
