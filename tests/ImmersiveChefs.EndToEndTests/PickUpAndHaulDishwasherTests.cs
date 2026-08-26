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
    "immersive-chefs.pick-up-and-haul-local-dishwasher-batch",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "Dubwise.DubsBadHygiene",
    "Mehni.PickUpAndHaul",
    "fumblesneeze.immersivechefs",
    MaxFrames = 3_600,
    MaxGameTicks = 12_000,
    MaxWallClockSeconds = 150)]
public sealed class PickUpAndHaulLocalDishwasherBatchTest : IRimWorldEndToEndTest
{
    private PickUpAndHaulDishwasherFixture fixture = null!;

    public void Arrange(IEndToEndContext context)
    {
        fixture = PickUpAndHaulDishwasherFixture.Create(context, requireProcessor: false);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before local dishwasher collection",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "enable ordinary Cleaning for the local appliance",
            _ => fixture.ActivateCleaning());
        yield return new TimeControlActionStep(
            "run one native local-dishwasher collection trip",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "one local-dishwasher job carries the full tracked batch",
            _ => fixture.AllDirtyWareTrackedAtDishwasher(),
            new EndToEndDeadline(1_800, 6_000, TimeSpan.FromSeconds(65)));
        yield return new WaitUntilStep(
            "the local dishwasher owns all three exact units",
            _ => fixture.AllWareOwnedByDishwasher(),
            new EndToEndDeadline(1_800, 6_000, TimeSpan.FromSeconds(65)));
        yield return new TimeControlActionStep(
            "pause after local holder admission",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the loaded local dishwasher",
            new[] { fixture.Dishwasher.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the local dishwasher and cleaner",
            new[] { fixture.Dishwasher.ThingID, fixture.Cleaner.ThingID },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "the non-Processor appliance visibly owns the full one-trip batch",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "local holder admission releases every upstream tracking claim exactly once",
            _ => fixture.AssertLoadedConservation());
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.pick-up-and-haul-processor-dishwasher-batch",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "syrchalis.processor.framework",
    "Dubwise.DubsBadHygiene",
    "Mehni.PickUpAndHaul",
    "fumblesneeze.immersivechefs",
    MaxFrames = 7_200,
    MaxGameTicks = 30_000,
    MaxWallClockSeconds = 300)]
public sealed class PickUpAndHaulProcessorDishwasherBatchTest : IRimWorldEndToEndTest
{
    private PickUpAndHaulDishwasherFixture fixture = null!;

