using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class DiningDirtPolicyTests
{
    [TestCase(true, true, WareRequirementMode.Prefer, true, true, false, true)]
    [TestCase(true, true, WareRequirementMode.Strict, true, true, false, true)]
    [TestCase(false, true, WareRequirementMode.Prefer, true, true, false, false)]
    [TestCase(true, false, WareRequirementMode.Prefer, true, true, false, false)]
    [TestCase(true, true, WareRequirementMode.Off, true, true, false, false)]
    [TestCase(true, true, WareRequirementMode.Prefer, false, true, false, false)]
    [TestCase(true, true, WareRequirementMode.Prefer, true, false, false, false)]
    [TestCase(true, true, WareRequirementMode.Prefer, true, true, true, false)]
    public void CreationRequiresACompletedEligibleMapMealWithoutCutlery(
        bool coveredMeal,
        bool humanlikeDiner,
        WareRequirementMode requirementMode,
        bool ingestionCompleted,
        bool mapAvailable,
        bool hasCutlery,
        bool expected)
    {
        var actual = DiningDirtPolicy.ShouldCreate(
            coveredMeal,
            humanlikeDiner,
            requirementMode,
            ingestionCompleted,
            mapAvailable,
            hasCutlery);

        Assert.That(actual, Is.EqualTo(expected));
    }
}
