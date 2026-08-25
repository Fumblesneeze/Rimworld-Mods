using System;
using System.Collections.Generic;
using ThinWalls.Geometry;
using Verse;

namespace ThinWalls.Rendering;

/// <summary>
/// The single immutable plan consumed by completed Thin structure rendering.
/// It binds the exhaustive topology union to the actual Core-derived raster,
/// door-frame aperture, owner partitions, and shadow coverage.
/// </summary>
public sealed class HybridWallRuntimePlan
{
    internal HybridWallRuntimePlan(
        HybridWallTopologyPlan topology,
        HybridWallRasterPlan raster)
    {
        Topology = topology;
        Raster = raster;
    }

    public HybridWallTopologyPlan Topology { get; }

    public HybridWallRasterPlan Raster { get; }

    public HybridWallRayMask Rays => Raster.Rays;

    public HybridWallRayMask DoorRays => Raster.DoorRays;

    public HybridWallRayMask PrimaryOwnerAt(int x, int y)
    {
        HybridWallRayMask owners = Raster[x, y].OwnerRays;
        if ((owners & (HybridWallRayMask.East | HybridWallRayMask.West)) != 0)
        {
            HybridWallRayMask preferred = x < HybridWallRasterPlan.Size / 2
                ? HybridWallRayMask.West
                : HybridWallRayMask.East;
            if (owners.HasFlag(preferred))
            {
                return preferred;
            }
            return owners.HasFlag(HybridWallRayMask.West)
                ? HybridWallRayMask.West
                : HybridWallRayMask.East;
        }

        if ((owners & (HybridWallRayMask.North | HybridWallRayMask.South)) != 0)
        {
            HybridWallRayMask preferred = y < HybridWallRasterPlan.Size / 2
                ? HybridWallRayMask.South
                : HybridWallRayMask.North;
            if (owners.HasFlag(preferred))
            {
                return preferred;
            }
            return owners.HasFlag(HybridWallRayMask.South)
                ? HybridWallRayMask.South
                : HybridWallRayMask.North;
        }

        return HybridWallRayMask.None;
    }
}

public static class HybridWallRuntimePlanCompiler
{
    public static HybridWallRuntimePlan Compile(
        HybridWallVertexTopology topology,
        HybridWallRayMask doubledRays,
        HybridWallRayMask doorRays)
    {
        HybridWallTopologyPlan union = HybridWallTopologyCompiler.Compile(topology, doubledRays);
        HybridWallRasterPlan raster = HybridWallRasterCompiler.Compile(
            topology.ThinRays,
            doubledRays & topology.ThinRays,
            topology.OrdinaryQuadrants,
            includeOutline: true,
            doorRays: doorRays & topology.ThinRays);
        return new HybridWallRuntimePlan(union, raster);
    }
}

public static class HybridWallTextureSampling
{
    public static bool UseMipmaps(
        HybridWallRuntimePlan? plan,
        HybridWallRayMask emittedPartition = HybridWallRayMask.None)
    {
        if (plan == null)
        {
            return true;
        }
        if (emittedPartition != HybridWallRayMask.None)
        {
            return (plan.DoorRays & emittedPartition) == HybridWallRayMask.None;
        }

        return (plan.Rays & ~plan.DoorRays) != HybridWallRayMask.None;
    }
}

public readonly struct HybridWallPartitionVisual
{
    public HybridWallPartitionVisual(
        HybridWallRayMask ray,
        int materialId,
        ThinWallMaterialFamily family,
        ThinWallDamageGrade damage,
        bool doubled,
        bool door)
    {
        Ray = ray;
        MaterialId = materialId;
        Family = family;
        Damage = damage;
        Doubled = doubled;
        Door = door;
    }

    public HybridWallRayMask Ray { get; }
    public int MaterialId { get; }
    public ThinWallMaterialFamily Family { get; }
    public ThinWallDamageGrade Damage { get; }
    public bool Doubled { get; }
    public bool Door { get; }
}

public static class HybridWallPartitionBatching
{
    public static bool CanRenderAsOneStructuralUnion(
        IReadOnlyList<HybridWallPartitionVisual> partitions)
    {
        if (partitions.Count < 2)
        {
            return false;
        }

        HybridWallPartitionVisual first = partitions[0];
        if (first.Doubled || first.Door || first.Damage != ThinWallDamageGrade.None)
        {
            return false;
        }
        for (int index = 1; index < partitions.Count; index++)
        {
            HybridWallPartitionVisual current = partitions[index];
            if (current.Doubled || current.Door ||
                current.MaterialId != first.MaterialId ||
                current.Family != first.Family ||
                current.Damage != first.Damage)
            {
                return false;
            }
        }
        return true;
    }
}

public readonly struct HybridWallSunShadowVolume
{
    public HybridWallSunShadowVolume(float centerX, float centerZ, float sizeX, float sizeZ, float height)
    {
        CenterX = centerX;
        CenterZ = centerZ;
        SizeX = sizeX;
        SizeZ = sizeZ;
        Height = height;
    }

    public float CenterX { get; }

    public float CenterZ { get; }

    public float SizeX { get; }

    public float SizeZ { get; }

    public float Height { get; }
}

public static class HybridWallSunShadowGeometry
{
    public const float Height = 1f;

    public static HybridWallSunShadowVolume ForEdge(SharedEdge edge, int ownerCount) =>
        ForEdgeSpan(edge, ownerCount, 0f, 1f);

    public static HybridWallSunShadowVolume ForEdgeSpan(
        SharedEdge edge,
        int ownerCount,
        float longitudinalMin,
        float longitudinalMax)
    {
        if (float.IsNaN(longitudinalMin) || float.IsInfinity(longitudinalMin) ||
            float.IsNaN(longitudinalMax) || float.IsInfinity(longitudinalMax) ||
            longitudinalMin >= longitudinalMax)
        {
            throw new ArgumentOutOfRangeException(nameof(longitudinalMin));
        }

        float width = (ownerCount > 1
            ? HybridWallRasterCompiler.DoubledTopWidth
            : HybridWallRasterCompiler.StandardTopWidth) / (float)HybridWallRasterPlan.Size;
        float length = longitudinalMax - longitudinalMin;
        float midpoint = (longitudinalMin + longitudinalMax) * 0.5f;
        if (edge.PositiveSide == ThinWallSide.North)
        {
            return new HybridWallSunShadowVolume(
                edge.AnchorCell.x + midpoint,
                edge.AnchorCell.z + 1f,
                length,
                width,
                Height);
        }

        return new HybridWallSunShadowVolume(
            edge.AnchorCell.x + 1f,
            edge.AnchorCell.z + midpoint,
            width,
            length,
            Height);
    }
}
