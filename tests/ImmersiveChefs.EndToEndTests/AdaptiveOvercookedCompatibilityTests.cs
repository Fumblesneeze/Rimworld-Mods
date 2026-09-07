using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.adaptive-overcooked-final-product",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "OskarPotocki.VanillaFactionsExpanded.Core",
    "VanillaExpanded.VCookE",
    "VanillaExpanded.VCookEBakery",
    "VanillaExpanded.VCookEHaute",
    "VanillaExpanded.VCookEStews",
    "VanillaExpanded.VCEF",
    "VanillaExpanded.VCookESushi",
    "ucp.friedmeals",
    "rabiosus.AdaptiveMealBill",
    "binchcannon.overcookedmeals",
    "fumblesneeze.immersivechefs",
    MaxFrames = 12_000,
    MaxGameTicks = 80_000,
    MaxWallClockSeconds = 240)]
public sealed class AdaptiveOvercookedCompatibilityTest : IRimWorldEndToEndTest
{
    private const int MaximumAttempts = 200;
    private const float TestRecipeWorkAmount = 100f;

    private readonly HashSet<Thing> issuedPlates = new();
    private Map map = null!;
    private IntVec3 center;
    private Pawn cook = null!;
    private ThingWithComps stove = null!;
    private ThingWithComps cookware = null!;
    private ThingWithComps rejectedWoodPlate = null!;
    private RecipeDef adaptiveRecipe = null!;
    private RecipeDef concreteRecipe = null!;
    private ThingDef ingredientDef = null!;
    private ThingDef overcookedDef = null!;
    private StatDef foodPoisonChance = null!;
    private ThingWithComps? finalProduct;
    private ThingWithComps? currentProduct;
    private Job? priorAttemptJob;
    private int attempts;
    private int adaptiveWaitStartedTick = -1;
    private int productionWaitStartedTick = -1;
    private bool nativeAdaptiveJobObserved;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        center = FindRoomCenter(map);
        BuildSealedRoom(map, center);

        adaptiveRecipe = DefDatabase<RecipeDef>.GetNamed("CookMealFine_Adaptive");
        concreteRecipe = DefDatabase<RecipeDef>.GetNamed("CookMealFine_Veg");
        overcookedDef = DefDatabase<ThingDef>.GetNamed("OvercookedMeals_MealOvercooked");
        ingredientDef = DefDatabase<ThingDef>.GetNamed("RawRice");
        CreateIngredientStockpile();
        cook = GenerateCook();

        var originalWorkAmount = adaptiveRecipe.workAmount;
        var priorRequirementMode = ImmersiveChefsMod.Settings.WareRequirementMode;
        var priorDirtyFallback = ImmersiveChefsMod.Settings.DirtyWareFallback;
        var priorAutoAssistants = ImmersiveChefsMod.Settings.AutoCallAssistants;
        foodPoisonChance = StatDefOf.FoodPoisonChance;
        var priorFoodPoisonBase = foodPoisonChance.defaultBaseValue;
        var priorFoodPoisonParts = foodPoisonChance.parts;
        var priorFoodPoisonSkillFactors = foodPoisonChance.skillNeedFactors;
        context.DeferCleanup(() =>
        {
            adaptiveRecipe.workAmount = originalWorkAmount;
            ImmersiveChefsMod.Settings.WareRequirementMode = priorRequirementMode;
            ImmersiveChefsMod.Settings.DirtyWareFallback = priorDirtyFallback;
            ImmersiveChefsMod.Settings.AutoCallAssistants = priorAutoAssistants;
            foodPoisonChance.defaultBaseValue = priorFoodPoisonBase;
            foodPoisonChance.parts = priorFoodPoisonParts;
            foodPoisonChance.skillNeedFactors = priorFoodPoisonSkillFactors;
            foodPoisonChance.Worker.TryClearCache();
        });
        adaptiveRecipe.workAmount = TestRecipeWorkAmount;
        ImmersiveChefsMod.Settings.WareRequirementMode = WareRequirementMode.Strict;
        ImmersiveChefsMod.Settings.DirtyWareFallback = DirtyWareFallback.Always;
        ImmersiveChefsMod.Settings.AutoCallAssistants = false;
        foodPoisonChance.defaultBaseValue = 1f;
        foodPoisonChance.parts = new List<StatPart>();
        foodPoisonChance.skillNeedFactors = new List<SkillNeed>();
        foodPoisonChance.Worker.ClearCacheForThing(cook);

