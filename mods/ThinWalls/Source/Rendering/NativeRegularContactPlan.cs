using System;
using System.Collections.Generic;

namespace ThinWalls.Rendering;

public readonly struct NativeRegularGridPoint : IEquatable<NativeRegularGridPoint>
{
    public NativeRegularGridPoint(int x, int z)
    {
        X = x;
        Z = z;
    }

    public int X { get; }
    public int Z { get; }

    public bool Equals(NativeRegularGridPoint other) => X == other.X && Z == other.Z;
    public override bool Equals(object? obj) => obj is NativeRegularGridPoint other && Equals(other);
    public override int GetHashCode() => unchecked((X * 397) ^ Z);
}

public readonly struct NativeRegularContactCandidate
{
    public NativeRegularContactCandidate(int vertexX, int vertexZ, NativeRegularContactRule rule)
    {
        VertexX = vertexX;
        VertexZ = vertexZ;
        Rule = rule;
    }

    public int VertexX { get; }
    public int VertexZ { get; }
    public NativeRegularContactRule Rule { get; }

    public bool ContainsReceiver(NativeRegularGridPoint receiver) =>
        receiver.Equals(new NativeRegularGridPoint(
            VertexX + Rule.FirstReceiverX,
            VertexZ + Rule.FirstReceiverZ)) ||
        receiver.Equals(new NativeRegularGridPoint(
            VertexX + Rule.SecondReceiverX,
            VertexZ + Rule.SecondReceiverZ));
}

public readonly struct NativeRegularContactRule
{
    public NativeRegularContactRule(
        HybridWallDirection stemDirection,
        int firstReceiverX,
        int firstReceiverZ,
        int secondReceiverX,
        int secondReceiverZ)
    {
        StemDirection = stemDirection;
        FirstReceiverX = firstReceiverX;
        FirstReceiverZ = firstReceiverZ;
        SecondReceiverX = secondReceiverX;
        SecondReceiverZ = secondReceiverZ;
    }

    public HybridWallDirection StemDirection { get; }
    public int FirstReceiverX { get; }
    public int FirstReceiverZ { get; }
    public int SecondReceiverX { get; }
    public int SecondReceiverZ { get; }
}

/// <summary>
/// Source-bound mixed-width side-T geometry. The centered diagonal ownership is
/// copied from the measured Thin T plan, while the receiving axis is remapped
/// onto the native Core wall's full surface bands. This is the accepted mixed-T
/// silhouette; lone regular-wall contacts never enter this plan.
/// </summary>
public static class NativeRegularContactPlan
{
    public const float CoreLinkedTopVertexAltitudeBias = 0.01f;
    private const float Body = 17f / 60f;
    private const HybridWallRayMask Horizontal = HybridWallRayMask.East | HybridWallRayMask.West;
    private const HybridWallRayMask Vertical = HybridWallRayMask.North | HybridWallRayMask.South;

    public static IReadOnlyList<NativeWallMeshQuad> EastStem(bool southReceiver) =>
        VerticalReceiver(HybridWallRayMask.East, southReceiver);

    public static IReadOnlyList<NativeWallMeshQuad> WestStem(bool southReceiver) =>
        VerticalReceiver(HybridWallRayMask.West, southReceiver);

    public static IReadOnlyList<NativeWallMeshQuad> NorthStem(bool westReceiver) =>
        HorizontalReceiver(HybridWallRayMask.North, westReceiver);

    public static IReadOnlyList<NativeWallMeshQuad> SouthStem(bool westReceiver) =>
        HorizontalReceiver(HybridWallRayMask.South, westReceiver);

