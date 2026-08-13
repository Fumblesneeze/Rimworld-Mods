using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorldDevGateway.EndToEndTesting;
using RimWorldDevGateway.PerformanceTesting;
using Verse;

namespace RimWorldDevGateway.PerformanceTests;

internal static class ImmersiveChefsNeutralContract
{
    public const string Owner = "fumblesneeze.rimworlddevgateway";
    public const string Subject = "fumblesneeze.immersivechefs";
    public const string PresentComparison = "gateway.immersive-chefs-neutral-present";
    public const string AbsentControl = "gateway.immersive-chefs-neutral-control";
    public const string Workload = "immersive-chefs-neutral-colony/v2";
    public const int WarmUpTicks = 1_200;
    public const int SampleTicks = 6_000;
    public const int HumanCount = 8;
    public const int AnimalCount = 6;
    public const string TickCheckpoint = "neutral-native-map-ticks";
}

[RimWorldPerformanceTest(
    "gateway.immersive-chefs-neutral-present.instrumented",
    ImmersiveChefsNeutralContract.Owner,
    ImmersiveChefsNeutralContract.Subject,
    "brrainz.harmony",
    "ludeon.rimworld",
    PerformanceTestContract.CircinusPackageId,
    ImmersiveChefsNeutralContract.Subject,
    WorkloadVersion = ImmersiveChefsNeutralContract.Workload,
    ComparisonId = ImmersiveChefsNeutralContract.PresentComparison,
    WarmUpTicks = ImmersiveChefsNeutralContract.WarmUpTicks,
    SampleTicks = ImmersiveChefsNeutralContract.SampleTicks,
    Repetitions = 3,
    EvidenceLens = PerformanceEvidenceLens.ProductInstrumented)]
[PerformanceMethodSelector(
    PerformanceMethodSelectorKind.Method,
    "RimWorldDevGateway.PerformanceTests.NeutralColonyTickComponent::MapComponentTick()",
    "neutral-native-tick")]
[PerformanceThroughputCheckpoint(ImmersiveChefsNeutralContract.TickCheckpoint, 5_900)]
public sealed class ImmersiveChefsNeutralPresentBenchmark : ImmersiveChefsNeutralColonyBenchmark { }

[RimWorldPerformanceTest(
    "gateway.immersive-chefs-neutral-present.armed-disabled",
    ImmersiveChefsNeutralContract.Owner,
    ImmersiveChefsNeutralContract.Subject,
    "brrainz.harmony",
    "ludeon.rimworld",
    PerformanceTestContract.CircinusPackageId,
    ImmersiveChefsNeutralContract.Subject,
    WorkloadVersion = ImmersiveChefsNeutralContract.Workload,
    ComparisonId = ImmersiveChefsNeutralContract.PresentComparison,
    WarmUpTicks = ImmersiveChefsNeutralContract.WarmUpTicks,
    SampleTicks = ImmersiveChefsNeutralContract.SampleTicks,
    Repetitions = 3,
    EvidenceLens = PerformanceEvidenceLens.ArmedDisabledWrapper)]
[PerformanceMethodSelector(
    PerformanceMethodSelectorKind.Method,
    "RimWorldDevGateway.PerformanceTests.NeutralColonyTickComponent::MapComponentTick()",
    "neutral-native-tick")]
[PerformanceThroughputCheckpoint(ImmersiveChefsNeutralContract.TickCheckpoint, 5_900)]
public sealed class ImmersiveChefsNeutralPresentArmedDisabledBenchmark :
    ImmersiveChefsNeutralColonyBenchmark { }

[RimWorldPerformanceTest(
    "gateway.immersive-chefs-neutral-present.disarmed",
    ImmersiveChefsNeutralContract.Owner,
    ImmersiveChefsNeutralContract.Subject,
    "brrainz.harmony",
    "ludeon.rimworld",
    PerformanceTestContract.CircinusPackageId,
    ImmersiveChefsNeutralContract.Subject,
    WorkloadVersion = ImmersiveChefsNeutralContract.Workload,
    ComparisonId = ImmersiveChefsNeutralContract.PresentComparison,
    WarmUpTicks = ImmersiveChefsNeutralContract.WarmUpTicks,
    SampleTicks = ImmersiveChefsNeutralContract.SampleTicks,
    Repetitions = 3,
    EvidenceLens = PerformanceEvidenceLens.FullyDisarmed)]
[PerformanceMethodSelector(
    PerformanceMethodSelectorKind.Method,
    "RimWorldDevGateway.PerformanceTests.NeutralColonyTickComponent::MapComponentTick()",
    "neutral-native-tick")]
[PerformanceThroughputCheckpoint(ImmersiveChefsNeutralContract.TickCheckpoint, 5_900)]
public sealed class ImmersiveChefsNeutralPresentDisarmedBenchmark :
    ImmersiveChefsNeutralColonyBenchmark { }

