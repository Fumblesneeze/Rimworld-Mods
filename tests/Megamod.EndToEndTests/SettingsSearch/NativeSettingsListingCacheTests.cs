using System.Reflection;
using HarmonyLib;
using LudeonTK;
using Megamod.Features.SettingsSearch.Runtime;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using UnityStubs.Gui;
using Verse;

namespace Megamod.EndToEndTests.SettingsSearch;

[RimWorldEndToEndTest("megamod.settings-search.native-listing-cache", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 1200, MaxWallClockSeconds = 450)]
public sealed class NativeSettingsListingCacheTests : IRimWorldEndToEndTest
{
    private const string ProbeOwner = "fumblesneeze.megamod.e2e.listing-cache";
    private static string? throwing;
    private static bool nesting;
    private CacheWindow panel = null!;
    private EndToEndNativeWindow window = null!;
    private string pointer = null!;
    private bool consoleRequested;
    private float originalScroll;
    private static readonly FieldInfo Positions = typeof(Listing_Standard).GetField("labelScrollbarPositions", BindingFlags.Instance | BindingFlags.NonPublic)!;
    private static readonly FieldInfo Seen = typeof(Listing_Standard).GetField("labelScrollbarPositionsSetThisFrame", BindingFlags.Instance | BindingFlags.NonPublic)!;

    public void Arrange(IEndToEndContext context)
    {
        EndToEndAssert.True(Current.Game.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting &&
            LanguageDatabase.activeLanguage.folderName == "English" && !Find.WindowStack.IsOpen<EditWindow_Log>());
        var speed = Find.TickManager.CurTimeSpeed;
        context.DeferCleanup(() => Find.TickManager.CurTimeSpeed = speed);
        context.DeferCleanup(() =>
        {
            if (consoleRequested) Find.WindowStack.Windows.OfType<EditWindow_Log>().FirstOrDefault()?.Close(false);
        });
        var actions = new SettingsReadActionGuards(message => Log.Warning(message));
        context.DeferCleanup(actions.Dispose);
        var controls = new SettingsReadControlGuards(message => Log.Warning(message));
        context.DeferCleanup(controls.Dispose);
        var guards = new SettingsReadListingGuards(message => Log.Warning(message));
        context.DeferCleanup(guards.Dispose);
        var probes = new HarmonyLib.Harmony(ProbeOwner);
        MethodInfo label = typeof(Listing_Standard).GetMethod(nameof(Listing_Standard.Label), new[] { typeof(string), typeof(float), typeof(TipSignal?) })!;
        MethodInfo end = typeof(Listing_Standard).GetMethod(nameof(Listing_Standard.End))!;
        context.DeferCleanup(() => { throwing = null; nesting = false; probes.Unpatch(label, HarmonyPatchType.Postfix, ProbeOwner); });
        context.DeferCleanup(() => probes.Unpatch(end, HarmonyPatchType.Postfix, ProbeOwner));
        probes.Patch(label, postfix: new HarmonyMethod(typeof(NativeSettingsListingCacheTests), nameof(ThrowAfter)));
        probes.Patch(end, postfix: new HarmonyMethod(typeof(NativeSettingsListingCacheTests), nameof(ThrowAfter)));
        EndToEndAssert.True(actions.Initialize() && controls.Initialize() && guards.Initialize());
        panel = new CacheWindow(actions, controls, guards);
        context.DeferCleanup(() => { if (Find.WindowStack.Windows.Contains(panel)) panel.Close(false); });
        Find.TickManager.Pause();
        Find.WindowStack.Add(panel);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new NativePointerScopeBeginStep("own listing cache pointer", value => pointer = value);
        yield return Capture();
        yield return Wait("observe original native long label", () => panel.Draws > 1);
        yield return Shot("original long label begins at its first line");
        yield return Click("scroll the native long label down", 510, 248);
        yield return Wait("observe native scrollbar movement", () => panel.Scroll > 100);
        yield return Shot("native long label shows later lines before hidden reading");
        yield return new AssertionStep("retain actual native scroll position", _ => originalScroll = panel.Scroll);
        yield return Capture();
        yield return Click("request hidden listing cache read", 168, 80);
        yield return Wait("complete hidden listing cache request", () => panel.Completed);
        yield return Shot("long label after hidden read retains visible scroll position");
        yield return new AssertionStep("retain original listing collections and visible scroll", _ =>
        {
            EndToEndAssert.True(panel.Failure == null, panel.Failure ?? "");
            EndToEndAssert.Equal(originalScroll, panel.Scroll);
        });
        yield return Capture();
        yield return Click("scroll the original long label back to its first line", 510, 121);
        yield return Wait("observe subsequent native scrolling", () => panel.Scroll == 0);
        yield return Shot("original long label still responds to native scrolling");
        yield return new NativePointerScopeEndStep("restore listing cache pointer", pointer);
        yield return new WindowCancelActionStep("close listing cache panel through native cancel", typeof(CacheWindow).FullName!);
        consoleRequested = true;
        yield return new NativeDeveloperLogActionStep("open listing cache native console", true);
        yield return Shot("native console after listing cache workflow");
        yield return new NativeDeveloperLogActionStep("close listing cache native console", false);
    }

