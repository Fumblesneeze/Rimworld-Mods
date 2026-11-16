using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Linq;
using HarmonyLib;
using LudeonTK;
using Megamod.EndToEndTests.Diagnostics;
using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.PracticalNeeds.Core;
using Megamod.Features.ReadableNumbers;
using Megamod.Features.SettingsSearch.Core;
using Megamod.Features.SettingsSearch.Runtime;
using Megamod.Settings;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using UnityStubs.Gui;
using Verse;

namespace Megamod.EndToEndTests.SettingsSearch;

// An owned stable-ID reveal component workflow; this does not claim chooser integration or foreign reveal.
[RimWorldEndToEndTest("megamod.settings-search.native-owned-reveal", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "fumblesneeze.megamod",
    MaxFrames = int.MaxValue, MaxGameTicks = 1200, MaxWallClockSeconds = 900)]
public sealed class NativeOwnedSettingsRevealTests : IRimWorldEndToEndTest
{
    private const string Owner = "fumblesneeze.megamod.e2e.owned-settings-reveal";
    private static readonly FieldInfo DialogMod = AccessTools.Field(typeof(Dialog_ModSettings), "mod");
    private static readonly FieldInfo SaveValues = AccessTools.Field(typeof(SettingsStore), "saveValues");
    private static NativeOwnedSettingsRevealTests? active;
    private readonly HashSet<Window> owned = new();
    private SettingsRuntime runtime = null!;
    private SettingsRenderer renderer = null!;
    private MegamodMod mod = null!;
    private Game game = null!;
    private SaveSettingsBinding binding = null!;
    private GlobalSettingsSnapshot globals = null!;
    private SaveSettingsSnapshot saved = null!;
    private SettingsPage page = null!;
    private SettingsPageRecorder ordinary = null!;
    private SettingsNeutralReader reader = null!;
    private NativeDiagnosticSettingsView view = null!;
    private EndToEndNativeWindow window = null!;
    private string pointer = null!;
    private Rect pageRect, button;
    private (Rect UseGlobal, Rect CurrentSave, Rect GlobalDefault) controls;
    private Vector2 beforeScroll, revealedScroll;
    private SettingsRowRevealStatus requestResult;
    private bool queued, consoleRequested, highlighted;
    private int requests, ordinaryAfterRequest, pendingReads, activeReads, cancelledFrames;
    private double highlightedAt = -1, expiredAt = -1;
    private string? failure;

