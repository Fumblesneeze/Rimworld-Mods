using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using ImmersiveChefs;
using RimWorld;
using RimWorld.Planet;
using RimWorldDevGateway.EndToEndTesting;
using RimWorldDevGateway.PerformanceTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.PerformanceTests;

internal static class ColonyContract
{
    public const string Owner = "fumblesneeze.immersivechefs";
    public const string Comparison = "immersive-chefs.base-colony";
    public const string Workload = "immersive-chefs-colony/v1";
    public const int Seed = 481516;
    public const int WarmUpTicks = 300;
    public const int SampleTicks = 12_000;
    public const int HumanCount = 36;
    public const int MapHumanCount = 32;
    public const int AnimalCount = 24;
    public const int MapAnimalCount = 20;
    public const int CaravanCount = 2;
}

[RimWorldPerformanceTest(
    "immersive-chefs.base-colony.instrumented",
    ColonyContract.Owner,
    ColonyContract.Owner,
    "brrainz.harmony",
    "ludeon.rimworld",
    PerformanceTestContract.CircinusPackageId,
    ColonyContract.Owner,
    DeterministicSeed = ColonyContract.Seed,
    WorkloadVersion = ColonyContract.Workload,
    ComparisonId = ColonyContract.Comparison,
    WarmUpTicks = ColonyContract.WarmUpTicks,
    SampleTicks = ColonyContract.SampleTicks,
    Repetitions = 3,
    EvidenceLens = PerformanceEvidenceLens.ProductInstrumented)]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.HarmonyOwner, ColonyContract.Owner, "product-harmony")]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.Method, "ImmersiveChefs.WorkGiver_DoDishes::JobOnThing", "dish-search")]
[PerformanceThroughputCheckpoint("native-map-ticks", 11_900)]
[PerformanceThroughputCheckpoint("observer-scans", 48)]
[PerformanceThroughputCheckpoint("meals-produced", 1)]
[PerformanceThroughputCheckpoint("simple-meals-produced", 1)]
[PerformanceThroughputCheckpoint("fine-meals-produced", 1)]
[PerformanceThroughputCheckpoint("lavish-meals-produced", 1)]
[PerformanceThroughputCheckpoint("assisted-cooking-sessions", 1)]
[PerformanceThroughputCheckpoint("ware-cleaned", 1)]
[PerformanceThroughputCheckpoint("domestic-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("industrial-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("map-meals-ingested", 1)]
[PerformanceThroughputCheckpoint("animal-meals-ingested", 4)]
[PerformanceThroughputCheckpoint("patients-fed", 1)]
[PerformanceThroughputCheckpoint("caravan-meals-ingested", 2)]
[PerformanceThroughputCheckpoint("microwave-reheats", 1)]
public sealed class BaseColonyInstrumentedBenchmark : ImmersiveChefsColonyBenchmark { }

[RimWorldPerformanceTest(
    "immersive-chefs.base-colony.armed-disabled",
    ColonyContract.Owner,
    ColonyContract.Owner,
    "brrainz.harmony",
    "ludeon.rimworld",
    PerformanceTestContract.CircinusPackageId,
    ColonyContract.Owner,
    DeterministicSeed = ColonyContract.Seed,
    WorkloadVersion = ColonyContract.Workload,
    ComparisonId = ColonyContract.Comparison,
    WarmUpTicks = ColonyContract.WarmUpTicks,
    SampleTicks = ColonyContract.SampleTicks,
    Repetitions = 3,
    EvidenceLens = PerformanceEvidenceLens.ArmedDisabledWrapper)]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.HarmonyOwner, ColonyContract.Owner, "product-harmony")]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.Method, "ImmersiveChefs.WorkGiver_DoDishes::JobOnThing", "dish-search")]
[PerformanceThroughputCheckpoint("native-map-ticks", 11_900)]
[PerformanceThroughputCheckpoint("observer-scans", 48)]
[PerformanceThroughputCheckpoint("meals-produced", 1)]
[PerformanceThroughputCheckpoint("simple-meals-produced", 1)]
[PerformanceThroughputCheckpoint("fine-meals-produced", 1)]
[PerformanceThroughputCheckpoint("lavish-meals-produced", 1)]
[PerformanceThroughputCheckpoint("assisted-cooking-sessions", 1)]
[PerformanceThroughputCheckpoint("ware-cleaned", 1)]
[PerformanceThroughputCheckpoint("domestic-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("industrial-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("map-meals-ingested", 1)]
[PerformanceThroughputCheckpoint("animal-meals-ingested", 4)]
[PerformanceThroughputCheckpoint("patients-fed", 1)]
[PerformanceThroughputCheckpoint("caravan-meals-ingested", 2)]
[PerformanceThroughputCheckpoint("microwave-reheats", 1)]
public sealed class BaseColonyArmedDisabledBenchmark : ImmersiveChefsColonyBenchmark { }

[RimWorldPerformanceTest(
    "immersive-chefs.base-colony.disarmed",
    ColonyContract.Owner,
    ColonyContract.Owner,
    "brrainz.harmony",
    "ludeon.rimworld",
    PerformanceTestContract.CircinusPackageId,
    ColonyContract.Owner,
    DeterministicSeed = ColonyContract.Seed,
    WorkloadVersion = ColonyContract.Workload,
    ComparisonId = ColonyContract.Comparison,
    WarmUpTicks = ColonyContract.WarmUpTicks,
    SampleTicks = ColonyContract.SampleTicks,
    Repetitions = 3,
    EvidenceLens = PerformanceEvidenceLens.FullyDisarmed)]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.HarmonyOwner, ColonyContract.Owner, "product-harmony")]
