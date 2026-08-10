using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using RimWorldDevGateway.IntegrationTesting;
using Verse;
using Verse.AI;

namespace ImmersiveChefs.CookForYourself.InGame.IntegrationTests;

public static class CookForYourselfIntegrationTests
{
    private static readonly string[] ExactPackages =
    {
        "brrainz.harmony",
        "ludeon.rimworld",
        "lordfelix.CookForYourself",
        "fumblesneeze.immersivechefs",
        "fumblesneeze.rimworlddevgateway"
    };

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ExactOptionalAssemblyAndFinalizedJobDefAreActive()
    {
        var active = LoadedModManager.RunningModsListForReading
            .Select(mod => mod.PackageId)
            .ToArray();
        var driverType = AccessTools.TypeByName(CookForYourselfCompatibility.DriverTypeName);
        var jobDef = DefDatabase<JobDef>.GetNamedSilentFail(CookForYourselfCompatibility.JobDefName);

        IntegrationAssert.Equal(
            string.Join("|", ExactPackages).ToLowerInvariant(),
            string.Join("|", active).ToLowerInvariant(),
            "The integration group must use the complete exact active package sequence.");
        IntegrationAssert.NotNull(driverType, "The audited custom job driver must be loaded.");
        IntegrationAssert.Equal(
            "CookForYourself, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null",
            driverType!.Assembly.FullName,
            "Cook for Yourself must retain the audited assembly identity.");
        IntegrationAssert.Equal(
            CookForYourselfCompatibility.ModuleVersionId,
            driverType.Module.ModuleVersionId,
            "Cook for Yourself must retain the audited module identity.");
        IntegrationAssert.NotNull(jobDef, "The one-off cooking JobDef must be finalized.");
        IntegrationAssert.Equal(
            driverType,
            jobDef!.driverClass,
            "The finalized one-off JobDef must retain the audited custom driver.");
        IntegrationAssert.True(
            ImmersiveChefsMod.IsIntegrationEnabled(OptionalIntegration.CookForYourself) &&
            CookForYourselfAdapter.Enabled,
            "The exact package and runtime-shape gates must activate the adapter.");
        IntegrationAssert.True(
            typeof(ImmersiveChefsMod).Assembly.GetReferencedAssemblies()
                .All(reference => reference.Name != CookForYourselfCompatibility.AssemblyName),
            "The product assembly must not hard-reference the optional assembly.");
    }

    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void ExactJobGiverFinalizersAndDriverPostfixAreOwnedOnce()
    {
        var selfType = AccessTools.TypeByName(CookForYourselfCompatibility.SelfJobGiverTypeName);
        var dependentType = AccessTools.TypeByName(CookForYourselfCompatibility.DependentJobGiverTypeName);
        var driverType = AccessTools.TypeByName(CookForYourselfCompatibility.DriverTypeName);
        var selfMethod = ExactMethod(selfType, "TryGiveJob", typeof(Job), typeof(Pawn));
        var dependentMethod = ExactMethod(dependentType, "TryGiveJob", typeof(Job), typeof(Pawn));
        var makeToils = ExactMethod(driverType, "MakeNewToils", typeof(IEnumerable<Toil>));

        IntegrationAssert.NotNull(selfMethod, "The audited self job-giver method must exist.");
        IntegrationAssert.NotNull(dependentMethod, "The audited dependent job-giver method must exist.");
        IntegrationAssert.NotNull(makeToils, "The audited custom-driver toil method must exist.");
        AssertOneLastFinalizer(selfMethod!, "self job giver");
        AssertOneLastFinalizer(dependentMethod!, "dependent job giver");
        IntegrationAssert.Equal(
            1,
            Harmony.GetPatchInfo(makeToils!)?.Postfixes.Count(patch =>
                patch.owner == ImmersiveChefsMod.PackageId) ?? 0,
            "The custom driver must have exactly one Immersive Chefs toil postfix.");
    }

    private static MethodInfo? ExactMethod(
        Type? type,
        string name,
        Type returnType,
        params Type[] parameters)
    {
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic |
                                   BindingFlags.Public | BindingFlags.DeclaredOnly;
        var method = type?.GetMethod(name, flags, null, parameters, null);
        return method?.ReturnType == returnType ? method : null;
    }

    private static void AssertOneLastFinalizer(MethodBase method, string seam)
    {
        var finalizers = Harmony.GetPatchInfo(method)?.Finalizers
            .Where(patch => patch.owner == ImmersiveChefsMod.PackageId)
            .ToArray() ?? Array.Empty<Patch>();
        IntegrationAssert.Equal(
            1,
            finalizers.Length,
            $"The {seam} must have exactly one Immersive Chefs admission finalizer.");
        IntegrationAssert.Equal(
            Priority.Last,
            finalizers[0].priority,
            $"The {seam} admission must observe the surviving result after ordinary finalizers.");
    }
}
