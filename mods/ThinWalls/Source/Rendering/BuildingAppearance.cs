using UnityEngine;

namespace ThinWalls.Rendering;

/// <summary>Cosmetic choices only; never changes a building's gameplay geometry.</summary>
public readonly struct BuildingAppearance
{
    public int ScaleStep { get; }
    public int ScalePercent => 100 - ScaleStep * 10;
    public float Scale => ScalePercent / 100f;
    public int OffsetStep { get; }
    public bool OffsetIsManual { get; }
    public float OffsetX => OffsetStep is 2 or 3 or 4 ? .2f : OffsetStep is 6 or 7 or 8 ? -.2f : 0f;
    public float OffsetZ => OffsetStep is 1 or 2 or 8 ? .2f : OffsetStep is 4 or 5 or 6 ? -.2f : 0f;
    public bool IsDefault => ScaleStep == 0 && OffsetStep == 0;

    public BuildingAppearance(int scaleStep = 0, int offsetStep = 0, bool? offsetIsManual = null)
    {
        ScaleStep = scaleStep >= 0 && scaleStep <= 5 ? scaleStep : 0;
        OffsetStep = offsetStep >= 0 && offsetStep <= 8 ? offsetStep : 0;
        // Missing flag is the legacy-save contract: only a nonzero offset was an explicit choice.
        OffsetIsManual = offsetIsManual ?? OffsetStep != 0;
    }

    public BuildingAppearance NextScale() => new((ScaleStep + 1) % 6, OffsetStep, OffsetIsManual);
    public BuildingAppearance NextOffset() => new(ScaleStep, (OffsetStep + 1) % 9, true);
    public BuildingAppearance WithAutomaticOffset(int offsetStep) =>
        OffsetIsManual ? this : new BuildingAppearance(ScaleStep, offsetStep, false);

    public Vector3 Transform(Vector3 point, Vector3 pivot) => new(
        pivot.x + (point.x - pivot.x) * Scale + OffsetX,
        point.y,
        pivot.z + (point.z - pivot.z) * Scale + OffsetZ);

    // Left-compose the world-space cosmetic transform. Every part retains its local rotation,
    // dimensions, material and relative displacement from the shared building center.
    public Matrix4x4 TransformMatrix(Matrix4x4 matrix, Vector3 pivot)
    {
        float scale = Scale;
        for (int column = 0; column < 4; column++)
        {
            matrix[0, column] *= scale;
            matrix[2, column] *= scale;
        }
        matrix.m03 += (1f - scale) * pivot.x + OffsetX;
        matrix.m23 += (1f - scale) * pivot.z + OffsetZ;
        return matrix;
    }
}
