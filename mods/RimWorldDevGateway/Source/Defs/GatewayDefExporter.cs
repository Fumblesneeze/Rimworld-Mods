using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using Verse;

namespace RimWorldDevGateway;

public interface IGatewayDefSource
{
    GatewayDefSourcePage CapturePage(GatewayDefSourceQuery query, CancellationToken cancellationToken);
}

public sealed class GatewayDefSourceQuery
{
    public GatewayDefSourceQuery(
        IEnumerable<string> defTypes,
        IEnumerable<string> defNames,
        IEnumerable<string> sourcePackageIds,
        string? afterDatabaseType,
        string? afterDefName,
        int limit)
    {
        DefTypes = Copy(defTypes);
        DefNames = Copy(defNames);
        SourcePackageIds = Copy(sourcePackageIds);
        AfterDatabaseType = afterDatabaseType;
        AfterDefName = afterDefName;
        Limit = limit;
    }

    public IReadOnlyList<string> DefTypes { get; }

    public IReadOnlyList<string> DefNames { get; }

    public IReadOnlyList<string> SourcePackageIds { get; }

    public string? AfterDatabaseType { get; }

    public string? AfterDefName { get; }

    public int Limit { get; }

    private static IReadOnlyList<string> Copy(IEnumerable<string> values) =>
        new ReadOnlyCollection<string>(values.ToArray());
}

public sealed class GatewayDefSourcePage
{
    public GatewayDefSourcePage(
        IEnumerable<GatewayDefRecord> records,
        bool truncated,
        IEnumerable<GatewayDefExportWarning>? sourceWarnings = null)
    {
        Records = new ReadOnlyCollection<GatewayDefRecord>(
            (records ?? throw new ArgumentNullException(nameof(records))).ToArray());
        Truncated = truncated;
        SourceWarnings = BoundWarnings(sourceWarnings);
    }

    public IReadOnlyList<GatewayDefRecord> Records { get; }

    public bool Truncated { get; }

    public IReadOnlyList<GatewayDefExportWarning> SourceWarnings { get; }

    private static IReadOnlyList<GatewayDefExportWarning> BoundWarnings(
        IEnumerable<GatewayDefExportWarning>? warnings)
    {
        var bounded = (warnings ?? Array.Empty<GatewayDefExportWarning>())
            .Take(GatewayDefExporter.MaximumSourceWarnings + 1)
            .ToArray();
        if (bounded.Length <= GatewayDefExporter.MaximumSourceWarnings)
        {
            return new ReadOnlyCollection<GatewayDefExportWarning>(bounded);
        }

        var result = bounded.Take(GatewayDefExporter.MaximumSourceWarnings - 1).ToList();
        result.Add(new GatewayDefExportWarning(
            "$source",
            "source_warning_limit",
            "Additional finalized Def source warnings were omitted."));
        return new ReadOnlyCollection<GatewayDefExportWarning>(result);
    }
}

public sealed class GatewayDefRecord
{
    public GatewayDefRecord(
        Type databaseType,
        Def value,
        string? sourcePackageId,
        string? sourcePackageName,
        string? sourceFile)
    {
        DatabaseType = databaseType ?? throw new ArgumentNullException(nameof(databaseType));
        Value = value ?? throw new ArgumentNullException(nameof(value));
        SourcePackageId = sourcePackageId;
        SourcePackageName = sourcePackageName;
        SourceFile = sourceFile;
    }

    public Type DatabaseType { get; }

    public Def Value { get; }

    public string? SourcePackageId { get; }

    public string? SourcePackageName { get; }

    public string? SourceFile { get; }
}

public sealed class GatewayDefExportRequest
{
    public const int MaximumFilterValues = 64;
    public const int MaximumFilterValueLength = 512;
    public const int MaximumFieldNames = 64;
    public const int MaximumFieldNameLength = 512;
    public const int MaximumCursorLength = GatewayDefPagingIdentity.MaximumCursorLength;

    public GatewayDefExportRequest(
        string? format = null,
        IEnumerable<string>? defTypes = null,
        IEnumerable<string>? defNames = null,
        IEnumerable<string>? sourcePackageIds = null,
        string? cursor = null,
        int? pageSize = null,
        IEnumerable<string>? fieldNames = null)
    {
        Format = string.IsNullOrWhiteSpace(format) ? "json" : format!;
        if (Format.Length > 16)
        {
            throw new GatewayDefExportException(
                "invalid_def_export_format",
                "The finalized Def export format value is too long.");
        }

        DefTypes = Copy(defTypes, "defTypes");
        DefNames = Copy(defNames, "defNames");
        SourcePackageIds = Copy(sourcePackageIds, "sourcePackageIds");
        FieldNames = Copy(
            fieldNames,
            "fieldNames",
            MaximumFieldNames,
            MaximumFieldNameLength,
            "too_many_def_export_fields",
            "invalid_def_export_field");
        ValidateCursor(cursor);
        if (pageSize.HasValue && (pageSize.Value < 1 || pageSize.Value > GatewayDefExporter.MaximumPageSize))
        {
            throw new GatewayDefExportException(
                "invalid_def_export_page_size",
                "Finalized Def export pageSize must be between 1 and " +
                GatewayDefExporter.MaximumPageSize.ToString(CultureInfo.InvariantCulture) + ".");
        }

        Cursor = cursor;
        PageSize = pageSize;
    }

    public string Format { get; }

    public IReadOnlyList<string> DefTypes { get; }

    public IReadOnlyList<string> DefNames { get; }

    public IReadOnlyList<string> SourcePackageIds { get; }

    public IReadOnlyList<string> FieldNames { get; }

    public string? Cursor { get; }

    public int? PageSize { get; }

    private static IReadOnlyList<string> Copy(IEnumerable<string>? source, string field)
    {
        var values = Copy(
            source,
            field,
            MaximumFilterValues,
            MaximumFilterValueLength,
            "too_many_def_export_filters",
            "invalid_def_export_filter");
        if (values.Any(value => !GatewayDefPagingIdentity.IsReversibleIdentity(value)))
        {
            throw new GatewayDefExportException(
                "invalid_def_export_filter",
                field + " contains an exact filter value that is not reversibly encodable as UTF-8.");
        }

        return values;
    }

