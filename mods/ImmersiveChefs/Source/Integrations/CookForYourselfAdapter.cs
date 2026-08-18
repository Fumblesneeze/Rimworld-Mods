using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

internal sealed class CookForYourselfShape
{
    internal CookForYourselfShape(
        string? assemblyName,
        Version? assemblyVersion,
        Guid moduleVersionId,
        string? selfJobGiverTypeName,
        bool selfTryGiveJobIsProtectedInstancePawnJob,
        string? dependentJobGiverTypeName,
        bool dependentTryGiveJobIsProtectedInstancePawnJob,
        string? driverTypeName,
        bool driverIsPublicSealedJobDriver,
        bool makeNewToilsIsProtectedInstanceEnumerableToil,
        bool workLeftIsPrivateInstanceFloat,
        bool recipeIsPrivateInstanceRecipeDef,
        bool recipientIsPrivateInstancePawn,
        bool deliveryModeIsPrivateInstanceInt,
        string? jobDefName,
        bool jobDefUsesDriver)
    {
        AssemblyName = assemblyName;
        AssemblyVersion = assemblyVersion;
        ModuleVersionId = moduleVersionId;
        SelfJobGiverTypeName = selfJobGiverTypeName;
        SelfTryGiveJobIsProtectedInstancePawnJob = selfTryGiveJobIsProtectedInstancePawnJob;
        DependentJobGiverTypeName = dependentJobGiverTypeName;
        DependentTryGiveJobIsProtectedInstancePawnJob = dependentTryGiveJobIsProtectedInstancePawnJob;
        DriverTypeName = driverTypeName;
        DriverIsPublicSealedJobDriver = driverIsPublicSealedJobDriver;
        MakeNewToilsIsProtectedInstanceEnumerableToil = makeNewToilsIsProtectedInstanceEnumerableToil;
        WorkLeftIsPrivateInstanceFloat = workLeftIsPrivateInstanceFloat;
        RecipeIsPrivateInstanceRecipeDef = recipeIsPrivateInstanceRecipeDef;
        RecipientIsPrivateInstancePawn = recipientIsPrivateInstancePawn;
        DeliveryModeIsPrivateInstanceInt = deliveryModeIsPrivateInstanceInt;
        JobDefName = jobDefName;
        JobDefUsesDriver = jobDefUsesDriver;
    }

    internal string? AssemblyName { get; set; }
    internal Version? AssemblyVersion { get; set; }
    internal Guid ModuleVersionId { get; set; }
    internal string? SelfJobGiverTypeName { get; set; }
    internal bool SelfTryGiveJobIsProtectedInstancePawnJob { get; set; }
    internal string? DependentJobGiverTypeName { get; set; }
    internal bool DependentTryGiveJobIsProtectedInstancePawnJob { get; set; }
    internal string? DriverTypeName { get; set; }
    internal bool DriverIsPublicSealedJobDriver { get; set; }
    internal bool MakeNewToilsIsProtectedInstanceEnumerableToil { get; set; }
    internal bool WorkLeftIsPrivateInstanceFloat { get; set; }
    internal bool RecipeIsPrivateInstanceRecipeDef { get; set; }
    internal bool RecipientIsPrivateInstancePawn { get; set; }
    internal bool DeliveryModeIsPrivateInstanceInt { get; set; }
    internal string? JobDefName { get; set; }
    internal bool JobDefUsesDriver { get; set; }
}

internal enum CookForYourselfAdmission
{
    PassThrough,
    Attach
}

internal readonly struct CookForYourselfIngredientEntry
{
    internal CookForYourselfIngredientEntry(
        bool hasThing,
        bool destroyed,
        int requestedCount,
        int stackCount)
    {
        HasThing = hasThing;
        Destroyed = destroyed;
        RequestedCount = requestedCount;
        StackCount = stackCount;
    }

    internal bool HasThing { get; }
    internal bool Destroyed { get; }
    internal int RequestedCount { get; }
    internal int StackCount { get; }
}

internal static class CookForYourselfCompatibility
{
    internal const string AssemblyName = "CookForYourself";
    internal static readonly Version AssemblyVersion = new(1, 0, 0, 0);
    internal static readonly Guid ModuleVersionId =
        Guid.Parse("d559047c-a763-4d2f-8cab-7077d5f4a309");
    internal const string SelfJobGiverTypeName = "CookForYourself.JobGiver_CookMealForSelf";
    internal const string DependentJobGiverTypeName = "CookForYourself.JobGiver_CookMealForDependent";
    internal const string DriverTypeName = "CookForYourself.JobDriver_CookMealForSelf";
    internal const string JobDefName = "CFS_CookMealForSelf";

