using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayDeveloperSpawnRouterTests
{
    [Test]
    public void Direct_spawn_route_uses_the_preflighted_idempotent_quickstart_engine()
    {
        var invocationCount = 0;
        IReadOnlyDictionary<string, object?>? captured = null;
        var registry = new GatewayAutomationRegistry(runIdFactory: () => "spawn-run");
        registry.RegisterBuiltIn(
            new GatewayAutomationDescriptor(
                "quickstart.spawn",
                "1",
                "Test spawn automation.",
                new Dictionary<string, object?>(),
                Array.Empty<string>(),
                mutating: true),
            (_, arguments) =>
            {
                invocationCount++;
                captured = arguments;
                return new { SpawnedHandles = new[] { "Steel_19", "Pawn_4" } };
            });
        var dispatcher = new GatewayDispatcher();
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(automations: registry),
            responseTimeout: TimeSpan.FromSeconds(2));
        const string body =
            "{\"arguments\":{\"version\":1," +
            "\"center\":{\"x\":20,\"z\":21}," +
            "\"items\":[{\"defName\":\"Steel\",\"count\":10," +
            "\"offset\":{\"x\":0,\"z\":0}}]," +
            "\"pawns\":[{\"kindDefName\":\"Colonist\",\"count\":1," +
            "\"offset\":{\"x\":2,\"z\":0}}]}," +
            "\"idempotencyKey\":\"developer-spawn-1\"}";

        var first = Dispatch(dispatcher, router, body, "direct-spawn-first");
        var second = Dispatch(dispatcher, router, body, "direct-spawn-replay");
        var responseBody = Encoding.UTF8.GetString(first.Body);

        Assert.Multiple(() =>
        {
            Assert.That(first.StatusCode, Is.EqualTo(200), responseBody);
            Assert.That(second.StatusCode, Is.EqualTo(200));
            Assert.That(responseBody, Does.Contain("quickstart.spawn"));
            Assert.That(responseBody, Does.Contain("Steel_19"));
            Assert.That(invocationCount, Is.EqualTo(1));
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!["items"], Is.InstanceOf<object[]>());
        });
    }

    private static GatewayHttpResponse Dispatch(
        GatewayDispatcher dispatcher,
        GatewayApiRouter router,
        string body,
        string requestId)
    {
        var request = new GatewayHttpRequest(
            "POST",
            "/api/v1/dev-tools/spawn",
            "/api/v1/dev-tools/spawn",
            string.Empty,
            new Dictionary<string, string>(),
            Encoding.UTF8.GetBytes(body));
        var responseTask = Task.Run(() => router.Handle(request, requestId));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.Update);
        return responseTask.GetAwaiter().GetResult();
    }

    private sealed class StubStateProvider : IGatewayStateProvider
    {
        public object CaptureStatus() => new { };

        public object CaptureUiState() => new { };
    }
}
