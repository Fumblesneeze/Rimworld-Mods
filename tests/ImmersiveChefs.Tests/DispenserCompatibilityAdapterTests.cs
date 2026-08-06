using NUnit.Framework;

namespace ImmersiveChefs.Tests;

[TestFixture]
public sealed class DispenserCompatibilityAdapterTests
{
    [TestCase(false, false, false)]
    [TestCase(true, true, false)]
    [TestCase(true, false, true)]
    public void Recognized_optional_dispenser_remains_fail_closed_after_resolver_failure(
        bool sourceRecognized,
        bool resolutionSucceeded,
        bool expected)
    {
        Assert.That(
            DispenserMealResolutionPolicy.Failed(sourceRecognized, resolutionSucceeded),
            Is.EqualTo(expected));
    }

    [TestCase("Replimat.ReplimatUtility", true, new[] { "Verse.Pawn", "Verse.Pawn" }, true, true)]
    [TestCase("Changed.Utility", true, new[] { "Verse.Pawn", "Verse.Pawn" }, true, false)]
    [TestCase("Replimat.ReplimatUtility", false, new[] { "Verse.Pawn", "Verse.Pawn" }, true, false)]
    [TestCase("Replimat.ReplimatUtility", true, new[] { "Verse.Pawn" }, true, false)]
    [TestCase("Replimat.ReplimatUtility", true, new[] { "Verse.Pawn", "Verse.Pawn" }, false, false)]
    public void Only_exact_replimat_native_meal_picker_shape_can_pin_the_reserved_tier(
        string utilityTypeName,
        bool sharesTerminalAssembly,
        string[] parameterTypeNames,
        bool returnsThingDef,
        bool expected)
    {
        Assert.That(
            ReplimatCompatibility.HasSupportedMealResolver(
                utilityTypeName,
                sharesTerminalAssembly,
                parameterTypeNames,
                returnsThingDef),
            Is.EqualTo(expected));
    }

    [Test]
    public void Exact_replimat_16_shape_is_supported()
    {
        Assert.That(
            ReplimatCompatibility.IsSupported(
                assemblyName: "Replimat",
                assemblyVersion: "1.0.0.0",
                terminalTypeName: "Replimat.Building_ReplimatTerminal",
                terminalIsPasteDispenser: true,
                tryDispenseParameterTypeNames: new[]
                {
                    "Verse.Pawn",
                    "Verse.Pawn",
                    "Verse.ThingDef",
                    "System.Int32"
                },
                tryDispenseReturnsThing: true,
                toilPrefixTypeName: "Replimat.Harmony_Toils_Ingest_TakeMealFromDispenser",
                toilPrefixShape: true,
                upstreamPatchOwner: "com.Replimat.patches",
                terminalDefNames: new[] { "ReplimatTerminal", "ReplimatTerminalWall" },
                mealRegistryValidated: true),
            Is.True);
    }