    private static IReadOnlyList<string> Copy(
        IEnumerable<string>? source,
        string field,
        int maximumValues,
        int maximumLength,
        string tooManyCode,
        string invalidCode)
    {
        var values = new List<string>();
        var distinct = new HashSet<string>(StringComparer.Ordinal);
        var examined = 0;
        foreach (var value in source ?? Array.Empty<string>())
        {
            examined++;
            if (examined > maximumValues)
            {
                throw new GatewayDefExportException(
                    tooManyCode,
                    field + " exceeds the fixed " + maximumValues.ToString(CultureInfo.InvariantCulture) +
                    "-value limit.");
            }

            if (string.IsNullOrEmpty(value) || value.Length > maximumLength)
            {
                throw new GatewayDefExportException(
                    invalidCode,
                    field + " contains an empty or overlong exact filter value.");
            }

            if (distinct.Add(value))
            {
                values.Add(value);
            }
        }

        return new ReadOnlyCollection<string>(values);
    }

    private static void ValidateCursor(string? cursor)
    {
        if (cursor is null)
        {
            return;
        }

        GatewayDefPagingIdentity.DecodeCursor(cursor);
    }

}

internal sealed class GatewayDefCursorValue
{
    public GatewayDefCursorValue(string databaseType, string defName)
    {
        DatabaseType = databaseType;
        DefName = defName;
    }

    public string DatabaseType { get; }

    public string DefName { get; }
}

internal static class GatewayDefPagingIdentity
{
    public const int MaximumIdentityCharacters = GatewayDefExportRequest.MaximumFilterValueLength;
    public const int MaximumIdentityUtf8Bytes = MaximumIdentityCharacters * 4;
    private const int MaximumBase64IdentityCharacters = ((MaximumIdentityUtf8Bytes + 2) / 3) * 4;
    public const int MaximumCursorLength = 4 + 2 * MaximumBase64IdentityCharacters;

    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static bool IsReversibleIdentity(string? value)
    {
        if (string.IsNullOrEmpty(value) || value!.Length > MaximumIdentityCharacters)
        {
            return false;
        }

        try
        {
            return StrictUtf8.GetByteCount(value) <= MaximumIdentityUtf8Bytes;
        }
        catch (EncoderFallbackException)
        {
            return false;
        }
    }

    public static string EncodeCursor(string databaseType, string defName)
    {
        if (!TryEncodeSegment(databaseType, out var typeSegment) ||
            !TryEncodeSegment(defName, out var nameSegment))
        {
            throw InvalidCursor();
        }

        return "v1." + typeSegment + "." + nameSegment;
    }

    public static GatewayDefCursorValue DecodeCursor(string encoded)
    {
        if (string.IsNullOrEmpty(encoded) || encoded.Length > MaximumCursorLength)
        {
            throw InvalidCursor();
        }

        var parts = encoded.Split('.');
        if (parts.Length != 3 || !string.Equals(parts[0], "v1", StringComparison.Ordinal))
        {
            throw InvalidCursor();
        }

        try
        {
            var databaseType = DecodeSegment(parts[1]);
            var defName = DecodeSegment(parts[2]);
            return new GatewayDefCursorValue(databaseType, defName);
        }
        catch (Exception exception) when (
            exception is FormatException ||
            exception is ArgumentException ||
            exception is DecoderFallbackException)
        {
            throw InvalidCursor();
        }
    }

    private static bool TryEncodeSegment(string value, out string encoded)
    {
        encoded = string.Empty;
        if (!IsReversibleIdentity(value))
        {
            return false;
        }

        encoded = Convert.ToBase64String(StrictUtf8.GetBytes(value));
        return true;
    }

    private static string DecodeSegment(string encoded)
    {
        if (encoded.Length == 0 || encoded.Length > MaximumBase64IdentityCharacters)
        {
            throw new FormatException();
        }

        var bytes = Convert.FromBase64String(encoded);
        if (bytes.Length == 0 || bytes.Length > MaximumIdentityUtf8Bytes ||
            !string.Equals(Convert.ToBase64String(bytes), encoded, StringComparison.Ordinal))
        {
            throw new FormatException();
        }

        var value = StrictUtf8.GetString(bytes);
        if (!IsReversibleIdentity(value))
        {
            throw new FormatException();
        }

        return value;
    }

    private static GatewayDefExportException InvalidCursor() =>
        new("invalid_def_export_cursor", "The finalized Def export cursor is invalid.");
}

public sealed class GatewayDefExportResult
{
    public GatewayDefExportResult(
        IReadOnlyList<GatewayDefExportItem> items,
        bool truncated,
        string? nextCursor,
        IReadOnlyList<GatewayDefExportWarning>? sourceWarnings = null)
    {
        Items = items;
        Truncated = truncated;
        NextCursor = nextCursor;
        SourceWarnings = sourceWarnings ?? Array.Empty<GatewayDefExportWarning>();
    }

    public string Serializer => "diagnostic-finalized-def-json-v1";

    public bool IsCanonicalSourceXml => false;

    public IReadOnlyList<GatewayDefExportItem> Items { get; }

    public bool Truncated { get; }

    public string? NextCursor { get; }

    public IReadOnlyList<GatewayDefExportWarning> SourceWarnings { get; }
}

public sealed class GatewayDefExportItem
{
    public GatewayDefExportItem(
        GatewayDefRecord record,
        IReadOnlyDictionary<string, object?> fields,
        IReadOnlyList<GatewayDefExportWarning> warnings)
    {
        DatabaseType = Bound(GatewayDefSafeText.TypeName(record.DatabaseType));
        RuntimeType = Bound(GatewayDefSafeText.TypeName(record.Value.GetType()));
        DefName = Bound(record.Value.defName ?? string.Empty);
        Label = BoundNullable(record.Value.label);
        SourcePackageId = BoundNullable(record.SourcePackageId);
        SourcePackageName = BoundNullable(record.SourcePackageName);
        SourceFile = BoundNullable(record.SourceFile);
        Fields = fields;
        Warnings = warnings;
    }

    public string DatabaseType { get; }

    public string RuntimeType { get; }

    public string DefName { get; }

    public string? Label { get; }

    public string? SourcePackageId { get; }

    public string? SourcePackageName { get; }

    public string? SourceFile { get; }

    public IReadOnlyDictionary<string, object?> Fields { get; }

    public IReadOnlyList<GatewayDefExportWarning> Warnings { get; }

    private static string Bound(string value) =>
        value.Length <= GatewayDefExporter.MaximumStringLength
            ? value
            : value.Substring(0, GatewayDefExporter.MaximumStringLength);

    private static string? BoundNullable(string? value) => value is null ? null : Bound(value);
}