    public void Arrange(IEndToEndContext context)
    {
        EndToEndAssert.True(active == null && Current.Game.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting &&
            Find.MainTabsRoot.OpenTab == null && !Find.WindowStack.IsOpen<Dialog_ModSettings>() &&
            !Find.WindowStack.IsOpen<EditWindow_Log>() && LanguageDatabase.activeLanguage.folderName == "English");
        game = Current.Game;
        runtime = SettingsRuntime.Instance ?? throw new InvalidOperationException("Common settings unavailable.");
        binding = runtime.CurrentBinding ?? throw new InvalidOperationException("Actual save binding unavailable.");
        mod = LoadedModManager.GetMod<MegamodMod>();
        renderer = (SettingsRenderer)(AccessTools.Field(typeof(MegamodMod), "renderer").GetValue(mod)
            ?? throw new InvalidOperationException("Actual renderer unavailable."));
        globals = runtime.Store.ExportGlobals(); saved = runtime.Store.ExportSave(binding);
        EndToEndAssert.True(runtime.SaveLifecycleAvailable && runtime.Store.GetGlobal(PotentialMoodSettings.Enabled) &&
            runtime.Store.UsesGlobal(PotentialMoodSettings.Enabled) && !saved.Values.ContainsKey(PotentialMoodSettings.Enabled.Id));
        EndToEndAssert.Equal(typeof(Dictionary<string, SaveSettingValue>), SaveValues.FieldType);
        page = new("owned-reveal", "owned-reveal-open", mod.SettingsCategory(), mod.Content.Name, null, "fumblesneeze.megamod");
        ordinary = new(page, "ordinary");
        reader = new(message => failure ??= message);
        view = new(() => pointer, PotentialMoodSettings.Enabled.Id);
        beforeScroll = view.Scroll;
        EndToEndAssert.Equal(Vector2.zero, beforeScroll, "The real initial settings viewport must start above the target.");
        var speed = Find.TickManager.CurTimeSpeed;
        var patches = new Harmony(Owner);
        context.DeferCleanup(() =>
        {
            active = null;
            var errors = new List<Exception>();
            void Restore(Action action) { try { action(); } catch (Exception error) { errors.Add(error); } }
            Restore(renderer.CancelReveal);
            Restore(reader.Dispose);
            Restore(() => patches.UnpatchAll(Owner));
            Restore(() =>
            {
                RequireGame();
                var values = (Dictionary<string, SaveSettingValue>)SaveValues.GetValue(runtime.Store)!;
                if (saved.Values.TryGetValue(PotentialMoodSettings.Enabled.Id, out var original)) values[PotentialMoodSettings.Enabled.Id] = original;
                else values.Remove(PotentialMoodSettings.Enabled.Id);
                runtime.Store.LoadGlobals(globals); // Existing Changed subscriber republishes the restored save snapshot.
                EndToEndAssert.True(saved.Values.OrderBy(p => p.Key).SequenceEqual(runtime.Store.ExportSave(binding).Values.OrderBy(p => p.Key)));
            });
            foreach (var candidate in Find.WindowStack.Windows.OfType<Dialog_ModSettings>())
                if (ReferenceEquals(DialogMod.GetValue(candidate), mod)) owned.Add(candidate);
            if (consoleRequested) foreach (var log in Find.WindowStack.Windows.OfType<EditWindow_Log>()) owned.Add(log);
            foreach (var candidate in owned.ToArray()) Restore(() => { if (Find.WindowStack.Windows.Contains(candidate)) candidate.Close(false); });
            Restore(mod.WriteSettings);
            Restore(() => view.RestoreScroll(beforeScroll));
            Restore(() => Find.TickManager.CurTimeSpeed = speed);
            if (errors.Count != 0) throw new AggregateException("Owned reveal cleanup failed.", errors);
        });
        patches.Patch(AccessTools.Method(typeof(Dialog_ModSettings), nameof(Dialog_ModSettings.DoWindowContents), new[] { typeof(Rect) }),
            transpiler: new HarmonyMethod(typeof(NativeOwnedSettingsRevealTests), nameof(ObserveNativeEdge)),
            postfix: new HarmonyMethod(typeof(NativeOwnedSettingsRevealTests), nameof(Addon)));
        active = this;
        Find.TickManager.Pause();
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new ModSettingsActionStep("open the real megamod settings page", "fumblesneeze.megamod");
        yield return new NativePointerScopeBeginStep("own native reveal pointer", value => pointer = value);
        yield return Capture("capture the initial native settings viewport");
        yield return Wait("observe the ordinary registered row label", () => ordinary.LastCapture != null);
        yield return new CheckpointStep("the registered target actually starts offscreen", _ =>
        {
            controls = view.MeasureBooleanControls(out bool visible);
            EndToEndAssert.False(visible);
            EndToEndAssert.True(controls.GlobalDefault.y > window.Height - 36 - 40 - Window.CloseButSize.y && ordinary.LastCapture!.Records.Count(r =>
                r.Kind == SettingsCaptureKind.Text && r.Text == "Consider potential mood for scheduled drugs") == 1);
            AssertValues(true, true, true);
            return view.Evidence(controls.GlobalDefault, visible);
        });
        yield return Shot("before reveal the target is below the ordinary viewport");
        yield return ClickButton("request the offscreen registered row through a native button");
        yield return Wait("observe the first ordinary reveal attempt", () => failure != null ||
            requests == 1 && (highlighted || requestResult == SettingsRowRevealStatus.Unavailable || ordinaryAfterRequest >= 4));
        // The first product checkpoint is after the real button, before metadata assertions.
        yield return new CheckpointStep("stable-ID reveal moves the real viewport without editing", _ =>
        {
            RequireHealthy();
            EndToEndAssert.Equal(SettingsRowRevealStatus.Pending, requestResult, "Actual native reveal request was refused.");
            EndToEndAssert.True(highlighted && view.Scroll.y > beforeScroll.y, "settings-owned-reveal-not-implemented");
            revealedScroll = view.Scroll;
            controls = view.MeasureBooleanControls(out bool visible);
            EndToEndAssert.True(visible && controls.GlobalDefault.y >= 54);
            EndToEndAssert.Equal(1, pendingReads);
            AssertValues(true, true, true);
            return view.Evidence(controls.GlobalDefault, visible);
        });
        yield return Shot("after native reveal the current row is highlighted and unchanged");
        yield return Wait("the original three-second highlight expires", () => failure != null || expiredAt >= 0);
        yield return new CheckpointStep("hidden draws preserve pending and active reveal timing", _ =>
        {
            RequireHealthy();
            EndToEndAssert.Equal(1, activeReads);
            EndToEndAssert.True(expiredAt - highlightedAt >= 2.85 && expiredAt - highlightedAt <= 4.5,
                "Highlight must use three elapsed seconds, including the intervening hidden read.");
            AssertMetadata(true, true, true);
            return new Dictionary<string, string> { ["pendingHiddenReads"] = pendingReads.ToString(),
                ["activeHiddenReads"] = activeReads.ToString(), ["highlightSeconds"] = (expiredAt - highlightedAt).ToString("F3") };
        });
        yield return Shot("three seconds later the same row remains visible without highlight");

        yield return ClickButton("request a second reveal before user navigation");
        yield return Wait("second reveal begins", () => failure != null || requests == 2 && renderer.RevealStatus == SettingsRowRevealStatus.Highlighting);
        // Native vertical scrollbar occupies the rightmost 16 GUI units of the viewport.
        yield return view.Click("native scrollbar navigation cancels reveal", new Vector2((float)window.Width - 36 - 8, 1));
        yield return Wait("cancelled reveal stays cancelled on ordinary repaints", () => failure != null || cancelledFrames >= 6);
        yield return new AssertionStep("navigation cancellation leaves original settings unchanged", _ =>
        {
            RequireHealthy(); EndToEndAssert.Equal(SettingsRowRevealStatus.Cancelled, renderer.RevealStatus);
            EndToEndAssert.True(view.Scroll.y < revealedScroll.y, "Real native navigation must move away without being pulled back.");
            AssertValues(true, true, true);
        });
        yield return ClickButton("reveal the original row again for deliberate editing");
        yield return Wait("the fresh editing reveal resolves", () => failure != null || requests == 3 && renderer.RevealStatus == SettingsRowRevealStatus.Highlighting);

        yield return Capture("measure the original checkbox after reveal");
        yield return view.Click("edit the revealed Global default checkbox natively", controls.GlobalDefault.center);
        yield return new AssertionStep("only the native checkbox edit changes the value", _ => AssertMetadata(false, false, true));
        yield return Shot("the player's original global checkbox edit is visible");
        yield return new WindowCancelActionStep("close the original settings page and persist the edit", typeof(Dialog_ModSettings).FullName!);
        yield return new CheckpointStep("normal native close writes the edited global value", _ => Persisted(false));
        yield return new ModSettingsActionStep("reopen the original settings page", "fumblesneeze.megamod");
        yield return Capture("capture the reopened edited checkbox");
        yield return new AssertionStep("native reopen retains the edited value", _ => AssertMetadata(false, false, true));
        yield return Shot("reopened original checkbox retains the player's edit");

        yield return view.Click("use an independent Current save value", controls.UseGlobal.center);
        yield return new AssertionStep("native inheritance toggle seeds the current value", _ => AssertMetadata(false, false, false));
        yield return Capture("remeasure the original controls after inheritance changes row height");
        yield return view.Click("restore Global default while Current save stays off", controls.GlobalDefault.center);
        yield return new CheckpointStep("metadata distinguishes global current effective and inheritance", _ =>
        {
            AssertMetadata(true, false, false);
            return new Dictionary<string, string> { ["globalRequested"] = "On", ["currentRequested"] = "Off", ["effective"] = "Off", ["usesGlobal"] = "False" };
        });
        yield return Shot("global On and independent current-save Off remain distinct");
        yield return Capture("remeasure the current-save editor before restoring it");
        yield return view.Click("restore the current-save checkbox natively", controls.CurrentSave.center);
        yield return new AssertionStep("current-save edit uses the original editor", _ => AssertMetadata(true, true, false));
        yield return Capture("remeasure native inheritance before restoring it");
        yield return view.Click("restore native Use global inheritance", controls.UseGlobal.center);
        yield return new AssertionStep("restore the initial effective settings", _ => AssertMetadata(true, true, true));
        yield return new WindowCancelActionStep("close the restored original settings page", typeof(Dialog_ModSettings).FullName!);
        yield return new CheckpointStep("normal close persists the restored global baseline", _ => Persisted(true));
        yield return new NativePointerScopeEndStep("restore native reveal pointer", pointer);
        consoleRequested = true;
        yield return new NativeDeveloperLogActionStep("open native developer console after owned reveal", true);
        yield return new NativeWindowCaptureStep("capture the exact native developer console", captures =>
        {
            RequireGame(); var log = Find.WindowStack.Windows.OfType<EditWindow_Log>().Single(); owned.Add(log);
            EndToEndAssert.Equal(1, captures.Count(c => c.NativeId == log.ID && c.RuntimeType == log.GetType().FullName));
        });
        yield return Shot("native console after owned reveal and original editing");
        yield return new NativeDeveloperLogActionStep("close the owned developer console", false);
    }

