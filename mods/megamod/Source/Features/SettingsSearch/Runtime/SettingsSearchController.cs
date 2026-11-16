using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.SettingsSearch.Core;
using Megamod.Features.SettingsSearch.Harmony;
using Megamod.Settings;
using RimWorld;
using UnityEngine;
using Verse;

namespace Megamod.Features.SettingsSearch.Runtime;

public sealed record SettingsSearchDrawnRow(SettingsPage Page, SettingsSearchEntry? Entry, Rect ScreenRect);

// Owns the complete native chooser lifetime. Construction/patch acquisition are loader-thread
// safe; reader/backend objects are created only after a successful native GUI-root completion.
public sealed partial class SettingsSearchController : IDisposable
{
    private readonly Mod owner;
    private readonly SettingsRuntime settings;
    private readonly SettingsRenderer renderer;
    private readonly Action<string> warning;
    private readonly List<IDisposable> hooks = new();
    private readonly Dictionary<Dialog_Options, DialogState> dialogs = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<Window, SettingsPage> opened = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<SettingsPage, IReadOnlyList<SettingsForeignOption>> observed = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<SettingsPage, string> observedCoverage = new(ReferenceEqualityComparer.Instance);
    private readonly List<SettingsSearchDrawnRow> drawnRows = new();
    private readonly Dictionary<string, Rect> controlRects = new(StringComparer.Ordinal);
    private readonly HashSet<string> warnings = new(StringComparer.Ordinal);
    private SettingsChooserSplice? chooser;
    private SettingsDiscoveryDriver? discovery;
    private SettingsForeignRecorder? recorder;
    private SettingsForeignReveal? foreignReveal;
    private Dialog_Options? completedOptions;
    private Window? activePageWindow;
    private Opening? pendingOpening;
    private Action? visibleDraw;
    private SettingsPage? visiblePage;
    private string? manualRevealFailure;
    private bool revealExplanationShown;
    private FieldInfo? widgetField, scrollField;
    private bool installed, disposed, pumping, initialized, searchOn, navigationOn, priorSearch;
    private volatile bool startupRequested;
    private object? game, world, language;
    private string? languageFolder;
    private long repaint, observedRevision;
    private static bool byAuthor = true, groupSingles = true;
    private sealed record OptionRoute(SettingsSearchEntry Entry, string? OwnedId = null, SettingsForeignTarget? Foreign = null);
    private sealed record PageRow(SettingsNativePage Native, SettingsChooserRow Row, string? Heading,
        bool StartGroup, IReadOnlyList<OptionRoute> Options);
    private sealed record Opening(SettingsNativePage Page, OptionRoute? Option, long Generation, long Activation, Window? NativeWindow = null);
    private sealed class DialogState
    {
        internal required Dialog_Options Dialog;
        internal required QuickSearchWidget Widget;
        internal int OriginalLimit;
        internal bool OwnsLimit;
        internal readonly SettingsChooserSourceRefresh Refresh = new();
        internal string Query = "";
        internal object? DiscoverySnapshot;
        internal SettingsRevision? Revision;
        internal long ObservedRevision = -1, Generation = -1;
        internal bool Search, Navigation, ByAuthor, GroupSingles;
        internal PageRow[] Rows = Array.Empty<PageRow>();
        internal readonly Dictionary<SettingsPage, Texture2D?> Icons = new(ReferenceEqualityComparer.Instance);
    }
    private sealed class Frame : IEnumerable<Mod>, IEnumerator<Mod>
    {
        internal Frame(DialogState state, Listing_Standard listing, PageRow[] rows, long generation, bool fullWidth)
        { State = state; Listing = listing; Rows = rows; Generation = generation; FullWidth = fullWidth; }
        internal readonly DialogState State;
        internal readonly Listing_Standard Listing;
        internal readonly PageRow[] Rows;
        internal readonly long Generation;
        internal readonly bool FullWidth;
        internal int Index = -1, Column;
        internal Rect Pair;
        internal Opening? Opening;
        public Mod Current => Rows[Index].Native.Native;
        object IEnumerator.Current => Current;
        public IEnumerator<Mod> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
        public bool MoveNext() => ++Index < Rows.Length;
        public void Reset() => throw new NotSupportedException();
        public void Dispose() { }
    }

