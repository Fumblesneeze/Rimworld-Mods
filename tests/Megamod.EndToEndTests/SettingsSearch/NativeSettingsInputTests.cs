using System.Reflection;
using LudeonTK;
using Megamod.Features.SettingsSearch.Runtime;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.SettingsSearch;

[RimWorldEndToEndTest("megamod.settings-search.native-input", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 1200, MaxWallClockSeconds = 450)]
public sealed class NativeSettingsInputTests : IRimWorldEndToEndTest
{
    private InputWindow panel = null!;
    private EndToEndNativeWindow window = null!;
    private string pointer = null!;
    private int keyboardBefore, pointerBefore, idleAfter;
    private bool consoleRequested;
    private Task<(SettingsReadInputStatus Status, bool Rejected)>? workerDiagnostics;

    public void Arrange(IEndToEndContext context)
    {
        EndToEndAssert.True(Current.Game.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting &&
            LanguageDatabase.activeLanguage.folderName == "English" && !Find.WindowStack.IsOpen<EditWindow_Log>());
        var speed = Find.TickManager.CurTimeSpeed;
        panel = new InputWindow();
        context.DeferCleanup(() => Find.TickManager.CurTimeSpeed = speed);
        context.DeferCleanup(() =>
        {
            if (consoleRequested) Find.WindowStack.Windows.OfType<EditWindow_Log>().FirstOrDefault()?.Close(false);
        });
        context.DeferCleanup(() => { if (Find.WindowStack.Windows.Contains(panel)) panel.Close(false); });
        Find.TickManager.Pause();
        Find.WindowStack.Add(panel);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new NativePointerScopeBeginStep("own input admission pointer", value => pointer = value);
        yield return Capture();
        yield return Wait("initialize actual native input admission", () => panel.IdleFrames >= 2);
        yield return Shot("idle native controls before input admission checks");
        yield return new NativeWindowClickActionStep("focus the original native input field", window.Handle,
            (window.X + 140) * window.UiScale, (window.Y + 104) * window.UiScale, null, pointer);
        yield return Wait("focused but idle editor remains eligible", () => panel.FocusedIdleFrames >= 2);
        yield return Shot("native focus remains eligible without editing text");
        yield return new AssertionStep("retain keyboard counter before actual typing", _ => keyboardBefore = panel.KeyboardBusyFrames);
        yield return Capture();
        yield return new NativeWindowReplaceTextActionStep("type while admission observes actual key edges", window.Handle,
            window.FocusedTextControlHandle!, "typed while discovery waits");
        yield return Wait("observe native typing and blocked keyboard repaints", () =>
            panel.Value == "typed while discovery waits" && panel.KeyboardBusyFrames > keyboardBefore);
        yield return new AssertionStep("mark idle observation after typing", _ => idleAfter = panel.FocusedIdleFrames);
        yield return Wait("resume eligibility with text focus retained", () => panel.FocusedIdleFrames > idleAfter);
        yield return Shot("native typing succeeds and focused idle admission resumes");
        yield return new AssertionStep("retain pointer counter before actual dragging", _ => pointerBefore = panel.PointerBusyFrames);
        yield return Capture();
        yield return new NativeWindowDragActionStep("drag the original slider while admission observes input", window.Handle,
            (window.X + 24 + 488 * panel.Slider) * window.UiScale, (window.Y + 160) * window.UiScale,
            (window.X + 480) * window.UiScale, (window.Y + 161) * window.UiScale, null, pointer);
        yield return Wait("observe slider edit and blocked pointer repaints", () => panel.Slider > .7f && panel.PointerBusyFrames > pointerBefore);
        yield return new AssertionStep("mark idle observation after dragging", _ => idleAfter = panel.IdleFrames);
        yield return Wait("resume idle admission after pointer release", () => panel.IdleFrames > idleAfter);
        yield return Shot("native slider edit survives idle admission observation");
        yield return new AssertionStep("schedule bounded worker diagnostics", _ => workerDiagnostics = panel.BeginWorkerDiagnostics());
        yield return Wait("observe worker completion without blocking the GUI", () => workerDiagnostics!.IsCompleted);
        yield return new AssertionStep("refuse worker reads and construction", _ =>
        {
            var result = workerDiagnostics!.GetAwaiter().GetResult(); // Already completed by the bounded wait.
            EndToEndAssert.Equal(SettingsReadInputStatus.Unavailable, result.Status);
            EndToEndAssert.True(result.Rejected);
        });
        yield return new AssertionStep("request bounded input binding diagnostics", _ => panel.DiagnosticsRequested = true);
        yield return Wait("complete owned thread device and empty IME diagnostics", () => panel.DiagnosticsDone);
        yield return new NativePointerScopeEndStep("restore input admission pointer", pointer);
        yield return new WindowCancelActionStep("close input admission panel through native cancel", typeof(InputWindow).FullName!);
        consoleRequested = true;
        yield return new NativeDeveloperLogActionStep("open input admission native console", true);
        yield return Shot("native console after input admission workflow");
        yield return new NativeDeveloperLogActionStep("close input admission native console", false);
    }

