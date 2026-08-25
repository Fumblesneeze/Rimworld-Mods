using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using ThinWalls.Buildings;
using ThinWalls.Geometry;
using Verse;
using Verse.AI;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest(
    "thin-walls.native-closed-enclosure-and-l-corner-routing",
    "fumblesneeze.thinwalls",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.thinwalls",
    MaxFrames = 5_400,
    MaxGameTicks = 9_000,
    MaxWallClockSeconds = 180)]
public sealed class ThinWallClosedAndCornerRoutingTest : IRimWorldEndToEndTest
{
    private readonly List<Thing> fixtures = new();
    private readonly List<Building_ThinWall> walls = new();
    private Map map = null!;
    private Pawn enclosedPawn = null!;
    private Pawn cornerPawn = null!;
    private Thing enclosureTarget = null!;
    private Thing cornerTarget = null!;
    private IntVec3 enclosureCell;
    private IntVec3 cornerStart;
    private IntVec3 cornerDestination;
    private IntVec3 cornerSample;
    private bool crossedBlockedCorner;
    private bool observedCornerDetour;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        IntVec3 center = FindClearCenter(map, 14);
        enclosureCell = center + new IntVec3(-6, 0, 0);
        cornerStart = center + new IntVec3(4, 0, -2);
        cornerDestination = cornerStart + new IntVec3(1, 0, 1);
        ThingDef wallDef = DefDatabase<ThingDef>.GetNamed(ThinWallUtility.ThinWallDefName);

        foreach (ThinWallSide side in new[]
                 {
                     ThinWallSide.North, ThinWallSide.East, ThinWallSide.South, ThinWallSide.West,
                 })
        {
            walls.Add(SpawnWall(wallDef, new OwnedEdge(enclosureCell, side)));
        }

        walls.Add(SpawnWall(wallDef, new OwnedEdge(cornerStart, ThinWallSide.North)));
        walls.Add(SpawnWall(wallDef, new OwnedEdge(cornerStart, ThinWallSide.East)));

        enclosedPawn = SpawnDraftedPawn("Thin Wall Enclosed Pathfinder", enclosureCell);
        cornerPawn = SpawnDraftedPawn("Thin Wall Corner Pathfinder", cornerStart);
        enclosureTarget = SpawnMarker(enclosureCell + new IntVec3(3, 0, 0));
        cornerTarget = SpawnMarker(cornerDestination);
        cornerSample = cornerPawn.Position;

