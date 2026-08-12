using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using RimWorldDevGateway.EndToEndTesting;
using RimWorldDevGateway.PerformanceTesting;
using Verse;
using Verse.AI;

namespace RimWorldDevGateway.PerformanceTests;

internal static class ImmersiveChefsNeutralContract
{
    public const string Owner = "fumblesneeze.rimworlddevgateway";
    public const string Subject = "fumblesneeze.immersivechefs";
    public const string PresentComparison = "gateway.immersive-chefs-neutral-present";
    public const string AbsentControl = "gateway.immersive-chefs-neutral-control";
    public const string Workload = "immersive-chefs-neutral-colony/v2";
    public const int Seed = 481516;
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
    DeterministicSeed = ImmersiveChefsNeutralContract.Seed,
    WorkloadVersion = ImmersiveChefsNeutralContract.Workload,
    ComparisonId = ImmersiveChefsNeutralContract.PresentComparison,
    WarmUpTicks = ImmersiveChefsNeutralContract.WarmUpTicks,
    SampleTicks = ImmersiveChefsNeutralContract.SampleTicks,
    Repetitions = 3,
    EvidenceLens = PerformanceEvidenceLens.ProductInstrumented,
    ProductAbsentControlId = ImmersiveChefsNeutralContract.AbsentControl)]
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
    DeterministicSeed = ImmersiveChefsNeutralContract.Seed,
    WorkloadVersion = ImmersiveChefsNeutralContract.Workload,
    ComparisonId = ImmersiveChefsNeutralContract.PresentComparison,
    WarmUpTicks = ImmersiveChefsNeutralContract.WarmUpTicks,
    SampleTicks = ImmersiveChefsNeutralContract.SampleTicks,
    Repetitions = 3,
    EvidenceLens = PerformanceEvidenceLens.ArmedDisabledWrapper,
    ProductAbsentControlId = ImmersiveChefsNeutralContract.AbsentControl)]
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
    DeterministicSeed = ImmersiveChefsNeutralContract.Seed,
    WorkloadVersion = ImmersiveChefsNeutralContract.Workload,
    ComparisonId = ImmersiveChefsNeutralContract.PresentComparison,
    WarmUpTicks = ImmersiveChefsNeutralContract.WarmUpTicks,
    SampleTicks = ImmersiveChefsNeutralContract.SampleTicks,
    Repetitions = 3,
    EvidenceLens = PerformanceEvidenceLens.FullyDisarmed,
    ProductAbsentControlId = ImmersiveChefsNeutralContract.AbsentControl)]
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
    DeterministicSeed = ImmersiveChefsNeutralContract.Seed,
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
    IPerformancePostSampleEvidence,
    IPerformanceThroughputCounter
{
    private Map? map;
    private NeutralColonyTickComponent? counter;
    private readonly List<Thing> fixtures = new();
    private readonly Dictionary<Pawn, IntVec3> pawnStartCells = new();
    private readonly Dictionary<Thing, IntVec3> foodStartCells = new();
    private readonly Dictionary<Pawn, Job> sampleWaitJobs = new();
    private string initialFingerprint = string.Empty;

    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap ?? throw new InvalidOperationException(
            "The neutral performance colony requires one playable map.");
        var center = map.Center;
        NormalizeDisposableMap(map);
        var priorUniqueIds = SeedUniqueIds();
        context.DeferCleanup(() => RestoreUniqueIdsWithoutReuse(priorUniqueIds));
        var priorRandState = SeedGlobalRand(ImmersiveChefsNeutralContract.Seed);
        context.DeferCleanup(() => RestoreGlobalRand(priorRandState));
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
            NormalizeHuman(pawn, index);
            pawn.needs.food.CurLevelPercentage = 0.75f;
            pawn.needs.rest.CurLevelPercentage = 0.9f;
            var cell = center + new IntVec3(-9 + (index % 4) * 6, 0, -7 + (index / 4) * 14);
            Spawn(pawn, cell);
            pawnStartCells.Add(pawn, cell);
            pawn.drafter.Drafted = true;
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
            NormalizeAnimal(animal, index);
            var cell = center + new IntVec3(-15 + index * 6, 0, 18);
            Spawn(animal, cell);
            pawnStartCells.Add(animal, cell);
            animal.needs.food.CurLevelPercentage = 1f;
        }

        for (var index = 0; index < 8; index++)
        {
            var berries = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RawBerries"));
            berries.stackCount = 30;
            var cell = center + new IntVec3(-14 + index * 4, 0, 0);
            Spawn(berries, cell);
            foodStartCells.Add(berries, cell);
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
        foreach (var pawn in pawns)
        {
            var age = pawn.RaceProps.Humanlike ? 30L : 5L;
            pawn.ageTracker.AgeBiologicalTicks = age * GenDate.TicksPerYear;
            pawn.ageTracker.AgeChronologicalTicks = age * GenDate.TicksPerYear;
            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: false);
            pawn.pather.StopDead();
            if (pawn.Position != pawnStartCells[pawn])
            {
                pawn.DeSpawn(DestroyMode.Vanish);
                GenSpawn.Spawn(pawn, pawnStartCells[pawn], map!);
            }
            pawn.needs.food.CurLevelPercentage = pawn.RaceProps.Humanlike ? 0.75f : 0.8f;
            if (pawn.needs.rest is { } rest) rest.CurLevelPercentage = 0.9f;
            pawn.inventory?.innerContainer.ClearAndDestroyContents();
            if (pawn.RaceProps.Humanlike) pawn.drafter.Drafted = false;
        }
        foreach (var pair in foodStartCells)
        {
            var food = pair.Key;
            if (food.Destroyed || !food.Spawned || food.stackCount != 30)
                throw new InvalidOperationException("Neutral warmup consumed a deterministic food fixture.");
            if (food.Position != pair.Value)
            {
                food.DeSpawn(DestroyMode.Vanish);
                GenSpawn.Spawn(food, pair.Value, map!);
            }
        }
        AssertSpatialRegistration(pawns.Cast<Thing>().Concat(foodStartCells.Keys));
        ResetIndexedUniqueIds(1_650_000, "activation");
        StartDeterministicWaitJobs(pawns);
        SeedGlobalRand(ImmersiveChefsNeutralContract.Seed + 1);
        var uniqueIdState = ResetSampleUniqueIds();
        var randState = ((ulong)RequireRandStateProperty().GetValue(null, null)!).ToString();
        initialFingerprint = Fingerprint(pawns);
        var workloadState = WorkloadState(pawns, foodStartCells.Keys)
            .Concat(new[] { "rand|" + randState, "ids|" + uniqueIdState })
            .ToArray();
        var workloadFingerprint = PerformanceDeterminismGuard.CanonicalStateSha256(workloadState);
        yield return new CheckpointStep(
            "neutral-colony-manifest",
            _ => new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["workloadVersion"] = ImmersiveChefsNeutralContract.Workload,
                ["deterministicSeed"] = ImmersiveChefsNeutralContract.Seed.ToString(),
                ["humanCount"] = ImmersiveChefsNeutralContract.HumanCount.ToString(),
                ["animalCount"] = ImmersiveChefsNeutralContract.AnimalCount.ToString(),
                ["pawnFingerprintSha256"] = initialFingerprint,
                ["sampleStartStateSha256"] = workloadFingerprint,
                ["sampleStartState"] = string.Join("\n", workloadState),
                ["sampleRandState"] = randState,
                ["sampleUniqueIdState"] = uniqueIdState,
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
        var pawns = fixtures.OfType<Pawn>().OrderBy(pawn => pawn.ThingID, StringComparer.Ordinal).ToArray();
        if (pawns.Length != ImmersiveChefsNeutralContract.HumanCount +
            ImmersiveChefsNeutralContract.AnimalCount || pawns.Any(pawn => pawn.Destroyed || !pawn.Spawned))
            throw new InvalidOperationException("The neutral fixture lost a retained pawn.");
        if (foodStartCells.Keys.Any(food => food.Destroyed || !food.Spawned || food.stackCount < 0))
            throw new InvalidOperationException("The neutral fixture lost a retained food stack.");
        foreach (var pawn in pawns)
        {
            if (!sampleWaitJobs.TryGetValue(pawn, out var wait) ||
                !ReferenceEquals(pawn.CurJob, wait) ||
                wait.def != JobDefOf.Wait_MaintainPosture ||
                !wait.playerForced ||
                wait.expiryInterval != int.MaxValue ||
                wait.checkOverrideOnExpire)
                throw new InvalidOperationException(
                    "A neutral fixture pawn did not retain its exact long-lived native wait job through sampling.");
        }
        AssertSpatialRegistration(pawns.Cast<Thing>().Concat(foodStartCells.Keys));
    }

    public IEnumerator<EndToEndStep> CapturePostSampleEvidence(IEndToEndContext context)
    {
        if (map is null) throw new InvalidOperationException("The neutral map is unavailable after sampling.");
        var pawns = fixtures.OfType<Pawn>().OrderBy(pawn => pawn.ThingID, StringComparer.Ordinal).ToArray();
        var foods = foodStartCells.Keys.OrderBy(food => food.ThingID, StringComparer.Ordinal).ToArray();
        var terminalRows = WorkloadState(pawns, foods);
        var terminalState = PerformanceDeterminismGuard.CanonicalStateSha256(terminalRows);
        yield return new CheckpointStep(
            "neutral-colony-terminal-state",
            _ => new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["workloadVersion"] = ImmersiveChefsNeutralContract.Workload,
                ["humanCount"] = pawns.Count(pawn => pawn.RaceProps.Humanlike).ToString(),
                ["animalCount"] = pawns.Count(pawn => pawn.RaceProps.Animal).ToString(),
                ["foodStackUnits"] = foods.Sum(food => food.stackCount).ToString(),
                ["nativeMapTicks"] = counter!.TickCount.ToString(),
                ["terminalStateSha256"] = terminalState,
                ["terminalState"] = string.Join("\n", terminalRows)
            });
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

    private static ulong SeedGlobalRand(int seed)
    {
        var stateProperty = RequireRandStateProperty();
        var prior = (ulong)stateProperty.GetValue(null, null)!;
        ulong seeded;
        Rand.PushState(seed);
        try { seeded = (ulong)stateProperty.GetValue(null, null)!; }
        finally { Rand.PopState(); }
        stateProperty.SetValue(null, seeded, null);
        return prior;
    }

    private static void RestoreGlobalRand(ulong state) =>
        RequireRandStateProperty().SetValue(null, state, null);

    private static void NormalizeHuman(Pawn pawn, int index)
    {
        pawn.ageTracker.AgeBiologicalTicks = 30L * GenDate.TicksPerYear;
        pawn.ageTracker.AgeChronologicalTicks = 30L * GenDate.TicksPerYear;
        pawn.gender = index % 2 == 0 ? Gender.Female : Gender.Male;
        pawn.story.Childhood = FixedBackstory(BackstorySlot.Childhood);
        pawn.story.Adulthood = FixedBackstory(BackstorySlot.Adulthood);
        pawn.story.traits.allTraits.Clear();
        foreach (var hediff in pawn.health.hediffSet.hediffs.ToArray())
            pawn.health.RemoveHediff(hediff);
        pawn.Notify_DisabledWorkTypesChanged();
        pawn.workSettings?.Notify_DisabledWorkTypesChanged();
        foreach (var skill in pawn.skills.skills)
        {
            skill.Level = 8;
            skill.passion = Passion.None;
            skill.xpSinceLastLevel = 0f;
            skill.xpSinceMidnight = 0f;
        }
        foreach (var apparel in pawn.apparel?.WornApparel.ToArray() ?? Array.Empty<Apparel>())
            apparel.Destroy(DestroyMode.Vanish);
        foreach (var equipment in pawn.equipment?.AllEquipmentListForReading.ToArray() ?? Array.Empty<ThingWithComps>())
            equipment.Destroy(DestroyMode.Vanish);
        pawn.inventory?.innerContainer.ClearAndDestroyContents();
    }

    private static void NormalizeAnimal(Pawn pawn, int index)
    {
        pawn.ageTracker.AgeBiologicalTicks = 5L * GenDate.TicksPerYear;
        pawn.ageTracker.AgeChronologicalTicks = 5L * GenDate.TicksPerYear;
        pawn.gender = index % 2 == 0 ? Gender.Female : Gender.Male;
        foreach (var hediff in pawn.health.hediffSet.hediffs.ToArray())
            pawn.health.RemoveHediff(hediff);
        pawn.inventory?.innerContainer.ClearAndDestroyContents();
    }

    private static BackstoryDef FixedBackstory(BackstorySlot slot) =>
        DefDatabase<BackstoryDef>.AllDefsListForReading
            .Where(backstory => backstory.slot == slot &&
                                backstory.workDisables == WorkTags.None &&
                                backstory.requiredWorkTags == WorkTags.None)
            .OrderBy(backstory => backstory.defName, StringComparer.Ordinal)
            .FirstOrDefault() ?? throw new InvalidOperationException(
                "Core exposes no deterministic unrestricted " + slot + " backstory.");

    private static IReadOnlyDictionary<FieldInfo, int> SeedUniqueIds()
    {
        const int deterministicBase = 1_500_000;
        var manager = Find.UniqueIDsManager ??
            throw new InvalidOperationException("The neutral fixture requires RimWorld's unique-ID manager.");
        var fields = manager.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(field => field.FieldType == typeof(int) && field.Name.StartsWith("next", StringComparison.Ordinal))
            .OrderBy(field => field.Name, StringComparer.Ordinal)
            .ToArray();
        if (fields.Length == 0 || fields.All(field => field.Name != "nextThingID"))
            throw new MissingMemberException(manager.GetType().FullName, "nextThingID");
        var prior = fields.ToDictionary(field => field, field => (int)field.GetValue(manager)!);
        if (prior.Values.Any(value => value >= deterministicBase))
            throw new InvalidOperationException(
                "The disposable performance process has already consumed the reserved deterministic ID range.");
        for (var index = 0; index < fields.Length; index++)
            fields[index].SetValue(manager, deterministicBase + index * 10_000);
        return prior;
    }

    private static void RestoreUniqueIdsWithoutReuse(IReadOnlyDictionary<FieldInfo, int> prior)
    {
        var manager = Find.UniqueIDsManager;
        if (manager is null) return;
        foreach (var entry in prior)
        {
            var current = (int)entry.Key.GetValue(manager)!;
            entry.Key.SetValue(manager, Math.Max(entry.Value, current));
        }
    }

    private static string ResetSampleUniqueIds() =>
        ResetIndexedUniqueIds(1_700_000, "sample");

    private static string ResetIndexedUniqueIds(int targetBase, string phase)
    {
        var manager = Find.UniqueIDsManager ??
            throw new InvalidOperationException("The neutral fixture requires RimWorld's unique-ID manager.");
        var fields = manager.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(field => field.FieldType == typeof(int) && field.Name.StartsWith("next", StringComparison.Ordinal))
            .OrderBy(field => field.Name, StringComparer.Ordinal)
            .ToArray();
        if (fields.Length == 0 || fields.All(field => field.Name != "nextThingID"))
            throw new MissingMemberException(manager.GetType().FullName, "nextThingID");
        var currentValues = fields.Select(field => (int)field.GetValue(manager)!).ToArray();
        var exhaustedIndex = PerformanceDeterminismGuard.FirstCounterAtOrAboveIndexedTarget(
            currentValues, targetBase, 10_000);
        if (exhaustedIndex >= 0)
            throw new InvalidOperationException(
                "Neutral warmup consumed the reserved deterministic " + phase + " ID range for " +
                fields[exhaustedIndex].Name + ".");
        for (var index = 0; index < fields.Length; index++)
            fields[index].SetValue(manager, targetBase + index * 10_000);
        return string.Join(",", fields.Select(field => field.Name + "=" + field.GetValue(manager)));
    }

    private void StartDeterministicWaitJobs(IEnumerable<Pawn> pawns)
    {
        sampleWaitJobs.Clear();
        foreach (var pawn in pawns)
        {
            var wait = JobMaker.MakeJob(JobDefOf.Wait_MaintainPosture);
            wait.playerForced = true;
            wait.expiryInterval = int.MaxValue;
            wait.checkOverrideOnExpire = false;
            pawn.jobs.StartJob(
                wait,
                JobCondition.InterruptForced,
                null,
                resumeCurJobAfterwards: false,
                cancelBusyStances: true,
                null,
                JobTag.Misc);
            if (pawn.CurJobDef != JobDefOf.Wait_MaintainPosture)
                throw new InvalidOperationException("A neutral fixture pawn rejected its deterministic wait job.");
            sampleWaitJobs.Add(pawn, wait);
        }
    }

    private static string CurrentUniqueIdState()
    {
        var manager = Find.UniqueIDsManager ??
            throw new InvalidOperationException("The neutral fixture requires RimWorld's unique-ID manager.");
        return string.Join(",", manager.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(field => field.FieldType == typeof(int) && field.Name.StartsWith("next", StringComparison.Ordinal))
            .OrderBy(field => field.Name, StringComparer.Ordinal)
            .Select(field => field.Name + "=" + field.GetValue(manager)));
    }

    private static PropertyInfo RequireRandStateProperty()
    {
        var property = typeof(Rand).GetProperty(
            "StateCompressed", BindingFlags.Static | BindingFlags.NonPublic);
        if (property is null || property.PropertyType != typeof(ulong) ||
            property.GetMethod is null || property.SetMethod is null)
            throw new MissingMemberException(typeof(Rand).FullName, "StateCompressed");
        return property;
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
        pawnStartCells.Clear();
        foodStartCells.Clear();
        sampleWaitJobs.Clear();
        initialFingerprint = string.Empty;
        counter = null;
        map = null;
    }

    private static string Fingerprint(IEnumerable<Pawn> pawns)
    {
        var rows = pawns.Select((pawn, index) => string.Join("|", new[]
        {
            index.ToString(),
            pawn.kindDef.defName,
            pawn.gender.ToString(),
            pawn.ageTracker.AgeBiologicalTicks.ToString(),
            string.Join(",", pawn.skills?.skills.OrderBy(skill => skill.def.defName)
                .Select(skill => skill.def.defName + ":" + skill.Level + ":" + skill.passion) ??
                Enumerable.Empty<string>()),
            string.Join(",", pawn.story?.traits.allTraits.OrderBy(trait => trait.def.defName)
                .ThenBy(trait => trait.Degree)
                .Select(trait => trait.def.defName + ":" + trait.Degree) ?? Enumerable.Empty<string>())
        }));
        using var sha = SHA256.Create();
        return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", rows)))
            .Select(value => value.ToString("x2")));
    }

    private void AssertSpatialRegistration(IEnumerable<Thing> things)
    {
        foreach (var thing in things)
            if (!thing.Spawned || thing.Map != map ||
                !map!.thingGrid.ThingsListAtFast(thing.Position).Contains(thing))
                throw new InvalidOperationException(
                    "Neutral sample-start spatial registration drifted for " + thing.ThingID + ".");
    }

    private static string[] WorkloadState(
        IEnumerable<Pawn> pawns,
        IEnumerable<Thing> foods)
    {
        return pawns.OrderBy(pawn => pawn.Position.z).ThenBy(pawn => pawn.Position.x)
            .Select((pawn, index) =>
            "pawn|" + index + "|" + pawn.kindDef.defName + "|" + pawn.Position + "|" +
            pawn.needs.food.CurLevelPercentage.ToString("R") + "|" +
            (pawn.needs.rest?.CurLevelPercentage.ToString("R") ?? "none") + "|" +
            (pawn.CurJobDef?.defName ?? "none") + "|" +
            (pawn.inventory?.innerContainer.Count ?? 0) + "|" +
            (pawn.RaceProps.Humanlike && pawn.drafter.Drafted))
            .Concat(foods.OrderBy(food => food.Position.z).ThenBy(food => food.Position.x)
                .Select((food, index) =>
                "food|" + index + "|" + food.def.defName + "|" + food.Position + "|" + food.stackCount))
            .ToArray();
    }
}

public sealed class NeutralColonyTickComponent : MapComponent
{
    public NeutralColonyTickComponent(Map map) : base(map) { }
    public long TickCount { get; private set; }
    public override void MapComponentTick() => TickCount++;
}