    private NativeWindowCaptureStep Capture() => new("capture exact input admission panel", windows =>
        window = windows.Single(value => value.NativeId == panel.ID && value.RuntimeType == typeof(InputWindow).FullName));
    private WaitUntilStep Wait(string name, Func<bool> condition) => new(name, _ =>
    {
        EndToEndAssert.True(panel.Failure == null, panel.Failure ?? "");
        return condition();
    }, new EndToEndDeadline(int.MaxValue, 120, TimeSpan.FromSeconds(15)));
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);

    private sealed class InputWindow : Window
    {
        private SettingsReadInputAdmission? admission;
        private readonly Func<KeyCode, bool> keyHeld = KeyReader("GetKeyRaw");
        private readonly Func<KeyCode, bool> keyPressed = KeyReader("GetKeyDownRaw");
        private readonly Func<KeyCode, bool> keyReleased = KeyReader("GetKeyUpRaw");
        private static readonly KeyCode[] ExercisedKeys = { KeyCode.LeftControl, KeyCode.A, KeyCode.Backspace };
        internal string Value = "original focused text";
        internal float Slider = .3737f;
        internal int IdleFrames, FocusedIdleFrames, KeyboardBusyFrames, PointerBusyFrames;
        internal string? Failure;
        internal bool DiagnosticsRequested, DiagnosticsDone;
        public override Vector2 InitialSize => new(700, 360);
        internal InputWindow() { doCloseX = true; closeOnCancel = true; absorbInputAroundWindow = true; }
        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new Rect(0, 0, inRect.width, 32), "Settings reader — native input admission");
            GUI.SetNextControlName("settings-input-original");
            Value = Widgets.TextField(new Rect(0, 70, inRect.width, 32), Value);
            Slider = Widgets.HorizontalSlider(new Rect(0, 126, 500, 30), Slider, 0, 1, middleAlignment: true);
            Widgets.Label(new Rect(510, 126, 140, 30), Slider.ToString("0.0000"));
            Widgets.Label(new Rect(0, 195, inRect.width, 110), Failure ??
                $"Idle observations: {IdleFrames}; focused idle: {FocusedIdleFrames}\nKeyboard waits: {KeyboardBusyFrames}; pointer waits: {PointerBusyFrames}");
            if (Failure != null || Event.current.type != EventType.Repaint) return;
            try
            {
                admission ??= new SettingsReadInputAdmission();
                // Independent native APIs witness the actually exercised keys/gesture. Character-only
                // delivery that has completed before Repaint need not leave an active key behind.
                bool keyboardActive = ExercisedKeys.Any(key => keyHeld(key) || keyPressed(key) || keyReleased(key));
                var movement = UnityEngine.InputSystem.Mouse.current.delta.ReadValue();
                bool pointerActive = Input.GetMouseButton(0) || Input.GetMouseButtonDown(0) || Input.GetMouseButtonUp(0) ||
                    movement.x != 0 || movement.y != 0 || Event.current.delta.x != 0 || Event.current.delta.y != 0;
                var status = admission.Check(out string? reason);
                EndToEndAssert.True(status != SettingsReadInputStatus.Unavailable, reason ?? "Native input state unavailable.");
                EndToEndAssert.True(!(keyboardActive || pointerActive) || status == SettingsReadInputStatus.Busy,
                    "A witnessed active native input repaint must never be admitted as idle.");
                if (status == SettingsReadInputStatus.Idle)
                {
                    IdleFrames++;
                    if (GUI.GetNameOfFocusedControl() == "settings-input-original") FocusedIdleFrames++;
                    if (DiagnosticsRequested)
                    {
                        DiagnosticsRequested = false;
                        DiagnoseBindings();
                        DiagnosticsDone = true;
                    }
                }
                if (keyboardActive && status == SettingsReadInputStatus.Busy) KeyboardBusyFrames++;
                if (pointerActive && status == SettingsReadInputStatus.Busy) PointerBusyFrames++;
            }
            catch (Exception error) { Failure = error.ToString(); }
        }

        private static Func<KeyCode, bool> KeyReader(string name) =>
            typeof(Input).GetMethod(name, BindingFlags.NonPublic | BindingFlags.Static, null, new[] { typeof(KeyCode) }, null)!
                .CreateDelegate<Func<KeyCode, bool>>();

        internal Task<(SettingsReadInputStatus Status, bool Rejected)> BeginWorkerDiagnostics()
        {
            var current = admission!;
            return Task.Run(() =>
            {
                var status = current.Check(out _);
                bool rejected = false;
                try { _ = new SettingsReadInputAdmission(); }
                catch (InvalidOperationException) { rejected = true; }
                return (status, rejected);
            });
        }

        private void DiagnoseBindings()
        {
            // Supporting binding checks only; no simulated field mutation is native input proof.
            var native = typeof(GUIUtility).Assembly;
            var keyboard = native.GetType("UnityEngine.InputSystem.InputRuntime", true)!.GetField("_keyboard", BindingFlags.Static | BindingFlags.NonPublic)!;
            var originalKeyboard = keyboard.GetValue(null);
            try
            {
                keyboard.SetValue(null, null);
                EndToEndAssert.Equal(SettingsReadInputStatus.Unavailable, admission!.Check(out _));
            }
            finally { keyboard.SetValue(null, originalKeyboard); }
            var ime = AppDomain.CurrentDomain.GetAssemblies().Single(assembly => assembly.GetName().Name == "RimWorld")
                .GetType("RimWorldWaylith.WindowsImeBridge", true)!;
            var bridge = ime.GetField("Current", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            var composing = ime.GetField("composing", BindingFlags.NonPublic | BindingFlags.Instance)!;
            var composition = native.GetType("UnityStubs.Internal.ImeComposition", true)!
                .GetField("<Text>k__BackingField", BindingFlags.NonPublic | BindingFlags.Static)!;
            EndToEndAssert.Equal("", (string)composition.GetValue(null)!);
            bool originalComposing = (bool)composing.GetValue(bridge)!;
            try
            {
                composing.SetValue(bridge, true);
                EndToEndAssert.Equal(SettingsReadInputStatus.Busy, admission!.Check(out string? reason));
                EndToEndAssert.Equal("settings-input-ime-active", reason);
            }
            finally { composing.SetValue(bridge, originalComposing); }
            EndToEndAssert.Equal(SettingsReadInputStatus.Idle, admission!.Check(out _));
        }
    }
}
