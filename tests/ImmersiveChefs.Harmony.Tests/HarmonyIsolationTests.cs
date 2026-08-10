using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using HarmonyLib;
using NUnit.Framework;
using RimWorld;
using UnityEngine;
using Verse;

namespace ImmersiveChefs.Harmony.Tests;

[TestFixture]
[NonParallelizable]
public sealed class HarmonyIsolationTests
{
    [Test]
    public void Explicit_patch_scope_changes_only_the_scoped_call_and_restores_the_original()
    {
        var target = typeof(HarmonyProbe).GetMethod(nameof(HarmonyProbe.Read), BindingFlags.Public | BindingFlags.Static)!;
        var postfix = typeof(HarmonyProbePatch).GetMethod(nameof(HarmonyProbePatch.Postfix), BindingFlags.Public | BindingFlags.Static)!;

        Assert.That(HarmonyProbe.Read(), Is.EqualTo("original"));

        using (HarmonyPatchScope.ApplyPostfix("fumblesneeze.immersivechefs.tests.patch-scope", target, postfix))
        {
            Assert.Multiple(() =>
            {
                Assert.That(HarmonyProbe.Read(), Is.EqualTo("patched"));
                Assert.That(
                    HarmonyLib.Harmony.GetPatchInfo(target)?.Postfixes.Count(patch =>
                        patch.owner == "fumblesneeze.immersivechefs.tests.patch-scope"),
                    Is.EqualTo(1));
            });
        }

        Assert.Multiple(() =>
        {
            Assert.That(HarmonyProbe.Read(), Is.EqualTo("original"));
            Assert.That(
                HarmonyLib.Harmony.GetPatchInfo(target)?.Owners.Contains("fumblesneeze.immersivechefs.tests.patch-scope") ?? false,
                Is.False);
        });
    }

    [Test]
    public void Patch_scope_removes_its_owner_when_the_test_body_throws()
    {
        const string ownerId = "fumblesneeze.immersivechefs.tests.throwing-patch-scope";
        var target = typeof(HarmonyProbe).GetMethod(nameof(HarmonyProbe.Read), BindingFlags.Public | BindingFlags.Static)!;
        var postfix = typeof(HarmonyProbePatch).GetMethod(nameof(HarmonyProbePatch.Postfix), BindingFlags.Public | BindingFlags.Static)!;

        Assert.That(
            () =>
            {
                using (HarmonyPatchScope.ApplyPostfix(ownerId, target, postfix))
                {
                    Assert.That(HarmonyProbe.Read(), Is.EqualTo("patched"));
                    throw new DeliberateTestBodyException();
                }
            },
            Throws.TypeOf<DeliberateTestBodyException>());

        Assert.Multiple(() =>
        {
            Assert.That(HarmonyProbe.Read(), Is.EqualTo("original"));
            Assert.That(HarmonyLib.Harmony.HasAnyPatches(ownerId), Is.False);
        });
    }

    [Test]
    public void Runtime_harmony_copy_matches_the_explicit_configured_source()
    {
        var testAssembly = typeof(HarmonyIsolationTests).Assembly;
        var sourcePath = testAssembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "HarmonyAssemblyPath")
            .Value;
        var runtimeAssembly = typeof(HarmonyLib.Harmony).Assembly;
        var sourceIdentity = AssemblyName.GetAssemblyName(sourcePath);
        var sourceHash = ComputeSha256(sourcePath);
        var runtimeHash = ComputeSha256(runtimeAssembly.Location);

        Assert.Multiple(() =>
        {
            Assert.That(sourceIdentity.Name, Is.EqualTo("0Harmony"));
            Assert.That(runtimeAssembly.GetName().FullName, Is.EqualTo(sourceIdentity.FullName));
            Assert.That(runtimeHash, Is.EqualTo(sourceHash));
            Assert.That(runtimeAssembly.ManifestModule.ModuleVersionId, Is.Not.EqualTo(Guid.Empty));
        });