public sealed class GatewayDefExportWarning
{
    public GatewayDefExportWarning(string path, string code, string message)
    {
        Path = Bound(path, 1024);
        Code = Bound(code, 128);
        Message = Bound(message, 1024);
    }

    public string Path { get; }

    public string Code { get; }

    public string Message { get; }

    private static string Bound(string value, int maximum) =>
        value.Length <= maximum ? value : value.Substring(0, maximum);
}

public sealed class GatewayDefReferenceValue
{
    public GatewayDefReferenceValue(string defType, string defName)
    {
        DefType = defType;
        DefName = defName;
    }

    public string Kind => "defReference";

    public string DefType { get; }

    public string DefName { get; }
}

public sealed class GatewayDefRepeatedReferenceValue
{
    public GatewayDefRepeatedReferenceValue(string firstPath)
    {
        FirstPath = firstPath;
    }

    public string Kind => "repeatedReference";

    public string FirstPath { get; }
}

public sealed class GatewayDefLimitValue
{
    public GatewayDefLimitValue(string limit)
    {
        Limit = limit;
    }

    public string Kind => "limit";

    public string Limit { get; }
}

public sealed class GatewayDefProjectionErrorValue
{
    public GatewayDefProjectionErrorValue(string code)
    {
        Code = code;
    }

    public string Kind => "projectionError";

    public string Code { get; }
}

public sealed class GatewayDefDictionaryEntry
{
    public GatewayDefDictionaryEntry(string keyType, string key, object? value, int sourceIndex)
    {
        KeyType = keyType;
        Key = key;
        Value = value;
        SourceIndex = sourceIndex;
    }

    public string KeyType { get; }

    public string Key { get; }

    public object? Value { get; }

    internal int SourceIndex { get; }
}

public sealed class GatewayDefDictionaryValue
{
    public GatewayDefDictionaryValue(IReadOnlyList<GatewayDefDictionaryEntry> entries, bool truncated)
    {
        Entries = entries;
        Truncated = truncated;
    }

    public string Kind => "dictionary";

    public IReadOnlyList<GatewayDefDictionaryEntry> Entries { get; }

    public bool Truncated { get; }
}

public sealed class GatewayDefExportException : Exception
{
    public GatewayDefExportException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public string Code { get; }
}

public sealed class GatewayDefExporter
{
    public const int DefaultPageSize = 16;
    public const int MaximumPageSize = 32;
    public const int MaximumProjectionDepth = 8;
    public const int MaximumFieldsPerObject = 512;
    public const int MaximumReflectedMembersExaminedPerObject = 4096;
    public const int MaximumReflectedTypeHierarchyDepth = 16;
    public const int MaximumCollectionItems = 1024;
    public const int MaximumStringLength = 64 * 1024;
    public const int MaximumProjectionPathLength = 4096;
    public const int MaximumProjectionNodesPerDef = 4096;
    public const int MaximumUtf8BytesPerProjectedField = 1024 * 1024;
    public const int MaximumUtf8BytesPerDef = 8 * 1024 * 1024;
    public const int MaximumWarningsPerDef = 8;
    public const int MaximumSourceWarnings = 32;
    public const int MaximumSerializedPageNodes = 500_000;
    public const int MaximumSerializedPageUtf8Bytes = 32 * 1024 * 1024;

    private readonly IGatewayDefSource source;
    private readonly Action<string>? projectionCheckpoint;

    public GatewayDefExporter(IGatewayDefSource source)
        : this(source, null)
    {
    }

    internal GatewayDefExporter(
        IGatewayDefSource source,
        Action<string>? projectionCheckpoint)
    {
        this.source = source ?? throw new ArgumentNullException(nameof(source));
        this.projectionCheckpoint = projectionCheckpoint;
    }

    public GatewayDefExportResult Export(
        GatewayDefExportRequest request,
        CancellationToken cancellationToken = default) =>
        Export(
            request,
            MaximumSerializedPageUtf8Bytes,
            MaximumSerializedPageNodes,
            cancellationToken);

    internal GatewayDefExportResult Export(
        GatewayDefExportRequest request,
        int maximumSerializedResultUtf8Bytes,
        CancellationToken cancellationToken = default)
        => Export(
            request,
            maximumSerializedResultUtf8Bytes,
            MaximumSerializedPageNodes,
            cancellationToken);

