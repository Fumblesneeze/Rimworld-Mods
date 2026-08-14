using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.sbz-fridge-passive-storage",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "adaptive.storage.framework",
    "sbz.NeatStorageFridge",
    "fumblesneeze.immersivechefs",
    MaxFrames = 13_000,
    MaxGameTicks = 52_000,
    MaxWallClockSeconds = 520)]
public sealed class SbzFridgeCompatibilityTest : IRimWorldEndToEndTest
{
    private const string SaveName = "ImmersiveChefsSbzFridgeCompatibility";

    private Map map = null!;
    private Pawn diner = null!;
    private ThingWithComps fridge = null!;
    private ThingWithComps meal = null!;
    private ThingWithComps plate = null!;
    private ThingWithComps cutlery = null!;
    private CulinaryServingSnapshot seededServing;
    private CulinaryServingSnapshot cooledServing;
    private CulinaryServingSnapshot warmedServing;
    private string dinerId = string.Empty;
    private string fridgeId = string.Empty;
    private string mealId = string.Empty;
    private string plateId = string.Empty;
    private string cutleryId = string.Empty;
    private bool nativeHaulObserved;
    private bool nativeFlickObserved;
    private bool exactCutleryObserved;
    private float poweredAmbient;
    private float unpoweredAmbient;
    private float thawStartTemperature;
    private int thawStartTick;
    private int thawElapsedTicks;
    private int ordinaryChewDuration;
    private int frozenChewDuration;

