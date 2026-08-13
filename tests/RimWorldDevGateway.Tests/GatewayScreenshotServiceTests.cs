using NUnit.Framework;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class GatewayScreenshotServiceTests
{
    [Test]
    public void Capture_forwards_a_camera_centered_crop_without_thing_handles()
    {
        var dispatcher = new GatewayDispatcher(capacity: 4);
        var backend = new RecordingScreenshotBackend(ValidPng());
        var service = new GatewayScreenshotService(dispatcher, backend, maximumPngBytes: 1024);
        var request = new GatewayScreenshotRequest
        {
            WidthPixels = 960,
            HeightPixels = 540,
            OffsetXPixels = 120,
            OffsetYPixels = -40
        };

        var capture = service.CaptureAsync(
            "screenshot-centered",
            request,
            TimeSpan.FromSeconds(5));
        dispatcher.Drain(DispatchPhase.EndOfFrame);

        Assert.Multiple(() =>
        {
            Assert.That(capture.GetAwaiter().GetResult(), Is.EqualTo(ValidPng()));
            Assert.That(backend.Request, Is.Not.Null);
            Assert.That(backend.Request!.ThingHandles, Is.Empty);
            Assert.That(backend.Request.WidthPixels, Is.EqualTo(960));
            Assert.That(backend.Request.HeightPixels, Is.EqualTo(540));
            Assert.That(backend.Request.OffsetXPixels, Is.EqualTo(120));
            Assert.That(backend.Request.OffsetYPixels, Is.EqualTo(-40));
        });
    }

    [Test]
    public void Target_handles_and_padding_are_forwarded_to_one_end_of_frame_capture()
    {
        var dispatcher = new GatewayDispatcher(capacity: 4);
        var backend = new RecordingScreenshotBackend(ValidPng());
        var service = new GatewayScreenshotService(dispatcher, backend, maximumPngBytes: 1024);
        var request = new GatewayScreenshotRequest
        {
            ThingHandles = new List<string> { "Pawn_42", "Building_9" },
            PaddingPixels = 24
        };

        var capture = service.CaptureAsync(
            "screenshot-targets",
            request,
            TimeSpan.FromSeconds(5));

        Assert.That(dispatcher.Drain(DispatchPhase.EndOfFrame), Is.EqualTo(1));

        Assert.Multiple(() =>
        {
            Assert.That(capture.GetAwaiter().GetResult(), Is.EqualTo(ValidPng()));
            Assert.That(backend.Request, Is.Not.SameAs(request));
            Assert.That(backend.Request!.ThingHandles, Is.EqualTo(request.ThingHandles));
            Assert.That(backend.Request.PaddingPixels, Is.EqualTo(request.PaddingPixels));
            Assert.That(backend.CaptureCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Negative_target_padding_is_rejected_before_end_of_frame_admission()
    {
        var dispatcher = new GatewayDispatcher(capacity: 4);
        var backend = new RecordingScreenshotBackend(ValidPng());
        var service = new GatewayScreenshotService(dispatcher, backend, maximumPngBytes: 1024);
        var request = new GatewayScreenshotRequest
        {
            ThingHandles = new List<string> { "Pawn_42" },
            PaddingPixels = -1
        };

        var capture = service.CaptureAsync(
            "screenshot-negative-padding",
            request,
            TimeSpan.FromSeconds(5));

        Assert.That(capture.IsCompleted, Is.True);
        Assert.Multiple(() =>
        {
            Assert.That(
                () => capture.GetAwaiter().GetResult(),
                Throws.TypeOf<GatewayScreenshotException>()
                    .With.Property(nameof(GatewayScreenshotException.Code))
                    .EqualTo("invalid_screenshot_request"));
            Assert.That(dispatcher.PendingCount, Is.Zero);
            Assert.That(backend.CaptureCount, Is.Zero);
        });
    }

    [Test]
    public void Capture_is_queued_until_end_of_frame_and_releases_the_temporary_resource()
    {
        var dispatcher = new GatewayDispatcher(capacity: 4);
        var backend = new RecordingScreenshotBackend(ValidPng());
        var service = new GatewayScreenshotService(dispatcher, backend, maximumPngBytes: 1024);

        var capture = service.CaptureAsync("screenshot-1", TimeSpan.FromSeconds(5));

        Assert.Multiple(() =>
        {
            Assert.That(capture.IsCompleted, Is.False);
            Assert.That(backend.CaptureCount, Is.Zero);
            Assert.That(dispatcher.Drain(DispatchPhase.Update), Is.Zero);
            Assert.That(backend.CaptureCount, Is.Zero);
        });

        Assert.That(dispatcher.Drain(DispatchPhase.EndOfFrame), Is.EqualTo(1));

        Assert.Multiple(() =>
        {
            Assert.That(capture.GetAwaiter().GetResult(), Is.EqualTo(ValidPng()));
            Assert.That(backend.CaptureCount, Is.EqualTo(1));
            Assert.That(backend.EncodeCount, Is.EqualTo(1));
            Assert.That(backend.DestroyCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Capture_rejects_an_invalid_png_with_a_stable_error_and_destroys_the_resource()
    {
        var dispatcher = new GatewayDispatcher(capacity: 4);
        var backend = new RecordingScreenshotBackend(new byte[] { 0x89, 0x50, 0x4e });
        var service = new GatewayScreenshotService(dispatcher, backend, maximumPngBytes: 1024);

        var capture = service.CaptureAsync("screenshot-invalid", TimeSpan.FromSeconds(5));
        dispatcher.Drain(DispatchPhase.EndOfFrame);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => capture.GetAwaiter().GetResult(),
                Throws.TypeOf<GatewayScreenshotException>()
                    .With.Property(nameof(GatewayScreenshotException.Code)).EqualTo("invalid_png"));
            Assert.That(backend.DestroyCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Capture_rejects_a_png_above_the_byte_limit_and_destroys_the_resource()
    {
        var dispatcher = new GatewayDispatcher(capacity: 4);
        var oversizedPng = ValidPng().Concat(new byte[] { 0x00 }).ToArray();
        var backend = new RecordingScreenshotBackend(oversizedPng);
        var service = new GatewayScreenshotService(dispatcher, backend, maximumPngBytes: 8);

        var capture = service.CaptureAsync("screenshot-large", TimeSpan.FromSeconds(5));
        dispatcher.Drain(DispatchPhase.EndOfFrame);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => capture.GetAwaiter().GetResult(),
                Throws.TypeOf<GatewayScreenshotException>()
                    .With.Property(nameof(GatewayScreenshotException.Code)).EqualTo("capture_too_large"));
            Assert.That(backend.DestroyCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void Capture_allows_only_one_request_in_flight_and_releases_capacity_after_completion()
    {
        var dispatcher = new GatewayDispatcher(capacity: 4);
        var backend = new RecordingScreenshotBackend(ValidPng());
        var service = new GatewayScreenshotService(dispatcher, backend, maximumPngBytes: 1024);

        var first = service.CaptureAsync("screenshot-first", TimeSpan.FromSeconds(5));
        var rejected = service.CaptureAsync("screenshot-overlap", TimeSpan.FromSeconds(5));

        Assert.That(rejected.IsCompleted, Is.True);
        Assert.That(
            () => rejected.GetAwaiter().GetResult(),
            Throws.TypeOf<GatewayScreenshotException>()
                .With.Property(nameof(GatewayScreenshotException.Code)).EqualTo("capture_busy"));

        dispatcher.Drain(DispatchPhase.EndOfFrame);
        Assert.That(first.GetAwaiter().GetResult(), Is.EqualTo(ValidPng()));

        var next = service.CaptureAsync("screenshot-next", TimeSpan.FromSeconds(5));
        Assert.That(next.IsCompleted, Is.False);
        dispatcher.Drain(DispatchPhase.EndOfFrame);

        Assert.Multiple(() =>
        {
            Assert.That(next.GetAwaiter().GetResult(), Is.EqualTo(ValidPng()));
            Assert.That(backend.CaptureCount, Is.EqualTo(2));
            Assert.That(backend.DestroyCount, Is.EqualTo(2));
        });
    }

    [Test]
    public void Capture_wraps_backend_failure_in_a_stable_error_and_destroys_the_resource()
    {
        var dispatcher = new GatewayDispatcher(capacity: 4);
        var backend = new RecordingScreenshotBackend(ValidPng())
        {
            EncodeException = new InvalidOperationException("encoder failed")
        };
        var service = new GatewayScreenshotService(dispatcher, backend, maximumPngBytes: 1024);

        var capture = service.CaptureAsync("screenshot-failed", TimeSpan.FromSeconds(5));
        dispatcher.Drain(DispatchPhase.EndOfFrame);

        Assert.Multiple(() =>
        {
            Assert.That(
                () => capture.GetAwaiter().GetResult(),
                Throws.TypeOf<GatewayScreenshotException>()
                    .With.Property(nameof(GatewayScreenshotException.Code)).EqualTo("capture_failed")
                    .And.InnerException.TypeOf<InvalidOperationException>());
            Assert.That(backend.DestroyCount, Is.EqualTo(1));
        });
    }

    private static byte[] ValidPng() =>
        new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };

    private sealed class RecordingScreenshotBackend : IGatewayScreenshotBackend, IGatewayTargetedScreenshotBackend
    {
        private readonly byte[] png;

        public RecordingScreenshotBackend(byte[] png)
        {
            this.png = png;
        }

        public int CaptureCount { get; private set; }

        public int EncodeCount { get; private set; }

        public int DestroyCount { get; private set; }

        public Exception? EncodeException { get; set; }

        public GatewayScreenshotRequest? Request { get; private set; }

        public object Capture()
        {
            CaptureCount++;
            return new object();
        }

        public object Capture(GatewayScreenshotRequest request)
        {
            Request = request;
            return Capture();
        }

        public byte[] EncodePng(object resource)
        {
            EncodeCount++;
            if (EncodeException is not null)
            {
                throw EncodeException;
            }

            return png;
        }

        public void Destroy(object resource)
        {
            DestroyCount++;
        }
    }
}
