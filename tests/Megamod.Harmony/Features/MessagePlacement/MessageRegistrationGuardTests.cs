using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.MessagePlacement;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.MessagePlacement;

[TestFixture, NonParallelizable]
public sealed class MessageRegistrationGuardTests
{
    public static NativeRect OtherRectangle = new(90, 91);
    private static int parameterConstructions, methodConstructions, layerChanges, storeObservations, entryObservations, factoryCalls;

    [Test]
    public void LateInnerRectangleStoreSkipRevokesQueuedRegistration()
    {
        var guard = Create();
        var foreign = new HarmonyLib.Harmony("tests.messages.queue-inner-store");
        MethodInfo target = typeof(NativeStack).GetMethod(nameof(NativeStack.Register))!;
        try
        {
            Assert.That(Admit(guard), Is.True);
            object? before = guard.Generation;
            Assert.That(before, Is.Not.Null);
            foreign.CreateProcessor(target).AddInnerPrefix(new HarmonyMethod(typeof(MessageRegistrationGuardTests), nameof(Skip))
                { innerTarget = new InnerTarget(typeof(NativeWindow).GetField(nameof(NativeWindow.Rectangle))!, InnerTargetKind.FieldWrite) }).Patch();
            var window = new NativeWindow { Rectangle = new(90, 91) };
            var stack = new NativeStack { Window = window };
            int calls = 0;
            Action callback = () => calls++;
            stack.Register(-1, new(12, 34), 3, callback, false, false, 0, null, false);
            Assert.That(stack.Window, Is.SameAs(window));
            Assert.That(window.Rectangle, Is.EqualTo(new NativeRect(90, 91)), "The actual foreign skip remains effective.");
            Assert.That(window.Callback, Is.SameAs(callback));
            Assert.That(calls, Is.Zero);
            Assert.That(guard.Available, Is.False);
            Assert.That(guard.Generation, Is.Null);
        }
        finally
        {
            try { guard.Candidate.Rollback(); }
            finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
        }
    }

    public static bool Skip() => false;

    [TestCase(false)]
    [TestCase(true)]
    public void InnerCallbackStoreSkipRevokesEitherRegistrationPath(bool newWindow)
    {
        var guard = Create();
        var foreign = new HarmonyLib.Harmony("tests.messages.queue-inner-callback");
        MethodInfo target = newWindow ? typeof(NativeStack).GetMethod("AddNew", BindingFlags.NonPublic | BindingFlags.Instance)! :
            typeof(NativeStack).GetMethod(nameof(NativeStack.Register))!;
        try
        {
            Assert.That(Admit(guard), Is.True);
            foreign.CreateProcessor(target).AddInnerPrefix(new HarmonyMethod(typeof(MessageRegistrationGuardTests), nameof(Skip))
                { innerTarget = new InnerTarget(typeof(NativeWindow).GetField(nameof(NativeWindow.Callback))!, InnerTargetKind.FieldWrite) }).Patch();
            int calls = 0;
            Action original = () => calls++;
            var stack = new NativeStack { Window = newWindow ? null : new NativeWindow { Callback = original } };
            Action replacement = () => calls += 10;
            stack.Register(-1, new(12, 34), 3, replacement, false, false, 0, null, false);
            Assert.That(stack.Window!.Rectangle, Is.EqualTo(new NativeRect(12, 34)));
            Assert.That(stack.Window.Callback, newWindow ? Is.Null : Is.SameAs(original));
            Assert.That(calls, Is.Zero);
            Assert.That(guard.Available, Is.False);
            Assert.That(guard.Generation, Is.Null);
        }
        finally
        {
            try { guard.Candidate.Rollback(); }
            finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
        }
    }

