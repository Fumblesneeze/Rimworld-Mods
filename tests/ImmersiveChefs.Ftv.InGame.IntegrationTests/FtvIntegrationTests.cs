using System;
using System.IO;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.IntegrationTesting;
using Verse;

namespace ImmersiveChefs.MealTexture.InGame.IntegrationTests;

public static class FtvIntegrationTests
{
    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FtvOwnsVarietyGraphicsWhileImmersiveStateRemainsAttached()
    {
        var phase = "active package identity";
        try
        {
            MealTextureDefAssertions.RequireActive(
                MealTextureDefAssertions.FtvCorePackageId,
                MealTextureDefAssertions.FtvPackageId);
            MealTextureDefAssertions.RequireAbsent(MealTextureDefAssertions.DmtrPackageId);
            phase = "vanilla meal graphic ownership";
            MealTextureDefAssertions.AssertVanillaMealOwner();
            phase = "FTV meal graphic and component ownership";
            MealTextureDefAssertions.AssertFtvOwnsVarietyMeals();
        }
        catch (IntegrationTestAssertionException)
        {
            throw;
        }
        catch (System.Exception exception)
        {
            throw new IntegrationTestAssertionException(
                $"FTV loaded-Def phase '{phase}' failed with " +
                $"{exception.GetType().FullName}: {exception.Message}");
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void SelectedTextureGroupSurvivesTheRealScribePipeline()
    {
        IntegrationAssert.True(
            FoodTextureVarietyAdapter.Enabled,
            "The exact FTV group must activate the shape-gated save/load adapter.");

        var mealDef = DefDatabase<ThingDef>.GetNamed("FTV_MealSimple");
        var original = (ThingWithComps)ThingMaker.MakeThing(mealDef);
        original.stackCount = 1;
        var ingredients = original.GetComp<CompIngredients>();
        var embedded = original.GetComp<CompEmbeddedWare>();
        var culinary = original.GetComp<CompCulinaryState>();
        IntegrationAssert.NotNull(ingredients, "FTV meal must retain CompIngredients.");
        IntegrationAssert.NotNull(embedded, "FTV meal must retain its physical plate component.");
        IntegrationAssert.NotNull(culinary, "FTV meal must retain its culinary-state component.");

        var rawRice = DefDatabase<ThingDef>.GetNamed("RawRice");
        ingredients!.ingredients.Add(rawRice);
        culinary!.ReplaceServings(new[]
        {
            new CulinaryServingRecord(
                78,
                64f,
                ContaminationSources.DirtyCookware,
                0,
                Find.TickManager.TicksGame)
        });
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        var plate = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.WoodLog);
        plate.GetComp<CompSanitation>()!.MarkDirty();
        var plateId = plate.ThingID;
        IntegrationAssert.True(embedded!.TryEmbedPlate(plate), "The FTV meal must accept a real plate.");

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

        var firstPath = Path.Combine(
            GenFilePaths.TempFolderPath,
            "immersive-chefs-ftv-first-" + Guid.NewGuid().ToString("N") + ".xml");
        var secondPath = Path.Combine(
            GenFilePaths.TempFolderPath,
            "immersive-chefs-ftv-second-" + Guid.NewGuid().ToString("N") + ".xml");
        Thing? loadedWithoutDrawing = null;
        Thing? loaded = null;
        try
        {
            Thing originalForScribe = original;
            try
            {
                Scribe.saver.InitSaving(firstPath, "ftvMealRoundTrip");
                Scribe_Deep.Look(ref originalForScribe, "thing");
                Scribe.saver.FinalizeSaving();
            }
            catch
            {
                Scribe.saver.ForceStop();
                throw;
            }

            try
            {
                Scribe.loader.InitLoading(firstPath);
                Scribe_Deep.Look(ref loadedWithoutDrawing, "thing");
                Scribe.loader.FinalizeLoading();
            }
            catch
            {
                Scribe.loader.ForceStop();
                throw;
            }

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
            try
            {
                Scribe.saver.InitSaving(secondPath, "ftvMealRoundTrip");
                Scribe_Deep.Look(ref intermediateForScribe, "thing");
                Scribe.saver.FinalizeSaving();
            }
            catch
            {
                Scribe.saver.ForceStop();
                throw;
            }

            try
            {
                Scribe.loader.InitLoading(secondPath);
                Scribe_Deep.Look(ref loaded, "thing");
                Scribe.loader.FinalizeLoading();
            }
            catch
            {
                Scribe.loader.ForceStop();
                throw;
            }

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
            IntegrationAssert.True(
                loadedIngredients?.ingredients.Count(def => def == rawRice) == 1,
                "Ingredient provenance must survive exactly once.");
            IntegrationAssert.Equal(1, loadedEmbedded!.EmbeddedPlateCount,
                "The real embedded plate must survive exactly once.");
            IntegrationAssert.Equal(plateId, loadedEmbedded.PeekPlateThing()!.ThingID,
                "The embedded plate identity must survive the Scribe round trip.");
            IntegrationAssert.True(
                ((ThingWithComps)loadedEmbedded.PeekPlateThing()!).GetComp<CompSanitation>()!.IsDirty,
                "The embedded plate sanitation state must survive.");
            IntegrationAssert.Equal(78, loadedCulinary!.PeekCurrentServingWithoutThermalUpdate()!.QualityScore,
                "Culinary quality must survive exactly once.");
        }
        finally
        {
            if (!original.Destroyed)
            {
                original.Destroy(DestroyMode.Vanish);
            }

            if (loaded is not null && !loaded.Destroyed)
            {
                loaded.Destroy(DestroyMode.Vanish);
            }

            if (loadedWithoutDrawing is not null && !loadedWithoutDrawing.Destroyed)
            {
                loadedWithoutDrawing.Destroy(DestroyMode.Vanish);
            }

            if (File.Exists(firstPath))
            {
                File.Delete(firstPath);
            }

            if (File.Exists(secondPath))
            {
                File.Delete(secondPath);
            }
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
}
