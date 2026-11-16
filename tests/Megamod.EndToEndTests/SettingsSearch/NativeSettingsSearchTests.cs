using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Linq;
using HarmonyLib;
using LudeonTK;
using Megamod.EndToEndTests.Diagnostics;
using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.PracticalNeeds.Core;
using Megamod.Features.SettingsSearch;
using Megamod.Features.SettingsSearch.Core;
using Megamod.Features.SettingsSearch.Runtime;
using Megamod.Settings;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace Megamod.EndToEndTests.SettingsSearch;

// Real Options query/result clicks drive production discovery and reveal. The PUAH option and XML
// provider pages are genuine installed pages. Appended test-owned checkboxes and native multirow
// tabs exercise duplicate/conditional/tab resolution; they are not upstream PUAH functionality.
// Expanded budget: 127 native input/capture/close/log/scope operations * 15s + 21 screenshots * 30s
// + 31 waits * 15s + 300s shared iterator/assertion reserve = 3300s (220 steps). The reserve is a
// scenario bound, not a per-callback guarantee. Paused UI has no assumed frame-rate cap.
[RimWorldEndToEndTest("megamod.settings-search.native-search", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "imranfish.xmlextensions", "Dreadarm.PickUpAndHaul",
    "fumblesneeze.megamod", "fumblesneeze.megamodsettingscatalogfixture",
    MaxFrames = int.MaxValue, MaxGameTicks = 1200, MaxWallClockSeconds = 3300)]
public sealed class NativeSettingsSearchTests : IRimWorldEndToEndTest
{
    private const string Owner = "fumblesneeze.megamod.e2e.settings-search";
    private const string PuahPackage = "Dreadarm.PickUpAndHaul";
    private const string FixturePackage = "fumblesneeze.megamodsettingscatalogfixture";
    private const string ProviderWindow = "XmlExtensions.ModSettingsWindow";
    private const string DuplicateLabel = "Ambiguous reveal fixture";
    private const string ConditionalLabel = "Conditional reveal fixture";
    private const string ArmLabel = "Hide conditional fixture on next opening";
    private const string TabbedLabel = "Tabbed amber fixture";
    private const string MoodLabel = "Consider potential mood for scheduled drugs";
    private static readonly FieldInfo DialogMod = AccessTools.Field(typeof(Dialog_ModSettings), "mod");
    private static NativeSettingsSearchTests? active;
    private readonly HashSet<Window> owned = new();
    private readonly Dictionary<FieldInfo, object?> puahValues = new();
    private readonly Dictionary<string, string> providerValues = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Mod> providerTwins = new(StringComparer.Ordinal);
    private readonly SettingsPageRecorder providerRecorder = new(new("search-provider", "search-provider", null, null, null), "ordinary");
    private SettingsSearchController Search => SettingsSearchController.Instance ?? throw new InvalidOperationException("Production search controller unavailable.");
    private SettingsRuntime runtime = null!;
    private MegamodMod megamod = null!;
    private SettingsRenderer renderer = null!;
    private GlobalSettingsSnapshot globals = null!;
    private SaveSettingsSnapshot saved = null!;
    private SaveSettingsBinding binding = null!;
    private Mod puah = null!, xmlMod = null!;
    private object puahSettings = null!;
    private List<string> denied = null!;
    private string[] deniedValues = null!;
    private FieldInfo puahScroll = null!;
    private object originalPuahScroll = null!;
    private FieldInfo activeMenu = null!, selectedMod = null!, oldCache = null!;
    private object? initialMenu, initialSelected, initialCache;
    private KeyValuePair<string, string>[] initialCacheEntries = Array.Empty<KeyValuePair<string, string>>();
    private MethodInfo getSetting = null!, setSetting = null!, drawMenu = null!;
    private NativeDiagnosticSettingsView moodView = null!, navigationView = null!;
    private Game game = null!;
    private Dialog_Options options = null!;
    private EndToEndNativeWindow window = null!;
    private string pointer = null!;
    private bool originalPuah, consoleRequested, duplicateA, duplicateB, conditionalValue, armConditional;
    private bool conditionalVisible = true;
    private int fixtureTab;
    private bool amberFixture, cobaltFixture;
    private bool measureTabHighlight;
    private double highlightStarted = -1, highlightEnded = -1, lastHighlightRepaint = -1, maximumHighlightGap;
    private int highlightRepaints;
    private string? highlightEndStatus;
    private int armedDialog = -1, hiddenPuahCalls, chooserRepaints;
    private string? openedTwin;
    private Vector2 originalOwnedScroll;

