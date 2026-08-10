using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.toxic-uranium-plate-dining",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "fumblesneeze.immersivechefs",
    MaxFrames = 3_600,
    MaxGameTicks = 12_000,
    MaxWallClockSeconds = 150)]
public sealed class ToxicUraniumPlateDiningTest : IRimWorldEndToEndTest
{
    private const string ObserverHarmonyId =
        "fumblesneeze.immersivechefs.e2e.toxic-uranium-plate-observer";

    private Pawn diner = null!;
    private ThingWithComps meal = null!;
    private ThingWithComps plate = null!;
    private ThingWithComps cutlery = null!;
    private Hediff toxicBuildup = null!;
    private string dinerId = string.Empty;
    private string mealId = string.Empty;
    private string plateId = string.Empty;
    private string cutleryId = string.Empty;

    public void Arrange(IEndToEndContext context)
    {
        var priorScale = ImmersiveChefsMod.Settings.ToxicKitchenwareExposureScale;
        var priorTemperature = ImmersiveChefsMod.Settings.MealTemperatureEnabled;
        context.DeferCleanup(() =>
        {
            ImmersiveChefsMod.Settings.ToxicKitchenwareExposureScale = priorScale;
            ImmersiveChefsMod.Settings.MealTemperatureEnabled = priorTemperature;
        });
        ImmersiveChefsMod.Settings.ToxicKitchenwareExposureScale = 1f;
        ImmersiveChefsMod.Settings.MealTemperatureEnabled = false;

        var map = Current.Game.CurrentMap;
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);

        diner = FoodSearchE2EFixture.CreateColonist("Uranium plate diner");
        FoodSearchE2EFixture.SetHunger(diner, 0.95f);
        GenSpawn.Spawn(diner, center + (IntVec3.West * 3), map);
        diner.drafter.Drafted = true;

