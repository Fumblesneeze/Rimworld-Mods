using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;

namespace ThinWalls.EndToEndTests;

[RimWorldEndToEndTest("thin-walls.native-pointer-hitboxes", "fumblesneeze.thinwalls",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "fumblesneeze.thinwalls",
    MaxFrames = 18000, MaxGameTicks = 1000, MaxWallClockSeconds = 360)]
public sealed class ThinWallPointerSelectionWorkflowTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private IntVec3 cell;
    private Pawn portraitPawn = null!;
    private readonly List<Thing> fixtures = new();

    public void Arrange(IEndToEndContext context)
    {
        map = Find.CurrentMap;
        cell = map.Center;
        BuildingAppearanceFixture.NormalizeNoonWithCleanup(context, map);
        portraitPawn = BuildingAppearanceWorkflowTest.CreateBuilder();
        GenSpawn.Spawn(portraitPawn, cell + new IntVec3(0, 0, 12), map);
        fixtures.Add(portraitPawn);
        context.DeferCleanup(() =>
        {
            foreach (Thing thing in fixtures) if (thing.Spawned) thing.Destroy(DestroyMode.Vanish);
            foreach (IntVec3 c in CellRect.CenteredOn(cell, 1)) map.fogGrid.Unfog(c);
        });
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep("pause pointer selection fixture", true, EndToEndGameSpeed.Normal);
        foreach (string defName in new[] { "TW_ThinWall", "TW_ThinDoor" })
        foreach (int phase in new[] { 0, 1, 2 })
        for (int rotation = 0; rotation < 4; rotation++)
        {
            ThingDef def = ThingDef.Named(defName);
            Thing target = Spawn(phase == 1 ? def.blueprintDef : phase == 2 ? def.frameDef : def, rotation);
            string label = $"{defName} phase {phase} rotation {rotation}";
            yield return new CameraActionStep("center pointer target " + label, new[] { target.ThingID }, 160);
            yield return new SelectionActionStep("clear precondition selection", Array.Empty<string>(), false);
            if (rotation == 0) yield return Shot("before pointer clicks " + label);
            if (phase == 0 && rotation == 0 && defName == "TW_ThinWall")
            {
                yield return new AssertionStep("Gateway failure scope restores native input and unrelated patches", _ => VerifyFailureCleanup());
                yield return new AssertionStep("Gateway rejects portrait overlays and active group movement", _ => VerifyObstructionGuards());
            }
            Vector3 center = cell.ToVector3Shifted();
            Vector3 normal = new Rot4(rotation).FacingCell.ToVector3();
            Vector3 edge = center + normal * .5f;
            Vector3 tangent = new Vector3(normal.z, 0, -normal.x);
            foreach (float side in new[] { -.20f, .20f })
            {
                yield return Click("click inside edge half " + side + " " + label, edge + normal * side);
                yield return Selected(target, "either half selects " + label);
                if (side > 0) yield return Shot("neighbor half selected " + label);
            }
            yield return Click("click remaining owner cell " + label, center);
            yield return Selected(null, "owner center excludes edge " + label);
            foreach (float end in new[] { -.54f, .54f })
            {
                yield return Click("click beyond segment end " + label, edge + tangent * end);
                yield return Selected(null, "past end excludes edge " + label);
            }
            yield return new AssertionStep("temporary native pointer patches are removed", _ =>
            {
                foreach (var method in new[] { AccessTools.PropertyGetter(typeof(UI), nameof(UI.MousePositionOnUI)),
                    AccessTools.PropertyGetter(typeof(Selector), nameof(Selector.ShiftIsHeld)) })
                    EndToEndAssert.False(Harmony.GetPatchInfo(method)?.Owners.Contains("fumblesneeze.rimworlddevgateway.map-pointer") == true,
                        "Gateway input prefix leaked after click.");
            });
            target.Destroy(DestroyMode.Vanish);
        }

        Thing north = Spawn(ThingDef.Named("TW_ThinWall"), 0);
        Thing east = Spawn(ThingDef.Named("TW_ThinWall"), 1);
        Spawn(ThingDef.Named("TW_ThinWall"), 3);
        Thing shelf = Spawn(ThingDef.Named("ShelfSmall"), 0);
        yield return new CameraActionStep("one cell U and shelf", new[] { shelf.ThingID }, 160);
        yield return Shot("U before center pointer click");
        yield return Click("click shelf in U center", cell.ToVector3Shifted());
        yield return Selected(shelf, "U center selects ordinary shelf not edge");
        yield return Shot("U shelf selected at center");
        var seen = new HashSet<Thing>();
        for (int i = 0; i < 3; i++)
        {
            yield return Click("repeat overlapping corner click " + i, cell.ToVector3Shifted() + new Vector3(.4f, 0, .4f));
            yield return new AssertionStep("native selection cycles corner candidates", _ => seen.Add(Find.Selector.SingleSelectedThing));
            yield return Shot("native U corner cycle " + i);
        }
        yield return new AssertionStep("shelf and both corner edges were selected", _ =>
            EndToEndAssert.True(seen.SetEquals(new[] { north, east, shelf }), "Overlapping native cycling lost a candidate."));
        foreach (Thing thing in fixtures.Where(x => x.Spawned).ToArray()) thing.Destroy(DestroyMode.Vanish);
        Spawn(ThingDef.Named("TW_ThinWall"), 0);
        map.fogGrid.Refog(new CellRect(cell.x, cell.z, 1, 1));
        yield return Click("fogged neighboring edge cannot be selected", cell.ToVector3Shifted() + new Vector3(0, 0, .7f));
        yield return Selected(null, "native fog eligibility retained");
        yield return Shot("fogged owner edge is not selected");
        foreach (var step in ThinWallNativeLogEvidence.Capture(context)) yield return step;
    }

    private Thing Spawn(ThingDef def, int rotation)
    {
        ThingDef stuff = ThingDef.Named("WoodLog");
        Thing thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? stuff : null);
        if (thing is Blueprint_Build blueprint) blueprint.stuffToUse = stuff;
        thing.SetFaction(Faction.OfPlayer);
        GenSpawn.Spawn(thing, cell, map, new Rot4(rotation));
        fixtures.Add(thing);
        return thing;
    }

    private void VerifyFailureCleanup()
    {
        // Supporting Gateway diagnostic: intentionally fail the native MouseUp seam, never manufacture selection.
        Type gateway = AppDomain.CurrentDomain.GetAssemblies().Single(x => x.GetName().Name == "RimWorldDevGateway")
            .GetType("RimWorldDevGateway.GatewayMapPointerSelection", true)!;
        var select = gateway.GetMethod("Select", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!;
        var mouse = AccessTools.PropertyGetter(typeof(UI), nameof(UI.MousePositionOnUI));
        var onGui = AccessTools.Method(typeof(Selector), nameof(Selector.SelectorOnGUI));
        var probe = new Harmony("thin-walls.e2e.pointer-failure-probe");
        var observer = AccessTools.Method(typeof(ThinWallPointerSelectionWorkflowTest), nameof(ObservePointer));
        var failure = AccessTools.Method(typeof(ThinWallPointerSelectionWorkflowTest), nameof(FailMouseUp));
        Event? previous = Event.current;
        Vector3 oldStart = Find.Selector.dragBox.start;
        var sentinel = new Event { type = EventType.KeyDown, keyCode = KeyCode.F8 };
        try
        {
            probe.Patch(mouse, prefix: new HarmonyMethod(observer));
            probe.Patch(onGui, prefix: new HarmonyMethod(failure));
            Event.current = sentinel;
            bool threw = false;
            try { select.Invoke(null, new object[] { cell.x + .5f, cell.z + 1f }); }
            catch (System.Reflection.TargetInvocationException ex)
            { threw = ex.InnerException?.Message == "expected-pointer-failure"; }
            EndToEndAssert.True(threw, "Native fault did not reach the Gateway input scope.");
            EndToEndAssert.True(Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.F8,
                "Synthetic input overwrote prior Event.current.");
            EndToEndAssert.False(Find.Selector.dragBox.active, "Fault left selection dragging active.");
            EndToEndAssert.Equal(oldStart, Find.Selector.dragBox.start, "Fault changed prior drag start.");
            EndToEndAssert.True(Harmony.GetPatchInfo(mouse).Owners.Contains(probe.Id), "Gateway removed an unrelated input patch.");
            EndToEndAssert.False(Harmony.GetPatchInfo(mouse).Owners.Contains("fumblesneeze.rimworlddevgateway.map-pointer"), "Fault leaked pointer override.");
            var shift = AccessTools.PropertyGetter(typeof(Selector), nameof(Selector.ShiftIsHeld));
            EndToEndAssert.False(Harmony.GetPatchInfo(shift)?.Owners.Contains("fumblesneeze.rimworlddevgateway.map-pointer") == true, "Fault leaked Shift override.");
        }
        finally
        {
            Event.current = previous;
            probe.Unpatch(mouse, observer);
            probe.Unpatch(onGui, failure);
        }
    }

    private void VerifyObstructionGuards()
    {
        Type gateway = AppDomain.CurrentDomain.GetAssemblies().Single(x => x.GetName().Name == "RimWorldDevGateway")
            .GetType("RimWorldDevGateway.GatewayMapPointerSelection", true)!;
        var select = gateway.GetMethod("Select", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)!;
        void AssertReject(Vector3 position, string code)
        {
            object result = select.Invoke(null, new object[] { position.x, position.z });
            object error = result.GetType().GetProperty("Error")!.GetValue(result);
            EndToEndAssert.True(error != null, "Obstructed map click succeeded.");
            EndToEndAssert.Equal(code, (string)error!.GetType().GetProperty("Code")!.GetValue(error), "Wrong guard rejection.");
        }
        var bar = Find.ColonistBar;
        int index = bar.Entries.FindIndex(x => x.pawn == portraitPawn);
        EndToEndAssert.True(index >= 0, "Portrait precondition absent.");
        Vector2 gui = bar.DrawLocs[index] + bar.Size / 2f;
        Vector3 world = UI.UIToMapPosition(new Vector2(gui.x, UI.screenHeight - gui.y));
        AssertReject(world, "pointer_obscured");
        var controller = Find.Selector.gotoController;
        var oldJob = portraitPawn.CurJob;
        try
        {
            controller.StartInteraction(cell);
            controller.AddPawn(portraitPawn);
            AssertReject(cell.ToVector3Shifted(), "action_unavailable");
            EndToEndAssert.True(controller.Active && portraitPawn.CurJob == oldJob,
                "Selection guard changed the existing movement interaction or issued a job.");
        }
        finally { controller.Deactivate(); }
    }

    private static void ObservePointer() { }
    private static void FailMouseUp()
    {
        if (Event.current.type == EventType.MouseUp) throw new InvalidOperationException("expected-pointer-failure");
    }

    private static MapPointerSelectionActionStep Click(string name, Vector3 point) => new(name, point.x, point.z);
    private static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
    private static AssertionStep Selected(Thing? expected, string name) => new(name, _ =>
        EndToEndAssert.True(Find.Selector.SingleSelectedThing == expected &&
            Find.Selector.NumSelected == (expected == null ? 0 : 1),
            $"Expected {expected?.ThingID ?? "none"}, got {Find.Selector.SingleSelectedThing?.ThingID ?? "none"}."));
}
