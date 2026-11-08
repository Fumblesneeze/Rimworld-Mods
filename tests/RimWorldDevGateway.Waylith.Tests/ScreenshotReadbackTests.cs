using NUnit.Framework;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class ScreenshotReadbackTests
{
    [Test]
    public async Task Deadline_completes_even_when_no_end_of_frame_is_drained()
    {
        var dispatcher = new GatewayDispatcher();
        var backend = new ReadbackBackend();
        var service = new GatewayScreenshotService(dispatcher, backend, 1024);
        var capture = service.CaptureOperation("no-frame", TimeSpan.FromMilliseconds(100));
        var error = Assert.ThrowsAsync<GatewayScreenshotException>(async () =>
            await capture.Completion.WaitAsync(TimeSpan.FromSeconds(3)));
        Assert.That(error!.Code, Is.EqualTo("screenshot_render_timeout"));
        var next = service.CaptureOperation("next", TimeSpan.FromSeconds(5));
        Assert.That(next.Completion.IsCompleted, Is.False);
        next.TryCancelBeforeStart();
        try { await next.Completion; } catch (GatewayScreenshotException) { }
        dispatcher.Drain(DispatchPhase.EndOfFrame, 8);
        Assert.That(backend.Reads, Is.Zero);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task Cancellation_after_admission_is_terminal_and_releases_owned_resources(bool duringReadback)
    {
        var dispatcher = new GatewayDispatcher();
        using var cancellation = new CancellationTokenSource();
        var backend = new ReadbackBackend { Available = true };
        var service = new GatewayScreenshotService(dispatcher, backend, 1024);
        var capture = service.CaptureOperation("cancel", TimeSpan.FromSeconds(5), cancellation.Token);
        dispatcher.Drain(DispatchPhase.EndOfFrame);
        await AwaitQueued(dispatcher);
        if (duringReadback)
        {
            backend.OnCapture = cancellation.Cancel;
            backend.Frame++;
            dispatcher.Drain(DispatchPhase.EndOfFrame);
        }
        else cancellation.Cancel();
        var error = Assert.ThrowsAsync<GatewayScreenshotException>(async () =>
            await capture.Completion.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.That(error!.Code, Is.EqualTo("screenshot_cancelled"));
        Assert.That(backend.Encoded, Is.Zero);
        Assert.That(backend.Destroyed, Is.EqualTo(duringReadback ? 1 : 0));
    }

    [Test]
    public async Task Shutdown_rejects_pending_readback_without_fabricating_evidence()
    {
        var dispatcher = new GatewayDispatcher();
        var backend = new ReadbackBackend();
        var service = new GatewayScreenshotService(dispatcher, backend, 1024);
        var capture = service.CaptureOperation("shutdown", TimeSpan.FromSeconds(5));
        dispatcher.Drain(DispatchPhase.EndOfFrame);
        await AwaitQueued(dispatcher);
        dispatcher.Stop();
        var error = Assert.ThrowsAsync<GatewayDispatchException>(async () =>
            await capture.Completion.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.That(error!.Code, Is.EqualTo("gateway_stopping"));
        Assert.That(backend.Encoded, Is.Zero);
        var next = service.CaptureOperation("after-stop", TimeSpan.FromSeconds(5));
        Assert.ThrowsAsync<GatewayDispatchException>(async () => await next.Completion);
    }

    [Test]
    public async Task Mismatching_frame_proof_is_released_without_encoding()
    {
        var dispatcher = new GatewayDispatcher();
        var backend = new ReadbackBackend { Available = true, ProofFrameOffset = -1 };
        var service = new GatewayScreenshotService(dispatcher, backend, 1024);
        var capture = service.CaptureOperation("unproven", TimeSpan.FromSeconds(5));
        dispatcher.Drain(DispatchPhase.EndOfFrame);
        await AwaitQueued(dispatcher);
        backend.Frame++;
        dispatcher.Drain(DispatchPhase.EndOfFrame);
        var error = Assert.ThrowsAsync<GatewayScreenshotException>(async () => await capture.Completion);
        Assert.That(error!.Code, Is.EqualTo("screenshot_readback_unproven"));
        Assert.That(backend.Encoded, Is.Zero);
        Assert.That(backend.Destroyed, Is.EqualTo(1));
    }

    [Test]
    public async Task Original_deadline_does_not_restart_after_a_failed_readback()
    {
        var now = DateTimeOffset.UtcNow;
        var dispatcher = new GatewayDispatcher(utcNow: () => now);
        var backend = new ReadbackBackend();
        var service = new GatewayScreenshotService(dispatcher, backend, 1024, () => now);
        var operation = service.CaptureOperation("deadline", TimeSpan.FromSeconds(5));
        dispatcher.Drain(DispatchPhase.EndOfFrame);
        await AwaitQueued(dispatcher);
        now += TimeSpan.FromSeconds(4);
        backend.Frame++;
        dispatcher.Drain(DispatchPhase.EndOfFrame);
        await AwaitQueued(dispatcher);
        now += TimeSpan.FromSeconds(2);
        dispatcher.Drain(DispatchPhase.EndOfFrame);
        var error = Assert.ThrowsAsync<GatewayScreenshotException>(async () =>
            await operation.Completion.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.That(error!.Code, Is.EqualTo("screenshot_render_timeout"));
        Assert.That(backend.Reads, Is.EqualTo(1));
        Assert.That(backend.Encoded, Is.Zero);
    }

    [Test]
    public async Task Cancellation_before_admission_releases_capacity_without_reading_or_waiting_for_a_frame()
    {
        var dispatcher = new GatewayDispatcher();
        var backend = new ReadbackBackend();
        var service = new GatewayScreenshotService(dispatcher, backend, 1024);
        var operation = service.CaptureOperation("cancel", TimeSpan.FromSeconds(5));
        Assert.That(operation.TryCancelBeforeStart(), Is.True);
        var error = Assert.ThrowsAsync<GatewayScreenshotException>(async () =>
            await operation.Completion.WaitAsync(TimeSpan.FromSeconds(2)));
        Assert.That(error!.Code, Is.EqualTo("screenshot_cancelled"));
        var next = service.CaptureOperation("next", TimeSpan.FromSeconds(5));
        Assert.That(next.Completion.IsCompleted, Is.False, "Cancellation releases the capture lease.");
        next.TryCancelBeforeStart();
        try { await next.Completion; } catch (GatewayScreenshotException) { }
        dispatcher.Drain(DispatchPhase.EndOfFrame, 8);
        Assert.That(backend.Reads, Is.Zero);
    }

    [Test]
    public async Task Unavailable_readback_waits_for_a_fresh_frame_under_one_capture_lease()
    {
        var dispatcher = new GatewayDispatcher();
        var backend = new ReadbackBackend();
        var service = new GatewayScreenshotService(dispatcher, backend, 1024);
        var operation = service.CaptureOperation("fresh", TimeSpan.FromSeconds(5));
        dispatcher.Drain(DispatchPhase.EndOfFrame);
        Assert.That(backend.Reads, Is.Zero, "Admission must not read an older/current frame.");
        Assert.That(operation.Completion.IsCompleted, Is.False, "Admission is not a rendered frame.");
        await AwaitQueued(dispatcher);
        backend.Frame = 11;
        dispatcher.Drain(DispatchPhase.EndOfFrame);
        await AwaitQueued(dispatcher);
        Assert.That(operation.Completion.IsCompleted, Is.False, "Unavailable readback must remain pending.");
        Assert.That(backend.Encoded, Is.Zero);
        var busy = service.CaptureOperation("busy", TimeSpan.FromSeconds(5));
        Assert.That(() => busy.Completion.GetAwaiter().GetResult(), Throws.TypeOf<GatewayScreenshotException>()
            .With.Property(nameof(GatewayScreenshotException.Code)).EqualTo("capture_busy"));
        dispatcher.Drain(DispatchPhase.EndOfFrame);
        await AwaitQueued(dispatcher);
        Assert.That(backend.Reads, Is.EqualTo(1), "Duplicate EOF drains in one frame must not read twice.");
        backend.Frame = 12;
        backend.Available = true;
        dispatcher.Drain(DispatchPhase.EndOfFrame);
        var result = await operation.Completion.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Multiple(() =>
        {
            Assert.That(result.Readback, Is.Not.Null);
            Assert.That(result.Readback!.AdmissionFrame, Is.EqualTo(10));
            Assert.That(result.Readback.RenderedFrame, Is.EqualTo(12));
            Assert.That(result.Readback.Attempts, Is.EqualTo(2));
            Assert.That(backend.Encoded, Is.EqualTo(1));
            Assert.That(backend.Destroyed, Is.EqualTo(1));
        });
    }

    private static async Task AwaitQueued(GatewayDispatcher dispatcher)
    {
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        while (dispatcher.PendingCount == 0)
        { deadline.Token.ThrowIfCancellationRequested(); await Task.Yield(); }
    }

    private sealed class ReadbackBackend : IGatewayScreenshotBackend, IGatewayDescribedScreenshotBackend,
        IGatewayRenderedScreenshotBackend
    {
        public int Frame = 10;
        public bool Available;
        public Action? OnCapture;
        public int ProofFrameOffset;
        public int Reads, Encoded, Destroyed;
        public bool RequiresReadback => true;
        public int CurrentFrame => Frame;
        public object Capture() => CaptureDescribed(new GatewayScreenshotRequest()).Resource;
        public GatewayScreenshotResource CaptureDescribed(GatewayScreenshotRequest request)
        {
            Reads++;
            OnCapture?.Invoke();
            if (!Available) throw new GatewayScreenshotException("screenshot_frame_unavailable", "No recorded frame.");
            return new GatewayScreenshotResource(new object(), 2, 2, new GatewayScreenshotCrop(0, 0, 2, 2),
                new GatewayRenderedFrameEvidence("native-test-boundary", Frame + ProofFrameOffset, DateTimeOffset.UtcNow));
        }
        public byte[] EncodePng(object resource)
        { Encoded++; return new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a }; }
        public void Destroy(object resource) => Destroyed++;
    }
}
