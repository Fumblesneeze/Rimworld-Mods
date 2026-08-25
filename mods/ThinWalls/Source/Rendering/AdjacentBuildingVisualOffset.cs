using System;
using RimWorld;
using ThinWalls.Geometry;
using Verse;
using UnityEngine;

namespace ThinWalls.Rendering;

public static class AdjacentBuildingVisualOffset
{
    public const float SourcePixel = 1f / 60f;

    public static DirectionalClearance MeasureClearance(
        DirectionalAlphaBounds alpha,
        Vector2 drawSize,
        IntVec2 footprintSize,
        float horizontalWallHalfDepth,
        float verticalWallHalfDepth)
    {
        alpha.Validate();
        if (drawSize.x <= 0f || drawSize.y <= 0f || footprintSize.x <= 0 || footprintSize.z <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(drawSize));
        }

        DirectionalAlphaExtents extents = ProjectAlphaBounds(
            alpha,
            drawSize,
            Rot4.North,
            shouldDrawRotated: false,
            flipped: false,
            Vector3.zero,
            rotatedExtraAngle: 0f,
            flipExtraRotation: 0f,
            useRealtimeDrawWorker: false);
        return MeasureClearance(
            extents,
            footprintSize,
            horizontalWallHalfDepth,
            verticalWallHalfDepth);
    }

    public static DirectionalClearance MeasureClearance(
        DirectionalAlphaExtents extents,
        IntVec2 footprintSize,
        float horizontalWallHalfDepth,
        float verticalWallHalfDepth)
    {
        if (footprintSize.x <= 0 || footprintSize.z <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(footprintSize));
        }
        return new DirectionalClearance(
            north: QuantizedClearance(
                extents.North + horizontalWallHalfDepth - (footprintSize.z / 2f)),
            east: QuantizedClearance(
                extents.East + verticalWallHalfDepth - (footprintSize.x / 2f)),
            south: QuantizedClearance(
                extents.South + horizontalWallHalfDepth - (footprintSize.z / 2f)),
            west: QuantizedClearance(
                extents.West + verticalWallHalfDepth - (footprintSize.x / 2f)));
    }

    public static DirectionalClearance MeasureOpaquePlaneClearance(
        Vector2 drawSize,
        IntVec2 footprintSize,
        float horizontalWallHalfDepth,
        float verticalWallHalfDepth,
        Vector3 drawOffset = default)
    {
        if (drawSize.x <= 0f || drawSize.y <= 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(drawSize));
        }
        return MeasureClearance(
            new DirectionalAlphaExtents(
                north: (drawSize.y / 2f) + drawOffset.z,
                east: (drawSize.x / 2f) + drawOffset.x,
                south: (drawSize.y / 2f) - drawOffset.z,
                west: (drawSize.x / 2f) - drawOffset.x),
            footprintSize,
            horizontalWallHalfDepth,
            verticalWallHalfDepth);
    }

    public static DirectionalAlphaExtents ProjectAlphaBounds(
        DirectionalAlphaBounds alpha,
        Vector2 drawSize,
        Rot4 rotation,
        bool shouldDrawRotated,
        bool flipped,
        Vector3 drawOffset,
        float rotatedExtraAngle,
        float flipExtraRotation = 0f,
        bool useRealtimeDrawWorker = false)
    {
        alpha.Validate();
        if (drawSize.x <= 0f || drawSize.y <= 0f || !rotation.IsValid)
        {
            throw new ArgumentOutOfRangeException(nameof(drawSize));
        }

        Vector2 planeSize = rotation.IsHorizontal && !shouldDrawRotated
            ? drawSize.Rotated()
            : drawSize;
        float minX = ((alpha.MinX / (float)alpha.TextureWidth) - 0.5f) * planeSize.x;
        float maxX = ((alpha.MaxXExclusive / (float)alpha.TextureWidth) - 0.5f) * planeSize.x;
        float minZ = (0.5f - (alpha.MaxYExclusive / (float)alpha.TextureHeight)) * planeSize.y;
        float maxZ = (0.5f - (alpha.MinY / (float)alpha.TextureHeight)) * planeSize.y;
        if (flipped && (!shouldDrawRotated || useRealtimeDrawWorker))
        {
            (minX, maxX) = (-maxX, -minX);
        }

        float angle = shouldDrawRotated
            ? rotation.AsAngle + rotatedExtraAngle + (flipped ? 180f : 0f)
            : (flipped && !useRealtimeDrawWorker ? flipExtraRotation : 0f);
        float radians = angle * Mathf.Deg2Rad;
        float cosine = Mathf.Cos(radians);
        float sine = Mathf.Sin(radians);
        float worldMinX = float.PositiveInfinity;
        float worldMaxX = float.NegativeInfinity;
        float worldMinZ = float.PositiveInfinity;
        float worldMaxZ = float.NegativeInfinity;
        foreach (Vector2 corner in new[]
                 {
                     new Vector2(minX, minZ),
                     new Vector2(minX, maxZ),
                     new Vector2(maxX, minZ),
                     new Vector2(maxX, maxZ),
                 })
        {
            float worldX = (cosine * corner.x) + (sine * corner.y) + drawOffset.x;
            float worldZ = (-sine * corner.x) + (cosine * corner.y) + drawOffset.z;
            worldMinX = Math.Min(worldMinX, worldX);
            worldMaxX = Math.Max(worldMaxX, worldX);
            worldMinZ = Math.Min(worldMinZ, worldZ);
            worldMaxZ = Math.Max(worldMaxZ, worldZ);
        }

        return new DirectionalAlphaExtents(
            north: worldMaxZ,
            east: worldMaxX,
            south: -worldMinZ,
            west: -worldMinX);
    }

    public static Vector3 Resolve(
        CellRect footprint,
        Func<SharedEdge, bool> hasThinEdge,
        DirectionalClearance clearance)
    {
        return ResolveCore(footprint, hasThinEdge, clearance);
    }

    private static Vector3 ResolveCore(
        CellRect footprint,
        Func<SharedEdge, bool> hasThinEdge,
        DirectionalClearance clearance)
    {
        if (hasThinEdge == null)
        {
            throw new ArgumentNullException(nameof(hasThinEdge));
        }

        bool north = false;
        bool east = false;
        bool south = false;
        bool west = false;

        for (int x = footprint.minX; x <= footprint.maxX; x++)
        {
            north |= hasThinEdge(new OwnedEdge(
                new IntVec3(x, 0, footprint.maxZ), ThinWallSide.North).Shared);
            south |= hasThinEdge(new OwnedEdge(
                new IntVec3(x, 0, footprint.minZ), ThinWallSide.South).Shared);
        }

        for (int z = footprint.minZ; z <= footprint.maxZ; z++)
        {
            east |= hasThinEdge(new OwnedEdge(
                new IntVec3(footprint.maxX, 0, z), ThinWallSide.East).Shared);
            west |= hasThinEdge(new OwnedEdge(
                new IntVec3(footprint.minX, 0, z), ThinWallSide.West).Shared);
        }

        return new Vector3(
            (west ? clearance.West : 0f) - (east ? clearance.East : 0f),
            0f,
            (south ? clearance.South : 0f) - (north ? clearance.North : 0f));
    }

    private static float QuantizedClearance(float overlapWithoutSafety)
    {
        float required = Math.Max(0f, overlapWithoutSafety + SourcePixel);
        return (float)Math.Ceiling((required * 60f) - 0.00001f) * SourcePixel;
    }

    internal static bool IsEligible(Thing thing, out ThingDef buildDef)
    {
        buildDef = null!;
        if (thing is not Building && thing is not Blueprint && thing is not Frame)
        {
            return false;
        }

        buildDef = (thing.def.entityDefToBuild as ThingDef) ?? thing.def;
        if (ThinWallUtility.IsThinEdgeDef(buildDef) || buildDef.category != ThingCategory.Building ||
            buildDef.building?.isWall == true)
        {
            return false;
        }

        Type? thingClass = buildDef.thingClass;
        return thingClass == null || !typeof(Building_Door).IsAssignableFrom(thingClass);
    }
}

