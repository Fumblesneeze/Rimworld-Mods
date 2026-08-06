using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class CaravanDiningPolicyTests
{
    [Test]
    public void Animals_never_enter_tableware_temperature_quality_or_thought_consequences()
    {
        Assert.That(DiningPawnPolicy.AppliesDiningConsequences(humanlike: false), Is.False);
    }

    [Test]
    public void Animals_never_receive_plate_eating_speed()
    {
        Assert.That(DiningPawnPolicy.AppliesPlateEatingSpeed(humanlike: false), Is.False);
    }

    [Test]
    public void Caravan_selection_prefers_best_clean_ware_before_dirty_ware()
    {
        var dirtyExcellent = new ServiceWareCandidate<string>("dirty", isDirty: true, serviceScore: 100f);
        var cleanPoor = new ServiceWareCandidate<string>("clean-poor", isDirty: false, serviceScore: 20f);
        var cleanGood = new ServiceWareCandidate<string>("clean-good", isDirty: false, serviceScore: 70f);

        var selected = CaravanDiningPolicy.SelectWare(
            new[] { dirtyExcellent, cleanPoor, cleanGood },
            WareRequirementMode.Prefer,
            DirtyWareFallback.Always,
            isEmergency: false);

        Assert.That(selected, Is.EqualTo("clean-good"));
    }

    [Test]
    public void Caravan_selection_uses_dirty_fallback_only_when_policy_allows_it()
    {
        var candidates = new[]
        {
            new ServiceWareCandidate<string>("dirty", isDirty: true, serviceScore: 80f)
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                CaravanDiningPolicy.SelectWare(
                    candidates,
                    WareRequirementMode.Prefer,
                    DirtyWareFallback.Always,
                    isEmergency: false),
                Is.EqualTo("dirty"));
            Assert.That(
                CaravanDiningPolicy.SelectWare(
                    candidates,
                    WareRequirementMode.Prefer,
                    DirtyWareFallback.Never,
                    isEmergency: false),
                Is.Null);
        });
    }

    [Test]
    public void Caravan_selection_does_not_take_ware_when_requirements_are_disabled()
    {
        var candidates = new[]
        {
            new ServiceWareCandidate<string>("clean", isDirty: false, serviceScore: 80f)
        };

        var selected = CaravanDiningPolicy.SelectWare(
            candidates,
            WareRequirementMode.Off,
            DirtyWareFallback.Always,
            isEmergency: false);

        Assert.That(selected, Is.Null);
    }

    [TestCase(0f, false)]
    [TestCase(-0.01f, false)]
    [TestCase(0.01f, true)]
    public void Caravan_dining_commits_only_when_rimworld_reports_positive_ingestion(
        float nutritionIngested,
        bool expected)
    {
        Assert.That(CaravanDiningPolicy.WasIngested(nutritionIngested), Is.EqualTo(expected));
    }

    [TestCase("Pemmican")]
    [TestCase("MealSurvivalPack")]
    [TestCase("BabyFood")]
    [TestCase("MealPrinter_NutriBar")]
    public void Travel_food_exclusions_are_not_covered(string defName)
    {
        Assert.That(MealCoveragePolicy.IsBuiltInExcluded(defName), Is.True);
    }
}
