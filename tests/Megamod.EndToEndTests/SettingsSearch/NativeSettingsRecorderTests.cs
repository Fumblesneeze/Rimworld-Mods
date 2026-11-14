using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Linq;
using HarmonyLib;
using LudeonTK;
using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.SettingsSearch.Core;
using Megamod.Features.SettingsSearch.Runtime;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using UnityStubs.Gui;
using Verse;

namespace Megamod.EndToEndTests.SettingsSearch;

// A real unfamiliar Workshop page, with test-only instrumentation around its ordinary native draw.
// This proves the recorder component, not automatic discovery or a completed search/reveal UI.
[RimWorldEndToEndTest("megamod.settings-search.native-recorder", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "Dreadarm.PickUpAndHaul", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 1200, MaxWallClockSeconds = 500)]
public sealed class NativeSettingsRecorderTests : IRimWorldEndToEndTest
{
    private const string PackageId = "Dreadarm.PickUpAndHaul";
    private const string PatchOwner = "fumblesneeze.megamod.e2e.settings-recorder";
    private static NativeSettingsRecorderTests? active;
    private readonly List<Dialog_ModSettings> ownedWindows = new();
    private readonly Dictionary<string, object> baseline = new(StringComparer.Ordinal);
    private Mod mod = null!;
    private object settings = null!;
    private SettingsPageRecorder recorder = null!;
    private Harmony patches = null!;
    private Game game = null!;
    private EndToEndNativeWindow window = null!;
    private string pointer = null!;
    private bool observing, originalOnTheWay, consoleRequested;
    private int repaintCalls, capturedRepaints, failureProbes;
    private long maxRecordingTicks;
    private Probe nextProbe;
    private enum Probe { None, ThrowAfterDraw, ReplaceBackend, NestedCapture }

