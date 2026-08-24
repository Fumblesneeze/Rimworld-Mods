using System;
using GuestBedGizmo.Beds;
using NUnit.Framework;

namespace GuestBedGizmo.Tests;

[TestFixture]
public sealed class PatchInstallTransactionTests
{
    [Test]
    public void FailedPatchApplicationRollsBackBeforeReportingIncompatibility()
    {
        bool rolledBack = false;

        bool installed = PatchInstallTransaction.TryApply(
            () => throw new InvalidOperationException("drifted vanilla seam"),
            () => rolledBack = true,
            out string? failure);

        Assert.Multiple(() =>
        {
            Assert.That(installed, Is.False);
            Assert.That(rolledBack, Is.True);
            Assert.That(failure, Does.Contain("drifted vanilla seam"));
        });
    }
}
