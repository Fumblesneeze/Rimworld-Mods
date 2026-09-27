using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using ThinWalls.Buildings;
using Verse;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest("thin-walls.native-route-around-region-boundary", "fumblesneeze.thinwalls",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "fumblesneeze.thinwalls",
    MaxFrames = 3_600, MaxGameTicks = 6_000, MaxWallClockSeconds = 140)]
public sealed class ThinWallRegionBoundaryTest : IRimWorldEndToEndTest
{
    private readonly List<Thing> fixtures = new();
    private Map map = null!;
    private Pawn pawn = null!;
    private Thing target = null!;
    private IntVec3 root;
    private bool god;
    private bool crossedBoundary;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        root = new IntVec3(map.Center.x / Region.GridSize * Region.GridSize, 0, map.Center.z / Region.GridSize * Region.GridSize);
        foreach (IntVec3 cell in new CellRect(root.x - 2, root.z - 2, 16, 17).Cells)
        {
            map.roofGrid.SetRoof(cell, null);
            foreach (Thing thing in cell.GetThingList(map).ToArray()) if (thing.def.destroyable) thing.Destroy(DestroyMode.Vanish);
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
            map.fogGrid.Unfog(cell);
        }
        foreach (IntVec3 cell in new CellRect(root.x - 1, root.z - 1, 14, 15).EdgeCells)
        {
            Thing wall = ThingMaker.MakeThing(ThingDefOf.Wall, ThingDefOf.Steel);
            wall.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(wall, cell, map);
            fixtures.Add(wall);
        }
        foreach (IntVec3 cell in new CellRect(root.x + 1, root.z + 7, 3, 3).EdgeCells)
        {
            Thing fence = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Fence"), ThingDefOf.WoodLog);
            fence.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(fence, cell, map);
            fixtures.Add(fence);
        }
        pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        pawn.Name = new NameTriple("Region", "Boundary Tester", "Test");
        GenSpawn.Spawn(pawn, root + new IntVec3(3, 0, 5), map);
        pawn.drafter.Drafted = true;
        fixtures.Add(pawn);
        target = ThingMaker.MakeThing(ThingDefOf.WoodLog);
        GenSpawn.Spawn(target, root + new IntVec3(8, 0, 5), map);
        fixtures.Add(target);
        // Materialize the old native merged spans before the first Thin edge changes the chunk.
        foreach (IntVec3 cell in new CellRect(root.x, root.z, 12, 13).Cells) _ = cell.GetRegion(map);
        god = DebugSettings.godMode;
        DebugSettings.godMode = true;
        context.DeferCleanup(() =>
        {
            DebugSettings.godMode = god;
            foreach (Thing thing in fixtures.ToArray()) if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish);
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var build = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(Array.Empty<string>(), new[] { "Structure" })
            .Single(g => !g.Disabled && g.BuildableDefName == ThinWallUtility.ThinWallDefName && g.Interaction == EndToEndGizmoInteraction.Drag);
        yield return new CameraActionStep("frame the one-cell route over the native chunk boundary", fixtures.Select(t => t.ThingID), paddingPixels: 120);
        yield return new ScreenshotStep("before native divider construction", fixtures.Select(t => t.ThingID), paddingPixels: 120);
        yield return new GizmoActionStep("build the first edge away from existing chunk boundaries", Array.Empty<string>(), build.RuntimeType,
            EndToEndGizmoInteraction.Drag, EndToEndCardinalRotation.East, new EndToEndBuildMaterial("Steel"),
            stableGizmoId: build.StableId, startCell: new EndToEndMapCell(root.x + 5, root.z + 5),
            endCell: new EndToEndMapCell(root.x + 5, root.z + 5), architectCategoryDefNames: new[] { "Structure" });
        yield return new AssertionStep("first interior edge preserves reciprocal native boundary links", _ =>
        {
            Region inside = (root + new IntVec3(3, 0, 11)).GetRegion(map);
            Region receiver = (root + new IntVec3(3, 0, 12)).GetRegion(map);
            EndToEndAssert.True(inside.links.Any(link => link.GetOtherRegion(inside) == receiver),
                "A newly edge-aware chunk must still link to its previously generated native neighbor.");
            EndToEndAssert.True(receiver.links.Any(link => link.GetOtherRegion(receiver) == inside),
                "The receiver must rebuild the matching side of changed link spans.");
            AssertInteriorFenceLinks();
        });
        yield return new GizmoActionStep("build a divider ending exactly at the region boundary", Array.Empty<string>(), build.RuntimeType,
            EndToEndGizmoInteraction.Drag, EndToEndCardinalRotation.East, new EndToEndBuildMaterial("Steel"),
            stableGizmoId: build.StableId, startCell: new EndToEndMapCell(root.x + 5, root.z),
            endCell: new EndToEndMapCell(root.x + 5, root.z + 11), architectCategoryDefNames: new[] { "Structure" });
        fixtures.AddRange(map.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed(ThinWallUtility.ThinWallDefName)));
        yield return new ScreenshotStep("divider leaves the north row open", fixtures.Select(t => t.ThingID), paddingPixels: 120);
        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>().Query(pawn.ThingID, target.ThingID);
        var move = options.SingleOrDefault(o => !o.Disabled && o.Label.IndexOf("go here", StringComparison.OrdinalIgnoreCase) >= 0);
        EndToEndAssert.NotNull(move, "The open row in the neighboring region chunk must remain reachable after first-edge construction.");
        yield return new FloatMenuActionStep("order the native move around the divider endpoint", pawn.ThingID, target.ThingID, move!.StableId);
        yield return new TimeControlActionStep("run the cross-chunk detour", false, EndToEndGameSpeed.Fast);
        yield return new WaitUntilStep("pawn reaches the far side through the north row", _ =>
        {
            crossedBoundary |= pawn.Position.z >= root.z + 12;
            return pawn.Position == target.Position;
        }, new EndToEndDeadline(2_400, 4_500, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep("pause after arrival", true, EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep("after native route through matching region links", fixtures.Select(t => t.ThingID), paddingPixels: 120);
        yield return new AssertionStep("the detour crossed the region boundary", _ => EndToEndAssert.True(crossedBoundary, "The pawn must actually traverse the open neighboring-chunk row."));
        foreach (Building_ThinWall wall in fixtures.OfType<Building_ThinWall>().ToArray())
        {
            var deconstruct = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { wall.ThingID }, Array.Empty<string>())
                .Single(g => !g.Disabled && g.Label.IndexOf("deconstruct", StringComparison.OrdinalIgnoreCase) >= 0);
            yield return new GizmoActionStep("remove the divider through native deconstruction", new[] { wall.ThingID }, deconstruct.RuntimeType,
                EndToEndGizmoInteraction.Invoke, stableGizmoId: deconstruct.StableId);
        }
        yield return new AssertionStep("final removal restores reciprocal native fence and boundary links", _ => AssertInteriorFenceLinks());
    }

    private void AssertInteriorFenceLinks()
    {
        Region fence = (root + new IntVec3(2, 0, 7)).GetRegion(map, RegionType.Set_All);
        Region inside = (root + new IntVec3(2, 0, 8)).GetRegion(map);
        Region outside = (root + new IntVec3(2, 0, 6)).GetRegion(map);
        EndToEndAssert.Equal(RegionType.Fence, fence.type, "The native multi-cell fence region is a real interior receiver.");
        foreach (Region neighbor in new[] { inside, outside })
        {
            EndToEndAssert.True(fence.links.Any(link => link.GetOtherRegion(fence) == neighbor), "Fence must retain its interior/exterior link.");
            EndToEndAssert.True(neighbor.links.Any(link => link.GetOtherRegion(neighbor) == fence), "The neighboring normal region must retain its reciprocal fence link.");
        }
    }
}