    internal static bool IsSupported(CookForYourselfShape shape)
    {
        return shape.AssemblyName == AssemblyName &&
               shape.AssemblyVersion == AssemblyVersion &&
               shape.ModuleVersionId == ModuleVersionId &&
               shape.SelfJobGiverTypeName == SelfJobGiverTypeName &&
               shape.SelfTryGiveJobIsProtectedInstancePawnJob &&
               shape.DependentJobGiverTypeName == DependentJobGiverTypeName &&
               shape.DependentTryGiveJobIsProtectedInstancePawnJob &&
               shape.DriverTypeName == DriverTypeName &&
               shape.DriverIsPublicSealedJobDriver &&
               shape.MakeNewToilsIsProtectedInstanceEnumerableToil &&
               shape.WorkLeftIsPrivateInstanceFloat &&
               shape.RecipeIsPrivateInstanceRecipeDef &&
               shape.RecipientIsPrivateInstancePawn &&
               shape.DeliveryModeIsPrivateInstanceInt &&
               shape.JobDefName == JobDefName &&
               shape.JobDefUsesDriver;
    }
}

internal static class CookForYourselfJobContract
{
    internal static bool IsValid(
        bool isExactJobDef,
        bool hasConcreteRecipeTag,
        bool stationIsUsableBillGiver,
        IReadOnlyList<CookForYourselfIngredientEntry> ingredients,
        int ingredientCountQueueCount,
        bool recipientIsAbsentOrPawn)
    {
        return isExactJobDef &&
               hasConcreteRecipeTag &&
               stationIsUsableBillGiver &&
               recipientIsAbsentOrPawn &&
               ingredients.Count > 0 &&
               ingredients.Count == ingredientCountQueueCount &&
               ingredients.All(ingredient =>
                   ingredient.HasThing &&
                   !ingredient.Destroyed &&
                   ingredient.RequestedCount > 0 &&
                   ingredient.RequestedCount <= ingredient.StackCount);
    }

    internal static CookForYourselfAdmission AdmissionFor(
        bool adapterEnabled,
        bool jobContractValid,
        bool recipeExists,
        bool recipeIsCovered)
    {
        return adapterEnabled && jobContractValid && recipeExists && recipeIsCovered
            ? CookForYourselfAdmission.Attach
            : CookForYourselfAdmission.PassThrough;
    }
}

internal readonly struct CookForYourselfToilShape
{
    internal CookForYourselfToilShape(
        string? debugName,
        bool completesNever,
        bool hasTickIntervalAction,
        bool hasInitAction,
        bool hasActiveSkill)
    {
        DebugName = debugName;
        CompletesNever = completesNever;
        HasTickIntervalAction = hasTickIntervalAction;
        HasInitAction = hasInitAction;
        HasActiveSkill = hasActiveSkill;
    }

    internal string? DebugName { get; }
    internal bool CompletesNever { get; }
    internal bool HasTickIntervalAction { get; }
    internal bool HasInitAction { get; }
    internal bool HasActiveSkill { get; }
}

internal static class CookForYourselfToilPolicy
{
    internal const string IngredientPlacementToilDebugName = "PlaceCookIngredient";
    internal const string CookingToilDebugName = "CookMealForSelf";

    internal static bool TrySelectWarePickupIndex(
        IReadOnlyList<CookForYourselfToilShape> toils,
        out int index)
    {
        index = -1;
        if (!TrySelectExactIndex(toils, out var cookingIndex))
        {
            return false;
        }

        var ingredientPlacementIndex = -1;
        for (var candidateIndex = 0; candidateIndex < cookingIndex; candidateIndex++)
        {
            if (toils[candidateIndex].DebugName != IngredientPlacementToilDebugName)
            {
                continue;
            }

            if (ingredientPlacementIndex >= 0)
            {
                return false;
            }

            ingredientPlacementIndex = candidateIndex;
        }

        if (ingredientPlacementIndex < 0 ||
            toils.Skip(cookingIndex + 1)
                .Any(toil => toil.DebugName == IngredientPlacementToilDebugName))
        {
            return false;
        }

        index = cookingIndex;
        return true;
    }