    public void Arrange(IEndToEndContext context)
    {
        var phase = "preserve settings";
        try
        {
            PreserveSettings(context);
            var savePath = GenFilePaths.FilePathForSavedGame(SaveName);
            context.DeferCleanup(() =>
            {
                if (File.Exists(savePath))
                {
                    File.Delete(savePath);
                }
            });

            phase = "build powered Adaptive Storage fixture";
            map = Current.Game.CurrentMap;
            var center = FoodSearchE2EFixture.FindRoomCenter(map);
            DispenserE2EFixture.SpawnConduitGrid(map, center, 6, 6);
            DispenserE2EFixture.SpawnPowerSources(map, center + new IntVec3(-4, 0, 4), 2);
            fridge = SpawnBuilding("sbz_Fridge", center + new IntVec3(2, 0, 1), Rot4.North);
            DispenserE2EFixture.SettlePower(map, new[] { fridge }, 400);
            EndToEndAssert.True(
                fridge.GetComp<CompPowerTrader>()?.PowerOn == true,
                "The real [sbz] fridge must join the supplied power net.");
            EndToEndAssert.Equal(
                "AdaptiveStorage.ThingClass",
                fridge.GetType().FullName,
                "The exact [sbz] Def must retain Adaptive Storage's native holder class.");

            phase = "configure native fridge storage";
            var storage = fridge as Building_Storage ??
                          throw new EndToEndAssertionException(
                              "Adaptive Storage's inspected ThingClass no longer derives from Building_Storage.");
            storage.settings.Priority = StoragePriority.Critical;
            storage.settings.filter.SetDisallowAll();
            storage.settings.filter.SetAllow(ThingDefOf.MealSimple, true);

            phase = "spawn inactive native hauler";
            diner = DubsProcessorDishwasherWaterInterruptionTest.CreateInactiveHauler(
                "[sbz] fridge compatibility diner");
            FoodSearchE2EFixture.SetHunger(diner, 0.95f);
            GenSpawn.Spawn(diner, center + new IntVec3(-4, 0, -1), map);

            phase = "seed exact culinary state and embedded plate";
            meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
            meal.stackCount = 1;
            var record = new CulinaryServingRecord(
                qualityScore: 73,
                temperatureCelsius: 21f,
                contamination: ContaminationSources.DirtyPlate,
                microwaveReheatCount: 1,
                lastThermalTick: Math.Max(1, Find.TickManager.TicksGame),
                hiddenSourceDefNames: new[] { "RawRice" },
                hiddenDietaryFlags: DietaryFlags.Plant | DietaryFlags.VegetarianCompatible);
            seededServing = record.Capture();
            meal.GetComp<CompCulinaryState>()!.ReplaceServings(new[] { record });
            meal.GetComp<CompIngredients>()!.RegisterIngredient(
                DefDatabase<ThingDef>.GetNamed("RawRice"));

            plate = FoodSearchE2EFixture.MakeCleanWare(
                "ImmersiveChefs_Plate",
                ThingDefOf.Steel);
            plate.GetComp<CompSanitation>()!.MarkDirty();
            EndToEndAssert.True(
                meal.GetComp<CompEmbeddedWare>()!.TryEmbedPlate(plate),
                "The [sbz] storage fixture must bind its exact dirty plate once.");
            cutlery = FoodSearchE2EFixture.MakeCleanWare(
                "ImmersiveChefs_Cutlery",
                ThingDefOf.Steel);

            GenSpawn.Spawn(meal, center + new IntVec3(-2, 0, 1), map);
            GenSpawn.Spawn(cutlery, center + new IntVec3(0, 0, -2), map);

            dinerId = diner.ThingID;
            fridgeId = fridge.ThingID;
            mealId = meal.ThingID;
            plateId = plate.ThingID;
            cutleryId = cutlery.ThingID;
        }
        catch (EndToEndAssertionException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new EndToEndAssertionException(
                $"[sbz] fridge arrange phase '{phase}' failed with " +
                $"{exception.GetType().FullName}: {exception.Message}");
        }
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var fixture = new[] { dinerId, fridgeId, mealId, cutleryId };
        yield return new TimeControlActionStep(
            "pause before native [sbz] storage",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the plated meal before native [sbz] hauling",
            new[] { mealId },
            additive: false);
        yield return new CameraActionStep("frame [sbz] storage fixture", fixture, 230);
        yield return new ScreenshotStep(
            "before native hauling into powered [sbz] fridge",
            Array.Empty<string>(),
            0);
        yield return new AssertionStep(
            "enable only the native hauling work used by the fixture",
            _ => DubsProcessorDishwasherWaterInterruptionTest.ActivateHauler(diner));
        yield return new TimeControlActionStep(
            "run native hauling into [sbz] fridge",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the pawn physically hauls the exact meal into Adaptive Storage",
            _ => ObserveNativeStorage(),
            new EndToEndDeadline(1_800, 6_000, TimeSpan.FromSeconds(70)));
        yield return new TimeControlActionStep(
            "pause on the native stored meal",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "make the native hauler inert after storage",
            _ =>
            {
                diner.workSettings.SetPriority(WorkTypeDefOf.Hauling, 0);
                diner.jobs.EndCurrentJob(JobCondition.InterruptForced);
                AssertStableState(seededServing, "native [sbz] holder transfer");
                poweredAmbient = meal.AmbientTemperature;
                EndToEndAssert.True(
                    poweredAmbient >= -10.1f && poweredAmbient <= -9.9f,
                    "Adaptive Storage alone must expose [sbz]'s exact -10 C cooling floor; observed " +
                    poweredAmbient.ToString("0.###") + " C.");
            });
        yield return new SelectionActionStep(
            "select the exact meal stored in [sbz] fridge",
            new[] { mealId },
            additive: false);
        yield return new CameraActionStep(
            "frame the exact stored meal and powered [sbz] fridge",
            new[] { fridgeId, mealId },
            220);
        yield return new ScreenshotStep(
            "native plated meal visibly stored in powered [sbz] fridge",
            Array.Empty<string>(),
            0);

        yield return new TimeControlActionStep(
            "run fallback culinary cooling against upstream [sbz] ambient",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the serving freezes toward Adaptive Storage's ambient floor",
            _ => CurrentTemperature() <= -5f,
            new EndToEndDeadline(3_600, 14_000, TimeSpan.FromSeconds(145)));
        yield return new TimeControlActionStep(
            "pause on the upstream-cooled serving",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "capture the exact cooled state before native save/load",
            _ =>
            {
                cooledServing = CurrentServing();
                AssertStableState(seededServing, "powered [sbz] cooling");
            });
        yield return new SelectionActionStep(
            "select the upstream-cooled serving",
            new[] { mealId },
            additive: false);
        yield return new ScreenshotStep(
            "culinary inspector after upstream [sbz] cooling",
            Array.Empty<string>(),
            0);
        yield return new SaveLoadActionStep(
            "save and load the meal in the real Adaptive Storage cell",
            SaveName);
        yield return new AssertionStep(
            "resolve the exact [sbz] holder and culinary state after load",
            _ =>
            {
                ResolveLoadedThings();
                AssertExactState(cooledServing, "native save/load inside [sbz] fridge");
                EndToEndAssert.Equal(fridge.Position, meal.Position,
                    "Native save/load must keep the exact meal in the [sbz] storage cell.");
            });

        var powerToggleCandidates = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { fridgeId }, Array.Empty<string>())
            .Where(option =>
                !option.Disabled &&
                option.Interaction == EndToEndGizmoInteraction.Toggle &&
                option.ToggleState == true &&
                string.Equals(option.HotKeyDefName, "Command_TogglePower", StringComparison.Ordinal))
            .ToArray();
        EndToEndAssert.Equal(
            1,
            powerToggleCandidates.Length,
            "The loaded [sbz] fridge must expose one enabled native power toggle.");
        var powerToggle = powerToggleCandidates[0];
        yield return new SelectionActionStep(
            "select the powered [sbz] fridge before the native toggle",
            new[] { fridgeId },
            additive: false);
        yield return new CameraActionStep(
            "frame the powered [sbz] fridge and its exact meal",
            new[] { fridgeId, mealId },
            220);
        yield return new ScreenshotStep(
            "before native [sbz] fridge power-off action",
            Array.Empty<string>(),
            0);
        yield return new GizmoActionStep(
            "designate [sbz] fridge power-off through its native gizmo",
            new[] { fridgeId },
            powerToggle.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: powerToggle.StableId);
        yield return new InspectPaneCloseActionStep(
            "close Adaptive Storage's force-pausing contents tab after designation",
            fridgeId,
            "AdaptiveStorage.ContentsITab");
        yield return new WindowAcceptActionStep(
            "confirm the exact native power-designation message box",
            "Verse.Dialog_MessageBox");
        yield return new AssertionStep(
            "enable only the native basic work needed to execute the flick designation",
            _ =>
            {
                var flickDesignation = DefDatabase<DesignationDef>.GetNamed("Flick");
                EndToEndAssert.True(
                    map.designationManager.DesignationOn(fridge, flickDesignation) is not null,
                    "Command_TogglePower's native gizmo must create a real Flick designation.");
                var basicWork = DefDatabase<WorkTypeDef>.GetNamed("BasicWorker");
                EndToEndAssert.True(
                    !diner.WorkTypeIsDisabled(basicWork),
                    "The fixture pawn must be capable of native BasicWorker flicking.");
                diner.workSettings.SetPriority(basicWork, 1);
                diner.jobs.EndCurrentJob(JobCondition.InterruptForced);
            });
        var flickJob = DefDatabase<JobDef>.GetNamed("Flick");
        if (diner.CurJobDef == flickJob &&
            ReferenceEquals(diner.CurJob?.targetA.Thing, fridge))
        {
            nativeFlickObserved = true;
        }
        else
        {
            var flickOptions = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
                .Query(dinerId, fridgeId);
            var prioritizeFlick = flickOptions.Where(option =>
                    !option.Disabled &&
                    option.Label.IndexOf("flick", StringComparison.OrdinalIgnoreCase) >= 0)
                .ToArray();
            EndToEndAssert.Equal(
                1,
                prioritizeFlick.Length,
                "Expected one enabled native prioritize-flick action; observed " +
                string.Join(", ", flickOptions.Select(option =>
                    $"'{option.Label}' (disabled={option.Disabled})")));
            yield return new FloatMenuActionStep(
                "prioritize the designated [sbz] power switch through the native menu",
                dinerId,
                fridgeId,
                prioritizeFlick[0].StableId);
        }
        yield return new TimeControlActionStep(
            "let the native [sbz] appliance process its power-off command",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the native [sbz] power toggle disables upstream cooling",
            _ => ObserveUnpoweredAmbient(),
            new EndToEndDeadline(600, 2_000, TimeSpan.FromSeconds(30)));
        yield return new AssertionStep(
            "capture the frozen serving at the start of natural thawing",
            _ =>
            {
                thawStartTemperature = CurrentTemperature();
                thawStartTick = Find.TickManager.TicksGame;
                EndToEndAssert.True(thawStartTemperature <= -5f,
                    "The [sbz] serving must still be deeply Frozen when upstream cooling stops.");
            });
        yield return new TimeControlActionStep(
            "run a bounded natural-thaw interval after upstream cooling is disabled",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "one thousand game ticks elapse without a microwave",
            _ => Find.TickManager.TicksGame >= thawStartTick + 1_000,
            new EndToEndDeadline(600, 1_200, TimeSpan.FromSeconds(40)));
        yield return new TimeControlActionStep(
            "pause on the unpowered warmed serving",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "capture exact state after upstream power transition",
            _ =>
            {
                warmedServing = CurrentServing();
                thawElapsedTicks = Find.TickManager.TicksGame - thawStartTick;
                AssertStableState(seededServing, "unpowered [sbz] warming");
                EndToEndAssert.True(
                    warmedServing.TemperatureCelsius > thawStartTemperature &&
                    warmedServing.TemperatureCelsius <= 0f,
                    "A naturally thawing [sbz] meal must warm but remain Frozen during the bounded interval; " +
                    "observed " + warmedServing.TemperatureCelsius.ToString("0.###") + " C.");
            });
        yield return new SelectionActionStep(
            "select the warmed serving after native [sbz] power-off",
            new[] { mealId },
            additive: false);
        yield return new ScreenshotStep(
            "culinary inspector after native [sbz] power-off",
            Array.Empty<string>(),
            0);

