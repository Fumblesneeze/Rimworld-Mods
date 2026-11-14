using Megamod.Features.DrugPolicyMaintenance.Core;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.DrugPolicyMaintenance;

[TestFixture]
public sealed class ScheduledDrugReadBoundaryTests
{
    [Test]
    public void AThrownCurrentReadIsUnavailableAndReportsTheActualFailure()
    {
        var error = new InvalidOperationException("current native reading failed");
        Exception? reported = null; int reads = 0;
        var result = ScheduledDrugReadBoundary.Evaluate(() => { reads++; throw error; }, value => reported = value);
        Assert.That(result, Is.EqualTo(ScheduledDrugReadResult.Unavailable));
        Assert.That(reported, Is.SameAs(error)); Assert.That(reads, Is.EqualTo(1));
    }

    [Test]
    public void FailedNoticeCannotEscapeAndTheNextCurrentReadingCanRecover()
    {
        int reports = 0;
        var failed = ScheduledDrugReadBoundary.Evaluate(() => throw new InvalidOperationException("read"),
            _ => { reports++; throw new ArgumentException("notice"); });
        Assert.That(failed, Is.EqualTo(ScheduledDrugReadResult.Unavailable));
        var next = ScheduledDrugReadBoundary.Evaluate(() => ScheduledDrugReadResult.Rejected,
            _ => reports++);
        Assert.That(next, Is.EqualTo(ScheduledDrugReadResult.Rejected)); Assert.That(reports, Is.EqualTo(1));
    }

    [TestCase(ScheduledDrugReadResult.Allowed)]
    [TestCase(ScheduledDrugReadResult.Rejected)]
    [TestCase(ScheduledDrugReadResult.Unavailable)]
    public void SuccessfulReadingRetainsItsActualClassification(ScheduledDrugReadResult expected)
    {
        int reads = 0, reports = 0;
        Assert.That(ScheduledDrugReadBoundary.Evaluate(() => { reads++; return expected; }, _ => reports++), Is.EqualTo(expected));
        Assert.That(reads, Is.EqualTo(1)); Assert.That(reports, Is.Zero);
    }
}