    internal static bool TrySelectExactIndex(
        IReadOnlyList<CookForYourselfToilShape> toils,
        out int index)
    {
        index = -1;
        for (var candidateIndex = 0; candidateIndex < toils.Count; candidateIndex++)
        {
            var candidate = toils[candidateIndex];
            if (candidate.DebugName != CookingToilDebugName ||
                !candidate.CompletesNever ||
                !candidate.HasTickIntervalAction ||
                !candidate.HasInitAction ||
                !candidate.HasActiveSkill)
            {
                continue;
            }

            if (index >= 0)
            {
                index = -1;
                return false;
            }

            index = candidateIndex;
        }

        return index >= 0;
    }
}

internal static class CookForYourselfWorkPolicy
{
    internal static float AdditionalProgress(
        float pawnWorkPerTick,
        float workTableFactor,
        int delta,
        float cookingSpeedFactor,
        float assistantBonus)
    {
        if (pawnWorkPerTick <= 0f || workTableFactor <= 0f || delta <= 0)
        {
            return 0f;
        }

        var toolProgress = pawnWorkPerTick * workTableFactor * delta *
                           (Math.Max(0.1f, cookingSpeedFactor) - 1f);
        var assistantProgress = pawnWorkPerTick * delta * Math.Max(0f, assistantBonus);
        return toolProgress + assistantProgress;
    }
}

internal static class StackGapIngredientDropPolicy
{
    internal static bool ShouldBypass(
        bool adapterEnabled,
        bool jobAdmitted,
        bool directMode,
        bool beforeActiveCooking,
        bool carriedIngredientMatchesCurrentTarget)
    {
        return adapterEnabled &&
               jobAdmitted &&
               directMode &&
               beforeActiveCooking &&
               carriedIngredientMatchesCurrentTarget;
    }
}

internal static class CookForYourselfPatchPolicy
{
    internal const int AdmissionFinalizerPriority = Priority.Last;

    internal static bool ShouldInstall(bool allTargetsValidated, int existingOwnedPatchCount)
    {
        return allTargetsValidated && existingOwnedPatchCount == 0;
    }

    internal static bool ShouldDecorateDriver(bool jobWasAdmitted) => jobWasAdmitted;
}

internal static class CookForYourselfAdapter
{
    private const string StackGapPackageId = "Andromeda.StackGap";
    private const string StackGapAssemblyName = "StackGap";
    private const string StackGapDropPatchTypeName =
        "StorageUpperBound.Pawn_CarryTracker_Patch";
    private const string StackGapDropPatch2TypeName =
        "StorageUpperBound.Pawn_CarryTracker_Patch2";
    private const string StackGapPlacementPatchTypeName =
        "StorageUpperBound.Patch_TryPlaceDirect+TryPlaceDirect_Patch";
    private static readonly Version StackGapAssemblyVersion = new(1, 0, 0, 0);
    private static readonly Guid StackGapModuleVersionId =
        Guid.Parse("e69587cb-b4c8-4b4a-a3f8-fe0910b5ed73");
    private const BindingFlags DeclaredInstance =
        BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;
    private const BindingFlags DeclaredStatic =
        BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

    private static Type? driverType;
    private static JobDef? jobDef;
    private static AccessTools.FieldRef<object, float>? workLeft;
    private static FieldInfo? stackGapEnabledField;
    private static readonly ConditionalWeakTable<Job, AdmittedJob> AdmittedJobs = new();

    internal static bool Enabled { get; private set; }

