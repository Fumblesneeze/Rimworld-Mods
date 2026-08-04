using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

[HarmonyPatch(typeof(WorkGiver_DoBill), nameof(WorkGiver_DoBill.JobOnThing))]
internal static class WorkGiverDoBillWarePatch
{
    private static void Postfix(Pawn pawn, Thing thing, ref Job? __result)
    {
        if (__result is null || !MealCoveragePolicy.IsCovered(__result.RecipeDef))
        {
            return;
        }

        if (!CookingSessionRegistry.TryAttach(pawn, __result, thing, out var missingReason))
        {
            JobFailReason.Is($"Missing {missingReason} (Immersive Chefs)");
            __result = null;
        }
    }
}

[HarmonyPatch(typeof(Bill), nameof(Bill.Notify_PawnDidWork))]
internal static class BillCookingWorkPatch
{
    private static readonly AccessTools.FieldRef<JobDriver_DoBill, float> WorkLeft =
        AccessTools.FieldRefAccess<JobDriver_DoBill, float>("workLeft");

    private static void Postfix(Bill __instance, Pawn p)
    {
        var bonus = CookingSessionRegistry.NotifyWorkTick(p);
        if (bonus <= 0f || p.jobs.curDriver is not JobDriver_DoBill driver)
        {
            return;
        }

        var workSpeedStat = __instance.recipe.workSpeedStat;
        var workPerTick = workSpeedStat is null ? 1f : p.GetStatValue(workSpeedStat);
        WorkLeft(driver) = Math.Max(0f, WorkLeft(driver) - (workPerTick * bonus));
    }
}

[HarmonyPatch(typeof(JobDriver_DoBill), "MakeNewToils")]
internal static class BillKitchenwarePickupToilsPatch
{
    private static void Postfix(JobDriver_DoBill __instance, ref IEnumerable<Toil> __result)
    {
        __result = CookingSessionRegistry.AddWarePickupToils(__instance.GetActor(), __result);
    }
}

[HarmonyPatch(typeof(Bill), nameof(Bill.Notify_DoBillStarted))]
internal static class BillCookingSpeedPatch
{
    private static readonly AccessTools.FieldRef<JobDriver_DoBill, float> WorkLeft =
        AccessTools.FieldRefAccess<JobDriver_DoBill, float>("workLeft");

    private static void Postfix(Pawn __0)
    {
        var billDoer = __0;
        if (billDoer.jobs.curDriver is not JobDriver_DoBill driver)
        {
            return;
        }

        var speedFactor = CookingSessionRegistry.CookingSpeedFactor(billDoer);
        WorkLeft(driver) = Math.Max(1f, WorkLeft(driver) / speedFactor);
    }
}

[HarmonyPatch(typeof(GenRecipe), nameof(GenRecipe.MakeRecipeProducts))]
internal static class GenRecipeKitchenwarePatch
{
    private static void Postfix(
        ref IEnumerable<Thing> __result,
        RecipeDef recipeDef,
        Pawn worker,
        List<Thing> ingredients)
    {
        __result = CookingSessionRegistry.ApplyProducts(
            PreparedFoodRuntime.ApplyProducts(__result, recipeDef, worker, ingredients),
            worker,
            ingredients);
    }
}

[HarmonyPatch(typeof(Pawn_JobTracker), "CleanupCurrentJob")]
internal static class CookingJobCleanupPatch
{
    private static void Prefix(Pawn_JobTracker __instance)
    {
        var pawn = Traverse.Create(__instance).Field("pawn").GetValue<Pawn>();
        CookingSessionRegistry.Cleanup(pawn, pawn.CurJob);
        KitchenAssistanceRegistry.Cleanup(pawn, pawn.CurJob);
        DiningSessionRegistry.Cleanup(pawn, pawn.CurJob);
        GastronomyAdapter.Cleanup(pawn, pawn.CurJob);
        CommonSenseAdapter.Cleanup(pawn.CurJob);
    }
}
