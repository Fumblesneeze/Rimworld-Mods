using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LudeonTK;
using Megamod.Features.Diagnostics;
using Megamod.Settings;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.Diagnostics;

// Fresh isolated XML-loaded group. Only native controls change settings, request reload or open Options.
[RimWorldEndToEndTest("megamod.diagnostics.definition-toggle-reload-default", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod", "fumblesneeze.megamoddiagnosticsfixture",
    "fumblesneeze.megamoddiagnosticslateoptions", MaxFrames = 18000, MaxGameTicks = 18000, MaxWallClockSeconds = 300)]
public sealed class NativeDefinitionDiagnosticsReloadTests : IRimWorldEndToEndTest
{
    private const string Fractional = "Parsed 37.5 as int.";
    private const string Marker = "\n\n[Def Error] Current definition context\n";
    private const string MissingPrefix = "Type MegamodDiagnosticsFixtureMissingDefType is not a Def type or could not be found, in file ";
    private const string LateError = "Unofficial OptionCategoryDef: megamod diagnostics late options fixture found, ignoring";
    private static readonly string[] Names = { "MegamodDiagnosticsFixtureFirst", "MegamodDiagnosticsFixtureSecond" };
    private SettingsRuntime settings = null!;
    private SaveSettingsBinding binding = null!;
    private GlobalSettingsSnapshot globals = null!;
    private SaveSettingsSnapshot save = null!;
    private NativeDiagnosticSettingsView settingsView = null!;
    private NativeDiagnosticLogView logView = null!;
    private string pointer = null!;
    private readonly HashSet<Window> ownedWindows = new();
    private readonly List<Record> history = new();
    private LogMessage[] initialWarnings = Array.Empty<LogMessage>();
    private LogMessage missing = null!;
    private EditWindow_Log? initialLog;
    private sealed record Record(LogMessage Message, string Text, string Trace, int Repeats);

