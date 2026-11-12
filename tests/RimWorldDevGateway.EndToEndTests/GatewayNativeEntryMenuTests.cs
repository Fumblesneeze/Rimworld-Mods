#if RIMWORLD_1_7
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using LudeonTK;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace RimWorldDevGateway.EndToEndTests;

// Replaces the entire disposable game: the runner must select this scenario alone.
// Maximum path:12 bounded native operations ×15s +7 screenshots ×30s +365s declared waits
// +120s reserve for synchronous actions/observations and cleanup =875s, below the900s cap.
[RimWorldEndToEndTest("gateway.native-entry-menu", EndToEndTestContract.GatewayPackageId,
    "brrainz.harmony", EndToEndTestContract.CorePackageId,
    MaxFrames = int.MaxValue, MaxGameTicks = 60000, MaxWallClockSeconds = 900)]
public sealed class GatewayNativeEntryMenuTests : IRimWorldEndToEndTest, IEndToEndProcessOwnedGameFixture
{
    private Game original = null!;
    private EndToEndNativeWindow window = null!;
    private EndToEndNativeEntryMenuControl options = null!, quicktest = null!;
    private bool confirmationExpected;
    private int quitIndex;
    private long entryFrame, closedFrame;

    public void Arrange(IEndToEndContext context)
    {
        var sourceGame = Current.Game;
        EndToEndAssert.True(sourceGame?.PlayerHasControl == true && Prefs.DevMode &&
            !sourceGame.Info.permadeathMode && !GameDataSaveLoader.SavingIsTemporarilyDisabled,
            "The isolated nonpermadeath quicktest must accept native menu input.");
        EndToEndAssert.True(!Find.WindowStack.IsOpen<EditWindow_Log>(), "The developer log must start closed.");
        original = sourceGame!; // The native source Game was validated above on the Unity thread.
        // This process-owned fixture creates no extra Things or settings; native scene teardown
        // and exact-process shutdown own the two disposable games. Do not restore old Game refs.
        context.DeferCleanup(() =>
        {
            var stack = Find.WindowStack;
            if (stack == null) return;
            foreach (var owned in stack.Windows.Where(value => value is Dialog_Options or MainTabWindow_Menu or EditWindow_Log).ToArray())
                owned.Close(false);
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep("pause the disposable source game", true, EndToEndGameSpeed.Normal);
        yield return new EscapeMenuActionStep("open the ordinary game menu", true);
        yield return new NativeWindowCaptureStep("capture the exact game menu", windows =>
        {
            var menu = Find.WindowStack.WindowOfType<MainTabWindow_Menu>();
            EndToEndAssert.NotNull(menu);
            window = windows.Single(value => value.NativeId == menu.ID && value.RuntimeType == typeof(MainTabWindow_Menu).FullName);
            bool hasFiles = (bool)typeof(MainTabWindow_Menu).GetField("anyGameFiles", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(menu)!;
            // Source-inspected native order: Save, optional Load, Review, Options, Quit.
            // Require single-line native heights before using the inspected row geometry.
            foreach (string key in new[] { "Save", "LoadGame", "ReviewScenario", "Options", "QuitToMainMenu" })
                EndToEndAssert.True(Text.CalcHeight(key.Translate(), 170) <= 45, "Native menu label exceeds inspected row height.");
            quitIndex = hasFiles ? 4 : 3;
            confirmationExpected = GameDataSaveLoader.CurrentGameStateIsValuable;
        });
        yield return Capture("native menu before quitting to Entry");
        yield return new NativeWindowClickActionStep("click native Quit to main menu", window.Handle,
            (window.X + Window.StandardMargin + 85) * window.UiScale,
            (window.Y + Window.StandardMargin + quitIndex * 52 + 22.5) * window.UiScale);
        if (confirmationExpected)
        {
            yield return new WaitUntilStep("wait for native quit confirmation", _ => Find.WindowStack?.WindowOfType<Dialog_MessageBox>() != null,
                Deadline(15));
            yield return new NativeWindowCaptureStep("capture native quit confirmation", windows =>
            {
                var box = Find.WindowStack.WindowOfType<Dialog_MessageBox>();
                EndToEndAssert.True(box != null && box.text.ToString() == "ConfirmQuit".Translate().ToString() &&
                    box.buttonAText == "Confirm".Translate().ToString() && box.buttonBText == "GoBack".Translate().ToString() &&
                    box.buttonCText == null && box.buttonADestructive, "The actual quit confirmation must be the expected native two-button dialog.");
                window = windows.Single(value => value.NativeId == box!.ID && value.RuntimeType == typeof(Dialog_MessageBox).FullName);
            });
            yield return Capture("native confirmation before leaving the disposable game");
            // Native button A is the rightmost of two buttons; no delegate is invoked here.
            yield return new NativeWindowClickActionStep("confirm native quit", window.Handle,
                (window.X + Window.StandardMargin + (window.Width - 2 * Window.StandardMargin) * 0.75 + 5) * window.UiScale,
                (window.Y + window.Height - Window.StandardMargin - 17.5) * window.UiScale);
        }
        yield return new WaitUntilStep("wait for the actual Entry root and null Game", _ =>
            Current.ProgramState == ProgramState.Entry && Current.Root is Root_Entry && Current.Game == null &&
            !LongEventHandler.AnyEventNowOrWaiting, Deadline(60));
        yield return new CheckpointStep("retain actual Entry transition", _ =>
        {
            entryFrame = context.FrameCount;
            return new Dictionary<string, string> { ["sourceGameIdentity"] = RuntimeHelpers.GetHashCode(original).ToString(),
                ["entryRoot"] = Current.Root.GetType().FullName!, ["currentGame"] = "null" };
        });
        yield return new WaitUntilStep("allow the native Entry layout to settle", _ => context.FrameCount >= entryFrame + 3, Deadline(10));
        yield return Capture("actual Entry menu after native quit");
        yield return new NativeEntryMenuCaptureStep("capture actual Entry Options and quicktest", controls =>
        {
            options = controls.Single(value => value.Control == "options");
            quicktest = controls.Single(value => value.Control == "dev-quicktest");
        });
        yield return new NativeEntryMenuClickActionStep("click actual Entry Options", options.Handle);
        yield return new WaitUntilStep("observe native Options from Entry", _ =>
            Current.Game == null && Find.WindowStack.WindowOfType<Dialog_Options>() != null, Deadline(15));
        yield return Capture("native Options opened from Entry");
        yield return new NativeWindowCaptureStep("capture the actual Options window", windows =>
        {
            var dialog = Find.WindowStack.WindowOfType<Dialog_Options>();
            EndToEndAssert.True(dialog != null && dialog.doCloseX);
            window = windows.Single(value => value.NativeId == dialog!.ID && value.RuntimeType == typeof(Dialog_Options).FullName);
        });
        // Window uses Widgets.CloseButtonFor: 18-pixel image, four-pixel inset in GUI units.
        yield return new NativeWindowClickActionStep("close Options through its native X", window.Handle,
            (window.X + window.Width - 13) * window.UiScale, (window.Y + 13) * window.UiScale);
        yield return new WaitUntilStep("observe Options closed at Entry", _ =>
            Current.Game == null && !Find.WindowStack.IsOpen<Dialog_Options>(), Deadline(15));
        yield return new CheckpointStep("record closed Options frame", _ =>
        { closedFrame = context.FrameCount; return new Dictionary<string, string> { ["currentGame"] = "null" }; });
        yield return new WaitUntilStep("allow fresh Entry layout after Options", _ => context.FrameCount >= closedFrame + 3, Deadline(10));
        yield return Capture("Entry menu before native developer quicktest");
        yield return new NativeEntryMenuCaptureStep("capture a fresh developer quicktest handle", controls =>
            quicktest = controls.Single(value => value.Control == "dev-quicktest"));
        yield return new NativeEntryMenuClickActionStep("click the actual developer quicktest button", quicktest.Handle);
        yield return new WaitUntilStep("wait for the distinct native quicktest game", _ => Current.ProgramState == ProgramState.Playing &&
            Current.Game?.PlayerHasControl == true && Current.Root is Root_Play && !ReferenceEquals(Current.Game, original) &&
            Find.CurrentMap != null && !LongEventHandler.AnyEventNowOrWaiting, Deadline(240));
        yield return new TimeControlActionStep("pause the newly generated disposable game", true, EndToEndGameSpeed.Normal);
        yield return Capture("distinct playable colony created by native quicktest");
        yield return new CheckpointStep("retain native new-game outcome", _ => new Dictionary<string, string>
        {
            ["sourceGameIdentity"] = RuntimeHelpers.GetHashCode(original).ToString(),
            ["newGameIdentity"] = RuntimeHelpers.GetHashCode(Current.Game).ToString(),
            ["differentGameReference"] = (!ReferenceEquals(original, Current.Game)).ToString(),
            ["root"] = Current.Root.GetType().FullName!, ["mapId"] = Find.CurrentMap.uniqueID.ToString(),
            ["playerHasControl"] = Current.Game.PlayerHasControl.ToString()
        });
        yield return new NativeDeveloperLogActionStep("open native console after Entry navigation", true);
        yield return Capture("native developer console after Entry navigation");
        yield return new NativeDeveloperLogActionStep("restore closed native console", false);
    }

    private static ScreenshotStep Capture(string name) => new(name, Array.Empty<string>(), 0);
    private static EndToEndDeadline Deadline(int seconds) => new(int.MaxValue, 60000, TimeSpan.FromSeconds(seconds));
}
#endif
