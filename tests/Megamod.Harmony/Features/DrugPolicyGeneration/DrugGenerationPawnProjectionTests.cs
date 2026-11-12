using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.DrugPolicyGeneration;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.DrugPolicyGeneration;

[TestFixture, NonParallelizable]
public sealed class DrugGenerationPawnProjectionTests
{
    private static MethodInfo Method(Type type, string name) => type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)!;
    private static FieldInfo Field(Type type, string name) => type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance)!;
    private static MethodInfo PawnGetter => typeof(Comp).GetProperty(nameof(Comp.Pawn))!.GetMethod!;
    private static DrugGenerationPawnBodyGuard PawnGuard() => new(PawnGetter, Field(typeof(Comp), nameof(Comp.Parent)), Field(typeof(Condition), nameof(Condition.Pawn)));

    [Test]
    public void ObservedPawnProjectionReturnsTheActualCurrentParentPawn()
    {
        var guard = PawnGuard(); var observation = new DrugGenerationObservedBodies(new[] { new DrugGenerationObservedBodies.Root(PawnGetter, guard.Validate) });
        var pawn = new Actor(); var parent = new Condition { Pawn = pawn }; var comp = new Comp { Parent = parent };
        try
        {
            observation.Install(); Assert.That(observation.Available, Is.True, observation.Failure);
            Assert.That(comp.Pawn, Is.SameAs(pawn));
            parent.Pawn = new Actor(); Assert.That(comp.Pawn, Is.SameAs(parent.Pawn));
            comp.Parent = new Condition { Pawn = new Actor() }; Assert.That(comp.Pawn, Is.SameAs(comp.Parent.Pawn));
        }
        finally { observation.Rollback(); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ActualPawnReplacementOrReceiverEscapeRevokesProjection(bool escape)
    {
        var guard = PawnGuard(); var observation = new DrugGenerationObservedBodies(new[] { new DrugGenerationObservedBodies.Root(PawnGetter, guard.Validate) });
        var foreign = new HarmonyLib.Harmony("tests.generation.pawn-effects");
        var comp = new Comp { Parent = new Condition { Pawn = new Actor() } }; Effects.Other = new Actor();
        try
        {
            observation.Install(); foreign.Patch(PawnGetter, transpiler: new HarmonyMethod(Method(typeof(Effects), escape ? nameof(Effects.Escape) : nameof(Effects.Replace))));
            var result = comp.Pawn;
            if (escape) Assert.That(Effects.Escaped, Is.SameAs(comp)); else Assert.That(result, Is.SameAs(Effects.Other));
            Assert.That(observation.Available, Is.False); Assert.That(observation.Failure, Does.Contain(nameof(Comp)));
            foreign.Unpatch(PawnGetter, HarmonyPatchType.Transpiler, foreign.Id);
            Assert.That(observation.Available, Is.True, observation.Failure);
        }
        finally
        {
            Effects.Other = null; Effects.Escaped = null;
            try { observation.Rollback(); } finally { foreign.Unpatch(PawnGetter, HarmonyPatchType.All, foreign.Id); }
        }
    }

    [Test]
    public void InheritedLookupAndHarmlessReferenceLocalShareOneDeclaration()
    {
        var inherited = typeof(DerivedComp).GetProperty(nameof(Comp.Pawn))!.GetMethod!;
        var guard = PawnGuard(); var root = new DrugGenerationObservedBodies.Root(inherited, guard.Validate);
        Assert.Throws<ArgumentException>(() => new DrugGenerationObservedBodies(new[] { root, new(PawnGetter, guard.Validate) }));
        var observation = new DrugGenerationObservedBodies(new[] { root }); var foreign = new HarmonyLib.Harmony("tests.generation.pawn-alias");
        try
        {
            foreign.Patch(PawnGetter, transpiler: new HarmonyMethod(Method(typeof(Effects), nameof(Effects.Alias))));
            observation.Install(); Assert.That(observation.Available, Is.True, observation.Failure);
            var comp = new DerivedComp { Parent = new Condition { Pawn = new Actor() } };
            Assert.That(comp.Pawn, Is.SameAs(comp.Parent.Pawn));
        }
        finally { try { observation.Rollback(); } finally { foreign.Unpatch(PawnGetter, HarmonyPatchType.All, foreign.Id); } }
    }

    [Test]
    public void LaterRootAcquisitionFailureRollsBackOnlyTheAttemptedBundle()
    {
        var other = typeof(OtherComp).GetProperty(nameof(OtherComp.Pawn))!.GetMethod!;
        var otherGuard = new DrugGenerationPawnBodyGuard(other, Field(typeof(Comp), nameof(Comp.Parent)), Field(typeof(Condition), nameof(Condition.Pawn)));
        var existing = new DrugGenerationObservedBodies(new[] { new DrugGenerationObservedBodies.Root(other, otherGuard.Validate) });
        var attempted = new DrugGenerationObservedBodies(new[] { new DrugGenerationObservedBodies.Root(PawnGetter, PawnGuard().Validate), new(other, otherGuard.Validate) });
        try
        {
            existing.Install(); var reports = new List<string>();
            var candidate = new PatchCandidate("pawn-bundle", "DG-NATIVE-FACTS", "pawn bundle", "pawn projection", "native controls", () => null, attempted.Install, attempted.Rollback);
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(new("pawn-bundle", AdmissionPhase.Early, new[] { candidate }), AdmissionPhase.Early), Is.False);
            Assert.That(reports, Has.Count.EqualTo(1)); Assert.That(existing.Available, Is.True, existing.Failure);
            Assert.That(HarmonyPublication.Inspect(PawnGetter, snapshot => snapshot.Transpilers.All(p => p.owner != OwnedHarmonyPatch.OwnerId)), Is.True);
            Assert.That(HarmonyPublication.Inspect(other, snapshot => snapshot.Transpilers.Count(p => p.owner == OwnedHarmonyPatch.OwnerId) == 1), Is.True);
        }
        finally { try { attempted.Rollback(); } finally { existing.Rollback(); } }
    }

    [Test]
    public void TwoRootsShareOneScalarCallbackObservationUntilItsLastUseIsRemoved()
    {
        var other = typeof(OtherComp).GetProperty(nameof(OtherComp.Pawn))!.GetMethod!;
        var otherGuard = new DrugGenerationPawnBodyGuard(other, Field(typeof(Comp), nameof(Comp.Parent)), Field(typeof(Condition), nameof(Condition.Pawn)));
        var observation = new DrugGenerationObservedBodies(new[] { new DrugGenerationObservedBodies.Root(PawnGetter, PawnGuard().Validate), new(other, otherGuard.Validate) });
        var foreign = new HarmonyLib.Harmony("tests.generation.pawn-observer"); var callback = Method(typeof(Effects), nameof(Effects.Observe)); Effects.Observed = 0;
        try
        {
            observation.Install(); foreign.Patch(PawnGetter, postfix: new HarmonyMethod(callback)); foreign.Patch(other, postfix: new HarmonyMethod(callback));
            Assert.That(observation.Available, Is.False); Assert.That(Effects.Observed, Is.Zero);
            Assert.That(HarmonyPublication.Inspect(callback, snapshot => snapshot.Transpilers.Length == 0), Is.True);
            Assert.That(observation.Revalidate(), Is.True, observation.Failure);
            Assert.That(HarmonyPublication.Inspect(callback, snapshot => snapshot.Transpilers.Count(p => p.owner == OwnedHarmonyPatch.OwnerId) == 1), Is.True);
            var parent = new Condition { Pawn = new Actor() };
            _ = new Comp { Parent = parent }.Pawn; _ = new OtherComp { Parent = parent }.Pawn; Assert.That(Effects.Observed, Is.EqualTo(2));
            foreign.Unpatch(PawnGetter, HarmonyPatchType.Postfix, foreign.Id); Assert.That(observation.Revalidate(), Is.True, observation.Failure);
            Assert.That(HarmonyPublication.Inspect(callback, snapshot => snapshot.Transpilers.Length == 1), Is.True);
            foreign.Unpatch(other, HarmonyPatchType.Postfix, foreign.Id); Assert.That(observation.Revalidate(), Is.True, observation.Failure);
            Assert.That(HarmonyPublication.Inspect(callback, snapshot => snapshot.Transpilers.Length == 0), Is.True);
        }
        finally { try { observation.Rollback(); } finally { foreign.Unpatch(PawnGetter, HarmonyPatchType.All, foreign.Id); foreign.Unpatch(other, HarmonyPatchType.All, foreign.Id); } }
    }

    public static class Effects
    {
        public static Actor? Other;
        public static Comp? Escaped;
        public static int Observed;
        [MethodImpl(MethodImplOptions.NoInlining)] public static void Observe() => Observed++;
        public static IEnumerable<CodeInstruction> Replace(IEnumerable<CodeInstruction> instructions) =>
            new[] { new CodeInstruction(OpCodes.Ldsfld, Field(typeof(Effects), nameof(Other))), new CodeInstruction(OpCodes.Ret) };
        public static IEnumerable<CodeInstruction> Escape(IEnumerable<CodeInstruction> instructions) =>
            new[] { new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Stsfld, Field(typeof(Effects), nameof(Escaped))) }.Concat(instructions);
        public static IEnumerable<CodeInstruction> Alias(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var local = generator.DeclareLocal(typeof(Comp));
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.IsLdarg() && instruction.ArgumentIndex() == 0)
                { yield return new(OpCodes.Stloc, local); yield return new(OpCodes.Ldloc, local); }
            }
        }
    }

    public sealed class Actor { }
    public sealed class Condition { public Actor Pawn = null!; }
    public class Comp
    {
        public Condition Parent = null!;
        public Actor Pawn { [MethodImpl(MethodImplOptions.NoInlining)] get => Parent.Pawn; }
    }
    public sealed class DerivedComp : Comp { }
    public sealed class OtherComp : Comp
    {
        public new Actor Pawn { [MethodImpl(MethodImplOptions.NoInlining)] get => Parent.Pawn; }
    }
}
