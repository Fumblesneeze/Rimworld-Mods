using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EmbedIO;
using EmbedIO.Actions;

namespace RimWorldDevGateway;

internal interface IGatewayLoopbackHost : IDisposable
{
    bool IsListening { get; }

    Task RunAsync(CancellationToken cancellationToken);
}

internal interface IGatewayTransportLogTarget
{
    void AttachLogBuffer(GatewayLogBuffer buffer);
}

internal delegate IGatewayLoopbackHost GatewayLoopbackHostFactory(
    string prefix,
    RequestHandlerCallback handler);

public sealed class GatewayLoopbackServer : IGatewayTransport, IGatewayTransportLogTarget
{
    public const int MaximumHeaderBytes = 16 * 1024;
    public const int MaximumBodyBytes = 32 * 1024 * 1024;
    public const int MaximumResponseBytes = 64 * 1024 * 1024;

    private static readonly TimeSpan DefaultStartupTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan DefaultShutdownTimeout = TimeSpan.FromSeconds(5);

    private readonly string bearerToken;
    private readonly Func<GatewayHttpRequest, string, GatewayHttpResponse> handler;
    private readonly GatewayRequestGate requestGate;
    private readonly int maximumResponseBytes;
    private readonly GatewayLoopbackHostFactory hostFactory;
    private readonly TimeSpan startupTimeout;
    private readonly TimeSpan shutdownTimeout;
    private readonly object lifecycleLock = new();
    private IGatewayLoopbackHost? webServer;
    private CancellationTokenSource? runCancellation;
    private Task? runTask;
    private GatewayLogBuffer? logBuffer;
    private bool disposed;

    public GatewayLoopbackServer(
        string bearerToken,
        Func<GatewayHttpRequest, string, GatewayHttpResponse> handler,
        int maximumConcurrentRequests = 8,
        int maximumResponseBytes = MaximumResponseBytes)
        : this(
            bearerToken,
            handler,
            maximumConcurrentRequests,
            maximumResponseBytes,
            CreateEmbedIoHost,
            DefaultStartupTimeout,
            DefaultShutdownTimeout)
    {
    }

