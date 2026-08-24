using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GuestBedGizmo.Compatibility.Hospitality;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GuestBedGizmo.Beds;

internal static class GuestBedGizmoPatchInstaller
{
    private static readonly MethodInfo Target = AccessTools.Method(
        typeof(Building_Bed),
        nameof(Building_Bed.GetGizmos));

    private static readonly MethodInfo Postfix = AccessTools.Method(
        typeof(GuestBedGizmoPatchInstaller),
        nameof(ProjectGizmos));

    private static HospitalityRuntimeAdapter? adapter;

    internal static bool TryInstall(
        Harmony harmony,
        HospitalityRuntimeAdapter supportedAdapter,
        out string? failure)
    {
        return PatchInstallTransaction.TryApply(
            () =>
            {
                GuestBedOwnerAssignmentPatch.Configure(supportedAdapter);
                adapter = supportedAdapter;
                ApplyPatches(harmony);
            },
            () =>
            {
                adapter = null;
                try
                {
                    harmony.UnpatchAll(GuestBedGizmoMod.PackageId);
                }
                finally
                {
                    GuestBedOwnerAssignmentPatch.Configure(null);
                }
            },
            out failure);
    }

    private static void ApplyPatches(Harmony harmony)
    {
        if (Harmony.GetPatchInfo(Target)?.Postfixes.Any(patch =>
                patch.owner == GuestBedGizmoMod.PackageId && patch.PatchMethod == Postfix) != true)
        {
            var postfix = new HarmonyMethod(Postfix)
            {
                after = new[]
                {
                    HospitalityRuntimeContract.HarmonyOwner,
                    HospitalityPackagePolicy.PackageId,
                },
            };
            harmony.Patch(Target, postfix: postfix);
        }

        if (Harmony.GetPatchInfo(GuestBedOwnerAssignmentPatch.OwnerInterfaceMethod)?.Transpilers.Any(patch =>
                patch.owner == GuestBedGizmoMod.PackageId &&
                patch.PatchMethod == GuestBedOwnerAssignmentPatch.TranspilerMethod) != true)
        {
            harmony.Patch(
                GuestBedOwnerAssignmentPatch.OwnerInterfaceMethod,
                transpiler: new HarmonyMethod(GuestBedOwnerAssignmentPatch.TranspilerMethod));
        }

        if (Harmony.GetPatchInfo(GuestBedOwnerAssignmentPatch.CommitMethod)?.Prefixes.Any(patch =>
                patch.owner == GuestBedGizmoMod.PackageId &&
                patch.PatchMethod == GuestBedOwnerAssignmentPatch.CommitPrefixMethod) != true)
        {
            harmony.Patch(
                GuestBedOwnerAssignmentPatch.CommitMethod,
                prefix: new HarmonyMethod(GuestBedOwnerAssignmentPatch.CommitPrefixMethod));
        }
    }

    private static void ProjectGizmos(Building_Bed __instance, ref IEnumerable<Gizmo> __result)
    {
        if (adapter != null)
        {
            __result = GuestBedGizmoGizmoProjection.Project(__instance, __result, adapter);
        }
    }
}
