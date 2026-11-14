// Requires the separately reviewed NativeEntryMenuSteps capability.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using HarmonyLib;
using LudeonTK;
using Megamod.EndToEndTests.Diagnostics;
using Megamod.Features.DrugTolerance;
using Megamod.Features.DrugTolerance.Core;
using Megamod.Features.DrugTolerance.Runtime;
using Megamod.Settings;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;
using Verse.AI;
using HarmonyOwner = HarmonyLib.Harmony;

namespace Megamod.EndToEndTests.DrugTolerance;

[RimWorldEndToEndTest("megamod.drug-tolerance.native-main-menu-save-switch", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 12000, MaxWallClockSeconds = 3420)]
public sealed class NativeToleranceSaveSwitchTests : IRimWorldEndToEndTest, IEndToEndProcessOwnedGameFixture
{
    private const BindingFlags InstanceFields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
    private static readonly FieldInfo BindingField = Field(typeof(SettingsRuntime), "binding");
    private static readonly FieldInfo SaveValuesField = Field(typeof(SettingsStore), "saveValues");
    private static readonly FieldInfo GlobalValuesField = Field(typeof(SettingsStore), "globals");
    private static readonly FieldInfo GlobalRevisionField = Field(typeof(SettingsStore), "globalRevision");
    private static readonly FieldInfo PolicyField = Field(typeof(Pawn_DrugPolicyTracker), "curPolicy");
    private static readonly FieldInfo RuntimeField = Field(typeof(MegamodMod), "drugTolerance");
    private static readonly FieldInfo PersistenceField = Field(typeof(MegamodMod), "drugTolerancePersistence");
    private static readonly FieldInfo ChewingField = Field(typeof(JobDriver_Ingest), "chewing");
    private static readonly FieldInfo ToilsField = Field(typeof(JobDriver), "toils");
    private static readonly MethodInfo Should = typeof(Pawn_DrugPolicyTracker).GetMethod(nameof(Pawn_DrugPolicyTracker.ShouldTryToTakeScheduledNow))!;
    private static readonly MethodInfo Allowed = typeof(Pawn_DrugPolicyTracker).GetMethod(nameof(Pawn_DrugPolicyTracker.AllowedToTakeScheduledNow))!;
    private static readonly MethodInfo Ingested = typeof(Thing).GetMethod(nameof(Thing.Ingested), new[] { typeof(Pawn), typeof(float) })!;
    private static NativeToleranceSaveSwitchTests? active;
    private readonly string marker = "ToleranceSwitch-" + Guid.NewGuid().ToString("N").Substring(0, 10);
    private readonly List<SaveRecord> saves = new();
    private readonly List<Window> ownedWindows = new();
    private readonly List<MethodInfo> traced = new();
    private SettingsRuntime settings = null!;
    private GlobalSettingsSnapshot globals = null!;
    private MegamodMod mod = null!;
    private NativeDrugToleranceRuntime runtime = null!;
    private DrugTolerancePersistencePatch persistence = null!;
    private NativeDiagnosticSettingsView settingsView = null!;
    private ThingDef beer = null!, yayo = null!;
    private HediffDef alcohol = null!, tolerance = null!;
    private SaveRecord a = null!, b = null!;
    private SaveRecord? current;
    private HarmonyOwner traceOwner = null!;
    private EndToEndNativeWindow window = null!;
    private Window actualWindow = null!;
    private string? pointer;
    private string entryHandle = null!;
    private string phase = "arrange";
    private string? traceFailure;
    private string retainedAdmission = "unobserved";
    private int nativeOperations, screenshots, waitSeconds, scrollClicks;

    public void Arrange(IEndToEndContext context)
    {
        EndToEndAssert.True(active == null && Current.Game?.PlayerHasControl == true &&
            Current.ProgramState == ProgramState.Playing && !LongEventHandler.AnyEventNowOrWaiting && Prefs.DevMode);
        EndToEndAssert.True(Find.MainTabsRoot.OpenTab == null && !Find.WindowStack.IsOpen<Dialog_Options>() &&
            !Find.WindowStack.IsOpen<Dialog_ModSettings>() && !Find.WindowStack.IsOpen<EditWindow_Log>());
        settings = SettingsRuntime.Instance ?? throw new InvalidOperationException("Common settings unavailable.");
        globals = settings.Store.ExportGlobals();
        // Registered first: LIFO cleanup restores globals even when later Game cleanup refuses ownership.
        context.DeferCleanup(CleanupGlobals);
        EndToEndAssert.True(settings.Store.GetGlobal(DrugToleranceSettings.Enabled) && settings.SaveLifecycleAvailable &&
            settings.Store.EffectiveOffReason(DrugToleranceSettings.Enabled) == null);
        mod = LoadedModManager.GetMod<MegamodMod>();
        runtime = RuntimeField.GetValue(mod) as NativeDrugToleranceRuntime ?? throw new InvalidOperationException("Tolerance runtime unavailable.");
        persistence = PersistenceField.GetValue(mod) as DrugTolerancePersistencePatch ?? throw new InvalidOperationException("Persistence unavailable.");
        EndToEndAssert.True(BindingField.FieldType == typeof(SaveSettingsBinding) && PolicyField.FieldType == typeof(DrugPolicy) &&
            SaveValuesField.FieldType == typeof(Dictionary<string, SaveSettingValue>) &&
            GlobalValuesField.FieldType == typeof(Dictionary<string, string>) && GlobalRevisionField.FieldType == typeof(long) &&
            ChewingField.FieldType == typeof(Toil) &&
            ToilsField.FieldType == typeof(List<Toil>));
        bool oldPauseOnLoad = Prefs.PauseOnLoad, oldEcology = DebugSettings.fastEcology;
        context.DeferCleanup(() => { Prefs.PauseOnLoad = oldPauseOnLoad; DebugSettings.fastEcology = oldEcology; });
        Prefs.PauseOnLoad = true; DebugSettings.fastEcology = false;
        settingsView = new(() => pointer ?? throw new InvalidOperationException("No owned pointer scope."), DrugToleranceSettings.Enabled.Id);
        var oldScroll = settingsView.Scroll;
        context.DeferCleanup(() => settingsView.RestoreScroll(oldScroll));
        context.DeferCleanup(CleanupFiles);
        context.DeferCleanup(CleanupCurrentGame);
        beer = DefDatabase<ThingDef>.GetNamed("Beer"); yayo = DefDatabase<ThingDef>.GetNamed("Yayo");
        alcohol = DefDatabase<HediffDef>.GetNamed("AlcoholHigh"); tolerance = DefDatabase<HediffDef>.GetNamed("AlcoholTolerance");
        a = ArrangeSave("A", true, "1:limit:0.2");
        traceOwner = new HarmonyOwner("fumblesneeze.tests.s138.save-switch." + marker);
        active = this;
        context.DeferCleanup(CleanupTraces);
        Install(Should, nameof(BeginDecision), nameof(EndDecision));
        Install(Allowed, nameof(ObserveAllowed));
        Install(Ingested, nameof(ObserveIngestion));
        RequireCurrent();
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        foreach (var step in Workflow(context))
        {
            if (step is ScreenshotStep) screenshots++;
            else if (step is WaitUntilStep wait) waitSeconds += checked((int)wait.Deadline.MaxWallClock.TotalSeconds);
            else if (step.Kind == EndToEndStepKind.Act || step is NativeWindowCaptureStep || step is NativeEntryMenuCaptureStep) nativeOperations++;
            yield return step;
        }
        yield return new AssertionStep("verify exact native save-switch budget and outcome inventory", _ =>
        {
            RequireCurrent();
            EndToEndAssert.True(scrollClicks >= 0 && scrollClicks <= 9);
            EndToEndAssert.Equal(2, saves.Count);
            EndToEndAssert.True(saves.All(save => save.Saved && save.Loads == 1));
            EndToEndAssert.Equal(84 + 2 * scrollClicks, nativeOperations);
            EndToEndAssert.Equal(14, screenshots);
            EndToEndAssert.Equal(980, waitSeconds);
            EndToEndAssert.True(nativeOperations * 15 + screenshots * 30 + waitSeconds + 180 <= 3420);
            EndToEndAssert.True(a.Subject.Decisions.Count >= 2 && b.Subject.Ingestions == 1);
        });
        yield return new CheckpointStep("final save-switch causality and accounting", _ => Evidence());
    }

