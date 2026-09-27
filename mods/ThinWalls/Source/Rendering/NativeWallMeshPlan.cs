using System;
using System.Collections.Generic;

namespace ThinWalls.Rendering;

public readonly struct NativeWallMeshVertex
{
    public NativeWallMeshVertex(float x, float y, float u, float v)
    {
        X = x;
        Y = y;
        U = u;
        V = v;
    }

    public float X { get; }
    public float Y { get; }
    public float U { get; }
    public float V { get; }
}

/// <summary>A source-material quad. UVs refer to the unchanged inner Core linked tile.</summary>
public readonly struct NativeWallMeshQuad
{
    public NativeWallMeshQuad(HybridWallUvRect destination, HybridWallUvRect source, HybridWallRayMask sourceLinks, byte triangle = 0)
        : this(destination, source, sourceLinks, triangle, 0f, 0f, 0f, null)
    {
    }

    private NativeWallMeshQuad(HybridWallUvRect destination, HybridWallUvRect source,
        HybridWallRayMask sourceLinks, byte triangle, float altitudeOffset, float altitudeSlopeX,
        float altitudeSlopeY, IReadOnlyList<NativeWallMeshVertex>? vertices)
    {
        Destination = destination;
        Source = source;
        SourceLinks = sourceLinks;
        Triangle = triangle;
        AltitudeOffset = altitudeOffset;
        AltitudeSlopeX = altitudeSlopeX;
        AltitudeSlopeY = altitudeSlopeY;
        Vertices = vertices;
    }

    public HybridWallUvRect Destination { get; }
    public HybridWallUvRect Source { get; }
    public HybridWallRayMask SourceLinks { get; }
    // 0 = full rectangle; 1/2 split BL-TR; 3/4 split TL-BR.
    public byte Triangle { get; }
    public float AltitudeOffset { get; }
    public float AltitudeSlopeX { get; }
    public float AltitudeSlopeY { get; }
    public IReadOnlyList<NativeWallMeshVertex>? Vertices { get; }

    public NativeWallMeshQuad WithAltitude(float offset, float slopeX, float slopeY) =>
        new(Destination, Source, SourceLinks, Triangle, offset, slopeX, slopeY, Vertices);

    public NativeWallMeshQuad WithVertices(IReadOnlyList<NativeWallMeshVertex> vertices)
    {
        if (vertices == null || vertices.Count < 3)
            throw new ArgumentException("A native wall polygon needs at least three vertices.", nameof(vertices));
        float minX = float.MaxValue;
        float minY = float.MaxValue;
        float maxX = float.MinValue;
        float maxY = float.MinValue;
        float minU = float.MaxValue;
        float minV = float.MaxValue;
        float maxU = float.MinValue;
        float maxV = float.MinValue;
        foreach (NativeWallMeshVertex vertex in vertices)
        {
            minX = Math.Min(minX, vertex.X);
            minY = Math.Min(minY, vertex.Y);
            maxX = Math.Max(maxX, vertex.X);
            maxY = Math.Max(maxY, vertex.Y);
            minU = Math.Min(minU, vertex.U);
            minV = Math.Min(minV, vertex.V);
            maxU = Math.Max(maxU, vertex.U);
            maxV = Math.Max(maxV, vertex.V);
        }
        return new NativeWallMeshQuad(
            new HybridWallUvRect(minX, minY, maxX - minX, maxY - minY),
            new HybridWallUvRect(minU, minV, maxU - minU, maxV - minV),
            SourceLinks,
            Triangle,
            AltitudeOffset,
            AltitudeSlopeX,
            AltitudeSlopeY,
            vertices);
    }

    public float AltitudeAt(float x, float y) => AltitudeOffset + AltitudeSlopeX * x + AltitudeSlopeY * y;
}

/// <summary>Geometry only: never allocates, reads, paints, or substitutes a texture.</summary>
public static class NativeWallMeshPlan
{
    public const float CompletedAltitudeOffset = 0.006f;

    public static IReadOnlyList<NativeWallMeshQuad> HalfRay(HybridWallRayMask ray)
    {
        bool horizontal = ray is HybridWallRayMask.East or HybridWallRayMask.West;
        if (!horizontal && ray is not (HybridWallRayMask.North or HybridWallRayMask.South))
            throw new ArgumentOutOfRangeException(nameof(ray));
        bool positive = ray is HybridWallRayMask.East or HybridWallRayMask.North;
        var result = new List<NativeWallMeshQuad>();
        foreach (NativeWallMeshQuad band in Straight(horizontal))
        {
            HybridWallUvRect d = band.Destination;
            result.Add(new NativeWallMeshQuad(
                horizontal
                    ? new HybridWallUvRect(positive ? 0f : -0.5f, d.Y, 0.5f, d.Height)
                    : new HybridWallUvRect(d.X, positive ? 0f : -0.5f, d.Width, 0.5f),
                horizontal
                    ? new HybridWallUvRect(positive ? 0.5f : 0f, band.Source.Y, 0.5f, band.Source.Height)
                    : new HybridWallUvRect(band.Source.X, positive ? 0.5f : 0f, band.Source.Width, 0.5f),
                band.SourceLinks));
        }
        return result;
    }

