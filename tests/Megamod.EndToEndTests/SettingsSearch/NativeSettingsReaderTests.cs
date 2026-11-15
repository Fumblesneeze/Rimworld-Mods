using System.Reflection;
using System.Reflection.Emit;
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

[RimWorldEndToEndTest("megamod.settings-search.native-reader", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "Dreadarm.PickUpAndHaul", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 1200, MaxWallClockSeconds = 500)]
public sealed class NativeSettingsReaderTests : IRimWorldEndToEndTest
{
    private const string Package = "Dreadarm.PickUpAndHaul";
    private const string Owner = "fumblesneeze.megamod.e2e.settings-reader";
    private static NativeSettingsReaderTests? active;
    private readonly Dictionary<FieldInfo, object?> baseline = new();
    private readonly List<Window> windows = new();
    private Mod mod = null!;
    private object settings = null!;
    private string denied = null!;
    private string[] deniedItems = null!;
    private SettingsPageRecorder ordinary = null!;
    private ReaderWindow panel = null!;
    private EndToEndNativeWindow window = null!;
    private string pointer = null!;
    private bool original;
    private bool consoleRequested;

    public void Arrange(IEndToEndContext context)
    {
        EndToEndAssert.True(active == null && Current.Game.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting &&
            !Find.WindowStack.IsOpen<EditWindow_Log>());
        mod = LoadedModManager.ModHandles.Single(m => string.Equals(m.Content.PackageIdPlayerFacing, Package,
            StringComparison.OrdinalIgnoreCase));
        settings = mod.GetType().GetProperty("Settings", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        foreach (var field in settings.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance)
            .Where(f => f.FieldType == typeof(bool) || f.FieldType == typeof(int)))
            baseline.Add(field, field.GetValue(settings));
        EndToEndAssert.Equal(12, baseline.Count);
        deniedItems = ((List<string>)Field("DeniedStorage").GetValue(settings)!).ToArray();
        denied = string.Join("\n", deniedItems);
        original = (bool)Field("OnTheWay").GetValue(settings)!;
        var page = new SettingsPage("native-reader-puah", "native-reader-open", mod.SettingsCategory(),
            mod.Content.Name, null, Package);
        ordinary = new(page, "ordinary");
        var patches = new Harmony(Owner);
        var speed = Find.TickManager.CurTimeSpeed;
        panel = new(mod, page, () => (bool)Field("OnTheWay").GetValue(settings)!);
        context.DeferCleanup(() =>
        {
            active = null;
            var failures = new List<Exception>();
            void Restore(Action action) { try { action(); } catch (Exception error) { failures.Add(error); } }
            Restore(panel.DisposeReader);
            Restore(() => patches.UnpatchAll(Owner));
            foreach (var pair in baseline) Restore(() => pair.Key.SetValue(settings, pair.Value));
            Restore(() => { var list = (List<string>)Field("DeniedStorage").GetValue(settings)!; list.Clear(); list.AddRange(deniedItems); });
            if (panel.BlockedWindow != null) windows.Add(panel.BlockedWindow);
            if (consoleRequested) windows.AddRange(Find.WindowStack.Windows.OfType<EditWindow_Log>());
            foreach (var owned in windows.ToArray()) Restore(() => { if (Find.WindowStack.Windows.Contains(owned)) owned.Close(false); });
            Restore(() => Find.TickManager.CurTimeSpeed = speed);
            if (failures.Count != 0) throw new AggregateException("Reader fixture cleanup failed.", failures);
        });
        patches.Patch(AccessTools.Method(typeof(Dialog_ModSettings), nameof(Dialog_ModSettings.DoWindowContents)),
            transpiler: new HarmonyMethod(typeof(NativeSettingsReaderTests), nameof(SurroundOrdinaryDraw)));
        active = this;
        Find.TickManager.Pause();
        windows.Add(panel);
        Find.WindowStack.Add(panel);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new NativePointerScopeBeginStep("own native reader pointer", value => pointer = value);
        yield return Capture<ReaderWindow>();
        yield return Shot("foreign page values before the integrated reader request");
        yield return Click("request an isolated read of the foreign page", 170, 80);
        yield return Wait("complete the requested foreign-page read", () => panel.Completed);
        yield return Shot("captured foreign label and help without drawing the hidden page");
        yield return new CheckpointStep("retain real label and help with every original option unchanged", _ =>
        {
            EndToEndAssert.True(panel.Failure == null, panel.Failure ?? "");
            EndToEndAssert.True(panel.FoundLabel && panel.FoundHelp && panel.Deferrals > 0);
            EndToEndAssert.Equal(1, panel.DrawCalls);
            AssertValues(original);
            return new Dictionary<string, string> { ["package"] = Package, ["captureStatus"] = panel.Capture!.Status.ToString(),
                ["records"] = panel.Capture.Records.Count.ToString(), ["deferredAttempts"] = panel.Deferrals.ToString(),
                ["actualForeignDraws"] = panel.DrawCalls.ToString() };
        });
        yield return Click("request a caught forbidden-action diagnostic", 500, 80);
        yield return Wait("finish failed-read isolation diagnostic", () => panel.ProbeCompleted);
        yield return Shot("failed read remains empty and native window remains usable");
        yield return new AssertionStep("a caught guard failure publishes nothing", _ =>
        {
            EndToEndAssert.True(panel.Failure == null, panel.Failure ?? "");
            EndToEndAssert.Equal(1, panel.ProbeCalls);
            EndToEndAssert.Equal("settings-read-window-add", panel.BlockedReason);
            EndToEndAssert.True(panel.BlockedWindow != null && !Find.WindowStack.Windows.Contains(panel.BlockedWindow));
            EndToEndAssert.Equal(4, panel.DiagnosticReads);
            EndToEndAssert.Equal(0, panel.NestedCalls);
            EndToEndAssert.True(panel.NestedRefused && panel.Recovered);
            EndToEndAssert.Equal(2, panel.DrawCalls);
            AssertValues(original);
        });
        yield return new WindowCancelActionStep("close reader panel through native cancel", typeof(ReaderWindow).FullName!);
        yield return new ModSettingsActionStep("open the original foreign settings page", Package);
        yield return Capture<Dialog_ModSettings>();
        yield return Wait("record the ordinary native option", () => ordinary.LastCapture?.Records.Any(r => r.Text == "Haul on the way") == true);
        yield return Shot("original settings page before deliberate editing");
        yield return ClickOption("deliberately edit the original Haul on the way checkbox");
        yield return new AssertionStep("only the deliberate native checkbox edit changes", _ => AssertValues(!original));
        yield return Shot("original checkbox changed by a native player action");
        yield return ClickOption("restore the original checkbox through native input");
        yield return new AssertionStep("all foreign options restored", _ => AssertValues(original));
        yield return Shot("original settings restored and still usable");
        yield return new NativePointerScopeEndStep("restore native reader pointer", pointer);
        yield return new WindowCancelActionStep("close original foreign page normally", typeof(Dialog_ModSettings).FullName!);
        consoleRequested = true;
        yield return new NativeDeveloperLogActionStep("open integrated reader native console", true);
        yield return Shot("native console after integrated reader and ordinary editing");
        yield return new NativeDeveloperLogActionStep("close integrated reader native console", false);
    }

