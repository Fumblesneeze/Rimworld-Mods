using System.Reflection;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

internal static class PickUpAndHaulCompatibility
{
    internal const string AssemblyName = "PickUpAndHaul";
    internal static readonly Version AssemblyVersion = new(1, 0, 0, 0);
    internal const string CompTypeName = "PickUpAndHaul.CompHauledToInventory";
    internal const string CheckerTypeName = "PickUpAndHaul.PawnUnloadChecker";
    internal const string SettingsTypeName = "PickUpAndHaul.Settings";
    internal const string UnloadDriverTypeName = "PickUpAndHaul.JobDriver_UnloadYourHauledInventory";

    internal static bool IsSupported(
        string? assemblyName,
        Version? assemblyVersion,
        string? compTypeName,
        bool compIsPublicThingComp,
        bool registerIsPublicInstanceThingVoid,
        bool getHashSetIsPublicInstanceThingHashSet,
        string? checkerTypeName,
        bool checkerIsPublic,
        bool checkIsPublicStaticPawnBoolVoid,
        string? settingsTypeName,
        bool settingsIsPublicModSettings,
        bool racePolicyIsPublicStaticRacePropertiesBoolean,
        string? unloadDriverTypeName,
        bool unloadDriverIsPublicJobDriver)
    {
        return assemblyName == AssemblyName &&
               assemblyVersion == AssemblyVersion &&
               compTypeName == CompTypeName &&
               compIsPublicThingComp &&
               registerIsPublicInstanceThingVoid &&
               getHashSetIsPublicInstanceThingHashSet &&
               checkerTypeName == CheckerTypeName &&
               checkerIsPublic &&
               checkIsPublicStaticPawnBoolVoid &&
               settingsTypeName == SettingsTypeName &&
               settingsIsPublicModSettings &&
               racePolicyIsPublicStaticRacePropertiesBoolean &&
               unloadDriverTypeName == UnloadDriverTypeName &&
               unloadDriverIsPublicJobDriver;
    }
}

internal readonly struct DishwashingBatchCandidate
{
    internal DishwashingBatchCandidate(
        string id,
        int distanceSquared,
        int count,
        float unitMass,
        bool sameSource,
        bool eligible,
        float unitPlateEquivalent = 1f)
    {
        Id = id;
        DistanceSquared = distanceSquared;
        Count = count;
        UnitMass = unitMass;
        SameSource = sameSource;
        Eligible = eligible;
        UnitPlateEquivalent = unitPlateEquivalent;
    }

    internal string Id { get; }
    internal int DistanceSquared { get; }
    internal int Count { get; }
    internal float UnitMass { get; }
    internal bool SameSource { get; }
    internal bool Eligible { get; }
    internal float UnitPlateEquivalent { get; }
}

internal readonly struct DishwashingBatchSelection : IEquatable<DishwashingBatchSelection>
{
    internal DishwashingBatchSelection(string id, int count)
    {
        Id = id;
        Count = count;
    }

    internal string Id { get; }
    internal int Count { get; }

    public bool Equals(DishwashingBatchSelection other) => Id == other.Id && Count == other.Count;
    public override bool Equals(object? obj) => obj is DishwashingBatchSelection other && Equals(other);
    public override int GetHashCode() => (Id.GetHashCode() * 397) ^ Count;
}

internal static class DishwashingBatchPolicy
{
    internal const int SearchRadius = 12;
    internal const int DishwasherAdmissionTicksPerUnit = 2;
    private const int SearchRadiusSquared = SearchRadius * SearchRadius;

    internal static bool IsDishwasherAdmissionReady(int elapsedTicks) =>
        elapsedTicks >= DishwasherAdmissionTicksPerUnit;