        yield return new AssertionStep(
            "activate hunger only for explicit native retrieval and eating",
            _ => FoodSearchE2EFixture.SetHunger(diner, 0.20f));
        var consumeOptions = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(dinerId, mealId);
        var consume = consumeOptions.Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("consume", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(
            1,
            consume.Length,
            "Expected one enabled native Consume option for the exact [sbz] meal; observed " +
            string.Join(", ", consumeOptions.Select(option =>
                $"'{option.Label}' (disabled={option.Disabled})")));
        yield return new FloatMenuActionStep(
            "order native retrieval and consumption from [sbz] fridge",
            dinerId,
            mealId,
            consume[0].StableId);
        yield return new TimeControlActionStep(
            "run native [sbz] retrieval and ingestion",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the diner retrieves the exact meal and cutlery",
            _ => ObserveNativeRetrieval(),
            new EndToEndDeadline(1_200, 4_000, TimeSpan.FromSeconds(55)));
        yield return new WaitUntilStep(
            "the diner reaches the temperature-extended Frozen chewing toil",
            _ => ObserveFrozenChewing(),
            new EndToEndDeadline(900, 3_000, TimeSpan.FromSeconds(40)));
        yield return new SelectionActionStep(
            "select diner during native [sbz] meal ingestion",
            new[] { dinerId },
            additive: false);
        yield return new ScreenshotStep(
            "native retrieval and ingestion from [sbz] fridge",
            Array.Empty<string>(),
            0);
        yield return new TimeControlActionStep(
            "finish native [sbz] dining",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the exact plate and cutlery return dirty after eating",
            _ => DiningCompleted(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep(
            "pause on exact returned [sbz] setting",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "assert passive [sbz] lifecycle conservation",
            _ => AssertDiningResult());
        yield return new SelectionActionStep(
            "select the [sbz] diner after eating frozen food",
            new[] { dinerId },
            additive: false);
        yield return new PawnInspectTabActionStep(
            "open the native Needs tab for the frozen-food thought",
            dinerId,
            EndToEndPawnInspectTab.Needs);
        yield return new ScreenshotStep(
            "visible stronger Frozen meal memory after [sbz] dining",
            Array.Empty<string>(),
            0);
        yield return new SelectionActionStep(
            "select exact dirty plate and cutlery returned from [sbz] meal",
            new[] { plateId, cutleryId },
            additive: false);
        yield return new CameraActionStep(
            "frame exact returned [sbz] setting",
            new[] { dinerId, fridgeId, plateId, cutleryId },
            220);
        yield return new ScreenshotStep(
            "exact dirty setting after native [sbz] storage dining",
            Array.Empty<string>(),
            0);
        yield return new SelectionActionStep(
            "select the exact returned [sbz] plate for its native inspector",
            new[] { plateId },
            additive: false);
        yield return new ScreenshotStep(
            "exact returned [sbz] plate visibly reports dirty",
            Array.Empty<string>(),
            0);
        yield return new SelectionActionStep(
            "select the exact returned [sbz] cutlery for its native inspector",
            new[] { cutleryId },
            additive: false);
        yield return new ScreenshotStep(
            "exact returned [sbz] cutlery visibly reports dirty",
            Array.Empty<string>(),
            0);
        yield return new CheckpointStep(
            "[sbz] fridge passive compatibility",
            _ => new Dictionary<string, string>
            {
                ["fridgeType"] = fridge.GetType().FullName ?? string.Empty,
                ["nativeHaulObserved"] = nativeHaulObserved.ToString(),
                ["nativeFlickObserved"] = nativeFlickObserved.ToString(),
                ["exactCutleryObserved"] = exactCutleryObserved.ToString(),
                ["poweredAmbient"] = poweredAmbient.ToString("0.###"),
                ["unpoweredAmbient"] = unpoweredAmbient.ToString("0.###"),
                ["cooledTemperature"] = cooledServing.TemperatureCelsius.ToString("0.###"),
                ["warmedTemperature"] = warmedServing.TemperatureCelsius.ToString("0.###"),
                ["naturalThawTicks"] = thawElapsedTicks.ToString(),
                ["ordinaryChewDuration"] = ordinaryChewDuration.ToString(),
                ["frozenChewDuration"] = frozenChewDuration.ToString(),
                ["frozenThoughtStage"] = FrozenTemperatureMemory()?.CurStageIndex.ToString() ?? "missing",
                ["frozenMoodOffset"] = FrozenTemperatureMemory()?.MoodOffset().ToString("0.###") ?? "missing",
                ["plate"] = plateId,
                ["cutlery"] = cutleryId
            });
    }

    private bool ObserveNativeStorage()
    {
        if (diner.CurJobDef == JobDefOf.HaulToCell &&
            ReferenceEquals(diner.carryTracker?.CarriedThing, meal))
        {
            nativeHaulObserved = true;
        }

        return nativeHaulObserved &&
               meal.Spawned &&
               meal.Position == fridge.Position;
    }

    private bool ObserveUnpoweredAmbient()
    {
        if (diner.CurJobDef == DefDatabase<JobDef>.GetNamed("Flick") &&
            ReferenceEquals(diner.CurJob?.targetA.Thing, fridge))
        {
            nativeFlickObserved = true;
        }

        if (fridge.GetComp<CompPowerTrader>()?.PowerOn != false ||
            fridge.GetComp<CompFlickable>()?.SwitchIsOn != false)
        {
            return false;
        }

        unpoweredAmbient = meal.AmbientTemperature;
        return unpoweredAmbient > poweredAmbient + 15f;
    }

    private bool ObserveNativeRetrieval()
    {
        if (meal.Destroyed || diner.CurJobDef != JobDefOf.Ingest)
        {
            return false;
        }

        var mealAcquired = ReferenceEquals(diner.carryTracker?.CarriedThing, meal) ||
                           meal.Position != fridge.Position;
        exactCutleryObserved = !cutlery.Spawned &&
                               ReferenceEquals(cutlery.holdingOwner, diner.inventory?.innerContainer);
        return mealAcquired && exactCutleryObserved;
    }

    private bool ObserveFrozenChewing()
    {
        if (meal.Destroyed || diner.CurJobDef != JobDefOf.Ingest ||
            CurrentToil(diner) is not { defaultCompleteMode: ToilCompleteMode.Delay } current)
        {
            return false;
        }

        var plateSpeed = plate.GetComp<CompKitchenwareStats>()?.CurrentStats.CookingSpeedFactor ?? 1f;
        var nativeMultiplier = meal.def.ingestible.useEatingSpeedStat
            ? 1f / Math.Max(0.01f, diner.GetStatValue(StatDefOf.EatingSpeed))
            : 1f;
        ordinaryChewDuration = (int)Math.Round(
            meal.def.ingestible.baseIngestTicks * nativeMultiplier /
            Math.Max(0.1f, plateSpeed),
            MidpointRounding.AwayFromZero);
        var expectedFrozenDuration = (int)Math.Round(
            ordinaryChewDuration * 1.5f,
            MidpointRounding.AwayFromZero);
        frozenChewDuration = diner.jobs.curDriver.ticksLeftThisToil;
        EndToEndAssert.True(
            ordinaryChewDuration > 0 &&
            frozenChewDuration >= expectedFrozenDuration - 4 &&
            frozenChewDuration <= expectedFrozenDuration,
            "The real Frozen chewing toil must be approximately 1.5 times the plate-adjusted " +
            "ordinary duration; observed ordinary=" + ordinaryChewDuration +
            ", expected frozen=" + expectedFrozenDuration +
            ", observed frozen ticks remaining=" + frozenChewDuration + ".");
        return true;
    }

    private bool DiningCompleted()
    {
        if (!meal.Destroyed)
        {
            return false;
        }

        plate = ResolveSpawned<ThingWithComps>(plateId, required: false) ?? plate;
        cutlery = ResolveSpawned<ThingWithComps>(cutleryId, required: false) ?? cutlery;
        return plate.Spawned &&
               cutlery.Spawned &&
               plate.GetComp<CompSanitation>()?.IsDirty == true &&
               cutlery.GetComp<CompSanitation>()?.IsDirty == true;
    }

    private void AssertDiningResult()
    {
        EndToEndAssert.True(nativeHaulObserved,
            "A real pawn must have performed the native HaulToCell job into [sbz] storage.");
        EndToEndAssert.True(nativeFlickObserved,
            "A real pawn must have performed the native Flick job after the player's power order.");
        EndToEndAssert.True(exactCutleryObserved,
            "Native retrieval must acquire the exact cutlery before ingestion.");
        EndToEndAssert.True(meal.Destroyed,
            "Native ingestion must consume the exact stored serving.");
        EndToEndAssert.Equal(plateId, plate.ThingID,
            "Native [sbz] dining must return the same embedded plate identity.");
        EndToEndAssert.Equal(cutleryId, cutlery.ThingID,
            "Native [sbz] dining must return the same cutlery identity.");
        EndToEndAssert.Equal(1, plate.stackCount,
            "Native [sbz] dining must return exactly one plate unit.");
        EndToEndAssert.Equal(1, cutlery.stackCount,
            "Native [sbz] dining must return exactly one cutlery unit.");
        EndToEndAssert.Equal(1, CountAllWareUnits(plate.def),
            "Native [sbz] storage and dining must neither duplicate nor lose the arranged plate.");
        EndToEndAssert.Equal(1, CountAllWareUnits(cutlery.def),
            "Native [sbz] dining must neither duplicate nor lose the arranged cutlery set.");
        EndToEndAssert.True(plate.GetComp<CompSanitation>()!.IsDirty,
            "The exact returned plate must be dirty after eating.");
        EndToEndAssert.True(cutlery.GetComp<CompSanitation>()!.IsDirty,
            "The exact returned cutlery must be dirty after eating.");
        var frozenMemory = FrozenTemperatureMemory();
        EndToEndAssert.NotNull(frozenMemory,
            "Native ingestion of the still-Frozen [sbz] meal must create the temperature memory.");
        EndToEndAssert.Equal(4, frozenMemory!.CurStageIndex,
            "Native ingestion must select the Frozen temperature thought stage.");
        EndToEndAssert.Equal(-10f, frozenMemory.MoodOffset(),
            "The visible Frozen meal memory must have the stronger -10 mood effect.");
    }

    private Thought_Memory? FrozenTemperatureMemory() =>
        diner.needs?.mood?.thoughts?.memories.GetFirstMemoryOfDef(
            DefDatabase<ThoughtDef>.GetNamed("ImmersiveChefs_MealTemperature"));

    private static Toil? CurrentToil(Pawn pawn)
    {
        var driver = pawn.jobs.curDriver;
        return driver is null
            ? null
            : typeof(JobDriver).GetProperty(
                    "CurToil",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(driver) as Toil;
    }

    private void ResolveLoadedThings()
    {
        map = Current.Game.CurrentMap;
        diner = map.mapPawns.AllPawnsSpawned.SingleOrDefault(pawn => pawn.ThingID == dinerId) ??
                throw new EndToEndAssertionException(
                    "Native save/load lost exact [sbz] diner " + dinerId + ".");
        fridge = ResolveSpawned<ThingWithComps>(fridgeId) ??
                 throw new EndToEndAssertionException(
                     "Native save/load lost exact [sbz] fridge " + fridgeId + ".");
        meal = ResolveSpawned<ThingWithComps>(mealId) ??
               throw new EndToEndAssertionException(
                   "Native save/load lost exact [sbz] meal " + mealId + ".");
        cutlery = ResolveSpawned<ThingWithComps>(cutleryId) ??
                   throw new EndToEndAssertionException(
                       "Native save/load lost exact [sbz] cutlery " + cutleryId + ".");
        plate = meal.GetComp<CompEmbeddedWare>()!.PeekPlateThing() as ThingWithComps ??
                throw new EndToEndAssertionException(
                    "Native save/load lost the exact plate embedded in the [sbz] meal.");
    }

    private T? ResolveSpawned<T>(string thingId, bool required = true) where T : Thing
    {
        var thing = map.listerThings.AllThings.OfType<T>()
            .SingleOrDefault(candidate => candidate.ThingID == thingId);
        if (thing is null && required)
        {
            throw new EndToEndAssertionException("Could not resolve spawned Thing " + thingId + ".");
        }

        return thing;
    }

    private float CurrentTemperature() => CurrentServing().TemperatureCelsius;

    private CulinaryServingSnapshot CurrentServing()
    {
        var record = meal.GetComp<CompCulinaryState>()!.PeekCurrentServing() ??
                     throw new EndToEndAssertionException(
                         "The exact [sbz] meal lost its culinary serving.");
        return record.Capture();
    }

    private CulinaryServingSnapshot CurrentServingWithoutThermalUpdate()
    {
        var record = meal.GetComp<CompCulinaryState>()!
                         .PeekCurrentServingWithoutThermalUpdate() ??
                     throw new EndToEndAssertionException(
                         "The exact [sbz] meal lost its persisted culinary serving.");
        return record.Capture();
    }

    private void AssertStableState(CulinaryServingSnapshot expected, string phase)
    {
        var actual = CurrentServing();
        AssertStableState(expected, actual, phase);
    }

    private void AssertStableState(
        CulinaryServingSnapshot expected,
        CulinaryServingSnapshot actual,
        string phase)
    {
        EndToEndAssert.Equal(expected.QualityScore, actual.QualityScore,
            phase + " must preserve culinary quality.");
        EndToEndAssert.Equal(expected.Contamination, actual.Contamination,
            phase + " must preserve contamination flags.");
        EndToEndAssert.Equal(expected.MicrowaveReheatCount, actual.MicrowaveReheatCount,
            phase + " must preserve microwave history.");
        EndToEndAssert.Equal(
            string.Join("|", expected.HiddenSourceDefNames),
            string.Join("|", actual.HiddenSourceDefNames),
            phase + " must preserve hidden provenance exactly once.");
        EndToEndAssert.Equal(expected.HiddenDietaryFlags, actual.HiddenDietaryFlags,
            phase + " must preserve dietary flags.");
        EndToEndAssert.Equal(expected.CookwareMaterial, actual.CookwareMaterial,
            phase + " must preserve hidden cookware material provenance.");
        AssertWareAndPublicProvenance(phase);
    }

    private void AssertExactState(CulinaryServingSnapshot expected, string phase)
    {
        var actual = CurrentServingWithoutThermalUpdate();
        AssertStableState(expected, actual, phase);
        EndToEndAssert.Equal(expected.TemperatureCelsius, actual.TemperatureCelsius,
            phase + " must preserve the exact culinary temperature.");
        EndToEndAssert.Equal(expected.LastThermalTick, actual.LastThermalTick,
            phase + " must preserve the exact thermal clock.");
    }

    private void AssertWareAndPublicProvenance(string phase)
    {
        EndToEndAssert.Equal(mealId, meal.ThingID,
            phase + " must preserve exact meal identity.");
        EndToEndAssert.Equal(1, meal.stackCount,
            phase + " must preserve exactly one serving.");
        EndToEndAssert.Equal(plateId, plate.ThingID,
            phase + " must preserve exact embedded plate identity.");
        EndToEndAssert.Equal(1, plate.stackCount,
            phase + " must preserve exactly one embedded plate.");
        EndToEndAssert.True(plate.GetComp<CompSanitation>()!.IsDirty,
            phase + " must preserve the plate's dirty sanitation state.");
        EndToEndAssert.Equal(1, CountAllWareUnits(plate.def),
            phase + " must preserve exactly one physical plate across all holders.");
        EndToEndAssert.Equal(1, CountAllWareUnits(cutlery.def),
            phase + " must preserve exactly one physical cutlery set across all holders.");
        EndToEndAssert.True(!cutlery.GetComp<CompSanitation>()!.IsDirty,
            phase + " must preserve the exact cutlery's seeded clean sanitation state before dining.");
        EndToEndAssert.Equal(
            "RawRice",
            string.Join("|", meal.GetComp<CompIngredients>()!.ingredients
                .Select(ingredient => ingredient.defName)),
            phase + " must preserve public ingredient provenance exactly once.");
    }

    private int CountAllWareUnits(ThingDef def)
    {
        var spawned = map.listerThings.ThingsOfDef(def).Sum(thing => thing.stackCount);
        var held = map.mapPawns.AllPawnsSpawned.Sum(pawn =>
            (pawn.inventory?.innerContainer
                 .Where(thing => thing.def == def)
                 .Sum(thing => thing.stackCount) ?? 0) +
            (pawn.carryTracker?.CarriedThing is { } carried && carried.def == def
                ? carried.stackCount
                : 0));
        var embedded = def == plate.def && !meal.Destroyed
            ? meal.GetComp<CompEmbeddedWare>()?.EmbeddedPlateCount ?? 0
            : 0;
        return spawned + held + embedded;
    }

    private static ThingWithComps SpawnBuilding(string defName, IntVec3 cell, Rot4 rotation)
    {
        var def = DefDatabase<ThingDef>.GetNamed(defName);
        var building = (ThingWithComps)ThingMaker.MakeThing(
            def,
            def.MadeFromStuff ? ThingDefOf.Steel : null);
        building.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(building, cell, Current.Game.CurrentMap, rotation);
        if (building.GetComp<CompFlickable>() is { SwitchIsOn: false } flickable)
        {
            flickable.DoFlick();
        }

        return building;
    }

    private static void PreserveSettings(IEndToEndContext context)
    {
        var settings = ImmersiveChefsMod.Settings;
        var priorWareMode = settings.WareRequirementMode;
        var priorTemperatureEnabled = settings.MealTemperatureEnabled;
        var priorHalfLife = settings.ThermalHalfLifeHours;
        var priorMicrowave = settings.AutoMicrowaveBelow;
        var priorStandards = settings.ColonyDiningStandards;
        var priorGodMode = DebugSettings.godMode;
        context.DeferCleanup(() =>
        {
            settings.WareRequirementMode = priorWareMode;
            settings.MealTemperatureEnabled = priorTemperatureEnabled;
            settings.ThermalHalfLifeHours = priorHalfLife;
            settings.AutoMicrowaveBelow = priorMicrowave;
            settings.ColonyDiningStandards = priorStandards;
            DebugSettings.godMode = priorGodMode;
        });
        settings.WareRequirementMode = WareRequirementMode.Strict;
        settings.MealTemperatureEnabled = true;
        settings.ThermalHalfLifeHours = 2f;
        settings.AutoMicrowaveBelow = -10f;
        settings.ColonyDiningStandards = false;
        DebugSettings.godMode = true;
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.adaptive-storage-framework-only",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "adaptive.storage.framework",
    "fumblesneeze.immersivechefs",
    MaxFrames = 5_000,
    MaxGameTicks = 16_000,
    MaxWallClockSeconds = 220)]
public sealed class AdaptiveStorageFrameworkOnlyTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn diner = null!;
    private ThingWithComps meal = null!;
    private ThingWithComps plate = null!;
    private ThingWithComps cutlery = null!;
    private string dinerId = string.Empty;
    private string mealId = string.Empty;
    private string plateId = string.Empty;
    private string cutleryId = string.Empty;
    private bool exactCutleryObserved;

    public void Arrange(IEndToEndContext context)
    {
        FoodSearchE2EFixture.UseStrictNonEmergencyDining(context);
        map = Current.Game.CurrentMap;
        EndToEndAssert.NotNull(
            GenTypes.GetTypeInAnyAssembly("AdaptiveStorage.ThingClass"),
            "The exact framework-only group must load Adaptive Storage's native holder type.");
        EndToEndAssert.True(
            DefDatabase<ThingDef>.GetNamedSilentFail("sbz_Fridge") is null,
            "The framework-only group must not silently admit [sbz] Fridge's Def.");

        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        diner = FoodSearchE2EFixture.CreateColonist("Adaptive Storage incomplete-chain diner");
        FoodSearchE2EFixture.SetHunger(diner, 0.95f);
        GenSpawn.Spawn(diner, center + new IntVec3(-2, 0, 0), map);

        meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        meal.stackCount = 1;
        meal.GetComp<CompCulinaryState>()!.ReplaceServings(new[]
        {
            new CulinaryServingRecord(
                qualityScore: 63,
                temperatureCelsius: 18f,
                contamination: ContaminationSources.None,
                microwaveReheatCount: 0,
                lastThermalTick: Math.Max(1, Find.TickManager.TicksGame),
                hiddenSourceDefNames: new[] { "RawRice" },
                hiddenDietaryFlags: DietaryFlags.Plant | DietaryFlags.VegetarianCompatible)
        });
        meal.GetComp<CompIngredients>()!.RegisterIngredient(
            DefDatabase<ThingDef>.GetNamed("RawRice"));
        plate = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Plate",
            ThingDefOf.Steel);
        EndToEndAssert.True(
            meal.GetComp<CompEmbeddedWare>()!.TryEmbedPlate(plate),
            "The framework-only fixture must bind its exact clean plate once.");
        cutlery = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cutlery",
            ThingDefOf.Steel);
        GenSpawn.Spawn(meal, center, map);
        GenSpawn.Spawn(cutlery, center + new IntVec3(2, 0, 0), map);

        dinerId = diner.ThingID;
        mealId = meal.ThingID;
        plateId = plate.ThingID;
        cutleryId = cutlery.ThingID;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before framework-only native dining",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select framework-only plated meal",
            new[] { mealId },
            additive: false);
        yield return new CameraActionStep(
            "frame framework-only dining fixture",
            new[] { dinerId, mealId, cutleryId },
            220);
        yield return new ScreenshotStep(
            "framework-only plated meal before native Consume",
            Array.Empty<string>(),
            0);
        yield return new AssertionStep(
            "activate hunger only for framework-only native Consume",
            _ => FoodSearchE2EFixture.SetHunger(diner, 0.20f));

        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(dinerId, mealId);
        var consume = options.Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("consume", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(
            1,
            consume.Length,
            "Expected one enabled native framework-only Consume action; observed " +
            string.Join(", ", options.Select(option =>
                $"'{option.Label}' (disabled={option.Disabled})")));
        yield return new FloatMenuActionStep(
            "order framework-only meal through native Consume",
            dinerId,
            mealId,
            consume[0].StableId);
        yield return new TimeControlActionStep(
            "run framework-only native retrieval",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "framework-only diner acquires exact meal and cutlery",
            _ => ObserveRetrieval(),
            new EndToEndDeadline(1_200, 4_000, TimeSpan.FromSeconds(55)));
        yield return new SelectionActionStep(
            "select framework-only diner during ingestion",
            new[] { dinerId },
            additive: false);
        yield return new ScreenshotStep(
            "framework-only native ingestion",
            Array.Empty<string>(),
            0);
        yield return new TimeControlActionStep(
            "finish framework-only native dining",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "framework-only dining returns exact dirty setting",
            _ => DiningCompleted(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep(
            "pause on framework-only returned setting",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "assert framework-only passive dining conservation",
            _ => AssertFinalState());
        yield return new SelectionActionStep(
            "select framework-only returned dirty plate",
            new[] { plateId },
            additive: false);
        yield return new CameraActionStep(
            "frame framework-only returned setting",
            new[] { dinerId, plateId, cutleryId },
            220);
        yield return new ScreenshotStep(
            "framework-only exact plate visibly reports dirty",
            Array.Empty<string>(),
            0);
        yield return new SelectionActionStep(
            "select framework-only returned dirty cutlery",
            new[] { cutleryId },
            additive: false);
        yield return new ScreenshotStep(
            "framework-only exact cutlery visibly reports dirty",
            Array.Empty<string>(),
            0);
        yield return new CheckpointStep(
            "Adaptive Storage framework-only passive compatibility",
            _ => new Dictionary<string, string>
            {
                ["adaptiveStorageTypeLoaded"] = "True",
                ["sbzFridgeDefPresent"] = "False",
                ["exactCutleryObserved"] = exactCutleryObserved.ToString(),
                ["plate"] = plateId,
                ["cutlery"] = cutleryId
            });
    }

    private bool ObserveRetrieval()
    {
        if (meal.Destroyed || diner.CurJobDef != JobDefOf.Ingest)
        {
            return false;
        }

        exactCutleryObserved = !cutlery.Spawned &&
                               ReferenceEquals(cutlery.holdingOwner, diner.inventory?.innerContainer);
        return exactCutleryObserved &&
               (ReferenceEquals(diner.carryTracker?.CarriedThing, meal) ||
                !meal.Spawned);
    }

    private bool DiningCompleted()
    {
        if (!meal.Destroyed)
        {
            return false;
        }

        plate = ResolveSpawned(plateId) ?? plate;
        cutlery = ResolveSpawned(cutleryId) ?? cutlery;
        return plate.Spawned &&
               cutlery.Spawned &&
               plate.GetComp<CompSanitation>()?.IsDirty == true &&
               cutlery.GetComp<CompSanitation>()?.IsDirty == true;
    }

    private void AssertFinalState()
    {
        EndToEndAssert.True(exactCutleryObserved,
            "Framework-only native dining must acquire the exact arranged cutlery.");
        EndToEndAssert.True(meal.Destroyed,
            "Framework-only native Consume must destroy the exact serving.");
        EndToEndAssert.Equal(plateId, plate.ThingID,
            "Framework-only dining must return the same embedded plate identity.");
        EndToEndAssert.Equal(cutleryId, cutlery.ThingID,
            "Framework-only dining must return the same cutlery identity.");
        EndToEndAssert.Equal(1, CountAllWareUnits(plate.def),
            "Framework-only dining must conserve exactly one plate unit.");
        EndToEndAssert.Equal(1, CountAllWareUnits(cutlery.def),
            "Framework-only dining must conserve exactly one cutlery unit.");
        EndToEndAssert.True(plate.GetComp<CompSanitation>()!.IsDirty,
            "Framework-only dining must visibly return the exact plate dirty.");
        EndToEndAssert.True(cutlery.GetComp<CompSanitation>()!.IsDirty,
            "Framework-only dining must visibly return the exact cutlery dirty.");
        EndToEndAssert.True(
            DefDatabase<ThingDef>.GetNamedSilentFail("sbz_Fridge") is null,
            "Framework-only dining must remain independent of an absent [sbz] Def.");
    }

    private ThingWithComps? ResolveSpawned(string thingId) =>
        map.listerThings.AllThings.OfType<ThingWithComps>()
            .SingleOrDefault(candidate => candidate.ThingID == thingId);

    private int CountAllWareUnits(ThingDef def)
    {
        var spawned = map.listerThings.ThingsOfDef(def).Sum(thing => thing.stackCount);
        var held = map.mapPawns.AllPawnsSpawned.Sum(pawn =>
            (pawn.inventory?.innerContainer
                 .Where(thing => thing.def == def)
                 .Sum(thing => thing.stackCount) ?? 0) +
            (pawn.carryTracker?.CarriedThing is { } carried && carried.def == def
                ? carried.stackCount
                : 0));
        var embedded = def == plate.def && !meal.Destroyed
            ? meal.GetComp<CompEmbeddedWare>()?.EmbeddedPlateCount ?? 0
            : 0;
        return spawned + held + embedded;
    }
}