    public void Arrange(IEndToEndContext context)
    {
        fixture = PickUpAndHaulDishwasherFixture.Create(context, requireProcessor: true);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before one-trip dishwasher collection",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the dirty mixed kitchenware",
            fixture.WareIds,
            additive: false);
        yield return new CameraActionStep(
            "frame dirty ware cleaner dishwasher and clean shelf",
            fixture.VisibleThingIds,
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "dirty mixed kitchenware awaits one cleaner beside the supplied dishwasher",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new AssertionStep(
            "enable the ordinary Cleaning workgiver",
            _ => fixture.ActivateCleaning());
        yield return new TimeControlActionStep(
            "run the ordinary Doing dishes job",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "one Doing dishes job collects all exact units before one dishwasher trip",
            _ => fixture.AllDirtyWareTrackedAtDishwasher(),
            new EndToEndDeadline(1_800, 6_000, TimeSpan.FromSeconds(65)));
        yield return new TimeControlActionStep(
            "pause while the cleaner carries the full dirty batch",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the cleaner carrying the tracked dirty batch",
            new[] { fixture.Cleaner.ThingID },
            additive: false);
        yield return new PawnInspectTabActionStep(
            "open the cleaner Gear tab before dishwasher admission",
            fixture.Cleaner.ThingID,
            EndToEndPawnInspectTab.Gear);
        yield return new ScreenshotStep(
            "Pick Up And Haul visibly carries the dirty mixed batch on one dishwasher trip",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "admit each tracked unit through Processor Framework",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the exact batch transfers from tracked inventory into one dishwasher",
            _ => fixture.AllWareOwnedByDishwasher(),
            new EndToEndDeadline(1_500, 5_000, TimeSpan.FromSeconds(55)));
        yield return new TimeControlActionStep(
            "pause after exact ownership transfer",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the loaded Processor dishwasher",
            new[] { fixture.Dishwasher.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "the appliance visibly owns all three exact stacks after one collection trip",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "run the supplied native dishwasher cycle",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the Processor dishwasher makes visible progress after one Dubs debit",
            _ => fixture.ObserveCycleProgress(),
            new EndToEndDeadline(1_500, 8_000, TimeSpan.FromSeconds(65)));
        yield return new AssertionStep(
            "enable ordinary hauling while cleaning remains available",
            _ => fixture.ActivateHaulingWhileCleaningRemainsEnabled());
        yield return new WaitUntilStep(
            "native Processor emptying returns every exact unit clean to the map",
            _ => fixture.AllCleanOutputsAwaitStorage(),
            new EndToEndDeadline(2_400, 10_000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep(
            "pause on the clean output batch",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select all exact clean outputs together",
            fixture.WareIds,
            additive: false);
        yield return new ScreenshotStep(
            "the completed dishwasher has returned the exact clean mixed batch",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new AssertionStep(
            "open the clean kitchenware shelf to ordinary hauling",
            _ => fixture.EnableCleanStorage());
        yield return new TimeControlActionStep(
            "let Pick Up And Haul batch the clean outputs to storage",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the same cleaner visibly tracks multiple clean outputs for one shelf trip",
            _ => fixture.ObserveNativeCleanBatchHaul(),
            new EndToEndDeadline(1_800, 6_000, TimeSpan.FromSeconds(65)));
        yield return new WaitUntilStep(
            "all exact clean outputs reach the kitchen shelf",
            _ => fixture.AllWareStoredClean(),
            new EndToEndDeadline(1_800, 7_000, TimeSpan.FromSeconds(70)));
        yield return new TimeControlActionStep(
            "pause after native batch unloading",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the neatly stored clean kitchenware",
            fixture.WareIds,
            additive: false);
        yield return new ScreenshotStep(
            "Pick Up And Haul has returned the exact clean batch to its kitchen shelf",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "conserve exact identities sanitation and physical units",
            _ => fixture.AssertFinalConservation());
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.pick-up-and-haul-processor-dishwasher-interruption",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "syrchalis.processor.framework",
    "Dubwise.DubsBadHygiene",
    "Mehni.PickUpAndHaul",
    "fumblesneeze.immersivechefs",
    MaxFrames = 4_800,
    MaxGameTicks = 16_000,
    MaxWallClockSeconds = 180)]
public sealed class PickUpAndHaulProcessorDishwasherInterruptionTest : IRimWorldEndToEndTest
{
    private PickUpAndHaulDishwasherFixture fixture = null!;

    public void Arrange(IEndToEndContext context)
    {
        fixture = PickUpAndHaulDishwasherFixture.Create(context, requireProcessor: true);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before partial Processor admission",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the interruption cleaner",
            new[] { fixture.Cleaner.ThingID },
            additive: false);
        yield return new AssertionStep(
            "enable ordinary Cleaning before partial admission",
            _ => fixture.ActivateCleaning());
        yield return new TimeControlActionStep(
            "run the one-trip Doing dishes job",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "one exact unit enters the dishwasher while two remain tracked",
            _ => fixture.ObservePartialDishwasherAdmission(),
            new EndToEndDeadline(1_800, 6_000, TimeSpan.FromSeconds(65)));
        yield return new TimeControlActionStep(
            "pause before the player interrupts admission",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new PawnInspectTabActionStep(
            "open Gear on the two still-carried dirty units",
            fixture.Cleaner.ThingID,
            EndToEndPawnInspectTab.Gear);
        yield return new ScreenshotStep(
            "one unit belongs to the dishwasher while two remain in tracked inventory",
            Array.Empty<string>(),
            paddingPixels: 0);
        var draft = fixture.RequiredDraftToggle(context);
        yield return new GizmoActionStep(
            "draft through the native pawn toggle to interrupt admission",
            new[] { fixture.Cleaner.ThingID },
            draft.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: draft.StableId);
        yield return new WaitUntilStep(
            "interruption leaves admitted and carried ownership disjoint",
            _ => fixture.PartialOwnershipRemainsDisjoint(),
            new EndToEndDeadline(300, 500, TimeSpan.FromSeconds(20)));
        yield return new AssertionStep(
            "disable new Cleaning work before upstream unloading",
            _ => fixture.DisableCleaning());
        draft = fixture.RequiredDraftToggle(context);
        yield return new GizmoActionStep(
            "undraft through the native pawn toggle",
            new[] { fixture.Cleaner.ThingID },
            draft.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: draft.StableId);
        yield return new TimeControlActionStep(
            "run only the queued upstream return",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "unadmitted units leave personal inventory through native unloading",
            _ => fixture.UnadmittedWareReturnedAfterInterruption(),
            new EndToEndDeadline(1_500, 5_000, TimeSpan.FromSeconds(55)));
        yield return new TimeControlActionStep(
            "pause on the disjoint interrupted ownership",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the still-dirty returned units",
            fixture.ReturnedUnadmittedWareIds,
            additive: false);
        yield return new CameraActionStep(
            "frame the admitted dishwasher and returned unadmitted units",
            fixture.InterruptedVisibleThingIds,
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "admitted ware remains appliance-owned while unadmitted ware returns dirty",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "partial admission conserves all exact physical units",
            _ => fixture.AssertInterruptedConservation());
    }
}

internal sealed class PickUpAndHaulDishwasherFixture
{
    private const string TrackerTypeName = "PickUpAndHaul.CompHauledToInventory";
    private const string HaulDriverTypeName = "PickUpAndHaul.JobDriver_HaulToInventory";
    private readonly Map map;
    private readonly ThingWithComps waterTower;
    private readonly IReadOnlyList<ThingWithComps> ware;
    private readonly Zone_Stockpile cleanStorage;
    private readonly HashSet<IntVec3> cleanStorageCells;
    private bool observedCleanBatchHaul;
    private ThingWithComps? admittedBeforeInterruption;

    private PickUpAndHaulDishwasherFixture(
        Map map,
        Pawn cleaner,
        ThingWithComps dishwasher,
        ThingWithComps waterTower,
        IReadOnlyList<ThingWithComps> ware,
        Zone_Stockpile cleanStorage,
        HashSet<IntVec3> cleanStorageCells)
    {
        this.map = map;
        Cleaner = cleaner;
        Dishwasher = dishwasher;
        this.waterTower = waterTower;
        this.ware = ware;
        this.cleanStorage = cleanStorage;
        this.cleanStorageCells = cleanStorageCells;
    }

    internal Pawn Cleaner { get; }
    internal ThingWithComps Dishwasher { get; }
    internal string[] WareIds => ware.Select(item => item.ThingID).ToArray();
    internal string[] VisibleThingIds => WareIds.Concat(new[]
    {
        Cleaner.ThingID,
        Dishwasher.ThingID,
        waterTower.ThingID
    }).ToArray();
    internal string[] ReturnedUnadmittedWareIds => ware
        .Where(item => !ReferenceEquals(item, admittedBeforeInterruption))
        .Select(item => item.ThingID)
        .ToArray();
    internal string[] InterruptedVisibleThingIds => ReturnedUnadmittedWareIds.Concat(new[]
    {
        Cleaner.ThingID,
        Dishwasher.ThingID,
        waterTower.ThingID
    }).ToArray();

    internal static PickUpAndHaulDishwasherFixture Create(
        IEndToEndContext context,
        bool requireProcessor)
    {
        var map = Current.Game.CurrentMap;
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);
        HandwashingE2EFixture.PreserveSettings(context);
        var settings = ImmersiveChefsMod.Settings;
        var priorPickUpAndHaul = settings.PickUpAndHaul;
        context.DeferCleanup(() => settings.PickUpAndHaul = priorPickUpAndHaul);
        settings.PickUpAndHaul = OptionalIntegrationMode.Auto;
        settings.DubsBadHygiene = OptionalIntegrationMode.Auto;
        settings.PreferDishwashers = true;
        settings.DishwashingWorkScale = requireProcessor ? 0.25f : 4f;

        var powerCell = SpawnPowerSource(map, center + new IntVec3(5, 0, 3));
        SpawnConduitsOutside(map, center, 7, 5, powerCell.OccupiedRect().Cells.ToHashSet());
        var dishwasher = DispenserE2EFixture.SpawnBuilding(
            map,
            "ImmersiveChefs_Dishwasher",
            center + new IntVec3(2, 0, 1));
        var tower = DispenserE2EFixture.SpawnDubsWaterSupply(map, dishwasher, 10f);
        DispenserE2EFixture.SettlePower(map, new[] { dishwasher }, 400);
        EndToEndAssert.Equal(requireProcessor, ProcessorFrameworkAdapter.Controls(dishwasher),
            requireProcessor
                ? "The exact Processor group must use the supported Processor Framework dishwasher."
                : "The exact local group must exercise the Immersive Chefs holder without Processor Framework.");

        var cleaner = HandwashingE2EFixture.CreateInactiveCleaner("Mara Dishrunner");
        GenSpawn.Spawn(cleaner, center + new IntVec3(-4, 0, -2), map);
        EndToEndAssert.Equal(1,
            cleaner.AllComps.Count(comp => comp.GetType().FullName == TrackerTypeName),
            "The supported Pick Up And Haul tracker must attach exactly once.");

        var ware = new[]
        {
            MakeDirtyWare("ImmersiveChefs_Plate", ThingDefOf.Steel),
            MakeDirtyWare("ImmersiveChefs_Cutlery", ThingDefOf.WoodLog),
            MakeDirtyWare("ImmersiveChefs_Cookware", ThingDefOf.Steel)
        };
        for (var index = 0; index < ware.Length; index++)
        {
            GenSpawn.Spawn(ware[index], center + new IntVec3(-3 + index, 0, -1), map);
        }

        var zone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
        map.zoneManager.RegisterZone(zone);
        var cells = new HashSet<IntVec3>();
        for (var x = 2; x <= 4; x++)
        {
            for (var z = -3; z <= -2; z++)
            {
                var cell = center + new IntVec3(x, 0, z);
                zone.AddCell(cell);
                cells.Add(cell);
            }
        }

        zone.settings.Priority = StoragePriority.Critical;
        zone.settings.filter.SetDisallowAll();
        map.resourceCounter.UpdateResourceCounts();
        return new PickUpAndHaulDishwasherFixture(
            map,
            cleaner,
            dishwasher,
            tower,
            ware,
            zone,
            cells);
    }

    internal void ActivateCleaning()
    {
        Cleaner.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 1);
        Cleaner.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }

    internal void ActivateHaulingWhileCleaningRemainsEnabled()
    {
        Cleaner.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 1);
        Cleaner.workSettings.SetPriority(WorkTypeDefOf.Hauling, 1);
        Cleaner.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }

    internal void DisableCleaning()
    {
        Cleaner.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 0);
    }

