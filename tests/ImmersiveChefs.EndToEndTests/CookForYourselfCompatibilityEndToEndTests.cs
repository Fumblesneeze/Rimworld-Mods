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
    "immersive-chefs.cook-for-yourself-self-cooking",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "lordfelix.CookForYourself",
    "Mehni.PickUpAndHaul",
    "Andromeda.StackGap",
    "fumblesneeze.immersivechefs",
    MaxFrames = 7_200,
    MaxGameTicks = 28_000,
    MaxWallClockSeconds = 285)]
public sealed class CookForYourselfSelfCookingTest : IRimWorldEndToEndTest
{
    private CookForYourselfFixture fixture = null!;
    private ThingWithComps? meal;
    private bool customCookingObserved;
    private bool activeCookwareObserved;
    private bool nativeIngestObserved;
    private int ingestDiagnosticTick;

    public void Arrange(IEndToEndContext context)
    {
        fixture = CookForYourselfFixture.Create(context, "Self-service cook", hunger: 0.10f);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before the one-off self-cooking workflow",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the drafted hungry self-service cook",
            new[] { fixture.Cook.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the one-off self-cooking kitchen",
            fixture.VisibleFixtureIds,
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "observe no prepared meal and no bill before self-cooking",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "the self-cooking fixture remains passive before player action",
            _ => AssertPassiveFixture());

        var draft = fixture.RequiredDraftToggle(context, expectedCurrentState: true);
        yield return new GizmoActionStep(
            "undraft through the native colonist gizmo to permit one-off self-cooking",
            new[] { fixture.Cook.ThingID },
            draft.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: draft.StableId);
        yield return new TimeControlActionStep(
            "run ordinary game time after native undraft",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "upstream custom cooking starts and the exact cookware becomes the work prop",
            _ => ObserveCustomCooking(),
            new EndToEndDeadline(1_800, 7_000, TimeSpan.FromSeconds(70)));
        yield return new SelectionActionStep(
            "select the cook during upstream one-off cooking",
            new[] { fixture.Cook.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the active upstream one-off cooking job",
            new[] { fixture.Cook.ThingID, fixture.Stove.ThingID },
            paddingPixels: 180);
        yield return new ScreenshotStep(
            "observe the exact kitchenware centrally rendered during upstream cooking",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "continue one-off cooking until the native ingestion transition is observable",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the upstream product enters native ingestion with its exact plate and cutlery",
            _ => ObserveNativeIngestion(),
            new EndToEndDeadline(2_400, 9_000, TimeSpan.FromSeconds(85)));
        yield return new SelectionActionStep(
            "select the self-service cook during native ingestion",
            new[] { fixture.Cook.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "observe upstream cooking transition into native plated ingestion",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "finish ordinary self-ingestion",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "native ingestion returns the same dirty service ware",
            _ => meal?.Destroyed == true &&
                 fixture.Plate.Spawned &&
                 fixture.Cutlery.Spawned,
            new EndToEndDeadline(2_400, 9_000, TimeSpan.FromSeconds(85)));
        yield return new TimeControlActionStep(
            "pause after one-off self-service dining",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the upstream self-cooking lifecycle completes exactly once",
            _ => AssertCompletedLifecycle());

        yield return new SelectionActionStep(
            "select the exact returned self-service cookware",
            new[] { fixture.Cookware.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the exact returned self-service ware",
            new[]
            {
                fixture.Cookware.ThingID,
                fixture.Plate.ThingID,
                fixture.Cutlery.ThingID
            },
            paddingPixels: 180);
        yield return new ScreenshotStep(
            "observe the exact returned dirty self-service kitchenware",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new SelectionActionStep(
            "select the exact returned self-service plate",
            new[] { fixture.Plate.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "observe the exact returned dirty self-service plate",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new SelectionActionStep(
            "select the exact returned self-service cutlery",
            new[] { fixture.Cutlery.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "observe the exact returned dirty self-service cutlery",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "Cook for Yourself self-cooking result",
            _ => new Dictionary<string, string>
            {
                ["upstreamJobObserved"] = customCookingObserved.ToString(),
                ["activeCookwareObserved"] = activeCookwareObserved.ToString(),
                ["nativeIngestObserved"] = nativeIngestObserved.ToString(),
                ["mealId"] = meal?.ThingID ?? "none",
                ["mealDestroyed"] = (meal?.Destroyed == true).ToString(),
                ["cookwareId"] = fixture.Cookware.ThingID,
                ["plateId"] = fixture.Plate.ThingID,
                ["cutleryId"] = fixture.Cutlery.ThingID
            });
    }

    private void AssertPassiveFixture()
    {
        EndToEndAssert.True(fixture.Cook.Drafted,
            "The hungry cook must remain drafted until the native player action starts the workflow.");
        EndToEndAssert.True(fixture.Cook.CurJobDef?.defName != "CFS_CookMealForSelf",
            "The upstream custom job must not begin before native undrafting.");
        EndToEndAssert.Equal(0, fixture.BillCount,
            "Cook for Yourself must remain a real one-off path with no hidden or visible bill.");
        EndToEndAssert.Equal(0, fixture.SpawnedCoveredMealCount,
            "The self-service fixture must not begin with a prepared covered meal.");
        fixture.AssertCleanWareUnits(meal: null);
    }

    private bool ObserveCustomCooking()
    {
        if (fixture.Cook.CurJobDef?.defName != "CFS_CookMealForSelf")
        {
            return false;
        }

        customCookingObserved = true;
        if (!CookingSessionRegistry.TryGetActiveWorkProp(
                fixture.Cook,
                out var activeCookware,
                out var activeStation))
        {
            return false;
        }

        EndToEndAssert.True(ReferenceEquals(fixture.Cookware, activeCookware),
            "The upstream custom cooking toil must render the exact reserved cookware.");
        EndToEndAssert.True(ReferenceEquals(fixture.Stove, activeStation),
            "The active kitchenware work prop must remain bound to the upstream-selected station.");
        EndToEndAssert.True(
            ReferenceEquals(fixture.Cookware.holdingOwner, fixture.Cook.inventory?.innerContainer),
            "The custom job must collect the exact cookware into the cook's inventory.");
        EndToEndAssert.True(
            ReferenceEquals(fixture.Plate.holdingOwner, fixture.Cook.inventory?.innerContainer),
            "The custom job must collect the exact plate before production.");
        fixture.AssertIngredientsPlacedBeforeWarePickup();
        EndToEndAssert.True(fixture.Cookware.GetComp<CompSanitation>()!.IsDirty,
            "Beginning actual cooking must dirty the exact cookware once.");
        EndToEndAssert.False(fixture.Plate.GetComp<CompSanitation>()!.IsDirty,
            "The unused plate must remain clean while cooking is in progress.");
        activeCookwareObserved = true;
        if (ingestDiagnosticTick == 0)
        {
            ingestDiagnosticTick = Find.TickManager.TicksGame + 2_000;
        }
        return true;
    }

    private bool ObserveNativeIngestion()
    {
        meal ??= fixture.JobTransitions.IngestMeal;
        if (!fixture.JobTransitions.NativeIngestObserved || meal is null)
        {
            if (ingestDiagnosticTick > 0 &&
                Find.TickManager.TicksGame >= ingestDiagnosticTick)
            {
                var covered = fixture.Map.listerThings.AllThings
                    .OfType<ThingWithComps>()
                    .Where(candidate => MealCoveragePolicy.IsCovered(candidate.def))
                    .Select(candidate =>
                        candidate.ThingID + ":" + candidate.def.defName +
                        ":plates=" +
                        (candidate.GetComp<CompEmbeddedWare>()?.EmbeddedPlateCount ?? -1))
                    .ToArray();
                throw new EndToEndAssertionException(
                    "The upstream cooking workflow did not reach observable native ingestion. " +
                    "Current job=" + (fixture.Cook.CurJobDef?.defName ?? "none") +
                    "; driver=" + (fixture.Cook.jobs.curDriver?.GetType().FullName ?? "none") +
                    "; covered meals=" + string.Join(",", covered) +
                    "; cookware spawned=" + fixture.Cookware.Spawned +
                    "; plate spawned=" + fixture.Plate.Spawned +
                    "; cutlery spawned=" + fixture.Cutlery.Spawned + ".");
            }
            return false;
        }

        EndToEndAssert.True(
            ReferenceEquals(fixture.JobTransitions.IngestMeal, meal),
            "The upstream driver must hand its exact newly cooked product to native Ingest.");
        EndToEndAssert.Equal(1, fixture.JobTransitions.EmbeddedPlateCount,
            "The upstream-produced meal must contain exactly one plate.");
        EndToEndAssert.True(
            ReferenceEquals(fixture.Plate, fixture.JobTransitions.EmbeddedPlate),
            "The newly cooked meal must contain the same physical reserved plate.");
        EndToEndAssert.Equal(1, fixture.JobTransitions.CulinaryServingCount,
            "The newly cooked one-serving meal must receive exactly one culinary record.");
        EndToEndAssert.Equal(0, fixture.BillCount,
            "Completing the one-off job must not create a persistent bill.");
        nativeIngestObserved = true;
        return true;
    }

    private void AssertCompletedLifecycle()
    {
        EndToEndAssert.True(customCookingObserved,
            "The upstream CFS_CookMealForSelf job must be observed before completion.");
        EndToEndAssert.True(activeCookwareObserved,
            "The exact cookware must be observed as the active work prop.");
        EndToEndAssert.True(nativeIngestObserved,
            "The upstream product must be observed in native Ingest before it disappears.");
        EndToEndAssert.Equal(1, fixture.JobTransitions.CustomCookingStartCount,
            "The ordinary self workflow must start exactly one CFS_CookMealForSelf job without a same-tick restart loop.");
        EndToEndAssert.True(meal?.Destroyed == true,
            "The exact upstream-produced meal must disappear only after native ingestion.");
        EndToEndAssert.True(fixture.Cookware.Spawned &&
                            fixture.Cookware.GetComp<CompSanitation>()!.IsDirty,
            "The same cookware must return to the map dirty after cooking.");
        EndToEndAssert.True(fixture.Plate.Spawned &&
                            fixture.Plate.GetComp<CompSanitation>()!.IsDirty,
            "The same embedded plate must return to the map dirty after eating.");
        EndToEndAssert.True(fixture.Cutlery.Spawned &&
                            fixture.Cutlery.GetComp<CompSanitation>()!.IsDirty,
            "The same acquired cutlery must return to the map dirty after eating.");
        EndToEndAssert.Equal(0, fixture.BillCount,
            "The completed upstream one-off workflow must leave no bill behind.");
        fixture.AssertWareUnits(meal);
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.cook-for-yourself-interruption",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "lordfelix.CookForYourself",
    "fumblesneeze.immersivechefs",
    MaxFrames = 4_800,
    MaxGameTicks = 18_000,
    MaxWallClockSeconds = 180)]
public sealed class CookForYourselfInterruptionTest : IRimWorldEndToEndTest
{
    private CookForYourselfFixture fixture = null!;

    public void Arrange(IEndToEndContext context)
    {
        fixture = CookForYourselfFixture.Create(context, "Interrupted one-off cook", hunger: 0.10f);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before the interruptible one-off cooking workflow",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the drafted interruptible one-off cook",
            new[] { fixture.Cook.ThingID },
            additive: false);
        var draft = fixture.RequiredDraftToggle(context, expectedCurrentState: true);
        yield return new GizmoActionStep(
            "undraft through the native toggle to begin interruptible cooking",
            new[] { fixture.Cook.ThingID },
            draft.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: draft.StableId);
        yield return new TimeControlActionStep(
            "run ordinary time until upstream cooking actually begins",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the upstream cooking toil dirties and renders the exact cookware",
            _ => fixture.Cook.CurJobDef?.defName == "CFS_CookMealForSelf" &&
                 CookingSessionRegistry.TryGetActiveWorkProp(
                     fixture.Cook,
                     out var cookware,
                     out var station) &&
                 ReferenceEquals(cookware, fixture.Cookware) &&
                 ReferenceEquals(station, fixture.Stove) &&
                 fixture.Cookware.GetComp<CompSanitation>()!.IsDirty,
            new EndToEndDeadline(1_800, 7_000, TimeSpan.FromSeconds(70)));
        yield return new SelectionActionStep(
            "select the cook during the interruptible upstream cooking toil",
            new[] { fixture.Cook.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the active interruptible one-off kitchen",
            new[] { fixture.Cook.ThingID, fixture.Stove.ThingID },
            paddingPixels: 180);
        yield return new ScreenshotStep(
            "observe active upstream cooking immediately before interruption",
            Array.Empty<string>(),
            paddingPixels: 0);

        draft = fixture.RequiredDraftToggle(context, expectedCurrentState: false);
        yield return new GizmoActionStep(
            "draft through the native toggle to interrupt upstream cooking",
            new[] { fixture.Cook.ThingID },
            draft.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: draft.StableId);
        yield return new WaitUntilStep(
            "native interruption returns used cookware and untouched service ware",
            _ => fixture.Cook.Drafted &&
                 fixture.Cook.CurJobDef?.defName != "CFS_CookMealForSelf" &&
                 fixture.Cookware.Spawned &&
                 fixture.Plate.Spawned &&
                 fixture.Cutlery.Spawned,
            new EndToEndDeadline(600, 1_500, TimeSpan.FromSeconds(25)));
        yield return new TimeControlActionStep(
            "pause after native one-off cooking interruption",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "interruption conserves exact ware with truthful sanitation",
            _ => AssertInterruptedLifecycle());
        yield return new SelectionActionStep(
            "select the exact dirty interrupted cookware",
            new[] { fixture.Cookware.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame exact ware after upstream cooking interruption",
            new[] { fixture.Cookware.ThingID, fixture.Plate.ThingID, fixture.Cutlery.ThingID },
            paddingPixels: 180);
        yield return new ScreenshotStep(
            "observe exact used cookware returned dirty after interruption",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new SelectionActionStep(
            "select the exact untouched plate after interruption",
            new[] { fixture.Plate.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "observe exact unused plate remains clean after interruption",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "Cook for Yourself interruption result",
            _ => new Dictionary<string, string>
            {
                ["cookDrafted"] = fixture.Cook.Drafted.ToString(),
                ["cookwareDirty"] = fixture.Cookware.GetComp<CompSanitation>()!.IsDirty.ToString(),
                ["plateDirty"] = fixture.Plate.GetComp<CompSanitation>()!.IsDirty.ToString(),
                ["cutleryDirty"] = fixture.Cutlery.GetComp<CompSanitation>()!.IsDirty.ToString(),
                ["nativeIngestObserved"] = fixture.JobTransitions.NativeIngestObserved.ToString()
            });
    }

    private void AssertInterruptedLifecycle()
    {
        EndToEndAssert.True(fixture.Cookware.GetComp<CompSanitation>()!.IsDirty,
            "Cookware used in the interrupted cooking toil must remain dirty.");
        EndToEndAssert.False(fixture.Plate.GetComp<CompSanitation>()!.IsDirty,
            "The collected but unused plate must return clean after interruption.");
        EndToEndAssert.False(fixture.Cutlery.GetComp<CompSanitation>()!.IsDirty,
            "Cutlery not yet acquired for dining must remain clean after interruption.");
        EndToEndAssert.False(fixture.JobTransitions.NativeIngestObserved,
            "Interrupting active cooking must not manufacture a meal or native ingestion transition.");
        EndToEndAssert.Equal(0, fixture.SpawnedCoveredMealCount,
            "Interrupted one-off cooking must leave no covered product on the map.");
        EndToEndAssert.Equal(0, fixture.BillCount,
            "Interrupted one-off cooking must leave no bill behind.");
        fixture.AssertWareUnits(meal: null);
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.cook-for-yourself-integration-off",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "lordfelix.CookForYourself",
    "fumblesneeze.immersivechefs",
    MaxFrames = 6_600,
    MaxGameTicks = 27_000,
    MaxWallClockSeconds = 260)]
public sealed class CookForYourselfIntegrationOffTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn cook = null!;
    private ThingWithComps stove = null!;
    private ThingWithComps loosePlate = null!;
    private CookForYourselfJobTransitionTrace transitions = null!;
    private ThingWithComps? meal;

    public void Arrange(IEndToEndContext context)
    {
        var settings = ImmersiveChefsMod.Settings;
        var priorIntegration = settings.CookForYourself;
        var priorWareMode = settings.WareRequirementMode;
        var priorEmergencyThreshold = settings.EmergencyHungerThreshold;
        context.DeferCleanup(() =>
        {
            settings.CookForYourself = priorIntegration;
            settings.WareRequirementMode = priorWareMode;
            settings.EmergencyHungerThreshold = priorEmergencyThreshold;
        });
        settings.CookForYourself = OptionalIntegrationMode.Off;
        settings.WareRequirementMode = WareRequirementMode.Strict;
        settings.EmergencyHungerThreshold = 0.05f;

        map = Current.Game.CurrentMap;
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);
        var cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        cook = CookForYourselfFixture.CreateCapableCook("Integration-off one-off cook", cooking);
        FoodSearchE2EFixture.SetHunger(cook, 0.10f);
        cook.workSettings.SetPriority(cooking, 1);
        GenSpawn.Spawn(cook, center + (IntVec3.South * 3), map);
        cook.drafter.Drafted = true;

        var stoveDef = DefDatabase<ThingDef>.GetNamed("FueledStove");
        stove = (ThingWithComps)ThingMaker.MakeThing(
            stoveDef,
            stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
        stove.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(stove, center, map, Rot4.North);
        stove.GetComp<CompRefuelable>()?.Refuel(999f);
        var rice = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RawRice"));
        rice.stackCount = 20;
        GenSpawn.Spawn(rice, center + (IntVec3.East * 2), map);
        loosePlate = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Plate",
            ThingDefOf.Steel);
        GenSpawn.Spawn(loosePlate, center + (IntVec3.West * 2), map);

        transitions = CookForYourselfJobTransitionTrace.Begin(cook);
        context.DeferCleanup(transitions.Dispose);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before the integration-off upstream workflow",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the drafted integration-off cook",
            new[] { cook.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the kitchen with service plate but no cookware",
            new[] { cook.ThingID, stove.ThingID, loosePlate.ThingID },
            paddingPixels: 190);
        yield return new ScreenshotStep(
            "observe the integration-off one-off cooking fixture",
            Array.Empty<string>(),
            paddingPixels: 0);
        var draft = RequiredDraftToggle(context, expectedCurrentState: true);
        yield return new GizmoActionStep(
            "undraft through the native toggle with Cook for Yourself integration off",
            new[] { cook.ThingID },
            draft.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: draft.StableId);
        yield return new TimeControlActionStep(
            "run the unmodified upstream one-off cooking workflow",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the integration-off upstream custom job starts without kitchenware",
            _ => cook.CurJobDef?.defName == "CFS_CookMealForSelf" &&
                 transitions.CustomCookingJobObserved,
            new EndToEndDeadline(1_800, 7_000, TimeSpan.FromSeconds(70)));
        yield return new ScreenshotStep(
            "observe upstream custom cooking remains usable with integration off",
            new[] { cook.ThingID, stove.ThingID },
            paddingPixels: 180);
        yield return new WaitUntilStep(
            "the integration-off product enters native ingestion",
            _ => ObserveUnmodifiedIngest(),
            new EndToEndDeadline(2_400, 10_000, TimeSpan.FromSeconds(90)));
        yield return new SelectionActionStep(
            "select the integration-off cook during native ingestion",
            new[] { cook.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "observe native ingestion of the unplated integration-off meal",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new TimeControlActionStep(
            "finish integration-off native ingestion",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the integration-off upstream meal is consumed",
            _ => meal?.Destroyed == true,
            new EndToEndDeadline(1_800, 7_000, TimeSpan.FromSeconds(65)));
        yield return new TimeControlActionStep(
            "pause after integration-off upstream dining",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "integration off preserves the upstream lifecycle without ware attachment",
            _ => AssertUnmodifiedLifecycle());
        yield return new CheckpointStep(
            "Cook for Yourself integration-off result",
            _ => new Dictionary<string, string>
            {
                ["customJobObserved"] = transitions.CustomCookingJobObserved.ToString(),
                ["nativeIngestObserved"] = transitions.NativeIngestObserved.ToString(),
                ["mealId"] = meal?.ThingID ?? "none",
                ["mealDestroyed"] = (meal?.Destroyed == true).ToString(),
                ["plateCountAtIngest"] = transitions.EmbeddedPlateCount.ToString(),
                ["culinaryServingCountAtIngest"] = transitions.CulinaryServingCount.ToString()
            });
    }

    private bool ObserveUnmodifiedIngest()
    {
        meal ??= transitions.IngestMeal;
        return transitions.NativeIngestObserved && meal is not null;
    }

    private void AssertUnmodifiedLifecycle()
    {
        EndToEndAssert.True(transitions.CustomCookingJobObserved,
            "The upstream custom cooking job must still run with the integration off.");
        EndToEndAssert.True(transitions.NativeIngestObserved && meal?.Destroyed == true,
            "The upstream product must still enter and complete native Ingest.");
        EndToEndAssert.Equal(0, transitions.EmbeddedPlateCount,
            "Integration Off must not attach a plate to the upstream product.");
        EndToEndAssert.Equal(1, transitions.CulinaryServingCount,
            "The ordinary covered-meal compatibility default must remain one serving; " +
            "Integration Off disables the optional cooking-session decoration, not the product mod's safe legacy-meal state.");
        EndToEndAssert.Equal(0, ((IBillGiver)stove).BillStack.Count,
            "The integration-off upstream workflow must remain bill-free.");
        EndToEndAssert.True(loosePlate.Spawned &&
                            loosePlate.GetComp<CompSanitation>()!.IsDirty,
            "Strict native dining must use and return the exact loose plate after optional cooking-session pass-through.");
        EndToEndAssert.Equal(1, loosePlate.stackCount,
            "Integration-off dining must conserve the exact loose plate as one physical unit.");
        EndToEndAssert.Equal(1,
            map.listerThings.AllThings.Count(thing =>
                thing.def.GetModExtension<KitchenwareExtension>() is not null),
            "The integration-off scenario must contain only the dining plate and no cookware that could satisfy a falsely active cooking adapter.");
    }

    private EndToEndGizmoOption RequiredDraftToggle(
        IEndToEndContext context,
        bool expectedCurrentState)
    {
        var options = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { cook.ThingID }, Array.Empty<string>())
            .Where(option =>
                !option.Disabled &&
                option.Interaction == EndToEndGizmoInteraction.Toggle &&
                option.ToggleState == expectedCurrentState &&
                string.Equals(option.HotKeyDefName, "Command_ColonistDraft", StringComparison.Ordinal))
            .ToArray();
        EndToEndAssert.Equal(1, options.Length,
            "The integration-off cook must expose one current native Draft toggle.");
        return options[0];
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.cook-for-yourself-baby-food-pass-through",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "ludeon.rimworld.biotech",
    "lordfelix.CookForYourself",
    "fumblesneeze.immersivechefs",
    MaxFrames = 7_200,
    MaxGameTicks = 28_000,
    MaxWallClockSeconds = 275)]
public sealed class CookForYourselfBabyFoodPassThroughTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn cook = null!;
    private Pawn baby = null!;
    private ThingWithComps stove = null!;
    private CookForYourselfJobTransitionTrace transitions = null!;
    private ThingWithComps? babyFood;
    private float initialBabyHunger;
    private int initialBabyFoodStackCount;
    private bool nativeCookingObserved;

    public void Arrange(IEndToEndContext context)
    {
        FoodSearchE2EFixture.UseStrictNonEmergencyDining(context);
        var settings = ImmersiveChefsMod.Settings;
        var priorIntegration = settings.CookForYourself;
        context.DeferCleanup(() => settings.CookForYourself = priorIntegration);
        settings.CookForYourself = OptionalIntegrationMode.Auto;

        EndToEndAssert.True(ModsConfig.BiotechActive,
            "The exact baby-food pass-through group must load Biotech.");
        EndToEndAssert.True(CookForYourselfAdapter.Enabled,
            "The exact supported Cook for Yourself adapter must be active in the Biotech group.");

        map = Current.Game.CurrentMap;
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);
        var cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        cook = CookForYourselfFixture.CreateCapableCook("Baby-food cook", cooking);
        FoodSearchE2EFixture.SetHunger(cook, 1f);
        cook.workSettings.SetPriority(cooking, 1);
        GenSpawn.Spawn(cook, center + (IntVec3.South * 3), map);
        cook.drafter.Drafted = true;

        baby = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            Faction.OfPlayer,
            forceGenerateNewPawn: true,
            allowDowned: true,
            canGeneratePawnRelations: false,
            fixedBiologicalAge: 1f,
            fixedChronologicalAge: 1f,
            developmentalStages: DevelopmentalStage.Baby,
            forceNoGear: true));
        HumanlikePawnFixture.SetName(baby, "Hungry baby recipient");
        FoodSearchE2EFixture.SetHunger(baby, 0.10f);
        initialBabyHunger = baby.needs!.food!.CurLevelPercentage;
        GenSpawn.Spawn(baby, center + (IntVec3.East * 3), map);

        var stoveDef = DefDatabase<ThingDef>.GetNamed("FueledStove");
        stove = (ThingWithComps)ThingMaker.MakeThing(
            stoveDef,
            stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
        stove.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(stove, center, map, Rot4.North);
        stove.GetComp<CompRefuelable>()?.Refuel(999f);
        var rice = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RawRice"));
        rice.stackCount = 20;
        GenSpawn.Spawn(rice, center + (IntVec3.East * 2), map);

        transitions = CookForYourselfJobTransitionTrace.Begin(cook);
        context.DeferCleanup(transitions.Dispose);
        AssertNoKitchenware();
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before the excluded baby-food workflow",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the drafted baby-food cook",
            new[] { cook.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the baby-food kitchen and recipient",
            new[] { cook.ThingID, baby.ThingID, stove.ThingID },
            paddingPixels: 210);
        yield return new ScreenshotStep(
            "observe the strict kitchen without any kitchenware before baby-food cooking",
            Array.Empty<string>(),
            paddingPixels: 0);

        var draft = RequiredDraftToggle(context, expectedCurrentState: true);
        yield return new GizmoActionStep(
            "undraft through the native colonist gizmo to release dependent baby-food cooking",
            new[] { cook.ThingID },
            draft.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: draft.StableId);
        yield return new TimeControlActionStep(
            "run the native excluded baby-food workflow",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "Cook for Yourself reaches its real baby-food cooking toil without a cooking session",
            _ => ObserveNativeExcludedCooking(),
            new EndToEndDeadline(2_100, 8_000, TimeSpan.FromSeconds(75)));
        yield return new SelectionActionStep(
            "select the cook during native excluded baby-food cooking",
            new[] { cook.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame native baby-food cooking at the stove",
            new[] { cook.ThingID, stove.ThingID },
            paddingPixels: 180);
        yield return new ScreenshotStep(
            "observe native baby-food cooking with no kitchenware work prop",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new WaitUntilStep(
            "the upstream excluded product enters native bottle feeding",
            _ => ObserveNativeBottleFeeding(),
            new EndToEndDeadline(2_400, 9_000, TimeSpan.FromSeconds(85)));
        yield return new SelectionActionStep(
            "select the cook during native bottle feeding",
            new[] { cook.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the native bottle-feeding action",
            new[] { cook.ThingID, baby.ThingID },
            paddingPixels: 170);
        yield return new ScreenshotStep(
            "observe the excluded baby food being fed through the native childcare job",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "finish native bottle feeding",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "native bottle feeding completes after the real baby gains nutrition",
            _ => baby.needs?.food is { } food &&
                 food.CurLevelPercentage > initialBabyHunger + 0.02f &&
                 !string.Equals(
                     cook.CurJobDef?.defName,
                     transitions.ExcludedDeliveryJobDefName,
                     StringComparison.Ordinal),
            new EndToEndDeadline(2_100, 8_000, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep(
            "pause after native excluded baby-food feeding",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "excluded baby food remains entirely owned by Cook for Yourself and Biotech",
            _ => AssertCompletedPassThrough());
        yield return new ScreenshotStep(
            "observe the completed native excluded-food lifecycle",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "Cook for Yourself excluded baby-food pass-through result",
            _ => new Dictionary<string, string>
            {
                ["customJobObserved"] = transitions.CustomCookingJobObserved.ToString(),
                ["customRecipientId"] = transitions.CustomCookingRecipient?.ThingID ?? "none",
                ["nativeCookingObserved"] = nativeCookingObserved.ToString(),
                ["excludedDeliveryObserved"] = transitions.ExcludedDeliveryObserved.ToString(),
                ["deliveryJob"] = transitions.ExcludedDeliveryJobDefName ?? "none",
                ["babyFoodId"] = babyFood?.ThingID ?? "none",
                ["babyFoodDef"] = babyFood?.def.defName ?? "none",
                ["initialBabyHunger"] = initialBabyHunger.ToString("0.###"),
                ["finalBabyHunger"] = baby.needs?.food?.CurLevelPercentage.ToString("0.###") ?? "none",
                ["initialBabyFoodStack"] = initialBabyFoodStackCount.ToString(),
                ["finalBabyFoodStack"] = babyFood is null || babyFood.Destroyed
                    ? "destroyed"
                    : babyFood.stackCount.ToString()
            });
    }

    private bool ObserveNativeExcludedCooking()
    {
        var currentToil = cook.jobs?.curDriver is { } driver
            ? Traverse.Create(driver).Property("CurToil").GetValue<Toil>()
            : null;
        if (cook.CurJobDef?.defName != CookForYourselfCompatibility.JobDefName ||
            !transitions.CustomCookingJobObserved ||
            !ReferenceEquals(transitions.CustomCookingRecipient, baby) ||
            currentToil?.debugName != "CookMealForSelf")
        {
            return false;
        }

        EndToEndAssert.False(CookingSessionRegistry.TryGetActiveWorkProp(
                cook,
                out _,
                out _),
            "Excluded baby-food cooking must not acquire or render Immersive Chefs kitchenware.");
        EndToEndAssert.Equal("Make_BabyFood", cook.CurJob.controlGroupTag,
            "The upstream custom job must retain the exact Biotech baby-food recipe tag.");
        AssertNoKitchenware();
        nativeCookingObserved = true;
        return true;
    }

    private bool ObserveNativeBottleFeeding()
    {
        babyFood ??= transitions.ExcludedFood;
        if (!transitions.ExcludedDeliveryObserved || babyFood is null)
        {
            return false;
        }

        EndToEndAssert.True(ReferenceEquals(transitions.ExcludedRecipient, baby),
            "Cook for Yourself must hand the excluded product to native childcare for the exact baby.");
        EndToEndAssert.Equal(JobDefOf.BottleFeedBaby.defName,
            transitions.ExcludedDeliveryJobDefName,
            "Cook for Yourself must hand the excluded product to Biotech's exact native BottleFeedBaby job.");
        EndToEndAssert.Equal("BabyFood", babyFood.def.defName,
            "The native excluded path must produce Biotech baby food.");
        EndToEndAssert.False(MealCoveragePolicy.IsCovered(babyFood.def),
            "Biotech baby food must remain excluded from Immersive Chefs meal coverage.");
        EndToEndAssert.True(babyFood.GetComp<CompEmbeddedWare>() is null,
            "Excluded baby food must not receive the embedded-plate component.");
        EndToEndAssert.True(babyFood.GetComp<CompCulinaryState>() is null,
            "Excluded baby food must not receive culinary serving state.");
        initialBabyFoodStackCount = babyFood.stackCount;
        AssertNoKitchenware();
        return true;
    }

    private void AssertCompletedPassThrough()
    {
        EndToEndAssert.True(nativeCookingObserved && transitions.CustomCookingJobObserved,
            "The native Cook for Yourself baby-food cooking job must be observed.");
        EndToEndAssert.True(transitions.ExcludedDeliveryObserved &&
                            ReferenceEquals(transitions.ExcludedRecipient, baby),
            "The native childcare delivery transition must target the exact baby.");
        EndToEndAssert.True(baby.needs?.food is { } food &&
                            food.CurLevelPercentage > initialBabyHunger + 0.02f,
            "The real baby must gain visible nutrition through native bottle feeding.");
        EndToEndAssert.Equal(0, ((IBillGiver)stove).BillStack.Count,
            "The excluded one-off baby-food workflow must remain bill-free.");
        EndToEndAssert.False(CookingSessionRegistry.TryGetActiveWorkProp(cook, out _, out _),
            "Excluded baby-food completion must leave no Immersive Chefs cooking session.");
        AssertNoKitchenware();
    }

    private void AssertNoKitchenware()
    {
        EndToEndAssert.Equal(0,
            map.listerThings.AllThings.Count(thing =>
                thing.def.GetModExtension<KitchenwareExtension>() is not null),
            "The baby-food pass-through fixture must never create or acquire kitchenware.");
    }

    private EndToEndGizmoOption RequiredDraftToggle(
        IEndToEndContext context,
        bool expectedCurrentState)
    {
        var options = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { cook.ThingID }, Array.Empty<string>())
            .Where(option =>
                !option.Disabled &&
                option.Interaction == EndToEndGizmoInteraction.Toggle &&
                option.ToggleState == expectedCurrentState &&
                string.Equals(option.HotKeyDefName, "Command_ColonistDraft", StringComparison.Ordinal))
            .ToArray();
        EndToEndAssert.Equal(1, options.Length,
            "The selected baby-food cook must expose one current native Draft toggle.");
        return options[0];
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.cook-for-yourself-conscious-patient",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "lordfelix.CookForYourself",
    "Mehni.PickUpAndHaul",
    "Andromeda.StackGap",
    "fumblesneeze.immersivechefs",
    MaxFrames = 9_000,
    MaxGameTicks = 34_000,
    MaxWallClockSeconds = 345)]
public sealed class CookForYourselfConsciousPatientTest : IRimWorldEndToEndTest
{
    private CookForYourselfFixture fixture = null!;
    private Pawn patient = null!;
    private Building_Bed bed = null!;
    private ThingWithComps? meal;
    private bool dependentCookingObserved;
    private bool activeCookwareObserved;
    private int dependentDiagnosticTick;
    private int feedDiagnosticTick;

    public void Arrange(IEndToEndContext context)
    {
        fixture = CookForYourselfFixture.Create(context, "Patient meal cook", hunger: 1f);
        var map = fixture.Map;
        var bedCell = fixture.Stove.Position + (IntVec3.North * 3);
        bed = (Building_Bed)ThingMaker.MakeThing(ThingDefOf.Bed, ThingDefOf.WoodLog);
        bed.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(bed, bedCell, map, Rot4.North);
        bed.Medical = true;

        patient = FoodSearchE2EFixture.CreateColonist("Conscious hungry patient");
        FoodSearchE2EFixture.SetHunger(patient, 0.10f);
        GenSpawn.Spawn(patient, fixture.Stove.Position + (IntVec3.East * 3), map);
        var torso = patient.health.hediffSet.GetNotMissingParts()
            .First(part => part.def == BodyPartDefOf.Torso);
        var injury = HediffMaker.MakeHediff(
            DefDatabase<HediffDef>.GetNamed("Crush"),
            patient,
            torso);
        injury.Severity = 0.5f;
        patient.health.AddHediff(injury);
        EndToEndAssert.True(HealthAIUtility.ShouldSeekMedicalRest(patient),
            "The conscious-patient fixture must genuinely require medical rest.");
        var layDown = JobMaker.MakeJob(JobDefOf.LayDown, bed);
        layDown.restUntilHealed = true;
        patient.jobs.StartJob(layDown, JobCondition.InterruptForced);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "allow the conscious patient to enter the medical bed",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the hungry conscious patient becomes eligible for native feeding",
            _ => patient.InBed() && patient.Awake() && FeedPatientUtility.ShouldBeFed(patient),
            new EndToEndDeadline(1_200, 4_500, TimeSpan.FromSeconds(50)));
        yield return new TimeControlActionStep(
            "pause before permitting dependent one-off cooking",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the drafted patient-meal cook",
            new[] { fixture.Cook.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the conscious-patient one-off kitchen",
            new[] { fixture.Cook.ThingID, patient.ThingID, fixture.Stove.ThingID },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "observe the conscious patient awaiting a freshly cooked meal",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "the dependent fixture is passive and bill-free before player action",
            _ => AssertPassivePatientFixture());

        var draft = fixture.RequiredDraftToggle(context, expectedCurrentState: true);
        yield return new GizmoActionStep(
            "undraft through the native colonist gizmo to permit dependent cooking",
            new[] { fixture.Cook.ThingID },
            draft.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: draft.StableId);
        yield return new AssertionStep(
            "the native undraft makes the upstream dependent think node eligible",
            _ => AssertUpstreamDependentEligibility());
        yield return new TimeControlActionStep(
            "run ordinary game time for the upstream dependent think node",
            paused: false,
            EndToEndGameSpeed.Normal);
        dependentDiagnosticTick = Find.TickManager.TicksGame + 600;
        yield return new WaitUntilStep(
            "upstream dependent cooking targets the exact patient and uses the exact kitchenware",
            _ => ObserveDependentCooking(),
            new EndToEndDeadline(1_800, 7_000, TimeSpan.FromSeconds(70)));
        yield return new SelectionActionStep(
            "select the cook during the upstream patient-meal job",
            new[] { fixture.Cook.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame upstream cooking for the conscious patient",
            new[] { fixture.Cook.ThingID, patient.ThingID, fixture.Stove.ThingID },
            paddingPixels: 210);
        yield return new ScreenshotStep(
            "observe exact kitchenware rendered during dependent one-off cooking",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "continue dependent cooking until native FeedPatient begins",
            paused: false,
            EndToEndGameSpeed.Normal);
        feedDiagnosticTick = Find.TickManager.TicksGame + 1_800;
        yield return new WaitUntilStep(
            "the upstream product enters native FeedPatient with its exact plate",
            _ => ObserveNativeFeedTransition(),
            new EndToEndDeadline(2_400, 10_000, TimeSpan.FromSeconds(90)));
        yield return new SelectionActionStep(
            "select the feeder and conscious patient during native feeding",
            new[] { fixture.Cook.ThingID, patient.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "observe upstream dependent cooking transition into native patient feeding",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "finish native conscious-patient feeding",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "native patient feeding returns the exact dirty setting",
            _ => meal?.Destroyed == true &&
                 fixture.Plate.Spawned &&
                 fixture.Cutlery.Spawned,
            new EndToEndDeadline(2_400, 10_000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep(
            "pause after conscious-patient feeding",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the dependent one-off meal and assisted dining lifecycle complete once",
            _ => AssertCompletedPatientLifecycle());

        yield return new SelectionActionStep(
            "select the exact returned patient plate",
            new[] { fixture.Plate.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame returned conscious-patient service ware",
            new[] { patient.ThingID, fixture.Cookware.ThingID, fixture.Plate.ThingID, fixture.Cutlery.ThingID },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "observe the exact dirty plate after native patient feeding",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new SelectionActionStep(
            "select the exact returned patient cutlery",
            new[] { fixture.Cutlery.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "observe the exact dirty cutlery after native patient feeding",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "Cook for Yourself conscious-patient result",
            _ => new Dictionary<string, string>
            {
                ["dependentCookingObserved"] = dependentCookingObserved.ToString(),
                ["activeCookwareObserved"] = activeCookwareObserved.ToString(),
                ["nativeFeedPatientObserved"] = fixture.JobTransitions.NativeFeedPatientObserved.ToString(),
                ["patientId"] = patient.ThingID,
                ["patientConscious"] = patient.Awake().ToString(),
                ["mealId"] = meal?.ThingID ?? "none",
                ["mealDestroyed"] = (meal?.Destroyed == true).ToString(),
                ["plateId"] = fixture.Plate.ThingID,
                ["cutleryId"] = fixture.Cutlery.ThingID
            });
    }

    private void AssertPassivePatientFixture()
    {
        EndToEndAssert.True(fixture.Cook.Drafted,
            "The patient-meal cook must remain drafted until the native player action.");
        EndToEndAssert.True(patient.InBed() && patient.Awake(),
            "The exact patient must be visibly conscious in the medical bed.");
        EndToEndAssert.True(FeedPatientUtility.ShouldBeFed(patient),
            "The exact hungry patient must remain eligible for native FeedPatient.");
        EndToEndAssert.Equal(0, fixture.BillCount,
            "The dependent workflow must begin without a bill.");
        EndToEndAssert.Equal(0, fixture.SpawnedCoveredMealCount,
            "The dependent cook must have no prepared covered meal to deliver.");
        AssertNoDiningMemories(fixture.Cook,
            "The cook must begin without patient-owned dining memories.");
        AssertNoDiningMemories(patient,
            "The patient must begin without fixture dining memories.");
        fixture.AssertCleanWareUnits(meal: null);
    }

    private bool ObserveDependentCooking()
    {
        if (fixture.Cook.CurJobDef?.defName != "CFS_CookMealForSelf" ||
            !fixture.JobTransitions.CustomCookingJobObserved ||
            !ReferenceEquals(fixture.JobTransitions.CustomCookingRecipient, patient))
        {
            if (dependentDiagnosticTick > 0 && Find.TickManager.TicksGame >= dependentDiagnosticTick)
            {
                throw new EndToEndAssertionException(
                    "The eligible upstream dependent node did not enter its custom job. " +
                    "Current job=" + (fixture.Cook.CurJobDef?.defName ?? "none") +
                    "; upstream invocations=" + fixture.JobTransitions.DependentInvocationCount +
                    "; upstream proposed jobs=" + fixture.JobTransitions.DependentProposedJobCount +
                    "; final surviving jobs=" + fixture.JobTransitions.DependentFinalJobCount +
                    "; patient should be fed=" + FeedPatientUtility.ShouldBeFed(patient) +
                    "; adapter enabled=" + CookForYourselfAdapter.Enabled + ".");
            }
            return false;
        }

        dependentCookingObserved = true;
        if (!CookingSessionRegistry.TryGetActiveWorkProp(
                fixture.Cook,
                out var activeCookware,
                out var activeStation))
        {
            return false;
        }

        EndToEndAssert.True(ReferenceEquals(fixture.Cookware, activeCookware),
            "Dependent one-off cooking must render the exact reserved cookware.");
        EndToEndAssert.True(ReferenceEquals(fixture.Stove, activeStation),
            "Dependent one-off cooking must remain bound to the upstream-selected station.");
        fixture.AssertIngredientsPlacedBeforeWarePickup();
        EndToEndAssert.True(fixture.Cookware.GetComp<CompSanitation>()!.IsDirty,
            "Actual dependent cooking must dirty the exact cookware.");
        EndToEndAssert.False(fixture.Plate.GetComp<CompSanitation>()!.IsDirty,
            "The patient plate must remain clean until feeding.");
        activeCookwareObserved = true;
        return true;
    }

    private void AssertUpstreamDependentEligibility()
    {
        EndToEndAssert.False(fixture.Cook.Drafted,
            "The native Draft toggle must actually undraft the patient-meal cook.");
        EndToEndAssert.True(patient.InBed() && patient.Awake() && FeedPatientUtility.ShouldBeFed(patient),
            "The conscious patient must remain eligible when the dependent think node is released.");
        var type = AccessTools.TypeByName("CookForYourself.JobGiver_CookMealForDependent");
        var getPriority = type is null
            ? null
            : AccessTools.Method(type, "GetPriority", new[] { typeof(Pawn) });
        EndToEndAssert.NotNull(getPriority,
            "The exact supported dependent job giver must expose its public priority seam.");
        var node = Activator.CreateInstance(type!);
        var priority = (float)getPriority!.Invoke(node, new object[] { fixture.Cook });
        EndToEndAssert.True(priority > 0f,
            "The upstream dependent think node must report a positive priority for the visible patient fixture; " +
            "observed " + priority.ToString("0.###") + ".");

        var selfGiverType = AccessTools.TypeByName("CookForYourself.JobGiver_CookMealForSelf");
        var findPlan = selfGiverType?.GetMethod(
            "FindBestMealPlan",
            System.Reflection.BindingFlags.Static |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic,
            binder: null,
            types: new[] { typeof(Pawn), typeof(Pawn), typeof(bool) },
            modifiers: null);
        EndToEndAssert.NotNull(findPlan,
            "The supported upstream build must expose its exact meal-planning seam.");
        var plan = findPlan!.Invoke(null, new object[] { fixture.Cook, patient, false });
        EndToEndAssert.NotNull(plan,
            "The upstream planner must find a real station/recipe/ingredient plan for the visible patient fixture.");
    }

    private bool ObserveNativeFeedTransition()
    {
        var transitions = fixture.JobTransitions;
        meal ??= transitions.FeedPatientMeal;
        if (!transitions.NativeFeedPatientObserved || meal is null)
        {
            if (feedDiagnosticTick > 0 && Find.TickManager.TicksGame >= feedDiagnosticTick)
            {
                var covered = fixture.Map.listerThings.AllThings
                    .Where(thing => MealCoveragePolicy.IsCovered(thing.def))
                    .Select(thing => thing.ThingID + ":" + thing.def.defName)
                    .ToArray();
                throw new EndToEndAssertionException(
                    "Dependent cooking did not reach native FeedPatient. Current cook job=" +
                    (fixture.Cook.CurJobDef?.defName ?? "none") +
                    "; observed cook jobs=" + string.Join(",", transitions.ObservedCookJobDefs) +
                    "; patient in bed=" + patient.InBed() +
                    "; patient awake=" + patient.Awake() +
                    "; patient hunger=" +
                    (patient.needs?.food?.CurLevelPercentage.ToString("0.###") ?? "none") +
                    "; patient should be fed=" + FeedPatientUtility.ShouldBeFed(patient) +
                    "; covered meals=" + string.Join(",", covered) + ".");
            }
            return false;
        }

        EndToEndAssert.True(ReferenceEquals(patient, transitions.FeedPatient),
            "The upstream driver must hand its product to native FeedPatient for the exact recipient.");
        EndToEndAssert.True(ReferenceEquals(fixture.Plate, transitions.FeedPatientEmbeddedPlate),
            "The patient meal must contain the same physical reserved plate.");
        EndToEndAssert.Equal(1, transitions.FeedPatientEmbeddedPlateCount,
            "The patient meal must contain exactly one plate.");
        EndToEndAssert.Equal(1, transitions.FeedPatientCulinaryServingCount,
            "The one-serving patient meal must contain exactly one culinary record.");
        EndToEndAssert.Equal(0, fixture.BillCount,
            "The dependent one-off job must not create a bill.");
        return true;
    }

    private void AssertCompletedPatientLifecycle()
    {
        EndToEndAssert.True(dependentCookingObserved && activeCookwareObserved,
            "The exact upstream dependent job and active cookware must both be observed.");
        EndToEndAssert.True(fixture.JobTransitions.NativeFeedPatientObserved,
            "The upstream product must visibly enter native FeedPatient.");
        EndToEndAssert.Equal(1, fixture.JobTransitions.CustomCookingStartCount,
            "The ordinary dependent workflow must start exactly one CFS_CookMealForSelf job without a same-tick restart loop.");
        EndToEndAssert.True(meal?.Destroyed == true,
            "The exact freshly cooked patient meal must be consumed through native feeding.");
        EndToEndAssert.True(patient.Awake(),
            "The patient must remain conscious for the assisted-dining workflow.");
        EndToEndAssert.True(fixture.Cookware.Spawned &&
                            fixture.Cookware.GetComp<CompSanitation>()!.IsDirty,
            "The exact cookware must return dirty after dependent cooking.");
        EndToEndAssert.True(fixture.Plate.Spawned &&
                            fixture.Plate.GetComp<CompSanitation>()!.IsDirty,
            "The exact patient plate must return dirty after feeding.");
        EndToEndAssert.True(fixture.Cutlery.Spawned &&
                            fixture.Cutlery.GetComp<CompSanitation>()!.IsDirty,
            "The feeder must return the exact cutlery dirty after native feeding.");
        EndToEndAssert.Equal(0, fixture.BillCount,
            "The completed dependent workflow must leave no bill behind.");
        EndToEndAssert.Equal(1, MemoryCount(patient, "ImmersiveChefs_CulinaryQuality"),
            "The conscious patient must receive the freshly cooked meal's culinary-quality outcome.");
        EndToEndAssert.Equal(1, MemoryCount(patient, "ImmersiveChefs_MealTemperature"),
            "The conscious patient must receive the freshly cooked meal's thermal outcome.");
        EndToEndAssert.Equal(1, MemoryCount(patient, "ImmersiveChefs_DiningExperience"),
            "The conscious patient must own the assisted dining outcome.");
        AssertNoDiningMemories(fixture.Cook,
            "The feeder/cook must never receive the patient's dining outcomes.");
        fixture.AssertWareUnits(meal);
    }

    private static void AssertNoDiningMemories(Pawn pawn, string message)
    {
        var count = MemoryCount(pawn, "ImmersiveChefs_CulinaryQuality") +
                    MemoryCount(pawn, "ImmersiveChefs_MealTemperature") +
                    MemoryCount(pawn, "ImmersiveChefs_DiningExperience");
        EndToEndAssert.Equal(0, count, message);
    }

    private static int MemoryCount(Pawn pawn, string defName)
    {
        var def = DefDatabase<ThoughtDef>.GetNamed(defName);
        return pawn.needs?.mood?.thoughts?.memories.Memories
                   .Count(memory => memory.def == def) ?? 0;
    }
}

internal sealed class CookForYourselfFixture
{
    private CookForYourselfFixture(
        Map map,
        Pawn cook,
        ThingWithComps stove,
        ThingWithComps cookware,
        ThingWithComps plate,
        ThingWithComps cutlery,
        CookForYourselfJobTransitionTrace jobTransitions)
    {
        Map = map;
        Cook = cook;
        Stove = stove;
        Cookware = cookware;
        Plate = plate;
        Cutlery = cutlery;
        JobTransitions = jobTransitions;
    }

    internal Map Map { get; }
    internal Pawn Cook { get; }
    internal ThingWithComps Stove { get; }
    internal ThingWithComps Cookware { get; }
    internal ThingWithComps Plate { get; }
    internal ThingWithComps Cutlery { get; }
    internal CookForYourselfJobTransitionTrace JobTransitions { get; }
    internal string[] VisibleFixtureIds =>
        new[] { Cook.ThingID, Stove.ThingID, Cookware.ThingID, Plate.ThingID, Cutlery.ThingID };
    internal int BillCount => ((IBillGiver)Stove).BillStack.Count;
    internal int SpawnedCoveredMealCount => Map.listerThings.AllThings.Count(thing =>
        thing.Spawned && MealCoveragePolicy.IsCovered(thing.def));

    internal static CookForYourselfFixture Create(
        IEndToEndContext context,
        string cookName,
        float hunger)
    {
        FoodSearchE2EFixture.UseStrictNonEmergencyDining(context);
        var settings = ImmersiveChefsMod.Settings;
        var priorIntegration = settings.CookForYourself;
        context.DeferCleanup(() => settings.CookForYourself = priorIntegration);
        settings.CookForYourself = OptionalIntegrationMode.Auto;

        var map = Current.Game.CurrentMap;
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);
        var cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        var cook = CreateCapableCook(cookName, cooking);
        FoodSearchE2EFixture.SetHunger(cook, hunger);
        cook.workSettings.SetPriority(cooking, 1);
        GenSpawn.Spawn(cook, center + (IntVec3.South * 3), map);
        cook.drafter.Drafted = true;

        var stoveDef = DefDatabase<ThingDef>.GetNamed("FueledStove");
        var stove = (ThingWithComps)ThingMaker.MakeThing(
            stoveDef,
            stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
        stove.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(stove, center, map, Rot4.North);
        stove.GetComp<CompRefuelable>()?.Refuel(999f);

        var rice = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RawRice"));
        rice.stackCount = 20;
        GenSpawn.Spawn(rice, center + (IntVec3.East * 2), map);

        var cookware = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cookware",
            ThingDefOf.Steel);
        var plate = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Plate",
            ThingDefOf.Steel);
        var cutlery = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cutlery",
            ThingDefOf.Steel);
        GenSpawn.Spawn(cookware, center + (IntVec3.West * 3), map);
        GenSpawn.Spawn(plate, center + (IntVec3.West * 2), map);
        GenSpawn.Spawn(cutlery, center + IntVec3.West, map);

        var jobTransitions = CookForYourselfJobTransitionTrace.Begin(cook);
        context.DeferCleanup(jobTransitions.Dispose);

        EndToEndAssert.True(CookForYourselfAdapter.Enabled,
            "The exact supported Cook for Yourself adapter must be active in this group.");
        return new CookForYourselfFixture(
            map,
            cook,
            stove,
            cookware,
            plate,
            cutlery,
            jobTransitions);
    }

    internal EndToEndGizmoOption RequiredDraftToggle(
        IEndToEndContext context,
        bool expectedCurrentState)
    {
        var options = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { Cook.ThingID }, Array.Empty<string>())
            .Where(option =>
                !option.Disabled &&
                option.Interaction == EndToEndGizmoInteraction.Toggle &&
                option.ToggleState == expectedCurrentState &&
                string.Equals(option.HotKeyDefName, "Command_ColonistDraft", StringComparison.Ordinal))
            .ToArray();
        EndToEndAssert.Equal(1, options.Length,
            "The selected self-service cook must expose one current native Draft toggle.");
        return options[0];
    }

    internal void AssertCleanWareUnits(ThingWithComps? meal)
    {
        AssertWareUnits(meal);
        EndToEndAssert.False(Cookware.GetComp<CompSanitation>()!.IsDirty,
            "The arranged cookware must begin clean.");
        EndToEndAssert.False(Plate.GetComp<CompSanitation>()!.IsDirty,
            "The arranged plate must begin clean.");
        EndToEndAssert.False(Cutlery.GetComp<CompSanitation>()!.IsDirty,
            "The arranged cutlery must begin clean.");
    }

    internal void AssertWareUnits(ThingWithComps? meal)
    {
        EndToEndAssert.Equal(1, CountUnits(Cookware.def, meal),
            "One-off cooking must conserve exactly one physical cookware unit.");
        EndToEndAssert.Equal(1, CountUnits(Plate.def, meal),
            "One-off cooking and dining must conserve exactly one physical plate unit.");
        EndToEndAssert.Equal(1, CountUnits(Cutlery.def, meal),
            "One-off dining must conserve exactly one physical cutlery unit.");
        EndToEndAssert.Equal(1, Cookware.stackCount,
            "The exact cookware fixture must remain one unit.");
        EndToEndAssert.Equal(1, Plate.stackCount,
            "The exact plate fixture must remain one unit.");
        EndToEndAssert.Equal(1, Cutlery.stackCount,
            "The exact cutlery fixture must remain one unit.");
    }

    internal void AssertIngredientsPlacedBeforeWarePickup()
    {
        var job = Cook.CurJob;
        EndToEndAssert.NotNull(job,
            "The cook must still own the upstream custom job while its active cooking toil is observed.");
        EndToEndAssert.True(JobTransitions.InitialIngredientTargetCount > 0,
            "The traced upstream job must begin with at least one exact ingredient target.");
        EndToEndAssert.Equal(0, job!.GetTargetQueue(TargetIndex.B)?.Count ?? 0,
            "Every upstream ingredient target must be extracted and placed before reserved ware pickup finishes.");
        EndToEndAssert.Equal(0, job.countQueue?.Count ?? 0,
            "Every upstream ingredient count must be consumed by placement before active cooking begins.");
        EndToEndAssert.True(Cook.carryTracker.CarriedThing is null,
            "The cook must return to the station without an ingredient or ware stranded in the carry tracker.");
    }

    private int CountUnits(ThingDef def, ThingWithComps? meal)
    {
        var spawned = Map.listerThings.ThingsOfDef(def).Sum(thing => thing.stackCount);
        var held = (Cook.inventory?.innerContainer
                        .Where(thing => thing.def == def)
                        .Sum(thing => thing.stackCount) ?? 0) +
                   (Cook.carryTracker?.CarriedThing is { } carried && carried.def == def
                       ? carried.stackCount
                       : 0);
        var embedded = def == Plate.def && meal is { Destroyed: false }
            ? meal.GetComp<CompEmbeddedWare>()?.EmbeddedPlateCount ?? 0
            : 0;
        return spawned + held + embedded;
    }

    internal static Pawn CreateCapableCook(string name, WorkTypeDef cooking)
    {
        for (var attempt = 0; attempt < 96; attempt++)
        {
            var pawn = FoodSearchE2EFixture.CreateColonist(name);
            if (!pawn.WorkTypeIsDisabled(cooking) &&
                pawn.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation) &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) >= 0.9f)
            {
                pawn.skills.GetSkill(SkillDefOf.Cooking).Level = 20;
                if (pawn.needs?.rest is { } rest)
                {
                    rest.CurLevelPercentage = 1f;
                }
                return pawn;
            }

            pawn.Destroy(DestroyMode.Vanish);
        }

        throw new EndToEndAssertionException("Could not generate a capable Cook for Yourself pawn.");
    }
}

internal sealed class CookForYourselfJobTransitionTrace : IDisposable
{
    private const string HarmonyOwner =
        "fumblesneeze.immersivechefs.e2e.cook-for-yourself-job-transition";
    private static CookForYourselfJobTransitionTrace? active;
    private readonly Pawn cook;
    private readonly Harmony harmony;
    private readonly List<string> observedCookJobDefs = new();

    private CookForYourselfJobTransitionTrace(Pawn cook)
    {
        this.cook = cook;
        harmony = new Harmony(HarmonyOwner);
    }

    internal bool NativeIngestObserved { get; private set; }
    internal ThingWithComps? IngestMeal { get; private set; }
    internal Thing? EmbeddedPlate { get; private set; }
    internal int EmbeddedPlateCount { get; private set; } = -1;
    internal int CulinaryServingCount { get; private set; } = -1;
    internal bool NativeFeedPatientObserved { get; private set; }
    internal ThingWithComps? FeedPatientMeal { get; private set; }
    internal Pawn? FeedPatient { get; private set; }
    internal Thing? FeedPatientEmbeddedPlate { get; private set; }
    internal int FeedPatientEmbeddedPlateCount { get; private set; } = -1;
    internal int FeedPatientCulinaryServingCount { get; private set; } = -1;
    internal int DependentInvocationCount { get; private set; }
    internal int DependentProposedJobCount { get; private set; }
    internal int DependentFinalJobCount { get; private set; }
    internal int CustomCookingStartCount { get; private set; }
    internal int InitialIngredientTargetCount { get; private set; }
    internal bool CustomCookingJobObserved { get; private set; }
    internal Pawn? CustomCookingRecipient { get; private set; }
    internal bool ExcludedDeliveryObserved { get; private set; }
    internal ThingWithComps? ExcludedFood { get; private set; }
    internal Pawn? ExcludedRecipient { get; private set; }
    internal string? ExcludedDeliveryJobDefName { get; private set; }
    internal IReadOnlyList<string> ObservedCookJobDefs => observedCookJobDefs;

    internal static CookForYourselfJobTransitionTrace Begin(Pawn cook)
    {
        if (active is not null)
        {
            throw new EndToEndAssertionException(
                "Only one Cook for Yourself transition trace may be active in a sequential E2E group.");
        }

        var trace = new CookForYourselfJobTransitionTrace(cook);
        var startJob = AccessTools.Method(
            typeof(Pawn_JobTracker),
            nameof(Pawn_JobTracker.StartJob),
            new[]
            {
                typeof(Job),
                typeof(JobCondition),
                typeof(ThinkNode),
                typeof(bool),
                typeof(bool),
                typeof(ThinkTreeDef),
                typeof(JobTag?),
                typeof(bool),
                typeof(bool),
                typeof(bool?),
                typeof(bool),
                typeof(bool),
                typeof(bool)
            });
        EndToEndAssert.NotNull(startJob,
            "The supported RimWorld build must expose the exact native StartJob transition seam.");
        active = trace;
        try
        {
            trace.harmony.Patch(
                startJob,
                prefix: new HarmonyMethod(
                    typeof(CookForYourselfJobTransitionTrace),
                    nameof(StartJobPrefix)));
            var dependentType = AccessTools.TypeByName(
                CookForYourselfCompatibility.DependentJobGiverTypeName);
            var dependentTryGiveJob = AccessTools.DeclaredMethod(
                dependentType,
                "TryGiveJob",
                new[] { typeof(Pawn) });
            EndToEndAssert.NotNull(dependentTryGiveJob,
                "The exact supported dependent job giver must expose TryGiveJob for passive transition tracing.");
            var finalizer = new HarmonyMethod(
                typeof(CookForYourselfJobTransitionTrace),
                nameof(DependentFinalizer))
            {
                priority = Priority.Last,
                after = new[] { ImmersiveChefsMod.PackageId }
            };
            trace.harmony.Patch(
                dependentTryGiveJob,
                prefix: new HarmonyMethod(
                    typeof(CookForYourselfJobTransitionTrace),
                    nameof(DependentPrefix)),
                postfix: new HarmonyMethod(
                    typeof(CookForYourselfJobTransitionTrace),
                    nameof(DependentPostfix)),
                finalizer: finalizer);
        }
        catch
        {
            trace.harmony.UnpatchAll(HarmonyOwner);
            active = null;
            throw;
        }
        return trace;
    }

    public void Dispose()
    {
        harmony.UnpatchAll(HarmonyOwner);
        if (ReferenceEquals(active, this))
        {
            active = null;
        }
    }

    private static void StartJobPrefix(Pawn_JobTracker __instance, Job newJob)
    {
        var trace = active;
        if (trace is null)
        {
            return;
        }

        var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
        if (!ReferenceEquals(pawn, trace.cook))
        {
            return;
        }

        if (trace.observedCookJobDefs.Count < 32)
        {
            trace.observedCookJobDefs.Add(newJob.def.defName);
        }

        if (newJob.def.defName == CookForYourselfCompatibility.JobDefName)
        {
            trace.CustomCookingStartCount++;
            if (trace.CustomCookingStartCount == 1)
            {
                trace.InitialIngredientTargetCount =
                    newJob.GetTargetQueue(TargetIndex.B)?.Count ?? 0;
            }
            trace.CustomCookingJobObserved = true;
            trace.CustomCookingRecipient = newJob.GetTarget(TargetIndex.C).Pawn;
            return;
        }

        if (!trace.ExcludedDeliveryObserved)
        {
            var targets = new[]
            {
                newJob.GetTarget(TargetIndex.A),
                newJob.GetTarget(TargetIndex.B),
                newJob.GetTarget(TargetIndex.C)
            };
            var excludedFood = targets
                .Select(target => target.Thing)
                .OfType<ThingWithComps>()
                .FirstOrDefault(thing =>
                    thing.def.IsNutritionGivingIngestible &&
                    !MealCoveragePolicy.IsCovered(thing.def));
            var excludedRecipient = targets
                .Select(target => target.Pawn)
                .FirstOrDefault(candidate => candidate is not null &&
                                             !ReferenceEquals(candidate, trace.cook));
            if (excludedFood is not null && excludedRecipient is not null)
            {
                trace.ExcludedDeliveryObserved = true;
                trace.ExcludedFood = excludedFood;
                trace.ExcludedRecipient = excludedRecipient;
                trace.ExcludedDeliveryJobDefName = newJob.def.defName;
            }
        }

        if ((newJob.def != JobDefOf.Ingest && newJob.def != JobDefOf.FeedPatient) ||
            newJob.GetTarget(TargetIndex.A).Thing is not ThingWithComps meal ||
            !MealCoveragePolicy.IsCovered(meal.def))
        {
            return;
        }

        var embedded = meal.GetComp<CompEmbeddedWare>();
        var plate = embedded?.PeekPlateThing();
        var plateCount = embedded?.EmbeddedPlateCount ?? -1;
        var servingCount = meal.GetComp<CompCulinaryState>()?.Servings.Count ?? -1;
        if (newJob.def == JobDefOf.Ingest)
        {
            trace.NativeIngestObserved = true;
            trace.IngestMeal = meal;
            trace.EmbeddedPlate = plate;
            trace.EmbeddedPlateCount = plateCount;
            trace.CulinaryServingCount = servingCount;
            return;
        }

        trace.NativeFeedPatientObserved = true;
        trace.FeedPatientMeal = meal;
        trace.FeedPatient = newJob.GetTarget(TargetIndex.B).Pawn;
        trace.FeedPatientEmbeddedPlate = plate;
        trace.FeedPatientEmbeddedPlateCount = plateCount;
        trace.FeedPatientCulinaryServingCount = servingCount;
    }

    private static void DependentPrefix(Pawn __0)
    {
        var trace = active;
        if (trace is not null && ReferenceEquals(trace.cook, __0))
        {
            trace.DependentInvocationCount++;
        }
    }

    private static void DependentPostfix(Pawn __0, Job? __result)
    {
        var trace = active;
        if (trace is not null && ReferenceEquals(trace.cook, __0) && __result is not null)
        {
            trace.DependentProposedJobCount++;
        }
    }

    private static Exception? DependentFinalizer(Pawn __0, Job? __result, Exception? __exception)
    {
        var trace = active;
        if (trace is not null && ReferenceEquals(trace.cook, __0) && __result is not null)
        {
            trace.DependentFinalJobCount++;
        }
        return __exception;
    }
}
