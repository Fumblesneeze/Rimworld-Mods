using System;
using System.Collections.Generic;

namespace ThinWalls.Rendering;

public enum HybridWallShadowCastingSide : byte
{
    West = 0,
    East = 1,
    South = 2,
}

public readonly struct HybridWallShadowRun
{
    public HybridWallShadowRun(int y, int minX, int maxX)
    {
        Y = y;
        MinX = minX;
        MaxX = maxX;
    }

    public int Y { get; }
    public int MinX { get; }
    public int MaxX { get; }
}

public readonly struct HybridWallShadowEdge
{
    public HybridWallShadowEdge(
        int minX,
        int minY,
        int maxX,
        int maxY,
        HybridWallShadowCastingSide castingSide)
    {
        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
        CastingSide = castingSide;
    }

    public int MinX { get; }
    public int MinY { get; }
    public int MaxX { get; }
    public int MaxY { get; }
    public HybridWallShadowCastingSide CastingSide { get; }
}

public static class HybridWallShadowMeshTopology
{
    private static readonly int[] VerticalTriangles = { 0, 2, 3, 0, 3, 1 };
    private static readonly int[] SouthTriangles = { 0, 1, 2, 1, 3, 2 };

    public static IReadOnlyList<int> TriangleIndices(HybridWallShadowCastingSide side) =>
        side == HybridWallShadowCastingSide.South
            ? SouthTriangles
            : VerticalTriangles;
}

public sealed class HybridRegularShadowPlan
{
    private readonly bool[] occupied;

    internal HybridRegularShadowPlan(
        bool[] occupied,
        IReadOnlyList<HybridWallShadowRun> runs,
        IReadOnlyList<HybridWallShadowEdge> castingEdges,
        int occupiedCount)
    {
        this.occupied = occupied;
        Runs = runs;
        CastingEdges = castingEdges;
        OccupiedCount = occupiedCount;
    }

    public int Width => HybridWallRasterPlan.Size;
    public int Height => HybridWallRasterPlan.Size;
    public int OccupiedCount { get; }
    public IReadOnlyList<HybridWallShadowRun> Runs { get; }
    public IReadOnlyList<HybridWallShadowEdge> CastingEdges { get; }

    public bool Contains(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
        {
            throw new ArgumentOutOfRangeException();
        }
        return occupied[y * Width + x];
    }
}

public static class HybridRegularShadowCompiler
{
    private const int Size = HybridWallRasterPlan.Size;

    public static HybridRegularShadowPlan Compile(HybridRegularCompositePlan raster)
    {
        if (raster == null)
        {
            throw new ArgumentNullException(nameof(raster));
        }

        var occupied = new bool[Size * Size];
        int occupiedCount = 0;
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            int canvasX = x + HybridRegularRasterCompositor.Padding;
            int canvasY = y + HybridRegularRasterCompositor.Padding;
            HybridWallRasterPixel thin = raster.ThinAt(canvasX, canvasY);
            bool top = thin.IsStructural
                ? thin.Surface == HybridWallRasterSurface.Top
                : raster.OriginalAt(canvasX, canvasY).Surface == HybridWallRasterSurface.Top;
            occupied[y * Size + x] = top;
            if (top)
            {
                occupiedCount++;
            }
        }

        var runs = new List<HybridWallShadowRun>();
        for (int y = 0; y < Size; y++)
        {
            int x = 0;
            while (x < Size)
            {
                while (x < Size && !occupied[y * Size + x])
                {
                    x++;
                }
                int minX = x;
                while (x < Size && occupied[y * Size + x])
                {
                    x++;
                }
                if (minX < x)
                {
                    runs.Add(new HybridWallShadowRun(y, minX, x));
                }
            }
        }