    public void Arrange(IEndToEndContext context)
    {
        settings = SettingsRuntime.Instance ?? throw new InvalidOperationException("Settings runtime is unavailable.");
        binding = settings.CurrentBinding ?? throw new InvalidOperationException("A loaded disposable game is required.");
        globals = settings.Store.ExportGlobals();
        save = settings.Store.ExportSave(binding);
        EndToEndAssert.True(Prefs.DevMode && Current.ProgramState == ProgramState.Playing);
        EndToEndAssert.True(settings.Store.GetEffective(DiagnosticSettings.DefinitionContext), "A fresh enabled profile is required.");
        EndToEndAssert.False(settings.Store.GetEffective(WarningCaptureSettings.MuteWarnings), "Warnings must initially be unmuted.");
        EndToEndAssert.False(Find.WindowStack.IsOpen<Dialog_ModSettings>() || Find.WindowStack.IsOpen<Dialog_Options>() ||
            Find.WindowStack.IsOpen<MainTabWindow_Menu>(), "The scenario must not take ownership of existing settings/menu windows.");
        settingsView = new NativeDiagnosticSettingsView(() => pointer);
        logView = new NativeDiagnosticLogView(() => pointer);
        Vector2 initialSettingsScroll = settingsView.Scroll;
        initialLog = Find.WindowStack.WindowOfType<EditWindow_Log>();
        bool autoOpen = EditWindow_Log.wantsToOpen;
        var logFields = new[] { "selectedMessage", "messagesScrollPosition", "detailsScrollPosition", "detailsPaneHeight" }
            .Select(name => typeof(EditWindow_Log).GetField(name, BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Native log observation field unavailable: " + name))
            .ToDictionary(field => field, field => field.GetValue(null));
        var speed = Find.TickManager.CurTimeSpeed;
        context.DeferCleanup(() =>
        {
            try
            {
                foreach (var window in ownedWindows)
                    if (Find.WindowStack.Windows.Contains(window)) window.Close();
                // Cleanup restores only the view. Interrupted setting edits remain in the disposable profile.
                if (initialLog != null && Find.WindowStack.WindowOfType<EditWindow_Log>() == null)
                    Find.WindowStack.Add(initialLog);
                foreach (var pair in logFields) pair.Key.SetValue(null, pair.Value);
                EditWindow_Log.wantsToOpen = autoOpen;
                settingsView.RestoreScroll(initialSettingsScroll);
            }
            finally { Find.TickManager.CurTimeSpeed = speed; }
        });
        Find.TickManager.CurTimeSpeed = TimeSpeed.Paused;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new CheckpointStep("retain initial actual loader diagnostics before any action", _ => Evidence());
        yield return Shot("native view before diagnostic setting reload workflow");
        yield return new AssertionStep("initial loader supplied both separate contexts and one original ErrorOnce", _ =>
        {
            EndToEndAssert.Equal(2, Occurrences());
            initialWarnings = Names.Select(name => Warnings().Single(message => HasName(message, name))).ToArray();
            foreach (var message in initialWarnings) { AssertContext(message); EndToEndAssert.Equal(1, message.repeats); Remember(message); }
            missing = Missing().Single();
            EndToEndAssert.Equal(1, missing.repeats);
            AssertContext(missing);
            EndToEndAssert.True(HasName(missing, "MegamodDiagnosticsFixtureMissingType"));
            Remember(missing);
            EndToEndAssert.Equal(0, Log.Messages.Count(message => message.text == LateError), "Options must not have been opened earlier.");
        });
        foreach (var step in Toggle(false)) yield return step;
        foreach (var step in Reload(context, "disabled", 4)) yield return step;
        LogMessage[] plain = Array.Empty<LogMessage>();
        yield return new AssertionStep("disabled reload emits only two new plain warnings and preserves history", _ =>
        {
            AssertHistory(); AssertChoice(false); AssertMissingOnce();
            plain = Warnings().Where(message => !initialWarnings.Contains(message)).ToArray();
            EndToEndAssert.True(plain.Length >= 1 && plain.Length <= 2);
            EndToEndAssert.Equal(2, plain.Sum(message => message.repeats));
            foreach (var message in plain) { EndToEndAssert.Equal(Fractional, message.text); Remember(message); }
        });
        foreach (var message in plain)
            foreach (var step in Inspect(message, "disabled plain warning")) yield return step;

        foreach (var step in Toggle(true)) yield return step;
        foreach (var step in Reload(context, "reenabled", 6)) yield return step;
        LogMessage[] renewed = Array.Empty<LogMessage>();
        yield return new AssertionStep("reenabled reload emits two new contexts without rewriting prior diagnostics", _ =>
        {
            AssertHistory(); AssertChoice(true); AssertMissingOnce();
            renewed = Warnings().Where(message => !initialWarnings.Contains(message) && !plain.Contains(message)).ToArray();
            EndToEndAssert.Equal(2, renewed.Length);
            foreach (string name in Names)
            {
                var message = renewed.Single(item => HasName(item, name));
                AssertContext(message); EndToEndAssert.Equal(1, message.repeats); Remember(message);
                EndToEndAssert.Equal(2, Warnings().Where(item => HasName(item, name)).Sum(item => item.repeats));
                EndToEndAssert.Equal(37, DefDatabase<StatCategoryDef>.GetNamed(name).displayOrder,
                    "Supporting observation: both native reloads retain ordinary fractional parsing behavior.");
            }
        });
        foreach (string name in Names)
            foreach (var step in Inspect(renewed.Single(message => HasName(message, name)), "reenabled " + name)) yield return step;

        foreach (var step in OpenLateOptions()) yield return step;
        LogMessage late = null!;
        yield return new AssertionStep("later ordinary Options error has no stale definition context", _ =>
        {
            AssertHistory(); AssertChoice(true); AssertMissingOnce();
            late = Log.Messages.Single(message => message.text.StartsWith(LateError, StringComparison.Ordinal));
            EndToEndAssert.Equal(LogMessageType.Error, late.type);
            EndToEndAssert.Equal(LateError, late.text, "The late native error must contain no inherited definition suffix.");
            EndToEndAssert.Equal(1, late.repeats);
            Remember(late);
        });
        foreach (var step in Inspect(late, "later ordinary Options error")) yield return step;
        yield return new CheckpointStep("complete observed loader and later native error records", _ => Evidence());
        yield return new AssertionStep("final occurrence counts and settings remain correct", _ =>
        {
            AssertHistory(); AssertChoice(true); AssertMissingOnce(); EndToEndAssert.Equal(6, Occurrences());
        });
        yield return new NativeDeveloperLogActionStep("restore the original native log open state", initialLog != null);
        if (initialLog != null) yield return CaptureLog("own the final reopened log for prior-view cleanup");
        yield return Shot("native view after restoring enabled diagnostics");
    }

    private IEnumerable<EndToEndStep> Toggle(bool enabled)
    {
        yield return new NativeDeveloperLogActionStep("close native log before opening settings", false);
        yield return new ModSettingsActionStep("open native settings to " + (enabled ? "enable" : "disable") + " definition context", "fumblesneeze.megamod");
        yield return new NativePointerScopeBeginStep("own pointer during diagnostic checkbox action", handle => pointer = handle);
        Rect editor = default;
        bool visible = false;
        for (int attempt = 0; ; attempt++)
        {
            yield return new NativeWindowCaptureStep("capture current settings geometry", windows =>
            {
                var actual = Find.WindowStack.WindowOfType<Dialog_ModSettings>() ?? throw new InvalidOperationException("Native settings did not open.");
                ownedWindows.Add(actual);
                settingsView.Bind(windows.Single(item => item.RuntimeType == typeof(Dialog_ModSettings).FullName), actual);
            });
            yield return new CheckpointStep("measure definition checkbox viewport", _ =>
            {
                editor = settingsView.MeasureCheckbox(out visible);
                return settingsView.Evidence(editor, visible);
            });
            yield return Shot("definition checkbox viewport before native action");
            if (visible) break;
            Vector2 point = default, before = default;
            yield return new AssertionStep("bounded measured settings scroll is possible", _ =>
            {
                EndToEndAssert.True(attempt < 3, "Three native track clicks did not expose the definition editor; inspect retained views.");
                before = settingsView.Scroll; point = settingsView.PlanTrackClick(editor);
            });
            yield return settingsView.Click("click native settings scrollbar toward diagnostic option", point);
            yield return Shot("settings after native scrollbar click");
            yield return new AssertionStep("native settings scroll moved vertically", _ =>
                EndToEndAssert.True(Math.Abs(settingsView.Scroll.y - before.y) > 0.5f && Math.Abs(settingsView.Scroll.x - before.x) < 0.01f));
        }
        yield return new AssertionStep("checkbox starts in the opposite state", _ =>
        {
            AssertHistory(); EndToEndAssert.Equal(!enabled, settings.Store.GetEffective(DiagnosticSettings.DefinitionContext));
        });
        yield return settingsView.Click("click the global definition-context checkbox " + (enabled ? "on" : "off"), editor.center);
        yield return Shot("native diagnostic checkbox after requested edit");
        yield return new AssertionStep("native checkbox changed only definition-context global setting", _ => { AssertChoice(enabled); AssertHistory(); });
        yield return new NativePointerScopeEndStep("restore pointer after settings edit", pointer);
        yield return new WindowCancelActionStep("close native mod settings and write preferences", typeof(Dialog_ModSettings).FullName!);
        yield return new AssertionStep("native settings closed", _ => EndToEndAssert.False(Find.WindowStack.IsOpen<Dialog_ModSettings>()));
    }

    private IEnumerable<EndToEndStep> Reload(IEndToEndContext context, string phase, int expected)
    {
        EndToEndDebugAction action = null!;
        yield return new CheckpointStep("rediscover current native Hot reload Defs action for " + phase, _ =>
        {
            var page = context.GetRequiredService<IEndToEndDebugActionCatalog>().Query("Hot reload Defs", new[] { "General" }, limit: 10);
            EndToEndAssert.False(page.PageTruncated);
            action = page.Items.Single(item => item.Path == "Actions\\Hot reload Defs" && item.SourceType == "Verse.DebugActionsMisc");
            EndToEndAssert.True(action.Visible && action.Active && action.DiscoveryError == null);
            return new Dictionary<string, string> { ["handle"] = action.Handle, ["path"] = action.Path,
                ["source"] = action.SourceType, ["phase"] = phase, ["previousOccurrences"] = Occurrences().ToString() };
        });
        yield return Shot("before native Hot reload Defs " + phase);
        yield return new NativeDebugActionStep("invoke the current native Hot reload Defs " + phase, action.Handle);
        yield return new CheckpointStep("native reload queue before in-flight view " + phase, _ => ReloadEventEvidence());
        yield return Shot("native in-flight view after Hot reload Defs " + phase);
        yield return new CheckpointStep("native reload queue after in-flight view " + phase, _ => ReloadEventEvidence());
        yield return new WaitUntilStep("native reload completed and emitted the next fractional warnings " + phase,
            _ => !LongEventHandler.AnyEventNowOrWaiting && Occurrences() >= expected,
            new EndToEndDeadline(6000, 6000, TimeSpan.FromSeconds(90)));
        yield return new CheckpointStep("actual retained records after native reload " + phase, _ => Evidence());
        yield return Shot("native view after reload " + phase);
        yield return new AssertionStep("reload occurrence count is exact " + phase, _ => EndToEndAssert.Equal(expected, Occurrences()));
    }

    private IEnumerable<EndToEndStep> Inspect(LogMessage target, string label)
    {
        yield return new NativeDeveloperLogActionStep("open native log for " + label, true);
        yield return CaptureLog("capture reopened log for " + label);
        yield return Shot("native log before inspecting " + label);
        yield return new NativePointerScopeBeginStep("own pointer while inspecting " + label, handle => pointer = handle);
        yield return logView.ToolbarClick("compact native trace before locating " + label, "Trace small");
        Rect row = default;
        bool visible = false;
        for (int attempt = 0; ; attempt++)
        {
            yield return CaptureLog("capture current native log viewport");
            yield return new CheckpointStep("measure actual row for " + label, _ =>
            {
                row = logView.MeasureRow(target, out visible); return logView.GeometryEvidence(row, visible);
            });
            yield return Shot("native row viewport for " + label);
            if (visible) break;
            Vector2 point = default, before = default;
            yield return new AssertionStep("bounded native track can expose " + label, _ =>
            {
                EndToEndAssert.True(attempt < 3, "Three track attempts did not expose this row; inspect retained observations.");
                AssertHistory(); before = NativeDiagnosticLogView.Read<Vector2>("messagesScrollPosition");
                point = logView.PlanNativeTrackClick(row);
            });
            yield return logView.Click("click native log scrollbar toward " + label, point);
            yield return Shot("native log after track action for " + label);
            yield return new AssertionStep("native log scroll moved vertically", _ =>
            {
                var after = NativeDiagnosticLogView.Read<Vector2>("messagesScrollPosition");
                EndToEndAssert.True(Math.Abs(after.y - before.y) > 0.5f && Math.Abs(after.x - before.x) < 0.01f);
            });
        }
        yield return logView.Click("select actual retained native " + label, row.center);
        yield return new AssertionStep("native selection is the requested retained record", _ =>
        {
            EndToEndAssert.True(ReferenceEquals(target, NativeDiagnosticLogView.Selected)); AssertHistory();
        });
        yield return CaptureLog("capture selected native record before expanding details");
        yield return logView.ToolbarClick("expand native details for " + label, "Trace big");
        yield return Shot("selected full native details for " + label);
        yield return new AssertionStep("complete selected diagnostic fits native details", _ =>
        {
            EndToEndAssert.True(ReferenceEquals(target, NativeDiagnosticLogView.Selected)); AssertHistory();
            var font = Text.Font;
            try
            {
                Text.Font = GameFont.Tiny;
                float height = NativeDiagnosticLogView.Read<float>("detailsPaneHeight");
                EndToEndAssert.True(height > 100 && Text.CalcHeight(target.text, logView.ContentWidth - 16) <= height - 7,
                    "Full context is clipped; inspect the retained screenshot and add native detail scrolling before accepting.");
            }
            finally { Text.Font = font; }
        });
        yield return new CheckpointStep("observed selected record " + label, _ => new Dictionary<string, string>
        { ["text"] = target.text, ["trace"] = target.StackTrace, ["repeats"] = target.repeats.ToString(), ["selectedReferenceVerified"] = "true" });
        yield return new NativePointerScopeEndStep("restore pointer after selected diagnostic", pointer);
        yield return new NativeDeveloperLogActionStep("close native log after selected diagnostic", false);
    }

    private IEnumerable<EndToEndStep> OpenLateOptions()
    {
        yield return new AssertionStep("the late diagnostic has not happened before the ordinary Options action", _ =>
            EndToEndAssert.Equal(0, Log.Messages.Count(message => message.text.StartsWith(LateError, StringComparison.Ordinal))));
        yield return new EscapeMenuActionStep("open the ordinary Escape menu after completed reloads", true);
        EndToEndNativeWindow window = null!;
        MainTabWindow_Menu menu = null!;
        Rect button = default;
        yield return new NativeWindowCaptureStep("capture the actual Escape menu", windows =>
        {
            menu = Find.WindowStack.WindowOfType<MainTabWindow_Menu>() ?? throw new InvalidOperationException("Native Escape menu is absent.");
            ownedWindows.Add(menu);
            window = windows.Single(item => item.RuntimeType == typeof(MainTabWindow_Menu).FullName);
        });
        yield return new CheckpointStep("measure native Options button from current menu prerequisites", _ =>
        {
            button = OptionsButton(menu, window);
            return new Dictionary<string, string> { ["buttonInContent"] = button.ToString(), ["menuHandle"] = window.Handle,
                ["contentOrigin"] = "window + (18,18); native MainTabWindow_Menu and OptionListingUtility" };
        });
        yield return Shot("native Escape menu before Options action");
        yield return new NativePointerScopeBeginStep("own pointer for ordinary Options action", handle => pointer = handle);
        yield return new NativeWindowClickActionStep("click ordinary native Options button", window.Handle,
            (window.X + 18 + button.center.x) * window.UiScale, (window.Y + 18 + button.center.y) * window.UiScale, null, pointer);
        yield return new AssertionStep("native Options opened and emitted the late diagnostic", _ =>
        {
            var options = Find.WindowStack.WindowOfType<Dialog_Options>();
            EndToEndAssert.NotNull(options, "Native Options did not open; no direct window construction is permitted.");
            ownedWindows.Add(options!);
            EndToEndAssert.Equal(1, Log.Messages.Where(message => message.text.StartsWith(LateError, StringComparison.Ordinal)).Sum(message => message.repeats));
        });
        yield return Shot("ordinary Options window after its native diagnostic");
        yield return new NativePointerScopeEndStep("restore pointer after Options action", pointer);
        yield return new WindowCancelActionStep("close ordinary Options with native cancel", typeof(Dialog_Options).FullName!);
    }

    private static Rect OptionsButton(MainTabWindow_Menu menu, EndToEndNativeWindow window)
    {
        EndToEndAssert.True(Current.ProgramState == ProgramState.Playing && string.IsNullOrEmpty(menu.optionalTitle));
        var savedField = typeof(MainTabWindow_Menu).GetField("anyGameFiles", BindingFlags.Instance | BindingFlags.NonPublic);
        EndToEndAssert.True(savedField?.FieldType == typeof(bool), "Native menu prerequisite observation is unavailable.");
        bool anySaved = (bool)savedField!.GetValue(menu)!;
        var labels = new List<string>();
        if (!GameDataSaveLoader.SavingIsTemporarilyDisabled && !Current.Game.Info.permadeathMode) labels.Add("Save");
        if (anySaved && !Current.Game.Info.permadeathMode) labels.Add("LoadGame");
        labels.Add("ReviewScenario");
        var font = Text.Font;
        try
        {
            Text.Font = GameFont.Small;
            float y = labels.Sum(label => Math.Max(45, Text.CalcHeight(label.Translate(), 170)) + 7);
            var button = new Rect(0, y, 170, Math.Max(45, Text.CalcHeight("Options".Translate(), 170)));
            EndToEndAssert.True(button.xMax <= window.Width - 36 && button.yMax <= window.Height - 36,
                "The measured native Options control is clipped.");
            return button;
        }
        finally { Text.Font = font; }
    }

    private NativeWindowCaptureStep CaptureLog(string name) => new(name, windows =>
    {
        var actual = Find.WindowStack.WindowOfType<EditWindow_Log>() ?? throw new InvalidOperationException("Native log is absent.");
        ownedWindows.Add(actual);
        logView.Bind(windows.Single(item => item.RuntimeType == typeof(EditWindow_Log).FullName), actual);
    });
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private static LogMessage[] Warnings() => Log.Messages.Where(message => message.type == LogMessageType.Warning && message.text.StartsWith(Fractional, StringComparison.Ordinal)).ToArray();
    private static LogMessage[] Missing() => Log.Messages.Where(message => message.type == LogMessageType.Error && message.text.StartsWith(MissingPrefix, StringComparison.Ordinal)).ToArray();
    private static int Occurrences() => Warnings().Sum(message => message.repeats);
    // Diagnostic reads only. A capture does not promise a Repaint or force native work to run.
    private static IReadOnlyDictionary<string, string> ReloadEventEvidence()
    {
        const BindingFlags fields = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;
        static object? ReadField(Type type, object? instance, string name) =>
            (type.GetField(name, fields) ?? throw new MissingFieldException(type.FullName, name)).GetValue(instance);

        var queue = ReadField(typeof(LongEventHandler), null, "eventQueue") as System.Collections.ICollection
            ?? throw new InvalidOperationException("The inspected native long-event queue is unavailable.");
        var current = ReadField(typeof(LongEventHandler), null, "currentEvent");
        var values = new Dictionary<string, string>
        {
            ["queuedCount"] = queue.Count.ToString(),
            ["currentPresent"] = (current != null).ToString(),
            ["nativeLongEventPending"] = LongEventHandler.AnyEventNowOrWaiting.ToString(),
            ["fractionalOccurrences"] = Occurrences().ToString()
        };
        if (current == null) return values;

        var type = current.GetType();
        var key = ReadField(type, current, "eventTextKey");
        if (key != null && (key is not string text || text.Length > 128))
            throw new InvalidOperationException("The native long-event source key exceeds the diagnostic bound.");
        values["eventTextKey"] = (string?)key ?? "<null>";
        foreach (string name in new[] { "doAsynchronously", "alreadyDisplayed", "canEverUseStandardWindow", "forceHideUI" })
            values[name] = ReadField(type, current, name) is bool flag ? flag.ToString()
                : throw new InvalidOperationException("The inspected native long-event flag changed: " + name);
        foreach (string name in new[] { "UseStandardWindow", "ShouldWaitUntilDisplayed" })
            values[name] = type.GetProperty(name, fields)?.GetValue(current) is bool flag ? flag.ToString()
                : throw new InvalidOperationException("The inspected native long-event predicate changed: " + name);
        return values;
    }
    private static bool HasName(LogMessage message, string name) => message.text.Contains("\nDefinition: " + name + "\n", StringComparison.Ordinal);
    private static void AssertContext(LogMessage message)
    {
        int index = message.text.IndexOf(Marker, StringComparison.Ordinal);
        EndToEndAssert.True(index > 0 && index == message.text.LastIndexOf(Marker, StringComparison.Ordinal));
        EndToEndAssert.True(message.text.Contains("Package: fumblesneeze.megamoddiagnosticsfixture", StringComparison.Ordinal)
            && message.text.Contains("DefinitionDiagnostics.xml", StringComparison.Ordinal));
        if (message.type == LogMessageType.Warning) EndToEndAssert.Equal(Fractional, message.text.Substring(0, index));
    }
    private void Remember(LogMessage message) => history.Add(new(message, message.text, message.StackTrace, message.repeats));
    private void AssertHistory()
    {
        var retained = Log.Messages.ToArray();
        foreach (var record in history)
        {
            EndToEndAssert.True(retained.Contains(record.Message), "A prior diagnostic left retained native history.");
            EndToEndAssert.Equal(record.Text, record.Message.text);
            EndToEndAssert.Equal(record.Trace, record.Message.StackTrace);
            EndToEndAssert.Equal(record.Repeats, record.Message.repeats);
        }
    }
    private void AssertMissingOnce()
    {
        EndToEndAssert.True(ReferenceEquals(missing, Missing().Single()), "Reload must not replace the original ErrorOnce diagnostic.");
        EndToEndAssert.Equal(1, missing.repeats);
    }
    private void AssertChoice(bool enabled)
    {
        EndToEndAssert.Equal(enabled, settings.Store.GetEffective(DiagnosticSettings.DefinitionContext));
        var current = settings.Store.ExportGlobals();
        EndToEndAssert.True(current.Values.TryGetValue(DiagnosticSettings.DefinitionContext.Id, out string? value) && value == (enabled ? "true" : "false"));
        EndToEndAssert.True(globals.Values.Where(pair => pair.Key != DiagnosticSettings.DefinitionContext.Id).OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .SequenceEqual(current.Values.Where(pair => pair.Key != DiagnosticSettings.DefinitionContext.Id).OrderBy(pair => pair.Key, StringComparer.Ordinal)),
            "The native diagnostic checkbox must not change unrelated global preferences.");
        EndToEndAssert.True(ReferenceEquals(binding, settings.CurrentBinding));
        EndToEndAssert.True(save.Values.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .SequenceEqual(settings.Store.ExportSave(binding).Values.OrderBy(pair => pair.Key, StringComparer.Ordinal)), "Save overrides/inheritance changed.");
    }
    private static IReadOnlyDictionary<string, string> Evidence()
    {
        var messages = Log.Messages.Where(message => message.text.StartsWith(Fractional, StringComparison.Ordinal)
            || message.text.StartsWith(MissingPrefix, StringComparison.Ordinal) || message.text.StartsWith(LateError, StringComparison.Ordinal)).ToArray();
        EndToEndAssert.True(messages.Length <= 16 && messages.All(message => message.text.Length <= 65536), "Diagnostic evidence exceeds its bound.");
        var values = new Dictionary<string, string> { ["fractionalOccurrences"] = Occurrences().ToString(),
            ["missingTypeOccurrences"] = Missing().Sum(message => message.repeats).ToString(),
            ["nativeLongEventPending"] = LongEventHandler.AnyEventNowOrWaiting.ToString() };
        for (int i = 0; i < messages.Length; i++) values["record-" + i] = messages[i].type + " x" + messages[i].repeats + ": " + messages[i].text;
        return values;
    }
}
