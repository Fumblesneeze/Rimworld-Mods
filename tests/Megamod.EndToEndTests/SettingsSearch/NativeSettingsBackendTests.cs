using LudeonTK;
using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.SettingsSearch.Core;
using Megamod.Features.SettingsSearch.Runtime;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using UnityStubs.Gui;
using Verse;

namespace Megamod.EndToEndTests.SettingsSearch;

// Native component workflow, not automatic foreign-page discovery or a provider integration.
[RimWorldEndToEndTest("megamod.settings-search.native-backend", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 1200, MaxWallClockSeconds = 400)]
public sealed class NativeSettingsBackendTests : IRimWorldEndToEndTest
{
    private BackendWindow panel = null!;
    private EndToEndNativeWindow window = null!;
    private string pointer = null!;
    private bool consoleRequested;

    public void Arrange(IEndToEndContext context)
    {
        EndToEndAssert.True(Current.Game.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting &&
            LanguageDatabase.activeLanguage.folderName == "English" && !Find.WindowStack.IsOpen<EditWindow_Log>());
        var speed = Find.TickManager.CurTimeSpeed;
        panel = new BackendWindow();
        context.DeferCleanup(() =>
        {
            if (Find.WindowStack.Windows.Contains(panel)) panel.Close(false);
            if (consoleRequested)
                foreach (var log in Find.WindowStack.Windows.OfType<EditWindow_Log>().ToArray()) log.Close(false);
            Find.TickManager.CurTimeSpeed = speed;
        });
        Find.TickManager.Pause();
        Find.WindowStack.Add(panel);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new NativePointerScopeBeginStep("own hidden-backend workflow pointer", value => pointer = value);
        yield return Capture();
        yield return Shot("native panel before hidden rendering");
        yield return Click("request bounded hidden rendering through the native button", 160, 88);
        yield return new WaitUntilStep("complete hidden native label capture", _ => panel.Finished,
            new EndToEndDeadline(int.MaxValue, 120, TimeSpan.FromSeconds(15)));
        yield return new AssertionStep("hidden capture retains native metrics and label geometry", _ =>
        {
            EndToEndAssert.True(panel.Failure == null, panel.Failure ?? "");
            EndToEndAssert.True(!panel.Checked, "Hidden rendering must not change the real checkbox.");
        });
        yield return Shot("native panel remains unchanged after hidden label and magenta fill");
        yield return new CheckpointStep("stop hidden rendering after its screenshot has completed", _ =>
        {
            panel.Requested = false;
            EndToEndAssert.True(panel.Failure == null && panel.Captures >= 2 && panel.Captures <= 128,
                panel.Failure ?? "The rendering probe must remain active through screenshot capture.");
            return new Dictionary<string, string> { ["hiddenRepaintsThroughCapture"] = panel.Captures.ToString() };
        });
        yield return Capture();
        yield return Click("toggle the original native checkbox after hidden rendering", 160, 140);
        yield return new AssertionStep("native checkbox still responds to deliberate input", _ => EndToEndAssert.True(panel.Checked));
        yield return Shot("original native checkbox changes through real input");
        yield return new NativePointerScopeEndStep("restore hidden-backend workflow pointer", pointer);
        yield return new WindowCancelActionStep("close backend fixture through native cancel", typeof(BackendWindow).FullName!);
        consoleRequested = true;
        yield return new NativeDeveloperLogActionStep("open backend workflow native console", true);
        yield return Shot("native console after hidden-backend workflow");
        yield return new NativeDeveloperLogActionStep("close backend workflow native console", false);
    }

