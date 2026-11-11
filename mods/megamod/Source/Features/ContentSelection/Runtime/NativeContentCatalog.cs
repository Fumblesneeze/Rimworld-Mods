using System;
using System.Collections.Generic;
using Megamod.Features.ContentSelection.Core;
using Verse;

namespace Megamod.Features.ContentSelection.Runtime;

public enum UnidentifiedContentReason { MissingDefinition, MissingSource, MissingType, MissingName }
public sealed record UnidentifiedContent(Def? Definition, UnidentifiedContentReason Reason);
public sealed record NativeContentCatalogResult(ContentCatalog Catalog, IReadOnlyList<UnidentifiedContent> Unidentified);

// Call only on the game thread after definitions finalize. This does not publish availability.
public static class NativeContentCatalog
{
    public static NativeContentCatalogResult Capture()
    {
        var entries = new List<ContentCatalogEntry>();
        var unidentified = new List<UnidentifiedContent>();
        foreach (Type databaseType in GenDefDatabase.AllDefTypesWithDatabases())
            foreach (Def definition in GenDefDatabase.GetAllDefsInDatabaseForDef(databaseType))
            {
                if (definition == null)
                {
                    unidentified.Add(new(null, UnidentifiedContentReason.MissingDefinition));
                    continue;
                }
                string? source = definition.modContentPack?.PackageIdPlayerFacing;
                string? fullTypeName = definition.GetType().FullName;
                UnidentifiedContentReason? reason = string.IsNullOrWhiteSpace(source)
                    ? UnidentifiedContentReason.MissingSource
                    : string.IsNullOrWhiteSpace(fullTypeName)
                        ? UnidentifiedContentReason.MissingType
                        : string.IsNullOrWhiteSpace(definition.defName)
                            ? UnidentifiedContentReason.MissingName : null;
                if (reason.HasValue)
                {
                    unidentified.Add(new(definition, reason.Value));
                    continue;
                }
                entries.Add(new(new ContentKey(source!, fullTypeName!, definition.defName), definition));
            }

        return new NativeContentCatalogResult(new ContentCatalog(entries), unidentified.AsReadOnly());
    }
}
