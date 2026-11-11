using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using LudeonTK;
using Megamod.Features.Diagnostics;
using Megamod.Settings;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.Diagnostics;

// Actual installed original, ordinary XML fixture, native settings-close/reload/log inspection.
// This is a bounded effective-off probe, not an original-mod compatibility certification.
[RimWorldEndToEndTest("megamod.diagnostics.original-definition-off", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "mlie.logafterdeferror", "fumblesneeze.megamod",
    "fumblesneeze.megamoddiagnosticsfixture", MaxFrames = int.MaxValue, MaxGameTicks = 6000,
    MaxWallClockSeconds = 1500)]
public sealed class NativeOriginalDefinitionOffTests : IRimWorldEndToEndTest
{
    private const string Fractional = "Parsed 37.5 as int.";
    private const string Marker = "\n\n[Def Error] Current definition context\n";
    private const string MissingPrefix = "Type MegamodDiagnosticsFixtureMissingDefType is not a Def type or could not be found, in file ";
    private const string WarningPrefix = "[fumblesneeze.megamod] Active original mlie.logafterdeferror;";
    private const string OffReason = "Megamod_Settings_OriginalProviderOff";
    private SettingsRuntime settings = null!;
    private SaveSettingsBinding binding = null!;
    private GlobalSettingsSnapshot globals = null!;
    private SaveSettingsSnapshot save = null!;
    private OriginalOffSettingsView settingsView = null!;
    private NativeDiagnosticLogView logView = null!;
    private string pointer = null!;
    private string settingsPath = null!;
    private string[] activePackages = Array.Empty<string>();
    private Game game = null!;
    private WindowStack windowStack = null!;
    private EditWindow_Log? initialLog;
    private ProtectedDiagnosticsRuntime protectedDiagnostics = null!;
    private LogMessage? overlap;
    private string startupWarning = null!;
    private string warningDestination = null!;
    private FallbackRecord? initialFallback;
    private FallbackRecord? observedFallback;
    private LogMessage missing = null!;
    private readonly HashSet<Window> ownedWindows = new();
    private readonly List<Record> history = new();
    private sealed record Record(LogMessage Message, string Text, string Trace, int Repeats);
    private sealed record FallbackRecord(string Path, string Text, string Sha256, int Bytes, string[] OriginalWarnings);