        HumanlikePawnFixture.SetName(cook, "Adaptive Overcooked Chef");
        GenSpawn.Spawn(cook, center + (IntVec3.South * 3), map);
        SatisfyCookHunger();

        var stoveDef = DefDatabase<ThingDef>.GetNamed("FueledStove");
        stove = (ThingWithComps)ThingMaker.MakeThing(
            stoveDef,
            stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
        stove.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(stove, center, map, Rot4.North);
        stove.GetComp<CompRefuelable>()?.Refuel(999f);

        var bill = new Bill_Production(adaptiveRecipe)
        {
            repeatMode = BillRepeatModeDefOf.RepeatCount,
            repeatCount = 1,
            ingredientSearchRadius = 8f
        };
        bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        bill.SetPawnRestriction(cook);
        ((IBillGiver)stove).BillStack.AddBill(bill);

        cookware = MakeCleanWare("ImmersiveChefs_Cookware", ThingDefOf.Steel);
        GenSpawn.Spawn(cookware, center + (IntVec3.West * 3), map);
        rejectedWoodPlate = MakeCleanWare("ImmersiveChefs_Plate", ThingDefOf.WoodLog);
        GenSpawn.Spawn(rejectedWoodPlate, center + (IntVec3.North * 3), map);
        IssueCleanSteelPlate();
        EnsureIngredientSupply();
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var initialTargets = new[]
        {
            cook.ThingID,
            stove.ThingID,
            cookware.ThingID,
            rejectedWoodPlate.ThingID
        };
        yield return new SelectionActionStep(
            "select the adaptive overcooking fixture",
            initialTargets,
            additive: false);
        yield return new CameraActionStep(
            "frame the adaptive overcooking fixture",
            initialTargets,
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "before ordinary adaptive Fine cooking",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new TimeControlActionStep(
            "run ordinary adaptive Cooking work",
            paused: false,
            EndToEndGameSpeed.Superfast);

        for (var attempt = 1; attempt <= MaximumAttempts; attempt++)
        {
            yield return new WaitUntilStep(
                "adaptive Fine bill begins attempt " + attempt,
                _ => ObserveAdaptiveBill(),
                new EndToEndDeadline(300, 2_000, TimeSpan.FromSeconds(20)));
            yield return new WaitUntilStep(
                "adaptive Fine recipe work begins attempt " + attempt,
                _ => ObserveRecipeWorkStarted(),
                new EndToEndDeadline(300, 2_000, TimeSpan.FromSeconds(20)));
            PrepareDeterministicPoisonAttempt(attempt);

            if (attempt == 1)
            {
                yield return new ScreenshotStep(
                    "ordinary adaptive Fine cooking in progress",
                    new[] { cook.ThingID, stove.ThingID },
                    paddingPixels: 220);
            }

            yield return new WaitUntilStep(
                "adaptive Fine attempt produces a native final product " + attempt,
                _ => TryResolveProducedMeal(),
                new EndToEndDeadline(600, 4_000, TimeSpan.FromSeconds(30)));
            yield return new WaitUntilStep(
                "the completed native bill returns its reusable cookware before inspection",
                _ => cook.CurJobDef != JobDefOf.DoBill && cookware.Spawned && !cookware.Destroyed,
                new EndToEndDeadline(600, 2_000, TimeSpan.FromSeconds(30)));
            if (currentProduct?.def == overcookedDef)
            {
                finalProduct = currentProduct;
                break;
            }

            PrepareNextAttempt();
        }

        yield return new TimeControlActionStep(
            "pause after the native overcooked replacement survives",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "adaptive selection and overcooked replacement finalize one surviving meal",
            _ => AssertFinalProduct());
        yield return new SelectionActionStep(
            "select the surviving overcooked meal",
            new[] { finalProduct!.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the surviving overcooked meal and returned cookware",
            new[]
            {
                finalProduct!.ThingID,
                cookware.ThingID,
                cook.ThingID,
                stove.ThingID,
                rejectedWoodPlate.ThingID
            },
            paddingPixels: 260);
        yield return new ScreenshotStep(
            "observe the adaptive overcooked survivor with one bound plate",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new SelectionActionStep(
            "select cookware dirtied by the completed native job",
            new[] { cookware.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "observe the one returned dirty cookware set",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "adaptive overcooked final-product result",
            _ => new Dictionary<string, string>
            {
                ["attempts"] = attempts.ToString(),
                ["adaptiveJobObserved"] = nativeAdaptiveJobObserved.ToString(),
                ["wrapperRecipe"] = adaptiveRecipe.defName,
                ["selectedConcreteRecipe"] = concreteRecipe.defName,
                ["concreteIngredient"] = ingredientDef.defName,
                ["survivor"] = finalProduct!.def.defName,
                ["survivorThingId"] = finalProduct.ThingID,
                ["plateThingId"] = finalProduct.GetComp<CompEmbeddedWare>()?.PeekPlateThing()?.ThingID ?? "missing",
                ["quality"] = finalProduct.GetComp<CompCulinaryState>()?.Servings.Single().QualityScore.ToString() ?? "missing",
                ["cookwareDirty"] = (cookware.GetComp<CompSanitation>()?.IsDirty == true).ToString(),
                ["rejectedWoodPlate"] = rejectedWoodPlate.Spawned ? "spawned" : "missing"
            });
    }

    private bool ObserveAdaptiveBill()
    {
        if (adaptiveWaitStartedTick < 0)
        {
            adaptiveWaitStartedTick = Find.TickManager.TicksGame;
        }

        var observed = cook.CurJobDef == JobDefOf.DoBill &&
                       cook.CurJob?.RecipeDef == adaptiveRecipe &&
                       !ReferenceEquals(cook.CurJob, priorAttemptJob);
        nativeAdaptiveJobObserved |= observed;
        if (observed)
        {
            adaptiveWaitStartedTick = -1;
            return true;
        }

        if (Find.TickManager.TicksGame - adaptiveWaitStartedTick < 1_000)
        {
            return false;
        }

        JobFailReason.Clear();
        var cookingWorkGiver = DefDatabase<WorkGiverDef>
            .GetNamed("DoBillsCook")
            .Worker as WorkGiver_DoBill;
        var diagnosticJob = cookingWorkGiver?.JobOnThing(cook, stove);
        var failureReason = JobFailReason.HaveReason ? JobFailReason.Reason : "none";
        var bill = ((IBillGiver)stove).BillStack.Bills.First();
        var ingredient = map.listerThings.ThingsOfDef(ingredientDef)
            .FirstOrDefault(thing => thing.Spawned);
        throw new EndToEndAssertionException(
            "The automatic Cooking work scan did not begin the adaptive bill after 1,000 ticks " +
            $"(ingredient={ingredientDef.defName}; spawned={ingredient?.stackCount ?? 0}; " +
            $"resourceCount={map.resourceCounter.GetCount(ingredientDef)}; " +
            $"billAllows={bill.ingredientFilter.Allows(ingredientDef)}; " +
            $"fixedAllows={concreteRecipe.fixedIngredientFilter?.Allows(ingredientDef)}; " +
            $"slotAllows={concreteRecipe.ingredients.All(slot => slot.filter.Allows(ingredientDef))}; " +
            $"reachable={ingredient is not null && cook.CanReserveAndReach(ingredient, PathEndMode.Touch, Danger.Some)}; " +
            $"nextSearch={bill.nextTickToSearchForIngredients}; tick={Find.TickManager.TicksGame}; " +
            $"stoveUsable={((IBillGiver)stove).UsableForBillsAfterFueling()}; " +
            $"workGiverResult={diagnosticJob?.def?.defName ?? "none"}; reason={failureReason})."
        );
    }

    private bool ObserveRecipeWorkStarted()
    {
        if (cook.CurJobDef != JobDefOf.DoBill ||
            cook.CurJob?.RecipeDef != adaptiveRecipe ||
            cook.jobs.curDriver is not JobDriver_DoBill driver)
        {
            return false;
        }

        if (!AdaptiveMealBillAdapter.TryResolveConcreteRecipe(cook.CurJob, out var selectedRecipe) ||
            selectedRecipe is null ||
            !MealCoveragePolicy.IsCovered(selectedRecipe))
        {
            return false;
        }

        concreteRecipe = selectedRecipe;

        var workLeft = driver.workLeft;
        return cook.Position == stove.InteractionCell &&
               workLeft > 0f &&
               workLeft < TestRecipeWorkAmount;
    }

    private bool TryResolveProducedMeal()
    {
        if (productionWaitStartedTick < 0)
        {
            productionWaitStartedTick = Find.TickManager.TicksGame;
        }

        currentProduct = map.listerThings.AllThings
            .OfType<ThingWithComps>()
            .FirstOrDefault(thing =>
                thing.Spawned &&
                (thing.def == overcookedDef ||
                 concreteRecipe.products.Any(product => product.thingDef == thing.def)) &&
                thing.GetComp<CompCulinaryState>()?.Servings.Count > 0);
        if (currentProduct is not null)
        {
            productionWaitStartedTick = -1;
            return true;
        }

        if (Find.TickManager.TicksGame - productionWaitStartedTick < 1_000)
        {
            return false;
        }

        var products = map.listerThings.AllThings
            .Where(thing => thing.Spawned &&
                            (thing.def == overcookedDef ||
                             concreteRecipe.products.Any(product => product.thingDef == thing.def)))
            .Select(thing => thing.def.defName + ":" +
                             ((thing as ThingWithComps)?.GetComp<CompCulinaryState>()?.Servings.Count ?? -1))
            .ToArray();
        throw new EndToEndAssertionException(
            "The accepted adaptive bill produced no finalized meal after 1,000 ticks " +
            $"(job={cook.CurJobDef?.defName ?? "none"}; skill={cook.skills.GetSkill(SkillDefOf.Cooking).Level}; " +
            $"products={string.Join(",", products)}; cookwareSpawned={cookware.Spawned}; " +
            $"cookwareHolder={cookware.holdingOwner?.GetType().Name ?? "none"}; " +
            $"cookwareDirty={cookware.GetComp<CompSanitation>()?.IsDirty}).");
    }

    private void PrepareNextAttempt()
    {
        EndToEndAssert.NotNull(currentProduct,
            "A completed non-overcooked product must exist before the next attempt is arranged.");
        priorAttemptJob = cook.CurJob;
        currentProduct!.Destroy(DestroyMode.Vanish);
        foreach (var plate in map.listerThings.ThingsOfDef(
                     DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate")).ToList())
        {
            if (!ReferenceEquals(plate, rejectedWoodPlate))
            {
                plate.Destroy(DestroyMode.Vanish);
            }
        }

        currentProduct = null;
        cook.skills.GetSkill(SkillDefOf.Cooking).Level = 6;
        SatisfyCookHunger();
        IssueCleanSteelPlate();
        EnsureIngredientSupply();
        ((Bill_Production)((IBillGiver)stove).BillStack.Bills.First()).repeatCount = 1;
    }

    private void SatisfyCookHunger()
    {
        if (cook.needs?.food is { } food)
        {
            food.CurLevel = food.MaxLevel;
        }
    }

    private void AssertFinalProduct()
    {
        EndToEndAssert.NotNull(
            finalProduct,
            $"An ordinary skill-zero adaptive Fine bill must produce an Overcooked Meals survivor within {MaximumAttempts} attempts.");
        EndToEndAssert.True(
            nativeAdaptiveJobObserved,
            "The cook must visibly enter Adaptive Meal Bill's native wrapper job.");
        EndToEndAssert.Equal(
            overcookedDef,
            finalProduct!.def,
            "Overcooked Meals must remain the final replacement owner.");
        var embedded = finalProduct.GetComp<CompEmbeddedWare>();
        var plate = embedded?.PeekPlateThing();
        EndToEndAssert.NotNull(embedded,
            "The surviving overcooked meal must receive finalized embedded ware.");
        EndToEndAssert.Equal(1, embedded!.EmbeddedPlateCount,
            "The surviving overcooked meal must bind exactly one physical plate.");
        EndToEndAssert.NotNull(plate,
            "The surviving overcooked meal must retain the reserved physical plate Thing.");
        EndToEndAssert.True(issuedPlates.Contains(plate!),
            "The survivor must hold one of the exact steel plates issued before its native job began.");
        EndToEndAssert.Equal(ThingDefOf.Steel, plate!.Stuff,
            "Adaptive Fine selection must apply the Advanced plate-material tier.");
        EndToEndAssert.True(rejectedWoodPlate.Spawned && !rejectedWoodPlate.Destroyed,
            "The ineligible wood plate must remain untouched for the Adaptive Fine job.");
        EndToEndAssert.False(ReferenceEquals(plate, rejectedWoodPlate),
            "The Adaptive wrapper must not bypass its selected Fine recipe's plate tier.");

        var culinary = finalProduct.GetComp<CompCulinaryState>();
        EndToEndAssert.NotNull(culinary,
            "The final surviving meal must receive culinary state.");
        EndToEndAssert.Equal(1, culinary!.Servings.Count,
            "The final surviving one-serving meal must receive one state record exactly once.");
        var cookwareScore = cookware.GetComp<CompKitchenwareStats>()?.CurrentStats.CulinaryToolScore ?? 0;
        var unpenalized = CulinaryQualityCalculator.Calculate(new CulinaryQualityInputs(
            leadSkill: 30f,
            ingredientDiversity: 20f,
            ingredientCraftsmanship: 50f,
            preparationQuality: 50f,
            cookware: cookwareScore,
            knife: 0f,
            assistants: 0f));
        var expectedQuality = Math.Max(0, unpenalized - 35);
        EndToEndAssert.Equal(expectedQuality, culinary.Servings.Single().QualityScore,
            "The final survivor must receive the severe 35-point penalty once, not zero or twice.");
        EndToEndAssert.True(culinary.Servings.Single().LastThermalTick > 0,
            "The final survivor must receive one active Immersive Chefs temperature state.");

        var ingredients = finalProduct.GetComp<CompIngredients>();
        EndToEndAssert.True(ingredients?.ingredients.Contains(ingredientDef) == true,
            "Overcooked Meals must copy the concrete adaptive ingredient provenance.");
        EndToEndAssert.Equal(
            0f,
            finalProduct.GetComp<CompFoodPoisonable>()?.PoisonPercent ?? -1f,
            "The upstream overcooked replacement must retain its native non-poisoned result.");
        EndToEndAssert.True(cookware.Spawned && !cookware.Destroyed,
            "The same reusable cookware set must return to the map after the completed job.");
        EndToEndAssert.True(cookware.GetComp<CompSanitation>()?.IsDirty == true,
            "The completed native cooking job must dirty the cookware once.");
    }

    private void PrepareDeterministicPoisonAttempt(int attempt)
    {
        EndToEndAssert.True(
            cook.CurJobDef == JobDefOf.DoBill &&
            cook.CurJob?.RecipeDef == adaptiveRecipe,
            "The native adaptive bill must still be active after recipe work begins.");
        foodPoisonChance.defaultBaseValue = 1f;
        foodPoisonChance.parts = new List<StatPart>();
        foodPoisonChance.skillNeedFactors = new List<SkillNeed>();
        foodPoisonChance.Worker.ClearCacheForThing(cook);
        SatisfyCookHunger();
        EndToEndAssert.Equal(
            1f,
            cook.GetStatValue(StatDefOf.FoodPoisonChance),
            "The test-only vanilla incompetent-cook poison chance must be deterministic.");
        attempts = attempt;
    }

    private void IssueCleanSteelPlate()
    {
        var plate = MakeCleanWare("ImmersiveChefs_Plate", ThingDefOf.Steel);
        issuedPlates.Add(plate);
        GenSpawn.Spawn(plate, center + (IntVec3.West * 2), map);
    }

    private void EnsureIngredientSupply()
    {
        var available = map.listerThings.ThingsOfDef(ingredientDef)
            .Where(thing => thing.Spawned)
            .Sum(thing => thing.stackCount);
        if (available >= 30)
        {
            return;
        }

        var ingredient = ThingMaker.MakeThing(ingredientDef);
        ingredient.stackCount = Math.Min(ingredientDef.stackLimit, 75);
        GenPlace.TryPlaceThing(
            ingredient,
            center + (IntVec3.East * 2),
            map,
            ThingPlaceMode.Near);
        map.resourceCounter.UpdateResourceCounts();
    }

    private void CreateIngredientStockpile()
    {
        var zone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
        map.zoneManager.RegisterZone(zone);
        for (var x = 1; x <= 5; x++)
        {
            for (var z = -2; z <= 2; z++)
            {
                zone.AddCell(center + new IntVec3(x, 0, z));
            }
        }

        zone.settings.Priority = StoragePriority.Critical;
        zone.settings.filter.SetDisallowAll();
        zone.settings.filter.SetAllow(ingredientDef, allow: true);
    }

    private static ThingWithComps MakeCleanWare(string defName, ThingDef stuff)
    {
        var ware = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed(defName),
            stuff);
        ware.GetComp<CompQuality>()?.SetQuality(
            QualityCategory.Normal,
            ArtGenerationContext.Colony);
        ware.GetComp<CompSanitation>()?.MarkClean(WashProvenance.Safe);
        return ware;
    }

    private static Pawn GenerateCook()
    {
        var cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        for (var attempt = 0; attempt < 64; attempt++)
        {
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                forceNoGear: true));
            if (pawn.WorkTypeIsDisabled(cooking) ||
                !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Moving) ||
                !pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                pawn.Destroy(DestroyMode.Vanish);
                continue;
            }

            pawn.inventory?.innerContainer.ClearAndDestroyContents();
            pawn.workSettings.EnableAndInitialize();
            foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (!pawn.WorkTypeIsDisabled(workType))
                {
                    pawn.workSettings.SetPriority(workType, 0);
                }
            }

            pawn.workSettings.SetPriority(cooking, 1);
            pawn.skills.GetSkill(SkillDefOf.Cooking).Level = 6;
            for (var hour = 0; hour < 24; hour++)
            {
                pawn.timetable?.SetAssignment(hour, TimeAssignmentDefOf.Work);
            }

            if (pawn.needs?.food is { } food)
            {
                food.CurLevelPercentage = 1f;
            }

            if (pawn.needs?.rest is { } rest)
            {
                rest.CurLevelPercentage = 1f;
            }

            return pawn;
        }

        throw new EndToEndAssertionException("Could not generate an Adaptive Meal Bill-capable cook.");
    }

    private static void BuildSealedRoom(Map map, IntVec3 center)
    {
        var granite = DefDatabase<ThingDef>.GetNamed("BlocksGranite");
        for (var x = -6; x <= 6; x++)
        {
            for (var z = -6; z <= 6; z++)
            {
                map.areaManager.Home[center + new IntVec3(x, 0, z)] = true;
            }
        }

        for (var offset = -6; offset <= 6; offset++)
        {
            SpawnWall(map, center + new IntVec3(offset, 0, -6), granite);
            SpawnWall(map, center + new IntVec3(offset, 0, 6), granite);
            if (offset is -6 or 6)
            {
                continue;
            }

            SpawnWall(map, center + new IntVec3(-6, 0, offset), granite);
            SpawnWall(map, center + new IntVec3(6, 0, offset), granite);
        }
    }

    private static void SpawnWall(Map map, IntVec3 cell, ThingDef stuff)
    {
        var wall = ThingMaker.MakeThing(ThingDefOf.Wall, stuff);
        wall.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(wall, cell, map);
    }

    private static IntVec3 FindRoomCenter(Map map)
    {
        for (var x = -72; x <= 72; x += 18)
        {
            for (var z = -72; z <= 72; z += 18)
            {
                var candidate = map.Center + new IntVec3(x, 0, z);
                if (SquareIsUsable(map, candidate, 7))
                {
                    return candidate;
                }
            }
        }

        throw new EndToEndAssertionException("Could not find a clean adaptive overcooking fixture area.");
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
                    map.roofGrid.Roofed(cell) ||
                    cell.GetThingList(map).Count != 0)
                {
                    return false;
                }
            }
        }

        return true;
    }
}
