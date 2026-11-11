using System.Reflection;
using HarmonyLib;
using Megamod.Features.DrugTolerance;
using Megamod.Patching;
using NUnit.Framework;
using Editor = Megamod.Harmony.Tests.Features.DrugTolerance.DrugToleranceEditorCallGuardTests.NativeEditor;
using Rectangle = Megamod.Harmony.Tests.Features.DrugTolerance.DrugToleranceEditorCallGuardTests.Rectangle;

namespace Megamod.Harmony.Tests.Features.DrugTolerance;

[TestFixture, NonParallelizable]
public sealed class DrugToleranceAllocationAdmissionTests
{
    private static readonly MethodInfo Target = typeof(Editor).GetMethod(nameof(Editor.Calculate))!;
    private static long observedState;
    [TestCase(nameof(WriteObjectArguments))]
    [TestCase(nameof(WriteAliasedArguments))]
    [TestCase(nameof(WriteInterfaceArguments))]
    public void EffectiveArgumentArrayWritesRevokeAllocationDespiteTheDeclaredType(string callback)
    {
        var admission = new DrugToleranceAllocationAdmission(Target, new(typeof(Rectangle).GetProperty(nameof(Rectangle.Width))!.GetMethod!));
        var foreign = new HarmonyLib.Harmony("tests.tolerance.allocation-array");
        var reports = new List<string>();
        try
        {
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(new("allocation", AdmissionPhase.Early, new[] { admission.Candidate }), AdmissionPhase.Early), Is.True);
            foreign.Patch(Target, postfix: new HarmonyMethod(typeof(DrugToleranceAllocationAdmissionTests), callback));
            var editor = new Editor(); editor.Row(new Rectangle { Width = 935 });
            Assert.That(editor.Last, Is.EqualTo(777).Within(0.001f), "Provider RestoreArgumentArray restores the captured zero outputs plus the injected width.");
            Assert.That(admission.Available, Is.False, "An effective mutable argument array cannot authorize the native allocation.");
        }
        finally { try { admission.Candidate.Rollback(); } finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); } }
    }
    [Test]
    public void PrivatePrefixStateDoesNotInvalidateNativeAllocation()
    {
        var admission = new DrugToleranceAllocationAdmission(Target, new(typeof(Rectangle).GetProperty(nameof(Rectangle.Width))!.GetMethod!));
        var foreign = new HarmonyLib.Harmony("tests.tolerance.allocation-state");
        var reports = new List<string>(); observedState = 0;
        try
        {
            foreign.Patch(Target, prefix: new HarmonyMethod(typeof(DrugToleranceAllocationAdmissionTests), nameof(CaptureState)),
                postfix: new HarmonyMethod(typeof(DrugToleranceAllocationAdmissionTests), nameof(ObserveState)));
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(new("allocation", AdmissionPhase.Early, new[] { admission.Candidate }), AdmissionPhase.Early), Is.True, string.Join("; ", reports));
            var editor = new Editor(); editor.Row(new Rectangle { Width = 935 });
            Assert.That(editor.Last, Is.EqualTo(935).Within(0.001f));
            Assert.That(observedState, Is.EqualTo(42)); Assert.That(admission.Available, Is.True);
        }
        finally { try { admission.Candidate.Rollback(); } finally { foreign.Unpatch(Target, HarmonyPatchType.All, foreign.Id); } }
    }
    public static void WriteObjectArguments(object __args) => ((object[])__args)[1] = 777f;
    public static void WriteAliasedArguments([HarmonyArgument("__args")] object renamed) => ((object[])renamed)[1] = 777f;
    public static void WriteInterfaceArguments(System.Collections.IList __args) => __args[1] = 777f;
    public static void CaptureState(out long __state) => __state = 42;
    public static void ObserveState(long __state) => observedState = __state;
    [Test]
    public void PublishedNativeCalculatorReceiptRetainsNativeAllocation()
    {
        var guard = new DrugToleranceColumnWidthGuard(typeof(Rectangle).GetProperty(nameof(Rectangle.Width))!.GetMethod!);
        var admission = new DrugToleranceAllocationAdmission(Target, guard);
        var reports = new List<string>();
        try
        {
            Assert.That(new AdmissionCoordinator(reports.Add).Apply(new("allocation", AdmissionPhase.Early, new[] { admission.Candidate }), AdmissionPhase.Early), Is.True);
            Assert.That(admission.Available, Is.True);
            var editor = new Editor(); editor.Row(new Rectangle { Width = 935 });
            Assert.That(editor.Last, Is.EqualTo(935).Within(0.001f));
        }
        finally { admission.Candidate.Rollback(); }
    }
}
