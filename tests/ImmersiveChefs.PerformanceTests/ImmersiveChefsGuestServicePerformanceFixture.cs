using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using RimWorldDevGateway.PerformanceTesting;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ImmersiveChefs.PerformanceTests;

public abstract partial class ImmersiveChefsColonyBenchmark
{
    private bool guestServiceActive;
    private Pawn? guestServiceGuest;
    private Pawn? guestServiceWaiter;
    private Thing? guestServiceDiningSpot;
    private Building? guestServiceDiningChair;
    private readonly List<Building> guestServiceDiningChairs = new();
    private ThingWithComps? guestServiceCashRegister;
    private ThingWithComps? guestServiceMeal;
    private ThingWithComps? guestServicePlate;
    private ThingWithComps? guestServiceColonyCutlery;
    private ThingWithComps? guestServicePersonalCutlery;
    private Thing? guestServiceTravelFood;
    private object? guestServiceRestaurant;
    private GuestServiceObserver? guestServiceObserver;
    private string guestServiceStartFingerprint = string.Empty;
    private string guestServiceStartState = string.Empty;
    private FieldInfo? commonSenseCleaningSetting;
    private bool priorCommonSenseCleaning;

    private void DetectGuestServiceBranch(IEndToEndContext context)
    {
        guestServiceActive = HasActivePackage("orion.hospitality") &&
                             HasActivePackage("orion.cashregister") &&
                             HasActivePackage("orion.gastronomy") &&
                             HasActivePackage("avilmask.commonsense");
        if (!guestServiceActive) return;
        commonSenseCleaningSetting = RequireType("CommonSense.Settings").GetField(
            "adv_cleaning_ingest", BindingFlags.Public | BindingFlags.Static);
        if (commonSenseCleaningSetting?.FieldType != typeof(bool))
            throw new MissingFieldException("CommonSense.Settings", "adv_cleaning_ingest:Boolean");
        priorCommonSenseCleaning = (bool)commonSenseCleaningSetting.GetValue(null)!;
        commonSenseCleaningSetting.SetValue(null, true);
        var setting = commonSenseCleaningSetting;
        var prior = priorCommonSenseCleaning;
        context.DeferCleanup(() => setting.SetValue(null, prior));
    }

    private void BuildGuestServiceInfrastructure()
    {
        if (!guestServiceActive) return;
        var dining = roomCenters[1];
        var restaurantCenter = dining + new IntVec3(7, 0, -5);
        foreach (var cell in CellRect.CenteredOn(restaurantCenter, 5))
            if (cell.InBounds(map!)) map!.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
        var table = SpawnBuilding(
            "Table1x2c", restaurantCenter, Rot4.North, ThingDefOf.WoodLog);
        var spotCell = table.OccupiedRect().Cells.OrderBy(cell => cell.z).First();
        guestServiceDiningSpot = Spawn(
            ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("Gastronomy_DiningSpot")),
            spotCell);
        var primaryChairCell = spotCell + IntVec3.South;
        guestServiceDiningChair = (Building)SpawnBuilding(
            "DiningChair", primaryChairCell, Rot4.North, ThingDefOf.WoodLog);
        guestServiceDiningChairs.Add(guestServiceDiningChair);

