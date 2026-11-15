using System.Reflection;
using System.Reflection.Emit;
using System.Xml.Linq;
using HarmonyLib;
using LudeonTK;
using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.SettingsSearch.Core;
using Megamod.Features.SettingsSearch.Runtime;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using UnityStubs.Gui;
using Verse;

namespace Megamod.EndToEndTests.SettingsSearch;

// The addon schedules the public driver after ordinary widgets return. It never supplies page
// callbacks, changes the native catalog, or registers a synthetic Mod with the loader.
[RimWorldEndToEndTest("megamod.settings-search.native-discovery", "fumblesneeze.megamod",
    "brrainz.harmony", "ludeon.rimworld", "imranfish.xmlextensions", "Dreadarm.PickUpAndHaul",
    "fumblesneeze.megamod", "fumblesneeze.megamodsettingscatalogfixture",
    MaxFrames = int.MaxValue, MaxGameTicks = 1200, MaxWallClockSeconds = 700)]
public sealed class NativeSettingsDiscoveryTests : IRimWorldEndToEndTest
{
    private const string Owner = "fumblesneeze.megamod.e2e.settings-discovery";
    private const string PuahPackage = "Dreadarm.PickUpAndHaul";
    private const string FixturePackage = "fumblesneeze.megamodsettingscatalogfixture";
    private const string ProviderWindowName = "XmlExtensions.ModSettingsWindow";
    private const string TwinTitle = "Catalog twins";
    private static readonly string[] Names = { "amber", "cobalt" };
    private static NativeSettingsDiscoveryTests? active;
    private readonly List<Window> owned = new();
    private readonly Dictionary<string, Mod> twins = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> providerValues = new(StringComparer.Ordinal);
    private readonly Dictionary<FieldInfo, object?> scalarValues = new();
    private readonly Dictionary<Mod, int> entries = new(ReferenceEqualityComparer.Instance);
    private Dictionary<Mod, int> generationStart = new(ReferenceEqualityComparer.Instance);
    private readonly SettingsPageRecorder chooser = Recorder("chooser");
    private readonly SettingsPageRecorder ordinary = Recorder("puah");
    private readonly SettingsPageRecorder provider = Recorder("provider");
    private SettingsDiscoveryDriver driver = null!;
    private Mod puah = null!, providerMod = null!;
    private object settings = null!;
    private List<string> denied = null!;
    private string[] deniedValues = null!;
    private MethodInfo drawChooser = null!, drawMenu = null!, getSetting = null!, setSetting = null!;
    private FieldInfo cachedPages = null!, activeMenu = null!, selectedMod = null!, oldValuesCache = null!;
    private ProviderState originalProvider = null!;
    private Game game = null!;
    private Dialog_Options options = null!;
    private EndToEndNativeWindow window = null!;
    private string pointer = null!;
    private string? failure;
    private bool originalPuah, started, consoleRequested, pauseForCancel, cancelReady, cancelled, warmRunning;
    private bool constructorRefreshArmed, constructorRefreshTriggered;
    private enum StartupConstructorAction { None, Cancel, Refresh }
    private StartupConstructorAction startupAction;
    private bool startupRefreshTriggered, startupCancelTriggered, observeStartupCancel;
    private int normalStartupPostOpens, retiredStartupPostOpens, startupCancelEntries, startupCancelRepaints;
    private int refreshes, cancelRepaints, cancelledEntryCount, lastEntryFrame = -1, hiddenCloses, hiddenWrites;
    private SettingsDiscoveryPage[] cancelledPages = Array.Empty<SettingsDiscoveryPage>();

