using Megamod.Features.MessagePlacement;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.MessagePlacement;

[TestFixture]
public sealed class MessageGuiEventTests
{
    [TestCase("consumer")]
    [TestCase("outer-exit")]
    [TestCase("exception")]
    public void AuthorityFailureDuringPreparedCommitCannotWriteOrRetainPendingSurfaces(string failure)
    {
        var pass = new MessageGuiPass();
        var timing = new MessageGuiEvent(pass, _ => {});
        object root = new();
        int writes = 0;
        var envelope = timing.Enter(root);
        var collection = timing.BeginMessages(root)!;
        pass.Observe(root, new(new(19, 37, 100, 25), _ => writes++, () => {}));
        Assert.That(timing.CompleteMessages(collection, true), Is.True);
        Assert.That(timing.CommitPrepared(collection, 1000, 800, 200, default, () =>
        {
            if (failure == "consumer") timing.NotifyConsumption();
            if (failure == "outer-exit") timing.Exit(envelope);
            if (failure == "exception") throw new InvalidOperationException("Current context failed");
            return true;
        }), Is.False);
        timing.Exit(envelope);
        Assert.That(writes, Is.Zero);
        var next = timing.Enter(root);
        try
        {
            var nextCollection = timing.BeginMessages(root)!;
            pass.Observe(root, new(new(19, 37, 100, 25), _ => writes++, () => {}));
            Assert.That(timing.CompleteMessages(nextCollection, true), Is.True);
            Assert.That(timing.CommitPrepared(nextCollection, 1000, 800, 200, default), Is.True);
            Assert.That(writes, Is.EqualTo(1));
        }
        finally { timing.Exit(next); }
    }

    [TestCase("consumer")]
    [TestCase("repeated-collection")]
    [TestCase("outer-exit")]
    [TestCase("collection-failure")]
    public void InterruptedPendingPlacementNeverMovesOrLeaksIntoTheNextInvocation(string interruption)
    {
        var pass = new MessageGuiPass();
        var timing = new MessageGuiEvent(pass, _ => {});
        object root = new();
        int writes = 0;
        var envelope = timing.Enter(root);
        var collection = timing.BeginMessages(root)!;
        pass.Observe(root, new(new(19, 37, 100, 25), _ => writes++, () => {}));
        bool succeeded = interruption != "collection-failure";
        Assert.That(timing.CompleteMessages(collection, succeeded), Is.EqualTo(succeeded));
        if (interruption == "consumer") timing.NotifyConsumption();
        if (interruption == "repeated-collection") Assert.That(timing.BeginMessages(root), Is.Null);
        if (interruption == "outer-exit") timing.Exit(envelope);
        Assert.That(timing.CommitPrepared(collection, 1000, 800, 200, default), Is.False);
        timing.Exit(envelope);
        Assert.That(writes, Is.Zero);
        var next = timing.Enter(root);
        try
        {
            var nextCollection = timing.BeginMessages(root)!;
            pass.Observe(root, new(new(19, 37, 100, 25), _ => writes++, () => {}));
            Assert.That(timing.CompleteMessages(nextCollection, true), Is.True);
            Assert.That(timing.CommitPrepared(nextCollection, 1000, 800, 200, default), Is.True);
            Assert.That(writes, Is.EqualTo(1));
        }
        finally { timing.Exit(next); }
    }

    [Test]
    public void PreparedBarrierUsesInterveningLayoutValuesForOneWholeStackCommit()
    {
        var pass = new MessageGuiPass();
        var timing = new MessageGuiEvent(pass, _ => {});
        object root = new();
        var envelope = timing.Enter(root);
        var moved = new List<MessageBounds>();
        double[] currentLayout = { 200 };
        try
        {
            var collection = timing.BeginMessages(root)!;
            Assert.That(timing.CommitPrepared(collection, 1000, 800, currentLayout[0], default), Is.False);
            pass.Observe(new object(), new(new(19, 37, 100, 25), moved.Add, () => {}));
            pass.Observe(new object(), new(new(19, 63, 100, 25), moved.Add, () => {}));
            Assert.That(timing.CompleteMessages(collection, true), Is.True);
            Assert.That(moved, Is.Empty);
            currentLayout[0] = 400; // Intervening GUI work; the same input object now describes current geometry.
            Assert.That(timing.CommitPrepared(collection, 1000, 800, currentLayout[0], default), Is.True);
            Assert.That(moved, Is.EqualTo(new[] { new MessageBounds(19, 404, 100, 25), new MessageBounds(19, 430, 100, 25) }));
            Assert.That(timing.CommitPrepared(collection, 1000, 800, 500, default), Is.False);
            timing.NotifyConsumption();
            Assert.That(moved, Has.Count.EqualTo(2));
        }
        finally { timing.Exit(envelope); }
    }

