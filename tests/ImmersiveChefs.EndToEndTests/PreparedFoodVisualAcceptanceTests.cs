using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest(
    "immersive-chefs.prepared-food-visual-acceptance",
    "fumblesneeze.immersivechefs", "brrainz.harmony", EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions", "fumblesneeze.immersivechefs",
    MaxFrames = 7200, MaxGameTicks = 12000, MaxWallClockSeconds = 240)]
public sealed class PreparedFoodVisualAcceptanceTest : IRimWorldEndToEndTest
{
    private readonly List<Thing> owned = new();
    private Map map = null!;
    private Pawn cook = null!;
    private ThingWithComps station = null!;
    private Thing raw = null!;
    private Thing prepared = null!;
    private Thing destinationMarker = null!;
    private ThingDef preparedDef = null!;
    private IntVec3 center;
    private IntVec3 storageCell;
    private Zone_Stockpile storage = null!;
    private readonly List<Thing> comparators = new();

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        center = FoodSearchE2EFixture.FindRoomCenter(map);
        preparedDef = DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_PreparedFood");
        var screenshotMode = Find.ScreenshotModeHandler.Active;
        context.DeferCleanup(() => Find.ScreenshotModeHandler.Active = screenshotMode);
        var previousAssistants = ImmersiveChefsMod.Settings.AutoCallAssistants;
        context.DeferCleanup(() => ImmersiveChefsMod.Settings.AutoCallAssistants = previousAssistants);
        var previousTerrain = CellRect.CenteredOn(center, 6).ToDictionary(cell => cell, cell => cell.GetTerrain(map));
        context.DeferCleanup(() =>
        {
            foreach (var entry in previousTerrain) map.terrainGrid.SetTerrain(entry.Key, entry.Value);
        });
        context.DeferCleanup(() =>
        {
            foreach (var thing in owned.Where(thing => !thing.Destroyed).ToArray())
                thing.Destroy(DestroyMode.Vanish);
            storage?.Delete();
        });
        HandwashingE2EFixture.PreserveSettings(context);
        ImmersiveChefsMod.Settings.AutoCallAssistants = false;
        foreach (var cell in CellRect.CenteredOn(center, 6))
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
        station = DispenserE2EFixture.SpawnBuilding(map, "ImmersiveChefs_PrepStation", center + new IntVec3(0, 0, 2));
        owned.Add(station);
        foreach (var entry in new[] { ("FueledStove", -4), ("TableButcher", 4) })
        {
            var thing = DispenserE2EFixture.SpawnBuilding(map, entry.Item1, center + new IntVec3(entry.Item2, 0, 2));
            owned.Add(thing);
            comparators.Add(thing);
        }
        cook = HandwashingE2EFixture.CreateInactiveKitchenWorker("Preparation cook");
        cook.workSettings.SetPriority(DefDatabase<WorkTypeDef>.GetNamed("Cooking"), 1);
        GenSpawn.Spawn(cook, center + new IntVec3(-1, 0, -1), map);
        owned.Add(cook);
        EndToEndAssert.NotNull(cook.drafter, "SpawnSetup must initialize the colonist's native draft controller.");
        cook.drafter.Drafted = true;
        raw = SpawnItem("RawRice", center + new IntVec3(1, 0, 0), 10);
        storageCell = center + new IntVec3(0, 0, -2);
        storage = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
        storage.settings.filter.SetDisallowAll();
        storage.settings.filter.SetAllow(preparedDef, true);
        map.zoneManager.RegisterZone(storage);
        storage.AddCell(storageCell);
        var bill = new Bill_Production(DefDatabase<RecipeDef>.GetNamed("ImmersiveChefs_PrepareIngredients"))
        {
            repeatMode = BillRepeatModeDefOf.RepeatCount,
            repeatCount = 1,
            ingredientSearchRadius = 10f
        };
        bill.ingredientFilter.SetDisallowAll();
        bill.ingredientFilter.SetAllow(raw.def, true);
        bill.SetPawnRestriction(cook);
        bill.SetStoreMode(BillStoreModeDefOf.BestStockpile);
        ((IBillGiver)station).BillStack.AddBill(bill);
        comparators.Add(SpawnItem("MealSimple", center + new IntVec3(-2, 0, -2), 1));
        comparators.Add(SpawnItem("Pemmican", center + new IntVec3(2, 0, -2), 10));
        comparators.Add(SpawnItem("MedicineIndustrial", center + new IntVec3(4, 0, -2), 1));
        destinationMarker = SpawnItem("Steel", center + new IntVec3(-5, 0, -5), 1);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return Pause("pause before native preparation");
        yield return new SupportingSceneTimeActionStep("set local noon", "map-" + map.uniqueID, 720);
        yield return new CameraActionStep("frame ingredients and preparation bench", new[] { station.ThingID, raw.ThingID, cook.ThingID }, 150);
        yield return new SelectionActionStep("inspect the preparation bill giver", new[] { station.ThingID }, false);
        yield return new AssertionStep("no prepared output exists before the order", _ =>
            EndToEndAssert.Equal(0, map.listerThings.ThingsOfDef(preparedDef).Count, "Output must be created by the real bill."));
        yield return Shot("before preparation order");
        yield return ToggleDraft(context, expected: true);
        yield return Choose(context, station, "prioritize", "prioritize the ordinary preparation bill");
        yield return Run("run the colonist's preparation job", EndToEndGameSpeed.Normal);
        yield return new WaitUntilStep("colonist reaches the preparation bench", _ =>
            cook.CurJobDef == JobDefOf.DoBill && cook.Position == ((Building)station).InteractionCell &&
            ReferenceEquals(cook.CurJob?.GetTarget(TargetIndex.A).Thing, station),
            new EndToEndDeadline(1200, 4000, TimeSpan.FromSeconds(45)));
        yield return Pause("pause during native preparation");
        yield return new SelectionActionStep("inspect the working cook", new[] { cook.ThingID }, false);
        yield return Shot("native preparing ingredients job");
        yield return Run("finish preparation and ordinary bill output hauling", EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep("ten produced portions reach the stockpile", _ =>
        {
            prepared = map.listerThings.ThingsOfDef(preparedDef).SingleOrDefault(thing => thing.Position == storageCell)!;
            return prepared is not null && prepared.stackCount == 10 && cook.carryTracker.CarriedThing is null;
        }, new EndToEndDeadline(1600, 5000, TimeSpan.FromSeconds(60)));
        owned.Add(prepared);
        yield return Pause("pause after native production and delivery");
        yield return new SelectionActionStep("inspect the produced stack", new[] { prepared.ThingID }, false);
        yield return Shot("native output delivered and selected");
        yield return ToggleDraft(context, expected: false);
        yield return Choose(context, destinationMarker, "go here", "move the cook away through the native order");
        yield return Run("let the cook clear the comparison row", EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep("cook is clear of the produced food", _ => cook.Position == destinationMarker.Position,
            new EndToEndDeadline(900, 2000, TimeSpan.FromSeconds(35)));
        yield return Pause("pause the unobscured comparison");
        yield return new SupportingSceneTimeActionStep("restore exact local noon for comparison", "map-" + map.uniqueID, 720);
        yield return new SelectionActionStep("clear visual selection", Array.Empty<string>(), false);
        yield return new ScreenshotModeActionStep("hide interface for identity review", true);
        foreach (var terrain in new[] { TerrainDefOf.Concrete, TerrainDefOf.WoodPlankFloor })
        foreach (var health in new[] { 1f, 0.55f, 0.15f })
        {
            // Supporting visual preconditions; production/hauling above are never manufactured.
            yield return new AssertionStep("arrange ground and damage grade", _ =>
            {
                foreach (var cell in CellRect.CenteredOn(center, 6)) map.terrainGrid.SetTerrain(cell, terrain);
                prepared.HitPoints = Math.Max(1, (int)(prepared.MaxHitPoints * health));
            });
            var previousZoom = 0f;
            foreach (var entry in new[] { ("close", 8f), ("ordinary", 16f), ("far", 24f) })
            {
                var things = comparators.Concat(new[] { station, prepared }).ToArray();
                var rects = things.Select(thing => thing.OccupiedRect()).ToArray();
                var height = rects.Max(rect => rect.maxZ) - rects.Min(rect => rect.minZ) + 1;
                var padding = (int)Math.Round(UnityEngine.Screen.height * (1f - height / (2f * entry.Item2)) / 2f);
                var label = terrain.defName + "-" + health.ToString(CultureInfo.InvariantCulture) + "-" + entry.Item1;
                yield return new CameraActionStep("frame " + label, things.Select(thing => thing.ThingID), padding);
                yield return new AssertionStep("verify distinct actual camera distance " + label, _ =>
                {
                    var actualZoom = Find.Camera.orthographicSize;
                    EndToEndAssert.True(previousZoom == 0f || actualZoom >= previousZoom * 1.3f,
                        "Each subsequent view must enlarge the actual camera distance by at least30percent.");
                    previousZoom = actualZoom;
                });
                yield return Shot(label);
                yield return new CheckpointStep("render state " + label, _ => new Dictionary<string, string>
                {
                    ["thingId"] = prepared.ThingID, ["count"] = prepared.stackCount.ToString(),
                    ["graphicPath"] = prepared.Graphic.path, ["hitPoints"] = prepared.HitPoints.ToString(),
                    ["orthographicSize"] = Find.Camera.orthographicSize.ToString(CultureInfo.InvariantCulture),
                    ["localHour"] = GenLocalDate.HourOfDay(map).ToString(CultureInfo.InvariantCulture)
                });
            }
        }
    }

    private Thing SpawnItem(string defName, IntVec3 cell, int count)
    {
        var thing = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(defName));
        thing.stackCount = count;
        GenSpawn.Spawn(thing, cell, map);
        owned.Add(thing);
        return thing;
    }

