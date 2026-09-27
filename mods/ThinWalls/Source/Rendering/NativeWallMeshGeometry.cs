using System;
using System.Collections.Generic;
using System.Linq;

namespace ThinWalls.Rendering;

/// <summary>Convex source-bound mesh geometry helpers; no texture data is created or copied.</summary>
public static class NativeWallMeshGeometry
{
    public static IReadOnlyList<NativeWallMeshVertex> Vertices(NativeWallMeshQuad quad)
    {
        if (quad.Vertices != null) return quad.Vertices;
        HybridWallUvRect d = quad.Destination;
        HybridWallUvRect uv = quad.Source;
        NativeWallMeshVertex[] corners =
        {
            new(d.X, d.Y, uv.X, uv.Y),
            new(d.X, d.YMax, uv.X, uv.YMax),
            new(d.XMax, d.YMax, uv.XMax, uv.YMax),
            new(d.XMax, d.Y, uv.XMax, uv.Y),
        };
        return quad.Triangle switch
        {
            1 => new[] { corners[0], corners[1], corners[2] },
            2 => new[] { corners[0], corners[2], corners[3] },
            3 => new[] { corners[0], corners[1], corners[3] },
            4 => new[] { corners[1], corners[2], corners[3] },
            _ => corners,
        };
    }

    public static NativeWallMeshQuad? ClipToRect(
        NativeWallMeshQuad source,
        float minX,
        float minY,
        float maxX,
        float maxY)
    {
        if (maxX <= minX || maxY <= minY) throw new ArgumentOutOfRangeException(nameof(maxX));
        List<NativeWallMeshVertex> vertices = Vertices(source).ToList();
        vertices = Clip(vertices, vertex => vertex.X >= minX - 0.00001f,
            (a, b) => IntersectX(a, b, minX));
        vertices = Clip(vertices, vertex => vertex.X <= maxX + 0.00001f,
            (a, b) => IntersectX(a, b, maxX));
        vertices = Clip(vertices, vertex => vertex.Y >= minY - 0.00001f,
            (a, b) => IntersectY(a, b, minY));
        vertices = Clip(vertices, vertex => vertex.Y <= maxY + 0.00001f,
            (a, b) => IntersectY(a, b, maxY));
        if (vertices.Count < 3 || Math.Abs(SignedTwiceArea(vertices)) <= 0.000001f) return null;
        return source.WithVertices(vertices);
    }

    private static List<NativeWallMeshVertex> Clip(
        IReadOnlyList<NativeWallMeshVertex> input,
        Func<NativeWallMeshVertex, bool> inside,
        Func<NativeWallMeshVertex, NativeWallMeshVertex, NativeWallMeshVertex> intersect)
    {
        var output = new List<NativeWallMeshVertex>(input.Count + 2);
        if (input.Count == 0) return output;
        for (int i = 0; i < input.Count; i++)
        {
            NativeWallMeshVertex current = input[i];
            NativeWallMeshVertex next = input[(i + 1) % input.Count];
            bool currentInside = inside(current);
            bool nextInside = inside(next);
            if (currentInside) output.Add(current);
            if (currentInside != nextInside) output.Add(intersect(current, next));
        }
        return RemoveAdjacentDuplicates(output);
    }

    private static NativeWallMeshVertex IntersectX(NativeWallMeshVertex a, NativeWallMeshVertex b, float x)
    {
        float t = (x - a.X) / (b.X - a.X);
        return Lerp(a, b, t);
    }

    private static NativeWallMeshVertex IntersectY(NativeWallMeshVertex a, NativeWallMeshVertex b, float y)
    {
        float t = (y - a.Y) / (b.Y - a.Y);
        return Lerp(a, b, t);
    }

    private static NativeWallMeshVertex Lerp(NativeWallMeshVertex a, NativeWallMeshVertex b, float t) =>
        new(
            a.X + (b.X - a.X) * t,
            a.Y + (b.Y - a.Y) * t,
            a.U + (b.U - a.U) * t,
            a.V + (b.V - a.V) * t);

    private static List<NativeWallMeshVertex> RemoveAdjacentDuplicates(List<NativeWallMeshVertex> vertices)
    {
        var result = new List<NativeWallMeshVertex>(vertices.Count);
        foreach (NativeWallMeshVertex vertex in vertices)
        {
            if (result.Count == 0 || !SamePosition(result[result.Count - 1], vertex)) result.Add(vertex);
        }
        if (result.Count > 1 && SamePosition(result[0], result[result.Count - 1])) result.RemoveAt(result.Count - 1);
        return result;
    }

    private static bool SamePosition(NativeWallMeshVertex a, NativeWallMeshVertex b) =>
        Math.Abs(a.X - b.X) <= 0.000001f && Math.Abs(a.Y - b.Y) <= 0.000001f;

    private static float SignedTwiceArea(IReadOnlyList<NativeWallMeshVertex> vertices)
    {
        float area = 0f;
        for (int i = 0; i < vertices.Count; i++)
        {
            NativeWallMeshVertex a = vertices[i];
            NativeWallMeshVertex b = vertices[(i + 1) % vertices.Count];
            area += a.X * b.Y - b.X * a.Y;
        }
        return area;
    }
}
