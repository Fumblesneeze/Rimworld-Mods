using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest("immersive-chefs.cookware-visual-base", "fumblesneeze.immersivechefs",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "imranfish.xmlextensions", "fumblesneeze.immersivechefs",
    MaxFrames = 10000, MaxGameTicks = 30000, MaxWallClockSeconds = 330)]
public sealed class CookwareVisualBaseTest : IRimWorldEndToEndTest
{
    private readonly CookwareVisualFixture fixture = new(false);
    public void Arrange(IEndToEndContext context) => fixture.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => fixture.Execute(context);
}

[RimWorldEndToEndTest("immersive-chefs.cookware-visual-vtex", "fumblesneeze.immersivechefs",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "imranfish.xmlextensions",
    "OskarPotocki.VanillaFactionsExpanded.Core", "VanillaExpanded.VTEXVariations", "fumblesneeze.immersivechefs",
    MaxFrames = 10000, MaxGameTicks = 30000, MaxWallClockSeconds = 330)]
public sealed class CookwareVisualVtexTest : IRimWorldEndToEndTest
{
    private readonly CookwareVisualFixture fixture = new(true);
    public void Arrange(IEndToEndContext context) => fixture.Arrange(context);
    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context) => fixture.Execute(context);
}

internal sealed class CookwareVisualFixture
{
    private readonly bool variations;
    private readonly bool primitive;
    private readonly List<Thing> owned = new();
    private readonly List<Thing> catalog = new();
    private Map map = null!;
    private Pawn actor = null!;
    private ThingWithComps stove = null!;
    private ThingWithComps cookware = null!;
    private ThingWithComps plate = null!;
    private Thing? meal;
    private Thing marker = null!;
    private IntVec3 center;
    private IntVec3 water;

    internal CookwareVisualFixture(bool variations, bool primitive = false)
    {
        this.variations = variations;
        this.primitive = primitive;
    }