    private NativeWindowCaptureStep Capture(string name) => new(name, captures =>
    {
        RequireHealthy();
        var actual = Find.WindowStack.Windows.OfType<Dialog_ModSettings>().Single(w => ReferenceEquals(DialogMod.GetValue(w), mod));
        owned.Add(actual);
        window = captures.Single(c => c.NativeId == actual.ID && c.RuntimeType == actual.GetType().FullName);
        view.Bind(window, actual);
        controls = view.MeasureBooleanControls(out _);
    });
    private NativeWindowClickActionStep ClickButton(string name) => new(name, window.Handle,
        (window.X + 18 + button.center.x) * window.UiScale, (window.Y + 18 + button.center.y) * window.UiScale, null, pointer);

    private static void Addon(Dialog_ModSettings __instance, Rect inRect)
    {
        var fixture = active;
        if (fixture == null || SettingsReadScope.Current != null || !ReferenceEquals(DialogMod.GetValue(__instance), fixture.mod)) return;
        fixture.owned.Add(__instance);
        fixture.button = new Rect(inRect.width - 290, 2, 280, 30);
        if (Widgets.ButtonText(fixture.button, "Reveal scheduled recreation mood")) fixture.queued = true;
        if (Event.current.type != EventType.Repaint || fixture.failure != null) return;
        try { fixture.AfterVisibleDraw(); }
        catch (Exception error) { fixture.failure = error.ToString(); }
    }