        var edges = new List<HybridWallShadowEdge>();
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            if (!occupied[y * Size + x])
            {
                continue;
            }
            if (x == 0
                    ? !raster.HasTopContinuationAtCanvas(
                        HybridRegularRasterCompositor.Padding - 1,
                        HybridRegularRasterCompositor.Padding + y)
                    : !occupied[y * Size + x - 1])
            {
                edges.Add(new HybridWallShadowEdge(
                    x, y, x, y + 1, HybridWallShadowCastingSide.West));
            }
            if (x == Size - 1
                    ? !raster.HasTopContinuationAtCanvas(
                        HybridRegularRasterCompositor.Padding + Size,
                        HybridRegularRasterCompositor.Padding + y)
                    : !occupied[y * Size + x + 1])
            {
                edges.Add(new HybridWallShadowEdge(
                    x + 1, y, x + 1, y + 1, HybridWallShadowCastingSide.East));
            }
            if (y == 0
                    ? !raster.HasTopContinuationAtCanvas(
                        HybridRegularRasterCompositor.Padding + x,
                        HybridRegularRasterCompositor.Padding - 1)
                    : !occupied[(y - 1) * Size + x])
            {
                edges.Add(new HybridWallShadowEdge(
                    x, y, x + 1, y, HybridWallShadowCastingSide.South));
            }
        }

        return new HybridRegularShadowPlan(
            occupied,
            runs,
            MergeCollinear(edges),
            occupiedCount);
    }

    internal static IReadOnlyList<HybridWallShadowEdge> MergeCollinear(
        IReadOnlyList<HybridWallShadowEdge> source)
    {
        var remaining = new List<HybridWallShadowEdge>(source);
        remaining.Sort((left, right) =>
        {
            bool leftVertical = left.MinX == left.MaxX;
            bool rightVertical = right.MinX == right.MaxX;
            int axis = leftVertical.CompareTo(rightVertical);
            if (axis != 0) return axis;
            int fixedAxis = leftVertical
                ? left.MinX.CompareTo(right.MinX)
                : left.MinY.CompareTo(right.MinY);
            if (fixedAxis != 0) return fixedAxis;
            return leftVertical
                ? left.MinY.CompareTo(right.MinY)
                : left.MinX.CompareTo(right.MinX);
        });

        var merged = new List<HybridWallShadowEdge>();
        foreach (HybridWallShadowEdge edge in remaining)
        {
            if (merged.Count == 0)
            {
                merged.Add(edge);
                continue;
            }
            HybridWallShadowEdge previous = merged[merged.Count - 1];
            bool sameSide = previous.CastingSide == edge.CastingSide;
            bool verticalJoin = sameSide && previous.MinX == previous.MaxX && edge.MinX == edge.MaxX &&
                                previous.MinX == edge.MinX && previous.MaxY == edge.MinY;
            bool horizontalJoin = sameSide && previous.MinY == previous.MaxY && edge.MinY == edge.MaxY &&
                                  previous.MinY == edge.MinY && previous.MaxX == edge.MinX;
            if (verticalJoin)
            {
                merged[merged.Count - 1] = new HybridWallShadowEdge(
                    previous.MinX, previous.MinY, edge.MaxX, edge.MaxY, previous.CastingSide);
            }
            else if (horizontalJoin)
            {
                merged[merged.Count - 1] = new HybridWallShadowEdge(
                    previous.MinX, previous.MinY, edge.MaxX, edge.MaxY, previous.CastingSide);
            }
            else
            {
                merged.Add(edge);
            }
        }
        return merged;
    }
}

public static class HybridThinShadowCompiler
{
    private const int Size = HybridWallRasterPlan.Size;
    private const int Center = Size / 2;

    public static HybridRegularShadowPlan Compile(
        HybridWallRayMask rays,
        HybridWallRayMask doubledRays)
    {
        doubledRays &= rays;
        var occupied = new bool[Size * Size];

        FillVertical(HybridWallRayMask.North, Center, Size - 1);
        FillHorizontal(HybridWallRayMask.East, Center, Size - 1);
        FillVertical(HybridWallRayMask.South, 0, Center);
        FillHorizontal(HybridWallRayMask.West, 0, Center);

        var runs = new List<HybridWallShadowRun>();
        int occupiedCount = 0;
        for (int y = 0; y < Size; y++)
        {
            int x = 0;
            while (x < Size)
            {
                while (x < Size && !At(x, y)) x++;
                int minX = x;
                while (x < Size && At(x, y))
                {
                    occupiedCount++;
                    x++;
                }
                if (minX < x)
                {
                    runs.Add(new HybridWallShadowRun(y, minX, x));
                }
            }
        }

        var edges = new List<HybridWallShadowEdge>();
        for (int y = 0; y < Size; y++)
        for (int x = 0; x < Size; x++)
        {
            if (!At(x, y)) continue;

            // A raster boundary bisects a real edge and is continued by the other
            // endpoint vertex partition. It can never be a celestial casting edge.
            if (x > 0 && !At(x - 1, y))
                edges.Add(new HybridWallShadowEdge(
                    x, y, x, y + 1, HybridWallShadowCastingSide.West));
            if (x < Size - 1 && !At(x + 1, y))
                edges.Add(new HybridWallShadowEdge(
                    x + 1, y, x + 1, y + 1, HybridWallShadowCastingSide.East));
            if (y > 0 && !At(x, y - 1))
                edges.Add(new HybridWallShadowEdge(
                    x, y, x + 1, y, HybridWallShadowCastingSide.South));
        }

        return new HybridRegularShadowPlan(
            occupied,
            runs,
            HybridRegularShadowCompiler.MergeCollinear(edges),
            occupiedCount);

        void FillVertical(HybridWallRayMask ray, int minY, int maxY)
        {
            if (!rays.HasFlag(ray)) return;
            (int min, int max) = CenteredBand(Width(ray));
            Fill(min, minY, max, maxY);
        }

        void FillHorizontal(HybridWallRayMask ray, int minX, int maxX)
        {
            if (!rays.HasFlag(ray)) return;
            (int min, int max) = CenteredBand(Width(ray));
            Fill(minX, min, maxX, max);
        }

        int Width(HybridWallRayMask ray) => doubledRays.HasFlag(ray)
            ? HybridWallRasterCompiler.DoubledTopWidth
            : HybridWallRasterCompiler.StandardTopWidth;

        static (int Min, int Max) CenteredBand(int width)
        {
            int min = Center - width / 2;
            return (min, min + width - 1);
        }

        void Fill(int minX, int minY, int maxX, int maxY)
        {
            for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
                occupied[y * Size + x] = true;
        }

        bool At(int x, int y) => occupied[y * Size + x];
    }
}
