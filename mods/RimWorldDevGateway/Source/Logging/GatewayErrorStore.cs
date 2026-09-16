using System.Security.Cryptography;
using System.Text;

namespace RimWorldDevGateway;

public sealed class GatewayErrorRecord
{
    internal GatewayErrorRecord(string id, long sequence, string type, string message, string stack,
        DateTimeOffset first, DateTimeOffset last, long occurrences, string? requestId,
        IReadOnlyList<GatewayCapturedFrame> frames, IReadOnlyList<GatewayErrorCause> causes, bool truncated)
    {
        Id = id; Sequence = sequence; Type = type; Message = message; Stack = stack;
        FirstSeenUtc = first; LastSeenUtc = last; Occurrences = occurrences; LastRequestId = requestId;
        Frames = frames; Causes = causes; Truncated = truncated;
    }
    public string Id { get; }
    public long Sequence { get; }
    public string Type { get; }
    public string Message { get; }
    public string Stack { get; }
    public DateTimeOffset FirstSeenUtc { get; }
    public DateTimeOffset LastSeenUtc { get; }
    public long Occurrences { get; }
    public string? LastRequestId { get; }
    public IReadOnlyList<GatewayCapturedFrame> Frames { get; }
    public IReadOnlyList<GatewayErrorCause> Causes { get; }
    public bool Truncated { get; }
}

public sealed class GatewayCapturedFrame
{
    public GatewayCapturedFrame(string methodHandle, string signature, int? ilOffset = null)
    { MethodHandle = methodHandle; Signature = signature; IlOffset = ilOffset; }
    public string MethodHandle { get; }
    public string Signature { get; }
    public int? IlOffset { get; }
}

public sealed class GatewayErrorCause
{
    public GatewayErrorCause(string type, string message, string stack, IReadOnlyList<GatewayCapturedFrame>? frames = null)
    { Type = type; Message = message; Stack = stack; Frames = frames ?? Array.Empty<GatewayCapturedFrame>(); }
    public string Type { get; }
    public string Message { get; }
    public string Stack { get; }
    public IReadOnlyList<GatewayCapturedFrame> Frames { get; }
}

public sealed class GatewayErrorPage
{
    internal GatewayErrorPage(IReadOnlyList<GatewayErrorRecord> entries, long evicted, bool more, long cursor)
    { Entries = entries; EvictedErrors = evicted; PageTruncated = more; NextCursor = cursor; }
    public IReadOnlyList<GatewayErrorRecord> Entries { get; }
    public long EvictedErrors { get; }
    public bool PageTruncated { get; }
    public long NextCursor { get; }
}

public sealed class GatewayErrorStore
{
    private readonly object sync = new();
    private readonly Dictionary<string, GatewayErrorRecord> records = new(StringComparer.Ordinal);
    private readonly int capacity;
    private readonly Func<string, string> redact;
    private readonly Func<DateTimeOffset> utcNow;
    private long sequence;
    private long evicted;