    private IEnumerable<EndToEndStep> Workflow(IEndToEndContext context)
    {
        foreach (var step in InitialView(a)) yield return step;
        foreach (var step in SaveNative(a)) yield return step;
        yield return CaptureForInput<MainTabWindow_Menu>("capture Menu reopened by native Save", _ => { });
        yield return new AssertionStep("fresh paused Save permits native Quit without confirmation", _ =>
        { RequireCurrent(); EndToEndAssert.False(GameDataSaveLoader.CurrentGameStateIsValuable); });
        yield return ClickContent("native Quit to main menu", MenuButton("QuitToMainMenu").center);
        yield return Wait("native Entry teardown revokes A without an implicit save", _ =>
            Current.ProgramState == ProgramState.Entry && Current.Game == null && !LongEventHandler.AnyEventNowOrWaiting, 0, 90);
        yield return new AssertionStep("observe completed Entry before opening settings", _ =>
        { current = null; phase = "Entry after A"; RequireEntry(); });
        yield return Shot("native main menu after A has been disposed");
        yield return EntryCapture("capture actual Entry Options", "options");
        yield return new NativeEntryMenuClickActionStep("open Options from native main menu", entryHandle);
        foreach (var step in EntrySettings()) yield return step;
        yield return EntryCapture("capture fresh Entry Dev Quicktest after closing Options", "dev-quicktest");
        yield return new NativeEntryMenuClickActionStep("create disposable B through native Dev Quicktest", entryHandle);
        yield return Wait("native Quicktest creates a distinct controlled Game", _ => Current.ProgramState == ProgramState.Playing &&
            Current.Game?.PlayerHasControl == true && !ReferenceEquals(Current.Game, a.Game) && !LongEventHandler.AnyEventNowOrWaiting, 0, 240);
        yield return new TimeControlActionStep("pause the new B before arranging its disposable subject", true, EndToEndGameSpeed.Normal);
        yield return new CheckpointStep("observe B native identity before any precondition seed", _ => LifecycleEvidence());
        yield return new AssertionStep("arrange only B's independent starting override and dose", _ =>
        { b = ArrangeSave("B", false, "1:limit:0.4"); EndToEndAssert.False(a.Identity == b.Identity); phase = "B arranged"; });
        foreach (var step in InitialView(b)) yield return step;
        foreach (var step in SaveNative(b)) yield return step;
        foreach (var step in LoadNative(a, menuAlreadyOpen: true)) yield return step;
        foreach (var step in LoadedSettings("A retains Current save on while Global default remains on")) yield return step;
        foreach (var step in Undraft(context, a.Subject)) yield return step;
        int startTick = a.Game.tickManager.TicksGame;
        yield return Run("let A make ordinary zero-limit decisions", EndToEndGameSpeed.Superfast);
        yield return Wait("A repeatedly refuses with the same original Beer", _ =>
        {
            RequireCurrent(); Retained(a.Subject);
            if (a.Subject.Decisions.Count != 0) AssertDecisions(a, false);
            EndToEndAssert.True(a.Subject.Pawn.CurJobDef != JobDefOf.Ingest);
            return a.Game.tickManager.TicksGame - startTick >= 600 && a.Subject.Decisions.Select(item => item.Tick).Distinct().Count() >= 2;
        }, 2400, 90);
        yield return Pause("pause A after repeated actual refusal");
        yield return new CameraActionStep("frame A with its untouched original Beer", new[] { a.Subject.PawnId }, paddingPixels: 200);
        yield return Gear(a.Subject); yield return Shot("save A retains its original Beer under numeric zero and save-on");
        yield return new CheckpointStep("A current-save native refusals", _ => Evidence());
        foreach (var step in LoadNative(b, menuAlreadyOpen: false)) yield return step;
        foreach (var step in LoadedSettings("B independently retains Current save off beside Global default on")) yield return step;
        foreach (var step in Undraft(context, b.Subject)) yield return step;
        yield return Run("let B's ordinary scheduler choose its original Beer");
        yield return Wait("observe B's actual ordinary scheduled chewing", _ =>
        {
            RequireCurrent();
            if (b.Subject.Pawn.CurJobDef != JobDefOf.Ingest) { Retained(b.Subject); return false; }
            BindJob(b.Subject); AssertDecisions(b, true);
            if (!IsChewing(b.Subject)) return false;
            b.Subject.ChewingTick = b.Game.tickManager.TicksGame; return true;
        }, 1800, 60);
        yield return Pause("pause B's selected native chewing dose");
        yield return new CameraActionStep("frame B's actual drinking", new[] { b.Subject.PawnId }, paddingPixels: 200);
        yield return Shot("save B performs ordinary drinking with its exact original Beer");
        yield return Run("finish B's selected native dose");
        yield return Wait("observe B's physical Beer consumption and native Health effects", _ =>
        {
            RequireCurrent(); var subject = b.Subject;
            if (!subject.Source.Destroyed || !subject.Pawn.drugs.HasEverTaken(beer)) return false;
            EndToEndAssert.True(subject.Ingestions == 1 && subject.IngestedSameJob && subject.IngestedNative &&
                subject.IngestedEpoch == b.Epoch && subject.IngestedJobId == subject.JobId && subject.IngestedTick >= subject.ChewingTick &&
                Severity(subject, alcohol) > 0 && Severity(subject, tolerance) > 0 &&
                subject.Pawn.inventory.innerContainer.TotalStackCountOfDef(beer) == 0 && subject.Pawn.carryTracker.CarriedThing == null);
            return true;
        }, 1800, 60);
        yield return Pause("pause after B's original Beer is consumed");
        yield return new PawnInspectTabActionStep("show B's native Alcohol effects", b.Subject.PawnId, EndToEndPawnInspectTab.Health);
        yield return Shot("B has native Alcohol effects after the observed dose");
        yield return Gear(b.Subject); yield return Shot("B's original physical Beer is gone from Gear");
        yield return new NativeDeveloperLogActionStep("open native console after save switching", true);
        yield return Capture<EditWindow_Log>("capture the exact native console", _ => { });
        yield return Shot("native console after Entry and two-save actions");
        yield return new NativeDeveloperLogActionStep("close native console after evidence", false);
    }

    private IEnumerable<EndToEndStep> InitialView(SaveRecord save)
    {
        yield return new CameraActionStep("frame save " + save.Label + " starting subject", new[] { save.Subject.PawnId }, paddingPixels: 200);
        yield return new SelectionActionStep("select save " + save.Label + " subject", new[] { save.Subject.PawnId }, additive: false);
        yield return Gear(save.Subject);
        yield return Shot("save " + save.Label + " owns one untouched Beer before native saving");
    }

    private IEnumerable<EndToEndStep> SaveNative(SaveRecord save)
    {
        yield return new InspectPaneCloseActionStep("close Gear before saving " + save.Label, save.Subject.PawnId, typeof(ITab_Pawn_Gear).FullName!);
        yield return new EscapeMenuActionStep("open native Menu to save " + save.Label, true);
        yield return CaptureForInput<MainTabWindow_Menu>("capture native Save entry", _ => { });
        yield return BeginPointer("own pointer for native save name and button");
        yield return ClickContent("open native Save dialog for " + save.Label, MenuButton("Save").center);
        yield return CaptureForInput<Dialog_SaveFileList_Save>("capture native save name field", _ => { });
        float nameY = (float)window.Height - 36 - Window.CloseButSize.y - 18 - 35;
        yield return ClickContent("focus native MapNameField", new Vector2(205, nameY + 17.5f));
        yield return CaptureForInput<Dialog_SaveFileList_Save>("capture focused native MapNameField", _ => { });
        EndToEndAssert.True(!string.IsNullOrEmpty(window.FocusedTextControlHandle));
        yield return new NativeWindowReplaceTextActionStep("type unique disposable save name", window.Handle, window.FocusedTextControlHandle!, save.Name);
        yield return new AssertionStep("bind immutable save inputs after native text entry", _ =>
        {
            RequireCurrent(); Retained(save.Subject);
            EndToEndAssert.Equal(save.Name, Read<string>(typeof(Dialog_FileList), actualWindow, "typingName"));
            EndToEndAssert.True(save.Game.tickManager.Paused && !DebugSettings.fastEcology && !File.Exists(save.Path));
            save.SavedTick = save.Game.tickManager.TicksGame;
            save.Snapshot = settings.Store.ExportSave(save.Binding);
            save.Requested = true;
        });
        yield return ClickContent("commit the native Save button", new Vector2(420 + ((float)window.Width - 36 - 420) / 2, nameY + 17.5f));
        yield return EndPointer("restore pointer after native Save gesture");
        yield return Wait("native save finishes with its own file and reopened Menu", _ => !LongEventHandler.AnyEventNowOrWaiting &&
            File.Exists(save.Path) && new FileInfo(save.Path).Length > 0 && !File.Exists(save.Path + ".new") &&
            !Find.WindowStack.IsOpen<Dialog_SaveFileList_Save>() && Find.WindowStack.IsOpen<MainTabWindow_Menu>(), 0, 30);
        yield return new AssertionStep("retain exact completed native save identity", _ =>
        { RequireCurrent(); save.Hash = Hash(save.Path); save.Saved = true; EndToEndAssert.Equal(save.SavedTick, save.Game.tickManager.TicksGame); });
        yield return new CheckpointStep("native save " + save.Label + " file and settings provenance", _ => Evidence());
    }