    public void Arrange(IEndToEndContext context)
    {
        game = Current.Game;
        EndToEndAssert.True(active == null && game.PlayerHasControl && !LongEventHandler.AnyEventNowOrWaiting &&
            Find.MainTabsRoot.OpenTab == null && LanguageDatabase.activeLanguage.folderName == "English" &&
            !Find.WindowStack.Windows.Any(IsScenarioWindow));
        puah = LoadedModManager.ModHandles.Single(m => string.Equals(m.Content.PackageIdPlayerFacing, PuahPackage, StringComparison.OrdinalIgnoreCase));
        settings = puah.GetType().GetProperty("Settings", BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!;
        foreach (var field in settings.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)
            .Where(f => f.FieldType == typeof(bool) || f.FieldType == typeof(int)))
            scalarValues.Add(field, field.GetValue(settings));
        EndToEndAssert.Equal(12, scalarValues.Count);
        denied = (List<string>)SettingField("DeniedStorage").GetValue(settings)!;
        deniedValues = denied.ToArray();
        originalPuah = (bool)SettingField("OnTheWay").GetValue(settings)!;
        providerMod = LoadedModManager.ModHandles.Single(m => m.GetType() == RequiredType("XmlExtensions.XmlMod"));
        getSetting = Method(RequiredType("XmlExtensions.SettingsManager"), "GetSetting", typeof(string), typeof(string));
        setSetting = Method(RequiredType("XmlExtensions.SettingsManager"), "SetSetting", typeof(string), typeof(string), typeof(string));
        var baseWindow = RequiredType("XmlExtensions.BaseSettingsWindow");
        activeMenu = Field(baseWindow, "activeMenu"); selectedMod = Field(baseWindow, "SelectedMod"); oldValuesCache = Field(baseWindow, "oldValuesCache");
        originalProvider = SnapshotProvider();
        foreach (string name in Names)
        {
            var matches = LoadedModManager.ModHandles.Where(m => m.Content?.PackageId == FixturePackage &&
                (m.GetType().GetField("settingsMenuDef", BindingFlags.Public | BindingFlags.Static)?.GetValue(null) as Def)?.defName == DefName(name)).ToArray();
            EndToEndAssert.Equal(1, matches.Length, "Actual provider startup must emit " + name);
            twins.Add(name, matches[0]); providerValues.Add(name, GetValue(name));
            EndToEndAssert.Equal(false, bool.Parse(providerValues[name]));
        }
        EndToEndAssert.True(!ReferenceEquals(twins["amber"], twins["cobalt"]) && ReferenceEquals(twins["amber"].Content, twins["cobalt"].Content));
        cachedPages = Field(typeof(Dialog_Options), "cachedModsWithSettings");
        EndToEndAssert.Equal(typeof(IEnumerable<Mod>), cachedPages.FieldType);
        drawChooser = Method(typeof(Dialog_Options), "DoModOptions", typeof(Listing_Standard));
        drawMenu = Method(RequiredType("XmlExtensions.SettingsMenuDef"), "DrawSettingsMenu", typeof(Rect));
        driver = new(message => failure ??= message);
        var patches = new Harmony(Owner);
        var speed = Find.TickManager.CurTimeSpeed;
        context.DeferCleanup(() =>
        {
            active = null;
            var failures = new List<Exception>();
            void Restore(Action action) { try { action(); } catch (Exception error) { failures.Add(error); } }
            Restore(driver.Dispose);
            Restore(() => patches.UnpatchAll(Owner));
            foreach (var pair in scalarValues) Restore(() => pair.Key.SetValue(settings, pair.Value));
            Restore(() => { SettingField("DeniedStorage").SetValue(settings, denied); denied.Clear(); denied.AddRange(deniedValues); });
            foreach (var pair in providerValues) Restore(() => setSetting.Invoke(null, new object[] { ModId(pair.Key), "enabled", pair.Value }));
            owned.AddRange(Find.WindowStack.Windows.Where(w => IsScenarioWindow(w) && (w is not EditWindow_Log || consoleRequested)));
            Restore(() => { if (Find.MainTabsRoot.OpenTab?.TabWindow is MainTabWindow_Menu) Find.MainTabsRoot.EscapeCurrentTab(false); });
            foreach (var item in owned.Distinct().ToArray()) Restore(() => { if (Find.WindowStack.Windows.Contains(item)) item.Close(false); });
            Restore(() => originalProvider.Restore(this));
            Restore(puah.WriteSettings); Restore(providerMod.WriteSettings);
            Restore(() => Find.TickManager.CurTimeSpeed = speed);
            if (failures.Count != 0) throw new AggregateException("Discovery fixture cleanup failed.", failures);
        });
        patches.Patch(Method(typeof(Dialog_Options), "DoOptions", typeof(OptionCategoryDef), typeof(Rect)),
            transpiler: new HarmonyMethod(typeof(NativeSettingsDiscoveryTests), nameof(SurroundChooser)));
        patches.Patch(Method(typeof(Dialog_Options), nameof(Dialog_Options.DoWindowContents), typeof(Rect)),
            postfix: new HarmonyMethod(typeof(NativeSettingsDiscoveryTests), nameof(OptionsAddon)));
        patches.Patch(AccessTools.Constructor(typeof(Dialog_Options), Type.EmptyTypes)
            ?? throw new MissingMethodException(typeof(Dialog_Options).FullName, ".ctor()"),
            postfix: new HarmonyMethod(typeof(NativeSettingsDiscoveryTests), nameof(ReplaceStartupConstruction)));
        patches.Patch(Method(typeof(Dialog_Options), nameof(Dialog_Options.PostOpen)),
            prefix: new HarmonyMethod(typeof(NativeSettingsDiscoveryTests), nameof(ObserveStartupPostOpen)));
        patches.Patch(Method(typeof(Dialog_ModSettings), nameof(Dialog_ModSettings.DoWindowContents), typeof(Rect)),
            transpiler: new HarmonyMethod(typeof(NativeSettingsDiscoveryTests), nameof(SurroundNativePage)));
        patches.Patch(AccessTools.Constructor(typeof(Dialog_ModSettings), new[] { typeof(Mod) })
            ?? throw new MissingMethodException(typeof(Dialog_ModSettings).FullName, ".ctor(Mod)"),
            postfix: new HarmonyMethod(typeof(NativeSettingsDiscoveryTests), nameof(RefreshAfterWrapperConstruction)));
        patches.Patch(Method(RequiredType(ProviderWindowName), "DoWindowContents", typeof(Rect)),
            transpiler: new HarmonyMethod(typeof(NativeSettingsDiscoveryTests), nameof(SurroundProvider)),
            postfix: new HarmonyMethod(typeof(NativeSettingsDiscoveryTests), nameof(ProviderAddon)));
        patches.Patch(Method(typeof(Window), nameof(Window.Close), typeof(bool)), prefix: new HarmonyMethod(typeof(NativeSettingsDiscoveryTests), nameof(ObserveClose)));
        patches.Patch(Method(typeof(Mod), nameof(Mod.WriteSettings)), prefix: new HarmonyMethod(typeof(NativeSettingsDiscoveryTests), nameof(ObserveWrite)));
        active = this;
        Find.TickManager.Pause();
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new NativePointerScopeBeginStep("own automatic discovery pointer", value => pointer = value);
        foreach (var step in OpenOptions()) yield return step;
        yield return Wait("automatic discovery completes without a per-page read command", () => started && (!driver.Pending || failure != null));
        yield return new CheckpointStep("every native page attempted and unfamiliar inline options captured", _ => VerifyComplete());
        yield return Shot("automatic discovery shows partial inline coverage and refused provider pages");

        int beforeConstructorRefresh = refreshes;
        yield return new AssertionStep("arm one refresh from a later native wrapper constructor", _ => constructorRefreshArmed = true);
        yield return Click("refresh discovery before the constructor invalidation probe", new(155, (float)window.Height - 35));
        yield return Wait("the reentrant replacement generation completes", () =>
            (constructorRefreshTriggered && !driver.Pending) || failure != null);
        yield return new CheckpointStep("constructor refresh cannot draw or retain the retired wrapper", _ =>
        {
            EndToEndAssert.True(constructorRefreshTriggered && !constructorRefreshArmed);
            EndToEndAssert.Equal(beforeConstructorRefresh + 2, refreshes);
            var evidence = new Dictionary<string, string>(VerifyComplete()) { ["constructorRefreshTriggered"] = "true" };
            return evidence;
        });

        yield return new AssertionStep("arrange a deterministic cancellation checkpoint", _ => pauseForCancel = true);
        yield return Click("refresh discovery through the native addon", new(155, (float)window.Height - 35));
        yield return Wait("a refreshed session has completed work and remaining pages", () => cancelReady || failure != null);
        yield return new AssertionStep("cancel has meaningful pending work", _ =>
        { RequireHealthy(); EndToEndAssert.True(cancelReady && CanCancelWithRetainedCapture); });
        yield return Click("cancel the pending discovery through its native button", new(63, (float)window.Height - 35));
        yield return Wait("cancelled discovery receives further ordinary repaints", () => cancelRepaints >= 6 || failure != null);
        yield return new CheckpointStep("cancel retains completed results and stops real renderer calls", _ =>
        {
            RequireHealthy(); AssertCancelled();
            return new Dictionary<string, string> { ["repaintsAfterCancel"] = cancelRepaints.ToString(), ["rendererEntries"] = TotalEntries.ToString() };
        });
        yield return Shot("cancelled session retains coverage while native pages remain accessible");
        int retryRefresh = refreshes + 1;
        yield return Click("retry all original pages through Refresh", new(155, (float)window.Height - 35));
        yield return Wait("refreshed automatic discovery completes", () => refreshes == retryRefresh && (!driver.Pending || failure != null));
        yield return new CheckpointStep("refresh revoked old identities and retried every native page", _ => VerifyComplete());

        yield return ClickCard(puah, "open the original Pick Up And Haul card");
        yield return Capture<Dialog_ModSettings>("capture the ordinary inline settings page", _ => { });
        yield return Wait("observe ordinary Haul on the way label", () => Label(ordinary, "Haul on the way") != null);
        yield return Shot("original inline checkbox before a deliberate native edit");
        yield return ClickLabel(ordinary, "Haul on the way", "edit the original inline checkbox");
        yield return new AssertionStep("ordinary inline edit changes only the intended value", _ => AssertPuah(!originalPuah));
        yield return Shot("original inline checkbox changed normally after discovery");
        yield return new WindowCancelActionStep("close original inline page with its deliberate edit", typeof(Dialog_ModSettings).FullName!);
        yield return new CheckpointStep("ordinary inline close persisted the changed option", _ => PersistedPuah(!originalPuah));
        yield return CaptureOptions();
        yield return ClickCard(puah, "reopen original inline settings after persistence");
        yield return Capture<Dialog_ModSettings>("capture reopened original inline settings", _ => { });
        yield return Wait("observe reopened original inline checkbox", () => Label(ordinary, "Haul on the way") != null);
        yield return new AssertionStep("reopened inline page retains the deliberate edit", _ => AssertPuah(!originalPuah));
        yield return Shot("reopened inline page retains its changed checkbox");
        yield return ClickLabel(ordinary, "Haul on the way", "restore the original inline checkbox");
        yield return new AssertionStep("inline options restored", _ => AssertPuah(originalPuah));
        yield return new WindowCancelActionStep("close original inline page normally", typeof(Dialog_ModSettings).FullName!);
        yield return new CheckpointStep("ordinary inline close persisted the restored baseline", _ => PersistedPuah(originalPuah));

        foreach (var step in OpenProvider("amber")) yield return step;
        ProviderState warm = null!; Window[] warmWindows = null!;
        yield return new AssertionStep("retain actual warm provider selection and cache", _ =>
        {
            warm = SnapshotProvider(); warmWindows = Find.WindowStack.Windows.ToArray();
            EndToEndAssert.True(warm.Selected != null && warm.Menu is Def menu && menu.defName == DefName("amber") && warm.Cache != null);
        });
        yield return Shot("real Amber provider window before automatic construction refusals");
        yield return Click("run ordinary startup discovery with the real Amber window open", new(110, (float)window.Height - 35));
        yield return Wait("unarmed startup discovery completes", () => warmRunning && (!driver.Pending || failure != null));
        yield return new CheckpointStep("ordinary startup carrier completes without disturbing Amber", _ =>
        {
            var evidence = new Dictionary<string, string>(VerifyComplete());
            EndToEndAssert.Equal(1, normalStartupPostOpens);
            warm.AssertUnchanged(this); EndToEndAssert.True(warmWindows.SequenceEqual(Find.WindowStack.Windows));
            evidence["normalStartupPostOpenCalls"] = normalStartupPostOpens.ToString();
            return evidence;
        });
        yield return Shot("same Amber page remains usable after ordinary startup discovery");
        yield return new AssertionStep("arm cancellation during the next startup constructor", _ => startupAction = StartupConstructorAction.Cancel);
        yield return Click("refresh discovery with cancellation during startup construction", new(110, (float)window.Height - 35));
        yield return Wait("cancelled startup receives further ordinary repaint pumps", () => startupCancelRepaints >= 6 || failure != null);
        yield return new CheckpointStep("startup cancellation prevents catalog publication and all page callbacks", _ =>
        {
            RequireHealthy(); AssertStartupCancelled();
            warm.AssertUnchanged(this); EndToEndAssert.True(warmWindows.SequenceEqual(Find.WindowStack.Windows));
            return new Dictionary<string, string> { ["startupCancelTriggered"] = startupCancelTriggered.ToString(),
                ["repaintsAfterCancel"] = startupCancelRepaints.ToString(), ["retiredPostOpenCalls"] = retiredStartupPostOpens.ToString() };
        });
        yield return new AssertionStep("arm startup replacement for the native retry", _ =>
        { observeStartupCancel = false; startupAction = StartupConstructorAction.Refresh; });
        yield return Click("refresh discovery while the original Amber page stays open", new(110, (float)window.Height - 35));
        yield return Wait("warm provider discovery completes automatically", () => warmRunning && (!driver.Pending || failure != null));
        yield return new CheckpointStep("hidden provider attempts preserve the actual open Amber window", _ =>
        {
            var evidence = new Dictionary<string, string>(VerifyComplete());
            EndToEndAssert.True(startupRefreshTriggered && startupAction == StartupConstructorAction.None);
            EndToEndAssert.Equal(0, retiredStartupPostOpens, "The replaced startup carrier must never enter hidden PostOpen.");
            warm.AssertUnchanged(this); EndToEndAssert.True(warmWindows.SequenceEqual(Find.WindowStack.Windows));
            evidence["warmProvider"] = "original Amber window, selection, menu and cache identity/contents unchanged";
            evidence["cacheEntryCount"] = warm.CacheEntries.Length.ToString();
            evidence["cacheScope"] = "The native fixture cache is empty; nonempty-cache restoration is not claimed.";
            evidence["startupConstructorRefresh"] = "replaced by actual Options; zero retired hidden PostOpen calls";
            return evidence;
        });
        yield return Shot("same Amber window and checkbox remain visible after provider refusal");
        foreach (var step in EditRestoreProvider("amber")) yield return step;
        foreach (var step in OpenProvider("cobalt")) yield return step;
        foreach (var step in EditRestoreProvider("cobalt")) yield return step;
        yield return CaptureOptions();
        yield return new AssertionStep("all original values and normal page access survived", _ => { AssertPuah(originalPuah); AssertProvider(false, false); });
        yield return Shot("native chooser remains usable after automatic discovery and ordinary editing");
        yield return new WindowCancelActionStep("close original Options normally", typeof(Dialog_Options).FullName!);
        yield return new NativePointerScopeEndStep("restore automatic discovery pointer", pointer);
        consoleRequested = true;
        yield return new NativeDeveloperLogActionStep("open automatic discovery native console", true);
        yield return Shot("native console after automatic discovery workflow");
        yield return new NativeDeveloperLogActionStep("close automatic discovery native console", false);
    }