    internal bool AllDirtyWareTrackedAtDishwasher()
    {
        return Cleaner.CurJobDef == ImmersiveChefsDefOf.ImmersiveChefs_DoDishes &&
               Cleaner.Position.DistanceToSquared(Dishwasher.InteractionCell) <= 2 &&
               ware.All(item => ReferenceEquals(item.holdingOwner, Cleaner.inventory?.innerContainer)) &&
               ware.All(IsTracked) &&
               ware.All(item => item.GetComp<CompSanitation>()!.IsDirty);
    }

    internal bool AllWareOwnedByDishwasher()
    {
        var held = DishwasherHeldWare();
        return ware.All(item => held.Any(candidate => ReferenceEquals(candidate, item))) &&
               ware.All(item => !IsTracked(item)) &&
               ware.All(item => !item.Spawned) &&
               Cleaner.CurJobDef != ImmersiveChefsDefOf.ImmersiveChefs_DoDishes;
    }

    internal bool ObservePartialDishwasherAdmission()
    {
        var held = DishwasherHeldWare();
        var admitted = ware.Where(item => held.Any(candidate => ReferenceEquals(candidate, item))).ToArray();
        var carried = ware.Where(item => ReferenceEquals(item.holdingOwner, Cleaner.inventory?.innerContainer)).ToArray();
        if (admitted.Length != 1 || carried.Length != 2 || !carried.All(IsTracked))
        {
            return false;
        }

        admittedBeforeInterruption = admitted[0];
        return !IsTracked(admittedBeforeInterruption);
    }

