using HarmonyLib;
using RimWorld;
using Verse;

namespace ImmersiveChefs;

[HarmonyPatch(
    typeof(PawnInventoryGenerator),
    nameof(PawnInventoryGenerator.GenerateInventoryFor),
    typeof(Pawn),
    typeof(PawnGenerationRequest))]
internal static class GeneratedPawnInventoryMealPlatingPatch
{
    private static void Postfix(Pawn __0)
    {
        if (__0.inventory is null ||
            !GeneratedMealPlatePolicy.AllowsExternalPawnInventory(
                __0.RaceProps.Humanlike,
                __0.Faction is not null,
                __0.Faction == Faction.OfPlayerSilentFail))
        {
            return;
        }

        GeneratedMealPlatingRuntime.EnsurePlated(
            __0.inventory.innerContainer.InnerListForReading,
            GeneratedMealOrigin.ExternalPawnInventory);
    }
}

[HarmonyPatch(
    typeof(ThingSetMaker_TraderStock),
    "Generate",
    typeof(ThingSetMakerParams),
    typeof(List<Thing>))]
internal static class GeneratedTraderStockMealPlatingPatch
{
    private static void Postfix(List<Thing> __1)
    {
        GeneratedMealPlatingRuntime.EnsurePlated(
            __1,
            GeneratedMealOrigin.TradeStock);
    }
}
