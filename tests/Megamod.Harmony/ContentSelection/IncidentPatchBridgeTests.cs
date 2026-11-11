using System.Runtime.CompilerServices;
using System.Reflection;
using HarmonyLib;
using Megamod.Features.ContentSelection.Harmony;
using Megamod.Patching;
using NUnit.Framework;

namespace Megamod.HarmonyTests.ContentSelection;

[TestFixture, NonParallelizable]
public sealed class IncidentPatchBridgeTests
{
    private static int factoryCalls;
    [Test]
    public void DeniedCompilationNeedsOnlyOneAutomaticRefreshAndLaterCompatibleRebuildRecovers()
    {
        var target = typeof(Worker).GetMethod(nameof(Worker.TryExecute))!;
        var foreign = new HarmonyLib.Harmony("test.incident.refresh-generation");
        var bridge = new IncidentPatchBridge(typeof(Worker), typeof(Parms), typeof(SampleDef), _ => true);
        int attempts = 0;
        var deniedReasons = new List<string?>();
        void Pump()
        {
            if (!bridge.NeedsPublicationRefresh) return;
            attempts++;
            if (!bridge.RefreshPublication()) deniedReasons.Add(bridge.Failure);
        }
        try
        {
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(bridge.Group, AdmissionPhase.DefsReady), Is.True);
            bridge.Enable();
            Assert.That(bridge.NeedsPublicationRefresh, Is.False);
            foreign.Patch(target, postfix: new HarmonyMethod(typeof(IncidentPatchBridgeTests), nameof(PassThroughAllow)));
            Assert.That(bridge.NeedsPublicationRefresh, Is.True);
            Pump();
            Pump();
            Pump();
            Assert.Multiple(() =>
            {
                Assert.That(bridge.Available, Is.False);
                Assert.That(bridge.NeedsPublicationRefresh, Is.False, "A denied generation has already been inspected.");
                Assert.That(attempts, Is.EqualTo(1));
                Assert.That(deniedReasons.Count, Is.EqualTo(1), "The composition owner has one failure reason to report for this generation.");
                Assert.That(deniedReasons[0], Is.Not.Null.And.Not.Empty);
            });

            foreign.Unpatch(target, HarmonyPatchType.Postfix, foreign.Id);
            foreign.Patch(target, prefix: new HarmonyMethod(typeof(IncidentPatchBridgeTests), nameof(Observer)));
            Assert.That(bridge.NeedsPublicationRefresh, Is.True, "A later normal rebuild opens a fresh attempt.");
            Pump();
            Assert.That(attempts, Is.EqualTo(2));
            Assert.That(bridge.Available, Is.True);
            Assert.That(bridge.NeedsPublicationRefresh, Is.False);
            Assert.That(deniedReasons.Count, Is.EqualTo(1));
            var worker = new Worker();
            Assert.That(worker.TryExecute(new Parms()), Is.False);
            Assert.That(worker.Executions, Is.Zero);
        }
        finally
        {
            foreach (var patch in bridge.Group.Patches.Reverse()) patch.Rollback();
            foreign.UnpatchAll(foreign.Id);
        }
    }

    [Test]
    public void ForeignFactoryRejectsWholeGroupBeforeAdmissionExecutesForeignCode()
    {
        var target = typeof(Worker).GetMethod(nameof(Worker.TryExecute))!;
        var foreign = new HarmonyLib.Harmony("test.incident.factory");
        var bridge = new IncidentPatchBridge(typeof(Worker), typeof(Parms), typeof(SampleDef), _ => true);
        try
        {
            foreign.Patch(target, postfix: new HarmonyMethod(typeof(IncidentPatchBridgeTests), nameof(Factory)));
            int before = factoryCalls;
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(bridge.Group, AdmissionPhase.DefsReady), Is.False);
            Assert.Multiple(() =>
            {
                Assert.That(factoryCalls, Is.EqualTo(before));
                Assert.That(bridge.Available, Is.False);
                Assert.That(HarmonyLib.Harmony.GetPatchInfo(typeof(Worker).GetMethod(nameof(Worker.CanFireNow))!)?.Owners,
                    Does.Not.Contain(OwnedHarmonyPatch.OwnerId));
            });
        }
        finally
        {
            foreach (var patch in bridge.Group.Patches.Reverse()) patch.Rollback();
            foreign.UnpatchAll(foreign.Id);
        }
    }

    private static MethodInfo Factory(MethodBase original) { factoryCalls++; return typeof(IncidentPatchBridgeTests).GetMethod(nameof(Observer), BindingFlags.NonPublic | BindingFlags.Static)!; }
    private static void Observer() { }

    [Test]
    public void ForeignSkipWithTrueResultIsStillVetoedAndEffectFree()
    {
        var foreign = new HarmonyLib.Harmony("test.incident.skip");
        var bridge = new IncidentPatchBridge(typeof(Worker), typeof(Parms), typeof(SampleDef), _ => true);
        try
        {
            foreign.Patch(typeof(Worker).GetMethod(nameof(Worker.TryExecute))!,
                prefix: new HarmonyMethod(typeof(IncidentPatchBridgeTests), nameof(SkipTrue)));
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(bridge.Group, AdmissionPhase.DefsReady), Is.True);
            Assert.That(bridge.RefreshPublication(), Is.True);
            bridge.Enable();
            var worker = new Worker();
            Assert.That(worker.TryExecute(new Parms()), Is.False);
            Assert.That(worker.Executions, Is.Zero);
        }
        finally
        {
            foreach (var patch in bridge.Group.Patches.Reverse()) patch.Rollback();
            foreign.UnpatchAll(foreign.Id);
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void FinalResultWriterPreventsPublicationWhileFinalObserverRemainsCompatible(bool writer)
    {
        var foreign = new HarmonyLib.Harmony("test.incident.finalizer");
        var bridge = new IncidentPatchBridge(typeof(Worker), typeof(Parms), typeof(SampleDef), _ => true);
        try
        {
            foreign.Patch(typeof(Worker).GetMethod(nameof(Worker.TryExecute))!, finalizer:
                new HarmonyMethod(typeof(IncidentPatchBridgeTests), writer ? nameof(ForceTrue) : nameof(Observer)));
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(bridge.Group, AdmissionPhase.DefsReady), Is.EqualTo(!writer));
            Assert.That(bridge.RefreshPublication(), Is.EqualTo(!writer));
            Assert.That(bridge.Available, Is.EqualTo(!writer));
        }
        finally
        {
            foreach (var patch in bridge.Group.Patches.Reverse()) patch.Rollback();
            foreign.UnpatchAll(foreign.Id);
        }
    }

    private static bool SkipTrue(ref bool __result) { __result = true; return false; }
    private static void ForceTrue(ref bool __result) { __result = true; }

    [TestCase(nameof(Worker.CanFireNow), true, true)]
    [TestCase(nameof(Worker.CanFireNow), false, true)]
    [TestCase(nameof(Worker.TryExecute), true, true)]
    [TestCase(nameof(Worker.TryExecute), false, true)]
    [TestCase(nameof(Worker.CanFireNow), true, false)]
    [TestCase(nameof(Worker.CanFireNow), false, false)]
    [TestCase(nameof(Worker.TryExecute), true, false)]
    [TestCase(nameof(Worker.TryExecute), false, false)]
    public void PassThroughResultWriterRejectsGroupRegardlessOfMetadataOrderButResultReaderRemainsCompatible(
        string methodName, bool sortedBefore, bool passThroughWriter)
    {
        var target = typeof(Worker).GetMethod(methodName)!;
        var foreign = new HarmonyLib.Harmony("test.incident.pass-through");
        var callback = typeof(IncidentPatchBridgeTests).GetMethod(
            passThroughWriter ? nameof(PassThroughAllow) : nameof(ReadResult), BindingFlags.NonPublic | BindingFlags.Static)!;
        var bridge = new IncidentPatchBridge(typeof(Worker), typeof(Parms), typeof(SampleDef), _ => true);
        try
        {
            foreign.Patch(target, postfix: new HarmonyMethod(callback)
            {
                before = sortedBefore ? new[] { OwnedHarmonyPatch.OwnerId } : Array.Empty<string>(),
                after = sortedBefore ? Array.Empty<string>() : new[] { OwnedHarmonyPatch.OwnerId }
            });
            bool admitted = new AdmissionCoordinator(_ => { }).Apply(bridge.Group, AdmissionPhase.DefsReady);
            if (admitted) bridge.Enable();
            var worker = new Worker();
            bool result = methodName == nameof(Worker.TryExecute) ? worker.TryExecute(new Parms()) : worker.CanFireNow(new Parms());
            Assert.Multiple(() =>
            {
                Assert.That(admitted, Is.EqualTo(!passThroughWriter));
                Assert.That(bridge.Available, Is.EqualTo(!passThroughWriter));
                Assert.That(result, Is.EqualTo(!admitted), "An admitted exclusion cannot return true after its void veto.");
                Assert.That(worker.Executions, Is.EqualTo(!admitted && methodName == nameof(Worker.TryExecute) ? 1 : 0));
                Assert.That(HarmonyLib.Harmony.GetPatchInfo(target)!.Owners, Does.Contain(foreign.Id));
                if (!admitted)
                    Assert.That(HarmonyLib.Harmony.GetPatchInfo(target)!.Owners, Does.Not.Contain(OwnedHarmonyPatch.OwnerId));
            });
        }
        finally
        {
            foreach (var patch in bridge.Group.Patches.Reverse()) patch.Rollback();
            foreign.UnpatchAll(foreign.Id);
        }
    }

    private static bool PassThroughAllow(bool result) => true;
    private static void ReadResult(bool __result) { }

    [TestCase(false)]
    [TestCase(true)]
    public void CallbackFaultInvalidatesPolicyWithoutLosingAnAlreadyMadeRefusal(bool refusalFault)
    {
        int reports = 0;
        var foreign = new HarmonyLib.Harmony("test.incident.fault-result");
        var bridge = new IncidentPatchBridge(typeof(Worker), typeof(Parms), typeof(SampleDef),
            _ => refusalFault ? true : throw new InvalidOperationException("predicate failed"),
            _ => throw new InvalidOperationException("notification failed"),
            _ => { reports++; throw new InvalidOperationException("recorder failed"); });
        try
        {
            foreign.Patch(typeof(Worker).GetMethod(nameof(Worker.TryExecute))!,
                postfix: new HarmonyMethod(typeof(IncidentPatchBridgeTests), nameof(ForceTrue)));
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(bridge.Group, AdmissionPhase.DefsReady), Is.True);
            Assert.That(bridge.RefreshPublication(), Is.True);
            bridge.Enable();
            var worker = new Worker();
            Assert.That(worker.TryExecute(new Parms()), Is.EqualTo(!refusalFault));
            Assert.Multiple(() =>
            {
                Assert.That(worker.Executions, Is.EqualTo(refusalFault ? 0 : 1));
                Assert.That(bridge.Available, Is.False);
                Assert.That(bridge.RefreshPublication(), Is.False, "A fault needs a fresh composition, not metadata-only reactivation.");
                Assert.That(worker.TryExecute(new Parms()), Is.True);
                Assert.That(reports, Is.EqualTo(1));
            });
        }
        finally
        {
            foreach (var patch in bridge.Group.Patches.Reverse()) patch.Rollback();
            foreign.UnpatchAll(foreign.Id);
        }
    }

    [Test]
    public void RepatchRevokesPublicationUntilExplicitMetadataRefresh()
    {
        var target = typeof(Worker).GetMethod(nameof(Worker.TryExecute))!;
        var foreign = new HarmonyLib.Harmony("test.incident.repatch");
        var bridge = new IncidentPatchBridge(typeof(Worker), typeof(Parms), typeof(SampleDef), _ => true);
        try
        {
            Assert.That(new AdmissionCoordinator(_ => { }).Apply(bridge.Group, AdmissionPhase.DefsReady), Is.True);
            Assert.That(bridge.RefreshPublication(), Is.True);
            bridge.Enable();
            foreign.Patch(target, prefix: new HarmonyMethod(typeof(IncidentPatchBridgeTests), nameof(Observer)));
            Assert.That(bridge.Available, Is.False, "Recompiled metadata must never inherit an earlier publication.");
            Assert.That(bridge.RefreshPublication(), Is.True, "A compatible observer can be admitted by an explicit owner refresh.");
            var worker = new Worker();
            Assert.That(worker.TryExecute(new Parms()), Is.False);
            Assert.That(worker.Executions, Is.Zero);
        }
        finally
        {
            foreach (var patch in bridge.Group.Patches.Reverse()) patch.Rollback();
            foreign.UnpatchAll(foreign.Id);
        }
    }

    [Test]
    public void AdmittedBridgeRefusesExcludedIdentityBeforeNativeEffectsAndPreservesAllowedRules()
    {
        var excluded = new SampleDef();
        int refused = 0;
        var bridge = new IncidentPatchBridge(typeof(Worker), typeof(Parms), typeof(SampleDef),
            identity => ReferenceEquals(identity, excluded), _ => refused++);
        try
        {
            var coordinator = new AdmissionCoordinator(_ => { });
            Assert.That(coordinator.Apply(bridge.Group, AdmissionPhase.DefsReady), Is.True);
            Assert.That(bridge.RefreshPublication(), Is.True);
            bridge.Enable();
            var blocked = new Worker { def = excluded };
            var allowed = new Worker();
            Assert.Multiple(() =>
            {
                Assert.That(blocked.CanFireNow(new Parms()), Is.False);
                Assert.That(blocked.TryExecute(new Parms()), Is.False);
                Assert.That(blocked.Executions, Is.Zero);
                Assert.That(refused, Is.EqualTo(1));
                Assert.That(allowed.CanFireNow(new Parms()), Is.True);
                Assert.That(allowed.TryExecute(new Parms()), Is.True);
                Assert.That(allowed.Executions, Is.EqualTo(1));
                allowed.NativeEligible = false;
                Assert.That(allowed.CanFireNow(new Parms()), Is.False);
            });
        }
        finally
        {
            foreach (var patch in bridge.Group.Patches.Reverse()) patch.Rollback();
        }
    }

    private sealed class SampleDef { }
    private sealed class Parms { }
    private sealed class Worker
    {
        public SampleDef def = new();
        public bool NativeEligible = true;
        public int Executions;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool CanFireNow(Parms parms) => NativeEligible;
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool TryExecute(Parms parms) { Executions++; return true; }
    }
}
