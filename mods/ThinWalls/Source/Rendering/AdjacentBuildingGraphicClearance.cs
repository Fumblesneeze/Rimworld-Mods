using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using RimWorld;
using UnityEngine;
using Verse;

namespace ThinWalls.Rendering;

internal static class AdjacentBuildingGraphicClearance
{
    private const float FrameOverdraw = 1.15f;
    private static readonly Dictionary<int, DirectionalAlphaBounds> AlphaBoundsByTexture = new();
    private static readonly Dictionary<MeasurementKey, ResolvedClearance> Measurements = new();

    public static bool TryResolve(Thing thing, ThingDef buildDef, CellRect footprint, out ResolvedClearance resolved)
    {
        try
        {
            if (thing is Frame)
            {
                Vector3 frameDrawOffset = buildDef.building?.isAttachment == true
                    ? (thing.Rotation.AsVector2 * 0.5f).ToVector3()
                    : Vector3.zero;
                MeasurementKey key = MeasurementKey.ForFrame(thing, footprint, frameDrawOffset);
                if (!Measurements.TryGetValue(key, out resolved))
                {
                    resolved = new ResolvedClearance(
                        key.GetHashCode(),
                        AdjacentBuildingVisualOffset.MeasureOpaquePlaneClearance(
                            new Vector2(footprint.Width * FrameOverdraw, footprint.Height * FrameOverdraw),
                            new IntVec2(footprint.Width, footprint.Height),
                            horizontalWallHalfDepth: 17.5f / HybridWallRasterPlan.Size,
                            verticalWallHalfDepth: 17f / HybridWallRasterPlan.Size,
                            drawOffset: frameDrawOffset));
                    Measurements[key] = resolved;
                }
                return true;
            }

            Graphic graphic = thing.Graphic.ExtractInnerGraphicFor(thing);
            if (graphic == null || graphic == BaseContent.BadGraphic)
            {
                resolved = default;
                return false;
            }
            Material? material = graphic.MatAt(thing.Rotation, thing);
            Texture? texture = material?.mainTexture;
            if (material == null || texture == null || material.mainTextureScale != Vector2.one ||
                material.mainTextureOffset != Vector2.zero)
            {
                // A transformed/custom UV layout cannot be inferred from the source texture's
                // global alpha bounds. Leave such custom rendering at its native position.
                resolved = default;
                return false;
            }

            bool flipped = thing.Rotation == Rot4.West && graphic.WestFlipped ||
                           thing.Rotation == Rot4.East && graphic.EastFlipped;
            Vector3 drawOffset = graphic.DrawOffset(thing.Rotation);
            float flipExtraRotation = graphic.data?.flipExtraRotation ?? 0f;
            bool useRealtimeDrawWorker = thing.def.drawerType == DrawerType.RealtimeOnly;
            var measurementKey = new MeasurementKey(
                RuntimeHelpers.GetHashCode(graphic),
                texture.GetInstanceID(),
                thing.Rotation,
                graphic.ShouldDrawRotated,
                flipped,
                graphic.DrawRotatedExtraAngleOffset,
                flipExtraRotation,
                useRealtimeDrawWorker,
                graphic.drawSize,
                drawOffset,
                footprint.Width,
                footprint.Height,
                phase: thing is Blueprint ? 1 : 0);
            if (Measurements.TryGetValue(measurementKey, out resolved))
            {
                return true;
            }

            DirectionalAlphaBounds alpha = ReadAlphaBounds(texture);
            DirectionalAlphaExtents extents = AdjacentBuildingVisualOffset.ProjectAlphaBounds(
                alpha,
                graphic.drawSize,
                thing.Rotation,
                graphic.ShouldDrawRotated,
                flipped,
                drawOffset,
                graphic.DrawRotatedExtraAngleOffset,
                flipExtraRotation,
                useRealtimeDrawWorker);
            DirectionalClearance clearance = AdjacentBuildingVisualOffset.MeasureClearance(
                extents,
                new IntVec2(footprint.Width, footprint.Height),
                horizontalWallHalfDepth: 17.5f / HybridWallRasterPlan.Size,
                verticalWallHalfDepth: 17f / HybridWallRasterPlan.Size);
            resolved = new ResolvedClearance(measurementKey.GetHashCode(), clearance);
            Measurements[measurementKey] = resolved;
            return true;
        }
        catch (Exception exception)
        {
            Log.WarningOnce(
                $"Thin Walls could not measure the actual render silhouette for {buildDef.defName}/{thing.GetType().Name}; " +
                $"the building keeps its native draw position. {exception.GetType().Name}: {exception.Message}",
                Gen.HashCombineInt(buildDef.shortHash, thing.GetType().GetHashCode()));
            resolved = default;
            return false;
        }
    }

