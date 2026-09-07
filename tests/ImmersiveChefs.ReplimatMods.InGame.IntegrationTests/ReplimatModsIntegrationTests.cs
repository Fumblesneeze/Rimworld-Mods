using System;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorldDevGateway.IntegrationTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.ReplimatMods.InGame.IntegrationTests;

public static class ReplimatModsIntegrationTests
{
    private static readonly string[] ExpectedPackages =
    {
        "brrainz.harmony",
        "ludeon.rimworld",
        "imranfish.xmlextensions",
        "sumghai.Replimat",
        "sumghai.ReplimatMeals",
        "Dubwise.DubsBadHygiene",
        "avilmask.CommonSense",
        "fumblesneeze.immersivechefs",
        "fumblesneeze.rimworlddevgateway"
    };

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ExactPackagesAssemblyAndGuardedAdapterAreActive()
    {
        var active = LoadedModManager.RunningModsListForReading.Select(mod => mod.PackageId).ToArray();
        IntegrationAssert.Equal(
            string.Join("|", ExpectedPackages).ToLowerInvariant(),
            string.Join("|", active).ToLowerInvariant(),
            "The Replimat sanitation group must use the complete exact active package sequence.");

        var terminalType = AccessTools.TypeByName(ReplimatCompatibility.TerminalTypeName);
        IntegrationAssert.Equal(
            "Replimat",
            terminalType?.Assembly.GetName().Name,
            "Replimat must expose the required types from its own assembly.");
        IntegrationAssert.True(
            ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.Replimat) &&
            ReplimatAdapter.Enabled,
            "The exact Replimat plus Replimat Meals chain must pass its passive shape gate.");
        IntegrationAssert.Equal(
            0,
            MealClassificationRuntime.ValidationFailures.Count,
            "The complete Replimat Meals registry must validate before the adapter activates.");
        IntegrationAssert.True(
            ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.DubsBadHygiene) &&
            ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.CommonSense) &&
            CommonSenseAdapter.Enabled,
            "The sanitation companions and validated Common Sense adapter must remain active in the maintained group.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void NativeDispenserOwnerAndImmersivePostfixComposeExactlyOnce()
    {
        var terminalType = AccessTools.TypeByName(ReplimatCompatibility.TerminalTypeName);
        var utilityType = AccessTools.TypeByName(ReplimatCompatibility.UtilityTypeName);
        var prefixType = AccessTools.TypeByName(ReplimatCompatibility.ToilPrefixTypeName);
        IntegrationAssert.NotNull(terminalType, "The exact Replimat terminal type must exist.");
        IntegrationAssert.NotNull(utilityType, "The exact Replimat meal-selection utility must exist.");
        IntegrationAssert.NotNull(prefixType, "The exact Replimat dispenser-toil patch type must exist.");

        var dispense = terminalType!.GetMethod(
            "TryDispenseFood",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            new[] { typeof(Pawn), typeof(Pawn), typeof(ThingDef), typeof(int) },
            null);
        var pickMeal = utilityType!.GetMethod(
            "PickMeal",
            BindingFlags.Public | BindingFlags.Static,
            null,
            new[] { typeof(Pawn), typeof(Pawn) },
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
        IntegrationAssert.NotNull(dispense, "The exact four-argument native Replimat dispense method must exist.");
        IntegrationAssert.Equal(
            typeof(ThingDef),
            pickMeal?.ReturnType,
            "The exact native Replimat meal picker must retain its two-pawn ThingDef shape.");
        IntegrationAssert.NotNull(prefix, "The exact native Replimat toil prefix must exist.");
        IntegrationAssert.NotNull(takeMeal, "RimWorld's dispenser ingestion boundary must exist.");

        IntegrationAssert.Equal(
            1,
            Harmony.GetPatchInfo(takeMeal!)?.Prefixes.Count(patch =>
                patch.owner == ReplimatCompatibility.UpstreamPatchOwner &&
                patch.PatchMethod == prefix) ?? 0,
            "Replimat must remain the sole owner of its validated native dispenser toil prefix.");
        IntegrationAssert.Equal(
            1,
            Harmony.GetPatchInfo(dispense!)?.Prefixes.Count(patch =>
                patch.owner == ImmersiveChefsMod.PackageId &&
                patch.PatchMethod?.DeclaringType == typeof(ReplimatAdapter)) ?? 0,
            "Immersive Chefs must pin exactly one native Replimat selection before dispensing.");
        IntegrationAssert.Equal(
            1,
            Harmony.GetPatchInfo(dispense!)?.Postfixes.Count(patch =>
                patch.owner == ImmersiveChefsMod.PackageId &&
                patch.PatchMethod?.DeclaringType == typeof(ReplimatAdapter)) ?? 0,
            "Immersive Chefs must attach exactly one post-success plate-binding postfix.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void FinalizedTerminalsAndAllFortyEightMealsRetainUpstreamOwnership()
    {
        var terminalType = AccessTools.TypeByName(ReplimatCompatibility.TerminalTypeName);
        foreach (var defName in new[] { "ReplimatTerminal", "ReplimatTerminalWall" })
        {
            var terminal = DefDatabase<ThingDef>.GetNamed(defName);
            IntegrationAssert.Equal(
                "sumghai.replimat",
                terminal.modContentPack?.PackageId.ToLowerInvariant(),
                defName + " must remain owned by Replimat.");
            IntegrationAssert.Equal(
                terminalType,
                terminal.thingClass,
                defName + " must retain the validated native terminal class.");
        }

        IntegrationAssert.Equal(
            48,
            MealClassificationCatalog.ReplimatMealDefNames.Count,
            "The installed Replimat Meals contract must contain exactly 48 products.");
        foreach (var defName in MealClassificationCatalog.ReplimatMealDefNames)
        {
            var meal = DefDatabase<ThingDef>.GetNamed(defName);
            IntegrationAssert.Equal(
                "sumghai.replimatmeals",
                meal.modContentPack?.PackageId.ToLowerInvariant(),
                defName + " must remain owned by Replimat Meals.");
            IntegrationAssert.NotNull(
                MealClassificationRuntime.ClassifyMeal(meal),
                defName + " must retain its explicit Simple/Fine/Lavish service tier.");
            IntegrationAssert.Equal(
                1,
                meal.comps.Count(properties => properties.compClass == typeof(CompEmbeddedWare)),
                defName + " must receive exactly one embedded-ware lifecycle.");
            IntegrationAssert.Equal(
                1,
                meal.comps.Count(properties => properties.compClass == typeof(CompCulinaryState)),
                defName + " must receive exactly one culinary lifecycle.");
        }
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void AnimalFeederAndSurvivalBatchRemainNativeHandheldExclusions()
    {
        var terminalType = AccessTools.TypeByName(ReplimatCompatibility.TerminalTypeName);
        var feederType = AccessTools.TypeByName("Replimat.Building_ReplimatAnimalFeeder");
        IntegrationAssert.NotNull(terminalType, "The exact Replimat terminal type must exist.");
        IntegrationAssert.NotNull(feederType, "The exact Replimat animal-feeder type must exist.");
        IntegrationAssert.Equal(
            terminalType!.Assembly,
            feederType!.Assembly,
            "The animal feeder must retain ownership in the validated Replimat assembly.");

        var feederDef = DefDatabase<ThingDef>.GetNamed("ReplimatAnimalFeeder");
        IntegrationAssert.Equal(
            "sumghai.replimat",
            feederDef.modContentPack?.PackageId.ToLowerInvariant(),
            "The animal feeder Def must remain owned by Replimat.");
        IntegrationAssert.Equal(
            feederType,
            feederDef.thingClass,
            "The animal feeder Def must retain its separate native building class.");

        var animalFeedDefs = feederDef.building.fixedStorageSettings.filter.AllowedThingDefs
            .OrderBy(def => def.defName, StringComparer.Ordinal)
            .ToArray();
        IntegrationAssert.Equal(
            "Hay|Kibble|Replimat_Synthmeat",
            string.Join("|", animalFeedDefs.Select(def => def.defName)),
            "The inspected feeder must retain only its three native loose animal-feed products.");
        foreach (var animalFeedDef in animalFeedDefs)
        {
            IntegrationAssert.True(
                !MealCoveragePolicy.IsCovered(animalFeedDef),
                animalFeedDef.defName + " must remain outside the plated-meal workflow.");
            IntegrationAssert.Equal(
                0,
                animalFeedDef.comps.Count(properties => properties.compClass == typeof(CompEmbeddedWare)),
                animalFeedDef.defName + " must not receive embedded tableware.");
            IntegrationAssert.Equal(
                0,
                animalFeedDef.comps.Count(properties => properties.compClass == typeof(CompCulinaryState)),
                animalFeedDef.defName + " must not receive culinary or temperature state.");
        }

        var feederTick = feederType.GetMethod(
            "Tick",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null);
        var toggleAnimalFeed = feederType.GetMethod(
            "ToggleAnimalFeedDef",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null);
        IntegrationAssert.NotNull(feederTick, "The native animal-feeder Tick boundary must exist.");
        IntegrationAssert.NotNull(toggleAnimalFeed, "The native animal-feed selector must exist.");
        IntegrationAssert.Equal(
            0,
            CountImmersiveChefsPatches(feederTick!),
            "Immersive Chefs must not patch native animal-feed production.");
        IntegrationAssert.Equal(
            0,
            CountImmersiveChefsPatches(toggleAnimalFeed!),
            "Immersive Chefs must not patch the native animal-feed selector.");

        var beginSurvivalBatch = terminalType.GetMethod(
            "TryBatchMakingSurvivalMeals",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            Type.EmptyTypes,
            null);
        var confirmSurvivalBatch = terminalType.GetMethod(
            "ConfirmAction",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            new[] { typeof(int), typeof(ThingDef), typeof(float) },
            null);
        IntegrationAssert.NotNull(
            beginSurvivalBatch,
            "The separate native survival-batch gizmo boundary must exist.");
        IntegrationAssert.NotNull(
            confirmSurvivalBatch,
            "The separate native survival-batch confirmation boundary must exist.");
        IntegrationAssert.Equal(
            0,
            CountImmersiveChefsPatches(beginSurvivalBatch!),
            "Immersive Chefs must not patch survival-batch dialog creation.");
        IntegrationAssert.Equal(
            0,
            CountImmersiveChefsPatches(confirmSurvivalBatch!),
            "Immersive Chefs must not patch survival-batch production.");

        var survivalDialogType = AccessTools.TypeByName("Replimat.Dialog_BatchMakeSurvivalMeals");
        IntegrationAssert.NotNull(
            survivalDialogType,
            "The native survival-batch dialog type must exist.");
        IntegrationAssert.Equal(
            terminalType.Assembly,
            survivalDialogType!.Assembly,
            "The survival-batch dialog must retain the validated Replimat assembly identity.");
        IntegrationAssert.Equal(
            typeof(Action<int, ThingDef>),
            survivalDialogType.GetField(
                "confirmAction",
                BindingFlags.NonPublic | BindingFlags.Instance)?.FieldType,
            "The native dialog must retain its exact two-argument confirmation callback.");
        IntegrationAssert.Equal(
            typeof(int),
            survivalDialogType.GetField(
                "curValue",
                BindingFlags.NonPublic | BindingFlags.Instance)?.FieldType,
            "The native dialog must retain its exact batch-count field.");
        IntegrationAssert.Equal(
            typeof(ThingDef),
            survivalDialogType.GetField(
                "selectedSurvivalMealType",
                BindingFlags.Public | BindingFlags.Instance)?.FieldType,
            "The native dialog must retain its exact selected-product field.");

        var survivalMeal = DefDatabase<ThingDef>.GetNamed("MealSurvivalPack");
        IntegrationAssert.Equal(
            "ludeon.rimworld",
            survivalMeal.modContentPack?.PackageId.ToLowerInvariant(),
            "The packaged survival meal must remain owned by Core.");
        IntegrationAssert.True(
            !MealCoveragePolicy.IsCovered(survivalMeal),
            "The packaged survival meal must remain a handheld exclusion.");
        IntegrationAssert.Equal(
            0,
            survivalMeal.comps.Count(properties => properties.compClass == typeof(CompEmbeddedWare)),
            "The packaged survival meal must not receive embedded tableware.");
        IntegrationAssert.Equal(
            0,
            survivalMeal.comps.Count(properties => properties.compClass == typeof(CompCulinaryState)),
            "The packaged survival meal must not receive culinary or temperature state.");
    }

    private static int CountImmersiveChefsPatches(MethodBase method)
    {
        var patchInfo = Harmony.GetPatchInfo(method);
        if (patchInfo is null)
        {
            return 0;
        }

        return patchInfo.Prefixes.Count(IsImmersiveChefsPatch) +
               patchInfo.Postfixes.Count(IsImmersiveChefsPatch) +
               patchInfo.Transpilers.Count(IsImmersiveChefsPatch) +
               patchInfo.Finalizers.Count(IsImmersiveChefsPatch);
    }

    private static bool IsImmersiveChefsPatch(Patch patch) =>
        patch.owner == ImmersiveChefsMod.PackageId;
}
