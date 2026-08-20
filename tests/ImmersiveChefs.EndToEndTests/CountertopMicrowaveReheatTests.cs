using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.countertop-microwave-native-reheat",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "fumblesneeze.immersivechefs",
    MaxFrames = 3_600,
    MaxGameTicks = 12_000,
    MaxWallClockSeconds = 120)]
public sealed class CountertopMicrowaveNativeReheatTest : IRimWorldEndToEndTest
{
    private CountertopMicrowaveDiningFixture fixture = null!;

    public void Arrange(IEndToEndContext context)
    {
        fixture = CountertopMicrowaveDiningFixture.Create(
            context,
            "Countertop Reheat Diner");
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        foreach (var step in fixture.FrameAndObserve("before native countertop reheating"))
        {
            yield return step;
        }
        yield return fixture.ToggleDraft(
            context,
            "undraft diner to start ordinary countertop reheating",
            expectedCurrentState: true);
        yield return new TimeControlActionStep(
            "run ordinary countertop reheating",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "diner reaches the active countertop heating toil",
            _ => fixture.IsActivelyHeating,
            new EndToEndDeadline(1_200, 3_000, TimeSpan.FromSeconds(45)));
        yield return new ScreenshotStep(
            "observe native countertop heating in progress",
            new[] { fixture.Diner.ThingID, fixture.Microwave.ThingID },
            paddingPixels: 140);
        yield return new WaitUntilStep(
            "countertop microwave completes exactly one reheat",
            _ => fixture.CurrentServingWithoutThermalUpdate is { MicrowaveReheatCount: 1, QualityScore: 71 } serving &&
                 Math.Abs(serving.TemperatureCelsius - 60f) < 0.01f,
            new EndToEndDeadline(900, 2_000, TimeSpan.FromSeconds(35)));
        yield return new TimeControlActionStep(
            "pause on the reheated carried meal",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep(
            "observe steaming reheated meal before dining",
            new[] { fixture.Diner.ThingID },
            paddingPixels: 160);
        yield return new TimeControlActionStep(
            "run into chewing after countertop reheating",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the reheated serving uses ordinary rather than frozen chewing time",
            _ => fixture.ObserveReheatedChewing(),
            new EndToEndDeadline(900, 2_000, TimeSpan.FromSeconds(35)));
        yield return new TimeControlActionStep(
            "pause on ordinary post-reheat chewing",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep(
            "observe ordinary chewing after the frozen meal was reheated",
            new[] { fixture.Diner.ThingID, fixture.Microwave.ThingID },
            paddingPixels: 150);
        yield return new TimeControlActionStep(
            "finish ordinary dining after reheating",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "reheated meal returns the exact dirty setting",
            _ => fixture.Meal.Destroyed && fixture.ExactSettingReturnedDirty,
            new EndToEndDeadline(1_800, 6_000, TimeSpan.FromSeconds(60)));
        yield return new TimeControlActionStep(
            "pause after completed countertop dining",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select exact returned countertop setting",
            new[] { fixture.Plate.ThingID, fixture.Cutlery.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "observe exact returned dirty countertop setting",
            new[] { fixture.Plate.ThingID, fixture.Cutlery.ThingID, fixture.Microwave.ThingID },
            paddingPixels: 160);
        yield return new CheckpointStep(
            "completed countertop reheat result",
            _ => new Dictionary<string, string>
            {
                ["mealDestroyed"] = fixture.Meal.Destroyed.ToString(),
                ["frozenReheatQuality"] = "71",
                ["postReheatChewDuration"] = fixture.PostReheatChewDuration.ToString(),
                ["plateDirty"] = fixture.Plate.GetComp<CompSanitation>().IsDirty.ToString(),
                ["cutleryDirty"] = fixture.Cutlery.GetComp<CompSanitation>().IsDirty.ToString(),
                ["microwaveStillSupported"] = fixture.Microwave.GetComp<CompMicrowave>().Operational.ToString()
            });
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.inventory-meal-countertop-reheat",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.immersivechefs",
    MaxFrames = 3_600,
    MaxGameTicks = 12_000,
    MaxWallClockSeconds = 120)]
public sealed class InventoryMealCountertopReheatTest : IRimWorldEndToEndTest
{
    private const string IngestionTraceOwner =
        "fumblesneeze.immersivechefs.e2e.inventory-meal-ingestion-trace";
    private static InventoryMealCountertopReheatTest? activeTrace;

    private CountertopMicrowaveDiningFixture fixture = null!;
    private Job? admittedJob;
    private int reheatCountBeforeIngestion;
    private bool exactMealIngested;
    private bool admittedJobReachedIngestion;

    public void Arrange(IEndToEndContext context)
    {
        fixture = CountertopMicrowaveDiningFixture.Create(
            context,
            "Inventory Reheat Diner",
            mealInDinerInventory: true);

        var traceHarmony = new Harmony(IngestionTraceOwner);
        activeTrace = this;
        traceHarmony.Patch(
            AccessTools.Method(typeof(Thing), nameof(Thing.Ingested)),
            prefix: new HarmonyMethod(
                typeof(InventoryMealCountertopReheatTest),
                nameof(TraceExactMealIngestion)));
        context.DeferCleanup(() =>
        {
            traceHarmony.UnpatchAll(IngestionTraceOwner);
            if (ReferenceEquals(activeTrace, this))
            {
                activeTrace = null;
            }
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new AssertionStep(
            "cold plated meal begins in the exact diner's inventory",
            _ => EndToEndAssert.True(
                ReferenceEquals(
                    fixture.Meal.holdingOwner,
                    fixture.Diner.inventory?.innerContainer),
                "The regression fixture must begin with the exact cold meal in the diner's inventory."));
        yield return new SelectionActionStep(
            "select the diner carrying the cold meal",
            new[] { fixture.Diner.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the inventory-meal diner and fallback microwave",
            new[] { fixture.Diner.ThingID, fixture.Microwave.ThingID },
            paddingPixels: 180);
        yield return new PawnInspectTabActionStep(
            "open the diner's native Gear tab before food search",
            fixture.Diner.ThingID,
            EndToEndPawnInspectTab.Gear);
        yield return new ScreenshotStep(
            "observe the cold meal in inventory before native food search",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return fixture.ToggleDraft(
            context,
            "undraft diner to start ordinary inventory food search",
            expectedCurrentState: true);
        yield return new TimeControlActionStep(
            "run ordinary food search for the inventory meal",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "one native ingest job admits the exact inventory meal",
            _ => TryCaptureAdmittedJob(),
            new EndToEndDeadline(900, 2_000, TimeSpan.FromSeconds(35)));
        yield return new WaitUntilStep(
            "the same ingest job reaches fallback microwave heating",
            _ => ReferenceEquals(fixture.Diner.CurJob, admittedJob) && fixture.IsActivelyHeating,
            new EndToEndDeadline(1_200, 3_000, TimeSpan.FromSeconds(45)));
        yield return new ScreenshotStep(
            "observe the inventory meal heating in the same native ingest job",
            new[] { fixture.Diner.ThingID, fixture.Microwave.ThingID },
            paddingPixels: 150);
        yield return new WaitUntilStep(
            "the same ingest job completes exactly one inventory-meal reheat",
            _ => ObserveCompletedReheat(),
            new EndToEndDeadline(900, 2_000, TimeSpan.FromSeconds(35)));
        yield return new TimeControlActionStep(
            "pause after the same job reheats its carried inventory meal",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the reheated inventory meal remains carried for native ingestion",
            _ => EndToEndAssert.True(
                ReferenceEquals(fixture.Diner.CurJob, admittedJob) &&
                ReferenceEquals(fixture.Diner.carryTracker?.CarriedThing, fixture.Meal) &&
                !fixture.Meal.Spawned &&
                !fixture.Diner.inventory.innerContainer.Contains(fixture.Meal),
                "The admitted ingest job must retain the exact reheated meal in its carrier instead of dropping it or restarting."));
        yield return new ScreenshotStep(
            "observe the same job retaining the reheated inventory meal",
            new[] { fixture.Diner.ThingID, fixture.Microwave.ThingID },
            paddingPixels: 150);
        yield return new TimeControlActionStep(
            "continue the admitted inventory-meal job into chewing",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the same admitted job reaches ordinary post-reheat chewing",
            _ => ReferenceEquals(fixture.Diner.CurJob, admittedJob) &&
                 fixture.ObserveReheatedChewing(),
            new EndToEndDeadline(900, 2_000, TimeSpan.FromSeconds(35)));
        yield return new TimeControlActionStep(
            "finish ordinary inventory-meal dining",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "inventory meal is consumed and returns the exact dirty setting",
            _ => fixture.Meal.Destroyed && fixture.ExactSettingReturnedDirty,
            new EndToEndDeadline(1_800, 6_000, TimeSpan.FromSeconds(60)));
        yield return new AssertionStep(
            "the admitted job performs the exact meal's ingestion",
            _ => EndToEndAssert.True(
                exactMealIngested && admittedJobReachedIngestion,
                "The exact meal must reach Thing.Ingested while the original admitted ingest job is still current."));
        yield return new TimeControlActionStep(
            "pause after completed inventory-meal dining",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the exact returned inventory-meal setting",
            new[] { fixture.Plate.ThingID, fixture.Cutlery.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "observe the exact dirty setting after one completed ingest job",
            new[] { fixture.Plate.ThingID, fixture.Cutlery.ThingID, fixture.Microwave.ThingID },
            paddingPixels: 160);
        yield return new CheckpointStep(
            "inventory meal microwave restart regression result",
            _ => new Dictionary<string, string>
            {
                ["admittedJob"] = admittedJob?.GetUniqueLoadID() ?? "none",
                ["mealDestroyed"] = fixture.Meal.Destroyed.ToString(),
                ["plateDirty"] = fixture.Plate.GetComp<CompSanitation>().IsDirty.ToString(),
                ["cutleryDirty"] = fixture.Cutlery.GetComp<CompSanitation>().IsDirty.ToString(),
                ["microwaveReheatCount"] = reheatCountBeforeIngestion.ToString(),
                ["admittedJobReachedIngestion"] = admittedJobReachedIngestion.ToString()
            });
    }

    private bool TryCaptureAdmittedJob()
    {
        var current = fixture.Diner.CurJob;
        if (current?.def != JobDefOf.Ingest ||
            !ReferenceEquals(current.GetTarget(TargetIndex.A).Thing, fixture.Meal))
        {
            return false;
        }

        admittedJob ??= current;
        return ReferenceEquals(current, admittedJob);
    }

    private bool ObserveCompletedReheat()
    {
        if (!ReferenceEquals(fixture.Diner.CurJob, admittedJob) ||
            fixture.CurrentServingWithoutThermalUpdate.MicrowaveReheatCount != 1)
        {
            return false;
        }

        reheatCountBeforeIngestion = 1;
        return true;
    }

    private static void TraceExactMealIngestion(Thing __instance, Pawn ingester)
    {
        var trace = activeTrace;
        if (trace is null ||
            !ReferenceEquals(__instance, trace.fixture.Meal) ||
            !ReferenceEquals(ingester, trace.fixture.Diner))
        {
            return;
        }

        trace.exactMealIngested = true;
        trace.admittedJobReachedIngestion =
            ReferenceEquals(ingester.CurJob, trace.admittedJob);
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.countertop-microwave-support-loss",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "fumblesneeze.immersivechefs",
    MaxFrames = 4_200,
    MaxGameTicks = 14_000,
    MaxWallClockSeconds = 150)]
public sealed class CountertopMicrowaveSupportLossTest : IRimWorldEndToEndTest
{
    private CountertopMicrowaveDiningFixture fixture = null!;
    private CulinaryServingSnapshot beforeSupportLoss;
    private Job activeHeatingJob = null!;
    private MinifiedThing? recoveredMicrowave;

    public void Arrange(IEndToEndContext context)
    {
        fixture = CountertopMicrowaveDiningFixture.Create(
            context,
            "Unsupported Reheat Diner",
            heatingTicksOverride: 2_000,
            createSupportBuilder: true);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        foreach (var step in fixture.FrameAndObserve("before support-loss reheating"))
        {
            yield return step;
        }
        yield return fixture.ToggleDraft(
            context,
            "undraft diner to start support-loss reheating",
            expectedCurrentState: true);
        yield return new TimeControlActionStep(
            "run toward active support-loss reheating",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "diner reaches heating before support loss",
            _ => fixture.IsActivelyHeating,
            new EndToEndDeadline(1_200, 3_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause during active support-loss heating",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep(
            "observe meal actively heating on its supported microwave",
            new[] { fixture.Diner.ThingID, fixture.Table.ThingID, fixture.Microwave.ThingID },
            paddingPixels: 160);
        yield return new AssertionStep(
            "capture the exact pre-cancellation serving",
            _ =>
            {
                activeHeatingJob = fixture.Diner.CurJob ??
                    throw new EndToEndAssertionException(
                        "The actively heating diner lost the captured ingest job.");
                beforeSupportLoss = fixture.CurrentServingWithoutThermalUpdate.Capture();
            });
        yield return fixture.DeconstructSupport(
            "deconstruct active microwave support through its native command");
        yield return new TimeControlActionStep(
            "allow the unsupported heating toil to cancel",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "ordinary Construction removes the designated support",
            _ => fixture.Table.Destroyed,
            new EndToEndDeadline(1_800, 4_000, TimeSpan.FromSeconds(60)));
        yield return new WaitUntilStep(
            "support loss cancels the exact active heating transaction",
            _ => !ReferenceEquals(fixture.Diner.CurJob, activeHeatingJob) &&
                 DiningSessionRegistry.MicrowaveFor(activeHeatingJob) is null,
            new EndToEndDeadline(300, 600, TimeSpan.FromSeconds(20)));
        yield return new TimeControlActionStep(
            "pause immediately after support-loss cancellation",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return fixture.ToggleDraft(
            context,
            "draft diner to suppress any ordinary cold-meal retry",
            expectedCurrentState: false);
        yield return new WaitUntilStep(
            "drafting clears any later retry dining session",
            _ => fixture.Diner.CurJobDef != JobDefOf.Ingest &&
                 DiningSessionRegistry.Current(fixture.Diner) is null,
            new EndToEndDeadline(60, 120, TimeSpan.FromSeconds(10)));
        yield return new AssertionStep(
            "support loss preserves meal, plate, and clean cutlery transactionally",
            _ =>
            {
                var after = fixture.CurrentServingWithoutThermalUpdate.Capture();
                EndToEndAssert.Equal(beforeSupportLoss.QualityScore, after.QualityScore,
                    "Cancelled support-loss heating must not change culinary quality.");
                EndToEndAssert.Equal(beforeSupportLoss.MicrowaveReheatCount, after.MicrowaveReheatCount,
                    "Cancelled support-loss heating must not record a microwave reheat.");
                EndToEndAssert.True(
                    after.TemperatureCelsius < ImmersiveChefsMod.Settings.AutoMicrowaveBelow &&
                    Math.Abs(after.TemperatureCelsius - 60f) > 0.001f,
                    "Cancelled support-loss heating must not apply the microwave target temperature; ordinary ambient progression remains valid.");
                EndToEndAssert.True(
                    after.LastThermalTick > beforeSupportLoss.LastThermalTick &&
                    after.TemperatureCelsius > beforeSupportLoss.TemperatureCelsius,
                    "The interrupted serving must continue its observed ambient warming with advancing thermal bookkeeping.");
                EndToEndAssert.Equal(beforeSupportLoss.Contamination, after.Contamination,
                    "Cancelled support-loss heating must not add contamination.");
                EndToEndAssert.True(
                    ReferenceEquals(
                        fixture.Plate,
                        fixture.Meal.GetComp<CompEmbeddedWare>().PeekPlateThing()),
                    "The interrupted meal must retain the same exact embedded plate.");
                EndToEndAssert.True(!fixture.Cutlery.GetComp<CompSanitation>().IsDirty,
                    "Interrupted heating must return the selected cutlery still clean.");
                EndToEndAssert.True(fixture.Diner.drafter?.Drafted == true,
                    "The native Draft toggle must prevent an automatic retry after cancellation.");
            });
        yield return new SelectionActionStep(
            "select unchanged meal after support-loss cancellation",
            new[] { fixture.Meal.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "observe unchanged meal and clean returned setting after cancellation",
            new[] { fixture.Meal.ThingID, fixture.Cutlery.ThingID },
            paddingPixels: 160);
        yield return new AssertionStep(
            "make the bounded local recovery radius temporarily unstandable",
            _ => fixture.BlockLocalRecoveryCells(context));
        yield return new TimeControlActionStep(
            "run native rare support recovery",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "unsupported appliance becomes exactly one recoverable minified microwave",
            _ =>
            {
                recoveredMicrowave = fixture.Map.listerThings.AllThings
                    .OfType<MinifiedThing>()
                    .SingleOrDefault(candidate => ReferenceEquals(candidate.InnerThing, fixture.Microwave));
                return recoveredMicrowave is not null;
            },
            new EndToEndDeadline(1_800, 2_500, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause on recovered microwave",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "unsupported appliance is conserved without duplication",
            _ => EndToEndAssert.Equal(
                1,
                fixture.Map.listerThings.AllThings.OfType<MinifiedThing>()
                    .Count(candidate => ReferenceEquals(candidate.InnerThing, fixture.Microwave)),
                "Support loss must retain exactly one wrapper for the original microwave."));
        yield return new AssertionStep(
            "map-wide fallback recovers beyond the blocked local radius",
            _ => EndToEndAssert.True(
                Math.Max(
                    Math.Abs(recoveredMicrowave!.Position.x - fixture.SupportPosition.x),
                    Math.Abs(recoveredMicrowave.Position.z - fixture.SupportPosition.z)) > 4,
                "The recovered microwave must prove the map-wide fallback escaped the blocked four-cell radius."));
        yield return new SelectionActionStep(
            "select recovered microwave and unchanged meal",
            new[] { recoveredMicrowave!.ThingID, fixture.Meal.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame recovered microwave and unchanged meal",
            new[] { recoveredMicrowave.ThingID, fixture.Meal.ThingID, fixture.Cutlery.ThingID },
            paddingPixels: 170);
        yield return new ScreenshotStep(
            "observe recovered microwave after transactional cancellation",
            new[] { recoveredMicrowave.ThingID, fixture.Meal.ThingID, fixture.Cutlery.ThingID },
            paddingPixels: 150);
        yield return new CheckpointStep(
            "countertop support-loss result",
            _ => new Dictionary<string, string>
            {
                ["mealDestroyed"] = fixture.Meal.Destroyed.ToString(),
                ["qualityBefore"] = beforeSupportLoss.QualityScore.ToString(),
                ["qualityAfter"] = fixture.CurrentServingWithoutThermalUpdate.QualityScore.ToString(),
                ["temperatureBefore"] = beforeSupportLoss.TemperatureCelsius.ToString("0.###"),
                ["temperatureAfter"] = fixture.CurrentServingWithoutThermalUpdate.TemperatureCelsius.ToString("0.###"),
                ["reheatsAfter"] = fixture.CurrentServingWithoutThermalUpdate.MicrowaveReheatCount.ToString(),
                ["samePlate"] = ReferenceEquals(
                    fixture.Plate,
                    fixture.Meal.GetComp<CompEmbeddedWare>().PeekPlateThing()).ToString(),
                ["cutleryDirty"] = fixture.Cutlery.GetComp<CompSanitation>().IsDirty.ToString(),
                ["recoveredMicrowaveId"] = recoveredMicrowave.ThingID
            });
    }
}

internal sealed class CountertopMicrowaveDiningFixture
{
    private readonly EndToEndGizmoOption deconstructSupport;

    private CountertopMicrowaveDiningFixture(
        Map map,
        Building table,
        Building_Microwave microwave,
        Pawn diner,
        ThingWithComps meal,
        ThingWithComps plate,
        ThingWithComps cutlery,
        EndToEndGizmoOption deconstructSupport)
    {
        Map = map;
        Table = table;
        Microwave = microwave;
        Diner = diner;
        Meal = meal;
        Plate = plate;
        Cutlery = cutlery;
        SupportPosition = table.Position;
        this.deconstructSupport = deconstructSupport;
    }

    internal Map Map { get; }
    internal Building Table { get; }
    internal Building_Microwave Microwave { get; }
    internal Pawn Diner { get; }
    internal ThingWithComps Meal { get; }
    internal ThingWithComps Plate { get; }
    internal ThingWithComps Cutlery { get; }
    internal IntVec3 SupportPosition { get; }
    internal int PostReheatChewDuration { get; private set; }

    internal bool IsActivelyHeating =>
        Diner.CurJob is { } job &&
        Diner.CurJobDef == JobDefOf.Ingest &&
        ReferenceEquals(Diner.carryTracker?.CarriedThing, Meal) &&
        Diner.Position == Microwave.InteractionCell &&
        Diner.pather?.Moving != true &&
        ReferenceEquals(DiningSessionRegistry.MicrowaveFor(job)?.parent, Microwave);

    internal CulinaryServingRecord CurrentServing =>
        Meal.GetComp<CompCulinaryState>().PeekCurrentServing() ??
        throw new EndToEndAssertionException("The countertop fixture meal lost its current serving.");

    internal CulinaryServingRecord CurrentServingWithoutThermalUpdate =>
        Meal.GetComp<CompCulinaryState>().PeekCurrentServingWithoutThermalUpdate() ??
        throw new EndToEndAssertionException("The countertop fixture meal lost its captured serving.");

    internal bool ExactSettingReturnedDirty =>
        Plate.Spawned &&
        Cutlery.Spawned &&
        Plate.GetComp<CompSanitation>().IsDirty &&
        Cutlery.GetComp<CompSanitation>().IsDirty;

    internal bool ObserveReheatedChewing()
    {
        if (Meal.Destroyed || Diner.CurJobDef != JobDefOf.Ingest ||
            CurrentServingWithoutThermalUpdate.MicrowaveReheatCount != 1 ||
            CurrentToil(Diner) is not { defaultCompleteMode: ToilCompleteMode.Delay })
        {
            return false;
        }

        var plateSpeed = Plate.GetComp<CompKitchenwareStats>()?.CurrentStats.CookingSpeedFactor ?? 1f;
        var nativeMultiplier = Meal.def.ingestible.useEatingSpeedStat
            ? 1f / Math.Max(0.01f, Diner.GetStatValue(StatDefOf.EatingSpeed))
            : 1f;
        var ordinaryDuration = (int)Math.Round(
            Meal.def.ingestible.baseIngestTicks * nativeMultiplier /
            Math.Max(0.1f, plateSpeed),
            MidpointRounding.AwayFromZero);
        PostReheatChewDuration = Diner.jobs.curDriver.ticksLeftThisToil;
        EndToEndAssert.True(
            PostReheatChewDuration >= ordinaryDuration - 4 &&
            PostReheatChewDuration <= ordinaryDuration,
            "A meal reheated from Frozen to 60 C must sample temperature when chewing begins " +
            "and use ordinary plate-adjusted chewing time; expected about " + ordinaryDuration +
            ", observed " + PostReheatChewDuration + ".");
        return true;
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

    internal void BlockLocalRecoveryCells(IEndToEndContext context)
    {
        var originalTerrains = new Dictionary<IntVec3, TerrainDef>();
        var deepWater = DefDatabase<TerrainDef>.GetNamed("WaterDeep");
        for (var x = -4; x <= 4; x++)
        {
            for (var z = -4; z <= 4; z++)
            {
                var cell = SupportPosition + new IntVec3(x, 0, z);
                if (!cell.InBounds(Map))
                {
                    continue;
                }

                originalTerrains[cell] = Map.terrainGrid.TerrainAt(cell);
                Map.terrainGrid.SetTerrain(cell, deepWater);
            }
        }

        context.DeferCleanup(() =>
        {
            foreach (var entry in originalTerrains)
            {
                Map.terrainGrid.SetTerrain(entry.Key, entry.Value);
            }
        });
        EndToEndAssert.True(
            originalTerrains.Keys.All(cell => !cell.Standable(Map)),
            "Every in-bounds cell in the local four-cell recovery radius must be temporarily unstandable.");
    }

    internal static CountertopMicrowaveDiningFixture Create(
        IEndToEndContext context,
        string pawnName,
        int? heatingTicksOverride = null,
        bool createSupportBuilder = false,
        bool mealInDinerInventory = false)
    {
        var map = Current.Game.CurrentMap;
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.UseStrictNonEmergencyDining(context);

        var originalGodMode = DebugSettings.godMode;
        DebugSettings.godMode = true;
        context.DeferCleanup(() => DebugSettings.godMode = originalGodMode);

        DispenserE2EFixture.SpawnConduitGrid(map, center, 6, 6);
        DispenserE2EFixture.SpawnPowerSources(map, center + new IntVec3(5, 0, 5), 1);

        var table = (Building)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("Table1x2c"),
            ThingDefOf.Steel);
        table.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(table, center + new IntVec3(0, 0, 2), map, Rot4.North);

        var microwaveDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Microwave");
        var microwaveCell = table.OccupiedRect().Cells.OrderBy(cell => cell.z).First();
        var placeWorker = new PlaceWorker_MicrowaveCountertop();
        var rotation = new[] { Rot4.North, Rot4.East, Rot4.South, Rot4.West }
            .First(candidate => placeWorker.AllowsPlacing(
                microwaveDef,
                microwaveCell,
                candidate,
                map).Accepted);
        var microwave = (Building_Microwave)ThingMaker.MakeThing(microwaveDef);
        microwave.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(microwave, microwaveCell, map, rotation);
        if (heatingTicksOverride.HasValue)
        {
            var properties = (CompProperties_Microwave)microwave.GetComp<CompMicrowave>().props;
            var originalHeatingTicks = properties.heatingTicks;
            properties.heatingTicks = Math.Max(1, heatingTicksOverride.Value);
            context.DeferCleanup(() => properties.heatingTicks = originalHeatingTicks);
        }

        DispenserE2EFixture.SettlePower(map, new ThingWithComps[] { microwave }, 200);
        EndToEndAssert.True(
            microwave.GetComp<CompMicrowave>().Operational,
            "The countertop fixture microwave must begin powered and supported.");

        var diner = FoodSearchE2EFixture.CreateColonist(pawnName);
        GenSpawn.Spawn(diner, center + new IntVec3(-4, 0, 0), map);
        FoodSearchE2EFixture.SetHunger(diner, 0.10f);
        diner.drafter.Drafted = true;

        if (createSupportBuilder)
        {
            var builder = FoodSearchE2EFixture.CreateColonist("Countertop Support Builder");
            builder.skills.GetSkill(SkillDefOf.Construction).Level = 20;
            builder.workSettings.SetPriority(WorkTypeDefOf.Construction, 1);
            GenSpawn.Spawn(builder, center + new IntVec3(3, 0, 0), map);
        }

        var meal = FoodSearchE2EFixture.MakePlatedMeal(
            ThingDefOf.MealSimple,
            ThingDefOf.Steel,
            out var plate);
        meal.GetComp<CompCulinaryState>().ReplaceServings(new[]
        {
            new CulinaryServingRecord(
                80,
                -5f,
                ContaminationSources.None,
                0,
                Find.TickManager.TicksGame)
        });
        var cutlery = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cutlery",
            ThingDefOf.Steel);
        if (mealInDinerInventory)
        {
            EndToEndAssert.True(
                diner.inventory?.innerContainer.TryAdd(
                    meal,
                    canMergeWithExistingStacks: false) == true,
                "The exact cold regression meal must enter the diner's inventory.");
        }
        else
        {
            GenSpawn.Spawn(meal, center + new IntVec3(-2, 0, 0), map);
        }
        GenSpawn.Spawn(cutlery, center + new IntVec3(-2, 0, -2), map);

        var gizmos = context.GetRequiredService<IEndToEndGizmoCatalog>();
        var deconstructCandidates = gizmos.Query(new[] { table.ThingID }, Array.Empty<string>())
            .Where(option =>
                !option.Disabled &&
                option.Interaction == EndToEndGizmoInteraction.Invoke &&
                option.Label.IndexOf("deconstruct", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(1, deconstructCandidates.Length,
            "The countertop support must expose one native deconstruct command.");

        return new CountertopMicrowaveDiningFixture(
            map,
            table,
            microwave,
            diner,
            meal,
            plate,
            cutlery,
            deconstructCandidates[0]);
    }

    internal IEnumerable<EndToEndStep> FrameAndObserve(string screenshotName)
    {
        yield return new SelectionActionStep(
            "select countertop diner and frozen meal",
            new[] { Diner.ThingID, Meal.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame countertop diner, meal, and appliance",
            new[] { Diner.ThingID, Meal.ThingID, Cutlery.ThingID, Table.ThingID, Microwave.ThingID },
            paddingPixels: 180);
        yield return new ScreenshotStep(
            screenshotName,
            new[] { Diner.ThingID, Meal.ThingID, Table.ThingID, Microwave.ThingID },
            paddingPixels: 150);
    }

    internal GizmoActionStep ToggleDraft(
        IEndToEndContext context,
        string name,
        bool expectedCurrentState)
    {
        var candidates = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { Diner.ThingID }, Array.Empty<string>())
            .Where(option =>
                !option.Disabled &&
                option.Interaction == EndToEndGizmoInteraction.Toggle &&
                option.ToggleState == expectedCurrentState &&
                string.Equals(option.HotKeyDefName, "Command_ColonistDraft", StringComparison.Ordinal))
            .ToArray();
        EndToEndAssert.Equal(
            1,
            candidates.Length,
            "The countertop diner must expose one native Draft toggle in the expected current state.");
        return new GizmoActionStep(
            name,
            new[] { Diner.ThingID },
            candidates[0].RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: candidates[0].StableId);
    }

    internal GizmoActionStep DeconstructSupport(string name) => new(
        name,
        new[] { Table.ThingID },
        deconstructSupport.RuntimeType,
        EndToEndGizmoInteraction.Invoke,
        stableGizmoId: deconstructSupport.StableId);
}
