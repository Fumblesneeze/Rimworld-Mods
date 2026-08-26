using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.common-sense-post-cooking-cleanup",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "avilmask.CommonSense",
    "fumblesneeze.immersivechefs",
    MaxFrames = 4_800,
    MaxGameTicks = 18_000,
    MaxWallClockSeconds = 180)]
public sealed class CommonSensePostCookingCleanupTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn cook = null!;
    private Thing stove = null!;
    private ThingWithComps cookware = null!;
    private ThingWithComps plate = null!;
    private ThingWithComps? meal;
    private IntVec3 waterCell;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);
        waterCell = center + new IntVec3(3, 0, -2);
        HandwashingE2EFixture.SetTemporaryTerrain(
            context,
            map,
            waterCell,
            TerrainDefOf.WaterShallow);

        PreserveSettings(context);
        var settings = ImmersiveChefsMod.Settings;
        settings.WareRequirementMode = WareRequirementMode.Strict;
        settings.DirtyWareFallback = DirtyWareFallback.Never;
        settings.AllowTerrainHandwashing = true;
        settings.PreferDishwashers = true;
        settings.DishwashingWorkScale = 1f;
        settings.MealTemperatureEnabled = false;
        settings.AutoCallAssistants = false;

        stove = SpawnFurniture("FueledStove", center, Rot4.North, ThingDefOf.Steel);
        stove.TryGetComp<CompRefuelable>()?.Refuel(999f);

        cook = CreateCookingAndCleaningCapablePawn("Common Sense chef");
        cook.skills.GetSkill(SkillDefOf.Cooking).Level = 20;
        FoodSearchE2EFixture.SetHunger(cook, 1f);
        GenSpawn.Spawn(cook, stove.InteractionCell + IntVec3.South, map);
        SetOnlyCookingAndCleaning(cook);

        cookware = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cookware",
            ThingDefOf.Steel);
        plate = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Plate",
            ThingDefOf.Steel);
        GenSpawn.Spawn(cookware, center + (IntVec3.West * 3), map);
        GenSpawn.Spawn(plate, center + (IntVec3.West * 2), map);
        var rice = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RawRice"));
        rice.stackCount = 20;
        GenSpawn.Spawn(rice, center + (IntVec3.East * 2), map);

        var bill = new Bill_Production(DefDatabase<RecipeDef>.GetNamed("CookMealSimple"))
        {
            repeatMode = BillRepeatModeDefOf.RepeatCount,
            repeatCount = 1,
            ingredientSearchRadius = 8f
        };
        bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        bill.SetPawnRestriction(cook);
        ((IBillGiver)stove).BillStack.AddBill(bill);
        cook.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        if (Find.WindowStack.Windows.Any(window =>
                string.Equals(
                    window.GetType().FullName,
                    "LudeonTK.EditWindow_Log",
                    StringComparison.Ordinal)))
        {
            yield return new WindowCancelActionStep(
                "close the startup developer log before observing gameplay",
                "LudeonTK.EditWindow_Log");
        }

        yield return new TimeControlActionStep(
            "pause before Common Sense cooking",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the exact Common Sense adapter and capable cook are active",
            _ =>
            {
                EndToEndAssert.True(
                    CommonSenseAdapter.Enabled,
                    "The exact Common Sense package must bind its validated public cleaning shape.");
                EndToEndAssert.True(
                    cook.workSettings.WorkIsActive(WorkTypeDefOf.Cleaning) &&
                    !cook.WorkTypeIsDisabled(WorkTypeDefOf.Cleaning),
                    "The cooking pawn must be eligible for Common Sense cleanup.");
                EndToEndAssert.False(
                    cookware.GetComp<CompSanitation>()!.IsDirty,
                    "The exact cookware must begin clean.");
            });

        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(cook.ThingID, stove.ThingID);
        var prioritize = FindPrioritizeCooking(options);
        yield return new FloatMenuActionStep(
            "prioritize the native cooking bill",
            cook.ThingID,
            stove.ThingID,
            prioritize.StableId);
        yield return new TimeControlActionStep(
            "run native cooking into the Common Sense cleanup handoff",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the same cook begins washing the exact returned cookware",
            _ => TryResolveMeal() &&
                 cook.CurJobDef == ImmersiveChefsDefOf.ImmersiveChefs_DoDishes &&
                 ReferenceEquals(cook.CurJob?.targetA.Thing, cookware),
            new EndToEndDeadline(2_400, 9_000, TimeSpan.FromSeconds(85)));
        yield return new TimeControlActionStep(
            "pause at the post-cooking cleanup handoff",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "completed cooking hands the exact dirty cookware to its cook",
            _ =>
            {
                EndToEndAssert.NotNull(meal,
                    "The native bill must complete a real plated meal before cleanup starts.");
                EndToEndAssert.True(
                    cookware.GetComp<CompSanitation>()!.IsDirty,
                    "The exact cookware must still be dirty when its washing job begins.");
                EndToEndAssert.True(
                    ReferenceEquals(cook.CurJob?.targetA.Thing, cookware),
                    "The immediate Doing dishes job must target the same cookware Thing returned by cooking.");
            });
        yield return new SelectionActionStep(
            "select the cook claiming the returned cookware",
            new[] { cook.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the cook stove cookware and washing source",
            new[] { cook.ThingID, stove.ThingID, cookware.ThingID },
            paddingPixels: 160);
        yield return new ScreenshotStep(
            "same cook immediately claims the exact dirty cookware after cooking",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "run the ordinary cookware washing toil",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the cook visibly washes the exact cookware at the selected source",
            _ => HandwashingE2EFixture.IsDoingDishesAt(
                cook,
                waterCell,
                cookware.ThingID),
            new EndToEndDeadline(900, 3_000, TimeSpan.FromSeconds(35)));
        yield return new TimeControlActionStep(
            "pause during exact cookware washing",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "keep the washing cook selected",
            new[] { cook.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "cook visibly washes the same cookware through the ordinary job",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "finish post-cooking cookware washing",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the exact cookware returns clean",
            _ => cookware.Spawned &&
                 !cookware.GetComp<CompSanitation>()!.IsDirty &&
                 cook.CurJobDef != ImmersiveChefsDefOf.ImmersiveChefs_DoDishes,
            new EndToEndDeadline(1_200, 4_500, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause after post-cooking cleanup",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the exact cleaned cookware",
            new[] { cookware.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the completed meal and cleaned cookware",
            new[] { meal!.ThingID, cookware.ThingID, stove.ThingID },
            paddingPixels: 160);
        yield return new ScreenshotStep(
            "exact cookware is clean after its cook finishes the handoff",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "Common Sense post-cooking cleanup result",
            _ => new Dictionary<string, string>
            {
                ["cook"] = cook.ThingID,
                ["meal"] = meal!.ThingID,
                ["cookware"] = cookware.ThingID,
                ["cookwareClean"] = (!cookware.GetComp<CompSanitation>()!.IsDirty).ToString(),
                ["washingSource"] = waterCell.ToString(),
                ["commonSenseAdapter"] = CommonSenseAdapter.Enabled.ToString()
            });
    }

    private bool TryResolveMeal()
    {
        meal ??= map.listerThings.ThingsOfDef(ThingDefOf.MealSimple)
            .OfType<ThingWithComps>()
            .FirstOrDefault(candidate => ReferenceEquals(
                candidate.GetComp<CompEmbeddedWare>()?.PeekPlateThing(),
                plate));
        return meal is not null;
    }

    internal static EndToEndFloatMenuOption FindPrioritizeCooking(
        IReadOnlyList<EndToEndFloatMenuOption> options)
    {
        var matches = options.Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("prioritize", StringComparison.OrdinalIgnoreCase) >= 0 &&
                option.Label.IndexOf("cook", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(
            1,
            matches.Length,
            "Expected one enabled native Prioritize cooking command; observed " +
            string.Join(", ", options.Select(option =>
                $"'{option.Label}' (disabled={option.Disabled})")));
        return matches[0];
    }

    internal static Pawn CreateCookingAndCleaningCapablePawn(string name)
    {
        var cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        for (var attempt = 0; attempt < 96; attempt++)
        {
            var pawn = FoodSearchE2EFixture.CreateColonist(name);
            if (!pawn.WorkTypeIsDisabled(cooking) &&
                !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Cleaning) &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) >= 0.9f &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) >= 0.9f)
            {
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

        throw new EndToEndAssertionException(
            "Could not generate a pawn capable of both Cooking and Cleaning.");
    }

    internal static void SetOnlyCookingAndCleaning(Pawn pawn)
    {
        foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
        {
            if (!pawn.WorkTypeIsDisabled(workType))
            {
                pawn.workSettings.SetPriority(workType, 0);
            }
        }

        pawn.workSettings.SetPriority(DefDatabase<WorkTypeDef>.GetNamed("Cooking"), 1);
        pawn.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 1);
        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }

    private Thing SpawnFurniture(
        string defName,
        IntVec3 cell,
        Rot4 rotation,
        ThingDef? stuff)
    {
        var def = DefDatabase<ThingDef>.GetNamed(defName);
        var thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? stuff : null);
        thing.SetFactionDirect(Faction.OfPlayer);
        return GenSpawn.Spawn(thing, cell, map, rotation);
    }

    private static void PreserveSettings(IEndToEndContext context)
    {
        var settings = ImmersiveChefsMod.Settings;
        var priorMode = settings.WareRequirementMode;
        var priorFallback = settings.DirtyWareFallback;
        var priorTerrain = settings.AllowTerrainHandwashing;
        var priorPreferDishwashers = settings.PreferDishwashers;
        var priorScale = settings.DishwashingWorkScale;
        var priorTemperature = settings.MealTemperatureEnabled;
        var priorAssistants = settings.AutoCallAssistants;
        context.DeferCleanup(() =>
        {
            settings.WareRequirementMode = priorMode;
            settings.DirtyWareFallback = priorFallback;
            settings.AllowTerrainHandwashing = priorTerrain;
            settings.PreferDishwashers = priorPreferDishwashers;
            settings.DishwashingWorkScale = priorScale;
            settings.MealTemperatureEnabled = priorTemperature;
            settings.AutoCallAssistants = priorAssistants;
        });
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.common-sense-active-processor-dishwasher",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "syrchalis.processor.framework",
    "Dubwise.DubsBadHygiene",
    "avilmask.CommonSense",
    "fumblesneeze.immersivechefs",
    MaxFrames = 9_600,
    MaxGameTicks = 36_000,
    MaxWallClockSeconds = 330)]
