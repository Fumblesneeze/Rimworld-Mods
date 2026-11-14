using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LudeonTK;
using Megamod.Features.DrugTolerance.Runtime;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.PracticalNeeds;

[RimWorldEndToEndTest("megamod.practical-needs.native-scheduled-mood-independent-availability", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 6000, MaxWallClockSeconds = 2700)]
public sealed class NativeScheduledMoodAvailabilityTests : IRimWorldEndToEndTest
{
    private readonly ScheduledMoodFixture fixture = new(MoodScenario.Availability);
    public void Arrange(IEndToEndContext context) => fixture.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => fixture.Execute(context);
}

internal sealed partial class ScheduledMoodFixture
{
    private HashSet<Window>? windowsBeforeScale;
    private readonly List<Message> notices = new();
    private List<Message> liveMessages = null!;
    private bool displayTransition;

    private void ArrangeDisplay()
    {
        EndToEndAssert.True(Screen.width == 1600 && Screen.height == 900 && Prefs.UIScalePreference == 1 &&
            Screen.fullScreenMode == FullScreenMode.Windowed && Math.Abs(ResolutionUtility.DpiScale - 1) < .001f);
        EndToEndAssert.True(!Find.WindowStack.Windows.Any(w => w is Dialog_Options or Dialog_ResolutionConfirm or FloatMenu));
        var scales = Read<float[]>(typeof(Dialog_Options), null, "UIScales");
        EndToEndAssert.True(scales.Count(s => s == 1) == 1 && scales.Count(s => s == 1.25f) == 1 && ResolutionUtility.UIScaleSafeWithResolution(1.25f, 1280, 1024));
        liveMessages = Read<List<Message>>(typeof(Messages), null, "liveMessages");
        EndToEndAssert.Equal(0, liveMessages.Count);
        int width = Screen.width, height = Screen.height, preferenceWidth = Prefs.ScreenWidth, preferenceHeight = Prefs.ScreenHeight;
        float scale = Prefs.UIScalePreference; var mode = Screen.fullScreenMode; var preferenceMode = Prefs.FullScreenMode;
        bool restoreRequested = false;
        context.DeferCleanupUntil(() =>
        {
            if (!restoreRequested)
            {
                EndToEndAssert.False(Find.WindowStack.IsOpen<Dialog_ResolutionConfirm>());
                Prefs.UIScalePreference = scale; GenUI.ClearLabelWidthCache();
                ResolutionUtility.SetResolutionRaw(width, height, mode);
                Prefs.ScreenWidth = preferenceWidth; Prefs.ScreenHeight = preferenceHeight; Prefs.FullScreenMode = preferenceMode;
                restoreRequested = true; return false;
            }
            return Screen.width == width && Screen.height == height && Screen.fullScreenMode == mode && Prefs.UIScalePreference == scale &&
                Prefs.ScreenWidth == preferenceWidth && Prefs.ScreenHeight == preferenceHeight && Prefs.FullScreenMode == preferenceMode;
        });
        context.DeferCleanup(() =>
        {
            RequireGame(); var errors = new List<Exception>();
            Attempt(errors, ClaimPendingWindows);
            Attempt(errors, () =>
            {
                if (windowsBeforeScale == null) return;
                var pending = Find.WindowStack.Windows.OfType<Dialog_ResolutionConfirm>().Where(w => !windowsBeforeScale.Contains(w)).ToArray();
                EndToEndAssert.True(pending.Length <= 1);
                if (pending.Length == 1) windows.Add(pending[0]);
            });
            foreach (var owned in windows.Reverse().ToArray()) Attempt(errors, () =>
            {
                if (!Find.WindowStack.Windows.Contains(owned)) return;
                if (owned is MainTabWindow_Assign && ReferenceEquals(Find.MainTabsRoot.OpenTab?.TabWindow, owned)) Find.MainTabsRoot.EscapeCurrentTab(false);
                else owned.Close(false);
            });
            Attempt(errors, () => EndToEndAssert.False(Find.WindowStack.IsOpen<Dialog_ResolutionConfirm>()));
            foreach (var notice in notices) Attempt(errors, () => liveMessages.Remove(notice));
            if (errors.Count != 0) throw new AggregateException("Native display control cleanup failed.", errors);
        });
    }