    private IReadOnlyDictionary<string, string> VerifyComplete()
    {
        RequireHealthy(); EndToEndAssert.True(!driver.Pending);
        var expected = ((IEnumerable<Mod>)cachedPages.GetValue(options)!).ToArray();
        var pages = driver.Pages;
        EndToEndAssert.Equal(expected.Length, pages.Count);
        for (int index = 0; index < expected.Length; index++)
        {
            var page = pages[index];
            EndToEndAssert.True(driver.TryResolve(page.Page, out var actual) && ReferenceEquals(actual, expected[index]));
            EndToEndAssert.Equal(1, page.Callbacks, page.Page.Title ?? "untitled");
            EndToEndAssert.True(page.Status is SettingsDiscoveryStatus.Partial or SettingsDiscoveryStatus.Failed);
            if (page.Status == SettingsDiscoveryStatus.Failed) EndToEndAssert.True(!string.IsNullOrEmpty(driver.FailureReason(page.Page)));
        }
        foreach (var mod in expected.Distinct<Mod>(ReferenceEqualityComparer.Instance))
            EndToEndAssert.Equal(Count(generationStart, mod) + expected.Count(m => ReferenceEquals(m, mod)), Count(entries, mod));
        var inline = pages.Single(p => driver.TryResolve(p.Page, out var mod) && ReferenceEquals(mod, puah));
        EndToEndAssert.Equal(SettingsDiscoveryStatus.Partial, inline.Status);
        var records = inline.Captures.SelectMany(c => c.Records).ToArray();
        EndToEndAssert.True(records.Any(r => r.Kind == SettingsCaptureKind.Text && r.Text == "Haul on the way") &&
            records.Any(r => r.Kind == SettingsCaptureKind.Tooltip && r.Text.Contains("bleeding", StringComparison.OrdinalIgnoreCase)));
        foreach (string name in Names)
        {
            var page = pages.Single(p => driver.TryResolve(p.Page, out var mod) && ReferenceEquals(mod, twins[name]));
            EndToEndAssert.Equal(SettingsDiscoveryStatus.Failed, page.Status);
            EndToEndAssert.Equal("settings-read-window-construction", driver.FailureReason(page.Page));
            EndToEndAssert.Equal(0, page.Captures.Count);
        }
        EndToEndAssert.Equal(0, hiddenCloses); EndToEndAssert.Equal(0, hiddenWrites);
        AssertPuah(originalPuah); AssertProvider(false, false);
        return new Dictionary<string, string> { ["pagesAttempted"] = pages.Count.ToString(), ["actualRendererEntries"] = TotalEntries.ToString(),
            ["refreshes"] = refreshes.ToString(), ["inlineLabelAndHelp"] = "captured; original values unchanged",
            ["providerTwins"] = "both failed settings-read-window-construction; original routes retained",
            ["scope"] = "root-layout discovery only; provider option capture, tabs, search presentation and reveal remain incomplete" };
    }