    internal GatewayDefExportResult Export(
        GatewayDefExportRequest request,
        int maximumSerializedResultUtf8Bytes,
        int maximumSerializedResultNodes,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (maximumSerializedResultUtf8Bytes <= 0 ||
            maximumSerializedResultUtf8Bytes > MaximumSerializedPageUtf8Bytes)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumSerializedResultUtf8Bytes));
        }

        if (maximumSerializedResultNodes <= 0 ||
            maximumSerializedResultNodes > MaximumSerializedPageNodes)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumSerializedResultNodes));
        }

        if (!string.Equals(request.Format, "json", StringComparison.OrdinalIgnoreCase))
        {
            throw new GatewayDefExportException(
                "unsupported_def_export_format",
                "Finalized Def export supports the bounded JSON diagnostic snapshot only; reconstructed XML is not canonical patched source XML.");
        }

        var cursor = DecodeCursor(request.Cursor);
        var typeFilter = Filter(request.DefTypes);
        var nameFilter = Filter(request.DefNames);
        var packageFilter = Filter(request.SourcePackageIds);
        var pageSize = Math.Min(Math.Max(request.PageSize ?? DefaultPageSize, 1), MaximumPageSize);
        cancellationToken.ThrowIfCancellationRequested();
        var sourcePage = source.CapturePage(
            new GatewayDefSourceQuery(
                request.DefTypes,
                request.DefNames,
                request.SourcePackageIds,
                cursor?.DatabaseType,
                cursor?.DefName,
                pageSize),
            cancellationToken) ?? throw new GatewayDefExportException(
                "def_source_failed",
                "The finalized Def source returned no page.");
        cancellationToken.ThrowIfCancellationRequested();
        var sourceWarnings = sourcePage.SourceWarnings.ToList();
        var matchingRecords = new List<IndexedRecord>();
        foreach (var record in sourcePage.Records)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!IndexedRecord.TryCreate(record, out var indexed))
            {
                AddSourceWarning(
                    sourceWarnings,
                    new GatewayDefExportWarning(
                        TypeName(record.DatabaseType),
                        "def_identity_not_reversible",
                        "A finalized Def with a non-reversible paging identity was omitted."));
                continue;
            }

            if (Matches(typeFilter, indexed.DatabaseType) &&
                Matches(nameFilter, indexed.DefName) &&
                Matches(packageFilter, record.SourcePackageId ?? string.Empty) &&
                (cursor is null || Compare(indexed.DatabaseType, indexed.DefName, cursor) > 0))
            {
                matchingRecords.Add(indexed);
            }
        }

        cancellationToken.ThrowIfCancellationRequested();
        var matches = matchingRecords
            .OrderBy(record => record.DatabaseType, StringComparer.Ordinal)
            .ThenBy(record => record.DefName, StringComparer.Ordinal)
            .ToArray();
        var page = matches.Take(pageSize).ToArray();
        var boundedSourceWarnings = BoundSourceWarnings(sourceWarnings);
        var projectedItems = new List<GatewayDefExportItem>(page.Length);
        long serializedAcceptedItemBytes = 0;
        long serializedAcceptedItemNodes = 0;
        foreach (var record in page)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var projected = Project(
                record.Record,
                request.FieldNames,
                cancellationToken,
                projectionCheckpoint);
            var serializedProjectedItem = MeasureSerializedItem(projected);
            cancellationToken.ThrowIfCancellationRequested();
            var candidateCursor = EncodeCursor(record.DatabaseType, record.DefName);
            var candidateFits = FitsSerializedCandidateResult(
                    boundedSourceWarnings,
                    projectedItems.Count + 1,
                    serializedAcceptedItemBytes + serializedProjectedItem.Utf8Bytes,
                    serializedAcceptedItemNodes + serializedProjectedItem.Nodes,
                    candidateCursor,
                    maximumSerializedResultUtf8Bytes,
                    maximumSerializedResultNodes);
            cancellationToken.ThrowIfCancellationRequested();
            if (!candidateFits)
            {
                if (projectedItems.Count == 0)
                {
                    throw new GatewayDefExportException(
                        "def_export_page_budget_too_small",
                        "The serialized finalized Def page budget cannot contain one bounded Def item.");
                }

                break;
            }

            projectedItems.Add(projected);
            serializedAcceptedItemBytes += serializedProjectedItem.Utf8Bytes;
            serializedAcceptedItemNodes += serializedProjectedItem.Nodes;
        }

        cancellationToken.ThrowIfCancellationRequested();
        var items = new ReadOnlyCollection<GatewayDefExportItem>(projectedItems);
        var truncated = sourcePage.Truncated ||
                        matches.Length > page.Length ||
                        projectedItems.Count < page.Length;
        var nextCursor = truncated && projectedItems.Count > 0
            ? EncodeCursor(
                page[projectedItems.Count - 1].DatabaseType,
                page[projectedItems.Count - 1].DefName)
            : null;
        var result = new GatewayDefExportResult(
            items,
            truncated,
            nextCursor,
            boundedSourceWarnings);
        if (!FitsSerializedResult(
                result,
                maximumSerializedResultUtf8Bytes,
                maximumSerializedResultNodes))
        {
            throw new GatewayDefExportException(
                "def_export_page_budget_failed",
                "The bounded finalized Def page exceeded its serialized result budget.");
        }

        return result;
    }

    private static GatewayJsonMeasurement MeasureSerializedItem(GatewayDefExportItem item) =>
        GatewayJsonWriter.Measure(
            item,
            maxDepth: MaximumProjectionDepth + 32,
            maxNodes: 100_000,
            maxUtf8Bytes: MaximumUtf8BytesPerDef);

    private static bool FitsSerializedCandidateResult(
        IReadOnlyList<GatewayDefExportWarning> sourceWarnings,
        int candidateItemCount,
        long serializedCandidateItemBytes,
        long serializedCandidateItemNodes,
        string candidateCursor,
        int maximumSerializedResultUtf8Bytes,
        int maximumSerializedResultNodes)
    {
        try
        {
            var emptyCandidateResult = new GatewayDefExportResult(
                Array.Empty<GatewayDefExportItem>(),
                truncated: true,
                candidateCursor,
                sourceWarnings);
            var emptyCandidateMeasurement = GatewayJsonWriter.Measure(
                emptyCandidateResult,
                maxDepth: 64,
                maxNodes: maximumSerializedResultNodes,
                maxUtf8Bytes: maximumSerializedResultUtf8Bytes);
            var separators = Math.Max(candidateItemCount - 1, 0);
            return emptyCandidateMeasurement.Utf8Bytes + serializedCandidateItemBytes + separators <=
                   maximumSerializedResultUtf8Bytes &&
                   emptyCandidateMeasurement.Nodes + serializedCandidateItemNodes <= maximumSerializedResultNodes;
        }
        catch (GatewayJsonSerializationException)
        {
            return false;
        }
    }

    private static bool FitsSerializedResult(
        GatewayDefExportResult result,
        int maximumSerializedResultUtf8Bytes,
        int maximumSerializedResultNodes)
    {
        try
        {
            GatewayJsonWriter.Write(
                result,
                maxDepth: 64,
                maxNodes: maximumSerializedResultNodes,
                maxUtf8Bytes: maximumSerializedResultUtf8Bytes);
            return true;
        }
        catch (GatewayJsonSerializationException)
        {
            return false;
        }
    }

    private static IReadOnlyList<GatewayDefExportWarning> BoundSourceWarnings(
        IEnumerable<GatewayDefExportWarning> warnings) =>
        new GatewayDefSourcePage(
            Array.Empty<GatewayDefRecord>(),
            truncated: false,
            warnings).SourceWarnings;

    private static void AddSourceWarning(
        ICollection<GatewayDefExportWarning> warnings,
        GatewayDefExportWarning warning)
    {
        if (warnings.Count >= MaximumSourceWarnings)
        {
            return;
        }

        if (warnings.Count == MaximumSourceWarnings - 1 && warning.Code != "source_warning_limit")
        {
            warning = new GatewayDefExportWarning(
                "$source",
                "source_warning_limit",
                "Additional finalized Def source warnings were omitted.");
        }

        warnings.Add(warning);
    }

    private static HashSet<string>? Filter(IReadOnlyList<string> values) =>
        values.Count == 0 ? null : new HashSet<string>(values, StringComparer.Ordinal);

    private static bool Matches(HashSet<string>? filter, string value) =>
        filter is null || filter.Contains(value);

    private static int Compare(string databaseType, string defName, GatewayDefCursorValue cursor)
    {
        var typeComparison = StringComparer.Ordinal.Compare(databaseType, cursor.DatabaseType);
        return typeComparison != 0
            ? typeComparison
            : StringComparer.Ordinal.Compare(defName, cursor.DefName);
    }

    private static string EncodeCursor(string databaseType, string defName) =>
        GatewayDefPagingIdentity.EncodeCursor(databaseType, defName);

    private static GatewayDefCursorValue? DecodeCursor(string? encoded)
    {
        if (string.IsNullOrEmpty(encoded))
        {
            return null;
        }

        return GatewayDefPagingIdentity.DecodeCursor(encoded!);
    }

    private static string TypeName(Type type) => GatewayDefSafeText.TypeName(type);

    private static GatewayDefExportItem Project(
        GatewayDefRecord record,
        IReadOnlyList<string> requestedFieldNames,
        CancellationToken cancellationToken,
        Action<string>? projectionCheckpoint)
    {
        var warnings = new List<GatewayDefExportWarning>();
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            WarnForTruncatedMetadata(record, warnings);
            var context = new ProjectionContext(warnings, cancellationToken, projectionCheckpoint);
            var fields = ProjectFields(
                record.Value,
                "$",
                0,
                context,
                requestedFieldNames.Count == 0
                    ? null
                    : new HashSet<string>(requestedFieldNames, StringComparer.Ordinal));
            context.ThrowIfCancellationRequested();
            var item = new GatewayDefExportItem(
                record,
                new ReadOnlyDictionary<string, object?>(fields),
                new ReadOnlyCollection<GatewayDefExportWarning>(warnings));
            try
            {
                GatewayJsonWriter.Write(
                    item,
                    maxDepth: MaximumProjectionDepth + 32,
                    maxNodes: 100_000,
                    maxUtf8Bytes: MaximumUtf8BytesPerDef);
                context.ThrowIfCancellationRequested();
                return item;
            }
            catch (GatewayJsonSerializationException)
            {
                AddWarning(warnings, new GatewayDefExportWarning(
                    "$",
                    "per_def_byte_limit",
                    "The projected Def exceeded the fixed per-Def UTF-8 byte limit."));
                return FitFieldPrefixToPerDefByteLimit(record, fields, warnings, context);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            AddWarning(warnings, new GatewayDefExportWarning(
                "$",
                "def_projection_failed",
                BoundedMessage(exception)));
            return new GatewayDefExportItem(
                record,
                new ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>
                {
                    ["$projection"] = new GatewayDefProjectionErrorValue("def_projection_failed")
                }),
                new ReadOnlyCollection<GatewayDefExportWarning>(warnings));
        }
    }

    private static Dictionary<string, object?> ProjectFields(
        object owner,
        string path,
        int depth,
        ProjectionContext context,
        HashSet<string>? requestedTopLevelFields = null)
    {
        var result = new Dictionary<string, object?>(StringComparer.Ordinal);
        var examined = 0;
        var fields = ProjectableFields(
            owner.GetType(),
            context,
            out var projectableFieldLimitReached,
            out var reflectedMemberWorkLimitReached);
        var foundRequested = requestedTopLevelFields is null
            ? null
            : new HashSet<string>(StringComparer.Ordinal);
        foreach (var field in fields)
        {
            context.ThrowIfCancellationRequested();
            if (requestedTopLevelFields is not null && !requestedTopLevelFields.Contains(field.Name))
            {
                continue;
            }

            foundRequested?.Add(field.Name);
            if (examined >= MaximumFieldsPerObject)
            {
                context.Warn(path, "field_limit", "An object exceeded the fixed projected-field limit.");
                result["$projectionLimit"] = new GatewayDefLimitValue("fields");
                break;
            }

            if (!context.HasNodeCapacity)
            {
                context.Warn(path, "node_limit", "The object graph exceeded the fixed per-Def node limit.");
                result["$projectionLimit"] = new GatewayDefLimitValue("nodes");
                break;
            }

            examined++;
            var projectedFieldName = ProjectText(
                field.Name,
                AppendPath(path, ".$fieldName", context),
                context);
            var fieldPath = AppendPath(path, "." + projectedFieldName, context);
            if (result.ContainsKey(projectedFieldName))
            {
                context.Warn(
                    fieldPath,
                    "field_name_collision",
                    "A reflected field name collided after bounded projection and was omitted.");
                continue;
            }

            try
            {
                var projectedValue = ProjectValue(field.GetValue(owner), fieldPath, depth + 1, context);
                result[projectedFieldName] = BoundProjectedFieldBytes(
                    projectedFieldName,
                    projectedValue,
                    fieldPath,
                    context);
            }
            catch (OperationCanceledException) when (context.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                context.Warn(fieldPath, "field_read_failed", exception);
                result[projectedFieldName] = new GatewayDefProjectionErrorValue("field_read_failed");
            }
        }

        if (projectableFieldLimitReached && !result.ContainsKey("$projectionLimit"))
        {
            context.Warn(path, "field_limit", "Reflected field discovery exceeded its fixed work limit.");
            result["$projectionLimit"] = new GatewayDefLimitValue("fields");
        }

        else if (reflectedMemberWorkLimitReached && !result.ContainsKey("$projectionLimit"))
        {
            context.Warn(
                path,
                "reflected_member_work_limit",
                "Reflected member discovery exceeded its fixed total work limit before exclusions.");
            result["$projectionLimit"] = new GatewayDefLimitValue("reflectedMembers");
        }

        if (requestedTopLevelFields is not null)
        {
            foreach (var missing in requestedTopLevelFields
                         .Where(name => foundRequested is null || !foundRequested.Contains(name))
                         .OrderBy(name => name, StringComparer.Ordinal))
            {
                context.Warn(
                    AppendPath(path, "." + missing, context),
                    "requested_field_not_found",
                    "The requested exact top-level field is not projectable on this finalized Def.");
            }
        }

        return result;
    }

    private static object? BoundProjectedFieldBytes(
        string projectedFieldName,
        object? projectedValue,
        string path,
        ProjectionContext context)
    {
        context.ThrowIfCancellationRequested();
        try
        {
            GatewayJsonWriter.Write(
                new SortedDictionary<string, object?>(StringComparer.Ordinal)
                {
                    [projectedFieldName] = projectedValue
                },
                maxDepth: MaximumProjectionDepth + 32,
                maxNodes: 100_000,
                maxUtf8Bytes: MaximumUtf8BytesPerProjectedField);
            context.ThrowIfCancellationRequested();
            return projectedValue;
        }
        catch (GatewayJsonSerializationException)
        {
            context.Warn(
                path,
                "field_byte_limit",
                "A projected top-level field exceeded its fixed UTF-8 byte limit.");
            return new GatewayDefLimitValue("fieldUtf8Bytes");
        }
    }

    private static GatewayDefExportItem FitFieldPrefixToPerDefByteLimit(
        GatewayDefRecord record,
        IReadOnlyDictionary<string, object?> fields,
        List<GatewayDefExportWarning> warnings,
        ProjectionContext context)
    {
        var ordered = fields
            .OrderBy(entry => entry.Key, StringComparer.Ordinal)
            .ToArray();
        var lower = 0;
        var upper = ordered.Length;
        while (lower < upper)
        {
            context.ThrowIfCancellationRequested();
            var candidateCount = lower + (upper - lower + 1) / 2;
            var candidate = CreateByteLimitedItem(record, ordered, candidateCount, warnings);
            if (FitsPerDefByteLimit(candidate))
            {
                lower = candidateCount;
            }
            else
            {
                upper = candidateCount - 1;
            }
        }

        context.ThrowIfCancellationRequested();
        return CreateByteLimitedItem(record, ordered, lower, warnings);
    }

    private static GatewayDefExportItem CreateByteLimitedItem(
        GatewayDefRecord record,
        IReadOnlyList<KeyValuePair<string, object?>> ordered,
        int retainedCount,
        IReadOnlyList<GatewayDefExportWarning> warnings)
    {
        var retained = new SortedDictionary<string, object?>(StringComparer.Ordinal)
        {
            ["$byteLimit"] = new GatewayDefLimitValue("perDefUtf8Bytes")
        };
        for (var index = 0; index < retainedCount; index++)
        {
            retained[ordered[index].Key] = ordered[index].Value;
        }

        return new GatewayDefExportItem(
            record,
            new ReadOnlyDictionary<string, object?>(retained),
            new ReadOnlyCollection<GatewayDefExportWarning>(warnings.ToArray()));
    }

    private static bool FitsPerDefByteLimit(GatewayDefExportItem item)
    {
        try
        {
            GatewayJsonWriter.Write(
                item,
                maxDepth: MaximumProjectionDepth + 32,
                maxNodes: 100_000,
                maxUtf8Bytes: MaximumUtf8BytesPerDef);
            return true;
        }
        catch (GatewayJsonSerializationException)
        {
            return false;
        }
    }

    private static object? ProjectValue(
        object? value,
        string path,
        int depth,
        ProjectionContext context)
    {
        context.ReachProjectionPath(path);
        if (!context.TryCountNode(path))
        {
            return new GatewayDefLimitValue("nodes");
        }

        if (value is null)
        {
            return null;
        }

        if (value is string text)
        {
            return ProjectText(text, path, context);
        }

        var type = value.GetType();
        if (value is float single && (float.IsNaN(single) || float.IsInfinity(single)) ||
            value is double floating && (double.IsNaN(floating) || double.IsInfinity(floating)))
        {
            context.Warn(path, "non_finite_number", "A non-finite number cannot be represented in JSON.");
            return new GatewayDefProjectionErrorValue("non_finite_number");
        }

        if (value is TimeSpan timeSpan)
        {
            context.ThrowIfCancellationRequested();
            return timeSpan.ToString("c", CultureInfo.InvariantCulture);
        }

        if (IsScalar(type))
        {
            if (value is Enum enumValue)
            {
                var enumText = enumValue.ToString();
                context.ThrowIfCancellationRequested();
                return ProjectText(enumText, path, context);
            }

            return value;
        }

        if (value is Type runtimeType)
        {
            var typeName = TypeName(runtimeType);
            context.ThrowIfCancellationRequested();
            return ProjectText(typeName, path, context);
        }

        if (value is Def nestedDef)
        {
            context.ThrowIfCancellationRequested();
            return new GatewayDefReferenceValue(
                ProjectText(
                    TypeName(nestedDef.GetType()),
                    AppendPath(path, ".defType", context),
                    context),
                ProjectText(
                    nestedDef.defName ?? string.Empty,
                    AppendPath(path, ".defName", context),
                    context));
        }

        if (depth > MaximumProjectionDepth)
        {
            context.Warn(path, "depth_limit", "The object graph exceeded the fixed projection depth.");
            return new GatewayDefLimitValue("depth");
        }

        if (!type.IsValueType && !context.TryVisit(value, path, out var firstPath))
        {
            return new GatewayDefRepeatedReferenceValue(ProjectText(firstPath!, path, context));
        }

        if (value is IDictionary dictionary)
        {
            if (!IsSafeDictionaryType(type))
            {
                context.Warn(
                    path,
                    "unsupported_collection_type",
                    "Custom dictionary iterators are not executed by finalized Def export.");
                return new GatewayDefProjectionErrorValue("unsupported_collection_type");
            }

            return ProjectDictionary(dictionary, path, depth, context);
        }

        if (value is IEnumerable sequence)
        {
            if (!IsSafeSequenceType(type))
            {
                context.Warn(
                    path,
                    "unsupported_collection_type",
                    "Custom collection iterators are not executed by finalized Def export.");
                return new GatewayDefProjectionErrorValue("unsupported_collection_type");
            }

            return ProjectSequence(sequence, path, depth, context);
        }

        return new ReadOnlyDictionary<string, object?>(ProjectFields(value, path, depth, context));
    }

    private static object ProjectSequence(
        IEnumerable sequence,
        string path,
        int depth,
        ProjectionContext context)
    {
        var items = new List<object?>();
        IEnumerator? enumerator = null;
        try
        {
            context.ThrowIfCancellationRequested();
            enumerator = sequence.GetEnumerator();
            while (items.Count < MaximumCollectionItems)
            {
                context.ThrowIfCancellationRequested();
                if (!enumerator.MoveNext())
                {
                    break;
                }

                context.ThrowIfCancellationRequested();
                var elementPath = AppendPath(
                    path,
                    "[" + items.Count.ToString(CultureInfo.InvariantCulture) + "]",
                    context);
                try
                {
                    items.Add(ProjectValue(enumerator.Current, elementPath, depth + 1, context));
                }
                catch (OperationCanceledException) when (context.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    context.Warn(elementPath, "collection_element_failed", exception);
                    items.Add(new GatewayDefProjectionErrorValue("collection_element_failed"));
                }
            }

            context.ThrowIfCancellationRequested();
            if (items.Count == MaximumCollectionItems && enumerator.MoveNext())
            {
                context.Warn(path, "collection_truncated", "A collection exceeded the fixed item limit.");
                items.Add(new GatewayDefLimitValue("collectionItems"));
            }
        }
        catch (OperationCanceledException) when (context.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            context.Warn(path, "collection_read_failed", exception);
            items.Add(new GatewayDefProjectionErrorValue("collection_read_failed"));
        }
        finally
        {
            if (enumerator is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (OperationCanceledException) when (context.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    context.Warn(path, "collection_dispose_failed", exception);
                }
            }
        }

        context.ThrowIfCancellationRequested();
        return new ReadOnlyCollection<object?>(items);
    }

    private static object ProjectDictionary(
        IDictionary dictionary,
        string path,
        int depth,
        ProjectionContext context)
    {
        var entries = new List<GatewayDefDictionaryEntry>();
        IDictionaryEnumerator? enumerator = null;
        var examined = 0;
        var truncated = false;
        try
        {
            context.ThrowIfCancellationRequested();
            enumerator = dictionary.GetEnumerator();
            while (examined < MaximumCollectionItems)
            {
                context.ThrowIfCancellationRequested();
                if (!enumerator.MoveNext())
                {
                    break;
                }

                context.ThrowIfCancellationRequested();
                var sourceIndex = examined++;
                string key;
                string keyType;
                try
                {
                    var rawKey = enumerator.Key;
                    keyType = ProjectText(
                        TypeName(rawKey?.GetType() ?? typeof(object)),
                        AppendPath(path, ".keyType", context),
                        context);
                    key = ScalarDictionaryKey(
                        rawKey,
                        AppendPath(path, ".key", context),
                        context);
                }
                catch (OperationCanceledException) when (context.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    context.Warn(path, "dictionary_key_failed", exception);
                    keyType = "<unsupported>";
                    key = "$invalid-" + sourceIndex.ToString(CultureInfo.InvariantCulture);
                }

                var elementPath = AppendPath(path, "[" + key + "]", context);
                object? projected;
                try
                {
                    projected = ProjectValue(enumerator.Value, elementPath, depth + 1, context);
                }
                catch (OperationCanceledException) when (context.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    context.Warn(elementPath, "collection_element_failed", exception);
                    projected = new GatewayDefProjectionErrorValue("collection_element_failed");
                }

                entries.Add(new GatewayDefDictionaryEntry(keyType, key, projected, sourceIndex));
            }

            context.ThrowIfCancellationRequested();
            if (examined == MaximumCollectionItems && enumerator.MoveNext())
            {
                context.Warn(path, "collection_truncated", "A dictionary exceeded the fixed item limit.");
                truncated = true;
            }
        }
        catch (OperationCanceledException) when (context.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            context.Warn(path, "collection_read_failed", exception);
        }
        finally
        {
            if (enumerator is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (OperationCanceledException) when (context.IsCancellationRequested)
                {
                    throw;
                }
                catch (Exception exception)
                {
                    context.Warn(path, "collection_dispose_failed", exception);
                }
            }
        }

        context.ThrowIfCancellationRequested();
        var ordered = entries
            .OrderBy(entry => entry.KeyType, StringComparer.Ordinal)
            .ThenBy(entry => entry.Key, StringComparer.Ordinal)
            .ThenBy(entry => entry.SourceIndex)
            .ToArray();
        return new GatewayDefDictionaryValue(
            new ReadOnlyCollection<GatewayDefDictionaryEntry>(ordered),
            truncated);
    }

    private static string ScalarDictionaryKey(object? key, string path, ProjectionContext context)
    {
        if (key is null)
        {
            return "null";
        }

        var type = key.GetType();
        if (key is string text)
        {
            return ProjectText(text, path, context);
        }

        if (!IsScalar(type))
        {
            throw new InvalidOperationException("Only scalar dictionary keys are projected.");
        }

        var formatted = key is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture) ?? string.Empty
            : key.ToString() ?? string.Empty;
        return ProjectText(formatted, path, context);
    }

    private static IReadOnlyList<FieldInfo> ProjectableFields(
        Type type,
        ProjectionContext context,
        out bool projectableFieldLimitReached,
        out bool reflectedMemberWorkLimitReached)
    {
        var fields = new Dictionary<string, FieldInfo>(StringComparer.Ordinal);
        var reflectedMembersExamined = 0;
        var hierarchyDepth = 0;
        projectableFieldLimitReached = false;
        reflectedMemberWorkLimitReached = false;
        for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            context.ThrowIfCancellationRequested();
            hierarchyDepth++;
            if (hierarchyDepth > MaximumReflectedTypeHierarchyDepth)
            {
                reflectedMemberWorkLimitReached = true;
                break;
            }

            foreach (var field in current.GetFields(
                         BindingFlags.Instance |
                         BindingFlags.Static |
                         BindingFlags.Public |
                         BindingFlags.NonPublic |
                         BindingFlags.DeclaredOnly))
            {
                context.ThrowIfCancellationRequested();
                reflectedMembersExamined++;
                if (reflectedMembersExamined > MaximumReflectedMembersExaminedPerObject)
                {
                    reflectedMemberWorkLimitReached = true;
                    break;
                }

                if (field.IsStatic || HasUnsavedAttribute(field) || fields.ContainsKey(field.Name))
                {
                    continue;
                }

                if (fields.Count >= MaximumFieldsPerObject)
                {
                    projectableFieldLimitReached = true;
                    break;
                }

                fields.Add(field.Name, field);
            }

            if (projectableFieldLimitReached || reflectedMemberWorkLimitReached)
            {
                break;
            }
        }

        context.ThrowIfCancellationRequested();
        return fields.Values.OrderBy(field => field.Name, StringComparer.Ordinal).ToArray();
    }

    private static bool HasUnsavedAttribute(FieldInfo field)
        => GatewayDefSafeText.HasAttribute(field, "Verse.UnsavedAttribute");

    private static bool IsScalar(Type type) =>
        type.IsPrimitive ||
        type.IsEnum ||
        type == typeof(decimal) ||
        type == typeof(Guid) ||
        type == typeof(DateTime) ||
        type == typeof(DateTimeOffset) ||
        type == typeof(TimeSpan) ||
        type == typeof(IntPtr) ||
        type == typeof(UIntPtr);

    private static string ProjectText(string value, string path, ProjectionContext context)
    {
        context.ThrowIfCancellationRequested();
        if (value.Length <= MaximumStringLength)
        {
            return value;
        }

        context.Warn(path, "string_truncated", "A string exceeded the fixed character limit.");
        return value.Substring(0, MaximumStringLength);
    }

    private static string AppendPath(string prefix, string suffix, ProjectionContext context)
    {
        if (prefix.Length <= MaximumProjectionPathLength - suffix.Length)
        {
            return prefix + suffix;
        }

        context.Warn(prefix, "path_truncated", "A projection path exceeded the fixed character limit.");
        if (prefix.Length >= MaximumProjectionPathLength)
        {
            return prefix.Substring(0, MaximumProjectionPathLength);
        }

        return prefix + suffix.Substring(0, MaximumProjectionPathLength - prefix.Length);
    }

    private static bool IsSafeSequenceType(Type type)
    {
        if (type.IsArray || type == typeof(ArrayList))
        {
            return true;
        }

        if (!type.IsGenericType)
        {
            return false;
        }

        var definition = type.GetGenericTypeDefinition();
        return definition == typeof(List<>);
    }

    private static bool IsSafeDictionaryType(Type type)
    {
        if (type == typeof(Hashtable))
        {
            return true;
        }

        if (!type.IsGenericType)
        {
            return false;
        }

        var definition = type.GetGenericTypeDefinition();
        return definition == typeof(Dictionary<,>) ||
               definition == typeof(SortedDictionary<,>);
    }

    private static string BoundedMessage(Exception exception)
    {
        var message = GatewayDefSafeText.ExceptionMarker(exception);
        return message.Length <= 1024 ? message : message.Substring(0, 1024);
    }

    private static void WarnForTruncatedMetadata(
        GatewayDefRecord record,
        ICollection<GatewayDefExportWarning> warnings)
    {
        WarnIfLong("$.databaseType", TypeName(record.DatabaseType));
        WarnIfLong("$.runtimeType", TypeName(record.Value.GetType()));
        WarnIfLong("$.defName", record.Value.defName);
        WarnIfLong("$.label", record.Value.label);
        WarnIfLong("$.sourcePackageId", record.SourcePackageId);
        WarnIfLong("$.sourcePackageName", record.SourcePackageName);
        WarnIfLong("$.sourceFile", record.SourceFile);

        void WarnIfLong(string path, string? value)
        {
            if (value is not null && value.Length > MaximumStringLength)
            {
                AddWarning(warnings, new GatewayDefExportWarning(
                    path,
                    "string_truncated",
                    "A metadata string exceeded the fixed character limit."));
            }
        }
    }

    private static void AddWarning(
        ICollection<GatewayDefExportWarning> warnings,
        GatewayDefExportWarning warning)
    {
        if (warnings.Count >= MaximumWarningsPerDef)
        {
            return;
        }

        if (warnings.Count == MaximumWarningsPerDef - 1 && warning.Code != "warning_limit")
        {
            warnings.Add(new GatewayDefExportWarning(
                "$",
                "warning_limit",
                "Additional projection warnings were omitted after the fixed per-Def warning limit."));
            return;
        }

        warnings.Add(warning);
    }

    private sealed class IndexedRecord
    {
        private IndexedRecord(GatewayDefRecord record, string databaseType, string defName)
        {
            Record = record;
            DatabaseType = databaseType;
            DefName = defName;
        }

        public static bool TryCreate(GatewayDefRecord record, out IndexedRecord indexed)
        {
            var databaseType = TypeName(record.DatabaseType);
            var defName = record.Value.defName ?? string.Empty;
            if (!GatewayDefSafeText.IsRuntimeType(record.DatabaseType) ||
                !GatewayDefPagingIdentity.IsReversibleIdentity(databaseType) ||
                !GatewayDefPagingIdentity.IsReversibleIdentity(defName))
            {
                indexed = null!;
                return false;
            }

            indexed = new IndexedRecord(record, databaseType, defName);
            return true;
        }

        public GatewayDefRecord Record { get; }

        public string DatabaseType { get; }

        public string DefName { get; }
    }

    private sealed class ProjectionContext
    {
        private readonly ICollection<GatewayDefExportWarning> warnings;
        private readonly CancellationToken cancellationToken;
        private readonly Action<string>? projectionCheckpoint;
        private readonly Dictionary<object, string> visited = new Dictionary<object, string>(ReferenceComparer.Instance);
        private int nodes;
        private bool nodeLimitWarned;

        public ProjectionContext(
            ICollection<GatewayDefExportWarning> warnings,
            CancellationToken cancellationToken,
            Action<string>? projectionCheckpoint)
        {
            this.warnings = warnings;
            this.cancellationToken = cancellationToken;
            this.projectionCheckpoint = projectionCheckpoint;
        }

        public bool HasNodeCapacity => nodes < MaximumProjectionNodesPerDef;

        public bool IsCancellationRequested => cancellationToken.IsCancellationRequested;

        public void ThrowIfCancellationRequested() =>
            cancellationToken.ThrowIfCancellationRequested();

        public void ReachProjectionPath(string path)
        {
            projectionCheckpoint?.Invoke(path);
            ThrowIfCancellationRequested();
        }

        public bool TryCountNode(string path)
        {
            nodes++;
            if (nodes <= MaximumProjectionNodesPerDef)
            {
                return true;
            }

            if (!nodeLimitWarned)
            {
                nodeLimitWarned = true;
                Warn(path, "node_limit", "The object graph exceeded the fixed per-Def node limit.");
            }

            return false;
        }

        public bool TryVisit(object value, string path, out string? firstPath)
        {
            if (visited.TryGetValue(value, out firstPath))
            {
                return false;
            }

            visited.Add(value, path);
            return true;
        }

        public void Warn(string path, string code, Exception exception) =>
            Warn(path, code, BoundedMessage(exception));

        public void Warn(string path, string code, string message) =>
            AddWarning(warnings, new GatewayDefExportWarning(path, code, message));
    }

    private sealed class ReferenceComparer : IEqualityComparer<object>
    {
        public static readonly ReferenceComparer Instance = new ReferenceComparer();

        bool IEqualityComparer<object>.Equals(object? left, object? right) => ReferenceEquals(left, right);

        int IEqualityComparer<object>.GetHashCode(object value) => RuntimeHelpers.GetHashCode(value);
    }
}
