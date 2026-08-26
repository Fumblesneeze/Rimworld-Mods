using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.native-fallback-meal-heating-chain",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "fumblesneeze.immersivechefs",
    MaxFrames = 8_000,
    MaxGameTicks = 32_000,
    MaxWallClockSeconds = 240)]
public sealed class NativeFallbackMealHeatingChainTest : IRimWorldEndToEndTest
{
    private FallbackMealHeatingFixture fixture = null!;

    public void Arrange(IEndToEndContext context)
    {
        fixture = FallbackMealHeatingFixture.Create(context);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "let RimWorld finalize the constructed room and start the Core heater",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return fixture.WaitForFallbackEmitters(
            "the enclosed campfire and powered Core heater begin actually emitting heat");
        yield return new TimeControlActionStep(
            "pause after the real heater emitter becomes available",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return fixture.FrameCurrentStage("observe stove, campfire, heater, and frozen meal before food search");
        yield return fixture.ToggleDraft(context, "undraft diner to choose the best heating source", true);
        yield return new TimeControlActionStep(
            "run native stove fallback",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return fixture.WaitForActiveHeating(
            "native ingest job reaches stove heating despite closer lower-tier sources",
            MealHeatingSourceKind.Stove);
        yield return fixture.ScreenshotCurrentStage("observe frozen meal heating at the preferred stove");
        yield return fixture.WaitForCompletedHeating(
            "stove heats the carried meal to its profile without microwave risk",
            MealHeatingSourceKind.Stove);
        yield return new TimeControlActionStep(
            "finish stove-heated dining",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return fixture.WaitForConsumed("stove-heated meal is consumed by the admitted ingest job");

        yield return fixture.ToggleDraft(context, "draft diner before changing fallback availability", false);
        yield return new AssertionStep("remove stove and prepare the campfire fallback", _ =>
            fixture.PrepareNextStage(
                MealHeatingSourceKind.Stove,
                MealHeatingSourceKind.Campfire,
                -5f));
        yield return fixture.ToggleDraft(context, "undraft diner for native campfire fallback", true);
        yield return new TimeControlActionStep(
            "run native campfire fallback",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return fixture.WaitForActiveHeating(
            "native ingest job reaches campfire heating before the heater",
            MealHeatingSourceKind.Campfire);
        yield return fixture.ScreenshotCurrentStage("observe frozen meal heating over the campfire");
        yield return fixture.WaitForCompletedHeating(
            "campfire heats more slowly with greater quality loss",
            MealHeatingSourceKind.Campfire);
        yield return new TimeControlActionStep(
            "finish campfire-heated dining",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return fixture.WaitForConsumed("campfire-heated meal is consumed by the admitted ingest job");

        yield return fixture.ToggleDraft(context, "draft diner before the last-resort heater stage", false);
        yield return new AssertionStep("remove campfire and prepare the heater fallback", _ =>
            fixture.PrepareNextStage(
                MealHeatingSourceKind.Campfire,
                MealHeatingSourceKind.AmbientHeater,
                -5f));
        yield return fixture.ToggleDraft(context, "undraft diner for native heater fallback", true);
        yield return new TimeControlActionStep(
            "run native heater fallback",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return fixture.WaitForActiveHeating(
            "native ingest job reaches the active heater only as a last resort",
            MealHeatingSourceKind.AmbientHeater);
        yield return fixture.ScreenshotCurrentStage("observe slow last-resort thawing at the heater");
        yield return fixture.WaitForCompletedHeating(
            "heater stops at twenty degrees with the greatest quality loss",
            MealHeatingSourceKind.AmbientHeater);
        yield return new TimeControlActionStep(
            "finish heater-thawed dining",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return fixture.WaitForConsumed("heater-thawed meal is consumed by the admitted ingest job");

        yield return new TimeControlActionStep(
            "pause before room-temperature control",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return fixture.ToggleDraft(context, "draft diner before room-temperature control", false);
        yield return new AssertionStep("prepare a room-temperature meal while heater remains available", _ =>
            fixture.PrepareNextStage(
                sourceToRemove: null,
                nextStage: MealHeatingSourceKind.AmbientHeater,
                temperatureCelsius: 15f));
        yield return fixture.ToggleDraft(context, "undraft diner to eat room-temperature meal directly", true);

        var consumeOptions = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(fixture.Diner.ThingID, fixture.Meal.ThingID)
            .Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("consume", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(
            1,
            consumeOptions.Length,
            "The room-temperature plated meal must expose one enabled native Consume action.");
        yield return new FloatMenuActionStep(
            "order native direct room-temperature dining",
            fixture.Diner.ThingID,
            fixture.Meal.ThingID,
            consumeOptions[0].StableId);
        yield return new TimeControlActionStep(
            "run native room-temperature dining",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "room-temperature meal is admitted without any heating source",
            _ => fixture.ObserveRoomTemperatureAdmission(),
            new EndToEndDeadline(1_200, 3_000, TimeSpan.FromSeconds(45)));
        yield return fixture.ScreenshotCurrentStage("observe room-temperature meal eaten without reheating");
        yield return new TimeControlActionStep(
            "finish direct room-temperature dining",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "room-temperature meal is consumed by the same direct ingest job",
            _ => fixture.ObserveRoomTemperatureConsumption(),
            new EndToEndDeadline(1_800, 6_000, TimeSpan.FromSeconds(60)));

        yield return new TimeControlActionStep(
            "pause before no-source fallback",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return fixture.ToggleDraft(context, "draft diner before removing the last heat source", false);
        yield return new AssertionStep("remove heater and prepare a frozen no-source meal", _ =>
            fixture.PrepareNextStage(
                MealHeatingSourceKind.AmbientHeater,
                MealHeatingSourceKind.AmbientHeater,
                -5f));
        yield return fixture.ToggleDraft(context, "undraft diner for no-source dining", true);

        var noSourceConsumeOptions = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(fixture.Diner.ThingID, fixture.Meal.ThingID)
            .Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("consume", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(
            1,
            noSourceConsumeOptions.Length,
            "The frozen plated meal must remain consumable when no heat source exists.");
        yield return new FloatMenuActionStep(
            "order native dining with no usable heat source",
            fixture.Diner.ThingID,
            fixture.Meal.ThingID,
            noSourceConsumeOptions[0].StableId);
        yield return new TimeControlActionStep(
            "run native no-source dining",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "frozen meal is admitted without a heating detour when no source exists",
            _ => fixture.ObserveNoSourceAdmission(),
            new EndToEndDeadline(1_200, 3_000, TimeSpan.FromSeconds(45)));
        yield return fixture.ScreenshotCurrentStage("observe frozen meal remain directly edible without a heat source");
        yield return new TimeControlActionStep(
            "finish no-source dining",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "frozen no-source meal is consumed by the same ingest job",
            _ => fixture.ObserveNoSourceConsumption(),
            new EndToEndDeadline(1_800, 6_000, TimeSpan.FromSeconds(60)));
        yield return new AssertionStep("fallback losses increase by source tier", _ =>
            fixture.AssertTieredResults());
        yield return new CheckpointStep(
            "native fallback meal heating results",
            _ => fixture.CaptureResults());
    }
}

internal sealed class FallbackMealHeatingFixture
{
    private readonly Dictionary<MealHeatingSourceKind, ThingWithComps> sources;
    private readonly Dictionary<MealHeatingSourceKind, int> heatedQuality = new();
    private readonly List<Thing> cleanupThings = new();
    private readonly List<ThingWithComps> plates = new();
    private Job? roomTemperatureJob;
    private bool roomTemperatureBypassedHeating;
    private Job? noSourceJob;
    private bool noSourceBypassedHeating;
    private bool roomTemperaturePrepared;
    private bool roomTopologyRebuilt;
    private int emitterWaitStartTick = -1;

    private FallbackMealHeatingFixture(
        Map map,
        Pawn diner,
        Dictionary<MealHeatingSourceKind, ThingWithComps> sources,
        IntVec3 mealCell)
    {
        Map = map;
        Diner = diner;
        this.sources = sources;
        MealCell = mealCell;
    }

    internal Map Map { get; }
    internal Pawn Diner { get; }
    internal IntVec3 MealCell { get; }
    internal ThingWithComps Meal { get; private set; } = null!;
    internal MealHeatingSourceKind CurrentStage { get; private set; }

    internal static FallbackMealHeatingFixture Create(IEndToEndContext context)
    {
        var settings = ImmersiveChefsMod.Settings;
        var priorWareMode = settings.WareRequirementMode;
        var priorTemperatureEnabled = settings.MealTemperatureEnabled;
        var priorThreshold = settings.AutoMicrowaveBelow;
        var priorQualityLoss = settings.MicrowaveQualityLoss;
        context.DeferCleanup(() =>
        {
            settings.WareRequirementMode = priorWareMode;
            settings.MealTemperatureEnabled = priorTemperatureEnabled;
            settings.AutoMicrowaveBelow = priorThreshold;
            settings.MicrowaveQualityLoss = priorQualityLoss;
        });
        settings.WareRequirementMode = WareRequirementMode.Off;
        settings.MealTemperatureEnabled = true;
        settings.AutoMicrowaveBelow = 30f;
        settings.MicrowaveQualityLoss = 5;

        var map = Current.Game.CurrentMap;
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);
        var constructedRoof = DefDatabase<RoofDef>.GetNamed("RoofConstructed");
        foreach (var cell in CellRect.CenteredOn(center, 11, 11).Cells)
        {
            map.roofGrid.SetRoof(cell, constructedRoof);
        }
        map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();

        DispenserE2EFixture.SpawnConduitGrid(map, center, 4, 4);
        DispenserE2EFixture.SpawnPowerSources(map, center + new IntVec3(3, 0, 3), 1);

        var stove = SpawnSource(map, "FueledStove", center + new IntVec3(2, 0, 2));
        stove.GetComp<CompRefuelable>()?.Refuel(999f);
        var campfire = SpawnSource(map, "Campfire", center + new IntVec3(-1, 0, 1));
        campfire.GetComp<CompRefuelable>()?.Refuel(999f);
        var heater = SpawnSource(map, "Heater", center + new IntVec3(0, 0, -2));
        heater.GetComp<CompTempControl>().TargetTemperature = 100f;
        DispenserE2EFixture.SettlePower(map, new[] { heater }, 300);

        var sources = new Dictionary<MealHeatingSourceKind, ThingWithComps>
        {
            [MealHeatingSourceKind.Stove] = stove,
            [MealHeatingSourceKind.Campfire] = campfire,
            [MealHeatingSourceKind.AmbientHeater] = heater
        };
        foreach (var entry in sources)
        {
            var runtime = MealHeatingSource.TryCreate(entry.Value);
            EndToEndAssert.NotNull(runtime, entry.Key + " fixture must expose a heating capability.");
            EndToEndAssert.Equal(entry.Key, runtime!.Kind, entry.Key + " fixture must classify exactly.");
        }

        var diner = FoodSearchE2EFixture.CreateColonist("Fallback Heat Diner");
        GenSpawn.Spawn(diner, center + new IntVec3(-3, 0, 0), map);
        diner.drafter.Drafted = true;

        var fixture = new FallbackMealHeatingFixture(
            map,
            diner,
            sources,
            center + new IntVec3(-2, 0, 0));
        fixture.cleanupThings.Add(diner);
        fixture.cleanupThings.AddRange(sources.Values);
        context.DeferCleanup(fixture.Cleanup);
        fixture.PrepareMeal(MealHeatingSourceKind.Stove, -5f);
        return fixture;
    }

    internal WaitUntilStep WaitForFallbackEmitters(string name) =>
        new(
            name,
            _ =>
            {
                emitterWaitStartTick = emitterWaitStartTick < 0
                    ? Find.TickManager.TicksGame
                    : emitterWaitStartTick;
                var heater = sources[MealHeatingSourceKind.AmbientHeater];
                var room = heater.Position.GetRoom(Map);
                if (room is not { UsesOutdoorTemperature: false } && !roomTopologyRebuilt)
                {
                    Map.regionAndRoomUpdater.RebuildAllRegionsAndRooms();
                    roomTopologyRebuilt = true;
                    return false;
                }

                if (room is not { UsesOutdoorTemperature: false })
                {
                    if (Find.TickManager.TicksGame >= emitterWaitStartTick + 300)
                    {
                        var center = MealCell + new IntVec3(2, 0, 0);
                        var wallCount = CellRect.CenteredOn(center, 11, 11).EdgeCells.Count(cell =>
                            cell.GetEdifice(Map)?.def == ThingDefOf.Wall);
                        throw new EndToEndAssertionException(
                            "The constructed fallback room did not become enclosed; wallCount=" + wallCount +
                            ", roomCells=" + room?.CellCount +
                            ", openRoofs=" + room?.OpenRoofCount +
                            ", centerRoofed=" + Map.roofGrid.Roofed(center) + ".");
                    }

                    return false;
                }

                if (!roomTemperaturePrepared)
                {
                    room.Temperature = 0f;
                    roomTemperaturePrepared = true;
                    return false;
                }

                return MealHeatingSource.TryCreate(
                           sources[MealHeatingSourceKind.Stove])?.IsOperational == true &&
                       MealHeatingSource.TryCreate(
                           sources[MealHeatingSourceKind.Campfire])?.IsOperational == true &&
                       MealHeatingSource.TryCreate(heater)?.IsOperational == true;
            },
            new EndToEndDeadline(1_800, 3_000, TimeSpan.FromSeconds(60)));

    internal void PrepareNextStage(
        MealHeatingSourceKind? sourceToRemove,
        MealHeatingSourceKind nextStage,
        float temperatureCelsius)
    {
        EndToEndAssert.True(Diner.Drafted, "The diner must remain drafted while fixture availability changes.");
        if (sourceToRemove.HasValue)
        {
            var source = sources[sourceToRemove.Value];
            if (!source.Destroyed)
            {
                source.Destroy(DestroyMode.Vanish);
            }
        }

        PrepareMeal(nextStage, temperatureCelsius);
    }

    internal EndToEndStep FrameCurrentStage(string name) =>
        new CameraActionStep(
            name,
            new[]
            {
                Diner.ThingID,
                Meal.ThingID,
                sources[MealHeatingSourceKind.Stove].ThingID,
                sources[MealHeatingSourceKind.Campfire].ThingID,
                sources[MealHeatingSourceKind.AmbientHeater].ThingID
            }.Where(id => !string.IsNullOrWhiteSpace(id)),
            paddingPixels: 180);

    internal EndToEndStep ScreenshotCurrentStage(string name)
    {
        var targets = new List<string> { Diner.ThingID };
        if (Diner.CurJob is { } job && DiningSessionRegistry.HeatingSourceFor(job) is { } source)
        {
            targets.Add(source.Thing.ThingID);
        }

        return new ScreenshotStep(name, targets, paddingPixels: 170);
    }

    internal WaitUntilStep WaitForActiveHeating(string name, MealHeatingSourceKind expectedKind) =>
        new(
            name,
            _ => IsActivelyHeating(expectedKind),
            new EndToEndDeadline(2_400, 6_000, TimeSpan.FromSeconds(75)));

    internal WaitUntilStep WaitForCompletedHeating(string name, MealHeatingSourceKind expectedKind) =>
        new(
            name,
            _ => ObserveCompletedHeating(expectedKind),
            new EndToEndDeadline(3_000, 8_000, TimeSpan.FromSeconds(90)));

    internal WaitUntilStep WaitForConsumed(string name) =>
        new(
            name,
            _ => Meal.Destroyed,
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(75)));

    internal GizmoActionStep ToggleDraft(
        IEndToEndContext actionContext,
        string name,
        bool expectedCurrentState)
    {
        var options = actionContext.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { Diner.ThingID }, Array.Empty<string>())
            .Where(option =>
                !option.Disabled &&
                option.Interaction == EndToEndGizmoInteraction.Toggle &&
                option.ToggleState == expectedCurrentState &&
                string.Equals(option.HotKeyDefName, "Command_ColonistDraft", StringComparison.Ordinal))
            .ToArray();
        EndToEndAssert.Equal(1, options.Length, "The diner must expose one native draft toggle.");
        return new GizmoActionStep(
            name,
            new[] { Diner.ThingID },
            options[0].RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: options[0].StableId);
    }

    internal void AssertTieredResults()
    {
        EndToEndAssert.True(
            heatedQuality[MealHeatingSourceKind.Stove] >
            heatedQuality[MealHeatingSourceKind.Campfire] &&
            heatedQuality[MealHeatingSourceKind.Campfire] >
            heatedQuality[MealHeatingSourceKind.AmbientHeater],
            "Stove, campfire, and heater fallbacks must impose progressively greater quality loss.");
        EndToEndAssert.True(
            roomTemperatureBypassedHeating &&
            roomTemperatureJob is not null &&
            DiningSessionRegistry.HeatingSourceFor(roomTemperatureJob) is null,
            "A meal at the 15 C room-temperature boundary must bypass every available heat source.");
        EndToEndAssert.True(
            noSourceBypassedHeating && noSourceJob is not null,
            "A frozen meal must remain directly ingestible when no usable heat source exists.");
        EndToEndAssert.True(
            plates.All(plate =>
                !plate.Destroyed &&
                plate.Spawned &&
                plate.GetComp<CompSanitation>()?.IsDirty == true),
            "Every exact embedded plate must survive its completed native dining job and return dirty.");
    }

    internal IReadOnlyDictionary<string, string> CaptureResults() =>
        new Dictionary<string, string>
        {
            ["stoveQuality"] = heatedQuality[MealHeatingSourceKind.Stove].ToString(),
            ["campfireQuality"] = heatedQuality[MealHeatingSourceKind.Campfire].ToString(),
            ["heaterQuality"] = heatedQuality[MealHeatingSourceKind.AmbientHeater].ToString(),
            ["heaterTargetCelsius"] = "20",
            ["roomTemperatureBypassedHeating"] = roomTemperatureBypassedHeating.ToString(),
            ["noSourceBypassedHeating"] = noSourceBypassedHeating.ToString(),
            ["exactDirtyPlatesReturned"] = plates.Count.ToString()
        };

    internal bool ObserveRoomTemperatureAdmission()
    {
        if (Meal.Destroyed ||
            Diner.CurJob is not { } job ||
            Diner.CurJobDef != JobDefOf.Ingest ||
            !ReferenceEquals(job.GetTarget(TargetIndex.A).Thing, Meal) ||
            DiningSessionRegistry.HeatingSourceFor(job) is not null)
        {
            return false;
        }

        roomTemperatureJob = job;
        return true;
    }

    internal bool ObserveRoomTemperatureConsumption()
    {
        if (Meal.Destroyed)
        {
            roomTemperatureBypassedHeating = roomTemperatureJob is not null;
            return roomTemperatureBypassedHeating;
        }

        if (roomTemperatureJob is not null && !ReferenceEquals(Diner.CurJob, roomTemperatureJob))
        {
            throw new EndToEndAssertionException(
                "The admitted direct room-temperature ingest job changed before consuming its exact meal.");
        }

        return false;
    }

    internal bool ObserveNoSourceAdmission()
    {
        if (Meal.Destroyed ||
            Diner.CurJob is not { } job ||
            Diner.CurJobDef != JobDefOf.Ingest ||
            !ReferenceEquals(job.GetTarget(TargetIndex.A).Thing, Meal) ||
            DiningSessionRegistry.HeatingSourceFor(job) is not null)
        {
            return false;
        }

        noSourceJob = job;
        return true;
    }

    internal bool ObserveNoSourceConsumption()
    {
        if (Meal.Destroyed)
        {
            noSourceBypassedHeating = noSourceJob is not null;
            return noSourceBypassedHeating;
        }

        if (noSourceJob is not null && !ReferenceEquals(Diner.CurJob, noSourceJob))
        {
            throw new EndToEndAssertionException(
                "The admitted no-source ingest job changed before consuming its exact frozen meal.");
        }

        return false;
    }

    private void PrepareMeal(MealHeatingSourceKind stage, float temperatureCelsius)
    {
        FoodSearchE2EFixture.SetHunger(Diner, 0.10f);
        Meal = FoodSearchE2EFixture.MakePlatedMeal(
            ThingDefOf.MealSimple,
            ThingDefOf.Steel,
            out var plate);
        Meal.GetComp<CompCulinaryState>().ReplaceServings(new[]
        {
            new CulinaryServingRecord(
                80,
                temperatureCelsius,
                ContaminationSources.None,
                0,
                Find.TickManager.TicksGame)
        });
        GenSpawn.Spawn(Meal, MealCell, Map);
        cleanupThings.Add(Meal);
        cleanupThings.Add(plate);
        plates.Add(plate);
        CurrentStage = stage;
    }

    private bool IsActivelyHeating(MealHeatingSourceKind expectedKind)
    {
        if (Meal.Destroyed ||
            Diner.CurJob is not { } job ||
            Diner.CurJobDef != JobDefOf.Ingest ||
            !ReferenceEquals(Diner.carryTracker?.CarriedThing, Meal) ||
            DiningSessionRegistry.HeatingSourceFor(job) is not { } source ||
            source.Kind != expectedKind ||
            CurrentToil(Diner) is not { defaultCompleteMode: ToilCompleteMode.Delay } toil)
        {
            return false;
        }

        return toil.defaultDuration == source.Profile.HeatingTicks;
    }

    private bool ObserveCompletedHeating(MealHeatingSourceKind expectedKind)
    {
        if (Meal.Destroyed ||
            Meal.GetComp<CompCulinaryState>().PeekCurrentServingWithoutThermalUpdate() is not { } serving)
        {
            return false;
        }

        var profile = MealHeatingPolicy.ProfileFor(expectedKind, microwaveHeatingTicks: 180);
        if (Math.Abs(serving.TemperatureCelsius - profile.TargetTemperatureCelsius) > 0.01f ||
            serving.QualityScore >= 80)
        {
            return false;
        }

        EndToEndAssert.Equal(
            0,
            serving.MicrowaveReheatCount,
            expectedKind + " heating must not increment microwave-reheat risk.");
        heatedQuality[expectedKind] = serving.QualityScore;
        return true;
    }

    private static ThingWithComps SpawnSource(Map map, string defName, IntVec3 position)
    {
        var def = DefDatabase<ThingDef>.GetNamed(defName);
        var source = (ThingWithComps)ThingMaker.MakeThing(
            def,
            def.MadeFromStuff ? ThingDefOf.Steel : null);
        source.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(source, position, map, Rot4.North);
        return source;
    }

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

    private void Cleanup()
    {
        foreach (var thing in cleanupThings.Distinct().Where(thing => !thing.Destroyed))
        {
            thing.Destroy(DestroyMode.Vanish);
        }
    }
}
