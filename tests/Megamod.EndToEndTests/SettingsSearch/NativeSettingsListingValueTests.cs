using System.Reflection;
using HarmonyLib;
using LudeonTK;
using Megamod.Features.SettingsSearch.Runtime;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using UnityStubs.Gui;
using Verse;

namespace Megamod.EndToEndTests.SettingsSearch;

[RimWorldEndToEndTest("megamod.settings-search.native-listing-values", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 1200, MaxWallClockSeconds = 450)]
public sealed class NativeSettingsListingValueTests : IRimWorldEndToEndTest
{
    private const string ProbeOwner = "fumblesneeze.megamod.e2e.listing-values";
    private static bool throwAfterListing;
    private ListingWindow panel = null!;
    private EndToEndNativeWindow window = null!;
    private string pointer = null!;
    private bool consoleRequested;

    public void Arrange(IEndToEndContext context)
    {
        EndToEndAssert.True(Current.Game.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting &&
            LanguageDatabase.activeLanguage.folderName == "English" && !Find.WindowStack.IsOpen<EditWindow_Log>());
        var speed = Find.TickManager.CurTimeSpeed;
        var actions = new SettingsReadActionGuards(message => Log.Warning(message));
        var controls = new SettingsReadControlGuards(message => Log.Warning(message));
        var probes = new HarmonyLib.Harmony(ProbeOwner);
        MethodInfo target = typeof(Listing_Standard).GetMethod(nameof(Listing_Standard.IntEntry))!;
        panel = new ListingWindow(actions, controls);
        context.DeferCleanup(() =>
        {
            try { controls.Dispose(); }
            finally
            {
                try { actions.Dispose(); }
                finally
                {
                    throwAfterListing = false;
                    probes.Unpatch(target, HarmonyPatchType.Postfix, ProbeOwner);
                    if (Find.WindowStack.Windows.Contains(panel)) panel.Close(false);
                    if (consoleRequested)
                        foreach (var log in Find.WindowStack.Windows.OfType<EditWindow_Log>().ToArray()) log.Close(false);
                    Find.TickManager.CurTimeSpeed = speed;
                }
            }
        });
        probes.Patch(target, postfix: new HarmonyMethod(typeof(NativeSettingsListingValueTests), nameof(ListingPostfix)));
        EndToEndAssert.True(actions.Initialize() && controls.Initialize());
        Find.TickManager.Pause();
        Find.WindowStack.Add(panel);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new NativePointerScopeBeginStep("own listing-value pointer", value => pointer = value);
        yield return Capture();
        yield return Shot("original listed integer before hidden minimum normalization");
        yield return Click("request a hidden listing minimum check", 150, 80);
        yield return new WaitUntilStep("complete hidden listing call and throwing postfix", _ => panel.Completed,
            new EndToEndDeadline(int.MaxValue, 120, TimeSpan.FromSeconds(15)));
        yield return new AssertionStep("preserve original listed integer and buffer", _ =>
        {
            EndToEndAssert.True(panel.Failure == null, panel.Failure ?? "");
            EndToEndAssert.Equal(7, panel.Value);
            EndToEndAssert.Equal("7", panel.Buffer);
        });
        yield return Shot("original listed integer retained after native request");
        yield return Capture();
        yield return Click("increment the original native listing value", 458, 134);
        yield return new AssertionStep("ordinary listing increment still edits", _ =>
        {
            EndToEndAssert.Equal(8, panel.Value);
            EndToEndAssert.Equal("8", panel.Buffer);
        });
        yield return Shot("original listing value responds to deliberate increment");
        yield return new NativePointerScopeEndStep("restore listing-value pointer", pointer);
        yield return new WindowCancelActionStep("close listing-value panel through native cancel", typeof(ListingWindow).FullName!);
        consoleRequested = true;
        yield return new NativeDeveloperLogActionStep("open listing-value native console", true);
        yield return Shot("native console after listing-value workflow");
        yield return new NativeDeveloperLogActionStep("close listing-value native console", false);
    }

