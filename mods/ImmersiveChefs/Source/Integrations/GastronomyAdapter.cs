using System.Runtime.CompilerServices;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

internal static class GastronomyAdapter
{
    private static readonly ConditionalWeakTable<Job, GastronomyServiceContext> Services = new();

    internal static bool Enabled { get; private set; }

    internal static bool TryInitialize(Harmony harmony, out string reason)
    {
        if (Enabled)
        {
            reason = string.Empty;
            return true;
        }

        var serveType = AccessTools.TypeByName("Gastronomy.Waiting.JobDriver_Serve");
        var dineType = AccessTools.TypeByName("Gastronomy.Dining.JobDriver_Dine");
        var reserve = serveType is null
            ? null
            : AccessTools.Method(serveType, nameof(JobDriver.TryMakePreToilReservations));
        var makeToils = serveType is null ? null : AccessTools.Method(serveType, "MakeNewToils");
        if (serveType is null || dineType is null || reserve is null || makeToils is null ||
            !typeof(JobDriver).IsAssignableFrom(serveType) || !typeof(JobDriver).IsAssignableFrom(dineType))
        {
            reason = "the installed Gastronomy waiter/diner job shape no longer matches the validated 1.6 API";
            return false;
        }

        try
        {
            harmony.Patch(
                reserve,
                postfix: new HarmonyMethod(typeof(GastronomyAdapter), nameof(ReserveServicePostfix)));
            harmony.Patch(
                makeToils,
                postfix: new HarmonyMethod(typeof(GastronomyAdapter), nameof(ServeToilsPostfix)));
            Enabled = true;
            reason = string.Empty;
            Log.Message("[ImmersiveChefs] Gastronomy adapter active; waiters own tableware and reheating service.");
            return true;
        }
        catch (Exception exception)
        {
            reason = $"waiter patch installation failed ({exception.GetType().Name}: {exception.Message})";
            return false;
        }
    }

    internal static void Cleanup(Pawn pawn, Job? job)
    {
        if (job is null || !Services.TryGetValue(job, out var service))
        {
            return;
        }

        service.Cancel(pawn);
        Services.Remove(job);
    }

    private static void ReserveServicePostfix(JobDriver __instance, ref bool __result)
    {
        if (!__result || __instance.job is not { } job || Services.TryGetValue(job, out _))
        {
            return;
        }

        var server = __instance.GetActor();
        var patron = job.GetTarget(TargetIndex.A).Pawn;
        var meal = job.GetTarget(TargetIndex.B).Thing;
        if (patron is null || meal is null || !MealCoveragePolicy.IsCovered(meal.def))
        {
            return;
        }

        var emergency = patron.needs?.food?.CurLevelPercentage <=
                        ImmersiveChefsMod.Settings.EmergencyHungerThreshold;
        var silverware = ImmersiveChefsMod.Settings.WareRequirementMode == WareRequirementMode.Off
            ? null
            : DiningSessionRegistry.SelectWare(server, job, KitchenwareProduct.Silverware, emergency);
        var microwave = DiningSessionRegistry.FindMicrowave(server, meal);
        Services.Add(job, new GastronomyServiceContext(patron, meal, silverware, microwave));
    }

    private static void ServeToilsPostfix(JobDriver __instance, ref IEnumerable<Toil> __result)
    {
        if (__instance.job is { } job && Services.TryGetValue(job, out var service))
        {
            __result = service.Wrap(__instance.GetActor(), job, __result);
        }
    }

    private sealed class GastronomyServiceContext
    {
        private readonly Pawn patron;
        private readonly Thing meal;
        private readonly Thing? selectedSilverware;
        private readonly Thing? microwave;
        private Thing? carriedSilverware;
        private bool delivered;

        internal GastronomyServiceContext(Pawn patron, Thing meal, Thing? selectedSilverware, Thing? microwave)
        {
            this.patron = patron;
            this.meal = meal;
            this.selectedSilverware = selectedSilverware;
            this.microwave = microwave;
        }

        internal IEnumerable<Toil> Wrap(Pawn server, Job job, IEnumerable<Toil> original)
        {
            var originalDiningTarget = job.GetTarget(TargetIndex.C);
            if (selectedSilverware is not null)
            {
                yield return Instant(() => job.SetTarget(TargetIndex.C, selectedSilverware));
                yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.Touch);
                yield return Instant(() => PickupSilverware(server));
            }

            if (microwave?.TryGetComp<CompMicrowave>() is { } microwaveComp)
            {
                yield return Instant(() => job.SetTarget(TargetIndex.C, microwave));
                yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
                yield return Toils_Haul.StartCarryThing(
                    TargetIndex.B,
                    putRemainderInQueue: false,
                    subtractNumTakenFromJobCount: false,
                    failIfStackCountLessThanJobCount: false,
                    reserve: false,
                    canTakeFromInventory: true);
                yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.InteractionCell);
                yield return Toils_General.Wait(microwaveComp.HeatingTicks, TargetIndex.C)
                    .WithProgressBarToilDelay(TargetIndex.C);
                yield return Instant(() =>
                {
                    if (server.carryTracker.CarriedThing is { } carried)
                    {
                        microwaveComp.TryReheat(carried);
                    }
                });
                yield return Toils_Haul.DropCarriedThing();
            }

