using System;
using System.Collections.Generic;
using System.Linq;
using Megamod.Settings;

namespace Megamod.Features.ContentSelection.Core;

// Transient browsing reads shared choices; it never changes or persists them.
public sealed class ContentChoiceBrowser
{
    private readonly SettingsStore store;
    private readonly IReadOnlyList<ContentChoiceRow> rows;
    public ContentChoiceBrowser(SettingsStore store, IEnumerable<ContentChoiceRow> rows)
    {
        this.store = store;
        this.rows = Array.AsReadOnly(rows.ToArray());
    }
    public IReadOnlyList<ContentChoiceRow> Select(string query, string? typeName = null, string? sourcePackageId = null)
        => Array.AsReadOnly(rows.Where(row => (typeName == null || row.Key.FullTypeName == typeName) &&
            (sourcePackageId == null || row.Key.SourcePackageId == sourcePackageId) &&
            new[] { row.Label, row.KindLabel, row.SourceLabel, row.Key.DefName, row.Key.FullTypeName, row.Key.SourcePackageId }
                .Any(field => field.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0))
            .OrderBy(row => store.ReadEffective(row.Definition).Value)
            .ThenBy(row => row.Label, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.SourceLabel, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Key.SourcePackageId, StringComparer.Ordinal)
            .ThenBy(row => row.Key.FullTypeName, StringComparer.Ordinal)
            .ThenBy(row => row.Key.DefName, StringComparer.Ordinal).ToArray());
}
