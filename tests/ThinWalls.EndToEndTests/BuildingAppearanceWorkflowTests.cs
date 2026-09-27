using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using HarmonyLib;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using ThinWalls.Rendering;
using UnityEngine;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest("thin-walls.building-appearance-controls", "fumblesneeze.thinwalls",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "fumblesneeze.thinwalls",
    MaxFrames = 18000, MaxGameTicks = 12000, MaxWallClockSeconds = 360)]
public sealed class BuildingAppearanceWorkflowTest : IRimWorldEndToEndTest
{
    private Building shelf = null!;
    private Building neighbor = null!;
    private Building bench = null!;
    private Building casket = null!;
    private Building enclosed = null!;
    private readonly List<string> ids = new();
    private readonly List<Thing> walls = new();
    private IntVec3 center;
    private Map map = null!;

    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap;
        center = map.Center;
        BuildingAppearanceFixture.NormalizeNoonWithCleanup(context, map);
        var originalTerrain = CellRect.CenteredOn(center, 10).ToDictionary(cell => cell, cell => map.terrainGrid.TerrainAt(cell));
        context.DeferCleanup(() =>
        {
            foreach (var item in originalTerrain) Find.CurrentMap.terrainGrid.SetTerrain(item.Key, item.Value);
        });
        foreach (IntVec3 cell in originalTerrain.Keys) map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
        shelf = Spawn("ShelfSmall", "WoodLog", -4, 2);
        neighbor = Spawn("ShelfSmall", "WoodLog", 0, 2);
        bench = Spawn("HandTailoringBench", "Steel", 4, 2);
        casket = Spawn("CryptosleepCasket", null, 1, -4);
        enclosed = Spawn("ShelfSmall", "WoodLog", -4, -4);
        foreach (Rot4 side in new[] { Rot4.North, Rot4.East, Rot4.West })
        {
            Thing wall = ThingMaker.MakeThing(ThingDef.Named("TW_ThinWall"), ThingDef.Named("BlocksGranite"));
            wall.SetFaction(Faction.OfPlayer);
            GenSpawn.Spawn(wall, enclosed.Position, map, side);
            walls.Add(wall);
            ids.Add(wall.ThingID);
        }
        bool originalShrink = ThinWallsMod.Settings.ShowShrinkGizmo;
        bool originalOffset = ThinWallsMod.Settings.ShowOffsetGizmo;
        bool originalGod = DebugSettings.godMode;
        ThinWallsMod.Settings.ShowShrinkGizmo = ThinWallsMod.Settings.ShowOffsetGizmo = true;
        context.DeferCleanup(() =>
        {
            ThinWallsMod.Settings.ShowShrinkGizmo = originalShrink;
            ThinWallsMod.Settings.ShowOffsetGizmo = originalOffset;
            DebugSettings.godMode = originalGod;
            foreach (Thing thing in Find.CurrentMap.listerThings.AllThings.Where(x => ids.Contains(x.ThingID)).ToArray())
                if (thing.Spawned) thing.Destroy(DestroyMode.Vanish);
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep("pause cosmetic fixture", true, EndToEndGameSpeed.Normal);
        yield return new CameraActionStep("frame native furniture comparators", ids, 100);
        yield return new SelectionActionStep("select shelf", new[] { shelf.ThingID }, false);
        yield return Shot("before appearance controls at 100 percent");
        Vector3[] shelfVertices = PrintVertices(shelf);
        Vector3[] neighborVertices = PrintVertices(neighbor);
        IntVec3 shelfPosition = shelf.Position;
        CellRect footprint = shelf.OccupiedRect();
        int hp = shelf.HitPoints;
        foreach (int expected in new[] { 90, 80, 70, 60, 50, 100 })
        {
            yield return Click(context, shelf, true);
            yield return new AssertionStep("scale " + expected + " preserves game state and neighboring graphics", _ =>
            {
                EndToEndAssert.Equal(expected, BuildingAppearanceControls.Get(shelf).ScalePercent, "Wrong scale cycle.");
                EndToEndAssert.Equal(shelfPosition, shelf.Position, "Cosmetic control moved logical position.");
                EndToEndAssert.Equal(footprint, shelf.OccupiedRect(), "Cosmetic control changed footprint.");
                EndToEndAssert.Equal(hp, shelf.HitPoints, "Cosmetic control changed hit points.");
                VerifyVertices(shelf, shelfVertices);
                AssertSame(neighborVertices, PrintVertices(neighbor));
            });
            yield return Shot("shelf native shrink " + expected);
        }
        for (int expected = 1; expected <= 9; expected++)
        {
            yield return Click(context, shelf, false);
            int offsetStep = expected % 9;
            yield return new AssertionStep("independent native offset step " + offsetStep, _ =>
            {
                EndToEndAssert.Equal(offsetStep, BuildingAppearanceControls.Get(shelf).OffsetStep, "Wrong offset cycle.");
                VerifyVertices(shelf, shelfVertices);
            });
            if (expected is 1 or 3 or 5 or 7 or 9) yield return Shot("native offset " + offsetStep);
        }

        for (int rotation = 0; rotation < 4; rotation++)
        {
            // Rotation is only a fixed comparator setup; the tested scale/offset changes use native gizmos.
            IntVec3 benchCell = bench.Position;
            bench.DeSpawn();
            GenSpawn.Spawn(bench, benchCell, map, new Rot4(rotation));
            Vector3[] before = PrintVertices(bench);
            IntVec3 interaction = bench.InteractionCell;
            yield return new SelectionActionStep("select rotated metal workbench", new[] { bench.ThingID }, false);
            yield return Click(context, bench, true);
            yield return Click(context, bench, false);
            yield return new AssertionStep("workbench transform preserves native interaction " + rotation, _ =>
            {
                EndToEndAssert.Equal(interaction, bench.InteractionCell, "Moved interaction spot.");
                VerifyVertices(bench, before);
            });
            yield return Shot("workbench rotation " + rotation + " at 90 percent offset north");
            for (int i = 0; i < 5; i++) yield return Click(context, bench, true);
            for (int i = 0; i < 8; i++) yield return Click(context, bench, false);
        }

        yield return new SelectionActionStep("select realtime casket", new[] { casket.ThingID }, false);
        yield return Shot("native realtime casket before");
        for (int i = 0; i < 5; i++) yield return Click(context, casket, true);
        for (int i = 0; i < 3; i++) yield return Click(context, casket, false);
        yield return Shot("native realtime casket at 50 percent offset east");
        yield return new SelectionActionStep("select shelf inside thin wall U", new[] { enclosed.ThingID }, false);
        yield return Shot("compact U before manual fit");
        for (int i = 0; i < 5; i++) yield return Click(context, enclosed, true);
        yield return Shot("compact U shelf at user selected 50 percent");

        for (int i = 0; i < 3; i++) yield return Click(context, shelf, true);
        yield return Click(context, shelf, false);
        yield return new SupportingHitPointFixtureActionStep("diagnostic damaged shelf precondition",
            new[] { new EndToEndHitPointFixture(shelf.ThingID, 0.4f) });
        yield return Shot("adjusted damaged shelf before save");
        string shelfId = shelf.ThingID, casketId = casket.ThingID, enclosedId = enclosed.ThingID;
        string saveName = "thin-walls-appearance-controls";
        context.DeferCleanup(() => { string path = GenFilePaths.FilePathForSavedGame(saveName); if (File.Exists(path)) File.Delete(path); });
        yield return new SaveLoadActionStep("native save and reload adjusted furniture", saveName);
        yield return new AssertionStep("save restores per building state not shared Def state", _ =>
        {
            map = Find.CurrentMap;
            shelf = (Building)map.listerThings.AllThings.Single(x => x.ThingID == shelfId);
            casket = (Building)map.listerThings.AllThings.Single(x => x.ThingID == casketId);
            enclosed = (Building)map.listerThings.AllThings.Single(x => x.ThingID == enclosedId);
            EndToEndAssert.Equal(70, BuildingAppearanceControls.Get(shelf).ScalePercent, "Lost shelf size on load.");
            EndToEndAssert.Equal(1, BuildingAppearanceControls.Get(shelf).OffsetStep, "Lost shelf offset on load.");
            EndToEndAssert.Equal(50, BuildingAppearanceControls.Get(casket).ScalePercent, "Lost realtime size.");
            EndToEndAssert.Equal(3, BuildingAppearanceControls.Get(casket).OffsetStep, "Lost realtime offset.");
            EndToEndAssert.Equal(50, BuildingAppearanceControls.Get(enclosed).ScalePercent, "Lost U shelf size.");
        });
        yield return new CameraActionStep("frame persistent adjusted objects", ids, 100);
        yield return new SelectionActionStep("select reloaded shelf", new[] { shelf.ThingID }, false);
        yield return Shot("native reload retains 70 percent north offset");

        DebugSettings.godMode = true; // Explicit instant-uninstall developer-mode precondition.
        var uninstall = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { shelf.ThingID }, Array.Empty<string>())
            .Single(x => x.Label == "DesignatorUninstall".Translate().ToString() && !x.Disabled);
        yield return new GizmoActionStep("native uninstall adjusted shelf", new[] { shelf.ThingID }, uninstall.RuntimeType,
            EndToEndGizmoInteraction.Invoke, uninstall.StableId);
        MinifiedThing packed = map.listerThings.AllThings.OfType<MinifiedThing>().Single(x => x.InnerThing == shelf);
        ids.Add(packed.ThingID);
        yield return new SelectionActionStep("select packed adjusted shelf", new[] { packed.ThingID }, false);
        yield return new AssertionStep("packed object keeps saved choices but not old map transform", _ =>
        {
            EndToEndAssert.Equal(70, BuildingAppearanceControls.Get(shelf).ScalePercent, "Packed size was lost.");
            EndToEndAssert.True(BuildingAppearanceControls.ForRendering(shelf).IsDefault, "Old map pivot still active while packed.");
        });
        yield return Shot("packed shelf retains native crate rendering");
        DebugSettings.godMode = false;
        var install = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { packed.ThingID }, Array.Empty<string>())
            .Single(x => x.RuntimeType == typeof(Designator_Install).FullName && !x.Disabled);
        IntVec3 reinstallCell = center + new IntVec3(-6, 0, 2);
        yield return new GizmoActionStep("native reinstall adjusted shelf at a different cell", new[] { packed.ThingID },
            install.RuntimeType, EndToEndGizmoInteraction.Place, EndToEndCardinalRotation.North, install.StableId,
            new EndToEndMapCell(reinstallCell.x, reinstallCell.z));
        Pawn builder = CreateBuilder();
        GenSpawn.Spawn(builder, center + new IntVec3(-6, 0, 0), map);
        ids.Add(builder.ThingID);
        yield return new TimeControlActionStep("ordinary construction installs the packed shelf", false, EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep("same adjusted shelf is installed by pawn work", _ => shelf.Spawned && shelf.Position == reinstallCell,
            new EndToEndDeadline(3600, 6000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep("pause after native reinstallation", true, EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep("select reinstalled shelf", new[] { shelf.ThingID }, false);
        yield return new AssertionStep("reinstallation retains original appearance identity", _ =>
        {
            EndToEndAssert.Equal(70, BuildingAppearanceControls.Get(shelf).ScalePercent, "Reinstall lost size.");
            EndToEndAssert.Equal(1, BuildingAppearanceControls.Get(shelf).OffsetStep, "Reinstall lost offset.");
        });
        ids.Remove(packed.ThingID);
        yield return Shot("pawn reinstalled same shelf with preserved 70 percent north offset");

        yield return new ModSettingsActionStep("open native Thin Walls mod options", ThinWallsMod.PackageId);
        yield return Shot("native mod options expose both independent visibility choices");
        yield return new WindowCancelActionStep("close settings", "RimWorld.Dialog_ModSettings");
        // Explicit configuration preconditions, not a claim of an automated checkbox click.
        ThinWallsMod.Settings.ShowShrinkGizmo = false;
        yield return new AssertionStep("hidden shrink leaves independent offset and saved state", _ =>
        {
            var options = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { shelf.ThingID }, Array.Empty<string>());
            EndToEndAssert.False(options.Any(x => x.RuntimeType == typeof(Command_ShrinkBuilding).FullName), "Shrink gizmo still visible.");
            EndToEndAssert.True(options.Any(x => x.RuntimeType == typeof(Command_OffsetBuilding).FullName), "Offset disappeared too.");
            EndToEndAssert.Equal(70, BuildingAppearanceControls.Get(shelf).ScalePercent, "Hide reset state.");
        });
        yield return Shot("shrink hidden offset remains");
        ThinWallsMod.Settings.ShowOffsetGizmo = false;
        yield return Shot("both appearance gizmos hidden but adjustments persist");
        ThinWallsMod.Settings.ShowShrinkGizmo = ThinWallsMod.Settings.ShowOffsetGizmo = true;
        yield return new SelectionActionStep("clear selection for identity views", Array.Empty<string>(), false);
        yield return Shot("ordinary zoom adjusted native objects");
        yield return new CameraActionStep("close detail adjusted native objects", new[] { enclosed.ThingID, casket.ThingID }, 75);
        yield return Shot("close adjusted native objects");
        yield return new CameraActionStep("far useful zoom adjusted native objects", ids, 280);
        yield return Shot("far adjusted native objects");
        foreach (var step in ThinWallNativeLogEvidence.Capture(context)) yield return step;
    }

    private Building Spawn(string defName, string? stuffName, int x, int z)
    {
        var thing = (Building)ThingMaker.MakeThing(ThingDef.Named(defName), stuffName == null ? null : ThingDef.Named(stuffName));
        thing.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(thing, center + new IntVec3(x, 0, z), map);
        ids.Add(thing.ThingID);
        return thing;
    }

    internal static Pawn CreateBuilder()
    {
        for (int attempt = 0; attempt < 64; attempt++)
        {
            Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
            if (pawn.WorkTypeIsDisabled(WorkTypeDefOf.Construction)) { pawn.Destroy(DestroyMode.Vanish); continue; }
            pawn.workSettings.EnableAndInitialize();
            foreach (WorkTypeDef work in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                if (!pawn.WorkTypeIsDisabled(work)) pawn.workSettings.SetPriority(work, 0);
            pawn.workSettings.SetPriority(WorkTypeDefOf.Construction, 1);
            pawn.skills.GetSkill(SkillDefOf.Construction).Level = 20;
            if (pawn.needs?.food != null) pawn.needs.food.CurLevelPercentage = 1;
            if (pawn.needs?.rest != null) pawn.needs.rest.CurLevelPercentage = 1;
            return pawn;
        }
        throw new EndToEndAssertionException("No capable fixture builder.");
    }

    internal static GizmoActionStep Click(IEndToEndContext context, Building target, bool shrink)
    {
        string type = shrink ? typeof(Command_ShrinkBuilding).FullName! : typeof(Command_OffsetBuilding).FullName!;
        var option = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { target.ThingID }, Array.Empty<string>())
            .Single(x => x.RuntimeType == type && !x.Disabled);
        return new GizmoActionStep("native " + (shrink ? "shrink " : "offset ") + target.ThingID,
            new[] { target.ThingID }, type, EndToEndGizmoInteraction.Invoke, option.StableId);
    }

    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);

    // Diagnostic corroboration: invoke the real patched print seam on a disposable mesh container.
    internal static Vector3[] PrintVertices(Building building)
    {
        var layer = new SectionLayer_ThingsGeneral(building.Map.mapDrawer.SectionAt(building.Position));
        try
        {
            AccessTools.Method(typeof(SectionLayer_ThingsGeneral), "TakePrintFrom").Invoke(layer, new object[] { building });
            return layer.subMeshes.SelectMany(x => x.verts).ToArray();
        }
        finally { layer.Dispose(); }
    }

    internal static void VerifyVertices(Building building, Vector3[] baseline)
    {
        BuildingAppearance appearance = BuildingAppearanceControls.Get(building);
        Vector3 pivot = building.TrueCenter();
        AssertSame(baseline.Select(x => appearance.Transform(x, pivot)).ToArray(), PrintVertices(building));
    }

    private static void AssertSame(Vector3[] expected, Vector3[] actual)
    {
        EndToEndAssert.True(expected.Length > 0 && expected.Length == actual.Length, "Mesh vertex inventory changed.");
        for (int i = 0; i < expected.Length; i++)
            EndToEndAssert.True((expected[i] - actual[i]).sqrMagnitude < .0000001f, "Geometry transform mismatch at " + i);
    }
}
