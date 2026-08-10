using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.food-texture-variety-vce-save-load",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "OskarPotocki.VanillaFactionsExpanded.Core",
    "VanillaExpanded.VCookE",
    "VanillaExpanded.VCookEBakery",
    "VanillaExpanded.VCookEHaute",
    "VanillaExpanded.VCookEStews",
    "VanillaExpanded.VCEF",
    "VanillaExpanded.VCookESushi",
    "Goat.Food.Texture.Variety.Core",
    "Goat.Food.Texture.Variety",
    "Goat.Food.Texture.Variety.VECooking",
    "Goat.Food.Texture.Variety.VEStew",
    "Goat.Food.Texture.Variety.VESushi",
    "Thekiborg.DMTR",
    "Evyatar108.VarietyMattersImprovedRedux",
    "VanillaExpanded.VanillaFoodVarietyExpanded",
    "fumblesneeze.immersivechefs",
    MaxFrames = 7_200,
    MaxGameTicks = 28_000,
    MaxWallClockSeconds = 300)]
public sealed class FoodTextureVarietyVcePersistenceTest : IRimWorldEndToEndTest
{
    private FoodTextureVarietyPersistenceFixture fixture = null!;

    public void Arrange(IEndToEndContext context)
    {
        fixture = new FoodTextureVarietyPersistenceFixture(
            "VCE_CookedStewSimple",
            "ImmersiveChefsE2E_FtvVcePersistence");
        fixture.Arrange(context);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) =>
        fixture.Execute(context);
}

[RimWorldEndToEndTest(
    "immersive-chefs.food-texture-variety-save-load",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "Goat.Food.Texture.Variety.Core",
    "Goat.Food.Texture.Variety",
    "fumblesneeze.immersivechefs",
    MaxFrames = 7_200,
    MaxGameTicks = 28_000,
    MaxWallClockSeconds = 300)]
public sealed class FoodTextureVarietyPersistenceTest : IRimWorldEndToEndTest
{
    private FoodTextureVarietyPersistenceFixture fixture = null!;

    public void Arrange(IEndToEndContext context)
    {
        fixture = new FoodTextureVarietyPersistenceFixture(
            "FTV_MealSimple",
            "ImmersiveChefsE2E_FtvPersistence");
        fixture.Arrange(context);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) =>
        fixture.Execute(context);
}

internal sealed class FoodTextureVarietyPersistenceFixture : IRimWorldEndToEndTest
{
    private readonly string mealDefName;
    private readonly string FirstSaveName;
    private readonly string SecondSaveName;
    private string mealId = string.Empty;
    private string holderPawnId = string.Empty;
    private string plateId = string.Empty;
    private IntVec3 mealCell;
    private string[] selectedPaths = Array.Empty<string>();
    private int selectedIndex;
    private CulinaryServingSnapshot expectedServing;

    internal FoodTextureVarietyPersistenceFixture(string mealDefName, string saveNamePrefix)
    {
        this.mealDefName = mealDefName;
        FirstSaveName = saveNamePrefix + "First";
        SecondSaveName = saveNamePrefix + "Second";
    }

    public void Arrange(IEndToEndContext context)
    {
        var priorMealTemperature = ImmersiveChefsMod.Settings.MealTemperatureEnabled;
        ImmersiveChefsMod.Settings.MealTemperatureEnabled = false;
        context.DeferCleanup(() =>
            ImmersiveChefsMod.Settings.MealTemperatureEnabled = priorMealTemperature);

        var savePaths = new[]
        {
            GenFilePaths.FilePathForSavedGame(FirstSaveName),
            GenFilePaths.FilePathForSavedGame(SecondSaveName)
        };
        context.DeferCleanup(() =>
        {
            foreach (var savePath in savePaths)
            {
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                }
            }
        });

