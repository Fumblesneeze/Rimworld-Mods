using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.microwave-shared-native-dining",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "fumblesneeze.immersivechefs",
    MaxFrames = 7_200,
    MaxGameTicks = 18_000,
    MaxWallClockSeconds = 180)]
public sealed class MicrowaveSharedNativeDiningTest : IRimWorldEndToEndTest
{
    private readonly MicrowaveSharingWorkflow workflow = new(false, false);
    public void Arrange(IEndToEndContext context) => workflow.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => workflow.Execute(context);
}

[RimWorldEndToEndTest("immersive-chefs.microwave-user-interrupted",
    "fumblesneeze.immersivechefs", "brrainz.harmony", EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions", "fumblesneeze.immersivechefs",
    MaxFrames = 7_200, MaxGameTicks = 18_000, MaxWallClockSeconds = 180)]
public sealed class MicrowaveUserInterruptedTest : IRimWorldEndToEndTest
{
    private readonly MicrowaveSharingWorkflow workflow = new(true, false);
    public void Arrange(IEndToEndContext context) => workflow.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => workflow.Execute(context);
}

[RimWorldEndToEndTest("immersive-chefs.microwave-queue-power-loss",
    "fumblesneeze.immersivechefs", "brrainz.harmony", EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions", "fumblesneeze.immersivechefs",
    MaxFrames = 7_200, MaxGameTicks = 18_000, MaxWallClockSeconds = 180)]
public sealed class MicrowaveQueuePowerLossTest : IRimWorldEndToEndTest
{
    private readonly MicrowaveSharingWorkflow workflow = new(false, true);
    public void Arrange(IEndToEndContext context) => workflow.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => workflow.Execute(context);
}

[RimWorldEndToEndTest("immersive-chefs.microwave-queue-save-load",
    "fumblesneeze.immersivechefs", "brrainz.harmony", EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions", "fumblesneeze.immersivechefs",
    MaxFrames = 7_200, MaxGameTicks = 18_000, MaxWallClockSeconds = 180)]
public sealed class MicrowaveQueueSaveLoadTest : IRimWorldEndToEndTest
{
    private readonly MicrowaveSharingWorkflow workflow = new(false, false, true);
    public void Arrange(IEndToEndContext context) => workflow.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => workflow.Execute(context);
}

internal sealed class MicrowaveSharingWorkflow
{
    private const int HeatingTicks = 900;
    private CountertopMicrowaveDiningFixture fixture = null!;
    private Pawn secondDiner = null!;
    private ThingWithComps secondMeal = null!;
    private ThingWithComps secondPlate = null!;
    private ThingWithComps secondCutlery = null!;
    private ThingWithComps stove = null!;
    private ThingWithComps campfire = null!;
    private Job firstJob = null!;
    private Job secondJob = null!;
    private int secondStartedTick;
    private readonly bool interruptUser;
    private readonly bool losePower;
    private readonly bool saveWhileBusy;
    private ThingWithComps generator = null!;
    private Pawn powerOperator = null!;

    internal MicrowaveSharingWorkflow(bool interruptUser, bool losePower, bool saveWhileBusy = false)
    {
        this.interruptUser = interruptUser;
        this.losePower = losePower;
        this.saveWhileBusy = saveWhileBusy;
    }

