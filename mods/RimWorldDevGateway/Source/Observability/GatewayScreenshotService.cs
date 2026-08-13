using System.Threading;
using System.Threading.Tasks;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway;

public interface IGatewayScreenshotBackend
{
    object Capture();

    byte[] EncodePng(object resource);

    void Destroy(object resource);
}

public interface IGatewayTargetedScreenshotBackend
{
    object Capture(GatewayScreenshotRequest request);
}

public interface IGatewayDescribedScreenshotBackend
{
    GatewayScreenshotResource CaptureDescribed(GatewayScreenshotRequest request);
}

public sealed class GatewayScreenshotResource
{
    public GatewayScreenshotResource(
        object resource,
        int frameWidth,
        int frameHeight,
        GatewayScreenshotCrop crop)
    {
        Resource = resource ?? throw new ArgumentNullException(nameof(resource));
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        Crop = crop ?? throw new ArgumentNullException(nameof(crop));
    }

    public object Resource { get; }

    public int FrameWidth { get; }

    public int FrameHeight { get; }

    public GatewayScreenshotCrop Crop { get; }
}

public sealed class GatewayScreenshotCapture
{
    public GatewayScreenshotCapture(
        byte[] png,
        int? frameWidth = null,
        int? frameHeight = null,
        GatewayScreenshotCrop? crop = null)
    {
        Png = png ?? throw new ArgumentNullException(nameof(png));
        FrameWidth = frameWidth;
        FrameHeight = frameHeight;
        Crop = crop;
    }

    public byte[] Png { get; }

    public int? FrameWidth { get; }

    public int? FrameHeight { get; }

    public GatewayScreenshotCrop? Crop { get; }
}

public sealed class GatewayScreenshotException : Exception
{
    public GatewayScreenshotException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    public GatewayScreenshotException(string code, string message, Exception innerException)
        : base(message, innerException)
    {
        Code = code;
    }

    public string Code { get; }
}

public sealed class GatewayScreenshotService
{
    public const int MaximumCropDimensionPixels = 16384;

    private readonly GatewayDispatcher dispatcher;
    private readonly IGatewayScreenshotBackend backend;
    private readonly int maximumPngBytes;
    private object? captureInFlight;

