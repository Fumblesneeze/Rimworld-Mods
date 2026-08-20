using HarmonyLib;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

[HarmonyPatch(typeof(WorkGiver_DoBill), nameof(WorkGiver_DoBill.JobOnThing))]
internal static class WorkGiverDoBillWarePatch
{
    private static void Postfix(Pawn pawn, Thing thing, ref Job? __result)
    {
        if (__result is null)
        {
            return;
        }

        if (!IntegratedSinkRuntime.CanStartBill(thing))
        {
            JobFailReason.Is("ImmersiveChefs_Dishwasher_PauseNoWater".Translate().CapitalizeFirst());
            __result = null;
            return;
        }

        if (CookingJobRequestScope.SkipWareAttachment)
        {
            return;
        }

        if (
            (!MealCoveragePolicy.IsCovered(__result.RecipeDef) &&
             !AdaptiveMealBillAdapter.Controls(__result.RecipeDef)))
        {
            return;
        }

        if (!CookingSessionRegistry.TryPreflight(
                pawn,
                __result,
                thing,
                out var missingReason,
                CookingJobRequestScope.ForceDirtyCookware))
        {
            if (!CookingJobRequestScope.ForceDirtyCookware &&
                CookingSessionRegistry.TryCreatePrerequisiteWashJob(
                    pawn,
                    __result,
                    out var washJob))
            {
                __result = washJob;
                return;
            }

            JobFailReason.Is("ImmersiveChefs_MissingKitchenware".Translate(missingReason));
            __result = null;
        }
    }
}

internal static class CookingJobRequestScope
{
    [ThreadStatic]
    private static int skipWareAttachment;

    [ThreadStatic]
    private static int forceDirtyCookware;

    internal static bool SkipWareAttachment => skipWareAttachment > 0;
    internal static bool ForceDirtyCookware => forceDirtyCookware > 0;

    internal static IDisposable WithoutWareAttachment()
    {
        skipWareAttachment++;
        return new Scope(() => skipWareAttachment--);
    }

    internal static IDisposable WithDirtyCookwareOverride()
    {
        forceDirtyCookware++;
        return new Scope(() => forceDirtyCookware--);
    }

    private sealed class Scope : IDisposable
    {
        private Action? release;

        internal Scope(Action release)
        {
            this.release = release;
        }

        public void Dispose()
        {
            var action = release;
            release = null;
            action?.Invoke();
        }
    }
}

[HarmonyPatch(typeof(FloatMenuMakerMap), nameof(FloatMenuMakerMap.GetOptions))]
internal static class DirtyCookwareFloatMenuPatch
{
    private static void Postfix(
        List<Pawn> selectedPawns,
        Vector3 clickPos,
        ref List<FloatMenuOption> __result)
    {
        if (selectedPawns.Count != 1 || selectedPawns[0] is not { Map: { } map } pawn)
        {
            return;
        }

        var cell = IntVec3.FromVector3(clickPos);
        foreach (var billGiver in cell.GetThingList(map).Where(thing => thing is IBillGiver))
        {
            if (!TryFindWorkerAndRunnableJob(pawn, billGiver, out var worker, out var probeJob) ||
                !CookingSessionRegistry.CanOfferDirtyCookwareOverride(pawn, probeJob))
            {
                continue;
            }

            __result.Add(new FloatMenuOption(
                "ImmersiveChefs_ForceCookDirtyCookware".Translate(),
                () => StartForcedJob(pawn, billGiver, worker)));
        }
    }

    private static bool TryFindWorkerAndRunnableJob(
        Pawn pawn,
        Thing billGiver,
        out WorkGiver_DoBill worker,
        out Job job)
    {
        var cookingWorkType = DefDatabase<WorkTypeDef>.GetNamedSilentFail("Cooking");
        if (cookingWorkType is null)
        {
            worker = null!;
            job = null!;
            return false;
        }

        foreach (var def in DefDatabase<WorkGiverDef>.AllDefsListForReading
                     .Where(def => def.workType == cookingWorkType))
        {
            if (def.Worker is not WorkGiver_DoBill candidate)
            {
                continue;
            }

            Job? candidateJob;
            using (CookingJobRequestScope.WithoutWareAttachment())
            {
                candidateJob = candidate.JobOnThing(pawn, billGiver, forced: true);
            }

            if (candidateJob is null ||
                (!MealCoveragePolicy.IsCovered(candidateJob.RecipeDef) &&
                 !AdaptiveMealBillAdapter.Controls(candidateJob.RecipeDef)))
            {
                continue;
            }

            worker = candidate;
            job = candidateJob;
            return true;
        }

        worker = null!;
        job = null!;
        return false;
    }

    private static void StartForcedJob(Pawn pawn, Thing billGiver, WorkGiver_DoBill worker)
    {
        Job? job;
        using (CookingJobRequestScope.WithDirtyCookwareOverride())
        {
            job = worker.JobOnThing(pawn, billGiver, forced: true);
        }

        if (job is null)
        {
            Messages.Message(
                "ImmersiveChefs_ForceCookDirtyCookwareUnavailable".Translate(),
                billGiver,
                MessageTypeDefOf.RejectInput,
                historical: false);
            return;
        }

        pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
    }
}

