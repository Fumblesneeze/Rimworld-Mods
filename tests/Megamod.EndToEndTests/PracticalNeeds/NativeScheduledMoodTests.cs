using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LudeonTK;
using Megamod.EndToEndTests.DrugPolicyMaintenance;
using Megamod.EndToEndTests.DrugTolerance;
using Megamod.Features.DrugPolicyMaintenance.Runtime;
using Megamod.Features.DrugTolerance;
using Megamod.Features.DrugTolerance.Core;
using Megamod.Features.DrugTolerance.Runtime;
using Megamod.Features.PracticalNeeds.Core;
using Megamod.Settings;
using RimWorld;
using RimWorld.Planet;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;
using Verse.AI;
using HarmonyOwner = HarmonyLib.Harmony;

namespace Megamod.EndToEndTests.PracticalNeeds;

[RimWorldEndToEndTest("megamod.practical-needs.native-scheduled-mood-composition", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 6000, MaxWallClockSeconds = 2700)]
public sealed class NativeScheduledMoodTests : IRimWorldEndToEndTest
{
    private readonly ScheduledMoodFixture fixture = new(MoodScenario.Map);
    public void Arrange(IEndToEndContext context) => fixture.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => fixture.Execute(context);
}

[RimWorldEndToEndTest("megamod.practical-needs.native-caravan-potential-mood", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 3000, MaxWallClockSeconds = 3000)]
public sealed class NativeCaravanPotentialMoodTests : IRimWorldEndToEndTest
{
    private readonly ScheduledMoodFixture fixture = new(MoodScenario.Caravan);
    public void Arrange(IEndToEndContext context) => fixture.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => fixture.Execute(context);
}

internal enum MoodScenario { Map, Caravan, Availability }

// One fixture owns only its disposable pawns/policies/physical Beer and passive observations.
// The availability workflow is in the sibling partial file; it shares these same native oracles.
internal sealed partial class ScheduledMoodFixture
{
    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
    private static readonly FieldInfo BindingField = Field(typeof(SettingsRuntime), "binding");
    private static readonly FieldInfo PolicyField = Field(typeof(Pawn_DrugPolicyTracker), "curPolicy");
    private static readonly FieldInfo HistoryField = Field(typeof(Pawn_DrugPolicyTracker), "drugTakeRecords");
    private static readonly FieldInfo TimesField = Field(typeof(DrugTakeRecord), "timesTakenThisDayInt");
    private static readonly FieldInfo DayField = Field(typeof(DrugTakeRecord), "thisDay");
    private static readonly MethodInfo TickTarget = Native(typeof(Caravan), "TickInterval", false, false, typeof(void), typeof(int));
    private static readonly MethodInfo ScheduleTarget = Native(typeof(CaravanDrugPolicyUtility), "TryTakeScheduledDrugs", true, false, typeof(void), typeof(Pawn), typeof(Caravan));
    private static readonly MethodInfo ShouldTarget = Native(typeof(Pawn_DrugPolicyTracker), "ShouldTryToTakeScheduledNow", false, true, typeof(bool), typeof(ThingDef));
    private static readonly MethodInfo AllowedTarget = Native(typeof(Pawn_DrugPolicyTracker), "AllowedToTakeScheduledNow", false, true, typeof(bool), typeof(ThingDef));
    private static readonly MethodInfo DrugTarget = Native(typeof(Caravan_NeedsTracker), "IngestDrug", false, true, typeof(void), typeof(Pawn), typeof(Thing), typeof(Pawn));
    private static readonly MethodInfo IngestedTarget = Native(typeof(Thing), "Ingested", false, true, typeof(float), typeof(Pawn), typeof(float));
    private static readonly MethodInfo SplitTarget = Native(typeof(ThingWithComps), "SplitOff", false, true, typeof(Thing), typeof(int));
    private static readonly MethodInfo PriorityTarget = Native(typeof(JobGiver_TakeDrugsForDrugPolicy), "GetPriority", false, true, typeof(float), typeof(Pawn));
    private static readonly MethodInfo JobTarget = Native(typeof(JobGiver_TakeDrugsForDrugPolicy), "TryGiveJob", false, false, typeof(Job), typeof(Pawn));
    private static readonly MethodInfo MoodTarget = typeof(Need_Mood).GetProperty(nameof(Need_Mood.CurInstantLevel))!.GetMethod!;
    private static ScheduledMoodFixture? active;
    private readonly MoodScenario scenario;
    private bool CaravanCase => scenario == MoodScenario.Caravan;
    private readonly string marker = "ScheduledMood-" + Guid.NewGuid().ToString("N")[..8];
    private readonly NativeDrugPolicyView policyView = new();
    private readonly List<Subject> subjects = new();
    private readonly List<Pawn> pawns = new();
    private readonly List<Caravan> caravans = new();
    private readonly List<DrugPolicy> policies = new();
    private readonly HashSet<Thing> things = new();
    private readonly HashSet<Window> windows = new();
    private readonly HashSet<Window> baselineWindows = new();
    private readonly List<TraceBinding> traceBindings = new();
    private readonly List<Decision> decisions = new();
    private readonly List<Dose> doses = new();
    private readonly List<Split> splits = new();
    private IEndToEndContext context = null!;
    private Game game = null!;
    private Map map = null!;
    private World world = null!;
    private MegamodMod mod = null!;
    private SettingsRuntime settings = null!;
    private SaveSettingsBinding binding = null!;
    private GlobalSettingsSnapshot globals = null!;
    private SaveSettingsSnapshot saved = null!;
    private NativeDrugToleranceRuntime tolerance = null!;
    private NativeScheduledDrugDecision scheduled = null!;
    private MoodSettingControls moodControls = null!, toleranceControls = null!;
    private ThingDef beer = null!;
    private HediffDef alcohol = null!, alcoholTolerance = null!;
    private HarmonyOwner traceOwner = null!;
    private IEndToEndNativeCaravanObservation health = null!;
    private EndToEndNativeWindow window = null!;
    private MainTabWindow_Assign assign = null!;
    private Dialog_ManageDrugPolicies manager = null!;
    private string pointer = null!, worldView = null!;
    private NativeScope? tickScope, scheduleScope, mapScope;
    private Decision? decisionScope;
    private Dose? doseScope;
    private string? traceFailure;
    private string phase = "prepared";
    private bool expectedMood = true, expectedTolerance = true, layoutUnavailable, running;
    private int day, initialTick, phaseTick, actions, screenshots, waitSeconds;
    private Subject A => subjects[0];
    private Subject B => subjects[1];
    internal ScheduledMoodFixture(MoodScenario scenario) => this.scenario = scenario;

