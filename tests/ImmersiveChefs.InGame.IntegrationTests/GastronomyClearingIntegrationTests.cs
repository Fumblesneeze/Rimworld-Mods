using System;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.IntegrationTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.InGame.IntegrationTests;

public static class GastronomyClearingIntegrationTests
{
    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void GastronomyOwnsBothExactWareJobsUntilCancellationAndCanScheduleAgain()
    {
        var map = Find.CurrentMap;
        var cells = map.AllCells
            .Where(cell => cell.Standable(map) && cell.GetThingList(map).Count == 0)
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .Take(10)
            .ToArray();
        IntegrationAssert.Equal(10, cells.Length, "The Gastronomy claim fixture requires ten empty cells.");
        var plateCell = cells[0];
        var destinationCells = cells.Skip(1)
            .OrderBy(cell => cell.DistanceToSquared(plateCell))
            .ToArray();
        var constrainedDishwasherCell = destinationCells[0];
        var acceptingDishwasherCell = destinationCells[destinationCells.Length - 1];
        var remainingCells = destinationCells.Skip(1).Take(destinationCells.Length - 2).ToArray();
        var server = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var cleaner = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var plate = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"), ThingDefOf.Steel);
        var cutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"), ThingDefOf.Steel);
        var replacement = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"), ThingDefOf.Steel);
        var unreachable = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"), ThingDefOf.Steel);
        var constrainedDishwasher = ThingMaker.MakeThing(ImmersiveChefsDefOf.ImmersiveChefs_Dishwasher);
        var acceptingDishwasher = ThingMaker.MakeThing(ImmersiveChefsDefOf.ImmersiveChefs_Dishwasher);
        var filler = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate"), ThingDefOf.Steel);
        Area_Allowed? serverArea = null;
        try
        {
            GenSpawn.Spawn(server, remainingCells[0], map);
            GenSpawn.Spawn(cleaner, remainingCells[1], map);
            GenSpawn.Spawn(plate, plateCell, map);
            GenSpawn.Spawn(cutlery, remainingCells[2], map);
            constrainedDishwasher.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(constrainedDishwasher, constrainedDishwasherCell, map);
            constrainedDishwasher.TryGetComp<CompPowerTrader>().PowerOn = true;
            acceptingDishwasher.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(acceptingDishwasher, acceptingDishwasherCell, map);
            acceptingDishwasher.TryGetComp<CompPowerTrader>().PowerOn = true;
            plate.GetComp<CompSanitation>().MarkDirty();
            cutlery.GetComp<CompSanitation>().MarkDirty();
            filler.stackCount = 15;
            filler.GetComp<CompSanitation>().MarkDirty();
            IntegrationAssert.True(
            constrainedDishwasher.TryGetComp<CompDishwasher>().GetDirectlyHeldThings().TryAdd(filler),
                "The constrained dishwasher must retain fifteen plate-equivalents for the capacity boundary.");
            IntegrationAssert.Equal(
                15f,
                constrainedDishwasher.TryGetComp<CompDishwasher>().UsedCapacity,
                "The nearer capacity fixture must accept either exact item alone but not the complete setting.");
            IntegrationAssert.True(
                constrainedDishwasher.Position.DistanceToSquared(plate.Position) <
                acceptingDishwasher.Position.DistanceToSquared(plate.Position),
                "The constrained dishwasher must be strictly nearer so only complete-setting capacity can reject it.");

            var clearing = map.GetComponent<MapComponent_GastronomyDishClearing>();
            clearing.Schedule(server, plate, cutlery);
            IntegrationAssert.Null(
                new WorkGiver_DoDishes().JobOnThing(cleaner, plate),
                "An ordinary cleaner must not claim pending Gastronomy ware.");
            IntegrationAssert.Null(
                new WorkGiver_DoDishes().JobOnThing(server, plate),
                "The exact server's ordinary work scan must not race its pending Gastronomy handoff.");
            IntegrationAssert.Null(
                new WorkGiver_DoDishes().JobOnThing(server, cutlery),
                "The exact server's ordinary work scan must not split its pending Gastronomy setting.");

            var ordinaryBusyJob = JobMaker.MakeJob(JobDefOf.Goto, remainingCells[4]);
            server.jobs.StartJob(
                ordinaryBusyJob,
                JobCondition.InterruptForced,
                resumeCurJobAfterwards: false,
                cancelBusyStances: true,
                tag: JobTag.Misc,
                preToilReservationsCanFail: false);
            IntegrationAssert.Equal(
                JobDefOf.Goto,
                server.CurJobDef,
                "The pending-claim fixture must put the waiter in unrelated ordinary work.");
            clearing.ProcessPendingClearing();
            IntegrationAssert.True(
                clearing.OwnsExactWare(server, plate) && clearing.OwnsExactWare(server, cutlery),
                "Unrelated ordinary work must leave the exact setting pending for the waiter.");
            IntegrationAssert.Null(
                new WorkGiver_DoDishes().JobOnThing(cleaner, plate),
                "The pending exact setting must stay unavailable to another cleaner while the waiter is busy.");

            // Quicktest can assign a player-forced maintain-posture job to freshly
            // spawned pawns. Put the server in the ordinary idle state that the
            // runtime clearing handoff is allowed to claim.
            server.drafter.Drafted = false;
            server.jobs.StopAll();
            EndAmbientWait(server);
            IntegrationAssert.False(server.Drafted, "The exact server must be undrafted before clearing.");
            IntegrationAssert.False(server.Downed, "The exact server must be standing before clearing.");
            IntegrationAssert.False(server.InMentalState, "The exact server must not be in a mental state before clearing.");
            IntegrationAssert.Null(server.CurJob,
                "The exact server must be ordinarily idle before clearing.");
            var dispatchProbe = WorkGiver_DoDishes.CreateExactWareJob(
                plate,
                WorkGiver_DoDishes.TryFindSharedDestination(
                    server,
                    new Thing[] { plate, cutlery },
                    out var dispatchDestination)
                    ? dispatchDestination
                    : throw new InvalidOperationException(
                        "The exact server must retain one shared native cleaning destination at dispatch time."));
            IntegrationAssert.NotNull(
                dispatchProbe,
                "The exact server must retain a native plate-cleaning job at dispatch time.");
            IntegrationAssert.True(
                clearing.OwnsExactWare(server, plate),
                "The clearing component must still own the exact plate at dispatch time.");
            IntegrationAssert.True(
                clearing.OwnsExactWare(server, cutlery),
                "The clearing component must still own the exact cutlery at dispatch time.");
            var dispatchDriver = dispatchProbe!.MakeDriver(server);
            IntegrationAssert.True(
                dispatchDriver.TryMakePreToilReservations(errorOnFailed: true),
                "The exact generated clearing job must reserve its ware and destination at dispatch time.");
            server.ClearReservationsForJob(dispatchProbe);
            clearing.ProcessPendingClearing();
            IntegrationAssert.Equal(
                ImmersiveChefsDefOf.ImmersiveChefs_DoDishes,
                server.CurJob?.def,
                "The exact Gastronomy server must start its first ordinary dishwashing job.");
            IntegrationAssert.True(
                ReferenceEquals(server.CurJob?.targetB.Thing, acceptingDishwasher),
                "The exact plate job must use the dishwasher that can accept the complete setting.");
            IntegrationAssert.True(
                server.jobs.jobQueue.Any(queued =>
                    queued.job.def == ImmersiveChefsDefOf.ImmersiveChefs_DoDishes &&
                    ReferenceEquals(queued.job.targetA.Thing, cutlery) &&
                    ReferenceEquals(queued.job.targetB.Thing, acceptingDishwasher)),
                "The exact server must retain the cutlery job for the same accepting dishwasher in its native queue.");
            IntegrationAssert.Null(
                new WorkGiver_DoDishes().JobOnThing(cleaner, cutlery),
                "An ordinary cleaner must not steal queued Gastronomy ware.");
            IntegrationAssert.True(
                map.reservationManager.ReservedBy(cutlery, server),
                "The queued exact cutlery must remain natively reserved against generic hauling.");

            var queuedCutleryJob = server.jobs.jobQueue
                .First(queued => ReferenceEquals(queued.job.targetA.Thing, cutlery))
                .job;
            IntegrationAssert.NotNull(
                server.jobs.jobQueue.Extract(queuedCutleryJob),
                "The partial-cancellation fixture must remove the queued cutlery job.");
            clearing.ProcessPendingClearing();
            IntegrationAssert.True(
                clearing.OwnsExactWare(server, plate),
                "Cancelling only the queued cutlery job must retain the active plate claim.");
            IntegrationAssert.True(
                map.reservationManager.ReservedBy(plate, server),
                "Cancelling only the queued cutlery job must retain the active plate reservation.");
            IntegrationAssert.False(
                clearing.IsClaimed(cutlery),
                "Cancelling the queued cutlery job must release only that soft claim.");
            IntegrationAssert.False(
                map.reservationManager.ReservedBy(cutlery, server),
                "Cancelling the queued cutlery job must release only that native reservation.");
            IntegrationAssert.NotNull(
                new WorkGiver_DoDishes().JobOnThing(cleaner, cutlery),
                "Another cleaner must be able to recover the partially cancelled cutlery immediately.");

            GenSpawn.Spawn(replacement, remainingCells[3], map);
            replacement.GetComp<CompSanitation>().MarkDirty();
            clearing.Schedule(server, replacement, null);
            IntegrationAssert.Null(
                new WorkGiver_DoDishes().JobOnThing(cleaner, replacement),
                "A newly served setting must receive independent pending ownership while older jobs are dispatched.");

            clearing.ProcessPendingClearing();
            IntegrationAssert.Null(
                new WorkGiver_DoDishes().JobOnThing(cleaner, replacement),
                "Processing while the waiter clears an older setting must retain the newly scheduled setting.");
            IntegrationAssert.True(
                ReferenceEquals(server.CurJob?.targetA.Thing, plate),
                "Processing the pending replacement must not interrupt the older exact clearing job.");

            server.jobs.StopAll();
            EndAmbientWait(server);
            clearing.ProcessPendingClearing();
            IntegrationAssert.Equal(
                ImmersiveChefsDefOf.ImmersiveChefs_DoDishes,
                server.CurJob?.def,
                "The server must dispatch the independently scheduled replacement after cancellation.");
            IntegrationAssert.True(
                ReferenceEquals(server.CurJob?.targetA.Thing, replacement),
                "The replacement dispatch must own the exact newly served plate.");
            IntegrationAssert.Null(
                new WorkGiver_DoDishes().JobOnThing(cleaner, replacement),
                "An ordinary cleaner must not steal the replacement while its new server job is active.");
            IntegrationAssert.NotNull(
                new WorkGiver_DoDishes().JobOnThing(cleaner, cutlery),
                "Cancellation must promptly release older exact ware to ordinary cleaning.");
            server.jobs.StopAll();
            EndAmbientWait(server);
            clearing.ProcessPendingClearing();
            IntegrationAssert.False(
                clearing.IsClaimed(replacement),
                "Cancelling the replacement dispatch must remove its soft Gastronomy claim.");
            IntegrationAssert.False(
                map.reservationManager.ReservedBy(replacement, server),
                "Cancelling the replacement dispatch must release its native waiter reservation.");
            IntegrationAssert.True(
                replacement.GetComp<CompSanitation>().IsDirty,
                "Cancelling the replacement dispatch must leave the unwashed plate dirty.");
            IntegrationAssert.True(
                cleaner.CanReserveAndReach(replacement, PathEndMode.Touch, Danger.Some),
                "Cancelling the replacement dispatch must make the plate reservable and reachable to another cleaner.");
            IntegrationAssert.NotNull(
                new WorkGiver_DoDishes().JobOnThing(cleaner, replacement),
                "Cancelling the replacement dispatch must release that exact plate to ordinary cleaning.");

            GenSpawn.Spawn(unreachable, remainingCells[5], map);
            unreachable.GetComp<CompSanitation>().MarkDirty();
            clearing.Schedule(server, unreachable, null);
            IntegrationAssert.True(
                map.areaManager.TryMakeNewAllowed(out serverArea),
                "The unreachable fixture must create an isolated server area.");
            serverArea![server.Position] = true;
            server.playerSettings.AreaRestrictionInPawnCurrentMap = serverArea;
            clearing.ProcessPendingClearing();
            IntegrationAssert.False(
                clearing.IsClaimed(unreachable),
                "A setting that becomes unreachable by its waiter must lose the soft claim immediately.");
            IntegrationAssert.False(
                map.reservationManager.ReservedBy(unreachable, server),
                "A setting that becomes unreachable by its waiter must release its native reservation immediately.");
            IntegrationAssert.NotNull(
                new WorkGiver_DoDishes().JobOnThing(cleaner, unreachable),
                "An ordinary cleaner must be able to recover waiter-unreachable dirty ware.");

        }
        finally
        {
            constrainedDishwasher.SetForbidden(false, warnOnFail: false);
            acceptingDishwasher.SetForbidden(false, warnOnFail: false);
            if (server.playerSettings is not null)
            {
                server.playerSettings.AreaRestrictionInPawnCurrentMap = null;
            }

            if (serverArea is not null && map.areaManager.AllAreas.Contains(serverArea))
            {
                serverArea.Delete();
            }

            foreach (var thing in new Thing[]
                     {
                         constrainedDishwasher, acceptingDishwasher, filler,
                         plate, cutlery, replacement, unreachable, cleaner, server
                     })
                if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish);
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void PendingGastronomyWareBlocksOrdinaryHaulingAndYieldsToAcceptedForcedHauling()
    {
        var map = Find.CurrentMap;
        var cells = EmptyCells(map, 5);
        var server = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var hauler = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        var plate = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        var cutlery = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"), ThingDefOf.Steel);
        var forcedPlate = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        var zone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
        try
        {
            GenSpawn.Spawn(server, cells[0], map);
            GenSpawn.Spawn(hauler, cells[1], map);
            GenSpawn.Spawn(plate, cells[2], map);
            GenSpawn.Spawn(cutlery, cells[3], map);
            GenSpawn.Spawn(forcedPlate, cells[4], map);
            plate.GetComp<CompSanitation>().MarkDirty();
            cutlery.GetComp<CompSanitation>().MarkDirty();
            forcedPlate.GetComp<CompSanitation>().MarkDirty();

            map.zoneManager.RegisterZone(zone);
            zone.AddCell(cells[0] + IntVec3.East);
            zone.settings.Priority = StoragePriority.Critical;
            zone.settings.filter.SetDisallowAll();
            zone.settings.filter.SetAllow(plateDef, allow: true);
            zone.settings.filter.SetAllow(
                DefDatabase<SpecialThingFilterDef>.GetNamed("ImmersiveChefs_AllowDirtyKitchenware"),
                allow: true);

            var haulGiver = DefDatabase<WorkGiverDef>.AllDefsListForReading
                .Select(def => def.Worker)
                .OfType<WorkGiver_HaulGeneral>()
                .Single();
            IntegrationAssert.NotNull(
                haulGiver.JobOnThing(hauler, plate, forced: false),
                "Vanilla HaulGeneral must offer the unclaimed dirty plate.");

            var clearing = map.GetComponent<MapComponent_GastronomyDishClearing>();
            clearing.Schedule(server, plate, cutlery);
            clearing.Schedule(server, forcedPlate, null);
            IntegrationAssert.Null(
                haulGiver.JobOnThing(server, plate, forced: false),
                "The owning waiter's ordinary haul scan must not split its setting.");
            IntegrationAssert.Null(
                haulGiver.JobOnThing(hauler, plate, forced: false),
                "Another pawn's ordinary haul scan must not steal claimed service ware.");

            var forcedJob = haulGiver.JobOnThing(hauler, forcedPlate, forced: true);
            IntegrationAssert.NotNull(
                forcedJob,
                "A player-forced HaulGeneral order must remain available for claimed ware.");
            IntegrationAssert.True(
                hauler.jobs.TryTakeOrderedJobPrioritizedWork(
                    forcedJob!,
                    haulGiver,
                    forcedPlate.Position),
                "The other pawn must accept the real prioritized forced-haul job.");
            IntegrationAssert.False(
                clearing.IsClaimed(forcedPlate),
                "Accepting the forced haul must release the exact taken-over ware claim.");
            IntegrationAssert.True(
                clearing.OwnsExactWare(server, plate) && clearing.OwnsExactWare(server, cutlery),
                "Forced takeover of another item must preserve the original place setting.");
        }
        finally
        {
            if (map.zoneManager.AllZones.Contains(zone)) zone.Delete();
            foreach (var thing in new Thing[] { plate, cutlery, forcedPlate, hauler, server })
                if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish);
        }
    }

