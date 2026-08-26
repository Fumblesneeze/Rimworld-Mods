using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace RimWorldDevGateway.ReleaseSmoke.EndToEndTests;

[RimWorldEndToEndTest(
    "release.immersive-chefs-subscribed-native-cooking-dining",
    EndToEndTestContract.GatewayPackageId,
    "brrainz.harmony",
    EndToEndTestContract.CorePackageId,
    "imranfish.xmlextensions",
    "fumblesneeze.immersivechefs",
    MaxFrames = 7_200,
    MaxGameTicks = 24_000,
    MaxWallClockSeconds = 240)]
public sealed class SubscribedImmersiveChefsSmokeTest : IRimWorldEndToEndTest
{
    private Map map = null!;
    private Pawn cook = null!;
    private Building_WorkTable stove = null!;
    private RecipeDef recipe = null!;
    private Thing rice = null!;
    private ThingWithComps cookware = null!;
    private ThingWithComps plate = null!;
    private ThingWithComps cutlery = null!;
    private ThingWithComps? meal;
    private string loadedRoot = string.Empty;
    private int initialRiceCount;

    public void Arrange(IEndToEndContext context)
    {
        map = Current.Game.CurrentMap;
        var expectedRoot = Environment.GetEnvironmentVariable("IMMERSIVE_CHEFS_RELEASE_EXPECTED_ROOT");
        EndToEndAssert.True(!string.IsNullOrWhiteSpace(expectedRoot),
            "The release smoke must declare the exact subscribed root before launch.");
        var productMod = LoadedModManager.RunningModsListForReading.Single(mod =>
            string.Equals(mod.PackageId, "fumblesneeze.immersivechefs", StringComparison.OrdinalIgnoreCase));
        loadedRoot = System.IO.Path.GetFullPath(productMod.RootDir).TrimEnd('\\');
        EndToEndAssert.Equal(
            System.IO.Path.GetFullPath(expectedRoot!).TrimEnd('\\'),
            loadedRoot,
            "RimWorld must load the release product from the exact subscribed Workshop directory.");

        var center = FindClearCenter();
        cook = GenerateCook();
        GenSpawn.Spawn(cook, center + (IntVec3.South * 4), map);

        var stoveDef = DefDatabase<ThingDef>.GetNamed("FueledStove");
        stove = (Building_WorkTable)ThingMaker.MakeThing(
            stoveDef,
            stoveDef.MadeFromStuff ? ThingDefOf.Steel : null);
        stove.SetFactionDirect(Faction.OfPlayer);
        GenSpawn.Spawn(stove, center, map, Rot4.North);
        stove.TryGetComp<CompRefuelable>()?.Refuel(999f);

        recipe = DefDatabase<RecipeDef>.GetNamed("CookMealSimple");
        var bill = new Bill_Production(recipe)
        {
            repeatMode = BillRepeatModeDefOf.RepeatCount,
            repeatCount = 1,
            ingredientSearchRadius = 8f
        };
        bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        bill.SetPawnRestriction(cook);
        stove.BillStack.AddBill(bill);

        rice = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("RawRice"));
        rice.stackCount = 20;
        initialRiceCount = rice.stackCount;
        GenSpawn.Spawn(rice, center + (IntVec3.East * 3), map);
        cookware = MakeCleanWare("ImmersiveChefs_Cookware");
        plate = MakeCleanWare("ImmersiveChefs_Plate");
        cutlery = MakeCleanWare("ImmersiveChefs_Cutlery");
        GenSpawn.Spawn(cookware, center + (IntVec3.West * 3), map);
        GenSpawn.Spawn(plate, center + (IntVec3.West * 2), map);
        GenSpawn.Spawn(cutlery, center + (IntVec3.East * 2), map);
        Find.TickManager.Pause();
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        var fixture = new[] { cook.ThingID, stove.ThingID, rice.ThingID, cookware.ThingID, plate.ThingID, cutlery.ThingID };
        yield return new SelectionActionStep("select the subscribed cooking fixture", fixture, additive: false);
        yield return new CameraActionStep("frame the subscribed cooking fixture", fixture, paddingPixels: 180);
        yield return new ScreenshotStep("before the subscribed native cooking order", Array.Empty<string>(), 0);

