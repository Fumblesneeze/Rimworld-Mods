using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

internal sealed class DiningSession : IThingHolder
{
    private readonly Caravan? caravan;
    private readonly ThingOwner<Thing> travelWare;
    private CompEmbeddedWare? travelEmbedded;
    private Thing? travelPlate;
    private bool travelPlateImported;
    private bool travelPlateWasDirty;
    private WashProvenance travelPlateWashProvenance;

    internal DiningSession(
        Pawn pawn,
        Job job,
        Thing? silverware,
        Thing? reservedPlate,
        Thing? microwave,
        Pawn? servingPawn = null)
        : this(pawn, job, null, silverware, reservedPlate, microwave, servingPawn)
    {
    }

    internal DiningSession(Pawn pawn, Caravan caravan)
        : this(pawn, null, caravan, null, null, null, null)
    {
    }

    private DiningSession(
        Pawn pawn,
        Job? job,
        Caravan? caravan,
        Thing? silverware,
        Thing? reservedPlate,
        Thing? microwave,
        Pawn? servingPawn)
    {
        Pawn = pawn;
        Job = job;
        this.caravan = caravan;
        travelWare = new ThingOwner<Thing>(this, oneStackOnly: false, LookMode.Deep);
        SetSilverware(silverware);
        Microwave = microwave;
        ReservedPlate = reservedPlate;
        ServingPawn = servingPawn;
    }

    internal Pawn Pawn { get; }
    internal Job? Job { get; }
    internal Thing? Silverware { get; private set; }
    internal Thing? CarriedSilverware { get; private set; }
    internal Thing? ReservedPlate { get; }
    internal Thing? CarriedPlate { get; private set; }
    internal bool SilverwareWasDirty { get; private set; }
    internal bool SilverwareWasWildWaterWashed { get; private set; }
    internal float? SilverwareServiceScore { get; private set; }
    internal Thing? Plate { get; private set; }
    internal ServiceWareSnapshot? PlateServiceSnapshot { get; private set; }
    internal ContaminationSources TravelPlateContamination { get; private set; }
    internal Thing? Microwave { get; }
    internal Pawn? ServingPawn { get; }

    internal void CapturePlate(Thing plate)
    {
        if (caravan is not null && plate.holdingOwner is null)
        {
            Plate = plate;
            if (!travelWare.TryAdd(plate, canMergeWithExistingStacks: false))
            {
                throw new InvalidOperationException("Caravan dining could not retain the released plate.");
            }
        }

        Plate = plate;
    }

    internal void AcquireTravelSilverware(Thing? silverware)
    {
        if (caravan is null)
        {
            throw new InvalidOperationException("Only caravan dining sessions can acquire travel ware.");
        }

        SetSilverware(TakeOneForTravel(silverware));
    }

    internal void TrackTravelPlate(CompEmbeddedWare embedded, Thing plate, bool imported)
    {
        travelEmbedded = embedded;
        travelPlate = plate;
        travelPlateImported = imported;
        var sanitation = (plate as ThingWithComps)?.GetComp<CompSanitation>();
        travelPlateWasDirty = sanitation?.IsDirty == true;
        travelPlateWashProvenance = sanitation?.WashProvenance ?? WashProvenance.None;
        PlateServiceSnapshot = KitchenwareRuntime.Describe(plate);
        TravelPlateContamination = SanitationContamination.ForPlate(
            travelPlateWasDirty,
            travelPlateWashProvenance);
    }

    internal void PickupSilverware()
    {
        if (Silverware is null || Silverware.Destroyed || CarriedSilverware is not null)
        {
            return;
        }

        var picked = Silverware.stackCount > 1 ? Silverware.SplitOff(1) : Silverware;
        if (picked.Spawned)
        {
            picked.DeSpawn(DestroyMode.Vanish);
        }

        if (Pawn.inventory?.innerContainer.TryAdd(picked, canMergeWithExistingStacks: false) == true)
        {
            CarriedSilverware = picked;
        }
        else if (Pawn.MapHeld is { } map)
        {
            GenPlace.TryPlaceThing(picked, Pawn.PositionHeld, map, ThingPlaceMode.Near);
        }
    }