    public GatewayScreenshotService(
        GatewayDispatcher dispatcher,
        IGatewayScreenshotBackend backend,
        int maximumPngBytes)
    {
        if (maximumPngBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumPngBytes));
        }

        this.dispatcher = dispatcher ?? throw new ArgumentNullException(nameof(dispatcher));
        this.backend = backend ?? throw new ArgumentNullException(nameof(backend));
        this.maximumPngBytes = maximumPngBytes;
    }

    public Task<byte[]> CaptureAsync(
        string requestId,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        var operation = CaptureOperation(
            requestId,
            new GatewayScreenshotRequest(),
            timeout,
            cancellationToken);
        return ReadPng(operation.Completion);
    }

    public Task<byte[]> CaptureAsync(
        string requestId,
        GatewayScreenshotRequest request,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        return ReadPng(CaptureOperation(requestId, request, timeout, cancellationToken).Completion);
    }

    public GatewayDispatchOperation<GatewayScreenshotCapture> CaptureOperation(
        string requestId,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        return CaptureOperation(
            requestId,
            new GatewayScreenshotRequest(),
            timeout,
            cancellationToken);
    }

    public GatewayDispatchOperation<GatewayScreenshotCapture> CaptureOperation(
        string requestId,
        GatewayScreenshotRequest request,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        GatewayScreenshotRequest normalizedRequest;
        try
        {
            normalizedRequest = Normalize(request);
        }
        catch (GatewayScreenshotException exception)
        {
            return new GatewayDispatchOperation<GatewayScreenshotCapture>(
                Task.FromException<GatewayScreenshotCapture>(exception));
        }

        var captureLease = new object();
        if (Interlocked.CompareExchange(ref captureInFlight, captureLease, null) is not null)
        {
            return new GatewayDispatchOperation<GatewayScreenshotCapture>(Task.FromException<GatewayScreenshotCapture>(
                new GatewayScreenshotException(
                    "capture_busy",
                    "A screenshot capture is already queued or running.")));
        }

        try
        {
            var capture = dispatcher.EnqueueOperation(
                requestId,
                "screenshot.capture",
                timeout,
                cancellation => Capture(normalizedRequest, cancellation),
                DispatchPhase.EndOfFrame,
                cancellationToken);
            capture.OnCancelledBeforeStart(() => ReleaseCapacity(captureLease));
            ReleaseCapacityWhenComplete(capture.Completion, captureLease);
            return capture;
        }
        catch
        {
            ReleaseCapacity(captureLease);
            throw;
        }
    }

    private void ReleaseCapacityWhenComplete(
        Task<GatewayScreenshotCapture> capture,
        object captureLease)
    {
        capture.ContinueWith(
            _ => ReleaseCapacity(captureLease),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private static async Task<byte[]> ReadPng(Task<GatewayScreenshotCapture> capture) =>
        (await capture.ConfigureAwait(false)).Png;

    private void ReleaseCapacity(object captureLease)
    {
        Interlocked.CompareExchange(ref captureInFlight, null, captureLease);
    }

    private GatewayScreenshotCapture Capture(
        GatewayScreenshotRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            GatewayScreenshotResource? described = null;
            var resource = backend is IGatewayDescribedScreenshotBackend descriptive
                ? (described = descriptive.CaptureDescribed(request)).Resource
                : request.ThingHandles.Count == 0 && !request.WidthPixels.HasValue
                    ? backend.Capture()
                    : backend is IGatewayTargetedScreenshotBackend targeted
                        ? targeted.Capture(request)
                        : throw new GatewayScreenshotException(
                            "targeted_capture_unavailable",
                            "The screenshot backend does not support cropped captures.");
            try
            {
                var png = backend.EncodePng(resource);
                ValidatePng(png);
                return new GatewayScreenshotCapture(
                    png,
                    described?.FrameWidth,
                    described?.FrameHeight,
                    described?.Crop);
            }
            finally
            {
                backend.Destroy(resource);
            }
        }
        catch (GatewayScreenshotException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new GatewayScreenshotException(
                "capture_failed",
                "The screenshot backend failed while capturing or encoding the frame.",
                exception);
        }
    }

    private static GatewayScreenshotRequest Normalize(GatewayScreenshotRequest request)
    {
        var suppliedHandles = request.ThingHandles ?? throw InvalidRequest(
            "thingHandles must be an array when supplied.");
        var hasCenteredSize = request.WidthPixels.HasValue || request.HeightPixels.HasValue;
        var hasCenteredOffset = request.OffsetXPixels.HasValue || request.OffsetYPixels.HasValue;
        if (suppliedHandles.Count == 0)
        {
            if (request.PaddingPixels.HasValue)
            {
                throw InvalidRequest("paddingPixels requires at least one thing handle.");
            }

            if (!hasCenteredSize)
            {
                if (hasCenteredOffset)
                {
                    throw InvalidRequest("offsetXPixels and offsetYPixels require widthPixels and heightPixels.");
                }

                return new GatewayScreenshotRequest();
            }

            if (!request.WidthPixels.HasValue || !request.HeightPixels.HasValue)
            {
                throw InvalidRequest("widthPixels and heightPixels must be supplied together.");
            }

            if (request.WidthPixels <= 0 ||
                request.HeightPixels <= 0 ||
                request.WidthPixels > MaximumCropDimensionPixels ||
                request.HeightPixels > MaximumCropDimensionPixels)
            {
                throw InvalidRequest(
                    $"widthPixels and heightPixels must be between 1 and {MaximumCropDimensionPixels}.");
            }

            return new GatewayScreenshotRequest
            {
                WidthPixels = request.WidthPixels,
                HeightPixels = request.HeightPixels,
                OffsetXPixels = request.OffsetXPixels ?? 0,
                OffsetYPixels = request.OffsetYPixels ?? 0
            };
        }

        if (hasCenteredSize || hasCenteredOffset)
        {
            throw InvalidRequest(
                "Thing-bounded and camera-centered screenshot crops are mutually exclusive.");
        }

        if (request.PaddingPixels < 0)
        {
            throw InvalidRequest("paddingPixels must be non-negative.");
        }

        var distinct = new List<string>(suppliedHandles.Count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var handle in suppliedHandles)
        {
            if (string.IsNullOrWhiteSpace(handle) ||
                handle.Length > GatewayThingController.MaximumHandleLength)
            {
                throw InvalidRequest(
                    $"Every thing handle must contain 1 through {GatewayThingController.MaximumHandleLength} characters.");
            }

            if (seen.Add(handle))
            {
                distinct.Add(handle);
            }
        }

        return new GatewayScreenshotRequest
        {
            ThingHandles = distinct,
            PaddingPixels = request.PaddingPixels ?? 32
        };
    }

    private static GatewayScreenshotException InvalidRequest(string message) =>
        new("invalid_screenshot_request", message);

    private void ValidatePng(byte[]? png)
    {
        if (png is null ||
            png.Length < 8 ||
            png[0] != 0x89 ||
            png[1] != 0x50 ||
            png[2] != 0x4e ||
            png[3] != 0x47 ||
            png[4] != 0x0d ||
            png[5] != 0x0a ||
            png[6] != 0x1a ||
            png[7] != 0x0a)
        {
            throw new GatewayScreenshotException(
                "invalid_png",
                "The screenshot backend did not produce a valid PNG signature.");
        }

        if (png.Length > maximumPngBytes)
        {
            throw new GatewayScreenshotException(
                "capture_too_large",
                $"The encoded screenshot exceeds the {maximumPngBytes}-byte limit.");
        }
    }
}
