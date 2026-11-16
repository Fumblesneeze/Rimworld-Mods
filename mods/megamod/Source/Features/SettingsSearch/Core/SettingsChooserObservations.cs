using System;
using System.Collections.Generic;
using System.Linq;
using Megamod.Features.ModSettingsNavigation;

namespace Megamod.Features.SettingsSearch.Core;

// An ordinary viewport is a partial observation. Only an unambiguous observed control updates
// its last-observed record; absence from that viewport never deletes another captured result.
public static class SettingsChooserObservations
{
    public static IReadOnlyList<SettingsForeignOption> MergeVisible(SettingsPage page,
        IReadOnlyList<SettingsForeignOption> retained, IReadOnlyList<SettingsForeignOption> visible) =>
        MergeVisible(page, retained, visible, out _);

    public static IReadOnlyList<SettingsForeignOption> MergeVisible(SettingsPage page,
        IReadOnlyList<SettingsForeignOption> retained, IReadOnlyList<SettingsForeignOption> visible, out bool truncated)
    {
        if (retained.Concat(visible).Any(option => !ReferenceEquals(option.Entry.Page, page) || !ReferenceEquals(option.Target.Page, page)))
            throw new ArgumentException("Visible observations must retain their exact page descriptor.");
        var result = retained.ToList();
        var nextOrdinal = retained.GroupBy(option => option.Entry.PathIdentity, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Max(option => option.Entry.Occurrence) + 1, StringComparer.Ordinal);
        var prior = retained.Select((option, index) => (Option: option, Index: index))
            .GroupBy(item => Key(item.Option)).ToDictionary(group => group.Key, group => group.Select(item => item.Index).ToArray());
        foreach (var group in visible.GroupBy(Key))
        {
            var fresh = group.ToArray();
            var previous = prior.TryGetValue(group.Key, out var indices) ? indices : Array.Empty<int>();
            if (previous.Length == 1 && fresh.Length == 1)
            {
                var old = retained[previous[0]];
                result[previous[0]] = fresh[0] with { Entry = fresh[0].Entry with { Occurrence = old.Entry.Occurrence } };
                continue;
            }
            // Equal controls cannot be paired by viewport encounter ordinal after culling.
            // Preserve their existing values; add only newly witnessed extra occurrences.
            foreach (var addition in fresh.Skip(previous.Length))
            {
                int ordinal = nextOrdinal.TryGetValue(addition.Entry.PathIdentity, out int next) ? next : 0;
                nextOrdinal[addition.Entry.PathIdentity] = ordinal + 1;
                result.Add(addition with { Entry = addition.Entry with { Occurrence = ordinal } });
            }
        }
        if (Retain(result, out var admitted)) { truncated = false; return admitted; }
        truncated = true;
        // A new visible batch must not evict unseen last-observed controls to make room.
        // Refuse that update when the combined retained page would exceed the shared budget.
        if (retained.Count == 0) return admitted;
        Retain(retained, out var priorAdmitted);
        return priorAdmitted;
    }

    private static bool Retain(IReadOnlyList<SettingsForeignOption> options, out IReadOnlyList<SettingsForeignOption> admitted)
    {
        var budget = new SettingsForeignRetention();
        var result = new List<SettingsForeignOption>(Math.Min(options.Count, SettingsForeignRetention.MaximumOptions));
        foreach (var option in options)
        {
            if (!budget.TryRetain(option, out var shared)) { admitted = result.AsReadOnly(); return false; }
            result.Add(shared);
        }
        admitted = result.AsReadOnly(); return true;
    }

    private static (string Path, SettingsForeignControlKind Kind, string Label, string? Help) Key(SettingsForeignOption option) =>
        (option.Entry.PathIdentity, option.Target.Kind, option.Target.Label, option.Target.Help);

    // These suffixes are presentation categories; detailed internal failure codes stay diagnostic.
    public static string? ReasonKey(SettingsDiscoveryStop stop, string? failure)
    {
        bool Has(string value) => failure?.Contains(value, StringComparison.Ordinal) == true;
        if (stop is SettingsDiscoveryStop.CallbackLimit or SettingsDiscoveryStop.TimeLimit or SettingsDiscoveryStop.SlowCallback or SettingsDiscoveryStop.RetentionLimit ||
            Has("limit") || Has("budget") || Has("slow-callback")) return "ReasonBudget";
        if (Has("changed") || Has("replaced") || Has("stale") || Has("missing") || Has("not-selected")) return "ReasonChanged";
        if (Has("unavailable") || Has("unsupported") || Has("admission") || Has("construction") || Has("settings-read-window-") || Has("settings-read-settings-write"))
            return "ReasonUnsupported";
        if (stop == SettingsDiscoveryStop.CaptureFailed || failure != null) return "ReasonCallback";
        return null;
    }
}

// Remembers the actual enumerable identity, including while a deferred acquisition is pending
// or has failed. Merely lacking a completed catalog is not another refresh request.
public sealed class SettingsChooserSourceRefresh
{
    private object? source;
    public bool NeedsRequest { get; private set; }
    public bool ObserveSource(object incoming)
    {
        ArgumentNullException.ThrowIfNull(incoming);
        if (ReferenceEquals(source, incoming)) return false;
        source = incoming; NeedsRequest = true; return true;
    }
    public void MarkRequested() => NeedsRequest = false;
    public void RequestRefresh() => NeedsRequest = true;
}
