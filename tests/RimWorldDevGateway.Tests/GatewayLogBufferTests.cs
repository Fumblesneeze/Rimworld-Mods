using System.Threading.Tasks;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayLogBufferTests
{
    [Test]
    public void Append_assigns_ordered_sequences_and_captures_log_context_in_utc()
    {
        var localTime = new DateTimeOffset(2026, 8, 1, 12, 30, 0, TimeSpan.FromHours(2));
        var buffer = new GatewayLogBuffer(capacity: 4, utcNow: () => localTime);

        var first = buffer.Append(
            "Warning",
            "first message",
            "first stack",
            "worker-7",
            "request-42");
        var second = buffer.Append("Error", "second message");

        Assert.Multiple(() =>
        {
            Assert.That(first.Sequence, Is.EqualTo(1L));
            Assert.That(second.Sequence, Is.EqualTo(2L));
            Assert.That(first.TimestampUtc, Is.EqualTo(new DateTimeOffset(2026, 8, 1, 10, 30, 0, TimeSpan.Zero)));
            Assert.That(first.TimestampUtc.Offset, Is.EqualTo(TimeSpan.Zero));
            Assert.That(first.Severity, Is.EqualTo("Warning"));
            Assert.That(first.Message, Is.EqualTo("first message"));
            Assert.That(first.Stack, Is.EqualTo("first stack"));
            Assert.That(first.Thread, Is.EqualTo("worker-7"));
            Assert.That(first.RequestId, Is.EqualTo("request-42"));
        });
    }

    [Test]
    public void Read_returns_retained_entries_in_order_and_reports_an_evicted_gap()
    {
        var buffer = new GatewayLogBuffer(capacity: 2);
        buffer.Append("Message", "one");
        buffer.Append("Message", "two");
        buffer.Append("Message", "three");

        var result = buffer.Read(afterExclusive: 0, limit: 10);
        var resumed = buffer.Read(afterExclusive: 2, limit: 10);

        Assert.Multiple(() =>
        {
            Assert.That(result.Entries.Select(entry => entry.Sequence), Is.EqualTo(new[] { 2L, 3L }));
            Assert.That(result.Entries.Select(entry => entry.Message), Is.EqualTo(new[] { "two", "three" }));
            Assert.That(result.OldestCursor, Is.EqualTo(2L));
            Assert.That(result.NewestCursor, Is.EqualTo(3L));
            Assert.That(result.HistoryEvicted, Is.True);
            Assert.That(resumed.Entries.Select(entry => entry.Sequence), Is.EqualTo(new[] { 3L }));
            Assert.That(resumed.HistoryEvicted, Is.False);
        });
    }

    [Test]
    public void Append_redacts_the_session_bearer_token_from_message_and_stack()
    {
        const string token = "gateway-secret-token";
        var buffer = new GatewayLogBuffer(capacity: 4, bearerToken: token);

        var entry = buffer.Append(
            "Error",
            "Authorization: Bearer " + token,
            "request failed with " + token);

        Assert.Multiple(() =>
        {
            Assert.That(entry.Message, Is.EqualTo("Authorization: Bearer [REDACTED]"));
            Assert.That(entry.Stack, Is.EqualTo("request failed with [REDACTED]"));
            Assert.That(entry.Message, Does.Not.Contain(token));
            Assert.That(entry.Stack, Does.Not.Contain(token));
        });
    }

    [Test]
    public void Append_bounds_retained_message_and_stack_by_UTF8_bytes_after_redaction()
    {
        const string token = "gateway-secret-token";
        var buffer = new GatewayLogBuffer(
            capacity: 2,
            bearerToken: token,
            maximumMessageUtf8Bytes: 32,
            maximumStackUtf8Bytes: 48);

        var entry = buffer.Append(
            "Error",
            token + new string('\u20ac', 40),
            token + new string('\u20ac', 40));

        Assert.Multiple(() =>
        {
            Assert.That(System.Text.Encoding.UTF8.GetByteCount(entry.Message), Is.LessThanOrEqualTo(32));
            Assert.That(System.Text.Encoding.UTF8.GetByteCount(entry.Stack!), Is.LessThanOrEqualTo(48));
            Assert.That(entry.Message, Does.Not.Contain(token));
            Assert.That(entry.Stack, Does.Not.Contain(token));
            Assert.That(entry.Message, Does.EndWith("..."));
            Assert.That(entry.Stack, Does.EndWith("..."));
        });
    }

    [Test]
    public void Read_clamps_requested_limits_to_one_and_the_configured_maximum()
    {
        var buffer = new GatewayLogBuffer(capacity: 4, maximumReadLimit: 2);
        buffer.Append("Message", "one");
        buffer.Append("Message", "two");
        buffer.Append("Message", "three");

        var belowMinimum = buffer.Read(afterExclusive: 0, limit: 0);
        var aboveMaximum = buffer.Read(afterExclusive: 0, limit: int.MaxValue);

        Assert.Multiple(() =>
        {
            Assert.That(belowMinimum.Entries.Select(entry => entry.Sequence), Is.EqualTo(new[] { 1L }));
            Assert.That(aboveMaximum.Entries.Select(entry => entry.Sequence), Is.EqualTo(new[] { 1L, 2L }));
        });
    }

    [Test]
    public void Read_stops_at_the_aggregate_json_budget_before_the_entry_limit()
    {
        var buffer = new GatewayLogBuffer(
            capacity: 16,
            maximumReadLimit: 16,
            maximumReadUtf8Bytes: 1_500);
        for (var index = 0; index < 10; index++)
        {
            buffer.Append("Message", new string('"', 400));
        }

        var result = buffer.Read(afterExclusive: 0, limit: 16);
        var serializedBytes = GatewayJsonWriter.Write(result).Length;

        Assert.Multiple(() =>
        {
            Assert.That(result.Entries.Count, Is.GreaterThan(0).And.LessThan(10));
            Assert.That(result.PageTruncated, Is.True);
            Assert.That(serializedBytes, Is.LessThan(2_048));
        });
    }

    [Test]
    public void Concurrent_appends_keep_every_sequence_unique_and_readable_in_order()
    {
        const int entryCount = 512;
        var buffer = new GatewayLogBuffer(capacity: entryCount, maximumReadLimit: entryCount);

        Parallel.For(
            0,
            entryCount,
            index => buffer.Append("Message", "entry-" + index));

        var result = buffer.Read(afterExclusive: 0, limit: entryCount);
        var sequences = result.Entries.Select(entry => entry.Sequence).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(sequences, Has.Length.EqualTo(entryCount));
            Assert.That(sequences, Is.Ordered);
            Assert.That(sequences.Distinct().ToArray(), Has.Length.EqualTo(entryCount));
            Assert.That(sequences.First(), Is.EqualTo(1L));
            Assert.That(sequences.Last(), Is.EqualTo((long)entryCount));
        });
    }
}
