using System;
using System.Collections.Generic;
using System.Diagnostics;
using Megamod.Features.ModSettingsNavigation;
using Megamod.Features.SettingsSearch.Core;
using RimWorld;
using UnityEngine;
using Verse;

namespace Megamod.Features.SettingsSearch.Runtime;

// Advances one complete root-layout attempt after ordinary visible drawing has returned.
// The eventual UI/lifecycle owner supplies refreshes and repaint calls; no hooks are registered here.
public sealed class SettingsDiscoveryDriver : IDisposable
{
    private readonly int thread = Environment.CurrentManagedThreadId;
    private readonly SettingsNativeCatalog catalog = new();
    private readonly SettingsNeutralReader reader;
    private readonly SettingsForeignRecorder? foreignRecorder;
    private SettingsForeignPageRead? foreignRead;
    private readonly Dictionary<SettingsPage, IReadOnlyList<SettingsForeignOption>> foreignOptions = new(ReferenceEqualityComparer.Instance);
    private readonly Dictionary<SettingsPage, string> failures = new(ReferenceEqualityComparer.Instance);
    private SettingsDiscoverySession? session;
    private SettingsDiscoveryWork? work;
    private Dialog_ModSettings? wrapper;
    private IReadOnlyList<SettingsDiscoveryPage> pages = Array.Empty<SettingsDiscoveryPage>();
    private object? game, world, language;
    private string? languageFolder, catalogFailure;
    private long generation;
    private long? lastRepaint;
    private bool bound, disposed, reading, cancelled;

    public SettingsDiscoveryDriver(Action<string> warning, SettingsForeignRecorder? foreignRecorder = null)
    { reader = new(warning); this.foreignRecorder = foreignRecorder; }
    public IReadOnlyList<SettingsDiscoveryPage> Pages { get { EnsureCurrent(); return pages; } }
    public bool Pending { get { EnsureCurrent(); return catalog.Pending || session is { IsFinished: false }; } }
    public string? CatalogFailure { get { EnsureCurrent(); return catalogFailure; } }
    public IReadOnlyList<SettingsNativePage> CatalogPages { get { EnsureCurrent(); return catalog.Entries; } }
    public long Generation { get { EnsureCurrent(); return generation; } }
    public bool Cancelled { get { EnsureCurrent(); return cancelled || session?.IsCancelled == true; } }
    public IReadOnlyDictionary<SettingsPage, IReadOnlyList<SettingsForeignOption>> ForeignOptions => foreignOptions;
    internal SettingsNeutralReader Reader => reader;
    public bool MatchesSource(Dialog_Options options, IEnumerable<Mod> collection)
    { EnsureCurrent(); return ReferenceEquals(catalog.Source, options) && ReferenceEquals(catalog.SourceCollection, collection); }

    public void Refresh(Dialog_Options options)
    {
        ArgumentNullException.ThrowIfNull(options);
        BindContext();
        catalog.Refresh(options);
    }

    public void RefreshStartup()
    {
        BindContext();
        catalog.RefreshStartup();
    }