        var registerSupport = SpawnBuilding(
            "Table1x2c", dining + new IntVec3(-6, 0, -5), Rot4.North, ThingDefOf.WoodLog);
        guestServiceCashRegister = (ThingWithComps)SpawnBuilding(
            "CashRegister_CashRegister",
            registerSupport.OccupiedRect().Cells.OrderBy(cell => cell.z).First(),
            Rot4.North);
    }

    private Pawn CreateGuestServicePawn(string role, int index)
    {
        var player = Faction.OfPlayer;
        var faction = Find.FactionManager.AllFactionsListForReading
            .Where(candidate => candidate != player &&
                                !candidate.HostileTo(player) &&
                                !player.HostileTo(candidate) &&
                                !candidate.def.hidden)
            .OrderBy(candidate => candidate.def.defName, StringComparer.Ordinal)
            .FirstOrDefault() ?? throw new InvalidOperationException(
            "The guest-service performance branch found no deterministic non-hostile guest faction.");
        for (var attempt = 0; attempt < 128; attempt++)
        {
            var pawn = PawnGenerator.GeneratePawn(new PawnGenerationRequest(
                PawnKindDefOf.Colonist,
                faction,
                forceGenerateNewPawn: true,
                canGeneratePawnRelations: false,
                fixedBiologicalAge: 30f,
                fixedChronologicalAge: 30f));
            if (pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) >= 0.8f &&
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) >= 0.8f)
            {
                NormalizeHuman(pawn, role, index);
                pawn.workSettings ??= new Pawn_WorkSettings(pawn);
                pawn.workSettings.EnableAndInitialize();
                pawn.Name = new NameTriple("Perf", "hospitalityGuest", "Fixture" + attempt);
                pawn.needs.food.CurLevelPercentage = 0.8f;
                pawn.needs.rest.CurLevelPercentage = 0.95f;
                // Non-player PawnGenerator paths do not guarantee the policy tracker
                // that Hospitality's public FoodUtility dereferences for arrived guests.
                pawn.foodRestriction ??= new Pawn_FoodRestrictionTracker(pawn);
                var guestFoodPolicy = new FoodPolicy(9_812, "Immersive Chefs performance guest");
                pawn.foodRestriction.CurrentFoodPolicy = guestFoodPolicy;
                guestServiceGuest = pawn;
                return pawn;
            }
            pawn.Destroy(DestroyMode.Vanish);
        }
        throw new InvalidOperationException("Could not generate the deterministic Hospitality guest.");
    }

    private void BuildGuestServiceFixtures(IEndToEndContext context)
    {
        if (!guestServiceActive) return;
        if (guestServiceGuest is null || guestServiceCashRegister is null ||
            guestServiceDiningSpot is null || map is null)
            throw new InvalidOperationException("The guest-service infrastructure is incomplete.");

        guestServiceWaiter = pawnRoles
            .Where(pair => pair.Value == "cleaner" && pair.Key != guestServiceGuest)
            .Select(pair => pair.Key)
            .First();
        var waitingWork = DefDatabase<WorkTypeDef>.GetNamed("Gastronomy_Waiting");
        if (guestServiceWaiter.WorkTypeIsDisabled(waitingWork))
            throw new InvalidOperationException("The deterministic guest-service waiter cannot perform Waiting work.");
        guestServiceWaiter.workSettings.SetPriority(waitingWork, 1);

        var managerType = RequireType("Gastronomy.Restaurant.RestaurantsManager");
        var restaurantType = RequireType("Gastronomy.Restaurant.RestaurantController");
        var manager = map.components.Single(managerType.IsInstanceOfType);
        var restaurants = managerType.GetField("restaurants", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(manager) as IList;
        if (restaurants is not { Count: > 0 } || restaurants[0] is not { } restaurant ||
            !restaurantType.IsInstanceOfType(restaurant))
            throw new MissingMemberException(managerType.FullName, "restaurants[0]:RestaurantController");
        guestServiceRestaurant = restaurant;
        RequireMethod(restaurantType, "LinkRegister", guestServiceCashRegister.GetType())
            .Invoke(restaurant, new object[] { guestServiceCashRegister });
        // Keep service closed through the ordinary warm-up. Otherwise Gastronomy can
        // legitimately consume/rewrite the staged stock before the deterministic
        // sample-start normalization has armed its exact guest order.
        RequireField(restaurantType, "openForBusiness", typeof(bool)).SetValue(restaurant, false);
        RequireField(restaurantType, "guestPricePercentage", typeof(float)).SetValue(restaurant, 0f);

        var shifts = guestServiceCashRegister.GetType()
            .GetField("shifts", BindingFlags.Public | BindingFlags.Instance)?.GetValue(guestServiceCashRegister) as IList;
        if (shifts is not { Count: > 0 } || shifts[0] is not { } shift)
            throw new MissingMemberException(guestServiceCashRegister.GetType().FullName, "shifts[0]");
        var assigned = shift.GetType().GetField("assigned", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(shift) as IList;
        var timetable = shift.GetType().GetField("timetable", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(shift);
        var times = timetable?.GetType().GetField("times", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(timetable) as IList;
        if (assigned is null || times is not { Count: 24 })
            throw new MissingMemberException(shift.GetType().FullName, "assigned/timetable.times[24]");
        assigned.Add(guestServiceWaiter);
        for (var hour = 0; hour < times.Count; hour++) times[hour] = true;
        RequireMethod(restaurantType, "RescanDiningSpots").Invoke(restaurant, Array.Empty<object>());

        RegisterHospitalityGuest(guestServiceGuest);
        if (!HospitalityAdapter.IsArrivedGuest(guestServiceGuest))
            throw new InvalidOperationException("Hospitality did not recognize the exact guest as arrived.");

        guestServiceMeal = MakePlatedMeal(18f);
        guestServicePlate = guestServiceMeal.GetComp<CompEmbeddedWare>()?.PeekPlateThing() as ThingWithComps ??
                            throw new InvalidOperationException("The guest meal lost its exact embedded plate.");
        guestServiceColonyCutlery = MakeWare("ImmersiveChefs_Cutlery", false);
        guestServicePersonalCutlery = MakeWare("ImmersiveChefs_Cutlery", false);
        if (guestServiceGuest.inventory?.innerContainer.TryAdd(
                guestServicePersonalCutlery, canMergeWithExistingStacks: false) != true)
            throw new InvalidOperationException("Could not give the guest its exact personal cutlery.");
        // Hospitality's visitor think tree may seek its own provisions after the
        // Gastronomy meal. Retain one excluded travel meal in the guest inventory:
        // it prevents broken ScroungeFood reservations without exercising or
        // changing the plated-meal/tableware path measured by this branch.
        guestServiceTravelFood = ThingMaker.MakeThing(ThingDefOf.MealSurvivalPack);
        if (guestServiceGuest.inventory?.innerContainer.TryAdd(
                guestServiceTravelFood, canMergeWithExistingStacks: false) != true)
            throw new InvalidOperationException("Could not give the guest its exact travel provisions.");
        Spawn(guestServiceColonyCutlery, guestServiceCashRegister.Position + IntVec3.North);
        Spawn(guestServiceMeal, guestServiceCashRegister.Position + IntVec3.East);
        guestServiceObserver = new GuestServiceObserver(
            map,
            guestServiceGuest,
            guestServiceWaiter,
            guestServiceMeal,
            guestServicePlate,
            guestServiceColonyCutlery,
            guestServicePersonalCutlery);
        map.components.Add(guestServiceObserver);
        context.DeferCleanup(() =>
        {
            if (map is not null && guestServiceObserver is not null) map.components.Remove(guestServiceObserver);
        });
    }

    private void PrepareGuestServiceBranch()
    {
        if (!guestServiceActive) return;
        if (guestServiceGuest is null || guestServiceWaiter is null || guestServiceMeal is null ||
            guestServicePlate is null || guestServiceColonyCutlery is null ||
            guestServicePersonalCutlery is null || guestServiceRestaurant is null ||
            guestServiceDiningChair is null ||
            guestServiceCashRegister is null ||
            guestServiceObserver is null ||
            !GastronomyAdapter.Enabled || !CommonSenseAdapter.Enabled)
            throw new InvalidOperationException(
                "The guest-service branch did not retain its exact active optional-mod shape during warm-up.");
        // Preserve the ordinary warm-up position. Despawning a pawn with a live
        // Gastronomy job bypasses its normal reservation lifecycle and can leave
        // a chair looking available to preflight but unavailable to the job driver.
        // The native Dine job already owns pathing from the pawn's current cell.
        RegisterHospitalityGuest(guestServiceGuest);
        if (!HospitalityAdapter.IsArrivedGuest(guestServiceGuest))
            throw new InvalidOperationException("Hospitality did not retain the normalized guest as arrived.");
        RequireField(guestServiceRestaurant.GetType(), "openForBusiness", typeof(bool))
            .SetValue(guestServiceRestaurant, true);
        RequireField(guestServiceRestaurant.GetType(), "allowGuests", typeof(bool))
            .SetValue(guestServiceRestaurant, true);
        RequireField(guestServiceRestaurant.GetType(), "allowColonists", typeof(bool))
            .SetValue(guestServiceRestaurant, false);
        RequireMethod(guestServiceRestaurant.GetType(), "RescanDiningSpots")
            .Invoke(guestServiceRestaurant, Array.Empty<object>());
        PlaceInRestaurantStock(guestServiceMeal, guestServiceCashRegister, guestServiceRestaurant);
        // Thermal evolution during the ordinary warm-up is expected to vary with
        // process startup duration. Re-anchor this fixture-owned meal immediately
        // before sampling so every lens begins with the same culinary state.
        guestServiceMeal.GetComp<CompCulinaryState>()?.ReplaceServings(new[]
        {
            new CulinaryServingRecord(
                65,
                18f,
                ContaminationSources.None,
                0,
                Find.TickManager.TicksGame)
        });
        var spot = guestServiceDiningSpot!;
        var nativeChairs = new List<Building>();
        RequireMethod(spot.GetType(), "GetReservationSpots", typeof(List<Building>))
            .Invoke(spot, new object[] { nativeChairs });
        foreach (var candidate in nativeChairs)
        {
            map!.reservationManager.ReleaseAllForTarget(candidate);
            candidate.SetForbidden(false, warnOnFail: false);
        }
        // Keep the restaurant lane isolated from the rest of the stochastic
        // colony workload. Other pawns may sit in this chair during warm-up;
        // move only those fixture pawns away through the normal despawn/spawn
        // lifecycle before starting the measured native Dine job.
        foreach (var occupant in map!.mapPawns.AllPawnsSpawned
                     .Where(pawn => !ReferenceEquals(pawn, guestServiceGuest) &&
                                    pawn.Position == guestServiceDiningChair.Position)
                     .ToArray())
        {
            var fallback = CellFinder.RandomClosewalkCellNear(occupant.Position, map, 8,
                cell => cell.Standable(map) && cell.GetFirstBuilding(map) is null);
            map.pawnDestinationReservationManager.ReleaseAllClaimedBy(occupant);
            occupant.DeSpawn(DestroyMode.Vanish);
            GenSpawn.Spawn(occupant, fallback, map);
        }
        var eligibleChairs = nativeChairs
            .Where(chair => chair.Position.AdjacentToCardinal(spot.Position))
            .Where(chair => !chair.IsForbidden(guestServiceGuest))
            .Where(chair => guestServiceGuest.CanReserve(chair))
            .Where(chair => !chair.HostileTo(guestServiceGuest))
            .Where(chair => chair.IsSociallyProper(guestServiceGuest))
            .Where(chair => !chair.IsBurning())
            .Where(chair => chair.Position.GetDangerFor(guestServiceGuest, map) <= Danger.Some)
            .ToArray();
        if (eligibleChairs.Length == 0)
            throw new InvalidOperationException(
                "The exact Gastronomy dining spot exposes no eligible adjacent native chair.");
        if (!eligibleChairs.Contains(guestServiceDiningChair) ||
            !guestServiceDiningChairs.Any(eligibleChairs.Contains))
            throw new InvalidOperationException(
                "Gastronomy did not expose any fixture-owned adjacent dining chair.");
        var restaurantType = guestServiceRestaurant.GetType();
        var stockCell = guestServiceMeal.Position;
        var shifts = guestServiceCashRegister.GetType()
            .GetField("shifts", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(guestServiceCashRegister) as IList;
        var shift = shifts is { Count: > 0 } ? shifts[0] : null;
        var assignedCount = shift?.GetType().GetField("assigned", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(shift) is IList assigned ? assigned.Count : -1;
        var enabledHours = shift?.GetType().GetField("timetable", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(shift)?.GetType().GetField("times", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(shift?.GetType().GetField("timetable", BindingFlags.Public | BindingFlags.Instance)
                ?.GetValue(shift)) is IList hours
            ? hours.Cast<object>().Count(value => value is true)
            : -1;
        var exactPlateEmbedded = ReferenceEquals(
            guestServiceMeal.GetComp<CompEmbeddedWare>()?.PeekPlateThing(), guestServicePlate);
        var exactPersonalHeld = guestServiceGuest.inventory?.innerContainer.Contains(guestServicePersonalCutlery) == true;
        var exactTravelFoodHeld = guestServiceGuest.inventory?.innerContainer.Contains(guestServiceTravelFood) == true;
        if (!exactPlateEmbedded || !exactPersonalHeld || !exactTravelFoodHeld ||
            commonSenseCleaningSetting?.GetValue(null) is not true)
            throw new InvalidOperationException(
                "The guest-service branch lost exact ware ownership or Common Sense cleaning eligibility.");
        var serving = guestServiceMeal.GetComp<CompCulinaryState>()?.PeekCurrentServing()?.Capture() ??
                      throw new InvalidOperationException("The guest meal lost its culinary serving state.");
        var startRows = new[]
        {
            "guest|" + guestServiceGuest.kindDef.defName + "|faction=" +
            guestServiceGuest.Faction?.def.defName + "|arrived=true|cell=" + Cell(guestServiceGuest.Position),
            "waiter|" + guestServiceWaiter.kindDef.defName + "|waiting=" +
            guestServiceWaiter.workSettings.GetPriority(DefDatabase<WorkTypeDef>.GetNamed("Gastronomy_Waiting")) +
            "|cell=" + Cell(guestServiceWaiter.Position),
            "restaurant|type=" + restaurantType.FullName + "|open=" +
            RequireField(restaurantType, "openForBusiness", typeof(bool)).GetValue(guestServiceRestaurant) +
            "|price=" + RequireField(restaurantType, "guestPricePercentage", typeof(float))
                .GetValue(guestServiceRestaurant) + "|assigned=" + assignedCount + "|hours=" + enabledHours,
            "dining|spot=" + Cell(guestServiceDiningSpot!.Position) + "|register=" +
            Cell(guestServiceCashRegister.Position) + "|stock=" + Cell(stockCell),
            "meal|def=" + guestServiceMeal.def.defName + "|stack=" + guestServiceMeal.stackCount +
            "|quality=" + serving.QualityScore + "|temperature=" +
            serving.TemperatureCelsius.ToString("R", CultureInfo.InvariantCulture) +
            "|contamination=" + (int)serving.Contamination + "|reheats=" + serving.MicrowaveReheatCount,
            WareRow("plate", guestServicePlate, holder: exactPlateEmbedded ? "embedded" : "other"),
            WareRow("colony-cutlery", guestServiceColonyCutlery,
                holder: guestServiceColonyCutlery.Spawned ? "map:" + Cell(guestServiceColonyCutlery.Position) : "other"),
            WareRow("personal-cutlery", guestServicePersonalCutlery,
                holder: exactPersonalHeld ? "guest-inventory" : "other"),
            "travel-food|def=" + guestServiceTravelFood!.def.defName + "|holder=guest-inventory",
            "common-sense|adv_cleaning_ingest=" + commonSenseCleaningSetting.GetValue(null)
        };
        guestServiceStartState = string.Join(";", startRows);
        guestServiceStartFingerprint = HashRows(startRows);
    }

    private void ActivateGuestServiceBranch()
    {
        if (!guestServiceActive) return;
        if (guestServiceGuest is null || guestServiceWaiter is null || guestServiceMeal is null ||
            guestServiceDiningSpot is null || guestServiceRestaurant is null)
            throw new InvalidOperationException("The guest-service branch cannot activate an incomplete fixture.");
        // Dine only requires hunger below 90%. Starting just below that threshold
        // leaves this exact native order eligible while one simple meal fully
        // satisfies the guest for the complete measured window. That prevents
        // Hospitality from arranging unrelated follow-up ScroungeFood work.
        guestServiceGuest.needs.food.CurLevelPercentage = 0.85f;
        var diningStarted = false;
        var diningDiagnostic = "No dining attempt was made.";
        for (var attempt = 0; attempt < 4 && !diningStarted; attempt++)
            diningStarted = TryRegisterDining(
                guestServiceGuest, guestServiceDiningSpot, guestServiceRestaurant, out diningDiagnostic);
        if (!diningStarted)
            throw new InvalidOperationException(
                "Gastronomy did not retain the exact dining-spot job after four native attempts. " +
                diningDiagnostic);
        StartService(guestServiceWaiter, guestServiceGuest, guestServiceMeal, guestServiceRestaurant);
        guestServiceObserver?.BeginSample();
    }

    private bool GuestServiceCompleted() => guestServiceActive &&
        guestServiceObserver is { OrdersServed: > 0, ColonySettingsReturned: > 0, GastronomyClearingOwned: > 0 } &&
        !guestServiceObserver.HasCommonSenseConflict;

    private long CompletedOptionalBranchCount(ColonyObserver current) =>
        (processorDubsActive && ProcessorDubsCompleted(current) ? 1L : 0L) +
        (guestServiceActive && GuestServiceCompleted() ? 1L : 0L);

    private string OptionalBranchName() => processorDubsActive && guestServiceActive
        ? "processor-dubs+guest-service"
        : processorDubsActive ? "processor-dubs" : guestServiceActive ? "guest-service" : "base";

    private string ActiveWorkloadVersion()
    {
        var declaration = GetType().GetCustomAttribute<RimWorldPerformanceTestAttribute>(inherit: false) ??
                          throw new InvalidOperationException(
                              "The active performance fixture has no exact RimWorldPerformanceTest declaration.");
        if (string.IsNullOrWhiteSpace(declaration.WorkloadVersion))
            throw new InvalidOperationException(
                "The active performance fixture declaration has no workload version.");
        return declaration.WorkloadVersion;
    }

    private void CleanupGuestServiceBranch()
    {
        if (map is not null && guestServiceObserver is not null) map.components.Remove(guestServiceObserver);
        guestServiceActive = false;
        guestServiceGuest = null;
        guestServiceWaiter = null;
        guestServiceDiningSpot = null;
        guestServiceDiningChair = null;
        guestServiceDiningChairs.Clear();
        guestServiceCashRegister = null;
        guestServiceMeal = null;
        guestServicePlate = null;
        guestServiceColonyCutlery = null;
        guestServicePersonalCutlery = null;
        guestServiceTravelFood = null;
        guestServiceRestaurant = null;
        guestServiceObserver = null;
        guestServiceStartFingerprint = string.Empty;
        guestServiceStartState = string.Empty;
        commonSenseCleaningSetting = null;
        priorCommonSenseCleaning = false;
    }

    private static void PlaceInRestaurantStock(Thing meal, Thing register, object restaurant)
    {
        var fields = register.GetType().GetProperty("Fields", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(register) as IEnumerable;
        var stockCell = fields?.Cast<object>().OfType<IntVec3>()
            .Where(cell => cell.InBounds(meal.Map) && cell.GetFirstItem(meal.Map) is null)
            .OrderBy(cell => cell.DistanceToSquared(meal.Position)).FirstOrDefault() ?? IntVec3.Invalid;
        if (!stockCell.IsValid) throw new InvalidOperationException("The cash register exposes no empty stock field.");
        var targetMap = meal.Map;
        meal.DeSpawn(DestroyMode.Vanish);
        GenSpawn.Spawn(meal, stockCell, targetMap);
        var stock = restaurant.GetType().GetProperty("Stock", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(restaurant);
        RequireMethod(stock?.GetType(), "RefreshStock").Invoke(stock, Array.Empty<object>());
        if (!RestaurantStockContains(restaurant, meal))
            throw new InvalidOperationException("Gastronomy stock did not discover the exact guest meal.");
    }

    private static bool RestaurantStockContains(object restaurant, Thing meal)
    {
        var stock = restaurant.GetType().GetProperty("Stock", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(restaurant);
        return RequireMethod(stock?.GetType(), "IsAvailable", typeof(Thing))
            .Invoke(stock, new object[] { meal }) is true;
    }

    private static bool TryRegisterDining(
        Pawn guest,
        Thing spot,
        object restaurant,
        out string diagnostic)
    {
        var managerType = RequireType("Gastronomy.Restaurant.RestaurantsManager");
        var restaurantType = RequireType("Gastronomy.Restaurant.RestaurantController");
        var manager = guest.Map?.components.Single(managerType.IsInstanceOfType);
        // Cancelling an older Dine job may unregister the pawn in its finish
        // action, so stop first and then establish the exact new registration.
        // The installed driver owns both the dining-spot reservation and its
        // chair choice; pre-reserving the chair makes that native validator
        // intermittently reject its own otherwise eligible sole chair.
        guest.jobs.StopAll();
        var chairs = new List<Building>();
        RequireMethod(spot.GetType(), "GetReservationSpots", typeof(List<Building>))
            .Invoke(spot, new object[] { chairs });
        foreach (var chair in chairs)
            guest.Map!.reservationManager.ReleaseAllForTarget(chair);
        guest.Map!.pawnDestinationReservationManager.ReleaseAllClaimedBy(guest);
        RequireMethod(managerType, "RegisterDiningAt", typeof(Pawn), restaurantType)
            .Invoke(manager, new[] { guest, restaurant });
        var dine = JobMaker.MakeJob(DefDatabase<JobDef>.GetNamed("Gastronomy_Dine"), spot);
        dine.playerForced = true;
        guest.jobs.StartJob(dine, JobCondition.InterruptForced, resumeCurJobAfterwards: false,
            cancelBusyStances: true, tag: JobTag.Misc, preToilReservationsCanFail: true);
        // Gastronomy chooses and reserves the native chair in the Dine driver's
        // first toil, not in StartJob reservations. Execute that one pawn job
        // tick while the performance sample is still paused so the arranged
        // lane owns its chair before unrelated colonists resume ordinary work.
        if (guest.CurJob?.def == dine.def)
        {
            guest.jobs.JobTrackerTick();
        }
        if (guest.CurJob?.def == dine.def && ReferenceEquals(guest.CurJob?.targetA.Thing, spot))
        {
            diagnostic = "The exact native Dine job was retained.";
            return true;
        }
        diagnostic = BuildDiningReservationDiagnostic(guest, spot, chairs);
        Log.Message("[ImmersiveChefs guest-service fixture] " + diagnostic);
        return false;
    }

    private static string BuildDiningReservationDiagnostic(
        Pawn guest,
        Thing spot,
        IReadOnlyCollection<Building> preStartChairs)
    {
        var postStartChairs = new List<Building>();
        RequireMethod(spot.GetType(), "GetReservationSpots", typeof(List<Building>))
            .Invoke(spot, new object[] { postStartChairs });
        var chairRows = postStartChairs
            .Concat(preStartChairs)
            .Distinct()
            .OrderBy(chair => chair.ThingID, StringComparer.Ordinal)
            .Select(chair =>
            {
                var occupants = chair.Map?.mapPawns.AllPawnsSpawned
                    .Where(pawn => pawn.Position == chair.Position)
                    .Select(pawn => pawn.ThingID + ":" + (pawn.CurJobDef?.defName ?? "<none>"))
                    .OrderBy(value => value, StringComparer.Ordinal)
                    .ToArray() ?? Array.Empty<string>();
                return chair.ThingID + "@" + Cell(chair.Position) +
                       ":rot=" + chair.Rotation.AsInt +
                       ":adjacent=" + chair.Position.AdjacentToCardinal(spot.Position) +
                       ":forbidden=" + chair.IsForbidden(guest) +
                       ":canReserve=" + guest.CanReserve(chair) +
                       ":hostile=" + chair.HostileTo(guest) +
                       ":social=" + chair.IsSociallyProper(guest) +
                       ":burning=" + chair.IsBurning() +
                       ":danger=" + chair.Position.GetDangerFor(guest, guest.Map) +
                       ":occupants=" + string.Join(",", occupants);
            })
            .ToArray();
        return "currentJob=" + (guest.CurJobDef?.defName ?? "<none>") +
               ";currentTarget=" + (guest.CurJob?.targetA.Thing?.ThingID ?? "<none>") +
               ";preStartChairCount=" + preStartChairs.Count +
               ";postStartChairCount=" + postStartChairs.Count +
               ";chairs=[" + string.Join("|", chairRows) + "].";
    }

    private static void StartService(Pawn waiter, Pawn guest, Thing meal, object restaurant)
    {
        var orders = restaurant.GetType().GetProperty("Orders", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(restaurant);
        RequireMethod(orders?.GetType(), "CreateOrder", typeof(Pawn), typeof(Thing))
            .Invoke(orders, new object[] { guest, meal });
        var order = RequireMethod(orders?.GetType(), "GetOrderFor", typeof(Pawn))
            .Invoke(orders, new object[] { guest }) ??
                    throw new InvalidOperationException("Gastronomy did not retain the exact guest order.");
        RequireField(order.GetType(), "consumable", typeof(Thing)).SetValue(order, meal);
        RequireField(order.GetType(), "hasToBeMade", typeof(bool)).SetValue(order, false);
        var serve = JobMaker.MakeJob(
            DefDatabase<JobDef>.GetNamed("Gastronomy_Serve"), guest, meal, guest.CurJob!.targetA);
        serve.playerForced = true;
        waiter.jobs.StartJob(serve, JobCondition.InterruptForced, resumeCurJobAfterwards: false,
            cancelBusyStances: true, tag: JobTag.Misc, preToilReservationsCanFail: false);
        if (waiter.CurJob?.def != serve.def || !ReferenceEquals(waiter.CurJob?.targetA.Pawn, guest) ||
            !ReferenceEquals(waiter.CurJob?.targetB.Thing, meal))
            throw new InvalidOperationException(
                "Gastronomy did not retain the exact ordered service job; current=" +
                (waiter.CurJob?.def?.defName ?? "<none>") + ".");
    }

    private static Type RequireType(string name) => AccessTools.TypeByName(name) ??
        throw new TypeLoadException("Missing exact optional runtime type " + name + ".");

    private void RegisterHospitalityGuest(Pawn guest)
    {
        if (map is null) throw new InvalidOperationException("Hospitality guest registration requires the map.");
        var compGuestType = RequireType("Hospitality.CompGuest");
        var hospitalityMapType = RequireType("Hospitality.Hospitality_MapComponent");
        var compGuest = guest.AllComps.SingleOrDefault(compGuestType.IsInstanceOfType) ??
                        throw new InvalidOperationException("Hospitality did not attach CompGuest to the guest pawn.");
        var hospitalityMap = map.components.Single(hospitalityMapType.IsInstanceOfType);
        var lordField = RequireField(compGuestType, "lord", typeof(Lord));
        if (lordField.GetValue(compGuest) is not Lord)
        {
            var lordJobType = RequireType("Hospitality.LordJob_VisitColony");
            var constructor = lordJobType.GetConstructor(new[]
            {
                typeof(Faction), typeof(IntVec3), typeof(int), typeof(bool)
            });
            if (constructor?.Invoke(new object[] { guest.Faction!, guest.Position, 60_000, false })
                is not LordJob lordJob)
                throw new MissingMethodException(lordJobType.FullName,
                    ".ctor(Faction,IntVec3,Int32,Boolean)");
            var lord = LordMaker.MakeNewLord(guest.Faction, lordJob, map, new[] { guest });
            RequireMethod(compGuestType, "ResetForGuest", typeof(Lord))
                .Invoke(compGuest, new object[] { lord });
        }
        RequireMethod(hospitalityMapType, "OnGuestJoinedLate", typeof(Pawn))
            .Invoke(hospitalityMap, new object[] { guest });
        RequireMethod(compGuestType, "Arrive").Invoke(compGuest, Array.Empty<object>());
    }

    private static MethodInfo RequireMethod(Type? type, string name, params Type[] parameters)
    {
        var method = type?.GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static,
            null, parameters, null);
        return method ?? throw new MissingMethodException(type?.FullName ?? "<null>", name);
    }

    private static FieldInfo RequireField(Type type, string name, Type fieldType)
    {
        var field = type.GetField(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
        return field?.FieldType == fieldType ? field :
            throw new MissingFieldException(type.FullName, name + ":" + fieldType.Name);
    }

    private static string Cell(IntVec3 cell) => cell.x + "," + cell.z;

    private static string WareRow(string role, ThingWithComps ware, string holder)
    {
        var sanitation = ware.GetComp<CompSanitation>();
        return role + "|def=" + ware.def.defName + "|stuff=" + (ware.Stuff?.defName ?? string.Empty) +
               "|quality=" + (ware.GetComp<CompQuality>()?.Quality.ToString() ?? "none") +
               "|dirty=" + (sanitation?.IsDirty == true) + "|wild=" +
               (sanitation?.WashedInWildWater == true) + "|holder=" + holder;
    }
}

internal sealed class GuestServiceObserver : MapComponent
{
    private readonly Pawn guest;
    private readonly Pawn waiter;
    private readonly ThingWithComps meal;
    private readonly ThingWithComps plate;
    private readonly ThingWithComps colonyCutlery;
    private readonly ThingWithComps personalCutlery;
    private bool sampleStarted;
    private bool nativeServeObserved;
    private bool commonSenseConflict;

    internal GuestServiceObserver(Map map, Pawn guest, Pawn waiter, ThingWithComps meal,
        ThingWithComps plate, ThingWithComps colonyCutlery, ThingWithComps personalCutlery) : base(map)
    {
        this.guest = guest;
        this.waiter = waiter;
        this.meal = meal;
        this.plate = plate;
        this.colonyCutlery = colonyCutlery;
        this.personalCutlery = personalCutlery;
    }

    internal long OrdersServed { get; private set; }
    internal long ColonySettingsReturned { get; private set; }
    internal long GastronomyClearingOwned { get; private set; }
    internal bool HasCommonSenseConflict => commonSenseConflict || GuestClaimsExactWare();
    internal void BeginSample() => sampleStarted = true;

    public override void MapComponentTick()
    {
        if (!sampleStarted) return;
        if (Find.TickManager.TicksGame % 15 != 0) return;
        if (GuestClaimsExactWare()) commonSenseConflict = true;
        if (OrdersServed > 0 && ColonySettingsReturned > 0 && GastronomyClearingOwned > 0) return;
        if (waiter.CurJobDef?.defName == "Gastronomy_Serve" &&
            ReferenceEquals(waiter.CurJob?.GetTarget(TargetIndex.A).Pawn, guest) &&
            ReferenceEquals(waiter.CurJob?.GetTarget(TargetIndex.B).Thing, meal))
            nativeServeObserved = true;
        var session = DiningSessionRegistry.Current(guest);
        if (nativeServeObserved && session is not null &&
            ReferenceEquals(session.ServingPawn, waiter) &&
            ReferenceEquals(session.CarriedCutlery, colonyCutlery))
            OrdersServed = 1;

        var returned = meal.Destroyed && plate.Spawned && colonyCutlery.Spawned &&
                       plate.GetComp<CompSanitation>()?.IsDirty == true &&
                       colonyCutlery.GetComp<CompSanitation>()?.IsDirty == true &&
                       guest.inventory?.innerContainer.Contains(personalCutlery) == true &&
                       personalCutlery.GetComp<CompSanitation>()?.IsDirty == false &&
                       guest.inventory?.innerContainer.Contains(plate) != true &&
                       guest.inventory?.innerContainer.Contains(colonyCutlery) != true;
        if (returned) ColonySettingsReturned = 1;
        if (!returned) return;

        var clearing = map.GetComponent<MapComponent_GastronomyDishClearing>();
        if (clearing.OwnsExactWare(waiter, plate) && clearing.OwnsExactWare(waiter, colonyCutlery) &&
            !HasCommonSenseConflict)
            GastronomyClearingOwned = 1;

        static bool Cleans(Job? job, Thing exact) =>
            job?.def == ImmersiveChefsDefOf.ImmersiveChefs_DoDishes &&
            ReferenceEquals(job.GetTarget(TargetIndex.A).Thing, exact);
        var waiterPlate = Cleans(waiter.CurJob, plate) || waiter.jobs.jobQueue.Any(queued => Cleans(queued.job, plate));
        var waiterCutlery = Cleans(waiter.CurJob, colonyCutlery) ||
                             waiter.jobs.jobQueue.Any(queued => Cleans(queued.job, colonyCutlery));
        if (waiterPlate && waiterCutlery && !HasCommonSenseConflict)
            GastronomyClearingOwned = 1;
    }

    private bool GuestClaimsExactWare()
    {
        static bool Cleans(Job? job, Thing exact) =>
            job?.def == ImmersiveChefsDefOf.ImmersiveChefs_DoDishes &&
            ReferenceEquals(job.GetTarget(TargetIndex.A).Thing, exact);
        return Cleans(guest.CurJob, plate) || Cleans(guest.CurJob, colonyCutlery) ||
               guest.jobs.jobQueue.Any(queued => Cleans(queued.job, plate) || Cleans(queued.job, colonyCutlery));
    }
}
