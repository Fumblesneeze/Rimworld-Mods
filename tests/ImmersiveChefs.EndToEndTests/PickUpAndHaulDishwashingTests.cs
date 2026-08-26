using System;
using System.Collections;
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
    "immersive-chefs.pick-up-and-haul-dishwashing-batch",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "Mehni.PickUpAndHaul",
    "fumblesneeze.immersivechefs",
    MaxFrames = 5_400,
    MaxGameTicks = 18_000,
    MaxWallClockSeconds = 210)]
public sealed class PickUpAndHaulDishwashingBatchTest : IRimWorldEndToEndTest
{
    private PickUpAndHaulDishwashingFixture fixture = null!;

    public void Arrange(IEndToEndContext context)
    {
        fixture = PickUpAndHaulDishwashingFixture.Create(context, "Batch dish cleaner");
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before batched dishwashing",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the mixed dirty kitchenware before cleaning",
            fixture.WareIds,
            additive: false);
        yield return new CameraActionStep(
            "frame the cleaner mixed ware water and return stockpile",
            fixture.AllVisibleIds,
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "three nearby dirty ware units await one ordinary cleaner",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new AssertionStep(
            "enable ordinary Cleaning work",
            _ => fixture.ActivateCleaning());
        yield return new TimeControlActionStep(
            "run the ordinary Cleaning workgiver",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "one Doing dishes job collects every exact ware unit before washing",
            _ => fixture.AllWareTrackedInInventoryAtSource(allMustBeDirty: true),
            new EndToEndDeadline(1_500, 5_000, TimeSpan.FromSeconds(55)));
        yield return new TimeControlActionStep(
            "pause while the cleaner holds the dirty batch at water",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the cleaner holding the Pick Up And Haul batch",
            new[] { fixture.Cleaner.ThingID },
            additive: false);
        yield return new PawnInspectTabActionStep(
            "open the cleaner Gear tab during batched washing",
            fixture.Cleaner.ThingID,
            EndToEndPawnInspectTab.Gear);
        yield return new ScreenshotStep(
            "native Gear tab visibly contains the three dirty kitchenware units at one water source",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "wash the tracked units one by one",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "one unit becomes clean while later tracked units remain dirty",
            _ => fixture.ObservePartialSequentialWash(),
            new EndToEndDeadline(1_200, 4_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause on the visible partial batch",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep(
            "the same one-source job has cleaned only part of its tracked batch",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "finish washing and begin Pick Up And Haul unloading",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "Pick Up And Haul owns the native unload trip",
            _ => fixture.ObserveNativeUnloadJob(),
            new EndToEndDeadline(1_500, 6_000, TimeSpan.FromSeconds(60)));
        yield return new TimeControlActionStep(
            "pause during the upstream unload job",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep(
            "Pick Up And Haul visibly unloads the cleaned batch from Gear",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "finish the upstream return trip",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "all exact clean ware reaches the return stockpile",
            _ => fixture.AllWareStored(cleanCount: 3, dirtyCount: 0),
            new EndToEndDeadline(1_500, 6_000, TimeSpan.FromSeconds(60)));
        yield return new TimeControlActionStep(
            "pause after batched dishwashing",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the three exact clean returned ware units",
            fixture.WareIds,
            additive: false);
        yield return new CameraActionStep(
            "frame the returned clean batch and cleaner",
            fixture.AllVisibleIds,
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "the exact mixed batch is visibly clean and stored while unrelated inventory remains personal",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new SelectionActionStep(
            "select the exact first-cleaned returned ware unit",
            new[] { fixture.FirstCleanedWareId },
            additive: false);
        yield return new ScreenshotStep(
            "the returned first-cleaned ware visibly reports clean sanitation",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "successful batching conserves exact identity and leaves unrelated inventory untouched",
            _ => fixture.AssertFinalConservation(cleanCount: 3, dirtyCount: 0));
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.pick-up-and-haul-dishwashing-interruption",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "Mehni.PickUpAndHaul",
    "fumblesneeze.immersivechefs",
    MaxFrames = 5_400,
    MaxGameTicks = 18_000,
    MaxWallClockSeconds = 210)]
public sealed class PickUpAndHaulDishwashingInterruptionTest : IRimWorldEndToEndTest
{
    private PickUpAndHaulDishwashingFixture fixture = null!;

    public void Arrange(IEndToEndContext context)
    {
        fixture = PickUpAndHaulDishwashingFixture.Create(context, "Interrupted dish cleaner");
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before interrupted batched dishwashing",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the interruption cleaner",
            new[] { fixture.Cleaner.ThingID },
            additive: false);
        yield return new AssertionStep(
            "enable ordinary Cleaning for the interruption case",
            _ => fixture.ActivateCleaning());
        yield return new TimeControlActionStep(
            "start ordinary batched dishwashing",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the first tracked unit finishes while two remain dirty",
            _ => fixture.ObservePartialSequentialWash(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(80)));
        yield return new TimeControlActionStep(
            "pause before the player interruption",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new PawnInspectTabActionStep(
            "open Gear on the one-clean two-dirty tracked batch",
            fixture.Cleaner.ThingID,
            EndToEndPawnInspectTab.Gear);
        yield return new ScreenshotStep(
            "Gear visibly shows the partially washed batch before interruption",
            Array.Empty<string>(),
            paddingPixels: 0);

        var draft = fixture.RequiredDraftToggle(context);
        yield return new GizmoActionStep(
            "draft through the native pawn toggle to interrupt Doing dishes",
            new[] { fixture.Cleaner.ThingID },
            draft.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: draft.StableId);
        yield return new WaitUntilStep(
            "drafting ends the washing job without changing later units",
            _ => fixture.InterruptionStateIsConserved(),
            new EndToEndDeadline(300, 500, TimeSpan.FromSeconds(20)));
        yield return new TimeControlActionStep(
            "pause on the conserved interrupted batch",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep(
            "the drafted cleaner retains one clean and two dirty exact tracked units",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new AssertionStep(
            "disable new Cleaning work while retaining the queued upstream unload",
            _ => fixture.DisableCleaning());
        draft = fixture.RequiredDraftToggle(context);
        yield return new GizmoActionStep(
            "undraft through the native toggle to permit queued unloading",
            new[] { fixture.Cleaner.ThingID },
            draft.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: draft.StableId);
        yield return new TimeControlActionStep(
            "run only the queued Pick Up And Haul return trip",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the interrupted clean and dirty units all return through native unloading",
            _ => fixture.AllWareStored(cleanCount: 1, dirtyCount: 2),
            new EndToEndDeadline(1_500, 6_000, TimeSpan.FromSeconds(60)));
        yield return new TimeControlActionStep(
            "pause after interrupted-batch unloading",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select every exact interrupted ware unit after return",
            fixture.WareIds,
            additive: false);
        yield return new CameraActionStep(
            "frame the conserved interrupted batch",
            fixture.AllVisibleIds,
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "the return stockpile visibly contains the conserved mixed sanitation states",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new SelectionActionStep(
            "select the exact first-cleaned interrupted ware unit",
            new[] { fixture.FirstCleanedWareId },
            additive: false);
        yield return new ScreenshotStep(
            "the returned completed unit visibly remains clean after interruption",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new SelectionActionStep(
            "select an exact still-dirty interrupted ware unit",
            new[] { fixture.FirstDirtyWareId },
            additive: false);
        yield return new ScreenshotStep(
            "the returned unwashed unit visibly remains dirty after interruption",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "interruption conserves exact clean dirty and unrelated inventory state",
            _ => fixture.AssertFinalConservation(cleanCount: 1, dirtyCount: 2));
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.pick-up-and-haul-dishwashing-save-load-fallback",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "Mehni.PickUpAndHaul",
    "fumblesneeze.immersivechefs",
    MaxFrames = 7_200,
    MaxGameTicks = 24_000,
    MaxWallClockSeconds = 270)]
public sealed class PickUpAndHaulDishwashingSaveLoadFallbackTest : IRimWorldEndToEndTest
{
    private const string SaveName = "ImmersiveChefs_PickUpAndHaulFallback";
    private PickUpAndHaulDishwashingFixture fixture = null!;

    public void Arrange(IEndToEndContext context)
    {
        var savePath = GenFilePaths.FilePathForSavedGame(SaveName);
        context.DeferCleanup(() =>
        {
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
        });
        fixture = PickUpAndHaulDishwashingFixture.Create(context, "Reloaded dish cleaner");
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before save-load adapter fallback",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "enable ordinary Cleaning before collecting the saved batch",
            _ => fixture.ActivateCleaning());
        yield return new TimeControlActionStep(
            "start the native batched dishwashing job before save",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "one tracked unit finishes while two remain dirty before save",
            _ => fixture.ObservePartialSequentialWash(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(80)));
        yield return new TimeControlActionStep(
            "pause the persisted partial batch",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the cleaner holding the partial persisted batch",
            new[] { fixture.Cleaner.ThingID },
            additive: false);
        yield return new PawnInspectTabActionStep(
            "open Gear on the partial persisted batch",
            fixture.Cleaner.ThingID,
            EndToEndPawnInspectTab.Gear);
        yield return new ScreenshotStep(
            "the save fixture visibly holds one clean and two dirty tracked ware units",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "turn the optional integration off and prevent a new Cleaning job after recovery",
            _ => fixture.DisablePickUpAndHaulAndCleaning());
        yield return new SaveLoadActionStep(
            "save and reload the partially washed batch through RimWorld",
            SaveName);
        yield return new AssertionStep(
            "resolve the same cleaner ware and medicine after native loading",
            _ =>
            {
                fixture.ResolveLoadedFixtures();
            });
        yield return new TimeControlActionStep(
            "let the loaded unavailable-adapter batch recover",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the loaded driver returns every exact item without an invalid ordinary target",
            _ => fixture.AllWareRecoveredAfterAdapterBecameUnavailable(),
            new EndToEndDeadline(1_200, 3_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause on the recovered physical batch",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the exact recovered first-cleaned item",
            new[] { fixture.FirstCleanedWareId },
            additive: false);
        yield return new CameraActionStep(
            "frame the loaded recovered cleaner and ware",
            fixture.AllVisibleIds,
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "the exact completed unit visibly remains clean after adapter fallback",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new SelectionActionStep(
            "select an exact recovered unwashed item",
            new[] { fixture.FirstDirtyWareId },
            additive: false);
        yield return new ScreenshotStep(
            "the exact later unit visibly remains dirty rather than being stranded",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new AssertionStep(
            "re-enable only ordinary single-target Cleaning with Pick Up And Haul still off",
            _ => fixture.ActivateCleaning());
        yield return new TimeControlActionStep(
            "run the ordinary fallback workgiver",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the recovered ware enters a valid ordinary one-target dishwashing job",
            _ => fixture.ObserveOrdinarySingleTargetFallback(),
            new EndToEndDeadline(1_200, 4_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause during the ordinary one-target fallback",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the cleaner performing ordinary one-target dishwashing",
            new[] { fixture.Cleaner.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "the pawn visibly performs ordinary Doing dishes with Pick Up And Haul off",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new TimeControlActionStep(
            "finish ordinary fallback washing",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "all recovered exact ware units finish clean outside inventory",
            _ => fixture.AllRecoveredWareEventuallyClean(),
            new EndToEndDeadline(1_800, 7_000, TimeSpan.FromSeconds(70)));
        yield return new TimeControlActionStep(
            "pause after save-load fallback cleaning",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select one exact finally clean recovered unit",
            new[] { fixture.FirstDirtyWareId },
            additive: false);
        yield return new ScreenshotStep(
            "ordinary one-target fallback visibly finishes the recovered ware clean",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "save-load fallback conserves exact identities and unrelated personal inventory",
            _ => fixture.AssertRecoveredFinalConservation());
    }
}

internal sealed class PickUpAndHaulDishwashingFixture
{
    private const string TrackerTypeName = "PickUpAndHaul.CompHauledToInventory";
    private const string UnloadDriverTypeName = "PickUpAndHaul.JobDriver_UnloadYourHauledInventory";
    private Map map;
    private readonly IntVec3 waterCell;
    private readonly HashSet<IntVec3> stockpileCells;
    private Thing unrelatedMedicine;
    private IReadOnlyList<ThingWithComps> ware;
    private readonly string cleanerId;
    private readonly string unrelatedMedicineId;
    private readonly string[] wareIds;
    private ThingWithComps? firstCleanedWare;
    private ThingWithComps? firstDirtyWare;

    private PickUpAndHaulDishwashingFixture(
        Map map,
        Pawn cleaner,
        IntVec3 waterCell,
        HashSet<IntVec3> stockpileCells,
        Thing unrelatedMedicine,
        IReadOnlyList<ThingWithComps> ware)
    {
        this.map = map;
        Cleaner = cleaner;
        this.waterCell = waterCell;
        this.stockpileCells = stockpileCells;
        this.unrelatedMedicine = unrelatedMedicine;
        this.ware = ware;
        cleanerId = cleaner.ThingID;
        unrelatedMedicineId = unrelatedMedicine.ThingID;
        wareIds = ware.Select(item => item.ThingID).ToArray();
    }

    internal Pawn Cleaner { get; private set; }
    internal string[] WareIds => wareIds;
    internal string[] AllVisibleIds => WareIds.Concat(new[] { Cleaner.ThingID }).ToArray();
    internal string FirstCleanedWareId => (firstCleanedWare ?? throw new InvalidOperationException(
        "The sequential-wash checkpoint must identify the first cleaned ware before inspection.")).ThingID;
    internal string FirstDirtyWareId => (firstDirtyWare ??= ware.First(item =>
        !ReferenceEquals(item, firstCleanedWare) && item.GetComp<CompSanitation>()!.IsDirty)).ThingID;

    internal static PickUpAndHaulDishwashingFixture Create(IEndToEndContext context, string pawnName)
    {
        var map = Current.Game.CurrentMap;
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);
        var waterCell = center + new IntVec3(0, 0, -3);
        SetReloadSafeTemporaryTerrain(context, map, waterCell, TerrainDefOf.WaterShallow);
        HandwashingE2EFixture.PreserveSettings(context);
        var settings = ImmersiveChefsMod.Settings;
        var priorPickUpAndHaul = settings.PickUpAndHaul;
        context.DeferCleanup(() => settings.PickUpAndHaul = priorPickUpAndHaul);
        settings.PickUpAndHaul = OptionalIntegrationMode.Auto;
        settings.PreferDishwashers = false;
        settings.AllowTerrainHandwashing = true;
        settings.DishwashingWorkScale = 1f;

        var cleaner = HandwashingE2EFixture.CreateInactiveCleaner(pawnName);
        foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
        {
            if (!cleaner.WorkTypeIsDisabled(workType))
            {
                cleaner.workSettings.SetPriority(workType, 0);
            }
        }

        GenSpawn.Spawn(cleaner, center + new IntVec3(-3, 0, -2), map);
        var unrelatedMedicine = ThingMaker.MakeThing(ThingDefOf.MedicineIndustrial);
        unrelatedMedicine.stackCount = 3;
        EndToEndAssert.True(
            cleaner.inventory!.innerContainer.TryAdd(unrelatedMedicine, canMergeWithExistingStacks: false),
            "The dish cleaner must begin with an unrelated medicine stack in personal inventory.");

        var ware = new[]
        {
            MakeDirtyWare("ImmersiveChefs_Plate", ThingDefOf.Steel),
            MakeDirtyWare("ImmersiveChefs_Cutlery", ThingDefOf.WoodLog),
            MakeDirtyWare("ImmersiveChefs_Cookware", ThingDefOf.Steel)
        };
        for (var index = 0; index < ware.Length; index++)
        {
            GenSpawn.Spawn(ware[index], center + new IntVec3(-2 + index, 0, -1), map);
        }

        var zone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
        map.zoneManager.RegisterZone(zone);
        var stockpileCells = new HashSet<IntVec3>();
        for (var x = 2; x <= 4; x++)
        {
            for (var z = 1; z <= 3; z++)
            {
                var cell = center + new IntVec3(x, 0, z);
                zone.AddCell(cell);
                stockpileCells.Add(cell);
            }
        }

        zone.settings.Priority = StoragePriority.Critical;
        zone.settings.filter.SetDisallowAll();
        foreach (var item in ware)
        {
            zone.settings.filter.SetAllow(item.def, allow: true);
        }
        zone.settings.filter.SetAllow(
            DefDatabase<SpecialThingFilterDef>.GetNamed("ImmersiveChefs_AllowCleanKitchenware"),
            allow: true);
        zone.settings.filter.SetAllow(
            DefDatabase<SpecialThingFilterDef>.GetNamed("ImmersiveChefs_AllowDirtyKitchenware"),
            allow: true);

        map.resourceCounter.UpdateResourceCounts();
        var fixture = new PickUpAndHaulDishwashingFixture(
            map,
            cleaner,
            waterCell,
            stockpileCells,
            unrelatedMedicine,
            ware);
        fixture.AssertInstalledTrackerShape();
        return fixture;
    }

    internal void ActivateCleaning()
    {
        Cleaner.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 1);
        Cleaner.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }

    internal void DisableCleaning()
    {
        Cleaner.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 0);
    }

    internal void DisablePickUpAndHaulAndCleaning()
    {
        DisableCleaning();
        ImmersiveChefsMod.Settings.PickUpAndHaul = OptionalIntegrationMode.Off;
    }

    internal void ResolveLoadedFixtures()
    {
        map = Current.Game.CurrentMap;
        Cleaner = map.mapPawns.AllPawnsSpawned.Single(pawn => pawn.ThingID == cleanerId);
        var inventory = Cleaner.inventory?.innerContainer ?? throw new InvalidOperationException(
            "The loaded dish cleaner lost the inventory required by the saved batch.");
        unrelatedMedicine = inventory.Single(item => item.ThingID == unrelatedMedicineId);
        var loadedThings = map.listerThings.AllThings
            .Concat(map.mapPawns.AllPawnsSpawned.SelectMany(pawn =>
                pawn.inventory?.innerContainer ?? Enumerable.Empty<Thing>()))
            .Concat(map.mapPawns.AllPawnsSpawned
                .Select(pawn => pawn.carryTracker?.CarriedThing)
                .OfType<Thing>())
            .ToArray();
        ware = wareIds.Select(id => (ThingWithComps)loadedThings.Single(item => item.ThingID == id)).ToArray();
        firstCleanedWare = ware.Single(item => !item.GetComp<CompSanitation>()!.IsDirty);
    }

    internal bool AllWareTrackedInInventoryAtSource(bool allMustBeDirty)
    {
        return Cleaner.CurJobDef == ImmersiveChefsDefOf.ImmersiveChefs_DoDishes &&
               !Cleaner.CurJob!.GetTarget(TargetIndex.B).HasThing &&
               Cleaner.CurJob.GetTarget(TargetIndex.B).Cell == waterCell &&
               Cleaner.Position.DistanceToSquared(waterCell) <= 2 &&
               ware.All(item => ReferenceEquals(item.holdingOwner, Cleaner.inventory?.innerContainer)) &&
               ware.All(IsTracked) &&
               (!allMustBeDirty || ware.All(item => item.GetComp<CompSanitation>()!.IsDirty)) &&
               UnrelatedInventoryIsUntouched();
    }

    internal bool ObservePartialSequentialWash()
    {
        if (!AllWareTrackedInInventoryAtSource(allMustBeDirty: false))
        {
            return false;
        }

        var clean = ware.Where(item => !item.GetComp<CompSanitation>()!.IsDirty).ToArray();
        if (clean.Length != 1 || ware.Count(item => item.GetComp<CompSanitation>()!.IsDirty) != 2)
        {
            return false;
        }

        firstCleanedWare = clean[0];
        return true;
    }

    internal bool ObserveNativeUnloadJob()
    {
        return Cleaner.jobs.curDriver?.GetType().FullName == UnloadDriverTypeName &&
               ware.All(item => !item.GetComp<CompSanitation>()!.IsDirty) &&
               UnrelatedInventoryIsUntouched();
    }

    internal bool InterruptionStateIsConserved()
    {
        return Cleaner.drafter?.Drafted == true &&
               Cleaner.CurJobDef != ImmersiveChefsDefOf.ImmersiveChefs_DoDishes &&
               ware.All(item => ReferenceEquals(item.holdingOwner, Cleaner.inventory?.innerContainer)) &&
               ware.All(IsTracked) &&
               ware.Count(item => !item.GetComp<CompSanitation>()!.IsDirty) == 1 &&
               ware.Count(item => item.GetComp<CompSanitation>()!.IsDirty) == 2 &&
               UnrelatedInventoryIsUntouched();
    }

    internal bool AllWareStored(int cleanCount, int dirtyCount)
    {
        return ware.All(item => item.Spawned && item.Map == map && stockpileCells.Contains(item.Position)) &&
               ware.Count(item => !item.GetComp<CompSanitation>()!.IsDirty) == cleanCount &&
               ware.Count(item => item.GetComp<CompSanitation>()!.IsDirty) == dirtyCount &&
               ware.All(item => !IsTracked(item)) &&
               UnrelatedInventoryIsUntouched();
    }

    internal bool AllWareRecoveredAfterAdapterBecameUnavailable()
    {
        return Cleaner.CurJobDef != ImmersiveChefsDefOf.ImmersiveChefs_DoDishes &&
               ware.All(item => item.Spawned && item.Map == map) &&
               ware.All(item => !IsTracked(item)) &&
               ware.Count(item => !item.GetComp<CompSanitation>()!.IsDirty) == 1 &&
               ware.Count(item => item.GetComp<CompSanitation>()!.IsDirty) == 2 &&
               UnrelatedInventoryIsUntouched();
    }

    internal bool ObserveOrdinarySingleTargetFallback()
    {
        return Cleaner.CurJobDef == ImmersiveChefsDefOf.ImmersiveChefs_DoDishes &&
               Cleaner.CurJob is { } job &&
               job.targetQueueA.NullOrEmpty() &&
               job.GetTarget(TargetIndex.A).HasThing &&
               ware.Any(item => ReferenceEquals(item, job.GetTarget(TargetIndex.A).Thing));
    }

    internal bool AllRecoveredWareEventuallyClean()
    {
        return ware.All(item => item.Spawned && item.Map == map) &&
               ware.All(item => !item.GetComp<CompSanitation>()!.IsDirty) &&
               ware.All(item => !IsTracked(item)) &&
               UnrelatedInventoryIsUntouched();
    }

    internal void AssertRecoveredFinalConservation()
    {
        EndToEndAssert.True(AllRecoveredWareEventuallyClean(),
            "Every exact saved ware unit must recover to the map and finish through valid ordinary one-target jobs.");
        EndToEndAssert.Equal(3, ware.Select(item => item.ThingID).Distinct().Count(),
            "Save-load adapter fallback must retain all three exact physical identities.");
        EndToEndAssert.True(ware.All(item => item.stackCount == 1),
            "Each exact recovered kitchenware fixture must remain one physical unit.");
        EndToEndAssert.Equal(3, TotalPhysicalKitchenwareUnits(),
            "Save-load adapter fallback must neither duplicate nor lose a kitchenware unit across any holder.");
        EndToEndAssert.True(UnrelatedInventoryIsUntouched(),
            "Save-load recovery must leave unrelated personal medicine in the pawn inventory.");
    }

    private int TotalPhysicalKitchenwareUnits()
    {
        return map.listerThings.AllThings
            .Concat(map.mapPawns.AllPawnsSpawned.SelectMany(pawn =>
                pawn.inventory?.innerContainer ?? Enumerable.Empty<Thing>()))
            .Concat(map.mapPawns.AllPawnsSpawned
                .Select(pawn => pawn.carryTracker?.CarriedThing)
                .OfType<Thing>())
            .Distinct()
            .Where(item => item.def.GetModExtension<KitchenwareExtension>()?.product is
                KitchenwareProduct.Plate or KitchenwareProduct.Cutlery or KitchenwareProduct.Cookware)
            .Sum(item => item.stackCount);
    }

    internal void AssertFinalConservation(int cleanCount, int dirtyCount)
    {
        EndToEndAssert.True(AllWareStored(cleanCount, dirtyCount),
            "Every exact collected ware unit must return through the upstream stockpiling path with its committed sanitation state.");
        EndToEndAssert.Equal(3, ware.Select(item => item.ThingID).Distinct().Count(),
            "The batch must retain three distinct physical Thing identities.");
        EndToEndAssert.True(ware.All(item => item.stackCount == 1),
            "Each exact returned ware fixture must remain one physical unit.");
        var physicalUnits = map.listerThings.AllThings
            .Where(item => item.def.GetModExtension<KitchenwareExtension>()?.product is
                KitchenwareProduct.Plate or KitchenwareProduct.Cutlery or KitchenwareProduct.Cookware)
            .Sum(item => item.stackCount);
        EndToEndAssert.Equal(3, physicalUnits,
            "Batched washing must neither duplicate nor lose a kitchenware unit.");
        EndToEndAssert.True(UnrelatedInventoryIsUntouched(),
            "Pick Up And Haul must not register, unload, merge, or otherwise mutate unrelated personal medicine.");
    }

    internal EndToEndGizmoOption RequiredDraftToggle(IEndToEndContext context)
    {
        var candidates = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { Cleaner.ThingID }, Array.Empty<string>())
            .Where(option =>
                !option.Disabled &&
                option.Interaction == EndToEndGizmoInteraction.Toggle &&
                string.Equals(option.HotKeyDefName, "Command_ColonistDraft", StringComparison.Ordinal))
            .ToArray();
        EndToEndAssert.Equal(1, candidates.Length,
            "The selected cleaner must expose exactly one current native Draft toggle.");
        return candidates[0];
    }

    private bool UnrelatedInventoryIsUntouched()
    {
        return unrelatedMedicine is { Destroyed: false, stackCount: 3 } &&
               ReferenceEquals(unrelatedMedicine.holdingOwner, Cleaner.inventory?.innerContainer) &&
               !IsTracked(unrelatedMedicine);
    }

    private bool IsTracked(Thing thing)
    {
        return GetTrackedThings().Cast<object>().Any(item => ReferenceEquals(item, thing));
    }

    private IEnumerable GetTrackedThings()
    {
        var tracker = Cleaner.AllComps.Single(comp => comp.GetType().FullName == TrackerTypeName);
        var method = tracker.GetType().GetMethod(
            "GetHashSet",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);
        EndToEndAssert.True(
            method is not null && method.ReturnType == typeof(HashSet<Thing>),
            "The exact installed Pick Up And Haul tracker must expose public HashSet<Thing> GetHashSet().");
        return (IEnumerable)method!.Invoke(tracker, null)!;
    }

    private void AssertInstalledTrackerShape()
    {
        var trackers = Cleaner.AllComps.Where(comp => comp.GetType().FullName == TrackerTypeName).ToArray();
        EndToEndAssert.Equal(1, trackers.Length,
            "The exact installed Pick Up And Haul mod must attach one tracked-inventory component to the cleaner.");
        EndToEndAssert.Equal("PickUpAndHaul", trackers[0].GetType().Assembly.GetName().Name,
            "The tracked-inventory component must come from the exact supported assembly.");
        EndToEndAssert.Equal(new Version(1, 0, 0, 0), trackers[0].GetType().Assembly.GetName().Version,
            "The tracked-inventory component must retain the exact supported assembly version.");
    }

    private static ThingWithComps MakeDirtyWare(string defName, ThingDef stuff)
    {
        var item = FoodSearchE2EFixture.MakeCleanWare(defName, stuff);
        item.GetComp<CompSanitation>()!.MarkDirty();
        return item;
    }

    private static void SetReloadSafeTemporaryTerrain(
        IEndToEndContext context,
        Map map,
        IntVec3 cell,
        TerrainDef terrain)
    {
        var original = map.terrainGrid.TerrainAt(cell);
        context.DeferCleanup(() =>
        {
            var loadedMap = Current.Game?.CurrentMap;
            if (loadedMap is not null && cell.InBounds(loadedMap))
            {
                loadedMap.terrainGrid.SetTerrain(cell, original);
            }
        });
        map.terrainGrid.SetTerrain(cell, terrain);
    }
}