    internal void PickupPlate()
    {
        if (ReservedPlate is null || ReservedPlate.Destroyed || CarriedPlate is not null)
        {
            return;
        }

        var picked = ReservedPlate.stackCount > 1 ? ReservedPlate.SplitOff(1) : ReservedPlate;
        if (picked.Spawned)
        {
            picked.DeSpawn(DestroyMode.Vanish);
        }

        if (Pawn.inventory?.innerContainer.TryAdd(picked, canMergeWithExistingStacks: false) == true)
        {
            CarriedPlate = picked;
        }
        else if (Pawn.MapHeld is { } map)
        {
            GenPlace.TryPlaceThing(picked, Pawn.PositionHeld, map, ThingPlaceMode.Near);
        }
    }

    internal void AcceptServedSilverware(Thing silverware)
    {
        CarriedSilverware = silverware;
    }

    internal void BindPastePlate(Thing meal)
    {
        BindCarriedPlate(meal, initializePasteServing: true);
    }

    internal void BindCarriedPlate(Thing meal, bool initializePasteServing = false)
    {
        if (CarriedPlate is null || meal is not ThingWithComps withComps)
        {
            return;
        }

        var sanitation = (CarriedPlate as ThingWithComps)?.GetComp<CompSanitation>();
        var dirty = sanitation?.IsDirty == true;
        var wildWaterWashed = sanitation?.WashedInWildWater == true;
        var embedded = withComps.GetComp<CompEmbeddedWare>();
        if (embedded is null || !embedded.TryEmbedPlate(CarriedPlate))
        {
            return;
        }

        Plate = embedded.PeekPlateThing();
        CarriedPlate = null;
        var culinary = withComps.GetComp<CompCulinaryState>();
        if (initializePasteServing)
        {
            culinary?.ReplaceServings(new[]
            {
                new CulinaryServingRecord(
                    20,
                    70f,
                    SanitationContamination.ForPlate(
                        dirty,
                        wildWaterWashed ? WashProvenance.WildWater : WashProvenance.Safe),
                    0,
                    Find.TickManager?.TicksGame ?? 0)
            });
        }
        else
        {
            culinary?.AddContaminationToCurrent(SanitationContamination.ForPlate(
                dirty,
                wildWaterWashed ? WashProvenance.WildWater : WashProvenance.Safe));
        }
    }

    internal void Finish()
    {
        if (caravan is not null)
        {
            ReturnTravelWare(washedAfterUse: true);
            return;
        }

        var clearingOrigin = Pawn.PositionHeld;
        DropPlate();
        if (Plate is { } embeddedPlate && Pawn.MapHeld is { } plateMap)
        {
            (embeddedPlate as ThingWithComps)?.GetComp<CompSanitation>()?.MarkDirty();
            if (embeddedPlate.holdingOwner is { } owner)
            {
                owner.TryDrop(embeddedPlate, clearingOrigin, plateMap, ThingPlaceMode.Near, out _);
            }
            else if (!embeddedPlate.Spawned)
            {
                GenPlace.TryPlaceThing(embeddedPlate, clearingOrigin, plateMap, ThingPlaceMode.Near);
            }

            Plate = null;
        }

        if (CarriedSilverware is not null)
        {
            (CarriedSilverware as ThingWithComps)?.GetComp<CompSanitation>()?.MarkDirty();
            DropCarried();
        }

        if (ServingPawn is { } server && Pawn.MapHeld is { } map)
        {
            map.GetComponent<MapComponent_GastronomyDishClearing>()?.Schedule(server, clearingOrigin);
        }
    }

    internal void Cancel()
    {
        if (caravan is not null)
        {
            RecoverImportedPlateForCancellation();
            RestoreTravelPlateSanitation();
            ReturnTravelWare(washedAfterUse: false);
            return;
        }

        DropPlate();
        DropCarried();
    }