        HealthUtility.AdjustSeverity(diner, HediffDefOf.ToxicBuildup, 0.039f);
        toxicBuildup = diner.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup);
        EndToEndAssert.NotNull(toxicBuildup,
            "The uranium-plate fixture must begin with vanilla ToxicBuildup.");
        EndToEndAssert.False(toxicBuildup.Visible,
            "Vanilla must hide the initial sub-threshold ToxicBuildup stage.");

        meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
        meal.stackCount = 1;
        meal.GetComp<CompCulinaryState>()!.ReplaceServings(new[]
        {
            new CulinaryServingRecord(
                qualityScore: 55,
                temperatureCelsius: 21f,
                contamination: ContaminationSources.None,
                microwaveReheatCount: 0,
                lastThermalTick: 0,
                cookwareMaterial: KitchenMaterialKind.Steel)
        });
        plate = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Plate",
            ThingDefOf.Uranium);
        cutlery = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cutlery",
            ThingDefOf.Steel);
        EndToEndAssert.True(meal.GetComp<CompEmbeddedWare>()!.TryEmbedPlate(plate),
            "The fixture must embed the exact physical uranium plate.");
        GenSpawn.Spawn(cutlery, center, map);
        GenSpawn.Spawn(meal, center + (IntVec3.East * 3), map);

        dinerId = diner.ThingID;
        mealId = meal.ThingID;
        plateId = plate.ThingID;
        cutleryId = cutlery.ThingID;

        PlateDoseObserver.Reset(diner);
        var target = AccessTools.Method(
            typeof(HealthUtility),
            nameof(HealthUtility.AdjustSeverity),
            new[] { typeof(Pawn), typeof(HediffDef), typeof(float) });
        EndToEndAssert.NotNull(target,
            "The exact vanilla toxic-severity application seam must remain available.");
        var observer = new Harmony(ObserverHarmonyId);
        observer.Patch(
            target,
            prefix: new HarmonyMethod(typeof(PlateDoseObserver), nameof(PlateDoseObserver.Prefix)));
        context.DeferCleanup(() =>
        {
            observer.Unpatch(target, HarmonyPatchType.Prefix, ObserverHarmonyId);
            PlateDoseObserver.Reset(null);
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before uranium-plate dining",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the uranium-plated meal before ingestion",
            new[] { mealId },
            additive: false);
        yield return new CameraActionStep(
            "frame the uranium plate dining fixture",
            new[] { dinerId, mealId, cutleryId },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "observe the uranium-plated meal without a toxicity diagnostic",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "uranium plate exposure remains latent before ingestion",
            _ => AssertBeforeIngestion());
        yield return new AssertionStep(
            "activate hunger only for native uranium-plate consumption",
            _ => FoodSearchE2EFixture.SetHunger(diner, 0.20f));

        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(dinerId, mealId);
        var consume = options.Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("consume", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(1, consume.Length,
            "Expected one enabled native Consume option for the uranium plate.");
        yield return new FloatMenuActionStep(
            "order native consumption from the uranium plate",
            dinerId,
            mealId,
            consume[0].StableId);
        yield return new TimeControlActionStep(
            "run native uranium-plate ingestion",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "ordinary ingest toils acquire the exact non-toxic cutlery",
            _ => !meal.Destroyed &&
                 diner.CurJobDef == JobDefOf.Ingest &&
                 !cutlery.Spawned &&
                 ReferenceEquals(cutlery.holdingOwner, diner.inventory?.innerContainer),
            new EndToEndDeadline(600, 2_000, TimeSpan.FromSeconds(35)));
        yield return new SelectionActionStep(
            "select the diner during uranium-plate ingestion",
            new[] { dinerId },
            additive: false);
        yield return new ScreenshotStep(
            "observe native ingestion from the uranium plate",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new WaitUntilStep(
            "native ingestion returns the uranium plate and applies its one dose",
            _ => OutcomeCompleted(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep(
            "pause after uranium-plate ingestion",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "apply only the exact physical uranium plate dose",
            _ => AssertCompleted());
        yield return new SelectionActionStep(
            "select the diner after uranium-plate ingestion",
            new[] { dinerId },
            additive: false);
        yield return new PawnInspectTabActionStep(
            "open native Health after the uranium plate crosses the threshold",
            dinerId,
            EndToEndPawnInspectTab.Health);
        yield return new ScreenshotStep(
            "observe vanilla Health revealing plate-caused toxic buildup",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return ThingInfoCardActionStep.Open(
            "open the exact returned uranium plate info card",
            plateId);
        yield return new ScreenshotStep(
            "observe returned uranium plate without a toxicity diagnostic",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return ThingInfoCardActionStep.Close(
            "close the exact returned uranium plate info card",
            plateId);
        yield return new CheckpointStep(
            "uranium plate dining result",
            _ => new Dictionary<string, string>
            {
                ["mealDestroyed"] = meal.Destroyed.ToString(),
                ["plate"] = plateId,
                ["cutlery"] = cutleryId,
                ["doseCount"] = PlateDoseObserver.Doses.Count.ToString(),
                ["dose"] = PlateDoseObserver.Doses.Single().ToString("0.000"),
                ["toxicSeverity"] = toxicBuildup.Severity.ToString("0.000"),
                ["toxicVisible"] = toxicBuildup.Visible.ToString()
            });
    }

    private void AssertBeforeIngestion()
    {
        EndToEndAssert.Equal(ThingDefOf.Uranium, plate.Stuff,
            "The embedded plate must actually use Core uranium.");
        EndToEndAssert.Equal(ThingDefOf.Steel, cutlery.Stuff,
            "The loose cutlery must remain non-toxic steel.");
        EndToEndAssert.True(
            meal.GetInspectString().IndexOf("toxic", StringComparison.OrdinalIgnoreCase) < 0,
            "The meal inspect pane must not expose latent uranium-plate toxicity.");
        EndToEndAssert.False(toxicBuildup.Visible,
            "The pawn's pre-threshold vanilla buildup must still be hidden.");
        EndToEndAssert.Equal(0, PlateDoseObserver.Doses.Count,
            "Spawning, embedding, carrying, and inspection must apply no plate dose.");
    }

    private bool OutcomeCompleted()
    {
        if (!meal.Destroyed || PlateDoseObserver.Doses.Count != 1)
        {
            return false;
        }

        toxicBuildup = diner.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup);
        return toxicBuildup is not null && toxicBuildup.Visible &&
               plate.Spawned && cutlery.Spawned;
    }

    private void AssertCompleted()
    {
        EndToEndAssert.Equal(1, PlateDoseObserver.Doses.Count,
            "The native ingestion must apply exactly one toxic-ware dose.");
        EndToEndAssert.True(
            Math.Abs(PlateDoseObserver.Doses[0] - ToxicKitchenwareExposurePolicy.PlateDose) < 0.0001f,
            "The physical uranium plate must contribute exactly 0.015 and no other ware dose.");
        EndToEndAssert.Equal(1, plate.stackCount,
            "Native ingestion must conserve one exact uranium plate unit.");
        EndToEndAssert.Equal(1, cutlery.stackCount,
            "Native ingestion must conserve one exact steel cutlery unit.");
        EndToEndAssert.True(plate.GetComp<CompSanitation>()!.IsDirty,
            "The returned uranium plate must follow the ordinary dirty-dish lifecycle.");
        EndToEndAssert.True(cutlery.GetComp<CompSanitation>()!.IsDirty,
            "The returned steel cutlery must follow the ordinary dirty-dish lifecycle.");
        EndToEndAssert.True(
            plate.GetInspectString().IndexOf("toxic", StringComparison.OrdinalIgnoreCase) < 0,
            "The returned uranium plate must not expose a toxicity diagnostic.");
    }

    private static class PlateDoseObserver
    {
        private static Pawn? target;

        internal static List<float> Doses { get; } = new();

        internal static void Reset(Pawn? pawn)
        {
            target = pawn;
            Doses.Clear();
        }

        public static void Prefix(Pawn __0, HediffDef __1, float __2)
        {
            if (ReferenceEquals(__0, target) &&
                ReferenceEquals(__1, HediffDefOf.ToxicBuildup) &&
                __2 > 0f)
            {
                Doses.Add(__2);
            }
        }
    }
}