    internal static bool TryInitialize(Harmony harmony, out string reason)
    {
        if (Enabled)
        {
            reason = string.Empty;
            return true;
        }

        var selfJobGiverType = AccessTools.TypeByName(CookForYourselfCompatibility.SelfJobGiverTypeName);
        var dependentJobGiverType = AccessTools.TypeByName(
            CookForYourselfCompatibility.DependentJobGiverTypeName);
        var foundDriverType = AccessTools.TypeByName(CookForYourselfCompatibility.DriverTypeName);
        var selfTryGiveJob = ExactMethod(
            selfJobGiverType,
            "TryGiveJob",
            typeof(Job),
            typeof(Pawn));
        var dependentTryGiveJob = ExactMethod(
            dependentJobGiverType,
            "TryGiveJob",
            typeof(Job),
            typeof(Pawn));
        var makeNewToils = ExactMethod(
            foundDriverType,
            "MakeNewToils",
            typeof(IEnumerable<Toil>));
        var workLeftField = ExactField(foundDriverType, "workLeft", typeof(float));
        var recipeField = ExactField(foundDriverType, "recipe", typeof(RecipeDef));
        var recipientField = ExactField(foundDriverType, "recipient", typeof(Pawn));
        var deliveryModeField = ExactField(foundDriverType, "deliveryModeValue", typeof(int));
        var foundJobDef = DefDatabase<JobDef>.GetNamedSilentFail(
            CookForYourselfCompatibility.JobDefName);
        var stackGapLoaded =
            ImmersiveChefsMod.Integrations?.ContainsPackage(StackGapPackageId) == true;
        var stackGapDropPatchType = stackGapLoaded
            ? AccessTools.TypeByName(StackGapDropPatchTypeName)
            : null;
        var stackGapDropPatch2Type = stackGapLoaded
            ? AccessTools.TypeByName(StackGapDropPatch2TypeName)
            : null;
        var stackGapPlacementPatchType = stackGapLoaded
            ? AccessTools.TypeByName(StackGapPlacementPatchTypeName)
            : null;
        var stackGapDropPrefix = ExactStaticMethod(
            stackGapDropPatchType,
            "Prefix",
            typeof(void),
            typeof(IntVec3),
            typeof(ThingPlaceMode),
            typeof(Pawn_CarryTracker));
        var stackGapDropPrefix2 = ExactStaticMethod(
            stackGapDropPatch2Type,
            "Prefix",
            typeof(void),
            typeof(IntVec3),
            typeof(ThingPlaceMode),
            typeof(Pawn_CarryTracker));
        var foundStackGapEnabledField = stackGapPlacementPatchType?.GetField(
            "Enabled",
            DeclaredStatic);
        var stackGapAssembly = stackGapDropPatchType?.Assembly;
        var stackGapShapeSupported = !stackGapLoaded ||
                                     stackGapAssembly?.GetName().Name == StackGapAssemblyName &&
                                     stackGapAssembly.GetName().Version == StackGapAssemblyVersion &&
                                     stackGapDropPatchType?.Module.ModuleVersionId == StackGapModuleVersionId &&
                                     stackGapDropPatch2Type?.Assembly == stackGapAssembly &&
                                     stackGapPlacementPatchType?.Assembly == stackGapAssembly &&
                                     stackGapDropPrefix is not null &&
                                     stackGapDropPrefix2 is not null &&
                                     foundStackGapEnabledField is
                                     {
                                         IsStatic: true,
                                         FieldType: not null
                                     } &&
                                     foundStackGapEnabledField.FieldType == typeof(bool);
        var assembly = foundDriverType?.Assembly;
        var shape = new CookForYourselfShape(
            assembly?.GetName().Name,
            assembly?.GetName().Version,
            foundDriverType?.Module.ModuleVersionId ?? Guid.Empty,
            selfJobGiverType?.FullName,
            IsProtectedInstancePawnJob(selfTryGiveJob, selfJobGiverType),
            dependentJobGiverType?.FullName,
            IsProtectedInstancePawnJob(dependentTryGiveJob, dependentJobGiverType),
            foundDriverType?.FullName,
            foundDriverType is { IsPublic: true, IsSealed: true } &&
            typeof(JobDriver).IsAssignableFrom(foundDriverType),
            IsProtectedInstanceToils(makeNewToils, foundDriverType),
            IsPrivateInstanceField(workLeftField, foundDriverType),
            IsPrivateInstanceField(recipeField, foundDriverType),
            IsPrivateInstanceField(recipientField, foundDriverType),
            IsPrivateInstanceField(deliveryModeField, foundDriverType),
            foundJobDef?.defName,
            foundJobDef?.driverClass == foundDriverType);
        var allSameAssembly = assembly is not null &&
                              selfJobGiverType?.Assembly == assembly &&
                              dependentJobGiverType?.Assembly == assembly;
        var targets = new[]
        {
            selfTryGiveJob,
            dependentTryGiveJob,
            makeNewToils,
            stackGapDropPrefix,
            stackGapDropPrefix2
        };
        var existingOwnedPatches = targets
            .Where(method => method is not null)
            .Sum(method => OwnedPatchCount(method!, harmony.Id));
        if (!CookForYourselfCompatibility.IsSupported(shape) ||
            !allSameAssembly ||
            !stackGapShapeSupported ||
            !CookForYourselfPatchPolicy.ShouldInstall(
                targets.Take(3).All(method => method is not null) &&
                (!stackGapLoaded || targets.Skip(3).All(method => method is not null)),
                existingOwnedPatches) ||
            selfTryGiveJob is null || dependentTryGiveJob is null || makeNewToils is null ||
            foundDriverType is null || workLeftField is null || foundJobDef is null)
        {
            reason = existingOwnedPatches > 0
                ? "a partial or duplicate Immersive Chefs patch already owns the Cook for Yourself seam"
                : stackGapLoaded && !stackGapShapeSupported
                    ? "the installed Stack Gap ingredient-placement patch no longer matches the audited 1.6 build"
                : "the installed Cook for Yourself assembly/job-driver shape no longer matches the audited 1.6 build";
            return false;
        }

        AccessTools.FieldRef<object, float> foundWorkLeft;
        try
        {
            foundWorkLeft = AccessTools.FieldRefAccess<float>(foundDriverType, workLeftField.Name);
            harmony.Patch(
                selfTryGiveJob,
                finalizer: AdmissionFinalizer());
            harmony.Patch(
                dependentTryGiveJob,
                finalizer: AdmissionFinalizer());
            harmony.Patch(
                makeNewToils,
                postfix: new HarmonyMethod(typeof(CookForYourselfAdapter), nameof(MakeNewToilsPostfix)));
            if (stackGapLoaded)
            {
                var bypass = new HarmonyMethod(
                    typeof(CookForYourselfAdapter),
                    nameof(StackGapDropPrefixPrefix))
                {
                    priority = Priority.First
                };
                harmony.Patch(stackGapDropPrefix!, prefix: bypass);
                harmony.Patch(stackGapDropPrefix2!, prefix: bypass);
            }
        }
        catch (Exception exception)
        {
            RemovePartialPatches(harmony, targets);
            reason = $"patch installation failed ({exception.GetType().Name}: {exception.Message})";
            return false;
        }

        driverType = foundDriverType;
        jobDef = foundJobDef;
        workLeft = foundWorkLeft;
        stackGapEnabledField = foundStackGapEnabledField;
        Enabled = true;
        reason = string.Empty;
        Log.Message(
            "[ImmersiveChefs] Cook for Yourself adapter active; covered one-off meals use the normal culinary ware lifecycle.");
        return true;
    }

