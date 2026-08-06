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
        "ludeon.rimworld",
        "brrainz.harmony",
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
            "Replimat, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            terminalType?.Assembly.FullName,
            "Replimat must retain the inspected 1.6 assembly identity.");
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
            ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.CommonSense),
            "The sanitation companions must remain active in the maintained group.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void NativeDispenserOwnerAndImmersivePostfixComposeExactlyOnce()
    {
        var terminalType = AccessTools.TypeByName(ReplimatCompatibility.TerminalTypeName);
        var prefixType = AccessTools.TypeByName(ReplimatCompatibility.ToilPrefixTypeName);
        IntegrationAssert.NotNull(terminalType, "The exact Replimat terminal type must exist.");
        IntegrationAssert.NotNull(prefixType, "The exact Replimat dispenser-toil patch type must exist.");

        var dispense = terminalType!.GetMethod(
            "TryDispenseFood",
            BindingFlags.Public | BindingFlags.Instance,
            null,
            new[] { typeof(Pawn), typeof(Pawn), typeof(ThingDef), typeof(int) },
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
}