[PerformanceMethodSelector(PerformanceMethodSelectorKind.Method, "ImmersiveChefs.WorkGiver_DoDishes::JobOnThing", "dish-search")]
[PerformanceThroughputCheckpoint("native-map-ticks", 11_900)]
[PerformanceThroughputCheckpoint("observer-scans", 48)]
[PerformanceThroughputCheckpoint("meals-produced", 1)]
[PerformanceThroughputCheckpoint("simple-meals-produced", 1)]
[PerformanceThroughputCheckpoint("fine-meals-produced", 1)]
[PerformanceThroughputCheckpoint("lavish-meals-produced", 1)]
[PerformanceThroughputCheckpoint("assisted-cooking-sessions", 1)]
[PerformanceThroughputCheckpoint("ware-cleaned", 1)]
[PerformanceThroughputCheckpoint("domestic-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("industrial-dishwasher-cycles", 1)]
[PerformanceThroughputCheckpoint("map-meals-ingested", 1)]
[PerformanceThroughputCheckpoint("animal-meals-ingested", 4)]
[PerformanceThroughputCheckpoint("patients-fed", 1)]
[PerformanceThroughputCheckpoint("caravan-meals-ingested", 2)]
[PerformanceThroughputCheckpoint("microwave-reheats", 1)]
public sealed class BaseColonyDisarmedBenchmark : ImmersiveChefsColonyBenchmark { }

public abstract class ImmersiveChefsColonyBenchmark :
    IRimWorldPerformanceTest,
    IPerformanceSamplePreparation,
    IPerformanceSampleValidation,
    IPerformanceThroughputCounter
{
    private readonly List<Thing> mapFixtures = new();
    private readonly List<Pawn> humans = new();
    private readonly List<Pawn> animals = new();
    private readonly List<Pawn> workersToActivate = new();
    private readonly List<Pawn> dinersToActivate = new();
    private readonly List<Pawn> patients = new();
    private readonly List<Caravan> caravans = new();
    private readonly List<ThingWithComps> dirtyWare = new();
    private readonly List<ThingWithComps> mapDiningMeals = new();
    private readonly List<ThingWithComps> animalMeals = new();
    private readonly List<ThingWithComps> caravanMeals = new();
    private readonly List<ThingWithComps> poweredBuildings = new();
    private readonly HashSet<IntVec3> nativePowerTransmitterCells = new();
    private readonly List<IntVec3> roomCenters = new();
    private readonly List<BuildingFixtureSpec> buildingFixtures = new();
    private readonly List<(Building_WorkTable Table, RecipeDef Recipe)> cookingTables = new();
    private readonly Dictionary<Pawn, string> pawnRoles = new();
    private readonly Dictionary<Pawn, IntVec3> mapPawnStartCells = new();
    private readonly List<CaravanDiningFixture> caravanDiningFixtures = new();
    private readonly List<AnimalDiningFixture> animalDiningFixtures = new();
    private readonly Dictionary<string, int> initialWareUnits = new(StringComparer.Ordinal);
    private Map? map;
    private ColonyObserver? observer;
    private Building_WorkTable? prepStation;
    private Thing? microwaveSupportTable;
    private ThingWithComps? microwave;
    private ThingWithComps? domesticDishwasher;
    private ThingWithComps? industrialDishwasher;
    private ThingWithComps? dubsWaterTower;
    private float processorDubsInitialWater;
    private bool processorDubsActive;
    private Pawn? microwaveDiner;
    private ThingWithComps? microwaveMeal;
    private string pawnFingerprint = string.Empty;
    private string pawnDemographicsFingerprint = string.Empty;
    private string pawnSkillsFingerprint = string.Empty;
    private string pawnHealthFingerprint = string.Empty;
    private string sampleStartFingerprint = string.Empty;
    private string sampleRandState = string.Empty;
    private string sampleUniqueIdState = string.Empty;
    private ulong priorRandState;
    private bool randSeeded;
    private IReadOnlyDictionary<FieldInfo, int>? priorUniqueIds;
    private string layoutFingerprint = string.Empty;
    private string processorDubsStartFingerprint = string.Empty;
    private bool sampleActivated;

    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap ?? throw new InvalidOperationException(
            "The Immersive Chefs performance fixture requires one playable map.");
        context.DeferCleanup(Cleanup);
        NormalizeDisposableMap(map);
        priorUniqueIds = SeedUniqueIds();
        priorRandState = SeedGlobalRand(ColonyContract.Seed);
        randSeeded = true;
        PreserveSettings(context);

        BuildRooms();
        BuildKitchenAndBills();
        BuildProcessorDubsBranch();
        BuildDiningAndWard();
        CreateMapPopulation();
        CreateCaravans();
        SeedFoodAndWare();
        CaptureInitialWareUnits();
        layoutFingerprint = CurrentLayoutFingerprint();

        observer = new ColonyObserver(
            map,
            mapDiningMeals,
            caravanMeals,
            dirtyWare,
            patients,
            animals.Where(pawn => pawn.Spawned),
            animalMeals,
            map.listerThings.AllThings.Where(IsTrackedMeal),
            humans.Where(pawn => pawn.Spawned),
            domesticDishwasher ?? throw new InvalidOperationException("The domestic dishwasher was not built."),
            industrialDishwasher ?? throw new InvalidOperationException("The industrial dishwasher was not built."));
        map.components.Add(observer);
        ValidateExactTopology();
        Find.CameraDriver.SetRootPosAndSize(map.Center.ToVector3Shifted(), 58f);
        Find.TickManager.Pause();
    }

    public IEnumerator<EndToEndStep> PrepareSample(IEndToEndContext context)
    {
        NormalizeDeterministicSampleStart();
        PrepareProcessorDubsBranch();
        ResetSampleDeterminism();
        ActivateNativeWorkload();
        // Activation constructs native jobs and is allowed to consume Rand. Re-anchor
        // the stochastic state after that deterministic setup so the measured window
        // begins from the exact same random sequence in every fresh process/lens.
        SeedGlobalRand(ColonyContract.Seed + 2);
        sampleActivated = true;
        CaptureDeterministicSampleStart();
        ValidateExactTopology();
        yield return new CheckpointStep(
            "immersive-chefs-colony-manifest",
            _ => ManifestCheckpoint());
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield break;
    }

    private void CaptureDeterministicSampleStart()
    {
        pawnFingerprint = Fingerprint(humans.Concat(animals));
        pawnDemographicsFingerprint = HashRows(humans.Concat(animals).Select((pawn, index) =>
            index + "|" + pawn.kindDef.defName + "|" + pawn.gender + "|" +
            pawn.ageTracker.AgeBiologicalTicks + "|" + pawn.ageTracker.AgeChronologicalTicks));
        pawnSkillsFingerprint = HashRows(humans.Concat(animals).Select((pawn, index) =>
            index + "|" + string.Join(",", pawn.skills?.skills.OrderBy(skill => skill.def.defName)
                .Select(skill => skill.def.defName + ":" + skill.Level + ":" + skill.passion) ??
                Enumerable.Empty<string>())));
        pawnHealthFingerprint = HashRows(humans.Concat(animals).Select((pawn, index) =>
            index + "|" + string.Join(",", pawn.health.hediffSet.hediffs
                .OrderBy(hediff => hediff.def.defName)
                .Select(hediff => hediff.def.defName + ":" +
                                  hediff.Severity.ToString("R", CultureInfo.InvariantCulture)))));
        sampleRandState = ((ulong)RequireRandStateProperty().GetValue(null, null)!).ToString();
        sampleUniqueIdState = CurrentUniqueIdState();
        sampleStartFingerprint = HashRows(humans.Concat(animals).Select((pawn, index) =>
        {
            var caravanIndex = caravans.FindIndex(caravan => caravan.PawnsListForReading.Contains(pawn));
            var holder = pawn.Spawned ? "map:" + pawn.Position.x + "," + pawn.Position.z :
                caravanIndex >= 0 ? "caravan:" + caravanIndex : "world";
            var inventory = string.Join(",", pawn.inventory?.innerContainer
                .OrderBy(thing => thing.def.defName, StringComparer.Ordinal)
                .ThenBy(thing => thing.stackCount)
                .Select(thing => thing.def.defName + ":" + thing.stackCount) ??
                Enumerable.Empty<string>());
            var targetThing = pawn.CurJob?.GetTarget(TargetIndex.A).Thing;
            var targetIndex = animalDiningFixtures.FindIndex(fixture =>
                ReferenceEquals(fixture.Meal, targetThing));
            var target = targetIndex >= 0 ? "animal-meal:" + targetIndex :
                targetThing is null ? string.Empty :
                targetThing.def.defName + "@" + targetThing.PositionHeld.x + "," + targetThing.PositionHeld.z;
            var role = pawnRoles.TryGetValue(pawn, out var assignedRole)
                ? assignedRole
                : pawn.RaceProps.Animal ? "animal" : "unknown";
            return index + "|" + role + "|" + pawn.kindDef.defName + "|" + holder + "|" +
                   pawn.needs.food.CurLevelPercentage.ToString("R", CultureInfo.InvariantCulture) + "|" +
                   (pawn.needs.rest?.CurLevelPercentage ?? -1f).ToString("R", CultureInfo.InvariantCulture) + "|" +
                   (pawn.CurJobDef?.defName ?? string.Empty) + "|" + target + "|" + inventory;
        }).Concat(new[]
        {
            "rand=" + sampleRandState,
            "ids=" + sampleUniqueIdState,
            "power=" + string.Join(",", poweredBuildings
                .OrderBy(thing => thing.def.defName, StringComparer.Ordinal)
                .ThenBy(thing => thing.Position.x)
                .ThenBy(thing => thing.Position.z)
                .Select(thing => thing.def.defName + "@" + thing.Position.x + "," + thing.Position.z +
                                 ":" + (thing.GetComp<CompPowerTrader>()?.PowerOn == true ? "on" : "off"))),
            "processorDubs=" + processorDubsStartFingerprint
        }));
    }

    private void NormalizeDeterministicSampleStart()
    {
        foreach (var pawn in humans.Concat(animals))
        {
            foreach (var hediff in pawn.health.hediffSet.hediffs.ToArray())
                pawn.health.RemoveHediff(hediff);
            pawn.ageTracker.AgeBiologicalTicks = (pawn.RaceProps.Humanlike ? 30L : 5L) * GenDate.TicksPerYear;
            pawn.ageTracker.AgeChronologicalTicks = (pawn.RaceProps.Humanlike ? 30L : 5L) * GenDate.TicksPerYear;
            pawn.needs.food.CurLevelPercentage = 0.8f;
            if (pawn.needs.rest is { } normalizedRest) normalizedRest.CurLevelPercentage = 0.95f;
            if (pawn.Spawned)
            {
                pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: false);
                pawn.pather.StopDead();
                var start = mapPawnStartCells[pawn];
                if (pawn.Position != start)
                {
                    pawn.DeSpawn(DestroyMode.Vanish);
                    GenSpawn.Spawn(pawn, start, map!);
                }
                if (!map!.thingGrid.ThingsListAtFast(start).Contains(pawn))
                    throw new InvalidOperationException("Sample-start pawn was not registered at " + start + ".");
            }
        }
        foreach (var patient in patients)
        {
            var anesthetic = HediffMaker.MakeHediff(HediffDefOf.Anesthetic, patient);
            anesthetic.Severity = 0.8f;
            patient.health.AddHediff(anesthetic);
        }
    }

    private void ResetSampleDeterminism()
    {
        SeedGlobalRand(ColonyContract.Seed + 1);
        const int sampleBase = 2_000_000;
        var manager = Find.UniqueIDsManager ??
            throw new InvalidOperationException("The performance fixture requires RimWorld's unique-ID manager.");
        var fields = UniqueIdFields(manager);
        var current = fields.Select(field => (int)field.GetValue(manager)!).ToArray();
        var exhausted = PerformanceDeterminismGuard.FirstCounterAtOrAboveIndexedTarget(
            current, sampleBase, 10_000);
        if (exhausted >= 0)
            throw new InvalidOperationException(
                "Performance warmup consumed the deterministic sample ID range for " +
                fields[exhausted].Name + ".");
        for (var index = 0; index < fields.Length; index++)
            fields[index].SetValue(manager, sampleBase + index * 10_000);
    }

    private static string CurrentUniqueIdState()
    {
        var manager = Find.UniqueIDsManager ??
            throw new InvalidOperationException("The performance fixture requires RimWorld's unique-ID manager.");
        return string.Join(",", UniqueIdFields(manager)
            .Select(field => field.Name + "=" + field.GetValue(manager)));
    }

    private static FieldInfo[] UniqueIdFields(object manager)
    {
        var fields = manager.GetType()
            .GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
            .Where(field => field.FieldType == typeof(int) &&
                            field.Name.StartsWith("next", StringComparison.Ordinal))
            .OrderBy(field => field.Name, StringComparer.Ordinal)
            .ToArray();
        if (fields.Length == 0 || fields.All(field => field.Name != "nextThingID"))
            throw new MissingMemberException(manager.GetType().FullName, "nextThingID");
        return fields;
    }

    public long Read(string id)
    {
        var current = observer ?? throw new InvalidOperationException("The colony observer is not attached.");
        return id switch
        {
            "native-map-ticks" => current.NativeMapTicks,
            "observer-scans" => current.ObservationScans,
            "meals-produced" => current.MealsProduced,
            "simple-meals-produced" => current.SimpleMealsProduced,
            "fine-meals-produced" => current.FineMealsProduced,
            "lavish-meals-produced" => current.LavishMealsProduced,
            "assisted-cooking-sessions" => current.AssistedCookingSessions,
            "ware-cleaned" => current.WareCleaned,
            "domestic-dishwasher-cycles" => current.DomesticDishwasherCycles,
            "industrial-dishwasher-cycles" => current.IndustrialDishwasherCycles,
            "map-meals-ingested" => current.MapMealsIngested,
            "animal-meals-ingested" => current.AnimalMealsIngested,
            "patients-fed" => current.PatientsFed,
            "caravan-meals-ingested" => current.CaravanMealsIngested,
            "microwave-reheats" => current.MicrowaveReheats,
            "processor-dubs-domestic-cycles" => processorDubsActive ? current.DomesticDishwasherCycles : 0,
            "processor-dubs-industrial-cycles" => processorDubsActive ? current.IndustrialDishwasherCycles : 0,
            "processor-dubs-water-milliliters" => ProcessorDubsWaterMilliliters(),
            "optional-group-branches" => ProcessorDubsCompleted(current) ? 1 : 0,
            "immersive-chefs.processor-dubs-branch" => ProcessorDubsCompleted(current) ? 1 : 0,
            "immersive-chefs.guest-service-branch" => 0,
            "immersive-chefs.variety-vnpe-material-dlc-branch" => 0,
            "immersive-chefs.all-supported-branch" => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(id), id, "Unknown colony throughput checkpoint.")
        };
    }

    public void ValidateSample(IEndToEndContext context) => ValidateExactTopology(requireSampleOutcomes: true);

    private void BuildRooms()
    {
        var origin = map!.Center;
        roomCenters.AddRange(new[]
        {
            origin + new IntVec3(-28, 0, -8),
            origin + new IntVec3(0, 0, -8),
            origin + new IntVec3(28, 0, -8),
            origin + new IntVec3(-16, 0, 24),
            origin + new IntVec3(18, 0, 24)
        });
        foreach (var center in roomCenters) BuildRoom(center, 19, 15);
    }

    private void BuildRoom(IntVec3 center, int width, int height)
    {
        var halfX = width / 2;
        var halfZ = height / 2;
        var roof = DefDatabase<RoofDef>.GetNamed("RoofConstructed");
        for (var x = -halfX; x <= halfX; x++)
        for (var z = -halfZ; z <= halfZ; z++)
        {
            var cell = center + new IntVec3(x, 0, z);
            if (!cell.InBounds(map!)) throw new InvalidOperationException("Performance room exceeds map bounds.");
            var perimeter = Math.Abs(x) == halfX || Math.Abs(z) == halfZ;
            if (!perimeter)
            {
                map!.roofGrid.SetRoof(cell, roof);
                continue;
            }
            if (z == -halfZ && x == 0)
            {
                SpawnBuilding("Door", cell, Rot4.North, ThingDefOf.WoodLog);
                continue;
            }
            SpawnBuilding("Wall", cell, Rot4.North, ThingDefOf.Steel);
        }
    }

    private void BuildKitchenAndBills()
    {
        var kitchen = roomCenters[0];
        var stoveCells = new[]
        {
            kitchen + new IntVec3(-5, 0, 2),
            kitchen + new IntVec3(0, 0, 2),
            kitchen + new IntVec3(5, 0, 2)
        };
        var recipes = new[] { "CookMealSimple", "CookMealFine", "CookMealLavish" };
        for (var index = 0; index < stoveCells.Length; index++)
        {
            var stove = (Building_WorkTable)SpawnBuilding("FueledStove", stoveCells[index], Rot4.South);
            stove.TryGetComp<CompRefuelable>()?.Refuel(999f);
            var bill = new Bill_Production(DefDatabase<RecipeDef>.GetNamed(recipes[index]))
            {
                repeatMode = BillRepeatModeDefOf.Forever,
                ingredientSearchRadius = 38f
            };
            // Lavish meals require more nutrition than one prepared-food batch contains.
            // Keep this bill on raw ingredients so vanilla does not repeatedly request a
            // 20-unit ThingCount from a legitimate 10-unit prepared stack.
            if (index == 2)
            {
                bill.ingredientFilter.SetAllow(
                    DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_PreparedFood"),
                    false);
            }
            bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
            stove.BillStack.AddBill(bill);
            cookingTables.Add((stove, bill.recipe));
        }

        prepStation = (Building_WorkTable)SpawnBuilding(
            "ImmersiveChefs_PrepStation", kitchen + new IntVec3(0, 0, -2), Rot4.North);
        var prepBill = new Bill_Production(DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_PrepareIngredients"))
        {
            repeatMode = BillRepeatModeDefOf.Forever,
            ingredientSearchRadius = 38f
        };
        prepBill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        prepStation.BillStack.AddBill(prepBill);

        var supportDefs = new[]
        {
            "ImmersiveChefs_SauceStation",
            "ImmersiveChefs_MeatStation",
            "ImmersiveChefs_VegetableStation",
            "ImmersiveChefs_PastryStation"
        };
        for (var index = 0; index < supportDefs.Length; index++)
            poweredBuildings.Add((ThingWithComps)SpawnBuilding(
                supportDefs[index], kitchen + new IntVec3(-7 + index * 4, 0, 0), Rot4.North));

        domesticDishwasher = (ThingWithComps)SpawnBuilding(
            "ImmersiveChefs_Dishwasher", kitchen + new IntVec3(-7, 0, 5), Rot4.South);
        industrialDishwasher = (ThingWithComps)SpawnBuilding(
            "ImmersiveChefs_IndustrialDishwasher", kitchen + new IntVec3(5, 0, 5), Rot4.South);
        poweredBuildings.Add(domesticDishwasher);
        poweredBuildings.Add(industrialDishwasher);

        for (var index = 0; index < 5; index++)
            SpawnBuilding("VanometricPowerCell", kitchen + new IntVec3(-8 + index * 4, 0, -6), Rot4.North);
        // Direct GenSpawn follows wipe rules. Put the generators down first, then lay
        // conduits beneath ordinary equipment while leaving native power transmitters
        // to own their occupied cells.
        SpawnConduitGrid(kitchen, 10, 8);
    }

    private void BuildProcessorDubsBranch()
    {
        processorDubsActive = HasActivePackage("syrchalis.processor.framework") &&
                              HasActivePackage("dubwise.dubsbadhygiene");
        if (!processorDubsActive)
            return;
        if (domesticDishwasher is null || industrialDishwasher is null)
            throw new InvalidOperationException("The Processor/Dubs branch requires both dishwashers.");

        var kitchen = roomCenters[0];
        dubsWaterTower = (ThingWithComps)SpawnBuilding(
            "WaterTowerS", kitchen + new IntVec3(0, 0, 5), Rot4.North, ThingDefOf.Steel);
        SetDubsStoredWater(dubsWaterTower, 30f);
        var occupied = new HashSet<IntVec3>(domesticDishwasher.OccupiedRect().Cells
            .Concat(industrialDishwasher.OccupiedRect().Cells)
            .Concat(dubsWaterTower.OccupiedRect().Cells));
        var pipeDef = DefDatabase<ThingDef>.GetNamed("sewagePipeHidden");
        var minimumX = Math.Min(domesticDishwasher.OccupiedRect().minX, dubsWaterTower.OccupiedRect().minX);
        var maximumX = Math.Max(industrialDishwasher.OccupiedRect().maxX, dubsWaterTower.OccupiedRect().maxX);
        for (var x = minimumX; x <= maximumX; x++)
        {
            var cell = new IntVec3(x, 0, domesticDishwasher.Position.z);
            if (occupied.Contains(cell)) continue;
            SpawnBuilding(pipeDef.defName, cell, Rot4.North, ThingDefOf.Steel);
        }
    }

    private void PrepareProcessorDubsBranch()
    {
        if (!processorDubsActive)
            return;
        if (domesticDishwasher is null || industrialDishwasher is null || dubsWaterTower is null ||
            !ProcessorFrameworkAdapter.Controls(domesticDishwasher) ||
            !ProcessorFrameworkAdapter.Controls(industrialDishwasher) ||
            !DubsWaterAdapter.HasSuppliedConnection(domesticDishwasher) ||
            !DubsWaterAdapter.HasSuppliedConnection(industrialDishwasher))
            throw new InvalidOperationException(
                "The Processor/Dubs performance branch did not establish two supplied native Processor dishwashers during warm-up.");
        if (ProcessorFrameworkAdapter.HasContents(domesticDishwasher) ||
            ProcessorFrameworkAdapter.HasContents(industrialDishwasher) ||
            ProcessorFrameworkAdapter.ProgressPercent(domesticDishwasher) != 0f ||
            ProcessorFrameworkAdapter.ProgressPercent(industrialDishwasher) != 0f)
            throw new InvalidOperationException(
                "The Processor/Dubs measured sample must begin with both native processors exactly empty and idle.");
        var domesticNetwork = DubsNetwork(domesticDishwasher);
        if (domesticNetwork is null || !ReferenceEquals(domesticNetwork, DubsNetwork(industrialDishwasher)) ||
            !ReferenceEquals(domesticNetwork, DubsNetwork(dubsWaterTower)))
            throw new InvalidOperationException(
                "Both performance dishwashers and their water tower must share one exact Dubs network.");
        processorDubsInitialWater = ReadDubsNetworkWater(domesticDishwasher);
        if (processorDubsInitialWater < 1f)
            throw new InvalidOperationException("The Processor/Dubs performance branch has insufficient supplied water.");
        processorDubsStartFingerprint = HashRows(new[]
        {
            ProcessorDubsApplianceState("domestic", domesticDishwasher),
            ProcessorDubsApplianceState("industrial", industrialDishwasher),
            "network|shared=true|water=" + processorDubsInitialWater.ToString("R", CultureInfo.InvariantCulture)
        }.Concat(dirtyWare
            .OrderBy(ware => ware.def.defName, StringComparer.Ordinal)
            .ThenBy(ware => ware.Position.x)
            .ThenBy(ware => ware.Position.z)
            .Select(ware => "ware|" + ware.def.defName + "|" +
                            ware.Position.x + "," + ware.Position.z + "|spawned=" + ware.Spawned +
                            "|dirty=" + (ware.GetComp<CompSanitation>()?.IsDirty == true))));
    }

    private bool ProcessorDubsCompleted(ColonyObserver current) =>
        processorDubsActive && current.DomesticDishwasherCycles > 0 &&
        current.IndustrialDishwasherCycles > 0 && ProcessorDubsWaterMilliliters() > 0;

    private long ProcessorDubsWaterMilliliters()
    {
        if (!processorDubsActive || domesticDishwasher is null || processorDubsInitialWater <= 0f)
            return 0;
        return Math.Max(0L, (long)Math.Round(
            (processorDubsInitialWater - ReadDubsNetworkWater(domesticDishwasher)) * 1000f));
    }

    private static bool HasActivePackage(string packageId) =>
        LoadedModManager.RunningModsListForReading.Any(mod =>
            string.Equals(mod.PackageIdPlayerFacing, packageId, StringComparison.OrdinalIgnoreCase));

    private static string ProcessorDubsApplianceState(string role, ThingWithComps appliance) =>
        role + "|" + appliance.def.defName + "|" +
        appliance.Position.x + "," + appliance.Position.z + "|controls=" +
        ProcessorFrameworkAdapter.Controls(appliance) + "|contents=" +
        ProcessorFrameworkAdapter.HeldWare(appliance).Count + "|progress=" +
        ProcessorFrameworkAdapter.ProgressPercent(appliance).ToString("R", CultureInfo.InvariantCulture) +
        "|supplied=" + DubsWaterAdapter.HasSuppliedConnection(appliance);

    private static void SetDubsStoredWater(ThingWithComps tower, float value)
    {
        var storage = tower.AllComps.SingleOrDefault(comp =>
            string.Equals(comp.GetType().FullName, "DubsBadHygiene.CompWaterStorage", StringComparison.Ordinal));
        var field = storage?.GetType().GetField("WaterStorage", BindingFlags.Public | BindingFlags.Instance);
        if (field?.FieldType != typeof(float))
            throw new MissingFieldException("DubsBadHygiene.CompWaterStorage", "WaterStorage:Single");
        field.SetValue(storage, value);
    }

    private static object? DubsNetwork(ThingWithComps fixture)
    {
        var pipe = fixture.AllComps.SingleOrDefault(comp =>
            string.Equals(comp.GetType().FullName, "DubsBadHygiene.CompPipe", StringComparison.Ordinal));
        var property = pipe?.GetType().GetProperty("pipeNet", BindingFlags.Public | BindingFlags.Instance);
        if (property is null)
            throw new MissingMemberException("DubsBadHygiene.CompPipe.pipeNet");
        return property.GetValue(pipe);
    }

    private static float ReadDubsNetworkWater(ThingWithComps fixture)
    {
        var network = DubsNetwork(fixture) ??
                      throw new InvalidOperationException("The Dubs fixture is not connected.");
        var property = network.GetType().GetProperty("WaterStorage", BindingFlags.Public | BindingFlags.Instance);
        if (property?.PropertyType != typeof(float))
            throw new MissingMemberException(network.GetType().FullName, "WaterStorage:Single");
        return (float)property.GetValue(network)!;
    }

    private void BuildDiningAndWard()
    {
        var dining = roomCenters[1];
        for (var index = 0; index < 6; index++)
        {
            var table = SpawnBuilding("Table2x2c", dining + new IntVec3(-6 + (index % 3) * 6, 0, -2 + (index / 3) * 6), Rot4.North, ThingDefOf.WoodLog);
            _ = table;
        }

        microwaveSupportTable = SpawnBuilding(
            "Table1x2c", dining + new IntVec3(0, 0, -5), Rot4.North, ThingDefOf.Steel);
        var microwaveCell = microwaveSupportTable.OccupiedRect().Cells.OrderBy(cell => cell.z).First();
        microwave = (ThingWithComps)SpawnBuilding(
            "ImmersiveChefs_Microwave", microwaveCell, Rot4.North);
        poweredBuildings.Add(microwave);
        SpawnBuilding("VanometricPowerCell", dining + new IntVec3(-8, 0, -6), Rot4.North);
        SpawnConduitGrid(dining, 9, 7);

        var ward = roomCenters[2];
        for (var index = 0; index < 8; index++)
            SpawnBuilding("Bed", ward + new IntVec3(-7 + (index % 4) * 5, 0, -3 + (index / 4) * 6), Rot4.North, ThingDefOf.Steel);
    }

    private void CreateMapPopulation()
    {
        var kitchen = roomCenters[0];
        var ward = roomCenters[2];
        for (var index = 0; index < ColonyContract.MapHumanCount; index++)
        {
            var role = index < 6 ? "cook" : index < 10 ? "assistant" :
                index < 16 ? "cleaner" : index < 20 ? "nurse" :
                index < 24 ? "patient" : "diner";
            var pawn = CreateCapableColonist(role, index);
            humans.Add(pawn);
            pawnRoles.Add(pawn, role);
            mapFixtures.Add(GenSpawn.Spawn(
                pawn,
                role == "patient"
                    ? ward + new IntVec3(-7 + (index - 20) * 5, 0, 0)
                    : kitchen + new IntVec3(-8 + (index % 6) * 3, 0, -9 - (index / 6) * 2),
                map!));
            mapPawnStartCells.Add(pawn, pawn.Position);
            if (role == "patient")
            {
                patients.Add(pawn);
                var anesthetic = HediffMaker.MakeHediff(HediffDefOf.Anesthetic, pawn);
                anesthetic.Severity = 0.8f;
                pawn.health.AddHediff(anesthetic);
                pawn.needs.food.CurLevelPercentage = 0.9f;
            }
            else
            {
                workersToActivate.Add(pawn);
                pawn.drafter.Drafted = true;
            }
            if (role == "diner") dinersToActivate.Add(pawn);
            ConfigureWork(pawn, role);
        }
        var cooks = pawnRoles.Where(pair => pair.Value == "cook").Select(pair => pair.Key).ToArray();
        for (var index = 0; index < cookingTables.Count; index++)
            cookingTables[index].Table.BillStack.Bills[0].SetPawnRestriction(cooks[index]);
        prepStation!.BillStack.Bills[0].SetPawnRestriction(cooks[3]);

        var animalKind = DefDatabase<PawnKindDef>.GetNamed("Muffalo");
        var pen = roomCenters[4];
        for (var index = 0; index < ColonyContract.MapAnimalCount; index++)
        {
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                animalKind,
                Faction.OfPlayer,
                canGeneratePawnRelations: false,
                fixedBiologicalAge: 5f,
                fixedChronologicalAge: 5f));
            NormalizeAnimal(pawn, index);
            animals.Add(pawn);
            mapFixtures.Add(GenSpawn.Spawn(
                pawn,
                pen + new IntVec3(-7 + (index % 5) * 3, 0, -5 + (index / 5) * 3),
                map!));
            mapPawnStartCells.Add(pawn, pawn.Position);
            pawn.needs.food.CurLevelPercentage = 0.8f;
        }
    }

    private void CreateCaravans()
    {
        var animalKind = DefDatabase<PawnKindDef>.GetNamed("Muffalo");
        for (var caravanIndex = 0; caravanIndex < ColonyContract.CaravanCount; caravanIndex++)
        {
            var members = new List<Pawn>();
            for (var index = 0; index < 2; index++)
            {
                var colonist = CreateCapableColonist("traveler", caravanIndex * 2 + index);
                colonist.needs.food.CurLevelPercentage = 0.9f;
                humans.Add(colonist);
                pawnRoles.Add(colonist, "traveler");
                members.Add(colonist);
                var animal = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                    animalKind,
                    Faction.OfPlayer,
                    canGeneratePawnRelations: false,
                    fixedBiologicalAge: 5f,
                    fixedChronologicalAge: 5f));
                NormalizeAnimal(animal, caravanIndex * 2 + index);
                animal.needs.food.CurLevelPercentage = 0.8f;
                animals.Add(animal);
                members.Add(animal);
            }
            var caravan = CaravanMaker.MakeCaravan(
                members,
                Faction.OfPlayer,
                map!.Tile,
                addToWorldPawnsIfNotAlready: true);
            caravans.Add(caravan);
        }
    }

    private void SeedFoodAndWare()
    {
        var storage = roomCenters[3];
        for (var index = 0; index < 12; index++)
        {
            var rice = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RawRice"));
            rice.stackCount = 75;
            Spawn(rice, storage + new IntVec3(-7 + (index % 6) * 3, 0, -4 + (index / 6) * 5));
        }
        // The mixed lavish recipe has two mandatory ingredient groups. A rice-only
        // stockpile can never start it, regardless of cook capacity.
        for (var index = 0; index < 6; index++)
        {
            var meatDef = DefDatabase<PawnKindDef>.GetNamed("Muffalo").race.race.meatDef ??
                          throw new InvalidOperationException("Core Muffalo exposes no cooking meat Def.");
            var meat = ThingMaker.MakeThing(meatDef);
            meat.stackCount = 50;
            Spawn(meat, storage + new IntVec3(-7 + index * 3, 0, -6));
        }

        for (var index = 0; index < 18; index++)
        {
            SpawnWare("ImmersiveChefs_Cookware", storage + new IntVec3(-7 + (index % 6) * 3, 0, 2 + (index / 6) * 2), false);
        }
        for (var index = 0; index < 48; index++)
        {
            var stuff = index < 12 ? ThingDefOf.Gold : ThingDefOf.Steel;
            SpawnWare(
                "ImmersiveChefs_Plate",
                storage + new IntVec3(-8 + (index % 12), 0, 7 + (index / 12)),
                false,
                stuff);
            SpawnWare(
                "ImmersiveChefs_Cutlery",
                storage + new IntVec3(-8 + (index % 12), 0, 11 + (index / 12)),
                false,
                stuff);
        }
        for (var index = 0; index < 12; index++)
        {
            var dirtyCell = processorDubsActive
                ? index < 6
                    ? domesticDishwasher!.Position + new IntVec3(-2 + index, 0, 2)
                    : industrialDishwasher!.Position + new IntVec3(-2 + (index - 6), 0, 2)
                : roomCenters[0] + new IntVec3(-8 + index, 0, 7);
            dirtyWare.Add(SpawnWare(
                index % 3 == 0 ? "ImmersiveChefs_Cookware" :
                index % 3 == 1 ? "ImmersiveChefs_Plate" : "ImmersiveChefs_Cutlery",
                dirtyCell,
                true));
        }

        for (var index = 0; index < dinersToActivate.Count; index++)
        {
            var meal = MakePlatedMeal(index == 0 ? -5f : 18f);
            mapDiningMeals.Add(meal);
            Spawn(meal, roomCenters[1] + new IntVec3(-8 + (index % 8) * 2, 0, 5 + (index / 8) * 2));
            if (index == 0)
            {
                microwaveDiner = dinersToActivate[index];
                microwaveMeal = meal;
            }
        }

        var pen = roomCenters[4];
        foreach (var pair in animals.Where(pawn => pawn.Spawned).Take(4).Select((pawn, index) => (pawn, index)))
        {
            var meal = MakePlatedMeal(18f);
            var plate = meal.GetComp<CompEmbeddedWare>()?.PeekPlateThing() as ThingWithComps ??
                        throw new InvalidOperationException("The animal meal lost its exact embedded plate.");
            animalMeals.Add(meal);
            animalDiningFixtures.Add(new AnimalDiningFixture(pair.pawn, meal, plate));
            Spawn(meal, pen + new IntVec3(-4 + pair.index * 3, 0, 4));
        }

        foreach (var caravan in caravans)
        {
            var carrier = caravan.PawnsListForReading.First(pawn => pawn.RaceProps.Humanlike);
            var meal = MakePlatedMeal(18f);
            var plate = meal.GetComp<CompEmbeddedWare>()?.PeekPlateThing() as ThingWithComps ??
                        throw new InvalidOperationException("The caravan meal lost its exact embedded plate.");
            caravanMeals.Add(meal);
            if (!carrier.inventory.innerContainer.TryAdd(meal, canMergeWithExistingStacks: false))
                throw new InvalidOperationException("Could not add the exact meal to its performance caravan.");
            var cutlery = MakeWare("ImmersiveChefs_Cutlery", false);
            if (!carrier.inventory.innerContainer.TryAdd(cutlery, canMergeWithExistingStacks: false))
                throw new InvalidOperationException("Could not add cutlery to its performance caravan.");
            caravanDiningFixtures.Add(new CaravanDiningFixture(caravan, meal, plate, cutlery));
            caravan.RecacheInventory();
        }
    }

    private void ActivateNativeWorkload()
    {
        foreach (var pawn in workersToActivate)
        {
            if (pawn.drafter.Drafted) pawn.drafter.Drafted = false;
            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
        }
        foreach (var pawn in dinersToActivate)
            pawn.needs.food.CurLevelPercentage = 0.06f;
        foreach (var patient in patients)
            patient.needs.food.CurLevelPercentage = 0.03f;
        foreach (var animal in animals.Where(pawn => pawn.Spawned))
            animal.needs.food.CurLevelPercentage = 0.20f;
        foreach (var fixture in animalDiningFixtures)
        {
            fixture.Animal.needs.food.CurLevelPercentage = 0.02f;
            var job = JobMaker.MakeJob(JobDefOf.Ingest, fixture.Meal);
            job.count = 1;
            fixture.Animal.jobs.StartJob(
                job,
                JobCondition.InterruptForced,
                resumeCurJobAfterwards: false,
                cancelBusyStances: true);
            if (fixture.Animal.CurJobDef != JobDefOf.Ingest ||
                !ReferenceEquals(fixture.Animal.CurJob?.GetTarget(TargetIndex.A).Thing, fixture.Meal))
                throw new InvalidOperationException(
                    "The native animal dining lane did not start its exact Ingest job.");
        }
        if (microwaveDiner is null || microwaveMeal is null)
            throw new InvalidOperationException("The native microwave dining lane was not arranged.");
        var microwaveJob = JobMaker.MakeJob(JobDefOf.Ingest, microwaveMeal);
        microwaveJob.count = 1;
        microwaveDiner.jobs.StartJob(
            microwaveJob,
            JobCondition.InterruptForced,
            resumeCurJobAfterwards: false,
            cancelBusyStances: true);
        if (microwaveDiner.CurJobDef != JobDefOf.Ingest ||
            !ReferenceEquals(microwaveDiner.CurJob?.GetTarget(TargetIndex.A).Thing, microwaveMeal))
            throw new InvalidOperationException(
                "The native microwave dining lane did not start its exact Ingest job.");
        foreach (var caravan in caravans)
        foreach (var pawn in caravan.PawnsListForReading.Where(pawn => pawn.RaceProps.Humanlike))
            pawn.needs.food.CurLevelPercentage = 0.02f;
        observer?.ResetSampleBaselines();
    }

    private Pawn CreateCapableColonist(string role, int index)
    {
        for (var attempt = 0; attempt < 128; attempt++)
        {
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                Faction.OfPlayer,
                canGeneratePawnRelations: false,
                fixedBiologicalAge: 30f,
                fixedChronologicalAge: 30f));
            if (pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) >= 0.8f &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) >= 0.8f)
            {
                NormalizeHuman(pawn, role, index);
                pawn.Name = new NameTriple("Perf", role + index, "Fixture" + attempt);
                pawn.needs.food.CurLevelPercentage = 0.8f;
                pawn.needs.rest.CurLevelPercentage = 0.95f;
                for (var hour = 0; hour < 24; hour++)
                    pawn.timetable.SetAssignment(hour, TimeAssignmentDefOf.Work);
                if (role == "cook") pawn.skills.GetSkill(SkillDefOf.Cooking).Level = 16;
                if (role == "nurse") pawn.skills.GetSkill(SkillDefOf.Medicine).Level = 14;
                return pawn;
            }
            pawn.Destroy(DestroyMode.Vanish);
        }
        throw new InvalidOperationException("Could not generate a capable " + role + " pawn.");
    }

    private static void NormalizeHuman(Pawn pawn, string role, int index)
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
        if (role == "cook") pawn.skills.GetSkill(SkillDefOf.Cooking).Level = 16;
        if (role == "nurse") pawn.skills.GetSkill(SkillDefOf.Medicine).Level = 14;
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

    private static void ConfigureWork(Pawn pawn, string role)
    {
        foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            if (!pawn.WorkTypeIsDisabled(workType)) pawn.workSettings.SetPriority(workType, 0);
        void Enable(string defName)
        {
            var workType = DefDatabase<WorkTypeDef>.GetNamed(defName);
            if (!pawn.WorkTypeIsDisabled(workType)) pawn.workSettings.SetPriority(workType, 1);
        }
        switch (role)
        {
            case "cook":
            case "assistant": Enable("Cooking"); break;
            case "cleaner": Enable("Cleaning"); Enable("Hauling"); break;
            case "nurse": Enable("Doctor"); Enable("Hauling"); break;
            case "diner": Enable("Hauling"); break;
        }
    }

    private Thing SpawnBuilding(string defName, IntVec3 cell, Rot4 rotation, ThingDef? stuff = null)
    {
        var def = DefDatabase<ThingDef>.GetNamed(defName);
        var thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? stuff ?? ThingDefOf.Steel : null);
        thing.SetFactionDirect(Faction.OfPlayer);
        var spawned = Spawn(thing, cell, rotation);
        buildingFixtures.Add(new BuildingFixtureSpec(
            spawned,
            defName,
            cell,
            rotation,
            spawned.Stuff?.defName ?? string.Empty));
        if (defName != "PowerConduit" && spawned is ThingWithComps transmitter &&
            transmitter.AllComps.OfType<CompPower>().Any(comp => comp.Props.transmitsPower))
        {
            foreach (var occupied in spawned.OccupiedRect().Cells)
                nativePowerTransmitterCells.Add(occupied);
        }
        return spawned;
    }

    private Thing Spawn(Thing thing, IntVec3 cell, Rot4? rotation = null)
    {
        if (!cell.InBounds(map!)) throw new InvalidOperationException("Fixture spawn is outside the map: " + cell);
        var spawned = GenSpawn.Spawn(thing, cell, map!, rotation ?? Rot4.North);
        mapFixtures.Add(spawned);
        return spawned;
    }

    private ThingWithComps SpawnWare(
        string defName,
        IntVec3 cell,
        bool dirty,
        ThingDef? stuff = null)
    {
        var ware = MakeWare(defName, dirty, stuff);
        Spawn(ware, cell);
        return ware;
    }

    private static ThingWithComps MakeWare(string defName, bool dirty, ThingDef? stuff = null)
    {
        var ware = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed(defName), stuff ?? ThingDefOf.Steel);
        ware.GetComp<CompQuality>()?.SetQuality(QualityCategory.Good, ArtGenerationContext.Colony);
        var sanitation = ware.GetComp<CompSanitation>() ??
            throw new InvalidOperationException(defName + " has no sanitation component.");
        if (dirty) sanitation.MarkDirty();
        else sanitation.MarkClean(WashProvenance.Safe);
        return ware;
    }

    private static ThingWithComps MakePlatedMeal(float temperature)
    {
        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        var plate = MakeWare("ImmersiveChefs_Plate", false);
        if (meal.GetComp<CompEmbeddedWare>()?.TryEmbedPlate(plate) != true)
            throw new InvalidOperationException("Could not bind a performance meal plate.");
        meal.GetComp<CompCulinaryState>()?.ReplaceServings(new[]
        {
            new CulinaryServingRecord(
                65,
                temperature,
                ContaminationSources.None,
                0,
                Find.TickManager.TicksGame)
        });
        return meal;
    }

    private void SpawnConduitGrid(IntVec3 center, int radiusX, int radiusZ)
    {
        for (var x = -radiusX; x <= radiusX; x++)
        for (var z = -radiusZ; z <= radiusZ; z++)
        {
            var cell = center + new IntVec3(x, 0, z);
            if (nativePowerTransmitterCells.Contains(cell))
                continue;
            SpawnBuilding("PowerConduit", cell, Rot4.North);
        }
    }

    private void CaptureInitialWareUnits()
    {
        foreach (var defName in new[]
                 {
                     "ImmersiveChefs_Cookware", "ImmersiveChefs_Plate", "ImmersiveChefs_Cutlery"
                 })
        {
            var count = CountWareUnits(defName);
            if (count <= 0)
                throw new InvalidOperationException("The performance fixture seeded no " + defName + ".");
            initialWareUnits.Add(defName, count);
        }
    }

    private int CountWareUnits(string defName)
    {
        if (map is null) return 0;
        var things = new List<Thing>();
        ThingOwnerUtility.GetAllThingsRecursively(
            map,
            ThingRequest.ForGroup(ThingRequestGroup.Everything),
            things,
            allowUnreal: true,
            passCheck: null,
            alsoGetSpawnedThings: true);
        foreach (var caravan in caravans.Where(candidate => !candidate.Destroyed))
            things.AddRange(ThingOwnerUtility.GetAllThingsRecursively(caravan, allowUnreal: true));

        var all = new HashSet<Thing>(things.Where(thing => thing is not null && !thing.Destroyed));
        foreach (var embedded in things.OfType<ThingWithComps>()
                     .Select(thing => thing.GetComp<CompEmbeddedWare>()?.PeekPlateThing())
                     .Where(thing => thing is not null && !thing.Destroyed))
            all.Add(embedded!);
        return all.Where(thing => string.Equals(thing.def.defName, defName, StringComparison.Ordinal))
            .Sum(thing => thing.stackCount);
    }

    private string CurrentLayoutFingerprint() => HashRows(buildingFixtures
        .OrderBy(fixture => fixture.DefName, StringComparer.Ordinal)
        .ThenBy(fixture => fixture.ExpectedCell.x)
        .ThenBy(fixture => fixture.ExpectedCell.z)
        .Select(fixture => fixture.ActualRow()));

    private IReadOnlyDictionary<string, string> ManifestCheckpoint() =>
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["workloadVersion"] = ColonyContract.Workload,
            ["deterministicSeed"] = ColonyContract.Seed.ToString(),
            ["humans"] = humans.Count.ToString(),
            ["animals"] = animals.Count.ToString(),
            ["mapHumans"] = humans.Count(pawn => pawn.Spawned).ToString(),
            ["mapAnimals"] = animals.Count(pawn => pawn.Spawned).ToString(),
            ["caravans"] = caravans.Count.ToString(),
            ["rooms"] = roomCenters.Count.ToString(),
            ["pawnFingerprintSha256"] = pawnFingerprint,
            ["pawnDemographicsSha256"] = pawnDemographicsFingerprint,
            ["pawnSkillsSha256"] = pawnSkillsFingerprint,
            ["pawnHealthSha256"] = pawnHealthFingerprint,
            ["sampleStartStateSha256"] = sampleStartFingerprint,
            ["sampleRandState"] = sampleRandState,
            ["sampleUniqueIdState"] = sampleUniqueIdState,
            ["layoutSha256"] = layoutFingerprint,
            ["cookwareUnits"] = initialWareUnits["ImmersiveChefs_Cookware"].ToString(),
            ["plateUnits"] = initialWareUnits["ImmersiveChefs_Plate"].ToString(),
            ["cutleryUnits"] = initialWareUnits["ImmersiveChefs_Cutlery"].ToString(),
            ["recipeTiers"] = "CookMealSimple,CookMealFine,CookMealLavish",
            ["supportStations"] = "prep,sauce,meat,vegetable,pastry",
            ["roles"] = "6-cooks,4-assistants,6-cleaners,4-nurses,4-patients,8-diners,4-travelers",
            ["dishwashers"] = "domestic,industrial",
            ["optionalBranch"] = processorDubsActive ? "processor-dubs" : "base",
            ["processorDubsInitialWater"] = processorDubsInitialWater.ToString("R", CultureInfo.InvariantCulture),
            ["processorDubsStartSha256"] = processorDubsStartFingerprint,
            ["temperature"] = "countertop-microwave",
            ["care"] = "nurses,patients",
            ["travel"] = "two-caravans"
        };

    private void ValidateExactTopology(bool requireSampleOutcomes = false)
    {
        if (map is null || observer is null && humans.Count == 0) return;
        if (humans.Count != ColonyContract.HumanCount ||
            humans.Count(pawn => pawn.Spawned) != ColonyContract.MapHumanCount)
            throw new InvalidOperationException("The performance workload no longer has exactly 36 humans / 32 map humans.");
        if (animals.Count != ColonyContract.AnimalCount ||
            animals.Count(pawn => pawn.Spawned) != ColonyContract.MapAnimalCount)
            throw new InvalidOperationException("The performance workload no longer has exactly 24 animals / 20 map animals.");
        if (caravans.Count != ColonyContract.CaravanCount || caravans.Any(caravan => caravan.Destroyed))
            throw new InvalidOperationException("The performance workload no longer has exactly two live caravans.");
        if (roomCenters.Select(center => center.GetRoom(map)).Distinct().Count() != roomCenters.Count)
            throw new InvalidOperationException("The performance workload no longer has five distinct rooms.");
        foreach (var fixture in buildingFixtures)
            fixture.AssertUnchanged(map);
        var conduitDef = DefDatabase<ThingDef>.GetNamed("PowerConduit");
        if (nativePowerTransmitterCells.Any(cell =>
                map.thingGrid.ThingsListAtFast(cell).Any(thing => thing.def == conduitDef)))
            throw new InvalidOperationException(
                "A performance power conduit overlaps a native transmitting building footprint.");
        if (!string.Equals(CurrentLayoutFingerprint(), layoutFingerprint, StringComparison.Ordinal))
            throw new InvalidOperationException("The performance workload's exact building layout drifted.");

        var expectedRecipes = new[] { "CookMealSimple", "CookMealFine", "CookMealLavish" };
        if (cookingTables.Count != expectedRecipes.Length)
            throw new InvalidOperationException("The performance workload lost one of its three cooking tables.");
        for (var index = 0; index < expectedRecipes.Length; index++)
        {
            var fixture = cookingTables[index];
            if (fixture.Table.Destroyed || !fixture.Table.Spawned ||
                fixture.Table.BillStack.Bills.Count != 1 ||
                !ReferenceEquals(fixture.Table.BillStack.Bills[0].recipe, fixture.Recipe) ||
                !string.Equals(fixture.Recipe.defName, expectedRecipes[index], StringComparison.Ordinal))
                throw new InvalidOperationException("The performance workload's exact recipe tier drifted: " +
                                                    expectedRecipes[index] + ".");
        }
        if (prepStation is null || prepStation.Destroyed || !prepStation.Spawned ||
            prepStation.BillStack.Bills.Count != 1 ||
            prepStation.BillStack.Bills[0].recipe.defName != "ImmersiveChefs_PrepareIngredients")
            throw new InvalidOperationException("The performance workload's preparation bill drifted.");

        var expectedRoles = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["cook"] = 6, ["assistant"] = 4, ["cleaner"] = 6,
            ["nurse"] = 4, ["patient"] = 4,
            ["diner"] = 8, ["traveler"] = 4
        };
        foreach (var role in expectedRoles)
            if (pawnRoles.Count(pair => string.Equals(pair.Value, role.Key, StringComparison.Ordinal)) != role.Value)
                throw new InvalidOperationException("The performance workload's exact pawn role count drifted: " + role.Key + ".");
        var cookingWork = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        if (pawnRoles.Where(pair => pair.Value is "cook" or "assistant")
                .Any(pair => pair.Key.workSettings.GetPriority(cookingWork) <= 0) ||
            pawnRoles.Where(pair => pair.Value == "nurse")
                .Any(pair => pair.Key.workSettings.GetPriority(WorkTypeDefOf.Doctor) <= 0) ||
            patients.Any(patient => patient.Destroyed || !patient.Spawned ||
                                    !patient.health.hediffSet.HasHediff(HediffDefOf.Anesthetic)))
            throw new InvalidOperationException("The performance workload's cooking/nursing role state drifted.");

        if (map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("Table2x2c")).Count != 6 ||
            microwaveSupportTable is null || microwave is null ||
            microwaveSupportTable.Destroyed || microwave.Destroyed ||
            !microwaveSupportTable.OccupiedRect().Contains(microwave.Position))
            throw new InvalidOperationException("The performance dining tables or countertop microwave relation drifted.");
        // PowerOn is established by RimWorld's ordinary power-net tick during the
        // declared warm-up. The arrange phase deliberately does not synthesize a
        // game tick merely to force that terminal state.
        if (sampleActivated && poweredBuildings.Any(thing => thing.Destroyed || !thing.Spawned ||
                                                             thing.GetComp<CompPowerTrader>() is not { PowerOn: true }))
            throw new InvalidOperationException("The performance workload lost powered kitchen equipment.");

        foreach (var expected in initialWareUnits)
        {
            var actual = CountWareUnits(expected.Key);
            if (actual != expected.Value)
                throw new InvalidOperationException(
                    $"The performance workload did not conserve {expected.Key} units: expected {expected.Value}, observed {actual}.");
        }

        if (caravans.Any(caravan => caravan.PawnsListForReading.Count(pawn => pawn.RaceProps.Humanlike) != 2 ||
                                    caravan.PawnsListForReading.Count(pawn => pawn.RaceProps.Animal) != 2))
            throw new InvalidOperationException("The performance workload's exact caravan membership drifted.");
        if (sampleActivated)
        {
            foreach (var fixture in caravanDiningFixtures.Where(fixture => fixture.Meal.Destroyed))
                fixture.AssertReturned();
            foreach (var fixture in animalDiningFixtures.Where(fixture => fixture.Meal.Destroyed))
                fixture.AssertReturned();
            if (requireSampleOutcomes && processorDubsActive &&
                (observer is null || !ProcessorDubsCompleted(observer)))
                throw new InvalidOperationException(
                    "The measured Processor/Dubs branch did not complete both exact dishwasher cycles with water consumption.");
        }
    }

    private void Cleanup()
    {
        if (map is not null && observer is not null) map.components.Remove(observer);
        foreach (var caravan in caravans)
            if (!caravan.Destroyed) caravan.Destroy();
        if (map is not null)
            foreach (var thing in map.listerThings.AllThings.ToArray())
            {
                if (!thing.Spawned) continue;
                if (thing.def.destroyable) thing.Destroy(DestroyMode.Vanish);
                else thing.DeSpawn(DestroyMode.Vanish);
            }
        for (var index = mapFixtures.Count - 1; index >= 0; index--)
            if (!mapFixtures[index].Destroyed) mapFixtures[index].Destroy(DestroyMode.Vanish);
        foreach (var pawn in humans.Concat(animals))
            if (!pawn.Destroyed) pawn.Destroy(DestroyMode.Vanish);
        RestoreUniqueIdsWithoutReuse(priorUniqueIds);
        priorUniqueIds = null;
        if (randSeeded)
        {
            RestoreGlobalRand(priorRandState);
            randSeeded = false;
        }
        mapFixtures.Clear(); humans.Clear(); animals.Clear(); workersToActivate.Clear();
        dinersToActivate.Clear(); patients.Clear(); caravans.Clear(); dirtyWare.Clear();
        mapDiningMeals.Clear(); animalMeals.Clear(); caravanMeals.Clear();
        poweredBuildings.Clear(); roomCenters.Clear();
        nativePowerTransmitterCells.Clear();
        buildingFixtures.Clear(); cookingTables.Clear(); pawnRoles.Clear();
        mapPawnStartCells.Clear();
        caravanDiningFixtures.Clear(); animalDiningFixtures.Clear(); initialWareUnits.Clear();
        prepStation = null; microwaveSupportTable = null; microwave = null;
        domesticDishwasher = null; industrialDishwasher = null;
        dubsWaterTower = null; processorDubsInitialWater = 0f; processorDubsActive = false;
        microwaveDiner = null; microwaveMeal = null;
        observer = null; map = null; pawnFingerprint = string.Empty;
        pawnDemographicsFingerprint = string.Empty;
        pawnSkillsFingerprint = string.Empty;
        pawnHealthFingerprint = string.Empty;
        sampleStartFingerprint = string.Empty;
        sampleRandState = string.Empty;
        sampleUniqueIdState = string.Empty;
        processorDubsStartFingerprint = string.Empty;
        layoutFingerprint = string.Empty;
        sampleActivated = false;
    }

    private static bool IsTrackedMeal(Thing thing) =>
        thing is ThingWithComps && thing.def is { ingestible: not null } &&
        thing.def.ingestible.preferability >= FoodPreferability.MealSimple;

    private static void PreserveSettings(IEndToEndContext context)
    {
        var settings = ImmersiveChefsMod.Settings;
        var mode = settings.WareRequirementMode;
        var fallback = settings.DirtyWareFallback;
        var assistants = settings.AutoCallAssistants;
        var maximumAssistants = settings.MaximumAssistants;
        var dishwashers = settings.PreferDishwashers;
        var temperature = settings.MealTemperatureEnabled;
        var processorFramework = settings.ProcessorFramework;
        var dubsBadHygiene = settings.DubsBadHygiene;
        context.DeferCleanup(() =>
        {
            settings.WareRequirementMode = mode;
            settings.DirtyWareFallback = fallback;
            settings.AutoCallAssistants = assistants;
            settings.MaximumAssistants = maximumAssistants;
            settings.PreferDishwashers = dishwashers;
            settings.MealTemperatureEnabled = temperature;
            settings.ProcessorFramework = processorFramework;
            settings.DubsBadHygiene = dubsBadHygiene;
        });
        settings.WareRequirementMode = WareRequirementMode.Strict;
        settings.DirtyWareFallback = DirtyWareFallback.UrgentOnly;
        settings.AutoCallAssistants = true;
        settings.MaximumAssistants = 4;
        settings.PreferDishwashers = true;
        settings.MealTemperatureEnabled = true;
        settings.ProcessorFramework = OptionalIntegrationMode.Auto;
        settings.DubsBadHygiene = OptionalIntegrationMode.Auto;
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
            var local = GenLocalDate.DayOfYear(target) * GenDate.TicksPerDay + GenLocalDate.DayTick(target);
            Find.TickManager.gameStartAbsTick += 15_000 - local;
        }
        if (GenLocalDate.DayOfYear(target) != 0 || GenLocalDate.HourInteger(target) != 6)
            throw new InvalidOperationException("Failed to normalize the colony's local date and time.");
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
        var property = RequireRandStateProperty();
        var prior = (ulong)property.GetValue(null, null)!;
        ulong seeded;
        Rand.PushState(seed);
        try { seeded = (ulong)property.GetValue(null, null)!; }
        finally { Rand.PopState(); }
        property.SetValue(null, seeded, null);
        return prior;
    }

    private static void RestoreGlobalRand(ulong state) =>
        RequireRandStateProperty().SetValue(null, state, null);

    private static IReadOnlyDictionary<FieldInfo, int> SeedUniqueIds()
    {
        const int deterministicBase = 1_500_000;
        var manager = Find.UniqueIDsManager ??
            throw new InvalidOperationException("The performance fixture requires RimWorld's unique-ID manager.");
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

    private static void RestoreUniqueIdsWithoutReuse(IReadOnlyDictionary<FieldInfo, int>? prior)
    {
        if (prior is null) return;
        var manager = Find.UniqueIDsManager;
        if (manager is null) return;
        foreach (var entry in prior)
        {
            var current = (int)entry.Key.GetValue(manager)!;
            entry.Key.SetValue(manager, Math.Max(entry.Value, current));
        }
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

    private static string Fingerprint(IEnumerable<Pawn> pawns)
    {
        var rows = pawns.Select((pawn, index) => index + "|" + pawn.kindDef.defName + "|" +
            pawn.gender + "|" + pawn.ageTracker.AgeBiologicalTicks + "|" +
            string.Join(",", pawn.skills?.skills.OrderBy(skill => skill.def.defName)
                .Select(skill => skill.def.defName + ":" + skill.Level + ":" + skill.passion) ??
                Enumerable.Empty<string>()));
        return HashRows(rows);
    }

    private static string HashRows(IEnumerable<string> rows)
    {
        using var sha = SHA256.Create();
        return string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(string.Join("\n", rows)))
            .Select(value => value.ToString("x2")));
    }
}

