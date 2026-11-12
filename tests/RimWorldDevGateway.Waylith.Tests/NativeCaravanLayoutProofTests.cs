using NUnit.Framework;

namespace RimWorldDevGateway.Waylith.Tests;

[TestFixture]
public sealed class NativeCaravanLayoutProofTests
{
    [Test]
    public void UnknownClipGeometryStillInvalidatesTheEnclosingFrame()
    {
        object input = new(), dispatcher = new(), host = new();
        var proof = new GatewayNativeCaravanLayoutProof(input, dispatcher, host, false, 2, 2);
        proof.BeginPending();
        Assert.That(proof.ObserveRow(GatewayNativeCaravanGeometry.Classify(5, 5, 15, 15, double.NaN, 0, 100, 100)), Is.False);
        proof.BodyReturned(true); proof.EndPending(true);
        Assert.That(proof.Complete(input, dispatcher, host, false, 2, 2, true, true), Is.False);
    }

    [TestCase(95)]
    [TestCase(110)]
    public void AVisibleRowSurvivesAKnownClippedNeighbour(double neighbourTop)
    {
        object input = new(), dispatcher = new(), host = new();
        var proof = new GatewayNativeCaravanLayoutProof(input, dispatcher, host, false, 2, 2);
        proof.BeginPending();
        Assert.That(proof.ObserveRow(GatewayNativeCaravanGeometry.Classify(5, 5, 15, 15, 0, 0, 100, 100)), Is.True);
        Assert.That(proof.ObserveRow(GatewayNativeCaravanGeometry.Classify(5, neighbourTop, 15, neighbourTop + 10, 0, 0, 100, 100)), Is.False);
        proof.BodyReturned(true); proof.EndPending(true);
        Assert.That(proof.Complete(input, dispatcher, host, false, 2, 2, true, true), Is.True);
    }

    [TestCase("late-body-failure")]
    [TestCase("swallowed-failure")]
    [TestCase("pending-failure")]
    [TestCase("duplicate-pending")]
    [TestCase("event")]
    [TestCase("foreign-finalizer")]
    [TestCase("original-skipped")]
    public void IncompleteOrDisownedLayoutsNeverPublishObservedRows(string fault)
    {
        object input = new(), dispatcher = new(), host = new();
        var proof = new GatewayNativeCaravanLayoutProof(input, dispatcher, host, false, 2, 2);
        proof.BeginPending(); proof.BodyReturned(true);
        if (fault == "late-body-failure") proof.Fail();
        if (fault == "duplicate-pending") proof.BeginPending();
        proof.ClearPending(); proof.EndPending(fault != "pending-failure");
        if (fault == "swallowed-failure") proof.ClearPending();
        Assert.That(proof.Complete(fault == "event" ? new() : input, dispatcher, host, false, 2, 2,
            fault != "original-skipped", fault != "foreign-finalizer"), Is.False);
    }

    [Test]
    public void NativeBodyAndPendingProcessingMustBothReturnBeforePublication()
    {
        object input = new(), dispatcher = new(), host = new();
        var proof = new GatewayNativeCaravanLayoutProof(input, dispatcher, host, false, 2, 2);
        proof.BeginPending(); proof.BodyReturned(true); proof.ClearPending(); proof.EndPending(true);
        Assert.That(proof.Complete(input, dispatcher, host, false, 2, 2, true, true), Is.True);
        Assert.That(proof.BodyCompleted, Is.True);
    }
}
