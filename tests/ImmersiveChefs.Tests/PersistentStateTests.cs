using NUnit.Framework;
using System.Reflection;
using Verse;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class PersistentStateTests
{
    [Test]
    public void Sanitation_round_trip_preserves_dirty_state_and_stack_identity()
    {
        var dirty = new SanitationStateModel();
        dirty.MarkDirty();

        var restored = SanitationStateModel.Restore(dirty.Capture());

        Assert.Multiple(() =>
        {
            Assert.That(restored.IsDirty, Is.True);
            Assert.That(restored.CanStackWith(new SanitationStateModel()), Is.False);
            Assert.That(restored.CanStackWith(SanitationStateModel.Restore(restored.Capture())), Is.True);
        });
    }

    [Test]
    public void Sanitation_round_trip_preserves_wild_water_provenance_and_prevents_lossy_stacking()
    {
        var wildWashed = new SanitationStateModel();
        wildWashed.MarkClean(WashProvenance.WildWater);
        wildWashed.MarkDirty();

        var restored = SanitationStateModel.Restore(wildWashed.Capture());
        var safelyWashed = new SanitationStateModel();
        safelyWashed.MarkClean(WashProvenance.Safe);
        safelyWashed.MarkDirty();

        Assert.Multiple(() =>
        {
            Assert.That(restored.IsDirty, Is.True);
            Assert.That(restored.WashProvenance, Is.EqualTo(WashProvenance.WildWater));
            Assert.That(restored.CanStackWith(safelyWashed), Is.False);
            Assert.That(restored.CanStackWith(SanitationStateModel.Restore(restored.Capture())), Is.True);
        });
    }

    [Test]
    public void Ordinary_sanitation_inspection_hides_wild_water_provenance()
    {
        Assert.Multiple(() =>
        {
            Assert.That(
                SanitationInspectionText.For(isDirty: false, selfCleaning: false),
                Is.EqualTo("Cleanliness: clean"));
            Assert.That(
                SanitationInspectionText.For(isDirty: true, selfCleaning: false),
                Is.EqualTo("Cleanliness: dirty"));
            Assert.That(
                SanitationInspectionText.For(isDirty: false, selfCleaning: true),
                Is.EqualTo("Cleanliness: self-cleaning"));
        });
    }

    [Test]
    public void Splitting_a_meal_stack_transfers_exact_plate_bindings_without_duplication()
    {
        var meals = new MealStackState(new[]
        {
            new PlateBinding("ImmersiveChefs_Plate", "Steel", 4, 91, false, WashProvenance.Safe),
            new PlateBinding("ImmersiveChefs_Plate", "Gold", 3, 73, true, WashProvenance.WildWater),
            new PlateBinding("ImmersiveChefs_PlateAdobe", null, 2, 48, false, WashProvenance.None)
        });

        var split = meals.SplitOff(2);

        Assert.Multiple(() =>
        {
            Assert.That(meals.PlateBindings, Has.Count.EqualTo(1));
            Assert.That(meals.PlateBindings[0].StuffDefName, Is.EqualTo("Steel"));
            Assert.That(split.PlateBindings.Select(binding => binding.StuffDefName),
                Is.EqualTo(new string?[] { "Gold", null }));
            Assert.That(split.PlateBindings[0].WashProvenance, Is.EqualTo(WashProvenance.WildWater));
            Assert.That(meals.PlateBindings.Count + split.PlateBindings.Count, Is.EqualTo(3));
        });
    }

    [Test]
    public void Prepared_food_round_trip_preserves_nutrition_quality_and_safety_provenance()
    {
        var prepared = new PreparedFoodState(
            new[]
            {
                new IngredientContribution("RawPotatoes", 0.55f, 2),
                new IngredientContribution("Meat_Human", 0.70f, 1)
            },
            preparationQuality: 87,
            preparerThingId: "Pawn_Chef_12",
            dietaryFlags: DietaryFlags.Plant | DietaryFlags.Animal | DietaryFlags.HumanMeat,
            exactSourcesHidden: false,
            ingredientPoisonChance: 0.03f);

        var restored = PreparedFoodState.Restore(prepared.Capture());

        Assert.Multiple(() =>
        {
            Assert.That(restored.TotalNutrition, Is.EqualTo(1.25f).Within(0.0001f));
            Assert.That(restored.PreparationQuality, Is.EqualTo(87));
            Assert.That(restored.PreparerThingId, Is.EqualTo("Pawn_Chef_12"));
            Assert.That(restored.DietaryFlags.HasFlag(DietaryFlags.HumanMeat), Is.True);
            Assert.That(restored.Contributions.Select(item => item.DefName),
                Is.EqualTo(new[] { "RawPotatoes", "Meat_Human" }));
            Assert.That(restored.IngredientPoisonChance, Is.EqualTo(0.03f));
        });
    }

    [Test]
    public void Culinary_serving_state_round_trip_preserves_temperature_contamination_and_reheats()
    {
        var serving = new CulinaryServingRecord(
            qualityScore: 72,
            temperatureCelsius: 4.5f,
            contamination: ContaminationSources.DirtyCookware | ContaminationSources.DirtyPlate,
            microwaveReheatCount: 2,
            lastThermalTick: 123456,
            hiddenSourceDefNames: new[] { "RawRice", "Meat_Human", "RawRice" },
            hiddenDietaryFlags: DietaryFlags.Plant | DietaryFlags.HumanMeat);

        var restored = CulinaryServingRecord.Restore(serving.Capture());

        Assert.Multiple(() =>
        {
            Assert.That(restored.QualityScore, Is.EqualTo(72));
            Assert.That(restored.TemperatureCelsius, Is.EqualTo(4.5f));
            Assert.That(restored.Contamination,
                Is.EqualTo(ContaminationSources.DirtyCookware | ContaminationSources.DirtyPlate));
            Assert.That(restored.MicrowaveReheatCount, Is.EqualTo(2));
            Assert.That(restored.LastThermalTick, Is.EqualTo(123456));
            Assert.That(restored.HiddenSourceDefNames, Is.EqualTo(new[] { "Meat_Human", "RawRice" }));
            Assert.That(restored.HiddenDietaryFlags,
                Is.EqualTo(DietaryFlags.Plant | DietaryFlags.HumanMeat));
        });
    }

    [Test]
    public void Full_stack_ingestion_defers_destroy_recovery_and_job_cleanup_until_callbacks_finish()
    {
        var lifecycle = new IngestionLifecycleState();

        Assert.Multiple(() =>
        {
            Assert.That(lifecycle.ShouldRecoverEmbeddedWareOnDestroy, Is.True);
            Assert.That(lifecycle.ShouldCancelDiningSessionOnJobCleanup, Is.True);
        });

        lifecycle.Begin();

        Assert.Multiple(() =>
        {
            Assert.That(lifecycle.ShouldRecoverEmbeddedWareOnDestroy, Is.False);
            Assert.That(lifecycle.ShouldCancelDiningSessionOnJobCleanup, Is.False);
        });

        lifecycle.End();

        Assert.Multiple(() =>
        {
            Assert.That(lifecycle.ShouldRecoverEmbeddedWareOnDestroy, Is.True);
            Assert.That(lifecycle.ShouldCancelDiningSessionOnJobCleanup, Is.True);
        });
    }

    [Test]
    public void Failed_ingestion_can_abort_every_nested_lifecycle_scope()
    {
        var lifecycle = new IngestionLifecycleState();
        lifecycle.Begin();
        lifecycle.Begin();

        lifecycle.Abort();

        Assert.Multiple(() =>
        {
            Assert.That(lifecycle.ShouldRecoverEmbeddedWareOnDestroy, Is.True);
            Assert.That(lifecycle.ShouldCancelDiningSessionOnJobCleanup, Is.True);
        });
    }

    [Test]
    public void Rimworld_identity_split_preserves_culinary_serving_state()
    {
        var meal = new ThingWithComps { stackCount = 1 };
        var culinary = new CompCulinaryState { parent = meal };
        typeof(ThingWithComps)
            .GetField("comps", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(meal, new List<ThingComp> { culinary });
        culinary.ReplaceServings(new[]
        {
            new CulinaryServingRecord(
                82,
                62f,
                ContaminationSources.None,
                0,
                100,
                new[] { "Meat_Human" },
                DietaryFlags.HumanMeat)
        });

        culinary.PostSplitOff(meal);

        Assert.Multiple(() =>
        {
            Assert.That(culinary.Servings, Has.Count.EqualTo(1));
            Assert.That(culinary.Servings[0].QualityScore, Is.EqualTo(82));
            Assert.That(culinary.Servings[0].TemperatureCelsius, Is.EqualTo(62f));
            Assert.That(culinary.Servings[0].HiddenSourceDefNames, Is.EqualTo(new[] { "Meat_Human" }));
            Assert.That(culinary.Servings[0].HiddenDietaryFlags, Is.EqualTo(DietaryFlags.HumanMeat));
        });
    }

}
