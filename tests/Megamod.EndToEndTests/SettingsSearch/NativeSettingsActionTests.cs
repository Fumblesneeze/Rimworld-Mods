using System.Reflection;
using HarmonyLib;
using LudeonTK;
using Megamod.Features.SettingsSearch.Runtime;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.SettingsSearch;

[RimWorldEndToEndTest("megamod.settings-search.native-actions", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 1200, MaxWallClockSeconds = 400)]
public sealed class NativeSettingsActionTests : IRimWorldEndToEndTest
{
    private const string BackstopOwner = "fumblesneeze.megamod.e2e.clipboard-backstop";
    private static int backstopCalls;
    private ActionWindow panel = null!;
    private EndToEndNativeWindow window = null!;
    private string pointer = null!;
    private bool consoleRequested;

    public void Arrange(IEndToEndContext context)
    {
        EndToEndAssert.True(Current.Game.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting &&
            LanguageDatabase.activeLanguage.folderName == "English" && !Find.WindowStack.IsOpen<EditWindow_Log>());
        var speed = Find.TickManager.CurTimeSpeed;
        var guards = new SettingsReadActionGuards(message => throw new InvalidOperationException(message));
        var clipboard = typeof(GUIUtility).GetProperty(nameof(GUIUtility.systemCopyBuffer))!.SetMethod!;
        var backstop = new HarmonyLib.Harmony(BackstopOwner);
        var extra = new SentinelWindow();
        string path = (string)typeof(LoadedModManager).GetMethod("GetSettingsFilename", BindingFlags.NonPublic | BindingFlags.Static)!
            .Invoke(null, new object[] { "megamod-reader-e2e", "action-guard" })!;
        EndToEndAssert.True(!File.Exists(path), "The settings fixture must own a new disposable file.");
        panel = new ActionWindow(guards, extra, path);
        context.DeferCleanup(() =>
        {
            try { guards.Dispose(); }
            finally
            {
                backstop.Unpatch(clipboard, HarmonyPatchType.Prefix, BackstopOwner);
                if (Find.WindowStack.Windows.Contains(extra)) extra.Close(false);
                if (Find.WindowStack.Windows.Contains(panel)) panel.Close(false);
                if (File.Exists(path)) File.Delete(path);
                if (consoleRequested)
                    foreach (var log in Find.WindowStack.Windows.OfType<EditWindow_Log>().ToArray()) log.Close(false);
                Find.TickManager.CurTimeSpeed = speed;
            }
        });
        backstopCalls = 0;
        backstop.Patch(clipboard, prefix: new HarmonyMethod(typeof(NativeSettingsActionTests), nameof(ClipboardBackstop))
            { priority = int.MinValue, after = new[] { "fumblesneeze.megamod" } });
        EndToEndAssert.True(guards.Initialize() && guards.Available, "All native action guards must admit together.");
        LoadedModManager.WriteModSettings("megamod-reader-e2e", "action-guard", new ProbeSettings());
        panel.Baseline = File.ReadAllBytes(path);
        Find.TickManager.Pause();
        Find.WindowStack.Add(panel);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new NativePointerScopeBeginStep("own action guard workflow pointer", value => pointer = value);
        yield return Capture();
        yield return Shot("native panel before guarded read attempts");
        yield return Click("request guarded native actions through the real button", 170, 88);
        yield return new WaitUntilStep("complete guarded native action attempts", _ => panel.Finished,
            new EndToEndDeadline(int.MaxValue, 120, TimeSpan.FromSeconds(15)));
        yield return new AssertionStep("all six boundaries fail without native effects", _ =>
        {
            EndToEndAssert.True(panel.Failure == null, panel.Failure ?? "");
            EndToEndAssert.Equal(6, panel.Blocked);
            EndToEndAssert.Equal(0, backstopCalls, "The production prefix must stop before the clipboard backstop.");
            EndToEndAssert.True(!panel.Checked && Find.WindowStack.Windows.Contains(panel));
        });
        yield return Shot("same panel and unchanged option after six refused actions");
        yield return Capture();
        yield return Click("edit original native checkbox after read refusals", 170, 140);
        yield return new AssertionStep("normal editing works after failed reads", _ => EndToEndAssert.True(panel.Checked));
        yield return Shot("original native checkbox changes after guarded reads");
        yield return new NativePointerScopeEndStep("restore action guard workflow pointer", pointer);
        yield return new WindowCancelActionStep("close guarded fixture through native cancel", typeof(ActionWindow).FullName!);
        consoleRequested = true;
        yield return new NativeDeveloperLogActionStep("open action guard workflow native console", true);
        yield return Shot("native console after action guard workflow");
        yield return new NativeDeveloperLogActionStep("close action guard workflow native console", false);
    }

