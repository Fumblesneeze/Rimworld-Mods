using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using RimWorld.Planet;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

internal static class CommonSenseCompatibility
{
    internal const string AssemblyName = "CommonSense";
    internal const string SettingsTypeName = "CommonSense.Settings";
    internal const string UtilityTypeName = "CommonSense.Utility";
    internal const string IngestSettingName = "adv_cleaning_ingest";
    internal const string IncapableMethodName = "IncapableOfCleaning";

    internal static bool IsSupported(
        string? assemblyName,
        string? settingsTypeName,
        bool settingsIsPublic,
        bool ingestSettingIsPublicStaticBoolean,
        string? utilityTypeName,
        bool utilityIsPublicStatic,
        bool incapableMethodIsPublicStaticPawnBoolean)
    {
        return assemblyName == AssemblyName &&
               settingsTypeName == SettingsTypeName &&
               settingsIsPublic &&
               ingestSettingIsPublicStaticBoolean &&
               utilityTypeName == UtilityTypeName &&
               utilityIsPublicStatic &&
               incapableMethodIsPublicStaticPawnBoolean;
    }
}

internal static class CommonSenseHandoffPolicy
{
    internal static bool ShouldClaim(
        bool integrationEnabled,
        bool ingestionCompleted,
        bool mapAvailable,
        bool caravanDining,
        bool gastronomyOwned,
        bool pawnCanClean)
    {
        return integrationEnabled && ingestionCompleted && mapAvailable && !caravanDining &&
               !gastronomyOwned && pawnCanClean;
    }
}

internal static class CommonSenseAdapter
{
    private static readonly ConditionalWeakTable<Job, HandoffRequest> Pending = new();
    private static Func<Pawn, bool>? incapableOfCleaning;

    internal static bool Enabled { get; private set; }

    internal static bool TryInitialize(out string reason)
    {
        if (Enabled)
        {
            reason = string.Empty;
            return true;
        }

        var settingsType = AccessTools.TypeByName(CommonSenseCompatibility.SettingsTypeName);
        var utilityType = AccessTools.TypeByName(CommonSenseCompatibility.UtilityTypeName);
        var ingestSetting = settingsType?.GetField(
            CommonSenseCompatibility.IngestSettingName,
            BindingFlags.Public | BindingFlags.Static);
        var incapableMethod = utilityType?.GetMethod(
            CommonSenseCompatibility.IncapableMethodName,
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(Pawn) },
            modifiers: null);
        var assemblyName = settingsType?.Assembly.GetName().Name;
        var supported = CommonSenseCompatibility.IsSupported(
            assemblyName,
            settingsType?.FullName,
            settingsType?.IsPublic == true,
            ingestSetting?.FieldType == typeof(bool) && ingestSetting.IsPublic && ingestSetting.IsStatic,
            utilityType?.FullName,
            utilityType?.IsPublic == true && utilityType.IsAbstract && utilityType.IsSealed,
            incapableMethod?.ReturnType == typeof(bool) &&
            incapableMethod.IsPublic &&
            incapableMethod.IsStatic &&
            incapableMethod.DeclaringType == utilityType &&
            incapableMethod.GetParameters() is [{ ParameterType: var pawnType }] && pawnType == typeof(Pawn));
        if (!supported || utilityType?.Assembly != settingsType?.Assembly || incapableMethod is null)
        {
            reason = "the installed Common Sense public settings/cleaning-capability shape no longer matches the validated 1.6 API";
            return false;
        }