internal sealed class BuildingFixtureSpec
{
    internal BuildingFixtureSpec(
        Thing thing,
        string defName,
        IntVec3 expectedCell,
        Rot4 expectedRotation,
        string expectedStuff)
    {
        Thing = thing;
        DefName = defName;
        ExpectedCell = expectedCell;
        ExpectedRotation = expectedRotation;
        ExpectedStuff = expectedStuff;
    }

    internal Thing Thing { get; }
    internal string DefName { get; }
    internal IntVec3 ExpectedCell { get; }
    internal Rot4 ExpectedRotation { get; }
    internal string ExpectedStuff { get; }

    internal void AssertUnchanged(Map map)
    {
        if (Thing.Destroyed || !Thing.Spawned || !ReferenceEquals(Thing.Map, map) ||
            Thing.def.defName != DefName || Thing.Position != ExpectedCell ||
            Thing.Rotation != ExpectedRotation ||
            (Thing.Stuff?.defName ?? string.Empty) != ExpectedStuff)
            throw new InvalidOperationException("The exact performance building drifted: " + DefName +
                                                " at " + ExpectedCell + ".");
    }

    internal string ActualRow() => DefName + "|" + Thing.Position.x + "|" + Thing.Position.z + "|" +
                                   Thing.Rotation.AsInt + "|" + (Thing.Stuff?.defName ?? string.Empty);
}

