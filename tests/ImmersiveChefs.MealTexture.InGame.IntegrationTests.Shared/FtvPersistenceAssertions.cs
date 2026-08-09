using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.IntegrationTesting;
using Verse;

namespace ImmersiveChefs.MealTexture.InGame.IntegrationTests;

internal static class FtvPersistenceAssertions
{
    internal static void AssertSelectedTextureGroupSurvivesRealScribePipeline(string mealDefName)
    {
        IntegrationAssert.True(
            FoodTextureVarietyAdapter.Enabled,
            "The exact FTV group must activate the shape-gated save/load adapter.");

        var mealDef = DefDatabase<ThingDef>.GetNamed(mealDefName);
        var original = (ThingWithComps)ThingMaker.MakeThing(mealDef);
        ThingWithComps? plate = null;
        Thing? loadedWithoutDrawing = null;
        Thing? loaded = null;
        string? firstPath = null;
        string? secondPath = null;
        Exception? primaryFailure = null;
        try
        {
            original.stackCount = 1;
            var mealId = original.ThingID;
            var ingredients = original.GetComp<CompIngredients>();
            var embedded = original.GetComp<CompEmbeddedWare>();
            var culinary = original.GetComp<CompCulinaryState>();
            IntegrationAssert.NotNull(ingredients, $"{mealDefName} must retain CompIngredients.");
            IntegrationAssert.NotNull(embedded, $"{mealDefName} must retain its physical plate component.");
            IntegrationAssert.NotNull(culinary, $"{mealDefName} must retain its culinary-state component.");

            var rawRice = DefDatabase<ThingDef>.GetNamed("RawRice");
            ingredients!.ingredients.Add(rawRice);
            var seededServing = new CulinaryServingRecord(
                78,
                64f,
                ContaminationSources.DirtyCookware | ContaminationSources.WildWaterPlate,
                2,
                Math.Max(1, Find.TickManager.TicksGame),
                new[] { "RawRice" },
                DietaryFlags.Plant | DietaryFlags.VegetarianCompatible);
            var expectedServing = seededServing.Capture();
            culinary!.ReplaceServings(new[] { seededServing });

            var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
            plate = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.WoodLog);
            plate.GetComp<CompSanitation>()!.MarkDirty();
            var plateId = plate.ThingID;
            IntegrationAssert.True(embedded!.TryEmbedPlate(plate), $"{mealDefName} must accept a real plate.");

            var graphic = original.Graphic;
            var subGraphicFor = graphic.GetType().GetMethod(
                "SubGraphicFor",
                BindingFlags.Instance | BindingFlags.Public,
                null,
                new[] { typeof(Thing) },
                null);
            IntegrationAssert.NotNull(subGraphicFor, "FTV graphic must expose its exact selection method.");
            subGraphicFor!.Invoke(graphic, new object[] { original });
            var comp = FindFtvComp(original);
            var selectedBefore = SelectedPaths(comp);
            var indexBefore = ReadField<int>(comp, "textureIndex");
            IntegrationAssert.Equal(3, selectedBefore.Length, "FTV must select one complete texture group.");
            IntegrationAssert.True(indexBefore >= 0, "Immersive Chefs must capture the finalized group index.");

            firstPath = Path.Combine(
                GenFilePaths.TempFolderPath,
                "immersive-chefs-ftv-first-" + Guid.NewGuid().ToString("N") + ".xml");
            secondPath = Path.Combine(
                GenFilePaths.TempFolderPath,
                "immersive-chefs-ftv-second-" + Guid.NewGuid().ToString("N") + ".xml");
            Thing originalForScribe = original;
            SaveThing(firstPath, ref originalForScribe);
            LoadThing(firstPath, ref loadedWithoutDrawing);

            var intermediateMeal = loadedWithoutDrawing as ThingWithComps;
            IntegrationAssert.NotNull(intermediateMeal, "Scribe must reconstruct the intermediate FTV meal.");
            var intermediateComp = FindFtvComp(intermediateMeal!);
            IntegrationAssert.True(
                ReadField<bool>(intermediateComp, "firstLoad"),
                "The intermediate FTV meal must remain on its pending first-load branch.");
            IntegrationAssert.Equal(
                0,
                (ReadField<Graphic[]>(intermediateComp, "storedGraphics") ?? Array.Empty<Graphic>()).Length,
                "The intermediate FTV meal must not reconstruct transient graphics before its second save.");

            Thing intermediateForScribe = intermediateMeal!;
            SaveThing(secondPath, ref intermediateForScribe);
            LoadThing(secondPath, ref loaded);

            var loadedMeal = loaded as ThingWithComps;
            IntegrationAssert.NotNull(
                loadedMeal,
                "Scribe must reconstruct the FTV meal after saving it again before its first draw.");
            var loadedGraphic = loadedMeal!.Graphic;
            subGraphicFor.Invoke(loadedGraphic, new object[] { loadedMeal });
            var loadedComp = FindFtvComp(loadedMeal);
            IntegrationAssert.Equal(
                string.Join("|", selectedBefore),
                string.Join("|", SelectedPaths(loadedComp)),
                "FTV must render the same selected group after load.");
            IntegrationAssert.Equal(
                indexBefore,
                ReadField<int>(loadedComp, "textureIndex"),
                "The stable finalized-array index must survive load.");
            IntegrationAssert.Equal(
                FoodTextureVarietyAdapter.GraphicTypeNameForDiagnostics,
                loadedGraphic.GetType().FullName,
                "FTV must remain the graphic owner after load.");

            var loadedIngredients = loadedMeal.GetComp<CompIngredients>();
            var loadedEmbedded = loadedMeal.GetComp<CompEmbeddedWare>();
            var loadedCulinary = loadedMeal.GetComp<CompCulinaryState>();
            IntegrationAssert.Equal(
                mealId,
                loadedMeal.ThingID,
                "The same physical meal identity must survive the Scribe round trip.");
            IntegrationAssert.Equal(1, loadedMeal.stackCount, "The meal stack must still contain one serving.");
            IntegrationAssert.NotNull(loadedIngredients, "The loaded meal must retain CompIngredients.");
            IntegrationAssert.NotNull(loadedEmbedded, "The loaded meal must retain CompEmbeddedWare.");
            IntegrationAssert.NotNull(loadedCulinary, "The loaded meal must retain CompCulinaryState.");
            IntegrationAssert.True(
                loadedIngredients?.ingredients.Count(def => def == rawRice) == 1,
                "Ingredient provenance must survive exactly once.");
            IntegrationAssert.Equal(
                1,
                loadedEmbedded!.EmbeddedPlateCount,
                "The real embedded plate must survive exactly once.");
            IntegrationAssert.Equal(
                plateId,
                loadedEmbedded.PeekPlateThing()!.ThingID,
                "The embedded plate identity must survive the Scribe round trip.");
            IntegrationAssert.True(
                ((ThingWithComps)loadedEmbedded.PeekPlateThing()!).GetComp<CompSanitation>()!.IsDirty,
                "The embedded plate sanitation state must survive.");
            IntegrationAssert.Equal(1, loadedCulinary!.Servings.Count,
                "Exactly one culinary serving must survive the round trip.");
            var actualServing = loadedCulinary.PeekCurrentServingWithoutThermalUpdate();
            IntegrationAssert.NotNull(actualServing, "The loaded meal must retain its exact culinary serving.");
            AssertServingEqual(expectedServing, actualServing!.Capture());
        }
        catch (Exception exception)
        {
            primaryFailure = exception;
            throw;
        }
        finally
        {
            var cleanupFailures = CleanupAll(
                () => DestroyIfNeeded(original),
                () => DestroyIfNeeded(plate),
                () => DestroyIfNeeded(loaded),
                () => DestroyIfNeeded(loadedWithoutDrawing),
                () => DeleteIfPresent(firstPath),
                () => DeleteIfPresent(secondPath));
            if (primaryFailure is null && cleanupFailures.Count > 0)
            {
                throw new IntegrationTestAssertionException(
                    "FTV persistence fixture cleanup failed: " +
                    string.Join(" | ", cleanupFailures.Select(exception =>
                        $"{exception.GetType().FullName}: {exception.Message}")));
            }
        }
    }

    private static void AssertServingEqual(
        CulinaryServingSnapshot expected,
        CulinaryServingSnapshot actual)
    {
        IntegrationAssert.Equal(expected.SchemaVersion, actual.SchemaVersion,
            "The culinary schema version must survive.");
        IntegrationAssert.Equal(expected.QualityScore, actual.QualityScore,
            "Culinary quality must survive.");
        IntegrationAssert.Equal(expected.TemperatureCelsius, actual.TemperatureCelsius,
            "Meal temperature must survive.");
        IntegrationAssert.Equal(expected.Contamination, actual.Contamination,
            "Every contamination flag must survive.");
        IntegrationAssert.Equal(expected.MicrowaveReheatCount, actual.MicrowaveReheatCount,
            "The microwave reheat count must survive.");
        IntegrationAssert.Equal(expected.LastThermalTick, actual.LastThermalTick,
            "The last thermal tick must survive.");
        IntegrationAssert.Equal(
            string.Join("|", expected.HiddenSourceDefNames),
            string.Join("|", actual.HiddenSourceDefNames),
            "Hidden ingredient provenance must survive.");
        IntegrationAssert.Equal(expected.HiddenDietaryFlags, actual.HiddenDietaryFlags,
            "Hidden dietary flags must survive.");
    }

    private static void SaveThing(string path, ref Thing thing)
    {
        try
        {
            Scribe.saver.InitSaving(path, "ftvMealRoundTrip");
            Scribe_Deep.Look(ref thing, "thing");
            Scribe.saver.FinalizeSaving();
        }
        catch
        {
            Scribe.saver.ForceStop();
            throw;
        }
    }

    private static void LoadThing(string path, ref Thing? thing)
    {
        try
        {
            Scribe.loader.InitLoading(path);
            Scribe_Deep.Look(ref thing, "thing");
            Scribe.loader.FinalizeLoading();
        }
        catch
        {
            Scribe.loader.ForceStop();
            throw;
        }
    }

    private static object FindFtvComp(ThingWithComps meal)
    {
        var comp = meal.AllComps.SingleOrDefault(candidate => string.Equals(
            candidate.GetType().FullName,
            MealTextureDefAssertions.FtvCompType,
            StringComparison.Ordinal));
        IntegrationAssert.NotNull(comp, "The FTV meal must retain exactly one alternate-texture comp.");
        return comp!;
    }

    private static string[] SelectedPaths(object comp)
    {
        var graphics = ReadField<Graphic[]>(comp, "storedGraphics") ?? Array.Empty<Graphic>();
        var path = typeof(Graphic).GetField(
            "path",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!;
        return graphics.Select(graphic => path.GetValue(graphic) as string ?? string.Empty).ToArray();
    }

    private static T ReadField<T>(object instance, string name)
    {
        return (T)instance.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!.GetValue(instance)!;
    }

    private static void DestroyIfNeeded(Thing? thing)
    {
        if (thing is not null && !thing.Destroyed)
        {
            thing.Destroy(DestroyMode.Vanish);
        }
    }

    private static void DeleteIfPresent(string? path)
    {
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            File.Delete(path);
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
}
