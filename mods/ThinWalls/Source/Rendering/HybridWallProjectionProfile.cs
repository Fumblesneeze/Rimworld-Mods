using System;
using System.Collections.Generic;

namespace ThinWalls.Rendering;

public readonly struct HybridWallUvRect : IEquatable<HybridWallUvRect>
{
    public HybridWallUvRect(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    public float X { get; }

    public float Y { get; }

    public float Width { get; }

    public float Height { get; }

    public float XMax => X + Width;

    public float YMax => Y + Height;

    public bool Equals(HybridWallUvRect other) =>
        X.Equals(other.X) && Y.Equals(other.Y) &&
        Width.Equals(other.Width) && Height.Equals(other.Height);

    public override bool Equals(object? obj) => obj is HybridWallUvRect other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = X.GetHashCode();
            hash = (hash * 397) ^ Y.GetHashCode();
            hash = (hash * 397) ^ Width.GetHashCode();
            return (hash * 397) ^ Height.GetHashCode();
        }
    }

    public override string ToString() => FormattableString.Invariant($"({X},{Y},{Width},{Height})");
}

public readonly struct HybridWallPhaseSpan : IEquatable<HybridWallPhaseSpan>
{
    public HybridWallPhaseSpan(float min, float max, float phaseMin, float phaseMax)
    {
        Min = min;
        Max = max;
        PhaseMin = phaseMin;
        PhaseMax = phaseMax;
    }

    public float Min { get; }

    public float Max { get; }

    public float PhaseMin { get; }

    public float PhaseMax { get; }

    public bool Equals(HybridWallPhaseSpan other) =>
        Min.Equals(other.Min) && Max.Equals(other.Max) &&
        PhaseMin.Equals(other.PhaseMin) && PhaseMax.Equals(other.PhaseMax);

    public override bool Equals(object? obj) => obj is HybridWallPhaseSpan other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = Min.GetHashCode();
            hash = (hash * 397) ^ Max.GetHashCode();
            hash = (hash * 397) ^ PhaseMin.GetHashCode();
            return (hash * 397) ^ PhaseMax.GetHashCode();
        }
    }
}

public readonly struct HybridWallPhaseRect
{
    public HybridWallPhaseRect(HybridWallPhaseSpan x, HybridWallPhaseSpan z)
    {
        MinX = x.Min;
        MaxX = x.Max;
        MinZ = z.Min;
        MaxZ = z.Max;
        UvMinX = x.PhaseMin;
        UvMaxX = x.PhaseMax;
        UvMinZ = z.PhaseMin;
        UvMaxZ = z.PhaseMax;
    }

    public float MinX { get; }

    public float MaxX { get; }

    public float MinZ { get; }

    public float MaxZ { get; }

    public float UvMinX { get; }

    public float UvMaxX { get; }

    public float UvMinZ { get; }

    public float UvMaxZ { get; }
}

public static class HybridWallTexturePhase
{
    public static IReadOnlyList<HybridWallPhaseSpan> Split(float min, float max)
    {
        if (!(min < max))
        {
            throw new ArgumentOutOfRangeException(nameof(max), "Texture spans must have positive length.");
        }

        var result = new List<HybridWallPhaseSpan>();
        float current = min;
        while (current < max)
        {
            float phaseBase = (float)Math.Floor(current);
            float next = Math.Min(max, phaseBase + 1f);
            if (!(current < next))
            {
                phaseBase += 1f;
                next = Math.Min(max, phaseBase + 1f);
            }

            result.Add(new HybridWallPhaseSpan(
                current,
                next,
                current - phaseBase,
                next - phaseBase));
            current = next;
        }

        return result;
    }

    public static IReadOnlyList<HybridWallPhaseRect> Split(
        float minX,
        float minZ,
        float maxX,
        float maxZ)
    {
        IReadOnlyList<HybridWallPhaseSpan> xs = Split(minX, maxX);
        IReadOnlyList<HybridWallPhaseSpan> zs = Split(minZ, maxZ);
        var result = new List<HybridWallPhaseRect>(xs.Count * zs.Count);
        foreach (HybridWallPhaseSpan x in xs)
        {
            foreach (HybridWallPhaseSpan z in zs)
            {
                result.Add(new HybridWallPhaseRect(x, z));
            }
        }

        return result;
    }
}

