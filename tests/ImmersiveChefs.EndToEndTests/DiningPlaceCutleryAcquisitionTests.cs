using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.dining-place-cutlery-acquisition",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "fumblesneeze.immersivechefs",
    MaxFrames = 16_000,
    MaxGameTicks = 40_000,
    MaxWallClockSeconds = 540)]
public sealed class DiningPlaceCutleryAcquisitionTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private ThingWithComps table = null!;
    private ThingWithComps inventoryTable = null!;
    private ThingWithComps sharedDiningStack = null!;
    private ThingWithComps startingAreaCutlery = null!;
    private ThingWithComps carriedCutlery = null!;
    private DiningCase first = null!;
    private DiningCase second = null!;
    private DiningCase inventory = null!;
    private bool simultaneousReservationsObserved;
    private Thing? firstAcquiredCutlery;
    private Thing? secondAcquiredCutlery;
    private IntVec3 firstEatSurface = IntVec3.Invalid;
    private IntVec3 secondEatSurface = IntVec3.Invalid;
    private IntVec3 inventoryEatSurface = IntVec3.Invalid;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);
        FoodSearchE2EFixture.UseStrictNonEmergencyDining(context);

        table = DispenserE2EFixture.SpawnBuilding(
            map,
            "Table1x2c",
            center + (IntVec3.East * 2));
        var tableCells = table.OccupiedRect().Cells.ToArray();
        EndToEndAssert.Equal(2, tableCells.Length,
            "The shared dining fixture requires the real two-cell Core table.");
        SpawnWestChairs(tableCells);

        inventoryTable = DispenserE2EFixture.SpawnBuilding(
            map,
            "Table1x2c",
            center + new IntVec3(2, 0, -3));
        var inventoryTableCells = inventoryTable.OccupiedRect().Cells.ToArray();
        SpawnWestChairs(inventoryTableCells);

        sharedDiningStack = MakeCleanCutleryStack(2);
        GenSpawn.Spawn(
            sharedDiningStack,
            tableCells[0] + (IntVec3.East * 2),
            map);
        startingAreaCutlery = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cutlery",
            ThingDefOf.Steel);
        GenSpawn.Spawn(
            startingAreaCutlery,
            center + new IntVec3(-1, 0, -2),
            map);

        first = CreateDiningCase(
            "First shared-stack diner",
            center + new IntVec3(-3, 0, 0),
            center + new IntVec3(-2, 0, 0));
        second = CreateDiningCase(
            "Second shared-stack diner",
            center + new IntVec3(-3, 0, 2),
            center + new IntVec3(-2, 0, 2));
        inventory = CreateDiningCase(
            "Carried-setting diner",
            center + new IntVec3(-3, 0, -2),
            center + new IntVec3(-2, 0, -2));
        carriedCutlery = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cutlery",
            ThingDefOf.Steel);
        EndToEndAssert.True(
            inventory.Diner.inventory!.innerContainer.TryAdd(
                carriedCutlery,
                canMergeWithExistingStacks: false),
            "The carried-setting diner must begin with one distinct clean cutlery unit.");

        AssertInitialFixture(tableCells.Concat(inventoryTableCells).ToArray());
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new TimeControlActionStep(
            "pause before concurrent native dining",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the shared dining-room cutlery stack",
            new[] { sharedDiningStack.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame starting diners and dining room",
            new[]
            {
                first.Diner.ThingID,
                second.Diner.ThingID,
                first.Meal.ThingID,
                second.Meal.ThingID,
                table.ThingID,
                sharedDiningStack.ThingID,
                startingAreaCutlery.ThingID
            },
            paddingPixels: 200);
        yield return new ScreenshotStep(
            "before concurrent dining with one two-unit dining-room stack",
            Array.Empty<string>(),
            paddingPixels: 0);

        foreach (var step in OrderNativeConsumption(context, first))
        {
            yield return step;
        }

        foreach (var step in OrderNativeConsumption(context, second))
        {
            yield return step;
        }

        yield return new TimeControlActionStep(
            "run both native ingest jobs until they share the stack reservation",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "both diners reserve one unit from the same dining-room stack",
            _ => ObserveSimultaneousReservations(),
            new EndToEndDeadline(1_800, 5_000, TimeSpan.FromSeconds(70)));
        yield return new TimeControlActionStep(
            "pause on the simultaneous shared-stack reservations",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "shared-stack reservations and dining destinations are exact",
            _ => AssertSimultaneousReservations());
        yield return new SelectionActionStep(
            "select the concurrently reserved cutlery stack",
            new[] { sharedDiningStack.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame both diners, the table, and the reserved stack",
            new[]
            {
                first.Diner.ThingID,
                second.Diner.ThingID,
                table.ThingID,
                sharedDiningStack.ThingID
            },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "action two diners concurrently reserve one unit each",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "continue concurrent dining until both exact settings are carried",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "both native ingest jobs acquire distinct units from the shared stack",
            _ => ObserveSharedStackPickup(),
            new EndToEndDeadline(1_800, 5_000, TimeSpan.FromSeconds(70)));
        yield return new TimeControlActionStep(
            "pause during concurrent native ingestion",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "shared source reservation is released after distinct pickup",
            _ => AssertSharedStackPickup());
        yield return new SelectionActionStep(
            "select the first concurrent diner during ingestion",
            new[] { first.Diner.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame both native ingest jobs at the table",
            new[] { first.Diner.ThingID, second.Diner.ThingID, table.ThingID },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "observe both diners eating at the resolved table",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "finish both shared-stack dining jobs",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "both native dining jobs return their exact cutlery dirty",
            _ => SharedStackDiningCompleted(),
            new EndToEndDeadline(3_600, 9_000, TimeSpan.FromSeconds(100)));
        yield return new TimeControlActionStep(
            "pause after shared-stack dining",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "shared-stack dining conserves two distinct dirty settings",
            _ => AssertSharedStackDiningCompleted());
        yield return new SelectionActionStep(
            "select the first returned shared-stack setting",
            new[] { firstAcquiredCutlery!.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the returned shared-stack settings",
            new[]
            {
                first.Diner.ThingID,
                second.Diner.ThingID,
                firstAcquiredCutlery!.ThingID,
                secondAcquiredCutlery!.ThingID,
                table.ThingID
            },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "after concurrent dining with both exact settings returned dirty",
            Array.Empty<string>(),
            paddingPixels: 0);

        foreach (var step in OrderNativeConsumption(context, inventory))
        {
            yield return step;
        }

        yield return new TimeControlActionStep(
            "run native ingestion for the carried-setting diner",
            paused: false,
            EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep(
            "the carried-setting diner uses inventory cutlery at the resolved table",
            _ => ObserveInventoryPickup(),
            new EndToEndDeadline(1_800, 5_000, TimeSpan.FromSeconds(70)));
        yield return new TimeControlActionStep(
            "pause during carried-setting ingestion",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "inventory cutlery wins without a map pickup detour",
            _ => AssertInventoryPickup());
        yield return new SelectionActionStep(
            "select the carried-setting diner during ingestion",
            new[] { inventory.Diner.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame carried-setting native ingestion",
            new[] { inventory.Diner.ThingID, inventoryTable.ThingID },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "observe carried cutlery used at the resolved dining place",
            Array.Empty<string>(),
            paddingPixels: 0);

        yield return new TimeControlActionStep(
            "finish carried-setting native dining",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "carried cutlery follows ordinary dirty table return",
            _ => InventoryDiningCompleted(),
            new EndToEndDeadline(3_600, 9_000, TimeSpan.FromSeconds(100)));
        yield return new TimeControlActionStep(
            "pause after carried-setting dining",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "all cutlery remains conserved and the starting-area option remains unused",
            _ => AssertFinalState());
        yield return new SelectionActionStep(
            "select the returned carried setting",
            new[] { carriedCutlery.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame final returned dining ware",
            new[]
            {
                inventory.Diner.ThingID,
                carriedCutlery.ThingID,
                firstAcquiredCutlery!.ThingID,
                secondAcquiredCutlery!.ThingID,
                table.ThingID,
                inventoryTable.ThingID
            },
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "after carried-cutlery dining with ordinary dirty return",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "dining-place cutlery acquisition result",
            _ => new Dictionary<string, string>
            {
                ["simultaneousReservationsObserved"] = simultaneousReservationsObserved.ToString(),
                ["sharedSourceThingId"] = sharedDiningStack.ThingID,
                ["firstAcquiredThingId"] = firstAcquiredCutlery?.ThingID ?? "missing",
                ["secondAcquiredThingId"] = secondAcquiredCutlery?.ThingID ?? "missing",
                ["carriedThingId"] = carriedCutlery.ThingID,
                ["unusedStartingAreaThingId"] = startingAreaCutlery.ThingID,
                ["firstEatSurface"] = firstEatSurface.ToString(),
                ["secondEatSurface"] = secondEatSurface.ToString(),
                ["inventoryEatSurface"] = inventoryEatSurface.ToString(),
                ["firstStart"] = first.StartPosition.ToString(),
                ["secondStart"] = second.StartPosition.ToString(),
                ["inventoryStart"] = inventory.StartPosition.ToString(),
                ["cutleryUnitsAfter"] = CountCutleryUnits().ToString(),
                ["unusedStartingAreaClean"] =
                    (startingAreaCutlery.GetComp<CompSanitation>()?.IsDirty == false).ToString()
            });
    }

    private DiningCase CreateDiningCase(string name, IntVec3 pawnCell, IntVec3 mealCell)
    {
        var diner = FoodSearchE2EFixture.CreateColonist(name);
        FoodSearchE2EFixture.SetHunger(diner, 0.20f);
        GenSpawn.Spawn(diner, pawnCell, map);
        diner.drafter.Drafted = true;

        var meal = FoodSearchE2EFixture.MakePlatedMeal(
            ThingDefOf.MealSimple,
            ThingDefOf.Steel,
            out var plate);
        GenSpawn.Spawn(meal, mealCell, map);
        return new DiningCase(diner, meal, plate, pawnCell);
    }

    private void SpawnWestChairs(IEnumerable<IntVec3> tableCells)
    {
        foreach (var tableCell in tableCells)
        {
            var chairCell = tableCell + IntVec3.West;
            var chair = ThingMaker.MakeThing(
                DefDatabase<ThingDef>.GetNamed("DiningChair"),
                ThingDefOf.WoodLog);
            chair.SetFactionDirect(Faction.OfPlayer);
            GenSpawn.Spawn(
                chair,
                chairCell,
                map,
                Rot4.FromIntVec3(tableCell - chairCell));
        }
    }

    private static ThingWithComps MakeCleanCutleryStack(int count)
    {
        var stack = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cutlery",
            ThingDefOf.Steel);
        for (var index = 1; index < count; index++)
        {
            var unit = FoodSearchE2EFixture.MakeCleanWare(
                "ImmersiveChefs_Cutlery",
                ThingDefOf.Steel);
            EndToEndAssert.True(
                stack.TryAbsorbStack(unit, respectStackLimit: true),
                "The fixture must build one honest homogeneous cutlery stack.");
        }

        EndToEndAssert.Equal(count, stack.stackCount,
            "The fixture must retain the requested physical cutlery count.");
        return stack;
    }

    private IEnumerable<EndToEndStep> OrderNativeConsumption(
        IEndToEndContext context,
        DiningCase diningCase)
    {
        yield return new SelectionActionStep(
            "select " + diningCase.Diner.LabelShortCap,
            new[] { diningCase.Diner.ThingID },
            additive: false);
        var draftToggle = RequiredDraftToggle(context, diningCase.Diner);
        yield return new GizmoActionStep(
            "undraft " + diningCase.Diner.LabelShortCap + " through the native colonist gizmo",
            new[] { diningCase.Diner.ThingID },
            draftToggle.RuntimeType,
            EndToEndGizmoInteraction.Toggle,
            stableGizmoId: draftToggle.StableId);

        var consume = RequiredConsumeOption(context, diningCase);
        yield return new FloatMenuActionStep(
            "order native consumption for " + diningCase.Diner.LabelShortCap,
            diningCase.Diner.ThingID,
            diningCase.Meal.ThingID,
            consume.StableId);
    }

    private static EndToEndGizmoOption RequiredDraftToggle(
        IEndToEndContext context,
        Pawn diner)
    {
        var candidates = context.GetRequiredService<IEndToEndGizmoCatalog>()
            .Query(new[] { diner.ThingID }, Array.Empty<string>())
            .Where(option =>
                !option.Disabled &&
                option.Interaction == EndToEndGizmoInteraction.Toggle &&
                option.ToggleState == true &&
                string.Equals(
                    option.HotKeyDefName,
                    "Command_ColonistDraft",
                    StringComparison.Ordinal))
            .ToArray();
        EndToEndAssert.Equal(1, candidates.Length,
            "The selected passive diner must expose one active native Draft toggle.");
        return candidates[0];
    }

    private static EndToEndFloatMenuOption RequiredConsumeOption(
        IEndToEndContext context,
        DiningCase diningCase)
    {
        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>()
            .Query(diningCase.Diner.ThingID, diningCase.Meal.ThingID);
        var consume = options.Where(option =>
                !option.Disabled &&
                option.Label.IndexOf("consume", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(1, consume.Length,
            "The plated meal must expose one enabled native Consume option; observed " +
            string.Join(", ", options.Select(option =>
                $"'{option.Label}' (disabled={option.Disabled})")));
        return consume[0];
    }

    private bool ObserveSimultaneousReservations()
    {
        var firstSession = DiningSessionRegistry.Current(first.Diner);
        var secondSession = DiningSessionRegistry.Current(second.Diner);
        if (first.Diner.CurJob is not { } firstJob ||
            second.Diner.CurJob is not { } secondJob ||
            !ReferenceEquals(firstSession?.Cutlery, sharedDiningStack) ||
            !ReferenceEquals(secondSession?.Cutlery, sharedDiningStack) ||
            !map.reservationManager.ReservedBy(sharedDiningStack, first.Diner, firstJob) ||
            !map.reservationManager.ReservedBy(sharedDiningStack, second.Diner, secondJob))
        {
            return false;
        }

        firstEatSurface = firstJob.GetTarget(TargetIndex.B).Cell;
        secondEatSurface = secondJob.GetTarget(TargetIndex.B).Cell;
        simultaneousReservationsObserved = true;
        Find.TickManager.Pause();
        return true;
    }

    private void AssertSimultaneousReservations()
    {
        EndToEndAssert.True(simultaneousReservationsObserved,
            "The real ReservationManager must expose simultaneous claims on the same stack.");
        var reservations = map.reservationManager.ReservationsReadOnly
            .Where(reservation => ReferenceEquals(reservation.Target.Thing, sharedDiningStack))
            .ToArray();
        EndToEndAssert.Equal(2, reservations.Length,
            "The two admitted ingest jobs must retain two simultaneous source-stack reservations.");
        EndToEndAssert.True(
            reservations.All(reservation =>
                reservation.StackCount == 1 &&
                reservation.MaxPawns == sharedDiningStack.def.stackLimit),
            "Each source-stack reservation must claim one unit with the stable stack-wide pawn limit.");
        EndToEndAssert.Equal(2, sharedDiningStack.stackCount,
            "Neither reservation may consume or claim away the unrequested physical unit.");
        AssertDiningOrigin(first, firstEatSurface);
        AssertDiningOrigin(second, secondEatSurface);
    }

    private void AssertDiningOrigin(DiningCase diningCase, IntVec3 eatSurface)
    {
        EndToEndAssert.True(
            eatSurface.IsValid && eatSurface.HasEatSurface(map),
            "Vanilla must resolve a real eat-surface cell before deferred cutlery selection.");
        EndToEndAssert.True(
            sharedDiningStack.Position.DistanceToSquared(eatSurface) <
            startingAreaCutlery.Position.DistanceToSquared(eatSurface),
            "The shared stack must be the setting closest to the resolved dining surface.");
        EndToEndAssert.True(
            startingAreaCutlery.Position.DistanceToSquared(diningCase.StartPosition) <
            sharedDiningStack.Position.DistanceToSquared(diningCase.StartPosition),
            "The unused competing setting must be closer to the diner's starting position.");
    }

    private bool ObserveSharedStackPickup()
    {
        var firstSession = DiningSessionRegistry.Current(first.Diner);
        var secondSession = DiningSessionRegistry.Current(second.Diner);
        var firstCandidate = firstSession?.CarriedCutlery;
        var secondCandidate = secondSession?.CarriedCutlery;
        if (first.Diner.CurJob is not { } firstJob ||
            second.Diner.CurJob is not { } secondJob ||
            firstJob.def != JobDefOf.Ingest ||
            secondJob.def != JobDefOf.Ingest ||
            !ReferenceEquals(firstJob.GetTarget(TargetIndex.A).Thing, first.Meal) ||
            !ReferenceEquals(secondJob.GetTarget(TargetIndex.A).Thing, second.Meal) ||
            firstCandidate is null ||
            secondCandidate is null ||
            ReferenceEquals(firstCandidate, secondCandidate) ||
            !ReferenceEquals(firstCandidate.holdingOwner, first.Diner.inventory?.innerContainer) ||
            !ReferenceEquals(secondCandidate.holdingOwner, second.Diner.inventory?.innerContainer) ||
            firstSession!.DiningPosition != first.Diner.Position ||
            secondSession!.DiningPosition != second.Diner.Position ||
            !first.Diner.Position.AdjacentTo8WayOrInside(firstEatSurface) ||
            !second.Diner.Position.AdjacentTo8WayOrInside(secondEatSurface))
        {
            return false;
        }

        firstAcquiredCutlery = firstCandidate;
        secondAcquiredCutlery = secondCandidate;
        Find.TickManager.Pause();
        return true;
    }

    private void AssertSharedStackPickup()
    {
        EndToEndAssert.True(
            firstAcquiredCutlery is not null &&
            secondAcquiredCutlery is not null &&
            !ReferenceEquals(firstAcquiredCutlery, secondAcquiredCutlery),
            "The two diners must carry two distinct physical cutlery Things.");
        EndToEndAssert.Equal(0, map.reservationManager.ReservationsReadOnly.Count(reservation =>
                ReferenceEquals(reservation.Target.Thing, sharedDiningStack)),
            "Pickup must release every source-stack reservation after the two units transfer.");
        EndToEndAssert.True(!sharedDiningStack.Spawned,
            "The two-unit source stack must be empty after two distinct pickups.");
        EndToEndAssert.True(
            startingAreaCutlery.Spawned &&
            startingAreaCutlery.GetComp<CompSanitation>()?.IsDirty == false,
            "The closer-to-start setting must remain clean and untouched.");
    }

    private bool SharedStackDiningCompleted() =>
        first.Meal.Destroyed &&
        second.Meal.Destroyed &&
        firstAcquiredCutlery is { Spawned: true } &&
        secondAcquiredCutlery is { Spawned: true } &&
        (firstAcquiredCutlery as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true &&
        (secondAcquiredCutlery as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true;

    private void AssertSharedStackDiningCompleted()
    {
        EndToEndAssert.True(SharedStackDiningCompleted(),
            "Both native ingestion jobs must consume their meals and return the exact settings dirty.");
        EndToEndAssert.True(
            first.Plate.Spawned && first.Plate.GetComp<CompSanitation>()?.IsDirty == true &&
            second.Plate.Spawned && second.Plate.GetComp<CompSanitation>()?.IsDirty == true,
            "Both exact embedded plates must follow the same visible dirty table return.");
        EndToEndAssert.True(
            IsAtSharedDiningTable(firstAcquiredCutlery!) &&
            IsAtSharedDiningTable(secondAcquiredCutlery!),
            "Both exact cutlery units must remain visibly at the physical dining table.");
        EndToEndAssert.Equal(4, CountCutleryUnits(),
            "Shared-stack dining must conserve the two used units, carried unit, and unused map unit.");
    }

    private bool ObserveInventoryPickup()
    {
        var session = DiningSessionRegistry.Current(inventory.Diner);
        if (inventory.Diner.CurJob is not { } job ||
            !ReferenceEquals(session?.Cutlery, carriedCutlery) ||
            !ReferenceEquals(session.CarriedCutlery, carriedCutlery))
        {
            return false;
        }

        inventoryEatSurface = job.GetTarget(TargetIndex.B).Cell;
        Find.TickManager.Pause();
        return true;
    }

    private void AssertInventoryPickup()
    {
        EndToEndAssert.True(
            inventoryEatSurface.IsValid && inventoryEatSurface.HasEatSurface(map),
            "The carried-setting diner must reach a vanilla-resolved eat surface.");
        EndToEndAssert.True(
            inventoryTable.OccupiedRect().Contains(inventoryEatSurface),
            "The carried-setting diner must use the dedicated open-capacity dining table.");
        EndToEndAssert.True(
            ReferenceEquals(
                carriedCutlery.holdingOwner,
                inventory.Diner.inventory?.innerContainer),
            "The already-carried clean setting must remain in the diner's inventory without a map detour.");
        EndToEndAssert.True(
            startingAreaCutlery.Spawned &&
            startingAreaCutlery.GetComp<CompSanitation>()?.IsDirty == false,
            "The clean map alternative must remain unused when clean inventory cutlery exists.");
    }

    private bool InventoryDiningCompleted() =>
        inventory.Meal.Destroyed &&
        carriedCutlery.Spawned &&
        carriedCutlery.GetComp<CompSanitation>()?.IsDirty == true;

    private void AssertFinalState()
    {
        EndToEndAssert.True(InventoryDiningCompleted(),
            "Native ingestion must return the colonist's carried setting through the colony dirty lifecycle.");
        EndToEndAssert.True(IsAtInventoryDiningTable(carriedCutlery),
            "The carried setting must be visibly returned at the resolved physical dining table.");
        EndToEndAssert.True(
            startingAreaCutlery.Spawned &&
            startingAreaCutlery.GetComp<CompSanitation>()?.IsDirty == false,
            "Dining-place ranking and inventory priority must leave the starting-area setting untouched.");
        EndToEndAssert.Equal(4, CountCutleryUnits(),
            "All four physical cutlery units must remain conserved after every native ingestion.");
    }

    private bool IsAtSharedDiningTable(Thing thing) =>
        thing.Spawned && table.OccupiedRect().Contains(thing.Position);

    private bool IsAtInventoryDiningTable(Thing thing) =>
        thing.Spawned && inventoryTable.OccupiedRect().Contains(thing.Position);

    private int CountCutleryUnits()
    {
        var def = sharedDiningStack.def;
        var spawned = map.listerThings.ThingsOfDef(def).Sum(thing => thing.stackCount);
        var held = new[] { first.Diner, second.Diner, inventory.Diner }.Sum(diner =>
            (diner.inventory?.innerContainer
                 .Where(thing => thing.def == def)
                 .Sum(thing => thing.stackCount) ?? 0) +
            (diner.carryTracker?.CarriedThing is { } carried && carried.def == def
                ? carried.stackCount
                : 0));
        return spawned + held;
    }

    private void AssertInitialFixture(IReadOnlyList<IntVec3> tableCells)
    {
        EndToEndAssert.Equal(2, sharedDiningStack.stackCount,
            "The dining room must begin with exactly two cutlery units in one stack.");
        EndToEndAssert.True(
            sharedDiningStack.GetComp<CompSanitation>()?.IsDirty == false &&
            startingAreaCutlery.GetComp<CompSanitation>()?.IsDirty == false &&
            carriedCutlery.GetComp<CompSanitation>()?.IsDirty == false,
            "Every candidate setting must begin clean.");
        EndToEndAssert.True(
            tableCells.All(cell => cell.HasEatSurface(map)),
            "The real Core table cells must expose finalized eat surfaces.");
        EndToEndAssert.Equal(4, CountCutleryUnits(),
            "The fixture must begin with two shared, one competing, and one carried cutlery unit.");
    }

    private sealed class DiningCase
    {
        internal DiningCase(
            Pawn diner,
            ThingWithComps meal,
            ThingWithComps plate,
            IntVec3 startPosition)
        {
            Diner = diner;
            Meal = meal;
            Plate = plate;
            StartPosition = startPosition;
        }

        internal Pawn Diner { get; }
        internal ThingWithComps Meal { get; }
        internal ThingWithComps Plate { get; }
        internal IntVec3 StartPosition { get; }
    }
}
