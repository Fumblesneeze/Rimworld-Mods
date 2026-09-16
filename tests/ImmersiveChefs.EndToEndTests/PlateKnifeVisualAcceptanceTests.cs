using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest("immersive-chefs.plate-knife-visual-base", "fumblesneeze.immersivechefs",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "imranfish.xmlextensions", "fumblesneeze.immersivechefs",
    MaxFrames = 9000, MaxGameTicks = 16000, MaxWallClockSeconds = 300)]
public sealed class PlateKnifeVisualBaseTest : IRimWorldEndToEndTest
{
    private readonly PlateKnifeVisualFixture fixture = new(false);
    public void Arrange(IEndToEndContext context) => fixture.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => fixture.Execute(context);
}

[RimWorldEndToEndTest("immersive-chefs.plate-knife-visual-vtex", "fumblesneeze.immersivechefs",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "imranfish.xmlextensions",
    "OskarPotocki.VanillaFactionsExpanded.Core", "VanillaExpanded.VTEXVariations", "fumblesneeze.immersivechefs",
    MaxFrames = 9000, MaxGameTicks = 16000, MaxWallClockSeconds = 300)]
public sealed class PlateKnifeVisualVtexTest : IRimWorldEndToEndTest
{
    private readonly PlateKnifeVisualFixture fixture = new(true);
    public void Arrange(IEndToEndContext context) => fixture.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => fixture.Execute(context);
}

internal sealed class PlateKnifeVisualFixture
{
    private readonly bool variations;
    private readonly List<Thing> owned = new();
    private readonly List<Thing> catalog = new();
    private readonly List<(ThingWithComps Meal, ThingWithComps Plate)> meals = new();
    private Map map = null!;
    private Pawn actor = null!;
    private Apparel knife = null!;
    private Apparel replacement = null!;
    private Thing marker = null!;
    private IntVec3 center;
    private IntVec3 water;

    internal PlateKnifeVisualFixture(bool variations) => this.variations = variations;

