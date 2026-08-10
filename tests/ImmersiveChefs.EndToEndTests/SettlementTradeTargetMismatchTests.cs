using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.settlement-trade-target-mismatch",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "fumblesneeze.immersivechefs",
    MaxFrames = 1_200,
    MaxGameTicks = 2_000,
    MaxWallClockSeconds = 60)]
public sealed class SettlementTradeTargetMismatchTest : IRimWorldEndToEndTest
{
    private Caravan caravan = null!;
    private Settlement nativeSettlement = null!;
    private Settlement mismatchedSettlement = null!;

    public void Arrange(IEndToEndContext context)
    {
        var faction = Find.FactionManager.AllFactionsVisible
            .Where(candidate =>
                !candidate.IsPlayer &&
                !candidate.HostileTo(Faction.OfPlayer) &&
                candidate.def.humanlikeFaction &&
                candidate.def.baseTraderKinds is { Count: > 0 })
            .OrderBy(candidate => candidate.loadID)
            .FirstOrDefault() ??
            throw new EndToEndAssertionException(
                "The base game must provide one neutral humanlike settlement trader faction.");
        var tile = TileFinder.RandomSettlementTileFor(faction);
        var firstSettlement = CreateSettlement(faction, tile, "E2E Native Candidate");
        context.DeferCleanup(() => DestroyWorldObject(firstSettlement));
        var secondSettlement = CreateSettlement(faction, tile, "E2E Mismatch Candidate");
        context.DeferCleanup(() => DestroyWorldObject(secondSettlement));

        var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
            PawnKindDefOf.Colonist,
            Faction.OfPlayer,
            forceGenerateNewPawn: true,
            canGeneratePawnRelations: false,
            forceNoGear: true));
        caravan = CaravanMaker.MakeCaravan(
            new[] { pawn },
            Faction.OfPlayer,
            tile,
            addToWorldPawnsIfNotAlready: true);
        context.DeferCleanup(() => DestroyWorldObject(caravan));

        nativeSettlement = CaravanVisitUtility.SettlementVisitedNow(caravan) ??
            throw new EndToEndAssertionException(
                "RimWorld did not resolve either same-tile settlement as the caravan's native visit target.");
        EndToEndAssert.True(
            ReferenceEquals(nativeSettlement, firstSettlement) ||
            ReferenceEquals(nativeSettlement, secondSettlement),
            "RimWorld's native visit target must be one of the exact arranged settlements.");
        mismatchedSettlement = ReferenceEquals(nativeSettlement, firstSettlement)
            ? secondSettlement
            : firstSettlement;
        Find.TickManager.Pause();
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new SettlementTradeActionStep(
            "reject a same-tile settlement that is not the native visit target",
            mismatchedSettlement.ID,
            caravan.ID,
            expectedFailureCode: "settlement_trade_target_mismatch");
        yield return new AssertionStep(
            "target mismatch leaves every native trade dialog closed",
            _ => EndToEndAssert.False(
                Find.WindowStack.Windows.Any(window => window is Dialog_Trade),
                "Rejecting a nonvisited settlement must not open a trade dialog."));
        yield return new ScreenshotStep(
            "observe no trade dialog after rejecting the wrong settlement",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "settlement trade target mismatch result",
            _ => new Dictionary<string, string>
            {
                ["nativeSettlementWorldObjectId"] = nativeSettlement.ID.ToString(),
                ["mismatchedSettlementWorldObjectId"] = mismatchedSettlement.ID.ToString(),
                ["caravanWorldObjectId"] = caravan.ID.ToString(),
                ["tradeDialogCount"] = Find.WindowStack.Windows.Count(window => window is Dialog_Trade).ToString()
            });
    }

    private static Settlement CreateSettlement(Faction faction, int tile, string name)
    {
        var created = (Settlement)WorldObjectMaker.MakeWorldObject(WorldObjectDefOf.Settlement);
        created.Tile = tile;
        created.SetFaction(faction);
        created.Name = name;
        Find.WorldObjects.Add(created);
        return created;
    }

    private static void DestroyWorldObject(WorldObject worldObject)
    {
        if (!worldObject.Destroyed)
        {
            worldObject.Destroy();
        }
    }
}