    private void AfterVisibleDraw()
    {
        if (queued)
        {
            // Wait for neutral-input admission after all visible widget/layout calls return.
            var admission = reader.Read(page, "admission", () => { }, out _, out string? reason);
            if (admission == SettingsNeutralReadStatus.Deferred) return;
            EndToEndAssert.Equal(SettingsNeutralReadStatus.Captured, admission, reason ?? "");
            queued = false; requests++; ordinaryAfterRequest = 0;
            requestResult = renderer.RequestReveal(PotentialMoodSettings.Enabled.Id, runtime.Store.Revision.Activation);
            if (requestResult == SettingsRowRevealStatus.Pending) { HiddenRead(SettingsRowRevealStatus.Pending); pendingReads++; }
            return;
        }
        if (requests == 0) return;
        ordinaryAfterRequest++;
        if (renderer.RevealStatus == SettingsRowRevealStatus.Highlighting)
        {
            highlighted = true;
            if (highlightedAt < 0) highlightedAt = Time.realtimeSinceStartup;
            // A late read makes a restarted highlight clock exceed the original expiry bound.
            if (activeReads == 0 && Time.realtimeSinceStartup - highlightedAt >= 2)
            { HiddenRead(SettingsRowRevealStatus.Highlighting); activeReads++; }
        }
        if (requests == 1 && highlighted && renderer.RevealStatus == SettingsRowRevealStatus.Expired && expiredAt < 0)
            expiredAt = Time.realtimeSinceStartup;
        if (requests == 2 && renderer.RevealStatus == SettingsRowRevealStatus.Cancelled) cancelledFrames++;
    }

