using System.Collections.Generic;
using System.Threading;
using Verse;

namespace RimWorldDevGateway;

public interface IGatewayVerseDefDatabase
{
    Type? ResolveDefType(string fullName);

    IEnumerable<Type> AllDatabaseTypes();

    Def? GetNamed(Type databaseType, string defName);

    IEnumerable<Def> AllDefs(Type databaseType);
}

public sealed class VerseGatewayDefDatabase : IGatewayVerseDefDatabase
{
    public Type? ResolveDefType(string fullName) =>
        GenTypes.GetTypeInAnyAssembly(fullName, null);

    public IEnumerable<Type> AllDatabaseTypes() =>
        GenDefDatabase.AllDefTypesWithDatabases();

    public Def? GetNamed(Type databaseType, string defName) =>
        GenDefDatabase.GetDefSilentFail(databaseType, defName, false);

    public IEnumerable<Def> AllDefs(Type databaseType) =>
        GenDefDatabase.GetAllDefsInDatabaseForDef(databaseType);
}

/// <summary>
/// Captures bounded pages from RimWorld's finalized Def databases. Callers must invoke this source
/// on the Unity main thread after play data has loaded.
/// </summary>
public sealed class VerseGatewayDefSource : IGatewayDefSource
{
    public const int MaximumDatabaseTypesScanned = 1024;
    public const int MaximumDefsScannedPerDatabase = 50_000;
    public const int MaximumDefsScannedPerRequest = 100_000;

    private readonly IGatewayVerseDefDatabase database;
    private readonly GatewayLogBuffer? diagnostics;
    private IReadOnlyList<Type>? cachedDatabaseTypes;

    public VerseGatewayDefSource(GatewayLogBuffer? diagnostics = null)
        : this(new VerseGatewayDefDatabase(), diagnostics)
    {
    }

    public VerseGatewayDefSource(
        IGatewayVerseDefDatabase database,
        GatewayLogBuffer? diagnostics = null)
    {
        this.database = database ?? throw new ArgumentNullException(nameof(database));
        this.diagnostics = diagnostics;
    }

    public GatewayDefSourcePage CapturePage(
        GatewayDefSourceQuery query,
        CancellationToken cancellationToken)
    {
        if (query is null)
        {
            throw new ArgumentNullException(nameof(query));
        }

        if (query.Limit < 1 || query.Limit > GatewayDefExporter.MaximumPageSize)
        {
            throw new GatewayDefExportException(
                "invalid_def_export_page_size",
                "The finalized Def source page size is outside the supported range.");
        }

        cancellationToken.ThrowIfCancellationRequested();
        var warnings = new List<GatewayDefExportWarning>();
        var types = ResolveDatabaseTypes(query, warnings, cancellationToken);
        return query.DefNames.Count > 0
            ? CaptureExactNames(query, types, warnings, cancellationToken)
            : CaptureScannedPage(query, types, warnings, cancellationToken);
    }

    private IReadOnlyList<Type> ResolveDatabaseTypes(
        GatewayDefSourceQuery query,
        ICollection<GatewayDefExportWarning> warnings,
        CancellationToken cancellationToken)
    {
        if (query.DefTypes.Count > 0)
        {
            var resolved = new Dictionary<string, Type>(StringComparer.Ordinal);
            foreach (var name in query.DefTypes.OrderBy(value => value, StringComparer.Ordinal))
            {
                cancellationToken.ThrowIfCancellationRequested();
                try
                {
                    var type = database.ResolveDefType(name);
                    if (!GatewayDefSafeText.IsRuntimeType(type) || type!.IsAbstract ||
                        !typeof(Def).IsAssignableFrom(type) ||
                        !string.Equals(TypeName(type), name, StringComparison.Ordinal))
                    {
                        AddWarning(
                            warnings,
                            new GatewayDefExportWarning(
                                name,
                                "def_type_not_found",
                                "The exact finalized Def database type is not loaded."));
                        continue;
                    }

                    resolved[name] = type;
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    AddWarning(
                        warnings,
                        SourceWarning(name, "def_type_resolution_failed", exception));
                }
            }

            return resolved.Values.OrderBy(TypeName, StringComparer.Ordinal).ToArray();
        }

        if (cachedDatabaseTypes is not null)
        {
            return cachedDatabaseTypes;
        }

        try
        {
            var types = new List<Type>();
            var candidatesScanned = 0;
            using var enumerator = database.AllDatabaseTypes().GetEnumerator();
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!enumerator.MoveNext())
                {
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();
                candidatesScanned++;
                if (candidatesScanned > MaximumDatabaseTypesScanned)
                {
                    throw Fail(
                        "def_database_type_scan_limit",
                        "Finalized Def database discovery exceeded its fixed type-scan limit.");
                }

                var type = enumerator.Current;
                if (GatewayDefSafeText.IsRuntimeType(type) && typeof(Def).IsAssignableFrom(type))
                {
                    types.Add(type);
                }
            }

            var resolved = types
                .GroupBy(TypeName, StringComparer.Ordinal)
                .Select(group => group.First())
                .OrderBy(TypeName, StringComparer.Ordinal)
                .ToArray();
            cachedDatabaseTypes = resolved;
            return resolved;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (GatewayDefExportException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw Fail(
                "def_database_discovery_failed",
                "Finalized Def database discovery failed: " + BoundedException(exception));
        }
    }