    [IntegrationTest(RunAt.PlayableMapLoaded)]
    public static void GastronomyDistinguishesHardUnreachableFromTransientDishwasherContention()
    {
        var map = Find.CurrentMap;
        var cells = EmptyCells(map, 6);
        var server = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var reserver = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        var dishwasher = ThingMaker.MakeThing(ImmersiveChefsDefOf.ImmersiveChefs_Dishwasher);
        var plateDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Plate");
        var plate = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        var filler = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        var reservedPlate = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        var hardBlocked = (ThingWithComps)ThingMaker.MakeThing(plateDef, ThingDefOf.Steel);
        Area_Allowed? area = null;
        var allowTerrain = ImmersiveChefsMod.Settings.AllowTerrainHandwashing;
        Job? reservationJob = null;
        try
        {
            GenSpawn.Spawn(server, cells[0], map);
            GenSpawn.Spawn(reserver, cells[1], map);
            dishwasher.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(dishwasher, cells[2], map);
            dishwasher.TryGetComp<CompPowerTrader>().PowerOn = true;
            GenSpawn.Spawn(plate, cells[3], map);
            plate.GetComp<CompSanitation>().MarkDirty();
            filler.stackCount = 16;
            filler.GetComp<CompSanitation>().MarkDirty();
            IntegrationAssert.True(
                dishwasher.TryGetComp<CompDishwasher>().GetDirectlyHeldThings().TryAdd(filler),
                "The transient-capacity fixture must completely fill its sole dishwasher.");

            ImmersiveChefsMod.Settings.AllowTerrainHandwashing = false;
            var clearing = map.GetComponent<MapComponent_GastronomyDishClearing>();
            clearing.Schedule(server, plate, null);
            IntegrationAssert.False(
                WorkGiver_DoDishes.TryFindSharedDestination(server, new Thing[] { plate }, out _),
                "The full dishwasher must not currently accept the pending plate.");
            clearing.ProcessPendingClearing();
            IntegrationAssert.True(
                clearing.IsClaimed(plate),
                "A full but physically reachable dishwasher must remain retryable.");
            clearing.ReleaseClaimFor(plate);

            filler.stackCount = 15;
            GenSpawn.Spawn(reservedPlate, cells[4], map);
            reservedPlate.GetComp<CompSanitation>().MarkDirty();
            reservationJob = JobMaker.MakeJob(JobDefOf.Goto, dishwasher.Position);
            IntegrationAssert.True(
                reserver.Reserve(dishwasher, reservationJob, 1, -1),
                "Another pawn must reserve the sole dishwasher for the contention fixture.");
            clearing.Schedule(server, reservedPlate, null);
            IntegrationAssert.False(
                WorkGiver_DoDishes.TryFindSharedDestination(server, new Thing[] { reservedPlate }, out _),
                "A currently reserved dishwasher must not be selected now.");
            clearing.ProcessPendingClearing();
            IntegrationAssert.True(
                clearing.IsClaimed(reservedPlate),
                "A physically reachable dishwasher reserved by another pawn must remain retryable.");
            clearing.ReleaseClaimFor(reservedPlate);
            reserver.ClearReservationsForJob(reservationJob);
            reservationJob = null;

            GenSpawn.Spawn(hardBlocked, server.Position, map);
            hardBlocked.GetComp<CompSanitation>().MarkDirty();
            dishwasher.SetForbidden(true, warnOnFail: false);
            IntegrationAssert.True(
                map.areaManager.TryMakeNewAllowed(out area),
                "The hard-unreachable fixture must create a one-cell waiter area.");
            area![server.Position] = true;
            server.playerSettings.AreaRestrictionInPawnCurrentMap = area;
            clearing.Schedule(server, hardBlocked, null);
            clearing.ProcessPendingClearing();
            IntegrationAssert.False(
                clearing.IsClaimed(hardBlocked),
                "A forbidden and area-unreachable wash path must release the service claim immediately.");
            IntegrationAssert.False(
                map.reservationManager.ReservedBy(hardBlocked, server),
                "Hard-unreachable washing must release the native ware reservation immediately.");
        }
        finally
        {
            ImmersiveChefsMod.Settings.AllowTerrainHandwashing = allowTerrain;
            dishwasher.SetForbidden(false, warnOnFail: false);
            if (reservationJob is not null) reserver.ClearReservationsForJob(reservationJob);
            if (server.playerSettings is not null) server.playerSettings.AreaRestrictionInPawnCurrentMap = null;
            if (area is not null && map.areaManager.AllAreas.Contains(area)) area.Delete();
            foreach (var thing in new Thing[]
                     { dishwasher, plate, filler, reservedPlate, hardBlocked, reserver, server })
                if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish);
        }
    }

    private static IntVec3[] EmptyCells(Map map, int count)
    {
        var cells = map.AllCells
            .Where(cell => cell.Standable(map) &&
                           cell.GetThingList(map).Count == 0 &&
                           map.zoneManager.ZoneAt(cell) is null)
            .OrderBy(cell => cell.DistanceToSquared(map.Center))
            .Take(count)
            .ToArray();
        IntegrationAssert.Equal(count, cells.Length, "The focused Gastronomy fixture lacks empty cells.");
        return cells;
    }

    private static void EndAmbientWait(Pawn pawn)
    {
        if (pawn.CurJobDef?.defName.StartsWith("Wait", StringComparison.OrdinalIgnoreCase) == true)
        {
            pawn.jobs.EndCurrentJob(JobCondition.InterruptForced, startNewJob: false);
        }
    }

}
