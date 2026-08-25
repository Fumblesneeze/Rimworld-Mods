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
    "thin-walls.bash-capable-job-destroys-endpoint-and-shared-edge",
    "fumblesneeze.thinwalls",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.thinwalls",
    MaxFrames = 6_000,
    MaxGameTicks = 12_000,
    MaxWallClockSeconds = 200)]
public sealed class ThinWallBashCapableTraversalTest : IRimWorldEndToEndTest
{
    private readonly List<Thing> fixtures = new();
    private Map map = null!;
    private Pawn raider = null!;
    private Building_ThinWall firstOwner = null!;
    private Building_ThinWall endpointOwner = null!;
    private IntVec3 start;
    private IntVec3 endpointDestination;
    private IntVec3 edgeApproach;
    private IntVec3 destination;
    private SharedEdge blockedEdge;
    private bool firstDamagedByMelee;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        IntVec3 center = FindClearCenter(map, 10);
        start = center + new IntVec3(-7, 0, -1);
        endpointDestination = start + IntVec3.North + IntVec3.East;
        edgeApproach = center;
        destination = center + IntVec3.East;

        ThingDef wallDef = DefDatabase<ThingDef>.GetNamed(ThinWallUtility.ThinWallDefName);
        var firstEdge = new OwnedEdge(center, ThinWallSide.East);
        blockedEdge = firstEdge.Shared;
        firstOwner = SpawnWall(wallDef, ThingDefOf.WoodLog, firstEdge);
        endpointOwner = SpawnWall(wallDef, ThingDefOf.WoodLog,
            new OwnedEdge(start + IntVec3.North, ThinWallSide.East));
        firstOwner.HitPoints = Math.Min(firstOwner.HitPoints, 35);
        endpointOwner.HitPoints = Math.Min(endpointOwner.HitPoints, 35);

        for (int x = -5; x <= 9; x++)
        {
            foreach (int z in new[] { -1, 1 })
            {
                var corridorWall = (Building)ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
                corridorWall.SetFactionDirect(Faction.OfPlayer);
                GenSpawn.Spawn(corridorWall, center + new IntVec3(x, 0, z), map);
                fixtures.Add(corridorWall);
            }
        }

        Faction enemy = Find.FactionManager.AllFactions
            .First(faction => faction.HostileTo(Faction.OfPlayer) && !faction.def.hidden);
        PawnKindDef kind = enemy.def.basicMemberKind ?? PawnKindDefOf.Colonist;
        raider = PawnGenerator.GeneratePawn(kind, enemy);
        raider.Name = new NameTriple("Thin", "Wall Raider", "Raider");
        raider.skills.GetSkill(SkillDefOf.Melee).Level = 20;
        GenSpawn.Spawn(raider, start, map);
        fixtures.Add(raider);

        Thing destinationMarker = ThingMaker.MakeThing(ThingDefOf.WoodLog);
        GenSpawn.Spawn(destinationMarker, destination, map);
        fixtures.Add(destinationMarker);

