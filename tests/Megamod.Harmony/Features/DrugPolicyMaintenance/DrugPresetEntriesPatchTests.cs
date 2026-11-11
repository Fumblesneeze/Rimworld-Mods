using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.DrugPolicyMaintenance;
using Megamod.Features.DrugPolicyMaintenance.Harmony;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.HarmonyTests.Features.DrugPolicyMaintenance;

[TestFixture, NonParallelizable]
public sealed class DrugPresetEntriesPatchTests
{
    [Test]
    public void BranchOntoCreationStoreCannotSubstituteAnAlternatePolicy() => CheckCreationEntry("store", false, false);

    [TestCase("store", true, false)]
    [TestCase("store", false, true)]
    [TestCase("store", true, true)]
    [TestCase("call", false, false)]
    [TestCase("call", true, true)]
    public void BranchAndSwitchEntriesIntoCreationChainAndNopsAreRejected(string destination, bool useSwitch, bool landingNop) =>
        CheckCreationEntry(destination, useSwitch, landingNop);

    [TestCase(false, false)]
    [TestCase(true, false)]
    [TestCase(false, true)]
    [TestCase(true, true)]
    public void EntryAtReceiverOrPrecedingNopRetainsCompleteCreationAndGuard(bool useSwitch, bool landingNop) =>
        CheckCreationEntry("receiver", useSwitch, landingNop, admitted: true);

