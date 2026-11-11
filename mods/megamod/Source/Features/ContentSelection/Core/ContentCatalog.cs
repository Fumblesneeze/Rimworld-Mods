using System;
using System.Collections.Generic;
using System.Linq;

namespace Megamod.Features.ContentSelection.Core;

public sealed record ContentCatalogEntry(ContentKey Key, object Identity);
public enum ContentResolutionStatus { Present, Missing, Ambiguous }
public sealed record ContentResolution(ContentResolutionStatus Status, object? Identity);

// One finalized catalog instance is an immutable identity snapshot, not availability policy.
public sealed class ContentCatalog
{
    public ContentCatalog(IEnumerable<ContentCatalogEntry> entries)
    {
        Entries = Array.AsReadOnly(entries.ToArray());
    }

    public IReadOnlyList<ContentCatalogEntry> Entries { get; }

    public ContentResolution Resolve(ContentKey key)
    {
        object? identity = null;
        foreach (var entry in Entries)
        {
            if (entry.Key != key) continue;
            if (identity != null && !ReferenceEquals(identity, entry.Identity))
                return new(ContentResolutionStatus.Ambiguous, null);
            identity = entry.Identity;
        }
        return identity == null
            ? new(ContentResolutionStatus.Missing, null)
            : new(ContentResolutionStatus.Present, identity);
    }
}
