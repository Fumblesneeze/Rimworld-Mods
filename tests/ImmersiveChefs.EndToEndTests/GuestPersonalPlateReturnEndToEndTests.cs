using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.AI.Group;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.arrived-trader-personal-meal-ware-return",
    "fumblesneeze.immersivechefs",
    EndToEndTestContract.CorePackageId,
    "brrainz.harmony",
    "fumblesneeze.immersivechefs",
    MaxFrames = 9_600,
    MaxGameTicks = 40_000,
    MaxWallClockSeconds = 400)]
public sealed class ArrivedTraderPersonalMealWareReturnTest : IRimWorldEndToEndTest
{
    private const string SaveName = "ImmersiveChefs_ArrivedTraderPersonalWare";
    private const string InfoButtonObserverHarmonyId =
        "fumblesneeze.immersivechefs.e2e.arrived-trader-info-button-observer";
    private static readonly EndToEndScreenPoint GearTab = new(108, 686);
    private static readonly Dictionary<int, EndToEndScreenPoint> InfoButtonCenters = new();

    private Map map = null!;
    private Pawn observer = null!;
    private Faction traderFaction = null!;
    private Pawn? trader;
    private ThingWithComps? meal;
    private ThingWithComps? plate;
    private ThingWithComps? cutlery;
    private ThingWithComps? unrelatedPersonalPlate;
    private int traderFactionLoadId;
    private bool ingestJobObserved;
    private bool personalSettingObserved;
    private string traderId = string.Empty;
    private string mealId = string.Empty;
    private string plateId = string.Empty;
    private string cutleryId = string.Empty;
    private string unrelatedPersonalPlateId = string.Empty;
    private string activeJobLoadId = string.Empty;
    private EndToEndScreenPoint returnedCutleryInfo;
    private EndToEndScreenPoint returnedPlateInfo;