public readonly struct DirectionalAlphaBounds
{
    public DirectionalAlphaBounds(
        int textureWidth,
        int textureHeight,
        int minX,
        int minY,
        int maxXExclusive,
        int maxYExclusive)
    {
        TextureWidth = textureWidth;
        TextureHeight = textureHeight;
        MinX = minX;
        MinY = minY;
        MaxXExclusive = maxXExclusive;
        MaxYExclusive = maxYExclusive;
    }

    public int TextureWidth { get; }
    public int TextureHeight { get; }
    public int MinX { get; }
    public int MinY { get; }
    public int MaxXExclusive { get; }
    public int MaxYExclusive { get; }

    internal void Validate()
    {
        if (TextureWidth <= 0 || TextureHeight <= 0 || MinX < 0 || MinY < 0 ||
            MaxXExclusive <= MinX || MaxYExclusive <= MinY ||
            MaxXExclusive > TextureWidth || MaxYExclusive > TextureHeight)
        {
            throw new ArgumentOutOfRangeException(nameof(DirectionalAlphaBounds));
        }
    }
}

public readonly struct DirectionalClearance
{
    public DirectionalClearance(float north, float east, float south, float west)
    {
        North = north;
        East = east;
        South = south;
        West = west;
    }

    public float North { get; }
    public float East { get; }
    public float South { get; }
    public float West { get; }

}

public readonly struct DirectionalAlphaExtents
{
    public DirectionalAlphaExtents(float north, float east, float south, float west)
    {
        North = north;
        East = east;
        South = south;
        West = west;
    }

    public float North { get; }
    public float East { get; }
    public float South { get; }
    public float West { get; }
}