internal sealed class CaravanDiningFixture
{
    internal CaravanDiningFixture(
        Caravan caravan,
        ThingWithComps meal,
        ThingWithComps plate,
        ThingWithComps cutlery)
    {
        Caravan = caravan;
        Meal = meal;
        Plate = plate;
        Cutlery = cutlery;
    }

    internal Caravan Caravan { get; }
    internal ThingWithComps Meal { get; }
    internal ThingWithComps Plate { get; }
    internal ThingWithComps Cutlery { get; }

    internal void AssertReturned()
    {
        var held = ThingOwnerUtility.GetAllThingsRecursively(Caravan, allowUnreal: true);
        if (!Meal.Destroyed || Plate.Destroyed || Cutlery.Destroyed ||
            Plate.stackCount != 1 || Cutlery.stackCount != 1 ||
            !held.Contains(Plate) || !held.Contains(Cutlery) ||
            Plate.GetComp<CompSanitation>() is not { IsDirty: false, WashedInWildWater: true } ||
            Cutlery.GetComp<CompSanitation>() is not { IsDirty: false, WashedInWildWater: true })
            throw new InvalidOperationException(
                "A performance caravan did not retain its exact plate and cutlery once, " +
                "cleaned with wild water after dining.");
    }
}

internal sealed class AnimalDiningFixture
{
    internal AnimalDiningFixture(Pawn animal, ThingWithComps meal, ThingWithComps plate)
    {
        Animal = animal;
        Meal = meal;
        Plate = plate;
    }