    public SettingsSearchController(Mod owner, SettingsRuntime settings, SettingsRenderer renderer, Action<string> warning)
    { this.owner = owner; this.settings = settings; this.renderer = renderer; this.warning = warning; }
    public static SettingsSearchController? Instance { get; private set; }
    public string Query { get; private set; } = "";
    public bool Discovering => searchOn && discovery?.Pending == true;
    public bool Cancelled => discovery?.Cancelled == true;
    public string? Failure { get; private set; }
    public IReadOnlyList<SettingsDiscoveryPage> Pages => discovery?.Pages ?? Array.Empty<SettingsDiscoveryPage>();
    public IReadOnlyList<SettingsSearchDrawnRow> DrawnRows => drawnRows.AsReadOnly();
    public IReadOnlyDictionary<string, Rect> ControlRects => controlRects;
    public SettingsForeignFrame? CurrentForeignFrame { get; private set; }
    public string RevealStatus => manualRevealFailure != null ? "Unavailable" : activePageWindow != null && opened.TryGetValue(activePageWindow, out var page) &&
        discovery?.TryResolve(page, out var mod) == true && ReferenceEquals(mod, owner)
        ? renderer.RevealStatus.ToString() : foreignReveal?.Status.ToString() ?? "Cancelled";
    public string? RevealFailure => manualRevealFailure ?? foreignReveal?.FailureReason;

    public bool Install()
    {
        if (installed) return true;
        if (disposed) return false;
        try
        {
            const BindingFlags declared = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly;
            var type = typeof(Dialog_Options);
            widgetField = type.GetField("quickSearchWidget", declared);
            scrollField = type.GetField("optionsScrollPosition", declared);
            var source = type.GetField("cachedModsWithSettings", declared);
            if (widgetField?.FieldType != typeof(QuickSearchWidget) || scrollField?.FieldType != typeof(Vector2) || source?.FieldType != typeof(IEnumerable<Mod>))
                throw new InvalidOperationException("Native Options field shapes changed.");
            MethodInfo Target(Type target, string name, params Type[] arguments) =>
                target.GetMethod(name, declared, null, arguments, null) ?? throw new MissingMethodException(target.FullName, name);
            chooser = new(Target(type, "DoModOptions", typeof(Listing_Standard)), source,
                typeof(Widgets).GetMethod(nameof(Widgets.ButtonInvisible), new[] { typeof(Rect), typeof(bool) })!,
                typeof(WindowStack).GetMethod(nameof(WindowStack.Add), new[] { typeof(Window) })!,
                SelectSource, DrawRow, Warn, window => completedOptions = (Dialog_Options)window,
                (context, added) =>
                {
                    var frame = (Frame)context;
                    if (frame.Opening != null && discovery?.Generation == frame.Generation)
                        pendingOpening = frame.Opening with { NativeWindow = (Window)added };
                });
            hooks.Add(chooser);
            if (!chooser.Install()) throw new InvalidOperationException(chooser.Failure ?? "Native chooser could not be admitted.");
            foreach (var root in new[] { typeof(UIRoot_Entry), typeof(UIRoot_Play) })
            {
                var callback = new SettingsChooserCompletion(Target(root, "UIRootOnGUI"), _ => RootCompleted(), Warn);
                hooks.Add(callback);
                if (!callback.Install()) throw new InvalidOperationException("Native GUI root completion could not be admitted.");
            }
            var draw = typeof(Mod).GetMethod(nameof(Mod.DoSettingsWindowContents), new[] { typeof(Rect) })!;
            foreach (var target in new[] { Target(typeof(Dialog_ModSettings), nameof(Dialog_ModSettings.DoWindowContents), typeof(Rect)),
                Target(type, "DoOptions", typeof(OptionCategoryDef), typeof(Rect)) })
            {
                var callback = new SettingsChooserPageDraw(target, draw,
                    (mod, rect) => ((Mod)mod).DoSettingsWindowContents((Rect)rect), ObservePage, Warn);
                hooks.Add(callback);
                if (!callback.Install()) throw new InvalidOperationException("Native page observation could not be admitted.");
            }
            installed = true; Instance = this; return true;
        }
        catch (Exception error)
        {
            Warn(error.GetBaseException().Message);
            for (int i = hooks.Count - 1; i >= 0; i--) try { hooks[i].Dispose(); } catch (Exception cleanup) { Warn(cleanup.Message); }
            hooks.Clear(); return false;
        }
    }

