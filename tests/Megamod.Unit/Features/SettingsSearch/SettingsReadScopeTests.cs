using Megamod.Features.SettingsSearch.Core;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.SettingsSearch;

[TestFixture]
public sealed class SettingsReadScopeTests
{
    [Test]
    public void ScopeIsVisibleUntilDisposedAndBeginsWithoutFailure()
    {
        Assert.That(SettingsReadScope.Current, Is.Null);
        using (var scope = SettingsReadScope.Begin())
        {
            Assert.That(SettingsReadScope.Current, Is.SameAs(scope));
            Assert.That(scope.Failed, Is.False);
            Assert.That(scope.FailureReason, Is.Null);
        }
        Assert.That(SettingsReadScope.Current, Is.Null);
    }

    [Test]
    public void AProviderCatchingRefusalCannotEraseTheFirstFailure()
    {
        using var scope = SettingsReadScope.Begin();
        var failure = Assert.Throws<InvalidOperationException>(() => throw scope.Refuse("window-write"));
        Assert.That(failure!.Message, Is.EqualTo("window-write"));
        Assert.That(scope.Failed, Is.True);
        Assert.That(scope.FailureReason, Is.EqualTo("window-write"));

        var later = scope.Refuse("settings-write");
        Assert.That(later.Message, Is.EqualTo("settings-write"));
        Assert.That(scope.FailureReason, Is.EqualTo("window-write"));
        Assert.That(SettingsReadScope.Current, Is.SameAs(scope));
    }

    [TestCase(false)]
    [TestCase(true)]
    public void RejectedNestingKeepsTheParentAndItsFirstFailure(bool alreadyFailed)
    {
        using var parent = SettingsReadScope.Begin();
        if (alreadyFailed) parent.Refuse("first-action");

        var nested = Assert.Throws<InvalidOperationException>(() => SettingsReadScope.Begin());

        Assert.That(SettingsReadScope.Current, Is.SameAs(parent));
        Assert.That(parent.Failed, Is.True);
        Assert.That(parent.FailureReason, Is.EqualTo(alreadyFailed ? "first-action" : "nested-settings-read"));
        Assert.That(nested!.Message, Is.EqualTo("nested-settings-read"));
    }

    [Test]
    public void DisposedHandleCannotClearOrFailASubsequentRead()
    {
        var previous = SettingsReadScope.Begin();
        previous.Dispose();
        using var next = SettingsReadScope.Begin();

        previous.Dispose();

        Assert.Multiple(() =>
        {
            Assert.That(SettingsReadScope.Current, Is.SameAs(next));
            Assert.Throws<ObjectDisposedException>(() => previous.Refuse("late-action"));
            Assert.That(previous.Failed, Is.False);
            Assert.That(next.Failed, Is.False);
        });
    }

    [Test]
    public void AnotherThreadHasItsOwnScopeAndCannotMutateOrDisposeTheOwnerScope()
    {
        using var owner = SettingsReadScope.Begin();
        Exception? refusal = null;
        Exception? cleanup = null;
        Exception? workerFailure = null;
        SettingsReadScope? initial = owner;
        SettingsReadScope? workerScope = null;
        SettingsReadScope? workerCurrentAfter = null;
        SettingsReadScope? workerCurrentAfterCleanup = owner;
        var worker = new Thread(() =>
        {
            try
            {
                initial = SettingsReadScope.Current;
                using (workerScope = SettingsReadScope.Begin())
                {
                    try { owner.Refuse("foreign-thread-action"); }
                    catch (Exception error) { refusal = error; }
                    try { owner.Dispose(); }
                    catch (Exception error) { cleanup = error; }
                    workerCurrentAfter = SettingsReadScope.Current;
                }
                workerCurrentAfterCleanup = SettingsReadScope.Current;
            }
            catch (Exception error) { workerFailure = error; }
        }) { IsBackground = true };
        worker.Start();
        Assert.That(worker.Join(TimeSpan.FromSeconds(5)), Is.True, "Worker did not finish.");

        Assert.Multiple(() =>
        {
            Assert.That(workerFailure, Is.Null);
            Assert.That(initial, Is.Null);
            Assert.That(refusal, Is.TypeOf<InvalidOperationException>());
            Assert.That(cleanup, Is.TypeOf<InvalidOperationException>());
            Assert.That(workerCurrentAfter, Is.SameAs(workerScope));
            Assert.That(workerCurrentAfterCleanup, Is.Null);
            Assert.That(SettingsReadScope.Current, Is.SameAs(owner));
            Assert.That(owner.Failed, Is.False);
        });
        owner.Dispose();
        Assert.That(SettingsReadScope.Current, Is.Null, "The owner must retain its cleanup authority.");
    }
}