    internal Pawn Animal { get; }
    internal ThingWithComps Meal { get; }
    internal ThingWithComps Plate { get; }

    internal void AssertReturned()
    {
        var cutleryDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery");
        var animalCutlery = (Animal.inventory?.innerContainer
                                 .Where(thing => thing.def == cutleryDef)
                                 .Sum(thing => thing.stackCount) ?? 0) +
                            (Animal.carryTracker?.CarriedThing?.def == cutleryDef
                                ? Animal.carryTracker.CarriedThing.stackCount
                                : 0);
        if (!Meal.Destroyed || Plate.Destroyed || !Plate.Spawned || Plate.stackCount != 1 ||
            Plate.GetComp<CompSanitation>() is not { IsDirty: false } || animalCutlery != 0 ||
            Animal.needs.mood is not null)
            throw new InvalidOperationException(
                "A performance animal did not eat its meal by hand and return exactly one clean plate.");
    }
}

public sealed class ColonyObserver : MapComponent
{
    private const int ObservationIntervalTicks = 250;
    private readonly HashSet<string> baselineMealIds;
    private readonly HashSet<string> producedMealIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> producedSimpleMealIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> producedFineMealIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> producedLavishMealIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> cleanedWareIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> ingestedMapMealIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> ingestedCaravanMealIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> fedPatientIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> reheatedMealIds = new(StringComparer.Ordinal);
    private readonly HashSet<string> ingestedAnimalMealIds = new(StringComparer.Ordinal);
    private readonly IReadOnlyList<ThingWithComps> mapMeals;
    private readonly IReadOnlyList<ThingWithComps> caravanMeals;
    private readonly IReadOnlyList<ThingWithComps> dirtyWare;
    private readonly IReadOnlyList<Pawn> patients;
    private readonly IReadOnlyList<AnimalDiningFixture> animalDiningFixtures;
    private readonly IReadOnlyList<Pawn> mapHumans;
    private readonly ThingWithComps domesticDishwasher;
    private readonly ThingWithComps industrialDishwasher;
    private readonly HashSet<string> observedAssistedCookJobs = new(StringComparer.Ordinal);
    private readonly HashSet<string> domesticHeldDirtyWare = new(StringComparer.Ordinal);
    private readonly HashSet<string> industrialHeldDirtyWare = new(StringComparer.Ordinal);
    private readonly HashSet<string> domesticEjectedCleanWare = new(StringComparer.Ordinal);
    private readonly HashSet<string> industrialEjectedCleanWare = new(StringComparer.Ordinal);
    private readonly Dictionary<string, float> patientNutrition;
    private readonly Dictionary<string, float> animalNutrition;

