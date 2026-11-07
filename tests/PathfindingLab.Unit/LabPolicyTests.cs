using NUnit.Framework;
using PathfindingLab.Core;

namespace PathfindingLab.Unit;

public class LabPolicyTests
{
    [TestCase(25, 25, 13)]
    [TestCase(50, 50, 26)]
    [TestCase(100, 100, 51)]
    [TestCase(200, 200, 101)]
    [TestCase(0, 50, 26)]
    [TestCase(1000, 50, 26)]
    public void Refinement_horizon_is_bounded_and_renews_halfway(int requested, int cells, int nodes)
    {
        Assert.That(LabPolicy.RefinementCells(requested), Is.EqualTo(cells));
        Assert.That(LabPolicy.RenewalNodes(requested), Is.EqualTo(nodes));
    }
    [Test]
    public void OversizingRequiresExplicitSupportedChoiceAndPlayerSettlement()
    {
        foreach (int size in new[] { 500, 750, 1000 })
        {
            Assert.That(LabPolicy.SettlementSize(size, true, false), Is.EqualTo(size));
            Assert.That(LabPolicy.SettlementSize(size, false, false), Is.Zero);
            Assert.That(LabPolicy.SettlementSize(size, true, true), Is.Zero);
        }
        foreach (int invalid in new[] { -1, 0, 250, 999, 1001, int.MaxValue })
            Assert.That(LabPolicy.SettlementSize(invalid, true, false), Is.Zero);
    }
}
