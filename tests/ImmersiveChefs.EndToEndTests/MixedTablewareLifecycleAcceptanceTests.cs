using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using RimWorld.Planet;
using RimWorldDevGateway.EndToEndTesting;
using Verse;

namespace ImmersiveChefs.EndToEndTests;

[RimWorldEndToEndTest("immersive-chefs.mixed-tableware-lavish-bill", "fumblesneeze.immersivechefs",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "imranfish.xmlextensions", "fumblesneeze.immersivechefs",
    MaxFrames = 6000, MaxGameTicks = 18000, MaxWallClockSeconds = 180)]
public sealed class MixedTablewareLavishBillTest : IRimWorldEndToEndTest
{
    private PlateMaterialTierTest.Fixture fixture = null!;
    private ThingWithComps? product;
    public void Arrange(IEndToEndContext context)
    {
        var map = Find.CurrentMap;
        var center = PlateMaterialTierTest.FindRoomCenters(map, 1)[0];
        fixture = PlateMaterialTierTest.CreateFixture(map, center, "Mixed plate lavish chef",
            DefDatabase<ThingDef>.GetNamed("MealLavish"), ThingDefOf.WoodLog, Array.Empty<ThingDef>());
        // Remove the imported meal: this scenario must produce its meal through a bill.
        fixture.Meal.Destroy(DestroyMode.Vanish);
        fixture.Pawn.skills.GetSkill(SkillDefOf.Cooking).Level = 20;
        var gold = MixedTablewareAcceptance.Ware("Plate", ThingDefOf.Gold, 1, 37);
        EndToEndAssert.True(fixture.Plate.TryAbsorbStack(gold, true), "Arrange wood followed by an eligible gold plate.");
        GenSpawn.Spawn(MixedTablewareAcceptance.Ware("Cookware", ThingDefOf.Steel, 1, 60),
            center + new IntVec3(-3, 0, 1), map);
        foreach (var ingredient in new[] { ("RawRice", new IntVec3(2, 0, 2)), ("Meat_Muffalo", new IntVec3(3, 0, 2)) })
        {
            var thing = ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed(ingredient.Item1));
            thing.stackCount = 40;
            GenSpawn.Spawn(thing, center + ingredient.Item2, map);
        }
        var bill = new Bill_Production(DefDatabase<RecipeDef>.GetNamed("CookMealLavish"))
        { repeatMode = BillRepeatModeDefOf.RepeatCount, repeatCount = 1, ingredientSearchRadius = 9f };
        bill.SetStoreMode(BillStoreModeDefOf.DropOnFloor);
        bill.SetPawnRestriction(fixture.Pawn);
        ((IBillGiver)fixture.Stove).BillStack.AddBill(bill);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return MixedTablewareAcceptance.Pause("pause before lavish bill");
        yield return new CameraActionStep("frame mixed plates and lavish bill", new[] { fixture.Plate.ThingID, fixture.Stove.ThingID }, 230);
        yield return new SelectionActionStep("inspect mixed wood and gold before cooking", new[] { fixture.Plate.ThingID }, false);
        yield return MixedTablewareAcceptance.Shot("wood and gold before native lavish cooking");
        yield return MixedTablewareAcceptance.Order(context, fixture.Pawn, fixture.Stove, "prioritize", "prioritize the native lavish bill");
        yield return MixedTablewareAcceptance.Run("cook lavish meal with eligible hidden gold plate");
        yield return new WaitUntilStep("native lavish bill produces its plated meal", _ =>
        {
            product = Find.CurrentMap.listerThings.ThingsOfDef(DefDatabase<ThingDef>.GetNamed("MealLavish")).OfType<ThingWithComps>()
                .FirstOrDefault(t => t.GetComp<CompEmbeddedWare>()?.EmbeddedPlateCount == 1);
            return product is not null;
        }, new EndToEndDeadline(4800, 14000, TimeSpan.FromSeconds(130)));
        yield return MixedTablewareAcceptance.Pause("pause after lavish cooking");
        yield return new AssertionStep("bill consumed the actual eligible unit only", _ =>
        {
            var plate = product!.GetComp<CompEmbeddedWare>().PeekPlateThing();
            EndToEndAssert.True(plate?.Stuff == ThingDefOf.Gold && plate.HitPoints == 37,
                "The newly cooked lavish meal must contain the actual gold plate with its original HP.");
            EndToEndAssert.True(fixture.Plate.Spawned && fixture.Plate.stackCount == 1 && fixture.Plate.Stuff == ThingDefOf.WoodLog,
                "The ineligible wooden unit must remain in the source pile.");
        });
        yield return new CameraActionStep("frame cooked meal and remaining wood plate", new[] { product!.ThingID, fixture.Plate.ThingID }, 230);
        yield return new SelectionActionStep("inspect native lavish meal", new[] { product.ThingID }, false);
        yield return MixedTablewareAcceptance.Shot("lavish meal cooked on gold while wood remains");
    }
}