    public void RequestStartupDiscovery() => startupRequested = true;

    private void RootCompleted()
    {
        if (disposed || !installed || pumping || SettingsReadScope.Current != null || Event.current?.type != EventType.Repaint ||
            Event.current.rawType != EventType.Repaint || !UnityData.IsInMainThread) return;
        pumping = true;
        try
        {
            settings.ProcessPendingLifecycle();
            searchOn = settings.Store.GetEffective(SettingsSearchSettings.Enabled);
            navigationOn = settings.Store.GetEffective(ModSettingsNavigationSettings.Enabled);
            if (!searchOn && !navigationOn)
            {
                if (initialized) { discovery!.Clear(); CancelReveal(); RestoreWidgets(); initialized = false; }
                priorSearch = false; return;
            }
            if (discovery == null)
            {
                recorder = new SettingsForeignRecorder(Warn);
                recorder.Initialize();
                foreignReveal = new SettingsForeignReveal(recorder);
                discovery = new SettingsDiscoveryDriver(Warn, recorder);
            }
            bool contextChanged = !initialized || !ReferenceEquals(game, Current.Game) || !ReferenceEquals(world, Current.Game?.World) ||
                !ReferenceEquals(language, LanguageDatabase.activeLanguage) || languageFolder != LanguageDatabase.activeLanguage?.folderName;
            if (contextChanged)
            {
                discovery.Clear(); CancelReveal(); observed.Clear(); observedCoverage.Clear(); opened.Clear(); observedRevision++;
                foreach (var state in dialogs.Values) { state.Refresh.RequestRefresh(); state.Generation = -1; state.Icons.Clear(); }
                game = Current.Game; world = Current.Game?.World; language = LanguageDatabase.activeLanguage;
                languageFolder = LanguageDatabase.activeLanguage?.folderName; initialized = true; startupRequested = true;
            }
            if (priorSearch && !searchOn) { discovery.Cancel(); CancelReveal(); }
            if (!priorSearch && searchOn)
            {
                startupRequested = true;
                foreach (var state in dialogs.Values) state.Refresh.RequestRefresh();
            }
            priorSearch = searchOn;
            PruneClosedViews();
            if (completedOptions != null && dialogs.TryGetValue(completedOptions, out var shown) && shown.Refresh.NeedsRequest &&
                Find.WindowStack.IsOpen(completedOptions))
            { discovery.Refresh(completedOptions); shown.Refresh.MarkRequested(); startupRequested = false; observed.Clear(); observedCoverage.Clear(); observedRevision++; }
            else if (startupRequested && !LongEventHandler.AnyEventNowOrWaiting)
            { startupRequested = false; discovery.RefreshStartup(); }
            ++repaint;
            if (foreignReveal!.NeedsLayout && visiblePage != null && visibleDraw != null && activePageWindow != null && Find.WindowStack.IsOpen(activePageWindow))
                foreignReveal.ReadLayout(discovery.Reader, visiblePage, visibleDraw);
            else if (discovery.Pending) discovery.ReadNext(repaint, searchOn);
            Failure = discovery.CatalogFailure;
        }
        catch (Exception error) { Warn(error.GetBaseException().Message); }
        finally { pumping = false; }
    }