    public void Arrange(IEndToEndContext context)
    {
        game = Current.Game;
        EndToEndAssert.True(game.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting &&
            LanguageDatabase.activeLanguage.folderName == "English" && active == null &&
            !Find.WindowStack.IsOpen<Dialog_ModSettings>() && !Find.WindowStack.IsOpen<EditWindow_Log>());
        mod = LoadedModManager.ModHandles.Single(m => string.Equals(m.Content.PackageIdPlayerFacing, PackageId,
            StringComparison.OrdinalIgnoreCase));
        settings = mod.GetType().GetProperty("Settings", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        foreach (var field in ScalarFields()) baseline.Add(field.Name, field.GetValue(settings)!);
        baseline.Add("DeniedStorage", string.Join("\n", (List<string>)Field("DeniedStorage").GetValue(settings)!));
        EndToEndAssert.Equal(13, baseline.Count, "The inspected fixture settings shape changed.");
        originalOnTheWay = (bool)baseline["OnTheWay"];
        recorder = new(new SettingsPage("native-pilot-page", "native-pilot-open", mod.SettingsCategory(),
            mod.Content.Name, null, mod.Content.PackageIdPlayerFacing), "root");
        var speed = Find.TickManager.CurTimeSpeed;
        patches = new Harmony(PatchOwner);
        context.DeferCleanup(() =>
        {
            // Failure cleanup only; successful execution restores the option through the native checkbox.
            observing = false; active = null;
            patches.UnpatchAll(PatchOwner);
            foreach (var field in ScalarFields()) field.SetValue(settings, baseline[field.Name]);
            var denied = (List<string>)Field("DeniedStorage").GetValue(settings)!;
            EndToEndAssert.Equal((string)baseline["DeniedStorage"], string.Join("\n", denied));
            foreach (var dialog in ownedWindows)
                if (Find.WindowStack.Windows.Contains(dialog)) dialog.Close(false);
            if (consoleRequested)
                foreach (var log in Find.WindowStack.Windows.OfType<EditWindow_Log>().ToArray()) log.Close(false);
            Find.TickManager.CurTimeSpeed = speed;
        });
        patches.Patch(AccessTools.Method(typeof(Dialog_ModSettings), nameof(Dialog_ModSettings.DoWindowContents)),
            transpiler: new HarmonyMethod(typeof(NativeSettingsRecorderTests), nameof(SurroundPageDraw)));
        patches.Patch(AccessTools.Method(typeof(TooltipHandler), nameof(TooltipHandler.TipRegion),
            new[] { typeof(Rect), typeof(TipSignal) }),
            prefix: new HarmonyMethod(typeof(NativeSettingsRecorderTests), nameof(ObserveLiteralTip)));
        active = this;
        Find.TickManager.Pause();
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        foreach (var step in Open()) yield return step;
        yield return new AssertionStep("native initial values before recorder activation", _ => AssertValues(originalOnTheWay));
        yield return Shot("unfamiliar native page before recording");
        yield return new AssertionStep("enable passive recorder for subsequent ordinary draws", _ => observing = true);
        yield return Wait("complete at least two native repaint callbacks", () => repaintCalls >= 2);
        yield return new CheckpointStep("publish readable native text and help without settings changes", _ =>
        {
            AssertValues(originalOnTheWay);
            var snapshot = RequireCapture();
            EndToEndAssert.True(snapshot.Records.Any(r => r.Kind == SettingsCaptureKind.Text && r.Text == "Haul on the way"));
            EndToEndAssert.True(snapshot.Records.Any(r => r.Kind == SettingsCaptureKind.Tooltip &&
                r.Text.Contains("bleeding", StringComparison.OrdinalIgnoreCase)), "Literal help was not captured from the real page.");
            return CaptureEvidence(snapshot);
        });
        yield return Shot("same native settings after passive recording");

        foreach (var probe in new[] { Probe.ThrowAfterDraw, Probe.ReplaceBackend, Probe.NestedCapture })
        {
            int expected = failureProbes + 1;
            yield return new AssertionStep("arm one recorder cleanup diagnostic: " + probe, _ => nextProbe = probe);
            yield return Wait("observe recorder diagnostic without losing native page", () => failureProbes == expected);
            yield return new AssertionStep("failed callback or replaced backend did not edit settings", _ => AssertValues(originalOnTheWay));
        }
        yield return Wait("recover ordinary complete capture after diagnostic", () => recorder.LastCapture?.Status == SettingsCaptureStatus.Partial);
        yield return Shot("native page remains usable after recorder cleanup diagnostics");

        yield return CaptureWindow();
        yield return ClickObservedLabel("intentionally toggle the original Haul on the way checkbox", "Haul on the way");
        yield return new AssertionStep("only the deliberate native checkbox edit changed", _ => AssertValues(!originalOnTheWay));
        yield return Shot("original native checkbox changed by the player action");
        foreach (var step in Close()) yield return step;
        yield return new CheckpointStep("normal native close persisted the foreign option", _ => PersistedValue(!originalOnTheWay));
        foreach (var step in Open()) yield return step;
        yield return new AssertionStep("reopened original page retains its own edited value", _ => AssertValues(!originalOnTheWay));
        yield return Shot("reopened native page retains the edited checkbox");
        yield return ClickObservedLabel("restore original option through the native checkbox", "Haul on the way");
        yield return new AssertionStep("all native option values restored", _ => AssertValues(originalOnTheWay));
        yield return Shot("original page values restored through native editing");
        foreach (var step in Close()) yield return step;
        yield return new CheckpointStep("restored option persisted through normal close", _ => PersistedValue(originalOnTheWay));
        consoleRequested = true;
        yield return new NativeDeveloperLogActionStep("open recorder workflow native console", true);
        yield return Shot("native console after recorder workflow");
        yield return new NativeDeveloperLogActionStep("close recorder workflow native console", false);
    }

    private IEnumerable<EndToEndStep> Open()
    {
        yield return new ModSettingsActionStep("open the actual unfamiliar native mod settings", PackageId);
        yield return new NativePointerScopeBeginStep("own native settings pointer", value => pointer = value);
        yield return CaptureWindow();
    }
    private IEnumerable<EndToEndStep> Close()
    {
        yield return new NativePointerScopeEndStep("restore native settings pointer", pointer);
        yield return new WindowCancelActionStep("close original settings through native cancel", typeof(Dialog_ModSettings).FullName!);
    }
    private NativeWindowCaptureStep CaptureWindow() => new("capture the actual current native page", windows =>
    {
        RequireGame();
        var actual = Find.WindowStack.Windows.OfType<Dialog_ModSettings>().Single();
        EndToEndAssert.True(ReferenceEquals(AccessTools.Field(typeof(Dialog_ModSettings), "mod").GetValue(actual), mod));
        if (!ownedWindows.Contains(actual)) ownedWindows.Add(actual);
        window = windows.Single(w => w.NativeId == actual.ID && w.RuntimeType == actual.GetType().FullName);
    });
    private NativeWindowClickActionStep ClickObservedLabel(string name, string label)
    {
        var bounds = RequireCapture().Records.Single(r => r.Kind == SettingsCaptureKind.Text && r.Text == label).Bounds;
        EndToEndAssert.True(bounds.X >= window.X + 18 && bounds.Y >= window.Y + 58 &&
            bounds.X + bounds.Width <= window.X + window.Width - 18 &&
            bounds.Y + bounds.Height < window.Y + window.Height - 60, "Observed label is not inside the visible native page.");
        return new(name, window.Handle, (bounds.X + bounds.Width / 2) * window.UiScale,
            (bounds.Y + bounds.Height / 2) * window.UiScale, null, pointer);
    }
    private SettingsCaptureSnapshot RequireCapture()
    {
        var capture = recorder.LastCapture;
        EndToEndAssert.NotNull(capture, "The complete real page callback did not publish any recorder observation.");
        EndToEndAssert.Equal(SettingsCaptureStatus.Partial, capture!.Status);
        EndToEndAssert.Equal(SettingsCaptureFailure.None, capture.Failure);
        return capture;
    }
    private IReadOnlyDictionary<string, string> CaptureEvidence(SettingsCaptureSnapshot snapshot) => new Dictionary<string, string>
    {
        ["package"] = mod.Content.PackageIdPlayerFacing, ["nativePageType"] = mod.GetType().FullName!,
        ["records"] = snapshot.Records.Count.ToString(), ["utf16Units"] = snapshot.Records.Sum(r => r.Text.Length).ToString(),
        ["status"] = snapshot.Status.ToString(), ["repaintCalls"] = repaintCalls.ToString(),
        ["capturedRepaints"] = capturedRepaints.ToString(),
        ["maximumObservedCallbackMilliseconds"] = (1000d * maxRecordingTicks / Stopwatch.Frequency).ToString(CultureInfo.InvariantCulture),
        ["scope"] = "ordinary real page callbacks only; automatic discovery, control identity and reveal remain unimplemented"
    };
    private FieldInfo Field(string name) => settings.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance)
        ?? throw new InvalidOperationException("Fixture setting shape changed: " + name);
    private FieldInfo[] ScalarFields() => settings.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
        .Where(f => f.FieldType == typeof(bool) || f.FieldType == typeof(int)).OrderBy(f => f.Name, StringComparer.Ordinal).ToArray();
    private void AssertValues(bool onTheWay)
    {
        RequireGame();
        foreach (var field in ScalarFields())
            EndToEndAssert.Equal(field.Name == "OnTheWay" ? onTheWay : baseline[field.Name], field.GetValue(settings)!, field.Name);
        EndToEndAssert.Equal((string)baseline["DeniedStorage"], string.Join("\n", (List<string>)Field("DeniedStorage").GetValue(settings)!));
    }
    private IReadOnlyDictionary<string, string> PersistedValue(bool expected)
    {
        var files = Directory.GetFiles(GenFilePaths.ConfigFolderPath, "Mod_*.xml")
            .Where(p => Path.GetFileName(p).Contains("PickUpAndHaul", StringComparison.OrdinalIgnoreCase)).ToArray();
        EndToEndAssert.Equal(1, files.Length, "Native close must write the exact foreign settings file.");
        var value = XDocument.Load(files[0]).Descendants("onTheWay").SingleOrDefault()?.Value;
        // The foreign Scribe default is true and can legitimately be omitted.
        EndToEndAssert.Equal(expected, value == null || bool.Parse(value));
        return new Dictionary<string, string> { ["nativeSettingsFile"] = Path.GetFileName(files[0]), ["onTheWay"] = expected.ToString() };
    }
    private void RequireGame() => EndToEndAssert.True(ReferenceEquals(Current.Game, game) && game.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting);
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private static WaitUntilStep Wait(string name, Func<bool> predicate) => new(name, _ => predicate(),
        new EndToEndDeadline(int.MaxValue, 120, TimeSpan.FromSeconds(15)));