        try
        {
            incapableOfCleaning = (Func<Pawn, bool>)Delegate.CreateDelegate(
                typeof(Func<Pawn, bool>),
                incapableMethod);
            Enabled = true;
            reason = string.Empty;
            Log.Message("[ImmersiveChefs] Common Sense adapter active; completed diners and patient-feeding nurses clean their exact dirty tableware.");
            return true;
        }
        catch (Exception exception)
        {
            reason = $"cleaning-capability binding failed ({exception.GetType().Name}: {exception.Message})";
            return false;
        }
    }

    internal static void ScheduleCommittedHandoff(
        Pawn responsiblePawn,
        Job? diningJob,
        Thing? dirtyPlate,
        Thing? dirtyCutlery,
        bool gastronomyOwned)
    {
        if (diningJob is null || !CanClaim(responsiblePawn, gastronomyOwned))
        {
            return;
        }

        var exactWare = new[] { dirtyPlate, dirtyCutlery }
            .Where(thing => thing is not null && IsSpawnedDirtyWareFor(thing, responsiblePawn))
            .Cast<Thing>()
            .Distinct()
            .ToArray();
        if (exactWare.Length == 0)
        {
            return;
        }

        Pending.Remove(diningJob);
        Pending.Add(diningJob, new HandoffRequest(responsiblePawn, exactWare));
    }

    internal static void TryQueueCommittedHandoff(Pawn responsiblePawn, Job? diningJob)
    {
        if (diningJob is null || !Pending.TryGetValue(diningJob, out var request))
        {
            return;
        }

        Pending.Remove(diningJob);
        if (!ReferenceEquals(request.ResponsiblePawn, responsiblePawn) || !CanClaim(responsiblePawn, gastronomyOwned: false))
        {
            return;
        }

        var jobs = request.ExactWare
            .Where(thing => IsSpawnedDirtyWareFor(thing, responsiblePawn))
            .Select(thing => new WorkGiver_DoDishes().JobOnThing(responsiblePawn, thing))
            .Where(job => job is not null)
            .Cast<Job>()
            .ToList();
        for (var index = jobs.Count - 1; index >= 0; index--)
        {
            responsiblePawn.jobs.jobQueue.EnqueueFirst(jobs[index], tag: JobTag.Misc);
        }
    }

    internal static void Cleanup(Job? diningJob)
    {
        if (diningJob is not null)
        {
            Pending.Remove(diningJob);
        }
    }

    private static bool CanClaim(Pawn pawn, bool gastronomyOwned)
    {
        var integrationEnabled = Enabled &&
                                 ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.CommonSense);
        var canClean = integrationEnabled && !IsIncapableOfCleaning(pawn);
        return CommonSenseHandoffPolicy.ShouldClaim(
            integrationEnabled,
            ingestionCompleted: true,
            mapAvailable: pawn.MapHeld is not null,
            caravanDining: pawn.GetCaravan() is not null,
            gastronomyOwned,
            pawnCanClean: canClean) &&
               !pawn.Downed && !pawn.Drafted && !pawn.InMentalState;
    }

    private static bool IsIncapableOfCleaning(Pawn pawn)
    {
        var predicate = incapableOfCleaning;
        if (predicate is null)
        {
            return true;
        }

        try
        {
            return predicate(pawn);
        }
        catch (Exception exception)
        {
            Enabled = false;
            incapableOfCleaning = null;
            OptionalIntegrationDiagnostics.WarnOnce(
                OptionalIntegration.CommonSense,
                $"cleaning-capability invocation failed ({exception.GetType().Name}: {exception.Message})");
            return true;
        }
    }

    private static bool IsSpawnedDirtyWareFor(Thing thing, Pawn pawn)
    {
        return !thing.Destroyed && thing.Spawned && thing.Map == pawn.MapHeld &&
               (thing as ThingWithComps)?.GetComp<CompSanitation>()?.IsDirty == true &&
               thing.def.GetModExtension<KitchenwareExtension>()?.product is
                   KitchenwareProduct.Plate or KitchenwareProduct.Cutlery;
    }

    private sealed class HandoffRequest
    {
        internal HandoffRequest(Pawn responsiblePawn, IReadOnlyList<Thing> exactWare)
        {
            ResponsiblePawn = responsiblePawn;
            ExactWare = exactWare;
        }

        internal Pawn ResponsiblePawn { get; }
        internal IReadOnlyList<Thing> ExactWare { get; }
    }
}
