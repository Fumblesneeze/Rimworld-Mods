using System;
using System.Collections.Generic;
using System.Linq;
using Megamod.Features.ModSettingsNavigation;

namespace Megamod.Features.SettingsSearch.Core;

public enum SettingsDiscoveryStatus { Pending, Reading, Partial, Failed, Cancelled }
public enum SettingsDiscoveryStop { None, Cancelled, CallbackLimit, TimeLimit, SlowCallback, RetentionLimit, CaptureFailed }
public sealed record SettingsDiscoveryPage(SettingsPage Page, SettingsDiscoveryStatus Status,
    SettingsDiscoveryStop StopReason, IReadOnlyList<SettingsCaptureSnapshot> Captures, int Callbacks,
    double ElapsedMilliseconds);

public sealed class SettingsDiscoveryWork
{
    internal SettingsDiscoveryWork(long generation, int pageOrdinal, SettingsPage page)
    { Generation = generation; PageOrdinal = pageOrdinal; Page = page; }
    public long Generation { get; }
    public int PageOrdinal { get; }
    public SettingsPage Page { get; }
}

// Owns admission/progress only. The native caller owns valid repaint phases, callbacks and elapsed-time measurement.
public sealed class SettingsDiscoverySession
{
    private sealed class PageState
    {
        internal PageState(SettingsPage page) => Page = page;
        internal readonly SettingsPage Page;
        internal SettingsDiscoveryStatus Status;
        internal SettingsDiscoveryStop StopReason;
        internal readonly List<SettingsCaptureSnapshot> Captures = new();
        internal int Callbacks;
        internal double ElapsedMilliseconds;
    }

    private PageState[] pages;
    private int nextPage;
    private long generation = 1;
    private SettingsDiscoveryWork? inFlight;
    private long? lastRepaint;
    private readonly int maxCallbacksPerPage;
    private readonly double maxPageMilliseconds;
    private readonly double maxCallbackMilliseconds;

    public SettingsDiscoverySession(IReadOnlyList<SettingsPage> pages, int maxCallbacksPerPage = 96,
        double maxPageMilliseconds = 500, double maxCallbackMilliseconds = 100)
    {
        if (maxCallbacksPerPage <= 0) throw new ArgumentOutOfRangeException(nameof(maxCallbacksPerPage));
        if (!double.IsFinite(maxPageMilliseconds) || maxPageMilliseconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxPageMilliseconds));
        if (!double.IsFinite(maxCallbackMilliseconds) || maxCallbackMilliseconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(maxCallbackMilliseconds));
        this.pages = CopyPages(pages);
        this.maxCallbacksPerPage = maxCallbacksPerPage;
        this.maxPageMilliseconds = maxPageMilliseconds;
        this.maxCallbackMilliseconds = maxCallbackMilliseconds;
    }
    public IReadOnlyList<SettingsDiscoveryPage> Pages => Array.AsReadOnly(pages.Select(page =>
        new SettingsDiscoveryPage(page.Page, page.Status, page.StopReason,
            Array.AsReadOnly(page.Captures.ToArray()), page.Callbacks, page.ElapsedMilliseconds)).ToArray());
    public bool IsFinished => nextPage == pages.Length;
    public bool IsCancelled { get; private set; }
    public SettingsDiscoveryWork? BeginNext(long repaintId)
    {
        if (IsFinished || inFlight != null || (lastRepaint.HasValue && repaintId <= lastRepaint.Value)) return null;
        pages[nextPage].Status = SettingsDiscoveryStatus.Reading;
        lastRepaint = repaintId;
        return inFlight = new SettingsDiscoveryWork(generation, nextPage, pages[nextPage].Page);
    }
    public bool Complete(SettingsDiscoveryWork work, SettingsCaptureSnapshot? capture,
        double elapsedMilliseconds, bool pageFinished, int callbackCount = 1,
        double? maximumCallbackMilliseconds = null)
    {
        if (inFlight == null || !ReferenceEquals(inFlight, work) || work.Generation != generation) return false;
        if (capture != null && !ReferenceEquals(capture.Page, work.Page)) return false;
        if (!double.IsFinite(elapsedMilliseconds) || elapsedMilliseconds < 0)
            throw new ArgumentOutOfRangeException(nameof(elapsedMilliseconds));
        if (callbackCount < 0) throw new ArgumentOutOfRangeException(nameof(callbackCount));
        double longestCallback = maximumCallbackMilliseconds ?? elapsedMilliseconds;
        if (!double.IsFinite(longestCallback) || longestCallback < 0 || longestCallback > elapsedMilliseconds)
            throw new ArgumentOutOfRangeException(nameof(maximumCallbackMilliseconds));
        var retained = capture == null ? null : capture with { Records = Array.AsReadOnly(capture.Records.ToArray()) };
        var page = pages[nextPage];
        int completedCallbacks = checked(page.Callbacks + callbackCount);
        if (retained != null) page.Captures.Add(retained);
        page.Callbacks = completedCallbacks;
        page.ElapsedMilliseconds += elapsedMilliseconds;
        inFlight = null;
        page.StopReason = capture?.Status == SettingsCaptureStatus.Failed ? SettingsDiscoveryStop.CaptureFailed
            : capture?.Status == SettingsCaptureStatus.Truncated ? SettingsDiscoveryStop.RetentionLimit
            : longestCallback > maxCallbackMilliseconds ? SettingsDiscoveryStop.SlowCallback
            : !pageFinished && page.ElapsedMilliseconds >= maxPageMilliseconds ? SettingsDiscoveryStop.TimeLimit
            : !pageFinished && page.Callbacks >= maxCallbacksPerPage ? SettingsDiscoveryStop.CallbackLimit
            : SettingsDiscoveryStop.None;
        if (pageFinished || page.StopReason != SettingsDiscoveryStop.None)
        {
            page.Status = page.StopReason == SettingsDiscoveryStop.CaptureFailed
                ? SettingsDiscoveryStatus.Failed : SettingsDiscoveryStatus.Partial;
            if (page.Status == SettingsDiscoveryStatus.Failed) page.Captures.Clear();
            nextPage++;
        }
        return true;
    }
    public void Cancel()
    {
        // Keep completed observations, including earlier callbacks of the interrupted page.
        for (int i = nextPage; i < pages.Length; i++)
        {
            pages[i].Status = SettingsDiscoveryStatus.Cancelled;
            pages[i].StopReason = SettingsDiscoveryStop.Cancelled;
        }
        inFlight = null;
        nextPage = pages.Length;
        IsCancelled = true;
    }

    public void Refresh(IReadOnlyList<SettingsPage> pages)
    {
        // Validate the replacement before invalidating current progress or an in-flight receipt.
        var replacement = CopyPages(pages);
        long nextGeneration = checked(generation + 1);
        this.pages = replacement;
        generation = nextGeneration;
        nextPage = 0;
        inFlight = null;
        // Catalog generations may change within one native repaint; admission must still advance.
        IsCancelled = false;
    }

    private static PageState[] CopyPages(IReadOnlyList<SettingsPage> pages)
    {
        ArgumentNullException.ThrowIfNull(pages);
        return pages.Select(page => new PageState(page ?? throw new ArgumentException("A page descriptor is missing.", nameof(pages)))).ToArray();
    }
}