    private static IEnumerable<CodeInstruction> SurroundPageDraw(IEnumerable<CodeInstruction> instructions)
    {
        var call = AccessTools.Method(typeof(Mod), nameof(Mod.DoSettingsWindowContents));
        var replacement = AccessTools.Method(typeof(NativeSettingsRecorderTests), nameof(DrawPage));
        int replaced = 0;
        foreach (var instruction in instructions)
        {
            if (instruction.Calls(call))
            {
                replaced++;
                var changed = new CodeInstruction(instruction) { opcode = OpCodes.Call, operand = replacement };
                yield return changed;
            }
            else yield return instruction;
        }
        if (replaced != 1) throw new InvalidOperationException("Expected exactly one native virtual settings-page callback.");
    }
    private static void ObserveLiteralTip(Rect __0, TipSignal __1) => SettingsPageRecorder.ObserveTooltip(__0, __1.text);
    private static void DrawPage(Mod page, Rect bounds)
    {
        var owner = active;
        if (owner == null || !owner.observing || !ReferenceEquals(owner.mod, page)) { page.DoSettingsWindowContents(bounds); return; }
        var previous = GuiBackend.Current;
        bool repaint = Event.current.type == EventType.Repaint;
        var probe = repaint ? owner.nextProbe : Probe.None;
        var sentinel = new InvalidOperationException("Owned recorder callback sentinel");
        IGuiBackend? replacement = null;
        SettingsPageRecorder? nestedCapture = null;
        var previousCapture = owner.recorder.LastCapture;
        int calls = 0;
        long started = Stopwatch.GetTimestamp();
        try
        {
            owner.recorder.Observe(() =>
            {
                calls++;
                if (probe == Probe.NestedCapture)
                {
                    nestedCapture = new SettingsPageRecorder(new SettingsPage("nested-probe", "unused", null, null, null), "root");
                    nestedCapture.Observe(() => page.DoSettingsWindowContents(bounds));
                }
                else page.DoSettingsWindowContents(bounds);
                EndToEndAssert.True(ReferenceEquals(previousCapture, owner.recorder.LastCapture),
                    "A still-running page callback published an unfinished capture.");
                if (probe == Probe.ThrowAfterDraw) throw sentinel;
                if (probe == Probe.ReplaceBackend)
                {
                    // Obtain a distinct real forwarding backend through a nested owned recorder.
                    // This callback only arranges the diagnostic; it never draws the page again.
                    var nested = new SettingsPageRecorder(new SettingsPage("replacement-probe", "unused", null, null, null), "root");
                    nested.Observe(() => replacement = GuiBackend.Current);
                    EndToEndAssert.True(replacement != null && !ReferenceEquals(replacement, previous) &&
                        !ReferenceEquals(replacement, GuiBackend.Current));
                    GuiBackend.Current = replacement!;
                }
            });
            EndToEndAssert.True(probe != Probe.ThrowAfterDraw, "Recorder swallowed the callback exception.");
        }
        catch (InvalidOperationException error) when (ReferenceEquals(error, sentinel) && probe == Probe.ThrowAfterDraw) { }
        finally
        {
            try
            {
                EndToEndAssert.Equal(1, calls, "Recorder must execute the ordinary native callback exactly once.");
                EndToEndAssert.True(ReferenceEquals(replacement ?? previous, GuiBackend.Current),
                    "Recorder overwrote a distinct replacement or failed to restore its own backend.");
            }
            finally
            {
                // The replacement is diagnostic-owned and must not escape this probe's GUI scope.
                if (replacement != null && ReferenceEquals(GuiBackend.Current, replacement)) GuiBackend.Current = previous;
            }
            if (repaint)
            {
                owner.repaintCalls++;
                owner.maxRecordingTicks = Math.Max(owner.maxRecordingTicks, Stopwatch.GetTimestamp() - started);
            }
        }
        if (!repaint) return;
        if (probe != Probe.None)
        {
            if (probe == Probe.NestedCapture)
            {
                EndToEndAssert.Equal(SettingsCaptureStatus.Partial, owner.recorder.LastCapture!.Status);
                EndToEndAssert.True(nestedCapture!.LastCapture!.Records.Any(r => r.Text == "Haul on the way"),
                    "The actual nested page did not receive its own observations.");
                EndToEndAssert.Equal(SettingsCaptureFailure.None, owner.recorder.LastCapture.Failure);
            }
            else
            {
                EndToEndAssert.Equal(SettingsCaptureStatus.Failed, owner.recorder.LastCapture!.Status);
                EndToEndAssert.Equal(probe == Probe.ThrowAfterDraw ? SettingsCaptureFailure.CallbackThrew : SettingsCaptureFailure.BackendReplaced,
                    owner.recorder.LastCapture.Failure);
            }
            EndToEndAssert.Equal(0, owner.recorder.LastCapture.Records.Count);
            owner.nextProbe = Probe.None; owner.failureProbes++;
        }
        else if (owner.recorder.LastCapture != null) owner.capturedRepaints++;
    }
}
