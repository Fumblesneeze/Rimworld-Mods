using LudeonTK;
using Megamod.Features.SettingsSearch.Runtime;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.SettingsSearch;

// Component workflow: a real native editor keeps its focus/state through an isolated callback.
// The owned fixture is not evidence of foreign-page discovery or provider compatibility.
[RimWorldEndToEndTest("megamod.settings-search.native-state", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 1200, MaxWallClockSeconds = 400)]
public sealed class NativeSettingsStateTests : IRimWorldEndToEndTest
{
    private EditorWindow editor = null!;
    private EndToEndNativeWindow window = null!;
    private string pointer = null!;
    private bool consoleRequested;

    public void Arrange(IEndToEndContext context)
    {
        EndToEndAssert.True(Current.Game.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting &&
            LanguageDatabase.activeLanguage.folderName == "English" && !Find.WindowStack.IsOpen<EditWindow_Log>());
        var speed = Find.TickManager.CurTimeSpeed;
        editor = new EditorWindow();
        context.DeferCleanup(() =>
        {
            if (Find.WindowStack.Windows.Contains(editor)) editor.Close(false);
            if (consoleRequested)
                foreach (var log in Find.WindowStack.Windows.OfType<EditWindow_Log>().ToArray()) log.Close(false);
            Find.TickManager.CurTimeSpeed = speed;
        });
        Find.TickManager.Pause();
        Find.WindowStack.Add(editor);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new NativePointerScopeBeginStep("own the native editor pointer", value => pointer = value);
        yield return Capture();
        yield return new NativeWindowClickActionStep("focus the real native text field", window.Handle,
            (window.X + 100) * window.UiScale, (window.Y + 92) * window.UiScale, null, pointer);
        yield return Capture();
        yield return new NativeWindowReplaceTextActionStep("type into the original native editor", window.Handle,
            window.FocusedTextControlHandle!, "before discovery");
        yield return new AssertionStep("native typing reached the original field", _ =>
            EndToEndAssert.Equal("before discovery", editor.Value));
        yield return Shot("focused native editor before state isolation");

        foreach (bool throws in new[] { false, true })
        {
            int expected = editor.Probes + 1;
            yield return new AssertionStep("arm isolated callback on the next ordinary repaint", _ =>
            { editor.ThrowInCallback = throws; editor.Requested = true; });
            yield return new WaitUntilStep("finish the isolated callback and restoration", _ => editor.Probes == expected,
                new EndToEndDeadline(int.MaxValue, 120, TimeSpan.FromSeconds(15)));
            yield return new AssertionStep("original editor and style survive isolation", _ =>
            {
                EndToEndAssert.True(editor.Failure == null, editor.Failure ?? "");
                EndToEndAssert.Equal("before discovery", editor.Value);
            });
            yield return Shot(throws ? "focused editor after throwing isolated callback" : "focused editor after isolated callback");
        }

        // No refocus click: the original focused control must remain the native input target.
        yield return Capture();
        yield return new NativeWindowReplaceTextActionStep("continue editing after state restoration", window.Handle,
            window.FocusedTextControlHandle!, "after discovery");
        yield return new AssertionStep("native input still edits the original field", _ =>
            EndToEndAssert.Equal("after discovery", editor.Value));
        yield return Shot("original native editor accepts subsequent typing");
        yield return new NativePointerScopeEndStep("restore the native editor pointer", pointer);
        yield return new WindowCancelActionStep("close the test editor through native cancel", typeof(EditorWindow).FullName!);
        consoleRequested = true;
        yield return new NativeDeveloperLogActionStep("open state-isolation workflow native console", true);
        yield return Shot("native console after state-isolation workflow");
        yield return new NativeDeveloperLogActionStep("close state-isolation workflow native console", false);
    }

    private NativeWindowCaptureStep Capture() => new("capture exact native editor and focus", windows =>
        window = windows.Single(w => w.NativeId == editor.ID && w.RuntimeType == typeof(EditorWindow).FullName));
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);

    private sealed class EditorWindow : Window
    {
        internal string Value = "initial";
        internal string? Failure;
        internal bool Requested, ThrowInCallback;
        internal int Probes;
        public override Vector2 InitialSize => new(620, 300);
        internal EditorWindow() { doCloseX = true; closeOnCancel = true; absorbInputAroundWindow = true; }

        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new Rect(0, 0, inRect.width, 32), "Settings reader — native state verification");
            GUI.SetNextControlName("settings-state-original-editor");
            Value = Widgets.TextField(new Rect(0, 52, inRect.width, 32), Value);
            Widgets.Label(new Rect(0, 102, inRect.width, 80), Failure ??
                (Probes == 0 ? "Type into this field before isolation." : "Original editor restored; continue typing."));
            if (!Requested || Event.current.type != EventType.Repaint) return;
            Requested = false;
            try { Probe(); }
            catch (Exception error) { Failure = error.ToString(); }
            finally { Probes++; }
        }

        private void Probe()
        {
            int originalFocus = GUIUtility.keyboardControl;
            EndToEndAssert.True(originalFocus != 0, "The real field must be focused before isolation.");
            var originalEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), originalFocus);
            string originalText = originalEditor.text;
            int originalCursor = originalEditor.cursorIndex, originalSelection = originalEditor.selectIndex;
            var originalFont = Text.Font;
            var originalAnchor = Text.Anchor;
            bool originalWrap = Text.WordWrap;
            var originalStyle = Text.CurFontStyle;
            var styleAnchor = originalStyle.alignment;
            bool styleWrap = originalStyle.wordWrap;
            var sentinel = new InvalidOperationException("Owned isolated callback failure");
            try
            {
                using var scope = SettingsGuiStateScope.Capture();
                scope.Isolate();
                EndToEndAssert.Equal(0, GUIUtility.keyboardControl, "Isolated controls must not inherit real focus.");
                var isolatedEditor = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), originalFocus);
                EndToEndAssert.True(!ReferenceEquals(originalEditor, isolatedEditor),
                    "Hidden native controls must not reuse the original editor object.");
                isolatedEditor.text = "hidden scratch";
                isolatedEditor.cursorIndex = 2;
                isolatedEditor.selectIndex = 4;
                GUIUtility.keyboardControl = originalFocus + 11;
                Text.Font = GameFont.Medium;
                Text.Anchor = TextAnchor.LowerRight;
                Text.WordWrap = !originalWrap;
                _ = Text.CurFontStyle;
                originalStyle.alignment = TextAnchor.LowerCenter;
                originalStyle.wordWrap = !styleWrap;
                if (ThrowInCallback) throw sentinel;
            }
            catch (InvalidOperationException error) when (ReferenceEquals(error, sentinel)) { }
            EndToEndAssert.Equal(originalFocus, GUIUtility.keyboardControl);
            EndToEndAssert.True(ReferenceEquals(originalEditor, GUIUtility.GetStateObject(typeof(TextEditor), originalFocus)));
            EndToEndAssert.Equal(originalText, originalEditor.text);
            EndToEndAssert.Equal(originalCursor, originalEditor.cursorIndex);
            EndToEndAssert.Equal(originalSelection, originalEditor.selectIndex);
            EndToEndAssert.Equal(originalFont, Text.Font);
            EndToEndAssert.Equal(originalAnchor, Text.Anchor);
            EndToEndAssert.Equal(originalWrap, Text.WordWrap);
            EndToEndAssert.Equal(styleAnchor, originalStyle.alignment);
            EndToEndAssert.Equal(styleWrap, originalStyle.wordWrap);
        }
    }
}
