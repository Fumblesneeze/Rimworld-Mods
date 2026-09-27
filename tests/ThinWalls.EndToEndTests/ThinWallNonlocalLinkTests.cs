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

[RimWorldEndToEndTest("thin-walls.asab-native-stairs-with-owned-edges", "fumblesneeze.thinwalls",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "astryl.asabovesobelow2", "fumblesneeze.thinwalls",
    MaxFrames = 10_800, MaxGameTicks = 16_000, MaxWallClockSeconds = 300)]
public sealed class ThinWallNonlocalLinkTest : IRimWorldEndToEndTest
{
    private readonly List<Thing> fixtures = new();
    private Map map = null!;
    private Pawn pawn = null!;
    private Thing stairs = null!;
    private Thing counterpart = null!;
    private Thing destination = null!;
    private IntVec3 origin;
    private bool godMode;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        MapComponent bands = map.components.Single(c => c.GetType().FullName == "AsAboveSoBelow.ABBandMap");
        Type type = bands.GetType();
        int count = (int)type.GetField("bandCount")!.GetValue(bands);
        int surface = (int)type.GetField("surfaceBand")!.GetValue(bands);
        EndToEndAssert.True(count > 1, "ASAB must generate a real multilevel quicktest map.");
        CellRect surfaceRect = (CellRect)type.GetMethod("RectOfBand")!.Invoke(bands, new object[] { surface });
        origin = surfaceRect.CenterCell;
        bool up = surface + 1 < count;
        IntVec3 other = (IntVec3)type.GetMethod("Translate")!.Invoke(bands,
            new object[] { origin, up ? surface + 1 : surface - 1 });
        // Disposable landing preconditions, not the transit outcome: ASAB owns pairing, links and movement.
        foreach (IntVec3 center in new[] { origin, other })
        foreach (IntVec3 cell in CellRect.CenteredOn(center, 12).Cells)
        {
            map.roofGrid.SetRoof(cell, null);
            foreach (Thing thing in cell.GetThingList(map).ToArray())
                if (thing.def.destroyable) thing.Destroy(DestroyMode.Vanish);
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
            map.fogGrid.Unfog(cell);
        }
        stairs = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(up ? "AB2_LadderUp" : "AB2_LadderDown"), ThingDefOf.Steel);
        stairs.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(stairs, origin, map);
        fixtures.Add(stairs);
        counterpart = ((IEnumerable)stairs.GetType().GetProperty("Counterparts")!.GetValue(stairs))
            .Cast<Thing>().Single();
        fixtures.Add(counterpart);
        destination = ThingMaker.MakeThing(ThingDefOf.WoodLog);
        GenSpawn.Spawn(destination, counterpart.Position + IntVec3.East * 4, map);
        fixtures.Add(destination);
        pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        pawn.Name = new NameTriple("Thin", "Stair Tester", "Test");
        GenSpawn.Spawn(pawn, origin + IntVec3.West * 5, map);
        pawn.drafter.Drafted = true;
        fixtures.Add(pawn);
        godMode = DebugSettings.godMode;
        DebugSettings.godMode = true;
        context.DeferCleanup(() =>
        {
            DebugSettings.godMode = godMode;
            foreach (Thing thing in fixtures.ToArray()) if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish);
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new AssertionStep("native cross-band reachability works before thin edges", _ =>
            EndToEndAssert.True(pawn.CanReach(destination, PathEndMode.OnCell, Danger.Deadly), "The no-wall native cross-band control must be reachable."));
        yield return new CameraActionStep("frame native ladder control", new[] { pawn.ThingID, stairs.ThingID }, paddingPixels: 180);
        yield return new ScreenshotStep("before no-wall native ladder control", new[] { pawn.ThingID, stairs.ThingID }, paddingPixels: 180);
        yield return new FloatMenuActionStep("climb ladder without thin edges", pawn.ThingID, stairs.ThingID, Travel(context, stairs));
        yield return new TimeControlActionStep("run native ladder control", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep("no-wall control reaches the other level", _ => OnBandOf(counterpart),
            new EndToEndDeadline(2_400, 3_500, TimeSpan.FromSeconds(70)));
        yield return new TimeControlActionStep("pause no-wall control", true, EndToEndGameSpeed.Normal);
        CameraJumper.TryJump(pawn);
        yield return new CameraActionStep("frame no-wall arrival", new[] { pawn.ThingID, counterpart.ThingID }, paddingPixels: 180);
        yield return new ScreenshotStep("native ladder works before thin edges", new[] { pawn.ThingID, counterpart.ThingID }, paddingPixels: 180);
        yield return new FloatMenuActionStep("return through native ladder", pawn.ThingID, counterpart.ThingID, Travel(context, counterpart));
        yield return new TimeControlActionStep("run return control", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep("control returns to source level", _ => OnBandOf(stairs),
            new EndToEndDeadline(2_400, 3_500, TimeSpan.FromSeconds(70)));
        yield return new TimeControlActionStep("pause before thin wall construction", true, EndToEndGameSpeed.Normal);
        CameraJumper.TryJump(pawn);
        var build = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(Array.Empty<string>(), new[] { "Structure" })
            .Single(g => !g.Disabled && g.BuildableDefName == ThinWallUtility.ThinWallDefName && g.Interaction == EndToEndGizmoInteraction.Drag);
        IntVec3 edgeCell = origin + new IntVec3(-8, 0, 4);
        yield return new CameraActionStep("frame pawn, stairs and distant wall site", new[] { pawn.ThingID, stairs.ThingID }, paddingPixels: 200);
        yield return new ScreenshotStep("before building a distant thin edge", new[] { pawn.ThingID, stairs.ThingID }, paddingPixels: 200);
        yield return new GizmoActionStep("build a distant thin wall with the native architect tool", Array.Empty<string>(), build.RuntimeType,
            EndToEndGizmoInteraction.Drag, EndToEndCardinalRotation.North, new EndToEndBuildMaterial("Steel"),
            stableGizmoId: build.StableId, startCell: new EndToEndMapCell(edgeCell.x, edgeCell.z),
            endCell: new EndToEndMapCell(edgeCell.x, edgeCell.z), architectCategoryDefNames: new[] { "Structure" });
        yield return new AssertionStep("distant thin wall exists", _ =>
        {
            Thing wall = edgeCell.GetThingList(map).OfType<Building_ThinWall>().Single();
            fixtures.Add(wall);
            var local = map.GetComponent<Pathing.ThinWallMapComponent>();
            EndToEndAssert.Equal(1, local.OwnedEdgeCount, "ASAB map size does not grow the owned edge index.");
            EndToEndAssert.Equal(6, local.MaskedCellCount, "One wall retains only its six incident mask cells on the multilevel map.");
        });
        yield return new ScreenshotStep("after native thin wall construction before cross-level order",
            new[] { pawn.ThingID, stairs.ThingID, fixtures.Last().ThingID }, paddingPixels: 180);
        yield return new AssertionStep("native cross-band reachability survives thin edges", _ =>
            EndToEndAssert.True(pawn.CanReach(destination, PathEndMode.OnCell, Danger.Deadly), "A distant Thin Wall must not erase native cross-band RegionLinks."));
        // View setup only: ASAB deliberately redirects remote clicks unless their band is visible.
        // Native CameraJumper invokes ASAB's ordinary band-switch hook; it does not move the pawn.
        CameraJumper.TryJump(destination);
        yield return new CameraActionStep("show the other level before issuing its order", new[] { destination.ThingID, counterpart.ThingID }, paddingPixels: 180);
        yield return new FloatMenuActionStep("order Go here to the visible other level with a distant thin wall", pawn.ThingID, destination.ThingID, GoHere(context));
        yield return new TimeControlActionStep("run cross-level native movement", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep("pawn arrives through ASAB stair transit", _ => OnBandOf(counterpart),
            new EndToEndDeadline(2_400, 3_500, TimeSpan.FromSeconds(70)));
        yield return new WaitUntilStep("pawn reaches ordered destination on the other level", _ => pawn.Position == destination.Position,
            new EndToEndDeadline(1_200, 1_800, TimeSpan.FromSeconds(30)));
        yield return new TimeControlActionStep("pause after arrival approach", true, EndToEndGameSpeed.Normal);
        yield return new CameraActionStep("frame arrived pawn and destination stairs", new[] { pawn.ThingID, counterpart.ThingID }, paddingPixels: 180);
        yield return new ScreenshotStep("native pawn arrival on the other level", new[] { pawn.ThingID, counterpart.ThingID }, paddingPixels: 180);
        yield return new CheckpointStep("sparse state after actual multilevel movement", _ => new Dictionary<string, string>
        {
            ["mapCells"] = map.cellIndices.NumGridCells.ToString(),
            ["ownedEdges"] = map.GetComponent<Pathing.ThinWallMapComponent>().OwnedEdgeCount.ToString(),
            ["maskCells"] = map.GetComponent<Pathing.ThinWallMapComponent>().MaskedCellCount.ToString(),
            ["removedNativeCells"] = map.GetComponent<Pathing.ThinWallMapComponent>().RemovedNativeCellCount.ToString(),
            ["temporaryRequestBuffers"] = map.GetComponent<Pathing.ThinWallMapComponent>().RequestSnapshotCount.ToString()
        });
        yield return new AssertionStep("return ladder is enabled from the outside position before enclosure", _ => Travel(context, counterpart));

        // Enclose the remote stair with native designations; no synthetic reachability state.
        foreach (ThinWallSide side in Enum.GetValues(typeof(ThinWallSide)))
        {
            yield return new GizmoActionStep("enclose the remote stair " + side, Array.Empty<string>(), build.RuntimeType,
                EndToEndGizmoInteraction.Drag, (EndToEndCardinalRotation)(int)side, new EndToEndBuildMaterial("Steel"),
                stableGizmoId: build.StableId, startCell: new EndToEndMapCell(counterpart.Position.x, counterpart.Position.z),
                endCell: new EndToEndMapCell(counterpart.Position.x, counterpart.Position.z), architectCategoryDefNames: new[] { "Structure" });
        }
        yield return new AssertionStep("enclosed stair no longer offers a cross-level move", _ =>
        {
            var enclosure = counterpart.Position.GetThingList(map).OfType<Building_ThinWall>().ToArray();
            fixtures.AddRange(enclosure);
            EndToEndAssert.Equal(4, enclosure.Select(w => w.Rotation.AsInt).Distinct().Count(),
                "All four distinct completed enclosure sides must exist.");
            EndToEndAssert.False(pawn.CanReach(stairs, PathEndMode.OnCell, Danger.Deadly),
                "Cross-band native reachability must reject an enclosed stair entrance.");
            var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>().Query(pawn.ThingID, counterpart.ThingID);
            var travel = options.Where(o => IsTravel(o.Label)).ToArray();
            EndToEndAssert.True(travel.Length == 1 && travel[0].Disabled &&
                travel[0].Label.IndexOf("no path", StringComparison.OrdinalIgnoreCase) >= 0,
                "The same native ladder command must remain present but report No path after enclosing thin edges; got " +
                string.Join(" | ", options.Select(o => o.Label + " disabled=" + o.Disabled)));
        });
        yield return new ScreenshotStep("thin enclosure blocks the return stair", new[] { pawn.ThingID, counterpart.ThingID }, paddingPixels: 180);
        yield return new CheckpointStep("installed ASAB routing evidence", _ => new Dictionary<string, string>
        {
            ["mapSize"] = map.Size.ToString(), ["source"] = stairs.Position.ToString(),
            ["destination"] = destination.Position.ToString(), ["arrivedPawn"] = pawn.Position.ToString(),
            ["asabAssembly"] = stairs.GetType().Assembly.ManifestModule.ModuleVersionId.ToString()
        });
    }

    private bool OnBandOf(Thing target)
    {
        MapComponent bands = map.components.Single(c => c.GetType().FullName == "AsAboveSoBelow.ABBandMap");
        var bandOf = bands.GetType().GetMethod("BandOf");
        return (int)bandOf!.Invoke(bands, new object[] { pawn.Position }) ==
            (int)bandOf.Invoke(bands, new object[] { target.Position });
    }

    private static bool IsTravel(string label) => label.IndexOf("ladder", StringComparison.OrdinalIgnoreCase) >= 0;

    private string Travel(IEndToEndContext context, Thing target)
    {
        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>().Query(pawn.ThingID, target.ThingID);
        var enabled = options.Where(o => !o.Disabled && IsTravel(o.Label)).ToArray();
        EndToEndAssert.True(enabled.Length == 1, "Expected one enabled native ladder command; got " +
            string.Join(" | ", options.Select(o => o.Label + " disabled=" + o.Disabled)));
        return enabled[0].StableId;
    }

    private string GoHere(IEndToEndContext context)
    {
        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>().Query(pawn.ThingID, destination.ThingID);
        var enabled = options.Where(o => !o.Disabled && o.Label.IndexOf("go here", StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        EndToEndAssert.True(enabled.Length == 1, "Expected enabled cross-level Go here; got " + string.Join(" | ", options.Select(o => o.Label + " disabled=" + o.Disabled)));
        return enabled[0].StableId;
    }
}
