using System;
using UnityEngine;

namespace ThinWalls.Rendering;

public readonly struct HybridWallDoorLeaf : IEquatable<HybridWallDoorLeaf>
{
    public HybridWallDoorLeaf(float min, float max, float uvMin, float uvMax)
    {
        Min = min;
        Max = max;
        UvMin = uvMin;
        UvMax = uvMax;
    }

    public float Min { get; }

    public float Max { get; }

    public float UvMin { get; }

    public float UvMax { get; }

    public bool Equals(HybridWallDoorLeaf other) =>
        Min.Equals(other.Min) && Max.Equals(other.Max) &&
        UvMin.Equals(other.UvMin) && UvMax.Equals(other.UvMax);

    public override bool Equals(object? obj) => obj is HybridWallDoorLeaf other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = Min.GetHashCode();
            hash = (hash * 397) ^ Max.GetHashCode();
            hash = (hash * 397) ^ UvMin.GetHashCode();
            return (hash * 397) ^ UvMax.GetHashCode();
        }
    }
}

public readonly struct HybridWallDoorPlan
{
    public HybridWallDoorPlan(HybridWallDoorLeaf left, HybridWallDoorLeaf right)
    {
        Left = left;
        Right = right;
    }

    public HybridWallDoorLeaf Left { get; }

    public HybridWallDoorLeaf Right { get; }

    public float OpeningWidth => Right.Min - Left.Max;

    public bool IsFullyClosed => OpeningWidth <= 0.000001f;

    public float FrameLength => HybridWallDoorGeometryCompiler.FrameLength;

    public float FrameTopWidth => HybridWallProjectionProfile.ThinTopWidth;

    public float ThinFrameAboveClosedLeafOffset =>
        HybridWallDoorGeometryCompiler.ThinFrameAboveClosedLeafOffset;

    public bool HasRaisedCap => false;
}

public readonly struct HybridWallDoorShadowPlan
{
    public HybridWallDoorShadowPlan(float leftMin, float leftMax, float rightMin, float rightMax)
    {
        LeftMin = leftMin;
        LeftMax = leftMax;
        RightMin = rightMin;
        RightMax = rightMax;
    }

    public float LeftMin { get; }
    public float LeftMax { get; }
    public float RightMin { get; }
    public float RightMax { get; }
}

public static class HybridWallDoorGeometryCompiler
{
    public const float FrameLength = 3f / 60f;
    public const float LeafAboveWallOffset = 0.004f;
    public const float MovingLeafBelowWallOffset = -0.001f;
    public const float ThinFrameAboveClosedLeafOffset = 0.002f;
    public const float CustomPlaneTopVerticesAltitudeBias = 0f;
    public const float ClosedUnionThreshold = 0.05f;
    public const float MaximumSlide = 0.30f;

    public static float LeafAltitude(
        float nativeWallAltitude,
        float completedThinWallAltitude,
        float openFraction) =>
        Compile(openFraction).IsFullyClosed
            ? completedThinWallAltitude + LeafAboveWallOffset
            : nativeWallAltitude + MovingLeafBelowWallOffset;

    public static float ThinFrameAltitude(float completedThinWallAltitude) =>
        completedThinWallAltitude + LeafAboveWallOffset + ThinFrameAboveClosedLeafOffset;

    public static float RegularFrameAltitude(float nativeWallAltitude) => nativeWallAltitude;

    public static bool HasThinOwnedFrame(
        HybridWallRayMask doorRays,
        HybridWallRayMask retainedVertexRays) =>
        (doorRays & retainedVertexRays) != HybridWallRayMask.None;

    public static HybridWallDoorPlan Compile(float openFraction)
    {
        float open = Math.Max(0f, Math.Min(1f, openFraction));
        float slide = open <= ClosedUnionThreshold ? 0f : open * MaximumSlide;
        return new HybridWallDoorPlan(
            new HybridWallDoorLeaf(
                -0.5f + FrameLength - slide,
                -slide,
                FrameLength,
                0.5f),
            new HybridWallDoorLeaf(
                slide,
                0.5f - FrameLength + slide,
                0.5f,
                1f - FrameLength));
    }

    public static HybridWallDoorPlan CompileVisible(float openFraction)
    {
        HybridWallDoorPlan physical = Compile(openFraction);
        float leftMinimum = Math.Max(physical.Left.Min, -0.5f + FrameLength);
        float rightMaximum = Math.Min(physical.Right.Max, 0.5f - FrameLength);
        return new HybridWallDoorPlan(
            new HybridWallDoorLeaf(
                leftMinimum,
                physical.Left.Max,
                physical.Left.UvMin + leftMinimum - physical.Left.Min,
                physical.Left.UvMax),
            new HybridWallDoorLeaf(
                physical.Right.Min,
                rightMaximum,
                physical.Right.UvMin,
                physical.Right.UvMax - (physical.Right.Max - rightMaximum)));
    }

    public static HybridWallDoorShadowPlan CompileShadow(float openFraction)
    {
        HybridWallDoorPlan visible = CompileVisible(openFraction);
        return new HybridWallDoorShadowPlan(
            -0.5f,
            visible.Left.Max,
            visible.Right.Min,
            0.5f);
    }
}

public static class HybridWallDoorAppearance
{
    public const float PanelBrightness = 0.84f;

    public static Color32 PanelTone(Color32 source) => new(
        (byte)Math.Max(0, Math.Min(255, (int)Math.Round(source.r * PanelBrightness))),
        (byte)Math.Max(0, Math.Min(255, (int)Math.Round(source.g * PanelBrightness))),
        (byte)Math.Max(0, Math.Min(255, (int)Math.Round(source.b * PanelBrightness))),
        source.a);

    public static Color32 ApplyPanelTone(Color32 source, bool doorLeaf) =>
        doorLeaf ? PanelTone(source) : source;
}
