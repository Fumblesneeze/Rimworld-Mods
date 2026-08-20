using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;
using Verse.AI;

namespace ImmersiveChefs;

internal enum MealHeatingSourceKind
{
    Microwave,
    Stove,
    Campfire,
    AmbientHeater
}

internal readonly struct MealHeatingSourceFacts
{
    internal MealHeatingSourceFacts(
        bool building,
        bool microwave,
        bool mealSource,
        bool openFlame,
        bool positiveHeatEmitter)
    {
        Building = building;
        Microwave = microwave;
        MealSource = mealSource;
        OpenFlame = openFlame;
        PositiveHeatEmitter = positiveHeatEmitter;
    }

    internal bool Building { get; }
    internal bool Microwave { get; }
    internal bool MealSource { get; }
    internal bool OpenFlame { get; }
    internal bool PositiveHeatEmitter { get; }
}

internal readonly struct MealHeatingProfile
{
    internal MealHeatingProfile(
        int heatingTicks,
        float targetTemperatureCelsius,
        int qualityLossPenalty,
        bool incrementsMicrowaveCount)
    {
        HeatingTicks = heatingTicks;
        TargetTemperatureCelsius = targetTemperatureCelsius;
        QualityLossPenalty = qualityLossPenalty;
        IncrementsMicrowaveCount = incrementsMicrowaveCount;
    }

    internal int HeatingTicks { get; }
    internal float TargetTemperatureCelsius { get; }
    internal int QualityLossPenalty { get; }
    internal bool IncrementsMicrowaveCount { get; }
}

internal readonly struct MealHeatingOperationalFacts
{
    internal MealHeatingOperationalFacts(
        bool spawned,
        bool powered,
        bool fueled,
        bool flickedOn,
        bool brokenDown,
        bool emittingHeat)
    {
        Spawned = spawned;
        Powered = powered;
        Fueled = fueled;
        FlickedOn = flickedOn;
        BrokenDown = brokenDown;
        EmittingHeat = emittingHeat;
    }

    internal bool Spawned { get; }
    internal bool Powered { get; }
    internal bool Fueled { get; }
    internal bool FlickedOn { get; }
    internal bool BrokenDown { get; }
    internal bool EmittingHeat { get; }
}

internal sealed class MealHeatingCandidate<T>
{
    internal MealHeatingCandidate(T value, MealHeatingSourceKind kind, int distanceSquared)
    {
        Value = value;
        Kind = kind;
        DistanceSquared = Math.Max(0, distanceSquared);
    }

    internal T Value { get; }
    internal MealHeatingSourceKind Kind { get; }
    internal int DistanceSquared { get; }
}

internal static class MealHeatingPolicy
{
    internal const float RoomTemperatureCelsius = 15f;

    internal static bool ShouldAutomaticallyHeat(
        float temperatureCelsius,
        float configuredThreshold) =>
        !float.IsNaN(temperatureCelsius) &&
        temperatureCelsius < Math.Min(configuredThreshold, RoomTemperatureCelsius);

    internal static MealHeatingProfile ProfileFor(
        MealHeatingSourceKind sourceKind,
        int microwaveHeatingTicks) =>
        sourceKind switch
        {
            MealHeatingSourceKind.Microwave => new MealHeatingProfile(
                Math.Max(1, microwaveHeatingTicks),
                60f,
                0,
                incrementsMicrowaveCount: true),
            MealHeatingSourceKind.Stove => new MealHeatingProfile(
                450,
                55f,
                3,
                incrementsMicrowaveCount: false),
            MealHeatingSourceKind.Campfire => new MealHeatingProfile(
                750,
                45f,
                6,
                incrementsMicrowaveCount: false),
            _ => new MealHeatingProfile(
                1800,
                20f,
                10,
                incrementsMicrowaveCount: false)
        };