[RimWorldEndToEndTest("immersive-chefs.mixed-tableware-partial-washing", "fumblesneeze.immersivechefs",
    "brrainz.harmony", EndToEndTestContract.CorePackageId, "imranfish.xmlextensions", "fumblesneeze.immersivechefs",
    MaxFrames = 6000, MaxGameTicks = 18000, MaxWallClockSeconds = 180)]
public sealed class MixedTablewarePartialWashingTest : IRimWorldEndToEndTest
{
    private Pawn cleaner = null!;
    private ThingWithComps dishwasher = null!;
    private ThingWithComps source = null!;
    private Thing? admitted;
    public void Arrange(IEndToEndContext context)
    {
        var map = Find.CurrentMap;
        var center = FoodSearchE2EFixture.FindRoomCenter(map);
        FoodSearchE2EFixture.BuildSealedRoom(map, center);
        HandwashingE2EFixture.PreserveSettings(context);
        ImmersiveChefsMod.Settings.PreferDishwashers = true;
        ImmersiveChefsMod.Settings.AllowTerrainHandwashing = false;
        ImmersiveChefsMod.Settings.DishwashingWorkScale = .25f;
        dishwasher = DispenserE2EFixture.SpawnBuilding(map, "ImmersiveChefs_Dishwasher", center);
        DispenserE2EFixture.SpawnBuilding(map, "VanometricPowerCell", center + IntVec3.North * 3);
        DispenserE2EFixture.SettlePower(map, new[] { dishwasher }, 1000);
        cleaner = HandwashingE2EFixture.CreateInactiveCleaner("Mixed batch washer");
        cleaner.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 1);
        GenSpawn.Spawn(cleaner, center + new IntVec3(-3, 0, -2), map);
        source = MixedTablewareAcceptance.Ware("Plate", ThingDefOf.Steel, 10, 60, dirty: true);
        var gold = MixedTablewareAcceptance.Ware("Plate", ThingDefOf.Gold, 8, 35, dirty: true);
        EndToEndAssert.True(source.TryAbsorbStack(gold, true), "Arrange one dirty stack containing 10 steel and 8 gold plates.");
        GenSpawn.Spawn(source, center + IntVec3.West * 2, map);
    }

    public IEnumerator<EndToEndStep> Execute(IEndToEndContext context)
    {
        yield return MixedTablewareAcceptance.Pause("pause before partial mixed washing");
        yield return new CameraActionStep("frame eighteen dirty mixed plates and sixteen-slot dishwasher", new[] { source.ThingID, dishwasher.ThingID }, 240);
        yield return new SelectionActionStep("inspect the original dirty mixed pile", new[] { source.ThingID }, false);
        yield return MixedTablewareAcceptance.Shot("eighteen dirty mixed plates before native admission");
        yield return MixedTablewareAcceptance.Order(context, cleaner, source, "dishes", "prioritize native mixed-batch washing");
        yield return MixedTablewareAcceptance.Run("admit only the sixteen plates that fit");
        yield return new WaitUntilStep("partial admission retains a two-gold remainder", _ =>
        {
            admitted = dishwasher.GetComp<CompDishwasher>().GetDirectlyHeldThings().FirstOrDefault(t => t.def == source.def);
            return admitted is not null && admitted.stackCount == 16 && source.Spawned && source.stackCount == 2;
        }, new EndToEndDeadline(1800, 5000, TimeSpan.FromSeconds(55)));
        yield return MixedTablewareAcceptance.Pause("pause after partial admission");
        // Prevent a subsequent autonomous job from washing the intentionally retained remainder.
        cleaner.workSettings.SetPriority(WorkTypeDefOf.Cleaning, 0);
        yield return new AssertionStep("admission preserves exact ordered material and HP", _ =>
        {
            MixedTablewareAcceptance.AssertUnits(admitted!, ("Steel", 60, 10), ("Gold", 35, 6));
            MixedTablewareAcceptance.AssertUnits(source, ("Gold", 35, 2));
            EndToEndAssert.True(source.GetComp<CompSanitation>().IsDirty, "Unadmitted units remain dirty.");
        });
        yield return new SelectionActionStep("inspect the dishwasher processing the admitted mixed batch", new[] { dishwasher.ThingID }, false);
        yield return MixedTablewareAcceptance.Shot("native partial washing leaves two dirty gold plates outside");
        yield return MixedTablewareAcceptance.Run("finish the admitted mixed cycle");
        yield return new WaitUntilStep("the exact admitted mixed batch returns clean", _ =>
            admitted!.Spawned && admitted.TryGetComp<CompSanitation>()?.IsDirty == false,
            new EndToEndDeadline(2400, 7000, TimeSpan.FromSeconds(75)));
        yield return MixedTablewareAcceptance.Pause("pause after mixed washing");
        yield return new AssertionStep("washing preserves both the cleaned batch and dirty remainder", _ =>
        {
            MixedTablewareAcceptance.AssertUnits(admitted!, ("Steel", 60, 10), ("Gold", 35, 6));
            MixedTablewareAcceptance.AssertUnits(source, ("Gold", 35, 2));
            EndToEndAssert.True(source.GetComp<CompSanitation>().IsDirty && !admitted!.CanStackWith(source),
                "Native sanitation boundaries must keep the clean output and dirty remainder separate.");
        });
        yield return new CameraActionStep("frame cleaned batch and dirty remainder", new[] { admitted!.ThingID, source.ThingID }, 240);
        yield return new SelectionActionStep("inspect the same cleaned mixed batch", new[] { admitted.ThingID }, false);
        yield return MixedTablewareAcceptance.Shot("sixteen mixed plates clean and two gold plates still dirty");
    }
}