    internal void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        center = GenRadial.RadialCellsAround(map.Center, 55f, true).First(candidate =>
            CellRect.CenteredOn(candidate, 11).All(cell => cell.InBounds(map) && cell.Walkable(map) && cell.GetEdifice(map) is null));
        water = center + new IntVec3(7, 0, -4);
        var cells = CellRect.CenteredOn(center, 11).ToArray();
        var oldTerrain = cells.ToDictionary(cell => cell, cell => cell.GetTerrain(map));
        var oldHome = cells.ToDictionary(cell => cell, cell => map.areaManager.Home[cell]);
        var screenshotMode = Find.ScreenshotModeHandler.Active;
        var dirtyTextures = ImmersiveChefsMod.Settings.ShowDirtyWareTextures;
        context.DeferCleanup(() =>
        {
            Find.ScreenshotModeHandler.Active = screenshotMode;
            ImmersiveChefsMod.Settings.ShowDirtyWareTextures = dirtyTextures;
            foreach (var thing in owned.Where(thing => !thing.Destroyed).Reverse().ToArray()) thing.Destroy(DestroyMode.Vanish);
            foreach (var cell in cells)
            {
                map.terrainGrid.SetTerrain(cell, oldTerrain[cell]);
                map.areaManager.Home[cell] = oldHome[cell];
            }
        });
        HandwashingE2EFixture.PreserveSettings(context);
        ImmersiveChefsMod.Settings.ShowDirtyWareTextures = true;
        ImmersiveChefsMod.Settings.AllowTerrainHandwashing = true;
        ImmersiveChefsMod.Settings.DishwashingWorkScale = 1f;
        foreach (var cell in cells)
        {
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
            map.areaManager.Home[cell] = true;
        }
        map.terrainGrid.SetTerrain(water, TerrainDefOf.WaterShallow);
        actor = HandwashingE2EFixture.CreateInactiveCleaner("Ware reviewer");
        Spawn(actor, -3, -4);
        actor.drafter.Drafted = true;
        knife = (Apparel)Make("ImmersiveChefs_ChefsKnife", "Steel", -1, -4);
        replacement = (Apparel)Make("ImmersiveChefs_ChefsKnife", "Silver", 1, -4);
        marker = Make("Steel", null, -8, -8);
        var stuffs = variations ? new[] { "WoodLog", "BlocksGranite" } : new[] { "Steel" };
        for (var i = 0; i < stuffs.Length; i++)
        {
            var meal = FoodSearchE2EFixture.MakePlatedMeal(ThingDefOf.MealSimple, DefDatabase<ThingDef>.GetNamed(stuffs[i]), out var plate);
            owned.Add(plate);
            Spawn(meal, i * 2 - 1, -6);
            meals.Add((meal, plate));
        }
        Make("ImmersiveChefs_Cutlery", "Steel", 3, -6);
    }

    internal IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return Pause("pause before native ware use");
        yield return new SupportingSceneTimeActionStep("set local noon", "map-" + map.uniqueID, 720);
        yield return new CameraActionStep("frame the native use area", new[] { actor.ThingID, knife.ThingID, meals[0].Meal.ThingID }, 220);
        yield return Shot("before native wear");
        yield return ToggleDraft(context, true);
        yield return Choose(context, knife, "force equip", "equip the chef's knife through its native menu");
        yield return Run("complete native wear");
        yield return Wait("the exact knife is worn", () => actor.apparel.WornApparel.Contains(knife));
        yield return Pause("inspect worn knife");
        yield return new SelectionActionStep("select the equipped pawn", new[] { actor.ThingID }, false);
        yield return new PawnInspectTabActionStep("open native Gear", actor.ThingID, EndToEndPawnInspectTab.Gear);
        yield return Shot("knife visible in native Gear");
        yield return Choose(context, replacement, "force equip", "equip the replacement belt item");
        yield return Run("complete native replacement");
        yield return Wait("the first knife drops through native replacement", () => knife.Spawned && actor.apparel.WornApparel.Contains(replacement));
        yield return Pause("inspect dropped knife");
        yield return new SelectionActionStep("select the dropped knife", new[] { knife.ThingID }, false);
        yield return Shot("same steel knife on ground after replacement");

        foreach (var pair in meals)
        {
            yield return new AssertionStep("arrange hunger for the next meal", _ => FoodSearchE2EFixture.SetHunger(actor, .25f));
            yield return Choose(context, pair.Meal, "consume", "consume the plated meal through the native menu");
            yield return Run("eat the plated meal");
            yield return Wait("ingestion returns the same physical dirty plate", () => pair.Meal.Destroyed && pair.Plate.Spawned && pair.Plate.GetComp<CompSanitation>().IsDirty);
            yield return Pause("inspect the returned dirty plate");
            yield return new SelectionActionStep("select the used plate", new[] { pair.Plate.ThingID }, false);
            yield return Shot("native dirty plate " + pair.Plate.Stuff.defName);
            yield return new AssertionStep("enable ordinary Cleaning eligibility", _ => actor.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 1));
            yield return Choose(context, pair.Plate, "prioritize", "prioritize washing the used plate");
            yield return Run("start the native dishwashing job");
            yield return Wait("the washer reaches water and is doing the timed wash", () =>
                HandwashingE2EFixture.IsDoingDishesAt(actor, water, pair.Plate.ThingID) &&
                actor.Position.AdjacentTo8WayOrInside(water) && actor.jobs.curDriver.ticksLeftThisToil > 0);
            yield return Pause("observe native washing");
            yield return new SelectionActionStep("select the washing pawn", new[] { actor.ThingID }, false);
            yield return Shot("native washing " + pair.Plate.Stuff.defName);
            yield return Run("finish native washing");
            yield return Wait("the same plate returns clean", () => pair.Plate.Spawned && !pair.Plate.GetComp<CompSanitation>().IsDirty);
            yield return Pause("observe cleaned plate");
            yield return new SelectionActionStep("select the clean result", new[] { pair.Plate.ThingID }, false);
            yield return Shot("native clean plate " + pair.Plate.Stuff.defName);
            yield return new AssertionStep("retain native result evidence and stop automatic cleaning", _ =>
            {
                actor.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 0);
                EndToEndAssert.Equal(1, pair.Plate.stackCount, "The physical plate must remain one unit.");
                EndToEndAssert.True(pair.Plate.GetComp<CompSanitation>().WashedInWildWater, "The real washing path must record its source.");
            });
        }
        yield return ToggleDraft(context, false);
        yield return Choose(context, marker, "go here", "move the pawn clear of the comparison");
        yield return Run("clear the visual comparison area");
        yield return Wait("the pawn reaches the clear cell", () => actor.Position == marker.Position);
        yield return Pause("pause for render comparison");
        yield return new AssertionStep("arrange supporting material and state catalog", _ => ArrangeCatalog());
        foreach (var thing in catalog.Where(thing => thing.def.defName.StartsWith("ImmersiveChefs_", StringComparison.Ordinal)))
            yield return new SelectionActionStep("inspect catalog " + thing.ThingID, new[] { thing.ThingID }, false);
        yield return new SelectionActionStep("clear selection for blind review", Array.Empty<string>(), false);
        yield return new ScreenshotModeActionStep("hide interface", true);
        yield return new SupportingSceneTimeActionStep("restore exact local noon", "map-" + map.uniqueID, 720);
        foreach (var terrain in new[] { TerrainDefOf.Concrete, TerrainDefOf.WoodPlankFloor })
        foreach (var health in new[] { 1f, .55f, .15f })
        {
            yield return new AssertionStep("arrange supporting ground and damage grade", _ =>
            {
                foreach (var cell in CellRect.CenteredOn(center, 10)) map.terrainGrid.SetTerrain(cell, terrain);
                foreach (var thing in catalog.Where(thing => thing.def.defName.StartsWith("ImmersiveChefs_", StringComparison.Ordinal)))
                    thing.HitPoints = Math.Max(1, (int)(thing.MaxHitPoints * health));
            });
            var previousZoom = 0f;
            foreach (var zoom in new[] { 11f, 15f, 20f })
            {
                var height = catalog.Max(thing => thing.Position.z) - catalog.Min(thing => thing.Position.z) + 1;
                var padding = (int)Math.Round(UnityEngine.Screen.height * (1f - height / (2f * zoom)) / 2f);
                var label = terrain.defName + "-" + health.ToString(CultureInfo.InvariantCulture) + "-" + zoom;
                yield return new CameraActionStep("frame " + label, catalog.Select(thing => thing.ThingID), padding);
                yield return new AssertionStep("verify genuinely different native zoom", _ =>
                {
                    var actual = Find.Camera.orthographicSize;
                    EndToEndAssert.True(previousZoom == 0 || actual >= previousZoom * 1.3f, "The actual camera distance must change at least30percent.");
                    previousZoom = actual;
                });
                yield return Shot(label);
                yield return new CheckpointStep("render evidence " + label, _ => catalog.ToDictionary(thing => thing.ThingID,
                    thing => thing.def.defName + ":" + thing.Stuff?.defName + ":dirty=" + thing.TryGetComp<CompSanitation>()?.IsDirty +
                    ":hp=" + thing.HitPoints + ":texture=" + thing.Graphic.MatSingleFor(thing).mainTexture.name +
                    ":zoom=" + Find.Camera.orthographicSize.ToString(CultureInfo.InvariantCulture) + ":hour=" + GenLocalDate.HourOfDay(map)));
            }
        }
    }

    private void ArrangeCatalog()
    {
        var plateStuffs = new[] { "Steel", "WoodLog", "BlocksGranite", "Silver", "Gold", "BlocksLimestone", "BlocksSandstone", "BlocksMarble", "BlocksSlate" };
        for (var i = 0; i < plateStuffs.Length; i++)
        for (var dirty = 0; dirty < 2; dirty++)
        {
            var thing = Make("ImmersiveChefs_Plate", plateStuffs[i], i * 2 - 8, 4 - dirty * 2);
            if (dirty == 1) thing.TryGetComp<CompSanitation>().MarkDirty(); // Catalog-only setup, separate from ingestion proof.
            catalog.Add(thing);
        }
        var metals = new[] { "Steel", "Silver", "Gold", "Uranium", "Plasteel" };
        for (var i = 0; i < metals.Length; i++)
        {
            catalog.Add(Make("ImmersiveChefs_ChefsKnife", metals[i], i * 2 - 4, 0));
            catalog.Add(Make("MeleeWeapon_Knife", metals[i], i * 2 - 4, -2));
        }
        foreach (var entry in new[] { ("MealSimple", -6), ("MedicineIndustrial", 0), ("Pemmican", 6) })
            catalog.Add(Make(entry.Item1, null, entry.Item2, 6));
    }

    private Thing Make(string defName, string? stuff, int x, int z)
    {
        var thing = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(defName), stuff is null ? null : DefDatabase<ThingDef>.GetNamed(stuff));
        thing.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
        Spawn(thing, x, z);
        return thing;
    }

    private void Spawn(Thing thing, int x, int z)
    {
        owned.Add(thing);
        GenSpawn.Spawn(thing, center + new IntVec3(x, 0, z), map);
    }

    private GizmoActionStep ToggleDraft(IEndToEndContext context, bool expected)
    {
        var option = context.GetRequiredService<IEndToEndGizmoCatalog>().Query(new[] { actor.ThingID }, Array.Empty<string>())
            .Single(item => !item.Disabled && item.ToggleState == expected && item.HotKeyDefName == "Command_ColonistDraft");
        return new GizmoActionStep("toggle native Draft", new[] { actor.ThingID }, option.RuntimeType,
            EndToEndGizmoInteraction.Toggle, stableGizmoId: option.StableId);
    }

    private FloatMenuActionStep Choose(IEndToEndContext context, Thing target, string text, string label)
    {
        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>().Query(actor.ThingID, target.ThingID);
        var matches = options.Where(item => !item.Disabled && item.Label.IndexOf(text, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        EndToEndAssert.Equal(1, matches.Length, "Expected one native order; observed " + string.Join("; ", options.Select(item => item.Label)));
        return new FloatMenuActionStep(label, actor.ThingID, target.ThingID, matches[0].StableId);
    }

    private static WaitUntilStep Wait(string label, Func<bool> predicate) => new(label, _ => predicate(), new EndToEndDeadline(1600, 5000, TimeSpan.FromSeconds(55)));
    private static TimeControlActionStep Pause(string label) => new(label, true, EndToEndGameSpeed.Normal);
    private static TimeControlActionStep Run(string label) => new(label, false, EndToEndGameSpeed.Superfast);
    private static ScreenshotStep Shot(string label) => new(label, Array.Empty<string>(), 0);
}
