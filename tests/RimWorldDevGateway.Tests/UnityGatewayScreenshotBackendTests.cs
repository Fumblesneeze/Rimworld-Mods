using NUnit.Framework;
using RimWorldDevGateway.Contracts;

namespace RimWorldDevGateway.Tests;

[TestFixture]
public sealed class UnityGatewayScreenshotBackendTests
{
    [Test]
    public void Targeted_capture_crops_the_projected_union_and_releases_the_full_frame()
    {
        var textures = new RecordingTextureOperations(width: 400, height: 300);
        var projector = new StaticTargetProjector(
            new GatewayProjectedScreenshotTarget("Pawn_42", 50.2, 40.8, 90.1, 80.2),
            new GatewayProjectedScreenshotTarget("Building_9", 200.4, 150.6, 250.2, 190.5));
        var backend = new UnityGatewayScreenshotBackend(textures, projector);
        var request = new GatewayScreenshotRequest
        {
            ThingHandles = new List<string> { "Pawn_42", "Building_9" },
            PaddingPixels = 24
        };

        var cropped = backend.Capture(request);
        var png = backend.EncodePng(cropped);
        backend.Destroy(cropped);

        Assert.Multiple(() =>
        {
            Assert.That(cropped, Is.SameAs(textures.CropResource));
            Assert.That(png, Has.Length.EqualTo(8));
            Assert.That(textures.CaptureCount, Is.EqualTo(1));
            Assert.That(textures.Crop, Is.Not.Null);
            Assert.That(textures.Crop!.X, Is.EqualTo(26));
            Assert.That(textures.Crop.Y, Is.EqualTo(16));
            Assert.That(textures.Crop.Width, Is.EqualTo(249));
            Assert.That(textures.Crop.Height, Is.EqualTo(199));
            Assert.That(
                textures.Destroyed,
                Is.EqualTo(new[] { textures.FullFrameResource, textures.CropResource }));
        });
    }

    [Test]
    public void Targeted_capture_releases_the_full_frame_when_cropping_throws()
    {
        var textures = new RecordingTextureOperations(width: 400, height: 300)
        {
            CropException = new InvalidOperationException("copy failed")
        };
        var backend = new UnityGatewayScreenshotBackend(
            textures,
            new StaticTargetProjector(
                new GatewayProjectedScreenshotTarget("Pawn_42", 50, 40, 90, 80)));

        Assert.That(
            () => backend.Capture(new GatewayScreenshotRequest
            {
                ThingHandles = new List<string> { "Pawn_42" },
                PaddingPixels = 32
            }),
            Throws.TypeOf<InvalidOperationException>()
                .With.Message.EqualTo("copy failed"));
        Assert.That(textures.Destroyed, Is.EqualTo(new[] { textures.FullFrameResource }));
    }

    [Test]
    public void Camera_centered_capture_crops_without_projecting_or_selecting_things()
    {
        var textures = new RecordingTextureOperations(width: 1920, height: 1080);
        var projector = new RejectingTargetProjector();
        var backend = new UnityGatewayScreenshotBackend(textures, projector);

        var cropped = backend.Capture(new GatewayScreenshotRequest
        {
            WidthPixels = 960,
            HeightPixels = 540,
            OffsetXPixels = 120,
            OffsetYPixels = -40
        });

        Assert.Multiple(() =>
        {
            Assert.That(cropped, Is.SameAs(textures.CropResource));
            Assert.That(textures.Crop, Is.Not.Null);
            Assert.That(textures.Crop!.X, Is.EqualTo(600));
            Assert.That(textures.Crop.Y, Is.EqualTo(230));
            Assert.That(textures.Crop.Width, Is.EqualTo(960));
            Assert.That(textures.Crop.Height, Is.EqualTo(540));
            Assert.That(projector.ProjectCount, Is.Zero);
            Assert.That(textures.Destroyed, Is.EqualTo(new[] { textures.FullFrameResource }));
        });
    }

    [Test]
    public void Described_capture_reports_the_exact_clamped_rectangle_and_source_frame()
    {
        var textures = new RecordingTextureOperations(width: 1600, height: 900);
        var backend = new UnityGatewayScreenshotBackend(
            textures,
            new RejectingTargetProjector());

        var described = backend.CaptureDescribed(new GatewayScreenshotRequest
        {
            WidthPixels = 1280,
            HeightPixels = 720,
            OffsetXPixels = 500,
            OffsetYPixels = -500
        });

        Assert.Multiple(() =>
        {
            Assert.That(described.FrameWidth, Is.EqualTo(1600));
            Assert.That(described.FrameHeight, Is.EqualTo(900));
            Assert.That(described.Crop.X, Is.EqualTo(320));
            Assert.That(described.Crop.Y, Is.Zero);
            Assert.That(described.Crop.Width, Is.EqualTo(1280));
            Assert.That(described.Crop.Height, Is.EqualTo(720));
        });
    }

    private sealed class StaticTargetProjector : IGatewayScreenshotTargetProjector
    {
        private readonly IReadOnlyList<GatewayProjectedScreenshotTarget> targets;

        public StaticTargetProjector(params GatewayProjectedScreenshotTarget[] targets)
        {
            this.targets = targets;
        }

        public IReadOnlyList<GatewayProjectedScreenshotTarget> Project(
            IReadOnlyList<string> handles,
            int frameWidth,
            int frameHeight) => targets;
    }

    private sealed class RejectingTargetProjector : IGatewayScreenshotTargetProjector
    {
        public int ProjectCount { get; private set; }

        public IReadOnlyList<GatewayProjectedScreenshotTarget> Project(
            IReadOnlyList<string> handles,
            int frameWidth,
            int frameHeight)
        {
            ProjectCount++;
            throw new AssertionException("Camera-centered capture must not project or select Things.");
        }
    }

    private sealed class RecordingTextureOperations : IGatewayScreenshotTextureOperations
    {
        private readonly int width;
        private readonly int height;

        public RecordingTextureOperations(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public object FullFrameResource { get; } = new();

        public object CropResource { get; } = new();

        public int CaptureCount { get; private set; }

        public GatewayScreenshotCrop? Crop { get; private set; }

        public Exception? CropException { get; set; }

        public List<object> Destroyed { get; } = new();

        public object Capture()
        {
            CaptureCount++;
            return FullFrameResource;
        }

        public int Width(object resource) => width;

        public int Height(object resource) => height;

        public object CropTexture(object resource, GatewayScreenshotCrop crop)
        {
            Crop = crop;
            if (CropException is not null)
            {
                throw CropException;
            }

            return CropResource;
        }

        public byte[] EncodePng(object resource) =>
            new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a };

        public void Destroy(object resource) => Destroyed.Add(resource);
    }
}