    public ColonyObserver(
        Map map,
        IEnumerable<ThingWithComps> mapMeals,
        IEnumerable<ThingWithComps> caravanMeals,
        IEnumerable<ThingWithComps> dirtyWare,
        IEnumerable<Pawn> patients,
        IEnumerable<Pawn> animals,
        IEnumerable<ThingWithComps> animalMeals,
        IEnumerable<Thing> baselineMeals,
        IEnumerable<Pawn> mapHumans,
        ThingWithComps domesticDishwasher,
        ThingWithComps industrialDishwasher) : base(map)
    {
        this.mapMeals = mapMeals.ToArray();
        this.caravanMeals = caravanMeals.ToArray();
        this.dirtyWare = dirtyWare.ToArray();
        this.patients = patients.ToArray();
        var animalArray = animals.ToArray();
        var animalMealArray = animalMeals.ToArray();
        if (animalArray.Length < animalMealArray.Length)
            throw new InvalidOperationException("The performance observer has fewer animals than animal meals.");
        animalDiningFixtures = animalMealArray.Select((meal, index) =>
        {
            var plate = meal.GetComp<CompEmbeddedWare>()?.PeekPlateThing() as ThingWithComps ??
                        throw new InvalidOperationException("An observed animal meal has no exact plate.");
            return new AnimalDiningFixture(animalArray[index], meal, plate);
        }).ToArray();
        baselineMealIds = baselineMeals.Select(thing => thing.ThingID).ToHashSet(StringComparer.Ordinal);
        this.mapHumans = mapHumans.ToArray();
        this.domesticDishwasher = domesticDishwasher;
        this.industrialDishwasher = industrialDishwasher;
        patientNutrition = this.patients.ToDictionary(
            pawn => pawn.ThingID,
            pawn => pawn.needs.food.CurLevel,
            StringComparer.Ordinal);
        animalNutrition = animalDiningFixtures.ToDictionary(
            fixture => fixture.Animal.ThingID,
            fixture => fixture.Animal.needs.food.CurLevel,
            StringComparer.Ordinal);
    }

