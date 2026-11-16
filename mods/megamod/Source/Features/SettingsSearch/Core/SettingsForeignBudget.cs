using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Megamod.Features.ModSettingsNavigation;

namespace Megamod.Features.SettingsSearch.Core;

public sealed class SettingsForeignTextBudget
{
    public SettingsForeignTextBudget(int limit = 262144) => Remaining = limit >= 0 ? limit : throw new ArgumentOutOfRangeException(nameof(limit));
    public int Remaining { get; private set; }
    public bool Reserve(long units)
    {
        if (units < 0 || units > Remaining) return false;
        Remaining -= (int)units;
        return true;
    }
    public static long PackedLength(string text) => text.Length + 1L + text.Length.ToString(CultureInfo.InvariantCulture).Length;
    public static long JoinedLength(IEnumerable<string> parts, int separatorLength)
    {
        long units = 0;
        bool first = true;
        foreach (string part in parts) { units += part.Length + (first ? 0 : separatorLength); first = false; }
        return units;
    }
}

public sealed record SettingsForeignPath(IReadOnlyList<SettingsForeignTab> Tabs, string Identity, string? Display);

// Paths are interned by their component values before any encoded/display string is allocated.
public sealed class SettingsForeignPaths
{
    private readonly Dictionary<IReadOnlyList<SettingsForeignTab>, SettingsForeignPath> paths = new(new PathComparer());
    private readonly SettingsForeignTextBudget budget;
    public SettingsForeignPaths(SettingsForeignTextBudget budget) => this.budget = budget;
    public bool TryGet(IReadOnlyList<SettingsForeignTab> tabs, out SettingsForeignPath path)
    {
        if (paths.TryGetValue(tabs, out path!)) return true;
        long units = 8 + SettingsForeignTextBudget.JoinedLength(tabs.Select(tab => tab.Label), 3);
        foreach (var tab in tabs)
            units += tab.Group.Length + (long)tab.Label.Length + SettingsForeignTextBudget.PackedLength(tab.Group) + SettingsForeignTextBudget.PackedLength(tab.Label);
        if (!budget.Reserve(units)) { path = null!; return false; }
        var copy = Array.AsReadOnly(tabs.ToArray());
        path = new(copy, "foreign:" + string.Concat(copy.Select(tab => SettingsForeignControls.Pack(tab.Group) + SettingsForeignControls.Pack(tab.Label))),
            copy.Count == 0 ? null : string.Join(" / ", copy.Select(tab => tab.Label)));
        paths.Add(copy, path);
        return true;
    }
    private sealed class PathComparer : IEqualityComparer<IReadOnlyList<SettingsForeignTab>>
    {
        public bool Equals(IReadOnlyList<SettingsForeignTab>? a, IReadOnlyList<SettingsForeignTab>? b) =>
            ReferenceEquals(a, b) || a != null && b != null && a.SequenceEqual(b);
        public int GetHashCode(IReadOnlyList<SettingsForeignTab> path)
        {
            var hash = new HashCode();
            foreach (var tab in path) { hash.Add(tab.Group, StringComparer.Ordinal); hash.Add(tab.Label, StringComparer.Ordinal); }
            return hash.ToHashCode();
        }
    }
}

public sealed class SettingsForeignRevealLifetime
{
    private int pendingFrames;
    private double? visibleAt;
    public void Start() { pendingFrames = 0; visibleAt = null; }
    public SettingsForeignRevealStatus NextRepaint(double now) => visibleAt.HasValue
        ? now - visibleAt.Value >= 3 ? SettingsForeignRevealStatus.Expired : SettingsForeignRevealStatus.Highlighting
        : ++pendingFrames > 120 ? SettingsForeignRevealStatus.Unavailable : SettingsForeignRevealStatus.Pending;
    public void Visible(double now) => visibleAt ??= now;
}

