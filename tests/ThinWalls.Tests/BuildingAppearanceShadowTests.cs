using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using ThinWalls.Rendering;
using UnityEngine;
using Verse;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class BuildingAppearanceShadowTests
{
    [Test]
    public void SectionPartitionsOwnOnlyTheirPartOfTheCasterAndNeverCastFromInternalSeams()
    {
        var vertices = new List<Vector3>();
        var colors = new List<Color32>();
        var triangles = new List<int>();
        var footprint = new CellRect(16, 8, 3, 1);
        var pivot = new Vector3(17.5f, 0, 8.5f);
        var appearance = new BuildingAppearance(5, 3);
        BuildingAppearanceShadowGeometry.Append(vertices, colors, triangles, footprint, pivot, appearance, 7, .5f,
            new CellRect(0, 0, 17, 17));
        Assert.That(vertices.Count, Is.EqualTo(8), "West section owns floor, west and south faces, never an internal east face.");
        Assert.That(vertices.Max(v => v.x), Is.EqualTo(17.45f).Within(.00001f));
        int boundary = vertices.Count;
        BuildingAppearanceShadowGeometry.Append(vertices, colors, triangles, footprint, pivot, appearance, 7, .5f,
            new CellRect(17, 0, 17, 17));
        Assert.That(vertices.Count - boundary, Is.EqualTo(8), "East section owns floor, east and south, never an internal west face.");
        Assert.That(vertices.Skip(boundary).Min(v => v.x), Is.EqualTo(17.45f).Within(.00001f));
        Assert.That(vertices.Min(v => v.x), Is.EqualTo(16.95f).Within(.00001f));
        Assert.That(vertices.Max(v => v.x), Is.EqualTo(18.45f).Within(.00001f));
        Assert.That(triangles.All(i => i >= 0 && i < vertices.Count), Is.True);
    }

    [Test]
    public void SunShadowFollowsScaledOffsetFootprintAndHeightWithNoOriginalCellBlob()
    {
        var vertices = new List<Vector3>();
        var colors = new List<Color32>();
        var triangles = new List<int>();
        BuildingAppearanceShadowGeometry.Append(vertices, colors, triangles, new CellRect(0, 0, 1, 1),
            new Vector3(.5f, 0f, .5f), new BuildingAppearance(5, 1), 7f, .5f);
        Assert.That(vertices.Count, Is.EqualTo(10));
        Assert.That(triangles.Count, Is.EqualTo(24));
        Assert.That(vertices.Min(x => x.x), Is.EqualTo(.25f).Within(.00001f));
        Assert.That(vertices.Max(x => x.x), Is.EqualTo(.75f).Within(.00001f));
        Assert.That(vertices.Min(x => x.z), Is.EqualTo(.45f).Within(.00001f));
        Assert.That(vertices.Max(x => x.z), Is.EqualTo(.95f).Within(.00001f));
        Assert.That(vertices.All(x => x.y == 7f), Is.True);
        Assert.That(colors.Take(4).All(x => x.a == 0), Is.True);
        Assert.That(colors.Skip(4).All(x => x.a == 64), Is.True);
        Assert.That(triangles.All(x => x >= 0 && x < vertices.Count), Is.True);
    }
}
