using Megamod.Features.DrugPolicyMaintenance.Core;
using Megamod.Features.PracticalNeeds.Core;
using NUnit.Framework;

namespace Megamod.Unit.Tests.Features.PracticalNeeds;

[TestFixture]
public sealed class PotentialMoodRuleTests
{
    [Test]
    public void RecoveringMoodAtThePolicyThresholdWithholdsTheScheduledDose() =>
        Assert.That(PotentialMoodRule.Evaluate(true, true, .5f, .4f, .5f),
            Is.EqualTo(ScheduledDrugReadResult.Rejected));

    [TestCase(.5f, .4f, .49f, ScheduledDrugReadResult.Allowed)]
    [TestCase(.9f, .4f, .7f, ScheduledDrugReadResult.Allowed)]
    [TestCase(.5f, .5f, .4f, ScheduledDrugReadResult.Rejected)]
    [TestCase(.5f, .4f, .50001f, ScheduledDrugReadResult.Rejected)]
    [TestCase(0f, 0f, 0f, ScheduledDrugReadResult.Rejected)]
    public void StrictComparisonUsesBothUnroundedValues(float threshold, float current, float potential, ScheduledDrugReadResult result) =>
        Assert.That(PotentialMoodRule.Evaluate(true, true, threshold, current, potential), Is.EqualTo(result));

    [TestCase(false, true, .5f)]
    [TestCase(true, false, .5f)]
    [TestCase(true, true, 1f)]
    public void InapplicableMoodDoesNotRequireAReading(bool enabled, bool hasMood, float threshold) =>
        Assert.That(PotentialMoodRule.Evaluate(enabled, hasMood, threshold, float.NaN, float.PositiveInfinity),
            Is.EqualTo(ScheduledDrugReadResult.Allowed));

    [TestCase(float.NaN, .4f, .6f)]
    [TestCase(float.PositiveInfinity, .4f, .6f)]
    [TestCase(-.01f, .4f, .6f)]
    [TestCase(1.01f, .4f, .6f)]
    [TestCase(.5f, float.NaN, .6f)]
    [TestCase(.5f, -.01f, .6f)]
    [TestCase(.5f, 1.01f, .6f)]
    [TestCase(.5f, .4f, float.NegativeInfinity)]
    [TestCase(.5f, .4f, -.01f)]
    [TestCase(.5f, .4f, 1.01f)]
    public void InvalidRelevantFactsAreUnavailableRatherThanZero(float threshold, float current, float potential) =>
        Assert.That(PotentialMoodRule.Evaluate(true, true, threshold, current, potential),
            Is.EqualTo(ScheduledDrugReadResult.Unavailable));
}
