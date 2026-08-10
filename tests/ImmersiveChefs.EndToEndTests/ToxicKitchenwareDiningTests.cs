using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.toxic-kitchenware-dining",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.immersivechefs",
    MaxFrames = 7_200,
    MaxGameTicks = 25_000,
    MaxWallClockSeconds = 270)]
public sealed class ToxicKitchenwareDiningTest : IRimWorldEndToEndTest
{
    private const string ObserverHarmonyId =
        "fumblesneeze.immersivechefs.e2e.toxic-kitchenware-dose-observer";

    private Map map = null!;
    private Pawn diner = null!;
    private Building stove = null!;
    private RecipeDef recipe = null!;
    private ThingWithComps cookware = null!;
    private ThingWithComps? meal;
    private ThingWithComps plate = null!;
    private ThingWithComps cutlery = null!;
    private Hediff toxicBuildup = null!;
    private string dinerId = string.Empty;
    private string stoveId = string.Empty;
    private string cookwareId = string.Empty;
    private string plateId = string.Empty;
    private string cutleryId = string.Empty;
    private bool nativeBillObserved;

    public void Arrange(IEndToEndContext context)
    {
        var previousExposureScale = ImmersiveChefsMod.Settings.ToxicKitchenwareExposureScale;
        var previousTemperature = ImmersiveChefsMod.Settings.MealTemperatureEnabled;
        var previousWareMode = ImmersiveChefsMod.Settings.WareRequirementMode;
        var previousAssistants = ImmersiveChefsMod.Settings.AutoCallAssistants;
        context.DeferCleanup(() =>
        {
            ImmersiveChefsMod.Settings.ToxicKitchenwareExposureScale = previousExposureScale;
            ImmersiveChefsMod.Settings.MealTemperatureEnabled = previousTemperature;
            ImmersiveChefsMod.Settings.WareRequirementMode = previousWareMode;
            ImmersiveChefsMod.Settings.AutoCallAssistants = previousAssistants;
        });
        ImmersiveChefsMod.Settings.ToxicKitchenwareExposureScale = 1f;
        ImmersiveChefsMod.Settings.MealTemperatureEnabled = false;
        ImmersiveChefsMod.Settings.WareRequirementMode = WareRequirementMode.Strict;
        ImmersiveChefsMod.Settings.AutoCallAssistants = false;

        map = Current.Game.CurrentMap;
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);

        diner = CreateCookingColonist("Toxic ware diner");
        FoodSearchE2EFixture.SetHunger(diner, 0.95f);
        GenSpawn.Spawn(diner, center + (IntVec3.West * 3), map);
        SetOnlyCookingPriority(diner, active: false);

