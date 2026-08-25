using System;
using System.Collections.Generic;
using System.Linq;

namespace ThinWalls.Rendering;

public enum HybridWallArmKind : byte
{
    None = 0,
    Thin = 1,
    Ordinary = 2,
}

public enum HybridWallDirection : byte
{
    North = 0,
    East = 1,
    South = 2,
    West = 3,
}

public enum HybridWallComponentKind : byte
{
    UnionSurface = 0,
}

[Flags]
public enum HybridWallRayMask : byte
{
    None = 0,
    North = 1,
    East = 2,
    South = 4,
    West = 8,
}

[Flags]
public enum HybridWallQuadrant : byte
{
    None = 0,
    NorthEast = 1,
    NorthWest = 2,
    SouthEast = 4,
    SouthWest = 8,
}

public readonly struct HybridWallVertexTopology
{
    private HybridWallVertexTopology(
        HybridWallQuadrant ordinaryQuadrants,
        HybridWallRayMask thinRays,
        HybridWallTopologyKey arms)
    {
        OrdinaryQuadrants = ordinaryQuadrants;
        ThinRays = thinRays;
        Arms = arms;
    }

    public HybridWallQuadrant OrdinaryQuadrants { get; }

    public HybridWallRayMask ThinRays { get; }

    public HybridWallTopologyKey Arms { get; }

    public static HybridWallVertexTopology FromOccupancy(
        HybridWallQuadrant ordinaryQuadrants,
        HybridWallRayMask thinRays)
    {
        return new HybridWallVertexTopology(
            ordinaryQuadrants,
            thinRays,
            new HybridWallTopologyKey(
                Arm(thinRays, HybridWallRayMask.North),
                Arm(thinRays, HybridWallRayMask.East),
                Arm(thinRays, HybridWallRayMask.South),
                Arm(thinRays, HybridWallRayMask.West)));
    }

    private static HybridWallArmKind Arm(HybridWallRayMask rays, HybridWallRayMask ray) =>
        rays.HasFlag(ray) ? HybridWallArmKind.Thin : HybridWallArmKind.None;
}

public readonly struct HybridWallTopologyKey : IEquatable<HybridWallTopologyKey>
{
    public static HybridWallTopologyKey Empty { get; } = new(
        HybridWallArmKind.None,
        HybridWallArmKind.None,
        HybridWallArmKind.None,
        HybridWallArmKind.None);

    public HybridWallTopologyKey(
        HybridWallArmKind north,
        HybridWallArmKind east,
        HybridWallArmKind south,
        HybridWallArmKind west)
    {
        North = north;
        East = east;
        South = south;
        West = west;
    }

    public HybridWallArmKind North { get; }

    public HybridWallArmKind East { get; }

    public HybridWallArmKind South { get; }

    public HybridWallArmKind West { get; }

    public bool IsEmpty =>
        North == HybridWallArmKind.None &&
        East == HybridWallArmKind.None &&
        South == HybridWallArmKind.None &&
        West == HybridWallArmKind.None;

    public IReadOnlyList<HybridWallDirection> OccupiedDirections
    {
        get
        {
            var result = new List<HybridWallDirection>(4);
            if (North != HybridWallArmKind.None)
            {
                result.Add(HybridWallDirection.North);
            }
            if (East != HybridWallArmKind.None)
            {
                result.Add(HybridWallDirection.East);
            }
            if (South != HybridWallArmKind.None)
            {
                result.Add(HybridWallDirection.South);
            }
            if (West != HybridWallArmKind.None)
            {
                result.Add(HybridWallDirection.West);
            }
            return result;
        }
    }

    public HybridWallArmKind this[HybridWallDirection direction] => direction switch
    {
        HybridWallDirection.North => North,
        HybridWallDirection.East => East,
        HybridWallDirection.South => South,
        HybridWallDirection.West => West,
        _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
    };

    public static HybridWallTopologyKey FromArms(
        params (HybridWallDirection Direction, HybridWallArmKind Kind)[] arms)
    {
        var values = new HybridWallArmKind[4];
        foreach ((HybridWallDirection direction, HybridWallArmKind kind) in arms)
        {
            int index = (int)direction;
            if ((byte)kind > (byte)values[index])
            {
                values[index] = kind;
            }
        }

        return new HybridWallTopologyKey(values[0], values[1], values[2], values[3]);
    }

    public bool Equals(HybridWallTopologyKey other) =>
        North == other.North && East == other.East && South == other.South && West == other.West;

    public override bool Equals(object? obj) => obj is HybridWallTopologyKey other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (int)North;
            hash = (hash * 397) ^ (int)East;
            hash = (hash * 397) ^ (int)South;
            return (hash * 397) ^ (int)West;
        }
    }

    public override string ToString() => $"N:{North}|E:{East}|S:{South}|W:{West}";

    public static bool operator ==(HybridWallTopologyKey left, HybridWallTopologyKey right) => left.Equals(right);

    public static bool operator !=(HybridWallTopologyKey left, HybridWallTopologyKey right) => !left.Equals(right);

}

