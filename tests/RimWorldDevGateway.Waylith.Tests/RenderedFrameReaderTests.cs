using NUnit.Framework;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class RenderedFrameReaderTests
{
    [Test]
    public void Native_readback_proof_survives_crop_and_full_frame_release()
    {
        var textures = new ProofTextures();
        var backend = new UnityGatewayScreenshotBackend(textures, new NoTargets());
        Assert.That((object)backend as IGatewayRenderedScreenshotBackend, Is.Not.Null);
        Assert.That(((IGatewayRenderedScreenshotBackend)(object)backend).RequiresReadback, Is.True);
        var capture = backend.CaptureDescribed(new GatewayScreenshotRequest { WidthPixels = 1, HeightPixels = 1 });
        Assert.That(capture.Readback, Is.SameAs(textures.Proof));
        Assert.That(textures.Destroyed, Is.EqualTo(new[] { textures.Full }));
        backend.Destroy(capture.Resource);
        Assert.That(textures.Destroyed, Is.EqualTo(new[] { textures.Full, textures.Crop }));
    }

    private sealed class NoTargets : IGatewayScreenshotTargetProjector
    {
        public IReadOnlyList<GatewayProjectedScreenshotTarget> Project(IReadOnlyList<string> handles,
            int frameWidth, int frameHeight) => throw new AssertionException("No crop targets expected.");
    }

    private sealed class ProofTextures : IGatewayScreenshotTextureOperations, IGatewayRenderedScreenshotBackend
    {
        public readonly GatewayRenderedFrameEvidence Proof = new("native-test", 20, DateTimeOffset.UtcNow);
        public readonly object Full;
        public readonly object Crop = new();
        public readonly List<object> Destroyed = new();
        public ProofTextures() => Full = new GatewayScreenshotTexture(new object(), Proof);
        public bool RequiresReadback => true;
        public int CurrentFrame => 20;
        public object Capture() => Full;
        public int Width(object resource) => 2;
        public int Height(object resource) => 2;
        public object CropTexture(object resource, GatewayScreenshotCrop crop) => Crop;
        public byte[] EncodePng(object resource) => throw new AssertionException("Encoding not needed here.");
        public void Destroy(object resource) => Destroyed.Add(resource);
    }

    [TestCase("length")]
    [TestCase("extent")]
    [TestCase("frame")]
    public void Inconsistent_native_readback_is_rejected_before_texture_creation(string fault)
    {
        var source = new Source { Available = true };
        if (fault == "length") source.Pixels = new byte[15];
        if (fault == "extent") source.Width = 0;
        if (fault == "frame") source.OnRead = () => source.Frame++;
        Assert.That(() => new GatewayRenderedFrameReader(source).Read(),
            Throws.TypeOf<GatewayScreenshotException>()
                .With.Property(nameof(GatewayScreenshotException.Code)).EqualTo("screenshot_readback_invalid"));
    }

    [Test]
    public void Genuine_black_pixels_are_accepted_only_with_successful_native_readback()
    {
        var source = new Source { Available = true };
        var reader = new GatewayRenderedFrameReader(source);
        var result = reader.Read();
        Assert.That(result, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result!.RgbaTopDown, Is.EqualTo(new byte[16]));
            Assert.That(result.Evidence.Provider, Is.EqualTo("native-boundary"));
            Assert.That(result.Evidence.Frame, Is.EqualTo(20));
            Assert.That(result.Width, Is.EqualTo(2));
            Assert.That(result.Height, Is.EqualTo(2));
        });
        source.Available = false;
        Assert.That(reader.Read(), Is.Null, "Failed readback is pending, regardless of returned pixel bytes.");
    }

    private sealed class Source : IGatewayScreenshotReadbackSource
    {
        public bool Available;
        public int Frame = 20;
        public byte[] Pixels = new byte[16];
        public int Width = 2, Height = 2;
        public Action? OnRead;
        public string Provider => "native-boundary";
        public int CurrentFrame => Frame;
        public bool TryRead(out byte[] pixels, out int width, out int height)
        { OnRead?.Invoke(); pixels = Pixels; width = Width; height = Height; return Available; }
    }
}