    internal static MealHeatingSourceKind? Classify(MealHeatingSourceFacts facts)
    {
        if (!facts.Building)
        {
            return null;
        }

        if (facts.Microwave)
        {
            return MealHeatingSourceKind.Microwave;
        }

        if (!facts.PositiveHeatEmitter)
        {
            return null;
        }

        if (facts.MealSource)
        {
            return facts.OpenFlame
                ? MealHeatingSourceKind.Campfire
                : MealHeatingSourceKind.Stove;
        }

        return MealHeatingSourceKind.AmbientHeater;
    }

    internal static bool IsOperational(MealHeatingOperationalFacts facts) =>
        facts.Spawned &&
        facts.Powered &&
        facts.Fueled &&
        facts.FlickedOn &&
        !facts.BrokenDown &&
        facts.EmittingHeat;

    internal static bool ShouldCancelWhenUnavailable(
        MealHeatingSourceKind sourceKind,
        bool normalDeliveryFallback) =>
        sourceKind == MealHeatingSourceKind.Microwave && !normalDeliveryFallback;

    internal static bool CanApplyCompletedCycle(
        bool interrupted,
        bool sourceOperational) =>
        !interrupted && sourceOperational;

    internal static MealHeatingCandidate<T>? Choose<T>(
        IEnumerable<MealHeatingCandidate<T>> candidates) =>
        Order(candidates).FirstOrDefault();

    internal static IEnumerable<MealHeatingCandidate<T>> Order<T>(
        IEnumerable<MealHeatingCandidate<T>> candidates) =>
        candidates
            .OrderBy(candidate => (int)candidate.Kind)
            .ThenBy(candidate => candidate.DistanceSquared);
}

internal sealed class MealHeatingSource
{
    private MealHeatingSource(
        Thing thing,
        MealHeatingSourceKind kind,
        MealHeatingProfile profile)
    {
        Thing = thing;
        Kind = kind;
        Profile = profile;
    }

    internal Thing Thing { get; }
    internal MealHeatingSourceKind Kind { get; }
    internal MealHeatingProfile Profile { get; }
    internal PathEndMode PathEndMode =>
        Kind == MealHeatingSourceKind.AmbientHeater
            ? PathEndMode.Touch
            : PathEndMode.InteractionCell;

    internal bool IsOperational
    {
        get
        {
            if (Kind == MealHeatingSourceKind.Microwave)
            {
                return Thing.TryGetComp<CompMicrowave>()?.Operational == true;
            }

            return MealHeatingPolicy.IsOperational(new MealHeatingOperationalFacts(
                Thing.Spawned,
                Thing.TryGetComp<CompPowerTrader>()?.PowerOn ?? true,
                Thing.TryGetComp<CompRefuelable>()?.HasFuel ?? true,
                Thing.TryGetComp<CompFlickable>()?.SwitchIsOn ?? true,
                Thing.TryGetComp<CompBreakdownable>()?.BrokenDown ?? false,
                IsEmittingHeat()));
        }
    }

    private bool IsEmittingHeat()
    {
        if (DubsRadiatorHeatRuntime.TryGetEmittingHeat(Thing, out var dubsRadiatorEmitting))
        {
            return dubsRadiatorEmitting;
        }

        if (Thing.TryGetComp<CompHeatPusher>() is { Props.heatPerSecond: > 0f } heatPusher)
        {
            return heatPusher.ShouldPushHeatNow;
        }

        if (Thing.TryGetComp<CompTempControl>() is { Props.energyPerSecond: > 0f } tempControl)
        {
            return tempControl.operatingAtHighPower;
        }

        return false;
    }

