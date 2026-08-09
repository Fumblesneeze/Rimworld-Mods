using System.Collections.Generic;
using System;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.texture-absent-hidden-paste",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "fumblesneeze.immersivechefs",
    MaxFrames = 7_200,
    MaxGameTicks = 28_000,
    MaxWallClockSeconds = 210)]
public sealed class TextureAbsentHiddenPasteTest : IRimWorldEndToEndTest
{
    private MealTextureHiddenPasteFixture fixture = null!;

    public void Arrange(IEndToEndContext context) =>
        fixture = MealTextureHiddenPasteFixture.Create(
            context,
            "base",
            ("CookMealSimple", "MealSimple", "Verse.Graphic_MealVariants"));

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) =>
        fixture.Execute(context).GetEnumerator();
}

[RimWorldEndToEndTest(
    "immersive-chefs.dmtr-hidden-paste",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "Thekiborg.DMTR",
    "fumblesneeze.immersivechefs",
    MaxFrames = 7_200,
    MaxGameTicks = 28_000,
    MaxWallClockSeconds = 210)]
public sealed class DmtrHiddenPasteTest : IRimWorldEndToEndTest
{
    private MealTextureHiddenPasteFixture fixture = null!;

    public void Arrange(IEndToEndContext context) =>
        fixture = MealTextureHiddenPasteFixture.Create(
            context,
            "DMTR",
            ("CookMealSimple", "MealSimple", "DynamicMealTextureReplacer.Graphic_IngredientsVariant"));

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) =>
        fixture.Execute(context).GetEnumerator();
}

[RimWorldEndToEndTest(
    "immersive-chefs.ftv-hidden-paste",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "Goat.Food.Texture.Variety.Core",
    "Goat.Food.Texture.Variety",
    "fumblesneeze.immersivechefs",
    MaxFrames = 7_200,
    MaxGameTicks = 28_000,
    MaxWallClockSeconds = 210)]
public sealed class FtvHiddenPasteTest : IRimWorldEndToEndTest
{
    private MealTextureHiddenPasteFixture fixture = null!;

    public void Arrange(IEndToEndContext context) =>
        fixture = MealTextureHiddenPasteFixture.Create(
            context,
            "FTV",
            ("FTV_CookMealSimple", "FTV_MealSimple", "FoodTextureVariety.Graphic_MealVariantsExpanded"));

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) =>
        fixture.Execute(context).GetEnumerator();
}

[RimWorldEndToEndTest(
    "immersive-chefs.dmtr-ftv-hidden-paste",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "Thekiborg.DMTR",
    "Goat.Food.Texture.Variety.Core",
    "Goat.Food.Texture.Variety",
    "fumblesneeze.immersivechefs",
    MaxFrames = 14_400,
    MaxGameTicks = 56_000,
    MaxWallClockSeconds = 420)]
public sealed class DmtrFtvHiddenPasteTest : IRimWorldEndToEndTest
{
    private MealTextureHiddenPasteFixture fixture = null!;

    public void Arrange(IEndToEndContext context) =>
        fixture = MealTextureHiddenPasteFixture.Create(
            context,
            "DMTR and FTV",
            ("CookMealSimple", "MealSimple", "DynamicMealTextureReplacer.Graphic_IngredientsVariant"),
            ("FTV_CookMealSimple", "FTV_MealSimple", "FoodTextureVariety.Graphic_MealVariantsExpanded"));

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) =>
        fixture.Execute(context).GetEnumerator();
}

internal sealed class MealTextureHiddenPasteFixture
{
    private readonly string groupName;
    private readonly IReadOnlyList<HiddenPasteCase> cases;
    private readonly ThingDef plateDef;
    private readonly ThingDef cutleryDef;

    private MealTextureHiddenPasteFixture(
        string groupName,
        IReadOnlyList<HiddenPasteCase> cases,
        ThingDef plateDef,
        ThingDef cutleryDef)
    {
        this.groupName = groupName;
        this.cases = cases;
        this.plateDef = plateDef;
        this.cutleryDef = cutleryDef;
    }