    internal static IReadOnlyList<DishwashingBatchSelection> Select(
        IEnumerable<DishwashingBatchCandidate> candidates,
        float availableMass,
        float availablePlateEquivalentCapacity = float.MaxValue)
    {
        var candidateList = candidates.ToList();
        var remainingMass = Math.Max(0f, availableMass);
        var remainingCapacity = Math.Max(0f, availablePlateEquivalentCapacity);
        if (candidateList.Count == 0 ||
            !candidateList[0].Eligible ||
            !candidateList[0].SameSource ||
            candidateList[0].DistanceSquared > SearchRadiusSquared ||
            candidateList[0].Count <= 0 ||
            candidateList[0].UnitMass <= 0f ||
            candidateList[0].UnitPlateEquivalent <= 0f ||
            remainingMass < candidateList[0].UnitMass ||
            remainingCapacity + 0.0001f < candidateList[0].UnitPlateEquivalent)
        {
            return Array.Empty<DishwashingBatchSelection>();
        }

        var selected = new List<DishwashingBatchSelection>();
        foreach (var candidate in candidateList
                     .Where(candidate => candidate.Eligible && candidate.SameSource)
                     .Where(candidate => candidate.DistanceSquared <= SearchRadiusSquared)
                     .OrderBy(candidate => candidate.DistanceSquared))
        {
            if (candidate.Count <= 0 || candidate.UnitMass <= 0f ||
                candidate.UnitPlateEquivalent <= 0f || remainingMass < candidate.UnitMass ||
                remainingCapacity + 0.0001f < candidate.UnitPlateEquivalent)
            {
                continue;
            }

            var massCount = (int)Math.Floor(remainingMass / candidate.UnitMass);
            var capacityCount = remainingCapacity == float.MaxValue
                ? candidate.Count
                : (int)Math.Floor((remainingCapacity + 0.0001f) / candidate.UnitPlateEquivalent);
            var count = Math.Min(candidate.Count, Math.Min(massCount, capacityCount));
            if (count <= 0)
            {
                continue;
            }

            selected.Add(new DishwashingBatchSelection(candidate.Id, count));
            remainingMass -= count * candidate.UnitMass;
            if (remainingCapacity != float.MaxValue)
            {
                remainingCapacity -= count * candidate.UnitPlateEquivalent;
            }
        }

        return selected;
    }

    internal static bool IsPersistedBatch(bool hasQueuedTargets, int collectedCount)
    {
        return hasQueuedTargets || collectedCount > 0;
    }
}

internal readonly struct DishwasherOutputBatchCandidate
{
    internal DishwasherOutputBatchCandidate(
        string id,
        int count,
        float unitMass,
        float progress,
        bool ruined)
    {
        Id = id;
        Count = count;
        UnitMass = unitMass;
        Progress = progress;
        Ruined = ruined;
    }

    internal string Id { get; }
    internal int Count { get; }
    internal float UnitMass { get; }
    internal float Progress { get; }
    internal bool Ruined { get; }
}

internal readonly struct DishwasherOutputBatchSelection : IEquatable<DishwasherOutputBatchSelection>
{
    internal DishwasherOutputBatchSelection(string id, int count)
    {
        Id = id;
        Count = count;
    }

    internal string Id { get; }
    internal int Count { get; }

    public bool Equals(DishwasherOutputBatchSelection other) => Id == other.Id && Count == other.Count;
    public override bool Equals(object? obj) => obj is DishwasherOutputBatchSelection other && Equals(other);
    public override int GetHashCode() => (Id.GetHashCode() * 397) ^ Count;
}

internal static class DishwasherOutputBatchPolicy
{
    internal static bool ShouldReplaceStockEmptying(
        bool processorDishwasher,
        bool canTrack,
        bool hasFittingNaturalOutput) =>
        processorDishwasher && canTrack && hasFittingNaturalOutput;

    internal static bool ShouldUseStockFallbackAtAppliance(bool hasFittingNaturalOutput) =>
        !hasFittingNaturalOutput;

    internal static IReadOnlyList<DishwasherOutputBatchSelection> Select(
        IEnumerable<DishwasherOutputBatchCandidate> candidates,
        float availableMass)
    {
        var remainingMass = Math.Max(0f, availableMass);
        var selected = new List<DishwasherOutputBatchSelection>();
        foreach (var candidate in candidates
                     .Where(candidate => candidate.Progress >= 1f &&
                                         !candidate.Ruined &&
                                         candidate.Count > 0 &&
                                         candidate.UnitMass > 0f)
                     .OrderBy(candidate => candidate.UnitMass))
        {
            if (remainingMass + 0.0001f < candidate.UnitMass)
            {
                continue;
            }

            var count = Math.Min(
                candidate.Count,
                (int)Math.Floor((remainingMass + 0.0001f) / candidate.UnitMass));
            if (count <= 0)
            {
                continue;
            }

            selected.Add(new DishwasherOutputBatchSelection(candidate.Id, count));
            remainingMass = Math.Max(0f, remainingMass - count * candidate.UnitMass);
        }

        return selected;
    }
}