    internal bool TryHeat(Thing meal)
    {
        if (!TemperatureOwnership.ImmersiveChefsFeaturesActive ||
            !IsOperational ||
            meal is not ThingWithComps withComps ||
            !MealCoveragePolicy.IsCovered(meal.def) ||
            withComps.GetComp<CompCulinaryState>() is not { } culinaryState ||
            culinaryState.PeekCurrentServing() is not { } serving ||
            !MealHeatingPolicy.ShouldAutomaticallyHeat(
                serving.TemperatureCelsius,
                ImmersiveChefsMod.Settings.AutoMicrowaveBelow))
        {
            return false;
        }

        return culinaryState.HeatCurrentServing(
            Profile.TargetTemperatureCelsius,
            ImmersiveChefsMod.Settings.MicrowaveQualityLoss,
            Profile.QualityLossPenalty,
            Find.TickManager?.TicksGame ?? 0,
            Profile.IncrementsMicrowaveCount);
    }

    internal static MealHeatingSource? TryCreate(Thing thing)
    {
        if (thing is not ThingWithComps || thing.def.comps is null)
        {
            return null;
        }

        var microwave = thing.TryGetComp<CompMicrowave>();
        var positiveHeatEmitter = thing.def.comps.Any(properties =>
            properties is CompProperties_HeatPusher { heatPerSecond: > 0f } ||
            properties is CompProperties_TempControl { energyPerSecond: > 0f });
        var kind = MealHeatingPolicy.Classify(new MealHeatingSourceFacts(
            thing.def.category == ThingCategory.Building,
            microwave is not null,
            thing.def.building?.isMealSource == true,
            thing.def.comps.Any(properties => properties is CompProperties_FireOverlay),
            positiveHeatEmitter));
        if (kind is null)
        {
            return null;
        }

        return new MealHeatingSource(
            thing,
            kind.Value,
            MealHeatingPolicy.ProfileFor(kind.Value, microwave?.HeatingTicks ?? 180));
    }
}

internal static class DubsRadiatorHeatRuntime
{
    private const string RadiatorCompTypeName = "DubsBadHygiene.CompRadiator";
    private static bool initialized;
    private static Type? radiatorCompType;
    private static MethodInfo? heaterTempGetter;
    private static MethodInfo? workingGetter;

    internal static bool TryGetEmittingHeat(Thing thing, out bool emittingHeat)
    {
        emittingHeat = false;
        if (!ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.DubsBadHygiene))
        {
            return false;
        }

        EnsureInitialized();
        if (radiatorCompType is null || heaterTempGetter is null || workingGetter is null ||
            thing is not ThingWithComps withComps)
        {
            return false;
        }

        var radiator = withComps.AllComps.FirstOrDefault(radiatorCompType.IsInstanceOfType);
        if (radiator is null)
        {
            return false;
        }

        try
        {
            emittingHeat = workingGetter.Invoke(radiator, null) is true &&
                           heaterTempGetter.Invoke(radiator, null) is float heaterTemperature &&
                           heaterTemperature > 0f;
            return true;
        }
        catch (Exception exception)
        {
            OptionalIntegrationDiagnostics.WarnOnce(
                OptionalIntegration.DubsBadHygiene,
                $"radiator heat state failed ({exception.GetType().Name}: {exception.Message}); " +
                "inactive radiators will not be used for meals");
            return true;
        }
    }

    private static void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;
        radiatorCompType = AccessTools.TypeByName(RadiatorCompTypeName);
        heaterTempGetter = radiatorCompType?
            .GetProperty("HeaterTemp", BindingFlags.Public | BindingFlags.Instance)?
            .GetMethod;
        workingGetter = radiatorCompType?
            .GetProperty("working", BindingFlags.Public | BindingFlags.Instance)?
            .GetMethod;
        if (radiatorCompType is null ||
            !typeof(ThingComp).IsAssignableFrom(radiatorCompType) ||
            heaterTempGetter?.ReturnType != typeof(float) ||
            workingGetter?.ReturnType != typeof(bool))
        {
            radiatorCompType = null;
            heaterTempGetter = null;
            workingGetter = null;
            OptionalIntegrationDiagnostics.WarnOnce(
                OptionalIntegration.DubsBadHygiene,
                "radiator heat-state shape changed; inactive radiators will not be used for meals");
        }
    }
}