public static class HybridWallProjectionProfile
{
    public const int AtlasPixels = 320;
    public const int SlotPixels = 80;
    public const int GutterPixels = 10;
    public const int InnerPixels = 60;
    public const int TopSourcePixels = 33;
    public const int ThinTopPixels = 7;
    public const int FrontFacePixels = 22;
    public const int WestSidePixels = 11;
    public const int EastSidePixels = 10;
    public const int OutlinePixels = 2;

    public const float ThinTopWidth = ThinTopPixels / (float)InnerPixels;
    public const float FrontFaceDepth = FrontFacePixels / (float)InnerPixels;
    public const float WestSideDepth = WestSidePixels / (float)InnerPixels;
    public const float EastSideDepth = EastSidePixels / (float)InnerPixels;
    public const float OutlineWidth = OutlinePixels / (float)InnerPixels;

    public const float BrickTopToFrontLuminance = 107.92f / 228.88f;
    public const float BrickTopToSideLuminance = 109.41f / 173.09f;

    public static int HorizontalWorldPhase(int x) => PositiveMod(x, InnerPixels);

    public static int VerticalWorldPhase(HybridWallRayMask ray, int y) => ray switch
    {
        HybridWallRayMask.South => PositiveMod(y + 30, InnerPixels),
        HybridWallRayMask.North => PositiveMod(y - 29, InnerPixels),
        _ => PositiveMod(y, InnerPixels),
    };

    public static HybridWallUvRect TopUv { get; } = SubRect(
        InnerTile(10),
        xPixels: 14,
        yPixels: 25,
        widthPixels: TopSourcePixels,
        heightPixels: TopSourcePixels);

    public static HybridWallUvRect FrontUv { get; } = SubRect(
        InnerTile(10),
        xPixels: 0,
        yPixels: 3,
        widthPixels: InnerPixels,
        heightPixels: FrontFacePixels);

    public static HybridWallUvRect WestSideUv { get; } = SubRect(
        InnerTile(5),
        xPixels: 3,
        yPixels: 0,
        widthPixels: WestSidePixels,
        heightPixels: InnerPixels);

    public static HybridWallUvRect EastSideUv { get; } = SubRect(
        InnerTile(5),
        xPixels: 47,
        yPixels: 0,
        widthPixels: EastSidePixels,
        heightPixels: InnerPixels);

    public static HybridWallUvRect InnerTile(int linkIndex)
    {
        if (linkIndex < 0 || linkIndex > 15)
        {
            throw new ArgumentOutOfRangeException(nameof(linkIndex), linkIndex, "A Core linked atlas has 16 slots.");
        }

        int column = linkIndex % 4;
        int row = linkIndex / 4;
        return new HybridWallUvRect(
            (column * SlotPixels + GutterPixels) / (float)AtlasPixels,
            (row * SlotPixels + GutterPixels) / (float)AtlasPixels,
            InnerPixels / (float)AtlasPixels,
            InnerPixels / (float)AtlasPixels);
    }

    public static HybridWallUvRect Slice(
        HybridWallUvRect source,
        float normalizedMinX,
        float normalizedMinY,
        float normalizedMaxX,
        float normalizedMaxY)
    {
        return new HybridWallUvRect(
            source.X + source.Width * normalizedMinX,
            source.Y + source.Height * normalizedMinY,
            source.Width * (normalizedMaxX - normalizedMinX),
            source.Height * (normalizedMaxY - normalizedMinY));
    }

    private static HybridWallUvRect SubRect(
        HybridWallUvRect tile,
        int xPixels,
        int yPixels,
        int widthPixels,
        int heightPixels)
    {
        return new HybridWallUvRect(
            tile.X + xPixels / (float)AtlasPixels,
            tile.Y + yPixels / (float)AtlasPixels,
            widthPixels / (float)AtlasPixels,
            heightPixels / (float)AtlasPixels);
    }

    private static int PositiveMod(int value, int modulus)
    {
        int result = value % modulus;
        return result < 0 ? result + modulus : result;
    }
}
