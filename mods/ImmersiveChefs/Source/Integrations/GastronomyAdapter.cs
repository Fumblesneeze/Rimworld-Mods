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
        var microwave = DiningSessionRegistry.FindMicrowave(server, meal);
        Services.Add(job, new GastronomyServiceContext(patron, meal, cutlery, microwave));
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
        private readonly Thing? microwave;
        private Thing? carriedCutlery;
        private bool delivered;

        internal GastronomyServiceContext(Pawn patron, Thing meal, Thing? selectedCutlery, Thing? microwave)
        {
            this.patron = patron;
            this.meal = meal;
            this.selectedCutlery = selectedCutlery;
            this.microwave = microwave;
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

            if (selectedCutlery is not null || microwave is not null)
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
    private readonly List<ClearingRequest> requests = new();

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

        var existing = requests.FirstOrDefault(request => request.Server == server);
        if (existing is not null)
        {
            existing.AddExactWare(exactWare, Find.TickManager.TicksGame + 5000);
            return;
        }

        requests.Add(new ClearingRequest(server, exactWare, Find.TickManager.TicksGame + 5000));
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

            request.RemoveUnavailableWare();
            if (request.ExactWare.Count == 0)
            {
                requests.RemoveAt(index);
                continue;
            }

            var jobs = request.ExactWare
                .Select(thing => new WorkGiver_DoDishes().JobOnThing(request.Server, thing))
                .ToArray();
            if (jobs.Any(job => job is null))
            {
                continue;
            }

            var exactJobs = jobs.Cast<Job>().ToArray();
            request.Server.jobs.StartJob(
                exactJobs[0],
                JobCondition.InterruptOptional,
                tag: JobTag.Misc,
                resumeCurJobAfterwards: true);
            for (var jobIndex = exactJobs.Length - 1; jobIndex >= 1; jobIndex--)
            {
                request.Server.jobs.jobQueue.EnqueueFirst(exactJobs[jobIndex], tag: JobTag.Misc);
            }

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

    private static bool IsSpawnedDirtyWare(Thing thing, Pawn server)
    {
        return !thing.Destroyed && thing.Spawned && thing.Map == server.MapHeld &&
               (thing as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true &&
               thing.def.GetModExtension<KitchenwareExtension>()?.product is
                   KitchenwareProduct.Plate or KitchenwareProduct.Cutlery;
    }

    private sealed class ClearingRequest
    {
        internal ClearingRequest(Pawn server, IEnumerable<Thing> exactWare, int expiresAt)
        {
            Server = server;
            ExactWare = exactWare.Distinct().ToList();
            ExpiresAt = expiresAt;
        }

        internal Pawn Server { get; }
        internal List<Thing> ExactWare { get; }
        internal int ExpiresAt { get; private set; }

        internal void AddExactWare(IEnumerable<Thing> exactWare, int expiresAt)
        {
            foreach (var thing in exactWare)
            {
                if (!ExactWare.Contains(thing))
                {
                    ExactWare.Add(thing);
                }
            }

            ExpiresAt = Math.Max(ExpiresAt, expiresAt);
        }

        internal void RemoveUnavailableWare() =>
            ExactWare.RemoveAll(thing => !IsSpawnedDirtyWare(thing, Server));
    }
}