    internal static MealTextureHiddenPasteFixture Create(
        IEndToEndContext context,
        string groupName,
        params (string RecipeDefName, string ProductDefName, string GraphicOwner)[] specs)
    {
        FoodSearchE2EFixture.UseStrictNonEmergencyDining(context);
        var map = Current.Game.CurrentMap;
        var centers = FindRoomCenters(map, specs.Length);
        var cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        var cutleryDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery");
        var cases = new List<HiddenPasteCase>();

        for (var index = 0; index < specs.Length; index++)
        {
            var spec = specs[index];
            var center = centers[index];
            FoodSearchE2EFixture.BuildSealedRoom(map, center);

            var cook = GenerateCook(groupName + " paste cook " + (index + 1));
            FoodSearchE2EFixture.SetHunger(cook, 1f);
            GenSpawn.Spawn(cook, center + (IntVec3.South * 3), map);

            var stoveDef = DefDatabase<ThingDef>.GetNamed("FueledStove");
            var stove = ThingMaker.MakeThing(
                stoveDef,
                stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
            stove.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(stove, center, map, Rot4.North);
            stove.TryGetComp<CompRefuelable>()?.Refuel(999f);

            var recipe = DefDatabase<RecipeDef>.GetNamed(spec.RecipeDefName);
            var bill = new Bill_Production(recipe)
            {
                repeatMode = BillRepeatModeDefOf.RepeatCount,
                repeatCount = 1,
                ingredientSearchRadius = 8f
            };
            bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
            bill.SetPawnRestriction(cook);
            ((IBillGiver)stove).BillStack.AddBill(bill);

            var prepared = (ThingWithComps)ThingMaker.MakeThing(
                DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_PreparedFood"));
            prepared.stackCount = 10;
            prepared.GetComp<CompPreparedFood>()!.Initialize(new PreparedFoodState(
                new[] { new IngredientContribution("RawRice", 0.05f, 1, 50) },
                preparationQuality: 50,
                preparerThingId: null,
                DietaryFlags.Plant | DietaryFlags.VegetarianCompatible,
                exactSourcesHidden: true,
                ingredientPoisonChance: 0f));
            GenSpawn.Spawn(prepared, center + (IntVec3.East * 2), map);

            var cookware = FoodSearchE2EFixture.MakeCleanWare(
                "ImmersiveChefs_Cookware",
                ThingDefOf.Steel);
            var plate = FoodSearchE2EFixture.MakeCleanWare(
                "ImmersiveChefs_Plate",
                ThingDefOf.Steel);
            var cutlery = FoodSearchE2EFixture.MakeCleanWare(
                "ImmersiveChefs_Cutlery",
                ThingDefOf.Steel);
            GenSpawn.Spawn(cookware, center + (IntVec3.West * 3), map);
            GenSpawn.Spawn(plate, center + (IntVec3.West * 2), map);
            GenSpawn.Spawn(cutlery, center + (IntVec3.West * 1), map);

            EndToEndAssert.Equal(0, cook.workSettings.GetPriority(cooking),
                "Later hidden-paste cases must remain inert until activated.");
            cases.Add(new HiddenPasteCase(
                cook,
                stove,
                recipe,
                DefDatabase<ThingDef>.GetNamed(spec.ProductDefName),
                spec.GraphicOwner,
                prepared,
                cookware,
                plate,
                cutlery));
        }

        return new MealTextureHiddenPasteFixture(groupName, cases, plateDef, cutleryDef);
    }

    internal IEnumerable<EndToEndStep> Execute(IEndToEndContext context)
    {
        var cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        foreach (var pasteCase in cases)
        {
            yield return new TimeControlActionStep(
                "pause before hidden-paste bill for " + pasteCase.ProductDef.label,
                paused: true,
                EndToEndGameSpeed.Normal);
            yield return new SelectionActionStep(
                "select paste-derived prepared ingredients for " + pasteCase.ProductDef.label,
                new[] { pasteCase.Prepared.ThingID },
                additive: false);
            yield return new CameraActionStep(
                "frame paste-derived prepared ingredients for " + pasteCase.ProductDef.label,
                new[] { pasteCase.Prepared.ThingID },
                paddingPixels: 150);
            yield return new ScreenshotStep(
                "observe prepared ingredients expose nutrient paste rather than hidden rice",
                Array.Empty<string>(),
                paddingPixels: 0);

            pasteCase.Cook.workSettings.SetPriority(cooking, 1);
            yield return new TimeControlActionStep(
                "run native " + pasteCase.ProductDef.defName + " hidden-paste bill",
                paused: false,
                EndToEndGameSpeed.Normal);
            yield return new SelectionActionStep(
                "select hidden-paste cook and stove for " + pasteCase.ProductDef.label,
                new[] { pasteCase.Cook.ThingID, pasteCase.Stove.ThingID },
                additive: false);
            yield return new CameraActionStep(
                "frame hidden-paste kitchen for " + pasteCase.ProductDef.label,
                new[] { pasteCase.Cook.ThingID, pasteCase.Stove.ThingID },
                paddingPixels: 160);
            yield return new WaitUntilStep(
                "cook enters native DoBill for hidden " + pasteCase.ProductDef.defName,
                _ => pasteCase.ObserveNativeBill(),
                new EndToEndDeadline(900, 3_000, TimeSpan.FromSeconds(30)));
            yield return new ScreenshotStep(
                "observe native cooking with hidden prepared paste for " + pasteCase.ProductDef.label,
                new[] { pasteCase.Cook.ThingID, pasteCase.Stove.ThingID },
                paddingPixels: 160);
            yield return new TimeControlActionStep(
                "finish native hidden-paste cooking for " + pasteCase.ProductDef.label,
                paused: false,
                EndToEndGameSpeed.Superfast);
            yield return new WaitUntilStep(
                "hidden-paste " + pasteCase.ProductDef.defName + " is plated",
                _ => pasteCase.TryResolveProduct(),
                new EndToEndDeadline(1_800, 8_000, TimeSpan.FromSeconds(60)));
            yield return new TimeControlActionStep(
                "pause after hidden-paste cooking for " + pasteCase.ProductDef.label,
                paused: true,
                EndToEndGameSpeed.Normal);
            yield return new AssertionStep(
                "hidden-paste provenance and exact plate remain separated",
                _ => AssertCookedState(pasteCase));
            yield return new SelectionActionStep(
                "select plated hidden-paste " + pasteCase.ProductDef.label,
                new[] { pasteCase.Product!.ThingID, pasteCase.Cook.ThingID },
                additive: false);
            yield return new CameraActionStep(
                "frame plated hidden-paste " + pasteCase.ProductDef.label,
                new[] { pasteCase.Product!.ThingID, pasteCase.Cook.ThingID },
                paddingPixels: 150);
            yield return new SelectionActionStep(
                "select only plated hidden-paste " + pasteCase.ProductDef.label + " for inspection",
                new[] { pasteCase.Product!.ThingID },
                additive: false);
            yield return new ScreenshotStep(
                "observe cooked hidden-paste " + pasteCase.ProductDef.label,
                Array.Empty<string>(),
                paddingPixels: 0);

            pasteCase.Cook.workSettings.SetPriority(cooking, 0);
            FoodSearchE2EFixture.SetHunger(pasteCase.Cook, 0.05f);
            var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
                .Query(pasteCase.Cook.ThingID, pasteCase.Product!.ThingID);
            var consume = options.Where(option =>
                    !option.Disabled &&
                    option.Label.IndexOf("consume", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            EndToEndAssert.Equal(1, consume.Length,
                "Expected one enabled native Consume option for hidden-paste " +
                pasteCase.ProductDef.defName + "; observed " +
                string.Join(", ", options.Select(option =>
                    $"'{option.Label}' (disabled={option.Disabled})")));
            yield return new FloatMenuActionStep(
                "order native consumption of hidden-paste " + pasteCase.ProductDef.label,
                pasteCase.Cook.ThingID,
                pasteCase.Product!.ThingID,
                consume[0].StableId);
            yield return new TimeControlActionStep(
                "run native hidden-paste ingestion",
                paused: false,
                EndToEndGameSpeed.Normal);
            yield return new WaitUntilStep(
                "diner enters native ingest job for hidden " + pasteCase.ProductDef.defName,
                _ => !pasteCase.Product!.Destroyed &&
                     pasteCase.Cook.CurJobDef == JobDefOf.Ingest,
                new EndToEndDeadline(900, 3_000, TimeSpan.FromSeconds(30)));
            yield return new ScreenshotStep(
                "observe native hidden-paste ingestion for " + pasteCase.ProductDef.label,
                new[] { pasteCase.Cook.ThingID },
                paddingPixels: 160);
            yield return new TimeControlActionStep(
                "finish native hidden-paste ingestion",
                paused: false,
                EndToEndGameSpeed.Superfast);
            yield return new WaitUntilStep(
                "hidden-paste meal returns its exact dirty setting",
                _ => pasteCase.Product!.Destroyed &&
                     pasteCase.Plate.Spawned &&
                     pasteCase.Cutlery.Spawned,
                new EndToEndDeadline(1_800, 8_000, TimeSpan.FromSeconds(60)));
            yield return new TimeControlActionStep(
                "pause after hidden-paste ingestion",
                paused: true,
                EndToEndGameSpeed.Normal);
            yield return new AssertionStep(
                "hidden-paste ingestion conserves the exact dirty setting once",
                _ => AssertConsumedState(pasteCase));
            yield return new SelectionActionStep(
                "select returned hidden-paste plate",
                new[] { pasteCase.Plate.ThingID },
                additive: false);
            yield return new CameraActionStep(
                "frame returned hidden-paste setting",
                new[] { pasteCase.Plate.ThingID, pasteCase.Cutlery.ThingID },
                paddingPixels: 150);
            yield return new ScreenshotStep(
                "observe exact dirty plate after hidden-paste dining",
                Array.Empty<string>(),
                paddingPixels: 0);
            yield return new SelectionActionStep(
                "select returned hidden-paste cutlery",
                new[] { pasteCase.Cutlery.ThingID },
                additive: false);
            yield return new ScreenshotStep(
                "observe exact dirty cutlery after hidden-paste dining",
                Array.Empty<string>(),
                paddingPixels: 0);
        }

        yield return new CheckpointStep(
            groupName + " hidden-paste lifecycle result",
            _ => cases.ToDictionary(
                pasteCase => pasteCase.ProductDef.defName,
                pasteCase =>
                    "nativeBill=" + pasteCase.NativeBillObserved +
                    "; destroyed=" + pasteCase.Product!.Destroyed +
                    "; graphicOwner=" + pasteCase.GraphicOwner +
                    "; plateId=" + pasteCase.Plate.ThingID +
                    "; plateDirty=" + pasteCase.Plate.GetComp<CompSanitation>()!.IsDirty +
                    "; cutleryDirty=" + pasteCase.Cutlery.GetComp<CompSanitation>()!.IsDirty));
    }

    private void AssertCookedState(HiddenPasteCase pasteCase)
    {
        var product = pasteCase.Product!;
        EndToEndAssert.True(pasteCase.NativeBillObserved,
            pasteCase.ProductDef.defName + " must be made through RimWorld's ordinary DoBill job.");
        EndToEndAssert.True(pasteCase.Prepared.Destroyed,
            "The actual hidden prepared-paste ingredient must be consumed by the bill.");
        EndToEndAssert.True(pasteCase.Cookware.GetComp<CompSanitation>()!.IsDirty,
            "Native cooking must leave the exact cookware dirty at the stove.");

        var publicIngredients = product.GetComp<CompIngredients>()!.ingredients
            .Select(def => def.defName)
            .ToArray();
        EndToEndAssert.Equal(
            "MealNutrientPaste",
            string.Join("|", publicIngredients),
            "Texture-facing provenance must expose paste, never its hidden raw source.");
        var serving = product.GetComp<CompCulinaryState>()!
            .PeekCurrentServingWithoutThermalUpdate()!;
        EndToEndAssert.Equal(
            "RawRice",
            string.Join("|", serving.HiddenSourceDefNames),
            "The culinary state must retain the exact hidden source separately.");

        var graphic = product.Graphic;
        EndToEndAssert.NotNull(graphic.MatSingleFor(product),
            product.def.defName + " must resolve its real material-selection seam.");
        EndToEndAssert.Equal(pasteCase.GraphicOwner, graphic.GetType().FullName,
            product.def.defName + " must retain the finalized texture owner.");
        EndToEndAssert.Equal(
            "MealNutrientPaste",
            string.Join("|", product.GetComp<CompIngredients>()!.ingredients.Select(def => def.defName)),
            "Material selection must not reveal or rewrite hidden rice provenance.");
        EndToEndAssert.Equal(
            "RawRice",
            string.Join("|", serving.HiddenSourceDefNames),
            "Material selection must leave the hidden provenance exact.");

        var embedded = product.GetComp<CompEmbeddedWare>()!;
        EndToEndAssert.Equal(1, embedded.EmbeddedPlateCount,
            "The cooked serving must contain exactly one physical plate.");
        EndToEndAssert.True(ReferenceEquals(pasteCase.Plate, embedded.PeekPlateThing()),
            "The cooked serving must contain the exact reserved plate Thing.");
    }

    private void AssertConsumedState(HiddenPasteCase pasteCase)
    {
        EndToEndAssert.True(pasteCase.Product!.Destroyed,
            "The exact hidden-paste meal must be consumed.");
        EndToEndAssert.True(!pasteCase.Plate.Destroyed && pasteCase.Plate.Spawned,
            "The exact embedded plate must return to the map after ingestion.");
        EndToEndAssert.True(!pasteCase.Cutlery.Destroyed && pasteCase.Cutlery.Spawned,
            "The exact acquired cutlery must return to the map after ingestion.");
        EndToEndAssert.Equal(1, pasteCase.Plate.stackCount,
            "The exact returned plate Thing must remain one physical plate.");
        EndToEndAssert.Equal(1, pasteCase.Cutlery.stackCount,
            "The exact returned cutlery Thing must remain one physical set.");
        EndToEndAssert.True(pasteCase.Plate.GetComp<CompSanitation>()!.IsDirty,
            "The returned plate must be visibly dirty in state.");
        EndToEndAssert.True(pasteCase.Cutlery.GetComp<CompSanitation>()!.IsDirty,
            "The returned cutlery must be visibly dirty in state.");
        EndToEndAssert.Equal(cases.Count, CountAllServiceWare(Current.Game.CurrentMap, plateDef),
            "Native cooking and ingestion must neither duplicate nor lose any arranged plate.");
        EndToEndAssert.Equal(cases.Count, CountAllServiceWare(Current.Game.CurrentMap, cutleryDef),
            "Native dining must neither duplicate nor lose any arranged cutlery set.");
    }

    private int CountAllServiceWare(Map map, ThingDef def)
    {
        var spawned = map.listerThings.ThingsOfDef(def).Sum(thing => thing.stackCount);
        var carriedOrInventoried = cases.Sum(pasteCase =>
            (pasteCase.Cook.inventory?.innerContainer
                 .Where(thing => thing.def == def)
                 .Sum(thing => thing.stackCount) ?? 0) +
            (pasteCase.Cook.carryTracker?.CarriedThing is { } carried && carried.def == def
                ? carried.stackCount
                : 0));
        var embedded = cases.Sum(pasteCase =>
            pasteCase.Product is { Destroyed: false }
                ? pasteCase.Product.GetComp<CompEmbeddedWare>()?.EmbeddedPlateCount ?? 0
                : 0);
        return spawned + carriedOrInventoried + (def == plateDef ? embedded : 0);
    }

    private static Pawn GenerateCook(string name)
    {
        var cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        for (var attempt = 0; attempt < 64; attempt++)
        {
            var pawn = FoodSearchE2EFixture.CreateColonist(name);
            if (!pawn.WorkTypeIsDisabled(cooking) &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) >= 0.9f &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) >= 0.9f)
            {
                pawn.skills.GetSkill(SkillDefOf.Cooking).Level = 20;
                return pawn;
            }

            pawn.Destroy(DestroyMode.Vanish);
        }

        throw new EndToEndAssertionException("Could not generate a capable hidden-paste cook.");
    }

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
            "Could not find enough separated hidden-paste fixture rooms.");
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

    private sealed class HiddenPasteCase
    {
        internal HiddenPasteCase(
            Pawn cook,
            Thing stove,
            RecipeDef recipe,
            ThingDef productDef,
            string graphicOwner,
            ThingWithComps prepared,
            ThingWithComps cookware,
            ThingWithComps plate,
            ThingWithComps cutlery)
        {
            Cook = cook;
            Stove = stove;
            Recipe = recipe;
            ProductDef = productDef;
            GraphicOwner = graphicOwner;
            Prepared = prepared;
            Cookware = cookware;
            Plate = plate;
            Cutlery = cutlery;
        }

        internal Pawn Cook { get; }
        internal Thing Stove { get; }
        internal RecipeDef Recipe { get; }
        internal ThingDef ProductDef { get; }
        internal string GraphicOwner { get; }
        internal ThingWithComps Prepared { get; }
        internal ThingWithComps Cookware { get; }
        internal ThingWithComps Plate { get; }
        internal ThingWithComps Cutlery { get; }
        internal ThingWithComps? Product { get; private set; }
        internal bool NativeBillObserved { get; private set; }

        internal bool ObserveNativeBill()
        {
            NativeBillObserved |= Cook.CurJobDef == JobDefOf.DoBill &&
                                  Cook.CurJob?.RecipeDef == Recipe;
            return NativeBillObserved;
        }

        internal bool TryResolveProduct()
        {
            Product ??= Cook.MapHeld?.listerThings.ThingsOfDef(ProductDef)
                .OfType<ThingWithComps>()
                .FirstOrDefault(candidate =>
                    candidate.Spawned &&
                    candidate.GetComp<CompEmbeddedWare>()?.EmbeddedPlateCount == 1);
            return Product is not null;
        }
    }
}
