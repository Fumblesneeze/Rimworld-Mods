#if RIMWORLD_1_7
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using LudeonTK;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace RimWorldDevGateway.EndToEndTests;

[RimWorldEndToEndTest("gateway.native-window-float-menu", EndToEndTestContract.GatewayPackageId,
    "brrainz.harmony", EndToEndTestContract.CorePackageId,
    MaxFrames = 1200, MaxGameTicks = 1200, MaxWallClockSeconds = 60)]
public sealed class GatewayNativeWindowFloatMenuTests : IRimWorldEndToEndTest
{
    private MenuProbe probe = null!;
    private EndToEndNativeWindow captured = null!;
    private Vector2 originalPointer;
    private string pointerScope = null!;
    private EndToEndNativeWindow menuCapture = null!;
    private EditWindow_Log? ownedLog;

    public void Arrange(IEndToEndContext context)
    {
        EndToEndAssert.False(Find.WindowStack.IsOpen<FloatMenu>(), "The probe requires no pre-existing menu.");
        EndToEndAssert.True(Prefs.DevMode && !Find.WindowStack.IsOpen<EditWindow_Log>(),
            "Developer mode and a closed native log are required.");
        probe = new MenuProbe();
        context.DeferCleanup(() =>
        {
            if (probe.Menu != null && Find.WindowStack.Windows.Contains(probe.Menu)) probe.Menu.Close(false);
            if (Find.WindowStack.Windows.Contains(probe)) probe.Close(false);
            if (ownedLog != null && Find.WindowStack.Windows.Contains(ownedLog)) ownedLog.Close(false);
        });
        Find.WindowStack.Add(probe); // Disposable UI precondition, never the expected popup/result.
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new WaitUntilStep("scroll-view probe rendered", _ => probe.DrawCount > 0,
            new EndToEndDeadline(120, 120, TimeSpan.FromSeconds(10)));
        yield return new NativeWindowCaptureStep("capture exact popup probe", windows =>
            captured = windows.Single(item => item.NativeId == probe.ID && item.RuntimeType == typeof(MenuProbe).FullName));
        yield return new CheckpointStep("retain pointer before opening the native menu", _ =>
        {
            originalPointer = UI.MousePositionOnUIInverted;
            return probe.Evidence(originalPointer);
        });
        yield return new ScreenshotStep("before native popup click", Array.Empty<string>(), 0);
        yield return new NativePointerScopeBeginStep("own pointer for native popup workflow", handle => pointerScope = handle);
        // The scroll starts at content y=40; its button occupies y=80..108 with zero scroll.
        yield return new NativeWindowClickActionStep("open ordinary FloatMenu from the scroll-view button", captured.Handle,
            (captured.X + Window.StandardMargin + (captured.Width - 2 * Window.StandardMargin - 20 - 8) / 4) * captured.UiScale,
            (captured.Y + Window.StandardMargin + 40 + 94) * captured.UiScale, null, pointerScope);
        // Preserve lifecycle evidence before any assertion can terminate this diagnostic scenario.
        yield return new CheckpointStep("retain popup creation closure and pointer evidence", _ => probe.Evidence(originalPointer));
        yield return new ScreenshotStep("native popup outcome before persistence assertion", Array.Empty<string>(), 0);
        yield return new AssertionStep("native button created exactly one ordinary menu", _ =>
        {
            EndToEndAssert.True(probe.ButtonActivations == 1 && probe.Menu != null,
                "Record actual button activation separately from dispatched mouse phases or menu persistence.");
            EndToEndAssert.True(probe.Menu!.vanishIfMouseDistant,
                "The fixture must retain ordinary native popup behavior.");
            EndToEndAssert.True(GenUI.DistFromRect(probe.OpenBounds.ContractedBy(-5), originalPointer) > 95,
                "This reproduction requires the original pointer outside the native menu's distance threshold; inspect retained coordinates.");
        });
        yield return new AssertionStep("native popup remains available for follow-up player input", _ =>
            EndToEndAssert.True(probe.Menu != null && Find.WindowStack.Windows.Contains(probe.Menu) &&
                probe.Menu.ID == probe.OpenNativeId && probe.MenuClosures == 0 && probe.Selections == 0,
                "The exact menu opened by the native button must remain without a selection or unrelated pointer move."));
        yield return new NativeWindowCaptureStep("capture exact opened menu", windows => menuCapture = windows.Single(
            item => item.NativeId == probe.OpenNativeId && item.RuntimeType == typeof(FloatMenu).FullName));
        yield return new NativeWindowClickActionStep("choose first native popup option", menuCapture.Handle,
            (menuCapture.X + 20) * menuCapture.UiScale, (menuCapture.Y + 14) * menuCapture.UiScale, null, pointerScope);
        yield return new CheckpointStep("retain native menu selection outcome", _ => probe.Evidence(originalPointer));
        yield return new ScreenshotStep("after native popup selection", Array.Empty<string>(), 0);
        yield return new AssertionStep("native option selected exactly once", _ =>
            EndToEndAssert.True(probe.Selections == 1 && probe.MenuClosures == 1 &&
                probe.Menu != null && !Find.WindowStack.Windows.Contains(probe.Menu)));
        yield return new NativePointerScopeEndStep("restore the workflow original pointer", pointerScope);
        yield return new CheckpointStep("retain restored workflow pointer", _ => probe.Evidence(originalPointer));
        yield return new ScreenshotStep("after original pointer restored", Array.Empty<string>(), 0);
        yield return new AssertionStep("workflow restored original engine pointer", _ =>
            EndToEndAssert.True(UI.MousePositionOnUIInverted == originalPointer));
        yield return new WindowCancelActionStep("close the disposable popup probe", typeof(MenuProbe).FullName!);
        yield return new NativeDeveloperLogActionStep("open native developer log after popup workflow", true);
        yield return new AssertionStep("native developer log opened", _ =>
        {
            ownedLog = Find.WindowStack.WindowOfType<EditWindow_Log>();
            EndToEndAssert.NotNull(ownedLog);
        });
        yield return new ScreenshotStep("native developer console after popup workflow", Array.Empty<string>(), 0);
        yield return new NativeDeveloperLogActionStep("restore closed native developer log", false);
    }