        var cookOptions = context.GetRequiredService<IEndToEndFloatMenuCatalog>().Query(cook.ThingID, stove.ThingID);
        var prioritize = cookOptions.Where(option =>
                !option.Disabled && option.Label.IndexOf("prioritize", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(1, prioritize.Length,
            "Expected one enabled native Prioritize option; observed " +
            string.Join(", ", cookOptions.Select(option => $"'{option.Label}' disabled={option.Disabled}")));
        yield return new FloatMenuActionStep(
            "prioritize the subscribed simple-meal bill through RimWorld's native float menu",
            cook.ThingID,
            stove.ThingID,
            prioritize[0].StableId);
        yield return new TimeControlActionStep("run the player-ordered subscribed cooking job", false, EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the player order starts the exact native cooking bill",
            _ => cook.CurJobDef == JobDefOf.DoBill && cook.CurJob?.RecipeDef == recipe,
            new EndToEndDeadline(1_200, 4_000, TimeSpan.FromSeconds(40)));
        yield return new ScreenshotStep("subscribed simple-meal bill visibly in progress", new[] { cook.ThingID, stove.ThingID }, 220);
        yield return new WaitUntilStep(
            "the subscribed native bill produces one plated simple meal",
            _ => TryResolveMeal() && cookware.Spawned,
            new EndToEndDeadline(2_400, 9_000, TimeSpan.FromSeconds(75)));
        yield return new TimeControlActionStep("pause after subscribed cooking", true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep(
            "the subscribed cooking path consumes food, binds the exact plate, and dirties cookware",
            _ =>
            {
                EndToEndAssert.NotNull(meal, "The player-ordered bill must produce the meal.");
                EndToEndAssert.True(rice.Destroyed || rice.stackCount < initialRiceCount,
                    "The ordinary bill must consume raw food.");
                EndToEndAssert.Equal(1, ReadIntCompProperty(meal!, "ImmersiveChefs.CompEmbeddedWare", "EmbeddedPlateCount"),
                    "The cooked meal must bind the supplied physical plate.");
                EndToEndAssert.True(ReadBoolCompProperty(cookware, "ImmersiveChefs.CompSanitation", "IsDirty"),
                    "Completed cooking must return the exact cookware dirty.");
            });
        yield return new SelectionActionStep("select the subscribed plated meal", new[] { meal!.ThingID }, additive: false);
        yield return new CameraActionStep("frame the subscribed plated meal inspector", new[] { meal.ThingID, stove.ThingID, cook.ThingID }, 220);
        yield return new ScreenshotStep("subscribed cooked meal with one bound plate", Array.Empty<string>(), 0);

        yield return new AssertionStep("make the cook hungry only for the explicit dining order", _ =>
        {
            SetOnlyCookingPriority(cook, active: false);
            if (cook.needs?.food is { } food) food.CurLevelPercentage = 0.15f;
        });
        var eatOptions = context.GetRequiredService<IEndToEndFloatMenuCatalog>().Query(cook.ThingID, meal.ThingID);
        var consume = eatOptions.Where(option =>
                !option.Disabled && option.Label.IndexOf("consume", StringComparison.OrdinalIgnoreCase) >= 0)
            .ToArray();
        EndToEndAssert.Equal(1, consume.Length, "Expected one enabled native Consume option for the cooked meal.");
        yield return new FloatMenuActionStep(
            "consume the subscribed plated meal through RimWorld's native float menu",
            cook.ThingID,
            meal.ThingID,
            consume[0].StableId);
        yield return new TimeControlActionStep("run the player-ordered subscribed dining job", false, EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep(
            "the native Ingest job collects cutlery and begins dining",
            _ => cook.CurJobDef == JobDefOf.Ingest,
            new EndToEndDeadline(900, 2_000, TimeSpan.FromSeconds(30)));
        yield return new ScreenshotStep("subscribed meal visibly being eaten", new[] { cook.ThingID, meal.ThingID }, 180);
        yield return new WaitUntilStep(
            "native dining consumes the meal and returns the exact dirty service ware",
            _ => meal.Destroyed && plate.Spawned && cutlery.Spawned &&
                 ReadBoolCompProperty(plate, "ImmersiveChefs.CompSanitation", "IsDirty") &&
                 ReadBoolCompProperty(cutlery, "ImmersiveChefs.CompSanitation", "IsDirty"),
            new EndToEndDeadline(1_800, 6_000, TimeSpan.FromSeconds(50)));
        yield return new TimeControlActionStep("pause after subscribed dining", true, EndToEndGameSpeed.Normal);
        yield return new SelectionActionStep("select the returned subscribed plate", new[] { plate.ThingID }, additive: false);
        yield return new CameraActionStep("frame the returned plate and cutlery", new[] { plate.ThingID, cutlery.ThingID, cook.ThingID }, 220);
        yield return new ScreenshotStep("subscribed returned plate visibly dirty", Array.Empty<string>(), 0);
        yield return new SelectionActionStep("select the returned subscribed cutlery", new[] { cutlery.ThingID }, additive: false);
        yield return new ScreenshotStep("subscribed returned cutlery visibly dirty", Array.Empty<string>(), 0);
        yield return new CheckpointStep(
            "subscribed Workshop native cooking and dining result",
            _ => new Dictionary<string, string>
            {
                ["loadedRoot"] = loadedRoot,
                ["playerActions"] = "native Prioritize and Consume float-menu callbacks",
                ["mealConsumed"] = meal.Destroyed.ToString(),
                ["plateReturnedDirty"] = ReadBoolCompProperty(plate, "ImmersiveChefs.CompSanitation", "IsDirty").ToString(),
                ["cutleryReturnedDirty"] = ReadBoolCompProperty(cutlery, "ImmersiveChefs.CompSanitation", "IsDirty").ToString(),
                ["cookwareReturnedDirty"] = ReadBoolCompProperty(cookware, "ImmersiveChefs.CompSanitation", "IsDirty").ToString()
            });
    }

    private bool TryResolveMeal()
    {
        meal ??= map.listerThings.ThingsOfDef(ThingDefOf.MealSimple)
            .OfType<ThingWithComps>()
            .FirstOrDefault(thing => thing.Spawned);
        return meal is not null;
    }

    private static ThingWithComps MakeCleanWare(string defName)
    {
        var thing = (ThingWithComps)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(defName), ThingDefOf.Steel);
        EndToEndAssert.False(ReadBoolCompProperty(thing, "ImmersiveChefs.CompSanitation", "IsDirty"),
            defName + " must begin clean.");
        return thing;
    }

    private static bool ReadBoolCompProperty(ThingWithComps thing, string typeName, string propertyName) =>
        (bool)ReadCompProperty(thing, typeName, propertyName);

    private static int ReadIntCompProperty(ThingWithComps thing, string typeName, string propertyName) =>
        (int)ReadCompProperty(thing, typeName, propertyName);

    private static object ReadCompProperty(ThingWithComps thing, string typeName, string propertyName)
    {
        var comp = thing.AllComps.Single(candidate => candidate.GetType().FullName == typeName);
        var property = comp.GetType().GetProperty(propertyName) ??
                       throw new EndToEndAssertionException(typeName + "." + propertyName + " is unavailable.");
        return property.GetValue(comp, null) ??
               throw new EndToEndAssertionException(typeName + "." + propertyName + " returned null.");
    }

    private static Pawn GenerateCook()
    {
        var cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        for (var attempt = 0; attempt < 64; attempt++)
        {
            var pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
            if (pawn.WorkTypeIsDisabled(cooking) ||
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Moving) < 0.9f ||
                pawn.health.capacities.GetLevel(PawnCapacityDefOf.Manipulation) < 0.9f)
            {
                pawn.Destroy(DestroyMode.Vanish);
                continue;
            }
            pawn.workSettings.EnableAndInitialize();
            foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
                if (!pawn.WorkTypeIsDisabled(workType)) pawn.workSettings.SetPriority(workType, 0);
            pawn.workSettings.SetPriority(cooking, 1);
            pawn.skills.GetSkill(SkillDefOf.Cooking).Level = 12;
            if (pawn.needs?.food is { } food) food.CurLevelPercentage = 1f;
            return pawn;
        }
        throw new EndToEndAssertionException("Could not generate a capable release-smoke cook.");
    }

    private static void SetOnlyCookingPriority(Pawn pawn, bool active)
    {
        var cooking = DefDatabase<WorkTypeDef>.GetNamed("Cooking");
        foreach (var workType in DefDatabase<WorkTypeDef>.AllDefsListForReading)
            if (!pawn.WorkTypeIsDisabled(workType)) pawn.workSettings.SetPriority(workType, 0);
        if (active) pawn.workSettings.SetPriority(cooking, 1);
    }

    private IntVec3 FindClearCenter()
    {
        foreach (var candidate in GenRadial.RadialCellsAround(map.Center, 55f, useCenter: true))
            if (CellRect.CenteredOn(candidate, 12).Cells.All(cell =>
                    cell.InBounds(map) && cell.Standable(map) && !map.roofGrid.Roofed(cell) && cell.GetThingList(map).Count == 0))
                return candidate;
        throw new EndToEndAssertionException("Could not find a clear release-smoke fixture area.");
    }
}
