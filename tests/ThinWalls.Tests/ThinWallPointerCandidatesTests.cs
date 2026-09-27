using System.Collections.Generic;
using NUnit.Framework;
using ThinWalls.Geometry;
using ThinWalls.Rendering;
using UnityEngine;
using Verse;

namespace ThinWalls.Tests;

[TestFixture]
public sealed class ThinWallPointerCandidatesTests
{
    private sealed class Candidate
    {
        public OwnedEdge? Edge;
        public bool Eligible = true;
    }

    [Test]
    public void FiltersOnlyMissedEdgesAndAppendsEligibleNeighborHitsOnceInNativeOrder()
    {
        var furniture = new Candidate();
        var pawn = new Candidate();
        var hit = new Candidate { Edge = new OwnedEdge(new IntVec3(10, 0, 20), ThinWallSide.North) };
        var miss = new Candidate { Edge = new OwnedEdge(new IntVec3(10, 0, 20), ThinWallSide.South) };
        var neighbor = new Candidate { Edge = new OwnedEdge(new IntVec3(10, 0, 21), ThinWallSide.South) };
        var ineligible = new Candidate { Edge = hit.Edge, Eligible = false };
        var unrelatedNeighbor = new Candidate();
        var native = new List<Candidate> { furniture, miss, hit, pawn };
        var nearby = new[] { furniture, miss, hit, neighbor, ineligible, unrelatedNeighbor, neighbor };
        int eligibilityChecks = 0;

        ThinWallPointerCandidates.Reconcile(native, nearby, new Vector3(10.5f, 0f, 21.1f),
            candidate => candidate.Edge, candidate => { eligibilityChecks++; return candidate.Eligible; });

        Assert.That(native, Is.EqualTo(new[] { furniture, hit, pawn, neighbor }));
        Assert.That(eligibilityChecks, Is.EqualTo(2), "Only new thin hits need native eligibility checks.");
    }

    [Test]
    public void NoNearbyEdgesLeavesNativeCandidatesUnchanged()
    {
        var furniture = new Candidate();
        var native = new List<Candidate> { furniture };
        ThinWallPointerCandidates.Reconcile(native, new[] { furniture }, Vector3.zero,
            candidate => candidate.Edge, _ => { Assert.Fail("Ordinary neighbors must not be reconsidered."); return false; });
        Assert.That(native, Is.EqualTo(new[] { furniture }));
    }

    [Test]
    public void SeveralEdgesOnOneCellAreSelectedIndependentlyLeavingCenterAvailable()
    {
        var north = new Candidate { Edge = new OwnedEdge(new IntVec3(10, 0, 20), ThinWallSide.North) };
        var east = new Candidate { Edge = new OwnedEdge(new IntVec3(10, 0, 20), ThinWallSide.East) };
        var south = new Candidate { Edge = new OwnedEdge(new IntVec3(10, 0, 20), ThinWallSide.South) };
        var furniture = new Candidate();
        var nearby = new[] { north, east, south, furniture };
        foreach (Vector3 point in new[] { new Vector3(10.5f, 0f, 21f), new Vector3(10.5f, 0f, 20.5f) })
        {
            var native = new List<Candidate>(nearby);
            ThinWallPointerCandidates.Reconcile(native, nearby, point, candidate => candidate.Edge, _ => true);
            Assert.That(native, Is.EqualTo(point.z == 21f ? new[] { north, furniture } : new[] { furniture }));
        }
    }
}
