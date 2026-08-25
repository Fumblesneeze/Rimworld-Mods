using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using ThinWalls.Geometry;
using ThinWalls.Rendering;
using UnityEngine;
using Verse;

namespace ThinWalls.Designation;

public class Designator_ThinWall : Designator_Build
{
    private static readonly FieldInfo StartDragCellField = AccessTools.Field(typeof(DesignationDragger), "startDragCell");
    private ThinWallSide retainedSide = ThinWallSide.South;
    private ThinWallSide singleCellSideAtDragStart = ThinWallSide.South;
    private IntVec3 trackedDragStart = IntVec3.Invalid;
    private bool trackingDrag;

    public Designator_ThinWall() : this(
        ThinWallUtility.ThinWallDefName,
        "TW_DesignatorLabel",
        "TW_DesignatorDescription")
    {
    }

    protected Designator_ThinWall(string defName, string labelKey, string descriptionKey)
        : base(DefDatabase<ThingDef>.GetNamed(defName))
    {
        defaultLabel = labelKey.Translate();
        defaultDesc = descriptionKey.Translate();
        placingRot = Rot4.South;
    }

    public ThinWallSide RetainedSide => retainedSide;

    public override AcceptanceReport CanDesignateCell(IntVec3 c)
    {
        UpdateSideFromDrag();
        AcceptanceReport placement = base.CanDesignateCell(c);
        return placement.Accepted
            ? Placement.ThinWallPlacementRules.Validate(PlacingDef, c, placingRot, Map)
            : placement;
    }

    public override void DesignateMultiCell(IEnumerable<IntVec3> cells)
    {
        List<IntVec3> orderedCells = cells.ToList();
        DesignationDragger? dragger = Find.DesignatorManager?.Dragger;
        if (dragger?.Dragging == true)
        {
            UpdateSideFromDrag();
        }
        else
        {
            // Semantic designator automation cannot keep RimWorld's dragger alive while it
            // invokes DesignateMultiCell. In that path placingRot carries the side chosen
            // from the original ordered drag vector. The live player path above remains
            // authoritative and derives the side directly from start cell to mouse cell.
            retainedSide = (ThinWallSide)placingRot.AsInt;
        }

        base.DesignateMultiCell(orderedCells);
    }

    public override void DesignateSingleCell(IntVec3 c)
    {
        UpdateSideFromDrag();
        base.DesignateSingleCell(c);
    }

    public override void Selected()
    {
        retainedSide = ThinWallSide.South;
        singleCellSideAtDragStart = retainedSide;
        trackedDragStart = IntVec3.Invalid;
        trackingDrag = false;
        placingRot = Rot4.South;
    }

    public override void SelectedUpdate()
    {
        UpdateSideFromDrag();
        base.SelectedUpdate();
    }

    public override void RenderHighlight(List<IntVec3> dragCells)
    {
        UpdateSideFromDrag();
        Material material = PreviewMaterial(
            (ThingDef)PlacingDef,
            StuffDef,
            new Color(0.45f, 1f, 0.55f),
            0.7f,
            retainedSide);
        foreach (IntVec3 cell in dragCells)
        {
            ThinWallRenderer.DrawRealtime(
                new OwnedEdge(cell, retainedSide),
                1,
                material,
                AltitudeLayer.Blueprint.AltitudeFor());
        }
    }

    protected override void DrawGhost(Color ghostCol)
    {
        UpdateSideFromDrag();
        Material material = PreviewMaterial(
            (ThingDef)PlacingDef,
            StuffDef,
            ghostCol,
            0.7f,
            retainedSide);
        ThinWallRenderer.DrawRealtime(
            new OwnedEdge(UI.MouseCell(), retainedSide),
            1,
            material,
            AltitudeLayer.Blueprint.AltitudeFor());
    }

    private Material PreviewMaterial(
        ThingDef buildDef,
        ThingDef? stuff,
        Color stateColor,
        float stateWeight,
        ThinWallSide side) => ThinWallRenderer.StateEdgeMaterial(
            buildDef,
            stuff,
            stateColor,
            stateWeight,
            side,
            ThinWallUtility.IsThinDoorDef(buildDef));

    public override void SelectedProcessInput(UnityEngine.Event ev)
    {
        base.SelectedProcessInput(ev);
        DesignationDragger? dragger = Find.DesignatorManager?.Dragger;
        if (dragger?.Dragging != true)
        {
            retainedSide = (ThinWallSide)placingRot.AsInt;
            singleCellSideAtDragStart = retainedSide;
        }
    }

    public override void DoExtraGuiControls(float leftX, float bottomY)
    {
        base.DoExtraGuiControls(leftX, bottomY);
    }

    private void UpdateSideFromDrag()
    {
        DesignationDragger? dragger = Find.DesignatorManager?.Dragger;
        if (dragger == null || !dragger.Dragging)
        {
            trackingDrag = false;
            trackedDragStart = IntVec3.Invalid;
            retainedSide = (ThinWallSide)placingRot.AsInt;
            return;
        }

        var start = (IntVec3)StartDragCellField.GetValue(dragger);
        if (!trackingDrag || start != trackedDragStart)
        {
            trackingDrag = true;
            trackedDragStart = start;
            singleCellSideAtDragStart = (ThinWallSide)placingRot.AsInt;
        }
        IntVec3 end = UI.MouseCell();
        retainedSide = ThinWallDirection.SideForDrag(
            end.x - start.x,
            end.z - start.z,
            singleCellSideAtDragStart);
        placingRot = RotationFor(retainedSide);
    }

    private static Rot4 RotationFor(ThinWallSide side)
    {
        return side switch
        {
            ThinWallSide.North => Rot4.North,
            ThinWallSide.East => Rot4.East,
            ThinWallSide.South => Rot4.South,
            ThinWallSide.West => Rot4.West,
            _ => Rot4.South,
        };
    }
}