    public long NativeMapTicks { get; private set; }
    public long ObservationScans { get; private set; }
    public long MealsProduced => producedMealIds.Count;
    public long SimpleMealsProduced => producedSimpleMealIds.Count;
    public long FineMealsProduced => producedFineMealIds.Count;
    public long LavishMealsProduced => producedLavishMealIds.Count;
    public long AssistedCookingSessions => observedAssistedCookJobs.Count;
    public long WareCleaned => cleanedWareIds.Count;
    public long DomesticDishwasherCycles => domesticEjectedCleanWare.Count > 0 ? 1 : 0;
    public long IndustrialDishwasherCycles => industrialEjectedCleanWare.Count > 0 ? 1 : 0;
    public long MapMealsIngested => ingestedMapMealIds.Count;
    public long PatientsFed => fedPatientIds.Count;
    public long CaravanMealsIngested => ingestedCaravanMealIds.Count;
    public long MicrowaveReheats => reheatedMealIds.Count;
    public long AnimalMealsIngested => ingestedAnimalMealIds.Count;

    public void ResetSampleBaselines()
    {
        foreach (var patient in patients)
            patientNutrition[patient.ThingID] = patient.needs.food.CurLevel;
        foreach (var fixture in animalDiningFixtures)
            animalNutrition[fixture.Animal.ThingID] = fixture.Animal.needs.food.CurLevel;
        domesticHeldDirtyWare.Clear();
        industrialHeldDirtyWare.Clear();
        domesticEjectedCleanWare.Clear();
        industrialEjectedCleanWare.Clear();
    }