    private static void ClipboardBackstop()
    {
        backstopCalls++;
        throw new InvalidOperationException("Test backstop: OS clipboard writes are forbidden in this fixture.");
    }
    private NativeWindowCaptureStep Capture() => new("capture exact native action fixture", windows =>
        window = windows.Single(w => w.NativeId == panel.ID && w.RuntimeType == typeof(ActionWindow).FullName));
    private NativeWindowClickActionStep Click(string name, float x, float y) => new(name, window.Handle,
        (window.X + x) * window.UiScale, (window.Y + y) * window.UiScale, null, pointer);
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);

    public sealed class ProbeSettings : ModSettings
    {
        public int Value = 17;
        public override void ExposeData() => Scribe_Values.Look(ref Value, "value", 0);
    }

    private sealed class SentinelWindow : Window
    {
        public override Vector2 InitialSize => new(260, 120);
        internal SentinelWindow() { soundAppear = null; soundClose = null; }
        public override void DoWindowContents(Rect inRect) => Widgets.Label(inRect, "Unexpected reader window");
    }

    private sealed class ActionWindow : Window
    {
        private readonly SettingsReadActionGuards guards;
        private readonly Window extra;
        private readonly string path;
        internal byte[] Baseline = Array.Empty<byte>();
        internal bool Requested, Finished, Checked;
        internal int Blocked;
        internal string? Failure;
        public override Vector2 InitialSize => new(660, 330);
        internal ActionWindow(SettingsReadActionGuards guards, Window extra, string path)
        {
            this.guards = guards; this.extra = extra; this.path = path;
            doCloseX = true; closeOnCancel = true; absorbInputAroundWindow = true;
        }
        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new Rect(0, 0, inRect.width, 32), "Settings reader — native action guards");
            if (Widgets.ButtonText(new Rect(0, 50, 320, 36), "Attempt actions during a read")) Requested = true;
            Widgets.CheckboxLabeled(new Rect(0, 104, inRect.width, 32), "Original native option", ref Checked);
            Widgets.Label(new Rect(0, 160, inRect.width, 110), Failure ?? (Finished
                ? $"{Blocked} actions refused. The panel, saved fixture and option are unchanged."
                : "Ready. Reading must not open or close windows or save settings."));
            if (!Requested || Finished || Event.current.type != EventType.Repaint) return;
            try { AttemptAll(); }
            catch (Exception error) { Failure = error.ToString(); }
            finally { Finished = true; }
        }

        private void AttemptAll()
        {
            var before = Find.WindowStack.Windows.ToArray();
            // RED stops on this first harmless disposable action before any later effects.
            Refused(() => Find.WindowStack.Add(extra));
            Refused(() => Find.WindowStack.ImmediateWindow(-1800141, new Rect(12, 12, 220, 100), WindowLayer.Dialog,
                () => throw new InvalidOperationException("Hidden immediate window callback executed.")));
            Refused(() => Find.WindowStack.TryRemove(this, false));
            Refused(() => LoadedModManager.WriteModSettings("megamod-reader-e2e", "action-guard", new ProbeSettings { Value = 29 }));
            Refused(() => GUIUtility.systemCopyBuffer = "Reader fixture marker");
            var ime = typeof(GUIUtility).Assembly.GetType("UnityStubs.Internal.ImeComposition", true)!;
            var text = ime.GetProperty("Text")!;
            var cursor = ime.GetProperty("Cursor")!;
            object? priorText = text.GetValue(null), priorCursor = cursor.GetValue(null);
            Refused(() => ime.GetMethod("Set", BindingFlags.Public | BindingFlags.Static)!.Invoke(null, new object[] { "reader marker", 1 }));
            EndToEndAssert.Equal(priorText, text.GetValue(null));
            EndToEndAssert.Equal(priorCursor, cursor.GetValue(null));
            EndToEndAssert.True(before.SequenceEqual(Find.WindowStack.Windows), "Guarded calls must leave the native window collection unchanged.");
            EndToEndAssert.True(Baseline.SequenceEqual(File.ReadAllBytes(path)), "Guarded saving must leave the disposable file unchanged.");
        }

        private void Refused(Action action)
        {
            using var scope = guards.BeginRead();
            bool threw = false;
            try { action(); }
            catch (Exception error) when (error.GetBaseException() is InvalidOperationException) { threw = true; }
            EndToEndAssert.True(threw && scope.Failed && !string.IsNullOrEmpty(scope.FailureReason),
                "A forbidden native action must throw and retain failed status even when caught.");
            Blocked++;
        }
    }
}
