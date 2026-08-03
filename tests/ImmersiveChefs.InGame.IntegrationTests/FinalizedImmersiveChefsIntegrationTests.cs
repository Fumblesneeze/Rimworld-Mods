using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using RimWorldDevGateway.IntegrationTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.InGame.IntegrationTests;

public static class FinalizedImmersiveChefsIntegrationTests
{
    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ImmersiveChefsModInitializedFromTheRealActiveSet()
    {
        IntegrationAssert.True(
            LoadedModManager.RunningModsListForReading.Any(
                mod => string.Equals(
                    mod.PackageId,
                    ImmersiveChefsMod.PackageId,
                    StringComparison.OrdinalIgnoreCase)),
            "Immersive Chefs must be an actually loaded ModContentPack, not merely a referenced assembly.");
        IntegrationAssert.NotNull(
            ImmersiveChefsMod.Integrations,
            "The real Immersive Chefs Mod constructor must initialize its optional-integration snapshot.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void GameplayDefsAreFinalizedAndResearchGated()
    {
        var expectedThings = new[]
        {
            "ImmersiveChefs_Cookware", "ImmersiveChefs_Plate", "ImmersiveChefs_Silverware",
            "ImmersiveChefs_ChefsKnife", "ImmersiveChefs_Dishwasher",
            "ImmersiveChefs_IndustrialDishwasher", "ImmersiveChefs_PreparedFood",
            "ImmersiveChefs_PrepStation", "ImmersiveChefs_SauceStation",
            "ImmersiveChefs_MeatStation", "ImmersiveChefs_VegetableStation",
            "ImmersiveChefs_PastryStation", "ImmersiveChefs_Microwave"
        };
        foreach (var defName in expectedThings)
        {
            IntegrationAssert.NotNull(DefDatabase<ThingDef>.GetNamedSilentFail(defName), $"Missing ThingDef {defName}.");
        }

        var professionalKitchens = DefDatabase<ResearchProjectDef>.GetNamedSilentFail(
            "ImmersiveChefs_ProfessionalKitchens");
        IntegrationAssert.NotNull(professionalKitchens, "Professional Kitchens research must finalize.");
        var prerequisites = professionalKitchens!.prerequisites.Select(value => value.defName).ToList();
        IntegrationAssert.True(
            prerequisites.Contains("ImmersiveChefs_Dishwashing") && prerequisites.Contains("Machining"),
            "Professional Kitchens must require both Dishwashing and vanilla Machining.");
        IntegrationAssert.NotNull(
            DefDatabase<RecipeDef>.GetNamedSilentFail("ImmersiveChefs_PrepareIngredients"),
            "Prepared-food recipe must finalize.");
        IntegrationAssert.True(
            !DefDatabase<ThingDef>.AllDefsListForReading.Any(def =>
                def.defName.StartsWith("ImmersiveChefs_Ceramic", StringComparison.OrdinalIgnoreCase) ||
                def.defName.StartsWith("ImmersiveChefs_Porcelain", StringComparison.OrdinalIgnoreCase)),
            "Immersive Chefs must not invent ceramic or porcelain content in this release.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FinalizedMealsContainRuntimeStateWithoutReplacingIngredients()
    {
        var meal = DefDatabase<ThingDef>.GetNamed("MealSimple");
        IntegrationAssert.True(meal.comps.Any(comp => comp.compClass == typeof(CompEmbeddedWare)),
            "MealSimple must receive embedded plate state after final Def initialization.");
        IntegrationAssert.True(meal.comps.Any(comp => comp.compClass == typeof(CompCulinaryState)),
            "MealSimple must receive culinary serving state after final Def initialization.");
        IntegrationAssert.True(meal.comps.Any(comp => comp.compClass == typeof(CompIngredients)),
            "MealSimple must retain vanilla CompIngredients for variety compatibility.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FinalizedTravelFoodsUseTheCoverageContract()
    {
        IntegrationAssert.True(
            MealCoveragePolicy.IsCovered(ThingDefOf.MealSimple),
            "A normal finalized meal must keep Immersive Chefs state while travelling.");
        IntegrationAssert.True(
            !MealCoveragePolicy.IsCovered(ThingDefOf.Pemmican),
            "Pemmican must remain a hand-eaten travel-food exclusion.");
        IntegrationAssert.True(
            !MealCoveragePolicy.IsCovered(ThingDefOf.MealSurvivalPack),
            "Packaged survival meals must remain a hand-eaten travel-food exclusion.");
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void CaravanIngestionReturnsTheExactWareWashedInWildWater()
    {
        var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var caravan = CaravanMaker.MakeCaravan(
            new[] { pawn },
            Faction.OfPlayer,
            Find.CurrentMap.Tile,
            addToWorldPawnsIfNotAlready: true);
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var silverware = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Silverware"),
            ThingDefOf.Steel);
        var originalPlateId = plate.ThingID;
        var originalSilverwareId = silverware.ThingID;

        try
        {
            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            silverware.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            var thermalStartTick = Math.Max(
                0,
                Find.TickManager.TicksGame - ThermalCalculator.TicksPerHour);
            meal.GetComp<CompCulinaryState>().ReplaceServings(new[]
            {
                new CulinaryServingRecord(
                    60,
                    70f,
                    ContaminationSources.None,
                    0,
                    thermalStartTick)
            });
            IntegrationAssert.True(
                pawn.inventory.innerContainer.TryAdd(meal, canMergeWithExistingStacks: false),
                "The travel fixture must put its meal in the caravan inventory.");
            IntegrationAssert.True(
                pawn.inventory.innerContainer.TryAdd(plate, canMergeWithExistingStacks: false),
                "The travel fixture must put its plate in the caravan inventory.");
            IntegrationAssert.True(
                pawn.inventory.innerContainer.TryAdd(silverware, canMergeWithExistingStacks: false),
                "The travel fixture must put its silverware in the caravan inventory.");

            var tileAmbient = GenTemperature.GetTemperatureAtTile(caravan.Tile);
            var expectedTemperature = ThermalCalculator.TemperatureAfter(
                70f,
                tileAmbient,
                Find.TickManager.TicksGame - thermalStartTick,
                ImmersiveChefsMod.Settings.ThermalHalfLifeHours);
            var travelServing = meal.GetComp<CompCulinaryState>().PeekCurrentServing();
            IntegrationAssert.True(
                Math.Abs(meal.AmbientTemperature - tileAmbient) < 0.01f,
                "A held caravan meal must resolve RimWorld's current world-tile ambient temperature.");
            IntegrationAssert.True(
                travelServing is not null && Math.Abs(travelServing.TemperatureCelsius - expectedTemperature) < 0.01f,
                $"Caravan meal temperature must continue moving toward the current world-tile climate " +
                $"(expected {expectedTemperature:0.###}, actual {travelServing?.TemperatureCelsius:0.###}, " +
                $"ambient {tileAmbient:0.###}).");

            meal.Ingested(pawn, 0.9f);
            caravan.RecacheInventory();

            var returnedPlate = caravan.AllThings.SingleOrDefault(thing => thing.ThingID == originalPlateId);
            var returnedSilverware = caravan.AllThings.SingleOrDefault(thing => thing.ThingID == originalSilverwareId);
            IntegrationAssert.True(
                ReferenceEquals(plate, returnedPlate),
                "Caravan dining must return the exact selected plate Thing without replacement or duplication.");
            IntegrationAssert.True(
                ReferenceEquals(silverware, returnedSilverware),
                "Caravan dining must return the exact selected silverware Thing without replacement or duplication.");
            IntegrationAssert.Equal(
                WashProvenance.WildWater,
                plate.GetComp<CompSanitation>().WashProvenance,
                "Travel-washed plates must retain the wild-water risk marker.");
            IntegrationAssert.Equal(
                WashProvenance.WildWater,
                silverware.GetComp<CompSanitation>().WashProvenance,
                "Travel-washed silverware must retain the wild-water risk marker.");
        }
        finally
        {
            if (!caravan.Destroyed)
            {
                caravan.Destroy();
            }

            if (!pawn.Destroyed)
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void CaravanIngestionReturnsTheExactEmbeddedPlateOnlyAfterEating()
    {
        var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var caravan = CaravanMaker.MakeCaravan(
            new[] { pawn },
            Faction.OfPlayer,
            Find.CurrentMap.Tile,
            addToWorldPawnsIfNotAlready: true);
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var silverware = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Silverware"),
            ThingDefOf.Steel);

        try
        {
            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            silverware.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            IntegrationAssert.True(
                meal.GetComp<CompEmbeddedWare>().TryEmbedPlate(plate),
                "The fixture must begin with its exact plate contained by the meal.");
            IntegrationAssert.True(
                pawn.inventory.innerContainer.TryAdd(meal, canMergeWithExistingStacks: false),
                "The fixture must put its plated meal in caravan inventory.");
            IntegrationAssert.True(
                pawn.inventory.innerContainer.TryAdd(silverware, canMergeWithExistingStacks: false),
                "The fixture must put its silverware in caravan inventory.");
            IntegrationAssert.True(
                !pawn.inventory.innerContainer.Contains(plate),
                "An embedded plate must not be a direct loose caravan inventory item before eating.");

            meal.Ingested(pawn, 0.9f);
            caravan.RecacheInventory();

            IntegrationAssert.True(
                caravan.AllThings.Any(thing => ReferenceEquals(thing, plate)),
                "Eating must move the exact plate out of the consumed meal and into caravan inventory.");
            IntegrationAssert.True(
                caravan.AllThings.Any(thing => ReferenceEquals(thing, silverware)),
                "Eating must return the exact selected silverware to caravan inventory.");
            IntegrationAssert.Equal(
                WashProvenance.WildWater,
                plate.GetComp<CompSanitation>().WashProvenance,
                "The returned embedded plate must receive caravan wild-water wash provenance.");
        }
        finally
        {
            if (!caravan.Destroyed)
            {
                caravan.Destroy();
            }

            if (!pawn.Destroyed)
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void CancelledCaravanIngestionRestoresUnusedWareWithoutWashing()
    {
        var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var caravan = CaravanMaker.MakeCaravan(
            new[] { pawn },
            Faction.OfPlayer,
            Find.CurrentMap.Tile,
            addToWorldPawnsIfNotAlready: true);
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var silverware = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Silverware"),
            ThingDefOf.Steel);

        try
        {
            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.WildWater);
            silverware.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            meal.GetComp<CompCulinaryState>().ReplaceServings(new[]
            {
                new CulinaryServingRecord(
                    55,
                    20f,
                    ContaminationSources.DirtyCookware,
                    0,
                    Find.TickManager.TicksGame)
            });
            pawn.inventory.innerContainer.TryAdd(meal, canMergeWithExistingStacks: false);
            pawn.inventory.innerContainer.TryAdd(plate, canMergeWithExistingStacks: false);
            pawn.inventory.innerContainer.TryAdd(silverware, canMergeWithExistingStacks: false);

            DiningSessionRegistry.TryAttachTravel(pawn, meal);
            IntegrationAssert.True(
                ReferenceEquals(meal.GetComp<CompEmbeddedWare>().PeekPlateThing(), plate),
                "The cancellation fixture must import its exact loose caravan plate before rollback.");
            DiningSessionRegistry.BeginIngestion(pawn);
            DiningSessionRegistry.EndIngestion(pawn);
            caravan.RecacheInventory();

            IntegrationAssert.True(
                meal.GetComp<CompEmbeddedWare>().PeekPlateThing() is null,
                "A cancelled travel attempt must detach the plate it imported into an unplated meal.");
            IntegrationAssert.True(
                caravan.AllThings.Any(thing => ReferenceEquals(thing, plate)),
                "A cancelled travel attempt must return the exact unused plate.");
            IntegrationAssert.True(
                caravan.AllThings.Any(thing => ReferenceEquals(thing, silverware)),
                "A cancelled travel attempt must return the exact unused silverware.");
            IntegrationAssert.Equal(
                ContaminationSources.DirtyCookware,
                meal.GetComp<CompCulinaryState>().PeekCurrentServing()!.Contamination,
                "A cancelled travel attempt must leave the uneaten meal's prior contamination unchanged.");
            IntegrationAssert.True(
                !plate.GetComp<CompSanitation>().IsDirty,
                "Cancellation must preserve a clean unused plate's sanitation state.");
            IntegrationAssert.Equal(
                WashProvenance.WildWater,
                plate.GetComp<CompSanitation>().WashProvenance,
                "Cancellation must preserve the unused plate's prior wild-water provenance.");
            IntegrationAssert.Equal(
                WashProvenance.Safe,
                silverware.GetComp<CompSanitation>().WashProvenance,
                "Cancellation must not claim that the unused silverware was washed in wild water.");
        }
        finally
        {
            if (!caravan.Destroyed)
            {
                caravan.Destroy();
            }

            if (!pawn.Destroyed)
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void CaravanAnimalsDoNotUseOrDestroyTableware()
    {
        var animal = PawnGenerator.GeneratePawn(PawnKindDefOf.Muffalo, Faction.OfPlayer);
        var caravan = CaravanMaker.MakeCaravan(
            new[] { animal },
            Faction.OfPlayer,
            Find.CurrentMap.Tile,
            addToWorldPawnsIfNotAlready: true);
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var silverware = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Silverware"),
            ThingDefOf.Steel);
        var settings = ImmersiveChefsMod.Settings;
        var originalCulinaryQualityEnabled = settings.CulinaryQualityEnabled;
        var originalMealTemperatureEnabled = settings.MealTemperatureEnabled;
        var originalFoodPoisoningEffectScale = settings.FoodPoisoningEffectScale;
        var originalMaximumCustomPoisonChance = settings.MaximumCustomPoisonChance;
        var originalMicrowaveExtraPoisonChance = settings.MicrowaveExtraPoisonChance;

        try
        {
            settings.CulinaryQualityEnabled = true;
            settings.MealTemperatureEnabled = true;
            settings.FoodPoisoningEffectScale = 3f;
            settings.MaximumCustomPoisonChance = 1f;
            settings.MicrowaveExtraPoisonChance = 5f;
            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            silverware.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            meal.GetComp<CompCulinaryState>().ReplaceServings(new[]
            {
                new CulinaryServingRecord(
                    0,
                    -20f,
                    ContaminationSources.DirtyCookware |
                    ContaminationSources.DirtyPlate |
                    ContaminationSources.DirtySilverware |
                    ContaminationSources.WildWaterCookware |
                    ContaminationSources.WildWaterPlate |
                    ContaminationSources.WildWaterSilverware,
                    20,
                    Find.TickManager.TicksGame)
            });
            AccessTools.Field(typeof(CompFoodPoisonable), "poisonPct")
                .SetValue(meal.GetComp<CompFoodPoisonable>(), 0f);
            IntegrationAssert.True(
                meal.GetComp<CompEmbeddedWare>().TryEmbedPlate(plate),
                "The animal exclusion fixture must start with a plated meal.");
            animal.inventory.innerContainer.TryAdd(meal, canMergeWithExistingStacks: false);
            animal.inventory.innerContainer.TryAdd(silverware, canMergeWithExistingStacks: false);

            meal.Ingested(animal, 0.9f);
            caravan.RecacheInventory();

            IntegrationAssert.True(
                caravan.AllThings.Any(thing => ReferenceEquals(thing, plate)),
                "An animal eating a meal must return its exact unused plate to caravan inventory.");
            IntegrationAssert.True(
                caravan.AllThings.Any(thing => ReferenceEquals(thing, silverware)),
                "Animal ingestion must not select or consume caravan silverware.");
            IntegrationAssert.Equal(
                WashProvenance.Safe,
                plate.GetComp<CompSanitation>().WashProvenance,
                "An animal-excluded plate must retain its original wash provenance.");
            IntegrationAssert.Equal(
                WashProvenance.Safe,
                silverware.GetComp<CompSanitation>().WashProvenance,
                "Animal-excluded silverware must retain its original wash provenance.");
            IntegrationAssert.True(
                animal.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.FoodPoisoning) is null,
                "Animal ingestion must not apply Immersive Chefs' custom food-poisoning risk.");
        }
        finally
        {
            settings.CulinaryQualityEnabled = originalCulinaryQualityEnabled;
            settings.MealTemperatureEnabled = originalMealTemperatureEnabled;
            settings.FoodPoisoningEffectScale = originalFoodPoisoningEffectScale;
            settings.MaximumCustomPoisonChance = originalMaximumCustomPoisonChance;
            settings.MicrowaveExtraPoisonChance = originalMicrowaveExtraPoisonChance;
            if (!caravan.Destroyed)
            {
                caravan.Destroy();
            }

            if (!animal.Destroyed)
            {
                animal.Destroy(DestroyMode.Vanish);
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void MapAnimalsDoNotReserveUseOrDirtyTableware()
    {
        var map = Find.CurrentMap;
        var animal = PawnGenerator.GeneratePawn(
            DefDatabase<PawnKindDef>.GetNamed("Raccoon"),
            null);
        var animalCell = CellFinder.RandomClosewalkCellNear(map.Center, map, 12);
        var mealCell = animalCell;
        var silverwareCell = new IntVec3(animalCell.x + 1, 0, animalCell.z);
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Plasteel);
        var silverware = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Silverware"),
            ThingDefOf.Steel);
        var settings = ImmersiveChefsMod.Settings;
        var originalCulinaryQualityEnabled = settings.CulinaryQualityEnabled;
        var originalMealTemperatureEnabled = settings.MealTemperatureEnabled;
        var originalFoodPoisoningEffectScale = settings.FoodPoisoningEffectScale;
        var originalMaximumCustomPoisonChance = settings.MaximumCustomPoisonChance;

        try
        {
            settings.CulinaryQualityEnabled = true;
            settings.MealTemperatureEnabled = true;
            settings.FoodPoisoningEffectScale = 3f;
            settings.MaximumCustomPoisonChance = 1f;
            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            silverware.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            meal.GetComp<CompCulinaryState>().ReplaceServings(new[]
            {
                new CulinaryServingRecord(
                    0,
                    -20f,
                    ContaminationSources.DirtyCookware |
                    ContaminationSources.DirtyPlate |
                    ContaminationSources.DirtySilverware,
                    20,
                    Find.TickManager.TicksGame)
            });
            AccessTools.Field(typeof(CompFoodPoisonable), "poisonPct")
                .SetValue(meal.GetComp<CompFoodPoisonable>(), 0f);
            IntegrationAssert.True(
                meal.GetComp<CompEmbeddedWare>().TryEmbedPlate(plate),
                "The map animal exclusion fixture must start with an exact embedded plate.");

            GenSpawn.Spawn(animal, animalCell, map);
            GenSpawn.Spawn(meal, mealCell, map);
            GenSpawn.Spawn(silverware, silverwareCell, map);
            animal.needs.food.CurLevel = 0.01f;
            var originalSilverwarePosition = silverware.Position;
            var ingestJob = JobMaker.MakeJob(JobDefOf.Ingest, meal);
            var silverwareWasReserved = false;

            animal.jobs.StartJob(ingestJob, JobCondition.InterruptForced);
            var chewMethod = AccessTools.Method(typeof(Toils_Ingest), nameof(Toils_Ingest.ChewIngestible));
            var chewOwners = Harmony.GetPatchInfo(chewMethod)?.Owners
                .Count(owner => owner == ImmersiveChefsMod.PackageId) ?? 0;
            IntegrationAssert.Equal(
                1,
                chewOwners,
                "Animal map ingestion must run with exactly one Immersive Chefs chew-speed patch owner.");
            var plateSpeed = plate.GetComp<CompKitchenwareStats>().CurrentStats.CookingSpeedFactor;
            IntegrationAssert.True(
                Math.Abs(plateSpeed - 1f) > 0.1f,
                "The animal chew-speed fixture must use a plate with a distinguishable non-native factor.");
            var platedChew = Toils_Ingest.ChewIngestible(
                animal,
                1f,
                TargetIndex.A,
                TargetIndex.None);
            var embedded = meal.GetComp<CompEmbeddedWare>();
            var releasedPlate = embedded.ReleasePlateThing();
            IntegrationAssert.True(
                ReferenceEquals(plate, releasedPlate),
                "The chew-speed fixture must temporarily release the exact embedded plate.");
            var unplatedChew = Toils_Ingest.ChewIngestible(
                animal,
                1f,
                TargetIndex.A,
                TargetIndex.None);
            IntegrationAssert.True(
                embedded.TryEmbedPlate(plate),
                "The chew-speed fixture must restore its exact plate before native ingestion.");
            IntegrationAssert.Equal(
                unplatedChew.defaultDuration,
                platedChew.defaultDuration,
                "A non-humanlike animal's native chew duration must ignore a distinguishable plate speed factor.");
            for (var tick = 0; tick < 5000 && !meal.Destroyed; tick++)
            {
                animal.jobs.JobTrackerTick();
                silverwareWasReserved |= map.reservationManager.IsReserved(silverware);
            }

            IntegrationAssert.True(
                meal.Destroyed,
                "A real animal JobDriver_Ingest must complete within the bounded fixture ticks.");
            IntegrationAssert.True(
                DiningSessionRegistry.SilverwareFor(ingestJob) is null,
                "The real animal map-ingest job must not select nearby silverware.");
            IntegrationAssert.True(
                DiningSessionRegistry.PlateFor(ingestJob) is null,
                "The real animal map-ingest job must not create a service-ware pickup session.");
            IntegrationAssert.True(
                !silverwareWasReserved,
                "The real animal map-ingest job must never reserve nearby silverware.");

            IntegrationAssert.True(
                plate.Spawned && ReferenceEquals(plate.Map, map) && plate.Position == animal.Position,
                "Animal map ingestion must recover the exact embedded plate at the eating location.");
            IntegrationAssert.Equal(
                WashProvenance.Safe,
                plate.GetComp<CompSanitation>().WashProvenance,
                "The recovered animal plate must remain clean with unchanged safe provenance.");
            IntegrationAssert.True(
                !plate.GetComp<CompSanitation>().IsDirty,
                "The recovered animal plate must retain its clean sanitation flag.");
            IntegrationAssert.True(
                silverware.Spawned && ReferenceEquals(silverware.Map, map) &&
                silverware.Position == originalSilverwarePosition,
                "Nearby map silverware must remain spawned at its original cell.");
            IntegrationAssert.Equal(
                WashProvenance.Safe,
                silverware.GetComp<CompSanitation>().WashProvenance,
                "Nearby map silverware must remain clean and untouched.");
            IntegrationAssert.True(
                !silverware.GetComp<CompSanitation>().IsDirty,
                "Nearby map silverware must retain its clean sanitation flag.");
            IntegrationAssert.True(
                animal.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.FoodPoisoning) is null,
                "Animal map ingestion must not apply Immersive Chefs' custom food-poisoning risk.");
        }
        finally
        {
            settings.CulinaryQualityEnabled = originalCulinaryQualityEnabled;
            settings.MealTemperatureEnabled = originalMealTemperatureEnabled;
            settings.FoodPoisoningEffectScale = originalFoodPoisoningEffectScale;
            settings.MaximumCustomPoisonChance = originalMaximumCustomPoisonChance;
            if (!meal.Destroyed)
            {
                meal.Destroy(DestroyMode.Vanish);
            }

            if (!plate.Destroyed)
            {
                plate.Destroy(DestroyMode.Vanish);
            }

            if (!silverware.Destroyed)
            {
                silverware.Destroy(DestroyMode.Vanish);
            }

            if (!animal.Destroyed)
            {
                animal.Destroy(DestroyMode.Vanish);
            }
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void CompletedMapDiningWithoutSilverwareCreatesOneDirtEvent()
    {
        var map = Find.CurrentMap;
        var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var diningCell = map.AllCells
            .Where(cell => cell.Standable(map) &&
                           cell.GetEdifice(map) is null &&
                           cell.GetThingList(map).All(thing => thing.def != ThingDefOf.Filth_Dirt))
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .First(cell => FilthMaker.CanMakeFilth(cell, map, ThingDefOf.Filth_Dirt));
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var settings = ImmersiveChefsMod.Settings;
        var originalWareRequirementMode = settings.WareRequirementMode;
        var preexistingSilverware = map.listerThings.AllThings
            .Where(thing => thing.def.GetModExtension<KitchenwareExtension>()?.product ==
                            KitchenwareProduct.Silverware)
            .Select(thing => new { Thing = thing, Forbidden = thing.IsForbidden(Faction.OfPlayer) })
            .ToList();

        try
        {
            settings.WareRequirementMode = WareRequirementMode.Prefer;
            foreach (var existing in preexistingSilverware)
            {
                existing.Thing.SetForbidden(true, warnOnFail: false);
            }

            plate.GetComp<CompSanitation>().MarkClean(WashProvenance.Safe);
            meal.GetComp<CompCulinaryState>().ReplaceServings(new[]
            {
                new CulinaryServingRecord(
                    50,
                    35f,
                    ContaminationSources.None,
                    0,
                    Find.TickManager.TicksGame)
            });
            IntegrationAssert.True(
                meal.GetComp<CompEmbeddedWare>().TryEmbedPlate(plate),
                "The missing-silverware fixture must start with an exact embedded plate.");

            GenSpawn.Spawn(pawn, diningCell, map);
            GenSpawn.Spawn(meal, diningCell, map);
            pawn.needs.food.CurLevel = 0.01f;
            var dirtBefore = map.listerThings.ThingsOfDef(ThingDefOf.Filth_Dirt)
                .Cast<Filth>()
                .Sum(filth => filth.thickness);
            var ingestJob = JobMaker.MakeJob(JobDefOf.Ingest, meal);
            pawn.jobs.StartJob(ingestJob, JobCondition.InterruptForced);

            for (var tick = 0; tick < 5000 && !meal.Destroyed; tick++)
            {
                pawn.jobs.JobTrackerTick();
            }

            IntegrationAssert.True(
                meal.Destroyed,
                "A real colonist JobDriver_Ingest must complete within the bounded fixture ticks.");
            var dirtAfter = map.listerThings.ThingsOfDef(ThingDefOf.Filth_Dirt)
                .Cast<Filth>()
                .Sum(filth => filth.thickness);
            IntegrationAssert.Equal(
                dirtBefore + 1,
                dirtAfter,
                "Completed eligible map dining without silverware must add exactly one dirt thickness.");
            IntegrationAssert.True(
                pawn.Position.GetThingList(map).Any(thing => thing.def == ThingDefOf.Filth_Dirt),
                "The native dirt event must occur at the diner's actual final eating location.");

            var diningThought = DefDatabase<ThoughtDef>.GetNamed("ImmersiveChefs_DiningExperience");
            var memory = pawn.needs.mood.thoughts.memories.GetFirstMemoryOfDef(diningThought);
            IntegrationAssert.NotNull(memory, "The diner must receive the combined dining thought.");
            IntegrationAssert.Equal(
                1,
                memory!.CurStageIndex,
                "A plated meal without silverware must select the missing-silverware thought stage.");
        }
        finally
        {
            settings.WareRequirementMode = originalWareRequirementMode;
            foreach (var existing in preexistingSilverware)
            {
                if (!existing.Thing.Destroyed)
                {
                    existing.Thing.SetForbidden(existing.Forbidden, warnOnFail: false);
                }
            }

            if (!meal.Destroyed)
            {
                meal.Destroy(DestroyMode.Vanish);
            }

            if (!plate.Destroyed)
            {
                plate.Destroy(DestroyMode.Vanish);
            }

            if (!pawn.Destroyed)
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void SanitationStorageFiltersAreFinalizedAgainstKitchenware()
    {
        var cleanFilter = DefDatabase<SpecialThingFilterDef>.GetNamedSilentFail(
            "ImmersiveChefs_AllowCleanKitchenware");
        var dirtyFilter = DefDatabase<SpecialThingFilterDef>.GetNamedSilentFail(
            "ImmersiveChefs_AllowDirtyKitchenware");
        IntegrationAssert.NotNull(cleanFilter, "The clean kitchenware storage filter must finalize.");
        IntegrationAssert.NotNull(dirtyFilter, "The dirty kitchenware storage filter must finalize.");

        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        IntegrationAssert.NotNull(cleanFilter!.Worker, "The clean filter worker must instantiate.");
        IntegrationAssert.NotNull(dirtyFilter!.Worker, "The dirty filter worker must instantiate.");
        IntegrationAssert.True(
            cleanFilter.allowedByDefault && dirtyFilter.allowedByDefault,
            "Both sanitation filters must preserve vanilla storage behavior until a player disables one.");
        IntegrationAssert.True(
            cleanFilter.Worker.CanEverMatch(plateDef),
            "The finalized clean filter must recognize the plate Def.");
        IntegrationAssert.True(
            dirtyFilter.Worker.CanEverMatch(plateDef),
            "The finalized dirty filter must recognize the plate Def.");
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void EmbeddedMealOwnsAndReleasesTheExactPlateThing()
    {
        var meal = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("MealSimple"));
        var plate = ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var plateId = plate.ThingID;
        var embedded = ((ThingWithComps)meal).GetComp<CompEmbeddedWare>();

        IntegrationAssert.True(embedded.TryEmbedPlate(plate), "The finalized meal must accept one physical plate.");
        ((ThingWithComps)plate).GetComp<CompSanitation>().MarkClean(WashProvenance.WildWater);
        IntegrationAssert.True(
            ReferenceEquals(plate, embedded.PeekPlateThing()),
            "Embedding must retain the original Thing instance.");
        IntegrationAssert.Equal(
            WashProvenance.WildWater,
            embedded.Bindings.Single().WashProvenance,
            "The lightweight plate binding must retain sanitation provenance.");
        var released = embedded.ReleasePlateThing();
        IntegrationAssert.True(
            ReferenceEquals(plate, released),
            "Releasing must return the exact original Thing instance.");
        IntegrationAssert.Equal(plateId, released!.ThingID, "The plate LoadID must remain unchanged.");

    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void RealStorageFiltersAndStackingTrackSpawnedSanitationTransitions()
    {
        var map = Find.CurrentMap;
        var pawn = map.mapPawns.FreeColonistsSpawned.First();
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        var cleanSpecial = DefDatabase<SpecialThingFilterDef>.GetNamed("ImmersiveChefs_AllowCleanKitchenware");
        var dirtySpecial = DefDatabase<SpecialThingFilterDef>.GetNamed("ImmersiveChefs_AllowDirtyKitchenware");
        var zoneCells = map.AllCells
            .Where(cell =>
                cell.Standable(map) &&
                map.zoneManager.ZoneAt(cell) is null &&
                cell.GetFirstItem(map) is null &&
                cell.GetEdifice(map) is null)
            .OrderBy(cell => cell.DistanceToSquared(pawn.Position))
            .Take(2)
            .ToList();
        IntegrationAssert.Equal(2, zoneCells.Count, "The quickstart map must provide two stockpile fixture cells.");

        var cleanZone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
        var dirtyZone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
        map.zoneManager.RegisterZone(cleanZone);
        map.zoneManager.RegisterZone(dirtyZone);
        cleanZone.AddCell(zoneCells[0]);
        dirtyZone.AddCell(zoneCells[1]);
        ConfigureSanitationStockpile(cleanZone, plateDef, cleanSpecial, dirtySpecial, clean: true);
        ConfigureSanitationStockpile(dirtyZone, plateDef, cleanSpecial, dirtySpecial, clean: false);

        var plate = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        var other = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        GenSpawn.Spawn(plate, zoneCells[0], map);

        try
        {
            var sanitation = plate.GetComp<CompSanitation>();
            var otherSanitation = other.GetComp<CompSanitation>();
            sanitation.MarkClean(WashProvenance.WildWater);
            otherSanitation.MarkClean(WashProvenance.Safe);

            IntegrationAssert.True(
                !sanitation.AllowStackWith(other),
                "Equal clean plates with different wash provenance must not stack and erase safety state.");

            var dirtyOnly = new ThingFilter();
            dirtyOnly.SetAllow(plateDef, allow: true);
            dirtyOnly.SetAllow(cleanSpecial, allow: false);
            dirtyOnly.SetAllow(dirtySpecial, allow: true);
            IntegrationAssert.True(!dirtyOnly.Allows(plate), "A clean plate must be excluded from dirty-only storage.");
            IntegrationAssert.True(
                !map.listerHaulables.ThingsPotentiallyNeedingHauling().Contains(plate),
                "A clean plate already in clean-only storage must not be queued for hauling.");

            sanitation.MarkDirty();
            IntegrationAssert.True(
                dirtyOnly.Allows(plate),
                "A spawned plate must enter dirty-only storage eligibility immediately after being dirtied.");
            IntegrationAssert.True(
                map.listerHaulables.ThingsPotentiallyNeedingHauling().Contains(plate),
                "Dirtifying a spawned plate must invalidate the haul cache for its clean-only stockpile.");
            var haulJob = HaulAIUtility.HaulToStorageJob(pawn, plate, forced: true);
            IntegrationAssert.NotNull(haulJob, "RimWorld must find the dirty-only stockpile after invalidation.");
            IntegrationAssert.Equal(
                zoneCells[1],
                haulJob!.GetTarget(Verse.AI.TargetIndex.B).Cell,
                "RimWorld's native hauling selector must route the dirty plate to dirty-only storage.");

            var cleanOnly = new ThingFilter();
            cleanOnly.SetAllow(plateDef, allow: true);
            cleanOnly.SetAllow(cleanSpecial, allow: true);
            cleanOnly.SetAllow(dirtySpecial, allow: false);
            IntegrationAssert.True(!cleanOnly.Allows(plate), "A dirty plate must be excluded from clean-only storage.");

            sanitation.MarkClean(WashProvenance.Safe);
            IntegrationAssert.True(
                cleanOnly.Allows(plate),
                "A spawned plate must enter clean-only storage eligibility immediately after being washed.");
        }
        finally
        {
            if (!plate.Destroyed)
            {
                plate.Destroy(DestroyMode.Vanish);
            }

            if (!other.Destroyed)
            {
                other.Destroy(DestroyMode.Vanish);
            }

            cleanZone.Delete();
            dirtyZone.Delete();
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void TypedWashSourcesWriteVanillaSerializableJobTargets()
    {
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        var dirtyWare = ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        var fixture = ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        try
        {
            var safe = WorkGiver_DoDishes.CreateJob(
                dirtyWare,
                DishwashingDestination.ForHandwashing(fixture, WashProvenance.Safe));
            var wild = WorkGiver_DoDishes.CreateJob(
                dirtyWare,
                DishwashingDestination.ForHandwashing(Find.CurrentMap.Center, WashProvenance.WildWater));

            IntegrationAssert.True(
                safe.GetTarget(Verse.AI.TargetIndex.C).HasThing,
                "A validated safe fixture must persist its provenance marker in vanilla Job target C.");
            IntegrationAssert.True(
                !wild.GetTarget(Verse.AI.TargetIndex.C).IsValid,
                "A wild-water destination must leave vanilla Job target C unset.");
        }
        finally
        {
            if (!dirtyWare.Destroyed)
            {
                dirtyWare.Destroy(DestroyMode.Vanish);
            }

            if (!fixture.Destroyed)
            {
                fixture.Destroy(DestroyMode.Vanish);
            }
        }
    }

    private static void ConfigureSanitationStockpile(
        Zone_Stockpile zone,
        ThingDef plateDef,
        SpecialThingFilterDef cleanSpecial,
        SpecialThingFilterDef dirtySpecial,
        bool clean)
    {
        zone.settings.Priority = StoragePriority.Critical;
        zone.settings.filter.SetDisallowAll();
        zone.settings.filter.SetAllow(plateDef, allow: true);
        zone.settings.filter.SetAllow(cleanSpecial, allow: clean);
        zone.settings.filter.SetAllow(dirtySpecial, allow: !clean);
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void EmbeddedMealTransfersTheExactPlateFromPawnInventory()
    {
        var pawn = Find.CurrentMap.mapPawns.FreeColonistsSpawned.First();
        var meal = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("MealSimple"));
        var plate = ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"),
            ThingDefOf.Steel);
        var embedded = ((ThingWithComps)meal).GetComp<CompEmbeddedWare>();

        IntegrationAssert.True(
            pawn.inventory.innerContainer.TryAdd(plate, canMergeWithExistingStacks: false),
            "The integration fixture must put the physical plate in a real pawn inventory.");
        IntegrationAssert.True(
            embedded.TryEmbedPlate(plate),
            "A cooking product must transfer its reserved plate out of Pawn_InventoryTracker.");
        IntegrationAssert.True(
            ReferenceEquals(plate, embedded.PeekPlateThing()),
            "Inventory transfer must retain the exact physical plate instance.");

        var released = embedded.ReleasePlateThing();
        IntegrationAssert.True(
            ReferenceEquals(plate, released),
            "The exact inventory-sourced plate must remain recoverable after embedding.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void CoreHarmonyOwnersAreInstalledExactlyOnce()
    {
        var cooking = AccessTools.Method(typeof(WorkGiver_DoBill), nameof(WorkGiver_DoBill.JobOnThing));
        var ingest = AccessTools.Method(typeof(JobDriver_Ingest), nameof(JobDriver_Ingest.TryMakePreToilReservations));
        var ingestOutcome = AccessTools.Method(
            typeof(Thing),
            nameof(Thing.Ingested),
            new[] { typeof(Pawn), typeof(float) });
        var chew = AccessTools.Method(typeof(Toils_Ingest), nameof(Toils_Ingest.ChewIngestible));
        foreach (var method in new[] { cooking, ingest, ingestOutcome, chew })
        {
            var owners = Harmony.GetPatchInfo(method)?.Owners
                .Where(owner => owner == ImmersiveChefsMod.PackageId)
                .ToList() ?? new System.Collections.Generic.List<string>();
            IntegrationAssert.Equal(1, owners.Count, $"Expected one Immersive Chefs patch owner on {method.Name}.");
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ImmersiveChefsXmlProbeContainsItsFinalPatch()
    {
        var steel = DefDatabase<ThingDef>.GetNamedSilentFail("Steel");
        var probe = steel?.GetModExtension<ImmersiveChefsIntegrationProbeExtension>();

        IntegrationAssert.NotNull(
            probe,
            "Finalized Core Steel must contain the Immersive Chefs XML-patched mod extension.");
        IntegrationAssert.Equal(
            "patched-by-immersive-chefs-xml",
            probe!.marker,
            "The finalized probe must contain the exact Immersive Chefs PatchOperation result.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ActiveProcessorFrameworkUsesOneIdentityPreservingBridge()
    {
        var processorActive = LoadedModManager.RunningModsListForReading.Any(mod =>
            string.Equals(mod.PackageId, "syrchalis.processor.framework", StringComparison.OrdinalIgnoreCase));
        if (!processorActive)
        {
            return;
        }

        var processorType = AccessTools.TypeByName("ProcessorFramework.CompProcessor");
        IntegrationAssert.NotNull(processorType, "Active Processor Framework must expose CompProcessor.");
        foreach (var defName in new[]
                 {
                     "ImmersiveChefs_Dishwasher",
                     "ImmersiveChefs_IndustrialDishwasher"
                 })
        {
            var def = DefDatabase<ThingDef>.GetNamed(defName);
            IntegrationAssert.Equal(
                1,
                def.comps.Count(comp => processorType!.IsAssignableFrom(comp.compClass)),
                $"{defName} must contain exactly one Processor Framework component.");
            IntegrationAssert.Equal(
                DrawerType.MapMeshAndRealTime,
                def.drawerType,
                $"{defName} must render Processor Framework progress.");
        }

        var takeOut = AccessTools.Method(processorType, "TakeOutProduct");
        var owners = Harmony.GetPatchInfo(takeOut)?.Owners
            .Count(owner => owner == ImmersiveChefsMod.PackageId) ?? 0;
        IntegrationAssert.Equal(1, owners, "Processor completion must have one Immersive Chefs identity bridge.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ActiveDubsAddsOneValidatedPipeToEachDishwasher()
    {
        var dubsActive = LoadedModManager.RunningModsListForReading.Any(mod =>
            string.Equals(mod.PackageId, "dubwise.dubsbadhygiene", StringComparison.OrdinalIgnoreCase));
        if (!dubsActive)
        {
            return;
        }

        var pipeType = AccessTools.TypeByName("DubsBadHygiene.CompPipe");
        IntegrationAssert.NotNull(pipeType, "Active Dubs Bad Hygiene must expose CompPipe.");
        foreach (var defName in new[]
                 {
                     "ImmersiveChefs_Dishwasher",
                     "ImmersiveChefs_IndustrialDishwasher"
                 })
        {
            var def = DefDatabase<ThingDef>.GetNamed(defName);
            IntegrationAssert.Equal(
                1,
                def.comps.Count(comp => pipeType!.IsAssignableFrom(comp.compClass)),
                $"{defName} must contain exactly one validated Dubs pipe component.");
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ActiveGastronomyInstallsOneGuardedWaiterBridge()
    {
        var activeIds = LoadedModManager.RunningModsListForReading
            .Select(mod => mod.PackageId)
            .ToList();
        var gastronomyActive = activeIds.Any(id =>
            string.Equals(id, "orion.gastronomy", StringComparison.OrdinalIgnoreCase));
        if (!gastronomyActive)
        {
            return;
        }

        IntegrationAssert.True(
            activeIds.Any(id => string.Equals(id, "orion.cashregister", StringComparison.OrdinalIgnoreCase)),
            "The supported Gastronomy matrix must load Cash Register first.");
        var serveType = AccessTools.TypeByName("Gastronomy.Waiting.JobDriver_Serve");
        IntegrationAssert.NotNull(serveType, "Active Gastronomy must expose its waiter driver.");
        var makeToils = AccessTools.Method(serveType, "MakeNewToils");
        var owners = Harmony.GetPatchInfo(makeToils)?.Owners
            .Count(owner => owner == ImmersiveChefsMod.PackageId) ?? 0;
        IntegrationAssert.Equal(1, owners, "Gastronomy serving must have one guarded Immersive Chefs bridge.");
    }
}
