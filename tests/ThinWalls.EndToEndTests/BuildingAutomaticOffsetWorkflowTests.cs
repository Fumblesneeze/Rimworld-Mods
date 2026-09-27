using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using ThinWalls.Rendering;
using UnityEngine;
using Verse;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest("thin-walls.native-automatic-offset-lifecycle", "fumblesneeze.thinwalls",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "fumblesneeze.thinwalls",
    MaxFrames = 24000, MaxGameTicks = 10000, MaxWallClockSeconds = 480)]
public sealed class BuildingAutomaticOffsetWorkflowTest : IRimWorldEndToEndTest
{
    private static readonly int[] Expected = { 0, 5, 7, 6, 1, 0, 8, 7, 3, 4, 0, 5, 2, 3, 1, 0 };
    private Map map = null!;
    private IntVec3 center;
    private readonly List<string> ids = new();
    private readonly List<Building> shelves = new();
    private EndToEndGizmoOption wallBuild = null!;
    private EndToEndGizmoOption shelfBuild = null!;

    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap;
        center = map.Center;
        BuildingAppearanceFixture.NormalizeNoonWithCleanup(context, map);
        bool oldGod = DebugSettings.godMode;
        bool oldOffset = ThinWallsMod.Settings.ShowOffsetGizmo;
        bool oldShrink = ThinWallsMod.Settings.ShowShrinkGizmo;
        DebugSettings.godMode = true;
        ThinWallsMod.Settings.ShowOffsetGizmo = ThinWallsMod.Settings.ShowShrinkGizmo = true;
        var terrain = CellRect.CenteredOn(center, 12).ToDictionary(c => c, c => c.GetTerrain(map));
        foreach (IntVec3 c in terrain.Keys) map.terrainGrid.SetTerrain(c, TerrainDefOf.Concrete);
        context.DeferCleanup(() =>
        {
            DebugSettings.godMode = oldGod;
            ThinWallsMod.Settings.ShowOffsetGizmo = oldOffset;
            ThinWallsMod.Settings.ShowShrinkGizmo = oldShrink;
            foreach (Thing thing in Find.CurrentMap.listerThings.AllThings.Where(t => ids.Contains(t.ThingID)).ToArray())
                if (thing.Spawned) thing.Destroy(DestroyMode.Vanish);
            foreach (var pair in terrain) Find.CurrentMap.terrainGrid.SetTerrain(pair.Key, pair.Value);
        });
        var catalog = context.GetRequiredService<IEndToEndGizmoCatalog>();
        wallBuild = catalog.Query(Array.Empty<string>(), new[] { "Structure" }).Single(x => x.BuildableDefName == "TW_ThinWall" && !x.Disabled);
        shelfBuild = catalog.Query(Array.Empty<string>(), new[] { "Furniture" }).Single(x => x.BuildableDefName == "ShelfSmall" && !x.Disabled);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep("pause automatic offset catalog", true, EndToEndGameSpeed.Normal);
        for (int mask = 0; mask < 16; mask++)
        {
            IntVec3 cell = center + new IntVec3(-8 + mask % 4 * 5, 0, 8 - mask / 4 * 5);
            yield return Place(shelfBuild, "Furniture", cell, 0);
            Building shelf = cell.GetThingList(map).OfType<Building>().Single(t => t.def.defName == "ShelfSmall");
            shelves.Add(shelf);
            ids.Add(shelf.ThingID);
            Vector3[] baseline = BuildingAppearanceWorkflowTest.PrintVertices(shelf);
            yield return new CameraActionStep("native shelf before walls mask " + mask, new[] { shelf.ThingID }, 160);
            if (mask is 3 or 11) yield return Shot("before native wall placement mask " + mask);
            for (int side = 0; side < 4; side++)
                if ((mask & (1 << side)) != 0)
                {
                    yield return Place(wallBuild, "Structure", cell, side);
                    ids.AddRange(cell.GetThingList(map).Where(t => ThinWallUtility.IsThinEdgeDef(t.def)).Select(t => t.ThingID));
                }
            int expected = Expected[mask];
            yield return new AssertionStep("all cardinal contacts choose preset mask " + mask, _ =>
            {
                AssertAppearance(shelf, expected, false);
                EndToEndAssert.Equal(cell, shelf.Position, "Cosmetic offset moved footprint.");
                EndToEndAssert.True((shelf.DrawPos - shelf.TrueCenter()).sqrMagnitude < .00001f, "Old DrawPos renderer still shifted the building.");
                BuildingAppearanceWorkflowTest.VerifyVertices(shelf, baseline);
            });
            yield return new SelectionActionStep("show resulting native offset gizmo " + mask, new[] { shelf.ThingID }, false);
            yield return Shot("automatic preset for contact mask " + mask);
        }
        yield return new SelectionActionStep("clear selection for contact catalog", Array.Empty<string>(), false);
        yield return new CameraActionStep("all sixteen native wall patterns", shelves.Select(x => x.ThingID), 100);
        yield return Shot("all sixteen automatic offsets");