    private IEnumerable<EndToEndStep> LoadNative(SaveRecord target, bool menuAlreadyOpen)
    {
        var departed = current ?? throw new InvalidOperationException("Load requires the actual current save.");
        if (!menuAlreadyOpen) yield return new EscapeMenuActionStep("open native Menu for save " + target.Label, true);
        yield return CaptureForInput<MainTabWindow_Menu>("capture native Load entry", _ => { });
        yield return ClickContent("open native Load dialog for save " + target.Label, MenuButton("LoadGame").center);
        yield return CaptureForInput<Dialog_SaveFileList_Load>("capture exact native save file row", _ => { });
        Vector2 button = LoadButton(target);
        yield return Shot("native Load dialog before choosing save " + target.Label);
        yield return ClickContent("load exact native save " + target.Label + " row", button);
        yield return Wait("native load completes for save " + target.Label, _ => Current.Game?.PlayerHasControl == true &&
            Current.ProgramState == ProgramState.Playing && !ReferenceEquals(Current.Game, departed.Game) && !LongEventHandler.AnyEventNowOrWaiting, 0, 180);
        yield return new CheckpointStep("fresh loaded identity before fixture rebinding", _ => LifecycleEvidence());
        yield return new AssertionStep("rebind only existing loaded save " + target.Label + " objects", _ => BindLoaded(target, departed));
        yield return new CheckpointStep("fresh current save " + target.Label + " values", _ => Evidence());
    }

    private IEnumerable<EndToEndStep> EntrySettings()
    {
        yield return CaptureForInput<Dialog_Options>("capture Entry Options", _ => { });
        yield return BeginPointer("retain pointer for Entry settings and disabled attempts");
        var options = (Dialog_Options)actualWindow;
        var categories = DefDatabase<OptionCategoryDef>.AllDefs.Where(item => (Prefs.DevMode || !item.isDev) &&
            item.modContentPack.IsOfficialMod && (item != OptionCategoryDefOf.Mods || Read<bool>(typeof(Dialog_Options), options, "hasModSettings"))).ToList();
        int index = categories.IndexOf(OptionCategoryDefOf.Mods); EndToEndAssert.True(index >= 0);
        yield return ClickContent("choose native Mods category at Entry", new Vector2(80, index * 50 + 24));
        yield return CaptureForInput<Dialog_Options>("capture rendered Mods category", _ => { });
        yield return ClickContent("open megamod through its actual native settings entry", ModButton(options));
        foreach (var step in MeasureSetting()) yield return step;
        Rect global = settingsView.MeasureCheckbox(out bool visible); EndToEndAssert.True(visible && global.yMin >= 22);
        var inherit = new Rect(global.x - 2 * (global.width + 8), global.y - 22, global.width, 28);
        var local = new Rect(global.x - (global.width + 8), global.y, global.width, 28);
        yield return new AssertionStep("main menu has no current or previous save authority", _ => RequireEntry());
        yield return Shot("Entry shows disabled Use global and Current save with its no-save explanation");
        yield return settingsView.Click("attempt disabled Use global at the main menu", inherit.center);
        yield return settingsView.Click("attempt disabled Current save at the main menu", local.center);
        yield return new AssertionStep("disabled native clicks changed neither globals nor A's saved bytes", _ => RequireEntry());
        yield return Shot("main-menu save controls remain disabled after both attempted clicks");
        yield return new CheckpointStep("Entry disabled actions leave globals and saved A untouched", _ => Evidence());
        yield return ClickOuter("close native megamod settings at Entry", new Vector2((float)window.Width / 2, (float)window.Height - 35));
        yield return Wait("the Entry mod dialog closes", _ => !Find.WindowStack.IsOpen<Dialog_ModSettings>(), 0, 5);
        yield return CaptureForInput<Dialog_Options>("recapture Options beneath the closed mod dialog", _ => { });
        yield return ClickContent("close Entry Options using native OK", new Vector2(((float)window.Width - 36) / 2, (float)window.Height - 36 - 20));
        yield return Wait("Options closes before new Game creation", _ => !Find.WindowStack.IsOpen<Dialog_Options>(), 0, 5);
        yield return EndPointer("restore Entry pointer before native Quicktest");
        yield return new AssertionStep("Entry remains unbound after native settings closure", _ => RequireEntry());
    }

    private IEnumerable<EndToEndStep> LoadedSettings(string caption)
    {
        yield return new ModSettingsActionStep("open native settings for " + caption, "fumblesneeze.megamod");
        yield return BeginPointer("retain pointer for loaded save settings");
        foreach (var step in MeasureSetting()) yield return step;
        yield return new AssertionStep("loaded controls project this save's independent setting", _ => RequireCurrent());
        yield return Shot(caption);
        yield return ClickOuter("close loaded save settings with native Close", new Vector2((float)window.Width / 2, (float)window.Height - 35));
        yield return Wait("loaded settings dialog closes", _ => !Find.WindowStack.IsOpen<Dialog_ModSettings>(), 0, 5);
        yield return EndPointer("restore pointer after loaded settings view");
    }

    private IEnumerable<EndToEndStep> MeasureSetting()
    {
        for (int attempt = 0; ; attempt++)
        {
            yield return CaptureForInput<Dialog_ModSettings>("capture current common settings row", _ => { });
            settingsView.Bind(window, (Dialog_ModSettings)actualWindow);
            Rect target = settingsView.MeasureCheckbox(out bool visible);
            if (visible && target.yMin >= 22) yield break;
            EndToEndAssert.True(attempt < 3 && Math.Abs(settingsView.Scroll.x) < .01f);
            scrollClicks++;
            yield return settingsView.Click("scroll native settings track to tolerance", settingsView.PlanTrackClick(target));
        }
    }

    private SaveRecord ArrangeSave(string label, bool enabled, string yayoChoice)
    {
        var game = Current.Game ?? throw new InvalidOperationException("Native Game missing.");
        EndToEndAssert.True(game.PlayerHasControl && game.tickManager.Paused && !LongEventHandler.AnyEventNowOrWaiting);
        var binding = BindingField.GetValue(settings) as SaveSettingsBinding ?? throw new InvalidOperationException("Native binding not yet published.");
        EndToEndAssert.True(!binding.IsRevoked && ReferenceEquals(settings.LifecycleReceipt.Owner, game));
        var initial = settings.Store.ExportSave(binding);
        var record = new SaveRecord(label, marker + "-" + label, game, binding, initial, enabled, yayoChoice);
        EndToEndAssert.False(saves.Any(value => value.Identity == record.Identity));
        EndToEndAssert.False(File.Exists(record.Path) || File.Exists(record.Path + ".new"));
        RequireOwnedPath(record);
        saves.Add(record); current = record; // Register ownership before disposable mutations.
        EndToEndAssert.True(settings.Store.SetUseGlobal(binding, DrugToleranceSettings.Enabled, false).Succeeded);
        EndToEndAssert.True(settings.Store.SetSaveOverride(binding, DrugToleranceSettings.Enabled, enabled).Succeeded);
        record.Policy = game.drugPolicyDatabase.MakeNewDrugPolicy();
        record.Policy.label = record.Name; record.PolicyId = record.Policy.id;
        for (int index = 0; index < record.Policy.Count; index++)
        {
            var row = record.Policy[index]; row.allowScheduled = false; row.allowedForJoy = false;
            row.allowedForAddiction = false; row.takeToInventory = 0;
        }
        var beerRow = SingleRow(record.Policy, beer);
        beerRow.allowScheduled = true; beerRow.daysFrequency = 1; beerRow.onlyIfMoodBelow = 1; beerRow.onlyIfJoyBelow = 1;
        NativeDrugToleranceState.WriteEncoded(beerRow, "1:limit:0");
        NativeDrugToleranceState.WriteEncoded(SingleRow(record.Policy, yayo), yayoChoice);
        var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(PawnKindDefOf.Colonist, Faction.OfPlayer,
            forceGenerateNewPawn: true, canGeneratePawnRelations: false, allowFood: false, allowAddictions: false,
            fixedBiologicalAge: 25, fixedChronologicalAge: 25, forceNoIdeo: true, forceNoGear: true));
        record.Owned.Add(pawn); pawn.Name = new NameTriple("Tolerance", "Save " + label, marker);
        foreach (var hediff in pawn.health.hediffSet.hediffs.ToArray()) pawn.health.RemoveHediff(hediff);
        foreach (var trait in pawn.story.traits.allTraits.ToArray()) pawn.story.traits.RemoveTrait(trait);
        pawn.inventory.innerContainer.ClearAndDestroyContents(); pawn.workSettings.EnableAndInitialize();
        foreach (var work in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            if (!pawn.WorkTypeIsDisabled(work)) pawn.workSettings.SetPriority(work, 0);
        for (int hour = 0; hour < 24; hour++) pawn.timetable.SetAssignment(hour, TimeAssignmentDefOf.Anything);
        pawn.needs.food.CurLevelPercentage = pawn.needs.rest.CurLevelPercentage = 1;
        pawn.needs.mood.CurLevelPercentage = pawn.needs.joy.CurLevelPercentage = 1;
        pawn.drugs.CurrentPolicy = record.Policy;
        var dose = ThingMaker.MakeThing(beer); dose.stackCount = 1; record.Owned.Add(dose);
        EndToEndAssert.True(pawn.inventory.innerContainer.TryAdd(dose));
        var map = game.CurrentMap;
        var cell = CellRect.CenteredOn(map.Center, 20).OrderBy(value => value.DistanceToSquared(map.Center))
            .First(value => value.InBounds(map) && value.Standable(map) && !value.Fogged(map) && value.GetThingList(map).Count == 0);
        GenSpawn.Spawn(pawn, cell, map); pawn.drafter.Drafted = true;
        record.Subject = new Subject(pawn, dose);
        record.Snapshot = settings.Store.ExportSave(binding);
        AssertGlobals(); Retained(record.Subject);
        return record;
    }

