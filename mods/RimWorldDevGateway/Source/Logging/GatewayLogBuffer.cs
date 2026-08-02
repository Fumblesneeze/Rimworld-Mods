using System.Globalization;
using System.Text;
using System.Threading;

namespace RimWorldDevGateway;

public sealed class GatewayLogEntry
{
    internal GatewayLogEntry(
        long sequence,
        DateTimeOffset timestampUtc,
        string severity,
        string message,
        string? stack,
        string thread,
        string? requestId)
    {
        Sequence = sequence;
        TimestampUtc = timestampUtc;
        Severity = severity;
        Message = message;
        Stack = stack;
        Thread = thread;
        RequestId = requestId;
    }

    public long Sequence { get; }

    public DateTimeOffset TimestampUtc { get; }

    public string Severity { get; }

    public string Message { get; }

    public string? Stack { get; }

    public string Thread { get; }

    public string? RequestId { get; }
}

public sealed class GatewayLogReadResult
{
    internal GatewayLogReadResult(
        IReadOnlyList<GatewayLogEntry> entries,
        long oldestCursor,
        long newestCursor,
        bool historyEvicted,
        bool pageTruncated)
    {
        Entries = entries;
        OldestCursor = oldestCursor;
        NewestCursor = newestCursor;
        HistoryEvicted = historyEvicted;
        PageTruncated = pageTruncated;
    }

    public IReadOnlyList<GatewayLogEntry> Entries { get; }

    public long OldestCursor { get; }

    public long NewestCursor { get; }

    public bool HistoryEvicted { get; }

    public bool PageTruncated { get; }
}

public sealed class GatewayLogBuffer
{
    private readonly object sync = new();
    private readonly GatewayLogEntry?[] entries;
    private readonly Func<DateTimeOffset> utcNow;
    private readonly string? bearerToken;
    private readonly int maximumReadLimit;
    private readonly int maximumReadUtf8Bytes;
    private readonly int maximumMessageUtf8Bytes;
    private readonly int maximumStackUtf8Bytes;
    private int start;
    private int count;
    private long sequence;

    public GatewayLogBuffer(
        int capacity = 2_000,
        Func<DateTimeOffset>? utcNow = null,
        string? bearerToken = null,
        int maximumReadLimit = 500,
        int maximumReadUtf8Bytes = 3 * 1024 * 1024,
        int maximumMessageUtf8Bytes = 8 * 1024,
        int maximumStackUtf8Bytes = 32 * 1024)
    {
        if (capacity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(capacity));
        }

        if (maximumReadLimit <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumReadLimit));
        }

        if (maximumReadUtf8Bytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumReadUtf8Bytes));
        }

        if (maximumMessageUtf8Bytes < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumMessageUtf8Bytes));
        }

        if (maximumStackUtf8Bytes < 3)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumStackUtf8Bytes));
        }

        entries = new GatewayLogEntry[capacity];
        this.utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
        this.bearerToken = string.IsNullOrEmpty(bearerToken) ? null : bearerToken;
        this.maximumReadLimit = maximumReadLimit;
        this.maximumReadUtf8Bytes = maximumReadUtf8Bytes;
        this.maximumMessageUtf8Bytes = maximumMessageUtf8Bytes;
        this.maximumStackUtf8Bytes = maximumStackUtf8Bytes;
    }

    public GatewayLogEntry Append(
        string severity,
        string message,
        string? stack = null,
        string? thread = null,
        string? requestId = null)
    {
        if (severity is null)
        {
            throw new ArgumentNullException(nameof(severity));
        }

        if (message is null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        lock (sync)
        {
            var entry = new GatewayLogEntry(
                ++sequence,
                utcNow().ToUniversalTime(),
                severity,
                BoundUtf8(RedactRequired(message), maximumMessageUtf8Bytes),
                BoundOptionalUtf8(RedactOptional(stack), maximumStackUtf8Bytes),
                thread ?? Thread.CurrentThread.ManagedThreadId.ToString(CultureInfo.InvariantCulture),
                requestId);
            if (count < entries.Length)
            {
                entries[(start + count) % entries.Length] = entry;
                count++;
            }
            else
            {
                entries[start] = entry;
                start = (start + 1) % entries.Length;
            }

            return entry;
        }
    }

    public GatewayLogReadResult Read(long afterExclusive, int limit)
    {
        lock (sync)
        {
            var clampedLimit = Math.Min(maximumReadLimit, Math.Max(1, limit));
            var oldestCursor = count == 0 ? 0 : EntryAt(0).Sequence;
            var newestCursor = count == 0 ? 0 : EntryAt(count - 1).Sequence;
            var selected = new List<GatewayLogEntry>(Math.Min(count, clampedLimit));
            var selectedJsonBytes = 2;
            var pageTruncated = false;
            for (var offset = 0; offset < count; offset++)
            {
                var entry = EntryAt(offset);
                if (entry.Sequence <= afterExclusive)
                {
                    continue;
                }

                if (selected.Count == clampedLimit)
                {
                    pageTruncated = true;
                    break;
                }

                var entryBytes = GatewayJsonWriter.Write(entry).Length;
                var projectedBytes = selectedJsonBytes + (selected.Count == 0 ? 0 : 1) + entryBytes;
                if (selected.Count > 0 && projectedBytes > maximumReadUtf8Bytes)
                {
                    pageTruncated = true;
                    break;
                }

                selected.Add(entry);
                selectedJsonBytes = projectedBytes;
            }

            var historyEvicted = oldestCursor > 1 && afterExclusive < oldestCursor - 1;

            return new GatewayLogReadResult(
                selected.AsReadOnly(),
                oldestCursor,
                newestCursor,
                historyEvicted,
                pageTruncated);
        }
    }

    private GatewayLogEntry EntryAt(int offset) =>
        entries[(start + offset) % entries.Length]!;

    private string RedactRequired(string value) =>
        bearerToken is null ? value : value.Replace(bearerToken, "[REDACTED]");

    private string? RedactOptional(string? value) =>
        value is null ? null : RedactRequired(value);

    private static string? BoundOptionalUtf8(string? value, int maximumBytes) =>
        value is null ? null : BoundUtf8(value, maximumBytes);

    private static string BoundUtf8(string value, int maximumBytes)
    {
        if (Encoding.UTF8.GetByteCount(value) <= maximumBytes)
        {
            return value;
        }

        const string suffix = "...";
        var contentBudget = maximumBytes - Encoding.UTF8.GetByteCount(suffix);
        var lower = 0;
        var upper = value.Length;
        while (lower < upper)
        {
            var candidate = lower + (upper - lower + 1) / 2;
            if (Encoding.UTF8.GetByteCount(value.Substring(0, candidate)) <= contentBudget)
            {
                lower = candidate;
            }
            else
            {
                upper = candidate - 1;
            }
        }

        if (lower > 0 && lower < value.Length &&
            char.IsHighSurrogate(value[lower - 1]) && char.IsLowSurrogate(value[lower]))
        {
            lower--;
        }

        return value.Substring(0, lower) + suffix;
    }
}