        HealthUtility.AdjustSeverity(diner, HediffDefOf.ToxicBuildup, 0.039f);
        toxicBuildup = diner.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup);
        EndToEndAssert.NotNull(toxicBuildup,
            "The toxicity fixture must begin with vanilla ToxicBuildup below its first visible stage.");
        EndToEndAssert.False(toxicBuildup.Visible,
            "Vanilla must keep the initial sub-threshold ToxicBuildup stage hidden.");

        var stoveDef = DefDatabase<ThingDef>.GetNamed("FueledStove");
        stove = (Building)ThingMaker.MakeThing(
            stoveDef,
            stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
        stove.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(stove, center, map, Rot4.North);
        stove.TryGetComp<CompRefuelable>()?.Refuel(999f);

        recipe = DefDatabase<RecipeDef>.GetNamed("CookMealSimple");
        var bill = new Bill_Production(recipe)
        {
            repeatMode = BillRepeatModeDefOf.RepeatCount,
            repeatCount = 1,
            ingredientSearchRadius = 8f
        };
        bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        bill.SetPawnRestriction(diner);
        ((IBillGiver)stove).BillStack.AddBill(bill);

        var rice = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RawRice"));
        rice.stackCount = 20;
        GenSpawn.Spawn(rice, center + (IntVec3.East * 2), map);

        cookware = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cookware",
            ThingDefOf.Uranium);
        plate = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Plate",
            ThingDefOf.Steel);
        cutlery = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cutlery",
            ThingDefOf.Uranium);
        GenSpawn.Spawn(cookware, center + (IntVec3.West * 2), map);
        GenSpawn.Spawn(plate, center + IntVec3.West, map);
        GenSpawn.Spawn(cutlery, center + IntVec3.East, map);

        dinerId = diner.ThingID;
        stoveId = stove.ThingID;
        cookwareId = cookware.ThingID;
        plateId = plate.ThingID;
        cutleryId = cutlery.ThingID;

        ToxicDoseObserver.Reset(diner);
        var target = AccessTools.Method(
            typeof(HealthUtility),
            nameof(HealthUtility.AdjustSeverity),
            new[] { typeof(Pawn), typeof(HediffDef), typeof(float) });
        EndToEndAssert.NotNull(target,
            "The exact vanilla toxic-severity application seam must remain available.");
        var observer = new Harmony(ObserverHarmonyId);
        observer.Patch(
            target,
            prefix: new HarmonyMethod(typeof(ToxicDoseObserver), nameof(ToxicDoseObserver.Prefix)));
        context.DeferCleanup(() =>
        {
            observer.Unpatch(target, HarmonyPatchType.Prefix, ObserverHarmonyId);
            ToxicDoseObserver.Reset(null);
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause below the vanilla toxic-buildup visibility threshold",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the pre-threshold toxic-ware diner",
            new[] { dinerId },
            additive: false);
        yield return new CameraActionStep(
            "frame the toxic-cookware kitchen fixture",
            new[] { dinerId, stoveId, cookwareId, plateId, cutleryId },
            paddingPixels: 220);
        yield return new PawnInspectTabActionStep(
            "open the native Health tab below the visibility threshold",
            dinerId,
            EndToEndPawnInspectTab.Health);
        yield return new ScreenshotStep(
            "observe vanilla Health hiding sub-threshold toxic buildup",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "enable the native simple-meal bill for the toxic-cookware chef",
            _ => SetOnlyCookingPriority(diner, active: true));
        yield return new TimeControlActionStep(
            "run native cooking with the exact uranium cookware",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the ordinary DoBill job reserves and renders the uranium cookware",
            _ => ObserveNativeCooking(),
            new EndToEndDeadline(1_200, 5_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause during native uranium-cookware bill work",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the chef using the exact uranium cookware",
            new[] { dinerId },
            additive: false);
        yield return new CameraActionStep(
            "frame the chef and stove during toxic-cookware work",
            new[] { dinerId, stoveId },
            paddingPixels: 150);
        yield return new ScreenshotStep(
            "observe the actual uranium cookware during ordinary cooking",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new TimeControlActionStep(
            "finish the native uranium-cookware bill",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "native cooking produces one steel-plated meal and returns the cookware",
            _ => TryResolveCookedMeal() && cookware.Spawned,
            new EndToEndDeadline(1_800, 7_000, TimeSpan.FromSeconds(60)));
        yield return new TimeControlActionStep(
            "pause after native toxic-cookware cooking",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the cooked serving records the actual uranium cookware exactly once",
            _ => AssertCookedToxicState());

        var mealId = meal!.ThingID;
        yield return new SelectionActionStep(
            "select the uranium-cooked meal before ingestion",
            new[] { mealId },
            additive: false);
        yield return new ScreenshotStep(
            "observe no toxicity diagnostic on the meal",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "ordinary ware and meal inspection keeps material exposure latent",
            _ => AssertThingInspectionIsPrivate());
        yield return new AssertionStep(
            "activate hunger only for the explicit toxic-ware Consume order",
            _ =>
            {
                SetOnlyCookingPriority(diner, active: false);
                FoodSearchE2EFixture.SetHunger(diner, 0.20f);
            });

        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(dinerId, mealId);
        var consume = options.Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("consume", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(1, consume.Length,
            "Expected one enabled native Consume option for the toxic-cookware dining fixture.");
        yield return new FloatMenuActionStep(
            "order native consumption with the uranium cookware provenance and cutlery",
            dinerId,
            mealId,
            consume[0].StableId);
        yield return new TimeControlActionStep(
            "run native toxic-cookware and cutlery ingestion",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "ordinary ingest toils acquire the exact uranium cutlery",
            _ => !meal!.Destroyed &&
                 diner.CurJobDef == JobDefOf.Ingest &&
                 !cutlery.Spawned &&
                 ReferenceEquals(cutlery.holdingOwner, diner.inventory?.innerContainer),
            new EndToEndDeadline(600, 2_000, TimeSpan.FromSeconds(35)));
        yield return new SelectionActionStep(
            "select the diner during uranium-cookware and cutlery ingestion",
            new[] { dinerId },
            additive: false);
        yield return new ScreenshotStep(
            "observe native ingestion using the uranium cutlery setting",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new WaitUntilStep(
            "native ingestion returns the exact setting and crosses the visible threshold",
            _ => OutcomeCompleted(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep(
            "pause after toxic-ware ingestion",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "apply exactly one material dose and preserve exact returned ware",
            _ => AssertCompleted());
        yield return new SelectionActionStep(
            "select the diner after toxic-ware ingestion",
            new[] { dinerId },
            additive: false);
        yield return new PawnInspectTabActionStep(
            "open the native Health tab after crossing the threshold",
            dinerId,
            EndToEndPawnInspectTab.Health);
        yield return new ScreenshotStep(
            "observe vanilla Health revealing toxic buildup after ingestion",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return ThingInfoCardActionStep.Open(
            "open the exact returned uranium cutlery info card",
            cutleryId);
        yield return new ScreenshotStep(
            "observe returned uranium cutlery without a toxicity diagnostic",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return ThingInfoCardActionStep.Close(
            "close the returned uranium cutlery info card",
            cutleryId);
        yield return new CheckpointStep(
            "toxic kitchenware dining result",
            _ => new Dictionary<string, string>
            {
                ["diner"] = dinerId,
                ["mealDestroyed"] = meal!.Destroyed.ToString(),
                ["nativeBillObserved"] = nativeBillObserved.ToString(),
                ["cookware"] = cookwareId,
                ["cookwareMaterial"] = KitchenMaterialKind.Uranium.ToString(),
                ["plate"] = plateId,
                ["cutlery"] = cutleryId,
                ["doseCount"] = ToxicDoseObserver.Doses.Count.ToString(),
                ["dose"] = ToxicDoseObserver.Doses.Single().ToString("0.000"),
                ["toxicSeverity"] = toxicBuildup.Severity.ToString("0.000"),
                ["toxicVisible"] = toxicBuildup.Visible.ToString()
            });
    }

    private void AssertThingInspectionIsPrivate()
    {
        EndToEndAssert.NotNull(meal,
            "Native cooking must create the meal before its ordinary inspection is checked.");
        foreach (var inspected in new Thing[] { meal!, cookware, plate, cutlery })
        {
            var text = inspected.GetInspectString();
            EndToEndAssert.True(
                text.IndexOf("toxic", StringComparison.OrdinalIgnoreCase) < 0,
                $"Ordinary inspection for {inspected.ThingID} must not reveal latent toxic exposure.");
        }

        EndToEndAssert.False(toxicBuildup.Visible,
            "The pawn's pre-threshold vanilla toxic buildup must remain hidden before ingestion.");
        EndToEndAssert.Equal(0, ToxicDoseObserver.Doses.Count,
            "Crafting-equivalent setup, carrying, selection, and inspection must apply no toxic dose.");
    }

    private bool OutcomeCompleted()
    {
        if (meal is null || !meal.Destroyed || ToxicDoseObserver.Doses.Count != 1)
        {
            return false;
        }

        toxicBuildup = diner.health.hediffSet.GetFirstHediffOfDef(HediffDefOf.ToxicBuildup);
        return toxicBuildup is not null && toxicBuildup.Visible &&
               plate.Spawned && cutlery.Spawned;
    }

    private void AssertCompleted()
    {
        EndToEndAssert.Equal(1, ToxicDoseObserver.Doses.Count,
            "The completed native ingestion must apply exactly one toxic-ware dose.");
        EndToEndAssert.True(
            Math.Abs(ToxicDoseObserver.Doses[0] - 0.030f) < 0.0001f,
            "Native uranium cookware and uranium cutlery at default scale must apply exactly 0.020 + 0.010.");
        EndToEndAssert.True(toxicBuildup.Visible,
            "Crossing vanilla's current threshold must make ToxicBuildup visible on the pawn.");
        EndToEndAssert.Equal(plateId, plate.ThingID,
            "Native ingestion must return the exact embedded steel plate.");
        EndToEndAssert.Equal(cutleryId, cutlery.ThingID,
            "Native ingestion must return the exact uranium cutlery.");
        EndToEndAssert.Equal(1, plate.stackCount,
            "Native ingestion must conserve one exact steel plate unit.");
        EndToEndAssert.Equal(1, cutlery.stackCount,
            "Native ingestion must conserve one exact uranium cutlery unit.");
        EndToEndAssert.True(plate.GetComp<CompSanitation>()!.IsDirty,
            "The exact returned steel plate must follow the ordinary dirty-dish lifecycle.");
        EndToEndAssert.True(cutlery.GetComp<CompSanitation>()!.IsDirty,
            "The exact returned uranium cutlery must follow the ordinary dirty-dish lifecycle.");
        AssertThingInspectionIsPrivateAfterDining();
    }

    private void AssertThingInspectionIsPrivateAfterDining()
    {
        foreach (var inspected in new Thing[] { cookware, plate, cutlery })
        {
            EndToEndAssert.True(
                inspected.GetInspectString().IndexOf("toxic", StringComparison.OrdinalIgnoreCase) < 0,
                $"Returned ware {inspected.ThingID} must not expose a toxicity diagnostic.");
        }
    }

    private bool ObserveNativeCooking()
    {
        nativeBillObserved |= diner.CurJobDef == JobDefOf.DoBill &&
                              diner.CurJob?.RecipeDef == recipe &&
                              CookingSessionRegistry.TryGetActiveWorkProp(
                                  diner,
                                  out var prop,
                                  out var giver) &&
                              ReferenceEquals(prop, cookware) &&
                              ReferenceEquals(giver, stove);
        return nativeBillObserved;
    }

    private bool TryResolveCookedMeal()
    {
        meal ??= map.listerThings.ThingsOfDef(ThingDefOf.MealSimple)
            .OfType<ThingWithComps>()
            .FirstOrDefault(candidate => ReferenceEquals(
                candidate.GetComp<CompEmbeddedWare>()?.PeekPlateThing(),
                plate));
        return meal is not null;
    }

    private void AssertCookedToxicState()
    {
        EndToEndAssert.True(nativeBillObserved,
            "The toxicity workflow must observe RimWorld's ordinary DoBill job.");
        EndToEndAssert.NotNull(meal,
            "The native bill must create one steel-plated simple meal.");
        var servings = meal!.GetComp<CompCulinaryState>()!.Servings;
        EndToEndAssert.Equal(1, servings.Count,
            "The native single-meal bill must create exactly one serving record.");
        EndToEndAssert.Equal(KitchenMaterialKind.Uranium, servings[0].CookwareMaterial,
            "The serving must capture its actual reserved uranium cookware material.");
        EndToEndAssert.True(ReferenceEquals(
                meal.GetComp<CompEmbeddedWare>()!.PeekPlateThing(),
                plate),
            "The native bill must embed the exact ordinary steel plate.");
        EndToEndAssert.True(cookware.Spawned &&
                            cookware.GetComp<CompSanitation>()!.IsDirty,
            "The exact uranium cookware must return dirty after native cooking.");
        EndToEndAssert.Equal(ThingDefOf.Uranium, cookware.Stuff,
            "The physical cookware fixture must actually be made from Core uranium.");
        EndToEndAssert.Equal(ThingDefOf.Steel, plate.Stuff,
            "The physical plate must remain non-toxic steel to isolate the cookware contribution.");
        EndToEndAssert.Equal(ThingDefOf.Uranium, cutlery.Stuff,
            "The dining cutlery fixture must actually be made from Core uranium.");
        EndToEndAssert.Equal(0, ToxicDoseObserver.Doses.Count,
            "Native cooking itself must not apply toxic buildup before positive ingestion.");
    }

    private static Pawn CreateCookingColonist(string name)
    {
        var cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        for (var attempt = 0; attempt < 96; attempt++)
        {
            var pawn = FoodSearchE2EFixture.CreateColonist(name);
            if (!pawn.WorkTypeIsDisabled(cooking) &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) >= 0.9f &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) >= 0.9f)
            {
                pawn.skills.GetSkill(SkillDefOf.Cooking).Level = 20;
                if (pawn.needs?.rest is { } rest)
                {
                    rest.CurLevelPercentage = 1f;
                }

                for (var hour = 0; hour < 24; hour++)
                {
                    pawn.timetable?.SetAssignment(hour, TimeAssignmentDefOf.Work);
                }

                return pawn;
            }

            pawn.Destroy(DestroyMode.Vanish);
        }

        throw new EndToEndAssertionException("Could not generate a capable toxic-cookware chef.");
    }

    private static void SetOnlyCookingPriority(Pawn pawn, bool active)
    {
        var cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
        {
            if (!pawn.WorkTypeIsDisabled(workType))
            {
                pawn.workSettings.SetPriority(workType, 0);
            }
        }

        if (active)
        {
            pawn.workSettings.SetPriority(cooking, 1);
        }

        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }

    private static class ToxicDoseObserver
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
