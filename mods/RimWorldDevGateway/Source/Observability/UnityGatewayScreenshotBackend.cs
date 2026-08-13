using System.Collections.Generic;
using RimWorldDevGateway.Contracts;
using UnityEngine;
using Verse;

namespace RimWorldDevGateway;

public interface IGatewayScreenshotTextureOperations
{
    object Capture();

    int Width(object resource);

    int Height(object resource);

    object CropTexture(object resource, GatewayScreenshotCrop crop);

    byte[] EncodePng(object resource);

    void Destroy(object resource);
}

public interface IGatewayScreenshotTargetProjector
{
    IReadOnlyList<GatewayProjectedScreenshotTarget> Project(
        IReadOnlyList<string> handles,
        int frameWidth,
        int frameHeight);
}

public sealed class UnityGatewayScreenshotBackend :
    IGatewayScreenshotBackend,
    IGatewayTargetedScreenshotBackend,
    IGatewayDescribedScreenshotBackend
{
    private readonly IGatewayScreenshotTextureOperations textures;
    private readonly IGatewayScreenshotTargetProjector targets;

    public UnityGatewayScreenshotBackend()
        : this(
            new UnityGatewayScreenshotTextureOperations(),
            new VerseGatewayScreenshotTargetProjector())
    {
    }

    public UnityGatewayScreenshotBackend(
        IGatewayScreenshotTextureOperations textures,
        IGatewayScreenshotTargetProjector targets)
    {
        this.textures = textures ?? throw new ArgumentNullException(nameof(textures));
        this.targets = targets ?? throw new ArgumentNullException(nameof(targets));
    }

    public object Capture() => textures.Capture();

    public object Capture(GatewayScreenshotRequest request) =>
        CaptureDescribed(request).Resource;

    public GatewayScreenshotResource CaptureDescribed(GatewayScreenshotRequest request)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var fullFrame = textures.Capture();
        try
        {
            var width = textures.Width(fullFrame);
            var height = textures.Height(fullFrame);
            var crop = request.ThingHandles.Count == 0 && !request.WidthPixels.HasValue
                ? new GatewayScreenshotCrop(0, 0, width, height)
                : request.WidthPixels.HasValue
                ? GatewayScreenshotCropPlanner.PlanCentered(
                    width,
                    height,
                    request.WidthPixels.Value,
                    request.HeightPixels!.Value,
                    request.OffsetXPixels ?? 0,
                    request.OffsetYPixels ?? 0)
                : GatewayScreenshotCropPlanner.Plan(
                    width,
                    height,
                    targets.Project(request.ThingHandles, width, height),
                    request.PaddingPixels ?? 32);
            var cropped = crop.X == 0 && crop.Y == 0 && crop.Width == width && crop.Height == height
                ? fullFrame
                : textures.CropTexture(fullFrame, crop);
            if (ReferenceEquals(cropped, fullFrame))
            {
                fullFrame = null!;
            }

            return new GatewayScreenshotResource(cropped, width, height, crop);
        }
        finally
        {
            if (fullFrame is not null)
            {
                textures.Destroy(fullFrame);
            }
        }
    }

    public byte[] EncodePng(object resource) => textures.EncodePng(resource);

    public void Destroy(object resource) => textures.Destroy(resource);
}

public sealed class UnityGatewayScreenshotTextureOperations : IGatewayScreenshotTextureOperations
{
    public object Capture()
    {
        var texture = ScreenCapture.CaptureScreenshotAsTexture();
        if (texture is null)
        {
            throw new InvalidOperationException("Unity returned no screenshot texture.");
        }

        return texture;
    }

    public int Width(object resource) => RequireTexture(resource).width;

    public int Height(object resource) => RequireTexture(resource).height;

    public object CropTexture(object resource, GatewayScreenshotCrop crop)
    {
        var source = RequireTexture(resource);
        Texture2D? cropped = null;
        try
        {
            cropped = new Texture2D(crop.Width, crop.Height, TextureFormat.RGB24, mipChain: false);
            cropped.SetPixels(source.GetPixels(crop.X, crop.Y, crop.Width, crop.Height));
            cropped.Apply(updateMipmaps: false, makeNoLongerReadable: false);
            return cropped;
        }
        catch
        {
            if (cropped is not null)
            {
                UnityEngine.Object.Destroy(cropped);
            }

            throw;
        }
    }

    public byte[] EncodePng(object resource) =>
        ImageConversion.EncodeToPNG(RequireTexture(resource));