    private void Refresh(bool startup)
    {
        var stale = driver.Pages.Select(p => p.Page).ToArray();
        generationStart = new(entries, ReferenceEqualityComparer.Instance);
        if (startup) driver.RefreshStartup(); else driver.Refresh(options);
        EndToEndAssert.Equal(0, driver.Pages.Count);
        foreach (var page in stale) EndToEndAssert.True(!driver.TryResolve(page, out _));
        refreshes++; cancelled = false; cancelRepaints = 0; cancelReady = false;
    }

    private void Cancel()
    {
        EndToEndAssert.True(cancelReady && CanCancelWithRetainedCapture);
        cancelledPages = driver.Pages.ToArray(); cancelledEntryCount = TotalEntries;
        driver.Cancel(); cancelled = true; pauseForCancel = false; cancelReady = false;
        AssertCancelled();
    }

    private void AssertCancelled()
    {
        EndToEndAssert.True(cancelled && !driver.Pending);
        EndToEndAssert.True(cancelledPages.Any(p => p.Status == SettingsDiscoveryStatus.Partial && p.Captures.Count > 0),
            "Cancellation must retain an actual successful partial capture, not only failed pages.");
        EndToEndAssert.Equal(cancelledEntryCount, TotalEntries);
        var current = driver.Pages;
        EndToEndAssert.Equal(cancelledPages.Length, current.Count);
        for (int index = 0; index < current.Count; index++)
        {
            EndToEndAssert.True(ReferenceEquals(current[index].Page, cancelledPages[index].Page) && driver.TryResolve(current[index].Page, out _));
            EndToEndAssert.Equal(cancelledPages[index].Callbacks, current[index].Callbacks);
            EndToEndAssert.True(current[index].Captures.SequenceEqual(cancelledPages[index].Captures));
            if (cancelledPages[index].Callbacks > 0) EndToEndAssert.Equal(cancelledPages[index].Status, current[index].Status);
        }
    }

