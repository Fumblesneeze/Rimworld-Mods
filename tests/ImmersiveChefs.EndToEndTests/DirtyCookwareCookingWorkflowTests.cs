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
    "immersive-chefs.dirty-cookware-cooking-workflow",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "fumblesneeze.immersivechefs",
    MaxFrames = 10_500,
    MaxGameTicks = 46_000,
    MaxWallClockSeconds = 390)]
public sealed class DirtyCookwareCookingWorkflowTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private CookingFixture automatic = null!;
    private CookingFixture forced = null!;
    private Pawn plateCleaner = null!;
    private ThingWithComps shortWashPlate = null!;
    private Pawn cutleryCleaner = null!;
    private ThingWithComps shortWashCutlery = null!;
    private IntVec3 automaticWater;
    private IntVec3 plateWater;
    private IntVec3 cutleryWater;
    private ThingWithComps? automaticProduct;
    private ThingWithComps? forcedProduct;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        PreserveSettings(context);
        var centers = FindRoomCenters(map, 4);
        automaticWater = centers[0] + new IntVec3(3, 0, -2);
        plateWater = centers[1] + new IntVec3(3, 0, -2);
        cutleryWater = centers[3] + new IntVec3(3, 0, -2);
        FoodSearchE2EFixture.BuildSealedRoom(map, centers[0]);
        FoodSearchE2EFixture.BuildSealedRoom(map, centers[1]);
        FoodSearchE2EFixture.BuildSealedRoom(map, centers[2]);
        FoodSearchE2EFixture.BuildSealedRoom(map, centers[3]);
        HandwashingE2EFixture.SetTemporaryTerrain(
            context,
            map,
            automaticWater,
            TerrainDefOf.WaterShallow);
        HandwashingE2EFixture.SetTemporaryTerrain(
            context,
            map,
            plateWater,
            TerrainDefOf.WaterShallow);
        HandwashingE2EFixture.SetTemporaryTerrain(
            context,
            map,
            cutleryWater,
            TerrainDefOf.WaterShallow);

        automatic = CreateCookingFixture(
            centers[0],
            "Cooking prerequisite washer",
            activateCooking: true);
        forced = CreateCookingFixture(
            centers[2],
            "Dirty cookware override chef",
            activateCooking: false);

        plateCleaner = CreateCapableWorker(
            "Short plate washer",
            requireCooking: false,
            requireCleaning: true);
        GenSpawn.Spawn(plateCleaner, centers[1] + (IntVec3.South * 3), map);
        SetOnlyWorkPriority(plateCleaner, workType: null);
        shortWashPlate = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Plate",
            ThingDefOf.Steel);
        shortWashPlate.GetComp<CompSanitation>()!.MarkDirty();
        GenSpawn.Spawn(shortWashPlate, centers[1] + (IntVec3.West * 2), map);

        cutleryCleaner = CreateCapableWorker(
            "Short cutlery washer",
            requireCooking: false,
            requireCleaning: true);
        GenSpawn.Spawn(cutleryCleaner, centers[3] + (IntVec3.South * 3), map);
        SetOnlyWorkPriority(cutleryCleaner, workType: null);
        shortWashCutlery = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cutlery",
            ThingDefOf.Steel);
        shortWashCutlery.GetComp<CompSanitation>()!.MarkDirty();
        GenSpawn.Spawn(shortWashCutlery, centers[3] + (IntVec3.West * 2), map);

        var settings = ImmersiveChefsMod.Settings;
        settings.WareRequirementMode = WareRequirementMode.Strict;
        settings.DirtyWareFallback = DirtyWareFallback.Never;
        settings.EmergencyHungerThreshold = 0.01f;
        settings.PreferDishwashers = true;
        settings.AllowTerrainHandwashing = true;
        settings.DishwashingWorkScale = 1f;
        settings.AutoCallAssistants = false;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before the dirty-cookware workflows",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "Cooking is enabled while Cleaning is disabled for the prerequisite washer",
            _ =>
            {
                EndToEndAssert.True(
                    automatic.Cook.workSettings.GetPriority(CookingWorkType) > 0,
                    "The prerequisite pawn must have ordinary Cooking work enabled.");
                EndToEndAssert.Equal(0, automatic.Cook.workSettings.GetPriority(WorkTypeDefOf.Cleaning),
                    "The prerequisite pawn must not rely on Cleaning work.");
                EndToEndAssert.True(automatic.Cookware.GetComp<CompSanitation>()!.IsDirty,
                    "The only available cookware must begin dirty.");
            });
        yield return new SelectionActionStep(
            "select the dirty cookware before ordinary Cooking work",
            new[] { automatic.Cookware.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the cook dirty cookware stove and water",
            new[]
            {
                automatic.Cook.ThingID,
                automatic.Cookware.ThingID,
                automatic.Stove.ThingID
            },
            paddingPixels: 190);
        yield return new ScreenshotStep(
            "dirty cookware is the only blocker before ordinary Cooking",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "run ordinary Cooking work with Cleaning disabled",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the Cooking worker starts the normal cookware washing toil",
            _ => IsWaitingToWash(
                automatic.Cook,
                automatic.Cookware,
                automaticWater,
                expectedDuration: 1000),
            new EndToEndDeadline(600, 2_500, TimeSpan.FromSeconds(30)));
        yield return new TimeControlActionStep(
            "pause during prerequisite cookware washing",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the prerequisite wash uses the full cookware-set duration",
            _ => AssertWashToil(
                automatic.Cook,
                automatic.Cookware,
                expectedDuration: 1000));
        yield return new SelectionActionStep(
            "select the cook doing prerequisite cookware washing",
            new[] { automatic.Cook.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "Cooking worker visibly hand-washes the exact cookware with Cleaning disabled",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "finish prerequisite washing and begin the original bill",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the same cook begins real bill work with the exact cookware prop",
            _ => CookingSessionRegistry.TryGetActiveWorkProp(
                     automatic.Cook,
                     out var prop,
                     out var giver) &&
                 ReferenceEquals(prop, automatic.Cookware) &&
                 ReferenceEquals(giver, automatic.Stove),
            new EndToEndDeadline(1_200, 5_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause during cooking with the exact cookware prop",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the paused cook still owns the exact active cookware prop",
            _ =>
            {
                EndToEndAssert.True(
                    CookingSessionRegistry.TryGetActiveWorkProp(
                        automatic.Cook,
                        out var prop,
                        out var giver) &&
                    ReferenceEquals(prop, automatic.Cookware) &&
                    ReferenceEquals(giver, automatic.Stove),
                    "The evidence pause must occur during real bill work with the exact cookware prop.");
                EndToEndAssert.True(ReferenceEquals(
                        automatic.Cookware.holdingOwner,
                        automatic.Cook.inventory?.innerContainer),
                    "The paused active cookware prop must remain in the cook inventory.");
            });
        yield return new SelectionActionStep(
            "select the cook using the exact cookware",
            new[] { automatic.Cook.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the active cook and stove closely",
            new[] { automatic.Cook.ThingID, automatic.Stove.ThingID },
            paddingPixels: 120);
        yield return new SelectionActionStep(
            "clear selection so the active cookware remains unobscured",
            Array.Empty<string>(),
            additive: false);
        yield return new ScreenshotStep(
            "exact Stuff-tinted cookware is visibly centered in front of the active cook",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "the work prop is the reserved physical cookware held by the cook",
            _ =>
            {
                EndToEndAssert.True(ReferenceEquals(
                        automatic.Cookware.holdingOwner,
                        automatic.Cook.inventory?.innerContainer),
                    "The rendered prop must remain the exact cookware Thing in the cook inventory.");
                EndToEndAssert.Equal(1, automatic.Cookware.stackCount,
                    "The rendered exact cookware set must remain one physical unit.");
            });

        yield return new TimeControlActionStep(
            "finish the ordinary bill after prerequisite washing",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the ordinary bill returns dirty cookware and a plated meal",
            _ => TryResolveProduct(automatic, out automaticProduct) &&
                 automatic.Cookware.Spawned &&
                 automatic.Cookware.GetComp<CompSanitation>()!.IsDirty,
            new EndToEndDeadline(1_800, 7_000, TimeSpan.FromSeconds(60)));
        yield return new TimeControlActionStep(
            "pause after ordinary cooking",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "prerequisite washing prevents dirty-cookware contamination",
            _ =>
            {
                EndToEndAssert.NotNull(automaticProduct,
                    "The native ordinary bill must produce its plated meal.");
                var serving = automaticProduct!.GetComp<CompCulinaryState>()!.Servings.Single();
                EndToEndAssert.False(
                    serving.Contamination.HasFlag(ContaminationSources.DirtyCookware),
                    "Cookware washed through the normal job must be clean when reserved for cooking.");
                EndToEndAssert.True(ReferenceEquals(
                        automaticProduct.GetComp<CompEmbeddedWare>()!.PeekPlateThing(),
                        automatic.Plate),
                    "The ordinary bill must embed its exact clean plate.");
                EndToEndAssert.True(
                    automatic.Stove.OccupiedRect().Contains(automatic.Cookware.Position),
                    "The exact used cookware must remain visibly on the stove surface after native cooking.");
            });

        yield return new AssertionStep(
            "activate ordinary Cleaning for the isolated plate timing check",
            _ =>
            {
                plateCleaner.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 1);
                plateCleaner.jobs.EndCurrentJob(JobCondition.InterruptForced);
            });
        yield return new TimeControlActionStep(
            "run ordinary plate hand-washing",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the plate cleaner begins the shorter plate washing toil",
            _ => IsWaitingToWash(
                plateCleaner,
                shortWashPlate,
                plateWater,
                expectedDuration: 250),
            new EndToEndDeadline(600, 2_000, TimeSpan.FromSeconds(25)));
        yield return new TimeControlActionStep(
            "pause during the shorter plate wash",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "one plate uses one quarter of a cookware-set wash",
            _ => AssertWashToil(plateCleaner, shortWashPlate, expectedDuration: 250));
        yield return new SelectionActionStep(
            "select the pawn washing one plate",
            new[] { plateCleaner.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the shorter plate washing action",
            new[] { plateCleaner.ThingID },
            paddingPixels: 190);
        yield return new ScreenshotStep(
            "one plate visibly uses the shorter normal washing progress",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new TimeControlActionStep(
            "finish the shorter plate wash",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the exact plate returns clean after its shorter wash",
            _ => shortWashPlate.Spawned &&
                 !shortWashPlate.GetComp<CompSanitation>()!.IsDirty,
            new EndToEndDeadline(800, 2_500, TimeSpan.FromSeconds(30)));
        yield return new TimeControlActionStep(
            "pause after plate washing",
            paused: true,
            EndToEndGameSpeed.Normal);

        yield return new AssertionStep(
            "activate ordinary Cleaning for the isolated cutlery timing check",
            _ =>
            {
                cutleryCleaner.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 1);
                cutleryCleaner.jobs.EndCurrentJob(JobCondition.InterruptForced);
            });
        yield return new TimeControlActionStep(
            "run ordinary cutlery hand-washing",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the cutlery cleaner begins the half-plate washing toil",
            _ => IsWaitingToWash(
                cutleryCleaner,
                shortWashCutlery,
                cutleryWater,
                expectedDuration: 125),
            new EndToEndDeadline(600, 2_000, TimeSpan.FromSeconds(25)));
        yield return new TimeControlActionStep(
            "pause during the cutlery wash",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "one cutlery setting uses the explicit half-plate duration",
            _ => AssertWashToil(cutleryCleaner, shortWashCutlery, expectedDuration: 125));
        yield return new SelectionActionStep(
            "select the pawn washing one cutlery setting",
            new[] { cutleryCleaner.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the cutlery washing action",
            new[] { cutleryCleaner.ThingID },
            paddingPixels: 190);
        yield return new ScreenshotStep(
            "one cutlery setting visibly uses the half-plate washing progress",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new TimeControlActionStep(
            "finish the cutlery wash",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the exact cutlery setting returns clean after its shorter wash",
            _ => shortWashCutlery.Spawned &&
                 !shortWashCutlery.GetComp<CompSanitation>()!.IsDirty,
            new EndToEndDeadline(800, 2_500, TimeSpan.FromSeconds(30)));
        yield return new TimeControlActionStep(
            "pause after cutlery washing",
            paused: true,
            EndToEndGameSpeed.Normal);

        yield return new AssertionStep(
            "activate the dirty-override cook while the game is paused",
            _ =>
            {
                forced.Cook.workSettings.SetPriority(CookingWorkType, 1);
                forced.Cook.jobs.EndCurrentJob(JobCondition.InterruptForced);
            });
        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(forced.Cook.ThingID, forced.Stove.ThingID);
        var forceOptions = options.Where(option =>
            !option.Disabled &&
            string.Equals(
                option.Label,
                "Force cook with dirty cookware",
                StringComparison.OrdinalIgnoreCase)).ToArray();
        yield return new AssertionStep(
            "the native right-click menu exposes exactly one dirty-cookware override",
            _ => EndToEndAssert.Equal(
                1,
                forceOptions.Length,
                "Expected exactly one enabled Force cook with dirty cookware option; observed " +
                string.Join(", ", options.Select(option =>
                    $"'{option.Label}' (disabled={option.Disabled})"))));
        yield return new FloatMenuActionStep(
            "choose the native one-job dirty-cookware override",
            forced.Cook.ThingID,
            forced.Stove.ThingID,
            forceOptions.Single().StableId);
        yield return new TimeControlActionStep(
            "run the explicitly forced dirty-cookware bill",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the forced bill begins with the exact dirty cookware prop",
            _ => CookingSessionRegistry.TryGetActiveWorkProp(
                     forced.Cook,
                     out var prop,
                     out var giver) &&
                 ReferenceEquals(prop, forced.Cookware) &&
                 ReferenceEquals(giver, forced.Stove),
            new EndToEndDeadline(1_200, 5_000, TimeSpan.FromSeconds(45)));
        yield return new TimeControlActionStep(
            "pause during the explicitly forced bill",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the chef executing the dirty override",
            new[] { forced.Cook.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the forced chef and stove closely",
            new[] { forced.Cook.ThingID, forced.Stove.ThingID },
            paddingPixels: 120);
        yield return new SelectionActionStep(
            "clear selection so the forced cookware remains unobscured",
            Array.Empty<string>(),
            additive: false);
        yield return new ScreenshotStep(
            "one-job override visibly cooks immediately with the exact dirty cookware",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "the forced job did not clean or replace its cookware",
            _ =>
            {
                EndToEndAssert.True(
                    forced.Cookware.GetComp<CompSanitation>()!.IsDirty,
                    "The right-click override must retain the dirty state during cooking.");
                EndToEndAssert.True(ReferenceEquals(
                        forced.Cookware.holdingOwner,
                        forced.Cook.inventory?.innerContainer),
                    "The forced bill must hold and render the same exact dirty cookware Thing.");
            });
        yield return new TimeControlActionStep(
            "finish the one-job dirty-cookware bill",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the forced bill produces a contaminated plated meal",
            _ => TryResolveProduct(forced, out forcedProduct) &&
                 forced.Cookware.Spawned,
            new EndToEndDeadline(1_800, 7_000, TimeSpan.FromSeconds(60)));
        yield return new TimeControlActionStep(
            "pause after the forced dirty-cookware bill",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the one-job override records dirty cookware contamination exactly once",
            _ =>
            {
                EndToEndAssert.NotNull(forcedProduct,
                    "The native forced bill must produce its plated meal.");
                var servings = forcedProduct!.GetComp<CompCulinaryState>()!.Servings;
                EndToEndAssert.Equal(1, servings.Count,
                    "The single-meal forced bill must retain exactly one serving record.");
                EndToEndAssert.True(
                    servings[0].Contamination.HasFlag(ContaminationSources.DirtyCookware),
                    "The forced dirty cookware must contribute its real contamination flag.");
                EndToEndAssert.True(ReferenceEquals(
                        forcedProduct.GetComp<CompEmbeddedWare>()!.PeekPlateThing(),
                        forced.Plate),
                    "The forced bill must still embed its exact clean plate.");
                EndToEndAssert.True(forced.Cookware.Spawned &&
                                    forced.Cookware.GetComp<CompSanitation>()!.IsDirty,
                    "The exact dirty cookware must return once after forced cooking.");
                EndToEndAssert.True(
                    forced.Stove.OccupiedRect().Contains(forced.Cookware.Position),
                    "The one-job override must return its exact cookware to the stove surface.");
            });
        yield return new SelectionActionStep(
            "select the forced meal after the one-job override",
            new[] { forcedProduct!.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the forced product returned cookware chef and stove",
            new[]
            {
                forcedProduct!.ThingID,
                forced.Cookware.ThingID,
                forced.Cook.ThingID,
                forced.Stove.ThingID
            },
            paddingPixels: 170);
        yield return new ScreenshotStep(
            "forced meal exists beside the exact returned dirty cookware",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new SelectionActionStep(
            "clear selection to inspect the returned dirty cookware at ordinary map zoom",
            Array.Empty<string>(),
            additive: false);
        yield return new ScreenshotStep(
            "returned dirty cookware is unmistakable without selection brackets",
            new[] { forced.Cookware.ThingID, forced.Stove.ThingID },
            paddingPixels: 170);
        yield return new SelectionActionStep(
            "select the exact returned dirty cookware for its native inspector",
            new[] { forced.Cookware.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "selected returned cookware keeps its exaggerated grime visible",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "dirty cookware cooking workflow result",
            _ => new Dictionary<string, string>
            {
                ["automaticCook"] = automatic.Cook.ThingID,
                ["automaticCookware"] = automatic.Cookware.ThingID,
                ["automaticProduct"] = automaticProduct!.ThingID,
                ["plateWashTicks"] = "250",
                ["cutleryWashTicks"] = "125",
                ["cookwareWashTicks"] = "1000",
                ["forcedCook"] = forced.Cook.ThingID,
                ["forcedCookware"] = forced.Cookware.ThingID,
                ["forcedProduct"] = forcedProduct!.ThingID,
                ["forcedDirtyContamination"] = "True"
            });
    }

    private CookingFixture CreateCookingFixture(
        IntVec3 center,
        string name,
        bool activateCooking)
    {
        var cook = CreateCapableWorker(name, requireCooking: true, requireCleaning: false);
        cook.skills.GetSkill(SkillDefOf.Cooking).Level = 20;
        GenSpawn.Spawn(cook, center + (IntVec3.South * 3), map);
        SetOnlyWorkPriority(cook, activateCooking ? CookingWorkType : null);

        var stoveDef = DefDatabase<ThingDef>.GetNamed("FueledStove");
        var stove = ThingMaker.MakeThing(
            stoveDef,
            stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
        stove.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(stove, center, map, Rot4.North);
        stove.TryGetComp<CompRefuelable>()?.Refuel(999f);

        var recipe = DefDatabase<RecipeDef>.GetNamed("CookMealSimple");
        var bill = new Bill_Production(recipe)
        {
            repeatMode = BillRepeatModeDefOf.RepeatCount,
            repeatCount = 1,
            ingredientSearchRadius = 8f
        };
        bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        bill.SetPawnRestriction(cook);
        ((IBillGiver)stove).BillStack.AddBill(bill);

        var cookware = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cookware",
            ThingDefOf.Steel);
        cookware.GetComp<CompSanitation>()!.MarkDirty();
        var plate = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Plate",
            ThingDefOf.Steel);
        GenSpawn.Spawn(cookware, center + (IntVec3.West * 3), map);
        GenSpawn.Spawn(plate, center + (IntVec3.West * 2), map);
        var rice = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RawRice"));
        rice.stackCount = 20;
        GenSpawn.Spawn(rice, center + (IntVec3.East * 2), map);

        return new CookingFixture(cook, stove, cookware, plate);
    }

    private static Pawn CreateCapableWorker(
        string name,
        bool requireCooking,
        bool requireCleaning)
    {
        for (var attempt = 0; attempt < 96; attempt++)
        {
            var pawn = FoodSearchE2EFixture.CreateColonist(name);
            var capable = (!requireCooking || !pawn.WorkTypeIsDisabled(CookingWorkType)) &&
                          (!requireCleaning || !pawn.WorkTypeIsDisabled(WorkTypeDefOf.Cleaning)) &&
                          pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) >= 0.9f &&
                          pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) >= 0.9f;
            if (capable)
            {
                FoodSearchE2EFixture.SetHunger(pawn, 1f);
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

        throw new EndToEndAssertionException("Could not generate capable cooking/washing fixture pawn.");
    }

    private static bool IsWaitingToWash(
        Pawn pawn,
        Thing ware,
        IntVec3 destination,
        int expectedDuration)
    {
        return HandwashingE2EFixture.IsDoingDishesAt(pawn, destination, ware.ThingID) &&
               CurrentToil(pawn) is { } toil &&
               toil.defaultCompleteMode == ToilCompleteMode.Delay &&
               toil.defaultDuration == expectedDuration;
    }

    private static void AssertWashToil(Pawn pawn, Thing ware, int expectedDuration)
    {
        EndToEndAssert.True(
            pawn.CurJobDef == ImmersiveChefsDefOf.ImmersiveChefs_DoDishes &&
            pawn.carryTracker.CarriedThing?.ThingID == ware.ThingID,
            "The real Doing dishes job must still carry the exact ware during its wait toil.");
        EndToEndAssert.Equal(
            expectedDuration,
            CurrentToil(pawn)?.defaultDuration ?? -1,
            "The real hand-washing toil must expose its product-scaled duration.");
    }

    private static Toil? CurrentToil(Pawn pawn)
    {
        var driver = pawn.jobs.curDriver;
        return driver is null
            ? null
            : typeof(JobDriver).GetProperty(
                    "CurToil",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(driver) as Toil;
    }

    private bool TryResolveProduct(CookingFixture fixture, out ThingWithComps? product)
    {
        product = map.listerThings.ThingsOfDef(ThingDefOf.MealSimple)
            .OfType<ThingWithComps>()
            .FirstOrDefault(meal => ReferenceEquals(
                meal.GetComp<CompEmbeddedWare>()?.PeekPlateThing(),
                fixture.Plate));
        return product is not null;
    }

    private static WorkTypeDef CookingWorkType =>
        DefDatabase<WorkTypeDef>.GetNamed("Cooking");

    private static void SetOnlyWorkPriority(Pawn pawn, WorkTypeDef? workType)
    {
        foreach (var candidate in DefDatabase<WorkTypeDef>.AllDefsListForReading)
        {
            if (!pawn.WorkTypeIsDisabled(candidate))
            {
                pawn.workSettings.SetPriority(candidate, 0);
            }
        }

        if (workType is not null)
        {
            pawn.workSettings.SetPriority(workType, 1);
        }

        pawn.jobs.EndCurrentJob(JobCondition.InterruptForced);
    }

    private static void PreserveSettings(IEndToEndContext context)
    {
        var settings = ImmersiveChefsMod.Settings;
        var priorMode = settings.WareRequirementMode;
        var priorFallback = settings.DirtyWareFallback;
        var priorThreshold = settings.EmergencyHungerThreshold;
        var priorPreferDishwashers = settings.PreferDishwashers;
        var priorTerrain = settings.AllowTerrainHandwashing;
        var priorScale = settings.DishwashingWorkScale;
        var priorAssistants = settings.AutoCallAssistants;
        context.DeferCleanup(() =>
        {
            settings.WareRequirementMode = priorMode;
            settings.DirtyWareFallback = priorFallback;
            settings.EmergencyHungerThreshold = priorThreshold;
            settings.PreferDishwashers = priorPreferDishwashers;
            settings.AllowTerrainHandwashing = priorTerrain;
            settings.DishwashingWorkScale = priorScale;
            settings.AutoCallAssistants = priorAssistants;
        });
    }

    private static IReadOnlyList<IntVec3> FindRoomCenters(Map map, int count)
    {
        var centers = new List<IntVec3>();
        for (var x = -54; x <= 54; x += 18)
        {
            for (var z = -54; z <= 54; z += 18)
            {
                var candidate = map.Center + new IntVec3(x, 0, z);
                if (!candidate.InBounds(map) ||
                    centers.Any(center => center.DistanceToSquared(candidate) < 225) ||
                    !SquareIsUsable(map, candidate, 6))
                {
                    continue;
                }

                centers.Add(candidate);
                if (centers.Count == count)
                {
                    return centers;
                }
            }
        }

        throw new EndToEndAssertionException("Could not find four separated cookware workflow rooms.");
    }

    private static bool SquareIsUsable(Map map, IntVec3 center, int radius)
    {
        for (var x = -radius; x <= radius; x++)
        {
            for (var z = -radius; z <= radius; z++)
            {
                var cell = center + new IntVec3(x, 0, z);
                if (!cell.InBounds(map) ||
                    !cell.Walkable(map) ||
                    cell.GetEdifice(map) is not null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private sealed class CookingFixture
    {
        internal CookingFixture(
            Pawn cook,
            Thing stove,
            ThingWithComps cookware,
            ThingWithComps plate)
        {
            Cook = cook;
            Stove = stove;
            Cookware = cookware;
            Plate = plate;
        }

        internal Pawn Cook { get; }
        internal Thing Stove { get; }
        internal ThingWithComps Cookware { get; }
        internal ThingWithComps Plate { get; }
    }
}