        Building manual = shelves[3];
        yield return BuildingAppearanceWorkflowTest.Click(context, manual, true);
        yield return new AssertionStep("shrink does not make offset manual", _ => AssertAppearance(manual, 6, false));
        for (int i = 0; i < 3; i++) yield return BuildingAppearanceWorkflowTest.Click(context, manual, false);
        yield return new AssertionStep("center is an explicit manual choice", _ => AssertAppearance(manual, 0, true));
        yield return Place(wallBuild, "Structure", manual.Position, 3);
        ids.AddRange(manual.Position.GetThingList(map).Where(t => ThinWallUtility.IsThinEdgeDef(t.def)).Select(t => t.ThingID));
        yield return new AssertionStep("new U wall preserves explicit center", _ => AssertAppearance(manual, 0, true));
        yield return new CameraActionStep("manual center alongside changed walls", new[] { manual.ThingID }, 160);
        yield return new SelectionActionStep("show manual center", new[] { manual.ThingID }, false);
        yield return Shot("manual center is not overwritten by the new U");

        Building automatic = shelves[0];
        DebugSettings.godMode = false;
        yield return Place(wallBuild, "Structure", automatic.Position, 0);
        Thing blueprint = automatic.Position.GetThingList(map).Single(t => t is Blueprint && ThinWallUtility.IsThinEdgeDef(t.def.entityDefToBuild as ThingDef));
        ids.Add(blueprint.ThingID);
        yield return new AssertionStep("planning a wall refreshes automatically", _ => AssertAppearance(automatic, 5, false));
        yield return new CameraActionStep("planned wall and automatic shelf", new[] { automatic.ThingID }, 160);
        yield return Shot("blueprint moves automatic shelf south");
        var cancel = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { blueprint.ThingID }, Array.Empty<string>())
            .Single(x => x.Interaction == EndToEndGizmoInteraction.Invoke && x.Label == "DesignatorCancel".Translate().ToString() && !x.Disabled);
        yield return new GizmoActionStep("native cancel wall blueprint", new[] { blueprint.ThingID }, cancel.RuntimeType,
            EndToEndGizmoInteraction.Invoke, cancel.StableId);
        yield return new AssertionStep("cancelling removes the automatic offset", _ => AssertAppearance(automatic, 0, false));
        yield return Shot("cancelled wall returns shelf to center");

        ThinWallsMod.Settings.ShowOffsetGizmo = ThinWallsMod.Settings.ShowShrinkGizmo = false;
        DebugSettings.godMode = true;
        yield return Place(wallBuild, "Structure", automatic.Position, 2);
        ids.AddRange(automatic.Position.GetThingList(map).Where(t => ThinWallUtility.IsThinEdgeDef(t.def)).Select(t => t.ThingID));
        yield return new AssertionStep("hidden gizmos do not suppress automatic placement", _ => AssertAppearance(automatic, 1, false));
        yield return Shot("automatic north offset with gizmos hidden");
        ThinWallsMod.Settings.ShowOffsetGizmo = ThinWallsMod.Settings.ShowShrinkGizmo = true;

        string manualId = manual.ThingID, automaticId = automatic.ThingID;
        const string save = "thin-walls-unified-offset-acceptance";
        context.DeferCleanup(() => { string path = GenFilePaths.FilePathForSavedGame(save); if (File.Exists(path)) File.Delete(path); });
        yield return new SaveLoadActionStep("native save and load manual and automatic modes", save);
        map = Find.CurrentMap;
        manual = (Building)map.listerThings.AllThings.Single(x => x.ThingID == manualId);
        automatic = (Building)map.listerThings.AllThings.Single(x => x.ThingID == automaticId);
        yield return new AssertionStep("save load preserves manual center and recomputes auto", _ =>
        {
            AssertAppearance(manual, 0, true);
            AssertAppearance(automatic, 1, false);
            EndToEndAssert.Equal(90, BuildingAppearanceControls.Get(manual).ScalePercent, "Lost independent shrink.");
        });
        yield return new SelectionActionStep("show reloaded automatic building", new[] { automatic.ThingID }, false);
        yield return Shot("automatic mode after native reload");

        // Neutral ownership is a disposable precondition; the result is produced by native Claim.
        automatic.SetFaction(null);
        yield return new AssertionStep("losing eligibility clears automatic displacement", _ => AssertAppearance(automatic, 0, false));
        yield return Shot("neutral shelf before native claim");
        var claim = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { automatic.ThingID }, Array.Empty<string>())
            .Single(x => x.Interaction == EndToEndGizmoInteraction.Invoke && x.Label == "DesignatorClaim".Translate().ToString() && !x.Disabled);
        yield return new GizmoActionStep("native Claim assigns automatic preset", new[] { automatic.ThingID }, claim.RuntimeType,
            EndToEndGizmoInteraction.Invoke, claim.StableId);
        yield return new AssertionStep("claim refreshes without respawn or wall change", _ => AssertAppearance(automatic, 1, false));
        yield return Shot("claimed shelf automatically offset north");

        DebugSettings.godMode = true;
        Thing completedWall = automatic.Position.GetThingList(map).Single(t => ThinWallUtility.IsThinEdgeDef(t.def));
        var deconstruct = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { completedWall.ThingID }, Array.Empty<string>())
            .Single(x => x.Interaction == EndToEndGizmoInteraction.Invoke && x.Label == "DesignatorDeconstruct".Translate().ToString() && !x.Disabled);
        yield return new GizmoActionStep("native deconstruct completed adjacent wall", new[] { completedWall.ThingID }, deconstruct.RuntimeType,
            EndToEndGizmoInteraction.Invoke, deconstruct.StableId);
        yield return new AssertionStep("completed wall removal recenters automatic shelf", _ =>
        {
            EndToEndAssert.False(completedWall.Spawned, "Native god-mode deconstruction did not remove the wall.");
            AssertAppearance(automatic, 0, false);
        });
        yield return Shot("completed wall deconstruction recenters shelf");
        yield return Place(wallBuild, "Structure", automatic.Position, 1);
        ids.AddRange(automatic.Position.GetThingList(map).Where(t => ThinWallUtility.IsThinEdgeDef(t.def)).Select(t => t.ThingID));
        yield return new AssertionStep("replacement east wall assigns automatic west", _ => AssertAppearance(automatic, 7, false));
        yield return Shot("west offset before native uninstall");
        var uninstall = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { automatic.ThingID }, Array.Empty<string>())
            .Single(x => x.Label == "DesignatorUninstall".Translate().ToString() && !x.Disabled);
        yield return new GizmoActionStep("native uninstall automatic shelf", new[] { automatic.ThingID }, uninstall.RuntimeType,
            EndToEndGizmoInteraction.Invoke, uninstall.StableId);
        MinifiedThing packed = map.listerThings.AllThings.OfType<MinifiedThing>().Single(x => x.InnerThing == automatic);
        ids.Add(packed.ThingID);
        yield return new AssertionStep("packed auto shelf has no obsolete map transform", _ =>
            EndToEndAssert.True(BuildingAppearanceControls.ForRendering(automatic).IsDefault, "Packed rendering retained map displacement."));
        yield return new SelectionActionStep("show packed automatic shelf", new[] { packed.ThingID }, false);
        yield return Shot("packed auto shelf before reinstall");
        DebugSettings.godMode = false;
        var install = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { packed.ThingID }, Array.Empty<string>())
            .Single(x => x.RuntimeType == typeof(Designator_Install).FullName && !x.Disabled);
        IntVec3 destination = center + new IntVec3(10, 0, -10);
        yield return new GizmoActionStep("native reinstall automatic shelf away from walls", new[] { packed.ThingID },
            install.RuntimeType, EndToEndGizmoInteraction.Place, EndToEndCardinalRotation.North, install.StableId,
            new EndToEndMapCell(destination.x, destination.z));
        Pawn builder = BuildingAppearanceWorkflowTest.CreateBuilder();
        GenSpawn.Spawn(builder, destination + IntVec3.South, map);
        ids.Add(builder.ThingID);
        yield return new TimeControlActionStep("pawn performs native reinstall", false, EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep("automatic shelf reinstalled by construction job", _ => automatic.Spawned && automatic.Position == destination,
            new EndToEndDeadline(3600, 6000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep("pause reinstalled shelf", true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep("automatic reinstall resolves new empty perimeter", _ => AssertAppearance(automatic, 0, false));
        yield return new CameraActionStep("reinstalled auto shelf", new[] { automatic.ThingID }, 160);
        yield return new SelectionActionStep("show reinstalled automatic center", new[] { automatic.ThingID }, false);
        yield return Shot("same shelf reinstalled with automatic center");
        foreach (var step in ThinWallNativeLogEvidence.Capture(context)) yield return step;
    }

    private static void AssertAppearance(Building building, int offset, bool manual)
    {
        var appearance = BuildingAppearanceControls.Get(building);
        EndToEndAssert.Equal(offset, appearance.OffsetStep, "Wrong shared offset preset.");
        EndToEndAssert.Equal(manual, appearance.OffsetIsManual, "Wrong automatic/manual mode.");
    }

    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private static GizmoActionStep Place(EndToEndGizmoOption option, string category, IntVec3 cell, int rotation) => new(
        "native Architect place " + option.BuildableDefName + " side " + rotation, Array.Empty<string>(), option.RuntimeType,
        option.Interaction!.Value, (EndToEndCardinalRotation)rotation, new EndToEndBuildMaterial("WoodLog"),
        stableGizmoId: option.StableId, startCell: new EndToEndMapCell(cell.x, cell.z),
        endCell: new EndToEndMapCell(cell.x, cell.z), architectCategoryDefNames: new[] { category });
}