    internal bool PartialOwnershipRemainsDisjoint()
    {
        if (admittedBeforeInterruption is null)
        {
            return false;
        }

        var held = DishwasherHeldWare();
        return held.Any(item => ReferenceEquals(item, admittedBeforeInterruption)) &&
               !IsTracked(admittedBeforeInterruption) &&
               ware.Where(item => !ReferenceEquals(item, admittedBeforeInterruption)).All(item =>
                   ReferenceEquals(item.holdingOwner, Cleaner.inventory?.innerContainer) && IsTracked(item));
    }

    internal bool UnadmittedWareReturnedAfterInterruption()
    {
        if (admittedBeforeInterruption is null)
        {
            return false;
        }

        var held = DishwasherHeldWare();
        return held.Any(item => ReferenceEquals(item, admittedBeforeInterruption)) &&
               ware.Where(item => !ReferenceEquals(item, admittedBeforeInterruption)).All(item =>
                   item.Spawned && item.Map == map && item.GetComp<CompSanitation>()!.IsDirty && !IsTracked(item));
    }

    internal bool ObserveCycleProgress()
    {
        return ProcessorFrameworkAdapter.ProgressPercent(Dishwasher) >= 5f &&
               HandwashingE2EFixture.ReadDubsNetworkWater(Dishwasher) < 10f;
    }

