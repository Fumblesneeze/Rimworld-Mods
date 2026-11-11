#if RIMWORLD_1_7
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using UnityEngine.InputSystem;
using Verse;

namespace RimWorldDevGateway.EndToEndTests;

// Paused UI budget:23 native operations ×15s +6 screenshots ×30s +10s initial wait =535s.
// Reserve65s for assertions/checkpoints, persistence, native cancel and owned cleanup:600s total.
// No observed rendering ceiling exists, so frames defer to this wall cap; ticks remain independent.
// Launch with at least900s to include180s startup and120s host shutdown/evidence headroom.
[RimWorldEndToEndTest("gateway.native-window-text", EndToEndTestContract.GatewayPackageId,
    "brrainz.harmony", EndToEndTestContract.CorePackageId,
    MaxFrames = int.MaxValue, MaxGameTicks = 1200, MaxWallClockSeconds = 600)]
public sealed class GatewayNativeWindowTextTests : IRimWorldEndToEndTest
{
    private TextProbe probe = null!;
    private EndToEndNativeWindow window = null!;
    private string pointer = null!;
    public void Arrange(IEndToEndContext context)
    {
        probe = new TextProbe();
        context.DeferCleanup(() => { if (Find.WindowStack.Windows.Contains(probe)) probe.Close(false); });
        Find.WindowStack.Add(probe); // Disposable native fields only; no result is assigned.
    }
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new WaitUntilStep("native text probe rendered", _ => probe.Draws > 0,
            new EndToEndDeadline(int.MaxValue, 120, TimeSpan.FromSeconds(10)));
        yield return Capture("capture native text probe");
        yield return new NativePointerScopeBeginStep("own pointer during native text workflow", value => pointer = value);
        yield return Shot("native text before input");
        foreach (var step in Replace("NaN", 70)) yield return step;
        yield return Click("click disabled native Apply", 350, 70);
        yield return new AssertionStep("invalid native draft remains unapplied", _ =>
            EndToEndAssert.True(probe.Numeric == "NaN" && probe.Applied == 3 && probe.ApplyCount == 0 && probe.Adjacent == "Unchanged"));
        yield return Shot("invalid native text and disabled Apply");
        foreach (var step in Replace("12.5", 70)) yield return step;
        yield return Click("apply native numeric draft", 350, 70);
        yield return new AssertionStep("native button applied actual typed value", _ =>
            EndToEndAssert.True(probe.Numeric == "12.5" && probe.Applied == 12.5f && probe.ApplyCount == 1));
        yield return Shot("native numeric value after Apply");
        foreach (var step in Replace("", 70)) yield return step;
        yield return new AssertionStep("empty replacement uses native deletion", _ =>
            EndToEndAssert.True(probe.Numeric == "" && probe.Applied == 12.5f));
        foreach (var step in Replace("Médicine 2", 120)) yield return step;
        yield return new AssertionStep("native name field received BMP text only", _ =>
            EndToEndAssert.True(probe.NameDraft == "Médicine 2" && probe.Adjacent == "Unchanged"));
        yield return Shot("native name and empty numeric draft");

        yield return Click("focus numeric field before adversarial layout", 100, 70);
        yield return Capture("capture focus before layout mutation", true);
        int keyEvents = probe.KeyDownEvents;
        yield return new CheckpointStep("arm native focus change during injected key layout", _ =>
        {
            probe.MoveFocusDuringKeyLayout = true; // Adversarial precondition, not the expected edit.
            return new Dictionary<string, string> { ["controlHandle"] = window.FocusedTextControlHandle! };
        });
        yield return new NativeWindowReplaceTextActionStep("reject changed focus after layout", window.Handle,
            window.FocusedTextControlHandle!, "999", "stale_native_text_focus");
        yield return new AssertionStep("changed field received no stale native key", _ =>
            EndToEndAssert.True(!probe.MoveFocusDuringKeyLayout && probe.Numeric == "" &&
                probe.Adjacent == "Unchanged" && probe.KeyDownEvents == keyEvents));
        yield return Shot("adjacent field unchanged after stale focus rejection");
        foreach (var step in Replace("Recovered", 170)) yield return step;
        yield return new AssertionStep("fresh captured field works after owned cleanup", _ =>
            EndToEndAssert.True(probe.Adjacent == "Recovered" && probe.NameDraft == "Médicine 2" && probe.Numeric == ""));
        yield return Shot("fresh focus works after rejection");
        yield return new NativePointerScopeEndStep("restore owned pointer after text workflow", pointer);
        yield return new WindowCancelActionStep("close native text probe", typeof(TextProbe).FullName!);
    }
    private IEnumerable<EndToEndStep> Replace(string text, float y)
    {
        yield return Click("focus native text field for " + (text == "" ? "empty text" : text), 100, y);
        yield return Capture("capture actual native focus", true);
        yield return new NativeWindowReplaceTextActionStep("replace focused native text", window.Handle, window.FocusedTextControlHandle!, text);
    }
    private NativeWindowCaptureStep Capture(string name, bool requireFocus = false) => new(name, windows =>
    {
        window = windows.Single(item => item.NativeId == probe.ID && item.RuntimeType == typeof(TextProbe).FullName);
        if (requireFocus) EndToEndAssert.True(!string.IsNullOrEmpty(window.FocusedTextControlHandle), "The actual native field must own a capture handle.");
    });
    private NativeWindowClickActionStep Click(string name, float x, float y) => new(name, window.Handle,
        (window.X + Window.StandardMargin + x) * window.UiScale, (window.Y + Window.StandardMargin + y) * window.UiScale,
        null, pointer);
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);

    private sealed class TextProbe : Window
    {
        public string Numeric = "3", NameDraft = "Existing", Adjacent = "Unchanged";
        public float Applied = 3;
        public int ApplyCount, Draws, KeyDownEvents;
        public bool MoveFocusDuringKeyLayout;
        public override Vector2 InitialSize => new(500, 330);
        public TextProbe() { forcePause = true; absorbInputAroundWindow = true; doCloseX = true; }
        public override void DoWindowContents(Rect inRect)
        {
            Draws++;
            if (Event.current.rawType == EventType.keyDown) KeyDownEvents++;
            Widgets.Label(new Rect(20, 10, 430, 30), "Native focused text verification");
            GUI.SetNextControlName("numeric-probe");
            Numeric = Widgets.TextField(new Rect(20, 55, 260, 30), Numeric);
            bool valid = float.TryParse(Numeric, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed) && float.IsFinite(parsed);
            if (Widgets.ButtonText(new Rect(300, 55, 130, 30), "Apply", active: valid)) { Applied = parsed; ApplyCount++; }
            GUI.SetNextControlName("name-probe");
            NameDraft = Widgets.TextField(new Rect(20, 105, 410, 30), NameDraft);
            GUI.SetNextControlName("adjacent-probe");
            Adjacent = Widgets.TextField(new Rect(20, 155, 410, 30), Adjacent);
            if (MoveFocusDuringKeyLayout && Event.current.type == EventType.layout && Keyboard.current.leftCtrlKey.isPressed)
            { MoveFocusDuringKeyLayout = false; GUI.FocusControl("adjacent-probe"); }
            Widgets.Label(new Rect(20, 210, 430, 55), "Applied: " + Applied.ToString(CultureInfo.InvariantCulture) + "; native Apply count: " + ApplyCount);
        }
    }
}
#endif
