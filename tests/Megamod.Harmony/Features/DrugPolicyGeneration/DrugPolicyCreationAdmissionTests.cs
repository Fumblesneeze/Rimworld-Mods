using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.DrugPolicyGeneration;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Features.DrugPolicyGeneration;

[TestFixture, NonParallelizable]
public sealed class DrugPolicyCreationAdmissionTests
{
    private static ConstructorInfo Constructor(Type type) => type.GetConstructor(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null)!;
    private static DrugPolicyCreationAdmission Create() => new(Constructor(typeof(Policy)), Constructor(typeof(BasePolicy)),
        Constructor(typeof(Entry)), typeof(Database).GetProperty(nameof(Database.AllPolicies))!.GetMethod!,
        typeof(Database).GetField("policies", BindingFlags.Instance | BindingFlags.NonPublic)!);

    [Test]
    public void NativeShapedConstructionStaysDetachedAndGetterReturnsActualList()
    {
        var admission = Create(); var reports = new List<string>();
        try
        {
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(new("creation", AdmissionPhase.Early,
                new[] { admission.Candidate }), AdmissionPhase.Early), Is.True, string.Join("; ", reports));
            Assert.That(admission.Available, Is.True, admission.Failure);
            var database = new Database(); var captured = database.AllPolicies;
            var policy = new Policy(); var entry = new Entry();
            Assert.That(captured, Is.Empty);
            Assert.That(entry.Frequency, Is.EqualTo(1));
            captured.Add(policy);
            Assert.That(database.AllPolicies, Is.SameAs(captured));
            Assert.That(database.AllPolicies.Single(), Is.SameAs(policy));
        }
        finally { admission.Candidate.Rollback(); }
    }

    [Test]
    public void ScalarObserverIsAdmittedWithoutExecutingItForInspection()
    {
        var admission = Create(); var foreign = new HarmonyLib.Harmony("tests.creation.scalar-observer");
        var target = Constructor(typeof(Policy)); var reports = new List<string>(); Counters.Observed = 0;
        try
        {
            foreign.Patch(target, postfix: new HarmonyMethod(typeof(Counters).GetMethod(nameof(Counters.Observe))!));
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(new("creation", AdmissionPhase.Early,
                new[] { admission.Candidate }), AdmissionPhase.Early), Is.True, string.Join("; ", reports));
            Assert.That(admission.Available, Is.True, admission.Failure);
            Assert.That(Counters.Observed, Is.Zero);
            _ = new Policy();
            Assert.That(Counters.Observed, Is.EqualTo(1));
        }
        finally { try { admission.Candidate.Rollback(); } finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); } }
    }

    public static class Counters
    {
        public static int Observed, AttributeCalls, Compositions;
        [Probe, MethodImpl(MethodImplOptions.NoInlining)] public static void Observe() => Observed++;
        [MethodImpl(MethodImplOptions.NoInlining)] public static void ObserveOther() => Observed += 10;
    }
    [AttributeUsage(AttributeTargets.Method)]
    public sealed class ProbeAttribute : Attribute { public ProbeAttribute() { Counters.AttributeCalls++; } }

    [Test]
    public void CallbackTypeInitializerIsNotAssumedHarmlessFromAnEmptyMethod()
    {
        var admission = Create(); var foreign = new HarmonyLib.Harmony("tests.creation.static-initializer");
        var target = Constructor(typeof(Policy));
        try
        {
            admission.Candidate.Install();
            foreign.Patch(target, postfix: new HarmonyMethod(typeof(InitializingObserver).GetMethod(nameof(InitializingObserver.Observe))!));
            Assert.That(admission.Revalidate(), Is.False, "The implicit type-initializer call is part of the observer's effects.");
        }
        finally { try { admission.Candidate.Rollback(); } finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); } }
    }
    public static class InitializingObserver
    {
        static InitializingObserver() { Counters.Observed++; }
        [MethodImpl(MethodImplOptions.NoInlining)] public static void Observe() { }
    }

    [TestCase(typeof(Policy))]
    [TestCase(typeof(BasePolicy))]
    [TestCase(typeof(Entry))]
    public void EffectiveConstructorEscapeIsRefusedAndRemovalRestoresAdmission(Type type)
    {
        var admission = Create(); var foreign = new HarmonyLib.Harmony("tests.creation.escape"); var target = Constructor(type);
        try
        {
            admission.Candidate.Install();
            foreign.Patch(target, transpiler: new HarmonyMethod(typeof(Effects).GetMethod(nameof(Effects.EscapeBody))!));
            Assert.That(admission.Available, Is.False); Assert.That(admission.Failure, Does.Contain(target.DeclaringType!.FullName));
            Effects.Escaped = null;
            object created = type == typeof(Entry) ? new Entry() : new Policy();
            Assert.That(Effects.Escaped, Is.SameAs(created), "The fixture executes a real receiver escape in the effective constructor.");
            foreign.Unpatch(target, HarmonyPatchType.Transpiler, foreign.Id);
            Assert.That(admission.Available, Is.True, admission.Failure);
        }
        finally { Effects.Escaped = null; try { admission.Candidate.Rollback(); } finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); } }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ActualGetterSubstitutionOrReplacementIsRefused(bool postfix)
    {
        var admission = Create(); var foreign = new HarmonyLib.Harmony("tests.creation.getter");
        var target = typeof(Database).GetProperty(nameof(Database.AllPolicies))!.GetMethod!;
        try
        {
            admission.Candidate.Install(); var database = new Database(); var original = database.AllPolicies;
            if (postfix) foreign.Patch(target, postfix: new HarmonyMethod(typeof(Effects).GetMethod(nameof(Effects.ReplaceList))!));
            else foreign.Patch(target, transpiler: new HarmonyMethod(typeof(Effects).GetMethod(nameof(Effects.SubstituteList))!));
            Assert.That(admission.Available, Is.False);
            Assert.That(database.AllPolicies, Is.Not.SameAs(original));
        }
        finally { try { admission.Candidate.Rollback(); } finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); } }
    }

    [Test]
    public void ActualSkipOriginalIsRefused()
    {
        var admission = Create(); var foreign = new HarmonyLib.Harmony("tests.creation.skip"); var target = Constructor(typeof(Entry));
        try
        {
            admission.Candidate.Install();
            foreign.Patch(target, prefix: new HarmonyMethod(typeof(Effects).GetMethod(nameof(Effects.Skip))!));
            Assert.That(new Entry().Frequency, Is.Zero);
            Assert.That(admission.Available, Is.False);
        }
        finally { try { admission.Candidate.Rollback(); } finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); } }
    }

    [Test]
    public void NewObserverRequiresExplicitRevalidationAndCallbackBodyRepatchRevokesIt()
    {
        var admission = Create(); var foreign = new HarmonyLib.Harmony("tests.creation.callback-repatch");
        var target = Constructor(typeof(Policy)); var callback = typeof(Counters).GetMethod(nameof(Counters.Observe))!;
        try
        {
            admission.Candidate.Install(); foreign.Patch(target, postfix: new HarmonyMethod(callback));
            Assert.That(admission.Available, Is.False);
            Assert.That(HarmonyPublication.Inspect(callback, snapshot => snapshot.Transpilers.Length == 0), Is.True, "Available must not acquire an observer.");
            Assert.That(admission.Revalidate(), Is.True, admission.Failure);
            foreign.Patch(callback, transpiler: new HarmonyMethod(typeof(Effects).GetMethod(nameof(Effects.ClearDatabaseBody))!));
            Assert.That(admission.Available, Is.False, "Unchanged callback signature cannot retain admission after its body changes.");
            Effects.Database = new Database();
            Effects.Database.AllPolicies.Add((Policy)RuntimeHelpers.GetUninitializedObject(typeof(Policy)));
            _ = new Policy(); Assert.That(Effects.Database.AllPolicies, Is.Empty);
            foreign.Unpatch(callback, HarmonyPatchType.Transpiler, foreign.Id);
            Assert.That(admission.Available, Is.True, admission.Failure);
        }
        finally
        {
            Effects.Database = null;
            try { admission.Candidate.Rollback(); }
            finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); foreign.Unpatch(callback, HarmonyPatchType.All, foreign.Id); }
        }
    }

    [Test]
    public void InnerScalarObserverPreservesTheRequiredPath()
    {
        var admission = Create(); var foreign = new HarmonyLib.Harmony("tests.creation.inner"); var target = Constructor(typeof(Entry));
        Counters.Observed = 0;
        try
        {
            foreign.CreateProcessor(target).AddInnerPostfix(new HarmonyMethod(typeof(Counters).GetMethod(nameof(Counters.Observe))!)
                { innerTarget = InnerTarget.Constant(1f) }).Patch();
            admission.Candidate.Install();
            Assert.That(admission.Available, Is.True, admission.Failure);
            Assert.That(Counters.Observed, Is.Zero);
            Assert.That(new Entry().Frequency, Is.EqualTo(1));
            Assert.That(Counters.Observed, Is.EqualTo(1));
        }
        finally { try { admission.Candidate.Rollback(); } finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); } }
    }

    [Test]
    public void IrrelevantScalarInitializationAndArithmeticRemainAdmitted()
    {
        var admission = Create(); var foreign = new HarmonyLib.Harmony("tests.creation.scalar-edits"); var target = Constructor(typeof(Entry));
        try
        {
            foreign.Patch(target, transpiler: new HarmonyMethod(typeof(Effects).GetMethod(nameof(Effects.ScalarEdits))!));
            admission.Candidate.Install();
            Assert.That(admission.Available, Is.True, admission.Failure);
            Assert.That(new Entry().Frequency, Is.EqualTo(2), "The complete transaction overwrites these detached initial scalar choices.");
        }
        finally { try { admission.Candidate.Rollback(); } finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); } }
    }

    [Test]
    public void GetterLocalAliasesPreserveTheActualDatabaseList()
    {
        var admission = Create(); var foreign = new HarmonyLib.Harmony("tests.creation.getter-local");
        var target = typeof(Database).GetProperty(nameof(Database.AllPolicies))!.GetMethod!;
        try
        {
            foreign.Patch(target, transpiler: new HarmonyMethod(typeof(Effects).GetMethod(nameof(Effects.LocalGetter))!));
            admission.Candidate.Install();
            Assert.That(admission.Available, Is.True, admission.Failure);
            var database = new Database(); var list = database.AllPolicies; list.Add(new Policy());
            Assert.That(database.AllPolicies, Is.SameAs(list)); Assert.That(list, Has.Count.EqualTo(1));
        }
        finally { try { admission.Candidate.Rollback(); } finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); } }
    }

    [Test]
    public void InnerListReplacementAndHiddenGlobalCallbackEffectsAreRefused()
    {
        var admission = Create(); var foreign = new HarmonyLib.Harmony("tests.creation.inner-list");
        var target = typeof(Database).GetProperty(nameof(Database.AllPolicies))!.GetMethod!;
        try
        {
            admission.Candidate.Install(); var database = new Database(); var original = database.AllPolicies;
            foreign.CreateProcessor(target).AddInnerPostfix(new HarmonyMethod(typeof(Effects).GetMethod(nameof(Effects.ReplaceList))!)
            { innerTarget = new InnerTarget(typeof(Database).GetField("policies", BindingFlags.NonPublic | BindingFlags.Instance)!, InnerTargetKind.FieldRead) }).Patch();
            Assert.That(admission.Available, Is.False);
            Assert.That(database.AllPolicies, Is.Not.SameAs(original));
            foreign.Unpatch(target, HarmonyPatchType.InnerPostfix, foreign.Id);
            Assert.That(admission.Available, Is.True, admission.Failure);
            foreign.CreateProcessor(target).AddInnerPostfix(new HarmonyMethod(typeof(Effects).GetMethod(nameof(Effects.ClearDatabase))!)
            { innerTarget = new InnerTarget(typeof(Database).GetField("policies", BindingFlags.NonPublic | BindingFlags.Instance)!, InnerTargetKind.FieldRead) }).Patch();
            Assert.That(admission.Revalidate(), Is.False);
        }
        finally { try { admission.Candidate.Rollback(); } finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); } }
    }

    [Test]
    public void RemovingOwnedObserverCannotLeaveAStalePositiveReceipt()
    {
        var admission = Create(); var owner = new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId);
        try
        {
            admission.Candidate.Install(); Assert.That(admission.Available, Is.True);
            owner.Unpatch(Constructor(typeof(Policy)), HarmonyPatchType.Transpiler, owner.Id);
            Assert.That(admission.Available, Is.False);
            Assert.That(admission.Revalidate(), Is.False, "A removed observer is not a new callback dependency.");
        }
        finally { admission.Candidate.Rollback(); }
    }

    [Test]
    public void ReplacingHarmlessObserversBeyondTheBoundRetainsOnlyCurrentDependencies()
    {
        var assembly = AssemblyBuilder.DefineDynamicAssembly(new AssemblyName("CreationObservers" + Guid.NewGuid().ToString("N")), AssemblyBuilderAccess.Run);
        var type = assembly.DefineDynamicModule("Observers").DefineType("Observers", TypeAttributes.Public | TypeAttributes.Abstract | TypeAttributes.Sealed);
        for (int index = 0; index < 36; index++)
            type.DefineMethod("Observe" + index, MethodAttributes.Public | MethodAttributes.Static, typeof(void), Type.EmptyTypes).GetILGenerator().Emit(OpCodes.Ret);
        var callbacks = type.CreateType()!.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly);
        var admission = Create(); var foreign = new HarmonyLib.Harmony("tests.creation.observer-churn"); var target = Constructor(typeof(Policy));
        try
        {
            admission.Candidate.Install();
            foreach (var callback in callbacks)
            {
                foreign.Unpatch(target, HarmonyPatchType.Postfix, foreign.Id);
                foreign.Patch(target, postfix: new HarmonyMethod(callback));
                Assert.That(admission.Revalidate(), Is.True, callback.Name + ": " + admission.Failure);
                _ = new Policy();
            }
            foreach (var old in callbacks.Take(callbacks.Length - 1))
                Assert.That(HarmonyPublication.Inspect(old, snapshot => snapshot.Transpilers.All(p => p.owner != OwnedHarmonyPatch.OwnerId)), Is.True);
            Assert.That(admission.Available, Is.True, admission.Failure);
        }
        finally { try { admission.Candidate.Rollback(); } finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); } }
    }

    [Test]
    public void NestedCallbackObserversAreTrackedAndPrunedWithTheirRoot()
    {
        var admission = Create(); var foreign = new HarmonyLib.Harmony("tests.creation.nested-callback"); var target = Constructor(typeof(Policy));
        var first = typeof(Counters).GetMethod(nameof(Counters.Observe))!; var second = typeof(Counters).GetMethod(nameof(Counters.ObserveOther))!;
        try
        {
            foreign.Patch(first, postfix: new HarmonyMethod(second)); foreign.Patch(target, postfix: new HarmonyMethod(first));
            admission.Candidate.Install(); Counters.Observed = 0;
            Assert.That(admission.Available, Is.True, admission.Failure);
            _ = new Policy(); Assert.That(Counters.Observed, Is.EqualTo(11));
            foreign.Unpatch(target, HarmonyPatchType.Postfix, foreign.Id);
            Assert.That(admission.Revalidate(), Is.True, admission.Failure);
            foreach (var callback in new[] { first, second })
                Assert.That(HarmonyPublication.Inspect(callback, snapshot => snapshot.Transpilers.Length == 0), Is.True);
            Assert.That(HarmonyPublication.Inspect(first, snapshot => snapshot.Postfixes.Single().Owner == foreign.Id), Is.True);
        }
        finally { try { admission.Candidate.Rollback(); } finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); foreign.Unpatch(first, HarmonyPatchType.All, foreign.Id); } }
    }

    [Test]
    public void AvailabilityDoesNotReplayForeignTranspilersOrConstructAttributes()
    {
        var admission = Create(); var foreign = new HarmonyLib.Harmony("tests.creation.inert-inspection"); var target = Constructor(typeof(Entry));
        try
        {
            foreign.Patch(target, transpiler: new HarmonyMethod(typeof(Effects).GetMethod(nameof(Effects.ScalarEdits))!),
                postfix: new HarmonyMethod(typeof(Counters).GetMethod(nameof(Counters.Observe))!));
            admission.Candidate.Install();
            Assert.That(Counters.Compositions, Is.GreaterThan(0)); Assert.That(Counters.AttributeCalls, Is.GreaterThan(0));
            Counters.Compositions = Counters.AttributeCalls = Counters.Observed = 0;
            for (int index = 0; index < 3; index++) Assert.That(admission.Available, Is.True, admission.Failure);
            Assert.That(new[] { Counters.Compositions, Counters.AttributeCalls, Counters.Observed }, Is.EqualTo(new[] { 0, 0, 0 }));
        }
        finally { try { admission.Candidate.Rollback(); } finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); } }
    }

    [Test]
    public void CleanupContinuesAfterOneChangedSlotAndRetainsThatOwnershipForRetry()
    {
        var admission = Create(); var owner = new HarmonyLib.Harmony(OwnedHarmonyPatch.OwnerId);
        var target = typeof(Database).GetProperty(nameof(Database.AllPolicies))!.GetMethod!;
        var interference = typeof(Effects).GetMethod(nameof(Effects.Passthrough))!;
        try
        {
            admission.Candidate.Install(); owner.Patch(target, transpiler: new HarmonyMethod(interference));
            Assert.Throws<AggregateException>(() => admission.Candidate.Rollback());
            Assert.That(admission.Available, Is.False);
            foreach (var other in new MethodBase[] { Constructor(typeof(Policy)), Constructor(typeof(BasePolicy)), Constructor(typeof(Entry)) })
                Assert.That(HarmonyPublication.Inspect(other, snapshot => snapshot.Transpilers.Length == 0), Is.True);
            owner.Unpatch(target, interference); admission.Candidate.Rollback();
            Assert.That(HarmonyPublication.Inspect(target, snapshot => snapshot.Transpilers.Length == 0), Is.True);
        }
        finally { owner.Unpatch(target, interference); admission.Candidate.Rollback(); }
    }

    [Test]
    public void ParameterlessObserverWithNativeGlobalEffectsIsRefusedAndAcquiredPatchIsCleaned()
    {
        var admission = Create(); var foreign = new HarmonyLib.Harmony("tests.creation.global-effects");
        var target = Constructor(typeof(Policy)); var callback = typeof(Effects).GetMethod(nameof(Effects.ClearDatabase))!;
        try
        {
            admission.Candidate.Install(); foreign.Patch(target, postfix: new HarmonyMethod(callback));
            Assert.That(admission.Revalidate(), Is.False);
            Assert.That(admission.Failure, Does.Contain(nameof(Effects.ClearDatabase)));
            Assert.That(HarmonyPublication.Inspect(callback, snapshot => snapshot.Transpilers.All(p => p.owner != OwnedHarmonyPatch.OwnerId)), Is.True);
            foreign.Unpatch(target, HarmonyPatchType.Postfix, foreign.Id);
            Assert.That(admission.Available, Is.True, admission.Failure);
        }
        finally { try { admission.Candidate.Rollback(); } finally { foreign.Unpatch(target, HarmonyPatchType.All, foreign.Id); } }
    }

    public static class Effects
    {
        public static object? Escaped;
        public static Database? Database;
        public static bool Skip() => false;
        public static void ReplaceList(ref List<Policy> __result) => __result = new();
        [MethodImpl(MethodImplOptions.NoInlining)] public static void ClearDatabase() => Database!.AllPolicies.Clear();
        public static IEnumerable<CodeInstruction> ClearDatabaseBody(IEnumerable<CodeInstruction> instructions) => new[]
        {
            new CodeInstruction(OpCodes.Ldsfld, typeof(Effects).GetField(nameof(Database))),
            new CodeInstruction(OpCodes.Callvirt, typeof(Database).GetProperty(nameof(Database.AllPolicies))!.GetMethod),
            new CodeInstruction(OpCodes.Callvirt, typeof(List<Policy>).GetMethod(nameof(List<Policy>.Clear))),
            new CodeInstruction(OpCodes.Ret)
        };
        public static IEnumerable<CodeInstruction> SubstituteList(IEnumerable<CodeInstruction> instructions) => new[]
        { new CodeInstruction(OpCodes.Newobj, typeof(List<Policy>).GetConstructor(Type.EmptyTypes)), new CodeInstruction(OpCodes.Ret) };
        public static IEnumerable<CodeInstruction> EscapeBody(IEnumerable<CodeInstruction> instructions)
        {
            foreach (var instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ret)
                {
                    yield return new(OpCodes.Ldarg_0);
                    yield return new(OpCodes.Stsfld, typeof(Effects).GetField(nameof(Escaped)));
                }
                yield return instruction;
            }
        }
        public static IEnumerable<CodeInstruction> ScalarEdits(IEnumerable<CodeInstruction> instructions)
        {
            Counters.Compositions++;
            yield return new(OpCodes.Ldc_I4_4); yield return new(OpCodes.Ldc_I4_5);
            yield return new(OpCodes.Add); yield return new(OpCodes.Pop);
            foreach (var instruction in instructions)
                yield return instruction.opcode == OpCodes.Ldc_R4 ? new(OpCodes.Ldc_R4, 2f) : instruction;
        }
        public static IEnumerable<CodeInstruction> Passthrough(IEnumerable<CodeInstruction> instructions) => instructions;
        public static IEnumerable<CodeInstruction> LocalGetter(IEnumerable<CodeInstruction> instructions, ILGenerator generator)
        {
            var receiver = generator.DeclareLocal(typeof(Database)); var list = generator.DeclareLocal(typeof(List<Policy>));
            return new[]
            {
                new CodeInstruction(OpCodes.Nop), new CodeInstruction(OpCodes.Ldarg_0), new CodeInstruction(OpCodes.Stloc, receiver),
                new CodeInstruction(OpCodes.Ldloc, receiver), new CodeInstruction(OpCodes.Ldfld, typeof(Database).GetField("policies", BindingFlags.Instance | BindingFlags.NonPublic)),
                new CodeInstruction(OpCodes.Stloc, list), new CodeInstruction(OpCodes.Ldloc, list), new CodeInstruction(OpCodes.Ret)
            };
        }
    }

    public class BasePolicy { public int Id; [MethodImpl(MethodImplOptions.NoInlining)] protected BasePolicy() { } }
    public sealed class Policy : BasePolicy { [MethodImpl(MethodImplOptions.NoInlining)] public Policy() { } }
    public sealed class Entry { public float Frequency = 1; [MethodImpl(MethodImplOptions.NoInlining)] public Entry() { } }
    public sealed class Database
    {
        private readonly List<Policy> policies = new();
        public List<Policy> AllPolicies { [MethodImpl(MethodImplOptions.NoInlining)] get => policies; }
    }
}
