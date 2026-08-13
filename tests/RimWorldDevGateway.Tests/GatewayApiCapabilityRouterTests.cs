using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayApiCapabilityRouterTests
{
    [Test]
    public void Csharp_endpoint_evaluates_plain_text_on_the_dispatcher_thread()
    {
        var dispatcher = new GatewayDispatcher();
        var evaluator = new GatewayCSharpEvaluator();
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(csharpEvaluator: evaluator),
            responseTimeout: TimeSpan.FromSeconds(2));
        var mainThreadId = Thread.CurrentThread.ManagedThreadId;

        var responseTask = Task.Run(() => router.Handle(
            TextPost("/api/v1/executions/csharp", "Thread.CurrentThread.ManagedThreadId"),
            "csharp-route"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.Update);
        var response = responseTask.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(body, Does.Contain("\"requestId\":\"csharp-route\""));
            Assert.That(body, Does.Contain("\"Succeeded\":true"));
            Assert.That(body, Does.Contain("\"Value\":\"" + mainThreadId + "\""));
            Assert.That(body, Does.Contain("\"Type\":\"System.Int32\""));
        });
    }

    [Test]
    public void Csharp_endpoint_rejects_non_text_content_before_dispatch()
    {
        var dispatcher = new GatewayDispatcher();
        var router = CSharpRouter(dispatcher);

        var response = router.Handle(
            RawPost(
                "/api/v1/executions/csharp",
                "application/json",
                Encoding.UTF8.GetBytes("1 + 2")),
            "csharp-media");
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(400));
            Assert.That(body, Does.Contain("\"code\":\"unsupported_media_type\""));
            Assert.That(dispatcher.PendingCount, Is.Zero);
        });
    }

    [Test]
    public void Csharp_endpoint_rejects_a_non_utf8_declared_charset_before_dispatch()
    {
        var dispatcher = new GatewayDispatcher();
        var router = CSharpRouter(dispatcher);

        var response = router.Handle(
            RawPost(
                "/api/v1/executions/csharp",
                "text/plain; charset=iso-8859-1",
                Encoding.UTF8.GetBytes("1 + 2")),
            "csharp-charset");
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(400));
            Assert.That(body, Does.Contain("\"code\":\"unsupported_media_type\""));
            Assert.That(dispatcher.PendingCount, Is.Zero);
        });
    }

    [Test]
    public void Csharp_endpoint_rejects_invalid_utf8_before_dispatch()
    {
        var dispatcher = new GatewayDispatcher();
        var router = CSharpRouter(dispatcher);

        var response = router.Handle(
            RawPost(
                "/api/v1/executions/csharp",
                "text/plain; charset=utf-8",
                new byte[] { 0xc3, 0x28 }),
            "csharp-utf8");
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(400));
            Assert.That(body, Does.Contain("\"code\":\"invalid_utf8\""));
            Assert.That(dispatcher.PendingCount, Is.Zero);
        });
    }

    [Test]
    public void Csharp_endpoint_accepts_and_strips_one_utf8_bom()
    {
        var dispatcher = new GatewayDispatcher();
        var router = CSharpRouter(dispatcher);
        var source = new byte[] { 0xef, 0xbb, 0xbf }
            .Concat(Encoding.UTF8.GetBytes("1 + 2"))
            .ToArray();

        var responseTask = Task.Run(() => router.Handle(
            RawPost("/api/v1/executions/csharp", "text/plain", source),
            "csharp-bom"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.Update);
        var response = responseTask.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(body, Does.Contain("\"Succeeded\":true"));
            Assert.That(body, Does.Contain("\"Value\":\"3\""));
        });
    }

    [Test]
    public void Csharp_endpoint_rejects_more_than_64_kib_before_dispatch()
    {
        var dispatcher = new GatewayDispatcher();
        var router = CSharpRouter(dispatcher);

        var response = router.Handle(
            RawPost(
                "/api/v1/executions/csharp",
                "text/plain; charset=utf-8",
                new byte[(64 * 1024) + 1]),
            "csharp-byte-limit");
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(400));
            Assert.That(body, Does.Contain("\"code\":\"csharp_source_too_large\""));
            Assert.That(dispatcher.PendingCount, Is.Zero);
        });
    }

    [Test]
    public void Uploaded_assembly_endpoint_executes_on_the_dispatcher_and_returns_its_bounded_result()
    {
        var dispatcher = new GatewayDispatcher();
        var executor = new GatewayAssemblyExecutor(4 * 1024 * 1024);
        var services = new GatewayApiServices(assemblyExecutor: executor);
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            services,
            responseTimeout: TimeSpan.FromSeconds(2));
        var payload = new GatewayAssemblyExecutionRequest
        {
            AssemblyBase64 = Convert.ToBase64String(
                File.ReadAllBytes(typeof(UploadedAssemblyFixture).Assembly.Location)),
            EntryType = typeof(UploadedAssemblyFixture).FullName!,
            EntryMethod = nameof(UploadedAssemblyFixture.Execute),
            RequestJson = "{\"api\":true}"
        };
        var request = Post("/api/v1/executions/assembly", GatewayContractJson.Write(payload));

        var responseTask = Task.Run(() => router.Handle(request, "execution-route"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.Update);
        var response = responseTask.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(body, Does.Contain("\"ok\":true"));
            Assert.That(body, Does.Contain("\"requestId\":\"execution-route\""));
            Assert.That(body, Does.Contain("\"Truncated\":false"));
            Assert.That(body, Does.Contain("\"OriginalUtf8Bytes\":"));
            Assert.That(body, Does.Contain(":{\\\"api\\\":true}"));
            Assert.That(executor.UploadCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Uploaded_assembly_context_observes_request_cancellation()
    {
        var dispatcher = new GatewayDispatcher();
        var executor = new GatewayAssemblyExecutor(4 * 1024 * 1024);
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(assemblyExecutor: executor),
            responseTimeout: TimeSpan.FromSeconds(5));
        var payload = new GatewayAssemblyExecutionRequest
        {
            AssemblyBase64 = Convert.ToBase64String(
                File.ReadAllBytes(typeof(UploadedAssemblyFixture).Assembly.Location)),
            EntryType = typeof(UploadedAssemblyFixture).FullName!,
            EntryMethod = nameof(UploadedAssemblyFixture.WaitForCancellation),
            RequestJson = "{}"
        };
        using var cancellation = new CancellationTokenSource();
        var request = Post(
            "/api/v1/executions/assembly",
            GatewayContractJson.Write(payload),
            cancellation.Token);

        var responseTask = Task.Run(() => router.Handle(request, "cancelled-assembly-route"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));
        dispatcher.Drain(DispatchPhase.Update);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => responseTask.GetAwaiter().GetResult(),
                Throws.TypeOf<OperationCanceledException>());
            Assert.That(dispatcher.PendingCount, Is.Zero);
        });
    }

    [Test]
    public void Uploaded_session_automation_observes_started_request_cancellation()
    {
        var dispatcher = new GatewayDispatcher();
        var automations = new GatewayAutomationRegistry(runIdFactory: () => "uploaded-cancel-run");
        var handlerStarted = new ManualResetEventSlim(false);
        var handlerObservedCancellation = false;
        new GatewayAssemblyRuntimeExtensions(automations).RegisterSessionAutomation(
            new GatewayAssemblyAutomationDescriptor(
                "uploaded.wait-for-cancellation",
                "1",
                "Waits for request cancellation.",
                mutating: false),
            (_, cancellationToken) =>
            {
                handlerStarted.Set();
                cancellationToken.WaitHandle.WaitOne(TimeSpan.FromSeconds(5));
                handlerObservedCancellation = cancellationToken.IsCancellationRequested;
                cancellationToken.ThrowIfCancellationRequested();
                return "not-cancelled";
            });
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(automations: automations),
            responseTimeout: TimeSpan.FromSeconds(5));
        using var cancellation = new CancellationTokenSource();
        var request = Post(
            "/api/v1/automations/uploaded.wait-for-cancellation/runs",
            "{\"arguments\":{}}",
            cancellation.Token);

        var responseTask = Task.Run(() => router.Handle(request, "cancelled-uploaded-automation"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        cancellation.CancelAfter(TimeSpan.FromMilliseconds(100));
        dispatcher.Drain(DispatchPhase.Update);

        Assert.Multiple(() =>
        {
            Assert.That(handlerStarted.IsSet, Is.True);
            Assert.That(handlerObservedCancellation, Is.True);
            Assert.That(automations.ListRuns().Single().State, Is.EqualTo("cancelled"));
            Assert.That(
                () => responseTask.GetAwaiter().GetResult(),
                Throws.TypeOf<OperationCanceledException>());
            Assert.That(dispatcher.PendingCount, Is.Zero);
        });
    }

    [Test]
    public void Screenshot_endpoint_waits_for_end_of_frame_and_returns_png_bytes()
    {
        var dispatcher = new GatewayDispatcher();
        var png = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
        var screenshot = new GatewayScreenshotService(
            dispatcher,
            new ScreenshotBackend(png),
            maximumPngBytes: 1024);
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(screenshotService: screenshot),
            responseTimeout: TimeSpan.FromSeconds(2));

        var responseTask = Task.Run(() => router.Handle(
            Post("/api/v1/screenshots", "{}"),
            "screenshot-route"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        Assert.That(dispatcher.Drain(DispatchPhase.Update), Is.Zero);
        dispatcher.Drain(DispatchPhase.EndOfFrame);
        var response = responseTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(response.ContentType, Is.EqualTo("image/png"));
            Assert.That(response.Body, Is.EqualTo(png));
        });
    }

    [Test]
    public void Screenshot_endpoint_deserializes_exact_target_handles_and_padding()
    {
        var dispatcher = new GatewayDispatcher();
        var png = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
        var backend = new ScreenshotBackend(png);
        var screenshot = new GatewayScreenshotService(dispatcher, backend, maximumPngBytes: 1024);
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(screenshotService: screenshot),
            responseTimeout: TimeSpan.FromSeconds(2));

        var responseTask = Task.Run(() => router.Handle(
            Post(
                "/api/v1/screenshots",
                "{\"thingHandles\":[\"Pawn_42\",\"Building_9\"],\"paddingPixels\":24}"),
            "screenshot-target-route"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.EndOfFrame);
        var response = responseTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(backend.Request, Is.Not.Null);
            Assert.That(backend.Request!.ThingHandles, Is.EqualTo(new[] { "Pawn_42", "Building_9" }));
            Assert.That(backend.Request.PaddingPixels, Is.EqualTo(24));
        });
    }

    [Test]
    public void Camera_crop_endpoint_returns_the_exact_applied_rectangle_without_target_projection()
    {
        var dispatcher = new GatewayDispatcher();
        var png = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
        var screenshot = new GatewayScreenshotService(
            dispatcher,
            new DescribedScreenshotBackend(png),
            maximumPngBytes: 1024);
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(screenshotService: screenshot),
            responseTimeout: TimeSpan.FromSeconds(2));

        var responseTask = Task.Run(() => router.Handle(
            Post(
                "/api/v1/screenshots",
                "{\"widthPixels\":960,\"heightPixels\":540,\"offsetXPixels\":120,\"offsetYPixels\":-40}"),
            "screenshot-camera-route"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        dispatcher.Drain(DispatchPhase.EndOfFrame);
        var response = responseTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200));
            Assert.That(response.Body, Is.EqualTo(png));
            Assert.That(response.Headers["X-Gateway-Frame-Width"], Is.EqualTo("1600"));
            Assert.That(response.Headers["X-Gateway-Frame-Height"], Is.EqualTo("900"));
            Assert.That(response.Headers["X-Gateway-Crop-X"], Is.EqualTo("320"));
            Assert.That(response.Headers["X-Gateway-Crop-Y"], Is.EqualTo("180"));
            Assert.That(response.Headers["X-Gateway-Crop-Width"], Is.EqualTo("960"));
            Assert.That(response.Headers["X-Gateway-Crop-Height"], Is.EqualTo("540"));
        });
    }

    [Test]
    public void Screenshot_endpoint_rejects_invalid_target_padding_as_a_client_error_without_dispatch()
    {
        var dispatcher = new GatewayDispatcher();
        var png = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
        var backend = new ScreenshotBackend(png);
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(
                screenshotService: new GatewayScreenshotService(
                    dispatcher,
                    backend,
                    maximumPngBytes: 1024)),
            responseTimeout: TimeSpan.FromSeconds(2));

        var response = router.Handle(
            Post(
                "/api/v1/screenshots",
                "{\"thingHandles\":[\"Pawn_42\"],\"paddingPixels\":-1}"),
            "screenshot-invalid-padding");
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(400));
            Assert.That(body, Does.Contain("\"code\":\"invalid_screenshot_request\""));
            Assert.That(dispatcher.PendingCount, Is.Zero);
            Assert.That(backend.CaptureCount, Is.Zero);
        });
    }

    [Test]
    public void Semantic_mutation_queued_past_http_deadline_is_cancelled_before_start()
    {
        var dispatcher = new GatewayDispatcher();
        var operations = new SemanticActionOperations();
        var semanticActions = new GatewaySemanticActionRegistry(
            dispatcher,
            operations,
            dispatchTimeout: TimeSpan.FromSeconds(2));
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(semanticActions: semanticActions),
            responseTimeout: TimeSpan.FromMilliseconds(75));

        var responseTask = Task.Run(() => router.Handle(
            Post("/api/v1/actions/game.pause", "{\"arguments\":{\"paused\":true}}"),
            "queued-semantic-mutation"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        var response = responseTask.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);
        dispatcher.Drain(DispatchPhase.Update);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(504));
            Assert.That(body, Does.Contain("\"code\":\"timed_out_before_start\""));
            Assert.That(operations.SetPausedCount, Is.Zero);
            Assert.That(dispatcher.PendingCount, Is.Zero);
        });
    }

    [Test]
    public void Semantic_discovery_queued_past_http_deadline_is_cancelled_before_start()
    {
        var dispatcher = new GatewayDispatcher();
        var operations = new SemanticActionOperations();
        var semanticActions = new GatewaySemanticActionRegistry(
            dispatcher,
            operations,
            dispatchTimeout: TimeSpan.FromSeconds(2));
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(semanticActions: semanticActions),
            responseTimeout: TimeSpan.FromMilliseconds(75));

        var responseTask = Task.Run(() => router.Handle(
            Get("/api/v1/actions"),
            "queued-semantic-discovery"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        var response = responseTask.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);
        dispatcher.Drain(DispatchPhase.Update);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(504));
            Assert.That(body, Does.Contain("\"code\":\"timed_out_before_start\""));
            Assert.That(operations.AvailabilityReadCount, Is.Zero);
        });
    }

    [Test]
    public void Screenshot_queued_past_http_deadline_is_cancelled_before_capture()
    {
        var dispatcher = new GatewayDispatcher();
        var backend = new ScreenshotBackend(new byte[] { 0x89, 0x50, 0x4e, 0x47 });
        var screenshot = new GatewayScreenshotService(dispatcher, backend, maximumPngBytes: 1024);
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(screenshotService: screenshot),
            responseTimeout: TimeSpan.FromMilliseconds(75));

        var responseTask = Task.Run(() => router.Handle(
            Post("/api/v1/screenshots", "{}"),
            "queued-screenshot"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        var response = responseTask.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);
        var followUp = screenshot.CaptureOperation(
            "screenshot-after-queued-timeout",
            TimeSpan.FromSeconds(2));
        var followUpAccepted = !followUp.Completion.IsCompleted;
        if (followUpAccepted)
        {
            followUp.TryCancelBeforeStart();
        }

        dispatcher.Drain(DispatchPhase.EndOfFrame, maximumItems: 2);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(504));
            Assert.That(body, Does.Contain("\"code\":\"timed_out_before_start\""));
            Assert.That(backend.CaptureCount, Is.Zero);
            Assert.That(followUpAccepted, Is.True, "capture capacity should be released before a Unity drain");
            Assert.That(dispatcher.PendingCount, Is.Zero);
        });
    }

    [Test]
    public void Semantic_mutation_that_started_before_http_deadline_reports_after_start_timeout()
    {
        var dispatcher = new GatewayDispatcher();
        var operations = new SemanticActionOperations(blockSetPaused: true);
        var semanticActions = new GatewaySemanticActionRegistry(
            dispatcher,
            operations,
            dispatchTimeout: TimeSpan.FromSeconds(2));
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(semanticActions: semanticActions),
            responseTimeout: TimeSpan.FromMilliseconds(75));

        var responseTask = Task.Run(() => router.Handle(
            Post("/api/v1/actions/game.pause", "{\"arguments\":{\"paused\":true}}"),
            "started-semantic-mutation"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        var drainTask = Task.Run(() => dispatcher.Drain(DispatchPhase.Update));
        Assert.That(operations.SetPausedStarted.Wait(1000), Is.True);
        var response = responseTask.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);
        operations.ReleaseSetPaused.Set();
        drainTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(504));
            Assert.That(body, Does.Contain("\"code\":\"response_timeout_after_start\""));
            Assert.That(operations.SetPausedCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Screenshot_that_started_before_http_deadline_reports_after_start_timeout()
    {
        var dispatcher = new GatewayDispatcher();
        var png = new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };
        var backend = new ScreenshotBackend(png, blockCapture: true);
        var screenshot = new GatewayScreenshotService(dispatcher, backend, maximumPngBytes: 1024);
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(screenshotService: screenshot),
            responseTimeout: TimeSpan.FromMilliseconds(75));

        var responseTask = Task.Run(() => router.Handle(
            Post("/api/v1/screenshots", "{}"),
            "started-screenshot"));
        Assert.That(SpinWait.SpinUntil(() => dispatcher.PendingCount == 1, 1000), Is.True);
        var drainTask = Task.Run(() => dispatcher.Drain(DispatchPhase.EndOfFrame));
        Assert.That(backend.CaptureStarted.Wait(1000), Is.True);
        var response = responseTask.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);
        backend.ReleaseCapture.Set();
        drainTask.GetAwaiter().GetResult();

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(504));
            Assert.That(body, Does.Contain("\"code\":\"response_timeout_after_start\""));
            Assert.That(backend.CaptureCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Timed_drag_runs_on_the_http_worker_without_blocking_the_dispatcher()
    {
        var dispatcher = new GatewayDispatcher();
        var platform = InputPlatform.Ready();
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(
                windowsInput: new GatewayWindowsInput(platform, processId: 77)),
            responseTimeout: TimeSpan.FromMilliseconds(75));
        var requestThreadId = 0;

        var response = Task.Run(() =>
        {
            requestThreadId = Thread.CurrentThread.ManagedThreadId;
            return router.Handle(
                Post(
                    "/api/v1/input/drag",
                    "{\"startX\":10,\"startY\":20,\"endX\":30,\"endY\":40," +
                    "\"button\":\"left\",\"durationMs\":25,\"steps\":2,\"activate\":false}"),
                "direct-drag");
        }).GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200), body);
            Assert.That(dispatcher.PendingCount, Is.Zero);
            Assert.That(platform.DelayThreadIds, Is.Not.Empty);
            Assert.That(platform.DelayThreadIds, Has.All.EqualTo(requestThreadId));
            Assert.That(platform.DelayThreadIds, Has.None.EqualTo(Thread.CurrentThread.ManagedThreadId));
        });
    }

    [Test]
    public void Click_runs_on_the_http_worker_without_blocking_the_dispatcher()
    {
        var dispatcher = new GatewayDispatcher();
        var platform = InputPlatform.Ready();
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(
                windowsInput: new GatewayWindowsInput(platform, processId: 77)),
            responseTimeout: TimeSpan.FromSeconds(2));
        var requestThreadId = 0;
        using var requestStarted = new ManualResetEventSlim(false);

        var responseTask = Task.Run(() =>
        {
            requestThreadId = Thread.CurrentThread.ManagedThreadId;
            requestStarted.Set();
            return router.Handle(
                Post(
                    "/api/v1/input/click",
                    "{\"x\":10,\"y\":20,\"button\":\"left\",\"activate\":false}"),
                "direct-click");
        });
        Assert.That(requestStarted.Wait(TimeSpan.FromSeconds(1)), Is.True);
        var completedWithoutDrain = responseTask.Wait(TimeSpan.FromMilliseconds(500));
        if (!completedWithoutDrain)
        {
            dispatcher.Drain(DispatchPhase.Update);
        }

        var response = responseTask.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);

        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200), body);
            Assert.That(completedWithoutDrain, Is.True);
            Assert.That(dispatcher.PendingCount, Is.Zero);
            Assert.That(platform.InjectionThreadIds, Is.Not.Empty);
            Assert.That(platform.InjectionThreadIds, Has.All.EqualTo(requestThreadId));
            Assert.That(platform.InjectionThreadIds, Has.None.EqualTo(Thread.CurrentThread.ManagedThreadId));
        });
    }

    [Test]
    public void Text_input_runs_on_the_http_worker_without_blocking_the_dispatcher()
    {
        AssertRawInputRunsWithoutDispatcher(
            "/api/v1/input/keys",
            "{\"text\":\"Hi\",\"activate\":false}",
            "direct-text");
    }

    [Test]
    public void Chord_input_runs_on_the_http_worker_without_blocking_the_dispatcher()
    {
        AssertRawInputRunsWithoutDispatcher(
            "/api/v1/input/keys",
            "{\"key\":\"S\",\"modifiers\":[\"Control\"],\"activate\":false}",
            "direct-chord");
    }

    [Test]
    public void Cancelling_the_http_request_interrupts_a_timed_drag_and_releases_its_button()
    {
        var dispatcher = new GatewayDispatcher();
        var platform = InputPlatform.Ready();
        platform.BlockDelay = true;
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(
                windowsInput: new GatewayWindowsInput(platform, processId: 77)),
            responseTimeout: TimeSpan.FromSeconds(15));
        using var cancellation = new CancellationTokenSource();
        var request = Post(
            "/api/v1/input/drag",
            "{\"startX\":10,\"startY\":20,\"endX\":30,\"endY\":40," +
            "\"button\":\"left\",\"durationMs\":5000,\"steps\":1,\"activate\":false}",
            cancellation.Token);
        var responseTask = Task.Run(() => router.Handle(request, "cancelled-direct-drag"));
        Assert.That(platform.DelayStarted.Wait(TimeSpan.FromSeconds(1)), Is.True);

        cancellation.Cancel();
        var completedPromptly = SpinWait.SpinUntil(
            () => responseTask.IsCompleted,
            TimeSpan.FromSeconds(1));
        try
        {
            Assert.Multiple(() =>
            {
                Assert.That(completedPromptly, Is.True);
                Assert.That(
                    () => responseTask.GetAwaiter().GetResult(),
                    Throws.TypeOf<OperationCanceledException>());
                Assert.That(platform.MouseButtonStates, Is.EqualTo(new[] { true, false }));
                Assert.That(dispatcher.PendingCount, Is.Zero);
            });
        }
        finally
        {
            platform.ReleaseDelay.Set();
        }
    }

    private static void AssertRawInputRunsWithoutDispatcher(
        string path,
        string json,
        string requestId)
    {
        var dispatcher = new GatewayDispatcher();
        var platform = InputPlatform.Ready();
        var router = new GatewayApiRouter(
            dispatcher,
            new StubStateProvider(),
            new GatewayLogBuffer(),
            new GatewayApiServices(
                windowsInput: new GatewayWindowsInput(platform, processId: 77)),
            responseTimeout: TimeSpan.FromSeconds(2));
        var requestThreadId = 0;
        using var requestStarted = new ManualResetEventSlim(false);

        var responseTask = Task.Run(() =>
        {
            requestThreadId = Thread.CurrentThread.ManagedThreadId;
            requestStarted.Set();
            return router.Handle(Post(path, json), requestId);
        });
        Assert.That(requestStarted.Wait(TimeSpan.FromSeconds(1)), Is.True);
        var completedWithoutDrain = responseTask.Wait(TimeSpan.FromMilliseconds(500));
        if (!completedWithoutDrain)
        {
            dispatcher.Drain(DispatchPhase.Update);
        }

        var response = responseTask.GetAwaiter().GetResult();
        var body = Encoding.UTF8.GetString(response.Body);
        Assert.Multiple(() =>
        {
            Assert.That(response.StatusCode, Is.EqualTo(200), body);
            Assert.That(completedWithoutDrain, Is.True);
            Assert.That(dispatcher.PendingCount, Is.Zero);
            Assert.That(platform.InjectionThreadIds, Is.Not.Empty);
            Assert.That(platform.InjectionThreadIds, Has.All.EqualTo(requestThreadId));
            Assert.That(platform.InjectionThreadIds, Has.None.EqualTo(Thread.CurrentThread.ManagedThreadId));
        });
    }

    private static GatewayHttpRequest Post(
        string path,
        string json,
        CancellationToken cancellationToken = default) => new(
        "POST",
        path,
        path,
        string.Empty,
        new Dictionary<string, string>(),
        Encoding.UTF8.GetBytes(json),
        cancellationToken);

    private static GatewayHttpRequest Get(string path) => new(
        "GET",
        path,
        path,
        string.Empty,
        new Dictionary<string, string>(),
        Array.Empty<byte>());

    private static GatewayHttpRequest TextPost(string path, string source) => new(
        "POST",
        path,
        path,
        string.Empty,
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Content-Type"] = "text/plain; charset=utf-8"
        },
        Encoding.UTF8.GetBytes(source));

    private static GatewayHttpRequest RawPost(string path, string contentType, byte[] body) => new(
        "POST",
        path,
        path,
        string.Empty,
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Content-Type"] = contentType
        },
        body);

    private static GatewayApiRouter CSharpRouter(GatewayDispatcher dispatcher) => new(
        dispatcher,
        new StubStateProvider(),
        new GatewayLogBuffer(),
        new GatewayApiServices(csharpEvaluator: new GatewayCSharpEvaluator()),
        responseTimeout: TimeSpan.FromSeconds(2));

    private sealed class StubStateProvider : IGatewayStateProvider
    {
        public object CaptureStatus() => new { State = "Entry" };

        public object CaptureUiState() => new { };
    }

    private sealed class ScreenshotBackend : IGatewayScreenshotBackend, IGatewayTargetedScreenshotBackend
    {
        private readonly byte[] png;
        private readonly bool blockCapture;

        public ScreenshotBackend(byte[] png, bool blockCapture = false)
        {
            this.png = png;
            this.blockCapture = blockCapture;
        }

        public int CaptureCount { get; private set; }

        public GatewayScreenshotRequest? Request { get; private set; }

        public ManualResetEventSlim CaptureStarted { get; } = new(false);

        public ManualResetEventSlim ReleaseCapture { get; } = new(false);

        public object Capture()
        {
            CaptureCount++;
            CaptureStarted.Set();
            if (blockCapture)
            {
                ReleaseCapture.Wait(TimeSpan.FromSeconds(5));
            }

            return new object();
        }

        public object Capture(GatewayScreenshotRequest request)
        {
            Request = request;
            return Capture();
        }

        public byte[] EncodePng(object resource) => png;

        public void Destroy(object resource)
        {
        }
    }

    private sealed class DescribedScreenshotBackend : IGatewayScreenshotBackend, IGatewayDescribedScreenshotBackend
    {
        private readonly byte[] png;

        public DescribedScreenshotBackend(byte[] png) => this.png = png;

        public object Capture() => throw new AssertionException("The described capture seam was bypassed.");

        public GatewayScreenshotResource CaptureDescribed(GatewayScreenshotRequest request)
        {
            Assert.That(request.ThingHandles, Is.Empty);
            Assert.That(request.WidthPixels, Is.EqualTo(960));
            return new GatewayScreenshotResource(
                new object(),
                frameWidth: 1600,
                frameHeight: 900,
                crop: new GatewayScreenshotCrop(320, 180, 960, 540));
        }

        public byte[] EncodePng(object resource) => png;

        public void Destroy(object resource)
        {
        }
    }

    private sealed class SemanticActionOperations : IGatewaySemanticActionOperations
    {
        private readonly bool blockSetPaused;

        public SemanticActionOperations(bool blockSetPaused = false)
        {
            this.blockSetPaused = blockSetPaused;
        }

        public int SetPausedCount { get; private set; }

        public int AvailabilityReadCount { get; private set; }

        public ManualResetEventSlim SetPausedStarted { get; } = new(false);

        public ManualResetEventSlim ReleaseSetPaused { get; } = new(false);

        public bool IsPaused { get; private set; }

        public bool ForcePaused => false;

        public GatewayGameSpeed CurrentSpeed { get; private set; } = GatewayGameSpeed.Normal;

        public string? ActiveWindowType => null;

        public bool DebugToolActive => false;

        public GatewaySemanticActionAvailability GetGameAvailability()
        {
            AvailabilityReadCount++;
            return GatewaySemanticActionAvailability.Available();
        }

        public GatewaySemanticActionAvailability GetWindowAvailability()
        {
            AvailabilityReadCount++;
            return GatewaySemanticActionAvailability.Unavailable("No window is open.");
        }

        public GatewaySemanticActionAvailability GetDebugToolAvailability()
        {
            AvailabilityReadCount++;
            return GatewaySemanticActionAvailability.Unavailable(
                "No native debug tool is active.");
        }

        public void SetPaused(bool paused)
        {
            SetPausedCount++;
            SetPausedStarted.Set();
            if (blockSetPaused)
            {
                ReleaseSetPaused.Wait(TimeSpan.FromSeconds(5));
            }

            IsPaused = paused;
        }

        public void SetSpeed(GatewayGameSpeed speed) => CurrentSpeed = speed;

        public void AcceptWindow()
        {
        }

        public void CancelWindow()
        {
        }

        public void CancelDebugTool()
        {
        }
    }

    private sealed class InputPlatform : IGatewayWindowsInputPlatform
    {
        public bool IsSupported => true;

        public IReadOnlyList<int> DelayThreadIds => delayThreadIds;

        public IReadOnlyList<int> InjectionThreadIds => injectionThreadIds;

        public IReadOnlyList<bool> MouseButtonStates => mouseButtonStates;

        private readonly List<int> delayThreadIds = new();

        private readonly List<int> injectionThreadIds = new();

        private readonly List<bool> mouseButtonStates = new();

        public bool BlockDelay { get; set; }

        public ManualResetEventSlim DelayStarted { get; } = new(false);

        public ManualResetEventSlim ReleaseDelay { get; } = new(false);

        public static InputPlatform Ready() => new();

        public IntPtr GetMainWindowHandle(int processId) => new(42);

        public bool IsWindow(IntPtr window) => window == new IntPtr(42);

        public int GetWindowProcessId(IntPtr window) => 77;

        public bool IsMinimized(IntPtr window) => false;

        public bool RestoreWindow(IntPtr window) => true;

        public IDisposable? TryAcquireForegroundWindow(IntPtr window) => new NoopDisposable();

        public IntPtr GetForegroundWindow() => new(42);

        private sealed class NoopDisposable : IDisposable
        {
            public void Dispose()
            {
            }
        }

        public bool TryGetClientRect(IntPtr window, out GatewayNativeRect rectangle)
        {
            rectangle = new GatewayNativeRect(0, 0, 800, 600);
            return true;
        }

        public bool TryClientToScreen(
            IntPtr window,
            GatewayClientPoint client,
            out GatewayClientPoint screen)
        {
            screen = client;
            return true;
        }

        public bool SendMouseMove(int screenX, int screenY)
        {
            injectionThreadIds.Add(Thread.CurrentThread.ManagedThreadId);
            return true;
        }

        public bool SendMouseButton(GatewayMouseButton button, bool down)
        {
            injectionThreadIds.Add(Thread.CurrentThread.ManagedThreadId);
            mouseButtonStates.Add(down);
            return true;
        }

        public bool SendVirtualKey(ushort virtualKey, bool down)
        {
            injectionThreadIds.Add(Thread.CurrentThread.ManagedThreadId);
            return true;
        }

        public bool SendUnicode(char character, bool down)
        {
            injectionThreadIds.Add(Thread.CurrentThread.ManagedThreadId);
            return true;
        }

        public void Delay(TimeSpan duration, CancellationToken cancellationToken)
        {
            delayThreadIds.Add(Thread.CurrentThread.ManagedThreadId);
            DelayStarted.Set();
            if (BlockDelay)
            {
                var outcome = WaitHandle.WaitAny(
                    new[] { ReleaseDelay.WaitHandle, cancellationToken.WaitHandle },
                    TimeSpan.FromSeconds(5));
                if (outcome == WaitHandle.WaitTimeout)
                {
                    throw new TimeoutException("The test did not release the blocked input delay.");
                }
            }

            cancellationToken.ThrowIfCancellationRequested();
        }
    }
}
