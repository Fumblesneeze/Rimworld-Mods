using LudeonTK;
using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.SettingsSearch.Core;
using Megamod.Features.SettingsSearch.Runtime;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using UnityStubs.Gui;
using Verse;

namespace Megamod.EndToEndTests.SettingsSearch;

[RimWorldEndToEndTest("megamod.settings-search.native-listing-bounds", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 1200, MaxWallClockSeconds = 450)]
public sealed class NativeSettingsListingBoundsTests : IRimWorldEndToEndTest
{
    private BoundsWindow panel = null!;
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
        var caches = new SettingsReadListingGuards(message => Log.Warning(message));
        var bounds = new SettingsReadListingBoundsGuard(controls, caches, message => Log.Warning(message));
        panel = new BoundsWindow(actions, controls, caches, bounds);
        context.DeferCleanup(() =>
        {
            try
            {
                if (Find.WindowStack.Windows.Contains(panel)) panel.Close(false);
                if (consoleRequested)
                    foreach (var log in Find.WindowStack.Windows.OfType<EditWindow_Log>().ToArray()) log.Close(false);
            }
            finally
            {
                try { bounds.Dispose(); }
                finally
                {
                    try { caches.Dispose(); }
                    finally
                    {
                        try { controls.Dispose(); }
                        finally { try { actions.Dispose(); } finally { Find.TickManager.CurTimeSpeed = speed; } }
                    }
                }
            }
        });
        Find.TickManager.Pause();
        Find.WindowStack.Add(panel);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new NativePointerScopeBeginStep("own listing-bounds pointer", value => pointer = value);
        yield return new NativeWindowCaptureStep("capture exact listing-bounds panel", windows =>
            window = windows.Single(value => value.NativeId == panel.ID && value.RuntimeType == typeof(BoundsWindow).FullName));
        yield return Shot("native bounded listing before contract request");
        yield return new NativeWindowClickActionStep("request the native listing contract", window.Handle,
            (window.X + 170) * window.UiScale, (window.Y + 76) * window.UiScale, null, pointer);
        yield return new WaitUntilStep("complete listing contract request", _ => panel.Completed,
            new EndToEndDeadline(int.MaxValue, 120, TimeSpan.FromSeconds(15)));
        yield return Shot("native listing contract request result");
        yield return new AssertionStep("bind every actual native listing consumer", _ =>
        {
            EndToEndAssert.True(panel.Failure == null, panel.Failure ?? "");
            EndToEndAssert.Equal(17, panel.ConsumerCount);
            EndToEndAssert.True(panel.GetterBound);
            EndToEndAssert.True(!panel.Value && panel.Scroll == 0 && !panel.OrdinarySawTarget);
        });
        yield return new NativeWindowClickActionStep("read an option below the visible native listing", window.Handle,
            (window.X + 490) * window.UiScale, (window.Y + 76) * window.UiScale, null, pointer);
        yield return new WaitUntilStep("complete hidden offscreen listing read", _ => panel.ReadCompleted,
            new EndToEndDeadline(int.MaxValue, 120, TimeSpan.FromSeconds(15)));
        yield return Shot("offscreen read result above the unchanged listing");
        yield return new AssertionStep("find offscreen label and help without moving or editing", _ =>
        {
            EndToEndAssert.True(panel.Failure == null, panel.Failure ?? "");
            EndToEndAssert.True(panel.FoundLabel && panel.FoundHelp && !panel.Value && panel.Scroll == 0);
        });
        yield return new NativeWindowDragActionStep("scroll normally to the original offscreen option", window.Handle,
            (window.X + 520) * window.UiScale, (window.Y + 150) * window.UiScale,
            (window.X + 520) * window.UiScale, (window.Y + 340) * window.UiScale, null, pointer);
        yield return new WaitUntilStep("observe the original option in the viewport", _ => panel.TargetVisible,
            new EndToEndDeadline(int.MaxValue, 120, TimeSpan.FromSeconds(15)));
        yield return Shot("original unchecked option revealed by native scrolling");
        yield return new NativeWindowClickActionStep("edit the original native checkbox", window.Handle,
            (window.X + 496) * window.UiScale, (window.Y + panel.TargetClickY) * window.UiScale, null, pointer);
        yield return new AssertionStep("the deliberate native edit changes the original option", _ => EndToEndAssert.True(panel.Value));
        yield return Shot("original option responds to deliberate native edit");
        yield return new NativePointerScopeEndStep("restore listing-bounds pointer", pointer);
        yield return new WindowCancelActionStep("close listing-bounds panel through native cancel", typeof(BoundsWindow).FullName!);
        consoleRequested = true;
        yield return new NativeDeveloperLogActionStep("open listing-bounds native console", true);
        yield return Shot("native console after listing-bounds request");
        yield return new NativeDeveloperLogActionStep("close listing-bounds native console", false);
    }

    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);

    private sealed class BoundsWindow : Window
    {
        private const string Target = "Offscreen habitat option";
        private const string Help = "Literal help for the offscreen habitat option.";
        private readonly Listing_Standard listing;
        private readonly SettingsReadActionGuards actions;
        private readonly SettingsReadControlGuards controls;
        private readonly SettingsReadListingGuards caches;
        private readonly SettingsReadListingBoundsGuard bounds;
        private readonly SettingsPage page = new("bounds-fixture", "open-bounds", "Native listing fixture", "Megamod", "Fumblesneeze",
            PackageIdentity: "fumblesneeze.megamod");
        private Vector2 scroll;
        private bool requested, readRequested;
        internal bool Completed, GetterBound;
        internal bool Value, OrdinarySawTarget, ReadCompleted, FoundLabel, FoundHelp;
        internal int ConsumerCount;
        internal string? Failure;
        private float targetY;
        internal float Scroll => scroll.y;
        internal float TargetClickY => 18 + 100 + targetY - scroll.y + 12;
        internal bool TargetVisible => targetY >= scroll.y && targetY + 24 <= scroll.y + 230;
        public override Vector2 InitialSize => new(700, 470);
        internal BoundsWindow(SettingsReadActionGuards actions, SettingsReadControlGuards controls,
            SettingsReadListingGuards caches, SettingsReadListingBoundsGuard bounds)
        {
            this.actions = actions; this.controls = controls; this.caches = caches; this.bounds = bounds;
            doCloseX = true; closeOnCancel = true; absorbInputAroundWindow = true;
            listing = new Listing_Standard(new Rect(0, 0, 490, 230), () => scroll);
        }
        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new Rect(0, 0, inRect.width, 32), "Settings reader — native listing bounds");
            if (Widgets.ButtonText(new Rect(0, 44, 320, 36), "Check native listing contract")) requested = true;
            if (Widgets.ButtonText(new Rect(336, 44, 295, 36), "Read offscreen option")) readRequested = true;
            var ordinary = new SettingsPageRecorder(page, "ordinary");
            ordinary.Observe(DrawListing);
            OrdinarySawTarget = ordinary.LastCapture?.Records.Any(record => record.Text == Target) == true;
            Widgets.Label(new Rect(0, 350, inRect.width, 75), Failure ?? (ReadCompleted
                ? $"Found: {Target}\nHelp captured: {FoundHelp}. Original value: {Value}."
                : Completed ? $"Native listing contract ready: {ConsumerCount} consumers."
                : "Request the native contract before reading offscreen options."));
            if (Event.current.type != EventType.Repaint) return;
            if (requested)
            {
                requested = false;
                try
                {
                    if (!SettingsListingBoundsTargets.TryBind(out var catalog, out var failure))
                        throw new InvalidOperationException(failure ?? "The native listing contract is unavailable.");
                    ConsumerCount = catalog!.Consumers.Count;
                    GetterBound = catalog.Getter.DeclaringType == typeof(Listing_Standard) &&
                        catalog.Getter.Name == "get_BoundingRectCached";
                }
                catch (Exception error) { Failure = error.Message; }
                finally { Completed = true; }
            }
            if (readRequested)
            {
                readRequested = false;
                try { ReadOffscreen(); }
                catch (Exception error) { Failure = error.Message; }
                finally { ReadCompleted = true; }
            }
        }

        private void DrawListing()
        {
            Widgets.BeginScrollView(new Rect(0, 100, 510, 230), ref scroll, new Rect(0, 0, 490, 720));
            try
            {
                listing.Begin(new Rect(0, 0, 490, 720));
                try
                {
                    for (int index = 0; index < 20; index++)
                    {
                        listing.Label($"Original row {index + 1:00}");
                        listing.Gap(8);
                    }
                    targetY = listing.CurHeight;
                    listing.CheckboxLabeled(Target, ref Value, Help);
                }
                finally { listing.End(); }
            }
            finally { Widgets.EndScrollView(); }
        }

        private void ReadOffscreen()
        {
            EndToEndAssert.True(actions.Initialize() && controls.Initialize() && caches.Initialize());
            EndToEndAssert.True(bounds.Initialize(), "The native listing bounds guard is unavailable.");
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
                NativeSettingsListingBoundsProbes.VerifyGenericFamily();
                NativeSettingsListingBoundsProbes.VerifyCallbackFailure(actions);
                using (var scope = actions.BeginRead())
                {
                    NativeSettingsListingBoundsProbes.VerifyInsideRead();
                    var recorder = new SettingsPageRecorder(page, "hidden");
                    recorder.Observe(DrawListing);
                    EndToEndAssert.True(!scope.Failed && !hidden.Failed && hidden.IsBalanced,
                        scope.FailureReason ?? hidden.FailureReason ?? "Listing structure imbalance.");
                    var capture = recorder.LastCapture!;
                    EndToEndAssert.True(capture.Status != SettingsCaptureStatus.Failed);
                    FoundLabel = capture.Records.Any(record => record.Kind == SettingsCaptureKind.Text && record.Text == Target);
                    FoundHelp = capture.Records.Any(record => record.Kind == SettingsCaptureKind.Tooltip && record.Text == Help);
                }
                NativeSettingsListingBoundsProbes.VerifySuppressionRefusal(bounds, actions);
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
