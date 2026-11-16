using System;
using System.Collections.Generic;
using System.Linq;
using Megamod.Features.ModSettingsNavigation;

namespace Megamod.Features.SettingsSearch.Core;

public enum SettingsForeignControlKind { Checkbox, Radio, Slider, Text, Button }
public sealed record SettingsForeignTab(string Group, string Label);
public sealed record SettingsForeignWidget(SettingsForeignControlKind Kind, string? Label, string? Value,
    SettingsCaptureRect Bounds, IReadOnlyList<int> Scrolls, IReadOnlyList<SettingsForeignTab>? Tabs = null, bool CanReveal = true);
public sealed record SettingsForeignTarget(SettingsPage Page, IReadOnlyList<SettingsForeignTab> Tabs,
    SettingsForeignControlKind Kind, string Label, string? Help, bool CanReveal = true);
public sealed record SettingsForeignOption(SettingsSearchEntry Entry, SettingsForeignTarget Target,
    SettingsCaptureRect Bounds, IReadOnlyList<int> Scrolls);
public enum SettingsForeignRevealStatus { Idle, Pending, Highlighting, Expired, Unavailable, Cancelled }
public sealed record SettingsForeignProjection(IReadOnlyList<SettingsForeignOption> Options, bool Truncated);

// Native widget observations establish controls. Text-only rectangles never become reveal targets.
public static class SettingsForeignControls
{
    public static IReadOnlyList<SettingsForeignOption> Project(SettingsCaptureSnapshot capture,
        IReadOnlyList<SettingsForeignWidget> widgets, IReadOnlyList<SettingsForeignTab> tabs) => ProjectBounded(capture, widgets, tabs).Options;

    public static SettingsForeignProjection ProjectBounded(SettingsCaptureSnapshot capture,
        IReadOnlyList<SettingsForeignWidget> widgets, IReadOnlyList<SettingsForeignTab> tabs, int maxTextUnits = 262144)
    {
        var budget = new SettingsForeignTextBudget(maxTextUnits);
        return ProjectBounded(capture, widgets, tabs, budget, new(budget));
    }

    internal static SettingsForeignProjection ProjectBounded(SettingsCaptureSnapshot capture,
        IReadOnlyList<SettingsForeignWidget> widgets, IReadOnlyList<SettingsForeignTab> tabs,
        SettingsForeignTextBudget budget, SettingsForeignPaths paths)
    {
        if (capture.Status == SettingsCaptureStatus.Failed) return new(Array.Empty<SettingsForeignOption>(), false);
        var result = new List<SettingsForeignOption>();
        bool truncated = capture.Status == SettingsCaptureStatus.Truncated;
        var externalLabels = widgets.Any(widget => string.IsNullOrWhiteSpace(widget.Label))
            ? capture.Records.Where(record => record.Kind == SettingsCaptureKind.Text &&
                !string.IsNullOrWhiteSpace(record.Text) && !widgets.Any(widget => Contains(widget.Bounds, record.Bounds))).ToArray()
            : Array.Empty<SettingsCaptureRecord>();
        foreach (var widget in widgets)
        {
            if (!Valid(widget.Bounds)) continue;
            string? label = widget.Label;
            var bounds = widget.Bounds;
            if (string.IsNullOrWhiteSpace(label))
            {
                // A native unlabeled editor may have one adjacent native label. Ties are unknown.
                var labels = externalLabels.Where(record => record.Text != widget.Value && Adjacent(record.Bounds, bounds))
                    .ToArray();
                if (labels.Length != 1) continue;
                label = labels[0].Text;
                bounds = Union(bounds, labels[0].Bounds);
            }
            // Literal tooltip emissions only; lazy tooltip getters are never evaluated here.
            var help = capture.Records.Where(record => record.Kind == SettingsCaptureKind.Tooltip &&
                    Overlaps(record.Bounds, bounds)).Select(record => record.Text).Distinct(StringComparer.Ordinal).ToArray();
            string? value = widget.Value;
            string[] displays = Array.Empty<string>();
            if (value == null && widget.Kind is SettingsForeignControlKind.Text or SettingsForeignControlKind.Slider)
            {
                displays = capture.Records.Where(record => record.Kind == SettingsCaptureKind.Text &&
                    record.Text != label && Contains(widget.Bounds, record.Bounds)).Select(record => record.Text)
                    .Distinct(StringComparer.Ordinal).ToArray();
            }
            long units = label!.Length + (long)(value?.Length ?? 0) + SettingsForeignTextBudget.JoinedLength(help, 1) +
                SettingsForeignTextBudget.JoinedLength(displays, 1);
            if (result.Count >= 4096 || !budget.Reserve(units) || !paths.TryGet(widget.Tabs ?? tabs, out var path))
            { truncated = true; break; }
            string? helpText = help.Length == 0 ? null : string.Join("\n", help);
            if (displays.Length != 0) value = string.Join(" ", displays);
            var target = new SettingsForeignTarget(capture.Page, path.Tabs, widget.Kind, label, helpText, widget.CanReveal);
            var entry = new SettingsSearchEntry(capture.Page, path.Identity, result.Count, label, helpText, value, path.Display);
            result.Add(new(entry, target, bounds, Array.AsReadOnly(widget.Scrolls.ToArray())));
        }
        return new(Array.AsReadOnly(result.ToArray()), truncated);
    }