    public ThingOwner GetDirectlyHeldThings() => travelWare;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, travelWare);
    }

    IThingHolder? IThingHolder.ParentHolder => caravan;

    private void SetSilverware(Thing? silverware)
    {
        Silverware = silverware;
        var sanitation = (silverware as ThingWithComps)?.GetComp<CompSanitation>();
        SilverwareWasDirty = sanitation?.IsDirty == true;
        SilverwareWasWildWaterWashed = sanitation?.WashedInWildWater == true;
        SilverwareServiceScore = silverware is null ? null : KitchenwareRuntime.ServiceScore(silverware);
    }

    private Thing? TakeOneForTravel(Thing? ware)
    {
        if (ware is null || ware.Destroyed || ware.stackCount <= 0)
        {
            return null;
        }

        if (ware.holdingOwner is { } source)
        {
            Thing? transferred;
            return source.TryTransferToContainer(
                       ware,
                       travelWare,
                       1,
                       out transferred,
                       canMergeWithExistingStacks: false) == 1
                ? transferred
                : null;
        }

        var single = ware.stackCount > 1 ? ware.SplitOff(1) : ware;
        return travelWare.TryAdd(single, canMergeWithExistingStacks: false) ? single : null;
    }

    private void ReturnTravelWare(bool washedAfterUse)
    {
        if (caravan is null)
        {
            return;
        }

        var destination = Pawn.inventory?.innerContainer ??
                          caravan.PawnsListForReading
                              .Select(candidate => candidate.inventory?.innerContainer)
                              .FirstOrDefault(container => container is not null);
        if (destination is null)
        {
            Log.Error("[ImmersiveChefs] Caravan dining could not find a pawn inventory for returned tableware.");
            return;
        }

        foreach (var ware in travelWare.InnerListForReading.ToList())
        {
            if (washedAfterUse)
            {
                (ware as ThingWithComps)?.GetComp<CompSanitation>()?.MarkClean(WashProvenance.WildWater);
            }

            var count = ware.stackCount;
            if (travelWare.TryTransferToContainer(
                    ware,
                    destination,
                    count,
                    canMergeWithExistingStacks: false) != count)
            {
                Log.Error($"[ImmersiveChefs] Caravan dining could not return {ware.LabelCap} to inventory.");
            }
        }

        Plate = null;
        CarriedSilverware = null;
        caravan.RecacheInventory();
    }

    private void RecoverImportedPlateForCancellation()
    {
        if (!travelPlateImported || Plate is not null || travelEmbedded is null || travelPlate is null ||
            !ReferenceEquals(travelEmbedded.PeekPlateThing(), travelPlate))
        {
            return;
        }

        Plate = travelEmbedded.ReleasePlateThing();
        if (Plate is not null && Plate.holdingOwner is null &&
            !travelWare.TryAdd(Plate, canMergeWithExistingStacks: false))
        {
            throw new InvalidOperationException("Caravan dining could not recover its unused imported plate.");
        }
    }

    private void RestoreTravelPlateSanitation()
    {
        if (Plate is null || !ReferenceEquals(Plate, travelPlate) ||
            (Plate as ThingWithComps)?.GetComp<CompSanitation>() is not { } sanitation)
        {
            return;
        }

        sanitation.MarkClean(travelPlateWashProvenance);
        if (travelPlateWasDirty)
        {
            sanitation.MarkDirty();
        }
    }

    private void DropPlate()
    {
        if (CarriedPlate is null || Pawn.MapHeld is not { } map)
        {
            return;
        }

        if (CarriedPlate.holdingOwner is { } owner)
        {
            owner.TryDrop(CarriedPlate, Pawn.PositionHeld, map, ThingPlaceMode.Near, out _);
        }
        else if (!CarriedPlate.Spawned)
        {
            GenPlace.TryPlaceThing(CarriedPlate, Pawn.PositionHeld, map, ThingPlaceMode.Near);
        }

        CarriedPlate = null;
    }

    private void DropCarried()
    {
        if (CarriedSilverware is null || Pawn.MapHeld is not { } map)
        {
            return;
        }

        if (CarriedSilverware.holdingOwner is { } owner)
        {
            owner.TryDrop(
                CarriedSilverware,
                Pawn.PositionHeld,
                map,
                ThingPlaceMode.Near,
                out _);
        }
        else if (!CarriedSilverware.Spawned)
        {
            GenPlace.TryPlaceThing(CarriedSilverware, Pawn.PositionHeld, map, ThingPlaceMode.Near);
        }

        CarriedSilverware = null;
    }
}

