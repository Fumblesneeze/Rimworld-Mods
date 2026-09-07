using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorldDevGateway.IntegrationTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.MealPrinterMods.InGame.IntegrationTests;

public static class MealPrinterModsIntegrationTests
{
    private static readonly string[] ExpectedPackages =
    {
        "brrainz.harmony",
        "ludeon.rimworld",
        "imranfish.xmlextensions",
        "Orion.Hospitality",
        "Mlie.MealPrinter",
        "Orion.CashRegister",
        "Orion.Gastronomy",
        "fumblesneeze.immersivechefs",
        "fumblesneeze.rimworlddevgateway"
    };

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ExactPackagesAssemblyAndServiceAdaptersAreActive()
    {
        var active = LoadedModManager.RunningModsListForReading.Select(mod => mod.PackageId).ToArray();
        IntegrationAssert.Equal(
            string.Join("|", ExpectedPackages).ToLowerInvariant(),
            string.Join("|", active).ToLowerInvariant(),
            "The printer-service group must use the complete dependency-valid package sequence.");

        var printerType = AccessTools.TypeByName(MealPrinterCompatibility.PrinterTypeName);
        IntegrationAssert.Equal(
            "MealPrinter",
            printerType?.Assembly.GetName().Name,
            "Meal Printer must expose the required types from its own assembly.");
        IntegrationAssert.True(
            ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.MealPrinter) &&
            MealPrinterAdapter.Enabled,
            "The exact Meal Printer package and finalized vanilla meal Defs must pass the passive shape gate.");
        IntegrationAssert.True(
            ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.Hospitality) &&
            ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.Gastronomy) &&
            GastronomyAdapter.Enabled,
            "Hospitality and Gastronomy must remain active around the native printer path.");

        var guestUtility = AccessTools.TypeByName("Hospitality.Utilities.GuestUtility");
        IntegrationAssert.True(
            HospitalityAdapter.TryBind(guestUtility, out var predicate, out var reason) && predicate is not null,
            "Hospitality must retain its validated arrived-guest shape: " + reason);
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void NativePrinterOwnerAndImmersivePostfixComposeExactlyOnce()
    {
        var printerType = AccessTools.TypeByName(MealPrinterCompatibility.PrinterTypeName);
        var prefixType = AccessTools.TypeByName(MealPrinterCompatibility.ToilPrefixTypeName);
        IntegrationAssert.NotNull(printerType, "The exact Meal Printer building type must exist.");
        IntegrationAssert.NotNull(prefixType, "The exact Meal Printer dispenser-toil patch type must exist.");

        var dispense = printerType!.GetMethod(
            "TryDispenseFood",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null);
        var configuredMeal = printerType.GetMethod(
            "GetMealThing",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null);
        var prefix = prefixType!.GetMethod(
            "Prefix",
            BindingFlags.NonPublic | BindingFlags.Static,
            null,
            new[]
            {
                typeof(TargetIndex).MakeByRefType(),
                typeof(Pawn).MakeByRefType(),
                typeof(Toil).MakeByRefType()
            },
            null);
        var takeMeal = AccessTools.Method(
            typeof(Toils_Ingest),
            nameof(Toils_Ingest.TakeMealFromDispenser),
            new[] { typeof(TargetIndex), typeof(Pawn) });
        IntegrationAssert.NotNull(dispense, "The exact zero-argument native printer method must exist.");
        IntegrationAssert.Equal(
            typeof(ThingDef),
            configuredMeal?.ReturnType,
            "The native printer output getter must retain its zero-argument ThingDef shape.");
        IntegrationAssert.NotNull(prefix, "The exact native Meal Printer toil prefix must exist.");
        IntegrationAssert.NotNull(takeMeal, "RimWorld's dispenser ingestion boundary must exist.");

        IntegrationAssert.Equal(
            1,
            Harmony.GetPatchInfo(takeMeal!)?.Prefixes.Count(patch =>
                patch.owner == MealPrinterCompatibility.UpstreamPatchOwner &&
                patch.PatchMethod == prefix) ?? 0,
            "Meal Printer must remain the sole owner of its validated native dispenser toil prefix.");
        IntegrationAssert.Equal(
            1,
            Harmony.GetPatchInfo(dispense!)?.Postfixes.Count(patch =>
                patch.owner == ImmersiveChefsMod.PackageId &&
                patch.PatchMethod?.DeclaringType == typeof(MealPrinterAdapter)) ?? 0,
            "Immersive Chefs must attach exactly one post-success printer plate-binding postfix.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void NativeGastronomyClearOrderDeliversCutleryBeforeFreeServiceCanEnd()
    {
        var waitingToils = AccessTools.TypeByName("Gastronomy.Waiting.Toils_Waiting");
        IntegrationAssert.NotNull(
            waitingToils,
            "The exact Gastronomy waiting-toil type must exist in the active printer-service group.");
        var clearOrder = AccessTools.Method(
            waitingToils,
            "ClearOrder",
            new[]
            {
                typeof(TargetIndex),
                typeof(TargetIndex),
                typeof(TargetIndex),
                typeof(TargetIndex)
            });
        IntegrationAssert.Equal(
            typeof(Toil),
            clearOrder?.ReturnType,
            "Gastronomy's exact four-target ClearOrder factory must retain its Toil return shape.");
        IntegrationAssert.True(
            clearOrder!.IsStatic,
            "Gastronomy's exact ClearOrder factory must remain static.");
        IntegrationAssert.Equal(
            1,
            Harmony.GetPatchInfo(clearOrder)?.Postfixes.Count(patch =>
                patch.owner == ImmersiveChefsMod.PackageId &&
                patch.PatchMethod?.DeclaringType == typeof(GastronomyAdapter)) ?? 0,
            "Immersive Chefs must attach exactly one pre-clear cutlery-delivery bridge.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FinalizedPrinterOutputsPreserveVanillaOwnershipAndNutriBarExclusion()
    {
        var printer = DefDatabase<ThingDef>.GetNamed("MealPrinter");
        var printerType = AccessTools.TypeByName(MealPrinterCompatibility.PrinterTypeName);
        IntegrationAssert.Equal(
            "mlie.mealprinter",
            printer.modContentPack?.PackageId.ToLowerInvariant(),
            "The building Def must remain owned by Meal Printer.");
        IntegrationAssert.Equal(
            printerType,
            printer.thingClass,
            "The building Def must retain its native printer class.");

        foreach (var defName in new[] { "MealNutrientPaste", "MealSimple", "MealFine" })
        {
            var meal = DefDatabase<ThingDef>.GetNamed(defName);
            IntegrationAssert.Equal(
                1,
                meal.comps.Count(properties => properties.compClass == typeof(CompEmbeddedWare)),
                defName + " must keep exactly one ordinary embedded-ware lifecycle.");
            IntegrationAssert.Equal(
                1,
                meal.comps.Count(properties => properties.compClass == typeof(CompCulinaryState)),
                defName + " must keep exactly one ordinary culinary lifecycle.");
        }

        var nutriBar = DefDatabase<ThingDef>.GetNamed("MealPrinter_NutriBar");
        IntegrationAssert.Equal(
            "mlie.mealprinter",
            nutriBar.modContentPack?.PackageId.ToLowerInvariant(),
            "NutriBar must remain owned by Meal Printer.");
        IntegrationAssert.True(
            !MealCoveragePolicy.IsCovered(nutriBar),
            "NutriBar must remain a hand-eaten emergency/travel product.");
        IntegrationAssert.Equal(
            0,
            nutriBar.comps.Count(properties =>
                properties.compClass == typeof(CompEmbeddedWare) ||
                properties.compClass == typeof(CompCulinaryState)),
            "NutriBar must receive no plate or culinary lifecycle.");
    }
}