    private NativeWindowCaptureStep Capture() => new("capture exact listing-value panel", windows =>
        window = windows.Single(value => value.NativeId == panel.ID && value.RuntimeType == typeof(ListingWindow).FullName));
    private NativeWindowClickActionStep Click(string name, float x, float y) => new(name, window.Handle,
        (window.X + x) * window.UiScale, (window.Y + y) * window.UiScale, null, pointer);
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private static void ListingPostfix(ref int __0, ref string __1)
    {
        if (!throwAfterListing) return;
        __0 = 99; __1 = "changed by owned postfix";
        throw new InvalidOperationException("Owned listing postfix failure");
    }

    private sealed class ListingWindow : Window
    {
        private readonly SettingsReadActionGuards actions;
        private readonly SettingsReadControlGuards controls;
        internal int Value = 7;
        internal string Buffer = "7";
        internal bool Requested, Completed;
        internal string? Failure;
        public override Vector2 InitialSize => new(660, 300);
        internal ListingWindow(SettingsReadActionGuards actions, SettingsReadControlGuards controls)
        {
            this.actions = actions; this.controls = controls;
            doCloseX = true; closeOnCancel = true; absorbInputAroundWindow = true;
        }
        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new Rect(0, 0, inRect.width, 32), "Settings reader — listed value preservation");
            if (Widgets.ButtonText(new Rect(0, 44, 320, 36), "Read listed value")) Requested = true;
            var visible = new Listing_Standard();
            visible.Begin(new Rect(0, 104, 500, 60));
            try { visible.IntEntry(ref Value, ref Buffer); }
            finally { visible.End(); }
            Widgets.Label(new Rect(0, 175, inRect.width, 80), Failure ?? (Completed
                ? "Read completed; original value and buffer retained."
                : "Read a higher minimum without changing the original value."));
            if (!Requested || Event.current.type != EventType.Repaint) return;
            Requested = false;
            try { Probe(); }
            catch (Exception error) { Failure = error.ToString(); }
            finally { Completed = true; }
        }

        private void Probe()
        {
            EndToEndAssert.True(actions.Available && controls.Available);
            var previousBackend = GuiBackend.Current;
            var previousEvent = Event.current;
            var hidden = new SettingsHiddenBackend(previousBackend);
            var neutral = new Event { type = EventType.Repaint, rawType = EventType.Repaint,
                mousePosition = new Vector2(-1000000, -1000000) };
            using var state = SettingsGuiStateScope.Capture();
            try
            {
                GuiBackend.Current = hidden; Event.current = neutral;
                state.Isolate(); GUI.matrix = Matrix4x4.identity; GUI.enabled = false;
                using var scope = actions.BeginRead();
                var listing = new Listing_Standard();
                listing.Begin(new Rect(10, 10, 500, 80));
                try
                {
                    listing.IntEntry(ref Value, ref Buffer, min: 29);
                    EndToEndAssert.Equal(7, Value, "A hidden listing minimum must not rewrite the original value.");
                    EndToEndAssert.Equal("7", Buffer);
                    throwAfterListing = true;
                    bool threw = false;
                    try { listing.IntEntry(ref Value, ref Buffer, min: 29); }
                    catch (InvalidOperationException error) when (error.Message == "Owned listing postfix failure") { threw = true; }
                    finally { throwAfterListing = false; }
                    EndToEndAssert.True(threw, "The original postfix exception must remain visible.");
                    EndToEndAssert.Equal(7, Value);
                    EndToEndAssert.Equal("7", Buffer);
                }
                finally { listing.End(); }
                EndToEndAssert.True(!scope.Failed && !hidden.Failed && hidden.IsBalanced,
                    scope.FailureReason ?? hidden.FailureReason ?? "Listing structure imbalance.");
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
        }
    }
}