    public static IReadOnlyList<NativeWallMeshQuad> Topology(HybridWallRayMask rays)
    {
        if ((int)rays > 15) throw new ArgumentOutOfRangeException(nameof(rays));
        if (rays == HybridWallRayMask.None) return Array.Empty<NativeWallMeshQuad>();
        if (rays == (HybridWallRayMask.East | HybridWallRayMask.West)) return Straight(true);
        if (rays == (HybridWallRayMask.North | HybridWallRayMask.South)) return Straight(false);
        if (rays is HybridWallRayMask.North or HybridWallRayMask.East or
            HybridWallRayMask.South or HybridWallRayMask.West)
            return TerminalRay(rays);
        return JunctionTopology(rays);
    }

    private static IReadOnlyList<NativeWallMeshQuad> TerminalRay(HybridWallRayMask ray)
    {
        const float body = 17f / 60f;
        IReadOnlyList<NativeWallMeshQuad> full = JunctionTopology(ray);
        var result = new List<NativeWallMeshQuad>(full.Count);

        switch (ray)
        {
            case HybridWallRayMask.East:
                AddClipped(-body, -body, 0f, body, body, 0f);
                AddClipped(body, -body, 0.5f, body, 0f, 0f);
                break;
            case HybridWallRayMask.West:
                AddClipped(0f, -body, body, body, -body, 0f);
                AddClipped(-0.5f, -body, -body, body, 0f, 0f);
                break;
            case HybridWallRayMask.North:
                AddClipped(-body, -body, body, 0f, 0f, body);
                AddClipped(-body, body, body, 0.5f, 0f, 0f);
                break;
            case HybridWallRayMask.South:
                AddClipped(-body, 0f, body, body, 0f, -body);
                AddClipped(-body, -0.5f, body, -body, 0f, 0f);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(ray));
        }

        return result;

