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
    private const int MaxPendingWarePerTick = 16;
    private static readonly AccessTools.FieldRef<JobDriver_DoBill, int> RecipeWorkTicks =
        AccessTools.FieldRefAccess<JobDriver_DoBill, int>("ticksSpentDoingRecipeWork");
    private static readonly HashSet<string> PendingWareRecoveryKeys =
        new(StringComparer.Ordinal);
    private static readonly Queue<PendingWareRecovery> PendingWareRecoveryQueue = new();
    private static int nextPendingRecoveryTick;

    public GameComponent_ImmersiveChefsRecovery(Game game)
    {
        PendingWareRecoveryKeys.Clear();
        PendingWareRecoveryQueue.Clear();
        nextPendingRecoveryTick = 0;
    }

    public override void LoadedGame()
    {
        base.LoadedGame();
        LongEventHandler.ExecuteWhenFinished(() =>
        {
            SchedulePersistedMarkedWare();
            RecoverInterruptedSessions();
            RecoverPendingWare();
        });
    }

    public override void GameComponentTick()
    {
        base.GameComponentTick();
        if (PendingWareRecoveryKeys.Count == 0 || Find.TickManager is not { } ticks ||
            ticks.TicksGame < nextPendingRecoveryTick)
        {
            return;
        }

        nextPendingRecoveryTick = ticks.TicksGame + 1;
        RecoverPendingWare();
    }

    internal static void ScheduleWareRecovery(Pawn pawn, Thing thing)
    {
        if (pawn is not null && thing is not null &&
            !string.IsNullOrEmpty(pawn.ThingID) && !string.IsNullOrEmpty(thing.ThingID))
        {
            var key = RecoveryKey(pawn.ThingID, thing.ThingID);
            if (PendingWareRecoveryKeys.Add(key))
            {
                PendingWareRecoveryQueue.Enqueue(new PendingWareRecovery(pawn, thing, key));
            }
            nextPendingRecoveryTick = 0;
        }
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
            if ((thing as ThingWithComps)?.GetComp<CompSanitation>() is { } personalWare &&
                personalWare.IsPersonalDiningWareFor(pawn))
            {
                personalWare.ClearPersonalDiningOwner();
                continue;
            }

            var sanitation = (thing as ThingWithComps)?.GetComp<CompSanitation>();
            if (sanitation?.ReturnToMapAfterInterruptedSession != true)
            {
                continue;
            }

            var product = thing.def.GetModExtension<KitchenwareExtension>()!.product;
            if (cookwareWasUsed && product == KitchenwareProduct.Cookware)
            {
                (thing as ThingWithComps)?.GetComp<CompSanitation>()?.MarkDirty();
            }

            if (DiningWarePlacementRuntime.DropAt(thing, pawn.PositionHeld, map) is not null)
            {
                sanitation.ClearSessionTransfer();
            }
            else
            {
                ScheduleWareRecovery(pawn, thing);
                Log.Error(
                    "[ImmersiveChefs] Interrupted-session recovery could not return marked colony ware " +
                    $"{thing.ThingID}; its recovery marker was retained for a later attempt.");
            }
        }
    }

    private static void SchedulePersistedMarkedWare()
    {
        foreach (var pawn in Find.Maps.SelectMany(map => map.mapPawns.AllPawns))
        {
            if (pawn.inventory?.innerContainer.Any(HasSessionTransferMarker) == true)
            {
                foreach (var thing in pawn.inventory.innerContainer
                             .Where(HasSessionTransferMarker))
                {
                    ScheduleWareRecovery(pawn, thing);
                }
            }
        }
    }

    private static void RecoverPendingWare()
    {
        if (PendingWareRecoveryKeys.Count == 0)
        {
            return;
        }

        var recoveryAttempts = Math.Min(MaxPendingWarePerTick, PendingWareRecoveryQueue.Count);
        for (var attempt = 0; attempt < recoveryAttempts; attempt++)
        {
            var pending = PendingWareRecoveryQueue.Dequeue();
            if (!PendingWareRecoveryKeys.Contains(pending.Key))
            {
                continue;
            }

            if (!pending.Pawn.TryGetTarget(out var pawn) ||
                !pending.Thing.TryGetTarget(out var thing))
            {
                PendingWareRecoveryKeys.Remove(pending.Key);
                continue;
            }

            var owner = pawn.inventory?.innerContainer;
            if (owner is null || !ReferenceEquals(thing.holdingOwner, owner) ||
                !HasSessionTransferMarker(thing))
            {
                PendingWareRecoveryKeys.Remove(pending.Key);
                continue;
            }

            if (pawn.MapHeld is { } map &&
                DiningWarePlacementRuntime.DropAt(thing, pawn.PositionHeld, map) is not null)
            {
                (thing as ThingWithComps)?.GetComp<CompSanitation>()?.ClearSessionTransfer();
                PendingWareRecoveryKeys.Remove(pending.Key);
            }
            else
            {
                PendingWareRecoveryQueue.Enqueue(pending);
            }
        }
    }

    private static string RecoveryKey(string pawnThingId, string wareThingId) =>
        pawnThingId + "\0" + wareThingId;

    private sealed class PendingWareRecovery
    {
        internal PendingWareRecovery(Pawn pawn, Thing thing, string key)
        {
            Pawn = new System.WeakReference<Pawn>(pawn);
            Thing = new System.WeakReference<Thing>(thing);
            Key = key;
        }

        internal System.WeakReference<Pawn> Pawn { get; }

        internal System.WeakReference<Thing> Thing { get; }

        internal string Key { get; }
    }

    private static bool HasSessionTransferMarker(Thing thing) =>
        (thing as ThingWithComps)?.GetComp<CompSanitation>()
            ?.ReturnToMapAfterInterruptedSession == true;
}