public readonly struct HybridWallLine : IEquatable<HybridWallLine>
{
    public HybridWallLine(float x1, float z1, float x2, float z2)
    {
        if (x1 < x2 || (x1.Equals(x2) && z1 <= z2))
        {
            X1 = x1;
            Z1 = z1;
            X2 = x2;
            Z2 = z2;
        }
        else
        {
            X1 = x2;
            Z1 = z2;
            X2 = x1;
            Z2 = z1;
        }
    }

    public float X1 { get; }

    public float Z1 { get; }

    public float X2 { get; }

    public float Z2 { get; }

    public bool IsAxisAligned => X1.Equals(X2) || Z1.Equals(Z2);

    public bool Equals(HybridWallLine other) =>
        X1.Equals(other.X1) && Z1.Equals(other.Z1) && X2.Equals(other.X2) && Z2.Equals(other.Z2);

    public override bool Equals(object? obj) => obj is HybridWallLine other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = X1.GetHashCode();
            hash = (hash * 397) ^ Z1.GetHashCode();
            hash = (hash * 397) ^ X2.GetHashCode();
            return (hash * 397) ^ Z2.GetHashCode();
        }
    }

    public override string ToString() => FormattableString.Invariant($"({X1},{Z1})-({X2},{Z2})");
}

public readonly struct HybridWallSurfaceCell : IEquatable<HybridWallSurfaceCell>
{
    public HybridWallSurfaceCell(float minX, float minZ, float maxX, float maxZ)
    {
        MinX = minX;
        MinZ = minZ;
        MaxX = maxX;
        MaxZ = maxZ;
    }

    public float MinX { get; }

    public float MinZ { get; }

    public float MaxX { get; }

    public float MaxZ { get; }

    public IReadOnlyList<HybridWallLine> Edges => new[]
    {
        new HybridWallLine(MinX, MinZ, MaxX, MinZ),
        new HybridWallLine(MaxX, MinZ, MaxX, MaxZ),
        new HybridWallLine(MinX, MaxZ, MaxX, MaxZ),
        new HybridWallLine(MinX, MinZ, MinX, MaxZ),
    };

    public bool Contains(float x, float z) =>
        x >= MinX && x <= MaxX && z >= MinZ && z <= MaxZ;

    public bool Equals(HybridWallSurfaceCell other) =>
        MinX.Equals(other.MinX) && MinZ.Equals(other.MinZ) &&
        MaxX.Equals(other.MaxX) && MaxZ.Equals(other.MaxZ);

    public override bool Equals(object? obj) => obj is HybridWallSurfaceCell other && Equals(other);

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = MinX.GetHashCode();
            hash = (hash * 397) ^ MinZ.GetHashCode();
            hash = (hash * 397) ^ MaxX.GetHashCode();
            return (hash * 397) ^ MaxZ.GetHashCode();
        }
    }

    public override string ToString() =>
        FormattableString.Invariant($"[{MinX},{MinZ}..{MaxX},{MaxZ}]");
}