    private static string creationDestination = "store";
    private static bool creationSwitch, creationNop;
    private static Database? replacementDatabase;
    private static void CheckCreationEntry(string destination, bool useSwitch, bool landingNop, bool admitted = false)
    {
        creationDestination = destination; creationSwitch = useSwitch; creationNop = landingNop;
        var drug = new object();
        var database = new Database(drug);
        var preset = new Preset { Entries = new() { new() { Drug = drug, Quantity = 17 } } };
        replacementPolicy = new Policy(new[] { drug });
        replacementDatabase = new Database(drug);
        var foreign = new HarmonyLib.Harmony("test.drug.preset.creation-store");
        var warnings = new List<string>();
        var patch = Patch(() => new PresetEntriesRead(new[] { drug }, () => true), warnings.Add);
        try
        {
            foreign.Patch(Target, transpiler: new HarmonyMethod(typeof(DrugPresetEntriesPatchTests), nameof(SubstituteCreatedPolicy)));
            var result = database.Build(preset);
            Assert.That(result, Is.SameAs(destination == "store" ? replacementPolicy :
                destination == "call" ? replacementDatabase.Created : database.Created), "Exercise the actual branch and its supplied stack value.");
            if (destination != "receiver") Assert.That(database.Created, Is.Null, "The original receiver's Create did not execute.");
            Assert.That(result[drug].Quantity, Is.EqualTo(17));
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(Group(patch), AdmissionPhase.DefsReady), Is.EqualTo(admitted));
            Assert.That(patch.Available, Is.EqualTo(admitted));
            if (admitted)
            {
                patch.Enable();
                preset.Entries.Insert(0, new Entry { Drug = new object() });
                result = database.Build(preset);
                Assert.That(result, Is.SameAs(database.Created), "Entry before the receiver still executes the complete creation chain.");
                Assert.That(result[drug].Quantity, Is.EqualTo(17), "The admitted guard skips only the unavailable entry before the native copy.");
                Assert.That(preset.Entries, Has.Count.EqualTo(2));
                Assert.That(warnings, Is.Empty);
            }
            else Assert.That(warnings.Single(), Does.Contain("loop changed"));
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(Target)!.Owners, Does.Contain(foreign.Id));
            Assert.That(database.Build(preset)[drug].Quantity, Is.EqualTo(17), "Rejected admission leaves foreign/native execution intact.");
        }
        finally
        {
            patch.Candidate.Rollback(); foreign.UnpatchAll(foreign.Id);
            replacementPolicy = null; replacementDatabase = null;
            creationDestination = "store"; creationSwitch = creationNop = false;
        }
    }
    public static IEnumerable<CodeInstruction> SubstituteCreatedPolicy(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var body = instructions.Select(instruction => new CodeInstruction(instruction)).ToList();
        int create = body.FindIndex(instruction => instruction.Calls(typeof(Database).GetMethod(nameof(Database.Create))!));
        int destination = create + (creationDestination == "store" ? 1 : creationDestination == "call" ? 0 : -1);
        Label entry = generator.DefineLabel();
        if (creationNop) body.Insert(destination, new CodeInstruction(OpCodes.Nop));
        body[destination].labels.Add(entry);
        var prefix = new List<CodeInstruction>();
        if (creationDestination != "receiver")
            prefix.Add(new CodeInstruction(OpCodes.Ldsfld, typeof(DrugPresetEntriesPatchTests).GetField(
                creationDestination == "store" ? nameof(replacementPolicy) : nameof(replacementDatabase),
                BindingFlags.NonPublic | BindingFlags.Static)!));
        if (creationSwitch)
        {
            prefix.Add(new CodeInstruction(OpCodes.Ldc_I4_0));
            prefix.Add(new CodeInstruction(OpCodes.Switch, new[] { entry }));
            if (creationDestination != "receiver") prefix.Add(new CodeInstruction(OpCodes.Pop));
        }
        else prefix.Add(new CodeInstruction(OpCodes.Br, entry));
        body.InsertRange(0, prefix);
        return body;
    }

    [Test]
    public void CaptureResultReplacementIsUnadmittedWithoutRunningItsCallbackDuringInspection()
    {
        var foreign = new HarmonyLib.Harmony("test.drug.preset.capture-result");
        var patch = Patch(() => new PresetEntriesRead(Array.Empty<object>(), () => true), _ => { });
        try
        {
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(Group(patch), AdmissionPhase.DefsReady), Is.True);
            patch.Enable();
            foreign.CreateProcessor(Target).AddInnerPostfix(new HarmonyMethod(typeof(DrugPresetEntriesPatchTests), nameof(ReplaceRead))
                { innerMethod = new InnerMethod(typeof(DrugPresetEntriesPatch).GetMethod("Capture", BindingFlags.NonPublic | BindingFlags.Static)!) }).Patch();
            observed = 0;
            Assert.That(patch.RefreshPublication(), Is.False);
            Assert.That(patch.Available, Is.False);
            Assert.That(observed, Is.Zero);
            // This unsupported writer can forge the emitted local. Do not claim native fallback
            // from a test that merely checks availability; remove it before exercising recovery.
            foreign.Unpatch(Target, HarmonyPatchType.InnerPostfix, foreign.Id);
            Assert.That(patch.RefreshPublication(), Is.True);
            Assert.DoesNotThrow(() => new Database().Build(new Preset { Entries = new() { new() { Drug = new object() } } }));
        }
        finally
        {
            try { foreign.Unpatch(Target, HarmonyPatchType.InnerPostfix, foreign.Id); }
            finally { patch.Candidate.Rollback(); }
        }
    }
    public static void ReplaceRead(ref PresetEntriesRead? __result)
    { observed++; __result = new PresetEntriesRead(Array.Empty<object>(), () => true); }

    [Test]
    public void RepatchDuringBaselineCaptureCannotAuthorizeTheOlderExecutingBody()
    {
        var foreign = new HarmonyLib.Harmony("test.drug.preset.capture-repatch");
        bool repatched = false;
        var patch = Patch(() =>
        {
            if (!repatched)
            {
                repatched = true;
                foreign.Patch(Target, transpiler: new HarmonyMethod(typeof(DrugPresetEntriesPatchTests), nameof(Identity)));
            }
            return new PresetEntriesRead(Array.Empty<object>(), () => true);
        }, _ => { });
        try
        {
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(Group(patch), AdmissionPhase.DefsReady), Is.True);
            patch.Enable();
            var preset = new Preset { Entries = new() { new() { Drug = new object() } } };
            Assert.Throws<ArgumentException>(() => new Database().Build(preset));
            Assert.That(patch.Available, Is.False);
            Assert.That(patch.RefreshPublication(), Is.True);
            Assert.DoesNotThrow(() => new Database().Build(preset));
        }
        finally { patch.Candidate.Rollback(); foreign.UnpatchAll(foreign.Id); }
    }

    [Test]
    public void BroadPermissionAndLastExplicitChoicesRemainNativeWithDetachedMembership()
    {
        var present = new object(); var broad = new object();
        var identities = new List<object> { present, broad };
        var read = new PresetEntriesRead(identities, () => true);
        identities.Clear();
        var explicitChoice = new Entry { Drug = present, Addiction = true, Recreation = false, Scheduled = true,
            Days = 2.5f, Mood = 0.3f, Joy = 0.4f, Quantity = 8 };
        var preset = new Preset { AllowPleasure = true, Entries = new() { new() { Drug = present, Quantity = 1 }, explicitChoice } };
        var patch = Patch(() => read, _ => { });
        try
        {
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(Group(patch), AdmissionPhase.DefsReady), Is.True);
            patch.Enable();
            var result = new Database(present, broad).Build(preset);
            Assert.That(result.CopyOrder, Is.EqualTo(new[] { present, present }));
            Assert.That(result[broad].Recreation, Is.True);
            Assert.That((result[present].Addiction, result[present].Recreation, result[present].Scheduled,
                result[present].Days, result[present].Mood, result[present].Joy, result[present].Quantity),
                Is.EqualTo((true, false, true, 2.5f, 0.3f, 0.4f, 8)));
        }
        finally { patch.Candidate.Rollback(); }
    }

    [Test]
    public void OuterPostfixMayTransformTheResultWithoutChangingOwnedNativeCopies()
    {
        var present = new object();
        replacementPolicy = new Policy(Array.Empty<object>());
        var foreign = new HarmonyLib.Harmony("test.drug.preset.postfix");
        var patch = Patch(() => new PresetEntriesRead(new[] { present }, () => true), _ => { });
        try
        {
            foreign.Patch(Target, postfix: new HarmonyMethod(typeof(DrugPresetEntriesPatchTests), nameof(TransformResult)));
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(Group(patch), AdmissionPhase.DefsReady), Is.True);
            patch.Enable();
            var database = new Database(present);
            Assert.That(database.Build(new Preset { Entries = new() { new() { Drug = new object() }, new() { Drug = present, Quantity = 5 } } }),
                Is.SameAs(replacementPolicy));
            Assert.That(database.Created![present].Quantity, Is.EqualTo(5));
        }
        finally { patch.Candidate.Rollback(); foreign.UnpatchAll(foreign.Id); replacementPolicy = null; }
    }
    public static Policy TransformResult(Policy __result) => replacementPolicy!;

    [Test]
    public void OversizedMetadataDoesNotPreventForeignRemovalAndARealLaterRebuildRecovers()
    {
        var foreign = new HarmonyLib.Harmony(new string('x', 4 * 1024 * 1024));
        var patch = Patch(() => new PresetEntriesRead(Array.Empty<object>(), () => true), _ => { });
        try
        {
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(Group(patch), AdmissionPhase.DefsReady), Is.True);
            patch.Enable();
            foreign.Patch(Target, transpiler: new HarmonyMethod(typeof(DrugPresetEntriesPatchTests), nameof(Identity)));
            Assert.That(patch.RefreshPublication(), Is.False);
            Assert.DoesNotThrow(() => foreign.Unpatch(Target, HarmonyPatchType.Transpiler, foreign.Id));
            Assert.That(patch.Available, Is.False);
            var missing = new Preset { Entries = new() { new() { Drug = new object() } } };
            Assert.Throws<ArgumentException>(() => new Database().Build(missing));
            new HarmonyLib.Harmony("test.drug.preset.recovery").CreateProcessor(Target).Patch();
            Assert.That(patch.RefreshPublication(), Is.True);
            Assert.DoesNotThrow(() => new Database().Build(missing));
        }
        finally
        {
            try { foreign.Unpatch(Target, HarmonyPatchType.Transpiler, foreign.Id); }
            finally { patch.Candidate.Rollback(); }
        }
    }

    private static int factoryCalls;
    [Test]
    public void FactoryRegistrationIsRejectedWithoutInspectionExecutingIt()
    {
        var foreign = new HarmonyLib.Harmony("test.drug.preset.factory");
        var patch = Patch(() => null, _ => { });
        try
        {
            foreign.Patch(Target, transpiler: new HarmonyMethod(typeof(DrugPresetEntriesPatchTests), nameof(Factory)));
            int before = factoryCalls;
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(Group(patch), AdmissionPhase.DefsReady), Is.False);
            Assert.That(factoryCalls, Is.EqualTo(before));
        }
        finally { patch.Candidate.Rollback(); foreign.UnpatchAll(foreign.Id); }
    }
    public static MethodInfo Factory(MethodBase original) { factoryCalls++; return typeof(DrugPresetEntriesPatchTests).GetMethod(nameof(Identity))!; }

    private static string changedShape = "";
    private static string[] suppliedShape = Array.Empty<string>();
    private static bool unchanged;
    [TestCase("source")]
    [TestCase("ambiguous")]
    [TestCase("bypass")]
    [TestCase("creation")]
    public void ChangedInputReturnsTheActualSuppliedStreamUnmodified(string shape)
    {
        changedShape = shape; unchanged = true;
        var foreign = new HarmonyLib.Harmony("test.drug.preset.shape");
        var warnings = new List<string>();
        var patch = Patch(() => null, warnings.Add);
        try
        {
            foreign.Patch(Target, transpiler: new HarmonyMethod(typeof(DrugPresetEntriesPatchTests), nameof(ChangeShape)));
            foreign.Patch(Target, transpiler: new HarmonyMethod(typeof(DrugPresetEntriesPatchTests), nameof(ObserveUnchanged))
                { priority = int.MinValue, after = new[] { OwnedHarmonyPatch.OwnerId } });
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(Group(patch), AdmissionPhase.DefsReady), Is.False);
            Assert.That(warnings.Single(), Does.Contain("loop changed"));
            Assert.That(unchanged, Is.True, "The failed validator must leave supplied operands, labels and exception blocks untouched.");
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(Target)!.Owners, Does.Contain(foreign.Id));
        }
        finally { patch.Candidate.Rollback(); foreign.UnpatchAll(foreign.Id); changedShape = ""; }
    }
    public static IEnumerable<CodeInstruction> ChangeShape(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var body = instructions.Select(instruction => new CodeInstruction(instruction)).ToList();
        int lookup = body.FindIndex(instruction => instruction.Calls(typeof(Policy).GetProperty("Item")!.GetMethod!));
        int copy = body.FindIndex(instruction => instruction.Calls(typeof(Entry).GetMethod(nameof(Entry.CopyFrom))!));
        if (changedShape == "source")
            body[lookup + 2].operand = typeof(Preset).GetField(nameof(Preset.OtherEntries));
        else if (changedShape == "ambiguous")
            body.InsertRange(copy + 1, body.Skip(copy - 11).Take(12).Select(instruction => new CodeInstruction(instruction.opcode, instruction.operand)).ToArray());
        else if (changedShape == "bypass")
        {
            Label inner = generator.DefineLabel(), native = generator.DefineLabel();
            body[lookup].labels.Add(inner); body[0].labels.Add(native);
            body.InsertRange(0, new[] { new CodeInstruction(OpCodes.Ldc_I4_0), new(OpCodes.Brfalse, native),
                new(OpCodes.Ldnull), new(OpCodes.Ldnull), new(OpCodes.Br, inner) });
        }
        else if (changedShape == "creation")
        {
            int create = body.FindIndex(instruction => instruction.Calls(typeof(Database).GetMethod(nameof(Database.Create))!));
            Label afterCreation = generator.DefineLabel();
            body[create + 2].labels.Add(afterCreation);
            body.InsertRange(0, new[] { new CodeInstruction(OpCodes.Ldarg_1),
                new(OpCodes.Ldfld, typeof(Preset).GetField(nameof(Preset.AllowPleasure))!), new(OpCodes.Brtrue, afterCreation) });
        }
        suppliedShape = Describe(body);
        return body;
    }
    public static IEnumerable<CodeInstruction> ObserveUnchanged(IEnumerable<CodeInstruction> instructions)
    {
        var body = instructions.ToList();
        unchanged &= suppliedShape.SequenceEqual(Describe(body));
        return body;
    }
    private static string[] Describe(IEnumerable<CodeInstruction> body) => body.Select(instruction =>
        instruction.opcode.Name + ":" + instruction.operand + ":" + string.Join(",", instruction.labels) + ":" +
        string.Join(",", instruction.blocks.Select(block => block.blockType + "/" + block.catchType))).ToArray();

    [TestCase("lookup")]
    [TestCase("copy")]
    public void RelevantInnerSkipsDenyAdmissionWithoutRemovingTheForeignPatch(string selected)
    {
        var foreign = new HarmonyLib.Harmony("test.drug.preset.inner-native");
        var patch = Patch(() => null, _ => { });
        try
        {
            MethodInfo method = selected == "lookup" ? typeof(Policy).GetProperty("Item")!.GetMethod! : typeof(Entry).GetMethod(nameof(Entry.CopyFrom))!;
            foreign.CreateProcessor(Target).AddInnerPrefix(new HarmonyMethod(typeof(DrugPresetEntriesPatchTests), nameof(Skip))
                { innerMethod = new InnerMethod(method) }).Patch();
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(Group(patch), AdmissionPhase.DefsReady), Is.False);
            Assert.That(patch.Available, Is.False);
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(Target)!.Owners, Does.Contain(foreign.Id));
        }
        finally { patch.Candidate.Rollback(); foreign.UnpatchAll(foreign.Id); }
    }
    [Test]
    public void ReadonlyInnerObservationIsNotExecutedByInspectionAndRetainsValidCopies()
    {
        var present = new object();
        var foreign = new HarmonyLib.Harmony("test.drug.preset.observer");
        var patch = Patch(() => new PresetEntriesRead(new[] { present }, () => true), _ => { });
        try
        {
            foreign.CreateProcessor(Target).AddInnerPrefix(new HarmonyMethod(typeof(DrugPresetEntriesPatchTests), nameof(Observe))
                { innerMethod = new InnerMethod(typeof(Entry).GetMethod(nameof(Entry.CopyFrom))!) }).Patch();
            observed = 0;
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(Group(patch), AdmissionPhase.DefsReady), Is.True);
            patch.Enable();
            Assert.That(observed, Is.Zero);
            var result = new Database(present).Build(new Preset { Entries = new() { new() { Drug = new object() }, new() { Drug = present, Quantity = 11 } } });
            Assert.That(result[present].Quantity, Is.EqualTo(11));
            Assert.That(observed, Is.EqualTo(1));
        }
        finally { patch.Candidate.Rollback(); foreign.UnpatchAll(foreign.Id); }
    }
    public static void Observe() => observed++;

    [Test]
    public void EscapedPolicyLocalCannotBeClaimedAsTheCreatedPolicyReceiver()
    {
        var foreign = new HarmonyLib.Harmony("test.drug.preset.receiver");
        var warnings = new List<string>();
        var patch = Patch(() => null, warnings.Add);
        try
        {
            foreign.Patch(Target, transpiler: new HarmonyMethod(typeof(DrugPresetEntriesPatchTests), nameof(EscapePolicy)));
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(Group(patch), AdmissionPhase.DefsReady), Is.False);
            Assert.That(warnings.Single(), Does.Contain("loop changed"));
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(Target)!.Owners, Does.Contain(foreign.Id));
        }
        finally { patch.Candidate.Rollback(); foreign.UnpatchAll(foreign.Id); }
    }
    public static void RefPolicy(ref Policy policy) { }
    public static IEnumerable<CodeInstruction> EscapePolicy(IEnumerable<CodeInstruction> instructions)
    {
        bool afterCreate = false;
        foreach (var instruction in instructions)
        {
            yield return instruction;
            if (afterCreate)
            {
                // The native-shaped fixture stores its created policy in local 0.
                yield return new(OpCodes.Ldloca_S, (byte)0);
                yield return new(OpCodes.Call, typeof(DrugPresetEntriesPatchTests).GetMethod(nameof(RefPolicy))!);
                afterCreate = false;
            }
            if (instruction.Calls(typeof(Database).GetMethod(nameof(Database.Create))!)) afterCreate = true;
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CaptureOrValidityFailureKeepsNativeCallsAndBoundsFailedReporting(bool validity)
    {
        var present = new object();
        int captures = 0, reports = 0;
        var patch = Patch(() =>
        {
            captures++;
            if (!validity) throw new InvalidOperationException(new string('x', 500));
            return new PresetEntriesRead(new[] { present }, () => throw new InvalidOperationException("invalid receipt"));
        }, warning => { reports++; Assert.That(warning.Length, Is.LessThan(700)); throw new Exception("sink failed"); });
        try
        {
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(Group(patch), AdmissionPhase.DefsReady), Is.True);
            patch.Enable();
            var preset = new Preset { Entries = new() { new() { Drug = present, Quantity = 6 } } };
            Assert.That(new Database(present).Build(preset)[present].Quantity, Is.EqualTo(6));
            Assert.That(new Database(present).Build(preset)[present].Quantity, Is.EqualTo(6));
            Assert.That(captures, Is.EqualTo(1));
            Assert.That(reports, Is.EqualTo(1));
        }
        finally { patch.Candidate.Rollback(); }
    }

    private static Preset? replacementPreset;
    private static Policy? replacementPolicy;
    private static int observed;
    [TestCase(false)]
    [TestCase(true)]
    public void EffectivePrefixInputAndSkippedBodyKeepNativeOuterSemantics(bool skip)
    {
        var present = new object();
        replacementPreset = new Preset { Entries = new() { new() { Drug = new object() }, new() { Drug = present, Quantity = 8 } } };
        replacementPolicy = new Policy(new[] { present });
        var foreign = new HarmonyLib.Harmony("test.drug.preset.outer");
        int captures = 0;
        var patch = Patch(() => { captures++; return new PresetEntriesRead(new[] { present }, () => true); }, _ => { });
        try
        {
            foreign.Patch(Target, prefix: new HarmonyMethod(typeof(DrugPresetEntriesPatchTests), skip ? nameof(SkipFactory) : nameof(ChangeInput)),
                postfix: new HarmonyMethod(typeof(DrugPresetEntriesPatchTests), nameof(ObservePolicy)));
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(Group(patch), AdmissionPhase.DefsReady), Is.True);
            patch.Enable(); observed = 0;
            var result = new Database(present).Build(new Preset());
            Assert.That(captures, Is.EqualTo(skip ? 0 : 1));
            Assert.That(observed, Is.EqualTo(1));
            if (skip) Assert.That(result, Is.SameAs(replacementPolicy));
            else Assert.That(result[present].Quantity, Is.EqualTo(8));
        }
        finally { patch.Candidate.Rollback(); foreign.UnpatchAll(foreign.Id); replacementPreset = null; replacementPolicy = null; }
    }
    public static void ChangeInput(ref Preset preset) => preset = replacementPreset!;
    public static bool SkipFactory(ref Policy __result) { __result = replacementPolicy!; return false; }
    public static void ObservePolicy(Policy __result) => observed++;

    [Test]
    public void RepatchRequiresExplicitFreshPublicationAndRetainsForeignOwnership()
    {
        var present = new object();
        var preset = new Preset { Entries = new() { new() { Drug = new object() }, new() { Drug = present, Quantity = 3 } } };
        var foreign = new HarmonyLib.Harmony("test.drug.preset.repatch");
        var patch = Patch(() => new PresetEntriesRead(new[] { present }, () => true), _ => { });
        try
        {
            var coordinator = new AdmissionCoordinator(_ => { });
            Assert.That(coordinator.Apply(Group(patch), AdmissionPhase.DefsReady), Is.True);
            Assert.That(coordinator.Apply(Group(patch), AdmissionPhase.DefsReady), Is.True);
            patch.Enable();
            foreign.Patch(Target, transpiler: new HarmonyMethod(typeof(DrugPresetEntriesPatchTests), nameof(Identity)) { after = new[] { OwnedHarmonyPatch.OwnerId } });
            Assert.That(patch.Available, Is.False);
            Assert.That(patch.NeedsPublicationRefresh, Is.True);
            Assert.That(patch.RefreshPublication(), Is.False);
            Assert.That(patch.NeedsPublicationRefresh, Is.False);
            Assert.Throws<ArgumentException>(() => new Database(present).Build(preset));
            foreign.Unpatch(Target, HarmonyPatchType.Transpiler, foreign.Id);
            Assert.That(patch.NeedsPublicationRefresh, Is.True);
            Assert.That(patch.RefreshPublication(), Is.True);
            Assert.That(new Database(present).Build(preset)[present].Quantity, Is.EqualTo(3));
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(Target)!.Transpilers.Count(p => p.owner == OwnedHarmonyPatch.OwnerId), Is.EqualTo(1));
        }
        finally { patch.Candidate.Rollback(); foreign.UnpatchAll(foreign.Id); }
    }
    public static IEnumerable<CodeInstruction> Identity(IEnumerable<CodeInstruction> instructions) => instructions;

    [Test]
    public void ExistingOwnedSlotRejectsWithoutRemovingItsMember()
    {
        var existing = new OwnedHarmonyPatch(Target, HarmonyPatchType.Transpiler, typeof(DrugPresetEntriesPatchTests).GetMethod(nameof(Identity))!);
        var patch = Patch(() => null, _ => { });
        try
        {
            existing.Install();
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(Group(patch), AdmissionPhase.DefsReady), Is.False);
            patch.Candidate.Rollback();
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(Target)!.Transpilers.Single(p => p.owner == OwnedHarmonyPatch.OwnerId).PatchMethod,
                Is.EqualTo(typeof(DrugPresetEntriesPatchTests).GetMethod(nameof(Identity))));
        }
        finally { existing.Rollback(); }
    }

    [Test]
    public void ChangedLoopCountWithholdsTheEntryGuard()
    {
        var foreign = new HarmonyLib.Harmony("test.drug.preset.count");
        var warnings = new List<string>();
        var patch = Patch(() => new PresetEntriesRead(Array.Empty<object>(), () => true), warnings.Add);
        try
        {
            foreign.CreateProcessor(Target).AddInnerPrefix(new HarmonyMethod(typeof(DrugPresetEntriesPatchTests), nameof(Skip))
                { innerMethod = new InnerMethod(typeof(List<Entry>).GetProperty(nameof(List<Entry>.Count))!.GetMethod!) }).Patch();
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(Group(patch), AdmissionPhase.DefsReady), Is.False);
            Assert.That(patch.Available, Is.False);
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(Target)!.Owners, Does.Contain(foreign.Id));
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(Target)!.Transpilers.Any(p => p.owner == OwnedHarmonyPatch.OwnerId), Is.False);
        }
        finally { patch.Candidate.Rollback(); foreign.UnpatchAll(foreign.Id); }
    }
    public static bool Skip() => false;

    [Test]
    public void DeniedDecisionResultWriterCannotSkipValidCopiesWhileTheGuardIsInactive()
    {
        var present = new object();
        var preset = new Preset { Entries = new() { new() { Drug = present, Quantity = 12 } } };
        var foreign = new HarmonyLib.Harmony("test.drug.preset.decision");
        var patch = Patch(() => new PresetEntriesRead(new[] { present }, () => true), _ => { });
        try
        {
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(Group(patch), AdmissionPhase.DefsReady), Is.True);
            patch.Enable();
            foreign.CreateProcessor(Target).AddInnerPostfix(new HarmonyMethod(typeof(DrugPresetEntriesPatchTests), nameof(RejectDecision))
                { innerMethod = new InnerMethod(typeof(DrugPresetEntriesPatch).GetMethod("ShouldCopy", BindingFlags.NonPublic | BindingFlags.Static)!) }).Patch();
            Assert.That(patch.RefreshPublication(), Is.False);
            Assert.That(patch.Available, Is.False);
            Assert.That(new Database(present).Build(preset)[present].Quantity, Is.EqualTo(12));
        }
        finally
        {
            try { foreign.Unpatch(Target, HarmonyPatchType.InnerPostfix, foreign.Id); }
            finally { patch.Candidate.Rollback(); }
        }
    }
    public static void RejectDecision(ref bool __result) => __result = false;

    [Test]
    public void HarmlessArgumentAliasAndLabeledNopsRetainTheGuard()
    {
        var present = new object();
        var preset = new Preset { Entries = new() { new() { Drug = new object() }, new() { Drug = present, Quantity = 7 } } };
        var foreign = new HarmonyLib.Harmony("test.drug.preset.benign");
        var warnings = new List<string>();
        var patch = Patch(() => new PresetEntriesRead(new[] { present }, () => true), warnings.Add);
        try
        {
            foreign.Patch(Target, transpiler: new HarmonyMethod(typeof(DrugPresetEntriesPatchTests), nameof(AliasAndNops)));
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(Group(patch), AdmissionPhase.DefsReady), Is.True, string.Join("\n", warnings));
            patch.Enable();
            Assert.That(new Database(present).Build(preset)[present].Quantity, Is.EqualTo(7));
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(Target)!.Owners, Does.Contain(foreign.Id));
        }
        finally { patch.Candidate.Rollback(); foreign.UnpatchAll(foreign.Id); }
    }
    public static IEnumerable<CodeInstruction> AliasAndNops(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var alias = generator.DeclareLocal(typeof(Preset));
        yield return new(OpCodes.Ldarg_1);
        yield return new(OpCodes.Stloc, alias);
        foreach (var original in instructions)
        {
            var instruction = new CodeInstruction(original);
            var nop = new CodeInstruction(OpCodes.Nop);
            nop.labels.AddRange(instruction.labels); instruction.labels.Clear();
            yield return nop;
            if (instruction.opcode == OpCodes.Ldarg_1) { instruction.opcode = OpCodes.Ldloc; instruction.operand = alias; }
            yield return instruction;
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MissingOrExpiredBaselineKeepsNativeFailureAndReportsOnce(bool expired)
    {
        var missing = new object();
        var database = new Database();
        var preset = new Preset { Entries = new() { new() { Drug = missing } } };
        var warnings = new List<string>();
        var patch = Patch(() => expired ? new PresetEntriesRead(Array.Empty<object>(), () => false) : null, warnings.Add);
        try
        {
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(Group(patch), AdmissionPhase.DefsReady), Is.True);
            patch.Enable();
            Assert.Throws<ArgumentException>(() => database.Build(preset));
            Assert.Throws<ArgumentException>(() => database.Build(preset));
            Assert.That(warnings, Has.Count.EqualTo(1));
            Assert.That(warnings[0], Does.Contain("baseline"));
            Assert.That(database.Created!.CopyOrder, Is.Empty);
        }
        finally { patch.Candidate.Rollback(); }
    }

    [Test]
    public void UnavailableEntryBeforeTwoValidEntriesPreservesNativePolicyAndExplicitChoices()
    {
        var removed = new object(); var first = new object(); var second = new object();
        var database = new Database(first, second);
        var preset = new Preset
        {
            Name = "custom policy", Entries = new List<Entry>
            {
                new() { Drug = removed, Quantity = 99 },
                new() { Drug = first, Quantity = 4 },
                new() { Drug = second, Quantity = 9 }
            }
        };
        var baseline = new PresetEntriesRead(new[] { first, second }, () => true);
        var warnings = new List<string>();
        var patch = Patch(() => baseline, warnings.Add);
        try
        {
            Assert.That(new AdmissionCoordinator(warnings.Add).Apply(Group(patch), AdmissionPhase.DefsReady), Is.True);
            patch.Enable();
            Policy? result = null;
            Assert.DoesNotThrow(() => result = database.Build(preset));
            Assert.That(result, Is.SameAs(database.Created));
            Assert.That(result!.Name, Is.EqualTo(preset.Name));
            Assert.That(result.Source, Is.SameAs(preset));
            Assert.That(result.CopyOrder, Is.EqualTo(new[] { first, second }));
            Assert.That(result[first].Quantity, Is.EqualTo(4));
            Assert.That(result[second].Quantity, Is.EqualTo(9));
            Assert.That(preset.Entries.Count, Is.EqualTo(3));
            Assert.That(warnings, Is.Empty);
        }
        finally { patch.Candidate.Rollback(); }
    }

    private static PatchGroup Group(DrugPresetEntriesPatch patch) => new("MM.DRUG.Preset", AdmissionPhase.DefsReady, new[] { patch.Candidate });
    private static MethodInfo Target => typeof(Database).GetMethod(nameof(Database.Build))!;
    private static DrugPresetEntriesPatch Patch(Func<PresetEntriesRead?> capture, Action<string> warning) => new(
        typeof(Database).GetMethod(nameof(Database.Build))!, typeof(Database).GetMethod(nameof(Database.Create))!,
        typeof(Preset).GetField(nameof(Preset.Entries))!, typeof(Entry).GetField(nameof(Entry.Drug))!,
        typeof(Policy).GetProperty("Item")!.GetMethod!, typeof(Entry).GetMethod(nameof(Entry.CopyFrom))!, capture, warning);

    public sealed class Preset
    {
        public string Name = "";
        public bool AllowPleasure;
        public List<Entry>? Entries;
        public List<Entry>? OtherEntries;
    }
    public sealed class Entry
    {
        public object? Drug;
        public int Quantity;
        public bool Addiction, Recreation, Scheduled;
        public float Days, Mood, Joy;
        public List<object?>? Trace;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void CopyFrom(Entry source)
        {
            Drug = source.Drug;
            Quantity = source.Quantity;
            Addiction = source.Addiction; Recreation = source.Recreation; Scheduled = source.Scheduled;
            Days = source.Days; Mood = source.Mood; Joy = source.Joy;
            Trace?.Add(Drug);
        }
    }
    public sealed class Policy
    {
        public string Name = "";
        public Preset? Source;
        public readonly List<object?> CopyOrder = new();
        public readonly List<Entry> rows;
        public Policy(IEnumerable<object> identities) => rows = identities.Select(identity => new Entry { Drug = identity, Trace = CopyOrder }).ToList();
        public Entry this[object drug]
        {
            [MethodImpl(MethodImplOptions.NoInlining)]
            get => rows.FirstOrDefault(row => ReferenceEquals(row.Drug, drug)) ?? throw new ArgumentException("Unavailable drug");
        }
    }
    public sealed class Database
    {
        private readonly object[] identities;
        public Policy? Created;
        public Database(params object[] identities) => this.identities = identities;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Policy Create() => Created = new Policy(identities);
        [MethodImpl(MethodImplOptions.NoInlining)]
        public Policy Build(Preset preset)
        {
            var policy = Create();
            policy.Name = preset.Name;
            policy.Source = preset;
            if (preset.AllowPleasure)
                for (int rowIndex = 0; rowIndex < policy.rows.Count; rowIndex++) policy.rows[rowIndex].Recreation = true;
            if (preset.Entries != null)
                for (int index = 0; index < preset.Entries.Count; index++)
                    policy[preset.Entries[index].Drug!].CopyFrom(preset.Entries[index]);
            return policy;
        }
    }
}