    private void HiddenRead(SettingsRowRevealStatus expected)
    {
        var scroll = view.Scroll;
        var revision = runtime.Store.Revision;
        var previousEvent = Event.current;
        var previousBackend = GuiBackend.Current;
        var windows = Find.WindowStack.Windows.ToArray();
        EndToEndAssert.Equal(expected, renderer.RevealStatus);
        var result = reader.Read(page, "reveal-isolation", () => mod.DoSettingsWindowContents(pageRect), out var capture, out string? reason);
        EndToEndAssert.Equal(SettingsNeutralReadStatus.Captured, result, reason ?? "");
        EndToEndAssert.True(capture != null && capture.Status == SettingsCaptureStatus.Partial && capture.Records.Any(r =>
            r.Kind == SettingsCaptureKind.Text && r.Text == "Consider potential mood for scheduled drugs"));
        EndToEndAssert.Equal(expected, renderer.RevealStatus);
        EndToEndAssert.Equal(scroll, view.Scroll);
        EndToEndAssert.Equal(revision, runtime.Store.Revision);
        EndToEndAssert.True(ReferenceEquals(Event.current, previousEvent) && ReferenceEquals(GuiBackend.Current, previousBackend) &&
            SettingsReadScope.Current == null && windows.SequenceEqual(Find.WindowStack.Windows));
        AssertValues(true, true, true);
    }

    private void AssertMetadata(bool global, bool effective, bool useGlobal)
    {
        AssertValues(global, effective, useGlobal);
        var rows = renderer.CaptureRows();
        EndToEndAssert.True(rows.Count > 0 && rows.Count <= 2048 && rows.Select(r => r.Id).Distinct().Count() == rows.Count);
        var target = rows.Single(r => r.Id == PotentialMoodSettings.Enabled.Id);
        string Display(bool value) => (value ? "Megamod_Settings_On" : "Megamod_Settings_Off").Translate();
        EndToEndAssert.Equal("Consider potential mood for scheduled drugs", target.Label);
        EndToEndAssert.Equal("Megamod.PotentialMood.EnabledHelp".Translate().ToString(), target.Help);
        EndToEndAssert.True(target.Visible && target.CategoryId == "recreation-care" && target.Category == "Recreation and care" &&
            target.Scope == SettingScope.GlobalAndSave && target.Revision == runtime.Store.Revision && target.UsesGlobal == useGlobal);
        EndToEndAssert.Equal(SettingApplicationTiming.NextEligibleOccurrence, target.ApplicationTiming);
        EndToEndAssert.Equal(Display(global), target.GlobalRequested.Text);
        EndToEndAssert.Equal(Display(effective), target.CurrentSaveRequested!.Text);
        EndToEndAssert.Equal(Display(effective), target.Effective.Text);
        EndToEndAssert.True(target.EffectiveOffReason == null && target.GlobalRequested.ErrorKey == null && target.Effective.ErrorKey == null);
        var composite = rows.Single(r => r.Id == ReadableNumbersSettings.Grouping.Id);
        EndToEndAssert.True(composite.GlobalRequested.Text == null && composite.CurrentSaveRequested!.Text == null &&
            composite.Effective.Text == null, "An unformatted composite is unknown, not its persistence encoding.");
        EndToEndAssert.False(rows.Any(r => r.Id == "messages.preview" || r.Id == "content.navigation"), "Sections are not fake settings.");
    }