    private sealed class MenuProbe : Window
    {
        private Vector2 scroll;
        public int DrawCount { get; private set; }
        public int MouseDowns { get; private set; }
        public int MouseUps { get; private set; }
        public int ButtonActivations { get; private set; }
        public int MenuClosures { get; private set; }
        public int Selections { get; private set; }
        public FloatMenu? Menu { get; private set; }
        public int OpenNativeId { get; private set; } = -1;
        public int OpenFrame { get; private set; } = -1;
        public int CloseFrame { get; private set; } = -1;
        public Vector2 OpenPointer { get; private set; }
        public Vector2 ClosePointer { get; private set; }
        public Rect OpenBounds { get; private set; }
        private string closeEvent = "not observed";
        public override Vector2 InitialSize => new(900, 700);

        public MenuProbe()
        { forcePause = true; absorbInputAroundWindow = true; doCloseX = true; }

        public override void DoWindowContents(Rect inRect)
        {
            DrawCount++;
            if (Event.current.button == 0 && Event.current.type == EventType.mouseDown) MouseDowns++;
            if (Event.current.button == 0 && Event.current.type == EventType.mouseUp) MouseUps++;
            Widgets.Label(new Rect(0, 0, inRect.width, 35), "Native popup lifecycle verification");
            var viewport = new Rect(0, 40, inRect.width, inRect.height - 80);
            var content = new Rect(0, 0, inRect.width - 20, 1200);
            Widgets.BeginScrollView(viewport, ref scroll, content);
            try
            {
                Widgets.Label(new Rect(0, 0, content.width, 28), "Scroll-view controls");
                Widgets.Label(new Rect(0, 32, content.width, 44), "Click opens an ordinary native menu. No popup behavior is overridden.");
                if (Widgets.ButtonText(new Rect(0, 80, (content.width - 8) / 2, 28), "Open native menu"))
                    OpenMenu();
                Widgets.Label(new Rect(0, 140, content.width, 28), "Button activations: " + ButtonActivations);
                Widgets.Label(new Rect(0, 174, content.width, 28), "Menu closures: " + MenuClosures + "; selections: " + Selections);
                Widgets.Label(new Rect(0, 208, content.width, 28), "Exact menu still open: " +
                    (Menu != null && Find.WindowStack.Windows.Contains(Menu)));
                Widgets.Label(new Rect(0, 242, content.width, 28), "Pointer at open / close: " + Point(OpenPointer) + " / " + Point(ClosePointer));
                Widgets.Label(new Rect(0, 276, content.width, 28), "Current engine pointer: " + Point(UI.MousePositionOnUIInverted));
            }
            finally { Widgets.EndScrollView(); }
        }