public sealed class SettingsForeignRetention
{
    public const int MaximumOptions = 4096, MaximumTextUnits = 262144;
    private readonly SettingsForeignTextBudget budget = new(MaximumTextUnits);
    private readonly SettingsForeignPaths paths;
    private readonly Dictionary<string, string> unpairedPaths = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> capturePaths = new(StringComparer.Ordinal);
    public SettingsForeignRetention() => paths = new(budget);
    public int Count { get; private set; }
    internal bool TryRetainPath(IReadOnlyList<SettingsForeignTab> tabs, out SettingsForeignPath path) => paths.TryGet(tabs, out path);
    public bool TryRetain(SettingsForeignOption option, out SettingsForeignOption retained)
    {
        retained = null!;
        long units = option.Entry.Label.Length + (long)(option.Entry.Help?.Length ?? 0) + (option.Entry.DisplayedValue?.Length ?? 0);
        if (Count >= MaximumOptions || !budget.Reserve(units) || !paths.TryGet(option.Target.Tabs, out var path)) return false;
        retained = option with { Entry = option.Entry with { PathIdentity = path.Identity, Tab = path.Display },
            Target = option.Target with { Tabs = path.Tabs } };
        Count++;
        return true;
    }
    public bool TryRetainUnpaired(SettingsPage page, string pathIdentity, SettingsCaptureRecord record,
        out SettingsSearchEntry retained)
    {
        retained = null!;
        if (Count >= MaximumOptions || !budget.Reserve(record.Text.Length)) return false;
        if (!unpairedPaths.TryGetValue(pathIdentity, out string? path))
        {
            if (!budget.Reserve(pathIdentity.Length * 2L + 9)) return false;
            path = pathIdentity + ":unpaired";
            unpairedPaths.Add(pathIdentity, path);
        }
        bool tooltip = record.Kind == SettingsCaptureKind.Tooltip;
        retained = new(page, path, record.Occurrence, tooltip ? "" : record.Text, tooltip ? record.Text : null);
        Count++;
        return true;
    }
    public bool TryRetainCapture(SettingsCaptureSnapshot capture, out SettingsCaptureSnapshot retained)
    {
        var records = new List<SettingsCaptureRecord>();
        bool complete = true;
        if (!capturePaths.TryGetValue(capture.PathIdentity, out string? path))
        {
            if (!budget.Reserve(capture.PathIdentity.Length)) complete = false;
            else capturePaths.Add(capture.PathIdentity, path = capture.PathIdentity);
        }
        if (complete)
            foreach (var record in capture.Records)
            {
                if (Count >= MaximumOptions || !budget.Reserve(record.Text.Length)) { complete = false; break; }
                records.Add(record);
                Count++;
            }
        retained = capture with { PathIdentity = path ?? "foreign:", Records = records.AsReadOnly(),
            Status = complete ? capture.Status : SettingsCaptureStatus.Truncated };
        return complete;
    }
}

public sealed class SettingsForeignReadBudget
{
    public int DrawCount { get; private set; }
    public double ElapsedMilliseconds { get; private set; }
    public string? StopReason { get; private set; }
    public bool CanExplore(int restorationDraws)
    {
        if (StopReason != null) return false;
        if (DrawCount + 1 + restorationDraws > 96) StopReason = "settings-foreign-draw-limit";
        return StopReason == null;
    }
    // Called after every actual callback, including mandatory restoration. An overrun stops
    // new exploration, never the undo callbacks needed to restore the original page.
    public void RecordDraw(double elapsedMilliseconds)
    {
        if (!double.IsFinite(elapsedMilliseconds) || elapsedMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(elapsedMilliseconds));
        DrawCount++;
        ElapsedMilliseconds += elapsedMilliseconds;
        if (elapsedMilliseconds > 100) StopReason ??= "settings-foreign-slow-callback";
        else if (ElapsedMilliseconds >= 500) StopReason ??= "settings-foreign-time-limit";
        else if (DrawCount >= 96) StopReason ??= "settings-foreign-draw-limit";
    }
}