        var map = Current.Game.CurrentMap;
        var cell = GenRadial.RadialCellsAround(map.Center, 35f, useCenter: true)
            .First(candidate => candidate.InBounds(map) && candidate.Standable(map) &&
                                candidate.GetEdifice(map) is null &&
                                candidate.GetFirstPawn(map) is null);
        var mealDef = DefDatabase<ThingDef>.GetNamed(mealDefName);
        var meal = (ThingWithComps)ThingMaker.MakeThing(mealDef);
        meal.stackCount = 1;
        meal.GetComp<CompIngredients>()!.ingredients.Add(
            DefDatabase<ThingDef>.GetNamed("RawRice"));
        var seededServing = new CulinaryServingRecord(
            78,
            64f,
            ContaminationSources.DirtyCookware | ContaminationSources.WildWaterPlate,
            2,
            Math.Max(1, Find.TickManager.TicksGame),
            new[] { "RawRice" },
            DietaryFlags.Plant | DietaryFlags.VegetarianCompatible,
            KitchenMaterialKind.Lead);
        expectedServing = seededServing.Capture();
        meal.GetComp<CompCulinaryState>()!.ReplaceServings(new[] { seededServing });

        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.WoodLog);
        plate.GetComp<CompSanitation>()!.MarkDirty();
        EndToEndAssert.True(
            meal.GetComp<CompEmbeddedWare>()!.TryEmbedPlate(plate),
            "The visible FTV meal must begin with one real embedded plate.");
        GenSpawn.Spawn(meal, cell, map);

        ((Graphic_StackCount)meal.Graphic).SubGraphicFor(meal);
        var ftvComp = FindFtvComp(meal);
        selectedPaths = SelectedPaths(ftvComp);
        selectedIndex = ReadField<int>(ftvComp, "textureIndex");
        EndToEndAssert.Equal(3, selectedPaths.Length,
            "FTV must visibly select one complete three-sprite group before saving.");
        EndToEndAssert.True(selectedIndex >= 0,
            "Immersive Chefs must capture the visible group's finalized index before saving.");

        var holderCell = GenRadial.RadialCellsAround(
                new IntVec3(map.Size.x - 30, 0, map.Size.z - 30),
                12f,
                useCenter: true)
            .First(candidate => candidate.InBounds(map) && candidate.Standable(map) &&
                                candidate.GetEdifice(map) is null &&
                                candidate.GetFirstPawn(map) is null);
        var holderPawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        GenSpawn.Spawn(holderPawn, holderCell, map);

        mealId = meal.ThingID;
        holderPawnId = holderPawn.ThingID;
        plateId = plate.ThingID;
        mealCell = cell;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause the visible FTV serving",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the FTV serving before saving",
            new[] { mealId },
            additive: false);
        yield return new CameraActionStep(
            "frame the FTV serving before saving",
            new[] { mealId },
            paddingPixels: 260);
        yield return new ScreenshotStep(
            "observe the selected FTV texture and plated meal before saving",
            new[] { mealId },
            paddingPixels: 320);
        yield return new CheckpointStep(
            "capture the visible FTV serving before saving",
            _ => CaptureState(FindLoadedMeal()));

        StoreMealForPendingRoundTrip();
        yield return new SaveLoadActionStep(
            "save and load the rendered FTV meal through RimWorld",
            FirstSaveName);
        yield return new WaitUntilStep(
            "the same meal is held without rendering after the first load",
            _ => TryFindHeldMeal() is not null,
            new EndToEndDeadline(2_400, 10_000, TimeSpan.FromSeconds(120)));
        yield return new AssertionStep(
            "the first loaded meal remains on FTV's pending first-draw branch",
            _ => AssertPendingFirstDraw(FindHeldMeal()));
        yield return new SaveLoadActionStep(
            "save and load again before FTV can draw the held meal",
            SecondSaveName);
        yield return new WaitUntilStep(
            "the same held meal is available after the second load",
            _ => TryFindHeldMeal() is not null,
            new EndToEndDeadline(2_400, 10_000, TimeSpan.FromSeconds(120)));

        var loaded = RestoreMealToMap();
        ((Graphic_StackCount)loaded.Graphic).SubGraphicFor(loaded);
        yield return new SelectionActionStep(
            "select the same FTV serving after loading",
            new[] { mealId },
            additive: false);
        yield return new CameraActionStep(
            "frame the same FTV serving after loading",
            new[] { mealId },
            paddingPixels: 260);
        yield return new AssertionStep(
            "the visible FTV serving and real meal state survive exactly once",
            _ => AssertReloadedState(FindLoadedMeal()));
        yield return new ScreenshotStep(
            "observe the same FTV texture and plated meal after loading",
            new[] { mealId },
            paddingPixels: 320);
        yield return new CheckpointStep(
            "capture the visible FTV serving after loading",
            _ => CaptureState(FindLoadedMeal()));
    }

    private void AssertReloadedState(ThingWithComps meal)
    {
        var ftvComp = FindFtvComp(meal);
        EndToEndAssert.Equal(
            string.Join("|", selectedPaths),
            string.Join("|", SelectedPaths(ftvComp)),
            "The loaded meal must render the same upstream-selected texture group.");
        EndToEndAssert.Equal(
            selectedIndex,
            ReadField<int>(ftvComp, "textureIndex"),
            "The stable FTV group index must survive the native game reload.");
        EndToEndAssert.Equal(
            "FoodTextureVariety.Graphic_MealVariantsExpanded",
            meal.Graphic.GetType().FullName,
            "Food Texture Variety must remain the visible graphic owner.");
        EndToEndAssert.Equal(mealId, meal.ThingID,
            "The exact meal identity must survive both native reloads.");
        EndToEndAssert.Equal(1, meal.stackCount,
            "The saved meal must remain one physical serving.");
        var ingredients = meal.GetComp<CompIngredients>()!.ingredients;
        EndToEndAssert.Equal(
            1,
            ingredients.Count,
            "Exactly one public ingredient entry must survive the native reloads.");
        EndToEndAssert.Equal("RawRice", ingredients[0].defName,
            "The sole surviving public ingredient must remain RawRice.");

        var embedded = meal.GetComp<CompEmbeddedWare>()!;
        EndToEndAssert.Equal(1, embedded.EmbeddedPlateCount,
            "The loaded serving must retain one real embedded plate.");
        var plate = (ThingWithComps)embedded.PeekPlateThing()!;
        EndToEndAssert.Equal(plateId, plate.ThingID,
            "The same physical plate identity must survive the native game reload.");
        EndToEndAssert.True(plate.GetComp<CompSanitation>()!.IsDirty,
            "The embedded plate's dirty state must survive the native game reload.");
        var culinary = meal.GetComp<CompCulinaryState>()!;
        EndToEndAssert.Equal(1, culinary.Servings.Count,
            "Exactly one culinary serving must survive the native reloads.");
        var actualServing = culinary.PeekCurrentServingWithoutThermalUpdate();
        EndToEndAssert.NotNull(actualServing,
            "The exact culinary serving must remain available after reload.");
        AssertServingEqual(expectedServing, actualServing!.Capture());
    }

    private static void AssertServingEqual(
        CulinaryServingSnapshot expected,
        CulinaryServingSnapshot actual)
    {
        EndToEndAssert.Equal(expected.SchemaVersion, actual.SchemaVersion,
            "Culinary schema version must survive.");
        EndToEndAssert.Equal(expected.QualityScore, actual.QualityScore,
            "Culinary quality must survive.");
        EndToEndAssert.Equal(expected.TemperatureCelsius, actual.TemperatureCelsius,
            "Culinary temperature must survive.");
        EndToEndAssert.Equal(expected.Contamination, actual.Contamination,
            "Culinary contamination must survive.");
        EndToEndAssert.Equal(expected.MicrowaveReheatCount, actual.MicrowaveReheatCount,
            "Culinary microwave count must survive.");
        EndToEndAssert.Equal(expected.LastThermalTick, actual.LastThermalTick,
            "Culinary thermal tick must survive.");
        EndToEndAssert.Equal(
            string.Join("|", expected.HiddenSourceDefNames),
            string.Join("|", actual.HiddenSourceDefNames),
            "Hidden culinary provenance must survive exactly.");
        EndToEndAssert.Equal(expected.HiddenDietaryFlags, actual.HiddenDietaryFlags,
            "Hidden dietary flags must survive.");
        EndToEndAssert.Equal(expected.CookwareMaterial, actual.CookwareMaterial,
            "Hidden cookware material provenance must survive.");
    }

    private static void AssertPendingFirstDraw(ThingWithComps meal)
    {
        var comp = FindFtvComp(meal);
        EndToEndAssert.True(
            ReadField<bool>(comp, "firstLoad"),
            "The first native load must leave the held FTV meal pending its first draw.");
        EndToEndAssert.Equal(
            0,
            (ReadField<Graphic[]>(comp, "storedGraphics") ?? Array.Empty<Graphic>()).Length,
            "The held meal must not reconstruct transient FTV graphics before the second native save.");
    }

    private IReadOnlyDictionary<string, string> CaptureState(ThingWithComps meal)
    {
        var embedded = meal.GetComp<CompEmbeddedWare>()!;
        var plate = (ThingWithComps)embedded.PeekPlateThing()!;
        return new Dictionary<string, string>
        {
            ["mealId"] = meal.ThingID,
            ["graphicOwner"] = meal.Graphic.GetType().FullName ?? string.Empty,
            ["textureIndex"] = ReadField<int>(FindFtvComp(meal), "textureIndex").ToString(),
            ["texturePaths"] = string.Join("|", SelectedPaths(FindFtvComp(meal))),
            ["plateId"] = plate.ThingID,
            ["plateDirty"] = plate.GetComp<CompSanitation>()!.IsDirty.ToString(),
            ["ingredientCount"] = meal.GetComp<CompIngredients>()!.ingredients.Count.ToString(),
            ["culinaryQuality"] = meal.GetComp<CompCulinaryState>()!
                .PeekCurrentServingWithoutThermalUpdate()!
                .QualityScore.ToString(),
            ["cookwareMaterial"] = meal.GetComp<CompCulinaryState>()!
                .PeekCurrentServingWithoutThermalUpdate()!
                .CookwareMaterial.ToString()
        };
    }

    private ThingWithComps FindLoadedMeal()
    {
        return TryFindLoadedMeal() ?? throw new EndToEndAssertionException(
            "The exact saved FTV meal is absent from the current map.");
    }

    private ThingWithComps? TryFindLoadedMeal()
    {
        return Current.Game?.CurrentMap?.listerThings.AllThings
            .OfType<ThingWithComps>()
            .SingleOrDefault(thing => string.Equals(thing.ThingID, mealId, StringComparison.Ordinal));
    }

    private void StoreMealForPendingRoundTrip()
    {
        var meal = FindLoadedMeal();
        var holder = FindHolderPawn();
        meal.DeSpawn(DestroyMode.Vanish);
        EndToEndAssert.True(
            holder.inventory.innerContainer.TryAdd(meal, canMergeWithExistingStacks: false),
            "The fixture must hold the exact meal outside the rendered map during the intermediate load.");
    }

    private ThingWithComps FindHeldMeal()
    {
        return TryFindHeldMeal() ?? throw new EndToEndAssertionException(
            "The exact saved FTV meal is absent from the holder pawn's inventory.");
    }

    private ThingWithComps? TryFindHeldMeal()
    {
        return FindHolderPawnOrNull()?.inventory?.innerContainer
            .OfType<ThingWithComps>()
            .SingleOrDefault(thing => string.Equals(thing.ThingID, mealId, StringComparison.Ordinal));
    }

    private ThingWithComps RestoreMealToMap()
    {
        var holder = FindHolderPawn();
        var meal = FindHeldMeal();
        EndToEndAssert.True(
            holder.inventory.innerContainer.TryDrop(
                meal,
                mealCell,
                holder.Map,
                ThingPlaceMode.Direct,
                out var dropped),
            "The fixture must return the exact twice-loaded meal to the visible map.");
        EndToEndAssert.Equal(
            mealId,
            dropped.ThingID,
            "Returning the twice-loaded meal must preserve its stable identity.");
        return (ThingWithComps)dropped;
    }

    private Pawn FindHolderPawn()
    {
        return FindHolderPawnOrNull() ?? throw new EndToEndAssertionException(
            "The exact persistence holder pawn is absent from the current map.");
    }

    private Pawn? FindHolderPawnOrNull()
    {
        return Current.Game?.CurrentMap?.mapPawns.AllPawns
            .SingleOrDefault(pawn => string.Equals(pawn.ThingID, holderPawnId, StringComparison.Ordinal));
    }

    private static object FindFtvComp(ThingWithComps meal)
    {
        return meal.AllComps.Single(comp => string.Equals(
            comp.GetType().FullName,
            "FoodTextureVariety.CompFoodAlternateTexture",
            StringComparison.Ordinal));
    }

    private static string[] SelectedPaths(object comp)
    {
        return ReadField<Graphic[]>(comp, "storedGraphics")
            .Select(graphic => graphic.path ?? string.Empty)
            .ToArray();
    }

    private static T ReadField<T>(object instance, string name)
    {
        return (T)instance.GetType().GetField(
            name,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)!.GetValue(instance)!;
    }
}