        private void OpenMenu()
        {
            ButtonActivations++;
            if (Menu != null) return; // A duplicate activation fails the count without leaking another fixture.
            var menu = new FloatMenu(new List<FloatMenuOption>
            {
                new("First native option", () => Selections++),
                new("Second native option", () => Selections++)
            });
            Menu = menu;
            menu.onCloseCallback = () =>
            {
                MenuClosures++;
                CloseFrame = Time.frameCount;
                ClosePointer = UI.MousePositionOnUIInverted;
                closeEvent = Event.current == null ? "none" : Event.current.type + "/" + Event.current.rawType;
            };
            Find.WindowStack.Add(menu); // Only the actual native button callback creates this result.
            OpenFrame = Time.frameCount;
            OpenNativeId = menu.ID;
            OpenBounds = menu.windowRect;
            OpenPointer = UI.MousePositionOnUIInverted;
        }

        public IReadOnlyDictionary<string, string> Evidence(Vector2 originalPointer) => new Dictionary<string, string>
        {
            ["originalPointerGui"] = Point(originalPointer),
            ["currentPointerGui"] = Point(UI.MousePositionOnUIInverted),
            ["pointerAtOpenGui"] = Point(OpenPointer),
            ["pointerAtCloseGui"] = Point(ClosePointer),
            ["sourceNativeId"] = ID.ToString(),
            ["menuNativeIdAtOpen"] = OpenNativeId.ToString(),
            ["menuCurrentNativeId"] = Menu?.ID.ToString() ?? "absent",
            ["exactMenuStillOpen"] = (Menu != null && Find.WindowStack.Windows.Contains(Menu)).ToString(),
            ["buttonActivations"] = ButtonActivations.ToString(),
            ["sourceMouseDowns"] = MouseDowns.ToString(),
            ["sourceMouseUps"] = MouseUps.ToString(),
            ["menuClosures"] = MenuClosures.ToString(),
            ["menuSelections"] = Selections.ToString(),
            ["openFrame"] = OpenFrame.ToString(),
            ["closeFrame"] = CloseFrame.ToString(),
            ["observationFrame"] = Time.frameCount.ToString(),
            ["closeEvent"] = closeEvent,
            ["menuOpenBounds"] = Point(new Vector2(OpenBounds.x, OpenBounds.y)) + ";" + Point(OpenBounds.size),
            ["originalPointerDistanceFromMenu"] = Menu == null ? "not created" :
                GenUI.DistFromRect(OpenBounds.ContractedBy(-5), originalPointer).ToString("R", CultureInfo.InvariantCulture)
        };

        private static string Point(Vector2 point) => point.x.ToString("R", CultureInfo.InvariantCulture) + "," +
            point.y.ToString("R", CultureInfo.InvariantCulture);
    }
}
#endif
