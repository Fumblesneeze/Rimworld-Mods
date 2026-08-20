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
        var waitingToilsType = AccessTools.TypeByName("Gastronomy.Waiting.Toils_Waiting");
        var reserve = serveType is null
            ? null
            : AccessTools.Method(serveType, nameof(JobDriver.TryMakePreToilReservations));
        var makeToils = serveType is null ? null : AccessTools.Method(serveType, "MakeNewToils");
        var clearOrder = waitingToilsType is null
            ? null
            : AccessTools.Method(
                waitingToilsType,
                "ClearOrder",
                new[]
                {
                    typeof(TargetIndex),
                    typeof(TargetIndex),
                    typeof(TargetIndex),
                    typeof(TargetIndex)
                });
        if (serveType is null || dineType is null || reserve is null || makeToils is null ||
            waitingToilsType is null || clearOrder is null || !clearOrder.IsStatic ||
            clearOrder.ReturnType != typeof(Toil) ||
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
            harmony.Patch(
                clearOrder,
                postfix: new HarmonyMethod(typeof(GastronomyAdapter), nameof(ClearOrderPostfix)));
            Enabled = true;
            reason = string.Empty;
            var serviceDescription = TemperatureOwnership.ImmersiveChefsFeaturesActive
                ? "tableware and fallback reheating service"
                : "tableware service; Thermodynamics owns meal heating";
            Log.Message($"[ImmersiveChefs] Gastronomy adapter active; waiters own {serviceDescription}.");
            return true;
        }
        catch (Exception exception)
        {
            RemovePartialPatches(harmony, reserve, makeToils, clearOrder);
            reason = $"waiter patch installation failed ({exception.GetType().Name}: {exception.Message})";
            return false;
        }
    }

    private static void RemovePartialPatches(
        Harmony harmony,
        params System.Reflection.MethodBase[] methods)
    {
        foreach (var method in methods)
        {
            try
            {
                harmony.Unpatch(method, HarmonyPatchType.Postfix, harmony.Id);
            }
            catch
            {
                // Keep the original installation failure authoritative.
            }
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
        var cutlery = ImmersiveChefsMod.Settings.WareRequirementMode == WareRequirementMode.Off
            ? null
            : DiningSessionRegistry.SelectWare(server, job, KitchenwareProduct.Cutlery, emergency);
        var heatingSource = DiningSessionRegistry.FindHeatingSource(server, meal);
        Services.Add(job, new GastronomyServiceContext(patron, meal, cutlery, heatingSource));
    }

    private static void ServeToilsPostfix(JobDriver __instance, ref IEnumerable<Toil> __result)
    {
        if (__instance.job is { } job && Services.TryGetValue(job, out var service))
        {
            __result = service.Wrap(__instance.GetActor(), job, __result);
        }
    }

    private static void ClearOrderPostfix(ref Toil __result)
    {
        if (__result is null)
        {
            return;
        }

        var clearOrder = __result;
        var nativeClearOrder = clearOrder.initAction;
        clearOrder.initAction = () =>
        {
            try
            {
                var server = clearOrder.actor;
                var serviceJob = server?.CurJob;
                if (server is not null && serviceJob is not null &&
                    Services.TryGetValue(serviceJob, out var service))
                {
                    service.DeliverCutlery(server, serviceJob);
                }
            }
            catch (Exception exception)
            {
                OptionalIntegrationDiagnostics.WarnOnce(
                    OptionalIntegration.Gastronomy,
                    $"waiter cutlery delivery failed ({exception.GetType().Name}: {exception.Message}); " +
                    "Gastronomy's native order clearing will continue");
            }
            finally
            {
                nativeClearOrder?.Invoke();
            }
        };
    }

    private sealed class GastronomyServiceContext
    {
        private readonly Pawn patron;
        private readonly Thing meal;
        private readonly Thing? selectedCutlery;
        private readonly MealHeatingSource? heatingSource;
        private Thing? carriedCutlery;
        private bool delivered;

        internal GastronomyServiceContext(
            Pawn patron,
            Thing meal,
            Thing? selectedCutlery,
            Thing? heatingSourceThing)
        {
            this.patron = patron;
            this.meal = meal;
            this.selectedCutlery = selectedCutlery;
            heatingSource = heatingSourceThing is null
                ? null
                : MealHeatingSource.TryCreate(heatingSourceThing);
        }

        internal IEnumerable<Toil> Wrap(Pawn server, Job job, IEnumerable<Toil> original)
        {
            var originalDiningTarget = job.GetTarget(TargetIndex.C);
            if (selectedCutlery is not null)
            {
                yield return Instant(() => job.SetTarget(TargetIndex.C, selectedCutlery));
                yield return Toils_Goto.GotoThing(TargetIndex.C, PathEndMode.Touch);
                yield return Instant(() => PickupCutlery(server));
            }

            if (heatingSource is { } source)
            {
                var heatingInterrupted = false;
                yield return Toils_Goto.GotoThing(TargetIndex.B, PathEndMode.Touch);
                yield return Toils_Haul.StartCarryThing(
                    TargetIndex.B,
                    putRemainderInQueue: false,
                    subtractNumTakenFromJobCount: false,
                    failIfStackCountLessThanJobCount: false,
                    reserve: false,
                    canTakeFromInventory: true);
                yield return IngestCutleryToilsPatch.GotoOptionalHeatingSource(
                    source,
                    normalDeliveryFallback: true,
                    onInterrupted: () => heatingInterrupted = true);
                yield return IngestCutleryToilsPatch.WaitAtOptionalHeatingSource(
                    source,
                    normalDeliveryFallback: true,
                    onInterrupted: () => heatingInterrupted = true);
                yield return Instant(() =>
                {
                    if (server.carryTracker.CarriedThing is { } carried &&
                        MealHeatingPolicy.CanApplyCompletedCycle(
                            heatingInterrupted,
                            source.IsOperational))
                    {
                        source.TryHeat(carried);
                    }
                });
                yield return Toils_Haul.DropCarriedThing();
            }

            if (selectedCutlery is not null)
            {
                yield return Instant(() => job.SetTarget(TargetIndex.C, originalDiningTarget));
            }

            foreach (var toil in original)
            {
                yield return toil;
            }
        }

        internal void Cancel(Pawn server)
        {
            if (delivered || carriedCutlery is null)
            {
                return;
            }

            if (server.MapHeld is not { } map)
            {
                GameComponent_ImmersiveChefsRecovery.ScheduleWareRecovery(server, carriedCutlery);
                return;
            }

            if (carriedCutlery.holdingOwner is { } owner)
            {
                if (owner.TryDrop(carriedCutlery, server.PositionHeld, map, ThingPlaceMode.Near, out _))
                {
                    (carriedCutlery as ThingWithComps)?.GetComp<CompSanitation>()?.ClearSessionTransfer();
                    carriedCutlery = null;
                }
                else
                {
                    GameComponent_ImmersiveChefsRecovery.ScheduleWareRecovery(server, carriedCutlery);
                }
            }
        }

        private void PickupCutlery(Pawn server)
        {
            if (selectedCutlery is null || selectedCutlery.Destroyed || carriedCutlery is not null)
            {
                return;
            }

            var picked = selectedCutlery.stackCount > 1
                ? selectedCutlery.SplitOff(1)
                : selectedCutlery;
            if (picked.Spawned)
            {
                picked.DeSpawn(DestroyMode.Vanish);
            }

            if (server.inventory?.innerContainer.TryAdd(picked, canMergeWithExistingStacks: false) == true)
            {
                (picked as ThingWithComps)?.GetComp<CompSanitation>()?.MarkSessionTransferredWare();
                carriedCutlery = picked;
            }
            else if (server.MapHeld is { } map)
            {
                GenPlace.TryPlaceThing(picked, server.PositionHeld, map, ThingPlaceMode.Near);
            }
        }

        internal void DeliverCutlery(Pawn server, Job serviceJob)
        {
            Thing? deliveredWare = null;
            if (carriedCutlery is not null && patron.inventory is not null &&
                carriedCutlery.holdingOwner is { } source &&
                source.TryTransferToContainer(
                    carriedCutlery,
                    patron.inventory.innerContainer,
                    1,
                    out var transferred,
                    canMergeWithExistingStacks: false) == 1)
            {
                deliveredWare = transferred;
            }

            if (carriedCutlery is not null && deliveredWare is null)
            {
                Cancel(server);
            }

            carriedCutlery = null;
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
    private List<ClearingRequest> requests = new();

    public MapComponent_GastronomyDishClearing(Map map) : base(map)
    {
    }

    internal void Schedule(Pawn server, Thing? dirtyPlate, Thing? dirtyCutlery)
    {
        var exactWare = new[] { dirtyPlate, dirtyCutlery }
            .Where(thing => thing is not null && IsSpawnedDirtyWare(thing, server))
            .Cast<Thing>()
            .Distinct()
            .ToArray();
        if (exactWare.Length == 0)
        {
            return;
        }

        var existing = requests.FirstOrDefault(request => request.Server == server && !request.Dispatched);
        if (existing is not null)
        {
            existing.AddExactWare(exactWare, Find.TickManager.TicksGame + 5000);
            return;
        }

        var request = new ClearingRequest(server, Find.TickManager.TicksGame + 5000);
        request.AddExactWare(exactWare, Find.TickManager.TicksGame + 5000);
        if (request.ExactWare.Count > 0)
        {
            requests.Add(request);
        }
    }

    internal bool IsClaimedByAnotherServer(Pawn pawn, Thing thing) =>
        requests.Any(request => !ReferenceEquals(request.Server, pawn) &&
                                request.OwnsClaimFor(thing, Find.TickManager.TicksGame));

    internal bool IsClaimed(Thing thing) =>
        requests.Any(request => request.OwnsClaimFor(thing, Find.TickManager.TicksGame));

    internal bool OwnsExactWare(Pawn server, Thing thing) =>
        requests.Any(request => ReferenceEquals(request.Server, server) &&
                                request.OwnsClaimFor(thing, Find.TickManager.TicksGame));

    internal void ReleaseClaimFor(Thing thing)
    {
        for (var index = requests.Count - 1; index >= 0; index--)
        {
            var request = requests[index];
            request.ReleaseWare(thing);
            if (request.ExactWare.Count == 0)
            {
                request.ReleaseAllReservations();
                requests.RemoveAt(index);
            }
        }
    }

    internal void ProcessPendingClearing()
    {
        for (var index = requests.Count - 1; index >= 0; index--)
        {
            var request = requests[index];
            if (request.Server.DestroyedOrNull() || request.Server.Map != map ||
                (!request.Dispatched && Find.TickManager.TicksGame > request.ExpiresAt) ||
                IsUnavailableForClearing(request.Server))
            {
                request.ReleaseAllReservations();
                requests.RemoveAt(index);
                continue;
            }

            request.RemoveCompletedWare();
            if (request.ExactWare.Count == 0)
            {
                request.ReleaseAllReservations();
                requests.RemoveAt(index);
                continue;
            }

            if (request.Dispatched)
            {
                request.ReleaseCancelledWare();
                if (request.ExactWare.Count == 0)
                {
                    request.ReleaseAllReservations();
                    requests.RemoveAt(index);
                }

                continue;
            }

            if (!CanInterruptForClearing(request.Server))
            {
                continue;
            }

            if (!WorkGiver_DoDishes.TryFindSharedDestination(
                    request.Server,
                    request.ExactWare,
                    out var destination))
            {
                if (!request.CanRemainPending() ||
                    !WorkGiver_DoDishes.HasReachablePotentialSharedDestination(
                        request.Server,
                        request.ExactWare))
                {
                    request.ReleaseAllReservations();
                    requests.RemoveAt(index);
                }

                continue;
            }

            var exactJobs = request.ExactWare
                .Select(thing => WorkGiver_DoDishes.CreateExactWareJob(thing, destination))
                .ToArray();
            if (!request.TransferReservationsTo(exactJobs))
            {
                continue;
            }

            request.Server.jobs.StartJob(
                exactJobs[0],
                JobCondition.InterruptForced,
                tag: JobTag.Misc,
                resumeCurJobAfterwards: true);
            for (var jobIndex = exactJobs.Length - 1; jobIndex >= 1; jobIndex--)
            {
                request.Server.jobs.jobQueue.EnqueueFirst(exactJobs[jobIndex], tag: JobTag.Misc);
            }

            request.MarkDispatched();
            request.ReleaseCancelledWare();
            if (request.ExactWare.Count == 0)
            {
                request.ReleaseAllReservations();
                requests.RemoveAt(index);
            }
        }
    }

    public override void MapComponentTick()
    {
        base.MapComponentTick();
        if (Find.TickManager.TicksGame % 30 != 0)
        {
            return;
        }

        ProcessPendingClearing();
    }

    public override void ExposeData()
    {
        base.ExposeData();
        Scribe_Collections.Look(ref requests, "immersiveChefsGastronomyClearing", LookMode.Deep);
        requests ??= new List<ClearingRequest>();
        if (Scribe.mode == LoadSaveMode.PostLoadInit)
        {
            foreach (var request in requests)
            {
                request.RestoreReservationOwnership();
            }
        }
    }

    private static bool IsUnavailableForClearing(Pawn pawn) =>
        pawn.Drafted || pawn.Downed || pawn.InMentalState || pawn.CurJob?.playerForced == true;

    private static bool IsPendingGastronomyService(Pawn pawn)
    {
        var current = pawn.CurJobDef?.defName ?? string.Empty;
        return current.StartsWith("Gastronomy_", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsExecutingDishClearing(Pawn pawn) =>
        pawn.CurJobDef == ImmersiveChefsDefOf.ImmersiveChefs_DoDishes;

    private static bool CanInterruptForClearing(Pawn pawn)
    {
        if (IsUnavailableForClearing(pawn))
        {
            return false;
        }

        var current = pawn.CurJobDef?.defName ?? string.Empty;
        return current.Length == 0 || current == "Gastronomy_StandBy" ||
               current.StartsWith("Wait", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsSpawnedDirtyWare(Thing thing, Pawn server)
    {
        return !thing.Destroyed && thing.Spawned && thing.Map == server.MapHeld &&
               (thing as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true &&
               thing.def.GetModExtension<KitchenwareExtension>()?.product is
                   KitchenwareProduct.Plate or KitchenwareProduct.Cutlery;
    }

    private sealed class ClearingRequest : IExposable
    {
        private Job? pendingReservationJob;
        private readonly Dictionary<Thing, Job> dispatchedReservationJobs = new();

        public ClearingRequest()
        {
            Server = null!;
            ExactWare = new List<Thing>();
        }

        internal ClearingRequest(Pawn server, int expiresAt)
        {
            Server = server;
            ExactWare = new List<Thing>();
            ExpiresAt = expiresAt;
            pendingReservationJob = NewPendingReservationJob();
        }

        internal Pawn Server { get; private set; }
        internal List<Thing> ExactWare { get; private set; }
        internal int ExpiresAt { get; private set; }
        internal bool Dispatched { get; private set; }

        internal void AddExactWare(IEnumerable<Thing> exactWare, int expiresAt)
        {
            pendingReservationJob ??= NewPendingReservationJob();
            foreach (var thing in exactWare)
            {
                if (!ExactWare.Contains(thing) &&
                    Server.Reserve(thing, pendingReservationJob, 1, 1))
                {
                    ExactWare.Add(thing);
                }
            }

            ExpiresAt = Math.Max(ExpiresAt, expiresAt);
        }

        internal void MarkDispatched() => Dispatched = true;

        internal bool TransferReservationsTo(IReadOnlyList<Job> jobs)
        {
            if (jobs.Count != ExactWare.Count)
            {
                return false;
            }

            var admitted = new List<(Thing Thing, Job Job)>();
            for (var index = 0; index < ExactWare.Count; index++)
            {
                var thing = ExactWare[index];
                var job = jobs[index];
                if (!Server.Reserve(thing, job, 1, 1))
                {
                    foreach (var pair in admitted)
                    {
                        ReleaseIfOwned(pair.Thing, pair.Job);
                    }

                    return false;
                }

                admitted.Add((thing, job));
            }

            foreach (var pair in admitted)
            {
                dispatchedReservationJobs[pair.Thing] = pair.Job;
            }

            ReleasePendingReservations();
            return true;
        }

        internal bool CanRemainPending() =>
            ExactWare.All(thing =>
                IsSpawnedDirtyWare(thing, Server) &&
                !thing.IsForbidden(Server) &&
                Server.CanReserveAndReach(thing, PathEndMode.Touch, Danger.Some));

        internal void ReleasePendingReservations()
        {
            if (pendingReservationJob is not null)
            {
                Server.ClearReservationsForJob(pendingReservationJob);
            }
        }

        internal void ReleaseAllReservations()
        {
            ReleasePendingReservations();
            foreach (var pair in dispatchedReservationJobs.ToArray())
            {
                ReleaseDispatchedReservation(pair.Key);
            }
        }

        internal bool OwnsClaimFor(Thing thing, int currentTick)
        {
            if ((!Dispatched && currentTick > ExpiresAt) || Server.DestroyedOrNull() ||
                Server.Drafted || Server.Downed || Server.InMentalState ||
                Server.CurJob?.playerForced == true || !ExactWare.Contains(thing))
            {
                return false;
            }

            return !Dispatched || OwnsExactWareJob(thing) ||
                   ReferenceEquals(Server.carryTracker?.CarriedThing, thing);
        }

        internal void RemoveCompletedWare() => RemoveWareWhere(thing =>
            thing.Destroyed ||
            (thing as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty != true ||
            (!thing.Spawned && !OwnsExactWareJob(thing) &&
             !ReferenceEquals(Server.carryTracker?.CarriedThing, thing)));

        internal void ReleaseCancelledWare() => RemoveWareWhere(thing =>
            !OwnsExactWareJob(thing) && !ReferenceEquals(Server.carryTracker?.CarriedThing, thing));

        internal void ReleaseWare(Thing thing) =>
            RemoveWareWhere(candidate => ReferenceEquals(candidate, thing));

        private void RemoveWareWhere(Predicate<Thing> predicate)
        {
            for (var index = ExactWare.Count - 1; index >= 0; index--)
            {
                var thing = ExactWare[index];
                if (!predicate(thing))
                {
                    continue;
                }

                if (!thing.Destroyed && pendingReservationJob is not null)
                {
                    ReleaseIfOwned(thing, pendingReservationJob);
                }

                ReleaseDispatchedReservation(thing);
                ExactWare.RemoveAt(index);
            }
        }

        private void ReleaseDispatchedReservation(Thing thing)
        {
            if (!dispatchedReservationJobs.TryGetValue(thing, out var reservationJob))
            {
                return;
            }

            if (!thing.Destroyed)
            {
                ReleaseIfOwned(thing, reservationJob);
            }

            dispatchedReservationJobs.Remove(thing);
        }

        private void ReleaseIfOwned(Thing thing, Job reservationJob)
        {
            var reservationManager = Server.Map?.reservationManager;
            if (reservationManager?.ReservedBy(thing, Server, reservationJob) == true)
            {
                reservationManager.Release(thing, Server, reservationJob);
            }
        }

        private bool OwnsExactWareJob(Thing thing) =>
            IsExactCleaningJob(Server.CurJob, thing) ||
            Server.jobs.jobQueue.Any(queued => IsExactCleaningJob(queued.job, thing));

        private static bool IsExactCleaningJob(Job? job, Thing thing) =>
            job?.def == ImmersiveChefsDefOf.ImmersiveChefs_DoDishes &&
            (ReferenceEquals(job.targetA.Thing, thing) ||
             job.targetQueueA?.Any(target => ReferenceEquals(target.Thing, thing)) == true);

        public void ExposeData()
        {
            var server = Server;
            Scribe_References.Look(ref server, "server");
            Server = server!;
            var exactWare = ExactWare;
            Scribe_Collections.Look(ref exactWare, "exactWare", LookMode.Reference);
            ExactWare = exactWare ?? new List<Thing>();
            var expiresAt = ExpiresAt;
            Scribe_Values.Look(ref expiresAt, "expiresAt");
            ExpiresAt = expiresAt;
            var dispatched = Dispatched;
            Scribe_Values.Look(ref dispatched, "dispatched");
            Dispatched = dispatched;
        }

        internal void RestoreReservationOwnership()
        {
            dispatchedReservationJobs.Clear();
            if (Server.DestroyedOrNull() || Server.Map is null)
            {
                return;
            }

            var activeJobs = new[] { Server.CurJob }
                .Concat(Server.jobs.jobQueue.Select(queued => queued.job))
                .Where(job => job is not null)
                .Cast<Job>()
                .ToArray();
            foreach (var thing in ExactWare.Where(thing => !thing.Destroyed))
            {
                var exactJob = activeJobs.FirstOrDefault(job => IsExactCleaningJob(job, thing));
                foreach (var stale in Server.Map.reservationManager.ReservationsReadOnly
                             .Where(reservation => ReferenceEquals(reservation.Claimant, Server) &&
                                                   ReferenceEquals(reservation.Target.Thing, thing) &&
                                                   reservation.Job.def == ImmersiveChefsDefOf.ImmersiveChefs_DoDishes &&
                                                   !ReferenceEquals(reservation.Job, exactJob))
                             .ToArray())
                {
                    Server.Map.reservationManager.Release(thing, Server, stale.Job);
                }

                if (Dispatched && exactJob is not null)
                {
                    dispatchedReservationJobs[thing] = exactJob;
                    if (!Server.Map.reservationManager.ReservedBy(thing, Server, exactJob))
                    {
                        Server.Reserve(thing, exactJob, 1, 1);
                    }
                }
            }

            if (Dispatched)
            {
                return;
            }

            pendingReservationJob = NewPendingReservationJob();
            foreach (var thing in ExactWare.Where(thing => !thing.Destroyed))
            {
                Server.Reserve(thing, pendingReservationJob, 1, 1);
            }
        }

        private static Job NewPendingReservationJob() =>
            JobMaker.MakeJob(ImmersiveChefsDefOf.ImmersiveChefs_DoDishes);
    }
}
