using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

/// <summary>
/// RimWorld persists the physical contents of pawn inventories, but transient job registries are
/// intentionally process-local. Active Immersive Chefs jobs therefore restart after a load while
/// returning their exact carried ware to the map, rather than losing or duplicating it.
/// </summary>
public sealed class GameComponent_ImmersiveChefsRecovery : GameComponent
{
    private static readonly AccessTools.FieldRef<JobDriver_DoBill, int> RecipeWorkTicks =
        AccessTools.FieldRefAccess<JobDriver_DoBill, int>("ticksSpentDoingRecipeWork");

    public GameComponent_ImmersiveChefsRecovery(Game game)
    {
    }

    public override void LoadedGame()
    {
        base.LoadedGame();
        LongEventHandler.ExecuteWhenFinished(RecoverInterruptedSessions);
    }

    private static void RecoverInterruptedSessions()
    {
        foreach (var pawn in Find.Maps
                     .SelectMany(map => map.mapPawns.AllPawnsSpawned)
                     .Where(pawn => pawn.CurJob is not null)
                     .ToList())
        {
            var job = pawn.CurJob!;
            var cooking = MealCoveragePolicy.IsCovered(job.RecipeDef);
            var dining = job.def == JobDefOf.Ingest;
            var assisting = job.def == ImmersiveChefsDefOf.ImmersiveChefs_AssistCooking;
            var serving = pawn.jobs.curDriver?.GetType().FullName?.IndexOf(
                "Gastronomy",
                StringComparison.OrdinalIgnoreCase) >= 0;
            if (!cooking && !dining && !assisting && !serving)
            {
                continue;
            }

            var cookwareWasUsed = cooking && pawn.jobs.curDriver is JobDriver_DoBill doBill &&
                                   RecipeWorkTicks(doBill) > 0;
            ReturnCarriedWare(pawn, cookwareWasUsed);
            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: true);
        }
    }

    private static void ReturnCarriedWare(Pawn pawn, bool cookwareWasUsed)
    {
        var owner = pawn.inventory?.innerContainer;
        var map = pawn.MapHeld;
        if (owner is null || map is null)
        {
            return;
        }

        foreach (var thing in owner.InnerListForReading
                     .Where(thing => thing.def.GetModExtension<KitchenwareExtension>() is not null)
                     .ToList())
        {
            var product = thing.def.GetModExtension<KitchenwareExtension>()!.product;
            if (cookwareWasUsed && product == KitchenwareProduct.Cookware)
            {
                (thing as ThingWithComps)?.GetComp<CompSanitation>()?.MarkDirty();
            }

            owner.TryDrop(thing, pawn.PositionHeld, map, ThingPlaceMode.Near, out _);
        }
    }
}
