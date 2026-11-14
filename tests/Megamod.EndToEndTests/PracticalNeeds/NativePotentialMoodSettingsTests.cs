using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using LudeonTK;
using Megamod.EndToEndTests.Diagnostics;
using Megamod.Features.PracticalNeeds.Core;
using Megamod.Settings;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.PracticalNeeds;

// The runner selects each actual baseline language; this fixture never changes language.
[RimWorldEndToEndTest("megamod.practical-needs.native-potential-mood-settings", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 1200, MaxWallClockSeconds = 1300)]
public sealed class NativePotentialMoodSettingsTests : IRimWorldEndToEndTest
{
    private static readonly FieldInfo Binding = Field(typeof(SettingsRuntime), "binding");
    private static readonly FieldInfo SaveValues = Field(typeof(SettingsStore), "saveValues");
    private static readonly FieldInfo Tips = typeof(TooltipHandler).GetField("activeTips", BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly FieldInfo TipFrame = typeof(TooltipHandler).GetField("frame", BindingFlags.Static | BindingFlags.NonPublic)!;
    private readonly string saveName = "Megamod-S024-" + Guid.NewGuid().ToString("N");
    private readonly HashSet<Window> windows = new();
    private SettingsRuntime settings = null!;
    private Game game = null!, initial = null!;
    private SaveSettingsBinding binding = null!;
    private GlobalSettingsSnapshot globals = null!;
    private SaveSettingsSnapshot saved = null!;
    private MoodSettingControls controls = null!;
    private bool english, reloadRequested, consoleRequested;
    private int mapId, savedTick, actions, screenshots, waits, saves;
    private string? expectedOverride;
    private bool expectedInheritance = true;

    public void Arrange(IEndToEndContext context)
    {
        game = initial = Current.Game; mapId = Find.CurrentMap.uniqueID;
        settings = SettingsRuntime.Instance ?? throw new InvalidOperationException("Common settings unavailable.");
        binding = Binding.GetValue(settings) as SaveSettingsBinding ?? throw new InvalidOperationException("Published binding unavailable.");
        globals = settings.Store.ExportGlobals(); saved = settings.Store.ExportSave(binding);
        EndToEndAssert.True(game.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting && settings.SaveLifecycleAvailable &&
            !binding.IsRevoked && Find.MainTabsRoot.OpenTab == null && !Find.WindowStack.IsOpen<Dialog_ModSettings>() &&
            !Find.WindowStack.IsOpen<EditWindow_Log>());
        EndToEndAssert.True(new[] { "English", "German", "Spanish", "French", "ChineseSimplified", "Russian", "Japanese" }
            .Contains(LanguageDatabase.activeLanguage.folderName));
        english = LanguageDatabase.activeLanguage.folderName == "English";
        EndToEndAssert.True(!saved.Values.ContainsKey(PotentialMoodSettings.Enabled.Id) &&
            settings.Store.GetGlobal(PotentialMoodSettings.Enabled) && settings.Store.UsesGlobal(PotentialMoodSettings.Enabled));
        EndToEndAssert.True(Tips.FieldType == typeof(Dictionary<int, ActiveTip>) && TipFrame.FieldType == typeof(int) &&
            SaveValues.FieldType == typeof(Dictionary<string, SaveSettingValue>));
        string path = GenFilePaths.FilePathForSavedGame(saveName);
        EndToEndAssert.False(File.Exists(path));
        var oldSpeed = Find.TickManager.CurTimeSpeed;
        controls = new(PotentialMoodSettings.Enabled.Id, RequireGame, windows);
        var oldScroll = controls.Scroll;
        // Registered before the first native window, input scope, or save slot can be acquired.
        context.DeferCleanup(() =>
        {
            if (consoleRequested)
                foreach (var log in Find.WindowStack.Windows.OfType<EditWindow_Log>().ToArray()) log.Close(false);
        });
        context.DeferCleanup(() =>
        {
            var errors = new List<Exception>();
            void Attempt(Action action) { try { action(); } catch (Exception error) { errors.Add(error); } }
            Attempt(() => { if (File.Exists(path)) File.Delete(path); });
            Attempt(() =>
            {
                var current = Current.Game;
                var currentBinding = Binding.GetValue(settings) as SaveSettingsBinding;
                EndToEndAssert.True(current != null && current.CurrentMap?.uniqueID == mapId && currentBinding != null &&
                    !currentBinding.IsRevoked && currentBinding.SaveIdentity == saved.SaveIdentity &&
                    ReferenceEquals(settings.LifecycleReceipt.Owner, current) &&
                    (ReferenceEquals(current, initial) || reloadRequested), "Cleanup refuses an unrelated or partially loaded Game.");
                game = current!; binding = currentBinding!;
                foreach (var window in windows.Concat(Find.WindowStack.Windows.OfType<Dialog_ModSettings>()).Distinct().ToArray())
                    Attempt(() => { if (Find.WindowStack.Windows.Contains(window)) window.Close(false); });
                var values = (Dictionary<string, SaveSettingValue>)SaveValues.GetValue(settings.Store)!;
                if (saved.Values.TryGetValue(PotentialMoodSettings.Enabled.Id, out var prior)) values[PotentialMoodSettings.Enabled.Id] = prior;
                else values.Remove(PotentialMoodSettings.Enabled.Id);
                settings.Store.LoadGlobals(globals); // Cleanup publication through the existing Changed subscriber.
                controls.RestoreScroll(oldScroll);
                EndToEndAssert.True(saved.Values.OrderBy(p => p.Key).SequenceEqual(settings.Store.ExportSave(binding).Values.OrderBy(p => p.Key)));
                EndToEndAssert.True(saved.Values.OrderBy(p => p.Key).SequenceEqual(game.GetComponent<MegamodSaveSettings>().Snapshot.Values.OrderBy(p => p.Key)));
                Find.TickManager.CurTimeSpeed = oldSpeed;
            });
            if (errors.Count != 0) throw new AggregateException("Potential-mood settings cleanup failed.", errors);
        });
        Find.TickManager.Pause(); AssertState();
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        foreach (var step in Workflow().Concat(Console()))
        {
            if (step is SaveLoadActionStep) saves++;
            else if (step is ScreenshotStep) screenshots++;
            else if (step is WaitUntilStep wait) waits += checked((int)wait.Deadline.MaxWallClock.TotalSeconds);
            else if (step.Kind == EndToEndStepKind.Act || step is NativeWindowCaptureStep) actions++;
            yield return step;
        }
        yield return new CheckpointStep("complete locale sequence and declared deadline accounting", _ =>
        {
            AssertState();
            // One initial settings visit; English adds one native reload and a second visit.
            EndToEndAssert.Equal((english ? 27 : 20) + 2 * controls.ScrollClicks, actions);
            EndToEndAssert.Equal(english ? 1 : 0, saves);
            EndToEndAssert.True(controls.ScrollClicks <= (english ? 6 : 3));
            EndToEndAssert.Equal(english ? 7 : 5, screenshots);
            EndToEndAssert.Equal(english ? 41 : 11, waits);
            int maximum = actions * 15 + screenshots * 30 + waits + saves * 300 + 90;
            EndToEndAssert.True(maximum <= (english ? 1300 : 720));
            return new Dictionary<string, string> { ["language"] = LanguageDatabase.activeLanguage.folderName,
                ["actionsAndCaptures"] = actions.ToString(), ["screenshots"] = screenshots.ToString(),
                ["explicitWaitSeconds"] = waits.ToString(), ["saveLoadSeconds"] = (saves * 300).ToString(), ["reserveSeconds"] = "90", ["totalSeconds"] = maximum.ToString(),
                ["scope"] = "new setting, dormant save-off, real localized help; English native reload; broader main-menu/provider obligations remain open" };
        });
    }

    private IEnumerable<EndToEndStep> Workflow()
    {
        foreach (var step in controls.Open()) yield return step;
        yield return new CheckpointStep("translated new row with inherited default and disabled current-save editor", _ =>
        {
            AssertState();
            var result = new Dictionary<string, string>(controls.Evidence());
            foreach (string key in new[] { "Megamod.PotentialMood.Category", "Megamod.PotentialMood.Enabled", "Megamod.PotentialMood.EnabledHelp" })
            { string text = key.Translate(); EndToEndAssert.True(text != key && !string.IsNullOrWhiteSpace(text)); result[key] = text; }
            result["currentSaveDisabled"] = settings.Store.UsesGlobal(PotentialMoodSettings.Enabled).ToString();
            return result;
        });
        yield return Shot("translated potential mood row: Use global, disabled Current save, Global default on");
        yield return controls.Click("uncheck Use global through the native inherited control", controls.Rects.UseGlobal.center);
        yield return new AssertionStep("save choice starts from global on", _ => { expectedInheritance = false; expectedOverride = "true"; AssertState(); });
        foreach (var step in controls.Refresh()) yield return step;
        yield return controls.Click("turn only Current save off through the native checkbox", controls.Rects.CurrentSave.center);
        yield return new AssertionStep("save-off leaves global-on unchanged", _ => { expectedOverride = "false"; AssertState(); });
        yield return Shot("native save-off beside unchanged global-on");
        foreach (var step in controls.Refresh()) yield return step;
        yield return controls.Click("check Use global and retain the dormant off override", controls.Rects.UseGlobal.center);
        yield return new AssertionStep("inherited on retains exact dormant-off", _ => { expectedInheritance = true; AssertState(); });
        foreach (var step in controls.Refresh()) yield return step;
        yield return Shot("Use global restores on while the stored save-off remains dormant");
        string help = "Megamod.PotentialMood.EnabledHelp".Translate();
        yield return controls.MarginClick("place native pointer on inert margin before the new help dwell");
        foreach (var step in controls.Refresh()) yield return step;
        yield return Wait("clear any earlier matching tooltip before the scoped help input", 3, () =>
            !((Dictionary<int, ActiveTip>)Tips.GetValue(null)!).Values.Any(t => t.signal.textGetter == null && t.signal.text == help));
        yield return controls.Click("place native pointer on this row's help label", controls.HelpPoint);
        foreach (var step in controls.Refresh()) yield return step;
        yield return Wait("wait for the actual translated help tooltip", 5, () => Tooltip(help) != null);
        int tooltipId = 0;
        yield return new CheckpointStep("actual native tooltip lifetime and language", _ =>
        {
            var tip = Tooltip(help) ?? throw new InvalidOperationException("Native tooltip disappeared."); tooltipId = tip.ID;
            return new Dictionary<string, string> { ["text"] = help, ["window"] = tip.windowRect.ToString(),
                ["windowId"] = tip.ID.ToString(), ["language"] = LanguageDatabase.activeLanguage.folderName,
                ["input"] = "scoped native click on inert label followed by actual pointer dwell" };
        });
        yield return Shot("translated native potential-mood tooltip");
        yield return controls.MarginClick("leave the tooltip through the inert dialog margin");
        foreach (var step in controls.Refresh()) yield return step;
        yield return Wait("retire the actual tooltip before further native input", 3, () =>
            !((Dictionary<int, ActiveTip>)Tips.GetValue(null)!).Values.Any(t => t.signal.textGetter == null && t.signal.text == help) &&
            !Find.WindowStack.Windows.Any(w => w.ID == tooltipId));
        foreach (var step in controls.Close()) yield return step;
        if (!english) yield break;

        var priorGame = game; var priorBinding = binding;
        var roundtrip = settings.Store.ExportSave(binding);
        savedTick = Find.TickManager.TicksGame; reloadRequested = true;
        yield return new SaveLoadActionStep("save and reload the actual dormant potential-mood override", saveName);
        yield return Wait("observe a fresh already-published native save binding", 30, () =>
            !ReferenceEquals(Current.Game, priorGame) && Binding.GetValue(settings) is SaveSettingsBinding current &&
            !ReferenceEquals(current, priorBinding) && !current.IsRevoked && ReferenceEquals(settings.LifecycleReceipt.Owner, Current.Game));
        yield return new AssertionStep("rebind only the successfully loaded continuation", _ =>
        {
            game = Current.Game; binding = (SaveSettingsBinding)Binding.GetValue(settings)!;
            EndToEndAssert.True(priorBinding.IsRevoked && binding.SaveIdentity == priorBinding.SaveIdentity &&
                binding.Generation != priorBinding.Generation && Find.CurrentMap.uniqueID == mapId && Find.TickManager.Paused &&
                Find.TickManager.TicksGame == savedTick + 1 && roundtrip.Values.OrderBy(p => p.Key)
                    .SequenceEqual(settings.Store.ExportSave(binding).Values.OrderBy(p => p.Key)));
            AssertState();
        });
        foreach (var step in controls.Open()) yield return step;
        yield return Shot("fresh loaded native settings retain inherited-on and dormant-off");
        yield return controls.Click("uncheck Use global after native reload", controls.Rects.UseGlobal.center);
        yield return new AssertionStep("native uncheck reveals retained off without reseeding", _ => { expectedInheritance = false; AssertState(); });
        foreach (var step in controls.Refresh()) yield return step;
        yield return Shot("loaded Current save off is restored beside Global default on");
        foreach (var step in controls.Close()) yield return step;
    }

    private IEnumerable<EndToEndStep> Console()
    {
        RequireGame(); consoleRequested = true;
        yield return new NativeDeveloperLogActionStep("open native console after potential-mood settings", true);
        yield return new NativeWindowCaptureStep("capture actual native settings console", captures =>
        {
            RequireGame(); var log = Find.WindowStack.Windows.OfType<EditWindow_Log>().Single();
            EndToEndAssert.Equal(1, captures.Count(w => w.NativeId == log.ID && w.RuntimeType == log.GetType().FullName));
        });
        yield return Shot("native console after potential-mood settings");
        yield return new NativeDeveloperLogActionStep("close owned native settings console", false);
    }

    private void RequireGame() => EndToEndAssert.True(ReferenceEquals(Current.Game, game) && game.PlayerHasControl &&
        !LongEventHandler.AnyEventNowOrWaiting && Find.CurrentMap.uniqueID == mapId, "Require the exact current disposable continuation.");
    private void AssertState()
    {
        RequireGame();
        EndToEndAssert.True(ReferenceEquals(Binding.GetValue(settings), binding) && !binding.IsRevoked && settings.SaveLifecycleAvailable);
        EndToEndAssert.True(globals.Values.OrderBy(p => p.Key).SequenceEqual(settings.Store.ExportGlobals().Values.OrderBy(p => p.Key)));
        var current = settings.Store.ExportSave(binding);
        EndToEndAssert.True(saved.Values.Where(p => p.Key != PotentialMoodSettings.Enabled.Id).OrderBy(p => p.Key)
            .SequenceEqual(current.Values.Where(p => p.Key != PotentialMoodSettings.Enabled.Id).OrderBy(p => p.Key)));
        if (expectedOverride == null) EndToEndAssert.False(current.Values.ContainsKey(PotentialMoodSettings.Enabled.Id));
        else EndToEndAssert.True(current.Values.TryGetValue(PotentialMoodSettings.Enabled.Id, out var value) &&
            value.Override == expectedOverride && value.UseGlobal == expectedInheritance);
        EndToEndAssert.True(settings.Store.GetGlobal(PotentialMoodSettings.Enabled) &&
            settings.Store.UsesGlobal(PotentialMoodSettings.Enabled) == expectedInheritance &&
            settings.Store.GetEffective(PotentialMoodSettings.Enabled) == (expectedInheritance || expectedOverride == "true"));
    }
    private static ImmediateWindow? Tooltip(string expected)
    {
        int frame = (int)TipFrame.GetValue(null)!;
        foreach (var tip in ((Dictionary<int, ActiveTip>)Tips.GetValue(null)!).Values)
        {
            if (tip.signal.textGetter != null || tip.signal.text != expected || tip.lastTriggerFrame < frame - 1 ||
                tip.lastTriggerFrame > frame || Time.realtimeSinceStartup <= tip.firstTriggerTime + tip.signal.delay) continue;
            int raw = unchecked(153 * tip.signal.uniqueId + 62346);
            EndToEndAssert.True(raw != int.MinValue);
            var window = Find.WindowStack.Windows.OfType<ImmediateWindow>().SingleOrDefault(w => w.ID == -Math.Abs(raw));
            if (window != null && window.windowRect.xMin >= 0 && window.windowRect.yMin >= 0 &&
                window.windowRect.xMax <= UI.screenWidth && window.windowRect.yMax <= UI.screenHeight) return window;
        }
        return null;
    }
    private static FieldInfo Field(Type type, string name) => type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly)
        ?? throw new MissingFieldException(type.FullName, name);
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private static WaitUntilStep Wait(string name, int seconds, Func<bool> predicate) => new(name, _ => predicate(),
        new EndToEndDeadline(int.MaxValue, 1200, TimeSpan.FromSeconds(seconds)));
}