public sealed class HybridWallTopologyPlan
{
    internal HybridWallTopologyPlan(
        HybridWallTopologyKey key,
        HybridWallQuadrant ordinaryQuadrants,
        HybridWallRayMask doubledRays,
        IReadOnlyList<HybridWallSurfaceCell> surfaceCells,
        IReadOnlyList<HybridWallLine> exteriorContour,
        bool isConnected,
        bool hasOverlappingSurfaceCells)
    {
        Key = key;
        OrdinaryQuadrants = ordinaryQuadrants;
        DoubledRays = doubledRays;
        SurfaceCells = surfaceCells;
        ExteriorContour = exteriorContour;
        IsConnected = isConnected;
        HasOverlappingSurfaceCells = hasOverlappingSurfaceCells;
        StableSignature = BuildSignature(key, ordinaryQuadrants, doubledRays, surfaceCells, exteriorContour);
    }

    public HybridWallTopologyKey Key { get; }

    public HybridWallQuadrant OrdinaryQuadrants { get; }

    public HybridWallRayMask DoubledRays { get; }

    public IReadOnlyList<HybridWallSurfaceCell> SurfaceCells { get; }

    public IReadOnlyList<HybridWallLine> ExteriorContour { get; }

    public IReadOnlyList<HybridWallLine> InternalContours { get; } = Array.Empty<HybridWallLine>();

    public IReadOnlyList<HybridWallDirection> OccupiedDirections => Key.OccupiedDirections;

    public IReadOnlyList<HybridWallComponentKind> ComponentKinds =>
        SurfaceCells.Count == 0
            ? Array.Empty<HybridWallComponentKind>()
            : new[] { HybridWallComponentKind.UnionSurface };

    public bool IsConnected { get; }

    public bool HasOverlappingSurfaceCells { get; }

    public string StableSignature { get; }

    public float WidthFor(HybridWallDirection direction) => HybridWallTopologyCompiler.WidthFor(
        Key[direction],
        DoubledRays.HasFlag(HybridWallTopologyCompiler.Mask(direction)));

    private static string BuildSignature(
        HybridWallTopologyKey key,
        HybridWallQuadrant ordinaryQuadrants,
        HybridWallRayMask doubledRays,
        IReadOnlyList<HybridWallSurfaceCell> cells,
        IReadOnlyList<HybridWallLine> contour)
    {
        string cellSignature = string.Join(",", cells
            .OrderBy(cell => cell.MinZ)
            .ThenBy(cell => cell.MinX)
            .Select(cell => cell.ToString()));
        string contourSignature = string.Join(",", contour
            .OrderBy(line => line.Z1)
            .ThenBy(line => line.X1)
            .ThenBy(line => line.Z2)
            .ThenBy(line => line.X2)
            .Select(line => line.ToString()));
        return FormattableString.Invariant($"{key};quadrants={ordinaryQuadrants};doubled={doubledRays};cells={cellSignature};contour={contourSignature}");
    }
}

public static class HybridWallTopologyCompiler
{
    public const float ThinTopWidth = 7f / 60f;
    public const float OrdinaryTopWidth = 33f / 60f;

    public static HybridWallTopologyPlan Compile(HybridWallTopologyKey key) =>
        Compile(key, HybridWallQuadrant.None, HybridWallRayMask.None);

    public static HybridWallTopologyPlan Compile(
        HybridWallTopologyKey key,
        HybridWallRayMask doubledRays) =>
        Compile(key, HybridWallQuadrant.None, doubledRays);

    public static HybridWallTopologyPlan Compile(HybridWallVertexTopology topology) =>
        Compile(topology, HybridWallRayMask.None);

    public static HybridWallTopologyPlan Compile(
        HybridWallVertexTopology topology,
        HybridWallRayMask doubledRays) =>
        Compile(topology.Arms, topology.OrdinaryQuadrants, doubledRays);