    public void Destroy(object resource)
    {
        if (resource is UnityEngine.Object unityObject)
        {
            UnityEngine.Object.Destroy(unityObject);
        }
    }

    private static Texture2D RequireTexture(object resource) =>
        resource as Texture2D ?? throw new ArgumentException(
            "The screenshot resource is not a Unity Texture2D.",
            nameof(resource));
}

public sealed class VerseGatewayScreenshotTargetProjector : IGatewayScreenshotTargetProjector
{
    public IReadOnlyList<GatewayProjectedScreenshotTarget> Project(
        IReadOnlyList<string> handles,
        int frameWidth,
        int frameHeight)
    {
        if (handles is null)
        {
            throw new ArgumentNullException(nameof(handles));
        }

        var map = Current.Game?.CurrentMap ?? throw new GatewayScreenshotException(
            "screenshot_target_not_found",
            "Thing-bounded screenshots require a current playable map.");
        var camera = Find.Camera;
        if (camera is null || Screen.width <= 0 || Screen.height <= 0)
        {
            throw new GatewayScreenshotException(
                "screenshot_camera_unavailable",
                "Thing-bounded screenshots require RimWorld's active map camera.");
        }

        var thingsByHandle = CaptureAddressableThings(map);
        var projected = new List<GatewayProjectedScreenshotTarget>(handles.Count);
        foreach (var handle in handles)
        {
            if (!thingsByHandle.TryGetValue(handle, out var thing))
            {
                throw new GatewayScreenshotException(
                    "screenshot_target_not_found",
                    $"Thing '{handle}' is not spawned on the current map.");
            }

            projected.Add(ProjectThing(
                camera,
                thing,
                handle,
                frameWidth / (double)Screen.width,
                frameHeight / (double)Screen.height));
        }

        return projected;
    }

    private static Dictionary<string, Thing> CaptureAddressableThings(Map map)
    {
        var result = new Dictionary<string, Thing>(StringComparer.Ordinal);
        foreach (var thing in map.listerThings.AllThings.ToArray())
        {
            try
            {
                if (!thing.Spawned ||
                    thing.Destroyed ||
                    thing.Map != map ||
                    !thing.def.HasThingIDNumber ||
                    string.IsNullOrWhiteSpace(thing.ThingID) ||
                    thing.ThingID.Length > GatewayThingController.MaximumHandleLength)
                {
                    continue;
                }

                AddAlias(result, thing.ThingID, thing);
                AddAlias(result, thing.GetUniqueLoadID(), thing);
            }
            catch (Exception)
            {
                // A hostile modded Thing must not abort the bounded map snapshot.
                // Any alias already added for this candidate remains usable.
            }
        }

        return result;
    }

    private static void AddAlias(Dictionary<string, Thing> result, string? handle, Thing thing)
    {
        if (!string.IsNullOrWhiteSpace(handle) &&
            handle!.Length <= GatewayThingController.MaximumHandleLength &&
            !result.ContainsKey(handle))
        {
            result.Add(handle, thing);
        }
    }

    private static GatewayProjectedScreenshotTarget ProjectThing(
        Camera camera,
        Thing thing,
        string handle,
        double scaleX,
        double scaleY)
    {
        var occupied = GenAdj.OccupiedRect(thing.Position, thing.Rotation, thing.def.size);
        var corners = new[]
        {
            new Vector3(occupied.minX, 0f, occupied.minZ),
            new Vector3(occupied.minX, 0f, occupied.maxZ + 1),
            new Vector3(occupied.maxX + 1, 0f, occupied.minZ),
            new Vector3(occupied.maxX + 1, 0f, occupied.maxZ + 1)
        };
        var minimumX = double.PositiveInfinity;
        var minimumY = double.PositiveInfinity;
        var maximumX = double.NegativeInfinity;
        var maximumY = double.NegativeInfinity;
        foreach (var corner in corners)
        {
            var screen = camera.WorldToScreenPoint(corner);
            if (screen.z <= 0f)
            {
                throw new GatewayScreenshotException(
                    "screenshot_target_not_visible",
                    $"Thing '{handle}' is behind the active map camera.");
            }

            var x = screen.x * scaleX;
            var y = screen.y * scaleY;
            minimumX = Math.Min(minimumX, x);
            minimumY = Math.Min(minimumY, y);
            maximumX = Math.Max(maximumX, x);
            maximumY = Math.Max(maximumY, y);
        }

        return new GatewayProjectedScreenshotTarget(
            handle,
            minimumX,
            minimumY,
            maximumX,
            maximumY);
    }
}
