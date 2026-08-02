using System.Globalization;
using System.IO;
using RimWorldDevGateway.Client;
using RimWorldDevGateway.Contracts;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayClientSessionProviderTests
{
    [Test]
    public void Load_requires_the_expected_exact_live_PID_and_process_start_identity()
    {
        var root = Path.Combine(TestContext.CurrentContext.WorkDirectory, "gateway-client-session-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var manifestPath = Path.Combine(root, "current.json");
            var processStart = new DateTimeOffset(2026, 8, 1, 10, 0, 0, TimeSpan.Zero);
            File.WriteAllText(
                manifestPath,
                GatewayContractJson.Write(new GatewaySessionManifest
                {
                    ApiVersion = "1",
                    RunId = "run-live",
                    State = "active",
                    BaseUrl = "http://127.0.0.1:43123/api/v1",
                    Token = "secret",
                    ProcessId = 4242,
                    ProcessStartUtc = processStart.ToString("O", CultureInfo.InvariantCulture)
                }));
            var processes = new StubProcessInspector(4242, processStart);
            var provider = new FileGatewaySessionProvider(processes, manifestPath);

            var loaded = provider.Load(manifestPath, expectedProcessId: 4242);

            Assert.That(loaded.RunId, Is.EqualTo("run-live"));
            Assert.That(
                () => provider.Load(manifestPath, expectedProcessId: 4243),
                Throws.TypeOf<GatewayClientException>().With.Message.Contains("PID"));

            processes.StartUtc = processStart.AddSeconds(1);
            Assert.That(
                () => provider.Load(manifestPath, expectedProcessId: 4242),
                Throws.TypeOf<GatewayClientException>().With.Message.Contains("start identity"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class StubProcessInspector : IGatewayProcessInspector
    {
        private readonly int processId;

        public StubProcessInspector(int processId, DateTimeOffset startUtc)
        {
            this.processId = processId;
            StartUtc = startUtc;
        }

        public DateTimeOffset StartUtc { get; set; }

        public DateTimeOffset? TryGetStartUtc(int requestedProcessId) =>
            requestedProcessId == processId ? StartUtc : null;
    }
}
