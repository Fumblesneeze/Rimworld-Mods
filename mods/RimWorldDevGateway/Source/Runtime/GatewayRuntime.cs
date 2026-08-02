using System.Threading;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway;

public interface IGatewayTransport : IDisposable
{
    bool IsRunning { get; }

    int Port { get; }

    int Start(int preferredPort = 0);

    void Stop();
}

public delegate IGatewayTransport GatewayTransportFactory(
    string bearerToken,
    Func<GatewayHttpRequest, string, GatewayHttpResponse> handler);

public delegate IDisposable GatewayLogSubscriptionFactory(GatewayLogBuffer buffer);

public delegate GatewayApiRouter GatewayRouterFactory(
    GatewayDispatcher dispatcher,
    IGatewayStateProvider stateProvider,
    GatewayLogBuffer logBuffer,
    Action requestShutdown);

public sealed class GatewayRuntimeIdentity
{
    public GatewayRuntimeIdentity(
        int processId,
        DateTimeOffset processStartUtc,
        string gameVersion,
        string modVersion)
    {
        if (processId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(processId));
        }

        ProcessId = processId;
        ProcessStartUtc = processStartUtc.ToUniversalTime();
        GameVersion = gameVersion ?? string.Empty;
        ModVersion = modVersion ?? string.Empty;
    }

    public int ProcessId { get; }

    public DateTimeOffset ProcessStartUtc { get; }

    public string GameVersion { get; }

    public string ModVersion { get; }
}

public sealed class GatewayRuntime : IDisposable
{
    private const int Created = 0;
    private const int Starting = 1;
    private const int Running = 2;
    private const int Stopping = 3;
    private const int Stopped = 4;

    private readonly object lifecycleSync = new();
    private readonly GatewayRuntimeIdentity identity;
    private readonly GatewaySessionManager sessionManager;
    private readonly GatewayDispatcher dispatcher;
    private readonly Func<GatewayDispatcher, IGatewayStateProvider> stateProviderFactory;
    private readonly GatewayTransportFactory transportFactory;
    private readonly GatewayLogSubscriptionFactory? logSubscriptionFactory;
    private readonly GatewayRouterFactory? routerFactory;
    private readonly int preferredPort;
    private readonly int mainThreadId;
    private int state;
    private int shutdownRequested;
    private GatewaySessionLease? sessionLease;
    private GatewaySessionManifest? activeManifest;
    private GatewayLogBuffer? logBuffer;
    private GatewayRequestJournal? requestJournal;
    private IDisposable? logSubscription;
    private IGatewayTransport? transport;
    private bool transportStopped;

    public GatewayRuntime(
        GatewayRuntimeIdentity identity,
        GatewaySessionManager sessionManager,
        GatewayDispatcher dispatcher,
        Func<GatewayDispatcher, IGatewayStateProvider> stateProviderFactory,
        GatewayTransportFactory transportFactory,
        GatewayLogSubscriptionFactory? logSubscriptionFactory = null,
        int preferredPort = 0,
        int? mainThreadId = null,
        GatewayRouterFactory? routerFactory = null)
    {
        this.identity = identity ?? throw new ArgumentNullException(nameof(identity));
        this.sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
        this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        this.stateProviderFactory = stateProviderFactory ?? throw new ArgumentNullException(nameof(stateProviderFactory));
        this.transportFactory = transportFactory ?? throw new ArgumentNullException(nameof(transportFactory));
        this.logSubscriptionFactory = logSubscriptionFactory;
        this.routerFactory = routerFactory;
        if (preferredPort is < 0 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(preferredPort));
        }

