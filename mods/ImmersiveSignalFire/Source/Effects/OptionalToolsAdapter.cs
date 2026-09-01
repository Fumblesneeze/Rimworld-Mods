using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ImmersiveSignalFire.Buildings;
using UnityEngine;
using Verse;

namespace ImmersiveSignalFire.Effects;

internal static class OptionalToolsAdapter
{
    private const string PackageId = "meathax.showmeyourtools";
    private static bool enabled;
    private static Material? blanketMaterial;

    internal static bool Enabled => enabled;

    public static void Initialize()
    {
        ModContentPack? package = LoadedModManager.RunningModsListForReading.FirstOrDefault(
            mod => string.Equals(mod.PackageId, PackageId, StringComparison.OrdinalIgnoreCase) ||
                   string.Equals(mod.PackageIdPlayerFacing, PackageId, StringComparison.OrdinalIgnoreCase));
        if (package is null)
        {
            return;
        }

        enabled = ResolveEnabled(
            packageActive: true,
            HasExpectedMarker(package.assemblies.loadedAssemblies));
        if (!enabled)
        {
            Log.Warning("[Immersive Signal Fire] Show Me Your Tools is active but its JobEffects.JobToolDef marker is missing; synchronized blankets are disabled.");
            return;
        }

        blanketMaterial = MaterialPool.MatFrom(
            "ImmersiveSignalFire/Effects/SignalBlanket",
            ShaderDatabase.Cutout);
    }

    internal static bool ResolveEnabled(bool packageActive, bool expectedMarkerPresent) =>
        packageActive && expectedMarkerPresent;

    internal static bool HasExpectedMarker(IEnumerable<Assembly> packageAssemblies) =>
        packageAssemblies.Any(assembly =>
            assembly?.GetType("JobEffects.JobToolDef", throwOnError: false, ignoreCase: false) is not null);

    public static void DrawBlankets(CompSignalFire comp)
    {
        if (!enabled || blanketMaterial is null || !comp.IsActive)
        {
            return;
        }

        bool raised = MorseCadence.IsSmokeOn(comp.ActiveElapsedTicks);
        Vector3 firePosition = comp.parent.DrawPos;
        foreach (Pawn pawn in comp.ActiveParticipants.Where(pawn => pawn.Spawned))
        {
            Vector3 delta = firePosition - pawn.DrawPos;
            float distance = Math.Max(0.01f, delta.MagnitudeHorizontal());
            Vector3 direction = delta / distance;
            float reach = Math.Min(distance * (raised ? 0.38f : 0.62f), raised ? 0.72f : 0.95f);
            Vector3 center = pawn.DrawPos + direction * reach;
            center.y = AltitudeLayer.MoteOverhead.AltitudeFor() + (raised ? 0.18f : 0.04f);
            float angle = delta.AngleFlat();
            Matrix4x4 matrix = Matrix4x4.TRS(
                center,
                Quaternion.AngleAxis(angle, Vector3.up),
                new Vector3(0.42f, 1f, raised ? 0.78f : 0.92f));
            Graphics.DrawMesh(MeshPool.plane10, matrix, blanketMaterial, 0);
        }
    }
}
