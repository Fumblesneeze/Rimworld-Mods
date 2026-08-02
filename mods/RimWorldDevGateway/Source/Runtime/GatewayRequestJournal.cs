using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;

namespace RimWorldDevGateway;

public sealed class GatewayRequestJournalEntry
{
    public string RequestId { get; set; } = string.Empty;

    public string Method { get; set; } = string.Empty;

    public string Path { get; set; } = string.Empty;

    public string State { get; set; } = string.Empty;

    public string StartedUtc { get; set; } = string.Empty;

    public string? CompletedUtc { get; set; }

    public long? ElapsedMilliseconds { get; set; }

    public int? StatusCode { get; set; }

    public string? ErrorType { get; set; }

    public string? ErrorMessage { get; set; }
}

public sealed class GatewayRequestJournal
{
    private const int MaximumRequestIdCharacters = 128;
    private const int MaximumMethodCharacters = 16;
    private const int MaximumPathCharacters = 1024;
    private const int MaximumErrorCharacters = 2048;
    private static readonly UTF8Encoding Utf8NoBom = new(false);
    private readonly object sync = new();
    private readonly string journalPath;
    private readonly string lastRequestPath;
    private readonly Func<DateTimeOffset> utcNow;
    private readonly SortedDictionary<long, GatewayRequestJournalEntry> activeAdmissions = new();
    private long nextAdmissionSequence;

    public GatewayRequestJournal(string runDirectory, Func<DateTimeOffset>? utcNow = null)
    {
        if (string.IsNullOrWhiteSpace(runDirectory))
        {
            throw new ArgumentException("A request-journal run directory is required.", nameof(runDirectory));
        }

        var fullRunDirectory = Path.GetFullPath(runDirectory);
        journalPath = Path.Combine(fullRunDirectory, "requests.jsonl");
        lastRequestPath = Path.Combine(fullRunDirectory, "last-request.json");
        this.utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public GatewayHttpResponse Track(
        GatewayHttpRequest request,
        string requestId,
        Func<GatewayHttpRequest, string, GatewayHttpResponse> handler)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new ArgumentException("A request ID is required.", nameof(requestId));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        var started = utcNow().ToUniversalTime();
        var stopwatch = Stopwatch.StartNew();
        var basis = new GatewayRequestJournalEntry
        {
            RequestId = Bound(requestId, MaximumRequestIdCharacters),
            Method = Bound(request.Method ?? string.Empty, MaximumMethodCharacters),
            Path = Bound(request.Path ?? string.Empty, MaximumPathCharacters),
            State = "started",
            StartedUtc = started.ToString("O", CultureInfo.InvariantCulture)
        };
        var admissionSequence = RecordStartBestEffort(basis);

        try
        {
            var response = handler(request, requestId);
            RecordTerminalBestEffort(
                Terminal(basis, "completed", stopwatch.ElapsedMilliseconds, response.StatusCode),
                admissionSequence);
            return response;
        }
        catch (Exception exception)
        {
            var terminal = Terminal(basis, "failed", stopwatch.ElapsedMilliseconds, null);
            terminal.ErrorType = Bound(exception.GetType().FullName ?? exception.GetType().Name, MaximumErrorCharacters);
            terminal.ErrorMessage = Bound(exception.Message ?? string.Empty, MaximumErrorCharacters);
            RecordTerminalBestEffort(terminal, admissionSequence);
            throw;
        }
    }

    private GatewayRequestJournalEntry Terminal(
        GatewayRequestJournalEntry basis,
        string state,
        long elapsedMilliseconds,
        int? statusCode)
    {
        return new GatewayRequestJournalEntry
        {
            RequestId = basis.RequestId,
            Method = basis.Method,
            Path = basis.Path,
            State = state,
            StartedUtc = basis.StartedUtc,
            CompletedUtc = utcNow().ToUniversalTime().ToString("O", CultureInfo.InvariantCulture),
            ElapsedMilliseconds = Math.Max(0, elapsedMilliseconds),
            StatusCode = statusCode
        };
    }

    private long RecordStartBestEffort(GatewayRequestJournalEntry entry)
    {
        lock (sync)
        {
            var admissionSequence = ++nextAdmissionSequence;
            activeAdmissions.Add(admissionSequence, entry);
            RecordBestEffortUnderLock(entry, entry);
            return admissionSequence;
        }
    }

    private void RecordTerminalBestEffort(
        GatewayRequestJournalEntry entry,
        long admissionSequence)
    {
        lock (sync)
        {
            activeAdmissions.Remove(admissionSequence);
            var visibleEntry = entry;
            foreach (var active in activeAdmissions)
            {
                visibleEntry = active.Value;
            }

            RecordBestEffortUnderLock(entry, visibleEntry);
        }
    }

    private void RecordBestEffortUnderLock(
        GatewayRequestJournalEntry entry,
        GatewayRequestJournalEntry visibleEntry)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(journalPath)!);
            var json = Encoding.UTF8.GetString(GatewayJsonWriter.Write(entry, maxDepth: 4, maxNodes: 32, maxUtf8Bytes: 16 * 1024));
            File.AppendAllText(journalPath, json + Environment.NewLine, Utf8NoBom);
            var visibleJson = ReferenceEquals(entry, visibleEntry)
                ? json
                : Encoding.UTF8.GetString(GatewayJsonWriter.Write(
                    visibleEntry,
                    maxDepth: 4,
                    maxNodes: 32,
                    maxUtf8Bytes: 16 * 1024));
            WriteAtomic(lastRequestPath, visibleJson);
        }
        catch (IOException)
        {
            // Diagnostics must never turn a valid local request into a gameplay failure.
        }
        catch (UnauthorizedAccessException)
        {
            // The gateway remains usable if the disposable evidence directory becomes read-only.
        }
    }

    private static void WriteAtomic(string path, string json)
    {
        var temporaryPath = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporaryPath, json, Utf8NoBom);
            if (File.Exists(path))
            {
                File.Replace(temporaryPath, path, null, ignoreMetadataErrors: true);
            }
            else
            {
                File.Move(temporaryPath, path);
            }
        }
        finally
        {
            if (File.Exists(temporaryPath))
            {
                File.Delete(temporaryPath);
            }
        }
    }

    private static string Bound(string value, int maximumCharacters) =>
        value.Length <= maximumCharacters
            ? value
            : value.Substring(0, maximumCharacters - 3) + "...";
}