    [TestCase("Lookalike", "1.0.0.0", "Replimat.Building_ReplimatTerminal", true, true, "Replimat.Harmony_Toils_Ingest_TakeMealFromDispenser", true, "com.Replimat.patches", true)]
    [TestCase("Replimat", "2.0.0.0", "Replimat.Building_ReplimatTerminal", true, true, "Replimat.Harmony_Toils_Ingest_TakeMealFromDispenser", true, "com.Replimat.patches", true)]
    [TestCase("Replimat", "1.0.0.0", "Changed.Terminal", true, true, "Replimat.Harmony_Toils_Ingest_TakeMealFromDispenser", true, "com.Replimat.patches", true)]
    [TestCase("Replimat", "1.0.0.0", "Replimat.Building_ReplimatTerminal", false, true, "Replimat.Harmony_Toils_Ingest_TakeMealFromDispenser", true, "com.Replimat.patches", true)]
    [TestCase("Replimat", "1.0.0.0", "Replimat.Building_ReplimatTerminal", true, false, "Replimat.Harmony_Toils_Ingest_TakeMealFromDispenser", true, "com.Replimat.patches", true)]
    [TestCase("Replimat", "1.0.0.0", "Replimat.Building_ReplimatTerminal", true, true, "Changed.Prefix", true, "com.Replimat.patches", true)]
    [TestCase("Replimat", "1.0.0.0", "Replimat.Building_ReplimatTerminal", true, true, "Replimat.Harmony_Toils_Ingest_TakeMealFromDispenser", false, "com.Replimat.patches", true)]
    [TestCase("Replimat", "1.0.0.0", "Replimat.Building_ReplimatTerminal", true, true, "Replimat.Harmony_Toils_Ingest_TakeMealFromDispenser", true, "lookalike.owner", true)]
    [TestCase("Replimat", "1.0.0.0", "Replimat.Building_ReplimatTerminal", true, true, "Replimat.Harmony_Toils_Ingest_TakeMealFromDispenser", true, "com.Replimat.patches", false)]
    public void Changed_or_partial_replimat_shape_fails_closed(
        string assemblyName,
        string assemblyVersion,
        string terminalTypeName,
        bool terminalIsPasteDispenser,
        bool tryDispenseReturnsThing,
        string prefixTypeName,
        bool prefixShape,
        string patchOwner,
        bool mealRegistryValidated)
    {
        Assert.That(
            ReplimatCompatibility.IsSupported(
                assemblyName,
                assemblyVersion,
                terminalTypeName,
                terminalIsPasteDispenser,
                new[] { "Verse.Pawn", "Verse.Pawn", "Verse.ThingDef", "System.Int32" },
                tryDispenseReturnsThing,
                prefixTypeName,
                prefixShape,
                patchOwner,
                new[] { "ReplimatTerminal", "ReplimatTerminalWall" },
                mealRegistryValidated),
            Is.False);
    }

    [Test]
    public void Exact_meal_printer_16_shape_is_supported()
    {
        Assert.That(
            MealPrinterCompatibility.IsSupported(
                assemblyName: "MealPrinter",
                assemblyVersion: "0.0.0.0",
                printerTypeName: "MealPrinter.Building_MealPrinter",
                printerIsPasteDispenser: true,
                tryDispenseParameterCount: 0,
                tryDispenseReturnsThing: true,
                toilPrefixTypeName: "MealPrinter.HarmonyPatches.Toils_Ingest_TakeMealFromDispenser",
                toilPrefixShape: true,
                upstreamPatchOwner: "MealPrinter",
                printerDefName: "MealPrinter",
                nutriBarDefName: "MealPrinter_NutriBar",
                vanillaMealDefsPresent: true),
            Is.True);
    }

    [Test]
    public void Exact_meal_printer_passive_shape_is_safe_to_initialize_before_its_patch_owner_exists()
    {
        Assert.That(
            MealPrinterCompatibility.HasSupportedPassiveShape(
                assemblyName: "MealPrinter",
                assemblyVersion: "0.0.0.0",
                printerTypeName: "MealPrinter.Building_MealPrinter",
                printerIsPasteDispenser: true,
                tryDispenseParameterCount: 0,
                tryDispenseReturnsThing: true,
                toilPrefixTypeName: "MealPrinter.HarmonyPatches.Toils_Ingest_TakeMealFromDispenser",
                toilPrefixShape: true,
                printerDefName: "MealPrinter",
                nutriBarDefName: "MealPrinter_NutriBar",
                vanillaMealDefsPresent: true),
            Is.True);
    }

    [TestCase("MealPrinter.MealPrinterMain", true, true, true)]
    [TestCase("Changed.Startup", true, true, false)]
    [TestCase("MealPrinter.MealPrinterMain", false, true, false)]
    [TestCase("MealPrinter.MealPrinterMain", true, false, false)]
    public void Only_exact_meal_printer_startup_owner_is_safe_to_initialize(
        string startupTypeName,
        bool hasStartupAttribute,
        bool sharesPrinterAssembly,
        bool expected)
    {
        Assert.That(
            MealPrinterCompatibility.HasSupportedStartupType(
                startupTypeName,
                hasStartupAttribute,
                sharesPrinterAssembly),
            Is.EqualTo(expected));
    }