            if (selectedSilverware is not null || microwave is not null)
            {
                yield return Instant(() => job.SetTarget(TargetIndex.C, originalDiningTarget));
            }

            foreach (var toil in original)
            {
                yield return toil;
            }

            yield return Instant(() => DeliverSilverware(server, job));
        }

        internal void Cancel(Pawn server)
        {
            if (delivered || carriedSilverware is null || server.MapHeld is not { } map)
            {
                return;
            }

            if (carriedSilverware.holdingOwner is { } owner)
            {
                owner.TryDrop(carriedSilverware, server.PositionHeld, map, ThingPlaceMode.Near, out _);
            }

            carriedSilverware = null;
        }

        private void PickupSilverware(Pawn server)
        {
            if (selectedSilverware is null || selectedSilverware.Destroyed || carriedSilverware is not null)
            {
                return;
            }

            var picked = selectedSilverware.stackCount > 1
                ? selectedSilverware.SplitOff(1)
                : selectedSilverware;
            if (picked.Spawned)
            {
                picked.DeSpawn(DestroyMode.Vanish);
            }

            if (server.inventory?.innerContainer.TryAdd(picked, canMergeWithExistingStacks: false) == true)
            {
                carriedSilverware = picked;
            }
            else if (server.MapHeld is { } map)
            {
                GenPlace.TryPlaceThing(picked, server.PositionHeld, map, ThingPlaceMode.Near);
            }
        }

        private void DeliverSilverware(Pawn server, Job serviceJob)
        {
            Thing? deliveredWare = null;
            if (carriedSilverware is not null && patron.inventory is not null &&
                carriedSilverware.holdingOwner is { } source &&
                source.TryTransferToContainer(carriedSilverware, patron.inventory.innerContainer, 1) > 0)
            {
                deliveredWare = carriedSilverware;
            }

            carriedSilverware = null;
            delivered = true;
            if (patron.CurJob is { } diningJob)
            {
                DiningSessionRegistry.TryAttachServed(patron, diningJob, meal, deliveredWare, server);
            }

            Services.Remove(serviceJob);
        }

        private static Toil Instant(Action action)
        {
            return new Toil
            {
                initAction = action,
                defaultCompleteMode = ToilCompleteMode.Instant
            };
        }
    }
}

internal sealed class MapComponent_GastronomyDishClearing : MapComponent
{
    private readonly List<ClearingRequest> requests = new();

    public MapComponent_GastronomyDishClearing(Map map) : base(map)
    {
    }

    internal void Schedule(Pawn server, IntVec3 origin)
    {
        if (!requests.Any(request => request.Server == server))
        {
            requests.Add(new ClearingRequest(server, origin, Find.TickManager.TicksGame + 5000));
        }
    }

    public override void MapComponentTick()
    {
        base.MapComponentTick();
        if (Find.TickManager.TicksGame % 30 != 0)
        {
            return;
        }

        for (var index = requests.Count - 1; index >= 0; index--)
        {
            var request = requests[index];
            if (request.Server.DestroyedOrNull() || request.Server.Map != map ||
                Find.TickManager.TicksGame > request.ExpiresAt)
            {
                requests.RemoveAt(index);
                continue;
            }

            if (!CanInterruptForClearing(request.Server))
            {
                continue;
            }

            var dirty = map.listerThings.AllThings
                .Where(thing => (thing as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true)
                .Where(thing => thing.def.GetModExtension<KitchenwareExtension>()?.product is
                    KitchenwareProduct.Plate or KitchenwareProduct.Silverware)
                .Where(thing => thing.Position.DistanceToSquared(request.Origin) <= 25)
                .OrderBy(thing => thing.Position.DistanceToSquared(request.Server.Position))
                .FirstOrDefault();
            if (dirty is null)
            {
                continue;
            }

            var cleaningJob = new WorkGiver_DoDishes().JobOnThing(request.Server, dirty);
            if (cleaningJob is null)
            {
                continue;
            }

            request.Server.jobs.StartJob(
                cleaningJob,
                JobCondition.InterruptOptional,
                tag: JobTag.Misc,
                resumeCurJobAfterwards: true);
            requests.RemoveAt(index);
        }
    }

    private static bool CanInterruptForClearing(Pawn pawn)
    {
        if (pawn.Drafted || pawn.Downed || pawn.InMentalState || pawn.CurJob?.playerForced == true)
        {
            return false;
        }

        var current = pawn.CurJobDef?.defName ?? string.Empty;
        return current.Length == 0 || current == "Gastronomy_StandBy" ||
               current.StartsWith("Wait", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class ClearingRequest
    {
        internal ClearingRequest(Pawn server, IntVec3 origin, int expiresAt)
        {
            Server = server;
            Origin = origin;
            ExpiresAt = expiresAt;
        }

        internal Pawn Server { get; }
        internal IntVec3 Origin { get; }
        internal int ExpiresAt { get; }
    }
}
