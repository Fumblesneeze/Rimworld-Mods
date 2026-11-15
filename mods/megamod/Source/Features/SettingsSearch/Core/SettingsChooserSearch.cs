using System;
using System.Collections.Generic;
using System.Linq;
using Megamod.Features.ModSettingsNavigation;

namespace Megamod.Features.SettingsSearch;

public sealed record SettingsChooserSearchResults(
    IReadOnlyList<SettingsChooserGroup> Pages, IReadOnlyList<SettingsSearchHit> Options);

// Both kinds share one detached query snapshot; page grouping precedes filtering.
public sealed class SettingsChooserSearch
{
    private readonly IReadOnlyList<SettingsChooserGroup> groups;
    private readonly SettingsSearch search;

    public SettingsChooserSearch(IEnumerable<SettingsChooserGroup> groups, IEnumerable<SettingsSearchEntry> options)
    {
        this.groups = Array.AsReadOnly(groups.ToArray());
        var rows = this.groups.SelectMany(group => group.Rows).ToArray();
        var owners = new HashSet<SettingsPage>(rows.Select(row => row.Page), ReferenceEqualityComparer.Instance);
        var capturedOptions = options.ToArray();
        if (capturedOptions.Any(option => option.IsPage || !owners.Contains(option.Page)))
            throw new ArgumentException("Options must retain exact page descriptors from this chooser snapshot.", nameof(options));
        var pages = rows.Select(row =>
            new SettingsSearchEntry(row.Page, "page", row.InputOrdinal, row.Title, IsPage: true,
                PageNormalizedAuthor: row.NormalizedAuthor, PageDisplayGroup: row.DisplayGroup, PageDisplayTitle: row.Title));
        search = new SettingsSearch(pages.Concat(capturedOptions));
    }

    public SettingsChooserSearchResults Find(string? query)
    {
        if (string.IsNullOrWhiteSpace(query)) return new(groups, Array.Empty<SettingsSearchHit>());
        var hits = search.Find(query);
        var pages = new HashSet<SettingsPage>(hits.Where(hit => hit.Entry.IsPage).Select(hit => hit.Entry.Page),
            ReferenceEqualityComparer.Instance);
        var filtered = groups.Select(group => new SettingsChooserGroup(group.Heading,
                group.Rows.Where(row => pages.Contains(row.Page)).ToArray()))
            .Where(group => group.Rows.Count != 0).ToArray();
        return new(Array.AsReadOnly(filtered), Array.AsReadOnly(hits.Where(hit => !hit.Entry.IsPage).ToArray()));
    }
}