    private void BindLoaded(SaveRecord target, SaveRecord departed)
    {
        var game = Current.Game ?? throw new InvalidOperationException("Loaded Game missing.");
        var oldGame = target.Game; var oldSubject = target.Subject; var oldPolicy = target.Policy; var oldBinding = target.Binding;
        var binding = BindingField.GetValue(settings) as SaveSettingsBinding ?? throw new InvalidOperationException("Loaded binding not published.");
        EndToEndAssert.True(target.Saved && target.Loads == 0 && !ReferenceEquals(game, departed.Game) && !ReferenceEquals(game, oldGame) &&
            game.PlayerHasControl && Current.ProgramState == ProgramState.Playing && !LongEventHandler.AnyEventNowOrWaiting && game.tickManager.Paused);
        EndToEndAssert.Equal(target.MapId, game.CurrentMap.uniqueID);
        EndToEndAssert.Equal(checked(target.SavedTick + 1), game.tickManager.TicksGame);
        EndToEndAssert.False(DebugSettings.fastEcology);
        EndToEndAssert.True(departed.Binding.IsRevoked && oldBinding.IsRevoked && !binding.IsRevoked &&
            !ReferenceEquals(binding, oldBinding) && !ReferenceEquals(binding, departed.Binding) && binding.Generation != oldBinding.Generation &&
            binding.Generation != departed.Binding.Generation && binding.SaveIdentity == target.Identity && ReferenceEquals(settings.LifecycleReceipt.Owner, game));
        EndToEndAssert.True(SameSettings(target.Snapshot, settings.Store.ExportSave(binding)) && Hash(target.Path) == target.Hash);
        var policy = game.drugPolicyDatabase.AllPolicies.Single(item => item.id == target.PolicyId && item.label == target.Name);
        EndToEndAssert.False(ReferenceEquals(policy, oldPolicy));
        EndToEndAssert.False(ReferenceEquals(SingleRow(policy, beer), SingleRow(oldPolicy, beer)));
        var pawn = game.CurrentMap.mapPawns.AllPawnsSpawned.Single(item => item.ThingID == oldSubject.PawnId && item.Name?.ToStringFull == oldSubject.FullName);
        var dose = pawn.inventory.innerContainer.Single(item => item.ThingID == oldSubject.SourceId && item.def == beer);
        EndToEndAssert.True(!ReferenceEquals(pawn, oldSubject.Pawn) && !ReferenceEquals(dose, oldSubject.Source) &&
            ReferenceEquals(PolicyField.GetValue(pawn.drugs), policy) && pawn.Drafted && pawn.Spawned && !pawn.Suspended &&
            pawn.def.tickerType == TickerType.Normal && dose.stackCount == 1);
        target.Game = game; target.Binding = binding; target.Policy = policy; target.Subject = new Subject(pawn, dose);
        target.Epoch++; target.Loads++; current = target; phase = "loaded " + target.Label;
        EndToEndAssert.False(Find.WindowStack.IsOpen<Dialog_SaveFileList_Load>() || Find.WindowStack.IsOpen<Dialog_ModSettings>() ||
            Find.WindowStack.IsOpen<Dialog_Options>() || Find.WindowStack.IsOpen<MainTabWindow_Menu>());
        RequireCurrent(); Retained(target.Subject);
    }

    private void RequireEntry()
    {
        EndToEndAssert.True(Current.ProgramState == ProgramState.Entry && Current.Game == null && !LongEventHandler.AnyEventNowOrWaiting);
        EndToEndAssert.True(current == null && a.Binding.IsRevoked && BindingField.GetValue(settings) == null &&
            settings.Store.CurrentSaveIdentity == null && settings.LifecycleReceipt.Owner == null);
        EndToEndAssert.True(((Dictionary<string, SaveSettingValue>)SaveValuesField.GetValue(settings.Store)!).Count == 0);
        EndToEndAssert.True(a.Saved && Hash(a.Path) == a.Hash && SameSettings(a.Snapshot, a.Component.Snapshot));
        AssertGlobals();
    }

    private void RequireCurrent()
    {
        var save = current ?? throw new InvalidOperationException("No fixture save is currently bound.");
        EndToEndAssert.True(Current.ProgramState == ProgramState.Playing && ReferenceEquals(Current.Game, save.Game) &&
            ReferenceEquals(Find.CurrentMap, save.Game.CurrentMap) && save.Game.CurrentMap.uniqueID == save.MapId && !LongEventHandler.AnyEventNowOrWaiting);
        EndToEndAssert.True(traceFailure == null, traceFailure ?? "Trace failure.");
        EndToEndAssert.True(ReferenceEquals(BindingField.GetValue(settings), save.Binding) && !save.Binding.IsRevoked &&
            save.Binding.SaveIdentity == save.Identity && ReferenceEquals(settings.LifecycleReceipt.Owner, save.Game) && settings.SaveLifecycleAvailable);
        EndToEndAssert.True(settings.Store.GetEffective(DrugToleranceSettings.Enabled) == save.Enabled &&
            !settings.Store.UsesGlobal(DrugToleranceSettings.Enabled) && settings.Store.GetGlobal(DrugToleranceSettings.Enabled) &&
            settings.Store.EffectiveOffReason(DrugToleranceSettings.Enabled) == null && persistence.CanUseChoices && runtime.Available == save.Enabled);
        AssertGlobals(); AssertRetainedAdmission();
        EndToEndAssert.True(SameSettings(save.Snapshot, settings.Store.ExportSave(save.Binding)));
        EndToEndAssert.True(save.Game.drugPolicyDatabase.AllPolicies.Contains(save.Policy) && save.Policy.id == save.PolicyId && save.Policy.label == save.Name);
        var row = SingleRow(save.Policy, beer);
        EndToEndAssert.True(NativeDrugToleranceState.ReadEncoded(row) == "1:limit:0" &&
            NativeDrugToleranceState.ReadEncoded(SingleRow(save.Policy, yayo)) == save.YayoChoice && row.allowScheduled &&
            !row.allowedForJoy && !row.allowedForAddiction && row.takeToInventory == 0 && row.daysFrequency == 1 &&
            row.onlyIfMoodBelow == 1 && row.onlyIfJoyBelow == 1);
        var subject = save.Subject;
        EndToEndAssert.True(subject.Pending.Count == 0 && subject.Pawn.Spawned && ReferenceEquals(subject.Pawn.Map, save.Game.CurrentMap) &&
            ReferenceEquals(PolicyField.GetValue(subject.Pawn.drugs), save.Policy) && !subject.Pawn.Dead && !subject.Pawn.Downed && !subject.Pawn.InMentalState);
        foreach (var target in traced)
        {
            var info = HarmonyOwner.GetPatchInfo(target);
            EndToEndAssert.True(info != null && info.Prefixes.Count(patch => patch.owner == traceOwner.Id) == 1 &&
                info.Postfixes.Count(patch => patch.owner == traceOwner.Id) == (target == Should ? 1 : 0));
            if (target == Should) EndToEndAssert.True(info!.Postfixes.Count == 1 && info.Finalizers.Count == 0);
            if (target == Allowed) EndToEndAssert.True(info!.Prefixes.Any(patch => patch.owner == "fumblesneeze.megamod") &&
                info.Finalizers.Any(patch => patch.owner == "fumblesneeze.megamod"));
        }
    }

