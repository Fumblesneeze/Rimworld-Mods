using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LudeonTK;
using Megamod.EndToEndTests.DrugPolicyMaintenance;
using Megamod.Features.DrugTolerance;
using Megamod.Features.DrugTolerance.Core;
using Megamod.Features.DrugTolerance.Runtime;
using Megamod.Settings;
using RimWorld;
using RimWorld.Planet;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;
using HarmonyOwner = HarmonyLib.Harmony;

namespace Megamod.EndToEndTests.DrugTolerance;

// 60 actions/captures x15 +13 screenshots x30 +495s waits +90s reserve =1875s.
// The100 counted steps exclude synchronous assertions/checkpoints. Root owns native acceptance.
[RimWorldEndToEndTest("megamod.drug-tolerance.native-caravan-scheduled", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 3000, MaxWallClockSeconds = 2100)]
public sealed class NativeToleranceCaravanTests : IRimWorldEndToEndTest
{
    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
    private static readonly FieldInfo RuntimeField = Field(typeof(MegamodMod), "drugTolerance");
    private static readonly FieldInfo BindingField = Field(typeof(SettingsRuntime), "binding");
    private static readonly FieldInfo PolicyField = Field(typeof(Pawn_DrugPolicyTracker), "curPolicy");
    private static readonly FieldInfo HistoryField = Field(typeof(Pawn_DrugPolicyTracker), "drugTakeRecords");
    private static readonly FieldInfo TimesField = Field(typeof(DrugTakeRecord), "timesTakenThisDayInt");
    private static readonly FieldInfo DayField = Field(typeof(DrugTakeRecord), "thisDay");
    private static readonly MethodInfo TickTarget = Native(typeof(Caravan), "TickInterval", false, false, typeof(void), typeof(int));
    private static readonly MethodInfo ScheduleTarget = Native(typeof(CaravanDrugPolicyUtility), "TryTakeScheduledDrugs", true, false,
        typeof(void), typeof(Pawn), typeof(Caravan));
    private static readonly MethodInfo ShouldTarget = Native(typeof(Pawn_DrugPolicyTracker), "ShouldTryToTakeScheduledNow", false, true, typeof(bool), typeof(ThingDef));
    private static readonly MethodInfo AllowedTarget = Native(typeof(Pawn_DrugPolicyTracker), "AllowedToTakeScheduledNow", false, true, typeof(bool), typeof(ThingDef));
    private static readonly MethodInfo DrugTarget = Native(typeof(Caravan_NeedsTracker), "IngestDrug", false, true, typeof(void), typeof(Pawn), typeof(Thing), typeof(Pawn));
    private static readonly MethodInfo IngestedTarget = Native(typeof(Thing), "Ingested", false, true, typeof(float), typeof(Pawn), typeof(float));
    private static readonly MethodInfo SplitTarget = Native(typeof(ThingWithComps), "SplitOff", false, true, typeof(Thing), typeof(int));
    private static NativeToleranceCaravanTests? active;

    private readonly string marker = "ToleranceCaravan-" + Guid.NewGuid().ToString("N").Substring(0, 8);
    private readonly NativeDrugPolicyView policyView = new();
    private readonly List<Pawn> ownedPawns = new();
    private readonly List<Caravan> ownedCaravans = new();
    private readonly List<DrugPolicy> ownedPolicies = new();
    private readonly HashSet<Thing> ownedBeer = new();
    private readonly List<Window> ownedWindows = new();
    private readonly List<TraceBinding> bindings = new();
    private readonly List<Decision> decisions = new();
    private readonly List<Dose> doses = new();
    private readonly List<Split> splits = new();
    private Game game = null!;
    private World world = null!;
    private Map map = null!;
    private MegamodMod mod = null!;
    private NativeDrugToleranceRuntime runtime = null!;
    private SettingsRuntime settings = null!;
    private SaveSettingsBinding saveBinding = null!;
    private SaveSettingsSnapshot initialSave = null!;
    private GlobalSettingsSnapshot initialGlobals = null!;
    private HarmonyOwner traceOwner = null!;
    private ThingDef beerDef = null!;
    private HediffDef highDef = null!, toleranceDef = null!;
    private Subject a = null!, b = null!;
    private IEndToEndNativeCaravanObservation health = null!;
    private EndToEndNativeWindow window = null!;
    private MainTabWindow_Assign assign = null!;
    private Dialog_ManageDrugPolicies manager = null!;
    private string pointer = null!, view = null!;
    private TickScope? tickScope;
    private ScheduleScope? scheduleScope;
    private Decision? decisionScope;
    private Dose? doseScope;
    private string? traceFailure;
    private bool zeroApplied, running;
    private int startDay, startTick = -1, pausedTick = -1, initialTick, nextTickId;
    private int nativeOperations, screenshots, waitSeconds, countedSteps;

    public void Arrange(IEndToEndContext context)
    {
        game = Current.Game; world = game.World; map = Find.CurrentMap; RequireGame();
        EndToEndAssert.True(active == null && game.PlayerHasControl && Prefs.DevMode && !LongEventHandler.AnyEventNowOrWaiting &&
            Find.MainTabsRoot.OpenTab == null && Find.Selector.SelectedObjects.Count == 0 && !WorldRendererUtility.WorldSelected &&
            !Find.WindowStack.IsOpen<Dialog_ManageDrugPolicies>() && !Find.WindowStack.IsOpen<EditWindow_Log>() &&
            map.mapPawns.AllPawnsSpawned.Count == 0 && world.worldObjects.Caravans.All(c => c.Faction != Faction.OfPlayer),
            "The paired caravan fixture requires settled empty disposable map control, with no competing player caravan.");
        EndToEndAssert.True(PolicyField.FieldType == typeof(DrugPolicy) && HistoryField.FieldType == typeof(List<DrugTakeRecord>) &&
            TimesField.FieldType == typeof(int) && DayField.FieldType == typeof(int) && BindingField.FieldType == typeof(SaveSettingsBinding));
        settings = SettingsRuntime.Instance ?? throw new InvalidOperationException("Common settings unavailable.");
        saveBinding = BindingField.GetValue(settings) as SaveSettingsBinding ?? throw new InvalidOperationException("Current save binding unavailable.");
        initialGlobals = settings.Store.ExportGlobals(); initialSave = settings.Store.ExportSave(saveBinding);
        mod = LoadedModManager.GetMod<MegamodMod>();
        runtime = RuntimeField.GetValue(mod) as NativeDrugToleranceRuntime ?? throw new InvalidOperationException("Tolerance runtime unavailable.");
        health = context.GetRequiredService<IEndToEndNativeCaravanObservation>();
        var oldSpeed = Find.TickManager.CurTimeSpeed;
        context.DeferCleanup(() => { RequireGame(); Find.TickManager.CurTimeSpeed = oldSpeed; });
        context.DeferCleanup(CleanupObjects); // Registered before any fixture resource is acquired.
        Find.TickManager.Pause(); initialTick = Find.TickManager.TicksGame; startDay = GenDate.DaysPassed;
        EndToEndAssert.True(60000 - initialTick % 60000 >= 3000, "The native dose/history comparison must remain within one game day.");
        beerDef = DefDatabase<ThingDef>.GetNamed("Beer"); highDef = DefDatabase<HediffDef>.GetNamed("AlcoholHigh");
        toleranceDef = DefDatabase<HediffDef>.GetNamed("AlcoholTolerance");
        EndToEndAssert.True(beerDef.thingClass == typeof(ThingWithComps) && beerDef.IsDrug && beerDef.IsAddictiveDrug &&
            beerDef.ingestible.defaultNumToIngestAtOnce == 1 && beerDef.stackLimit >= 2 &&
            beerDef.ingestible.outcomeDoers.OfType<IngestionOutcomeDoer_GiveHediff>().Any(value =>
                ReferenceEquals(value.hediffDef, toleranceDef) && value.toleranceChemical?.toleranceHediff == toleranceDef) &&
            beerDef.ingestible.outcomeDoers.OfType<IngestionOutcomeDoer_GiveHediff>().Any(value => ReferenceEquals(value.hediffDef, highDef)));
        var neighbors = new List<PlanetTile>(); world.grid.GetTileNeighbors(map.Tile, neighbors);
        var tiles = neighbors.Where(tile => tile.Valid && tile.Layer == world.grid.Surface && world.pathGrid.Passable(tile)).Distinct().Take(2).ToArray();
        EndToEndAssert.Equal(2, tiles.Length, "Two distinct passable surface tiles are required.");
        a = CreateSubject("A Zero", tiles[0]); b = CreateSubject("B Any", tiles[1]);
        traceOwner = new HarmonyOwner("fumblesneeze.tests.s138.caravan." + marker);
        context.DeferCleanup(CleanupTrace); active = this;
        Install(TickTarget, nameof(BeginTick), null, nameof(EndTick));
        Install(ScheduleTarget, nameof(BeginSchedule), null, nameof(EndSchedule));
        Install(ShouldTarget, nameof(BeginDecision), nameof(ObserveDecision), nameof(EndDecision));
        Install(AllowedTarget, nameof(BeginAllowed), nameof(ObserveAllowed), null);
        Install(DrugTarget, nameof(BeginDrug), nameof(ObserveDrug), nameof(EndDrug));
        Install(IngestedTarget, nameof(BeginIngested), nameof(ObserveIngested), nameof(EndIngested));
        Install(SplitTarget, nameof(BeginSplit), nameof(ObserveSplit), nameof(EndSplit));
        RequireCurrent(); AssertBefore();
    }

