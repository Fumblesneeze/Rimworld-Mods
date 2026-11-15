using System.Collections;
using System.Reflection;
using HarmonyLib;
using LudeonTK;
using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.SettingsSearch.Core;
using Megamod.Features.SettingsSearch.Runtime;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using UnityStubs.Gui;
using Verse;

namespace Megamod.EndToEndTests.SettingsSearch;

[RimWorldEndToEndTest("megamod.settings-search.native-controls", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 1200, MaxWallClockSeconds = 450)]
public sealed class NativeSettingsControlTests : IRimWorldEndToEndTest
{
    private const string ProbeOwner = "fumblesneeze.megamod.e2e.control-probes";
    private static bool throwAfterVector;
    private static bool throwAfterText;
    private ControlWindow panel = null!;
    private EndToEndNativeWindow window = null!;
    private string pointer = null!;
    private bool consoleRequested;
    private HarmonyLib.Harmony probes = null!;
    private MethodInfo integerTarget = null!;

    public void Arrange(IEndToEndContext context)
    {
        EndToEndAssert.True(Current.Game.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting &&
            LanguageDatabase.activeLanguage.folderName == "English" && !Find.WindowStack.IsOpen<EditWindow_Log>());
        var speed = Find.TickManager.CurTimeSpeed;
        var actions = new SettingsReadActionGuards(message => Log.Warning(message));
        var controls = new SettingsReadControlGuards(message => Log.Warning(message));
        probes = new HarmonyLib.Harmony(ProbeOwner);
        integerTarget = typeof(Widgets).GetMethods().Single(m => m.Name == nameof(Widgets.TextFieldNumeric) && m.IsGenericMethodDefinition)
            .MakeGenericMethod(typeof(int));
        var vectorTarget = typeof(Widgets).GetMethod(nameof(Widgets.TextFieldVector))!;
        var limitedTextTarget = typeof(Widgets).GetMethod(nameof(Widgets.TextField), new[]
            { typeof(Rect), typeof(string), typeof(int), typeof(System.Text.RegularExpressions.Regex) })!;
        panel = new ControlWindow(actions, controls);
        context.DeferCleanup(() =>
        {
            try { controls.Dispose(); }
            finally
            {
                try { actions.Dispose(); }
                finally
                {
                    throwAfterVector = false;
                    throwAfterText = false;
                    probes.Unpatch(integerTarget, HarmonyPatchType.Postfix, ProbeOwner);
                    probes.Unpatch(vectorTarget, HarmonyPatchType.Postfix, ProbeOwner);
                    probes.Unpatch(limitedTextTarget, HarmonyPatchType.Postfix, ProbeOwner);
                    if (Find.WindowStack.Windows.Contains(panel)) panel.Close(false);
                    if (consoleRequested)
                        foreach (var log in Find.WindowStack.Windows.OfType<EditWindow_Log>().ToArray()) log.Close(false);
                    Find.TickManager.CurTimeSpeed = speed;
                }
            }
        });
        probes.Patch(vectorTarget, postfix: new HarmonyMethod(typeof(NativeSettingsControlTests), nameof(VectorPostfix)));
        probes.Patch(limitedTextTarget, postfix: new HarmonyMethod(typeof(NativeSettingsControlTests), nameof(TextPostfix)));
        EndToEndAssert.True(actions.Initialize() && controls.Initialize(), "All action/control guards must admit.");
        Find.TickManager.Pause();
        Find.WindowStack.Add(panel);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new NativePointerScopeBeginStep("own control preservation pointer", value => pointer = value);
        yield return Capture();
        yield return Shot("original native text and slider before automatic control reads");
        yield return Click("request native control reads through the button", 170, 88);
        yield return CompleteProbe(1);
        yield return AssertProbe();
        yield return Shot("original native text and slider retained after guarded reads");
        yield return new AssertionStep("arrange an actual integer-closure postfix composition", _ =>
            probes.Patch(integerTarget, postfix: new HarmonyMethod(typeof(NativeSettingsControlTests), nameof(IntegerPostfix))));
        yield return new WaitUntilStep("observe integer composition without changing its float sibling", _ => panel.NativeInteger == 12345,
            new EndToEndDeadline(int.MaxValue, 120, TimeSpan.FromSeconds(15)));
        yield return new AssertionStep("actual native float closure remains independent", _ => SameBits(.33333f, panel.NativeFloat));
        yield return Capture();
        yield return Click("repeat native reads with the integer closure repatched", 170, 88);
        yield return CompleteProbe(2);
        yield return AssertProbe();
        yield return Shot("values retained under native integer closure composition");
        yield return new AssertionStep("remove only the fixture integer postfix and reset its disposable input", _ =>
        {
            probes.Unpatch(integerTarget, HarmonyPatchType.Postfix, ProbeOwner);
            panel.NativeInteger = 7; panel.IntegerBuffer = "7";
        });
        yield return new WaitUntilStep("observe the cleaned native integer and float pair", _ => panel.NativeInteger == 7 && panel.NativeFloat == .33333f,
            new EndToEndDeadline(int.MaxValue, 120, TimeSpan.FromSeconds(15)));
        yield return Capture();
        yield return Click("focus the original native text control", 170, 140);
        yield return Capture();
        yield return new NativeWindowReplaceTextActionStep("edit original text after hidden controls", window.Handle,
            window.FocusedTextControlHandle!, "after automatic reading");
        yield return new AssertionStep("ordinary native text still edits", _ => EndToEndAssert.Equal("after automatic reading", panel.Value));
        yield return Capture();
        // This native slider arms on mouse-down and changes only on an actual MouseDrag pass.
        yield return new NativeWindowDragActionStep("edit the original native slider after hidden controls", window.Handle,
            (window.X + 24 + 488 * panel.SliderValue) * window.UiScale, (window.Y + 190) * window.UiScale,
            (window.X + 480) * window.UiScale, (window.Y + 191) * window.UiScale, null, pointer);
        yield return new AssertionStep("ordinary native slider still edits", _ =>
            EndToEndAssert.True(panel.SliderValue > .7f, $"Expected a deliberate native drag to increase the slider, observed {panel.SliderValue}."));
        yield return Shot("original native text and slider respond to deliberate input");
        yield return new NativePointerScopeEndStep("restore control preservation pointer", pointer);
        yield return new WindowCancelActionStep("close control fixture through native cancel", typeof(ControlWindow).FullName!);
        consoleRequested = true;
        yield return new NativeDeveloperLogActionStep("open control workflow native console", true);
        yield return Shot("native console after control preservation");
        yield return new NativeDeveloperLogActionStep("close control workflow native console", false);
    }