    public void Arrange(IEndToEndContext context)
    {
        game = Current.Game ?? throw new InvalidOperationException("A disposable loaded game is required.");
        EndToEndAssert.True(Prefs.DevMode && Current.ProgramState == ProgramState.Playing && game.PlayerHasControl);
        activePackages = LoadedModManager.RunningModsListForReading.Select(mod => mod.PackageIdPlayerFacing.Trim().ToLowerInvariant()).ToArray();
        EndToEndAssert.True(activePackages.SequenceEqual(new[] { "brrainz.harmony", "ludeon.rimworld", "mlie.logafterdeferror",
            "fumblesneeze.megamod", "fumblesneeze.megamoddiagnosticsfixture", "fumblesneeze.rimworlddevgateway" }),
            "This probe requires the real original and exactly the normal fixture, with Gateway appended last.");
        settings = SettingsRuntime.Instance ?? throw new InvalidOperationException("Settings runtime is unavailable.");
        binding = settings.CurrentBinding ?? throw new InvalidOperationException("The disposable game has no settings binding.");
        globals = settings.Store.ExportGlobals();
        save = settings.Store.ExportSave(binding);
        AssertChoices();
        EndToEndAssert.True(globals.Values.TryGetValue(DiagnosticSettings.DefinitionContext.Id, out var value) && value == "true",
            "Prelaunch must explicitly seed requested context=true; a default alone does not prove retention.");
        EndToEndAssert.True(globals.Values.TryGetValue(WarningCaptureSettings.MuteWarnings.Id, out var mute) && mute == "false",
            "Prelaunch must explicitly seed unrelated warning mute=false.");
        EndToEndAssert.True(GenCommandLine.TryGetCommandLineArg("savedatafolder", out var isolated) && !string.IsNullOrWhiteSpace(isolated),
            "Only the runner's explicit disposable savedatafolder may be inspected.");
        EndToEndAssert.Equal(Path.GetFullPath(isolated), Path.GetFullPath(GenFilePaths.SaveDataFolderPath));
        settingsPath = Path.Combine(GenFilePaths.ConfigFolderPath, "Mod_fumblesneeze.megamod_MegamodMod.xml");
        AssertSerializedChoices();
        protectedDiagnostics = (ProtectedDiagnosticsRuntime)(typeof(MegamodMod)
            .GetField("diagnostics", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(LoadedModManager.GetMod<MegamodMod>())
            ?? throw new InvalidOperationException("The actual owned protected diagnostics sink is unavailable."));
        windowStack = Find.WindowStack;
        EndToEndAssert.False(windowStack.IsOpen<Dialog_ModSettings>() || windowStack.IsOpen<Dialog_Options>() ||
            windowStack.IsOpen<MainTabWindow_Menu>(), "Existing settings/menu windows are not scenario-owned.");
        settingsView = new OriginalOffSettingsView(() => pointer);
        logView = new NativeDiagnosticLogView(() => pointer);
        var priorScroll = settingsView.Scroll;
        initialLog = windowStack.WindowOfType<EditWindow_Log>();
        bool priorAutoOpen = EditWindow_Log.wantsToOpen;
        var logFields = new[] { "selectedMessage", "messagesScrollPosition", "detailsScrollPosition", "detailsPaneHeight" }
            .Select(name => typeof(EditWindow_Log).GetField(name, BindingFlags.Static | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Native log observation unavailable: " + name))
            .ToDictionary(field => field, field => field.GetValue(null));
        var ticks = Find.TickManager;
        var priorSpeed = ticks.CurTimeSpeed;
        context.DeferCleanup(() =>
        {
            // No save/load is requested. Never mutate a replacement game merely because its IDs resemble ours.
            if (!ReferenceEquals(game, Current.Game) || !ReferenceEquals(windowStack, Find.WindowStack) ||
                !ReferenceEquals(ticks, Find.TickManager))
                throw new InvalidOperationException("Scenario game/window ownership changed; cleanup refused replacement state.");
            var errors = new List<Exception>();
            void Attempt(Action action) { try { action(); } catch (Exception error) { errors.Add(error); } }
            // A native reload may auto-open a log before the next capture, including on a failed assertion.
            var currentLog = windowStack.WindowOfType<EditWindow_Log>();
            if (currentLog != null && !ReferenceEquals(currentLog, initialLog)) ownedWindows.Add(currentLog);
            foreach (var window in ownedWindows)
                Attempt(() => { if (windowStack.Windows.Contains(window)) window.Close(); });
            Attempt(() => { if (initialLog != null && !windowStack.IsOpen<EditWindow_Log>()) windowStack.Add(initialLog); });
            foreach (var pair in logFields) Attempt(() => pair.Key.SetValue(null, pair.Value));
            Attempt(() => EditWindow_Log.wantsToOpen = priorAutoOpen);
            Attempt(() => settingsView.RestoreScroll(priorScroll));
            Attempt(() => ticks.CurTimeSpeed = priorSpeed);
            if (errors.Count > 0) throw new AggregateException("Original-off view cleanup failed.", errors);
        });
        ticks.CurTimeSpeed = TimeSpeed.Paused; // Disposable setup only; not the behavior under test.
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new AssertionStep("retain actual startup warning and fixture occurrences before native actions", _ =>
        {
            ObserveStartupWarning(initial: true);
            EndToEndAssert.True(startupWarning.Contains("S094 definition diagnostic context", StringComparison.Ordinal) &&
                startupWarning.Contains("best-effort ordinary feature-off", StringComparison.Ordinal) &&
                startupWarning.Contains("stored preferences are retained", StringComparison.Ordinal));
            EndToEndAssert.Equal(2, Occurrences());
            missing = Missing().Single();
            EndToEndAssert.Equal(1, missing.repeats);
            var nativeHistory = Warnings().Concat(new[] { missing });
            if (overlap is { } retainedOverlap) nativeHistory = nativeHistory.Append(retainedOverlap);
            foreach (var message in nativeHistory)
            {
                EndToEndAssert.False(message.text.Contains(Marker, StringComparison.Ordinal));
                history.Add(new(message, message.text, message.StackTrace, message.repeats));
            }
        });
        yield return new CheckpointStep("initial retained preference and actual startup diagnostics", _ => Evidence());
        yield return Shot("before native original-off inspection");
        if (overlap is { } nativeOverlap)
            foreach (var step in Inspect(nativeOverlap, "startup original-provider warning")) yield return step;
        // -NoFileLog uses the durable owned warning file, not a manufactured native Log.Message.
        yield return new NativeDeveloperLogActionStep("close any existing native log before settings inspection", false);
        foreach (var step in InspectSettings()) yield return step;

        EndToEndDebugAction action = null!;
        yield return new CheckpointStep("discover exact native Hot reload Defs action", _ =>
        {
            var page = context.GetRequiredService<IEndToEndDebugActionCatalog>().Query("Hot reload Defs", new[] { "General" }, limit: 10);
            EndToEndAssert.False(page.PageTruncated);
            action = page.Items.Single(item => item.Path == "Actions\\Hot reload Defs" && item.SourceType == "Verse.DebugActionsMisc");
            EndToEndAssert.True(action.Visible && action.Active && action.DiscoveryError == null);
            return new Dictionary<string, string> { ["handle"] = action.Handle, ["path"] = action.Path, ["source"] = action.SourceType };
        });
        yield return Shot("before native reload with requested context on but effectively off");
        yield return new NativeDebugActionStep("invoke native Hot reload Defs with original active", action.Handle);
        yield return Shot("native in-flight view after original-off reload action");
        yield return new WaitUntilStep("native reload finishes with two further fractional occurrences",
            _ => !LongEventHandler.AnyEventNowOrWaiting && Occurrences() >= 4,
            new EndToEndDeadline(int.MaxValue, 6000, TimeSpan.FromSeconds(90)));
        yield return Shot("native view after original-off reload completion");
        LogMessage[] changed = Array.Empty<LogMessage>();
        yield return new AssertionStep("reload retains normal warnings without owned context or rewritten preferences", _ =>
        {
            AssertChoices(); AssertSerializedChoices(); AssertHistory();
            EndToEndAssert.Equal(4, Occurrences());
            changed = Warnings().Where(message => message.repeats > PriorRepeats(message)).ToArray();
            EndToEndAssert.True(changed.Length >= 1 && changed.Length <= 2);
            EndToEndAssert.Equal(2, changed.Sum(message => message.repeats - PriorRepeats(message)));
            foreach (var message in changed) EndToEndAssert.False(message.text.Contains(Marker, StringComparison.Ordinal));
            // Original informational messages may remain. We assert only absence of our exact context section.
        });
        for (int i = 0; i < changed.Length; i++)
            foreach (var step in Inspect(changed[i], "reloaded ordinary warning " + (i + 1))) yield return step;
        yield return new AssertionStep("final history and preferences remain intact without overlap-warning spam", _ =>
        { AssertHistory(); AssertChoices(); AssertSerializedChoices(); EndToEndAssert.Equal(4, Occurrences()); });
        yield return new CheckpointStep("final original-off observation after actual native console selection", _ => Evidence());
        yield return new NativeDeveloperLogActionStep("restore prior native log open state", initialLog != null);
        if (initialLog != null) yield return CaptureLog("capture owned reopened log before restoring prior view");
        yield return Shot("native view after original-off workflow");
    }

    private IEnumerable<EndToEndStep> InspectSettings()
    {
        yield return new ModSettingsActionStep("open native megamod settings with retained requested preference", "fumblesneeze.megamod");
        yield return new NativePointerScopeBeginStep("own pointer during settings inspection", handle => pointer = handle);
        Rect row = default;
        bool visible = false;
        for (int attempt = 0; ; attempt++)
        {
            yield return new NativeWindowCaptureStep("capture native settings and current dynamic row layout", windows =>
            {
                var actual = windowStack.WindowOfType<Dialog_ModSettings>() ?? throw new InvalidOperationException("Native settings did not open.");
                ownedWindows.Add(actual);
                settingsView.Bind(windows.Single(item => item.RuntimeType == typeof(Dialog_ModSettings).FullName), actual);
            });
            yield return new CheckpointStep("measure entire requested checkbox and effective-off explanation row", _ =>
            { row = settingsView.MeasureRow(out visible); return settingsView.Evidence(row, visible); });
            yield return Shot("native retained preference and effective-off explanation viewport");
            if (visible) break;
            Vector2 before = default, point = default;
            yield return new AssertionStep("one measured settings track correction can expose the complete row", _ =>
            {
                EndToEndAssert.True(attempt == 0, "One measured native track click did not expose the row; inspect screenshots.");
                before = settingsView.Scroll; point = settingsView.PlanTrackClick(row);
            });
            yield return settingsView.Click("scroll native settings to original-off explanation", point);
            yield return Shot("native settings after measured scrollbar action");
            yield return new AssertionStep("native settings scroll moved without preference edits", _ =>
            {
                EndToEndAssert.True(Math.Abs(settingsView.Scroll.y - before.y) > 0.5f && Math.Abs(settingsView.Scroll.x - before.x) < 0.01f);
                AssertChoices();
            });
        }
        yield return new AssertionStep("requested context is on and ordinary effective value remains off", _ => AssertChoices());
        yield return new NativePointerScopeEndStep("restore pointer after settings inspection", pointer);
        yield return new WindowCancelActionStep("close native settings and serialize retained choices", typeof(Dialog_ModSettings).FullName!);
        yield return new AssertionStep("native close retained all serialized globals and unrelated save settings", _ =>
        {
            EndToEndAssert.False(windowStack.IsOpen<Dialog_ModSettings>());
            AssertChoices(); AssertSerializedChoices(); AssertHistory();
        });
    }

    private IEnumerable<EndToEndStep> Inspect(LogMessage target, string label)
    {
        yield return new NativeDeveloperLogActionStep("open native console for " + label, true);
        yield return CaptureLog("capture actual console for " + label);
        yield return new NativePointerScopeBeginStep("own pointer while selecting " + label, handle => pointer = handle);
        yield return logView.ToolbarClick("compact native trace for " + label, "Trace small");
        Rect row = default;
        bool visible = false;
        for (int attempt = 0; ; attempt++)
        {
            yield return CaptureLog("capture current row viewport for " + label);
            yield return new CheckpointStep("measure retained native row for " + label, _ =>
            { row = logView.MeasureRow(target, out visible); return logView.GeometryEvidence(row, visible); });
            yield return Shot("native row viewport before selecting " + label);
            if (visible) break;
            Vector2 point = default, before = default;
            yield return new AssertionStep("one measured native track correction can expose " + label, _ =>
            {
                EndToEndAssert.True(attempt == 0, "One measured track click did not expose the row; inspect retained views.");
                before = NativeDiagnosticLogView.Read<Vector2>("messagesScrollPosition");
                point = logView.PlanNativeTrackClick(row);
            });
            yield return logView.Click("scroll native log to " + label, point);
            yield return Shot("native log after scrollbar action for " + label);
            yield return new AssertionStep("native log scroll moved vertically", _ =>
            {
                var after = NativeDiagnosticLogView.Read<Vector2>("messagesScrollPosition");
                EndToEndAssert.True(Math.Abs(after.y - before.y) > 0.5f && Math.Abs(after.x - before.x) < 0.01f);
            });
        }
        yield return logView.Click("select actual native " + label, row.center);
        yield return new AssertionStep("native selected identity matches " + label, _ =>
            EndToEndAssert.True(ReferenceEquals(target, NativeDiagnosticLogView.Selected)));
        yield return CaptureLog("reacquire selected native console before Trace big");
        yield return logView.ToolbarClick("expand native details for " + label, "Trace big");
        yield return Shot("selected full native details for " + label);
        yield return new AssertionStep("complete selected explanation is visible", _ =>
        {
            EndToEndAssert.True(ReferenceEquals(target, NativeDiagnosticLogView.Selected));
            AssertHistory();
            var font = Text.Font;
            try
            {
                Text.Font = GameFont.Tiny;
                float height = NativeDiagnosticLogView.Read<float>("detailsPaneHeight");
                EndToEndAssert.True(height > 100 && Text.CalcHeight(target.text, logView.ContentWidth - 16) <= height - 7,
                    "Selected details are clipped; screenshots do not prove the full explanation.");
            }
            finally { Text.Font = font; }
        });
        yield return new CheckpointStep("observed selected native " + label, _ => new Dictionary<string, string>
        { ["text"] = target.text, ["trace"] = target.StackTrace, ["repeats"] = target.repeats.ToString() });
        yield return new NativePointerScopeEndStep("restore pointer after inspecting " + label, pointer);
        yield return new NativeDeveloperLogActionStep("close native console after " + label, false);
    }

    private NativeWindowCaptureStep CaptureLog(string name) => new(name, windows =>
    {
        var actual = windowStack.WindowOfType<EditWindow_Log>() ?? throw new InvalidOperationException("Native log is absent.");
        ownedWindows.Add(actual);
        logView.Bind(windows.Single(item => item.RuntimeType == typeof(EditWindow_Log).FullName), actual);
    });
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private static LogMessage[] Warnings() => Log.Messages.Where(message => message.type == LogMessageType.Warning && message.text.StartsWith(Fractional, StringComparison.Ordinal)).ToArray();
    private static LogMessage[] Missing() => Log.Messages.Where(message => message.type == LogMessageType.Error && message.text.StartsWith(MissingPrefix, StringComparison.Ordinal)).ToArray();
    private static LogMessage[] OverlapWarnings() => Log.Messages.Where(message => message.type == LogMessageType.Warning && message.text.StartsWith(WarningPrefix, StringComparison.Ordinal)).ToArray();
    private static int Occurrences() => Warnings().Sum(message => message.repeats);
    private int PriorRepeats(LogMessage message) => history.SingleOrDefault(record => ReferenceEquals(record.Message, message))?.Repeats ?? 0;

    private void AssertHistory()
    {
        var retained = Log.Messages.ToArray();
        foreach (var record in history)
        {
            EndToEndAssert.True(retained.Contains(record.Message), "A prior native record disappeared.");
            EndToEndAssert.Equal(record.Text, record.Message.text);
            EndToEndAssert.Equal(record.Trace, record.Message.StackTrace);
            if (record.Message.type == LogMessageType.Warning && record.Text.StartsWith(Fractional, StringComparison.Ordinal))
                EndToEndAssert.True(record.Message.repeats >= record.Repeats, "A native repeat count decreased.");
            else EndToEndAssert.Equal(record.Repeats, record.Message.repeats);
        }
        EndToEndAssert.True(ReferenceEquals(missing, Missing().Single()));
        EndToEndAssert.Equal(1, missing.repeats); // Existing fixture error allowance stays exactly one.
        ObserveStartupWarning(initial: false);
    }

    private void ObserveStartupWarning(bool initial)
    {
        // Read the existing sink only. Constructing a new sink or calling Write/TryWrite would manufacture evidence.
        EndToEndAssert.Equal(0L, protectedDiagnostics.Sink.UndeliveredCount);
        EndToEndAssert.Equal(0, protectedDiagnostics.Sink.DeliveryFailures.Count);
        EndToEndAssert.Equal(0L, protectedDiagnostics.Sink.ReentrantCount);
        var native = OverlapWarnings();
        observedFallback = ReadOwnedFallback();
        int fileOccurrences = observedFallback?.OriginalWarnings.Length ?? 0;
        EndToEndAssert.Equal(1, native.Sum(message => message.repeats) + fileOccurrences,
            "Exactly one actual original-provider warning must exist in retained native output or the owned durable fallback.");
        if (initial)
        {
            overlap = native.SingleOrDefault();
            initialFallback = observedFallback;
            warningDestination = overlap == null ? "owned-durable-fallback" : "native-retained-warning";
            startupWarning = overlap?.text ?? observedFallback!.OriginalWarnings.Single();
        }
        else
        {
            EndToEndAssert.True(ReferenceEquals(overlap, native.SingleOrDefault()), "Startup warning destination/identity changed.");
            EndToEndAssert.Equal(startupWarning, overlap?.text ?? observedFallback!.OriginalWarnings.Single());
            if (initialFallback != null)
            {
                EndToEndAssert.True(observedFallback != null && observedFallback.Path == initialFallback.Path &&
                    observedFallback.Text.StartsWith(initialFallback.Text, StringComparison.Ordinal),
                    "Previously delivered fallback evidence was removed or rewritten.");
            }
        }
    }

    private FallbackRecord? ReadOwnedFallback()
    {
        string path = protectedDiagnostics.FallbackFilePath ?? throw new InvalidOperationException("Owned fallback identity is unavailable.");
        string absolute = Path.GetFullPath(path);
        string logs = Path.Combine(Path.GetFullPath(GenFilePaths.SaveDataFolderPath), "Logs");
        string fileName = Path.GetFileName(absolute);
        const string prefix = "Megamod-startup-";
        EndToEndAssert.True(string.Equals(Path.GetDirectoryName(absolute), logs, StringComparison.OrdinalIgnoreCase) &&
            fileName.StartsWith(prefix, StringComparison.Ordinal) && fileName.EndsWith(".log", StringComparison.Ordinal) &&
            Guid.TryParseExact(fileName.Substring(prefix.Length, fileName.Length - prefix.Length - 4), "N", out _),
            "Protected fallback must be the runtime's unique file beneath this disposable save-data Logs directory.");
        if (!File.Exists(absolute)) return null;
        // The product keeps its write handle open with FileShare.Read. Our read must allow that existing writer.
        using var stream = new FileStream(absolute, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        long byteCount = stream.Length;
        EndToEndAssert.True(byteCount > 0 && byteCount <= 1024 * 1024, "Protected warning file exceeds the diagnostic bound.");
        var bytes = new byte[(int)byteCount];
        stream.ReadExactly(bytes);
        EndToEndAssert.Equal(-1, stream.ReadByte(), "Protected warning file changed during bounded observation; do not accept a torn snapshot.");
        string text = new UTF8Encoding(false, true).GetString(bytes);
        var warnings = text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Where(line => line.StartsWith(WarningPrefix, StringComparison.Ordinal)).ToArray();
        EndToEndAssert.True(warnings.All(line => line.Length <= 4096));
        return new(absolute, text, Convert.ToHexString(SHA256.HashData(bytes)), bytes.Length, warnings);
    }

    private void AssertChoices()
    {
        EndToEndAssert.True(ReferenceEquals(game, Current.Game));
        EndToEndAssert.True(settings.Store.ReadCurrentRequested(DiagnosticSettings.DefinitionContext).Value);
        EndToEndAssert.False(settings.Store.GetEffective(DiagnosticSettings.DefinitionContext));
        EndToEndAssert.Equal(OffReason, settings.Store.EffectiveOffReason(DiagnosticSettings.DefinitionContext));
        EndToEndAssert.False(settings.Store.ReadCurrentRequested(WarningCaptureSettings.MuteWarnings).Value);
        EndToEndAssert.False(settings.Store.GetEffective(WarningCaptureSettings.MuteWarnings));
        EndToEndAssert.True(settings.Store.EffectiveOffReason(WarningCaptureSettings.MuteWarnings) == null);
        EndToEndAssert.True(globals.Values.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .SequenceEqual(settings.Store.ExportGlobals().Values.OrderBy(pair => pair.Key, StringComparer.Ordinal)));
        EndToEndAssert.True(ReferenceEquals(binding, settings.CurrentBinding));
        EndToEndAssert.True(save.Values.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .SequenceEqual(settings.Store.ExportSave(binding).Values.OrderBy(pair => pair.Key, StringComparer.Ordinal)));
    }

    private void AssertSerializedChoices()
    {
        var info = new FileInfo(settingsPath);
        EndToEndAssert.True(info.Exists && info.Length > 0 && info.Length <= 1024 * 1024, "Expected bounded isolated settings XML is absent.");
        using var reader = XmlReader.Create(settingsPath, new XmlReaderSettings
        { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null, MaxCharactersInDocument = 1024 * 1024 });
        var document = XDocument.Load(reader);
        var block = document.Root?.Element("ModSettings") ?? throw new InvalidOperationException("Ordinary settings block is absent.");
        EndToEndAssert.Equal("SettingsBlock", document.Root!.Name.LocalName);
        EndToEndAssert.Equal("Megamod.Settings.MegamodGlobalSettings", (string?)block.Attribute("Class"));
        var dictionary = block.Element("values") ?? throw new InvalidOperationException("Serialized choices are absent.");
        var keys = dictionary.Element("keys")?.Elements("li").Select(item => item.Value).ToArray() ?? Array.Empty<string>();
        var values = dictionary.Element("values")?.Elements("li").Select(item => item.Value).ToArray() ?? Array.Empty<string>();
        EndToEndAssert.True(keys.Length > 0 && keys.Length <= 2048 && keys.Length == values.Length);
        var parsed = keys.Select((key, index) => new KeyValuePair<string, string>(key, values[index])).ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal);
        EndToEndAssert.True(globals.Values.OrderBy(pair => pair.Key, StringComparer.Ordinal)
            .SequenceEqual(parsed.OrderBy(pair => pair.Key, StringComparer.Ordinal)), "Native settings close rewrote requested preferences.");
    }

    private IReadOnlyDictionary<string, string> Evidence() => new Dictionary<string, string>
    {
        ["requestedContext"] = settings.Store.ReadCurrentRequested(DiagnosticSettings.DefinitionContext).Value.ToString(),
        ["effectiveContext"] = settings.Store.GetEffective(DiagnosticSettings.DefinitionContext).ToString(),
        ["effectiveOffReason"] = settings.Store.EffectiveOffReason(DiagnosticSettings.DefinitionContext) ?? "<none>",
        ["unrelatedMute"] = settings.Store.GetEffective(WarningCaptureSettings.MuteWarnings).ToString(),
        ["fractionalOccurrences"] = Occurrences().ToString(), ["missingTypeOccurrences"] = Missing().Sum(message => message.repeats).ToString(),
        ["nativeOverlapOccurrences"] = OverlapWarnings().Sum(message => message.repeats).ToString(),
        ["fallbackOverlapOccurrences"] = (observedFallback?.OriginalWarnings.Length ?? 0).ToString(),
        ["startupWarningDestination"] = warningDestination, ["startupWarning"] = startupWarning,
        ["noFileLogArgument"] = GenCommandLine.CommandLineArgPassed("NoFileLog").ToString(),
        ["protectedNativeUnavailableReason"] = protectedDiagnostics.NativeUnavailableReason ?? "<no construction failure; readiness may still reject>",
        ["ownedFallbackPath"] = protectedDiagnostics.FallbackFilePath ?? "<unavailable>",
        ["observedFallbackSha256"] = observedFallback?.Sha256 ?? "<no file>",
        ["observedFallbackBytes"] = observedFallback?.Bytes.ToString() ?? "0",
        ["isolatedSettingsPath"] = settingsPath,
        ["activePackageOrder"] = string.Join(",", activePackages)
    };

    // Scenario-local adaptation: whole dynamic S094 row, not the old fixed-height helper contract.
    // Reads layout metadata/measurement only; never invokes Draw, Reset or editor commit delegates.
    private sealed class OriginalOffSettingsView
    {
        private readonly SettingsRenderer renderer;
        private readonly FieldInfo scrollField;
        private readonly Func<string> pointerScope;
        private EndToEndNativeWindow window = null!;
        private float viewHeight;
        internal OriginalOffSettingsView(Func<string> pointerScope)
        {
            this.pointerScope = pointerScope;
            renderer = (SettingsRenderer)(typeof(MegamodMod).GetField("renderer", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(LoadedModManager.GetMod<MegamodMod>()) ?? throw new InvalidOperationException("Owned renderer is absent."));
            scrollField = typeof(SettingsRenderer).GetField("scroll", BindingFlags.Instance | BindingFlags.NonPublic)
                ?? throw new InvalidOperationException("Owned scroll observation is absent.");
            EndToEndAssert.Equal(typeof(Vector2), scrollField.FieldType);
        }
        internal Vector2 Scroll => (Vector2)scrollField.GetValue(renderer)!;
        internal void RestoreScroll(Vector2 value) => scrollField.SetValue(renderer, value); // Cleanup only.
        internal void Bind(EndToEndNativeWindow captured, Dialog_ModSettings actual)
        {
            window = captured;
            EndToEndAssert.True(string.IsNullOrEmpty(actual.optionalTitle) && window.Width >= 700 && window.Height >= 500);
        }
        private float ViewportWidth => (float)window.Width - 36;
        private float ViewportHeight => (float)window.Height - 36 - 40 - Window.CloseButSize.y;
        private float ViewWidth => ViewportWidth - 20;
        internal Rect MeasureRow(out bool visible)
        {
            var rows = (IDictionary)(typeof(SettingsRenderer).GetField("rows", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.GetValue(renderer) ?? throw new InvalidOperationException("Owned row metadata is absent."));
            EndToEndAssert.True(rows.Count > 0 && rows.Count <= 2048);
            var objects = rows.Values.Cast<object>().ToDictionary(row => Property<SettingsRowLayout>(row, "Layout").Id, StringComparer.Ordinal);
            var ordered = SettingsRowLayout.Capture(objects.Values.Select(row => Property<SettingsRowLayout>(row, "Layout")));
            var font = Text.Font;
            var anchor = Text.Anchor;
            Rect result = default;
            try
            {
                Text.Font = GameFont.Small; Text.Anchor = TextAnchor.UpperLeft;
                float y = 0;
                string? category = null;
                foreach (var placement in ordered)
                {
                    if (category != placement.CategoryId) { y += 32; category = placement.CategoryId; }
                    var row = objects[placement.Id];
                    var measure = Property<Func<float, float>?>(row, "MeasureHeight");
                    float height = measure?.Invoke(ViewWidth) ?? Property<float>(row, "Height");
                    EndToEndAssert.True(float.IsFinite(height) && height > 0 && height <= 4096);
                    if (placement.Id == DiagnosticSettings.DefinitionContext.Id)
                    {
                        EndToEndAssert.True(measure != null && height >= 130, "Expected measured original-off Boolean row is absent.");
                        result = new Rect(-Scroll.x, y - Scroll.y, ViewWidth, height);
                    }
                    y += height;
                }
                viewHeight = y;
            }
            finally { Text.Font = font; Text.Anchor = anchor; }
            visible = result.height > 0 && result.yMin >= 0 && result.yMax <= ViewportHeight && Math.Abs(Scroll.x) < 0.01f;
            return result;
        }
        internal Vector2 PlanTrackClick(Rect row)
        {
            var handler = typeof(GUI).GetMethod("HandleScrollbars", BindingFlags.Static | BindingFlags.NonPublic, null,
                new[] { typeof(Rect), typeof(Vector2), typeof(Rect), typeof(float), typeof(float) }, null);
            EndToEndAssert.True(typeof(GUI).Assembly.GetName().Name == "UnityStubs" && handler?.ReturnType == typeof(Vector2));
            float maximum = viewHeight - ViewportHeight;
            EndToEndAssert.True(maximum > 0 && row.height > 0 && row.height < ViewportHeight && Math.Abs(Scroll.x) < 0.01f);
            float thumb = Math.Max(20, ViewportHeight * Mathf.Clamp01(ViewportHeight / viewHeight));
            float travel = ViewportHeight - thumb;
            EndToEndAssert.True(travel > 0);
            float top = Scroll.y / maximum * travel;
            float absolute = row.yMin + Scroll.y;
            float desired = Mathf.Clamp(absolute + row.height / 2 - ViewportHeight / 2, 0, maximum);
            foreach (float point in new[] { thumb / 2 + desired / maximum * travel, top - 2, top + thumb + 2 })
            {
                if (point < 1 || point > ViewportHeight - 1 || (point >= top - 1 && point <= top + thumb + 1)) continue;
                float resulting = Mathf.Clamp01((point - thumb / 2) / travel) * maximum;
                if (absolute - resulting >= 1 && absolute - resulting + row.height <= ViewportHeight - 1)
                    return new Vector2(ViewportWidth - 8, point);
            }
            throw new InvalidOperationException("No measured outside-thumb track point exposes the entire original-off row.");
        }
        internal NativeWindowClickActionStep Click(string name, Vector2 point) => new(name, window.Handle,
            (window.X + 18 + point.x) * window.UiScale, (window.Y + 58 + point.y) * window.UiScale, null, pointerScope());
        internal IReadOnlyDictionary<string, string> Evidence(Rect row, bool visible) => new Dictionary<string, string>
        {
            ["windowHandle"] = window.Handle, ["contentOrigin"] = "window + (18,58)", ["wholeRow"] = row.ToString(),
            ["globalCheckbox"] = new Rect(2 * ((ViewWidth - 16) / 3 + 8) - Scroll.x, row.y + 54, (ViewWidth - 16) / 3, 28).ToString(),
            ["visible"] = visible.ToString(), ["scroll"] = Scroll.ToString(),
            ["explanation"] = OffReason.Translate().ToString(), ["measuredViewHeight"] = viewHeight.ToString()
        };
        private static T Property<T>(object row, string name) => (T)(row.GetType().GetProperty(name)
            ?? throw new InvalidOperationException("Owned row metadata changed: " + name)).GetValue(row)!;
    }
}
