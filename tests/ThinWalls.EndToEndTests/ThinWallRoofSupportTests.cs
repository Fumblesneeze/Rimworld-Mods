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
    "thin-walls.native-final-real-roof-support-removal",
    "fumblesneeze.thinwalls",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.thinwalls",
    MaxFrames = 4_800,
    MaxGameTicks = 8_000,
    MaxWallClockSeconds = 180)]
public sealed class ThinWallRoofSupportTest : IRimWorldEndToEndTest
{
    private readonly List<Thing> fixtures = new();
    private readonly List<IntVec3> roofCells = new();
    private Map map = null!;
    private IntVec3 center;
    private Pawn builder = null!;
    private Building support = null!;
    private Building_ThinWall firstWall = null!;
    private Building_ThinWall adjacentWall = null!;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        center = FindClearCenter(map, 10);
        RoofDef roof = DefDatabase<RoofDef>.GetNamed("RoofConstructed");

        builder = GenerateBuilder();
        GenSpawn.Spawn(builder, center + new IntVec3(-2, 0, 0), map);
        fixtures.Add(builder);

        ThingDef columnDef = DefDatabase<ThingDef>.GetNamed("Column");
        support = (Building)ThingMaker.MakeThing(columnDef, ThingDefOf.Steel);
        support.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(support, center, map);
        fixtures.Add(support);

        ThingDef thinWallDef = DefDatabase<ThingDef>.GetNamed(ThinWallUtility.ThinWallDefName);
        IntVec3 firstOwner = center + new IntVec3(2, 0, 0);
        IntVec3 secondOwner = firstOwner + IntVec3.East;
        firstWall = (Building_ThinWall)ThingMaker.MakeThing(thinWallDef, ThingDefOf.Steel);
        firstWall.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(firstWall, firstOwner, map, Rot4.South);
        adjacentWall = (Building_ThinWall)ThingMaker.MakeThing(thinWallDef, ThingDefOf.WoodLog);
        adjacentWall.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(adjacentWall, secondOwner, map, Rot4.South);
        fixtures.Add(firstWall);
        fixtures.Add(adjacentWall);

        for (int x = 1; x <= 3; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                IntVec3 cell = center + new IntVec3(x, 0, z);
                map.roofGrid.SetRoof(cell, roof);
                roofCells.Add(cell);
            }
        }

        context.DeferCleanup(() =>
        {
            foreach (IntVec3 cell in roofCells)
            {
                if (cell.InBounds(map) && map.roofGrid.Roofed(cell))
                {
                    map.roofGrid.SetRoof(cell, null);
                }
            }

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
            "select final real support and two unique thin edges",
            new[] { support.ThingID, firstWall.ThingID, adjacentWall.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame supported roof and non-supporting thin walls",
            fixtures.Select(thing => thing.ThingID),
            paddingPixels: 190);
        yield return new ScreenshotStep(
            "before native removal of the final real roof support",
            fixtures.Select(thing => thing.ThingID),
            paddingPixels: 190);
        yield return new AssertionStep(
            "multiple unique thin walls advertise zero roof support",
            _ =>
            {
                EndToEndAssert.True(roofCells.All(cell => map.roofGrid.Roofed(cell)),
                    "The real column must initially hold the constructed-roof fixture.");
                EndToEndAssert.False(firstWall.def.holdsRoof,
                    "A single Thin Wall must not hold roofs.");
                EndToEndAssert.False(adjacentWall.def.holdsRoof,
                    "A second unique Thin Wall must not contribute roof support.");
                EndToEndAssert.False(firstWall.OwnedEdge.Shared.Equals(adjacentWall.OwnedEdge.Shared),
                    "The fixture must not manufacture an opposite-owner duplicate.");
            });

        EndToEndGizmoOption deconstruct = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { support.ThingID }, Array.Empty<string>())
            .Single(option => !option.Disabled &&
                              option.Interaction == EndToEndGizmoInteraction.Invoke &&
                              option.Label.IndexOf("deconstruct", StringComparison.OrdinalIgnoreCase) >= 0);
        yield return new GizmoActionStep(
            "order native deconstruction of the final real roof support",
            new[] { support.ThingID },
            deconstruct.RuntimeType,
            EndToEndGizmoInteraction.Invoke,
            stableGizmoId: deconstruct.StableId);

        EndToEndFloatMenuOption prioritize = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(builder.ThingID, support.ThingID)
            .Single(option => !option.Disabled &&
                              option.Label.IndexOf("prioritize", StringComparison.OrdinalIgnoreCase) >= 0 &&
                              option.Label.IndexOf("deconstruct", StringComparison.OrdinalIgnoreCase) >= 0);
        yield return new FloatMenuActionStep(
            "prioritize native deconstruction of the final real support",
            builder.ThingID,
            support.ThingID,
            prioritize.StableId);
        yield return new TimeControlActionStep(
            "run ordinary support deconstruction and roof collapse",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "ordinary Construction removes the final real support",
            _ => support.Destroyed,
            new EndToEndDeadline(2_400, 5_000, TimeSpan.FromSeconds(80)));
        yield return new WaitUntilStep(
            "unsupported constructed roof collapses despite multiple thin walls",
            _ => roofCells.All(cell => !map.roofGrid.Roofed(cell)),
            new EndToEndDeadline(1_200, 2_000, TimeSpan.FromSeconds(50)));
        yield return new TimeControlActionStep(
            "pause after unsupported roof collapse",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new CameraActionStep(
            "frame roof outcome after final real support removal",
            fixtures.Where(thing => !thing.Destroyed).Select(thing => thing.ThingID),
            paddingPixels: 190);
        yield return new ScreenshotStep(
            "after native final support removal and unsupported roof collapse",
            fixtures.Where(thing => !thing.Destroyed).Select(thing => thing.ThingID),
            paddingPixels: 190);
        yield return new CheckpointStep(
            "thin-wall roof-support outcome",
            _ => new Dictionary<string, string>
            {
                ["supportDestroyed"] = support.Destroyed.ToString(),
                ["remainingRoofCells"] = roofCells.Count(cell => map.roofGrid.Roofed(cell)).ToString(),
                ["firstSharedEdgeOwnerCount"] = ThinWallUtility.ThingsOnSharedEdge(
                    map,
                    firstWall.OwnedEdge.Shared,
                    completedOnly: true).Count().ToString(),
                ["holdsRoof"] = firstWall.def.holdsRoof.ToString(),
            });
    }

    private static Pawn GenerateBuilder()
    {
        for (int attempt = 0; attempt < 64; attempt++)
        {
            Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
            if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Construction))
            {
                pawn.Destroy(DestroyMode.Vanish);
                continue;
            }

            pawn.Name = new NameTriple("Thin", "Roof Builder", "Tester");
            pawn.workSettings.EnableAndInitialize();
            foreach (WorkTypeDef workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            {
                if (!pawn.WorkTypeIsDisabled(workType))
                {
                    pawn.workSettings.SetPriority(workType, 0);
                }
            }

            pawn.workSettings.SetPriority(WorkTypeDefOf.Construction, 1);
            pawn.skills.GetSkill(SkillDefOf.Construction).Level = 20;
            return pawn;
        }

        throw new EndToEndAssertionException("Could not generate a capable roof-support builder.");
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
                        cell.Standable(map) && !map.roofGrid.Roofed(cell) && cell.GetThingList(map).Count == 0))
                {
                    return candidate;
                }
            }
        }

        throw new EndToEndAssertionException("Could not find a clear Thin Walls roof fixture area.");
    }
}