    private NativeWindowCaptureStep Capture<T>() where T : Window => new("capture native " + typeof(T).Name, captured =>
    {
        var actual = Find.WindowStack.Windows.OfType<T>().Single();
        if (!windows.Contains(actual)) windows.Add(actual);
        window = captured.Single(w => w.NativeId == actual.ID && w.RuntimeType == actual.GetType().FullName);
    });
    private NativeWindowClickActionStep Click(string name, double x, double y) => new(name, window.Handle,
        (window.X + x) * window.UiScale, (window.Y + y) * window.UiScale, null, pointer);
    private NativeWindowClickActionStep ClickOption(string name)
    {
        var bounds = ordinary.LastCapture!.Records.Single(r => r.Kind == SettingsCaptureKind.Text && r.Text == "Haul on the way").Bounds;
        EndToEndAssert.True(bounds.X >= window.X && bounds.Y >= window.Y && bounds.Y + bounds.Height < window.Y + window.Height);
        return new(name, window.Handle, (bounds.X + bounds.Width / 2) * window.UiScale,
            (bounds.Y + bounds.Height / 2) * window.UiScale, null, pointer);
    }
    private FieldInfo Field(string name) => settings.GetType().GetField(name, BindingFlags.Public | BindingFlags.Instance)!;
    private void AssertValues(bool onTheWay)
    {
        foreach (var pair in baseline)
            EndToEndAssert.Equal(pair.Key.Name == "OnTheWay" ? onTheWay : pair.Value, pair.Key.GetValue(settings), pair.Key.Name);
        EndToEndAssert.Equal(denied, string.Join("\n", (List<string>)Field("DeniedStorage").GetValue(settings)!));
    }
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private static WaitUntilStep Wait(string name, Func<bool> condition) => new(name, _ => condition(),
        new EndToEndDeadline(int.MaxValue, 120, TimeSpan.FromSeconds(20)));

    private static IEnumerable<CodeInstruction> SurroundOrdinaryDraw(IEnumerable<CodeInstruction> instructions)
    {
        int count = 0;
        foreach (var instruction in instructions)
            if (instruction.Calls(AccessTools.Method(typeof(Mod), nameof(Mod.DoSettingsWindowContents))))
            {
                count++;
                yield return new CodeInstruction(instruction) { opcode = OpCodes.Call,
                    operand = AccessTools.Method(typeof(NativeSettingsReaderTests), nameof(DrawOrdinary)) };
            }
            else yield return instruction;
        if (count != 1) throw new InvalidOperationException("Expected one native mod settings draw.");
    }
    private static void DrawOrdinary(Mod mod, Rect rect)
    {
        if (active is { } owner && ReferenceEquals(owner.mod, mod)) owner.ordinary.Observe(() => mod.DoSettingsWindowContents(rect));
        else mod.DoSettingsWindowContents(rect);
    }