    private void PruneClosedViews()
    {
        if (pendingOpening?.NativeWindow is { } pendingWindow && !Find.WindowStack.IsOpen(pendingWindow)) CancelReveal();
        foreach (var pair in dialogs.ToArray())
        {
            if (Find.WindowStack.IsOpen(pair.Key)) continue;
            RestoreWidget(pair.Value); dialogs.Remove(pair.Key);
            if (ReferenceEquals(completedOptions, pair.Key)) completedOptions = null;
            Query = ""; drawnRows.Clear(); controlRects.Clear();
        }
        foreach (var window in opened.Keys.ToArray())
            if (!Find.WindowStack.IsOpen(window))
            {
                opened.Remove(window);
                if (ReferenceEquals(activePageWindow, window)) { CancelReveal(); activePageWindow = null; CurrentForeignFrame = null; visibleDraw = null; visiblePage = null; }
            }
        if (activePageWindow is Dialog_Options options && options.selectedMod == null)
        { CancelReveal(); opened.Remove(options); activePageWindow = null; CurrentForeignFrame = null; visibleDraw = null; visiblePage = null; }
    }

    private object SelectSource(object source, object window, object suppliedListing, out object? context)
    {
        context = null;
        if (disposed || SettingsReadScope.Current != null || !UnityData.IsInMainThread || widgetField == null) return source;
        if (hooks.OfType<SettingsChooserCompletion>().Any(hook => !hook.Available) || hooks.OfType<SettingsChooserPageDraw>().Any(hook => !hook.Available)) return source;
        settings.ProcessPendingLifecycle();
        bool navigation = settings.Store.GetEffective(ModSettingsNavigationSettings.Enabled), search = settings.Store.GetEffective(SettingsSearchSettings.Enabled);
        var dialog = (Dialog_Options)window;
        if (!navigation && !search)
        { if (dialogs.TryGetValue(dialog, out var old)) RestoreWidget(old); return source; }
        if (widgetField.GetValue(dialog) is not QuickSearchWidget widget) return source;
        if (!dialogs.TryGetValue(dialog, out var state))
        {
            state = new DialogState { Dialog = dialog, Widget = widget, OriginalLimit = widget.maxSearchTextLength };
            dialogs.Add(dialog, state);
        }
        if (!state.OwnsLimit) { state.OriginalLimit = widget.maxSearchTextLength; widget.maxSearchTextLength = 512; state.OwnsLimit = true; }
        if (state.Refresh.ObserveSource(source))
        {
            // A provider replaced its collection. Retire old result/reveal capabilities now;
            // materialization still waits for the completed native GUI-root boundary.
            CancelReveal(); discovery?.Clear(); observed.Clear(); observedCoverage.Clear(); opened.Clear(); observedRevision++;
            state.Generation = -1; state.Rows = Array.Empty<PageRow>(); state.Icons.Clear();
            activePageWindow = null; visibleDraw = null; visiblePage = null; CurrentForeignFrame = null;
        }
        Query = widget.filter.Text ?? "";
        var listing = (Listing_Standard)suppliedListing;
        if (Event.current?.type == EventType.Repaint) { drawnRows.Clear(); controlRects.Clear(); }
        RememberControl("Query", new Rect(0, listing.CurHeight - 42f, listing.ColumnWidth, 30f));
        if (!initialized || discovery == null) return source;
        DrawControls(state, listing, navigation, search);
        if (!discovery.MatchesSource(dialog, (IEnumerable<Mod>)source))
        { widget.noResultsMatched = false; return source; }
        try { EnsureProjection(state, navigation, search); }
        catch (Exception error) { Warn("Settings query projection unavailable: " + error.GetType().Name); return source; }
        widget.noResultsMatched = state.Rows.Length == 0 && !Discovering && Pages.All(page => page.Status != SettingsDiscoveryStatus.Failed);
        if (state.Rows.Length == 0) listing.Label(T("NoResults"));
        var frame = new Frame(state, listing, state.Rows, discovery.Generation,
            search && !string.IsNullOrWhiteSpace(Query) || listing.ColumnWidth < 300f);
        context = frame;
        return frame;
    }