    internal static bool OwnsDriver(JobDriver? driver)
    {
        return driver is not null &&
               driverType?.IsInstanceOfType(driver) == true &&
               driver.job is { } job &&
               CookForYourselfPatchPolicy.ShouldDecorateDriver(AdmittedJobs.TryGetValue(job, out _));
    }

    private static HarmonyMethod AdmissionFinalizer()
    {
        return new HarmonyMethod(typeof(CookForYourselfAdapter), nameof(JobGiverFinalizer))
        {
            priority = CookForYourselfPatchPolicy.AdmissionFinalizerPriority
        };
    }

    internal static void Cleanup(Job? job)
    {
        if (job is not null)
        {
            AdmittedJobs.Remove(job);
        }
    }

    private static MethodInfo? ExactMethod(
        Type? type,
        string name,
        Type returnType,
        params Type[] parameters)
    {
        var method = type?.GetMethod(
            name,
            DeclaredInstance,
            binder: null,
            types: parameters,
            modifiers: null);
        return method?.ReturnType == returnType ? method : null;
    }

    private static MethodInfo? ExactStaticMethod(
        Type? type,
        string name,
        Type returnType,
        params Type[] parameters)
    {
        var method = type?.GetMethod(
            name,
            DeclaredStatic,
            binder: null,
            types: parameters,
            modifiers: null);
        return method is { IsStatic: true } && method.ReturnType == returnType
            ? method
            : null;
    }

    private static FieldInfo? ExactField(Type? type, string name, Type fieldType)
    {
        var field = type?.GetField(name, DeclaredInstance);
        return field?.FieldType == fieldType ? field : null;
    }

    private static bool IsProtectedInstancePawnJob(MethodInfo? method, Type? declaringType)
    {
        return method is { IsStatic: false, IsFamily: true } && method.DeclaringType == declaringType;
    }