    [TestCase("GetMealThing", true, 0, true, true)]
    [TestCase("ChangedMealGetter", true, 0, true, false)]
    [TestCase("GetMealThing", false, 0, true, false)]
    [TestCase("GetMealThing", true, 1, true, false)]
    [TestCase("GetMealThing", true, 0, false, false)]
    public void Only_exact_meal_printer_native_output_getter_can_drive_dining_requirements(
        string methodName,
        bool sharesPrinterType,
        int parameterCount,
        bool returnsThingDef,
        bool expected)
    {
        Assert.That(
            MealPrinterCompatibility.HasSupportedMealResolver(
                methodName,
                sharesPrinterType,
                parameterCount,
                returnsThingDef),
            Is.EqualTo(expected));
    }

    [TestCase("Lookalike", "0.0.0.0", "MealPrinter.Building_MealPrinter", true, 0, true, "MealPrinter.HarmonyPatches.Toils_Ingest_TakeMealFromDispenser", true, "MealPrinter", "MealPrinter_NutriBar", true)]
    [TestCase("MealPrinter", "0.0.0.0", "MealPrinter.Building_MealPrinter", true, 1, true, "MealPrinter.HarmonyPatches.Toils_Ingest_TakeMealFromDispenser", true, "MealPrinter", "MealPrinter_NutriBar", true)]
    [TestCase("MealPrinter", "0.0.0.0", "MealPrinter.Building_MealPrinter", true, 0, true, "Changed.Prefix", true, "MealPrinter", "MealPrinter_NutriBar", true)]
    public void Changed_meal_printer_passive_shape_is_never_initialized(
        string assemblyName,
        string assemblyVersion,
        string printerTypeName,
        bool printerIsPasteDispenser,
        int parameterCount,
        bool tryDispenseReturnsThing,
        string prefixTypeName,
        bool prefixShape,
        string printerDefName,
        string nutriBarDefName,
        bool vanillaMealDefsPresent)
    {
        Assert.That(
            MealPrinterCompatibility.HasSupportedPassiveShape(
                assemblyName,
                assemblyVersion,
                printerTypeName,
                printerIsPasteDispenser,
                parameterCount,
                tryDispenseReturnsThing,
                prefixTypeName,
                prefixShape,
                printerDefName,
                nutriBarDefName,
                vanillaMealDefsPresent),
            Is.False);
    }