internal static class MixedTablewareAcceptance
{
    internal static ThingWithComps Ware(string suffix, ThingDef material, int count, int hitPoints, bool dirty = false)
    {
        var ware = (ThingWithComps)ThingMaker.MakeThing(DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_" + suffix), material);
        ware.stackCount = count;
        ware.HitPoints = hitPoints;
        if (dirty) ware.GetComp<CompSanitation>().MarkDirty();
        ware.SetForbidden(false, false);
        return ware;
    }

    internal static void AssertUnits(Thing thing, params (string Material, int HitPoints, int Count)[] expected)
    {
        var actual = thing.TryGetComp<CompTablewareStack>().Units.Select(u => u.MaterialDefName + ":" + u.HitPoints);
        var wanted = expected.SelectMany(e => Enumerable.Repeat(e.Material + ":" + e.HitPoints, e.Count));
        EndToEndAssert.True(actual.SequenceEqual(wanted) && thing.stackCount == expected.Sum(e => e.Count),
            "The physical stack and ordered per-unit ledger must match: " + string.Join(",", wanted));
    }

    internal static FloatMenuActionStep Order(IEndToEndContext context, Pawn pawn, Thing target, string labelFragment, string name)
    {
        var options = context.GetRequiredService<IEndToEndFloatMenuCatalog>().Query(pawn.ThingID, target.ThingID);
        var matches = options.Where(o => !o.Disabled && o.Label.IndexOf(labelFragment, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
        EndToEndAssert.Equal(1, matches.Length, "Exactly one native order must match: " + string.Join("; ", options.Select(o => o.Label)));
        return new FloatMenuActionStep(name, pawn.ThingID, target.ThingID, matches.Single().StableId);
    }
    internal static TimeControlActionStep Pause(string name) => new(name, true, EndToEndGameSpeed.Normal);
    internal static TimeControlActionStep Run(string name) => new(name, false, EndToEndGameSpeed.Superfast);
    internal static ScreenshotStep Shot(string name) => new(name, Array.Empty<string>(), 0);
}