    private NativeWindowCaptureStep Capture() => new("capture exact listing cache panel", windows =>
        window = windows.Single(value => value.NativeId == panel.ID && value.RuntimeType == typeof(CacheWindow).FullName));
    private NativeWindowClickActionStep Click(string name, float x, float y) => new(name, window.Handle,
        (window.X + x) * window.UiScale, (window.Y + y) * window.UiScale, null, pointer);
    private WaitUntilStep Wait(string name, Func<bool> predicate) => new(name, _ => predicate(),
        new EndToEndDeadline(int.MaxValue, 120, TimeSpan.FromSeconds(15)));
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private static void ThrowAfter(MethodBase __originalMethod, Listing_Standard __instance)
    {
        if (nesting && __originalMethod.Name == nameof(Listing_Standard.Label))
        {
            nesting = false;
            __instance.Label(string.Join("\n", Enumerable.Repeat("Nested native label", 15)), 60);
        }
        if (throwing == __originalMethod.Name) throw new InvalidOperationException("Owned listing cache failure");
    }

    private sealed class Snapshot
    {
        private readonly Listing_Standard listing;
        private readonly List<Pair<Vector2, Vector2>>? positions;
        private readonly List<Vector2>? seen;
        private readonly Pair<Vector2, Vector2>[]? values;
        private readonly Vector2[]? seenValues;
        internal Snapshot(Listing_Standard listing)
        {
            this.listing = listing;
            positions = (List<Pair<Vector2, Vector2>>?)Positions.GetValue(listing);
            seen = (List<Vector2>?)Seen.GetValue(listing);
            values = positions?.ToArray(); seenValues = seen?.ToArray();
        }
        internal void AssertOriginal()
        {
            EndToEndAssert.True(ReferenceEquals(positions, Positions.GetValue(listing)) && ReferenceEquals(seen, Seen.GetValue(listing)),
                "Hidden listing callbacks changed original cache references/nullness.");
            EndToEndAssert.True((positions == null || positions.SequenceEqual(values!)) && (seen == null || seen.SequenceEqual(seenValues!)),
                "Hidden listing callbacks changed original cache contents.");
        }
    }

    private sealed class CacheWindow : Window
    {
        private static readonly string LongText = string.Join("\n", Enumerable.Range(1, 20).Select(index => $"Line {index:00} — native long-label scrolling"));
        private readonly SettingsReadActionGuards actions;
        private readonly SettingsReadControlGuards controls;
        private readonly SettingsReadListingGuards guards;
        private readonly Listing_Standard listing = new();
        internal int Draws;
        internal bool Requested, Completed;
        internal string? Failure;
        internal float Scroll => ((List<Pair<Vector2, Vector2>>?)Positions.GetValue(listing))?.Single().Second.y ?? 0;
        public override Vector2 InitialSize => new(700, 430);
        internal CacheWindow(SettingsReadActionGuards actions, SettingsReadControlGuards controls, SettingsReadListingGuards guards)
        {
            this.actions = actions; this.controls = controls; this.guards = guards;
            doCloseX = true; closeOnCancel = true; absorbInputAroundWindow = true;
        }
        public override void DoWindowContents(Rect inRect)
        {
            Widgets.Label(new Rect(0, 0, inRect.width, 32), "Settings reader — native long-label scroll state");
            if (Widgets.ButtonText(new Rect(0, 44, 320, 36), "Read reused listing")) Requested = true;
            listing.Begin(new Rect(0, 100, 500, 200));
            try { listing.Label(LongText, 140); }
            finally { listing.End(); }
            Draws++;
            Widgets.Label(new Rect(0, 260, inRect.width, 110), Failure == null
                ? (Completed ? "Hidden read completed; original label scroll retained." : "Scroll down, then read another label without moving this view.")
                : "Hidden read failed: " + Failure);
            if (!Requested || Event.current.type != EventType.Repaint) return;
            Requested = false;
            try { Probe(); }
            catch (Exception error) { Failure = error.Message; }
            finally { Completed = true; }
        }

