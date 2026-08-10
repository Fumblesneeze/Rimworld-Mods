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
    "immersive-chefs.rimfridge-thermodynamics-dining",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "rimfridge.kv.rw",
    "Mlie.DThermodynamicsHotMeals",
    "fumblesneeze.immersivechefs",
    MaxFrames = 5_000,
    MaxGameTicks = 20_000,
    MaxWallClockSeconds = 150)]
public sealed class ThermodynamicsRimFridgeDiningTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn cook = null!;
    private Pawn diner = null!;
    private ThingWithComps stove = null!;
    private ThingWithComps fridge = null!;
    private ThingWithComps microwave = null!;
    private ThingWithComps cookware = null!;
    private ThingWithComps plate = null!;
    private ThingWithComps cutlery = null!;
    private ThingWithComps? meal;
    private int cookedQuality;
    private float inertImmersiveTemperature;
    private int inertImmersiveReheatCount;
    private int inertImmersiveThermalTick;
    private double coldThermodynamicsTemperature;
    private double heatedThermodynamicsTemperature;
    private bool nativeCookingObserved;
    private bool nativeFridgeStorageObserved;
    private bool exactMealRetrievedObserved;
    private bool thermodynamicsHeatingObserved;
    private EndToEndGizmoOption draftToggle = null!;

    public void Arrange(IEndToEndContext context)
    {
        var phase = "resolve current map";
        try
        {
            map = Current.Game.CurrentMap;
            phase = "build fixture room";
            var center = FoodSearchE2EFixture.FindRoomCenter(map);
            FoodSearchE2EFixture.BuildSealedRoom(map, center);
            FoodSearchE2EFixture.UseStrictNonEmergencyDining(context);

            phase = "configure Immersive Chefs fixture settings";
            var priorAutoAssistants = ImmersiveChefsMod.Settings.AutoCallAssistants;
            ImmersiveChefsMod.Settings.AutoCallAssistants = false;
            context.DeferCleanup(() => ImmersiveChefsMod.Settings.AutoCallAssistants = priorAutoAssistants);

            phase = "verify finalized temperature ownership";
            EndToEndAssert.False(
                TemperatureOwnership.ImmersiveChefsFeaturesActive,
                "The exact Thermodynamics package must suppress every Immersive Chefs thermal feature.");
            EndToEndAssert.NotNull(
                DefDatabase<ThingDef>.GetNamedSilentFail("DMicrowave"),
                "Thermodynamics must retain its DMicrowave Def.");
            EndToEndAssert.NotNull(
                DefDatabase<JobDef>.GetNamedSilentFail("HeatMeal"),
                "Thermodynamics must retain its HeatMeal job Def.");
            EndToEndAssert.True(
                DefDatabase<ThingDef>.GetNamedSilentFail("ImmersiveChefs_Microwave") is null,
                "The fallback microwave must not be constructed in the exact Thermodynamics process.");
            phase = "configure observable Thermodynamics heating speed";
            ConfigureThermodynamicsHeatingSpeed(context, 0.1f);

            phase = "spawn shared power grid";
            DispenserE2EFixture.SpawnConduitGrid(map, center, 5, 5);
            DispenserE2EFixture.SpawnPowerSources(map, center + new IntVec3(-4, 0, 4), 2);
            phase = "spawn fueled stove";
            stove = SpawnBuilding("FueledStove", center, Rot4.North);
            stove.GetComp<CompRefuelable>()?.Refuel(999f);
            phase = "spawn RimFridge";
            fridge = SpawnBuilding("RimFridge_SingleRefrigerator", center + new IntVec3(3, 0, 2), Rot4.North);
            phase = "spawn Thermodynamics microwave";
            microwave = SpawnBuilding("DMicrowave", center + new IntVec3(-3, 0, 2), Rot4.North);
            phase = "settle shared power net";
            DispenserE2EFixture.SettlePower(map, new[] { fridge, microwave }, 400);

            phase = "configure RimFridge storage";
            var storage = (Building_Storage)fridge;
            storage.settings.Priority = StoragePriority.Critical;
            storage.settings.filter.SetAllow(ThingDefOf.MealSimple, true);

            phase = "spawn chef";
            cook = GenerateCook("Thermodynamics compatibility chef");
            GenSpawn.Spawn(cook, center + new IntVec3(0, 0, -3), map);
            phase = "spawn drafted diner";
            diner = FoodSearchE2EFixture.CreateColonist("Thermodynamics compatibility diner");
            FoodSearchE2EFixture.SetHunger(diner, 0.05f);
            GenSpawn.Spawn(diner, center + new IntVec3(4, 0, -3), map);
            diner.drafter.Drafted = true;

            phase = "capture native diner draft toggle";
            var draftCandidates = context.GetRequiredService<IEndToEndGizmoCatalog>()
                .Query(new[] { diner.ThingID }, Array.Empty<string>())
                .Where(option =>
                    !option.Disabled &&
                    option.Interaction == EndToEndGizmoInteraction.Toggle &&
                    option.ToggleState == true &&
                    string.Equals(option.HotKeyDefName, "Command_ColonistDraft", StringComparison.Ordinal))
                .ToArray();
            EndToEndAssert.Equal(
                1,
                draftCandidates.Length,
                "The drafted Thermodynamics diner must expose one enabled native draft toggle.");
            draftToggle = draftCandidates[0];

            phase = "create native Simple-meal bill";
            var recipe = DefDatabase<RecipeDef>.GetNamed("CookMealSimple");
            var bill = new Bill_Production(recipe)
            {
                repeatMode = BillRepeatModeDefOf.RepeatCount,
                repeatCount = 1,
                ingredientSearchRadius = 9f
            };
            bill.SetStoreMode(BillStoreModeDefOf.BestStockpile);
            bill.SetPawnRestriction(cook);
            ((IBillGiver)stove).BillStack.AddBill(bill);

            phase = "spawn ingredients and service ware";
            var rice = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RawRice"));
            rice.stackCount = 50;
            GenSpawn.Spawn(rice, center + new IntVec3(3, 0, -2), map);
            cookware = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Cookware", ThingDefOf.Steel);
            plate = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Plate", ThingDefOf.Steel);
            cutlery = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Cutlery", ThingDefOf.Steel);
            GenSpawn.Spawn(cookware, center + new IntVec3(-1, 0, -2), map);
            GenSpawn.Spawn(plate, center + new IntVec3(-2, 0, -2), map);
            GenSpawn.Spawn(cutlery, center + new IntVec3(2, 0, -2), map);

            phase = "verify finalized meal components";
            EndToEndAssert.True(
                ThingDefOf.MealSimple.comps.Any(properties =>
                    string.Equals(
                        properties.compClass?.FullName,
                        "DHotMeals.Comps.CompDFoodTemperature",
                        StringComparison.Ordinal)),
                "The finalized simple meal must retain Thermodynamics' food-temperature comp.");
            EndToEndAssert.True(
                ThingDefOf.MealSimple.comps.Any(properties => properties.compClass == typeof(CompEmbeddedWare)) &&
                ThingDefOf.MealSimple.comps.Any(properties => properties.compClass == typeof(CompCulinaryState)),
                "The same finalized simple meal must retain Immersive Chefs ware and culinary comps.");
        }
        catch (EndToEndAssertionException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new EndToEndAssertionException(
                $"Thermodynamics fixture arrange phase '{phase}' failed with " +
                $"{exception.GetType().FullName}: {exception.Message}");
        }
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var initialTargets = new[]
        {
            cook.ThingID, diner.ThingID, stove.ThingID, fridge.ThingID, microwave.ThingID,
            cookware.ThingID, plate.ThingID, cutlery.ThingID
        };
        yield return new SelectionActionStep(
            "select exact thermal-storage fixture",
            initialTargets,
            additive: false);
        yield return new CameraActionStep(
            "frame exact thermal-storage fixture",
            initialTargets,
            paddingPixels: 180);
        yield return new ScreenshotStep(
            "before native Thermodynamics compatibility cooking",
            initialTargets,
            paddingPixels: 160);
        yield return new TimeControlActionStep(
            "run the ordinary Simple-meal bill",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the chef enters the native cooking bill",
            _ => ObserveNativeCooking(),
            new EndToEndDeadline(1_200, 4_000, TimeSpan.FromSeconds(45)));
        yield return new WaitUntilStep(
            "the native bill stores its plated meal in RimFridge",
            _ => ObserveNativeFridgeStorage(),
            new EndToEndDeadline(2_500, 10_000, TimeSpan.FromSeconds(90)));
        yield return new SelectionActionStep(
            "select the plated meal stored in RimFridge",
            new[] { meal!.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the RimFridge meal and Thermodynamics microwave",
            new[] { meal!.ThingID, fridge.ThingID, microwave.ThingID },
            paddingPixels: 180);
        yield return new ScreenshotStep(
            "native plated meal stored in RimFridge",
            new[] { meal!.ThingID, fridge.ThingID },
            paddingPixels: 150);
        yield return new WaitUntilStep(
            "Thermodynamics cools the stored meal inside RimFridge",
            _ => ObserveColdStoredMeal(),
            new EndToEndDeadline(2_500, 8_000, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep(
            "pause on the Thermodynamics-owned cold meal state",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the Thermodynamics-cooled meal",
            new[] { meal!.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "Thermodynamics temperature UI on the RimFridge meal",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "only Thermodynamics exposes temperature on the cold meal",
            _ => AssertSingleTemperatureOwner());

        var floatMenu = context.GetRequiredService<IEndToEndFloatMenuCatalog>();
        var options = floatMenu.Query(diner.ThingID, meal!.ThingID);
        var heatOptions = options.Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("heat", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        var consumeOptions = options.Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("consume", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(
            1,
            heatOptions.Length,
            "Thermodynamics must expose one enabled native Heat meal action on the cold stored meal.");
        EndToEndAssert.Equal(
            1,
            consumeOptions.Length,
            "The cold plated meal must retain one enabled native Consume action.");

        yield return new FloatMenuActionStep(
            "order Thermodynamics native heating of the cold RimFridge meal",
            diner.ThingID,
            meal!.ThingID,
            heatOptions[0].StableId);
        yield return new TimeControlActionStep(
            "run the Thermodynamics HeatMeal job",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the diner visibly heats the exact meal at DMicrowave",
            _ => ObserveThermodynamicsHeating(),
            new EndToEndDeadline(1_200, 3_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause during Thermodynamics heating",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select diner heating at DMicrowave",
            new[] { diner.ThingID, microwave.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "Thermodynamics heating the plated meal at DMicrowave",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "Thermodynamics heating leaves Immersive culinary state inert",
            _ => AssertCulinaryStateUnchangedDuringHeating());
        yield return new TimeControlActionStep(
            "finish the native Thermodynamics HeatMeal job",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "Thermodynamics finishes heating the exact meal",
            _ => meal is not null &&
                 !meal.Destroyed &&
                 ReadThermodynamicsTemperature(meal) >= 50d &&
                 !string.Equals(diner.CurJobDef?.defName, "HeatMeal", StringComparison.Ordinal),
            new EndToEndDeadline(1_200, 4_000, TimeSpan.FromSeconds(45)));
        yield return new AssertionStep(
            "completed Thermodynamics heating leaves Immersive culinary state inert",
            _ => AssertCulinaryStateUnchangedDuringHeating());
        yield return new TimeControlActionStep(
            "pause before ordinary dining",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new GizmoActionStep(
            "undraft the heated-meal diner",
            new[] { diner.ThingID },
            draftToggle.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: draftToggle.StableId);
        yield return new TimeControlActionStep(
            "let ordinary hunger AI eat the heated meal",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the diner begins ordinary heated-meal ingestion",
            _ => diner.CurJobDef == JobDefOf.Ingest,
            new EndToEndDeadline(900, 3_000, TimeSpan.FromSeconds(40)));
        yield return new WaitUntilStep(
            "the native dining job consumes the exact heated meal",
            _ => meal!.Destroyed,
            new EndToEndDeadline(2_000, 7_000, TimeSpan.FromSeconds(70)));
        yield return new TimeControlActionStep(
            "pause after exact thermal-storage dining",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "exact service ware returns dirty once without duplicate temperature effects",
            _ => AssertCompletedDining());
        yield return new SelectionActionStep(
            "select exact returned plate and cutlery",
            new[] { plate.ThingID, cutlery.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame returned setting beside Thermodynamics fixture",
            new[] { diner.ThingID, plate.ThingID, cutlery.ThingID, fridge.ThingID, microwave.ThingID },
            paddingPixels: 180);
        yield return new ScreenshotStep(
            "exact dirty setting after Thermodynamics dining",
            new[] { plate.ThingID, cutlery.ThingID, diner.ThingID },
            paddingPixels: 160);
        yield return new CheckpointStep(
            "RimFridge Thermodynamics dining result",
            _ => new Dictionary<string, string>
            {
                ["nativeCookingObserved"] = nativeCookingObserved.ToString(),
                ["nativeFridgeStorageObserved"] = nativeFridgeStorageObserved.ToString(),
                ["exactMealRetrievedObserved"] = exactMealRetrievedObserved.ToString(),
                ["thermodynamicsHeatingObserved"] = thermodynamicsHeatingObserved.ToString(),
                ["coldThermodynamicsTemperature"] = coldThermodynamicsTemperature.ToString("0.###"),
                ["heatedThermodynamicsTemperature"] = heatedThermodynamicsTemperature.ToString("0.###"),
                ["culinaryQuality"] = cookedQuality.ToString(),
                ["plateThingId"] = plate.ThingID,
                ["cutleryThingId"] = cutlery.ThingID,
                ["plateDirty"] = (plate.GetComp<CompSanitation>()?.IsDirty == true).ToString(),
                ["cutleryDirty"] = (cutlery.GetComp<CompSanitation>()?.IsDirty == true).ToString()
            });
    }

    private ThingWithComps SpawnBuilding(string defName, IntVec3 cell, Rot4 rotation)
    {
        var def = DefDatabase<ThingDef>.GetNamed(defName);
        var building = (ThingWithComps)ThingMaker.MakeThing(
            def,
            def.MadeFromStuff ? ThingDefOf.Steel : null);
        building.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(building, cell, map, rotation);
        if (building.GetComp<CompFlickable>() is { SwitchIsOn: false } flick)
        {
            flick.DoFlick();
        }

        return building;
    }

    private static Pawn GenerateCook(string name)
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

            HumanlikePawnFixture.SetName(pawn, name);
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
            pawn.skills.GetSkill(SkillDefOf.Cooking).Level = 16;
            for (var hour = 0; hour < 24; hour++)
            {
                pawn.timetable?.SetAssignment(hour, TimeAssignmentDefOf.Work);
            }

            FoodSearchE2EFixture.SetHunger(pawn, 1f);
            if (pawn.needs?.rest is { } rest)
            {
                rest.CurLevelPercentage = 1f;
            }

            return pawn;
        }

        throw new EndToEndAssertionException("Could not generate a capable Thermodynamics compatibility chef.");
    }

    private bool ObserveNativeCooking()
    {
        if (cook.CurJobDef != JobDefOf.DoBill ||
            !ReferenceEquals(cook.CurJob?.GetTarget(TargetIndex.A).Thing, stove))
        {
            return false;
        }

        nativeCookingObserved = true;
        return true;
    }

    private bool ObserveNativeFridgeStorage()
    {
        meal ??= map.listerThings.ThingsOfDef(ThingDefOf.MealSimple)
            .OfType<ThingWithComps>()
            .FirstOrDefault(candidate => candidate.GetComp<CompEmbeddedWare>()?.EmbeddedPlateCount == 1);
        if (meal is null || !meal.Spawned || meal.Position != fridge.Position)
        {
            return false;
        }

        var embedded = meal.GetComp<CompEmbeddedWare>();
        EndToEndAssert.True(
            ReferenceEquals(embedded?.PeekPlateThing(), plate),
            "The meal stored in RimFridge must retain the exact plate reserved by native cooking.");
        EndToEndAssert.True(
            cookware.Spawned && cookware.GetComp<CompSanitation>()?.IsDirty == true,
            "Native cooking must return the exact cookware dirty before storage.");
        EndToEndAssert.NotNull(
            FindThermodynamicsComp(meal),
            "The cooked meal must retain Thermodynamics' concrete temperature comp.");

        var serving = meal.GetComp<CompCulinaryState>()?.PeekCurrentServingWithoutThermalUpdate();
        EndToEndAssert.NotNull(serving, "Native cooking must create one Immersive Chefs culinary serving.");
        cookedQuality = serving!.QualityScore;
        inertImmersiveTemperature = serving.TemperatureCelsius;
        inertImmersiveReheatCount = serving.MicrowaveReheatCount;
        inertImmersiveThermalTick = serving.LastThermalTick;
        nativeFridgeStorageObserved = true;
        return true;
    }

    private bool ObserveColdStoredMeal()
    {
        if (meal is null || meal.Destroyed || !meal.Spawned || meal.Position != fridge.Position)
        {
            return false;
        }

        var temperature = ReadThermodynamicsTemperature(meal);
        if (temperature > 30d)
        {
            return false;
        }

        coldThermodynamicsTemperature = temperature;
        return true;
    }

    private void AssertSingleTemperatureOwner()
    {
        EndToEndAssert.NotNull(meal, "The RimFridge meal must exist before temperature inspection.");
        var thermodynamicsComp = FindThermodynamicsComp(meal!);
        EndToEndAssert.NotNull(
            thermodynamicsComp,
            "Thermodynamics must remain the concrete temperature-comp owner on the stored meal.");
        var thermodynamicsInspect = thermodynamicsComp!.CompInspectStringExtra();
        EndToEndAssert.True(
            !string.IsNullOrWhiteSpace(thermodynamicsInspect),
            "Thermodynamics must expose its ordinary temperature/state inspector text.");
        var immersiveInspect = meal!.GetComp<CompCulinaryState>()?.CompInspectStringExtra() ?? string.Empty;
        EndToEndAssert.True(
            immersiveInspect.IndexOf("Culinary quality", StringComparison.OrdinalIgnoreCase) >= 0,
            "Immersive Chefs must retain its culinary-quality inspector alongside Thermodynamics.");
        EndToEndAssert.True(
            immersiveInspect.IndexOf("Meal temperature", StringComparison.OrdinalIgnoreCase) < 0,
            "Immersive Chefs must not expose a duplicate meal-temperature inspector.");
    }

    private bool ObserveThermodynamicsHeating()
    {
        if (meal is null || meal.Destroyed ||
            !string.Equals(diner.CurJobDef?.defName, "HeatMeal", StringComparison.Ordinal))
        {
            return false;
        }

        var temperature = ReadThermodynamicsTemperature(meal);
        if (temperature <= coldThermodynamicsTemperature + 0.5d)
        {
            return false;
        }

        EndToEndAssert.True(
            ReferenceEquals(diner.CurJob?.GetTarget(TargetIndex.C).Thing, microwave),
            "Thermodynamics' native HeatMeal job must target the exact DMicrowave.");
        EndToEndAssert.True(
            ReferenceEquals(diner.carryTracker?.CarriedThing, meal),
            "Thermodynamics must physically retrieve and carry the exact plated meal from RimFridge.");
        EndToEndAssert.True(
            diner.Position == microwave.InteractionCell || diner.Position.AdjacentTo8Way(microwave.Position),
            "The diner must visibly stand at the Thermodynamics microwave while its meal heats.");
        heatedThermodynamicsTemperature = temperature;
        exactMealRetrievedObserved = true;
        thermodynamicsHeatingObserved = true;
        return true;
    }

    private void AssertCulinaryStateUnchangedDuringHeating()
    {
        EndToEndAssert.NotNull(meal, "The exact meal must still exist during Thermodynamics heating.");
        var serving = meal!.GetComp<CompCulinaryState>()?.PeekCurrentServingWithoutThermalUpdate();
        EndToEndAssert.NotNull(serving, "Thermodynamics heating must retain the culinary serving.");
        EndToEndAssert.Equal(cookedQuality, serving!.QualityScore,
            "Thermodynamics heating must not apply Immersive Chefs microwave quality loss.");
        EndToEndAssert.Equal(inertImmersiveTemperature, serving.TemperatureCelsius,
            "Thermodynamics heating must not rewrite the inert Immersive Chefs temperature field.");
        EndToEndAssert.Equal(inertImmersiveReheatCount, serving.MicrowaveReheatCount,
            "Thermodynamics heating must not increment the Immersive Chefs reheat count.");
        EndToEndAssert.Equal(inertImmersiveThermalTick, serving.LastThermalTick,
            "Thermodynamics heating must not advance the Immersive Chefs thermal clock.");
        EndToEndAssert.True(
            ReferenceEquals(meal.GetComp<CompEmbeddedWare>()?.PeekPlateThing(), plate),
            "The exact embedded plate must survive Thermodynamics heating.");
    }

    private void AssertCompletedDining()
    {
        EndToEndAssert.True(nativeCookingObserved, "The native cooking bill must have been observed.");
        EndToEndAssert.True(nativeFridgeStorageObserved, "The exact plated meal must have been stored in RimFridge.");
        EndToEndAssert.True(exactMealRetrievedObserved,
            "Thermodynamics must have physically retrieved the exact plated meal from RimFridge.");
        EndToEndAssert.True(thermodynamicsHeatingObserved, "Thermodynamics heating at DMicrowave must be observed.");
        EndToEndAssert.True(meal?.Destroyed == true, "The heated plated meal must be consumed exactly once.");
        EndToEndAssert.True(
            plate.Spawned && plate.GetComp<CompSanitation>()?.IsDirty == true,
            "The same physical cooking plate must return dirty after ingestion.");
        EndToEndAssert.True(
            cutlery.Spawned && cutlery.GetComp<CompSanitation>()?.IsDirty == true,
            "The same physical cutlery must return dirty after ingestion.");
        EndToEndAssert.Equal(
            1,
            map.listerThings.ThingsOfDef(plate.def).Count,
            "The exact process must contain one returned plate without duplication.");
        EndToEndAssert.Equal(
            1,
            map.listerThings.ThingsOfDef(cutlery.def).Count,
            "The exact process must contain one returned cutlery setting without duplication.");

        var memories = diner.needs?.mood?.thoughts?.memories;
        EndToEndAssert.NotNull(memories, "The humanlike diner must retain ordinary mood memories.");
        var immersiveTemperature = DefDatabase<ThoughtDef>.GetNamed("ImmersiveChefs_MealTemperature");
        EndToEndAssert.True(
            memories!.GetFirstMemoryOfDef(immersiveTemperature) is null,
            "Thermodynamics ownership must suppress the Immersive Chefs temperature memory.");
        var thermodynamicsThoughts = new[] { "DAteGoodThing", "DAteTooHot", "DAteTooCold", "DAteMeh" }
            .Select(defName => DefDatabase<ThoughtDef>.GetNamed(defName))
            .ToArray();
        var thermodynamicsMemoryCount = memories.Memories.Count(memory =>
            thermodynamicsThoughts.Contains(memory.def));
        EndToEndAssert.Equal(
            1,
            thermodynamicsMemoryCount,
            "Thermodynamics must add exactly one of its temperature memories.");
        EndToEndAssert.NotNull(
            memories.GetFirstMemoryOfDef(DefDatabase<ThoughtDef>.GetNamed("ImmersiveChefs_CulinaryQuality")),
            "Immersive Chefs culinary quality must remain active alongside Thermodynamics.");
        EndToEndAssert.NotNull(
            memories.GetFirstMemoryOfDef(DefDatabase<ThoughtDef>.GetNamed("ImmersiveChefs_DiningExperience")),
            "Immersive Chefs tableware dining effects must remain active alongside Thermodynamics.");
    }

    private static ThingComp? FindThermodynamicsComp(ThingWithComps thing) =>
        thing.AllComps.FirstOrDefault(comp => string.Equals(
            comp.GetType().FullName,
            "DHotMeals.Comps.CompDFoodTemperature",
            StringComparison.Ordinal));

    private static double ReadThermodynamicsTemperature(ThingWithComps thing)
    {
        var comp = FindThermodynamicsComp(thing) ??
                   throw new EndToEndAssertionException(
                       "The exact meal lost Thermodynamics' CompDFoodTemperature.");
        var field = comp.GetType().GetField(
            "curTemp",
            BindingFlags.Instance | BindingFlags.Public) ??
                    throw new EndToEndAssertionException(
                        "Thermodynamics' public curTemp field changed shape.");
        return Convert.ToDouble(field.GetValue(comp));
    }

    private static void ConfigureThermodynamicsHeatingSpeed(
        IEndToEndContext context,
        float speed)
    {
        var settingsType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType(
                "DHotMeals.HotMealsSettings",
                throwOnError: false))
            .FirstOrDefault(type => type is not null) ??
            throw new EndToEndAssertionException(
                "Could not resolve Thermodynamics' loaded HotMealsSettings type.");
        var field = settingsType.GetField(
            "heatSpeedMult",
            BindingFlags.Public | BindingFlags.Static) ??
            throw new EndToEndAssertionException(
                "Thermodynamics' public heatSpeedMult setting changed shape.");
        var original = Convert.ToSingle(field.GetValue(null));
        field.SetValue(null, speed);
        context.DeferCleanup(() => field.SetValue(null, original));
    }
}