    private GatewayDefSourcePage CaptureExactNames(
        GatewayDefSourceQuery query,
        IReadOnlyList<Type> types,
        ICollection<GatewayDefExportWarning> warnings,
        CancellationToken cancellationToken)
    {
        var records = new List<GatewayDefRecord>(query.Limit + 1);
        var names = query.DefNames
            .Distinct(StringComparer.Ordinal)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToArray();
        foreach (var databaseType in types)
        {
            if (BeforeCursorDatabase(databaseType, query))
            {
                continue;
            }

            foreach (var defName in names)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!AfterCursor(TypeName(databaseType), defName, query))
                {
                    continue;
                }

                Def? def;
                try
                {
                    def = database.GetNamed(databaseType, defName);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    AddWarning(
                        warnings,
                        SourceWarning(TypeName(databaseType) + ":" + defName, "def_named_lookup_failed", exception));
                    continue;
                }

                if (def is null || !string.Equals(def.defName, defName, StringComparison.Ordinal))
                {
                    continue;
                }

                var record = CaptureRecord(databaseType, def, warnings, cancellationToken);
                if (record is null || !MatchesPackage(record, query.SourcePackageIds))
                {
                    continue;
                }

                records.Add(record);
                if (records.Count > query.Limit)
                {
                    return Page(records, query.Limit, truncated: true, warnings);
                }
            }
        }

        return Page(records, query.Limit, truncated: false, warnings);
    }

    private GatewayDefSourcePage CaptureScannedPage(
        GatewayDefSourceQuery query,
        IReadOnlyList<Type> types,
        ICollection<GatewayDefExportWarning> warnings,
        CancellationToken cancellationToken)
    {
        var records = new List<GatewayDefRecord>(query.Limit + 1);
        var candidatesScanned = 0;
        foreach (var databaseType in types)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (BeforeCursorDatabase(databaseType, query))
            {
                continue;
            }

            var fromDatabase = CaptureSmallestFromDatabase(
                databaseType,
                query,
                query.Limit + 1 - records.Count,
                warnings,
                cancellationToken,
                ref candidatesScanned);
            records.AddRange(fromDatabase);
            if (records.Count > query.Limit)
            {
                return Page(records, query.Limit, truncated: true, warnings);
            }
        }

        return Page(records, query.Limit, truncated: false, warnings);
    }

    private IReadOnlyList<GatewayDefRecord> CaptureSmallestFromDatabase(
        Type databaseType,
        GatewayDefSourceQuery query,
        int capacity,
        ICollection<GatewayDefExportWarning> warnings,
        CancellationToken cancellationToken,
        ref int candidatesScanned)
    {
        var selected = new SortedDictionary<string, GatewayDefRecord>(StringComparer.Ordinal);
        var scanned = 0;
        try
        {
            using var enumerator = database.AllDefs(databaseType).GetEnumerator();
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!enumerator.MoveNext())
                {
                    break;
                }

                cancellationToken.ThrowIfCancellationRequested();
                scanned++;
                if (scanned > MaximumDefsScannedPerDatabase)
                {
                    throw Fail(
                        "def_database_scan_limit",
                        "Finalized Def database '" + TypeName(databaseType) +
                        "' exceeded its fixed Def-scan limit.");
                }

                candidatesScanned++;
                if (candidatesScanned > MaximumDefsScannedPerRequest)
                {
                    throw Fail(
                        "def_source_scan_limit",
                        "Finalized Def source scanning exceeded its fixed aggregate per-request candidate limit.");
                }

                var def = enumerator.Current;
                if (def is null || string.IsNullOrEmpty(def.defName) ||
                    !AfterCursor(TypeName(databaseType), def.defName, query))
                {
                    continue;
                }

                var record = CaptureRecord(databaseType, def, warnings, cancellationToken);
                if (record is null || !MatchesPackage(record, query.SourcePackageIds))
                {
                    continue;
                }

                if (selected.ContainsKey(def.defName))
                {
                    AddWarning(
                        warnings,
                        new GatewayDefExportWarning(
                            TypeName(databaseType) + ":" + def.defName,
                            "duplicate_def_name",
                            "A duplicate finalized Def name was omitted from the diagnostic page."));
                    continue;
                }

                selected.Add(def.defName, record);
                if (selected.Count > capacity)
                {
                    selected.Remove(selected.Keys.Last());
                }
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (GatewayDefExportException)
        {
            throw;
        }
        catch (Exception exception)
        {
            AddWarning(
                warnings,
                SourceWarning(TypeName(databaseType), "def_database_enumeration_failed", exception));
            selected.Clear();
        }

        return selected.Values.ToArray();
    }

    private GatewayDefRecord? CaptureRecord(
        Type databaseType,
        Def def,
        ICollection<GatewayDefExportWarning> warnings,
        CancellationToken cancellationToken)
    {
        var databaseTypeName = TypeName(databaseType);
        if (!GatewayDefSafeText.IsRuntimeType(databaseType) ||
            !GatewayDefPagingIdentity.IsReversibleIdentity(databaseTypeName) ||
            !GatewayDefPagingIdentity.IsReversibleIdentity(def.defName))
        {
            AddWarning(
                warnings,
                new GatewayDefExportWarning(
                    databaseTypeName,
                    "def_identity_not_reversible",
                    "A finalized Def with a non-reversible paging identity was omitted."));
            return null;
        }

        try
        {
            var content = def.modContentPack;
            return new GatewayDefRecord(
                databaseType,
                def,
                CaptureProvenance(() => content?.PackageId, databaseType, def, warnings, cancellationToken),
                CaptureProvenance(() => content?.Name, databaseType, def, warnings, cancellationToken),
                CaptureProvenance(() => def.fileName, databaseType, def, warnings, cancellationToken));
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            AddWarning(
                warnings,
                SourceWarning(
                    TypeName(databaseType) + ":" + (def.defName ?? string.Empty),
                    "def_metadata_capture_failed",
                    exception));
            return null;
        }
    }

    private string? CaptureProvenance(
        Func<string?> capture,
        Type databaseType,
        Def def,
        ICollection<GatewayDefExportWarning> warnings,
        CancellationToken cancellationToken)
    {
        try
        {
            return capture();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            AddWarning(
                warnings,
                SourceWarning(
                    TypeName(databaseType) + ":" + (def.defName ?? string.Empty),
                    "def_provenance_capture_failed",
                    exception));
            return null;
        }
    }

    private static bool MatchesPackage(
        GatewayDefRecord record,
        IReadOnlyList<string> packageIds) =>
        packageIds.Count == 0 ||
        packageIds.Contains(record.SourcePackageId ?? string.Empty, StringComparer.Ordinal);

    private static bool BeforeCursorDatabase(Type databaseType, GatewayDefSourceQuery query) =>
        query.AfterDatabaseType is not null &&
        StringComparer.Ordinal.Compare(TypeName(databaseType), query.AfterDatabaseType) < 0;

    private static bool AfterCursor(
        string databaseType,
        string defName,
        GatewayDefSourceQuery query)
    {
        if (query.AfterDatabaseType is null)
        {
            return true;
        }

        var typeComparison = StringComparer.Ordinal.Compare(databaseType, query.AfterDatabaseType);
        return typeComparison > 0 ||
               typeComparison == 0 &&
               StringComparer.Ordinal.Compare(defName, query.AfterDefName ?? string.Empty) > 0;
    }

    private static GatewayDefSourcePage Page(
        IReadOnlyList<GatewayDefRecord> records,
        int limit,
        bool truncated,
        IEnumerable<GatewayDefExportWarning> warnings) =>
        new GatewayDefSourcePage(records.Take(limit), truncated, warnings);

    private void AddWarning(
        ICollection<GatewayDefExportWarning> warnings,
        GatewayDefExportWarning warning)
    {
        if (warnings.Count >= GatewayDefExporter.MaximumSourceWarnings)
        {
            return;
        }

        if (warnings.Count == GatewayDefExporter.MaximumSourceWarnings - 1 &&
            !string.Equals(warning.Code, "source_warning_limit", StringComparison.Ordinal))
        {
            warning = new GatewayDefExportWarning(
                "$source",
                "source_warning_limit",
                "Additional finalized Def source warnings were omitted.");
        }

        warnings.Add(warning);
        diagnostics?.Append(
            "Warning",
            warning.Code + ": " + warning.Message,
            requestId: GatewayRequestScope.CurrentRequestId);
    }

    private static GatewayDefExportWarning SourceWarning(
        string path,
        string code,
        Exception exception) =>
        new(path, code, BoundedException(exception));

    private GatewayDefExportException Fail(string code, string message)
    {
        diagnostics?.Append(
            "Error",
            code + ": " + message,
            requestId: GatewayRequestScope.CurrentRequestId);
        return new GatewayDefExportException(code, message);
    }

    private static string BoundedException(Exception exception)
    {
        var value = GatewayDefSafeText.ExceptionMarker(exception);
        return value.Length <= 1024 ? value : value.Substring(0, 1024);
    }

    private static string TypeName(Type type) => GatewayDefSafeText.TypeName(type);
}