    public static bool UsesThinSource(NativeWallMeshQuad quad, HybridWallRayMask stem)
    {
        if (stem is not (HybridWallRayMask.North or HybridWallRayMask.East or
            HybridWallRayMask.South or HybridWallRayMask.West))
            throw new ArgumentOutOfRangeException(nameof(stem));
        HybridWallRayMask thinOrientation = stem is HybridWallRayMask.East or HybridWallRayMask.West
            ? Horizontal
            : Vertical;
        return (quad.SourceLinks & thinOrientation) != HybridWallRayMask.None &&
               quad.SourceLinks != (stem is HybridWallRayMask.East or HybridWallRayMask.West
                   ? Vertical
                   : Horizontal);
    }

    public static bool TryResolve(
        HybridWallRayMask thinRays,
        HybridWallQuadrant ordinaryQuadrants,
        HybridWallRayMask doorRays,
        HybridWallRayMask doubledRays,
        out NativeRegularContactRule rule)
    {
        rule = default;
        if (doorRays != HybridWallRayMask.None || doubledRays != HybridWallRayMask.None)
            return false;

        if (thinRays == HybridWallRayMask.South &&
            ordinaryQuadrants == (HybridWallQuadrant.NorthEast | HybridWallQuadrant.NorthWest))
            rule = new NativeRegularContactRule(HybridWallDirection.South, -1, 0, 0, 0);
        else if (thinRays == HybridWallRayMask.North &&
                 ordinaryQuadrants == (HybridWallQuadrant.SouthEast | HybridWallQuadrant.SouthWest))
            rule = new NativeRegularContactRule(HybridWallDirection.North, -1, -1, 0, -1);
        else if (thinRays == HybridWallRayMask.East &&
                 ordinaryQuadrants == (HybridWallQuadrant.NorthWest | HybridWallQuadrant.SouthWest))
            rule = new NativeRegularContactRule(HybridWallDirection.East, -1, -1, -1, 0);
        else if (thinRays == HybridWallRayMask.West &&
                 ordinaryQuadrants == (HybridWallQuadrant.NorthEast | HybridWallQuadrant.SouthEast))
            rule = new NativeRegularContactRule(HybridWallDirection.West, 0, -1, 0, 0);
        else
            return false;

        return true;
    }

    public static IReadOnlyList<NativeRegularGridPoint> ReceiverVertices(NativeRegularContactRule rule)
    {
        var vertices = new HashSet<NativeRegularGridPoint>();
        AddCell(rule.FirstReceiverX, rule.FirstReceiverZ);
        AddCell(rule.SecondReceiverX, rule.SecondReceiverZ);
        return new List<NativeRegularGridPoint>(vertices);

        void AddCell(int x, int z)
        {
            vertices.Add(new NativeRegularGridPoint(x, z));
            vertices.Add(new NativeRegularGridPoint(x + 1, z));
            vertices.Add(new NativeRegularGridPoint(x, z + 1));
            vertices.Add(new NativeRegularGridPoint(x + 1, z + 1));
        }
    }

    public static bool CanReplaceAtomically(
        NativeRegularContactRule rule,
        IReadOnlyList<NativeRegularContactCandidate> candidates)
    {
        var first = new NativeRegularGridPoint(rule.FirstReceiverX, rule.FirstReceiverZ);
        var second = new NativeRegularGridPoint(rule.SecondReceiverX, rule.SecondReceiverZ);
        int firstCount = 0;
        int secondCount = 0;
        foreach (NativeRegularContactCandidate candidate in candidates)
        {
            if (candidate.ContainsReceiver(first)) firstCount++;
            if (candidate.ContainsReceiver(second)) secondCount++;
        }
        return firstCount == 1 && secondCount == 1;
    }

    public static NativeWallMeshQuad WithNativeReceiverAltitude(
        NativeWallMeshQuad source,
        float receiverCellMinY) => source.WithAltitude(
        -CoreLinkedTopVertexAltitudeBias * receiverCellMinY,
        0f,
        CoreLinkedTopVertexAltitudeBias);