    private void AssertStartupCancelled()
    {
        EndToEndAssert.True(startupCancelTriggered && startupAction == StartupConstructorAction.None && !driver.Pending);
        EndToEndAssert.Equal(0, driver.Pages.Count);
        EndToEndAssert.Equal(startupCancelEntries, TotalEntries);
        EndToEndAssert.Equal(0, retiredStartupPostOpens);
    }

    private void Pump()
    {
        if (Event.current?.type != EventType.Repaint || failure != null) return;
        // This pause is test scheduling only: the Cancel button releases it, then real repeated
        // ReadNext calls must themselves stop. The initial and retry scans have no pause.
        if (pauseForCancel && CanCancelWithRetainedCapture) cancelReady = true;
        if (cancelReady) return;
        var beforeWindows = Find.WindowStack.Windows.ToArray(); var beforeProvider = SnapshotProvider();
        var beforeEvent = Event.current; var beforeBackend = GuiBackend.Current;
        string amber = GetValue("amber"), cobalt = GetValue("cobalt");
        bool beforeOnTheWay = (bool)SettingField("OnTheWay").GetValue(settings)!;
        int before = TotalEntries, priorPages = driver.Pages.Count;
        driver.ReadNext(Time.frameCount);
        EndToEndAssert.True(TotalEntries - before <= 1, "One repaint invoked multiple page renderers.");
        if (priorPages == 0 && driver.Pages.Count > 0) EndToEndAssert.Equal(before, TotalEntries, "Catalog acquisition and page drawing shared an attempt.");
        int once = TotalEntries;
        driver.ReadNext(Time.frameCount);
        EndToEndAssert.Equal(once, TotalEntries, "Repeated repaint ID admitted another renderer.");
        beforeProvider.AssertUnchanged(this);
        EndToEndAssert.True(beforeWindows.SequenceEqual(Find.WindowStack.Windows) && ReferenceEquals(Current.Game, game));
        EndToEndAssert.True(ReferenceEquals(Event.current, beforeEvent) && ReferenceEquals(GuiBackend.Current, beforeBackend) && SettingsReadScope.Current == null);
        EndToEndAssert.Equal(amber, GetValue("amber")); EndToEndAssert.Equal(cobalt, GetValue("cobalt"));
        AssertPuah(beforeOnTheWay);
        if (driver.CatalogFailure != null) failure = driver.CatalogFailure;
        if (cancelled) { AssertCancelled(); cancelRepaints++; }
        if (observeStartupCancel) { AssertStartupCancelled(); startupCancelRepaints++; }
    }

    private IEnumerable<EndToEndStep> OpenOptions()
    {
        yield return new EscapeMenuActionStep("open native Escape menu", true);
        MainTabWindow_Menu menu = null!;
        yield return Capture<MainTabWindow_Menu>("capture native Escape menu", actual => menu = actual);
        yield return Click("open Options with its original native button", OptionsPoint(menu));
        yield return CaptureOptions();
        var categories = DefDatabase<OptionCategoryDef>.AllDefs.Where(d => (Prefs.DevMode || !d.isDev) && d.modContentPack.IsOfficialMod).ToList();
        int index = categories.IndexOf(OptionCategoryDefOf.Mods);
        EndToEndAssert.True(index >= 0 && ReadBool(typeof(Dialog_Options), "hasModSettings", options));
        yield return Click("select original Mod options and start automatic discovery", new(98, 18 + index * 50 + 24));
        yield return Wait("native Mod options selected", () => options.selectedCategory == OptionCategoryDefOf.Mods);
        yield return CaptureOptions();
    }

    private IEnumerable<EndToEndStep> OpenProvider(string name)
    {
        yield return CaptureOptions();
        yield return ClickCard(twins[name], "open original " + name + " provider card");
        yield return Wait("original provider selects " + name, () => Find.WindowStack.Windows.Any(w => w.GetType().FullName == ProviderWindowName) &&
            (activeMenu.GetValue(null) as Def)?.defName == DefName(name));
        yield return new NativeWindowCaptureStep("capture original provider window", captured =>
        {
            Retain(Find.WindowStack.Windows.Single(w => w.GetType().FullName == ProviderWindowName), captured);
            EndToEndAssert.True(!Find.WindowStack.IsOpen<Dialog_ModSettings>());
        });
        yield return Wait("observe original " + name + " checkbox", () => Label(provider, "Catalog " + name + " option") != null);
    }

