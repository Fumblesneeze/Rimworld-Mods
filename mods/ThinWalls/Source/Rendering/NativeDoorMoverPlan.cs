using System;

namespace ThinWalls.Rendering;

public readonly struct NativeDoorLeafPlan
{
    public NativeDoorLeafPlan(
        float longitudinalMin,
        float longitudinalMax,
        float uAtMin,
        float uAtMax,
        float vMin,
        float vMax)
    {
        LongitudinalMin = longitudinalMin;
        LongitudinalMax = longitudinalMax;
        UAtMin = uAtMin;
        UAtMax = uAtMax;
        VMin = vMin;
        VMax = vMax;
    }

    public float LongitudinalMin { get; }
    public float LongitudinalMax { get; }
    public float UAtMin { get; }
    public float UAtMax { get; }
    public float VMin { get; }
    public float VMax { get; }
}

public readonly struct NativeDoorMoverPlan
{
    public const float FrameLength = 3f / 60f;
    public const float ClosedLeafLength = 0.5f - FrameLength;
    public const float MaximumSlide = 0.30f;
    public const float SourceAlphaUMin = 0f;
    public const float SourceAlphaUMax = 34f / 64f;
    public const float SourceAlphaVMin = 10f / 64f;
    public const float SourceAlphaVMax = 54f / 64f;
    public const float NormalHalfWidth = 17f / 60f;

    private NativeDoorMoverPlan(NativeDoorLeafPlan left, NativeDoorLeafPlan right)
    {
        Left = left;
        Right = right;
    }

    public NativeDoorLeafPlan Left { get; }
    public NativeDoorLeafPlan Right { get; }
    public float NormalMin => -NormalHalfWidth;
    public float NormalMax => NormalHalfWidth;
    public float OpeningWidth => Right.LongitudinalMin - Left.LongitudinalMax;

    public static NativeDoorMoverPlan Compile(float openFraction)
    {
        float open = Math.Max(0f, Math.Min(1f, openFraction));
        float slide = open <= HybridWallDoorGeometryCompiler.ClosedUnionThreshold
            ? 0f
            : open * MaximumSlide;
        float leftPhysicalMin = -ClosedLeafLength - slide;
        float leftPhysicalMax = -slide;
        float rightPhysicalMin = slide;
        float rightPhysicalMax = ClosedLeafLength + slide;
        float leftVisibleMin = Math.Max(leftPhysicalMin, -ClosedLeafLength);
        float rightVisibleMax = Math.Min(rightPhysicalMax, ClosedLeafLength);
        float sourceWidth = SourceAlphaUMax - SourceAlphaUMin;
        float leftCrop = (leftVisibleMin - leftPhysicalMin) / ClosedLeafLength;
        float rightVisibleFraction = (rightVisibleMax - rightPhysicalMin) / ClosedLeafLength;
        return new NativeDoorMoverPlan(
            new NativeDoorLeafPlan(
                leftVisibleMin,
                leftPhysicalMax,
                SourceAlphaUMin + leftCrop * sourceWidth,
                SourceAlphaUMax,
                SourceAlphaVMin,
                SourceAlphaVMax),
            new NativeDoorLeafPlan(
                rightPhysicalMin,
                rightVisibleMax,
                SourceAlphaUMax,
                SourceAlphaUMax - rightVisibleFraction * sourceWidth,
                SourceAlphaVMin,
                SourceAlphaVMax));
    }
}
