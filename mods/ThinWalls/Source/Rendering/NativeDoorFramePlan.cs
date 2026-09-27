using System;
using System.Collections.Generic;

namespace ThinWalls.Rendering;

/// <summary>
/// A fixed, flush three-pixel frame sourced from the matching Core straight-wall
/// slot. Moving Core door leaves occupy the remainder of the shared edge.
/// </summary>
public static class NativeDoorFramePlan
{
    public static IReadOnlyList<NativeWallMeshQuad> ForRay(HybridWallRayMask ray)
    {
        bool horizontal = ray == HybridWallRayMask.East || ray == HybridWallRayMask.West;
        if (!horizontal && ray != HybridWallRayMask.North && ray != HybridWallRayMask.South)
            throw new ArgumentOutOfRangeException(nameof(ray));
        bool positive = ray == HybridWallRayMask.East || ray == HybridWallRayMask.North;
        float minimum = positive ? 0f : -NativeDoorMoverPlan.FrameLength;
        float maximum = positive ? NativeDoorMoverPlan.FrameLength : 0f;
        var result = new List<NativeWallMeshQuad>();
        foreach (NativeWallMeshQuad band in NativeWallMeshPlan.Straight(horizontal))
        {
            HybridWallUvRect d = band.Destination;
            result.Add(new NativeWallMeshQuad(
                horizontal
                    ? new HybridWallUvRect(minimum, d.Y, maximum - minimum, d.Height)
                    : new HybridWallUvRect(d.X, minimum, d.Width, maximum - minimum),
                horizontal
                    ? new HybridWallUvRect(minimum + 0.5f, band.Source.Y,
                        maximum - minimum, band.Source.Height)
                    : new HybridWallUvRect(band.Source.X, minimum + 0.5f,
                        band.Source.Width, maximum - minimum),
                band.SourceLinks));
        }
        return result;
    }
}