    public void ReadNext(long repaintId, bool readPages = true)
    {
        EnsureCurrent();
        if (SettingsReadScope.Current is { } parent) throw parent.Refuse("nested-settings-discovery");
        if (reading) throw new InvalidOperationException("nested-settings-discovery");
        if (!bound || Event.current?.type != EventType.Repaint || Event.current.rawType != EventType.Repaint ||
            (lastRepaint.HasValue && repaintId <= lastRepaint.Value)) return;
        // Refresh/cancel and input deferral do not permit a second attempt in this repaint.
        lastRepaint = repaintId;
        long currentGeneration = generation;
        reading = true;
        try
        {
            if (catalog.Pending)
            {
                var status = catalog.ReadPending(reader, out var reason);
                if (!CanPublish(currentGeneration) || status == SettingsNeutralReadStatus.Deferred) return;
                if (status != SettingsNeutralReadStatus.Captured)
                { catalogFailure = reason ?? "settings-discovery-catalog-failed"; return; }
                session = new(catalog.Pages);
                pages = session.Pages;
                return;
            }
            if (!readPages || session == null || session.IsFinished) return;
            if (work == null)
            {
                work = session.BeginNext(repaintId);
                if (work == null) return;
                pages = session.Pages;
            }
            var currentWork = work;
            var currentSession = session;
            SettingsCaptureSnapshot? capture = null;
            string? failure = null;
            SettingsNeutralReadStatus result;
            bool pageFinished = true;
            int callbackCount = 0;
            double? maximumCallbackMilliseconds = null;
            var elapsed = Stopwatch.StartNew();
            try
            {
                if (!catalog.TryResolve(currentWork.Page, out var native))
                    throw new InvalidOperationException("The current settings page cannot be resolved.");
                // Construct only the native wrapper, before isolation; never open or close it.
                var drawWindow = wrapper ?? new Dialog_ModSettings(native!) { soundAppear = null, soundAmbient = null };
                // Constructors/getters can synchronously refresh or cancel through foreign
                // patches. Never retain an old wrapper after its work has been revoked.
                if (!CanPublish(currentGeneration)) return;
                wrapper = drawWindow;
                var size = drawWindow.InitialSize;
                if (!CanPublish(currentGeneration)) return;
                var rect = new Rect(0, 0, size.x - 2 * Window.StandardMargin, size.y - 2 * Window.StandardMargin);
                if (!float.IsFinite(rect.width) || !float.IsFinite(rect.height) || rect.width <= 0 || rect.height <= 0)
                    throw new InvalidOperationException("The native settings layout is unavailable.");
                Action draw = () =>
                {
                    if (!CanPublish(currentGeneration))
                        throw SettingsReadScope.Current!.Refuse("settings-discovery-work-replaced");
                    callbackCount++;
                    drawWindow.DoWindowContents(rect);
                };
                if (foreignRecorder != null && foreignRecorder.Available)
                {
                    foreignRead ??= new SettingsForeignPageRead(currentWork.Page, foreignRecorder);
                    result = foreignRead.Step(reader, draw);
                    callbackCount = foreignRead.LastDrawCount;
                    maximumCallbackMilliseconds = foreignRead.LastMaximumDrawMilliseconds;
                    capture = foreignRead.LastCapture;
                    failure = foreignRead.FailureReason;
                    pageFinished = foreignRead.Finished;
                }
                else result = reader.Read(currentWork.Page, "root", draw, out capture, out failure);
            }
            catch (Exception)
            {
                result = SettingsNeutralReadStatus.Failed;
                failure = "settings-discovery-page-dispatch-failed";
            }
            elapsed.Stop();
            if (!CanPublish(currentGeneration) || result == SettingsNeutralReadStatus.Deferred) return;
            if (result != SettingsNeutralReadStatus.Captured || capture == null)
            {
                failures[currentWork.Page] = failure ?? "settings-discovery-page-failed";
                capture = new(currentWork.Page, "root", Array.Empty<SettingsCaptureRecord>(),
                    SettingsCaptureStatus.Failed, SettingsCaptureFailure.RecordingFailed);
            }
            if (!currentSession.Complete(currentWork, capture, elapsed.Elapsed.TotalMilliseconds, pageFinished, callbackCount, maximumCallbackMilliseconds))
                throw new InvalidOperationException("The current settings discovery receipt was rejected.");
            if (foreignRead != null && result == SettingsNeutralReadStatus.Captured)
                foreignOptions[currentWork.Page] = foreignRead.Options;
            else if (result != SettingsNeutralReadStatus.Captured) foreignOptions.Remove(currentWork.Page);
            if (failure != null) failures[currentWork.Page] = failure;
            work = null;
            pages = currentSession.Pages;
            if (pageFinished || pages.Any(page => ReferenceEquals(page.Page, currentWork.Page) && page.Status != SettingsDiscoveryStatus.Reading))
            { wrapper = null; foreignRead = null; }
        }
        finally { reading = false; }
    }

    public void Cancel()
    {
        EnsureCurrent();
        cancelled = true;
        generation++;
        // Startup construction can reenter before a pending catalog request exists.
        // There are no completed opening routes to retain until a session was acquired.
        if (session == null || catalog.Pending) catalog.Clear();
        session?.Cancel();
        work = null;
        wrapper = null;
        foreignRead = null;
        pages = session?.Pages ?? Array.Empty<SettingsDiscoveryPage>();
    }

    public void Clear() { RequireOwner(); ClearCore(); }

    public string? FailureReason(SettingsPage page)
    {
        ArgumentNullException.ThrowIfNull(page);
        EnsureCurrent();
        return failures.TryGetValue(page, out var reason) ? reason : null;
    }

    public bool TryResolve(SettingsPage page, out Mod? mod)
    { EnsureCurrent(); return catalog.TryResolve(page, out mod); }

    public void Dispose()
    {
        if (Environment.CurrentManagedThreadId != thread || !UnityData.IsInMainThread)
            throw new InvalidOperationException("settings-discovery-gui-thread-required");
        if (disposed) return;
        if (reading) throw SettingsReadScope.Current?.Refuse("settings-discovery-dispose-during-read")
            ?? new InvalidOperationException("settings-discovery-dispose-during-read");
        try { reader.Dispose(); }
        finally { ClearCore(); disposed = true; }
    }

    private void BindContext()
    {
        RequireOwner();
        ClearCore();
        game = Current.Game;
        world = Current.Game?.World;
        language = LanguageDatabase.activeLanguage;
        languageFolder = LanguageDatabase.activeLanguage?.folderName;
        bound = true;
    }

    private bool CanPublish(long expected)
    { EnsureCurrent(); return generation == expected; }

    private void EnsureCurrent()
    {
        RequireOwner();
        if (bound && (!ReferenceEquals(game, Current.Game) || !ReferenceEquals(world, Current.Game?.World) ||
            !ReferenceEquals(language, LanguageDatabase.activeLanguage) || languageFolder != LanguageDatabase.activeLanguage?.folderName))
        {
            ClearCore();
            catalogFailure = "settings-discovery-context-changed";
        }
    }

    private void ClearCore()
    {
        generation++;
        cancelled = false;
        catalog.Clear();
        session?.Cancel();
        session = null;
        work = null;
        wrapper = null;
        foreignRead = null;
        foreignOptions.Clear();
        pages = Array.Empty<SettingsDiscoveryPage>();
        failures.Clear();
        catalogFailure = null;
        game = world = language = null;
        languageFolder = null;
        bound = false;
    }

    private void RequireOwner()
    {
        if (Environment.CurrentManagedThreadId != thread || !UnityData.IsInMainThread)
            throw new InvalidOperationException("settings-discovery-gui-thread-required");
        ObjectDisposedException.ThrowIf(disposed, this);
    }
}