    private GizmoActionStep ToggleDraft(IEndToEndContext context, bool expected)
    {
        var options = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { cook.ThingID }, Array.Empty<string>())
            .Where(option => !option.Disabled && option.ToggleState == expected && option.HotKeyDefName == "Command_ColonistDraft").ToArray();
        EndToEndAssert.Equal(1, options.Length, "One native Draft toggle is required.");
        return new GizmoActionStep("toggle native Draft", new[] { cook.ThingID }, options[0].RuntimeType,
            EndToEndGizmoInteraction.Toggle, stableGizmoId: options[0].StableId);
    }

    private FloatMenuActionStep Choose(IEndToEndContext context, Thing target, string text, string label)
    {
        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>().Query(cook.ThingID, target.ThingID);
        var matches = options.Where(option => !option.Disabled && option.Label.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        EndToEndAssert.Equal(1, matches.Length, "Expected one native order; observed " + string.Join("; ", options.Select(option => option.Label)));
        return new FloatMenuActionStep(label, cook.ThingID, target.ThingID, matches[0].StableId);
    }

    private static TimeControlActionStep Pause(string label) => new(label, true, EndToEndGameSpeed.Normal);
    private static TimeControlActionStep Run(string label, EndToEndGameSpeed speed) => new(label, false, speed);
    private static ScreenshotStep Shot(string label) => new(label, Array.Empty<string>(), 0);
}
