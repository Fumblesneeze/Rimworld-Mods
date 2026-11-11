using System.Reflection;
using Megamod.Features.DrugTolerance.Core;
using NUnit.Framework;

namespace Megamod.Unit.Features.DrugTolerance;

[TestFixture]
public sealed class DrugToleranceJoyProviderTests
{
    [Test]
    public void InheritedNativeDecisionRemainsEligible()
    {
        var native = Resolve(typeof(NativeIngest));
        var inherited = Resolve(typeof(DrugJoy));
        Assert.That(DrugToleranceJoyProvider.UsesNativeDecision(inherited, native), Is.True,
            "Looking up the inherited native method through a worker must not disable tolerance controls.");
    }

    [TestCase(typeof(OverrideJoy))]
    [TestCase(typeof(HiddenJoy))]
    public void ASeparateWorkerDecisionRemainsUnavailable(Type worker) =>
        Assert.That(DrugToleranceJoyProvider.UsesNativeDecision(Resolve(worker), Resolve(typeof(NativeIngest))), Is.False);

    [Test]
    public void MissingDecisionRemainsUnavailable() =>
        Assert.That(DrugToleranceJoyProvider.UsesNativeDecision(null, Resolve(typeof(NativeIngest))), Is.False);

    [Test]
    public void AClosedGenericDeclarationCannotBorrowAnotherReceiversAdmission() =>
        Assert.That(DrugToleranceJoyProvider.UsesNativeDecision(Resolve(typeof(GenericIngest<int>)),
            Resolve(typeof(GenericIngest<string>))), Is.False);

    private static MethodInfo Resolve(Type type) => type.GetMethod("CanIngest",
        BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(object), typeof(object) }, null)!;
    private class NativeIngest { protected virtual bool CanIngest(object pawn, object thing) => true; }
    private sealed class DrugJoy : NativeIngest { }
    private sealed class OverrideJoy : NativeIngest { protected override bool CanIngest(object pawn, object thing) => base.CanIngest(pawn, thing); }
    private class HiddenJoy : NativeIngest { protected new bool CanIngest(object pawn, object thing) => true; }
    private sealed class GenericIngest<T> { private bool CanIngest(object pawn, object thing) => true; }
}