    private IEnumerable<EndToEndStep> EditRestoreProvider(string name)
    {
        yield return Shot("original " + name + " checkbox before editing after discovery");
        yield return ClickLabel(provider, "Catalog " + name + " option", "edit original " + name + " checkbox");
        yield return new AssertionStep("only original " + name + " option changed", _ => AssertProvider(name == "amber", name == "cobalt"));
        yield return Shot("original " + name + " checkbox responds to normal native input");
        yield return new WindowCancelActionStep("close original provider and persist edit", ProviderWindowName);
        yield return new CheckpointStep("ordinary provider close persisted the edit", _ => PersistedProvider());
        foreach (var step in OpenProvider(name)) yield return step;
        yield return new AssertionStep("provider reopen retains deliberate edit", _ => EndToEndAssert.Equal(true, bool.Parse(GetValue(name))));
        yield return Shot("reopened original " + name + " provider retains its edited checkbox");
        yield return ClickLabel(provider, "Catalog " + name + " option", "restore original " + name + " checkbox");
        yield return new AssertionStep("both provider values restored", _ => AssertProvider(false, false));
        yield return new WindowCancelActionStep("close restored original provider page", ProviderWindowName);
        yield return new CheckpointStep("ordinary close persisted restored provider defaults", _ => PersistedProvider());
    }