    private Subject CreateSubject(string label, PlanetTile tile)
    {
        var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer,
            forceGenerateNewPawn: true, canGeneratePawnRelations: false, allowFood: false, allowAddictions: false,
            fixedBiologicalAge: 25, fixedChronologicalAge: 25, forceNoIdeo: true, forceNoGear: true));
        ownedPawns.Add(pawn); pawn.Name = new NameTriple("Tolerance", label, marker);
        foreach (var hediff in pawn.health.hediffSet.hediffs.ToArray()) pawn.health.RemoveHediff(hediff);
        foreach (var trait in pawn.story.traits.allTraits.ToArray()) pawn.story.traits.RemoveTrait(trait);
        pawn.inventory.innerContainer.ClearAndDestroyContents();
        pawn.needs.food.CurLevelPercentage = 1f; pawn.needs.rest.CurLevelPercentage = 1f;
        pawn.needs.mood.CurLevelPercentage = 1f; pawn.needs.joy.CurLevelPercentage = 1f;
        var policy = game.drugPolicyDatabase.MakeNewDrugPolicy(); ownedPolicies.Add(policy); policy.label = marker + " " + label;
        for (int i = 0; i < policy.Count; i++)
        {
            policy[i].allowScheduled = false; policy[i].allowedForJoy = false;
            policy[i].allowedForAddiction = false; policy[i].takeToInventory = 0;
        }
        var entry = policy[beerDef]; entry.allowScheduled = true; entry.daysFrequency = 1;
        entry.onlyIfMoodBelow = 1; entry.onlyIfJoyBelow = 1; entry.takeToInventory = 2;
        EndToEndAssert.Equal(DrugToleranceChoiceKind.Any, NativeDrugToleranceState.Read(entry).Kind);
        pawn.drugs.CurrentPolicy = policy;
        var beer = ThingMaker.MakeThing(beerDef); ownedBeer.Add(beer); beer.stackCount = 2;
        EndToEndAssert.True(beer.GetStatValue(StatDefOf.Nutrition) > 0 && pawn.inventory.innerContainer.TryAdd(beer));
        var caravan = CaravanMaker.MakeCaravan(new[] { pawn }, Faction.OfPlayer, tile, true); ownedCaravans.Add(caravan);
        caravan.Name = marker + " " + label;
        return new Subject(pawn, caravan, policy, entry, beer, tile);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        foreach (var step in Workflow(context))
        {
            if (step is ScreenshotStep) { screenshots++; countedSteps++; }
            else if (step is WaitUntilStep wait) { waitSeconds += checked((int)wait.Deadline.MaxWallClock.TotalSeconds); countedSteps++; }
            else if (step.Kind == EndToEndStepKind.Act || step is NativeWindowCaptureStep or NativeWorldViewCaptureStep or NativeCaravanCaptureStep or NativeCaravanHealthCaptureStep)
            { nativeOperations++; countedSteps++; }
            if (step.Kind == EndToEndStepKind.Act)
                yield return new AssertionStep("validate current caravan identities before " + step.Name, _ => RequireCurrent());
            yield return step;
        }
        yield return new AssertionStep("verify caravan sequence and deadline accounting", _ =>
        {
            RequireCurrent(); AssertAfter();
            EndToEndAssert.Equal(60, nativeOperations); EndToEndAssert.Equal(13, screenshots);
            EndToEndAssert.Equal(495, waitSeconds); EndToEndAssert.Equal(100, countedSteps);
            EndToEndAssert.Equal(1875, nativeOperations * 15 + screenshots * 30 + waitSeconds + 90);
        });
        yield return new CheckpointStep("paired caravan native result; broader S138 remains open", _ => Evidence());
    }

    private IEnumerable<EndToEndStep> Workflow(IEndToEndContext context)
    {
        yield return Pause("pause the paired caravan preconditions");
        yield return new NativeMainTabActionStep("open Assign for the caravan policies", "Assign", true);
        yield return Capture<MainTabWindow_Assign>("capture the native Assign manager entry", value => assign = value);
        yield return new NativePointerScopeBeginStep("own native policy pointer workflow", value => pointer = value);
        yield return Click("open native Manage drug policies", policyView.ManageButton(assign).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture the current native policy manager", value => manager = value);
        yield return Click("select A's exact caravan policy", policyView.PolicyRow(manager, a.Policy).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture A's selected initial Any row", value => { manager = value; RequireSelected(a); });
        yield return Shot("A caravan policy before the native drag: Any and ordinary daily scheduling");
        yield return NativeToleranceSliderView.Drag("drag A's native tolerance slider to numeric zero", window, pointer, manager, a.Policy, a.Entry, 0);
        yield return new AssertionStep("observe A's actual native zero commit", _ =>
        { EndToEndAssert.Equal("1:limit:0", NativeDrugToleranceState.ReadEncoded(a.Entry)); zeroApplied = true; RequireCurrent(); AssertBefore(); });
        yield return Capture<Dialog_ManageDrugPolicies>("capture A's committed numeric-zero row", value => { manager = value; RequireSelected(a); });
        yield return Shot("A caravan Beer policy shows numeric zero; native schedule and stock target remain unchanged");
        yield return Click("select B's exact caravan policy", policyView.PolicyRow(manager, b.Policy).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture B's selected Any row", value => { manager = value; RequireSelected(b); });
        yield return NativeToleranceSliderView.Drag("retain B's Any through the native slider", window, pointer, manager, b.Policy, b.Entry, 101);
        yield return new AssertionStep("observe B Any without requiring a no-op encoding rewrite", _ =>
        { EndToEndAssert.Equal(DrugToleranceChoiceKind.Any, NativeDrugToleranceState.Read(b.Entry).Kind); b.ChoiceEncoding = NativeDrugToleranceState.ReadEncoded(b.Entry); RequireCurrent(); });
        yield return Capture<Dialog_ManageDrugPolicies>("capture B's retained Any row", value => { manager = value; RequireSelected(b); });
        yield return Shot("B caravan Beer policy retains Any, daily interval and stock target two");
        yield return new WindowCancelActionStep("close the native policy manager", typeof(Dialog_ManageDrugPolicies).FullName!);
        yield return new NativePointerScopeEndStep("restore the policy pointer", pointer);
        yield return new NativeMainTabActionStep("close Assign after policy choices", "Assign", false);

        var renderer = world.renderer; var surface = world.grid.Surface;
        var camera = WorldCameraManager.WorldCamera; var driver = WorldCameraManager.WorldCameraDriver;
        EndToEndAssert.True(surface.WorldDrawLayers.Count <= 128);
        var terrain = surface.WorldDrawLayers.OfType<WorldDrawLayer_Terrain>().ToArray();
        EndToEndAssert.True(terrain.Length > 0, "The Core surface terrain must be observable before opening World.");
        yield return new NativeWorldViewCaptureStep("capture the complete inactive World baseline", value => view = value);
        yield return new NativeWorldViewActionStep("open World through its native command", view, true);
        yield return Wait("wait for native terrain generation and settled World control", () =>
            ReferenceEquals(Current.Game, game) && ReferenceEquals(game.World, world) && ReferenceEquals(Find.CurrentMap, map) &&
            ReferenceEquals(world.renderer, renderer) && ReferenceEquals(world.grid.Surface, surface) &&
            ReferenceEquals(WorldCameraManager.WorldCamera, camera) && ReferenceEquals(WorldCameraManager.WorldCameraDriver, driver) &&
            renderer.wantedMode == WorldRenderMode.Planet && WorldRendererUtility.WorldSelected && surface.Visible &&
            camera.gameObject.activeSelf && driver.isActiveAndEnabled && !LongEventHandler.AnyEventNowOrWaiting && game.PlayerHasControl &&
            terrain.All(layer => surface.WorldDrawLayers.Contains(layer) && !layer.Dirty), 30);
        yield return new NativeCaravanCaptureStep("capture exact caravan A", view, a.Caravan.GetUniqueLoadID(), value => a.Handle = value);
        yield return new NativeCaravanCaptureStep("capture exact caravan B", view, b.Caravan.GetUniqueLoadID(), value => b.Handle = value);
        foreach (var step in ObserveSubject(a, "before")) yield return step;
        foreach (var step in ObserveSubject(b, "before")) yield return step;
        yield return new CheckpointStep("both prepared stocks and native Health before world time", _ => { AssertBefore(); return Evidence(); });

        context.DeferCleanup(() => { RequireGame(); Find.TickManager.Pause(); });
        RequireCurrent(); AssertBefore(); running = true; startTick = Find.TickManager.TicksGame;
        yield return new TimeControlActionStep("allow ordinary native world scheduling", false, EndToEndGameSpeed.Normal);
        yield return Wait("observe actual zero refusals, one Any dose and later daily-interval refusals", () =>
        {
            RequireCurrent(); AssertProgress();
            return Find.TickManager.TicksGame - startTick >= 600 && doses.Count == 1 &&
                decisions.Where(value => ReferenceEquals(value.Subject, a) && !value.Result).Select(value => value.Tick).Distinct().Count() >= 2 &&
                decisions.Where(value => ReferenceEquals(value.Subject, b) && value.HistoryBefore.Count == 1 && !value.Result && !value.NativeAllowed)
                    .Select(value => value.Tick).Distinct().Count() >= 2;
        }, 90, 2400);
        yield return Pause("pause the observed native caravan result");
        pausedTick = Find.TickManager.TicksGame;
        yield return new CheckpointStep("native tick-to-dose provenance and unchanged daily interval", _ => { AssertAfter(); return Evidence(); });
        foreach (var step in ObserveSubject(b, "after")) yield return step;
        foreach (var step in ObserveSubject(a, "after")) yield return step;
        yield return new NativeWorldViewActionStep("close native World after the paired comparison", view, false);
        yield return new NativeWorldViewRestoreStep("restore the captured complete World baseline", view);
        yield return Wait("wait for the restored map and absent owned detail", () => !WorldRendererUtility.WorldSelected &&
            !world.UI.inspectPane.IsOpen && !WorldCameraManager.WorldCamera.gameObject.activeSelf && !DetailWindowOpen());
        yield return Shot("restored native map after paired caravan scheduled consumption");
        yield return new NativeDeveloperLogActionStep("open native developer log after caravan consumption", true);
        yield return Capture<EditWindow_Log>("capture the exact native developer log", _ => { });
        yield return Shot("native developer console after the paired caravan workflow");
        yield return new NativeDeveloperLogActionStep("restore the closed native developer log", false);
    }

    private IEnumerable<EndToEndStep> ObserveSubject(Subject subject, string phase)
    {
        string label = subject.Pawn.Name.ToStringShort + " " + phase;
        yield return new NativeCaravanSelectStep("select " + label, subject.Handle);
        yield return Wait("wait for exact native caravan selection " + label, () => Selected(subject) && world.UI.inspectPane.IsOpen);
        yield return new NativeCaravanTabStep("open native Items " + label, subject.Handle, EndToEndCaravanTab.Items, true);
        yield return Wait("wait for native Items " + label, () => Tab(subject, typeof(WITab_Caravan_Items)));
        yield return Shot(label + " native Items: " + (phase == "after" && ReferenceEquals(subject, b) ? "one remaining Beer" : "two untouched Beer"));
        yield return new NativeCaravanTabStep("open native Health " + label, subject.Handle, EndToEndCaravanTab.Health, true);
        yield return Wait("wait for fresh Health row with detail closed " + label, () => Health(subject).RowVisible && !Health(subject).DetailVisible);
        string row = null!;
        yield return new NativeCaravanHealthCaptureStep("capture exact native Health row " + label, subject.Handle, subject.Pawn.GetUniqueLoadID(), value => row = value);
        yield return new NativeCaravanHealthClickStep("click native detailed Health open " + label, row);
        yield return Wait("observe completed native Health detail " + label, () => Health(subject).DetailVisible);
        yield return Shot(label + " native detailed Health: " + (phase == "after" && ReferenceEquals(subject, b) ? "actual Alcohol effects" : "no Alcohol effects"));
        yield return new NativeCaravanHealthCaptureStep("recapture native row for detail closure " + label, subject.Handle, subject.Pawn.GetUniqueLoadID(), value => row = value);
        yield return new NativeCaravanHealthClickStep("close native detailed Health " + label, row);
        yield return Wait("observe native detail closed " + label, () => Health(subject).RowVisible && !Health(subject).DetailVisible && !DetailWindowOpen());
        yield return new NativeCaravanTabStep("close native Health tab " + label, subject.Handle, EndToEndCaravanTab.Health, false);
        yield return Wait("observe no expanded native tab " + label, () => Selected(subject) && world.UI.inspectPane.OpenTabType == null);
    }

    private void RequireGame() => EndToEndAssert.True(ReferenceEquals(Current.Game, game) && ReferenceEquals(game.World, world) &&
        ReferenceEquals(Find.CurrentMap, map) && Current.ProgramState == ProgramState.Playing, "Refuse a departed Game, World or map.");
    private void RequireCurrent(bool inCallback = false)
    {
        RequireGame(); EndToEndAssert.True(traceFailure == null, traceFailure ?? string.Empty);
        EndToEndAssert.True(ReferenceEquals(RuntimeField.GetValue(mod), runtime) && runtime.Available && settings.SaveLifecycleAvailable &&
            ReferenceEquals(BindingField.GetValue(settings), saveBinding) && !saveBinding.IsRevoked && settings.Store.GetEffective(DrugToleranceSettings.Enabled));
        EndToEndAssert.True(initialGlobals.Values.OrderBy(p => p.Key).SequenceEqual(settings.Store.ExportGlobals().Values.OrderBy(p => p.Key)) &&
            initialSave.Values.OrderBy(p => p.Key).SequenceEqual(settings.Store.ExportSave(saveBinding).Values.OrderBy(p => p.Key)), "Common settings must remain unchanged.");
        EndToEndAssert.Equal(startDay, GenDate.DaysPassed);
        EndToEndAssert.True(map.mapPawns.AllPawnsSpawned.Count == 0 && world.worldObjects.Caravans.Where(c => c.Faction == Faction.OfPlayer)
            .OrderBy(c => c.ID).SequenceEqual(ownedCaravans.OrderBy(c => c.ID)));
        RequireSubject(a); RequireSubject(b);
        if (!inCallback)
        {
            EndToEndAssert.True(tickScope == null && scheduleScope == null && decisionScope == null && doseScope == null, "Native trace scopes must retire before an observation boundary.");
            foreach (var binding in bindings) EndToEndAssert.True(PublicationCurrent(binding.Identity), "Native patch publication changed after trace admission.");
        }
    }
    private void RequireSubject(Subject subject)
    {
        var pawn = subject.Pawn; var caravan = subject.Caravan;
        EndToEndAssert.True(!pawn.Spawned && !pawn.Destroyed && !pawn.Dead && !pawn.Downed && !pawn.InMentalState && pawn.CurJob == null &&
            pawn.carryTracker.CarriedThing == null && pawn.story.traits.allTraits.Count == 0 && !pawn.needs.AllNeeds.OfType<Need_Chemical>().Any() &&
            !pawn.health.hediffSet.hediffs.Any(h => h is Hediff_Addiction or Hediff_ChemicalDependency or Hediff_Injury) && (int)pawn.needs.food.CurCategory < 1);
        EndToEndAssert.True(caravan.Spawned && !caravan.Destroyed && caravan.Faction == Faction.OfPlayer && caravan.Tile == subject.Tile &&
            !caravan.pather.MovingNow && caravan.PawnsListForReading.Count == 1 && ReferenceEquals(caravan.PawnsListForReading[0], pawn) &&
            world.worldObjects.Caravans.Contains(caravan) && ReferenceEquals(PolicyField.GetValue(pawn.drugs), subject.Policy) &&
            subject.Policy.id == subject.PolicyId && game.drugPolicyDatabase.AllPolicies.Contains(subject.Policy) && subject.Policy.Count == subject.Rows.Length);
        for (int i = 0; i < subject.Rows.Length; i++) subject.Rows[i].Require(subject.Policy[i], subject.Entry);
        EndToEndAssert.True(subject.Rows.Count(row => ReferenceEquals(row.Entry, subject.Entry) && ReferenceEquals(row.Drug, beerDef)) == 1);
        var choice = NativeDrugToleranceState.Read(subject.Entry);
        if (ReferenceEquals(subject, a) && zeroApplied) EndToEndAssert.True(choice.Kind == DrugToleranceChoiceKind.Limit && choice.Limit == 0);
        else EndToEndAssert.True(choice.Kind == DrugToleranceChoiceKind.Any && choice.Encoding == subject.ChoiceEncoding);
    }

    private void AssertBefore()
    {
        RequireCurrent(); EndToEndAssert.Equal(initialTick, Find.TickManager.TicksGame);
        EndToEndAssert.True(decisions.Count == 0 && doses.Count == 0 && splits.Count == 0);
        foreach (var subject in new[] { a, b }) { Held(subject, 2); NoEffects(subject); }
    }
    private void AssertProgress()
    {
        Held(a, 2); NoEffects(a);
        EndToEndAssert.True(decisions.Count <= 256 && doses.Count <= 1 && splits.Count <= 1);
        foreach (var decision in decisions)
        {
            EndToEndAssert.True(decision.Closed && decision.Postfix && decision.Original && decision.Error == null && decision.AllowedCalls == 1 &&
                decision.AllowedCompleted && decision.AllowedOriginal && decision.Scope.Closed && decision.Scope.Original && decision.Scope.Error == null &&
                decision.Scope.Parent.Closed && decision.Scope.Parent.Original && decision.Scope.Parent.Error == null && decision.Tick >= startTick);
            if (ReferenceEquals(decision.Subject, a)) EndToEndAssert.True(decision.Choice == "1:limit:0" && decision.NativeAllowed && !decision.Result && decision.HistoryBefore.Count == 0);
            else EndToEndAssert.True(decision.Subject == b && decision.AnyChoice &&
                (decision.HistoryBefore.Count == 0 ? decision.NativeAllowed && decision.Result : !decision.NativeAllowed && !decision.Result && decision.HistoryBefore.Count == 1));
        }
        if (doses.Count == 0) { Held(b, 2); NoEffects(b); EndToEndAssert.Equal(0, splits.Count); return; }
        var dose = doses.Single(); var history = SnapshotHistory(b);
        EndToEndAssert.True(dose.Subject == b && ReferenceEquals(dose.Source, b.Beer) && ReferenceEquals(dose.Owner, b.Pawn) &&
            dose.Scope.Subject == b && dose.Decision.Result && dose.Decision.Scope == dose.Scope && dose.Decision.Closed &&
            dose.Closed && dose.Postfix && dose.Original && dose.Error == null && dose.IngestedCalls == 1 &&
            dose.IngestedClosed && dose.IngestedPostfix && dose.IngestedOriginal && dose.IngestedError == null && dose.NutritionWanted == 0 &&
            dose.Before == 2 && dose.After == 1 && dose.HistoryBefore.Count == 0 && dose.HistoryAfter.Count == 1 &&
            dose.HighBefore == 0 && dose.ToleranceBefore == 0 && dose.HighAfter > 0 && dose.ToleranceAfter > 0 &&
            dose.HistoryAfter.SequenceEqual(history) && history.Count == 1 && history[0].Drug == beerDef.defName &&
            history[0].Tick == dose.Tick && history[0].Day == startDay && history[0].Count == 1 &&
            dose.Scope.Closed && dose.Scope.Parent.Closed && dose.Scope.Parent.Original && dose.Scope.Parent.Error == null);
        Held(b, 1); EndToEndAssert.True(Severity(b, highDef) > 0 && Severity(b, toleranceDef) > 0);
        var split = splits.Single();
        EndToEndAssert.True(split.Dose == dose && split.Source == b.Beer && split.Before == 2 && split.Requested == 1 && split.After == 1 &&
            split.Result != null && split.Result != b.Beer && split.Result.def == beerDef && split.ResultCount == 1 &&
            split.ResultHolder == null && split.Closed && split.Postfix && split.Original && split.Error == null);
        EndToEndAssert.Equal(3, a.Pawn.inventory.innerContainer.TotalStackCountOfDef(beerDef) + b.Pawn.inventory.innerContainer.TotalStackCountOfDef(beerDef));
    }
    private void AssertAfter()
    {
        RequireCurrent(); AssertProgress(); EndToEndAssert.Equal(1, doses.Count);
        EndToEndAssert.True(pausedTick - startTick >= 600 && pausedTick - startTick <= 3000 && Find.TickManager.TicksGame == pausedTick);
        EndToEndAssert.True(decisions.Count(d => d.Subject == b && d.Result) == 1 &&
            decisions.Where(d => d.Subject == a && !d.Result).Select(d => d.Tick).Distinct().Count() >= 2 &&
            decisions.Where(d => d.Subject == b && d.HistoryBefore.Count == 1 && !d.NativeAllowed && !d.Result).Select(d => d.Tick).Distinct().Count() >= 2);
    }
    private void Held(Subject subject, int count) => EndToEndAssert.True(!subject.Beer.Destroyed && !subject.Beer.Spawned && subject.Beer.stackCount == count &&
        ReferenceEquals(subject.Beer.holdingOwner, subject.Pawn.inventory.innerContainer) && subject.Pawn.inventory.innerContainer.Count == 1 &&
        ReferenceEquals(subject.Pawn.inventory.innerContainer[0], subject.Beer));
    private void NoEffects(Subject subject) => EndToEndAssert.True(SnapshotHistory(subject).Count == 0 && subject.Pawn.health.hediffSet.hediffs.Count == 0);
    private static float Severity(Subject subject, HediffDef def) => subject.Pawn.health.hediffSet.GetFirstHediffOfDef(def)?.Severity ?? 0;
    private static List<History> SnapshotHistory(Subject subject) => ((List<DrugTakeRecord>)HistoryField.GetValue(subject.Pawn.drugs)!)
        .Select(value => new History(value.drug.defName, value.lastTakenTicks, (int)DayField.GetValue(value)!, (int)TimesField.GetValue(value)!)).ToList();

    // Callbacks never mutate native arguments/results or throw into gameplay. Context is retired in finalizers.
    private void Observe(Action observation)
    { try { observation(); } catch (Exception error) { traceFailure ??= error.GetBaseException().Message; } }
    private Subject? SubjectFor(Pawn pawn) => ReferenceEquals(pawn, a.Pawn) ? a : ReferenceEquals(pawn, b.Pawn) ? b : null;
    private Subject? SubjectFor(Caravan caravan) => ReferenceEquals(caravan, a.Caravan) ? a : ReferenceEquals(caravan, b.Caravan) ? b : null;
    private static void BeginTick(Caravan __instance, int __0, out TickScope? __state)
    {
        __state = null; var f = active; if (f == null || f.SubjectFor(__instance) is not { } subject) return;
        TickScope? state = null;
        f.Observe(() => { f.RequireGame(); EndToEndAssert.True(f.running && f.tickScope == null && __0 > 0); state = new TickScope(subject, ++f.nextTickId, Find.TickManager.TicksGame, __0); f.tickScope = state; subject.TickCalls++; });
        __state = state;
    }
    private static Exception? EndTick(Exception? __exception, bool __runOriginal, TickScope? __state)
    {
        var f = active; if (f == null || __state == null) return __exception;
        f.Observe(() => { __state.Closed = true; __state.Original = __runOriginal; __state.Error = __exception?.GetType().FullName;
            EndToEndAssert.True(ReferenceEquals(f.tickScope, __state) && f.scheduleScope == null && f.decisionScope == null && f.doseScope == null && __runOriginal && __exception == null); });
        if (ReferenceEquals(f.tickScope, __state)) f.tickScope = null;
        return __exception;
    }
    private static void BeginSchedule(Pawn __0, Caravan __1, out ScheduleScope? __state)
    {
        __state = null; var f = active; if (f == null || f.SubjectFor(__0) is not { } subject) return;
        ScheduleScope? state = null;
        f.Observe(() => { f.RequireCurrent(true); EndToEndAssert.True(f.running && f.tickScope?.Subject == subject && subject.Caravan == __1 && f.scheduleScope == null);
            state = new ScheduleScope(subject, f.tickScope!); f.scheduleScope = state; subject.ScheduleCalls++; });
        __state = state;
    }
    private static Exception? EndSchedule(Exception? __exception, bool __runOriginal, ScheduleScope? __state)
    {
        var f = active; if (f == null || __state == null) return __exception;
        f.Observe(() => { __state.Closed = true; __state.Original = __runOriginal; __state.Error = __exception?.GetType().FullName;
            EndToEndAssert.True(ReferenceEquals(f.scheduleScope, __state) && f.decisionScope == null && f.doseScope == null && __runOriginal && __exception == null); });
        if (ReferenceEquals(f.scheduleScope, __state)) f.scheduleScope = null;
        return __exception;
    }
    private static void BeginDecision(Pawn_DrugPolicyTracker __instance, ThingDef __0, out Decision? __state)
    {
        __state = null; var f = active; if (f == null || __0 != f.beerDef || f.SubjectFor(__instance.pawn) is not { } subject) return;
        Decision? state = null;
        f.Observe(() => { f.RequireCurrent(true); EndToEndAssert.True(f.scheduleScope?.Subject == subject && f.decisionScope == null && f.decisions.Count < 256 && ReferenceEquals(subject.Pawn.drugs, __instance));
            state = new Decision(subject, f.scheduleScope!); f.decisions.Add(state); f.decisionScope = state; f.scheduleScope!.LastDecision = state; });
        __state = state;
    }
    private static void ObserveDecision(bool __result, bool __runOriginal, Decision? __state)
    { if (__state != null) { __state.Result = __result; __state.Original = __runOriginal; __state.Postfix = true; } }
    private static Exception? EndDecision(Exception? __exception, bool __runOriginal, Decision? __state)
    {
        var f = active; if (f == null || __state == null) return __exception;
        f.Observe(() => { __state.Closed = true; __state.Error = __exception?.GetType().FullName;
            EndToEndAssert.True(ReferenceEquals(f.decisionScope, __state) && __state.Postfix && __runOriginal && __exception == null); });
        if (ReferenceEquals(f.decisionScope, __state)) f.decisionScope = null;
        return __exception;
    }
    private static void BeginAllowed(Pawn_DrugPolicyTracker __instance, ThingDef __0, out Decision? __state)
    {
        __state = null; var f = active; if (f == null || __0 != f.beerDef || f.SubjectFor(__instance.pawn) is not { } subject) return;
        Decision? state = null;
        f.Observe(() => { EndToEndAssert.True(f.decisionScope?.Subject == subject && ReferenceEquals(subject.Pawn.drugs, __instance)); state = f.decisionScope; state!.AllowedCalls++; });
        __state = state;
    }
    private static void ObserveAllowed(bool __result, bool __runOriginal, Decision? __state)
    { if (__state != null) { __state.NativeAllowed = __result; __state.AllowedOriginal = __runOriginal; __state.AllowedCompleted = true; } }
    private static void BeginDrug(Caravan_NeedsTracker __instance, Pawn __0, Thing __1, Pawn __2, out Dose? __state)
    {
        __state = null; var f = active; if (f == null || f.SubjectFor(__0) is not { } subject) return;
        Dose? state = null;
        f.Observe(() => { f.RequireCurrent(true); EndToEndAssert.True(subject == f.b && __instance == subject.Caravan.needs && __1 == subject.Beer && __2 == subject.Pawn &&
            f.scheduleScope?.Subject == subject && f.scheduleScope.LastDecision is { Closed: true, Result: true } && f.doseScope == null && f.doses.Count < 16);
            state = new Dose(subject, __1, __2, f.scheduleScope!, f.highDef, f.toleranceDef); f.doses.Add(state); f.doseScope = state; });
        __state = state;
    }
    private static void ObserveDrug(bool __runOriginal, Dose? __state)
    { if (__state != null) { __state.Postfix = true; __state.Original = __runOriginal; } }
    private static Exception? EndDrug(Exception? __exception, bool __runOriginal, Dose? __state)
    {
        var f = active; if (f == null || __state == null) return __exception;
        f.Observe(() => { __state.Closed = true; __state.Error = __exception?.GetType().FullName;
            EndToEndAssert.True(ReferenceEquals(f.doseScope, __state) && __state.Postfix && __state.IngestedClosed && __runOriginal && __exception == null); });
        if (ReferenceEquals(f.doseScope, __state)) f.doseScope = null;
        return __exception;
    }
    private static void BeginIngested(Thing __instance, Pawn __0, float __1, out Dose? __state)
    {
        __state = null; var f = active; if (f == null || !f.ownedBeer.Contains(__instance) && f.SubjectFor(__0) == null) return;
        Dose? state = null;
        f.Observe(() => { f.RequireCurrent(true); EndToEndAssert.True(f.doseScope != null && f.doseScope.Source == __instance && f.doseScope.Subject.Pawn == __0 &&
            f.scheduleScope == f.doseScope.Scope && f.doseScope.IngestedCalls == 0 && __1 == 0);
            state = f.doseScope; state!.IngestedCalls++; state.NutritionWanted = __1; });
        __state = state;
    }
    private static void ObserveIngested(bool __runOriginal, Dose? __state)
    {
        var f = active; if (f == null || __state == null) return;
        f.Observe(() => { __state.IngestedPostfix = true; __state.IngestedOriginal = __runOriginal; __state.After = __state.Source.stackCount;
            __state.HistoryAfter = SnapshotHistory(__state.Subject); __state.HighAfter = Severity(__state.Subject, f.highDef); __state.ToleranceAfter = Severity(__state.Subject, f.toleranceDef); });
    }
    private static Exception? EndIngested(Exception? __exception, bool __runOriginal, Dose? __state)
    {
        var f = active; if (f == null || __state == null) return __exception;
        f.Observe(() => { __state.IngestedClosed = true; __state.IngestedError = __exception?.GetType().FullName;
            EndToEndAssert.True(ReferenceEquals(f.doseScope, __state) && __state.IngestedPostfix && __runOriginal && __exception == null); });
        return __exception;
    }
    private static void BeginSplit(ThingWithComps __instance, int __0, out Split? __state)
    {
        __state = null; var f = active; if (f == null || !f.ownedBeer.Contains(__instance)) return;
        Split? state = null;
        f.Observe(() => { EndToEndAssert.True(f.doseScope != null && f.doseScope.Source == __instance && f.doseScope.IngestedCalls == 1 && !f.doseScope.IngestedClosed && f.splits.Count < 16);
            state = new Split(f.doseScope!, __instance, __0); f.splits.Add(state); });
        __state = state;
    }
    private static void ObserveSplit(Thing __result, bool __runOriginal, Split? __state)
    {
        var f = active; if (f == null || __state == null) return;
        if (__result != null) f.ownedBeer.Add(__result);
        f.Observe(() => { __state.Postfix = true; __state.Original = __runOriginal; __state.Result = __result;
            __state.After = __state.Source.stackCount; __state.ResultCount = __result?.stackCount ?? -1; __state.ResultHolder = __result?.ParentHolder; });
    }
    private static Exception? EndSplit(Exception? __exception, bool __runOriginal, Split? __state)
    {
        var f = active; if (f == null || __state == null) return __exception;
        f.Observe(() => { __state.Closed = true; __state.Error = __exception?.GetType().FullName; EndToEndAssert.True(__state.Postfix && __runOriginal && __exception == null); });
        return __exception;
    }

    private void Install(MethodInfo target, string prefix, string? postfix, string? finalizer)
    {
        var binding = new TraceBinding(target, Callback(prefix), postfix == null ? null : Callback(postfix), finalizer == null ? null : Callback(finalizer));
        bindings.Add(binding);
        traceOwner.Patch(target, new HarmonyMethod(binding.Prefix), binding.Postfix == null ? null : new HarmonyMethod(binding.Postfix),
            finalizer: binding.Finalizer == null ? null : new HarmonyMethod(binding.Finalizer));
        ValidatePublication(binding);
        EndToEndAssert.True(runtime.Available, "Passive trace installation must preserve the actual atomic S138 group.");
    }

    // Reuse the product's metadata reader through its exact internal shape. No copied provider parsing,
    // GetPatchInfo attribute construction, foreign callback invocation, or new production API.
    private static readonly Type PublicationType = typeof(DrugToleranceDecisionPatch).Assembly.GetType("Megamod.Patching.HarmonyPublication", true)!;
    private static readonly MethodInfo InspectPublication = PublicationType.GetMethod("Inspect", BindingFlags.Static | BindingFlags.NonPublic)!;
    private static object Property(object value, string name) => value.GetType().GetProperty(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(value)!;
    private static object[] Registrations(object snapshot, string name) => ((IEnumerable)Property(snapshot, name)).Cast<object>().ToArray();
    private static bool Inspect(MethodInfo target, Func<object, bool> visitor)
    {
        var callback = visitor.Method.CreateDelegate(InspectPublication.GetParameters()[1].ParameterType, visitor.Target);
        return (bool)InspectPublication.Invoke(null, new object[] { target, callback })!;
    }
    private static bool PublicationCurrent(object identity) => (bool)identity.GetType().GetMethod("IsCurrent", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(identity, null)!;
    private void ValidatePublication(TraceBinding binding)
    {
        EndToEndAssert.True(Inspect(binding.Target, snapshot =>
        {
            var prefix = Registrations(snapshot, "Prefixes"); var postfix = Registrations(snapshot, "Postfixes"); var finalizer = Registrations(snapshot, "Finalizers");
            var all = prefix.Concat(postfix).Concat(finalizer).ToArray();
            EndToEndAssert.True(all.All(value => !(bool)Property(value, "IsFactory")));
            void Slot(object[] values, MethodInfo? expected)
            {
                var owned = values.Where(value => (string)Property(value, "Owner") == traceOwner.Id).ToArray();
                EndToEndAssert.True(expected == null ? owned.Length == 0 : owned.Length == 1 && (MethodInfo)Property(owned[0], "Method") == expected);
            }
            Slot(prefix, binding.Prefix); Slot(postfix, binding.Postfix); Slot(finalizer, binding.Finalizer);
            var transpilers = (Patch[])Property(snapshot, "Transpilers");
            EndToEndAssert.Equal(0, ((Array)snapshot.GetType().GetField("effects", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(snapshot)!).Length,
                "This Core-only provenance requires no unproved inner result transformations.");
            if (binding.Target == AllowedTarget)
            {
                var type = typeof(DrugToleranceDecisionPatch);
                MethodInfo Product(string name) => type.GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;
                EndToEndAssert.True(prefix.Length == 2 && postfix.Length == 2 && finalizer.Length == 1 && transpilers.Length == 1 &&
                    transpilers[0].owner == "fumblesneeze.megamod" && transpilers[0].PatchMethod == Product("Observe") &&
                    all.Where(value => (string)Property(value, "Owner") != traceOwner.Id).All(value => (string)Property(value, "Owner") == "fumblesneeze.megamod"));
                MethodInfo[] Sorted(object[] values) => PatchProcessor.GetSortedPatchMethods(binding.Target, values.Select(value =>
                    (Patch)value.GetType().GetMethod("ToOrderingPatch", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(value, null)!).ToArray()).ToArray();
                EndToEndAssert.True(Sorted(prefix).Last() == Product("ScheduledPrefix") && Sorted(postfix).First() == Product("Postfix") &&
                    Sorted(postfix).Last() == binding.Postfix && Sorted(finalizer).Single() == Product("Finalizer"));
            }
            else EndToEndAssert.True(all.All(value => (string)Property(value, "Owner") == traceOwner.Id) && transpilers.Length == 0,
                "This exact native trace cannot prove a foreign result-transforming composition.");
            binding.Identity = Property(snapshot, "Identity"); return true;
        }), "The native publication snapshot must be available.");
    }

    private void CleanupTrace()
    {
        if (ReferenceEquals(active, this)) active = null;
        var errors = new List<Exception>();
        foreach (var binding in bindings) Attempt(errors, () => traceOwner.Unpatch(binding.Target, HarmonyPatchType.All, traceOwner.Id));
        foreach (var binding in bindings) Attempt(errors, () => EndToEndAssert.True(Inspect(binding.Target, snapshot =>
            new[] { "Prefixes", "Postfixes", "Finalizers" }.All(name => Registrations(snapshot, name).All(value => (string)Property(value, "Owner") != traceOwner.Id)))));
        tickScope = null; scheduleScope = null; decisionScope = null; doseScope = null;
        if (ReferenceEquals(Current.Game, game)) Attempt(errors, () => EndToEndAssert.True(runtime.Available, "Removing only fixture observers must preserve S138 availability."));
        if (errors.Count != 0) throw new AggregateException("Caravan trace cleanup failed.", errors);
    }
    private void CleanupObjects()
    {
        RequireGame(); var errors = new List<Exception>();
        foreach (var owned in ownedWindows.AsEnumerable().Reverse()) Attempt(errors, () =>
        {
            if (!Find.WindowStack.Windows.Contains(owned)) return;
            if (owned is MainTabWindow_Assign && Find.MainTabsRoot.OpenTab?.TabWindow == owned) Find.MainTabsRoot.EscapeCurrentTab(false);
            else owned.Close(false);
        });
        foreach (var caravan in ownedCaravans.AsEnumerable().Reverse()) Attempt(errors, () =>
        {
            foreach (var pawn in ownedPawns.Where(caravan.ContainsPawn).ToArray()) caravan.RemovePawn(pawn);
            if (!caravan.Destroyed) caravan.Destroy();
        });
        foreach (var pawn in ownedPawns.AsEnumerable().Reverse()) Attempt(errors, () =>
        { if (world.worldPawns.Contains(pawn)) world.worldPawns.RemovePawn(pawn); if (!pawn.Destroyed) pawn.Destroy(DestroyMode.Vanish); });
        foreach (var thing in ownedBeer) Attempt(errors, () => { if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish); });
        foreach (var policy in ownedPolicies) Attempt(errors, () => game.drugPolicyDatabase.AllPolicies.Remove(policy));
        if (errors.Count != 0) throw new AggregateException("Caravan fixture object cleanup failed.", errors);
    }

    private NativeWindowCaptureStep Capture<T>(string name, Action<T> retain) where T : Window => new(name, windows =>
    {
        RequireCurrent(); var actual = Find.WindowStack.Windows.OfType<T>().Single();
        if (!ownedWindows.Contains(actual)) ownedWindows.Add(actual);
        window = windows.Single(item => item.NativeId == actual.ID && item.RuntimeType == actual.GetType().FullName);
        EndToEndAssert.True(actual.windowRect.xMin >= 0 && actual.windowRect.yMin >= 0 && actual.windowRect.xMax <= UI.screenWidth && actual.windowRect.yMax <= UI.screenHeight);
        retain(actual);
    });
    private NativeWindowClickActionStep Click(string name, Vector2 local) => new(name, window.Handle,
        (window.X + local.x) * window.UiScale, (window.Y + local.y) * window.UiScale, null, pointer);
    private void RequireSelected(Subject subject) => EndToEndAssert.True(ReferenceEquals(policyView.Selected(manager), subject.Policy));
    private EndToEndCaravanHealthObservation Health(Subject subject) => health.ObserveHealth(subject.Handle, subject.Pawn.GetUniqueLoadID());
    private bool Selected(Subject subject) => WorldRendererUtility.WorldSelected && world.UI.selector.SelectedObjects.Count == 1 && ReferenceEquals(world.UI.selector.SingleSelectedObject, subject.Caravan);
    private bool Tab(Subject subject, Type type) => Selected(subject) && world.UI.inspectPane.IsOpen && world.UI.inspectPane.OpenTabType == type;
    private static bool DetailWindowOpen() => Find.WindowStack.Windows.Any(value => value is ImmediateWindow && value.ID == -1439870015);
    private static TimeControlActionStep Pause(string name) => new(name, true, EndToEndGameSpeed.Normal);
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private static WaitUntilStep Wait(string name, Func<bool> predicate, int seconds = 15, int ticks = 3000) =>
        new(name, _ => predicate(), new EndToEndDeadline(int.MaxValue, ticks, TimeSpan.FromSeconds(seconds)));
    private static void Attempt(List<Exception> errors, Action action) { try { action(); } catch (Exception error) { errors.Add(error); } }
    private static FieldInfo Field(Type type, string name) => type.GetField(name, InstanceFields) ?? throw new MissingFieldException(type.FullName, name);
    private static MethodInfo Callback(string name) => typeof(NativeToleranceCaravanTests).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!;
    private static MethodInfo Native(Type type, string name, bool isStatic, bool isPublic, Type result, params Type[] parameters)
    {
        var method = type.GetMethod(name, BindingFlags.DeclaredOnly | (isStatic ? BindingFlags.Static : BindingFlags.Instance) |
            (isPublic ? BindingFlags.Public : BindingFlags.NonPublic), null, parameters, null) ?? throw new MissingMethodException(type.FullName, name);
        EndToEndAssert.True(method.ReturnType == result && !method.IsGenericMethod && !method.ContainsGenericParameters && method.GetMethodBody() != null);
        return method;
    }
    private IReadOnlyDictionary<string, string> Evidence()
    {
        RequireCurrent();
        return new Dictionary<string, string>
        {
            ["scope"] = "Separate Core caravans0/Any; actual scheduled direct ingestion and same-day interval; no map-job/refill/formation claim",
            ["startTick"] = startTick.ToString(), ["pausedTick"] = pausedTick.ToString(), ["day"] = startDay.ToString(),
            ["A"] = SubjectEvidence(a), ["B"] = SubjectEvidence(b), ["traceOwner"] = traceOwner.Id,
            ["decisions"] = string.Join(" | ", decisions.Select(d => $"tick={d.Tick};nativeTick={d.Scope.Parent.Id}:{d.Scope.Parent.Delta};pawn={d.Subject.Pawn.ThingID};policy={d.Subject.PolicyId};choice={d.Choice ?? "absent"};historyBefore={string.Join(",", d.HistoryBefore)};allowedCalls={d.AllowedCalls};nativeAllowed={d.NativeAllowed};finalShould={d.Result};original={d.Original};closed={d.Closed};error={d.Error ?? "none"}")),
            ["doses"] = string.Join(" | ", doses.Select(d => $"tick={d.Tick};pawn={d.Subject.Pawn.ThingID};source={d.Source.ThingID};owner={d.Owner.ThingID};wanted={d.NutritionWanted};count={d.Before}->{d.After};nativeIngestedCalls={d.IngestedCalls};original={d.Original}/{d.IngestedOriginal};closed={d.Closed}/{d.IngestedClosed};history={string.Join(",", d.HistoryBefore)}->{string.Join(",", d.HistoryAfter)};high={Number(d.HighBefore)}->{Number(d.HighAfter)};tolerance={Number(d.ToleranceBefore)}->{Number(d.ToleranceAfter)};error={d.Error ?? d.IngestedError ?? "none"}")),
            ["splits"] = string.Join(" | ", splits.Select(s => $"source={s.Source.ThingID};before={s.Before};requested={s.Requested};after={s.After};result={s.Result?.ThingID};resultCount={s.ResultCount};holder={s.ResultHolder?.GetType().FullName ?? "none"};original={s.Original};closed={s.Closed};error={s.Error ?? "none"}")),
            ["nativeOperations"] = nativeOperations.ToString(), ["screenshots"] = screenshots.ToString(), ["waitSeconds"] = waitSeconds.ToString(),
            ["countedSteps"] = countedSteps.ToString(), ["budget"] = "60x15+13x30+495+90=1875; scenario2100; launcher2400; ticks3000",
            ["traceFailure"] = traceFailure ?? "none"
        };
    }
    private string SubjectEvidence(Subject s) => $"pawn={s.Pawn.ThingID};caravan={s.Caravan.GetUniqueLoadID()};tile={s.Caravan.Tile};policy={s.PolicyId}:{s.Policy.label};choice={NativeDrugToleranceState.ReadEncoded(s.Entry) ?? "absent"};schedule={s.Entry.allowScheduled};joy={s.Entry.allowedForJoy};addiction={s.Entry.allowedForAddiction};frequency={s.Entry.daysFrequency};stockTarget={s.Entry.takeToInventory};source={s.Beer.ThingID}:{s.Beer.stackCount};holder={s.Beer.ParentHolder?.GetType().FullName};history={string.Join(",", SnapshotHistory(s))};high={Number(Severity(s, highDef))};tolerance={Number(Severity(s, toleranceDef))};food={Number(s.Pawn.needs.food.CurLevelPercentage)};job={s.Pawn.CurJobDef?.defName ?? "none"};ticks={s.TickCalls};scheduleCalls={s.ScheduleCalls}";
    private static string Number(float value) => value.ToString("R", CultureInfo.InvariantCulture);

    private sealed class Subject
    {
        internal readonly Pawn Pawn; internal readonly Caravan Caravan; internal readonly DrugPolicy Policy; internal readonly DrugPolicyEntry Entry;
        internal readonly Thing Beer; internal readonly PlanetTile Tile; internal readonly int PolicyId; internal readonly NativeRow[] Rows;
        internal string Handle = null!; internal string? ChoiceEncoding; internal int TickCalls, ScheduleCalls;
        internal Subject(Pawn pawn, Caravan caravan, DrugPolicy policy, DrugPolicyEntry entry, Thing beer, PlanetTile tile)
        { Pawn = pawn; Caravan = caravan; Policy = policy; Entry = entry; Beer = beer; Tile = tile; PolicyId = policy.id;
          ChoiceEncoding = NativeDrugToleranceState.ReadEncoded(entry); Rows = Enumerable.Range(0, policy.Count).Select(i => new NativeRow(policy[i])).ToArray(); }
    }
    private sealed class NativeRow
    {
        internal readonly DrugPolicyEntry Entry; internal readonly ThingDef Drug;
        private readonly bool scheduled, joy, addiction; private readonly int stock; private readonly float frequency, mood, recreation; private readonly string? choice;
        internal NativeRow(DrugPolicyEntry entry) { Entry = entry; Drug = entry.drug; scheduled = entry.allowScheduled; joy = entry.allowedForJoy; addiction = entry.allowedForAddiction;
            stock = entry.takeToInventory; frequency = entry.daysFrequency; mood = entry.onlyIfMoodBelow; recreation = entry.onlyIfJoyBelow; choice = NativeDrugToleranceState.ReadEncoded(entry); }
        internal void Require(DrugPolicyEntry current, DrugPolicyEntry edited) => EndToEndAssert.True(ReferenceEquals(current, Entry) && ReferenceEquals(current.drug, Drug) &&
            current.allowScheduled == scheduled && current.allowedForJoy == joy && current.allowedForAddiction == addiction && current.takeToInventory == stock &&
            current.daysFrequency == frequency && current.onlyIfMoodBelow == mood && current.onlyIfJoyBelow == recreation &&
            (ReferenceEquals(current, edited) || NativeDrugToleranceState.ReadEncoded(current) == choice));
    }
    private sealed record History(string Drug, int Tick, int Day, int Count);
    private sealed class TickScope
    {
        internal readonly Subject Subject; internal readonly int Id, Tick, Delta; internal bool Closed, Original; internal string? Error;
        internal TickScope(Subject subject, int id, int tick, int delta) { Subject = subject; Id = id; Tick = tick; Delta = delta; }
    }
    private sealed class ScheduleScope
    {
        internal readonly Subject Subject; internal readonly TickScope Parent; internal Decision? LastDecision; internal bool Closed, Original; internal string? Error;
        internal ScheduleScope(Subject subject, TickScope parent) { Subject = subject; Parent = parent; }
    }
    private sealed class Decision
    {
        internal readonly Subject Subject; internal readonly ScheduleScope Scope; internal readonly int Tick; internal readonly string? Choice; internal readonly bool AnyChoice;
        internal readonly List<History> HistoryBefore; internal bool Result, Original, Postfix, Closed, NativeAllowed, AllowedCompleted, AllowedOriginal;
        internal int AllowedCalls; internal string? Error;
        internal Decision(Subject subject, ScheduleScope scope) { Subject = subject; Scope = scope; Tick = Find.TickManager.TicksGame;
            var choice = NativeDrugToleranceState.Read(subject.Entry); Choice = choice.Encoding; AnyChoice = choice.Kind == DrugToleranceChoiceKind.Any; HistoryBefore = SnapshotHistory(subject); }
    }
    private sealed class Dose
    {
        internal readonly Subject Subject; internal readonly Thing Source; internal readonly Pawn Owner; internal readonly ScheduleScope Scope; internal readonly Decision Decision;
        internal readonly int Tick, Before; internal readonly List<History> HistoryBefore; internal List<History> HistoryAfter = new();
        internal readonly float HighBefore, ToleranceBefore; internal float HighAfter, ToleranceAfter, NutritionWanted;
        internal int After, IngestedCalls; internal bool Original, Postfix, Closed, IngestedOriginal, IngestedPostfix, IngestedClosed; internal string? Error, IngestedError;
        internal Dose(Subject subject, Thing source, Pawn owner, ScheduleScope scope, HediffDef high, HediffDef tolerance)
        { Subject = subject; Source = source; Owner = owner; Scope = scope; Decision = scope.LastDecision!; Tick = Find.TickManager.TicksGame; Before = source.stackCount;
          HistoryBefore = SnapshotHistory(subject); HighBefore = Severity(subject, high); ToleranceBefore = Severity(subject, tolerance); }
    }
    private sealed class Split
    {
        internal readonly Dose Dose; internal readonly Thing Source; internal readonly int Before, Requested;
        internal int After, ResultCount; internal Thing? Result; internal IThingHolder? ResultHolder; internal bool Original, Postfix, Closed; internal string? Error;
        internal Split(Dose dose, Thing source, int requested) { Dose = dose; Source = source; Before = source.stackCount; Requested = requested; }
    }
    private sealed class TraceBinding
    {
        internal readonly MethodInfo Target, Prefix; internal readonly MethodInfo? Postfix, Finalizer; internal object Identity = null!;
        internal TraceBinding(MethodInfo target, MethodInfo prefix, MethodInfo? postfix, MethodInfo? finalizer)
        { Target = target; Prefix = prefix; Postfix = postfix; Finalizer = finalizer; }
    }
}