    [Test]
    public void ObservationAfterCollectionCannotChangeThePendingStack()
    {
        var pass = new MessageGuiPass();
        var timing = new MessageGuiEvent(pass, _ => {});
        object root = new();
        var envelope = timing.Enter(root);
        int writes = 0;
        try
        {
            var collection = timing.BeginMessages(root)!;
            pass.Observe(root, new(new(19, 37, 100, 25), _ => writes++, () => {}));
            Assert.That(timing.CompleteMessages(collection, true), Is.True);
            Assert.That(pass.Observe(new object(), new(new(19, 63, 100, 25), _ => writes++, () => {})), Is.False);
            Assert.That(timing.CommitPrepared(collection, 1000, 800, 200, default), Is.False);
            Assert.That(writes, Is.Zero);
        }
        finally { timing.Exit(envelope); }
    }

    [Test]
    public void CollectionCompletionLeavesQueuedSurfacesUnmoved()
    {
        var pass = new MessageGuiPass();
        var timing = new MessageGuiEvent(pass, _ => {});
        object root = new();
        var envelope = timing.Enter(root);
        var moved = new List<MessageBounds>();
        try
        {
            var collection = timing.BeginMessages(root)!;
            pass.Observe(root, new(new(19, 37, 100, 25), moved.Add, () => {}));
            Assert.That(timing.CompleteMessages(collection, true), Is.True);
            Assert.That(moved, Is.Empty, "Collection completion must wait for the actual prepared-state barrier.");
        }
        finally { timing.Exit(envelope); }
    }

    [Test]
    public void ExitingAnUnfinishedInvocationClearsItsPassAndStaleTokensCannotAffectTheNextOne()
    {
        var pass = new MessageGuiPass();
        var timing = new MessageGuiEvent(pass, _ => {});
        object root = new();
        int writes = 0;
        var first = timing.Enter(root);
        var unfinished = timing.BeginMessages(root)!;
        pass.Observe(root, new(new(10, 10, 50, 25), _ => writes++, () => {}));
        timing.Exit(first);
        var next = timing.Enter(root);
        var current = timing.BeginMessages(root)!;
        timing.Exit(first);
        Assert.That(FinishAndCommit(timing, unfinished, true, 1000, 800, null, default), Is.False);
        Assert.That(pass.Observe(root, new(new(10, 10, 50, 25), _ => writes++, () => {})), Is.True);
        Assert.That(FinishAndCommit(timing, current, true, 1000, 800, null, default), Is.True);
        timing.Exit(next);
        Assert.That(writes, Is.EqualTo(1));
    }