    internal void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        center = GenRadial.RadialCellsAround(map.Center, 55f, true).First(candidate =>
            CellRect.CenteredOn(candidate, 11).All(cell => cell.InBounds(map) && cell.Walkable(map) && cell.GetEdifice(map) is null));
        water = center + new IntVec3(7, 0, -4);
        var cells = CellRect.CenteredOn(center, 11).ToArray();
        var terrain = cells.ToDictionary(cell => cell, cell => cell.GetTerrain(map));
        var home = cells.ToDictionary(cell => cell, cell => map.areaManager.Home[cell]);
        var settings = ImmersiveChefsMod.Settings;
        var priorDirty = settings.ShowDirtyWareTextures;
        var priorMode = settings.WareRequirementMode;
        var priorFallback = settings.DirtyWareFallback;
        var priorTemperature = settings.MealTemperatureEnabled;
        var priorAssistants = settings.AutoCallAssistants;
        var priorScreenshotMode = Find.ScreenshotModeHandler.Active;
        context.DeferCleanup(() =>
        {
            settings.ShowDirtyWareTextures = priorDirty;
            settings.WareRequirementMode = priorMode;
            settings.DirtyWareFallback = priorFallback;
            settings.MealTemperatureEnabled = priorTemperature;
            settings.AutoCallAssistants = priorAssistants;
            Find.ScreenshotModeHandler.Active = priorScreenshotMode;
            foreach (var thing in owned.AsEnumerable().Reverse())
                if (!thing.Destroyed) thing.Destroy(DestroyMode.Vanish);
            foreach (var cell in cells)
            {
                map.terrainGrid.SetTerrain(cell, terrain[cell]);
                map.areaManager.Home[cell] = home[cell];
            }
        });
        HandwashingE2EFixture.PreserveSettings(context);
        settings.ShowDirtyWareTextures = true;
        settings.WareRequirementMode = WareRequirementMode.Strict;
        settings.DirtyWareFallback = DirtyWareFallback.Never;
        settings.MealTemperatureEnabled = false;
        settings.AutoCallAssistants = false;
        settings.AllowTerrainHandwashing = true;
        settings.DishwashingWorkScale = 1f;
        foreach (var cell in cells)
        {
            map.terrainGrid.SetTerrain(cell, TerrainDefOf.Concrete);
            map.areaManager.Home[cell] = true;
        }
        map.terrainGrid.SetTerrain(water, TerrainDefOf.WaterShallow);
        actor = HandwashingE2EFixture.CreateInactiveKitchenWorker("Cookware reviewer");
        Spawn(actor, -4, -7);
        actor.workSettings.SetPriority(DefDatabase<WorkTypeDef>.GetNamed("Cooking"), 1);
        actor.drafter.Drafted = true;
        stove = (ThingWithComps)Make("FueledStove", "Steel", -4, -3);
        stove.GetComp<CompRefuelable>()!.Refuel(999f);
        var bill = new Bill_Production(DefDatabase<RecipeDef>.GetNamed("CookMealSimple"))
        {
            repeatMode = BillRepeatModeDefOf.RepeatCount, repeatCount = 1, ingredientSearchRadius = 8f
        };
        bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        bill.SetPawnRestriction(actor);
        ((IBillGiver)stove).BillStack.AddBill(bill);
        // Stone is a retained legacy-item/selector specimen; current ordinary recipes are metallic.
        cookware = FoodSearchE2EFixture.MakeCleanWare(primitive ? "ImmersiveChefs_PrimitiveCookware" : "ImmersiveChefs_Cookware",
            primitive ? DefDatabase<ThingDef>.GetNamed(variations ? "BlocksSlate" : "BlocksGranite") :
            variations ? DefDatabase<ThingDef>.GetNamed("BlocksGranite") : ThingDefOf.Steel);
        Spawn(cookware, -7, -3);
        plate = FoodSearchE2EFixture.MakeCleanWare("ImmersiveChefs_Plate", ThingDefOf.Steel);
        Spawn(plate, -6, -5);
        var rice = Make("RawRice", null, -1, -3);
        rice.stackCount = 20;
        marker = Make("Steel", null, -8, -8);
        catalog.Add(stove);
        catalog.Add(Make("TableButcher", "Steel", 3, -3));
    }

    internal IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return Pause("pause before native cooking");
        yield return new SupportingSceneTimeActionStep("set local noon", "map-" + map.uniqueID, 720);
        yield return new CameraActionStep("frame the cooking workflow", new[] { actor.ThingID, stove.ThingID, cookware.ThingID }, 200);
        yield return Shot("clean cookware before native use");
        yield return ToggleDraft(context, true);
        yield return Choose(context, stove, "prioritize", "prioritize the ordinary cooking bill");
        yield return Run("start ordinary cooking");
        yield return Wait("the native bill visibly uses the exact cookware", () => actor.CurJobDef == JobDefOf.DoBill &&
            CookingSessionRegistry.TryGetActiveWorkProp(actor, out var activeWare, out var activeStove) &&
            ReferenceEquals(activeWare, cookware) && ReferenceEquals(activeStove, stove));
        yield return Pause("observe active cooking");
        yield return new SelectionActionStep("select the working cook", new[] { actor.ThingID }, false);
        yield return Shot("native cooking with the same physical cookware");
        yield return Run("finish the native cooking bill");
        yield return Wait("the bill returns dirty cookware and a plated meal", () => ResolveMeal() && cookware.Spawned && cookware.GetComp<CompSanitation>().IsDirty);
        yield return Pause("observe native dirty result");
        yield return new SelectionActionStep("select the used cookware", new[] { cookware.ThingID }, false);
        yield return Shot("native cooking returned dirty cookware");
        yield return new AssertionStep("allow Cleaning work", _ => actor.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 1));
        yield return Choose(context, cookware, "prioritize", "prioritize washing the used cookware");
        yield return Run("start ordinary water washing");
        yield return Wait("the same cookware reaches timed washing at water", () =>
            HandwashingE2EFixture.IsDoingDishesAt(actor, water, cookware.ThingID) &&
            actor.Position.AdjacentTo8WayOrInside(water) && actor.jobs.curDriver.ticksLeftThisToil > 0);
        yield return Pause("observe timed cookware washing");
        yield return new SelectionActionStep("select the washing pawn", new[] { actor.ThingID }, false);
        yield return Shot("native washing with the same dirty cookware");
        yield return Run("finish native cookware washing");
        yield return Wait("the same cookware returns clean", () => cookware.Spawned && !cookware.GetComp<CompSanitation>().IsDirty);
        yield return Pause("observe native clean result");
        yield return new SelectionActionStep("select the clean cookware", new[] { cookware.ThingID }, false);
        yield return Shot("native washing returned clean cookware");
        yield return new AssertionStep("verify units and stop autonomous cleaning", _ =>
        {
            EndToEndAssert.Equal(1, cookware.stackCount, "The same complete set remains one item.");
            EndToEndAssert.True(cookware.GetComp<CompSanitation>().WashedInWildWater, "The ordinary wash must retain its source.");
            actor.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 0);
        });
        yield return ToggleDraft(context, false);
        yield return Choose(context, marker, "go here", "move clear of the render catalog");
        yield return Run("move the actor clear");
        yield return Wait("the actor reaches the clear cell", () => actor.Position == marker.Position);
        yield return Pause("pause for comparison");
        yield return new AssertionStep("arrange supporting material/condition catalog", _ => ArrangeCatalog());
        foreach (var thing in catalog.Where(thing => thing.def.defName == "ImmersiveChefs_Cookware"))
            yield return new SelectionActionStep("inspect " + thing.ThingID, new[] { thing.ThingID }, false);
        yield return new SelectionActionStep("clear native selection", Array.Empty<string>(), false);
        yield return new ScreenshotModeActionStep("hide comparison interface", true);
        yield return new SupportingSceneTimeActionStep("restore exact local noon", "map-" + map.uniqueID, 720);
        foreach (var floor in new[] { TerrainDefOf.Concrete, TerrainDefOf.WoodPlankFloor })
        foreach (var health in new[] { 1f, .55f, .15f })
        {
            yield return new AssertionStep("arrange supporting floor and hit points", _ =>
            {
                foreach (var cell in CellRect.CenteredOn(center, 10)) map.terrainGrid.SetTerrain(cell, floor);
                foreach (var thing in catalog.Where(thing => thing.def.defName == "ImmersiveChefs_Cookware"))
                    thing.HitPoints = Math.Max(1, (int)(thing.MaxHitPoints * health));
            });
            var previousZoom = 0f;
            foreach (var zoom in new[] { 11f, 15f, 20f })
            {
                var height = catalog.Max(thing => thing.Position.z) - catalog.Min(thing => thing.Position.z) + 1;
                var padding = (int)Math.Round(UnityEngine.Screen.height * (1f - height / (2f * zoom)) / 2f);
                var label = floor.defName + "-" + health.ToString(CultureInfo.InvariantCulture) + "-" + zoom;
                yield return new CameraActionStep("frame " + label, catalog.Select(thing => thing.ThingID), padding);
                yield return new AssertionStep("verify distinct actual zoom", _ =>
                {
                    var actual = Find.Camera.orthographicSize;
                    EndToEndAssert.True(previousZoom == 0 || actual >= previousZoom * 1.3f, "Camera distance must change by at least30percent.");
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

    private bool ResolveMeal()
    {
        if (meal is not null) return true;
        meal = map.listerThings.ThingsOfDef(ThingDefOf.MealSimple).FirstOrDefault(item =>
            ReferenceEquals(item.TryGetComp<CompEmbeddedWare>()?.PeekPlateThing(), plate));
        if (meal is not null) owned.Add(meal);
        return meal is not null;
    }

    private void ArrangeCatalog()
    {
        var stuffs = primitive
            ? new[] { "BlocksGranite", "BlocksSlate", "BlocksSandstone", "BlocksLimestone", "BlocksMarble" }
            : new[] { "Steel", "Silver", "Gold", "Uranium", "Plasteel", "BlocksGranite", "BlocksLimestone", "BlocksSandstone", "BlocksMarble", "BlocksSlate" };
        for (var i = 0; i < stuffs.Length; i++)
        for (var dirty = 0; dirty < 2; dirty++)
        {
            var thing = Make(primitive ? "ImmersiveChefs_PrimitiveCookware" : "ImmersiveChefs_Cookware", stuffs[i], i * 2 - (stuffs.Length - 1), 4 - dirty * 2);
            if (dirty == 1) thing.TryGetComp<CompSanitation>().MarkDirty(); // Supporting render setup, not cooking proof.
            catalog.Add(thing);
        }
        foreach (var entry in new[] { ("ComponentIndustrial", -6), ("MedicineIndustrial", 0), ("Pemmican", 6) })
            catalog.Add(Make(entry.Item1, null, entry.Item2, 6));
    }

    private Thing Make(string defName, string? stuff, int x, int z)
    {
        var def = DefDatabase<ThingDef>.GetNamed(defName);
        var thing = ThingMaker.MakeThing(def, def.MadeFromStuff && stuff is not null ? DefDatabase<ThingDef>.GetNamed(stuff) : null);
        thing.TryGetComp<CompQuality>()?.SetQuality(QualityCategory.Normal, ArtGenerationContext.Colony);
        if (thing is Building) thing.SetFactionDirect(Faction.OfPlayer);
        Spawn(thing, x, z);
        return thing;
    }
    private void Spawn(Thing thing, int x, int z)
    {
        owned.Add(thing);
        GenSpawn.Spawn(thing, center + new IntVec3(x, 0, z), map, Rot4.North);
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
