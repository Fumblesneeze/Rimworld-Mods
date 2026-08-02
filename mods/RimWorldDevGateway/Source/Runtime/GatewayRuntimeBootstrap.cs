using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimWorldDevGateway;

public static class GatewayRuntimeBootstrap
{
    private static readonly object Sync = new();
    private static GatewayTransportFactory? transportFactory;
    private static GatewayRuntimeHost? host;
    private static int scheduled;

    public static void ConfigureTransportFactory(GatewayTransportFactory factory)
    {
        if (factory is null)
        {
            throw new ArgumentNullException(nameof(factory));
        }

        lock (Sync)
        {
            if (host is not null)
            {
                throw new InvalidOperationException("The gateway transport cannot be replaced after runtime startup.");
            }

            transportFactory = factory;
        }
    }

    public static void Schedule(ModContentPack content, GatewayTransportFactory factory)
    {
        ConfigureTransportFactory(factory);
        Schedule(content);
    }

    public static void Schedule(ModContentPack content)
    {
        if (content is null)
        {
            throw new ArgumentNullException(nameof(content));
        }

        if (Interlocked.CompareExchange(ref scheduled, 1, 0) != 0)
        {
            return;
        }

        LongEventHandler.ExecuteWhenFinished(() => StartScheduled(content));
    }

    internal static void NotifyDestroyed(GatewayRuntimeHost destroyedHost)
    {
        lock (Sync)
        {
            if (ReferenceEquals(host, destroyedHost))
            {
                host = null;
                Volatile.Write(ref scheduled, 0);
            }
        }
    }

    private static void StartScheduled(ModContentPack content)
    {
        GameObject? gameObject = null;
        GatewayIntegrationTestCoordinator? integrationTests = null;
        try
        {
            GatewayTransportFactory factory;
            lock (Sync)
            {
                if (host is not null)
                {
                    return;
                }

                factory = transportFactory ?? throw new InvalidOperationException(
                    "No gateway transport factory was configured before deferred startup.");
            }

            var runIntegrationTests = GenCommandLine.CommandLineArgPassed(
                "devGatewayRunIntegrationTests");
            integrationTests = GatewayIntegrationTestCoordinator.Create(
                runIntegrationTests,
                () => GatewayIntegrationTestCoordinator.CreateEnabled(
                    GenFilePaths.SaveDataFolderPath,
                    new GatewayIntegrationTestManifestCatalog(
                        new VerseGatewayIntegrationTestManifestSource()),
                    new GatewayIntegrationTestStableReadiness(
                        new VerseGatewayIntegrationTestLifecycleProbe()),
                    (operation, exception) => Log.Error(
                        "[RimWorldDevGateway] Integration-test " + operation +
                        " failed: " + exception)));

            gameObject = new GameObject("[RimWorldDevGateway] Runtime")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            UnityEngine.Object.DontDestroyOnLoad(gameObject);
            var createdHost = gameObject.AddComponent<GatewayRuntimeHost>();
            lock (Sync)
            {
                host = createdHost;
            }

            var runtime = CreateDefaultRuntime(content, factory, integrationTests);
            createdHost.Initialize(runtime, integrationTests);
            integrationTests = null;
            Log.Warning(
                $"[RimWorldDevGateway] API active on authenticated IPv4 loopback port {runtime.Port}; " +
                "unrestricted execution is enabled; in-game integration tests are " +
                (runIntegrationTests ? "enabled." : "disabled."));
        }
        catch (Exception exception)
        {
            integrationTests?.Dispose();
            if (gameObject is not null)
            {
                UnityEngine.Object.Destroy(gameObject);
            }

            lock (Sync)
            {
                host = null;
                Volatile.Write(ref scheduled, 0);
            }

            Log.Error("[RimWorldDevGateway] Runtime startup failed: " + exception);
        }
    }