    public void Arrange(IEndToEndContext context)
    {
        game = Current.Game;
        EndToEndAssert.True(active == null && game.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting &&
            Find.MainTabsRoot.OpenTab == null && LanguageDatabase.activeLanguage.folderName == "English" &&
            !Find.WindowStack.Windows.Any(IsScenarioWindow));
        runtime = SettingsRuntime.Instance ?? throw new InvalidOperationException("Common settings unavailable.");
        binding = runtime.CurrentBinding ?? throw new InvalidOperationException("Current save binding unavailable.");
        globals = runtime.Store.ExportGlobals(); saved = runtime.Store.ExportSave(binding);
        EndToEndAssert.True(runtime.Store.GetGlobal(PotentialMoodSettings.Enabled) && runtime.Store.UsesGlobal(PotentialMoodSettings.Enabled) &&
            runtime.Store.GetGlobal(ModSettingsNavigationSettings.Enabled) && runtime.Store.UsesGlobal(ModSettingsNavigationSettings.Enabled));
        megamod = LoadedModManager.GetMod<MegamodMod>();
        renderer = (SettingsRenderer)Field(typeof(MegamodMod), "renderer").GetValue(megamod)!;
        moodView = new(() => pointer, PotentialMoodSettings.Enabled.Id);
        navigationView = new(() => pointer, ModSettingsNavigationSettings.Enabled.Id);
        originalOwnedScroll = moodView.Scroll;
        puah = LoadedModManager.ModHandles.Single(m => string.Equals(m.Content.PackageIdPlayerFacing, PuahPackage, StringComparison.OrdinalIgnoreCase));
        puahSettings = puah.GetType().GetProperty("Settings", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        foreach (var field in puahSettings.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)
            .Where(f => f.FieldType == typeof(bool) || f.FieldType == typeof(int))) puahValues.Add(field, field.GetValue(puahSettings));
        EndToEndAssert.Equal(12, puahValues.Count);
        denied = (List<string>)PuahField("DeniedStorage").GetValue(puahSettings)!; deniedValues = denied.ToArray();
        puahScroll = Field(puah.GetType(), "scrollPosition");
        EndToEndAssert.Equal(typeof(Vector2), puahScroll.FieldType); originalPuahScroll = puahScroll.GetValue(puah)!;
        originalPuah = (bool)PuahField("OnTheWay").GetValue(puahSettings)!;
        xmlMod = LoadedModManager.ModHandles.Single(m => m.GetType() == RequiredType("XmlExtensions.XmlMod"));
        getSetting = Method(RequiredType("XmlExtensions.SettingsManager"), "GetSetting", typeof(string), typeof(string));
        setSetting = Method(RequiredType("XmlExtensions.SettingsManager"), "SetSetting", typeof(string), typeof(string), typeof(string));
        drawMenu = Method(RequiredType("XmlExtensions.SettingsMenuDef"), "DrawSettingsMenu", typeof(Rect));
        var providerBase = RequiredType("XmlExtensions.BaseSettingsWindow");
        activeMenu = Field(providerBase, "activeMenu"); selectedMod = Field(providerBase, "SelectedMod"); oldCache = Field(providerBase, "oldValuesCache");
        initialMenu = activeMenu.GetValue(null); initialSelected = selectedMod.GetValue(null); initialCache = oldCache.GetValue(null);
        if (initialCache is Dictionary<string, string> cache) initialCacheEntries = cache.ToArray();
        foreach (string name in new[] { "amber", "cobalt" })
        {
            string defName = "MegamodCatalog" + char.ToUpperInvariant(name[0]) + name[1..];
            var matches = LoadedModManager.ModHandles.Where(mod => mod.Content?.PackageId == FixturePackage &&
                (mod.GetType().GetField("settingsMenuDef", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as Def)?.defName == defName).ToArray();
            EndToEndAssert.Equal(1, matches.Length, "Actual provider startup must emit the independent " + name + " page.");
            providerTwins.Add(name, matches[0]);
            providerValues.Add(name, ProviderValue(name));
            EndToEndAssert.False(bool.Parse(providerValues[name]));
        }
        EndToEndAssert.True(!ReferenceEquals(providerTwins["amber"], providerTwins["cobalt"]) &&
            ReferenceEquals(providerTwins["amber"].Content, providerTwins["cobalt"].Content));
        var speed = Find.TickManager.CurTimeSpeed;
        var patches = new Harmony(Owner);
        context.DeferCleanup(() =>
        {
            active = null;
            var errors = new List<Exception>();
            void Restore(Action action) { try { action(); } catch (Exception error) { errors.Add(error); } }
            Restore(() => patches.UnpatchAll(Owner));
            Restore(renderer.CancelReveal);
            foreach (var pair in puahValues) Restore(() => pair.Key.SetValue(puahSettings, pair.Value));
            Restore(() => { PuahField("DeniedStorage").SetValue(puahSettings, denied); denied.Clear(); denied.AddRange(deniedValues); });
            foreach (var pair in providerValues) Restore(() => setSetting.Invoke(null, new object[] { ModId(pair.Key), "enabled", pair.Value }));
            Restore(() => { RequireGame(); runtime.Store.LoadGlobals(globals); AssertSaveUnchanged(); });
            foreach (var candidate in Find.WindowStack.Windows.Where(w => IsScenarioWindow(w) && (w is not EditWindow_Log || consoleRequested))) owned.Add(candidate);
            Restore(() => { if (Find.MainTabsRoot.OpenTab?.TabWindow is MainTabWindow_Menu) Find.MainTabsRoot.EscapeCurrentTab(false); });
            foreach (var candidate in owned.ToArray()) Restore(() => { if (Find.WindowStack.Windows.Contains(candidate)) candidate.Close(false); });
            Restore(() =>
            {
                if (initialCache is Dictionary<string, string> values)
                { values.Clear(); foreach (var pair in initialCacheEntries) values.Add(pair.Key, pair.Value); }
                activeMenu.SetValue(null, initialMenu); selectedMod.SetValue(null, initialSelected); oldCache.SetValue(null, initialCache);
            });
            Restore(puah.WriteSettings); Restore(xmlMod.WriteSettings); Restore(megamod.WriteSettings);
            Restore(() => puahScroll.SetValue(puah, originalPuahScroll));
            Restore(() => moodView.RestoreScroll(originalOwnedScroll));
            // Retire the test-only controls from process-local search after removing our patches.
            Restore(() => SettingsSearchController.Instance?.RequestStartupDiscovery());
            Restore(() => Find.TickManager.CurTimeSpeed = speed);
            if (errors.Count != 0) throw new AggregateException("Combined settings-search cleanup failed.", errors);
        });
        patches.Patch(Method(puah.GetType(), nameof(Mod.DoSettingsWindowContents), typeof(Rect)),
            prefix: new HarmonyMethod(typeof(NativeSettingsSearchTests), nameof(BeforePuah)),
            postfix: new HarmonyMethod(typeof(NativeSettingsSearchTests), nameof(AfterPuah)));
        patches.Patch(Method(RequiredType(ProviderWindow), nameof(Window.DoWindowContents), typeof(Rect)),
            transpiler: new HarmonyMethod(typeof(NativeSettingsSearchTests), nameof(ObserveProvider)));
        patches.Patch(Method(typeof(Dialog_Options), nameof(Dialog_Options.DoWindowContents), typeof(Rect)),
            postfix: new HarmonyMethod(typeof(NativeSettingsSearchTests), nameof(CountChooserRepaint)));
        patches.Patch(Method(typeof(Dialog_ModSettings), nameof(Dialog_ModSettings.DoWindowContents), typeof(Rect)),
            postfix: new HarmonyMethod(typeof(NativeSettingsSearchTests), nameof(ObserveForeignHighlight)));
        active = this;
        Find.TickManager.Pause();
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new NativePointerScopeBeginStep("own combined search pointer", value => pointer = value);
        foreach (var step in OpenOptions()) yield return step;
        // The fixture is installed after startup discovery: request its normal production refresh.
        yield return ClickControl("Refresh", "refresh all real pages through the production control");
        yield return Wait("production discovery completes", () => !Search.Discovering && Search.Pages.Count != 0);
        yield return new CheckpointStep("all pages remain eligible and XML coverage gaps remain explicit", _ => Coverage());
        yield return Shot("native chooser after discovery before the first typed query");
        foreach (var step in Query("\"Catalog amber option\"")) yield return step;
        yield return new AssertionStep("unread provider options are not invented as search results", _ =>
            EndToEndAssert.Equal(0, Search.DrawnRows.Count(row => row.Entry != null)));
        yield return Shot("no provider option hit still shows the incomplete coverage notice");

        foreach (var step in Query("\"" + MoodLabel + "\"")) yield return step;
        yield return Shot("owned label-only query finds an option beyond the initial page viewport");
        yield return ClickOption(MoodLabel);
        yield return CaptureMod(megamod);
        yield return Wait("owned option is revealed by production selection", () => Search.RevealStatus == "Highlighting");
        yield return new CheckpointStep("owned selection reveals without editing", _ =>
        { AssertCommon(true, true); return OwnedEvidence(moodView); });
        yield return Shot("the owned result opens and highlights its real original row");
        yield return ClickOwned(moodView, "change the real Global default checkbox");
        yield return new AssertionStep("only the original owned editor changes the setting", _ => AssertCommon(false, true));
        yield return CloseMod("close owned settings through native cancel");
        yield return new CheckpointStep("owned normal close persists its edit", _ => PersistedOwned(false));
        yield return CaptureOptions();
        yield return ClickOption(MoodLabel);
        yield return CaptureMod(megamod);
        yield return new AssertionStep("owned reopen retains the edit", _ => AssertCommon(false, true));
        yield return Shot("the reopened owned Global default remains edited before restoration");
        yield return ClickOwned(moodView, "restore the original Global default checkbox");
        yield return CloseMod("persist restored owned settings");
        yield return new CheckpointStep("restored owned value is persisted", _ => PersistedOwned(true));

        // A separate fresh request makes view-lifetime cancellation observable before its timer ends.
        yield return CaptureOptions();
        yield return ClickOption(MoodLabel);
        yield return Wait("fresh owned reveal starts before closing the view", () => Search.RevealStatus == "Highlighting");
        yield return CloseMod("close a view with an active owned reveal");
        yield return new AssertionStep("closing a view cancels its reveal", _ =>
            EndToEndAssert.Equal(SettingsRowRevealStatus.Cancelled, renderer.RevealStatus));
        yield return new ModSettingsActionStep("reopen the same real page without selecting a result", "fumblesneeze.megamod");
        yield return CaptureMod(megamod);
        yield return new AssertionStep("ordinary reopen cannot revive the previous request", _ =>
        { EndToEndAssert.Equal(SettingsRowRevealStatus.Cancelled, renderer.RevealStatus); AssertCommon(true, true); });
        yield return Shot("ordinary reopen retains the viewport without reviving its highlight");
        yield return CloseMod("return to the shared chooser after cancellation");
        yield return CaptureOptions();

        foreach (var step in Query("\"Haul on the way\"")) yield return step;
        yield return new AssertionStep("foreign option survives without a matching page title", _ =>
        {
            EndToEndAssert.Equal(1, OptionRows("Haul on the way").Length);
            EndToEndAssert.True(!puah.SettingsCategory().Contains("Haul on the way", StringComparison.OrdinalIgnoreCase));
        });
        yield return ClickOption("Haul on the way");
        yield return CaptureMod(puah);
        yield return Wait("foreign original checkbox is freshly revealed", () => Search.RevealStatus == "Highlighting");
        SettingsForeignOption retainedOffscreen = null!;
        yield return new AssertionStep("foreign result selection never changes its values and ordinary culling leaves an indexed control offscreen", _ =>
        { AssertPuah(originalPuah); retainedOffscreen = FindOffscreenRootCheckbox(); });
        yield return Shot("search reveals the actual unfamiliar PUAH checkbox");
        yield return ClickForeign("Haul on the way", "edit the actual foreign checkbox");
        yield return new AssertionStep("original PUAH editor makes the sole intended change", _ => AssertPuah(!originalPuah));
        yield return CloseMod("close original PUAH settings and persist the edit");
        yield return new CheckpointStep("foreign close persists through its own settings file", _ => PersistedPuah(!originalPuah));
        yield return CaptureOptions();
        yield return ClickOption("Haul on the way");
        yield return CaptureMod(puah);
        yield return new AssertionStep("foreign reopen retains the edit", _ => AssertPuah(!originalPuah));
        yield return Shot("the reopened original PUAH checkbox remains edited before restoration");
        yield return ClickForeign("Haul on the way", "restore the actual foreign checkbox");
        yield return CloseMod("persist the restored PUAH option before tab navigation");
        yield return new CheckpointStep("restored foreign preference is persisted", _ => PersistedPuah(originalPuah));
        yield return CaptureOptions();
        foreach (var step in Query("\"" + retainedOffscreen.Entry.Label + "\"")) yield return step;
        yield return new AssertionStep("ordinary culled page draws retain the automatically discovered option and value", _ =>
        {
            var rows = OptionRows(retainedOffscreen.Entry.Label);
            EndToEndAssert.Equal(1, rows.Length);
            EndToEndAssert.Equal(retainedOffscreen.Entry.DisplayedValue, rows[0].Entry!.DisplayedValue);
        });
        yield return ClickOption(retainedOffscreen.Entry.Label);
        yield return CaptureMod(puah);
        yield return Wait("the retained offscreen checkbox still has a typed native reveal target", () => Search.RevealStatus == "Highlighting");
        yield return new AssertionStep("revealing the retained checkbox leaves all actual PUAH values unchanged", _ => AssertPuah(originalPuah));
        yield return Shot("an automatically captured checkbox survives ordinary culling and is visibly revealed");
        yield return CloseMod("return after the retained offscreen option reveal");
        yield return CaptureOptions();
        foreach (var step in Query("tab:\"Tab amber\" \"" + TabbedLabel + "\"")) yield return step;
        yield return new AssertionStep("discovery visits a native fixture tab and restores its original selection", _ =>
        { EndToEndAssert.Equal(0, fixtureTab); EndToEndAssert.Equal(1, OptionRows(TabbedLabel).Length); measureTabHighlight = true; });
        yield return ClickOption(TabbedLabel);
        yield return CaptureMod(puah);
        yield return Wait("result navigation selects the actual observed native tab", () => Search.RevealStatus == "Highlighting" && fixtureTab == 1);
        yield return new AssertionStep("native tab reveal never edits a checkbox", _ =>
        { EndToEndAssert.True(!amberFixture && !cobaltFixture); AssertPuah(originalPuah); });
        yield return Shot("an observed native TabDrawer callback reveals the original fixture control");
        yield return Wait("observe the highlight ending on ordinary native repaints", () => highlightEnded >= 0);
        yield return new CheckpointStep("retain highlight duration at the actual native repaint rate", _ => new Dictionary<string, string>
        {
            ["elapsedSeconds"] = (highlightEnded - highlightStarted).ToString("F6", CultureInfo.InvariantCulture),
            ["visibleRepaints"] = highlightRepaints.ToString(CultureInfo.InvariantCulture),
            ["maximumRepaintGapSeconds"] = maximumHighlightGap.ToString("F6", CultureInfo.InvariantCulture),
            ["terminalStatus"] = highlightEndStatus ?? "missing"
        });
        yield return new AssertionStep("the foreign highlight lasts three elapsed seconds rather than a fixed number of repaints", _ =>
        {
            EndToEndAssert.Equal("Expired", highlightEndStatus);
            EndToEndAssert.True(highlightStarted >= 0 && highlightRepaints > 1);
            double elapsed = highlightEnded - highlightStarted;
            double tolerance = Math.Max(0.05, maximumHighlightGap);
            EndToEndAssert.True(Math.Abs(elapsed - 3) <= tolerance,
                "The first visible-to-expired observation must be three seconds within one observed repaint interval (minimum 50ms clock/callback tolerance).");
            measureTabHighlight = false;
        });
        yield return Shot("the same native tab remains open after its three-second highlight disappears");
        yield return ClickForeignTab("Reveal fixture");
        yield return Wait("native manual tab navigation returns to the prerequisite control", () => fixtureTab == 0 &&
            Search.CurrentForeignFrame?.Options.Any(item => item.Entry.Label == ArmLabel) == true);
        yield return ClickForeign(ArmLabel, "arm conditional removal through its real fixture checkbox");
        yield return new AssertionStep("native prerequisite action leaves its target visible until reopening", _ =>
        { EndToEndAssert.True(armConditional && conditionalVisible && armedDialog >= 0); AssertPuah(originalPuah); });
        yield return CloseMod("return to search with the last observed conditional option");
        yield return CaptureOptions();

        foreach (var step in Query("\"" + ConditionalLabel + "\"")) yield return step;
        yield return ClickOption(ConditionalLabel);
        yield return CaptureMod(puah);
        yield return Wait("missing current control reports an unavailable reveal", () => Search.RevealStatus == "Unavailable");
        yield return new CheckpointStep("stale conditional result opens the correct page without guessing", _ => FallbackEvidence("missing"));
        yield return Shot("the missing conditional control reports unavailable on its original page");
        yield return CloseMod("return from the missing-control fallback");
        yield return CaptureOptions();
        foreach (var step in Query("\"" + DuplicateLabel + "\"")) yield return step;
        yield return new AssertionStep("duplicate observations retain separate search occurrences", _ =>
            EndToEndAssert.Equal(2, OptionRows(DuplicateLabel).Length));
        yield return ClickOption(DuplicateLabel, 0);
        yield return CaptureMod(puah);
        yield return Wait("ambiguous current controls refuse automatic reveal", () => Search.RevealStatus == "Unavailable");
        yield return new CheckpointStep("ambiguous reveal never chooses or edits a nearby control", _ => FallbackEvidence("ambiguous"));
        yield return Shot("two identical native controls leave a visible manual-navigation fallback");
        yield return CloseMod("return from the ambiguous-control fallback");
        yield return CaptureOptions();

        foreach (var step in Query("\"Catalog twins\"")) yield return step;
        yield return new AssertionStep("both real same-title provider pages remain separate results", _ => EndToEndAssert.Equal(2, TwinRows().Length));
        yield return Shot("two independent provider page results survive failed option discovery");
        var visitedTwins = new HashSet<string>(StringComparer.Ordinal);
        for (int ordinal = 0; ordinal < 2; ordinal++)
        {
            SettingsSearchDrawnRow selectedTwin = null!;
            string expectedTwin = null!;
            yield return new AssertionStep("bind provider result " + ordinal + " to its original native identity", _ =>
            {
                selectedTwin = TwinRows()[ordinal];
                expectedTwin = ResolveTwin(selectedTwin);
                EndToEndAssert.True(!visitedTwins.Contains(expectedTwin), "A second result must route to the other actual provider page.");
            });
            yield return ClickTwin(selectedTwin.Page, expectedTwin, "open the original " + expectedTwin + " provider result");
            yield return CaptureProvider(expectedTwin);
            visitedTwins.Add(expectedTwin);
            yield return Wait("observe the provider's original checkbox", () => ProviderControl() != null);
            yield return ClickRect("edit the original " + openedTwin + " provider checkbox", ProviderBounds());
            yield return new AssertionStep("only the chosen provider page changed", _ => AssertProvider(openedTwin));
            yield return Shot("the actual " + openedTwin + " provider checkbox changes through its original control");
            yield return new WindowCancelActionStep("close original provider window", ProviderWindow);
            yield return new CheckpointStep("provider close persists its own independent value", _ => PersistedProvider());
            yield return CaptureOptions();
            yield return ClickTwin(selectedTwin.Page, expectedTwin, "reopen the same original " + expectedTwin + " provider result");
            yield return CaptureProvider(expectedTwin);
            yield return new AssertionStep("provider reopen retains its value", _ => AssertProvider(openedTwin));
            yield return Shot("the original " + openedTwin + " provider page retains its edit after reopening");
            yield return Wait("observe reopened provider checkbox", () => ProviderControl() != null);
            yield return ClickRect("restore the original provider checkbox", ProviderBounds());
            yield return new WindowCancelActionStep("persist restored provider option", ProviderWindow);
            yield return new CheckpointStep("restored independent provider defaults persist", _ =>
            { AssertProvider(null); return PersistedProvider(); });
            yield return CaptureOptions();
        }
        yield return new AssertionStep("both distinct native provider twins were opened and verified", _ =>
            EndToEndAssert.True(visitedTwins.SetEquals(providerTwins.Keys)));

        foreach (var step in Query("\"Organize mod settings pages\"")) yield return step;
        yield return ClickOption("Organize mod settings pages");
        yield return CaptureMod(megamod);
        yield return Wait("reveal the actual page-organization setting", () => Search.RevealStatus == "Highlighting");
        yield return ClickOwned(navigationView, "disable S047 through its original common setting");
        yield return CloseMod("return to search with page sorting disabled");
        yield return CaptureOptions();
        foreach (var step in Query("\"Haul on the way\"")) yield return step;
        yield return new AssertionStep("individual-option search remains available while S047 is off", _ =>
        { AssertCommon(true, false); EndToEndAssert.Equal(1, OptionRows("Haul on the way").Length); });
        yield return Shot("one original chooser still finds foreign options with page sorting off");
        foreach (var step in Query("\"Organize mod settings pages\"")) yield return step;
        yield return ClickOption("Organize mod settings pages");
        yield return CaptureMod(megamod);
        yield return Wait("reveal page organization again while S047 is off", () => Search.RevealStatus == "Highlighting");
        yield return ClickOwned(navigationView, "restore ordinary S047 page organization");
        yield return CloseMod("persist restored page organization");
        yield return CaptureOptions();

        foreach (var step in Query(string.Empty)) yield return step;
        yield return new AssertionStep("empty query restores page browsing without option rows", _ =>
            EndToEndAssert.True(Search.DrawnRows.Count != 0 && Search.DrawnRows.All(row => row.Entry == null)));
        yield return ClickControl("ByAuthor", "change the actual author grouping control");
        yield return ClickControl("ByAuthor", "restore the actual author grouping control");
        var priorIdentities = Search.Pages.Select(page => page.Page.PageIdentity).ToArray();
        yield return ClickControl("Refresh", "refresh the real catalog and option observations");
        bool pendingAtCancel = Search.Discovering;
        yield return ClickControl("Cancel", "cancel through the production discovery control");
        int stoppedCalls = hiddenPuahCalls, cancelledAt = chooserRepaints;
        yield return Wait("observe ordinary repaints after native cancellation", () => chooserRepaints >= cancelledAt + 6);
        yield return new CheckpointStep("cancel stops further automatic calls and reports its exact scope", _ =>
        {
            EndToEndAssert.True(Search.Cancelled && !Search.Discovering);
            EndToEndAssert.Equal(stoppedCalls, hiddenPuahCalls);
            return new Dictionary<string, string> { ["pendingAtCancelInput"] = pendingAtCancel.ToString(),
                ["scope"] = pendingAtCancel ? "cancellation requested while discovery was pending" : "cancel after fast completion; no pending-cancellation claim" };
        });
        yield return ClickControl("Refresh", "retry discovery after cancellation");
        yield return Wait("refreshed production discovery finishes", () => !Search.Discovering && !Search.Cancelled && Search.Pages.Count != 0);
        yield return new CheckpointStep("refresh retires old page identities and preserves all settings", _ =>
        {
            EndToEndAssert.True(!Search.Pages.Any(page => priorIdentities.Contains(page.Page.PageIdentity, StringComparer.Ordinal)));
            AssertCommon(true, true); AssertPuah(originalPuah); AssertProvider(null); return Coverage();
        });
        yield return Shot("refreshed chooser preserves normal pages and truthful coverage");
        yield return new WindowCancelActionStep("close the real Options window", typeof(Dialog_Options).FullName!);
        yield return new NativePointerScopeEndStep("restore combined search pointer", pointer);
        consoleRequested = true;
        yield return new NativeDeveloperLogActionStep("open combined search native console", true);
        yield return Shot("native console after combined search and original editing");
        yield return new NativeDeveloperLogActionStep("close combined search native console", false);
    }

    private IEnumerable<EndToEndStep> OpenOptions()
    {
        yield return new EscapeMenuActionStep("open the native Escape menu", true);
        MainTabWindow_Menu menu = null!;
        yield return Capture<MainTabWindow_Menu>("capture actual Escape menu", value => menu = value);
        var labels = new List<string>();
        if (!GameDataSaveLoader.SavingIsTemporarilyDisabled && !game.Info.permadeathMode) labels.Add("Save");
        if ((bool)Field(typeof(MainTabWindow_Menu), "anyGameFiles").GetValue(menu)! && !game.Info.permadeathMode) labels.Add("LoadGame");
        labels.Add("ReviewScenario");
        float y; var font = Text.Font;
        try { Text.Font = GameFont.Small; y = 18 + labels.Sum(key => Math.Max(45, Text.CalcHeight(key.Translate(), 170)) + 7) + Math.Max(45, Text.CalcHeight("Options".Translate(), 170)) / 2; }
        finally { Text.Font = font; }
        yield return ClickRect("open native Options", new Rect((float)window.X + 102, (float)window.Y + y - 1, 2, 2));
        yield return CaptureOptions();
        var categories = DefDatabase<OptionCategoryDef>.AllDefs.Where(d => (Prefs.DevMode || !d.isDev) && d.modContentPack.IsOfficialMod).ToList();
        int index = categories.IndexOf(OptionCategoryDefOf.Mods); EndToEndAssert.True(index >= 0);
        yield return ClickRect("choose native Mod options", new Rect((float)window.X + 97, (float)window.Y + 18 + index * 50 + 23, 2, 2));
        yield return Wait("observe native Mod options and its production search field", () => options.selectedCategory == OptionCategoryDefOf.Mods && Search.ControlRects.ContainsKey("Query"));
        yield return CaptureOptions();
    }

    private IEnumerable<EndToEndStep> Query(string query)
    {
        yield return ClickControl("Query", "focus the original shared query field");
        yield return CaptureOptions();
        EndToEndAssert.True(!string.IsNullOrEmpty(window.FocusedTextControlHandle));
        yield return new NativeWindowReplaceTextActionStep("type the real query: " + query, window.Handle, window.FocusedTextControlHandle!, query);
        int afterTyping = chooserRepaints;
        yield return Wait("observe results from the actual typed query", () => Search.Query == query && chooserRepaints > afterTyping);
    }
    private SettingsSearchDrawnRow[] OptionRows(string label) => Search.DrawnRows.Where(row => row.Entry?.Label == label).ToArray();
    private SettingsSearchDrawnRow[] TwinRows() => Search.DrawnRows.Where(row => row.Entry == null && row.Page.PackageIdentity == FixturePackage).ToArray();
    private SettingsForeignOption FindOffscreenRootCheckbox()
    {
        var discovery = Field(typeof(SettingsSearchController), "discovery").GetValue(Search) as SettingsDiscoveryDriver;
        EndToEndAssert.NotNull(discovery);
        var page = discovery!.CatalogPages.Single(entry => ReferenceEquals(entry.Native, puah)).Page;
        var frame = Search.CurrentForeignFrame;
        EndToEndAssert.True(frame != null && ReferenceEquals(frame.Capture.Page, page));
        var candidates = discovery.ForeignOptions[page]
            .Where(option => option.Target.Kind == SettingsForeignControlKind.Checkbox && option.Target.Tabs.Count == 0 &&
                option.Entry.Label != "Haul on the way" && option.Entry.Label.Length is > 0 and < 400 &&
                !option.Entry.Label.Contains('"') && !frame!.Options.Any(current => current.Entry.Label == option.Entry.Label))
            .GroupBy(option => option.Entry.Label, StringComparer.Ordinal).Where(group => group.Count() == 1)
            .Select(group => group.Single()).OrderBy(option => option.Entry.Label, StringComparer.Ordinal).ToArray();
        EndToEndAssert.True(candidates.Length != 0,
            "The actual PUAH viewport must cull at least one uniquely indexed original root checkbox for this retention case.");
        return candidates[0];
    }
    private string ResolveTwin(SettingsSearchDrawnRow row)
    {
        var discovery = Field(typeof(SettingsSearchController), "discovery").GetValue(Search) as SettingsDiscoveryDriver;
        EndToEndAssert.NotNull(discovery, "The production catalog must already be published.");
        var native = discovery!.CatalogPages.Single(entry => ReferenceEquals(entry.Page, row.Page));
        var source = ((IEnumerable<Mod>)Field(typeof(Dialog_Options), "cachedModsWithSettings").GetValue(options)!).ToArray();
        EndToEndAssert.True(native.SourceOrdinal >= 0 && native.SourceOrdinal < source.Length &&
            ReferenceEquals(native.Native, source[native.SourceOrdinal]), "The result must retain its actual native source occurrence.");
        return providerTwins.Single(pair => ReferenceEquals(pair.Value, native.Native)).Key;
    }
    private NativeWindowClickActionStep ClickTwin(SettingsPage page, string expected, string action)
    {
        var row = TwinRows().Single(candidate => ReferenceEquals(candidate.Page, page));
        EndToEndAssert.Equal(expected, ResolveTwin(row), "The selected result must retain its original provider identity before each click.");
        return ClickRect(action, row.ScreenRect);
    }
    private NativeWindowClickActionStep ClickOption(string label, int occurrence = 0)
    {
        var rows = OptionRows(label); EndToEndAssert.True(rows.Length > occurrence);
        return ClickRect("select the actual option result: " + label, rows[occurrence].ScreenRect);
    }
    private NativeWindowClickActionStep ClickControl(string key, string name) => ClickRect(name, Search.ControlRects[key]);
    private NativeWindowClickActionStep ClickRect(string name, Rect screen)
    {
        EndToEndAssert.True(float.IsFinite(screen.x) && float.IsFinite(screen.y) && screen.width > 0 && screen.height > 0 &&
            screen.center.x >= window.X + 18 && screen.center.x <= window.X + window.Width - 18 &&
            screen.center.y >= window.Y + 18 && screen.center.y <= window.Y + window.Height - 18,
            "Native click must use a currently observed control inside the exact captured window.");
        return new(name, window.Handle, screen.center.x * window.UiScale, screen.center.y * window.UiScale, null, pointer);
    }
    private NativeWindowClickActionStep ClickForeign(string label, string action)
    {
        var option = Search.CurrentForeignFrame?.Options.SingleOrDefault(item => item.Entry.Label == label);
        EndToEndAssert.NotNull(option, "No current ordinary native control observation: " + label);
        var b = option!.Bounds;
        return ClickRect(action, new Rect(b.X, b.Y, b.Width, b.Height));
    }
    private NativeWindowClickActionStep ClickForeignTab(string label)
    {
        var record = Search.CurrentForeignFrame?.Capture.Records.SingleOrDefault(item => item.Kind == SettingsCaptureKind.Text && item.Text == label);
        EndToEndAssert.NotNull(record, "No current ordinary native tab label: " + label);
        var b = record!.Bounds;
        return ClickRect("select the actual native tab: " + label, new Rect(b.X, b.Y, b.Width, b.Height));
    }
    private NativeWindowClickActionStep ClickOwned(NativeDiagnosticSettingsView view, string action)
    {
        var controls = view.MeasureBooleanControls(out bool visible); EndToEndAssert.True(visible);
        return view.Click(action, controls.GlobalDefault.center);
    }
    private IReadOnlyDictionary<string, string> OwnedEvidence(NativeDiagnosticSettingsView view)
    {
        var controls = view.MeasureBooleanControls(out bool visible); EndToEndAssert.True(visible);
        return view.Evidence(controls.GlobalDefault, visible);
    }
    private NativeWindowCaptureStep CaptureOptions() => Capture<Dialog_Options>("capture current native Options", value => options = value);
    private NativeWindowCaptureStep CaptureMod(Mod expected) => Capture<Dialog_ModSettings>("capture original settings page", actual =>
    {
        EndToEndAssert.True(ReferenceEquals(DialogMod.GetValue(actual), expected));
        if (ReferenceEquals(expected, megamod)) { moodView.Bind(window, actual); navigationView.Bind(window, actual); }
    });
    private NativeWindowCaptureStep Capture<T>(string name, Action<T> receive) where T : Window => new(name, captured =>
    { RequireGame(); var actual = Find.WindowStack.Windows.OfType<T>().Single(); Retain(actual, captured); receive(actual); });
    private void Retain(Window actual, IReadOnlyList<EndToEndNativeWindow> captured)
    { owned.Add(actual); window = captured.Single(value => value.NativeId == actual.ID && value.RuntimeType == actual.GetType().FullName); }
    private NativeWindowCaptureStep CaptureProvider(string expected) => new("capture actual independent " + expected + " provider window", captured =>
    {
        var actual = Find.WindowStack.Windows.Single(value => value.GetType().FullName == ProviderWindow); Retain(actual, captured);
        var defName = (activeMenu.GetValue(null) as Def)?.defName;
        string expectedDef = "MegamodCatalog" + char.ToUpperInvariant(expected[0]) + expected[1..];
        EndToEndAssert.Equal(expectedDef, defName, "Native opening must reach the provider identified before the result click.");
        openedTwin = expected;
        EndToEndAssert.False(Find.WindowStack.IsOpen<Dialog_ModSettings>());
    });
    private SettingsCaptureRecord? ProviderControl() => providerRecorder.LastCapture?.Records.SingleOrDefault(row => row.Kind == SettingsCaptureKind.Text && row.Text == "Catalog " + openedTwin + " option");
    private Rect ProviderBounds() { var b = ProviderControl()!.Bounds; return new(b.X, b.Y, b.Width, b.Height); }

    private IReadOnlyDictionary<string, string> Coverage()
    {
        RequireGame(); EndToEndAssert.True(Search.Failure == null, Search.Failure ?? "");
        var twins = Search.Pages.Where(page => page.Page.PackageIdentity == FixturePackage).ToArray();
        EndToEndAssert.Equal(2, twins.Length);
        EndToEndAssert.True(twins[0].Page.PageIdentity != twins[1].Page.PageIdentity && twins.All(page => page.Status == SettingsDiscoveryStatus.Failed));
        EndToEndAssert.True(Search.Pages.Any(page => string.Equals(page.Page.PackageIdentity, PuahPackage, StringComparison.OrdinalIgnoreCase)));
        AssertPuah(originalPuah); AssertProvider(null);
        return new Dictionary<string, string> { ["pages"] = Search.Pages.Count.ToString(), ["providerTwins"] = "2",
            ["providerOptionScope"] = "native constructor refusal is an explicit coverage gap; ordinary opening/editing verified separately" };
    }
    private IReadOnlyDictionary<string, string> FallbackEvidence(string expected)
    {
        EndToEndAssert.True(ReferenceEquals(DialogMod.GetValue(Find.WindowStack.Windows.OfType<Dialog_ModSettings>().Single()), puah));
        EndToEndAssert.True(!string.IsNullOrEmpty(Search.RevealFailure) && Search.RevealFailure.Contains(expected, StringComparison.OrdinalIgnoreCase), Search.RevealFailure ?? "No unavailable reason.");
        EndToEndAssert.True(!duplicateA && !duplicateB && !conditionalValue);
        AssertPuah(originalPuah);
        return new Dictionary<string, string> { ["reason"] = Search.RevealFailure!, ["originalPage"] = puah.SettingsCategory(),
            ["fixtureScope"] = "test-owned native rows exercise resolution only; no fixture checkbox was edited by result selection" };
    }
    private void AssertCommon(bool mood, bool navigation)
    {
        RequireGame(); EndToEndAssert.Equal(mood, runtime.Store.GetGlobal(PotentialMoodSettings.Enabled));
        EndToEndAssert.Equal(mood, runtime.Store.GetEffective(PotentialMoodSettings.Enabled));
        EndToEndAssert.Equal(navigation, runtime.Store.GetEffective(ModSettingsNavigationSettings.Enabled));
        AssertSaveUnchanged();
        EndToEndAssert.True(globals.Values.Where(pair => pair.Key != PotentialMoodSettings.Enabled.Id && pair.Key != ModSettingsNavigationSettings.Enabled.Id).OrderBy(pair => pair.Key)
            .SequenceEqual(runtime.Store.ExportGlobals().Values.Where(pair => pair.Key != PotentialMoodSettings.Enabled.Id && pair.Key != ModSettingsNavigationSettings.Enabled.Id).OrderBy(pair => pair.Key)));
    }
    private void AssertSaveUnchanged() => EndToEndAssert.True(saved.Values.OrderBy(pair => pair.Key).SequenceEqual(runtime.Store.ExportSave(binding).Values.OrderBy(pair => pair.Key)));
    private void AssertPuah(bool onTheWay)
    {
        RequireGame();
        foreach (var pair in puahValues) EndToEndAssert.Equal(pair.Key.Name == "OnTheWay" ? onTheWay : pair.Value, pair.Key.GetValue(puahSettings), pair.Key.Name);
        EndToEndAssert.True(deniedValues.SequenceEqual((List<string>)PuahField("DeniedStorage").GetValue(puahSettings)!));
    }
    private void AssertProvider(string? enabled)
    { foreach (string name in providerValues.Keys) EndToEndAssert.Equal(name == enabled, bool.Parse(ProviderValue(name))); }
    private string ProviderValue(string name) => (string)getSetting.Invoke(null, new object[] { ModId(name), "enabled" })!;
    private IReadOnlyDictionary<string, string> PersistedPuah(bool expected)
    {
        var path = Directory.GetFiles(GenFilePaths.ConfigFolderPath, "Mod_*.xml").Single(file => Path.GetFileName(file).Contains("PickUpAndHaul", StringComparison.OrdinalIgnoreCase));
        string? value = XDocument.Load(path).Descendants("onTheWay").SingleOrDefault()?.Value;
        EndToEndAssert.Equal(expected, value == null || bool.Parse(value));
        return new Dictionary<string, string> { ["settingsFile"] = Path.GetFileName(path), ["onTheWay"] = expected.ToString() };
    }
    private IReadOnlyDictionary<string, string> PersistedOwned(bool expected)
    {
        var path = Directory.GetFiles(GenFilePaths.ConfigFolderPath, "Mod_*.xml").Single(file => Path.GetFileName(file).Equals("Mod_fumblesneeze.megamod_MegamodMod.xml", StringComparison.OrdinalIgnoreCase));
        var values = ReadDictionary(XDocument.Load(path).Descendants("ModSettings").Single().Element("values")!);
        EndToEndAssert.Equal(expected, bool.Parse(values[PotentialMoodSettings.Enabled.Id]));
        return new Dictionary<string, string> { ["settingsFile"] = Path.GetFileName(path), ["ownedEnabled"] = expected.ToString() };
    }
    private IReadOnlyDictionary<string, string> PersistedProvider()
    {
        var path = Directory.GetFiles(GenFilePaths.ConfigFolderPath, "Mod_*.xml").Single(file => Path.GetFileName(file).EndsWith("_XmlMod.xml", StringComparison.OrdinalIgnoreCase));
        var values = ReadDictionary(XDocument.Load(path).Descendants("dataDict").Single());
        foreach (string name in providerValues.Keys) EndToEndAssert.Equal(bool.Parse(ProviderValue(name)), bool.Parse(values[ModId(name) + ";enabled"]));
        return new Dictionary<string, string> { ["settingsFile"] = Path.GetFileName(path), ["amber"] = ProviderValue("amber"), ["cobalt"] = ProviderValue("cobalt") };
    }
    private static Dictionary<string, string> ReadDictionary(XElement element)
    {
        var keys = element.Element("keys")!.Elements("li").Select(value => value.Value).ToArray();
        var values = element.Element("values")!.Elements("li").Select(value => value.Value).ToArray();
        EndToEndAssert.Equal(keys.Length, values.Length);
        return keys.Zip(values, (key, value) => (key, value)).ToDictionary(pair => pair.key, pair => pair.value);
    }

    private static void BeforePuah(ref Rect __0, out Rect __state)
    {
        __state = __0;
        var owner = active; if (owner == null) return;
        if (SettingsReadScope.Current != null) owner.hiddenPuahCalls++;
        else if (owner.armConditional && owner.conditionalVisible)
        {
            var dialog = Find.WindowStack.Windows.OfType<Dialog_ModSettings>().SingleOrDefault(window => ReferenceEquals(DialogMod.GetValue(window), owner.puah));
            if (dialog != null && dialog.ID != owner.armedDialog) owner.conditionalVisible = false;
        }
        EndToEndAssert.True(__0.height > 340, "Fixture requires separate original-page, two-row tab and checkbox regions.");
        __0.height -= 224;
    }
    private static void AfterPuah(Rect __state)
    {
        var owner = active; if (owner == null) return;
        var section = new Rect(__state.x, __state.yMax - 144, __state.width - 20, 140);
        var labels = new[] { "Reveal fixture", "Tab amber", "Tab cobalt" };
        // The selected amber target is on the second native row of one logical TabDrawer group.
        var tabs = new[] { 0, 2, 1 }.Select(index => new TabRecord(labels[index], () => owner.fixtureTab = index, () => owner.fixtureTab == index)).ToList();
        TabDrawer.DrawTabs<TabRecord>(section, tabs, rows: 2, maxTabWidth: 200f);
        float y = __state.yMax - 132;
        Rect Next() { var rect = new Rect(__state.x, y, __state.width - 20, 28); y += 32; return rect; }
        if (owner.fixtureTab == 1) { Widgets.CheckboxLabeled(Next(), TabbedLabel, ref owner.amberFixture); return; }
        if (owner.fixtureTab == 2) { Widgets.CheckboxLabeled(Next(), "Tabbed cobalt fixture", ref owner.cobaltFixture); return; }
        Widgets.CheckboxLabeled(Next(), DuplicateLabel, ref owner.duplicateA);
        Widgets.CheckboxLabeled(Next(), DuplicateLabel, ref owner.duplicateB);
        var conditionalRect = Next();
        if (owner.conditionalVisible) Widgets.CheckboxLabeled(conditionalRect, ConditionalLabel, ref owner.conditionalValue);
        bool before = owner.armConditional;
        Widgets.CheckboxLabeled(Next(), ArmLabel, ref owner.armConditional);
        if (!before && owner.armConditional && SettingsReadScope.Current == null)
            owner.armedDialog = Find.WindowStack.Windows.OfType<Dialog_ModSettings>().Single(window => ReferenceEquals(DialogMod.GetValue(window), owner.puah)).ID;
    }
    private static void CountChooserRepaint(Dialog_Options __instance)
    { if (active != null && SettingsReadScope.Current == null && Event.current.type == EventType.Repaint && __instance.selectedCategory == OptionCategoryDefOf.Mods) active.chooserRepaints++; }
    private static void ObserveForeignHighlight(Dialog_ModSettings __instance)
    {
        var owner = active;
        if (owner == null || !owner.measureTabHighlight || owner.fixtureTab != 1 ||
            SettingsReadScope.Current != null || Event.current?.type != EventType.Repaint ||
            !ReferenceEquals(DialogMod.GetValue(__instance), owner.puah) || owner.highlightEnded >= 0) return;
        double now = Stopwatch.GetTimestamp() / (double)Stopwatch.Frequency;
        string status = owner.Search.RevealStatus;
        if (status == "Highlighting")
        {
            if (owner.highlightStarted < 0) owner.highlightStarted = now;
            if (owner.lastHighlightRepaint >= 0) owner.maximumHighlightGap = Math.Max(owner.maximumHighlightGap, now - owner.lastHighlightRepaint);
            owner.lastHighlightRepaint = now;
            owner.highlightRepaints++;
        }
        else if (owner.highlightStarted >= 0 && status != "Pending")
        {
            owner.maximumHighlightGap = Math.Max(owner.maximumHighlightGap, now - owner.lastHighlightRepaint);
            owner.highlightEnded = now;
            owner.highlightEndStatus = status;
        }
    }
    private static IEnumerable<CodeInstruction> ObserveProvider(IEnumerable<CodeInstruction> instructions)
    {
        var original = Method(RequiredType("XmlExtensions.SettingsMenuDef"), "DrawSettingsMenu", typeof(Rect));
        int count = 0;
        foreach (var instruction in instructions)
            if (instruction.Calls(original))
            { count++; yield return new CodeInstruction(instruction) { opcode = OpCodes.Call, operand = AccessTools.Method(typeof(NativeSettingsSearchTests), nameof(DrawProvider)) }; }
            else yield return instruction;
        if (count != 1) throw new InvalidOperationException("Expected one actual provider menu draw edge.");
    }
    private static void DrawProvider(object menu, Rect rect)
    {
        var owner = active;
        if (owner == null) { Method(RequiredType("XmlExtensions.SettingsMenuDef"), "DrawSettingsMenu", typeof(Rect)).Invoke(menu, new object[] { rect }); return; }
        owner.providerRecorder.Observe(() => owner.drawMenu.Invoke(menu, new object[] { rect }));
    }
    private void RequireGame() => EndToEndAssert.True(ReferenceEquals(Current.Game, game) && game.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting && ReferenceEquals(runtime.CurrentBinding, binding) && !binding.IsRevoked);
    private FieldInfo PuahField(string name) => Field(puahSettings.GetType(), name);
    private static string ModId(string name) => "megamod.catalog.fixture." + name;
    private static bool IsScenarioWindow(Window value) => value is Dialog_Options or Dialog_ModSettings or MainTabWindow_Menu or EditWindow_Log || value.GetType().FullName == ProviderWindow;
    private static FieldInfo Field(Type type, string name) => AccessTools.Field(type, name) ?? throw new MissingFieldException(type.FullName, name);
    private static Type RequiredType(string name) => AccessTools.TypeByName(name) ?? throw new TypeLoadException(name);
    private static MethodInfo Method(Type type, string name, params Type[] arguments) => type.GetMethod(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static,
        null, arguments, null) ?? throw new MissingMethodException(type.FullName, name);
    private static WindowCancelActionStep CloseMod(string name) => new(name, typeof(Dialog_ModSettings).FullName!);
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private static WaitUntilStep Wait(string name, Func<bool> condition) => new(name, _ => condition(), new EndToEndDeadline(int.MaxValue, 1200, TimeSpan.FromSeconds(15)));
}