    internal GatewayLoopbackServer(
        string bearerToken,
        Func<GatewayHttpRequest, string, GatewayHttpResponse> handler,
        int maximumConcurrentRequests,
        int maximumResponseBytes,
        GatewayLoopbackHostFactory hostFactory,
        TimeSpan startupTimeout,
        TimeSpan shutdownTimeout)
    {
        if (string.IsNullOrWhiteSpace(bearerToken))
        {
            throw new ArgumentException("A bearer token is required.", nameof(bearerToken));
        }

        if (handler is null)
        {
            throw new ArgumentNullException(nameof(handler));
        }

        if (maximumConcurrentRequests <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumConcurrentRequests));
        }

        if (maximumResponseBytes < 1024)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumResponseBytes));
        }

        if (hostFactory is null)
        {
            throw new ArgumentNullException(nameof(hostFactory));
        }

        if (startupTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(startupTimeout));
        }

        if (shutdownTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(shutdownTimeout));
        }

        this.bearerToken = bearerToken;
        this.handler = handler;
        requestGate = new GatewayRequestGate(maximumConcurrentRequests);
        this.maximumResponseBytes = maximumResponseBytes;
        this.hostFactory = hostFactory;
        this.startupTimeout = startupTimeout;
        this.shutdownTimeout = shutdownTimeout;
    }

    public IPAddress? BoundAddress { get; private set; }

    public int Port { get; private set; }

    public string TransportName => "EmbedIO managed listener";

    public bool IsRunning
    {
        get
        {
            lock (lifecycleLock)
            {
                return webServer?.IsListening == true;
            }
        }
    }

    internal void AttachLogBuffer(GatewayLogBuffer buffer)
    {
        if (buffer is null)
        {
            throw new ArgumentNullException(nameof(buffer));
        }

        lock (lifecycleLock)
        {
            ThrowIfDisposed();
            if (webServer is not null)
            {
                throw new InvalidOperationException(
                    "Transport diagnostics must be attached before the listener starts.");
            }

            logBuffer = buffer;
        }
    }

    void IGatewayTransportLogTarget.AttachLogBuffer(GatewayLogBuffer buffer) =>
        AttachLogBuffer(buffer);

    public int Start(int preferredPort = 0)
    {
        if (preferredPort is < 0 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(preferredPort));
        }

        lock (lifecycleLock)
        {
            ThrowIfDisposed();
            if (webServer is not null)
            {
                throw new InvalidOperationException("The gateway server is already started.");
            }

            var selectedPort = preferredPort == 0 ? FindAvailableLoopbackPort() : preferredPort;
            var prefix = $"http://127.0.0.1:{selectedPort}/";
            var candidate = hostFactory(prefix, HandleContextAsync) ??
                throw new InvalidOperationException("The gateway loopback-host factory returned null.");
            var cancellation = new CancellationTokenSource();
            Task candidateTask;

            try
            {
                candidateTask = Task.Run(
                    () => candidate.RunAsync(cancellation.Token),
                    CancellationToken.None);
                WaitUntilListening(candidate, candidateTask, startupTimeout);
            }
            catch
            {
                cancellation.Cancel();
                candidate.Dispose();
                cancellation.Dispose();
                throw;
            }

            webServer = candidate;
            runCancellation = cancellation;
            runTask = candidateTask;
            BoundAddress = IPAddress.Loopback;
            Port = selectedPort;
            return Port;
        }
    }

    public void Stop()
    {
        lock (lifecycleLock)
        {
            var activeServer = webServer;
            if (activeServer is null)
            {
                return;
            }

            var cancellation = runCancellation!;
            var activeTask = runTask!;
            cancellation.Cancel();
            activeServer.Dispose();
            WaitForShutdown(activeTask, shutdownTimeout);
            cancellation.Dispose();
            webServer = null;
            runCancellation = null;
            runTask = null;
        }
    }

    public void Dispose()
    {
        lock (lifecycleLock)
        {
            disposed = true;
        }

        Stop();
    }

    private async Task HandleContextAsync(IHttpContext context)
    {
        var requestId = CreateRequestId();
        var stopwatch = Stopwatch.StartNew();
        var admission = requestGate.TryEnter();
        if (admission is null)
        {
            AppendTransportLog(
                "Warning",
                requestId,
                $"[RimWorldDevGateway] Request {requestId} rejected: concurrent request capacity reached.");
            var busy = GatewayTransportResponsePolicy.Error(
                503,
                "Service Unavailable",
                requestId,
                "gateway_busy",
                "The gateway is at its concurrent-request capacity.",
                stopwatch.ElapsedMilliseconds,
                retryable: true);
            await WriteResponseAsync(context, busy, requestId).ConfigureAwait(false);
            context.SetHandled();
            return;
        }

        using (admission)
        {
        GatewayHttpResponse response;

        try
        {
            if (EstimateHeaderBytes(context.Request) > MaximumHeaderBytes)
            {
                response = GatewayTransportResponsePolicy.Error(
                    431,
                    "Request Header Fields Too Large",
                    requestId,
                    "request_headers_too_large",
                    "Request headers exceed the 16 KiB limit.",
                    stopwatch.ElapsedMilliseconds);
            }
            else if (context.Request.Headers["Origin"] is not null)
            {
                response = GatewayTransportResponsePolicy.Error(
                    403,
                    "Forbidden",
                    requestId,
                    "browser_origin_rejected",
                    "Browser Origin requests are not accepted.",
                    stopwatch.ElapsedMilliseconds);
            }
            else if (!TryResolveRequestId(
                         context.Request,
                         ref requestId,
                         stopwatch.ElapsedMilliseconds,
                         out response))
            {
                // The helper supplies the validation response.
            }
            else if (!GatewayAuthenticator.Authorize(
                         context.RemoteEndPoint,
                         context.Request.Headers["Authorization"],
                         bearerToken))
            {
                response = GatewayTransportResponsePolicy.Error(
                    401,
                    "Unauthorized",
                    requestId,
                    "unauthorized",
                    "A valid per-run bearer token is required.",
                    stopwatch.ElapsedMilliseconds);
            }
            else if (context.Request.ContentLength64 > MaximumBodyBytes)
            {
                response = GatewayTransportResponsePolicy.Error(
                    413,
                    "Payload Too Large",
                    requestId,
                    "request_too_large",
                    "Request body exceeds the 32 MiB limit.",
                    stopwatch.ElapsedMilliseconds);
            }
            else
            {
                var request = await CreateRequestAsync(context).ConfigureAwait(false);
                response = handler(request, requestId);
            }
        }
        catch (RequestBodyTooLargeException)
        {
            response = GatewayTransportResponsePolicy.Error(
                413,
                "Payload Too Large",
                requestId,
                "request_too_large",
                "Request body exceeds the 32 MiB limit.",
                stopwatch.ElapsedMilliseconds);
        }
        catch (IOException exception)
        {
            response = GatewayTransportResponsePolicy.Error(
                408,
                "Request Timeout",
                requestId,
                "io_timeout",
                exception.Message,
                stopwatch.ElapsedMilliseconds,
                retryable: true);
        }
        catch (OperationCanceledException) when (context.CancellationToken.IsCancellationRequested)
        {
            AppendTransportLog(
                "Warning",
                requestId,
                $"[RimWorldDevGateway] Request {requestId} was cancelled by the HTTP client.");
            return;
        }
        catch (Exception exception)
        {
            AppendTransportLog(
                "Error",
                requestId,
                $"[RimWorldDevGateway] Request {requestId} failed: {exception.Message}",
                exception.ToString());
            response = GatewayTransportResponsePolicy.Error(
                500,
                "Internal Server Error",
                requestId,
                "internal_error",
                exception.Message,
                stopwatch.ElapsedMilliseconds);
        }

        response = GatewayTransportResponsePolicy.EnforceLimit(
            response,
            maximumResponseBytes,
            requestId,
            stopwatch.ElapsedMilliseconds);
        RecordRequestCompleted(
            requestId,
            context.Request.HttpMethod,
            context.Request.Url.AbsolutePath,
            response.StatusCode,
            stopwatch.ElapsedMilliseconds);
        await WriteResponseAsync(context, response, requestId).ConfigureAwait(false);
        context.SetHandled();
        }
    }

    internal static void LogWithRequestScope(string requestId, Action log)
    {
        if (log is null)
        {
            throw new ArgumentNullException(nameof(log));
        }

        var previousRequestId = GatewayRequestScope.CurrentRequestId;
        try
        {
            GatewayRequestScope.CurrentRequestId = requestId;
            log();
        }
        finally
        {
            GatewayRequestScope.CurrentRequestId = previousRequestId;
        }
    }

    private void AppendTransportLog(
        string severity,
        string requestId,
        string message,
        string? stack = null)
    {
        logBuffer?.Append(
            severity,
            message,
            stack,
            Thread.CurrentThread.ManagedThreadId.ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            requestId);
    }

    internal void RecordRequestCompleted(
        string requestId,
        string method,
        string path,
        int statusCode,
        long elapsedMilliseconds)
    {
        AppendTransportLog(
            "Message",
            requestId,
            $"[RimWorldDevGateway] Request {requestId} {method} {path} completed " +
            $"with HTTP {statusCode} in {elapsedMilliseconds} ms.");
    }

    private static async Task<GatewayHttpRequest> CreateRequestAsync(IHttpContext context)
    {
        var target = context.Request.RawUrl ?? context.Request.Url.PathAndQuery;
        var queryIndex = target.IndexOf('?');
        var path = queryIndex < 0 ? target : target.Substring(0, queryIndex);
        var query = queryIndex < 0 ? string.Empty : target.Substring(queryIndex + 1);
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in context.Request.Headers.AllKeys)
        {
            if (name is not null)
            {
                headers.Add(name, context.Request.Headers[name] ?? string.Empty);
            }
        }

        var body = await ReadBodyAsync(context.Request, context.CancellationToken).ConfigureAwait(false);
        return new GatewayHttpRequest(
            context.Request.HttpMethod,
            target,
            path,
            query,
            headers,
            body,
            context.CancellationToken);
    }

    private static async Task<byte[]> ReadBodyAsync(IHttpRequest request, CancellationToken cancellationToken)
    {
        if (!request.HasEntityBody)
        {
            return Array.Empty<byte>();
        }

        var initialCapacity = request.ContentLength64 is > 0 and <= MaximumBodyBytes
            ? (int)request.ContentLength64
            : 0;
        using var destination = new MemoryStream(initialCapacity);
        var buffer = new byte[16 * 1024];
        var remaining = request.ContentLength64;
        while (remaining != 0)
        {
            var requestedBytes = remaining > 0
                ? (int)Math.Min(buffer.Length, remaining)
                : buffer.Length;
            var read = await request.InputStream
                .ReadAsync(buffer, 0, requestedBytes, cancellationToken)
                .ConfigureAwait(false);
            if (read == 0)
            {
                if (remaining > 0)
                {
                    throw new IOException("The request ended before its declared body was complete.");
                }

                break;
            }

            if (destination.Length + read > MaximumBodyBytes)
            {
                throw new RequestBodyTooLargeException();
            }

            destination.Write(buffer, 0, read);
            if (remaining > 0)
            {
                remaining -= read;
            }
        }

        return destination.ToArray();
    }

    private static async Task WriteResponseAsync(
        IHttpContext context,
        GatewayHttpResponse response,
        string requestId)
    {
        context.Response.StatusCode = response.StatusCode;
        context.Response.StatusDescription = response.ReasonPhrase;
        context.Response.ContentType = response.ContentType;
        context.Response.ContentLength64 = response.Body.Length;
        context.Response.KeepAlive = false;
        context.Response.SendChunked = false;
        context.Response.Headers["X-Request-Id"] = requestId;
        if (response.Body.Length > 0)
        {
            await context.Response.OutputStream
                .WriteAsync(response.Body, 0, response.Body.Length, context.CancellationToken)
                .ConfigureAwait(false);
        }

    }

    private static long EstimateHeaderBytes(IHttpRequest request)
    {
        long total = Encoding.UTF8.GetByteCount(request.HttpMethod) +
                     Encoding.UTF8.GetByteCount(request.RawUrl ?? string.Empty) +
                     "  HTTP/1.1\r\n\r\n".Length;
        foreach (var name in request.Headers.AllKeys)
        {
            if (name is null)
            {
                continue;
            }

            var values = request.Headers.GetValues(name) ?? Array.Empty<string>();
            foreach (var value in values)
            {
                total += Encoding.UTF8.GetByteCount(name) + 2L +
                         Encoding.UTF8.GetByteCount(value ?? string.Empty) + 2L;
                if (total > MaximumHeaderBytes)
                {
                    return total;
                }
            }
        }

        return total;
    }

    private static bool TryResolveRequestId(
        IHttpRequest request,
        ref string requestId,
        long durationMilliseconds,
        out GatewayHttpResponse response)
    {
        var supplied = request.Headers["X-Request-Id"];
        if (supplied is null)
        {
            response = null!;
            return true;
        }

        if (!IsValidRequestId(supplied))
        {
            response = GatewayTransportResponsePolicy.Error(
                400,
                "Bad Request",
                requestId,
                "invalid_request_id",
                "X-Request-Id is invalid.",
                durationMilliseconds);
            return false;
        }

        requestId = supplied;
        response = null!;
        return true;
    }

    private static string CreateRequestId() => Guid.NewGuid().ToString("N");

    private static bool IsValidRequestId(string value)
    {
        if (value.Length is < 1 or > 64)
        {
            return false;
        }

        foreach (var character in value)
        {
            if (!char.IsLetterOrDigit(character) && character is not '-' and not '_' and not '.' and not ':')
            {
                return false;
            }
        }

        return true;
    }

    private static int FindAvailableLoopbackPort()
    {
        using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
        return ((IPEndPoint)socket.LocalEndPoint!).Port;
    }

    private static void WaitUntilListening(
        IGatewayLoopbackHost server,
        Task candidateTask,
        TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (!server.IsListening)
        {
            if (candidateTask.IsFaulted)
            {
                candidateTask.GetAwaiter().GetResult();
            }

            if (candidateTask.IsCompleted)
            {
                throw new InvalidOperationException("EmbedIO stopped before the gateway listener became ready.");
            }

            if (DateTime.UtcNow >= deadline)
            {
                throw new TimeoutException("EmbedIO did not start the gateway listener within the startup timeout.");
            }

            Thread.Sleep(10);
        }
    }

    private static void WaitForShutdown(Task activeTask, TimeSpan timeout)
    {
        try
        {
            if (!activeTask.Wait(timeout))
            {
                throw new TimeoutException("EmbedIO did not stop the gateway listener within the shutdown timeout.");
            }
        }
        catch (AggregateException exception)
            when (exception.InnerExceptions.All(item => item is OperationCanceledException))
        {
        }
    }

    private void ThrowIfDisposed()
    {
        if (disposed)
        {
            throw new ObjectDisposedException(nameof(GatewayLoopbackServer));
        }
    }

    private static IGatewayLoopbackHost CreateEmbedIoHost(
        string prefix,
        RequestHandlerCallback handler)
    {
        return new EmbedIoGatewayLoopbackHost(prefix, handler);
    }

    private sealed class EmbedIoGatewayLoopbackHost : IGatewayLoopbackHost
    {
        private readonly WebServer server;

        public EmbedIoGatewayLoopbackHost(string prefix, RequestHandlerCallback handler)
        {
            server = new WebServer(options => options
                .WithUrlPrefix(prefix)
                .WithEmbedIOHttpListener());
            server.Modules.Add("gateway-api", new ActionModule(handler));
        }

        public bool IsListening => server.State == WebServerState.Listening;

        public Task RunAsync(CancellationToken cancellationToken)
        {
            return server.RunAsync(cancellationToken);
        }

        public void Dispose()
        {
            server.Dispose();
        }
    }

    private sealed class RequestBodyTooLargeException : Exception
    {
    }
}
