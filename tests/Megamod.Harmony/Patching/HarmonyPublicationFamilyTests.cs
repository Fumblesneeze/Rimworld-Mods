using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.Harmony.Tests.Patching;

[TestFixture, NonParallelizable]
public sealed class HarmonyPublicationFamilyTests
{
    private static readonly MethodInfo Definition = typeof(HarmonyPublicationFamilyTests).GetMethod(nameof(Native))!;
    private static readonly MethodInfo Integer = Definition.MakeGenericMethod(typeof(int));
    private static int attributes, factories, compositions, callbacks;

    [Test]
    public void EmptyFamilyReceiptBecomesStaleWhenAClosedMemberIsPublished()
    {
        var owner = new HarmonyLib.Harmony("tests.publication.family.new-member");
        try
        {
            Assert.That(Native<int>(7), Is.EqualTo(7));
            Assert.That(HarmonyPublication.TryCaptureFamily(Definition, out var empty), Is.True);
            Assert.That(empty!.PublishedMembers, Is.Empty);
            Assert.That(empty.IsCurrent(), Is.True);

            owner.Patch(Integer, prefix: new HarmonyMethod(typeof(HarmonyPublicationFamilyTests), nameof(Increment)));
            Assert.That(Native<int>(7), Is.EqualTo(8), "The owned target really executes the published patch.");
            Assert.That(empty.IsCurrent(), Is.False, "A new closure must invalidate an older empty-family receipt.");
            Assert.That(empty.PublishedMembers, Is.Empty, "The old inventory remains immutable.");

            Assert.That(HarmonyPublication.TryCaptureFamily(Definition, out var current), Is.True);
            Assert.That(current!.PublishedMembers, Is.EquivalentTo(new[] { Integer }));
            Assert.That(current.IsCurrent(), Is.True);
        }
        finally { owner.Unpatch(Integer, HarmonyPatchType.All, owner.Id); }
        Assert.That(Native<int>(7), Is.EqualTo(7));
    }

    [Test]
    public void MembershipChangesAndReplacementsInvalidateOldReceiptsPermanently()
    {
        var definition = Method(nameof(Changing));
        var integer = definition.MakeGenericMethod(typeof(int));
        var smaller = definition.MakeGenericMethod(typeof(short));
        var owner = new HarmonyLib.Harmony("tests.publication.family.changes");
        try
        {
            owner.Patch(integer, prefix: Patch(nameof(Increment)));
            Assert.That(Changing<int>(7), Is.EqualTo(8));
            var first = Capture(definition);
            owner.Patch(smaller, prefix: Patch(nameof(Increment)));
            Assert.That(Changing<short>(7), Is.EqualTo(8));
            Assert.That(first.IsCurrent(), Is.False);
            var both = Capture(definition);
            Assert.That(both.PublishedMembers, Is.EquivalentTo(new[] { integer, smaller }));
            owner.Unpatch(integer, HarmonyPatchType.Prefix, owner.Id);
            Assert.That(Changing<int>(7), Is.EqualTo(7));
            Assert.That(both.IsCurrent(), Is.False);
            owner.Patch(integer, prefix: Patch(nameof(Increment)));
            Assert.That(Changing<int>(7), Is.EqualTo(8));
            Assert.That(first.IsCurrent(), Is.False);
            Assert.That(both.IsCurrent(), Is.False);
            Assert.That(Capture(definition).IsCurrent(), Is.True);
        }
        finally { Cleanup(owner, integer, smaller); }
    }

    [Test]
    public void TransientPatchAndUnpatchCannotEscapeAnUncheckedEmptyFamilyReceipt()
    {
        var definition = Method(nameof(Transient));
        var target = definition.MakeGenericMethod(typeof(UnknownStruct));
        var owner = new HarmonyLib.Harmony("tests.publication.family.transient");
        try
        {
            var before = Capture(definition);
            Assert.That(before.PublishedMembers, Is.Empty);
            owner.Patch(target, prefix: Patch(nameof(Increment)));
            Assert.That(Transient<UnknownStruct>(7), Is.EqualTo(8));
            owner.Unpatch(target, HarmonyPatchType.All, owner.Id);
            Assert.That(Transient<UnknownStruct>(7), Is.EqualTo(7));
            // Deliberately no intermediate IsCurrent call while the patch was present.
            Assert.That(before.IsCurrent(), Is.False);
            var after = Capture(definition);
            Assert.That(after.PublishedMembers, Does.Contain(target),
                "This inspected provider retains the new publication key after unpatch, preserving the history witness.");
            Assert.That(after.IsCurrent(), Is.True);
        }
        finally { Cleanup(owner, target); }
    }

