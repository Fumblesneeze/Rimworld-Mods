using System;
using System.Collections.Generic;

namespace ThinWalls.Rendering;

public enum HybridWallSurfaceDecorationKind : byte
{
    OsbFlake = 0,
    OsbFiber = 1,
    PlateSeam = 2,
    Rivet = 3,
}

public readonly struct HybridWallSurfaceDecoration : IEquatable<HybridWallSurfaceDecoration>
{
    public HybridWallSurfaceDecoration(
        HybridWallSurfaceDecorationKind kind,
        float minU,
        float minV,
        float maxU,
        float maxV)
    {
        Kind = kind;
        MinU = minU;
        MinV = minV;
        MaxU = maxU;
        MaxV = maxV;
    }

    public HybridWallSurfaceDecorationKind Kind { get; }

    public float MinU { get; }

    public float MinV { get; }

    public float MaxU { get; }

    public float MaxV { get; }

    public bool Equals(HybridWallSurfaceDecoration other) =>
        Kind == other.Kind &&
        MinU.Equals(other.MinU) && MinV.Equals(other.MinV) &&
        MaxU.Equals(other.MaxU) && MaxV.Equals(other.MaxV);

    public override bool Equals(object? obj) => obj is HybridWallSurfaceDecoration other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (int)Kind;
            hash = (hash * 397) ^ MinU.GetHashCode();
            hash = (hash * 397) ^ MinV.GetHashCode();
            hash = (hash * 397) ^ MaxU.GetHashCode();
            return (hash * 397) ^ MaxV.GetHashCode();
        }
    }
}

public static class HybridWallSurfaceDecorationRecipe
{
    private static readonly HybridWallSurfaceDecoration[] Wood =
    {
        Mark(HybridWallSurfaceDecorationKind.OsbFlake, 0.07f, 0.19f, 0.15f, 0.24f),
        Mark(HybridWallSurfaceDecorationKind.OsbFiber, 0.20f, 0.67f, 0.36f, 0.71f),
        Mark(HybridWallSurfaceDecorationKind.OsbFlake, 0.42f, 0.36f, 0.50f, 0.42f),
        Mark(HybridWallSurfaceDecorationKind.OsbFiber, 0.57f, 0.80f, 0.73f, 0.84f),
        Mark(HybridWallSurfaceDecorationKind.OsbFlake, 0.78f, 0.23f, 0.88f, 0.29f),
        Mark(HybridWallSurfaceDecorationKind.OsbFiber, 0.11f, 0.48f, 0.27f, 0.52f),
        Mark(HybridWallSurfaceDecorationKind.OsbFlake, 0.63f, 0.53f, 0.70f, 0.59f),
        Mark(HybridWallSurfaceDecorationKind.OsbFiber, 0.33f, 0.12f, 0.48f, 0.16f),
    };

    private static readonly HybridWallSurfaceDecoration[] Metal =
    {
        Mark(HybridWallSurfaceDecorationKind.PlateSeam, 0.04f, 0.48f, 0.96f, 0.52f),
        Mark(HybridWallSurfaceDecorationKind.Rivet, 0.08f, 0.18f, 0.115f, 0.24f),
        Mark(HybridWallSurfaceDecorationKind.Rivet, 0.31f, 0.18f, 0.345f, 0.24f),
        Mark(HybridWallSurfaceDecorationKind.Rivet, 0.65f, 0.18f, 0.685f, 0.24f),
        Mark(HybridWallSurfaceDecorationKind.Rivet, 0.88f, 0.18f, 0.915f, 0.24f),
        Mark(HybridWallSurfaceDecorationKind.Rivet, 0.08f, 0.76f, 0.115f, 0.82f),
        Mark(HybridWallSurfaceDecorationKind.Rivet, 0.31f, 0.76f, 0.345f, 0.82f),
        Mark(HybridWallSurfaceDecorationKind.Rivet, 0.65f, 0.76f, 0.685f, 0.82f),
        Mark(HybridWallSurfaceDecorationKind.Rivet, 0.88f, 0.76f, 0.915f, 0.82f),
    };

    public static IReadOnlyList<HybridWallSurfaceDecoration> Compile(ThinWallMaterialFamily family) =>
        family switch
        {
            ThinWallMaterialFamily.Wood => Wood,
            ThinWallMaterialFamily.Metal => Metal,
            _ => Array.Empty<HybridWallSurfaceDecoration>(),
        };

    private static HybridWallSurfaceDecoration Mark(
        HybridWallSurfaceDecorationKind kind,
        float minU,
        float minV,
        float maxU,
        float maxV) => new(kind, minU, minV, maxU, maxV);
}
