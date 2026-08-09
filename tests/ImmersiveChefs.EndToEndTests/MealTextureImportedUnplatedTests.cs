using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.dmtr-imported-unplated",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "Thekiborg.DMTR",
    "fumblesneeze.immersivechefs",
    MaxFrames = 3_600,
    MaxGameTicks = 12_000,
    MaxWallClockSeconds = 120)]
public sealed class DmtrImportedUnplatedTest : IRimWorldEndToEndTest
{
    private MealTextureImportedFixture fixture = null!;

    public void Arrange(IEndToEndContext context)
    {
        fixture = MealTextureImportedFixture.Create("DMTR", "MealSimple");
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) =>
        fixture.Execute(context).GetEnumerator();
}

[RimWorldEndToEndTest(
    "immersive-chefs.ftv-imported-unplated",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "Goat.Food.Texture.Variety.Core",
    "Goat.Food.Texture.Variety",
    "fumblesneeze.immersivechefs",
    MaxFrames = 3_600,
    MaxGameTicks = 12_000,
    MaxWallClockSeconds = 120)]
public sealed class FtvImportedUnplatedTest : IRimWorldEndToEndTest
{
    private MealTextureImportedFixture fixture = null!;

    public void Arrange(IEndToEndContext context)
    {
        fixture = MealTextureImportedFixture.Create("FTV", "FTV_MealSimple");
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) =>
        fixture.Execute(context).GetEnumerator();
}

[RimWorldEndToEndTest(
    "immersive-chefs.dmtr-ftv-imported-unplated",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "Thekiborg.DMTR",
    "Goat.Food.Texture.Variety.Core",
    "Goat.Food.Texture.Variety",
    "fumblesneeze.immersivechefs",
    MaxFrames = 7_200,
    MaxGameTicks = 24_000,
    MaxWallClockSeconds = 240)]
public sealed class DmtrFtvImportedUnplatedTest : IRimWorldEndToEndTest
{
    private MealTextureImportedFixture fixture = null!;

    public void Arrange(IEndToEndContext context)
    {
        fixture = MealTextureImportedFixture.Create(
            "DMTR and FTV",
            "MealSimple",
            "FTV_MealSimple");
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) =>
        fixture.Execute(context).GetEnumerator();
}

internal sealed class MealTextureImportedFixture
{
    private const string DmtrGraphicType =
        "DynamicMealTextureReplacer.Graphic_IngredientsVariant";
    private const string FtvGraphicType =
        "FoodTextureVariety.Graphic_MealVariantsExpanded";
    private readonly string groupName;
    private readonly IReadOnlyList<ImportedMealCase> cases;
    private readonly ThingDef plateDef;
    private readonly ThingDef cutleryDef;

    private MealTextureImportedFixture(
        string groupName,
        IReadOnlyList<ImportedMealCase> cases,
        ThingDef plateDef,
        ThingDef cutleryDef)
    {
        this.groupName = groupName;
        this.cases = cases;
        this.plateDef = plateDef;
        this.cutleryDef = cutleryDef;
    }

