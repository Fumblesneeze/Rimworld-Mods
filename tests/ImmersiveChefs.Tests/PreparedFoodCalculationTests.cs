using NUnit.Framework;
using RimWorld;
using System.Reflection;
using Verse;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class PreparedFoodCalculationTests
{
    [Test]
    public void Animal_products_are_not_misclassified_as_plant_food()
    {
        var flags = DietaryClassification.ForFoodType(
            FoodTypeFlags.AnimalProduct,
            "EggChickenUnfertilized");

        Assert.Multiple(() =>
        {
            Assert.That(flags.HasFlag(DietaryFlags.Animal), Is.True);
            Assert.That(flags.HasFlag(DietaryFlags.Plant), Is.False);
            Assert.That(flags.HasFlag(DietaryFlags.VegetarianCompatible), Is.False);
        });
    }

    [Test]
    public void Prepared_stacks_keep_preparer_and_source_count_provenance_distinct()
    {
        var left = new CompPreparedFood();
        left.Initialize(new PreparedFoodState(
            new[] { new IngredientContribution("RawRice", 0.5f, 1) },
            60,
            "Pawn_A",
            DietaryFlags.Plant,
            false,
            0f));
        var differentPreparer = new CompPreparedFood();
        differentPreparer.Initialize(new PreparedFoodState(
            new[] { new IngredientContribution("RawRice", 0.5f, 1) },
            60,
            "Pawn_B",
            DietaryFlags.Plant,
            false,
            0f));
        var differentCount = new CompPreparedFood();
        differentCount.Initialize(new PreparedFoodState(
            new[] { new IngredientContribution("RawRice", 0.5f, 2) },
            60,
            "Pawn_A",
            DietaryFlags.Plant,
            false,
            0f));

        Assert.Multiple(() =>
        {
            Assert.That(left.CompatibleWith(differentPreparer), Is.False);
            Assert.That(left.CompatibleWith(differentCount), Is.False);
        });
    }

    [Test]
    public void Prepared_source_policy_rejects_one_disallowed_or_unresolved_source()
    {
        var permitted = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "RawRice",
            "RawPotatoes"
        };

        Assert.Multiple(() =>
        {
            Assert.That(
                PreparedFoodDietaryPolicy.AllSourcesAllowed(
                    new[] { "RawRice", "RawPotatoes" },
                    permitted.Contains),
                Is.True);
            Assert.That(
                PreparedFoodDietaryPolicy.AllSourcesAllowed(
                    new[] { "RawRice", "Meat_Human" },
                    permitted.Contains),
                Is.False);
            Assert.That(
                PreparedFoodDietaryPolicy.AllSourcesAllowed(
                    new[] { "RawRice", "Missing_Ingredient" },
                    source => source != "Missing_Ingredient" && permitted.Contains(source)),
                Is.False);
        });
    }

    [Test]
    public void Paste_contributions_retain_distinct_hidden_sources_without_duplicating_nutrition()
    {
        var contributions = PreparedFoodDietaryPolicy.CreatePasteContributions(
            new[] { "RawRice", "Meat_Human", "RawRice" },
            totalNutrition: 0.05f);

        Assert.Multiple(() =>
        {
            Assert.That(
                contributions.Select(value => value.DefName),
                Is.EqualTo(new[] { "Meat_Human", "RawRice" }));
            Assert.That(
                contributions.Sum(value => value.Nutrition),
                Is.EqualTo(0.05f).Within(0.0001f));
            Assert.That(contributions.All(value => value.SourceCount == 1), Is.True);
        });
    }

    [Test]
    public void Paste_contributions_fall_back_to_the_vanilla_paste_meal_def()
    {
        var contribution = PreparedFoodDietaryPolicy.CreatePasteContributions(
            Array.Empty<string>(),
            totalNutrition: 0.05f).Single();

        Assert.That(contribution.DefName, Is.EqualTo("MealNutrientPaste"));
    }

    [Test]
    public void Paste_display_provenance_uses_one_vanilla_proxy_while_ordinary_prep_stays_exact()
    {
        var sourceDefNames = new[] { "RawRice", "Meat_Human" };

        Assert.Multiple(() =>
        {
            Assert.That(
                PreparedFoodDietaryPolicy.VisibleSourceDefNames(
                    sourceDefNames,
                    exactSourcesHidden: true),
                Is.EqualTo(new[] { "MealNutrientPaste" }));
            Assert.That(
                PreparedFoodDietaryPolicy.VisibleSourceDefNames(
                    sourceDefNames,
                    exactSourcesHidden: false),
                Is.EqualTo(new[] { "Meat_Human", "RawRice" }));
        });
    }

    [Test]
    public void Dietary_runtime_reads_current_hidden_meal_sources_without_exposing_an_ingredient_comp()
    {
        var meal = new ThingWithComps { stackCount = 1 };
        var culinary = new CompCulinaryState { parent = meal };
        typeof(ThingWithComps)
            .GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(meal, new List<ThingComp> { culinary });
        culinary.ReplaceServings(new[]
        {
            new CulinaryServingRecord(
                50,
                40f,
                ContaminationSources.None,
                0,
                0,
                new[] { "RawRice", "Meat_Human", "RawRice" },
                DietaryFlags.Plant | DietaryFlags.HumanMeat)
        });

        Assert.Multiple(() =>
        {
            Assert.That(
                PreparedFoodDietaryRuntime.SourceDefNamesFor(meal),
                Is.EqualTo(new[] { "Meat_Human", "RawRice" }));
            Assert.That(meal.GetComp<CompIngredients>(), Is.Null);
        });
    }

    [Test]
    public void Urgent_production_window_is_explicit_bounded_and_consumable()
    {
        var request = new UrgentProductionWindow(durationTicks: 600);

        request.Register(currentTick: 1000);

        Assert.That(request.IsActive(1599), Is.True);
        Assert.That(request.IsActive(1600), Is.False);
        request.Register(currentTick: 2000);
        request.Consume();
        Assert.That(request.IsActive(2001), Is.False);
    }

    [TestCase(0f, 0.4f, 1f)]
    [TestCase(0.5f, 0.4f, 0.8f)]
    [TestCase(1f, 0.4f, 0.6f)]
    [TestCase(1f, 0.75f, 0.25f)]
    public void WorkFactor_IsProportionalAndBounded(float preparedFraction, float maximumReduction, float expected)
    {
        Assert.That(PreparedFoodCalculator.WorkFactor(preparedFraction, maximumReduction), Is.EqualTo(expected).Within(0.0001f));
    }

    [TestCase(-5, 0)]
    [TestCase(0, 0)]
    [TestCase(10, 50)]
    [TestCase(20, 100)]
    [TestCase(50, 100)]
    public void SkillQuality_MapsCookingSkillToZeroThroughOneHundred(int skill, int expected)
    {
        Assert.That(PreparedFoodCalculator.SkillQuality(skill), Is.EqualTo(expected));
    }
}