    private static GatewayRuntime CreateDefaultRuntime(
        ModContentPack content,
        GatewayTransportFactory factory,
        GatewayIntegrationTestCoordinator integrationTests)
    {
        using var process = Process.GetCurrentProcess();
        var processStartUtc = new DateTimeOffset(process.StartTime.ToUniversalTime(), TimeSpan.Zero);
        var modVersion = content.ModMetaData.ModVersion;
        if (string.IsNullOrEmpty(modVersion))
        {
            modVersion = typeof(RimWorldDevGatewayMod).Assembly.GetName().Version?.ToString() ?? string.Empty;
        }

        var identity = new GatewayRuntimeIdentity(
            process.Id,
            processStartUtc,
            VersionControl.CurrentVersionStringWithRev,
            modVersion);
        var dispatcher = new GatewayDispatcher();
        return new GatewayRuntime(
            identity,
            new GatewaySessionManager(GenFilePaths.SaveDataFolderPath),
            dispatcher,
            activeDispatcher => new RimWorldGatewayStateProvider(
                new VerseGatewaySnapshotSource(),
                activeDispatcher,
                identity.ProcessId),
            factory,
            buffer => new UnityGatewayLogSubscription(buffer),
            routerFactory: (activeDispatcher, stateProvider, logBuffer, requestShutdown) =>
            {
                var automations = new GatewayAutomationRegistry(diagnostics: logBuffer);
                GatewayQuickstartAutomation.Register(automations);
                return new GatewayApiRouter(
                    activeDispatcher,
                    stateProvider,
                    logBuffer,
                    new GatewayApiServices(
                        assemblyExecutor: new GatewayAssemblyExecutor(16 * 1024 * 1024),
                        csharpEvaluator: new GatewayCSharpEvaluator(),
                        screenshotService: new GatewayScreenshotService(
                            activeDispatcher,
                            new UnityGatewayScreenshotBackend(),
                            maximumPngBytes: 64 * 1024 * 1024),
                        windowsInput: new GatewayWindowsInput(),
                        semanticActions: new GatewaySemanticActionRegistry(
                            activeDispatcher,
                            new VerseGatewaySemanticActionOperations(),
                            diagnostics: logBuffer),
                        gameControl: new GatewayGameControlController(
                            new VerseGatewayGameControlOperations()),
                        camera: new GatewayCameraController(
                            new VerseGatewayCameraOperations()),
                        things: new GatewayThingController(
                            new VerseGatewayThingOperations(logBuffer),
                            logBuffer),
                        debugActions: new GatewayDebugActionRegistry(
                            new VerseGatewayDebugActionSource()),
                        gizmos: new GatewayGizmoRegistry(
                            new VerseGatewayGizmoSource(logBuffer)),
                        automations: automations,
                        defExporter: new GatewayDefExporter(
                            new VerseGatewayDefSource(logBuffer)),
                        integrationTestSnapshot: () => integrationTests.PublishedSnapshot,
                        requestShutdown: requestShutdown));
            });
    }
}

public sealed class GatewayRuntimeHost : MonoBehaviour
{
    private GatewayRuntime? runtime;
    private GatewayIntegrationTestCoordinator? integrationTests;
    private int stopping;
    private int shutdownRequestedFrame = -1;

    private void OnGUI()
    {
        if (runtime?.IsRunning != true || Volatile.Read(ref stopping) != 0)
        {
            return;
        }

        const string warning = "DEV GATEWAY ACTIVE — UNRESTRICTED CODE EXECUTION ENABLED";
        GUI.Box(new Rect(8f, 8f, 620f, 30f), warning);
        var testSnapshot = integrationTests?.Snapshot;
        if (testSnapshot?.Enabled == true)
        {
            var mainMenu = testSnapshot.LifecyclePoints.FirstOrDefault(
                point => point.RunAt == RimWorldDevGateway.IntegrationTesting.RunAt.MainMenuLoaded);
            var playableMap = testSnapshot.LifecyclePoints.FirstOrDefault(
                point => point.RunAt == RimWorldDevGateway.IntegrationTesting.RunAt.PlayableMapLoaded);
            var state = "IN-GAME TESTS — discovery " + testSnapshot.DiscoveryState +
                        "; menu " + (mainMenu?.State ?? "pending") +
                        "; map " + (playableMap?.State ?? "pending");
            GUI.Box(new Rect(8f, 42f, 620f, 30f), state);
        }
    }