    private NativeWindowCaptureStep CaptureOptions() => Capture<Dialog_Options>("capture original Options", actual => options = actual);
    private NativeWindowCaptureStep Capture<T>(string name, Action<T> receive) where T : Window => new(name, captured =>
    { var actual = Find.WindowStack.Windows.OfType<T>().Single(); Retain(actual, captured); receive(actual); });
    private void Retain(Window actual, IReadOnlyList<EndToEndNativeWindow> captured)
    { if (!owned.Contains(actual)) owned.Add(actual); window = captured.Single(w => w.NativeId == actual.ID && w.RuntimeType == actual.GetType().FullName); }
    private NativeWindowClickActionStep Click(string name, Vector2 point) => new(name, window.Handle,
        (window.X + point.x) * window.UiScale, (window.Y + point.y) * window.UiScale, null, pointer);
    private NativeWindowClickActionStep ClickObserved(string name, SettingsCaptureRect bounds)
    {
        EndToEndAssert.True(bounds.X >= window.X + 18 && bounds.Y >= window.Y + 18 &&
            bounds.X + bounds.Width <= window.X + window.Width - 18 && bounds.Y + bounds.Height <= window.Y + window.Height - 18);
        return new(name, window.Handle, (bounds.X + bounds.Width / 2) * window.UiScale, (bounds.Y + bounds.Height / 2) * window.UiScale, null, pointer);
    }
    private NativeWindowClickActionStep ClickCard(Mod mod, string action)
    {
        EndToEndAssert.Equal(OptionCategoryDefOf.Mods, options.selectedCategory);
        EndToEndAssert.Equal(string.Empty, Field(typeof(Dialog_Options), "modFilter").GetValue(options) as string,
            "This workflow locates the complete unfiltered native chooser.");
        var originals = ((IEnumerable<Mod>)cachedPages.GetValue(options)!).ToArray();
        var labels = chooser.LastCapture!.Records.Where(r => r.Kind == SettingsCaptureKind.Text).ToArray();
        EndToEndAssert.Equal(6, originals.Length, "The exact active-mod group exposes six original native cards.");
        EndToEndAssert.Equal(originals.Length, labels.Length);
        var cacheField = Field(typeof(Dialog_Options), "modOptionTruncationCache");
        EndToEndAssert.True(!cacheField.IsStatic && cacheField.FieldType == typeof(Dictionary<string, string>));
        var cache = cacheField.GetValue(options) as Dictionary<string, string>
            ?? throw new InvalidOperationException("The native chooser truncation cache is unavailable.");
        // Native truncation is cached by title, independent of later width changes. Read the exact
        // existing values used by this Options instance; never populate or recompute its cache.
        for (int index = 0; index < originals.Length; index++)
        {
            EndToEndAssert.True(cache.TryGetValue(originals[index].SettingsCategory(), out var rendered),
                "Every original card title must already have a native cached label.");
            EndToEndAssert.Equal(rendered, labels[index].Text,
                "Observed native card text must match its exact source occurrence.");
        }
        int ordinal = Array.FindIndex(originals, m => ReferenceEquals(m, mod)); EndToEndAssert.True(ordinal >= 0);
        return ClickObserved(action, labels[ordinal].Bounds);
    }
    private NativeWindowClickActionStep ClickLabel(SettingsPageRecorder recorder, string label, string action) => ClickObserved(action, Label(recorder, label)!.Bounds);
    private static SettingsCaptureRecord? Label(SettingsPageRecorder recorder, string text) => recorder.LastCapture?.Records.SingleOrDefault(r => r.Kind == SettingsCaptureKind.Text && r.Text == text);
    private static SettingsPageRecorder Recorder(string role) => new(new("discovery-fixture-" + role, "discovery-fixture-" + role, null, null, null), "ordinary");
    private FieldInfo SettingField(string name) => Field(settings.GetType(), name);
    private void AssertPuah(bool onTheWay)
    {
        foreach (var pair in scalarValues) EndToEndAssert.Equal(pair.Key.Name == "OnTheWay" ? onTheWay : pair.Value, pair.Key.GetValue(settings), pair.Key.Name);
        EndToEndAssert.True(ReferenceEquals(denied, SettingField("DeniedStorage").GetValue(settings)) && deniedValues.SequenceEqual(denied));
    }
    private string GetValue(string name) => (string)getSetting.Invoke(null, new object[] { ModId(name), "enabled" })!;
    private void AssertProvider(bool amber, bool cobalt)
    { EndToEndAssert.Equal(amber, bool.Parse(GetValue("amber"))); EndToEndAssert.Equal(cobalt, bool.Parse(GetValue("cobalt"))); }
    private IReadOnlyDictionary<string, string> PersistedPuah(bool expected)
    {
        var files = Directory.GetFiles(GenFilePaths.ConfigFolderPath, "Mod_*.xml")
            .Where(p => Path.GetFileName(p).Contains("PickUpAndHaul", StringComparison.OrdinalIgnoreCase)).ToArray();
        EndToEndAssert.Equal(1, files.Length, "Native close must write the original PUAH settings file.");
        var value = XDocument.Load(files[0]).Descendants("onTheWay").SingleOrDefault()?.Value;
        // The original Scribe default is true and may be omitted from its native file.
        EndToEndAssert.Equal(expected, value == null || bool.Parse(value));
        return new Dictionary<string, string> { ["settingsFile"] = Path.GetFileName(files[0]), ["onTheWay"] = expected.ToString() };
    }
    private IReadOnlyDictionary<string, string> PersistedProvider()
    {
        var files = Directory.GetFiles(GenFilePaths.ConfigFolderPath, "Mod_*.xml").Where(p => Path.GetFileName(p).EndsWith("_XmlMod.xml", StringComparison.OrdinalIgnoreCase)).ToArray();
        EndToEndAssert.Equal(1, files.Length);
        var data = XDocument.Load(files[0]).Descendants("dataDict").Single();
        var keys = data.Element("keys")!.Elements("li").Select(e => e.Value).ToArray();
        var values = data.Element("values")!.Elements("li").Select(e => e.Value).ToArray();
        EndToEndAssert.Equal(keys.Length, values.Length);
        var saved = keys.Zip(values, (key, value) => (key, value)).ToDictionary(p => p.key, p => p.value);
        foreach (string name in Names) EndToEndAssert.Equal(bool.Parse(GetValue(name)), bool.Parse(saved[ModId(name) + ";enabled"]));
        return new Dictionary<string, string> { ["settingsFile"] = Path.GetFileName(files[0]), ["amber"] = GetValue("amber"), ["cobalt"] = GetValue("cobalt") };
    }
    private ProviderState SnapshotProvider() => new(selectedMod.GetValue(null), activeMenu.GetValue(null), oldValuesCache.GetValue(null));
    private sealed class ProviderState
    {
        internal readonly object? Selected, Menu, Cache;
        internal readonly KeyValuePair<string, string>[] CacheEntries;
        internal ProviderState(object? selected, object? menu, object? cache)
        { Selected = selected; Menu = menu; Cache = cache; CacheEntries = cache is Dictionary<string, string> values ? values.ToArray() : Array.Empty<KeyValuePair<string, string>>(); }
        internal void AssertUnchanged(NativeSettingsDiscoveryTests owner)
        {
            EndToEndAssert.True(ReferenceEquals(Selected, owner.selectedMod.GetValue(null)) && ReferenceEquals(Menu, owner.activeMenu.GetValue(null)) && ReferenceEquals(Cache, owner.oldValuesCache.GetValue(null)));
            if (Cache is Dictionary<string, string> values) EndToEndAssert.True(CacheEntries.OrderBy(p => p.Key).SequenceEqual(values.OrderBy(p => p.Key)));
        }
        internal void Restore(NativeSettingsDiscoveryTests owner)
        {
            if (Cache is Dictionary<string, string> values) { values.Clear(); foreach (var pair in CacheEntries) values.Add(pair.Key, pair.Value); }
            owner.selectedMod.SetValue(null, Selected); owner.activeMenu.SetValue(null, Menu); owner.oldValuesCache.SetValue(null, Cache);
        }
    }
    private Vector2 OptionsPoint(MainTabWindow_Menu menu)
    {
        var labels = new List<string>();
        if (!GameDataSaveLoader.SavingIsTemporarilyDisabled && !game.Info.permadeathMode) labels.Add("Save");
        if (ReadBool(typeof(MainTabWindow_Menu), "anyGameFiles", menu) && !game.Info.permadeathMode) labels.Add("LoadGame");
        labels.Add("ReviewScenario"); var font = Text.Font;
        try { Text.Font = GameFont.Small; return new(103, 18 + labels.Sum(key => Math.Max(45, Text.CalcHeight(key.Translate(), 170)) + 7) + Math.Max(45, Text.CalcHeight("Options".Translate(), 170)) / 2); }
        finally { Text.Font = font; }
    }
    private void RequireHealthy()
    { EndToEndAssert.True(failure == null, failure ?? ""); EndToEndAssert.True(driver.CatalogFailure == null, driver.CatalogFailure ?? ""); }
    private int TotalEntries => entries.Values.Sum();
    private bool CanCancelWithRetainedCapture
    {
        get
        {
            var pages = driver.Pages;
            return driver.Pending && pages.Any(p => p.Status == SettingsDiscoveryStatus.Partial && p.Captures.Count > 0) &&
                pages.Any(p => p.Status == SettingsDiscoveryStatus.Pending);
        }
    }
    private static int Count(Dictionary<Mod, int> counts, Mod mod) => counts.TryGetValue(mod, out int value) ? value : 0;
    private static bool IsScenarioWindow(Window w) => w is Dialog_Options or Dialog_ModSettings or MainTabWindow_Menu or EditWindow_Log || w.GetType().FullName == ProviderWindowName;
    private static string ModId(string name) => "megamod.catalog.fixture." + name;
    private static string DefName(string name) => "MegamodCatalog" + char.ToUpperInvariant(name[0]) + name[1..];
    private static Type RequiredType(string name) => AccessTools.TypeByName(name) ?? throw new TypeLoadException(name);
    private static FieldInfo Field(Type type, string name) => AccessTools.Field(type, name) ?? throw new MissingFieldException(type.FullName, name);
    private static MethodInfo Method(Type type, string name, params Type[] arguments) => type.GetMethod(name,
        BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static, null, arguments, null) ?? throw new MissingMethodException(type.FullName, name);
    private static bool ReadBool(Type type, string name, object instance) => Field(type, name).GetValue(instance) is bool value ? value : throw new InvalidOperationException("Expected native Boolean field.");
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private static WaitUntilStep Wait(string name, Func<bool> condition) => new(name, _ => condition(), new EndToEndDeadline(int.MaxValue, 180, TimeSpan.FromSeconds(30)));