        context.DeferCleanup(() =>
        {
            foreach (Thing thing in fixtures.ToArray())
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
            "select the far endpoint owner blocking a diagonal step",
            new[] { endpointOwner.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame bash-capable pawn and diagonal endpoint blocker",
            fixtures.Select(thing => thing.ThingID),
            paddingPixels: 180);
        yield return new ScreenshotStep(
            "before bash-capable diagonal traversal reaches the far endpoint",
            fixtures.Select(thing => thing.ThingID),
            paddingPixels: 180);

        StartDestroyableRoute(endpointDestination);
        yield return new TimeControlActionStep(
            "run bash-capable traversal into the far endpoint owner",
            paused: false,
            EndToEndGameSpeed.Fast);
        yield return new WaitUntilStep(
            "far endpoint crossing starts the owned edge-melee job",
            _ => raider.CurJobDef?.defName == ThinWallUtility.ThinWallBashJobDefName,
            new EndToEndDeadline(1_500, 2_500, TimeSpan.FromSeconds(55)));
        yield return new ScreenshotStep(
            "during owned melee against the far endpoint owner",
            fixtures.Where(thing => !thing.Destroyed).Select(thing => thing.ThingID),
            paddingPixels: 180);
        yield return new WaitUntilStep(
            "ordinary melee destroys the far endpoint owner",
            _ => endpointOwner.Destroyed,
            new EndToEndDeadline(2_400, 5_000, TimeSpan.FromSeconds(80)));
        StartDestroyableRoute(endpointDestination);
        yield return new WaitUntilStep(
            "pawn reaches the diagonal destination after endpoint destruction",
            _ => raider.Position == endpointDestination,
            new EndToEndDeadline(1_500, 2_500, TimeSpan.FromSeconds(55)));
        StartDestroyableRoute(edgeApproach);
        yield return new WaitUntilStep(
            "pawn reaches the adjacent shared-edge approach cell through native movement",
            _ => raider.Position == edgeApproach,
            new EndToEndDeadline(1_500, 2_500, TimeSpan.FromSeconds(55)));
        yield return new TimeControlActionStep(
            "pause at the adjacent shared-edge approach cell",
            paused: true,
            EndToEndGameSpeed.Normal);

        yield return new SelectionActionStep(
            "select the unique wall on the shared edge",
            new[] { firstOwner.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame bash-capable pawn and unique shared edge",
            fixtures.Where(thing => !thing.Destroyed).Select(thing => thing.ThingID),
            paddingPixels: 180);
        yield return new ScreenshotStep(
            "before bash-capable traversal reaches the thin wall",
            fixtures.Where(thing => !thing.Destroyed).Select(thing => thing.ThingID),
            paddingPixels: 180);

        StartDestroyableRoute();
        yield return new CheckpointStep(
            "pawn begins an explicitly bash-capable path",
            _ => new Dictionary<string, string>
            {
                ["job"] = raider.CurJobDef?.defName ?? "none",
                ["canBashDoors"] = (raider.CurJob?.canBashDoors ?? false).ToString(),
                ["ownerCount"] = ThinWallUtility.ThingsOnSharedEdge(map, blockedEdge, completedOnly: true).Count().ToString(),
            });
        yield return new TimeControlActionStep(
            "run bash-capable traversal into the first concrete owner",
            paused: false,
            EndToEndGameSpeed.Fast);
        yield return new WaitUntilStep(
            "pawn receives the owned edge-melee blocker job",
            _ =>
            {
                firstDamagedByMelee |= firstOwner.HitPoints < 35;
                return raider.CurJobDef?.defName == ThinWallUtility.ThinWallBashJobDefName ||
                       firstDamagedByMelee;
            },
            new EndToEndDeadline(1_500, 2_500, TimeSpan.FromSeconds(55)));
        yield return new ScreenshotStep(
            "during ordinary melee against the first thin-wall owner",
            fixtures.Where(thing => !thing.Destroyed).Select(thing => thing.ThingID),
            paddingPixels: 180);
        yield return new WaitUntilStep(
            "ordinary melee destroys the first concrete owner",
            _ =>
            {
                firstDamagedByMelee |= firstOwner.HitPoints < 35;
                return firstOwner.Destroyed;
            },
            new EndToEndDeadline(2_400, 5_000, TimeSpan.FromSeconds(80)));
        yield return new TimeControlActionStep(
            "pause after shared-edge wall destruction",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "destroying the unique wall reopens the shared edge",
            _ =>
            {
                EndToEndAssert.True(firstDamagedByMelee,
                    "The first owner must visibly lose hit points through ordinary melee before destruction.");
                EndToEndAssert.False(ThinWallUtility.HasWall(map, blockedEdge),
                    "The unique shared-edge structure must be gone after ordinary melee destroys it.");
            });

        StartDestroyableRoute();
        yield return new TimeControlActionStep(
            "run movement after the shared-edge wall is destroyed",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "pawn crosses only after the shared edge is open",
            _ => raider.Position == destination,
            new EndToEndDeadline(2_400, 3_500, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep(
            "pause after final crossing",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep(
            "after ordinary melee removes the wall and the pawn reaches the far side",
            fixtures.Where(thing => !thing.Destroyed).Select(thing => thing.ThingID),
            paddingPixels: 180);
        yield return new CheckpointStep(
            "endpoint and shared-edge bash-capable outcome",
            _ => new Dictionary<string, string>
            {
                ["firstDamagedByMelee"] = firstDamagedByMelee.ToString(),
                ["firstDestroyed"] = firstOwner.Destroyed.ToString(),
                ["raiderDestination"] = raider.Position.ToString(),
            });
    }

    private Building_ThinWall SpawnWall(ThingDef def, ThingDef stuff, OwnedEdge edge)
    {
        var wall = (Building_ThinWall)ThingMaker.MakeThing(def, stuff);
        wall.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(wall, edge.Cell, map, new Rot4((int)edge.Side));
        fixtures.Add(wall);
        return wall;
    }

    private void StartDestroyableRoute()
    {
        StartDestroyableRoute(destination);
    }

    private void StartDestroyableRoute(IntVec3 routeDestination)
    {
        Job job = JobMaker.MakeJob(JobDefOf.Goto, routeDestination);
        job.canBashDoors = true;
        job.locomotionUrgency = LocomotionUrgency.Jog;
        raider.jobs.StartJob(job, JobCondition.InterruptForced);
    }

    private static IntVec3 FindClearCenter(Map map, int radius)
    {
        for (int x = -60; x <= 60; x += 24)
        {
            for (int z = -60; z <= 60; z += 24)
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
        throw new EndToEndAssertionException("Could not find a clear bash-capable fixture area.");
    }
}
