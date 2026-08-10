using RimWorld;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

public sealed class Alert_MissingKitchenware : Alert
{
    public override string GetLabel() => KitchenwareAlertRuntime.MissingLabel(
        KitchenwareAlertRuntime.CaptureMissingShortages().Select(shortage => shortage.Product));

    public override TaggedString GetExplanation()
    {
        var shortages = KitchenwareAlertRuntime.CaptureMissingShortages();
        if (shortages.Count == 0)
        {
            return "ImmersiveChefs_AlertMissingKitchenware_DefaultExplanation".Translate();
        }

        var products = shortages
            .Select(shortage => shortage.Product)
            .Distinct()
            .OrderBy(product => product)
            .Select(KitchenwareAlertRuntime.ProductLabel);
        return "ImmersiveChefs_AlertMissingKitchenware_Explanation".Translate(
            KitchenwareAlertRuntime.JoinWithAnd(products));
    }

    public override AlertReport GetReport()
    {
        var culprits = KitchenwareAlertRuntime.CaptureMissingShortages()
            .SelectMany(shortage => shortage.Culprits)
            .Where(thing => thing is not null && !thing.Destroyed)
            .Distinct()
            .ToList();
        return culprits.Count > 0 ? AlertReport.CulpritsAre(culprits) : false;
    }
}

internal sealed class KitchenwareShortage
{
    internal KitchenwareShortage(
        KitchenwareProduct product,
        IReadOnlyList<Thing> culprits)
    {
        Product = product;
        Culprits = culprits;
    }

    internal KitchenwareProduct Product { get; }

    internal IReadOnlyList<Thing> Culprits { get; }
}

internal sealed class KitchenwareAlertMapState
{
    internal KitchenwareAlertMapState(Map map, IReadOnlyList<Pawn> cooks)
    {
        Map = map;
        Cooks = cooks;
    }

    internal Map Map { get; }

    internal IReadOnlyList<Pawn> Cooks { get; }

    internal Dictionary<KitchenwareProduct, List<Thing>> Targets { get; } = new();

    internal HashSet<KitchenwareProduct> PresentProducts { get; } = new();

    internal bool PhysicalInventoryScanReliable { get; set; } = true;
}

internal static class KitchenwareAlertRuntime
{
    internal static IReadOnlyList<KitchenwareShortage> CaptureMissingShortages()
    {
        if (ImmersiveChefsMod.Settings.WareRequirementMode != WareRequirementMode.Strict)
        {
            return Array.Empty<KitchenwareShortage>();
        }

        var result = new List<KitchenwareShortage>();
        foreach (var map in Find.Maps.Where(map => map.IsPlayerHome))
        {
            KitchenwareAlertMapState state;
            try
            {
                state = CaptureMap(map);
            }
            catch (Exception exception)
            {
                Log.WarningOnce(
                    $"[ImmersiveChefs] Suppressed the kitchenware alert on map {map.uniqueID} " +
                    $"because its scan failed with {exception.GetType().Name}.",
                    unchecked(0x4B415700 + map.uniqueID));
                continue;
            }

            foreach (var product in state.Targets.Keys.OrderBy(value => value))
            {
                if (!KitchenwareAlertPolicy.IsAlertWorthyAbsence(
                        state.PhysicalInventoryScanReliable,
                        state.PresentProducts.Contains(product)))
                {
                    continue;
                }

                result.Add(new KitchenwareShortage(
                    product,
                    state.Targets[product]
                        .Where(thing => !thing.Destroyed)
                        .ToList()));
            }
        }

        return result;
    }

    internal static KitchenwareAlertMapState CaptureMap(Map map)
    {
        var cooking = DefDatabase<WorkTypeDef>.GetNamedSilentFail("Cooking");
        var cooks = map.mapPawns.FreeColonistsSpawned
            .Where(pawn => KitchenwareAlertPolicy.IsEligibleAlertWorker(
                pawn.Drafted,
                pawn.Downed,
                pawn.InMentalState))
            .Where(pawn => cooking is null ||
                           pawn.workSettings?.WorkIsActive(cooking) == true)
            .ToList();
        var state = new KitchenwareAlertMapState(map, cooks);
        CaptureRequirements(state);
        CapturePresentProducts(state);
        return state;
    }

    internal static string ProductTranslationKey(KitchenwareProduct product) => product switch
    {
        KitchenwareProduct.Cookware => "ImmersiveChefs_Product_CookwarePlural",
        KitchenwareProduct.Plate => "ImmersiveChefs_Product_Plates",
        KitchenwareProduct.Cutlery => "ImmersiveChefs_Product_Cutlery",
        _ => "ImmersiveChefs_Product_Kitchenware"
    };

    internal static string ProductLabel(KitchenwareProduct product) =>
        ProductTranslationKey(product).Translate();