    internal void Arrange(IEndToEndContext context)
    {
        this.context = context; game = Current.Game; map = Find.CurrentMap; world = game.World;
        foreach (var window in Find.WindowStack.Windows) baselineWindows.Add(window);
        EndToEndAssert.True(active == null && game.PlayerHasControl && Prefs.DevMode && !LongEventHandler.AnyEventNowOrWaiting &&
            Find.MainTabsRoot.OpenTab == null && Find.Selector.SelectedObjects.Count == 0 && !WorldRendererUtility.WorldSelected &&
            map.mapPawns.AllPawnsSpawned.Count == 0 && !Find.WindowStack.Windows.Any(w => w is Dialog_ModSettings or Dialog_ManageDrugPolicies or EditWindow_Log));
        settings = SettingsRuntime.Instance ?? throw new InvalidOperationException("Common settings unavailable.");
        binding = BindingField.GetValue(settings) as SaveSettingsBinding ?? throw new InvalidOperationException("Published binding unavailable.");
        globals = settings.Store.ExportGlobals(); saved = settings.Store.ExportSave(binding);
        mod = LoadedModManager.GetMod<MegamodMod>();
        tolerance = (NativeDrugToleranceRuntime)Field(typeof(MegamodMod), "drugTolerance").GetValue(mod)!;
        scheduled = (NativeScheduledDrugDecision)Field(typeof(MegamodMod), "scheduledDrugDecision").GetValue(mod)!;
        EndToEndAssert.True(tolerance != null && scheduled != null && tolerance.Available && scheduled.Available && scheduled.PotentialMood.Available);
        EndToEndAssert.True(settings.Store.UsesGlobal(PotentialMoodSettings.Enabled) && settings.Store.UsesGlobal(DrugToleranceSettings.Enabled) &&
            settings.Store.GetGlobal(PotentialMoodSettings.Enabled) && settings.Store.GetGlobal(DrugToleranceSettings.Enabled));
        var speed = Find.TickManager.CurTimeSpeed;
        moodControls = new(PotentialMoodSettings.Enabled.Id, RequireGame, windows);
        toleranceControls = new(DrugToleranceSettings.Enabled.Id, RequireGame, windows);
        var scroll = moodControls.Scroll;
        context.DeferCleanup(() =>
        {
            RequireGame(); var errors = new List<Exception>();
            Attempt(errors, CleanupObjects);
            Attempt(errors, () => { settings.Store.LoadGlobals(globals); moodControls.RestoreScroll(scroll); });
            Attempt(errors, () => EndToEndAssert.True(ReferenceEquals(BindingField.GetValue(settings), binding) &&
                saved.Values.OrderBy(p => p.Key).SequenceEqual(settings.Store.ExportSave(binding).Values.OrderBy(p => p.Key))));
            Attempt(errors, () => Find.TickManager.CurTimeSpeed = speed);
            if (errors.Count != 0) throw new AggregateException("Scheduled-mood fixture restoration failed.", errors);
        });
        Find.TickManager.Pause(); initialTick = Find.TickManager.TicksGame; day = GenDate.DaysPassed;
        EndToEndAssert.True(60000 - initialTick % 60000 >= (CaravanCase ? 3000 : 6000), "The whole comparison must fit in one native day.");
        beer = DefDatabase<ThingDef>.GetNamed("Beer"); alcohol = DefDatabase<HediffDef>.GetNamed("AlcoholHigh");
        alcoholTolerance = DefDatabase<HediffDef>.GetNamed("AlcoholTolerance");
        EndToEndAssert.True(beer.thingClass == typeof(ThingWithComps) && beer.IsDrug && beer.ingestible.defaultNumToIngestAtOnce == 1 &&
            beer.ingestible.outcomeDoers.OfType<IngestionOutcomeDoer_GiveHediff>().Any(o => o.hediffDef == alcohol) &&
            beer.ingestible.outcomeDoers.OfType<IngestionOutcomeDoer_GiveHediff>().Any(o => o.hediffDef == alcoholTolerance));
        PlanetTile[] tiles = Array.Empty<PlanetTile>();
        if (CaravanCase)
        {
            EndToEndAssert.True(world.worldObjects.Caravans.All(c => c.Faction != Faction.OfPlayer));
            var neighbors = new List<PlanetTile>(); world.grid.GetTileNeighbors(map.Tile, neighbors);
            tiles = neighbors.Where(t => t.Valid && t.Layer == world.grid.Surface && world.pathGrid.Passable(t)).Distinct().Take(2).ToArray();
            EndToEndAssert.Equal(2, tiles.Length); health = context.GetRequiredService<IEndToEndNativeCaravanObservation>();
        }
        var offsets = new[] { new IntVec3(-3, 0, 0), new IntVec3(3, 0, 0), new IntVec3(0, 0, 3) };
        var center = CellRect.CenteredOn(map.Center, 15).OrderBy(c => c.DistanceToSquared(map.Center)).First(c =>
            offsets.All(o => (c + o).InBounds(map) && (c + o).Standable(map) && !(c + o).Fogged(map)));
        int count = scenario == MoodScenario.Availability ? 3 : 2;
        for (int i = 0; i < count; i++) subjects.Add(CreateSubject(i, center + offsets[i], CaravanCase ? tiles[i] : default));
        if (scenario == MoodScenario.Availability) ArrangeDisplay();
        traceOwner = new HarmonyOwner("fumblesneeze.tests.s024.native." + marker);
        context.DeferCleanup(CleanupTrace); active = this;
        Install(ShouldTarget, nameof(BeginDecision), nameof(ObserveDecision), nameof(EndDecision));
        Install(AllowedTarget, nameof(BeginAllowed), nameof(ObserveAllowed), null);
        Install(MoodTarget, nameof(BeginMood), nameof(ObserveMood), nameof(EndMood));
        Install(IngestedTarget, nameof(BeginIngested), nameof(ObserveIngested), nameof(EndIngested));
        Install(SplitTarget, nameof(BeginSplit), nameof(ObserveSplit), nameof(EndSplit));
        if (CaravanCase)
        {
            Install(TickTarget, nameof(BeginTick), null, nameof(EndTick));
            Install(ScheduleTarget, nameof(BeginSchedule), null, nameof(EndSchedule));
            Install(DrugTarget, nameof(BeginDrug), nameof(ObserveDrug), nameof(EndDrug));
        }
        else
        {
            Install(PriorityTarget, nameof(BeginMap), null, nameof(EndMap));
            Install(JobTarget, nameof(BeginMap), nameof(ObserveJob), nameof(EndMap));
        }
        context.DeferCleanup(() => { RequireGame(); Find.TickManager.Pause(); });
        RequireCurrent();
    }

