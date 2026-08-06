using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

internal enum DiningCutlerySource
{
    Colony,
    PersonalInventory
}

internal sealed class DiningSession : IThingHolder
{
    private readonly Caravan? caravan;
    private readonly ThingOwner<Thing> travelWare;
    private CompEmbeddedWare? travelEmbedded;
    private Thing? travelPlate;
    private bool travelPlateImported;
    private bool travelPlateWasDirty;
    private WashProvenance travelPlateWashProvenance;
    private bool cutleryFromPersonalInventory;

    internal DiningSession(
        Pawn pawn,
        Job job,
        Thing? cutlery,
        Thing? reservedPlate,
        Thing? microwave,
        DiningCutlerySource cutlerySource,
        Pawn? servingPawn = null,
        Pawn? carrierPawn = null,
        ThingDef? requestedMealDef = null)
        : this(
            pawn,
            job,
            null,
            cutlery,
            reservedPlate,
            microwave,
            cutlerySource,
            servingPawn,
            carrierPawn,
            requestedMealDef)
    {
    }

    internal DiningSession(Pawn pawn, Caravan caravan)
        : this(
            pawn,
            null,
            caravan,
            null,
            null,
            null,
            DiningCutlerySource.Colony,
            null,
            null,
            null)
    {
    }

    private DiningSession(
        Pawn pawn,
        Job? job,
        Caravan? caravan,
        Thing? cutlery,
        Thing? reservedPlate,
        Thing? microwave,
        DiningCutlerySource cutlerySource,
        Pawn? servingPawn,
        Pawn? carrierPawn,
        ThingDef? requestedMealDef)
    {
        Pawn = pawn;
        CarrierPawn = carrierPawn ?? pawn;
        Job = job;
        this.caravan = caravan;
        travelWare = new ThingOwner<Thing>(this, oneStackOnly: false, LookMode.Deep);
        cutleryFromPersonalInventory = cutlerySource == DiningCutlerySource.PersonalInventory;
        SetCutlery(cutlery);
        Microwave = microwave;
        ReservedPlate = reservedPlate;
        ServingPawn = servingPawn;
        RequestedMealDef = requestedMealDef;
    }

    internal Pawn Pawn { get; }
    internal Pawn CarrierPawn { get; }
    internal bool IsAssisted => !ReferenceEquals(Pawn, CarrierPawn);
    internal Job? Job { get; }
    internal Thing? Cutlery { get; private set; }
    internal Thing? CarriedCutlery { get; private set; }
    internal Thing? ReservedPlate { get; }
    internal Thing? CarriedPlate { get; private set; }
    internal bool CutleryWasDirty { get; private set; }
    internal bool CutleryWasWildWaterWashed { get; private set; }
    internal float? CutleryServiceScore { get; private set; }
    internal Thing? Plate { get; private set; }
    internal ServiceWareSnapshot? PlateServiceSnapshot { get; private set; }
    internal ContaminationSources TravelPlateContamination { get; private set; }
    internal Thing? Microwave { get; }
    internal Pawn? ServingPawn { get; private set; }
    internal ThingDef? RequestedMealDef { get; }