    public override void MapComponentTick()
    {
        NativeMapTicks++;
        if (NativeMapTicks % ObservationIntervalTicks != 0) return;
        ObservationScans++;
        foreach (var thing in map.listerThings.AllThings.Where(thing =>
                     thing is ThingWithComps && thing.def.ingestible?.preferability >= FoodPreferability.MealSimple))
            if (!baselineMealIds.Contains(thing.ThingID))
            {
                producedMealIds.Add(thing.ThingID);
                if (thing.def.defName == "MealSimple") producedSimpleMealIds.Add(thing.ThingID);
                else if (thing.def.defName == "MealFine") producedFineMealIds.Add(thing.ThingID);
                else if (thing.def.defName == "MealLavish") producedLavishMealIds.Add(thing.ThingID);
            }
        foreach (var ware in dirtyWare)
            if (!ware.Destroyed && ware.GetComp<CompSanitation>()?.IsDirty == false)
                cleanedWareIds.Add(ware.ThingID);
        foreach (var cook in mapHumans)
            if (!cook.Destroyed &&
                CookingSessionRegistry.TryGetAssistantContribution(cook, out var contribution) &&
                contribution > 0f && cook.CurJob is { } job)
                observedAssistedCookJobs.Add(cook.ThingID + ":" + job.loadID);
        ObserveDishwasherCycle(
            domesticDishwasher,
            domesticHeldDirtyWare,
            domesticEjectedCleanWare);
        ObserveDishwasherCycle(
            industrialDishwasher,
            industrialHeldDirtyWare,
            industrialEjectedCleanWare);
        foreach (var meal in mapMeals)
        {
            if (meal.Destroyed) ingestedMapMealIds.Add(meal.ThingID);
            else if (meal.GetComp<CompCulinaryState>()?.PeekCurrentServing()?.MicrowaveReheatCount > 0)
                reheatedMealIds.Add(meal.ThingID);
        }
        foreach (var meal in caravanMeals)
            if (meal.Destroyed) ingestedCaravanMealIds.Add(meal.ThingID);
        foreach (var fixture in animalDiningFixtures)
            if (fixture.Meal.Destroyed &&
                fixture.Animal.needs.food.CurLevel > animalNutrition[fixture.Animal.ThingID] + 0.05f)
                ingestedAnimalMealIds.Add(fixture.Meal.ThingID);
        foreach (var patient in patients)
            if (!patient.Destroyed && patient.needs.food.CurLevel > patientNutrition[patient.ThingID] + 0.05f)
                fedPatientIds.Add(patient.ThingID);
    }

    private void ObserveDishwasherCycle(
        ThingWithComps dishwasher,
        ISet<string> heldDirty,
        ISet<string> ejectedClean)
    {
        var heldThings = ProcessorFrameworkAdapter.Controls(dishwasher)
            ? ProcessorFrameworkAdapter.HeldWare(dishwasher)
            : dishwasher.GetComp<CompDishwasher>()?.GetDirectlyHeldThings().Cast<Thing>() ??
              throw new InvalidOperationException("A performance dishwasher lost its native holder.");
        var held = heldThings
                       .ToDictionary(thing => thing.ThingID, StringComparer.Ordinal) ??
                   throw new InvalidOperationException("A performance dishwasher lost its native holder.");
        foreach (var thing in held.Values)
            if ((thing as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true)
                heldDirty.Add(thing.ThingID);
        foreach (var thingId in heldDirty)
        {
            var fixture = dirtyWare.FirstOrDefault(ware => ware.ThingID == thingId);
            if (fixture is { Destroyed: false, Spawned: true } &&
                fixture.GetComp<CompSanitation>()?.IsDirty == false &&
                !held.ContainsKey(thingId))
                ejectedClean.Add(thingId);
        }
    }
}
