using System.Collections.Generic;
using HarmonyLib;
using Verse;
using Verse.AI;

namespace ThinWalls.Pathing;

[HarmonyPatch(typeof(TouchPathEndModeUtility), nameof(TouchPathEndModeUtility.AddAllowedAdjacentRegions))]
public static class ThinWallTouchRegionsPatch
{
    [HarmonyPostfix]
    public static void Postfix(LocalTargetInfo dest, TraverseParms traverseParams, Map map, List<Region> regions)
    {
        if (regions.Count == 0 || !ThinWallMapComponent.TryGet(map, out var component) ||
            !component.HasCompletedEdgeStructures || (dest.HasThing && ThinWallUtility.IsThinEdgeDef(dest.Thing.def))) return;
        CellRect occupied = dest.HasThing ? dest.Thing.OccupiedRect() : CellRect.SingleCell(dest.Cell);
        var legal = SimplePool<HashSet<Region>>.Get();
        try
        {
            PathingContext pathing = map.pathing.For(traverseParams);
            foreach (IntVec3 candidate in occupied.ExpandedBy(1).Cells)
            {
                if (!candidate.InBounds(map) || occupied.Contains(candidate) ||
                    !TouchPathEndModeUtility.IsAdjacentOrInsideAndAllowedToTouch(candidate, dest, pathing)) continue;
                Region region = candidate.GetRegion(map);
                if (region == null || legal.Contains(region)) continue;
                foreach (IntVec3 target in occupied.Cells)
                {
                    if (!candidate.AdjacentTo8WayOrInside(target) || !component.AllowsStep(candidate, target, traverseParams)) continue;
                    legal.Add(region);
                    break;
                }
            }
            regions.RemoveAll(region => !legal.Contains(region));
        }
        finally
        {
            legal.Clear();
            SimplePool<HashSet<Region>>.Return(legal);
        }
    }
}
