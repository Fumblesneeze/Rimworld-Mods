using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using LudeonTK;
using Megamod.EndToEndTests.Diagnostics;
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
using Verse.AI;
using HarmonyOwner = HarmonyLib.Harmony;

namespace Megamod.EndToEndTests.DrugTolerance;

[RimWorldEndToEndTest("megamod.drug-tolerance.native-save-override-inheritance", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 15000, MaxWallClockSeconds = 3000)]
public sealed class NativeToleranceSaveOverrideTests : IRimWorldEndToEndTest
{
    private readonly NativeToleranceSaveContextFixture fixture = new(false);
    public void Arrange(IEndToEndContext context) => fixture.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => fixture.Execute(context);
}

[RimWorldEndToEndTest("megamod.drug-tolerance.native-ingestion-save-resume", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 12000, MaxWallClockSeconds = 2400)]
public sealed class NativeToleranceIngestionResumeTests : IRimWorldEndToEndTest
{
    private readonly NativeToleranceSaveContextFixture fixture = new(true);
    public void Arrange(IEndToEndContext context) => fixture.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => fixture.Execute(context);
}

// Private D/E fixture. One native save/load per case; setup never manufactures an ingestion result.
// IDs cross serialization; object references are meaningful only inside their actual current Game.
internal sealed class NativeToleranceSaveContextFixture
{
    private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;
    private static readonly FieldInfo RuntimeField = Field(typeof(MegamodMod), "drugTolerance");
    private static readonly FieldInfo PersistenceField = Field(typeof(MegamodMod), "drugTolerancePersistence");
    private static readonly FieldInfo SaveValuesField = Field(typeof(SettingsStore), "saveValues");
    private static readonly FieldInfo SettingsBindingField = Field(typeof(SettingsRuntime), "binding");
    private static readonly FieldInfo PawnPolicyField = Field(typeof(Pawn_DrugPolicyTracker), "curPolicy");
    private static readonly FieldInfo ChewingField = Field(typeof(JobDriver_Ingest), "chewing");
    private static readonly FieldInfo ToilsField = Field(typeof(JobDriver), "toils");
    private static readonly MethodInfo ShouldTarget = typeof(Pawn_DrugPolicyTracker).GetMethod(nameof(Pawn_DrugPolicyTracker.ShouldTryToTakeScheduledNow))!;
    private static readonly MethodInfo AllowedTarget = typeof(Pawn_DrugPolicyTracker).GetMethod(nameof(Pawn_DrugPolicyTracker.AllowedToTakeScheduledNow))!;
    private static readonly MethodInfo IngestedTarget = typeof(Thing).GetMethod(nameof(Thing.Ingested), new[] { typeof(Pawn), typeof(float) })!;
    private static NativeToleranceSaveContextFixture? active;
    private readonly bool resume;
    private readonly string marker = "ToleranceSave-" + Guid.NewGuid().ToString("N").Substring(0, 10);
    private readonly List<Subject> subjects = new();
    private readonly List<Thing> initialOwned = new();
    private readonly List<Window> ownedWindows = new();
    private readonly List<MethodInfo> traced = new();
    private readonly NativeDrugPolicyView policyView = new();
    private Game initial = null!, game = null!;
    private Game? cleanupBoundGame;
    private Map map = null!;
    private int mapId, policyId, savedTick, savedToil = -1, savedTicksLeft = -1;
    private int loadedTick = -1, loadedToil = -1, loadedTicksLeft = -1;
    private DrugPolicy policy = null!;
    private DrugPolicyEntry beerEntry = null!;
    private ThingDef beerDef = null!;
    private HediffDef alcoholHigh = null!, toleranceDef = null!;
    private SettingsRuntime settings = null!;
    private SaveSettingsBinding binding = null!;
    private SaveSettingsSnapshot initialSave = null!, serializedSave = null!;
    private GlobalSettingsSnapshot initialGlobals = null!;
    private MegamodMod mod = null!;
    private NativeDrugToleranceRuntime runtime = null!;
    private DrugTolerancePersistencePatch persistence = null!;
    private HarmonyOwner traceOwner = null!;
    private NativeDiagnosticSettingsView settingsView = null!;
    private EndToEndNativeWindow window = null!;
    private MainTabWindow_Assign assign = null!;
    private Dialog_ManageDrugPolicies manager = null!;
    private string pointer = null!;
    private string choice = null!;
    private bool saveRequested, reloaded, traceFault, expectedEnabled = true, expectedInheritance = true;
    private string? traceFailure;
    private string? expectedOverride;
    private int nativeOperations, screenshots, waitSeconds, saves, settingsScrollClicks;
    private string phase = "arranged preconditions";
    private string retainedAdmission = "unobserved";
    private string SaveName => marker + (resume ? "-resume" : "-override");
    private string SavePath => GenFilePaths.FilePathForSavedGame(SaveName);

    internal NativeToleranceSaveContextFixture(bool resume) => this.resume = resume;

    internal void Arrange(IEndToEndContext context)
    {
        initial = game = Current.Game; map = game.CurrentMap; mapId = map.uniqueID; RequireGame();
        EndToEndAssert.True(active == null && game.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting && Prefs.DevMode);
        EndToEndAssert.True(Find.MainTabsRoot.OpenTab == null && !Find.WindowStack.IsOpen<Dialog_ModSettings>() && !Find.WindowStack.IsOpen<EditWindow_Log>());
        settings = SettingsRuntime.Instance ?? throw new InvalidOperationException("Common settings are unavailable.");
        binding = settings.CurrentBinding ?? throw new InvalidOperationException("No active native save binding.");
        EndToEndAssert.True(settings.SaveLifecycleAvailable && settings.Store.GetGlobal(DrugToleranceSettings.Enabled) &&
            settings.Store.UsesGlobal(DrugToleranceSettings.Enabled) && settings.Store.EffectiveOffReason(DrugToleranceSettings.Enabled) == null);
        initialGlobals = settings.Store.ExportGlobals(); initialSave = settings.Store.ExportSave(binding);
        EndToEndAssert.False(initialSave.Values.ContainsKey(DrugToleranceSettings.Enabled.Id), "This isolated case starts with a missing inherited save entry.");
        mod = LoadedModManager.GetMod<MegamodMod>();
        runtime = RuntimeField.GetValue(mod) as NativeDrugToleranceRuntime ?? throw new InvalidOperationException("Actual tolerance runtime unavailable.");
        persistence = PersistenceField.GetValue(mod) as DrugTolerancePersistencePatch ?? throw new InvalidOperationException("Actual persistence support unavailable.");
        settingsView = new(() => pointer, DrugToleranceSettings.Enabled.Id);
        EndToEndAssert.True(SaveValuesField.FieldType == typeof(Dictionary<string, SaveSettingValue>) &&
            SettingsBindingField.FieldType == typeof(SaveSettingsBinding) && PawnPolicyField.FieldType == typeof(DrugPolicy) &&
            ChewingField.FieldType == typeof(Toil) && ToilsField.FieldType == typeof(List<Toil>));
        var oldScroll = settingsView.Scroll; var oldSpeed = Find.TickManager.CurTimeSpeed;
        EndToEndAssert.False(File.Exists(SavePath));
        context.DeferCleanup(() => { if (saveRequested && File.Exists(SavePath)) File.Delete(SavePath); });
        context.DeferCleanup(() => { _ = OwnedCleanupGame(); Find.TickManager.CurTimeSpeed = oldSpeed; });
        // Renderer state belongs to the process and must restore even if native load leaves a partial Game.
        context.DeferCleanup(() => settingsView.RestoreScroll(oldScroll));
        context.DeferCleanup(CleanupWorldAndSettings);
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
        beerDef = DefDatabase<ThingDef>.GetNamed("Beer"); alcoholHigh = DefDatabase<HediffDef>.GetNamed("AlcoholHigh");
        toleranceDef = DefDatabase<HediffDef>.GetNamed("AlcoholTolerance");
        choice = resume ? "1:any" : "1:limit:0";
        policy = game.drugPolicyDatabase.MakeNewDrugPolicy(); policyId = policy.id; policy.label = marker;
        for (int i = 0; i < policy.Count; i++)
        {
            policy[i].allowScheduled = false; policy[i].allowedForJoy = false;
            policy[i].allowedForAddiction = false; policy[i].takeToInventory = 0;
        }
        beerEntry = policy[beerDef]; beerEntry.allowScheduled = true; beerEntry.daysFrequency = 1f;
        beerEntry.onlyIfMoodBelow = 1f; beerEntry.onlyIfJoyBelow = 1f;
        NativeDrugToleranceState.WriteEncoded(beerEntry, choice); // Initial policy precondition only.
        var offsets = new[] { new IntVec3(-3, 0, 0), new IntVec3(3, 0, 0) };
        var center = CellRect.CenteredOn(map.Center, 20).OrderBy(cell => cell.DistanceToSquared(map.Center))
            .First(cell => offsets.All(offset => (cell + offset).InBounds(map) && (cell + offset).Standable(map) &&
                !(cell + offset).Fogged(map) && (cell + offset).GetThingList(map).Count == 0));
        subjects.Add(CreateSubject("First Dose", center + offsets[0]));
        subjects.Add(CreateSubject("Next Decision", center + offsets[1]));
        traceOwner = new HarmonyOwner("fumblesneeze.tests.s138.save-context." + marker); active = this;
        context.DeferCleanup(() =>
        {
            if (ReferenceEquals(active, this)) active = null;
            var errors = new List<Exception>();
            foreach (var target in traced) Attempt(errors, () => traceOwner.Unpatch(target, HarmonyPatchType.All, traceOwner.Id));
            foreach (var target in traced) Attempt(errors, () => EndToEndAssert.False(HarmonyOwner.GetPatchInfo(target)?.Owners.Contains(traceOwner.Id) == true));
            if (errors.Count != 0) throw new AggregateException("Tolerance save-context observer cleanup failed.", errors);
        });
        Install(ShouldTarget, nameof(BeginDecision), nameof(CompleteDecision));
        Install(AllowedTarget, nameof(ObserveAllowed)); Install(IngestedTarget, nameof(ObserveIngestion));
        RequireCurrent();
    }