    private void EnsureProjection(DialogState state, bool navigation, bool search)
    {
        var pageSnapshot = discovery!.Pages;
        var revision = settings.Store.Revision;
        if (state.Query == Query && ReferenceEquals(state.DiscoverySnapshot, pageSnapshot) && state.Revision == revision &&
            state.Generation == discovery.Generation && state.ObservedRevision == observedRevision && state.Search == search &&
            state.Navigation == navigation && state.ByAuthor == byAuthor && state.GroupSingles == groupSingles) return;
        var catalog = discovery.CatalogPages;
        var sourceMap = catalog.ToDictionary<SettingsNativePage, SettingsPage>(page => page.Page, ReferenceEqualityComparer.Instance);
        var labels = new SettingsChooserLabels(T("VariousAuthors"), T("UnknownAuthor"), T("HugsLibGenerated"),
            T("VanillaExpanded"), T("SargBjornson"), T("Helixien"), T("UntitledPage"));
        IReadOnlyList<SettingsChooserGroup> groups = new SettingsChooser(catalog.Select(page => page.Page), labels, CultureInfo.CurrentCulture)
            .Project(search ? null : Query, navigation && byAuthor, groupSingles);
        if (!navigation)
        {
            var rows = groups.SelectMany(group => group.Rows).OrderBy(row => row.InputOrdinal)
                .Select(row => row with { DisplayGroup = null, ShowAuthor = true }).ToArray();
            groups = new[] { new SettingsChooserGroup(null, rows) };
        }
        var routes = new List<OptionRoute>();
        if (search)
        {
            foreach (var native in catalog)
            {
                if (ReferenceEquals(native.Native, owner))
                {
                    int ordinal = 0;
                    foreach (var row in renderer.CaptureRows())
                    {
                        string values = T("GlobalValue", row.GlobalRequested.Text ?? T("UnknownValue")) +
                            (row.CurrentSaveRequested == null ? "" : "; " + T("SaveValue", row.CurrentSaveRequested.Text ?? T("UnknownValue"))) +
                            "; " + T("EffectiveValue", row.Effective.Text ?? T("UnknownValue"));
                        string help = string.Join("\n", new[] { row.Help, row.Status, row.UnavailableReason,
                            row.EffectiveOffReason == null ? null : row.EffectiveOffReason.Translate().ToString(),
                            T("Timing_" + row.ApplicationTiming) }.Where(text => !string.IsNullOrEmpty(text)));
                        routes.Add(new(new(native.Page, "owned:" + row.Id, ordinal++, row.Label, help, values,
                            Feature: row.Id, Category: row.Category), row.Id));
                    }
                }
                else
                {
                    var options = observed.TryGetValue(native.Page, out var live) ? live :
                        discovery.ForeignOptions.TryGetValue(native.Page, out var read) ? read : Array.Empty<SettingsForeignOption>();
                    var retention = new SettingsForeignRetention();
                    bool truncated = false;
                    foreach (var option in options)
                    {
                        if (!retention.TryRetain(option, out var retained)) { truncated = true; break; }
                        routes.Add(new(retained.Entry, Foreign: retained.Target));
                    }
                    // Readable custom/unpaired text remains eligible. It carries no invented
                    // control identity; selecting it still opens the correct original page.
                    // Typed controls and raw observations share the same per-page budget.
                    var captures = pageSnapshot.FirstOrDefault(page => ReferenceEquals(page.Page, native.Page))?.Captures ?? Array.Empty<SettingsCaptureSnapshot>();
                    var pairedText = new HashSet<string>(options.SelectMany(option => new[] { option.Entry.Label, option.Entry.Help,
                        option.Entry.DisplayedValue, option.Entry.Tab }).Where(text => !string.IsNullOrEmpty(text)).Select(text => text!), StringComparer.Ordinal);
                    foreach (var capture in captures.GroupBy(capture => capture.PathIdentity, StringComparer.Ordinal).Select(group => group.Last()))
                    {
                        if (truncated) break;
                        foreach (var record in capture.Records)
                        {
                            if (pairedText.Contains(record.Text) || options.Any(option => option.Entry.Help?.Contains(record.Text, StringComparison.Ordinal) == true)) continue;
                            if (!retention.TryRetainUnpaired(native.Page, capture.PathIdentity, record, out var retained))
                            { truncated = true; break; }
                            routes.Add(new(retained));
                        }
                    }
                    if (truncated) RecordCoverage(native.Page, "settings-foreign-retention-limit");
                }
            }
            var result = new SettingsChooserSearch(groups, routes.Select(route => route.Entry)).Find(Query);
            var pageMatches = new HashSet<SettingsPage>(result.Pages.SelectMany(group => group.Rows).Select(row => row.Page), ReferenceEqualityComparer.Instance);
            var routeByEntry = routes.ToDictionary<OptionRoute, SettingsSearchEntry>(route => route.Entry, ReferenceEqualityComparer.Instance);
            var matchingRoutes = result.Options.Select(hit => routeByEntry[hit.Entry]).ToLookup(route => route.Entry.Page, ReferenceEqualityComparer.Instance);
            var projected = new List<PageRow>();
            foreach (var group in groups)
            {
                bool first = true;
                foreach (var row in group.Rows)
                {
                    var options = matchingRoutes[row.Page].ToArray();
                    if (!pageMatches.Contains(row.Page) && options.Length == 0) continue;
                    projected.Add(new(sourceMap[row.Page], row, group.Heading, first, options)); first = false;
                }
            }
            state.Rows = projected.ToArray();
        }
        else state.Rows = groups.SelectMany(group => group.Rows.Select((row, index) =>
            new PageRow(sourceMap[row.Page], row, group.Heading, index == 0, Array.Empty<OptionRoute>()))).ToArray();
        state.Query = Query; state.DiscoverySnapshot = pageSnapshot; state.Revision = revision; state.Generation = discovery.Generation;
        state.ObservedRevision = observedRevision; state.Search = search; state.Navigation = navigation; state.ByAuthor = byAuthor; state.GroupSingles = groupSingles;
    }

