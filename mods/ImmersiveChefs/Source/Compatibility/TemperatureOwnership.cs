using RimWorld;
using Verse;

namespace ImmersiveChefs;

public enum MealTemperatureOwner
{
    ImmersiveChefs,
    Thermodynamics
}

public readonly struct TemperatureOwnershipDecision
{
    public TemperatureOwnershipDecision(MealTemperatureOwner owner, bool shapeWarningRequired)
    {
        Owner = owner;
        ShapeWarningRequired = shapeWarningRequired;
    }

    public MealTemperatureOwner Owner { get; }

    public bool ShapeWarningRequired { get; }

    public bool ImmersiveChefsFeaturesActive => Owner == MealTemperatureOwner.ImmersiveChefs;
}

public static class TemperatureOwnership
{
    public const string ThermodynamicsPackageId = "Mlie.DThermodynamicsHotMeals";

    private static readonly Lazy<bool> ImmersiveChefsFeatures = new(() => Decide(
        CurrentPackageIds(),
        thermodynamicsShapeValid: true).ImmersiveChefsFeaturesActive);
    private static bool shapeWarningLogged;

    public static bool ImmersiveChefsFeaturesActive => ImmersiveChefsFeatures.Value;

    public static TemperatureOwnershipDecision Decide(
        IEnumerable<string> loadedPackageIds,
        bool thermodynamicsShapeValid)
    {
        if (loadedPackageIds is null)
        {
            throw new ArgumentNullException(nameof(loadedPackageIds));
        }

        var thermodynamicsActive = loadedPackageIds.Any(packageId =>
            string.Equals(packageId, ThermodynamicsPackageId, StringComparison.OrdinalIgnoreCase));
        if (!thermodynamicsActive)
        {
            return new TemperatureOwnershipDecision(MealTemperatureOwner.ImmersiveChefs, false);
        }

        return new TemperatureOwnershipDecision(
            MealTemperatureOwner.Thermodynamics,
            shapeWarningRequired: !thermodynamicsShapeValid);
    }

    internal static void ValidateActiveProviderShape()
    {
        if (ImmersiveChefsFeaturesActive || shapeWarningLogged)
        {
            return;
        }

        var shapeValid = AppDomain.CurrentDomain.GetAssemblies().Any(assembly =>
                             string.Equals(
                                 assembly.GetName().Name,
                                 "Hot Meals",
                                 StringComparison.Ordinal) &&
                             assembly.GetType("DHotMeals.JobDriver_HeatMeal", throwOnError: false) is not null) &&
                         DefDatabase<ThingDef>.GetNamedSilentFail("DMicrowave") is not null &&
                         DefDatabase<JobDef>.GetNamedSilentFail("HeatMeal") is not null;
        var decision = Decide(CurrentPackageIds(), shapeValid);
        if (!decision.ShapeWarningRequired)
        {
            return;
        }

        shapeWarningLogged = true;
        Log.Warning(
            "[ImmersiveChefs] Thermodynamics - Hot Meals is active but its inspected 1.6.6 " +
            "assembly/DMicrowave/HeatMeal shape changed. Immersive Chefs temperature and microwave " +
            "features remain suppressed to prevent duplicate providers; only non-temperature systems continue.");
    }

    private static IEnumerable<string> CurrentPackageIds()
    {
        return ImmersiveChefsMod.Integrations?.LoadedPackageIds ??
               LoadedModManager.RunningModsListForReading.Select(mod => mod.PackageId);
    }
}
