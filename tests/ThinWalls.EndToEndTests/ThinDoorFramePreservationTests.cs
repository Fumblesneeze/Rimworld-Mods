using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using ThinWalls.Buildings;
using Verse;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest("thin-walls.native-door-frame-preservation",
    "fumblesneeze.thinwalls", "brrainz.harmony", EndToEndTestContract.CorePackageId,
    "fumblesneeze.thinwalls", MaxFrames = 12000, MaxGameTicks = 34000, MaxWallClockSeconds = 300)]
public sealed class ThinDoorFramePreservationTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private IntVec3 cell;
    private Pawn builder = null!;
    private Thing steel = null!;
    private EndToEndGizmoOption build = null!;
    private ThingDef door = null!;

    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap;
        cell = map.Center;
        bool originalGod = DebugSettings.godMode;
        DebugSettings.godMode = false;
        context.DeferCleanup(() => DebugSettings.godMode = originalGod);
        door = DefDatabase<ThingDef>.GetNamed("TW_ThinDoor");
        builder = ThinWallConstructionLifecycleTest.GenerateBuilder();
        GenSpawn.Spawn(builder, cell + IntVec3.South, map);
        steel = ThingMaker.MakeThing(ThingDefOf.Steel);
        steel.stackCount = 26;
        GenSpawn.Spawn(steel, cell + new IntVec3(-2, 0, 0), map);
        build = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(Array.Empty<string>(), new[] { "Structure" })
            .Single(x => x.BuildableDefName == "TW_ThinDoor" && !x.Disabled);
        context.DeferCleanup(() =>
        {
            foreach (Thing thing in cell.GetThingList(map).Where(x => ThinWallUtility.IsThinDoorDef(x.def))
                .Concat(new Thing[] { builder, steel }).ToArray())
                if (thing.Spawned && !thing.Destroyed) thing.Destroy(DestroyMode.Vanish);
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new CameraActionStep("frame builder with exactly two door material costs", new[] { builder.ThingID, steel.ThingID }, 130);
        yield return new ScreenshotStep("before native door construction", Array.Empty<string>(), 0);
        yield return Place(EndToEndCardinalRotation.South);
        Blueprint_Build blueprint = cell.GetThingList(map).OfType<Blueprint_ThinDoor>().Single();
        EndToEndAssert.True(Math.Abs(door.GetStatValueAbstract(StatDefOf.WorkToBuild, blueprint.stuffToUse) - ThingDefOf.Door.GetStatValueAbstract(StatDefOf.WorkToBuild, ThingDefOf.Steel) / 2f) < .001f,
            "Door's native work must remain half the Core counterpart.");
        yield return new TimeControlActionStep("allow ordinary hauling and construction", false, EndToEndGameSpeed.Superfast);
        Frame frame = null!;
        yield return new WaitUntilStep("native door blueprint becomes a funded frame",
            _ => (frame = cell.GetThingList(map).OfType<Frame>().SingleOrDefault(x => x.def == door.frameDef)!) != null &&
                frame.resourceContainer.TotalStackCount == 13,
            new EndToEndDeadline(4500, 12000, TimeSpan.FromSeconds(120)));
        yield return new TimeControlActionStep("pause funded frame before second edge designation", true, EndToEndGameSpeed.Normal);
        string frameId = frame.ThingID;
        int funded = frame.resourceContainer.TotalStackCount;
        EndToEndAssert.Equal(13, funded, "The preserved frame must actually contain its delivered material cost.");
        EndToEndAssert.Equal(Rot4.South, frame.Rotation, "The first frame owns the south edge.");
        yield return new SelectionActionStep("select native door frame", new[] { frameId }, false);
        yield return new ScreenshotStep("first funded frame before the next door edge", Array.Empty<string>(), 0);
        yield return Place(EndToEndCardinalRotation.East);
        yield return new AssertionStep("second edge preserves exact frame and its delivered resources", _ =>
        {
            EndToEndAssert.True(frame.Spawned && !frame.Destroyed, "Different-side door blueprint cancelled the first frame.");
            EndToEndAssert.Equal(frameId, frame.ThingID, "Frame identity changed.");
            EndToEndAssert.Equal(funded, frame.resourceContainer.TotalStackCount, "Delivered frame resources changed.");
            EndToEndAssert.Equal(Rot4.South, frame.Rotation, "Second designation changed first frame rotation.");
            EndToEndAssert.True(cell.GetThingList(map).OfType<Blueprint_ThinDoor>().Any(x => x.Rotation == Rot4.East), "Second blueprint absent.");
        });
        yield return new ScreenshotStep("funded frame and second blueprint together", Array.Empty<string>(), 0);
        yield return new TimeControlActionStep("finish both doors with ordinary Construction jobs", false, EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep("both co-located doors completed by builder",
            _ => cell.GetThingList(map).OfType<Building_ThinDoor>().Count() == 2,
            new EndToEndDeadline(6000, 18000, TimeSpan.FromSeconds(140)));
        yield return new TimeControlActionStep("pause completed co-located doors", true, EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep("select one completed thin door", new[] { cell.GetThingList(map).OfType<Building_ThinDoor>().First().ThingID }, false);
        yield return new ScreenshotStep("two doors completed with their cell still usable", Array.Empty<string>(), 0);
    }

    private GizmoActionStep Place(EndToEndCardinalRotation rotation) => new(
        "native designate Thin Door " + rotation, Array.Empty<string>(), build.RuntimeType,
        EndToEndGizmoInteraction.Drag, rotation, new EndToEndBuildMaterial("Steel"),
        stableGizmoId: build.StableId, startCell: new EndToEndMapCell(cell.x, cell.z),
        endCell: new EndToEndMapCell(cell.x, cell.z), architectCategoryDefNames: new[] { "Structure" });
}
