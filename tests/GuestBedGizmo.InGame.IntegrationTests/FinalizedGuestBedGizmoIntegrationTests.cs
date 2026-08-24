using System;
using System.Linq;
using HarmonyLib;
using RimWorld;
using RimWorldDevGateway.IntegrationTesting;
using Verse;

namespace GuestBedGizmo.InGame.IntegrationTests;

public static class FinalizedGuestBedGizmoIntegrationTests
{
    [IntegrationTest(RunAt.MainMenuLoaded)]
    public static void HospitalityShapeAndUnifiedOwnerPatchAreLiveExactlyOnce()
    {
        ThingDef guestBed = DefDatabase<ThingDef>.GetNamed("BedGuest");
        Type? hospitalityGuestBed = AppDomain.CurrentDomain.GetAssemblies()
            .Single(assembly => assembly.GetName().Name == "Hospitality")
            .GetType("Hospitality.Building_GuestBed", throwOnError: false);
        var target = AccessTools.Method(typeof(Building_Bed), nameof(Building_Bed.GetGizmos));
        Patches? patches = Harmony.GetPatchInfo(target);
        Patch[] productPostfixes = patches?.Postfixes
            .Where(patch => patch.owner == GuestBedGizmoMod.PackageId)
            .ToArray() ?? Array.Empty<Patch>();
        var ownerInterface = AccessTools.Method(
            typeof(Building_Bed),
            nameof(Building_Bed.SetBedOwnerTypeByInterface));
        Type ownerClosure = typeof(Building_Bed).GetNestedType(
            "<>c__DisplayClass55_0",
            System.Reflection.BindingFlags.NonPublic)!;
        var deferredCommit = AccessTools.Method(
            ownerClosure,
            "<SetBedOwnerTypeByInterface>b__0");
        Patch[] ownerTranspilers = Harmony.GetPatchInfo(ownerInterface)?.Transpilers
            .Where(patch => patch.owner == GuestBedGizmoMod.PackageId)
            .ToArray() ?? Array.Empty<Patch>();
        Patch[] commitPrefixes = Harmony.GetPatchInfo(deferredCommit)?.Prefixes
            .Where(patch => patch.owner == GuestBedGizmoMod.PackageId)
            .ToArray() ?? Array.Empty<Patch>();

        IntegrationAssert.True(GuestBedGizmoMod.IntegrationActive,
            "The exact loaded Hospitality shape must activate Guest Bed Gizmo.");
        IntegrationAssert.Equal("hospitality_supported", GuestBedGizmoMod.IntegrationStatus,
            "The selected runtime strategy must remain explicit.");
        IntegrationAssert.NotNull(hospitalityGuestBed,
            "Hospitality must expose its inspected public guest-bed runtime type.");
        IntegrationAssert.Equal(hospitalityGuestBed, guestBed.thingClass,
            "Hospitality's finalized BedGuest Def must use the inspected guest-bed runtime type.");
        IntegrationAssert.Equal(1, productPostfixes.Length,
            "Guest Bed Gizmo must own exactly one Building_Bed.GetGizmos postfix.");
        IntegrationAssert.Equal(1, ownerTranspilers.Length,
            "Guest Bed Gizmo must prepare the exact deferred vanilla owner assignment once.");
        IntegrationAssert.Equal(1, commitPrefixes.Length,
            "Guest Bed Gizmo must convert guest beds only at the deferred vanilla commit once.");
        IntegrationAssert.True(productPostfixes[0].after.Contains("Orion.Hospitality"),
            "The unified projection must run after Hospitality's legacy gizmo postfix.");
        IntegrationAssert.True(patches?.Postfixes.Any(patch => patch.owner == "Orion.Hospitality") == true,
            "The real Hospitality postfix must be active in the exact loader process.");
        IntegrationAssert.False(typeof(GuestBedGizmoMod).Assembly.GetReferencedAssemblies()
                .Any(reference => (reference.Name ?? string.Empty).StartsWith("RimWorldDevGateway", StringComparison.Ordinal)),
            "The product assembly must not reference the development Gateway.");
    }
}