    private static IReadOnlyList<NativeWallMeshQuad> HorizontalReceiver(HybridWallRayMask stem, bool westReceiver)
    {
        bool receiverNorth = stem == HybridWallRayMask.South;
        float cellMinX = westReceiver ? -1f : 0f;
        float cellMinY = receiverNorth ? 0f : -1f;
        var result = new List<NativeWallMeshQuad>(36);
        IReadOnlyList<NativeWallMeshQuad> topology = NativeWallMeshPlan.Topology(Horizontal | stem);
        float apertureStart;
        float apertureEnd;
        if (stem == HybridWallRayMask.South)
        {
            AddPatch(quad => quad.Destination.YMax <= 8f / 60f + 0.00001f,
                destination => new HybridWallUvRect(destination.X, destination.Y + Body,
                    destination.Width, destination.Height));
            AddCentralStrip(25f / 60f, 28f / 60f);
            apertureStart = 25f / 60f;
            apertureEnd = 28f / 60f;
            AddRegularCentral(apertureEnd, cellMinY + 1f);
        }
        else
        {
            AddPatch(quad => quad.Destination.Y >= 15f / 60f - 0.00001f,
                destination => new HybridWallUvRect(destination.X,
                    destination.Y + 43f / 60f + cellMinY,
                    destination.Width, destination.Height));
            AddCentralStrip(cellMinY + 55f / 60f, cellMinY + 58f / 60f);
            apertureStart = cellMinY + 55f / 60f;
            apertureEnd = cellMinY + 58f / 60f;
            AddRegularCentral(cellMinY, apertureStart);
        }
        AddRegularStripSides(apertureStart, apertureEnd);
        foreach (NativeWallMeshQuad quad in NativeWallMeshPlan.HalfRay(stem))
        {
            foreach (NativeWallMeshQuad altitudeBand in StemAltitudeBands(quad, stem))
            {
                NativeWallMeshQuad? clipped = ClipReceiverHalf(
                    altitudeBand, splitX: true, keepNegative: westReceiver);
                if (clipped.HasValue) result.Add(clipped.Value);
            }
        }

        float outerMinX = westReceiver ? -1f : Body;
        float outerMaxX = westReceiver ? -Body : 1f;
        AddRegular(outerMinX, cellMinY, outerMaxX, cellMinY + 1f);
        return result;

        void AddPatch(Func<NativeWallMeshQuad, bool> include, Func<HybridWallUvRect, HybridWallUvRect> transform)
        {
            foreach (NativeWallMeshQuad quad in topology)
            {
                if (!InsideMeasuredBody(quad) || !include(quad)) continue;
                AddClipped(RemapDestination(quad, transform(quad.Destination)));
            }
        }

        void AddCentralStrip(float minY, float maxY)
        {
            foreach (NativeWallMeshQuad quad in topology)
            {
                if (quad.SourceLinks != (Horizontal | stem) ||
                    !Nearly(quad.Destination.X, -3f / 60f) ||
                    !Nearly(quad.Destination.XMax, 4f / 60f) ||
                    !Nearly(quad.Destination.Y, 8f / 60f) ||
                    !Nearly(quad.Destination.YMax, 15f / 60f)) continue;
                AddClipped(RemapDestination(quad, new HybridWallUvRect(
                    quad.Destination.X, minY, quad.Destination.Width, maxY - minY)));
            }
        }

        void AddClipped(NativeWallMeshQuad quad)
        {
            NativeWallMeshQuad? clipped = ClipReceiverHalf(quad, splitX: true, keepNegative: westReceiver);
            if (!clipped.HasValue) return;
            result.Add(UsesThinSource(clipped.Value, stem)
                ? clipped.Value
                : RephaseRegularLongitudinal(clipped.Value, horizontal: true, cellMinX));
        }

        void AddRegularCentral(float minY, float maxY)
        {
            float minX = westReceiver ? -Body : 0f;
            float maxX = westReceiver ? 0f : Body;
            AddRegular(minX, minY, maxX, maxY);
        }

        void AddRegularStripSides(float minY, float maxY)
        {
            float halfMin = westReceiver ? -Body : 0f;
            float halfMax = westReceiver ? 0f : Body;
            const float stripMin = -3f / 60f;
            const float stripMax = 4f / 60f;
            if (halfMin < stripMin) AddRegular(halfMin, minY, Math.Min(halfMax, stripMin), maxY);
            if (halfMax > stripMax) AddRegular(Math.Max(halfMin, stripMax), minY, halfMax, maxY);
        }

        void AddRegular(float minX, float minY, float maxX, float maxY)
        {
            result.Add(new NativeWallMeshQuad(
                new HybridWallUvRect(minX, minY, maxX - minX, maxY - minY),
                new HybridWallUvRect(minX - cellMinX, minY - cellMinY,
                    maxX - minX, maxY - minY), Horizontal));
        }
    }