    private static DirectionalAlphaBounds ReadAlphaBounds(Texture texture)
    {
        int id = texture.GetInstanceID();
        if (AlphaBoundsByTexture.TryGetValue(id, out DirectionalAlphaBounds cached))
        {
            return cached;
        }

        RenderTexture previous = RenderTexture.active;
        RenderTexture temporary = RenderTexture.GetTemporary(
            texture.width,
            texture.height,
            0,
            RenderTextureFormat.ARGB32,
            RenderTextureReadWrite.Default);
        Texture2D? readable = null;
        try
        {
            Graphics.Blit(texture, temporary);
            RenderTexture.active = temporary;
            readable = new Texture2D(texture.width, texture.height, TextureFormat.RGBA32, false);
            readable.ReadPixels(new Rect(0f, 0f, texture.width, texture.height), 0, 0);
            readable.Apply(updateMipmaps: false, makeNoLongerReadable: false);

            Color32[] pixels = readable.GetPixels32();
            int minX = texture.width;
            int minUnityY = texture.height;
            int maxXExclusive = 0;
            int maxUnityYExclusive = 0;
            for (int y = 0; y < texture.height; y++)
            {
                int row = y * texture.width;
                for (int x = 0; x < texture.width; x++)
                {
                    if (pixels[row + x].a == 0)
                    {
                        continue;
                    }

                    minX = Math.Min(minX, x);
                    minUnityY = Math.Min(minUnityY, y);
                    maxXExclusive = Math.Max(maxXExclusive, x + 1);
                    maxUnityYExclusive = Math.Max(maxUnityYExclusive, y + 1);
                }
            }

            if (maxXExclusive <= minX || maxUnityYExclusive <= minUnityY)
            {
                throw new InvalidOperationException($"Building texture {texture.name} has no nonzero-alpha pixels.");
            }

            cached = new DirectionalAlphaBounds(
                texture.width,
                texture.height,
                minX,
                texture.height - maxUnityYExclusive,
                maxXExclusive,
                texture.height - minUnityY);
            AlphaBoundsByTexture[id] = cached;
            return cached;
        }
        finally
        {
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(temporary);
            if (readable != null)
            {
                UnityEngine.Object.Destroy(readable);
            }
        }
    }

    internal readonly struct ResolvedClearance
    {
        public ResolvedClearance(int identity, DirectionalClearance clearance)
        {
            Identity = identity;
            Clearance = clearance;
        }

        public int Identity { get; }
        public DirectionalClearance Clearance { get; }
    }

    private readonly struct MeasurementKey : IEquatable<MeasurementKey>
    {
        public MeasurementKey(
            int graphicIdentity,
            int textureIdentity,
            Rot4 rotation,
            bool shouldDrawRotated,
            bool flipped,
            float extraAngle,
            float flipExtraRotation,
            bool useRealtimeDrawWorker,
            Vector2 drawSize,
            Vector3 drawOffset,
            int footprintWidth,
            int footprintHeight,
            int phase)
        {
            GraphicIdentity = graphicIdentity;
            TextureIdentity = textureIdentity;
            Rotation = rotation;
            ShouldDrawRotated = shouldDrawRotated;
            Flipped = flipped;
            ExtraAngle = extraAngle;
            FlipExtraRotation = flipExtraRotation;
            UseRealtimeDrawWorker = useRealtimeDrawWorker;
            DrawSize = drawSize;
            DrawOffset = drawOffset;
            FootprintWidth = footprintWidth;
            FootprintHeight = footprintHeight;
            Phase = phase;
        }

        public static MeasurementKey ForFrame(Thing thing, CellRect footprint, Vector3 drawOffset) => new(
            graphicIdentity: thing.def.shortHash,
            textureIdentity: 0,
            thing.Rotation,
            shouldDrawRotated: true,
            flipped: false,
            extraAngle: 0f,
            flipExtraRotation: 0f,
            useRealtimeDrawWorker: true,
            new Vector2(footprint.Width * FrameOverdraw, footprint.Height * FrameOverdraw),
            drawOffset,
            footprint.Width,
            footprint.Height,
            phase: 2);

        private int GraphicIdentity { get; }
        private int TextureIdentity { get; }
        private Rot4 Rotation { get; }
        private bool ShouldDrawRotated { get; }
        private bool Flipped { get; }
        private float ExtraAngle { get; }
        private float FlipExtraRotation { get; }
        private bool UseRealtimeDrawWorker { get; }
        private Vector2 DrawSize { get; }
        private Vector3 DrawOffset { get; }
        private int FootprintWidth { get; }
        private int FootprintHeight { get; }
        private int Phase { get; }

        public bool Equals(MeasurementKey other) =>
            GraphicIdentity == other.GraphicIdentity && TextureIdentity == other.TextureIdentity &&
            Rotation == other.Rotation && ShouldDrawRotated == other.ShouldDrawRotated &&
            Flipped == other.Flipped && ExtraAngle.Equals(other.ExtraAngle) &&
            FlipExtraRotation.Equals(other.FlipExtraRotation) &&
            UseRealtimeDrawWorker == other.UseRealtimeDrawWorker &&
            DrawSize.Equals(other.DrawSize) && DrawOffset.Equals(other.DrawOffset) &&
            FootprintWidth == other.FootprintWidth && FootprintHeight == other.FootprintHeight &&
            Phase == other.Phase;

        public override bool Equals(object? obj) => obj is MeasurementKey other && Equals(other);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = GraphicIdentity;
                hash = (hash * 397) ^ TextureIdentity;
                hash = (hash * 397) ^ Rotation.GetHashCode();
                hash = (hash * 397) ^ ShouldDrawRotated.GetHashCode();
                hash = (hash * 397) ^ Flipped.GetHashCode();
                hash = (hash * 397) ^ ExtraAngle.GetHashCode();
                hash = (hash * 397) ^ FlipExtraRotation.GetHashCode();
                hash = (hash * 397) ^ UseRealtimeDrawWorker.GetHashCode();
                hash = (hash * 397) ^ DrawSize.GetHashCode();
                hash = (hash * 397) ^ DrawOffset.GetHashCode();
                hash = (hash * 397) ^ FootprintWidth;
                hash = (hash * 397) ^ FootprintHeight;
                return (hash * 397) ^ Phase;
            }
        }
    }
}
