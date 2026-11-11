using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.DrugTolerance;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.DrugTolerance;

[TestFixture, NonParallelizable]
public sealed class DrugToleranceSocialPatchTests
{
    public sealed class Drug { public bool Allowed = true; public bool Below; }
    public sealed class Pawn { public List<Drug> Drugs = new(); }
    private static readonly List<Drug> candidates = new();
    private static readonly MethodInfo Target = typeof(DrugToleranceSocialPatchTests).GetMethod(nameof(Select), BindingFlags.Static | BindingFlags.Public)!;
    private static readonly FieldInfo Candidates = typeof(DrugToleranceSocialPatchTests).GetField(nameof(candidates), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static Pawn? replacement;

    [MethodImpl(MethodImplOptions.NoInlining)]
    public static bool Select(int center, Pawn pawn, out Drug? drink)
    {
        candidates.Clear();
        foreach (var drug in pawn.Drugs)
            if (drug.Allowed) candidates.Add(drug);
        if (candidates.Count == 0) { drink = null; return false; }
        drink = candidates[0];
        return true;
    }

    [Test]
    public void FilteringCandidatesKeepsAnAlternativeAndThenAllowsNativeDrinklessFallback()
    {
        var blocked = new Drug(); var accepted = new Drug { Below = true };
        var pawn = new Pawn { Drugs = new() { blocked, accepted } };
        var reports = new List<string>();
        DrugToleranceSocialPatch? patch = null;
        patch = new(Target, Candidates, null, (_, drug) => ((Drug)drug).Below, () => patch!.Available, reports.Add);
        try
        {
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(new("social-test", AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early), Is.True);
            Assert.That(Select(0, pawn, out var drink), Is.True);
            Assert.That(drink, Is.SameAs(accepted));
            accepted.Below = false;
            Assert.That(Select(0, pawn, out drink), Is.False);
            Assert.That(drink, Is.Null);
            Assert.That(reports, Is.Empty);
        }
        finally { patch.Candidate.Rollback(); candidates.Clear(); }
    }

    [TestCase(nameof(BypassClear), false)]
    [TestCase(nameof(EnterAppendAfterReceiver), false)]
    [TestCase(nameof(EscapeList), false)]
    [TestCase(nameof(EscapePawnAddress), false)]
    [TestCase(nameof(ReadPawnAddress), true)]
    [TestCase(nameof(BranchToReceiver), true)]
    public void IncomingRelationshipChangesAreAdmittedOnlyWhenTheCandidateAndRecipientRemainProved(string rewrite, bool supported)
    {
        var foreign = new HarmonyLib.Harmony("tests.tolerance.social-stream");
        var reports = new List<string>();
        var patch = new DrugToleranceSocialPatch(Target, Candidates, null, (_, _) => false, () => true, reports.Add);
        var native = new Drug { Below = true };
        try
        {
            foreign.Patch(Target, transpiler: new HarmonyMethod(typeof(DrugToleranceSocialPatchTests), rewrite));
            bool admitted = new AdmissionCoordinator(reports.Add).Apply(new("social-test", AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early);
            Assert.That(admitted, Is.EqualTo(supported), string.Join("; ", reports));
            candidates.Add(native); // Proves native behavior even for the executable clear bypass.
            Assert.That(Select(1, new Pawn { Drugs = new() { native } }, out var drink), Is.EqualTo(!supported));
            Assert.That(drink, supported ? Is.Null : Is.SameAs(native));
        }
        finally
        {
            try { patch.Candidate.Rollback(); }
            finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); candidates.Clear(); }
        }
    }

