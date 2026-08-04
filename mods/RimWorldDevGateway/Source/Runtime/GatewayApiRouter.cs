using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.Serialization;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway;

public interface IGatewayStateProvider
{
    object CaptureStatus();

    object CaptureUiState();
}

public sealed class GatewayApiRouter
{
    private const int MaximumCSharpSourceBytes = 64 * 1024;
    private const int MaximumDefExportResponseNodes = GatewayDefExporter.MaximumSerializedPageNodes;
    private const int MaximumDefExportResponseDepth = 64;
    private static readonly TimeSpan DebugActionResponseTimeout = TimeSpan.FromSeconds(60);
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);
    private readonly GatewayDispatcher dispatcher;
    private readonly IGatewayStateProvider stateProvider;
    private readonly GatewayLogBuffer logBuffer;
    private readonly GatewayApiServices services;
    private readonly TimeSpan responseTimeout;

    public GatewayApiRouter(
        GatewayDispatcher dispatcher,
        IGatewayStateProvider stateProvider,
        GatewayLogBuffer logBuffer,
        TimeSpan? responseTimeout = null)
        : this(dispatcher, stateProvider, logBuffer, new GatewayApiServices(), responseTimeout)
    {
    }

    public GatewayApiRouter(
        GatewayDispatcher dispatcher,
        IGatewayStateProvider stateProvider,
        GatewayLogBuffer logBuffer,
        GatewayApiServices services,
        TimeSpan? responseTimeout = null)
    {
        this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        this.stateProvider = stateProvider ?? throw new ArgumentNullException(nameof(stateProvider));
        this.logBuffer = logBuffer ?? throw new ArgumentNullException(nameof(logBuffer));
        this.services = services ?? throw new ArgumentNullException(nameof(services));
        this.responseTimeout = responseTimeout ?? TimeSpan.FromSeconds(15);
        if (this.responseTimeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(responseTimeout));
        }
    }

    public GatewayHttpResponse Handle(GatewayHttpRequest request, string requestId)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (string.IsNullOrWhiteSpace(requestId))
        {
            throw new ArgumentException("A request ID is required.", nameof(requestId));
        }

        request.CancellationToken.ThrowIfCancellationRequested();
        var stopwatch = Stopwatch.StartNew();
        try
        {
            if (request.Path.StartsWith("/api/v", StringComparison.Ordinal) &&
                !request.Path.StartsWith("/api/v1/", StringComparison.Ordinal) &&
                !string.Equals(request.Path, "/api/v1", StringComparison.Ordinal))
            {
                return Error(400, "Bad Request", requestId, "unsupported_api_version", "Only API version 1 is supported.", stopwatch);
            }

            if (request.Method == "GET" && request.Path == "/api/v1/status")
            {
                return DispatchSnapshot(request, requestId, "status", stateProvider.CaptureStatus, stopwatch);
            }

            if (request.Method == "GET" && request.Path == "/api/v1/ui-state")
            {
                return DispatchSnapshot(request, requestId, "ui-state", stateProvider.CaptureUiState, stopwatch);
            }

            if (request.Method == "GET" && request.Path == "/api/v1/game-state")
            {
                Require(services.GameControl, "game_control_unavailable");
                return DispatchSnapshot(
                    request,
                    requestId,
                    "game-state.capture",
                    CaptureExpandedGameState,
                    stopwatch);
            }

            if (request.Method == "POST" && request.Path == "/api/v1/game-state")
            {
                var gameControl = Require(services.GameControl, "game_control_unavailable");
                var payload = GatewayGameControlRequestJson.Read(Encoding.UTF8.GetString(request.Body));
                return DispatchSnapshot(
                    request,
                    requestId,
                    "game-state.mutate",
                    () => gameControl.Mutate(payload),
                    stopwatch);
            }

            if (request.Method == "POST" && request.Path == "/api/v1/camera")
            {
                var camera = Require(services.Camera, "camera_control_unavailable");
                var payload = DeserializeBody<GatewayCameraMutationRequest>(request);
                return DispatchSnapshot(
                    request,
                    requestId,
                    "camera.mutate",
                    () => camera.Mutate(payload),
                    stopwatch);
            }

            if (request.Method == "POST" && request.Path == "/api/v1/things/query")
            {
                var things = Require(services.Things, "thing_control_unavailable");
                var payload = DeserializeBody<GatewayThingQueryRequest>(request);
                return DispatchSnapshot(
                    request,
                    requestId,
                    "things.query",
                    () => things.Query(payload),
                    stopwatch);
            }

            const string thingPrefix = "/api/v1/things/";
            if (request.Method == "GET" &&
                request.Path.StartsWith(thingPrefix, StringComparison.Ordinal))
            {
                var things = Require(services.Things, "thing_control_unavailable");
                var handle = Uri.UnescapeDataString(request.Path.Substring(thingPrefix.Length));
                if (handle.Length == 0 || handle.IndexOf('/') >= 0)
                {
                    throw new FormatException("Thing handle is invalid.");
                }

                return DispatchSnapshot(
                    request,
                    requestId,
                    "things.inspect",
                    () => things.Inspect(handle),
                    stopwatch);
            }

            if (request.Method == "POST" && request.Path == "/api/v1/selection")
            {
                var things = Require(services.Things, "thing_control_unavailable");
                var payload = DeserializeBody<GatewaySelectionRequest>(request);
                return DispatchSnapshot(
                    request,
                    requestId,
                    "selection.mutate",
                    () => things.MutateSelection(payload),
                    stopwatch);
            }

            if (request.Method == "POST" && request.Path == "/api/v1/dev-tools/actions/query")
            {
                var debugActions = Require(
                    services.DebugActions,
                    "debug_actions_unavailable");
                var query = GatewayDebugActionRequestJson.ReadQuery(
                    Encoding.UTF8.GetString(request.Body));
                return DispatchSnapshot(
                    request,
                    requestId,
                    "debug-actions.query",
                    () => debugActions.Query(query),
                    stopwatch,
                    DebugActionResponseTimeout);
            }

            const string debugActionPrefix = "/api/v1/dev-tools/actions/";
            const string invokeSuffix = "/invoke";
            if (request.Method == "POST" &&
                request.Path.StartsWith(debugActionPrefix, StringComparison.Ordinal) &&
                request.Path.EndsWith(invokeSuffix, StringComparison.Ordinal))
            {
                var debugActions = Require(
                    services.DebugActions,
                    "debug_actions_unavailable");
                var encodedHandle = request.Path.Substring(
                    debugActionPrefix.Length,
                    request.Path.Length - debugActionPrefix.Length - invokeSuffix.Length);
                var handle = Uri.UnescapeDataString(encodedHandle);
                if (string.IsNullOrWhiteSpace(handle) || handle.IndexOf('/') >= 0)
                {
                    throw new FormatException("The debug-action handle is invalid.");
                }

                return DispatchSnapshot(
                    request,
                    requestId,
                    "debug-actions.invoke",
                    () => debugActions.Invoke(handle),
                    stopwatch);
            }

            if (request.Method == "POST" && request.Path == "/api/v1/dev-tools/spawn")
            {
                var automations = Require(services.Automations, "automations_unavailable");
                var payload = GatewayAutomationRequestJson.Read(
                    Encoding.UTF8.GetString(request.Body));
                return DispatchSnapshot(
                    request,
                    requestId,
                    "developer-spawn",
                    () => automations.StartRun(
                        "quickstart.spawn",
                        requestId,
                        payload.Arguments,
                        payload.IdempotencyKey),
                    stopwatch);
            }

            if (request.Method == "POST" && request.Path == "/api/v1/gizmos/query")
            {
                var gizmos = Require(services.Gizmos, "gizmos_unavailable");
                var query = GatewayGizmoRequestJson.ReadQuery(
                    Encoding.UTF8.GetString(request.Body));
                return DispatchSnapshot(
                    request,
                    requestId,
                    "gizmos.query",
                    () => gizmos.Query(query),
                    stopwatch);
            }

            const string gizmoPrefix = "/api/v1/gizmos/";
            if (request.Method == "POST" &&
                request.Path.StartsWith(gizmoPrefix, StringComparison.Ordinal) &&
                request.Path.EndsWith(invokeSuffix, StringComparison.Ordinal))
            {
                var gizmos = Require(services.Gizmos, "gizmos_unavailable");
                var encodedHandle = request.Path.Substring(
                    gizmoPrefix.Length,
                    request.Path.Length - gizmoPrefix.Length - invokeSuffix.Length);
                var handle = Uri.UnescapeDataString(encodedHandle);
                if (string.IsNullOrWhiteSpace(handle) || handle.IndexOf('/') >= 0)
                {
                    throw new FormatException("The gizmo handle is invalid.");
                }

                return DispatchSnapshot(
                    request,
                    requestId,
                    "gizmos.invoke",
                    () => gizmos.Invoke(handle),
                    stopwatch);
            }

            if (request.Method == "GET" && request.Path == "/api/v1/interactions/current")
            {
                var gizmos = Require(services.Gizmos, "gizmos_unavailable");
                return DispatchSnapshot(
                    request,
                    requestId,
                    "interactions.current",
                    () => gizmos.CurrentInteraction,
                    stopwatch);
            }

            const string interactionPrefix = "/api/v1/interactions/";
            const string applySuffix = "/apply";
            const string cancelSuffix = "/cancel";
            if (request.Method == "POST" &&
                request.Path.StartsWith(interactionPrefix, StringComparison.Ordinal) &&
                (request.Path.EndsWith(applySuffix, StringComparison.Ordinal) ||
                 request.Path.EndsWith(cancelSuffix, StringComparison.Ordinal)))
            {
                var gizmos = Require(services.Gizmos, "gizmos_unavailable");
                var applying = request.Path.EndsWith(applySuffix, StringComparison.Ordinal);
                var suffix = applying ? applySuffix : cancelSuffix;
                var encodedHandle = request.Path.Substring(
                    interactionPrefix.Length,
                    request.Path.Length - interactionPrefix.Length - suffix.Length);
                var handle = Uri.UnescapeDataString(encodedHandle);
                if (string.IsNullOrWhiteSpace(handle) || handle.IndexOf('/') >= 0)
                {
                    throw new FormatException("The interaction handle is invalid.");
                }

                if (applying)
                {
                    var input = GatewayGizmoRequestJson.ReadInteractionInput(
                        Encoding.UTF8.GetString(request.Body));
                    return DispatchSnapshot(
                        request,
                        requestId,
                        "interactions.apply",
                        () => gizmos.Apply(handle, input),
                        stopwatch);
                }

                return DispatchSnapshot(
                    request,
                    requestId,
                    "interactions.cancel",
                    () => gizmos.Cancel(handle),
                    stopwatch);
            }

            if (request.Method == "GET" && request.Path == "/api/v1/logs")
            {
                var query = ParseQuery(request.Query);
                var after = ParseLong(query, "after", 0, minimum: 0);
                var limit = (int)ParseLong(query, "limit", 100, minimum: 1);
                var read = logBuffer.Read(after, limit);
                return Success(requestId, read, stopwatch);
            }

            if (request.Method == "GET" && request.Path == "/api/v1/integration-tests")
            {
                var capture = Require(
                    services.IntegrationTestSnapshot,
                    "integration_test_status_unavailable");
                var capturedSnapshot = capture();
                if (capturedSnapshot is null)
                {
                    return Error(
                        503,
                        "Service Unavailable",
                        requestId,
                        "integration_test_status_pending",
                        "Integration-test status is pending its initial durable session commit.",
                        stopwatch,
                        retryable: true);
                }

                return Success(
                    requestId,
                    capturedSnapshot,
                    stopwatch,
                    jsonLimits: new GatewayJsonLimits(
                        maximumDepth: 8,
                        maximumNodes: 32 * 1024,
                        maximumUtf8Bytes: GatewayIntegrationTestSnapshot.MaximumSerializedUtf8Bytes));
            }

            if (request.Method == "POST" && request.Path == "/api/v1/defs/export")
            {
                var exporter = Require(services.DefExporter, "def_export_unavailable");
                var payload = DeserializeDefExportBody(request);
                var exportRequest = new GatewayDefExportRequest(
                    payload.Format,
                    (IEnumerable<string>?)payload.DefTypes ?? Array.Empty<string>(),
                    (IEnumerable<string>?)payload.DefNames ?? Array.Empty<string>(),
                    (IEnumerable<string>?)payload.SourcePackageIds ?? Array.Empty<string>(),
                    payload.Cursor,
                    payload.PageSize,
                    (IEnumerable<string>?)payload.FieldNames ?? Array.Empty<string>());
                var maximumSerializedResultUtf8Bytes = MaximumDefExportResultUtf8Bytes(requestId);
                var maximumSerializedResultNodes = MaximumDefExportResultNodes(requestId);
                return DispatchSnapshotWithCancellation(
                    request,
                    requestId,
                    "defs.export",
                    cancellationToken => exporter.Export(
                        exportRequest,
                        maximumSerializedResultUtf8Bytes,
                        maximumSerializedResultNodes,
                        cancellationToken),
                    stopwatch,
                    new GatewayJsonLimits(
                        MaximumDefExportResponseDepth,
                        MaximumDefExportResponseNodes,
                        GatewayDefExporter.MaximumSerializedPageUtf8Bytes));
            }

            if (request.Method == "POST" && request.Path == "/api/v1/executions/csharp")
            {
                var evaluator = Require(services.CSharpEvaluator, "csharp_execution_unavailable");
                var source = ReadCSharpSource(request);
                return DispatchSnapshot(
                    request,
                    requestId,
                    "execution.csharp",
                    () => evaluator.Evaluate(source),
                    stopwatch);
            }

            if (request.Method == "POST" && request.Path == "/api/v1/executions/assembly")
            {
                var executor = Require(services.AssemblyExecutor, "assembly_execution_unavailable");
                var payload = DeserializeBody<GatewayAssemblyExecutionRequest>(request);
                var assemblyBytes = Convert.FromBase64String(payload.AssemblyBase64);
                return DispatchSnapshot(
                    request,
                    requestId,
                    "execution.assembly",
                    () => executor.Execute(
                        assemblyBytes,
                        payload.EntryType,
                        payload.EntryMethod,
                        payload.RequestJson),
                    stopwatch);
            }

            if (request.Method == "POST" && request.Path == "/api/v1/screenshots")
            {
                var screenshot = Require(services.ScreenshotService, "screenshot_unavailable");
                using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                    request.CancellationToken);
                var capture = screenshot.CaptureOperation(
                    requestId,
                    responseTimeout,
                    timeoutCancellation.Token);
                var timeoutResponse = WaitForOperation(
                    capture,
                    requestId,
                    "screenshot.capture",
                    stopwatch,
                    timeoutCancellation,
                    request.CancellationToken);
                if (timeoutResponse is not null)
                {
                    return timeoutResponse;
                }

                return new GatewayHttpResponse(
                    200,
                    "OK",
                    "image/png",
                    capture.Completion.GetAwaiter().GetResult());
            }

            if (request.Method == "POST" && request.Path == "/api/v1/input/click")
            {
                var input = Require(services.WindowsInput, "raw_input_unavailable");
                var payload = DeserializeBody<GatewayClickRequest>(request);
                var button = ParseMouseButton(payload.Button);
                return Success(
                    requestId,
                    input.Click(
                        new GatewayClientPoint(payload.X, payload.Y),
                        button,
                        payload.Activate,
                        request.CancellationToken),
                    stopwatch);
            }

            if (request.Method == "POST" && request.Path == "/api/v1/input/drag")
            {
                var input = Require(services.WindowsInput, "raw_input_unavailable");
                var payload = DeserializeBody<GatewayDragRequest>(request);
                var button = ParseMouseButton(payload.Button);
                return Success(
                    requestId,
                    input.Drag(
                        new GatewayClientPoint(payload.StartX, payload.StartY),
                        new GatewayClientPoint(payload.EndX, payload.EndY),
                        button,
                        TimeSpan.FromMilliseconds(payload.DurationMs),
                        payload.Steps,
                        payload.Activate,
                        request.CancellationToken),
                    stopwatch);
            }

            if (request.Method == "POST" && request.Path == "/api/v1/input/keys")
            {
                var input = Require(services.WindowsInput, "raw_input_unavailable");
                var payload = DeserializeBody<GatewayKeysRequest>(request);
                if (payload.Text is not null && payload.Key is not null)
                {
                    throw new FormatException("Specify either 'text' or 'key', not both.");
                }

                if (payload.Text is not null)
                {
                    return Success(
                        requestId,
                        input.SendText(
                            payload.Text,
                            payload.Activate,
                            request.CancellationToken),
                        stopwatch);
                }

                if (string.IsNullOrWhiteSpace(payload.Key))
                {
                    throw new FormatException("A 'key' or 'text' value is required.");
                }

                return Success(
                    requestId,
                    input.SendChord(
                        payload.Modifiers,
                        payload.Key!,
                        payload.Activate,
                        request.CancellationToken),
                    stopwatch);
            }

            if (request.Method == "GET" && request.Path == "/api/v1/actions")
            {
                var actions = Require(services.SemanticActions, "semantic_actions_unavailable");
                using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                    request.CancellationToken);
                return AwaitJsonOperation(
                    actions.DiscoverOperation(requestId, timeoutCancellation.Token),
                    requestId,
                    "semantic-actions.discover",
                    stopwatch,
                    timeoutCancellation,
                    request.CancellationToken);
            }

            const string actionPrefix = "/api/v1/actions/";
            if (request.Method == "POST" && request.Path.StartsWith(actionPrefix, StringComparison.Ordinal))
            {
                var actions = Require(services.SemanticActions, "semantic_actions_unavailable");
                var actionName = Uri.UnescapeDataString(request.Path.Substring(actionPrefix.Length));
                if (string.IsNullOrWhiteSpace(actionName) || actionName.IndexOf('/') >= 0)
                {
                    throw new FormatException("The semantic action name is invalid.");
                }

                var payload = DeserializeBody<GatewaySemanticActionRequest>(request);
                using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(
                    request.CancellationToken);
                return AwaitJsonOperation(
                    actions.InvokeOperation(
                        requestId,
                        actionName,
                        payload.Arguments,
                        timeoutCancellation.Token),
                    requestId,
                    actionName,
                    stopwatch,
                    timeoutCancellation,
                    request.CancellationToken);
            }

            if (request.Method == "GET" && request.Path == "/api/v1/automations")
            {
                var automations = Require(services.Automations, "automations_unavailable");
                return DispatchSnapshot(
                    request,
                    requestId,
                    "automations.discover",
                    () => automations.Describe(),
                    stopwatch);
            }

            const string automationPrefix = "/api/v1/automations/";
            const string runSuffix = "/runs";
            if (request.Method == "POST" &&
                request.Path.StartsWith(automationPrefix, StringComparison.Ordinal) &&
                request.Path.EndsWith(runSuffix, StringComparison.Ordinal))
            {
                var automations = Require(services.Automations, "automations_unavailable");
                var encodedName = request.Path.Substring(
                    automationPrefix.Length,
                    request.Path.Length - automationPrefix.Length - runSuffix.Length);
                var name = Uri.UnescapeDataString(encodedName);
                if (string.IsNullOrWhiteSpace(name) || name.IndexOf('/') >= 0)
                {
                    throw new FormatException("The automation name is invalid.");
                }

                var payload = GatewayAutomationRequestJson.Read(Encoding.UTF8.GetString(request.Body));
                return DispatchSnapshot(
                    request,
                    requestId,
                    "automation." + name,
                    () => automations.StartRun(name, requestId, payload.Arguments, payload.IdempotencyKey),
                    stopwatch);
            }

            if (request.Method == "POST" && request.Path == "/api/v1/server/shutdown")
            {
                var requestShutdown = services.RequestShutdown ??
                    throw new GatewayCapabilityException(
                        "shutdown_unavailable",
                        "Controlled gateway shutdown is not configured.");
                return Success(
                    requestId,
                    new SortedDictionary<string, object?> { ["shutdownRequested"] = true },
                    stopwatch,
                    202,
                    "Accepted").WithTransportCompletion(requestShutdown);
            }

            return Error(404, "Not Found", requestId, "route_not_found", "No gateway route matches the request.", stopwatch);
        }
        catch (GatewayDispatchException exception)
        {
            var status = exception.Code == "queue_full" ? 503 : 504;
            var reason = status == 503 ? "Service Unavailable" : "Gateway Timeout";
            var publicCode = exception.Code == "queue_full" ? "gateway_busy" : exception.Code;
            return Error(status, reason, requestId, publicCode, exception.Message, stopwatch, retryable: true);
        }
        catch (GatewayExecutionException exception)
        {
            return Error(400, "Bad Request", requestId, exception.Code, exception.Message, stopwatch);
        }
        catch (GatewayScreenshotException exception)
        {
            var status = exception.Code == "capture_busy" ? 409 : 500;
            return Error(status, status == 409 ? "Conflict" : "Internal Server Error", requestId, exception.Code, exception.Message, stopwatch);
        }
        catch (GatewayInputException exception)
        {
            return Error(400, "Bad Request", requestId, exception.Code, exception.Message, stopwatch);
        }
        catch (GatewayAutomationException exception)
        {
            return Error(400, "Bad Request", requestId, exception.Code, exception.Message, stopwatch);
        }
        catch (GatewayGameControlException exception)
        {
            return Error(400, "Bad Request", requestId, exception.Code, exception.Message, stopwatch);
        }
        catch (GatewayCameraException exception)
        {
            return Error(400, "Bad Request", requestId, exception.Code, exception.Message, stopwatch);
        }
        catch (GatewayThingControlException exception)
        {
            var status = exception.Code == "thing_not_found" ? 404 : 400;
            return Error(
                status,
                status == 404 ? "Not Found" : "Bad Request",
                requestId,
                exception.Code,
                exception.Message,
                stopwatch);
        }
        catch (GatewayDebugActionException exception)
        {
            var status = exception.Code == "debug_action_discovery_failed" ? 500 : 400;
            if (status == 500)
            {
                logBuffer.Append(
                    "Error",
                    $"Request {requestId} debug-action discovery failed: {exception.Message}",
                    exception.ToString(),
                    requestId: requestId);
            }

            return Error(
                status,
                status == 500 ? "Internal Server Error" : "Bad Request",
                requestId,
                exception.Code,
                exception.Message,
                stopwatch);
        }
        catch (GatewayGizmoException exception)
        {
            var status = exception.Code == "gizmo_discovery_failed" ? 500 : 400;
            if (status == 500)
            {
                logBuffer.Append(
                    "Error",
                    $"Request {requestId} gizmo discovery failed: {exception.Message}",
                    exception.ToString(),
                    requestId: requestId);
            }

            return Error(
                status,
                status == 500 ? "Internal Server Error" : "Bad Request",
                requestId,
                exception.Code,
                exception.Message,
                stopwatch);
        }
        catch (GatewayDefExportException exception)
        {
            var internalFailure = exception.Code.StartsWith("def_database_", StringComparison.Ordinal) ||
                                  exception.Code.StartsWith("def_source_", StringComparison.Ordinal) ||
                                  exception.Code.StartsWith("def_export_page_budget_", StringComparison.Ordinal);
            return Error(
                internalFailure ? 500 : 400,
                internalFailure ? "Internal Server Error" : "Bad Request",
                requestId,
                exception.Code,
                exception.Message,
                stopwatch);
        }
        catch (GatewayCapabilityException exception)
        {
            return Error(501, "Not Implemented", requestId, exception.Code, exception.Message, stopwatch);
        }
        catch (SerializationException exception)
        {
            return Error(400, "Bad Request", requestId, "invalid_json", exception.Message, stopwatch);
        }
        catch (FormatException exception)
        {
            return Error(400, "Bad Request", requestId, "invalid_argument", exception.Message, stopwatch);
        }
        catch (OperationCanceledException) when (request.CancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception)
        {
            logBuffer.Append("Error", $"Request {requestId} failed: {exception.Message}", exception.ToString(), requestId: requestId);
            return Error(500, "Internal Server Error", requestId, "internal_error", exception.Message, stopwatch);
        }
    }

    private GatewayHttpResponse DispatchSnapshot(
        GatewayHttpRequest request,
        string requestId,
        string operation,
        Func<object?> capture,
        Stopwatch stopwatch,
        TimeSpan? operationTimeout = null)
        => DispatchSnapshotCore(
            request,
            requestId,
            operation,
            _ => capture(),
            stopwatch,
            operationTimeout,
            jsonLimits: null);

    private GatewayHttpResponse DispatchSnapshotWithCancellation(
        GatewayHttpRequest request,
        string requestId,
        string operation,
        Func<CancellationToken, object?> capture,
        Stopwatch stopwatch,
        GatewayJsonLimits jsonLimits,
        TimeSpan? operationTimeout = null)
        => DispatchSnapshotCore(
            request,
            requestId,
            operation,
            capture,
            stopwatch,
            operationTimeout,
            jsonLimits);

    private GatewayHttpResponse DispatchSnapshotCore(
        GatewayHttpRequest request,
        string requestId,
        string operation,
        Func<CancellationToken, object?> capture,
        Stopwatch stopwatch,
        TimeSpan? operationTimeout,
        GatewayJsonLimits? jsonLimits)
    {
        var timeout = operationTimeout ?? responseTimeout;
        using var timeoutCancellation = CancellationTokenSource.CreateLinkedTokenSource(
            request.CancellationToken);
        var pending = dispatcher.EnqueueOperation(
            requestId,
            operation,
            timeout,
            capture,
            cancellationToken: timeoutCancellation.Token);

        var timeoutResponse = WaitForOperation(
            pending,
            requestId,
            operation,
            stopwatch,
            timeoutCancellation,
            request.CancellationToken,
            timeout);
        if (timeoutResponse is not null)
        {
            return timeoutResponse;
        }

        var result = pending.Completion.GetAwaiter().GetResult();
        return jsonLimits is null
            ? Success(requestId, result, stopwatch)
            : Success(requestId, result, stopwatch, jsonLimits: jsonLimits);
    }

    private object CaptureExpandedGameState()
    {
        var control = Require(services.GameControl, "game_control_unavailable").Capture();
        GatewayCameraSnapshot? camera = null;
        if (services.Camera is not null)
        {
            try
            {
                camera = services.Camera.Capture();
            }
            catch (GatewayCameraException exception) when (
                exception.Code is "map_unavailable" or "camera_unavailable")
            {
                // The main menu has no current map or map camera; those fields are nullable by contract.
            }
        }

        IReadOnlyList<GatewayThingSummary> selection = Array.Empty<GatewayThingSummary>();
        if (services.Things is not null)
        {
            try
            {
                selection = services.Things.CaptureSelection();
            }
            catch (GatewayThingControlException exception) when (
                exception.Code is "map_unavailable" or "camera_unavailable")
            {
                // Selection is empty when the game is not currently presenting a playable map.
            }
        }

        return new GatewayExpandedGameStateSnapshot(control, camera, selection);
    }

    private GatewayHttpResponse AwaitJsonOperation<T>(
        GatewayDispatchOperation<T> pending,
        string requestId,
        string operation,
        Stopwatch stopwatch,
        CancellationTokenSource timeoutCancellation,
        CancellationToken requestCancellation)
    {
        var timeoutResponse = WaitForOperation(
            pending,
            requestId,
            operation,
            stopwatch,
            timeoutCancellation,
            requestCancellation);
        if (timeoutResponse is not null)
        {
            return timeoutResponse;
        }

        return Success(requestId, pending.Completion.GetAwaiter().GetResult(), stopwatch);
    }

    private GatewayHttpResponse? WaitForOperation<T>(
        GatewayDispatchOperation<T> pending,
        string requestId,
        string operation,
        Stopwatch stopwatch,
        CancellationTokenSource timeoutCancellation,
        CancellationToken requestCancellation,
        TimeSpan? operationTimeout = null)
    {
        var waitOutcome = WaitFor(
            pending.Completion,
            operationTimeout ?? responseTimeout,
            requestCancellation);
        if (waitOutcome == GatewayWaitOutcome.Completed)
        {
            return null;
        }

        if (waitOutcome == GatewayWaitOutcome.Cancelled)
        {
            pending.TryCancelBeforeStart();
            timeoutCancellation.Cancel();
            ObserveAbandonedCompletion(pending.Completion);
            throw new OperationCanceledException(
                "The HTTP client cancelled the gateway request.",
                requestCancellation);
        }

        var cancelledBeforeStart = pending.TryCancelBeforeStart();
        timeoutCancellation.Cancel();
        if (!cancelledBeforeStart)
        {
            var timeoutElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            logBuffer.Append(
                "Warning",
                $"Operation '{operation}' exceeded its HTTP response deadline after it started " +
                "and may still be running.",
                requestId: requestId);
            ObserveLateCompletion(
                pending.Completion,
                requestId,
                operation,
                stopwatch,
                timeoutElapsedMilliseconds);
        }

        var code = cancelledBeforeStart
            ? "timed_out_before_start"
            : "response_timeout_after_start";
        var timing = cancelledBeforeStart
            ? "expired before it started"
            : "did not respond after it started";
        return Error(
            504,
            "Gateway Timeout",
            requestId,
            code,
            $"Operation '{operation}' {timing} before the HTTP deadline.",
            stopwatch,
            retryable: true);
    }

    private static void ObserveAbandonedCompletion<T>(Task<T> completion)
    {
        _ = completion.ContinueWith(
            task => _ = task.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private void ObserveLateCompletion<T>(
        Task<T> completion,
        string requestId,
        string operation,
        Stopwatch stopwatch,
        long timeoutElapsedMilliseconds)
    {
        _ = completion.ContinueWith(
            task =>
            {
                var completionElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                var afterTimeoutMilliseconds = Math.Max(
                    0,
                    completionElapsedMilliseconds - timeoutElapsedMilliseconds);
                var timing = $"{completionElapsedMilliseconds} ms after request start, " +
                    $"{afterTimeoutMilliseconds} ms after timeout";
                if (task.IsFaulted)
                {
                    var aggregate = task.Exception!.Flatten();
                    var cause = aggregate.InnerExceptions.Count == 1
                        ? aggregate.InnerExceptions[0]
                        : aggregate;
                    logBuffer.Append(
                        "Error",
                        $"Operation '{operation}' completed after its HTTP response timed out ({timing}): {cause.Message}",
                        cause.ToString(),
                        requestId: requestId);
                    return;
                }

                if (task.IsCanceled)
                {
                    logBuffer.Append(
                        "Warning",
                        $"Operation '{operation}' was cancelled after its HTTP response timed out ({timing}).",
                        requestId: requestId);
                    return;
                }

                logBuffer.Append(
                    "Warning",
                    $"Operation '{operation}' completed successfully after its HTTP response timed out ({timing}).",
                    requestId: requestId);
            },
            CancellationToken.None,
            TaskContinuationOptions.None,
            TaskScheduler.Default);
    }

    private static GatewayWaitOutcome WaitFor(
        Task task,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var timeoutTask = Task.Delay(timeout);
        var cancellationTask = Task.Delay(Timeout.Infinite, cancellationToken);
        var winner = Task.WhenAny(task, timeoutTask, cancellationTask).GetAwaiter().GetResult();
        if (ReferenceEquals(winner, task))
        {
            return GatewayWaitOutcome.Completed;
        }

        return cancellationToken.IsCancellationRequested
            ? GatewayWaitOutcome.Cancelled
            : GatewayWaitOutcome.TimedOut;
    }

    private enum GatewayWaitOutcome
    {
        Completed,
        TimedOut,
        Cancelled
    }

    private static T DeserializeBody<T>(GatewayHttpRequest request)
    {
        try
        {
            return GatewayContractJson.Read<T>(Encoding.UTF8.GetString(request.Body));
        }
        catch (SerializationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new SerializationException("The request body is not valid JSON for this endpoint.", exception);
        }
    }

    private static GatewayDefExportApiRequest DeserializeDefExportBody(GatewayHttpRequest request)
    {
        string json;
        try
        {
            json = StrictUtf8.GetString(request.Body);
        }
        catch (DecoderFallbackException)
        {
            throw new GatewayDefExportException(
                "invalid_def_export_utf8",
                "The finalized Def export request body is not valid UTF-8.");
        }

        try
        {
            return GatewayContractJson.Read<GatewayDefExportApiRequest>(json);
        }
        catch (SerializationException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new SerializationException(
                "The request body is not valid JSON for finalized Def export.",
                exception);
        }
    }

    private static string ReadCSharpSource(GatewayHttpRequest request)
    {
        if (!request.Headers.TryGetValue("Content-Type", out var contentType) ||
            !IsUtf8PlainText(contentType))
        {
            throw new GatewayExecutionException(
                "unsupported_media_type",
                "C# submissions require Content-Type text/plain with UTF-8 source.");
        }

        if (request.Body.Length > MaximumCSharpSourceBytes)
        {
            throw new GatewayExecutionException(
                "csharp_source_too_large",
                $"C# source exceeds the {MaximumCSharpSourceBytes}-byte endpoint limit.");
        }

        try
        {
            var source = StrictUtf8.GetString(request.Body);
            return source.Length > 0 && source[0] == '\ufeff' ? source.Substring(1) : source;
        }
        catch (DecoderFallbackException exception)
        {
            throw new GatewayExecutionException(
                "invalid_utf8",
                "C# source must be valid UTF-8 text.",
                exception);
        }
    }

    private static bool IsUtf8PlainText(string contentType)
    {
        var segments = contentType.Split(';');
        if (segments.Length == 0 ||
            !string.Equals(segments[0].Trim(), "text/plain", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (segments.Length == 1)
        {
            return true;
        }

        if (segments.Length != 2)
        {
            return false;
        }

        var parameter = segments[1].Trim();
        var separator = parameter.IndexOf('=');
        if (separator <= 0 || separator == parameter.Length - 1 ||
            !string.Equals(parameter.Substring(0, separator).Trim(), "charset", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var charset = parameter.Substring(separator + 1).Trim().Trim('"');
        return string.Equals(charset, "utf-8", StringComparison.OrdinalIgnoreCase);
    }

    private static T Require<T>(T? service, string code) where T : class
    {
        if (service is null)
        {
            throw new GatewayCapabilityException(code, "This gateway capability is not configured.");
        }

        return service;
    }

    private static GatewayMouseButton ParseMouseButton(string value)
    {
        switch ((value ?? string.Empty).ToLowerInvariant())
        {
            case "left":
                return GatewayMouseButton.Left;
            case "right":
                return GatewayMouseButton.Right;
            case "middle":
                return GatewayMouseButton.Middle;
            default:
                throw new FormatException("Mouse button must be left, right, or middle.");
        }
    }

    private static GatewayHttpResponse Success(
        string requestId,
        object? result,
        Stopwatch stopwatch,
        int statusCode = 200,
        string reasonPhrase = "OK",
        GatewayJsonLimits? jsonLimits = null)
    {
        var envelope = new SortedDictionary<string, object?>
        {
            ["apiVersion"] = "1",
            ["durationMs"] = stopwatch.ElapsedMilliseconds,
            ["ok"] = true,
            ["requestId"] = requestId,
            ["result"] = result
        };
        return new GatewayHttpResponse(
            statusCode,
            reasonPhrase,
            "application/json; charset=utf-8",
            jsonLimits is null
                ? GatewayJsonWriter.Write(envelope)
                : GatewayJsonWriter.Write(
                    envelope,
                    jsonLimits.MaximumDepth,
                    jsonLimits.MaximumNodes,
                    jsonLimits.MaximumUtf8Bytes));
    }

    private static int MaximumDefExportResultUtf8Bytes(string requestId)
    {
        var worstCaseEnvelope = new SortedDictionary<string, object?>
        {
            ["apiVersion"] = "1",
            ["durationMs"] = long.MaxValue,
            ["ok"] = true,
            ["requestId"] = requestId,
            ["result"] = null
        };
        var envelopeWithNull = GatewayJsonWriter.Write(
            worstCaseEnvelope,
            MaximumDefExportResponseDepth,
            MaximumDefExportResponseNodes,
            GatewayDefExporter.MaximumSerializedPageUtf8Bytes);
        const int serializedNullUtf8Bytes = 4;
        var envelopeOverhead = envelopeWithNull.Length - serializedNullUtf8Bytes;
        return GatewayDefExporter.MaximumSerializedPageUtf8Bytes - envelopeOverhead;
    }

    private static int MaximumDefExportResultNodes(string requestId)
    {
        var envelopeWithNull = GatewayJsonWriter.Measure(
            new SortedDictionary<string, object?>
            {
                ["apiVersion"] = "1",
                ["durationMs"] = long.MaxValue,
                ["ok"] = true,
                ["requestId"] = requestId,
                ["result"] = null
            },
            MaximumDefExportResponseDepth,
            MaximumDefExportResponseNodes,
            GatewayDefExporter.MaximumSerializedPageUtf8Bytes);
        const int serializedNullNodes = 1;
        var envelopeOverhead = envelopeWithNull.Nodes - serializedNullNodes;
        return MaximumDefExportResponseNodes - envelopeOverhead;
    }

    private sealed class GatewayJsonLimits
    {
        public GatewayJsonLimits(int maximumDepth, int maximumNodes, int maximumUtf8Bytes)
        {
            MaximumDepth = maximumDepth;
            MaximumNodes = maximumNodes;
            MaximumUtf8Bytes = maximumUtf8Bytes;
        }

        public int MaximumDepth { get; }

        public int MaximumNodes { get; }

        public int MaximumUtf8Bytes { get; }
    }

    private static GatewayHttpResponse Error(
        int status,
        string reason,
        string requestId,
        string code,
        string message,
        Stopwatch stopwatch,
        bool retryable = false)
    {
        var error = new SortedDictionary<string, object?>
        {
            ["code"] = code,
            ["message"] = message,
            ["retryable"] = retryable
        };
        var envelope = new SortedDictionary<string, object?>
        {
            ["apiVersion"] = "1",
            ["durationMs"] = stopwatch.ElapsedMilliseconds,
            ["error"] = error,
            ["ok"] = false,
            ["requestId"] = requestId
        };
        return new GatewayHttpResponse(
            status,
            reason,
            "application/json; charset=utf-8",
            GatewayJsonWriter.Write(envelope));
    }

    private static Dictionary<string, string> ParseQuery(string query)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (string.IsNullOrEmpty(query))
        {
            return values;
        }

        foreach (var pair in query.Split('&'))
        {
            var separator = pair.IndexOf('=');
            var rawName = separator < 0 ? pair : pair.Substring(0, separator);
            var rawValue = separator < 0 ? string.Empty : pair.Substring(separator + 1);
            var name = Uri.UnescapeDataString(rawName.Replace('+', ' '));
            var value = Uri.UnescapeDataString(rawValue.Replace('+', ' '));
            if (name.Length == 0 || values.ContainsKey(name))
            {
                throw new FormatException("Query parameter names must be non-empty and unique.");
            }

            values.Add(name, value);
        }

        return values;
    }

    private static long ParseLong(
        IReadOnlyDictionary<string, string> query,
        string name,
        long defaultValue,
        long minimum)
    {
        if (!query.TryGetValue(name, out var text))
        {
            return defaultValue;
        }

        if (!long.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value < minimum)
        {
            throw new FormatException($"Query parameter '{name}' is invalid.");
        }

        return value;
    }
}
