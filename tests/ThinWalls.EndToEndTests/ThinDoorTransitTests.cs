using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using ThinWalls.Buildings;
using Verse;
using Verse.AI;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest("thin-walls.native-safe-destination-through-warm-door-room", "fumblesneeze.thinwalls",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "fumblesneeze.thinwalls",
    MaxFrames = 3_600, MaxGameTicks = 6_000, MaxWallClockSeconds = 140)]
public sealed class ThinDoorTransitTest : IRimWorldEndToEndTest
{
    private readonly List<Thing> fixtures = new();
    private Map map = null!;
    private Pawn pawn = null!;
    private Thing target = null!;
    private IntVec3 root;
    private bool god;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        root = map.Center;
        foreach (IntVec3 cell in new CellRect(root.x - 1, root.z - 2, 11, 5).Cells)
        {
            map.roofGrid.SetRoof(cell, null);
            foreach (Thing thing in cell.GetThingList(map).ToArray()) if (thing.def.destroyable) thing.Destroy(DestroyMode.Vanish);
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
            map.fogGrid.Unfog(cell);
        }
        foreach (IntVec3 cell in new CellRect(root.x, root.z - 1, 9, 3).EdgeCells) Spawn(ThingDefOf.Wall, cell);
        Spawn(ThingDefOf.Door, root + IntVec3.East * 2);
        pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        pawn.Name = new NameTriple("Warm", "Transit Tester", "Test");
        GenSpawn.Spawn(pawn, root + IntVec3.East, map);
        pawn.drafter.Drafted = true;
        fixtures.Add(pawn);
        target = ThingMaker.MakeThing(ThingDefOf.WoodLog);
        GenSpawn.Spawn(target, root + IntVec3.East * 7, map);
        fixtures.Add(target);
        god = DebugSettings.godMode;
        DebugSettings.godMode = true;
        context.DeferCleanup(() =>
        {
            DebugSettings.godMode = god;
            foreach (IntVec3 cell in new CellRect(root.x, root.z - 1, 9, 3).Cells) map.roofGrid.SetRoof(cell, null);
            foreach (Thing thing in fixtures.ToArray()) if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish);
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var build = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(Array.Empty<string>(), new[] { "Structure" })
            .Single(g => !g.Disabled && g.BuildableDefName == ThinWallUtility.ThinDoorDefName && g.Interaction == EndToEndGizmoInteraction.Drag);
        yield return new GizmoActionStep("build the thin exit door with the native architect tool", Array.Empty<string>(), build.RuntimeType,
            EndToEndGizmoInteraction.Drag, EndToEndCardinalRotation.East, new EndToEndBuildMaterial("Steel"),
            stableGizmoId: build.StableId, startCell: new EndToEndMapCell(root.x + 5, root.z),
            endCell: new EndToEndMapCell(root.x + 5, root.z), architectCategoryDefNames: new[] { "Structure" });
        fixtures.AddRange((root + IntVec3.East * 5).GetThingList(map).OfType<Building_ThinDoor>());
        yield return new AssertionStep("safe final destination admits a moderately dangerous transit room", _ =>
        {
            foreach (IntVec3 cell in new CellRect(root.x, root.z - 1, 9, 3).Cells) map.roofGrid.SetRoof(cell, RoofDefOf.RoofConstructed);
            Room warm = (root + IntVec3.East * 4).GetRoom(map);
            Room start = pawn.Position.GetRoom(map);
            Room finish = target.Position.GetRoom(map);
            EndToEndAssert.False(ReferenceEquals(warm, start) || ReferenceEquals(warm, finish), "Both doors must divide their native rooms.");
            start.Temperature = finish.Temperature = pawn.SafeTemperatureRange().Average;
            warm.Temperature = pawn.SafeTemperatureRange().max + 20f;
            Region transit = (root + IntVec3.East * 5).GetRegion(map);
            TraverseParms parms = TraverseParms.For(pawn, Danger.None);
            EndToEndAssert.True(transit.Allows(parms, isDestination: false), "Core allows moderately dangerous transit.");
            EndToEndAssert.False(transit.Allows(parms, isDestination: true), "Core refuses stopping in that room under Danger.None.");
            EndToEndAssert.True(map.reachability.CanReach(pawn.Position, target.Position, PathEndMode.OnCell, parms),
                "A synthetic Thin Door endpoint is transit, not the player's final destination.");
        });
        yield return new CameraActionStep("frame the regular-door and thin-door route", fixtures.Select(t => t.ThingID), paddingPixels: 150);
        yield return new ScreenshotStep("before the native route through both doors", fixtures.Select(t => t.ThingID), paddingPixels: 150);
        var move = context.GetRequiredService<IEndToEndFloatMenuCatalog>().Query(pawn.ThingID, target.ThingID)
            .Single(g => !g.Disabled && g.Label.IndexOf("go here", StringComparison.OrdinalIgnoreCase) >= 0);
        yield return new FloatMenuActionStep("order the native route through regular and thin doors", pawn.ThingID, target.ThingID, move.StableId);
        yield return new TimeControlActionStep("run both native door interactions", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep("pawn reaches the safe far room", _ => pawn.Position == target.Position,
            new EndToEndDeadline(2_400, 4_000, TimeSpan.FromSeconds(80)));
        yield return new TimeControlActionStep("pause at the safe destination", true, EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep("after native movement through both doors", fixtures.Select(t => t.ThingID), paddingPixels: 150);
    }

    private void Spawn(ThingDef def, IntVec3 cell)
    {
        Thing thing = ThingMaker.MakeThing(def, ThingDefOf.Steel);
        thing.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(thing, cell, map);
        fixtures.Add(thing);
    }
}