[RimWorldPerformanceTest(
    ImmersiveChefsNeutralContract.AbsentControl,
    ImmersiveChefsNeutralContract.Owner,
    ImmersiveChefsNeutralContract.Subject,
    "brrainz.harmony",
    "ludeon.rimworld",
    PerformanceTestContract.CircinusPackageId,
    WorkloadVersion = ImmersiveChefsNeutralContract.Workload,
    WarmUpTicks = ImmersiveChefsNeutralContract.WarmUpTicks,
    SampleTicks = ImmersiveChefsNeutralContract.SampleTicks,
    Repetitions = 3,
    EvidenceLens = PerformanceEvidenceLens.ProductAbsentControl)]
[PerformanceMethodSelector(
    PerformanceMethodSelectorKind.Method,
    "RimWorldDevGateway.PerformanceTests.NeutralColonyTickComponent::MapComponentTick()",
    "neutral-native-tick")]
[PerformanceThroughputCheckpoint(ImmersiveChefsNeutralContract.TickCheckpoint, 5_900)]
public sealed class ImmersiveChefsNeutralAbsentBenchmark : ImmersiveChefsNeutralColonyBenchmark { }

public abstract class ImmersiveChefsNeutralColonyBenchmark :
    IRimWorldPerformanceTest,
    IPerformanceSamplePreparation,
    IPerformanceSampleValidation,
    IPerformanceThroughputCounter
{
    private Map? map;
    private NeutralColonyTickComponent? counter;
    private readonly List<Thing> fixtures = new();

    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap ?? throw new InvalidOperationException(
            "The neutral performance colony requires one playable map.");
        var center = map.Center;
        NormalizeDisposableMap(map);
        counter = new NeutralColonyTickComponent(map);
        map.components.Add(counter);
        context.DeferCleanup(Cleanup);

        var pawnKind = PawnKindDefOf.Colonist;
        for (var index = 0; index < ImmersiveChefsNeutralContract.HumanCount; index++)
        {
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                pawnKind,
                Faction.OfPlayer,
                canGeneratePawnRelations: false,
                fixedBiologicalAge: 30f,
                fixedChronologicalAge: 30f));
            pawn.needs.food.CurLevelPercentage = 0.75f;
            pawn.needs.rest.CurLevelPercentage = 0.9f;
            var cell = center + new IntVec3(-9 + (index % 4) * 6, 0, -7 + (index / 4) * 14);
            Spawn(pawn, cell);
        }

        var animalKind = DefDatabase<PawnKindDef>.GetNamed("Muffalo");
        for (var index = 0; index < ImmersiveChefsNeutralContract.AnimalCount; index++)
        {
            var animal = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                animalKind,
                Faction.OfPlayer,
                canGeneratePawnRelations: false,
                fixedBiologicalAge: 5f,
                fixedChronologicalAge: 5f));
            var cell = center + new IntVec3(-15 + index * 6, 0, 18);
            Spawn(animal, cell);
            animal.needs.food.CurLevelPercentage = 1f;
        }

        for (var index = 0; index < 8; index++)
        {
            var berries = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RawBerries"));
            berries.stackCount = 30;
            var cell = center + new IntVec3(-14 + index * 4, 0, 0);
            Spawn(berries, cell);
        }

        if (map.mapPawns.FreeColonistsSpawnedCount != ImmersiveChefsNeutralContract.HumanCount)
            throw new InvalidOperationException("The neutral fixture did not create exactly eight colonists.");
        var ownedAnimals = map.mapPawns.SpawnedPawnsInFaction(Faction.OfPlayer)
            .Count(pawn => pawn.RaceProps.Animal);
        if (ownedAnimals != ImmersiveChefsNeutralContract.AnimalCount)
            throw new InvalidOperationException("The neutral fixture did not create exactly six animals.");

        Find.CameraDriver.SetRootPosAndSize(center.ToVector3Shifted(), 38f);
    }

    public IEnumerator<EndToEndStep> PrepareSample(IEndToEndContext context)
    {
        var pawns = fixtures.OfType<Pawn>().ToArray();
        if (pawns.Count(pawn => pawn.RaceProps.Humanlike && pawn.Spawned) !=
            ImmersiveChefsNeutralContract.HumanCount ||
            pawns.Count(pawn => pawn.RaceProps.Animal && pawn.Spawned) !=
            ImmersiveChefsNeutralContract.AnimalCount)
            throw new InvalidOperationException("The neutral scenario lost its broad population during warm-up.");
        yield return new CheckpointStep(
            "neutral-colony-manifest",
            _ => new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["workloadVersion"] = ImmersiveChefsNeutralContract.Workload,
                ["humanCount"] = ImmersiveChefsNeutralContract.HumanCount.ToString(),
                ["animalCount"] = ImmersiveChefsNeutralContract.AnimalCount.ToString(),
                ["scenario"] = "ordinary-autonomous-colony",
                ["foodStacksPresent"] = fixtures.Count(thing =>
                    thing.def.defName == "RawBerries" && thing.Spawned).ToString(),
                ["dayOfYear"] = GenLocalDate.DayOfYear(map!).ToString(),
                ["hour"] = GenLocalDate.HourInteger(map!).ToString(),
                ["weather"] = map!.weatherManager.curWeather.defName
            });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield break;
    }

    public void ValidateSample(IEndToEndContext context)
    {
        if (map is null || counter is null || !map.components.Contains(counter))
            throw new InvalidOperationException("The neutral fixture lost its native map tick counter.");
        var pawns = fixtures.OfType<Pawn>().ToArray();
        if (pawns.Length != ImmersiveChefsNeutralContract.HumanCount +
            ImmersiveChefsNeutralContract.AnimalCount || pawns.Any(pawn => pawn.Destroyed || !pawn.Spawned))
            throw new InvalidOperationException("The neutral fixture lost a retained pawn.");
        if (counter.TickCount < ImmersiveChefsNeutralContract.SampleTicks - 100)
            throw new InvalidOperationException("The neutral scenario did not advance through its sample window.");
    }

    public long Read(string id)
    {
        if (!string.Equals(id, ImmersiveChefsNeutralContract.TickCheckpoint, StringComparison.Ordinal))
            throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown neutral throughput checkpoint.");
        if (map is null || counter is null || !map.components.Contains(counter))
            throw new InvalidOperationException("The neutral tick counter is no longer attached to its map.");
        return counter.TickCount;
    }

    private void Spawn(Thing thing, IntVec3 cell)
    {
        if (map is null) throw new InvalidOperationException("The neutral map is not arranged.");
        if (!cell.InBounds(map) || !cell.Walkable(map))
            throw new InvalidOperationException("The neutral fixture cell is not usable: " + cell + ".");
        fixtures.Add(GenSpawn.Spawn(thing, cell, map));
    }

    private static void NormalizeDisposableMap(Map target)
    {
        target.Parent.Tile = new PlanetTile(0);
        target.TileInfo.PrimaryBiome = BiomeDefOf.SeaIce;
        target.TileInfo.temperature = 20f;
        target.TileInfo.rainfall = 0f;
        target.TileInfo.swampiness = 0f;
        target.TileInfo.pollution = 0f;
        Find.TickManager.DebugSetTicksGame(0);
        Find.TickManager.gameStartAbsTick = GenDate.TicksPerYear + 15_000;
        for (var pass = 0; pass < 2; pass++)
        {
            var localTicksIntoYear =
                GenLocalDate.DayOfYear(target) * GenDate.TicksPerDay + GenLocalDate.DayTick(target);
            Find.TickManager.gameStartAbsTick += 15_000 - localTicksIntoYear;
        }
        if (GenLocalDate.DayOfYear(target) != 0 || GenLocalDate.HourInteger(target) != 6)
            throw new InvalidOperationException("Failed to normalize the neutral colony's local date and time.");

        foreach (var cell in target.AllCells)
            if (target.roofGrid.Roofed(cell)) target.roofGrid.SetRoof(cell, null);
        foreach (var thing in target.listerThings.AllThings.ToArray())
        {
            if (!thing.Spawned) continue;
            if (thing.def.destroyable) thing.Destroy(DestroyMode.Vanish);
            else thing.DeSpawn(DestroyMode.Vanish);
        }
        foreach (var cell in target.AllCells)
        {
            target.terrainGrid.SetTerrain(cell, TerrainDefOf.Soil);
            target.snowGrid.SetDepth(cell, 0f);
        }
        target.weatherManager.curWeather = WeatherDefOf.Clear;
        target.weatherManager.lastWeather = WeatherDefOf.Clear;
        target.weatherManager.curWeatherAge = 0;
        target.weatherManager.prevSkyTargetLerp = 1f;
        target.weatherManager.currSkyTargetLerp = 1f;
        target.weatherManager.ResetSkyTargetLerpCache();
        target.mapDrawer.RegenerateEverythingNow();
    }

    private void Cleanup()
    {
        if (map is not null && counter is not null) map.components.Remove(counter);
        for (var index = fixtures.Count - 1; index >= 0; index--)
        {
            var thing = fixtures[index];
            if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish);
        }
        fixtures.Clear();
        counter = null;
        map = null;
    }

}

public sealed class NeutralColonyTickComponent : MapComponent
{
    public NeutralColonyTickComponent(Map map) : base(map) { }
    public long TickCount { get; private set; }
    public override void MapComponentTick() => TickCount++;
}
