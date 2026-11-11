using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using Megamod.Patching;
using NUnit.Framework;
using HarmonyInstance = HarmonyLib.Harmony;
using Log = Megamod.Harmony.Tests.Patching.NativeLogPatchTests.SampleLog;

namespace Megamod.Harmony.Tests.Patching;

[TestFixture, NonParallelizable]
public sealed class NativeLogPublishedEffectsTests
{
    private static readonly MethodInfo Target = typeof(Log).GetMethod(nameof(Log.Warning))!;
    private static readonly MethodInfo Suppression = typeof(Log).GetMethod("get_PreventLogging", BindingFlags.Static | BindingFlags.NonPublic)!;
    private static int observations;
    private static bool observedSuppression;
    private static NativeLogPatch? refreshing;
    private static bool? reentrantResult;
    private static bool throwDuringRebuild;
    private static int markerConstructions;
    private static string markerConstructionTrace = "";
    private static int versionInspectionConstructions;
    private static readonly ManualResetEventSlim CompositionEntered = new(false);
    private static readonly ManualResetEventSlim ReleaseComposition = new(false);

    [Test]
    public void OversizedPublishedMetadataCannotPreventForeignRemovalAndLaterRecovery()
    {
        var foreign = new HarmonyInstance(new string('x', 4 * 1024 * 1024));
        int projections = 0, captures = 0;
        bool mute = false;
        var route = new NativeLogPatch(typeof(Log), nameof(Log.Warning),
            text => { projections++; return text + " [context]"; },
            capture: _ => { captures++; return !mute; });
        try
        {
            Log.Reset();
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(
                new PatchGroup("reporting", AdmissionPhase.Early, [route.Candidate]), AdmissionPhase.Early), Is.True);
            route.Enable();
            Log.Warning("before");
            Assert.That(Log.Captured, Is.EqualTo(new[] { "before [context]" }));
            Assert.That(projections, Is.EqualTo(1));
            Assert.That(captures, Is.EqualTo(1));

            foreign.Patch(Target, transpiler: new HarmonyMethod(typeof(NativeLogPublishedEffectsTests), nameof(ObserveInstructions)));
            Assert.That(route.Available, Is.False);
            Assert.That(route.CaptureAvailable, Is.False);
            Assert.DoesNotThrow(() => foreign.Unpatch(Target, HarmonyPatchType.Transpiler, foreign.Id));
            Assert.That(route.Failure, Does.Contain("4 MiB inspection limit"));
            mute = true;
            Log.Warning("fallback");
            Log.Suppressed = true;
            Log.Warning("native suppressed");
            Log.Suppressed = false;
            Assert.That(Log.Captured, Is.EqualTo(new[] { "before [context]", "fallback" }));
            Assert.That(Log.AcceptedEffects, Is.EqualTo(2));
            Assert.That(projections, Is.EqualTo(1), "The unchanged fallback body must not project text.");
            Assert.That(captures, Is.EqualTo(1), "The unchanged fallback body must not invoke owned muting.");
            Assert.That(route.RefreshPublication(), Is.False, "Metadata alone cannot admit the rejected incoming generation.");

            new HarmonyInstance("tests.megamod.log.metadata-recovery").CreateProcessor(Target).Patch();
            Assert.That(route.Available, Is.False, "A compatible rebuild still requires explicit safe publication.");
            Assert.That(route.RefreshPublication(), Is.True);
            Assert.That(route.CaptureAvailable, Is.True);
            Assert.That(route.Failure, Is.Null);
            Log.Warning("muted after recovery");
            Assert.That(captures, Is.EqualTo(2));
            Assert.That(Log.AcceptedEffects, Is.EqualTo(2));
            mute = false;
            Log.Warning("recovered");
            Assert.That(Log.Captured, Is.EqualTo(new[] { "before [context]", "fallback", "recovered [context]" }));
            Assert.That(Log.AcceptedEffects, Is.EqualTo(3));
            Assert.That(projections, Is.EqualTo(2));
            Assert.That(captures, Is.EqualTo(3));
        }
        finally
        {
            // Remove this exact owned transpiler first: the pre-fix oversized-metadata failure
            // must not strand the foreign registration during a failing regression run.
            try { new HarmonyInstance(OwnedHarmonyPatch.OwnerId).Unpatch(Target, HarmonyPatchType.Transpiler, OwnedHarmonyPatch.OwnerId); }
            finally
            {
                try { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); }
                finally { route.Candidate.Rollback(); Log.Reset(); }
            }
        }
    }

    [Test]
    public void ChangedInnerSuppressionRejectsThenRemovalAdmitsFreshGeneration()
    {
        var foreign = new HarmonyInstance("tests.megamod.inner.suppression");
        var route = new NativeLogPatch(typeof(Log), nameof(Log.Warning), text => text + " [context]");
        try
        {
            Log.Reset();
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(
                new PatchGroup("reporting", AdmissionPhase.Early, [route.Candidate]), AdmissionPhase.Early), Is.True);
            route.Enable();
            foreign.CreateProcessor(Target).AddInnerPostfix(new HarmonyMethod(
                typeof(NativeLogPublishedEffectsTests).GetMethod(nameof(ChangeSuppression))!)
                { innerMethod = new InnerMethod(Suppression) }).Patch();

            Assert.That(route.RefreshPublication(), Is.False, "The published inner result change invalidates native suppression.");
            Assert.That(route.Available, Is.False);
            Log.Suppressed = true;
            Log.Warning("foreign accepted");
            Assert.That(Log.Captured, Is.EqualTo(new[] { "foreign accepted" }));

            foreign.Unpatch(Target, HarmonyPatchType.InnerPostfix, foreign.Id);
            Assert.That(route.Available, Is.False);
            Assert.That(route.RefreshPublication(), Is.True);
            Log.Suppressed = false;
            Log.Warning("fresh");
            Assert.That(Log.Captured, Is.EqualTo(new[] { "foreign accepted", "fresh [context]" }));
        }
        finally
        {
            try { foreign.Unpatch(Target, HarmonyPatchType.InnerPostfix, foreign.Id); }
            finally { route.Candidate.Rollback(); Log.Reset(); }
        }
    }

    public static void ChangeSuppression(ref bool __result) => __result = false;

    [Test]
    public void InnerCaptureHelperFailurePreservesIndependentContext()
    {
        var foreign = new HarmonyInstance("tests.megamod.inner.capture-only");
        int captures = 0;
        var route = new NativeLogPatch(typeof(Log), nameof(Log.Warning), text => text + " [context]", capture: _ => { captures++; return false; });
        try
        {
            Log.Reset();
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(
                new PatchGroup("reporting", AdmissionPhase.Early, [route.Candidate]), AdmissionPhase.Early), Is.True);
            route.Enable();
            foreign.CreateProcessor(Target).AddInnerPostfix(new HarmonyMethod(typeof(NativeLogPublishedEffectsTests).GetMethod(nameof(ForceCapture))!)
                { innerMethod = new InnerMethod(typeof(NativeLogPatch).GetMethod("ShouldCapture", BindingFlags.NonPublic | BindingFlags.Static)!) }).Patch();
            Assert.That(route.RefreshPublication(), Is.True, "The capture-only effect must not reject working definition context.");
            Assert.That(route.Available, Is.True);
            Assert.That(route.CaptureAvailable, Is.False);
            Log.Warning("ordinary");
            Assert.That(captures, Is.Zero);
            Assert.That(Log.Captured, Is.EqualTo(new[] { "ordinary [context]" }));
        }
        finally
        {
            try { foreign.Unpatch(Target, HarmonyPatchType.InnerPostfix, foreign.Id); }
            finally { route.Candidate.Rollback(); Log.Reset(); }
        }
    }
    public static void ForceCapture(ref bool __result) => __result = true;

    [TestCase(-1, true)]
    [TestCase(1, false)]
    public void LiteralPositionsDistinguishUnrelatedEffectFromOutputProvenance(int position, bool available)
    {
        var foreign = new HarmonyInstance("tests.megamod.inner.literal-position");
        var route = new NativeLogPatch(typeof(Log), nameof(Log.Warning), text => text + " [context]");
        try
        {
            Log.Reset();
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(
                new PatchGroup("reporting", AdmissionPhase.Early, [route.Candidate]), AdmissionPhase.Early), Is.True);
            route.Enable();
            foreign.CreateProcessor(Target).AddInnerPostfix(new HarmonyMethod(typeof(NativeLogPublishedEffectsTests).GetMethod(nameof(ChangeConstant))!)
                { innerTarget = InnerTarget.Constant(1, position) }).Patch();
            Assert.That(route.RefreshPublication(), Is.EqualTo(available));
            Log.Warning("retained");
            Assert.That(Log.Captured, Is.EqualTo(new[] { available ? "retained [context]" : "retained" }));
            Assert.That(Log.AcceptedEffects, Is.EqualTo(available ? 42 : 1));
        }
        finally
        {
            try { foreign.Unpatch(Target, HarmonyPatchType.InnerPostfix, foreign.Id); }
            finally { route.Candidate.Rollback(); Log.Reset(); }
        }
    }
    public static void ChangeConstant(ref int __result) => __result = 42;

    [Test]
    public void SignedByteLiteralInspectionUsesPublishedProviderSelectorSemantics()
    {
        var foreign = new HarmonyInstance("tests.megamod.inner.signed-byte");
        try
        {
            Log.Reset();
            foreign.CreateProcessor(Target)
                .AddTranspiler(new HarmonyMethod(typeof(NativeLogPublishedEffectsTests).GetMethod(nameof(UseSignedByteConstants))!))
                .AddInnerPostfix(new HarmonyMethod(typeof(NativeLogPublishedEffectsTests).GetMethod(nameof(ChangeConstant))!)
                    { innerTarget = InnerTarget.Constant(-1, 1) }).Patch();
            Log.Warning("retained");
            Assert.That(Log.AcceptedEffects, Is.EqualTo(-1), "The actual provider selects the first minus-one literal.");
            InnerTarget selector = HarmonyInstance.GetPatchInfo(Target)!.InnerPostfixes.Single(patch => patch.owner == foreign.Id).innerTarget;
            MethodInfo matches = typeof(InnerTarget).GetMethod("Matches", BindingFlags.Instance | BindingFlags.NonPublic)!;
            var byteLiteral = new CodeInstruction(OpCodes.Ldc_I4_S, (byte)255);
            Assert.That(matches.Invoke(selector, new object[] { byteLiteral }), Is.True, "The installed provider interprets byte255 as signed minus one.");
            // This emitter rejects boxed-byte ldc.i4.s. Inspect its accepted selector semantics on
            // the literal stream without claiming that the byte form can execute on this provider.
            Assert.That(HarmonyPublication.Inspect(Target, snapshot =>
            {
                Assert.That(snapshot.FailureFor(new[] {
                    new HarmonyPublication.Operation(byteLiteral.opcode, byteLiteral.operand),
                    new HarmonyPublication.Operation(OpCodes.Ldc_I4_M1, null)
                }, new HashSet<int> { 0 }), Is.Not.Null, "The protected first occurrence must not be mistaken for the unrelated later literal.");
                return true;
            }), Is.True);
        }
        finally
        {
            foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id);
            Log.Reset();
        }
    }
    public static IEnumerable<CodeInstruction> UseSignedByteConstants(IEnumerable<CodeInstruction> instructions)
    {
        int occurrences = 0;
        foreach (var instruction in instructions)
        {
            if (instruction.opcode == OpCodes.Ldc_I4_1)
            {
                instruction.opcode = ++occurrences == 1 ? OpCodes.Ldc_I4_S : OpCodes.Ldc_I4_M1;
                instruction.operand = occurrences == 1 ? (sbyte)-1 : null;
            }
            yield return instruction;
        }
    }

    [Test]
    public void SameNameOuterMarkerCannotHideOuterTextMutationAtAnUnrelatedSite()
    {
        var foreign = new HarmonyInstance("tests.megamod.inner.same-name-outer");
        var route = new NativeLogPatch(typeof(Log), nameof(Log.Warning), text => text + " [context]");
        try
        {
            Log.Reset(); markerConstructions = 0;
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(
                new PatchGroup("reporting", AdmissionPhase.Early, [route.Candidate]), AdmissionPhase.Early), Is.True);
            route.Enable();
            foreign.CreateProcessor(Target)
                .AddTranspiler(new HarmonyMethod(typeof(NativeLogPublishedEffectsTests).GetMethod(nameof(AddUnrelatedEntryCall))!))
                .AddInnerPrefix(new HarmonyMethod(CreateSameNameOuterCallback())
                    { innerMethod = new InnerMethod(typeof(NativeLogPublishedEffectsTests).GetMethod(nameof(UnrelatedEntry))!) }).Patch();
            Assert.That(markerConstructions, Is.GreaterThan(0), "The actual provider consumed the alternate-assembly marker.");
            markerConstructions = 0;
            bool available = route.RefreshPublication();
            Assert.That(markerConstructions, Is.Zero, "Inspection must use metadata without constructing attributes. " + markerConstructionTrace);
            versionInspectionConstructions = -1;
            foreign.Patch(Target, transpiler: new HarmonyMethod(typeof(NativeLogPublishedEffectsTests).GetMethod(nameof(CheckExpectedVersion))!));
            Assert.That(versionInspectionConstructions, Is.Zero, "Expected-version inspection must also avoid constructing attributes.");
            markerConstructions = 0;
            available = route.RefreshPublication();
            Assert.That(markerConstructions, Is.Zero);
            Log.Warning("original");
            Assert.Multiple(() =>
            {
                Assert.That(available, Is.False);
                Assert.That(Log.Captured, Is.EqualTo(new[] { "changed by outer binding" }));
            });
        }
        finally
        {
            try { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); }
            finally { route.Candidate.Rollback(); Log.Reset(); }
        }
    }
    public static IEnumerable<CodeInstruction> AddUnrelatedEntryCall(IEnumerable<CodeInstruction> instructions)
    {
        yield return new CodeInstruction(OpCodes.Call, typeof(NativeLogPublishedEffectsTests).GetMethod(nameof(UnrelatedEntry))!);
        foreach (var instruction in instructions) yield return instruction;
    }

    [Test]
    public void AdmissionPreflightDoesNotConstructPublishedForeignParameterAttributes()
    {
        var foreign = new HarmonyInstance("tests.megamod.inner.preflight-marker");
        var route = new NativeLogPatch(typeof(Log), nameof(Log.Warning), text => text + " [context]");
        try
        {
            foreign.CreateProcessor(Target)
                .AddTranspiler(new HarmonyMethod(typeof(NativeLogPublishedEffectsTests).GetMethod(nameof(AddUnrelatedEntryCall))!))
                .AddInnerPrefix(new HarmonyMethod(CreateSameNameOuterCallback())
                    { innerMethod = new InnerMethod(typeof(NativeLogPublishedEffectsTests).GetMethod(nameof(UnrelatedEntry))!) }).Patch();
            markerConstructions = 0;
            TestContext.WriteLine("Published marker fixture metadata bytes=" + PublishedStateSize());
            Assert.That(route.Candidate.Validate(), Is.Null);
            Assert.That(markerConstructions, Is.Zero, "Factory and ownership preflight must inspect metadata without constructing attributes.");
        }
        finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); Log.Reset(); }
    }

    [TestCase(4190208, true)]
    [TestCase(4194304, false)]
    public void PublishedMetadataInspectionLimitReportsUnavailability(int ownerLength, bool admitted)
    {
        var foreign = new HarmonyInstance(new string('x', ownerLength));
        var route = new NativeLogPatch(typeof(Log), nameof(Log.Warning), text => text + " [context]");
        var warnings = new List<string>();
        try
        {
            Log.Reset();
            foreign.Patch(Target, transpiler: new HarmonyMethod(typeof(NativeLogPublishedEffectsTests).GetMethod(nameof(ObserveInstructions))!));
            TestContext.WriteLine("Published boundary fixture metadata bytes=" + PublishedStateSize());
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(
                new PatchGroup("reporting", AdmissionPhase.Early, [route.Candidate]), AdmissionPhase.Early), Is.EqualTo(admitted));
            if (admitted) route.Enable();
            else Assert.That(warnings.Single(), Does.Contain("4 MiB inspection limit"));
            Log.Warning("retained");
            Assert.That(Log.Captured, Is.EqualTo(new[] { admitted ? "retained [context]" : "retained" }));
        }
        finally
        {
            try { route.Candidate.Rollback(); }
            finally { foreign.Unpatch(Target, HarmonyPatchType.Transpiler, foreign.Id); Log.Reset(); }
        }
    }
    public static IEnumerable<CodeInstruction> ObserveInstructions(IEnumerable<CodeInstruction> instructions) => instructions;
    private static int PublishedStateSize()
    {
        var state = (Dictionary<MethodBase, byte[]>)typeof(PatchProcessor).Assembly.GetType("HarmonyLib.HarmonySharedState")!
            .GetField("state", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        lock (state) return state[Target].Length;
    }
    public static void UnrelatedEntry() { }
    public static void MarkerConstructed() { markerConstructions++; markerConstructionTrace = Environment.StackTrace; }
    public static IEnumerable<CodeInstruction> CheckExpectedVersion(IEnumerable<CodeInstruction> instructions)
    {
        int before = markerConstructions;
        HarmonyPublication.ExpectedVersion(Target);
        versionInspectionConstructions = markerConstructions - before;
        return instructions;
    }
    private static MethodInfo CreateSameNameOuterCallback()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("Megamod.Tests.OuterMarker." + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
        var module = assembly.DefineDynamicModule("Fixture");
        var marker = module.DefineType("HarmonyLib.HarmonyOuter", TypeAttributes.Public | TypeAttributes.Sealed, typeof(Attribute));
        var constructor = marker.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
        var constructorIl = constructor.GetILGenerator();
        constructorIl.Emit(OpCodes.Ldarg_0);
        constructorIl.Emit(OpCodes.Call, typeof(Attribute).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, Type.EmptyTypes, null)!);
        constructorIl.Emit(OpCodes.Call, typeof(NativeLogPublishedEffectsTests).GetMethod(nameof(MarkerConstructed))!);
        constructorIl.Emit(OpCodes.Ret);
        Type markerType = marker.CreateType()!;
        Assert.That(markerType, Is.Not.EqualTo(typeof(HarmonyOuter)));
        var callback = module.DefineType("OwnedCallback", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
        var method = callback.DefineMethod("ChangeText", MethodAttributes.Public | MethodAttributes.Static, typeof(void), new[] { typeof(string).MakeByRefType() });
        method.DefineParameter(1, ParameterAttributes.None, "text").SetCustomAttribute(new CustomAttributeBuilder(markerType.GetConstructor(Type.EmptyTypes)!, Array.Empty<object>()));
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldstr, "changed by outer binding");
        il.Emit(OpCodes.Stind_Ref);
        il.Emit(OpCodes.Ret);
        return callback.CreateType()!.GetMethod("ChangeText")!;
    }

    [TestCase(false)]
    [TestCase(true)]
    public void InjectedProjectionIsProtectedButAnObserverMayRemain(bool changesResult)
    {
        var foreign = new HarmonyInstance("tests.megamod.inner.projection");
        var route = new NativeLogPatch(typeof(Log), nameof(Log.Warning), text => text + " [context]");
        try
        {
            Log.Reset(); observations = 0;
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(
                new PatchGroup("reporting", AdmissionPhase.Early, [route.Candidate]), AdmissionPhase.Early), Is.True);
            route.Enable();
            foreign.CreateProcessor(Target).AddInnerPostfix(new HarmonyMethod(typeof(NativeLogPublishedEffectsTests)
                .GetMethod(changesResult ? nameof(ChangeProjection) : nameof(Observe))!)
                { innerMethod = new InnerMethod(typeof(NativeLogPatch).GetMethod("Project", BindingFlags.NonPublic | BindingFlags.Static)!) }).Patch();
            Assert.That(route.RefreshPublication(), Is.EqualTo(!changesResult));
            Assert.That(observations, Is.Zero);
            Log.Warning("retained");
            Assert.That(Log.Captured, Is.EqualTo(new[] { changesResult ? "retained [foreign]" : "retained [context]" }));
            Assert.That(observations, Is.EqualTo(1));
        }
        finally
        {
            try { foreign.Unpatch(Target, HarmonyPatchType.InnerPostfix, foreign.Id); }
            finally { route.Candidate.Rollback(); Log.Reset(); }
        }
    }
    public static void ChangeProjection(ref string __result) { observations++; __result += " [foreign]"; }

    [Test]
    public void RefreshDefersWithoutWaitingForAnotherCompilation()
    {
        var foreign = new HarmonyInstance("tests.megamod.inner.busy");
        var route = new NativeLogPatch(typeof(Log), nameof(Log.Warning), text => text + " [context]");
        Task? patching = null;
        try
        {
            Log.Reset(); CompositionEntered.Reset(); ReleaseComposition.Reset();
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(
                new PatchGroup("reporting", AdmissionPhase.Early, [route.Candidate]), AdmissionPhase.Early), Is.True);
            route.Enable();
            patching = Task.Run(() => foreign.Patch(Target, transpiler: new HarmonyMethod(typeof(NativeLogPublishedEffectsTests).GetMethod(nameof(HoldComposition))!)));
            Assert.That(CompositionEntered.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Task<bool> refresh = Task.Run(route.RefreshPublication);
            Assert.That(refresh.Wait(TimeSpan.FromSeconds(1)), Is.True, "Refresh must defer rather than wait for the provider monitor.");
            Assert.That(refresh.Result, Is.False);
            ReleaseComposition.Set();
            Assert.That(patching.Wait(TimeSpan.FromSeconds(5)), Is.True);
            Assert.That(route.RefreshPublication(), Is.True);
            Log.Warning("fresh");
            Assert.That(Log.Captured, Is.EqualTo(new[] { "fresh [context]" }));
        }
        finally
        {
            ReleaseComposition.Set();
            try { if (patching != null) Assert.That(patching.Wait(TimeSpan.FromSeconds(5)), Is.True); }
            finally
            {
                try { foreign.Unpatch(Target, HarmonyPatchType.Transpiler, foreign.Id); }
                finally { route.Candidate.Rollback(); Log.Reset(); }
            }
        }
    }
    public static IEnumerable<CodeInstruction> HoldComposition(IEnumerable<CodeInstruction> instructions)
    {
        CompositionEntered.Set();
        if (!ReleaseComposition.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("The bounded fixture did not release compilation.");
        return instructions;
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CompatibleObservationAndUnrelatedFieldEffectRemainAvailable(bool unrelated)
    {
        var foreign = new HarmonyInstance("tests.megamod.inner.compatible");
        var route = new NativeLogPatch(typeof(Log), nameof(Log.Warning), text => text + " [context]");
        try
        {
            Log.Reset(); observations = 0;
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(
                new PatchGroup("reporting", AdmissionPhase.Early, [route.Candidate]), AdmissionPhase.Early), Is.True);
            route.Enable();
            var patch = new HarmonyMethod(typeof(NativeLogPublishedEffectsTests).GetMethod(unrelated ? nameof(ChangeUnrelated) : nameof(Observe))!);
            if (unrelated) patch.innerTarget = new InnerTarget(typeof(Log).GetField(nameof(Log.AcceptedEffects))!, InnerTargetKind.FieldWrite);
            else patch.innerMethod = new InnerMethod(Suppression);
            foreign.CreateProcessor(Target).AddInnerPrefix(patch).Patch();
            Assert.That(route.RefreshPublication(), Is.True);
            Assert.That(observations, Is.Zero, "Metadata inspection must not execute the foreign observation.");
            Log.Warning("accepted");
            Assert.That(Log.Captured, Is.EqualTo(new[] { "accepted [context]" }));
            Assert.That(observations, Is.EqualTo(1));
            Assert.That(Log.AcceptedEffects, Is.EqualTo(unrelated ? 42 : 1));
        }
        finally
        {
            try { route.Candidate.Rollback(); }
            finally { foreign.Unpatch(Target, HarmonyPatchType.InnerPrefix, foreign.Id); Log.Reset(); }
        }
    }
    public static void Observe() => observations++;
    public static void ChangeUnrelated(ref int value) { observations++; value = 42; }

    [Test]
    public void ReadOnlyScalarResultObserverPreservesNativeSuppressionAndContext()
    {
        var foreign = new HarmonyInstance("tests.megamod.inner.scalar-observer");
        var route = new NativeLogPatch(typeof(Log), nameof(Log.Warning), text => text + " [context]");
        try
        {
            Log.Reset(); observations = 0; observedSuppression = false;
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(
                new PatchGroup("reporting", AdmissionPhase.Early, [route.Candidate]), AdmissionPhase.Early), Is.True);
            route.Enable();
            foreign.CreateProcessor(Target).AddInnerPostfix(new HarmonyMethod(typeof(NativeLogPublishedEffectsTests)
                .GetMethod(nameof(ObserveScalar))!) { innerMethod = new InnerMethod(Suppression) }).Patch();
            Assert.That(route.RefreshPublication(), Is.True);
            Assert.That(observations, Is.Zero, "Metadata inspection must not execute the observer.");
            Log.Suppressed = true;
            Log.Warning("suppressed");
            Assert.That(observedSuppression, Is.True);
            Assert.That(Log.Captured, Is.Empty);
            Log.Suppressed = false;
            Log.Warning("accepted");
            Assert.That(observedSuppression, Is.False);
            Assert.That(observations, Is.EqualTo(2));
            Assert.That(Log.Captured, Is.EqualTo(new[] { "accepted [context]" }));
        }
        finally
        {
            try { foreign.Unpatch(Target, HarmonyPatchType.InnerPostfix, foreign.Id); }
            finally { route.Candidate.Rollback(); Log.Reset(); }
        }
    }
    public static void ObserveScalar(bool __result) { observations++; observedSuppression = __result; }

    [TestCase(false)]
    [TestCase(true)]
    public void ReentrantRefreshAndFailedCompilationCannotAdmitOldMetadata(bool fail)
    {
        var foreign = new HarmonyInstance("tests.megamod.inner.reentrant");
        var route = new NativeLogPatch(typeof(Log), nameof(Log.Warning), text => text + " [context]");
        try
        {
            Log.Reset(); refreshing = route; reentrantResult = null; throwDuringRebuild = fail;
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(
                new PatchGroup("reporting", AdmissionPhase.Early, [route.Candidate]), AdmissionPhase.Early), Is.True);
            route.Enable();
            var late = new HarmonyMethod(typeof(NativeLogPublishedEffectsTests).GetMethod(nameof(RefreshWhileComposing))!)
                { priority = int.MinValue, after = new[] { OwnedHarmonyPatch.OwnerId } };
            if (fail) Assert.Catch(() => foreign.Patch(Target, transpiler: late));
            else foreign.Patch(Target, transpiler: late);
            Assert.That(reentrantResult, Is.False);
            Assert.That(route.RefreshPublication(), Is.False);
            Assert.That(route.Available, Is.False);
            Log.Warning("unpublished");
            Assert.That(Log.Captured, Is.EqualTo(new[] { "unpublished" }));
            throwDuringRebuild = false;
            foreign.Unpatch(Target, HarmonyPatchType.Transpiler, foreign.Id);
            Assert.That(route.RefreshPublication(), Is.True);
            Log.Warning("recovered");
            Assert.That(Log.Captured, Is.EqualTo(new[] { "unpublished", "recovered [context]" }));
        }
        finally
        {
            throwDuringRebuild = false; refreshing = null;
            try { route.Candidate.Rollback(); }
            finally { foreign.Unpatch(Target, HarmonyPatchType.Transpiler, foreign.Id); Log.Reset(); }
        }
    }
    public static IEnumerable<CodeInstruction> RefreshWhileComposing(IEnumerable<CodeInstruction> instructions)
    {
        reentrantResult = refreshing?.RefreshPublication();
        if (throwDuringRebuild) throw new InvalidOperationException("The fixture deliberately rejects this compiled generation.");
        return instructions;
    }

    [Test]
    public void OlderCompiledBodyCannotBorrowANewerPublishedGeneration()
    {
        var composer = new HarmonyInstance("tests.megamod.inner.body-generation");
        var route = new NativeLogPatch(typeof(Log), nameof(Log.Warning), text => text + " [context]");
        try
        {
            Log.Reset();
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(
                new PatchGroup("reporting", AdmissionPhase.Early, [route.Candidate]), AdmissionPhase.Early), Is.True);
            route.Enable();
            MethodInfo olderBody = composer.CreateProcessor(Target).Patch();
            Assert.That(route.RefreshPublication(), Is.True);
            composer.CreateProcessor(Target).Patch();
            Assert.That(route.RefreshPublication(), Is.True, "Identical metadata still represents a fresh completed compilation.");
            olderBody.Invoke(null, new object[] { "older" });
            Log.Warning("current");
            Assert.That(Log.Captured, Is.EqualTo(new[] { "older", "current [context]" }));
        }
        finally { route.Candidate.Rollback(); Log.Reset(); }
    }
}