public sealed class CommonSenseActiveProcessorDishwasherTest : IRimWorldEndToEndTest
{
    private PickUpAndHaulDishwasherFixture dishwasherFixture = null!;
    private Map map = null!;
    private Pawn cook = null!;
    private Thing stove = null!;
    private ThingWithComps cookware = null!;
    private ThingWithComps mealPlate = null!;
    private ThingWithComps? meal;
    private bool observedCommonSenseJob;
    private float firstProgressBeforeCooking;
    private float firstProgressAfterAdmission;
    private float cookwareProgressAfterAdmission;
    private float waterAfterFirstAdmission;
    private float waterAfterCookwareAdmission;

    public void Arrange(IEndToEndContext context)
    {
        dishwasherFixture = PickUpAndHaulDishwasherFixture.Create(
            context,
            requireProcessor: true,
            requirePickUpAndHaul: false);
        dishwasherFixture.PrepareContinuousAdmission();
        map = Current.Game.CurrentMap;

        var settings = ImmersiveChefsMod.Settings;
        var priorCommonSense = settings.CommonSense;
        var priorWorkScale = settings.DishwashingWorkScale;
        var priorTemperature = settings.MealTemperatureEnabled;
        var priorAssistants = settings.AutoCallAssistants;
        context.DeferCleanup(() =>
        {
            settings.CommonSense = priorCommonSense;
            settings.DishwashingWorkScale = priorWorkScale;
            settings.MealTemperatureEnabled = priorTemperature;
            settings.AutoCallAssistants = priorAssistants;
        });
        settings.CommonSense = OptionalIntegrationMode.Auto;
        // Keep the earlier load active throughout native cooking without making the
        // 12%-progress precondition or the final two-load completion exceed this group's
        // bounded native-frame deadlines.
        settings.DishwashingWorkScale = 0.25f;
        settings.MealTemperatureEnabled = false;
        settings.AutoCallAssistants = false;

        var center = dishwasherFixture.Dishwasher.Position + new IntVec3(-2, 0, -1);
        stove = SpawnFurniture(map, "FueledStove", center + new IntVec3(0, 0, 2), Rot4.North);
        stove.TryGetComp<CompRefuelable>()?.Refuel(999f);

        cook = CommonSensePostCookingCleanupTest.CreateCookingAndCleaningCapablePawn(
            "Common Sense processor chef");
        cook.skills.GetSkill(SkillDefOf.Cooking).Level = 20;
        FoodSearchE2EFixture.SetHunger(cook, 1f);
        GenSpawn.Spawn(cook, stove.InteractionCell + IntVec3.South, map);
        CommonSensePostCookingCleanupTest.SetOnlyCookingAndCleaning(cook);
        // The native Prioritize command below owns the start of cooking. Keep the cook
        // from autonomously completing the bill while the earlier dishwasher load is
        // still being established.
        cook.workSettings.SetPriority(DefDatabase<WorkTypeDef>.GetNamed("Cooking"), 0);
        cook.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 0);

