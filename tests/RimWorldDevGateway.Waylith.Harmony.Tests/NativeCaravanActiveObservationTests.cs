using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using NUnit.Framework;
using Verse;

namespace RimWorldDevGateway.Waylith.Harmony.Tests;

[TestFixture, NonParallelizable]
public sealed class NativeCaravanActiveObservationTests
{
    [TestCase("close-button")]
    [TestCase("late-postfix")]
    [TestCase("none")]
    public void ProductionDetailHooksWaitForContentsAndKeepSwallowedFailuresInvisible(string fault)
    {
        var providerType = typeof(VerseGatewayNativeWindowInput);
        var installed = providerType.GetField("installed", BindingFlags.Static | BindingFlags.NonPublic)!;
        Assert.That(installed.GetValue(null), Is.Null);
        Assert.That(LoadedModManager.RunningModsListForReading, Is.Empty);
        var provider = (VerseGatewayNativeWindowInput)RuntimeHelpers.GetUninitializedObject(providerType);
        var host = new DetailHost(fault);
        providerType.GetField("healthDetailObservation", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(provider, host.Observation);
        var harmony = new HarmonyLib.Harmony("gateway.caravan-detail." + Guid.NewGuid().ToString("N"));
        var contents = typeof(DetailHost).GetMethod(nameof(DetailHost.Contents))!;
        MethodInfo Hook(string name) => providerType.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;
        try
        {
            installed.SetValue(null, provider);
            harmony.Patch(contents, prefix: new HarmonyMethod(Hook("BeforeCaravanDetailContents")),
                postfix: new HarmonyMethod(typeof(NativeCaravanActiveObservationTests).GetMethod(nameof(DetailPostfix), BindingFlags.Static | BindingFlags.NonPublic)!),
                finalizer: new HarmonyMethod(Hook("FinalizeCaravanDetailContents")) { priority = int.MinValue });
            host.NativeWindowCatch();
            Assert.Multiple(() =>
            {
                Assert.That(host.Caught, Is.EqualTo(fault != "none"));
                Assert.That(host.PublishedBeforeContentsReturned, Is.False);
                Assert.That(host.Observation.Visible, Is.EqualTo(fault == "none"));
            });
        }
        finally { harmony.UnpatchAll(harmony.Id); installed.SetValue(null, null); }
        Assert.That(HarmonyLib.Harmony.GetPatchInfo(contents)?.Owners.Contains(harmony.Id) ?? false, Is.False);
    }
    private static void DetailPostfix(DetailHost __instance)
    { if (__instance.Fault == "late-postfix") throw new InvalidOperationException("late contents postfix"); }
    private sealed class DetailHost
    {
        internal readonly string Fault;
        internal readonly GatewayNativeCaravanDetailObservation Observation;
        internal bool Caught, PublishedBeforeContentsReturned;
        internal DetailHost(string fault) { Fault = fault; Observation = new(this, () => true); }
        internal void NativeWindowCatch() { try { Contents(); } catch (InvalidOperationException) { Caught = true; } }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Contents()
        {
            Observation.CardReturned();
            PublishedBeforeContentsReturned = Observation.Visible;
            if (Fault == "close-button") throw new InvalidOperationException("native CloseButtonFor after card");
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public void ABodyPostfixFailureCannotPublishThroughANativeStyleCatch(bool fail)
    {
        Assert.That(LoadedModManager.RunningModsListForReading, Is.Empty);
        Assert.That(DefDatabase<ThingDef>.AllDefsListForReading, Is.Empty);
        var harmony = new HarmonyLib.Harmony("gateway.caravan-observation." + Guid.NewGuid().ToString("N"));
        var body = typeof(Host).GetMethod(nameof(Host.Body))!;
        var type = typeof(NativeCaravanActiveObservationTests);
        try
        {
            harmony.Patch(body, postfix: new HarmonyMethod(type.GetMethod(nameof(Late), BindingFlags.Static | BindingFlags.NonPublic)!),
                finalizer: new HarmonyMethod(type.GetMethod(nameof(FinalizeBody), BindingFlags.Static | BindingFlags.NonPublic)!));
            var host = new Host(fail);
            Assert.That(host.Layout(), Is.EqualTo(!fail));
            Assert.That(host.Caught, Is.EqualTo(fail));
        }
        finally { harmony.UnpatchAll(harmony.Id); }
        Assert.That(HarmonyLib.Harmony.GetPatchInfo(body)?.Owners.Contains(harmony.Id) ?? false, Is.False);
    }
    private static void Late(Host __instance) { if (__instance.Fail) throw new InvalidOperationException("late native body failure"); }
    private static Exception? FinalizeBody(Host __instance, Exception? __exception)
    { if (__exception != null) __instance.Proof.Fail(); return __exception; }
    private sealed class Host
    {
        internal readonly bool Fail;
        internal bool Caught;
        internal readonly GatewayNativeCaravanLayoutProof Proof;
        internal Host(bool fail) { Fail = fail; Proof = new(this, this, this, false, 2, 2); }
        internal bool Layout()
        {
            Proof.BeginPending();
            try { Body(); } catch (InvalidOperationException) { Caught = true; }
            Proof.ClearPending(); Proof.EndPending(true);
            return Proof.Complete(this, this, this, false, 2, 2, true, true) && Proof.BodyCompleted;
        }
        [MethodImpl(MethodImplOptions.NoInlining)]
        public void Body() => Proof.BodyReturned(true);
    }
}