    private static IReadOnlyList<NativeWallMeshQuad> VerticalReceiver(HybridWallRayMask stem, bool southReceiver)
    {
        bool receiverWest = stem == HybridWallRayMask.East;
        float cellMinX = receiverWest ? -1f : 0f;
        float cellMinY = southReceiver ? -1f : 0f;
        var result = new List<NativeWallMeshQuad>(36);
        IReadOnlyList<NativeWallMeshQuad> topology = NativeWallMeshPlan.Topology(Vertical | stem);
        float stripMinX;
        float stripMaxX;
        if (stem == HybridWallRayMask.East)
        {
            AddPatch(quad => quad.Destination.X >= 4f / 60f - 0.00001f,
                destination => new HybridWallUvRect(destination.X - Body,
                    destination.Y, destination.Width, destination.Height));
            stripMinX = cellMinX + 44f / 60f;
            stripMaxX = cellMinX + 47f / 60f;
            AddCentralStrip(stripMinX, stripMaxX);
            AddRegularCentral(cellMinX, stripMinX);
        }
        else
        {
            AddPatch(quad => quad.Destination.XMax <= -3f / 60f + 0.00001f,
                destination => new HybridWallUvRect(destination.X + Body,
                    destination.Y, destination.Width, destination.Height));
            stripMinX = cellMinX + 14f / 60f;
            stripMaxX = cellMinX + 17f / 60f;
            AddCentralStrip(stripMinX, stripMaxX);
            AddRegularCentral(stripMaxX, cellMinX + 1f);
        }
        AddRegularStripEnds(stripMinX, stripMaxX);
        foreach (NativeWallMeshQuad quad in NativeWallMeshPlan.HalfRay(stem))
        {
            foreach (NativeWallMeshQuad altitudeBand in StemAltitudeBands(quad, stem))
            {
                NativeWallMeshQuad? clipped = ClipReceiverHalf(
                    altitudeBand, splitX: false, keepNegative: southReceiver);
                if (clipped.HasValue) result.Add(clipped.Value);
            }
        }

        float outerMinY = southReceiver ? -1f : Body;
        float outerMaxY = southReceiver ? -Body : 1f;
        AddRegular(cellMinX, outerMinY, cellMinX + 1f, outerMaxY);
        return result;

        void AddPatch(Func<NativeWallMeshQuad, bool> include, Func<HybridWallUvRect, HybridWallUvRect> transform)
        {
            foreach (NativeWallMeshQuad quad in topology)
            {
                if (!InsideMeasuredBody(quad) || !include(quad)) continue;
                AddClipped(RemapDestination(quad, transform(quad.Destination)));
            }
        }

        void AddCentralStrip(float minX, float maxX)
        {
            foreach (NativeWallMeshQuad quad in topology)
            {
                if (quad.SourceLinks != (Vertical | stem) ||
                    !Nearly(quad.Destination.X, -3f / 60f) ||
                    !Nearly(quad.Destination.XMax, 4f / 60f) ||
                    !Nearly(quad.Destination.Y, 8f / 60f) ||
                    !Nearly(quad.Destination.YMax, 15f / 60f)) continue;
                AddClipped(RemapDestination(quad, new HybridWallUvRect(
                    minX, quad.Destination.Y, maxX - minX, quad.Destination.Height)));
            }
        }

        void AddClipped(NativeWallMeshQuad quad)
        {
            NativeWallMeshQuad? clipped = ClipReceiverHalf(quad, splitX: false, keepNegative: southReceiver);
            if (!clipped.HasValue) return;
            result.Add(UsesThinSource(clipped.Value, stem)
                ? clipped.Value
                : RephaseRegularLongitudinal(clipped.Value, horizontal: false, cellMinY));
        }

        void AddRegularCentral(float minX, float maxX)
        {
            float minY = southReceiver ? -Body : 0f;
            float maxY = southReceiver ? 0f : Body;
            AddRegular(minX, minY, maxX, maxY);
        }

        void AddRegularStripEnds(float minX, float maxX)
        {
            float halfMin = southReceiver ? -Body : 0f;
            float halfMax = southReceiver ? 0f : Body;
            const float stripMin = 8f / 60f;
            const float stripMax = 15f / 60f;
            if (halfMin < stripMin) AddRegular(minX, halfMin, maxX, Math.Min(halfMax, stripMin));
            if (halfMax > stripMax) AddRegular(minX, Math.Max(halfMin, stripMax), maxX, halfMax);
        }

        void AddRegular(float minX, float minY, float maxX, float maxY)
        {
            result.Add(new NativeWallMeshQuad(
                new HybridWallUvRect(minX, minY, maxX - minX, maxY - minY),
                new HybridWallUvRect(minX - cellMinX, minY - cellMinY,
                    maxX - minX, maxY - minY), Vertical));
        }
    }