    internal bool AllCleanOutputsAwaitStorage()
    {
        return !ProcessorFrameworkAdapter.HasContents(Dishwasher) &&
               ware.All(item => item.Spawned && item.Map == map) &&
               ware.All(item => item.GetComp<CompSanitation>() is
                   { IsDirty: false, WashProvenance: WashProvenance.Safe }) &&
               ware.All(item => !cleanStorageCells.Contains(item.Position)) &&
               ware.All(item => !IsTracked(item));
    }

    internal void EnableCleanStorage()
    {
        foreach (var item in ware)
        {
            cleanStorage.settings.filter.SetAllow(item.def, allow: true);
        }
        cleanStorage.settings.filter.SetAllow(
            DefDatabase<SpecialThingFilterDef>.GetNamed("ImmersiveChefs_AllowCleanKitchenware"),
            allow: true);
        cleanStorage.settings.filter.SetAllow(
            DefDatabase<SpecialThingFilterDef>.GetNamed("ImmersiveChefs_AllowDirtyKitchenware"),
            allow: false);
        map.resourceCounter.UpdateResourceCounts();
        Cleaner.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }

    internal bool ObserveNativeCleanBatchHaul()
    {
        var trackedClean = ware.Count(item => IsTracked(item) &&
                                             ReferenceEquals(item.holdingOwner, Cleaner.inventory?.innerContainer));
        if (Cleaner.jobs.curDriver?.GetType().FullName == HaulDriverTypeName && trackedClean >= 2)
        {
            observedCleanBatchHaul = true;
        }

        return observedCleanBatchHaul;
    }

    internal bool AllWareStoredClean()
    {
        ObserveNativeCleanBatchHaul();
        return observedCleanBatchHaul &&
               ware.All(item => item.Spawned && item.Map == map && cleanStorageCells.Contains(item.Position)) &&
               ware.All(item => item.GetComp<CompSanitation>()!.IsDirty == false) &&
               ware.All(item => !IsTracked(item));
    }

    internal void AssertFinalConservation()
    {
        EndToEndAssert.True(AllWareStoredClean(),
            "The exact clean mixed batch must reach the declared kitchen storage through native hauling.");
        EndToEndAssert.Equal(3, ware.Select(item => item.ThingID).Distinct().Count(),
            "Dishwasher batching must preserve all three exact identities.");
        EndToEndAssert.True(ware.All(item => item.stackCount == 1),
            "Each exact kitchenware fixture must remain one physical unit.");
        EndToEndAssert.Equal(3, map.listerThings.AllThings
            .Where(item => item.def.GetModExtension<KitchenwareExtension>()?.product is
                KitchenwareProduct.Plate or KitchenwareProduct.Cutlery or KitchenwareProduct.Cookware)
            .Sum(item => item.stackCount),
            "The one-trip Processor workflow must neither duplicate nor lose kitchenware.");
    }