    private void AssertRetainedAdmission()
    {
        // Ordinary off short-circuits Available. Inspect retained publications; never call Admitted/Validate or acquire observers.
        EndToEndAssert.True(Field(runtime.GetType(), "installed").GetValue(runtime) is true && Field(runtime.GetType(), "resolutionFailure").GetValue(runtime) == null);
        var geometry = Field(runtime.GetType(), "geometry").GetValue(runtime) as NativeDrugToleranceGeometry
            ?? throw new InvalidOperationException("Geometry admission missing.");
        EndToEndAssert.True(Field(geometry.GetType(), "resolutionFailure").GetValue(geometry) == null && geometry.Failure == null);
        var geometryRoutes = (List<DrugToleranceGeometryAdmission>)Field(geometry.GetType(), "admissions").GetValue(geometry)!;
        EndToEndAssert.Equal(8, geometryRoutes.Count);
        var ownedRoutes = geometryRoutes.Cast<object>().Concat(new[] { "allocation", "joy", "social", "header", "row" }
            .Select(name => Field(runtime.GetType(), name).GetValue(runtime) ?? throw new InvalidOperationException("Missing atomic member " + name))).ToArray();
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
            var generation = Field(type, "generation").GetValue(route); var receipt = Field(type, "receipt").GetValue(route);
            EndToEndAssert.True(Field(type, "installed").GetValue(route) is true && generation != null && receipt != null &&
                type.GetProperty("Failure")!.GetValue(route) == null);
            EndToEndAssert.True(ReferenceEquals(Property(receipt!, "Generation"), generation));
            var identity = Property(receipt!, "Identity");
            var isCurrent = identity.GetType().GetMethod("IsCurrent", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new MissingMethodException(identity.GetType().FullName, "IsCurrent");
            EndToEndAssert.True(Equals(Field(identity.GetType(), "target").GetValue(identity), target) &&
                isCurrent.ReturnType == typeof(bool) && isCurrent.GetParameters().Length == 0 && isCurrent.Invoke(identity, null) is true);
            if (route is DrugToleranceSocialPatch)
            {
                var nativeList = (FieldInfo)Field(type, "candidateList").GetValue(route)!;
                EndToEndAssert.True(ReferenceEquals(Field(type, "workingList").GetValue(route), nativeList.GetValue(null)));
            }
            observed.Add((ReferenceEquals(route, sharedRoute) ? "shared:" : "S138-owned:") + target.DeclaringType!.FullName + "." + target.Name + ":generation=" + Property(generation!, "Version"));
        }
        retainedAdmission = string.Join(";", observed);
    }

    private void Install(MethodInfo target, string prefix, string? postfix = null)
    {
        traced.Add(target);
        HarmonyMethod Callback(string name) => new(typeof(NativeToleranceSaveSwitchTests).GetMethod(name, BindingFlags.Static | BindingFlags.NonPublic)!);
        traceOwner.Patch(target, prefix: Callback(prefix), postfix: postfix == null ? null : Callback(postfix));
    }
    private Subject? TracedSubject(Pawn pawn) => current != null && ReferenceEquals(Current.Game, current.Game) &&
        !LongEventHandler.AnyEventNowOrWaiting && ReferenceEquals(current.Subject.Pawn, pawn) ? current.Subject : null;
    private static void BeginDecision(Pawn_DrugPolicyTracker __instance, ThingDef __0)
    {
        var fixture = active; var subject = fixture?.TracedSubject(__instance.pawn);
        if (fixture == null || subject == null || __0 != fixture.beer || fixture.traceFailure != null) return;
        if (subject.Decisions.Count >= 1024) { fixture.traceFailure = "Decision trace exceeded its bounded inventory."; return; }
        var policy = PolicyField.GetValue(__instance) as DrugPolicy;
        if (policy == null) { fixture.traceFailure = "Scheduled subject has no stored policy."; return; }
        var rows = Enumerable.Range(0, policy.Count).Select(index => policy[index]).Where(row => row?.drug == __0).ToArray();
        if (rows.Length != 1) { fixture.traceFailure = "Scheduled subject has no unique current Beer row."; return; }
        var record = fixture.current!;
        var decision = new Decision { Tick = record.Game.tickManager.TicksGame, Epoch = record.Epoch, SaveIdentity = record.Identity,
            Choice = NativeDrugToleranceState.ReadEncoded(rows[0]) ?? "absent", Effective = fixture.settings.Store.GetEffective(DrugToleranceSettings.Enabled),
            Before = fixture.Severity(subject, fixture.tolerance), History = __instance.HasEverTaken(__0) };
        subject.Decisions.Add(decision); subject.Pending.Push(decision);
    }
    private static void ObserveAllowed(Pawn_DrugPolicyTracker __instance, ThingDef __0)
    {
        var fixture = active; var subject = fixture?.TracedSubject(__instance.pawn);
        if (fixture == null || subject == null || __0 != fixture.beer || fixture.traceFailure != null) return;
        if (subject.Pending.Count == 0) { fixture.traceFailure = "Unpaired native Allowed call."; return; }
        subject.Pending.Peek().AllowedCalls++;
    }
    private static void EndDecision(Pawn_DrugPolicyTracker __instance, ThingDef __0, bool __result, bool __runOriginal)
    {
        var fixture = active; var subject = fixture?.TracedSubject(__instance.pawn);
        if (fixture == null || subject == null || __0 != fixture.beer || fixture.traceFailure != null) return;
        if (subject.Pending.Count == 0) { fixture.traceFailure = "Unpaired native Should result."; return; }
        var decision = subject.Pending.Pop(); decision.Completed = true; decision.Result = __result; decision.RanOriginal = __runOriginal;
        decision.After = fixture.Severity(subject, fixture.tolerance); decision.ExitTick = fixture.current!.Game.tickManager.TicksGame;
    }
    private static void ObserveIngestion(Thing __instance, Pawn __0)
    {
        var fixture = active; var subject = fixture?.TracedSubject(__0);
        if (fixture == null || subject == null || !ReferenceEquals(__instance, subject.Source)) return;
        subject.Ingestions++; subject.IngestedTick = fixture.current!.Game.tickManager.TicksGame;
        subject.IngestedEpoch = fixture.current.Epoch; subject.IngestedJobId = __0.CurJob?.loadID ?? -1;
        subject.IngestedSameJob = ReferenceEquals(__0.CurJob, subject.Job);
        subject.IngestedNative = __0.CurJob?.def == JobDefOf.Ingest && !__0.CurJob.playerForced &&
            __0.CurJob.jobGiver?.GetType() == typeof(JobGiver_TakeDrugsForDrugPolicy) && ReferenceEquals(__0.CurJob.targetA.Thing, __instance);
    }

