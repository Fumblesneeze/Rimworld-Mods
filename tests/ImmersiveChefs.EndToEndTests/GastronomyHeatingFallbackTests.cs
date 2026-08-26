using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.gastronomy-heating-source-loss-delivers",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "Orion.Hospitality",
    "Orion.CashRegister",
    "Orion.Gastronomy",
    "fumblesneeze.immersivechefs",
    MaxFrames = 5_000,
    MaxGameTicks = 16_000,
    MaxWallClockSeconds = 180)]
public sealed class GastronomyHeatingSourceLossDeliversTest : IRimWorldEndToEndTest
{
    private Pawn patron = null!;
    private Pawn waiter = null!;
    private Pawn sourceController = null!;
    private ThingWithComps meal = null!;
    private ThingWithComps plate = null!;
    private ThingWithComps stove = null!;
    private ThingWithComps powerController = null!;
    private Job serveJob = null!;
    private int qualityBefore;
    private bool normalDeliveryObserved;
    private int sourceLossTick = -1;
    private Map map = null!;
    private HashSet<Thing> preexistingThings = null!;
    private object? restaurant;
    private object? restaurantsManager;
    private bool restaurantWasPreexisting;
    private bool restaurantWasOpen;
    private float restaurantGuestPrice;
    private HashSet<object>? restaurantDiningSpots;
    private ThingWithComps? cashRegister;