        this.preferredPort = preferredPort;
        this.mainThreadId = mainThreadId ?? Thread.CurrentThread.ManagedThreadId;
    }

    public GatewayDispatcher Dispatcher => dispatcher;

    public GatewayLogBuffer? LogBuffer => logBuffer;

    public bool IsRunning => Volatile.Read(ref state) == Running;

    public bool IsStopped => Volatile.Read(ref state) == Stopped;

    public bool IsShutdownRequested => Volatile.Read(ref shutdownRequested) != 0;

    public int Port => transport?.Port ?? 0;

    public GatewaySessionManifest Start()
    {
        EnsureMainThread();
        lock (lifecycleSync)
        {
            if (state == Running)
            {
                return activeManifest!;
            }

            if (state != Created)
            {
                throw new InvalidOperationException("The gateway runtime cannot be started from its current lifecycle state.");
            }

            Volatile.Write(ref state, Starting);
            try
            {
                sessionLease = sessionManager.Prepare(
                    identity.ProcessId,
                    identity.ProcessStartUtc,
                    identity.GameVersion,
                    identity.ModVersion);
                logBuffer = new GatewayLogBuffer(bearerToken: sessionLease.Token);
                var stateProvider = stateProviderFactory(dispatcher) ??
                    throw new InvalidOperationException("The gateway state-provider factory returned null.");
                var router = routerFactory?.Invoke(dispatcher, stateProvider, logBuffer, RequestShutdown) ??
                    new GatewayApiRouter(dispatcher, stateProvider, logBuffer);
                requestJournal = sessionManager.CreateRequestJournal(sessionLease);
                logSubscription = logSubscriptionFactory?.Invoke(logBuffer);
                transport = transportFactory(
                    sessionLease.Token,
                    (request, requestId) => requestJournal.Track(request, requestId, router.Handle)) ??
                    throw new InvalidOperationException("The gateway transport factory returned null.");
                if (transport is IGatewayTransportLogTarget transportLogTarget)
                {
                    transportLogTarget.AttachLogBuffer(logBuffer);
                }
                var boundPort = transport.Start(preferredPort);
                activeManifest = sessionLease.Publish(boundPort);
                Volatile.Write(ref state, Running);
                return activeManifest;
            }
            catch
            {
                var cleaned = Cleanup(throwOnFailure: false);
                Volatile.Write(ref state, cleaned ? Stopped : Stopping);
                throw;
            }
        }
    }

    public int Drain(DispatchPhase phase, int maximumItems = 1)
    {
        EnsureMainThread();
        return IsRunning ? dispatcher.Drain(phase, maximumItems) : 0;
    }

    public void RequestShutdown()
    {
        Interlocked.Exchange(ref shutdownRequested, 1);
    }

    public void Stop()
    {
        lock (lifecycleSync)
        {
            if (state == Stopped)
            {
                return;
            }

            Volatile.Write(ref state, Stopping);
            var cleaned = false;
            try
            {
                cleaned = Cleanup(throwOnFailure: true);
            }
            finally
            {
                if (cleaned)
                {
                    Volatile.Write(ref state, Stopped);
                }
            }
        }
    }

    public void Dispose()
    {
        Stop();
    }

    private bool Cleanup(bool throwOnFailure)
    {
        List<Exception>? failures = null;
        TryCleanup(() => dispatcher.Stop(), ref failures);

        if (!transportStopped &&
            TryCleanup(() => transport?.Stop(), ref failures))
        {
            transportStopped = true;
        }

        if (transportStopped)
        {
            if (TryCleanup(() => sessionLease?.Stop(), ref failures))
            {
                sessionLease = null;
            }

            if (TryCleanup(() => transport?.Dispose(), ref failures))
            {
                transport = null;
                transportStopped = false;
            }

            if (TryCleanup(() => logSubscription?.Dispose(), ref failures))
            {
                logSubscription = null;
            }

            requestJournal = null;
        }

        if (throwOnFailure && failures is not null)
        {
            throw new AggregateException("One or more gateway runtime resources failed to stop cleanly.", failures);
        }

        return failures is null;
    }

    private static bool TryCleanup(Action cleanup, ref List<Exception>? failures)
    {
        try
        {
            cleanup();
            return true;
        }
        catch (Exception exception)
        {
            failures ??= new List<Exception>();
            failures.Add(exception);
            return false;
        }
    }

    private void EnsureMainThread()
    {
        if (Thread.CurrentThread.ManagedThreadId != mainThreadId)
        {
            throw new InvalidOperationException("Gateway runtime lifecycle and dispatch draining must run on its Unity main thread.");
        }
    }
}
