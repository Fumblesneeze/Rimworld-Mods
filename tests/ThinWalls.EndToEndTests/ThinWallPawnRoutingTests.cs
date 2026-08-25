using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using ThinWalls.Buildings;
using ThinWalls.Geometry;
using Verse;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest(
    "thin-walls.native-pawn-routes-around-endpoint",
    "fumblesneeze.thinwalls",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.thinwalls",
    MaxFrames = 4_800,
    MaxGameTicks = 8_000,
    MaxWallClockSeconds = 150)]
public sealed class ThinWallPawnRoutesAroundEndpointTest : IRimWorldEndToEndTest
{
    private readonly List<Thing> fixtures = new();
    private readonly List<Building_ThinWall> walls = new();
    private Map map = null!;
    private Pawn pawn = null!;
    private Thing target = null!;
    private IntVec3 start;
    private IntVec3 destination;
    private string goHereOptionId = string.Empty;
    private IntVec3 sampledPosition;
    private bool crossedBlockedEdge;
    private bool observedDetour;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        IntVec3 center = FindClearCenter(map, 10);
        start = center + new IntVec3(0, 0, -3);
        destination = center + new IntVec3(0, 0, 3);

        ThingDef thinWallDef = DefDatabase<ThingDef>.GetNamed(ThinWallUtility.ThinWallDefName);
        for (int x = -2; x <= 2; x++)
        {
            var wall = (Building_ThinWall)ThingMaker.MakeThing(thinWallDef, ThingDefOf.WoodLog);
            wall.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(wall, center + new IntVec3(x, 0, 0), map, Rot4.North);
            walls.Add(wall);
        }

        pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        pawn.Name = new NameTriple("Thin", "Pathfinder", "Tester");
        GenSpawn.Spawn(pawn, start, map);
        pawn.drafter.Drafted = true;

        target = ThingMaker.MakeThing(ThingDefOf.WoodLog);
        target.stackCount = 1;
        GenSpawn.Spawn(target, destination, map);
        fixtures.Add(pawn);
        fixtures.Add(target);
        sampledPosition = pawn.Position;

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

        EndToEndFloatMenuOption[] options = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(pawn.ThingID, target.ThingID)
            .Where(option => !option.Disabled &&
                             option.Label.IndexOf("go here", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(1, options.Length,
            "A drafted colonist must expose exactly one enabled native Go here order");
        goHereOptionId = options[0].StableId;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new SelectionActionStep(
            "select drafted pawn before edge-aware move order",
            new[] { pawn.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame pawn target and finite thin-wall line",
            new[] { pawn.ThingID, target.ThingID }.Concat(walls.Select(wall => wall.ThingID)),
            paddingPixels: 160);
        yield return new ScreenshotStep(
            "before native Go here order across finite thin-wall line",
            new[] { pawn.ThingID, target.ThingID }.Concat(walls.Select(wall => wall.ThingID)),
            paddingPixels: 150);

        yield return new FloatMenuActionStep(
            "give drafted pawn the native Go here order",
            pawn.ThingID,
            target.ThingID,
            goHereOptionId);
        yield return new TimeControlActionStep(
            "run endpoint-routing movement",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "pawn visibly commits to a route around the wall endpoint",
            _ =>
            {
                SampleMovement();
                return pawn.Position != start && pawn.pather.MovingNow;
            },
            new EndToEndDeadline(1_200, 1_500, TimeSpan.FromSeconds(45)));
        yield return new ScreenshotStep(
            "during native movement around the thin-wall endpoint",
            new[] { pawn.ThingID, target.ThingID }.Concat(walls.Select(wall => wall.ThingID)),
            paddingPixels: 150);
        yield return new WaitUntilStep(
            "pawn reaches the ordered cell without crossing a thin-wall edge",
            _ =>
            {
                SampleMovement();
                return pawn.Position == destination;
            },
            new EndToEndDeadline(3_000, 4_500, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep(
            "pause after endpoint-routing arrival",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "native path reaches target only by the open endpoint",
            _ =>
            {
                EndToEndAssert.False(crossedBlockedEdge,
                    "The pawn must never change cells through a completed thin-wall edge.");
                EndToEndAssert.True(observedDetour,
                    "The pawn must leave the direct central corridor and visibly route around an endpoint.");
                EndToEndAssert.Equal(destination, pawn.Position,
                    "The native move order must still reach its exact destination.");
            });
        yield return new ScreenshotStep(
            "after native endpoint detour reaches destination",
            new[] { pawn.ThingID, target.ThingID }.Concat(walls.Select(wall => wall.ThingID)),
            paddingPixels: 150);
        yield return new CheckpointStep(
            "edge-aware endpoint route outcome",
            _ => new Dictionary<string, string>
            {
                ["pawnThingId"] = pawn.ThingID,
                ["wallCount"] = walls.Count.ToString(),
                ["observedDetour"] = observedDetour.ToString(),
                ["crossedBlockedEdge"] = crossedBlockedEdge.ToString(),
                ["destination"] = destination.ToString(),
            });
    }

    private void SampleMovement()
    {
        IntVec3 current = pawn.Position;
        if (current != sampledPosition)
        {
            crossedBlockedEdge |= ThinWallUtility.BlocksStep(map, sampledPosition, current);
            sampledPosition = current;
        }

        IntVec3 center = new(destination.x, 0, start.z + 3);
        observedDetour |= Math.Abs(current.x - center.x) >= 3;
    }

    private static IntVec3 FindClearCenter(Map map, int radius)
    {
        for (int x = -60; x <= 60; x += 20)
        {
            for (int z = -60; z <= 60; z += 20)
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

        throw new EndToEndAssertionException("Could not find a clear Thin Walls routing area.");
    }
}