    private static IEnumerable<CodeInstruction> SurroundChooser(IEnumerable<CodeInstruction> instructions) => ReplaceCall(instructions, Method(typeof(Dialog_Options), "DoModOptions", typeof(Listing_Standard)), nameof(DrawChooser));
    private static IEnumerable<CodeInstruction> SurroundNativePage(IEnumerable<CodeInstruction> instructions) => ReplaceCall(instructions, Method(typeof(Mod), nameof(Mod.DoSettingsWindowContents), typeof(Rect)), nameof(DrawNativePage));
    private static IEnumerable<CodeInstruction> SurroundProvider(IEnumerable<CodeInstruction> instructions) => ReplaceCall(instructions, Method(RequiredType("XmlExtensions.SettingsMenuDef"), "DrawSettingsMenu", typeof(Rect)), nameof(DrawProvider));
    private static IEnumerable<CodeInstruction> ReplaceCall(IEnumerable<CodeInstruction> instructions, MethodInfo method, string replacement)
    {
        int found = 0;
        foreach (var instruction in instructions)
            if (instruction.Calls(method))
            {
                found++;
                yield return new CodeInstruction(instruction) { opcode = OpCodes.Call,
                    operand = AccessTools.Method(typeof(NativeSettingsDiscoveryTests), replacement) };
            }
            else yield return instruction;
        if (found != 1) throw new InvalidOperationException("Expected one ordinary native draw edge: " + method);
    }
    private static void DrawChooser(Dialog_Options instance, Listing_Standard listing)
    { var owner = active!; owner.chooser.Observe(() => owner.drawChooser.Invoke(instance, new object[] { listing })); }
    private static void DrawProvider(object menu, Rect rect)
    { var owner = active!; owner.provider.Observe(() => owner.drawMenu.Invoke(menu, new object[] { rect })); }
    private static void DrawNativePage(Mod mod, Rect rect)
    {
        var owner = active!;
        if (SettingsReadScope.Current != null)
        {
            EndToEndAssert.True(owner.lastEntryFrame != Time.frameCount, "Multiple hidden renderers ran in one native frame.");
            owner.lastEntryFrame = Time.frameCount; owner.entries[mod] = Count(owner.entries, mod) + 1;
            mod.DoSettingsWindowContents(rect);
        }
        else if (ReferenceEquals(owner.puah, mod)) owner.ordinary.Observe(() => mod.DoSettingsWindowContents(rect));
        else mod.DoSettingsWindowContents(rect);
    }
    private static void ObserveClose() { if (active is { } owner && SettingsReadScope.Current != null) owner.hiddenCloses++; }
    private static void ObserveWrite() { if (active is { } owner && SettingsReadScope.Current != null) owner.hiddenWrites++; }
    private static void RefreshAfterWrapperConstruction(Mod mod)
    {
        if (active is not { constructorRefreshArmed: true } owner || SettingsReadScope.Current != null || !ReferenceEquals(mod, owner.puah)) return;
        owner.constructorRefreshArmed = false;
        owner.constructorRefreshTriggered = true;
        owner.Refresh(false);
    }
    private static void ReplaceStartupConstruction()
    {
        if (active is not { } owner || owner.startupAction == StartupConstructorAction.None || SettingsReadScope.Current != null) return;
        var action = owner.startupAction;
        owner.startupAction = StartupConstructorAction.None;
        if (action == StartupConstructorAction.Cancel)
        {
            owner.startupCancelTriggered = true;
            owner.startupCancelEntries = owner.TotalEntries;
            owner.driver.Cancel();
            owner.observeStartupCancel = true;
        }
        else
        {
            owner.startupRefreshTriggered = true;
            owner.Refresh(false);
        }
    }
    private static void ObserveStartupPostOpen()
    {
        if (active is not { } owner || SettingsReadScope.Current == null) return;
        if (owner.startupRefreshTriggered || owner.startupCancelTriggered) owner.retiredStartupPostOpens++;
        else owner.normalStartupPostOpens++;
    }
    private static void OptionsAddon(Dialog_Options __instance, Rect inRect)
    {
        var owner = active;
        if (owner == null || __instance.selectedCategory != OptionCategoryDefOf.Mods) return;
        try
        {
            owner.options = __instance;
            if (!owner.started) { owner.started = true; owner.Refresh(false); }
            if (Widgets.ButtonText(new Rect(0, inRect.yMax - 34, 90, 32), "Cancel")) owner.Cancel();
            if (Widgets.ButtonText(new Rect(100, inRect.yMax - 34, 90, 32), "Refresh")) owner.Refresh(false);
            owner.DrawStatus(new Rect(0, inRect.yMax - 61, inRect.width, 24));
            if (!Find.WindowStack.Windows.Any(w => w.GetType().FullName == ProviderWindowName || w is Dialog_ModSettings)) owner.Pump();
        }
        catch (Exception error) { owner.failure ??= error.ToString(); }
    }
    private static void ProviderAddon(Rect inRect)
    {
        var owner = active;
        if (owner == null) return;
        try
        {
            if (Widgets.ButtonText(new Rect(0, inRect.yMax - 34, 190, 32), "Refresh discovery"))
            { owner.warmRunning = true; owner.Refresh(true); }
            owner.DrawStatus(new Rect(0, inRect.yMax - 61, inRect.width, 24));
            if (owner.warmRunning) owner.Pump();
        }
        catch (Exception error) { owner.failure ??= error.ToString(); }
    }
    private void DrawStatus(Rect rect)
    {
        var pages = driver.Pages;
        Widgets.Label(rect, failure == null
            ? $"Pages {pages.Count}; partial {pages.Count(p => p.Status == SettingsDiscoveryStatus.Partial)}; failed {pages.Count(p => p.Status == SettingsDiscoveryStatus.Failed)}; " +
                (cancelReady ? "Ready to cancel" : cancelled ? "Cancelled" : driver.Pending ? "Reading" : "Finished")
            : "Discovery failed: " + failure[..Math.Min(failure.Length, 130)]);
    }
}
