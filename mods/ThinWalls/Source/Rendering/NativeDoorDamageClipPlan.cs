using System;

namespace ThinWalls.Rendering;

public readonly struct NativeDoorDamageClip
{
    public NativeDoorDamageClip(float visibleMin, float visibleMax, float sourceMin, float sourceMax)
    {
        VisibleMin = visibleMin;
        VisibleMax = visibleMax;
        SourceMin = sourceMin;
        SourceMax = sourceMax;
    }

    public float VisibleMin { get; }
    public float VisibleMax { get; }
    public float SourceMin { get; }
    public float SourceMax { get; }
    public float Center => (VisibleMin + VisibleMax) * 0.5f;
    public float Width => VisibleMax - VisibleMin;
}

public static class NativeDoorDamageClipPlan
{
    public static bool TryClip(
        NativeThinDamageMark mark,
        float movedLongitudinalCenter,
        NativeDoorLeafPlan leaf,
        out NativeDoorDamageClip clip)
    {
        float desiredMin = movedLongitudinalCenter - mark.Size * 0.5f;
        float desiredMax = movedLongitudinalCenter + mark.Size * 0.5f;
        float visibleMin = Math.Max(desiredMin, leaf.LongitudinalMin);
        float visibleMax = Math.Min(desiredMax, leaf.LongitudinalMax);
        if (visibleMax - visibleMin < 0.02f)
        {
            clip = default;
            return false;
        }
        clip = new NativeDoorDamageClip(
            visibleMin,
            visibleMax,
            (visibleMin - desiredMin) / mark.Size,
            (visibleMax - desiredMin) / mark.Size);
        return true;
    }
}
