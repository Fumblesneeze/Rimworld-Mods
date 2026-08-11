using NUnit.Framework;
using RimWorldDevGateway.Performance;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class PerformanceTickBoundaryPatchTests
{
    [TearDown]
    public void TearDown() => PerformanceTickBoundaryGate.Disarm();

    [Test]
    public void Failed_install_cleans_an_exact_prefix_that_was_applied_before_the_failure()
    {
        var installed = false;
        var cleanupCalls = 0;

        var accepted = PerformanceTickBoundaryPatch.TryAcquireForTests(
            install: () =>
            {
                installed = true;
                throw new InvalidOperationException("failure after Patch");
            },
            uninstall: () =>
            {
                cleanupCalls++;
                installed = false;
            },
            ownsExactPatch: () => installed,
            out var lease,
            out var reason);

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.False);
            Assert.That(lease, Is.Null);
            Assert.That(installed, Is.False);
            Assert.That(cleanupCalls, Is.EqualTo(1));
            Assert.That(reason, Does.Contain("failure after Patch"));
        });
    }

    [Test]
    public void Missing_catalog_confirmation_still_unpatches_after_Patch_returned()
    {
        var uninstallCalls = 0;

        var accepted = PerformanceTickBoundaryPatch.TryAcquireForTests(
            install: () => { },
            uninstall: () => uninstallCalls++,
            ownsExactPatch: () => false,
            out var lease,
            out var reason);

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.False);
            Assert.That(lease, Is.Null);
            Assert.That(uninstallCalls, Is.EqualTo(1));
            Assert.That(reason, Does.Contain("did not expose one exact"));
        });
    }

    [Test]
    public void Failed_unpatch_keeps_the_lease_retryable_until_exact_ownership_is_gone()
    {
        var installed = false;
        var uninstallAttempts = 0;
        Assert.That(PerformanceTickBoundaryPatch.TryAcquireForTests(
            install: () => installed = true,
            uninstall: () =>
            {
                uninstallAttempts++;
                if (uninstallAttempts == 1) throw new InvalidOperationException("transient unpatch failure");
                installed = false;
            },
            ownsExactPatch: () => installed,
            out var lease,
            out var reason), Is.True, reason);

        var first = Assert.Throws<InvalidOperationException>(() => lease!.Dispose());
        Assert.Multiple(() =>
        {
            Assert.That(first!.Message, Does.Contain("transient unpatch failure"));
            Assert.That(installed, Is.True);
        });

        Assert.DoesNotThrow(() => lease!.Dispose());
        Assert.Multiple(() =>
        {
            Assert.That(installed, Is.False);
            Assert.That(uninstallAttempts, Is.EqualTo(2));
        });
    }

    [Test]
    public void Backend_owned_resource_is_released_only_after_dispose_succeeds()
    {
        var resource = new ThrowOnceDisposable();
        ThrowOnceDisposable? owned = resource;

        Assert.Throws<InvalidOperationException>(() =>
            GatewayCircinusPerformanceBackend.DisposeOwned(ref owned));
        Assert.That(owned, Is.SameAs(resource));

        Assert.DoesNotThrow(() => GatewayCircinusPerformanceBackend.DisposeOwned(ref owned));
        Assert.Multiple(() =>
        {
            Assert.That(owned, Is.Null);
            Assert.That(resource.Attempts, Is.EqualTo(2));
        });
    }

    private sealed class ThrowOnceDisposable : IDisposable
    {
        public int Attempts { get; private set; }

        public void Dispose()
        {
            Attempts++;
            if (Attempts == 1) throw new InvalidOperationException("transient disposal failure");
        }
    }
}
