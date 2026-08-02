using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Verse;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayDefExportRouterTests
{
    [Test]
    public void Finalized_def_export_is_deserialized_and_captured_on_the_dispatcher_thread()
    {
        var mainThreadId = Thread.CurrentThread.ManagedThreadId;
        var source = new RecordingSource(new GatewayDefRecord(
            typeof(ProbeDef),
            new ProbeDef
            {
                defName = "Probe",
                label = "patched label",
                marker = "patched",
                unselected = "must not be projected"
            },
            "example.mod",
            "Example Mod",
            "Defs/Probe.xml"));
        var dispatcher = new GatewayDispatcher();
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(defExporter: new GatewayDefExporter(source)),
            responseTimeout: TimeSpan.FromSeconds(2));

        var responseTask = Task.Run(() => router.Handle(
            Post(
                "/api/v1/defs/export",
                "{\"format\":\"json\",\"defTypes\":[\"" + typeof(ProbeDef).FullName +
                "\"],\"defNames\":[\"Probe\"],\"sourcePackageIds\":[\"example.mod\"]," +
                "\"fieldNames\":[\"label\",\"marker\"],\"pageSize\":1}"),
            "def-export-route"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.Update);
        var response = responseTask.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200), body);
            Assert.That(source.CaptureThreadId, Is.EqualTo(mainThreadId));
            Assert.That(body, Does.Contain("\"requestId\":\"def-export-route\""));
            Assert.That(body, Does.Contain("\"DefName\":\"Probe\""));
            Assert.That(body, Does.Contain("\"label\":\"patched label\""));
            Assert.That(body, Does.Contain("\"marker\":\"patched\""));
            Assert.That(body, Does.Not.Contain("\"unselected\":"));
            Assert.That(body, Does.Contain("\"SourcePackageId\":\"example.mod\""));
            Assert.That(body, Does.Contain("\"IsCanonicalSourceXml\":false"));
        });
    }

    [Test]
    public void Xml_export_request_fails_explicitly_after_main_thread_admission()
    {
        var source = new RecordingSource();
        var dispatcher = new GatewayDispatcher();
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(defExporter: new GatewayDefExporter(source)),
            responseTimeout: TimeSpan.FromSeconds(2));

        var responseTask = Task.Run(() => router.Handle(
            Post("/api/v1/defs/export", "{\"format\":\"xml\"}"),
            "def-export-xml"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.Update);
        var response = responseTask.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(400), body);
            Assert.That(body, Does.Contain("\"code\":\"unsupported_def_export_format\""));
            Assert.That(source.CaptureCount, Is.Zero);
        });
    }

    private static GatewayHttpRequest Post(string path, string body) => new(
        "POST",
        path,
        path,
        string.Empty,
        new Dictionary<string, string>(),
        Encoding.UTF8.GetBytes(body));

    private sealed class RecordingSource : IGatewayDefSource
    {
        private readonly IReadOnlyList<GatewayDefRecord> records;

        public RecordingSource(params GatewayDefRecord[] records)
        {
            this.records = records;
        }

        public int CaptureCount { get; private set; }

        public int CaptureThreadId { get; private set; }

        public GatewayDefSourcePage CapturePage(
            GatewayDefSourceQuery query,
            CancellationToken cancellationToken)
        {
            CaptureCount++;
            CaptureThreadId = Thread.CurrentThread.ManagedThreadId;
            return new GatewayDefSourcePage(records.Take(query.Limit), records.Count > query.Limit);
        }
    }

    private sealed class ProbeDef : Def
    {
        public string marker = string.Empty;
        public string unselected = string.Empty;
    }

    private sealed class StubStateProvider : IGatewayStateProvider
    {
        public object CaptureStatus() => new { };

        public object CaptureUiState() => new { };
    }
}