    private static HybridWallTopologyPlan Compile(
        HybridWallTopologyKey key,
        HybridWallQuadrant ordinaryQuadrants,
        HybridWallRayMask doubledRays)
    {
        List<SurfaceRectangle> surfaces = BuildSurfaces(key, ordinaryQuadrants, doubledRays);
        if (surfaces.Count == 0)
        {
            return new HybridWallTopologyPlan(
                key,
                ordinaryQuadrants,
                doubledRays,
                Array.Empty<HybridWallSurfaceCell>(),
                Array.Empty<HybridWallLine>(),
                isConnected: true,
                hasOverlappingSurfaceCells: false);
        }

        float[] xs = surfaces.SelectMany(surface => new[] { surface.MinX, surface.MaxX }).Distinct().OrderBy(value => value).ToArray();
        float[] zs = surfaces.SelectMany(surface => new[] { surface.MinZ, surface.MaxZ }).Distinct().OrderBy(value => value).ToArray();
        var cells = new List<HybridWallSurfaceCell>();
        for (int x = 0; x < xs.Length - 1; x++)
        {
            for (int z = 0; z < zs.Length - 1; z++)
            {
                float minX = xs[x];
                float maxX = xs[x + 1];
                float minZ = zs[z];
                float maxZ = zs[z + 1];
                float centerX = (minX + maxX) * 0.5f;
                float centerZ = (minZ + maxZ) * 0.5f;
                if (surfaces.Any(surface => surface.Contains(centerX, centerZ)))
                {
                    cells.Add(new HybridWallSurfaceCell(minX, minZ, maxX, maxZ));
                }
            }
        }

        IReadOnlyList<HybridWallLine> contour = ExteriorContour(cells);
        return new HybridWallTopologyPlan(
            key,
            ordinaryQuadrants,
            doubledRays,
            cells,
            contour,
            IsConnected(cells),
            HasOverlap(cells));
    }

    public static float WidthFor(HybridWallArmKind kind, bool doubled = false) => kind switch
    {
        HybridWallArmKind.None => 0f,
        HybridWallArmKind.Thin => doubled ? ThinTopWidth * 2f : ThinTopWidth,
        HybridWallArmKind.Ordinary => OrdinaryTopWidth,
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
    };

    public static IReadOnlyList<HybridWallDirection> DirectionsCovering(
        HybridWallTopologyKey key,
        HybridWallSurfaceCell cell) =>
        DirectionsCovering(key, HybridWallRayMask.None, cell);

    public static IReadOnlyList<HybridWallDirection> DirectionsCovering(
        HybridWallTopologyPlan plan,
        HybridWallSurfaceCell cell) =>
        DirectionsCovering(plan.Key, plan.DoubledRays, cell);

    private static IReadOnlyList<HybridWallDirection> DirectionsCovering(
        HybridWallTopologyKey key,
        HybridWallRayMask doubledRays,
        HybridWallSurfaceCell cell)
    {
        float x = (cell.MinX + cell.MaxX) * 0.5f;
        float z = (cell.MinZ + cell.MaxZ) * 0.5f;
        var result = new List<HybridWallDirection>(4);
        foreach (HybridWallDirection direction in StableDirections)
        {
            float width = WidthFor(key[direction], doubledRays.HasFlag(Mask(direction)));
            if (width <= 0f)
            {
                continue;
            }

            float half = width * 0.5f;
            bool inside = direction switch
            {
                HybridWallDirection.North => x >= -half && x <= half && z >= 0f && z <= 0.5f,
                HybridWallDirection.East => x >= 0f && x <= 0.5f && z >= -half && z <= half,
                HybridWallDirection.South => x >= -half && x <= half && z >= -0.5f && z <= 0f,
                HybridWallDirection.West => x >= -0.5f && x <= 0f && z >= -half && z <= half,
                _ => false,
            };
            if (inside)
            {
                result.Add(direction);
            }
        }

        return result;
    }

    public static IReadOnlyList<HybridWallQuadrant> QuadrantsCovering(
        HybridWallTopologyPlan plan,
        HybridWallSurfaceCell cell)
    {
        float x = (cell.MinX + cell.MaxX) * 0.5f;
        float z = (cell.MinZ + cell.MaxZ) * 0.5f;
        var result = new List<HybridWallQuadrant>(1);
        foreach (HybridWallQuadrant quadrant in StableQuadrants)
        {
            if (!plan.OrdinaryQuadrants.HasFlag(quadrant))
            {
                continue;
            }

            bool inside = quadrant switch
            {
                HybridWallQuadrant.NorthEast => x >= 0f && z >= 0f,
                HybridWallQuadrant.NorthWest => x <= 0f && z >= 0f,
                HybridWallQuadrant.SouthEast => x >= 0f && z <= 0f,
                HybridWallQuadrant.SouthWest => x <= 0f && z <= 0f,
                _ => false,
            };
            if (inside)
            {
                result.Add(quadrant);
            }
        }

        return result;
    }