    internal static IReadOnlyList<string> MissingProductTranslationKeys(
        IEnumerable<KitchenwareProduct> products) => products
        .Distinct()
        .OrderBy(product => product)
        .Select(ProductTranslationKey)
        .ToArray();

    internal static string MissingLabel(IEnumerable<KitchenwareProduct> products)
    {
        var labels = MissingProductTranslationKeys(products)
            .Select(key => key.Translate().ToString())
            .ToArray();
        return labels.Length == 0
            ? "ImmersiveChefs_AlertMissingKitchenware_DefaultLabel".Translate()
            : "ImmersiveChefs_AlertMissingKitchenware_Label".Translate(JoinWithAnd(labels));
    }

    internal static string JoinWithAnd(IEnumerable<string> values)
    {
        var items = values.ToArray();
        return items.Length switch
        {
            0 => string.Empty,
            1 => items[0],
            _ => "ImmersiveChefs_ListAnd".Translate(string.Join(", ", items.Take(items.Length - 1)), items[^1])
        };
    }

    private static void CaptureRequirements(KitchenwareAlertMapState state)
    {
        if (state.Cooks.Count == 0)
        {
            return;
        }

        foreach (var station in state.Map.listerThings.AllThings
                     .OfType<Building_WorkTable>()
                     .Where(station => station is IBillGiver))
        {
            var playerOwned = station.Faction == Faction.OfPlayerSilentFail;
            var alertEnabled =
                station.def.GetModExtension<KitchenwareAlertStationExtension>()?.enabled == true;
            var operational = IsOperational(station);
            var hasEligibleCook = state.Cooks.Any(pawn => CanReachStation(pawn, station));
            var bills = ((IBillGiver)station).BillStack.Bills;
            foreach (var bill in bills)
            {
                var shouldDoNow = bill is Bill_Production production && ShouldDoNow(production);
                if (!KitchenwareAlertPolicy.IsRelevantBill(
                        ImmersiveChefsMod.Settings.WareRequirementMode,
                        playerOwned,
                        alertEnabled,
                        operational,
                        bill.suspended,
                        shouldDoNow,
                        MealCoveragePolicy.IsCovered(bill.recipe),
                        hasEligibleCook))
                {
                    continue;
                }

                AddTarget(state, KitchenwareProduct.Cookware, station);
                AddTarget(state, KitchenwareProduct.Plate, station);
            }
        }
    }

    private static void CapturePresentProducts(KitchenwareAlertMapState state)
    {
        var seenThings = new HashSet<Thing>();
        var seenHolders = new HashSet<IThingHolder>();
        var holders = new Queue<IThingHolder>();

        void VisitThing(Thing thing)
        {
            if (!seenThings.Add(thing))
            {
                return;
            }

            if (Product(thing) is { } product && thing.stackCount > 0)
            {
                state.PresentProducts.Add(product);
            }

            if (thing is IThingHolder holder)
            {
                holders.Enqueue(holder);
            }
        }

        foreach (var thing in state.Map.listerThings.AllThings)
        {
            VisitThing(thing);
        }

        while (holders.Count > 0)
        {
            var holder = holders.Dequeue();
            if (!seenHolders.Add(holder))
            {
                continue;
            }

            ThingOwner? directlyHeld;
            try
            {
                directlyHeld = holder.GetDirectlyHeldThings();
            }
            catch (Exception)
            {
                directlyHeld = null;
                state.PhysicalInventoryScanReliable = false;
            }

            if (directlyHeld is not null)
            {
                foreach (var thing in directlyHeld)
                {
                    VisitThing(thing);
                }
            }

            var children = new List<IThingHolder>();
            try
            {
                holder.GetChildHolders(children);
            }
            catch (Exception)
            {
                children.Clear();
                state.PhysicalInventoryScanReliable = false;
            }

            foreach (var child in children.Where(child => child is not null))
            {
                holders.Enqueue(child);
            }
        }
    }

    private static void AddTarget(
        KitchenwareAlertMapState state,
        KitchenwareProduct product,
        Thing target)
    {
        if (!state.Targets.TryGetValue(product, out var targets))
        {
            targets = new List<Thing>();
            state.Targets.Add(product, targets);
        }

        if (!targets.Contains(target))
        {
            targets.Add(target);
        }
    }

    private static bool CanReachStation(Pawn pawn, Building_WorkTable station)
    {
        try
        {
            return !station.IsForbidden(pawn) &&
                   pawn.CanReach(station, PathEndMode.InteractionCell, Danger.Some);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool IsOperational(Building_WorkTable station)
    {
        try
        {
            return WorkGiver_PlateMeals.IsOperational(station);
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static bool ShouldDoNow(Bill_Production bill)
    {
        try
        {
            return bill.ShouldDoNow();
        }
        catch (Exception)
        {
            return false;
        }
    }

    private static KitchenwareProduct? Product(Thing thing) =>
        thing.def.GetModExtension<KitchenwareExtension>()?.product;
}
