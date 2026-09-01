using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.crash-landing-uranium-radiation",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "OskarPotocki.VanillaFactionsExpanded.Core",
    "Argon.CoreLib",
    "Argon.ExpandedMaterials.Metals",
    "Katavrik.CrashLanding",
    "fumblesneeze.immersivechefs",
    MaxFrames = 4_800,
    MaxGameTicks = 18_000,
    MaxWallClockSeconds = 210)]
public sealed class CrashLandingUraniumRadiationTest : IRimWorldEndToEndTest
{
    private const string ObserverHarmonyId =
        "fumblesneeze.immersivechefs.e2e.crash-landing-radiation-observer";

    private Pawn diner = null!;
    private ThingWithComps meal = null!;
    private ThingWithComps plate = null!;
    private ThingWithComps cutlery = null!;
    private HediffDef radiationDef = null!;

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
        radiationDef = DefDatabase<HediffDef>.GetNamed(CrashLandingRadiationPolicy.DefName);
        EndToEndAssert.True(
            string.Equals(
                CrashLandingRadiationPolicy.PackageId,
                radiationDef.modContentPack.PackageId,
                StringComparison.OrdinalIgnoreCase),
            "The exact Crash Landing package must own the admitted radiation Hediff.");

        diner = FoodSearchE2EFixture.CreateColonist("Uranium radiation diner");
        FoodSearchE2EFixture.SetHunger(diner, 0.95f);
        GenSpawn.Spawn(diner, center + (IntVec3.West * 3), map);
        diner.drafter.Drafted = true;
        HealthUtility.AdjustSeverity(diner, radiationDef, 0.09f);
        var initialRadiation = diner.health.hediffSet.GetFirstHediffOfDef(radiationDef);
        EndToEndAssert.NotNull(initialRadiation,
            "The Crash Landing fixture must begin with latent radiation sickness.");
        EndToEndAssert.False(initialRadiation.Visible,
            "Crash Landing must hide radiation sickness below its first visible stage.");

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
        plate = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Plate", ThingDefOf.Uranium);
        cutlery = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Cutlery", ThingDefOf.Steel);
        EndToEndAssert.True(meal.GetComp<CompEmbeddedWare>()!.TryEmbedPlate(plate),
            "The Crash Landing fixture must embed the exact uranium plate.");
        GenSpawn.Spawn(cutlery, center, map);
        GenSpawn.Spawn(meal, center + (IntVec3.East * 3), map);