    internal void Initialize(
        GatewayRuntime value,
        GatewayIntegrationTestCoordinator integrationTestCoordinator)
    {
        if (value is null)
        {
            throw new ArgumentNullException(nameof(value));
        }

        if (runtime is not null)
        {
            throw new InvalidOperationException("The gateway runtime host is already initialized.");
        }

        if (integrationTestCoordinator is null)
        {
            throw new ArgumentNullException(nameof(integrationTestCoordinator));
        }

        runtime = value;
        integrationTests = integrationTestCoordinator;
        useGUILayout = false;
        Application.quitting += OnApplicationQuitting;
        var manifest = runtime.Start();
        integrationTests.AttachSession(manifest.RunId, manifest.Token);
        StartCoroutine(DrainEndOfFrame());
    }

    private void Update()
    {
        var active = runtime;
        if (active is null || Volatile.Read(ref stopping) != 0)
        {
            return;
        }

        if (!active.IsShutdownRequested)
        {
            active.Drain(DispatchPhase.Update);
            if (!active.IsShutdownRequested)
            {
                integrationTests?.Tick();
            }
            if (!active.IsShutdownRequested)
            {
                return;
            }
        }

        if (shutdownRequestedFrame < 0)
        {
            shutdownRequestedFrame = Time.frameCount;
            return;
        }

        if (Time.frameCount > shutdownRequestedFrame)
        {
            Shutdown();
            UnityEngine.Object.Destroy(gameObject);
        }
    }

    private IEnumerator DrainEndOfFrame()
    {
        var wait = new WaitForEndOfFrame();
        while (Volatile.Read(ref stopping) == 0)
        {
            yield return wait;
            var active = runtime;
            if (active?.IsShutdownRequested == false)
            {
                active.Drain(DispatchPhase.EndOfFrame);
            }
        }
    }

    private void OnApplicationQuitting()
    {
        Shutdown();
    }

    private void OnApplicationQuit()
    {
        Shutdown();
    }

    private void OnDestroy()
    {
        Shutdown();
        GatewayRuntimeBootstrap.NotifyDestroyed(this);
    }

    private void Shutdown()
    {
        if (Interlocked.Exchange(ref stopping, 1) != 0)
        {
            return;
        }

        Application.quitting -= OnApplicationQuitting;
        StopAllCoroutines();
        try
        {
            integrationTests?.Dispose();
            runtime?.Stop();
        }
        catch (Exception exception)
        {
            Log.Error("[RimWorldDevGateway] Runtime shutdown encountered cleanup failures: " + exception);
        }
        finally
        {
            integrationTests = null;
            runtime = null;
        }
    }
}

internal sealed class UnityGatewayLogSubscription : IDisposable
{
    private readonly GatewayLogBuffer buffer;
    private int disposed;

    public UnityGatewayLogSubscription(GatewayLogBuffer buffer)
    {
        this.buffer = buffer ?? throw new ArgumentNullException(nameof(buffer));
        Application.logMessageReceivedThreaded += OnLog;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref disposed, 1) == 0)
        {
            Application.logMessageReceivedThreaded -= OnLog;
        }
    }

    private void OnLog(string message, string stackTrace, LogType type)
    {
        if (Volatile.Read(ref disposed) != 0)
        {
            return;
        }

        var severity = type switch
        {
            LogType.Warning => "Warning",
            LogType.Error or LogType.Assert or LogType.Exception => "Error",
            _ => "Message"
        };
        buffer.Append(
            severity,
            message ?? string.Empty,
            string.IsNullOrEmpty(stackTrace) ? null : stackTrace,
            Thread.CurrentThread.ManagedThreadId.ToString(CultureInfo.InvariantCulture),
            GatewayRequestScope.CurrentRequestId);
    }
}
