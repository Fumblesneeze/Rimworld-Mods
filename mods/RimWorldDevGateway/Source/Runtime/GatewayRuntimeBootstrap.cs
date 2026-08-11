using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.Threading;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using RimWorldDevGateway.Performance;
using RimWorldDevGateway.PerformanceTesting;
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
        GatewayEndToEndCoordinator? endToEndTests = null;
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

            var featureSelection = GatewayStartupFeatureSelection.Capture(
                GenCommandLine.CommandLineArgPassed,
                ReadCommandLineArgument);
            integrationTests = GatewayIntegrationTestCoordinator.Create(
                featureSelection.RunIntegrationTests,
                () => GatewayIntegrationTestCoordinator.CreateEnabled(
                    GenFilePaths.SaveDataFolderPath,
                    new GatewayIntegrationTestManifestCatalog(
                        new VerseGatewayIntegrationTestManifestSource()),
                    new GatewayIntegrationTestStableReadiness(
                        new VerseGatewayIntegrationTestLifecycleProbe()),
                    (operation, exception) => Log.Error(
                        "[RimWorldDevGateway] Integration-test " + operation +
                        " failed: " + exception)));
            endToEndTests = GatewayEndToEndCoordinator.Create(
                featureSelection.RunEndToEndTests,
                () => GatewayEndToEndCoordinator.CreateEnabled(
                    GenFilePaths.SaveDataFolderPath,
                    new VerseGatewayEndToEndManifestSource(),
                    (operation, exception) => Log.Error(
                        "[RimWorldDevGateway] End-to-end test " + operation +
                        " failed: " + exception),
                    featureSelection.SelectedEndToEndTestIds));

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

            var runtime = CreateDefaultRuntime(content, factory, integrationTests, endToEndTests);
            createdHost.Initialize(runtime, integrationTests, endToEndTests);
            integrationTests = null;
            endToEndTests = null;
            Log.Warning(
                $"[RimWorldDevGateway] API active on authenticated IPv4 loopback port {runtime.Port}; " +
                "unrestricted execution is enabled; in-game integration tests are " +
                (featureSelection.RunIntegrationTests ? "enabled; " : "disabled; ") +
                "end-to-end tests are " +
                (featureSelection.RunEndToEndTests ? "enabled." : "disabled."));
        }
        catch (Exception exception)
        {
            integrationTests?.Dispose();
            endToEndTests?.Dispose();
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

    private static string? ReadCommandLineArgument(string key) =>
        GenCommandLine.TryGetCommandLineArg(key, out var value) ? value : null;

    private static GatewayRuntime CreateDefaultRuntime(
        ModContentPack content,
        GatewayTransportFactory factory,
        GatewayIntegrationTestCoordinator integrationTests,
        GatewayEndToEndCoordinator endToEndTests)
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
                var screenshots = new GatewayScreenshotService(
                    activeDispatcher,
                    new UnityGatewayScreenshotBackend(),
                    maximumPngBytes: 64 * 1024 * 1024);
                var windowsInput = new GatewayWindowsInput();
                var gameControl = new GatewayGameControlController(
                    new VerseGatewayGameControlOperations());
                var camera = new GatewayCameraController(
                    new VerseGatewayCameraOperations());
                var things = new GatewayThingController(
                    new VerseGatewayThingOperations(logBuffer),
                    logBuffer);
                var gizmos = new GatewayGizmoRegistry(
                    new VerseGatewayGizmoSource(logBuffer));

                if (endToEndTests.Snapshot.Enabled)
                {
                    var floatMenus = new VerseGatewayEndToEndFloatMenuActions();
                    var tradeDialogs = new GatewayEndToEndTradeDialogActions(
                        new VerseGatewayEndToEndTradeDialogOperations());
                    var settlementTrade = new GatewayEndToEndSettlementTradeActions(
                        new VerseGatewayEndToEndSettlementTradeOperations());
                    var gizmoCatalog = new GatewayEndToEndGizmoCatalog(gizmos);
                    var clock = new VerseGatewayEndToEndClock();
                    var isolation = new GatewayEndToEndTestIsolation(
                        new VerseGatewayEndToEndIsolationOperations(
                            gameControl,
                            camera,
                            gizmos));
                    var contextServices = new Dictionary<Type, object>
                    {
                        [typeof(GatewayGameControlController)] = gameControl,
                        [typeof(GatewayCameraController)] = camera,
                        [typeof(GatewayThingController)] = things,
                        [typeof(GatewayGizmoRegistry)] = gizmos,
                        [typeof(GatewayScreenshotService)] = screenshots,
                        [typeof(GatewayWindowsInput)] = windowsInput,
                        [typeof(IGatewayEndToEndFloatMenuActions)] = floatMenus,
                        [typeof(IEndToEndFloatMenuCatalog)] = floatMenus,
                        [typeof(IEndToEndGizmoCatalog)] = gizmoCatalog
                    };
                    endToEndTests.ConfigureExecution(
                        new VerseGatewayEndToEndExecutionReadiness(),
                        new GatewayEndToEndExecutionFactory(
                            clock,
                            artifactDirectory =>
                            {
                                var testServices = new Dictionary<Type, object>(contextServices);
                                Func<IReadOnlyList<string>> activePerformancePackages = () =>
                                    LoadedModManager.RunningModsListForReading
                                        .Select(mod => mod.PackageId)
                                        .ToArray();
                                var packages = activePerformancePackages();
                                IGatewayPerformanceRuntimeBackend performanceBackend =
                                    packages.Contains(
                                        PerformanceTestContract.DpaPackageId,
                                        StringComparer.OrdinalIgnoreCase) &&
                                    !packages.Contains(
                                        PerformanceTestContract.CircinusPackageId,
                                        StringComparer.OrdinalIgnoreCase)
                                        ? new GatewayDpaPerformanceBackend(
                                            () => AppDomain.CurrentDomain.GetAssemblies(),
                                            activePerformancePackages,
                                            artifactDirectory)
                                        : new GatewayCircinusPerformanceBackend(
                                            () => AppDomain.CurrentDomain.GetAssemblies(),
                                            activePerformancePackages,
                                            () => GenFilePaths.SaveDataFolderPath,
                                            artifactDirectory);
                                testServices[typeof(GatewayPerformanceRunService)] =
                                    new CoordinatedGatewayPerformanceRunService(performanceBackend);
                                return new GatewayEndToEndTestContext(
                                    () => clock.FrameCount,
                                    () => clock.GameTick,
                                    serviceType => testServices.TryGetValue(serviceType, out var service)
                                        ? service
                                        : null);
                            },
                            artifactDirectory => new GatewayEndToEndNativeStepDriver(
                                new GatewayEndToEndNativeActions(
                                    new GatewayEndToEndGatewayBackend(
                                        gameControl,
                                        things,
                                        camera,
                                        gizmos,
                                        floatMenus,
                                        settlementTrade,
                                        tradeDialogs,
                                        windowsInput,
                                        screenshots,
                                        artifactDirectory))),
                            isolation));
                }

                return new GatewayApiRouter(
                    activeDispatcher,
                    stateProvider,
                    logBuffer,
                    new GatewayApiServices(
                        assemblyExecutor: new GatewayAssemblyExecutor(
                            maximumAssemblyBytes: 16 * 1024 * 1024,
                            maximumResultUtf8Bytes: GatewayAssemblyExecutor.DefaultMaximumResultUtf8Bytes,
                            runtimeExtensions: new GatewayAssemblyRuntimeExtensions(automations)),
                        csharpEvaluator: new GatewayCSharpEvaluator(),
                        screenshotService: screenshots,
                        windowsInput: windowsInput,
                        semanticActions: new GatewaySemanticActionRegistry(
                            activeDispatcher,
                            new VerseGatewaySemanticActionOperations(),
                            diagnostics: logBuffer),
                        gameControl: gameControl,
                        camera: camera,
                        things: things,
                        debugActions: new GatewayDebugActionRegistry(
                            new VerseGatewayDebugActionSource()),
                        gizmos: gizmos,
                        automations: automations,
                        defExporter: new GatewayDefExporter(
                            new VerseGatewayDefSource(logBuffer)),
                        integrationTestSnapshot: () => integrationTests.PublishedSnapshot,
                        endToEndTestSnapshot: () => endToEndTests.PublishedSnapshot,
                        requestShutdown: requestShutdown));
            });
    }
}

