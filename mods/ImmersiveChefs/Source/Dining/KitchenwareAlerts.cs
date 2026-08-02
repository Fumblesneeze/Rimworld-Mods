using RimWorld;
using Verse;

namespace ImmersiveChefs;

public sealed class Alert_MissingKitchenware : Alert
{
    public override string GetLabel() => "Missing clean kitchenware";

    public override TaggedString GetExplanation() =>
        "A player colony has cooks and plated meals enabled but lacks at least one clean cookware set, plate, or silverware setting. Craft more kitchenware or clean the highlighted dirty items.";

    public override AlertReport GetReport()
    {
        if (ImmersiveChefsMod.Settings.WareRequirementMode == WareRequirementMode.Off)
        {
            return false;
        }

        var culprits = new List<Thing>();
        foreach (var map in Find.Maps.Where(map => map.IsPlayerHome))
        {
            if (!KitchenwareIsInUse(map))
            {
                continue;
            }

            var ware = map.listerThings.AllThings.Where(IsWare).ToList();
            var missing = new[]
            {
                KitchenwareProduct.Cookware,
                KitchenwareProduct.Plate,
                KitchenwareProduct.Silverware
            }.Any(product => !ware.Any(thing => Product(thing) == product && IsClean(thing)));
            if (!missing)
            {
                continue;
            }

            culprits.AddRange(ware.Where(thing => !IsClean(thing)));
            if (culprits.Count == 0)
            {
                culprits.AddRange(map.mapPawns.FreeColonistsSpawned.Take(1));
            }
        }

        return culprits.Count > 0 ? AlertReport.CulpritsAre(culprits) : false;
    }

    private static bool IsWare(Thing thing) => Product(thing).HasValue;

    private static bool KitchenwareIsInUse(Map map)
    {
        var cooking = DefDatabase<WorkTypeDef>.GetNamedSilentFail("Cooking");
        var hasAvailableCook = map.mapPawns.FreeColonistsSpawned.Any(pawn =>
            !pawn.Downed && !pawn.InMentalState &&
            (cooking is null || !pawn.WorkTypeIsDisabled(cooking)));
        if (!hasAvailableCook)
        {
            return false;
        }

        var hasCoveredMeal = map.listerThings.AllThings.Any(thing =>
            thing.def.IsNutritionGivingIngestible && MealCoveragePolicy.IsCovered(thing.def));
        var hasCoveredBill = map.listerThings.AllThings
            .OfType<IBillGiver>()
            .Any(giver => giver.BillStack.Bills.Any(bill =>
                !bill.suspended && MealCoveragePolicy.IsCovered(bill.recipe)));
        return hasCoveredMeal || hasCoveredBill;
    }

    private static KitchenwareProduct? Product(Thing thing) =>
        thing.def.GetModExtension<KitchenwareExtension>()?.product;
    private static bool IsClean(Thing thing) =>
        (thing as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty != true;
}

public sealed class Alert_DirtyKitchenwareBacklog : Alert
{
    public override string GetLabel() => "Dirty dishes need washing";

    public override TaggedString GetExplanation() =>
        "There is an accumulated backlog of dirty cookware, plates, or silverware. Enable Cleaning work, provide an accessible water source, or build a powered dishwasher.";

    public override AlertReport GetReport()
    {
        var dirty = Find.Maps.Where(map => map.IsPlayerHome)
            .SelectMany(map => map.listerThings.AllThings)
            .Where(thing => (thing as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true)
            .Take(64)
            .ToList();
        return dirty.Count >= 4 ? AlertReport.CulpritsAre(dirty) : false;
    }
}