    private WaitUntilStep CompleteProbe(int count) => new("complete requested hidden native controls", _ => panel.Completed == count,
        new EndToEndDeadline(int.MaxValue, 120, TimeSpan.FromSeconds(15)));
    private AssertionStep AssertProbe() => new("preserve native values, buffers, scroll and literal help", _ =>
    {
        EndToEndAssert.True(panel.Failure == null, panel.Failure ?? "");
        EndToEndAssert.Equal("original text value", panel.Value);
        SameBits(.3737f, panel.SliderValue);
    });
    private NativeWindowCaptureStep Capture() => new("capture exact native controls fixture", windows =>
        window = windows.Single(w => w.NativeId == panel.ID && w.RuntimeType == typeof(ControlWindow).FullName));
    private NativeWindowClickActionStep Click(string name, float x, float y) => new(name, window.Handle,
        (window.X + x) * window.UiScale, (window.Y + y) * window.UiScale, null, pointer);
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private static void SameBits(float expected, float actual) =>
        EndToEndAssert.Equal(BitConverter.SingleToInt32Bits(expected), BitConverter.SingleToInt32Bits(actual), "Native float bits must be retained.");
    private static void IntegerPostfix(ref int __1) => __1 = 12345;
    private static void TextPostfix()
    {
        if (throwAfterText) throw new InvalidOperationException("Owned string postfix failure");
    }
    private static void VectorPostfix(ref Vector3 __1, ref string[] __2)
    {
        if (!throwAfterVector) return;
        __1 = new Vector3(7, 8, 9);
        if (__2 != null && __2.Length != 0) __2[0] = "changed by fixture";
        __2 = new[] { "replacement", "replacement", "replacement" };
        throw new InvalidOperationException("Owned vector postfix failure");
    }