        cookware = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cookware",
            ThingDefOf.Steel);
        mealPlate = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Plate",
            ThingDefOf.Steel);
        GenSpawn.Spawn(cookware, center + new IntVec3(-1, 0, 1), map);
        GenSpawn.Spawn(mealPlate, center, map);
        var rice = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RawRice"));
        rice.stackCount = 20;
        GenSpawn.Spawn(rice, center + new IntVec3(1, 0, 0), map);

        var bill = new Bill_Production(DefDatabase<RecipeDef>.GetNamed("CookMealSimple"))
        {
            repeatMode = BillRepeatModeDefOf.RepeatCount,
            repeatCount = 1,
            ingredientSearchRadius = 8f
        };
        bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        bill.SetPawnRestriction(cook);
        ((IBillGiver)stove).BillStack.AddBill(bill);
        cook.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        if (Find.WindowStack.Windows.Any(window =>
                string.Equals(
                    window.GetType().FullName,
                    "LudeonTK.EditWindow_Log",
                    StringComparison.Ordinal)))
        {
            yield return new WindowCancelActionStep(
                "close the startup developer log before observing gameplay",
                "LudeonTK.EditWindow_Log");
        }

        yield return new TimeControlActionStep(
            "pause before the established dishwasher load",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the exact optional integrations are active without Pick Up And Haul",
            _ =>
            {
                EndToEndAssert.True(CommonSenseAdapter.Enabled,
                    "Common Sense must bind before cooking cleanup is exercised.");
                EndToEndAssert.True(ProcessorFrameworkAdapter.Controls(dishwasherFixture.Dishwasher),
                    "The supplied dishwasher must use the exact Processor Framework path.");
                EndToEndAssert.Equal(0,
                    cook.AllComps.Count(comp =>
                        comp.GetType().FullName == "PickUpAndHaul.CompHauledToInventory"),
                    "The Common Sense cook must have no Pick Up And Haul tracker in this exact group.");
            });
        yield return new AssertionStep(
            "enable ordinary Cleaning for the earlier dirty plate",
            _ => dishwasherFixture.ActivateCleaning());
        yield return new TimeControlActionStep(
            "let the earlier plate enter and start washing",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the earlier exact plate is visibly partway through washing",
            _ => dishwasherFixture.FirstContinuousLoadHasProgress(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(80)));
        yield return new TimeControlActionStep(
            "pause on the already-active dishwasher",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "capture the earlier load progress and water debit",
            _ =>
            {
                dishwasherFixture.CaptureFirstContinuousLoad();
                firstProgressBeforeCooking = ProcessorFrameworkAdapter.ProgressPercent(
                    dishwasherFixture.Dishwasher,
                    dishwasherFixture.FirstContinuousWare);
                waterAfterFirstAdmission = HandwashingE2EFixture.ReadDubsNetworkWater(
                    dishwasherFixture.Dishwasher);
                dishwasherFixture.DisableCleanerWork();
            });
        yield return new SelectionActionStep(
            "select the dishwasher already washing the earlier plate",
            new[] { dishwasherFixture.Dishwasher.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the active dishwasher stove cook and exact cookware",
            new[]
            {
                dishwasherFixture.Dishwasher.ThingID,
                cook.ThingID,
                stove.ThingID,
                cookware.ThingID
            },
            paddingPixels: 190);
        yield return new ScreenshotStep(
            "an earlier exact plate is already washing before Common Sense cooking",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new AssertionStep(
            "assign the paused cook to cooking and Common Sense cleanup",
            _ =>
            {
                cook.workSettings.SetPriority(
                    DefDatabase<WorkTypeDef>.GetNamed("Cooking"),
                    1);
                cook.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 1);
            });
        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(cook.ThingID, stove.ThingID);
        var prioritize = CommonSensePostCookingCleanupTest.FindPrioritizeCooking(options);
        yield return new FloatMenuActionStep(
            "prioritize the native cooking bill while the dishwasher remains active",
            cook.ThingID,
            stove.ThingID,
            prioritize.StableId);
        yield return new TimeControlActionStep(
            "run native cooking into Common Sense Processor admission",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the native bill returns the exact cookware dirty beside its plated meal",
            _ => ObserveCompletedCooking(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(80)));
        yield return new WaitUntilStep(
            "the same cook carries the exact returned cookware into the active dishwasher",
            _ => ObserveCommonSenseAdmission(),
            new EndToEndDeadline(2_400, 8_000, TimeSpan.FromSeconds(80)));
        yield return new TimeControlActionStep(
            "pause after Common Sense joins the active dishwasher",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the active appliance owns both independently timed exact loads",
            _ =>
            {
                EndToEndAssert.NotNull(meal,
                    "The native bill must finish one real plated meal before cookware cleanup.");
                EndToEndAssert.True(observedCommonSenseJob,
                    "The cooking pawn must be observed in its real Common Sense Doing dishes job.");
                EndToEndAssert.True(firstProgressAfterAdmission + 0.01f >= firstProgressBeforeCooking,
                    "Common Sense admission must not reset the earlier plate's progress.");
                EndToEndAssert.True(cookwareProgressAfterAdmission + 5f < firstProgressAfterAdmission,
                    "The newly admitted cookware must begin on a younger independent progress clock.");
                EndToEndAssert.True(waterAfterCookwareAdmission < waterAfterFirstAdmission - 0.001f,
                    "The cookware admission must debit its own Dubs water after the earlier plate debit.");
            });
        yield return new SelectionActionStep(
            "select the active dishwasher containing both exact loads",
            new[] { dishwasherFixture.Dishwasher.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "Common Sense adds exact dirty cookware to the dishwasher already washing",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new AssertionStep(
            "enable native output hauling after both admissions",
            _ => dishwasherFixture.ActivateHaulingWhileCleaningRemainsEnabled());
        yield return new TimeControlActionStep(
            "finish both independently timed dishwasher loads",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the earlier plate and Common Sense cookware both return clean",
            _ => BothExactLoadsReturnedClean(),
            new EndToEndDeadline(2_400, 10_000, TimeSpan.FromSeconds(95)));
        yield return new TimeControlActionStep(
            "pause after both exact loads return clean",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the exact cleaned plate and cookware",
            new[]
            {
                dishwasherFixture.FirstContinuousWare.ThingID,
                cookware.ThingID
            },
            additive: false);
        yield return new ScreenshotStep(
            "both exact identities return clean after independent washing",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new SelectionActionStep(
            "select the exact cleaned plate for its ordinary inspector",
            new[] { dishwasherFixture.FirstContinuousWare.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "the earlier exact plate visibly returns clean",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new SelectionActionStep(
            "select the exact cleaned cookware for its ordinary inspector",
            new[] { cookware.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "the Common Sense cookware visibly returns clean",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "Common Sense active Processor dishwasher result",
            _ => new Dictionary<string, string>
            {
                ["dishwasher"] = dishwasherFixture.Dishwasher.ThingID,
                ["earlierPlate"] = dishwasherFixture.FirstContinuousWare.ThingID,
                ["commonSenseCookware"] = cookware.ThingID,
                ["commonSenseCook"] = cook.ThingID,
                ["commonSenseJobObserved"] = observedCommonSenseJob.ToString(),
                ["firstProgressBeforeCooking"] = firstProgressBeforeCooking.ToString("R"),
                ["firstProgressAfterAdmission"] = firstProgressAfterAdmission.ToString("R"),
                ["cookwareProgressAfterAdmission"] = cookwareProgressAfterAdmission.ToString("R"),
                ["waterAfterFirstAdmission"] = waterAfterFirstAdmission.ToString("R"),
                ["waterAfterCookwareAdmission"] = waterAfterCookwareAdmission.ToString("R")
            });
    }

    private bool ObserveCommonSenseAdmission()
    {
        ObserveCompletedCooking();

        if (cook.CurJobDef == ImmersiveChefsDefOf.ImmersiveChefs_DoDishes &&
            ReferenceEquals(cook.CurJob?.targetA.Thing, cookware) &&
            ReferenceEquals(cook.CurJob?.targetB.Thing, dishwasherFixture.Dishwasher))
        {
            observedCommonSenseJob = true;
        }

        var held = ProcessorFrameworkAdapter.HeldWare(dishwasherFixture.Dishwasher);
        if (!held.Any(item => ReferenceEquals(item, dishwasherFixture.FirstContinuousWare)) ||
            !held.Any(item => ReferenceEquals(item, cookware)))
        {
            return false;
        }

        firstProgressAfterAdmission = ProcessorFrameworkAdapter.ProgressPercent(
            dishwasherFixture.Dishwasher,
            dishwasherFixture.FirstContinuousWare);
        cookwareProgressAfterAdmission = ProcessorFrameworkAdapter.ProgressPercent(
            dishwasherFixture.Dishwasher,
            cookware);
        waterAfterCookwareAdmission = HandwashingE2EFixture.ReadDubsNetworkWater(
            dishwasherFixture.Dishwasher);
        return firstProgressAfterAdmission + 0.01f >= firstProgressBeforeCooking &&
               cookwareProgressAfterAdmission > 0f &&
               cookwareProgressAfterAdmission + 5f < firstProgressAfterAdmission;
    }

    private bool ObserveCompletedCooking()
    {
        meal ??= map.listerThings.ThingsOfDef(ThingDefOf.MealSimple)
            .OfType<ThingWithComps>()
            .FirstOrDefault(candidate => ReferenceEquals(
                candidate.GetComp<CompEmbeddedWare>()?.PeekPlateThing(),
                mealPlate));
        return meal is not null && cookware.Spawned && cookware.Map == map &&
               cookware.GetComp<CompSanitation>()?.IsDirty == true;
    }

    private bool BothExactLoadsReturnedClean()
    {
        return dishwasherFixture.FirstContinuousWare.Spawned &&
               dishwasherFixture.FirstContinuousWare.GetComp<CompSanitation>() is
                   { IsDirty: false, WashProvenance: WashProvenance.Safe } &&
               cookware.Spawned && cookware.Map == map &&
               cookware.GetComp<CompSanitation>() is
                   { IsDirty: false, WashProvenance: WashProvenance.Safe } &&
               dishwasherFixture.FirstContinuousWare.stackCount == 1 &&
               cookware.stackCount == 1;
    }

    private static Thing SpawnFurniture(
        Map map,
        string defName,
        IntVec3 cell,
        Rot4 rotation)
    {
        var def = DefDatabase<ThingDef>.GetNamed(defName);
        var thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? ThingDefOf.Steel : null);
        thing.SetFactionDirect(Faction.OfPlayer);
        return GenSpawn.Spawn(thing, cell, map, rotation);
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.common-sense-cook-for-yourself-cleanup-order",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "avilmask.CommonSense",
    "lordfelix.CookForYourself",
    "fumblesneeze.immersivechefs",
    MaxFrames = 6_000,
    MaxGameTicks = 24_000,
    MaxWallClockSeconds = 240)]
public sealed class CommonSenseCookForYourselfCleanupOrderTest : IRimWorldEndToEndTest
{
    private CookForYourselfFixture fixture = null!;
    private IntVec3 waterCell;
    private ThingWithComps? meal;

    public void Arrange(IEndToEndContext context)
    {
        fixture = CookForYourselfFixture.Create(
            context,
            "Common Sense self-cook",
            hunger: 0.10f,
            requireCleaning: true);
        var settings = ImmersiveChefsMod.Settings;
        var priorTerrain = settings.AllowTerrainHandwashing;
        var priorPreferDishwashers = settings.PreferDishwashers;
        var priorScale = settings.DishwashingWorkScale;
        context.DeferCleanup(() =>
        {
            settings.AllowTerrainHandwashing = priorTerrain;
            settings.PreferDishwashers = priorPreferDishwashers;
            settings.DishwashingWorkScale = priorScale;
        });
        settings.AllowTerrainHandwashing = true;
        settings.PreferDishwashers = true;
        settings.DishwashingWorkScale = 1f;

        waterCell = fixture.Stove.Position + new IntVec3(3, 0, -2);
        HandwashingE2EFixture.SetTemporaryTerrain(
            context,
            fixture.Map,
            waterCell,
            TerrainDefOf.WaterShallow);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before Common Sense one-off cooking",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "both exact optional integrations are active",
            _ =>
            {
                EndToEndAssert.True(CommonSenseAdapter.Enabled,
                    "Common Sense must bind before the one-off cooking workflow.");
                EndToEndAssert.True(CookForYourselfAdapter.Enabled,
                    "Cook for Yourself must bind before the one-off cooking workflow.");
                EndToEndAssert.True(
                    fixture.Cook.workSettings.WorkIsActive(WorkTypeDefOf.Cleaning),
                    "The self-cook must have Cleaning work active for Common Sense.");
            });
        yield return new SelectionActionStep(
            "select the drafted Common Sense self-cook",
            new[] { fixture.Cook.ThingID },
            additive: false);
        var draft = fixture.RequiredDraftToggle(context, expectedCurrentState: true);
        yield return new GizmoActionStep(
            "undraft through the native gizmo to start one-off cooking",
            new[] { fixture.Cook.ThingID },
            draft.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: draft.StableId);
        yield return new TimeControlActionStep(
            "run the native one-off cooking transition",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the direct ingestion transition starts exact cookware cleanup first",
            _ => TryResolveMeal() &&
                 fixture.Cook.CurJobDef == ImmersiveChefsDefOf.ImmersiveChefs_DoDishes &&
                 ReferenceEquals(fixture.Cook.CurJob?.targetA.Thing, fixture.Cookware),
            new EndToEndDeadline(2_600, 10_000, TimeSpan.FromSeconds(100)));
        yield return new TimeControlActionStep(
            "pause before Cook for Yourself ingestion",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "cleanup replaces the direct follow-up while preserving it in the queue",
            _ =>
            {
                EndToEndAssert.NotNull(meal,
                    "Cook for Yourself must finish a real covered meal before cleanup starts.");
                EndToEndAssert.True(
                    fixture.Cookware.GetComp<CompSanitation>()!.IsDirty,
                    "The exact cookware must remain dirty at cleanup admission.");
                EndToEndAssert.True(
                    fixture.Cook.jobs.jobQueue.Any(queued =>
                        queued.job.def == JobDefOf.Ingest &&
                        ReferenceEquals(queued.job.targetA.Thing, meal)),
                    "The unchanged upstream ingestion job must remain queued behind cookware cleanup.");
            });
        yield return new SelectionActionStep(
            "select the self-cook claiming exact cookware",
            new[] { fixture.Cook.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame one-off meal cookware cook and stove",
            new[]
            {
                fixture.Cook.ThingID,
                fixture.Cookware.ThingID,
                fixture.Stove.ThingID,
                meal!.ThingID
            },
            paddingPixels: 170);
        yield return new ScreenshotStep(
            "Cook for Yourself cleanup visibly runs before its preserved ingestion",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "run exact cookware hand-washing before ingestion",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the self-cook washes exact cookware at the fallback source",
            _ => HandwashingE2EFixture.IsDoingDishesAt(
                fixture.Cook,
                waterCell,
                fixture.Cookware.ThingID),
            new EndToEndDeadline(900, 3_000, TimeSpan.FromSeconds(35)));
        yield return new TimeControlActionStep(
            "finish cookware cleanup into preserved ingestion",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the preserved native ingestion starts after cookware is clean",
            _ => !fixture.Cookware.GetComp<CompSanitation>()!.IsDirty &&
                 fixture.Cook.CurJobDef == JobDefOf.Ingest &&
                 ReferenceEquals(fixture.Cook.CurJob?.targetA.Thing, meal),
            new EndToEndDeadline(1_300, 5_000, TimeSpan.FromSeconds(50)));
        yield return new TimeControlActionStep(
            "pause at the resumed native ingestion",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "Cook for Yourself resumes only after exact cookware is clean",
            _ =>
            {
                EndToEndAssert.False(
                    fixture.Cookware.GetComp<CompSanitation>()!.IsDirty,
                    "The exact cookware must be clean before the preserved ingestion starts.");
                EndToEndAssert.True(
                    fixture.Cook.CurJobDef == JobDefOf.Ingest,
                    "The unchanged upstream ingestion job must resume after cleaning.");
            });
        yield return new ScreenshotStep(
            "self-cook begins the preserved meal only after cookware cleanup",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "finish the preserved native ingestion",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the one-off meal is consumed after cleanup",
            _ => meal!.Destroyed &&
                 fixture.Cookware.Spawned &&
                 !fixture.Cookware.GetComp<CompSanitation>()!.IsDirty &&
                 fixture.Plate.Spawned &&
                 fixture.Plate.GetComp<CompSanitation>()!.IsDirty &&
                 fixture.Cutlery.Spawned &&
                 fixture.Cutlery.GetComp<CompSanitation>()!.IsDirty,
            new EndToEndDeadline(1_600, 6_000, TimeSpan.FromSeconds(60)));
        yield return new TimeControlActionStep(
            "pause after cleanup-first one-off dining",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new CheckpointStep(
            "Common Sense Cook for Yourself cleanup order result",
            _ => new Dictionary<string, string>
            {
                ["cook"] = fixture.Cook.ThingID,
                ["cookware"] = fixture.Cookware.ThingID,
                ["cookwareClean"] =
                    (!fixture.Cookware.GetComp<CompSanitation>()!.IsDirty).ToString(),
                ["mealConsumed"] = meal!.Destroyed.ToString(),
                ["plateDirty"] = fixture.Plate.GetComp<CompSanitation>()!.IsDirty.ToString(),
                ["cutleryDirty"] = fixture.Cutlery.GetComp<CompSanitation>()!.IsDirty.ToString()
            });
    }

    private bool TryResolveMeal()
    {
        meal ??= fixture.Map.listerThings.ThingsOfDef(ThingDefOf.MealSimple)
            .OfType<ThingWithComps>()
            .FirstOrDefault(candidate => ReferenceEquals(
                candidate.GetComp<CompEmbeddedWare>()?.PeekPlateThing(),
                fixture.Plate));
        return meal is not null;
    }
}
