using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Megamod.Features.ModSettingsNavigation;

// Tokens are opaque dialog-local identities. Opening remains the native adapter's responsibility.
public sealed record SettingsPage(string PageIdentity, string OpenToken, string? Title, string? ModName,
    string? RawAuthors, string? PackageIdentity = null, string? ProviderPageIdentity = null,
    bool IsHugsLibGenerated = false);

public sealed record SettingsChooserLabels(string VariousAuthors, string UnknownAuthor, string HugsLibGenerated,
    string VanillaExpanded, string SargBjornson, string Helixien, string UntitledPage);

public sealed record SettingsChooserRow(SettingsPage Page, string Title, string NormalizedAuthor,
    string? DisplayGroup, bool ShowAuthor, int InputOrdinal);

public sealed class SettingsChooserGroup
{
    internal SettingsChooserGroup(string? heading, SettingsChooserRow[] rows)
    { Heading = heading; Rows = Array.AsReadOnly(rows); }
    public string? Heading { get; }
    public IReadOnlyList<SettingsChooserRow> Rows { get; }
}

public sealed class SettingsChooser
{
    private readonly SettingsPage[] pages;
    private readonly StringComparer textOrder;
    private readonly SettingsChooserLabels labels;

    public SettingsChooser(IEnumerable<SettingsPage> pages, SettingsChooserLabels labels, CultureInfo culture)
    {
        this.pages = pages.ToArray();
        textOrder = StringComparer.Create(culture, false);
        this.labels = labels;
    }

    public IReadOnlyList<SettingsChooserGroup> Project(string? query = null, bool byAuthor = true, bool groupSingles = true)
    {
        var rows = pages.Select((page, ordinal) => new SettingsChooserRow(page, SafeTitle(page),
            NormalizeAuthor(page), null, true, ordinal)).ToArray();
        if (!byAuthor)
        {
            var matches = Sort(rows.Where(row => Matches(row, query)));
            return matches.Length == 0 ? Array.Empty<SettingsChooserGroup>() :
                Array.AsReadOnly(new[] { new SettingsChooserGroup(null, matches) });
        }

        var counts = rows.GroupBy(row => row.NormalizedAuthor, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Count(), StringComparer.Ordinal);
        var grouped = rows.GroupBy(row =>
        {
            var synthetic = groupSingles && counts[row.NormalizedAuthor] == 1;
            // A translated bucket label can also be a real author's name.
            return (Synthetic: synthetic, Author: synthetic ? string.Empty : row.NormalizedAuthor);
        }).OrderBy(group => group.Key.Synthetic)
            .ThenBy(group => group.Key.Author, textOrder)
            .Select(group =>
            {
                var heading = group.Key.Synthetic ? labels.VariousAuthors : group.Key.Author;
                var matches = group.Select(row => row with
                {
                    DisplayGroup = heading,
                    ShowAuthor = group.Key.Synthetic
                }).Where(row => Matches(row, query));
                return new SettingsChooserGroup(heading, Sort(matches));
            }).Where(group => group.Rows.Count > 0).ToArray();
        return Array.AsReadOnly(grouped);
    }

    private string SafeTitle(SettingsPage page) => !string.IsNullOrEmpty(page.Title) ? page.Title! :
        !string.IsNullOrEmpty(page.ModName) ? page.ModName! :
        !string.IsNullOrEmpty(page.PackageIdentity) ? page.PackageIdentity! : labels.UntitledPage;

    private string NormalizeAuthor(SettingsPage page)
    {
        if (page.IsHugsLibGenerated) return labels.HugsLibGenerated;
        if (Contains(page.ModName, "Vanilla") && Contains(page.ModName, "Expanded")) return labels.VanillaExpanded;
        if (Contains(page.RawAuthors, "sarg")) return labels.SargBjornson;
        if (Contains(page.RawAuthors, "helixien")) return labels.Helixien;
        return string.IsNullOrEmpty(page.RawAuthors) ? labels.UnknownAuthor : page.RawAuthors!;
    }

    private static bool Contains(string? value, string query) =>
        value?.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool Matches(SettingsChooserRow row, string? query) => string.IsNullOrEmpty(query) ||
        Contains(row.Title, query!) || Contains(row.Page.ModName, query!) || Contains(row.Page.RawAuthors, query!) ||
        Contains(row.NormalizedAuthor, query!) || Contains(row.DisplayGroup, query!);

    private SettingsChooserRow[] Sort(IEnumerable<SettingsChooserRow> rows) => rows
        .OrderBy(row => row.Title, textOrder)
        .ThenBy(row => row.Page.ModName, textOrder)
        .ThenBy(row => row.Page.PackageIdentity, StringComparer.Ordinal)
        .ThenBy(row => row.Page.ProviderPageIdentity, StringComparer.Ordinal)
        .ThenBy(row => row.InputOrdinal).ToArray();
}