    private static bool IsProtectedInstanceToils(MethodInfo? method, Type? declaringType)
    {
        return method is { IsStatic: false, IsFamily: true } && method.DeclaringType == declaringType;
    }

    private static bool IsPrivateInstanceField(FieldInfo? field, Type? declaringType)
    {
        return field is { IsStatic: false, IsPrivate: true } && field.DeclaringType == declaringType;
    }

    private static int OwnedPatchCount(MethodBase method, string owner)
    {
        var info = Harmony.GetPatchInfo(method);
        return info is null
            ? 0
            : info.Prefixes.Concat(info.Postfixes).Concat(info.Transpilers).Concat(info.Finalizers)
                .Count(patch => patch.owner == owner);
    }

    private static void RemovePartialPatches(Harmony harmony, IEnumerable<MethodInfo?> methods)
    {
        foreach (var method in methods.Where(method => method is not null))
        {
            try
            {
                harmony.Unpatch(method!, HarmonyPatchType.All, harmony.Id);
            }
            catch
            {
                // Preserve the installation failure as the authoritative diagnostic.
            }
        }
    }

    private static Exception? JobGiverFinalizer(
        Pawn __0,
        ref Job? __result,
        Exception? __exception)
    {
        if (__exception is not null ||
            !Enabled ||
            !ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.CookForYourself) ||
            __result is not { } finalJob)
        {
            return __exception;
        }

        try
        {
            AdmitFinalJob(__0, finalJob, ref __result);
        }
        catch (Exception exception)
        {
            CookingSessionRegistry.AbortAdmission(__0, finalJob);
            Cleanup(finalJob);
            __result = finalJob;
            Disable($"one-off job admission failed ({RootMessage(exception)})");
        }

