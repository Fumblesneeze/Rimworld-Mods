using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace RimWorldDevGateway.EndToEndTests;

[RimWorldEndToEndTest("gateway.native-pawn-movement", EndToEndTestContract.GatewayPackageId,
    "brrainz.harmony", EndToEndTestContract.CorePackageId,
    MaxFrames = 3600, MaxGameTicks = 3000, MaxWallClockSeconds = 90)]
public sealed class GatewayNativePawnMovementTests : IRimWorldEndToEndTest
{
    private Pawn pawn = null!;
    private Thing destinationMarker = null!;
    private IntVec3 origin;
    private IntVec3 destination;

    public void Arrange(IEndToEndContext context)
    {
        var map = Current.Game.CurrentMap;
        // Reuse walkable terrain; never leave a terrain edit behind for the next test.
        origin = CellRect.CenteredOn(map.Center, 25).OrderBy(cell => cell.DistanceToSquared(map.Center))
            .First(cell => Enumerable.Range(0, 17).All(offset => {
                var step = cell + new IntVec3(offset, 0, 0);
                return step.InBounds(map) && step.Walkable(map);
            }));
        destination = origin + new IntVec3(16, 0, 0);
        pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        GenSpawn.Spawn(pawn, origin, map);
        destinationMarker = GenSpawn.Spawn(ThingMaker.MakeThing(ThingDefOf.WoodLog), destination, map);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep("pause fixture", true, EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep("select colonist", new[] { pawn.ThingID }, false);
        yield return new CameraActionStep("frame origin and destination", new[] { pawn.ThingID, destinationMarker.ThingID }, 100);
        yield return new ScreenshotStep("before native draft and move", new[] { pawn.ThingID, destinationMarker.ThingID }, 100);
        var draft = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { pawn.ThingID }, Array.Empty<string>())
            .Single(option => !option.Disabled && option.Interaction == EndToEndGizmoInteraction.Toggle &&
                option.ToggleState == false && option.HotKeyDefName == "Command_ColonistDraft");
        yield return new GizmoActionStep("draft via native colonist gizmo", new[] { pawn.ThingID },
            draft.RuntimeType, EndToEndGizmoInteraction.Toggle, stableGizmoId: draft.StableId);
        var go = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(pawn.ThingID, destinationMarker.ThingID)
            .Single(option => !option.Disabled && option.Label == "GoHere".Translate().ToString());
        yield return new FloatMenuActionStep("order Go here through native float-menu command",
            pawn.ThingID, destinationMarker.ThingID, go.StableId);
        yield return new ScreenshotStep("native move ordered while paused", new[] { pawn.ThingID, destinationMarker.ThingID }, 100);
        yield return new TimeControlActionStep("allow ordinary simulation", false, EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep("pawn arrives through ordinary path following",
            _ => pawn.Position == destination,
            new EndToEndDeadline(2400, 2400, TimeSpan.FromSeconds(60)));
        yield return new TimeControlActionStep("pause at destination", true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep("pawn visibly changed cells", _ => {
            EndToEndAssert.True(pawn.Position != origin && pawn.Position == destination,
                "The pawn must traverse the walkable corridor after the native Go here command.");
        });
        yield return new ScreenshotStep("after pawn reaches destination", new[] { pawn.ThingID, destinationMarker.ThingID }, 100);
    }
}