internal static class PickUpAndHaulAdapter
{
    private static Type? compType;
    private static MethodInfo? registerMethod;
    private static MethodInfo? trackedThingsMethod;
    private static MethodInfo? queueUnloadMethod;
    private static Func<RaceProperties, bool>? raceAllowed;
    private static JobDef? unloadJobDef;

    internal static bool Enabled { get; private set; }

    internal static bool TryInitialize(out string reason)
    {
        if (Enabled)
        {
            reason = string.Empty;
            return true;
        }

        var foundCompType = AccessTools.TypeByName(PickUpAndHaulCompatibility.CompTypeName);
        var checkerType = AccessTools.TypeByName(PickUpAndHaulCompatibility.CheckerTypeName);
        var settingsType = AccessTools.TypeByName(PickUpAndHaulCompatibility.SettingsTypeName);
        var unloadDriverType = AccessTools.TypeByName(PickUpAndHaulCompatibility.UnloadDriverTypeName);
        var register = foundCompType?.GetMethod(
            "RegisterHauledItem",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: new[] { typeof(Thing) },
            modifiers: null);
        var tracked = foundCompType?.GetMethod(
            "GetHashSet",
            BindingFlags.Public | BindingFlags.Instance,
            binder: null,
            types: Type.EmptyTypes,
            modifiers: null);
        var queueUnload = checkerType?.GetMethod(
            "CheckIfPawnShouldUnloadInventory",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(Pawn), typeof(bool) },
            modifiers: null);
        var racePolicy = settingsType?.GetMethod(
            "IsAllowedRace",
            BindingFlags.Public | BindingFlags.Static,
            binder: null,
            types: new[] { typeof(RaceProperties) },
            modifiers: null);
        var assembly = foundCompType?.Assembly;
        var unloadJob = DefDatabase<JobDef>.GetNamedSilentFail("UnloadYourHauledInventory");
        var supported = PickUpAndHaulCompatibility.IsSupported(
            assembly?.GetName().Name,
            assembly?.GetName().Version,
            foundCompType?.FullName,
            foundCompType is { IsPublic: true } && typeof(ThingComp).IsAssignableFrom(foundCompType),
            register is { IsPublic: true, IsStatic: false } &&
            register.ReturnType == typeof(void) && register.DeclaringType == foundCompType,
            tracked is { IsPublic: true, IsStatic: false } &&
            tracked.ReturnType == typeof(HashSet<Thing>) && tracked.DeclaringType == foundCompType,
            checkerType?.FullName,
            checkerType?.IsPublic == true,
            queueUnload is { IsPublic: true, IsStatic: true } &&
            queueUnload.ReturnType == typeof(void) && queueUnload.DeclaringType == checkerType,
            settingsType?.FullName,
            settingsType is { IsPublic: true } && typeof(ModSettings).IsAssignableFrom(settingsType),
            racePolicy is { IsPublic: true, IsStatic: true } &&
            racePolicy.ReturnType == typeof(bool) && racePolicy.DeclaringType == settingsType,
            unloadDriverType?.FullName,
            unloadDriverType is { IsPublic: true } &&
            typeof(JobDriver).IsAssignableFrom(unloadDriverType) &&
            unloadJob?.driverClass == unloadDriverType);
        if (!supported || assembly != checkerType?.Assembly || assembly != settingsType?.Assembly ||
            assembly != unloadDriverType?.Assembly || foundCompType is null || register is null ||
            tracked is null || queueUnload is null || racePolicy is null)
        {
            reason = "the installed Pick Up And Haul public tracked-inventory/unload shape no longer matches the validated 1.6 API";
            return false;
        }

        Func<RaceProperties, bool> boundRacePolicy;
        try
        {
            boundRacePolicy = (Func<RaceProperties, bool>)Delegate.CreateDelegate(
                typeof(Func<RaceProperties, bool>),
                racePolicy);
        }
        catch (Exception exception)
        {
            reason = $"race-policy binding failed ({exception.GetType().Name}: {exception.Message})";
            return false;
        }

