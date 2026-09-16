using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest("immersive-chefs.mixed-tableware-dining", "fumblesneeze.immersivechefs",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "imranfish.xmlextensions", "fumblesneeze.immersivechefs",
    MaxFrames = 9000, MaxGameTicks = 16000, MaxWallClockSeconds = 240)]
public sealed class MixedTablewareDiningTest : IRimWorldEndToEndTest
{
    private PlateMaterialTierTest.Fixture fixture = null!;
    private Thing cutlery = null!;
    private Thing? usedCutlery;
    private Thing? usedPlate;
    public void Arrange(IEndToEndContext context)
    {
        var map = Find.CurrentMap;
        var center = PlateMaterialTierTest.FindRoomCenters(map, 1)[0];
        fixture = PlateMaterialTierTest.CreateFixture(map, center, "Mixed tableware cook", DefDatabase<ThingDef>.GetNamed("MealLavish"),
            ThingDefOf.WoodLog, Array.Empty<ThingDef>());
        var gold = ThingMaker.MakeThing(fixture.Plate.def, ThingDefOf.Gold);
        EndToEndAssert.True(fixture.Plate.TryAbsorbStack(gold, true), "Supporting mixed pile must contain wood then gold.");
        cutlery = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_Cutlery"), ThingDefOf.Steel);
        EndToEndAssert.True(cutlery.TryAbsorbStack(ThingMaker.MakeThing(cutlery.def, ThingDefOf.Gold), true), "Supporting cutlery pile must merge.");
        GenSpawn.Spawn(cutlery, center + new IntVec3(2, 0, -2), map);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return new SupportingSceneTimeActionStep("noon for native tableware use", "map-" + Find.CurrentMap.uniqueID, 720);
        yield return new CameraActionStep("frame mixed ware and unplated lavish meal",
            new[] { fixture.Stove.ThingID, fixture.Plate.ThingID, fixture.Meal.ThingID, cutlery.ThingID }, 220);
        yield return new SelectionActionStep("inspect wood and gold pile", new[] { fixture.Plate.ThingID }, false);
        yield return new ScreenshotStep("before native eligible plating", Array.Empty<string>(), 0);
        yield return new TimeControlActionStep("allow ordinary cooking work", false, EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep("ordinary job plates lavish meal from eligible hidden gold unit", _ =>
            fixture.Meal.Spawned && fixture.Meal.GetComp<CompEmbeddedWare>().EmbeddedPlateCount == 1,
            new EndToEndDeadline(3000, 7000, TimeSpan.FromSeconds(90)));
        yield return new TimeControlActionStep("pause after native plating", true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep("only gold was taken from mixed pile", _ =>
        {
            usedPlate = fixture.Meal.GetComp<CompEmbeddedWare>().PeekPlateThing();
            EndToEndAssert.True(usedPlate?.Stuff == ThingDefOf.Gold, "Lavish plating must take actual gold, not exposed wood.");
            EndToEndAssert.True(fixture.Plate.Spawned && fixture.Plate.stackCount == 1 && fixture.Plate.Stuff == ThingDefOf.WoodLog,
                "The ineligible wooden plate remains available.");
            FoodSearchE2EFixture.SetHunger(fixture.Pawn, .25f);
        });
        yield return new SelectionActionStep("inspect native plated meal", new[] { fixture.Meal.ThingID }, false);
        yield return new ScreenshotStep("gold plate bound while wood remains", Array.Empty<string>(), 0);
        var option = context.GetRequiredService<IEndToEndFloatMenuCatalog>().Query(fixture.Pawn.ThingID, fixture.Meal.ThingID)
            .Single(o => !o.Disabled && o.Label.IndexOf("consume", StringComparison.OrdinalIgnoreCase) >= 0);
        yield return new FloatMenuActionStep("consume the plated meal", fixture.Pawn.ThingID, fixture.Meal.ThingID, option.StableId);
        yield return new TimeControlActionStep("collect one cutlery setting and eat", false, EndToEndGameSpeed.Superfast);
        yield return new WaitUntilStep("diner carries the split physical cutlery setting", _ =>
        {
            usedCutlery = DiningSessionRegistry.Current(fixture.Pawn)?.CarriedCutlery;
            return usedCutlery is not null;
        }, new EndToEndDeadline(1800, 4000, TimeSpan.FromSeconds(50)));
        yield return new WaitUntilStep("native eating returns both used items dirty", _ => fixture.Meal.Destroyed &&
            usedPlate!.Spawned && usedPlate.TryGetComp<CompSanitation>().IsDirty &&
            usedCutlery!.Spawned && usedCutlery.TryGetComp<CompSanitation>().IsDirty,
            new EndToEndDeadline(1800, 4000, TimeSpan.FromSeconds(50)));
        yield return new TimeControlActionStep("pause after native dining", true, EndToEndGameSpeed.Normal);
        yield return new AssertionStep("cutlery split and returned items retain real materials without quality", _ =>
        {
            EndToEndAssert.True(usedCutlery!.Stuff == ThingDefOf.Steel && cutlery.Stuff == ThingDefOf.Gold && cutlery.stackCount == 1,
                "Actual steel setting is used and gold remains in the source pile.");
            EndToEndAssert.True(usedCutlery.TryGetComp<CompQuality>() is null && usedPlate!.TryGetComp<CompQuality>() is null,
                "Dining must not recreate obsolete quality components.");
        });
        yield return new SelectionActionStep("inspect returned ungraded cutlery", new[] { usedCutlery!.ThingID }, false);
        yield return new ScreenshotStep("actual materials after native dining", Array.Empty<string>(), 0);
    }
}
