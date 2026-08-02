using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class AssistantContributionTests
{
    [Test]
    public void Claimed_assistant_does_not_contribute_until_station_work_toil_begins()
    {
        var gate = new AssistantWorkGate();

        Assert.That(gate.IsWorking, Is.False);
        gate.BeginWorking();
        Assert.That(gate.IsWorking, Is.True);
        gate.StopWorking();
        Assert.That(gate.IsWorking, Is.False);
    }

    [Test]
    public void ExpertAndHalfSkilledAssistants_AccrueOnlyDuringLeadWork()
    {
        var contribution = new AssistantContributionAccumulator();

        contribution.RecordLeadTick(new[] { 20, 10 }, 1f);
        contribution.RecordLeadTick(Array.Empty<int>(), 1f);

        Assert.That(contribution.CurrentSpeedBonus, Is.EqualTo(0f));
        Assert.That(contribution.TotalSpeedWork, Is.EqualTo(0.225f).Within(0.0001f));
        Assert.That(contribution.QualityScore, Is.EqualTo(18.75f).Within(0.0001f));
    }

    [Test]
    public void MalformedContributorCount_CannotExceedSpeedCap()
    {
        var contribution = new AssistantContributionAccumulator();
        contribution.RecordLeadTick(Enumerable.Repeat(20, 12), 3f);

        Assert.That(contribution.CurrentSpeedBonus, Is.EqualTo(1.8f));
        Assert.That(contribution.QualityScore, Is.EqualTo(100f));
    }

    [Test]
    public void CancelledCook_HasNoQualityContribution()
    {
        Assert.That(new AssistantContributionAccumulator().QualityScore, Is.Zero);
    }
}