// Native settings navigation only. All row geometry comes from the existing owned helper.
internal sealed class MoodSettingControls
{
    private readonly NativeDiagnosticSettingsView view;
    private readonly Action require;
    private readonly ICollection<Window> windows;
    private string pointer = null!;
    private int visitScrollClicks;
    private EndToEndNativeWindow window = null!;
    internal (Rect UseGlobal, Rect CurrentSave, Rect GlobalDefault) Rects { get; private set; }
    internal int ScrollClicks { get; private set; }
    internal Vector2 Scroll => view.Scroll;
    internal MoodSettingControls(string id, Action require, ICollection<Window> windows)
    { this.require = require; this.windows = windows; view = new(() => pointer, id); }
    internal void RestoreScroll(Vector2 value) => view.RestoreScroll(value);
    internal IEnumerable<EndToEndStep> Open()
    {
        visitScrollClicks = 0;
        yield return new ModSettingsActionStep("open actual megamod settings", "fumblesneeze.megamod");
        yield return new NativePointerScopeBeginStep("own native settings pointer", value => pointer = value);
        foreach (var step in Refresh()) yield return step;
    }
    internal IEnumerable<EndToEndStep> Refresh()
    {
        for (int attempt = 0; ; attempt++)
        {
            bool visible = false;
            yield return new NativeWindowCaptureStep("capture and measure the current exact settings row", captures =>
            {
                require(); var actual = Find.WindowStack.Windows.OfType<Dialog_ModSettings>().Single();
                if (!windows.Contains(actual)) windows.Add(actual);
                window = captures.Single(w => w.NativeId == actual.ID && w.RuntimeType == actual.GetType().FullName);
                view.Bind(window, actual); Rects = view.MeasureBooleanControls(out visible);
                visible &= Rects.GlobalDefault.y >= 54; // Include this row's actual help label above its three controls.
                EndToEndAssert.True(Math.Abs(view.Scroll.x) < .01f);
            });
            if (visible) yield break;
            EndToEndAssert.True(attempt < 3 && visitScrollClicks < 3, "At most three native track clicks per visit may expose the complete current row.");
            var all = new Rect(Rects.UseGlobal.xMin, Rects.GlobalDefault.y - 54,
                Rects.GlobalDefault.xMax - Rects.UseGlobal.xMin, 82);
            var point = view.PlanTrackClick(all); ScrollClicks++; visitScrollClicks++;
            yield return view.Click("scroll native settings track to this row", point);
        }
    }
    internal IEnumerable<EndToEndStep> Close()
    {
        yield return new NativePointerScopeEndStep("restore native settings pointer", pointer);
        yield return new WindowCancelActionStep("close native mod settings", typeof(Dialog_ModSettings).FullName!);
    }
    internal NativeWindowClickActionStep Click(string name, Vector2 point) { require(); return view.Click(name, point); }
    internal NativeWindowClickActionStep MarginClick(string name) => new(name, window.Handle,
        (window.X + 9) * window.UiScale, (window.Y + window.Height / 2) * window.UiScale, null, pointer);
    internal Vector2 HelpPoint => new(Rects.UseGlobal.x + Rects.UseGlobal.width / 2, Rects.GlobalDefault.y - 54 + 13);
    internal IReadOnlyDictionary<string, string> Evidence() => view.Evidence(Rects.GlobalDefault, true);
}