    private static NativeWallMeshQuad RemapDestination(NativeWallMeshQuad source, HybridWallUvRect destination)
    {
        NativeWallMeshVertex[] vertices = Vertices(source)
            .ConvertAll(vertex => new NativeWallMeshVertex(
                destination.X + (vertex.X - source.Destination.X) / source.Destination.Width * destination.Width,
                destination.Y + (vertex.Y - source.Destination.Y) / source.Destination.Height * destination.Height,
                vertex.U,
                vertex.V))
            .ToArray();
        return new NativeWallMeshQuad(destination, source.Source, source.SourceLinks, source.Triangle)
            .WithAltitude(source.AltitudeOffset, source.AltitudeSlopeX, source.AltitudeSlopeY)
            .WithVertices(vertices);
    }

    private static NativeWallMeshQuad? ClipReceiverHalf(NativeWallMeshQuad source, bool splitX, bool keepNegative)
    {
        List<NativeWallMeshVertex> input = Vertices(source);
        var output = new List<NativeWallMeshVertex>(input.Count + 2);
        for (int i = 0; i < input.Count; i++)
        {
            NativeWallMeshVertex current = input[i];
            NativeWallMeshVertex next = input[(i + 1) % input.Count];
            bool currentInside = Inside(current);
            bool nextInside = Inside(next);
            if (currentInside) output.Add(current);
            if (currentInside == nextInside) continue;
            float currentAxis = splitX ? current.X : current.Y;
            float nextAxis = splitX ? next.X : next.Y;
            float t = -currentAxis / (nextAxis - currentAxis);
            output.Add(new NativeWallMeshVertex(
                current.X + (next.X - current.X) * t,
                current.Y + (next.Y - current.Y) * t,
                current.U + (next.U - current.U) * t,
                current.V + (next.V - current.V) * t));
        }
        if (output.Count < 3) return null;
        return source.WithVertices(output);

        bool Inside(NativeWallMeshVertex vertex)
        {
            float value = splitX ? vertex.X : vertex.Y;
            return keepNegative ? value <= 0.00001f : value >= -0.00001f;
        }
    }