public sealed class GatewayRuntimeHost : MonoBehaviour
{
    private GatewayRuntime? runtime;
    private GatewayIntegrationTestCoordinator? integrationTests;
    private GatewayEndToEndCoordinator? endToEndTests;
    private readonly GatewayShutdownLifecycle shutdownLifecycle = new();
    private int stopping;
    private int shutdownInitialized;
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

        var endToEndSnapshot = endToEndTests?.Snapshot;
        if (endToEndSnapshot?.Enabled == true)
        {
            var state = "E2E TESTS — discovery " + endToEndSnapshot.DiscoveryState +
                        "; admitted " + endToEndSnapshot.Tests.Count +
                        "; failures " + endToEndSnapshot.Failures.Count;
            GUI.Box(new Rect(8f, 76f, 620f, 30f), state);
        }
    }

    internal void Initialize(
        GatewayRuntime value,
        GatewayIntegrationTestCoordinator integrationTestCoordinator,
        GatewayEndToEndCoordinator endToEndTestCoordinator)
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

        if (endToEndTestCoordinator is null)
        {
            throw new ArgumentNullException(nameof(endToEndTestCoordinator));
        }

        runtime = value;
        integrationTests = integrationTestCoordinator;
        endToEndTests = endToEndTestCoordinator;
        useGUILayout = false;
        Application.quitting += OnApplicationQuitting;
        var manifest = runtime.Start();
        integrationTests.AttachSession(manifest.RunId, manifest.Token);
        endToEndTests.AttachSession(manifest.RunId, manifest.Token);
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
                endToEndTests?.Tick();
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
            if (TryShutdown(allowRetry: true))
            {
                UnityEngine.Object.Destroy(gameObject);
            }
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
        TryShutdown(allowRetry: false);
    }

    private void OnApplicationQuit()
    {
        TryShutdown(allowRetry: false);
    }

    private void OnDestroy()
    {
        TryShutdown(allowRetry: false);
        GatewayRuntimeBootstrap.NotifyDestroyed(this);
    }

    private bool TryShutdown(bool allowRetry)
    {
        if (Interlocked.Exchange(ref stopping, 1) != 0)
        {
            return runtime is null;
        }

        if (Interlocked.Exchange(ref shutdownInitialized, 1) == 0)
        {
            Application.quitting -= OnApplicationQuitting;
            StopAllCoroutines();
        }

        var attempt = shutdownLifecycle.TryShutdown(
            () =>
            {
                runtime?.Stop();
                integrationTests?.Dispose();
                endToEndTests?.Dispose();
            },
            allowRetry,
            DateTimeOffset.UtcNow);
        if (attempt.ReportableFailure is not null)
        {
            if (attempt.IsTerminal)
            {
                Log.Error(
                    "[RimWorldDevGateway] Runtime shutdown encountered cleanup failures: " +
                    attempt.ReportableFailure);
            }
            else
            {
                Log.Warning(GatewayShutdownLogMessages.RetryRetained);
            }
        }

        if (attempt.IsTerminal)
        {
            if (attempt.RecoveredAfterRetainedFailure)
            {
                Log.Warning(GatewayShutdownLogMessages.RetryRecovered);
            }

            integrationTests = null;
            endToEndTests = null;
            runtime = null;
            return true;
        }

        Volatile.Write(ref stopping, 0);
        return false;
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