    private void ObservePage(object nativeMod, object window, Action draw)
    {
        if (!initialized || !searchOn || SettingsReadScope.Current != null || discovery == null || foreignReveal == null)
        { draw(); return; }
        var nativeWindow = (Window)window;
        SettingsPage? page = null;
        Opening? opening = pendingOpening;
        if (opening != null && opening.Generation == discovery.Generation && ReferenceEquals(opening.NativeWindow, nativeWindow) && ReferenceEquals(opening.Page.Native, nativeMod))
        {
            page = opening.Page.Page; pendingOpening = null; CancelReveal();
            opened[nativeWindow] = page; activePageWindow = nativeWindow;
            if (opening.Option?.OwnedId is string id) renderer.RequestReveal(id, opening.Activation);
            else if (opening.Option?.Foreign is { } target) foreignReveal.Request(target);
            else if (opening.Option != null) manualRevealFailure = "settings-option-control-unresolved";
        }
        else if (opened.TryGetValue(nativeWindow, out var existing))
        {
            if (discovery.TryResolve(existing, out var previousMod) && ReferenceEquals(previousMod, nativeMod)) page = existing;
            else { opened.Remove(nativeWindow); CancelReveal(); }
        }
        if (page == null)
        {
            var candidates = discovery.CatalogPages.Where(entry => ReferenceEquals(entry.Native, nativeMod)).ToArray();
            if (candidates.Length == 1) { page = candidates[0].Page; opened[nativeWindow] = page; }
        }
        if (page == null || !discovery.TryResolve(page, out var resolved) || !ReferenceEquals(resolved, nativeMod)) { draw(); return; }
        if (activePageWindow != null && !ReferenceEquals(activePageWindow, nativeWindow)) CancelReveal();
        activePageWindow = nativeWindow;
        visiblePage = page; visibleDraw = draw;
        if (ReferenceEquals(nativeMod, owner)) { draw(); ExplainReveal(); return; }
        CurrentForeignFrame = foreignReveal.ObserveVisible(page, draw);
        if (Event.current?.type == EventType.Repaint)
        {
            if (CurrentForeignFrame.FailureReason is { } failed) RecordCoverage(page, failed);
            else if (CurrentForeignFrame.CoverageReason is { } incomplete) RecordCoverage(page, incomplete);
        }
        if (Event.current?.type == EventType.Repaint && CurrentForeignFrame.FailureReason == null)
        {
            var fresh = CurrentForeignFrame.Options;
            var previous = observed.TryGetValue(page, out var old) ? old :
                discovery.ForeignOptions.TryGetValue(page, out var captured) ? captured : Array.Empty<SettingsForeignOption>();
            var merged = SettingsChooserObservations.MergeVisible(page, previous, fresh, out bool truncated);
            if (truncated) RecordCoverage(page, "settings-foreign-retention-limit");
            // Keep current bounds even when only scroll/layout changed; query rebuilding still
            // depends on readable entry changes rather than every ordinary repaint.
            observed[page] = merged;
            if (!previous.Select(option => option.Entry).SequenceEqual(merged.Select(option => option.Entry)))
                observedRevision++;
        }
        ExplainReveal();
    }