    [Test]
    public void RepeatedCollectionPreservesFirstCommitAndDeclinesFurtherMovement()
    {
        var pass = new MessageGuiPass();
        var timing = new MessageGuiEvent(pass, _ => {});
        object root = new();
        int writes = 0;
        var envelope = timing.Enter(root);
        var first = timing.BeginMessages(root)!;
        pass.Observe(root, new(new(10, 10, 50, 25), _ => writes++, () => writes--));
        Assert.That(FinishAndCommit(timing, first, true, 1000, 800, null, default), Is.True);
        Assert.That(timing.BeginMessages(root), Is.Null);
        Assert.That(FinishAndCommit(timing, first, true, 1000, 800, null, default), Is.False);
        timing.Exit(envelope);
        Assert.That(writes, Is.EqualTo(1));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void NestedInvocationRejectsFurtherMovementWithoutUndoingCompletedEffects(bool completedFirst)
    {
        var pass = new MessageGuiPass();
        var timing = new MessageGuiEvent(pass, _ => {});
        object root = new();
        int writes = 0;
        var outer = timing.Enter(root);
        var collection = timing.BeginMessages(root)!;
        pass.Observe(root, new(new(10, 10, 50, 25), _ => writes++, () => writes--));
        if (completedFirst) Assert.That(FinishAndCommit(timing, collection, true, 1000, 800, null, default), Is.True);
        var inner = timing.Enter(root);
        Assert.That(timing.BeginMessages(root), Is.Null);
        timing.Exit(inner);
        if (!completedFirst) Assert.That(FinishAndCommit(timing, collection, true, 1000, 800, null, default), Is.False);
        Assert.That(timing.BeginMessages(root), Is.Null);
        timing.Exit(outer);
        Assert.That(writes, Is.EqualTo(completedFirst ? 1 : 0));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void MissingOrDifferentRootCannotAuthorizeCollectionAndReportsOnlyOnce(bool hasEnvelope)
    {
        var pass = new MessageGuiPass();
        int reports = 0;
        var timing = new MessageGuiEvent(pass, _ => { reports++; throw new Exception("diagnostic"); });
        var envelope = hasEnvelope ? timing.Enter(new object()) : null;
        Assert.That(timing.BeginMessages(new object()), Is.Null);
        Assert.That(timing.BeginMessages(new object()), Is.Null);
        Assert.That(reports, Is.EqualTo(1));
        if (envelope != null) timing.Exit(envelope);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void EarlyConsumptionVetoesTheEnvelopeAndDoesNotLeakToTheNextInvocation(bool beforeCollection)
    {
        var pass = new MessageGuiPass();
        var warnings = new List<string>();
        var timing = new MessageGuiEvent(pass, warnings.Add);
        object root = new();
        int writes = 0;
        var envelope = timing.Enter(root);
        if (beforeCollection) timing.NotifyConsumption();
        var collection = timing.BeginMessages(root);
        if (beforeCollection) Assert.That(collection, Is.Null);
        else
        {
            Assert.That(pass.Observe(root, new(new(10, 10, 50, 25), _ => writes++, () => {})), Is.True);
            timing.NotifyConsumption();
            Assert.That(FinishAndCommit(timing, collection!, true, 1000, 800, null, default), Is.False);
        }
        timing.NotifyConsumption(); timing.NotifyConsumption();
        timing.Exit(envelope);
        Assert.That(writes, Is.Zero);
        Assert.That(warnings, Has.Count.EqualTo(1));
        var next = timing.Enter(root);
        var later = timing.BeginMessages(root);
        Assert.That(pass.Observe(root, new(new(10, 10, 50, 25), _ => writes++, () => {})), Is.True);
        Assert.That(FinishAndCommit(timing, later!, true, 1000, 800, null, default), Is.True);
        timing.Exit(next);
        Assert.That(writes, Is.EqualTo(1));
    }

    [Test]
    public void OneRootInvocationAllowsOneSuccessfulNativeCollectionBeforeLaterConsumption()
    {
        var pass = new MessageGuiPass();
        var warnings = new List<string>();
        var timing = new MessageGuiEvent(pass, warnings.Add);
        object root = new();
        var envelope = timing.Enter(root);
        int writes = 0;
        try
        {
            var collection = timing.BeginMessages(root);
            Assert.That(collection, Is.Not.Null);
            Assert.That(pass.Observe(new object(), new(new(19, 37, 100, 25), _ => writes++, () => {})), Is.True);
            Assert.That(FinishAndCommit(timing, collection!, true, 1000, 800, null, new(10, 0)), Is.True);
            timing.NotifyConsumption();
            Assert.That(writes, Is.EqualTo(1));
            Assert.That(warnings, Is.Empty);
        }
        finally { timing.Exit(envelope); }
    }
    private static bool FinishAndCommit(MessageGuiEvent timing, MessageGuiEvent.Collection collection,
        bool succeeded, double width, double height, double? barBottom, MessageOffsets offsets) =>
        timing.CompleteMessages(collection, succeeded) && timing.CommitPrepared(collection, width, height, barBottom, offsets);
}