    private static NativeWallMeshQuad RephaseRegularLongitudinal(
        NativeWallMeshQuad source, bool horizontal, float cellMin)
    {
        List<NativeWallMeshVertex> vertices = Vertices(source);
        return source.WithVertices(vertices.ConvertAll(vertex => new NativeWallMeshVertex(
            vertex.X,
            vertex.Y,
            horizontal ? vertex.X - cellMin : vertex.U,
            horizontal ? vertex.V : vertex.Y - cellMin)));
    }

    private static IEnumerable<NativeWallMeshQuad> StemAltitudeBands(
        NativeWallMeshQuad source,
        HybridWallRayMask stem)
    {
        HybridWallUvRect d = source.Destination;
        NativeWallMeshQuad? shoulder = stem switch
        {
            HybridWallRayMask.North => NativeWallMeshGeometry.ClipToRect(source, d.X, 0f, d.XMax, Body),
            HybridWallRayMask.East => NativeWallMeshGeometry.ClipToRect(source, 0f, d.Y, Body, d.YMax),
            HybridWallRayMask.South => NativeWallMeshGeometry.ClipToRect(source, d.X, -Body, d.XMax, 0f),
            HybridWallRayMask.West => NativeWallMeshGeometry.ClipToRect(source, -Body, d.Y, 0f, d.YMax),
            _ => throw new ArgumentOutOfRangeException(nameof(stem)),
        };
        NativeWallMeshQuad? exterior = stem switch
        {
            HybridWallRayMask.North => NativeWallMeshGeometry.ClipToRect(source, d.X, Body, d.XMax, 0.5f),
            HybridWallRayMask.East => NativeWallMeshGeometry.ClipToRect(source, Body, d.Y, 0.5f, d.YMax),
            HybridWallRayMask.South => NativeWallMeshGeometry.ClipToRect(source, d.X, -0.5f, d.XMax, -Body),
            HybridWallRayMask.West => NativeWallMeshGeometry.ClipToRect(source, -0.5f, d.Y, -Body, d.YMax),
            _ => throw new ArgumentOutOfRangeException(nameof(stem)),
        };

        float slope = NativeWallMeshPlan.CompletedAltitudeOffset / Body;
        if (shoulder.HasValue)
        {
            yield return stem switch
            {
                HybridWallRayMask.North => shoulder.Value.WithAltitude(0f, 0f, slope),
                HybridWallRayMask.East => shoulder.Value.WithAltitude(0f, slope, 0f),
                HybridWallRayMask.South => shoulder.Value.WithAltitude(0f, 0f, -slope),
                _ => shoulder.Value.WithAltitude(0f, -slope, 0f),
            };
        }
        if (exterior.HasValue)
            yield return exterior.Value.WithAltitude(NativeWallMeshPlan.CompletedAltitudeOffset, 0f, 0f);
    }

    private static List<NativeWallMeshVertex> Vertices(NativeWallMeshQuad quad)
    {
        if (quad.Vertices != null) return new List<NativeWallMeshVertex>(quad.Vertices);
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
            1 => new List<NativeWallMeshVertex> { corners[0], corners[1], corners[2] },
            2 => new List<NativeWallMeshVertex> { corners[0], corners[2], corners[3] },
            3 => new List<NativeWallMeshVertex> { corners[0], corners[1], corners[3] },
            4 => new List<NativeWallMeshVertex> { corners[1], corners[2], corners[3] },
            _ => new List<NativeWallMeshVertex>(corners),
        };
    }

    private static bool InsideMeasuredBody(NativeWallMeshQuad quad) =>
        quad.Destination.X >= -Body - 0.00001f &&
        quad.Destination.XMax <= Body + 0.00001f &&
        quad.Destination.Y >= -Body - 0.00001f &&
        quad.Destination.YMax <= Body + 0.00001f;

    private static bool Nearly(float left, float right) => Math.Abs(left - right) < 0.00001f;
}