        void AddClipped(float minX, float minY, float maxX, float maxY, float shiftX, float shiftY)
        {
            foreach (NativeWallMeshQuad quad in full)
            {
                NativeWallMeshQuad? clipped = NativeWallMeshGeometry.ClipToRect(quad, minX, minY, maxX, maxY);
                if (!clipped.HasValue) continue;
                if (Math.Abs(shiftX) <= 0.000001f && Math.Abs(shiftY) <= 0.000001f)
                {
                    result.Add(clipped.Value);
                    continue;
                }

                IReadOnlyList<NativeWallMeshVertex> sourceVertices = NativeWallMeshGeometry.Vertices(clipped.Value);
                var shifted = new NativeWallMeshVertex[sourceVertices.Count];
                for (int i = 0; i < sourceVertices.Count; i++)
                {
                    NativeWallMeshVertex vertex = sourceVertices[i];
                    shifted[i] = new NativeWallMeshVertex(
                        vertex.X + shiftX,
                        vertex.Y + shiftY,
                        vertex.U,
                        vertex.V);
                }
                result.Add(clipped.Value.WithVertices(shifted));
            }
        }
    }

    private static IReadOnlyList<NativeWallMeshQuad> JunctionTopology(HybridWallRayMask rays)
    {
        bool n = rays.HasFlag(HybridWallRayMask.North);
        bool e = rays.HasFlag(HybridWallRayMask.East);
        bool s = rays.HasFlag(HybridWallRayMask.South);
        bool w = rays.HasFlag(HybridWallRayMask.West);
        var quads = new List<NativeWallMeshQuad>(32);
        const HybridWallRayMask h = HybridWallRayMask.East | HybridWallRayMask.West;
        const HybridWallRayMask v = HybridWallRayMask.North | HybridWallRayMask.South;

        // The measured Core corner is split into its two actual projected faces.
        // The diagonal stays at the native face-depth angle; each face retains
        // world-length UV phase all the way through its incoming straight strip.
        quads.Add(new NativeWallMeshQuad(Rect(-3, 8, 4, 15), Rect(14, 25, 47, 58), rays));
        Patch(-3, -17, 4, 8, !s, s ? 14 : 0, s ? 47 : 25);
        Patch(-3, 15, 4, 17, !n, n ? 14 : 58, n ? 47 : 60);
        Patch(-17, 8, -3, 15, w, w ? 25 : 0, w ? 58 : 14);
        Patch(4, 8, 17, 15, e, e ? 25 : 47, e ? 58 : 60);
        Corner(-17, -17, -3, 8, w, s, 0, 25, 0, 14, 1, 2);
        Corner(4, -17, 17, 8, e, s, 0, 25, 47, 60, 4, 3);
        Corner(-17, 15, -3, 17, w, n, 58, 60, 0, 14, 3, 4);
        Corner(4, 15, 17, 17, e, n, 58, 60, 47, 60, 2, 1);
        AddRay(HybridWallRayMask.West, true, -0.5f, -17f / 60f);
        AddRay(HybridWallRayMask.East, true, 17f / 60f, 0.5f);
        AddRay(HybridWallRayMask.South, false, -0.5f, -17f / 60f);
        AddRay(HybridWallRayMask.North, false, 17f / 60f, 0.5f);
        return quads;

        void Patch(int x0, int y0, int x1, int y1, bool horizontal, int source0, int source1, byte triangle = 0)
        {
            HybridWallUvRect d = Rect(x0, y0, x1, y1);
            HybridWallUvRect uv = horizontal
                ? new HybridWallUvRect(d.X + 0.5f, source0 / 60f, d.Width, (source1 - source0) / 60f)
                : new HybridWallUvRect(source0 / 60f, d.Y + 0.5f, (source1 - source0) / 60f, d.Height);
            quads.Add(new NativeWallMeshQuad(d, uv, horizontal ? h : v, triangle));
        }

        void Corner(int x0, int y0, int x1, int y1, bool horizontalRay, bool verticalRay,
            int h0, int h1, int v0, int v1, byte upperSide, byte lowerSide)
        {
            if (horizontalRay != verticalRay)
            {
                Patch(x0, y0, x1, y1, horizontalRay, horizontalRay ? h0 : v0, horizontalRay ? h1 : v1);
                return;
            }
            // Concave joins reverse ownership across the same diagonal.
            Patch(x0, y0, x1, y1, horizontalRay, horizontalRay ? h0 : v0, horizontalRay ? h1 : v1, upperSide);
            Patch(x0, y0, x1, y1, !horizontalRay, horizontalRay ? v0 : h0, horizontalRay ? v1 : h1, lowerSide);
        }

        void AddRay(HybridWallRayMask ray, bool horizontal, float min, float max)
        {
            if (!rays.HasFlag(ray)) return;
            foreach (var band in Straight(horizontal))
            {
                HybridWallUvRect d = band.Destination;
                HybridWallUvRect uv = band.Source;
                quads.Add(new NativeWallMeshQuad(
                    horizontal ? new HybridWallUvRect(min, d.Y, max - min, d.Height) : new HybridWallUvRect(d.X, min, d.Width, max - min),
                    horizontal ? new HybridWallUvRect(min + 0.5f, uv.Y, max - min, uv.Height) : new HybridWallUvRect(uv.X, min + 0.5f, uv.Width, max - min),
                    band.SourceLinks));
            }
        }
    }

    private static HybridWallUvRect Rect(int x0, int y0, int x1, int y1) =>
        new(x0 / 60f, y0 / 60f, (x1 - x0) / 60f, (y1 - y0) / 60f);

    public static IReadOnlyList<NativeWallMeshQuad> Straight(bool horizontal)
    {
        // Unity's source coordinates are bottom-up. Only the 33-pixel top is
        // compressed; native mortar, face height, outline and length survive.
        int[] source = horizontal ? new[] { 0, 3, 25, 58, 60 } : new[] { 0, 3, 14, 47, 57, 60 };
        int[] target = horizontal ? new[] { -17, -14, 8, 15, 17 } : new[] { -17, -14, -3, 4, 14, 17 };
        var quads = new List<NativeWallMeshQuad>(source.Length - 1);
        for (int i = 0; i < source.Length - 1; i++)
            quads.Add(new NativeWallMeshQuad(
                horizontal
                    ? new HybridWallUvRect(-0.5f, target[i] / 60f, 1f, (target[i + 1] - target[i]) / 60f)
                    : new HybridWallUvRect(target[i] / 60f, -0.5f, (target[i + 1] - target[i]) / 60f, 1f),
                horizontal
                    ? new HybridWallUvRect(0f, source[i] / 60f, 1f, (source[i + 1] - source[i]) / 60f)
                    : new HybridWallUvRect(source[i] / 60f, 0f, (source[i + 1] - source[i]) / 60f, 1f),
                horizontal ? HybridWallRayMask.East | HybridWallRayMask.West : HybridWallRayMask.North | HybridWallRayMask.South));
        return quads;
    }
}
