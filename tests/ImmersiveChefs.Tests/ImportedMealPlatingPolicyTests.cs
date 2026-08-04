using NUnit.Framework;
using System.Reflection;
using Verse;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class ImportedMealPlatingPolicyTests
{
    [Test]
    public void Ordinary_unplated_meal_is_queued_and_withheld_until_plated()
    {
        var strict = ImportedMealPlatingPolicy.Evaluate(
            WareRequirementMode.Strict,
            coveredMeal: true,
            servingCount: 4,
            embeddedPlateCount: 0,
            platingOpportunityFailed: false,
            emergency: false,
            canPlateNow: true);
        var prefer = ImportedMealPlatingPolicy.Evaluate(
            WareRequirementMode.Prefer,
            coveredMeal: true,
            servingCount: 4,
            embeddedPlateCount: 0,
            platingOpportunityFailed: false,
            emergency: false,
            canPlateNow: true);

        Assert.Multiple(() =>
        {
            Assert.That(strict.ShouldQueuePlating, Is.True);
            Assert.That(strict.AllowDining, Is.False);
            Assert.That(strict.PlatesRequired, Is.EqualTo(4));
            Assert.That(prefer.ShouldQueuePlating, Is.True);
            Assert.That(prefer.AllowDining, Is.False);
        });
    }

    [Test]
    public void Failed_prefer_opportunity_releases_meal_but_strict_remains_blocked()
    {
        var firstPreferScan = ImportedMealPlatingPolicy.Evaluate(
            WareRequirementMode.Prefer,
            coveredMeal: true,
            servingCount: 2,
            embeddedPlateCount: 0,
            platingOpportunityFailed: false,
            emergency: false,
            canPlateNow: false);
        var releasedPrefer = ImportedMealPlatingPolicy.Evaluate(
            WareRequirementMode.Prefer,
            coveredMeal: true,
            servingCount: 2,
            embeddedPlateCount: 0,
            platingOpportunityFailed: true,
            emergency: false,
            canPlateNow: false);
        var failedStrict = ImportedMealPlatingPolicy.Evaluate(
            WareRequirementMode.Strict,
            coveredMeal: true,
            servingCount: 2,
            embeddedPlateCount: 0,
            platingOpportunityFailed: true,
            emergency: false,
            canPlateNow: false);

        Assert.Multiple(() =>
        {
            Assert.That(firstPreferScan.RecordFailedOpportunity, Is.True);
            Assert.That(firstPreferScan.AllowDining, Is.False);
            Assert.That(releasedPrefer.RecordFailedOpportunity, Is.False);
            Assert.That(releasedPrefer.AllowDining, Is.True);
            Assert.That(failedStrict.AllowDining, Is.False);
        });
    }

    [Test]
    public void Emergency_off_excluded_and_fully_plated_meals_do_not_wait()
    {
        var emergency = ImportedMealPlatingPolicy.Evaluate(
            WareRequirementMode.Strict,
            coveredMeal: true,
            servingCount: 1,
            embeddedPlateCount: 0,
            platingOpportunityFailed: false,
            emergency: true,
            canPlateNow: false);
        var off = ImportedMealPlatingPolicy.Evaluate(
            WareRequirementMode.Off,
            coveredMeal: true,
            servingCount: 1,
            embeddedPlateCount: 0,
            platingOpportunityFailed: false,
            emergency: false,
            canPlateNow: true);
        var excluded = ImportedMealPlatingPolicy.Evaluate(
            WareRequirementMode.Strict,
            coveredMeal: false,
            servingCount: 1,
            embeddedPlateCount: 0,
            platingOpportunityFailed: false,
            emergency: false,
            canPlateNow: true);
        var plated = ImportedMealPlatingPolicy.Evaluate(
            WareRequirementMode.Strict,
            coveredMeal: true,
            servingCount: 3,
            embeddedPlateCount: 3,
            platingOpportunityFailed: false,
            emergency: false,
            canPlateNow: true);

        Assert.Multiple(() =>
        {
            Assert.That(emergency.AllowDining, Is.True);
            Assert.That(emergency.RecordFailedOpportunity, Is.False);
            Assert.That(off.AllowDining, Is.True);
            Assert.That(off.WareExempt, Is.True);
            Assert.That(off.ShouldQueuePlating, Is.False);
            Assert.That(excluded.AllowDining, Is.True);
            Assert.That(excluded.ShouldQueuePlating, Is.False);
            Assert.That(plated.AllowDining, Is.True);
            Assert.That(plated.ShouldQueuePlating, Is.False);
            Assert.That(plated.PlatesRequired, Is.Zero);
        });
    }

    [Test]
    public void Failed_opportunity_survives_stack_splits_and_prevents_lossy_merges()
    {
        var original = MealWithEmbeddedWare(4, out var originalComp);
        var split = MealWithEmbeddedWare(2, out var splitComp);
        var fresh = MealWithEmbeddedWare(2, out _);

        originalComp.RecordFailedPlatingOpportunity();
        originalComp.PostSplitOff(split);

        Assert.Multiple(() =>
        {
            Assert.That(originalComp.PlatingOpportunityFailed, Is.True);
            Assert.That(splitComp.PlatingOpportunityFailed, Is.True);
            Assert.That(splitComp.AllowStackWith(original), Is.True);
            Assert.That(splitComp.AllowStackWith(fresh), Is.False);
        });
    }

    [Test]
    public void Otherwise_equal_plated_meal_receives_only_a_tie_breaking_bonus()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                ImportedMealPlatingPolicy.FoodOptimalityBonus(
                    coveredMeal: true,
                    servingCount: 2,
                    embeddedPlateCount: 2),
                Is.GreaterThan(0f).And.LessThan(1f));
            Assert.That(
                ImportedMealPlatingPolicy.FoodOptimalityBonus(
                    coveredMeal: true,
                    servingCount: 2,
                    embeddedPlateCount: 1),
                Is.Zero);
            Assert.That(
                ImportedMealPlatingPolicy.FoodOptimalityBonus(
                    coveredMeal: false,
                    servingCount: 1,
                    embeddedPlateCount: 1),
                Is.Zero);
        });
    }

    private static ThingWithComps MealWithEmbeddedWare(int stackCount, out CompEmbeddedWare embedded)
    {
        var meal = new ThingWithComps { stackCount = stackCount };
        embedded = new CompEmbeddedWare { parent = meal };
        typeof(ThingWithComps)
            .GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(meal, new List<ThingComp> { embedded });
        return meal;
    }
}
