using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Verse;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayDefExportRobustnessRouterTests
{
    [Test]
    public void Invalid_cursor_is_rejected_before_main_thread_dispatch()
    {
        var source = new EmptySource();
        var dispatcher = new GatewayDispatcher();
        var router = Router(dispatcher, source);

        var send = Task.Run(() => router.Handle(
            Post("{\"cursor\":\"not-base64!\"}"),
            "invalid-def-cursor"));
        Assert.That(SpinWait.SpinUntil(() => send.IsCompleted || dispatcher.PendingCount > 0, 1000), Is.True);
        var wasDispatched = dispatcher.PendingCount > 0;
        if (wasDispatched)
        {
            dispatcher.Drain(DispatchPhase.Update);
        }

        var response = send.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(wasDispatched, Is.False);
            Assert.That(response.StatusCode, Is.EqualTo(400), body);
            Assert.That(body, Does.Contain("\"code\":\"invalid_def_export_cursor\""));
            Assert.That(source.CaptureCount, Is.Zero);
        });
    }

    [Test]
    public void Oversized_filter_sets_values_and_pages_are_rejected_before_dispatch()
    {
        var cases = new[]
        {
            (
                "{\"defNames\":[" + string.Join(",", Enumerable.Range(0, GatewayDefExportRequest.MaximumFilterValues + 1).Select(index => "\"Def" + index + "\"")) + "]}",
                "too_many_def_export_filters"),
            (
                "{\"defNames\":[\"" + new string('x', GatewayDefExportRequest.MaximumFilterValueLength + 1) + "\"]}",
                "invalid_def_export_filter"),
            (
                "{\"fieldNames\":[" + string.Join(",", Enumerable.Range(0, GatewayDefExportRequest.MaximumFieldNames + 1).Select(index => "\"field" + index + "\"")) + "]}",
                "too_many_def_export_fields"),
            (
                "{\"fieldNames\":[\"" + new string('x', GatewayDefExportRequest.MaximumFieldNameLength + 1) + "\"]}",
                "invalid_def_export_field"),
            (
                "{\"pageSize\":" + (GatewayDefExporter.MaximumPageSize + 1) + "}",
                "invalid_def_export_page_size")
        };

        foreach (var testCase in cases)
        {
            var source = new EmptySource();
            var dispatcher = new GatewayDispatcher();
            var send = Task.Run(() => Router(dispatcher, source).Handle(
                Post(testCase.Item1),
                "invalid-def-bounds"));
            Assert.That(SpinWait.SpinUntil(() => send.IsCompleted || dispatcher.PendingCount > 0, 1000), Is.True);
            var wasDispatched = dispatcher.PendingCount > 0;
            if (wasDispatched)
            {
                dispatcher.Drain(DispatchPhase.Update);
            }

            var response = send.GetAwaiter().GetResult();
            var body = Encoding.UTF8.GetString(response.Body);
            Assert.Multiple(() =>
            {
                Assert.That(wasDispatched, Is.False, testCase.Item2);
                Assert.That(response.StatusCode, Is.EqualTo(400), body);
                Assert.That(body, Does.Contain("\"code\":\"" + testCase.Item2 + "\""));
                Assert.That(source.CaptureCount, Is.Zero);
            });
        }
    }

    [Test]
    public void Schema_valid_request_larger_than_legacy_64_kib_reaches_export()
    {
        var defNames = Enumerable.Range(0, GatewayDefExportRequest.MaximumFilterValues)
            .Select(index => "Def" + index.ToString("D2") + new string('d', 507));
        var sourcePackageIds = Enumerable.Range(0, GatewayDefExportRequest.MaximumFilterValues)
            .Select(index => "Pkg" + index.ToString("D2") + new string('p', 507));
        var json = "{\"defNames\":[\"" + string.Join("\",\"", defNames) +
                   "\"],\"sourcePackageIds\":[\"" + string.Join("\",\"", sourcePackageIds) + "\"]}";
        var requestBytes = Encoding.UTF8.GetByteCount(json);
        var source = new EmptySource();
        var dispatcher = new GatewayDispatcher();
        var send = Task.Run(() => Router(dispatcher, source).Handle(
            Post(json),
            "large-valid-def-request"));

        Assert.That(requestBytes, Is.GreaterThan(64 * 1024));
        Assert.That(SpinWait.SpinUntil(() => send.IsCompleted || dispatcher.PendingCount > 0, 1000), Is.True);
        var wasDispatched = dispatcher.PendingCount > 0;
        if (wasDispatched)
        {
            dispatcher.Drain(DispatchPhase.Update);
        }

        var response = send.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);
        Assert.Multiple(() =>
        {
            Assert.That(wasDispatched, Is.True, body);
            Assert.That(response.StatusCode, Is.EqualTo(200), body);
            Assert.That(body, Does.Not.Contain("\"code\":\"def_export_request_too_large\""));
            Assert.That(source.CaptureCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Maximum_legal_projection_page_uses_the_def_specific_response_budget()
    {
        var records = Enumerable.Range(0, GatewayDefExporter.MaximumPageSize)
            .Select(index => new GatewayDefRecord(
                typeof(NodeHeavyDef),
                new NodeHeavyDef
                {
                    defName = "Heavy-" + index.ToString("D3"),
                    payload = Enumerable.Range(0, 32)
                        .Select(_ => (object)Enumerable.Range(0, 128).ToArray())
                        .ToArray()
                },
                "example.heavy",
                "Heavy",
                "Defs/Heavy.xml"))
            .ToArray();
        var source = new RecordSource(records);
        var dispatcher = new GatewayDispatcher();
        var cancellation = new CancellationTokenSource();
        var request = Post("{\"pageSize\":" + GatewayDefExporter.MaximumPageSize + "}", cancellation.Token);

        var send = Task.Run(() => Router(dispatcher, source).Handle(request, "maximum-def-page"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.Update);
        var response = send.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200), body);
            Assert.That(source.LastCancellation.CanBeCanceled, Is.True);
            Assert.That(body, Does.Contain("\"DefName\":\"Heavy-000\""));
            Assert.That(body, Does.Contain("\"DefName\":\"Heavy-031\""));
            Assert.That(response.Body.Length, Is.LessThanOrEqualTo(GatewayDefExporter.MaximumSerializedPageUtf8Bytes));
        });
    }

    [Test]
    public void Def_export_route_stops_at_32_mib_with_a_cursor_before_the_next_worst_escaped_item()
    {
        var worstEscaped = new string('\ud800', GatewayDefExporter.MaximumStringLength);
        var records = Enumerable.Range(0, 6)
            .Select(index => new GatewayDefRecord(
                typeof(PageByteHeavyDef),
                PageByteHeavyDef.Create("Heavy-" + index.ToString("D3"), worstEscaped),
                "example.page-bytes",
                "Page bytes",
                "Defs/PageBytes.xml"))
            .ToArray();
        var source = new RecordSource(records);
        var dispatcher = new GatewayDispatcher();
        var requestId = new string('\u754c', 64);
        var send = Task.Run(() => Router(dispatcher, source).Handle(
            Post("{\"pageSize\":6}"),
            requestId));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.Update);

        var response = send.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200), body);
            Assert.That(response.Body.Length, Is.LessThanOrEqualTo(GatewayDefExporter.MaximumSerializedPageUtf8Bytes));
            Assert.That(body, Does.Contain("\"Truncated\":true"));
            Assert.That(body, Does.Contain("\"NextCursor\":\"v1."));
            Assert.That(body, Does.Contain("\"DefName\":\"Heavy-000\""));
            Assert.That(body, Does.Contain("\"DefName\":\"Heavy-003\""));
            Assert.That(body, Does.Not.Contain("\"DefName\":\"Heavy-004\""));
        });
    }

    [Test]
    public void Def_export_route_pages_before_the_response_node_limit_and_resumes_at_the_omitted_def()
    {
        var records = Enumerable.Range(0, GatewayDefExporter.MaximumPageSize)
            .Select(index => new GatewayDefRecord(
                typeof(DictionaryNodeHeavyDef),
                DictionaryNodeHeavyDef.Create("Dictionary-" + index.ToString("D3")),
                "example.dictionary-nodes",
                "Dictionary nodes",
                "Defs/DictionaryNodes.xml"))
            .ToArray();
        var source = new RecordSource(records);
        var dispatcher = new GatewayDispatcher();
        var router = Router(dispatcher, source);

        var firstSend = Task.Run(() => router.Handle(
            Post("{\"pageSize\":" + GatewayDefExporter.MaximumPageSize + "}"),
            "dictionary-node-page-1"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.Update);
        var first = firstSend.GetAwaiter().GetResult();
        var firstBody = Encoding.UTF8.GetString(first.Body);

        Assert.That(first.StatusCode, Is.EqualTo(200), firstBody);
        Assert.That(firstBody, Does.Contain("\"Truncated\":true"));
        var cursorMatch = Regex.Match(firstBody, "\\\"NextCursor\\\":\\\"(?<cursor>[^\\\"]+)\\\"");
        Assert.That(cursorMatch.Success, Is.True, firstBody);
        Assert.That(firstBody, Does.Contain("\"DefName\":\"Dictionary-000\""));
        Assert.That(firstBody, Does.Not.Contain("\"DefName\":\"Dictionary-031\""));

        var cursor = cursorMatch.Groups["cursor"].Value;
        var secondSend = Task.Run(() => router.Handle(
            Post("{\"cursor\":\"" + cursor + "\",\"pageSize\":" + GatewayDefExporter.MaximumPageSize + "}"),
            "dictionary-node-page-2"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.Update);
        var second = secondSend.GetAwaiter().GetResult();
        var secondBody = Encoding.UTF8.GetString(second.Body);

        Assert.Multiple(() =>
        {
            Assert.That(second.StatusCode, Is.EqualTo(200), secondBody);
            Assert.That(secondBody, Does.Contain("\"DefName\":\"Dictionary-031\""));
            Assert.That(secondBody, Does.Contain("\"Truncated\":false"));
            Assert.That(secondBody, Does.Contain("\"NextCursor\":null"));
        });
    }

    private static GatewayApiRouter Router(GatewayDispatcher dispatcher, IGatewayDefSource source) =>
        new(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(defExporter: new GatewayDefExporter(source)),
            responseTimeout: TimeSpan.FromSeconds(30));

    private static GatewayHttpRequest Post(string body, CancellationToken cancellationToken = default) => new(
        "POST",
        "/api/v1/defs/export",
        "/api/v1/defs/export",
        string.Empty,
        new Dictionary<string, string>(),
        Encoding.UTF8.GetBytes(body),
        cancellationToken);

    private sealed class EmptySource : IGatewayDefSource
    {
        public int CaptureCount { get; private set; }

        public GatewayDefSourcePage CapturePage(
            GatewayDefSourceQuery query,
            CancellationToken cancellationToken)
        {
            CaptureCount++;
            return new GatewayDefSourcePage(Array.Empty<GatewayDefRecord>(), false);
        }
    }

    private sealed class RecordSource : IGatewayDefSource
    {
        private readonly IReadOnlyList<GatewayDefRecord> records;

        public RecordSource(IReadOnlyList<GatewayDefRecord> records)
        {
            this.records = records;
        }

        public CancellationToken LastCancellation { get; private set; }

        public GatewayDefSourcePage CapturePage(
            GatewayDefSourceQuery query,
            CancellationToken cancellationToken)
        {
            LastCancellation = cancellationToken;
            return new GatewayDefSourcePage(records.Take(query.Limit), records.Count > query.Limit);
        }
    }

    private sealed class NodeHeavyDef : Def
    {
        public object[] payload = Array.Empty<object>();
    }

    private sealed class PageByteHeavyDef : Def
    {
        public object? field00;
        public object? field01;
        public object? field02;
        public object? field03;
        public object? field04;
        public object? field05;
        public object? field06;
        public object? field07;
        public object? field08;
        public object? field09;

        public static PageByteHeavyDef Create(string name, string payload)
        {
            object Value() => new[] { payload, payload };
            return new PageByteHeavyDef
            {
                defName = name,
                field00 = Value(),
                field01 = Value(),
                field02 = Value(),
                field03 = Value(),
                field04 = Value(),
                field05 = Value(),
                field06 = Value(),
                field07 = Value(),
                field08 = Value(),
                field09 = Value()
            };
        }
    }

    private sealed class DictionaryNodeHeavyDef : Def
    {
        public Dictionary<int, int> aDictionary0 = Entries();
        public Dictionary<int, int> aDictionary1 = Entries();
        public Dictionary<int, int> aDictionary2 = Entries();
        public Dictionary<int, int> aDictionary3 = Entries();

        public static DictionaryNodeHeavyDef Create(string name) => new() { defName = name };

        private static Dictionary<int, int> Entries() => Enumerable.Range(0, GatewayDefExporter.MaximumCollectionItems)
            .ToDictionary(value => value, value => value);
    }

    private sealed class StubStateProvider : IGatewayStateProvider
    {
        public object CaptureStatus() => new { };

        public object CaptureUiState() => new { };
    }
}
