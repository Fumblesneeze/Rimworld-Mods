using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayLoopbackServerTests
{
    [Test]
    public void Request_gate_rejects_saturation_and_reuses_only_released_capacity()
    {
        var gate = new GatewayRequestGate(2);
        var first = gate.TryEnter();
        var second = gate.TryEnter();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(gate.ActiveCount, Is.EqualTo(2));
            Assert.That(gate.TryEnter(), Is.Null);
        });

        first!.Dispose();
        first.Dispose();
        using var replacement = gate.TryEnter();
        Assert.Multiple(() =>
        {
            Assert.That(replacement, Is.Not.Null);
            Assert.That(gate.ActiveCount, Is.EqualTo(2));
        });

        second!.Dispose();
        replacement!.Dispose();
        Assert.That(gate.ActiveCount, Is.Zero);
    }

    [Test]
    public void Transport_errors_are_valid_common_envelopes_and_oversized_responses_are_replaced()
    {
        var invalidMessage = "bad\tvalue\u0001\n";
        var error = GatewayTransportResponsePolicy.Error(
            503,
            "Service Unavailable",
            "transport-test",
            "gateway_busy",
            invalidMessage,
            durationMilliseconds: 7,
            retryable: true);
        var json = Encoding.UTF8.GetString(error.Body);
        var oversized = new GatewayHttpResponse(200, "OK", "text/plain", new byte[1025]);
        var bounded = GatewayTransportResponsePolicy.EnforceLimit(
            oversized,
            maximumResponseBytes: 1024,
            requestId: "bounded-test",
            durationMilliseconds: 9);
        var boundedJson = Encoding.UTF8.GetString(bounded.Body);

        Assert.Multiple(() =>
        {
            Assert.That(error.StatusCode, Is.EqualTo(503));
            Assert.That(json, Does.Contain("\"apiVersion\":\"1\""));
            Assert.That(json, Does.Contain("\"durationMs\":7"));
            Assert.That(json, Does.Contain("\"requestId\":\"transport-test\""));
            Assert.That(json, Does.Contain("\"retryable\":true"));
            Assert.That(json, Does.Not.Contain("\u0001"));
            Assert.That(bounded.StatusCode, Is.EqualTo(500));
            Assert.That(boundedJson, Does.Contain("response_too_large"));
            Assert.That(boundedJson, Does.Contain("\"requestId\":\"bounded-test\""));
        });
    }

    [Test]
    public void Transport_logging_sets_the_correlated_request_scope_and_restores_the_caller_scope()
    {
        GatewayRequestScope.CurrentRequestId = "outer-request";
        string? observed = null;
        try
        {
            GatewayLoopbackServer.LogWithRequestScope(
                "transport-request",
                () => observed = GatewayRequestScope.CurrentRequestId);

            Assert.Multiple(() =>
            {
                Assert.That(observed, Is.EqualTo("transport-request"));
                Assert.That(GatewayRequestScope.CurrentRequestId, Is.EqualTo("outer-request"));
            });
        }
        finally
        {
            GatewayRequestScope.CurrentRequestId = null;
        }
    }

    [Test]
    public void Server_uses_EmbedIO_managed_mode_on_IPv4_loopback_and_admits_only_authenticated_non_browser_requests()
    {
        RequireUnityMonoManagedListener();
        const string token = "server-test-token";
        var handled = 0;
        using var server = new GatewayLoopbackServer(
            token,
            (request, requestId) =>
            {
                Interlocked.Increment(ref handled);
                return new GatewayHttpResponse(
                    200,
                    "OK",
                    "application/json; charset=utf-8",
                    Encoding.UTF8.GetBytes("{\"ok\":true}"));
            });

        var port = server.Start(preferredPort: 0);

        var authorized = Send(
            port,
            "GET",
            "/api/v1/status",
            token,
            requestId: "test-request");
        var wrongToken = Send(
            port,
            "GET",
            "/api/v1/status",
            "wrong-token");
        var browserOrigin = Send(
            port,
            "GET",
            "/api/v1/status",
            token,
            origin: "https://attacker.example");

        Assert.Multiple(() =>
        {
            Assert.That(server.BoundAddress, Is.EqualTo(IPAddress.Loopback));
            Assert.That(server.TransportName, Is.EqualTo("EmbedIO managed listener"));
            Assert.That(authorized.StatusCode, Is.EqualTo(200));
            Assert.That(authorized.ReasonPhrase, Is.EqualTo("OK"));
            Assert.That(authorized.Headers["X-Request-Id"], Is.EqualTo("test-request"));
            Assert.That(wrongToken.StatusCode, Is.EqualTo(401));
            Assert.That(browserOrigin.StatusCode, Is.EqualTo(403));
            Assert.That(handled, Is.EqualTo(1));
        });

        using var ipv6Probe = new TcpClient(AddressFamily.InterNetworkV6);
        Assert.That(
            () => ipv6Probe.Connect(IPAddress.IPv6Loopback, port),
            Throws.TypeOf<SocketException>());
    }

    [Test]
    public void Server_rejects_oversized_headers_and_declared_bodies_before_router_dispatch()
    {
        RequireUnityMonoManagedListener();
        const string token = "server-test-token";
        var handled = 0;
        using var server = new GatewayLoopbackServer(
            token,
            (_, _) =>
            {
                Interlocked.Increment(ref handled);
                return GatewayHttpResponse.Empty(204, "No Content");
            });
        var port = server.Start(0);

        var oversizedHeaders = Send(
            port,
            "GET",
            "/api/v1/status",
            token,
            customHeaderValue: new string('x', GatewayLoopbackServer.MaximumHeaderBytes));
        var oversizedBody = SendDeclaredOversizedBody(port, token);

        Assert.Multiple(() =>
        {
            Assert.That(oversizedHeaders.StatusCode, Is.EqualTo(431));
            Assert.That(oversizedHeaders.ReasonPhrase, Is.EqualTo("Request Header Fields Too Large"));
            Assert.That(oversizedBody.StatusCode, Is.EqualTo(413));
            Assert.That(oversizedBody.ReasonPhrase, Is.EqualTo("Payload Too Large"));
            Assert.That(handled, Is.Zero);
        });
    }

    [Test]
    public void Server_delivers_the_exact_path_query_headers_and_body_to_the_router()
    {
        RequireUnityMonoManagedListener();
        const string token = "server-test-token";
        GatewayHttpRequest? captured = null;
        using var server = new GatewayLoopbackServer(
            token,
            (request, _) =>
            {
                captured = request;
                return GatewayHttpResponse.Empty(204, "No Content");
            });
        var port = server.Start(0);
        var body = Encoding.UTF8.GetBytes("{\"value\":42}");

        var response = Send(
            port,
            "POST",
            "/api/v1/automations/run?name=quickstart.spawn&count=2",
            token,
            customHeaderValue: "preserved",
            body: body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(204));
            Assert.That(response.ReasonPhrase, Is.EqualTo("No Content"));
            Assert.That(captured, Is.Not.Null);
            Assert.That(captured!.Method, Is.EqualTo("POST"));
            Assert.That(captured.Target, Is.EqualTo("/api/v1/automations/run?name=quickstart.spawn&count=2"));
            Assert.That(captured.Path, Is.EqualTo("/api/v1/automations/run"));
            Assert.That(captured.Query, Is.EqualTo("name=quickstart.spawn&count=2"));
            Assert.That(captured.Headers["x-custom"], Is.EqualTo("preserved"));
            Assert.That(captured.Body, Is.EqualTo(body));
            Assert.That(captured.CancellationToken.CanBeCanceled, Is.True);
        });
    }

    [Test]
    public void Stop_closes_the_listener_owned_by_the_server()
    {
        using var server = new GatewayLoopbackServer(
            "server-test-token",
            (_, _) => GatewayHttpResponse.Empty(204, "No Content"));
        var port = server.Start(0);

        server.Stop();

        Assert.That(server.IsRunning, Is.False);
        using var probe = new TcpClient();
        Assert.That(
            () => probe.Connect(IPAddress.Loopback, port),
            Throws.TypeOf<SocketException>());
    }

    [Test]
    public void Stop_timeout_retains_the_owned_host_and_allows_cleanup_retry_before_restart()
    {
        var host = new ControllableLoopbackHost();
        var server = new GatewayLoopbackServer(
            "server-test-token",
            (_, _) => GatewayHttpResponse.Empty(204, "No Content"),
            maximumConcurrentRequests: 8,
            maximumResponseBytes: GatewayLoopbackServer.MaximumResponseBytes,
            hostFactory: (_, _) => host,
            startupTimeout: TimeSpan.FromMilliseconds(100),
            shutdownTimeout: TimeSpan.FromMilliseconds(10));
        try
        {
            server.Start(40106);

            Assert.That(
                () => server.Stop(),
                Throws.TypeOf<TimeoutException>()
                    .With.Message.Contains("did not stop"));
            Assert.Multiple(() =>
            {
                Assert.That(host.CancellationRequested, Is.True);
                Assert.That(host.DisposeCount, Is.EqualTo(1));
                Assert.That(
                    () => server.Start(40107),
                    Throws.TypeOf<InvalidOperationException>()
                        .With.Message.Contains("already started"));
            });

            host.Complete();
            server.Stop();
            server.Stop();

            Assert.Multiple(() =>
            {
                Assert.That(server.IsRunning, Is.False);
                Assert.That(host.DisposeCount, Is.EqualTo(2));
            });
        }
        finally
        {
            host.Complete();
            server.Dispose();
        }
    }

    private static TestResponse Send(
        int port,
        string method,
        string target,
        string token,
        string? requestId = null,
        string? origin = null,
        string? customHeaderValue = null,
        byte[]? body = null)
    {
        var request = (HttpWebRequest)WebRequest.Create($"http://127.0.0.1:{port}{target}");
        request.Method = method;
        request.KeepAlive = false;
        request.Proxy = null;
        request.Timeout = 5000;
        request.ReadWriteTimeout = 5000;
        request.Headers[HttpRequestHeader.Authorization] = "Bearer " + token;
        if (requestId is not null)
        {
            request.Headers["X-Request-Id"] = requestId;
        }

        if (origin is not null)
        {
            request.Headers["Origin"] = origin;
        }

        if (customHeaderValue is not null)
        {
            request.Headers["X-Custom"] = customHeaderValue;
        }

        if (body is not null)
        {
            request.ContentLength = body.Length;
            using var requestStream = request.GetRequestStream();
            requestStream.Write(body, 0, body.Length);
        }

        try
        {
            return ReadResponse((HttpWebResponse)request.GetResponse());
        }
        catch (WebException exception) when (exception.Response is HttpWebResponse errorResponse)
        {
            return ReadResponse(errorResponse);
        }
    }

    private static TestResponse SendDeclaredOversizedBody(int port, string token)
    {
        using var client = new TcpClient();
        client.ReceiveTimeout = 5000;
        client.SendTimeout = 5000;
        client.Connect(IPAddress.Loopback, port);
        using var stream = client.GetStream();
        var request =
            "POST /api/v1/status HTTP/1.1\r\n" +
            "Host: 127.0.0.1:" + port + "\r\n" +
            "Authorization: Bearer " + token + "\r\n" +
            "Content-Length: " + (GatewayLoopbackServer.MaximumBodyBytes + 1L) + "\r\n\r\n";
        var bytes = Encoding.ASCII.GetBytes(request);
        stream.Write(bytes, 0, bytes.Length);
        stream.Flush();

        using var reader = new StreamReader(stream, Encoding.ASCII, false, 1024, leaveOpen: true);
        var statusLine = reader.ReadLine() ?? throw new IOException("The server returned no status line.");
        var statusParts = statusLine.Split(new[] { ' ' }, 3);
        return new TestResponse(
            int.Parse(statusParts[1], System.Globalization.CultureInfo.InvariantCulture),
            statusParts[2],
            new WebHeaderCollection(),
            Array.Empty<byte>());
    }

    private static TestResponse ReadResponse(HttpWebResponse response)
    {
        using (response)
        using (var stream = response.GetResponseStream())
        using (var destination = new MemoryStream())
        {
            stream?.CopyTo(destination);
            return new TestResponse(
                (int)response.StatusCode,
                response.StatusDescription,
                response.Headers,
                destination.ToArray());
        }
    }

    private static void RequireUnityMonoManagedListener()
    {
        if (Type.GetType("Mono.Runtime") is null)
        {
            Assert.Ignore(
                "EmbedIO 3.5.2's managed listener does not dispatch requests under Microsoft's net48 CLR. " +
                "Unity Mono request handling is covered by scripts/Invoke-GatewaySmoke.ps1.");
        }
    }

    private sealed class TestResponse
    {
        public TestResponse(int statusCode, string reasonPhrase, WebHeaderCollection headers, byte[] body)
        {
            StatusCode = statusCode;
            ReasonPhrase = reasonPhrase;
            Headers = headers;
            Body = body;
        }

        public int StatusCode { get; }

        public string ReasonPhrase { get; }

        public WebHeaderCollection Headers { get; }

        public byte[] Body { get; }
    }

    private sealed class ControllableLoopbackHost : IGatewayLoopbackHost
    {
        private readonly TaskCompletionSource<object?> completion = new();

        public bool IsListening { get; private set; } = true;

        public bool CancellationRequested { get; private set; }

        public int DisposeCount { get; private set; }

        public Task RunAsync(CancellationToken cancellationToken)
        {
            cancellationToken.Register(() => CancellationRequested = true);
            return completion.Task;
        }

        public void Complete()
        {
            completion.TrySetResult(null);
        }

        public void Dispose()
        {
            DisposeCount++;
            IsListening = false;
        }
    }
}
