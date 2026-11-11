using Megamod.Settings;
using NUnit.Framework;

namespace Megamod.Unit.Settings;

[TestFixture]
public sealed class SettingsLifecycleMailboxTests
{
    [Test]
    public void ReconciliationDoesNotOverwriteWorkerFinalizationPublishedDuringTheNativeRead()
    {
        var mailbox = new SettingsLifecycleMailbox<object>();
        var first = new object();
        var next = new object();
        mailbox.SetAvailable(true);
        mailbox.Observe(first);
        SettingsLifecycleRequest<object>? published = null;

        bool reconciled = mailbox.TryReconcileOwner(() =>
        {
            // The native getter already captured A; the worker publishes B before it returns A.
            Task.Run(() =>
            {
                mailbox.Observe(next);
                mailbox.NotifyFinalized(next, mailbox.Read().Generation, new("next"));
                published = mailbox.Read();
            }).Wait();
            return first;
        }, exception => throw exception, out _);
        var afterStaleRead = mailbox.Read();
        bool nextRead = mailbox.TryReconcileOwner(() => next, exception => throw exception, out var actual);

        Assert.Multiple(() =>
        {
            Assert.That(reconciled, Is.False);
            Assert.That(afterStaleRead, Is.SameAs(published));
            Assert.That(nextRead, Is.True);
            Assert.That(mailbox.TryGetReady(actual, true, out var ready), Is.True);
            Assert.That(ready, Is.SameAs(published));
            Assert.That(ready.Save?.SaveIdentity, Is.EqualTo("next"));
        });
    }

    [Test]
    public void UnavailableOrFailedGetterIsNotRetriedAndRevokesBeforeOneFailureNotification()
    {
        var mailbox = new SettingsLifecycleMailbox<object>();
        int reads = 0;
        int reports = 0;
        bool notificationSawRevocation = false;
        object? ThrowingGetter() { reads++; throw new InvalidOperationException("getter failed"); }
        Assert.That(mailbox.TryReadOwner(ThrowingGetter, _ => reports++, out _), Is.False);
        Assert.That(reads, Is.Zero);
        var game = new object();
        mailbox.SetAvailable(true);
        mailbox.Observe(game);
        mailbox.NotifyFinalized(game, mailbox.Read().Generation, new("failure"));
        mailbox.TryGetReady(game, true, out var ready);
        var binding = new SettingsStore().ActivateSave(ready.Save!);
        mailbox.TryBind(ready, binding);

        Assert.DoesNotThrow(() => mailbox.TryReadOwner(ThrowingGetter, _ =>
        {
            reports++;
            notificationSawRevocation = binding.IsRevoked;
            throw new Exception("diagnostic sink failed");
        }, out _));
        mailbox.TryReadOwner(ThrowingGetter, _ => reports++, out _);
        Assert.Multiple(() =>
        {
            Assert.That(reads, Is.EqualTo(1));
            Assert.That(reports, Is.EqualTo(1));
            Assert.That(notificationSawRevocation, Is.True);
            Assert.That(binding.IsRevoked, Is.True);
            Assert.That(mailbox.Read().Available, Is.False);
        });
    }

    [Test]
    public void WorkerIdentityChangeRevokesTheBoundTokenWithoutInvokingStoreChanges()
    {
        var mailbox = new SettingsLifecycleMailbox<object>();
        var owner = new object();
        mailbox.SetAvailable(true);
        mailbox.Observe(owner);
        mailbox.NotifyFinalized(owner, mailbox.Read().Generation, new("bound"));
        mailbox.TryGetReady(owner, true, out var ready);
        var store = new SettingsStore();
        var binding = store.ActivateSave(ready.Save!);
        int changes = 0;
        store.Changed += () => changes++;
        bool bound = mailbox.TryBind(ready, binding);

        Task.Run(() => mailbox.Observe(new object())).Wait();

        Assert.Multiple(() =>
        {
            Assert.That(bound, Is.True);
            Assert.That(binding.IsRevoked, Is.True);
            Assert.That(changes, Is.Zero);
            Assert.That(mailbox.TryBind(ready, binding), Is.False);
        });
    }

    [Test]
    public void WorkerFinalizationMustMatchTheLatestOwnerAndWaitForTheFinishedBoundary()
    {
        var mailbox = new SettingsLifecycleMailbox<object>();
        var first = new object();
        var current = new object();
        mailbox.SetAvailable(true);
        mailbox.Observe(first);
        long firstGeneration = mailbox.Read().Generation;
        mailbox.Observe(current);
        long currentGeneration = mailbox.Read().Generation;
        bool staleAccepted = Task.Run(() => mailbox.NotifyFinalized(first, firstGeneration, new("old"))).Result;
        bool accepted = Task.Run(() => mailbox.NotifyFinalized(current, currentGeneration, new("current"))).Result;

        Assert.Multiple(() =>
        {
            Assert.That(staleAccepted, Is.False);
            Assert.That(accepted, Is.True);
            Assert.That(mailbox.TryGetReady(current, false, out _), Is.False);
            Assert.That(mailbox.TryGetReady(first, true, out _), Is.False);
            Assert.That(mailbox.TryGetReady(current, true, out var ready), Is.True);
            Assert.That(ready.Save?.SaveIdentity, Is.EqualTo("current"));
        });
    }
}
