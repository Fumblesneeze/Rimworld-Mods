using HarmonyLib;
using RimWorld;
using Verse;

namespace ImmersiveChefs;

[HarmonyPatch(typeof(CollectionsMassCalculator), nameof(CollectionsMassCalculator.MassUsage),
    typeof(List<ThingCount>), typeof(IgnorePawnsInventoryMode), typeof(bool), typeof(bool))]
internal static class TablewareSelectedMassPatch
{
    private static void Postfix(List<ThingCount> thingCounts, ref float __result)
    {
        foreach (var entry in thingCounts)
            if (entry.Count > 0 && CompTablewareStack.For(entry.Thing) is { } pile)
                __result += pile.SumStat(StatDefOf.Mass, entry.Count) -
                            entry.Count * entry.Thing.GetStatValue(StatDefOf.Mass);
    }

    internal static float RemainderCorrection(Thing thing, int count)
    {
        if (count <= 0 || count >= thing.stackCount || CompTablewareStack.For(thing) is not { } pile) return 0f;
        return pile.SumStat(StatDefOf.Mass, thing.stackCount) -
               pile.SumStat(StatDefOf.Mass, thing.stackCount - count) - pile.SumStat(StatDefOf.Mass, count);
    }
}

[HarmonyPatch(typeof(CollectionsMassCalculator), nameof(CollectionsMassCalculator.MassUsageLeftAfterTransfer))]
internal static class TablewareRemainderMassPatch
{
    private static void Postfix(List<TransferableOneWay> transferables, ref float __result)
    {
        foreach (var transfer in transferables)
        {
            var remaining = transfer.MaxCount - transfer.CountToTransfer;
            for (var i = transfer.things.Count - 1; i >= 0 && remaining > 0; i--)
            {
                var thing = transfer.things[i];
                var count = Math.Min(remaining, thing.stackCount);
                __result += TablewareSelectedMassPatch.RemainderCorrection(thing, count);
                remaining -= count;
            }
        }
    }
}

[HarmonyPatch(typeof(CollectionsMassCalculator), nameof(CollectionsMassCalculator.MassUsageLeftAfterTradeableTransfer))]
internal static class TablewareTradeRemainderMassPatch
{
    private static void Postfix(List<Thing> allCurrentThings, List<Tradeable> tradeables, ref float __result)
    {
        var counts = new List<ThingCount>();
        TransferableUtility.SimulateTradeableTransfer(allCurrentThings, tradeables, counts);
        foreach (var entry in counts)
            if (allCurrentThings.Contains(entry.Thing))
                __result += TablewareSelectedMassPatch.RemainderCorrection(entry.Thing, entry.Count);
    }
}

[HarmonyPatch(typeof(StatWorker), nameof(StatWorker.GetValue), typeof(StatRequest), typeof(bool))]
internal static class TablewareAggregateStatPatch
{
    private static bool Prefix(StatRequest req, bool applyPostProcess, StatDef ___stat, ref float __result)
    {
        var exposure = ___stat == StatDefOf.Flammability || ___stat == StatDefOf.DeteriorationRate;
        if ((!exposure && ___stat != StatDefOf.Mass && ___stat != StatDefOf.MarketValue) ||
            CompTablewareStack.For(req.Thing) is not { } pile || req.Thing.stackCount <= 0) return true;
        __result = exposure
            ? Enumerable.Range(0, req.Thing.stackCount).Max(index =>
                ___stat.Worker.GetValue(StatRequest.For(pile.UnitView(index)), applyPostProcess))
            : pile.SumStat(___stat, req.Thing.stackCount, applyPostProcess) / req.Thing.stackCount;
        return false;
    }
}

[HarmonyPatch(typeof(MassUtility), nameof(MassUtility.WillBeOverEncumberedAfterPickingUp))]
internal static class TablewarePartialMassPatch
{
    private static bool Prefix(Pawn pawn, Thing thing, int count, ref bool __result)
    {
        if (CompTablewareStack.For(thing) is not { } pile) return true;
        __result = MassUtility.FreeSpace(pawn) < pile.SumStat(StatDefOf.Mass, Math.Max(0, count));
        return false;
    }
}

[HarmonyPatch(typeof(MassUtility), nameof(MassUtility.CountToPickUpUntilOverEncumbered))]
internal static class TablewareCarryCapacityPatch
{
    private static bool Prefix(Pawn pawn, Thing thing, ref int __result)
    {
        if (CompTablewareStack.For(thing) is not { } pile) return true;
        var remaining = MassUtility.FreeSpace(pawn);
        __result = 0;
        for (var i = 0; i < thing.stackCount; i++)
        {
            var mass = StatDefOf.Mass.Worker.GetValue(StatRequest.For(pile.UnitView(i)));
            if (mass > remaining) break;
            remaining -= mass;
            __result++;
        }
        return false;
    }
}

[HarmonyPatch(typeof(Tradeable), nameof(Tradeable.GetPriceFor))]
internal static class TablewareTradePricePatch
{
    private static bool Prefix(Tradeable __instance, TradeAction action, ref float __result)
    {
        if (!TradeSession.Active || CompTablewareStack.For(__instance.AnyThing) is null) return true;
        var things = action == TradeAction.PlayerBuys ? __instance.thingsTrader : __instance.thingsColony;
        var remaining = Math.Max(1, Math.Abs(__instance.CountToTransfer));
        var total = 0f;
        var priced = 0;
        // ResolveTrade uses this same list order, then each pile's SplitOff order.
        foreach (var thing in things)
        {
            var count = Math.Min(remaining, thing.stackCount);
            var pile = CompTablewareStack.For(thing);
            for (var i = 0; i < count; i++)
            {
                var unit = pile?.UnitView(i) ?? thing;
                var quote = new Tradeable();
                quote.AddThing(unit, action == TradeAction.PlayerBuys ? Transactor.Trader : Transactor.Colony);
                // Unit views cannot re-enter this patch. Core owns all trader,
                // negotiator, currency, rounding and buy/sell cap rules.
                total += quote.GetPriceFor(action);
            }
            priced += count;
            remaining -= count;
            if (remaining == 0) break;
        }
        if (priced == 0) return true;
        __result = total / priced;
        return false;
    }
}
