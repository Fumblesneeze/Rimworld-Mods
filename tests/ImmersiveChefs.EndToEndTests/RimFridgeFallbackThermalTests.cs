using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.rimfridge-refrigerated-microwave-depth",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "rimfridge.kv.rw",
    "fumblesneeze.immersivechefs",
    MaxFrames = 9_000,
    MaxGameTicks = 42_000,
    MaxWallClockSeconds = 360)]
public sealed class RimFridgeRefrigeratedMicrowaveDepthTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn cook = null!;
    private Pawn diner = null!;
    private ThingWithComps stove = null!;
    private ThingWithComps fridge = null!;
    private Building_Microwave microwave = null!;
    private ThingWithComps meal = null!;
    private ThingWithComps cookware = null!;
    private ThingWithComps plate = null!;
    private ThingWithComps cutlery = null!;
    private string dinerId = string.Empty;
    private string fridgeId = string.Empty;
    private string microwaveId = string.Empty;
    private string mealId = string.Empty;
    private string plateId = string.Empty;
    private string cutleryId = string.Empty;
    private bool nativeStorageObserved;
    private bool nativeCookingObserved;
    private bool nativeHeatingObserved;
    private float desiredTemperature;
    private float internalTemperature;
    private float refrigeratedTemperature;
    private int qualityBeforeReheat;
    private int qualityAfterReheat;

    public void Arrange(IEndToEndContext context)
    {
        PreserveSettings(context);
        map = Current.Game.CurrentMap;
        var center = FoodSearchE2EFixture.FindRoomCenter(map);

        DispenserE2EFixture.SpawnConduitGrid(map, center, 7, 6);
        DispenserE2EFixture.SpawnPowerSources(map, center + new IntVec3(-5, 0, 5), 2);
        fridge = SpawnBuilding(
            "RimFridge_SingleRefrigerator",
            center + new IntVec3(3, 0, 2),
            Rot4.North);
        stove = SpawnBuilding("FueledStove", center, Rot4.North);
        stove.GetComp<CompRefuelable>()?.Refuel(999f);

        var table = (Building)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("Table1x2c"),
            ThingDefOf.Steel);
        table.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(table, center + new IntVec3(-2, 0, 2), map, Rot4.North);
        microwave = SpawnCountertopMicrowave(table);
        DispenserE2EFixture.SettlePower(map, new ThingWithComps[] { fridge, microwave }, 400);
        EndToEndAssert.True(fridge.GetComp<CompPowerTrader>()?.PowerOn == true,
            "The exact RimFridge must begin on the native supplied power net.");
        EndToEndAssert.True(microwave.GetComp<CompMicrowave>().Operational,
            "The fallback countertop microwave must begin powered and supported.");

        var storage = fridge as Building_Storage ??
                      throw new EndToEndAssertionException(
                          "The supported RimFridge Def must remain native Building_Storage.");
        storage.settings.Priority = StoragePriority.Critical;
        storage.settings.filter.SetDisallowAll();
        storage.settings.filter.SetAllow(ThingDefOf.MealSimple, true);

        cook = CreateInactiveCook("RimFridge refrigerator chef");
        GenSpawn.Spawn(cook, center + new IntVec3(0, 0, -4), map);
        diner = FoodSearchE2EFixture.CreateColonist("RimFridge refrigerator diner");
        FoodSearchE2EFixture.SetHunger(diner, 0.95f);
        GenSpawn.Spawn(diner, center + new IntVec3(-4, 0, -1), map);

        var bill = new Bill_Production(DefDatabase<RecipeDef>.GetNamed("CookMealSimple"))
        {
            repeatMode = BillRepeatModeDefOf.RepeatCount,
            repeatCount = 1,
            ingredientSearchRadius = 9f
        };
        bill.SetStoreMode(BillStoreModeDefOf.BestStockpile);
        bill.SetPawnRestriction(cook);
        ((IBillGiver)stove).BillStack.AddBill(bill);

        var rice = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RawRice"));
        rice.stackCount = 50;
        GenSpawn.Spawn(rice, center + new IntVec3(3, 0, -2), map);
        cookware = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cookware",
            ThingDefOf.Steel);
        plate = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Plate",
            ThingDefOf.Steel);
        cutlery = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cutlery",
            ThingDefOf.Steel);
        GenSpawn.Spawn(cookware, center + new IntVec3(-1, 0, -2), map);
        GenSpawn.Spawn(plate, center + new IntVec3(-2, 0, -2), map);
        GenSpawn.Spawn(cutlery, center + new IntVec3(2, 0, -2), map);

        dinerId = diner.ThingID;
        fridgeId = fridge.ThingID;
        microwaveId = microwave.ThingID;
        plateId = plate.ThingID;
        cutleryId = cutlery.ThingID;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before configuring RimFridge",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select RimFridge before its player temperature command",
            new[] { fridgeId },
            additive: false);
        yield return new CameraActionStep(
            "frame RimFridge refrigerator fixture",
            new[]
            {
                cook.ThingID, dinerId, stove.ThingID, fridgeId, microwaveId,
                cookware.ThingID, plateId, cutleryId
            },
            230);
        yield return new ScreenshotStep(
            "RimFridge at its default freezer target before player adjustment",
            Array.Empty<string>(),
            0);

        var raiseTen = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { fridgeId }, Array.Empty<string>())
            .Where(option =>
                !option.Disabled &&
                option.Interaction == EndToEndGizmoInteraction.Invoke &&
                option.Label.StartsWith("+", StringComparison.Ordinal) &&
                option.Label.IndexOf("10", StringComparison.Ordinal) >= 0)
            .ToArray();
        EndToEndAssert.Equal(1, raiseTen.Length,
            "RimFridge must expose one enabled native +10 temperature command.");
        yield return new GizmoActionStep(
            "raise RimFridge from its default -5 C freezer target to a 5 C refrigerator target",
            new[] { fridgeId },
            raiseTen[0].RuntimeType,
            EndToEndGizmoInteraction.Invoke,
            stableGizmoId: raiseTen[0].StableId);
        yield return new TimeControlActionStep(
            "let the native RimFridge compressor settle at refrigerator temperature",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "RimFridge reaches its player-configured 5 C target",
            _ => ObserveRimFridgeTemperature(),
            new EndToEndDeadline(1_200, 4_000, TimeSpan.FromSeconds(50)));
        yield return new TimeControlActionStep(
            "pause on the settled refrigerator inspector",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep(
            "RimFridge visibly settled at its refrigerator target",
            Array.Empty<string>(),
            0);

        yield return new AssertionStep(
            "enable only the native cooking work for the RimFridge bill",
            _ =>
            {
                cook.workSettings.SetPriority(DefDatabase<WorkTypeDef>.GetNamed("Cooking"), 1);
                cook.jobs.EndCurrentJob(JobCondition.InterruptForced);
            });
        yield return new TimeControlActionStep(
            "run the native cooking bill and its configured RimFridge storage",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the chef enters the native RimFridge-bound cooking bill",
            _ => ObserveNativeCooking(),
            new EndToEndDeadline(1_200, 4_000, TimeSpan.FromSeconds(50)));
        yield return new SelectionActionStep(
            "select the chef during native RimFridge-bound cooking",
            new[] { cook.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "native cooking before RimFridge storage",
            new[] { cook.ThingID, stove.ThingID, fridgeId },
            170);
        yield return new WaitUntilStep(
            "the chef cooks and the bill stores its plated meal in RimFridge",
            _ => ObserveNativeStorage(),
            new EndToEndDeadline(3_000, 12_000, TimeSpan.FromSeconds(110)));
        yield return new AssertionStep(
            "make the native chef inert after storage",
            _ =>
            {
                cook.workSettings.SetPriority(DefDatabase<WorkTypeDef>.GetNamed("Cooking"), 0);
                cook.jobs.EndCurrentJob(JobCondition.InterruptForced);
            });
        yield return new TimeControlActionStep(
            "cool the stored serving against RimFridge's real ambient temperature",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the serving reaches a refrigerated but not frozen temperature",
            _ => CurrentServing().TemperatureCelsius <= 7f,
            new EndToEndDeadline(4_200, 17_000, TimeSpan.FromSeconds(170)));
        yield return new TimeControlActionStep(
            "pause on the refrigerated serving",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "capture refrigerator source depth before native retrieval",
            _ =>
            {
                var serving = CurrentServing();
                refrigeratedTemperature = serving.TemperatureCelsius;
                qualityBeforeReheat = serving.QualityScore;
                EndToEndAssert.True(
                    refrigeratedTemperature > 0f && refrigeratedTemperature <= 7f,
                    "RimFridge must leave this serving refrigerated rather than Frozen; observed " +
                    refrigeratedTemperature.ToString("0.###") + " C.");
                EndToEndAssert.True(qualityBeforeReheat > 0,
                    "Native cooking must produce a positive culinary quality before passive cooling.");
                AssertExactWareBeforeDining();
            });
        yield return new SelectionActionStep(
            "select the refrigerated plated meal in RimFridge",
            new[] { mealId },
            additive: false);
        yield return new ScreenshotStep(
            "refrigerated meal inspector before native microwave retrieval",
            Array.Empty<string>(),
            0);

        yield return new AssertionStep(
            "activate hunger only for explicit native microwave dining",
            _ => FoodSearchE2EFixture.SetHunger(diner, 0.10f));
        var consumeOptions = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(dinerId, mealId);
        var consume = consumeOptions.Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("consume", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(1, consume.Length,
            "The refrigerated RimFridge meal must expose one enabled native Consume action.");
        yield return new FloatMenuActionStep(
            "order native retrieval, microwave reheating, and dining from RimFridge",
            dinerId,
            mealId,
            consume[0].StableId);
        yield return new TimeControlActionStep(
            "run native RimFridge retrieval and countertop heating",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the diner reaches the real countertop microwave heating toil",
            _ => ObserveNativeHeating(),
            new EndToEndDeadline(1_800, 6_000, TimeSpan.FromSeconds(70)));
        yield return new SelectionActionStep(
            "select diner during native refrigerated-meal heating",
            new[] { dinerId },
            additive: false);
        yield return new ScreenshotStep(
            "native countertop reheating after RimFridge retrieval",
            Array.Empty<string>(),
            0);
        yield return new WaitUntilStep(
            "the refrigerated serving completes exactly one depth-aware reheat",
            _ => ObserveCompletedReheat(),
            new EndToEndDeadline(900, 2_500, TimeSpan.FromSeconds(40)));
        yield return new TimeControlActionStep(
            "pause on the reheated RimFridge meal before ingestion",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep(
            "reheated RimFridge meal before native ingestion",
            new[] { dinerId, microwaveId },
            150);
        yield return new TimeControlActionStep(
            "finish native RimFridge dining",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the exact RimFridge setting returns dirty after ingestion",
            _ => DiningCompleted(),
            new EndToEndDeadline(1_800, 6_000, TimeSpan.FromSeconds(70)));
        yield return new TimeControlActionStep(
            "pause on the returned RimFridge setting",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select exact returned RimFridge plate and cutlery",
            new[] { plateId, cutleryId },
            additive: false);
        yield return new CameraActionStep(
            "frame completed RimFridge refrigerator dining",
            new[] { dinerId, fridgeId, microwaveId, plateId, cutleryId },
            220);
        yield return new ScreenshotStep(
            "exact dirty setting after refrigerated RimFridge dining",
            Array.Empty<string>(),
            0);
        yield return new CheckpointStep(
            "RimFridge refrigerator and depth-aware microwave result",
            _ => new Dictionary<string, string>
            {
                ["nativeStorageObserved"] = nativeStorageObserved.ToString(),
                ["nativeCookingObserved"] = nativeCookingObserved.ToString(),
                ["nativeHeatingObserved"] = nativeHeatingObserved.ToString(),
                ["desiredTemperature"] = desiredTemperature.ToString("0.###"),
                ["internalTemperature"] = internalTemperature.ToString("0.###"),
                ["refrigeratedTemperature"] = refrigeratedTemperature.ToString("0.###"),
                ["qualityBeforeReheat"] = qualityBeforeReheat.ToString(),
                ["qualityAfterReheat"] = qualityAfterReheat.ToString(),
                ["plate"] = plateId,
                ["cutlery"] = cutleryId
            });
    }

    private bool ObserveRimFridgeTemperature()
    {
        var refrigerator = fridge.AllComps.SingleOrDefault(comp =>
            string.Equals(comp.GetType().FullName, "RimFridge.CompRefrigerator", StringComparison.Ordinal));
        if (refrigerator is null)
        {
            throw new EndToEndAssertionException(
                "The supported RimFridge lost its exact CompRefrigerator runtime component.");
        }

        var type = refrigerator.GetType();
        desiredTemperature = ReadPublicFloat(type, refrigerator, "desiredTemp");
        internalTemperature = ReadPublicFloat(type, refrigerator, "currentTemp");
        return Math.Abs(desiredTemperature - 5f) < 0.1f &&
               internalTemperature >= 4.9f && internalTemperature <= 6f;
    }

    private bool ObserveNativeStorage()
    {
        ObserveNativeCooking();

        meal ??= map.listerThings.ThingsOfDef(ThingDefOf.MealSimple)
            .OfType<ThingWithComps>()
            .FirstOrDefault(candidate =>
                ReferenceEquals(candidate.GetComp<CompEmbeddedWare>()?.PeekPlateThing(), plate));
        if (meal is null || !meal.Spawned || meal.Position != fridge.Position)
        {
            return false;
        }

        mealId = meal.ThingID;
        EndToEndAssert.True(nativeCookingObserved,
            "The exact RimFridge product must come from the observed native DoBill workflow.");
        nativeStorageObserved = true;
        EndToEndAssert.True(cookware.Spawned && cookware.GetComp<CompSanitation>()?.IsDirty == true,
            "Native RimFridge cooking must return the exact cookware dirty.");
        return true;
    }

    private bool ObserveNativeCooking()
    {
        nativeCookingObserved |= cook.CurJobDef == JobDefOf.DoBill &&
                                 ReferenceEquals(cook.CurJob?.GetTarget(TargetIndex.A).Thing, stove);
        return nativeCookingObserved;
    }

    private bool ObserveNativeHeating()
    {
        var job = diner.CurJob;
        nativeHeatingObserved |= job is not null &&
                                 diner.CurJobDef == JobDefOf.Ingest &&
                                 ReferenceEquals(diner.carryTracker?.CarriedThing, meal) &&
                                 diner.Position == microwave.InteractionCell &&
                                 diner.pather?.Moving != true &&
                                 ReferenceEquals(DiningSessionRegistry.MicrowaveFor(job)?.parent, microwave);
        return nativeHeatingObserved;
    }

    private bool ObserveCompletedReheat()
    {
        if (meal.Destroyed)
        {
            return false;
        }

        var serving = meal.GetComp<CompCulinaryState>()!
            .PeekCurrentServingWithoutThermalUpdate();
        if (serving is null || serving.MicrowaveReheatCount != 1 ||
            Math.Abs(serving.TemperatureCelsius - 60f) >= 0.01f)
        {
            return false;
        }

        qualityAfterReheat = serving.QualityScore;
        EndToEndAssert.Equal(Math.Max(0, qualityBeforeReheat - 7), qualityAfterReheat,
            "A roughly 5-7 C refrigerated serving must lose base microwave quality plus two depth points.");
        return true;
    }

    private bool DiningCompleted()
    {
        if (!meal.Destroyed)
        {
            return false;
        }

        plate = ResolveSpawned(plateId) ?? plate;
        cutlery = ResolveSpawned(cutleryId) ?? cutlery;
        return plate.Spawned && cutlery.Spawned &&
               plate.GetComp<CompSanitation>()?.IsDirty == true &&
               cutlery.GetComp<CompSanitation>()?.IsDirty == true;
    }

    private CulinaryServingRecord CurrentServing() =>
        meal.GetComp<CompCulinaryState>()?.PeekCurrentServing() ??
        throw new EndToEndAssertionException("The exact RimFridge meal lost its culinary serving.");

    private void AssertExactWareBeforeDining()
    {
        EndToEndAssert.Equal(plateId,
            meal.GetComp<CompEmbeddedWare>()?.PeekPlateThing()?.ThingID ?? "missing",
            "RimFridge cooling must preserve the exact embedded plate.");
        EndToEndAssert.True(!plate.GetComp<CompSanitation>()!.IsDirty,
            "RimFridge cooling must preserve the plate's clean state.");
        EndToEndAssert.True(!cutlery.GetComp<CompSanitation>()!.IsDirty,
            "RimFridge cooling must preserve the cutlery's clean state.");
    }

    private ThingWithComps? ResolveSpawned(string thingId) =>
        map.listerThings.AllThings.OfType<ThingWithComps>()
            .SingleOrDefault(candidate => candidate.ThingID == thingId);

    private static float ReadPublicFloat(Type type, object instance, string fieldName)
    {
        var field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public);
        EndToEndAssert.NotNull(field,
            "Supported RimFridge must retain public float field " + fieldName + ".");
        EndToEndAssert.Equal(typeof(float), field!.FieldType,
            "Supported RimFridge field " + fieldName + " must retain its exact float shape.");
        return (float)field.GetValue(instance);
    }

    private static ThingWithComps SpawnBuilding(string defName, IntVec3 cell, Rot4 rotation)
    {
        var def = DefDatabase<ThingDef>.GetNamed(defName);
        var building = (ThingWithComps)ThingMaker.MakeThing(
            def,
            def.MadeFromStuff ? ThingDefOf.Steel : null);
        building.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(building, cell, Current.Game.CurrentMap, rotation);
        return building;
    }

    private static Building_Microwave SpawnCountertopMicrowave(Building table)
    {
        var def = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Microwave");
        var cell = table.OccupiedRect().Cells.OrderBy(candidate => candidate.z).First();
        var placeWorker = new PlaceWorker_MicrowaveCountertop();
        var rotation = new[] { Rot4.North, Rot4.East, Rot4.South, Rot4.West }
            .First(candidate => placeWorker.AllowsPlacing(def, cell, candidate, table.Map).Accepted);
        var microwave = (Building_Microwave)ThingMaker.MakeThing(def);
        microwave.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(microwave, cell, table.Map, rotation);
        return microwave;
    }

    private static Pawn CreateInactiveCook(string name)
    {
        var cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        for (var attempt = 0; attempt < 96; attempt++)
        {
            var pawn = FoodSearchE2EFixture.CreateColonist(name);
            if (pawn.WorkTypeIsDisabled(cooking))
            {
                pawn.Destroy(DestroyMode.Vanish);
                continue;
            }

            foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (!pawn.WorkTypeIsDisabled(workType))
                {
                    pawn.workSettings.SetPriority(workType, 0);
                }
            }

            pawn.skills.GetSkill(SkillDefOf.Cooking).Level = 12;
            FoodSearchE2EFixture.SetHunger(pawn, 1f);
            if (pawn.needs?.rest is { } rest)
            {
                rest.CurLevelPercentage = 1f;
            }

            for (var hour = 0; hour < 24; hour++)
            {
                pawn.timetable?.SetAssignment(hour, TimeAssignmentDefOf.Work);
            }

            return pawn;
        }

        throw new EndToEndAssertionException("Could not generate a capable RimFridge compatibility chef.");
    }

    private static void PreserveSettings(IEndToEndContext context)
    {
        var settings = ImmersiveChefsMod.Settings;
        var priorWareMode = settings.WareRequirementMode;
        var priorTemperatureEnabled = settings.MealTemperatureEnabled;
        var priorHalfLife = settings.ThermalHalfLifeHours;
        var priorMicrowaveThreshold = settings.AutoMicrowaveBelow;
        var priorMicrowaveLoss = settings.MicrowaveQualityLoss;
        var priorStandards = settings.ColonyDiningStandards;
        var priorGodMode = DebugSettings.godMode;
        context.DeferCleanup(() =>
        {
            settings.WareRequirementMode = priorWareMode;
            settings.MealTemperatureEnabled = priorTemperatureEnabled;
            settings.ThermalHalfLifeHours = priorHalfLife;
            settings.AutoMicrowaveBelow = priorMicrowaveThreshold;
            settings.MicrowaveQualityLoss = priorMicrowaveLoss;
            settings.ColonyDiningStandards = priorStandards;
            DebugSettings.godMode = priorGodMode;
        });
        settings.WareRequirementMode = WareRequirementMode.Strict;
        settings.MealTemperatureEnabled = true;
        settings.ThermalHalfLifeHours = 2f;
        settings.AutoMicrowaveBelow = 10f;
        settings.MicrowaveQualityLoss = 5;
        settings.ColonyDiningStandards = false;
        DebugSettings.godMode = true;
    }
}
