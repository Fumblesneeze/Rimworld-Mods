using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Megamod.Features.ContentSelection.Harmony;
using Megamod.Patching;
using NUnit.Framework;
using HarmonyInstance = HarmonyLib.Harmony;

namespace Megamod.HarmonyTests.ContentSelection;

[TestFixture, NonParallelizable]
public sealed class IncidentPatchOrderTests
{
    private const string ForeignOwner = "tests.megamod.incident-order";
    private static int factoryCalls;
    private static object? observedIdentity;

    [TestCase(true)]
    [TestCase(false)]
    public void LaterResultWriterIsRejectedWhileObserverIsAllowed(bool writesResult)
    {
        var target = Method(nameof(Target));
        var decision = Method(nameof(VetoResult));
        var owned = new OwnedHarmonyPatch(target, HarmonyPatchType.Postfix, decision);
        var foreign = new HarmonyInstance(ForeignOwner);
        try
        {
            owned.Install();
            foreign.Patch(target, postfix: new HarmonyMethod(Method(writesResult ? nameof(AllowResult) : nameof(Observer)))
            {
                after = new[] { OwnedHarmonyPatch.OwnerId }
            });
            Assert.That(IncidentPatchOrder.ValidatePostfix(target, decision) != null, Is.EqualTo(writesResult));
        }
        finally
        {
            foreign.Unpatch(target, HarmonyPatchType.Postfix, ForeignOwner);
            owned.Rollback();
        }
    }

    [Test]
    public void LaterReadOnlyIdentityObserverRemainsCompatible()
    {
        var target = typeof(Worker).GetMethod(nameof(Worker.Run))!;
        var decision = Method(nameof(Decision));
        var owned = new OwnedHarmonyPatch(target, HarmonyPatchType.Prefix, decision);
        var foreign = new HarmonyInstance(ForeignOwner);
        try
        {
            owned.Install();
            foreign.Patch(target, prefix: new HarmonyMethod(Method(nameof(ReadIdentity)))
            {
                after = new[] { OwnedHarmonyPatch.OwnerId }
            });
            Assert.That(IncidentPatchOrder.ValidatePrefix(target, decision), Is.Null);
            var worker = new Worker();
            Assert.That(worker.Run(), Is.True);
            Assert.That(observedIdentity, Is.SameAs(worker.def));
        }
        finally
        {
            foreign.Unpatch(target, HarmonyPatchType.Prefix, ForeignOwner);
            owned.Rollback();
            observedIdentity = null;
        }
    }

    [Test]
    public void LaterInstanceIdentityWriterIsRejected()
    {
        var target = typeof(Worker).GetMethod(nameof(Worker.Run))!;
        var decision = Method(nameof(Decision));
        var owned = new OwnedHarmonyPatch(target, HarmonyPatchType.Prefix, decision);
        var foreign = new HarmonyInstance(ForeignOwner);
        try
        {
            owned.Install();
            foreign.Patch(target, prefix: new HarmonyMethod(Method(nameof(ChangeIdentity)))
            {
                after = new[] { OwnedHarmonyPatch.OwnerId }
            });
            Assert.That(IncidentPatchOrder.ValidatePrefix(target, decision), Is.Not.Null);
        }
        finally
        {
            foreign.Unpatch(target, HarmonyPatchType.Prefix, ForeignOwner);
            owned.Rollback();
        }
    }

    [Test]
    public void FactoryIsRejectedWithoutInvokingItDuringInspection()
    {
        var target = Method(nameof(Target));
        var decision = Method(nameof(Decision));
        var owned = new OwnedHarmonyPatch(target, HarmonyPatchType.Prefix, decision);
        var foreign = new HarmonyInstance(ForeignOwner);
        try
        {
            factoryCalls = 0;
            owned.Install();
            foreign.Patch(target, prefix: new HarmonyMethod(Method(nameof(Factory))));
            int afterNativeInstallation = factoryCalls;
            Assert.That(afterNativeInstallation, Is.GreaterThan(0));
            Assert.Multiple(() =>
            {
                Assert.That(IncidentPatchOrder.ValidatePrefix(target, decision), Is.Not.Null);
                Assert.That(factoryCalls, Is.EqualTo(afterNativeInstallation));
            });
        }
        finally
        {
            foreign.Unpatch(target, HarmonyPatchType.Prefix, ForeignOwner);
            owned.Rollback();
        }
    }

    [Test]
    public void ForeignRegistrationOfSameCallbackDoesNotSupplyOwnedDecision()
    {
        var target = Method(nameof(Target));
        var decision = Method(nameof(Decision));
        var foreign = new HarmonyInstance(ForeignOwner);
        try
        {
            foreign.Patch(target, prefix: new HarmonyMethod(decision));
            Assert.That(IncidentPatchOrder.ValidatePrefix(target, decision), Is.Not.Null);
        }
        finally { foreign.Unpatch(target, HarmonyPatchType.Prefix, ForeignOwner); }
    }

    [Test]
    public void LaterUnrelatedObserverPrefixDoesNotInvalidateTheDecision()
    {
        var target = Method(nameof(Target));
        var decision = Method(nameof(Decision));
        var owned = new OwnedHarmonyPatch(target, HarmonyPatchType.Prefix, decision);
        var foreign = new HarmonyInstance(ForeignOwner);
        try
        {
            owned.Install();
            foreign.Patch(target, prefix: new HarmonyMethod(Method(nameof(Observer)))
            {
                after = new[] { OwnedHarmonyPatch.OwnerId }
            });
            Assert.That(IncidentPatchOrder.ValidatePrefix(target, decision), Is.Null);
        }
        finally
        {
            foreign.Unpatch(target, HarmonyPatchType.Prefix, ForeignOwner);
            owned.Rollback();
        }
    }

    private static MethodInfo Method(string name) => typeof(IncidentPatchOrderTests)
        .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static)!;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Target() => true;
    private static bool Decision() => true;
    private static void VetoResult(ref bool __result) => __result = false;
    private static void AllowResult(ref bool __result) => __result = true;
    private static void Observer() { }
    private static void ChangeIdentity(Worker __instance) => __instance.def = new object();
    private static void ReadIdentity(Worker __instance) => observedIdentity = __instance.def;
    private sealed class Worker
    {
        public object def = new();
        [MethodImpl(MethodImplOptions.NoInlining)]
        public bool Run() => true;
    }
    private static MethodInfo Factory(MethodBase original)
    {
        factoryCalls++;
        return Method(nameof(Observer));
    }
}
