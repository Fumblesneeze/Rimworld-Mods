using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.meals-on-wheels-borrowed-meal",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "Argon.CheapMeals",
    "Memegoddess.MealsOnWheels",
    "seekiworksmod.no10",
    "fumblesneeze.immersivechefs",
    MaxFrames = 6_600,
    MaxGameTicks = 27_000,
    MaxWallClockSeconds = 235)]
public sealed class MealsOnWheelsBorrowedMealTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn eater = null!;
    private Pawn holder = null!;
    private ThingWithComps meal = null!;
    private ThingWithComps plate = null!;
    private ThingWithComps cutlery = null!;
    private bool borrowedJobObserved;
    private bool holderReleasedMeal;
    private bool cutleryAcquiredObserved;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);

        eater = FoodSearchE2EFixture.CreateColonist("Meals on Wheels diner");
        holder = FoodSearchE2EFixture.CreateColonist("Meals on Wheels holder");
        GenSpawn.Spawn(eater, center + (IntVec3.West * 2), map);
        GenSpawn.Spawn(holder, center + (IntVec3.East * 2), map);
        holder.drafter.Drafted = true;
        FoodSearchE2EFixture.SetHunger(eater, 0.10f);
        FoodSearchE2EFixture.SetHunger(holder, 1f);

        meal = FoodSearchE2EFixture.MakePlatedMeal(ThingDefOf.MealSimple, ThingDefOf.WoodLog, out plate);
        EndToEndAssert.True(
            holder.inventory!.innerContainer.TryAdd(meal, canMergeWithExistingStacks: false),
            "The source pawn must begin with the exact plated meal in its inventory.");
        cutlery = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Cutlery", ThingDefOf.WoodLog);
        GenSpawn.Spawn(cutlery, center, map);

        FoodSearchE2EFixture.UseStrictNonEmergencyDining(context);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var fixtureTargets = new[] { eater.ThingID, holder.ThingID, cutlery.ThingID };
        yield return new SelectionActionStep(
            "select the mobile meal fixture",
            fixtureTargets,
            additive: false);
        yield return new CameraActionStep(
            "frame the mobile meal fixture",
            fixtureTargets,
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "before ordinary mobile food search",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new TimeControlActionStep(
            "run ordinary hunger-driven food search",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "Meals on Wheels assigns the other pawn's exact meal",
            _ => ObserveBorrowedJob(),
            new EndToEndDeadline(1_200, 5_000, TimeSpan.FromSeconds(45)));
        yield return new WaitUntilStep(
            "the meal transfers while retaining its embedded plate",
            _ => ObserveMealTransfer(),
            new EndToEndDeadline(1_200, 5_000, TimeSpan.FromSeconds(45)));
        yield return new WaitUntilStep(
            "the ordinary dining path acquires the exact cutlery",
            _ => ObserveCutleryAcquisition(),
            new EndToEndDeadline(1_200, 5_000, TimeSpan.FromSeconds(45)));
        yield return new SelectionActionStep(
            "select the diner carrying the borrowed plated meal",
            new[] { eater.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "borrowed plated meal remains intact during native ingestion",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new WaitUntilStep(
            "borrowed meal completes and returns its exact service ware",
            _ => meal.Destroyed &&
                 plate.Spawned &&
                 plate.GetComp<CompSanitation>()?.IsDirty == true &&
                 cutlery.Spawned &&
                 cutlery.GetComp<CompSanitation>()?.IsDirty == true,
            new EndToEndDeadline(2_400, 10_000, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep(
            "pause after borrowed meal ingestion",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "mobile holder ownership and dining ware complete once",
            _ => AssertCompletedLifecycle());
        yield return new SelectionActionStep(
            "select the returned borrowed-meal plate",
            new[] { plate.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame returned borrowed-meal service ware",
            new[] { eater.ThingID, holder.ThingID, plate.ThingID, cutlery.ThingID },
            paddingPixels: 260);
        yield return new ScreenshotStep(
            "observe exact returned dirty borrowed-meal plate",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new SelectionActionStep(
            "select the returned borrowed-meal cutlery",
            new[] { cutlery.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "observe exact returned dirty borrowed-meal cutlery",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "Meals on Wheels dining result",
            _ => new Dictionary<string, string>
            {
                ["borrowedJobObserved"] = borrowedJobObserved.ToString(),
                ["holderReleasedMeal"] = holderReleasedMeal.ToString(),
                ["cutleryAcquiredObserved"] = cutleryAcquiredObserved.ToString(),
                ["mealThingId"] = meal.ThingID,
                ["mealDestroyed"] = meal.Destroyed.ToString(),
                ["plateThingId"] = plate.ThingID,
                ["plateDirty"] = (plate.GetComp<CompSanitation>()?.IsDirty == true).ToString(),
                ["cutleryThingId"] = cutlery.ThingID,
                ["cutleryDirty"] = (cutlery.GetComp<CompSanitation>()?.IsDirty == true).ToString()
            });
    }

    private bool ObserveBorrowedJob()
    {
        if (eater.CurJobDef != JobDefOf.Ingest)
        {
            return false;
        }

        var selected = eater.CurJob?.GetTarget(TargetIndex.A).Thing;
        if (!ReferenceEquals(selected, meal))
        {
            throw new EndToEndAssertionException(
                "The ordinary food job selected " +
                (selected?.def.defName ?? "nothing") +
                " instead of the exact meal held by the other pawn.");
        }

        borrowedJobObserved = true;
        return true;
    }

    private bool ObserveMealTransfer()
    {
        holderReleasedMeal |= !ReferenceEquals(meal.holdingOwner, holder.inventory?.innerContainer);
        if (!holderReleasedMeal || meal.Destroyed)
        {
            return false;
        }

        var embedded = meal.GetComp<CompEmbeddedWare>();
        EndToEndAssert.Equal(
            1,
            embedded?.EmbeddedPlateCount ?? -1,
            "The exact plate must remain embedded while the borrowed meal changes holder.");
        EndToEndAssert.True(
            ReferenceEquals(plate, embedded?.PeekPlateThing()),
            "Meals on Wheels must transfer the same physical embedded plate with the meal.");
        EndToEndAssert.False(
            plate.Spawned,
            "The embedded plate must not appear separately before ingestion completes.");
        return true;
    }

    private bool ObserveCutleryAcquisition()
    {
        if (meal.Destroyed || cutlery.Destroyed)
        {
            return false;
        }

        cutleryAcquiredObserved |=
            !cutlery.Spawned &&
            ReferenceEquals(cutlery.holdingOwner, eater.inventory?.innerContainer) &&
            cutlery.stackCount == 1;
        return cutleryAcquiredObserved;
    }

    private void AssertCompletedLifecycle()
    {
        EndToEndAssert.True(borrowedJobObserved,
            "The native hunger job must visibly select the other pawn's meal.");
        EndToEndAssert.True(holderReleasedMeal,
            "The source pawn must release the exact meal through the native ingest job.");
        EndToEndAssert.True(cutleryAcquiredObserved,
            "The ordinary ingest toils must acquire the exact cutlery before eating.");
        EndToEndAssert.True(meal.Destroyed,
            "The borrowed meal must disappear only after native ingestion completes.");
        EndToEndAssert.False(
            holder.inventory?.innerContainer.Contains(meal) == true,
            "The source pawn must no longer own the consumed meal.");
        EndToEndAssert.True(plate.Spawned && plate.GetComp<CompSanitation>()?.IsDirty == true,
            "The original embedded plate must return to the map dirty.");
        EndToEndAssert.True(cutlery.Spawned && cutlery.GetComp<CompSanitation>()?.IsDirty == true,
            "The exact acquired cutlery must return to the map dirty.");
        EndToEndAssert.Equal(1, plate.stackCount,
            "The exact returned plate must remain one physical unit.");
        EndToEndAssert.Equal(1, cutlery.stackCount,
            "The exact returned cutlery must remain one physical unit.");
        EndToEndAssert.Equal(1,
            FoodSearchE2EFixture.CountThingUnits(map, plate.def, meal, eater, holder),
            "Borrowed dining must conserve exactly one plate across all holders.");
        EndToEndAssert.Equal(1,
            FoodSearchE2EFixture.CountThingUnits(map, cutlery.def, meal, eater, holder),
            "Borrowed dining must conserve exactly one cutlery set across all holders.");
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.prioritize-perishable-plated-meal",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "Argon.CheapMeals",
    "Memegoddess.MealsOnWheels",
    "seekiworksmod.no10",
    "fumblesneeze.immersivechefs",
    MaxFrames = 6_600,
    MaxGameTicks = 27_000,
    MaxWallClockSeconds = 235)]
public sealed class PrioritizePerishablePlatedMealTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn eater = null!;
    private ThingWithComps perishableMeal = null!;
    private ThingWithComps plate = null!;
    private ThingWithComps cutlery = null!;
    private Thing survivalMeal = null!;
    private Thing pemmican = null!;
    private int survivalCount;
    private int pemmicanCount;
    private bool prioritizedJobObserved;
    private bool cutleryAcquiredObserved;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);

        eater = FoodSearchE2EFixture.CreateColonist("Perishable meal diner");
        GenSpawn.Spawn(eater, center + (IntVec3.West * 3), map);
        FoodSearchE2EFixture.SetHunger(eater, 0.10f);

        perishableMeal = FoodSearchE2EFixture.MakePlatedMeal(
            DefDatabase<ThingDef>.GetNamed("CM_SimpleFastMeal"),
            ThingDefOf.WoodLog,
            out plate);
        GenSpawn.Spawn(perishableMeal, center + (IntVec3.East * 3), map);
        cutlery = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Cutlery", ThingDefOf.WoodLog);
        GenSpawn.Spawn(cutlery, center + IntVec3.South, map);

        survivalMeal = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("MealSurvivalPack"));
        survivalMeal.stackCount = 2;
        GenSpawn.Spawn(survivalMeal, center + (IntVec3.West * 2), map);
        survivalCount = survivalMeal.stackCount;
        pemmican = ThingMaker.MakeThing(ThingDefOf.Pemmican);
        pemmican.stackCount = 20;
        GenSpawn.Spawn(pemmican, center + (IntVec3.West * 1), map);
        pemmicanCount = pemmican.stackCount;

        FoodSearchE2EFixture.UseStrictNonEmergencyDining(context);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var beforeTargets = new[]
        {
            eater.ThingID,
            perishableMeal.ThingID,
            survivalMeal.ThingID,
            pemmican.ThingID,
            cutlery.ThingID
        };
        yield return new SelectionActionStep(
            "select perishable and preserved food choices",
            beforeTargets,
            additive: false);
        yield return new CameraActionStep(
            "frame perishable and preserved food choices",
            beforeTargets,
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "before ordinary prioritized food search",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new TimeControlActionStep(
            "run ordinary prioritized food search",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "Prioritize Meals selects the farther perishable plated meal",
            _ => ObservePrioritizedJob(),
            new EndToEndDeadline(1_200, 5_000, TimeSpan.FromSeconds(45)));
        yield return new WaitUntilStep(
            "the ordinary dining path acquires prioritized-meal cutlery",
            _ => ObserveCutleryAcquisition(),
            new EndToEndDeadline(1_200, 5_000, TimeSpan.FromSeconds(45)));
        yield return new WaitUntilStep(
            "the diner reaches the selected perishable plated meal",
            _ => ReachedSelectedPerishableMeal(),
            new EndToEndDeadline(1_200, 5_000, TimeSpan.FromSeconds(45)));
        yield return new SelectionActionStep(
            "select the diner consuming the perishable plated meal",
            new[] { eater.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "ordinary job targets the perishable plated meal",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new WaitUntilStep(
            "the perishable meal completes its ordinary dining lifecycle",
            _ => perishableMeal.Destroyed &&
                 plate.Spawned &&
                 plate.GetComp<CompSanitation>()?.IsDirty == true &&
                 cutlery.Spawned &&
                 cutlery.GetComp<CompSanitation>()?.IsDirty == true,
            new EndToEndDeadline(2_400, 10_000, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep(
            "pause after prioritized meal ingestion",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "preserved controls remain untouched while dining ware returns",
            _ => AssertCompletedLifecycle());
        yield return new SelectionActionStep(
            "select returned prioritized-meal plate",
            new[] { plate.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame returned ware and untouched preserved foods",
            new[] { eater.ThingID, plate.ThingID, cutlery.ThingID, survivalMeal.ThingID, pemmican.ThingID },
            paddingPixels: 260);
        yield return new ScreenshotStep(
            "observe dirty plate beside untouched preserved foods",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new SelectionActionStep(
            "select returned prioritized-meal cutlery",
            new[] { cutlery.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "observe dirty cutlery after prioritized dining",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "Prioritize Meals dining result",
            _ => new Dictionary<string, string>
            {
                ["prioritizedJobObserved"] = prioritizedJobObserved.ToString(),
                ["cutleryAcquiredObserved"] = cutleryAcquiredObserved.ToString(),
                ["selectedMeal"] = perishableMeal.def.defName,
                ["selectedMealThingId"] = perishableMeal.ThingID,
                ["mealDestroyed"] = perishableMeal.Destroyed.ToString(),
                ["plateThingId"] = plate.ThingID,
                ["plateDirty"] = (plate.GetComp<CompSanitation>()?.IsDirty == true).ToString(),
                ["cutleryThingId"] = cutlery.ThingID,
                ["cutleryDirty"] = (cutlery.GetComp<CompSanitation>()?.IsDirty == true).ToString(),
                ["survivalMeal"] = survivalMeal.Spawned + ":" + survivalMeal.stackCount,
                ["pemmican"] = pemmican.Spawned + ":" + pemmican.stackCount
            });
    }

    private bool ObservePrioritizedJob()
    {
        if (eater.CurJobDef != JobDefOf.Ingest)
        {
            return false;
        }

        var selected = eater.CurJob?.GetTarget(TargetIndex.A).Thing;
        if (!ReferenceEquals(selected, perishableMeal))
        {
            throw new EndToEndAssertionException(
                "The ordinary food job selected " +
                (selected?.def.defName ?? "nothing") +
                " instead of the farther perishable plated meal.");
        }

        prioritizedJobObserved = true;
        return true;
    }

    private bool ObserveCutleryAcquisition()
    {
        if (perishableMeal.Destroyed || cutlery.Destroyed)
        {
            return false;
        }

        cutleryAcquiredObserved |=
            !cutlery.Spawned &&
            ReferenceEquals(cutlery.holdingOwner, eater.inventory?.innerContainer) &&
            cutlery.stackCount == 1;
        return cutleryAcquiredObserved;
    }

    private bool ReachedSelectedPerishableMeal()
    {
        if (perishableMeal.Destroyed ||
            eater.CurJobDef != JobDefOf.Ingest ||
            !ReferenceEquals(eater.CurJob?.GetTarget(TargetIndex.A).Thing, perishableMeal))
        {
            return false;
        }

        return ReferenceEquals(eater.carryTracker.CarriedThing, perishableMeal) ||
               eater.Position.DistanceToSquared(perishableMeal.PositionHeld) <= 2;
    }

    private void AssertCompletedLifecycle()
    {
        EndToEndAssert.True(prioritizedJobObserved,
            "The ordinary hunger job must visibly choose the perishable meal.");
        EndToEndAssert.True(cutleryAcquiredObserved,
            "The ordinary ingest toils must acquire the exact prioritized-meal cutlery before eating.");
        EndToEndAssert.True(perishableMeal.Destroyed,
            "The selected perishable meal must complete native ingestion.");
        EndToEndAssert.True(plate.Spawned && plate.GetComp<CompSanitation>()?.IsDirty == true,
            "The selected meal's exact plate must return dirty.");
        EndToEndAssert.True(cutlery.Spawned && cutlery.GetComp<CompSanitation>()?.IsDirty == true,
            "The exact acquired cutlery must return dirty.");
        EndToEndAssert.Equal(1, plate.stackCount,
            "The exact returned prioritized-meal plate must remain one physical unit.");
        EndToEndAssert.Equal(1, cutlery.stackCount,
            "The exact returned prioritized-meal cutlery must remain one physical unit.");
        EndToEndAssert.Equal(1,
            FoodSearchE2EFixture.CountThingUnits(map, plate.def, perishableMeal, eater),
            "Prioritized dining must conserve exactly one plate across all holders.");
        EndToEndAssert.Equal(1,
            FoodSearchE2EFixture.CountThingUnits(map, cutlery.def, perishableMeal, eater),
            "Prioritized dining must conserve exactly one cutlery set across all holders.");
        EndToEndAssert.True(survivalMeal.Spawned && survivalMeal.stackCount == survivalCount,
            "The closer packaged survival meal must remain untouched.");
        EndToEndAssert.True(pemmican.Spawned && pemmican.stackCount == pemmicanCount,
            "The closer pemmican stack must remain untouched.");
        EndToEndAssert.True((survivalMeal as ThingWithComps)?.GetComp<CompEmbeddedWare>() is null,
            "The excluded survival meal must not acquire embedded ware.");
        EndToEndAssert.True((pemmican as ThingWithComps)?.GetComp<CompEmbeddedWare>() is null,
            "The excluded pemmican must not acquire embedded ware.");
    }
}

[RimWorldEndToEndTest(
    "immersive-chefs.prioritize-trader-caravan-compensation",
    "fumblesneeze.immersivechefs",
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "Argon.CheapMeals",
    "Memegoddess.MealsOnWheels",
    "seekiworksmod.no10",
    "fumblesneeze.immersivechefs",
    MaxFrames = 4_800,
    MaxGameTicks = 16_000,
    MaxWallClockSeconds = 150)]
public sealed class PrioritizeTraderCaravanCompensationTest : IRimWorldEndToEndTest
{
    private const string ObserverHarmonyId =
        "fumblesneeze.immersivechefs.e2e.prioritize-trader-caravan-compensation";
    private const string UpstreamHarmonyId =
        "Prioritize_Meals_over_Preserved_Foods.HarmonyPatch";

    private static PrioritizeTraderCaravanCompensationTest? active;

    private Map map = null!;
    private Pawn observer = null!;
    private Pawn? controlledTrader;
    private MethodInfo sendLetter = null!;
    private Harmony harmony = null!;
    private string controlledTraderId = string.Empty;
    private bool foodlessBeforeUpstream;
    private bool upstreamPostfixCompleted;
    private int traderFactionLoadId;
    private readonly List<Thing> compensatedFoods = new();

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        observer = FoodSearchE2EFixture.CreateColonist("Trader caravan observer");
        GenSpawn.Spawn(observer, map.Center, map);

        sendLetter = ResolveUpstreamSendLetter();
        harmony = new Harmony(ObserverHarmonyId);
        active = this;
        harmony.Patch(
            sendLetter,
            prefix: new HarmonyMethod(
                typeof(PrioritizeTraderCaravanCompensationTest),
                nameof(BeforeTraderLetter)) { priority = Priority.First },
            postfix: new HarmonyMethod(
                typeof(PrioritizeTraderCaravanCompensationTest),
                nameof(AfterTraderLetter)) { after = new[] { UpstreamHarmonyId } });
        context.DeferCleanup(() =>
        {
            harmony.Unpatch(sendLetter, HarmonyPatchType.All, ObserverHarmonyId);
            if (ReferenceEquals(active, this))
            {
                active = null;
            }
        });

        var faction = Find.FactionManager.AllFactionsVisible
            .FirstOrDefault(candidate =>
                candidate != Faction.OfPlayer &&
                !candidate.HostileTo(Faction.OfPlayer) &&
                candidate.def.humanlikeFaction &&
                candidate.def.caravanTraderKinds is { Count: > 0 });
        EndToEndAssert.NotNull(
            faction,
            "The native trader-arrival scenario requires an eligible neutral humanlike faction.");
        traderFactionLoadId = faction!.loadID;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new SelectionActionStep(
            "select the colony before the queued trader incident",
            new[] { observer.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the colony before the queued trader incident",
            new[] { observer.ThingID },
            paddingPixels: 260);
        yield return new ScreenshotStep(
            "before native trader caravan arrival",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new IncidentActionStep(
            "execute RimWorld's native trader caravan incident",
            "TraderCaravanArrival",
            traderFactionLoadId);
        yield return new AssertionStep(
            "the native incident completed the exact upstream compensation postfix",
            _ => AssertUpstreamBoundary());
        yield return new TimeControlActionStep(
            "let the compensated native caravan enter the map",
            paused: false,
            EndToEndGameSpeed.Fast);
        yield return new WaitUntilStep(
            "the compensated native trader becomes visible on the map",
            _ => controlledTrader is { Spawned: true },
            new EndToEndDeadline(2_400, 10_000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep(
            "pause after native trader compensation",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the foodless trader received preserved food from the upstream postfix",
            _ => AssertCompensation());
        yield return new SelectionActionStep(
            "select the compensated native trader",
            new[] { controlledTrader!.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the arrived native trader caravan",
            map.mapPawns.AllPawnsSpawned
                .Where(pawn => pawn.Faction == controlledTrader!.Faction)
                .Select(pawn => pawn.ThingID),
            paddingPixels: 220);
        yield return new ScreenshotStep(
            "observe the arrived caravan and selected compensated trader",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new CheckpointStep(
            "Prioritize Meals trader compensation result",
            _ => new Dictionary<string, string>
            {
                ["controlledTraderId"] = controlledTraderId,
                ["foodlessBeforeUpstream"] = foodlessBeforeUpstream.ToString(),
                ["upstreamPostfixCompleted"] = upstreamPostfixCompleted.ToString(),
                ["compensatedFoods"] = string.Join(
                    "|",
                    compensatedFoods.Select(food => food.def.defName + ":" + food.stackCount))
            });
    }

    public static void BeforeTraderLetter(ref List<Pawn> pawns)
    {
        active?.MakeOneTraderFoodless(pawns);
    }

    public static void AfterTraderLetter(ref List<Pawn> pawns)
    {
        active?.CaptureUpstreamCompensation(pawns);
    }

    private static MethodInfo ResolveUpstreamSendLetter()
    {
        var upstreamPostfix = AccessTools.TypeByName(
                "seekiworks_Prioritize_Meals_over_Preserved_Foods." +
                "Patch_IncidentWorker_TraderCaravanArrival")
            ?.GetMethod("SendLetter_Postfix", BindingFlags.NonPublic | BindingFlags.Static);
        EndToEndAssert.NotNull(
            upstreamPostfix,
            "The exact Prioritize Meals caravan postfix must be loaded for this E2E scenario.");

        var targets = Harmony.GetAllPatchedMethods()
            .Where(method =>
                method.DeclaringType == typeof(IncidentWorker_TraderCaravanArrival) &&
                method.Name == "SendLetter")
            .Where(method =>
                (Harmony.GetPatchInfo(method)?.Postfixes ?? Enumerable.Empty<Patch>())
                .Any(patch =>
                    patch.owner == UpstreamHarmonyId &&
                    patch.PatchMethod == upstreamPostfix))
            .Cast<MethodInfo>()
            .ToArray();
        EndToEndAssert.Equal(
            1,
            targets.Length,
            "The upstream trader compensation must patch exactly one SendLetter target.");
        return targets[0];
    }

    private void MakeOneTraderFoodless(IReadOnlyList<Pawn> pawns)
    {
        controlledTrader = pawns.FirstOrDefault(pawn =>
            pawn.RaceProps.Humanlike && pawn.inventory is not null);
        EndToEndAssert.NotNull(
            controlledTrader,
            "The native trader caravan must contain a humanlike pawn with an inventory.");
        controlledTraderId = controlledTrader!.ThingID;

        var qualifying = controlledTrader.inventory!.innerContainer
            .Where(IsUpstreamQualifyingFood)
            .ToArray();
        foreach (var food in qualifying)
        {
            controlledTrader.inventory.innerContainer.Remove(food);
            food.Destroy(DestroyMode.Vanish);
        }

        foodlessBeforeUpstream = !controlledTrader.inventory.innerContainer.Any(IsUpstreamQualifyingFood);
    }

    private void CaptureUpstreamCompensation(IReadOnlyList<Pawn> pawns)
    {
        EndToEndAssert.True(
            controlledTrader is not null && pawns.Contains(controlledTrader),
            "The controlled trader must remain in the native SendLetter pawn list.");
        var preservedFoods = AccessTools.TypeByName(
                "seekiworks_Prioritize_Meals_over_Preserved_Foods.Foods")
            ?.GetField("preservedFoods", BindingFlags.NonPublic | BindingFlags.Static)
            ?.GetValue(null) as HashSet<ThingDef>;
        EndToEndAssert.NotNull(
            preservedFoods,
            "The upstream preserved-food ledger must exist during caravan compensation.");

        compensatedFoods.Clear();
        compensatedFoods.AddRange(
            controlledTrader!.inventory!.innerContainer
                .Where(food => preservedFoods!.Contains(food.def)));
        upstreamPostfixCompleted = true;
    }

    private void AssertCompensation()
    {
        EndToEndAssert.True(
            foodlessBeforeUpstream,
            "The controlled trader must have no qualifying food before the upstream postfix runs.");
        EndToEndAssert.True(
            upstreamPostfixCompleted,
            "The exact native SendLetter path must complete the upstream postfix.");
        EndToEndAssert.True(
            controlledTrader is { Spawned: true } && controlledTrader.Map == map,
            "The compensated trader must visibly arrive on the current map.");
        EndToEndAssert.True(
            compensatedFoods.Count > 0 && compensatedFoods.All(food =>
                food.holdingOwner == controlledTrader!.inventory!.innerContainer),
            "The foodless native trader must receive upstream-classified preserved food in inventory.");
    }

    private void AssertUpstreamBoundary()
    {
        EndToEndAssert.True(
            controlledTrader is not null,
            "The native incident must pass a humanlike trader through SendLetter.");
        EndToEndAssert.True(
            foodlessBeforeUpstream,
            "The controlled trader must be foodless immediately before the upstream postfix.");
        EndToEndAssert.True(
            upstreamPostfixCompleted,
            "The exact Prioritize Meals SendLetter postfix boundary must complete.");
        EndToEndAssert.True(
            compensatedFoods.Count > 0,
            "The upstream postfix must add at least one preserved food to the controlled trader.");
    }

    private static bool IsUpstreamQualifyingFood(Thing thing) =>
        thing.def.ingestible is { } ingestible &&
        ingestible.preferability >= FoodPreferability.RawTasty;
}

internal static class FoodSearchE2EFixture
{
    internal static int CountThingUnits(
        Map map,
        ThingDef def,
        ThingWithComps meal,
        params Pawn[] pawns)
    {
        var spawned = map.listerThings.ThingsOfDef(def).Sum(thing => thing.stackCount);
        var heldByPawns = pawns.Sum(pawn =>
            (pawn.inventory?.innerContainer
                 .Where(thing => thing.def == def)
                 .Sum(thing => thing.stackCount) ?? 0) +
            (pawn.carryTracker?.CarriedThing is { } carried && carried.def == def
                ? carried.stackCount
                : 0));
        var embedded = !meal.Destroyed &&
                       string.Equals(def.defName, "ImmersiveChefs_Plate", StringComparison.Ordinal)
            ? meal.GetComp<CompEmbeddedWare>()?.EmbeddedPlateCount ?? 0
            : 0;
        return spawned + heldByPawns + embedded;
    }

    internal static void UseStrictNonEmergencyDining(IEndToEndContext context)
    {
        var priorMode = ImmersiveChefsMod.Settings.WareRequirementMode;
        var priorThreshold = ImmersiveChefsMod.Settings.EmergencyHungerThreshold;
        context.DeferCleanup(() =>
        {
            ImmersiveChefsMod.Settings.WareRequirementMode = priorMode;
            ImmersiveChefsMod.Settings.EmergencyHungerThreshold = priorThreshold;
        });
        ImmersiveChefsMod.Settings.WareRequirementMode = WareRequirementMode.Strict;
        ImmersiveChefsMod.Settings.EmergencyHungerThreshold = 0.05f;
    }

    internal static Pawn CreateColonist(string name)
    {
        var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
        pawn.Name = new NameSingle(name);
        pawn.inventory?.innerContainer.ClearAndDestroyContents();
        pawn.workSettings.EnableAndInitialize();
        foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
        {
            if (!pawn.WorkTypeIsDisabled(workType))
            {
                pawn.workSettings.SetPriority(workType, 0);
            }
        }

        pawn.jobs.StopAll();
        return pawn;
    }

    internal static void SetHunger(Pawn pawn, float percentage)
    {
        if (pawn.needs?.food is { } food)
        {
            food.CurLevelPercentage = percentage;
        }
    }

    internal static ThingWithComps MakePlatedMeal(
        ThingDef mealDef,
        ThingDef plateStuff,
        out ThingWithComps plate)
    {
        var meal = (ThingWithComps)ThingMaker.MakeThing(mealDef);
        plate = MakeCleanWare("ImmersiveChefs_Plate", plateStuff);
        var embedded = meal.GetComp<CompEmbeddedWare>();
        EndToEndAssert.NotNull(
            embedded,
            mealDef.defName + " must expose finalized embedded-ware state.");
        EndToEndAssert.True(
            embedded!.TryEmbedPlate(plate),
            "The fixture must embed its exact clean plate once.");
        EndToEndAssert.True(
            ReferenceEquals(plate, embedded.PeekPlateThing()),
            "The fixture must retain the same physical plate Thing.");
        return meal;
    }

    internal static ThingWithComps MakeCleanWare(string defName, ThingDef stuff)
    {
        var ware = (ThingWithComps)ThingMaker.MakeThing(
            DefDatabase<ThingDef>.GetNamed(defName),
            stuff);
        ware.GetComp<CompQuality>()?.SetQuality(
            QualityCategory.Normal,
            ArtGenerationContext.Colony);
        ware.GetComp<CompSanitation>()?.MarkClean(WashProvenance.Safe);
        return ware;
    }

    internal static void BuildSealedRoom(Map map, IntVec3 center)
    {
        var granite = DefDatabase<ThingDef>.GetNamed("BlocksGranite");
        for (var offset = -5; offset <= 5; offset++)
        {
            SpawnWall(map, center + new IntVec3(offset, 0, -5), granite);
            SpawnWall(map, center + new IntVec3(offset, 0, 5), granite);
            if (offset is -5 or 5)
            {
                continue;
            }

            SpawnWall(map, center + new IntVec3(-5, 0, offset), granite);
            SpawnWall(map, center + new IntVec3(5, 0, offset), granite);
        }
    }

    internal static IntVec3 FindRoomCenter(Map map)
    {
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 55f, useCenter: true))
        {
            if (SquareIsUsable(map, candidate, 6))
            {
                return candidate;
            }
        }

        throw new EndToEndAssertionException("Could not find a clear food-search fixture area.");
    }

    private static bool SquareIsUsable(Map map, IntVec3 center, int radius)
    {
        for (var x = -radius; x <= radius; x++)
        {
            for (var z = -radius; z <= radius; z++)
            {
                var cell = center + new IntVec3(x, 0, z);
                if (!cell.InBounds(map) ||
                    !cell.Walkable(map) ||
                    cell.GetEdifice(map) is not null)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private static void SpawnWall(Map map, IntVec3 cell, ThingDef stuff)
    {
        var wall = ThingMaker.MakeThing(ThingDefOf.Wall, stuff);
        wall.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(wall, cell, map);
    }
}
