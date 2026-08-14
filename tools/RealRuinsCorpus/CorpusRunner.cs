using System.Collections.Concurrent;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;

namespace RealRuinsCorpus;

public sealed record CorpusRunOptions(
    int MetadataLimit,
    int BlueprintLimit,
    int Concurrency,
    string OutputDirectory,
    string OutputFormat);

public sealed record CorpusRunResult(
    int MetadataCount,
    int RequestedBlueprintCount,
    int AttemptedBlueprintCount,
    int ParsedCount,
    int RejectedCount,
    long CompressedBytes,
    string MetadataSha256,
    string SummaryPath);

public sealed record CorpusSummary(
    string Schema,
    DateTimeOffset CompletedAtUtc,
    int MetadataCount,
    int RequestedBlueprintCount,
    int AttemptedBlueprintCount,
    int ParsedCount,
    int RejectedCount,
    long CompressedBytes,
    string MetadataSha256,
    PlacementMetric DiningTablesWithEnoughAdjacentChairs,
    PlacementMetric PowerConduitsUnderWalls,
    PlacementMetric PowerConduitsAtOrAdjacentToWalls,
    PlacementMetric WorkstationsTouchingWalls,
    PlacementMetric FixedOutdoorUtilitiesRoofed,
    IReadOnlyDictionary<string, ChairDistribution> DiningTableChairDistribution,
    IReadOnlyDictionary<string, UtilityDistribution> OutdoorUtilityDistribution,
    IReadOnlyDictionary<string, int> ItemDefCounts,
    IReadOnlyList<CorpusBlueprintRecord> Blueprints);

public sealed record ChairDistribution(int Tables, double MeanAdjacentChairs, int Minimum, int Maximum);

public sealed record UtilityDistribution(int Total, int Roofed);

public sealed record CorpusBlueprintRecord(
    string Name,
    string Status,
    long CompressedBytes,
    string? Sha256,
    string? Error,
    BlueprintAnalysis? Analysis);

internal sealed record RealRuinsMetadata(string NameInBucket);

