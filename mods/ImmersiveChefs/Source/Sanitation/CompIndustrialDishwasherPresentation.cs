using UnityEngine;
using Verse;

namespace ImmersiveChefs;

public sealed class CompProperties_IndustrialDishwasherPresentation : CompProperties
{
    public CompProperties_IndustrialDishwasherPresentation()
    {
        compClass = typeof(CompIndustrialDishwasherPresentation);
    }
}

public sealed class CompIndustrialDishwasherPresentation : ThingComp
{
    private readonly List<Thing> visibleUnits = new(3);
    private readonly ThingOwner?[] sampledOwners = new ThingOwner?[3];
    private CompDishwasher? dishwasher;
    private Graphic? selectedFamily;
    private Graphic? closedGraphic;
    private float nextSampleTime = -1f;
    private Rot4 printedRotation = Rot4.Invalid;
    private Graphic? printedGraphic;
    private Rot4 requestedRotation = Rot4.Invalid;
    private Graphic? requestedGraphic;

    public DishwasherPresentationState State { get; private set; }
    public Graphic? DisplayGraphic => closedGraphic is null ? selectedFamily :
        State == DishwasherPresentationState.Washing ? closedGraphic : selectedFamily;

    public override bool DontDrawParent() => parent.Spawned && ResolveGraphics();

    public override void PostPrintOnto(SectionLayer layer)
    {
        if (!parent.Spawned || !ResolveGraphics())
            return;

        printedGraphic = DisplayGraphic;
        printedRotation = parent.Rotation;
        requestedGraphic = printedGraphic;
        requestedRotation = printedRotation;
        printedGraphic!.Print(layer, parent, 0f);
    }

    public override void DrawAt(Vector3 drawLoc, bool flip = false)
    {
        if (!parent.Spawned || !ResolveGraphics())
            return;

        var now = Time.realtimeSinceStartup;
        if (now >= nextSampleTime)
        {
            dishwasher ??= parent.GetComp<CompDishwasher>();
            State = dishwasher?.ReadPresentation(visibleUnits) ?? DishwasherPresentationState.Empty;
            for (var i = 0; i < visibleUnits.Count; i++)
                sampledOwners[i] = visibleUnits[i].holdingOwner;
            nextSampleTime = now + 0.25f;
        }

        if (!ReferenceEquals(requestedGraphic, DisplayGraphic) || requestedRotation != parent.Rotation)
        {
            parent.DirtyMapMesh(parent.Map);
            // Keep a requested state too, so a delayed section rebuild does not dirty it every frame.
            requestedGraphic = DisplayGraphic;
            requestedRotation = parent.Rotation;
        }

        // A section can still show the old closed endpoint for a frame after being dirtied.
        if (!ReferenceEquals(printedGraphic, DisplayGraphic) || printedRotation != parent.Rotation)
            return;

        for (var i = 0; i < visibleUnits.Count; i++)
        {
            var ware = visibleUnits[i];
            if (ware.Destroyed || ware.Spawned || !ReferenceEquals(ware.holdingOwner, sampledOwners[i]))
                continue;

            var unitIndex = 0;
            for (var prior = 0; prior <= i; prior++)
                if (ReferenceEquals(visibleUnits[prior], ware))
                    unitIndex++;
            if (unitIndex > ware.stackCount || !DishwasherPresentationPolicy.TryGetContentSlot(
                    State, parent.Rotation.AsInt, i, visibleUnits.Count, out var slot))
                continue;

            var displayedUnit = CompTablewareStack.For(ware)?.UnitView(unitIndex - 1) ?? ware;
            var graphic = displayedUnit.Graphic;
            var longestEdge = Math.Max(graphic.drawSize.x, graphic.drawSize.y);
            if (longestEdge <= 0f)
                continue;
            var scale = slot.Size / longestEdge;
            // Ordinary printed planes have a .01 top-vertex bias. Keep contents above that
            // whole plane and below the native damage layer; position remains inside the aperture.
            var position = drawLoc + new Vector3(slot.X, 0.04f + i * 0.001f, slot.Z);
            var matrix = Matrix4x4.TRS(position, Quaternion.identity,
                new Vector3(graphic.drawSize.x * scale, 1f, graphic.drawSize.y * scale));
            GenDraw.DrawMeshNowOrLater(MeshPool.plane10, matrix, graphic.MatSingleFor(displayedUnit), false);
        }
    }

    private bool ResolveGraphics()
    {
        var graphic = parent.Graphic;
        if (ReferenceEquals(graphic, selectedFamily))
            return closedGraphic is not null;

        selectedFamily = graphic;
        closedGraphic = null;
        if (graphic is not Graphic_Multi)
            return false;

        var closedPath = DishwasherPresentationPolicy.TexturePath(graphic.path, DishwasherPresentationState.Washing);
        foreach (var direction in GenAdj.CardinalDirections)
        {
            var texturePath = DishwasherPresentationPolicy.CardinalTexturePath(
                closedPath, Rot4.FromIntVec3(direction).AsInt);
            if (ContentFinder<Texture2D>.Get(texturePath, reportFailure: false) is null)
            {
                Log.WarningOnce("[ImmersiveChefs] Dishwasher presentation has no complete closed family for " +
                    graphic.path + "; preserving the selected graphic.", GenText.StableStringHash(closedPath));
                return false;
            }
        }

        closedGraphic = GraphicDatabase.Get<Graphic_Multi>(closedPath, graphic.Shader, graphic.drawSize,
            graphic.color, graphic.colorTwo, graphic.data);
        return true;
    }
}
