#if RIMWORLD_1_7
using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace RimWorldDevGateway.EndToEndTests;

[RimWorldEndToEndTest("gateway.native-window-checkbox", EndToEndTestContract.GatewayPackageId,
    "brrainz.harmony", EndToEndTestContract.CorePackageId,
    MaxFrames = 1200, MaxGameTicks = 1200, MaxWallClockSeconds = 60)]
public sealed class GatewayNativeWindowCheckboxTests : IRimWorldEndToEndTest
{
    private CheckboxProbe window = null!;
    private EndToEndNativeWindow captured = null!;
    private EditWindow_Log? ownedLog;

    public void Arrange(IEndToEndContext context)
    {
        EndToEndAssert.True(Prefs.DevMode && !Find.WindowStack.IsOpen<EditWindow_Log>(),
            "Developer mode and a closed native log are required.");
        context.DeferCleanup(() => { if (ownedLog != null && Find.WindowStack.Windows.Contains(ownedLog)) ownedLog.Close(false); });
        window = new CheckboxProbe();
        context.DeferCleanup(() => { if (Find.WindowStack.Windows.Contains(window)) window.Close(false); });
        Find.WindowStack.Add(window); // Disposable native control fixture only; no expected state is assigned.
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new WaitUntilStep("native probe has rendered", _ => window.DrawCount > 0,
            new EndToEndDeadline(120, 120, TimeSpan.FromSeconds(10)));
        yield return new NativeWindowCaptureStep("capture exact probe instance", windows => captured = windows.Single(
            item => item.NativeId == window.ID && item.RuntimeType == typeof(CheckboxProbe).FullName));
        yield return Capture("before native checkbox input");
        yield return new NativeWindowClickActionStep("click enabled native checkbox", captured.Handle,
            (captured.X + Window.StandardMargin + 338) * captured.UiScale,
            (captured.Y + Window.StandardMargin + 75) * captured.UiScale);
        yield return new AssertionStep("enabled checkbox changed through Widgets", _ =>
            EndToEndAssert.True(window.EnabledChecked && window.Changes == 1,
                "The enabled native checkbox must toggle exactly once."));
        yield return Capture("after enabled checkbox changed");
        yield return new NativeWindowClickActionStep("click disabled native checkbox", captured.Handle,
            (captured.X + Window.StandardMargin + 338) * captured.UiScale,
            (captured.Y + Window.StandardMargin + 120) * captured.UiScale);
        yield return new AssertionStep("disabled checkbox ignored native input", _ =>
            EndToEndAssert.True(!window.DisabledChecked && window.EnabledChecked && window.Changes == 1,
                "The disabled checkbox must stay unchecked and the enabled control must remain unchanged."));
        yield return Capture("after disabled checkbox stayed unchecked");
        yield return new CheckpointStep("native checkbox outcome", _ => new Dictionary<string, string>
        {
            ["windowHandle"] = captured.Handle,
            ["nativeId"] = captured.NativeId.ToString(),
            ["uiScale"] = captured.UiScale.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["enabledChecked"] = window.EnabledChecked.ToString(),
            ["disabledChecked"] = window.DisabledChecked.ToString(),
            ["nativeWidgetChanges"] = window.Changes.ToString()
        });
        yield return new WindowCancelActionStep("close probe through native cancel", typeof(CheckboxProbe).FullName!);
        yield return new AssertionStep("probe closed", _ => EndToEndAssert.True(!Find.WindowStack.Windows.Contains(window),
            "The disposable probe must be closed."));
        yield return new NativeDeveloperLogActionStep("open the real native developer log", true);
        yield return new AssertionStep("native developer log is open", _ =>
        {
            ownedLog = Find.WindowStack.WindowOfType<EditWindow_Log>();
            EndToEndAssert.NotNull(ownedLog);
        });
        yield return Capture("native developer console after checkbox verification");
        yield return new NativeDeveloperLogActionStep("restore the closed native developer log", false);
    }

    private static ScreenshotStep Capture(string name) => new(name, Array.Empty<string>(), 0);

    private sealed class CheckboxProbe : Window
    {
        private bool enabledChecked;
        private bool disabledChecked;
        public bool EnabledChecked => enabledChecked;
        public bool DisabledChecked => disabledChecked;
        public int Changes { get; private set; }
        public int DrawCount { get; private set; }
        public override Vector2 InitialSize => new(420, 260);
        public CheckboxProbe()
        {
            forcePause = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = false;
        }
        public override void DoWindowContents(Rect inRect)
        {
            DrawCount++;
            Widgets.Label(new Rect(20, 10, 340, 35), "Native input verification");
            bool before = enabledChecked;
            Widgets.CheckboxLabeled(new Rect(20, 60, 330, 30), "Enabled checkbox", ref enabledChecked);
            if (before != enabledChecked) Changes++;
            Widgets.CheckboxLabeled(new Rect(20, 105, 330, 30), "Disabled checkbox", ref disabledChecked, disabled: true);
            Widgets.Label(new Rect(20, 155, 340, 30), "Native widget changes: " + Changes);
        }
    }
}
#endif
