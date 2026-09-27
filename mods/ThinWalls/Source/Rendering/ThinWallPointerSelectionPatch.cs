using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using ThinWalls.Geometry;
using UnityEngine;
using Verse;

namespace ThinWalls.Rendering;

/// <summary>Correct only Thin candidates; let Core keep eligibility and selection cycling.</summary>
[HarmonyPatch(typeof(GenUI), nameof(GenUI.ThingsUnderMouse),
    new[] { typeof(Vector3), typeof(float), typeof(TargetingParameters), typeof(ITargetingSource) })]
public static class ThinWallPointerSelectionPatch
{
    [HarmonyPostfix]
    public static void Postfix(Vector3 clickPos, TargetingParameters clickParams, ITargetingSource source,
        List<Thing> __result)
    {
        if (!clickParams.mustBeSelectable) return;
        Map map = Find.CurrentMap;
        if (map == null) return;

        ThinWallPointerCandidates.Reconcile(__result, NearbyThings(map, clickPos), clickPos,
            thing => ThinWallUtility.TryGetOwnedEdge(thing, out OwnedEdge edge) ? edge : (OwnedEdge?)null,
            thing => clickParams.CanTarget(thing, source));
    }

    private static IEnumerable<Thing> NearbyThings(Map map, Vector3 clickPos)
    {
        // A fractional edge box may be in the next cell. Every possible owner is in this
        // fixed neighborhood, including exact segment-end borders; no map-wide search/cache.
        IntVec3 clicked = IntVec3.FromVector3(clickPos);
        for (int x = clicked.x - 1; x <= clicked.x + 1; x++)
        for (int z = clicked.z - 1; z <= clicked.z + 1; z++)
        {
            var cell = new IntVec3(x, 0, z);
            if (!cell.InBounds(map)) continue;
            foreach (Thing thing in cell.GetThingList(map))
                yield return thing;
        }
    }
}