    [Test]
    public void InnerNewWindowForwardSkipRevokesPairAndRecoversAfterRemoval()
    {
        var guard = Create();
        var foreign = new HarmonyLib.Harmony("tests.messages.queue-inner-forward");
        MethodInfo target = typeof(NativeStack).GetMethod(nameof(NativeStack.Register))!;
        MethodInfo addNew = typeof(NativeStack).GetMethod("AddNew", BindingFlags.NonPublic | BindingFlags.Instance)!;
        try
        {
            Assert.That(Admit(guard), Is.True);
            object? before = guard.Generation;
            foreign.CreateProcessor(target).AddInnerPrefix(new HarmonyMethod(typeof(MessageRegistrationGuardTests), nameof(Skip))
                { innerMethod = new InnerMethod(addNew) }).Patch();
            var stack = new NativeStack();
            Action callback = () => {};
            stack.Register(-1, new(12, 34), 3, callback, false, false, 0, null, false);
            Assert.That(stack.Window, Is.Null, "Owned admission must preserve the foreign skip.");
            Assert.That(guard.Available, Is.False);
            Assert.That(guard.Generation, Is.Null);
            foreign.Unpatch(target, HarmonyPatchType.InnerPrefix, foreign.Id);
            Assert.That(guard.Available, Is.True);
            Assert.That(guard.Generation, Is.Not.Null.And.Not.SameAs(before));
            stack.Register(-1, new(12, 34), 3, callback, false, false, 0, null, false);
            Assert.That(stack.Window!.Rectangle, Is.EqualTo(new NativeRect(12, 34)));
            Assert.That(stack.Window.Callback, Is.SameAs(callback));
        }
        finally
        {
            try { guard.Candidate.Rollback(); }
            finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CompatibleInnerEffectsPreserveNativeRegistrationWithoutInspectionSideEffects(bool newWindow)
    {
        var guard = Create();
        var foreign = new HarmonyLib.Harmony("tests.messages.queue-benign-inner");
        MethodInfo target = newWindow ? typeof(NativeStack).GetMethod("AddNew", BindingFlags.NonPublic | BindingFlags.Instance)! :
            typeof(NativeStack).GetMethod(nameof(NativeStack.Register))!;
        try
        {
            parameterConstructions = methodConstructions = layerChanges = storeObservations = entryObservations = 0;
            foreign.CreateProcessor(target).AddInnerPrefix(new HarmonyMethod(typeof(MessageRegistrationGuardTests), nameof(ChangeLayerValue))
                { innerTarget = new InnerTarget(typeof(NativeWindow).GetField(nameof(NativeWindow.Layer))!, InnerTargetKind.FieldWrite) }).Patch();
            foreign.CreateProcessor(target).AddInnerPrefix(new HarmonyMethod(typeof(MessageRegistrationGuardTests), nameof(ObserveStore))
                { innerTarget = new InnerTarget(typeof(NativeWindow).GetField(nameof(NativeWindow.Rectangle))!, InnerTargetKind.FieldWrite) }).Patch();
            foreign.Patch(target, prefix: new(typeof(MessageRegistrationGuardTests), nameof(ObserveEntry)));
            Assert.That(parameterConstructions, Is.GreaterThan(0));
            Assert.That(methodConstructions, Is.GreaterThan(0));
            parameterConstructions = methodConstructions = 0;
            Assert.That(guard.Candidate.Validate(), Is.Null);
            Assert.That(parameterConstructions + methodConstructions, Is.Zero);
            Assert.That(Admit(guard), Is.True);
            parameterConstructions = methodConstructions = 0;
            Assert.That(guard.Available, Is.True);
            object? generation = guard.Generation;
            Assert.That(generation, Is.Not.Null);
            Assert.That(guard.Generation, Is.SameAs(generation));
            Assert.That(parameterConstructions + methodConstructions, Is.Zero);
            Assert.That(layerChanges + storeObservations + entryObservations, Is.Zero);

            var stack = new NativeStack { Window = newWindow ? null : new NativeWindow() };
            int calls = 0;
            Action callback = () => calls++;
            stack.Register(-1, new(12, 34), 3, callback, false, false, 0, null, false);
            Assert.That(stack.Window!.Layer, Is.EqualTo(17));
            Assert.That(stack.Window.Rectangle, Is.EqualTo(new NativeRect(12, 34)));
            Assert.That(stack.Window.Callback, Is.SameAs(callback));
            Assert.That(calls, Is.Zero);
            Assert.That(layerChanges, Is.EqualTo(1));
            Assert.That(storeObservations, Is.EqualTo(1));
            Assert.That(entryObservations, Is.EqualTo(1));
            Assert.That(parameterConstructions + methodConstructions, Is.Zero);

            guard.Candidate.Rollback();
            if (newWindow) stack.Window = null;
            stack.Register(-1, new(56, 78), 4, callback, false, false, 0, null, false);
            Assert.That(stack.Window!.Rectangle, Is.EqualTo(new NativeRect(56, 78)));
            Assert.That(stack.Window.Layer, Is.EqualTo(17));
            Assert.That(stack.Window.Callback, Is.SameAs(callback));
            Assert.That(layerChanges, Is.EqualTo(2), "Owned rollback retains the foreign effects.");
            Assert.That(storeObservations, Is.EqualTo(2));
            Assert.That(entryObservations, Is.EqualTo(2));
        }
        finally
        {
            try { guard.Candidate.Rollback(); }
            finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
        }
    }

    public static void ChangeLayerValue([CountParameter] ref int value) { layerChanges++; value = 17; }
    public static void ObserveStore() => storeObservations++;
    [CountMethod]
    public static void ObserveEntry() => entryObservations++;
    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class CountParameterAttribute : Attribute { public CountParameterAttribute() => parameterConstructions++; }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class CountMethodAttribute : Attribute { public CountMethodAttribute() => methodConstructions++; }

    [TestCase(false)]
    [TestCase(true)]
    public void OversizedPublicationCannotBlockForeignRemovalAndPairRecovery(bool newWindow)
    {
        var guard = Create();
        var foreign = new HarmonyLib.Harmony(new string('q', 4 * 1024 * 1024));
        MethodInfo register = typeof(NativeStack).GetMethod(nameof(NativeStack.Register))!;
        MethodInfo addNew = typeof(NativeStack).GetMethod("AddNew", BindingFlags.NonPublic | BindingFlags.Instance)!;
        MethodInfo target = newWindow ? addNew : register;
        try
        {
            Assert.That(Admit(guard), Is.True);
            object? before = guard.Generation;
            foreign.Patch(target, transpiler: new(typeof(MessageRegistrationGuardTests), nameof(AddNops)));
            Assert.That(guard.Available, Is.False);
            Assert.DoesNotThrow(() => foreign.Unpatch(target, HarmonyPatchType.Transpiler, foreign.Id));
            Assert.That(guard.Generation, Is.Null);
            int calls = 0;
            var stack = new NativeStack { Window = newWindow ? null : new NativeWindow() };
            Action callback = () => calls++;
            stack.Register(-1, new(12, 34), 3, callback, false, false, 0, null, false);
            Assert.That(stack.Window!.Rectangle, Is.EqualTo(new NativeRect(12, 34)));
            Assert.That(stack.Window.Callback, Is.SameAs(callback));
            Assert.That(calls, Is.Zero);
            stack.Draw();
            Assert.That(calls, Is.EqualTo(1));
            new HarmonyLib.Harmony("tests.messages.queue-recovery").CreateProcessor(target).Patch();
            Assert.That(guard.Available, Is.True);
            Assert.That(guard.Generation, Is.Not.Null.And.Not.SameAs(before));
        }
        finally
        {
            try { new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId).Unpatch(register, HarmonyPatchType.Transpiler, OwnedHarmonyPatch.OwnerId); }
            finally
            {
                try { new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId).Unpatch(addNew, HarmonyPatchType.Transpiler, OwnedHarmonyPatch.OwnerId); }
                finally
                {
                    try { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
                    finally { guard.Candidate.Rollback(); }
                }
            }
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void LaterTranspilerFactoryRevokesPairWithoutInspectionExecution(bool newWindow)
    {
        var guard = Create();
        var foreign = new HarmonyLib.Harmony("tests.messages.queue-factory");
        MethodInfo target = newWindow ? typeof(NativeStack).GetMethod("AddNew", BindingFlags.NonPublic | BindingFlags.Instance)! :
            typeof(NativeStack).GetMethod(nameof(NativeStack.Register))!;
        try
        {
            Assert.That(Admit(guard), Is.True);
            factoryCalls = 0;
            foreign.Patch(target, transpiler: new(typeof(MessageRegistrationGuardTests), nameof(TranspilerFactory)));
            Assert.That(factoryCalls, Is.GreaterThan(0), "Normal provider composition executes the factory.");
            factoryCalls = 0;
            Assert.That(guard.Available, Is.False);
            Assert.That(guard.Generation, Is.Null);
            Assert.That(factoryCalls, Is.Zero, "Inspection must reject before sorting executes a factory.");
            guard.Candidate.Rollback();
            Assert.That(HarmonyPublication.Inspect(target, snapshot =>
                snapshot.Transpilers.Any(patch => patch.owner == foreign.Id)), Is.True);
            var stack = new NativeStack();
            Action callback = () => {};
            stack.Register(-1, new(12, 34), 3, callback, false, false, 0, null, false);
            Assert.That(stack.Window!.Rectangle, Is.EqualTo(new NativeRect(12, 34)));
            Assert.That(stack.Window.Callback, Is.SameAs(callback));
        }
        finally
        {
            try { guard.Candidate.Rollback(); }
            finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
        }
    }
    public static MethodInfo TranspilerFactory(MethodBase original)
    {
        factoryCalls++;
        return typeof(MessageRegistrationGuardTests).GetMethod(nameof(AddNops))!;
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RepatchPublishesFreshGenerationOnlyForStillSupportedRegistration(bool incompatible)
    {
        var guard = Create();
        var foreign = new HarmonyLib.Harmony("tests.messages.queue-repatch");
        MethodInfo target = typeof(NativeStack).GetMethod(nameof(NativeStack.Register))!;
        try
        {
            Assert.That(Admit(guard), Is.True);
            object? before = guard.Generation;
            Assert.That(before, Is.Not.Null);
            foreign.Patch(target, transpiler: new(typeof(MessageRegistrationGuardTests),
                incompatible ? nameof(ChangeForwardedCallback) : nameof(AddNops)));
            Assert.That(guard.Available, Is.EqualTo(!incompatible));
            if (incompatible) Assert.That(guard.Generation, Is.Null);
            else Assert.That(guard.Generation, Is.Not.Null.And.Not.SameAs(before));
            var stack = new NativeStack();
            Action callback = () => {};
            stack.Register(-1, new(12, 34), 3, callback, false, false, 0, null, false);
            Assert.That(stack.Window!.Callback, incompatible ? Is.Null : Is.SameAs(callback));
        }
        finally { guard.Candidate.Rollback(); foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }
    [Test]
    public void ExplicitLaterTransformCannotAuthorizeUnobservedRegistration()
    {
        var guard = Create();
        var foreign = new HarmonyLib.Harmony("tests.messages.queue-forced-after");
        MethodInfo target = typeof(NativeStack).GetMethod(nameof(NativeStack.Register))!;
        try
        {
            Assert.That(Admit(guard), Is.True);
            foreign.Patch(target, transpiler: new HarmonyMethod(typeof(MessageRegistrationGuardTests), nameof(AddNops))
                { after = [OwnedHarmonyPatch.OwnerId] });
            Assert.That(guard.Available, Is.False);
            Assert.That(guard.Generation, Is.Null);
            var stack = new NativeStack();
            stack.Register(-1, new(12, 34), 3, () => {}, false, false, 0, null, false);
            Assert.That(stack.Window!.Rectangle, Is.EqualTo(new NativeRect(12, 34)));
        }
        finally { guard.Candidate.Rollback(); foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }
    public static IEnumerable<CodeInstruction> AddNops(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions) { yield return instruction; yield return new(OpCodes.Nop); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MutatingRectangleThroughAnAddressDoesNotRetainItsOriginalProvenance(bool localAlias)
    {
        var guard = Create();
        var foreign = new HarmonyLib.Harmony("tests.messages.rectangle-address");
        MethodInfo target = typeof(NativeStack).GetMethod(nameof(NativeStack.Register))!;
        try
        {
            foreign.Patch(target, transpiler: new(typeof(MessageRegistrationGuardTests),
                localAlias ? nameof(MutateRectangleAlias) : nameof(MutateRectangleArgument)));
            Assert.That(Admit(guard), Is.False);
            var stack = new NativeStack { Window = new NativeWindow() };
            stack.Register(-1, new(12, 34), 3, () => {}, false, false, 0, null, false);
            Assert.That(stack.Window.Rectangle, Is.EqualTo(default(NativeRect)));
        }
        finally { guard.Candidate.Rollback(); foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }
    public static IEnumerable<CodeInstruction> MutateRectangleArgument(IEnumerable<CodeInstruction> instructions)
    {
        yield return new(OpCodes.Ldarga_S, (byte)2);
        yield return new(OpCodes.Initobj, typeof(NativeRect));
        foreach (var instruction in instructions) yield return instruction;
    }
    public static IEnumerable<CodeInstruction> MutateRectangleAlias(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var local = generator.DeclareLocal(typeof(NativeRect));
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Stfld && Equals(instruction.operand, typeof(NativeWindow).GetField(nameof(NativeWindow.Rectangle))))
            {
                yield return new(OpCodes.Stloc, local);
                yield return new(OpCodes.Ldloca, local);
                yield return new(OpCodes.Initobj, typeof(NativeRect));
                yield return new(OpCodes.Ldloc, local);
            }
            yield return instruction;
        }
    }

    [Test]
    public void RetainedRectangleAddressCannotRegainTrustedProvenanceAfterAssignment()
    {
        var guard = Create();
        var foreign = new HarmonyLib.Harmony("tests.messages.retained-rectangle-address");
        MethodInfo target = typeof(NativeStack).GetMethod(nameof(NativeStack.Register))!;
        try
        {
            foreign.Patch(target, transpiler: new(typeof(MessageRegistrationGuardTests), nameof(MutateThroughRetainedRectangleAddress)));
            var stack = new NativeStack { Window = new NativeWindow() };
            Action callback = () => {};
            stack.Register(-1, new(12, 34), 3, callback, false, false, 0, null, false);
            Assert.That(stack.Window.Rectangle, Is.EqualTo(default(NativeRect)), "The retained address really mutates the reassigned local.");
            Assert.That(Admit(guard), Is.False);
            Assert.That(guard.Generation, Is.Null);
            stack.Register(-1, new(56, 78), 3, callback, false, false, 0, null, false);
            Assert.That(stack.Window.Rectangle, Is.EqualTo(default(NativeRect)), "Rejection must retain the foreign registration.");
            Assert.That(stack.Window.Callback, Is.SameAs(callback));
        }
        finally { guard.Candidate.Rollback(); foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }
    public static IEnumerable<CodeInstruction> MutateThroughRetainedRectangleAddress(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var local = generator.DeclareLocal(typeof(NativeRect));
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Stfld && Equals(instruction.operand, typeof(NativeWindow).GetField(nameof(NativeWindow.Rectangle))))
            {
                yield return new(OpCodes.Pop); // Replace the rectangle value, retaining its window receiver.
                yield return new(OpCodes.Ldloca, local);
                yield return new(OpCodes.Ldarg_2);
                yield return new(OpCodes.Stloc, local);
                yield return new(OpCodes.Initobj, typeof(NativeRect)); // The earlier address survives stloc.
                yield return new(OpCodes.Ldloc, local);
            }
            yield return instruction;
        }
    }

    [Test]
    public void OverwritingTheRectangleArgumentDoesNotProveOriginalRectangleStorage()
    {
        var guard = Create();
        var foreign = new HarmonyLib.Harmony("tests.messages.overwritten-argument");
        MethodInfo target = typeof(NativeStack).GetMethod(nameof(NativeStack.Register))!;
        try
        {
            foreign.Patch(target, transpiler: new(typeof(MessageRegistrationGuardTests), nameof(OverwriteRectangleArgument)));
            Assert.That(Admit(guard), Is.False);
            var stack = new NativeStack();
            stack.Register(-1, new(12, 34), 3, () => {}, false, false, 0, null, false);
            Assert.That(stack.Window!.Rectangle, Is.EqualTo(OtherRectangle));
        }
        finally { guard.Candidate.Rollback(); foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }
    public static IEnumerable<CodeInstruction> OverwriteRectangleArgument(IEnumerable<CodeInstruction> instructions)
    {
        yield return new(OpCodes.Ldsfld, typeof(MessageRegistrationGuardTests).GetField(nameof(OtherRectangle)));
        yield return new(OpCodes.Starg_S, (byte)2);
        foreach (var instruction in instructions) yield return instruction;
    }

    [Test]
    public void CompatibleLocalAliasesAndUnrelatedPrefixRemainEffective()
    {
        var guard = Create();
        var foreign = new HarmonyLib.Harmony("tests.messages.compatible-queue");
        MethodInfo register = typeof(NativeStack).GetMethod(nameof(NativeStack.Register))!;
        MethodInfo addNew = typeof(NativeStack).GetMethod("AddNew", BindingFlags.NonPublic | BindingFlags.Instance)!;
        try
        {
            foreign.Patch(register, prefix: new(typeof(MessageRegistrationGuardTests), nameof(ChangeLayer)),
                transpiler: new(typeof(MessageRegistrationGuardTests), nameof(AliasStoredArguments)));
            foreign.Patch(addNew, transpiler: new(typeof(MessageRegistrationGuardTests), nameof(AliasStoredArguments)));
            Assert.That(Admit(guard), Is.True);
            var stack = new NativeStack();
            Action callback = () => {};
            stack.Register(-1, new(12, 34), 3, callback, false, false, 0, null, false);
            Assert.That(stack.Window!.Rectangle, Is.EqualTo(new NativeRect(12, 34)));
            Assert.That(stack.Window.Callback, Is.SameAs(callback));
            Assert.That(stack.Window.Layer, Is.EqualTo(7));
            guard.Candidate.Rollback();
            stack.Register(-1, new(45, 67), 3, callback, false, false, 0, null, false);
            Assert.That(stack.Window.Layer, Is.EqualTo(7), "Owned rollback must retain the foreign prefix.");
        }
        finally
        {
            guard.Candidate.Rollback();
            foreign.Unpatch(register, HarmonyPatchType.All, foreign.Id);
            foreign.Unpatch(addNew, HarmonyPatchType.All, foreign.Id);
        }
    }
    public static void ChangeLayer(ref int layer) => layer = 7;
    public static IEnumerable<CodeInstruction> AliasStoredArguments(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Stfld && instruction.operand is FieldInfo field &&
                (field.Name == nameof(NativeWindow.Rectangle) || field.Name == nameof(NativeWindow.Callback)))
            {
                var alias = generator.DeclareLocal(field.FieldType);
                yield return new(OpCodes.Stloc, alias);
                yield return new(OpCodes.Nop);
                yield return new(OpCodes.Ldloc, alias);
            }
            yield return instruction;
        }
    }

    [Test]
    public void DirectDrawingCallbackInvocationDuringRegistrationIsRejectedWithoutSuppressingIt()
    {
        var guard = Create();
        var foreign = new HarmonyLib.Harmony("tests.messages.early-draw");
        MethodInfo target = typeof(NativeStack).GetMethod(nameof(NativeStack.Register))!;
        try
        {
            foreign.Patch(target, transpiler: new(typeof(MessageRegistrationGuardTests), nameof(InvokeDrawingCallback)));
            Assert.That(Admit(guard), Is.False);
            int calls = 0;
            new NativeStack().Register(-1, new(12, 34), 3, () => calls++, false, false, 0, null, false);
            Assert.That(calls, Is.EqualTo(1));
        }
        finally { guard.Candidate.Rollback(); foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }
    public static IEnumerable<CodeInstruction> InvokeDrawingCallback(IEnumerable<CodeInstruction> instructions)
    {
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ret)
            {
                var argument = new CodeInstruction(OpCodes.Ldarg_S, (byte)4);
                argument.labels.AddRange(instruction.labels);
                instruction.labels.Clear();
                yield return argument;
                yield return new(OpCodes.Callvirt, typeof(Action).GetMethod(nameof(Action.Invoke)));
            }
            yield return instruction;
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NewWindowMustReceiveTheSameRectangleAndDrawingCallback(bool changeRectangle)
    {
        var guard = Create();
        var foreign = new HarmonyLib.Harmony("tests.messages.changed-forwarding");
        MethodInfo target = typeof(NativeStack).GetMethod(nameof(NativeStack.Register))!;
        try
        {
            foreign.Patch(target, transpiler: new(typeof(MessageRegistrationGuardTests),
                changeRectangle ? nameof(ChangeForwardedRectangle) : nameof(ChangeForwardedCallback)));
            Assert.That(Admit(guard), Is.False);
            var stack = new NativeStack();
            var bounds = new NativeRect(12, 34);
            Action callback = () => {};
            stack.Register(-1, bounds, 3, callback, false, false, 0, null, false);
            Assert.That(stack.Window!.Rectangle, Is.EqualTo(changeRectangle ? OtherRectangle : bounds));
            Assert.That(stack.Window.Callback, changeRectangle ? Is.SameAs(callback) : Is.Null);
        }
        finally { guard.Candidate.Rollback(); foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }

    public static IEnumerable<CodeInstruction> ChangeForwardedRectangle(IEnumerable<CodeInstruction> instructions) =>
        ChangeForwarding(instructions, 8, new(OpCodes.Ldsfld, typeof(MessageRegistrationGuardTests).GetField(nameof(OtherRectangle))));
    public static IEnumerable<CodeInstruction> ChangeForwardedCallback(IEnumerable<CodeInstruction> instructions) =>
        ChangeForwarding(instructions, 6, new(OpCodes.Ldarg_S, (byte)8));
    private static IEnumerable<CodeInstruction> ChangeForwarding(IEnumerable<CodeInstruction> instructions, int before, CodeInstruction replacement)
    {
        var incoming = instructions.Select(instruction => new CodeInstruction(instruction)).ToList();
        int call = incoming.FindIndex(instruction => instruction.Calls(typeof(NativeStack).GetMethod("AddNew", BindingFlags.NonPublic | BindingFlags.Instance)));
        incoming[call - before].opcode = replacement.opcode;
        incoming[call - before].operand = replacement.operand;
        return incoming;
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RectangleAndCallbackStoredOnDifferentWindowsDoNotAuthorizePlacement(bool newWindow)
    {
        var guard = Create();
        var foreign = new HarmonyLib.Harmony("tests.messages.split-window");
        MethodInfo target = typeof(NativeStack).GetMethod(newWindow ? "AddNew" : nameof(NativeStack.Register),
            BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
        try
        {
            foreign.Patch(target, transpiler: new(typeof(MessageRegistrationGuardTests), nameof(SplitCallbackReceiver)));
            Assert.That(Admit(guard), Is.False);
            var stack = new NativeStack { Window = newWindow ? null : new NativeWindow() };
            var bounds = new NativeRect(12, 34);
            stack.Register(-1, bounds, 3, () => {}, false, false, 0, null, false);
            Assert.That(stack.Window!.Rectangle, Is.EqualTo(bounds));
            Assert.That(stack.Window.Callback, Is.Null, "Guard rejection must retain the foreign registration unchanged.");
        }
        finally { guard.Candidate.Rollback(); foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); }
    }

    public static IEnumerable<CodeInstruction> SplitCallbackReceiver(IEnumerable<CodeInstruction> instructions)
    {
        var incoming = instructions.Select(instruction => new CodeInstruction(instruction)).ToList();
        int store = incoming.FindIndex(instruction => instruction.opcode == OpCodes.Stfld &&
            Equals(instruction.operand, typeof(NativeWindow).GetField(nameof(NativeWindow.Callback))));
        incoming[store - 2].opcode = OpCodes.Newobj;
        incoming[store - 2].operand = typeof(NativeWindow).GetConstructor(Type.EmptyTypes);
        return incoming;
    }

    [Test]
    public void ExistingAndNewWindowsKeepExactQueuedRectangleAndCallback()
    {
        var guard = Create();
        try
        {
            Assert.That(Admit(guard), Is.True);
            Assert.That(guard.Available, Is.True);
            var stack = new NativeStack();
            int calls = 0;
            Action callback = () => calls++;
            var first = new NativeRect(10, 20);
            stack.Register(-1, first, 3, callback, false, false, 0, null, false);
            Assert.That(stack.Window!.Rectangle, Is.EqualTo(first));
            Assert.That(stack.Window.Callback, Is.SameAs(callback));
            var window = stack.Window;
            var second = new NativeRect(30, 40);
            stack.Register(-1, second, 3, callback, false, false, 0, null, false);
            Assert.That(stack.Window, Is.SameAs(window));
            Assert.That(window.Rectangle, Is.EqualTo(second));
            Assert.That(calls, Is.Zero, "Registration must not invoke the native callback.");
            stack.Draw();
            Assert.That(calls, Is.EqualTo(1));
        }
        finally { guard.Candidate.Rollback(); }
    }

    private static bool Admit(MessageRegistrationGuard guard) =>
        new AdmissionCoordinator(_ => {}).Apply(new("messages", AdmissionPhase.Early, [guard.Candidate]), AdmissionPhase.Early);
    private static MessageRegistrationGuard Create() => new(
        typeof(NativeStack).GetMethod(nameof(NativeStack.Register))!,
        typeof(NativeStack).GetMethod("AddNew", BindingFlags.NonPublic | BindingFlags.Instance)!,
        typeof(NativeWindow).GetField(nameof(NativeWindow.Rectangle))!,
        typeof(NativeWindow).GetField(nameof(NativeWindow.Callback))!);

    public readonly record struct NativeRect(int X, int Y);
    public sealed class NativeWindow
    {
        public NativeRect Rectangle;
        public Action? Callback;
        public Action? Outside;
        public int Layer;
    }
    public sealed class NativeStack
    {
        public NativeWindow? Window;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Register(int id, NativeRect rectangle, int layer, Action callback, bool background,
            bool absorb, float shadow, Action? outside, bool ignoreFader)
        {
            if (Window != null)
            {
                NativeWindow window = Window;
                window.Rectangle = rectangle;
                window.Callback = callback;
                window.Outside = outside;
                window.Layer = layer;
            }
            else AddNew(id, rectangle, layer, callback, background, absorb, shadow, outside, ignoreFader);
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        private void AddNew(int id, NativeRect rectangle, int layer, Action callback, bool background,
            bool absorb, float shadow, Action? outside, bool ignoreFader)
        {
            var window = new NativeWindow();
            window.Layer = layer;
            window.Callback = callback;
            window.Outside = outside;
            window.Rectangle = rectangle;
            Window = window;
        }
        public void Draw() => Window!.Callback!();
    }
}
