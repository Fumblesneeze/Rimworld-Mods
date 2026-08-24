using System;
using System.Linq;
using System.Threading;
using GuestBedGizmo.Beds;
using GuestBedGizmo.Compatibility.Hospitality;
using HarmonyLib;
using Verse;

namespace GuestBedGizmo;

public sealed class GuestBedGizmoMod : Mod
{
    public const string PackageId = "fumblesneeze.guestbedgizmo";

    private static int initialized;

    public GuestBedGizmoMod(ModContentPack content) : base(content)
    {
        if (Interlocked.Exchange(ref initialized, 1) != 0)
        {
            return;
        }

        string[] activePackages = LoadedModManager.RunningModsListForReading
            .Select(mod => mod.PackageId)
            .ToArray();
        if (!HospitalityPackagePolicy.ShouldActivate(activePackages))
        {
            IntegrationStatus = "prerequisites_absent";
            return;
        }

        if (!HospitalityRuntimeAdapter.TryResolve(
                AppDomain.CurrentDomain.GetAssemblies(),
                out HospitalityRuntimeAdapter? adapter,
                out string? failure) ||
            adapter == null)
        {
            IntegrationStatus = $"hospitality_incompatible: {failure}";
            Log.Warning(
                $"[Guest Bed Gizmo] Hospitality integration disabled because its supported RimWorld 1.6 shape changed: {failure}.");
            return;
        }

        IntegrationStatus = "hospitality_pending";
        LongEventHandler.ExecuteWhenFinished(() => InitializeAfterContentLoad(adapter));
    }

    private static void InitializeAfterContentLoad(HospitalityRuntimeAdapter adapter)
    {
        if (!adapter.TryValidateUiAssets(out string? failure))
        {
            IntegrationStatus = $"hospitality_incompatible: {failure}";
            Log.Warning(
                $"[Guest Bed Gizmo] Hospitality integration disabled because its supported RimWorld 1.6 UI shape changed: {failure}.");
            return;
        }

        var harmony = new Harmony(PackageId);
        if (!GuestBedGizmoPatchInstaller.TryInstall(harmony, adapter, out string? patchFailure))
        {
            IntegrationStatus = $"hospitality_incompatible: {patchFailure}";
            Log.Warning(
                $"[Guest Bed Gizmo] Hospitality integration disabled and rolled back because the supported RimWorld 1.6 patch seam changed: {patchFailure}.");
            return;
        }

        HarmonyInstance = harmony;
        Adapter = adapter;
        IntegrationStatus = "hospitality_supported";
    }

    public static bool IntegrationActive => Adapter != null;

    public static string IntegrationStatus { get; private set; } = "not_initialized";

    internal static HospitalityRuntimeAdapter? Adapter { get; private set; }

    internal static Harmony? HarmonyInstance { get; private set; }
}