    private IEnumerable<EndToEndStep> AvailabilityWorkflow()
    {
        yield return new CheckpointStep("arrange explicit disposable display precondition", _ =>
            new Dictionary<string, string> { ["width"] = "1280", ["height"] = "1024", ["scale"] = "1", ["kind"] = "fixture setup, not native acceptance action" });
        ResolutionUtility.SetResolutionRaw(1280, 1024, FullScreenMode.Windowed);
        yield return Wait("observe the disposable display request on a later native frame", () => Screen.width == 1280 && Screen.height == 1024, 5);
        foreach (var step in Policies()) yield return step;
        yield return new CameraActionStep("frame recovering, bypass and fresh-zero subjects", pawns.Select(p => p.ThingID), paddingPixels: 220);
        foreach (var step in Inspect(A, EndToEndPawnInspectTab.Needs)) yield return step;
        yield return Shot("A actual recovering-mood needs before availability change");
        foreach (var s in subjects)
        {
            foreach (var step in Inspect(s, EndToEndPawnInspectTab.Gear)) yield return step;
            yield return Shot(s.Pawn.LabelShort + " original two Beer before native time");
        }
        BeginPhase("available both on");
        foreach (var s in new[] { A, B }) foreach (var step in Undraft(s)) yield return step;
        yield return Run("ordinary native selections before layout failure");
        yield return Wait("establish two native-eligible mood and tolerance rejections", () =>
        { RequireCurrent(); Held(A, 2); Held(B, 2); Held(subjects[2], 2); return Rejections(A, true, true) >= 2 && Rejections(B, true, true) >= 2; }, 45, 600);
        yield return Pause("pause original independently rejected controls");
        foreach (var step in ChangeScale(1.25f)) yield return step;
        foreach (var step in AvailabilityManager(false)) yield return step;
        BeginPhase("tolerance unavailable");
        foreach (var step in ConsumeMap(B, false)) yield return step;
        yield return new AssertionStep("S024 remains enforced through actual scheduled selections despite S138 atomic unavailability", _ =>
        {
            RequireCurrent(); Held(A, 2); Held(subjects[2], 2);
            EndToEndAssert.True(!tolerance.Available && scheduled.Available && scheduled.PotentialMood.Available && Rejections(A, true, true) >= 2);
            EndToEndAssert.True(subjects[2].Pawn.Drafted && History(subjects[2]).Count == 0);
        });
        foreach (var step in Inspect(A, EndToEndPawnInspectTab.Gear)) yield return step;
        yield return Shot("A retains two physical Beer while S138 is unavailable and B consumed");
        // A's Any choice would legitimately consume once S024 turns off. Hold A through its native gizmo
        // so the only new eligibility control in that window is the untouched history-empty C.
        yield return new SelectionActionStep("select A for native drafted hold during fresh control", new[] { A.Pawn.ThingID }, additive: false);
        var draft = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { A.Pawn.ThingID }, Array.Empty<string>())
            .Single(o => !o.Disabled && o.Interaction == EndToEndGizmoInteraction.Toggle && o.ToggleState == false && o.HotKeyDefName == "Command_ColonistDraft");
        yield return new GizmoActionStep("draft A through its native gizmo", new[] { A.Pawn.ThingID }, draft.RuntimeType,
            EndToEndGizmoInteraction.Toggle, stableGizmoId: draft.StableId);
        yield return new CheckpointStep("native Draft isolated A without changing its policy, stock or history", _ =>
        { EndToEndAssert.True(A.Pawn.Drafted); Held(A, 2); RequireCurrent(); return Evidence(); });
        foreach (var step in ChangeScale(1)) yield return step;
        foreach (var step in AvailabilityManager(true)) yield return step;
        foreach (var step in SetGlobal(true, false)) yield return step;
        var fresh = subjects[2];
        yield return new AssertionStep("fresh control has never consumed and current S138 is re-admitted", _ =>
        { RequireCurrent(); Held(fresh, 2); EndToEndAssert.True(fresh.Pawn.Drafted && tolerance.Available && !expectedMood && fresh.Choice == "1:limit:0"); });
        BeginPhase("fresh tolerance only");
        foreach (var step in Undraft(fresh)) yield return step;
        yield return Run("native selections of the untouched zero subject with S024 off");
        yield return Wait("two original-native-true fresh zero refusals prove S138 remains independently active", () =>
        { RequireCurrent(); Held(fresh, 2); return Rejections(fresh, false, true) >= 2; }, 45, 600);
        yield return Pause("pause fresh independent tolerance evidence");
        foreach (var step in Inspect(fresh, EndToEndPawnInspectTab.Gear)) yield return step;
        yield return Shot("fresh zero retains both original Beer with empty native dose history");
        foreach (var step in Inspect(fresh, EndToEndPawnInspectTab.Health)) yield return step;
        yield return Shot("fresh zero has no new Alcohol effects after native-eligible refusals");
        yield return new AssertionStep("retain distinct causal availability outcomes", _ =>
        { Held(A, 2); AssertDose(B); Held(fresh, 2); EndToEndAssert.Equal(1, doses.Count); });
        foreach (var step in Console()) yield return step;
    }

    private IEnumerable<EndToEndStep> AvailabilityManager(bool available)
    {
        yield return new NativeMainTabActionStep("open Assign to observe current tolerance geometry", "Assign", true);
        yield return Capture<MainTabWindow_Assign>("capture actual Manage entry", value => assign = value);
        yield return new NativePointerScopeBeginStep("own availability manager pointer", value => pointer = value);
        yield return Click("open actual native policy manager", policyView.ManageButton(assign).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture actual manager at current UI scale", value => manager = value);
        if (!available)
            yield return Wait("native unavailable notice appears on the real manager workflow", () =>
            {
                var message = liveMessages.FirstOrDefault(m => m.text == "Megamod.DrugTolerance.LayoutUnavailable".Translate().ToString().CapitalizeFirst());
                if (message == null) return false; if (!notices.Contains(message)) notices.Add(message); return true;
            }, 5);
        else
        {
            yield return Click("select original zero policy after supported native recovery", policyView.PolicyRow(manager, B.Policy).center);
            yield return Capture<Dialog_ManageDrugPolicies>("capture retained numeric-zero editor after recovery", value => manager = value);
        }
        yield return new AssertionStep("actual geometry controls S138 atomic availability only", _ =>
        {
            RequireCurrent(); EndToEndAssert.Equal(available, tolerance.Available);
            EndToEndAssert.Equal(available ? null : "Megamod.DrugTolerance.LayoutUnavailable", tolerance.StatusKey);
            EndToEndAssert.True(scheduled.Available && scheduled.PotentialMood.Available && expectedMood && expectedTolerance);
        });
        yield return Shot(available ? "native 1x manager restores retained numeric zero and current S138 admission" : "native 1.25x manager explains unavailable S138 while S024 remains active");
        yield return new WindowCancelActionStep("close native manager before ordinary thinking", typeof(Dialog_ManageDrugPolicies).FullName!);
        yield return new NativePointerScopeEndStep("restore availability manager pointer", pointer);
        yield return new NativeMainTabActionStep("close native Assign", "Assign", false);
    }

    // Native options path reused from the accepted layout fixture, including the immediate Keep action.
    private IEnumerable<EndToEndStep> ChangeScale(float scale)
    {
        EndToEndAssert.True(Find.TickManager.Paused && Prefs.UIScalePreference != scale &&
            !Find.WindowStack.Windows.Any(w => w is Dialog_Options or Dialog_ResolutionConfirm or FloatMenu));
        yield return new EscapeMenuActionStep("open native Escape for scale selection", true);
        MainTabWindow_Menu menu = null!;
        yield return Capture<MainTabWindow_Menu>("capture actual Escape menu", value => menu = value);
        yield return new NativePointerScopeBeginStep("own display-options pointer", value => pointer = value);
        var labels = new List<string>();
        if (!GameDataSaveLoader.SavingIsTemporarilyDisabled && !game.Info.permadeathMode) labels.Add("Save");
        if (Read<bool>(typeof(MainTabWindow_Menu), menu, "anyGameFiles") && !game.Info.permadeathMode) labels.Add("LoadGame");
        labels.Add("ReviewScenario");
        Vector2 point; var font = Text.Font;
        try { Text.Font = GameFont.Small; float y = labels.Sum(key => Math.Max(45, Text.CalcHeight(key.Translate(), 170)) + 7);
            point = new(103, 18 + y + Math.Max(45, Text.CalcHeight("Options".Translate(), 170)) / 2); }
        finally { Text.Font = font; }
        yield return Click("open native Options", point);
        Dialog_Options options = null!;
        yield return Capture<Dialog_Options>("capture actual native Options", value => options = value);
        var categories = DefDatabase<OptionCategoryDef>.AllDefs.Where(d => (Prefs.DevMode || !d.isDev) && d.modContentPack.IsOfficialMod &&
            (d != OptionCategoryDefOf.Mods || Read<bool>(typeof(Dialog_Options), options, "hasModSettings"))).ToList();
        int index = categories.IndexOf(OptionCategoryDefOf.Interface); EndToEndAssert.True(index >= 0);
        yield return Click("choose native Interface options", new(98, 18 + index * 50 + 24));
        yield return Capture<Dialog_Options>("recapture current Interface options", value => options = value);
        EndToEndAssert.True(ReferenceEquals(Read<OptionCategoryDef>(typeof(Dialog_Options), options, "selectedCategory"), OptionCategoryDefOf.Interface) && options.selectedMod == null);
        float height = Read<float>(typeof(Dialog_Options), options, "optionsViewRectHeight");
        EndToEndAssert.True(float.IsFinite(height) && height > 0 && Read<Vector2>(typeof(Dialog_Options), options, "optionsScrollPosition") == Vector2.zero);
        float width = (float)window.Width - 36 - 177 - (height > window.Height - 36 - 60 ? 26 : 0);
        yield return Click("open native UI-scale choices", new(18 + 177 + width * .8f, 45));
        var choices = Find.WindowStack.Windows.OfType<FloatMenu>().Single(); windows.Add(choices);
        string label = scale + "x";
        EndToEndAssert.Equal(1, Read<List<FloatMenuOption>>(typeof(FloatMenu), choices, "options").Count(o => o.Label == label && !o.Disabled));
        windowsBeforeScale = Find.WindowStack.Windows.ToHashSet();
        displayTransition = true; // Native preview/confirmation may publish the new layout over more than one frame; time stays paused.
        yield return new CurrentFloatMenuActionStep("choose native " + label, label, captureSoleUnownedMenu: true);
        layoutUnavailable = scale > 1;
        var confirmation = Find.WindowStack.Windows.OfType<Dialog_ResolutionConfirm>().Single();
        EndToEndAssert.False(windowsBeforeScale.Contains(confirmation)); windows.Add(confirmation); windowsBeforeScale = null;
        yield return Capture<Dialog_ResolutionConfirm>("capture native ten-second confirmation immediately", _ => { });
        yield return Click("keep actual native display change immediately", new(18 + ((float)window.Width - 36) / 4 - 10, (float)window.Height - 18 - 17.5f));
        yield return Wait("observe chosen scale and retired confirmation", () => !Find.WindowStack.IsOpen<Dialog_ResolutionConfirm>() &&
            !Find.WindowStack.IsOpen<FloatMenu>() && Prefs.UIScalePreference == scale, 5);
        displayTransition = false;
        RequireCurrent();
        yield return new NativePointerScopeEndStep("restore native options pointer", pointer);
        yield return new WindowCancelActionStep("close actual native Options", typeof(Dialog_Options).FullName!);
    }

    private static T Read<T>(Type type, object? owner, string name)
    {
        var field = type.GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static)
            ?? throw new MissingFieldException(type.FullName, name);
        EndToEndAssert.True(field.FieldType == typeof(T) && field.IsStatic == (owner == null)); return (T)field.GetValue(owner)!;
    }
}