    private sealed class ReaderWindow : Window
    {
        private readonly Mod mod;
        private readonly SettingsPage page;
        private readonly Func<bool> value;
        private readonly SettingsNeutralReader reader;
        private bool requested, probe;
        private int probePhase;
        internal bool Completed, ProbeCompleted, FoundLabel, FoundHelp;
        internal bool NestedRefused, Recovered;
        internal int Deferrals, DrawCalls, ProbeCalls, DiagnosticReads, NestedCalls;
        internal string? Failure;
        internal string? BlockedReason;
        internal Window? BlockedWindow;
        internal SettingsCaptureSnapshot? Capture;
        public override Vector2 InitialSize => new(720, 330);
        internal ReaderWindow(Mod mod, SettingsPage page, Func<bool> value)
        {
            this.mod = mod; this.page = page; this.value = value;
            reader = new(message => Failure ??= message);
            doCloseX = true; closeOnCancel = true; absorbInputAroundWindow = true;
        }
        internal void DisposeReader() => reader.Dispose();
        public override void DoWindowContents(Rect rect)
        {
            // All visible widget/metric calls have returned; the enclosing Window callback is
            // still active. Hidden metrics never reenter a native DrawText/Layout operation.
            DrawPanel(rect);
            if (!requested && !probe) return;
            bool probing = probe;
            var originalEvent = Event.current;
            var originalBackend = GuiBackend.Current;
            var originalColor = GUI.color;
            var status = reader.Read(page, probing ? "failure-probe" : "root", () =>
            {
                if (probing && probePhase == 0)
                {
                    ProbeCalls++;
                    BlockedWindow = new Dialog_MessageBox("Forbidden reader window");
                    try { Find.WindowStack.Add(BlockedWindow); }
                    catch (InvalidOperationException error) { BlockedReason = error.Message; }
                    Widgets.Label(new Rect(0, 0, 300, 30), "Must never publish this failed observation");
                }
                else if (probing && probePhase == 1)
                {
                    GUI.color = Color.red;
                    Widgets.Label(new Rect(0, 0, 300, 30), "Discard this throwing callback");
                    throw new InvalidOperationException("Owned integrated reader exception probe");
                }
                else if (probing && probePhase == 2)
                {
                    var nestedStatus = reader.Read(page, "nested", () => NestedCalls++, out var nestedCapture, out string? nestedReason);
                    NestedRefused = nestedStatus == SettingsNeutralReadStatus.Failed && nestedCapture == null && nestedReason == "nested-settings-read";
                }
                else { DrawCalls++; Text.Font = GameFont.Small; mod.DoSettingsWindowContents(new Rect(0, 40, 864, 584)); }
            }, out var capture, out string? reason);
            if (!ReferenceEquals(Event.current, originalEvent) || !ReferenceEquals(GuiBackend.Current, originalBackend) ||
                SettingsReadScope.Current != null || GUI.color != originalColor)
                Failure = "Reader did not restore its exact Event/backend/color/scope.";
            if (status == SettingsNeutralReadStatus.Deferred) { Deferrals++; return; }
            if (probing)
            {
                DiagnosticReads++;
                if (probePhase < 3)
                {
                    string expected = probePhase == 0 ? "settings-read-window-add" : probePhase == 1 ? "settings-reader-callback-failed" : "nested-settings-read";
                    if (status != SettingsNeutralReadStatus.Failed || capture != null || reason != expected)
                        Failure = reason ?? "Failed capture was published.";
                    probePhase++;
                }
                else
                {
                    probe = false; ProbeCompleted = true;
                    Recovered = status == SettingsNeutralReadStatus.Captured && capture?.Records.Any(r => r.Text == "Haul on the way") == true;
                    if (!Recovered) Failure = reason ?? "Reader did not recover after failures.";
                }
            }
            else
            {
                requested = false; Completed = true; Capture = capture;
                if (status != SettingsNeutralReadStatus.Captured) Failure = reason ?? status.ToString();
                FoundLabel = capture?.Records.Any(r => r.Kind == SettingsCaptureKind.Text && r.Text == "Haul on the way") == true;
                FoundHelp = capture?.Records.Any(r => r.Kind == SettingsCaptureKind.Tooltip && r.Text.Contains("bleeding", StringComparison.OrdinalIgnoreCase)) == true;
            }
        }
        private void DrawPanel(Rect rect)
        {
            Widgets.Label(new Rect(0, 0, rect.width, 35), "Integrated reader — Pick Up And Haul");
            if (Widgets.ButtonText(new Rect(0, 44, 310, 36), "Read original page")) requested = true;
            if (Widgets.ButtonText(new Rect(328, 44, 330, 36), "Check failed-read isolation")) probe = true;
            Widgets.Label(new Rect(0, 100, rect.width, 145), Failure ?? (Completed
                ? $"Haul on the way found: {FoundLabel}\nBleeding help found: {FoundHelp}\nOriginal checkbox: {value()}\nFailed read rejected: {ProbeCompleted}"
                : $"Original checkbox: {value()}\nThe requested read must leave this panel unchanged."));
        }
    }
}