[HarmonyPatch(typeof(Pawn), "DrawAt")]
internal static class CookingWorkPropDrawPatch
{
    private static void Postfix(Pawn __instance, Vector3 drawLoc)
    {
        if (!__instance.Spawned ||
            !CookingSessionRegistry.TryGetActiveWorkProp(
                __instance,
                out var cookware,
                out var billGiver) ||
            cookware is null ||
            billGiver is null)
        {
            return;
        }

        var forward = billGiver.DrawPos - __instance.DrawPos;
        forward.y = 0f;
        var lengthSquared = (forward.x * forward.x) + (forward.z * forward.z);
        if (lengthSquared < 0.0001f)
        {
            var facing = __instance.Rotation.FacingCell;
            forward = new Vector3(facing.x, 0f, facing.z);
            lengthSquared = Math.Max(0.0001f,
                (forward.x * forward.x) + (forward.z * forward.z));
        }

        forward /= (float)Math.Sqrt(lengthSquared);
        var propPosition = drawLoc + (forward * 0.42f);
        propPosition.y = drawLoc.y + CookingWorkPropPolicy.AltitudeOffsetFor(forward.z);
        try
        {
            cookware.Graphic.Draw(propPosition, Rot4.South, cookware, 0f);
        }
        catch (Exception exception)
        {
            Log.ErrorOnce(
                $"[Immersive Chefs] Failed to draw active cookware {cookware.ThingID}: {exception}",
                Gen.HashCombineInt(0x49C3E7, cookware.thingIDNumber));
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
        var pawn = __instance.GetActor();
        var job = __instance.job;
        var requiresWare = MealCoveragePolicy.IsCovered(job.RecipeDef) ||
                           AdaptiveMealBillAdapter.Controls(job.RecipeDef);
        var driverJobIsCurrent = ReferenceEquals(job, pawn.CurJob);
        var billGiver = job.GetTarget(TargetIndex.A).Thing;
        string? missingReason = null;
        if (requiresWare &&
            (!CookingAdmissionLifecyclePolicy.MayReserveWare(
                 CookingAdmissionPhase.AcceptedDriverStart,
                 driverJobIsCurrent) ||
             billGiver is null ||
             !CookingSessionRegistry.TryAttachAtDriverStart(
                 pawn,
                 job,
                 billGiver,
                 out missingReason)))
        {
            __result = CookingAdmissionFailureToils.Create(pawn, missingReason);
            return;
        }

        __result = IntegratedSinkRuntime.AddPreparationWaterUse(
            __instance,
            CookingSessionRegistry.AddWarePickupToils(pawn, __result));
    }
}

internal static class CookingAdmissionFailureToils
{
    internal static IEnumerable<Toil> Create(Pawn pawn, string? missingReason)
    {
        yield return Toils_General.DoAtomic(() =>
        {
            if (!missingReason.NullOrEmpty())
            {
                Messages.Message(
                    "ImmersiveChefs_MissingKitchenware".Translate(missingReason),
                    pawn,
                    MessageTypeDefOf.RejectInput,
                    historical: false);
            }

            pawn.jobs.EndCurrentJob(JobCondition.Incompletable);
        });
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
            recipeDef,
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
        CookForYourselfAdapter.Cleanup(pawn.CurJob);
        KitchenAssistanceRegistry.Cleanup(pawn, pawn.CurJob);
        DiningSessionRegistry.Cleanup(pawn, pawn.CurJob);
        GastronomyAdapter.Cleanup(pawn, pawn.CurJob);
        CommonSenseAdapter.Cleanup(pawn.CurJob);
    }
}

[HarmonyPatch(typeof(Pawn_JobTracker), nameof(Pawn_JobTracker.StartJob))]
internal static class CookForYourselfPostCookingCleanupTransitionPatch
{
    private static void Prefix(
        Pawn ___pawn,
        ref Job newJob,
        JobCondition lastJobEndCondition,
        JobTag? tag)
    {
        var cookForYourselfDriver = CookForYourselfAdapter.OwnsDriver(___pawn.jobs.curDriver);
        var currentJobSucceeded = lastJobEndCondition == JobCondition.Succeeded;
        Job? cleanupJob = null;
        var cleanupCreated = cookForYourselfDriver && currentJobSucceeded &&
                             CookingSessionRegistry.TryPrepareImmediatePostCookingCleanup(
                                 ___pawn,
                                 ___pawn.CurJob,
                                 out cleanupJob);
        if (!CommonSenseCookingCleanupPolicy.ShouldReplaceImmediateFollowup(
                cookForYourselfDriver,
                currentJobSucceeded,
                cleanupCreated))
        {
            return;
        }

        ___pawn.jobs.jobQueue.EnqueueFirst(newJob, tag);
        newJob = cleanupJob!;
    }
}