    private sealed class ControlWindow : Window
    {
        private readonly SettingsReadActionGuards actions;
        private readonly SettingsReadControlGuards controls;
        internal string Value = "original text value";
        internal float SliderValue = .3737f, NativeFloat = .33333f;
        internal int NativeInteger = 7;
        internal string IntegerBuffer = "7", FloatBuffer = ".33333";
        internal int Completed;
        internal bool Requested;
        internal string? Failure;
        public override Vector2 InitialSize => new(700, 420);
        internal ControlWindow(SettingsReadActionGuards actions, SettingsReadControlGuards controls)
        {
            this.actions = actions; this.controls = controls;
            doCloseX = true; closeOnCancel = true; absorbInputAroundWindow = true;
        }
        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new Rect(0, 0, inRect.width, 32), "Settings reader — native value preservation");
            if (Widgets.ButtonText(new Rect(0, 50, 320, 36), "Read native controls")) Requested = true;
            GUI.SetNextControlName("settings-controls-original-text");
            Value = Widgets.TextField(new Rect(0, 104, inRect.width, 32), Value);
            SliderValue = Widgets.HorizontalSlider(new Rect(0, 156, 500, 30), SliderValue, 0, 1, middleAlignment: true);
            Widgets.Label(new Rect(510, 156, 145, 30), SliderValue.ToString("0.0000"));
            Widgets.TextFieldNumeric(new Rect(0, 212, 160, 30), ref NativeInteger, ref IntegerBuffer);
            Widgets.TextFieldNumeric(new Rect(180, 212, 160, 30), ref NativeFloat, ref FloatBuffer);
            Widgets.Label(new Rect(350, 212, 290, 30), "Native int / float closures");
            Widgets.Label(new Rect(0, 270, inRect.width, 95), Failure ?? (Completed == 0
                ? "Read controls without changing their stored values."
                : $"{Completed} read(s) completed; values, buffers and scroll positions retained."));
            if (!Requested || Event.current.type != EventType.Repaint) return;
            Requested = false;
            try { Probe(); }
            catch (Exception error) { Failure = error.ToString(); }
            finally { Completed++; }
        }

        private void Probe()
        {
            EndToEndAssert.True(controls.Available && actions.Available);
            var previousBackend = GuiBackend.Current;
            var previousEvent = Event.current;
            var hidden = new SettingsHiddenBackend(previousBackend);
            var neutral = new Event { type = EventType.Repaint, rawType = EventType.Repaint,
                mousePosition = new Vector2(-1000000, -1000000) };
            var recorder = new SettingsPageRecorder(new SettingsPage("controls-fixture", "controls-open",
                "Control fixture", "Owned native fixture", null, "fumblesneeze.megamod"), "root");
            using var state = SettingsGuiStateScope.Capture();
            try
            {
                GuiBackend.Current = hidden; Event.current = neutral;
                state.Isolate(); GUI.matrix = Matrix4x4.identity; GUI.enabled = false;
                using var scope = actions.BeginRead();
                recorder.Observe(ReadControls);
                EndToEndAssert.True(!scope.Failed && !hidden.Failed && hidden.IsBalanced,
                    scope.FailureReason ?? hidden.FailureReason ?? "Hidden control structure imbalance.");
                EndToEndAssert.True(recorder.LastCapture?.Status == SettingsCaptureStatus.Partial &&
                    recorder.LastCapture.Records.Any(record => record.Kind == SettingsCaptureKind.Tooltip && record.Text == "Literal hidden help"));
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
        }

        private void ReadControls()
        {
            var rect = new Rect(10, 10, 250, 32);
            string original = Value;
            Value = GUI.TextField(rect, Value, 3, Text.CurTextFieldStyle);
            EndToEndAssert.Equal(original, Value, "A repaint must not truncate the original field.");
            EndToEndAssert.Equal(original, GUI.TextArea(rect, original, 3, Text.CurTextAreaStyle));
            EndToEndAssert.True(GUI.PasswordField(rect, null!, '*', Text.CurTextFieldStyle) == null);
            EndToEndAssert.True(Widgets.TextField(rect, null!) == null);
            EndToEndAssert.True(Widgets.TextField(rect, null!, 3) == null);
            throwAfterText = true;
            bool textThrew = false;
            try { Widgets.TextField(rect, null!, 3); }
            catch (InvalidOperationException error) when (error.Message == "Owned string postfix failure") { textThrew = true; }
            finally { throwAfterText = false; }
            EndToEndAssert.True(textThrew, "The native string exception must remain visible.");
            EndToEndAssert.Equal(original, GUI.TextField(rect, original, 3, Text.CurTextFieldStyle),
                "String nesting must unwind before an independent control, including after failure.");
            EndToEndAssert.Equal(original, Widgets.TextField(rect, original, 3));
            EndToEndAssert.True(Widgets.TextArea(rect, null!) == null);
            string delayed = "uncommitted buffer";
            EndToEndAssert.Equal(original, Widgets.DelayedTextField(rect, original, ref delayed, "delayed-fixture", "delayed-fixture"));
            EndToEndAssert.Equal("uncommitted buffer", delayed);
            int integer = 19; string intBuffer = null!;
            Widgets.TextFieldNumeric(rect, ref integer, ref intBuffer);
            EndToEndAssert.Equal(19, integer); EndToEndAssert.True(intBuffer == null);
            float number = -.0f; string floatBuffer = null!;
            Widgets.TextFieldNumeric(rect, ref number, ref floatBuffer);
            SameBits(-.0f, number); EndToEndAssert.True(floatBuffer == null);
            number = 1.75f;
            Widgets.TextFieldPercent(rect, ref number, ref floatBuffer);
            SameBits(1.75f, number); EndToEndAssert.True(floatBuffer == null);
            var vector = new Vector3(.12345679f, -.0f, 3.25f);
            Vector3 beforeVector = vector; string[] buffer = null!;
            Widgets.TextFieldVector(rect, ref vector, ref buffer);
            EndToEndAssert.Equal(beforeVector, vector); EndToEndAssert.True(buffer == null);
            buffer = new[] { "one", "two", "three", "untouched fourth" };
            var originalBuffer = buffer;
            throwAfterVector = true;
            bool threw = false;
            try { Widgets.TextFieldVector(rect, ref vector, ref buffer); }
            catch (InvalidOperationException error) when (error.Message == "Owned vector postfix failure") { threw = true; }
            finally { throwAfterVector = false; }
            EndToEndAssert.True(threw && ReferenceEquals(buffer, originalBuffer));
            EndToEndAssert.Equal(beforeVector, vector);
            EndToEndAssert.True(buffer.SequenceEqual(new[] { "one", "two", "three", "untouched fourth" }));
            var soundTime = typeof(Widgets).GetField("lastDragSliderSoundTime", BindingFlags.Static | BindingFlags.NonPublic)!;
            float originalSoundTime = (float)soundTime.GetValue(null)!;
            SliderValue = Widgets.HorizontalSlider(rect, SliderValue, 0, 1, roundTo: .25f);
            SameBits(.3737f, SliderValue);
            SameBits(originalSoundTime, (float)soundTime.GetValue(null)!);
            number = .3737f;
            Widgets.HorizontalSlider(rect, ref number, new FloatRange(0, 1), roundTo: .25f);
            SameBits(.3737f, number);
            SameBits(.3737f, Widgets.FrequencyHorizontalSlider(rect, .3737f, .1f, 5f, true));
            SameBits(9f, GUI.Slider(rect, 9f, .1f, 0f, 1f, GUIStyle.none, GUIStyle.none, true, 123));
            var range = new FloatRange(.12345679f, .8765432f); var beforeRange = range;
            Widgets.FloatRange(rect, 117, ref range, roundTo: .1f);
            EndToEndAssert.Equal(beforeRange, range);
            Widgets.FloatRangeWithTypeIn(rect, 118, ref range);
            EndToEndAssert.Equal(beforeRange, range);
            var integerRange = new IntRange(-2, 101); var beforeIntegerRange = integerRange;
            Widgets.IntRange(rect, 119, ref integerRange);
            EndToEndAssert.Equal(beforeIntegerRange, integerRange);
            var scroll = new Vector2(999, 999); var view = new Rect(0, 0, 400, 400);
            Vector2 returned = GUI.BeginScrollView(rect, scroll, view);
            try { EndToEndAssert.Equal(scroll, returned, "The default public scroll entry must retain its original position."); }
            finally { GUI.EndScrollView(); }
            returned = GUI.BeginScrollView(rect, scroll, view, GUIStyle.none, GUIStyle.none);
            try { EndToEndAssert.Equal(scroll, returned, "The styled public scroll entry must retain its original position."); }
            finally { GUI.EndScrollView(); }
            returned = GUI.BeginScrollView(rect, scroll, view, true, true);
            try { EndToEndAssert.Equal(scroll, returned, "The explicit-bar public scroll entry must retain its original position."); }
            finally { GUI.EndScrollView(); }
            returned = GUI.BeginScrollView(rect, scroll, view, true, true, GUIStyle.none, GUIStyle.none);
            try { EndToEndAssert.Equal(scroll, returned, "The styled explicit-bar public scroll entry must retain its original position."); }
            finally { GUI.EndScrollView(); }
            Widgets.BeginScrollView(rect, ref scroll, view);
            try { EndToEndAssert.Equal(new Vector2(999, 999), scroll); }
            finally { Widgets.EndScrollView(); }
            Widgets.BeginScrollView(rect, ref scroll, view, showScrollbars: false);
            try { EndToEndAssert.Equal(new Vector2(999, 999), scroll); }
            finally { Widgets.EndScrollView(); }
            var activeTips = (IDictionary)typeof(TooltipHandler).GetField("activeTips", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
            int tipsBefore = activeTips.Count;
            bool edges = DebugViewSettings.drawTooltipEdges;
            try
            {
                DebugViewSettings.drawTooltipEdges = true;
                TooltipHandler.TipRegion(rect, "Literal hidden help");
                TooltipHandler.TipRegion(rect, new TipSignal(() => throw new InvalidOperationException("Lazy help must not run"), 179141));
                EndToEndAssert.Equal(tipsBefore, activeTips.Count);
            }
            finally { DebugViewSettings.drawTooltipEdges = edges; }
        }
    }
}
