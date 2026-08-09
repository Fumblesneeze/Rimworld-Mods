using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.IntegrationTesting;
using Verse;

namespace ImmersiveChefs.MealTexture.InGame.IntegrationTests;

internal static class MealTextureLifecycleAssertions
{
    internal static void AssertHonestFallbackAndExactPlateLifecycle(
        string mealDefName,
        string expectedGraphicType)
    {
        ThingWithComps? meal = null;
        ThingWithComps? plate = null;
        Exception? primaryFailure = null;
        var phase = "create meal";
        try
        {
            var mealDef = DefDatabase<ThingDef>.GetNamed(mealDefName);
            meal = (ThingWithComps)ThingMaker.MakeThing(mealDef);
            meal.stackCount = 1;

            phase = "resolve state components";
            var embedded = meal.GetComp<CompEmbeddedWare>();
            var ingredients = meal.GetComp<CompIngredients>();
            var culinary = meal.GetComp<CompCulinaryState>();
            IntegrationAssert.NotNull(embedded,
                $"{mealDefName} must retain physical-plate state in this exact group.");
            IntegrationAssert.NotNull(ingredients,
                $"{mealDefName} must retain public ingredient provenance in this exact group.");
            IntegrationAssert.NotNull(culinary,
                $"{mealDefName} must retain hidden culinary provenance in this exact group.");

            IntegrationAssert.Equal(0, embedded!.EmbeddedPlateCount,
                $"A mod/debug/imported {mealDefName} must begin honestly unplated.");
            IntegrationAssert.True(embedded.ReleasePlateThing() is null,
                $"An unplated {mealDefName} must not fabricate a plate when one is requested.");

            phase = "seed public and hidden provenance";
            var paste = DefDatabase<ThingDef>.GetNamed("MealNutrientPaste");
            ingredients!.ingredients.Clear();
            ingredients.RegisterIngredient(paste);
            culinary!.ReplaceServings(new[]
            {
                new CulinaryServingRecord(
                    42,
                    21f,
                    ContaminationSources.EmergencyUnplated,
                    0,
                    Math.Max(0, Find.TickManager?.TicksGame ?? 0),
                    new[] { "RawRice" },
                    DietaryFlags.Plant | DietaryFlags.VegetarianCompatible)
            });

            IntegrationAssert.Equal(
                "MealNutrientPaste",
                string.Join("|", ingredients.ingredients.Select(def => def.defName)),
                $"Hidden prepared-paste sources on {mealDefName} must expose only the public paste provenance.");
            IntegrationAssert.Equal(
                "RawRice",
                string.Join("|", culinary.Servings.Single().HiddenSourceDefNames),
                $"{mealDefName} must retain the exact hidden source independently of public texture provenance.");
            phase = "resolve graphic owner";
            var graphic = meal.Graphic;
            IntegrationAssert.Equal(
                expectedGraphicType,
                graphic.GetType().FullName,
                $"Resolving hidden-paste graphics must leave the upstream owner authoritative for {mealDefName}.");
            var materialSelector = graphic.GetType().GetMethod(
                "MatSingleFor",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(Thing) },
                null);
            IntegrationAssert.NotNull(materialSelector,
                $"The upstream graphic owner must expose the standard material-selection seam for {mealDefName}.");
            phase = "invoke ingredient-driven material selection";
            IntegrationAssert.NotNull(
                materialSelector!.Invoke(graphic, new object[] { meal }),
                $"The upstream owner must resolve one real ingredient-driven material for {mealDefName}.");
            IntegrationAssert.Equal(
                "MealNutrientPaste",
                string.Join("|", ingredients.ingredients.Select(def => def.defName)),
                $"The upstream graphic owner must not reveal or rewrite hidden sources for {mealDefName}.");
            IntegrationAssert.Equal(
                "RawRice",
                string.Join("|", culinary.Servings.Single().HiddenSourceDefNames),
                $"The upstream graphic owner must not reveal or rewrite hidden culinary sources for {mealDefName}.");

            phase = "bind and release exact plate";
            plate = (ThingWithComps)ThingMaker.MakeThing(
                DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
                ThingDefOf.Steel);
            plate.GetComp<CompQuality>()?.SetQuality(
                QualityCategory.Normal,
                ArtGenerationContext.Colony);
            plate.GetComp<CompSanitation>()?.MarkClean(WashProvenance.Safe);
            var plateId = plate.ThingID;
            IntegrationAssert.True(embedded.TryEmbedPlate(plate),
                $"{mealDefName} must accept one exact physical plate.");
            IntegrationAssert.Equal(1, embedded.EmbeddedPlateCount,
                $"{mealDefName} must contain exactly one plate after binding.");
            IntegrationAssert.True(ReferenceEquals(plate, embedded.PeekPlateThing()),
                $"{mealDefName} must retain the same physical plate instance.");

            var released = embedded.ReleasePlateThing();
            IntegrationAssert.True(ReferenceEquals(plate, released),
                $"{mealDefName} must release the same physical plate instance.");
            IntegrationAssert.Equal(plateId, released?.ThingID,
                $"{mealDefName} must preserve plate identity through release.");
            IntegrationAssert.Equal(0, embedded.EmbeddedPlateCount,
                $"{mealDefName} must contain no duplicate plate after release.");
            IntegrationAssert.True(embedded.ReleasePlateThing() is null,
                $"{mealDefName} must not release a replacement plate after the exact one is removed.");
        }
        catch (IntegrationTestAssertionException exception)
        {
            primaryFailure = exception;
            throw;
        }
        catch (Exception exception)
        {
            primaryFailure = exception;
            var reportable = exception is TargetInvocationException { InnerException: not null } invocation
                ? invocation.InnerException
                : exception;
            throw new IntegrationTestAssertionException(
                $"{mealDefName} lifecycle phase '{phase}' failed with " +
                $"{reportable!.GetType().FullName}: {reportable.Message}");
        }
        finally
        {
            var cleanupFailures = CleanupAll(
                () => DestroyIfNeeded(meal),
                () => DestroyIfNeeded(plate));
            if (primaryFailure is null && cleanupFailures.Count > 0)
            {
                throw new IntegrationTestAssertionException(
                    "Meal-texture lifecycle fixture cleanup failed: " +
                    string.Join(" | ", cleanupFailures.Select(exception =>
                        $"{exception.GetType().FullName}: {exception.Message}")));
            }
        }
    }

    private static IReadOnlyList<Exception> CleanupAll(params Action[] actions)
    {
        var failures = new List<Exception>();
        foreach (var action in actions)
        {
            try
            {
                action();
            }
            catch (Exception exception)
            {
                failures.Add(exception);
            }
        }

        return failures.AsReadOnly();
    }

    private static void DestroyIfNeeded(Thing? thing)
    {
        if (thing is not null && !thing.Destroyed)
        {
            thing.Destroy(DestroyMode.Vanish);
        }
    }
}
