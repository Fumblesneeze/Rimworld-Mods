using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using LudeonTK;
using Megamod.EndToEndTests.DrugPolicyMaintenance;
using Megamod.Features.DrugTolerance.Core;
using Megamod.Features.DrugTolerance.Runtime;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.DrugTolerance;

// Run this exact case once per baseline language; the actual culture is observed, never assigned.
[RimWorldEndToEndTest("megamod.drug-tolerance.native-language-slider", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 1200, MaxWallClockSeconds = 900)]
public sealed class NativeToleranceLanguageTests : IRimWorldEndToEndTest
{
    private readonly NativeDrugPolicyView view = new();
    private readonly HashSet<Window> ownedWindows = new();
    private readonly Dictionary<DrugPolicyEntry, NativeRow> originalRows = new();
    private readonly string marker = "ToleranceLanguage-" + Guid.NewGuid().ToString("N").Substring(0, 8);
    private IEndToEndContext context = null!;
    private Game game = null!;
    private NativeDrugToleranceRuntime runtime = null!;
    private DrugPolicy policy = null!;
    private DrugPolicyEntry beer = null!, smokeleaf = null!;
    private DrugToleranceChoice malformed = null!, numeric = null!;
    private MainTabWindow_Assign assign = null!;
    private Dialog_ManageDrugPolicies manager = null!;
    private EndToEndNativeWindow window = null!;
    private string pointerScope = null!;
    private int nativeOperations, screenshots, waitSeconds;
    private static readonly FieldInfo Tips = typeof(TooltipHandler).GetField("activeTips", BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly FieldInfo TipFrame = typeof(TooltipHandler).GetField("frame", BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly MethodInfo MeasureColumn = typeof(NativeDrugToleranceRuntime).GetMethod("MeasureColumn", BindingFlags.Static | BindingFlags.NonPublic)!;

    public void Arrange(IEndToEndContext context)
    {
        this.context = context; game = Current.Game;
        EndToEndAssert.True(game.PlayerHasControl && Current.ProgramState == ProgramState.Playing && !LongEventHandler.AnyEventNowOrWaiting);
        EndToEndAssert.True(Find.MainTabsRoot.OpenTab == null && !Find.WindowStack.IsOpen<Dialog_ManageDrugPolicies>() &&
            !Find.WindowStack.IsOpen<EditWindow_Log>());
        EndToEndAssert.True(Tips?.FieldType == typeof(Dictionary<int, ActiveTip>) && TipFrame?.FieldType == typeof(int) && MeasureColumn != null,
            "The inspected passive tooltip and column measurement contracts must exist.");
        EndToEndAssert.True(new[] { "English", "German", "Spanish", "French", "ChineseSimplified", "Russian", "Japanese" }
            .Contains(LanguageDatabase.activeLanguage.folderName));
        runtime = (NativeDrugToleranceRuntime)typeof(MegamodMod).GetField("drugTolerance", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(LoadedModManager.GetMod<MegamodMod>())!;
        EndToEndAssert.True(runtime != null && runtime.Available, "The actual current S138 runtime must be available before native input.");
        var speed = Find.TickManager.CurTimeSpeed;
        context.DeferCleanup(() => { RequireGame(); Find.TickManager.CurTimeSpeed = speed; });
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
        policy = game.drugPolicyDatabase.MakeNewDrugPolicy(); policy.label = marker;
        context.DeferCleanup(() => { RequireGame(); if (policy.label == marker) game.drugPolicyDatabase.AllPolicies.Remove(policy); });
        beer = policy[DefDatabase<ThingDef>.GetNamed("Beer")];
        smokeleaf = policy[DefDatabase<ThingDef>.GetNamed("SmokeleafJoint")];
        beer.allowScheduled = beer.allowedForJoy = smokeleaf.allowScheduled = true;
        beer.onlyIfMoodBelow = .65f; beer.onlyIfJoyBelow = .75f;
        NativeDrugToleranceState.Write(beer, DrugToleranceChoice.Any);
        malformed = DrugToleranceChoice.Decode("language-retained-malformed");
        NativeDrugToleranceState.Write(smokeleaf, malformed);
        for (int i = 0; i < policy.Count; i++) originalRows.Add(policy[i], ReadNativeRow(policy[i]));
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        foreach (var step in Workflow())
        {
            if (step is ScreenshotStep) screenshots++;
            else if (step.Kind == EndToEndStepKind.Act || step is NativeWindowCaptureStep) nativeOperations++;
            yield return step;
        }
        yield return new AssertionStep("count the complete selected language workflow", _ =>
        {
            EndToEndAssert.Equal(29, nativeOperations); EndToEndAssert.Equal(7, screenshots); EndToEndAssert.Equal(16, waitSeconds);
            EndToEndAssert.True(nativeOperations * 15 + screenshots * 30 + waitSeconds + 180 <= 900);
        });
    }

    private IEnumerable<EndToEndStep> Workflow()
    {
        yield return new NativeMainTabActionStep("open Assign for the selected language", "Assign", true);
        yield return Capture<MainTabWindow_Assign>("capture native Manage entry", value => assign = value);
        yield return new NativePointerScopeBeginStep("retain native pointer for translated tooltips", value => pointerScope = value);
        yield return Click("open native drug-policy manager", view.ManageButton(assign).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture native policy list", value => manager = value);
        yield return Click("select the disposable language policy", view.PolicyRow(manager, policy).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture translated native controls", value => manager = value);
        yield return Check("record translated labels and current geometry");
        yield return Shot("translated tolerance header Any and Needs review beside native columns");
        foreach (var step in Tooltip("Megamod.DrugTolerance.RuleHelp", () => OwnedRectangle(true).center, "rule")) yield return step;

        yield return NativeToleranceSliderView.Drag("drag the native tolerance slider to twenty percent", window, pointerScope, manager, policy, beer, 20);
        yield return Capture<Dialog_ManageDrugPolicies>("capture translated numeric slider", value => manager = value);
        yield return new AssertionStep("native drag commits numeric twenty percent", _ =>
        {
            numeric = NativeDrugToleranceState.Read(beer);
            EndToEndAssert.Equal(DrugToleranceChoiceKind.Limit, numeric.Kind); EndToEndAssert.Equal(.2f, numeric.Limit);
            AssertRetained(true, true);
        });
        yield return Shot("translated numeric tolerance after native drag");
        yield return Click("disable native recreational use", view.BeerJoyCheckbox(manager, policy).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture native recreation off", value => manager = value);
        yield return new AssertionStep("only recreation changed", _ => AssertRetained(false, true));
        var scheduled = view.BeerJoyCheckbox(manager, policy); scheduled.x += 36;
        yield return Click("disable native scheduled use", scheduled.center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture retained inactive numeric choice", value => manager = value);
        yield return new AssertionStep("both native permissions off retain the exact choice", _ => AssertRetained(false, false));
        yield return Shot("translated inactive choice after native permission toggles");
        foreach (var step in Tooltip("Megamod.DrugTolerance.InactiveHelp", () => NativeToleranceSliderView.Point(manager, policy, beer, 20), "inactive")) yield return step;
        yield return new AssertionStep("ordinary inactive slider press preserves numeric precision", _ => AssertRetained(false, false));
        yield return Click("reactivate native recreational use", view.BeerJoyCheckbox(manager, policy).center);
        yield return Capture<Dialog_ManageDrugPolicies>("capture reactivated translated tolerance", value => manager = value);
        yield return new AssertionStep("reactivation retains the exact numeric choice", _ => AssertRetained(true, false));
        yield return Shot("translated choice active again with scheduled use still off");
        yield return Check("record final language culture and retained choices");
        yield return new WindowCancelActionStep("close native policy manager", typeof(Dialog_ManageDrugPolicies).FullName!);
        yield return new NativePointerScopeEndStep("restore native pointer after translated controls", pointerScope);
        yield return new NativeMainTabActionStep("close native Assign", "Assign", false);
        yield return new NativeDeveloperLogActionStep("open native developer console", true);
        yield return Capture<EditWindow_Log>("capture exact native console", _ => { });
        yield return Shot("native console after selected language workflow");
        yield return new NativeDeveloperLogActionStep("restore closed native console", false);
    }

    private IEnumerable<EndToEndStep> Tooltip(string key, Func<Vector2> target, string description)
    {
        string expected = key.Translate().ToString();
        EndToEndAssert.True(expected != key && !string.IsNullOrWhiteSpace(expected));
        // The left nine-unit margin lies outside the18-unit native contents inset and away from close buttons.
        yield return Click("place pointer on inert margin before " + description + " tooltip", new Vector2(9, manager.windowRect.height / 2));
        yield return Capture<Dialog_ManageDrugPolicies>("recapture manager at neutral pointer", value => manager = value);
        yield return Wait("wait for earlier matching " + description + " tooltip to clear", 3, () =>
            !ActiveTips().Values.Any(t => t.signal.textGetter == null && t.signal.text == expected));
        yield return Click("place native pointer over " + description + " help", target());
        yield return Capture<Dialog_ManageDrugPolicies>("capture manager while native pointer remains on help", value => manager = value);
        yield return Wait("wait for actual native " + description + " tooltip window", 5, () => TipWindow(expected) != null);
        yield return new CheckpointStep("retain native " + description + " tooltip evidence", _ =>
        {
            var tip = TipWindow(expected) ?? throw new InvalidOperationException("The native tooltip disappeared before observation.");
            return new Dictionary<string, string> { ["key"] = key, ["text"] = expected, ["windowId"] = tip.ID.ToString(),
                ["windowRect"] = tip.windowRect.ToString(), ["language"] = LanguageDatabase.activeLanguage.folderName,
                ["action"] = "scoped native click then real pointer dwell; no draw or TipRegion invocation" };
        });
        yield return Shot("translated " + description + " tooltip drawn by native UI");
    }

    private Dictionary<int, ActiveTip> ActiveTips() { RequireGame(); return (Dictionary<int, ActiveTip>)Tips.GetValue(null)!; }
    private ImmediateWindow? TipWindow(string expected)
    {
        int frame = (int)TipFrame.GetValue(null)!;
        foreach (var tip in ActiveTips().Values)
        {
            if (tip.signal.textGetter != null || tip.signal.text != expected || tip.lastTriggerFrame < frame - 1 ||
                tip.lastTriggerFrame > frame || Time.realtimeSinceStartup <= tip.firstTriggerTime + tip.signal.delay) continue;
            int raw = unchecked(153 * tip.signal.uniqueId + 62346);
            EndToEndAssert.True(raw != int.MinValue, "Native tooltip ID normalization must be representable.");
            var current = Find.WindowStack.Windows.OfType<ImmediateWindow>().SingleOrDefault(w => w.ID == -Math.Abs(raw));
            if (current != null && DrugToleranceLayout.FitsViewport(current.windowRect.x, current.windowRect.y,
                    current.windowRect.width, current.windowRect.height, UI.screenWidth, UI.screenHeight)) return current;
        }
        return null;
    }

    private Rect OwnedRectangle(bool header)
    {
        RequireGame();
        EndToEndAssert.True(runtime.Available, "The native tolerance allocation must remain available.");
        EndToEndAssert.True(ReferenceEquals(view.Selected(manager), policy));
        float measured = (float)MeasureColumn.Invoke(null, null)!; // Existing read-only text measurement restores its GUI font.
        EndToEndAssert.True(DrugToleranceLayout.TryReserve(manager.windowRect.width - 36 - 210 - (header ? 0 : 2), measured, header, out var space));
        int index = Enumerable.Range(0, policy.Count).Single(i => ReferenceEquals(policy[i], beer));
        var rect = new Rect(18 + 210 + (header ? 0 : 1) + space.ColumnOffset,
            18 + (header ? 110 : 147 + index * 35), space.ColumnWidth, header ? 36 : 35);
        EndToEndAssert.True(DrugToleranceLayout.FitsViewport(manager.windowRect.x + rect.x, manager.windowRect.y + rect.y,
            rect.width, rect.height, UI.screenWidth, UI.screenHeight));
        return rect;
    }

    private void AssertRetained(bool joy, bool scheduled)
    {
        RequireGame();
        EndToEndAssert.True(ReferenceEquals(view.Selected(manager), policy) && game.drugPolicyDatabase.AllPolicies.Contains(policy));
        EndToEndAssert.True(ReferenceEquals(policy[beer.drug], beer) && ReferenceEquals(NativeDrugToleranceState.Read(beer), numeric));
        EndToEndAssert.True(ReferenceEquals(NativeDrugToleranceState.Read(smokeleaf), malformed));
        EndToEndAssert.Equal(originalRows.Count, policy.Count);
        for (int i = 0; i < policy.Count; i++)
        {
            var row = policy[i];
            EndToEndAssert.True(originalRows.TryGetValue(row, out var original));
            var expected = ReferenceEquals(row, beer) ? original! with { Joy = joy, Scheduled = scheduled } : original;
            EndToEndAssert.Equal(expected, ReadNativeRow(row));
        }
    }

    private sealed record NativeRow(bool Addiction, bool Joy, bool Scheduled, float Days, float Mood, float Recreation, int Stock);
    private static NativeRow ReadNativeRow(DrugPolicyEntry entry) => new(entry.allowedForAddiction, entry.allowedForJoy, entry.allowScheduled,
        entry.daysFrequency, entry.onlyIfMoodBelow, entry.onlyIfJoyBelow, entry.takeToInventory);

    private CheckpointStep Check(string name) => new(name, _ => new Dictionary<string, string>
    {
        ["language"] = LanguageDatabase.activeLanguage.folderName, ["culture"] = CultureInfo.CurrentCulture.Name,
        ["decimalSeparator"] = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator,
        ["numericExample"] = .2f.ToString("P2", CultureInfo.CurrentCulture),
        ["header"] = "Megamod.DrugTolerance.Header".Translate().ToString(), ["any"] = "Megamod.DrugTolerance.Any".Translate().ToString(),
        ["needsReview"] = "Megamod.DrugTolerance.NeedsReview".Translate().ToString(),
        ["inactive"] = "Megamod.DrugTolerance.Inactive".Translate(.2f.ToString("P2", CultureInfo.CurrentCulture)).ToString(),
        ["managerRect"] = manager.windowRect.ToString(), ["headerRect"] = OwnedRectangle(true).ToString(),
        ["rowRect"] = OwnedRectangle(false).ToString(), ["uiScale"] = Prefs.UIScale.ToString(CultureInfo.InvariantCulture),
        ["beerChoice"] = NativeDrugToleranceState.ReadEncoded(beer) ?? "absent", ["smokeleafChoice"] = NativeDrugToleranceState.ReadEncoded(smokeleaf) ?? "absent",
        ["scope"] = "selected-language native slider labels, retained inactive choice and actual tooltips; no text parser or ingestion claim"
    });
    private WaitUntilStep Wait(string name, int seconds, Func<bool> condition)
    { waitSeconds += seconds; return new(name, _ => condition(), new EndToEndDeadline(int.MaxValue, 1200, TimeSpan.FromSeconds(seconds))); }
    private void RequireGame() => EndToEndAssert.True(ReferenceEquals(Current.Game, game), "Refuse a departed game during native language verification.");
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private NativeWindowClickActionStep Click(string name, Vector2 point) => new(name, window.Handle,
        (window.X + point.x) * window.UiScale, (window.Y + point.y) * window.UiScale, null, pointerScope);
    private NativeWindowCaptureStep Capture<T>(string name, Action<T> retain) where T : Window => new(name, windows =>
    {
        RequireGame();
        var current = Find.WindowStack.WindowOfType<T>() ?? throw new InvalidOperationException("Missing native window: " + typeof(T));
        if (ownedWindows.Add(current)) context.DeferCleanup(() =>
        {
            RequireGame();
            if (current is MainTabWindow_Assign && ReferenceEquals(Find.MainTabsRoot.OpenTab?.TabWindow, current)) Find.MainTabsRoot.EscapeCurrentTab(false);
            else if (Find.WindowStack.Windows.Contains(current)) current.Close(false);
        });
        retain(current);
        window = windows.Single(item => item.NativeId == current.ID && item.RuntimeType == current.GetType().FullName);
    });
}
