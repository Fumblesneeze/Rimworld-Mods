using System.Threading;
using System.Threading.Tasks;

namespace RimWorldDevGateway;

public interface IGatewayScreenshotBackend
{
    object Capture();

    byte[] EncodePng(object resource);

    void Destroy(object resource);
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
        return CaptureOperation(requestId, timeout, cancellationToken).Completion;
    }

    public GatewayDispatchOperation<byte[]> CaptureOperation(
        string requestId,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
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
                Capture,
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

    private byte[] Capture(CancellationToken cancellationToken)
    {
        try
        {
            var resource = backend.Capture();
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