    private IEnumerable<EndToEndStep> Undraft(IEndToEndContext context, Subject subject)
    {
        yield return new SelectionActionStep("select the current save's subject", new[] { subject.PawnId }, additive: false);
        EndToEndAssert.True(subject.Pawn.Drafted);
        var toggle = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { subject.PawnId }, Array.Empty<string>())
            .Single(item => !item.Disabled && item.Interaction == EndToEndGizmoInteraction.Toggle && item.ToggleState == true && item.HotKeyDefName == "Command_ColonistDraft");
        yield return new GizmoActionStep("native Undraft " + subject.Pawn.Name.ToStringShort, new[] { subject.PawnId }, toggle.RuntimeType,
            EndToEndGizmoInteraction.Toggle, stableGizmoId: toggle.StableId);
    }
    private void Retained(Subject subject) => EndToEndAssert.True(!subject.Source.Destroyed && subject.Source.stackCount == 1 &&
        subject.Source.ThingID == subject.SourceId && ReferenceEquals(subject.Source.holdingOwner, subject.Pawn.inventory.innerContainer) &&
        !subject.Pawn.drugs.HasEverTaken(beer) && subject.Ingestions == 0 && Severity(subject, alcohol) == 0 && Severity(subject, tolerance) == 0);
    private void AssertDecisions(SaveRecord record, bool result) => EndToEndAssert.True(record.Subject.Decisions.Count > 0 &&
        record.Subject.Decisions.All(item => item.Completed && item.RanOriginal && item.Result == result && item.AllowedCalls == 1 && !item.History &&
            item.Before == 0 && item.After == 0 && item.Tick == item.ExitTick && item.Choice == "1:limit:0" && item.Effective == record.Enabled &&
            item.Epoch == record.Epoch && item.SaveIdentity == record.Identity), "Actual final native decisions must belong to this loaded save and its unchanged choice.");
    private static void BindJob(Subject subject)
    {
        var job = subject.Pawn.CurJob;
        EndToEndAssert.True(job?.def == JobDefOf.Ingest && !job.playerForced && job.jobGiver?.GetType() == typeof(JobGiver_TakeDrugsForDrugPolicy) &&
            ReferenceEquals(job.targetA.Thing, subject.Source) && (subject.Job == null || ReferenceEquals(subject.Job, job)));
        subject.Job = job; subject.JobId = job!.loadID;
    }
    private static bool IsChewing(Subject subject)
    {
        if (subject.Pawn.jobs.curDriver is not JobDriver_Ingest driver || !ReferenceEquals(driver.job, subject.Job)) return false;
        var chewing = ChewingField.GetValue(driver) as Toil; var toils = ToilsField.GetValue(driver) as List<Toil>; int index = driver.CurToilIndex;
        return chewing != null && toils != null && index >= 0 && index < toils.Count && ReferenceEquals(toils[index], chewing) &&
            driver.ticksLeftThisToil > 0 && ReferenceEquals(subject.Pawn.carryTracker.CarriedThing, subject.Source);
    }
    private float Severity(Subject subject, HediffDef def) => subject.Pawn.health.hediffSet.GetFirstHediffOfDef(def)?.Severity ?? 0;

    private Rect MenuButton(string key)
    {
        EndToEndAssert.True(actualWindow.GetType() == typeof(MainTabWindow_Menu) && Current.ProgramState == ProgramState.Playing &&
            !Current.Game.Info.permadeathMode && !GameDataSaveLoader.SavingIsTemporarilyDisabled);
        var labels = new List<string> { "Save" };
        if (Read<bool>(typeof(MainTabWindow_Menu), actualWindow, "anyGameFiles")) labels.Add("LoadGame");
        labels.AddRange(new[] { "ReviewScenario", "Options", "QuitToMainMenu", "QuitToOS" });
        int index = labels.IndexOf(key); EndToEndAssert.True(index >= 0);
        var font = Text.Font;
        try
        {
            Text.Font = GameFont.Small;
            float y = labels.Take(index).Sum(label => Math.Max(45, Text.CalcHeight(label.Translate(), 170)) + 7);
            var button = new Rect(0, y, 170, Math.Max(45, Text.CalcHeight(key.Translate(), 170)));
            EndToEndAssert.True(button.xMax <= window.Width - 36 && button.yMax <= window.Height - 36);
            return button;
        }
        finally { Text.Font = font; }
    }

    private Vector2 LoadButton(SaveRecord save)
    {
        EndToEndAssert.True(actualWindow.GetType() == typeof(Dialog_SaveFileList_Load) && save.Saved && Hash(save.Path) == save.Hash);
        var files = Read<List<SaveFileInfo>>(typeof(Dialog_FileList), actualWindow, "files");
        var scroll = Read<Vector2>(typeof(Dialog_FileList), actualWindow, "scrollPosition");
        var search = Read<QuickSearchWidget>(typeof(Dialog_FileList), actualWindow, "search");
        EndToEndAssert.True(files.Count <= 32 && string.IsNullOrEmpty(search.filter.Text) && scroll == Vector2.zero,
            "This disposable two-save fixture requires an unfiltered visible native file inventory.");
        var matches = files.Select((file, index) => (file, index)).Where(item =>
            string.Equals(Path.GetFullPath(item.file.FileInfo.FullName), save.Path, StringComparison.OrdinalIgnoreCase)).ToArray();
        EndToEndAssert.Equal(1, matches.Length);
        float width = (float)window.Width - 36;
        float height = (float)window.Height - 36;
        float y = 34 + matches[0].index * 40 + 20;
        var button = new Rect(width - 16 - 36 - 100, y - 18, 100, 36);
        EndToEndAssert.True(button.yMin >= 34 && button.yMax <= height - Window.CloseButSize.y - 10 && button.xMin > 0);
        return button.center;
    }

    private Vector2 ModButton(Dialog_Options options)
    {
        EndToEndAssert.True(ReferenceEquals(actualWindow, options) && options.selectedCategory == OptionCategoryDefOf.Mods && options.selectedMod == null);
        var cached = Read<IEnumerable<Mod>>(typeof(Dialog_Options), options, "cachedModsWithSettings").Take(65).ToArray();
        EndToEndAssert.True(cached.Length > 0 && cached.Length <= 64 && string.IsNullOrEmpty(Read<string>(typeof(Dialog_Options), options, "modFilter")));
        int index = Array.FindIndex(cached, item => ReferenceEquals(item, mod));
        EndToEndAssert.True(index >= 0 && cached.Count(item => ReferenceEquals(item, mod)) == 1);
        float height = Read<float>(typeof(Dialog_Options), options, "optionsViewRectHeight");
        var scroll = Read<Vector2>(typeof(Dialog_Options), options, "optionsScrollPosition");
        EndToEndAssert.True(float.IsFinite(height) && height > 0 && scroll == Vector2.zero);
        float width = (float)window.Width - 36 - 177 - (height > window.Height - 36 - 60 ? 26 : 0);
        var row = new Rect(177, 12 + 30 + 12 + (index / 2) * 40, width, 40);
        var button = (index % 2 == 0 ? row.LeftHalf() : row.RightHalf()).ContractedBy(2);
        EndToEndAssert.True(button.xMin >= 177 && button.xMax <= window.Width - 36 && button.yMax <= window.Height - 36 - 60);
        return button.center;
    }

    private NativeWindowCaptureStep Capture<T>(string name, Action<T> receive) where T : Window => new(name, captures =>
    {
        var actuals = Find.WindowStack.Windows.OfType<T>().Where(item => item.GetType() == typeof(T)).ToArray();
        EndToEndAssert.Equal(1, actuals.Length, "Require exact native window " + typeof(T).FullName);
        actualWindow = actuals[0]; ownedWindows.Add(actualWindow);
        window = captures.Single(item => item.NativeId == actualWindow.ID && item.RuntimeType == actualWindow.GetType().FullName);
        receive((T)actualWindow);
    });
    private NativeWindowCaptureStep CaptureForInput<T>(string name, Action<T> receive) where T : Window => Capture<T>(name, actual =>
    {
        // The inspected interactive content origins require an untitled native window. Merely
        // observing the titled developer console needs identity/ownership, not these coordinates.
        EndToEndAssert.True(string.IsNullOrEmpty(actual.optionalTitle) && window.Width > 36 && window.Height > 36 &&
            window.UiScale > 0 && Math.Abs(window.UiScale - Prefs.UIScale) < .001);
        receive(actual);
    });
    private NativeEntryMenuCaptureStep EntryCapture(string name, string control) => new(name, controls =>
    {
        RequireEntry(); var match = controls.Single(item => item.Control == control);
        entryHandle = match.Handle;
    });
    private NativePointerScopeBeginStep BeginPointer(string name) => new(name, handle =>
    { EndToEndAssert.True(pointer == null); pointer = handle; });
    private NativePointerScopeEndStep EndPointer(string name)
    {
        string owned = pointer ?? throw new InvalidOperationException("No pointer scope to end.");
        pointer = null;
        return new(name, owned);
    }
    private NativeWindowClickActionStep ClickContent(string name, Vector2 point) => ClickOuter(name, point + new Vector2(18, 18));
    private NativeWindowClickActionStep ClickOuter(string name, Vector2 point)
    {
        EndToEndAssert.True(float.IsFinite(point.x) && float.IsFinite(point.y) && point.x >= 0 && point.x < window.Width && point.y >= 0 && point.y < window.Height);
        return new(name, window.Handle, (window.X + point.x) * window.UiScale, (window.Y + point.y) * window.UiScale, null, pointer);
    }
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private static PawnInspectTabActionStep Gear(Subject subject) => new("show physical Beer in Gear", subject.PawnId, EndToEndPawnInspectTab.Gear);
    private static TimeControlActionStep Pause(string name) => new(name, true, EndToEndGameSpeed.Normal);
    private static TimeControlActionStep Run(string name, EndToEndGameSpeed speed = EndToEndGameSpeed.Normal) => new(name, false, speed);
    private static WaitUntilStep Wait(string name, Func<IEndToEndContext, bool> predicate, int ticks, int seconds) => new(name, predicate,
        new EndToEndDeadline(int.MaxValue, ticks == 0 ? int.MaxValue : ticks, TimeSpan.FromSeconds(seconds)));

    private IReadOnlyDictionary<string, string> LifecycleEvidence()
    {
        var game = Current.Game; var binding = BindingField.GetValue(settings) as SaveSettingsBinding;
        var result = new Dictionary<string, string>
        {
            ["phase"] = phase, ["programState"] = Current.ProgramState.ToString(), ["longEvent"] = LongEventHandler.AnyEventNowOrWaiting.ToString(),
            ["playerControl"] = game?.PlayerHasControl.ToString() ?? "no Game", ["map"] = game?.CurrentMap?.uniqueID.ToString() ?? "none",
            ["tick"] = game?.tickManager?.TicksGame.ToString() ?? "none", ["paused"] = game?.tickManager?.Paused.ToString() ?? "none",
            ["binding"] = binding == null ? "none" : $"{binding.SaveIdentity};generation={binding.Generation};revoked={binding.IsRevoked}",
            ["lifecycleOwnerIsCurrent"] = ReferenceEquals(settings.LifecycleReceipt.Owner, game).ToString(),
            ["windows"] = string.Join(";", Find.WindowStack.Windows.Select(item => item.GetType().FullName)),
            ["globalOn"] = settings.Store.GetGlobal(DrugToleranceSettings.Enabled).ToString(),
            ["currentSaveIdentity"] = settings.Store.CurrentSaveIdentity ?? "none"
        };
        foreach (var save in saves)
        {
            result[save.Label + " old binding revoked"] = save.Binding.IsRevoked.ToString();
            result[save.Label + " expected identity"] = save.Identity;
            var policies = game?.drugPolicyDatabase?.AllPolicies.Where(item => item.id == save.PolicyId && item.label == save.Name).ToArray() ?? Array.Empty<DrugPolicy>();
            result[save.Label + " current policy matches"] = policies.Length.ToString();
            if (policies.Length == 1)
                result[save.Label + " current rows"] = "Beer=" + NativeDrugToleranceState.ReadEncoded(SingleRow(policies[0], beer)) +
                    ";Yayo=" + NativeDrugToleranceState.ReadEncoded(SingleRow(policies[0], yayo));
            var subject = save.Subject;
            if (subject == null) continue;
            var pawns = game?.CurrentMap?.mapPawns.AllPawnsSpawned.Where(item => item.ThingID == subject.PawnId && item.Name?.ToStringFull == subject.FullName).ToArray() ?? Array.Empty<Pawn>();
            result[save.Label + " current pawn matches"] = pawns.Length.ToString();
            if (pawns.Length == 1)
            {
                var pawn = pawns[0];
                result[save.Label + " current pawn"] = $"{pawn.ThingID};drafted={pawn.Drafted};storedPolicy={(PolicyField.GetValue(pawn.drugs) as DrugPolicy)?.id};job={pawn.CurJob?.def?.defName}";
                result[save.Label + " current dose"] = string.Join(";", pawn.inventory.innerContainer.Concat(pawn.carryTracker.innerContainer)
                    .Where(item => item.ThingID == subject.SourceId).Select(item => $"{item.ThingID};count={item.stackCount};destroyed={item.Destroyed}"));
            }
        }
        return result;
    }
    private IReadOnlyDictionary<string, string> Evidence()
    {
        var result = new Dictionary<string, string>(LifecycleEvidence())
        {
            ["nativeOperations"] = nativeOperations.ToString(), ["screenshots"] = screenshots.ToString(), ["waitMaxSeconds"] = waitSeconds.ToString(),
            ["scrollClicks"] = scrollClicks.ToString(), ["retainedAdmission"] = retainedAdmission, ["traceFailure"] = traceFailure ?? "none",
            ["scope"] = "Main menu disabled save controls and two native save identities; row sentinels are supporting persistence evidence; no complete S138 claim."
        };
        foreach (var save in saves)
        {
            result[save.Label + " save"] = $"identity={save.Identity};path={save.Path};hash={save.Hash};savedTick={save.SavedTick};enabled={save.Enabled};loads={save.Loads};epoch={save.Epoch}";
            result[save.Label + " stored settings"] = string.Join(";", save.Snapshot.Values.OrderBy(pair => pair.Key).Select(pair => $"{pair.Key}:inherit={pair.Value.UseGlobal}:override={pair.Value.Override}"));
            var subject = save.Subject;
            if (subject == null) continue;
            result[save.Label + " subject"] = $"pawn={subject.PawnId};name={subject.FullName};dose={subject.SourceId};job={subject.JobId};chewTick={subject.ChewingTick};ingestions={subject.Ingestions};ingestJob={subject.IngestedJobId};ingestTick={subject.IngestedTick};ingestEpoch={subject.IngestedEpoch};sameJob={subject.IngestedSameJob};native={subject.IngestedNative}";
            result[save.Label + " decisions"] = string.Join(";", subject.Decisions.Select(item => item.ToString()));
        }
        return result;
    }

    private void CleanupCurrentGame()
    {
        var errors = new List<Exception>();
        foreach (var owned in ownedWindows.Distinct()) Attempt(errors, () =>
        { if (Find.WindowStack.Windows.Contains(owned)) owned.Close(false); });
        // No previous Game is made current, loaded, or written during cleanup.
        if (Current.Game == null)
        {
            Attempt(errors, () => EndToEndAssert.True(Current.ProgramState == ProgramState.Entry));
        }
        else
        {
            SaveRecord? record = null;
            Attempt(errors, () =>
            {
                var game = Current.Game;
                EndToEndAssert.True(game.PlayerHasControl && Current.ProgramState == ProgramState.Playing && !LongEventHandler.AnyEventNowOrWaiting);
                var binding = BindingField.GetValue(settings) as SaveSettingsBinding;
                EndToEndAssert.True(binding != null && !binding.IsRevoked && ReferenceEquals(settings.LifecycleReceipt.Owner, game));
                var candidate = saves.Single(item => item.Identity == binding!.SaveIdentity && item.MapId == game.CurrentMap.uniqueID);
                EndToEndAssert.True(ReferenceEquals(candidate.Game, game) || game.drugPolicyDatabase.AllPolicies.Any(item => item.id == candidate.PolicyId && item.label == candidate.Name));
                record = candidate; // Publish cleanup authority only after every ownership assertion passed.
            });
            if (record != null)
            {
                var owned = record; var game = Current.Game;
                Attempt(errors, () => Find.Selector.ClearSelection());
                Attempt(errors, () =>
                {
                    var binding = BindingField.GetValue(settings) as SaveSettingsBinding;
                    EndToEndAssert.True(binding != null && binding.SaveIdentity == owned.Identity && !binding.IsRevoked);
                    var values = (Dictionary<string, SaveSettingValue>)SaveValuesField.GetValue(settings.Store)!;
                    if (owned.Initial.Values.TryGetValue(DrugToleranceSettings.Enabled.Id, out var original)) values[DrugToleranceSettings.Enabled.Id] = original;
                    else values.Remove(DrugToleranceSettings.Enabled.Id);
                    settings.Store.LoadGlobals(globals); // Cleanup publication through the existing Changed subscriber only.
                    EndToEndAssert.True(SameSettings(owned.Initial, settings.Store.ExportSave(binding!)) &&
                        SameSettings(owned.Initial, game.GetComponent<MegamodSaveSettings>().Snapshot));
                });
                Pawn? pawn = null;
                if (owned.Subject != null)
                {
                    Attempt(errors, () => pawn = game.CurrentMap.mapPawns.AllPawnsSpawned.SingleOrDefault(item => item.ThingID == owned.Subject.PawnId && item.Name?.ToStringFull == owned.Subject.FullName));
                    Thing[] candidates = Array.Empty<Thing>();
                    Attempt(errors, () => candidates = game.CurrentMap.listerThings.AllThings.Where(item => item.ThingID == owned.Subject.SourceId && item.def == beer)
                        .Concat(pawn == null ? Enumerable.Empty<Thing>() : pawn.inventory.innerContainer.Concat(pawn.carryTracker.innerContainer).Where(item => item.ThingID == owned.Subject.SourceId)).Distinct().ToArray());
                    foreach (var dose in candidates) Attempt(errors, () => { if (!dose.Destroyed) dose.Destroy(DestroyMode.Vanish); });
                    Attempt(errors, () => { if (pawn != null && !pawn.Destroyed) pawn.Destroy(DestroyMode.Vanish); });
                }
                // Also covers partial Arrange failure before the Subject record exists, only in that exact original Game.
                if (ReferenceEquals(owned.Origin, game))
                    foreach (var thing in owned.Owned.AsEnumerable().Reverse()) Attempt(errors, () => { if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish); });
                Attempt(errors, () => game.drugPolicyDatabase.AllPolicies.RemoveAll(item => item.id == owned.PolicyId && item.label == owned.Name));
                Attempt(errors, () =>
                {
                    Find.CameraDriver.SetRootPosAndSize(new Vector3(owned.CameraCell.x + .5f, 0, owned.CameraCell.z + .5f), owned.CameraSize);
                    game.tickManager.CurTimeSpeed = owned.Speed;
                });
            }
        }
        if (errors.Count != 0) throw new AggregateException("Native two-save fixture cleanup failed.", errors);
    }
    private void CleanupGlobals()
    {
        var errors = new List<Exception>();
        Attempt(errors, () =>
        {
            // Cleanup only. LoadGlobals raises Changed and could capture an unproved/departed save.
            // Restore the process-owned global store, then use the existing globals-only publication path.
            GlobalValuesField.SetValue(settings.Store, new Dictionary<string, string>(globals.Values, StringComparer.Ordinal));
            GlobalRevisionField.SetValue(settings.Store, checked((long)GlobalRevisionField.GetValue(settings.Store)! + 1));
        });
        Attempt(errors, settings.CaptureGlobals);
        Attempt(errors, AssertGlobals);
        Attempt(errors, () => EndToEndAssert.True(settings.PublishedGlobals.GlobalRevision == settings.Store.Revision.Global &&
            globals.Values.OrderBy(pair => pair.Key).SequenceEqual(settings.PublishedGlobals.Values.Values.OrderBy(pair => pair.Key))));
        if (errors.Count != 0) throw new AggregateException("Process-owned global settings cleanup failed.", errors);
    }
    private void CleanupFiles()
    {
        var errors = new List<Exception>();
        foreach (var save in saves)
            foreach (var path in new[] { save.Path, save.Path + ".new" }) Attempt(errors, () =>
            { RequireOwnedPath(save); if (save.Requested && File.Exists(path)) File.Delete(path); });
        if (errors.Count != 0) throw new AggregateException("Owned save file cleanup failed.", errors);
    }
    private void CleanupTraces()
    {
        if (ReferenceEquals(active, this)) active = null;
        var errors = new List<Exception>();
        foreach (var target in traced) Attempt(errors, () => traceOwner.Unpatch(target, HarmonyPatchType.All, traceOwner.Id));
        foreach (var target in traced) Attempt(errors, () => EndToEndAssert.False(HarmonyOwner.GetPatchInfo(target)?.Owners.Contains(traceOwner.Id) == true));
        if (errors.Count != 0) throw new AggregateException("Save-switch trace cleanup failed.", errors);
    }
    private void RequireOwnedPath(SaveRecord save)
    {
        var root = Path.GetFullPath(GenFilePaths.SavedGamesFolderPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar) + Path.DirectorySeparatorChar;
        EndToEndAssert.True(save.Name == marker + "-" + save.Label && (save.Label == "A" || save.Label == "B") &&
            save.Path.StartsWith(root, StringComparison.OrdinalIgnoreCase) && Path.GetFileNameWithoutExtension(save.Path) == save.Name &&
            string.Equals(save.Path, Path.GetFullPath(GenFilePaths.FilePathForSavedGame(save.Name)), StringComparison.OrdinalIgnoreCase));
    }
    private void AssertGlobals() => EndToEndAssert.True(globals.Values.OrderBy(pair => pair.Key)
        .SequenceEqual(settings.Store.ExportGlobals().Values.OrderBy(pair => pair.Key)), "Global encoded values must remain unchanged.");
    private static bool SameSettings(SaveSettingsSnapshot left, SaveSettingsSnapshot right) => left.SaveIdentity == right.SaveIdentity &&
        left.Values.OrderBy(pair => pair.Key).SequenceEqual(right.Values.OrderBy(pair => pair.Key));
    private static DrugPolicyEntry SingleRow(DrugPolicy policy, ThingDef drug) => Enumerable.Range(0, policy.Count).Select(index => policy[index]).Single(row => row?.drug == drug);
    private static FieldInfo Field(Type type, string name) => type.GetField(name, InstanceFields) ?? throw new MissingFieldException(type.FullName, name);
    private static T Read<T>(Type type, object value, string name)
    {
        var field = Field(type, name); EndToEndAssert.Equal(typeof(T), field.FieldType); return (T)field.GetValue(value)!;
    }
    private static object Property(object value, string name) => value.GetType().GetProperty(name, InstanceFields)?.GetValue(value)
        ?? throw new MissingMemberException(value.GetType().FullName, name);
    private static string Hash(string path)
    { using var stream = File.OpenRead(path); using var algorithm = SHA256.Create(); return BitConverter.ToString(algorithm.ComputeHash(stream)).Replace("-", ""); }
    private static void Attempt(List<Exception> errors, Action action) { try { action(); } catch (Exception error) { errors.Add(error); } }

    private sealed class SaveRecord
    {
        internal readonly string Label, Name, Path, Identity, YayoChoice;
        internal readonly bool Enabled;
        internal readonly Game Origin;
        internal readonly MegamodSaveSettings Component;
        internal readonly SaveSettingsSnapshot Initial;
        internal readonly int MapId;
        internal readonly IntVec3 CameraCell;
        internal readonly float CameraSize;
        internal readonly TimeSpeed Speed;
        internal readonly List<Thing> Owned = new();
        internal Game Game;
        internal SaveSettingsBinding Binding;
        internal SaveSettingsSnapshot Snapshot;
        internal DrugPolicy Policy = null!;
        internal Subject Subject = null!;
        internal int PolicyId = -1, SavedTick = -1, Loads, Epoch;
        internal string Hash = "not saved";
        internal bool Requested, Saved;
        internal SaveRecord(string label, string name, Game game, SaveSettingsBinding binding, SaveSettingsSnapshot initial, bool enabled, string yayoChoice)
        {
            Label = label; Name = name; Game = Origin = game; Binding = binding; Initial = Snapshot = initial;
            Component = game.GetComponent<MegamodSaveSettings>();
            Identity = binding.SaveIdentity; Enabled = enabled; YayoChoice = yayoChoice; MapId = game.CurrentMap.uniqueID;
            Path = System.IO.Path.GetFullPath(GenFilePaths.FilePathForSavedGame(name));
            CameraCell = Find.CameraDriver.MapPosition; CameraSize = Find.CameraDriver.RootSize; Speed = game.tickManager.CurTimeSpeed;
        }
    }
    private sealed class Subject
    {
        internal readonly Pawn Pawn;
        internal readonly Thing Source;
        internal readonly string PawnId, SourceId, FullName;
        internal readonly List<Decision> Decisions = new();
        internal readonly Stack<Decision> Pending = new();
        internal Job? Job;
        internal int JobId = -1, ChewingTick = -1, Ingestions, IngestedTick = -1, IngestedJobId = -1, IngestedEpoch = -1;
        internal bool IngestedSameJob, IngestedNative;
        internal Subject(Pawn pawn, Thing source)
        { Pawn = pawn; Source = source; PawnId = pawn.ThingID; SourceId = source.ThingID; FullName = pawn.Name.ToStringFull; }
    }
    private sealed class Decision
    {
        internal string SaveIdentity = null!, Choice = null!;
        internal int Tick, ExitTick, Epoch, AllowedCalls;
        internal float Before, After;
        internal bool Effective, History, Completed, Result, RanOriginal;
        public override string ToString() => $"save={SaveIdentity};epoch={Epoch};tick={Tick}/{ExitTick};choice={Choice};effective={Effective};raw={Before:R}/{After:R};allowed={AllowedCalls};history={History};complete={Completed};original={RanOriginal};result={Result}";
    }
}
