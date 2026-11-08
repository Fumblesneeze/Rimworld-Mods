namespace RimWorldDevGateway;

public interface IGatewayScreenshotReadbackSource
{
    string Provider { get; }
    int CurrentFrame { get; }
    bool TryRead(out byte[] rgbaTopDown, out int width, out int height);
}

public sealed class GatewayReadbackPixels
{
    public GatewayReadbackPixels(byte[] rgbaTopDown, int width, int height, GatewayRenderedFrameEvidence evidence)
    { RgbaTopDown = rgbaTopDown; Width = width; Height = height; Evidence = evidence; }
    public byte[] RgbaTopDown { get; }
    public int Width { get; }
    public int Height { get; }
    public GatewayRenderedFrameEvidence Evidence { get; }
}

public sealed class GatewayRenderedFrameReader
{
    private readonly IGatewayScreenshotReadbackSource source;
    public GatewayRenderedFrameReader(IGatewayScreenshotReadbackSource source) =>
        this.source = source ?? throw new ArgumentNullException(nameof(source));
    public int CurrentFrame => source.CurrentFrame;
    public GatewayReadbackPixels? Read()
    {
        var frame = source.CurrentFrame;
        if (!source.TryRead(out var pixels, out var width, out var height)) return null;
        if (source.CurrentFrame != frame || width <= 0 || height <= 0 || pixels == null ||
            (long)width * height * 4 != pixels.Length || string.IsNullOrWhiteSpace(source.Provider))
            throw new GatewayScreenshotException("screenshot_readback_invalid",
                "Native screenshot readback returned inconsistent dimensions, pixels or frame identity.");
        return new GatewayReadbackPixels(pixels, width, height,
            new GatewayRenderedFrameEvidence(source.Provider, frame, DateTimeOffset.UtcNow));
    }
}

public interface IGatewayRenderedScreenshotBackend
{
    bool RequiresReadback { get; }
    int CurrentFrame { get; }
}

public sealed class GatewayScreenshotTexture
{
    public GatewayScreenshotTexture(object texture, GatewayRenderedFrameEvidence evidence)
    { Texture = texture; Evidence = evidence; }
    public object Texture { get; }
    public GatewayRenderedFrameEvidence Evidence { get; }
}

public sealed class GatewayRenderedFrameEvidence
{
    public GatewayRenderedFrameEvidence(string provider, int frame, DateTimeOffset readbackUtc)
    { Provider = provider; Frame = frame; ReadbackUtc = readbackUtc; }
    public string Provider { get; }
    public int Frame { get; }
    public DateTimeOffset ReadbackUtc { get; }
}

public sealed class GatewayScreenshotReadbackReceipt
{
    public GatewayScreenshotReadbackReceipt(string provider, int admissionFrame, int renderedFrame,
        DateTimeOffset readbackUtc, int attempts)
    { Provider = provider; AdmissionFrame = admissionFrame; RenderedFrame = renderedFrame;
      ReadbackUtc = readbackUtc; Attempts = attempts; }
    public string Provider { get; }
    public int AdmissionFrame { get; }
    public int RenderedFrame { get; }
    public DateTimeOffset ReadbackUtc { get; }
    public int Attempts { get; }
}
