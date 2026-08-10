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
    "immersive-chefs.terrain-handwashing-lifecycle",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.immersivechefs",
    MaxFrames = 4_200,
    MaxGameTicks = 12_000,
    MaxWallClockSeconds = 180)]
public sealed class TerrainHandwashingLifecycleTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn cleaner = null!;
    private ThingWithComps plate = null!;
    private IntVec3 waterCell;
    private string plateId = string.Empty;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        HandwashingE2EFixture.PreserveSettings(context);
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);
        waterCell = center + new IntVec3(3, 0, -2);
        HandwashingE2EFixture.SetTemporaryTerrain(context, map, waterCell, TerrainDefOf.WaterShallow);

        cleaner = HandwashingE2EFixture.CreateInactiveCleaner("Terrain dish cleaner");
        GenSpawn.Spawn(cleaner, center + new IntVec3(-1, 0, -2), map);
        plate = HandwashingE2EFixture.MakeDirtyPlate(ThingDefOf.WoodLog);
        GenSpawn.Spawn(plate, center + new IntVec3(-2, 0, -2), map);
        plateId = plate.ThingID;

        var settings = ImmersiveChefsMod.Settings;
        settings.PreferDishwashers = true;
        settings.AllowTerrainHandwashing = true;
        settings.DishwashingWorkScale = 0.25f;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before terrain handwashing",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "terrain is the selected handwashing destination without Dubs",
            _ => HandwashingE2EFixture.AssertTerrainDestination(cleaner, plate, waterCell));
        yield return new SelectionActionStep(
            "select the dirty plate before terrain handwashing",
            new[] { plate.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the cleaner plate and water terrain",
            new[] { cleaner.ThingID, plate.ThingID },
            paddingPixels: 260);
        yield return new ScreenshotStep(
            "dirty plate and ordinary cleaner beside the only reachable water terrain",
            Array.Empty<string>(),
            paddingPixels: 0);

        HandwashingE2EFixture.ActivateCleaner(cleaner);
        yield return new TimeControlActionStep(
            "run ordinary Cleaning work toward terrain water",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the cleaner carries the exact plate to terrain water",
            _ => HandwashingE2EFixture.IsDoingDishesAt(cleaner, waterCell, plateId),
            new EndToEndDeadline(900, 3_000, TimeSpan.FromSeconds(35)));
        yield return new TimeControlActionStep(
            "pause while the cleaner hand washes at terrain water",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the cleaner hand washing at terrain water",
            new[] { cleaner.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the native terrain handwashing action",
            new[] { cleaner.ThingID },
            paddingPixels: 260);
        yield return new ScreenshotStep(
            "ordinary Doing dishes job visibly uses the terrain water cell",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "finish terrain handwashing",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the exact terrain-washed plate returns clean",
            _ => plate.Spawned &&
                 plate.GetComp<CompSanitation>() is { IsDirty: false, WashedInWildWater: true },
            new EndToEndDeadline(1_200, 5_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause after terrain handwashing",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "terrain handwashing preserves identity and records wild water",
            _ =>
            {
                EndToEndAssert.Equal(plateId, plate.ThingID,
                    "Terrain handwashing must return the exact plate identity.");
                EndToEndAssert.Equal(1, plate.stackCount,
                    "Terrain handwashing must return exactly one physical plate.");
                HandwashingE2EFixture.AssertTotalPlateUnits(map, 1);
                EndToEndAssert.Equal(WashProvenance.WildWater, plate.GetComp<CompSanitation>()!.WashProvenance,
                    "Terrain handwashing must record wild-water provenance.");
            });
        yield return new SelectionActionStep(
            "select the exact terrain-washed plate",
            new[] { plate.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the terrain-washed plate and water cell",
            new[] { plate.ThingID, cleaner.ThingID },
            paddingPixels: 260);
        yield return new ScreenshotStep(
            "exact plate visibly reports clean without exposing wild-water provenance",
            Array.Empty<string>(),
            paddingPixels: 0);

        ImmersiveChefsMod.Settings.AllowTerrainHandwashing = false;
        plate.GetComp<CompSanitation>()!.MarkDirty();
        cleaner.jobs.EndCurrentJob(JobCondition.InterruptForced);
        var disabledProbeEndTick = Find.TickManager.TicksGame + 500;
        yield return new TimeControlActionStep(
            "let ordinary Cleaning reconsider while terrain fallback is disabled",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the disabled terrain fallback remains unused for 500 game ticks",
            _ => Find.TickManager.TicksGame >= disabledProbeEndTick,
            new EndToEndDeadline(600, 1_500, TimeSpan.FromSeconds(20)));
        yield return new TimeControlActionStep(
            "pause after the disabled terrain fallback probe",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "disabling terrain handwashing prevents a terrain-only job",
            _ =>
            {
                EndToEndAssert.False(
                    WorkGiver_DoDishes.TryFindDestination(cleaner, plate, out var _ignoredDestination),
                    "No handwashing destination may remain when terrain is the only source and the toggle is off.");
                EndToEndAssert.True(plate.Spawned && plate.GetComp<CompSanitation>()!.IsDirty,
                    "The exact plate must remain spawned and dirty while terrain handwashing is disabled.");
                HandwashingE2EFixture.AssertTotalPlateUnits(map, 1);
                EndToEndAssert.False(cleaner.CurJobDef == ImmersiveChefsDefOf.ImmersiveChefs_DoDishes,
                    "Ordinary work selection must not start Doing dishes without a valid source.");
            });
        yield return new SelectionActionStep(
            "select the still-dirty plate with terrain fallback disabled",
            new[] { plate.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the idle cleaner dirty plate and disabled water fallback",
            new[] { cleaner.ThingID, plate.ThingID },
            paddingPixels: 260);
        yield return new ScreenshotStep(
            "dirty plate remains untouched after terrain fallback is disabled",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "terrain handwashing lifecycle result",
            _ => new Dictionary<string, string>
            {
                ["plate"] = plateId,
                ["terrain"] = waterCell.ToString(),
                ["wildWaterObserved"] = "True",
                ["disabledFallbackObserved"] = "True"
            });
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.dubs-sink-handwashing-priority",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "Dubwise.DubsBadHygiene",
    "fumblesneeze.immersivechefs",
    MaxFrames = 6_600,
    MaxGameTicks = 23_000,
    MaxWallClockSeconds = 280)]
public sealed class DubsSinkHandwashingPriorityTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn cleaner = null!;
    private ThingWithComps plate = null!;
    private ThingWithComps sink = null!;
    private ThingWithComps tower = null!;
    private IntVec3 waterCell;
    private string plateId = string.Empty;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        HandwashingE2EFixture.PreserveSettings(context);
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);
        waterCell = center + new IntVec3(3, 0, -2);
        HandwashingE2EFixture.SetTemporaryTerrain(context, map, waterCell, TerrainDefOf.WaterShallow);

        sink = DispenserE2EFixture.SpawnBuilding(map, "KitchenSink", center + new IntVec3(3, 0, 2));
        tower = HandwashingE2EFixture.SpawnDubsSinkWaterSupply(map, sink, 10f);
        EndToEndAssert.True(DubsWaterAdapter.IsPlumbedDubsFixture(sink),
            "The real kitchen sink must expose the validated Dubs pipe component.");

        cleaner = HandwashingE2EFixture.CreateInactiveCleaner("Dubs sink dish cleaner");
        GenSpawn.Spawn(cleaner, center + new IntVec3(-1, 0, -2), map);
        plate = HandwashingE2EFixture.MakeDirtyPlate(ThingDefOf.Steel);
        GenSpawn.Spawn(plate, center + new IntVec3(-2, 0, -2), map);
        plateId = plate.ThingID;

        var settings = ImmersiveChefsMod.Settings;
        settings.PreferDishwashers = true;
        settings.AllowTerrainHandwashing = true;
        settings.DishwashingWorkScale = 0.25f;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before Dubs sink handwashing",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new TimeControlActionStep(
            "let the native Dubs plumbing network settle",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the real Dubs sink and tower settle onto one network",
            _ => HandwashingE2EFixture.DubsFixturesShareNetwork(sink, tower),
            new EndToEndDeadline(900, 2_000, TimeSpan.FromSeconds(30)));
        yield return new TimeControlActionStep(
            "pause with the supplied Dubs sink ready",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the shared Dubs network exposes the exact supplied water",
            _ =>
            {
                var storedWater = HandwashingE2EFixture.ReadDubsNetworkWater(sink);
                EndToEndAssert.True(HandwashingE2EFixture.Nearly(storedWater, 10f),
                    $"The shared Dubs network must expose the exact 10 L fixture supply, but exposed {storedWater} L.");
                EndToEndAssert.True(DubsWaterAdapter.IsOperationalFixture(sink),
                    "The exact supplied Dubs kitchen sink must be operational.");
                EndToEndAssert.True(DubsWaterAdapter.CanSupplyCycleWater(sink, 1f),
                    "The exact supplied Dubs kitchen sink must expose at least one washing charge.");
            });
        yield return new AssertionStep(
            "the supplied Dubs kitchen sink outranks terrain water",
            _ =>
            {
                EndToEndAssert.True(
                    WorkGiver_DoDishes.TryFindDestination(cleaner, plate, out var destination) &&
                    ReferenceEquals(destination.Target.Thing, sink) &&
                    destination.Provenance == WashProvenance.Safe,
                    "The supplied Dubs kitchen sink must be the exact safe handwashing destination.");
                EndToEndAssert.True(HandwashingE2EFixture.Nearly(
                        HandwashingE2EFixture.ReadDubsNetworkWater(sink),
                        10f),
                    "The connected Dubs network must start at exactly 10 liters.");
            });
        yield return new SelectionActionStep(
            "select the supplied Dubs water tower before washing",
            new[] { tower.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the supplied sink tower cleaner plate and terrain fallback",
            new[] { sink.ThingID, tower.ThingID, cleaner.ThingID, plate.ThingID },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "connected Dubs network visibly starts with ten liters beside the terrain fallback",
            Array.Empty<string>(),
            paddingPixels: 0);

        HandwashingE2EFixture.ActivateCleaner(cleaner);
        yield return new TimeControlActionStep(
            "run ordinary Cleaning work toward the Dubs sink",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the cleaner carries the exact plate to the supplied Dubs sink",
            _ => HandwashingE2EFixture.IsDoingDishesAt(cleaner, sink, plateId) &&
                 HandwashingE2EFixture.Nearly(HandwashingE2EFixture.ReadDubsNetworkWater(sink), 9f),
            new EndToEndDeadline(1_200, 4_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause during the supplied Dubs sink wash",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the cleaner washing at the Dubs sink",
            new[] { cleaner.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame native Dubs sink handwashing",
            new[] { sink.ThingID, tower.ThingID, cleaner.ThingID },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "ordinary Doing dishes job visibly uses the connected kitchen sink",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new SelectionActionStep(
            "select the Dubs tower after the handwash debit",
            new[] { tower.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "Dubs network visibly reports one consumed liter during the same wash",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "finish supplied sink handwashing",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the exact sink-washed plate returns clean",
            _ => plate.Spawned && !plate.GetComp<CompSanitation>()!.IsDirty,
            new EndToEndDeadline(1_200, 5_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause after supplied sink handwashing",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the Dubs sink returns the exact plate safely and debits once",
            _ =>
            {
                EndToEndAssert.Equal(plateId, plate.ThingID,
                    "Dubs sink handwashing must preserve the exact plate identity.");
                EndToEndAssert.Equal(WashProvenance.Safe, plate.GetComp<CompSanitation>()!.WashProvenance,
                    "A supplied Dubs sink must record safe provenance.");
                HandwashingE2EFixture.AssertTotalPlateUnits(map, 1);
                EndToEndAssert.True(
                    HandwashingE2EFixture.Nearly(HandwashingE2EFixture.ReadDubsNetworkWater(sink), 9f),
                    "Completing the same handwash must not debit Dubs water a second time.");
            });
        yield return new SelectionActionStep(
            "select the exact safely sink-washed plate",
            new[] { plate.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the safely sink-washed plate",
            new[] { plate.ThingID, sink.ThingID, tower.ThingID },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "exact plate visibly reports clean after supplied sink handwashing",
            Array.Empty<string>(),
            paddingPixels: 0);

        HandwashingE2EFixture.SetDubsStoredWater(tower, sink, 0f);
        plate.GetComp<CompSanitation>()!.MarkDirty();
        cleaner.jobs.EndCurrentJob(JobCondition.InterruptForced);
        yield return new AssertionStep(
            "an unsupplied Dubs sink falls back to terrain water",
            _ => HandwashingE2EFixture.AssertTerrainDestination(cleaner, plate, waterCell));
        yield return new TimeControlActionStep(
            "run ordinary Cleaning after Dubs supply loss",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the cleaner carries the exact plate to fallback terrain",
            _ => HandwashingE2EFixture.IsDoingDishesAt(cleaner, waterCell, plateId),
            new EndToEndDeadline(1_200, 4_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause during terrain fallback handwashing",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the cleaner after supplied sink loss",
            new[] { cleaner.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the unsupplied sink and native terrain fallback action",
            new[] { cleaner.ThingID, sink.ThingID, tower.ThingID },
            paddingPixels: 240);
        yield return new ScreenshotStep(
            "ordinary Doing dishes visibly falls back from the unsupplied sink to terrain",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new TimeControlActionStep(
            "finish terrain fallback handwashing",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the exact fallback-washed plate returns clean",
            _ => plate.Spawned &&
                 plate.GetComp<CompSanitation>() is { IsDirty: false, WashedInWildWater: true },
            new EndToEndDeadline(1_200, 5_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause after terrain fallback handwashing",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "terrain fallback replaces safe provenance without another Dubs debit",
            _ =>
            {
                EndToEndAssert.Equal(plateId, plate.ThingID,
                    "Sink loss fallback must return the same exact plate.");
                EndToEndAssert.Equal(1, plate.stackCount,
                    "Sink loss fallback must conserve one physical plate.");
                HandwashingE2EFixture.AssertTotalPlateUnits(map, 1);
                EndToEndAssert.Equal(WashProvenance.WildWater, plate.GetComp<CompSanitation>()!.WashProvenance,
                    "Terrain fallback must replace safe provenance with wild-water provenance.");
                EndToEndAssert.True(
                    HandwashingE2EFixture.Nearly(HandwashingE2EFixture.ReadDubsNetworkWater(sink), 0f),
                    "An unsupplied sink must not fabricate or consume water during terrain fallback.");
            });
        yield return new SelectionActionStep(
            "select the exact plate after terrain fallback",
            new[] { plate.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the exact fallback-washed plate and unsupplied sink",
            new[] { plate.ThingID, sink.ThingID, tower.ThingID },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "exact plate reports clean without exposing fallback wash provenance",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "Dubs sink handwashing priority result",
            _ => new Dictionary<string, string>
            {
                ["plate"] = plateId,
                ["sink"] = sink.ThingID,
                ["tower"] = tower.ThingID,
                ["waterBefore"] = "10",
                ["waterAfterSafeWash"] = "9",
                ["fallbackProvenance"] = plate.GetComp<CompSanitation>()!.WashProvenance.ToString()
            });
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.dubs-handwashing-full-fallback-order",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "Dubwise.DubsBadHygiene",
    "fumblesneeze.immersivechefs",
    MaxFrames = 15_600,
    MaxGameTicks = 55_000,
    MaxWallClockSeconds = 540)]
public sealed class DubsHandwashingFullFallbackOrderTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn cleaner = null!;
    private ThingWithComps plate = null!;
    private ThingWithComps kitchenSink = null!;
    private ThingWithComps sinkTower = null!;
    private ThingWithComps basin = null!;
    private ThingWithComps basinTower = null!;
    private ThingWithComps waterTub = null!;
    private ThingWithComps well = null!;
    private IntVec3 terrainCell;
    private string plateId = string.Empty;
    private float basinWaterAfterWash;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        HandwashingE2EFixture.PreserveSettings(context);
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);
        terrainCell = center + new IntVec3(4, 0, -4);
        HandwashingE2EFixture.SetTemporaryTerrain(context, map, terrainCell, TerrainDefOf.WaterShallow);

        kitchenSink = DispenserE2EFixture.SpawnBuilding(
            map,
            "KitchenSink",
            center + new IntVec3(3, 0, 2));
        sinkTower = HandwashingE2EFixture.SpawnDubsSinkWaterSupply(map, kitchenSink, 10f);
        HandwashingE2EFixture.SpawnDubsSewageOutlet(
            map,
            kitchenSink,
            center + new IntVec3(-5, 0, 4));
        basin = DispenserE2EFixture.SpawnBuilding(
            map,
            "BasinStuff",
            center + new IntVec3(3, 0, -1));
        basinTower = DispenserE2EFixture.SpawnDubsWaterSupply(map, basin, 10f);
        HandwashingE2EFixture.SpawnDubsSewageOutlet(
            map,
            basin,
            center + new IntVec3(5, 0, -4));
        waterTub = DispenserE2EFixture.SpawnBuilding(
            map,
            "WashBucket",
            center + new IntVec3(1, 0, -3));
        HandwashingE2EFixture.SetDubsHauledWaterUses(waterTub, 3);
        well = DispenserE2EFixture.SpawnBuilding(
            map,
            "PrimitiveWell",
            center + new IntVec3(-3, 0, -3));

        cleaner = HandwashingE2EFixture.CreateInactiveCleaner("Dubs fallback dish cleaner");
        GenSpawn.Spawn(cleaner, center + new IntVec3(-1, 0, 0), map);
        plate = HandwashingE2EFixture.MakeDirtyPlate(ThingDefOf.Steel);
        GenSpawn.Spawn(plate, center + new IntVec3(0, 0, 0), map);
        plateId = plate.ThingID;

        var settings = ImmersiveChefsMod.Settings;
        settings.PreferDishwashers = true;
        settings.AllowTerrainHandwashing = true;
        settings.DishwashingWorkScale = 0.25f;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "settle both exact Dubs plumbing networks",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the kitchen sink and basin each pass their native supplied Dubs report",
            _ => HandwashingE2EFixture.DubsFixtureAllowsAndWorks(cleaner, kitchenSink, 1f) &&
                 HandwashingE2EFixture.DubsFixtureAllowsAndWorks(cleaner, basin, 1f),
            new EndToEndDeadline(900, 2_000, TimeSpan.FromSeconds(30)));
        yield return new TimeControlActionStep(
            "pause before the ordered handwashing sequence",
            paused: true,
            EndToEndGameSpeed.Normal);

        foreach (var step in WashAtObject(
                     "connected kitchen sink",
                     kitchenSink,
                     WashProvenance.Safe,
                     () => HandwashingE2EFixture.Nearly(
                         HandwashingE2EFixture.ReadDubsNetworkWater(kitchenSink),
                         9f)))
        {
            yield return step;
        }

        yield return new AssertionStep(
            "disallow colonists through the kitchen sink's native Dubs fixture policy",
            _ =>
            {
                HandwashingE2EFixture.SetDubsFixtureAllowsColonists(kitchenSink, false);
                EndToEndAssert.False(
                    HandwashingE2EFixture.DubsFixtureAllowsAndWorks(cleaner, kitchenSink, 1f),
                    "The real Dubs kitchen sink must reject this colonist before fallback selection.");
            });
        foreach (var step in WashAtObject(
                     "connected basin fallback",
                     basin,
                     WashProvenance.Safe,
                     () => HandwashingE2EFixture.Nearly(
                         HandwashingE2EFixture.ReadDubsNetworkWater(basin),
                         9f)))
        {
            yield return step;
        }

        yield return new AssertionStep(
            "remove basin water so its native Working report selects the hauled-water tier",
            _ =>
            {
                basinWaterAfterWash = HandwashingE2EFixture.ReadDubsNetworkWater(basin);
                HandwashingE2EFixture.SetDubsStoredWater(basinTower, basin, 0f);
                EndToEndAssert.False(
                    HandwashingE2EFixture.DubsFixtureAllowsAndWorks(cleaner, basin, 1f),
                    "The real Dubs basin must reject work after its supplied water is removed.");
            });
        foreach (var step in WashAtObject(
                     "hauled-water tub fallback",
                     waterTub,
                     WashProvenance.WildWater,
                     () => HandwashingE2EFixture.ReadDubsHauledWaterUses(waterTub) == 2))
        {
            yield return step;
        }

        yield return new AssertionStep(
            "empty the hauled-water tub so the primitive well becomes eligible",
            _ => HandwashingE2EFixture.SetDubsHauledWaterUses(waterTub, 0));
        foreach (var step in WashAtObject(
                     "primitive well fallback",
                     well,
                     WashProvenance.WildWater,
                     () => HandwashingE2EFixture.ReadDubsHauledWaterUses(waterTub) == 0))
        {
            yield return step;
        }

        yield return new AssertionStep(
            "forbid the well so water terrain becomes the final fallback",
            _ => well.SetForbidden(true, warnOnFail: false));
        foreach (var step in WashAtTerrain())
        {
            yield return step;
        }

        yield return new CheckpointStep(
            "full Dubs handwashing fallback order result",
            _ => new Dictionary<string, string>
            {
                ["plate"] = plateId,
                ["sink"] = kitchenSink.ThingID,
                ["basin"] = basin.ThingID,
                ["waterTub"] = waterTub.ThingID,
                ["well"] = well.ThingID,
                ["terrain"] = terrainCell.ToString(),
                ["sinkWaterAfter"] = HandwashingE2EFixture.ReadDubsNetworkWater(kitchenSink).ToString("R"),
                ["basinWaterAfterWash"] = basinWaterAfterWash.ToString("R"),
                ["basinWaterFinal"] = HandwashingE2EFixture.ReadDubsNetworkWater(basin).ToString("R"),
                ["tubUsesAfter"] = HandwashingE2EFixture.ReadDubsHauledWaterUses(waterTub).ToString()
            });
    }

    private IEnumerable<EndToEndStep> WashAtObject(
        string label,
        ThingWithComps source,
        WashProvenance expectedProvenance,
        Func<bool> debitAssertion)
    {
        yield return new AssertionStep(
            $"prepare the exact plate for {label}",
            _ =>
            {
                plate.GetComp<CompSanitation>()!.MarkDirty();
                cleaner.jobs.EndCurrentJob(JobCondition.InterruptForced);
                HandwashingE2EFixture.AssertObjectDestination(
                    cleaner,
                    plate,
                    source,
                    expectedProvenance);
            });
        HandwashingE2EFixture.ActivateCleaner(cleaner);
        yield return new TimeControlActionStep(
            $"run ordinary Cleaning toward the {label}",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            $"the cleaner carries the exact plate to the {label}",
            _ => HandwashingE2EFixture.IsDoingDishesAt(cleaner, source, plateId),
            new EndToEndDeadline(1_200, 4_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            $"pause during the {label} wash",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            $"select the cleaner using the {label}",
            new[] { cleaner.ThingID },
            additive: false);
        yield return new CameraActionStep(
            $"frame the cleaner and {label}",
            new[] { cleaner.ThingID, source.ThingID },
            paddingPixels: 240);
        yield return new ScreenshotStep(
            $"ordinary Doing dishes visibly uses the {label}",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new TimeControlActionStep(
            $"finish the {label} wash",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            $"the exact plate returns clean from the {label}",
            _ => plate.Spawned &&
                 plate.GetComp<CompSanitation>() is { IsDirty: false } sanitation &&
                 sanitation.WashProvenance == expectedProvenance,
            new EndToEndDeadline(1_200, 5_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            $"pause after the {label} wash",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            $"the {label} consumes once and preserves the exact plate",
            _ =>
            {
                EndToEndAssert.True(debitAssertion(), $"The {label} must consume its exact one-wash charge once.");
                EndToEndAssert.Equal(plateId, plate.ThingID,
                    $"The {label} must return the same exact plate.");
                EndToEndAssert.Equal(1, plate.stackCount,
                    $"The {label} must conserve one physical plate.");
                HandwashingE2EFixture.AssertTotalPlateUnits(map, 1);
            });
    }

    private IEnumerable<EndToEndStep> WashAtTerrain()
    {
        yield return new AssertionStep(
            "prepare the exact plate for final terrain fallback",
            _ =>
            {
                plate.GetComp<CompSanitation>()!.MarkDirty();
                cleaner.jobs.EndCurrentJob(JobCondition.InterruptForced);
                HandwashingE2EFixture.AssertTerrainDestination(cleaner, plate, terrainCell);
            });
        HandwashingE2EFixture.ActivateCleaner(cleaner);
        yield return new TimeControlActionStep(
            "run ordinary Cleaning toward final terrain fallback",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the cleaner carries the exact plate to final terrain fallback",
            _ => HandwashingE2EFixture.IsDoingDishesAt(cleaner, terrainCell, plateId),
            new EndToEndDeadline(1_200, 4_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause during final terrain handwashing",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the cleaner at final terrain fallback",
            new[] { cleaner.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame final terrain handwashing",
            new[] { cleaner.ThingID },
            paddingPixels: 260);
        yield return new ScreenshotStep(
            "ordinary Doing dishes visibly reaches terrain only after every object tier is unavailable",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new TimeControlActionStep(
            "finish final terrain handwashing",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the exact terrain-washed plate returns clean",
            _ => plate.Spawned &&
                 plate.GetComp<CompSanitation>() is { IsDirty: false, WashedInWildWater: true },
            new EndToEndDeadline(1_200, 5_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause after final terrain handwashing",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the exact plate after the full fallback chain",
            new[] { plate.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the exact plate and exhausted fallback fixture",
            new[] { plate.ThingID, waterTub.ThingID, well.ThingID },
            paddingPixels: 240);
        yield return new ScreenshotStep(
            "same plate visibly records wild-water cleaning after the final fallback",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "the full fallback chain conserves the exact plate once",
            _ =>
            {
                EndToEndAssert.Equal(plateId, plate.ThingID,
                    "The final terrain fallback must return the same exact plate.");
                EndToEndAssert.Equal(1, plate.stackCount,
                    "The full fallback chain must conserve one physical plate.");
                HandwashingE2EFixture.AssertTotalPlateUnits(map, 1);
            });
    }
}

internal static class HandwashingE2EFixture
{
    internal static void PreserveSettings(IEndToEndContext context)
    {
        var settings = ImmersiveChefsMod.Settings;
        var priorPreferDishwashers = settings.PreferDishwashers;
        var priorTerrain = settings.AllowTerrainHandwashing;
        var priorWorkScale = settings.DishwashingWorkScale;
        var priorDubsMode = settings.DubsBadHygiene;
        context.DeferCleanup(() =>
        {
            settings.PreferDishwashers = priorPreferDishwashers;
            settings.AllowTerrainHandwashing = priorTerrain;
            settings.DishwashingWorkScale = priorWorkScale;
            settings.DubsBadHygiene = priorDubsMode;
        });
        settings.DubsBadHygiene = OptionalIntegrationMode.Auto;
    }

    internal static void SetTemporaryTerrain(
        IEndToEndContext context,
        Map map,
        IntVec3 cell,
        TerrainDef terrain)
    {
        var original = map.terrainGrid.TerrainAt(cell);
        context.DeferCleanup(() =>
        {
            if (cell.InBounds(map))
            {
                map.terrainGrid.SetTerrain(cell, original);
            }
        });
        map.terrainGrid.SetTerrain(cell, terrain);
    }

    internal static Pawn CreateInactiveCleaner(string name)
    {
        for (var attempt = 0; attempt < 64; attempt++)
        {
            var pawn = FoodSearchE2EFixture.CreateColonist(name);
            if (!pawn.WorkTypeIsDisabled(WorkTypeDefOf.Cleaning) &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) >= 0.9f &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) >= 0.9f)
            {
                if (pawn.needs?.food is { } food)
                {
                    food.CurLevelPercentage = 1f;
                }

                if (pawn.needs?.rest is { } rest)
                {
                    rest.CurLevelPercentage = 1f;
                }

                for (var hour = 0; hour < 24; hour++)
                {
                    pawn.timetable?.SetAssignment(hour, TimeAssignmentDefOf.Work);
                }

                pawn.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 0);
                return pawn;
            }

            pawn.Destroy(DestroyMode.Vanish);
        }

        throw new EndToEndAssertionException("Could not generate a capable handwashing cleaner.");
    }

    internal static void ActivateCleaner(Pawn pawn)
    {
        pawn.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 1);
        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }

    internal static ThingWithComps MakeDirtyPlate(ThingDef stuff)
    {
        var plate = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Plate", stuff);
        plate.GetComp<CompSanitation>()!.MarkDirty();
        return plate;
    }

    internal static void AssertTotalPlateUnits(Map map, int expected)
    {
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        var spawned = map.listerThings.ThingsOfDef(plateDef).Sum(thing => thing.stackCount);
        var carried = map.mapPawns.AllPawnsSpawned
            .Select(pawn => pawn.carryTracker.CarriedThing)
            .Where(thing => thing?.def == plateDef)
            .Sum(thing => thing!.stackCount);
        var inventoried = map.mapPawns.AllPawnsSpawned
            .Where(pawn => pawn.inventory is not null)
            .SelectMany(pawn => pawn.inventory!.innerContainer)
            .Where(thing => thing.def == plateDef)
            .Sum(thing => thing.stackCount);
        EndToEndAssert.Equal(expected, spawned + carried + inventoried,
            "Handwashing must conserve the exact total number of physical plate units.");
    }

    internal static void AssertTerrainDestination(Pawn pawn, Thing ware, IntVec3 cell)
    {
        EndToEndAssert.True(
            WorkGiver_DoDishes.TryFindDestination(pawn, ware, out var destination) &&
            !destination.Target.HasThing &&
            destination.Target.Cell == cell &&
            destination.Provenance == WashProvenance.WildWater,
            "The exact reachable water-terrain cell must be selected with wild-water provenance.");
    }

    internal static void AssertObjectDestination(
        Pawn pawn,
        Thing ware,
        Thing source,
        WashProvenance provenance)
    {
        EndToEndAssert.True(
            WorkGiver_DoDishes.TryFindDestination(pawn, ware, out var destination) &&
            ReferenceEquals(destination.Target.Thing, source) &&
            destination.Provenance == provenance,
            $"The exact {source.def.defName} source must be selected with {provenance} provenance.");
    }

    internal static bool IsDoingDishesAt(Pawn pawn, Thing destination, string wareId) =>
        pawn.CurJobDef == ImmersiveChefsDefOf.ImmersiveChefs_DoDishes &&
        ReferenceEquals(pawn.CurJob?.GetTarget(TargetIndex.B).Thing, destination) &&
        pawn.carryTracker.CarriedThing?.ThingID == wareId;

    internal static bool IsDoingDishesAt(Pawn pawn, IntVec3 destination, string wareId) =>
        pawn.CurJobDef == ImmersiveChefsDefOf.ImmersiveChefs_DoDishes &&
        !pawn.CurJob!.GetTarget(TargetIndex.B).HasThing &&
        pawn.CurJob.GetTarget(TargetIndex.B).Cell == destination &&
        pawn.carryTracker.CarriedThing?.ThingID == wareId;

    internal static float ReadDubsNetworkWater(ThingWithComps fixture)
    {
        var pipe = GetExactDubsComp(fixture, "DubsBadHygiene.CompPipe", "plumbing");
        var network = ReadRequiredProperty(
            pipe,
            GetRequiredPublicInstanceProperty(pipe.GetType(), "pipeNet"),
            "Dubs plumbing network");
        EndToEndAssert.NotNull(network,
            "The exact Dubs fixture must remain attached to a plumbing network.");
        var waterStorage = GetRequiredPublicInstanceProperty(network!.GetType(), "WaterStorage");
        EndToEndAssert.True(waterStorage.PropertyType == typeof(float),
            "The exact Dubs network WaterStorage property must remain a public Single.");
        var value = ReadRequiredProperty(network, waterStorage, "Dubs network water storage");
        EndToEndAssert.True(value is float,
            "The exact Dubs network WaterStorage value must remain a Single.");
        return (float)value!;
    }

    internal static ThingWithComps SpawnDubsSinkWaterSupply(
        Map map,
        ThingWithComps sink,
        float storedWater)
    {
        var pipeDef = DefDatabase<ThingDef>.GetNamed("sewagePipeHidden");
        var towerCell = sink.Position + new IntVec3(-5, 0, -1);
        // Dubs destroys an existing CompPipe parent when a later pipe is spawned in its occupied cells.
        // Bridge only the cells between the tower's two-cell footprint and the sink's three-cell footprint.
        for (var x = towerCell.x + 2; x <= sink.Position.x - 2; x++)
        {
            var pipe = ThingMaker.MakeThing(pipeDef, ThingDefOf.Steel);
            pipe.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(pipe, new IntVec3(x, 0, sink.Position.z), map);
        }

        var tower = DispenserE2EFixture.SpawnBuilding(map, "WaterTowerS", towerCell);
        SetDubsStoredWaterField(tower, storedWater);
        RefreshDubsPipeNetworks(sink);

        return tower;
    }

    internal static bool DubsFixturesShareNetwork(ThingWithComps first, ThingWithComps second)
    {
        var firstNetwork = ReadDubsNetwork(first);
        return firstNetwork is not null && ReferenceEquals(firstNetwork, ReadDubsNetwork(second));
    }

    internal static void SetDubsStoredWater(
        ThingWithComps tower,
        ThingWithComps sink,
        float value)
    {
        SetDubsStoredWaterField(tower, value);
        EndToEndAssert.True(Nearly(ReadDubsNetworkWater(sink), value),
            "The connected Dubs network must observe the fixture water change.");
    }

    internal static int ReadDubsHauledWaterUses(ThingWithComps source)
    {
        var field = GetRequiredPublicInstanceField(source.GetType(), "WaterUsesRemaining");
        var value = ReadRequiredField(source, field, "Dubs hauled-water uses");
        EndToEndAssert.True(value is int,
            "The installed Dubs hauled-water use count must remain an Int32.");
        return (int)value!;
    }

    internal static void SetDubsHauledWaterUses(ThingWithComps source, int uses)
    {
        var field = GetRequiredPublicInstanceField(source.GetType(), "WaterUsesRemaining");
        EndToEndAssert.True(field.FieldType == typeof(int),
            "The installed Dubs WaterUsesRemaining field must remain a public Int32.");
        WriteRequiredField(source, field, Math.Max(0, uses), "Dubs hauled-water uses");
    }

    internal static void SetDubsFixtureAllowsColonists(ThingWithComps fixture, bool allowed)
    {
        var field = GetRequiredPublicInstanceField(fixture.GetType(), "AllowColonists");
        EndToEndAssert.True(field.FieldType == typeof(bool),
            "The installed Dubs fixture AllowColonists field must remain a public Boolean.");
        WriteRequiredField(fixture, field, allowed, "Dubs fixture colonist permission");
    }

    internal static bool DubsFixtureAllowsAndWorks(Pawn pawn, ThingWithComps fixture, float waterUsed)
    {
        var pawnAllowed = fixture.GetType().GetMethod(
            "PawnAllowed",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(Pawn) },
            modifiers: null);
        var working = fixture.GetType().GetMethod(
            "Working",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(float) },
            modifiers: null);
        EndToEndAssert.True(
            pawnAllowed?.ReturnType == typeof(AcceptanceReport) &&
            working?.ReturnType == typeof(AcceptanceReport),
            "The installed Dubs fixture must retain exact PawnAllowed(Pawn) and Working(float) reports.");
        return pawnAllowed!.Invoke(fixture, new object[] { pawn }) is AcceptanceReport pawnReport &&
               pawnReport.Accepted &&
               working!.Invoke(fixture, new object[] { waterUsed }) is AcceptanceReport workingReport &&
               workingReport.Accepted;
    }

    internal static ThingWithComps SpawnDubsSewageOutlet(
        Map map,
        ThingWithComps fixture,
        IntVec3 outletCell)
    {
        var pipeDef = DefDatabase<ThingDef>.GetNamed("sewagePipeHidden");
        var outlet = DispenserE2EFixture.SpawnBuilding(map, "SewageOutlet", outletCell);
        var cursor = new IntVec3(
            outletCell.x + Math.Sign(fixture.Position.x - outletCell.x),
            0,
            outletCell.z);
        while (cursor.x != fixture.Position.x)
        {
            SpawnPipeIfMissing(map, pipeDef, cursor);
            cursor = new IntVec3(
                cursor.x + Math.Sign(fixture.Position.x - cursor.x),
                0,
                cursor.z);
        }
        while (cursor.z != fixture.Position.z)
        {
            SpawnPipeIfMissing(map, pipeDef, cursor);
            cursor = new IntVec3(
                cursor.x,
                0,
                cursor.z + Math.Sign(fixture.Position.z - cursor.z));
        }

        RefreshDubsPipeNetworks(fixture);
        EndToEndAssert.True(
            DubsFixturesShareNetwork(fixture, outlet),
            "The real Dubs sewage outlet must join the fixture's exact plumbing network.");
        return outlet;
    }

    private static object? ReadDubsNetwork(ThingWithComps fixture)
    {
        var pipe = GetExactDubsComp(fixture, "DubsBadHygiene.CompPipe", "plumbing");
        return ReadRequiredProperty(
            pipe,
            GetRequiredPublicInstanceProperty(pipe.GetType(), "pipeNet"),
            "Dubs plumbing network");
    }

    private static void SetDubsStoredWaterField(ThingWithComps tower, float value)
    {
        var storage = GetExactDubsComp(
            tower,
            "DubsBadHygiene.CompWaterStorage",
            "water storage");
        var field = GetRequiredPublicInstanceField(storage.GetType(), "WaterStorage");
        EndToEndAssert.True(field.FieldType == typeof(float),
            "The installed Dubs water storage field must remain a public Single.");
        WriteRequiredField(storage, field, value, "Dubs stored-water fixture");
    }

    private static void RefreshDubsPipeNetworks(ThingWithComps fixture)
    {
        var pipe = GetExactDubsComp(fixture, "DubsBadHygiene.CompPipe", "plumbing");
        var mapCompField = GetRequiredPublicInstanceField(pipe.GetType(), "MapComp");
        var mapComp = ReadRequiredField(pipe, mapCompField, "Dubs hygiene map component");
        EndToEndAssert.NotNull(mapComp,
            "The spawned Dubs fixture must register with its hygiene map component.");
        var mapCompType = mapComp!.GetType();
        EndToEndAssert.Equal("DubsBadHygiene.MapComponent_Hygiene", mapCompType.FullName,
            "The installed Dubs CompPipe.MapComp field must retain its exact supported map-component identity.");
        var pipeComp = ReadRequiredProperty(
            mapComp,
            GetRequiredPublicInstanceProperty(mapCompType, "PipeComp"),
            "Dubs hygiene pipe-map component");
        EndToEndAssert.NotNull(pipeComp,
            "The spawned Dubs fixture must expose its registered pipe-map component.");
        var pipeCompType = pipeComp!.GetType();
        EndToEndAssert.Equal("DubsBadHygiene.HygienePipeMapComp", pipeCompType.FullName,
            "The installed Dubs MapComponent_Hygiene.PipeComp property must retain its exact supported identity.");
        InvokeRequiredVoidMethod(
            pipeComp,
            GetRequiredPublicInstanceVoidMethod(pipeCompType, "DirtyAllPipeGrids"),
            "mark the Dubs plumbing grids dirty");
        InvokeRequiredVoidMethod(
            pipeComp,
            GetRequiredPublicInstanceVoidMethod(pipeCompType, "RegenPipeGrids"),
            "regenerate the Dubs plumbing grids");
    }

    private static void SpawnPipeIfMissing(Map map, ThingDef pipeDef, IntVec3 cell)
    {
        if (cell.GetThingList(map).Any(thing => thing.def == pipeDef))
        {
            return;
        }

        var pipe = ThingMaker.MakeThing(pipeDef, ThingDefOf.Steel);
        pipe.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(pipe, cell, map);
    }

    private static ThingComp GetExactDubsComp(
        ThingWithComps fixture,
        string fullTypeName,
        string role)
    {
        var matches = fixture.AllComps
            .Where(comp => string.Equals(
                comp.GetType().FullName,
                fullTypeName,
                StringComparison.Ordinal))
            .ToArray();
        EndToEndAssert.Equal(1, matches.Length,
            $"The exact {fixture.def.defName} fixture must expose exactly one public-shape Dubs {role} comp ({fullTypeName}).");
        return matches[0];
    }

    private static PropertyInfo GetRequiredPublicInstanceProperty(Type type, string name)
    {
        var property = type.GetProperty(name, BindingFlags.Public | BindingFlags.Instance);
        EndToEndAssert.True(
            property is not null && property.CanRead && property.GetIndexParameters().Length == 0,
            $"The installed Dubs type {type.FullName} must expose a readable, non-indexed public instance property named {name}.");
        return property!;
    }

    private static FieldInfo GetRequiredPublicInstanceField(Type type, string name)
    {
        var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance);
        EndToEndAssert.NotNull(field,
            $"The installed Dubs type {type.FullName} must expose a public instance field named {name}.");
        return field!;
    }

    private static MethodInfo GetRequiredPublicInstanceVoidMethod(Type type, string name)
    {
        var method = type.GetMethod(
            name,
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);
        EndToEndAssert.True(
            method is not null && !method.IsGenericMethod && method.ReturnType == typeof(void),
            $"The installed Dubs type {type.FullName} must expose a public, parameterless, non-generic void method named {name}.");
        return method!;
    }

    private static object? ReadRequiredProperty(object target, PropertyInfo property, string operation)
    {
        try
        {
            return property.GetValue(target);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Could not read {operation} through {property.DeclaringType?.FullName}.{property.Name}.", exception);
        }
    }

    private static object? ReadRequiredField(object target, FieldInfo field, string operation)
    {
        try
        {
            return field.GetValue(target);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Could not read {operation} through {field.DeclaringType?.FullName}.{field.Name}.", exception);
        }
    }

    private static void WriteRequiredField(object target, FieldInfo field, object value, string operation)
    {
        try
        {
            field.SetValue(target, value);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Could not write {operation} through {field.DeclaringType?.FullName}.{field.Name}.", exception);
        }
    }

    private static void InvokeRequiredVoidMethod(object target, MethodInfo method, string operation)
    {
        try
        {
            method.Invoke(target, Array.Empty<object>());
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException($"Could not {operation} through {method.DeclaringType?.FullName}.{method.Name}().", exception);
        }
    }

    internal static bool Nearly(float left, float right) => Math.Abs(left - right) < 0.01f;
}
