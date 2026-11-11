using Megamod.Features.Diagnostics;
using NUnit.Framework;

namespace Megamod.Unit.Features.Diagnostics;

[TestFixture]
public sealed class DefinitionContextTests
{
    [TestCase(false)]
    [TestCase(true)]
    public void RevokedOrFailedFrameValidityCannotExposeOrReviveTheParent(bool throws)
    {
        var scope = new DefinitionContextScope();
        var caller = new DefinitionContext("Caller", null, null, null, null, null);
        var parent = new DefinitionContext("Parent", null, null, null, null, null);
        var child = new DefinitionContext("Child", null, null, null, null, null);
        bool valid = true;
        using (scope.Enter(caller))
        {
            using (scope.Enter(parent, () => valid ? true : throws ? throw new InvalidOperationException("owned validity failure") : false))
            {
                Assert.That(scope.Current, Is.EqualTo(parent));
                valid = false;
                Assert.That(scope.Current, Is.Null);
                valid = true;
                using (scope.Enter(child, () => true)) Assert.That(scope.Current, Is.EqualTo(child));
                Assert.That(scope.Current, Is.Null);
            }
            Assert.That(scope.Current, Is.EqualTo(caller));
        }
        Assert.That(scope.Current, Is.Null);
    }
    [Test]
    public void InvalidatedParentStaysHiddenUntilItsOriginalTokenRestoresCaller()
    {
        var scope = new DefinitionContextScope();
        var caller = new DefinitionContext("Caller", null, null, null, null, null);
        var parent = new DefinitionContext("Parent", null, null, null, null, null);
        var later = new DefinitionContext("Later", null, null, null, null, null);
        using (scope.Enter(caller))
        {
            using (scope.Enter(parent))
            {
                scope.InvalidateCurrent();
                Assert.That(scope.Current, Is.Null);
                using (scope.Enter(later)) Assert.That(scope.Current, Is.EqualTo(later));
                Assert.That(scope.Current, Is.Null);
            }
            Assert.That(scope.Current, Is.EqualTo(caller));
        }
        Assert.That(scope.Current, Is.Null);
    }
    [Test]
    public void OutOfOrderCleanupCannotResurrectAnExitedDefinition()
    {
        var scope = new DefinitionContextScope();
        var outerContext = new DefinitionContext("Outer", "ThingDef", null, null, null, null);
        var innerContext = new DefinitionContext("Inner", "ThingDef", null, null, null, null);
        IDisposable outer = scope.Enter(outerContext);
        IDisposable inner = scope.Enter(innerContext);

        outer.Dispose();
        Assert.That(scope.Current, Is.Null);
        inner.Dispose();
        Assert.That(scope.Current, Is.Null);
        using (scope.Enter(innerContext)) Assert.That(scope.Current, Is.EqualTo(innerContext));
        Assert.That(scope.Current, Is.Null);
    }

    [Test]
    public void RepeatedCleanupAfterAnExceptionCannotRestoreStaleContext()
    {
        var scope = new DefinitionContextScope();
        var outer = new DefinitionContext("Outer", "ThingDef", null, null, null, null);
        var inner = new DefinitionContext("Inner", "ThingDef", null, null, null, null);
        var later = new DefinitionContext("Later", "ThingDef", null, null, null, null);
        var originalFailure = new InvalidOperationException("native conversion failure");
        using (scope.Enter(outer))
        {
            IDisposable interrupted = scope.Enter(inner);
            Assert.That(Assert.Throws<InvalidOperationException>(() =>
            {
                using (interrupted) throw originalFailure;
            }), Is.SameAs(originalFailure));
            Assert.That(scope.Current, Is.EqualTo(outer));

            using (scope.Enter(later))
            {
                interrupted.Dispose();
                Assert.That(scope.Current, Is.EqualTo(later));
            }
            Assert.That(scope.Current, Is.EqualTo(outer));
        }
        Assert.That(scope.Current, Is.Null);
    }

    [Test]
    public void ConcurrentConversionsCannotReadEachOthersContext()
    {
        var scope = new DefinitionContextScope();
        var outer = new DefinitionContext("Main", "ThingDef", null, null, null, null);
        var inner = new DefinitionContext("Worker", "ThingDef", null, null, null, null);
        using var entered = new ManualResetEventSlim();
        using var release = new ManualResetEventSlim();
        using (scope.Enter(outer))
        {
            Task<DefinitionContext?> worker = Task.Run(() =>
            {
                var before = scope.Current;
                using (scope.Enter(inner))
                {
                    entered.Set();
                    if (!release.Wait(TimeSpan.FromSeconds(5))) throw new TimeoutException("Fixture release timed out.");
                    Assert.That(scope.Current, Is.EqualTo(inner));
                }
                Assert.That(scope.Current, Is.Null);
                return before;
            });
            try
            {
                Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
                Assert.That(scope.Current, Is.EqualTo(outer));
            }
            finally
            {
                release.Set();
                Assert.That(worker.GetAwaiter().GetResult(), Is.Null);
            }
            Assert.That(scope.Current, Is.EqualTo(outer));
        }
        Assert.That(scope.Current, Is.Null);
    }

    [Test]
    public void NestedUnknownDefinitionReplacesTheSourceAndRestoresTheOuterContext()
    {
        var scope = new DefinitionContextScope();
        var outer = new DefinitionContext("Outer", "ThingDef", "Outer mod", "fixture.outer",
            @"C:\Mods\Outer", @"C:\Mods\Outer\Defs\Outer.xml");
        var inner = new DefinitionContext("Inner", "ThingDef", null, null, null, null);

        using (scope.Enter(outer))
        {
            Assert.That(scope.Current, Is.EqualTo(outer));
            using (scope.Enter(inner))
            {
                Assert.That(scope.Current, Is.EqualTo(inner));
                Assert.That(scope.Current!.SourceFilePath, Is.Null);
                Assert.That(scope.Current.PackageId, Is.Null);
            }
            Assert.That(scope.Current, Is.EqualTo(outer));
        }
        Assert.That(scope.Current, Is.Null);
    }
}