    [Test]
    public void UnrelatedPublicationsDoNotInvalidateTheSelectedFamily()
    {
        var definition = Method(nameof(Selected));
        var selected = definition.MakeGenericMethod(typeof(decimal));
        var ordinary = Method(nameof(Unrelated));
        var other = Method(nameof(OtherFamily)).MakeGenericMethod(typeof(UnknownStruct));
        var owner = new HarmonyLib.Harmony("tests.publication.family.unrelated");
        try
        {
            owner.Patch(selected, prefix: Patch(nameof(Increment)));
            var receipt = Capture(definition);
            owner.Patch(ordinary, prefix: Patch(nameof(Increment)));
            owner.Patch(other, prefix: Patch(nameof(Increment)));
            Assert.That(Unrelated(7), Is.EqualTo(8));
            Assert.That(OtherFamily<UnknownStruct>(7), Is.EqualTo(8));
            Assert.That(receipt.IsCurrent(), Is.True);
            Assert.That(receipt.PublishedMembers, Is.EquivalentTo(new[] { selected }));
            owner.Unpatch(other, HarmonyPatchType.All, owner.Id);
            Assert.That(receipt.IsCurrent(), Is.True);
        }
        finally { Cleanup(owner, selected, ordinary, other); }
    }

    [Test]
    public void PassiveReadsDoNotReplayForeignCompositionOrConstructAttributes()
    {
        var definition = Method(nameof(Observed));
        var target = definition.MakeGenericMethod(typeof(UnknownStruct));
        var owner = new HarmonyLib.Harmony("tests.publication.family.passive");
        try
        {
            owner.Patch(target, prefix: Patch(nameof(PrefixFactory)), transpiler: Patch(nameof(CountComposition)));
            Assert.That(Observed<UnknownStruct>(7), Is.EqualTo(7));
            Assert.That(callbacks, Is.GreaterThan(0));
            (int Attributes, int Factories, int Compositions, int Callbacks) baseline = (attributes, factories, compositions, callbacks);
            var receipt = Capture(definition);
            for (int count = 0; count < 8; count++)
            {
                Assert.That(receipt.IsCurrent(), Is.True);
                Assert.That(Capture(definition).IsCurrent(), Is.True);
            }
            Assert.That((attributes, factories, compositions, callbacks), Is.EqualTo(baseline));
        }
        finally { Cleanup(owner, target); }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void BusyProviderMonitorsRefuseAndLatchWithoutWaiting(bool stateMonitor)
    {
        var definition = Method(nameof(Busy));
        var owner = new HarmonyLib.Harmony("tests.publication.family.busy");
        var target = definition.MakeGenericMethod(typeof(int));
        try
        {
            owner.Patch(target, prefix: Patch(nameof(Increment)));
            var receipt = Capture(definition);
            object gate = stateMonitor
                ? typeof(PatchProcessor).Assembly.GetType("HarmonyLib.HarmonySharedState")!.GetField("state", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!
                : typeof(PatchProcessor).GetField("locker", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            Monitor.Enter(gate);
            try
            {
                Assert.That(HarmonyPublication.TryCaptureFamily(definition, out _), Is.False, "Same-thread monitor reentry cannot inspect a partial publication.");
                var worker = Task.Run(() => (Captured: HarmonyPublication.TryCaptureFamily(definition, out _), Current: receipt.IsCurrent()));
                Assert.That(worker.Wait(TimeSpan.FromSeconds(3)), Is.True, "Inspection must not block on a foreign monitor owner.");
                Assert.That(worker.Result, Is.EqualTo((false, false)));
            }
            finally { Monitor.Exit(gate); }
            Assert.That(receipt.IsCurrent(), Is.False, "A failed old receipt stays failed after contention ends.");
            Assert.That(Capture(definition).IsCurrent(), Is.True);
        }
        finally { Cleanup(owner, target); }
    }

    private static MethodInfo Method(string name) => typeof(HarmonyPublicationFamilyTests).GetMethod(name)!;
    private static HarmonyMethod Patch(string name) => new(Method(name));
    private static HarmonyPublication.FamilyIdentity Capture(MethodInfo definition)
    {
        Assert.That(HarmonyPublication.TryCaptureFamily(definition, out var result), Is.True);
        return result!;
    }
    private static void Cleanup(HarmonyLib.Harmony owner, params MethodInfo[] targets)
    {
        var failures = new List<Exception>();
        foreach (var target in targets)
            try { owner.Unpatch(target, HarmonyPatchType.All, owner.Id); }
            catch (Exception failure) { failures.Add(failure); }
        if (failures.Count != 0) throw new AggregateException(failures);
    }

    [AttributeUsage(AttributeTargets.Parameter)]
    public sealed class CountAttribute : Attribute { public CountAttribute() => attributes++; }
    public static void Observe([Count] int value) => callbacks++;
    public static MethodInfo PrefixFactory(MethodBase target) { factories++; return Method(nameof(Observe)); }
    public static IEnumerable<CodeInstruction> CountComposition(IEnumerable<CodeInstruction> source) { compositions++; return source; }

    public static void Increment(ref int value) => value++;
    [MethodImpl(MethodImplOptions.NoInlining)]
    public static int Native<T>(int value) where T : struct => value;
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Changing<T>(int value) where T : struct => value;
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Selected<T>(int value) where T : struct => value;
    [MethodImpl(MethodImplOptions.NoInlining)] public static int OtherFamily<T>(int value) where T : struct => value;
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Observed<T>(int value) where T : struct => value;
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Busy<T>(int value) where T : struct => value;
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Transient<T>(int value) where T : struct => value;
    [MethodImpl(MethodImplOptions.NoInlining)] public static int Unrelated(int value) => value;
    public readonly struct UnknownStruct { }
}