    internal static MealTextureImportedFixture Create(
        string groupName,
        params string[] mealDefNames)
    {
        var map = Current.Game.CurrentMap;
        var centers = FindRoomCenters(map, mealDefNames.Length);
        var rice = DefDatabase<ThingDef>.GetNamed("RawRice");
        var cases = new List<ImportedMealCase>();
        for (var index = 0; index < mealDefNames.Length; index++)
        {
            var center = centers[index];
            FoodSearchE2EFixture.BuildSealedRoom(map, center);

            var pawn = FoodSearchE2EFixture.CreateColonist(
                groupName + " imported diner " + (index + 1));
            FoodSearchE2EFixture.SetHunger(pawn, 1f);
            GenSpawn.Spawn(pawn, center + (IntVec3.West * 2), map);

            var mealDef = DefDatabase<ThingDef>.GetNamed(mealDefNames[index]);
            var meal = (ThingWithComps)ThingMaker.MakeThing(mealDef);
            var embedded = meal.GetComp<CompEmbeddedWare>();
            var ingredients = meal.GetComp<CompIngredients>();
            EndToEndAssert.NotNull(embedded,
                mealDef.defName + " must retain finalized embedded-ware state.");
            EndToEndAssert.NotNull(ingredients,
                mealDef.defName + " must retain public ingredient provenance.");
            EndToEndAssert.Equal(0, embedded!.EmbeddedPlateCount,
                "The imported fixture must begin honestly unplated.");
            ingredients!.RegisterIngredient(rice);
            GenSpawn.Spawn(meal, center + (IntVec3.East * 2), map);

            var graphic = meal.Graphic;
            EndToEndAssert.NotNull(
                graphic.MatSingleFor(meal),
                mealDef.defName + " must resolve an ingredient-driven material.");
            var expectedOwner = mealDef.defName.StartsWith("FTV_", StringComparison.Ordinal)
                ? FtvGraphicType
                : DmtrGraphicType;
            EndToEndAssert.Equal(expectedOwner, graphic.GetType().FullName,
                mealDef.defName + " must retain its exact upstream graphic owner.");
            EndToEndAssert.Equal(
                "RawRice",
                string.Join("|", ingredients.ingredients.Select(def => def.defName)),
                mealDef.defName + " rendering must retain its mapped public rice provenance.");
            cases.Add(new ImportedMealCase(pawn, meal, expectedOwner));
        }

        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        var cutleryDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery");
        var fixture = new MealTextureImportedFixture(groupName, cases, plateDef, cutleryDef);
        EndToEndAssert.Equal(0, fixture.CountServiceWare(map),
            "The imported-meal fixture must not arrange loose or held service ware.");
        return fixture;
    }