    internal void AssertLoadedConservation()
    {
        EndToEndAssert.True(AllWareOwnedByDishwasher(),
            "The exact local dishwasher must own all three one-trip units.");
        EndToEndAssert.True(ware.All(item => !IsTracked(item)),
            "Local appliance admission must release every Pick Up And Haul tracking claim.");
        EndToEndAssert.True(ware.All(item => item.stackCount == 1),
            "Local appliance admission must preserve one physical unit per exact fixture.");
        EndToEndAssert.Equal(3, TotalPhysicalUnitsAcrossHolders(),
            "Local appliance admission must neither duplicate nor lose kitchenware.");
    }

    internal void AssertInterruptedConservation()
    {
        EndToEndAssert.True(UnadmittedWareReturnedAfterInterruption(),
            "Only unadmitted units must return through upstream unloading after interruption.");
        EndToEndAssert.Equal(3, ware.Select(item => item.ThingID).Distinct().Count(),
            "Partial Processor admission must retain all three exact identities.");
        EndToEndAssert.True(ware.All(item => item.stackCount == 1),
            "Partial Processor admission must retain one physical unit per fixture.");
        EndToEndAssert.Equal(3, TotalPhysicalUnitsAcrossHolders(),
            "Partial Processor admission must neither duplicate nor lose a physical unit.");
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

    private int TotalPhysicalUnitsAcrossHolders()
    {
        return map.listerThings.AllThings
            .Concat(map.mapPawns.AllPawnsSpawned.SelectMany(pawn =>
                pawn.inventory?.innerContainer ?? Enumerable.Empty<Thing>()))
            .Concat(DishwasherHeldWare())
            .Distinct()
            .Where(item => item.def.GetModExtension<KitchenwareExtension>()?.product is
                KitchenwareProduct.Plate or KitchenwareProduct.Cutlery or KitchenwareProduct.Cookware)
            .Sum(item => item.stackCount);
    }

    private IReadOnlyList<Thing> DishwasherHeldWare()
    {
        return ProcessorFrameworkAdapter.Controls(Dishwasher)
            ? ProcessorFrameworkAdapter.HeldWare(Dishwasher)
            : Dishwasher.GetComp<CompDishwasher>()?.GetDirectlyHeldThings().Cast<Thing>().ToArray()
              ?? Array.Empty<Thing>();
    }

    private bool IsTracked(Thing thing)
    {
        var tracker = Cleaner.AllComps.Single(comp => comp.GetType().FullName == TrackerTypeName);
        var method = tracker.GetType().GetMethod(
            "GetHashSet",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);
        return method?.Invoke(tracker, null) is IEnumerable tracked &&
               tracked.Cast<object>().Any(candidate => ReferenceEquals(candidate, thing));
    }

    private static ThingWithComps MakeDirtyWare(string defName, ThingDef stuff)
    {
        var item = FoodSearchE2EFixture.MakeCleanWare(defName, stuff);
        item.GetComp<CompSanitation>()!.MarkDirty();
        return item;
    }

    private static ThingWithComps SpawnPowerSource(Map map, IntVec3 cell)
    {
        var def = DefDatabase<ThingDef>.GetNamed("VanometricPowerCell");
        var source = (ThingWithComps)ThingMaker.MakeThing(def, def.MadeFromStuff ? ThingDefOf.Steel : null);
        source.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(source, cell, map, Rot4.North);
        return source;
    }

    private static void SpawnConduitsOutside(
        Map map,
        IntVec3 center,
        int radiusX,
        int radiusZ,
        ISet<IntVec3> excluded)
    {
        var def = DefDatabase<ThingDef>.GetNamed("PowerConduit");
        for (var x = -radiusX; x <= radiusX; x++)
        {
            for (var z = -radiusZ; z <= radiusZ; z++)
            {
                var cell = center + new IntVec3(x, 0, z);
                if (!cell.InBounds(map) || excluded.Contains(cell))
                {
                    continue;
                }

                var conduit = ThingMaker.MakeThing(def);
                conduit.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(conduit, cell, map);
            }
        }
    }
}
