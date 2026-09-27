using System;
using System.Collections.Generic;

namespace ThinWalls.Rendering;

public readonly struct NativeThinDamageMark : IEquatable<NativeThinDamageMark>
{
    public NativeThinDamageMark(
        float longitudinalCenter,
        float normalCenter,
        float size,
        float rotationDegrees,
        int coreScratchIndex)
    {
        LongitudinalCenter = longitudinalCenter;
        NormalCenter = normalCenter;
        Size = size;
        RotationDegrees = rotationDegrees;
        CoreScratchIndex = coreScratchIndex;
    }

    public float LongitudinalCenter { get; }
    public float NormalCenter { get; }
    public float Size { get; }
    public float RotationDegrees { get; }
    public int CoreScratchIndex { get; }

    public bool Equals(NativeThinDamageMark other) =>
        LongitudinalCenter.Equals(other.LongitudinalCenter) &&
        NormalCenter.Equals(other.NormalCenter) &&
        Size.Equals(other.Size) &&
        RotationDegrees.Equals(other.RotationDegrees) &&
        CoreScratchIndex == other.CoreScratchIndex;

    public override bool Equals(object? obj) => obj is NativeThinDamageMark other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = LongitudinalCenter.GetHashCode();
            hash = (hash * 397) ^ NormalCenter.GetHashCode();
            hash = (hash * 397) ^ Size.GetHashCode();
            hash = (hash * 397) ^ RotationDegrees.GetHashCode();
            return (hash * 397) ^ CoreScratchIndex;
        }
    }
}

/// <summary>
/// Deterministic placement only. The renderer binds each mark to one of Core's
/// installed Damage/Scratch materials; this plan never creates or edits pixels.
/// </summary>
public static class NativeThinDamagePlan
{
    // Keep Core's transparent damage depth. The structural center remains the
    // canonical edge, while this native lift prevents the scratch material
    // sorting behind the source-bound wall mesh.
    public const float CompletedSurfaceBias = 15f / 82f;

    public static float StaticOverlayAltitude(float buildingAltitude) =>
        buildingAltitude + CompletedSurfaceBias;

    public static IReadOnlyList<NativeThinDamageMark> Compile(ThinWallDamageGrade grade, int seed)
    {
        int count = grade switch
        {
            ThinWallDamageGrade.Moderate => 1,
            ThinWallDamageGrade.Heavy => 2,
            ThinWallDamageGrade.Severe => 3,
            _ => 0,
        };
        if (count == 0) return Array.Empty<NativeThinDamageMark>();

        var result = new List<NativeThinDamageMark>(count);
        uint state = unchecked((uint)seed * 747796405u + 2891336453u);
        float[] anchors = { -0.18f, 0f, 0.18f };
        // Native scratch sprites are sparse and are normally drawn across a full
        // cell. Preserve a near-native scale, with only the tiny rotation range
        // that still keeps the complete source plane on the edge silhouette.
        float[] sizes = { 0.54f, 0.52f, 0.54f };
        for (int index = 0; index < count; index++)
        {
            state = Next(state);
            float longitudinalJitter = Unit(state) * 0.05f - 0.025f;
            state = Next(state);
            float normal = Unit(state) * 0.004f - 0.002f;
            state = Next(state);
            float rotation = Unit(state) * 4f - 2f;
            int scratch = (int)((state >> 16) + (uint)index) % 3;
            result.Add(new NativeThinDamageMark(
                anchors[index] + longitudinalJitter,
                normal,
                sizes[index],
                rotation,
                scratch));
        }
        return result;
    }

    private static uint Next(uint value) => unchecked(value * 1664525u + 1013904223u);

    private static float Unit(uint value) => (value & 0x00ffffffu) / 16777215f;
}
