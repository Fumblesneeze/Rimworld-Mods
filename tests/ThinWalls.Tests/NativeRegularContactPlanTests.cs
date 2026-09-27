using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ThinWalls.Rendering;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class NativeRegularContactPlanTests
{
    private const float Epsilon = 0.00001f;
    private const float Body = 17f / 60f;

    [TestCase(HybridWallRayMask.North, -1f, 1f, -1f, 0.5f)]
    [TestCase(HybridWallRayMask.East, -1f, 0.5f, -1f, 1f)]
    [TestCase(HybridWallRayMask.South, -1f, 1f, -0.5f, 1f)]
    [TestCase(HybridWallRayMask.West, -0.5f, 1f, -1f, 1f)]
    public void AdmittedTwoReceiverContactRestoresTheCenteredDiagonalShoulder(
        HybridWallRayMask stem,
        float expectedMinX,
        float expectedMaxX,
        float expectedMinY,
        float expectedMaxY)
    {
        NativeWallMeshQuad[] actual = ContactHalf(stem, true)
            .Concat(ContactHalf(stem, false))
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(actual.Any(quad => quad.Vertices is { Count: >= 3 } && HasDiagonalEdge(quad.Vertices)),
                Is.True, "The accepted mixed T needs the centered diagonal shoulder visible in the regression reference.");
            Assert.That(actual.Any(quad => NativeRegularContactPlan.UsesThinSource(quad, stem)), Is.True);
            Assert.That(actual.Any(quad => !NativeRegularContactPlan.UsesThinSource(quad, stem)), Is.True);
            NativeWallMeshQuad[] thin = actual
                .Where(quad => NativeRegularContactPlan.UsesThinSource(quad, stem))
                .ToArray();
            float[] thinAltitudes = thin
                .SelectMany(quad => NativeWallMeshGeometry.Vertices(quad)
                    .Select(vertex => quad.AltitudeAt(vertex.X, vertex.Y)))
                .ToArray();
            Assert.That(thin.Any(quad => Math.Abs(quad.AltitudeSlopeX) > Epsilon ||
                                         Math.Abs(quad.AltitudeSlopeY) > Epsilon), Is.True,
                "The source-derived shoulder must rise only through the bounded mixed junction.");
            Assert.That(thinAltitudes.Min(), Is.EqualTo(0f).Within(Epsilon),
                "The shoulder must meet the regular receiver at native altitude.");
            Assert.That(thinAltitudes.Max(), Is.EqualTo(NativeWallMeshPlan.CompletedAltitudeOffset).Within(Epsilon),
                "The Thin half-edge must reach the completed Thin altitude outside the aperture.");
            Assert.That(actual.Min(quad => quad.Destination.X), Is.EqualTo(expectedMinX).Within(Epsilon));
            Assert.That(actual.Max(quad => quad.Destination.XMax), Is.EqualTo(expectedMaxX).Within(Epsilon));
            Assert.That(actual.Min(quad => quad.Destination.Y), Is.EqualTo(expectedMinY).Within(Epsilon));
            Assert.That(actual.Max(quad => quad.Destination.YMax), Is.EqualTo(expectedMaxY).Within(Epsilon));
        });
    }

    [TestCase(HybridWallRayMask.North)]
    [TestCase(HybridWallRayMask.East)]
    [TestCase(HybridWallRayMask.South)]
    [TestCase(HybridWallRayMask.West)]
    public void MixedContactCoversTheTwoReceiversAndStemExactlyOnce(HybridWallRayMask stem)
    {
        NativeWallMeshQuad[] plan = ContactHalf(stem, true)
            .Concat(ContactHalf(stem, false))
            .ToArray();

        const int samplesPerCell = 60;
        for (int sx = -60; sx < 60; sx++)
        for (int sy = -60; sy < 60; sy++)
        {
            // Irrational-looking subpixel offsets avoid sampling exactly on the
            // deliberately shared diagonal/band edges, where either owner is valid.
            float x = (sx + 0.371f) / samplesPerCell;
            float y = (sy + 0.613f) / samplesPerCell;
            bool expected = IsExpectedUnion(stem, x, y);
            int coverage = plan.Count(quad => Contains(NativeWallMeshGeometry.Vertices(quad), x, y));
            Assert.That(coverage, Is.EqualTo(expected ? 1 : 0),
                $"{stem} coverage at ({x:R},{y:R}) must be exactly one inside the union and zero outside.");
        }
    }

    [TestCase(HybridWallRayMask.North)]
    [TestCase(HybridWallRayMask.East)]
    [TestCase(HybridWallRayMask.South)]
    [TestCase(HybridWallRayMask.West)]
    public void MixedContactAltitudeTransitionIsConfinedToTheBodyDepth(HybridWallRayMask stem)
    {
        NativeWallMeshQuad[] thin = ContactHalf(stem, true)
            .Concat(ContactHalf(stem, false))
            .Where(quad => NativeRegularContactPlan.UsesThinSource(quad, stem))
            .ToArray();

        foreach (NativeWallMeshQuad quad in thin)
        {
            IReadOnlyList<NativeWallMeshVertex> vertices = NativeWallMeshGeometry.Vertices(quad);
            float maxDistance = vertices.Max(vertex => LongitudinalDistance(stem, vertex));
            if (maxDistance > Body + Epsilon)
            {
                Assert.Multiple(() =>
                {
                    Assert.That(quad.AltitudeSlopeX, Is.Zero.Within(Epsilon));
                    Assert.That(quad.AltitudeSlopeY, Is.Zero.Within(Epsilon));
                    Assert.That(vertices.All(vertex =>
                            Math.Abs(quad.AltitudeAt(vertex.X, vertex.Y) -
                                     NativeWallMeshPlan.CompletedAltitudeOffset) <= Epsilon), Is.True,
                        "The exterior Thin half-edge must be flat at completed altitude.");
                });
            }
        }
    }

    [TestCase(HybridWallRayMask.North,
        HybridWallQuadrant.SouthEast | HybridWallQuadrant.SouthWest)]
    [TestCase(HybridWallRayMask.East,
        HybridWallQuadrant.NorthWest | HybridWallQuadrant.SouthWest)]
    [TestCase(HybridWallRayMask.South,
        HybridWallQuadrant.NorthEast | HybridWallQuadrant.NorthWest)]
    [TestCase(HybridWallRayMask.West,
        HybridWallQuadrant.NorthEast | HybridWallQuadrant.SouthEast)]
    public void AtomicReplacementInspectsSixUniqueVerticesAndRejectsEitherCompetingReceiver(
        HybridWallRayMask stem,
        HybridWallQuadrant quadrants)
    {
        Assert.That(NativeRegularContactPlan.TryResolve(
            stem, quadrants, HybridWallRayMask.None, HybridWallRayMask.None,
            out NativeRegularContactRule rule), Is.True);
        NativeRegularGridPoint[] vertices = NativeRegularContactPlan.ReceiverVertices(rule).ToArray();
        var root = new NativeRegularContactCandidate(0, 0, rule);
        var firstCompetitor = new NativeRegularContactCandidate(
            rule.FirstReceiverX,
            rule.FirstReceiverZ,
            new NativeRegularContactRule(Direction(stem), 0, 0, 9, 9));
        var secondCompetitor = new NativeRegularContactCandidate(
            rule.SecondReceiverX,
            rule.SecondReceiverZ,
            new NativeRegularContactRule(Direction(stem), 0, 0, 9, 9));

        Assert.Multiple(() =>
        {
            Assert.That(vertices.Length, Is.EqualTo(6));
            Assert.That(vertices.Distinct().Count(), Is.EqualTo(6));
            Assert.That(NativeRegularContactPlan.CanReplaceAtomically(rule, Array.Empty<NativeRegularContactCandidate>()),
                Is.False, "Missing candidate evidence must retain native receivers and the ordinary Thin terminal.");
            Assert.That(NativeRegularContactPlan.CanReplaceAtomically(rule, new[] { root }), Is.True);
            Assert.That(NativeRegularContactPlan.CanReplaceAtomically(rule, new[] { root, root }), Is.False,
                "A duplicated/ambiguous candidate must not suppress Thin printing.");
            Assert.That(NativeRegularContactPlan.CanReplaceAtomically(rule, new[] { root, firstCompetitor }), Is.False);
            Assert.That(NativeRegularContactPlan.CanReplaceAtomically(rule, new[] { root, secondCompetitor }), Is.False);
        });
    }

    [TestCase(0f)]
    [TestCase(-1f)]
    public void ReceiverFragmentsRetainTheNativeCoreTopVertexAltitudeBias(float receiverCellMinY)
    {
        var source = new NativeWallMeshQuad(
            new HybridWallUvRect(-0.4f, receiverCellMinY + 0.2f, 0.8f, 0.6f),
            new HybridWallUvRect(0f, 0.2f, 1f, 0.6f),
            HybridWallRayMask.East | HybridWallRayMask.West);

        NativeWallMeshQuad actual = NativeRegularContactPlan.WithNativeReceiverAltitude(
            source,
            receiverCellMinY);

        Assert.Multiple(() =>
        {
            Assert.That(actual.AltitudeAt(0f, receiverCellMinY), Is.EqualTo(0f).Within(Epsilon));
            Assert.That(actual.AltitudeAt(0f, receiverCellMinY + 1f),
                Is.EqualTo(NativeRegularContactPlan.CoreLinkedTopVertexAltitudeBias).Within(Epsilon));
            Assert.That(actual.AltitudeAt(0f, receiverCellMinY + 0.2f),
                Is.EqualTo(0.002f).Within(Epsilon));
            Assert.That(actual.AltitudeAt(0f, receiverCellMinY + 0.8f),
                Is.EqualTo(0.008f).Within(Epsilon));
        });
    }

    private static IEnumerable<NativeWallMeshQuad> ContactHalf(HybridWallRayMask stem, bool first) => stem switch
    {
        HybridWallRayMask.North => NativeRegularContactPlan.NorthStem(first),
        HybridWallRayMask.East => NativeRegularContactPlan.EastStem(first),
        HybridWallRayMask.South => NativeRegularContactPlan.SouthStem(first),
        HybridWallRayMask.West => NativeRegularContactPlan.WestStem(first),
        _ => throw new ArgumentOutOfRangeException(nameof(stem)),
    };

    private static bool HasDiagonalEdge(IReadOnlyList<NativeWallMeshVertex> vertices)
    {
        for (int i = 0; i < vertices.Count; i++)
        {
            NativeWallMeshVertex current = vertices[i];
            NativeWallMeshVertex next = vertices[(i + 1) % vertices.Count];
            if (Math.Abs(current.X - next.X) > Epsilon && Math.Abs(current.Y - next.Y) > Epsilon)
                return true;
        }
        return false;
    }

    private static bool IsExpectedUnion(HybridWallRayMask stem, float x, float y)
    {
        bool receiver = stem switch
        {
            HybridWallRayMask.North => x >= -1f && x <= 1f && y >= -1f && y <= 0f,
            HybridWallRayMask.East => x >= -1f && x <= 0f && y >= -1f && y <= 1f,
            HybridWallRayMask.South => x >= -1f && x <= 1f && y >= 0f && y <= 1f,
            HybridWallRayMask.West => x >= 0f && x <= 1f && y >= -1f && y <= 1f,
            _ => false,
        };
        bool thin = stem switch
        {
            HybridWallRayMask.North => x >= -Body && x <= Body && y >= 0f && y <= 0.5f,
            HybridWallRayMask.East => x >= 0f && x <= 0.5f && y >= -Body && y <= Body,
            HybridWallRayMask.South => x >= -Body && x <= Body && y >= -0.5f && y <= 0f,
            HybridWallRayMask.West => x >= -0.5f && x <= 0f && y >= -Body && y <= Body,
            _ => false,
        };
        return receiver || thin;
    }

    private static bool Contains(IReadOnlyList<NativeWallMeshVertex> vertices, float x, float y)
    {
        bool inside = false;
        for (int i = 0, j = vertices.Count - 1; i < vertices.Count; j = i++)
        {
            NativeWallMeshVertex a = vertices[i];
            NativeWallMeshVertex b = vertices[j];
            if ((a.Y > y) == (b.Y > y)) continue;
            float crossingX = (b.X - a.X) * (y - a.Y) / (b.Y - a.Y) + a.X;
            if (x < crossingX) inside = !inside;
        }
        return inside;
    }

    private static float LongitudinalDistance(HybridWallRayMask stem, NativeWallMeshVertex vertex) => stem switch
    {
        HybridWallRayMask.North => vertex.Y,
        HybridWallRayMask.East => vertex.X,
        HybridWallRayMask.South => -vertex.Y,
        HybridWallRayMask.West => -vertex.X,
        _ => throw new ArgumentOutOfRangeException(nameof(stem)),
    };

    private static HybridWallDirection Direction(HybridWallRayMask stem) => stem switch
    {
        HybridWallRayMask.North => HybridWallDirection.North,
        HybridWallRayMask.East => HybridWallDirection.East,
        HybridWallRayMask.South => HybridWallDirection.South,
        HybridWallRayMask.West => HybridWallDirection.West,
        _ => throw new ArgumentOutOfRangeException(nameof(stem)),
    };

    [TestCase(HybridWallRayMask.South,
        HybridWallQuadrant.NorthEast | HybridWallQuadrant.NorthWest,
        HybridWallDirection.South, -1, 0, 0, 0)]
    [TestCase(HybridWallRayMask.North,
        HybridWallQuadrant.SouthEast | HybridWallQuadrant.SouthWest,
        HybridWallDirection.North, -1, -1, 0, -1)]
    [TestCase(HybridWallRayMask.East,
        HybridWallQuadrant.NorthWest | HybridWallQuadrant.SouthWest,
        HybridWallDirection.East, -1, -1, -1, 0)]
    [TestCase(HybridWallRayMask.West,
        HybridWallQuadrant.NorthEast | HybridWallQuadrant.SouthEast,
        HybridWallDirection.West, 0, -1, 0, 0)]
    public void AdmissionUsesOnlyTheExactCurrentVertexAndTwoReceiverOffsets(
        HybridWallRayMask thinRays,
        HybridWallQuadrant ordinaryQuadrants,
        HybridWallDirection expectedStem,
        int firstX,
        int firstZ,
        int secondX,
        int secondZ)
    {
        bool accepted = NativeRegularContactPlan.TryResolve(
            thinRays,
            ordinaryQuadrants,
            HybridWallRayMask.None,
            HybridWallRayMask.None,
            out NativeRegularContactRule rule);

        Assert.Multiple(() =>
        {
            Assert.That(accepted, Is.True);
            Assert.That(rule.StemDirection, Is.EqualTo(expectedStem));
            Assert.That(rule.FirstReceiverX, Is.EqualTo(firstX));
            Assert.That(rule.FirstReceiverZ, Is.EqualTo(firstZ));
            Assert.That(rule.SecondReceiverX, Is.EqualTo(secondX));
            Assert.That(rule.SecondReceiverZ, Is.EqualTo(secondZ));
        });
    }

    [TestCase(HybridWallRayMask.South, HybridWallQuadrant.NorthEast,
        HybridWallRayMask.None, HybridWallRayMask.None)]
    [TestCase(HybridWallRayMask.South,
        HybridWallQuadrant.NorthEast | HybridWallQuadrant.NorthWest | HybridWallQuadrant.SouthEast,
        HybridWallRayMask.None, HybridWallRayMask.None)]
    [TestCase(HybridWallRayMask.South | HybridWallRayMask.East,
        HybridWallQuadrant.NorthEast | HybridWallQuadrant.NorthWest,
        HybridWallRayMask.None, HybridWallRayMask.None)]
    [TestCase(HybridWallRayMask.South,
        HybridWallQuadrant.NorthEast | HybridWallQuadrant.NorthWest,
        HybridWallRayMask.South, HybridWallRayMask.None)]
    [TestCase(HybridWallRayMask.South,
        HybridWallQuadrant.NorthEast | HybridWallQuadrant.NorthWest,
        HybridWallRayMask.None, HybridWallRayMask.South)]
    public void AdmissionRejectsLoneExtraDoorAndDoubledCurrentVertexStates(
        HybridWallRayMask thinRays,
        HybridWallQuadrant ordinaryQuadrants,
        HybridWallRayMask doorRays,
        HybridWallRayMask doubledRays)
    {
        Assert.That(NativeRegularContactPlan.TryResolve(
            thinRays,
            ordinaryQuadrants,
            doorRays,
            doubledRays,
            out _), Is.False);
    }
}