        compType = foundCompType;
        registerMethod = register;
        trackedThingsMethod = tracked;
        queueUnloadMethod = queueUnload;
        raceAllowed = boundRacePolicy;
        unloadJobDef = unloadJob;
        Enabled = true;
        reason = string.Empty;
        Log.Message("[ImmersiveChefs] Pick Up And Haul adapter active; nearby dishwashing batches use its tracked inventory and native unload job.");
        return true;
    }

    internal static bool CanTrack(Pawn pawn)
    {
        if (!Enabled ||
            !ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.PickUpAndHaul) ||
            pawn.Faction != Faction.OfPlayerSilentFail || FindTracker(pawn) is null ||
            pawn.inventory is null || raceAllowed is null)
        {
            return false;
        }

        try
        {
            return raceAllowed(pawn.RaceProps);
        }
        catch (Exception exception)
        {
            InvocationFailure("race eligibility", exception);
            return false;
        }
    }

    internal static bool TryRegister(Pawn pawn, Thing thing, out string reason)
    {
        var tracker = FindTracker(pawn);
        if (!Enabled ||
            !ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.PickUpAndHaul) ||
            tracker is null || registerMethod is null)
        {
            reason = "the pawn has no validated Pick Up And Haul tracked-inventory component";
            return false;
        }

        try
        {
            registerMethod.Invoke(tracker, new object[] { thing });
            if (trackedThingsMethod?.Invoke(tracker, null) is not ICollection<Thing> tracked ||
                !tracked.Contains(thing))
            {
                reason = "Pick Up And Haul did not confirm its tracked ownership";
                return false;
            }

            reason = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            reason = InvocationFailure("tracked-item registration", exception);
            return false;
        }
    }

    internal static bool TryResolveTrackedItems(
        Pawn pawn,
        Thing thing,
        out ICollection<Thing>? tracked,
        out string reason)
    {
        tracked = null;
        var tracker = FindTracker(pawn);
        if (tracker is null || trackedThingsMethod is null)
        {
            reason = "the pawn's validated Pick Up And Haul tracker is no longer available";
            return false;
        }

        try
        {
            tracked = trackedThingsMethod.Invoke(tracker, null) as ICollection<Thing>;
            if (tracked is null || !tracked.Contains(thing))
            {
                reason = "the kitchenware is no longer owned by Pick Up And Haul tracking";
                tracked = null;
                return false;
            }

            reason = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            reason = InvocationFailure("tracked-item ownership lookup", exception);
            Disable(reason);
            tracked = null;
            return false;
        }
    }

    internal static bool TryRemoveResolvedTrackedItem(
        ICollection<Thing> tracked,
        Thing thing,
        out string reason)
    {
        if (!tracked.Remove(thing) || tracked.Contains(thing))
        {
            reason = "Pick Up And Haul did not release its tracked ownership";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    internal static bool TryRemoveTracked(Pawn pawn, Thing thing, out string reason)
    {
        return TryResolveTrackedItems(pawn, thing, out var tracked, out reason) &&
               TryRemoveResolvedTrackedItem(tracked!, thing, out reason);
    }

    internal static bool TryQueueUnload(Pawn pawn, out string reason)
    {
        if (!Enabled ||
            !ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.PickUpAndHaul) ||
            queueUnloadMethod is null || unloadJobDef is null)
        {
            reason = "the validated Pick Up And Haul unload entry point is unavailable";
            return false;
        }

        try
        {
            queueUnloadMethod.Invoke(null, new object[] { pawn, true });
            if (!pawn.jobs.jobQueue.Any(queued => queued.job.def == unloadJobDef))
            {
                reason = "Pick Up And Haul declined to queue its native unload job for this pawn";
                return false;
            }

            reason = string.Empty;
            return true;
        }
        catch (Exception exception)
        {
            reason = InvocationFailure("native unload queueing", exception);
            return false;
        }
    }

    private static ThingComp? FindTracker(Pawn pawn)
    {
        var expectedType = compType;
        return expectedType is null
            ? null
            : pawn.AllComps.FirstOrDefault(expectedType.IsInstanceOfType);
    }

    private static string InvocationFailure(string operation, Exception exception)
    {
        var root = exception is TargetInvocationException { InnerException: { } inner }
            ? inner
            : exception;
        var reason = $"{operation} failed ({root.GetType().Name}: {root.Message})";
        Disable(reason);
        return reason;
    }

    private static void Disable(string reason)
    {
        Enabled = false;
        OptionalIntegrationDiagnostics.WarnOnce(OptionalIntegration.PickUpAndHaul, reason);
    }
}