internal static class DiningSessionRegistry
{
    private static readonly ConditionalWeakTable<Job, DiningSession> Sessions = new();
    private static readonly ConditionalWeakTable<Pawn, DiningSession> PawnSessions = new();
    private static readonly ConditionalWeakTable<Pawn, ActiveIngestion> ActiveIngestions = new();

    private sealed class ActiveIngestion
    {
        internal ActiveIngestion(Job? job, DiningSession session)
        {
            Job = job;
            Session = session;
            Lifecycle.Begin();
        }

        internal Job? Job { get; }
        internal DiningSession Session { get; }
        internal IngestionLifecycleState Lifecycle { get; } = new();
    }

    internal static bool TryAttach(Pawn pawn, Job job, Thing meal)
    {
        var pasteDispenser = meal is Building_NutrientPasteDispenser;
        if ((!MealCoveragePolicy.IsCovered(meal.def) && !pasteDispenser) ||
            !DiningPawnPolicy.AppliesDiningConsequences(pawn.RaceProps.Humanlike) || Sessions.TryGetValue(job, out _))
        {
            return true;
        }

        Thing? selected = null;
        Thing? plate = null;
        if (ImmersiveChefsMod.Settings.WareRequirementMode != WareRequirementMode.Off)
        {
            var emergency = pawn.needs?.food?.CurLevelPercentage <=
                            ImmersiveChefsMod.Settings.EmergencyHungerThreshold;
            var needsPlate = pasteDispenser ||
                             (meal as ThingWithComps)?.GetComp<CompEmbeddedWare>()?.PeekOne() is null;
            if (needsPlate)
            {
                plate = SelectWare(
                    pawn,
                    job,
                    KitchenwareProduct.Plate,
                    emergency,
                    ImmersiveChefsMod.Settings.WareRequirementMode);
                if (plate is null &&
                    ImmersiveChefsMod.Settings.WareRequirementMode == WareRequirementMode.Strict &&
                    !emergency)
                {
                    return false;
                }
            }

            var candidates = pawn.Map.listerThings.AllThings
                .Where(thing => thing.def.GetModExtension<KitchenwareExtension>()?.product == KitchenwareProduct.Silverware)
                .Where(thing => !thing.IsForbidden(pawn) && pawn.CanReach(thing, PathEndMode.Touch, Danger.Some))
                .Where(thing => pawn.CanReserve(thing, 1, 1))
                .Select(thing => new
                {
                    Thing = thing,
                    Dirty = (thing as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true,
                    Score = KitchenwareRuntime.ServiceScore(thing) -
                            (thing.PositionHeld.DistanceToSquared(pawn.PositionHeld) * 0.01f)
                })
                .OrderBy(candidate => candidate.Dirty)
                .ThenByDescending(candidate => candidate.Score)
                .ToList();
            var selection = WareSelectionPolicy.Select(
                WareRequirementMode.Prefer,
                ImmersiveChefsMod.Settings.DirtyWareFallback,
                emergency,
                candidates.Any(candidate => !candidate.Dirty),
                candidates.Any(candidate => candidate.Dirty));
            var wantDirty = selection.Use == WareUse.Dirty;
            selected = candidates.FirstOrDefault(candidate => candidate.Dirty == wantDirty)?.Thing;
            if (selected is not null && !pawn.Reserve(selected, job, 1, 1))
            {
                selected = null;
            }
        }

        var microwave = pasteDispenser ? null : FindMicrowave(pawn, meal);
        var session = new DiningSession(pawn, job, selected, plate, microwave);
        Sessions.Add(job, session);
        PawnSessions.Remove(pawn);
        PawnSessions.Add(pawn, session);
        return true;
    }

    internal static void TryAttachTravel(Pawn pawn, ThingWithComps meal)
    {
        if (!MealCoveragePolicy.IsCovered(meal.def) ||
            !DiningPawnPolicy.AppliesDiningConsequences(pawn.RaceProps.Humanlike) ||
            PawnSessions.TryGetValue(pawn, out _))
        {
            return;
        }

        var caravan = CaravanUtility.GetCaravan(pawn);
        if (caravan is null)
        {
            return;
        }

        Thing? plate = null;
        Thing? silverware = null;
        var settings = ImmersiveChefsMod.Settings;
        if (settings.WareRequirementMode != WareRequirementMode.Off)
        {
            var emergency = pawn.needs?.food?.CurLevelPercentage <= settings.EmergencyHungerThreshold;
            if (meal.GetComp<CompEmbeddedWare>()?.PeekPlateThing() is null)
            {
                plate = SelectTravelWare(
                    caravan,
                    KitchenwareProduct.Plate,
                    emergency,
                    settings.WareRequirementMode);
            }

            silverware = SelectTravelWare(
                caravan,
                KitchenwareProduct.Silverware,
                emergency,
                WareRequirementMode.Prefer);
        }

        var session = new DiningSession(pawn, caravan);
        PawnSessions.Add(pawn, session);
        try
        {
            session.AcquireTravelSilverware(silverware);
            if (meal.GetComp<CompEmbeddedWare>() is { } embedded)
            {
                if (embedded.PeekPlateThing() is { } existingPlate)
                {
                    session.TrackTravelPlate(embedded, existingPlate, imported: false);
                }
                else if (plate is not null)
                {
                    if (embedded.TryEmbedPlate(plate) && embedded.PeekPlateThing() is { } importedPlate)
                    {
                        session.TrackTravelPlate(embedded, importedPlate, imported: true);
                    }
                }
            }

            caravan.RecacheInventory();
        }
        catch
        {
            try
            {
                session.Cancel();
            }
            catch (Exception cleanupError)
            {
                Log.Error($"[ImmersiveChefs] Caravan dining rollback failed: {cleanupError.GetType().Name}: {cleanupError.Message}");
            }

            PawnSessions.Remove(pawn);
            caravan.RecacheInventory();
            throw;
        }
    }

    internal static void TryAttachServed(
        Pawn patron,
        Job diningJob,
        Thing meal,
        Thing? silverware,
        Pawn server)
    {
        if (!MealCoveragePolicy.IsCovered(meal.def) || Sessions.TryGetValue(diningJob, out _))
        {
            return;
        }

        var session = new DiningSession(patron, diningJob, silverware, null, null, server);
        if (silverware is not null)
        {
            session.AcceptServedSilverware(silverware);
        }

        Sessions.Add(diningJob, session);
        PawnSessions.Remove(patron);
        PawnSessions.Add(patron, session);
    }

    internal static bool HasPickup(Job job) =>
        Sessions.TryGetValue(job, out var session) && session.Silverware is not null;

    internal static bool HasPlatePickup(Job job) =>
        Sessions.TryGetValue(job, out var session) && session.ReservedPlate is not null;

    internal static Thing? SilverwareFor(Job job) =>
        Sessions.TryGetValue(job, out var session) ? session.Silverware : null;

    internal static Thing? PlateFor(Job job) =>
        Sessions.TryGetValue(job, out var session) ? session.ReservedPlate : null;

    internal static CompMicrowave? MicrowaveFor(Job job) =>
        Sessions.TryGetValue(job, out var session)
            ? session.Microwave?.TryGetComp<CompMicrowave>()
            : null;

    internal static void Pickup(Pawn pawn)
    {
        if (pawn.CurJob is { } job && Sessions.TryGetValue(job, out var session))
        {
            session.PickupSilverware();
        }
    }

    internal static void PickupPlate(Pawn pawn)
    {
        if (pawn.CurJob is { } job && Sessions.TryGetValue(job, out var session))
        {
            session.PickupPlate();
        }
    }

    internal static void BindPastePlate(Pawn pawn, Thing meal)
    {
        if (pawn.CurJob is { } job && Sessions.TryGetValue(job, out var session))
        {
            session.BindPastePlate(meal);
        }
    }

    internal static void BindReservedPlate(Pawn pawn, Thing meal)
    {
        if (pawn.CurJob is { } job && Sessions.TryGetValue(job, out var session))
        {
            session.BindCarriedPlate(meal);
        }
    }

    internal static bool CapturePlate(Pawn pawn, Thing plate)
    {
        if (Current(pawn) is { } session)
        {
            session.CapturePlate(plate);
            return true;
        }

        return false;
    }

    internal static DiningSession? Current(Pawn pawn)
    {
        if (ActiveIngestions.TryGetValue(pawn, out var active))
        {
            return active.Session;
        }

        if (PawnSessions.TryGetValue(pawn, out var pawnSession))
        {
            return pawnSession;
        }

        return pawn.CurJob is { } job && Sessions.TryGetValue(job, out var session) ? session : null;
    }

    internal static void BeginIngestion(Pawn pawn)
    {
        if (!PawnSessions.TryGetValue(pawn, out var session) ||
            ActiveIngestions.TryGetValue(pawn, out _))
        {
            return;
        }

        ActiveIngestions.Add(pawn, new ActiveIngestion(session.Job, session));
    }

    internal static void Complete(Pawn pawn)
    {
        if (ActiveIngestions.TryGetValue(pawn, out var active))
        {
            active.Session.Finish();
            active.Lifecycle.End();
            if (active.Job is not null)
            {
                Sessions.Remove(active.Job);
            }
            PawnSessions.Remove(pawn);
            ActiveIngestions.Remove(pawn);
            return;
        }

        if (PawnSessions.TryGetValue(pawn, out var pawnSession))
        {
            pawnSession.Finish();
            if (pawnSession.Job is not null)
            {
                Sessions.Remove(pawnSession.Job);
            }
            PawnSessions.Remove(pawn);
            return;
        }

        if (pawn.CurJob is not { } job || !Sessions.TryGetValue(job, out var session))
        {
            return;
        }

        session.Finish();
        Sessions.Remove(job);
    }

    internal static void EndIngestion(Pawn pawn)
    {
        if (!ActiveIngestions.TryGetValue(pawn, out var active))
        {
            return;
        }

        active.Lifecycle.End();
        active.Session.Cancel();
        if (active.Job is not null)
        {
            Sessions.Remove(active.Job);
        }
        PawnSessions.Remove(pawn);
        ActiveIngestions.Remove(pawn);
    }

    internal static void Cleanup(Pawn pawn, Job? job)
    {
        if (job is not null &&
            ActiveIngestions.TryGetValue(pawn, out var active) &&
            ReferenceEquals(active.Job, job) &&
            !active.Lifecycle.ShouldCancelDiningSessionOnJobCleanup)
        {
            return;
        }

        if (job is null || !Sessions.TryGetValue(job, out var session))
        {
            return;
        }

        session.Cancel();
        Sessions.Remove(job);
        if (PawnSessions.TryGetValue(pawn, out var pawnSession) && ReferenceEquals(pawnSession, session))
        {
            PawnSessions.Remove(pawn);
        }
    }

    internal static Thing? FindMicrowave(Pawn pawn, Thing meal)
    {
        if (!ImmersiveChefsMod.Settings.MealTemperatureEnabled ||
            (meal as ThingWithComps)?.GetComp<CompCulinaryState>()?.PeekCurrentServing() is not { } serving ||
            serving.TemperatureCelsius >= ImmersiveChefsMod.Settings.AutoMicrowaveBelow)
        {
            return null;
        }

        return pawn.Map.listerThings.AllThings
            .Where(thing => thing.TryGetComp<CompMicrowave>()?.Operational == true)
            .Where(thing => !thing.IsForbidden(pawn) &&
                            pawn.CanReserveAndReach(thing, PathEndMode.InteractionCell, Danger.Some))
            .OrderBy(thing => thing.Position.DistanceToSquared(pawn.Position))
            .FirstOrDefault(thing => pawn.Reserve(thing, pawn.CurJob, 1, -1));
    }

    internal static Thing? SelectWare(
        Pawn pawn,
        Job job,
        KitchenwareProduct product,
        bool emergency,
        WareRequirementMode mode = WareRequirementMode.Prefer)
    {
        var candidates = pawn.Map.listerThings.AllThings
            .Where(thing => thing.def.GetModExtension<KitchenwareExtension>()?.product == product)
            .Where(thing => !thing.IsForbidden(pawn) && pawn.CanReach(thing, PathEndMode.Touch, Danger.Some))
            .Where(thing => pawn.CanReserve(thing, 1, 1))
            .Select(thing => new
            {
                Thing = thing,
                Dirty = (thing as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true,
                Score = KitchenwareRuntime.ServiceScore(thing) -
                        (thing.PositionHeld.DistanceToSquared(pawn.PositionHeld) * 0.01f)
            })
            .OrderBy(candidate => candidate.Dirty)
            .ThenByDescending(candidate => candidate.Score)
            .ToList();
        var selection = WareSelectionPolicy.Select(
            mode,
            ImmersiveChefsMod.Settings.DirtyWareFallback,
            emergency,
            candidates.Any(candidate => !candidate.Dirty),
            candidates.Any(candidate => candidate.Dirty));
        var wantDirty = selection.Use == WareUse.Dirty;
        var selected = candidates.FirstOrDefault(candidate => candidate.Dirty == wantDirty)?.Thing;
        return selected is not null && pawn.Reserve(selected, job, 1, 1) ? selected : null;
    }

    private static Thing? SelectTravelWare(
        Caravan caravan,
        KitchenwareProduct product,
        bool emergency,
        WareRequirementMode mode)
    {
        var candidates = caravan.AllThings
            .Where(thing => !thing.Destroyed && thing.stackCount > 0)
            .Where(thing => thing.def.GetModExtension<KitchenwareExtension>()?.product == product)
            .Select(thing => new CaravanWareCandidate<Thing>(
                thing,
                (thing as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true,
                KitchenwareRuntime.ServiceScore(thing)))
            .ToList();
        return CaravanDiningPolicy.SelectWare(
            candidates,
            mode,
            ImmersiveChefsMod.Settings.DirtyWareFallback,
            emergency);
    }
}

internal static class KitchenwareRuntime
{
    internal static float ServiceScore(Thing thing)
    {
        var quality = QualityUtility.TryGetQuality(thing, out var foundQuality)
            ? foundQuality
            : QualityCategory.Normal;
        var craftsmanship = quality switch
        {
            QualityCategory.Awful => 0,
            QualityCategory.Poor => 25,
            QualityCategory.Normal => 50,
            QualityCategory.Good => 65,
            QualityCategory.Excellent => 80,
            QualityCategory.Masterwork => 90,
            QualityCategory.Legendary => 100,
            _ => 50
        };
        var cleanliness = (thing as ThingWithComps)?.GetComp<CompKitchenwareStats>()?
            .CurrentStats.MaterialCleanliness ?? 50f;
        return DiningOutcomeCalculator.ServiceScore(craftsmanship, cleanliness);
    }

    internal static ServiceWareSnapshot? Describe(Thing? thing)
    {
        if (thing is null)
        {
            return null;
        }

        var extension = thing.def.GetModExtension<KitchenwareExtension>();
        var product = extension?.product ?? KitchenwareProduct.Plate;
        var material = extension?.fixedMaterialKind ?? ResolveMaterial(thing.Stuff, product);
        var quality = QualityUtility.TryGetQuality(thing, out var found)
            ? found
            : QualityCategory.Normal;
        var stats = (thing as ThingWithComps)?.GetComp<CompKitchenwareStats>()?.CurrentStats ??
                    KitchenwareStatCalculator.Calculate(material, product, quality);
        var dirty = (thing as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true;
        return new ServiceWareSnapshot(material, quality, stats.Comfort, dirty);
    }

    private static KitchenMaterialKind ResolveMaterial(ThingDef? stuff, KitchenwareProduct product)
    {
        if (stuff is null)
        {
            return KitchenMaterialKind.Steel;
        }

        var categories = stuff.stuffProps?.categories;
        var descriptor = new KitchenMaterialDescriptor(
            stuff.defName,
            categories?.Any(category => category.defName == "Metallic") == true,
            categories?.Any(category => category.defName == "Woody") == true,
            categories?.Any(category => category.defName == "Stony") == true);
        return OptionalMaterialAdapter.CreateClassifier().Classify(descriptor, product)?.Kind ??
               KitchenMaterialKind.OtherMetal;
    }
}
