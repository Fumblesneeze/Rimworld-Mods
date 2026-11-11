using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.DrugTolerance;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.DrugTolerance;

[TestFixture, NonParallelizable]
public sealed class DrugToleranceJoyExceptionGuardTests
{
    public sealed class Trait { }
    public static class Traits { public static Trait DrugDesire = new(); }
    public sealed class TraitSet { public int Degree; public int DegreeOfTrait(Trait trait) => Degree; }
    public sealed class Story { public TraitSet traits = new(); }
    public sealed class Pawn { public Story? story = new(); public bool InMentalState { get; set; } }
    public sealed class Drug { public bool NativeAllowed = true; public bool AllowedForJoy; }
    public sealed class NativeJoy
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Eligible(Pawn pawn, Drug drug)
        {
            if (!drug.NativeAllowed) return false;
            if (!drug.AllowedForJoy && pawn.story != null && pawn.story.traits.DegreeOfTrait(Traits.DrugDesire) <= 0 && !pawn.InMentalState) return false;
            return true;
        }
    }
    private static string? failure;
    private static readonly MethodInfo Target = typeof(NativeJoy).GetMethod(nameof(NativeJoy.Eligible))!;
    internal static DrugToleranceJoyExceptionGuard Guard() => new(new(typeof(Pawn).GetField(nameof(Pawn.story))!,
        typeof(Story).GetField(nameof(Story.traits))!, typeof(Traits).GetField(nameof(Traits.DrugDesire))!,
        typeof(TraitSet).GetMethod(nameof(TraitSet.DegreeOfTrait))!, typeof(Pawn).GetProperty(nameof(Pawn.InMentalState))!.GetMethod!));

    [TestCase(1, false, false, true)]
    [TestCase(0, true, false, true)]
    [TestCase(0, false, true, true)]
    [TestCase(0, false, false, false)]
    public void NativeExceptionsAndPolicyPermissionRemainAdmitted(int degree, bool mental, bool joy, bool expected)
    {
        var owner = new HarmonyLib.Harmony("tests.tolerance.joy-exception.native");
        failure = "Observer missing.";
        try
        {
            owner.Patch(Target, transpiler: new HarmonyMethod(typeof(DrugToleranceJoyExceptionGuardTests), nameof(Inspect)));
            var pawn = new Pawn { InMentalState = mental }; pawn.story!.traits.Degree = degree;
            Assert.That(new NativeJoy().Eligible(pawn, new Drug { AllowedForJoy = joy }), Is.EqualTo(expected));
            Assert.That(failure, Is.Null);
        }
        finally { owner.Unpatch(Target, HarmonyPatchType.All, owner.Id); }
    }

    [Test]
    public void ChangedEffectiveExceptionRevokesTheDecisionAndKeepsNativeResult()
    {
        var reports = new List<string>();
        var foreign = new HarmonyLib.Harmony("tests.tolerance.joy-exception.repatch");
        DrugToleranceDecisionPatch? patch = null;
        patch = new("DT-test-joy", Target, false, (_, _) => false, () => patch!.Available, reports.Add, Guard().Validate);
        try
        {
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(new("joy", AdmissionPhase.Early, new[] { patch.Candidate }), AdmissionPhase.Early), Is.True);
            var pawn = new Pawn(); var drug = new Drug { AllowedForJoy = true }; var joy = new NativeJoy();
            Assert.That(joy.Eligible(pawn, drug), Is.False);
            foreign.Patch(Target, transpiler: new HarmonyMethod(typeof(DrugToleranceJoyExceptionGuardTests), nameof(ChangeThreshold)) { priority = Priority.First });
            Assert.That(joy.Eligible(pawn, drug), Is.True, "Invalid exception admission must stop the owned veto on the real native call.");
            Assert.That(patch.Available, Is.False);
        }
        finally { try { patch.Candidate.Rollback(); } finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); } }
    }

    [Test]
    public void ChangedPositiveTraitBoundaryCannotAuthorizeTheNativeException()
    {
        var owner = new HarmonyLib.Harmony("tests.tolerance.joy-exception");
        failure = "Observer missing.";
        try
        {
            owner.Patch(Target, transpiler: new HarmonyMethod(typeof(DrugToleranceJoyExceptionGuardTests), nameof(ChangeThreshold)) { priority = Priority.First });
            owner.Patch(Target, transpiler: new HarmonyMethod(typeof(DrugToleranceJoyExceptionGuardTests), nameof(Inspect)) { priority = Priority.Last });
            var pawn = new Pawn(); pawn.story!.traits.Degree = 1;
            Assert.That(new NativeJoy().Eligible(pawn, new Drug()), Is.False, "The changed real native predicate must reject degree1.");
            Assert.That(failure, Is.Not.Null, "A changed exception must make the atomic feature unavailable.");
        }
        finally { owner.Unpatch(Target, HarmonyPatchType.All, owner.Id); }
    }
    public static IEnumerable<CodeInstruction> ChangeThreshold(IEnumerable<CodeInstruction> instructions)
    {
        var body = instructions.ToList();
        int degree = body.FindIndex(i => i.Calls(typeof(TraitSet).GetMethod(nameof(TraitSet.DegreeOfTrait))!));
        body[degree + 1].opcode = OpCodes.Ldc_I4_1;
        return body;
    }
    public static IEnumerable<CodeInstruction> Inspect(IEnumerable<CodeInstruction> instructions)
    { var body = instructions.ToList(); failure = Guard().Validate(body); return body; }
}