        context.DeferCleanup(() =>
        {
            foreach (Thing thing in fixtures.Concat<Thing>(walls).ToArray())
            {
                if (!thing.Destroyed)
                {
                    thing.Destroy(DestroyMode.Vanish);
                }
            }
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new SelectionActionStep(
            "select pawns at closed and L-corner edge fixtures",
            new[] { enclosedPawn.ThingID, cornerPawn.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame closed enclosure and L-corner path fixtures",
            fixtures.Select(thing => thing.ThingID).Concat(walls.Select(wall => wall.ThingID)),
            paddingPixels: 170);
        yield return new ScreenshotStep(
            "before native movement checks at enclosure and L corner",
            fixtures.Select(thing => thing.ThingID).Concat(walls.Select(wall => wall.ThingID)),
            paddingPixels: 170);

        EndToEndFloatMenuOption[] enclosureOptions = context
            .GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(enclosedPawn.ThingID, enclosureTarget.ThingID)
            .Where(option => option.Label.IndexOf("go here", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        yield return new AssertionStep(
            "native move menu exposes no enabled path through closed edges",
            _ =>
            {
                EndToEndAssert.False(enclosureOptions.Any(option => !option.Disabled),
                    "A drafted pawn fully enclosed by four edge owners must have no enabled Go here order outside.");
                EndToEndAssert.False(enclosedPawn.CanReach(enclosureTarget, PathEndMode.OnCell, Danger.Deadly),
                    "Edge-aware reachability must refine the vanilla region answer to false.");
            });

        Job impossibleMove = JobMaker.MakeJob(JobDefOf.Goto, enclosureTarget.Position);
        impossibleMove.canBashDoors = false;
        enclosedPawn.jobs.StartJob(impossibleMove, JobCondition.InterruptForced);
        int noPathStartTick = Find.TickManager.TicksGame;
        yield return new TimeControlActionStep(
            "run a normal non-bashing path request against the enclosure",
            paused: false,
            EndToEndGameSpeed.Fast);
        yield return new WaitUntilStep(
            "closed enclosure remains stationary through the no-path window",
            _ => Find.TickManager.TicksGame >= noPathStartTick + 180,
            new EndToEndDeadline(900, 1_000, TimeSpan.FromSeconds(35)));
        yield return new TimeControlActionStep(
            "pause after closed-enclosure no-path outcome",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "closed edge enclosure prevents every cardinal and diagonal escape",
            _ => EndToEndAssert.Equal(enclosureCell, enclosedPawn.Position,
                "The normal path request must not move the pawn through any enclosing edge."));

        string cornerMove = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(cornerPawn.ThingID, cornerTarget.ThingID)
            .Single(option => !option.Disabled &&
                              option.Label.IndexOf("go here", StringComparison.OrdinalIgnoreCase) >= 0)
            .StableId;
        yield return new FloatMenuActionStep(
            "give native diagonal destination order at the L corner",
            cornerPawn.ThingID,
            cornerTarget.ThingID,
            cornerMove);
        yield return new TimeControlActionStep(
            "run native movement around the L endpoint",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "corner pawn reaches the diagonal cell by a genuinely open route",
            _ =>
            {
                SampleCornerMovement();
                return cornerPawn.Position == cornerDestination;
            },
            new EndToEndDeadline(3_000, 4_500, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep(
            "pause after L-corner detour",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep(
            "after closed-enclosure rejection and L-corner detour",
            fixtures.Select(thing => thing.ThingID).Concat(walls.Select(wall => wall.ThingID)),
            paddingPixels: 170);
        yield return new AssertionStep(
            "L junction blocks diagonal corner slip without sealing the open map",
            _ =>
            {
                EndToEndAssert.False(crossedBlockedCorner,
                    "No sampled transition may cross either L edge or their common endpoint.");
                EndToEndAssert.True(observedCornerDetour,
                    "The pawn must take more than the direct one-step diagonal route.");
                EndToEndAssert.Equal(cornerDestination, cornerPawn.Position,
                    "An open route around the L endpoints must still reach the destination.");
            });
        yield return new CheckpointStep(
            "closed and corner edge-routing outcome",
            _ => new Dictionary<string, string>
            {
                ["enclosedPawnPosition"] = enclosedPawn.Position.ToString(),
                ["enclosureEnabledGoHere"] = enclosureOptions.Any(option => !option.Disabled).ToString(),
                ["cornerCrossedBlockedEdge"] = crossedBlockedCorner.ToString(),
                ["cornerDetour"] = observedCornerDetour.ToString(),
            });
    }

    private Building_ThinWall SpawnWall(ThingDef def, OwnedEdge edge)
    {
        var wall = (Building_ThinWall)ThingMaker.MakeThing(def, ThingDefOf.Steel);
        wall.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(wall, edge.Cell, map, new Rot4((int)edge.Side));
        return wall;
    }

    private Pawn SpawnDraftedPawn(string name, IntVec3 cell)
    {
        Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        pawn.Name = new NameTriple("Thin", name, "Tester");
        GenSpawn.Spawn(pawn, cell, map);
        pawn.drafter.Drafted = true;
        fixtures.Add(pawn);
        return pawn;
    }

    private Thing SpawnMarker(IntVec3 cell)
    {
        Thing marker = ThingMaker.MakeThing(ThingDefOf.WoodLog);
        GenSpawn.Spawn(marker, cell, map);
        fixtures.Add(marker);
        return marker;
    }

    private void SampleCornerMovement()
    {
        IntVec3 current = cornerPawn.Position;
        if (current == cornerSample)
        {
            return;
        }

        crossedBlockedCorner |= ThinWallUtility.BlocksStep(map, cornerSample, current);
        observedCornerDetour |= current != cornerDestination || cornerSample != cornerStart;
        cornerSample = current;
    }

    private static IntVec3 FindClearCenter(Map map, int radius)
    {
        for (int x = -60; x <= 60; x += 28)
        {
            for (int z = -60; z <= 60; z += 28)
            {
                IntVec3 candidate = map.Center + new IntVec3(x, 0, z);
                CellRect area = CellRect.CenteredOn(candidate, radius);
                if (area.InBounds(map) && area.Cells.All(cell =>
                        cell.Standable(map) && !cell.Fogged(map) && cell.GetThingList(map).Count == 0))
                {
                    return candidate;
                }
            }
        }
        throw new EndToEndAssertionException("Could not find a clear enclosure/corner fixture area.");
    }
}
