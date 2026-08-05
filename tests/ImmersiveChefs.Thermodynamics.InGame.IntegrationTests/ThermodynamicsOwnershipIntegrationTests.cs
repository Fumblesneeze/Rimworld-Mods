using System;
using System.Linq;
using RimWorld;
using RimWorldDevGateway.IntegrationTesting;
using Verse;

namespace ImmersiveChefs.Thermodynamics.InGame.IntegrationTests;

public static class ThermodynamicsOwnershipIntegrationTests
{
    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ThermodynamicsExclusivelyOwnsFinalizedTemperatureDefs()
    {
        IntegrationAssert.True(
            LoadedModManager.RunningModsListForReading.Any(mod => string.Equals(
                mod.PackageId,
                TemperatureOwnership.ThermodynamicsPackageId,
                StringComparison.OrdinalIgnoreCase)),
            "Thermodynamics must be an actually active ModContentPack for this group.");
        IntegrationAssert.True(
            !TemperatureOwnership.ImmersiveChefsFeaturesActive,
            "Package presence must suppress every Immersive Chefs temperature feature.");
        IntegrationAssert.NotNull(
            DefDatabase<ThingDef>.GetNamedSilentFail("DMicrowave"),
            "Thermodynamics must retain its own DMicrowave Def.");
        IntegrationAssert.NotNull(
            DefDatabase<JobDef>.GetNamedSilentFail("HeatMeal"),
            "Thermodynamics must retain its own HeatMeal job.");
        IntegrationAssert.Null(
            DefDatabase<ThingDef>.GetNamedSilentFail("ImmersiveChefs_Microwave"),
            "The Immersive Chefs fallback microwave must be removed before Def deserialization.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void CulinaryStateRemainsNonThermalAlongsideThermodynamics()
    {
        var mealDef = DefDatabase<ThingDef>.GetNamed("MealSimple");
        IntegrationAssert.True(
            mealDef.comps.Any(properties => properties.compClass == typeof(CompEmbeddedWare)),
            "Immersive Chefs plate state must remain attached to meals.");
        IntegrationAssert.True(
            mealDef.comps.Any(properties => properties.compClass == typeof(CompCulinaryState)),
            "Immersive Chefs culinary-quality state must remain attached to meals.");

        IntegrationAssert.True(
            !TemperatureOwnership.ImmersiveChefsFeaturesActive,
            "The same finalized meal Def must not reactivate Immersive Chefs thermal behavior.");
    }
}