    private Subject CreateSubject(string name, IntVec3 cell)
    {
        var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer,
            forceGenerateNewPawn: true, canGeneratePawnRelations: false, allowFood: false, allowAddictions: false,
            fixedBiologicalAge: 25, fixedChronologicalAge: 25, forceNoIdeo: true, forceNoGear: true));
        initialOwned.Add(pawn); pawn.Name = new NameTriple("Tolerance", name, marker);
        foreach (var hediff in pawn.health.hediffSet.hediffs.ToArray()) pawn.health.RemoveHediff(hediff);
        foreach (var trait in pawn.story.traits.allTraits.ToArray()) pawn.story.traits.RemoveTrait(trait);
        pawn.inventory.innerContainer.ClearAndDestroyContents(); pawn.workSettings.EnableAndInitialize();
        foreach (var work in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            if (!pawn.WorkTypeIsDisabled(work)) pawn.workSettings.SetPriority(work, 0);
        for (int hour = 0; hour < 24; hour++) pawn.timetable.SetAssignment(hour, TimeAssignmentDefOf.Anything);
        pawn.needs.food.CurLevelPercentage = 1f; pawn.needs.rest.CurLevelPercentage = 1f;
        pawn.needs.mood.CurLevelPercentage = 1f; pawn.needs.joy.CurLevelPercentage = 1f;
        pawn.drugs.CurrentPolicy = policy;
        var dose = ThingMaker.MakeThing(beerDef); dose.stackCount = 1; initialOwned.Add(dose);
        EndToEndAssert.True(pawn.inventory.innerContainer.TryAdd(dose));
        GenSpawn.Spawn(pawn, cell, map); pawn.drafter.Drafted = true;
        return new Subject(pawn, dose);
    }

    internal IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        foreach (var step in Workflow(context))
        {
            if (step is SaveLoadActionStep) saves++;
            else if (step is ScreenshotStep) screenshots++;
            else if (step is WaitUntilStep wait) waitSeconds += checked((int)wait.Deadline.MaxWallClock.TotalSeconds);
            else if (step.Kind == EndToEndStepKind.Act || step is NativeWindowCaptureStep) nativeOperations++;
            if (step.Kind == EndToEndStepKind.Act)
                yield return new AssertionStep("retain actual current authority before " + step.Name, _ => RequireCurrent());
            yield return step;
        }
        yield return new AssertionStep("count the complete selected D/E workflow", _ =>
        {
            EndToEndAssert.Equal(1, saves);
            EndToEndAssert.True(settingsScrollClicks >= 0 && settingsScrollClicks <= (resume ? 0 : 12));
            EndToEndAssert.Equal(resume ? 43 : 60 + 2 * settingsScrollClicks, nativeOperations);
            EndToEndAssert.Equal(resume ? 12 : 21, screenshots);
            EndToEndAssert.Equal(resume ? 240 : 360, waitSeconds);
            EndToEndAssert.True(nativeOperations * 15 + screenshots * 30 + waitSeconds + 300 + 180 <= (resume ? 2400 : 3000));
        });
        yield return new CheckpointStep("final scoped D/E accounting and provenance", _ => Evidence());
    }

    private IEnumerable<EndToEndStep> Workflow(IEndToEndContext context)
    {
        yield return Run("obtain ordinary native drafted hold jobs");
        yield return Wait("observe both native drafted holds", _ => { RequireCurrent(); return subjects.All(IsDraftedHold); }, 600, 30);
        yield return Pause("pause before the save-context workflow");
        yield return Frame("frame the two named subjects and their starting physical supplies");
        foreach (var subject in subjects)
        {
            yield return Select(subject); yield return Health(subject); yield return Shot(subject.Name + " starts without Alcohol effects or tolerance");
            yield return Gear(subject); yield return Shot(subject.Name + " owns its one original Beer");
        }
        foreach (var step in resume ? ResumeWorkflow(context) : OverrideWorkflow(context)) yield return step;
        yield return new NativeDeveloperLogActionStep("open native console after save-context actions", true);
        yield return Capture<EditWindow_Log>("retain the exact owned native console", _ => { });
        yield return Shot("native console after the selected save-context workflow");
        yield return new NativeDeveloperLogActionStep("close native console after save-context evidence", false);
    }

    private IEnumerable<EndToEndStep> OverrideWorkflow(IEndToEndContext context)
    {
        foreach (var step in SettingsVisit("save an inherited-on choice with dormant-off", prepareDormant: true, inherit: true)) yield return step;
        foreach (var step in SaveAndRebind()) yield return step;
        foreach (var step in SettingsVisit("reveal the persisted dormant-off value", prepareDormant: false, inherit: false)) yield return step;
        var first = subjects[0]; var next = subjects[1];
        int start = first.Decisions.Count;
        foreach (var step in Undraft(context, first)) yield return step;
        yield return new AssertionStep("native save-off overrides still-on global at the actual first selection", _ =>
        { RequireCurrent(); AssertDecisions(first, start, true); BindSelected(first); AssertRetained(next); });
        foreach (var step in ReachChewing(first, start)) yield return step;
        foreach (var step in FinishDose(first)) yield return step;
        foreach (var step in SettingsVisit("restore inheritance for the next untouched dose", prepareDormant: false, inherit: true)) yield return step;
        int blocked = next.Decisions.Count;
        foreach (var step in Undraft(context, next)) yield return step;
        foreach (var step in ObserveBlocked(next, blocked)) yield return step;
        foreach (var step in SettingsVisit("restore the saved dormant-off choice again", prepareDormant: false, inherit: false)) yield return step;
        int allowed = next.Decisions.Count;
        foreach (var step in ReachChewing(next, allowed)) yield return step;
        foreach (var step in FinishDose(next)) yield return step;
        yield return new CheckpointStep("save-local dormant override controls actual doses without changing globals", _ => Evidence());
    }

    private IEnumerable<EndToEndStep> ResumeWorkflow(IEndToEndContext context)
    {
        var first = subjects[0]; var next = subjects[1]; int start = first.Decisions.Count;
        foreach (var step in Undraft(context, first)) yield return step;
        yield return new AssertionStep("bind the actual ordinary Any job before its execution", _ =>
        { RequireCurrent(); AssertDecisions(first, start, true); BindSelected(first); AssertRetained(next); });
        foreach (var step in ReachChewing(first, start)) yield return step;
        foreach (var step in SetZeroThroughSlider()) yield return step;
        yield return new AssertionStep("the native policy edit leaves the original carried dose executing", _ =>
        { RequireCurrent(); AssertSelected(first); EndToEndAssert.True(IsChewing(first) && first.Ingestions == 0); });
        yield return Select(first); yield return Frame("frame the original executing dose after the policy becomes zero");
        yield return Shot("the same native ingestion job still holds Beer after applying zero");
        foreach (var step in SaveAndRebind()) yield return step;
        yield return Frame("frame the fresh loaded pawn and the same serialized physical Beer");
        yield return Select(first); yield return Shot("the reloaded native job retains the same dose after its one initialization tick");
        foreach (var step in FinishDose(first)) yield return step;
        int blocked = next.Decisions.Count;
        foreach (var step in Undraft(context, next)) yield return step;
        foreach (var step in ObserveBlocked(next, blocked)) yield return step;
        yield return new CheckpointStep("same loaded job consumed under zero while the next native selection refused it", _ => Evidence());
    }

    private IEnumerable<EndToEndStep> SettingsVisit(string reason, bool prepareDormant, bool inherit)
    {
        yield return new ModSettingsActionStep("open native settings to " + reason, "fumblesneeze.megamod");
        yield return new NativePointerScopeBeginStep("retain pointer for save-scope controls", handle => pointer = handle);
        Rect global = default; bool visible = false;
        for (int attempt = 0; ; attempt++)
        {
            yield return Capture<Dialog_ModSettings>("capture the current native save-scope row", _ => { });
            yield return new CheckpointStep("measure current save/global controls without invoking drawing", _ =>
            {
                settingsView.Bind(window, Find.WindowStack.WindowOfType<Dialog_ModSettings>()!);
                global = settingsView.MeasureCheckbox(out visible);
                visible = visible && global.yMin >= 22; // Include the inheritance checkbox above both editors.
                EndToEndAssert.True(Math.Abs(settingsView.Scroll.x) < .01f);
                return settingsView.Evidence(global, visible);
            });
            if (visible) break;
            Vector2 track = default;
            yield return new AssertionStep("plan bounded native scrolling to the save setting", _ =>
            { EndToEndAssert.True(attempt < 3); track = settingsView.PlanTrackClick(global); });
            settingsScrollClicks++;
            yield return settingsView.Click("scroll the native settings track to the tolerance row", track);
        }
        // Same measured row/column geometry as the actual SettingsRenderer: inheritance +32, editors +54.
        var inheritance = new Rect(global.x - 2 * (global.width + 8), global.y - 22, global.width, 28);
        var current = new Rect(global.x - (global.width + 8), global.y, global.width, 28);
        EndToEndAssert.True(inheritance.xMin >= 0 && inheritance.yMin >= 0 && current.xMin >= 0);
        yield return Shot("native settings before " + reason + "; Global default remains on");
        if (prepareDormant)
        {
            yield return settingsView.Click("uncheck Use global to create a current-save choice", inheritance.center);
            yield return new AssertionStep("native inheritance edit seeds current-save on without changing globals", _ =>
            { expectedInheritance = false; expectedOverride = "true"; RequireCurrent(); });
            yield return settingsView.Click("turn only Current save off", current.center);
            yield return new AssertionStep("native current-save off retains global-on", _ =>
            { expectedEnabled = false; expectedOverride = "false"; RequireCurrent(); });
            yield return Shot("Current save is off while Global default remains on before saving dormant state");
        }
        yield return settingsView.Click(inherit ? "check Use global and retain dormant-off" : "uncheck Use global and reveal retained dormant-off", inheritance.center);
        yield return new AssertionStep("native scope toggle preserves the exact off override and on global", _ =>
        {
            expectedInheritance = inherit; expectedEnabled = inherit; expectedOverride = "false";
            RequireCurrent(); phase = reason;
        });
        yield return Shot(inherit ? "Use global is checked and global-on governs; saved off remains dormant" :
            "Use global is unchecked; preserved Current save off is visible beside Global default on");
        yield return new CheckpointStep("exact saved scope and current availability after " + reason, _ => Evidence());
        yield return new NativePointerScopeEndStep("restore pointer after save-scope editing", pointer);
        yield return new WindowCancelActionStep("close native settings after the save-scope action", typeof(Dialog_ModSettings).FullName!);
    }

    private IEnumerable<EndToEndStep> SetZeroThroughSlider()
    {
        yield return new NativeMainTabActionStep("open Assign while the original dose is paused in progress", "Assign", true);
        yield return Capture<MainTabWindow_Assign>("capture actual Assign policy-manager entry", value => assign = value);
        yield return new NativePointerScopeBeginStep("retain pointer during the in-flight policy edit", handle => pointer = handle);
        yield return Click("open the native drug-policy manager", policyView.ManageButton(assign).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture the current native policy list", value => manager = value);
        yield return Click("select the exact shared scheduled policy", policyView.PolicyRow(manager, policy).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture the selected policy before editing", value => manager = value);
        yield return NativeToleranceSliderView.Drag("drag the native Beer tolerance slider to zero", window, pointer, manager, policy, beerEntry, 0);
        yield return new AssertionStep("the native slider committed zero without changing the in-flight job", _ =>
        { choice = "1:limit:0"; RequireCurrent(); AssertSelected(subjects[0]); EndToEndAssert.True(IsChewing(subjects[0])); });
        yield return Capture<Dialog_ManageDrugPolicies>("capture the committed zero row", value => manager = value);
        yield return Shot("native policy row displays zero after the slider action while the selected dose remains in progress");
        yield return new WindowCancelActionStep("close policy manager before native serialization", typeof(Dialog_ManageDrugPolicies).FullName!);
        yield return new NativePointerScopeEndStep("restore pointer before saving the executing order", pointer);
        yield return new NativeMainTabActionStep("close Assign before native serialization", "Assign", false);
    }

    private IEnumerable<EndToEndStep> SaveAndRebind()
    {
        // Health/Gear evidence and pawn re-selection leave the native Inspect pane open.
        // Close that exact selected pawn's Gear pane through its existing native close command.
        yield return new InspectPaneCloseActionStep("close the selected pawn Gear pane before native serialization",
            subjects[resume ? 0 : 1].PawnId, typeof(ITab_Pawn_Gear).FullName!);
        yield return new AssertionStep("retain immutable native serialization inputs before the sole roundtrip", _ =>
        {
            RequireCurrent();
            EndToEndAssert.False(saveRequested, "This case permits exactly one native save/load request.");
            EndToEndAssert.True(Find.TickManager.Paused, "The native game must stay paused before saving; speed=" + Find.TickManager.CurTimeSpeed);
            EndToEndAssert.False(DebugSettings.fastEcology, "The inspected native load tick requires ordinary ecology ticking.");
            foreach (var subject in subjects) AssertNormalPawnTick(subject.Pawn, "before save: " + subject.Name);
            EndToEndAssert.False(File.Exists(SavePath), "The unique disposable save must not already exist: " + SaveName);
            AssertClosedEditors();
            EndToEndAssert.False(GameDataSaveLoader.SavingIsTemporarilyDisabled, "Native save admission must permit this real save action.");
            savedTick = Find.TickManager.TicksGame; serializedSave = settings.Store.ExportSave(binding);
            if (resume)
            {
                AssertSelected(subjects[0]); EndToEndAssert.True(IsChewing(subjects[0]) && subjects[0].Ingestions == 0);
                savedToil = subjects[0].Pawn.jobs.curDriver.CurToilIndex; savedTicksLeft = subjects[0].Pawn.jobs.curDriver.ticksLeftThisToil;
                EndToEndAssert.True(savedTicksLeft > 1, "The original dose must remain in chewing after the one native initialization tick; remaining=" + savedTicksLeft);
            }
            else EndToEndAssert.True(subjects.All(IsDraftedHold));
            saveRequested = true; phase = "native save/load requested";
        });
        yield return new CheckpointStep("immutable pre-save job/source/settings evidence", _ => Evidence());
        yield return new SaveLoadActionStep("native save and reload of this exact save-context case", SaveName);
        yield return new CheckpointStep("observe actual fresh loaded values before rebinding or validating progress", _ => LoadedEvidence());
        yield return new AssertionStep("resolve every current reference from the newly loaded Game", _ => RebindLoaded());
        yield return new CheckpointStep("fresh loaded identities before further native action", _ => Evidence());
    }

    private void RebindLoaded()
    {
        var current = Current.Game;
        EndToEndAssert.True(saveRequested && !reloaded, "Rebinding follows exactly one requested native load.");
        EndToEndAssert.True(current != null && !ReferenceEquals(current, initial), "Native load must replace the original Game object.");
        EndToEndAssert.True(current!.PlayerHasControl, "The loaded Game must be player-controlled.");
        EndToEndAssert.False(LongEventHandler.AnyEventNowOrWaiting, "The native load must finish its long events before rebinding.");
        EndToEndAssert.Equal(ProgramState.Playing, Current.ProgramState, "The loaded Game must be Playing.");
        var loadedMap = current.CurrentMap ?? throw new EndToEndAssertionException("The loaded Game must have a current map.");
        EndToEndAssert.Equal(mapId, loadedMap.uniqueID, "The loaded current map must retain the original immutable map ID.");
        EndToEndAssert.True(current.tickManager.Paused, "The native provider must restore the paused state.");
        EndToEndAssert.False(DebugSettings.fastEcology, "The exact native initialization tick requires ordinary ecology ticking.");
        // Game.LoadGame performs one DoSingleTick before setting Paused; this is not arbitrary slack.
        EndToEndAssert.Equal(checked(savedTick + 1), current.tickManager.TicksGame, "Paused native load performs exactly one initialization tick.");
        loadedTick = current.tickManager.TicksGame;
        var policies = current.drugPolicyDatabase.AllPolicies.Where(item => item.id == policyId && item.label == marker).ToArray();
        EndToEndAssert.Equal(1, policies.Length, "Resolve exactly one loaded policy by original immutable ID and owned marker.");
        var loadedPolicy = policies[0];
        var newBinding = SettingsBindingField.GetValue(settings) as SaveSettingsBinding
            ?? throw new InvalidOperationException("The native lifecycle pump has not published a loaded settings binding.");
        EndToEndAssert.True(ReferenceEquals(settings.LifecycleReceipt.Owner, current), "The native lifecycle receipt must own the actual loaded Game.");
        EndToEndAssert.False(newBinding.IsRevoked, "The native published loaded binding must not be revoked.");
        EndToEndAssert.True(binding.IsRevoked, "The original save binding must be revoked by native load.");
        EndToEndAssert.False(ReferenceEquals(newBinding, binding), "Native load must publish a fresh binding object.");
        EndToEndAssert.True(newBinding.Generation != binding.Generation, "The current binding must have a fresh activation generation.");
        EndToEndAssert.Equal(binding.SaveIdentity, newBinding.SaveIdentity, "The serialized save identity must survive loading.");
        EndToEndAssert.False(ReferenceEquals(loadedPolicy, policy), "Resolve a new policy object after loading.");
        EndToEndAssert.False(ReferenceEquals(loadedPolicy[beerDef], beerEntry), "Resolve a new row object after loading.");
        EndToEndAssert.True(serializedSave.Values.OrderBy(pair => pair.Key).SequenceEqual(settings.Store.ExportSave(newBinding).Values.OrderBy(pair => pair.Key)),
            "Every serialized save setting must survive the native roundtrip unchanged.");
        foreach (var subject in subjects)
        {
            var pawns = loadedMap.mapPawns.AllPawnsSpawned.Where(item => item.ThingID == subject.PawnId && item.Name?.ToStringFull == subject.FullName).ToArray();
            EndToEndAssert.Equal(1, pawns.Length, subject.Name + " must resolve once by its original ID/name.");
            var loadedPawn = pawns[0]; AssertNormalPawnTick(loadedPawn, "after load: " + subject.Name);
            var doses = loadedPawn.inventory.innerContainer.Concat(loadedPawn.carryTracker.innerContainer).Where(item => item.ThingID == subject.SourceId).ToArray();
            EndToEndAssert.Equal(1, doses.Length, subject.Name + " must still hold its exact original physical dose.");
            var loadedDose = doses[0];
            EndToEndAssert.False(ReferenceEquals(loadedPawn, subject.Pawn), subject.Name + " must have a fresh loaded Pawn reference.");
            EndToEndAssert.False(ReferenceEquals(loadedDose, subject.Source), subject.Name + " must have a fresh loaded source reference.");
            EndToEndAssert.True(loadedDose.def == beerDef && loadedDose.stackCount == 1, subject.Name + " must retain the same single Beer.");
            EndToEndAssert.True(ReferenceEquals(StoredPolicy(loadedPawn), loadedPolicy), subject.Name + " must already store the current loaded policy without default-policy fallback.");
            if (subject.Job != null)
            {
                var loadedJob = loadedPawn.CurJob;
                EndToEndAssert.True(loadedJob != null && !ReferenceEquals(loadedJob, subject.Job), subject.Name + " must have a fresh serialized current Job reference.");
                EndToEndAssert.Equal(subject.JobId, loadedJob!.loadID, subject.Name + " must retain the original selected job loadID.");
                EndToEndAssert.True(loadedJob.def == JobDefOf.Ingest && !loadedJob.playerForced, subject.Name + " must retain ordinary native ingestion.");
                EndToEndAssert.True(loadedJob.jobGiver?.GetType() == typeof(JobGiver_TakeDrugsForDrugPolicy), subject.Name + " must retain its actual scheduled job giver.");
                EndToEndAssert.True(ReferenceEquals(loadedJob.targetA.Thing, loadedDose), subject.Name + " must keep the exact loaded physical source as job target.");
                subject.Job = loadedJob;
            }
            subject.Pawn = loadedPawn; subject.Source = loadedDose;
        }
        game = current; map = loadedMap; policy = loadedPolicy; beerEntry = loadedPolicy[beerDef]; binding = newBinding; reloaded = true;
        AssertClosedEditors();
        if (resume)
        {
            AssertSelected(subjects[0]);
            EndToEndAssert.True(IsChewing(subjects[0]), "The loaded original job must still be executing its reconstructed native chewing toil with the same carried dose.");
            EndToEndAssert.Equal(0, subjects[0].Ingestions, "The one native load tick must not complete this deliberately unfinished dose.");
            loadedToil = subjects[0].Pawn.jobs.curDriver.CurToilIndex;
            loadedTicksLeft = subjects[0].Pawn.jobs.curDriver.ticksLeftThisToil;
            EndToEndAssert.Equal(savedToil, loadedToil, "Native load must restore the same current chewing toil index.");
            EndToEndAssert.Equal(savedTicksLeft - 1, loadedTicksLeft, "The one native load tick advances the same job by exactly one chewing tick.");
            AssertRetained(subjects[1]);
        }
        else foreach (var subject in subjects) { EndToEndAssert.True(IsDraftedHold(subject), subject.Name + " must retain its native drafted hold after loading."); AssertRetained(subject); }
        phase = "fresh native save activation"; RequireCurrent();
    }

    private static void AssertNormalPawnTick(Pawn pawn, string context) => EndToEndAssert.True(pawn.Spawned &&
        !pawn.Suspended && !pawn.IsWorldPawn() && pawn.def.tickerType == TickerType.Normal,
        context + ": exact load progress requires the inspected spawned, nonsuspended normal Pawn.Tick path.");

    private static DrugPolicy? StoredPolicy(Pawn pawn) => pawn.drugs == null ? null : PawnPolicyField.GetValue(pawn.drugs) as DrugPolicy;

    private IReadOnlyDictionary<string, string> LoadedEvidence()
    {
        // Read only actual Current.Game references resolved by immutable IDs; never rebind or create state here.
        var current = Current.Game; var currentMap = current?.CurrentMap;
        // CurrentBinding pumps lifecycle work; this diagnostic observes the already-published field only.
        var currentBinding = SettingsBindingField.GetValue(settings) as SaveSettingsBinding;
        var result = new Dictionary<string, string>
        {
            ["saveRequested"] = saveRequested.ToString(), ["alreadyRebound"] = reloaded.ToString(),
            ["newGame"] = (current != null && !ReferenceEquals(current, initial)).ToString(),
            ["programState"] = Current.ProgramState.ToString(), ["longEvent"] = LongEventHandler.AnyEventNowOrWaiting.ToString(),
            ["playerControl"] = current?.PlayerHasControl.ToString() ?? "no Game", ["map"] = currentMap?.uniqueID.ToString() ?? "none",
            ["savedTick"] = savedTick.ToString(), ["expectedLoadTick"] = checked(savedTick + 1).ToString(),
            ["actualLoadTick"] = current?.tickManager.TicksGame.ToString() ?? "no Game", ["paused"] = current?.tickManager.Paused.ToString() ?? "no Game",
            ["fastEcology"] = DebugSettings.fastEcology.ToString(), ["savedToil"] = savedToil.ToString(), ["savedTicksLeft"] = savedTicksLeft.ToString(),
            ["oldBindingRevoked"] = binding.IsRevoked.ToString(), ["binding"] = currentBinding == null ? "none" :
                currentBinding.SaveIdentity + ":generation=" + currentBinding.Generation + ":revoked=" + currentBinding.IsRevoked,
            ["lifecycleOwnerIsCurrent"] = ReferenceEquals(settings.LifecycleReceipt.Owner, current).ToString(),
            ["nativeWindows"] = string.Join(";", Find.WindowStack.Windows.Select(value => value.GetType().FullName))
        };
        var policies = current?.drugPolicyDatabase.AllPolicies.Where(item => item.id == policyId && item.label == marker).ToArray() ?? Array.Empty<DrugPolicy>();
        result["policyMatches"] = policies.Length.ToString();
        if (policies.Length == 1)
        {
            var rows = Enumerable.Range(0, policies[0].Count).Select(index => policies[0][index]).Where(row => row?.drug == beerDef).ToArray();
            result["rowMatches"] = rows.Length.ToString();
            if (rows.Length == 1) result["loadedChoice"] = NativeDrugToleranceState.ReadEncoded(rows[0]) ?? "absent";
        }
        foreach (var subject in subjects)
        {
            var pawns = currentMap?.mapPawns.AllPawnsSpawned.Where(value => value.ThingID == subject.PawnId && value.Name?.ToStringFull == subject.FullName).ToArray() ?? Array.Empty<Pawn>();
            result[subject.Name + " matches"] = pawns.Length.ToString();
            if (pawns.Length != 1) continue;
            var pawn = pawns[0]; var job = pawn.CurJob; var driver = pawn.jobs?.curDriver;
            var inventory = pawn.inventory?.innerContainer?.AsEnumerable() ?? Enumerable.Empty<Thing>();
            var carried = pawn.carryTracker?.innerContainer?.AsEnumerable() ?? Enumerable.Empty<Thing>();
            var doses = inventory.Concat(carried).Where(value => value.ThingID == subject.SourceId).ToArray();
            result[subject.Name] = "pawn=" + pawn.ThingID + ";samePreloadPawn=" + ReferenceEquals(pawn, subject.Pawn) +
                ";spawned=" + pawn.Spawned + ";suspended=" + pawn.Suspended + ";ticker=" + pawn.def.tickerType + ";drafted=" + pawn.Drafted +
                ";storedPolicy=" + StoredPolicy(pawn)?.id + ";savedJob=" + subject.JobId + ";job=" + job?.loadID + ";jobDef=" + job?.def?.defName +
                ";giver=" + job?.jobGiver?.GetType().FullName + ";playerForced=" + job?.playerForced + ";target=" + job?.targetA.Thing?.ThingID +
                ";driver=" + driver?.GetType().FullName + ";toil=" + driver?.CurToilIndex + ";ticksLeft=" + driver?.ticksLeftThisToil +
                ";carried=" + pawn.carryTracker?.CarriedThing?.ThingID + ";sourceMatches=" + doses.Length + ";history=" + pawn.drugs?.HasEverTaken(beerDef);
            if (doses.Length == 1) result[subject.Name + " source"] = doses[0].ThingID + ":def=" + doses[0].def.defName +
                ":count=" + doses[0].stackCount + ":destroyed=" + doses[0].Destroyed + ":samePreloadSource=" + ReferenceEquals(doses[0], subject.Source);
        }
        return result;
    }

    private static void AssertClosedEditors()
    {
        // SelectorOnGUI reopens the benign Inspect main tab while selection remains. The native
        // Gear close clears OpenTabType; an absent Inspect main tab is not a stable save requirement.
        var open = Find.MainTabsRoot.OpenTab;
        var mainTabs = Find.WindowStack.Windows.OfType<MainTabWindow>().ToArray();
        EndToEndAssert.True(mainTabs.Length <= 1 && mainTabs.All(tab => tab.GetType() == typeof(MainTabWindow_Inspect) &&
            ReferenceEquals(tab.def, MainButtonDefOf.Inspect)),
            "Only the ordinary selected-pawn Inspect main tab may remain; open=" + open?.defName);
        // Observe only stacked native windows; TabWindow's lazy getter would construct UI after load.
        var inspect = mainTabs.SingleOrDefault() as MainTabWindow_Inspect;
        EndToEndAssert.True(inspect == null || inspect.OpenTabType == null,
            "The native inner inspection tab must stay closed; inner=" + inspect?.OpenTabType?.FullName);
        EndToEndAssert.False(Find.WindowStack.IsOpen<Dialog_ModSettings>(), "Owned native settings must be closed at the save boundary.");
        EndToEndAssert.False(Find.WindowStack.IsOpen<Dialog_ManageDrugPolicies>(), "Owned native policy manager must be closed at the save boundary.");
    }

    private IEnumerable<EndToEndStep> Undraft(IEndToEndContext context, Subject subject)
    {
        yield return Select(subject);
        EndToEndAssert.True(subject.Pawn.Drafted);
        var toggle = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { subject.PawnId }, Array.Empty<string>())
            .Single(option => !option.Disabled && option.Interaction == EndToEndGizmoInteraction.Toggle && option.ToggleState == true && option.HotKeyDefName == "Command_ColonistDraft");
        yield return new GizmoActionStep("native Undraft for " + subject.Name, new[] { subject.PawnId }, toggle.RuntimeType,
            EndToEndGizmoInteraction.Toggle, stableGizmoId: toggle.StableId);
    }
    private IEnumerable<EndToEndStep> ReachChewing(Subject subject, int start)
    {
        yield return Run("let ordinary scheduling and ingestion progress for " + subject.Name);
        yield return Wait("observe actual native Beer chewing for " + subject.Name, _ =>
        {
            RequireCurrent();
            if (subject.Job == null && subject.Pawn.CurJobDef != JobDefOf.Ingest) { AssertRetained(subject); return false; }
            BindSelected(subject); AssertDecisions(subject, start, true);
            EndToEndAssert.True(!subject.Source.Destroyed && subject.Source.stackCount == 1 && subject.Ingestions == 0 && !subject.Pawn.drugs.HasEverTaken(beerDef));
            if (!IsChewing(subject)) return false;
            subject.ChewingTick = Find.TickManager.TicksGame; return true;
        }, 1800, 60);
        yield return Pause("pause the real carried chewing dose for " + subject.Name);
        yield return Frame("frame both subjects and the actual physical dose for " + subject.Name);
        yield return Shot(subject.Name + " performs ordinary native Beer ingestion with its exact carried dose");
    }
    private IEnumerable<EndToEndStep> FinishDose(Subject subject)
    {
        yield return Run("resume the actual selected dose for " + subject.Name);
        yield return Wait("observe exact-source consumption by the selected native job for " + subject.Name, _ =>
        {
            RequireCurrent();
            if (!subject.Source.Destroyed || !subject.Pawn.drugs.HasEverTaken(beerDef)) return false;
            EndToEndAssert.True(subject.Ingestions == 1 && subject.IngestedSameJob && subject.IngestedNative &&
                subject.IngestedJobId == subject.JobId && subject.IngestedTick >= subject.ChewingTick && subject.IngestedEpoch == (reloaded ? 1 : 0) &&
                Severity(subject, alcoholHigh) > 0 && Severity(subject, toleranceDef) > 0 &&
                subject.Pawn.inventory.innerContainer.TotalStackCountOfDef(beerDef) == 0 && subject.Pawn.carryTracker.CarriedThing == null);
            return true;
        }, 1800, 60);
        yield return Pause("pause after actual Beer consumption for " + subject.Name);
        yield return Health(subject); yield return Shot(subject.Name + " has native Alcohol effects from the observed original dose");
        yield return Gear(subject); yield return Shot(subject.Name + " no longer holds its consumed original Beer");
        yield return new CheckpointStep("physical dose and native completion provenance for " + subject.Name, _ => Evidence());
    }
    private IEnumerable<EndToEndStep> ObserveBlocked(Subject subject, int start)
    {
        yield return new AssertionStep("the new zero-limited selection rejects while retaining original Beer", _ =>
        { RequireCurrent(); AssertDecisions(subject, start, false); AssertRetained(subject); EndToEndAssert.True(subject.Pawn.CurJobDef != JobDefOf.Ingest); });
        int tick = Find.TickManager.TicksGame;
        yield return Run("allow repeated genuine native attempts under the current zero choice", EndToEndGameSpeed.Superfast);
        yield return Wait("observe repeated native zero refusals at different ticks", _ =>
        {
            RequireCurrent(); AssertRetained(subject); AssertDecisions(subject, start, false);
            EndToEndAssert.True(subject.Pawn.CurJobDef != JobDefOf.Ingest);
            return Find.TickManager.TicksGame - tick >= 600 && subject.Decisions.Skip(start).Select(value => value.Tick).Distinct().Count() >= 2;
        }, 2400, 90);
        yield return Pause("pause the rejected subject with its untouched Beer");
        yield return Gear(subject); yield return Shot(subject.Name + " retains its original Beer after actual repeated zero-limit refusals");
        yield return new CheckpointStep("fresh native rejection and retained physical source", _ => Evidence());
    }

    private void Install(MethodInfo target, string prefix, string? postfix = null)
    {
        traced.Add(target);
        HarmonyMethod Callback(string name) => new(typeof(NativeToleranceSaveContextFixture).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!);
        traceOwner.Patch(target, prefix: Callback(prefix), postfix: postfix == null ? null : Callback(postfix)); RequireCurrent();
    }
    private Subject? FindSubject(Pawn pawn) => ReferenceEquals(Current.Game, game) && !LongEventHandler.AnyEventNowOrWaiting
        ? subjects.FirstOrDefault(subject => ReferenceEquals(subject.Pawn, pawn)) : null;
    private static void BeginDecision(Pawn_DrugPolicyTracker __instance, ThingDef __0)
    {
        var fixture = active; var subject = fixture?.FindSubject(__instance.pawn);
        if (fixture == null || subject == null || __0 != fixture.beerDef || fixture.traceFault) return;
        if (subject.Decisions.Count >= 1024) { fixture.traceFault = true; return; }
        var storedPolicy = PawnPolicyField.GetValue(__instance) as DrugPolicy;
        if (storedPolicy == null)
        { fixture.traceFault = true; fixture.traceFailure = "Native scheduled observer found no stored policy for " + subject.PawnId; return; }
        var rows = Enumerable.Range(0, storedPolicy.Count).Select(index => storedPolicy[index]).Where(row => row?.drug == __0).ToArray();
        if (rows.Length != 1)
        { fixture.traceFault = true; fixture.traceFailure = "Native scheduled observer found " + rows.Length + " stored Beer rows for " + subject.PawnId; return; }
        var decision = new Decision { Tick = Find.TickManager.TicksGame, Epoch = fixture.reloaded ? 1 : 0,
            Choice = NativeDrugToleranceState.ReadEncoded(rows[0]) ?? "absent",
            Effective = fixture.settings.Store.GetEffective(DrugToleranceSettings.Enabled), History = __instance.HasEverTaken(__0),
            Before = fixture.Severity(subject, fixture.toleranceDef) };
        subject.Decisions.Add(decision); subject.Pending.Push(decision);
    }
    private static void ObserveAllowed(Pawn_DrugPolicyTracker __instance, ThingDef __0)
    {
        var fixture = active; var subject = fixture?.FindSubject(__instance.pawn);
        if (fixture == null || subject == null || __0 != fixture.beerDef || fixture.traceFault) return;
        if (subject.Pending.Count == 0) { fixture.traceFault = true; return; }
        subject.Pending.Peek().AllowedCalls++;
    }
    private static void CompleteDecision(Pawn_DrugPolicyTracker __instance, ThingDef __0, bool __result, bool __runOriginal)
    {
        var fixture = active; var subject = fixture?.FindSubject(__instance.pawn);
        if (fixture == null || subject == null || __0 != fixture.beerDef || fixture.traceFault) return;
        if (subject.Pending.Count == 0) { fixture.traceFault = true; return; }
        var decision = subject.Pending.Pop(); decision.Completed = true; decision.Result = __result; decision.RanOriginal = __runOriginal;
        decision.After = fixture.Severity(subject, fixture.toleranceDef); decision.ExitTick = Find.TickManager.TicksGame;
    }
    private static void ObserveIngestion(Thing __instance, Pawn __0)
    {
        var fixture = active; var subject = fixture?.FindSubject(__0);
        if (fixture == null || subject == null || !ReferenceEquals(__instance, subject.Source)) return;
        // Passive, value-only provenance. No target calls, ref/out arguments, or result/state changes.
        subject.Ingestions++; subject.IngestedTick = Find.TickManager.TicksGame;
        subject.IngestedJobId = __0.CurJob?.loadID ?? -1; subject.IngestedEpoch = fixture.reloaded ? 1 : 0;
        subject.IngestedSameJob = ReferenceEquals(__0.CurJob, subject.Job);
        subject.IngestedNative = __0.CurJob?.def == JobDefOf.Ingest && !__0.CurJob.playerForced &&
            __0.CurJob.jobGiver?.GetType() == typeof(JobGiver_TakeDrugsForDrugPolicy) && ReferenceEquals(__0.CurJob.targetA.Thing, __instance);
    }

    private void RequireGame() => EndToEndAssert.True(ReferenceEquals(Current.Game, game) && ReferenceEquals(Find.CurrentMap, map) &&
        Current.ProgramState == ProgramState.Playing && map.uniqueID == mapId, "Refuse stale or unrelated Game/map references.");
    private void RequireCurrent()
    {
        RequireGame(); EndToEndAssert.False(traceFault, traceFailure ?? "Native observer provenance failed.");
        EndToEndAssert.True(ReferenceEquals(settings.CurrentBinding, binding) && !binding.IsRevoked && settings.SaveLifecycleAvailable &&
            binding.SaveIdentity == initialSave.SaveIdentity && persistence.CanUseChoices &&
            ReferenceEquals(RuntimeField.GetValue(mod), runtime) && ReferenceEquals(PersistenceField.GetValue(mod), persistence));
        EndToEndAssert.True(settings.Store.GetGlobal(DrugToleranceSettings.Enabled) &&
            settings.Store.GetEffective(DrugToleranceSettings.Enabled) == expectedEnabled &&
            settings.Store.UsesGlobal(DrugToleranceSettings.Enabled) == expectedInheritance &&
            settings.Store.EffectiveOffReason(DrugToleranceSettings.Enabled) == null && runtime.Available == expectedEnabled,
            "Actual atomic availability must follow the intended ordinary setting, with healthy persistence: " + runtime.StatusKey);
        AssertRetainedAdmission();
        AssertGlobals();
        var saved = settings.Store.ExportSave(binding);
        EndToEndAssert.True(initialSave.Values.Where(pair => pair.Key != DrugToleranceSettings.Enabled.Id).OrderBy(pair => pair.Key)
            .SequenceEqual(saved.Values.Where(pair => pair.Key != DrugToleranceSettings.Enabled.Id).OrderBy(pair => pair.Key)),
            "The native tolerance controls do not rewrite unrelated save preferences.");
        if (expectedOverride != null)
            EndToEndAssert.True(saved.Values.TryGetValue(DrugToleranceSettings.Enabled.Id, out var value) &&
                value.UseGlobal == expectedInheritance && value.Override == expectedOverride);
        else EndToEndAssert.False(saved.Values.ContainsKey(DrugToleranceSettings.Enabled.Id));
        foreach (var target in traced)
        {
            var info = HarmonyOwner.GetPatchInfo(target);
            EndToEndAssert.True(info != null && info.Prefixes.Count(patch => patch.owner == traceOwner.Id) == 1 &&
                info.Postfixes.Count(patch => patch.owner == traceOwner.Id) == (target == ShouldTarget ? 1 : 0));
            if (target == AllowedTarget) EndToEndAssert.True(info!.Prefixes.Any(patch => patch.owner == "fumblesneeze.megamod") &&
                info.Finalizers.Any(patch => patch.owner == "fumblesneeze.megamod"));
            if (target == ShouldTarget) EndToEndAssert.True(info!.Postfixes.Count == 1 && info.Finalizers.Count == 0);
        }
        EndToEndAssert.True(game.drugPolicyDatabase.AllPolicies.Contains(policy) && policy.id == policyId && policy.label == marker &&
            ReferenceEquals(policy[beerDef], beerEntry) && NativeDrugToleranceState.ReadEncoded(beerEntry) == choice &&
            beerEntry.allowScheduled && !beerEntry.allowedForJoy && !beerEntry.allowedForAddiction && beerEntry.takeToInventory == 0 &&
            beerEntry.daysFrequency == 1f && beerEntry.onlyIfMoodBelow == 1f && beerEntry.onlyIfJoyBelow == 1f);
        foreach (var subject in subjects)
            EndToEndAssert.True(subject.Pending.Count == 0 && subject.Pawn.Spawned && ReferenceEquals(subject.Pawn.Map, map) &&
                subject.Pawn.ThingID == subject.PawnId && ReferenceEquals(StoredPolicy(subject.Pawn), policy) &&
                !subject.Pawn.Dead && !subject.Pawn.Downed && !subject.Pawn.InMentalState);
    }
    private void AssertGlobals() => EndToEndAssert.True(initialGlobals.Values.OrderBy(pair => pair.Key)
        .SequenceEqual(settings.Store.ExportGlobals().Values.OrderBy(pair => pair.Key)), "Global settings retain their exact encoded values.");
    private void AssertRetainedAdmission()
    {
        // Available intentionally short-circuits while off. Inspect already-admitted publication
        // receipts without calling any component Available/Admitted/Validate or refreshing admission.
        EndToEndAssert.True(Field(runtime.GetType(), "installed").GetValue(runtime) is true &&
            Field(runtime.GetType(), "resolutionFailure").GetValue(runtime) == null);
        var geometry = Field(runtime.GetType(), "geometry").GetValue(runtime) as NativeDrugToleranceGeometry
            ?? throw new InvalidOperationException("Actual geometry admission is missing.");
        EndToEndAssert.True(Field(geometry.GetType(), "resolutionFailure").GetValue(geometry) == null && geometry.Failure == null);
        var geometryRoutes = (List<DrugToleranceGeometryAdmission>)Field(geometry.GetType(), "admissions").GetValue(geometry)!;
        EndToEndAssert.Equal(8, geometryRoutes.Count);
        var ownedRoutes = geometryRoutes.Cast<object>().Concat(new[] { "allocation", "joy", "social", "header", "row" }
            .Select(name => Field(runtime.GetType(), name).GetValue(runtime) ?? throw new InvalidOperationException("Missing atomic member: " + name))).ToArray();
        EndToEndAssert.Equal(13, ownedRoutes.Length);
        var shared = Field(runtime.GetType(), "scheduledDecision").GetValue(runtime)
            ?? throw new InvalidOperationException("Shared scheduled owner is missing.");
        EndToEndAssert.True(ReferenceEquals(Field(mod.GetType(), "scheduledDrugDecision").GetValue(mod), shared) &&
            Field(shared.GetType(), "resolutionFailure").GetValue(shared) == null);
        var bridge = Field(shared.GetType(), "bridge").GetValue(shared)
            ?? throw new InvalidOperationException("Shared scheduled bridge is missing.");
        var sharedRoute = Field(bridge.GetType(), "decision").GetValue(bridge)
            ?? throw new InvalidOperationException("Shared scheduled decision receipt is missing.");
        var routes = ownedRoutes.Concat(new[] { sharedRoute }).ToArray();
        EndToEndAssert.Equal(14, routes.Length);
        var observed = new List<string>();
        foreach (var route in routes)
        {
            var type = route.GetType(); var target = (MethodInfo)Field(type, "target").GetValue(route)!;
            var generation = Field(type, "generation").GetValue(route);
            var receipt = Field(type, "receipt").GetValue(route);
            EndToEndAssert.True(Field(type, "installed").GetValue(route) is true && generation != null && receipt != null &&
                type.GetProperty("Failure")!.GetValue(route) == null, "Retained admission missing or failed: " + target);
            EndToEndAssert.True(ReferenceEquals(ReadProperty(receipt!, "Generation"), generation), "Receipt generation is stale: " + target);
            var identity = ReadProperty(receipt!, "Identity");
            var isCurrent = identity.GetType().GetMethod("IsCurrent", PrivateInstance)
                ?? throw new MissingMethodException(identity.GetType().FullName, "IsCurrent");
            EndToEndAssert.True(Equals(Field(identity.GetType(), "target").GetValue(identity), target) &&
                isCurrent.ReturnType == typeof(bool) && isCurrent.GetParameters().Length == 0 && isCurrent.Invoke(identity, null) is true,
                "Existing validated receipt must still name the actual published target: " + target);
            if (route is DrugToleranceSocialPatch)
            {
                var nativeList = (FieldInfo)Field(type, "candidateList").GetValue(route)!;
                EndToEndAssert.True(ReferenceEquals(Field(type, "workingList").GetValue(route), nativeList.GetValue(null)));
            }
            observed.Add((ReferenceEquals(route, sharedRoute) ? "shared:" : "S138-owned:") + target.DeclaringType!.FullName + "." + target.Name + ":generation=" + ReadProperty(generation!, "Version"));
        }
        retainedAdmission = string.Join(";", observed);
    }
    private static object ReadProperty(object owner, string name) => owner.GetType().GetProperty(name,
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(owner)
        ?? throw new MissingMemberException(owner.GetType().FullName, name);
    private void AssertRetained(Subject subject) => EndToEndAssert.True(!subject.Source.Destroyed && subject.Source.ThingID == subject.SourceId &&
        subject.Source.stackCount == 1 && ReferenceEquals(subject.Source.holdingOwner, subject.Pawn.inventory.innerContainer) &&
        !subject.Pawn.drugs.HasEverTaken(beerDef) && subject.Ingestions == 0 && Severity(subject, alcoholHigh) == 0);
    private void AssertDecisions(Subject subject, int start, bool result)
    {
        var observed = subject.Decisions.Skip(start).ToArray();
        EndToEndAssert.True(observed.Length > 0 && observed.All(value => value.Completed && value.RanOriginal && value.Result == result &&
            value.AllowedCalls == 1 && !value.History && value.Before == value.After && value.Tick == value.ExitTick &&
            value.Choice == choice && value.Effective == expectedEnabled && value.Epoch == (reloaded ? 1 : 0)), "Require actual final native selections in this exact context.");
    }
    private void BindSelected(Subject subject)
    {
        var job = subject.Pawn.CurJob;
        EndToEndAssert.True(job?.def == JobDefOf.Ingest && !job.playerForced &&
            job.jobGiver?.GetType() == typeof(JobGiver_TakeDrugsForDrugPolicy) && ReferenceEquals(job.targetA.Thing, subject.Source));
        if (subject.Job != null) { AssertSelected(subject); return; }
        subject.Job = job; subject.JobId = job!.loadID; subject.SelectedTick = Find.TickManager.TicksGame;
    }
    private void AssertSelected(Subject subject) => EndToEndAssert.True(subject.Job != null && ReferenceEquals(subject.Pawn.CurJob, subject.Job) &&
        subject.Pawn.CurJob.loadID == subject.JobId && subject.Pawn.CurJob.def == JobDefOf.Ingest && !subject.Pawn.CurJob.playerForced &&
        subject.Pawn.CurJob.jobGiver?.GetType() == typeof(JobGiver_TakeDrugsForDrugPolicy) && ReferenceEquals(subject.Pawn.CurJob.targetA.Thing, subject.Source));
    private static bool IsChewing(Subject subject)
    {
        if (subject.Pawn.jobs.curDriver is not JobDriver_Ingest driver || !ReferenceEquals(driver.job, subject.Pawn.CurJob)) return false;
        var chewing = ChewingField.GetValue(driver) as Toil; var toils = ToilsField.GetValue(driver) as List<Toil>;
        int index = driver.CurToilIndex;
        return chewing != null && toils != null && index >= 0 && index < toils.Count && ReferenceEquals(toils[index], chewing) &&
            driver.ticksLeftThisToil > 0 && ReferenceEquals(subject.Pawn.carryTracker.CarriedThing, subject.Source);
    }
    private static bool IsDraftedHold(Subject subject) => subject.Pawn.Drafted && subject.Pawn.CurJobDef == JobDefOf.Wait_Combat && subject.Pawn.jobs.IsCurrentJobPlayerInterruptible();
    private float Severity(Subject subject, HediffDef def) => subject.Pawn.health.hediffSet.GetFirstHediffOfDef(def)?.Severity ?? 0f;

    private IReadOnlyDictionary<string, string> Evidence()
    {
        RequireCurrent();
        var result = new Dictionary<string, string>
        {
            ["scope"] = resume ? "Native policy edit and same-dose job serialization/resume, then fresh zero rejection" : "Native dormant override roundtrip, inherited rejection and restored override ingestion",
            ["phase"] = phase, ["saveIdentity"] = binding.SaveIdentity, ["activation"] = binding.Generation.ToString(),
            ["epoch"] = reloaded ? "1" : "0", ["map"] = mapId.ToString(), ["policy"] = policyId + ":" + choice,
            ["effective"] = expectedEnabled.ToString(), ["inheritance"] = expectedInheritance.ToString(), ["dormantOverride"] = expectedOverride ?? "absent",
            ["available"] = runtime.Available.ToString(), ["persistenceCurrent"] = persistence.CanUseChoices.ToString(), ["traceOwner"] = traceOwner.Id,
            ["retainedAtomicReceipts"] = retainedAdmission,
            ["saveTick"] = savedTick.ToString(), ["savedToil"] = savedToil.ToString(), ["savedTicksLeft"] = savedTicksLeft.ToString(),
            ["loadTick"] = loadedTick.ToString(), ["loadedToil"] = loadedToil.ToString(), ["loadedTicksLeft"] = loadedTicksLeft.ToString(),
            ["nativeOperations"] = nativeOperations.ToString(), ["screenshots"] = screenshots.ToString(), ["waitSeconds"] = waitSeconds.ToString(), ["saves"] = saves.ToString(),
            ["settingsScrollClicks"] = settingsScrollClicks.ToString(), ["saveLoadReserveSeconds"] = "300", ["synchronousReserveSeconds"] = "180",
            ["nativeOpenTab"] = Find.MainTabsRoot.OpenTab?.defName ?? "none", ["paused"] = Find.TickManager.Paused.ToString(),
            ["nativeInspectTab"] = Find.WindowStack.WindowOfType<MainTabWindow_Inspect>()?.OpenTabType?.FullName ?? "none",
            ["nativeSaveBlocked"] = GameDataSaveLoader.SavingIsTemporarilyDisabled.ToString(),
            ["timeSpeed"] = Find.TickManager.CurTimeSpeed.ToString(), ["saveRequested"] = saveRequested.ToString(), ["savePathExists"] = File.Exists(SavePath).ToString()
        };
        foreach (var subject in subjects)
        {
            result[subject.Name] = subject.ToString() + ";alcohol=" + Severity(subject, alcoholHigh).ToString("R", CultureInfo.InvariantCulture) +
                ";tolerance=" + Severity(subject, toleranceDef).ToString("R", CultureInfo.InvariantCulture);
            result[subject.Name + " decisions"] = string.Join("\n", subject.Decisions.Select((value, index) => index + ":" + value));
        }
        foreach (var target in traced)
        {
            var info = HarmonyOwner.GetPatchInfo(target)!;
            result[target.Name] = string.Join(";", info.Prefixes.Select(patch => "prefix:" + patch.owner + ":" + patch.PatchMethod.Name)
                .Concat(info.Postfixes.Select(patch => "postfix:" + patch.owner + ":" + patch.PatchMethod.Name))
                .Concat(info.Transpilers.Select(patch => "transpiler:" + patch.owner + ":" + patch.PatchMethod.Name))
                .Concat(info.Finalizers.Select(patch => "finalizer:" + patch.owner + ":" + patch.PatchMethod.Name)));
        }
        return result;
    }

    private Game OwnedCleanupGame()
    {
        var current = Current.Game;
        EndToEndAssert.True(current != null && current.CurrentMap?.uniqueID == mapId && Current.ProgramState == ProgramState.Playing &&
            !LongEventHandler.AnyEventNowOrWaiting && (ReferenceEquals(current, initial) || ReferenceEquals(current, cleanupBoundGame) ||
            (reloaded && ReferenceEquals(current, game)) ||
            (saveRequested && current.drugPolicyDatabase.AllPolicies.Any(item => item.id == policyId && item.label == marker) &&
                subjects.All(subject => current.CurrentMap.mapPawns.AllPawnsSpawned.Any(pawn => pawn.ThingID == subject.PawnId && pawn.Name?.ToStringFull == subject.FullName)))),
            "Cleanup refuses a non-owned or partial Game.");
        cleanupBoundGame = current; // Retain proved identity after owned marker resources have been removed.
        return current!;
    }
    private void CleanupWorldAndSettings()
    {
        var current = OwnedCleanupGame(); var errors = new List<Exception>();
        foreach (var ownedWindow in ownedWindows.Distinct()) Attempt(errors, () =>
        {
            if (!Find.WindowStack.Windows.Contains(ownedWindow)) return;
            if (ownedWindow is MainTabWindow_Assign && ReferenceEquals(Find.MainTabsRoot.OpenTab?.TabWindow, ownedWindow)) Find.MainTabsRoot.EscapeCurrentTab(false);
            else ownedWindow.Close(false);
        });
        Attempt(errors, () => Find.Selector.ClearSelection());
        Attempt(errors, () =>
        {
            var currentBinding = settings.CurrentBinding ?? throw new InvalidOperationException("Cleanup needs the exact live save binding.");
            EndToEndAssert.True(currentBinding.SaveIdentity == initialSave.SaveIdentity && !currentBinding.IsRevoked);
            var values = (Dictionary<string, SaveSettingValue>)SaveValuesField.GetValue(settings.Store)!;
            // Cleanup only: native UI introduced this one entry; do not manufacture a new activation.
            if (initialSave.Values.TryGetValue(DrugToleranceSettings.Enabled.Id, out var prior)) values[DrugToleranceSettings.Enabled.Id] = prior;
            else values.Remove(DrugToleranceSettings.Enabled.Id);
            settings.Store.LoadGlobals(initialGlobals); // Publishes the restored save through the existing Changed subscriber.
            EndToEndAssert.True(ReferenceEquals(currentBinding, settings.CurrentBinding) &&
                initialSave.Values.OrderBy(pair => pair.Key).SequenceEqual(settings.Store.ExportSave(currentBinding).Values.OrderBy(pair => pair.Key)));
            var componentSnapshot = current.GetComponent<MegamodSaveSettings>().Snapshot;
            EndToEndAssert.True(componentSnapshot.SaveIdentity == initialSave.SaveIdentity &&
                initialSave.Values.OrderBy(pair => pair.Key).SequenceEqual(componentSnapshot.Values.OrderBy(pair => pair.Key)),
                "Existing Changed publication must restore the current native save component as well as its store.");
            AssertGlobals();
        });
        if (ReferenceEquals(current, initial))
            foreach (var thing in initialOwned.AsEnumerable().Reverse()) Attempt(errors, () => { if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish); });
        else
            foreach (var subject in subjects)
            {
                Pawn? pawn = null;
                Attempt(errors, () => pawn = current.CurrentMap.mapPawns.AllPawnsSpawned.SingleOrDefault(item => item.ThingID == subject.PawnId && item.Name?.ToStringFull == subject.FullName));
                Attempt(errors, () =>
                {
                    var ground = current.CurrentMap.listerThings.AllThings.Where(item => item.ThingID == subject.SourceId && item.def == beerDef);
                    var held = pawn == null ? Enumerable.Empty<Thing>() : pawn.inventory.innerContainer.Concat(pawn.carryTracker.innerContainer);
                    foreach (var dose in ground.Concat(held.Where(item => item.ThingID == subject.SourceId)).Distinct().ToArray())
                        Attempt(errors, () => { if (!dose.Destroyed) dose.Destroy(DestroyMode.Vanish); });
                });
                Attempt(errors, () => { if (pawn != null && !pawn.Destroyed) pawn.Destroy(DestroyMode.Vanish); });
            }
        Attempt(errors, () => current.drugPolicyDatabase.AllPolicies.RemoveAll(item => item.id == policyId && item.label == marker));
        if (errors.Count != 0) throw new AggregateException("Tolerance save-context cleanup failed.", errors);
    }
    private static void Attempt(List<Exception> errors, Action action) { try { action(); } catch (Exception error) { errors.Add(error); } }
    private static FieldInfo Field(Type owner, string name) => owner.GetField(name, PrivateInstance) ?? throw new MissingFieldException(owner.FullName, name);

    private CameraActionStep Frame(string name) => new(name, subjects.Select(subject => subject.PawnId), paddingPixels: 200);
    private static SelectionActionStep Select(Subject subject) => new("select " + subject.Name, new[] { subject.PawnId }, additive: false);
    private static PawnInspectTabActionStep Health(Subject subject) => new("show native Health for " + subject.Name, subject.PawnId, EndToEndPawnInspectTab.Health);
    private static PawnInspectTabActionStep Gear(Subject subject) => new("show physical Beer in Gear for " + subject.Name, subject.PawnId, EndToEndPawnInspectTab.Gear);
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private static TimeControlActionStep Pause(string name) => new(name, true, EndToEndGameSpeed.Normal);
    private static TimeControlActionStep Run(string name, EndToEndGameSpeed speed = EndToEndGameSpeed.Normal) => new(name, false, speed);
    private static WaitUntilStep Wait(string name, Func<IEndToEndContext, bool> predicate, int ticks, int seconds) => new(name, predicate,
        new EndToEndDeadline(int.MaxValue, ticks, TimeSpan.FromSeconds(seconds)));
    private NativeWindowClickActionStep Click(string name, Vector2 point) => new(name, window.Handle,
        (window.X + point.x) * window.UiScale, (window.Y + point.y) * window.UiScale, null, pointer);
    private NativeWindowCaptureStep Capture<T>(string name, Action<T> retain) where T : Window => new(name, windows =>
    {
        RequireCurrent(); var actual = Find.WindowStack.WindowOfType<T>() ?? throw new InvalidOperationException("Missing native window " + typeof(T));
        ownedWindows.Add(actual); retain(actual);
        window = windows.Single(item => item.NativeId == actual.ID && item.RuntimeType == actual.GetType().FullName);
    });

    private sealed class Subject
    {
        internal Pawn Pawn;
        internal Thing Source;
        internal Job? Job;
        internal readonly string PawnId, SourceId, Name, FullName;
        internal int JobId = -1, SelectedTick = -1, ChewingTick = -1, Ingestions, IngestedJobId = -1, IngestedTick = -1, IngestedEpoch = -1;
        internal bool IngestedSameJob, IngestedNative;
        internal readonly List<Decision> Decisions = new();
        internal readonly Stack<Decision> Pending = new();
        internal Subject(Pawn pawn, Thing source)
        { Pawn = pawn; Source = source; PawnId = pawn.ThingID; SourceId = source.ThingID; Name = pawn.Name.ToStringShort; FullName = pawn.Name.ToStringFull; }
        public override string ToString() => "pawn=" + PawnId + ";source=" + SourceId + ";destroyed=" + Source.Destroyed +
            ";job=" + JobId + ";selected=" + SelectedTick + ";chewing=" + ChewingTick + ";ingestions=" + Ingestions +
            ";ingestedJob=" + IngestedJobId + ";ingestedTick=" + IngestedTick + ";ingestedEpoch=" + IngestedEpoch +
            ";sameJob=" + IngestedSameJob + ";nativeSource=" + IngestedNative;
    }
    private sealed class Decision
    {
        internal int Tick, ExitTick, Epoch, AllowedCalls;
        internal float Before, After;
        internal string Choice = null!;
        internal bool Effective, History, Completed, Result, RanOriginal;
        public override string ToString() => $"epoch={Epoch};tick={Tick}/{ExitTick};choice={Choice};effective={Effective};raw={Before:R}/{After:R};allowed={AllowedCalls};history={History};complete={Completed};original={RanOriginal};result={Result}";
    }
}