        private void Probe()
        {
            EndToEndAssert.True(actions.Available && controls.Available && guards.Available);
            var before = new Snapshot(listing);
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
                // Native setup retains a nonempty seen list before End. This is a supporting
                // preservation probe, separate from the player's visible scrolled listing.
                var warm = new Listing_Standard();
                warm.Begin(new Rect(10, 10, 500, 1000));
                warm.Label(LongText, 140);
                var warmBefore = new Snapshot(warm);
                using var scope = actions.BeginRead();
                try
                {
                    warm.Label(LongText + "\nDifferent row", 140);
                    warmBefore.AssertOriginal();
                    nesting = true;
                    warm.Label(LongText, 140);
                    EndToEndAssert.True(!nesting, "The nested native label probe must execute.");
                    warmBefore.AssertOriginal();
                    throwing = nameof(Listing_Standard.Label);
                    bool threw = false;
                    try { warm.Label(LongText, 140); }
                    catch (InvalidOperationException error) when (error.Message == "Owned listing cache failure") { threw = true; }
                    finally { throwing = null; }
                    EndToEndAssert.True(threw);
                    warmBefore.AssertOriginal();
                }
                finally { nesting = false; throwing = null; warm.End(); }
                warmBefore.AssertOriginal();
                DrawHidden(listing, null);
                before.AssertOriginal();
                DrawHidden(listing, nameof(Listing_Standard.Label));
                before.AssertOriginal();
                DrawHidden(listing, nameof(Listing_Standard.End));
                before.AssertOriginal();
                var cold = new Listing_Standard();
                var coldBefore = new Snapshot(cold);
                DrawHidden(cold, null);
                coldBefore.AssertOriginal();
                EndToEndAssert.True(!scope.Failed && !hidden.Failed && hidden.IsBalanced,
                    scope.FailureReason ?? hidden.FailureReason ?? "Listing cache structure imbalance.");
                scope.Dispose();
                ProbeBoundary(false);
                ProbeBoundary(true);
                EndToEndAssert.True(!hidden.Failed && hidden.IsBalanced, hidden.FailureReason ?? "Boundary probe structure imbalance.");
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

        private void ProbeBoundary(bool overLimit)
        {
            var target = new Listing_Standard();
            bool ended = false;
            target.Begin(new Rect(10, 10, 500, 1000));
            try
            {
                // Native initialization, followed by bounded diagnostic setup. One position plus
                // these touched coordinates exercises the aggregate limit across both fields.
                target.Label(LongText, 140);
                var touched = (List<Vector2>)Seen.GetValue(target)!;
                while (touched.Count < (overLimit ? 65536 : 65535)) touched.Add(touched[0]);
                var before = new Snapshot(target);
                using (var read = actions.BeginRead())
                {
                    bool refused = false;
                    try { target.Label(LongText, 140); }
                    catch (InvalidOperationException error) when (error.Message == "settings-read-listing-cache-limit") { refused = true; }
                    before.AssertOriginal();
                    if (overLimit)
                    {
                        EndToEndAssert.True(refused && read.Failed && read.FailureReason == "settings-read-listing-cache-limit",
                            "One entry over the combined bound must refuse and latch before changing native caches.");
                    }
                    else
                    {
                        EndToEndAssert.True(!refused && !read.Failed, "The exact combined bound must remain eligible.");
                        target.End(); ended = true;
                        before.AssertOriginal();
                    }
                }
            }
            finally
            {
                // Refusal intentionally aborts the read; close this diagnostic native group after
                // the read scope has unwound, so the same over-limit End guard cannot strand it.
                if (!ended) target.End();
            }
        }

        private static void DrawHidden(Listing_Standard target, string? failure)
        {
            bool threw = false;
            target.Begin(new Rect(10, 10, 500, 400));
            try
            {
                target.Gap(60);
                throwing = failure;
                try { target.Label(LongText + "\nHidden extra line", 140); }
                catch (InvalidOperationException error) when (error.Message == "Owned listing cache failure") { threw = true; }
            }
            finally
            {
                try { target.End(); }
                catch (InvalidOperationException error) when (error.Message == "Owned listing cache failure") { threw = true; }
                finally { throwing = null; }
            }
            EndToEndAssert.Equal(failure != null, threw, "Original native/foreign exceptions must remain visible.");
        }
    }
}
