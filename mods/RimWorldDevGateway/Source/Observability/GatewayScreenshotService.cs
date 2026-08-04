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
        return CaptureOperation(
            requestId,
            new GatewayScreenshotRequest(),
            timeout,
            cancellationToken).Completion;
    }

    public Task<byte[]> CaptureAsync(
        string requestId,
        GatewayScreenshotRequest request,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        return CaptureOperation(requestId, request, timeout, cancellationToken).Completion;
    }

    public GatewayDispatchOperation<byte[]> CaptureOperation(
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

    public GatewayDispatchOperation<byte[]> CaptureOperation(
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
            return new GatewayDispatchOperation<byte[]>(Task.FromException<byte[]>(exception));
        }

        var captureLease = new object();
        if (Interlocked.CompareExchange(ref captureInFlight, captureLease, null) is not null)
        {
            return new GatewayDispatchOperation<byte[]>(Task.FromException<byte[]>(
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

    private void ReleaseCapacityWhenComplete(Task<byte[]> capture, object captureLease)
    {
        capture.ContinueWith(
            _ => ReleaseCapacity(captureLease),
            CancellationToken.None,
            TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private void ReleaseCapacity(object captureLease)
    {
        Interlocked.CompareExchange(ref captureInFlight, null, captureLease);
    }

    private byte[] Capture(GatewayScreenshotRequest request, CancellationToken cancellationToken)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();
            var resource = request.ThingHandles.Count == 0
                ? backend.Capture()
                : backend is IGatewayTargetedScreenshotBackend targeted
                    ? targeted.Capture(request)
                    : throw new GatewayScreenshotException(
                        "targeted_capture_unavailable",
                        "The screenshot backend does not support Thing-bounded captures.");
            try
            {
                var png = backend.EncodePng(resource);
                ValidatePng(png);
                return png;
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
        if (suppliedHandles.Count == 0)
        {
            if (request.PaddingPixels.HasValue)
            {
                throw InvalidRequest("paddingPixels requires at least one thing handle.");
            }

            return new GatewayScreenshotRequest();
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