    public GatewayErrorStore(int capacity = 512, Func<string, string>? redact = null,
        Func<DateTimeOffset>? utcNow = null)
    {
        if (capacity < 1) throw new ArgumentOutOfRangeException(nameof(capacity));
        this.capacity = capacity; this.redact = redact ?? (value => value);
        this.utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public GatewayErrorRecord Add(string type, string message, string stack,
        IReadOnlyList<GatewayCapturedFrame>? frames = null, IReadOnlyList<GatewayErrorCause>? causes = null,
        string? requestId = null, bool truncated = false)
    {
        var budget = new RetentionBudget { Truncated = truncated };
        var boundedType = Bound(type, 1024, budget);
        var boundedMessage = Bound(message, 8192, budget);
        var boundedStack = Bound(stack, 32768, budget);
        var boundedFrames = BoundFrames(frames, budget);
        var boundedCauses = new List<GatewayErrorCause>();
        if (causes is not null)
        {
            if (causes.Count > 8) budget.Truncated = true;
            foreach (var cause in causes.Take(8))
                boundedCauses.Add(new GatewayErrorCause(Bound(cause.Type, 1024, budget),
                    Bound(cause.Message, 8192, budget), Bound(cause.Stack, 32768, budget),
                    BoundFrames(cause.Frames, budget)));
        }
        // Length-prefixed values avoid ambiguous concatenations. Request/time are not identity.
        var identity = new StringBuilder();
        void Part(string value) => identity.Append(value.Length).Append(':').Append(value);
        Part(boundedType); Part(boundedMessage); Part(boundedStack);
        foreach (var frame in boundedFrames) { Part(frame.MethodHandle); Part(frame.Signature); }
        foreach (var cause in boundedCauses)
        {
            Part(cause.Type); Part(cause.Message); Part(cause.Stack);
            foreach (var frame in cause.Frames) { Part(frame.MethodHandle); Part(frame.Signature); }
        }
        var boundedRequest = requestId is null ? null : Bound(requestId, 128, budget);
        // Incomplete snapshots never assert equivalence with another error.
        if (budget.Truncated) Part(Guid.NewGuid().ToString("N"));
        using var sha = SHA256.Create();
        var id = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(identity.ToString()))).Replace("-", "");
        lock (sync)
        {
            records.TryGetValue(id, out var previous);
            var now = utcNow().ToUniversalTime();
            var record = new GatewayErrorRecord(id, ++sequence, boundedType, boundedMessage, boundedStack,
                previous?.FirstSeenUtc ?? now, now, (previous?.Occurrences ?? 0) + 1,
                boundedRequest,
                boundedFrames, boundedCauses.AsReadOnly(), budget.Truncated);
            records[id] = record;
            if (records.Count > capacity)
            {
                var oldest = records.Values.OrderBy(value => value.Sequence).First();
                records.Remove(oldest.Id); evicted++;
            }
            return record;
        }
    }

    public GatewayErrorRecord Get(string id)
    {
        lock (sync) return records.TryGetValue(id, out var record) ? record :
            throw new ArgumentException("Error is unknown or was evicted: " + id);
    }

    public GatewayErrorPage Query(long after = 0, int limit = 50, string? filter = null)
    {
        if (after < 0 || limit < 1 || limit > 100 || (filter?.Length ?? 0) > 1024)
            throw new ArgumentException("after must be nonnegative; limit 1–100; filter at most 1024 characters.");
        lock (sync)
        {
            var matches = records.Values.Where(value => value.Sequence > after &&
                (string.IsNullOrEmpty(filter) || (value.Type + " " + value.Message).IndexOf(filter,
                    StringComparison.OrdinalIgnoreCase) >= 0)).OrderBy(value => value.Sequence).Take(limit + 1).ToArray();
            var page = matches.Take(limit).ToArray();
            return new GatewayErrorPage(Array.AsReadOnly(page), evicted, matches.Length > limit,
                page.LastOrDefault()?.Sequence ?? after);
        }
    }

    private IReadOnlyList<GatewayCapturedFrame> BoundFrames(IReadOnlyList<GatewayCapturedFrame>? frames, RetentionBudget budget)
    {
        if (frames is null) return Array.Empty<GatewayCapturedFrame>();
        if (frames.Count > budget.Frames) budget.Truncated = true;
        var result = new List<GatewayCapturedFrame>();
        foreach (var frame in frames.Take(budget.Frames).ToArray())
            result.Add(new GatewayCapturedFrame(Bound(frame.MethodHandle, 256, budget),
                Bound(frame.Signature, 2048, budget), frame.IlOffset));
        budget.Frames -= result.Count;
        return result.AsReadOnly();
    }

    private sealed class RetentionBudget
    {
        internal int Characters = 65536;
        internal int Frames = 64;
        internal bool Truncated;
    }

    private string Bound(string value, int maximum, RetentionBudget budget)
    {
        value = redact(value ?? string.Empty);
        maximum = Math.Min(maximum, budget.Characters);
        budget.Characters -= Math.Min(value.Length, maximum);
        if (value.Length <= maximum) return value;
        budget.Truncated = true;
        return value.Substring(0, maximum);
    }
}