public static class CorpusRunner
{
    private const int MaximumMetadata = 10_000;
    private const int MaximumBlueprints = 5_000;
    private const int MaximumConcurrency = 24;
    private const long MaximumCompressedBlueprintBytes = 8L * 1024 * 1024;
    private const long MaximumCompressedRunBytes = 4L * 1024 * 1024 * 1024;
    private static readonly Uri ApiRoot = new("https://woolstrand.art/");
    private static readonly Uri BucketRoot = new("https://realruinsv2.sfo2.digitaloceanspaces.com/");
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true
    };

    public static async Task<CorpusRunResult> RunAsync(CorpusRunOptions options, CancellationToken cancellationToken)
    {
        Validate(options);
        var outputRoot = Path.GetFullPath(options.OutputDirectory);
        Directory.CreateDirectory(outputRoot);
        var blueprintRoot = Path.Combine(outputRoot, "blueprints");
        Directory.CreateDirectory(blueprintRoot);
        var metadataPath = Path.Combine(outputRoot, "metadata.json");

        using var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            MaxConnectionsPerServer = options.Concurrency,
            AutomaticDecompression = DecompressionMethods.None,
            ConnectTimeout = TimeSpan.FromSeconds(15),
            PooledConnectionLifetime = TimeSpan.FromMinutes(2)
        };
        using var client = new HttpClient(handler) { Timeout = TimeSpan.FromSeconds(30) };
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("ImmersiveChefs-RealRuinsResearch", "1.0"));

        byte[] metadataBytes;
        if (File.Exists(metadataPath))
        {
            metadataBytes = await File.ReadAllBytesAsync(metadataPath, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            var metadataUri = new Uri(ApiRoot, $"maps/random?limit={options.MetadataLimit}");
            using var response = await client.GetAsync(metadataUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            if (response.RequestMessage?.RequestUri?.Scheme != Uri.UriSchemeHttps ||
                !string.Equals(response.RequestMessage.RequestUri.Host, ApiRoot.Host, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException("Real Ruins metadata request left its exact HTTPS host.");
            }

            metadataBytes = await ReadBoundedAsync(
                await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
                16L * 1024 * 1024,
                cancellationToken).ConfigureAwait(false);
            await WriteAtomicAsync(metadataPath, metadataBytes, cancellationToken).ConfigureAwait(false);
        }

        var metadataSha = Convert.ToHexString(SHA256.HashData(metadataBytes));
        var metadata = JsonSerializer.Deserialize<RealRuinsMetadata[]>(metadataBytes, JsonOptions)
            ?? throw new InvalidDataException("Real Ruins metadata response was empty.");
        if (metadata.Length < options.BlueprintLimit || metadata.Length > options.MetadataLimit)
        {
            throw new InvalidDataException(
                $"Real Ruins returned {metadata.Length} metadata rows; expected {options.BlueprintLimit}..{options.MetadataLimit}.");
        }

        var attemptCount = Math.Min(
            metadata.Length,
            checked(options.BlueprintLimit + Math.Max(100, options.BlueprintLimit / 10)));
        var selected = metadata
            .OrderBy(item => item.NameInBucket, StringComparer.OrdinalIgnoreCase)
            .Take(attemptCount)
            .ToArray();
        var duplicate = selected.GroupBy(item => item.NameInBucket, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicate is not null)
        {
            throw new InvalidDataException($"Real Ruins metadata contains duplicate object '{duplicate.Key}'.");
        }

        var records = new ConcurrentBag<CorpusBlueprintRecord>();
        long totalCompressedBytes = 0;
        await Parallel.ForEachAsync(
            selected,
            new ParallelOptions { MaxDegreeOfParallelism = options.Concurrency, CancellationToken = cancellationToken },
            async (item, token) =>
            {
                var name = RequireGuid(item.NameInBucket);
                var destination = Path.Combine(blueprintRoot, name + ".bp");
                long length = 0;
                string? hash = null;
                try
                {
                    if (!File.Exists(destination))
                    {
                        await DownloadBlueprintAsync(client, name, destination, token).ConfigureAwait(false);
                    }

                    length = new FileInfo(destination).Length;
                    var runBytes = Interlocked.Add(ref totalCompressedBytes, length);
                    if (runBytes > MaximumCompressedRunBytes)
                    {
                        throw new InvalidDataException("Corpus exceeds the 4-GiB compressed invocation budget.");
                    }

                    await using var stream = new FileStream(
                        destination,
                        FileMode.Open,
                        FileAccess.Read,
                        FileShare.Read,
                        64 * 1024,
                        FileOptions.SequentialScan);
                    hash = Convert.ToHexString(await SHA256.HashDataAsync(stream, token).ConfigureAwait(false));
                    stream.Position = 0;
                    var analysis = BlueprintAnalyzer.Analyze(stream, name);
                    records.Add(new CorpusBlueprintRecord(name, "parsed", length, hash, null, analysis));
                }
                catch (Exception exception) when (exception is not OperationCanceledException)
                {
                    records.Add(new CorpusBlueprintRecord(name, "rejected", length, hash, exception.Message, null));
                }
            }).ConfigureAwait(false);

        var ordered = records.OrderBy(record => record.Name, StringComparer.Ordinal).ToArray();
        var analyses = ordered.Where(record => record.Analysis is not null).Select(record => record.Analysis!).ToArray();
        var summary = BuildSummary(
            metadata.Length,
            options.BlueprintLimit,
            selected.Length,
            totalCompressedBytes,
            metadataSha,
            ordered,
            analyses);
        var summaryPath = Path.Combine(outputRoot, "summary.json");
        await WriteAtomicAsync(
            summaryPath,
            JsonSerializer.SerializeToUtf8Bytes(summary, JsonOptions),
            cancellationToken).ConfigureAwait(false);

        return new CorpusRunResult(
            metadata.Length,
            options.BlueprintLimit,
            selected.Length,
            analyses.Length,
            ordered.Length - analyses.Length,
            totalCompressedBytes,
            metadataSha,
            summaryPath);
    }

    private static CorpusSummary BuildSummary(
        int metadataCount,
        int requested,
        int attempted,
        long compressedBytes,
        string metadataSha,
        CorpusBlueprintRecord[] records,
        BlueprintAnalysis[] analyses)
    {
        static PlacementMetric Sum(IEnumerable<PlacementMetric> metrics) =>
            new(metrics.Sum(metric => metric.Total), metrics.Sum(metric => metric.Matching));

        var tableDistributions = analyses.SelectMany(analysis => analysis.DiningTables)
            .GroupBy(item => item.DefName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => new ChairDistribution(
                    group.Count(),
                    Math.Round(group.Average(item => item.AdjacentChairCount), 3),
                    group.Min(item => item.AdjacentChairCount),
                    group.Max(item => item.AdjacentChairCount)),
                StringComparer.Ordinal);
        var utilityDistributions = analyses.SelectMany(analysis => analysis.OutdoorUtilities)
            .GroupBy(item => item.DefName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => new UtilityDistribution(group.Count(), group.Count(item => item.Roofed)),
                StringComparer.Ordinal);
        var defCounts = analyses.SelectMany(analysis => analysis.ItemDefCounts)
            .GroupBy(pair => pair.Key, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.Sum(pair => pair.Value), StringComparer.Ordinal);

        return new CorpusSummary(
            "real-ruins-corpus/v1",
            DateTimeOffset.UtcNow,
            metadataCount,
            requested,
            attempted,
            analyses.Length,
            records.Length - analyses.Length,
            compressedBytes,
            metadataSha,
            Sum(analyses.Select(item => item.DiningTablesWithEnoughAdjacentChairs)),
            Sum(analyses.Select(item => item.PowerConduitsUnderWalls)),
            Sum(analyses.Select(item => item.PowerConduitsAtOrAdjacentToWalls)),
            Sum(analyses.Select(item => item.WorkstationsTouchingWalls)),
            Sum(analyses.Select(item => item.FixedOutdoorUtilitiesRoofed)),
            tableDistributions,
            utilityDistributions,
            defCounts,
            records);
    }

    private static async Task DownloadBlueprintAsync(
        HttpClient client,
        string name,
        string destination,
        CancellationToken cancellationToken)
    {
        var uri = new Uri(BucketRoot, name + ".bp");
        using var response = await client.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        if (response.RequestMessage?.RequestUri?.Scheme != Uri.UriSchemeHttps ||
            !string.Equals(response.RequestMessage.RequestUri.Host, BucketRoot.Host, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidDataException("Real Ruins blueprint request left its exact HTTPS host.");
        }

        var bytes = await ReadBoundedAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false),
            MaximumCompressedBlueprintBytes,
            cancellationToken).ConfigureAwait(false);
        await WriteAtomicAsync(destination, bytes, cancellationToken).ConfigureAwait(false);
    }

    private static async Task<byte[]> ReadBoundedAsync(Stream source, long maximumBytes, CancellationToken cancellationToken)
    {
        using var result = new MemoryStream();
        var buffer = new byte[64 * 1024];
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                return result.ToArray();
            }

            if (result.Length + read > maximumBytes)
            {
                throw new InvalidDataException($"Response exceeds the {maximumBytes}-byte budget.");
            }

            result.Write(buffer, 0, read);
        }
    }

    private static async Task WriteAtomicAsync(string destination, byte[] bytes, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(destination) ?? throw new InvalidDataException("Destination has no directory.");
        Directory.CreateDirectory(directory);
        var temporary = Path.Combine(directory, ".rr-" + Guid.NewGuid().ToString("N") + ".tmp");
        try
        {
            await using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                await stream.WriteAsync(bytes, cancellationToken).ConfigureAwait(false);
                await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
            }

            File.Move(temporary, destination, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporary))
            {
                File.Delete(temporary);
            }
        }
    }

    private static string RequireGuid(string value)
    {
        return Guid.TryParseExact(value, "D", out var parsed)
            ? parsed.ToString("D").ToUpperInvariant()
            : throw new InvalidDataException("Real Ruins object name is not a canonical GUID.");
    }

    private static void Validate(CorpusRunOptions options)
    {
        if (options.MetadataLimit is < 1 or > MaximumMetadata)
        {
            throw new ArgumentException($"--metadata-limit must be 1..{MaximumMetadata}.");
        }

        if (options.BlueprintLimit is < 1 or > MaximumBlueprints || options.BlueprintLimit > options.MetadataLimit)
        {
            throw new ArgumentException($"--blueprint-limit must be 1..{MaximumBlueprints} and no larger than metadata limit.");
        }

        if (options.Concurrency is < 1 or > MaximumConcurrency)
        {
            throw new ArgumentException($"--concurrency must be 1..{MaximumConcurrency}.");
        }

        if (!string.Equals(options.OutputFormat, "json", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(options.OutputFormat, "table", StringComparison.OrdinalIgnoreCase))
        {
            throw new ArgumentException("--output must be json or table.");
        }
    }
}