        TestContext.Progress.WriteLine(
            $"Harmony source={sourcePath}; version={sourceIdentity.Version}; " +
            $"sha256={sourceHash}; mvid={runtimeAssembly.ManifestModule.ModuleVersionId:D}");
    }

    [Test]
    public void Failed_patch_acquisition_leaves_no_owner_or_changed_behavior()
    {
        const string ownerId = "fumblesneeze.immersivechefs.tests.failed-patch-acquisition";
        var target = typeof(HarmonyProbe).GetMethod(nameof(HarmonyProbe.Read), BindingFlags.Public | BindingFlags.Static)!;
        var invalidPostfix = typeof(HarmonyProbePatch).GetMethod(
            nameof(HarmonyProbePatch.InvalidPostfix),
            BindingFlags.Public | BindingFlags.Static)!;

        Assert.That(
            () => HarmonyPatchScope.ApplyPostfix(ownerId, target, invalidPostfix),
            Throws.Exception);
        Assert.Multiple(() =>
        {
            Assert.That(HarmonyProbe.Read(), Is.EqualTo("original"));
            Assert.That(HarmonyLib.Harmony.HasAnyPatches(ownerId), Is.False);
        });
    }

    [Test]
    public void Failed_cleanup_can_be_retried_before_the_scope_becomes_disposed()
    {
        var attempts = 0;
        var scope = HarmonyPatchScope.CreateCleanupProbe(() =>
        {
            attempts++;
            if (attempts == 1)
            {
                throw new DeliberateCleanupException();
            }
        });

        Assert.That(() => scope.Dispose(), Throws.TypeOf<DeliberateCleanupException>());
        Assert.That(() => scope.Dispose(), Throws.Nothing);
        scope.Dispose();

        Assert.That(attempts, Is.EqualTo(2));
    }

    [Test]
    public void Imported_meal_food_selection_patches_bind_to_the_real_rimworld_signatures()
    {
        var productAssembly = typeof(ImmersiveChefsMod).Assembly;
        var diningPatch = productAssembly
            .GetType("ImmersiveChefs.ImportedMealDiningGatePatch", throwOnError: true)!
            .GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static)!;
        var optimalityPatch = productAssembly
            .GetType("ImmersiveChefs.PlatedMealFoodOptimalityPatch", throwOnError: true)!
            .GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static)!;
        var diningBoundary = AccessTools.Method(
            typeof(RimWorld.FoodUtility),
            "IsFoodSourceOnMapSociallyProper",
            new[] { typeof(Verse.Thing), typeof(Verse.Pawn), typeof(Verse.Pawn), typeof(bool) });
        var optimalityBoundary = AccessTools.Method(
            typeof(RimWorld.FoodUtility),
            "FoodOptimality",
            new[] { typeof(Verse.Pawn), typeof(Verse.Thing), typeof(Verse.ThingDef), typeof(float), typeof(bool) });

        Assert.That(diningBoundary, Is.Not.Null);
        Assert.That(optimalityBoundary, Is.Not.Null);
        Assert.That(
            () =>
            {
                using (HarmonyPatchScope.ApplyPostfix(
                           "fumblesneeze.immersivechefs.tests.imported-dining-gate",
                           diningBoundary!,
                           diningPatch))
                {
                }

                using (HarmonyPatchScope.ApplyPostfix(
                           "fumblesneeze.immersivechefs.tests.plated-optimality",
                           optimalityBoundary!,
                           optimalityPatch))
                {
                }
            },
            Throws.Nothing);
    }

    [Test]
    public void Generated_meal_plating_patches_bind_to_inventory_and_trader_stock_boundaries()
    {
        var productAssembly = typeof(ImmersiveChefsMod).Assembly;
        var pawnPostfix = productAssembly
            .GetType("ImmersiveChefs.GeneratedPawnInventoryMealPlatingPatch", throwOnError: true)!
            .GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static)!;
        var traderPostfix = productAssembly
            .GetType("ImmersiveChefs.GeneratedTraderStockMealPlatingPatch", throwOnError: true)!
            .GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static)!;
        var pawnBoundary = AccessTools.Method(
            typeof(RimWorld.PawnInventoryGenerator),
            nameof(RimWorld.PawnInventoryGenerator.GenerateInventoryFor),
            new[] { typeof(Verse.Pawn), typeof(Verse.PawnGenerationRequest) });
        var traderBoundary = AccessTools.Method(
            typeof(RimWorld.ThingSetMaker_TraderStock),
            "Generate",
            new[] { typeof(RimWorld.ThingSetMakerParams), typeof(List<Verse.Thing>) });

        Assert.Multiple(() =>
        {
            Assert.That(pawnBoundary, Is.Not.Null);
            Assert.That(traderBoundary, Is.Not.Null);
        });
        Assert.That(
            () =>
            {
                using (HarmonyPatchScope.ApplyPostfix(
                           "fumblesneeze.immersivechefs.tests.generated-pawn-meals",
                           pawnBoundary!,
                           pawnPostfix))
                {
                }

                using (HarmonyPatchScope.ApplyPostfix(
                           "fumblesneeze.immersivechefs.tests.generated-trader-meals",
                           traderBoundary!,
                           traderPostfix))
                {
                }
            },
            Throws.Nothing);
    }

    [Test]
    public void Prepared_food_bill_policy_patch_binds_to_the_real_ingredient_boundary()
    {
        var productAssembly = typeof(ImmersiveChefsMod).Assembly;
        var policyPatch = productAssembly
            .GetType("ImmersiveChefs.PreparedFoodBillIngredientPatch", throwOnError: true)!
            .GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static)!;
        var ingredientBoundary = AccessTools.Method(
            typeof(RimWorld.Bill),
            "IsFixedOrAllowedIngredient",
            new[] { typeof(Verse.Thing) });

        Assert.That(ingredientBoundary, Is.Not.Null);
        Assert.That(
            () =>
            {
                using (HarmonyPatchScope.ApplyPostfix(
                           "fumblesneeze.immersivechefs.tests.prepared-food-bill-policy",
                           ingredientBoundary!,
                           policyPatch))
                {
                }
            },
            Throws.Nothing);
    }

    [Test]
    public void Prepared_food_policy_patch_binds_to_the_real_thing_boundary()
    {
        var productAssembly = typeof(ImmersiveChefsMod).Assembly;
        var policyPatch = productAssembly
            .GetType("ImmersiveChefs.PreparedFoodPolicyPatch", throwOnError: true)!
            .GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static)!;
        var foodPolicyBoundary = AccessTools.Method(
            typeof(RimWorld.FoodPolicy),
            nameof(RimWorld.FoodPolicy.Allows),
            new[] { typeof(Verse.Thing) });

        Assert.That(foodPolicyBoundary, Is.Not.Null);
        Assert.That(
            () =>
            {
                using (HarmonyPatchScope.ApplyPostfix(
                           "fumblesneeze.immersivechefs.tests.prepared-food-policy",
                           foodPolicyBoundary!,
                           policyPatch))
                {
                }
            },
            Throws.Nothing);
    }

    [Test]
    public void Dirty_cookware_control_and_work_prop_patches_bind_to_real_player_boundaries()
    {
        var productAssembly = typeof(ImmersiveChefsMod).Assembly;
        var doBillPostfix = productAssembly
            .GetType("ImmersiveChefs.WorkGiverDoBillWarePatch", throwOnError: true)!
            .GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static)!;
        var floatMenuPostfix = productAssembly
            .GetType("ImmersiveChefs.DirtyCookwareFloatMenuPatch", throwOnError: true)!
            .GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static)!;
        var drawPostfix = productAssembly
            .GetType("ImmersiveChefs.CookingWorkPropDrawPatch", throwOnError: true)!
            .GetMethod("Postfix", BindingFlags.NonPublic | BindingFlags.Static)!;
        var doBillBoundary = AccessTools.Method(
            typeof(WorkGiver_DoBill),
            nameof(WorkGiver_DoBill.JobOnThing),
            new[] { typeof(Pawn), typeof(Thing), typeof(bool) });
        var floatMenuBoundary = AccessTools.Method(
            typeof(FloatMenuMakerMap),
            nameof(FloatMenuMakerMap.GetOptions),
            new[]
            {
                typeof(List<Pawn>),
                typeof(Vector3),
                typeof(FloatMenuContext).MakeByRefType()
            });
        var drawBoundary = AccessTools.Method(
            typeof(Pawn),
            "DrawAt",
            new[] { typeof(Vector3), typeof(bool) });

        Assert.Multiple(() =>
        {
            Assert.That(doBillBoundary, Is.Not.Null);
            Assert.That(floatMenuBoundary, Is.Not.Null);
            Assert.That(drawBoundary, Is.Not.Null);
        });
        Assert.That(
            () =>
            {
                using (HarmonyPatchScope.ApplyPostfix(
                           "fumblesneeze.immersivechefs.tests.dirty-cookware-do-bill",
                           doBillBoundary!,
                           doBillPostfix))
                {
                }

                using (HarmonyPatchScope.ApplyPostfix(
                           "fumblesneeze.immersivechefs.tests.dirty-cookware-float-menu",
                           floatMenuBoundary!,
                           floatMenuPostfix))
                {
                }

                using (HarmonyPatchScope.ApplyPostfix(
                           "fumblesneeze.immersivechefs.tests.cooking-work-prop",
                           drawBoundary!,
                           drawPostfix))
                {
                }
            },
            Throws.Nothing);
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        using var sha256 = SHA256.Create();
        return BitConverter.ToString(sha256.ComputeHash(stream)).Replace("-", string.Empty);
    }

    private static class HarmonyProbe
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static string Read() => "original";
    }

    private static class HarmonyProbePatch
    {
        public static void Postfix(ref string __result)
        {
            __result = "patched";
        }

        public static void InvalidPostfix(string parameterThatDoesNotExist)
        {
        }
    }

    private sealed class DeliberateTestBodyException : Exception
    {
    }

    private sealed class DeliberateCleanupException : Exception
    {
    }
}
