#if RIMWORLD_1_7
using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace RimWorldDevGateway.EndToEndTests;

// 10 bounded native operations ×15s +7 screenshots ×30s +3 waits ×10s +120s synchronous/cleanup reserve =510s.
// Paused UI has no assumed refresh-rate ceiling; wall time is the finite frame-lifetime bound.
[RimWorldEndToEndTest("gateway.native-window-layout-replacement", EndToEndTestContract.GatewayPackageId,
    "brrainz.harmony", EndToEndTestContract.CorePackageId,
    MaxFrames = int.MaxValue, MaxGameTicks = 1800, MaxWallClockSeconds = 600)]
public sealed class GatewayNativeWindowLayoutReplacementTests : IRimWorldEndToEndTest
{
    private readonly List<Window> owned = new();
    private SourceProbe source = null!;
    private Probe? replacement;
    private EndToEndNativeWindow captured = null!;

    public void Arrange(IEndToEndContext context)
    {
        EndToEndAssert.True(Prefs.DevMode && !Find.WindowStack.IsOpen<EditWindow_Log>(),
            "Developer mode and a closed native log are required.");
        context.DeferCleanup(() =>
        {
            foreach (Window window in owned)
                if (Find.WindowStack.Windows.Contains(window)) window.Close(false);
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        foreach (bool onRelease in new[] { false, true })
        {
            string edge = onRelease ? "release" : "press";
            yield return new CheckpointStep("prepare replacement during " + edge + " layout", _ =>
            {
                replacement = null;
                source = new SourceProbe(onRelease, () =>
                {
                    replacement = new Probe("Replacement: must receive no stale input", onRelease);
                    owned.Add(replacement);
                    Find.WindowStack.Add(replacement);
                });
                owned.Add(source);
                Find.WindowStack.Add(source); // Disposable adversarial native UI fixture.
                return new Dictionary<string, string> { ["replacementEdge"] = edge };
            });
            yield return new WaitUntilStep("source rendered before " + edge, _ => source.DrawCount > 0,
                new EndToEndDeadline(int.MaxValue, 120, TimeSpan.FromSeconds(10)));
            yield return new NativeWindowCaptureStep("capture source before " + edge,
                windows => captured = windows.Single(item => item.NativeId == source.ID &&
                    item.RuntimeType == typeof(SourceProbe).FullName));
            yield return new ScreenshotStep("before " + edge + " layout replacement", Array.Empty<string>(), 0);
            yield return Click("reject stale " + edge + " after layout", "native_input_not_dispatched");
            if (onRelease)
                yield return new CheckpointStep("retain replacement control evidence", _ =>
                    replacement?.ControlEvidence() ?? new Dictionary<string, string> { ["replacementMissing"] = "true" });
            yield return new AssertionStep("replacement received no stale " + edge, _ =>
            {
                EndToEndAssert.True(replacement != null && source.Replaced &&
                    !Find.WindowStack.Windows.Contains(source), "The fixture must replace the exact source during layout.");
                EndToEndAssert.True(replacement!.MouseEvents == 0 && replacement.Clicks == 0,
                    "The replacement must receive neither stale mouse events nor a button activation.");
                if (onRelease)
                    EndToEndAssert.True(replacement.ControlEstablishmentAttempted &&
                        replacement.EstablishedHotControl != 0 &&
                        replacement.EstablishedControlWindowId == replacement.ID &&
                        GUIUtility.hotControl == replacement.EstablishedHotControl,
                        "The native release tail and owned cleanup must preserve the replacement's established control. " +
                        "Established/current: " + replacement.EstablishedHotControl + "/" + GUIUtility.hotControl +
                        "; established owner/replacement: " + replacement.EstablishedControlWindowId + "/" + replacement.ID);
            });
            yield return new ScreenshotStep("replacement unchanged after rejected " + edge, Array.Empty<string>(), 0);
            if (onRelease)
            {
                yield return new CheckpointStep("release replacement fixture control", _ =>
                {
                    replacement!.ReleaseFixtureControl = true;
                    return new Dictionary<string, string> { ["preservedControlId"] = replacement.EstablishedHotControl.ToString() };
                });
                yield return new WaitUntilStep("replacement completed its control lifecycle", _ => GUIUtility.hotControl == 0,
                    new EndToEndDeadline(int.MaxValue, 120, TimeSpan.FromSeconds(10)));
            }
            yield return new NativeWindowCaptureStep("capture replacement after " + edge,
                windows => captured = windows.Single(item => item.NativeId == replacement!.ID &&
                    item.RuntimeType == typeof(Probe).FullName));
            yield return Click("independent click after rejected " + edge);
            yield return new AssertionStep("lane recovered after rejected " + edge, _ =>
                EndToEndAssert.True(replacement!.Clicks == 1,
                    "A fresh native click must activate the replacement exactly once after owned cleanup."));
            yield return new ScreenshotStep("fresh click worked after rejected " + edge, Array.Empty<string>(), 0);
            yield return new WindowCancelActionStep("close replacement after " + edge, typeof(Probe).FullName!);
        }
        yield return new NativeDeveloperLogActionStep("open native log after replacement verification", true);
        yield return new AssertionStep("native developer log opened", _ =>
        {
            var log = Find.WindowStack.WindowOfType<EditWindow_Log>();
            EndToEndAssert.NotNull(log);
            owned.Add(log);
        });
        yield return new ScreenshotStep("native developer console after replacement verification", Array.Empty<string>(), 0);
        yield return new NativeDeveloperLogActionStep("restore closed native developer log", false);
    }

    private NativeWindowClickActionStep Click(string name, string? expectedFailure = null)
        => new(name, captured.Handle, (captured.X + Window.StandardMargin + 185) * captured.UiScale,
            (captured.Y + Window.StandardMargin + 75) * captured.UiScale, expectedFailure);

    private class Probe : Window
    {
        private readonly string title;
        private readonly bool establishControl;
        private readonly List<string> controlObservations = new();
        public bool ControlEstablishmentAttempted { get; private set; }
        public int EstablishedHotControl { get; private set; }
        public int EstablishedControlWindowId { get; private set; } = int.MinValue;
        public int EstablishmentFrame { get; private set; }
        public bool ReleaseFixtureControl { get; set; }
        public int DrawCount { get; private set; }
        public int MouseEvents { get; private set; }
        public int Clicks { get; private set; }
        public override Vector2 InitialSize => new(460, 300);
        public Probe(string title, bool establishControl = false)
        { this.title = title; this.establishControl = establishControl;
          forcePause = true; absorbInputAroundWindow = true; doCloseX = true; }
        public override void DoWindowContents(Rect inRect)
        {
            DrawCount++;
            Event input = Event.current;
            if (ControlEstablishmentAttempted) ObserveControl("entry", input);
            if (input.button == 0 && input.type is EventType.mouseDown or EventType.mouseUp or EventType.mouseDrag)
                MouseEvents++;
            Widgets.Label(new Rect(20, 10, 400, 40), title);
            if (Widgets.ButtonText(new Rect(20, 60, 330, 30), "Native button")) Clicks++;
            Widgets.Label(new Rect(20, 110, 400, 30), "Received mouse events: " + MouseEvents);
            Widgets.Label(new Rect(20, 145, 400, 30), "Native button activations: " + Clicks);
            if (establishControl)
            {
                // Adversarial fixture precondition: a new native control owns capture during the
                // replacement layout. The scenario observes preservation, never assigns the result.
                GUI.SetNextControlName("replacement-owned-capture");
                GUI.TextField(new Rect(20, 185, 330, 24), "Replacement control");
                if (input.type == EventType.layout && !ControlEstablishmentAttempted)
                {
                    ControlEstablishmentAttempted = true; // Never retry or recreate a lost control.
                    GUI.FocusControl("replacement-owned-capture");
                    EstablishedHotControl = GUIUtility.keyboardControl;
                    EstablishedControlWindowId = GUI.GetFocusedControlWindowId();
                    EstablishmentFrame = Time.frameCount;
                    GUIUtility.hotControl = EstablishedHotControl;
                    ObserveControl("established", input);
                }
                if (ReleaseFixtureControl && GUIUtility.hotControl == EstablishedHotControl)
                    GUIUtility.hotControl = 0; // Only this fixture's identified control, after observation.
                Widgets.Label(new Rect(20, 210, 400, 24), "Owned/current capture: " +
                    EstablishedHotControl + "/" + GUIUtility.hotControl);
            }
        }

        public IReadOnlyDictionary<string, string> ControlEvidence() => new Dictionary<string, string>
        {
            ["controlEstablishmentAttempted"] = ControlEstablishmentAttempted.ToString(),
            ["establishedHotControl"] = EstablishedHotControl.ToString(),
            ["currentHotControl"] = GUIUtility.hotControl.ToString(),
            ["currentKeyboardControl"] = GUIUtility.keyboardControl.ToString(),
            ["establishedControlWindowId"] = EstablishedControlWindowId.ToString(),
            ["currentFocusedWindowId"] = GUI.GetFocusedControlWindowId().ToString(),
            ["replacementWindowId"] = ID.ToString(),
            ["establishmentFrame"] = EstablishmentFrame.ToString(),
            ["observationFrame"] = Time.frameCount.ToString(),
            ["replacementMouseEvents"] = MouseEvents.ToString(),
            ["replacementClicks"] = Clicks.ToString(),
            ["controlObservations"] = string.Join("; ", controlObservations)
        };

        private void ObserveControl(string phase, Event input)
        {
            if (controlObservations.Count < 24)
                controlObservations.Add(phase + ":frame=" + Time.frameCount + ",event=" + input.type +
                    ",hot=" + GUIUtility.hotControl + ",keyboard=" + GUIUtility.keyboardControl +
                    ",focusedWindow=" + GUI.GetFocusedControlWindowId());
        }
    }

    private sealed class SourceProbe : Probe
    {
        private readonly bool onRelease;
        private readonly Action replace;
        public bool Replaced { get; private set; }
        public SourceProbe(bool onRelease, Action replace) : base("Source: replacement is armed")
        { this.onRelease = onRelease; this.replace = replace; }
        public override void ExtraOnGUI()
        {
            if (Replaced || Event.current.type != EventType.layout ||
                !(onRelease ? Input.GetMouseButtonUp(0) : Input.GetMouseButtonDown(0))) return;
            Replaced = true;
            Close(false);
            replace();
        }
    }
}
#endif