    private void RecordCoverage(SettingsPage page, string reason)
    {
        if (!observedCoverage.TryGetValue(page, out var previous) ||
            SettingsChooserObservations.ReasonKey(SettingsDiscoveryStop.None, previous) != "ReasonBudget")
            observedCoverage[page] = reason;
    }

    private void CancelReveal()
    { pendingOpening = null; manualRevealFailure = null; revealExplanationShown = false; renderer.CancelReveal(); foreignReveal?.Cancel(); }
    private void ExplainReveal()
    {
        if (revealExplanationShown || RevealStatus != "Unavailable") return;
        revealExplanationShown = true;
        Messages.Message(T("RevealUnavailable"), MessageTypeDefOf.RejectInput, false);
    }
    private void RestoreWidgets() { foreach (var state in dialogs.Values) RestoreWidget(state); }
    private static void RestoreWidget(DialogState state)
    {
        if (!state.OwnsLimit) return;
        if (state.Widget.maxSearchTextLength == 512) state.Widget.maxSearchTextLength = state.OriginalLimit;
        state.OwnsLimit = false;
    }
    private void Warn(string message)
    {
        message = message.Length > 320 ? message.Substring(0, 320) : message;
        Failure = message;
        if (!warnings.Add(message)) return;
        try { warning("[fumblesneeze.megamod] S047/S141 settings chooser: " + message); } catch { }
    }
    private static string T(string suffix, params NamedArgument[] arguments) => ("Megamod_Search_" + suffix).Translate(arguments).ToString();
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        if (discovery != null) { CancelReveal(); RestoreWidgets(); discovery.Dispose(); recorder?.Dispose(); }
        for (int i = hooks.Count - 1; i >= 0; i--) hooks[i].Dispose();
        hooks.Clear(); dialogs.Clear(); opened.Clear(); observed.Clear(); observedCoverage.Clear();
        if (ReferenceEquals(Instance, this)) Instance = null;
    }
}