        var severityMethod = AccessTools.Method(
            typeof(HealthUtility),
            nameof(HealthUtility.AdjustSeverity),
            new[] { typeof(Pawn), typeof(HediffDef), typeof(float) });
        EndToEndAssert.NotNull(severityMethod,
            "The exact vanilla health-severity seam must remain available.");
        var observer = new Harmony(ObserverHarmonyId);
        observer.Patch(
            severityMethod,
            prefix: new HarmonyMethod(typeof(RadiationObserver), nameof(RadiationObserver.SeverityPrefix)));
        RadiationObserver.Reset(diner, radiationDef);
        context.DeferCleanup(() =>
        {
            observer.Unpatch(severityMethod, HarmonyPatchType.Prefix, ObserverHarmonyId);
            RadiationObserver.Reset(null, null);
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before Crash Landing uranium dining",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the uranium meal before ingestion",
            new[] { meal.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the Crash Landing uranium dining fixture",
            new[] { diner.ThingID, meal.ThingID, cutlery.ThingID },
            paddingPixels: 190);
        yield return new ScreenshotStep(
            "observe the meal without a radiation diagnostic",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "Crash Landing exposure is absent before ingestion",
            _ => AssertBefore());
        yield return new AssertionStep(
            "activate hunger for native uranium consumption",
            _ => FoodSearchE2EFixture.SetHunger(diner, 0.20f));

        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(diner.ThingID, meal.ThingID);
        var consume = options.Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("consume", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(1, consume.Length,
            "Expected one enabled native Consume option for the Crash Landing case.");
        yield return new FloatMenuActionStep(
            "order native uranium consumption",
            diner.ThingID,
            meal.ThingID,
            consume[0].StableId);
        yield return new TimeControlActionStep(
            "run native uranium ingestion",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "ordinary ingest toils acquire the exact cutlery",
            _ => !meal.Destroyed &&
                 diner.CurJobDef == JobDefOf.Ingest &&
                 !cutlery.Spawned &&
                 ReferenceEquals(cutlery.holdingOwner, diner.inventory?.innerContainer),
            new EndToEndDeadline(600, 2_000, TimeSpan.FromSeconds(35)));
        yield return new SelectionActionStep(
            "select the diner during native uranium ingestion",
            new[] { diner.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "observe native Crash Landing uranium ingestion",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new WaitUntilStep(
            "native ingestion returns ware and applies Crash Landing radiation",
            _ => OutcomeCompleted(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep(
            "pause after Crash Landing uranium ingestion",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "route uranium exposure only to Crash Landing",
            _ => AssertCompleted());
        yield return new SelectionActionStep(
            "select the diner after Crash Landing uranium ingestion",
            new[] { diner.ThingID },
            additive: false);
        yield return new PawnInspectTabActionStep(
            "open native Health for the Crash Landing diner",
            diner.ThingID,
            EndToEndPawnInspectTab.Health);
        yield return new ScreenshotStep(
            "observe visible Crash Landing radiation after dining",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return ThingInfoCardActionStep.Open(
            "open the returned uranium plate info card",
            plate.ThingID);
        yield return new ScreenshotStep(
            "observe returned uranium plate without a radiation diagnostic",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return ThingInfoCardActionStep.Close(
            "close the returned uranium plate info card",
            plate.ThingID);
        yield return new CheckpointStep(
            "Crash Landing radiation provider result",
            _ => new Dictionary<string, string>
            {
                ["radiationDose"] = RadiationObserver.RadiationDoses.Single().ToString("0.000"),
                ["radiationSeverity"] = CurrentRadiation().Severity.ToString("0.000"),
                ["coreToxicDoseCount"] = RadiationObserver.CoreToxicDoses.Count.ToString(),
                ["plate"] = plate.ThingID,
                ["cutlery"] = cutlery.ThingID
            });
    }

    private void AssertBefore()
    {
        EndToEndAssert.True(
            meal.GetInspectString().IndexOf("radiation", StringComparison.OrdinalIgnoreCase) < 0 &&
            meal.GetInspectString().IndexOf("toxic", StringComparison.OrdinalIgnoreCase) < 0,
            "The meal inspect pane must not expose latent unsafe-material health state.");
        EndToEndAssert.Equal(0, RadiationObserver.RadiationDoses.Count,
            "Spawning, embedding, carrying, and inspection must apply no radiation dose.");
        EndToEndAssert.Equal(0, RadiationObserver.CoreToxicDoses.Count,
            "Pre-ingestion setup must apply no Core toxic dose.");
    }

    private bool OutcomeCompleted()
    {
        var radiation = diner.health.hediffSet.GetFirstHediffOfDef(radiationDef);
        return meal.Destroyed &&
               RadiationObserver.RadiationDoses.Count == 1 &&
               plate.Spawned &&
               cutlery.Spawned &&
               radiation is not null &&
               radiation.Visible;
    }

    private void AssertCompleted()
    {
        EndToEndAssert.Equal(1, RadiationObserver.RadiationDoses.Count,
            "Native ingestion must apply Crash Landing radiation exactly once.");
        EndToEndAssert.True(
            Math.Abs(RadiationObserver.RadiationDoses.Single() -
                     ToxicKitchenwareExposurePolicy.PlateDose) < 0.0001f,
            "The uranium plate must contribute exactly its 0.015 severity-equivalent dose.");
        EndToEndAssert.Equal(0, RadiationObserver.CoreToxicDoses.Count,
            "Crash Landing-owned uranium must not also add Core toxic buildup.");
        EndToEndAssert.Equal(1, plate.stackCount,
            "Native ingestion must conserve the exact uranium plate unit.");
        EndToEndAssert.Equal(1, cutlery.stackCount,
            "Native ingestion must conserve the exact cutlery unit.");
        EndToEndAssert.True(plate.GetComp<CompSanitation>()!.IsDirty,
            "The returned uranium plate must follow the ordinary dirty-dish lifecycle.");
        EndToEndAssert.True(cutlery.GetComp<CompSanitation>()!.IsDirty,
            "The returned cutlery must follow the ordinary dirty-dish lifecycle.");
        EndToEndAssert.True(
            plate.GetInspectString().IndexOf("radiation", StringComparison.OrdinalIgnoreCase) < 0 &&
            plate.GetInspectString().IndexOf("toxic", StringComparison.OrdinalIgnoreCase) < 0,
            "Returned ware must not expose latent unsafe-material health state.");
    }

    private Hediff CurrentRadiation() =>
        diner.health.hediffSet.GetFirstHediffOfDef(radiationDef) ??
        throw new InvalidOperationException("Expected Crash Landing radiation sickness after ingestion.");

    private static class RadiationObserver
    {
        private static Pawn? target;
        private static HediffDef? radiation;

        internal static List<float> RadiationDoses { get; } = new();
        internal static List<float> CoreToxicDoses { get; } = new();

        internal static void Reset(Pawn? pawn, HediffDef? radiationDef)
        {
            target = pawn;
            radiation = radiationDef;
            RadiationDoses.Clear();
            CoreToxicDoses.Clear();
        }

        public static void SeverityPrefix(Pawn __0, HediffDef __1, float __2)
        {
            if (!ReferenceEquals(__0, target) || __2 <= 0f)
            {
                return;
            }

            if (ReferenceEquals(__1, HediffDefOf.ToxicBuildup))
            {
                CoreToxicDoses.Add(__2);
            }
            else if (ReferenceEquals(__1, radiation))
            {
                RadiationDoses.Add(__2);
            }
        }
    }
}
