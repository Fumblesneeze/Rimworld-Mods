using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorldDevGateway.IntegrationTesting;
using Verse;

namespace ImmersiveChefs.TextureVariations.InGame.IntegrationTests;

public static class TextureVariationFinalizedDefTests
{
    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ExactVtexVefGroupInstallsPortableSelectorsAndBuildingVariations()
    {
        var requiredPackages = new[]
        {
            "OskarPotocki.VanillaFactionsExpanded.Core",
            "VanillaExpanded.VTEXVariations"
        };
        foreach (var packageId in requiredPackages)
        {
            IntegrationAssert.True(
                LoadedModManager.RunningModsListForReading.Any(mod => string.Equals(
                    mod.PackageId,
                    packageId,
                    StringComparison.OrdinalIgnoreCase)),
                packageId + " must be an actually active ModContentPack for this exact group.");
        }

        var expectedDefNames = new[]
        {
            "ImmersiveChefs_Cookware",
            "ImmersiveChefs_Plate",
            "ImmersiveChefs_Cutlery",
            "ImmersiveChefs_ChefsKnife"
        };
        var selected = DefDatabase<ThingDef>.AllDefsListForReading
            .Where(def =>
                string.Equals(
                    def.modContentPack?.PackageId,
                    ImmersiveChefsMod.PackageId,
                    StringComparison.OrdinalIgnoreCase) &&
                def.graphicData?.graphicClass == typeof(Graphic_PortableKitchenwareVariation))
            .Select(def => def.defName)
            .OrderBy(defName => defName, StringComparer.Ordinal)
            .ToArray();

        IntegrationAssert.Equal(
            string.Join("|", expectedDefNames.OrderBy(value => value, StringComparer.Ordinal)),
            string.Join("|", selected),
            "The exact VTEX/VEF group must install the selector on exactly the four supported portable Defs.");
        foreach (var defName in expectedDefNames)
        {
            IntegrationAssert.Equal(
                typeof(Graphic_PortableKitchenwareVariation),
                DefDatabase<ThingDef>.GetNamed(defName).graphicData.graphicClass,
                defName + " must retain the finalized optional selector.");
        }

        IntegrationAssert.Equal(
            typeof(Graphic_Single),
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_AdobePlate").graphicData.graphicClass,
            "The fixed adobe plate must not receive a Stuff/material selector.");
        IntegrationAssert.Equal(
            typeof(Graphic_Single),
            DefDatabase<ThingDef>.GetNamed("ImmersiveChefs_GlitterworldCookware").graphicData.graphicClass,
            "Fixed-color glitterworld cookware must not receive the portable selector.");

        var buildingVariants = new Dictionary<string, (string BasePath, string VariantPath, string BaseName, string VariantName)>
        {
            ["ImmersiveChefs_Dishwasher"] =
                ("ImmersiveChefs/Things/Building/Dishwasher/Dishwasher", "ImmersiveChefs/Things/Building/Dishwasher/Dishwasher_Variant01", "standard dishwasher", "alternate dishwasher"),
            ["ImmersiveChefs_IndustrialDishwasher"] =
                ("ImmersiveChefs/Things/Building/Dishwasher/IndustrialDishwasher", "ImmersiveChefs/Things/Building/Dishwasher/IndustrialDishwasher_Variant01", "standard industrial dishwasher", "alternate industrial dishwasher"),
            ["ImmersiveChefs_PrepStation"] =
                ("ImmersiveChefs/Things/Building/KitchenStation/PrepStation", "ImmersiveChefs/Things/Building/KitchenStation/PrepStation_Variant01", "standard prep station", "alternate prep station"),
            ["ImmersiveChefs_SauceStation"] =
                ("ImmersiveChefs/Things/Building/KitchenStation/SauceStation", "ImmersiveChefs/Things/Building/KitchenStation/SauceStation_Variant01", "standard sauce station", "alternate sauce station"),
            ["ImmersiveChefs_MeatStation"] =
                ("ImmersiveChefs/Things/Building/KitchenStation/MeatStation", "ImmersiveChefs/Things/Building/KitchenStation/MeatStation_Variant01", "standard meat station", "alternate meat station"),
            ["ImmersiveChefs_VegetableStation"] =
                ("ImmersiveChefs/Things/Building/KitchenStation/VegetableStation", "ImmersiveChefs/Things/Building/KitchenStation/VegetableStation_Variant01", "standard vegetable station", "alternate vegetable station"),
            ["ImmersiveChefs_PastryStation"] =
                ("ImmersiveChefs/Things/Building/KitchenStation/PastryStation", "ImmersiveChefs/Things/Building/KitchenStation/PastryStation_Variant01", "standard pastry station", "alternate pastry station"),
            ["ImmersiveChefs_Microwave"] =
                ("ImmersiveChefs/Things/Building/Appliance/Microwave", "ImmersiveChefs/Things/Building/Appliance/Microwave_Variant01", "standard microwave", "alternate microwave")
        };
        foreach (var (defName, variant) in buildingVariants)
        {
            var properties = DefDatabase<ThingDef>.GetNamed(defName).comps
                .Where(value => value.GetType().FullName ==
                    "VEF.Buildings.CompProperties_RandomBuildingGraphic")
                .ToArray();
            IntegrationAssert.Equal(
                1,
                properties.Length,
                defName + " must finalize exactly one VEF random-building-graphic comp.");
            var propertyType = properties[0].GetType();
            IntegrationAssert.Equal(
                variant.BasePath + "|" + variant.VariantPath,
                ReadStrings(propertyType, properties[0], "randomGraphics"),
                defName + " must finalize standard and alternate sprite paths in that order.");
            IntegrationAssert.Equal(
                variant.BaseName + "|" + variant.VariantName,
                ReadStrings(propertyType, properties[0], "optionalNames"),
                defName + " must finalize the matching player-facing variant names.");
        }
    }

    private static string ReadStrings(Type type, object owner, string fieldName)
    {
        var values = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.Public)
            ?.GetValue(owner) as IEnumerable;
        IntegrationAssert.NotNull(values, type.FullName + "." + fieldName + " must retain the inspected VEF shape.");
        var materialized = values!.Cast<object>().Select(value => value?.ToString() ?? string.Empty).ToArray();
        IntegrationAssert.Equal(2, materialized.Length, fieldName + " must contain exactly two entries.");
        return string.Join("|", materialized);
    }
}
