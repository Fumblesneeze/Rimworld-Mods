using ThinWalls.Geometry;
using ThinWalls.Rendering;
using UnityEngine;
using Verse;

namespace ThinWalls.Buildings;

public sealed class Building_ThinWall : Building, IThinEdgeStructure
{
    public override int HitPoints
    {
        get => base.HitPoints;
        set
        {
            ThinWallDamageGrade before = ThinWallVisualResolver.DamageGrade(base.HitPoints, MaxHitPoints);
            base.HitPoints = value;
            ThinWallDamageGrade after = ThinWallVisualResolver.DamageGrade(base.HitPoints, MaxHitPoints);
            if (before != after && Spawned)
            {
                ThinWallRenderer.Dirty(Map, OwnedEdge);
            }
        }
    }

    public ThinWallSide OwnedSide => (ThinWallSide)Rotation.AsInt;

    public OwnedEdge OwnedEdge => new(Position, OwnedSide);

    public override void SpawnSetup(Map map, bool respawningAfterLoad)
    {
        base.SpawnSetup(map, respawningAfterLoad);
        map.GetComponent<Pathing.ThinWallMapComponent>().NotifyCompletedEdgeChanged(OwnedEdge);
        Rooms.ThinEdgeRegionUtility.NotifyEdgeChanged(this);
    }

    public override void DeSpawn(DestroyMode mode = DestroyMode.Vanish)
    {
        Map previousMap = Map;
        OwnedEdge previousEdge = OwnedEdge;
        Rooms.ThinEdgeRegionUtility.NotifyEdgeChanged(this);
        base.DeSpawn(mode);
        previousMap?.GetComponent<Pathing.ThinWallMapComponent>().NotifyCompletedEdgeChanged(previousEdge);
    }

    public override string GetInspectString()
    {
        string inherited = base.GetInspectString();
        string edge = "TW_InspectEdge".Translate(OwnedSide.ToString().Translate());
        return inherited.NullOrEmpty() ? edge : inherited + "\n" + edge;
    }

    public override void Print(SectionLayer layer)
    {
        ThinWallRenderer.PrintCompleted(layer, this);
    }

    public override void DrawExtraSelectionOverlays()
    {
        base.DrawExtraSelectionOverlays();
        SharedEdge edge = OwnedEdge.Shared;
        float altitude = AltitudeLayer.MetaOverlays.AltitudeFor();
        Vector3 first;
        Vector3 second;
        if (edge.PositiveSide == ThinWallSide.North)
        {
            first = new Vector3(edge.AnchorCell.x, altitude, edge.AnchorCell.z + 1f);
            second = new Vector3(edge.AnchorCell.x + 1f, altitude, edge.AnchorCell.z + 1f);
        }
        else
        {
            first = new Vector3(edge.AnchorCell.x + 1f, altitude, edge.AnchorCell.z);
            second = new Vector3(edge.AnchorCell.x + 1f, altitude, edge.AnchorCell.z + 1f);
        }

        GenDraw.DrawLineBetween(first, second, SimpleColor.White, 0.08f);
    }
}