    private static List<SurfaceRectangle> BuildSurfaces(
        HybridWallTopologyKey key,
        HybridWallQuadrant ordinaryQuadrants,
        HybridWallRayMask doubledRays)
    {
        var result = new List<SurfaceRectangle>();
        AddQuadrant(HybridWallQuadrant.NorthEast, 0f, 0f, 0.5f, 0.5f);
        AddQuadrant(HybridWallQuadrant.NorthWest, -0.5f, 0f, 0f, 0.5f);
        AddQuadrant(HybridWallQuadrant.SouthEast, 0f, -0.5f, 0.5f, 0f);
        AddQuadrant(HybridWallQuadrant.SouthWest, -0.5f, -0.5f, 0f, 0f);
        Add(HybridWallDirection.North, key.North);
        Add(HybridWallDirection.East, key.East);
        Add(HybridWallDirection.South, key.South);
        Add(HybridWallDirection.West, key.West);
        return result;

        void AddQuadrant(HybridWallQuadrant quadrant, float minX, float minZ, float maxX, float maxZ)
        {
            if (ordinaryQuadrants.HasFlag(quadrant))
            {
                result.Add(new SurfaceRectangle(minX, minZ, maxX, maxZ));
            }
        }

        void Add(HybridWallDirection direction, HybridWallArmKind kind)
        {
            float width = WidthFor(kind, doubledRays.HasFlag(Mask(direction)));
            if (width <= 0f)
            {
                return;
            }

            float half = width * 0.5f;
            result.Add(direction switch
            {
                HybridWallDirection.North => new SurfaceRectangle(-half, 0f, half, 0.5f),
                HybridWallDirection.East => new SurfaceRectangle(0f, -half, 0.5f, half),
                HybridWallDirection.South => new SurfaceRectangle(-half, -0.5f, half, 0f),
                HybridWallDirection.West => new SurfaceRectangle(-0.5f, -half, 0f, half),
                _ => throw new ArgumentOutOfRangeException(nameof(direction), direction, null),
            });
        }
    }

    private static IReadOnlyList<HybridWallLine> ExteriorContour(
        IReadOnlyList<HybridWallSurfaceCell> cells)
    {
        var counts = new Dictionary<HybridWallLine, int>();
        foreach (HybridWallLine edge in cells.SelectMany(cell => cell.Edges))
        {
            counts.TryGetValue(edge, out int count);
            counts[edge] = count + 1;
        }

        HybridWallLine[] segments = counts
            .Where(pair => pair.Value == 1)
            .Select(pair => pair.Key)
            .OrderBy(line => line.Z1)
            .ThenBy(line => line.X1)
            .ThenBy(line => line.Z2)
            .ThenBy(line => line.X2)
            .ToArray();
        return MergeCollinear(segments);
    }

    private static IReadOnlyList<HybridWallLine> MergeCollinear(
        IReadOnlyList<HybridWallLine> segments)
    {
        var result = new List<HybridWallLine>();
        foreach (IGrouping<float, HybridWallLine> row in segments
                     .Where(line => line.Z1.Equals(line.Z2))
                     .GroupBy(line => line.Z1))
        {
            HybridWallLine[] ordered = row.OrderBy(line => line.X1).ToArray();
            float start = ordered[0].X1;
            float end = ordered[0].X2;
            for (int index = 1; index < ordered.Length; index++)
            {
                if (ordered[index].X1.Equals(end))
                {
                    end = ordered[index].X2;
                }
                else
                {
                    result.Add(new HybridWallLine(start, row.Key, end, row.Key));
                    start = ordered[index].X1;
                    end = ordered[index].X2;
                }
            }
            result.Add(new HybridWallLine(start, row.Key, end, row.Key));
        }

        foreach (IGrouping<float, HybridWallLine> column in segments
                     .Where(line => line.X1.Equals(line.X2))
                     .GroupBy(line => line.X1))
        {
            HybridWallLine[] ordered = column.OrderBy(line => line.Z1).ToArray();
            float start = ordered[0].Z1;
            float end = ordered[0].Z2;
            for (int index = 1; index < ordered.Length; index++)
            {
                if (ordered[index].Z1.Equals(end))
                {
                    end = ordered[index].Z2;
                }
                else
                {
                    result.Add(new HybridWallLine(column.Key, start, column.Key, end));
                    start = ordered[index].Z1;
                    end = ordered[index].Z2;
                }
            }
            result.Add(new HybridWallLine(column.Key, start, column.Key, end));
        }

        return result
            .OrderBy(line => line.Z1)
            .ThenBy(line => line.X1)
            .ThenBy(line => line.Z2)
            .ThenBy(line => line.X2)
            .ToArray();
    }

