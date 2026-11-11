using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Megamod.Settings;

namespace Megamod.Features.ContentSelection.Core;

public sealed class ContentBrowseSession
{
    private readonly SettingsStore store;
    private readonly ContentChoiceRow[] rows;
    private readonly ContentChoiceBrowser browser;
    private SettingsRevision? revision;
    private IReadOnlyDictionary<ContentKey, int>? ranks;
    public ContentBrowseSession(SettingsStore store, IEnumerable<ContentChoiceRow> rows)
    {
        this.store = store;
        this.rows = rows.ToArray();
        browser = new(store, this.rows);
    }
    public string Query { get; private set; } = "";
    public string? TypeName { get; private set; }
    public string? SourcePackageId { get; private set; }
    public void SetQuery(string query) { if (Query != query) { Query = query; ranks = null; } }
    public bool SelectType(string? identity)
    {
        if (identity != null && !rows.Any(row => row.Key.FullTypeName == identity)) return false;
        TypeName = identity; ranks = null; return true;
    }
    public bool SelectSource(string? identity)
    {
        if (identity != null && !rows.Any(row => row.Key.SourcePackageId == identity)) return false;
        SourcePackageId = identity; ranks = null; return true;
    }
    public IReadOnlyDictionary<ContentKey, int> VisibleRanks()
    {
        if (ranks != null && revision == store.Revision) return ranks;
        revision = store.Revision;
        ranks = new ReadOnlyDictionary<ContentKey, int>(browser.Select(Query, TypeName, SourcePackageId)
            .Select((row, index) => (row.Key, index)).ToDictionary(pair => pair.Key, pair => pair.index));
        return ranks;
    }
    public void Reset() { Query = ""; TypeName = null; SourcePackageId = null; ranks = null; }
}
