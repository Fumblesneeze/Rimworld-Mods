using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class CookingAdmissionLifecyclePolicyTests
{
    [Test]
    public void Speculative_candidates_never_own_ware_but_started_jobs_do()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                CookingAdmissionLifecyclePolicy.MayReserveWare(
                    CookingAdmissionPhase.CandidateEvaluation,
                    driverJobIsCurrent: false),
                Is.False,
                "Work scans may inspect availability but must not reserve physical ware.");
            Assert.That(
                CookingAdmissionLifecyclePolicy.MayReserveWare(
                    CookingAdmissionPhase.AcceptedDriverStart,
                    driverJobIsCurrent: true),
                Is.True,
                "The accepted driver must commit its ware before any cooking toil runs.");
            Assert.That(
                CookingAdmissionLifecyclePolicy.MayReserveWare(
                    CookingAdmissionPhase.AcceptedDriverStart,
                    driverJobIsCurrent: false),
                Is.False,
                "A cached or queued driver's pre-reservation pass must not touch the pawn's old current job.");
        });
    }
}
