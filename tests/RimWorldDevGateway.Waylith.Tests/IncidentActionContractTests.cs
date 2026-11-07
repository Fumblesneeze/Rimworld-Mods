using System;
using NUnit.Framework;
using RimWorldDevGateway.EndToEndTesting;

namespace RimWorldDevGateway.Waylith.Tests;

public sealed class IncidentActionContractTests
{
    [TestCase("RaidEnemy", " ", "ImmediateAttack")]
    [TestCase("RaidEnemy", "EdgeWalkIn", "")]
    [TestCase("TraderCaravanArrival", "EdgeWalkIn", null)]
    [TestCase("TraderCaravanArrival", null, "ImmediateAttack")]
    public void Invalid_raid_choices_fail_before_dispatch(string incident, string? arrival, string? strategy) =>
        Assert.Throws<ArgumentException>(() => new IncidentActionStep("invalid", incident, null, arrival, strategy));

    [Test]
    public void Exact_raid_choices_are_preserved_without_removing_the_original_constructor()
    {
        var step = new IncidentActionStep("edge raid", "RaidEnemy", -17, "EdgeWalkIn", "ImmediateAttack");
        Assert.That(step.RaidArrivalModeDefName, Is.EqualTo("EdgeWalkIn"));
        Assert.That(step.RaidStrategyDefName, Is.EqualTo("ImmediateAttack"));
        Assert.That(step.FactionLoadId, Is.EqualTo(-17));
        Assert.That(typeof(IncidentActionStep).GetConstructor(new[] { typeof(string), typeof(string), typeof(int?) }), Is.Not.Null);
        var original = new IncidentActionStep("trader", "TraderCaravanArrival");
        Assert.That(original.RaidArrivalModeDefName, Is.Null);
        Assert.That(original.RaidStrategyDefName, Is.Null);
    }
}