    public void Arrange(IEndToEndContext context)
    {
        var settings = ImmersiveChefsMod.Settings;
        var priorWareMode = settings.WareRequirementMode;
        var priorTemperatureEnabled = settings.MealTemperatureEnabled;
        var priorThreshold = settings.AutoMicrowaveBelow;
        context.DeferCleanup(() =>
        {
            settings.WareRequirementMode = priorWareMode;
            settings.MealTemperatureEnabled = priorTemperatureEnabled;
            settings.AutoMicrowaveBelow = priorThreshold;
        });
        settings.WareRequirementMode = WareRequirementMode.Off;
        settings.MealTemperatureEnabled = true;
        settings.AutoMicrowaveBelow = 30f;

        map = Current.Game.CurrentMap;
        preexistingThings = map.listerThings.AllThings.ToHashSet();
        context.DeferCleanup(Cleanup);
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        var restaurantType = Type.GetType(
            "Gastronomy.Restaurant.RestaurantController, Gastronomy",
            throwOnError: true)!;
        var managerType = Type.GetType(
            "Gastronomy.Restaurant.RestaurantsManager, Gastronomy",
            throwOnError: true)!;
        restaurantsManager = map.components.Single(managerType.IsInstanceOfType);
        var restaurants = (IList)(managerType
            .GetField("restaurants", BindingFlags.Public | BindingFlags.Instance)?
            .GetValue(restaurantsManager) ?? throw new EndToEndAssertionException(
            "Gastronomy's manager must expose its restaurant list before fixture mutation."));
        var preexistingRestaurants = restaurants.Cast<object>()
            .Where(restaurantType.IsInstanceOfType)
            .ToHashSet();
        var preexistingRestaurant = preexistingRestaurants.FirstOrDefault();
        var preexistingRestaurantWasOpen = preexistingRestaurant is not null &&
                                           (bool)(restaurantType
                                               .GetField("openForBusiness", BindingFlags.Public | BindingFlags.Instance)?
                                               .GetValue(preexistingRestaurant) ?? false);
        var preexistingRestaurantGuestPrice = preexistingRestaurant is null
            ? 0f
            : (float)(restaurantType
                .GetField("guestPricePercentage", BindingFlags.Public | BindingFlags.Instance)?
                .GetValue(preexistingRestaurant) ?? 0f);
        var preexistingDiningSpots = preexistingRestaurant is null
            ? null
            : ((IEnumerable)(restaurantType
                    .GetField("diningSpots", BindingFlags.Public | BindingFlags.Instance)?
                    .GetValue(preexistingRestaurant) ?? Array.Empty<object>()))
                .Cast<object>()
                .ToHashSet();

        DispenserE2EFixture.SpawnConduitGrid(map, center, 5, 5);
        powerController = DispenserE2EFixture.SpawnBuilding(
            map,
            "WoodFiredGenerator",
            center + new IntVec3(3, 0, 3));
        var service = DispenserE2EFixture.CreateGastronomyGuestServiceFixture(map, center);
        patron = service.Guest;
        waiter = service.Waiter;
        cashRegister = service.CashRegister;
        restaurant = service.Restaurant;
        restaurantWasPreexisting = preexistingRestaurants.Contains(restaurant);
        if (restaurantWasPreexisting)
        {
            EndToEndAssert.True(
                ReferenceEquals(restaurant, preexistingRestaurant),
                "The fixture must mutate the captured preexisting restaurant controller.");
            restaurantWasOpen = preexistingRestaurantWasOpen;
            restaurantGuestPrice = preexistingRestaurantGuestPrice;
            restaurantDiningSpots = preexistingDiningSpots;
        }
        FoodSearchE2EFixture.SetHunger(patron, 0.05f);
        FoodSearchE2EFixture.SetHunger(waiter, 1f);
        sourceController = FoodSearchE2EFixture.CreateColonist("Source Controller");
        sourceController.workSettings.SetPriority(
            DefDatabase<WorkTypeDef>.GetNamed("BasicWorker"),
            1);
        for (var hour = 0; hour < 24; hour++)
        {
            sourceController.timetable?.SetAssignment(hour, TimeAssignmentDefOf.Work);
        }
        GenSpawn.Spawn(sourceController, center + new IntVec3(2, 0, -2), map);

        stove = DispenserE2EFixture.SpawnBuilding(
            map,
            "ElectricStove",
            center + new IntVec3(0, 0, -3));
        DispenserE2EFixture.SettlePower(map, new[] { stove }, 300);
        var source = MealHeatingSource.TryCreate(stove);
        EndToEndAssert.True(
            source is { Kind: MealHeatingSourceKind.Stove, IsOperational: true },
            "The restaurant fixture must begin with one actually emitting fueled stove.");

        meal = FoodSearchE2EFixture.MakePlatedMeal(
            ThingDefOf.MealSimple,
            ThingDefOf.Steel,
            out plate);
        meal.GetComp<CompCulinaryState>().ReplaceServings(new[]
        {
            new CulinaryServingRecord(
                qualityScore: 80,
                temperatureCelsius: -5f,
                contamination: ContaminationSources.None,
                microwaveReheatCount: 0,
                lastThermalTick: Find.TickManager.TicksGame)
        });
        qualityBefore = meal.GetComp<CompCulinaryState>()
            .PeekCurrentServingWithoutThermalUpdate()!.QualityScore;
        GenSpawn.Spawn(meal, center + new IntVec3(0, 0, 3), map);
        DispenserE2EFixture.PlaceInNativeRestaurantStock(
            meal,
            service.CashRegister,
            service.Restaurant);
        DispenserE2EFixture.StartNativeGastronomyDining(
            patron,
            service.DiningSpot,
            service.Restaurant);
        DispenserE2EFixture.StartNativeGastronomyService(
            waiter,
            patron,
            meal,
            service.Restaurant);
        serveJob = waiter.CurJob ?? throw new EndToEndAssertionException(
            "The native Gastronomy service job was not admitted.");
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new CameraActionStep(
            "frame the cold restaurant order and fallback stove",
            new[] { patron.ThingID, waiter.ThingID, meal.ThingID, stove.ThingID },
            paddingPixels: 190);
        yield return new TimeControlActionStep(
            "run the native waiter toward stove reheating",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "waiter begins the real stove heating toil",
            _ => IsActivelyHeating(),
            new EndToEndDeadline(2_000, 5_000, TimeSpan.FromSeconds(60)));
        yield return new TimeControlActionStep(
            "pause while the waiter heats the restaurant order",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep(
            "observe Gastronomy waiter heating the exact order at the stove",
            new[] { waiter.ThingID, stove.ThingID },
            paddingPixels: 170);

        var toggles = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { powerController.ThingID }, Array.Empty<string>())
            .Where(option =>
                !option.Disabled &&
                option.Interaction == EndToEndGizmoInteraction.Toggle &&
                option.ToggleState == true &&
                string.Equals(
                    option.HotKeyDefName,
                    "Command_TogglePower",
                    StringComparison.Ordinal))
            .ToArray();
        EndToEndAssert.Equal(1, toggles.Length, "The stove's power source must expose one enabled native power toggle.");
        yield return new GizmoActionStep(
            "switch off the stove's power source through its native command",
            new[] { powerController.ThingID },
            toggles[0].RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: toggles[0].StableId);
        yield return new WindowAcceptActionStep(
            "confirm the exact native power-designation message box",
            "Verse.Dialog_MessageBox");
        yield return new AssertionStep(
            "the native power-source toggle creates a real flick designation",
            _ => EndToEndAssert.NotNull(
                map.designationManager.DesignationOn(
                    powerController,
                    DefDatabase<DesignationDef>.GetNamed("Flick")),
                "The power-source toggle must create RimWorld's real Flick designation."));
        var flickOptions = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(sourceController.ThingID, powerController.ThingID)
            .Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("flick", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(
            1,
            flickOptions.Length,
            "The source controller must expose one enabled native prioritize-flick action.");
        yield return new FloatMenuActionStep(
            "prioritize the power source's native flick order",
            sourceController.ThingID,
            powerController.ThingID,
            flickOptions[0].StableId);
        yield return new TimeControlActionStep(
            "let a colonist perform the native flick order",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the completed player flick order makes the stove unusable",
            _ => ObserveSourceLoss(),
            new EndToEndDeadline(1_800, 5_000, TimeSpan.FromSeconds(60)));
        yield return new WaitUntilStep(
            "waiter falls back to normal delivery without abandoning the order",
            _ => ObserveNormalDelivery(),
            new EndToEndDeadline(2_400, 6_000, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep(
            "pause on the delivered unheated restaurant order",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new ScreenshotStep(
            "observe the same order delivered after stove loss",
            new[] { patron.ThingID, waiter.ThingID },
            paddingPixels: 180);
        yield return new AssertionStep(
            "source loss preserves the exact unheated meal and plate",
            _ =>
            {
                var serving = meal.GetComp<CompCulinaryState>().PeekCurrentServingWithoutThermalUpdate();
                EndToEndAssert.NotNull(serving, "The delivered order must retain its culinary serving.");
                EndToEndAssert.Equal(
                    qualityBefore,
                    serving!.QualityScore,
                    "Interrupted waiter heating must not reduce culinary quality.");
                EndToEndAssert.Equal(
                    0,
                    serving.MicrowaveReheatCount,
                    "Interrupted waiter heating must not record a microwave cycle.");
                EndToEndAssert.True(
                    ReferenceEquals(plate, meal.GetComp<CompEmbeddedWare>().PeekPlateThing()),
                    "Normal delivery after source loss must retain the exact embedded plate.");
                EndToEndAssert.True(
                    normalDeliveryObserved,
                    "The exact native Gastronomy service must reach normal delivery.");
            });
        yield return new CheckpointStep(
            "Gastronomy source-loss fallback result",
            _ => new System.Collections.Generic.Dictionary<string, string>
            {
                ["normalDeliveryObserved"] = normalDeliveryObserved.ToString(),
                ["sameServeJob"] = ReferenceEquals(waiter.CurJob, serveJob).ToString(),
                ["qualityAfter"] = meal.GetComp<CompCulinaryState>()
                    .PeekCurrentServingWithoutThermalUpdate()!.QualityScore.ToString(),
                ["samePlate"] = ReferenceEquals(
                    plate,
                    meal.GetComp<CompEmbeddedWare>().PeekPlateThing()).ToString()
            });
    }

    private bool IsActivelyHeating()
    {
        if (!ReferenceEquals(waiter.CurJob, serveJob) ||
            !ReferenceEquals(waiter.carryTracker?.CarriedThing, meal))
        {
            return false;
        }

        var toil = CurrentToil(waiter);
        return toil is { defaultCompleteMode: ToilCompleteMode.Delay, defaultDuration: 450 };
    }

    private bool ObserveNormalDelivery()
    {
        var session = DiningSessionRegistry.Current(patron);
        if (session?.ServingPawn is not null)
        {
            EndToEndAssert.True(
                ReferenceEquals(session.ServingPawn, waiter),
                "The delivered session must retain the exact native waiter.");
            normalDeliveryObserved = true;
            return true;
        }

        if (!ReferenceEquals(waiter.CurJob, serveJob))
        {
            var endedSession = DiningSessionRegistry.Current(patron);
            throw new EndToEndAssertionException(
                "The Gastronomy service job ended before the interrupted order reached normal delivery; " +
                "waiterJob=" + (waiter.CurJobDef?.defName ?? "null") +
                ", patronJob=" + (patron.CurJobDef?.defName ?? "null") +
                ", mealSpawned=" + meal.Spawned +
                ", mealDestroyed=" + meal.Destroyed +
                ", mealHolder=" + (meal.ParentHolder?.GetType().FullName ?? "null") +
                ", session=" + (endedSession is not null) +
                ", servingPawn=" + (endedSession?.ServingPawn?.LabelShort ?? "null") + ".");
        }

        if (sourceLossTick >= 0 && Find.TickManager.TicksGame >= sourceLossTick + 600)
        {
            var toil = CurrentToil(waiter);
            throw new EndToEndAssertionException(
                "The exact Gastronomy service remained stuck after active-source loss; toil=" +
                (toil?.debugName ?? "null") +
                ", carriedMeal=" + ReferenceEquals(waiter.carryTracker?.CarriedThing, meal) +
                ", mealSpawned=" + meal.Spawned +
                ", waiterPosition=" + waiter.PositionHeld +
                ", mealPosition=" + meal.PositionHeld + ".");
        }

        return false;
    }

    private bool ObserveSourceLoss()
    {
        if (MealHeatingSource.TryCreate(stove)?.IsOperational != false)
        {
            return false;
        }

        sourceLossTick = Find.TickManager.TicksGame;
        return true;
    }

    private static Toil? CurrentToil(Pawn pawn)
    {
        var driver = pawn.jobs.curDriver;
        return driver is null
            ? null
            : typeof(JobDriver).GetProperty(
                    "CurToil",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                ?.GetValue(driver) as Toil;
    }

    private void Cleanup()
    {
        if (waiter is not null && serveJob is not null)
        {
            GastronomyAdapter.Cleanup(waiter, serveJob);
        }

        if (restaurant is not null)
        {
            var restaurantType = restaurant.GetType();
            if (patron is not null)
            {
                restaurantType.GetProperty("Orders", BindingFlags.Public | BindingFlags.Instance)?
                    .GetValue(restaurant)?
                    .GetType()
                    .GetMethod(
                        "CancelOrder",
                        BindingFlags.Public | BindingFlags.Instance,
                        null,
                        new[] { typeof(Pawn) },
                        null)?
                    .Invoke(
                        restaurantType.GetProperty("Orders", BindingFlags.Public | BindingFlags.Instance)?
                            .GetValue(restaurant),
                        new object[] { patron });
            }

            if (cashRegister is not null)
            {
                restaurantType.GetMethod(
                        "RemoveRegister",
                        BindingFlags.Public | BindingFlags.Instance,
                        null,
                        new[] { cashRegister.GetType() },
                        null)?
                    .Invoke(restaurant, new object[] { cashRegister });
            }

            if (restaurantWasPreexisting)
            {
                restaurantType.GetField("openForBusiness", BindingFlags.Public | BindingFlags.Instance)?
                    .SetValue(restaurant, restaurantWasOpen);
                restaurantType.GetField("guestPricePercentage", BindingFlags.Public | BindingFlags.Instance)?
                    .SetValue(restaurant, restaurantGuestPrice);
                var diningSpots = restaurantType
                    .GetField("diningSpots", BindingFlags.Public | BindingFlags.Instance)?
                    .GetValue(restaurant);
                if (diningSpots is not null)
                {
                    diningSpots.GetType().GetMethod("Clear", Type.EmptyTypes)?
                        .Invoke(diningSpots, Array.Empty<object>());
                    var add = diningSpots.GetType().GetMethod("Add");
                    foreach (var prior in restaurantDiningSpots ?? Enumerable.Empty<object>())
                    {
                        add?.Invoke(diningSpots, new[] { prior });
                    }
                }
            }
        }

        if (sourceController is not null && !sourceController.Destroyed)
        {
            sourceController.Destroy(DestroyMode.Vanish);
        }

        if (patron is not null && map is not null)
        {
            var managerType = Type.GetType("Gastronomy.Restaurant.RestaurantsManager, Gastronomy");
            var manager = map.components.FirstOrDefault(component => managerType?.IsInstanceOfType(component) == true);
            if (managerType?.GetField("diningAt", BindingFlags.NonPublic | BindingFlags.Instance)?
                    .GetValue(manager) is IDictionary diningAt)
            {
                diningAt.Remove(patron);
            }

            var guestComp = patron.AllComps.FirstOrDefault(comp =>
                comp.GetType().FullName == "Hospitality.CompGuest");
            guestComp?.GetType().GetMethod(
                    "Leave",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { typeof(bool) },
                    null)?
                .Invoke(guestComp, new object[] { true });
        }

        waiter?.jobs?.StopAll();
        patron?.jobs?.StopAll();
        var createdThings = map?.listerThings.AllThings
            .Where(thing => preexistingThings is null || !preexistingThings.Contains(thing))
            .ToArray() ?? Array.Empty<Thing>();
        foreach (var thing in createdThings)
        {
            if (!thing.Destroyed)
            {
                thing.Destroy(DestroyMode.Vanish);
            }
        }

        if (restaurant is not null && restaurantWasPreexisting)
        {
            restaurant.GetType().GetMethod(
                    "RescanDiningSpots",
                    BindingFlags.Public | BindingFlags.Instance)?
                .Invoke(restaurant, Array.Empty<object>());
        }
        else if (restaurant is not null && restaurantsManager is not null)
        {
            restaurantsManager.GetType().GetMethod(
                    "DeleteRestaurant",
                    BindingFlags.Public | BindingFlags.Instance,
                    null,
                    new[] { restaurant.GetType() },
                    null)?
                .Invoke(restaurantsManager, new[] { restaurant });
        }
    }
}