        return null;
    }

    private static void AdmitFinalJob(Pawn pawn, Job job, ref Job? result)
    {

        var ingredientTargets = job.GetTargetQueue(TargetIndex.B);
        var counts = job.countQueue;
        var ingredients = ingredientTargets?.Select((target, index) =>
        {
            var thing = target.Thing;
            var requested = counts is not null && index < counts.Count ? counts[index] : int.MinValue;
            return new CookForYourselfIngredientEntry(
                target.HasThing,
                thing?.Destroyed ?? false,
                requested,
                thing?.stackCount ?? 0);
        }).ToArray() ?? Array.Empty<CookForYourselfIngredientEntry>();
        var station = job.GetTarget(TargetIndex.A).Thing;
        var recipient = job.GetTarget(TargetIndex.C);
        var contractValid = CookForYourselfJobContract.IsValid(
            job.def == jobDef,
            !job.controlGroupTag.NullOrEmpty(),
            station is IBillGiver billGiver &&
            billGiver.CurrentlyUsableForBills() &&
            billGiver.UsableForBillsAfterFueling(),
            ingredients,
            counts?.Count ?? 0,
            !recipient.IsValid || recipient.Pawn is not null);
        var recipe = job.controlGroupTag.NullOrEmpty()
            ? null
            : DefDatabase<RecipeDef>.GetNamedSilentFail(job.controlGroupTag);
        var admission = CookForYourselfJobContract.AdmissionFor(
            Enabled,
            contractValid,
            recipe is not null,
            recipe is not null && MealCoveragePolicy.IsCovered(recipe));
        if (!contractValid || recipe is null)
        {
            Disable(
                "a returned one-off job no longer matches the validated recipe/station/ingredient/recipient contract");
            return;
        }

        if (admission != CookForYourselfAdmission.Attach || station is null)
        {
            return;
        }

        var consumer = recipient.Pawn ?? pawn;
        var emergency = consumer.needs?.food?.CurLevelPercentage <=
                        ImmersiveChefsMod.Settings.EmergencyHungerThreshold;
        if (!CookingSessionRegistry.TryAttach(
                pawn,
                job,
                station,
                recipe,
                emergency,
                out _))
        {
            result = null;
            return;
        }

        AdmittedJobs.Add(job, new AdmittedJob());
    }

    private static void MakeNewToilsPostfix(
        JobDriver __instance,
        ref IEnumerable<Toil> __result)
    {
        if (!OwnsDriver(__instance))
        {
            return;
        }

        __result = WrapToils(__instance, __result);
    }

    private static bool StackGapDropPrefixPrefix(
        IntVec3 __0,
        ThingPlaceMode __1,
        Pawn_CarryTracker __2)
    {
        var pawn = __2.pawn;
        var job = pawn.CurJob;
        var carriedThing = __2.CarriedThing;
        var ingredientTarget = job?.GetTarget(TargetIndex.B).Thing;
        var currentIngredientMatches = carriedThing is not null &&
                                       ingredientTarget is not null &&
                                       (ReferenceEquals(carriedThing, ingredientTarget) ||
                                        carriedThing.def == ingredientTarget.def);
        var beforeActiveCooking =
            !CookingSessionRegistry.TryGetActiveWorkProp(pawn, out _, out _);
        if (!StackGapIngredientDropPolicy.ShouldBypass(
                Enabled,
                job is not null && AdmittedJobs.TryGetValue(job, out _),
                __1 == ThingPlaceMode.Direct,
                beforeActiveCooking,
                currentIngredientMatches))
        {
            return true;
        }

        try
        {
            stackGapEnabledField!.SetValue(null, false);
            return false;
        }
        catch (Exception exception)
        {
            Disable($"Stack Gap ingredient-drop bypass failed ({RootMessage(exception)})");
            return true;
        }
    }

    private static IEnumerable<Toil> WrapToils(
        JobDriver driver,
        IEnumerable<Toil> original)
    {
        var pawn = driver.GetActor();
        var toils = original.ToList();
        var shapes = toils.Select(toil => new CookForYourselfToilShape(
            toil.debugName,
            toil.defaultCompleteMode == ToilCompleteMode.Never,
            toil.tickIntervalAction is not null,
            toil.initAction is not null,
            toil.activeSkill is not null)).ToArray();
        if (!CookForYourselfToilPolicy.TrySelectWarePickupIndex(shapes, out var cookingIndex))
        {
            CookingSessionRegistry.Cleanup(pawn, pawn.CurJob);
            Cleanup(pawn.CurJob);
            Disable(
                "the custom driver no longer exposes one validated ingredient-placement toil before its active cooking toil");
            return toils;
        }

        var cookingToil = toils[cookingIndex];
        var upstreamTick = cookingToil.tickIntervalAction!;
        cookingToil.tickIntervalAction = delta =>
        {
            try
            {
                ApplyOwnedWorkProgress(driver, pawn, delta);
            }
            catch (Exception exception)
            {
                CookingSessionRegistry.Cleanup(pawn, pawn.CurJob);
                Cleanup(pawn.CurJob);
                Disable($"custom-driver work decoration failed ({RootMessage(exception)})");
            }

            upstreamTick(delta);
        };
        return CookingSessionRegistry.InsertWarePickupToils(
            pawn,
            toils,
            cookingIndex);
    }

    private static void ApplyOwnedWorkProgress(JobDriver driver, Pawn pawn, int delta)
    {
        var recipe = pawn.CurJob?.controlGroupTag.NullOrEmpty() == false
            ? DefDatabase<RecipeDef>.GetNamedSilentFail(pawn.CurJob.controlGroupTag)
            : null;
        var accessor = workLeft;
        if (recipe is null || accessor is null)
        {
            throw new InvalidOperationException("validated recipe/work field is unavailable");
        }

        var pawnWorkPerTick = recipe.workSpeedStat is null
            ? 1f
            : pawn.GetStatValue(recipe.workSpeedStat);
        var workTableFactor = 1f;
        if (recipe.workTableSpeedStat is not null &&
            pawn.CurJob?.GetTarget(TargetIndex.A).Thing is Building_WorkTable table)
        {
            workTableFactor = table.GetStatValue(recipe.workTableSpeedStat);
        }

        var assistantBonus = CookingSessionRegistry.NotifyWorkTick(pawn);
        var additional = CookForYourselfWorkPolicy.AdditionalProgress(
            pawnWorkPerTick,
            workTableFactor,
            delta,
            CookingSessionRegistry.CookingSpeedFactor(pawn),
            assistantBonus);
        ref var remaining = ref accessor(driver);
        remaining -= additional;
    }

    private static void Disable(string reason)
    {
        Enabled = false;
        OptionalIntegrationDiagnostics.WarnOnce(OptionalIntegration.CookForYourself, reason);
    }

    private static string RootMessage(Exception exception)
    {
        var root = exception is TargetInvocationException { InnerException: { } inner }
            ? inner
            : exception;
        return $"{root.GetType().Name}: {root.Message}";
    }

    private sealed class AdmittedJob
    {
    }
}