    private static bool HasOverlap(IReadOnlyList<HybridWallSurfaceCell> cells)
    {
        for (int i = 0; i < cells.Count; i++)
        {
            for (int j = i + 1; j < cells.Count; j++)
            {
                if (Math.Min(cells[i].MaxX, cells[j].MaxX) > Math.Max(cells[i].MinX, cells[j].MinX) &&
                    Math.Min(cells[i].MaxZ, cells[j].MaxZ) > Math.Max(cells[i].MinZ, cells[j].MinZ))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsConnected(IReadOnlyList<HybridWallSurfaceCell> cells)
    {
        if (cells.Count <= 1)
        {
            return true;
        }

        var visited = new HashSet<int> { 0 };
        var pending = new List<int> { 0 };
        int pendingIndex = 0;
        while (pendingIndex < pending.Count)
        {
            int current = pending[pendingIndex++];
            for (int candidate = 0; candidate < cells.Count; candidate++)
            {
                if (!visited.Contains(candidate) && SharesEdge(cells[current], cells[candidate]))
                {
                    visited.Add(candidate);
                    pending.Add(candidate);
                }
            }
        }

        return visited.Count == cells.Count;
    }

    private static bool SharesEdge(HybridWallSurfaceCell left, HybridWallSurfaceCell right)
    {
        bool vertical =
            (left.MaxX.Equals(right.MinX) || right.MaxX.Equals(left.MinX)) &&
            Math.Min(left.MaxZ, right.MaxZ) > Math.Max(left.MinZ, right.MinZ);
        bool horizontal =
            (left.MaxZ.Equals(right.MinZ) || right.MaxZ.Equals(left.MinZ)) &&
            Math.Min(left.MaxX, right.MaxX) > Math.Max(left.MinX, right.MinX);
        return vertical || horizontal;
    }

    private readonly struct SurfaceRectangle
    {
        public SurfaceRectangle(float minX, float minZ, float maxX, float maxZ)
        {
            MinX = minX;
            MinZ = minZ;
            MaxX = maxX;
            MaxZ = maxZ;
        }

        public float MinX { get; }

        public float MinZ { get; }

        public float MaxX { get; }

        public float MaxZ { get; }

        public bool Contains(float x, float z) => x >= MinX && x <= MaxX && z >= MinZ && z <= MaxZ;
    }

    private static readonly HybridWallDirection[] StableDirections =
    {
        HybridWallDirection.North,
        HybridWallDirection.East,
        HybridWallDirection.South,
        HybridWallDirection.West,
    };

    private static readonly HybridWallQuadrant[] StableQuadrants =
    {
        HybridWallQuadrant.NorthEast,
        HybridWallQuadrant.NorthWest,
        HybridWallQuadrant.SouthEast,
        HybridWallQuadrant.SouthWest,
    };

    internal static HybridWallRayMask Mask(HybridWallDirection direction) => direction switch
    {
        HybridWallDirection.North => HybridWallRayMask.North,
        HybridWallDirection.East => HybridWallRayMask.East,
        HybridWallDirection.South => HybridWallRayMask.South,
        HybridWallDirection.West => HybridWallRayMask.West,
        _ => HybridWallRayMask.None,
    };
}