    public void Arrange(IEndToEndContext context)
    {
        fixture = CountertopMicrowaveDiningFixture.Create(
            context, "First Microwave Diner", HeatingTicks, mealInDinerInventory: true);
        secondDiner = FoodSearchE2EFixture.CreateColonist("Waiting Microwave Diner");
        GenSpawn.Spawn(secondDiner, fixture.Diner.Position + new IntVec3(0, 0, -4), fixture.Map);
        FoodSearchE2EFixture.SetHunger(secondDiner, 0.10f);
        secondDiner.drafter.Drafted = true;
        // Load recovery may drop both carried meals on the shared interaction cell.
        // Distinct meal Defs keep native stack merging from invalidating per-Thing identity assertions.
        secondMeal = FoodSearchE2EFixture.MakePlatedMeal(
            saveWhileBusy ? ThingDefOf.MealFine : ThingDefOf.MealSimple, ThingDefOf.Steel, out secondPlate);
        secondMeal.GetComp<CompCulinaryState>().ReplaceServings(new[]
        {
            new CulinaryServingRecord(80, -5f, ContaminationSources.None, 0, Find.TickManager.TicksGame)
        });
        EndToEndAssert.True(secondDiner.inventory.innerContainer.TryAdd(secondMeal, false),
            "The waiting diner must hold its own exact cold meal.");
        secondCutlery = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Cutlery", ThingDefOf.Steel);
        GenSpawn.Spawn(secondCutlery, secondDiner.Position + new IntVec3(1, 0, 0), fixture.Map);
        stove = SpawnFueledSource("FueledStove", fixture.SupportPosition + new IntVec3(-6, 0, -3));
        campfire = SpawnFueledSource("Campfire", fixture.SupportPosition + new IntVec3(-2, 0, -5));
        EndToEndAssert.True(MealHeatingSource.TryCreate(stove)?.IsOperational == true &&
                               MealHeatingSource.TryCreate(campfire)?.IsOperational == true,
            "Both lower-tier alternatives must be usable so reservation fallback is observable.");
        if (losePower)
        {
            var powerCell = fixture.Map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("VanometricPowerCell")).Single();
            var generatorPosition = powerCell.Position;
            powerCell.Destroy();
            generator = SpawnFueledSource("WoodFiredGenerator", generatorPosition);
            DispenserE2EFixture.SettlePower(fixture.Map, new[] { fixture.Microwave }, 300);
            powerOperator = FoodSearchE2EFixture.CreateColonist("Microwave Power Operator");
            powerOperator.workSettings.SetPriority(DefDatabase<WorkTypeDef>.GetNamed("BasicWorker"), 1);
            for (var hour = 0; hour < 24; hour++)
                powerOperator.timetable.SetAssignment(hour, TimeAssignmentDefOf.Work);
            GenSpawn.Spawn(powerOperator, generatorPosition + new IntVec3(-2, 0, 0), fixture.Map);
        }
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return Screenshot("two hungry diners and three usable heating sources");
        yield return fixture.ToggleDraft(context, "release the first hungry diner", true);
        yield return Run("run the first native food search");
        yield return Wait("first diner selects microwave while still approaching", () =>
            fixture.Diner.CurJobDef == JobDefOf.Ingest &&
            DiningSessionRegistry.MicrowaveFor(fixture.Diner.CurJob) is not null);
        yield return new AssertionStep("microwave has no advance dining reservation", _ =>
        {
            firstJob = fixture.Diner.CurJob;
            EndToEndAssert.True(!fixture.Map.reservationManager.IsReserved(fixture.Microwave),
                "Selecting a microwave must not reserve it during approach or for the whole dining job.");
            EndToEndAssert.True(fixture.Diner.Position != fixture.Microwave.InteractionCell &&
                                   !fixture.Map.physicalInteractionReservationManager.IsReserved(fixture.Microwave),
                "An approaching diner must not book physical use before actually arriving.");
        });
        yield return Wait("first diner begins exclusive heating at arrival", () =>
            fixture.IsActivelyHeating && IsUsing(fixture.Diner));
        yield return ToggleSecond(context, "release the second hungry diner", true);
        yield return Wait("second diner selects a heat source while microwave is busy", () =>
            secondDiner.CurJobDef == JobDefOf.Ingest &&
            DiningSessionRegistry.HeatingSourceFor(secondDiner.CurJob) is not null);
        yield return new AssertionStep("busy microwave remains preferred over stove and campfire", _ =>
        {
            secondJob = secondDiner.CurJob;
            EndToEndAssert.True(ReferenceEquals(DiningSessionRegistry.MicrowaveFor(secondJob)?.parent, fixture.Microwave),
                "A busy operational microwave must remain the selected source rather than a reservable stove or campfire.");
            EndToEndAssert.True(ReferenceEquals(DiningSessionRegistry.CutleryFor(secondJob), secondCutlery),
                "The second fixture diner must acquire its own nearby cutlery so interruption can track each exact setting.");
        });
        yield return Wait("second diner arrives and waits behind active heating", () =>
            secondDiner.Position == fixture.Microwave.InteractionCell &&
            !secondDiner.pather.Moving && IsUsing(fixture.Diner) && !IsUsing(secondDiner));
        yield return Pause("pause on the busy microwave and waiting diner");
        yield return new AssertionStep("waiting does not heat or degrade the second meal", _ =>
        {
            var serving = SecondServing();
            EndToEndAssert.Equal(0, serving.MicrowaveReheatCount, "Waiting is not a completed reheat.");
            EndToEndAssert.Equal(80, serving.QualityScore, "Waiting must not charge quality loss.");
            EndToEndAssert.True(!fixture.Map.reservationManager.IsReserved(fixture.Microwave),
                "Waiting and active heating must not create an advance reservation.");
        });
        yield return Screenshot("first diner heats while second waits with the original cold meal");
        if (saveWhileBusy)
        {
            foreach (var step in SaveWhileBusy(context)) yield return step;
            yield break;
        }
        if (losePower)
        {
            foreach (var step in LosePower(context)) yield return step;
            yield break;
        }
        if (interruptUser)
        {
            yield return fixture.ToggleDraft(context, "draft the active microwave user", false);
            yield return new AssertionStep("cancelled cycle preserves the first exact meal", _ =>
            {
                EndToEndAssert.Equal(0, fixture.CurrentServingWithoutThermalUpdate.MicrowaveReheatCount,
                    "Drafting an active user must not complete its cycle.");
                EndToEndAssert.Equal(80, fixture.CurrentServingWithoutThermalUpdate.QualityScore,
                    "Interrupted heating must not charge quality loss.");
                EndToEndAssert.True(!IsUsing(fixture.Diner), "Drafting must immediately release physical use.");
            });
        }
        yield return Run("finish the first cycle and admit the waiting diner");
        yield return Wait("first finishes and second starts before first finishes eating", () =>
            !fixture.Meal.Destroyed &&
            (interruptUser || fixture.CurrentServingWithoutThermalUpdate.MicrowaveReheatCount == 1) &&
            IsUsing(secondDiner));
        yield return Pause("pause on the active-use handoff");
        yield return new AssertionStep("native dining jobs survive the handoff", _ =>
        {
            secondStartedTick = Find.TickManager.TicksGame;
            EndToEndAssert.True((interruptUser || ReferenceEquals(fixture.Diner.CurJob, firstJob)) &&
                                   ReferenceEquals(secondDiner.CurJob, secondJob),
                "Contention must preserve both original native ingest jobs.");
            EndToEndAssert.Equal(0, SecondServing().MicrowaveReheatCount,
                "The waiting diner must still perform a full cycle after admission.");
        });
        yield return Screenshot(interruptUser
            ? "drafted first diner has a cold meal and waiting diner has taken over heating"
            : "first meal is hot and second diner now owns active heating");
        yield return Run("complete the second full heating cycle");
        yield return Wait("second exact meal completes one full microwave cycle", () =>
            !secondMeal.Destroyed && SecondServing().MicrowaveReheatCount == 1);
        yield return Pause("pause after the second completed reheat");
        yield return new AssertionStep("waiting time did not shorten active heating", _ =>
        {
            EndToEndAssert.True(Find.TickManager.TicksGame - secondStartedTick >= HeatingTicks - 4,
                "The second diner must spend the complete configured duration heating after active use begins.");
            EndToEndAssert.True(SecondServing().TemperatureCelsius >= 55f,
                "The second exact serving must visibly be steaming after reheating.");
        });
        yield return Screenshot("second original meal is steaming after its full cycle");
        yield return new TimeControlActionStep("finish both ordinary dining jobs", false, EndToEndGameSpeed.Superfast);
        yield return Wait("both exact meals are eaten and their exact settings return dirty", () =>
            (interruptUser || fixture.Meal.Destroyed && fixture.ExactSettingReturnedDirty) && secondMeal.Destroyed &&
            secondPlate.Spawned && secondPlate.GetComp<CompSanitation>().IsDirty &&
            secondCutlery.Spawned && secondCutlery.GetComp<CompSanitation>().IsDirty);
        yield return Pause("pause after both diners finish");
        yield return Screenshot("both exact dirty settings returned after sharing the microwave");
        yield return new AssertionStep("no microwave ownership remains after dining", _ =>
            EndToEndAssert.True(!fixture.Map.reservationManager.IsReserved(fixture.Microwave) &&
                                   !fixture.Map.physicalInteractionReservationManager.IsReserved(fixture.Microwave),
                "Neither advance reservations nor active-use ownership may outlive the cycles."));
    }

    private bool IsUsing(Pawn pawn) => fixture.Map.physicalInteractionReservationManager.IsReservedBy(pawn, fixture.Microwave);

    private IEnumerable<EndToEndStep> LosePower(IEndToEndContext context)
    {
        var toggle = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { generator.ThingID }, Array.Empty<string>())
            .Single(option => !option.Disabled && option.Interaction == EndToEndGizmoInteraction.Toggle &&
                              option.ToggleState == true && option.HotKeyDefName == "Command_TogglePower");
        yield return new GizmoActionStep("switch off the microwave generator", new[] { generator.ThingID },
            toggle.RuntimeType, EndToEndGizmoInteraction.Toggle, stableGizmoId: toggle.StableId);
        yield return new WindowAcceptActionStep("confirm the native flick tutorial", "Verse.Dialog_MessageBox");
        var flick = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(powerOperator.ThingID, generator.ThingID)
            .Single(option => !option.Disabled && option.Label.IndexOf("flick", StringComparison.OrdinalIgnoreCase) >= 0);
        yield return new FloatMenuActionStep("prioritize switching off the generator",
            powerOperator.ThingID, generator.ThingID, flick.StableId);
        yield return Run("let the operator switch off power while diners queue");
        yield return Wait("power loss releases active and waiting dining jobs", () =>
            !fixture.Microwave.GetComp<CompMicrowave>().Operational &&
            !ReferenceEquals(fixture.Diner.CurJob, firstJob) && !ReferenceEquals(secondDiner.CurJob, secondJob));
        yield return Pause("pause after both microwave jobs cancel");
        yield return new AssertionStep("power loss applies no heating and leaks no active use", _ =>
        {
            EndToEndAssert.Equal(0, fixture.CurrentServingWithoutThermalUpdate.MicrowaveReheatCount, "Interrupted owner must remain unheated.");
            EndToEndAssert.Equal(0, SecondServing().MicrowaveReheatCount, "Waiting meal must remain unheated.");
            EndToEndAssert.Equal(80, SecondServing().QualityScore, "Waiting meal must retain its original quality.");
            EndToEndAssert.True(!fixture.Map.physicalInteractionReservationManager.IsReserved(fixture.Microwave),
                "An unpowered microwave must have no stale physical user.");
        });
        yield return Screenshot("unpowered microwave releases both diners without heating their meals");
        yield return Run("allow ordinary food search to use usable lower-tier heat sources");
        yield return Wait("waiting diner selects a fallback after actual power loss", () =>
            secondDiner.CurJob is { } job && DiningSessionRegistry.HeatingSourceFor(job) is { } source &&
            source.Kind != MealHeatingSourceKind.Microwave);
        yield return Wait("waiting diner physically reheats at the selected fallback", () =>
            secondDiner.CurJob is { } job && DiningSessionRegistry.HeatingSourceFor(job) is { } source &&
            secondDiner.Position == source.Thing.InteractionCell && !secondDiner.pather.Moving);
        yield return Pause("pause on valid unpowered-microwave fallback");
        yield return Screenshot("diner uses a fallback only after the microwave loses power");
        yield return Run("complete ordinary fallback heating");
        yield return Wait("the second exact meal receives completed fallback heat", () =>
            !secondMeal.Destroyed && SecondServing().TemperatureCelsius >= 40f && SecondServing().QualityScore < 80);
        yield return Pause("pause after actual fallback heating");
        yield return new AssertionStep("fallback never counts as a microwave cycle", _ =>
            EndToEndAssert.Equal(0, SecondServing().MicrowaveReheatCount, "Fallback heating must not record a microwave cycle."));
        yield return Screenshot("second meal is hot after completing the fallback cycle");
        yield return Run("finish fallback dining");
        yield return Wait("fallback diner eats the same meal and returns its dirty setting", () =>
            secondMeal.Destroyed && secondPlate.Spawned && secondPlate.GetComp<CompSanitation>().IsDirty &&
            secondCutlery.Spawned && secondCutlery.GetComp<CompSanitation>().IsDirty);
        yield return Pause("pause after completed fallback dining");
        yield return Screenshot("dirty setting returned after valid power-loss fallback");
    }

    private IEnumerable<EndToEndStep> SaveWhileBusy(IEndToEndContext context)
    {
        const string saveName = "ImmersiveChefsE2E_MicrowaveQueue";
        var savePath = GenFilePaths.FilePathForSavedGame(saveName);
        context.DeferCleanup(() => { if (System.IO.File.Exists(savePath)) System.IO.File.Delete(savePath); });
        var secondDinerId = secondDiner.ThingID;
        var secondMealId = secondMeal.ThingID;
        var secondPlateId = secondPlate.ThingID;
        var secondCutleryId = secondCutlery.ThingID;
        var stoveId = stove.ThingID;
        var campfireId = campfire.ThingID;
        yield return new SaveLoadActionStep("save and load while one diner heats and another waits", saveName);
        yield return new AssertionStep("load recovery preserves both cold meals and cancels transient cycles", _ =>
        {
            fixture = fixture.ResolveAfterLoad();
            secondDiner = (Pawn)CountertopMicrowaveDiningFixture.FindLoadedThing(fixture.Map, secondDinerId);
            secondMeal = (ThingWithComps)CountertopMicrowaveDiningFixture.FindLoadedThing(fixture.Map, secondMealId);
            secondPlate = (ThingWithComps)secondMeal.GetComp<CompEmbeddedWare>().PeekPlateThing()!;
            secondCutlery = (ThingWithComps)CountertopMicrowaveDiningFixture.FindLoadedThing(fixture.Map, secondCutleryId);
            stove = (ThingWithComps)CountertopMicrowaveDiningFixture.FindLoadedThing(fixture.Map, stoveId);
            campfire = (ThingWithComps)CountertopMicrowaveDiningFixture.FindLoadedThing(fixture.Map, campfireId);
            EndToEndAssert.Equal(secondPlateId, secondPlate.ThingID, "The waiting diner's embedded plate must survive exactly.");
            EndToEndAssert.Equal(0, fixture.CurrentServingWithoutThermalUpdate.MicrowaveReheatCount, "Load must not complete the interrupted cycle.");
            EndToEndAssert.Equal(0, SecondServing().MicrowaveReheatCount, "Load must not complete the waiting cycle.");
            EndToEndAssert.Equal(80, SecondServing().QualityScore, "Load must preserve the waiting meal's quality.");
            EndToEndAssert.True(!fixture.Map.reservationManager.IsReserved(fixture.Microwave), "Load must not create an advance booking.");
        });
        yield return Screenshot("same cold meals and microwave remain after queue save and load");
        var firstReheated = false;
        var secondReheated = false;
        yield return Run("let recovered native dining jobs reheat both exact meals");
        yield return Wait("both recovered diners complete exactly one microwave cycle", () =>
        {
            if (!fixture.Meal.Destroyed)
                firstReheated |= fixture.CurrentServingWithoutThermalUpdate.MicrowaveReheatCount == 1;
            if (!secondMeal.Destroyed)
                secondReheated |= SecondServing().MicrowaveReheatCount == 1;
            return firstReheated && secondReheated;
        });
        yield return Pause("pause after both loaded meals have reheated");
        yield return Screenshot("recovered diners resume normal reheating after load");
        yield return Run("finish loaded dining jobs");
        yield return Wait("both loaded meals are consumed with all exact ware recovered", () =>
            fixture.Meal.Destroyed && fixture.ExactSettingReturnedDirty && secondMeal.Destroyed &&
            secondPlate.Spawned && secondPlate.GetComp<CompSanitation>().IsDirty &&
            secondCutlery.Spawned && secondCutlery.GetComp<CompSanitation>().IsDirty);
        yield return Pause("pause after loaded dining completes");
        yield return Screenshot("all exact dirty settings returned after queue save and load");
        yield return new AssertionStep("load recovery leaves no stale microwave user", _ =>
            EndToEndAssert.True(!fixture.Map.physicalInteractionReservationManager.IsReserved(fixture.Microwave),
                "The saved active user must not leave an orphaned physical reservation."));
    }
    private CulinaryServingRecord SecondServing() => secondMeal.GetComp<CompCulinaryState>().PeekCurrentServingWithoutThermalUpdate()!;
    private static EndToEndStep Wait(string name, Func<bool> predicate) => new WaitUntilStep(
        name, _ => predicate(), new EndToEndDeadline(2_400, 6_000, TimeSpan.FromSeconds(65)));
    private static EndToEndStep Run(string name) => new TimeControlActionStep(name, false, EndToEndGameSpeed.Normal);
    private static EndToEndStep Pause(string name) => new TimeControlActionStep(name, true, EndToEndGameSpeed.Normal);
    private EndToEndStep Screenshot(string name) => new ScreenshotStep(name,
        new[] { fixture.Diner.ThingID, secondDiner.ThingID, fixture.Microwave.ThingID, stove.ThingID, campfire.ThingID }, 160);

    private EndToEndStep ToggleSecond(IEndToEndContext context, string name, bool current)
    {
        var option = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { secondDiner.ThingID }, Array.Empty<string>())
            .Single(candidate => !candidate.Disabled && candidate.Interaction == EndToEndGizmoInteraction.Toggle &&
                                 candidate.ToggleState == current && candidate.HotKeyDefName == "Command_ColonistDraft");
        return new GizmoActionStep(name, new[] { secondDiner.ThingID }, option.RuntimeType,
            EndToEndGizmoInteraction.Toggle, stableGizmoId: option.StableId);
    }

    private ThingWithComps SpawnFueledSource(string defName, IntVec3 position)
    {
        var def = DefDatabase<ThingDef>.GetNamed(defName);
        var source = (ThingWithComps)ThingMaker.MakeThing(def, def.MadeFromStuff ? ThingDefOf.Steel : null);
        source.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(source, position, fixture.Map, Rot4.North);
        source.TryGetComp<CompRefuelable>()?.Refuel(100f);
        return source;
    }
}
