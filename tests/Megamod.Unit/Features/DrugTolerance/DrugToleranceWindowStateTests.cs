using Megamod.Features.DrugTolerance.Core;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.DrugTolerance;

[TestFixture]
public sealed class DrugToleranceWindowStateTests
{
    [Test]
    public void AnotherSupportedRowCannotClearTheFailedRowsAllocation()
    {
        var state = new DrugToleranceWindowState();
        var policy = new object(); var failed = new object(); var other = new object();
        state.AssessRow(policy, failed, false);
        state.AssessRow(policy, other, true);
        Assert.That(state.HasFailure((p, r) => ReferenceEquals(p, policy) && (ReferenceEquals(r, failed) || ReferenceEquals(r, other))), Is.True);
    }
    [Test]
    public void OnlyTheSameCurrentAllocationRecoversAndRemovedRowsArePruned()
    {
        var state = new DrugToleranceWindowState();
        var policy = new object(); var row = new object(); var otherPolicy = new object(); var second = new object();
        state.AssessRow(policy, row, false);
        state.AssessRow(policy, second, false);
        state.AssessRow(otherPolicy, row, true);
        state.AssessRow(policy, second, true);
        Assert.That(state.HasFailure((_, _) => true), Is.True, "another policy and row cannot recover this allocation");
        state.AssessRow(policy, row, true);
        Assert.That(state.HasFailure((_, _) => true), Is.False);
        state.AssessRow(policy, row, false);
        Assert.That(state.HasFailure((p, r) => !ReferenceEquals(r, row)), Is.False, "the removed or replaced row no longer owns an allocation");
    }
    [Test]
    public void AHeaderRecoveryDoesNotClearRowFailuresAndAClosedWindowHasNoCarryOver()
    {
        var state = new DrugToleranceWindowState();
        state.AssessHeader(false); state.AssessRow(new object(), new object(), false); state.AssessHeader(true);
        Assert.That(state.HasFailure((_, _) => true), Is.True);
        Assert.That(new DrugToleranceWindowState().HasFailure((_, _) => true), Is.False);
    }
    [Test]
    public void DerivedManagerCannotUseProofForBaseVirtualGeometry()
    {
        var native = new NativeManager(); var overridden = new OverrideManager();
        Assert.That(native.Width, Is.EqualTo(935));
        Assert.That(overridden.Width, Is.EqualTo(5));
        Assert.That(DrugToleranceWindowState.Supports(native, typeof(NativeManager)), Is.True);
        Assert.That(DrugToleranceWindowState.Supports(overridden, typeof(NativeManager)), Is.False);
    }
    private class NativeManager { public virtual int Width => 935; }
    private sealed class OverrideManager : NativeManager { public override int Width => 5; }
    [Test]
    public void ContextExplanationIsQuietWhenAvailableAndBoundedToEachWindowAndReason()
    {
        var first = new DrugToleranceWindowState();
        Assert.That(first.TakeNotice(null), Is.False);
        Assert.That(first.TakeNotice("Off"), Is.True);
        Assert.That(first.TakeNotice("Off"), Is.False);
        Assert.That(first.TakeNotice("LayoutUnavailable"), Is.True);
        Assert.That(first.TakeNotice(null), Is.False);
        Assert.That(first.TakeNotice("LayoutUnavailable"), Is.False);
        Assert.That(new DrugToleranceWindowState().TakeNotice("Off"), Is.True);
    }
}