    private NativeWindowCaptureStep Capture() => new("capture exact native backend fixture", windows =>
        window = windows.Single(w => w.NativeId == panel.ID && w.RuntimeType == typeof(BackendWindow).FullName));
    private NativeWindowClickActionStep Click(string name, float x, float y) => new(name, window.Handle,
        (window.X + x) * window.UiScale, (window.Y + y) * window.UiScale, null, pointer);
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);

    private sealed class BackendWindow : Window
    {
        private const string HiddenText = "Hidden reader marker";
        internal bool Requested, Finished, Checked;
        internal int Captures;
        internal string? Failure;
        public override Vector2 InitialSize => new(620, 320);
        internal BackendWindow() { doCloseX = true; closeOnCancel = true; absorbInputAroundWindow = true; }

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new Rect(0, 0, inRect.width, 32), "Settings reader — hidden drawing verification");
            if (Widgets.ButtonText(new Rect(0, 50, 300, 36), "Capture hidden label")) Requested = true;
            Widgets.CheckboxLabeled(new Rect(0, 104, inRect.width, 32), "Original native option", ref Checked);
            Widgets.Label(new Rect(0, 160, inRect.width, 90), Failure ??
                (Finished ? "Capture returned. No hidden text or magenta fill should be visible." : "The native panel is ready."));
            if (!Requested || Event.current.type != EventType.Repaint) return;
            // ScreenshotStep reads a later rendered frame. Keep this bounded probe active until
            // the step finishes so a one-frame hidden-render leak cannot escape visual inspection.
            if (++Captures > 128)
            {
                Failure = "Hidden rendering exceeded its 128-repaint fixture budget.";
                Requested = false;
                Finished = true;
                return;
            }
            try { CaptureHidden(); }
            catch (Exception error) { Failure = error.ToString(); Requested = false; }
            finally { Finished = true; }
        }

        private void CaptureHidden()
        {
            // Each real metric call completes before the hidden callback starts; no layout reentry.
            var previousBackend = GuiBackend.Current;
            var previousEvent = Event.current;
            var previousMatrix = GUI.matrix;
            Vector2 expectedSize = Text.CalcSize(HiddenText);
            EndToEndAssert.True(expectedSize.x > 0 && expectedSize.y > 0);
            var hidden = new SettingsHiddenBackend(previousBackend);
            var neutral = new Event { type = EventType.Repaint, rawType = EventType.Repaint,
                mousePosition = new Vector2(-1000000, -1000000) };
            var recorder = new SettingsPageRecorder(new SettingsPage("backend-fixture", "backend-open",
                "Backend fixture", "Owned verification fixture", null, "fumblesneeze.megamod"), "root");
            using var state = SettingsGuiStateScope.Capture();
            try
            {
                GuiBackend.Current = hidden;
                Event.current = neutral;
                state.Isolate();
                GUI.matrix = Matrix4x4.identity;
                EndToEndAssert.Equal(expectedSize, Text.CalcSize(HiddenText), "Hidden text metrics must match the real native backend.");
                recorder.Observe(() =>
                {
                    var transform = Matrix4x4.identity;
                    transform.m00 = transform.m11 = 1.5f;
                    GUI.matrix = transform;
                    GUI.BeginGroup(new Rect(.4f, .4f, 400, 200));
                    GUI.BeginGroup(new Rect(.4f, .4f, 300, 150));
                    EndToEndAssert.Equal(2, hidden.GroupCount);
                    hidden.GetGroup(1, out _, out var origin);
                    EndToEndAssert.True(Math.Abs(origin.x - 4f / 3) < .001f && Math.Abs(origin.y - 4f / 3) < .001f,
                        "Fractional nested groups must snap each origin.");
                    Widgets.DrawBoxSolid(new Rect(0, 0, 300, 150), Color.magenta);
                    Widgets.Label(new Rect(10, 6, 200, 30), HiddenText);
                    GUI.EndGroup();
                    GUI.EndGroup();
                });
                EndToEndAssert.True(!hidden.Failed && hidden.IsBalanced, hidden.FailureReason ?? "Hidden structures did not balance.");
                var capture = recorder.LastCapture;
                EndToEndAssert.NotNull(capture);
                EndToEndAssert.Equal(SettingsCaptureStatus.Partial, capture!.Status);
                var label = capture.Records.Single(r => r.Kind == SettingsCaptureKind.Text && r.Text == HiddenText);
                EndToEndAssert.True(float.IsFinite(label.Bounds.X) && float.IsFinite(label.Bounds.Y) &&
                    label.Bounds.Width > 0 && label.Bounds.Height > 0, "The native label needs usable recorded bounds.");
            }
            finally
            {
                try { state.Dispose(); }
                finally
                {
                    if (ReferenceEquals(GuiBackend.Current, hidden)) GuiBackend.Current = previousBackend;
                    if (ReferenceEquals(Event.current, neutral)) Event.current = previousEvent;
                }
            }
            EndToEndAssert.True(ReferenceEquals(GuiBackend.Current, previousBackend) && ReferenceEquals(Event.current, previousEvent));
            EndToEndAssert.Equal(previousMatrix, GUI.matrix);
        }
    }
}