    internal IEnumerable<EndToEndStep> Execute(IEndToEndContext context)
    {
        foreach (var mealCase in cases)
        {
            FoodSearchE2EFixture.SetHunger(mealCase.Pawn, 0.05f);
            yield return new TimeControlActionStep(
                "pause before " + mealCase.Meal.def.defName + " import dining",
                paused: true,
                EndToEndGameSpeed.Normal);
            yield return new SelectionActionStep(
                "select imported " + mealCase.Meal.def.label + " and diner",
                new[] { mealCase.Pawn.ThingID, mealCase.Meal.ThingID },
                additive: false);
            yield return new CameraActionStep(
                "frame imported " + mealCase.Meal.def.label + " and diner",
                new[] { mealCase.Pawn.ThingID, mealCase.Meal.ThingID },
                paddingPixels: 150);
            yield return new ScreenshotStep(
                "observe mapped imported " + mealCase.Meal.def.label + " without a plate",
                new[] { mealCase.Pawn.ThingID, mealCase.Meal.ThingID },
                paddingPixels: 140);

            var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
                .Query(mealCase.Pawn.ThingID, mealCase.Meal.ThingID);
            var consume = options.Where(option =>
                    !option.Disabled &&
                    option.Label.IndexOf("consume", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            EndToEndAssert.Equal(1, consume.Length,
                "Expected one enabled native Consume option for imported " +
                mealCase.Meal.def.defName + "; observed " +
                string.Join(", ", options.Select(option =>
                    $"'{option.Label}' (disabled={option.Disabled})")));
            yield return new FloatMenuActionStep(
                "order native consumption of imported " + mealCase.Meal.def.label,
                mealCase.Pawn.ThingID,
                mealCase.Meal.ThingID,
                consume[0].StableId);
            yield return new TimeControlActionStep(
                "run native imported-meal ingestion",
                paused: false,
                EndToEndGameSpeed.Normal);
            yield return new WaitUntilStep(
                "diner reaches native ingest toil for " + mealCase.Meal.def.defName,
                _ => !mealCase.Meal.Destroyed &&
                     mealCase.Pawn.CurJobDef == JobDefOf.Ingest,
                new EndToEndDeadline(900, 2_000, TimeSpan.FromSeconds(35)));
            yield return new ScreenshotStep(
                "observe native ingestion of imported " + mealCase.Meal.def.label,
                new[] { mealCase.Pawn.ThingID },
                paddingPixels: 140);
            yield return new TimeControlActionStep(
                "finish imported-meal ingestion",
                paused: false,
                EndToEndGameSpeed.Superfast);
            yield return new WaitUntilStep(
                "imported " + mealCase.Meal.def.defName + " is consumed",
                _ => mealCase.Meal.Destroyed,
                new EndToEndDeadline(1_800, 6_000, TimeSpan.FromSeconds(60)));
            yield return new TimeControlActionStep(
                "pause after imported-meal ingestion",
                paused: true,
                EndToEndGameSpeed.Normal);
            yield return new AssertionStep(
                "imported meal returns no fabricated service ware",
                _ =>
                {
                    EndToEndAssert.True(mealCase.Meal.Destroyed,
                        "The exact imported meal must be consumed.");
                    EndToEndAssert.Equal(0, CountServiceWare(Current.Game.CurrentMap),
                        "Eating a meal that never contained a plate must not fabricate plate or cutlery.");
                });
            yield return new SelectionActionStep(
                "select imported-meal diner after ingestion",
                new[] { mealCase.Pawn.ThingID },
                additive: false);
            yield return new ScreenshotStep(
                "observe no fabricated setting after imported " + mealCase.Meal.def.label,
                new[] { mealCase.Pawn.ThingID },
                paddingPixels: 180);
        }

        yield return new CheckpointStep(
            groupName + " imported unplated result",
            _ => cases.ToDictionary(
                mealCase => mealCase.Meal.def.defName,
                mealCase =>
                    "destroyed=" + mealCase.Meal.Destroyed +
                    "; graphicOwner=" + mealCase.GraphicOwner +
                    "; serviceWare=" + CountServiceWare(Current.Game.CurrentMap)));
    }

    private int CountServiceWare(Map map)
    {
        var mapCount = map.listerThings.AllThings.Count(IsServiceWare);
        var pawnCount = cases.Sum(mealCase =>
            (mealCase.Pawn.inventory?.innerContainer.Count(IsServiceWare) ?? 0) +
            (IsServiceWare(mealCase.Pawn.carryTracker?.CarriedThing) ? 1 : 0));
        return mapCount + pawnCount;
    }

    private bool IsServiceWare(Thing? thing) =>
        thing is not null && (thing.def == plateDef || thing.def == cutleryDef);

    private static IReadOnlyList<IntVec3> FindRoomCenters(Map map, int count)
    {
        var centers = new List<IntVec3>();
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 70f, useCenter: true))
        {
            if (centers.Any(center => center.DistanceToSquared(candidate) < 225) ||
                !SquareIsUsable(map, candidate, 6))
            {
                continue;
            }

            centers.Add(candidate);
            if (centers.Count == count)
            {
                return centers;
            }
        }

        throw new EndToEndAssertionException(
            "Could not find enough separated imported-meal fixture rooms.");
    }

    private static bool SquareIsUsable(Map map, IntVec3 center, int radius)
    {
        for (var x = -radius; x <= radius; x++)
        {
            for (var z = -radius; z <= radius; z++)
            {
                var cell = center + new IntVec3(x, 0, z);
                if (!cell.InBounds(map) ||
                    !cell.Walkable(map) ||
                    cell.GetEdifice(map) is not null ||
                    cell.GetFirstPawn(map) is not null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private sealed class ImportedMealCase
    {
        internal ImportedMealCase(Pawn pawn, ThingWithComps meal, string graphicOwner)
        {
            Pawn = pawn;
            Meal = meal;
            GraphicOwner = graphicOwner;
        }

        internal Pawn Pawn { get; }
        internal ThingWithComps Meal { get; }
        internal string GraphicOwner { get; }
    }
}