    [Test]
    public void ActualPrefixRecipientAndNativeCandidateRejectionRemainAuthoritative()
    {
        var foreign = new HarmonyLib.Harmony("tests.tolerance.social-actor");
        var reports = new List<string>();
        var rejected = new Drug { Allowed = false, Below = true };
        var allowed = new Drug { Below = true };
        replacement = new Pawn { Drugs = new() { rejected, allowed } };
        var observed = new List<(object Pawn, object Drug)>();
        var patch = new DrugToleranceSocialPatch(Target, Candidates, null,
            (pawn, drug) => { observed.Add((pawn, drug)); return true; }, () => true, reports.Add);
        try
        {
            foreign.Patch(Target, prefix: new HarmonyMethod(typeof(DrugToleranceSocialPatchTests), nameof(ReplacePawn)));
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(new("social-test", AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early), Is.True);
            Assert.That(Select(0, new Pawn(), out var drink), Is.True);
            Assert.That(drink, Is.SameAs(allowed));
            Assert.That(observed, Is.EqualTo(new[] { ((object)replacement, (object)allowed) }));
        }
        finally { replacement = null; try { patch.Candidate.Rollback(); } finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); candidates.Clear(); } }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void UnavailableOrFailedOwnedDecisionRetainsTheNativeCandidate(bool throws)
    {
        var reports = new List<string>();
        var patch = new DrugToleranceSocialPatch(Target, Candidates, null, (_, _) => throw new InvalidOperationException("owned decision failed"), () => throws, reports.Add);
        var native = new Drug();
        try
        {
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(new("social-test", AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early), Is.True);
            Assert.That(Select(0, new Pawn { Drugs = new() { native } }, out var drink), Is.True);
            Assert.That(drink, Is.SameAs(native));
            Assert.That(reports, Has.Count.EqualTo(throws ? 1 : 0));
        }
        finally { patch.Candidate.Rollback(); candidates.Clear(); }
    }

    [TestCase(nameof(ChangeResult))]
    [TestCase(nameof(ChangeDrink))]
    public void OutputChangingCompletionCannotClaimFilteredSelection(string callback)
    {
        var reports = new List<string>();
        var foreign = new HarmonyLib.Harmony("tests.tolerance.social-output");
        var patch = new DrugToleranceSocialPatch(Target, Candidates, null, (_, _) => false, () => true, reports.Add);
        try
        {
            foreign.Patch(Target, postfix: new HarmonyMethod(typeof(DrugToleranceSocialPatchTests), callback));
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(new("social-test", AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early), Is.False);
            Assert.That(patch.Available, Is.False);
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(Target)!.Owners, Is.EqualTo(new[] { foreign.Id }));
        }
        finally { try { patch.Candidate.Rollback(); } finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); candidates.Clear(); } }
    }

    [TestCase(nameof(AliasResult), false)]
    [TestCase(nameof(AliasDrink), false)]
    [TestCase(nameof(IndexDrink), false)]
    [TestCase(nameof(MethodAliasResult), false)]
    [TestCase(nameof(MethodAliasDrink), false)]
    [TestCase(nameof(AliasArguments), false)]
    [TestCase(nameof(ObserveAliasedResult), true)]
    public void EffectiveCompletionBindingsDistinguishMutationFromObservation(string callback, bool supported)
    {
        var foreign = new HarmonyLib.Harmony("tests.tolerance.social-alias");
        var reports = new List<string>();
        var patch = new DrugToleranceSocialPatch(Target, Candidates, null, (_, _) => false, () => true, reports.Add);
        try
        {
            foreign.Patch(Target, postfix: new HarmonyMethod(typeof(DrugToleranceSocialPatchTests), callback));
            bool result = Select(0, new Pawn(), out var drink);
            if (callback.Contains("Result", StringComparison.Ordinal) && !supported) Assert.That(result, Is.True);
            else if (!supported) Assert.That(drink, Is.Not.Null);
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(new("social-alias", AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early), Is.EqualTo(supported), string.Join("; ", reports));
            Assert.That(Select(0, new Pawn { Drugs = new() { new Drug() } }, out drink), Is.EqualTo(!supported));
            Assert.That(drink, supported ? Is.Null : Is.Not.Null);
        }
        finally { try { patch.Candidate.Rollback(); } finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); candidates.Clear(); } }
    }

    [TestCase(nameof(ZeroPaddedDrink))]
    [TestCase(nameof(AliasZeroPaddedDrink))]
    [TestCase(nameof(MethodAliasZeroPaddedDrink))]
    public void NumericArgumentSpellingsCannotReplaceTheFilteredDrink(string callback)
    {
        var foreign = new HarmonyLib.Harmony("tests.tolerance.social-numeric-alias");
        var reports = new List<string>();
        var patch = new DrugToleranceSocialPatch(Target, Candidates, null, (_, _) => false, () => true, reports.Add);
        try
        {
            foreign.Patch(Target, postfix: new HarmonyMethod(typeof(DrugToleranceSocialPatchTests), callback));
            Assert.That(Select(0, new Pawn(), out var drink), Is.False);
            Assert.That(drink, Is.Not.Null, "The actual provider binds this numeric spelling to the native out-drink argument.");
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(new("social-numeric-alias", AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early), Is.False);
            var native = new Drug();
            Assert.That(Select(0, new Pawn { Drugs = new() { native } }, out drink), Is.True);
            Assert.That(drink, Is.Not.Null.And.Not.SameAs(native), "Unavailable owned admission leaves the foreign native completion intact.");
        }
        finally { try { patch.Candidate.Rollback(); } finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); candidates.Clear(); } }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NativeSkipAndThrowDoNotExecuteAnOwnedCandidateDecision(bool throws)
    {
        var foreign = new HarmonyLib.Harmony("tests.tolerance.social-skip");
        var reports = new List<string>();
        int calls = 0;
        var patch = new DrugToleranceSocialPatch(Target, Candidates, null, (_, _) => { calls++; return false; }, () => true, reports.Add);
        try
        {
            if (!throws) foreign.Patch(Target, prefix: new HarmonyMethod(typeof(DrugToleranceSocialPatchTests), nameof(SkipSelection)));
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(new("social-test", AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early), Is.True);
            if (throws) Assert.Throws<NullReferenceException>(() => Select(0, null!, out _));
            else Assert.That(Select(0, new Pawn { Drugs = new() { new Drug() } }, out _), Is.False);
            Assert.That(calls, Is.Zero);
            Assert.That(reports, Has.Count.EqualTo(throws ? 0 : 1));
        }
        finally { try { patch.Candidate.Rollback(); } finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); candidates.Clear(); } }
    }

    [Test]
    public void CompatibleRepatchReadmitsAndRemovalCannotLeaveAnEmittedVetoActive()
    {
        var foreign = new HarmonyLib.Harmony("tests.tolerance.social-generation");
        var reports = new List<string>();
        var patch = new DrugToleranceSocialPatch(Target, Candidates, null, (_, _) => false, () => true, reports.Add);
        var native = new Drug();
        var pawn = new Pawn { Drugs = new() { native } };
        try
        {
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(new("social-test", AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early), Is.True);
            foreign.Patch(Target, prefix: new HarmonyMethod(typeof(DrugToleranceSocialPatchTests), nameof(Observe)));
            Assert.That(Select(0, pawn, out _), Is.False);
            new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId).Unpatch(Target, HarmonyPatchType.Finalizer, OwnedHarmonyPatch.OwnerId);
            Assert.That(patch.Available, Is.False);
            Assert.That(Select(0, pawn, out var drink), Is.True, "The previously emitted forwarding append must fall back to native behavior.");
            Assert.That(drink, Is.SameAs(native));
        }
        finally { try { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); } finally { patch.Candidate.Rollback(); candidates.Clear(); } }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void FinalInnerEffectsDistinguishAnObserverFromASkippedNativeClear(bool observation)
    {
        var foreign = new HarmonyLib.Harmony("tests.tolerance.social-inner");
        var reports = new List<string>();
        var patch = new DrugToleranceSocialPatch(Target, Candidates, null, (_, _) => false, () => true, reports.Add);
        var stale = new Drug();
        try
        {
            foreign.CreateProcessor(Target).AddInnerPrefix(new HarmonyMethod(typeof(DrugToleranceSocialPatchTests), observation ? nameof(Observe) : nameof(SkipClear))
                { innerMethod = new InnerMethod(typeof(List<Drug>).GetMethod(nameof(List<Drug>.Clear))!) }).Patch();
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(new("social-test", AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early), Is.EqualTo(observation));
            candidates.Add(stale);
            Assert.That(Select(0, new Pawn { Drugs = new() { new Drug() } }, out var drink), Is.EqualTo(!observation));
            Assert.That(drink, observation ? Is.Null : Is.SameAs(stale));
            Assert.That(HarmonyLib.Harmony.GetPatchInfo(Target)!.Owners, Does.Contain(foreign.Id));
        }
        finally { try { patch.Candidate.Rollback(); } finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); candidates.Clear(); } }
    }

    [Test]
    public void DiagnosticStreamWithholdsTheAlreadyEmittedFilterUntilARealRepatch()
    {
        var foreign = new HarmonyLib.Harmony("tests.tolerance.social-diagnostic");
        var reports = new List<string>();
        var patch = new DrugToleranceSocialPatch(Target, Candidates, null, (_, _) => false, () => true, reports.Add);
        var pawn = new Pawn { Drugs = new() { new Drug() } };
        try
        {
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(new("social-test", AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early), Is.True);
            Assert.That(PatchProcessor.GetCurrentInstructions(Target), Is.Not.Empty);
            Assert.That(patch.Available, Is.False);
            Assert.That(Select(0, pawn, out _), Is.True);
            foreign.Patch(Target, prefix: new HarmonyMethod(typeof(DrugToleranceSocialPatchTests), nameof(Observe)));
            Assert.That(Select(0, pawn, out _), Is.False);
        }
        finally { try { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); } finally { patch.Candidate.Rollback(); candidates.Clear(); } }
    }

    public static void ReplacePawn(ref Pawn __1) => __1 = replacement!;
    public static void ChangeResult(ref bool __result) => __result = true;
    public static void ChangeDrink(ref Drug? __2) => __2 = new Drug();
    public static void AliasResult([HarmonyArgument("__result")] ref bool renamed) => renamed = true;
    public static void AliasDrink([HarmonyArgument("drink")] ref Drug? renamed) => renamed = new Drug();
    public static void IndexDrink([HarmonyArgument(2)] ref Drug? renamed) => renamed = new Drug();
    [HarmonyArgument("renamed", "__result")]
    public static void MethodAliasResult(ref bool renamed) => renamed = true;
    [HarmonyArgument("renamed", "drink")]
    public static void MethodAliasDrink(ref Drug? renamed) => renamed = new Drug();
    public static void AliasArguments([HarmonyArgument("__args")] object[] renamed) => renamed[2] = new Drug();
    public static void ObserveAliasedResult([HarmonyArgument("__result")] bool renamed) { }
    public static void ZeroPaddedDrink(ref Drug? __02) => __02 = new Drug();
    public static void AliasZeroPaddedDrink([HarmonyArgument("__02")] ref Drug? renamed) => renamed = new Drug();
    [HarmonyArgument("renamed", "__02")]
    public static void MethodAliasZeroPaddedDrink(ref Drug? renamed) => renamed = new Drug();
    public static void Observe() { }
    public static bool SkipClear() => false;
    public static bool SkipSelection(ref bool __result, ref Drug? __2) { __result = false; __2 = null; return false; }
    public static IEnumerable<CodeInstruction> ReadPawnAddress(IEnumerable<CodeInstruction> instructions) =>
        new[] { new CodeInstruction(OpCodes.Ldarga_S, (byte)1), new CodeInstruction(OpCodes.Ldind_Ref), new CodeInstruction(OpCodes.Pop) }.Concat(instructions);
    public static IEnumerable<CodeInstruction> EscapePawnAddress(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var alias = generator.DeclareLocal(typeof(Pawn).MakeByRefType());
        return new[] { new CodeInstruction(OpCodes.Ldarga_S, (byte)1), new CodeInstruction(OpCodes.Stloc, alias) }.Concat(instructions);
    }
    public static IEnumerable<CodeInstruction> EscapeList(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var alias = generator.DeclareLocal(typeof(List<Drug>));
        return new[] { new CodeInstruction(OpCodes.Ldsfld, Candidates), new CodeInstruction(OpCodes.Stloc, alias) }.Concat(instructions);
    }
    public static IEnumerable<CodeInstruction> BypassClear(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var body = instructions.ToList();
        int clear = body.FindIndex(i => i.Calls(typeof(List<Drug>).GetMethod(nameof(List<Drug>.Clear))!));
        var after = generator.DefineLabel(); body[clear + 1].labels.Add(after);
        body.InsertRange(0, new[] { new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Brtrue, after) });
        return body;
    }
    public static IEnumerable<CodeInstruction> EnterAppendAfterReceiver(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var body = instructions.ToList();
        int append = body.FindIndex(i => i.Calls(typeof(List<Drug>).GetMethod(nameof(List<Drug>.Add))!));
        int start = append - 2;
        var consume = generator.DefineLabel(); body[append].labels.Add(consume);
        var alternative = body.Skip(start).Take(2).Select(i => new CodeInstruction(i)).ToList();
        alternative.Add(new(OpCodes.Br, consume)); body.InsertRange(start, alternative);
        return body;
    }
    public static IEnumerable<CodeInstruction> BranchToReceiver(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
    {
        var body = instructions.ToList();
        int append = body.FindIndex(i => i.Calls(typeof(List<Drug>).GetMethod(nameof(List<Drug>.Add))!));
        var receiver = generator.DefineLabel(); body[append - 2].labels.Add(receiver);
        body.Insert(append - 2, new(OpCodes.Br, receiver));
        return body;
    }
}