    private Subject CreateSubject(int index, IntVec3 cell, PlanetTile tile)
    {
        string label = index == 0 ? "A recovering" : index == 1 ? "B control" : "C fresh zero";
        var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer,
            forceGenerateNewPawn: true, canGeneratePawnRelations: false, allowFood: false, allowAddictions: false,
            fixedBiologicalAge: 25, fixedChronologicalAge: 25, forceNoIdeo: true, forceNoGear: true));
        pawns.Add(pawn); pawn.Name = new NameTriple("Mood", label, marker);
        foreach (var h in pawn.health.hediffSet.hediffs.ToArray()) pawn.health.RemoveHediff(h);
        foreach (var t in pawn.story.traits.allTraits.ToArray()) pawn.story.traits.RemoveTrait(t);
        pawn.inventory.innerContainer.ClearAndDestroyContents(); pawn.workSettings.EnableAndInitialize();
        foreach (var work in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            if (!pawn.WorkTypeIsDisabled(work)) pawn.workSettings.SetPriority(work, 0);
        for (int hour = 0; hour < 24; hour++) pawn.timetable.SetAssignment(hour, TimeAssignmentDefOf.Anything);
        pawn.needs.food.CurLevelPercentage = pawn.needs.rest.CurLevelPercentage = pawn.needs.joy.CurLevelPercentage = 1;
        foreach (var memory in pawn.needs.mood.thoughts.memories.Memories.ToArray()) pawn.needs.mood.thoughts.memories.RemoveMemory(memory);
        var optimism = DefDatabase<ThoughtDef>.GetNamed("NewColonyOptimism");
        EndToEndAssert.True(optimism.stages.Count == 1 && optimism.stages[0].baseMoodEffect == 10 && optimism.durationDays == 8 &&
            pawn.needs.mood.GetType() == typeof(Need_Mood) && pawn.needs.mood.def.baseLevel == .32f && pawn.needs.mood.MaxLevel == 1);
        pawn.needs.mood.thoughts.memories.TryGainMemory(optimism);
        pawn.needs.mood.CurLevelPercentage = .4f;
        var policy = game.drugPolicyDatabase.MakeNewDrugPolicy(); policies.Add(policy); policy.label = marker + " " + label;
        for (int i = 0; i < policy.Count; i++)
        { policy[i].allowScheduled = policy[i].allowedForJoy = policy[i].allowedForAddiction = false; policy[i].takeToInventory = 0; }
        var entry = policy[beer]; entry.allowScheduled = true; entry.daysFrequency = 1; entry.onlyIfJoyBelow = 1;
        entry.onlyIfMoodBelow = index == 0 ? .5f : CaravanCase ? .9f : 1;
        entry.takeToInventory = CaravanCase ? 2 : 0;
        EndToEndAssert.Equal(DrugToleranceChoiceKind.Any, NativeDrugToleranceState.Read(entry).Kind);
        pawn.drugs.CurrentPolicy = policy;
        var stock = ThingMaker.MakeThing(beer); things.Add(stock); stock.stackCount = 2;
        EndToEndAssert.True(pawn.inventory.innerContainer.TryAdd(stock));
        Caravan? caravan = null;
        if (CaravanCase)
        { caravan = CaravanMaker.MakeCaravan(new[] { pawn }, Faction.OfPlayer, tile, true); caravans.Add(caravan); caravan.Name = marker + " " + label; }
        else { GenSpawn.Spawn(pawn, cell, map); pawn.drafter.Drafted = true; }
        // Setup only: actual native thought computation; never touch its cache or refresh it during execution.
        float potential = pawn.needs.mood.CurInstantLevelPercentage;
        var thoughts = new List<Thought>(); pawn.needs.mood.thoughts.GetAllMoodThoughts(thoughts);
        EndToEndAssert.True(thoughts.Any(t => t.def == optimism && t.MoodOffset() == 10), "Require the actual pinned Core optimism thought.");
        EndToEndAssert.True(float.IsFinite(potential) && potential >= .6f && (!CaravanCase || index != 1 || potential < .9f),
            "Prepared native mood must be recovering and caravan B strictly below its relevant threshold; actual=" + potential);
        return new Subject(pawn, policy, entry, stock, caravan, tile,
            string.Join(";", thoughts.Select(t => t.def.defName + ":" + t.CurStageIndex + ":" + t.MoodOffset().ToString("R", CultureInfo.InvariantCulture))), potential);
    }

    internal IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new CheckpointStep("disposable precondition mutation ledger before any native time", _ => Evidence());
        foreach (var step in scenario == MoodScenario.Caravan ? CaravanWorkflow() : scenario == MoodScenario.Availability ? AvailabilityWorkflow() : MapWorkflow())
        {
            if (step is ScreenshotStep) screenshots++;
            else if (step is WaitUntilStep wait) waitSeconds += checked((int)wait.Deadline.MaxWallClock.TotalSeconds);
            else if (step.Kind == EndToEndStepKind.Act || step is NativeWindowCaptureStep or NativeWorldViewCaptureStep or NativeCaravanCaptureStep or NativeCaravanHealthCaptureStep) actions++;
            if (step.Kind == EndToEndStepKind.Act) yield return new AssertionStep("validate current inputs before " + step.Name, _ => RequireCurrent());
            yield return step;
        }
        yield return new CheckpointStep("complete native scheduled-mood sequence and bounded accounting", _ =>
        {
            RequireCurrent(); VerifyBudget(); return Evidence();
        });
    }

    private IEnumerable<EndToEndStep> Policies()
    {
        yield return new NativeMainTabActionStep("open Assign for the distinct scheduled policies", "Assign", true);
        yield return Capture<MainTabWindow_Assign>("capture actual Manage entry", value => assign = value);
        yield return new NativePointerScopeBeginStep("own policy pointer", value => pointer = value);
        yield return Click("open native drug-policy manager", policyView.ManageButton(assign).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture native policy manager", value => manager = value);
        foreach (var subject in subjects)
        {
            yield return Click("select exact policy " + subject.Pawn.LabelShort, policyView.PolicyRow(manager, subject.Policy).center);
            yield return Capture<Dialog_ManageDrugPolicies>("capture selected native schedule", value =>
            { manager = value; EndToEndAssert.True(ReferenceEquals(policyView.Selected(manager), subject.Policy)); });
            int target = CaravanCase || subject == A ? 101 : 0;
            yield return NativeToleranceSliderView.Drag("set native tolerance " + (target == 0 ? "zero" : "Any"), window, pointer, manager, subject.Policy, subject.Entry, target);
            yield return new AssertionStep("retain only the actual native choice commit", _ =>
            {
                var choice = NativeDrugToleranceState.Read(subject.Entry);
                EndToEndAssert.True(target == 0 ? choice.Kind == DrugToleranceChoiceKind.Limit && choice.Limit == 0 : choice.Kind == DrugToleranceChoiceKind.Any);
                subject.Choice = NativeDrugToleranceState.ReadEncoded(subject.Entry);
            });
            yield return Capture<Dialog_ManageDrugPolicies>("capture committed native policy row", value => manager = value);
            yield return Shot(subject.Pawn.LabelShort + " exact mood threshold, daily interval, stock target and tolerance choice");
        }
        yield return new WindowCancelActionStep("close native manager", typeof(Dialog_ManageDrugPolicies).FullName!);
        yield return new NativePointerScopeEndStep("restore policy pointer", pointer);
        yield return new NativeMainTabActionStep("close native Assign", "Assign", false);
    }

    private IEnumerable<EndToEndStep> SetGlobal(bool mood, bool value)
    {
        var controls = mood ? moodControls : toleranceControls;
        var definition = mood ? PotentialMoodSettings.Enabled : DrugToleranceSettings.Enabled;
        EndToEndAssert.True(settings.Store.UsesGlobal(definition) && settings.Store.GetGlobal(definition) != value);
        foreach (var step in controls.Open()) yield return step;
        yield return controls.Click("native Global default " + definition.Id + " = " + value, controls.Rects.GlobalDefault.center);
        yield return new AssertionStep("observe exact native global setting change", _ =>
        { if (mood) expectedMood = value; else expectedTolerance = value; RequireCurrent(); });
        foreach (var step in controls.Refresh()) yield return step;
        yield return Shot("native setting " + definition.Id + " now " + value + "; distinct policies and choices retained");
        foreach (var step in controls.Close()) yield return step;
    }

    private IEnumerable<EndToEndStep> MapWorkflow()
    {
        foreach (var step in Policies()) yield return step;
        yield return new CameraActionStep("frame the paired scheduled subjects", pawns.Select(p => p.ThingID), paddingPixels: 220);
        yield return Inspect(A, EndToEndPawnInspectTab.Needs); yield return Shot("A actual current mood and native potential marker before time");
        foreach (var s in subjects) { yield return Inspect(s, EndToEndPawnInspectTab.Gear); yield return Shot(s.Pawn.LabelShort + " owns two initial Beer"); }
        BeginPhase("both on");
        foreach (var s in subjects) foreach (var step in Undraft(s)) yield return step;
        yield return Run("ordinary native map thinking with both predicates enabled");
        yield return Wait("observe two fresh native-eligible rejections for each independent predicate", () =>
        { RequireCurrent(); Held(A, 2); Held(B, 2); return Rejections(A, true, true) >= 2 && Rejections(B, true, true) >= 2; }, 45, 600);
        yield return Pause("pause observed conjunction rejections");
        foreach (var s in subjects) { yield return Inspect(s, EndToEndPawnInspectTab.Gear); yield return Shot(s.Pawn.LabelShort + " retained Beer after real rejected selections"); }
        yield return Inspect(A, EndToEndPawnInspectTab.Needs); yield return Shot("A native recovering-mood marker after scheduled rejections");
        foreach (var step in SetGlobal(false, false)) yield return step;
        BeginPhase("tolerance off");
        foreach (var step in ConsumeMap(B, reenableMood: false)) yield return step;
        EndToEndAssert.True(Rejections(A, true, false) >= 2, "A must remain mood-rejected while the native zero control consumes."); Held(A, 2);
        foreach (var step in SetGlobal(false, true)) yield return step;
        foreach (var step in SetGlobal(true, false)) yield return step;
        BeginPhase("mood off selection");
        foreach (var step in ConsumeMap(A, reenableMood: true)) yield return step;
        yield return Run("observe unchanged native interval after both first doses");
        yield return Wait("B remains interval-rejected rather than receiving a second Beer", () =>
        { RequireCurrent(); Held(A, 1); Held(B, 1); return IntervalRejections(B) >= 2; }, 45, 600);
        yield return Pause("pause final paired map result");
        foreach (var s in subjects) AssertDose(s);
        foreach (var step in Console()) yield return step;
    }

    private IEnumerable<EndToEndStep> Undraft(Subject subject)
    {
        yield return new SelectionActionStep("select drafted " + subject.Pawn.LabelShort, new[] { subject.Pawn.ThingID }, additive: false);
        var option = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { subject.Pawn.ThingID }, Array.Empty<string>())
            .Single(o => !o.Disabled && o.Interaction == EndToEndGizmoInteraction.Toggle && o.ToggleState == true && o.HotKeyDefName == "Command_ColonistDraft");
        yield return new GizmoActionStep("native Undraft " + subject.Pawn.LabelShort, new[] { subject.Pawn.ThingID }, option.RuntimeType,
            EndToEndGizmoInteraction.Toggle, stableGizmoId: option.StableId);
    }

    private IEnumerable<EndToEndStep> ConsumeMap(Subject subject, bool reenableMood)
    {
        yield return Run("allow ordinary scheduled selection for " + subject.Pawn.LabelShort);
        yield return Wait("observe the actual selected and executing ingestion job", () =>
        {
            RequireCurrent(); var job = subject.Pawn.CurJob;
            if (job?.def != JobDefOf.Ingest || subject.Pawn.jobs.curDriver is not JobDriver_Ingest driver || !driver.GainingNutritionNow) return false;
            EndToEndAssert.True(subject.SelectedJob != null && ReferenceEquals(job, subject.SelectedJob) && !job.playerForced &&
                job.jobGiver?.GetType() == typeof(JobGiver_TakeDrugsForDrugPolicy) && subject.Stock.Contains(job.targetA.Thing) &&
                subject.Stock.Contains(subject.Pawn.carryTracker.CarriedThing) && !doses.Any(d => d.Subject == subject));
            subject.VisibleJob = job; subject.VisibleSource = subject.Pawn.carryTracker.CarriedThing;
            return true;
        }, 60, 1200);
        yield return Pause("pause the actual drinking action before the physical dose");
        yield return Inspect(subject, EndToEndPawnInspectTab.Gear);
        yield return Shot(subject.Pawn.LabelShort + " selected native Beer ingestion in progress");
        if (reenableMood)
        {
            foreach (var step in SetGlobal(true, true)) yield return step;
            yield return new AssertionStep("new setting changes future selection only", _ => EndToEndAssert.True(
                ReferenceEquals(subject.Pawn.CurJob, subject.VisibleJob) && ReferenceEquals(subject.Pawn.carryTracker.CarriedThing, subject.VisibleSource)));
        }
        yield return Run("finish that unchanged native ingestion job");
        yield return Wait("observe one physical dose, native Alcohol and ordinary history", () =>
        { RequireCurrent(); if (doses.Count(d => d.Subject == subject && d.Closed) != 1) return false; AssertDose(subject); return true; }, 60, 1200);
        yield return Pause("pause the completed native dose");
        yield return Inspect(subject, EndToEndPawnInspectTab.Health); yield return Shot(subject.Pawn.LabelShort + " actual Alcohol effects after the dose");
        yield return Inspect(subject, EndToEndPawnInspectTab.Gear); yield return Shot(subject.Pawn.LabelShort + " one Beer remains after exactly one dose");
    }

    private IEnumerable<EndToEndStep> CaravanWorkflow()
    {
        foreach (var step in Policies()) yield return step;
        foreach (var step in OpenWorld(first: true)) yield return step;
        foreach (var s in subjects) foreach (var step in ObserveCaravan(s, "before")) yield return step;
        BeginPhase("both on"); yield return Run("ordinary native stationary caravan ticks");
        yield return Wait("A mood-rejects while active relevant-threshold B consumes once then hits native interval", () =>
        {
            RequireCurrent(); Held(A, 2);
            if (Rejections(A, true, true) < 2 || doses.Count(d => d.Subject == B && d.Closed) != 1 || IntervalRejections(B) < 2) return false;
            AssertDose(B); return true;
        }, 90, 1200);
        yield return Pause("pause observed active potential-mood caravan composition");
        foreach (var s in new[] { B, A }) foreach (var step in ObserveCaravan(s, "after active composition")) yield return step;
        yield return new NativeWorldViewActionStep("leave owned World for native map settings", worldView, false);
        foreach (var step in SetGlobal(true, false)) yield return step;
        foreach (var step in OpenWorld(first: false)) yield return step;
        BeginPhase("mood off"); yield return Run("ordinary caravan ticks after native potential-mood off");
        yield return Wait("A now consumes one under its original policy and B retains its interval", () =>
        {
            RequireCurrent(); if (doses.Count(d => d.Subject == A && d.Closed) != 1) return false;
            AssertDose(A); AssertDose(B); return IntervalRejections(B) >= 2;
        }, 90, 1200);
        yield return Pause("pause actual second caravan outcome");
        foreach (var step in ObserveCaravan(A, "after native mood off")) yield return step;
        yield return new NativeWorldViewActionStep("close native World", worldView, false);
        yield return new NativeWorldViewRestoreStep("restore complete captured World baseline", worldView);
        yield return Wait("observe restored map with no owned detailed Health", () => !WorldRendererUtility.WorldSelected &&
            !world.UI.inspectPane.IsOpen && !WorldCameraManager.WorldCamera.gameObject.activeSelf && !DetailOpen(), 15);
        yield return Shot("restored map after actual scheduled caravan consumption");
        foreach (var step in Console()) yield return step;
    }

    private IEnumerable<EndToEndStep> OpenWorld(bool first)
    {
        var renderer = world.renderer; var surface = world.grid.Surface;
        var camera = WorldCameraManager.WorldCamera; var driver = WorldCameraManager.WorldCameraDriver;
        EndToEndAssert.True(surface.WorldDrawLayers.Count <= 128);
        var terrain = surface.WorldDrawLayers.OfType<WorldDrawLayer_Terrain>().ToArray(); EndToEndAssert.True(terrain.Length > 0);
        if (first) yield return new NativeWorldViewCaptureStep("capture inactive World lease before opening", value => worldView = value);
        yield return new NativeWorldViewActionStep("open owned World through native mode command", worldView, true);
        yield return Wait("wait for the committed complete World settlement predicate", () =>
            ReferenceEquals(Current.Game, game) && ReferenceEquals(game.World, world) && ReferenceEquals(Find.CurrentMap, map) &&
            ReferenceEquals(world.renderer, renderer) && ReferenceEquals(world.grid.Surface, surface) &&
            ReferenceEquals(WorldCameraManager.WorldCamera, camera) && ReferenceEquals(WorldCameraManager.WorldCameraDriver, driver) &&
            renderer.wantedMode == WorldRenderMode.Planet && WorldRendererUtility.WorldSelected && surface.Visible && camera.gameObject.activeSelf &&
            driver.isActiveAndEnabled && !LongEventHandler.AnyEventNowOrWaiting && game.PlayerHasControl &&
            terrain.All(layer => surface.WorldDrawLayers.Contains(layer) && !layer.Dirty), 30);
        foreach (var subject in subjects)
            yield return new NativeCaravanCaptureStep("capture exact current caravan " + subject.Pawn.LabelShort, worldView,
                subject.Caravan!.GetUniqueLoadID(), value => subject.Handle = value);
    }

    private IEnumerable<EndToEndStep> ObserveCaravan(Subject subject, string label)
    {
        yield return new NativeCaravanSelectStep("select exact caravan " + label, subject.Handle);
        yield return new NativeCaravanTabStep("open actual Items " + label, subject.Handle, EndToEndCaravanTab.Items, true);
        yield return new AssertionStep("native selected Items belongs to this caravan", _ => EndToEndAssert.True(
            ReferenceEquals(world.UI.selector.SingleSelectedObject, subject.Caravan) && world.UI.inspectPane.OpenTabType == typeof(WITab_Caravan_Items)));
        yield return Shot(subject.Pawn.LabelShort + " " + label + " physical Beer in native Items");
        yield return new NativeCaravanTabStep("open actual Health " + label, subject.Handle, EndToEndCaravanTab.Health, true);
        yield return Wait("wait for exact visible native Health row with detail closed", () => Health(subject).RowVisible && !Health(subject).DetailVisible, 15);
        string row = null!;
        yield return new NativeCaravanHealthCaptureStep("capture current Health detail opener", subject.Handle, subject.Pawn.GetUniqueLoadID(), value => row = value);
        yield return new NativeCaravanHealthClickStep("open exact native Health detail", row);
        yield return Wait("observe actual native Health detail", () => Health(subject).DetailVisible, 15);
        yield return Shot(subject.Pawn.LabelShort + " " + label + " native detailed Health");
        yield return new NativeCaravanHealthCaptureStep("recapture exact row before detail closure", subject.Handle, subject.Pawn.GetUniqueLoadID(), value => row = value);
        yield return new NativeCaravanHealthClickStep("close owned native Health detail", row);
        yield return Wait("observe actual detail closed before another surface", () => !Health(subject).DetailVisible && !DetailOpen(), 15);
        yield return new NativeCaravanTabStep("close exact native Health tab", subject.Handle, EndToEndCaravanTab.Health, false);
    }

    private IEnumerable<EndToEndStep> Console()
    {
        yield return new NativeDeveloperLogActionStep("open native console after scheduled-mood workflow", true);
        yield return Capture<EditWindow_Log>("capture actual native console", _ => { });
        yield return Shot("native console after scheduled-mood workflow");
        yield return new NativeDeveloperLogActionStep("close owned native console", false);
    }

    private void BeginPhase(string name) { RequireCurrent(); phase = name; phaseTick = Find.TickManager.TicksGame; running = true; }
    private void RequireGame() => EndToEndAssert.True(ReferenceEquals(Current.Game, game) && ReferenceEquals(game.World, world) &&
        ReferenceEquals(Find.CurrentMap, map) && Current.ProgramState == ProgramState.Playing, "Refuse a departed Game/World/map.");
    private void RequireCurrent(bool callback = false)
    {
        RequireGame(); EndToEndAssert.True(traceFailure == null, traceFailure ?? string.Empty);
        EndToEndAssert.True(ReferenceEquals(Field(typeof(MegamodMod), "drugTolerance").GetValue(mod), tolerance) &&
            ReferenceEquals(Field(typeof(MegamodMod), "scheduledDrugDecision").GetValue(mod), scheduled) && scheduled.Available &&
            (displayTransition ? Find.TickManager.Paused : tolerance.Available == (expectedTolerance && !layoutUnavailable)) &&
            (!expectedMood || scheduled.PotentialMood.Available));
        EndToEndAssert.True(ReferenceEquals(BindingField.GetValue(settings), binding) && !binding.IsRevoked && settings.SaveLifecycleAvailable &&
            settings.Store.GetEffective(PotentialMoodSettings.Enabled) == expectedMood && settings.Store.GetEffective(DrugToleranceSettings.Enabled) == expectedTolerance &&
            settings.Store.UsesGlobal(PotentialMoodSettings.Enabled) && settings.Store.UsesGlobal(DrugToleranceSettings.Enabled));
        EndToEndAssert.True(globals.Values.Where(p => p.Key != PotentialMoodSettings.Enabled.Id && p.Key != DrugToleranceSettings.Enabled.Id).OrderBy(p => p.Key)
            .SequenceEqual(settings.Store.ExportGlobals().Values.Where(p => p.Key != PotentialMoodSettings.Enabled.Id && p.Key != DrugToleranceSettings.Enabled.Id).OrderBy(p => p.Key)) &&
            saved.Values.OrderBy(p => p.Key).SequenceEqual(settings.Store.ExportSave(binding).Values.OrderBy(p => p.Key)));
        EndToEndAssert.Equal(day, GenDate.DaysPassed);
        EndToEndAssert.True(Find.TickManager.TicksGame - initialTick < (CaravanCase ? 3000 : 6000));
        foreach (var s in subjects)
        {
            EndToEndAssert.True(!s.Pawn.Destroyed && !s.Pawn.Dead && !s.Pawn.Downed && !s.Pawn.InMentalState &&
                s.Pawn.story.traits.allTraits.Count == 0 && !s.Pawn.needs.AllNeeds.OfType<Need_Chemical>().Any() &&
                !s.Pawn.health.hediffSet.hediffs.Any(h => h is Hediff_Addiction or Hediff_ChemicalDependency or Hediff_Injury) &&
                (int)s.Pawn.needs.food.CurCategory < 1 && ReferenceEquals(PolicyField.GetValue(s.Pawn.drugs), s.Policy) &&
                policies.Contains(s.Policy) && game.drugPolicyDatabase.AllPolicies.Contains(s.Policy) && s.Policy.id == s.PolicyId && s.Policy.Count == s.Rows.Length);
            for (int i = 0; i < s.Rows.Length; i++) EndToEndAssert.Equal(
                ReferenceEquals(s.Rows[i].Entry, s.Entry) ? s.Rows[i] with { Choice = s.Choice } : s.Rows[i], NativeRow.From(s.Policy[i]));
            EndToEndAssert.Equal(s.Choice, NativeDrugToleranceState.ReadEncoded(s.Entry));
            if (CaravanCase) EndToEndAssert.True(!s.Pawn.Spawned && s.Pawn.CurJob == null && s.Caravan!.Spawned && !s.Caravan.Destroyed &&
                s.Caravan.Tile == s.Tile && !s.Caravan.pather.MovingNow && s.Caravan.PawnsListForReading.Count == 1 && s.Caravan.PawnsListForReading[0] == s.Pawn);
            else EndToEndAssert.True(s.Pawn.Spawned && ReferenceEquals(s.Pawn.Map, map));
        }
        if (!callback)
        {
            EndToEndAssert.True(tickScope == null && scheduleScope == null && mapScope == null && decisionScope == null && doseScope == null);
            foreach (var observed in traceBindings) EndToEndAssert.True(PublicationCurrent(observed.Identity));
            ValidateDecisions();
        }
    }

    private void ValidateDecisions()
    {
        EndToEndAssert.True(decisions.Count <= 1024 && doses.Count <= subjects.Count && splits.Count <= subjects.Count * 4);
        foreach (var d in decisions)
        {
            EndToEndAssert.True(d.Closed && d.Postfix && d.Original && d.Error == null && d.Scope.Closed && d.Scope.Original && d.Scope.Error == null &&
                d.AllowedCalls == 1 && d.AllowedCompleted && d.AllowedOriginal && (d.Scope.Parent == null || d.Scope.Parent.Closed && d.Scope.Parent.Original && d.Scope.Parent.Error == null));
            if (!d.NativeAllowed) { EndToEndAssert.False(d.Result); continue; }
            if (d.MoodEnabled && d.Subject.Entry.onlyIfMoodBelow < 1)
            {
                EndToEndAssert.True(d.MoodAvailable && d.PotentialCalls == 1 && d.PotentialClosed && d.PotentialOriginal && d.PotentialError == null &&
                    float.IsFinite(d.Potential) && d.CurrentMood < d.Subject.Entry.onlyIfMoodBelow);
                if (d.Subject == A) EndToEndAssert.True(d.Potential >= .5f && !d.Result, "A's actual recovering-mood native-eligible selection must reject.");
                else if (CaravanCase) EndToEndAssert.True(d.Subject == B && d.Potential < .9f && d.CurrentMood < .9f && d.Result,
                    "Enabled relevant-threshold caravan B must permit the real production reading.");
            }
            else EndToEndAssert.Equal(0, d.PotentialCalls, "Bypass/off selections must not need a potential getter.");
            if (d.ToleranceEnabled && d.ToleranceAvailable && d.Choice == "1:limit:0") EndToEndAssert.False(d.Result);
        }
    }
    private int Rejections(Subject s, bool mood, bool tol) => decisions.Where(d => d.Subject == s && d.Phase == phase &&
        d.NativeAllowed && !d.Result && d.MoodEnabled == mood && d.ToleranceEnabled == tol && d.HistoryBefore.Count == 0)
        .Select(d => d.Tick).Distinct().Count();
    private int IntervalRejections(Subject s) => decisions.Where(d => d.Subject == s && d.Phase == phase && !d.NativeAllowed && !d.Result &&
        d.HistoryBefore.Count == 1).Select(d => d.Tick).Distinct().Count();
    private void Held(Subject s, int count)
    {
        var live = s.Stock.Where(t => !t.Destroyed).ToArray();
        EndToEndAssert.Equal(count, live.Sum(t => t.stackCount), s.Pawn.LabelShort + " physical stock");
        EndToEndAssert.True(live.All(t => t.def == beer && (ReferenceEquals(t.holdingOwner, s.Pawn.inventory.innerContainer) ||
            ReferenceEquals(t.holdingOwner, s.Pawn.carryTracker.innerContainer))), "Every remaining fixture Beer must stay with its exact owner.");
        if (count == 2) EndToEndAssert.True(History(s).Count == 0 && Severity(s, alcohol) == 0 && Severity(s, alcoholTolerance) == 0);
    }
    private void AssertDose(Subject s)
    {
        var dose = doses.Single(d => d.Subject == s);
        EndToEndAssert.True(dose.Closed && dose.Original && dose.Postfix && dose.Error == null && dose.IngestedCalls == 1 &&
            dose.IngestedClosed && dose.IngestedOriginal && dose.IngestedPostfix && dose.IngestedError == null && dose.Before == 2 && dose.After == 1 &&
            dose.HistoryBefore.Count == 0 && dose.HistoryAfter.Count == 1 && dose.HistoryAfter[0].Drug == beer.defName &&
            dose.HistoryAfter[0].Tick == dose.Tick && dose.HistoryAfter[0].Day == day && dose.HistoryAfter[0].Count == 1 &&
            dose.HighBefore == 0 && dose.ToleranceBefore == 0 && dose.HighAfter > 0 && dose.ToleranceAfter > 0 && dose.HistoryAfter.SequenceEqual(History(s)));
        EndToEndAssert.True(dose.Decision.Subject == s && dose.Decision.Closed && dose.Decision.NativeAllowed && dose.Decision.Result);
        if (CaravanCase)
        {
            EndToEndAssert.True(dose.Scope != null && dose.Scope.Closed && dose.Scope.Original && dose.Scope.Error == null &&
                dose.Scope.Parent is { Closed: true, Original: true, Error: null } && dose.Decision.Scope == dose.Scope && dose.Source == s.Source && dose.NutritionWanted == 0);
            var split = splits.Single(p => p.Dose == dose);
            EndToEndAssert.True(split.Source == s.Source && split.Before == 2 && split.Requested == 1 && split.After == 1 &&
                split.Result != null && split.Result != s.Source && split.ResultCount == 1 && split.ResultHolder == null && split.Closed && split.Original && split.Error == null);
        }
        else EndToEndAssert.True(dose.Job == s.SelectedJob && dose.Job == s.VisibleJob && dose.Source == s.VisibleSource &&
            !dose.Job!.playerForced && dose.Job.jobGiver?.GetType() == typeof(JobGiver_TakeDrugsForDrugPolicy));
        Held(s, 1); EndToEndAssert.True(Severity(s, alcohol) > 0 && Severity(s, alcoholTolerance) > 0);
    }

    private void CleanupObjects()
    {
        RequireGame(); var errors = new List<Exception>();
        Attempt(errors, ClaimPendingWindows);
        foreach (var owned in windows.ToArray()) Attempt(errors, () =>
        {
            if (!Find.WindowStack.Windows.Contains(owned)) return;
            if (owned is MainTabWindow_Assign && ReferenceEquals(Find.MainTabsRoot.OpenTab?.TabWindow, owned)) Find.MainTabsRoot.EscapeCurrentTab(false);
            else owned.Close(false);
        });
        Attempt(errors, () => Find.Selector.ClearSelection());
        foreach (var caravan in caravans.AsEnumerable().Reverse()) Attempt(errors, () =>
        { foreach (var pawn in pawns.Where(caravan.ContainsPawn).ToArray()) caravan.RemovePawn(pawn); if (!caravan.Destroyed) caravan.Destroy(); });
        foreach (var pawn in pawns.AsEnumerable().Reverse()) Attempt(errors, () =>
        { if (world.worldPawns.Contains(pawn)) world.worldPawns.RemovePawn(pawn); if (!pawn.Destroyed) pawn.Destroy(DestroyMode.Vanish); });
        foreach (var thing in things) Attempt(errors, () => { if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish); });
        foreach (var policy in policies) Attempt(errors, () => { if (policy.label.StartsWith(marker, StringComparison.Ordinal)) game.drugPolicyDatabase.AllPolicies.Remove(policy); });
        if (errors.Count != 0) throw new AggregateException("Owned scheduled-mood objects failed cleanup.", errors);
    }
    private void ClaimPendingWindows()
    {
        RequireGame();
        // A native action may open its window before a failed provider result prevents the next capture.
        // The fixture starts with these surfaces absent; claim only exact supported new types in this Game.
        var pending = Find.WindowStack.Windows.Where(w => !baselineWindows.Contains(w) && w is
            Dialog_ModSettings or Dialog_ManageDrugPolicies or MainTabWindow_Assign or MainTabWindow_Menu or
            Dialog_Options or Dialog_ResolutionConfirm or FloatMenu or EditWindow_Log).ToArray();
        EndToEndAssert.True(pending.GroupBy(w => w.GetType()).All(g => g.Count() == 1), "Ambiguous pending native fixture window.");
        foreach (var owned in pending) windows.Add(owned);
    }
    private IReadOnlyDictionary<string, string> Evidence()
    {
        var result = new Dictionary<string, string> { ["phase"] = phase, ["scenario"] = scenario.ToString(),
            ["setupWrites"] = "disposable adult pawns; clear traits/health/memories; native NewColonyOptimism +10/8d; food/rest/joy1; mood0.40; work disabled/Anything; separate scheduled Beer rows; original stock2; map drafted or stationary caravan; no later fixture gameplay writes",
            ["gameTick"] = Find.TickManager.TicksGame.ToString(), ["initialTick"] = initialTick.ToString(), ["day"] = day.ToString(),
            ["moodEnabled"] = expectedMood.ToString(), ["toleranceEnabled"] = expectedTolerance.ToString(),
            ["sharedAvailable"] = scheduled.Available.ToString(), ["toleranceAvailable"] = tolerance.Available.ToString(),
            ["traceFailure"] = traceFailure ?? "none", ["nativeOperations"] = actions.ToString(), ["screenshots"] = screenshots.ToString(), ["waitSeconds"] = waitSeconds.ToString() };
        foreach (var s in subjects) result[s.Pawn.ThingID] = "policy=" + s.Policy.id + "; choice=" + (s.Choice ?? "Any absent") +
            "; threshold=" + s.Entry.onlyIfMoodBelow.ToString("R") + "; preparedPotential=" + s.PreparedPotential.ToString("R") + "; actualThoughts=" + s.Thoughts +
            "; drafted=" + s.Pawn.Drafted + "; liveStock=" + s.Stock.Where(t => !t.Destroyed).Sum(t => t.stackCount) + "; history=" + string.Join(",", History(s));
        result["decisions"] = string.Join("\n", decisions.Select(d => $"{d.Subject.Pawn.ThingID}; tick={d.Tick}; phase={d.Phase}; nativeAllowed={d.NativeAllowed}; finalShould={d.Result}; current={d.CurrentMood:R}; potential={d.Potential:R}; reads={d.PotentialCalls}; moodOn={d.MoodEnabled}; moodAvailable={d.MoodAvailable}; toleranceOn={d.ToleranceEnabled}; toleranceAvailable={d.ToleranceAvailable}; scope={d.Scope.Kind}; closed={d.Closed}; error={d.Error}"));
        result["doses"] = string.Join("\n", doses.Select(d => $"{d.Subject.Pawn.ThingID}; tick={d.Tick}; source={d.Source.ThingID}; units={d.Before}->{d.After}; job={d.Job?.loadID}; closed={d.Closed}; alcohol={d.HighBefore:R}->{d.HighAfter:R}; tolerance={d.ToleranceBefore:R}->{d.ToleranceAfter:R}; history={string.Join(",", d.HistoryAfter)}"));
        return result;
    }
    private void VerifyBudget()
    {
        int scrolls = moodControls.ScrollClicks + toleranceControls.ScrollClicks;
        var expected = scenario switch
        {
            MoodScenario.Map => (Actions: 76, Screenshots: 19, Waits: 330, Scrolls: 12, Limit: 2700),
            MoodScenario.Caravan => (Actions: 80, Screenshots: 15, Waits: 480, Scrolls: 3, Limit: 3000),
            _ => (Actions: 101, Screenshots: 17, Waits: 230, Scrolls: 3, Limit: 2700)
        };
        EndToEndAssert.True(scrolls >= 0 && scrolls <= expected.Scrolls);
        EndToEndAssert.Equal(expected.Actions + 2 * scrolls, actions);
        EndToEndAssert.Equal(expected.Screenshots, screenshots); EndToEndAssert.Equal(expected.Waits, waitSeconds);
        EndToEndAssert.True(actions * 15 + screenshots * 30 + waitSeconds + 90 <= expected.Limit);
    }
    private static FieldInfo Field(Type type, string name) => type.GetField(name, InstanceFields) ?? throw new MissingFieldException(type.FullName, name);
    private static MethodInfo Native(Type type, string name, bool isStatic, bool isPublic, Type returns, params Type[] args)
    {
        var method = type.GetMethod(name, BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, args, null)
            ?? throw new MissingMethodException(type.FullName, name);
        EndToEndAssert.True(method.IsStatic == isStatic && method.IsPublic == isPublic && method.ReturnType == returns); return method;
    }
    private static void Attempt(List<Exception> errors, Action action) { try { action(); } catch (Exception error) { errors.Add(error); } }
    private static float Severity(Subject s, HediffDef def) => s.Pawn.health.hediffSet.GetFirstHediffOfDef(def)?.Severity ?? 0;
    private static List<HistoryRecord> History(Subject s) => ((List<DrugTakeRecord>)HistoryField.GetValue(s.Pawn.drugs)!).Select(r =>
        new HistoryRecord(r.drug.defName, r.lastTakenTicks, (int)DayField.GetValue(r)!, (int)TimesField.GetValue(r)!)).ToList();
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private static TimeControlActionStep Pause(string name) => new(name, true, EndToEndGameSpeed.Normal);
    private static TimeControlActionStep Run(string name) => new(name, false, EndToEndGameSpeed.Normal);
    private static PawnInspectTabActionStep Inspect(Subject s, EndToEndPawnInspectTab tab) => new("show actual " + tab + " of " + s.Pawn.LabelShort, s.Pawn.ThingID, tab);
    private static WaitUntilStep Wait(string name, Func<bool> predicate, int seconds, int ticks = 1) => new(name, _ => predicate(), new EndToEndDeadline(int.MaxValue, ticks, TimeSpan.FromSeconds(seconds)));
    private EndToEndCaravanHealthObservation Health(Subject s) => health.ObserveHealth(s.Handle, s.Pawn.GetUniqueLoadID());
    private static bool DetailOpen() => Find.WindowStack.Windows.Any(w => w is ImmediateWindow && w.ID == -1439870015);
    private NativeWindowCaptureStep Capture<T>(string name, Action<T> receive) where T : Window => new(name, captures =>
    {
        RequireGame(); var actual = Find.WindowStack.Windows.OfType<T>().Single(); windows.Add(actual);
        window = captures.Single(w => w.NativeId == actual.ID && w.RuntimeType == actual.GetType().FullName); receive(actual);
    });
    private NativeWindowClickActionStep Click(string name, Vector2 point) => new(name, window.Handle,
        (window.X + point.x) * window.UiScale, (window.Y + point.y) * window.UiScale, null, pointer);

    // No observer changes native arguments/results; errors are retained and the original exception is returned unchanged.
    private void Observe(Action read) { try { read(); } catch (Exception error) { traceFailure ??= error.GetBaseException().Message; } }
    private Subject? SubjectFor(Pawn pawn) => subjects.SingleOrDefault(s => ReferenceEquals(s.Pawn, pawn));
    private static void BeginTick(Caravan __instance, int __0, out NativeScope? __state)
    {
        __state = null; var f = active; var subject = f?.subjects.SingleOrDefault(s => s.Caravan == __instance); if (f == null || subject == null) return;
        NativeScope? scope = null;
        f.Observe(() => { f.RequireGame(); EndToEndAssert.True(f.running && f.tickScope == null && __0 > 0);
            scope = new(subject, "Caravan.TickInterval(" + __0 + ")", null); f.tickScope = scope; }); __state = scope;
    }
    private static Exception? EndTick(Exception? __exception, bool __runOriginal, NativeScope? __state)
    {
        var f = active; if (f == null || __state == null) return __exception;
        f.Observe(() => { Close(__state, __exception, __runOriginal); EndToEndAssert.True(f.tickScope == __state && f.scheduleScope == null && f.decisionScope == null && f.doseScope == null); });
        if (f.tickScope == __state) f.tickScope = null; return __exception;
    }
    private static void BeginSchedule(Pawn __0, Caravan __1, out NativeScope? __state)
    {
        __state = null; var f = active; var s = f?.SubjectFor(__0); if (f == null || s == null) return;
        NativeScope? scope = null;
        f.Observe(() => { f.RequireCurrent(true); EndToEndAssert.True(f.running && f.tickScope?.Subject == s && s.Caravan == __1 && f.scheduleScope == null);
            scope = new(s, "TryTakeScheduledDrugs", f.tickScope); f.scheduleScope = scope; }); __state = scope;
    }
    private static Exception? EndSchedule(Exception? __exception, bool __runOriginal, NativeScope? __state)
    {
        var f = active; if (f == null || __state == null) return __exception;
        f.Observe(() => { Close(__state, __exception, __runOriginal); EndToEndAssert.True(f.scheduleScope == __state && f.decisionScope == null && f.doseScope == null); });
        if (f.scheduleScope == __state) f.scheduleScope = null; return __exception;
    }
    private static void BeginMap(Pawn __0, MethodBase __originalMethod, out NativeScope? __state)
    {
        __state = null; var f = active; var s = f?.SubjectFor(__0); if (f == null || s == null) return;
        NativeScope? scope = null;
        f.Observe(() => { f.RequireCurrent(true); EndToEndAssert.True(f.running && f.mapScope == null);
            scope = new(s, __originalMethod.Name, null); f.mapScope = scope; }); __state = scope;
    }
    private static void ObserveJob(Job? __result, bool __runOriginal, NativeScope? __state)
    {
        var f = active; if (f == null || __state == null || __result == null) return;
        f.Observe(() =>
        {
            var s = __state.Subject;
            EndToEndAssert.True(__runOriginal && __result.def == JobDefOf.Ingest && !__result.playerForced && __result.count == 1 &&
                s.Stock.Contains(__result.targetA.Thing) && __state.LastDecision is { Result: true, Closed: true });
            EndToEndAssert.True(s.SelectedJob == null, "Only one native dose job may be selected per subject.");
            s.SelectedJob = __result; s.SelectedDecision = __state.LastDecision;
        });
    }
    private static Exception? EndMap(Exception? __exception, bool __runOriginal, NativeScope? __state)
    {
        var f = active; if (f == null || __state == null) return __exception;
        f.Observe(() => { Close(__state, __exception, __runOriginal); EndToEndAssert.True(f.mapScope == __state && f.decisionScope == null); });
        if (f.mapScope == __state) f.mapScope = null; return __exception;
    }
    private static void Close(NativeScope state, Exception? error, bool original)
    { state.Closed = true; state.Original = original; state.Error = error?.GetType().FullName; EndToEndAssert.True(original && error == null); }

    private static void BeginDecision(Pawn_DrugPolicyTracker __instance, ThingDef __0, out Decision? __state)
    {
        __state = null; var f = active; var s = f?.SubjectFor(__instance.pawn);
        if (f == null || s == null || __0 != f.beer || (f.scheduleScope ?? f.mapScope) is not { } parent) return;
        Decision? decision = null;
        f.Observe(() =>
        {
            f.RequireCurrent(true); EndToEndAssert.True(parent.Subject == s && f.decisionScope == null && f.decisions.Count < 1024 && ReferenceEquals(s.Pawn.drugs, __instance));
            decision = new(s, parent, f); f.decisions.Add(decision); f.decisionScope = decision; parent.LastDecision = decision;
        }); __state = decision;
    }
    private static void ObserveDecision(bool __result, bool __runOriginal, Decision? __state)
    { if (__state != null) { __state.Result = __result; __state.Original = __runOriginal; __state.Postfix = true; } }
    private static Exception? EndDecision(Exception? __exception, bool __runOriginal, Decision? __state)
    {
        var f = active; if (f == null || __state == null) return __exception;
        f.Observe(() => { __state.Closed = true; __state.Error = __exception?.GetType().FullName;
            EndToEndAssert.True(f.decisionScope == __state && __state.Postfix && __runOriginal && __exception == null); });
        if (f.decisionScope == __state) f.decisionScope = null; return __exception;
    }
    private static void BeginAllowed(Pawn_DrugPolicyTracker __instance, ThingDef __0, out Decision? __state)
    {
        __state = null; var f = active; if (f?.decisionScope is not { } d || __0 != f.beer) return;
        f.Observe(() => { EndToEndAssert.True(ReferenceEquals(d.Subject.Pawn.drugs, __instance)); d.AllowedCalls++; }); __state = d;
    }
    private static void ObserveAllowed(bool __result, bool __runOriginal, Decision? __state)
    { if (__state != null) { __state.NativeAllowed = __result; __state.AllowedOriginal = __runOriginal; __state.AllowedCompleted = true; } }
    private static void BeginMood(Need_Mood __instance, out Decision? __state)
    {
        __state = null; var f = active; if (f?.decisionScope is not { } d) return;
        f.Observe(() =>
        {
            EndToEndAssert.True(ReferenceEquals(__instance, d.Subject.Pawn.needs.mood) && d.AllowedCompleted && d.AllowedOriginal && d.NativeAllowed &&
                d.MoodEnabled && d.MoodAvailable && d.Subject.Entry.onlyIfMoodBelow < 1 && !d.Closed && __instance.MaxLevel == 1,
                "Only the production finalizer's exact relevant scheduled need read may count; native/UI reads outside that scope are excluded.");
            d.PotentialCalls++;
        }); __state = d;
    }
    private static void ObserveMood(float __result, bool __runOriginal, Decision? __state)
    { if (__state != null) { __state.Potential = __result; __state.PotentialOriginal = __runOriginal; } }
    private static Exception? EndMood(Exception? __exception, bool __runOriginal, Decision? __state)
    {
        if (__state != null) { __state.PotentialClosed = true; __state.PotentialError = __exception?.GetType().FullName;
            active?.Observe(() => EndToEndAssert.True(__runOriginal && __exception == null)); }
        return __exception;
    }

    private static void BeginDrug(Caravan_NeedsTracker __instance, Pawn __0, Thing __1, Pawn __2, out Dose? __state)
    {
        __state = null; var f = active; var s = f?.SubjectFor(__0); if (f == null || s == null) return;
        Dose? dose = null;
        f.Observe(() =>
        {
            f.RequireCurrent(true); EndToEndAssert.True(__instance == s.Caravan!.needs && __1 == s.Source && __2 == s.Pawn &&
                f.scheduleScope?.Subject == s && f.scheduleScope.LastDecision is { Result: true, Closed: true } && f.doseScope == null && f.doses.All(d => d.Subject != s));
            dose = new(s, __1, f.scheduleScope, f.scheduleScope!.LastDecision!, f); f.doses.Add(dose); f.doseScope = dose;
        }); __state = dose;
    }
    private static void ObserveDrug(bool __runOriginal, Dose? __state)
    { if (__state != null) { __state.Original = __runOriginal; __state.Postfix = true; } }
    private static Exception? EndDrug(Exception? __exception, bool __runOriginal, Dose? __state)
    {
        var f = active; if (f == null || __state == null) return __exception;
        f.Observe(() => { __state.Closed = true; __state.Error = __exception?.GetType().FullName;
            EndToEndAssert.True(f.doseScope == __state && __state.IngestedClosed && __state.Postfix && __runOriginal && __exception == null); });
        if (f.doseScope == __state) f.doseScope = null; return __exception;
    }
    private static void BeginIngested(Thing __instance, Pawn __0, float __1, out Dose? __state)
    {
        __state = null; var f = active; var s = f?.SubjectFor(__0);
        if (f == null || s == null && !f.things.Contains(__instance)) return;
        Dose? dose = null;
        f.Observe(() =>
        {
            f.RequireCurrent(true); EndToEndAssert.True(s != null && s.Stock.Contains(__instance));
            if (f.CaravanCase)
            {
                EndToEndAssert.True(f.doseScope != null && f.doseScope.Source == __instance && f.doseScope.Subject == s &&
                    f.doseScope.Scope == f.scheduleScope && __1 == 0); dose = f.doseScope;
            }
            else
            {
                EndToEndAssert.True(s!.SelectedJob != null && ReferenceEquals(s.Pawn.CurJob, s.SelectedJob) &&
                    ReferenceEquals(s.VisibleJob, s.SelectedJob) && ReferenceEquals(s.VisibleSource, __instance) &&
                    s.SelectedDecision is { Result: true, Closed: true } && f.doseScope == null && f.doses.All(d => d.Subject != s));
                dose = new(s, __instance, null, s.SelectedDecision!, f); f.doses.Add(dose); f.doseScope = dose;
            }
            EndToEndAssert.Equal(0, dose!.IngestedCalls); dose.IngestedCalls++; dose.NutritionWanted = __1;
        }); __state = dose;
    }
    private static void ObserveIngested(bool __runOriginal, Dose? __state)
    {
        var f = active; if (f == null || __state == null) return;
        f.Observe(() => { __state.IngestedPostfix = true; __state.IngestedOriginal = __runOriginal;
            __state.After = __state.Subject.Stock.Where(t => !t.Destroyed).Sum(t => t.stackCount);
            __state.HistoryAfter = History(__state.Subject); __state.HighAfter = Severity(__state.Subject, f.alcohol);
            __state.ToleranceAfter = Severity(__state.Subject, f.alcoholTolerance);
            if (!f.CaravanCase) { __state.Postfix = true; __state.Original = __runOriginal; }
        });
    }
    private static Exception? EndIngested(Exception? __exception, bool __runOriginal, Dose? __state)
    {
        var f = active; if (f == null || __state == null) return __exception;
        f.Observe(() => { __state.IngestedClosed = true; __state.IngestedError = __exception?.GetType().FullName;
            EndToEndAssert.True(f.doseScope == __state && __state.IngestedPostfix && __runOriginal && __exception == null);
            if (!f.CaravanCase) { __state.Closed = true; __state.Error = __exception?.GetType().FullName; }
        });
        if (!f.CaravanCase && f.doseScope == __state) f.doseScope = null; return __exception;
    }
    private static void BeginSplit(ThingWithComps __instance, int __0, out Split? __state)
    {
        __state = null; var f = active; if (f == null || !f.things.Contains(__instance)) return;
        Split? split = null;
        f.Observe(() =>
        {
            var s = f.subjects.Single(subject => subject.Stock.Contains(__instance));
            EndToEndAssert.True(f.running && __0 == 1 && f.splits.Count < f.subjects.Count * 4);
            if (f.CaravanCase) EndToEndAssert.True(f.doseScope?.Subject == s && f.doseScope.Source == __instance && f.doseScope.IngestedCalls == 1 && !f.doseScope.IngestedClosed);
            else EndToEndAssert.True(s.SelectedJob != null && ReferenceEquals(s.Pawn.CurJob, s.SelectedJob), "A map split must belong to the selected native dose job.");
            split = new(s, f.doseScope, __instance, __0); f.splits.Add(split);
        }); __state = split;
    }
    private static void ObserveSplit(Thing __result, bool __runOriginal, Split? __state)
    {
        var f = active; if (f == null || __state == null) return;
        if (__result != null)
        {
            f.things.Add(__result);
            // Partial caravan ingestion discards this split; retain cleanup ownership, not stock.
            if (!f.CaravanCase) __state.Subject.Stock.Add(__result);
        }
        f.Observe(() => { __state.Original = __runOriginal; __state.Postfix = true; __state.Result = __result;
            __state.ResultCount = __result?.stackCount ?? -1; __state.ResultHolder = __result?.ParentHolder; __state.After = __state.Source.stackCount; });
    }
    private static Exception? EndSplit(Exception? __exception, bool __runOriginal, Split? __state)
    {
        if (__state != null) { __state.Closed = true; __state.Error = __exception?.GetType().FullName;
            active?.Observe(() => EndToEndAssert.True(__state.Postfix && __runOriginal && __exception == null)); } return __exception;
    }

    // The accepted caravan observer's publication proof is reused unchanged for the native Allowed boundary.
    private static readonly Type PublicationType = typeof(DrugToleranceDecisionPatch).Assembly.GetType("Megamod.Patching.HarmonyPublication", true)!;
    private static readonly MethodInfo InspectPublication = PublicationType.GetMethod("Inspect", BindingFlags.Static | BindingFlags.NonPublic)!;
    private static object Property(object value, string name) => value.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(value)!;
    private static object[] Registrations(object snapshot, string name) => ((IEnumerable)Property(snapshot, name)).Cast<object>().ToArray();
    private static bool InspectPublicationOf(MethodInfo target, Func<object, bool> visitor)
    {
        var callback = visitor.Method.CreateDelegate(InspectPublication.GetParameters()[1].ParameterType, visitor.Target);
        return (bool)InspectPublication.Invoke(null, new object[] { target, callback })!;
    }
    private static bool PublicationCurrent(object identity) => (bool)identity.GetType().GetMethod("IsCurrent", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(identity, null)!;
    private void Install(MethodInfo target, string prefix, string? postfix, string? finalizer)
    {
        MethodInfo Callback(string name) => typeof(ScheduledMoodFixture).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;
        var observed = new TraceBinding(target, Callback(prefix), postfix == null ? null : Callback(postfix), finalizer == null ? null : Callback(finalizer));
        traceBindings.Add(observed); // Cleanup knows the target even if Patch or admission fails part way.
        traceOwner.Patch(target, new HarmonyMethod(observed.Prefix), observed.Postfix == null ? null : new HarmonyMethod(observed.Postfix),
            finalizer: observed.Finalizer == null ? null : new HarmonyMethod(observed.Finalizer));
        EndToEndAssert.True(InspectPublicationOf(target, snapshot =>
        {
            var prefixes = Registrations(snapshot, "Prefixes"); var postfixes = Registrations(snapshot, "Postfixes"); var finalizers = Registrations(snapshot, "Finalizers");
            var all = prefixes.Concat(postfixes).Concat(finalizers).ToArray();
            EndToEndAssert.True(all.All(value => !(bool)Property(value, "IsFactory")));
            void Slot(object[] values, MethodInfo? expected)
            {
                var own = values.Where(value => (string)Property(value, "Owner") == traceOwner.Id).ToArray();
                EndToEndAssert.True(expected == null ? own.Length == 0 : own.Length == 1 && (MethodInfo)Property(own[0], "Method") == expected);
            }
            Slot(prefixes, observed.Prefix); Slot(postfixes, observed.Postfix); Slot(finalizers, observed.Finalizer);
            var transpilers = (Patch[])Property(snapshot, "Transpilers");
            EndToEndAssert.Equal(0, ((Array)snapshot.GetType().GetField("effects", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(snapshot)!).Length);
            if (target == AllowedTarget)
            {
                MethodInfo Product(string name) => typeof(DrugToleranceDecisionPatch).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;
                MethodInfo[] Sorted(object[] values) => PatchProcessor.GetSortedPatchMethods(target, values.Select(value =>
                    (Patch)value.GetType().GetMethod("ToOrderingPatch", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(value, null)!).ToArray()).ToArray();
                EndToEndAssert.True(prefixes.Length == 2 && postfixes.Length == 2 && finalizers.Length == 1 && transpilers.Length == 1 &&
                    transpilers[0].owner == "fumblesneeze.megamod" && transpilers[0].PatchMethod == Product("Observe") &&
                    all.Where(value => (string)Property(value, "Owner") != traceOwner.Id).All(value => (string)Property(value, "Owner") == "fumblesneeze.megamod") &&
                    Sorted(prefixes).Last() == Product("ScheduledPrefix") && Sorted(postfixes).First() == Product("Postfix") &&
                    Sorted(postfixes).Last() == observed.Postfix && Sorted(finalizers).Single() == Product("Finalizer"));
            }
            else EndToEndAssert.True(all.All(value => (string)Property(value, "Owner") == traceOwner.Id) && transpilers.Length == 0,
                "Exact Core-only provenance excludes an unproved foreign callback composition.");
            observed.Identity = Property(snapshot, "Identity"); return true;
        }));
        EndToEndAssert.True(scheduled.Available && tolerance.Available, "Passive observation must preserve both actual admissions.");
    }
    private void CleanupTrace()
    {
        if (ReferenceEquals(active, this)) active = null;
        var errors = new List<Exception>();
        foreach (var observed in traceBindings) Attempt(errors, () => traceOwner.Unpatch(observed.Target, HarmonyPatchType.All, traceOwner.Id));
        foreach (var observed in traceBindings) Attempt(errors, () => EndToEndAssert.True(InspectPublicationOf(observed.Target, snapshot =>
            new[] { "Prefixes", "Postfixes", "Finalizers" }.All(name => Registrations(snapshot, name).All(value => (string)Property(value, "Owner") != traceOwner.Id)))));
        tickScope = scheduleScope = mapScope = null; decisionScope = null; doseScope = null;
        if (ReferenceEquals(Current.Game, game)) Attempt(errors, () => EndToEndAssert.True(scheduled.Available,
            "Removing only passive observers must retain the actual shared scheduled capability."));
        if (errors.Count != 0) throw new AggregateException("Scheduled-mood trace cleanup failed.", errors);
    }

    private sealed class Subject
    {
        internal readonly Pawn Pawn; internal readonly DrugPolicy Policy; internal readonly DrugPolicyEntry Entry; internal readonly Thing Source;
        internal readonly Caravan? Caravan; internal readonly PlanetTile Tile; internal readonly int PolicyId; internal readonly NativeRow[] Rows;
        internal readonly HashSet<Thing> Stock = new(); internal readonly string Thoughts; internal readonly float PreparedPotential;
        internal string? Choice; internal string Handle = null!; internal Job? SelectedJob, VisibleJob; internal Thing? VisibleSource; internal Decision? SelectedDecision;
        internal Subject(Pawn pawn, DrugPolicy policy, DrugPolicyEntry entry, Thing source, Caravan? caravan, PlanetTile tile, string thoughts, float potential)
        {
            Pawn = pawn; Policy = policy; Entry = entry; Source = source; Caravan = caravan; Tile = tile; PolicyId = policy.id;
            Rows = Enumerable.Range(0, policy.Count).Select(i => NativeRow.From(policy[i])).ToArray(); Stock.Add(source);
            Choice = NativeDrugToleranceState.ReadEncoded(entry); Thoughts = thoughts; PreparedPotential = potential;
        }
    }
    private sealed record NativeRow(DrugPolicyEntry Entry, ThingDef Drug, bool Scheduled, bool Joy, bool Addiction, int Stock, float Frequency, float Mood, float Recreation, string? Choice)
    {
        internal static NativeRow From(DrugPolicyEntry row) => new(row, row.drug, row.allowScheduled, row.allowedForJoy, row.allowedForAddiction,
            row.takeToInventory, row.daysFrequency, row.onlyIfMoodBelow, row.onlyIfJoyBelow, NativeDrugToleranceState.ReadEncoded(row));
    }
    private sealed record HistoryRecord(string Drug, int Tick, int Day, int Count);
    private sealed class NativeScope
    {
        internal readonly Subject Subject; internal readonly string Kind; internal readonly NativeScope? Parent;
        internal Decision? LastDecision; internal bool Closed, Original; internal string? Error;
        internal NativeScope(Subject subject, string kind, NativeScope? parent) { Subject = subject; Kind = kind; Parent = parent; }
    }
    private sealed class Decision
    {
        internal readonly Subject Subject; internal readonly NativeScope Scope; internal readonly int Tick; internal readonly string Phase;
        internal readonly string? Choice; internal readonly float CurrentMood; internal readonly bool MoodEnabled, MoodAvailable, ToleranceEnabled, ToleranceAvailable;
        internal readonly List<HistoryRecord> HistoryBefore;
        internal bool Result, Original, Postfix, Closed, NativeAllowed, AllowedCompleted, AllowedOriginal, PotentialClosed, PotentialOriginal;
        internal int AllowedCalls, PotentialCalls; internal float Potential = float.NaN; internal string? Error, PotentialError;
        internal Decision(Subject subject, NativeScope scope, ScheduledMoodFixture f)
        {
            Subject = subject; Scope = scope; Tick = Find.TickManager.TicksGame; Phase = f.phase; Choice = NativeDrugToleranceState.ReadEncoded(subject.Entry);
            HistoryBefore = History(subject); CurrentMood = subject.Pawn.needs.mood.CurLevelPercentage;
            MoodEnabled = f.settings.Store.GetEffective(PotentialMoodSettings.Enabled); MoodAvailable = f.scheduled.PotentialMood.Available;
            ToleranceEnabled = f.settings.Store.GetEffective(DrugToleranceSettings.Enabled); ToleranceAvailable = f.tolerance.Available;
        }
    }
    private sealed class Dose
    {
        internal readonly Subject Subject; internal readonly Thing Source; internal readonly NativeScope? Scope; internal readonly Decision Decision; internal readonly Job? Job;
        internal readonly int Tick, Before; internal readonly List<HistoryRecord> HistoryBefore; internal List<HistoryRecord> HistoryAfter = new();
        internal readonly float HighBefore, ToleranceBefore; internal float HighAfter, ToleranceAfter, NutritionWanted;
        internal int After, IngestedCalls; internal bool Original, Postfix, Closed, IngestedOriginal, IngestedPostfix, IngestedClosed; internal string? Error, IngestedError;
        internal Dose(Subject subject, Thing source, NativeScope? scope, Decision decision, ScheduledMoodFixture f)
        { Subject = subject; Source = source; Scope = scope; Decision = decision; Job = subject.Pawn.CurJob; Tick = Find.TickManager.TicksGame;
            Before = subject.Stock.Where(t => !t.Destroyed).Sum(t => t.stackCount); HistoryBefore = History(subject);
            HighBefore = Severity(subject, f.alcohol); ToleranceBefore = Severity(subject, f.alcoholTolerance); }
    }
    private sealed class Split
    {
        internal readonly Subject Subject; internal readonly Dose? Dose; internal readonly Thing Source; internal readonly int Before, Requested;
        internal int After, ResultCount; internal Thing? Result; internal IThingHolder? ResultHolder; internal bool Original, Postfix, Closed; internal string? Error;
        internal Split(Subject subject, Dose? dose, Thing source, int count) { Subject = subject; Dose = dose; Source = source; Before = source.stackCount; Requested = count; }
    }
    private sealed class TraceBinding
    {
        internal readonly MethodInfo Target, Prefix; internal readonly MethodInfo? Postfix, Finalizer; internal object Identity = null!;
        internal TraceBinding(MethodInfo target, MethodInfo prefix, MethodInfo? postfix, MethodInfo? finalizer)
        { Target = target; Prefix = prefix; Postfix = postfix; Finalizer = finalizer; }
    }
}
