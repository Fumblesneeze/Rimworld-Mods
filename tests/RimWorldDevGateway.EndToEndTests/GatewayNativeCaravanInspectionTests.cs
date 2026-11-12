#if RIMWORLD_1_7
using System;
using System.Collections.Generic;
using System.Linq;
using LudeonTK;
using RimWorld;
using RimWorld.Planet;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace RimWorldDevGateway.EndToEndTests;

// 23 native actions/captures ×15s +2 view capture/restore ×15s +12 waits ×15s
// +30s native World generation wait +8 screenshots ×30s +90s reserve (including aggregate30s cleanup) =915s.
// Paused UI work assumes no FPS. The host run must allow at least1200s.
[RimWorldEndToEndTest("gateway.native-caravan-inspection", EndToEndTestContract.GatewayPackageId,
    "brrainz.harmony", EndToEndTestContract.CorePackageId,
    MaxFrames = int.MaxValue, MaxGameTicks = 60000, MaxWallClockSeconds = 960)]
public sealed class GatewayNativeCaravanInspectionTests : IRimWorldEndToEndTest, IEndToEndProcessOwnedGameFixture
{
    private Game game = null!;
    private World world = null!;
    private Caravan a = null!, b = null!;
    private Pawn pawnA = null!;
    private string view = null!, handleA = null!, handleB = null!, row = null!, stale = null!;
    private IEndToEndNativeCaravanObservation observation = null!;

    public void Arrange(IEndToEndContext context)
    {
        game = Current.Game; world = game.World;
        EndToEndAssert.True(game.PlayerHasControl && Find.CurrentMap != null && !WorldRendererUtility.WorldSelected,
            "The disposable native caravan fixture requires settled map control.");
        EndToEndAssert.True(!Find.WindowStack.IsOpen<EditWindow_Log>(), "The developer log must start closed.");
        Find.TickManager.Pause(); // Disposable precondition; no ingestion or formation claim.
        observation = context.GetRequiredService<IEndToEndNativeCaravanObservation>();
        var map = Find.CurrentMap ?? throw new InvalidOperationException("The disposable map disappeared during setup.");
        var neighbors = new List<PlanetTile>();
        world.grid.GetTileNeighbors(map.Tile, neighbors);
        var tiles = neighbors.Where(t => t.Valid && t.Layer == world.grid.Surface && world.pathGrid.Passable(t)).Take(2).ToArray();
        EndToEndAssert.True(tiles.Length == 2, "The fixture requires two distinct passable adjacent surface tiles.");
        (a, pawnA) = Create("Gateway Caravan A", "Arden", tiles[0], context);
        (b, _) = Create("Gateway Caravan B", "Briar", tiles[1], context);
    }