    internal void CapturePlate(Thing plate)
    {
        PlateServiceSnapshot ??= KitchenwareRuntime.Describe(plate);
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

    internal void AcquireTravelCutlery(Thing? cutlery)
    {
        if (caravan is null)
        {
            throw new InvalidOperationException("Only caravan dining sessions can acquire travel ware.");
        }

        SetCutlery(TakeOneForTravel(cutlery));
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

    internal void PickupCutlery()
    {
        if (Cutlery is null || Cutlery.Destroyed || CarriedCutlery is not null)
        {
            return;
        }

        if (cutleryFromPersonalInventory &&
            ReferenceEquals(Cutlery.holdingOwner, CarrierPawn.inventory?.innerContainer))
        {
            var inventory = CarrierPawn.inventory!.innerContainer;
            var personal = inventory.Take(Cutlery, 1);
            if (inventory.TryAdd(personal, canMergeWithExistingStacks: false))
            {
                CarriedCutlery = personal;
            }
            else if (CarrierPawn.MapHeld is { } map)
            {
                GenPlace.TryPlaceThing(personal, CarrierPawn.PositionHeld, map, ThingPlaceMode.Near);
                Log.Error("[ImmersiveChefs] Could not retain a guest's personal cutlery in inventory; " +
                          "the exact item was placed beside the guest instead.");
            }
            else
            {
                Log.Error("[ImmersiveChefs] Could not reinsert a guest's personal cutlery after splitting its stack.");
            }

            return;
        }

        var picked = Cutlery.stackCount > 1 ? Cutlery.SplitOff(1) : Cutlery;
        if (picked.Spawned)
        {
            picked.DeSpawn(DestroyMode.Vanish);
        }

        if (CarrierPawn.inventory?.innerContainer.TryAdd(picked, canMergeWithExistingStacks: false) == true)
        {
            CarriedCutlery = picked;
        }
        else if (CarrierPawn.MapHeld is { } map)
        {
            GenPlace.TryPlaceThing(picked, CarrierPawn.PositionHeld, map, ThingPlaceMode.Near);
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

        if (CarrierPawn.inventory?.innerContainer.TryAdd(picked, canMergeWithExistingStacks: false) == true)
        {
            CarriedPlate = picked;
        }
        else if (CarrierPawn.MapHeld is { } map)
        {
            GenPlace.TryPlaceThing(picked, CarrierPawn.PositionHeld, map, ThingPlaceMode.Near);
        }
    }

    internal void AcceptServedCutlery(Thing cutlery)
    {
        CarriedCutlery = cutlery;
    }

    internal void AcceptWaiterService(Thing? deliveredCutlery, Pawn server)
    {
        ServingPawn = server;
        if (deliveredCutlery is null || ReferenceEquals(CarriedCutlery, deliveredCutlery))
        {
            return;
        }

        if (CarriedCutlery is not null && !cutleryFromPersonalInventory &&
            CarrierPawn.MapHeld is { } map && CarriedCutlery.holdingOwner is { } owner)
        {
            owner.TryDrop(CarriedCutlery, CarrierPawn.PositionHeld, map, ThingPlaceMode.Near, out _);
        }

        cutleryFromPersonalInventory = false;
        SetCutlery(deliveredCutlery);
        CarriedCutlery = deliveredCutlery;
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
        if (DiningDirtPolicy.ShouldCreate(
                coveredMeal: true,
                humanlikeDiner: DiningPawnPolicy.AppliesDiningConsequences(Pawn.RaceProps.Humanlike),
                requirementMode: ImmersiveChefsMod.Settings.WareRequirementMode,
                ingestionCompleted: true,
                mapAvailable: Pawn.MapHeld is not null,
                hasCutlery: CarriedCutlery is not null) &&
            Pawn.MapHeld is { } diningMap)
        {
            FilthMaker.TryMakeFilth(clearingOrigin, diningMap, ThingDefOf.Filth_Dirt, count: 1);
        }

        DropPlate(Pawn);
        Thing? dirtyPlate = null;
        if (Plate is { } embeddedPlate && Pawn.MapHeld is { } plateMap)
        {
            (embeddedPlate as ThingWithComps)?.GetComp<CompSanitation>()?.MarkDirty();
            dirtyPlate = DropAt(embeddedPlate, clearingOrigin, plateMap);

            Plate = null;
        }

        Thing? dirtyCutlery = null;
        if (CarriedCutlery is not null)
        {
            var usedCutlery = CarriedCutlery;
            (usedCutlery as ThingWithComps)?.GetComp<CompSanitation>()?.MarkDirty();
            if (cutleryFromPersonalInventory &&
                ReferenceEquals(usedCutlery.holdingOwner, CarrierPawn.inventory?.innerContainer))
            {
                CarriedCutlery = null;
            }
            else
            {
                dirtyCutlery = DropCarried(Pawn);
            }
        }

        if (ServingPawn is { } server && Pawn.MapHeld is { } map)
        {
            map.GetComponent<MapComponent_GastronomyDishClearing>()?.Schedule(
                server,
                dirtyPlate,
                dirtyCutlery);
        }
        else
        {
            CommonSenseAdapter.ScheduleCommittedHandoff(
                CarrierPawn,
                Job,
                dirtyPlate,
                dirtyCutlery,
                gastronomyOwned: false);
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

        DropPlate(CarrierPawn);
        if (cutleryFromPersonalInventory)
        {
            CarriedCutlery = null;
        }
        else
        {
            DropCarried(CarrierPawn);
        }
    }

    public ThingOwner GetDirectlyHeldThings() => travelWare;

    public void GetChildHolders(List<IThingHolder> outChildren)
    {
        ThingOwnerUtility.AppendThingHoldersFromThings(outChildren, travelWare);
    }

    IThingHolder? IThingHolder.ParentHolder => caravan;

    private void SetCutlery(Thing? cutlery)
    {
        Cutlery = cutlery;
        var sanitation = (cutlery as ThingWithComps)?.GetComp<CompSanitation>();
        CutleryWasDirty = sanitation?.IsDirty == true;
        CutleryWasWildWaterWashed = sanitation?.WashedInWildWater == true;
        CutleryServiceScore = cutlery is null ? null : KitchenwareRuntime.ServiceScore(cutlery);
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
        CarriedCutlery = null;
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

    private Thing? DropPlate(Pawn dropPawn)
    {
        if (CarriedPlate is null || dropPawn.MapHeld is not { } map)
        {
            return null;
        }

        var dropped = DropAt(CarriedPlate, dropPawn.PositionHeld, map);
        CarriedPlate = null;
        return dropped;
    }

    private Thing? DropCarried(Pawn dropPawn)
    {
        if (CarriedCutlery is null || dropPawn.MapHeld is not { } map)
        {
            return null;
        }

        var dropped = DropAt(CarriedCutlery, dropPawn.PositionHeld, map);
        CarriedCutlery = null;
        return dropped;
    }

    private static Thing? DropAt(Thing thing, IntVec3 position, Map map)
    {
        if (thing.holdingOwner is { } owner)
        {
            return owner.TryDrop(thing, position, map, ThingPlaceMode.Near, out var dropped)
                ? dropped
                : null;
        }

        if (thing.Spawned)
        {
            return thing;
        }

        return GenPlace.TryPlaceThing(thing, position, map, ThingPlaceMode.Near, out var placed)
            ? placed
            : null;
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

    private readonly struct DiningMealResolution
    {
        internal DiningMealResolution(ThingDef? mealDef, bool failed)
        {
            MealDef = mealDef;
            Failed = failed;
        }

        internal ThingDef? MealDef { get; }
        internal bool Failed { get; }
    }

    internal static bool TryAttach(Pawn pawn, Job job, Thing meal)
    {
        if (!DiningPawnPolicy.AppliesDiningConsequences(pawn.RaceProps.Humanlike) ||
            Sessions.TryGetValue(job, out _))
        {
            return true;
        }

        var pasteDispenser = meal is Building_NutrientPasteDispenser;
        var resolution = ResolveDiningMealDef(meal, pasteDispenser, pawn, pawn);
        if (resolution.Failed)
        {
            return false;
        }

        var diningMealDef = resolution.MealDef;
        if (diningMealDef is null || !MealCoveragePolicy.IsCovered(diningMealDef))
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
                    ImmersiveChefsMod.Settings.WareRequirementMode,
                    plateComplexity: MealComplexityRuntime.Classify(diningMealDef));
                if (plate is null &&
                    ImmersiveChefsMod.Settings.WareRequirementMode == WareRequirementMode.Strict &&
                    !emergency)
                {
                    return false;
                }
            }

            selected = SelectWare(
                pawn,
                job,
                KitchenwareProduct.Cutlery,
                emergency,
                WareRequirementMode.Prefer,
                allowPersonalInventory: MayUsePersonalInventory(pawn));
        }

        var microwave = pasteDispenser ? null : FindMicrowave(pawn, meal);
        var cutlerySource = selected is not null &&
                            ReferenceEquals(selected.holdingOwner, pawn.inventory?.innerContainer)
            ? DiningCutlerySource.PersonalInventory
            : DiningCutlerySource.Colony;
        var session = new DiningSession(
            pawn,
            job,
            selected,
            plate,
            microwave,
            cutlerySource,
            requestedMealDef: diningMealDef);
        Sessions.Add(job, session);
        PawnSessions.Remove(pawn);
        PawnSessions.Add(pawn, session);
        return true;
    }

    internal static bool TryAttachAssisted(Pawn feeder, Pawn patient, Job job, Thing foodSource)
    {
        if (!DiningPawnPolicy.AppliesDiningConsequences(patient.RaceProps.Humanlike) ||
            Sessions.TryGetValue(job, out _))
        {
            return true;
        }

        var pasteDispenser = foodSource is Building_NutrientPasteDispenser;
        var resolution = ResolveDiningMealDef(foodSource, pasteDispenser, patient, feeder);
        if (resolution.Failed)
        {
            return false;
        }

        var diningMealDef = resolution.MealDef;
        if (diningMealDef is null || !MealCoveragePolicy.IsCovered(diningMealDef))
        {
            return true;
        }

        Thing? cutlery = null;
        Thing? plate = null;
        var settings = ImmersiveChefsMod.Settings;
        if (settings.WareRequirementMode != WareRequirementMode.Off)
        {
            var emergency = patient.needs?.food?.CurLevelPercentage <= settings.EmergencyHungerThreshold;
            var needsPlate = pasteDispenser ||
                             (foodSource as ThingWithComps)?.GetComp<CompEmbeddedWare>()?.PeekOne() is null;
            if (needsPlate)
            {
                plate = SelectWare(
                    feeder,
                    job,
                    KitchenwareProduct.Plate,
                    emergency,
                    settings.WareRequirementMode,
                    plateComplexity: MealComplexityRuntime.Classify(diningMealDef));
                if (plate is null && settings.WareRequirementMode == WareRequirementMode.Strict && !emergency)
                {
                    return false;
                }
            }

            cutlery = SelectWare(
                feeder,
                job,
                KitchenwareProduct.Cutlery,
                emergency,
                WareRequirementMode.Prefer);
        }

        var microwave = pasteDispenser ? null : FindMicrowave(feeder, foodSource);
        var session = new DiningSession(
            patient,
            job,
            cutlery,
            plate,
            microwave,
            DiningCutlerySource.Colony,
            carrierPawn: feeder,
            requestedMealDef: diningMealDef);
        Sessions.Add(job, session);
        PawnSessions.Remove(patient);
        PawnSessions.Add(patient, session);
        return true;
    }

    private static DiningMealResolution ResolveDiningMealDef(
        Thing foodSource,
        bool pasteDispenser,
        Pawn eater,
        Pawn getter)
    {
        if (pasteDispenser && ReplimatAdapter.AppliesTo(foodSource))
        {
            var resolved = ReplimatAdapter.TryResolveMealDef(foodSource, eater, getter, out var replimatMeal);
            return new DiningMealResolution(
                replimatMeal,
                DispenserMealResolutionPolicy.Failed(sourceRecognized: true, resolved));
        }

        if (pasteDispenser && MealPrinterAdapter.AppliesTo(foodSource))
        {
            var resolved = MealPrinterAdapter.TryResolveMealDef(foodSource, out var printedMeal);
            return new DiningMealResolution(
                printedMeal,
                DispenserMealResolutionPolicy.Failed(sourceRecognized: true, resolved));
        }

        return new DiningMealResolution(
            pasteDispenser ? FoodUtility.GetFinalIngestibleDef(foodSource, false) : foodSource.def,
            failed: false);
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
        Thing? cutlery = null;
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
                    settings.WareRequirementMode,
                    MealComplexityRuntime.Classify(meal.def));
            }

            cutlery = SelectTravelWare(
                caravan,
                KitchenwareProduct.Cutlery,
                emergency,
                WareRequirementMode.Prefer);
        }

        var session = new DiningSession(pawn, caravan);
        PawnSessions.Add(pawn, session);
        try
        {
            session.AcquireTravelCutlery(cutlery);
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
        Thing? cutlery,
        Pawn server)
    {
        if (!MealCoveragePolicy.IsCovered(meal.def))
        {
            return;
        }

        if (Sessions.TryGetValue(diningJob, out var existing))
        {
            existing.AcceptWaiterService(cutlery, server);
            return;
        }

        var session = new DiningSession(
            patron,
            diningJob,
            cutlery,
            null,
            null,
            DiningCutlerySource.Colony,
            server);
        if (cutlery is not null)
        {
            session.AcceptServedCutlery(cutlery);
        }

        Sessions.Add(diningJob, session);
        PawnSessions.Remove(patron);
        PawnSessions.Add(patron, session);
    }

    internal static bool HasPickup(Job job) =>
        Sessions.TryGetValue(job, out var session) && session.Cutlery is not null;

    internal static bool HasPlatePickup(Job job) =>
        Sessions.TryGetValue(job, out var session) && session.ReservedPlate is not null;

    internal static Thing? CutleryFor(Job job) =>
        Sessions.TryGetValue(job, out var session) ? session.Cutlery : null;

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
            session.PickupCutlery();
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

    internal static ThingDef? RequestedMealDefFor(Pawn pawn)
    {
        return Current(pawn)?.RequestedMealDef;
    }

    internal static bool MatchesCurrentSource(Pawn pawn, Thing source)
    {
        return pawn.CurJob is { } job &&
               ReferenceEquals(job.GetTarget(TargetIndex.A).Thing, source) &&
               Sessions.TryGetValue(job, out _);
    }

    internal static void RollbackFailedDispense(Pawn pawn, Thing source)
    {
        if (MatchesCurrentSource(pawn, source))
        {
            Cleanup(pawn, pawn.CurJob);
        }
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
        if (job is null || !Sessions.TryGetValue(job, out var session))
        {
            return;
        }

        if (ActiveIngestions.TryGetValue(session.Pawn, out var active) &&
            ReferenceEquals(active.Job, job) &&
            !active.Lifecycle.ShouldCancelDiningSessionOnJobCleanup)
        {
            return;
        }

        session.Cancel();
        Sessions.Remove(job);
        if (PawnSessions.TryGetValue(session.Pawn, out var pawnSession) && ReferenceEquals(pawnSession, session))
        {
            PawnSessions.Remove(session.Pawn);
        }
    }

    internal static Thing? FindMicrowave(Pawn pawn, Thing meal)
    {
        if (!TemperatureOwnership.ImmersiveChefsFeaturesActive ||
            !ImmersiveChefsMod.Settings.MealTemperatureEnabled ||
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
        WareRequirementMode mode = WareRequirementMode.Prefer,
        bool allowPersonalInventory = false,
        MealComplexity? plateComplexity = null)
    {
        var colonyCandidates = pawn.Map.listerThings.AllThings
            .Where(thing => thing.def.GetModExtension<KitchenwareExtension>()?.product == product)
            .Where(thing => product != KitchenwareProduct.Plate ||
                            PlateMaterialEligibilityRuntime.Allows(thing, plateComplexity))
            .Where(thing => !thing.IsForbidden(pawn) && pawn.CanReach(thing, PathEndMode.Touch, Danger.Some))
            .Where(thing => pawn.CanReserve(thing, 1, 1))
            .Select(thing => new ServiceWareCandidate<Thing>(
                thing,
                (thing as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true,
                KitchenwareRuntime.ServiceScore(thing) -
                (thing.PositionHeld.DistanceToSquared(pawn.PositionHeld) * 0.01f)))
            .ToList();
        var personalCandidates = allowPersonalInventory && pawn.inventory is not null
            ? pawn.inventory.innerContainer.InnerListForReading
                .Where(thing => !thing.Destroyed && thing.stackCount > 0)
                .Where(thing => thing.def.GetModExtension<KitchenwareExtension>()?.product == product)
                .Where(thing => product != KitchenwareProduct.Plate ||
                                PlateMaterialEligibilityRuntime.Allows(thing, plateComplexity))
                .Select(thing => new ServiceWareCandidate<Thing>(
                    thing,
                    (thing as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true,
                    KitchenwareRuntime.ServiceScore(thing)))
                .ToList()
            : new List<ServiceWareCandidate<Thing>>();
        var selected = GuestWareSelectionPolicy.Select(
            colonyCandidates,
            personalCandidates,
            allowPersonalInventory,
            mode,
            ImmersiveChefsMod.Settings.DirtyWareFallback,
            emergency);
        if (selected is null || ReferenceEquals(selected.holdingOwner, pawn.inventory?.innerContainer))
        {
            return selected;
        }

        return pawn.Reserve(selected, job, 1, 1) ? selected : null;
    }

    private static bool MayUsePersonalInventory(Pawn pawn)
    {
        var playerFaction = Faction.OfPlayer;
        var pawnFaction = pawn.Faction;
        var ordinaryNonHostileGuest = playerFaction is not null && pawnFaction is not null &&
                                      pawnFaction != playerFaction &&
                                      !pawnFaction.HostileTo(playerFaction) &&
                                      !pawn.IsPrisonerOfColony;
        var arrivedHospitalityGuest =
            ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.Hospitality) &&
            HospitalityAdapter.IsArrivedGuest(pawn);
        return GuestWareSelectionPolicy.MayUsePersonalInventory(
            ordinaryNonHostileGuest,
            arrivedHospitalityGuest);
    }

    private static Thing? SelectTravelWare(
        Caravan caravan,
        KitchenwareProduct product,
        bool emergency,
        WareRequirementMode mode,
        MealComplexity? plateComplexity = null)
    {
        var candidates = caravan.AllThings
            .Where(thing => !thing.Destroyed && thing.stackCount > 0)
            .Where(thing => thing.def.GetModExtension<KitchenwareExtension>()?.product == product)
            .Where(thing => product != KitchenwareProduct.Plate ||
                            PlateMaterialEligibilityRuntime.Allows(thing, plateComplexity))
            .Select(thing => new ServiceWareCandidate<Thing>(
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