    [TestCase("Lookalike", "0.0.0.0", "MealPrinter.Building_MealPrinter", true, 0, true, "MealPrinter.HarmonyPatches.Toils_Ingest_TakeMealFromDispenser", true, "MealPrinter", "MealPrinter", "MealPrinter_NutriBar", true)]
    [TestCase("MealPrinter", "1.0.0.0", "MealPrinter.Building_MealPrinter", true, 0, true, "MealPrinter.HarmonyPatches.Toils_Ingest_TakeMealFromDispenser", true, "MealPrinter", "MealPrinter", "MealPrinter_NutriBar", true)]
    [TestCase("MealPrinter", "0.0.0.0", "Changed.Printer", true, 0, true, "MealPrinter.HarmonyPatches.Toils_Ingest_TakeMealFromDispenser", true, "MealPrinter", "MealPrinter", "MealPrinter_NutriBar", true)]
    [TestCase("MealPrinter", "0.0.0.0", "MealPrinter.Building_MealPrinter", false, 0, true, "MealPrinter.HarmonyPatches.Toils_Ingest_TakeMealFromDispenser", true, "MealPrinter", "MealPrinter", "MealPrinter_NutriBar", true)]
    [TestCase("MealPrinter", "0.0.0.0", "MealPrinter.Building_MealPrinter", true, 1, true, "MealPrinter.HarmonyPatches.Toils_Ingest_TakeMealFromDispenser", true, "MealPrinter", "MealPrinter", "MealPrinter_NutriBar", true)]
    [TestCase("MealPrinter", "0.0.0.0", "MealPrinter.Building_MealPrinter", true, 0, false, "MealPrinter.HarmonyPatches.Toils_Ingest_TakeMealFromDispenser", true, "MealPrinter", "MealPrinter", "MealPrinter_NutriBar", true)]
    [TestCase("MealPrinter", "0.0.0.0", "MealPrinter.Building_MealPrinter", true, 0, true, "Changed.Prefix", true, "MealPrinter", "MealPrinter", "MealPrinter_NutriBar", true)]
    [TestCase("MealPrinter", "0.0.0.0", "MealPrinter.Building_MealPrinter", true, 0, true, "MealPrinter.HarmonyPatches.Toils_Ingest_TakeMealFromDispenser", false, "MealPrinter", "MealPrinter", "MealPrinter_NutriBar", true)]
    [TestCase("MealPrinter", "0.0.0.0", "MealPrinter.Building_MealPrinter", true, 0, true, "MealPrinter.HarmonyPatches.Toils_Ingest_TakeMealFromDispenser", true, "lookalike.owner", "MealPrinter", "MealPrinter_NutriBar", true)]
    [TestCase("MealPrinter", "0.0.0.0", "MealPrinter.Building_MealPrinter", true, 0, true, "MealPrinter.HarmonyPatches.Toils_Ingest_TakeMealFromDispenser", true, "MealPrinter", "Changed", "MealPrinter_NutriBar", true)]
    [TestCase("MealPrinter", "0.0.0.0", "MealPrinter.Building_MealPrinter", true, 0, true, "MealPrinter.HarmonyPatches.Toils_Ingest_TakeMealFromDispenser", true, "MealPrinter", "MealPrinter", "Changed", true)]
    [TestCase("MealPrinter", "0.0.0.0", "MealPrinter.Building_MealPrinter", true, 0, true, "MealPrinter.HarmonyPatches.Toils_Ingest_TakeMealFromDispenser", true, "MealPrinter", "MealPrinter", "MealPrinter_NutriBar", false)]
    public void Changed_or_partial_meal_printer_shape_fails_closed(
        string assemblyName,
        string assemblyVersion,
        string printerTypeName,
        bool printerIsPasteDispenser,
        int parameterCount,
        bool tryDispenseReturnsThing,
        string prefixTypeName,
        bool prefixShape,
        string patchOwner,
        string printerDefName,
        string nutriBarDefName,
        bool vanillaMealDefsPresent)
    {
        Assert.That(
            MealPrinterCompatibility.IsSupported(
                assemblyName,
                assemblyVersion,
                printerTypeName,
                printerIsPasteDispenser,
                parameterCount,
                tryDispenseReturnsThing,
                prefixTypeName,
                prefixShape,
                patchOwner,
                printerDefName,
                nutriBarDefName,
                vanillaMealDefsPresent),
            Is.False);
    }

    [TestCase("VanillaPaste", "MealNutrientPaste", true)]
    [TestCase("Replimat", "MealNutrientPaste", false)]
    [TestCase("Replimat", "ReplimatMeals_F_Ramen", false)]
    [TestCase("MealPrinter", "MealNutrientPaste", true)]
    [TestCase("MealPrinter", "MealFine", false)]
    public void Only_actual_nutrient_paste_output_uses_paste_quality_initialization(
        string sourceName,
        string mealDefName,
        bool expected)
    {
        var source = (DispenserMealSource)Enum.Parse(typeof(DispenserMealSource), sourceName);
        Assert.That(
            DispenserMealBindingPolicy.UsePasteInitialization(source, mealDefName),
            Is.EqualTo(expected));
    }

    [TestCase("Replimat", "ReplimatMeals_F_Ramen", true, 1, true)]
    [TestCase("Replimat", "MealSurvivalPack", false, 10, false)]
    [TestCase("MealPrinter", "MealFine", true, 1, true)]
    [TestCase("MealPrinter", "MealPrinter_NutriBar", false, 1, false)]
    [TestCase("MealPrinter", "MealFine", true, 2, false)]
    [TestCase("MealPrinter", "MealFine", false, 1, false)]
    public void Only_one_covered_ordinary_dispensed_serving_can_consume_the_reserved_plate(
        string sourceName,
        string mealDefName,
        bool covered,
        int stackCount,
        bool expected)
    {
        var source = (DispenserMealSource)Enum.Parse(typeof(DispenserMealSource), sourceName);

        Assert.That(
            DispenserMealBindingPolicy.ShouldBind(source, mealDefName, covered, stackCount),
            Is.EqualTo(expected));
    }
}