    private (Caravan, Pawn) Create(string name, string nickname, PlanetTile tile, IEndToEndContext context)
    {
        var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        context.DeferCleanup(() =>
        {
            RequireOwnedWorld();
            if (world.worldPawns.Contains(pawn)) world.worldPawns.RemovePawn(pawn);
            if (!pawn.Destroyed) pawn.Destroy(DestroyMode.Vanish);
        });
        pawn.Name = new NameTriple("Gateway", nickname, "Probe");
        foreach (var hediff in pawn.health.hediffSet.hediffs.ToArray()) pawn.health.RemoveHediff(hediff);
        var beer = ThingMaker.MakeThing(ThingDefOf.Beer);
        context.DeferCleanup(() => { RequireOwnedWorld(); if (!beer.Destroyed) beer.Destroy(DestroyMode.Vanish); });
        EndToEndAssert.True(pawn.inventory.innerContainer.TryAdd(beer), "One ordinary Beer must be visible in native caravan Items.");
        var caravan = CaravanMaker.MakeCaravan(new[] { pawn }, Faction.OfPlayer, tile, true);
        context.DeferCleanup(() =>
        {
            RequireOwnedWorld();
            if (caravan.ContainsPawn(pawn)) caravan.RemovePawn(pawn);
            if (!caravan.Destroyed) caravan.Destroy();
        });
        caravan.Name = name;
        return (caravan, pawn);
    }
    private void RequireOwnedWorld() => EndToEndAssert.True(ReferenceEquals(Current.Game, game) && ReferenceEquals(game.World, world),
        "Fixture cleanup must never modify a departed Game or World.");

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var map = Find.CurrentMap;
        var renderer = world.renderer;
        var surface = world.grid.Surface;
        var camera = WorldCameraManager.WorldCamera;
        var driver = WorldCameraManager.WorldCameraDriver;
        var terrain = surface.WorldDrawLayers.OfType<WorldDrawLayer_Terrain>().ToArray();
        EndToEndAssert.True(terrain.Length > 0, "The Core-only fixture requires visible surface terrain to observe native generation.");
        yield return new TimeControlActionStep("pause the disposable caravan inspection", true, EndToEndGameSpeed.Normal);
        yield return new NativeWorldViewCaptureStep("capture the complete inactive World view", h => view = h);
        yield return Capture("map before native World command");
        yield return new NativeWorldViewActionStep("open World through its native main button", view, true);
        // ToggleWorld sets wantedMode before a later WorldUpdate queues dirty terrain generation.
        // Waiting only for no current long event can pass before that first render has started.
        yield return new WaitUntilStep("wait for native World terrain generation and settled control", _ =>
            ReferenceEquals(Current.Game, game) && ReferenceEquals(game.World, world) && ReferenceEquals(Find.CurrentMap, map) &&
            ReferenceEquals(world.renderer, renderer) && ReferenceEquals(world.grid.Surface, surface) &&
            ReferenceEquals(WorldCameraManager.WorldCamera, camera) && ReferenceEquals(WorldCameraManager.WorldCameraDriver, driver) &&
            renderer.wantedMode == WorldRenderMode.Planet && WorldRendererUtility.WorldSelected && surface.Visible &&
            camera.gameObject.activeSelf && driver.isActiveAndEnabled && !LongEventHandler.AnyEventNowOrWaiting && game.PlayerHasControl &&
            terrain.All(layer => surface.WorldDrawLayers.Contains(layer) && !layer.Dirty),
            new EndToEndDeadline(int.MaxValue, 60000, TimeSpan.FromSeconds(30)));
        yield return new NativeCaravanCaptureStep("capture exact owned caravan A", view, a.GetUniqueLoadID(), h => handleA = h);
        yield return new NativeCaravanSelectStep("jump and select caravan A through the native command", handleA);
        yield return Wait("wait for native caravan A inspect pane", () => Selected(a) && world.UI.inspectPane.IsOpen);
        yield return new NativeCaravanTabStep("open native Items for caravan A", handleA, EndToEndCaravanTab.Items, true);
        yield return Wait("wait for caravan A Items tab", () => Tab(a, typeof(WITab_Caravan_Items)));
        yield return Capture("caravan A native Items with ordinary Beer");
        yield return new NativeCaravanTabStep("open native Health for caravan A", handleA, EndToEndCaravanTab.Health, true);
        yield return Wait("wait for the observed Arden Health row", () => HealthA().RowVisible && !HealthA().DetailVisible);
        yield return new NativeCaravanHealthCaptureStep("capture Arden native detailed Health button", handleA, pawnA.GetUniqueLoadID(), h => row = h);
        yield return new NativeCaravanHealthClickStep("click Arden native detailed Health button", row);
        yield return Wait("observe native Arden Health card drawing", () => HealthA().DetailVisible);
        yield return Capture("Arden native detailed Health after the row click");
        yield return new NativeCaravanTabStep("switch the same caravan to Items", handleA, EndToEndCaravanTab.Items, true);
        yield return Wait("wait for the same caravan Items view", () => Tab(a, typeof(WITab_Caravan_Items)));
        yield return new NativeCaravanTabStep("return to the same caravan Health tab", handleA, EndToEndCaravanTab.Health, true);
        yield return Wait("observe retained Arden detail without clicking its toggle again", () => HealthA().DetailVisible);
        yield return Capture("Arden details retained across native Items and Health");
        yield return new NativeCaravanHealthCaptureStep("capture the row capability that will become stale", handleA, pawnA.GetUniqueLoadID(), h => stale = h);
        yield return new NativeCaravanCaptureStep("capture exact owned caravan B", view, b.GetUniqueLoadID(), h => handleB = h);
        yield return new NativeCaravanSelectStep("select caravan B through the native command", handleB);
        yield return Wait("wait for native caravan B Health with no Arden card", () => Selected(b) && world.UI.inspectPane.IsOpen &&
            !Find.WindowStack.Windows.Any(w => w is ImmediateWindow && w.ID == -1439870015));
        yield return new NativeCaravanHealthClickStep("reject the captured caravan A row while B is selected", stale, "stale_caravan_health_context");
        yield return Capture("caravan B unchanged after stale Arden row rejection");
        yield return new NativeCaravanSelectStep("return to exact caravan A", handleA);
        yield return new NativeCaravanTabStep("open native Health for caravan A again", handleA, EndToEndCaravanTab.Health, true);
        yield return Wait("wait for fresh Arden row with detail closed", () => HealthA().RowVisible && !HealthA().DetailVisible);
        yield return new NativeCaravanHealthCaptureStep("recapture the fresh Arden row", handleA, pawnA.GetUniqueLoadID(), h => row = h);
        yield return new NativeCaravanHealthClickStep("open Arden details using the fresh native row", row);
        yield return Wait("observe Arden card after the fresh click", () => HealthA().DetailVisible);
        yield return Capture("Arden native Health reopened by fresh input");
        yield return new NativeCaravanHealthCaptureStep("recapture Arden row for explicit native detail closure", handleA, pawnA.GetUniqueLoadID(), h => row = h);
        yield return new NativeCaravanHealthClickStep("close Arden details through the same native toggle", row);
        yield return Wait("observe the owned detailed Health card closed", () => HealthA().RowVisible && !HealthA().DetailVisible &&
            !Find.WindowStack.Windows.Any(w => w is ImmediateWindow && w.ID == -1439870015));
        yield return new NativeCaravanTabStep("close the exact native Health tab", handleA, EndToEndCaravanTab.Health, false);
        yield return new NativeWorldViewActionStep("close World through its native main button", view, false);
        yield return new NativeWorldViewRestoreStep("restore and verify the captured World camera and view flags", view);
        yield return Wait("wait for the restored native map view", () => !WorldRendererUtility.WorldSelected && !world.UI.inspectPane.IsOpen &&
            !WorldCameraManager.WorldCamera.gameObject.activeSelf);
        yield return Capture("native map view after complete World restoration");
        yield return new NativeDeveloperLogActionStep("open the native developer console", true);
        yield return Wait("wait for the native developer console", () => Find.WindowStack.IsOpen<EditWindow_Log>());
        yield return Capture("native developer console after caravan inspection");
        yield return new NativeDeveloperLogActionStep("restore the closed native developer console", false);
    }
    private EndToEndCaravanHealthObservation HealthA() => observation.ObserveHealth(handleA, pawnA.GetUniqueLoadID());
    private bool Selected(Caravan caravan) => ReferenceEquals(world.UI.selector.SingleSelectedObject, caravan) && WorldRendererUtility.WorldSelected;
    private bool Tab(Caravan caravan, Type type) => Selected(caravan) && world.UI.inspectPane.IsOpen && world.UI.inspectPane.OpenTabType == type;
    private static WaitUntilStep Wait(string name, Func<bool> condition) => new(name, _ => condition(),
        new EndToEndDeadline(int.MaxValue, 60000, TimeSpan.FromSeconds(15)));
    private static ScreenshotStep Capture(string name) => new(name, Array.Empty<string>(), 0);
}
#endif
