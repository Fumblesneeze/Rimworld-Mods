using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.rimatomics-uranium-radiation",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "OskarPotocki.VanillaFactionsExpanded.Core",
    "Argon.CoreLib",
    "Argon.ExpandedMaterials.Metals",
    "Dubwise.Rimatomics",
    "Katavrik.CrashLanding",
    "fumblesneeze.immersivechefs",
    MaxFrames = 8_400,
    MaxGameTicks = 30_000,
    MaxWallClockSeconds = 330)]
public sealed class RimatomicsUraniumRadiationTest : IRimWorldEndToEndTest
{
    private const string ObserverHarmonyId =
        "fumblesneeze.immersivechefs.e2e.rimatomics-radiation-observer";

    private readonly List<DiningCase> cases = new();
    private HediffDef radiationDef = null!;
    private HediffDef crashLandingRadiationDef = null!;

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
        radiationDef = DefDatabase<HediffDef>.GetNamed("RimatomicsRadiation");
        crashLandingRadiationDef = DefDatabase<HediffDef>.GetNamed(CrashLandingRadiationPolicy.DefName);
        var lead = DefDatabase<ThingDef>.GetNamed("EM_Lead");

        cases.Add(CreateCase(map, center + (IntVec3.North * 2), "Uranium radiation diner", ThingDefOf.Steel));
        cases.Add(CreateCase(map, center + (IntVec3.South * 2), "Mixed exposure diner", lead));

        var radiationMethod = ResolveRadiationMethod();
        var severityMethod = AccessTools.Method(
            typeof(HealthUtility),
            nameof(HealthUtility.AdjustSeverity),
            new[] { typeof(Pawn), typeof(HediffDef), typeof(float) });
        EndToEndAssert.NotNull(severityMethod,
            "The exact vanilla health-severity seam must remain available.");