    public void Arrange(IEndToEndContext context)
    {
        var infoButton = AccessTools.Method(
            typeof(Widgets),
            nameof(Widgets.InfoCardButton),
            new[] { typeof(float), typeof(float), typeof(Thing) });
        EndToEndAssert.NotNull(
            infoButton,
            "The exact native Thing info-card button seam must remain available for player-input evidence.");
        var observerHarmony = new Harmony(InfoButtonObserverHarmonyId);
        observerHarmony.Patch(
            infoButton,
            prefix: new HarmonyMethod(
                typeof(ArrivedTraderPersonalMealWareReturnTest),
                nameof(ObserveInfoCardButton)));
        context.DeferCleanup(() =>
        {
            observerHarmony.Unpatch(infoButton, HarmonyPatchType.Prefix, InfoButtonObserverHarmonyId);
            InfoButtonCenters.Clear();
        });

        var savePath = GenFilePaths.FilePathForSavedGame(SaveName);
        context.DeferCleanup(() =>
        {
            if (File.Exists(savePath))
            {
                File.Delete(savePath);
            }
        });

        map = Current.Game.CurrentMap;
        var preIncidentPawnIds = map.mapPawns.AllPawns
            .Select(pawn => pawn.thingIDNumber)
            .ToHashSet();
        var preIncidentLordIds = map.lordManager.lords
            .Select(lord => lord.GetUniqueLoadID())
            .ToHashSet(StringComparer.Ordinal);
        context.DeferCleanup(() => CleanupIncidentFixtures(preIncidentPawnIds, preIncidentLordIds));
        FoodSearchE2EFixture.UseStrictNonEmergencyDining(context);
        observer = FoodSearchE2EFixture.CreateColonist("Trader dining observer");
        FoodSearchE2EFixture.SetHunger(observer, 1f);
        GenSpawn.Spawn(observer, FoodSearchE2EFixture.FindRoomCenter(map), map);
        observer.drafter.Drafted = true;

        var faction = Find.FactionManager.AllFactionsVisible
            .FirstOrDefault(candidate =>
                candidate != Faction.OfPlayer &&
                !candidate.HostileTo(Faction.OfPlayer) &&
                candidate.def.humanlikeFaction &&
                candidate.def.caravanTraderKinds is { Count: > 0 });
        EndToEndAssert.NotNull(
            faction,
            "The native personal-tableware scenario requires a neutral humanlike trader faction.");
        traderFaction = faction!;
        traderFactionLoadId = traderFaction.loadID;
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new IncidentActionStep(
            "execute RimWorld's native trader caravan arrival",
            "TraderCaravanArrival",
            traderFactionLoadId);
        yield return new TimeControlActionStep(
            "let the exact meal-carrying trader enter the map",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "a native trader arrives and receives the exact personal travel setting",
            _ => TrySeedAndObserveArrivedPersonalSetting(),
            new EndToEndDeadline(2_400, 10_000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep(
            "pause before personal trader dining",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep(
            "select the arrived trader carrying a plated personal meal",
            new[] { trader!.ThingID },
            additive: false);
        yield return new CameraActionStep(
            "frame the arrived trader before dining",
            new[] { trader.ThingID },
            paddingPixels: 260);
        yield return ProcessInputActionStep.Click(
            "open the arrived trader's native Gear tab before dining",
            GearTab,
            EndToEndMouseButton.Left);
        yield return new ScreenshotStep(
            "observe arrived trader's personal meal and cutlery before dining",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "activate ordinary trader hunger after arrival",
            _ => FoodSearchE2EFixture.SetHunger(trader, 0.10f));
        yield return new TimeControlActionStep(
            "allow ordinary trader food search and ingestion",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the trader starts native ingestion of the exact personal meal",
            _ => ObserveNativeIngestion(),
            new EndToEndDeadline(1_800, 6_000, TimeSpan.FromSeconds(65)));
        yield return new SelectionActionStep(
            "select the arrived trader during native ingestion",
            new[] { trader.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "observe the arrived trader eating its own plated meal",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new TimeControlActionStep(
            "pause the active personal-meal job before native persistence",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new SaveLoadActionStep(
            "save and load the arrived trader during its active personal-meal job",
            SaveName);
        yield return new WaitUntilStep(
            "loaded-game recovery interrupts the persisted dining job",
            _ => TryObserveLoadedRecovery(),
            new EndToEndDeadline(600, 2_000, TimeSpan.FromSeconds(30)));
        yield return new AssertionStep(
            "resolve the loaded trader and preserve personal ware provenance",
            _ =>
            {
                ResolveLoadedThings();
                AssertLoadedPersonalSetting();
            });
        yield return new SelectionActionStep(
            "select the same arrived trader after native loading",
            new[] { trader.ThingID },
            additive: false);
        yield return ProcessInputActionStep.Click(
            "open the loaded trader's native Gear tab",
            GearTab,
            EndToEndMouseButton.Left);
        yield return new ScreenshotStep(
            "observe personal cutlery and unrelated kitchenware retained after interrupted-job loading",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "remove the unrelated recovery sentinel after its persistence is proven",
            _ => RemoveUnrelatedRecoverySentinel());
        yield return new TimeControlActionStep(
            "finish the arrived trader's native ingestion",
            paused: false,
            EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the personal meal finishes and returns the exact dirty setting",
            _ => PersonalSettingReturned(),
            new EndToEndDeadline(1_800, 8_000, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep(
            "pause on the returned personal setting",
            paused: true,
            EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "assert exact personal-tableware identity and unit conservation",
            _ => AssertCompleted());
        yield return new SelectionActionStep(
            "select the trader owning the exact returned setting",
            new[] { trader.ThingID },
            additive: false);
        yield return new ScreenshotStep(
            "observe exact dirty plate and cutlery retained in trader inventory",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return new AssertionStep(
            "resolve the native info buttons for the exact returned personal setting",
            _ => ResolveReturnedInfoButtons());
        yield return ProcessInputActionStep.Click(
            "open the exact returned plate's native info card",
            returnedPlateInfo,
            EndToEndMouseButton.Left);
        yield return new AssertionStep(
            "bind the opened native info card to the exact returned plate",
            _ => AssertExactInfoCard(plate!));
        yield return new ScreenshotStep(
            "observe the returned personal plate's dirty sanitation state",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return ProcessInputActionStep.Key(
            "close the returned plate info card",
            "Escape");
        yield return ProcessInputActionStep.Click(
            "open the exact returned cutlery's native info card",
            returnedCutleryInfo,
            EndToEndMouseButton.Left);
        yield return new AssertionStep(
            "bind the opened native info card to the exact returned cutlery",
            _ => AssertExactInfoCard(cutlery!));
        yield return new ScreenshotStep(
            "observe the returned personal cutlery's dirty sanitation state",
            Array.Empty<string>(),
            paddingPixels: 0);
        yield return ProcessInputActionStep.Key(
            "close the returned cutlery info card",
            "Escape");
        yield return new CheckpointStep(
            "arrived trader personal tableware result",
            _ => new Dictionary<string, string>
            {
                ["trader"] = trader.ThingID,
                ["meal"] = meal!.ThingID,
                ["mealDestroyed"] = meal.Destroyed.ToString(),
                ["plate"] = plate!.ThingID,
                ["plateDirty"] = plate.GetComp<CompSanitation>()!.IsDirty.ToString(),
                ["plateInPersonalInventory"] =
                    trader.inventory!.innerContainer.Contains(plate).ToString(),
                ["cutlery"] = cutlery!.ThingID,
                ["cutleryDirty"] = cutlery.GetComp<CompSanitation>()!.IsDirty.ToString(),
                ["cutleryInPersonalInventory"] =
                    trader.inventory.innerContainer.Contains(cutlery).ToString()
            });
    }

    private bool TrySeedAndObserveArrivedPersonalSetting()
    {
        if (trader is not null)
        {
            return ObserveArrivedPersonalSetting();
        }

        trader = map.mapPawns.AllPawnsSpawned.FirstOrDefault(candidate =>
            candidate.Faction == traderFaction &&
            candidate.RaceProps.Humanlike &&
            candidate.inventory is not null &&
            candidate.Position.DistanceToSquared(observer.Position) <= 35 * 35);
        if (trader is null)
        {
            return false;
        }

        foreach (var carried in trader!.inventory!.innerContainer.ToArray())
        {
            trader.inventory.innerContainer.Remove(carried);
            carried.Destroy(DestroyMode.Vanish);
        }

        meal = FoodSearchE2EFixture.MakePlatedMeal(
            ThingDefOf.MealSimple,
            ThingDefOf.WoodLog,
            out var exactPlate);
        meal.stackCount = 1;
        plate = exactPlate;
        cutlery = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Cutlery",
            ThingDefOf.WoodLog);
        unrelatedPersonalPlate = FoodSearchE2EFixture.MakeCleanWare(
            "ImmersiveChefs_Plate",
            ThingDefOf.WoodLog);
        traderId = trader.ThingID;
        mealId = meal.ThingID;
        plateId = plate.ThingID;
        cutleryId = cutlery.ThingID;
        unrelatedPersonalPlateId = unrelatedPersonalPlate.ThingID;
        EndToEndAssert.True(
            trader.inventory.innerContainer.TryAdd(meal, canMergeWithExistingStacks: false),
            "The controlled trader must bring the exact plated meal in personal inventory.");
        EndToEndAssert.True(
            trader.inventory.innerContainer.TryAdd(cutlery, canMergeWithExistingStacks: false),
            "The controlled trader must bring the exact personal cutlery in inventory.");
        EndToEndAssert.True(
            trader.inventory.innerContainer.TryAdd(unrelatedPersonalPlate, canMergeWithExistingStacks: false),
            "The controlled trader must also carry unrelated personal kitchenware as a recovery sentinel.");
        FoodSearchE2EFixture.SetHunger(trader, 1f);
        EndToEndAssert.True(
            trader!.Spawned && trader.Map == map,
            "The controlled trader must remain an arrived map pawn after its ration fixture is replaced. " +
            $"Spawned={trader.Spawned}; Map={trader.Map?.uniqueID.ToString() ?? "none"}; " +
            $"ExpectedMap={map.uniqueID}; Dead={trader.Dead}; Destroyed={trader.Destroyed}; " +
            $"Faction={trader.Faction?.def.defName ?? "none"}.");
        return ObserveArrivedPersonalSetting();
    }

    private bool ObserveArrivedPersonalSetting()
    {
        if (trader is not { Spawned: true } || trader.Map != map ||
            meal is null || plate is null || cutlery is null || unrelatedPersonalPlate is null)
        {
            return false;
        }

        var inventory = trader.inventory?.innerContainer;
        if (inventory?.Contains(meal) != true ||
            inventory.Contains(cutlery) != true ||
            inventory.Contains(unrelatedPersonalPlate) != true)
        {
            throw new EndToEndAssertionException(
                "The arrived controlled trader lost its exact personal meal, cutlery, or unrelated kitchenware " +
                "before hunger was armed.");
        }

        EndToEndAssert.True(
            ReferenceEquals(meal.GetComp<CompEmbeddedWare>()?.PeekPlateThing(), plate),
            "The arrived trader's personal meal must still contain its exact clean plate.");
        EndToEndAssert.False(
            plate.GetComp<CompSanitation>()!.IsDirty || cutlery.GetComp<CompSanitation>()!.IsDirty,
            "The arrived trader's personal setting must begin clean.");
        personalSettingObserved = true;
        return true;
    }

    private bool ObserveNativeIngestion()
    {
        if (trader?.CurJobDef != JobDefOf.Ingest || meal is null || meal.Destroyed)
        {
            return false;
        }

        EndToEndAssert.True(
            ReferenceEquals(trader.CurJob?.GetTarget(TargetIndex.A).Thing, meal),
            "The native trader food job must target the exact personal plated meal.");
        EndToEndAssert.True(
            ReferenceEquals(cutlery!.holdingOwner, trader.inventory?.innerContainer),
            "The native dining session must retain the exact personal cutlery in trader inventory.");
        activeJobLoadId = trader.CurJob!.GetUniqueLoadID();
        ingestJobObserved = true;
        return true;
    }

    private bool PersonalSettingReturned()
    {
        if (meal?.Destroyed != true || plate is null || cutlery is null || trader?.inventory is null)
        {
            return false;
        }

        var inventory = trader.inventory.innerContainer;
        return inventory.Contains(plate) &&
               inventory.Contains(cutlery) &&
               plate.GetComp<CompSanitation>()?.IsDirty == true &&
               cutlery.GetComp<CompSanitation>()?.IsDirty == true;
    }

    private void AssertCompleted()
    {
        EndToEndAssert.True(personalSettingObserved && ingestJobObserved,
            "The workflow must observe the arrived personal setting and its exact native ingest job.");
        EndToEndAssert.True(PersonalSettingReturned(),
            "The arrived trader must retain its exact dirty plate and cutlery after eating its own meal.");
        EndToEndAssert.Equal(1, plate!.stackCount,
            "The returned exact personal plate must remain one physical unit.");
        EndToEndAssert.Equal(1, cutlery!.stackCount,
            "The returned exact personal cutlery must remain one physical unit.");
        EndToEndAssert.False(plate.Spawned || cutlery.Spawned,
            "Personal trader tableware must not be dropped into colony map ownership.");
        EndToEndAssert.Equal(
            1,
            FoodSearchE2EFixture.CountThingUnits(map, plate.def, meal!, trader!),
            "Arrived-trader dining must conserve the exact fixture plate across the map, trader, and original meal roots.");
        EndToEndAssert.Equal(
            1,
            FoodSearchE2EFixture.CountThingUnits(map, cutlery.def, meal!, trader!),
            "Arrived-trader dining must conserve the exact fixture cutlery across the map, trader, and original meal roots.");
    }

    private void ResolveLoadedThings()
    {
        map = Current.Game.CurrentMap;
        trader = map.mapPawns.AllPawnsSpawned.SingleOrDefault(pawn => pawn.ThingID == traderId) ??
                 throw new EndToEndAssertionException(
                     "Native save/load lost the exact arrived trader " + traderId + ".");
        meal = FindThing(mealId) as ThingWithComps ??
               throw new EndToEndAssertionException(
                   "Native save/load lost the exact personal meal " + mealId + ".");
        plate = meal.GetComp<CompEmbeddedWare>()?.PeekPlateThing() as ThingWithComps ??
                throw new EndToEndAssertionException(
                    "Native save/load lost the exact embedded personal plate " + plateId + ".");
        cutlery = trader.inventory?.innerContainer
                       .OfType<ThingWithComps>()
                       .SingleOrDefault(thing => thing.ThingID == cutleryId) ??
                   throw new EndToEndAssertionException(
                       "Native save/load removed the exact personal cutlery from trader inventory " +
                       cutleryId + ".");
        unrelatedPersonalPlate = trader.inventory?.innerContainer
                                     .OfType<ThingWithComps>()
                                     .SingleOrDefault(thing => thing.ThingID == unrelatedPersonalPlateId) ??
                                 throw new EndToEndAssertionException(
                                     "Native save/load confiscated the unrelated personal kitchenware " +
                                     unrelatedPersonalPlateId + ".");
    }

    private bool TryObserveLoadedRecovery()
    {
        map = Current.Game.CurrentMap;
        trader = map.mapPawns.AllPawnsSpawned.SingleOrDefault(pawn => pawn.ThingID == traderId);
        return trader is not null &&
               (trader.CurJob is null ||
                !StringComparer.Ordinal.Equals(trader.CurJob.GetUniqueLoadID(), activeJobLoadId));
    }

    private Thing? FindThing(string thingId)
    {
        var spawned = map.listerThings.AllThings.FirstOrDefault(thing => thing.ThingID == thingId);
        if (spawned is not null)
        {
            return spawned;
        }

        foreach (var pawn in map.mapPawns.AllPawns)
        {
            if (pawn.carryTracker?.CarriedThing?.ThingID == thingId)
            {
                return pawn.carryTracker.CarriedThing;
            }

            var inventoried = pawn.inventory?.innerContainer
                .FirstOrDefault(thing => thing.ThingID == thingId);
            if (inventoried is not null)
            {
                return inventoried;
            }
        }

        return null;
    }

    private void AssertLoadedPersonalSetting()
    {
        EndToEndAssert.Equal(mealId, meal!.ThingID,
            "Native save/load must preserve the exact personal meal identity.");
        EndToEndAssert.Equal(plateId, plate!.ThingID,
            "Native save/load must preserve the exact embedded plate identity.");
        EndToEndAssert.Equal(cutleryId, cutlery!.ThingID,
            "Native save/load must preserve the exact personal cutlery identity.");
        EndToEndAssert.True(
            trader!.inventory!.innerContainer.Contains(cutlery),
            "Loaded-job recovery must retain personal cutlery in the arrived trader's inventory.");
        EndToEndAssert.True(
            trader.inventory.innerContainer.Contains(unrelatedPersonalPlate!),
            "Loaded-job recovery must leave unrelated personal kitchenware in the arrived trader's inventory.");
        var cutlerySanitation = cutlery.GetComp<CompSanitation>()!;
        var replacementUsesExactMeal = trader.CurJobDef == JobDefOf.Ingest &&
                                       ReferenceEquals(
                                           trader.CurJob?.GetTarget(TargetIndex.A).Thing,
                                           meal);
        EndToEndAssert.True(
            !cutlerySanitation.IsPersonalDiningWareFor(trader) || replacementUsesExactMeal,
            "A personal-cutlery marker after recovery is valid only when the replacement native " +
            "ingest job has already repicked the exact personal meal.");
        EndToEndAssert.False(
            cutlerySanitation.ReturnToMapAfterInterruptedSession,
            "Loaded recovery must never reinterpret the visitor's personal cutlery as colony ware.");
        EndToEndAssert.True(
            meal.GetComp<CompEmbeddedWare>()!.IsPersonalPlateFor(trader),
            "Loaded-job recovery must retain the exact trader-owned plate provenance on the meal.");
        EndToEndAssert.False(
            plate.GetComp<CompSanitation>()!.IsDirty || cutlery.GetComp<CompSanitation>()!.IsDirty,
            "Interrupted native loading must not dirty the trader's unused personal setting.");
        var unrelatedSanitation = unrelatedPersonalPlate!.GetComp<CompSanitation>()!;
        EndToEndAssert.False(
            unrelatedSanitation.IsDirty ||
            unrelatedSanitation.IsPersonalDiningWareFor(trader) ||
            unrelatedSanitation.ReturnToMapAfterInterruptedSession,
            "Unrelated visitor kitchenware must remain clean and carry no active-session recovery marker.");
    }

    private void RemoveUnrelatedRecoverySentinel()
    {
        EndToEndAssert.NotNull(unrelatedPersonalPlate,
            "The unrelated visitor-kitchenware sentinel must still exist after loaded recovery.");
        EndToEndAssert.True(
            trader!.inventory!.innerContainer.Remove(unrelatedPersonalPlate!),
            "The proven unrelated visitor-kitchenware sentinel must remain removable from its exact inventory.");
        unrelatedPersonalPlate!.Destroy(DestroyMode.Vanish);
    }

    private static void AssertExactInfoCard(Thing expected)
    {
        var dialog = Find.WindowStack.Windows.OfType<Dialog_InfoCard>().SingleOrDefault();
        EndToEndAssert.NotNull(dialog,
            "The native info-card click must open exactly one ordinary Thing info dialog.");
        var openedThing = typeof(Dialog_InfoCard)
            .GetField("thing", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.GetValue(dialog) as Thing;
        EndToEndAssert.True(
            ReferenceEquals(openedThing, expected),
            $"The native info card must describe exact Thing {expected.ThingID}, not " +
            (openedThing?.ThingID ?? "no Thing"));
    }

    private void ResolveReturnedInfoButtons()
    {
        EndToEndAssert.True(
            InfoButtonCenters.TryGetValue(plate!.thingIDNumber, out returnedPlateInfo),
            "The native Gear tab must render an info-card button for the exact returned personal plate.");
        EndToEndAssert.True(
            InfoButtonCenters.TryGetValue(cutlery!.thingIDNumber, out returnedCutleryInfo),
            "The native Gear tab must render an info-card button for the exact returned personal cutlery.");
        EndToEndAssert.False(
            returnedPlateInfo.X == returnedCutleryInfo.X && returnedPlateInfo.Y == returnedCutleryInfo.Y,
            "The exact plate and cutlery must expose distinct native info-card click targets.");
    }

    private static void ObserveInfoCardButton(float x, float y, Thing thing)
    {
        if (thing is null)
        {
            return;
        }

        const int nativeInfoButtonSize = 24;
        var screenPoint = GUIUtility.GUIToScreenPoint(new Vector2(
            x + nativeInfoButtonSize / 2f,
            y + nativeInfoButtonSize / 2f));
        InfoButtonCenters[thing.thingIDNumber] = new EndToEndScreenPoint(
            Mathf.RoundToInt(screenPoint.x),
            Mathf.RoundToInt(screenPoint.y));
    }

    private void CleanupIncidentFixtures(
        HashSet<int> preIncidentPawnIds,
        HashSet<string> preIncidentLordIds)
    {
        foreach (var lord in map.lordManager.lords
                     .Where(candidate => !preIncidentLordIds.Contains(candidate.GetUniqueLoadID()))
                     .ToList())
        {
            map.lordManager.RemoveLord(lord);
        }

        foreach (var pawn in map.mapPawns.AllPawns
                     .Where(candidate => !preIncidentPawnIds.Contains(candidate.thingIDNumber))
                     .ToList())
        {
            pawn.ClearAllReservations(releaseDestinationsOnlyIfObsolete: false);
            pawn.jobs?.StopAll();
            pawn.inventory?.innerContainer.ClearAndDestroyContents();
            if (!pawn.Destroyed)
            {
                pawn.Destroy(DestroyMode.Vanish);
            }
        }
    }
}
