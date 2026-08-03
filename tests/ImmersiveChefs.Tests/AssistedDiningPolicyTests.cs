using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class AssistedDiningPolicyTests
{
    [TestCase(false, false, WareRequirementMode.Prefer, false, true)]
    [TestCase(true, true, WareRequirementMode.Prefer, false, true)]
    [TestCase(true, false, WareRequirementMode.Prefer, false, false)]
    [TestCase(true, false, WareRequirementMode.Strict, false, false)]
    [TestCase(true, false, WareRequirementMode.Prefer, true, true)]
    [TestCase(true, false, WareRequirementMode.Off, false, true)]
    public void MissingCutleryMemoryBelongsOnlyToAnEligibleDiner(
        bool assisted,
        bool dinerConscious,
        WareRequirementMode cutleryRequirement,
        bool hasCutlery,
        bool expected)
    {
        var actual = AssistedDiningPolicy.ShouldRecordDiningMemory(
            assisted,
            dinerConscious,
            cutleryRequirement,
            hasCutlery);

        Assert.That(actual, Is.EqualTo(expected));
    }
}