    public static SettingsForeignOption? Resolve(SettingsForeignTarget target, SettingsPage page,
        IReadOnlyList<SettingsForeignOption> current, out string? failure)
    {
        failure = null;
        if (!target.CanReveal) { failure = "settings-foreign-tab-navigation-incomplete"; return null; }
        if (!ReferenceEquals(target.Page, page)) { failure = "settings-foreign-page-changed"; return null; }
        var matches = current.Where(option => ReferenceEquals(option.Target.Page, page) &&
            option.Target.CanReveal && option.Target.Kind == target.Kind && option.Target.Label == target.Label && option.Target.Help == target.Help &&
            option.Target.Tabs.SequenceEqual(target.Tabs)).Take(2).ToArray();
        if (matches.Length != 1)
        { failure = matches.Length == 0 ? "settings-foreign-control-missing" : "settings-foreign-control-ambiguous"; return null; }
        return matches[0];
    }

    public static string PathIdentity(IEnumerable<SettingsForeignTab> tabs)
    {
        var path = tabs as IReadOnlyList<SettingsForeignTab> ?? tabs.Take(257).ToArray();
        if (path.Count > 256 || !new SettingsForeignPaths(new()).TryGet(path, out var captured))
            throw new ArgumentException("Settings tab path exceeds the retention budget.", nameof(tabs));
        return captured.Identity;
    }
    public static string Pack(string value) => value.Length + ":" + value;
    public static bool Valid(SettingsCaptureRect rect) => float.IsFinite(rect.X) && float.IsFinite(rect.Y) &&
        float.IsFinite(rect.Width) && rect.Width > 0 && float.IsFinite(rect.Height) && rect.Height > 0 &&
        float.IsFinite(rect.X + rect.Width) && float.IsFinite(rect.Y + rect.Height);
    public static bool Contains(SettingsCaptureRect outer, SettingsCaptureRect inner) =>
        inner.X >= outer.X - 1 && inner.Y >= outer.Y - 1 &&
        inner.X + inner.Width <= outer.X + outer.Width + 1 && inner.Y + inner.Height <= outer.Y + outer.Height + 1;
    public static bool Overlaps(SettingsCaptureRect a, SettingsCaptureRect b) =>
        a.X < b.X + b.Width && a.X + a.Width > b.X && a.Y < b.Y + b.Height && a.Y + a.Height > b.Y;
    public static SettingsCaptureRect Union(SettingsCaptureRect a, SettingsCaptureRect b) => new(
        Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Max(a.X + a.Width, b.X + b.Width) - Math.Min(a.X, b.X),
        Math.Max(a.Y + a.Height, b.Y + b.Height) - Math.Min(a.Y, b.Y));

    private static bool Adjacent(SettingsCaptureRect label, SettingsCaptureRect control) =>
        (label.X + label.Width <= control.X + 1 && control.X - label.X - label.Width <= 24 &&
         label.Y < control.Y + control.Height && label.Y + label.Height > control.Y) ||
        (label.Y + label.Height <= control.Y + 1 && control.Y - label.Y - label.Height <= 12 &&
         label.X < control.X + control.Width && label.X + label.Width > control.X);
}
