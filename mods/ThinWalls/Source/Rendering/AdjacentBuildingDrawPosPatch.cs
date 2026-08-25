using System.Runtime.CompilerServices;
using HarmonyLib;
using ThinWalls.Geometry;
using ThinWalls.Pathing;
using UnityEngine;
using Verse;

namespace ThinWalls.Rendering;

[HarmonyPatch(typeof(Thing), nameof(Thing.DrawPos), MethodType.Getter)]
public static class AdjacentBuildingDrawPosPatch
{
    private static readonly ConditionalWeakTable<Thing, CacheEntry> Cache = new();

    [HarmonyPostfix]
    public static void Postfix(Thing __instance, ref Vector3 __result)
    {
        if (AdjacentBuildingMapMeshContext.IsPrinting(__instance))
        {
            return;
        }

        __result += ResolveOffset(__instance);
    }

    internal static Vector3 ResolveOffset(Thing thing)
    {
        Map? map = thing.Map;
        if (map == null || !AdjacentBuildingVisualOffset.IsEligible(thing, out ThingDef buildDef))
        {
            return Vector3.zero;
        }

        ThinWallMapComponent component = map.GetComponent<ThinWallMapComponent>();
        CacheEntry entry = Cache.GetOrCreateValue(thing);
        CellRect footprint = GenAdj.OccupiedRect(
            thing.Position,
            thing.Rotation,
            buildDef.Size);
        if (!HasThinPerimeter(map, footprint) ||
            !AdjacentBuildingGraphicClearance.TryResolve(thing, buildDef, footprint,
                out AdjacentBuildingGraphicClearance.ResolvedClearance resolvedClearance))
        {
            return Vector3.zero;
        }
        if (!ReferenceEquals(entry.Map, map) || entry.Version != component.ThinEdgeVisualRevision ||
            entry.Position != thing.Position || entry.Rotation != thing.Rotation ||
            !ReferenceEquals(entry.BuildDef, buildDef) ||
            entry.GraphicClearanceIdentity != resolvedClearance.Identity)
        {
            entry.Offset = AdjacentBuildingVisualOffset.Resolve(
                footprint,
                edge => ThinWallUtility.HasEdgeStructure(map, edge, completedOnly: false),
                resolvedClearance.Clearance);
            entry.Map = map;
            entry.Version = component.ThinEdgeVisualRevision;
            entry.Position = thing.Position;
            entry.Rotation = thing.Rotation;
            entry.BuildDef = buildDef;
            entry.GraphicClearanceIdentity = resolvedClearance.Identity;
        }

        return entry.Offset;
    }

    private static bool HasThinPerimeter(Map map, CellRect footprint)
    {
        for (int x = footprint.minX; x <= footprint.maxX; x++)
        {
            if (ThinWallUtility.HasEdgeStructure(map, new OwnedEdge(
                    new IntVec3(x, 0, footprint.maxZ), ThinWallSide.North).Shared, completedOnly: false) ||
                ThinWallUtility.HasEdgeStructure(map, new OwnedEdge(
                    new IntVec3(x, 0, footprint.minZ), ThinWallSide.South).Shared, completedOnly: false))
            {
                return true;
            }
        }
        for (int z = footprint.minZ; z <= footprint.maxZ; z++)
        {
            if (ThinWallUtility.HasEdgeStructure(map, new OwnedEdge(
                    new IntVec3(footprint.maxX, 0, z), ThinWallSide.East).Shared, completedOnly: false) ||
                ThinWallUtility.HasEdgeStructure(map, new OwnedEdge(
                    new IntVec3(footprint.minX, 0, z), ThinWallSide.West).Shared, completedOnly: false))
            {
                return true;
            }
        }
        return false;
    }

    private sealed class CacheEntry
    {
        public Map? Map;
        public int Version = -1;
        public IntVec3 Position = IntVec3.Invalid;
        public Rot4 Rotation = Rot4.Invalid;
        public ThingDef? BuildDef;
        public int GraphicClearanceIdentity;
        public Vector3 Offset;
    }
}