    private void AssertValues(bool global, bool effective, bool useGlobal)
    {
        RequireHealthy();
        EndToEndAssert.Equal(global, runtime.Store.GetGlobal(PotentialMoodSettings.Enabled));
        EndToEndAssert.Equal(effective, runtime.Store.GetEffective(PotentialMoodSettings.Enabled));
        EndToEndAssert.Equal(useGlobal, runtime.Store.UsesGlobal(PotentialMoodSettings.Enabled));
        EndToEndAssert.True(globals.Values.Where(p => p.Key != PotentialMoodSettings.Enabled.Id).OrderBy(p => p.Key)
            .SequenceEqual(runtime.Store.ExportGlobals().Values.Where(p => p.Key != PotentialMoodSettings.Enabled.Id).OrderBy(p => p.Key)));
        EndToEndAssert.True(saved.Values.Where(p => p.Key != PotentialMoodSettings.Enabled.Id).OrderBy(p => p.Key)
            .SequenceEqual(runtime.Store.ExportSave(binding).Values.Where(p => p.Key != PotentialMoodSettings.Enabled.Id).OrderBy(p => p.Key)));
    }

    private IReadOnlyDictionary<string, string> Persisted(bool expected)
    {
        RequireHealthy();
        var paths = Directory.GetFiles(GenFilePaths.ConfigFolderPath, "Mod_*.xml")
            .Where(p => Path.GetFileName(p).Equals("Mod_fumblesneeze.megamod_MegamodMod.xml", StringComparison.OrdinalIgnoreCase)).ToArray();
        EndToEndAssert.Equal(1, paths.Length, "Normal close must write the owned original settings file.");
        var values = XDocument.Load(paths[0]).Descendants("ModSettings").Single().Element("values")!;
        var keys = values.Element("keys")!.Elements("li").Select(e => e.Value).ToArray();
        var stored = values.Element("values")!.Elements("li").Select(e => e.Value).ToArray();
        EndToEndAssert.Equal(keys.Length, stored.Length);
        int index = Array.IndexOf(keys, PotentialMoodSettings.Enabled.Id);
        EndToEndAssert.True(index >= 0);
        EndToEndAssert.Equal(expected, bool.Parse(stored[index]));
        return new Dictionary<string, string> { ["settingsFile"] = Path.GetFileName(paths[0]), ["persistedEnabled"] = expected.ToString() };
    }

    private void RequireGame() => EndToEndAssert.True(ReferenceEquals(Current.Game, game) && game.PlayerHasControl &&
        !LongEventHandler.AnyEventNowOrWaiting && ReferenceEquals(runtime.CurrentBinding, binding) && !binding.IsRevoked);
    private void RequireHealthy() { RequireGame(); EndToEndAssert.True(failure == null, failure ?? ""); }
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private static WaitUntilStep Wait(string name, Func<bool> predicate) => new(name, _ => predicate(),
        new EndToEndDeadline(int.MaxValue, 1200, TimeSpan.FromSeconds(15)));

    private static IEnumerable<CodeInstruction> ObserveNativeEdge(IEnumerable<CodeInstruction> instructions)
    {
        var method = AccessTools.Method(typeof(Mod), nameof(Mod.DoSettingsWindowContents), new[] { typeof(Rect) });
        int found = 0;
        foreach (var instruction in instructions)
            if (instruction.Calls(method))
            {
                found++;
                yield return new CodeInstruction(instruction) { opcode = OpCodes.Call,
                    operand = AccessTools.Method(typeof(NativeOwnedSettingsRevealTests), nameof(DrawOriginal)) };
            }
            else yield return instruction;
        if (found != 1) throw new InvalidOperationException("Expected exactly one ordinary native settings draw edge.");
    }
    private static void DrawOriginal(Mod instance, Rect rect)
    {
        var fixture = active;
        if (fixture == null || !ReferenceEquals(instance, fixture.mod)) { instance.DoSettingsWindowContents(rect); return; }
        fixture.pageRect = rect;
        fixture.ordinary.Observe(() => instance.DoSettingsWindowContents(rect));
    }
}