        var observer = new Harmony(ObserverHarmonyId);
        observer.Patch(
            radiationMethod,
            prefix: new HarmonyMethod(typeof(RadiationObserver), nameof(RadiationObserver.RadiationPrefix)));
        observer.Patch(
            severityMethod,
            prefix: new HarmonyMethod(typeof(RadiationObserver), nameof(RadiationObserver.SeverityPrefix)));
        context.DeferCleanup(() =>
        {
            observer.Unpatch(radiationMethod, HarmonyPatchType.Prefix, ObserverHarmonyId);
            observer.Unpatch(severityMethod, HarmonyPatchType.Prefix, ObserverHarmonyId);
            RadiationObserver.Reset(null, null, null);
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        for (var index = 0; index < cases.Count; index++)
        {
            foreach (var step in ExecuteCase(context, cases[index], index))
            {
                yield return step;
            }
        }
    }

    private IEnumerable<EndToEndStep> ExecuteCase(
        IEndToEndContext context,
        DiningCase fixture,
        int index)
    {
        var label = fixture.ExpectLeadDose ? "mixed lead and uranium" : "uranium-only";
        RadiationObserver.Reset(fixture.Diner, radiationDef, crashLandingRadiationDef);

        yield return new TimeControlActionStep(
            $"pause before {label} dining",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            $"select the {label} meal before ingestion",
            new[] { fixture.Meal.ThingID },
            additive: false);
        yield return new CameraActionStep(
            $"frame the {label} dining fixture",
            new[] { fixture.Diner.ThingID, fixture.Meal.ThingID, fixture.Cutlery.ThingID },
            paddingPixels: 190);
        yield return new ScreenshotStep(
            $"observe the {label} meal without a radiation diagnostic",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            $"{label} exposure is absent before ingestion",
            _ => AssertBefore(fixture));
        yield return new AssertionStep(
            $"activate hunger for {label} native consumption",
            _ => FoodSearchE2EFixture.SetHunger(fixture.Diner, 0.20f));

        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(fixture.Diner.ThingID, fixture.Meal.ThingID);
        var consume = options.Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("consume", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(1, consume.Length,
            $"Expected one enabled native Consume option for the {label} case.");
        yield return new FloatMenuActionStep(
            $"order native {label} consumption",
            fixture.Diner.ThingID,
            fixture.Meal.ThingID,
            consume[0].StableId);
        yield return new TimeControlActionStep(
            $"run native {label} ingestion",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            $"ordinary ingest toils acquire the exact {label} cutlery",
            _ => !fixture.Meal.Destroyed &&
                 fixture.Diner.CurJobDef == JobDefOf.Ingest &&
                 !fixture.Cutlery.Spawned &&
                 ReferenceEquals(fixture.Cutlery.holdingOwner, fixture.Diner.inventory?.innerContainer),
            new EndToEndDeadline(600, 2_000, TimeSpan.FromSeconds(35)));
        yield return new SelectionActionStep(
            $"select the diner during {label} ingestion",
            new[] { fixture.Diner.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            $"observe native {label} ingestion",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new WaitUntilStep(
            $"native {label} ingestion returns ware and applies radiation",
            _ => OutcomeCompleted(fixture),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep(
            $"pause after {label} ingestion",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            $"route {label} exposure to the exact health providers",
            _ => AssertCompleted(fixture));
        yield return new SelectionActionStep(
            $"select the {label} diner after ingestion",
            new[] { fixture.Diner.ThingID },
            additive: false);
        yield return new PawnInspectTabActionStep(
            $"open native Health for the {label} diner",
            fixture.Diner.ThingID,
            EndToEndPawnInspectTab.Health);
        yield return new ScreenshotStep(
            $"observe Rimatomics radiation after {label} dining",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return ThingInfoCardActionStep.Open(
            $"open the returned {label} uranium plate info card",
            fixture.Plate.ThingID);
        yield return new ScreenshotStep(
            $"observe returned {label} plate without a radiation diagnostic",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return ThingInfoCardActionStep.Close(
            $"close the returned {label} uranium plate info card",
            fixture.Plate.ThingID);
        yield return new CheckpointStep(
            $"{label} provider result",
            _ => new Dictionary<string, string>
            {
                ["caseIndex"] = index.ToString(),
                ["radiationStrength"] = RadiationObserver.RadiationStrengths.Single().ToString("0.000000"),
                ["radiationSeverity"] = CurrentRadiation(fixture).Severity.ToString("0.000"),
                ["coreToxicDoseCount"] = RadiationObserver.CoreToxicDoses.Count.ToString(),
                ["coreToxicDose"] = RadiationObserver.CoreToxicDoses.Count == 0
                    ? "0.000"
                    : RadiationObserver.CoreToxicDoses.Single().ToString("0.000"),
                ["crashLandingRadiationDoseCount"] =
                    RadiationObserver.CrashLandingRadiationDoses.Count.ToString(),
                ["plate"] = fixture.Plate.ThingID,
                ["cutlery"] = fixture.Cutlery.ThingID
            });
    }

    private DiningCase CreateCase(Map map, IntVec3 center, string name, ThingDef cutleryStuff)
    {
        var diner = FoodSearchE2EFixture.CreateColonist(name);
        FoodSearchE2EFixture.SetHunger(diner, 0.95f);
        GenSpawn.Spawn(diner, center + (IntVec3.West * 3), map);
        diner.drafter.Drafted = true;
        HealthUtility.AdjustSeverity(diner, radiationDef, 0.039f);
        var initialRadiation = diner.health.hediffSet.GetFirstHediffOfDef(radiationDef);
        EndToEndAssert.NotNull(initialRadiation,
            "The Rimatomics fixture must begin with latent radiation sickness.");
        EndToEndAssert.False(initialRadiation.Visible,
            "Rimatomics must hide radiation sickness below its first visible stage.");

        var meal = (ThingWithComps)ThingMaker.MakeThing(ThingDefOf.MealSimple);
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
        var plate = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Plate", ThingDefOf.Uranium);
        var cutlery = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Cutlery", cutleryStuff);
        EndToEndAssert.True(meal.GetComp<CompEmbeddedWare>()!.TryEmbedPlate(plate),
            "The Rimatomics fixture must embed the exact uranium plate.");
        GenSpawn.Spawn(cutlery, center, map);
        GenSpawn.Spawn(meal, center + (IntVec3.East * 3), map);
        return new DiningCase(diner, meal, plate, cutlery, cutleryStuff.defName == "EM_Lead");
    }

    private void AssertBefore(DiningCase fixture)
    {
        EndToEndAssert.Equal(ThingDefOf.Uranium, fixture.Plate.Stuff,
            "The embedded plate must use Core uranium.");
        EndToEndAssert.True(
            fixture.Meal.GetInspectString().IndexOf("radiation", StringComparison.OrdinalIgnoreCase) < 0 &&
            fixture.Meal.GetInspectString().IndexOf("toxic", StringComparison.OrdinalIgnoreCase) < 0,
            "The meal inspect pane must not expose latent unsafe-material health state.");
        EndToEndAssert.Equal(0, RadiationObserver.RadiationStrengths.Count,
            "Spawning, embedding, carrying, and inspection must apply no uranium exposure.");
        EndToEndAssert.Equal(0, RadiationObserver.CoreToxicDoses.Count,
            "Pre-ingestion setup must apply no Core toxic dose.");
        EndToEndAssert.Equal(0, RadiationObserver.CrashLandingRadiationDoses.Count,
            "Pre-ingestion setup must apply no Crash Landing radiation dose.");
    }

    private bool OutcomeCompleted(DiningCase fixture)
    {
        if (!fixture.Meal.Destroyed || RadiationObserver.RadiationStrengths.Count != 1 ||
            !fixture.Plate.Spawned || !fixture.Cutlery.Spawned)
        {
            return false;
        }

        var radiation = fixture.Diner.health.hediffSet.GetFirstHediffOfDef(radiationDef);
        return radiation is not null && radiation.Visible;
    }

    private void AssertCompleted(DiningCase fixture)
    {
        EndToEndAssert.Equal(1, RadiationObserver.RadiationStrengths.Count,
            "Native ingestion must invoke Rimatomics radiation exactly once.");
        EndToEndAssert.True(
            Math.Abs(
                RadiationObserver.RadiationStrengths.Single() -
                RimatomicsRadiationPolicy.StrengthForSeverity(ToxicKitchenwareExposurePolicy.PlateDose)) < 0.00001f,
            "The exact uranium plate must use the calibrated 0.015 severity-equivalent strength.");
        EndToEndAssert.Equal(fixture.ExpectLeadDose ? 1 : 0, RadiationObserver.CoreToxicDoses.Count,
            "Only an actually used lead component may add Core toxic buildup beside Rimatomics.");
        EndToEndAssert.Equal(0, RadiationObserver.CrashLandingRadiationDoses.Count,
            "Rimatomics must own uranium once when both radiation providers are active.");
        if (fixture.ExpectLeadDose)
        {
            EndToEndAssert.True(
                Math.Abs(RadiationObserver.CoreToxicDoses.Single() - ToxicKitchenwareExposurePolicy.CutleryDose) <
                0.0001f,
                "The physical lead cutlery must contribute exactly its 0.010 Core dose.");
        }

        EndToEndAssert.Equal(1, fixture.Plate.stackCount,
            "Native ingestion must conserve the exact uranium plate unit.");
        EndToEndAssert.Equal(1, fixture.Cutlery.stackCount,
            "Native ingestion must conserve the exact cutlery unit.");
        EndToEndAssert.True(fixture.Plate.GetComp<CompSanitation>()!.IsDirty,
            "The returned uranium plate must follow the ordinary dirty-dish lifecycle.");
        EndToEndAssert.True(fixture.Cutlery.GetComp<CompSanitation>()!.IsDirty,
            "The returned cutlery must follow the ordinary dirty-dish lifecycle.");
        EndToEndAssert.True(
            fixture.Plate.GetInspectString().IndexOf("radiation", StringComparison.OrdinalIgnoreCase) < 0 &&
            fixture.Plate.GetInspectString().IndexOf("toxic", StringComparison.OrdinalIgnoreCase) < 0,
            "Returned ware must not expose latent unsafe-material health state.");
    }

    private Hediff CurrentRadiation(DiningCase fixture) =>
        fixture.Diner.health.hediffSet.GetFirstHediffOfDef(radiationDef) ??
        throw new InvalidOperationException("Expected Rimatomics radiation sickness after ingestion.");

    private static MethodInfo ResolveRadiationMethod()
    {
        var type = AccessTools.TypeByName(RimatomicsRadiationPolicy.TypeName);
        EndToEndAssert.NotNull(type, "The exact Rimatomics radiation utility type must be loaded.");
        var method = type.GetMethod(
            RimatomicsRadiationPolicy.MethodName,
            BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly,
            null,
            new[] { typeof(Pawn), typeof(float) },
            null);
        EndToEndAssert.NotNull(method,
            "The exact public static Rimatomics radiation method must remain available.");
        return method;
    }

    private sealed class DiningCase
    {
        internal DiningCase(
            Pawn diner,
            ThingWithComps meal,
            ThingWithComps plate,
            ThingWithComps cutlery,
            bool expectLeadDose)
        {
            Diner = diner;
            Meal = meal;
            Plate = plate;
            Cutlery = cutlery;
            ExpectLeadDose = expectLeadDose;
        }

        internal Pawn Diner { get; }
        internal ThingWithComps Meal { get; }
        internal ThingWithComps Plate { get; }
        internal ThingWithComps Cutlery { get; }
        internal bool ExpectLeadDose { get; }
    }

    private static class RadiationObserver
    {
        private static Pawn? target;
        private static HediffDef? radiation;
        private static HediffDef? crashLandingRadiation;

        internal static List<float> RadiationStrengths { get; } = new();
        internal static List<float> CoreToxicDoses { get; } = new();
        internal static List<float> CrashLandingRadiationDoses { get; } = new();

        internal static void Reset(
            Pawn? pawn,
            HediffDef? radiationDef,
            HediffDef? crashLandingRadiationDef)
        {
            target = pawn;
            radiation = radiationDef;
            crashLandingRadiation = crashLandingRadiationDef;
            RadiationStrengths.Clear();
            CoreToxicDoses.Clear();
            CrashLandingRadiationDoses.Clear();
        }

        public static void RadiationPrefix(Pawn __0, float __1)
        {
            if (ReferenceEquals(__0, target) && __1 > 0f)
            {
                RadiationStrengths.Add(__1);
            }
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
            else if (ReferenceEquals(__1, crashLandingRadiation))
            {
                CrashLandingRadiationDoses.Add(__2);
            }
            else if (ReferenceEquals(__1, radiation))
            {
                EndToEndAssert.True(RadiationStrengths.Count > 0,
                    "Rimatomics radiation severity must originate inside its observed public application seam.");
            }
        }
    }
}
